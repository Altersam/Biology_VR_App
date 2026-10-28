using System.Collections;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// What every chapter shares: beat bookkeeping, the autoplay switch used by tests, and small helpers
    /// for objectives, prompts (VR / PC wording), professor lines and waits. The campaign starts a chapter
    /// with <see cref="Begin"/>; tests and the debug bridge use <see cref="Restart"/>.
    /// </summary>
    public abstract class Bvr2ChapterBase : MonoBehaviour
    {
        public Bvr2Game game;
        public bool autoplay;
        [Tooltip("Debug: start at this beat (0 = from the beginning).")]
        public int startBeat;

        public abstract int Number { get; }
        public abstract string Title { get; }
        public int Beat { get; protected set; } = -1;
        public string Step { get; protected set; } = "";
        public bool Finished { get; protected set; }
        public bool Running => run != null;
        public int Stars { get; protected set; }
        public int CorrectPredictions { get; protected set; }
        public virtual string StatusLine => $"ch{Number} beat {Beat} · {Step} · s={game?.PlayerS:F0} · flow={game?.organism?.LocalFlow:F2} · stars {Stars}";

        /// <summary>Set by the campaign: start the following chapter (null = this is the last one).</summary>
        public System.Action onNextChapter;
        /// <summary>Title of the following chapter for the "what next" choice.</summary>
        public string nextTitle = "";

        protected Coroutine run;
        protected Bvr2Professor Prof => game.professor;
        protected Bvr2Hud Hud => game.hud;
        protected bool XR => game.rig.IsXR;

        /// <summary>Start the chapter from the top (or <see cref="startBeat"/>).</summary>
        public virtual void Begin() { Restart(startBeat); }

        public void Restart(int beat)
        {
            if (run != null) StopCoroutine(run);
            startBeat = beat; Finished = false;
            run = StartCoroutine(RunWrapper());
        }

        /// <summary>Stop and tidy up (switching to another chapter).</summary>
        public void Stop()
        {
            if (run != null) StopCoroutine(run);
            run = null; StopAllCoroutines(); ResetProps(); Beat = -1; Step = "";
        }

        IEnumerator RunWrapper() { yield return Run(); run = null; }

        protected abstract IEnumerator Run();
        protected abstract void ResetProps();

        // ---------------- helpers -----------------------------------------------------------
        protected string C(string vr, string pc) => XR ? vr : pc;
        protected void Objective(string text) { Step = text; Hud.SetObjective(text); }
        protected void Prompt(string vr, string pc) { Hud.SetPrompt(C(vr, pc)); }
        protected void NoPrompt() { Hud.SetPrompt(""); }

        protected IEnumerator Say(string text, Mood m = Mood.Neutral)
        {
            Prof.Say(text, m);
            if (autoplay) { yield return new WaitForSecondsRealtime(0.15f); Prof.Interrupt(); yield break; }
            yield return null;
            while (Prof.Busy) yield return null;
        }

        protected IEnumerator Wait(float seconds) { float t = 0; while (t < (autoplay ? Mathf.Min(seconds, 0.2f) : seconds)) { t += Time.unscaledDeltaTime; yield return null; } }

        protected IEnumerator WaitKnowledge() { yield return null; while (game.KnowledgeBusy || (Prof.Busy && !autoplay)) yield return null; }

        /// <summary>Common tidy-up for every chapter start.</summary>
        protected void ResetShared()
        {
            game.swimmer.Detach(); game.environment.windowOn = false; game.environment.alarm = 0;
            game.environment.windowRagged = 0; game.environment.windowSlit = false; game.environment.windowEdge = Color.clear;
            game.swimmer.externalPull = Vector3.zero; game.organism.leakRate = 0; game.blood.ClearSink();
            game.field.Deactivate(); game.field.scenarioFactor = 1f; game.field.SetFactor(1f); game.field.debugGrab = false;
            game.organism.stressDrive = 0;
            Hud.SetCompass(null, null); Hud.SetPrompt(""); Hud.SetObjective(""); Hud.TopHidden = false; Prof.Interrupt(); Prof.LeaveStage();
            if (game.question) game.question.Cancel();
            game.CancelKnowledge();
            Bvr2Environment.Current?.SetFocus(false); game.blood.ClearFocus();
            Hud.showTemperature = false; game.swimmer.regionMin = game.swimmer.regionMax = 0;
            if (game.immune) game.immune.ResetAll();
            if (game.sync) { game.sync.End(); game.sync.tKiller = false; }
            if (game.antibodies) game.antibodies.specific = false;
            if (game.immune) { game.immune.autoEatDebris = false; game.immune.debrisFade = 0; }
            game.rig.SetOverlay(Color.clear, 0f);
            if (game.antibodies) game.antibodies.unlocked = false;
            if (game.abilities) game.abilities.factorUnlocked = false;
            if (game.factor) { game.factor.stitchMode = false; game.factor.ResetCounters(); }
            game.organism.refillRate = 0; game.organism.lesionConductance = 1f;
            Bvr2Time.Release();
        }

        /// <summary>The end-of-chapter board: title, stars, three "what you learned" cards and why the stars.</summary>
        protected Transform ShowBoard(string title, int stars, string[] heads, string[] bodies, string why)
        {
            var head = game.rig.head.transform;
            Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized; if (fwd.sqrMagnitude < 0.01f) fwd = head.forward;
            var root = new GameObject("Debrief").transform; root.SetParent(game.rig.transform, false);
            root.position = head.position + fwd * 1.7f + Vector3.up * 0.03f; root.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            var canvas = Bvr2Ui.Canvas("Debrief Canvas", root, new Vector2(1700, 900), 0.0009f, 85);
            Bvr2Ui.Backdrop(canvas);
            Hud.TopHidden = true; Bvr2Environment.Current?.SetFocus(true); game.blood.SetFocus(root.position, 1.1f);
            Bvr2Ui.Panel(canvas, new Color(0.01f, 0.03f, 0.05f, 0.92f), "Back");
            Bvr2Ui.Fit(Bvr2Ui.Label(Bvr2Ui.Rect(canvas, "Title", new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -150), new Vector2(-300, -20)), title, 64, Bvr2Style.Text, TextAnchor.MiddleLeft, FontStyle.Bold), 36, 64);
            Bvr2Ui.Label(Bvr2Ui.Rect(canvas, "Stars", new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -150), new Vector2(-40, -20)), new string('★', stars) + new string('☆', 3 - stars), 80, Bvr2Style.Knowledge, TextAnchor.MiddleRight, FontStyle.Bold);
            for (int i = 0; i < heads.Length && i < 3; i++)
            {
                var card = Bvr2Ui.Rect(canvas, "Card " + i, new Vector2(0, 0), new Vector2(0, 0)); card.pivot = new Vector2(0.5f, 0.5f);
                card.sizeDelta = new Vector2(500, 470); card.anchoredPosition = new Vector2(310 + i * 540, 430);
                Bvr2Ui.Panel(card, new Color(0.03f, 0.1f, 0.13f, 0.95f), "Back");
                var h = Bvr2Ui.Label(card, heads[i], 36, Bvr2Style.Ally, TextAnchor.UpperLeft, FontStyle.Bold); h.rectTransform.offsetMin = new Vector2(26, 0); h.rectTransform.offsetMax = new Vector2(-20, -22);
                var b = Bvr2Ui.Label(card, bodies[i], 32, Bvr2Style.Text, TextAnchor.UpperLeft); b.rectTransform.offsetMin = new Vector2(26, 20); b.rectTransform.offsetMax = new Vector2(-20, -90); b.lineSpacing = 1.2f;
                Bvr2Ui.Fit(b, 24, 32);
            }
            Bvr2Ui.Label(Bvr2Ui.Rect(canvas, "Why", new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 40), new Vector2(-40, 150)), why, 30, Bvr2Style.TextDim, TextAnchor.MiddleCenter);
            game.sound.Play("SFX_Stage_Complete", 1f);
            return root;
        }

        protected IEnumerator BoardIn(Transform root)
        {
            for (float k = 0; k < 1f && root; k += Time.unscaledDeltaTime / 0.35f) { root.localScale = Vector3.one * Mathf.SmoothStep(0.01f, 1f, k); yield return null; }
            if (root) root.localScale = Vector3.one;
        }

        protected IEnumerator BoardOut(Transform root)
        {
            for (float k = 0; k < 1f && root; k += Time.unscaledDeltaTime / 0.3f) { root.localScale = Vector3.one * Mathf.Lerp(1f, 0.01f, k); yield return null; }
            if (root) root.gameObject.SetActive(false);
            Hud.TopHidden = false; Bvr2Environment.Current?.SetFocus(false); game.blood.ClearFocus();
        }

        /// <summary>Wait for the trigger (or a timeout) with a "continue" prompt.</summary>
        protected IEnumerator WaitContinue(float timeout = 30f)
        {
            Prompt("Курок — продолжить", "ЛКМ — продолжить");
            for (float t = 0; t < timeout && !autoplay; t += Time.unscaledDeltaTime) { if (t > 1.2f && game.input.ToolDown) break; yield return null; }
            NoPrompt();
        }

        /// <summary>End-of-chapter choice: next chapter / replay / free swim. Returns after acting on it.</summary>
        protected IEnumerator WhatNext(string freeSwimLine)
        {
            bool hasNext = onNextChapter != null;
            var options = hasNext ? new[] { $"глава {Number + 1} «{nextTitle}»", "пройти заново", "плавать свободно" } : new[] { "пройти главу заново", "плавать свободно" };
            yield return game.question.Ask("Что дальше?", options, -1);
            int choice = game.question.LastChoice - (hasNext ? 1 : 0);
            if (game.question.LastChoice < 0) yield break;
            if (choice < 0) onNextChapter();
            else if (choice == 0) { game.codex.ResetChapter(Number); CorrectPredictions = 0; Restart(0); }
            else { Objective("Свободное плавание"); Prof.Say(freeSwimLine, Mood.Smile); }
        }

        /// <summary>Fade through white and place the player (chapter openings, retries).</summary>
        protected void Arrive(float s, float fade = 1f)
        {
            var v = game.vessel;
            game.swimmer.Teleport(v.Centre(s), v.Tangent(s));
            game.rig.Fade(Color.white, 1f, 50f); game.rig.FadeTo(Color.white, 0f, fade);
        }
    }
}
