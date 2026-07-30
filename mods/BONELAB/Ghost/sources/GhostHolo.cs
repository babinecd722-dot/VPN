using System;
using System.Collections.Generic;
using BoneLib;
using Il2CppSLZ.Marrow;
using LabFusion.Player;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BePrime.Ghost;

/// <summary>
/// Cyberpunk-style yellow hologram floating on the LEFT FOREARM.
/// Poke with right index fingertip.
/// </summary>
public static class GhostHolo
{
    private enum Tab { Nick, Lobby, Custom }

    // Landscape holo plate (~19cm x ~7cm)
    private const float CanvasW = 480f;
    private const float CanvasH = 168f;
    private const float WorldScale = 0.00040f;

    private static GameObject _root;
    private static RectTransform _canvasRt;
    private static RectTransform _panel;
    private static RectTransform _content;
    private static RectTransform _toastRt;
    private static Image _toastBg;
    private static Image _scanSweep;
    private static Text _toastTitle;
    private static Text _toastBody;
    private static Text _headerSub;
    private static Font _font;

    private static readonly List<HoloBtn> _buttons = new List<HoloBtn>();
    private static Tab _tab = Tab.Nick;
    private static string _keypad = "";
    private static int _playerPage;
    private static bool _rebuildQueued;
    private static Tab _queuedTab;

    private static int _hoverIndex = -1;
    private static int _insideIndex = -1;
    private static float _clickLockUntil;
    private static bool _touchDown;
    private const float ClickCooldown = 0.28f;

    // Всё в мировых метрах: расстояние от кончика пальца до поверхности кнопки.
    private const float TouchThickness = 0.016f; // толщина коллайдера кнопки
    private const float HoverDist = 0.040f;      // ближе этого — подсветка
    private const float TouchEnter = 0.018f;     // ближе этого — нажатие
    private const float TouchExit = 0.030f;      // дальше этого — отпустил

    private static float _toastT = 1f;
    private static float _toastHoldUntil;
    private static string _toastTitleStr = "";
    private static string _toastBodyStr = "";
    private const float ToastIn = 0.12f;
    private const float ToastHold = 1.7f;
    private const float ToastOut = 0.2f;

    private static float _appearT = 1f;
    private static float _baseScale = WorldScale;
    private static float _scanT;

    // CP2077-ish yellow holo glass
    private static readonly Color Glass = new Color(0.18f, 0.14f, 0.02f, 0.42f);
    private static readonly Color GlassDeep = new Color(0.10f, 0.08f, 0.01f, 0.55f);
    private static readonly Color Frame = new Color(1f, 0.90f, 0.12f, 0.55f);
    private static readonly Color Yellow = new Color(1f, 0.91f, 0.14f, 0.95f);
    private static readonly Color YellowSoft = new Color(1f, 0.86f, 0.20f, 0.55f);
    private static readonly Color YellowDim = new Color(0.70f, 0.55f, 0.08f, 0.35f);
    private static readonly Color YellowHot = new Color(1f, 0.96f, 0.55f, 0.85f);
    private static readonly Color RowIdle = new Color(1f, 0.88f, 0.15f, 0.10f);
    private static readonly Color RowHover = new Color(1f, 0.90f, 0.20f, 0.28f);
    private static readonly Color RowActive = new Color(1f, 0.85f, 0.10f, 0.38f);
    private static readonly Color TextCol = new Color(1f, 0.94f, 0.55f, 0.92f);
    private static readonly Color TextDim = new Color(0.85f, 0.72f, 0.25f, 0.70f);
    private static readonly Color Danger = new Color(1f, 0.32f, 0.18f, 0.55f);
    private static readonly Color DangerText = new Color(1f, 0.55f, 0.40f, 0.95f);
    private static readonly Color ToastBg = new Color(0.12f, 0.10f, 0.02f, 0.72f);
    private static readonly Color Scan = new Color(1f, 0.92f, 0.20f, 0.07f);

    private sealed class HoloBtn
    {
        public RectTransform Rt;
        public Image Bg;
        public Image Accent;
        public Text Label;
        public Action OnClick;
        public bool DangerStyle;
        public bool AccentStyle;
        public float PressAnim;
        public float HoverBlend;
        public Vector3 BaseScale;
        public Collider Col;      // реальный коллайдер кнопки — и стоп пальцу, и детект тапа
    }

    public static void Tick()
    {
        if (!GhostMod.Enabled)
        {
            Destroy();
            return;
        }
        if (!GhostMod.FusionLoaded) return;

        Ensure();
        if (_root == null) return;

        if (_rebuildQueued)
        {
            _rebuildQueued = false;
            Rebuild(_queuedTab);
        }

        float dt = Time.unscaledDeltaTime;
        AttachToLeftForearm();
        AnimateAppear(dt);
        AnimateScan(dt);
        AnimateButtons(dt);
        AnimateToast(dt);
        HandleTouch();
    }

    public static void Notify(string title, string body)
    {
        _toastTitleStr = string.IsNullOrEmpty(title) ? "GHOST" : title.ToUpperInvariant();
        _toastBodyStr = body ?? "";
        _toastT = 0f;
        _toastHoldUntil = Time.unscaledTime + ToastHold;
        ApplyToastVisual();
    }

    public static void ShowToast(string msg) => Notify("GHOST", msg);

    public static void Destroy()
    {
        _buttons.Clear();
        _hoverIndex = -1;
        _insideIndex = -1;
        // без сброса плата при повторном показе стартовала со старым
        // направлением и первые кадры «доезжала» на место
        _touchDown = false;
        _outInit = false;
        if (_root != null)
        {
            Object.Destroy(_root);
            _root = null;
            _canvasRt = null;
            _panel = null;
            _content = null;
            _toastRt = null;
            _toastBg = null;
            _scanSweep = null;
            _toastTitle = null;
            _toastBody = null;
            _headerSub = null;
        }
        _appearT = 1f;
    }

