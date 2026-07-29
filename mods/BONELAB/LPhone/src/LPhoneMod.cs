using System;
using System.Collections.Generic;
using BoneLib.BoneMenu;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(LPhone.LPhoneMod), "LPhone", "0.3.1", "BE PRIME")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace LPhone
{
    public class LPhoneMod : MelonMod
    {
        private static readonly List<PhoneInstance> _phones = new List<PhoneInstance>();
        /// <summary>Тач можно отключить — полезно, чтобы локализовать краш.</summary>
        public static bool EnableTouch = true;
        private static int _ticks;

        public override void OnInitializeMelon()
        {
            BuildMenu();
            PalletBinder.Install(HarmonyInstance);
            MelonLogger.Msg("[LPhone] загружен");
            MelonLogger.Msg("[LPhone] Telegram: @be_primex");
        }

        public override void OnUpdate()
        {
            PalletBinder.Scan();      // телефоны, заспавненные из паллета

            foreach (var p in PalletBinder.Bound)
            {
                if (p == null || !p.Alive) continue;
                try
                {
                    // Хват/физику у паллета делает сам SLZ — нам остаётся экран и тач.
                    if (p.RuntimeBuilt) PhoneGrab.Tick(p);
                    if (EnableTouch) TouchInput.Tick(p);
                    p.OS.Tick();
                }
                catch (Exception e) { MelonLogger.Warning("[LPhone] tick: " + e.Message); }
            }

            for (int i = _phones.Count - 1; i >= 0; i--)
            {
                var p = _phones[i];
                if (!p.Alive) { _phones.RemoveAt(i); continue; }
            }
        }

        private static void BuildMenu()
        {
            try
            {
                var root = Page.Root.CreatePage("Phone", new Color(0.95f, 0.45f, 0.12f), 0, true);
                root.CreateFunction("Spawn LPhone (без паллета)", new Color(0.3f, 0.85f, 1f),
                    (Action)SpawnInFront);
                root.CreateFunction("Despawn all", new Color(1f, 0.4f, 0.35f),
                    (Action)DespawnAll);
                // SLZ-компоненты в рантайме — частая причина нативных крашей,
                // поэтому хват отдельным тумблером, по умолчанию выключен.
                root.CreateBool("Touch input", new Color(0.4f, 0.9f, 0.6f), EnableTouch,
                    (Action<bool>)((v) =>
                    {
                        EnableTouch = v;
                        MelonLogger.Msg("[LPhone] тач: " + (v ? "ВКЛ" : "выкл"));
                    }));
                root.CreateBool("Хват рукой", new Color(0.9f, 0.8f, 0.2f), PhoneGrab.Enabled,
                    (Action<bool>)((v) =>
                    {
                        PhoneGrab.Enabled = v;
                        MelonLogger.Msg("[LPhone] хват: " + (v ? "ВКЛ" : "выкл"));
                    }));

                root.CreateBool("Притягивание", new Color(0.5f, 0.8f, 1f), PhoneGrab.PullEnabled,
                    (Action<bool>)((v) => PhoneGrab.PullEnabled = v));

                // Позу удержания можно довести прямо на устройстве.
                var hold = root.CreatePage("Поза в руке", new Color(0.6f, 0.75f, 1f), 0, true);
                hold.CreateFloat("Выше кисти, см", new Color(0.8f, 0.85f, 1f),
                    PhoneGrab.HoldUp * 100f, 0.5f, -6f, 12f,
                    (Action<float>)((v) => PhoneGrab.HoldUp = v / 100f));
                hold.CreateFloat("Вперёд, см", new Color(0.8f, 0.85f, 1f),
                    PhoneGrab.HoldForward * 100f, 0.5f, -6f, 12f,
                    (Action<float>)((v) => PhoneGrab.HoldForward = v / 100f));
                hold.CreateFloat("Наклон, град", new Color(0.8f, 0.85f, 1f),
                    PhoneGrab.HoldTilt, 2f, -40f, 40f,
                    (Action<float>)((v) => PhoneGrab.HoldTilt = v));
            }
            catch (Exception e)
            {
                MelonLogger.Error("[LPhone] меню: " + e);
            }
        }

        /// <summary>Спавним телефон перед игроком, экраном к нему.</summary>
        public static void SpawnInFront()
        {
            try
            {
                var head = BoneLib.Player.Head;
                Vector3 pos;
                Quaternion rot;
                if (head != null)
                {
                    pos = head.position + head.forward * 0.42f - head.up * 0.18f;
                    // экран (+Z меша) смотрит на игрока
                    rot = Quaternion.LookRotation(-head.forward, head.up);
                }
                else
                {
                    pos = Vector3.up * 1.2f;
                    rot = Quaternion.identity;
                }

                var phone = PhoneBuilder.Build(pos, rot);
                phone.RuntimeBuilt = true;
                PalletBinder.Register(phone);
                _phones.Add(phone);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[LPhone] спавн: " + e);
            }
        }

        public static void DespawnAll()
        {
            foreach (var p in _phones) { PhoneGrab.Forget(p); PalletBinder.Forget(p); p.Destroy(); }
            _phones.Clear();
            MelonLogger.Msg("[LPhone] все телефоны убраны");
        }
    }
}
