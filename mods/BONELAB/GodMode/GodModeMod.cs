using System;
using System.Reflection;
using BoneLib.BoneMenu;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(GodMode.GodModeMod), "God Mode", "1.0.0", "you")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace GodMode
{
    public class GodModeMod : MelonMod
    {
        /// <summary>Включён ли режим бога. Читается из Harmony-префиксов.</summary>
        public static bool Enabled { get; private set; }

        private const string PlayerHealthType = "Il2CppSLZ.Marrow.Player_Health";

        public override void OnInitializeMelon()
        {
            BuildMenu();
            ApplyPatches();
            MelonLogger.Msg("God Mode загружен. Переключатель — в BoneMenu.");
        }

        private void BuildMenu()
        {
            // Page.Root создаётся самим BoneLib до загрузки модов.
            Page page = Page.Root.CreatePage("God Mode", Color.red);
            page.CreateBool("Бессмертие", Color.green, Enabled, OnToggle);
        }

        private static void OnToggle(bool value)
        {
            Enabled = value;
            MelonLogger.Msg(value ? "God Mode: ВКЛ — ты бессмертен" : "God Mode: ВЫКЛ");
        }

        private void ApplyPatches()
        {
            Type health = AccessTools.TypeByName(PlayerHealthType);
            if (health == null)
            {
                MelonLogger.Error($"God Mode: тип {PlayerHealthType} не найден — патчи не применены");
                return;
            }

            var prefix = new HarmonyMethod(
                typeof(GodModeMod).GetMethod(nameof(BlockWhenEnabled),
                    BindingFlags.Static | BindingFlags.NonPublic));

            // Все пути урона/смерти игрока: обычный урон, мгновенное убийство, прямая смерть.
            foreach (string method in new[] { "TAKEDAMAGE", "ApplyKillDamage", "Death" })
                TryPatch(health, method, prefix);
        }

        private void TryPatch(Type type, string methodName, HarmonyMethod prefix)
        {
            try
            {
                MethodBase target = AccessTools.Method(type, methodName);
                if (target == null)
                {
                    MelonLogger.Warning($"God Mode: метод {methodName} не найден, пропускаю");
                    return;
                }
                HarmonyInstance.Patch(target, prefix: prefix);
                MelonLogger.Msg($"God Mode: пропатчен {methodName}");
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"God Mode: не удалось пропатчить {methodName} — {e.Message}");
            }
        }

        /// <summary>Harmony-префикс: false = оригинал не выполнится (урон/смерть отменяются).</summary>
        private static bool BlockWhenEnabled()
        {
            return !Enabled;
        }
    }
}
