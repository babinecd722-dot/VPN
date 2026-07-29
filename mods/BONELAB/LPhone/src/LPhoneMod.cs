using System;
using System.Collections.Generic;
using BoneLib.BoneMenu;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(LPhone.LPhoneMod), "LPhone", "0.4.0", "BE PRIME")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]
[assembly: MelonOptionalDependencies("LabFusion")]

namespace LPhone
{
    public class LPhoneMod : MelonMod
    {
        private static readonly List<PhoneInstance> _phones = new List<PhoneInstance>();
        public static bool EnableTouch = true;

        public override void OnInitializeMelon()
        {
            BuildMenu();
            PalletBinder.Install(HarmonyInstance);
            MelonLogger.Msg("[LPhone] загружен v0.4.0");
            MelonLogger.Msg("[LPhone] Telegram: @be_primex");
        }

        /// <summary>
        /// Сеть регистрируем именно здесь: порядок загрузки модов не гарантирован,
        /// и в OnInitializeMelon LabFusion может быть ещё не поднят.
        /// </summary>
        public override void OnLateInitializeMelon()
        {
            if (FusionBridge.Present) FusionBridge.RegisterHandler();
        }

        public override void OnUpdate()
        {
            PalletBinder.Scan();

            foreach (var p in PalletBinder.Bound)
            {
                if (p == null || !p.Alive) continue;
                try
                {
                    // хват и физику у паллетной версии делает сам SLZ
                    if (p.RuntimeBuilt) PhoneGrab.Tick(p);
                    if (EnableTouch) TouchInput.Tick(p);
                    p.OS.Tick();
                }
                catch (Exception e) { MelonLogger.Warning("[LPhone] tick: " + e.Message); }
            }

            for (int i = _phones.Count - 1; i >= 0; i--)
                if (!_phones[i].Alive) _phones.RemoveAt(i);
        }

        private static void BuildMenu()
        {
            try
            {
                var root = Page.Root.CreatePage("Phone", new Color(0.95f, 0.45f, 0.12f), 0, true);

                root.CreateFunction("Spawn LPhone", new Color(0.3f, 0.85f, 1f), (Action)SpawnInFront);
                root.CreateFunction("Despawn all", new Color(1f, 0.4f, 0.35f), (Action)DespawnAll);

                root.CreateBool("Touch input", new Color(0.4f, 0.9f, 0.6f), EnableTouch,
                    (Action<bool>)((v) => { EnableTouch = v; MelonLogger.Msg("[LPhone] touch: " + v); }));
                root.CreateBool("Hand grab", new Color(0.9f, 0.8f, 0.2f), PhoneGrab.Enabled,
                    (Action<bool>)((v) => { PhoneGrab.Enabled = v; MelonLogger.Msg("[LPhone] grab: " + v); }));
                root.CreateBool("Pull to hand", new Color(0.5f, 0.8f, 1f), PhoneGrab.PullEnabled,
                    (Action<bool>)((v) => PhoneGrab.PullEnabled = v));

                // позу удержания удобно доводить прямо на устройстве
                var hold = root.CreatePage("Hold pose", new Color(0.6f, 0.75f, 1f), 0, true);
                hold.CreateFloat("Off palm, cm", new Color(0.8f, 0.85f, 1f),
                    PhoneGrab.HoldOut * 100f, 0.2f, -2f, 6f,
                    (Action<float>)((v) => PhoneGrab.HoldOut = v / 100f));
                hold.CreateFloat("Along fingers, cm", new Color(0.8f, 0.85f, 1f),
                    PhoneGrab.HoldUp * 100f, 0.5f, -4f, 14f,
                    (Action<float>)((v) => PhoneGrab.HoldUp = v / 100f));
                hold.CreateFloat("Across palm, cm", new Color(0.8f, 0.85f, 1f),
                    PhoneGrab.HoldSide * 100f, 0.2f, -4f, 4f,
                    (Action<float>)((v) => PhoneGrab.HoldSide = v / 100f));
                hold.CreateFloat("Tilt, deg", new Color(0.8f, 0.85f, 1f),
                    PhoneGrab.HoldTilt, 2f, -40f, 40f,
                    (Action<float>)((v) => PhoneGrab.HoldTilt = v));
            }
            catch (Exception e) { MelonLogger.Error("[LPhone] menu: " + e); }
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
            catch (Exception e) { MelonLogger.Error("[LPhone] спавн: " + e); }
        }

        public static void DespawnAll()
        {
            foreach (var p in _phones)
            {
                PhoneGrab.Forget(p);
                TouchInput.Forget(p);
                PalletBinder.Forget(p);
                p.Destroy();
            }
            _phones.Clear();
            MelonLogger.Msg("[LPhone] все телефоны убраны");
        }
    }
}
