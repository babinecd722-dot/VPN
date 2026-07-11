using System;
using System.Collections.Generic;
using System.Reflection;
using BoneLib.BoneMenu;
using HarmonyLib;
using Il2CppSLZ.Marrow.Combat;
using Il2CppSLZ.Marrow.PuppetMasta;
using MelonLoader;
using UnityEngine;
using MHealth = Il2CppSLZ.Marrow.Health;

[assembly: MelonInfo(typeof(MonsterPanel.MonsterPanelMod), "MONSTER Panel", "2.1.0", "you")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace MonsterPanel
{
    public class MonsterPanelMod : MelonMod
    {
        /// <summary>Бессмертие игрока.</summary>
        public static bool Invincible { get; private set; }

        /// <summary>Монстер-урон: ваншот врагов/объектов/игроков + жёсткий отброс.</summary>
        public static bool MonsterDamage { get; private set; }

        /// <summary>Танк: тебя не могут сдвинуть/поднять — риг игрока становится «тяжёлым».</summary>
        public static bool TankMode { get; private set; }

        // Сила отброса врага (VelocityChange, м/с — не зависит от массы тела).
        private const float LaunchSpeed = 28f;
        // Урон, который проставляем в сетевую атаку по игроку.
        private const float MaxDamage = 1_000_000f;
        // Во сколько раз утяжеляем риг в Tank Mode (экспериментально — можно крутить).
        private const float TankMassMultiplier = 40f;

        // Оригинальные массы тел рига (ключ — instanceID), чтобы вернуть при выключении.
        private static readonly Dictionary<int, float> _origMass = new();
        private static bool _tankApplied;

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
            if (TankMode) EnforceTank();
            else if (_tankApplied) RestoreTank();
        }

        /// <summary>Утяжеляем все тела физического рига игрока — другие не могут сдвинуть/поднять.</summary>
        private static void EnforceTank()
        {
            var rig = BoneLib.Player.PhysicsRig;
            if (rig == null) return;
            foreach (var rb in rig.GetComponentsInChildren<Rigidbody>())
            {
                if (rb == null) continue;
                int id = rb.GetInstanceID();
                if (!_origMass.ContainsKey(id)) _origMass[id] = rb.mass;
                float target = _origMass[id] * TankMassMultiplier;
                if (rb.mass != target) rb.mass = target;
            }
            _tankApplied = true;
        }

        /// <summary>Возвращаем оригинальные массы.</summary>
        private static void RestoreTank()
        {
            var rig = BoneLib.Player.PhysicsRig;
            if (rig != null)
            {
                foreach (var rb in rig.GetComponentsInChildren<Rigidbody>())
                {
                    if (rb == null) continue;
                    if (_origMass.TryGetValue(rb.GetInstanceID(), out float m))
                    {
                        try { rb.mass = m; } catch { }
                    }
                }
            }
            _origMass.Clear();
            _tankApplied = false;
        }

        private void BuildMenu()
        {
            Page page = Page.Root.CreatePage("MONSTER Panel", Color.red);
            page.CreateBool("Invincible", Color.green, Invincible, v => { Invincible = v; Log("Invincible", v); });
            page.CreateBool("Monster Damage", new Color(1f, 0.4f, 0f), MonsterDamage,
                v => { MonsterDamage = v; Log("Monster Damage", v); });
            page.CreateBool("Tank Mode", new Color(0.3f, 0.6f, 1f), TankMode,
                v => { TankMode = v; Log("Tank Mode", v); });
        }

        private static void Log(string name, bool on) =>
            MelonLogger.Msg(on ? $"{name}: ВКЛ" : $"{name}: ВЫКЛ");

        private void ApplyPatches()
        {
            // --- Бессмертие игрока ---
            Type playerHealth = AccessTools.TypeByName(PlayerHealthType);
            if (playerHealth != null)
            {
                var god = Hm(nameof(GodPrefix));
                foreach (string m in new[] { "TAKEDAMAGE", "ApplyKillDamage", "Death" })
                    TryPatch(playerHealth, m, god);
            }
            else MelonLogger.Error("MONSTER Panel: Player_Health не найден — бессмертие не активно");

            // --- Враги-гуманоиды (PuppetMaster): ваншот + отброс ---
            TryPatchTyped(typeof(SubBehaviourHealth), "TakeDamage", Hm(nameof(PuppetPrefix)), "SubBehaviourHealth.TakeDamage");

            // --- Объекты/ящики с Health: ваншот ---
            TryPatchTyped(typeof(MHealth), "TAKEDAMAGE", Hm(nameof(HealthPrefix)), "Health.TAKEDAMAGE");

            // --- Игроки в Fusion: бустим сетевой урон (если Fusion установлен) ---
            Type fusion = AccessTools.TypeByName(FusionReceiverPatch);
            if (fusion != null)
                TryPatch(fusion, "ReceiveAttack", Hm(nameof(FusionAttackPrefix)));
            else
                MelonLogger.Msg("MONSTER Panel: LabFusion не найден — урон по игрокам в сети выключен (это нормально без Fusion).");
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

        /// <summary>Бессмертие: false = урон/смерть игрока не выполнится.</summary>
        private static bool GodPrefix() => !Invincible;

        /// <summary>Враг-гуманоид: мгновенно убиваем и отбрасываем в направлении удара.</summary>
        private static void PuppetPrefix(SubBehaviourHealth __instance, Attack attack)
        {
            if (!MonsterDamage || _reentry || __instance == null) return;
            try
            {
                _reentry = true;
                __instance.Kill();          // распинывает куклу → ragdoll
                Launch(attack);
            }
            catch (Exception e) { MelonLogger.Warning("MONSTER Panel: puppet kill — " + e.Message); }
            finally { _reentry = false; }
        }

        /// <summary>Объект с Health: уничтожаем.</summary>
        private static void HealthPrefix(MHealth __instance)
        {
            if (!MonsterDamage || _reentry || __instance == null) return;
            try { _reentry = true; __instance.Death(); }
            catch (Exception e) { MelonLogger.Warning("MONSTER Panel: health death — " + e.Message); }
            finally { _reentry = false; }
        }

        /// <summary>Fusion: подменяем урон сетевой атаки по игроку на максимум (attack по ссылке).</summary>
        private static void FusionAttackPrefix(ref Attack attack)
        {
            if (MonsterDamage)
                attack.damage = MaxDamage;
        }

        /// <summary>Жёсткий отброс: раскидываем весь риг задетого тела в направлении удара.</summary>
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
            {
                if (rb != null)
                {
                    try { rb.velocity = v; } catch { }
                }
            }
        }
    }
}
