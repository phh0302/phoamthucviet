using System.Collections.Generic;
using UnityEngine;

namespace PhoAmThucViet.Audio
{
    public enum Sfx { Click, Coin, Upgrade, Hire, LevelUp, Chest }

    /// <summary>
    /// Âm thanh tạo bằng code (không cần file âm thanh): tiếng bấm, "ting" thu tiền, nâng cấp, thuê người,
    /// nhạc lên đời, mở rương. Khi có file âm thanh thật, thay phần tạo clip bằng Resources.Load.
    /// Bật/tắt được lưu trong PlayerPrefs.
    /// </summary>
    public static class SoundFX
    {
        private const int Rate = 44100;
        private const string MuteKey = "pho_mute";
        private static AudioSource _source;
        private static readonly Dictionary<Sfx, AudioClip> Clips = new Dictionary<Sfx, AudioClip>();
        private static float _lastCoin;

        public static bool Muted
        {
            get => PlayerPrefs.GetInt(MuteKey, 0) == 1;
            set { PlayerPrefs.SetInt(MuteKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static void Play(Sfx sfx, float volume = 0.6f)
        {
            if (Muted) return;
            if (sfx == Sfx.Coin)
            {
                // Tránh "ting" dồn dập khi thu nhiều quầy liên tiếp
                if (Time.unscaledTime - _lastCoin < 0.06f) return;
                _lastCoin = Time.unscaledTime;
            }
            EnsureSource();
            if (!Clips.TryGetValue(sfx, out var clip)) Clips[sfx] = clip = Make(sfx);
            _source.pitch = sfx == Sfx.Coin ? Random.Range(0.95f, 1.08f) : 1f;
            _source.PlayOneShot(clip, volume);
        }

        private static void EnsureSource()
        {
            if (_source != null) return;
            var go = new GameObject("SoundFX");
            Object.DontDestroyOnLoad(go);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            if (Object.FindFirstObjectByType<AudioListener>() == null) go.AddComponent<AudioListener>();
        }

        // ---------- Tạo âm thanh ----------

        private static AudioClip Make(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Click: return Tones("click", new[] { 660f }, 0.05f, 0.4f);
                case Sfx.Coin: return Tones("coin", new[] { 1319f, 1760f }, 0.09f, 0.5f);
                case Sfx.Upgrade: return Tones("upgrade", new[] { 523f, 784f }, 0.07f, 0.5f);
                case Sfx.Hire: return Tones("hire", new[] { 440f, 554f, 659f }, 0.07f, 0.5f);
                case Sfx.Chest: return Tones("chest", new[] { 784f, 988f, 1175f, 1568f }, 0.08f, 0.5f);
                default: return Tones("levelup", new[] { 523f, 659f, 784f, 1047f, 1319f }, 0.12f, 0.55f);
            }
        }

        /// <summary>Chuỗi nốt sóng sin + bồi âm, mỗi nốt có đoạn tắt dần để nghe "ting" tròn tiếng.</summary>
        private static AudioClip Tones(string name, float[] freqs, float noteSeconds, float gain)
        {
            int noteLen = (int)(Rate * noteSeconds);
            int tail = (int)(Rate * 0.12f);
            var data = new float[noteLen * freqs.Length + tail];
            for (int n = 0; n < freqs.Length; n++)
            {
                int start = n * noteLen;
                int len = noteLen + tail;
                for (int i = 0; i < len && start + i < data.Length; i++)
                {
                    float t = i / (float)Rate;
                    float env = Mathf.Exp(-t * 18f) * Mathf.Min(1f, i / 80f); // bật nhanh, tắt dần
                    float w = Mathf.Sin(2 * Mathf.PI * freqs[n] * t) + 0.3f * Mathf.Sin(4 * Mathf.PI * freqs[n] * t);
                    data[start + i] += w * env * gain * 0.7f;
                }
            }
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
