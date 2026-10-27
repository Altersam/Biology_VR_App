using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.V2
{
    /// <summary>
    /// Chapter 4 «Прорыв» (wound and hemostasis): the heart races, pressure falls → follow the pull to a tear
    /// in the wall → anchor, scan (exposed collagen) → predict → spasm with the vascular field (less leak,
    /// less oxygen downstream) → the Factor paints "glue" on collagen, platelets stick — rims before the
    /// centre or the stream rips the plug away; glue on healthy wall grows a clot → the clotting cascade in
    /// order (thromboplastin → thrombin → fibrin) → stitch fibrin across the plug by sweeping the beam;
    /// too much grows a clot into the lumen → cut surplus clots with the Impulse → macrophages clear
    /// debris, the wall zips closed, the volume refills → debrief.
    /// Shock (systolic below 70) rolls the current phase back.
    /// </summary>
    public sealed class Bvr2Chapter4 : Bvr2ChapterBase
    {
        public override int Number => 4;
        public override string Title => "Прорыв";

        public float startS = 1735f;
        public Bvr2Wound wound;
        public Bvr2Scannable woundScan;
        public Transform anchorPad;
        public Mesh nodeMesh, ringMesh; public Material nodeMaterial;

        public int Rollbacks { get; private set; }
        public int Torn { get; private set; }
        public int StraysMade { get; private set; }
        public int CascadeMistakes { get; private set; }
        public float MinVolume { get; private set; } = 5f;
        public override string StatusLine => base.StatusLine + $" · sealed {(wound ? wound.SealedCount : 0)}/9 · leak {(wound ? wound.Leak : 0):F2} · fibrin {(wound ? wound.FibrinProgress : 0):F2} · vol {game.organism.bloodVolume:F2} · sys {game.organism.systolic:F0} · hr {game.organism.heartRate:F0} · torn {Torn} · strays {StraysMade} · rollbacks {Rollbacks}";

        Bvr2Immune Imm => game.immune;
        Transform board, nodesRoot; float shockGrace; bool hooked;
        readonly List<Node> nodes = new List<Node>();
        class Node { public Transform t; public Renderer core, ring; public int order; public bool lit; }

        void Start() { HideProps(); }

        void HideProps()
        {
            if (wound) { wound.Show(false); wound.ResetWound(); wound.gameObject.SetActive(false); }
            if (woundScan) woundScan.gameObject.SetActive(false);
            if (anchorPad) anchorPad.gameObject.SetActive(false);
            if (nodesRoot) Destroy(nodesRoot.gameObject);
            nodes.Clear();
        }

        protected override void ResetProps()
        {
            ResetShared();
            HideProps();
            if (board) Destroy(board.gameObject);
            if (game.abilities) { game.abilities.signalUnlocked = false; game.abilities.SetMode(Bvr2Abilities.Mode.Attract, false); }
            game.bioTool.locked = false;
            Rollbacks = 0; Torn = 0; StraysMade = 0; CascadeMistakes = 0; MinVolume = 5f;
        }

        void Hook()
        {
            if (hooked || !wound) return; hooked = true;
            wound.OnTorn += p => { if (!Running) return; Torn++; Prof.Say(Torn == 1 ? "Сорвало! В центре пробке не за что держаться — сначала закрой края." : "Опять сорвало: сначала края, потом середина.", Mood.Surprised); };
            if (game.factor) game.factor.OnStray += p => { if (!Running) return; StraysMade++; if (StraysMade == 1) Prof.Say("Фактор попал на здоровую стенку — там тоже растёт сгусток. Это уже тромб! Потом придётся убрать.", Mood.Serious); };
        }

        void Update()
        {
            if (!Running) return;
            var o = game.organism;
            MinVolume = Mathf.Min(MinVolume, o.bloodVolume);
            game.environment.alarm = Mathf.Clamp01((4.7f - o.bloodVolume) / 1.3f) * 0.6f;
            if (Time.frameCount % 15 == 0 && wound)
                Hud.SetCounters($"ЗНАНИЯ {game.codex.FoundIn(4)}/{game.codex.CountIn(4)}", $"КРОВЬ {o.bloodVolume:0.0} л · УТЕЧКА {Mathf.RoundToInt(wound.Leak * 100)} %");
        }

        // ---------------- the chapter -------------------------------------------------------
        protected override IEnumerator Run()
        {
            ResetProps(); Hook();
            var o = game.organism; o.ResetState();
            game.scanner.locked = false; game.scanner.allowPlasma = false;
            game.abilities.impulseUnlocked = true;
            game.field.unlocked = true;
            Imm.replicate = false;
            wound.ResetWound(); wound.Show(true); wound.leakScale = 0.35f;
            game.sound.Mood("tension");
            if (startBeat <= 0) yield return Intro();
            else
            {
                Arrive(startBeat <= 1 ? startS : wound.s - 12f); Prof.Appear(true);
                game.codex.Discover("bleeding");
                if (startBeat >= 2) { game.codex.Discover("wound"); PlaceAnchor(); game.swimmer.AttachTo(anchorPad); anchorPad.gameObject.SetActive(false); }
                if (startBeat >= 3) { game.codex.Discover("spasm"); game.field.scenarioFactor = 0.85f; }
                if (startBeat >= 4) { game.codex.Discover("plug"); DebugSealAll(); }
                if (startBeat >= 5) game.codex.Discover("coagulation");
                if (startBeat >= 6) { while (wound.FibrinProgress < 1f) game.factor.Lay(Random.Range(-0.8f, 0.8f), Random.Range(-0.8f, 0.8f)); }
                if (startBeat == 6) wound.SpawnStray(wound.PointOn(0, 0, 0.6f), 0.9f);
                o.stressDrive = 8f; o.Settle(4.4f);
            }
            if (startBeat <= 1) yield return Find();
            if (startBeat <= 2) yield return Spasm();
            if (startBeat <= 3) yield return Plug();
            if (startBeat <= 4) yield return Cascade();
            if (startBeat <= 5) yield return Stitch();
            if (startBeat <= 6) yield return Cleanup();
            if (startBeat <= 7) yield return Recovery();
            yield return Debrief();
        }

        IEnumerator Intro()
        {
            Beat = 0;
            Arrive(startS, 1.2f);
            game.sound.Play("SFX_Transition", 0.9f);
            Hud.Title("ГЛАВА 4 · ПРОРЫВ", "давление падает — найди утечку", 4f);
            Prof.Appear(true);
            game.organism.stressDrive = 8f;
            yield return Wait(2.5f);
            Objective("Посмотри на пульс и давление");
            yield return Say(C("Тревога! Подними левую руку: пульс растёт, а давление ползёт вниз. Где-то уходит кровь.", "Тревога! Посмотри на панель слева внизу: пульс растёт, а давление ползёт вниз. Где-то уходит кровь."), Mood.Serious);
            game.Reveal("bleeding", game.Head + game.rig.head.transform.forward * 2f);
            yield return WaitKnowledge();
        }

        void PlaceAnchor()
        {
            float R = game.vessel.Radius(wound.s);
            anchorPad.position = game.vessel.PointAt(wound.s - 3.4f, R * 0.12f, wound.angleDeg);
            anchorPad.rotation = Quaternion.LookRotation(wound.Centre - anchorPad.position);
            anchorPad.gameObject.SetActive(true);
        }

        IEnumerator Find()
        {
            Beat = 1;
            wound.leakScale = 0.5f; wound.pullPlayer = true;
            Objective("Найди место утечки");
            yield return Say("Плыви по течению. Почувствуешь, что тянет к стенке, — мы рядом.", Mood.Neutral);
            float t = 0;
            if (autoplay) Arrive(wound.s - 9f, 0.4f);
            while (Vector3.Distance(game.Head, wound.Centre) > 15f)
            {
                t += Time.unscaledDeltaTime;
                if (t > 8f) Hud.SetCompass(wound.Centre, "утечка");
                yield return null;
            }
            // anchor in front of the tear: the stream drags everything into it
            PlaceAnchor();
            Objective("Прицепись к маяку у разрыва");
            Prompt("Хват (grip) — якорь", "E — якорь");
            yield return Say("Нас затягивает в разрыв! Прицепись к маяку.", Mood.Surprised);
            while (!game.swimmer.Anchored)
            {
                float d = Vector3.Distance(game.Head, anchorPad.position);
                Hud.SetCompass(anchorPad.position, "маяк");
                if ((d < 4.5f && game.input.AnchorDown) || autoplay) { game.swimmer.AttachTo(anchorPad); Bvr2Haptics.Both(0.5f, 0.1f); }
                else if (game.input.AnchorDown) Hud.Toast("Подплыви ближе к маяку", Bvr2Style.TextDim, 1.2f);
                yield return null;
            }
            wound.pullPlayer = false;
            Hud.SetCompass(null, null); NoPrompt();
            for (float w = 0; w < 0.8f && !autoplay; w += Time.unscaledDeltaTime) yield return null;
            anchorPad.gameObject.SetActive(false); // the player keeps hanging there; the beacon would only block the view
            if (autoplay) game.rig.LookAt(wound.Centre);
            // scan the tear
            woundScan.transform.position = wound.Centre; woundScan.gameObject.SetActive(true);
            Objective("Отсканируй разрыв");
            Prompt("Левый курок — сканировать разрыв", "ПКМ / F — сканировать разрыв");
            wound.leakScale = 0.35f;
            while (!game.codex.Has("wound"))
            {
                if (autoplay) game.scanner.ForceScan("wound", wound.Centre);
                Hud.SetCompass(wound.Centre, "разрыв");
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            yield return WaitKnowledge();
            woundScan.gameObject.SetActive(false);
            if (autoplay) game.question.ForcedChoice = 0;
            yield return game.question.Ask("Что организм сделает первым делом?", new[] { "сожмёт повреждённый сосуд", "сразу зарастит стенку", "поднимет температуру" }, 0);
            bool right = game.question.LastChoice == 0; if (right) CorrectPredictions++;
            yield return Say(right ? "Верно — спазм. Сделаем так же." : "Первым делом — спазм: мышцы сжимают сосуд. Сделаем так же.", right ? Mood.Smile : Mood.Neutral);
        }

        IEnumerator Spasm()
        {
            Beat = 2;
            var f = game.field;
            f.unlocked = true; f.Activate(wound.s, 12f); f.scenarioFactor = 1f;
            wound.leakScale = 0.6f;
            Objective("Сожми сосуд полем");
            Prompt("Зажми оба хвата и сведи руки — сжать", "Колесо вниз / Z — сжать сосуд");
            yield return Say("Сожми сосуд полем — так делают его мышцы.", Mood.Neutral);
            float held = 0;
            while (held < (autoplay ? 0.3f : 1.5f))
            {
                if (autoplay) f.SetFactor(0.72f);
                if (f.Effective <= 0.8f) held += Time.unscaledDeltaTime;
                Hud.SetObjective($"Сожми сосуд полем  ·  просвет {Mathf.RoundToInt(f.Effective * 100)} %  ·  утечка {Mathf.RoundToInt(wound.Leak * Mathf.Clamp(game.organism.fieldConductance, 0.15f, 1.5f) * 100)} %");
                yield return null;
            }
            NoPrompt();
            game.sound.Play("SFX_Success_Step", 0.8f);
            game.Reveal("spasm", game.Head + game.rig.head.transform.forward * 2f);
            yield return WaitKnowledge();
            yield return Say("Кровь уходит медленнее. Но и тканям дальше по течению достаётся меньше — долго так нельзя. Спазм дал нам время.", Mood.Serious);
            // the vessel keeps a lighter spasm by itself while we work
            f.Deactivate(); f.SetFactor(1f); f.scenarioFactor = 0.85f;
        }

        IEnumerator Plug()
        {
            Beat = 3;
            var fac = game.factor;
            game.abilities.factorUnlocked = true; game.abilities.SetMode(Bvr2Abilities.Mode.Factor, false);
            fac.stitchMode = false;
            Hud.Toast("+ СПОСОБНОСТЬ · ФАКТОР", Bvr2FactorTool.PaintColor, 2.8f);
            game.sound.Play("SFX_Reveal", 0.9f);
            wound.leakScale = 0.4f;
            yield return Say("Новый режим BioTool — Фактор: белок-«клей» для тромбоцитов. Наноси его на коллаген в разрыве.", Mood.Smile);
            yield return Say("Начинай с краёв! Середине пробки нужна опора — иначе поток её сорвёт.", Mood.Serious);
            Prompt("Курок — нанести фактор · B — режим", "ЛКМ — нанести фактор · Q — режим");
            Objective("Закрой разрыв пробкой");
            float startVolume = Mathf.Max(game.organism.bloodVolume, 4.2f);
            wound.leakScale = 1f;
            int[] autoOrder = { 4, 0, 2, 3, 5, 6, 8, 1, 4, 7 };
            int autoStep = 0; bool autoStray = false; float autoWait = 0;
            while (wound.SealedCount < wound.patches.Count)
            {
                int sealedNow = wound.SealedCount;
                Hud.SetObjective($"Закрой разрыв пробкой: {sealedNow}/{wound.patches.Count}  ·  утечка {Mathf.RoundToInt(wound.Leak * 100)} %");
                if (sealedNow >= 1 && !game.codex.Has("plug")) game.Reveal("plug", wound.Centre);
                if (game.abilities.Current != Bvr2Abilities.Mode.Factor && Time.frameCount % 120 == 0) Hud.Toast(C("B — включить Фактор", "Q — включить Фактор"), Bvr2Style.TextDim, 1.2f);
                Bvr2Wound.Patch next = null;
                foreach (var p in wound.patches) if (!p.Sealed && !p.IsCentre) { next = p; break; }
                if (next == null) foreach (var p in wound.patches) if (!p.Sealed) { next = p; break; }
                if (next != null && !autoplay) Hud.SetCompass(Vector3.Angle(game.rig.head.transform.forward, next.t.position - game.Head) > 35f ? next.t.position : (Vector3?)null, "коллаген");
                if (autoplay) AutoPaint(autoOrder, ref autoStep, ref autoStray, ref autoWait);
                if (ShockNow) { yield return Rollback(startVolume); wound.ResetPatches(); autoStep = 1; }
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            if (!game.codex.Has("plug")) { game.Reveal("plug", wound.Centre); }
            yield return WaitKnowledge();
            game.sound.Play("SFX_Success_Step", 0.9f);
        }

        void AutoPaint(int[] order, ref int step, ref bool strayDone, ref float wait)
        {
            if (!strayDone && step >= 3)
            {
                strayDone = true;
                game.factor.ForceStray(game.vessel.SurfacePoint(wound.s - 7f, wound.angleDeg + 70f, 0f));
            }
            if (step >= order.Length) { foreach (var p in wound.patches) if (!p.Sealed) wound.Paint(p, Time.deltaTime * 3f); return; }
            var target = wound.patches[order[step]];
            if (step == 0)
            {
                // deliberately paint a centre patch first: the stream must rip it away
                wound.Paint(target, Time.deltaTime * 3f);
                if (Torn > 0) step++;
                return;
            }
            if (target.Sealed || target.paint >= 1f)
            {
                // a centre patch waits for its rims to hold
                if (target.IsCentre && !wound.RimsHold(target)) return;
                step++; return;
            }
            if (target.IsCentre && !wound.RimsHold(target)) return;
            wound.Paint(target, Time.deltaTime * 3f);
        }

        IEnumerator Cascade()
        {
            Beat = 4;
            game.abilities.SetMode(Bvr2Abilities.Mode.Attract, false); game.bioTool.locked = true;
            wound.leakScale = 0.6f;
            yield return Say("Пробка держит, но она рыхлая — кровь сочится. Нужен сгусток: запусти свёртывание.", Mood.Neutral);
            yield return Say("Тромбоциты и раненая ткань выделяют тромбопластин. Он превращает протромбин в тромбин, а тромбин — фибриноген в нити фибрина.", Mood.Neutral);
            BuildNodes();
            Objective("Запусти свёртывание: звенья по порядку");
            Prompt("Курок на звено — активировать", "ЛКМ на звено — активировать");
            yield return Say("Активируй звенья по порядку.", Mood.Smile);
            wound.leakScale = 0.8f;
            int next = 0; float autoT = 0; bool autoMistake = false;
            float startVolume = Mathf.Max(game.organism.bloodVolume, 4.2f);
            while (next < nodes.Count)
            {
                Node pick = null;
                if (game.input.ToolDown) pick = Aimed();
                if (autoplay)
                {
                    autoT += Time.unscaledDeltaTime;
                    if (autoT > 0.35f) { autoT = 0; pick = !autoMistake ? NodeOf(1) : NodeOf(next); autoMistake = true; }
                }
                if (pick != null && !pick.lit)
                {
                    if (pick.order == next)
                    {
                        pick.lit = true; next++;
                        game.sound.Play("BVR2_Node", 0.9f, 0.9f + next * 0.12f);
                        game.fx.Shock(pick.t.position, game.Head - pick.t.position, new Color(1f, 0.85f, 0.4f), 0.05f, 0.6f, 0.5f);
                        Bvr2Haptics.Pulse(false, 0.5f, 0.08f);
                    }
                    else
                    {
                        CascadeMistakes++; next = 0; foreach (var n in nodes) n.lit = false;
                        game.sound.Play("SFX_Error", 0.8f);
                        game.fx.Burst(pick.t.position, Bvr2Style.Danger, 16, 1f, 0.03f, 0.5f);
                        Prof.Say("Не тот порядок — каскад рассыпался. Всё начинается с тромбопластина.", Mood.Serious);
                    }
                }
                UpdateNodes();
                if (ShockNow) { yield return Rollback(startVolume); next = 0; foreach (var n in nodes) n.lit = false; }
                yield return null;
            }
            UpdateNodes();
            NoPrompt();
            game.sound.Play("SFX_Success_Step", 0.9f);
            // fibrinogen turns into fibrin threads around the plug
            game.fx.Burst(wound.Centre, new Color(1f, 0.92f, 0.6f), 40, 2f, 0.04f, 0.9f);
            for (float t = 0; t < (autoplay ? 0.1f : 0.8f); t += Time.unscaledDeltaTime) { UpdateNodes(); yield return null; }
            if (nodesRoot) Destroy(nodesRoot.gameObject);
            nodes.Clear();
            Bvr2Environment.Current?.SetFocus(false); game.blood.ClearFocus();
            game.Reveal("coagulation", wound.Centre);
            yield return WaitKnowledge();
            game.bioTool.locked = false;
        }

        void BuildNodes()
        {
            if (nodesRoot) Destroy(nodesRoot.gameObject);
            nodes.Clear();
            nodesRoot = new GameObject("Cascade Nodes").transform;
            var head = game.rig.head.transform;
            Vector3 toWound = (wound.Centre - head.position); Vector3 fwd = toWound.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized; if (right.sqrMagnitude < 0.01f) right = head.right;
            Vector3 up = Vector3.Cross(fwd, right).normalized;
            Vector3 centre = head.position + fwd * Mathf.Min(1.7f, toWound.magnitude * 0.4f) + up * 0.3f;
            string[] titles = { "ТРОМБОПЛАСТИН", "ТРОМБИН", "ФИБРИН" };
            string[] subs = { "из тромбоцитов и раны, + Ca²⁺", "из протромбина плазмы", "из фибриногена — нити" };
            int[] slot = { 1, 2, 0 }; // shuffled positions: the order has to be known, not read left to right
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("Link · " + titles[i]); go.transform.SetParent(nodesRoot, false);
                go.transform.position = centre + right * ((slot[i] - 1) * 0.52f) + up * (slot[i] == 1 ? 0.1f : -0.04f);
                var core = new GameObject("Core"); core.transform.SetParent(go.transform, false); core.transform.localScale = Vector3.one * 0.16f;
                core.AddComponent<MeshFilter>().sharedMesh = nodeMesh; var cr = core.AddComponent<MeshRenderer>(); cr.sharedMaterial = nodeMaterial; cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var ring = new GameObject("Ring"); ring.transform.SetParent(go.transform, false); ring.transform.localScale = Vector3.one * 0.2f;
                ring.AddComponent<MeshFilter>().sharedMesh = ringMesh; var rr = ring.AddComponent<MeshRenderer>(); rr.sharedMaterial = nodeMaterial; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var canvas = Bvr2Ui.Canvas("Label", go.transform, new Vector2(520, 170), 0.001f, 70);
                canvas.localPosition = new Vector3(0, 0.31f, 0);
                var t1 = Bvr2Ui.Label(Bvr2Ui.Rect(canvas, "Title", new Vector2(0, 0.45f), new Vector2(1, 1)), titles[i], 54, Bvr2Style.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                Bvr2Ui.Fit(t1, 30, 54); t1.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.85f);
                var t2 = Bvr2Ui.Label(Bvr2Ui.Rect(canvas, "Sub", new Vector2(0, 0), new Vector2(1, 0.45f)), subs[i], 38, new Color(0.85f, 0.92f, 1f), TextAnchor.MiddleCenter);
                Bvr2Ui.Fit(t2, 26, 38); t2.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.85f);
                go.transform.rotation = Quaternion.LookRotation(go.transform.position - head.position, up);
                nodes.Add(new Node { t = go.transform, core = cr, ring = rr, order = i });
            }
            game.fx.Shock(centre, -fwd, new Color(1f, 0.85f, 0.4f), 0.1f, 1.2f, 0.6f);
            game.sound.Play("SFX_Reveal", 0.7f);
            // a quiet moment around the links: the world dims, cells drift aside
            Bvr2Environment.Current?.SetFocus(true); game.blood.SetFocus(centre, 1.3f);
        }

        Node NodeOf(int order) { foreach (var n in nodes) if (n.order == order) return n; return null; }

        Node Aimed()
        {
            var hand = game.rig.rightHand; Node best = null; float bestA = 10f;
            foreach (var n in nodes)
            {
                float a = Vector3.Angle(hand.forward, n.t.position - hand.position);
                if (a < bestA) { bestA = a; best = n; }
            }
            return best;
        }

        void UpdateNodes()
        {
            var aimed = Aimed(); float time = Time.unscaledTime;
            var mpb = new MaterialPropertyBlock();
            foreach (var n in nodes)
            {
                Color c = n.lit ? new Color(1f, 0.82f, 0.35f) : n == aimed ? new Color(0.6f, 0.95f, 1f) : new Color(0.45f, 0.7f, 0.85f, 0.7f);
                mpb.SetColor("_Color", c); n.core.SetPropertyBlock(mpb); n.ring.SetPropertyBlock(mpb);
                n.core.transform.localScale = Vector3.one * 0.16f * (n.lit ? 1.15f + 0.08f * Mathf.Sin(time * 6f) : n == aimed ? 1.1f : 1f);
                n.ring.transform.localRotation = Quaternion.Euler(time * 40f, time * 60f + n.order * 50f, 0);
            }
        }

        IEnumerator Stitch()
        {
            Beat = 5;
            var fac = game.factor;
            game.abilities.factorUnlocked = true; game.abilities.SetMode(Bvr2Abilities.Mode.Factor, false);
            fac.stitchMode = true;
            wound.leakScale = 0.5f;
            yield return Say("Фибриноген стал фибрином! Прошей пробку: держи курок и веди лучом поперёк разрыва — от края к краю.", Mood.Smile);
            Prompt("Держи курок и веди поперёк разрыва", "Держи ЛКМ и веди поперёк разрыва");
            Objective("Прошей пробку фибрином");
            wound.leakScale = 1f;
            float startVolume = Mathf.Max(game.organism.bloodVolume, 4.2f);
            float autoT = 0; int autoN = 0;
            System.Action<float, float> onStitch = (a, b) => { if (wound.FibrinProgress > 1.2f) { StraysMade++; wound.SpawnStray(wound.PointOn((a + b) * 0.5f, 0, 0.7f), 0.8f); Prof.Say("Перебор! Лишний фибрин растёт в просвет — это тромб.", Mood.Surprised); } };
            fac.OnStitch += onStitch;
            while (wound.FibrinProgress < 1f)
            {
                Hud.SetObjective($"Прошей пробку фибрином: {Mathf.RoundToInt(Mathf.Clamp01(wound.FibrinProgress) * 100)} %  ·  нитей {wound.FibrinThreads}");
                if (game.abilities.Current != Bvr2Abilities.Mode.Factor && Time.frameCount % 120 == 0) Hud.Toast(C("B — включить Фактор", "Q — включить Фактор"), Bvr2Style.TextDim, 1.2f);
                if (autoplay) { autoT += Time.unscaledDeltaTime; if (autoT > 0.15f) { autoT = 0; autoN++; fac.Lay(Mathf.Sin(autoN * 1.7f) * 0.7f, Mathf.Sin(autoN * 2.9f + 1f) * 0.7f); } }
                if (ShockNow) { yield return Rollback(startVolume); wound.ClearFibrin(); }
                yield return null;
            }
            // enough: a short moment in which extra threads overshoot
            game.sound.Play("SFX_Success_Step", 0.9f);
            Prompt("Отпусти курок — достаточно", "Отпусти ЛКМ — достаточно");
            Hud.SetObjective("Пробка прошита — достаточно!");
            if (autoplay) fac.Lay(0.1f, -0.2f);
            for (float t = 0; t < 1.6f; t += Time.unscaledDeltaTime) { if (t > 0.4f && !game.input.Tool) break; yield return null; }
            fac.OnStitch -= onStitch;
            fac.stitchMode = false; game.abilities.SetMode(Bvr2Abilities.Mode.Attract, false);
            NoPrompt();
            yield return Say("Сгусток держит — утечки нет!", Mood.Smile);
        }

        IEnumerator Cleanup()
        {
            Beat = 6;
            if (wound.AliveStrays == 0) yield break;
            game.abilities.impulseUnlocked = true; game.abilities.SetMode(Bvr2Abilities.Mode.Impulse);
            yield return Say("Лишние сгустки сужают артерию — это тромбы. Срежь их Импульсом.", Mood.Serious);
            Prompt("Курок — Импульс по сгустку", "ЛКМ — Импульс по сгустку");
            Objective("Срежь лишние сгустки");
            float autoT = 0;
            while (wound.AliveStrays > 0)
            {
                Bvr2StrayClot near = null; float bd = 1e9f;
                foreach (var c in wound.strays) if (c && c.Alive) { float d = Vector3.Distance(c.transform.position, game.Head); if (d < bd) { bd = d; near = c; } }
                Hud.SetObjective($"Срежь лишние сгустки: осталось {wound.AliveStrays}  ·  просвет {Mathf.RoundToInt(wound.StrayConductance * 100)} %");
                Hud.SetCompass(near ? near.transform.position : (Vector3?)null, "тромб");
                if (game.abilities.Current != Bvr2Abilities.Mode.Impulse && Time.frameCount % 120 == 0) Hud.Toast(C("B — включить Импульс", "Q — включить Импульс"), Bvr2Style.TextDim, 1.2f);
                if (autoplay && near)
                {
                    autoT += Time.unscaledDeltaTime;
                    if (autoT > 0.45f) { autoT = 0; game.rig.LookAt(near.transform.position); var tip = game.impulse.tip ? game.impulse.tip.position : game.rig.rightHand.position; game.impulse.Fire((near.transform.position - tip).normalized); }
                }
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            game.sound.Play("SFX_Success_Step", 0.9f);
            game.abilities.SetMode(Bvr2Abilities.Mode.Attract, false);
        }

        IEnumerator Recovery()
        {
            Beat = 7;
            var head = game.rig.head.transform;
            Imm.Deploy(2, head);
            // debris of dead cells around the injury, up- and downstream of the tear
            float[] dsList = { -2.6f, -1.6f, 1.3f, 2.2f, 3f }; float[] daList = { -40f, 35f, -30f, 45f, -10f };
            for (int i = 0; i < dsList.Length; i++)
            {
                float ss = wound.s + dsList[i] * wound.halfLength * 0.9f;
                Imm.SpawnDebris(game.vessel.PointAt(ss, game.vessel.Radius(ss) - 1.3f - (i % 2) * 0.6f, wound.angleDeg + daList[i]));
            }
            game.abilities.signalUnlocked = true; game.abilities.SetMode(Bvr2Abilities.Mode.Signal);
            yield return Say("Вокруг раны — обломки погибших клеток. Позови макрофагов Сигналом.", Mood.Neutral);
            Prompt("Курок на обломки — Сигнал", "ЛКМ на обломки — Сигнал");
            Objective("Позови макрофагов к обломкам");
            float t = 0, autoT = 0;
            while (Imm.AliveDebris > 0)
            {
                t += Time.unscaledDeltaTime;
                Hud.SetObjective($"Наведи макрофагов на обломки: осталось {Imm.AliveDebris}");
                var near = Imm.NearestVirus(game.Head, 40f, false);
                Hud.SetCompass(near && Vector3.Angle(game.rig.head.transform.forward, near.transform.position - game.Head) > 35f ? near.transform.position : (Vector3?)null, "обломки");
                if ((autoplay || t > 40f) && near) { autoT += Time.unscaledDeltaTime; if (autoT > 0.8f) { autoT = 0; Imm.Signal(near.transform.position, Imm.OnTheBeat); } }
                if (t > 70f) foreach (var v in Imm.viruses) if (v.debris && v.gameObject.activeSelf && v.Alive) v.Kill(false);
                yield return null;
            }
            NoPrompt(); Hud.SetCompass(null, null);
            game.abilities.SetMode(Bvr2Abilities.Mode.Attract, false);
            // healing: the wall zips closed, plasmin dissolves the fibrin, the volume refills
            game.Reveal("healing", wound.Centre);
            yield return WaitKnowledge();
            Objective("Рана заживает");
            game.sound.Mood("recovery");
            game.sound.PlayAt("BVR2_Zip", wound.Centre, 1f);
            game.organism.refillRate = 1.5f; game.organism.stressDrive = 0;
            Prof.Say("Стенка зарастает: клетки делятся и закрывают разрыв, а ненужный фибрин растворяется.", Mood.Smile);
            float dur = autoplay ? 0.6f : 6f;
            for (float k = 0; k < 1f; k += Time.unscaledDeltaTime / dur)
            {
                wound.Heal(Mathf.SmoothStep(0, 1, k));
                game.field.scenarioFactor = Mathf.Lerp(0.85f, 1f, k);
                if (Time.frameCount % 4 == 0) game.fx.Burst(wound.PointOn(Mathf.Lerp(-1f, 1f, k), 0, 0.2f), Bvr2Style.Good, 3, 0.8f, 0.03f, 0.5f);
                yield return null;
            }
            wound.Heal(1f); game.field.scenarioFactor = 1f;
            Imm.DismissPhagocytes();
            while (Prof.Busy && !autoplay) yield return null;
            yield return Wait(1.5f);
        }

        IEnumerator Debrief()
        {
            Beat = 8;
            Objective("Глава 4 пройдена");
            game.field.scenarioFactor = 1f;
            var o = game.organism;
            float lost = 5f - MinVolume;
            bool safeStar = Rollbacks == 0 && lost < 1f;
            bool cleanStar = Torn == 0 && StraysMade == 0 && CascadeMistakes == 0;
            Stars = 1 + (safeStar ? 1 : 0) + (cleanStar ? 1 : 0);
            string[] heads = { "ГЕМОСТАЗ ПО ШАГАМ", "СВЁРТЫВАНИЕ КРОВИ", "КРОВОПОТЕРЯ" };
            string[] bodies = {
                "1. спазм сосуда\n2. тромбоцитарная пробка:\nприлипли → зовут соседей → склеились\n3. сгусток из нитей фибрина\n4. стенка зарастает",
                "тромбопластин (+ Ca²⁺, витамин K)\nпротромбин → тромбин\nфибриноген → нити фибрина\nсгусток там, где не нужно, — тромб",
                "давление падает, пульс растёт\nпотеря трети крови опасна\nартериальное кровотечение —\nжгут выше раны" };
            string why = $"★ глава пройдена   {(safeStar ? "★" : "☆")} без шока, потеряно меньше 1 л (было {lost:0.0} л)   {(cleanStar ? "★" : "☆")} чисто: без сорванных пробок, лишних тромбов и ошибок в каскаде";
            board = ShowBoard("ГЛАВА 4 · ПРОРЫВ — ПРОЙДЕНА", Stars, heads, bodies, why);
            yield return BoardIn(board);
            yield return Say(Stars == 3 ? "Безупречно! Так работает настоящий гемостаз." : "Рана закрыта! Пройди ещё раз, чтобы собрать все звёзды.", Mood.Smile);
            yield return WaitContinue();
            yield return BoardOut(board);
            if (autoplay) game.question.ForcedChoice = 0;
            yield return game.question.Ask("Какой белок образует нити сгустка?", new[] { "фибрин (из фибриногена)", "гемоглобин", "коллаген" }, 0, "ПРОВЕРЬ СЕБЯ");
            yield return Say(game.question.LastChoice == 0 ? "Точно. Тромбин превращает фибриноген в фибрин." : "Нет: нити сгустка — это фибрин. Его делает тромбин из фибриногена.", Mood.Neutral);
            Finished = true;
            Objective("Глава 4 пройдена");
            o.refillRate = 0.6f;
            if (autoplay) yield break;
            if (board) { Destroy(board.gameObject); board = null; }
            yield return WhatNext("Рана закрыта. Плавай сколько хочешь.");
        }

        // ---------------- shock ------------------------------------------------------------
        bool ShockNow => game.organism.systolic < 70f && Time.time > shockGrace;

        IEnumerator Rollback(float volume)
        {
            Rollbacks++;
            game.environment.alarm = 1f;
            game.rig.Fade(new Color(0.25f, 0f, 0f), 0.85f, 3f);
            Hud.Title("ШОК", "давление ниже 70 — этап сначала", 3f);
            game.sound.Play("BVR2_Alarm", 0.9f);
            Bvr2Haptics.Both(0.8f, 0.3f);
            yield return Say("Шок! Потеряно слишком много крови. Я влил раствор — повторим этот этап быстрее.", Mood.Surprised);
            game.organism.Settle(volume);
            shockGrace = Time.time + 6f;
            game.rig.FadeTo(new Color(0.25f, 0f, 0f), 0f, 1.5f);
        }

        // ---------------- debug hooks -------------------------------------------------------
        public void DebugSealAll() { foreach (var p in wound.patches) { p.paint = 1f; p.platelets = 1f; p.sealedFlag = true; } }
        public void DebugLookAtWound() { game.rig.LookAt(wound.Centre); }
        public void DebugPaint(int index) { if (index >= 0 && index < wound.patches.Count) wound.Paint(wound.patches[index], 1f); }
        public void DebugStitch(int n) { for (int i = 0; i < n; i++) game.factor.Lay(Random.Range(-0.8f, 0.8f), Random.Range(-0.8f, 0.8f)); }
        public void DebugStray() { game.factor.ForceStray(game.vessel.SurfacePoint(wound.s - 6f, wound.angleDeg + 60f, 0f)); }
        public void DebugVolume(float v) { game.organism.Settle(v); }
        public void DebugReveal(string id) { game.codex.Forget(id); game.Reveal(id, wound.Centre); }
        public void DebugLookAtStray() { foreach (var c in wound.strays) if (c && c.Alive) { game.rig.LookAt(c.transform.position); return; } }
        public void DebugLookAtNodes() { if (nodes.Count > 0) game.rig.LookAt(nodes[0].t.position); }
    }
}
