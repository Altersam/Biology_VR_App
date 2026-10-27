using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.V2
{
    /// <summary>
    /// Chapter 6 «Гомеостаз» (scenes 16–18): a calm flight to the exit through six sensor rings — pulse,
    /// pressure, oxygen, temperature, glucose, acidity. Each ring reads the live value against the norm and
    /// against the start of the journey. At the pressure ring a small experiment shows negative feedback
    /// (squeeze the vessel → pressure up → the heart slows by itself). All six in norm = homeostasis, a dynamic
    /// balance. Then the exit ring, the final report for the whole run and free swimming.
    /// </summary>
    public sealed class Bvr2Chapter6 : Bvr2ChapterBase
    {
        public override int Number => 6;
        public override string Title => "Гомеостаз";

        public float startS = 2262f;
        public List<Bvr2Gate> sensors = new List<Bvr2Gate>();
        public Bvr2Gate exitGate;
        public Bvr2Campaign campaign;

        public int Read { get; private set; }
        public float FeedbackHrDrop { get; private set; }
        public override string StatusLine => base.StatusLine + $" · read {Read}/6 · hr {game.organism.heartRate:F0} · sys {game.organism.systolic:F0} · t {game.organism.temperature:F1} · drop {FeedbackHrDrop:F1}";

        static readonly float[] SensorS = { 2284f, 2302f, 2322f, 2342f, 2360f, 2378f };
        static readonly float[] SensorR = { 1.2f, 2.2f, 2.4f, 2f, 2.5f, 1.6f };
        static readonly float[] SensorA = { 0f, 120f, 240f, 60f, 300f, 180f };
        Transform board, readout; readonly List<Transform> panels = new List<Transform>();

        void Start() { HideProps(); }

        void HideProps()
        {
            foreach (var g in sensors) if (g) g.gameObject.SetActive(false);
            if (exitGate) exitGate.gameObject.SetActive(false);
            foreach (var p in panels) if (p) Destroy(p.gameObject);
            panels.Clear();
        }

        protected override void ResetProps()
        {
            ResetShared();
            HideProps();
            if (board) Destroy(board.gameObject);
            if (game.abilities) game.abilities.SetMode(Bvr2Abilities.Mode.Attract, false);
            game.bioTool.locked = false;
            Read = 0; FeedbackHrDrop = 0;
        }

        void Update()
        {
            if (!Running) return;
            if (Time.frameCount % 15 == 0) Hud.SetCounters($"ЗНАНИЯ {game.codex.FoundIn(6)}/{game.codex.CountIn(6)}", $"ДАТЧИКИ {Read}/6");
        }

        // ---------------- the chapter -------------------------------------------------------
        protected override IEnumerator Run()
        {
            ResetProps();
            var o = game.organism; o.ResetState();
            // the fever of the battle is still fading when we start
            o.temperature = 37.3f;
            Hud.showTemperature = true;
            game.scanner.locked = false;
            game.field.unlocked = true;
            game.sound.Mood("calm");
            for (int i = 0; i < sensors.Count && i < SensorS.Length; i++) { var g = sensors[i]; g.s = SensorS[i]; Place(g.transform, SensorS[i], SensorR[i], SensorA[i]); g.ResetGate(); g.isNext = false; g.gameObject.SetActive(true); }
            if (startBeat <= 0) yield return Intro();
            else { Arrive(startBeat >= 3 ? SensorS[3] - 10f : startS); Prof.Appear(true); }
            if (startBeat <= 1) yield return SensorsA();
            if (startBeat <= 2) yield return SensorsB();
            if (startBeat <= 3) yield return Exit();
            yield return Report();
        }

        void Place(Transform t, float s, float r, float angle)
        {
            var v = game.vessel; v.Frame(s, out _, out var tan, out var n, out _);
            t.position = v.PointAt(s, r, angle); t.rotation = Quaternion.LookRotation(tan, n);
        }

        IEnumerator Intro()
        {
            Beat = 0;
            Arrive(startS, 1.2f);
            game.sound.Play("SFX_Transition", 0.9f);
            Hud.Title("ГЛАВА 6 · ГОМЕОСТАЗ", "всё ли вернулось в норму?", 4f);
            Prof.Appear(true);
            yield return Wait(2.5f);
            yield return Say("Инфекция побеждена. Плывём к выходу — по пути шесть датчиков. Проверим, вернулся ли организм в норму.", Mood.Smile);
        }

        IEnumerator SensorsA()
        {
            Beat = 1;
            yield return ReadSensor(0);
            yield return ReadSensor(1);
            yield return Feedback();
            yield return ReadSensor(2);
        }

        IEnumerator SensorsB()
        {
            Beat = 2;
            if (Read < 3) Read = 3;
            yield return ReadSensor(3);
            game.Reveal("thermoregulation", sensors[3].transform.position);
            yield return WaitKnowledge();
            yield return ReadSensor(4);
            game.Reveal("glucose", sensors[4].transform.position);
            yield return WaitKnowledge();
            yield return ReadSensor(5);
            yield return Say("Все шесть — в норме. Заметь: ни один показатель не стоит на месте, они чуть колеблются — и всё время возвращаются.", Mood.Smile);
            game.Reveal("homeostasis", game.Head + game.rig.head.transform.forward * 2f);
            yield return WaitKnowledge();
        }

        static readonly string[] Names = { "ПУЛЬС", "ДАВЛЕНИЕ", "КИСЛОРОД (SpO₂)", "ТЕМПЕРАТУРА", "ГЛЮКОЗА", "КИСЛОТНОСТЬ (pH)" };

        /// <summary>Swim through the ring: it reads the value, compares it with the norm and with the start.</summary>
        IEnumerator ReadSensor(int i)
        {
            var g = sensors[i]; g.isNext = true; g.gameObject.SetActive(true);
            Objective($"Датчик {i + 1}/6 · {Names[i].ToLowerInvariant()}: проплыви сквозь кольцо");
            if (i == 0) Prompt("Левый стик — плыть", "W A S D — плыть");
            while (!g.passed)
            {
                g.Track(game.Head);
                Hud.SetCompass(g.transform.position, "датчик");
                if (autoplay) g.ForcePass();
                if (!g.passed && game.PlayerS > g.s + 6f) { g.s = game.PlayerS + 14f; Place(g.transform, g.s, SensorR[i] * 0.5f, SensorA[i]); }
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            Read++;
            game.fx.Shock(g.transform.position, g.transform.forward, Bvr2Style.Good, 0.5f, 3f, 0.6f);
            game.sound.Play("BVR2_Gate", 0.8f, 1f + Read * 0.06f);
            Bvr2Haptics.Both(0.35f, 0.08f);
            ShowReadout(i);
            for (float t = 0; t < (autoplay ? 0.1f : 1.4f); t += Time.unscaledDeltaTime) yield return null;
        }

        /// <summary>A small panel in front of the player with the reading (stays a few seconds, then fades).</summary>
        void ShowReadout(int i)
        {
            var o = game.organism;
            string value, norm, start; bool ok;
            switch (i)
            {
                case 0: value = $"{Mathf.RoundToInt(o.heartRate)} уд/мин"; norm = "норма 60–90"; start = "в начале: 72"; ok = o.heartRate >= 60 && o.heartRate <= 90; break;
                case 1: value = $"{o.PressureText} мм рт. ст."; norm = "норма ≈ 120/80"; start = "в начале: 120/80"; ok = o.systolic >= 100 && o.systolic <= 135; break;
                case 2: value = $"{Mathf.RoundToInt(o.spo2)} %"; norm = "норма 95–100 %"; start = "в начале: 98 %"; ok = o.spo2 >= 95; break;
                case 3: value = $"{o.temperature:0.0} °C"; norm = "норма 36,0–37,0"; start = "в бою было до 39"; ok = o.temperature <= 37.2f; break;
                case 4: value = $"{o.glucose:0.0} ммоль/л"; norm = "норма 3,3–5,5"; start = "в начале: 4,9"; ok = o.glucose >= 3.3f && o.glucose <= 5.5f; break;
                default: value = $"{o.pH:0.00}"; norm = "норма 7,35–7,45"; start = "в начале: 7,40"; ok = o.pH >= 7.35f && o.pH <= 7.45f; break;
            }
            var head = game.rig.head.transform;
            Vector3 f = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized; if (f.sqrMagnitude < 0.01f) f = head.forward;
            var root = new GameObject("Readout " + Names[i]).transform; root.SetParent(game.rig.transform, false);
            root.position = head.position + f * 1.5f + Vector3.up * 0.32f; root.rotation = Quaternion.LookRotation(f, Vector3.up);
            var canvas = Bvr2Ui.Canvas("Canvas", root, new Vector2(760, 250), 0.001f, 80);
            Bvr2Ui.Panel(canvas, new Color(0.01f, 0.04f, 0.06f, 0.88f), "Back");
            var bar = Bvr2Ui.Panel(canvas, ok ? Bvr2Style.Good : Bvr2Style.Danger, "Bar"); bar.rectTransform.anchorMax = new Vector2(0, 1); bar.rectTransform.offsetMax = new Vector2(10, 0);
            Bvr2Ui.Label(Bvr2Ui.Rect(canvas, "Name", new Vector2(0, 0.66f), new Vector2(1, 1), new Vector2(34, 0), new Vector2(-20, -10)), $"ДАТЧИК {i + 1} · {Names[i]}", 34, Bvr2Style.Ally, TextAnchor.MiddleLeft, FontStyle.Bold);
            Bvr2Ui.Label(Bvr2Ui.Rect(canvas, "Value", new Vector2(0, 0.28f), new Vector2(0.62f, 0.7f), new Vector2(34, 0), new Vector2(0, 0)), value, 62, Bvr2Style.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            Bvr2Ui.Label(Bvr2Ui.Rect(canvas, "Status", new Vector2(0.62f, 0.28f), new Vector2(1, 0.7f), new Vector2(0, 0), new Vector2(-24, 0)), ok ? "✓ в норме" : "≠ вне нормы", 44, ok ? Bvr2Style.Good : Bvr2Style.Danger, TextAnchor.MiddleRight, FontStyle.Bold);
            Bvr2Ui.Label(Bvr2Ui.Rect(canvas, "Norm", new Vector2(0, 0), new Vector2(1, 0.3f), new Vector2(34, 8), new Vector2(-24, 0)), $"{norm}   ·   {start}", 30, Bvr2Style.TextDim, TextAnchor.MiddleLeft);
            panels.Add(root);
            StartCoroutine(FadeReadout(root, autoplay ? 0.3f : 4.5f));
        }

        IEnumerator FadeReadout(Transform root, float hold)
        {
            for (float k = 0; k < 1f && root; k += Time.unscaledDeltaTime / 0.25f) { root.localScale = Vector3.one * Mathf.SmoothStep(0.2f, 1f, k); yield return null; }
            for (float t = 0; t < hold && root; t += Time.unscaledDeltaTime) yield return null;
            for (float k = 0; k < 1f && root; k += Time.unscaledDeltaTime / 0.3f) { root.localScale = Vector3.one * Mathf.Lerp(1f, 0.01f, k); yield return null; }
            if (root) { panels.Remove(root); Destroy(root.gameObject); }
        }

        /// <summary>Negative feedback, live: narrow the vessel, pressure rises, the heart slows down by itself.</summary>
        IEnumerator Feedback()
        {
            var o = game.organism; var f = game.field;
            yield return Say("Давай проверим, как организм держит давление. Сейчас ты сузишь сосуд.", Mood.Neutral);
            if (autoplay) game.question.ForcedChoice = 0;
            yield return game.question.Ask("Давление поднялось. Что сделает сердце?", new[] { "забьётся реже", "забьётся чаще", "остановится" }, 0);
            bool right = game.question.LastChoice == 0; if (right) CorrectPredictions++;
            f.unlocked = true; f.Activate(game.PlayerS + 6f, 12f);
            Objective("Сожми сосуд и смотри на пульс и давление");
            Prompt("Зажми оба хвата и сведи руки — сжать", "Колесо вниз / Z — сжать сосуд");
            float hr0 = o.heartRate, held = 0, sys0 = o.systolic;
            while (held < (autoplay ? 0.6f : 5f))
            {
                if (autoplay) { f.SetFactor(0.7f); f.debugGrab = true; }
                if (f.Effective <= 0.82f) held += Time.unscaledDeltaTime * (autoplay ? 8f : 1f);
                FeedbackHrDrop = Mathf.Max(FeedbackHrDrop, hr0 - o.heartRate);
                Hud.SetObjective($"Сожми сосуд  ·  давление {o.PressureText} (было {Mathf.RoundToInt(sys0)})  ·  пульс {Mathf.RoundToInt(o.heartRate)} (было {Mathf.RoundToInt(hr0)})");
                yield return null;
            }
            // let the reflex play out a little longer, then show it
            for (float t = 0; t < (autoplay ? 0.2f : 2.5f); t += Time.unscaledDeltaTime)
            {
                FeedbackHrDrop = Mathf.Max(FeedbackHrDrop, hr0 - o.heartRate);
                Hud.SetObjective($"Давление {o.PressureText}  ·  пульс {Mathf.RoundToInt(o.heartRate)} (было {Mathf.RoundToInt(hr0)})");
                yield return null;
            }
            NoPrompt();
            game.Reveal("feedback", game.Head + game.rig.head.transform.forward * 2f);
            yield return WaitKnowledge();
            yield return Say(right ? "Как ты и предсказал: давление выросло — сердце само забилось реже. Отпусти сосуд." : "Сердце забилось реже — само, без команды. Давление выросло, и организм ответил против отклонения. Отпусти сосуд.", Mood.Smile);
            f.SetFactor(1f); f.debugGrab = false; f.Deactivate();
        }

        IEnumerator Exit()
        {
            Beat = 3;
            if (Read < 6) Read = 6;
            var g = exitGate; float s = SensorS[5] + 14f;
            g.s = s; Place(g.transform, s, 0f, 0f); g.ResetGate(); g.isNext = true; g.gameObject.SetActive(true);
            g.transform.localScale = Vector3.one * g.radius;
            Objective("Плыви к выходу");
            yield return Say("Вон выход. Плыви в кольцо — возвращаемся в лабораторию.", Mood.Smile);
            while (!g.passed)
            {
                g.Track(game.Head);
                Hud.SetCompass(g.transform.position, "выход");
                if (autoplay) g.ForcePass();
                if (!g.passed && game.PlayerS > g.s + 6f) { g.s = game.PlayerS + 12f; Place(g.transform, g.s, 0f, 0f); }
                yield return null;
            }
            Hud.SetCompass(null, null);
            game.sound.Play("BVR2_Victory", 0.9f);
            game.rig.Fade(Color.white, 1f, 2.5f);
            for (float t = 0; t < (autoplay ? 0.1f : 1.2f); t += Time.unscaledDeltaTime) yield return null;
            Hud.Title("МИССИЯ ВЫПОЛНЕНА", "пациент здоров", 3.5f);
            game.rig.FadeTo(Color.white, 0f, 0.8f);
        }

        IEnumerator Report()
        {
            Beat = 4;
            Objective("Итоговый отчёт");
            Stars = 1 + (CorrectPredictions > 0 ? 1 : 0) + (Read >= 6 ? 1 : 0);
            board = FinalBoard();
            yield return BoardIn(board);
            yield return Say("Ты видел изнутри, как организм держит равновесие. Спасибо, коллега!", Mood.Smile);
            yield return WaitContinue();
            yield return BoardOut(board);
            if (autoplay) game.question.ForcedChoice = 0;
            yield return game.question.Ask("Что такое гомеостаз?", new[] { "постоянство внутренней среды организма", "быстрый рост клеток", "свёртывание крови" }, 0, "ПРОВЕРЬ СЕБЯ");
            yield return Say(game.question.LastChoice == 0 ? "Верно. И держится оно не само — его всё время поддерживает весь организм." : "Гомеостаз — это постоянство внутренней среды: температуры, давления, состава крови.", Mood.Neutral);
            Finished = true;
            Objective("Путешествие завершено");
            if (autoplay) yield break;
            if (board) { Destroy(board.gameObject); board = null; }
            yield return WhatNext("Плавай свободно — организм в равновесии.");
        }

        /// <summary>The report for the whole run: stars of each chapter played, knowledge found.</summary>
        Transform FinalBoard()
        {
            var head = game.rig.head.transform;
            Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized; if (fwd.sqrMagnitude < 0.01f) fwd = head.forward;
            var root = new GameObject("Final Report").transform; root.SetParent(game.rig.transform, false);
            root.position = head.position + fwd * 1.7f + Vector3.up * 0.03f; root.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            var canvas = Bvr2Ui.Canvas("Report Canvas", root, new Vector2(1500, 960), 0.0009f, 85);
            Bvr2Ui.Backdrop(canvas);
            Hud.TopHidden = true; Bvr2Environment.Current?.SetFocus(true); game.blood.SetFocus(root.position, 1.1f);
            Bvr2Ui.Panel(canvas, new Color(0.01f, 0.03f, 0.05f, 0.93f), "Back");
            Bvr2Ui.Label(Bvr2Ui.Rect(canvas, "Title", new Vector2(0, 1), new Vector2(1, 1), new Vector2(50, -140), new Vector2(-50, -20)), "ИТОГОВЫЙ ОТЧЁТ · ПУТЕШЕСТВИЕ ПО АРТЕРИИ", 52, Bvr2Style.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            var chapters = campaign ? campaign.chapters : new List<Bvr2ChapterBase>();
            int total = 0, max = 0, row = 0;
            foreach (var c in chapters)
            {
                if (!c) continue;
                bool done = c.Finished || c == this; int st = c == this ? Stars : c.Finished ? c.Stars : 0;
                total += st; max += 3;
                var r = Bvr2Ui.Rect(canvas, "Row " + row, new Vector2(0, 1), new Vector2(1, 1), new Vector2(50, -220 - row * 92), new Vector2(-50, -140 - row * 92));
                Bvr2Ui.Panel(r, new Color(0.03f, 0.1f, 0.13f, row % 2 == 0 ? 0.9f : 0.7f), "Back");
                Bvr2Ui.Label(Bvr2Ui.Rect(r, "Name", new Vector2(0, 0), new Vector2(0.7f, 1), new Vector2(26, 0), Vector2.zero), $"Глава {c.Number} · «{c.Title}»", 40, done ? Bvr2Style.Text : Bvr2Style.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);
                Bvr2Ui.Label(Bvr2Ui.Rect(r, "Stars", new Vector2(0.7f, 0), new Vector2(1, 1), Vector2.zero, new Vector2(-26, 0)), done ? new string('★', st) + new string('☆', 3 - st) : "не пройдена", done ? 50 : 32, done ? Bvr2Style.Knowledge : Bvr2Style.TextDim, TextAnchor.MiddleRight, FontStyle.Bold);
                row++;
            }
            string summary = $"Звёзды: {total} из {max}   ·   Знания в справочнике: {game.codex.FoundCount} из {game.codex.entries.Count}   ·   Показатели организма: в норме";
            Bvr2Ui.Fit(Bvr2Ui.Label(Bvr2Ui.Rect(canvas, "Summary", new Vector2(0, 0), new Vector2(1, 0), new Vector2(50, 40), new Vector2(-50, 130)), summary, 34, Bvr2Style.Good, TextAnchor.MiddleCenter, FontStyle.Bold), 24, 34);
            game.sound.Play("SFX_Stage_Complete", 1f);
            return root;
        }

        // ---------------- debug hooks -------------------------------------------------------
        public void DebugLookAtSensor(int i) { if (i >= 0 && i < sensors.Count && sensors[i]) game.rig.LookAt(sensors[i].transform.position); }
        public void DebugPass(int i) { if (i >= 0 && i < sensors.Count && sensors[i]) sensors[i].ForcePass(); }
        public void DebugReadout(int i) { ShowReadout(i); }
        public void DebugChoose(int option) { game.question.ForcedChoice = option; }
        public void DebugSqueeze(float f) { game.field.SetFactor(f); game.field.debugGrab = true; }
    }
}
