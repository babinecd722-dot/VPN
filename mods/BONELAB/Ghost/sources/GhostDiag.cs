using System;
using BoneLib;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Combat;
using Il2CppSLZ.Marrow.Data;
using MelonLoader;
using UnityEngine;

namespace BePrime.Ghost;

/// <summary>
/// Диагностика тела игрока в духе Cyberpunk: реальный урон по зонам, причина
/// (пуля / удар / порез / ожог) и починка.
///
/// Данные НЕ выдуманные. BONELAB хранит здоровье по конечностям
/// (Health.cur_arm_lf/rt, cur_leg_lf/rt) плюс общий curr_Health/max_Health, а
/// Player_Health.OnReceivedDamage(attack, part) сообщает и точную часть тела,
/// и тип атаки. Мы это перехватываем, копим по шести зонам и умеем чинить:
/// вернуть HP конечности и стереть кровь (Health.ResetHits).
/// </summary>
public static class GhostDiag
{
    public enum Zone { Head, Torso, ArmL, ArmR, LegL, LegR }
    public const int ZoneCount = 6;

    public sealed class ZoneInfo
    {
        public float Ratio = 1f;       // 1 цел .. 0 уничтожен
        public AttackType LastCause;   // чем в последний раз задело
        public float LastDamage;       // сколько сняло последним ударом
        public float LastHitAt = -999f;// когда (Time.time)
        public int Hits;               // сколько раз задевало с последней починки
    }

    private static readonly ZoneInfo[] _zones = NewZones();
    private static bool _hooked;

    public static ZoneInfo Get(Zone z) => _zones[(int)z];

    private static ZoneInfo[] NewZones()
    {
        var a = new ZoneInfo[ZoneCount];
        for (int i = 0; i < a.Length; i++) a[i] = new ZoneInfo();
        return a;
    }

    // ─────────────── хук урона ───────────────

    public static void Install(HarmonyLib.Harmony h)
    {
        if (_hooked || h == null) return;
        try
        {
            var m = AccessTools.Method(typeof(Player_Health), nameof(Player_Health.OnReceivedDamage));
            if (m == null) { MelonLogger.Warning("Ghost diag: OnReceivedDamage не найден"); return; }
            h.Patch(m, postfix: new HarmonyMethod(typeof(GhostDiag), nameof(OnDamage)));
            _hooked = true;
            MelonLogger.Msg("Ghost diag: damage hook ready");
        }
        catch (Exception e) { MelonLogger.Warning("Ghost diag hook: " + e.Message); }
    }

    private static void OnDamage(Attack attack, PlayerDamageReceiver.BodyPart part)
    {
        try
        {
            Zone z = MapPart(part);
            var info = _zones[(int)z];
            info.LastCause = attack != null ? attack.attackType : AttackType.None;
            info.LastDamage = attack != null ? Mathf.Abs(attack.damage) : 0f;
            info.LastHitAt = Time.time;
            info.Hits++;
        }
        catch { }
    }

    private static Zone MapPart(PlayerDamageReceiver.BodyPart p)
    {
        switch (p)
        {
            case PlayerDamageReceiver.BodyPart.Head:
            case PlayerDamageReceiver.BodyPart.Neck:
                return Zone.Head;
            case PlayerDamageReceiver.BodyPart.ArmUpperLf:
            case PlayerDamageReceiver.BodyPart.ArmLowerLf:
            case PlayerDamageReceiver.BodyPart.HandLf:
                return Zone.ArmL;
            case PlayerDamageReceiver.BodyPart.ArmUpperRt:
            case PlayerDamageReceiver.BodyPart.ArmLowerRt:
            case PlayerDamageReceiver.BodyPart.HandRt:
                return Zone.ArmR;
            case PlayerDamageReceiver.BodyPart.LegUpperLf:
            case PlayerDamageReceiver.BodyPart.LegLowerLf:
            case PlayerDamageReceiver.BodyPart.FootLf:
                return Zone.LegL;
            case PlayerDamageReceiver.BodyPart.LegUpperRt:
            case PlayerDamageReceiver.BodyPart.LegLowerRt:
            case PlayerDamageReceiver.BodyPart.FootRt:
                return Zone.LegR;
            default:
                return Zone.Torso;   // Chest, Spine, Pelvis
        }
    }

