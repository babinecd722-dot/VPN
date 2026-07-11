using System;
using System.Reflection;
using BoneLib.BoneMenu;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(MonsterPanel.MonsterPanelMod), "MONSTER Panel", "1.1.0", "you")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace MonsterPanel
{
    public class MonsterPanelMod : MelonMod
    {
        /// <summary>Бессмертие игрока (читается god-префиксами Player_Health).</summary>
        public static bool Invincible { get; private set; }

        /// <summary>Монстер-урон: любой наносимый урон = максимум (читается Health.TAKEDAMAGE-префиксом).</summary>
        public static bool MonsterDamage { get; private set; }

        // Урон, который ставим при включённом монстер-режиме. Большой, но без overflow/NaN.
        private const float MaxDamage = 1_000_000f;

        private const string PlayerHealthType = "Il2CppSLZ.Marrow.Player_Health";
        private const string HealthType = "Il2CppSLZ.Marrow.Health";

        public override void OnInitializeMelon()
        {
            BuildMenu();
            ApplyPatches();
            MelonLogger.Msg("MONSTER Panel загружен.");
        }

        private void BuildMenu()
        {
            // Page.Root создаётся самим BoneLib до загрузки модов. Латиница — в шрифте
            // BoneMenu (arlon) нет кириллицы, русские буквы превратились бы в кракозябры.
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
                var godPrefix = Method(nameof(GodPrefix));
                foreach (string m in new[] { "TAKEDAMAGE", "ApplyKillDamage", "Death" })
                    TryPatch(playerHealth, m, godPrefix);
            }

            // --- Монстер-урон: любой урон по врагам (удары + стрельба) = максимум ---
            Type health = AccessTools.TypeByName(HealthType);
            if (health == null)
                MelonLogger.Error($"MONSTER Panel: тип {HealthType} не найден — монстер-урон не активен");
            else
                TryPatch(health, "TAKEDAMAGE", Method(nameof(MonsterDamagePrefix)));
        }

        private static HarmonyMethod Method(string name) =>
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

        /// <summary>Монстер-урон: подменяем наносимый врагу урон на максимум.</summary>
        private static void MonsterDamagePrefix(ref float damage)
        {
            if (MonsterDamage)
                damage = MaxDamage;
        }
    }
}
