using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// Chapter 2 «Бляшка» (scenes 06–07 of the script): the flow is weak → find the cause (a plaque) →
    /// learn what it is made of → predict what a cap rupture does → "Импульс": melt the lipid core through
    /// windows that open between pulse waves, without hitting the thin fibrous cap → a cap ruptures
    /// (yours if you were careless, otherwise an unstable neighbour) → a thrombus grows → a piece breaks off
    /// → climax: catch the embolus with the beam and hold it while the thrombolytic dissolves it, before
    /// it plugs the narrow branch.
    /// </summary>
    public sealed class Bvr2Chapter2 : Bvr2ChapterBase
    {
        public override int Number => 2;
        public override string Title => "Бляшка";

        public float startS = 950f;
        [Tooltip("Arc length where the branch becomes narrow; the embolus lodges here.")]
        public float narrowS = 1222f;
        public List<Bvr2LipidTarget> lipids = new List<Bvr2LipidTarget>();
        public Bvr2Scannable plaqueScan, foamCell, thrombusScan;
        public Bvr2Clot thrombus;
        public Bvr2Embolus embolus;
        public Transform anchorPad;

        public float CapIntegrity { get; private set; } = 1f;
        public int Melted { get; private set; }
        public int CapHits { get; private set; }
        public bool PlayerRuptured { get; private set; }
        public float CatchDistanceLeft { get; private set; }
        public int Lodgings { get; private set; }
        public override string StatusLine => base.StatusLine + $" · lipids {Melted}/{lipids.Count} · cap {CapIntegrity:P0} · embolus {(embolus ? embolus.Current.ToString() : "-")}";

        Bvr2Vessel.Plaque baseA, baseB; bool saved;
        int ruptured = -1; // which plaque ruptured
        Transform board;
        Bvr2Vessel V => game.vessel;

        void Awake() { SavePlaques(); }

        void SavePlaques()
        {
            if (saved || !game || !game.vessel || game.vessel.plaques.Length < 2) return;
            baseA = game.vessel.plaques[0]; baseB = game.vessel.plaques[1]; saved = true;
        }

        void Start()
        {
            SavePlaques();
            if (game.impulse)
            {
                game.impulse.OnHitTarget += (t, open) => { if (Running && t is Bvr2LipidTarget l) OnTarget(l, open); };
                game.impulse.OnHitWall += (p, inset) => { if (Running) OnWall(p, inset); };
            }
            HideProps();
        }

        void HideProps()
        {
            if (thrombus) thrombus.gameObject.SetActive(false);
            if (embolus) embolus.Hide();
            if (anchorPad) anchorPad.gameObject.SetActive(false);
            foreach (var l in lipids) if (l) l.gameObject.SetActive(false);
        }

        protected override void ResetProps()
        {
            ResetShared();
            HideProps();
            if (board) Destroy(board.gameObject);
            if (saved) { V.plaques[0] = baseA; V.plaques[1] = baseB; }
            game.organism.lesionConductance = 1f;
            game.bioTool.locked = false;
            if (game.abilities) game.abilities.SetMode(Bvr2Abilities.Mode.Attract, false);
            CapIntegrity = 1f; Melted = 0; CapHits = 0; PlayerRuptured = false; ruptured = -1; CatchDistanceLeft = 0; Lodgings = 0;
        }

        void Update()
        {
            if (!Running) return;
            UpdateLesion();
            if (Time.frameCount % 15 == 0) Hud.SetCounters($"ЗНАНИЯ {game.codex.FoundIn(2)}/{game.codex.CountIn(2)}", $"ЖИР {Melted}/{lipids.Count}");
        }

        /// <summary>The artery's conductance follows what is narrowing it right now.</summary>
        void UpdateLesion()
        {
            float k = 1f;
            var a = V.plaques[0]; var b = V.plaques[1];
            if (a.active && baseA.height > 0) k *= 1f - 0.38f * (a.height / baseA.height);
            if (b.active && baseB.height > 0) k *= 1f - 0.1f * (b.height / baseB.height);
            if (thrombus && thrombus.gameObject.activeSelf) k *= 1f - 0.35f * thrombus.growth * (1f - thrombus.dissolve);
            if (embolus && embolus.Current == Bvr2Embolus.State.Lodged) k *= 0.1f;
            game.organism.lesionConductance = Mathf.Lerp(game.organism.lesionConductance, Mathf.Clamp(k, 0.05f, 1f), Time.deltaTime * 2f);
        }

        // ---------------- the chapter -------------------------------------------------------
        protected override IEnumerator Run()
        {
            ResetProps();
            var o = game.organism; o.ResetState();
            game.scanner.locked = false; game.scanner.allowPlasma = false; game.bioTool.locked = false;
            game.field.unlocked = true;
            if (game.abilities) game.abilities.impulseUnlocked = startBeat >= 2;
            game.sound.Mood("calm");
            if (startBeat <= 0) yield return Intro();
            else
            {
                float[] at = { startS, startS + 30f, baseA.s - 14f, baseA.s - 4f, baseB.s - 8f, baseA.s };
                Arrive(at[Mathf.Clamp(startBeat, 0, at.Length - 1)]); Prof.Appear(true);
                if (startBeat >= 2) PreDiscover("plaque", "foam");
            }
            if (startBeat <= 1) yield return Diagnose();
            if (startBeat <= 2) yield return MeltCore();
            if (startBeat <= 3) yield return Rupture();
            if (startBeat <= 4) yield return Chase();
            yield return Debrief();
        }

        void PreDiscover(params string[] ids) { foreach (var id in ids) game.codex.Discover(id); }

        Vector3 Crest(int i) { var p = V.plaques[i]; return V.SurfacePoint(p.s, p.angleDeg, 0f); }

        IEnumerator Intro()
        {
            Beat = 0;
            Arrive(startS, 1.2f);
            game.sound.Play("SFX_Transition", 0.9f);
            Hud.Title("ГЛАВА 2 · БЛЯШКА", "кровоток слабеет — найди причину", 4f);
            Prof.Appear(true);
            yield return Wait(2.5f);
            Objective("Посмотри на кровоток на запястье");
            yield return Say(C("Мы ниже по течению. Подними левую руку: кровоток здесь меньше нормы.", "Мы ниже по течению. Посмотри на панель внизу слева: кровоток здесь меньше нормы."), Mood.Serious);
            yield return Say("Что-то сужает артерию. Плыви вперёд, найди это и отсканируй.", Mood.Neutral);
        }

        IEnumerator Diagnose()
        {
            Beat = 1;
            var a = V.plaques[0];
            Objective("Найди, что мешает кровотоку");
            Prompt("Левый стик — плыть · держись течения", "WASD — плыть · течение поможет");
            float t = 0;
            while (Vector3.Distance(game.Head, Crest(0)) > 16f && !autoplay)
            {
                t += Time.unscaledDeltaTime;
                if (t > 10f) Hud.SetCompass(Crest(0), "препятствие");
                yield return null;
            }
            Hud.SetCompass(null, null);
            yield return Say("Вот оно: стенка вздулась внутрь сосуда. Отсканируй!", Mood.Surprised);
            Objective("Отсканируй вздутие на стенке");
            Prompt("Левый курок — сканировать", "ПКМ / F — сканировать");
            while (!game.codex.Has("plaque"))
            {
                if (autoplay) game.scanner.ForceScan("plaque", Crest(0));
                Hud.SetCompass(Crest(0), "бляшка");
                yield return null;
            }
            Hud.SetCompass(null, null);
            // x-ray: the lipid core lights up under the cap while the knowledge moment plays
            StartCoroutine(Reveal(0, 1f, 6f));
            yield return WaitKnowledge();
            yield return Say("На бляшке сидит странная клетка. Отсканируй и её.", Mood.Neutral);
            Objective("Отсканируй клетку на бляшке");
            while (!game.codex.Has("foam"))
            {
                if (autoplay && foamCell) game.scanner.ForceScan("foam", foamCell.transform.position);
                if (foamCell) Hud.SetCompass(foamCell.transform.position, "клетка");
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            yield return WaitKnowledge();
            // predict
            yield return Say("Самое опасное в бляшке — не размер. Как думаешь, что?", Mood.Serious);
            if (autoplay) game.question.ForcedChoice = 1;
            yield return game.question.Ask("Если покрышка бляшки порвётся, что произойдёт?", new[] { "кровь вытечет из сосуда", "на разрыве вырастет тромб", "бляшка сразу исчезнет" }, 1);
            bool right = game.question.LastChoice == 1; if (right) CorrectPredictions++;
            yield return Say(right ? "Верно. На разрыве сразу начнёт расти тромб — поэтому покрышку надо беречь." : "Нет — кровь из сосуда не выйдет. А вот на разрыве вырастет тромб. Поэтому покрышку надо беречь.", right ? Mood.Smile : Mood.Neutral);
        }

        IEnumerator Reveal(int plaque, float to, float seconds)
        {
            float from = V.plaques[plaque].reveal;
            for (float k = 0; k < 1f; k += Time.deltaTime / 0.6f) { var p = V.plaques[plaque]; p.reveal = Mathf.Lerp(from, to, k); V.plaques[plaque] = p; yield return null; }
            yield return new WaitForSeconds(seconds);
            for (float k = 0; k < 1f; k += Time.deltaTime / 1.2f) { var p = V.plaques[plaque]; p.reveal = Mathf.Lerp(to, 0.45f, k); V.plaques[plaque] = p; yield return null; }
        }

        IEnumerator MeltCore()
        {
            Beat = 2;
            var o = game.organism;
            // anchor opposite the plaque: a steady firing position
            var a = V.plaques[0];
            anchorPad.position = V.PointAt(a.s - 3f, V.Radius(a.s - 3f) * 0.45f, a.angleDeg + 180f);
            anchorPad.rotation = Quaternion.LookRotation(Crest(0) - anchorPad.position);
            anchorPad.gameObject.SetActive(true);
            Objective("Прицепись к маяку напротив бляшки");
            Prompt("Хват (grip) — якорь", "E — якорь");
            yield return Say("Закрепись: прицепись якорем к маяку напротив бляшки.", Mood.Neutral);
            while (!game.swimmer.Anchored)
            {
                float d = Vector3.Distance(game.Head, anchorPad.position);
                Hud.SetCompass(anchorPad.position, "маяк");
                if ((d < 4.5f && game.input.AnchorDown) || autoplay) { game.swimmer.AttachTo(anchorPad); Bvr2Haptics.Both(0.5f, 0.1f); }
                else if (game.input.AnchorDown) Hud.Toast("Подплыви ближе к маяку", Bvr2Style.TextDim, 1.2f);
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            Objective("Растопи жир в бляшке");
            for (float w = 0; w < 0.9f && !autoplay; w += Time.unscaledDeltaTime) yield return null;
            anchorPad.gameObject.SetActive(false);
            // the lipid windows appear
            foreach (var l in lipids) if (l) { l.ResetTarget(); l.organism = o; l.transform.position = V.SurfacePoint(l.s, l.angleDeg, 0.05f); }
            game.fx.Shock(Crest(0), game.Head - Crest(0), new Color(1f, 0.8f, 0.3f), 0.5f, 4f, 0.8f);
            // new ability
            game.abilities.impulseUnlocked = true; game.abilities.SetMode(Bvr2Abilities.Mode.Impulse, false);
            Hud.Toast("+ СПОСОБНОСТЬ · ИМПУЛЬС", new Color(1f, 0.8f, 0.3f), 2.8f);
            game.sound.Play("SFX_Reveal", 0.9f);
            StartCoroutine(Reveal(0, 0.8f, 1.5f));
            yield return Say("Новый режим BioTool — Импульс. Он растапливает жир.", Mood.Smile);
            yield return Say("Но покрышка тонкая. Стреляй, когда кольцо зелёное. Жёлтое — сейчас придёт пульсовая волна и закроет окно.", Mood.Serious);
            Prompt("Правый курок — Импульс · B — сменить режим", "ЛКМ — Импульс · Q — сменить режим");
            bool warnedClosed = false, warnedCrack = false; float autoTimer = 0;
            while (Melted < lipids.Count && CapIntegrity > 0.01f)
            {
                Hud.SetObjective($"Растопи жир Импульсом: {Melted}/{lipids.Count}  ·  покрышка {Mathf.RoundToInt(CapIntegrity * 100)} %");
                if (game.abilities.Current != Bvr2Abilities.Mode.Impulse && Time.frameCount % 120 == 0) Hud.Toast(C("B — включить Импульс", "Q — включить Импульс"), Bvr2Style.TextDim, 1.2f);
                if (CapHits > 0 && !warnedClosed) { warnedClosed = true; Prof.Say("Окно было закрыто — Импульс ударил в покрышку! Дождись, пока пройдёт пульсовая волна.", Mood.Serious); }
                if (CapIntegrity < 0.4f && !warnedCrack) { warnedCrack = true; Prof.Say("Покрышка трещит! Ещё одна ошибка — и она лопнет.", Mood.Surprised); }
                if (autoplay)
                {
                    autoTimer += Time.unscaledDeltaTime;
                    if (autoTimer > 0.4f)
                        foreach (var l in lipids)
                            if (l && l.Alive && l.gameObject.activeSelf && l.Open && !l.Closing && Bvr2LipidTarget.WindowOpen(o, l.s, 0.12f, 0.25f))
                            { var tip = game.impulse.tip ? game.impulse.tip.position : game.rig.rightHand.position; game.impulse.Fire((l.transform.position - tip).normalized); autoTimer = 0; break; }
                }
                yield return null;
            }
            NoPrompt();
            game.swimmer.Detach();
            if (CapIntegrity <= 0.01f) { PlayerRuptured = true; ruptured = 0; }
            else
            {
                game.sound.Play("SFX_Success_Step", 0.9f);
                yield return Say("Жир растоплен, просвет почти свободен! Посмотри, как вырос кровоток.", Mood.Smile);
            }
        }

        void OnTarget(Bvr2LipidTarget t, bool open)
        {
            if (open)
            {
                Melted++;
                var p = V.plaques[0]; p.height = baseA.height * Mathf.Lerp(0.22f, 1f, 1f - Melted / (float)Mathf.Max(1, lipids.Count)); V.plaques[0] = p;
                StartCoroutine(Reseat());
                game.fx.Burst(t.transform.position, new Color(1f, 0.8f, 0.3f), 34, 2.2f, 0.06f, 0.8f);
                game.fx.Shock(t.transform.position, game.Head - t.transform.position, new Color(1f, 0.85f, 0.4f), 0.1f, 1.2f, 0.4f);
                game.sound.Play("BVR2_Melt", 0.9f, 0.95f + Melted * 0.05f);
                Bvr2Haptics.Pulse(false, 0.5f, 0.08f);
            }
            else CapHit(t.transform.position);
        }

        void OnWall(Vector3 p, float inset)
        {
            if (Beat != 2) return;
            if (inset > 0.4f && Vector3.Distance(p, Crest(0)) < baseA.halfLength + 2f) CapHit(p);
        }

        /// <summary>The plaque shrank: the remaining droplets sink with its surface.</summary>
        IEnumerator Reseat()
        {
            for (float k = 0; k < 1f; k += Time.deltaTime / 0.6f)
            {
                foreach (var l in lipids) if (l && l.Alive && l.gameObject.activeSelf) l.transform.position = Vector3.Lerp(l.transform.position, V.SurfacePoint(l.s, l.angleDeg, 0.05f), k);
                yield return null;
            }
        }

        void CapHit(Vector3 at)
        {
            CapHits++;
            CapIntegrity = Mathf.Max(0f, CapIntegrity - 0.34f);
            var pl = V.plaques[0]; pl.rupture = (1f - CapIntegrity) * 0.4f; V.plaques[0] = pl;
            game.fx.Burst(at, new Color(1f, 0.35f, 0.25f), 26, 2.4f, 0.05f, 0.6f);
            game.sound.Play("BVR2_Crack", 1f, 0.9f + CapHits * 0.08f);
            Bvr2Haptics.Both(0.6f, 0.12f);
            game.environment.alarm = 0.35f;
        }

        IEnumerator Rupture()
        {
            Beat = 3;
            var o = game.organism;
            if (ruptured < 0)
            {
                // the careful player did nothing wrong: an unstable neighbour gives way when the pressure jumps
                yield return Wait(1f);
                o.stressDrive = 28f; game.sound.Play("BVR2_Alarm", 0.9f); game.sound.Mood("tension");
                for (float a = 0; a < 1f; a += Time.unscaledDeltaTime / 1.2f) { game.environment.alarm = a * 0.7f; yield return null; }
                yield return Say("Стоп… давление подскочило. Смотри на соседнюю бляшку!", Mood.Surprised);
                ruptured = 1;
            }
            else { game.sound.Mood("tension"); game.environment.alarm = 0.7f; }
            // the cap tears
            var pl = V.plaques[ruptured];
            Vector3 crest = Crest(ruptured), inward = (V.Centre(pl.s) - crest).normalized;
            Hud.SetCompass(crest, "разрыв");
            Bvr2Time.SlowMo(0.45f, 5f);
            for (float k = 0; k < 1f; k += Time.unscaledDeltaTime / 0.5f) { var p = V.plaques[ruptured]; p.rupture = Mathf.Lerp(p.rupture, 1f, k); V.plaques[ruptured] = p; yield return null; }
            game.fx.Shock(crest, inward, Bvr2Style.Danger, 0.3f, 4.5f, 0.8f);
            game.fx.Burst(crest + inward * 0.4f, new Color(0.9f, 0.2f, 0.15f), 60, 3f, 0.07f, 0.9f);
            game.sound.Play("BVR2_Crack", 1f, 0.7f);
            Bvr2Haptics.Both(0.8f, 0.25f);
            if (PlayerRuptured) yield return Say("Покрышка лопнула!", Mood.Surprised);
            else yield return Say("Её покрышка была совсем тонкой — и не выдержала.", Mood.Serious);
            // thrombus grows on the tear: platelets rush in
            thrombus.transform.position = crest - inward * 0.2f; thrombus.transform.rotation = Quaternion.LookRotation(inward);
            thrombus.growth = 0; thrombus.dissolve = 0; thrombus.gameObject.SetActive(true);
            for (int i = 0; i < 18; i++) game.blood.Inject(CellKind.Platelet, Random.Range(-6f, 2f) + (pl.s - game.PlayerS), Random.Range(0.4f, 0.9f), pl.angleDeg + Random.Range(-40f, 40f));
            Objective("Отсканируй то, что растёт на разрыве");
            Prompt("Левый курок — сканировать", "ПКМ / F — сканировать");
            Prof.Say("Смотри: на разрыв слетаются тромбоциты. Отсканируй, что там растёт!", Mood.Surprised);
            float grow = 0;
            while (!game.codex.Has("thrombus"))
            {
                grow = Mathf.Min(1f, grow + Time.unscaledDeltaTime / 6f); thrombus.growth = grow;
                if (autoplay && grow > 0.3f) game.scanner.ForceScan("thrombus", thrombus.transform.position);
                Hud.SetCompass(thrombus.transform.position, "разрыв");
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            while (grow < 1f) { grow = Mathf.Min(1f, grow + Time.unscaledDeltaTime / 3f); thrombus.growth = grow; yield return null; }
            yield return WaitKnowledge();
            // a piece tears off
            yield return Say("Тромб растёт прямо в потоке… Осторожно!", Mood.Serious);
            thrombus.growth = 0.65f;
            embolus.stopAtS = narrowS;
            embolus.Launch(pl.s + 1.5f, pl.angleDeg);
            game.fx.Burst(embolus.transform.position, new Color(0.9f, 0.2f, 0.2f), 40, 3f, 0.06f, 0.7f);
            game.Reveal("embolus", embolus.transform.position);
            yield return WaitKnowledge();
        }

        IEnumerator Chase()
        {
            Beat = 4;
            var o = game.organism;
            if (!embolus.gameObject.activeSelf || embolus.Current == Bvr2Embolus.State.Idle)
            {
                // started directly at this beat
                var b = V.plaques[1]; ruptured = 1;
                var p = V.plaques[1]; p.rupture = 1f; V.plaques[1] = p;
                thrombus.transform.position = Crest(1); thrombus.transform.rotation = Quaternion.LookRotation(V.Centre(b.s) - Crest(1));
                thrombus.growth = 0.65f; thrombus.gameObject.SetActive(true);
                embolus.stopAtS = narrowS; embolus.Launch(b.s + 1.5f, b.angleDeg);
                PreDiscover("plaque", "foam", "thrombus", "embolus");
            }
            float launchS = embolus.S;
            game.abilities.SetMode(Bvr2Abilities.Mode.Attract);
            game.bioTool.locked = true; // the beam is for the embolus now, not for cells
            game.sound.Mood("tension"); o.stressDrive = 12f;
            yield return Say("Он плывёт к узкой ветви. Догони и поймай его лучом Притяжения!", Mood.Surprised);
            Prompt("Рывок — A/X · правый курок на эмбол — держать луч", "Shift — рывок · ЛКМ на эмбол — держать луч");
            bool caughtSaid = false;
            System.Action onCaught = () => { if (!caughtSaid) { caughtSaid = true; Prof.Say("Держу! Ввожу тромболитик — не отпускай луч!", Mood.Serious); } };
            System.Action onEscaped = () => { Hud.Toast("Вырвался! Лови снова", Bvr2Style.Danger, 1.6f); caughtSaid = false; };
            embolus.OnCaught += onCaught; embolus.OnEscaped += onEscaped;
            while (true)
            {
                if (embolus.Current == Bvr2Embolus.State.Dissolved) break;
                if (embolus.Current == Bvr2Embolus.State.Lodged)
                {
                    Lodgings++;
                    yield return Occlusion();
                    // retry from the tear
                    embolus.Launch(launchS, V.plaques[Mathf.Max(0, ruptured)].angleDeg);
                    Arrive(launchS - 9f, 0.8f);
                    o.lesionConductance = 0.6f; game.environment.alarm = 0.4f;
                    yield return Say("Ещё раз — быстрее! Используй рывок.", Mood.Serious);
                    Prompt("Рывок — A/X · правый курок на эмбол — держать луч", "Shift — рывок · ЛКМ на эмбол — держать луч");
                    continue;
                }
                float left = Mathf.Max(0, narrowS - embolus.S);
                Hud.SetCompass(embolus.transform.position, "эмбол");
                if (embolus.Current == Bvr2Embolus.State.Caught) Hud.SetObjective($"Держи луч! Растворение {Mathf.RoundToInt(embolus.Lysis * 100)} %  ·  до узкой ветви {left:0} м");
                else Hud.SetObjective($"Поймай эмбол лучом!  Захват {Mathf.RoundToInt(embolus.CatchProgress * 100)} %  ·  до узкой ветви {left:0} м");
                game.environment.alarm = Mathf.Lerp(game.environment.alarm, Mathf.Lerp(0.8f, 0.25f, left / Mathf.Max(1f, narrowS - launchS)), Time.unscaledDeltaTime);
                if (autoplay)
                {
                    if (Vector3.Distance(game.Head, embolus.transform.position) > 6f)
                    {
                        float sP = Mathf.Max(launchS, embolus.S - 5f);
                        game.swimmer.Teleport(V.Centre(sP), (embolus.transform.position - V.Centre(sP)).normalized);
                    }
                    embolus.forceAim = true;
                }
                yield return null;
            }
            embolus.forceAim = false;
            embolus.OnCaught -= onCaught; embolus.OnEscaped -= onEscaped;
            CatchDistanceLeft = Mathf.Clamp01((narrowS - embolus.S) / Mathf.Max(1f, narrowS - launchS));
            Hud.SetCompass(null, null); NoPrompt();
            game.bioTool.locked = false;
            o.stressDrive = 0; game.sound.Mood("recovery");
            game.Reveal("lysis", game.Head + game.rig.head.transform.forward * 2f);
            yield return WaitKnowledge();
            // the thrombolytic reaches the thrombus on the tear too
            for (float k = 0; k < 1f; k += Time.unscaledDeltaTime / 3f) { thrombus.dissolve = k; game.environment.alarm = Mathf.Lerp(game.environment.alarm, 0, k); yield return null; }
            thrombus.gameObject.SetActive(false);
            if (!game.codex.Has("ischemia"))
            {
                yield return Say("Успели! А если бы эмбол застрял…", Mood.Neutral);
                game.Reveal("ischemia", game.Head + game.rig.head.transform.forward * 2f);
                yield return WaitKnowledge();
            }
            yield return Say("Кровоток восстановлен. Посмотри на запястье — всё снова в зелёной зоне.", Mood.Smile);
        }

        IEnumerator Occlusion()
        {
            game.environment.alarm = 1f; game.sound.Play("BVR2_Alarm", 1f);
            Hud.SetCompass(null, null);
            Bvr2Time.SlowMo(0.5f, 3f);
            if (!game.codex.Has("ischemia")) { game.Reveal("ischemia", embolus.transform.position); yield return WaitKnowledge(); }
            else yield return Say("Закупорка! Ткани за ней остались без кислорода.", Mood.Sad);
            yield return Wait(1.5f);
        }

        IEnumerator Debrief()
        {
            Beat = 5;
            Objective("Глава 2 пройдена");
            bool capStar = !PlayerRuptured && CapHits <= 1;
            bool chaseStar = Lodgings == 0 && CatchDistanceLeft >= 0.4f;
            Stars = 1 + (capStar ? 1 : 0) + (chaseStar ? 1 : 0);
            string[] heads = { "БЛЯШКА", "ТРОМБ И ЭМБОЛ", "БЕРЕГИ СОСУДЫ" };
            string[] bodies = {
                "жир (холестерин) копится в стенке\nсверху — тонкая фиброзная покрышка\nбляшка сужает просвет → меньше крови",
                "покрышка рвётся → на разрыве тромб\nоторвался кусок — эмбол\nзастрял в узком сосуде → ишемия\n(сердце — инфаркт, мозг — инсульт)",
                "больше двигаться\nменьше жирной и солёной еды\nне курить\nследить за давлением" };
            string why = $"★ глава пройдена   {(capStar ? "★" : "☆")} покрышка цела (ошибок {CapHits})   {(chaseStar ? "★" : "☆")} эмбол пойман задолго до ветви";
            board = ShowBoard("ГЛАВА 2 · БЛЯШКА — ПРОЙДЕНА", Stars, heads, bodies, why);
            Prof.Present(board.position, 12f, 1f, 1.05f);
            yield return BoardIn(board);
            yield return Say(Stars == 3 ? "Ювелирная работа! Три звезды." : "Справился! Пройди ещё раз, чтобы собрать все звёзды.", Mood.Smile);
            yield return WaitContinue();
            yield return BoardOut(board);
            if (autoplay) game.question.ForcedChoice = 0;
            yield return game.question.Ask("Почему эмбол опасен?", new[] { "закупоривает сосуд — ткани без кислорода", "ускоряет кровоток", "разрушает эритроциты" }, 0, "ПРОВЕРЬ СЕБЯ");
            yield return Say(game.question.LastChoice == 0 ? "Точно. Поэтому при инфаркте и инсульте счёт идёт на минуты." : "Главное другое: эмбол закупоривает сосуд, и ткани остаются без кислорода.", Mood.Neutral);
            Finished = true;
            Objective("Глава 2 пройдена");
            if (autoplay) yield break;
            if (board) { Destroy(board.gameObject); board = null; }
            yield return WhatNext("Плавай сколько хочешь. Импульс — B / Q.");
        }

        // ---------------- debug hooks -------------------------------------------------------
        public void DebugAnchor() { if (anchorPad && anchorPad.gameObject.activeInHierarchy) { var p = anchorPad.position; game.swimmer.Teleport(p + (Crest(0) - p).normalized * -1f, (Crest(0) - p).normalized); game.swimmer.AttachTo(anchorPad); } }
        public void DebugScan(string id) { game.scanner.ForceScan(id, game.Head + game.rig.head.transform.forward * 2f); }
        public void DebugChoose(int option) { game.question.ForcedChoice = option; }
        public void DebugLookAtPlaque(int i) { game.rig.LookAt(Crest(Mathf.Clamp(i, 0, 1))); }
        public void DebugLookAtRupture() { if (ruptured >= 0) game.rig.LookAt(Crest(ruptured)); }
        public void DebugFireAtLipid() { foreach (var l in lipids) if (l && l.Alive && l.gameObject.activeSelf) { var tip = game.impulse.tip ? game.impulse.tip.position : game.rig.rightHand.position; game.impulse.Fire((l.transform.position - tip).normalized); return; } }
        public void DebugGoToEmbolus() { if (embolus && embolus.gameObject.activeSelf) { float s = embolus.S - 5f; game.swimmer.Teleport(V.Centre(s), (embolus.transform.position - V.Centre(s)).normalized); } }
        public void DebugHoldEmbolus(bool on) { if (embolus) embolus.forceAim = on; }
    }
}
