using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppSLZ.Marrow.Interaction;
using MelonLoader;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Связывает код-мод с телефоном, заспавненным из паллета Marrow SDK.
    ///
    /// Договор с паллетом ровно один: внутри префаба есть меш с именем
    /// "Screen_UI_Anchor" — плоский экран с UV 0..1. Всё остальное (грип,
    /// физика, слои, MarrowEntity и синхронизация Fusion) делает сам паллет.
    ///
    /// Тот же механизм подхватывает и телефон, собранный в рантайме, поэтому
    /// оба пути работают одновременно.
    /// </summary>
    internal static class PalletBinder
    {
        public const string ScreenName = "Screen_UI_Anchor";

        // Очередь сущностей, пришедших из хука MarrowEntity.Awake.
        private static readonly Queue<GameObject> _pending = new Queue<GameObject>();
        private static bool _hooked;

        private static readonly Dictionary<int, PhoneInstance> _bound =
            new Dictionary<int, PhoneInstance>();

        public static IEnumerable<PhoneInstance> Bound => _bound.Values;

        /// <summary>Регистрируем телефон, созданный нами же (рантайм-сборка).</summary>
        public static void Register(PhoneInstance phone)
        {
            if (phone?.Root == null) return;
            _bound[phone.Root.GetInstanceID()] = phone;
        }

        public static void Forget(PhoneInstance phone)
        {
            if (phone?.Root != null) _bound.Remove(phone.Root.GetInstanceID());
        }

        /// <summary>
        /// Ставим хук на появление сущностей Marrow. Раньше здесь был периодический
        /// FindObjectsOfType&lt;MeshFilter&gt;() — в сцене BONELAB их тысячи, и это
        /// давало просадку каждые полсекунды. Теперь стоимость нулевая: реагируем
        /// только на реальный спавн.
        /// </summary>
        public static void Install(HarmonyLib.Harmony harmony)
        {
            if (_hooked || harmony == null) return;
            try
            {
                var m = AccessTools.Method(typeof(MarrowEntity), "Awake");
                if (m == null) { MelonLogger.Warning("[LPhone] MarrowEntity.Awake не найден"); return; }
                harmony.Patch(m, postfix: new HarmonyMethod(typeof(PalletBinder), nameof(OnEntityAwake)));
                _hooked = true;
                MelonLogger.Msg("[LPhone] хук спавна паллета установлен");
            }
            catch (Exception e) { MelonLogger.Warning("[LPhone] хук спавна: " + e.Message); }
        }

        private static void OnEntityAwake(MarrowEntity __instance)
        {
            try
            {
                if (__instance == null) return;
                var go = __instance.gameObject;
                if (go != null) _pending.Enqueue(go);      // связываем в следующем кадре
            }
            catch { }
        }

        /// <summary>Разбираем очередь спавнов и подчищаем уничтоженные телефоны.</summary>
        public static void Scan()
        {
            // подчищаем уничтоженные
            List<int> dead = null;
            foreach (var kv in _bound)
                if (kv.Value?.Root == null) (dead ??= new List<int>()).Add(kv.Key);
            if (dead != null)
                foreach (var k in dead) _bound.Remove(k);

            while (_pending.Count > 0)
            {
                var go = _pending.Dequeue();
                try
                {
                    if (go == null) continue;

                    // ищем экран ТОЛЬКО внутри заспавненной сущности — это дёшево
                    Transform screen = null;
                    foreach (var t in go.GetComponentsInChildren<Transform>(true))
                    {
                        if (t != null && string.Equals(t.name, ScreenName, StringComparison.Ordinal))
                        { screen = t; break; }
                    }
                    if (screen == null) continue;

                    Transform root = go.transform;
                    var rb = go.GetComponentInParent<Rigidbody>();
                    if (rb != null) root = rb.transform;

                    int id = root.gameObject.GetInstanceID();
                    if (_bound.ContainsKey(id)) continue;

                    var phone = new PhoneInstance(root.gameObject, screen.GetComponent<Renderer>(), screen);
                    _bound[id] = phone;
                    MelonLogger.Msg($"[LPhone] подхвачен телефон из паллета: {root.name}");
                }
                catch (Exception e) { MelonLogger.Warning("[LPhone] привязка: " + e.Message); }
            }
        }
    }
}
