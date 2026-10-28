using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// Chapter 3 «Первый контакт» (scenes 08–09 of the script): a virus in the blood → scan it → open the
    /// capsid with both hands in slow motion → scan the spike (antigen) → predict → antibodies (left hand)
    /// tag viruses → macrophages arrive, the Signal (right hand) sends them to the tagged ones →
    /// climax: three waves under an accelerating heartbeat; infected wall cells release new viruses and the
    /// temperature rises (fever) → Synchronisation: the player becomes a macrophage → debrief.
    /// </summary>
    public sealed class Bvr2Chapter3 : Bvr2ChapterBase
    {
        public override int Number => 3;
        public override string Title => "Первый контакт";

        public float startS = 1372f, arenaS = 1452f;
        public Transform virusExhibit, halfA, halfB, rna;
        public Bvr2Scannable antigenSpike;

        public float PeakLoad { get; private set; }
        public int Wave { get; private set; }
        public int SyncEaten { get; private set; }
        public override string StatusLine => base.StatusLine + $" · wave {Wave} · alive {(game.immune ? game.immune.Alive : 0)} · killed {(game.immune ? game.immune.Killed : 0)} · load {game.organism.viralLoad:F2} · t {game.organism.temperature:F1}";

        Bvr2Immune Imm => game.immune;
        Transform board; Vector3 halfARest, halfBRest; bool restSaved;

        void Start() { HideProps(); }

        void HideProps() { if (virusExhibit) virusExhibit.gameObject.SetActive(false); }

        protected override void ResetProps()
        {
            ResetShared();
            HideProps();
            if (board) Destroy(board.gameObject);
            if (game.abilities) { game.abilities.signalUnlocked = false; game.abilities.SetMode(Bvr2Abilities.Mode.Attract, false); }
            game.bioTool.locked = false;
            PeakLoad = 0; Wave = 0; SyncEaten = 0;
        }

        void Update()
        {
            if (!Running) return;
            PeakLoad = Mathf.Max(PeakLoad, game.organism.viralLoad);
            if (Time.frameCount % 15 == 0) Hud.SetCounters($"ЗНАНИЯ {game.codex.FoundIn(3)}/{game.codex.CountIn(3)}", $"УНИЧТОЖЕНО {Imm.Killed}");
        }

        protected override IEnumerator Run()
        {
            ResetProps();
            var o = game.organism; o.ResetState();
            Hud.showTemperature = true;
            game.scanner.locked = false; game.scanner.allowPlasma = false;
            game.abilities.impulseUnlocked = true;
            Imm.replicate = false;
            game.sound.Mood("calm");
            if (startBeat <= 0) yield return Intro();
            else
            {
                float[] at = { startS, startS + 20f, startS + 30f, startS + 40f, startS + 50f, arenaS - 4f, arenaS, arenaS };
                Arrive(at[Mathf.Clamp(startBeat, 0, at.Length - 1)]); Prof.Appear(true); o.viralLoad = 0.12f;
                if (startBeat >= 2) game.codex.Discover("virus");
                if (startBeat >= 3) game.codex.Discover("antigen");
                if (startBeat >= 4) { game.codex.Discover("antibody"); game.antibodies.unlocked = true; }
                if (startBeat >= 5) { game.codex.Discover("phagocytosis"); game.abilities.signalUnlocked = true; game.abilities.SetMode(Bvr2Abilities.Mode.Signal, false); }
                if (startBeat == 5 || startBeat == 6) Imm.Deploy(3, game.rig.head.transform);
            }
            if (startBeat <= 1) yield return Discover();
            if (startBeat <= 2) yield return OpenVirus();
            if (startBeat <= 3) yield return Antibodies();
            if (startBeat <= 4) yield return SignalBeat();
            if (startBeat <= 5) yield return Waves();
            if (startBeat <= 6) yield return Synchronise();
            yield return Debrief();
        }

        IEnumerator Intro()
        {
            Beat = 0;
            Arrive(startS, 1.2f);
            game.sound.Play("SFX_Transition", 0.9f);
            Hud.Title("ГЛАВА 3 · ПЕРВЫЙ КОНТАКТ", "в крови — чужие частицы", 4f);
            Prof.Appear(true);
            game.organism.viralLoad = 0.12f;
            yield return Wait(2.5f);
            Objective("Посмотри на температуру на запястье");
            yield return Say("Тревога иммунитета: в крови что-то чужое. Температура уже поднимается — видишь на запястье?", Mood.Serious);
            yield return Say("Найди нарушителя и отсканируй.", Mood.Neutral);
        }

        IEnumerator Discover()
        {
            Beat = 1;
            var v = Imm.Spawn(game.PlayerS + 14f, Random.Range(0f, 360f), 0.3f, 0.12f, 60f);
            Objective("Найди чужую частицу и отсканируй");
            Prompt("Левый курок — сканировать", "ПКМ / F — сканировать");
            float t = 0;
            while (!game.codex.Has("virus"))
            {
                t += Time.unscaledDeltaTime;
                if (v && t > 6f) Hud.SetCompass(v.transform.position, "чужак");
                if (autoplay && v) game.scanner.ForceScan("virus", v.transform.position);
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            yield return WaitKnowledge();
            if (v) v.gameObject.SetActive(false);
        }

        IEnumerator OpenVirus()
        {
            Beat = 2;
            // a big model of the virus in front of the player, the world slowed down
            var head = game.rig.head.transform;
            Vector3 f = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized; if (f.sqrMagnitude < 0.01f) f = head.forward;
            virusExhibit.SetParent(game.rig.transform, true);
            virusExhibit.position = head.position + f * 1.15f - Vector3.up * 0.08f; virusExhibit.rotation = Quaternion.LookRotation(f, Vector3.up);
            if (!restSaved) { halfARest = halfA.localPosition; halfBRest = halfB.localPosition; restSaved = true; }
            halfA.localPosition = halfARest; halfB.localPosition = halfBRest; rna.localScale = Vector3.one * 0.01f;
            virusExhibit.gameObject.SetActive(true);
            game.fx.Shock(virusExhibit.position, -f, new Color(0.65f, 1f, 0.35f), 0.1f, 1f, 0.5f);
            Bvr2Time.SlowMo(0.15f, 600f);
            Bvr2Environment.Current?.SetFocus(true); game.blood.SetFocus(virusExhibit.position, 0.9f);
            Objective("Раскрой оболочку вируса");
            Prompt("Зажми оба хвата и разведи руки", "Держи X (или крути колесо вверх) — раскрыть");
            yield return Say("Я поймал его в стоп-кадр. Раскрой оболочку — посмотрим, что внутри.", Mood.Neutral);
            float open = 0, d0 = -1;
            while (open < 1f)
            {
                if (XR)
                {
                    bool both = game.input.GripL && game.input.GripR;
                    float d = Vector3.Distance(game.rig.leftHand.position, game.rig.rightHand.position);
                    if (both) { if (d0 < 0) d0 = d; open = Mathf.Max(open, Mathf.Clamp01((d - d0) / 0.25f)); } else d0 = -1;
                }
                else if (game.input.FieldDelta > 0) open = Mathf.Min(1f, open + game.input.FieldDelta * 2.5f);
                if (autoplay) open += Time.unscaledDeltaTime * 4f;
                ApplyOpen(open);
                yield return null;
            }
            ApplyOpen(1f);
            game.sound.Play("SFX_Reveal", 0.9f); Bvr2Haptics.Both(0.6f, 0.15f);
            game.fx.Burst(virusExhibit.position, new Color(1f, 0.6f, 0.9f), 30, 1.2f, 0.03f, 0.8f);
            NoPrompt();
            yield return Say("Внутри — его генетическая программа. Попав в клетку, вирус заставит её делать новые вирусы.", Mood.Serious);
            Objective("Отсканируй шип на оболочке");
            Prompt("Левый курок — сканировать шип", "ПКМ / F — сканировать шип");
            yield return Say("А теперь отсканируй шип на оболочке — это его «лицо».", Mood.Neutral);
            while (!game.codex.Has("antigen"))
            {
                if (autoplay) game.scanner.ForceScan("antigen", antigenSpike.transform.position);
                yield return null;
            }
            NoPrompt();
            yield return WaitKnowledge();
            virusExhibit.gameObject.SetActive(false);
            Bvr2Time.Release(); Bvr2Environment.Current?.SetFocus(false); game.blood.ClearFocus();
            // predict
            if (autoplay) game.question.ForcedChoice = 0;
            yield return game.question.Ask("Как иммунитет узнаёт вирус?", new[] { "по шипам-антигенам на оболочке", "по цвету", "по скорости движения" }, 0);
            bool right = game.question.LastChoice == 0; if (right) CorrectPredictions++;
            yield return Say(right ? "Верно! К каждому антигену иммунитет подбирает свой ключ — антитело." : "Нет — по шипам-антигенам. К каждому из них иммунитет подбирает свой ключ — антитело.", right ? Mood.Smile : Mood.Neutral);
        }

        void ApplyOpen(float k)
        {
            float e = Mathf.SmoothStep(0, 1, Mathf.Clamp01(k));
            halfA.localPosition = halfARest + Vector3.up * 0.16f * e; halfB.localPosition = halfBRest - Vector3.up * 0.16f * e;
            halfA.localRotation = Quaternion.Euler(-12f * e, 0, 0); halfB.localRotation = Quaternion.Euler(12f * e, 0, 0);
            rna.localScale = Vector3.one * Mathf.Lerp(0.01f, 1f, e);
            rna.Rotate(Vector3.up, 40f * Time.unscaledDeltaTime, Space.Self);
        }

        IEnumerator Antibodies()
        {
            Beat = 3;
            game.antibodies.unlocked = true;
            Hud.Toast("+ СПОСОБНОСТЬ · АНТИТЕЛА", Bvr2Style.Ally, 2.8f);
            game.sound.Play("SFX_Reveal", 0.9f);
            yield return Say("Сканер запомнил антиген и теперь делает антитела. Держи левый курок на вирусе — и пометь его.", Mood.Smile);
            var list = new List<Bvr2Virus>();
            for (int i = 0; i < 3; i++) { var v = Imm.Spawn(game.PlayerS + 10f + i * 4f, i * 120f + 30f, 0.35f, 0.04f, 60f); if (v) list.Add(v); }
            Prompt("Левый курок на вирус — антитела", "ПКМ / F на вирус — антитела");
            int tagged = 0; Step = "антитела";
            while (tagged < list.Count)
            {
                tagged = 0; foreach (var v in list) if (v && v.Tagged) tagged++;
                Hud.SetObjective($"Пометь антителами вирусы: {tagged}/{list.Count}");
                Bvr2Virus next = null; foreach (var v in list) if (v && v.gameObject.activeSelf && !v.Tagged) { next = v; break; }
                Hud.SetCompass(next ? next.transform.position : (Vector3?)null, "вирус");
                if (autoplay && next) next.AddTag(1f);
                if (tagged >= 1 && !game.codex.Has("antibody")) game.Reveal("antibody", game.Head + game.rig.head.transform.forward * 2f);
                // a practice virus that got away (eaten, drifted off) is replaced by a fresh one ahead
                for (int i = 0; i < list.Count; i++)
                    if (list[i] && !list[i].Tagged && (!list[i].gameObject.activeSelf || !list[i].Alive || Vector3.Distance(list[i].transform.position, game.Head) > 45f))
                        list[i] = Imm.Spawn(game.PlayerS + 10f, Random.Range(0f, 360f), 0.3f, 0.04f, 60f);
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            yield return WaitKnowledge();
        }

        IEnumerator SignalBeat()
        {
            Beat = 4;
            Imm.Deploy(3, game.rig.head.transform);
            game.sound.Play("SFX_Reveal", 0.8f);
            yield return Say("Подоспели макрофаги — клетки-пожиратели. Укажи им цель — это Сигнал.", Mood.Smile);
            game.abilities.signalUnlocked = true; game.abilities.SetMode(Bvr2Abilities.Mode.Signal);
            Hud.Toast("+ СПОСОБНОСТЬ · СИГНАЛ", new Color(0.7f, 1f, 0.45f), 2.6f);
            Prompt("Правый курок на вирус — Сигнал · в такт сердцу — сильнее", "ЛКМ на вирус — Сигнал · в такт сердцу — сильнее");
            if (Imm.Alive < 3) for (int i = Imm.Alive; i < 3; i++) { var v = Imm.Spawn(game.PlayerS + 9f + i * 3f, i * 120f, 0.3f, 0.04f, 60f); if (v) v.AddTag(1f); }
            Step = "сигнал"; int start = Imm.Killed; float t = 0, auto = 0;
            while (Imm.Killed - start < 3 && Imm.Alive > 0)
            {
                t += Time.unscaledDeltaTime;
                Hud.SetObjective($"Наведи макрофагов на помеченные вирусы: {Imm.Killed - start}/3");
                var target = Imm.NearestVirus(game.Head, 60f, false);
                Hud.SetCompass(target ? target.transform.position : (Vector3?)null, "вирус");
                if ((autoplay || t > 50f) && target) { auto += Time.unscaledDeltaTime; if (auto > 0.8f) { auto = 0; Imm.Signal(target.transform.position, false); } }
                if (Imm.KilledTagged > 0 && !game.codex.Has("phagocytosis")) game.Reveal("phagocytosis", game.Head + game.rig.head.transform.forward * 2f);
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            if (!game.codex.Has("phagocytosis")) game.Reveal("phagocytosis", game.Head + game.rig.head.transform.forward * 2f);
            yield return WaitKnowledge();
            yield return Say("Помеченный вирус съедается в разы быстрее — это опсонизация. А непомеченный может и вырваться.", Mood.Neutral);
        }

        IEnumerator Waves()
        {
            Beat = 5;
            var o = game.organism; var sw = game.swimmer;
            if (Mathf.Abs(game.PlayerS - arenaS) > 30f) Arrive(arenaS - 4f, 0.8f);
            float centre = game.PlayerS + 4f;
            sw.regionMin = centre - 20f; sw.regionMax = centre + 24f;
            Imm.replicate = true;
            game.sound.Mood("tension");
            yield return Say("Это была разведка. Сейчас пойдёт волна! Левая рука — метить, правая — направлять макрофагов. Не дай им сесть на стенку!", Mood.Surprised);
            int[] counts = { 7, 10, 14 }; float[] seek = { 0.7f, 0.9f, 1.1f }, infect = { 8f, 7f, 6f }, stress = { 8f, 22f, 36f };
            for (int w = 0; w < counts.Length; w++)
            {
                Wave = w + 1; Step = $"волна {Wave}";
                o.stressDrive = stress[w];
                Hud.Toast($"ВОЛНА {Wave}", Bvr2Style.Danger, 1.8f); game.sound.Play("BVR2_Alarm", 0.7f);
                float t = 0, spawned = 0, auto = 0;
                while (true)
                {
                    float dt = Time.unscaledDeltaTime; t += dt;
                    // viruses arrive over a few seconds from around the arena
                    while (spawned < counts[w] && t > spawned * 0.55f)
                    {
                        Imm.Spawn(centre + Random.Range(-14f, 18f), Random.Range(0f, 360f), Random.Range(0.15f, 0.55f), seek[w], infect[w]);
                        spawned++;
                    }
                    int alive = Imm.Alive, pending = Imm.PendingReleases;
                    Hud.SetObjective($"Волна {Wave}/3  ·  вирусов {alive + pending}  ·  заражение {Mathf.RoundToInt(o.viralLoad * 100)} %  ·  {o.temperature:0.0}°");
                    var near = Imm.NearestVirus(game.Head, 40f, false);
                    Hud.SetCompass(near && Vector3.Angle(game.rig.head.transform.forward, near.transform.position - game.Head) > 40f ? near.transform.position : (Vector3?)null, "вирус");
                    if (o.temperature >= 37.6f && !game.codex.Has("fever")) game.Reveal("fever", game.Head + game.rig.head.transform.forward * 2f);
                    if (autoplay)
                    {
                        auto += dt;
                        if (auto > 0.25f)
                        {
                            auto = 0;
                            foreach (var v in Imm.viruses) if (v.gameObject.activeSelf && v.Alive && !v.Tagged) { v.AddTag(1f); Imm.Signal(v.transform.position, Imm.OnTheBeat); break; }
                        }
                        if (w == 1 && !game.codex.Has("fever")) game.Reveal("fever", game.Head + game.rig.head.transform.forward * 2f);
                    }
                    if (spawned >= counts[w] && alive == 0 && pending == 0 && !game.KnowledgeBusy) break;
                    if (t > 95f) { Imm.ClearSpots(); foreach (var v in Imm.viruses) if (v.gameObject.activeSelf && v.Alive) v.Kill(false); }
                    yield return null;
                }
                Hud.SetCompass(null, null);
                game.sound.Play("SFX_Success_Step", 0.9f);
                if (w < counts.Length - 1)
                {
                    Imm.ClearSpots();
                    if (w == 0) Hud.Toast($"ПУЛЬС {Mathf.RoundToInt(o.heartRate)} УД/МИН", Bvr2Style.Danger, 3f);
                    yield return Say(w == 0 ? "Волна отбита! Сердце бьётся всё чаще — организм мобилизуется. Следующая будет сильнее." : "Держись! Последняя волна — самая большая.", Mood.Serious);
                    yield return Wait(1.5f);
                }
            }
            Imm.ClearSpots();
        }

        IEnumerator Synchronise()
        {
            Beat = 6;
            var sync = game.sync;
            game.Reveal("macrophage", game.Head + game.rig.head.transform.forward * 2f);
            yield return WaitKnowledge();
            yield return Say("Остатки прячутся у стенок. Подключаю тебя к макрофагу — ненадолго!", Mood.Surprised);
            Imm.replicate = false;
            float centre = game.PlayerS;
            for (int i = 0; i < 12; i++) Imm.Spawn(centre + Random.Range(-10f, 16f), Random.Range(0f, 360f), Random.Range(0.2f, 0.6f), 0.35f, 30f);
            sync.Begin(autoplay ? 3f : 25f); Step = "синхронизация";
            Hud.Title("СИНХРОНИЗАЦИЯ", "ты — макрофаг", 2.5f);
            Prompt("Плыви к вирусам и хватай их руками", "Подплывай к вирусам — они поглощаются");
            int autoN = 0;
            while (sync.Active)
            {
                Hud.SetObjective($"Синхронизация: {Mathf.CeilToInt(sync.TimeLeft)} с  ·  поглощено {sync.Eaten}");
                var near = Imm.NearestVirus(game.Head, 40f, false);
                Hud.SetCompass(near ? near.transform.position : (Vector3?)null, "вирус");
                if (autoplay && near && autoN++ % 10 == 0) near.Absorb(game.rig.rightHand);
                yield return null;
            }
            SyncEaten = sync.Eaten;
            Hud.SetCompass(null, null); NoPrompt();
            Imm.DismissPhagocytes();
            foreach (var v in Imm.viruses) if (v.gameObject.activeSelf && v.Alive) v.Kill(false);
            game.sound.Mood("recovery"); game.organism.stressDrive = 0;
            game.swimmer.regionMin = game.swimmer.regionMax = 0;
            Hud.Toast($"ПОГЛОЩЕНО ВИРУСОВ: {SyncEaten}", Bvr2Style.Good, 3f);
            yield return Say("Синхронизация завершена — отличная охота! Температура скоро спадёт.", Mood.Smile);
        }

        IEnumerator Debrief()
        {
            Beat = 7;
            Objective("Глава 3 пройдена");
            bool loadStar = PeakLoad < 0.45f;
            bool skillStar = SyncEaten >= 8 || Imm.BestCombo >= 5;
            Stars = 1 + (loadStar ? 1 : 0) + (skillStar ? 1 : 0);
            string[] heads = { "ВИРУС", "АНТИТЕЛА И ФАГОЦИТОЗ", "ЛИХОРАДКА" };
            string[] bodies = {
                "белковая оболочка + генетическая программа\nшипы на оболочке — антигены\nразмножается только внутри клеток",
                "антитело подходит к антигену,\nкак ключ к замку, и метит вирус\nмакрофаг поглощает и переваривает\nпомеченного — быстрее",
                "иммунитет поднимает температуру\nвирусам хуже, иммунитету легче\nвыше 38,5 °C — к врачу" };
            string why = $"★ глава пройдена   {(loadStar ? "★" : "☆")} заражение не выше 45 % (было {Mathf.RoundToInt(PeakLoad * 100)} %)   {(skillStar ? "★" : "☆")} синхронизация ≥ 8 или комбо ≥ 5";
            board = ShowBoard("ГЛАВА 3 · ПЕРВЫЙ КОНТАКТ — ПРОЙДЕНА", Stars, heads, bodies, why);
            yield return BoardIn(board);
            yield return Say(Stars == 3 ? "Великолепно! Иммунитет гордился бы тобой." : "Хорошо отбились! Пройди ещё раз, чтобы собрать все звёзды.", Mood.Smile);
            yield return WaitContinue();
            yield return BoardOut(board);
            if (autoplay) game.question.ForcedChoice = 0;
            yield return game.question.Ask("Зачем нужны антитела?", new[] { "метят вирус, чтобы его быстрее нашли и съели", "сами переваривают вирус", "переносят кислород" }, 0, "ПРОВЕРЬ СЕБЯ");
            yield return Say(game.question.LastChoice == 0 ? "Точно. Антитела — метки и «наручники», а едят фагоциты." : "Нет: антитела метят и склеивают вирусы, а переваривают их фагоциты.", Mood.Neutral);
            Finished = true;
            Objective("Глава 3 пройдена");
            if (autoplay) yield break;
            if (board) { Destroy(board.gameObject); board = null; }
            yield return WhatNext("Плавай сколько хочешь. Сигнал и антитела работают.");
        }

        // ---------------- debug hooks -------------------------------------------------------
        public void DebugOpen() { ApplyOpen(1f); }
        public void DebugTagAll() { foreach (var v in Imm.viruses) if (v.gameObject.activeSelf && v.Alive) v.AddTag(1f); }
        public void DebugSignalNearest() { var v = Imm.NearestVirus(game.Head, 60f, false); if (v) Imm.Signal(v.transform.position, Imm.OnTheBeat); }
        public void DebugLookAtNearest() { var v = Imm.NearestVirus(game.Head, 80f, false); if (v) game.rig.LookAt(v.transform.position); }
        public void DebugSpawn(int n) { for (int i = 0; i < n; i++) Imm.Spawn(game.PlayerS + Random.Range(4f, 14f), Random.Range(0f, 360f), Random.Range(0.1f, 0.4f), 0.4f, 20f); }
    }
}
