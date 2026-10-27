using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.V2
{
    /// <summary>
    /// Chapter 1 «Поток» (scenes 03–05 of the script): release into the bloodstream, learn to swim,
    /// study the blood (scanner + attraction), the wall layers (anchor + cut-away), the link between
    /// lumen and flow (predict → try with the vascular field), and the climax: a spasm to fight through.
    /// Pacing follows the player: episodes are placed ahead of wherever the current has carried you.
    /// </summary>
    public sealed class Bvr2Chapter1 : Bvr2ChapterBase
    {
        public override int Number => 1;
        public override string Title => "Поток";
        public List<Bvr2Gate> tutorialGates = new List<Bvr2Gate>();
        public List<Bvr2Gate> raceGates = new List<Bvr2Gate>();
        public Transform anchorPad, exhibit, endBeacon;
        public Bvr2Scannable intima, media, adventitia;
        public float startS = 14f;
        public float SpasmGoodShare { get; private set; }

        readonly Dictionary<CellKind, int> samples = new Dictionary<CellKind, int>();
        float chapterStart;

        void Start()
        {
            foreach (var g in tutorialGates) if (g) g.gameObject.SetActive(false);
            foreach (var g in raceGates) if (g) g.gameObject.SetActive(false);
            if (exhibit) exhibit.gameObject.SetActive(false);
            if (anchorPad) anchorPad.gameObject.SetActive(false);
            if (endBeacon) endBeacon.gameObject.SetActive(false);
            game.bioTool.OnSample += (k, p) => { if (Running) { samples.TryGetValue(k, out int n); samples[k] = n + 1; Counters(); } };
        }

        void Counters()
        {
            int total = game.codex.CountIn(1);
            samples.TryGetValue(CellKind.RedCell, out int r); samples.TryGetValue(CellKind.WhiteCell, out int w); samples.TryGetValue(CellKind.Platelet, out int p);
            Hud.SetCounters($"ЗНАНИЯ {game.codex.FoundIn(1)}/{total}", $"ОБРАЗЦЫ {r + w + p}");
        }

        void Update() { if (Running && Time.frameCount % 15 == 0 && game && game.codex) Counters(); }

        protected override void ResetProps()
        {
            foreach (var g in tutorialGates) if (g) g.gameObject.SetActive(false);
            foreach (var g in raceGates) if (g) g.gameObject.SetActive(false);
            if (exhibit) exhibit.gameObject.SetActive(false);
            if (anchorPad) anchorPad.gameObject.SetActive(false);
            HideExhibit();
            game.swimmer.Detach(); game.environment.windowOn = false; game.environment.alarm = 0;
            game.field.Deactivate(); game.field.scenarioFactor = 1f; game.field.SetFactor(1f); game.organism.stressDrive = 0;
            Hud.SetCompass(null, null); Hud.SetPrompt(""); Hud.SetObjective(""); Prof.Interrupt();
            if (game.question) game.question.Cancel();
            game.CancelKnowledge();
            if (debriefRoot) Destroy(debriefRoot.gameObject);
            Hud.TopHidden = false; Bvr2Environment.Current?.SetFocus(false); game.blood.ClearFocus();
            Bvr2Time.Release();
        }

        readonly List<GameObject> labels = new List<GameObject>();
        Transform debriefRoot;

        void LayerLabel(Bvr2Scannable layer)
        {
            var mf = layer.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) return;
            var b = mf.sharedMesh.bounds; var lp = layer.transform.localPosition;
            int idx = layer == intima ? 0 : layer == media ? 1 : 2;
            var head = game.rig.head.transform;
            var canvas = Bvr2Ui.Canvas("Label " + layer.codexId, exhibit, new Vector2(460, 84), 0.00072f / Mathf.Max(exhibit.lossyScale.x, 0.01f), 88);
            canvas.position = exhibit.TransformPoint(new Vector3(b.center.x, b.min.y - 0.07f - idx * 0.11f, lp.z + b.max.z - 0.07f));
            canvas.rotation = Quaternion.LookRotation(canvas.position - head.position, Vector3.up);
            Bvr2Ui.Panel(canvas, Bvr2Style.Panel, "Back", Bvr2Ui.Rounded);
            var e = game.codex.Get(layer.codexId);
            var col = e != null ? e.color : Bvr2Style.Text;
            var bar = Bvr2Ui.Rect(canvas, "Bar", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 8), new Vector2(10, -8));
            Bvr2Ui.Panel(bar, col, "Fill");
            var t = Bvr2Ui.Label(canvas, e != null ? e.title : layer.displayName, 40, col, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.offsetMin = new Vector2(18, 0);
            labels.Add(canvas.gameObject);
            StartCoroutine(Flash(layer.GetComponent<Renderer>(), col, 6f));
        }

        IEnumerator Flash(Renderer r, Color c, float seconds)
        {
            if (!r) yield break; var b = new MaterialPropertyBlock();
            for (float t = 0; t < seconds && r; t += Time.unscaledDeltaTime)
            {
                float k = (0.35f + 0.25f * Mathf.Sin(t * 5f)) * Mathf.Clamp01((seconds - t) / 0.8f) * Mathf.Clamp01(t / 0.3f);
                r.GetPropertyBlock(b); b.SetColor("_Emission", c * k); r.SetPropertyBlock(b); yield return null;
            }
            if (r) { r.GetPropertyBlock(b); b.SetColor("_Emission", Color.black); r.SetPropertyBlock(b); }
        }

        // ---------------- the chapter ---------------------------------------------------------
        protected override IEnumerator Run()
        {
            chapterStart = Time.unscaledTime;
            ResetProps();
            var o = game.organism; o.ResetState();
            game.field.unlocked = startBeat >= 3; game.field.Deactivate(); game.field.scenarioFactor = 1; game.field.SetFactor(1);
            game.environment.alarm = 0; game.environment.windowOn = false;
            game.scanner.locked = startBeat == 0; game.bioTool.locked = startBeat <= 1;
            game.scanner.allowPlasma = false;
            game.sound.Mood("calm");
            if (startBeat <= 0) { yield return Intro(); yield return Tutorial(); }
            else { game.swimmer.Teleport(game.vessel.Centre(startS + 120f * startBeat), game.vessel.Tangent(startS + 120f * startBeat)); Prof.Appear(true); game.scanner.locked = false; }
            if (startBeat <= 1) yield return Blood();
            if (startBeat <= 2) yield return Wall();
            if (startBeat <= 3) yield return Flow();
            if (startBeat <= 4) yield return Spasm();
            yield return Debrief();
        }

        IEnumerator Intro()
        {
            Beat = 0;
            var v = game.vessel;
            game.swimmer.Teleport(v.Centre(startS), v.Tangent(startS));
            game.rig.Fade(Color.white, 1f, 50f); game.rig.FadeTo(Color.white, 0f, 0.5f);
            game.sound.Play("SFX_Transition", 0.9f);
            Hud.Title("ГЛАВА 1 · ПОТОК", "кровь, стенка артерии и кровоток", 5f);
            Objective("Осмотрись");
            yield return Wait(2.2f);
            Prof.Appear(true);
            game.sound.Play("SFX_Reveal", 0.7f);
            yield return Wait(1.0f);
            yield return Say("Получилось! Ты уменьшился до размера клетки и попал в артерию. Слышишь? Это сердце.", Mood.Smile);
            yield return Say("Вокруг — кровь. Течение несёт тебя вперёд, но плыть ты можешь сам.", Mood.Neutral);
        }

        IEnumerator Tutorial()
        {
            Beat = 0;
            var v = game.vessel;
            float[] ss = { 36, 56, 76, 96, 116, 140 };
            float[] rr = { 0, 2.6f, 2.8f, 2.6f, 1.6f, 3.0f };
            float[] aa = { 0, 90, 0, 210, 300, 160 };
            for (int i = 0; i < tutorialGates.Count && i < ss.Length; i++)
            {
                var g = tutorialGates[i]; g.s = ss[i];
                Place(g.transform, ss[i], rr[i], aa[i]); g.ResetGate(); g.isNext = i == 0;
            }
            Objective("Проплыви сквозь кольца");
            Prompt("Левый стик — плыть  ·  правый стик ↑↓ — вверх / вниз", "W A S D — плыть  ·  Пробел / Ctrl — вверх / вниз  ·  мышь — обзор");
            int passed = 0;
            for (int i = 0; i < tutorialGates.Count; i++)
            {
                var g = tutorialGates[i]; g.isNext = true;
                if (i + 1 < tutorialGates.Count) tutorialGates[i + 1].gameObject.SetActive(true);
                if (i == 4) { Prompt("A или X — рывок", "Shift — рывок"); Prof.Say("Последние кольца далеко. Попробуй рывок!", Mood.Smile); }
                while (!g.passed)
                {
                    g.Track(game.Head);
                    Hud.SetCompass(g.transform.position, "кольцо");
                    if (autoplay) g.ForcePass();
                    // missed it? the current carried you past – move the ring ahead of you
                    if (!g.passed && game.PlayerS > g.s + 6f) { g.s = game.PlayerS + 18f; Place(g.transform, g.s, rr[i] * 0.6f, aa[i]); }
                    yield return null;
                }
                passed++;
                game.fx.Shock(g.transform.position, g.transform.forward, Bvr2Style.Good, 0.5f, 3f, 0.6f);
                game.fx.Burst(g.transform.position, Bvr2Style.Ally, 30, 3f, 0.06f, 0.7f);
                game.sound.Play("BVR2_Gate", 0.8f, 1f + passed * 0.06f);
                Bvr2Haptics.Both(0.4f, 0.08f);
                Hud.Toast($"{passed} / {tutorialGates.Count}", Bvr2Style.Good, 1f);
                g.isNext = false;
            }
            Hud.SetCompass(null, null); NoPrompt();
            yield return Say("Отлично плаваешь! Видишь, как тебя то подталкивает, то отпускает? Это толчки сердца.", Mood.Smile);
        }

        IEnumerator Blood()
        {
            Beat = 1;
            var blood = game.blood;
            Objective("Изучи кровь");
            yield return Say("Сначала наблюдаем. Потом делаем вывод. Чего здесь больше всего?", Mood.Neutral);
            var ask = new[] { "эритроцитов", "лейкоцитов", "тромбоцитов" };
            if (autoplay) game.question.ForcedChoice = 0;
            yield return game.question.Ask("Каких клеток в крови больше всего?", ask, 0);
            bool right1 = game.question.LastChoice == 0; if (right1) CorrectPredictions++;
            yield return Say(right1 ? "Похоже на то. Сейчас проверим сканером." : "Посмотрим! Сканер всё покажет.", right1 ? Mood.Smile : Mood.Neutral);

            // 1) red cell — scanner introduced
            game.scanner.locked = false;
            Objective("Отсканируй красную клетку");
            Prompt("Левый курок (держи) — сканер", "ПКМ или F (держи) — сканер");
            yield return WaitScan("rbc", () => Pick(CellKind.RedCell));
            yield return WaitKnowledge();
            // 2) leukocyte — rare, glowing, find it
            int w = blood.Inject(CellKind.WhiteCell, 22f, 0.55f, UnityEngine.Random.Range(0f, 360f));
            Objective("Найди белую клетку — лейкоцит");
            yield return Say("Теперь найди охранника. Белая клетка, крупнее остальных. Их совсем мало.", Mood.Neutral);
            yield return WaitScan("wbc", () => Pick(CellKind.WhiteCell), () => w >= 0 && !blood.IsHidden(w) ? blood.PositionOf(w) : (Vector3?)null, "лейкоцит");
            yield return WaitKnowledge();
            // 3) platelet — the smallest
            Objective("Найди самый маленький элемент крови");
            yield return Say("А теперь — самый маленький. Ищи золотистые пластинки.", Mood.Neutral);
            yield return WaitScan("platelet", () => Pick(CellKind.Platelet));
            yield return WaitKnowledge();
            yield return Say(right1 ? "Ты был прав: на один лейкоцит здесь сотни эритроцитов!" : "Видишь? На один лейкоцит здесь сотни эритроцитов.", Mood.Smile);
            // 4) plasma — what is between the cells
            game.scanner.allowPlasma = true;
            Objective("Отсканируй то, что между клетками");
            yield return Say("А что же между клетками? Направь сканер в пустоту между ними.", Mood.Serious);
            yield return WaitScan("plasma", () => game.Head + game.rig.head.transform.forward * 3f);
            yield return WaitKnowledge();
            // 5) attraction — take samples
            game.bioTool.locked = false;
            samples.Clear(); Counters();
            Objective("Возьми образцы: эритроцит, лейкоцит, тромбоцит");
            Prompt("Правый курок (держи) — Притяжение: тянет клетку к тебе", "ЛКМ (держи) — Притяжение: тянет клетку к тебе");
            yield return Say("Включаю твоё Притяжение. Возьми по образцу каждого вида — для лаборатории.", Mood.Smile);
            Hud.Toast("+ СПОСОБНОСТЬ · ПРИТЯЖЕНИЕ", Bvr2Style.Ally, 2.4f);
            game.sound.Play("SFX_Reveal", 0.8f);
            int w2 = -1;
            while (!(Has(CellKind.RedCell) && Has(CellKind.WhiteCell) && Has(CellKind.Platelet)))
            {
                string need = !Has(CellKind.RedCell) ? "эритроцит" : !Has(CellKind.WhiteCell) ? "лейкоцит" : "тромбоцит";
                Hud.SetObjective($"Возьми образцы: {Mark(CellKind.RedCell)} эритроцит  {Mark(CellKind.WhiteCell)} лейкоцит  {Mark(CellKind.Platelet)} тромбоцит");
                if (!Has(CellKind.WhiteCell))
                {
                    if (w2 < 0 || blood.IsHidden(w2) || Vector3.Distance(blood.PositionOf(w2), game.Head) > 45f) w2 = blood.Inject(CellKind.WhiteCell, 16f, 0.5f, UnityEngine.Random.Range(0f, 360f));
                    if (w2 >= 0) Hud.SetCompass(blood.PositionOf(w2), "лейкоцит");
                }
                else Hud.SetCompass(null, null);
                if (autoplay) { var k = !Has(CellKind.RedCell) ? CellKind.RedCell : !Has(CellKind.WhiteCell) ? CellKind.WhiteCell : CellKind.Platelet; game.bioTool.ForceSample(k, game.Head); }
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            game.sound.Play("SFX_Stage_Complete", 0.8f);
            yield return Say("Образцы у нас. Кровь — это клетки и плазма. Теперь посмотрим на сам сосуд.", Mood.Smile);
        }

        bool Has(CellKind k) => samples.TryGetValue(k, out int n) && n > 0;
        string Mark(CellKind k) => Has(k) ? "✓" : "○";

        Vector3 Pick(CellKind k)
        {
            var b = game.blood; var head = game.rig.head.transform;
            if (b.Pick(new Ray(head.position, head.forward), 30f, 25f, out int i, out _, out _, (int)k)) return b.PositionOf(i);
            return head.position + head.forward * 4f;
        }

        IEnumerator WaitScan(string id, Func<Vector3> autoPoint, Func<Vector3?> compass = null, string compassName = "")
        {
            bool done = game.codex.Has(id);
            void H(string got, Vector3 at, bool first) { if (got == id) done = true; }
            game.scanner.OnScan += H;
            float stuck = 0;
            while (!done)
            {
                if (compass != null) { var c = compass(); Hud.SetCompass(c, compassName); }
                if (autoplay) game.scanner.ForceScan(id, autoPoint());
                stuck += Time.unscaledDeltaTime;
                if (stuck > 25f && !autoplay) { stuck = 0; Prof.Say("Держи курок, пока кольцо сканера не заполнится.", Mood.Neutral); }
                yield return null;
            }
            game.scanner.OnScan -= H;
            Hud.SetCompass(null, null); NoPrompt();
        }

        IEnumerator Wall()
        {
            Beat = 2;
            var v = game.vessel;
            float sEx = Mathf.Min(game.PlayerS + 24f, v.Length - 60f);
            const float alpha = 270f;
            PlaceExhibit(sEx, alpha);
            anchorPad.gameObject.SetActive(true);
            Objective("Подплыви к маяку у стенки");
            Prompt("Хват (grip) — прицепиться якорем", "E — прицепиться якорем");
            yield return Say("Видишь сияющий маяк у стенки? Подплыви к нему и прицепись якорем.", Mood.Neutral);
            while (!game.swimmer.Anchored)
            {
                float d = Vector3.Distance(game.Head, anchorPad.position);
                Hud.SetCompass(anchorPad.position, "маяк");
                if ((d < 4.5f && game.input.AnchorDown) || autoplay) { game.swimmer.AttachTo(anchorPad); Bvr2Haptics.Both(0.5f, 0.1f); }
                else if (game.input.AnchorDown) Hud.Toast("Подплыви ближе к маяку", Bvr2Style.TextDim, 1.2f);
                if (game.PlayerS > sEx + 10f) { sEx = game.PlayerS + 18f; PlaceExhibit(sEx, alpha); }
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            for (float t = 0; t < 0.9f && !autoplay; t += Time.unscaledDeltaTime) yield return null; // let the magnet glide settle
            anchorPad.gameObject.SetActive(false);
            // a slice of the wall, exploded like a textbook figure, floats in front of the player
            PresentExhibit(alpha);
            exhibit.gameObject.SetActive(true);
            game.fx.Shock(exhibit.position, -game.rig.head.transform.forward, Bvr2Style.Ally, 0.2f, 1.6f, 0.7f);
            game.fx.Burst(exhibit.position + (exhibit.position - game.Head).normalized * 0.8f, Bvr2Style.Ally, 40, 1.5f, 0.04f, 0.9f);
            game.sound.Play("SFX_Reveal", 0.9f);
            var labelled = new HashSet<string>();
            Objective("Отсканируй три оболочки стенки");
            Prompt("Левый курок — сканировать слой", "ПКМ / F — сканировать слой");
            yield return Say("Я вырезал для тебя кусочек стенки. Стенка артерии — три слоя. Изучи каждый.", Mood.Smile);
            bool unlockedField = false;
            while (!(game.codex.Has("intima") && game.codex.Has("media") && game.codex.Has("adventitia")))
            {
                Hud.SetObjective($"Оболочки стенки: {(game.codex.Has("intima") ? "✓" : "○")} внутренняя  {(game.codex.Has("media") ? "✓" : "○")} средняя  {(game.codex.Has("adventitia") ? "✓" : "○")} наружная");
                if (autoplay)
                {
                    foreach (var s in new[] { intima, media, adventitia }) if (!game.codex.Has(s.codexId)) { game.scanner.ForceScan(s.codexId, s.transform.position); break; }
                }
                if (game.codex.Has("media") && !unlockedField) { unlockedField = true; }
                foreach (var l in new[] { intima, media, adventitia }) if (game.codex.Has(l.codexId) && labelled.Add(l.codexId)) LayerLabel(l);
                yield return null;
                if (game.KnowledgeBusy) yield return WaitKnowledge();
            }
            yield return WaitKnowledge();
            game.field.unlocked = true;
            Hud.Toast("+ СПОСОБНОСТЬ · СОСУДИСТОЕ ПОЛЕ", Bvr2Style.Ally, 2.8f);
            game.sound.Play("SFX_Reveal", 0.9f);
            yield return Say("Ты понял, как работает средняя оболочка. Теперь у тебя есть Сосудистое поле — ты можешь сжимать сосуд, как его мышцы.", Mood.Smile);
            HideExhibit();
            game.swimmer.Detach(); anchorPad.gameObject.SetActive(false);
            NoPrompt();
        }

        void PlaceExhibit(float sEx, float alpha)
        {
            var v = game.vessel; float R = v.BaseRadius(sEx);
            anchorPad.position = v.PointAt(sEx, R - 2.2f, alpha);
            anchorPad.rotation = Quaternion.LookRotation(v.PointAt(sEx, R, alpha) - anchorPad.position);
        }

        void PresentExhibit(float alpha)
        {
            var head = game.rig.head.transform; var v = game.vessel; float s = game.PlayerS;
            Vector3 f = (v.PointAt(s, v.Radius(s), alpha) - head.position); f = Vector3.ProjectOnPlane(f, Vector3.up);
            if (f.sqrMagnitude < 0.01f) f = Vector3.ProjectOnPlane(head.forward, Vector3.up); f.Normalize();
            Vector3 up = Vector3.up, z = Vector3.Cross(f, up);
            // turned 30° so the stepped end (cut faces = layer thickness) faces the player
            const float k = 0.65f;
            exhibit.SetParent(exhibitHome ? exhibitHome : exhibit.parent, true);
            exhibit.localScale = Vector3.one * k;
            exhibit.rotation = Quaternion.AngleAxis(-30f, up) * Quaternion.LookRotation(z, up);
            Vector3 sliceCentre = head.position + f * 1.3f - up * 0.06f;
            exhibit.position = sliceCentre - exhibit.rotation * new Vector3(0.78f * k, 0, 0);
            if (!exhibitHome) exhibitHome = exhibit.parent;
            exhibit.SetParent(game.rig.transform, true); // rides with the player (magnet glide, snap turns)
        }

        Transform exhibitHome;
        void HideExhibit()
        {
            if (!exhibit) return;
            foreach (var l in labels) if (l) Destroy(l); labels.Clear();
            exhibit.gameObject.SetActive(false);
            if (exhibitHome) exhibit.SetParent(exhibitHome, true);
        }

        // ---------------- debug hooks (editor bridge, visual checks) ----------------------
        public void DebugAnchor()
        {
            if (!anchorPad || !anchorPad.gameObject.activeInHierarchy) return;
            var p = anchorPad.position; var toC = game.vessel.Centre(game.vessel.Locate(p, ref dbgHint, out _)) - p;
            game.swimmer.Teleport(p + toC.normalized * 1.4f, -toC.normalized); game.swimmer.AttachTo(anchorPad);
        }
        int dbgHint = -1;
        public void DebugScan(string id) { game.scanner.ForceScan(id, game.Head + game.rig.head.transform.forward * 2f); }
        public void DebugChoose(int option) { game.question.ForcedChoice = option; }
        public void DebugField(float factor, bool grab) { game.field.SetFactor(factor); game.field.debugGrab = grab; }
        public void DebugFieldPreview(float factor) { game.field.unlocked = true; game.field.Activate(game.PlayerS + 7f, 10f); game.field.SetFactor(factor); game.field.debugGrab = true; }
        public void DebugBlood(bool on) { game.blood.enabled = on; }
        public void DebugBoost() { game.swimmer.RequestBoost(); }
        public void DebugLook(float yaw, float pitch) { game.rig.LookAt(game.Head + Quaternion.Euler(pitch, yaw, 0) * Vector3.ProjectOnPlane(game.rig.head.transform.forward, Vector3.up).normalized * 10f); }

        IEnumerator Flow()
        {
            Beat = 3;
            var o = game.organism;
            Objective("Понаблюдай за стенкой");
            yield return Say("Посмотри на стенку и дождись удара сердца. Что происходит?", Mood.Neutral);
            int beats = o.Beats; float watched = 0;
            while (!game.codex.Has("pulse"))
            {
                var head = game.rig.head.transform; float s = game.PlayerS;
                Vector3 wall = game.vessel.PointAt(s + 4f, game.vessel.Radius(s), 0);
                bool looking = Vector3.Angle(head.forward, (game.vessel.Centre(s + 6f) - head.position)) > 25f; // looking at a wall, not down the lumen
                if (looking) watched += Time.unscaledDeltaTime;
                if ((o.Beats > beats && watched > 1.2f) || autoplay || watched > 9f) { Bvr2Time.SlowMo(0.25f, 1.5f); game.Reveal("pulse", head.position + head.forward * 2f); }
                beats = o.Beats;
                yield return null;
            }
            yield return WaitKnowledge();
            // predict
            yield return Say("А теперь вопрос посложнее.", Mood.Serious);
            var ask = new[] { "почти столько же (≈ 80 %)", "почти вдвое меньше (≈ 40 %)", "больше — кровь ведь быстрее" };
            if (autoplay) game.question.ForcedChoice = 1;
            yield return game.question.Ask("Сосуд сузили на 20 %. Сколько крови теперь пройдёт?", ask, 1);
            bool right2 = game.question.LastChoice == 1; if (right2) CorrectPredictions++;
            yield return Say(right2 ? "Смелый ответ! Проверим." : "Проверим на опыте!", Mood.Smile);
            // experiment with the field
            var f = game.field; float centre = Mathf.Min(game.PlayerS + 26f, game.vessel.Length - 80f);
            f.minFactor = 0.55f; f.maxFactor = 1.3f; f.SetFactor(1f); f.Activate(centre, 12f);
            Objective("Сузь сосуд Сосудистым полем на 20 %");
            Prompt("Зажми оба хвата: руки вместе — сузить, врозь — расширить", "Колесо мыши или Z / X — сузить / расширить");
            yield return Say("Видишь кольца впереди? Сожми сосуд между ними — и следи за кровотоком на запястье.", Mood.Neutral);
            float hold = 0;
            while (hold < 1.6f)
            {
                Hud.SetObjective($"Сузь сосуд на 20 %:  сейчас {Mathf.RoundToInt((1 - f.Factor) * 100)} %  ·  кровоток {Mathf.RoundToInt(o.LocalFlow * 100)} %");
                if (autoplay) f.SetFactor(0.8f);
                if (f.Factor <= 0.83f && f.Factor >= 0.72f) hold += Time.unscaledDeltaTime; else hold = 0;
                if (f.Factor < 0.72f) Hud.SetPrompt("Слишком сильно! Нужно около 20 %");
                else Prompt("Зажми оба хвата: руки вместе — сузить, врозь — расширить", "Колесо мыши или Z / X — сузить / расширить");
                yield return null;
            }
            NoPrompt();
            game.Reveal("narrowing", game.vessel.Centre(centre));
            yield return WaitKnowledge();
            yield return Say($"Просвет меньше всего на пятую часть, а крови проходит {Mathf.RoundToInt(o.LocalFlow * 100)} %! И давление подросло: сердцу тяжелее.", Mood.Surprised);
            Objective("Верни сосуд в норму");
            while (Mathf.Abs(f.Factor - 1f) > 0.05f)
            {
                if (autoplay) f.SetFactor(1f);
                Hud.SetObjective($"Верни сосуд в норму:  просвет {Mathf.RoundToInt(f.Factor * 100)} %");
                yield return null;
            }
            f.Deactivate(); f.SetFactor(1f);
            game.sound.Play("SFX_Success_Step", 0.8f);
            yield return Say("Вот так мышцы сосудов распределяют кровь между органами. Запомни это — скоро пригодится.", Mood.Neutral);
        }

        IEnumerator Spasm()
        {
            Beat = 4;
            var o = game.organism; var f = game.field; var v = game.vessel; var env = game.environment;
            float centre = Mathf.Min(game.PlayerS + 62f, v.Length - 60f), half = 34f;
            f.unlocked = true; f.minFactor = 0.8f; f.maxFactor = 1.55f; f.SetFactor(1f);
            float[] offs = { -24, -12, 0, 12, 24 }; float[] rr = { 1.2f, 2.0f, 0.5f, 1.8f, 0.8f }; float[] aa = { 40, 200, 0, 120, 300 };
            for (int i = 0; i < raceGates.Count && i < offs.Length; i++) { var g = raceGates[i]; g.s = centre + offs[i]; Place(g.transform, g.s, rr[i], aa[i]); g.ResetGate(); g.isNext = i == 0; g.gameObject.SetActive(true); }
            // alarm
            game.sound.Play("BVR2_Alarm", 1f); game.sound.Mood("tension");
            o.stressDrive = 18f;
            float t = 0;
            while (t < 2.5f) { t += Time.unscaledDeltaTime; env.alarm = Mathf.Clamp01(t / 1.5f); f.scenarioFactor = Mathf.Lerp(1f, 0.7f, t / 2.5f); f.Activate(centre, half); yield return null; }
            Objective("Спазм впереди! Пробейся сквозь кольца");
            yield return Say("Тревога! Впереди сосуд сжался в спазме — крови почти не проходит. Расширяй его полем и пробивайся сквозь кольца!", Mood.Surprised);
            Prompt("Руки врозь — расширять  ·  A / X — рывок", "Колесо вверх или X — расширить  ·  Shift — рывок");
            int next = 0; float good = 0, total = 0, low = 0, timer = 0; bool failed = false;
            while (next < raceGates.Count)
            {
                float dt = Time.unscaledDeltaTime; timer += dt;
                // the spasm breathes: harder every few heartbeats
                f.scenarioFactor = 0.71f + 0.05f * Mathf.Sin(Time.time * 0.9f);
                var g = raceGates[next]; g.isNext = true; g.Track(game.Head);
                if (autoplay) { f.SetFactor(1.42f); g.ForcePass(); }
                if (g.passed)
                {
                    game.fx.Shock(g.transform.position, g.transform.forward, Bvr2Style.Good, 0.5f, 3f, 0.6f);
                    game.sound.Play("BVR2_Gate", 0.8f, 1.05f + next * 0.07f); Bvr2Haptics.Both(0.45f, 0.08f);
                    next++; continue;
                }
                if (game.PlayerS > g.s + 5f) { g.s = game.PlayerS + 14f; Place(g.transform, g.s, rr[next] * 0.5f, aa[next]); }
                Hud.SetCompass(g.transform.position, "кольцо");
                float q = o.LocalFlow; total += dt; if (q >= 0.7f) good += dt;
                low = q < 0.5f ? low + dt : 0;
                env.alarm = Mathf.Lerp(env.alarm, q < 0.7f ? 1f : 0.35f, dt * 2f);
                Hud.SetObjective($"Кольца {next}/{raceGates.Count}  ·  кровоток {Mathf.RoundToInt(q * 100)} %  (держи выше 70 %)");
                if (low > 6f || timer > 75f) { failed = true; break; }
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            if (failed)
            {
                game.sound.Play("SFX_Error", 1f);
                yield return Say("Ткани остались без кислорода слишком долго. Ничего — попробуем ещё раз. Расширяй сосуд сильнее!", Mood.Sad);
                f.scenarioFactor = 1f; f.Deactivate(); env.alarm = 0; o.stressDrive = 0;
                game.swimmer.Teleport(v.Centre(centre - half - 10f), v.Tangent(centre));
                yield return Spasm();
                yield break;
            }
            SpasmGoodShare = total > 0 ? good / total : 0;
            // resolve
            t = 0;
            while (t < 2f) { t += Time.unscaledDeltaTime; f.scenarioFactor = Mathf.Lerp(0.71f, 1f, t / 2f); env.alarm = Mathf.Lerp(env.alarm, 0, t / 2f); yield return null; }
            f.scenarioFactor = 1f; f.Deactivate(); f.SetFactor(1f); env.alarm = 0; o.stressDrive = 0;
            game.sound.Mood("recovery"); game.sound.Play("SFX_Heal", 0.9f);
            Bvr2Time.SlowMo(0.3f, 1.2f);
            yield return Say("Спазм прошёл, кровоток восстановлен! Ты держал сосуд открытым — ткани получили кислород.", Mood.Smile);
        }

        IEnumerator Debrief()
        {
            Beat = 5;
            Objective("Глава пройдена");
            Stars = 1 + (CorrectPredictions >= 2 ? 1 : 0) + (SpasmGoodShare >= 0.7f ? 1 : 0);
            game.sound.Play("SFX_Stage_Complete", 1f);
            var head = game.rig.head.transform;
            Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized; if (fwd.sqrMagnitude < 0.01f) fwd = head.forward;
            var root = new GameObject("Debrief").transform; root.SetParent(game.rig.transform, false); debriefRoot = root;
            root.position = head.position + fwd * 1.7f + Vector3.up * 0.03f; root.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            var canvas = Bvr2Ui.Canvas("Debrief Canvas", root, new Vector2(1700, 900), 0.0009f, 85);
            Bvr2Ui.Backdrop(canvas);
            Hud.TopHidden = true; Bvr2Environment.Current?.SetFocus(true); game.blood.SetFocus(root.position, 1.1f);
            Prof.Present(root.position, 14f, 1f, 1.05f);
            Bvr2Ui.Panel(canvas, new Color(0.01f, 0.03f, 0.05f, 0.92f), "Back");
            var title = Bvr2Ui.Label(Bvr2Ui.Rect(canvas, "Title", new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -150), new Vector2(-40, -20)), "ГЛАВА 1 · ПОТОК — ПРОЙДЕНА", 64, Bvr2Style.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            string stars = new string('★', Stars) + new string('☆', 3 - Stars);
            Bvr2Ui.Label(Bvr2Ui.Rect(canvas, "Stars", new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -150), new Vector2(-40, -20)), stars, 80, Bvr2Style.Knowledge, TextAnchor.MiddleRight, FontStyle.Bold);
            string[] heads = { "СОСТАВ КРОВИ", "СТЕНКА АРТЕРИИ", "ПРОСВЕТ И КРОВОТОК" };
            string[] bodies = {
                "эритроциты — их больше всего, переносят O₂\nлейкоциты — защитники, их мало\nтромбоциты — останавливают кровь\nплазма — жидкость, ≈ 55 %",
                "внутренняя — гладкий эндотелий\nсредняя — гладкие мышцы\nнаружная — прочная соединительная ткань",
                "сосуд уже на 20 % → крови проходит\nпочти вдвое меньше\nсердцу тяжелее → давление растёт" };
            for (int i = 0; i < 3; i++)
            {
                var card = Bvr2Ui.Rect(canvas, "Card " + i, new Vector2(0, 0), new Vector2(0, 0)); card.pivot = new Vector2(0.5f, 0.5f);
                card.sizeDelta = new Vector2(500, 470); card.anchoredPosition = new Vector2(310 + i * 540, 430);
                Bvr2Ui.Panel(card, new Color(0.03f, 0.1f, 0.13f, 0.95f), "Back");
                var h = Bvr2Ui.Label(card, heads[i], 36, Bvr2Style.Ally, TextAnchor.UpperLeft, FontStyle.Bold); h.rectTransform.offsetMin = new Vector2(26, 0); h.rectTransform.offsetMax = new Vector2(-20, -22);
                var b = Bvr2Ui.Label(card, bodies[i], 32, Bvr2Style.Text, TextAnchor.UpperLeft); b.rectTransform.offsetMin = new Vector2(26, 20); b.rectTransform.offsetMax = new Vector2(-20, -90); b.lineSpacing = 1.2f;
            }
            string why = $"★ глава пройдена   {(CorrectPredictions >= 2 ? "★" : "☆")} оба прогноза верны ({CorrectPredictions}/2)   {(SpasmGoodShare >= 0.7f ? "★" : "☆")} кровоток в спазме ≥ 70 % ({Mathf.RoundToInt(SpasmGoodShare * 100)} % времени)";
            Bvr2Ui.Label(Bvr2Ui.Rect(canvas, "Why", new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 40), new Vector2(-40, 150)), why, 30, Bvr2Style.TextDim, TextAnchor.MiddleCenter);
            root.localScale = Vector3.one * 0.01f;
            for (float k = 0; k < 1f; k += Time.unscaledDeltaTime / 0.35f) { root.localScale = Vector3.one * Mathf.SmoothStep(0.01f, 1f, k); yield return null; }
            root.localScale = Vector3.one;
            yield return Say(Stars == 3 ? "Блестяще! Три звезды. Ты настоящий нанопилот." : "Хорошая работа! Можно пройти ещё раз и собрать все звёзды.", Mood.Smile);
            Prompt("Курок — продолжить", "ЛКМ — продолжить");
            for (float t = 0; t < 30f && !autoplay; t += Time.unscaledDeltaTime) { if (t > 1.2f && game.input.ToolDown) break; yield return null; }
            NoPrompt();
            for (float k = 0; k < 1f; k += Time.unscaledDeltaTime / 0.3f) { root.localScale = Vector3.one * Mathf.Lerp(1f, 0.01f, k); yield return null; }
            root.gameObject.SetActive(false);
            Hud.TopHidden = false; Bvr2Environment.Current?.SetFocus(false); game.blood.ClearFocus();
            // one question to close the loop
            if (autoplay) game.question.ForcedChoice = 0;
            yield return game.question.Ask("Почему узкий сосуд пропускает меньше крови?", new[] { "растёт сопротивление", "кровь густеет", "сердце бьётся реже" }, 0, "ПРОВЕРЬ СЕБЯ");
            yield return Say(game.question.LastChoice == 0 ? "Верно — сопротивление растёт очень быстро." : "Не совсем: главное — сопротивление, оно растёт очень быстро.", Mood.Neutral);
            yield return Say("Дальше по течению кровоток слабеет — там бляшка. Это глава 2.", Mood.Serious);
            Finished = true;
            Objective("Глава 1 пройдена");
            if (autoplay) yield break;
            yield return WhatNext("Плавай сколько хочешь. Сканер и Притяжение работают.");
            if (root) Destroy(root.gameObject); debriefRoot = null;

        }

        void Place(Transform t, float s, float r, float angle)
        {
            var v = game.vessel; v.Frame(s, out _, out var tan, out var n, out _);
            t.position = v.PointAt(s, r, angle); t.rotation = Quaternion.LookRotation(tan, n);
        }
    }
}
