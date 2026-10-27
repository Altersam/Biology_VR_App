using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// Chapter 5 «Очаг» — the final battle (scene 15 of the script). The artery widens at a fork; a patch of the
    /// wall is infected: swollen cells bud new virions, fever is high. Phases: hold the line with antibodies +
    /// Signal and the allies (neutrophils, macrophages) while the scanner works on the key; armoured "hedgehogs"
    /// shrug ordinary antibodies off → the key puzzle (fit the antibody's paratope to the epitope, slow motion)
    /// gives specific antibodies → traitors: infected cells show flags, mark them with the Signal and the
    /// T-killers make them die by apoptosis (a healthy cell marked by mistake gets damaged) → the last surge:
    /// Synchronisation as a T-killer, destroying freshly infected cells while antibodies in the blood clear the
    /// virions → victory, fever falls, immune memory → debrief.
    /// </summary>
    public sealed class Bvr2Chapter5 : Bvr2ChapterBase
    {
        public override int Number => 5;
        public override string Title => "Очаг";

        public float startS = 2140f, arenaS = 2197f, focusAngle = 250f;
        public List<Bvr2WallCell> cells = new List<Bvr2WallCell>();
        public List<Bvr2TKiller> killers = new List<Bvr2TKiller>();
        public Bvr2KeyPuzzle key;

        public float KeyProgress { get; private set; }
        public float PeakTemp { get; private set; }
        public int HealthyHit { get; private set; }
        public int SpotsKilledBySync { get; private set; }
        public int Phase { get; private set; }
        public override string StatusLine => base.StatusLine + $" · key {KeyProgress:F2} · alive {(Imm ? Imm.AliveViruses : 0)} · spots {(Imm ? Imm.SpotCount : 0)} · infectedCells {AliveInfected} · load {game.organism.viralLoad:F2} · t {game.organism.temperature:F1} · peak {PeakTemp:F1} · healthyHit {HealthyHit} · keyMistakes {(key ? key.Mistakes : 0)} · syncKills {SpotsKilledBySync}";

        Bvr2Immune Imm => game.immune;
        Transform board; bool hooked, glanced; float keyBonus, ambientBefore = -1;
        int AliveInfected { get { int n = 0; foreach (var c in cells) if (c && c.InfectedAlive) n++; return n; } }

        void Start() { HideProps(); }

        void HideProps()
        {
            foreach (var c in cells) if (c) c.gameObject.SetActive(false);
            foreach (var k in killers) if (k) k.gameObject.SetActive(false);
            if (key) key.Hide();
        }

        protected override void ResetProps()
        {
            ResetShared();
            HideProps();
            if (board) Destroy(board.gameObject);
            if (game.abilities) { game.abilities.signalUnlocked = false; game.abilities.SetMode(Bvr2Abilities.Mode.Attract, false); }
            game.bioTool.locked = false; game.swimmer.frozen = false;
            if (ambientBefore > 0) { game.environment.ambientStrength = ambientBefore; ambientBefore = -1; }
            for (int i = 0; i < Imm.phagocytes.Count; i++) if (Imm.phagocytes[i]) { Imm.phagocytes[i].tint = new Color(0, 0, 0, 0); Imm.phagocytes[i].transform.localScale = Vector3.one; }
            KeyProgress = 0; PeakTemp = 36.6f; HealthyHit = 0; SpotsKilledBySync = 0; Phase = 0; keyBonus = 0; glanced = false;
        }

        void Hook()
        {
            if (hooked) return; hooked = true;
            Imm.OnSignal += (p, beat) => { if (Running && Phase == 3) Dispatch(p); };
            Imm.OnGlance += v =>
            {
                if (!Running || Phase != 1) return;
                keyBonus += 0.02f;
                if (!glanced) { glanced = true; Prof.Say("Ёжик! По броне обычные антитела соскальзывают. Но держи на нём сканер — так ключ найдётся быстрее.", Mood.Serious); }
            };
            foreach (var c in cells)
            {
                if (!c) continue;
                c.OnApoptosis += cell => { if (Running && !game.codex.Has("apoptosis")) game.Reveal("apoptosis", cell.transform.position + cell.transform.up * 1.5f); };
                c.OnDamaged += cell => { if (!Running) return; HealthyHit++; Prof.Say("Это была здоровая клетка — на ней не было флажков! Т-киллер повредил стенку.", Mood.Serious); };
            }
        }

        void Update()
        {
            if (!Running) return;
            var o = game.organism;
            // infected wall cells keep the viral load (and the fever) up
            if (Phase < 5) o.viralLoad = Mathf.Max(o.viralLoad, 0.08f * AliveInfected);
            PeakTemp = Mathf.Max(PeakTemp, o.temperature);
            // fever: a hot, trembling rim of the view
            if (!game.sync.Active)
            {
                float fever = Mathf.Clamp01((o.temperature - 37.4f) / 2f);
                game.rig.SetOverlay(new Color(1f, 0.45f, 0.15f, 1f), fever * 0.42f + fever * 0.06f * Mathf.Sin(Time.time * 23f) * Mathf.Sin(Time.time * 7f));
            }
            if (Time.frameCount % 15 == 0) Hud.SetCounters($"ЗНАНИЯ {game.codex.FoundIn(5)}/{game.codex.CountIn(5)}", $"УНИЧТОЖЕНО {Imm.Killed}");
        }

        // ---------------- the chapter -------------------------------------------------------
        protected override IEnumerator Run()
        {
            ResetProps(); Hook();
            var o = game.organism; o.ResetState();
            Hud.showTemperature = true;
            game.scanner.locked = false; game.scanner.allowPlasma = false;
            game.abilities.impulseUnlocked = true;
            Imm.replicate = true; Imm.maxAlive = 44;
            SetupCells();
            // the fork: the mouth of a side branch opens in the wall opposite the focus
            var env = game.environment;
            env.windowOn = true; env.windowS = arenaS + 16f; env.windowAngle01 = Mathf.Repeat(focusAngle + 180f, 360f) / 360f;
            env.windowHalfLength = 5f; env.windowHalfAngle01 = 0.085f; env.windowRagged = 0f; env.windowSlit = true; env.windowEdge = new Color(0.95f, 0.5f, 0.5f, 1f);
            o.viralLoad = 0.55f; o.temperature = 38.1f;
            game.sound.Mood("tension");
            if (startBeat <= 0) yield return Intro();
            else
            {
                Arrive(startBeat <= 1 ? arenaS - 22f : arenaS - 6f); Prof.Appear(true);
                if (startBeat >= 2) game.codex.Discover("infected");
                if (startBeat >= 2) DeployAllies();
                if (startBeat >= 3) { game.codex.Discover("neutrophil"); game.antibodies.unlocked = true; game.abilities.signalUnlocked = true; game.abilities.SetMode(Bvr2Abilities.Mode.Signal, false); }
                if (startBeat >= 4) { game.codex.Discover("specificity"); game.antibodies.specific = true; }
                if (startBeat >= 5) game.codex.Discover("tkiller");
                if (startBeat >= 6) { game.codex.Discover("apoptosis"); foreach (var c in cells) if (c && c.InfectedAlive) c.Hit(); o.viralLoad = 0.35f; }
                if (startBeat >= 8) { o.viralLoad = 0f; Phase = 6; }
            }
            if (startBeat <= 1) yield return Scan();
            if (startBeat <= 2) yield return Hold();
            if (startBeat <= 3) yield return Key();
            if (startBeat <= 5) yield return Traitors();
            if (startBeat <= 6) yield return Surge();
            if (startBeat <= 7) yield return Victory();
            yield return Debrief();
        }

        void SetupCells()
        {
            // a 4 × 3 patch of wall cells; infected ones scattered among healthy ones
            bool[] pattern = { true, false, true, false, true, true, true, true, false, false, true, false };
            float[] ds = { -9f, -3f, 3f, 9f }; float[] da = { -24f, 0f, 24f };
            for (int i = 0; i < cells.Count && i < pattern.Length; i++)
            {
                var c = cells[i]; if (!c) continue;
                c.Setup(game.vessel, Imm, game.fx, game.sound, arenaS + ds[i / 3] + (i % 2) * 0.8f, focusAngle + da[i % 3] + ((i / 3) % 2) * 5f, pattern[i]);
                var sc = c.GetComponent<Bvr2Scannable>(); if (sc) sc.scannable = pattern[i];
            }
        }

        void DeployAllies()
        {
            var head = game.rig.head.transform;
            Imm.Deploy(4, head);
            // two of the four phagocytes are neutrophils: smaller, lilac
            for (int i = 2; i < Imm.phagocytes.Count; i++) if (Imm.phagocytes[i]) { Imm.phagocytes[i].tint = new Color(0.86f, 0.82f, 1f, 1f); Imm.phagocytes[i].transform.localScale = Vector3.one * 0.72f; }
        }

        IEnumerator Intro()
        {
            Beat = 0; Phase = 0;
            Arrive(startS, 1.2f);
            game.sound.Play("SFX_Transition", 0.9f);
            Hud.Title("ГЛАВА 5 · ОЧАГ", "финальная битва с вирусами", 4f);
            Prof.Appear(true);
            yield return Wait(2.5f);
            Objective("Плыви к очагу");
            yield return Say("Вот он, очаг инфекции: стенка у развилки заражена, и из её клеток выходят новые вирусы. Температура 38 и растёт.", Mood.Serious);
            if (autoplay) Arrive(arenaS - 22f, 0.4f);
            float t = 0;
            while (game.PlayerS < arenaS - 26f)
            {
                t += Time.unscaledDeltaTime;
                if (t > 6f) Hud.SetCompass(cells[0].transform.position, "очаг");
                yield return null;
            }
            Hud.SetCompass(null, null);
        }

        IEnumerator Scan()
        {
            Beat = 1; Phase = 0;
            Objective("Отсканируй раздутую клетку стенки");
            Prompt("Левый курок — сканировать", "ПКМ / F — сканировать");
            yield return Say("Отсканируй раздутую клетку — посмотрим, что с ней.", Mood.Neutral);
            Bvr2WallCell target = null; foreach (var c in cells) if (c && c.infected) { target = c; break; }
            while (!game.codex.Has("infected"))
            {
                if (target) Hud.SetCompass(Vector3.Angle(game.rig.head.transform.forward, target.transform.position - game.Head) > 30f ? target.transform.position : (Vector3?)null, "клетка");
                if (autoplay && target) game.scanner.ForceScan("infected", target.transform.position);
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            yield return WaitKnowledge();
        }

        // ---------------- phase 1: hold ------------------------------------------------------
        IEnumerator Hold()
        {
            Beat = 2; Phase = 1;
            var sw = game.swimmer; sw.regionMin = arenaS - 24f; sw.regionMax = arenaS + 22f;
            if (Imm.phagocytes.Count > 0 && !Imm.phagocytes[0].gameObject.activeSelf) DeployAllies();
            game.Reveal("neutrophil", game.Head + game.rig.head.transform.forward * 2.2f);
            yield return WaitKnowledge();
            game.antibodies.unlocked = true; game.abilities.signalUnlocked = true; game.abilities.SetMode(Bvr2Abilities.Mode.Signal, false);
            yield return Say("Левая рука — антитела, правая — Сигнал для фагоцитов. Держись, пока сканер подбирает ключ к этому вирусу!", Mood.Surprised);
            Objective("Держись, пока сканер ищет ключ");
            foreach (var c in cells) if (c) { c.releasing = true; c.releaseEvery = 16f; }
            Prompt("Левый курок — антитела · правый — Сигнал", "ПКМ — антитела · ЛКМ — Сигнал");
            float t = 0, spawnT = 0, autoT = 0; int spawned = 0;
            float duration = autoplay ? 6f : 70f;
            while (KeyProgress < 1f)
            {
                float dt = Time.unscaledDeltaTime; t += dt; spawnT += dt;
                KeyProgress = Mathf.Clamp01(t / duration + keyBonus);
                if (spawnT > 2.6f && Imm.AliveViruses < 20)
                {
                    spawnT = 0; spawned++;
                    if (spawned % 6 == 0) Swarm();
                    else SpawnLane(spawned % 3, spawned % 4 == 0);
                }
                Hud.SetObjective($"Держись!  Ключ {Mathf.RoundToInt(KeyProgress * 100)} %  ·  вирусов {Imm.AliveViruses}  ·  {game.organism.temperature:0.0}°");
                if (autoplay)
                {
                    autoT += dt;
                    if (autoT > 0.25f)
                    {
                        autoT = 0;
                        foreach (var v in Imm.viruses) if (v.gameObject.activeSelf && v.Alive && !v.Tagged && !v.debris) { if (v.armored) Imm.Glance(v); else { v.AddTag(1f); Imm.Signal(v.transform.position, Imm.OnTheBeat); } break; }
                    }
                }
                yield return null;
            }
            NoPrompt();
            game.sound.Play("SFX_Reveal", 0.9f);
        }

        /// <summary>Lanes: 0 — with the stream from upstream, 1 — from the top of the arena, 2 — the infected wall itself.</summary>
        void SpawnLane(int lane, bool hedgehog)
        {
            float up = game.vessel.AngleOf(arenaS, Vector3.up);
            switch (lane)
            {
                case 0: Imm.Spawn(arenaS - 22f + Random.Range(-3f, 3f), Random.Range(0f, 360f), Random.Range(0.1f, 0.4f), hedgehog ? 0.5f : 0.8f, 7f, hedgehog); break;
                case 1: Imm.Spawn(arenaS + Random.Range(-8f, 8f), up + Random.Range(-30f, 30f), 0.55f, hedgehog ? 0.45f : 0.85f, 7f, hedgehog); break;
                default:
                    var c = cells[Random.Range(0, cells.Count)];
                    Imm.Spawn(c.s, c.angleDeg + Random.Range(-10f, 10f), 0.45f, hedgehog ? 0.45f : 0.8f, 7f, hedgehog); break;
            }
        }

        void Swarm()
        {
            float s0 = arenaS - 18f, a0 = Random.Range(0f, 360f);
            for (int i = 0; i < 5; i++) Imm.Spawn(s0 + Random.Range(-1.2f, 1.2f), a0 + Random.Range(-12f, 12f), 0.3f + Random.Range(-0.08f, 0.08f), 0.85f, 6.5f);
        }

        // ---------------- phase 2: the key -------------------------------------------------
        IEnumerator Key()
        {
            Beat = 3; Phase = 2;
            var head = game.rig.head.transform;
            Vector3 f = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized; if (f.sqrMagnitude < 0.01f) f = head.forward;
            game.swimmer.frozen = true;
            Bvr2Time.SlowMo(0.08f, 900f);
            Bvr2Environment.Current?.SetFocus(true);
            key.Show(game.rig.transform, head.position + f * 0.85f - Vector3.up * 0.05f, Quaternion.LookRotation(f, Vector3.up));
            game.blood.SetFocus(head.position + f * 0.85f, 0.8f);
            Hud.TopHidden = false;
            Objective("Подбери ключ: эпитоп к антителу");
            Prompt("Стик — вращать вирион · курок — пристыковать", "WASD — вращать вирион · ЛКМ — пристыковать");
            yield return Say("Сканер поймал ёжика! Поверни вирион так, чтобы шип с таким же кончиком, как у антитела, смотрел на антитело, — и пристыкуй.", Mood.Neutral);
            float autoT = 0; int autoStep = 0;
            while (!key.Done)
            {
                Hud.SetObjective($"Подбери ключ: совпадений {key.Locks}/{key.needed}");
                if (autoplay)
                {
                    autoT += Time.unscaledDeltaTime;
                    if (autoT > 0.3f)
                    {
                        autoT = 0;
                        if (autoStep == 0) { key.AutoAlignWrong(); key.TryDock(); } // check the bounce once
                        else { key.AutoAlign(); key.TryDock(); }
                        autoStep++;
                    }
                }
                if (key.Mistakes == 1 && !autoplay && !Prof.Busy && Time.frameCount % 600 == 0) Prof.Say("Этот кончик не подходит. Ищи шип с колечком — как на концах антитела.", Mood.Neutral);
                yield return null;
            }
            Hud.SetObjective($"Подбери ключ: совпадений {key.Locks}/{key.needed}");
            game.sound.Play("SFX_Success_Step", 1f);
            for (float w = 0; w < (autoplay ? 0.1f : 0.8f); w += Time.unscaledDeltaTime) yield return null;
            key.Hide(); NoPrompt();
            Bvr2Time.Release(); game.swimmer.frozen = false;
            Bvr2Environment.Current?.SetFocus(false); game.blood.ClearFocus();
            game.antibodies.specific = true;
            Hud.Toast("+ СПЕЦИФИЧЕСКИЕ АНТИТЕЛА", Bvr2Style.Knowledge, 2.8f);
            game.Reveal("specificity", game.Head + game.rig.head.transform.forward * 2f);
            yield return WaitKnowledge();
            yield return Say("Теперь одно попадание антителами — и вирион нейтрализован. Даже ёжик!", Mood.Smile);
            // predict before the traitors
            if (autoplay) game.question.ForcedChoice = 0;
            yield return game.question.Ask("Как остановить вирус, который уже размножается внутри клетки?", new[] { "уничтожить заражённую клетку", "пометить её антителами", "поднять температуру ещё выше" }, 0);
            bool right = game.question.LastChoice == 0; if (right) CorrectPredictions++;
            yield return Say(right ? "Верно. Антитела внутрь клетки не достают — нужны Т-киллеры." : "Антитела внутрь клетки не достают. Заражённую клетку надо уничтожить — это работа Т-киллеров.", right ? Mood.Smile : Mood.Neutral);
        }

        // ---------------- phase 3: traitors --------------------------------------------------
        IEnumerator Traitors()
        {
            Beat = 5; Phase = 3;
            var head = game.rig.head.transform;
            Vector3[] slots = { new(-3.2f, -1.2f, 5.5f), new(3.2f, -1f, 6f) };
            for (int i = 0; i < killers.Count; i++) if (killers[i]) { killers[i].transform.position = head.position - head.forward * 6f + Random.insideUnitSphere; killers[i].Init(game.vessel, head, slots[i % slots.Length], game.fx); }
            foreach (var c in cells) if (c) { c.flagsShown = true; c.releaseEvery = 10f; }
            Imm.autoEatDebris = true; Imm.debrisFade = 14f;
            game.sound.Play("SFX_Reveal", 0.9f);
            game.Reveal("tkiller", game.Head + game.rig.head.transform.forward * 2.2f);
            yield return WaitKnowledge();
            game.abilities.SetMode(Bvr2Abilities.Mode.Signal, false);
            yield return Say("Заражённые клетки выставили флажки. Отмечай их Сигналом — Т-киллеры пойдут в атаку. Здоровые не трогай!", Mood.Serious);
            Objective("Отметь заражённые клетки Сигналом");
            Prompt("Правый курок на клетку с флажками — Сигнал", "ЛКМ на клетку с флажками — Сигнал");
            float t = 0, spawnT = 0, autoT = 0; bool autoMistake = false;
            while (AliveInfected > 0) // cells infected during the fight (spots) are left for the last surge
            {
                float dt = Time.unscaledDeltaTime; t += dt; spawnT += dt;
                if (spawnT > 5f && Imm.AliveViruses < 10) { spawnT = 0; SpawnLane(Random.Range(0, 2), Random.value < 0.3f); }
                Hud.SetObjective($"Заражённых клеток {AliveInfected + Imm.SpotCount}  ·  вирусов {Imm.AliveViruses}  ·  {game.organism.temperature:0.0}°");
                Bvr2WallCell next = null; foreach (var c in cells) if (c && c.InfectedAlive && !c.Targeted) { next = c; break; }
                if (next && !autoplay) Hud.SetCompass(Vector3.Angle(game.rig.head.transform.forward, next.transform.position - game.Head) > 35f ? next.transform.position : (Vector3?)null, "флажки");
                if (autoplay || t > 100f)
                {
                    autoT += dt;
                    if (autoT > 0.7f)
                    {
                        autoT = 0;
                        if (autoplay && !autoMistake) { autoMistake = true; foreach (var c in cells) if (c && !c.infected && c.Current == Bvr2WallCell.State.Alive) { Imm.Signal(c.transform.position + c.transform.up * 1.4f, false); break; } }
                        else if (next) Imm.Signal(next.transform.position + next.transform.up * 1.4f, false);
                        else { var sp = Imm.NearestSpot(game.Head, 80f); if (sp.HasValue) Imm.Signal(sp.Value, false); }
                        // a decent player sprays specific antibodies at whatever comes close
                        foreach (var v in Imm.viruses) if (v.gameObject.activeSelf && v.Alive && !v.Tagged && !v.debris && Vector3.Distance(v.transform.position, game.Head) < 30f) v.Neutralize();
                    }
                }
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            game.sound.Play("SFX_Success_Step", 0.9f);
            yield return Say("Очаг почти погас! Но он готовит последний выброс…", Mood.Surprised);
        }

        /// <summary>The player's Signal near a wall cell (or an infected spot) sends a free T-killer there.</summary>
        void Dispatch(Vector3 p)
        {
            Bvr2TKiller free = null; foreach (var k in killers) if (k && k.gameObject.activeSelf && k.Free) { free = k; break; }
            if (!free) { Hud.Toast("Т-киллеры заняты", Bvr2Style.TextDim, 1f); return; }
            Bvr2WallCell best = null; float bd = 4.2f * 4.2f;
            foreach (var c in cells)
            {
                if (!c || c.Current != Bvr2WallCell.State.Alive || c.Targeted) continue;
                float d = (c.transform.position - p).sqrMagnitude; if (d < bd) { bd = d; best = c; }
            }
            // an infected spot (a cell infected during the fight) closer than any wall cell wins
            var spot = Imm.NearestSpot(p, 4f);
            if (spot.HasValue && (!best || (spot.Value - p).sqrMagnitude < bd)) { Vector3 at = spot.Value; free.SendTo(at, () => Imm.KillSpotNear(at, 1.5f)); return; }
            if (best) free.Send(best);
        }

        // ---------------- phase 4: the last surge, synchronised as a T-killer ----------------
        IEnumerator Surge()
        {
            Beat = 6; Phase = 4;
            var o = game.organism;
            game.sound.Play("BVR2_Alarm", 0.8f);
            Hud.Toast("ПОСЛЕДНИЙ ВЫБРОС", Bvr2Style.Danger, 2f);
            for (int i = 0; i < 16; i++) SpawnLane(i % 3, i % 4 == 3);
            yield return Say("Последняя волна! Подключаю тебя к Т-киллеру: уничтожай клетки, которые успеют заразиться. Вирусы в крови берут на себя антитела.", Mood.Surprised);
            game.sync.tKiller = true; game.sync.Begin(autoplay ? 4f : 30f); Step = "синхронизация";
            Hud.Title("СИНХРОНИЗАЦИЯ", "ты — Т-киллер", 2.5f);
            Prompt("Касайся заражённых клеток · курок — удар издалека", "Подплывай к заражённым клеткам · ЛКМ — удар издалека");
            float neutral = 0, autoT = 0;
            while (game.sync.Active)
            {
                float dt = Time.unscaledDeltaTime;
                // plasma cells now make the specific antibodies: virions in the blood are neutralised one after another
                neutral += dt;
                if (neutral > 0.45f) { neutral = 0; foreach (var v in Imm.viruses) if (v.gameObject.activeSelf && v.Alive && !v.Tagged && !v.debris && v.Current == Bvr2Virus.State.Free) { v.Neutralize(); break; } }
                // touch: hands or head close to an infected spot kill it
                foreach (var tr in new[] { game.rig.leftHand, game.rig.rightHand, game.rig.head.transform })
                    if (tr && Imm.KillSpotNear(tr.position, 1.8f)) { SpotsKilledBySync++; Bvr2Haptics.Both(0.6f, 0.1f); }
                // strike from a distance along the right hand
                if (game.input.ToolDown)
                {
                    var hand = game.rig.rightHand; var sp = Imm.NearestSpot(hand.position + hand.forward * 6f, 6.5f);
                    if (sp.HasValue && Vector3.Angle(hand.forward, sp.Value - hand.position) < 14f && Imm.KillSpotNear(sp.Value, 0.5f))
                    {
                        SpotsKilledBySync++;
                        var beam = game.fx.GetBeam(); if (beam != null) { Bvr2Fx.ShapeBeam(beam, hand.position, hand.forward, sp.Value, new Color(0.7f, 0.8f, 1f, 0.9f), 0.04f, 0.02f); StartCoroutine(ReleaseBeam(beam)); }
                        game.sound.Play("BVR2_Strike", 0.8f);
                    }
                }
                if (autoplay) { autoT += dt; if (autoT > 0.3f) { autoT = 0; var sp = Imm.NearestSpot(game.Head, 80f); if (sp.HasValue && Imm.KillSpotNear(sp.Value, 0.5f)) SpotsKilledBySync++; } }
                var near = Imm.NearestSpot(game.Head, 60f);
                Hud.SetCompass(near, "заражённая клетка");
                Hud.SetObjective($"Т-киллер: {Mathf.CeilToInt(game.sync.TimeLeft)} с  ·  уничтожено {SpotsKilledBySync}  ·  вирусов {Imm.AliveViruses}");
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            game.sync.tKiller = false;
        }

        IEnumerator ReleaseBeam(Bvr2Fx.Beam b) { yield return new WaitForSeconds(0.2f); game.fx.ReleaseBeam(b); }

        // ---------------- victory ---------------------------------------------------------------
        IEnumerator Victory()
        {
            Beat = 7; Phase = 5;
            var o = game.organism;
            // the last virions are neutralised in slow motion; allies finish the remaining infected cells
            Bvr2Time.SlowMo(0.25f, autoplay ? 0.3f : 2.2f);
            Imm.replicate = false;
            foreach (var v in Imm.viruses) if (v.gameObject.activeSelf && v.Alive && !v.debris) { if (v.Current == Bvr2Virus.State.Free) v.Neutralize(); else v.Kill(false); }
            int guard = 0; while (Imm.SpotCount > 0 && guard++ < 50) { var sp = Imm.NearestSpot(game.Head, 200f); if (!sp.HasValue || !Imm.KillSpotNear(sp.Value, 0.5f)) break; }
            foreach (var c in cells) if (c && c.InfectedAlive) c.Hit();
            game.sound.Play("BVR2_Victory", 1f);
            game.sound.Mood("recovery");
            Hud.Title("ОЧАГ ПОГАШЕН", "вирусная нагрузка падает", 3f);
            if (ambientBefore < 0) ambientBefore = game.environment.ambientStrength;
            Objective("Очаг погашен");
            float t = 0; float load0 = o.viralLoad;
            while (t < (autoplay ? 0.5f : 4f))
            {
                t += Time.unscaledDeltaTime; float k = Mathf.Clamp01(t / (autoplay ? 0.5f : 4f));
                o.viralLoad = Mathf.Lerp(load0, 0f, k);
                game.environment.ambientStrength = Mathf.Lerp(ambientBefore, ambientBefore * 1.45f, Mathf.Sin(k * Mathf.PI * 0.5f)); // the wall brightens
                yield return null;
            }
            o.viralLoad = 0;
            Imm.ClearSpots();
            foreach (var v in Imm.viruses) if (v.gameObject.activeSelf && v.debris) { game.fx.Burst(v.transform.position, Bvr2Style.Good, 4, 0.6f, 0.04f, 0.4f); v.gameObject.SetActive(false); }
            game.Reveal("memory", game.Head + game.rig.head.transform.forward * 2f);
            yield return WaitKnowledge();
            foreach (var k in killers) if (k) k.gameObject.SetActive(false);
            Imm.DismissPhagocytes();
            game.swimmer.regionMin = game.swimmer.regionMax = 0;
            yield return Say("Температура уже снижается. Иммунитет победил — и запомнил этот вирус.", Mood.Smile);
        }

        IEnumerator Debrief()
        {
            Beat = 8; Phase = 6;
            Objective("Глава 5 пройдена");
            bool precise = HealthyHit == 0 && (key ? key.Mistakes : 0) <= 1;
            bool body = PeakTemp < 39.0f;
            Stars = 1 + (precise ? 1 : 0) + (body ? 1 : 0);
            string[] heads = { "ДВА ОРУЖИЯ ИММУНИТЕТА", "СПЕЦИФИЧНОСТЬ", "ПАМЯТЬ И ПРИВИВКИ" };
            string[] bodies = {
                "антитела — против вирусов в крови\nТ-киллеры — против заражённых клеток\nфагоциты (нейтрофилы, макрофаги)\nубирают остатки",
                "одно антитело — один эпитоп,\nкак ключ к замку\nподобрать нужное — дело нескольких\nдней, поэтому болезнь длится",
                "остаются клетки памяти\nповторная встреча — быстрый ответ\nпрививка учит иммунитет заранее" };
            string why = $"★ глава пройдена   {(precise ? "★" : "☆")} точно: здоровые клетки целы, ключ почти без ошибок   {(body ? "★" : "☆")} температура не выше 39 °C (было {PeakTemp:0.0} °C)";
            board = ShowBoard("ГЛАВА 5 · ОЧАГ — ПРОЙДЕНА", Stars, heads, bodies, why);
            yield return BoardIn(board);
            yield return Say(Stars == 3 ? "Блестяще! Это была настоящая иммунная оборона." : "Победа! Пройди ещё раз, чтобы собрать все звёзды.", Mood.Smile);
            yield return WaitContinue();
            yield return BoardOut(board);
            if (autoplay) game.question.ForcedChoice = 0;
            yield return game.question.Ask("Почему той же болезнью второй раз часто не болеют?", new[] { "остаются клетки памяти", "вирус исчезает навсегда", "кровь становится горячее" }, 0, "ПРОВЕРЬ СЕБЯ");
            yield return Say(game.question.LastChoice == 0 ? "Точно. На этом и основаны прививки." : "Нет: остаются клетки памяти — иммунитет узнаёт вирус сразу. На этом основаны прививки.", Mood.Neutral);
            Finished = true;
            Objective("Глава 5 пройдена");
            if (autoplay) yield break;
            if (board) { Destroy(board.gameObject); board = null; }
            yield return WhatNext("Очаг погашен. Плавай сколько хочешь.");
        }

        // ---------------- debug hooks -------------------------------------------------------
        public void DebugLookAtFork() { float ss = arenaS + 16f; game.rig.LookAt(game.vessel.PointAt(ss, game.vessel.Radius(ss), focusAngle + 180f)); }
        public void DebugLookAtFocus() { if (cells.Count > 4 && cells[4]) game.rig.LookAt(cells[4].transform.position); }
        public void DebugLookAtNearest() { var v = Imm.NearestVirus(game.Head, 80f, false); if (v) game.rig.LookAt(v.transform.position); }
        public void DebugLookAtKey() { game.rig.LookAt(game.Head + game.rig.head.transform.forward); }
        public void DebugKeyAlign() { if (key) key.AutoAlign(); }
        public void DebugSpawn(int n) { for (int i = 0; i < n; i++) SpawnLane(i % 3, i % 3 == 1); }
        public void DebugSignalCell(int i) { if (i >= 0 && i < cells.Count && cells[i]) Imm.Signal(cells[i].transform.position + cells[i].transform.up * 1.4f, false); }
        public void DebugFlags(bool on) { foreach (var c in cells) if (c) c.flagsShown = on; }
    }
}
