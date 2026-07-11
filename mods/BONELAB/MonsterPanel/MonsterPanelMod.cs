using System;
using System.Reflection;
using BoneLib.BoneMenu;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Combat;
using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.PuppetMasta;
using MelonLoader;
using UnityEngine;
using MHealth = Il2CppSLZ.Marrow.Health;

[assembly: MelonInfo(typeof(MonsterPanel.MonsterPanelMod), "MONSTER Panel", "2.4.0", "you")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace MonsterPanel
{
    public class MonsterPanelMod : MelonMod
    {
        /// <summary>Бессмертие игрока.</summary>
        public static bool Invincible { get; private set; }

        /// <summary>Монстер-урон: ваншот врагов/объектов/игроков + жёсткий отброс.</summary>
        public static bool MonsterDamage { get; private set; }

        /// <summary>Remote Kill: наводишь рукой на игрока (зелёный маркер) + grip → максимальный урон по сети.</summary>
        public static bool RemoteKill { get; private set; }

        private const float LaunchSpeed = 28f;
        private const float MaxDamage = 1_000_000f;

        // Remote Kill
        private const float RkRange = 40f;          // дальность наведения
        private const float RkGripThreshold = 0.6f;
        private static GameObject _leftMarker, _rightMarker;
        private static bool _leftGripPrev, _rightGripPrev;

        private const string PlayerHealthType = "Il2CppSLZ.Marrow.Player_Health";
        private const string FusionReceiverPatch = "LabFusion.Patching.PlayerDamageReceiverPatches";

        [ThreadStatic] private static bool _reentry;

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
                AimHand(BoneLib.Player.LeftHand?.transform, BoneLib.Player.LeftController, ref _leftMarker, ref _leftGripPrev);
                AimHand(BoneLib.Player.RightHand?.transform, BoneLib.Player.RightController, ref _rightMarker, ref _rightGripPrev);
            }
            else
            {
                HideMarker(_leftMarker); HideMarker(_rightMarker);
                _leftGripPrev = _rightGripPrev = false;
            }
        }

        // ---------------- Remote Kill ----------------

        private static void AimHand(Transform hand, BaseController controller, ref GameObject marker, ref bool gripPrev)
        {
            if (hand == null || controller == null) { HideMarker(marker); gripPrev = false; return; }

            PlayerDamageReceiver target = FindTargetPlayer(hand);

            if (target == null)
            {
                HideMarker(marker);
                gripPrev = controller.GetGripForce() > RkGripThreshold; // не «стреляем» при появлении цели во время зажатого grip
                return;
            }

            // Боевой крестик на цели (зелёный = можно убить), всегда развёрнут к лицу.
            if (marker == null) marker = CreateCrosshair();
            marker.SetActive(true);
            marker.transform.position = target.transform.position + Vector3.up * 0.25f;
            var head = BoneLib.Player.Head;
            if (head != null)
            {
                Vector3 away = marker.transform.position - head.position;
                if (away.sqrMagnitude > 0.0001f)
                    marker.transform.rotation = Quaternion.LookRotation(away, Vector3.up);
            }

            bool grip = controller.GetGripForce() > RkGripThreshold;
            if (grip && !gripPrev) KillPlayer(target, hand);   // срабатывание по нажатию, не по удержанию
            gripPrev = grip;
        }

        /// <summary>Луч из руки → ближайший игрок (PlayerDamageReceiver), не считая себя.</summary>
        private static PlayerDamageReceiver FindTargetPlayer(Transform hand)
        {
            Vector3 origin = hand.position + hand.forward * 0.3f;
            var hits = Physics.RaycastAll(origin, hand.forward, RkRange,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            PlayerDamageReceiver best = null;
            float bestDist = float.MaxValue;
            foreach (var h in hits)
            {
                if (h.collider == null) continue;
                var recv = h.collider.GetComponentInParent<PlayerDamageReceiver>();
                if (recv == null) continue;
                if (IsOwnRig(recv.transform)) continue;         // не наводимся на себя
                if (h.distance < bestDist) { bestDist = h.distance; best = recv; }
            }
            return best;
        }

        /// <summary>Строим макс-атаку и отдаём в приёмник урона игрока — LabFusion доставит её цели по сети.</summary>
        private static void KillPlayer(PlayerDamageReceiver target, Transform hand)
        {
            try
            {
                var attack = new Attack
                {
                    damage = MaxDamage,
                    attackType = AttackType.Blunt,
                    direction = (target.transform.position - hand.position).normalized,
                    origin = hand.position,
                };
                target.ReceiveAttack(attack);
                MelonLogger.Msg("Remote Kill: урон отправлен игроку.");
            }
            catch (Exception e) { MelonLogger.Warning("Remote Kill: " + e.Message); }
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

        private static void Bar(Transform parent, Vector3 localPos, Vector3 scale, Color c)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = g.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);  // не мешает лучу/физике
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = scale;
            var r = g.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = c;
                try { r.material.EnableKeyword("_EMISSION"); r.material.SetColor("_EmissionColor", c * 2.2f); } catch { }
            }
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
            if (MonsterDamage) attack.damage = MaxDamage;
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
    }
}
