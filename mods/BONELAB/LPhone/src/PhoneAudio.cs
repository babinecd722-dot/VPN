using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Звуки телефона. Генерируем волной прямо в коде — так не нужно тащить
    /// в мод аудиофайлы, а звучит чисто (синус с обертоном и мягкой огибающей).
    /// Источник висит на корпусе, поэтому рингтон слышно «из телефона».
    /// </summary>
    internal sealed class PhoneAudio
    {
        private const int Rate = 22050;

        private static AudioClip _ring, _click, _ding, _dial, _end;
        private static bool _built;

        private readonly AudioSource _src;
        private readonly AudioSource _loop;

        public PhoneAudio(GameObject root)
        {
            Build();
            try
            {
                var go = new GameObject("LPhone_Audio");
                go.transform.SetParent(root.transform, false);

                _src = go.AddComponent<AudioSource>();
                _src.playOnAwake = false;
                _src.spatialBlend = 1f;
                _src.minDistance = 0.3f;
                _src.maxDistance = 14f;
                _src.volume = 0.85f;

                _loop = go.AddComponent<AudioSource>();
                _loop.playOnAwake = false;
                _loop.loop = true;
                _loop.spatialBlend = 1f;
                _loop.minDistance = 0.3f;
                _loop.maxDistance = 18f;
                _loop.volume = 0.9f;
            }
            catch (Exception e) { MelonLogger.Warning("[LPhone] аудио: " + e.Message); }
        }

        public void Click() => One(_click, 0.5f);
        public void Ding() => One(_ding, 0.8f);
        public void Hangup() => One(_end, 0.7f);

        public void RingStart() => Loop(_ring);
        public void DialStart() => Loop(_dial);

        public void LoopStop()
        {
            try { if (_loop != null && _loop.isPlaying) _loop.Stop(); } catch { }
        }

        public bool LoopPlaying
        {
            get { try { return _loop != null && _loop.isPlaying; } catch { return false; } }
        }

        private void One(AudioClip c, float vol)
        {
            try { if (_src != null && c != null) _src.PlayOneShot(c, vol); } catch { }
        }

        private void Loop(AudioClip c)
        {
            try
            {
                if (_loop == null || c == null) return;
                if (_loop.isPlaying && _loop.clip == c) return;
                _loop.clip = c;
                _loop.Play();
            }
            catch { }
        }

        // ─────────────── генерация ───────────────

        private static void Build()
        {
            if (_built) return;
            _built = true;
            try
            {
                _ring = Ringtone();
                _dial = Dialtone();
                _click = Shutter();
                _ding = DingClip();
                _end = EndBeep();
                MelonLogger.Msg("[LPhone] звуки собраны");
            }
            catch (Exception e) { MelonLogger.Warning("[LPhone] звуки: " + e.Message); }
        }

        private static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(new Il2CppStructArray<float>(data), 0);
            return clip;
        }

        private static float Note(float freq, float t, float dur)
        {
            if (t < 0f || t > dur) return 0f;
            float env = Mathf.Min(1f, t / 0.006f) * Mathf.Exp(-3.6f * t / dur);
            float w = Mathf.Sin(2f * Mathf.PI * freq * t)
                    + 0.32f * Mathf.Sin(4f * Mathf.PI * freq * t)
                    + 0.11f * Mathf.Sin(6f * Mathf.PI * freq * t);
            return env * w * 0.34f;
        }

        /// <summary>Входящий вызов: восходящее арпеджио, петля 2.4 с.</summary>
        private static AudioClip Ringtone()
        {
            int n = (int)(2.4f * Rate);
            var d = new float[n];
            // A4 C#5 E5 A5 — мажорное трезвучие, звучит «дорого»
            float[] f = { 440f, 554.37f, 659.25f, 880f, 659.25f, 554.37f };
            float step = 0.135f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float v = 0f;
                for (int k = 0; k < f.Length; k++) v += Note(f[k], t - k * step, 0.55f);
                for (int k = 0; k < f.Length; k++) v += Note(f[k], t - 1.2f - k * step, 0.55f);
                d[i] = Mathf.Clamp(v, -1f, 1f);
            }
            return Make("lphone_ring", d);
        }

        /// <summary>Исходящий вызов: длинные гудки.</summary>
        private static AudioClip Dialtone()
        {
            int n = (int)(4f * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float ph = t % 4f;
                float v = 0f;
                if (ph < 1.0f)
                {
                    float e = Mathf.Min(1f, ph / 0.02f) * Mathf.Min(1f, (1.0f - ph) / 0.05f);
                    v = e * 0.22f * (Mathf.Sin(2f * Mathf.PI * 425f * t) * 0.8f
                                   + Mathf.Sin(2f * Mathf.PI * 340f * t) * 0.35f);
                }
                d[i] = v;
            }
            return Make("lphone_dial", d);
        }

        private static AudioClip Shutter()
        {
            int n = (int)(0.14f * Rate);
            var d = new float[n];
            var rnd = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Exp(-46f * t);
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                d[i] = env * (noise * 0.5f + Mathf.Sin(2f * Mathf.PI * 1900f * t) * 0.3f) * 0.8f;
            }
            return Make("lphone_shutter", d);
        }

        private static AudioClip DingClip()
        {
            int n = (int)(0.55f * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                d[i] = Note(1174.66f, t, 0.5f) + Note(1567.98f, t - 0.06f, 0.44f);
            }
            return Make("lphone_ding", d);
        }

        private static AudioClip EndBeep()
        {
            int n = (int)(0.4f * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                d[i] = Note(520f, t, 0.16f) + Note(390f, t - 0.17f, 0.2f);
            }
            return Make("lphone_end", d);
        }
    }
}
