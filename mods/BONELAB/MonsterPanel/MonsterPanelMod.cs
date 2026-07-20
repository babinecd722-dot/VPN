using System;
using System.Reflection;
using BoneLib.BoneMenu;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Combat;
using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.PuppetMasta;
using LabFusion.Entities;
using LabFusion.Extensions;
using LabFusion.Marrow.Extenders;
using LabFusion.RPC;
using LabFusion.Utilities;
using MelonLoader;
using UnityEngine;
using MHealth = Il2CppSLZ.Marrow.Health;

[assembly: MelonInfo(typeof(MonsterPanel.MonsterPanelMod), "MONSTER Panel", "2.30.19", "you")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace MonsterPanel
{
    public class MonsterPanelMod : MelonMod
    {
        /// <summary>Бессмертие игрока.</summary>
        public static bool Invincible { get; private set; }

        /// <summary>Монстер-урон: ваншот врагов/объектов/игроков + жёсткий отброс.</summary>
        public static bool MonsterDamage { get; private set; }

        /// <summary>Kill Aura: макс. урон всем игрокам рядом (радиус AuraRange), без наведения.</summary>
        public static bool KillAura { get; private set; }

        // ---- Kill Aura настройки (меню) ----
        /// <summary>Режим: бить ВСЕХ игроков (true) или только выбранного (false).</summary>
        public static bool AuraGlobal = true;
        /// <summary>Скорость: сколько раз в секунду шлём урон (1 = медленно, 60 = моментально).</summary>
        public static float AuraRate = 15f;
        /// <summary>Игнорировать дистанцию (бить независимо от расстояния).</summary>
        public static bool AuraIgnoreDist = true;
        /// <summary>SmallID выбранной цели (для одиночного режима). -1 = не выбран.</summary>
        public static int AuraTargetSid = -1;
        /// <summary>Молния по цели(ям) при Kill Aura — локальный визуал, бьёт ровно в игрока, без лагов.</summary>
        public static bool AuraLightning = true;

        /// <summary>Бесконечные патроны: запас в инвентаре бесконечный (магазин расходуется штатно).</summary>
        public static bool InfiniteAmmo { get; private set; }

        /// <summary>Tank: тебя нельзя схватить/поднять (движение и удары как обычно).</summary>
        public static bool TankMode { get; private set; }

        /// <summary>Disarm: у всех рядом вырывает оружие; своё при этом забрать нельзя.</summary>
        public static bool Disarm { get; private set; }

        private const float LaunchSpeed = 28f;
        private const float MaxDamage = 1_000_000f;

        // Aura (Kill / Disarm)
        private const float AuraRange = 15f;             // «рядом», метры
        private const float KillTickInterval = 0.25f;    // как часто бьём
        private const float DisarmTickInterval = 0.2f;   // как часто вырываем стволы
        private const float DisarmRadius = 1.8f;         // запасной sweep вокруг рук
        private const float DisarmSpeed = 28f;           // сила вырывания

        // Tank Mode
        private const float TankReapplyInterval = 0.5f;
        private static bool _tankApplied;
        private static float _tankTimer;

        /// <summary>Флаг: сейчас шлём урон Kill Aura — бустим до максимума даже без Monster Damage.</summary>
        private static bool _remoteKillSending;

        /// <summary>LabFusion загружен (кэш) — Kill/Disarm Aura без сети бессмысленны.</summary>
        private static bool _fusionLoaded;

        private const string PlayerHealthType = "Il2CppSLZ.Marrow.Player_Health";
        private const string FusionReceiverPatch = "LabFusion.Patching.PlayerDamageReceiverPatches";

        [ThreadStatic] private static bool _reentry;

        public override void OnInitializeMelon()
        {
            _fusionLoaded = Teleporter.FusionLoaded;
            if (_fusionLoaded)
            {
                PidSpoof.Init(HarmonyInstance); // Spoofing PID: hook SetPlatformID + restore saved state
                FusionCleanup.Install(HarmonyInstance); // Fusion Admin → Cleanup → Despawn All (non-host too)
                Tracking.Init(HarmonyInstance); // profile Add to Tracking + /v1/track poll
            }
            AntiManip.Install(HarmonyInstance); // silent Dev Manipulator immunity (no UI)
            BuildMenu();
            ApplyPatches();
            MelonLogger.Msg("MONSTER Panel loaded.");
        }

        public override void OnUpdate()
        {
            TankUpdate();

            if (_fusionLoaded)
            {
                Freedom.Tick();   // снимаем ЧУЖИЕ констрейны с тебя и предметов рядом (свои не трогаем)
                AntiManip.Tick(); // backup: release manipulator locks on our rig
                if (KillAura) Aura.KillTick();
                if (Disarm) Aura.DisarmTick();
                Guards.Tick();
                NetLightning.Tick();   // авто-удаление отживших сетевых молний
                AdminNick.Tick();      // OWNER/dev-gold shimmer nametag (metadata @ ~10 Hz)
                Tracking.Tick();       // idle cache warm-up + join-alert cooldowns
            }
            else
            {
                // Singleplayer / no Fusion: still strip local manipulator forces.
                AntiManip.Tick();
            }
        }

        // ---------------- Kill Aura / Disarm Aura (LabFusion) ----------------
        //
        // Без наведения и крестиков: идём по списку сетевых игроков NetworkPlayer.Players,
        // берём тех, кто рядом (радиус AuraRange), и бьём/разоружаем. Код с типами LabFusion
        // изолирован в классе Aura — JIT-ится только при загруженном Fusion.

        /// <summary>Мировая позиция рига (физ-риг → корень).</summary>
        private static Vector3 RigPos(RigManager rig)
        {
            try { return rig.physicsRig != null ? rig.physicsRig.transform.position : rig.transform.position; }
            catch { return rig.transform.position; }
        }

        /// <summary>Точка груди игрока (чуть выше физ-рига) — чтобы молния била ровно в него.</summary>
        private static Vector3 ChestPos(RigManager rig) => RigPos(rig) + Vector3.up * 1.1f;

        /// <summary>Одна максимальная атака в ресивер тела (тот же путь, что и Monster Damage:
        /// game ReceiveAttack + патч LabFusion шлёт урон владельцу; буст гарантируем флагом).</summary>
        private static void SendAttackTo(PlayerDamageReceiver recv, Vector3 fromPos)
        {
            if (recv == null) return;
            var proxy = LocalProxy();
            if (proxy == null) return;   // без proxy патч LabFusion не отправит урон владельцу
            Vector3 dir = (recv.transform.position - fromPos).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            var attack = new Attack
            {
                damage = MaxDamage,
                attackType = AttackType.Blunt,
                direction = dir,
                origin = fromPos,
                normal = -dir,
                collider = recv.GetComponentInChildren<Collider>(),
                proxy = proxy,   // КЛЮЧ: root proxy → мой локальный риг, иначе ReceiveAttack не шлёт SendPlayerDamage
            };
            try { recv.ReceiveAttack(attack); } catch (Exception e) { MelonLogger.Warning("Kill Aura send: " + e.Message); }
        }

        /// <summary>TriggerRefProxy собственного рига (по нему NPC видят игрока). Его root резолвится
        /// в мой локальный риг, поэтому патч LabFusion.ReceiveAttack посылает SendPlayerDamage владельцу.
        /// Пересоздаётся при смене аватара — поэтому перечитываем, если ссылка протухла.</summary>
        private static Il2CppSLZ.Marrow.AI.TriggerRefProxy _localProxy;
        private static Il2CppSLZ.Marrow.AI.TriggerRefProxy LocalProxy()
        {
            try
            {
                if (_localProxy != null) return _localProxy;
                var rig = BoneLib.Player.RigManager;
                if (rig == null) return null;
                _localProxy = rig.GetComponentInChildren<Il2CppSLZ.Marrow.AI.TriggerRefProxy>(true);
            }
            catch (Exception e) { MelonLogger.Warning("LocalProxy: " + e.Message); }
            return _localProxy;
        }

        private static readonly Collider[] _overlapBuf = new Collider[64];
        private static readonly System.Collections.Generic.HashSet<int> _yankSeen = new System.Collections.Generic.HashSet<int>();

        /// <summary>
        /// Disarm одного сетевого игрока:
        /// 1) Grabber.Detach — чистит Fusion _lastGrabs (иначе CheckDetachAndReattach сразу вернёт хват),
        /// 2) ForceDetach + TakeOwnership по grip в руках,
        /// 3) DropWeapon из кобур,
        /// 4) запасной OverlapSphere вокруг рук (не скипает held-пропы на риге).
        /// </summary>
        private static bool DisarmPlayer(NetworkPlayer np, Vector3 fromPos)
        {
            if (np == null || !np.HasRig || np.RigRefs == null) return false;
            var refs = np.RigRefs;
            bool any = false;

            // Снять grip с Fusion-кэша ПЕРЕД физическим detach — иначе патч Grip.OnDetachedFromHand
            // зовёт CheckDetachAndReattach и оружие мгновенно возвращается в руку на нашем клиенте.
            try
            {
                var grabber = np.Grabber;
                if (grabber != null)
                {
                    grabber.Detach(Handedness.LEFT);
                    grabber.Detach(Handedness.RIGHT);
                }
            }
            catch { }

            any |= YankHeldHand(refs.LeftHand, fromPos);
            any |= YankHeldHand(refs.RightHand, fromPos);
            any |= DropHolsters(refs, fromPos);

            try
            {
                if (refs.LeftHand != null) YankLooseNear(refs.LeftHand.transform.position, fromPos);
                if (refs.RightHand != null) YankLooseNear(refs.RightHand.transform.position, fromPos);
            }
            catch { }

            return any;
        }

        private static bool YankHeldHand(Hand hand, Vector3 fromPos)
        {
            if (hand == null) return false;
            Grip grip = ResolveHandGrip(hand);
            if (grip == null) return false;
            return YankGrip(grip, fromPos);
        }

        private static Grip ResolveHandGrip(Hand hand)
        {
            try
            {
                var recv = hand.AttachedReceiver;
                if (recv != null)
                {
                    var asGrip = recv.TryCast<Grip>();
                    if (asGrip != null) return asGrip;
                }
            }
            catch { }

            try
            {
                var go = hand.m_CurrentAttachedGO;
                if (go == null) return null;
                var cached = Grip.Cache.Get(go);
                if (cached != null) return cached;
                return go.GetComponent<Grip>();
            }
            catch { return null; }
        }

        private static bool DropHolsters(RigRefs refs, Vector3 fromPos)
        {
            if (refs.RigSlots == null) return false;
            bool any = false;
            foreach (var slot in refs.RigSlots)
            {
                if (slot == null) continue;
                WeaponSlot weapon = null;
                try { weapon = slot._slottedWeapon; } catch { continue; }
                if (weapon == null) continue;

                Grip grip = null;
                try { grip = weapon.grip; } catch { }

                try { slot.DropWeapon(); } catch { }

                if (grip != null)
                    any |= YankGrip(grip, fromPos);
            }
            return any;
        }

        private static bool YankGrip(Grip grip, Vector3 fromPos)
        {
            if (grip == null) return false;

            // Не трогаем грипы самого тела (AvatarGrip / риг без пропа).
            try
            {
                if (grip.TryCast<AvatarGrip>() != null) return false;
            }
            catch { }

            TakeOwnershipFromGrip(grip);

            // Сорвать все руки с хоста
            try
            {
                if (grip.HasHost)
                {
                    var host = grip.Host.TryCast<InteractableHost>();
                    if (host != null) host.TryDetach();
                }
            }
            catch { }

            try
            {
                // Копия списка — ForceDetach мутирует attachedHands.
                var hands = grip.attachedHands;
                if (hands != null && hands.Count > 0)
                {
                    var copy = new System.Collections.Generic.List<Hand>(hands.Count);
                    foreach (var h in hands)
                        if (h != null) copy.Add(h);
                    foreach (var h in copy)
                    {
                        try { grip.ForceDetach(h); } catch { }
                        try { h.TryDetach(); } catch { }
                    }
                }
            }
            catch { }

            LaunchGrip(grip, fromPos);
            return true;
        }

        private static void LaunchGrip(Grip grip, Vector3 fromPos)
        {
            Rigidbody rb = null;
            try
            {
                if (grip.HasHost && grip.Host != null)
                    rb = grip.Host.Rb;
            }
            catch { }
            if (rb == null)
            {
                try { rb = grip.GetComponentInParent<Rigidbody>(); } catch { }
            }
            if (rb == null) return;

            Vector3 dir = rb.position - fromPos;
            dir.y += 0.55f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.up + Vector3.forward * 0.2f;
            try { rb.velocity = dir.normalized * DisarmSpeed; } catch { }
            try { rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 8f; } catch { }
        }

        private static void TakeOwnershipFromGrip(Grip grip)
        {
            try
            {
                // Предпочтительно кэш GripExtender → NetworkEntity
                if (LabFusion.Marrow.Extenders.GripExtender.Cache.TryGet(grip, out var ne) && ne != null)
                {
                    if (ne.IsRegistered && ne.ID != 0 && !ne.IsOwner)
                        LabFusion.Entities.NetworkEntityManager.TakeOwnership(ne);
                    return;
                }
            }
            catch { }

            try
            {
                var marrow = grip._marrowEntity;
                if (marrow == null && grip.HasHost)
                {
                    var host = grip.Host.TryCast<InteractableHost>();
                    if (host != null) marrow = host.marrowEntity;
                }
                if (marrow == null) return;
                var ne = LabFusion.Entities.IMarrowEntityExtender.Cache.Get(marrow);
                if (ne == null || !ne.IsRegistered || ne.ID == 0 || ne.IsOwner) return;
                LabFusion.Entities.NetworkEntityManager.TakeOwnership(ne);
            }
            catch { }
        }

        /// <summary>
        /// Запасной sweep: предметы рядом с рукой. Раньше скипали всё под RigManager —
        /// а held-оружие как раз parented к руке/ригу, поэтому Disarm ничего не брал.
        /// Теперь скипаем только «чистое» тело без InteractableHost.
        /// </summary>
        private static void YankLooseNear(Vector3 center, Vector3 fromPos)
        {
            try
            {
                int count = Physics.OverlapSphereNonAlloc(center, DisarmRadius, _overlapBuf,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    var col = _overlapBuf[i];
                    if (col == null) continue;
                    var rb = col.attachedRigidbody;
                    if (rb == null) continue;
                    if (!_yankSeen.Add(rb.GetInstanceID())) continue;

                    // Тело игрока без хоста — не трогаем. Held prop с InteractableHost — трогаем.
                    var host = rb.GetComponentInParent<InteractableHost>();
                    if (host == null)
                    {
                        if (rb.GetComponentInParent<RigManager>() != null) continue;
                        continue; // нет хоста — не оружие
                    }

                    Grip grip = null;
                    try
                    {
                        foreach (var g in host.GetComponentsInChildren<Grip>(true))
                        {
                            if (g != null && g.TryCast<AvatarGrip>() == null) { grip = g; break; }
                        }
                    }
                    catch { }
                    if (grip != null) YankGrip(grip, fromPos);
                    else
                    {
                        TakeItemOwnership(col);
                        Vector3 dir = rb.position - fromPos; dir.y += 0.55f;
                        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.up;
                        try { rb.velocity = dir.normalized * DisarmSpeed; } catch { }
                    }
                }
            }
            catch (Exception e) { MelonLogger.Warning("Disarm sweep: " + e.Message); }
        }

        /// <summary>Резолвим предмет → MarrowEntity → NetworkEntity и забираем владение себе.</summary>
        private static void TakeItemOwnership(Collider col)
        {
            try
            {
                var me = col.GetComponentInParent<Il2CppSLZ.Marrow.Interaction.MarrowEntity>();
                if (me == null) return;
                var ne = LabFusion.Entities.IMarrowEntityExtender.Cache.Get(me);
                if (ne == null) return;
                if (!ne.IsRegistered || ne.ID == 0) return;
                if (ne.IsOwner) return;
                LabFusion.Entities.NetworkEntityManager.TakeOwnership(ne);
            }
            catch { }
        }

        // ---------------- Disarm: защита своего оружия ----------------
        //
        // Пока Disarm ON — чужие не могут забрать твой лоадаут тем же путём, которым
        // ты снимаешь их: TakeOwnership обратно + Grabber.Detach / ForceDetach / SendObjectDetach
        // на чужие руки. Свои руки и кобуры не трогаем (не дропаем своё).

        private static void GuardOwnLoadout()
        {
            try
            {
                Hand localL = BoneLib.Player.LeftHand;
                Hand localR = BoneLib.Player.RightHand;

                GuardOwnHeld(localL, localL, localR);
                GuardOwnHeld(localR, localL, localR);

                var rig = BoneLib.Player.RigManager;
                if (rig == null) return;
                var refs = new RigRefs(rig);
                GuardOwnHolsters(refs, localL, localR);
            }
            catch (Exception e) { MelonLogger.Warning("Disarm guard: " + e.Message); }
        }

        private static void GuardOwnHeld(Hand hand, Hand localL, Hand localR)
        {
            if (hand == null) return;
            Grip grip = ResolveHandGrip(hand);
            if (grip == null) return;
            try { if (grip.TryCast<AvatarGrip>() != null) return; } catch { }

            TakeOwnershipFromGrip(grip);
            StripForeignHandsFromGrip(grip, localL, localR);
        }

        private static void GuardOwnHolsters(RigRefs refs, Hand localL, Hand localR)
        {
            if (refs == null || refs.RigSlots == null) return;
            foreach (var slot in refs.RigSlots)
            {
                if (slot == null) continue;
                WeaponSlot weapon = null;
                try { weapon = slot._slottedWeapon; } catch { continue; }
                if (weapon == null) continue;

                Grip grip = null;
                try { grip = weapon.grip; } catch { }
                if (grip == null) continue;

                TakeOwnershipFromGrip(grip);
                StripForeignHandsFromGrip(grip, localL, localR);
            }
        }

        /// <summary>
        /// Срывает с грипа все руки, кроме локальных L/R.
        /// Зеркало DisarmPlayer: сначала Fusion Grabber.Detach у владельца руки, потом ForceDetach,
        /// плюс SendObjectDetach чтобы отцепление ушло в сеть.
        /// </summary>
        private static void StripForeignHandsFromGrip(Grip grip, Hand localL, Hand localR)
        {
            if (grip == null) return;

            System.Collections.Generic.List<Hand> copy = null;
            try
            {
                var hands = grip.attachedHands;
                if (hands == null || hands.Count == 0) return;
                copy = new System.Collections.Generic.List<Hand>(hands.Count);
                foreach (var h in hands)
                    if (h != null) copy.Add(h);
            }
            catch { return; }
            if (copy == null || copy.Count == 0) return;

            foreach (var h in copy)
            {
                if (h == null) continue;
                if (h == localL || h == localR) continue;
                if (IsLocalPlayerHand(h)) continue;

                // Fusion-кэш хвата у чужого NetworkPlayer — иначе CheckDetachAndReattach вернёт ствол им.
                try
                {
                    foreach (var np in NetworkPlayer.Players)
                    {
                        if (np == null || np.PlayerID == null || np.PlayerID.IsMe || !np.HasRig) continue;
                        var r = np.RigRefs;
                        if (r == null) continue;
                        bool theirs = h == r.LeftHand || h == r.RightHand;
                        if (!theirs) continue;
                        try
                        {
                            Handedness hd;
                            try { hd = h.handedness; }
                            catch { hd = (h == r.LeftHand) ? Handedness.LEFT : Handedness.RIGHT; }
                            np.Grabber?.Detach(hd);
                        }
                        catch { }
                        break;
                    }
                }
                catch { }

                try { LabFusion.Grabbables.GrabHelper.SendObjectDetach(h); } catch { }
                try { grip.ForceDetach(h); } catch { }
                try { h.TryDetach(); } catch { }
            }
        }

        private static bool IsLocalPlayerHand(Hand hand)
        {
            if (hand == null) return false;
            try
            {
                if (hand == BoneLib.Player.LeftHand || hand == BoneLib.Player.RightHand) return true;
            }
            catch { }
            try
            {
                var rig = BoneLib.Player.RigManager;
                if (rig != null && hand.transform != null && hand.transform.IsChildOf(rig.transform))
                    return true;
            }
            catch { }
            return false;
        }

        // Весь код с типами LabFusion — здесь (JIT только при загруженном Fusion).
        private static class Aura
        {
            private static float _killTimer, _disarmTimer, _logTimer;
            // позиции груди задетых целей — для сетевой молнии (round-robin), переиспользуем список.
            private static readonly System.Collections.Generic.List<Vector3> _hitPos =
                new System.Collections.Generic.List<Vector3>();
            /// <summary>Kill Aura: с частотой AuraRate/сек — макс. урон цели(ям).
            /// Режим ALL (AuraGlobal) — по всем; иначе только по AuraTargetSid.
            /// AuraIgnoreDist — без ограничения радиуса.</summary>
            public static void KillTick()
            {
                float rate = AuraRate < 1f ? 1f : (AuraRate > 60f ? 60f : AuraRate);
                _killTimer -= Time.deltaTime;
                if (_killTimer > 0f) return;
                _killTimer = 1f / rate;

                var meRig = BoneLib.Player.RigManager;
                if (meRig == null) return;
                Vector3 me = RigPos(meRig);
                bool useDist = !AuraIgnoreDist;
                float r2 = AuraRange * AuraRange;

                bool doNet = NetLightning.Ready(1f / rate);   // троттл сетевой молнии (видят все)
                if (doNet) _hitPos.Clear();

                try
                {
                    _remoteKillSending = true;
                    int hit = 0;
                    foreach (var np in NetworkPlayer.Players)
                    {
                        if (np == null || np.PlayerID == null || np.PlayerID.IsMe || !np.HasRig) continue;
                        if (!AuraGlobal && np.PlayerID.SmallID != AuraTargetSid) continue;   // одиночная цель
                        RigManager rig = np.RigRefs.RigManager;
                        if (rig == null) continue;
                        Vector3 tp = ChestPos(rig);   // точно по груди игрока, а не «рядом»
                        if (useDist && (tp - me).sqrMagnitude > r2) continue;
                        foreach (var recv in rig.GetComponentsInChildren<PlayerDamageReceiver>())
                            if (recv != null) SendAttackTo(recv, me);
                        if (doNet) _hitPos.Add(tp);   // копим позиции груди для сетевой молнии
                        hit++;
                    }
                    if (doNet) NetLightning.StrikeOne(_hitPos);   // один сетевой удар за залп (round-robin)
                    // лог не чаще раза в 2 сек, иначе спам на высокой скорости
                    _logTimer -= 1f / rate;
                    if (hit > 0 && _logTimer <= 0f)
                    {
                        _logTimer = 2f;
                        MelonLogger.Msg($"Kill Aura: hitting {hit} player(s) at {rate:0}/s ({(AuraGlobal ? "ALL" : "target " + AuraTargetSid)}).");
                    }
                }
                catch (Exception e) { MelonLogger.Warning("Kill Aura: " + e.Message); }
                finally { _remoteKillSending = false; }
            }

            /// <summary>Disarm Aura: раз в DisarmTickInterval — чужие стволы + защита своего лоадаута.</summary>
            public static void DisarmTick()
            {
                _disarmTimer -= Time.deltaTime;
                if (_disarmTimer > 0f) return;
                _disarmTimer = DisarmTickInterval;

                var meRig = BoneLib.Player.RigManager;
                if (meRig == null) return;
                Vector3 me = RigPos(meRig);
                float r2 = AuraRange * AuraRange;

                // Сначала страхуем свой лоадаут (ownership + срыв чужих рук), потом бьём чужих.
                GuardOwnLoadout();

                try
                {
                    _yankSeen.Clear();
                    int hit = 0;
                    foreach (var np in NetworkPlayer.Players)
                    {
                        if (np == null || np.PlayerID == null || np.PlayerID.IsMe || !np.HasRig) continue;
                        RigManager rig = np.RigRefs.RigManager;
                        if (rig == null) continue;
                        Vector3 p = RigPos(rig);
                        if ((p - me).sqrMagnitude > r2) continue;
                        if (DisarmPlayer(np, me)) hit++;
                    }
                    _logTimer -= DisarmTickInterval;
                    if (hit > 0 && _logTimer <= 0f)
                    {
                        _logTimer = 2f;
                        MelonLogger.Msg($"Disarm: stripped {hit} player(s).");
                    }
                }
                catch (Exception e) { MelonLogger.Warning("Disarm Aura: " + e.Message); }
            }
        }

        // ---------------- Kill Aura: меню-настройки (LabFusion) ----------------
        //
        // Подстраница "Kill Aura": режим (все/один), скорость (ползунок), игнор дистанции,
        // мастер-выключатель и список игроков с пометкой [HOST]. Список обновляется при
        // открытии страницы (как Teleport). Клик по игроку → одиночная цель + включить.
        private static class KillAuraMenu
        {
            private static Page _page;
            private static bool _hooked;

            private static bool _rebuildQueued;

            public static void Install(Page root)
            {
                // maxElements=0 — pagination/index pages crash Quest GUIPool.
                _page = root.CreatePage("Kill Aura", new Color(0.7f, 0.4f, 1f), 0, true);
                if (!_hooked)
                {
                    Menu.OnPageOpened += (Action<Page>)OnPageOpened;
                    _hooked = true;
                }
                Rebuild();
            }

            private static void OnPageOpened(Page opened)
            {
                if (opened != _page) return;
                // Defer RemoveAll — sync rebuild inside OnPageOpened → GUIPool NRE.
                if (_rebuildQueued) return;
                _rebuildQueued = true;
                MelonCoroutines.Start(DeferredRebuild());
            }

            private static System.Collections.IEnumerator DeferredRebuild()
            {
                yield return null;
                yield return null;
                _rebuildQueued = false;
                Rebuild();
            }

            private static void Rebuild()
            {
                if (_page == null) return;
                try
                {
                    _page.RemoveAll();

                    _page.CreateBool("ENABLE (start killing)", new Color(1f, 0.15f, 0.15f), KillAura,
                        v => { KillAura = v; Log("Kill Aura", v); });
                    _page.CreateBool("Mode: ALL players", new Color(1f, 0.6f, 0.2f), AuraGlobal,
                        v => { AuraGlobal = v; MelonLogger.Msg("Kill Aura mode: " + (v ? "ALL" : "single target")); });
                    _page.CreateFloat("Speed (hits/sec)", new Color(0.9f, 0.8f, 0.2f), AuraRate, 1f, 1f, 60f,
                        v => { AuraRate = v; });
                    _page.CreateBool("Ignore distance", new Color(0.4f, 0.7f, 1f), AuraIgnoreDist,
                        v => { AuraIgnoreDist = v; MelonLogger.Msg("Kill Aura distance: " + (v ? "ignored (any range)" : "limited")); });
                    _page.CreateBool("Lightning FX", new Color(0.6f, 0.85f, 1f), AuraLightning,
                        v => { AuraLightning = v; MelonLogger.Msg("Kill Aura lightning: " + (v ? "ON" : "OFF")); });
                    _page.CreateFunction("Refresh player list", new Color(0.6f, 0.6f, 0.6f), (Action)Rebuild);

                    int count = 0;
                    foreach (var np in NetworkPlayer.Players)
                    {
                        if (np == null || np.PlayerID == null || np.PlayerID.IsMe || !np.HasRig) continue;
                        byte sid = np.PlayerID.SmallID;
                        bool host = false; try { host = np.PlayerID.IsHost; } catch { }
                        bool sel = (!AuraGlobal && AuraTargetSid == sid);
                        string label = (sel ? "> " : "") + SafeName(np.Username, sid) + (host ? " [HOST]" : "");
                        Color col = sel ? new Color(0.3f, 1f, 0.4f) : (host ? new Color(1f, 0.85f, 0.2f) : Color.white);
                        _page.CreateFunction(label, col, (Action)(() => SelectTarget(sid)));
                        count++;
                    }
                    if (count == 0)
                        _page.CreateFunction("No other players", new Color(0.6f, 0.6f, 0.6f), (Action)(() => { }));
                }
                catch (Exception e) { MelonLogger.Warning("Kill Aura menu: " + e.Message); }
            }

            /// <summary>Выбрать одиночную цель, выключить режим ALL и включить ауру.</summary>
            private static void SelectTarget(byte sid)
            {
                AuraTargetSid = sid;
                AuraGlobal = false;
                KillAura = true;
                MelonLogger.Msg($"Kill Aura: single target sid {sid}, ENABLED.");
                Rebuild();   // обновить пометку выбранной цели
            }

            /// <summary>Имя для BoneMenu: без rich-text тегов и не-ASCII (шрифт меню — латиница).</summary>
            private static string SafeName(string username, byte sid)
            {
                if (string.IsNullOrEmpty(username)) return "Player " + sid;
                var sb = new System.Text.StringBuilder(username.Length);
                bool inTag = false;
                foreach (char c in username)
                {
                    if (c == '<') { inTag = true; continue; }
                    if (c == '>') { inTag = false; continue; }
                    if (inTag) continue;
                    if (c >= 32 && c < 127) sb.Append(c);
                }
                string s = sb.ToString().Trim();
                return s.Length == 0 ? ("Player " + sid) : s;
            }
        }

        // ---------------- Fusion Cleanup (Admin → Cleanup → Despawn All) ----------------
        //
        // Vanilla LabFusion only runs PooleeUtilities.DespawnAll when NetworkInfo.IsHost.
        // We patch that method so the SAME Fusion menu button works for non-hosts too:
        // despawn every networked NetworkProp+Poolee (skip circuit fixtures / players).
        // Non-host path uses NetworkAssetSpawner.Despawn (DespawnRequest → server → all clients).
        private static class FusionCleanup
        {
            private static bool _patched;

            public static void Install(HarmonyLib.Harmony harmony)
            {
                if (_patched) return;
                try
                {
                    var target = AccessTools.Method(typeof(PooleeUtilities), nameof(PooleeUtilities.DespawnAll));
                    if (target == null)
                    {
                        MelonLogger.Warning("Fusion Cleanup: PooleeUtilities.DespawnAll not found.");
                        return;
                    }
                    harmony.Patch(target,
                        prefix: new HarmonyMethod(typeof(FusionCleanup), nameof(DespawnAllPrefix)));
                    _patched = true;
                    MelonLogger.Msg("Fusion Cleanup: Despawn All unlocked (works without host).");
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("Fusion Cleanup patch: " + e.Message);
                }
            }

            /// <summary>Harmony prefix — replace host-only DespawnAll.</summary>
            private static bool DespawnAllPrefix()
            {
                Run();
                return false; // skip original IsHost gate
            }

            /// <summary>Same entity filter as LabFusion PooleeUtilities.DespawnAll.</summary>
            public static void Run()
            {
                try
                {
                    bool hasServer = false;
                    try { hasServer = LabFusion.Network.NetworkInfo.HasServer; } catch { }
                    if (!hasServer)
                    {
                        MelonLogger.Msg("Fusion Cleanup: not in a Fusion lobby.");
                        return;
                    }

                    var lookup = NetworkEntityManager.IDManager.RegisteredEntities.EntityIDLookup;
                    if (lookup == null)
                    {
                        MelonLogger.Msg("Fusion Cleanup: no registered entities.");
                        return;
                    }

                    var entities = new System.Collections.Generic.List<NetworkEntity>();
                    try
                    {
                        foreach (var key in lookup.Keys)
                            if (key != null) entities.Add(key);
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Warning("Fusion Cleanup: enum failed — " + e.Message);
                        return;
                    }

                    int n = 0;
                    for (int i = 0; i < entities.Count; i++)
                    {
                        var networkEntity = entities[i];
                        try
                        {
                            if (networkEntity.GetExtender<NetworkProp>() == null) continue;
                            if (networkEntity.GetExtender<PooleeExtender>() == null) continue;
                            // Don't despawn fixtures (same as Fusion IsFixture).
                            if (networkEntity.GetExtender<CircuitSocketExtender>() != null) continue;
                            // Extra safety: never touch player entities.
                            try
                            {
                                if (networkEntity.GetExtender<NetworkPlayer>() != null) continue;
                            }
                            catch { }

                            NetworkAssetSpawner.Despawn(new NetworkAssetSpawner.DespawnRequestInfo
                            {
                                EntityID = networkEntity.ID,
                                DespawnEffect = false,
                            });
                            n++;
                        }
                        catch { }
                    }

                    MelonLogger.Msg($"Fusion Cleanup: Despawn All → {n} prop(s).");
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("Fusion Cleanup: " + e.Message);
                }
            }
        }

        // ---------------- Security Guards (сетевой спавн + эскорт) ----------------
        //
        // Спавним 3 сетевых NPC «Security Guard» (видят все), берём владение (ИИ считаем мы),
        // и держим их в эскорте вокруг тебя. Barcode находим сами по названию крейта.
        // Всё с типами LabFusion/Marrow-warehouse — здесь (JIT только при вызове).
        private static class Guards
        {
            private const int GuardCount = 3;
            private const float EscortRadius = 2.2f;
            private const float TickInterval = 0.5f;
            private static float _timer;
            private static bool _active;
            private static string _barcode;
            private static readonly System.Collections.Generic.List<BehaviourBaseNav> _navs =
                new System.Collections.Generic.List<BehaviourBaseNav>();
            private static readonly System.Collections.Generic.List<GameObject> _bodies =
                new System.Collections.Generic.List<GameObject>();

            public static void Spawn()
            {
                // Сетевой спавн уходит на сервер Fusion; без активного лобби колбэк не придёт.
                bool hasServer = false;
                try { hasServer = LabFusion.Network.NetworkInfo.HasServer; } catch { }
                MelonLogger.Msg($"Security Guards: HasServer={hasServer}.");
                if (!hasServer)
                {
                    MelonLogger.Msg("Security Guards: нет активного сервера Fusion — создай/зайди в лобби и повтори.");
                    return;
                }

                string bc = FindBarcode();
                if (bc == null) { MelonLogger.Msg("Security Guards: crate 'Security Guard' не найден в реестре."); return; }
                var me = BoneLib.Player.RigManager;
                if (me == null) { MelonLogger.Msg("Security Guards: нет рига игрока."); return; }
                Vector3 c = RigPos(me);
                for (int i = 0; i < GuardCount; i++)
                {
                    Vector3 p = c + Quaternion.Euler(0f, i * (360f / GuardCount), 0f) * (Vector3.forward * EscortRadius);
                    SpawnOne(bc, p);
                }
                _active = true;
                MelonLogger.Msg($"Security Guards: запрошен спавн x{GuardCount}.");
            }

            public static void Despawn()
            {
                _active = false;
                foreach (var go in _bodies) { if (go != null) { try { UnityEngine.Object.Destroy(go); } catch { } } }
                _bodies.Clear();
                _navs.Clear();
                MelonLogger.Msg("Security Guards: убраны.");
            }

            /// <summary>Эскорт: держим охранников на точках вокруг тебя. Плюс ОТЛОЖЕННЫЙ поиск nav —
            /// в момент спавна AI-компонент часто ещё не собран, поэтому доищем его здесь по кадрам.</summary>
            public static void Tick()
            {
                if (!_active) return;
                _timer -= Time.deltaTime;
                if (_timer > 0f) return;
                _timer = TickInterval;

                // Отложенный поиск AI-компонента на телах, где его ещё не нашли.
                if (_navs.Count < _bodies.Count)
                {
                    foreach (var body in _bodies)
                    {
                        if (body == null) continue;
                        try
                        {
                            var nav = body.GetComponentInChildren<BehaviourBaseNav>(true);
                            if (nav != null && !_navs.Contains(nav))
                            {
                                _navs.Add(nav);
                                MelonLogger.Msg($"Security Guards: nav найден отложенно (navs {_navs.Count}).");
                            }
                        }
                        catch { }
                    }
                }

                if (_navs.Count == 0) return;
                var me = BoneLib.Player.RigManager;
                if (me == null) return;
                Vector3 c = RigPos(me);
                for (int i = 0; i < _navs.Count; i++)
                {
                    var nav = _navs[i];
                    if (nav == null) continue;
                    Vector3 p = c + Quaternion.Euler(0f, i * (360f / GuardCount), 0f) * (Vector3.forward * EscortRadius);
                    try { nav.SetHomePosition(p, true, false); } catch { }
                    try { nav.SetPath(p); } catch { }
                }
            }

            private static string FindBarcode()
            {
                if (!string.IsNullOrEmpty(_barcode)) return _barcode;
                try
                {
                    var wh = Il2CppSLZ.Marrow.Warehouse.AssetWarehouse.Instance;
                    if (wh == null) return null;
                    foreach (var crate in wh.GetCrates())
                    {
                        if (crate == null) continue;
                        string t = crate.Title;
                        if (!string.IsNullOrEmpty(t) &&
                            t.IndexOf("Security Guard", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _barcode = crate.Barcode.ID;
                            MelonLogger.Msg($"Security Guards: crate '{t}' -> {_barcode}");
                            return _barcode;
                        }
                    }
                    MelonLogger.Msg("Security Guards: крейт с названием 'Security Guard' не найден.");
                }
                catch (Exception e) { MelonLogger.Warning("Guards barcode: " + e.Message); }
                return null;
            }

            private static void SpawnOne(string barcode, Vector3 pos)
            {
                try
                {
                    var spawnable = new Spawnable { crateRef = new Il2CppSLZ.Marrow.Warehouse.SpawnableCrateReference(barcode) };
                    var info = new LabFusion.RPC.NetworkAssetSpawner.SpawnRequestInfo
                    {
                        Spawnable = spawnable,
                        Position = pos,
                        Rotation = Quaternion.identity,
                        SpawnEffect = false,
                        SpawnCallback = OnSpawned,
                    };
                    LabFusion.RPC.NetworkAssetSpawner.Spawn(info);
                }
                catch (Exception e) { MelonLogger.Warning("Guards spawn: " + e.Message); }
            }

            private static bool _dumped;
            private static void OnSpawned(LabFusion.RPC.NetworkAssetSpawner.SpawnCallbackInfo info)
            {
                try
                {
                    var go = info.Spawned;
                    if (go != null)
                    {
                        _bodies.Add(go);
                        var nav = go.GetComponentInChildren<BehaviourBaseNav>(true);
                        if (nav != null) _navs.Add(nav);
                        else DumpComponents(go);   // диагностика: какой AI реально на теле
                    }
                    // Владение: спавнер и так владелец заспавненного — отдельный TakeOwnership не нужен.
                    MelonLogger.Msg($"Security Guards: заспавнен (bodies {_bodies.Count}, navs {_navs.Count}).");
                }
                catch (Exception e) { MelonLogger.Warning("Guards onSpawned: " + e.Message); }
            }

            /// <summary>Один раз выводим типы компонентов заспавненного тела — чтобы узнать реальный AI-класс.</summary>
            private static void DumpComponents(GameObject go)
            {
                if (_dumped) return;
                _dumped = true;
                try
                {
                    var comps = go.GetComponentsInChildren<MonoBehaviour>(true);
                    var seen = new System.Collections.Generic.HashSet<string>();
                    var sb = new System.Text.StringBuilder();
                    foreach (var c in comps)
                    {
                        if (c == null) continue;
                        string n;
                        try { n = c.GetIl2CppType().Name; } catch { n = "?"; }
                        if (seen.Add(n)) { sb.Append(n); sb.Append(", "); }
                    }
                    MelonLogger.Msg("Security Guards: components on body -> " + sb.ToString());
                }
                catch (Exception e) { MelonLogger.Warning("Guards dump: " + e.Message); }
            }
        }

        /// <summary>Принадлежит ли трансформ собственному ригу игрока.</summary>
        private static bool IsOwnRig(Transform t)
        {
            var rig = BoneLib.Player.RigManager;
            if (rig == null || t == null) return false;
            return t.IsChildOf(rig.transform);
        }

        // ---------------- Меню ----------------

        private void BuildMenu()
        {
            // Большой maxElements — чтобы BoneMenu НЕ разбивал страницу на под-страницы со стрелками.
            // Пагинация (индекс-страницы) в этой версии BoneMenu крашит GUIPool при листании.
            Page page = Page.Root.CreatePage("MONSTER Panel", Color.red, 64, true);
            page.CreateBool("Invincible", Color.green, Invincible, v => { Invincible = v; Log("Invincible", v); });
            page.CreateBool("Monster Damage", new Color(1f, 0.4f, 0f), MonsterDamage,
                v => { MonsterDamage = v; Log("Monster Damage", v); });
            page.CreateBool("Infinite Ammo", new Color(1f, 0.85f, 0.1f), InfiniteAmmo,
                v => { InfiniteAmmo = v; Log("Infinite Ammo", v); });
            page.CreateBool("Tank Mode", new Color(0.4f, 0.6f, 0.9f), TankMode,
                v => { TankMode = v; Log("Tank Mode", v); });
            page.CreateBool("Disarm", new Color(0.9f, 0.2f, 0.5f), Disarm,
                v => { Disarm = v; Log("Disarm", v); });

            // Teleport + ник + телохранители + Spoofing PID: только когда загружен LabFusion.
            if (Teleporter.FusionLoaded)
            {
                PidSpoof.InstallMenu(page);
                AdminNick.Install(page); // OWNER/dev-gold staff nametag (AnimatedName-style)
                page.CreateFunction("Fusion Cleanup (Despawn All)", new Color(1f, 0.55f, 0.15f),
                    (Action)FusionCleanup.Run);
                page.CreateFunction("Spawn 3 Bodyguards", new Color(0.2f, 0.55f, 1f), (Action)Guards.Spawn);
                page.CreateFunction("Despawn Bodyguards", new Color(0.5f, 0.5f, 0.5f), (Action)Guards.Despawn);
                page.CreateFunction("Avatar preview 6114112", new Color(0.7f, 0.5f, 1f), (Action)NickHider.SetAvatarPreview);
                KillAuraMenu.Install(page);
                NickHider.Install(page);
                Teleporter.Install(page);
                Tracking.InstallMenu(page);
                MelonLogger.Msg("MONSTER Panel: Kill Aura + Teleport + Tracking + Nickname + Bodyguards + Spoofing PID + Cleanup added (LabFusion found).");
            }
            else
                MelonLogger.Msg("MONSTER Panel: LabFusion not loaded - Teleport/Nickname/Bodyguards/Spoofing PID/Tracking hidden.");
        }

        private static void Log(string name, bool on) =>
            MelonLogger.Msg(on ? $"{name}: ON" : $"{name}: OFF");

        // ---------------- Патчи урона ----------------

        private void ApplyPatches()
        {
            Type playerHealth = AccessTools.TypeByName(PlayerHealthType);
            if (playerHealth != null)
            {
                var god = Hm(nameof(GodPrefix));
                foreach (string m in new[] { "TAKEDAMAGE", "ApplyKillDamage", "Death" })
                    TryPatch(playerHealth, m, god);
            }
            else MelonLogger.Error("MONSTER Panel: Player_Health not found - invincibility inactive");

            TryPatchTyped(typeof(SubBehaviourHealth), "TakeDamage", Hm(nameof(PuppetPrefix)), "SubBehaviourHealth.TakeDamage");
            TryPatchTyped(typeof(MHealth), "TAKEDAMAGE", Hm(nameof(HealthPrefix)), "Health.TAKEDAMAGE");

            Type fusion = AccessTools.TypeByName(FusionReceiverPatch);
            if (fusion != null)
                TryPatch(fusion, "ReceiveAttack", Hm(nameof(FusionAttackPrefix)));
            else
                MelonLogger.Msg("MONSTER Panel: LabFusion not found - network player damage disabled (normal without Fusion).");

            ApplyAmmoPatches();
        }

        /// <summary>Бесконечный запас патронов: GetCartridgeCount всегда возвращает большой запас,
        /// поэтому перезарядка всегда есть. Сам магазин расходуется штатно.</summary>
        private void ApplyAmmoPatches()
        {
            try
            {
                var post = Hm(nameof(AmmoCountPostfix));
                int patched = 0;
                foreach (var mi in AccessTools.GetDeclaredMethods(typeof(AmmoInventory)))
                {
                    if (mi.Name != "GetCartridgeCount" || mi.ReturnType != typeof(int)) continue;
                    HarmonyInstance.Patch(mi, postfix: post);
                    patched++;
                }
                MelonLogger.Msg($"MONSTER Panel: Infinite Ammo - patched AmmoInventory.GetCartridgeCount ({patched} overloads).");
            }
            catch (Exception e) { MelonLogger.Warning("MONSTER Panel: Infinite Ammo - " + e.Message); }
        }

        private static HarmonyMethod Hm(string name) =>
            new HarmonyMethod(typeof(MonsterPanelMod).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic));

        private void TryPatch(Type type, string methodName, HarmonyMethod prefix)
        {
            try
            {
                MethodBase target = AccessTools.Method(type, methodName);
                if (target == null) { MelonLogger.Warning($"MONSTER Panel: {type.Name}.{methodName} not found"); return; }
                HarmonyInstance.Patch(target, prefix: prefix);
                MelonLogger.Msg($"MONSTER Panel: patched {type.Name}.{methodName}");
            }
            catch (Exception e) { MelonLogger.Warning($"MONSTER Panel: {type.Name}.{methodName} - {e.Message}"); }
        }

        private void TryPatchTyped(Type type, string methodName, HarmonyMethod prefix, string label)
        {
            try
            {
                MethodBase target = AccessTools.Method(type, methodName);
                if (target == null) { MelonLogger.Warning($"MONSTER Panel: {label} not found"); return; }
                HarmonyInstance.Patch(target, prefix: prefix);
                MelonLogger.Msg($"MONSTER Panel: patched {label}");
            }
            catch (Exception e) { MelonLogger.Warning($"MONSTER Panel: {label} - {e.Message}"); }
        }

        private static bool GodPrefix() => !Invincible;

        private static void PuppetPrefix(SubBehaviourHealth __instance, Attack attack)
        {
            if (!MonsterDamage || _reentry || __instance == null) return;
            try { _reentry = true; __instance.Kill(); Launch(attack); }
            catch (Exception e) { MelonLogger.Warning("MONSTER Panel: puppet - " + e.Message); }
            finally { _reentry = false; }
        }

        private static void HealthPrefix(MHealth __instance, ref float damage)
        {
            if (!MonsterDamage || __instance == null) return;
            // Объектам — сразу максимальный урон (проходит их штатным путём с эффектами разрушения).
            damage = MaxDamage;
        }

        private static void FusionAttackPrefix(ref Attack attack)
        {
            // Буст при Monster Damage, а также всегда во время отправки Remote Kill
            // (чтобы Remote Kill работал даже с выключенным Monster Damage).
            if (MonsterDamage || _remoteKillSending) attack.damage = MaxDamage;
        }

        /// <summary>Запас патронов в инвентаре — бесконечный (перезарядка всегда доступна).</summary>
        private static void AmmoCountPostfix(ref int __result)
        {
            if (InfiniteAmmo && __result < 999) __result = 999;
        }

        private static void Launch(Attack attack)
        {
            var col = attack.collider;
            if (col == null) return;
            Vector3 dir = attack.direction;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.up;
            Vector3 v = dir.normalized * LaunchSpeed;
            var root = col.transform.root;
            if (root == null) return;
            foreach (var rb in root.GetComponentsInChildren<Rigidbody>())
                if (rb != null) { try { rb.velocity = v; } catch { } }
        }

        // ---------------- Сетевая молния (видят ВСЕ) ----------------
        //
        // Спавним настоящий сетевой электро-объект РОВНО в грудь цели через NetworkAssetSpawner,
        // поэтому эффект видят все (у других мода нет — но Fusion сам синхронит заспавненное).
        // Позиция — точно по игроку (грудь), а не с offset над головой (раньше падало «рядом»).
        // Чтобы не лагало и не засоряло лобби: один удар за залп (round-robin), авто-удаление
        // по таймеру, кап одновременных.
        private static class NetLightning
        {
            private const float NetInterval = 0.45f;  // как часто сетевой удар (сек)
            private const float Life = 0.7f;          // сколько объект живёт до авто-удаления
            private const int MaxConcurrent = 6;      // кап одновременных

            private static readonly string[] _keywords =
            { "lightning", "electric", "tesla", "shock", "zeus", "spark", "arc", "bolt", "zap" };

            private static string _barcode;
            private static bool _searched;
            private static float _timer;
            private static int _rr;

            private struct Pending { public ushort id; public float despawnAt; }
            private static readonly System.Collections.Generic.List<Pending> _pending =
                new System.Collections.Generic.List<Pending>();

            /// <summary>Готов ли сетевой удар (троттл + сервер + найден крейт + не превышен кап).</summary>
            public static bool Ready(float dt)
            {
                if (!AuraLightning) return false;
                bool srv = false; try { srv = LabFusion.Network.NetworkInfo.HasServer; } catch { }
                if (!srv) return false;
                if (Barcode() == null) return false;
                if (_pending.Count >= MaxConcurrent) return false;
                _timer -= dt;
                if (_timer > 0f) return false;
                _timer = NetInterval;
                return true;
            }

            private static string Barcode()
            {
                if (_searched) return _barcode;
                try
                {
                    var wh = Il2CppSLZ.Marrow.Warehouse.AssetWarehouse.Instance;
                    if (wh == null) return null;   // реестр ещё не готов — попробуем позже
                    _searched = true;
                    foreach (var crate in wh.GetCrates())
                    {
                        if (crate == null) continue;
                        string t = crate.Title;
                        if (string.IsNullOrEmpty(t)) continue;
                        string tl = t.ToLowerInvariant();
                        foreach (var kw in _keywords)
                            if (tl.Contains(kw))
                            {
                                _barcode = crate.Barcode.ID;
                                MelonLogger.Msg($"Net Lightning: crate '{t}' -> {_barcode}");
                                return _barcode;
                            }
                    }
                    MelonLogger.Msg("Net Lightning: электро-крейт (lightning/electric/...) не найден в реестре.");
                }
                catch (Exception e) { MelonLogger.Warning("Net Lightning barcode: " + e.Message); }
                return _barcode;
            }

            /// <summary>Один сетевой удар за залп: спавним РОВНО в грудь цели (round-robin).</summary>
            public static void StrikeOne(System.Collections.Generic.List<Vector3> targets)
            {
                if (targets == null || targets.Count == 0 || _barcode == null) return;
                try
                {
                    _rr = (_rr + 1) % targets.Count;
                    Vector3 pos = targets[_rr];   // ровно грудь игрока (ChestPos), без offset над головой
                    var spawnable = new Spawnable { crateRef = new Il2CppSLZ.Marrow.Warehouse.SpawnableCrateReference(_barcode) };
                    var info = new LabFusion.RPC.NetworkAssetSpawner.SpawnRequestInfo
                    {
                        Spawnable = spawnable,
                        Position = pos,
                        Rotation = Quaternion.identity,
                        SpawnEffect = false,
                        SpawnCallback = OnSpawned,
                    };
                    LabFusion.RPC.NetworkAssetSpawner.Spawn(info);
                }
                catch (Exception e) { MelonLogger.Warning("Net Lightning spawn: " + e.Message); }
            }

            private static void OnSpawned(LabFusion.RPC.NetworkAssetSpawner.SpawnCallbackInfo info)
            {
                try
                {
                    var ent = info.Entity;
                    if (ent == null) return;
                    _pending.Add(new Pending { id = ent.ID, despawnAt = Time.time + Life });
                }
                catch (Exception e) { MelonLogger.Warning("Net Lightning onSpawn: " + e.Message); }
            }

            /// <summary>Убираем отжившие сетевые объекты по таймеру (авто-очистка у всех).</summary>
            public static void Tick()
            {
                if (_pending.Count == 0) return;
                float now = Time.time;
                for (int i = _pending.Count - 1; i >= 0; i--)
                {
                    if (now < _pending[i].despawnAt) continue;
                    ushort id = _pending[i].id;
                    _pending.RemoveAt(i);
                    try
                    {
                        LabFusion.RPC.NetworkAssetSpawner.Despawn(new LabFusion.RPC.NetworkAssetSpawner.DespawnRequestInfo
                        { EntityID = id, DespawnEffect = true });
                    }
                    catch { }
                }
            }
        }

        // ---------------- Tank Mode ----------------
        //
        // Чисто анти-захват, без изменения массы: отключаем AvatarGrip на своём риге — другие
        // физически не могут за тебя взяться и сдвинуть (позицией своего тела владеет твой клиент).
        // Массу/руки НЕ трогаем вообще, поэтому рост, движение и удары — как обычно.
        private static void TankUpdate()
        {
            if (TankMode)
            {
                _tankTimer -= Time.deltaTime;
                if (!_tankApplied || _tankTimer <= 0f)
                {
                    TankSetGrips(false);
                    _tankTimer = TankReapplyInterval;   // переприменяем: грипы могли пересоздаться
                    _tankApplied = true;
                }
            }
            else if (_tankApplied)
            {
                TankSetGrips(true);
                _tankApplied = false;
                MelonLogger.Msg("Tank Mode: off, grips restored.");
            }
        }

        private static void TankSetGrips(bool enabled)
        {
            var rig = BoneLib.Player.RigManager;
            if (rig == null) return;
            try
            {
                foreach (var g in rig.GetComponentsInChildren<AvatarGrip>())
                    if (g != null && g.enabled != enabled) g.enabled = enabled;
            }
            catch (Exception e) { MelonLogger.Warning("Tank grips: " + e.Message); }
        }

        // ---------------- Anti-Constrainer (всегда, без UI) ----------------
        //
        // Constrainer скрепляет предметы, вешая на них компонент ConstraintTracker (+ Unity joint).
        // Кто-то приконстрейнил твоё оружие → ты не мог им двигать. Мы периодически находим
        // ConstraintTracker'ы на тебе и на предметах рядом и зовём DeleteConstraint() — штатное
        // снятие скрепа (LabFusion патчит этот путь, снятие синхронится).
        // ВАЖНО: снимаем ТОЛЬКО ЧУЖИЕ скрепы. Владельца берём из NetworkConstraint.Cache →
        // NetworkEntity.OwnerID: если OwnerID.IsMe (или владельца не определить) — НЕ трогаем,
        // чтобы твои собственные скрепы держались. Бьём точечно по ConstraintTracker, а не
        // сносим все Unity-джойнты, чтобы не ломать двери/петли уровня.
        private static class Freedom
        {
            private const float Interval = 0.35f;   // как часто чистим
            private const float Radius = 3.5f;      // предметы «в досягаемости» вокруг тебя
            private static float _timer;
            private static readonly Collider[] _buf = new Collider[128];
            private static readonly System.Collections.Generic.HashSet<int> _seen =
                new System.Collections.Generic.HashSet<int>();

            public static void Tick()
            {
                _timer -= Time.deltaTime;
                if (_timer > 0f) return;
                _timer = Interval;

                var rig = BoneLib.Player.RigManager;
                if (rig == null) return;
                _seen.Clear();

                // 1) констрейны прямо на твоём риге (если приконстрейнили тело/руки).
                try
                {
                    foreach (var ct in rig.GetComponentsInChildren<Il2CppSLZ.Marrow.ConstraintTracker>(true))
                        FreeOne(ct);
                }
                catch { }

                // 2) констрейны на предметах вокруг (держишь в руках / рядом, чтобы взять).
                try
                {
                    Vector3 c = RigPos(rig);
                    int n = Physics.OverlapSphereNonAlloc(c, Radius, _buf,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < n; i++)
                    {
                        var col = _buf[i];
                        if (col == null) continue;
                        var ct = col.GetComponentInParent<Il2CppSLZ.Marrow.ConstraintTracker>();
                        FreeOne(ct);
                    }
                }
                catch { }
            }

            private static void FreeOne(Il2CppSLZ.Marrow.ConstraintTracker ct)
            {
                if (ct == null) return;
                if (!_seen.Add(ct.GetInstanceID())) return;   // в этом тике уже обработали
                try
                {
                    // Владелец скрепа: только ЧУЖИЕ снимаем. Свои и «не определить» — не трогаем.
                    var ne = LabFusion.Marrow.Extenders.NetworkConstraint.Cache.Get(ct);
                    if (ne == null) return;                              // не сетевой/не в кэше → считаем своим
                    var owner = ne.OwnerID;
                    if (owner == null || owner.IsMe) return;             // мой скреп — оставляем
                }
                catch { return; }                                       // не смогли определить владельца → не трогаем
                try { ct.DeleteConstraint(); }                          // чужой — снимаем
                catch { }
            }
        }

        // ---------------- Свой ник: скрытие и цветные DEV-пресеты (LabFusion) ----------------
        //
        // Ник синхронизируется по сети как метаданные, поэтому меняет то, что видят другие
        // над твоим персонажем. Nametag рисуется через TextMeshPro — он понимает rich-text
        // теги (<color=…>, <b>), а лимит имени = 32 символа (теги «съедают» его).
        // Изолировано в отдельном классе (JIT-ится только при загруженном LabFusion).
        private static class NickHider
        {
            public static void Install(Page root)
            {
                Page p = root.CreatePage("Nickname", new Color(0.5f, 0.8f, 1f), 64, true);   // без пагинации/стрелок
                // Пресеты MONSTER в цвете (каждый ≤32 символов вместе с тегами).
                p.CreateFunction("MONSTER (red)",   new Color(1f, 0.2f, 0.2f), (Action)(() => SetNick("<color=#ff2020>MONSTER</color>")));
                p.CreateFunction("MONSTER (green)", new Color(0.2f, 1f, 0.3f),  (Action)(() => SetNick("<color=#20ff40>MONSTER</color>")));
                p.CreateFunction("MONSTER (gold)",  new Color(1f, 0.82f, 0.12f),(Action)(() => SetNick("<color=#ffd21e>MONSTER</color>")));
                p.CreateFunction("MONSTER (cyan)",  new Color(0.2f, 0.88f, 1f), (Action)(() => SetNick("<color=#20e0ff>MONSTER</color>")));
                p.CreateFunction("MONSTER (pink)",  new Color(1f, 0.4f, 0.8f),  (Action)(() => SetNick("<color=#ff40c0>MONSTER</color>")));
                p.CreateFunction("DEV (gold)",      new Color(1f, 0.82f, 0.12f),(Action)(() => SetNick("<color=#ffd21e>DEV</color>")));
                p.CreateFunction("Hide (empty)",    new Color(0.6f, 0.6f, 0.6f),(Action)(() => SetNick(" ")));
                p.CreateFunction("Reset to default",new Color(0.8f, 0.8f, 0.8f),(Action)ResetNick);
            }

            /// <summary>Кнопка превью аватара — живёт в самой панели, не в подстранице ника.</summary>
            public static void SetAvatarPreview() => SetAvatarModId(6114112);

            private static readonly System.Random _rng = new System.Random();
            private static readonly string[] _handles =
            {
                "Shadow", "Ghost", "Reaper", "Nova", "Viper", "Frost", "Blaze", "Rogue",
                "Cipher", "Storm", "Raven", "Onyx", "Zero", "Havoc", "Wraith", "Echo",
                "Talon", "Ember", "Kilo", "Delta", "Fox", "Ryder", "Ace", "Neo",
            };

            /// <summary>Случайный правдоподобный ник (handle + цифры) для строки Username в списке.</summary>
            private static string RandomUsername()
            {
                string h = _handles[_rng.Next(_handles.Length)];
                return h + _rng.Next(10, 9999);
            }

            /// <summary>Подменяем превью аватара в списке игроков (mod.io id → иконка/подпись).</summary>
            private static void SetAvatarModId(int id)
            {
                try
                {
                    var md = LabFusion.Player.LocalPlayer.Metadata;
                    if (md == null || md.AvatarModID == null)
                    {
                        MelonLogger.Msg("Avatar: metadata unavailable - join a Fusion lobby and retry.");
                        return;
                    }
                    md.AvatarModID.SetValue(id);
                    Notify("Avatar preview set", "mod " + id);
                    MelonLogger.Msg($"Avatar: preview mod id set {id} (synced).");
                }
                catch (Exception e) { MelonLogger.Warning("Avatar set: " + e.Message); }
            }

            private static void SetNick(string value)
            {
                try
                {
                    if (value != null && value.Length > 32)   // страховка под лимит имени LabFusion
                        value = value.Substring(0, 32);
                    // 1) Ник (nametag над головой) — через настройки LabFusion (правильный синхро-путь).
                    LabFusion.Preferences.Client.ClientSettings.Nickname.Value = value;
                    LabFusion.Preferences.Client.ClientSettings.NicknameVisibility.Value = LabFusion.Senders.NicknameVisibility.SHOW;
                    SendSettings();

                    // 2) Username в списке игроков — СЛУЧАЙНЫЙ левый ник (не связан с тегом над головой):
                    //    в ростере будто отдельный игрок, а над головой — выбранный ник. AvatarTitle тоже
                    //    делаем случайным, чтобы подпись аватара не выдавала настоящий ник.
                    string fake = RandomUsername();
                    var md = LabFusion.Player.LocalPlayer.Metadata;
                    if (md != null)
                    {
                        try { md.Username?.SetValue(fake); } catch { }
                        try { md.AvatarTitle?.SetValue(fake); } catch { }
                    }

                    string shown = StripTags(value);
                    Notify("Identity changed", $"tag: {(string.IsNullOrWhiteSpace(shown) ? "(empty)" : shown)} | list: {fake}");
                    MelonLogger.Msg($"Identity: nametag '{value}', list username '{fake}' (random, synced).");
                }
                catch (Exception e) { MelonLogger.Warning("Identity set: " + e.Message); }
            }

            private static void ResetNick()
            {
                try
                {
                    LabFusion.Preferences.Client.ClientSettings.Nickname.Value = "";   // пусто → откат на платформенный ник
                    SendSettings();

                    // Возвращаем Username к реальному платформенному, имя аватара убираем (LabFusion
                    // переброадкастит его при следующей смене аватара).
                    var md = LabFusion.Player.LocalPlayer.Metadata;
                    if (md != null)
                    {
                        try { md.Username?.SetValue(LabFusion.Player.LocalPlayer.Username); } catch { }
                        try { md.AvatarTitle?.Remove(); } catch { }
                    }

                    Notify("Identity reset", "default");
                    MelonLogger.Msg("Identity: reset to default.");
                }
                catch (Exception e) { MelonLogger.Warning("Identity reset: " + e.Message); }
            }

            /// <summary>Проталкиваем настройки клиента по сети (метод internal — зовём рефлексией).</summary>
            private static void SendSettings()
            {
                try { AccessTools.Method("LabFusion.Preferences.FusionPreferences:SendClientSettings")?.Invoke(null, null); }
                catch (Exception e) { MelonLogger.Warning("Nickname send: " + e.Message); }
            }

            /// <summary>Всплывающая плашка LabFusion — сразу видно, что ник сменился.</summary>
            private static void Notify(string title, string message)
            {
                try
                {
                    var n = new LabFusion.UI.Popups.Notification();
                    n.Title = title;                 // string → NotificationText (неявное преобразование)
                    n.Message = message;
                    n.Type = LabFusion.UI.Popups.NotificationType.SUCCESS;
                    n.ShowPopup = true;
                    n.PopupLength = 3f;
                    LabFusion.UI.Popups.Notifier.Send(n);
                }
                catch (Exception e) { MelonLogger.Warning("Nickname notify: " + e.Message); }
            }

            /// <summary>Срезаем rich-text теги для читаемого текста плашки.</summary>
            private static string StripTags(string s)
            {
                if (string.IsNullOrEmpty(s)) return s;
                var sb = new System.Text.StringBuilder(s.Length);
                bool inTag = false;
                foreach (char c in s)
                {
                    if (c == '<') { inTag = true; continue; }
                    if (c == '>') { inTag = false; continue; }
                    if (!inTag) sb.Append(c);
                }
                return sb.ToString().Trim();
            }
        }

        // ---------------- Teleport (LabFusion) ----------------
        //
        // Весь код, трогающий типы LabFusion, изолирован здесь: методы JIT-ятся только
        // когда класс реально вызван (а вызываем его лишь при загруженном LabFusion),
        // поэтому без Fusion мод не падает с TypeLoadException.
        private static class Teleporter
        {
            private static Page _page;
            private static bool _hooked;

            /// <summary>LabFusion загружен? (тип резолвится только если сборка в игре есть.)</summary>
            public static bool FusionLoaded => AccessTools.TypeByName("LabFusion.Entities.NetworkPlayer") != null;

            private static bool _rebuildQueued;

            /// <summary>Создаёт подстраницу Teleport в корне панели и вешает авто-обновление списка.</summary>
            public static void Install(Page root)
            {
                // Flat list only — CreatePage-per-player + pagination crashed Quest GUIPool.
                _page = root.CreatePage("Teleport", new Color(0.3f, 0.7f, 1f), 0, true);
                if (!_hooked)
                {
                    Menu.OnPageOpened += (Action<Page>)OnPageOpened;   // при каждом открытии — свежий список
                    _hooked = true;
                }
                Rebuild();
            }

            private static void OnPageOpened(Page opened)
            {
                if (opened != _page) return;
                if (_rebuildQueued) return;
                _rebuildQueued = true;
                MelonCoroutines.Start(DeferredRebuild());
            }

            private static System.Collections.IEnumerator DeferredRebuild()
            {
                yield return null;
                yield return null;
                _rebuildQueued = false;
                Rebuild();
            }

            /// <summary>Flat list: TP / Bring / Tracking per player — no nested pages.</summary>
            private static void Rebuild()
            {
                if (_page == null) return;
                try
                {
                    _page.RemoveAll();
                    _page.CreateFunction("Refresh", new Color(0.7f, 0.7f, 0.7f), (Action)(() =>
                    {
                        if (_rebuildQueued) return;
                        _rebuildQueued = true;
                        MelonCoroutines.Start(DeferredRebuild());
                    }));

                    int count = 0;
                    foreach (var np in NetworkPlayer.Players)
                    {
                        if (np == null || np.PlayerID == null || np.PlayerID.IsMe) continue;
                        if (!np.HasRig) continue;

                        byte sid = np.PlayerID.SmallID;
                        string name = SafeName(np.Username, sid);   // без rich-text тегов и не-ASCII: шрифт BoneMenu только латиница
                        string shortName = name.Length > 14 ? name.Substring(0, 14) : name;

                        _page.CreateFunction("TP  " + shortName, new Color(0.3f, 1f, 0.5f), (Action)(() => TeleportSelfTo(sid)));
                        _page.CreateFunction("Bring  " + shortName, new Color(1f, 0.6f, 0.2f), (Action)(() => BringToMe(sid)));

                        string trackPid = null;
                        string trackName = np.Username;
                        try { trackPid = np.PlayerID.PlatformID; } catch { /* */ }
                        if (!string.IsNullOrWhiteSpace(trackPid))
                        {
                            string pidCopy = trackPid.Trim();
                            string nameCopy = trackName;
                            bool tracked = Tracking.IsTracked(pidCopy);
                            _page.CreateFunction(
                                (tracked ? "Untrack  " : "Track  ") + shortName,
                                tracked ? new Color(1f, 0.4f, 0.35f) : new Color(0.35f, 0.9f, 1f),
                                (Action)(() =>
                                {
                                    if (Tracking.IsTracked(pidCopy)) Tracking.Remove(pidCopy);
                                    else Tracking.Add(pidCopy, nameCopy);
                                    if (_rebuildQueued) return;
                                    _rebuildQueued = true;
                                    MelonCoroutines.Start(DeferredRebuild());
                                }));
                        }
                        count++;
                    }

                    if (count == 0)
                        _page.CreateFunction("No other players", new Color(0.6f, 0.6f, 0.6f), (Action)(() => { }));
                }
                catch (Exception e) { MelonLogger.Warning("Teleport rebuild: " + e.Message); }
            }

            private static NetworkPlayer Find(byte sid)
            {
                foreach (var np in NetworkPlayer.Players)
                    if (np != null && np.PlayerID != null && np.PlayerID.SmallID == sid)
                        return np;
                return null;
            }

            /// <summary>Имя для BoneMenu: срезаем rich-text теги (&lt;color&gt;…) и не-ASCII (кириллицу),
            /// иначе шрифт меню рисует кашу/квадраты. Пусто → "Player N".</summary>
            private static string SafeName(string username, byte sid)
            {
                if (string.IsNullOrEmpty(username)) return "Player " + sid;
                var sb = new System.Text.StringBuilder(username.Length);
                bool inTag = false;
                foreach (char c in username)
                {
                    if (c == '<') { inTag = true; continue; }
                    if (c == '>') { inTag = false; continue; }
                    if (inTag) continue;
                    if (c >= 32 && c < 127) sb.Append(c);   // только печатная латиница
                }
                string s = sb.ToString().Trim();
                return s.Length == 0 ? ("Player " + sid) : s;
            }

            /// <summary>Телепортируемся к выбранному игроку (свой риг — синхронизируется по сети штатно).</summary>
            private static void TeleportSelfTo(byte sid)
            {
                try
                {
                    var np = Find(sid);
                    if (np == null || !np.HasRig) { MelonLogger.Msg("Teleport: player unavailable (left?)."); return; }
                    RigManager target = np.RigRefs.RigManager;
                    RigManager me = BoneLib.Player.RigManager;
                    if (target == null || me == null) return;

                    Vector3 dest = Grounded(target);
                    // Небольшой отступ, чтобы не оказаться внутри игрока.
                    Vector3 myPos = Grounded(me);
                    Vector3 off = myPos - dest; off.y = 0f;
                    off = off.sqrMagnitude > 0.01f ? off.normalized : -target.transform.forward;
                    me.Teleport(dest + off * 0.8f, true);
                    MelonLogger.Msg($"Teleport: teleported to {SafeName(np.Username, sid)} (sid {sid}).");
                }
                catch (Exception e) { MelonLogger.Warning("Teleport self: " + e.Message); }
            }

            /// <summary>Притягиваем игрока к себе. Best-effort: его позицией владеет его клиент, может не «прилипнуть».</summary>
            private static void BringToMe(byte sid)
            {
                try
                {
                    var np = Find(sid);
                    if (np == null || !np.HasRig) { MelonLogger.Msg("Teleport: player unavailable (left?)."); return; }
                    RigManager target = np.RigRefs.RigManager;
                    RigManager me = BoneLib.Player.RigManager;
                    if (target == null || me == null) return;

                    Vector3 dest = Grounded(me);
                    var head = BoneLib.Player.Head;
                    Vector3 fwd = head != null ? head.forward : me.transform.forward;
                    fwd.y = 0f;
                    if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
                    target.Teleport(dest + fwd.normalized * 1.2f, true);
                    MelonLogger.Msg($"Teleport: pulled {SafeName(np.Username, sid)} (sid {sid}) - if it didn't stick, that's network position ownership.");
                }
                catch (Exception e) { MelonLogger.Warning("Teleport bring: " + e.Message); }
            }

            /// <summary>Позиция ног рига на полу. Луч вниз ИГНОРИРУЕТ тела игроков — иначе телепорт
            /// «косо»: попадали на колено/бедро цели и оказывались в воздухе/внутри неё.</summary>
            private static Vector3 Grounded(RigManager rig)
            {
                Vector3 p;
                try { p = rig.physicsRig != null ? rig.physicsRig.transform.position : rig.transform.position; }
                catch { p = rig.transform.position; }

                var hits = Physics.RaycastAll(p + Vector3.up * 0.4f, Vector3.down, 6f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                float floorY = float.NegativeInfinity; bool found = false;
                foreach (var h in hits)
                {
                    if (h.collider == null) continue;
                    if (h.collider.GetComponentInParent<RigManager>() != null) continue; // пропускаем любые тела игроков
                    if (h.point.y > floorY) { floorY = h.point.y; found = true; }        // ближайший пол под ногами
                }
                if (found) return new Vector3(p.x, floorY, p.z);
                return new Vector3(p.x, p.y - 0.9f, p.z);   // запас: примерно на уровень ног
            }
        }
    }
}
