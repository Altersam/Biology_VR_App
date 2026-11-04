using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.V2
{
    /// <summary>
    /// Title screen, main menu and pause menu (04.11).
    /// The game opens on a holographic stage away from the lab and the artery (built by Bvr2HoloFx.MenuStage):
    /// the title page («нажмите курок, чтобы начать»), then the main menu — continue, new game, chapters,
    /// settings, about, exit. In a chapter the Menu button / Y (Tab or Esc on a PC) opens the pause menu in
    /// front of the player: the world stops (Bvr2Pause), the menu offers continue, restart the chapter,
    /// settings, chapters and the main menu. Pointing: the hand ray in VR (the hand whose trigger was pressed
    /// last, with a laser), the screen centre on a PC; the stick / arrows and Enter work too; B / Esc — back.
    /// Progress and settings are kept by <see cref="Bvr2Save"/>.
    /// Was Bvr2Menu.cs until 04.11: Yandex Disk kept turning every new write of that path into conflict copies
    /// («Bvr2Menu (2).cs» …) — duplicate classes broke compilation, so the class and file were renamed.
    /// </summary>
    public sealed class Bvr2MainMenu : MonoBehaviour
    {
        public Bvr2Game game;
        [Header("Stage (Bvr2HoloFx.MenuStage)")]
        public Transform stage; public Transform stand; public Transform panelSpot;
        public Transform[] spinners; public float[] spinSpeeds;
        public Material laserMaterial;
        public string version = "0.13.0";

        public enum Page { None, Title, Main, Chapters, Settings, About, ConfirmExit, ConfirmReset, Pause }
        public Page Current { get; private set; } = Page.None;
        public bool IsOpen => Current != Page.None;
        public bool Paused => pauseMode;
        public string StatusLine => $"page {Current} · focus {focus} ({(focus >= 0 && focus < buttons.Count ? buttons[focus].label.text : "-")}) · buttons {buttons.Count} · paused {pauseMode} · captured {(game && game.input ? game.input.Captured : false)}";

        sealed class Btn { public RectTransform rt; public Image back, edge; public Text label, sub; public Action click; public bool enabled = true; public float hover; public Color tint = Bvr2Style.Ally; }

        readonly List<Btn> buttons = new List<Btn>();
        RectTransform canvas; CanvasGroup group; Image reticle; LineRenderer laser; Text blink;
        Transform pauseRoot; bool pauseMode, onStage;
        int focus = -1, lastRayHit = -1; float openedAt, pageAt, closedAt; bool focusByNav, releasePending;
        readonly List<Text> settingValues = new List<Text>();

        static readonly string[] ChapterInfo =
        {
            "Профессор, приборы и уменьшение до размера клетки",
            "Из чего состоит кровь и как она течёт",
            "Сужение артерии, давление и тромб",
            "Вирусы и первая линия иммунной защиты",
            "Рана сосуда и остановка кровотечения",
            "Заражённые клетки, Т-киллеры, финальная битва",
            "Организм в равновесии: итог путешествия",
        };

        static readonly Color ButtonIdle = new Color(0.035f, 0.11f, 0.15f, 0.94f), ButtonHover = new Color(0.08f, 0.3f, 0.37f, 0.98f), ButtonOff = new Color(0.03f, 0.06f, 0.08f, 0.7f);

        // ======================================================================================= open / close
        /// <summary>Game start: the title page on the menu stage.</summary>
        public void OpenTitle() { EnterStage(); Show(Page.Title); }

        /// <summary>The main menu on the stage (from the pause menu or the end of a chapter).</summary>
        public void OpenMain()
        {
            if (pauseMode) EndPause();
            EnterStage(); Show(Page.Main);
        }

        /// <summary>A chapter starts: leave the stage, give the controls back to the game.</summary>
        public void CloseForGame()
        {
            if (pauseMode) EndPause();
            ClearPage(); Current = Page.None;
            if (onStage)
            {
                onStage = false;
                if (stage) stage.gameObject.SetActive(false);
                if (game.swimmer) game.swimmer.frozen = false;
                if (game.blood) game.blood.enabled = true;
                ShowTools(true);
            }
            if (game.hud) game.hud.Hidden = false;
            if (game.input && game.input.Captured) { releasePending = true; closedAt = Time.unscaledTime; }
            if (laser) laser.enabled = false;
        }

        void Start() { Bvr2Save.Apply(game); }

        void EnterStage()
        {
            Bvr2Save.Apply(game);
            releasePending = false; if (game.input) game.input.Captured = true;
            if (game.hud) { game.hud.Hidden = true; game.hud.ClearSubtitle(); game.hud.SetObjective(""); game.hud.SetPrompt(""); }
            if (game.professor) { game.professor.Interrupt(); game.professor.LeaveStage(); }
            if (game.question) game.question.Cancel();
            ShowTools(false);
            if (stage && stand)
            {
                stage.gameObject.SetActive(true);
                game.swimmer.roomMode = false; game.swimmer.frozen = true; game.swimmer.roomRects = null;
                game.swimmer.Teleport(stand.position, stand.forward);
            }
            if (game.blood) game.blood.enabled = false;
            if (game.sound) game.sound.Mood("calm");
            game.rig.SetOverlay(Color.clear, 0f);
            if (!onStage) { game.rig.Fade(Color.black, 1f, 50f); game.rig.FadeTo(Color.black, 0f, 1.1f); }
            onStage = true;
        }

        void ShowTools(bool on)
        {
            if (game.scanner) game.scanner.gameObject.SetActive(on);
            if (game.bioTool) game.bioTool.gameObject.SetActive(on);
        }

        void OpenPause()
        {
            pauseMode = true; Bvr2Pause.Set(true);
            releasePending = false; game.input.Captured = true; if (game.hud) game.hud.Hidden = true;
            var head = game.rig.head.transform;
            Vector3 f = Vector3.ProjectOnPlane(head.forward, Vector3.up); if (f.sqrMagnitude < 0.01f) f = head.forward; f.Normalize();
            pauseRoot = new GameObject("Pause Menu").transform; pauseRoot.SetParent(game.rig.transform, true);
            pauseRoot.position = head.position + f * 1.45f + Vector3.up * -0.08f; pauseRoot.rotation = Quaternion.LookRotation(f, Vector3.up);
            game.sound?.Play("UI_Panel", 0.8f);
            Show(Page.Pause);
        }

        void EndPause()
        {
            pauseMode = false; Bvr2Pause.Set(false);
            ClearPage(); Current = Page.None;
            if (pauseRoot) Destroy(pauseRoot.gameObject); pauseRoot = null;
        }

        void Resume()
        {
            EndPause();
            releasePending = true; closedAt = Time.unscaledTime; if (game.hud) game.hud.Hidden = false;
            if (laser) laser.enabled = false;
            game.sound?.Play("UI_Panel", 0.6f, 0.9f);
        }

        void StartChapter(int number)
        {
            game.sound?.Play("SFX_Success_Step", 0.7f);
            Bvr2Haptics.Pulse(game.input.UiHandLeft, 0.5f, 0.08f);
            if (pauseMode) EndPause();
            game.campaign.StartChapter(number);
        }

        // ======================================================================================= pages
        void Show(Page page)
        {
            ClearPage(); Current = page; pageAt = Time.unscaledTime;
            if (page == Page.Title) openedAt = Time.unscaledTime;
            Transform parent = pauseMode ? pauseRoot : panelSpot ? panelSpot : transform;
            float scale = pauseMode ? 0.00092f : 0.00105f;
            canvas = Bvr2Ui.Canvas("Menu Canvas", parent, new Vector2(1500, 950), scale, 95);
            group = canvas.gameObject.AddComponent<CanvasGroup>(); group.alpha = 0;
            Bvr2Ui.Backdrop(canvas);
            Bvr2Ui.Panel(canvas, page == Page.Title ? new Color(0.01f, 0.03f, 0.05f, 0.7f) : new Color(0.01f, 0.035f, 0.055f, 0.93f), "Back");
            Frame(canvas);
            switch (page)
            {
                case Page.Title: BuildTitle(); break;
                case Page.Main: BuildMain(); break;
                case Page.Chapters: BuildChapters(); break;
                case Page.Settings: BuildSettings(); break;
                case Page.About: BuildAbout(); break;
                case Page.ConfirmExit: BuildConfirm("Выйти из игры?", "Прогресс глав и настройки сохранены.", "Выйти", Quit, "Остаться", () => Show(Page.Main)); break;
                case Page.ConfirmReset: BuildConfirm("Сбросить прогресс?", "Пройденные главы и звёзды будут стёрты. Настройки останутся.", "Сбросить", () => { Bvr2Save.ResetProgress(); game.sound?.Play("SFX_Release", 0.7f); Show(Page.Settings); }, "Отмена", () => Show(Page.Settings)); break;
                case Page.Pause: BuildPause(); break;
            }
            reticle = Bvr2Ui.Panel(canvas, new Color(1f, 1f, 1f, 0.9f), "Reticle", Bvr2Ui.Dot);
            Bvr2Ui.Place(reticle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(22, 22), Vector2.zero);
            reticle.enabled = false;
            focus = -1; lastRayHit = -1;
            for (int i = 0; i < buttons.Count; i++) if (buttons[i].enabled) { focus = i; break; }
        }

        void ClearPage()
        {
            buttons.Clear(); settingValues.Clear(); blink = null;
            if (canvas) Destroy(canvas.gameObject); canvas = null;
        }

        void BuildTitle()
        {
            var t = Txt(canvas, "BIOLOGY VR", 150, Bvr2Style.Text, new Vector2(0, 200), new Vector2(1400, 190), TextAnchor.MiddleCenter, FontStyle.Bold);
            Txt(canvas, "Путешествие внутри артерии", 58, Bvr2Style.AllySoft, new Vector2(0, 70), new Vector2(1300, 80));
            Line(canvas, new Vector2(0, 10), 620);
            Txt(canvas, "Учебная VR-игра по биологии · 8–9 класс\nкровь, сосуды, иммунитет, гомеостаз", 34, Bvr2Style.TextDim, new Vector2(0, -60), new Vector2(1300, 100));
            blink = Txt(canvas, Xr ? "Нажмите курок, чтобы начать" : "Нажмите ЛКМ или Enter, чтобы начать", 48, Bvr2Style.Knowledge, new Vector2(0, -240), new Vector2(1300, 70), TextAnchor.MiddleCenter, FontStyle.Bold);
            Txt(canvas, "версия " + version, 26, Bvr2Style.TextDim, new Vector2(560, -435), new Vector2(340, 40), TextAnchor.MiddleRight);
            Txt(canvas, Bvr2Save.HasProgress ? "С возвращением! Ваш прогресс сохранён." : "Профессор ждёт вас в лаборатории.", 28, Bvr2Style.TextDim, new Vector2(-380, -435), new Vector2(700, 40), TextAnchor.MiddleLeft);
        }

        void BuildMain()
        {
            Header("BIOLOGY VR", "главное меню");
            float x = -330, y = 228, step = 116; var size = new Vector2(640, 102);
            int cont = Bvr2Save.ContinueChapter;
            var b = Button(canvas, "Продолжить", Bvr2Save.HasProgress ? ChapterName(cont) : "сохранённого прогресса пока нет", new Vector2(x, y), size, () => StartChapter(cont));
            b.enabled = Bvr2Save.HasProgress;
            Button(canvas, "Новая игра", "пролог в лаборатории профессора", new Vector2(x, y -= step), size, () => StartChapter(0));
            Button(canvas, "Главы", "любая глава отдельно — для урока", new Vector2(x, y -= step), size, () => Show(Page.Chapters));
            Button(canvas, "Настройки", "звук, субтитры, поворот, комфорт", new Vector2(x, y -= step), size, () => Show(Page.Settings));
            Button(canvas, "Об игре", "что это и как управлять", new Vector2(x, y -= step), size, () => Show(Page.About));
            var q = Button(canvas, "Выход", "", new Vector2(x, y -= step), size, () => Show(Page.ConfirmExit)); q.tint = Bvr2Style.Danger;
            // progress card
            var card = Bvr2Ui.Rect(canvas, "Progress", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)); Bvr2Ui.Place(card, new Vector2(0.5f, 0.5f), new Vector2(600, 640), new Vector2(380, -50));
            Bvr2Ui.Panel(card, new Color(0.03f, 0.09f, 0.12f, 0.95f), "Back");
            Txt(card, "ПРОГРЕСС", 34, Bvr2Style.Knowledge, new Vector2(0, 280), new Vector2(540, 50), TextAnchor.MiddleLeft, FontStyle.Bold);
            var lines = new System.Text.StringBuilder();
            foreach (var c in Chapters())
            {
                bool done = Bvr2Save.Done(c.Number);
                string mark = done ? $"<color=#{Bvr2Ui.Hex(Bvr2Style.Good)}>✓</color>" : "<color=#5f7884>○</color>";
                string stars = c.Number == 0 ? "" : "  <color=#" + Bvr2Ui.Hex(Bvr2Style.Knowledge) + ">" + new string('★', Bvr2Save.Stars(c.Number)) + "</color><color=#3c5560>" + new string('☆', 3 - Bvr2Save.Stars(c.Number)) + "</color>";
                lines.Append(mark).Append("  ").Append(c.Number == 0 ? "Пролог" : "Глава " + c.Number).Append(" «").Append(c.Title).Append("»").Append(stars).Append('\n');
            }
            Txt(card, lines.ToString().TrimEnd(), 27, Bvr2Style.Text, new Vector2(0, 10), new Vector2(556, 470), TextAnchor.UpperLeft).lineSpacing = 1.35f;
            Txt(card, $"Пройдено: {Bvr2Save.DoneCount} из {Chapters().Count}   ·   звёзды: {Bvr2Save.StarCount} из {3 * Bvr2Save.MaxChapter}", 28, Bvr2Style.TextDim, new Vector2(0, -280), new Vector2(540, 50), TextAnchor.MiddleLeft);
            Hint();
        }

        void BuildChapters()
        {
            Header("ГЛАВЫ", "каждую главу можно пройти отдельно — например, за один урок");
            var list = Chapters();
            for (int i = 0; i < list.Count; i++)
            {
                var c = list[i]; int row = i < 4 ? 0 : 1, col = row == 0 ? i : i - 4, perRow = row == 0 ? 4 : list.Count - 4;
                float w = 330, gap = 22, x0 = -(perRow * w + (perRow - 1) * gap) / 2f + w / 2f;
                var pos = new Vector2(x0 + col * (w + gap), row == 0 ? 120 : -205);
                int number = c.Number;
                var b = Button(canvas, "«" + c.Title + "»", "", pos, new Vector2(w, 300), () => StartChapter(number), 40);
                b.label.alignment = TextAnchor.MiddleCenter; b.label.rectTransform.offsetMin = new Vector2(16, 152); b.label.rectTransform.offsetMax = new Vector2(-16, -64);
                Bvr2Ui.Fit(b.label, 28, 40);
                Txt(b.rt, number == 0 ? "ПРОЛОГ" : "ГЛАВА " + number, 30, Bvr2Style.Ally, new Vector2(0, 112), new Vector2(w - 40, 44), TextAnchor.MiddleCenter, FontStyle.Bold);
                Bvr2Ui.Fit(Txt(b.rt, number < ChapterInfo.Length ? ChapterInfo[number] : "", 25, Bvr2Style.TextDim, new Vector2(0, -40), new Vector2(w - 40, 84)), 18, 25);
                bool done = Bvr2Save.Done(number);
                string status = number == 0 ? (done ? "✓ пройден" : "не пройден") : done ? new string('★', Bvr2Save.Stars(number)) + new string('☆', 3 - Bvr2Save.Stars(number)) : "не пройдена";
                Txt(b.rt, status, done && number > 0 ? 40 : 26, done ? (number == 0 ? Bvr2Style.Good : Bvr2Style.Knowledge) : Bvr2Style.TextDim, new Vector2(0, -122), new Vector2(w - 40, 44));
            }
            Back(new Vector2(-560, -410));
            Hint();
        }

        void BuildSettings()
        {
            Header("НАСТРОЙКИ", "сохраняются автоматически");
            float y = 250;
            Row("Общая громкость", y, () => Pct(Bvr2Save.Master), d => Bvr2Save.Master = Step(Bvr2Save.Master, d)); y -= 92;
            Row("Музыка", y, () => Pct(Bvr2Save.Music), d => Bvr2Save.Music = Step(Bvr2Save.Music, d)); y -= 92;
            Row("Голос профессора", y, () => Pct(Bvr2Save.Voice), d => Bvr2Save.Voice = Step(Bvr2Save.Voice, d)); y -= 92;
            Row("Субтитры", y, () => Bvr2Save.Subtitles ? "Вкл" : "Выкл", d => Bvr2Save.Subtitles = !Bvr2Save.Subtitles); y -= 92;
            Row("Поворот в VR (шаг)", y, () => Bvr2Save.Snap + "°", d => { int[] s = { 30, 45, 90 }; int i = Array.IndexOf(s, Bvr2Save.Snap); Bvr2Save.Snap = s[((i < 0 ? 0 : i) + d + s.Length) % s.Length]; }); y -= 92;
            Row("Виньетка при движении", y, () => Bvr2Save.Vignette ? "Вкл" : "Выкл", d => Bvr2Save.Vignette = !Bvr2Save.Vignette);
            Back(new Vector2(-560, -410));
            if (!pauseMode) { var r = Button(canvas, "Сбросить прогресс", "", new Vector2(450, -318), new Vector2(420, 80), () => Show(Page.ConfirmReset), 34); r.tint = Bvr2Style.Danger; r.label.alignment = TextAnchor.MiddleCenter; }
            Hint();
        }

        void Row(string name, float y, Func<string> value, Action<int> change)
        {
            Txt(canvas, name, 40, Bvr2Style.Text, new Vector2(-330, y), new Vector2(620, 70), TextAnchor.MiddleLeft);
            var box = Bvr2Ui.Rect(canvas, "Value", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)); Bvr2Ui.Place(box, new Vector2(0.5f, 0.5f), new Vector2(260, 70), new Vector2(330, y));
            Bvr2Ui.Panel(box, new Color(0.02f, 0.07f, 0.1f, 0.95f), "Back");
            var v = Txt(box, value(), 38, Bvr2Style.Knowledge, Vector2.zero, new Vector2(250, 66), TextAnchor.MiddleCenter, FontStyle.Bold);
            settingValues.Add(v);
            Action<int> apply = d => { change(d); Bvr2Save.Apply(game); v.text = value(); game.sound?.Play("UI_Panel", 0.7f, d > 0 ? 1.1f : 0.9f); };
            Button(canvas, "◀", "", new Vector2(150, y), new Vector2(84, 70), () => apply(-1), 36).label.alignment = TextAnchor.MiddleCenter;
            Button(canvas, "▶", "", new Vector2(510, y), new Vector2(84, 70), () => apply(+1), 36).label.alignment = TextAnchor.MiddleCenter;
        }

        static string Pct(float v) => Mathf.RoundToInt(v * 100f) + " %";
        static float Step(float v, int d) => Mathf.Clamp01(Mathf.Round(v * 10f + d) / 10f);

        void BuildAbout()
        {
            Header("ОБ ИГРЕ", "Biology VR · версия " + version);
            string body =
                "Учебная VR-игра о крови и сосудах для 8–9 классов. Пролог в лаборатории профессора и шесть глав внутри одной артерии: " +
                "«Поток», «Бляшка», «Первый контакт», «Прорыв», «Очаг», «Гомеостаз». Наблюдай, предсказывай, проверяй: сканер собирает знания, " +
                "BioTool притягивает клетки и меняет способности, профессор подсказывает по связи.\n\n" +
                "<b>VR:</b> левый стик — плыть (в лаборатории — идти) · правый стик — вверх/вниз и поворот · A/X — рывок · левый курок — сканер · " +
                "правый курок — BioTool, B — сменить способность · хват — якорь · Menu или Y — пауза.\n\n" +
                "<b>Компьютер:</b> WASD — движение · мышь — взгляд · Пробел / C — вверх / вниз · ПКМ или F — сканер · ЛКМ — BioTool · Q — способность · " +
                "E — якорь · Shift — рывок · Tab или Esc — пауза.";
            var t = Txt(canvas, body, 31, Bvr2Style.Text, new Vector2(-40, -25), new Vector2(1300, 540), TextAnchor.UpperLeft); t.lineSpacing = 1.15f; Bvr2Ui.Fit(t, 22, 31);
            Txt(canvas, "Проект Biology VR · github.com/Altersam/Biology_VR_App\nГолос профессора — синтез речи · лаборатория — по ассету SecVarLab", 25, Bvr2Style.TextDim, new Vector2(130, -400), new Vector2(1100, 80), TextAnchor.MiddleRight);
            Back(new Vector2(-560, -410));
        }

        void BuildConfirm(string question, string note, string yes, Action onYes, string no, Action onNo)
        {
            Txt(canvas, question, 70, Bvr2Style.Text, new Vector2(0, 150), new Vector2(1300, 100), TextAnchor.MiddleCenter, FontStyle.Bold);
            Txt(canvas, note, 34, Bvr2Style.TextDim, new Vector2(0, 50), new Vector2(1200, 80));
            Button(canvas, no, "", new Vector2(-230, -140), new Vector2(400, 110), onNo, 44).label.alignment = TextAnchor.MiddleCenter;
            var y = Button(canvas, yes, "", new Vector2(230, -140), new Vector2(400, 110), onYes, 44); y.label.alignment = TextAnchor.MiddleCenter; y.tint = Bvr2Style.Danger;
        }

        void BuildPause()
        {
            var cur = game.campaign ? game.campaign.Current : null;
            Header("ПАУЗА", cur ? ChapterName(cur.Number) : "");
            float x = 0, y = 200, step = 120; var size = new Vector2(720, 104);
            Button(canvas, "Продолжить", "вернуться в игру", new Vector2(x, y), size, Resume);
            Button(canvas, "Начать главу заново", cur ? "с самого начала главы" : "", new Vector2(x, y -= step), size, () => { if (pauseMode) EndPause(); game.campaign.RestartCurrent(); });
            Button(canvas, "Настройки", "звук, субтитры, поворот, комфорт", new Vector2(x, y -= step), size, () => Show(Page.Settings));
            Button(canvas, "Главы", "перейти к другой главе", new Vector2(x, y -= step), size, () => Show(Page.Chapters));
            Button(canvas, "В главное меню", "прогресс пройденных глав сохранён", new Vector2(x, y -= step), size, () => game.campaign.ToMainMenu());
            Hint();
        }

        // ======================================================================================= widgets
        void Header(string title, string sub)
        {
            Txt(canvas, title, 62, Bvr2Style.Text, new Vector2(-180, 405), new Vector2(1080, 80), TextAnchor.MiddleLeft, FontStyle.Bold);
            if (!string.IsNullOrEmpty(sub)) Txt(canvas, sub, 30, Bvr2Style.TextDim, new Vector2(-180, 352), new Vector2(1080, 44), TextAnchor.MiddleLeft);
            Line(canvas, new Vector2(0, 318), 1380);
        }

        void Hint()
        {
            string h = Xr ? "наведите луч и нажмите курок  ·  B — назад" : "наведите прицел и нажмите ЛКМ  ·  ↑ ↓ и Enter  ·  Esc — назад";
            if (Current == Page.Main) h = Xr ? "наведите луч и нажмите курок" : "наведите прицел и нажмите ЛКМ  ·  ↑ ↓ и Enter";
            Txt(canvas, h, 26, Bvr2Style.TextDim, new Vector2(Current == Page.Chapters || Current == Page.Settings ? 140 : 0, -440), new Vector2(1000, 40));
        }

        void Back(Vector2 pos)
        {
            Button(canvas, "←  Назад", "", pos, new Vector2(300, 84), GoBack, 36).label.alignment = TextAnchor.MiddleCenter;
        }

        Btn Button(RectTransform parent, string label, string sub, Vector2 pos, Vector2 size, Action click, int fontSize = 46)
        {
            var r = Bvr2Ui.Rect(parent, "Button " + label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)); Bvr2Ui.Place(r, new Vector2(0.5f, 0.5f), size, pos);
            var b = new Btn { rt = r, click = click };
            b.back = Bvr2Ui.Panel(r, ButtonIdle, "Back");
            b.edge = Bvr2Ui.Panel(r, Bvr2Style.Ally, "Edge"); var er = b.edge.rectTransform; er.anchorMin = new Vector2(0, 0.12f); er.anchorMax = new Vector2(0, 0.88f); er.sizeDelta = new Vector2(7, 0); er.anchoredPosition = new Vector2(10, 0);
            bool hasSub = !string.IsNullOrEmpty(sub);
            b.label = Bvr2Ui.Label(r, label, fontSize, Bvr2Style.Text, hasSub ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft, FontStyle.Bold, "Label");
            b.label.rectTransform.offsetMin = new Vector2(34, hasSub ? 0 : 0); b.label.rectTransform.offsetMax = new Vector2(-20, hasSub ? -10 : 0);
            if (hasSub) { b.sub = Bvr2Ui.Label(r, sub, 26, Bvr2Style.TextDim, TextAnchor.LowerLeft, FontStyle.Normal, "Sub"); b.sub.rectTransform.offsetMin = new Vector2(36, 12); b.sub.rectTransform.offsetMax = new Vector2(-20, 0); }
            buttons.Add(b);
            return b;
        }

        Text Txt(RectTransform parent, string text, int size, Color color, Vector2 pos, Vector2 box, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var r = Bvr2Ui.Rect(parent, "Text", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)); Bvr2Ui.Place(r, new Vector2(0.5f, 0.5f), box, pos);
            return Bvr2Ui.Label(r, text, size, color, align, style);
        }

        void Line(RectTransform parent, Vector2 pos, float width)
        {
            var i = Bvr2Ui.Panel(parent, new Color(Bvr2Style.Ally.r, Bvr2Style.Ally.g, Bvr2Style.Ally.b, 0.35f), "Line", null); i.sprite = null; i.type = Image.Type.Simple;
            Bvr2Ui.Place(i.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(width, 3), pos);
        }

        void Frame(RectTransform c)
        {
            var col = new Color(Bvr2Style.Ally.r, Bvr2Style.Ally.g, Bvr2Style.Ally.b, 0.9f); const float L = 60, T = 6;
            foreach (var a in new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) })
            {
                float sx = a.x < 0.5f ? 1 : -1, sy = a.y < 0.5f ? 1 : -1;
                var h = Bvr2Ui.Panel(c, col, "Corner", null); h.sprite = null; h.type = Image.Type.Simple; var hr = h.rectTransform; hr.anchorMin = hr.anchorMax = a; hr.sizeDelta = new Vector2(L, T); hr.anchoredPosition = new Vector2(sx * L / 2, sy * T / 2);
                var v = Bvr2Ui.Panel(c, col, "Corner", null); v.sprite = null; v.type = Image.Type.Simple; var vr = v.rectTransform; vr.anchorMin = vr.anchorMax = a; vr.sizeDelta = new Vector2(T, L); vr.anchoredPosition = new Vector2(sx * T / 2, sy * L / 2);
            }
        }

        // ======================================================================================= actions
        void GoBack()
        {
            game.sound?.Play("UI_Panel", 0.6f, 0.85f);
            switch (Current)
            {
                case Page.Chapters: case Page.Settings: Show(pauseMode ? Page.Pause : Page.Main); break;
                case Page.About: case Page.ConfirmExit: Show(Page.Main); break;
                case Page.ConfirmReset: Show(Page.Settings); break;
                case Page.Pause: Resume(); break;
            }
        }

        static void Quit()
        {
#if UNITY_EDITOR
            Debug.Log("[BVR2] Menu: Exit (ignored in the editor)");
#else
            Application.Quit();
#endif
        }

        List<Bvr2ChapterBase> Chapters()
        {
            var list = new List<Bvr2ChapterBase>();
            if (game && game.campaign) foreach (var c in game.campaign.chapters) if (c) list.Add(c);
            list.Sort((a, b) => a.Number.CompareTo(b.Number));
            return list;
        }

        string ChapterName(int number)
        {
            foreach (var c in Chapters()) if (c.Number == number) return (number == 0 ? "пролог" : "глава " + number) + " «" + c.Title + "»";
            return number == 0 ? "пролог" : "глава " + number;
        }

        bool Xr => game && game.rig && game.rig.IsXR;

        // ======================================================================================= update
        void Update()
        {
            if (!game || !game.input) return;
            var input = game.input;
            if (!IsOpen)
            {
                // the click that closed the menu must not reach the game: give the controls back once the trigger is up
                if (releasePending && (!input.UiTriggerHeld || Time.unscaledTime - closedAt > 1.2f)) { releasePending = false; input.Captured = false; }
                // in a chapter: the Menu button / Y (Tab, Esc) pauses the game
                if (!releasePending && input.PauseDown && game.campaign && game.campaign.Current && !game.campaign.Current.autoplay) OpenPause();
                return;
            }
            float dt = Time.unscaledDeltaTime, now = Time.unscaledTime;
            if (onStage && spinners != null)
                for (int i = 0; i < spinners.Length; i++) if (spinners[i]) spinners[i].Rotate(Vector3.up, (spinSpeeds != null && i < spinSpeeds.Length ? spinSpeeds[i] : 10f) * dt, Space.Self);
            if (!canvas) return;
            group.alpha = Mathf.MoveTowards(group.alpha, 1f, dt * 4f);
            canvas.localScale = Vector3.one * (pauseMode ? 0.00092f : 0.00105f) * Mathf.Lerp(0.96f, 1f, group.alpha);
            if (blink) blink.color = new Color(blink.color.r, blink.color.g, blink.color.b, 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(now * 3.2f)));

            // pointer
            Transform hand = Xr ? (input.UiHandLeft ? game.rig.leftHand : game.rig.rightHand) : game.rig.head.transform;
            var ray = new Ray(hand.position, hand.forward);
            int hit = Hit(ray, out Vector3 point, out bool onCanvas);
            if (hit != lastRayHit) { if (hit >= 0 && buttons[hit].enabled) { SetFocus(hit); focusByNav = false; } lastRayHit = hit; }
            if (reticle)
            {
                reticle.enabled = onCanvas && Current != Page.Title;
                if (onCanvas) { Vector3 l = canvas.InverseTransformPoint(point); reticle.rectTransform.anchoredPosition = new Vector2(l.x, l.y); }
            }
            if (Xr)
            {
                if (!laser) MakeLaser();
                laser.enabled = true;
                Vector3 end = onCanvas ? point : ray.GetPoint(2.5f);
                laser.SetPosition(0, hand.position + hand.forward * 0.03f); laser.SetPosition(1, end);
            }
            else if (laser) laser.enabled = false;

            // keyboard / stick focus
            if (input.UiNavY != 0) { MoveFocus(input.UiNavY); focusByNav = true; }
            if (input.UiNavX != 0 && (Current == Page.Settings || Current == Page.Chapters || Current == Page.ConfirmExit || Current == Page.ConfirmReset)) { MoveFocus(input.UiNavX); focusByNav = true; }

            // buttons
            for (int i = 0; i < buttons.Count; i++)
            {
                var b = buttons[i]; bool on = i == focus && b.enabled;
                b.hover = Mathf.MoveTowards(b.hover, on ? 1f : 0f, dt * 8f);
                b.back.color = !b.enabled ? ButtonOff : Color.Lerp(ButtonIdle, Color.Lerp(ButtonHover, b.tint * 0.45f + new Color(0, 0, 0, 0.98f), b.tint == Bvr2Style.Ally ? 0f : 0.5f), b.hover);
                b.edge.color = new Color(b.tint.r, b.tint.g, b.tint.b, b.enabled ? 0.35f + 0.65f * b.hover : 0.12f);
                b.label.color = b.enabled ? Color.Lerp(Bvr2Style.Text, Color.white, b.hover) : new Color(0.5f, 0.58f, 0.62f);
                b.rt.localScale = Vector3.one * (1f + 0.025f * b.hover);
            }

            // confirm / back
            bool ready = now - pageAt > 0.25f;
            if (Current == Page.Title)
            {
                if (ready && input.UiClickDown) { game.sound?.Play("SFX_Success_Step", 0.7f); Bvr2Haptics.Pulse(input.UiHandLeft, 0.4f, 0.06f); Show(Page.Main); }
                return;
            }
            if (ready && input.UiClickDown)
            {
                // a trigger / mouse click presses the button under the pointer; Enter (or a trigger after
                // stepping with the stick) presses the focused one
                int target = hit >= 0 ? hit : (input.UiKeyConfirm || focusByNav) ? focus : -1;
                if (target >= 0 && target < buttons.Count && buttons[target].enabled) { Click(target); return; }
            }
            if (ready && input.UiBackDown) { GoBack(); return; }
            if (ready && pauseMode && Current == Page.Pause && input.PauseDown) Resume();
        }

        void Click(int i)
        {
            var b = buttons[i];
            game.sound?.Play("UI_Panel", 0.9f, 1.15f);
            Bvr2Haptics.Pulse(game.input.UiHandLeft, 0.45f, 0.06f);
            b.click?.Invoke();
        }

        void SetFocus(int i)
        {
            if (i == focus) return;
            focus = i;
            game.sound?.Play("UI_Hover", 0.45f);
            Bvr2Haptics.Pulse(game.input.UiHandLeft, 0.12f, 0.02f);
        }

        void MoveFocus(int d)
        {
            if (buttons.Count == 0) return;
            int i = focus < 0 ? (d > 0 ? -1 : 0) : focus;
            for (int k = 0; k < buttons.Count; k++)
            {
                i = (i + d + buttons.Count) % buttons.Count;
                if (buttons[i].enabled) { SetFocus(i); return; }
            }
        }

        int Hit(Ray ray, out Vector3 point, out bool onCanvas)
        {
            point = Vector3.zero; onCanvas = false;
            if (!canvas) return -1;
            var plane = new Plane(-canvas.forward, canvas.position);
            if (!plane.Raycast(ray, out float d) || d <= 0) return -1;
            point = ray.GetPoint(d);
            Vector3 lc = canvas.InverseTransformPoint(point);
            onCanvas = canvas.rect.Contains(new Vector2(lc.x, lc.y));
            if (!onCanvas) return -1;
            for (int i = buttons.Count - 1; i >= 0; i--)
            {
                var rt = buttons[i].rt; Vector3 l = rt.InverseTransformPoint(point);
                if (rt.rect.Contains(new Vector2(l.x, l.y))) return i;
            }
            return -1;
        }

        void MakeLaser()
        {
            var go = new GameObject("Menu Laser"); go.transform.SetParent(transform, false);
            laser = go.AddComponent<LineRenderer>(); laser.positionCount = 2; laser.useWorldSpace = true; laser.widthMultiplier = 0.004f;
            laser.sharedMaterial = laserMaterial; laser.numCapVertices = 2; laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; laser.receiveShadows = false;
            laser.startColor = new Color(Bvr2Style.Ally.r, Bvr2Style.Ally.g, Bvr2Style.Ally.b, 0.15f); laser.endColor = new Color(Bvr2Style.Ally.r, Bvr2Style.Ally.g, Bvr2Style.Ally.b, 0.95f);
        }

        // ======================================================================================= tests / screenshots
        /// <summary>Debug bridge: open a page by name (Title, Main, Chapters, Settings, About, ConfirmExit, Pause).</summary>
        public void DebugPage(string page)
        {
            var p = (Page)Enum.Parse(typeof(Page), page);
            if (p == Page.Pause) { if (!pauseMode) OpenPause(); else Show(Page.Pause); return; }
            if (!onStage && !pauseMode) EnterStage();
            Show(p);
        }

        /// <summary>Debug bridge: press the button whose label contains the text (or the focused one for "").</summary>
        public string DebugClick(string label)
        {
            if (Current == Page.Title) { Show(Page.Main); return "title → main"; }
            for (int i = 0; i < buttons.Count; i++)
                if (buttons[i].enabled && (label == "" ? i == focus : buttons[i].label.text.Contains(label))) { focus = i; Click(i); return "clicked " + buttons.Count; }
            return "no button " + label;
        }

        /// <summary>Debug bridge: open the pause menu as if the Menu button was pressed.</summary>
        public void DebugPause() { if (!IsOpen && game.campaign && game.campaign.Current) OpenPause(); }
    }
}
