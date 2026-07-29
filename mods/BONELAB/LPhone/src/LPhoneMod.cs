using System;
using System.Collections.Generic;
using BoneLib.BoneMenu;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(LPhone.LPhoneMod), "LPhone", "0.1.1", "BE PRIME")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace LPhone
{
    public class LPhoneMod : MelonMod
    {
        private static readonly List<PhoneInstance> _phones = new List<PhoneInstance>();

        public override void OnInitializeMelon()
        {
            BuildMenu();
            MelonLogger.Msg("[LPhone] загружен");
            MelonLogger.Msg("[LPhone] Telegram: @be_primex");
        }

        public override void OnUpdate()
        {
            for (int i = _phones.Count - 1; i >= 0; i--)
            {
                var p = _phones[i];
                if (!p.Alive) { _phones.RemoveAt(i); continue; }
                try
                {
                    TouchInput.Tick(p);
                    p.OS.Tick();
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("[LPhone] tick: " + e.Message);
                }
            }
        }

        private static void BuildMenu()
        {
            try
            {
                var root = Page.Root.CreatePage("Phone", new Color(0.95f, 0.45f, 0.12f), 0, true);
                root.CreateFunction("Spawn LPhone 17 PRO MAX", new Color(0.3f, 0.85f, 1f),
                    (Action)SpawnInFront);
                root.CreateFunction("Despawn all", new Color(1f, 0.4f, 0.35f),
                    (Action)DespawnAll);
                // SLZ-компоненты в рантайме — частая причина нативных крашей,
                // поэтому хват отдельным тумблером, по умолчанию выключен.
                root.CreateBool("Grip (экспериментально)", new Color(0.9f, 0.8f, 0.2f),
                    PhoneBuilder.EnableGrip,
                    (Action<bool>)((v) =>
                    {
                        PhoneBuilder.EnableGrip = v;
                        MelonLogger.Msg("[LPhone] хват: " + (v ? "ВКЛ" : "выкл"));
                    }));
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
                _phones.Add(phone);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[LPhone] спавн: " + e);
            }
        }

        public static void DespawnAll()
        {
            foreach (var p in _phones) p.Destroy();
            _phones.Clear();
            MelonLogger.Msg("[LPhone] все телефоны убраны");
        }
    }
}