    // ─────────────── чтение реального HP ───────────────

    private static Player_Health Ph()
    {
        try { return Player.RigManager?.health?.TryCast<Player_Health>(); }
        catch { return null; }
    }

    /// <summary>Обновляет Ratio каждой зоны из настоящих полей здоровья BONELAB.</summary>
    public static void Refresh()
    {
        try
        {
            var ph = Ph();
            if (ph == null) return;

            float max = ph.max_Health > 1e-3f ? ph.max_Health : 100f;
            float body = Mathf.Clamp01(ph.curr_Health / max);

            _zones[(int)Zone.Head].Ratio = body;
            _zones[(int)Zone.Torso].Ratio = body;
            _zones[(int)Zone.ArmL].Ratio = Mathf.Clamp01(ph.cur_arm_lf / max);
            _zones[(int)Zone.ArmR].Ratio = Mathf.Clamp01(ph.cur_arm_rt / max);
            _zones[(int)Zone.LegL].Ratio = Mathf.Clamp01(ph.cur_leg_lf / max);
            _zones[(int)Zone.LegR].Ratio = Mathf.Clamp01(ph.cur_leg_rt / max);
        }
        catch { }
    }

    public static float BodyPercent()
    {
        try
        {
            var ph = Ph();
            if (ph == null) return 1f;
            float max = ph.max_Health > 1e-3f ? ph.max_Health : 100f;
            return Mathf.Clamp01(ph.curr_Health / max);
        }
        catch { return 1f; }
    }

    // ─────────────── починка ───────────────

    /// <summary>Чинит зону: возвращает HP конечности/тела и стирает кровь.</summary>
    public static bool Heal(Zone z)
    {
        try
        {
            var ph = Ph();
            if (ph == null) return false;
            float max = ph.max_Health > 1e-3f ? ph.max_Health : 100f;

            switch (z)
            {
                case Zone.ArmL: ph.cur_arm_lf = max; break;
                case Zone.ArmR: ph.cur_arm_rt = max; break;
                case Zone.LegL: ph.cur_leg_lf = max; break;
                case Zone.LegR: ph.cur_leg_rt = max; break;
                default:                            // голова/торс — общий пул
                    ph.curr_Health = max;
                    break;
            }

            // Кровь и пробоины стираются целиком — точечно их снять API не даёт.
            try { ph.ResetHits(); } catch { }

            var info = _zones[(int)z];
            info.Hits = 0;
            info.LastDamage = 0f;
            info.LastHitAt = -999f;
            Refresh();
            return true;
        }
        catch (Exception e) { MelonLogger.Warning("Ghost heal: " + e.Message); return false; }
    }

    /// <summary>Полная починка всего тела.</summary>
    public static void HealAll()
    {
        for (int i = 0; i < ZoneCount; i++) Heal((Zone)i);
    }

    // ─────────────── текст для UI ───────────────

    public static string ZoneName(Zone z) => z switch
    {
        Zone.Head => "ГОЛОВА",
        Zone.Torso => "ТОРС",
        Zone.ArmL => "ЛЕВАЯ РУКА",
        Zone.ArmR => "ПРАВАЯ РУКА",
        Zone.LegL => "ЛЕВАЯ НОГА",
        Zone.LegR => "ПРАВАЯ НОГА",
        _ => "?"
    };

    public static string CauseName(AttackType t)
    {
        // Flags — берём самый характерный признак
        if ((t & AttackType.Piercing) != 0) return "Пулевое ранение";
        if ((t & AttackType.Stabbing) != 0) return "Колющий удар";
        if ((t & AttackType.Slicing) != 0) return "Резаная рана";
        if ((t & AttackType.Fire) != 0) return "Ожог";
        if ((t & AttackType.Blunt) != 0) return "Тупой удар";
        return "Повреждение";
    }
}
