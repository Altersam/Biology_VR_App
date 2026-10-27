using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BiologyVR.V2.Editor
{
    /// <summary>
    /// Automated run of chapter 1 in Play Mode: the chapter's autoplay resolves every step
    /// (gates, scans, samples, questions, anchor, field, spasm) and the run is logged.
    /// Then a short physics check drives the swimmer with virtual input.
    /// Report: Assets/BiologyVR/BiologyVR2/Reports/Chapter1_Autoplay.json (simulated input, not a headset test).
    /// </summary>
    [InitializeOnLoad]
    public static class Bvr2Playtest
    {
        const string Key = "BVR2_Playtest_Running", ChapterKey = "BVR2_Playtest_Chapter";
        static int chapterNo = 1;
        static Bvr2Playtest()
        {
            if (!SessionState.GetBool(Key, false)) return;
            chapterNo = SessionState.GetInt(ChapterKey, 1);
            // re-attach after the domain reload that happens when Play Mode starts
            startedAt = EditorApplication.timeSinceStartup;
            Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
        }

        static string Report => Bvr2SceneBuilder.Root + $"/Reports/Chapter{chapterNo}_Autoplay.json";
        static double startedAt, phaseAt; static int phase; static readonly List<string> log = new List<string>();
        static readonly List<string> errors = new List<string>(); static int frames; static double fpsStart;
        static string lastBeat = ""; static Vector3 swimStart;

        [MenuItem("Biology VR 2/Test Prelude (autoplay)")]
        public static void Run0() => RunChapter(0);

        [MenuItem("Biology VR 2/Test Chapter 1 (autoplay)")]
        public static void Run() => RunChapter(1);

        [MenuItem("Biology VR 2/Test Chapter 2 (autoplay)")]
        public static void Run2() => RunChapter(2);

        [MenuItem("Biology VR 2/Test Chapter 3 (autoplay)")]
        public static void Run3() => RunChapter(3);

        [MenuItem("Biology VR 2/Test Chapter 4 (autoplay)")]
        public static void Run4() => RunChapter(4);

        [MenuItem("Biology VR 2/Test Chapter 5 (autoplay)")]
        public static void Run5() => RunChapter(5);

        [MenuItem("Biology VR 2/Test Chapter 6 (autoplay)")]
        public static void Run6() => RunChapter(6);

        public static void RunChapter(int number)
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[BVR2] stop Play Mode first"); return; }
            chapterNo = number; SessionState.SetInt(ChapterKey, number);
            PlayerPrefs.SetInt(Bvr2Campaign.StartChapterKey, number); PlayerPrefs.Save();
            if (SceneManager.GetActiveScene().path != Bvr2SceneBuilder.ScenePath) EditorSceneManager.OpenScene(Bvr2SceneBuilder.ScenePath);
            log.Clear(); errors.Clear(); phase = 0; phaseAt = 0; lastBeat = "";
            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            Write(true, false);
            Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
            startedAt = EditorApplication.timeSinceStartup;
        }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error) { if (errors.Count < 40) errors.Add(msg + (type == LogType.Exception ? " | " + stack.Split('\n')[0] : "")); }
        }

        static void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - startedAt > 400) { Finish(false, "timeout"); return; }
            if (!EditorApplication.isPlaying) { if (phase > 0 || now - startedAt > 60) Finish(false, "play mode ended"); return; }
            Bvr2ChapterBase ch = null;
            foreach (var c in Object.FindObjectsByType<Bvr2ChapterBase>(FindObjectsSortMode.None)) if (c.Number == chapterNo) ch = c;
            var game = Object.FindFirstObjectByType<Bvr2Game>();
            if (!ch || !game) return;
            switch (phase)
            {
                case 0: // let the intro start for real, measure FPS for a moment
                    if (phaseAt == 0) { phaseAt = now; frames = 0; fpsStart = now; }
                    frames++;
                    if (now - phaseAt > 6) { log.Add($"fps(editor, first 6 s) ≈ {frames / (now - fpsStart):F0}; cells drawn {game.blood.Drawn}"); ch.autoplay = true; ch.Restart(0); phase = 1; phaseAt = now; }
                    break;
                case 1:
                    string b = "beat " + ch.Beat + " · " + ch.Step;
                    if (b != lastBeat) { lastBeat = b; log.Add($"{now - phaseAt:F1}s {b}"); }
                    if (ch.Finished) { log.Add($"finished: stars {ch.Stars}, predictions {ch.CorrectPredictions}, knowledge {game.codex.FoundIn(chapterNo)}/{game.codex.CountIn(chapterNo)} · {ch.StatusLine}"); phase = 2; phaseAt = now; }
                    if (now - phaseAt > 240) Finish(false, "chapter stuck at " + b);
                    break;
                case 2: // physics: swim forward with virtual input
                    ch.autoplay = false; ch.StopAllCoroutines();
                    game.swimmer.Detach();
                    swimStart = game.Head;
                    game.input.Virtual = new Bvr2Input.VirtualInput { active = true, move = new Vector2(0, 1) };
                    phase = 3; phaseAt = now; break;
                case 3:
                    if (now - phaseAt > 2.5)
                    {
                        game.input.Virtual = default;
                        float moved = Vector3.Distance(swimStart, game.Head);
                        log.Add($"swim test: moved {moved:F1} m in 2.5 s; speed over wall {game.swimmer.SpeedOverWall:F1} m/s; inside lumen r={game.swimmer.RadialDistance:F1} of R={game.vessel.Radius(game.swimmer.S):F1}");
                        bool ok = moved > 3f && game.swimmer.RadialDistance < game.vessel.Radius(game.swimmer.S);
                        Finish(ok && ch.Finished, ok ? "ok" : "swim check failed");
                    }
                    break;
            }
        }

        static void Finish(bool passed, string note)
        {
            EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog; SessionState.SetBool(Key, false);
            log.Add("result: " + note);
            Write(false, passed && errors.Count == 0);
            Debug.Log($"[BVR2] Chapter {chapterNo} autoplay {(passed && errors.Count == 0 ? "PASS" : "FAIL")} — {note}; errors {errors.Count}");
            EditorApplication.isPlaying = false;
        }

        static void Write(bool running, bool passed)
        {
            var sb = new StringBuilder();
            sb.Append("{\n  \"utc\": \"").Append(System.DateTime.UtcNow.ToString("o")).Append("\",\n  \"running\": ").Append(running ? "true" : "false")
              .Append(",\n  \"passed\": ").Append(passed ? "true" : "false").Append(",\n  \"note\": \"simulated input in the editor; not a headset test\",\n  \"log\": [");
            for (int i = 0; i < log.Count; i++) sb.Append(i == 0 ? "\n    " : ",\n    ").Append(Json(log[i]));
            sb.Append("\n  ],\n  \"errors\": [");
            for (int i = 0; i < errors.Count; i++) sb.Append(i == 0 ? "\n    " : ",\n    ").Append(Json(errors[i]));
            sb.Append("\n  ]\n}\n");
            File.WriteAllText(Report, sb.ToString());
        }

        static string Json(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ") + "\"";
    }
}
