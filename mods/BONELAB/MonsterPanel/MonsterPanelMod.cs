using System;
using System.Collections;
using System.Reflection;
using BoneLib.BoneMenu;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.AI;
using Il2CppSLZ.Marrow.Combat;
using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.PuppetMasta;
using Il2CppSLZ.Marrow.Warehouse;
using LabFusion.Entities;
using LabFusion.Extensions;
using LabFusion.Marrow.Extenders;
using MelonLoader;
using UnityEngine;
using UnityEngine.AI;
using MHealth = Il2CppSLZ.Marrow.Health;

[assembly: MelonInfo(typeof(MonsterPanel.MonsterPanelMod), "MONSTER Panel", "2.29.10", "you")]
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
                PidSpoof.Init(HarmonyInstance); // Spoofing PID: hook SetPlatformID + restore saved state
            AntiManip.Install(HarmonyInstance); // silent Dev Manipulator immunity (no UI)
            Guards.Install(HarmonyInstance);    // bodyguards: never agro the local player
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
                AdminNick.Tick();      // typewriter nametag (throttled network sync)
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

            public static void Install(Page root)
            {
                _page = root.CreatePage("Kill Aura", new Color(0.7f, 0.4f, 1f), 16, true);
                if (!_hooked)
                {
                    Menu.OnPageOpened += (Action<Page>)OnPageOpened;
                    _hooked = true;
                }
                Rebuild();
            }

            private static void OnPageOpened(Page opened)
            {
                if (opened == _page) Rebuild();
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

        // ---------------- Security Guards (сетевой спавн + полная активация) ----------------
        //
        // NetworkAssetSpawner syncs the prefab for everyone, but Fusion often leaves the
        // PuppetMaster in Disabled (T-pose / no muscle drives / colliders off) and the
        // BehaviourBaseNav asleep. We take ownership (restores muscleSpring/Damper via
        // PuppetMasterExtender), force Alive+Active, wake nav/AI, keep asserting during
        // a wake window, stay friendly to the local player, agro other players, and
        // try to put a networked pistol in their right hand.
        private static class Guards
        {
            private const int GuardCount = 3;
            private const float EscortRadius = 3.2f;
            private const float TickInterval = 0.5f;
            private const float WakeWindow = 2f;
            private const float AgroRange = 40f;
            private const float ArmDelay = 1.5f;       // Tick-driven (MelonCoroutines do NOT resume on Quest)
            private const float SettleTime = 5f;       // no SetPath/agro while they plant feet
            private const float ResnapBelow = 1.25f;
            private const float HeartbeatEvery = 2f;

            // Official SLZ BONELAB AKM (BONELAB barcode lists / BoneLib).
            private const string AkmBarcode = "c1534c5a-a6b5-4177-beb8-04d947756e41";

            private static float _timer;
            private static float _heartbeat;
            private static bool _active;
            private static string _barcode;
            private static string _weaponBarcode;
            private static bool _weaponLookupDone;
            private static bool _dumped;
            private static bool _patchesInstalled;
            private static int _ownerTeam = 0;

            // Weapon spawn callbacks must be method-groups on Quest — lambdas often never fire.
            private static readonly System.Collections.Generic.Queue<int> _pendingArmBodyIds =
                new System.Collections.Generic.Queue<int>();

            private struct Guard
            {
                public GameObject body;
                public ushort entityId;
                public BehaviourBaseNav nav;
                public PuppetMaster puppet;
                public NavMeshAgent agent;
                public float wakeUntil;
                public bool ready;
                public GameObject weapon;
                public ushort weaponEntityId;
                public bool armed;
                public bool armRequested;
                public float spawnedAt;
                public bool blockedCols;
                public bool ignoredPlayerCols;
                public bool allegianceOnce;
                public bool settleDone;
                public bool activatedOnce;
            }

            private static readonly System.Collections.Generic.List<Guard> _guards =
                new System.Collections.Generic.List<Guard>();

            private static bool BodyAlive(GameObject go)
            {
                // Il2Cpp Unity fake-null safe check.
                try { return go != null && go; } catch { return go != null; }
            }

            public static void Install(HarmonyLib.Harmony harmony)
            {
                if (_patchesInstalled) return;
                // Quest/LemonLoader frequently fails IL compile when patching Il2Cpp AI methods
                // ("Guards patches: IL Compile Error"). Friendly behaviour is enforced in Tick
                // via ProtectOwner / DirectHostility instead — no Harmony required.
                _patchesInstalled = true;
                MelonLogger.Msg("Security Guards: friendly mode via tick (no Harmony agro patches).");
            }

            private static bool IsLocalPlayerProxy(TriggerRefProxy trp)
            {
                if (trp == null) return false;
                try
                {
                    var me = BoneLib.Player.RigManager;
                    if (me == null) return false;
                    if (trp.transform != null && trp.transform.IsChildOf(me.transform)) return true;
                    if (trp.root != null && trp.root.transform != null &&
                        (trp.root.transform == me.transform || trp.root.transform.IsChildOf(me.transform)))
                        return true;
                    var mine = me.GetComponentInChildren<TriggerRefProxy>(true);
                    if (mine != null && mine == trp) return true;
                }
                catch { }
                return false;
            }

            public static void Spawn()
            {
                bool hasServer = false;
                try { hasServer = LabFusion.Network.NetworkInfo.HasServer; } catch { }
                if (!hasServer)
                {
                    MelonLogger.Msg("Security Guards: no Fusion lobby — join/create one first.");
                    return;
                }

                string bc = FindBarcode();
                if (bc == null)
                {
                    MelonLogger.Msg("Security Guards: crate 'Security Guard' not found in warehouse.");
                    return;
                }

                var me = BoneLib.Player.RigManager;
                if (me == null)
                {
                    MelonLogger.Msg("Security Guards: no local rig.");
                    return;
                }

                Despawn();
                _active = true;
                _weaponLookupDone = false;
                _weaponBarcode = null;
                _heartbeat = 0f; // log hb immediately on first TryArmSquad
                _timer = 0f;

                // Resolve AKM up-front so the log always shows whether the crate exists.
                string akm = FindWeaponBarcode();
                MelonLogger.Msg(akm != null
                    ? $"Security Guards: AKM barcode ready -> {akm}"
                    : "Security Guards: AKM barcode NOT found in warehouse.");

                Vector3 c = RigPos(me);
                Vector3 fwd = Vector3.forward;
                Vector3 right = Vector3.right;
                try
                {
                    var t = me.transform;
                    fwd = t.forward; fwd.y = 0f;
                    if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
                    fwd.Normalize();
                    right = Vector3.Cross(Vector3.up, fwd).normalized;
                }
                catch { }

                Vector3[] offsets =
                {
                    fwd * EscortRadius + right * (-EscortRadius * 0.85f),
                    fwd * EscortRadius + right * (EscortRadius * 0.85f),
                    fwd * (EscortRadius + 1.1f),
                };

                for (int i = 0; i < GuardCount; i++)
                {
                    Vector3 p = PlaceOnNavMesh(c + offsets[i % offsets.Length], c);
                    SpawnOne(bc, p);
                }
                // NOTE: MelonCoroutines IEnumerator does NOT resume on Quest LemonLoader —
                // arming is driven from Tick.TryArmSquad every frame instead.
                MelonLogger.Msg(
                    $"Security Guards: spawn requested x{GuardCount} (settle {SettleTime:0.#}s, Tick-arm at {ArmDelay:0.#}s).");
            }

            /// <summary>
            /// Quest-safe arming: MelonCoroutines never continued past the first yield in 2.29.9.
            /// Called every frame from Tick.
            /// </summary>
            private static void TryArmSquad()
            {
                if (!_active) return;
                float now = Time.time;

                _heartbeat -= Time.deltaTime;
                if (_heartbeat <= 0f)
                {
                    _heartbeat = HeartbeatEvery;
                    int readyN = 0, armedN = 0, reqN = 0;
                    for (int i = 0; i < _guards.Count; i++)
                    {
                        var g = _guards[i];
                        if (!BodyAlive(g.body)) continue;
                        if (g.ready) readyN++;
                        if (g.armed) armedN++;
                        if (g.armRequested) reqN++;
                    }
                    MelonLogger.Msg(
                        $"Security Guards: hb squad={_guards.Count} ready={readyN} armed={armedN} req={reqN}");
                }

                for (int i = 0; i < _guards.Count; i++)
                {
                    var g = _guards[i];
                    try
                    {
                        if (!BodyAlive(g.body)) continue;

                        CacheParts(ref g);
                        if (!g.ready)
                        {
                            // Soft ready: have puppet+nav after first activate.
                            g.ready = IsGuardReady(g);
                            if (!g.ready && g.puppet != null && g.nav != null)
                                g.ready = true; // still try to arm even if mode flickers
                        }

                        float age = now - g.spawnedAt;
                        if (!g.armed && !g.armRequested && age >= ArmDelay)
                        {
                            MelonLogger.Msg(
                                $"Security Guards: Tick-arm id={g.entityId} age={age:0.0}s ready={g.ready}");
                            RequestWeapon(ref g);
                        }

                        _guards[i] = g;
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Warning($"Security Guards: TryArm i={i}: {e.Message}");
                    }
                }
            }

            public static void Despawn()
            {
                _active = false;
                _pendingArmBodyIds.Clear();
                for (int i = 0; i < _guards.Count; i++)
                {
                    var g = _guards[i];
                    DespawnWeapon(g);
                    try
                    {
                        if (g.entityId != 0)
                        {
                            LabFusion.RPC.NetworkAssetSpawner.Despawn(
                                new LabFusion.RPC.NetworkAssetSpawner.DespawnRequestInfo
                                {
                                    EntityID = g.entityId,
                                    DespawnEffect = false,
                                });
                        }
                        else if (BodyAlive(g.body))
                            UnityEngine.Object.Destroy(g.body);
                    }
                    catch
                    {
                        try { if (BodyAlive(g.body)) UnityEngine.Object.Destroy(g.body); } catch { }
                    }
                }
                _guards.Clear();
                MelonLogger.Msg("Security Guards: despawned.");
            }

            private static void DespawnWeapon(Guard g)
            {
                try
                {
                    if (g.weaponEntityId != 0)
                    {
                        LabFusion.RPC.NetworkAssetSpawner.Despawn(
                            new LabFusion.RPC.NetworkAssetSpawner.DespawnRequestInfo
                            {
                                EntityID = g.weaponEntityId,
                                DespawnEffect = false,
                            });
                    }
                    else if (g.weapon != null)
                        UnityEngine.Object.Destroy(g.weapon);
                }
                catch
                {
                    try { if (g.weapon != null) UnityEngine.Object.Destroy(g.weapon); } catch { }
                }
            }

            public static void Tick()
            {
                if (!_active) return;

                // Arm FIRST every frame — never gated on RigManager / throttle / escort.
                // (2.29.9 MelonCoroutines died after first yield; Tick backup never logged arm check.)
                try { TryArmSquad(); }
                catch (Exception e) { MelonLogger.Warning("Guards TryArm: " + e.Message); }

                try
                {
                    var me = BoneLib.Player.RigManager;
                    if (me == null) return;
                    var myProxy = GetProxy(me);

                    try
                    {
                        if (myProxy != null)
                            _ownerTeam = myProxy.teamNumber;
                    }
                    catch { }

                    // Per-frame protect (no Harmony on Quest).
                    for (int i = 0; i < _guards.Count; i++)
                    {
                        try
                        {
                            var g = _guards[i];
                            if (!BodyAlive(g.body) || g.nav == null) continue;
                            ApplyAllegiance(ref g, myProxy);
                            ProtectOwner(ref g, myProxy);
                            _guards[i] = g;
                        }
                        catch { }
                    }

                    _timer -= Time.deltaTime;
                    if (_timer > 0f) return;
                    _timer = TickInterval;

                    Vector3 c = RigPos(me);
                    float now = Time.time;
                    var enemy = FindNearestEnemyProxy(c);

                    for (int i = _guards.Count - 1; i >= 0; i--)
                    {
                        var g = _guards[i];
                        try
                        {
                            if (!BodyAlive(g.body))
                            {
                                DespawnWeapon(g);
                                _guards.RemoveAt(i);
                                continue;
                            }

                            EnsureOwnership(g);
                            CacheParts(ref g);

                            if (!g.ready || !g.activatedOnce)
                            {
                                ActivateGuard(ref g, full: !g.activatedOnce);
                                g.ready = IsGuardReady(g) || (g.puppet != null && g.nav != null);
                                if (g.ready && !g.blockedCols)
                                {
                                    try { g.nav?.BlockCollisions(5f); g.blockedCols = true; } catch { }
                                }
                            }
                            else
                            {
                                Stabilize(ref g);
                                ResnapIfBuried(ref g, c);
                            }

                            IgnorePlayerCollisions(ref g, me);
                            ApplyAllegiance(ref g, myProxy);
                            ProtectOwner(ref g, myProxy);

                            bool settling = (now - g.spawnedAt) < SettleTime;
                            if (settling)
                            {
                                // Plant feet: home only, agent stopped, no SetPath.
                                try
                                {
                                    if (g.agent != null) g.agent.isStopped = true;
                                }
                                catch { }
                                try { UpdateHomeOnly(ref g, i, c); } catch { }
                            }
                            else
                            {
                                g.settleDone = true;
                                bool fighting = false;
                                try
                                {
                                    fighting = enemy != null && DirectHostility(ref g, enemy, myProxy);
                                }
                                catch (Exception e)
                                {
                                    MelonLogger.Warning("Guards agro: " + e.Message);
                                }
                                try
                                {
                                    if (!fighting)
                                        Escort(ref g, i, c);
                                    else
                                        UpdateHomeOnly(ref g, i, c);
                                }
                                catch (Exception e)
                                {
                                    MelonLogger.Warning("Guards escort: " + e.Message);
                                }
                            }

                            _guards[i] = g;
                        }
                        catch (Exception e)
                        {
                            MelonLogger.Warning($"Guards Tick i={i}: {e.Message}");
                        }
                    }

                    if (_guards.Count == 0) _active = false;
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("Guards Tick: " + e.Message);
                }
            }

            private static TriggerRefProxy GetProxy(RigManager rig)
            {
                if (rig == null) return null;
                try { return rig.GetComponentInChildren<TriggerRefProxy>(true); }
                catch { return null; }
            }

            private static TriggerRefProxy FindNearestEnemyProxy(Vector3 from)
            {
                TriggerRefProxy best = null;
                float bestDist = AgroRange * AgroRange;
                try
                {
                    foreach (var np in NetworkPlayer.Players)
                    {
                        if (np == null || np.PlayerID == null || np.PlayerID.IsMe || !np.HasRig) continue;
                        RigManager rig = null;
                        try { rig = np.RigRefs.RigManager; } catch { }
                        if (rig == null) continue;
                        float d = (RigPos(rig) - from).sqrMagnitude;
                        if (d > bestDist) continue;
                        var trp = GetProxy(rig);
                        if (trp == null) continue;
                        bestDist = d;
                        best = trp;
                    }
                }
                catch { }
                return best;
            }

            /// <summary>
            /// Same team as local player so vanilla sensor agro skips you.
            /// Other players are usually also that team — we still force SetAgro on them.
            /// </summary>
            private static void ApplyAllegiance(ref Guard g, TriggerRefProxy myProxy)
            {
                if (g.nav == null) return;
                int team = _ownerTeam;
                try
                {
                    if (myProxy != null) team = myProxy.teamNumber;
                }
                catch { }

                try { g.nav.SetTeam(team); } catch { }
                try
                {
                    if (g.nav.sensors != null && g.nav.sensors.selfTrp != null)
                        g.nav.sensors.selfTrp.teamNumber = team;
                }
                catch { }

                if (g.allegianceOnce || g.body == null) return;
                g.allegianceOnce = true;
                try
                {
                    foreach (var brain in g.body.GetComponentsInChildren<AIBrain>(true))
                    {
                        if (brain == null) continue;
                        try { brain.SpawnGroupIgnore(true); } catch { }
                    }
                    try { g.nav.enableThrowAttack = false; } catch { }
                }
                catch { }
            }

            private static void ProtectOwner(ref Guard g, TriggerRefProxy myProxy)
            {
                if (g.nav == null || myProxy == null) return;
                try
                {
                    var sens = g.nav.sensors;
                    if (sens != null)
                    {
                        try { sens.RemoveTarget(myProxy); } catch { }
                        try
                        {
                            if (sens.target == myProxy)
                                sens.target = null;
                        }
                        catch { }
                    }
                }
                catch { }

                try
                {
                    // If locked onto you — drop agro immediately.
                    var sens = g.nav.sensors;
                    bool onMe = sens != null && sens.target == myProxy;
                    if (onMe || IsLocalPlayerProxy(sens != null ? sens.target : null))
                    {
                        try { sens.target = null; } catch { }
                        try { g.nav.SwitchMentalState(BehaviourBaseNav.MentalState.Roam); } catch { }
                    }
                }
                catch { }
            }

            private static bool DirectHostility(ref Guard g, TriggerRefProxy enemy, TriggerRefProxy myProxy)
            {
                if (g.nav == null || enemy == null) return false;
                if (IsLocalPlayerProxy(enemy)) return false;
                if (myProxy != null && enemy == myProxy) return false;

                // Don't overwrite if somehow still targeting owner.
                try
                {
                    if (g.nav.sensors != null && g.nav.sensors.target == myProxy)
                        return false;
                }
                catch { }

                try
                {
                    g.nav.AddThreat(enemy, 100f);
                    g.nav.SetAgro(enemy);
                    try { g.nav.SetEngaged(enemy); } catch { }
                    return true;
                }
                catch { return false; }
            }

            private static void Stabilize(ref Guard g)
            {
                if (g.nav == null) return;
                try
                {
                    var loco = g.nav.locoState;
                    if (loco == BehaviourBaseNav.LocoState.Fallen ||
                        loco == BehaviourBaseNav.LocoState.InAir)
                    {
                        try { g.nav.SwitchLocoState(BehaviourBaseNav.LocoState.GetUp, 0f, true); } catch { }
                    }
                }
                catch { }

                // Keep muscle drives alive without re-running full ActivateGuard.
                try
                {
                    var pm = g.puppet;
                    if (pm == null) return;
                    if (pm.muscleSpring <= 0f && pm._defaultMuscleSpring > 0f)
                        pm.muscleSpring = pm._defaultMuscleSpring;
                    if (pm.muscleWeight < 0.5f)
                        pm.muscleWeight = pm._defaultMuscleWeight > 0f ? pm._defaultMuscleWeight : 1f;
                }
                catch { }
            }

            private static void ResnapIfBuried(ref Guard g, Vector3 playerPos)
            {
                if (g.body == null) return;
                try
                {
                    Vector3 p = g.body.transform.position;
                    if (p.y > playerPos.y - ResnapBelow) return;
                    Vector3 fixedPos = PlaceOnNavMesh(
                        new Vector3(p.x, playerPos.y, p.z), playerPos);
                    TeleportGuard(ref g, fixedPos);
                }
                catch { }
            }

            private static void TeleportGuard(ref Guard g, Vector3 pos)
            {
                if (g.body == null) return;
                try
                {
                    g.body.transform.position = pos;
                    if (g.agent != null && g.agent.enabled)
                    {
                        try { g.agent.Warp(pos); } catch { }
                    }
                    try
                    {
                        var marrow = MarrowEntity.Cache.Get(g.body);
                        if (marrow != null)
                            marrow.Teleport(pos, g.body.transform.rotation);
                    }
                    catch { }
                    try { g.nav?.BlockCollisions(1.25f); } catch { }
                }
                catch { }
            }

            private static void IgnorePlayerCollisions(ref Guard g, RigManager me)
            {
                if (g.ignoredPlayerCols || g.body == null || me == null) return;
                try
                {
                    var mine = me.GetComponentsInChildren<Collider>(true);
                    var theirs = g.body.GetComponentsInChildren<Collider>(true);
                    for (int i = 0; i < mine.Length; i++)
                    {
                        if (mine[i] == null || !mine[i].enabled) continue;
                        for (int j = 0; j < theirs.Length; j++)
                        {
                            if (theirs[j] == null || !theirs[j].enabled) continue;
                            try { Physics.IgnoreCollision(mine[i], theirs[j], true); } catch { }
                        }
                    }
                    g.ignoredPlayerCols = true;
                }
                catch { }
            }

            private static void UpdateHomeOnly(ref Guard g, int index, Vector3 center)
            {
                if (g.nav == null) return;
                Vector3 home = EscortPoint(center, index);
                try { g.nav.SetHomeIsPost(false); } catch { }
                try { g.nav.SetHomePosition(home, true, true); } catch { }
            }

            private static Vector3 EscortPoint(Vector3 center, int index)
            {
                Vector3 fwd = Vector3.forward;
                Vector3 right = Vector3.right;
                try
                {
                    var me = BoneLib.Player.RigManager;
                    if (me != null)
                    {
                        fwd = me.transform.forward; fwd.y = 0f;
                        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
                        fwd.Normalize();
                        right = Vector3.Cross(Vector3.up, fwd).normalized;
                    }
                }
                catch { }

                Vector3[] offsets =
                {
                    fwd * EscortRadius + right * (-EscortRadius * 0.85f),
                    fwd * EscortRadius + right * (EscortRadius * 0.85f),
                    fwd * (EscortRadius + 1.1f),
                };
                return PlaceOnNavMesh(center + offsets[index % offsets.Length], center);
            }

            private static void Escort(ref Guard g, int index, Vector3 center)
            {
                if (g.nav == null) return;
                Vector3 home = EscortPoint(center, index);
                try { g.nav.freezeWhileResting = false; } catch { }
                try { g.nav.SetHomeIsPost(false); } catch { }
                try { g.nav.SetHomePosition(home, true, true); } catch { }

                // Don't hard-SetPath every tick while getting up — causes stumble loops.
                bool gettingUp = false;
                try
                {
                    var loco = g.nav.locoState;
                    gettingUp = loco == BehaviourBaseNav.LocoState.Fallen
                                || loco == BehaviourBaseNav.LocoState.GetUp
                                || loco == BehaviourBaseNav.LocoState.InAir;
                }
                catch { }

                if (!gettingUp)
                {
                    try { g.nav.SetPath(home); } catch { }
                    if (g.agent != null)
                    {
                        try
                        {
                            if (!g.agent.enabled) g.agent.enabled = true;
                            if (g.agent.isOnNavMesh)
                            {
                                g.agent.isStopped = false;
                                g.agent.SetDestination(home);
                            }
                        }
                        catch { }
                    }
                }

                try
                {
                    if (g.nav.mentalState == BehaviourBaseNav.MentalState.Rest)
                        g.nav.SwitchMentalState(BehaviourBaseNav.MentalState.Roam);
                }
                catch
                {
                    try { g.nav.SwitchMentalState(BehaviourBaseNav.MentalState.Roam); } catch { }
                }
            }

            private static void RequestWeapon(ref Guard g)
            {
                MelonLogger.Msg($"Security Guards: arm check id={g.entityId} armed={g.armed} req={g.armRequested}");
                string wbc = FindWeaponBarcode();
                if (wbc == null || !BodyAlive(g.body))
                {
                    MelonLogger.Warning("Security Guards: no AKM/Rifle barcode — guards stay unarmed.");
                    g.armRequested = true;
                    return;
                }

                g.armRequested = true;
                int bodyId = g.body.GetInstanceID();
                Vector3 handPos = g.body.transform.position + Vector3.up * 1.1f + g.body.transform.right * 0.25f;
                try
                {
                    var hand = FindRightHand(g);
                    if (hand != null) handPos = hand.position;
                }
                catch { }

                try
                {
                    var spawnable = new Spawnable
                    {
                        crateRef = new SpawnableCrateReference(wbc)
                    };
                    // Queue body id BEFORE spawn — callback must be a method group (no lambda capture on Quest).
                    _pendingArmBodyIds.Enqueue(bodyId);
                    var info = new LabFusion.RPC.NetworkAssetSpawner.SpawnRequestInfo
                    {
                        Spawnable = spawnable,
                        Position = handPos,
                        Rotation = g.body.transform.rotation,
                        SpawnEffect = false,
                        SpawnSource = LabFusion.Entities.EntitySource.Player,
                        SpawnCallback = OnWeaponSpawnedQueued,
                    };
                    LabFusion.RPC.NetworkAssetSpawner.Spawn(info);
                    MelonLogger.Msg($"Security Guards: spawning AKM/Rifle '{wbc}' for body={bodyId}.");
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("Guards arm: " + e.Message);
                    g.armRequested = false;
                    try { if (_pendingArmBodyIds.Count > 0) _pendingArmBodyIds.Dequeue(); } catch { }
                }
            }

            private static void OnWeaponSpawnedQueued(LabFusion.RPC.NetworkAssetSpawner.SpawnCallbackInfo info)
            {
                int bodyId = 0;
                try
                {
                    if (_pendingArmBodyIds.Count > 0)
                        bodyId = _pendingArmBodyIds.Dequeue();
                }
                catch { }
                MelonLogger.Msg($"Security Guards: weapon callback bodyId={bodyId} go={(info.Spawned != null)}");
                OnWeaponSpawned(info, bodyId);
            }

            private static void OnWeaponSpawned(LabFusion.RPC.NetworkAssetSpawner.SpawnCallbackInfo info, int bodyId)
            {
                try
                {
                    var go = info.Spawned;
                    if (go == null) return;

                    ushort wid = 0;
                    try
                    {
                        if (info.Entity != null)
                        {
                            wid = info.Entity.ID;
                            ClaimOwnership(info.Entity);
                        }
                    }
                    catch { }

                    for (int i = 0; i < _guards.Count; i++)
                    {
                        var g = _guards[i];
                        if (g.body == null || g.body.GetInstanceID() != bodyId) continue;
                        AttachWeapon(ref g, go, wid);
                        _guards[i] = g;
                        MelonLogger.Msg($"Security Guards: weapon attached to id={g.entityId}.");
                        return;
                    }

                    try
                    {
                        if (wid != 0)
                            LabFusion.RPC.NetworkAssetSpawner.Despawn(
                                new LabFusion.RPC.NetworkAssetSpawner.DespawnRequestInfo
                                { EntityID = wid, DespawnEffect = false });
                        else
                            UnityEngine.Object.Destroy(go);
                    }
                    catch { }
                }
                catch (Exception e) { MelonLogger.Warning("Guards onWeapon: " + e.Message); }
            }

            private static void AttachWeapon(ref Guard g, GameObject weaponGo, ushort weaponEntityId)
            {
                if (g.body == null || weaponGo == null) return;
                g.weapon = weaponGo;
                g.weaponEntityId = weaponEntityId;

                Rigidbody handRb = FindRightHand(g);
                Transform handTf = handRb != null ? handRb.transform : g.body.transform;

                // No FixedJoint — Quest NPCs tip over from joint + gun mass. Parent kinematic instead.
                try
                {
                    foreach (var j in weaponGo.GetComponentsInChildren<Joint>(true))
                    {
                        if (j == null) continue;
                        try { UnityEngine.Object.Destroy(j); } catch { }
                    }
                }
                catch { }

                try
                {
                    foreach (var rb in weaponGo.GetComponentsInChildren<Rigidbody>(true))
                    {
                        if (rb == null) continue;
                        try
                        {
                            rb.velocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;
                            rb.isKinematic = true;
                            rb.useGravity = false;
                            rb.detectCollisions = false;
                        }
                        catch { }
                    }
                }
                catch { }

                try
                {
                    foreach (var col in weaponGo.GetComponentsInChildren<Collider>(true))
                    {
                        if (col == null) continue;
                        try { col.enabled = false; } catch { }
                    }
                }
                catch { }

                try
                {
                    weaponGo.transform.SetParent(handTf, false);
                    weaponGo.transform.localPosition = new Vector3(0f, 0f, 0.05f);
                    weaponGo.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                }
                catch { }

                try
                {
                    var gun = weaponGo.GetComponentInChildren<Gun>(true);
                    if (gun != null)
                    {
                        try
                        {
                            if (g.nav != null && g.nav.sensors != null && g.nav.sensors.selfTrp != null)
                                gun.proxyOverride = g.nav.sensors.selfTrp;
                        }
                        catch { }
                        try { gun.Charge(); } catch { }
                    }
                }
                catch { }

                try
                {
                    foreach (var host in weaponGo.GetComponentsInChildren<InteractableHost>(true))
                    {
                        if (host == null) continue;
                        try { host.DisableFarHover(); } catch { }
                    }
                }
                catch { }

                g.armed = true;
            }

            private static Rigidbody FindRightHand(Guard g)
            {
                try
                {
                    if (g.nav != null && g.nav.sensors != null && g.nav.sensors.selfTrp != null)
                    {
                        var rb = g.nav.sensors.selfTrp.rtHandRb;
                        if (rb != null) return rb;
                    }
                }
                catch { }

                if (g.body == null) return null;
                try
                {
                    foreach (var t in g.body.GetComponentsInChildren<Transform>(true))
                    {
                        if (t == null) continue;
                        string n = t.name;
                        if (string.IsNullOrEmpty(n)) continue;
                        string low = n.ToLowerInvariant();
                        if (low.Contains("hand_r") || low.Contains("r_hand") || low.Contains("righthand")
                            || low.Contains("hand.r") || low.Contains("right hand")
                            || (low.Contains("hand") && low.Contains("right")))
                        {
                            var rb = t.GetComponent<Rigidbody>();
                            if (rb != null) return rb;
                        }
                    }
                }
                catch { }
                return null;
            }

            private static void SpawnOne(string barcode, Vector3 pos)
            {
                try
                {
                    var spawnable = new Spawnable
                    {
                        crateRef = new Il2CppSLZ.Marrow.Warehouse.SpawnableCrateReference(barcode)
                    };
                    Vector3 look = Vector3.forward;
                    try
                    {
                        if (BoneLib.Player.RigManager != null)
                            look = (RigPos(BoneLib.Player.RigManager) - pos).normalized;
                        if (look.sqrMagnitude < 0.001f) look = Vector3.forward;
                    }
                    catch { }

                    var info = new LabFusion.RPC.NetworkAssetSpawner.SpawnRequestInfo
                    {
                        Spawnable = spawnable,
                        Position = pos,
                        Rotation = Quaternion.LookRotation(look),
                        SpawnEffect = true,
                        SpawnSource = LabFusion.Entities.EntitySource.Player,
                        SpawnCallback = OnSpawned,
                    };
                    LabFusion.RPC.NetworkAssetSpawner.Spawn(info);
                }
                catch (Exception e) { MelonLogger.Warning("Guards spawn: " + e.Message); }
            }

            private static void OnSpawned(LabFusion.RPC.NetworkAssetSpawner.SpawnCallbackInfo info)
            {
                try
                {
                    var go = info.Spawned;
                    if (go == null) return;

                    ushort id = 0;
                    try
                    {
                        var ent = info.Entity;
                        if (ent != null)
                        {
                            id = ent.ID;
                            ClaimOwnership(ent);
                        }
                    }
                    catch { }

                    // Late entity resolve: Fusion may hand back a GO before NetworkEntity exists.
                    if (id == 0)
                    {
                        try
                        {
                            var marrow = MarrowEntity.Cache.Get(go);
                            if (marrow != null && IMarrowEntityExtender.Cache.TryGet(marrow, out var ne) && ne != null)
                            {
                                id = ne.ID;
                                ClaimOwnership(ne);
                            }
                        }
                        catch { }
                    }

                    var g = new Guard
                    {
                        body = go,
                        entityId = id,
                        wakeUntil = Time.time + WakeWindow,
                        ready = false,
                        spawnedAt = Time.time,
                    };
                    CacheParts(ref g);
                    ActivateGuard(ref g, full: true);
                    ApplyAllegiance(ref g, GetProxy(BoneLib.Player.RigManager));
                    g.ready = IsGuardReady(g) || (g.puppet != null && g.nav != null);
                    if (g.ready)
                    {
                        try { g.nav?.BlockCollisions(5f); g.blockedCols = true; } catch { }
                    }
                    _guards.Add(g);

                    // No MelonCoroutines — they do not resume on Quest. Tick handles wake/arm.

                    if (g.puppet == null)
                    {
                        DumpComponents(go);
                        MelonLogger.Warning(
                            "Security Guards: no PuppetMaster yet on spawn callback — waiting for full NPC hierarchy. " +
                            "If this stays Avatar+Poolee only, the crate was not an NPC SpawnableCrate.");
                    }

                    MelonLogger.Msg(
                        $"Security Guards: spawned id={id} ready={g.ready} mode={(g.puppet != null ? g.puppet.mode.ToString() : "?")} " +
                        $"state={(g.puppet != null ? g.puppet.state.ToString() : "?")} (squad {_guards.Count}).");
                }
                catch (Exception e) { MelonLogger.Warning("Guards onSpawned: " + e.Message); }
            }

            // WakeRoutine removed — MelonCoroutines IEnumerator never continued on Quest (2.29.9 log).

            private static void ClaimOwnership(NetworkEntity ent)
            {
                if (ent == null) return;
                try
                {
                    if (!ent.IsRegistered) return;
                    if (!ent.IsOwner)
                        NetworkEntityManager.TakeOwnership(ent);
                }
                catch { }
            }

            private static void EnsureOwnership(Guard g)
            {
                try
                {
                    if (g.puppet == null) return;
                    if (PuppetMasterExtender.Cache.TryGet(g.puppet, out var ent) && ent != null)
                        ClaimOwnership(ent);
                }
                catch { }

                try
                {
                    var pm = g.puppet;
                    if (pm == null) return;
                    pm.updateJointAnchors = true;
                    if (pm._defaultMuscleSpring > 0f) pm.muscleSpring = pm._defaultMuscleSpring;
                    if (pm._defaultMuscleDamper >= 0f) pm.muscleDamper = pm._defaultMuscleDamper;
                    if (pm._defaultMuscleWeight > 0f) pm.muscleWeight = pm._defaultMuscleWeight;
                    else if (pm.muscleWeight < 0.5f) pm.muscleWeight = 1f;
                    if (pm.mappingWeight < 0.5f) pm.mappingWeight = 1f;
                }
                catch { }
            }

            private static bool IsGuardReady(Guard g)
            {
                try
                {
                    if (g.puppet == null || g.nav == null) return false;
                    if (g.puppet.isDead) return false;
                    return g.puppet.mode == PuppetMaster.Mode.Active
                           && g.puppet.state == PuppetMaster.State.Alive
                           && g.nav.enabled
                           && !g.nav.deactivated;
                }
                catch { return false; }
            }

            private static void CacheParts(ref Guard g)
            {
                var go = g.body;
                if (go == null) return;
                try
                {
                    if (g.puppet == null)
                        g.puppet = go.GetComponentInChildren<PuppetMaster>(true);
                    if (g.nav == null)
                        g.nav = go.GetComponentInChildren<BehaviourBaseNav>(true);
                    if (g.agent == null)
                        g.agent = go.GetComponentInChildren<NavMeshAgent>(true);
                }
                catch { }
            }

            private static void ActivateGuard(ref Guard g, bool full = true)
            {
                var go = g.body;
                if (go == null) return;

                CacheParts(ref g);
                EnsureOwnership(g);

                // Light path: already woken once — only keep muscles / GetUp. Full Resurrect/SwitchModes
                // every frame is what knocks Security Guards over on Quest.
                if (!full && g.activatedOnce)
                {
                    Stabilize(ref g);
                    return;
                }

                try
                {
                    var pm = g.puppet;
                    if (pm != null)
                    {
                        pm.enabled = true;
                        try { pm.gameObject.SetActive(true); } catch { }

                        try
                        {
                            if (!pm.CheckIfInitiated())
                                pm.Initiate();
                        }
                        catch
                        {
                            try { pm.Initiate(); } catch { }
                        }

                        try
                        {
                            if (pm.isDead || pm.state != PuppetMaster.State.Alive)
                                pm.Resurrect();
                        }
                        catch
                        {
                            try { pm.state = PuppetMaster.State.Alive; } catch { }
                        }

                        try { pm.SetAnimationEnabled(true); } catch { }

                        try
                        {
                            pm.updateJointAnchors = true;
                            if (pm._defaultMuscleSpring > 0f) pm.muscleSpring = pm._defaultMuscleSpring;
                            else if (pm.muscleSpring <= 0f) pm.muscleSpring = 100f;
                            if (pm._defaultMuscleDamper >= 0f) pm.muscleDamper = pm._defaultMuscleDamper;
                            if (pm._defaultMuscleWeight > 0f) pm.muscleWeight = pm._defaultMuscleWeight;
                            else pm.muscleWeight = 1f;
                            pm.mappingWeight = 1f;
                        }
                        catch { }

                        try
                        {
                            if (pm.mode != PuppetMaster.Mode.Active)
                            {
                                var from = pm.mode;
                                pm.mode = PuppetMaster.Mode.Active;
                                if (from == PuppetMaster.Mode.Disabled)
                                {
                                    try { pm.StartCoroutine(pm.DisabledToActive()); }
                                    catch { try { pm.SwitchModes(); } catch { } }
                                }
                                else
                                {
                                    try { pm.SwitchModes(); }
                                    catch
                                    {
                                        try { pm.StartCoroutine(pm.DisabledToActive()); } catch { }
                                    }
                                }
                            }
                            // Do NOT call SwitchModes when already Active — ragdolls them.
                        }
                        catch
                        {
                            try { pm.mode = PuppetMaster.Mode.Active; } catch { }
                        }

                        try { pm.FixMusclePositions(); } catch { }
                    }
                }
                catch { }

                try
                {
                    var nav = g.nav;
                    if (nav != null)
                    {
                        nav.enabled = true;
                        try { nav.gameObject.SetActive(true); } catch { }
                        try { nav.deactivated = false; } catch { }
                        try { nav.freezeWhileResting = false; } catch { }

                        // Minimal wake — full Resurrect/ResetAnimator/Standing spam ragdolls Quest NPCs.
                        try { nav.OnEntityUncull(); } catch { }
                        try { nav.Activate(); } catch { }
                        try { nav.EnableNav(); } catch { }
                        try { nav.SwitchMentalState(BehaviourBaseNav.MentalState.Roam); } catch { }
                    }
                    else
                    {
                        foreach (var b in go.GetComponentsInChildren<BehaviourBase>(true))
                        {
                            if (b == null) continue;
                            try
                            {
                                b.enabled = true;
                                b.deactivated = false;
                                b.Activate();
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                try
                {
                    foreach (var brain in go.GetComponentsInChildren<Il2CppSLZ.Marrow.AI.AIBrain>(true))
                    {
                        if (brain == null) continue;
                        brain.enabled = true;
                        try { brain.OnResurrection(); }
                        catch { try { brain.Reset(); } catch { } }
                    }
                }
                catch { }

                try
                {
                    var agent = g.agent;
                    if (agent != null)
                    {
                        agent.enabled = true;
                        // Warp only on first activate — repeated Warp = stumble.
                        if (!g.activatedOnce)
                        {
                            Vector3 p = go.transform.position;
                            if (NavMesh.SamplePosition(p, out var hit, 4f, NavMesh.AllAreas))
                            {
                                try { agent.Warp(hit.position); } catch { }
                                try { go.transform.position = hit.position; } catch { }
                            }
                        }
                        try { agent.isStopped = false; } catch { }
                        try { agent.updatePosition = true; agent.updateRotation = true; } catch { }
                    }
                }
                catch { }

                try
                {
                    foreach (var col in go.GetComponentsInChildren<Collider>(true))
                    {
                        if (col == null) continue;
                        try { col.enabled = true; } catch { }
                    }
                    foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
                    {
                        if (rb == null) continue;
                        try
                        {
                            rb.detectCollisions = true;
                            rb.WakeUp();
                        }
                        catch { }
                    }
                }
                catch { }

                try
                {
                    foreach (var anim in go.GetComponentsInChildren<Animator>(true))
                    {
                        if (anim == null) continue;
                        anim.enabled = true;
                        try { anim.cullingMode = AnimatorCullingMode.AlwaysAnimate; } catch { }
                    }
                }
                catch { }

                g.activatedOnce = true;
            }

            private static Vector3 PlaceOnNavMesh(Vector3 desired, Vector3 fallbackPlayer)
            {
                // Prefer NavMesh sample, then ground ray from high above.
                try
                {
                    Vector3 probe = desired;
                    if (NavMesh.SamplePosition(probe, out var navHit, 4f, NavMesh.AllAreas))
                        probe = navHit.position;
                    else if (NavMesh.SamplePosition(fallbackPlayer + Vector3.up * 0.2f, out navHit, 6f, NavMesh.AllAreas))
                        probe = navHit.position + (desired - fallbackPlayer);

                    if (Physics.Raycast(probe + Vector3.up * 6f, Vector3.down, out var hit, 14f,
                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        // Reject hits far below the player (ceilings/underside geo).
                        if (hit.point.y >= fallbackPlayer.y - 2.5f)
                            return hit.point + Vector3.up * 0.08f;
                    }

                    if (NavMesh.SamplePosition(probe, out navHit, 4f, NavMesh.AllAreas))
                        return navHit.position + Vector3.up * 0.08f;
                }
                catch { }

                // Last resort: player height ring.
                return new Vector3(desired.x, fallbackPlayer.y, desired.z);
            }

            private static Vector3 SnapToGround(Vector3 pos)
            {
                return PlaceOnNavMesh(pos, pos);
            }

            private static string FindBarcode()
            {
                if (!string.IsNullOrEmpty(_barcode)) return _barcode;
                try
                {
                    var wh = AssetWarehouse.Instance;
                    if (wh == null) return null;

                    string bestId = null;
                    string bestTitle = null;
                    int bestScore = -1;

                    foreach (var crate in wh.GetCrates())
                    {
                        if (crate == null) continue;
                        if (crate.TryCast<AvatarCrate>() != null) continue;
                        if (crate.TryCast<SpawnableCrate>() == null) continue;

                        string t = crate.Title;
                        if (string.IsNullOrEmpty(t)) continue;

                        int score = 0;
                        if (t.IndexOf("Security Guard", StringComparison.OrdinalIgnoreCase) >= 0)
                            score = 100;
                        else if (t.Replace(" ", "").Equals("SecurityGuard", StringComparison.OrdinalIgnoreCase))
                            score = 95;
                        else
                            continue;

                        try
                        {
                            var tags = crate.Tags;
                            if (tags != null)
                            {
                                foreach (var tag in tags)
                                {
                                    string ts = tag != null ? tag.ToString() : null;
                                    if (string.IsNullOrEmpty(ts)) continue;
                                    if (ts.IndexOf("NPC", StringComparison.OrdinalIgnoreCase) >= 0) score += 25;
                                    if (ts.IndexOf("Enemy", StringComparison.OrdinalIgnoreCase) >= 0) score += 10;
                                }
                            }
                        }
                        catch { }

                        try
                        {
                            var pallet = crate.Pallet;
                            if (pallet != null && pallet.Internal) score += 15;
                        }
                        catch { }

                        MelonLogger.Msg($"Security Guards: candidate SpawnableCrate '{t}' score={score} -> {crate.Barcode.ID}");
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestId = crate.Barcode.ID;
                            bestTitle = t;
                        }
                    }

                    if (bestId != null)
                    {
                        _barcode = bestId;
                        MelonLogger.Msg($"Security Guards: using SpawnableCrate '{bestTitle}' score={bestScore} -> {_barcode}");
                        return _barcode;
                    }

                    MelonLogger.Msg("Security Guards: no SpawnableCrate titled 'Security Guard' (Avatar crates ignored).");
                }
                catch (Exception e) { MelonLogger.Warning("Guards barcode: " + e.Message); }
                return null;
            }

            /// <summary>Prefer official SLZ AKM GUID, then warehouse AKM/Rifle search.</summary>
            private static string FindWeaponBarcode()
            {
                if (_weaponLookupDone && !string.IsNullOrEmpty(_weaponBarcode))
                    return _weaponBarcode;

                try
                {
                    // 1) Exact collectible barcode (always try — Crate probe can lie on Quest).
                    string[] hardIds =
                    {
                        AkmBarcode,
                        "SLZ.BONELAB.Content.Spawnable.AKM",
                        "SLZ.BONELAB.Content.Spawnable.RifleAKM",
                    };
                    foreach (var hid in hardIds)
                    {
                        try
                        {
                            var cref = new SpawnableCrateReference(hid);
                            if (cref != null && cref.Crate != null)
                            {
                                _weaponBarcode = hid;
                                _weaponLookupDone = true;
                                MelonLogger.Msg($"Security Guards: weapon hard-id OK '{hid}'.");
                                return _weaponBarcode;
                            }
                        }
                        catch { }
                    }

                    var wh = AssetWarehouse.Instance;
                    if (wh == null)
                    {
                        // Don't lock failure — warehouse may appear a moment later.
                        MelonLogger.Warning("Security Guards: weapon search — AssetWarehouse null (retry later).");
                        _weaponBarcode = AkmBarcode; // spawn may still resolve the GUID
                        return _weaponBarcode;
                    }

                    string bestId = null;
                    string bestTitle = null;
                    int bestScore = -1;

                    foreach (var crate in wh.GetCrates())
                    {
                        if (crate == null) continue;
                        if (crate.TryCast<AvatarCrate>() != null) continue;
                        if (crate.TryCast<SpawnableCrate>() == null) continue;

                        string t = crate.Title ?? "";
                        string id = "";
                        try { id = crate.Barcode != null ? crate.Barcode.ID : ""; } catch { }
                        if (string.IsNullOrEmpty(t) && string.IsNullOrEmpty(id)) continue;

                        string[] avoid =
                        {
                            "Spawn Gun", "Utility", "Dev Tool", "DevTool", "Gravity", "Nimbus",
                            "Constrainer", "Balloon", "Board", "Avatar", "NPC", "Gun Gun", "Melee"
                        };
                        bool bad = false;
                        foreach (var a in avoid)
                        {
                            if (t.IndexOf(a, StringComparison.OrdinalIgnoreCase) >= 0) { bad = true; break; }
                        }
                        if (bad) continue;

                        int score = 0;
                        bool akm = t.IndexOf("AKM", StringComparison.OrdinalIgnoreCase) >= 0
                                   || id.IndexOf("AKM", StringComparison.OrdinalIgnoreCase) >= 0
                                   || id.Equals(AkmBarcode, StringComparison.OrdinalIgnoreCase);
                        bool rifle = t.IndexOf("Rifle", StringComparison.OrdinalIgnoreCase) >= 0
                                     || id.IndexOf("Rifle", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (akm) score += 200;
                        else if (rifle) score += 80;
                        else continue;

                        try
                        {
                            var pallet = crate.Pallet;
                            if (pallet != null)
                            {
                                if (pallet.Internal) score += 40;
                                string author = pallet.Author ?? "";
                                if (author.IndexOf("Stress Level Zero", StringComparison.OrdinalIgnoreCase) >= 0
                                    || author.Equals("SLZ", StringComparison.OrdinalIgnoreCase)
                                    || author.IndexOf("SLZ", StringComparison.OrdinalIgnoreCase) >= 0)
                                    score += 50;
                            }
                        }
                        catch { }

                        if (id.IndexOf("SLZ.BONELAB.Content", StringComparison.OrdinalIgnoreCase) >= 0)
                            score += 30;
                        if (id.Equals(AkmBarcode, StringComparison.OrdinalIgnoreCase))
                            score += 100;

                        MelonLogger.Msg($"Security Guards: weapon candidate '{t}' score={score} -> {id}");
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestId = id;
                            bestTitle = t;
                        }
                    }

                    if (bestId != null)
                    {
                        _weaponBarcode = bestId;
                        MelonLogger.Msg($"Security Guards: weapon using '{bestTitle}' score={bestScore} -> {_weaponBarcode}");
                    }
                    else
                    {
                        // Last resort: known GUID — NetworkAssetSpawner often resolves it anyway.
                        _weaponBarcode = AkmBarcode;
                        MelonLogger.Msg($"Security Guards: no warehouse AKM hit — forcing GUID '{AkmBarcode}'.");
                    }
                    _weaponLookupDone = true;
                }
                catch (Exception e) { MelonLogger.Warning("Guards weapon barcode: " + e.Message); }
                return _weaponBarcode;
            }

            private static void DumpComponents(GameObject go)
            {
                if (_dumped || go == null) return;
                _dumped = true;
                try
                {
                    int children = 0;
                    try { children = go.transform.childCount; } catch { }
                    bool hasMarrow = false;
                    try { hasMarrow = MarrowEntity.Cache.Get(go) != null; } catch { }
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
                    MelonLogger.Msg(
                        $"Security Guards: dump children={children} marrowEntity={hasMarrow} comps -> {sb}");
                }
                catch (Exception e) { MelonLogger.Warning("Guards dump: " + e.Message); }
            }
        }

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
                AdminNick.Install(page); // animated staff-looking nametag
                page.CreateFunction("Spawn 3 Bodyguards", new Color(0.2f, 0.55f, 1f), (Action)Guards.Spawn);
                page.CreateFunction("Despawn Bodyguards", new Color(0.5f, 0.5f, 0.5f), (Action)Guards.Despawn);
                page.CreateFunction("Avatar preview 6114112", new Color(0.7f, 0.5f, 1f), (Action)NickHider.SetAvatarPreview);
                KillAuraMenu.Install(page);
                NickHider.Install(page);
                Teleporter.Install(page);
                MelonLogger.Msg("MONSTER Panel: Kill Aura + Teleport + Nickname + Bodyguards + Spoofing PID added (LabFusion found).");
            }
            else
                MelonLogger.Msg("MONSTER Panel: LabFusion not loaded - Teleport/Nickname/Bodyguards/Spoofing PID hidden.");
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

            /// <summary>Создаёт подстраницу Teleport в корне панели и вешает авто-обновление списка.</summary>
            public static void Install(Page root)
            {
                _page = root.CreatePage("Teleport", new Color(0.3f, 0.7f, 1f), 16, true);
                if (!_hooked)
                {
                    Menu.OnPageOpened += (Action<Page>)OnPageOpened;   // при каждом открытии — свежий список
                    _hooked = true;
                }
                Rebuild();
            }

            private static void OnPageOpened(Page opened)
            {
                if (opened == _page) Rebuild();
            }

            /// <summary>Пересобираем список: под каждого игрока — подстраница с выбором направления телепорта.</summary>
            private static void Rebuild()
            {
                if (_page == null) return;
                try
                {
                    _page.RemoveAll();
                    _page.CreateFunction("Refresh", new Color(0.7f, 0.7f, 0.7f), (Action)Rebuild);

                    int count = 0;
                    foreach (var np in NetworkPlayer.Players)
                    {
                        if (np == null || np.PlayerID == null || np.PlayerID.IsMe) continue;
                        if (!np.HasRig) continue;

                        byte sid = np.PlayerID.SmallID;
                        string name = SafeName(np.Username, sid);   // без rich-text тегов и не-ASCII: шрифт BoneMenu только латиница

                        Page sub = _page.CreatePage(name, new Color(0.6f, 0.85f, 1f), 16, true);
                        sub.CreateFunction("Teleport to player", new Color(0.3f, 1f, 0.5f), (Action)(() => TeleportSelfTo(sid)));
                        sub.CreateFunction("Bring player to me", new Color(1f, 0.6f, 0.2f), (Action)(() => BringToMe(sid)));
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
