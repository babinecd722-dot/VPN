using System;
using System.Collections.Generic;
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

        private const float ScanInterval = 0.5f;
        private static float _nextScan;

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

        /// <summary>Периодически ищем новые телефоны из паллета и оживляем их экран.</summary>
        public static void Scan()
        {
            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + ScanInterval;

            // подчищаем уничтоженные
            List<int> dead = null;
            foreach (var kv in _bound)
                if (kv.Value?.Root == null) (dead ??= new List<int>()).Add(kv.Key);
            if (dead != null)
                foreach (var k in dead) _bound.Remove(k);

            try
            {
                var filters = UnityEngine.Object.FindObjectsOfType<MeshFilter>();
                if (filters == null) return;

                for (int i = 0; i < filters.Length; i++)
                {
                    var mf = filters[i];
                    if (mf == null || mf.gameObject == null) continue;
                    if (!string.Equals(mf.gameObject.name, ScreenName, StringComparison.Ordinal)) continue;

                    // корень телефона = объект с Rigidbody выше по иерархии
                    Transform root = mf.transform;
                    var rb = mf.GetComponentInParent<Rigidbody>();
                    if (rb != null) root = rb.transform;
                    else if (root.parent != null) root = root.parent;

                    int id = root.gameObject.GetInstanceID();
                    if (_bound.ContainsKey(id)) continue;

                    var rend = mf.GetComponent<Renderer>();
                    var phone = new PhoneInstance(root.gameObject, rend, mf.transform);
                    _bound[id] = phone;
                    MelonLogger.Msg($"[LPhone] подхвачен телефон из паллета: {root.name}");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[LPhone] скан паллета: " + e.Message);
            }
        }
    }
}
