using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// Progress and settings kept between sessions (PlayerPrefs): which chapters are done, best stars,
    /// the last chapter started (for "Continue"), and the menu settings (volumes, subtitles, turning, comfort).
    /// Autoplay tests do not write progress.
    /// </summary>
    public static class Bvr2Save
    {
        const string P = "bvr2.";
        public const int MaxChapter = 6;

        // ---------------- progress ----------------
        public static bool Done(int chapter) => PlayerPrefs.GetInt(P + "done." + chapter, 0) == 1;
        public static int Stars(int chapter) => PlayerPrefs.GetInt(P + "stars." + chapter, 0);
        /// <summary>The last chapter started (−1: never played).</summary>
        public static int Last => PlayerPrefs.GetInt(P + "last", -1);
        public static bool HasProgress => Last >= 0;

        public static void Started(int chapter) { PlayerPrefs.SetInt(P + "last", chapter); PlayerPrefs.Save(); }

        public static void Completed(int chapter, int stars)
        {
            PlayerPrefs.SetInt(P + "done." + chapter, 1);
            if (stars > Stars(chapter)) PlayerPrefs.SetInt(P + "stars." + chapter, Mathf.Clamp(stars, 0, 3));
            PlayerPrefs.Save();
        }

        public static int DoneCount { get { int n = 0; for (int c = 0; c <= MaxChapter; c++) if (Done(c)) n++; return n; } }
        public static int StarCount { get { int n = 0; for (int c = 1; c <= MaxChapter; c++) n += Stars(c); return n; } }

        /// <summary>Where "Continue" leads: the last chapter if it is not finished, otherwise the next one.</summary>
        public static int ContinueChapter
        {
            get
            {
                int last = Last; if (last < 0) return 0;
                return Done(last) && last < MaxChapter ? last + 1 : last;
            }
        }

        public static void ResetProgress()
        {
            for (int c = 0; c <= MaxChapter; c++) { PlayerPrefs.DeleteKey(P + "done." + c); PlayerPrefs.DeleteKey(P + "stars." + c); }
            PlayerPrefs.DeleteKey(P + "last"); PlayerPrefs.Save();
        }

        // ---------------- settings ----------------
        public static float Master { get => PlayerPrefs.GetFloat(P + "vol.master", 1f); set { PlayerPrefs.SetFloat(P + "vol.master", Mathf.Clamp01(value)); PlayerPrefs.Save(); } }
        public static float Music { get => PlayerPrefs.GetFloat(P + "vol.music", 1f); set { PlayerPrefs.SetFloat(P + "vol.music", Mathf.Clamp01(value)); PlayerPrefs.Save(); } }
        public static float Voice { get => PlayerPrefs.GetFloat(P + "vol.voice", 1f); set { PlayerPrefs.SetFloat(P + "vol.voice", Mathf.Clamp01(value)); PlayerPrefs.Save(); } }
        public static bool Subtitles { get => PlayerPrefs.GetInt(P + "subs", 1) == 1; set { PlayerPrefs.SetInt(P + "subs", value ? 1 : 0); PlayerPrefs.Save(); } }
        public static int Snap { get => PlayerPrefs.GetInt(P + "snap", 30); set { PlayerPrefs.SetInt(P + "snap", value); PlayerPrefs.Save(); } }
        public static bool Vignette { get => PlayerPrefs.GetInt(P + "vignette", 1) == 1; set { PlayerPrefs.SetInt(P + "vignette", value ? 1 : 0); PlayerPrefs.Save(); } }

        /// <summary>Pushes the saved settings into the running game.</summary>
        public static void Apply(Bvr2Game game)
        {
            if (!game) return;
            if (game.sound) { game.sound.master = Master; game.sound.musicVolume = 0.42f * Music; }
            if (game.professor && game.professor.voiceSource) game.professor.voiceSource.volume = Master * Voice;
            if (game.hud) game.hud.subtitles = Subtitles;
            if (game.rig) { game.rig.snapDegrees = Snap; game.rig.comfortVignette = Vignette; }
        }
    }

    /// <summary>Game pause (the pause menu): world time stops, the professor's voice pauses, chapter waits hold.</summary>
    public static class Bvr2Pause
    {
        public static bool Active { get; private set; }
        static float scaleBefore = 1f;

        public static void Set(bool on)
        {
            if (on == Active) return;
            Active = on;
            if (on) { scaleBefore = Time.timeScale > 0 ? Time.timeScale : 1f; Time.timeScale = 0f; }
            else Time.timeScale = scaleBefore;
            var game = Bvr2Game.Instance;
            var voice = game && game.professor ? game.professor.voiceSource : null;
            if (voice) { if (on) voice.Pause(); else voice.UnPause(); }
        }
    }
}
