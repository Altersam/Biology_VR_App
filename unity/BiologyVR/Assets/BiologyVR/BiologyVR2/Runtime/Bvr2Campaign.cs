using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// Runs the chapters in one scene (one long artery, each chapter further downstream).
    /// At start: a chapter choice (any chapter can be played on its own for a lesson), unless a chapter
    /// was requested from the editor menu (PlayerPrefs "BVR2_StartChapter").
    /// </summary>
    public sealed class Bvr2Campaign : MonoBehaviour
    {
        public const string StartChapterKey = "BVR2_StartChapter";
        public Bvr2Game game;
        public List<Bvr2ChapterBase> chapters = new List<Bvr2ChapterBase>();
        public bool chapterMenu = true;
        public Bvr2ChapterBase Current { get; private set; }

        IEnumerator Start()
        {
            yield return null; // every component has run Start()
            // each chapter offers the next one at its end
            for (int i = 0; i < chapters.Count - 1; i++)
            {
                int next = chapters[i + 1].Number; chapters[i].nextTitle = chapters[i + 1].Title;
                chapters[i].onNextChapter = () => StartChapter(next);
            }
            int forced = PlayerPrefs.GetInt(StartChapterKey, -1); // 0 = the lab prelude
            if (forced >= 0) { PlayerPrefs.DeleteKey(StartChapterKey); PlayerPrefs.Save(); StartChapter(forced); yield break; }
            if (!chapterMenu || chapters.Count < 2) { StartChapter(1); yield break; }
            yield return Menu();
        }

        IEnumerator Menu()
        {
            var v = game.vessel; game.swimmer.Teleport(v.Centre(14f), v.Tangent(14f));
            game.hud.Title("BIOLOGY VR", "путешествие внутри артерии", 3f);
            yield return new WaitForSecondsRealtime(4.2f);
            if (Current) yield break; // a chapter was started meanwhile (debug bridge, tests)
            var options = new string[chapters.Count];
            for (int i = 0; i < chapters.Count; i++) options[i] = chapters[i].Number == 0 ? $"пролог\n«{chapters[i].Title}»" : $"глава {chapters[i].Number}\n«{chapters[i].Title}»";
            yield return game.question.Ask("С какой главы начнём?", options, -1, "ВЫБОР ГЛАВЫ");
            if (Current || game.question.LastChoice < 0) yield break;
            StartChapter(chapters[Mathf.Clamp(game.question.LastChoice, 0, chapters.Count - 1)].Number);
        }

        public void StartChapter(int number) => StartChapter(number, 0);

        /// <summary>Start a chapter at a beat (debug bridge, tests); any other chapter is stopped first.</summary>
        public void StartChapter(int number, int beat)
        {
            Bvr2ChapterBase next = null;
            foreach (var c in chapters) if (c && c.Number == number) next = c;
            if (!next) next = chapters[0];
            foreach (var c in chapters) if (c && c != next) c.Stop();
            Current = next; next.startBeat = beat; next.Begin();
        }
    }
}
