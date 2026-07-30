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

    /// <summary>Одно попадание для детального вида части тела.</summary>
    public struct Mark
    {
        public PlayerDamageReceiver.BodyPart Part; // точная под-часть (кисть/предплечье/…)
        public Vector2 Dir;                        // откуда прилетело, в 2D экрана
        public AttackType Type;                    // пуля / удар / порез / ожог
        public float Damage;
        public float At;
    }

    public sealed class ZoneInfo
    {
        public float Ratio = 1f;       // 1 цел .. 0 уничтожен
        public AttackType LastCause;   // чем в последний раз задело
        public float LastDamage;       // сколько сняло последним ударом
        public float LastHitAt = -999f;// когда (Time.time)
        public int Hits;               // сколько раз задевало с последней починки
        public readonly System.Collections.Generic.List<Mark> Marks =
            new System.Collections.Generic.List<Mark>();
        public const int MaxMarks = 6;
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
            var type = attack != null ? attack.attackType : AttackType.None;
            float dmg = attack != null ? Mathf.Abs(attack.damage) : 0f;

            info.LastCause = type;
            info.LastDamage = dmg;
            info.LastHitAt = Time.time;
            info.Hits++;

            info.Marks.Add(new Mark
            {
                Part = part,
                Dir = attack != null ? Project2D(attack.direction) : new Vector2(0f, 1f),
                Type = type,
                Damage = dmg,
                At = Time.time,
            });
            while (info.Marks.Count > ZoneInfo.MaxMarks) info.Marks.RemoveAt(0);
        }
        catch { }
    }

    /// <summary>
    /// Проецирует мировое направление удара в 2D панели: x — вбок относительно
    /// взгляда игрока, y — вверх. Так «красная траектория» показывает реальный
    /// угол, откуда прилетело.
    /// </summary>
    private static Vector2 Project2D(Vector3 worldDir)
    {
        try
        {
            var head = Player.Head;
            Vector3 right = head != null ? head.right : Vector3.right;
            Vector3 up = head != null ? head.up : Vector3.up;
            Vector2 v = new Vector2(Vector3.Dot(worldDir, right), Vector3.Dot(worldDir, up));
            if (v.sqrMagnitude < 1e-6f) return new Vector2(0f, 1f);
            return v.normalized;
        }
        catch { return new Vector2(0f, 1f); }
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
            info.Marks.Clear();
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

    /// <summary>Позиция вдоль части: 0 — верх (плечо/бедро), 1 — низ (кисть/стопа).</summary>
    public static float AlongAxis(PlayerDamageReceiver.BodyPart p)
    {
        switch (p)
        {
            case PlayerDamageReceiver.BodyPart.Head: return 0.15f;
            case PlayerDamageReceiver.BodyPart.Neck: return 0.30f;
            case PlayerDamageReceiver.BodyPart.Chest: return 0.30f;
            case PlayerDamageReceiver.BodyPart.Spine: return 0.55f;
            case PlayerDamageReceiver.BodyPart.Pelvis: return 0.80f;
            case PlayerDamageReceiver.BodyPart.ArmUpperLf:
            case PlayerDamageReceiver.BodyPart.ArmUpperRt: return 0.20f;
            case PlayerDamageReceiver.BodyPart.ArmLowerLf:
            case PlayerDamageReceiver.BodyPart.ArmLowerRt: return 0.55f;
            case PlayerDamageReceiver.BodyPart.HandLf:
            case PlayerDamageReceiver.BodyPart.HandRt: return 0.90f;
            case PlayerDamageReceiver.BodyPart.LegUpperLf:
            case PlayerDamageReceiver.BodyPart.LegUpperRt: return 0.20f;
            case PlayerDamageReceiver.BodyPart.LegLowerLf:
            case PlayerDamageReceiver.BodyPart.LegLowerRt: return 0.55f;
            case PlayerDamageReceiver.BodyPart.FootLf:
            case PlayerDamageReceiver.BodyPart.FootRt: return 0.92f;
            default: return 0.5f;
        }
    }

    public static string PartName(PlayerDamageReceiver.BodyPart p) => p switch
    {
        PlayerDamageReceiver.BodyPart.Head => "Голова",
        PlayerDamageReceiver.BodyPart.Neck => "Шея",
        PlayerDamageReceiver.BodyPart.Chest => "Грудь",
        PlayerDamageReceiver.BodyPart.Spine => "Живот",
        PlayerDamageReceiver.BodyPart.Pelvis => "Таз",
        PlayerDamageReceiver.BodyPart.ArmUpperLf => "Левое плечо",
        PlayerDamageReceiver.BodyPart.ArmLowerLf => "Левое предплечье",
        PlayerDamageReceiver.BodyPart.HandLf => "Левая кисть",
        PlayerDamageReceiver.BodyPart.ArmUpperRt => "Правое плечо",
        PlayerDamageReceiver.BodyPart.ArmLowerRt => "Правое предплечье",
        PlayerDamageReceiver.BodyPart.HandRt => "Правая кисть",
        PlayerDamageReceiver.BodyPart.LegUpperLf => "Левое бедро",
        PlayerDamageReceiver.BodyPart.LegLowerLf => "Левая голень",
        PlayerDamageReceiver.BodyPart.FootLf => "Левая стопа",
        PlayerDamageReceiver.BodyPart.LegUpperRt => "Правое бедро",
        PlayerDamageReceiver.BodyPart.LegLowerRt => "Правая голень",
        PlayerDamageReceiver.BodyPart.FootRt => "Правая стопа",
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