    private static void Ensure()
    {
        if (_root != null) return;
        try
        {
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _root = new GameObject("BE_PRIME_GHOST_HOLO");
            Object.DontDestroyOnLoad(_root);

            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 90;
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 16f;
            _root.AddComponent<GraphicRaycaster>();

            _canvasRt = _root.GetComponent<RectTransform>();
            _canvasRt.sizeDelta = new Vector2(CanvasW, CanvasH);
            _baseScale = WorldScale;
            _root.transform.localScale = Vector3.one * _baseScale;

            // Кинематический Rigidbody: панель двигается трансформом каждый кадр,
            // и без RB её коллайдеры считались бы статикой — двигать статику
            // дорого и неправильно. С кинематическим RB это корректный «движущийся
            // стенд», об который физическая рука упирается.
            var rb = _root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Soft outer glow frame
            var glow = MakeImage(_root.transform, "Glow", new Color(1f, 0.85f, 0.1f, 0.12f));
            Stretch(glow.rectTransform);
            glow.rectTransform.offsetMin = new Vector2(-6f, -6f);
            glow.rectTransform.offsetMax = new Vector2(6f, 6f);

            // Thin neon border
            var border = MakeImage(_root.transform, "Border", Frame);
            Stretch(border.rectTransform);

            // Glass plate
            _panel = MakeImage(_root.transform, "Glass", Glass).rectTransform;
            Stretch(_panel);
            Inset(_panel, 2f);

            // Inner wash
            var wash = MakeImage(_panel, "Wash", GlassDeep);
            Stretch(wash.rectTransform);
            Inset(wash.rectTransform, 1f);

            // Scanlines (static bands)
            BuildScanlines(_panel);

            // Moving sweep
            _scanSweep = MakeImage(_panel, "Sweep", Scan);
            SetAnchors(_scanSweep.rectTransform, 0f, 0f, 1f, 0f);
            _scanSweep.rectTransform.pivot = new Vector2(0.5f, 0f);
            _scanSweep.rectTransform.sizeDelta = new Vector2(0f, 18f);
            _scanSweep.rectTransform.anchoredPosition = Vector2.zero;

            // Corner brackets (CP chrome)
            AddCorner(_panel, "TL", true, true);
            AddCorner(_panel, "TR", false, true);
            AddCorner(_panel, "BL", true, false);
            AddCorner(_panel, "BR", false, false);

            BuildHeader(_panel);
            BuildTabsBar(_panel);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(_panel, false);
            _content = contentGo.AddComponent<RectTransform>();
            SetAnchors(_content, 0f, 0f, 1f, 1f);
            _content.offsetMin = new Vector2(10f, 10f);
            _content.offsetMax = new Vector2(-10f, -52f);

            BuildToast();
            _appearT = 0f;
            Rebuild(_tab);   // внутри навешиваются коллайдеры (кнопки + плита)
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"Ghost holo build: {ex}");
            Destroy();
        }
    }

    private static void BuildHeader(RectTransform parent)
    {
        var top = MakeImage(parent, "Header", new Color(1f, 0.85f, 0.1f, 0.08f)).rectTransform;
        SetAnchors(top, 0f, 1f, 1f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.sizeDelta = new Vector2(0f, 28f);
        top.anchoredPosition = Vector2.zero;
        InsetX(top, 8f);

        var mark = MakeImage(top, "Mark", Yellow).rectTransform;
        SetAnchors(mark, 0f, 0.2f, 0f, 0.8f);
        mark.pivot = new Vector2(0f, 0.5f);
        mark.sizeDelta = new Vector2(3f, 0f);
        mark.anchoredPosition = new Vector2(6f, 0f);

        var title = MakeText(top, "Title", "GHOST  //  NETRUNNER", 13, Yellow, TextAnchor.MiddleLeft);
        SetAnchors(title.rectTransform, 0f, 0f, 0.62f, 1f);
        title.rectTransform.offsetMin = new Vector2(14f, 0f);
        title.rectTransform.offsetMax = new Vector2(0f, 0f);
        title.fontStyle = FontStyle.Bold;

        _headerSub = MakeText(top, "Sub", "LINK ACTIVE", 10, TextDim, TextAnchor.MiddleRight);
        SetAnchors(_headerSub.rectTransform, 0.55f, 0f, 1f, 1f);
        _headerSub.rectTransform.offsetMin = new Vector2(0f, 0f);
        _headerSub.rectTransform.offsetMax = new Vector2(-10f, 0f);

        var line = MakeImage(parent, "HeaderLine", YellowSoft).rectTransform;
        SetAnchors(line, 0f, 1f, 1f, 1f);
        line.pivot = new Vector2(0.5f, 1f);
        line.sizeDelta = new Vector2(0f, 1.2f);
        line.anchoredPosition = new Vector2(0f, -28f);
        InsetX(line, 10f);
    }

    private static void BuildTabsBar(RectTransform parent)
    {
        var tabs = MakeImage(parent, "Tabs", new Color(1f, 0.85f, 0.1f, 0.06f)).rectTransform;
        SetAnchors(tabs, 0f, 1f, 1f, 1f);
        tabs.pivot = new Vector2(0.5f, 1f);
        tabs.sizeDelta = new Vector2(0f, 22f);
        tabs.anchoredPosition = new Vector2(0f, -30f);
        InsetX(tabs, 8f);
    }

    private static void BuildScanlines(RectTransform parent)
    {
        var host = new GameObject("Scanlines");
        host.transform.SetParent(parent, false);
        var rt = host.AddComponent<RectTransform>();
        Stretch(rt);
        for (int i = 0; i < 14; i++)
        {
            float y = 1f - (i + 0.5f) / 14f;
            var line = MakeImage(rt, "SL" + i, new Color(1f, 0.9f, 0.2f, i % 2 == 0 ? 0.035f : 0.018f));
            SetAnchors(line.rectTransform, 0f, y, 1f, y);
            line.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            line.rectTransform.sizeDelta = new Vector2(0f, 2.2f);
            line.rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    private static void AddCorner(RectTransform parent, string name, bool left, bool top)
    {
        float ax = left ? 0f : 1f;
        float ay = top ? 1f : 0f;
        var go = new GameObject("Corner_" + name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(ax, ay);
        rt.anchorMax = new Vector2(ax, ay);
        rt.pivot = new Vector2(ax, ay);
        rt.sizeDelta = new Vector2(18f, 18f);
        rt.anchoredPosition = new Vector2(left ? 4f : -4f, top ? -4f : 4f);

        var h = MakeImage(rt, "H", Yellow).rectTransform;
        h.anchorMin = new Vector2(left ? 0f : 0.35f, top ? 0.85f : 0f);
        h.anchorMax = new Vector2(left ? 0.65f : 1f, top ? 1f : 0.15f);
        h.offsetMin = Vector2.zero;
        h.offsetMax = Vector2.zero;

        var v = MakeImage(rt, "V", Yellow).rectTransform;
        v.anchorMin = new Vector2(left ? 0f : 0.85f, top ? 0.35f : 0f);
        v.anchorMax = new Vector2(left ? 0.15f : 1f, top ? 1f : 0.65f);
        v.offsetMin = Vector2.zero;
        v.offsetMax = Vector2.zero;
    }

    private static void BuildToast()
    {
        var go = new GameObject("Toast");
        go.transform.SetParent(_panel, false);
        _toastRt = go.AddComponent<RectTransform>();
        SetAnchors(_toastRt, 0f, 1f, 1f, 1f);
        _toastRt.pivot = new Vector2(0.5f, 1f);
        _toastRt.sizeDelta = new Vector2(0f, 36f);
        _toastRt.anchoredPosition = new Vector2(0f, 42f);
        InsetX(_toastRt, 12f);

        _toastBg = go.AddComponent<Image>();
        _toastBg.color = ToastBg;

        var accent = MakeImage(_toastRt, "Accent", Yellow).rectTransform;
        SetAnchors(accent, 0f, 0f, 0f, 1f);
        accent.pivot = new Vector2(0f, 0.5f);
        accent.sizeDelta = new Vector2(3f, 0f);

        _toastTitle = MakeText(_toastRt, "TTitle", "", 10, Yellow, TextAnchor.MiddleLeft);
        SetAnchors(_toastTitle.rectTransform, 0f, 0.48f, 1f, 1f);
        _toastTitle.rectTransform.offsetMin = new Vector2(10f, 0f);
        _toastTitle.rectTransform.offsetMax = new Vector2(-6f, -2f);

        _toastBody = MakeText(_toastRt, "TBody", "", 11, TextCol, TextAnchor.MiddleLeft);
        SetAnchors(_toastBody.rectTransform, 0f, 0f, 1f, 0.55f);
        _toastBody.rectTransform.offsetMin = new Vector2(10f, 2f);
        _toastBody.rectTransform.offsetMax = new Vector2(-6f, 0f);

        _toastT = 1f;
        ApplyToastVisual();
    }

    // Сглаженное направление «наружу из предплечья». Хранится между кадрами,
    // иначе плата дёргается при каждом движении головы.
    private static Vector3 _outSmooth;
    private static bool _outInit;

    private const float LiftOffArm = 0.075f;   // на сколько поднять над рукой
    private const float AlongArm = 0.45f;      // где вдоль предплечья (0 локоть, 1 кисть)

    /// <summary>
    /// Плата лежит на предплечье, ЛИЦОМ К ИГРОКУ и ГОРИЗОНТАЛЬНО (длинная
    /// сторона вдоль предплечья).
    ///
    /// Что было не так. Направление «наружу» выбиралось как самая большая из
    /// проекций трёх осей кости — это лотерея: при смене позы руки побеждала
    /// другая ось, и плата прыгала на другую сторону. Плюс лицо Canvas
    /// направлялось ВНУТРЬ руки: у мирового Canvas лицевая сторона — это +forward,
    /// а не -forward, поэтому панель была видна с обратной стороны.
    ///
    /// Теперь базис детерминированный:
    ///   forward (лицо)  = от предплечья К ГОЛОВЕ, спроецировано на плоскость
    ///                     перпендикулярно оси руки — то есть всегда на игрока;
    ///   right           = вдоль предплечья, поэтому плата лежит горизонтально;
    ///   up              = выбирается так, чтобы смотреть В МИРОВОЙ ВЕРХ —
    ///                     панель физически не может оказаться перевёрнутой.
    /// </summary>
    private static void AttachToLeftForearm()
    {
        try
        {
            if (_root == null) return;

            RigManager rm = Player.RigManager;
            ArtRig art = rm?.physicsRig?.artOutput;
            Transform lower = art?.artLowerArmLf;
            Transform wrist = art?.artWristLf;
            Hand hand = Player.LeftHand;

            Vector3 elbowPos, wristPos;

            if (lower != null && wrist != null)
            {
                elbowPos = lower.position;
                wristPos = wrist.position;
            }
            else
            {
                // ART-рига нет — строим отрезок предплечья от кисти назад
                if (hand == null) return;
                Transform palm = hand.palmPositionTransform != null ? hand.palmPositionTransform : hand.transform;
                wristPos = palm.position;
                elbowPos = palm.position - palm.forward * 0.26f;
            }

            Vector3 along = wristPos - elbowPos;
            if (along.sqrMagnitude < 1e-8f) return;
            along.Normalize();

            Vector3 basePos = Vector3.Lerp(elbowPos, wristPos, AlongArm);

            // Сторона, куда панель ОТКЛОНЕНА от руки — к игроку. Само лицо
            // Canvas направим в другую сторону: у мирового Canvas читаемая
            // сторона та, куда смотрит forward (по направлению взгляда игрока),
            // поэтому forward = -outward. В прошлой версии я передал +outward и
            // панель встала задом к игроку — текст читался зеркально (TSOHG).
            Vector3 outward = Vector3.zero;
            Transform head = Player.Head;
            if (head != null)
                outward = Vector3.ProjectOnPlane(head.position - basePos, along);

            if (outward.sqrMagnitude < 1e-6f)
            {
                // рука смотрит точно в лицо — берём любую перпендикулярную ось
                outward = Vector3.ProjectOnPlane(Vector3.up, along);
                if (outward.sqrMagnitude < 1e-6f) outward = Vector3.ProjectOnPlane(Vector3.forward, along);
                if (outward.sqrMagnitude < 1e-6f) return;
            }
            outward.Normalize();

            if (!_outInit) { _outSmooth = outward; _outInit = true; }
            else
            {
                float k = 1f - Mathf.Exp(-12f * Mathf.Max(1e-4f, Time.unscaledDeltaTime));
                _outSmooth = Vector3.Slerp(_outSmooth, outward, k);
                if (_outSmooth.sqrMagnitude < 1e-6f) _outSmooth = outward;
                _outSmooth.Normalize();
            }

            // right панели = вдоль предплечья, поэтому она лежит горизонтально.
            // Знак выбираем так, чтобы панель не оказалась перевёрнутой.
            Vector3 up = Vector3.Cross(_outSmooth, along);
            if (up.sqrMagnitude < 1e-8f) return;
            up.Normalize();

            float upness = Vector3.Dot(up, Vector3.up);
            if (Mathf.Abs(upness) > 0.15f)
            {
                // обычная поза: верх панели — в мировой верх
                if (upness < 0f) up = -up;
            }
            else
            {
                // Рука почти вертикальна: «верх по миру» тут вырождается в ноль
                // и знак становился случайным — панель могла встать вверх ногами.
                // Тогда ориентир другой: длинная сторона смотрит К КИСТИ.
                if (Vector3.Dot(Vector3.Cross(up, _outSmooth), along) < 0f) up = -up;
            }

            // Панель приподнята над рукой в сторону игрока (+outward),
            // а её ЛИЦО смотрит на игрока — значит forward = -outward.
            Vector3 pos = basePos + _outSmooth * LiftOffArm;
            _root.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-_outSmooth, up));
        }
        catch { /* rig missing */ }
    }

    private static void AnimateAppear(float dt)
    {
        if (_appearT >= 1f || _root == null) return;
        _appearT = Mathf.Min(1f, _appearT + dt / 0.18f);
        float e = EaseOutCubic(_appearT);
        float s = _baseScale * Mathf.Lerp(0.88f, 1f, e);
        _root.transform.localScale = Vector3.one * s;
    }

    private static void AnimateScan(float dt)
    {
        if (_scanSweep == null) return;
        _scanT += dt * 0.35f;
        if (_scanT > 1f) _scanT -= 1f;
        float y = Mathf.Lerp(0f, CanvasH - 28f, _scanT);
        _scanSweep.rectTransform.anchoredPosition = new Vector2(0f, y);
        Color c = Scan;
        c.a = 0.04f + 0.05f * Mathf.Sin(_scanT * Mathf.PI);
        _scanSweep.color = c;
    }

    private static void AnimateButtons(float dt)
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            HoloBtn b = _buttons[i];
            if (b?.Rt == null || b.Bg == null) continue;

            bool hover = i == _hoverIndex;
            b.HoverBlend = Mathf.MoveTowards(b.HoverBlend, hover ? 1f : 0f, dt * 12f);
            if (b.PressAnim > 0f)
                b.PressAnim = Mathf.Max(0f, b.PressAnim - dt / 0.14f);

            float punch = b.PressAnim > 0f
                ? 1f - Mathf.Sin((1f - b.PressAnim) * Mathf.PI) * 0.06f
                : 1f;
            b.Rt.localScale = b.BaseScale * punch;

            Color idle = b.DangerStyle ? new Color(Danger.r, Danger.g, Danger.b, 0.18f)
                : (b.AccentStyle ? RowActive : RowIdle);
            Color hot = b.DangerStyle ? Danger : RowHover;
            b.Bg.color = Color.Lerp(idle, hot, b.HoverBlend);

            if (b.Accent != null)
            {
                Color a = b.DangerStyle ? DangerText : Yellow;
                a.a = Mathf.Lerp(0.35f, 0.95f, b.HoverBlend);
                if (b.AccentStyle) a.a = 0.95f;
                b.Accent.color = a;
            }

            if (b.Label != null)
            {
                Color tc = b.DangerStyle ? DangerText : TextCol;
                if (b.AccentStyle) tc = Yellow;
                tc.a = Mathf.Lerp(0.75f, 1f, b.HoverBlend);
                b.Label.color = tc;
            }
        }
    }

    private static void AnimateToast(float dt)
    {
        if (_toastRt == null) return;
        if (_toastT < 1f)
        {
            if (Time.unscaledTime < _toastHoldUntil)
                _toastT = Mathf.MoveTowards(_toastT, 0f, dt / ToastIn);
            else
                _toastT = Mathf.MoveTowards(_toastT, 1f, dt / ToastOut);
        }
        ApplyToastVisual();
    }

    private static void ApplyToastVisual()
    {
        if (_toastRt == null) return;
        float e = EaseOutCubic(1f - _toastT);
        _toastRt.anchoredPosition = new Vector2(0f, Mathf.Lerp(40f, -2f, e));
        if (_toastBg != null)
        {
            Color c = ToastBg; c.a = ToastBg.a * e; _toastBg.color = c;
        }
        if (_toastTitle != null)
        {
            _toastTitle.text = "// " + _toastTitleStr;
            var c = Yellow; c.a = e; _toastTitle.color = c;
        }
        if (_toastBody != null)
        {
            _toastBody.text = _toastBodyStr;
            var c = TextCol; c.a = e; _toastBody.color = c;
        }
    }

    /// <summary>
    /// Нажатие пальцем.
    ///
    /// Раньше срабатывание считалось по расстоянию до ПЛОСКОСТИ Canvas, и было
    /// две беды. Первая: нужно было попасть в зону толщиной 1.8 см ровно между
    /// двумя кадрами — при 72 к/с палец её перескакивает. Вторая: та математика
    /// зависела от того, как повёрнут Canvas, а мы его как раз крутили.
    ///
    /// Теперь у каждой кнопки настоящий BoxCollider, и расстояние берём как
    /// Collider.ClosestPoint — это ровно «насколько кончик пальца близко к телу
    /// кнопки», без плоскостей и знаков, независимо от ориентации. Плюс тот же
    /// коллайдер физически упирает палец (не проходит насквозь). Гистерезис:
    /// нажали при входе в зону контакта, отпустили при выходе за зону отпускания.
    /// Опрашиваются кончики обеих рук.
    /// </summary>
    private static void HandleTouch()
    {
        if (_buttons.Count == 0)
        {
            _touchDown = false;
            _hoverIndex = -1;
            _insideIndex = -1;
            return;
        }

        int nTips = CollectTips();
        int best = -1;
        float bestDist = float.MaxValue;

        for (int t = 0; t < nTips; t++)
        {
            Vector3 tip = _tips[t];
            for (int i = 0; i < _buttons.Count; i++)
            {
                HoloBtn b = _buttons[i];
                if (b?.Col == null) continue;
                Vector3 cp = b.Col.ClosestPoint(tip);       // тело кнопки, любая ориентация
                float d = Vector3.Distance(cp, tip);
                if (d < bestDist) { bestDist = d; best = i; }
            }
        }

        _hoverIndex = (best >= 0 && bestDist <= HoverDist) ? best : -1;

        // Диагностика раз в 2 с: если тапы молчат, лог скажет, есть ли вообще
        // кончик пальца и как далеко он от ближайшей кнопки (в сантиметрах).
        if (Time.unscaledTime >= _touchDbgAt)
        {
            _touchDbgAt = Time.unscaledTime + 2f;
            bool tipR = TryGetFingerTip(true, out _);
            bool tipL = TryGetFingerTip(false, out _);
            MelonLogger.Msg($"[Ghost] touch: btns={_buttons.Count} tipR={tipR} tipL={tipL} " +
                            $"best={best} dist={(best >= 0 ? (bestDist * 100f).ToString("0.0") + "cm" : "-")}");
        }

        if (best < 0 || bestDist > TouchExit)
        {
            _touchDown = false;
            _insideIndex = -1;
            return;
        }

        if (!_touchDown && bestDist <= TouchEnter)
        {
            _touchDown = true;
            _insideIndex = best;
            if (Time.unscaledTime >= _clickLockUntil) FireButton(best);
        }
    }

    private static float _touchDbgAt;

    private static void FireButton(int index)
    {
        if (index < 0 || index >= _buttons.Count) return;
        HoloBtn b = _buttons[index];
        if (b == null) return;
        _clickLockUntil = Time.unscaledTime + ClickCooldown;
        b.PressAnim = 1f;
        try { b.OnClick?.Invoke(); }
        catch (Exception ex) { MelonLogger.Warning($"Ghost btn: {ex.Message}"); }
    }

    // Буфер точек касания: до 4 кончиков × 2 руки + 2 запасных от ладони.
    private static readonly Vector3[] _tips = new Vector3[10];

    /// <summary>
    /// Собирает ВСЕ доступные кончики пальцев обеих рук. Раньше брался один
    /// указательный, и если именно его кость null или не там — тап не срабатывал.
    /// Теперь проверяем все четыре пальца каждой руки плюс запас от ладони:
    /// хоть одна точка да попадёт по кнопке.
    /// </summary>
    private static int CollectTips()
    {
        int n = 0;
        try
        {
            ArtRig art = Player.RigManager?.physicsRig?.artOutput;
            if (art != null)
            {
                AddTip(ref n, art.artFingerRt13, art.artFingerRt12);
                AddTip(ref n, art.artFingerRt23, art.artFingerRt22);
                AddTip(ref n, art.artFingerRt33, art.artFingerRt32);
                AddTip(ref n, art.artFingerRt43, art.artFingerRt42);
                AddTip(ref n, art.artFingerLf13, art.artFingerLf12);
                AddTip(ref n, art.artFingerLf23, art.artFingerLf22);
                AddTip(ref n, art.artFingerLf33, art.artFingerLf32);
                AddTip(ref n, art.artFingerLf43, art.artFingerLf42);
            }
        }
        catch { }

        if (n == 0)
        {
            // ART-рига нет — берём кончик от ладони каждой руки
            AddPalmTip(ref n, Player.RightHand, 0.01f);
            AddPalmTip(ref n, Player.LeftHand, -0.01f);
        }
        return n;
    }

    private static void AddTip(ref int n, Transform tip, Transform mid)
    {
        if (tip == null || n >= _tips.Length) return;
        try
        {
            Vector3 dir = mid != null ? (tip.position - mid.position) : tip.forward;
            if (dir.sqrMagnitude < 1e-8f) dir = tip.forward;
            _tips[n++] = tip.position + dir.normalized * 0.012f;  // до подушечки
        }
        catch { }
    }

    private static void AddPalmTip(ref int n, Hand hand, float sx)
    {
        if (hand == null || n >= _tips.Length) return;
        try
        {
            if (hand.palmPositionTransform != null)
                _tips[n++] = hand.palmPositionTransform.TransformPoint(new Vector3(sx, 0.02f, 0.06f));
            else
                _tips[n++] = hand.transform.TransformPoint(new Vector3(0f, 0.02f, 0.05f));
        }
        catch { }
    }

    /// <summary>Для диагностики: есть ли хоть один кончик у указанной руки.</summary>
    private static bool TryGetFingerTip(bool right, out Vector3 tip)
    {
        tip = default;
        try
        {
            ArtRig art = Player.RigManager?.physicsRig?.artOutput;
            Transform bone = art == null ? null : (right ? art.artFingerRt13 : art.artFingerLf13);
            if (bone != null) { tip = bone.position; return true; }
            Hand hand = right ? Player.RightHand : Player.LeftHand;
            if (hand?.palmPositionTransform != null) { tip = hand.palmPositionTransform.position; return true; }
            if (hand != null) { tip = hand.transform.position; return true; }
        }
        catch { }
        return false;
    }

    private static void QueueRebuild(Tab tab)
    {
        _queuedTab = tab;
        _rebuildQueued = true;
    }

    private static void Rebuild(Tab tab)
    {
        _tab = tab;
        ClearContent();
        if (_panel == null || _content == null) return;

        AddTab(0, "IDENTITY", Tab.Nick);
        AddTab(1, "LOBBY", Tab.Lobby);
        AddTab(2, "MOD.IO", Tab.Custom);

        switch (tab)
        {
            case Tab.Nick: BuildNick(); break;
            case Tab.Lobby: BuildLobby(); break;
            case Tab.Custom: BuildCustom(); break;
        }

        if (_headerSub != null)
            _headerSub.text = tab == Tab.Lobby && !GhostLobby.IsHost ? "HOST ONLY" : "LINK ACTIVE";

        _insideIndex = -1;
        _hoverIndex = -1;

        BuildColliders();
    }

    /// <summary>
    /// Навешивает коллайдеры на кнопки и плиту ПОСЛЕ принудительного апдейта
    /// Canvas. Ключевой момент: у только что созданных дочерних RectTransform
    /// поле rect ещё нулевое до ближайшей перестройки Canvas, поэтому
    /// GetWorldCorners в том же кадре давал нулевой размер и коллайдер не
    /// создавался вообще — оттого палец и проходил насквозь, и тапы молчали.
    /// ForceUpdateCanvases считает layout немедленно.
    /// </summary>
    private static void BuildColliders()
    {
        try { Canvas.ForceUpdateCanvases(); } catch { }

        int made = 0;
        for (int i = 0; i < _buttons.Count; i++)
        {
            var b = _buttons[i];
            if (b?.Rt == null) continue;
            if (b.Col == null) b.Col = AddTouchCollider(b.Rt.gameObject, b.Rt);
            if (b.Col != null) made++;
        }

        if (_panel != null && _panel.GetComponent<Collider>() == null)
            AddTouchCollider(_panel.gameObject, _panel);

        MelonLogger.Msg($"[Ghost] colliders: {made}/{_buttons.Count} buttons + panel");
    }

    private static void BuildNick()
    {
        // Left column — identity ops
        var left = MakeSection(_content, "ID", "IDENTITY", 0f, 0f, 0.48f, 1f);
        float y = -4f;
        AddRow(left, ref y, "HIDE NAME", "braille blank", () =>
        {
            GhostIdentity.ApplyInvisible();
            Notify("NICK", "Name hidden");
        });
        AddRow(left, ref y, "SET · GHOST", "preset", () =>
        {
            GhostIdentity.ApplyPreset("GHOST");
            Notify("NICK", "Set to GHOST");
        });
        AddRow(left, ref y, "SET · UNKNOWN", "preset", () =>
        {
            GhostIdentity.ApplyPreset("UNKNOWN");
            Notify("NICK", "Set to UNKNOWN");
        });
        AddRow(left, ref y, "SET · ANON", "preset", () =>
        {
            GhostIdentity.ApplyPreset("ANON");
            Notify("NICK", "Set to ANON");
        });
        AddRow(left, ref y, "RESTORE PROFILE", "revert", () =>
        {
            GhostIdentity.Restore();
            Notify("RESTORE", "Real profile back");
        }, danger: true);

        // Right column — session players
        var right = MakeSection(_content, "PLY", "SESSION PLAYERS", 0.52f, 0f, 1f, 1f);
        float ry = -4f;
        var players = GhostIdentity.ListSessionPlayers();
        if (players.Count == 0)
        {
            AddHint(right, ref ry, "No other players in session");
        }
        else
        {
            int start = _playerPage * 4;
            int shown = 0;
            for (int i = start; i < players.Count && shown < 4; i++, shown++)
            {
                var entry = players[i];
                PlayerID id = entry.id;
                string captured = entry.label;
                string name = Trim(entry.label, 14);
                AddRow(right, ref ry, "CLONE · " + name, "copy identity", () =>
                {
                    GhostIdentity.CloneFromPlayer(id);
                    Notify("CLONE", Trim(captured, 16));
                });
            }
            if (players.Count > 4)
            {
                AddRow(right, ref ry, "NEXT PAGE ›", $"{_playerPage + 1}/{Mathf.CeilToInt(players.Count / 4f)}", () =>
                {
                    _playerPage++;
                    if (_playerPage * 4 >= players.Count) _playerPage = 0;
                    QueueRebuild(Tab.Nick);
                }, accent: true);
            }
        }
    }

    private static void BuildLobby()
    {
        var left = MakeSection(_content, "LB", "LOBBY SPOOF", 0f, 0f, 0.48f, 1f);
        float y = -4f;
        string hostHint = GhostLobby.IsHost ? $"fakes online · {GhostLobby.Fakes.Count}" : "host required";
        AddHint(left, ref y, hostHint);
        AddRow(left, ref y, "ADD FAKE SLOT", "inject playerinfo", () =>
        {
            int before = GhostLobby.Fakes.Count;
            GhostLobby.AddFake();
            if (GhostLobby.Fakes.Count > before)
            {
                Notify("LOBBY", $"Fake +1 · {GhostLobby.Fakes.Count}");
                QueueRebuild(Tab.Lobby);
            }
        }, accent: true);
        AddRow(left, ref y, "CLEAR ALL FAKES", "wipe metadata", () =>
        {
            if (!GhostLobby.IsHost)
            {
                Notify("DENIED", GhostLobby.EnsureHostOrError());
                return;
            }
            GhostLobby.ClearFakes();
            Notify("LOBBY", "Fakes cleared");
            QueueRebuild(Tab.Lobby);
        }, danger: true);

        var right = MakeSection(_content, "AV", "AVATAR ICON", 0.52f, 0f, 1f, 1f);
        float ry = -4f;
        AddHint(right, ref ry, "apply local avatar meta");
        for (int i = 0; i < GhostLobby.AvatarPresets.Length && i < 5; i++)
        {
            string av = GhostLobby.AvatarPresets[i];
            AddRow(right, ref ry, av.ToUpperInvariant(), "icon preset", () =>
            {
                GhostIdentity.ApplyAvatarMeta(av, -1);
                Notify("AVATAR", "Icon → " + av);
            });
        }
    }

    private static void BuildCustom()
    {
        var left = MakeSection(_content, "IN", "MOD.IO ID", 0f, 0f, 0.42f, 1f);
        float y = -6f;
        AddHint(left, ref y, "enter numeric mod id");

        var display = MakeImage(left, "Display", new Color(1f, 0.88f, 0.12f, 0.12f)).rectTransform;
        display.anchorMin = new Vector2(0f, 1f);
        display.anchorMax = new Vector2(1f, 1f);
        display.pivot = new Vector2(0.5f, 1f);
        display.sizeDelta = new Vector2(0f, 28f);
        display.anchoredPosition = new Vector2(0f, y);
        var edge = MakeImage(display, "E", YellowSoft);
        Stretch(edge.rectTransform);
        Inset(edge.rectTransform, 0f);
        edge.color = new Color(Yellow.r, Yellow.g, Yellow.b, 0.35f);
        var fill = MakeImage(display, "F", RowIdle);
        Stretch(fill.rectTransform);
        Inset(fill.rectTransform, 1f);
        string shown = string.IsNullOrEmpty(_keypad) ? "——" : _keypad + "_";
        var dispTxt = MakeText(display, "V", shown, 16, Yellow, TextAnchor.MiddleCenter);
        Stretch(dispTxt.rectTransform);

        var right = MakeSection(_content, "KP", "KEYPAD", 0.46f, 0f, 1f, 1f);
        string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "CLR", "0", "OK" };
        for (int i = 0; i < keys.Length; i++)
        {
            int col = i % 3;
            int row = i / 3;
            string key = keys[i];
            float x = 4f + col * 58f;
            float yy = -4f - row * 26f;
            bool danger = key == "CLR";
            bool accent = key == "OK";
            AddRowAt(right, x, yy, 54f, 22f, key, null, () =>
            {
                if (key == "CLR")
                {
                    _keypad = "";
                    Notify("CODE", "Cleared");
                }
                else if (key == "OK")
                {
                    if (int.TryParse(_keypad, out int modId) && modId > 0)
                    {
                        GhostIdentity.ApplyAvatarMeta("mod.io/" + modId, modId);
                        Notify("MOD.IO", "Linked " + modId);
                    }
                    else Notify("ERROR", "Need numeric id");
                }
                else if (_keypad.Length < 9)
                {
                    _keypad += key;
                }
                QueueRebuild(Tab.Custom);
            }, danger, accent);
        }
    }

    private static RectTransform MakeSection(Transform parent, string id, string title, float x0, float y0, float x1, float y1)
    {
        var go = new GameObject("Sec_" + id);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        SetAnchors(rt, x0, y0, x1, y1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = MakeImage(rt, "Bg", new Color(1f, 0.88f, 0.12f, 0.05f));
        Stretch(bg.rectTransform);

        var border = MakeImage(rt, "Bd", new Color(1f, 0.9f, 0.15f, 0.22f));
        Stretch(border.rectTransform);
        // hollow feel: shrink fill slightly via nested wash
        var inner = MakeImage(rt, "In", new Color(0.05f, 0.04f, 0.01f, 0.15f));
        Stretch(inner.rectTransform);
        Inset(inner.rectTransform, 1f);

        var head = MakeText(rt, "H", title, 9, TextDim, TextAnchor.MiddleLeft);
        SetAnchors(head.rectTransform, 0f, 1f, 1f, 1f);
        head.rectTransform.pivot = new Vector2(0f, 1f);
        head.rectTransform.sizeDelta = new Vector2(0f, 14f);
        head.rectTransform.anchoredPosition = Vector2.zero;
        head.rectTransform.offsetMin = new Vector2(6f, -14f);
        head.rectTransform.offsetMax = new Vector2(-4f, 0f);

        var body = new GameObject("Body");
        body.transform.SetParent(rt, false);
        var brt = body.AddComponent<RectTransform>();
        SetAnchors(brt, 0f, 0f, 1f, 1f);
        brt.offsetMin = new Vector2(5f, 4f);
        brt.offsetMax = new Vector2(-5f, -16f);
        return brt;
    }

    private static void AddHint(Transform host, ref float y, string text)
    {
        var t = MakeText(host, "Hint", text, 9, TextDim, TextAnchor.MiddleLeft);
        t.rectTransform.anchorMin = new Vector2(0f, 1f);
        t.rectTransform.anchorMax = new Vector2(1f, 1f);
        t.rectTransform.pivot = new Vector2(0f, 1f);
        t.rectTransform.sizeDelta = new Vector2(0f, 12f);
        t.rectTransform.anchoredPosition = new Vector2(2f, y);
        y -= 14f;
    }

    private static void AddTab(int index, string label, Tab tab)
    {
        var tabs = _panel.Find("Tabs");
        if (tabs == null) return;
        float w = 78f;
        float x = 6f + index * (w + 6f);
        bool active = _tab == tab;
        AddRowAt(tabs, x, -2f, w, 18f, label, null, () => QueueRebuild(tab), false, active);
    }

    private static void AddRow(Transform host, ref float y, string label, string sub, Action act, bool danger = false, bool accent = false)
    {
        AddRowAt(host, 0f, y, -1f, 20f, label, sub, act, danger, accent);
        y -= 22f;
    }

    private static void AddRowAt(Transform host, float x, float y, float w, float h, string label, string sub, Action act, bool danger = false, bool accent = false)
    {
        var go = new GameObject("Row_" + label);
        go.transform.SetParent(host, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = w < 0f ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(w < 0f ? 0f : w, h);
        rt.anchoredPosition = new Vector2(x, y);

        var fillImg = go.AddComponent<Image>();
        fillImg.color = accent ? RowActive : RowIdle;
        fillImg.raycastTarget = false;

        var accentBar = MakeImage(rt, "Acc", Yellow).rectTransform;
        SetAnchors(accentBar, 0f, 0.15f, 0f, 0.85f);
        accentBar.pivot = new Vector2(0f, 0.5f);
        accentBar.sizeDelta = new Vector2(2f, 0f);
        accentBar.anchoredPosition = new Vector2(2f, 0f);
        var accentImg = accentBar.GetComponent<Image>();

        var txt = MakeText(rt, "L", label, 10, TextCol, TextAnchor.MiddleLeft);
        SetAnchors(txt.rectTransform, 0f, 0f, 1f, 1f);
        txt.rectTransform.offsetMin = new Vector2(8f, 0f);
        txt.rectTransform.offsetMax = new Vector2(sub != null ? -48f : -4f, 0f);
        if (accent) { txt.color = Yellow; txt.fontStyle = FontStyle.Bold; }
        if (danger) txt.color = DangerText;

        if (!string.IsNullOrEmpty(sub))
        {
            var st = MakeText(rt, "S", sub, 8, TextDim, TextAnchor.MiddleRight);
            SetAnchors(st.rectTransform, 0.45f, 0f, 1f, 1f);
            st.rectTransform.offsetMin = new Vector2(0f, 0f);
            st.rectTransform.offsetMax = new Vector2(-4f, 0f);
        }

        _buttons.Add(new HoloBtn
        {
            Rt = rt,
            Bg = fillImg,
            Accent = accentImg,
            Label = txt,
            OnClick = act,
            DangerStyle = danger,
            AccentStyle = accent,
            BaseScale = Vector3.one
            // коллайдер навешивается позже в BuildColliders(): rect дочернего
            // RectTransform в этот момент ещё нулевой
        });
    }

    /// <summary>
    /// Даёт кнопке настоящий BoxCollider: об него палец физически упирается
    /// (не проходит насквозь), а тап детектится через ClosestPoint — это не
    /// зависит от того, как повёрнут Canvas, в отличие от прежней математики
    /// плоскости, которая и не срабатывала.
    ///
    /// Размер берём из мировых углов rect и переводим в локальные единицы через
    /// lossyScale: у растянутых строк sizeDelta не отражает реальную ширину, а
    /// углы отражают всегда.
    /// </summary>
    private static Collider AddTouchCollider(GameObject go, RectTransform rt)
    {
        try
        {
            // rt.rect уже в ЛОКАЛЬНЫХ единицах Canvas и учитывает растянутые
            // якоря (у таких строк sizeDelta = 0, а rect.width = ширине родителя).
            Rect r = rt.rect;
            if (r.width < 1e-3f || r.height < 1e-3f) return null;

            float sz = Mathf.Max(1e-6f, Mathf.Abs(rt.lossyScale.z));

            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(r.center.x, r.center.y, 0f);
            box.size = new Vector3(r.width, r.height, TouchThickness / sz);
            box.isTrigger = false;                  // палец реально упирается
            return box;
        }
        catch { return null; }
    }

    private static void ClearContent()
    {
        for (int i = 0; i < _buttons.Count; i++)
            if (_buttons[i]?.Rt != null) Object.Destroy(_buttons[i].Rt.gameObject);
        _buttons.Clear();

        if (_panel == null) return;
        var tabs = _panel.Find("Tabs");
        if (tabs != null)
            for (int i = tabs.childCount - 1; i >= 0; i--)
                Object.Destroy(tabs.GetChild(i).gameObject);

        if (_content != null)
            for (int i = _content.childCount - 1; i >= 0; i--)
                Object.Destroy(_content.GetChild(i).gameObject);
    }

    private static string Trim(string s, int n)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= n ? s : s.Substring(0, n - 1) + "…";
    }

    private static float EaseOutCubic(float x)
    {
        float t = 1f - x;
        return 1f - t * t * t;
    }

    private static Image MakeImage(Transform parent, string name, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;
        return img;
    }

    private static Text MakeText(Transform parent, string name, string value, int size, Color col, TextAnchor anchor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.font = _font;
        t.text = value;
        t.fontSize = size;
        t.color = col;
        t.alignment = anchor;
        t.raycastTarget = false;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        return t;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(RectTransform rt, float x0, float y0, float x1, float y1)
    {
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
    }

    private static void Inset(RectTransform rt, float v)
    {
        rt.offsetMin = new Vector2(v, v);
        rt.offsetMax = new Vector2(-v, -v);
    }

    private static void InsetX(RectTransform rt, float v)
    {
        rt.offsetMin = new Vector2(v, rt.offsetMin.y);
        rt.offsetMax = new Vector2(-v, rt.offsetMax.y);
    }
}
