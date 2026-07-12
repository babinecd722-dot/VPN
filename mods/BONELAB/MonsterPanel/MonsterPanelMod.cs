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

[assembly: MelonInfo(typeof(MonsterPanel.MonsterPanelMod), "MONSTER Panel", "2.14.0", "you")]
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

        /// <summary>Бесконечные патроны: запас в инвентаре бесконечный (магазин расходуется штатно).</summary>
        public static bool InfiniteAmmo { get; private set; }

        /// <summary>Tank: тебя нельзя схватить/поднять (движение и удары как обычно).</summary>
        public static bool TankMode { get; private set; }

        /// <summary>Disarm: наводишь руку на игрока + кнопка A (одно нажатие) → его оружие вырывает силой из рук.</summary>
        public static bool Disarm { get; private set; }

        private const float LaunchSpeed = 28f;
        private const float MaxDamage = 1_000_000f;

        // Tank Mode
        private const float TankReapplyInterval = 0.5f;
        private static bool _tankApplied;
        private static float _tankTimer;

        // Disarm
        private const float DisarmRadius = 1.3f;   // радиус вокруг цели, откуда вырываем предметы
        private const float DisarmSpeed = 22f;     // сила вырывания

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
            MelonLogger.Msg("MONSTER Panel loaded.");
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

            TankUpdate();

            if (Disarm)
            {
                DisarmHand(BoneLib.Player.LeftHand?.transform, BoneLib.Player.LeftController);
                DisarmHand(BoneLib.Player.RightHand?.transform, BoneLib.Player.RightController);
            }
        }

        // ---------------- Remote Kill ----------------

        private static void AimHand(Transform hand, BaseController controller, HandState state)
        {
            if (hand == null || controller == null) { HideMarker(state.Marker); state.FirePrev = false; return; }

            // Кнопка «выстрела»: срабатывает и на grip (средний палец), и на триггер (указательный) —
            // что нажмёшь, то и сработает. Значения пишем в лог, чтобы точно видеть, какая кнопка идёт.
            float gripF = SafeGrip(controller);
            float trigF = SafeTrigger(controller);
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
                    MelonLogger.Msg($"Remote Kill: button pressed (grip={gripF:0.00} trig={trigF:0.00}), but no crosshair/target - aim your hand at a player.");
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
                MelonLogger.Msg($"Remote Kill: FIRE (grip={gripF:0.00} trig={trigF:0.00}) -> target {target.gameObject.name}.");
                KillPlayer(target, hand, hit);
            }
            state.FirePrev = fire;
        }

        // Читаем оси контроллера без аллокаций (без лямбд — это горячий путь каждый кадр).
        private static float SafeGrip(BaseController c) { try { return c.GetGripForce(); } catch { return 0f; } }
        private static float SafeTrigger(BaseController c) { try { return c.GetIndexCurlAxis(); } catch { return 0f; } }

        // Переиспользуемый буфер для SphereCastNonAlloc — не мусорим массивами каждый кадр.
        private static readonly RaycastHit[] _castBuf = new RaycastHit[32];

        /// <summary>«Толстый» луч (SphereCast) из руки → ближайший игрок, не считая себя.
        /// Ключевое: цепляемся за РИГ игрока по любому его коллайдеру (в т.ч. триггер-хитбоксу),
        /// а PlayerDamageReceiver берём С РИГА — он может висеть не на том коллайдере, куда попал луч.</summary>
        private static PlayerDamageReceiver FindTargetPlayer(Transform hand, out RaycastHit bestHit)
        {
            bestHit = default;
            Vector3 origin = hand.position + hand.forward * 0.3f;
            int count = Physics.SphereCastNonAlloc(origin, RkAimRadius, hand.forward, _castBuf, RkRange,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);   // триггеры-хитбоксы тоже ловим
            PlayerDamageReceiver best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit h = _castBuf[i];
                if (h.collider == null) continue;

                var rig = h.collider.GetComponentInParent<RigManager>();
                if (rig == null) continue;                       // не риг игрока — мимо
                if (IsOwnRig(rig.transform)) continue;           // не наводимся на себя

                var recv = h.collider.GetComponentInParent<PlayerDamageReceiver>();
                if (recv == null) recv = rig.GetComponentInChildren<PlayerDamageReceiver>();
                if (recv == null) continue;

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

                MelonLogger.Msg($"Remote Kill: {sent} attacks sent to {target.transform.root?.name ?? target.gameObject.name}.");
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
            page.CreateBool("Tank Mode", new Color(0.4f, 0.6f, 0.9f), TankMode,
                v => { TankMode = v; Log("Tank Mode", v); });
            page.CreateBool("Disarm", new Color(0.9f, 0.2f, 0.5f), Disarm,
                v => { Disarm = v; Log("Disarm", v); });

            // Teleport + ник: только когда загружен LabFusion.
            if (Teleporter.FusionLoaded)
            {
                NickHider.Install(page);
                Teleporter.Install(page);
                MelonLogger.Msg("MONSTER Panel: Teleport + Nickname sections added (LabFusion found).");
            }
            else
                MelonLogger.Msg("MONSTER Panel: LabFusion not loaded - Teleport/Nickname hidden.");
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

        // ---------------- Disarm ----------------
        //
        // Наводишь руку на игрока и жмёшь кнопку A (одно нажатие) → вырываем силой все предметы
        // (стволы) рядом с ним. Это физика, а не урон — чужое бессмертие не мешает. Тела самих
        // игроков (риги) не трогаем, только отдельные предметы.
        private static void DisarmHand(Transform hand, BaseController controller)
        {
            if (hand == null || controller == null) return;
            bool aDown;
            try { aDown = controller.GetAButtonDown(); } catch { aDown = false; }
            if (!aDown) return;   // GetAButtonDown уже edge-триггер: срабатывает один раз на нажатие

            var target = FindTargetPlayer(hand, out _);
            if (target != null) YankItemsFrom(target);
            else MelonLogger.Msg("Disarm: A pressed, no target - point your hand at a player.");
        }

        private static readonly Collider[] _overlapBuf = new Collider[64];
        private static readonly System.Collections.Generic.HashSet<int> _yankSeen = new System.Collections.Generic.HashSet<int>();

        private static void YankItemsFrom(PlayerDamageReceiver target)
        {
            try
            {
                Vector3 center = target.transform.position;
                int count = Physics.OverlapSphereNonAlloc(center, DisarmRadius, _overlapBuf,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                _yankSeen.Clear();
                int n = 0;
                for (int i = 0; i < count; i++)
                {
                    var col = _overlapBuf[i];
                    if (col == null) continue;
                    var rb = col.attachedRigidbody;
                    if (rb == null) continue;
                    if (rb.transform.root != null && rb.transform.root.GetComponentInParent<RigManager>() != null)
                        continue;                                   // это тело игрока — не трогаем
                    if (!_yankSeen.Add(rb.GetInstanceID())) continue;   // каждое тело один раз
                    Vector3 dir = rb.position - center; dir.y += 0.4f;
                    if (dir.sqrMagnitude < 0.0001f) dir = Vector3.up;
                    try { rb.velocity = dir.normalized * DisarmSpeed; n++; } catch { }
                }
                MelonLogger.Msg($"Disarm: ripped {n} items from {target.gameObject.name}.");
            }
            catch (Exception e) { MelonLogger.Warning("Disarm: " + e.Message); }
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
                try
                {
                    if (value != null && value.Length > 32)   // страховка под лимит имени LabFusion
                        value = value.Substring(0, 32);
                    // Применяем ник через настройки LabFusion — он раскидывает его ВЕЗДЕ:
                    // nametag, меню Fusion, и синхронизирует другим игрокам.
                    LabFusion.Preferences.Client.ClientSettings.Nickname.Value = value;
                    LabFusion.Preferences.Client.ClientSettings.NicknameVisibility.Value = LabFusion.Senders.NicknameVisibility.SHOW;
                    SendSettings();

                    string shown = StripTags(value);
                    Notify("Nickname changed", string.IsNullOrWhiteSpace(shown) ? "(empty)" : shown);
                    MelonLogger.Msg($"Nickname: set '{value}' via ClientSettings (synced everywhere).");
                }
                catch (Exception e) { MelonLogger.Warning("Nickname set: " + e.Message); }
            }

            private static void ResetNick()
            {
                try
                {
                    LabFusion.Preferences.Client.ClientSettings.Nickname.Value = "";   // пусто → откат на платформенный ник
                    SendSettings();
                    Notify("Nickname reset", "default");
                    MelonLogger.Msg("Nickname: reset to default.");
                }
                catch (Exception e) { MelonLogger.Warning("Nickname reset: " + e.Message); }
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
