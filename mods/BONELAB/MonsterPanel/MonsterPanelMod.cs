using System;
using System.Reflection;
using BoneLib.BoneMenu;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Combat;
using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.PuppetMasta;
using LabFusion.Entities;
using MelonLoader;
using UnityEngine;
using MHealth = Il2CppSLZ.Marrow.Health;

[assembly: MelonInfo(typeof(MonsterPanel.MonsterPanelMod), "MONSTER Panel", "2.8.0", "you")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace MonsterPanel
{
    public class MonsterPanelMod : MelonMod
    {
        /// <summary>Бессмертие игрока.</summary>
        public static bool Invincible { get; private set; }

        /// <summary>Монстер-урон: ваншот врагов/объектов/игроков + жёсткий отброс.</summary>
        public static bool MonsterDamage { get; private set; }

        /// <summary>Remote Kill: наводишь рукой на игрока (зелёный маркер) + grip/триггер → максимальный урон по сети.</summary>
        public static bool RemoteKill { get; private set; }

        /// <summary>Бесконечные патроны: у всех стволов магазин не пустеет + запас в инвентаре бесконечный.</summary>
        public static bool InfiniteAmmo { get; private set; }

        private const float LaunchSpeed = 28f;
        private const float MaxDamage = 1_000_000f;

        // Remote Kill
        private const float RkRange = 40f;          // дальность наведения
        private const float RkGripThreshold = 0.6f; // grip (средний палец)
        private const float RkTriggerThreshold = 0.7f; // триггер (указательный)
        private const float RkAimRadius = 0.28f;    // «толщина» луча (SphereCast) — крестик не скачет
        private const float RkPersist = 0.4f;       // сколько держим цель после потери луча, сек
        private static readonly HandState _left = new HandState();
        private static readonly HandState _right = new HandState();

        /// <summary>Флаг: мы прямо сейчас шлём Remote Kill — бустим урон даже если Monster Damage выкл.</summary>
        private static bool _remoteKillSending;

        private const string PlayerHealthType = "Il2CppSLZ.Marrow.Player_Health";
        private const string FusionReceiverPatch = "LabFusion.Patching.PlayerDamageReceiverPatches";

        [ThreadStatic] private static bool _reentry;

        /// <summary>Состояние наведения одной руки (маркер, фиксация кнопки, удержание цели против мерцания).</summary>
        private class HandState
        {
            public GameObject Marker;
            public bool FirePrev;
            public PlayerDamageReceiver LastTarget;
            public float LastSeen;
        }

        public override void OnInitializeMelon()
        {
            BuildMenu();
            ApplyPatches();
            MelonLogger.Msg("MONSTER Panel загружен.");
        }

        public override void OnUpdate()
        {
            if (RemoteKill)
            {
                AimHand(BoneLib.Player.LeftHand?.transform, BoneLib.Player.LeftController, _left);
                AimHand(BoneLib.Player.RightHand?.transform, BoneLib.Player.RightController, _right);
            }
            else
            {
                HideMarker(_left.Marker); HideMarker(_right.Marker);
                _left.FirePrev = _right.FirePrev = false;
                _left.LastTarget = _right.LastTarget = null;
            }
        }

        // ---------------- Remote Kill ----------------

        private static void AimHand(Transform hand, BaseController controller, HandState state)
        {
            if (hand == null || controller == null) { HideMarker(state.Marker); state.FirePrev = false; return; }

            // Кнопка «выстрела»: срабатывает и на grip (средний палец), и на триггер (указательный) —
            // что нажмёшь, то и сработает. Значения пишем в лог, чтобы точно видеть, какая кнопка идёт.
            float gripF = SafeAxis(() => controller.GetGripForce());
            float trigF = SafeAxis(() => controller.GetIndexCurlAxis());
            bool fire = gripF > RkGripThreshold || trigF > RkTriggerThreshold;
            bool firedNow = fire && !state.FirePrev;

            PlayerDamageReceiver target = FindTargetPlayer(hand, out RaycastHit hit);

            if (target != null)
            {
                state.LastTarget = target;
                state.LastSeen = Time.time;
            }
            else if (state.LastTarget != null && Time.time - state.LastSeen < RkPersist)
            {
                // Луч соскользнул (игрок движется) — держим прежнюю цель короткое время, чтобы крестик не мерцал.
                target = state.LastTarget;
            }

            if (target == null)
            {
                HideMarker(state.Marker);
                // Диагностика: даже без цели показываем, что кнопка нажалась — сразу видно, рабочая ли она.
                if (firedNow)
                    MelonLogger.Msg($"Remote Kill: кнопка нажата (grip={gripF:0.00} trig={trigF:0.00}), но крестика/цели нет — наведи руку на игрока.");
                state.FirePrev = fire;
                return;
            }

            // Боевой крестик на цели (зелёный = можно убить), всегда развёрнут к лицу.
            if (state.Marker == null) state.Marker = CreateCrosshair();
            state.Marker.SetActive(true);
            state.Marker.transform.position = target.transform.position + Vector3.up * 0.25f;
            var head = BoneLib.Player.Head;
            if (head != null)
            {
                Vector3 away = state.Marker.transform.position - head.position;
                if (away.sqrMagnitude > 0.0001f)
                    state.Marker.transform.rotation = Quaternion.LookRotation(away, Vector3.up);
                // Масштаб по дистанции — крестик читаем и вблизи, и издалека.
                float dist = away.magnitude;
                state.Marker.transform.localScale = Vector3.one * Mathf.Clamp(dist * 0.25f, 0.6f, 4f);
            }

            if (firedNow)   // срабатывание по нажатию, не по удержанию
            {
                MelonLogger.Msg($"Remote Kill: FIRE (grip={gripF:0.00} trig={trigF:0.00}) → цель {target.gameObject.name}.");
                KillPlayer(target, hand, hit);
            }
            state.FirePrev = fire;
        }

        /// <summary>Безопасно читаем ось контроллера (0, если метод кинул).</summary>
        private static float SafeAxis(Func<float> read)
        {
            try { return read(); } catch { return 0f; }
        }

        /// <summary>«Толстый» луч (SphereCast) из руки → ближайший игрок (PlayerDamageReceiver), не считая себя.</summary>
        private static PlayerDamageReceiver FindTargetPlayer(Transform hand, out RaycastHit bestHit)
        {
            bestHit = default;
            Vector3 origin = hand.position + hand.forward * 0.3f;
            var hits = Physics.SphereCastAll(origin, RkAimRadius, hand.forward, RkRange,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            PlayerDamageReceiver best = null;
            float bestDist = float.MaxValue;
            foreach (var h in hits)
            {
                if (h.collider == null) continue;
                var recv = h.collider.GetComponentInParent<PlayerDamageReceiver>();
                if (recv == null) continue;
                if (IsOwnRig(recv.transform)) continue;         // не наводимся на себя
                if (h.distance < bestDist) { bestDist = h.distance; best = recv; bestHit = h; }
            }
            return best;
        }

        /// <summary>
        /// Бьём по ВСЕМ ресиверам тела цели (голова/грудь/…) максимальным уроном через тот же путь,
        /// что и рабочий Monster Damage: игровой PlayerDamageReceiver.ReceiveAttack, поверх которого
        /// сидит патч LabFusion и отправляет урон владельцу по сети. Буст гарантируем флагом.
        /// </summary>
        private static void KillPlayer(PlayerDamageReceiver target, Transform hand, RaycastHit hit)
        {
            try
            {
                _remoteKillSending = true;

                var root = target.transform.root;
                var receivers = root != null
                    ? root.GetComponentsInChildren<PlayerDamageReceiver>()
                    : null;

                int sent = 0;
                if (receivers != null && receivers.Length > 0)
                {
                    foreach (var recv in receivers)
                    {
                        if (recv == null || IsOwnRig(recv.transform)) continue;
                        SendAttack(recv, hand, hit);
                        sent++;
                    }
                }
                else
                {
                    SendAttack(target, hand, hit);
                    sent = 1;
                }

                MelonLogger.Msg($"Remote Kill: {sent} атак отправлено по {target.transform.root?.name ?? target.gameObject.name}.");
            }
            catch (Exception e) { MelonLogger.Warning("Remote Kill: " + e.Message); }
            finally { _remoteKillSending = false; }
        }

        /// <summary>Одна максимальная атака в конкретный ресивер тела.</summary>
        private static void SendAttack(PlayerDamageReceiver recv, Transform hand, RaycastHit hit)
        {
            Vector3 dir = (recv.transform.position - hand.position).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = hand.forward;
            var attack = new Attack
            {
                damage = MaxDamage,
                attackType = AttackType.Blunt,
                direction = dir,
                origin = hand.position,
                normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal : -dir,
                collider = hit.collider,
            };
            try { recv.ReceiveAttack(attack); } catch (Exception e) { MelonLogger.Warning("Remote Kill send: " + e.Message); }
        }

        /// <summary>Тактический прицел-крестик: 4 штриха вокруг центра + точка, светящийся зелёный.</summary>
        private static GameObject CreateCrosshair()
        {
            var root = new GameObject("MP_Crosshair");
            UnityEngine.Object.DontDestroyOnLoad(root);
            Color c = new Color(0.2f, 1f, 0.35f);   // ярко-зелёный

            // Штрихи (в локальной плоскости XY, форма читается как боевой прицел с зазором в центре).
            Bar(root.transform, new Vector3(0f, 0.09f, 0f), new Vector3(0.014f, 0.07f, 0.014f), c); // верх
            Bar(root.transform, new Vector3(0f, -0.09f, 0f), new Vector3(0.014f, 0.07f, 0.014f), c); // низ
            Bar(root.transform, new Vector3(0.09f, 0f, 0f), new Vector3(0.07f, 0.014f, 0.014f), c);  // право
            Bar(root.transform, new Vector3(-0.09f, 0f, 0f), new Vector3(0.07f, 0.014f, 0.014f), c); // лево
            Bar(root.transform, Vector3.zero, new Vector3(0.022f, 0.022f, 0.022f), c);               // центр-точка
            return root;
        }

        private static Material _markerMat;

        /// <summary>URP-совместимый светящийся материал (без него рантайм-примитивы в URP невидимы/розовые).</summary>
        private static Material MarkerMaterial()
        {
            if (_markerMat != null) return _markerMat;
            Color c = new Color(0.2f, 1f, 0.35f);
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            var m = new Material(sh);
            try { m.color = c; } catch { }
            try { m.SetColor("_BaseColor", c); } catch { }
            try { m.SetColor("_Color", c); } catch { }
            try { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 2f); } catch { }
            UnityEngine.Object.DontDestroyOnLoad(m);
            _markerMat = m;
            return m;
        }

        private static void Bar(Transform parent, Vector3 localPos, Vector3 scale, Color c)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = g.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);  // не мешает лучу/физике
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = scale;
            var r = g.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MarkerMaterial();  // URP-материал, иначе не видно
        }

        private static void HideMarker(GameObject marker)
        {
            if (marker != null && marker.activeSelf) marker.SetActive(false);
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
            Page page = Page.Root.CreatePage("MONSTER Panel", Color.red);
            page.CreateBool("Invincible", Color.green, Invincible, v => { Invincible = v; Log("Invincible", v); });
            page.CreateBool("Monster Damage", new Color(1f, 0.4f, 0f), MonsterDamage,
                v => { MonsterDamage = v; Log("Monster Damage", v); });
            page.CreateBool("Remote Kill", new Color(0.7f, 0.4f, 1f), RemoteKill,
                v => { RemoteKill = v; Log("Remote Kill", v); });
            page.CreateBool("Infinite Ammo", new Color(1f, 0.85f, 0.1f), InfiniteAmmo,
                v => { InfiniteAmmo = v; Log("Infinite Ammo", v); });

            // Teleport + ник: только когда загружен LabFusion.
            if (Teleporter.FusionLoaded)
            {
                NickHider.Install(page);
                Teleporter.Install(page);
                MelonLogger.Msg("MONSTER Panel: разделы Teleport + Nickname добавлены (LabFusion найден).");
            }
            else
                MelonLogger.Msg("MONSTER Panel: LabFusion не загружен — Teleport/Nickname скрыты.");
        }

        private static void Log(string name, bool on) =>
            MelonLogger.Msg(on ? $"{name}: ВКЛ" : $"{name}: ВЫКЛ");

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
            else MelonLogger.Error("MONSTER Panel: Player_Health не найден — бессмертие не активно");

            TryPatchTyped(typeof(SubBehaviourHealth), "TakeDamage", Hm(nameof(PuppetPrefix)), "SubBehaviourHealth.TakeDamage");
            TryPatchTyped(typeof(MHealth), "TAKEDAMAGE", Hm(nameof(HealthPrefix)), "Health.TAKEDAMAGE");

            Type fusion = AccessTools.TypeByName(FusionReceiverPatch);
            if (fusion != null)
                TryPatch(fusion, "ReceiveAttack", Hm(nameof(FusionAttackPrefix)));
            else
                MelonLogger.Msg("MONSTER Panel: LabFusion не найден — урон по игрокам в сети выключен (нормально без Fusion).");

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
                MelonLogger.Msg($"MONSTER Panel: Infinite Ammo — пропатчен AmmoInventory.GetCartridgeCount ({patched} перегрузк).");
            }
            catch (Exception e) { MelonLogger.Warning("MONSTER Panel: Infinite Ammo — " + e.Message); }
        }

        private static HarmonyMethod Hm(string name) =>
            new HarmonyMethod(typeof(MonsterPanelMod).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic));

        private void TryPatch(Type type, string methodName, HarmonyMethod prefix)
        {
            try
            {
                MethodBase target = AccessTools.Method(type, methodName);
                if (target == null) { MelonLogger.Warning($"MONSTER Panel: {type.Name}.{methodName} не найден"); return; }
                HarmonyInstance.Patch(target, prefix: prefix);
                MelonLogger.Msg($"MONSTER Panel: пропатчен {type.Name}.{methodName}");
            }
            catch (Exception e) { MelonLogger.Warning($"MONSTER Panel: {type.Name}.{methodName} — {e.Message}"); }
        }

        private void TryPatchTyped(Type type, string methodName, HarmonyMethod prefix, string label)
        {
            try
            {
                MethodBase target = AccessTools.Method(type, methodName);
                if (target == null) { MelonLogger.Warning($"MONSTER Panel: {label} не найден"); return; }
                HarmonyInstance.Patch(target, prefix: prefix);
                MelonLogger.Msg($"MONSTER Panel: пропатчен {label}");
            }
            catch (Exception e) { MelonLogger.Warning($"MONSTER Panel: {label} — {e.Message}"); }
        }

        private static bool GodPrefix() => !Invincible;

        private static void PuppetPrefix(SubBehaviourHealth __instance, Attack attack)
        {
            if (!MonsterDamage || _reentry || __instance == null) return;
            try { _reentry = true; __instance.Kill(); Launch(attack); }
            catch (Exception e) { MelonLogger.Warning("MONSTER Panel: puppet — " + e.Message); }
            finally { _reentry = false; }
        }

        private static void HealthPrefix(MHealth __instance)
        {
            if (!MonsterDamage || _reentry || __instance == null) return;
            try { _reentry = true; __instance.Death(); }
            catch (Exception e) { MelonLogger.Warning("MONSTER Panel: health — " + e.Message); }
            finally { _reentry = false; }
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
                Page p = root.CreatePage("Nickname", new Color(0.5f, 0.8f, 1f), 16, true);
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

            private static void SetNick(string value)
            {
                var md = Metadata();
                if (md == null) return;
                try
                {
                    if (value.Length > 32)   // страховка под лимит имени LabFusion
                        value = value.Substring(0, 32);
                    md.Nickname.SetValue(value);
                    MelonLogger.Msg($"Nickname: установлен «{value}».");
                }
                catch (Exception e) { MelonLogger.Warning("Nickname set: " + e.Message); }
            }

            private static void ResetNick()
            {
                var md = Metadata();
                if (md == null) return;
                try { md.Nickname.Remove(); MelonLogger.Msg("Nickname: сброшен на обычный."); }
                catch (Exception e) { MelonLogger.Warning("Nickname reset: " + e.Message); }
            }

            private static LabFusion.Player.PlayerMetadata Metadata()
            {
                var md = LabFusion.Player.LocalPlayer.Metadata;
                if (md == null || md.Nickname == null)
                {
                    MelonLogger.Msg("Nickname: метаданные недоступны — зайди в лобби Fusion и повтори.");
                    return null;
                }
                return md;
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
                        string name = string.IsNullOrEmpty(np.Username) ? ("Player " + sid) : np.Username;

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

            /// <summary>Телепортируемся к выбранному игроку (свой риг — синхронизируется по сети штатно).</summary>
            private static void TeleportSelfTo(byte sid)
            {
                try
                {
                    var np = Find(sid);
                    if (np == null || !np.HasRig) { MelonLogger.Msg("Teleport: игрок недоступен (вышел?)."); return; }
                    RigManager target = np.RigRefs.RigManager;
                    RigManager me = BoneLib.Player.RigManager;
                    if (target == null || me == null) return;

                    Vector3 dest = Grounded(target);
                    // Небольшой отступ, чтобы не оказаться внутри игрока.
                    Vector3 myPos = Grounded(me);
                    Vector3 off = myPos - dest; off.y = 0f;
                    off = off.sqrMagnitude > 0.01f ? off.normalized : -target.transform.forward;
                    me.Teleport(dest + off * 0.8f, true);
                    MelonLogger.Msg($"Teleport: перенёсся к {np.Username} (sid {sid}).");
                }
                catch (Exception e) { MelonLogger.Warning("Teleport self: " + e.Message); }
            }

            /// <summary>Притягиваем игрока к себе. Best-effort: его позицией владеет его клиент, может не «прилипнуть».</summary>
            private static void BringToMe(byte sid)
            {
                try
                {
                    var np = Find(sid);
                    if (np == null || !np.HasRig) { MelonLogger.Msg("Teleport: игрок недоступен (вышел?)."); return; }
                    RigManager target = np.RigRefs.RigManager;
                    RigManager me = BoneLib.Player.RigManager;
                    if (target == null || me == null) return;

                    Vector3 dest = Grounded(me);
                    var head = BoneLib.Player.Head;
                    Vector3 fwd = head != null ? head.forward : me.transform.forward;
                    fwd.y = 0f;
                    if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
                    target.Teleport(dest + fwd.normalized * 1.2f, true);
                    MelonLogger.Msg($"Teleport: притянул {np.Username} (sid {sid}) — если не прилип, это сетевое владение позицией.");
                }
                catch (Exception e) { MelonLogger.Warning("Teleport bring: " + e.Message); }
            }

            /// <summary>Позиция ног рига на полу (луч вниз от физ-рига), с запасным вариантом.</summary>
            private static Vector3 Grounded(RigManager rig)
            {
                Vector3 p;
                try { p = rig.physicsRig != null ? rig.physicsRig.transform.position : rig.transform.position; }
                catch { p = rig.transform.position; }

                if (Physics.Raycast(p + Vector3.up * 0.3f, Vector3.down, out RaycastHit hit, 5f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    return hit.point;
                return p;
            }
        }
    }
}
