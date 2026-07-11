using System;
using System.Reflection;
using BoneLib.BoneMenu;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(MonsterPanel.MonsterPanelMod), "MONSTER Panel", "1.2.0", "you")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace MonsterPanel
{
    public class MonsterPanelMod : MelonMod
    {
        /// <summary>Бессмертие игрока (читается god-префиксами Player_Health).</summary>
        public static bool Invincible { get; private set; }

        /// <summary>Монстер-урон: любой удар/выстрел мгновенно убивает врага.</summary>
        public static bool MonsterDamage { get; private set; }

        private const string PlayerHealthType = "Il2CppSLZ.Marrow.Player_Health";
        private const string HealthType = "Il2CppSLZ.Marrow.Health";
        private const string PuppetHealthType = "Il2CppSLZ.Marrow.PuppetMasta.SubBehaviourHealth";

        // Кэш методов «смерти» врага (вызываем рефлексией — надёжнее модификации урона).
        private static MethodInfo _puppetKill;   // SubBehaviourHealth.Kill()
        private static MethodInfo _healthDeath;  // Health.Death()

        // Защита от рекурсии, если Kill()/Death() внутри снова дёрнут урон.
        [ThreadStatic] private static bool _inKill;

        public override void OnInitializeMelon()
        {
            BuildMenu();
            ApplyPatches();
            MelonLogger.Msg("MONSTER Panel загружен.");
        }

        private void BuildMenu()
        {
            // Латиница: в шрифте BoneMenu (arlon) нет кириллицы.
            Page page = Page.Root.CreatePage("MONSTER Panel", Color.red);
            page.CreateBool("Invincible", Color.green, Invincible, v => { Invincible = v; Log("Invincible", v); });
            page.CreateBool("Monster Damage", new Color(1f, 0.4f, 0f), MonsterDamage,
                v => { MonsterDamage = v; Log("Monster Damage", v); });
        }

        private static void Log(string name, bool on) =>
            MelonLogger.Msg(on ? $"{name}: ВКЛ" : $"{name}: ВЫКЛ");

        private void ApplyPatches()
        {
            // --- Бессмертие: гасим все пути урона/смерти игрока ---
            Type playerHealth = AccessTools.TypeByName(PlayerHealthType);
            if (playerHealth == null)
                MelonLogger.Error($"MONSTER Panel: тип {PlayerHealthType} не найден — бессмертие не активно");
            else
            {
                var godPrefix = Hm(nameof(GodPrefix));
                foreach (string m in new[] { "TAKEDAMAGE", "ApplyKillDamage", "Death" })
                    TryPatch(playerHealth, m, godPrefix);
            }

            // --- Монстер-урон по гуманоидам (враги-болванчики = PuppetMaster) ---
            Type puppet = AccessTools.TypeByName(PuppetHealthType);
            if (puppet == null)
                MelonLogger.Error($"MONSTER Panel: тип {PuppetHealthType} не найден — монстер-урон по врагам не активен");
            else
            {
                _puppetKill = AccessTools.Method(puppet, "Kill");
                if (_puppetKill == null)
                    MelonLogger.Warning("MONSTER Panel: SubBehaviourHealth.Kill не найден");
                TryPatch(puppet, "TakeDamage", Hm(nameof(PuppetDamagePrefix)));
            }

            // --- Монстер-урон по прочему (ящики/объекты с Health) ---
            Type health = AccessTools.TypeByName(HealthType);
            if (health != null)
            {
                _healthDeath = AccessTools.Method(health, "Death");
                TryPatch(health, "TAKEDAMAGE", Hm(nameof(HealthDamagePrefix)));
            }
        }

        private static HarmonyMethod Hm(string name) =>
            new HarmonyMethod(typeof(MonsterPanelMod).GetMethod(name,
                BindingFlags.Static | BindingFlags.NonPublic));

        private void TryPatch(Type type, string methodName, HarmonyMethod prefix)
        {
            try
            {
                MethodBase target = AccessTools.Method(type, methodName);
                if (target == null)
                {
                    MelonLogger.Warning($"MONSTER Panel: метод {type.Name}.{methodName} не найден, пропускаю");
                    return;
                }
                HarmonyInstance.Patch(target, prefix: prefix);
                MelonLogger.Msg($"MONSTER Panel: пропатчен {type.Name}.{methodName}");
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"MONSTER Panel: не удалось пропатчить {type.Name}.{methodName} — {e.Message}");
            }
        }

        /// <summary>Бессмертие: false = оригинал (урон/смерть игрока) не выполнится.</summary>
        private static bool GodPrefix() => !Invincible;

        /// <summary>Монстер-урон по гуманоиду: мгновенно убиваем врага, оригинал (физика удара) при этом отрабатывает — враг отлетает.</summary>
        private static void PuppetDamagePrefix(object __instance)
        {
            if (!MonsterDamage || _inKill || _puppetKill == null || __instance == null) return;
            InvokeDeath(_puppetKill, __instance);
        }

        /// <summary>Монстер-урон по объекту с Health: убиваем через Death().</summary>
        private static void HealthDamagePrefix(object __instance)
        {
            if (!MonsterDamage || _inKill || _healthDeath == null || __instance == null) return;
            InvokeDeath(_healthDeath, __instance);
        }

        private static void InvokeDeath(MethodInfo method, object instance)
        {
            try
            {
                _inKill = true;
                method.Invoke(instance, null);
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"MONSTER Panel: {method.Name} упал — {e.Message}");
            }
            finally
            {
                _inKill = false;
            }
        }
    }
}
