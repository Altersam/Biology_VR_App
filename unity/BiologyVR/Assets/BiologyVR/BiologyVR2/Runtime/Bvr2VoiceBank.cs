using System;
using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// The professor's recorded lines: subtitle text → clip + lip-sync curves (mouth open / rounded / wide,
    /// 0..255 at <see cref="Line.fps"/>), generated offline from the same text (Tools/voice) and imported by
    /// Biology VR 2/Voice/Import Voice Bank. Lines without a recording are shown as subtitles only.
    /// </summary>
    [CreateAssetMenu(menuName = "Biology VR 2/Voice Bank")]
    public sealed class Bvr2VoiceBank : ScriptableObject
    {
        [Serializable]
        public sealed class Line
        {
            public string text;
            public AudioClip clip;
            public float fps = 50;
            public byte[] open, round, wide;

            /// <summary>Lip-sync values at playback time t (seconds), 0..1.</summary>
            public void Sample(float t, out float o, out float r, out float w)
            {
                o = r = w = 0;
                if (open == null || open.Length == 0) return;
                float f = t * fps; int i = Mathf.FloorToInt(f); float k = f - i;
                if (i < 0 || i >= open.Length) return;
                int j = Mathf.Min(i + 1, open.Length - 1);
                o = Mathf.Lerp(open[i], open[j], k) / 255f;
                if (round != null && round.Length == open.Length) r = Mathf.Lerp(round[i], round[j], k) / 255f;
                if (wide != null && wide.Length == open.Length) w = Mathf.Lerp(wide[i], wide[j], k) / 255f;
            }
        }

        public string voiceInfo;
        public List<Line> lines = new List<Line>();
        Dictionary<string, Line> map;

        public static string Key(string text) => (text ?? "").Trim().Replace("\r", "");

        public Line Find(string text)
        {
            if (map == null || map.Count != lines.Count)
            {
                map = new Dictionary<string, Line>();
                foreach (var l in lines) if (l != null && l.clip && !string.IsNullOrEmpty(l.text)) map[Key(l.text)] = l;
            }
            return map.TryGetValue(Key(text), out var line) ? line : null;
        }

        void OnEnable() { map = null; }
    }
}
