using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.V2
{
    /// <summary>
    /// Player-facing interface. Visor layer (lazy-follow, 2 m): one-line objective on top,
    /// control prompt, professor subtitles below the horizon, toasts and chapter titles.
    /// Wrist panel on the left hand: live gauges (pressure, pulse, flow, oxygen) and counters.
    /// Nothing important farther than ~25° from the view centre, short texts only.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class Bvr2Hud : MonoBehaviour
    {
        public Bvr2Rig rig;
        public Bvr2Organism organism;
        public float visorDistance = 2f;
        [Tooltip("The fourth wrist gauge shows temperature instead of oxygen (infection chapters).")]
        public bool showTemperature;

        RectTransform visor; Text objective, objectiveTag, prompt, subtitle, speaker, toast, title, subtitleTitle, compassLabel;
        Image objectivePanel, subtitlePanel, compassArrow; CanvasGroup objectiveGroup, subtitleGroup, toastGroup, titleGroup, promptGroup, compassGroup;
        RectTransform wrist; CanvasGroup wristGroup; Text knowledgeText, samplesText, wristTitle;
        Gauge pressure, pulse, flow, oxygen;
        float subtitleUntil, toastUntil, titleUntil; Vector3 visorForward; bool built;
        RectTransform objectiveTop, promptTop; float topShown = 1;
        /// <summary>While a question/board is in front of the player the top lines step aside.</summary>
        public bool TopHidden { get; set; }

        // ---- comm screen: the professor's waist-up hologram pops up here while he speaks ----
        public Transform CommDock { get; private set; }
        public float CommPanelHeight { get; private set; } = 0.42f;
        /// <summary>Written by the professor every frame (0 hidden … 1 fully materialised).</summary>
        public float CommAlpha { get; set; }
        [Header("Comm screen")]
        public Vector2 commAngles = new Vector2(-33.5f, 1f);
        public float commDistance = 1.25f;
        RectTransform commCanvas; CanvasGroup commGroup; Image commDot, commGlow; Image[] commBars; float commShown, commGlitch;
        Vector3? compassTarget; string compassName; Text compassGlyph;

        class Gauge
        {
            public Image fill, back; public Text value, label; public RectTransform root; public float shown;
        }

        void Start() { Build(); }

        public void Build()
        {
            if (built || !rig || !rig.head) return; built = true;
            // ---- visor -----------------------------------------------------------------
            visor = new GameObject("Visor", typeof(RectTransform)).GetComponent<RectTransform>();
            visor.SetParent(transform, false);
            var canvas = Bvr2Ui.Canvas("Visor Canvas", visor, new Vector2(1600, 900), 0.0018f, 80);
            canvas.localPosition = new Vector3(0, 0, visorDistance);

            var obj = Bvr2Ui.Rect(canvas, "Objective", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Bvr2Ui.Place(obj, new Vector2(0.5f, 0.5f), new Vector2(1120, 118), new Vector2(0, 318));
            objectiveGroup = obj.gameObject.AddComponent<CanvasGroup>();
            objectivePanel = Bvr2Ui.Panel(obj, Bvr2Style.Panel, "Back");
            var edge = Bvr2Ui.Panel(obj, Bvr2Style.Knowledge, "Edge"); var er = edge.rectTransform; er.anchorMin = new Vector2(0, 0); er.anchorMax = new Vector2(0, 1); er.offsetMin = new Vector2(0, 10); er.offsetMax = new Vector2(8, -10);
            objectiveTag = Bvr2Ui.Label(obj, "ЦЕЛЬ", 24, Bvr2Style.Knowledge, TextAnchor.UpperLeft, FontStyle.Bold, "Tag");
            objectiveTag.rectTransform.offsetMin = new Vector2(28, 0); objectiveTag.rectTransform.offsetMax = new Vector2(-20, -10);
            objective = Bvr2Ui.Label(obj, "", 42, Bvr2Style.Text, TextAnchor.MiddleLeft, FontStyle.Bold, "Objective");
            objective.rectTransform.offsetMin = new Vector2(28, 8); objective.rectTransform.offsetMax = new Vector2(-24, -34);
            objective.resizeTextForBestFit = true; objective.resizeTextMinSize = 24; objective.resizeTextMaxSize = 42;
            objective.horizontalOverflow = HorizontalWrapMode.Wrap; objective.verticalOverflow = VerticalWrapMode.Truncate;
            objectiveTop = obj;

            var pr = Bvr2Ui.Rect(canvas, "Prompt", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Bvr2Ui.Place(pr, new Vector2(0.5f, 0.5f), new Vector2(1080, 56), new Vector2(0, 236));
            promptGroup = pr.gameObject.AddComponent<CanvasGroup>(); promptTop = pr;
            Bvr2Ui.Panel(pr, new Color(0.02f, 0.06f, 0.09f, 0.72f), "Back");
            prompt = Bvr2Ui.Label(pr, "", 32, Bvr2Style.Ally, TextAnchor.MiddleCenter, FontStyle.Bold, "Prompt");
            Bvr2Ui.Fit(prompt, 20, 32);

            var sub = Bvr2Ui.Rect(canvas, "Subtitle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Bvr2Ui.Place(sub, new Vector2(0.5f, 0.5f), new Vector2(1080, 184), new Vector2(0, -300));
            subtitleGroup = sub.gameObject.AddComponent<CanvasGroup>(); subtitleGroup.alpha = 0;
            subtitlePanel = Bvr2Ui.Panel(sub, Bvr2Style.Panel, "Back");
            speaker = Bvr2Ui.Label(sub, "ПРОФЕССОР", 24, Bvr2Style.Ally, TextAnchor.UpperLeft, FontStyle.Bold, "Speaker");
            speaker.rectTransform.offsetMin = new Vector2(30, 0); speaker.rectTransform.offsetMax = new Vector2(-30, -12);
            subtitle = Bvr2Ui.Label(sub, "", 38, Bvr2Style.Text, TextAnchor.UpperLeft, FontStyle.Normal, "Line");
            subtitle.rectTransform.offsetMin = new Vector2(30, 10); subtitle.rectTransform.offsetMax = new Vector2(-30, -44);
            subtitle.lineSpacing = 1.05f;
            subtitle.resizeTextForBestFit = true; subtitle.resizeTextMinSize = 24; subtitle.resizeTextMaxSize = 38;
            subtitle.horizontalOverflow = HorizontalWrapMode.Wrap; subtitle.verticalOverflow = VerticalWrapMode.Truncate;

            var ts = Bvr2Ui.Rect(canvas, "Toast", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Bvr2Ui.Place(ts, new Vector2(0.5f, 0.5f), new Vector2(1200, 90), new Vector2(0, 120));
            toastGroup = ts.gameObject.AddComponent<CanvasGroup>(); toastGroup.alpha = 0;
            toast = Bvr2Ui.Label(ts, "", 52, Bvr2Style.Knowledge, TextAnchor.MiddleCenter, FontStyle.Bold, "Toast");
            var sh = toast.gameObject.AddComponent<Outline>(); sh.effectColor = new Color(0, 0, 0, 0.75f); sh.effectDistance = new Vector2(2, -2);

            var tt = Bvr2Ui.Rect(canvas, "Title", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Bvr2Ui.Place(tt, new Vector2(0.5f, 0.5f), new Vector2(1500, 300), new Vector2(0, 40));
            titleGroup = tt.gameObject.AddComponent<CanvasGroup>(); titleGroup.alpha = 0;
            title = Bvr2Ui.Label(tt, "", 96, Bvr2Style.Text, TextAnchor.MiddleCenter, FontStyle.Bold, "Title");
            title.rectTransform.offsetMin = new Vector2(0, 90);
            subtitleTitle = Bvr2Ui.Label(tt, "", 40, Bvr2Style.Ally, TextAnchor.UpperCenter, FontStyle.Normal, "Sub");
            subtitleTitle.rectTransform.offsetMax = new Vector2(0, -205);

            var cp = Bvr2Ui.Rect(canvas, "Compass", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Bvr2Ui.Place(cp, new Vector2(0.5f, 0.5f), new Vector2(120, 120), Vector2.zero);
            compassGroup = cp.gameObject.AddComponent<CanvasGroup>(); compassGroup.alpha = 0;
            compassArrow = Bvr2Ui.Panel(cp, Bvr2Style.Knowledge, "Arrow", Bvr2Ui.Glow);
            compassArrow.type = Image.Type.Simple;
            var arrowText = Bvr2Ui.Label(compassArrow.rectTransform, "→", 96, Bvr2Style.Knowledge, TextAnchor.MiddleCenter, FontStyle.Bold, "Glyph");
            compassGlyph = arrowText;
            compassArrow.color = new Color(1, 0.8f, 0.3f, 0.25f);
            compassLabel = Bvr2Ui.Label(cp, "", 30, Bvr2Style.Knowledge, TextAnchor.UpperCenter, FontStyle.Bold, "Name");
            compassLabel.rectTransform.offsetMin = new Vector2(-200, -70); compassLabel.rectTransform.offsetMax = new Vector2(200, -110);
            arrowText.rectTransform.localRotation = Quaternion.identity;

            BuildComm();

            // ---- wrist panel ---------------------------------------------------------------
            wrist = Bvr2Ui.Canvas("Wrist Panel", transform, new Vector2(620, 380), 0.0005f, 70);
            wristGroup = wrist.gameObject.AddComponent<CanvasGroup>();
            Bvr2Ui.Panel(wrist, new Color(0.015f, 0.04f, 0.06f, 0.9f), "Back");
            var frame = Bvr2Ui.Panel(wrist, new Color(0.45f, 0.93f, 1f, 0.18f), "Frame"); frame.rectTransform.offsetMin = new Vector2(4, 4); frame.rectTransform.offsetMax = new Vector2(-4, -4);
            wristTitle = Bvr2Ui.Label(wrist, "ПАЦИЕНТ · АРТЕРИЯ", 26, Bvr2Style.TextDim, TextAnchor.UpperLeft, FontStyle.Bold, "Title");
            wristTitle.rectTransform.offsetMin = new Vector2(24, 0); wristTitle.rectTransform.offsetMax = new Vector2(-24, -14);
            pressure = MakeGauge(wrist, "ДАВЛЕНИЕ", new Vector2(-225, 22));
            pulse = MakeGauge(wrist, "ПУЛЬС", new Vector2(-75, 22));
            flow = MakeGauge(wrist, "КРОВОТОК", new Vector2(75, 22));
            oxygen = MakeGauge(wrist, "КИСЛОРОД", new Vector2(225, 22));
            knowledgeText = Bvr2Ui.Label(wrist, "", 28, Bvr2Style.Knowledge, TextAnchor.LowerLeft, FontStyle.Bold, "Knowledge");
            knowledgeText.rectTransform.offsetMin = new Vector2(24, 16); knowledgeText.rectTransform.offsetMax = new Vector2(-24, 0);
            samplesText = Bvr2Ui.Label(wrist, "", 28, Bvr2Style.Ally, TextAnchor.LowerRight, FontStyle.Bold, "Samples");
            samplesText.rectTransform.offsetMin = new Vector2(24, 16); samplesText.rectTransform.offsetMax = new Vector2(-24, 0);
            SetObjective(Objective); SetPrompt(Prompt);
        }

        void BuildComm()
        {
            var dock = new GameObject("Comm Dock").transform; dock.SetParent(visor, false);
            Vector3 dir = Quaternion.Euler(-commAngles.y, commAngles.x, 0) * Vector3.forward;
            dock.localPosition = dir * commDistance; dock.localRotation = Quaternion.LookRotation(dir, Vector3.up);
            CommDock = dock;
            const float mpu = 0.001f; var size = new Vector2(350, 450); CommPanelHeight = size.y * mpu;
            commCanvas = Bvr2Ui.Canvas("Comm Panel", dock, size, mpu, 75);
            commGroup = commCanvas.gameObject.AddComponent<CanvasGroup>(); commGroup.alpha = 0;
            // dark glass behind the hologram (regular UI material so the hologram draws over it)
            var back = Bvr2Ui.Panel(commCanvas, new Color(0.01f, 0.05f, 0.07f, 0.72f), "Glass"); back.material = null;
            Bvr2Ui.Backdrop(commCanvas, 12f, 0f);
            // projector glow at the cut line
            commGlow = Bvr2Ui.Panel(commCanvas, new Color(0.45f, 0.93f, 1f, 0.55f), "Projector", Bvr2Ui.Glow);
            var gr = commGlow.rectTransform; gr.anchorMin = gr.anchorMax = new Vector2(0.5f, 0f); gr.sizeDelta = new Vector2(300, 46); gr.anchoredPosition = new Vector2(0, 34);
            // frame
            var edge = new Color(0.45f, 0.93f, 1f, 0.85f);
            void Line(Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax) { var l = Bvr2Ui.Panel(commCanvas, edge, "Frame"); var r = l.rectTransform; r.anchorMin = aMin; r.anchorMax = aMax; r.offsetMin = oMin; r.offsetMax = oMax; }
            Line(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -3), Vector2.zero);
            Line(new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 3));
            Line(new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(3, 0));
            Line(new Vector2(1, 0), new Vector2(1, 1), new Vector2(-3, 0), Vector2.zero);
            // corner accents
            foreach (var c in new[] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0) })
            {
                var k = Bvr2Ui.Panel(commCanvas, Bvr2Style.Ally, "Corner"); var r = k.rectTransform; r.anchorMin = r.anchorMax = c; r.sizeDelta = new Vector2(26, 26); r.anchoredPosition = new Vector2(c.x > 0.5f ? -10 : 10, c.y > 0.5f ? -10 : 10);
            }
            // header: live dot, caller, signal bars
            var header = Bvr2Ui.Rect(commCanvas, "Header", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -46), new Vector2(0, -6));
            commDot = Bvr2Ui.Panel(header, new Color(1f, 0.35f, 0.3f), "Live", Bvr2Ui.Dot); var dr = commDot.rectTransform; dr.anchorMin = dr.anchorMax = new Vector2(0, 0.5f); dr.sizeDelta = new Vector2(16, 16); dr.anchoredPosition = new Vector2(20, 0);
            var cap = Bvr2Ui.Label(header, "ПРОФЕССОР · СВЯЗЬ", 22, Bvr2Style.Ally, TextAnchor.MiddleLeft, FontStyle.Bold, "Caller"); cap.rectTransform.offsetMin = new Vector2(36, 0);
            commBars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var b = Bvr2Ui.Panel(header, Bvr2Style.Ally, "Bar"); var br = b.rectTransform; br.anchorMin = br.anchorMax = new Vector2(1, 0.5f); br.pivot = new Vector2(0.5f, 0f);
                br.sizeDelta = new Vector2(6, 8 + i * 6); br.anchoredPosition = new Vector2(-34 + i * 10, -9); commBars[i] = b;
            }
        }

        void UpdateComm(float dt)
        {
            if (!commCanvas) return;
            float a = Mathf.Clamp01(CommAlpha);
            if (a > 0.01f && commShown <= 0.01f) commGlitch = 0.28f; // incoming call: a short glitch
            commShown = a; commGroup.alpha = Mathf.Clamp01(a * 1.6f);
            commCanvas.gameObject.SetActive(a > 0.001f);
            if (a <= 0.001f) return;
            float t = Time.unscaledTime;
            commGlitch = Mathf.Max(0, commGlitch - dt);
            float jitter = commGlitch > 0 ? (Mathf.PerlinNoise(t * 60f, 0.3f) - 0.5f) * 0.02f : 0f;
            commCanvas.localPosition = new Vector3(jitter, 0, 0);
            commCanvas.localScale = Vector3.one * 0.001f * Mathf.Lerp(0.86f, 1f, Mathf.SmoothStep(0, 1, a));
            if (commDot) commDot.color = new Color(1f, 0.35f, 0.3f, 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(t * 3f)));
            if (commGlow) commGlow.color = new Color(0.45f, 0.93f, 1f, (0.35f + 0.2f * Mathf.Sin(t * 7f)) * a);
            if (commBars != null) for (int i = 0; i < commBars.Length; i++) commBars[i].color = new Color(0.45f, 0.93f, 1f, (i < 2 || Mathf.Repeat(t, 2.3f) > 0.4f) ? 0.95f : 0.25f);
        }

        Gauge MakeGauge(Transform parent, string name, Vector2 pos)
        {
            var g = new Gauge();
            g.root = Bvr2Ui.Rect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Bvr2Ui.Place(g.root, new Vector2(0.5f, 0.5f), new Vector2(130, 130), pos);
            g.back = Bvr2Ui.Fill(g.root, new Color(1, 1, 1, 0.12f), Bvr2Ui.RingSprite, 0.75f, "Back");
            g.back.rectTransform.localRotation = Quaternion.Euler(0, 0, -45);
            g.fill = Bvr2Ui.Fill(g.root, Bvr2Style.Good, Bvr2Ui.RingSprite, 0.4f, "Fill");
            g.fill.rectTransform.localRotation = Quaternion.Euler(0, 0, -45);
            g.value = Bvr2Ui.Label(g.root, "", 34, Bvr2Style.Text, TextAnchor.MiddleCenter, FontStyle.Bold, "Value");
            g.label = Bvr2Ui.Label(g.root, name, 19, Bvr2Style.TextDim, TextAnchor.UpperCenter, FontStyle.Bold, "Label");
            g.label.rectTransform.offsetMin = new Vector2(-40, -44); g.label.rectTransform.offsetMax = new Vector2(40, -118);
            return g;
        }

        // ----- public API -----------------------------------------------------------------
        public string Objective { get; private set; } = "";
        public string Prompt { get; private set; } = "";
        public string LastToast { get; private set; } = "";
        public string LastLine { get; private set; } = "";

        public void SetObjective(string text) { Objective = text ?? ""; if (!objective) return; objective.text = Objective; objectiveGroup.alpha = string.IsNullOrEmpty(Objective) ? 0 : 1; }
        public void SetPrompt(string text) { Prompt = text ?? ""; if (!prompt) return; prompt.text = Prompt; promptGroup.alpha = string.IsNullOrEmpty(Prompt) ? 0 : 1; }
        public void Say(string who, string text, float seconds)
        {
            LastLine = text; if (!subtitle) return;
            speaker.text = who.ToUpperInvariant(); subtitle.text = text; subtitleUntil = Time.unscaledTime + seconds; subtitleGroup.alpha = 1;
        }
        public void ClearSubtitle() { subtitleUntil = 0; }
        public void Toast(string text, Color color, float seconds = 2.2f)
        {
            LastToast = text; if (!toast) return;
            toast.text = text; toast.color = color; toastUntil = Time.unscaledTime + seconds; toastGroup.alpha = 1; toast.rectTransform.localScale = Vector3.one * 1.25f;
        }
        public void Title(string big, string small, float seconds)
        {
            if (!title) return; title.text = big; subtitleTitle.text = small; titleUntil = Time.unscaledTime + seconds; titleGroup.alpha = 0.01f;
        }
        public void SetCompass(Vector3? target, string name) { compassTarget = target; compassName = name; }
        public void SetCounters(string knowledge, string samples) { if (knowledgeText) { knowledgeText.text = knowledge; samplesText.text = samples; } }
        public bool SubtitleVisible => Time.unscaledTime < subtitleUntil;

        void LateUpdate()
        {
            if (!built) { Build(); if (!built) return; }
            var head = rig.head.transform;
            float dt = Time.unscaledDeltaTime;
            // Visor: lazy follow in VR (comfortable), locked on desktop (it is the screen).
            visor.position = head.position;
            if (!rig.IsXR) visorForward = head.forward;
            else
            {
                if (visorForward == Vector3.zero) visorForward = head.forward;
                float angle = Vector3.Angle(visorForward, head.forward);
                if (angle > 14f) visorForward = Vector3.Slerp(visorForward, head.forward, 1f - Mathf.Exp(-dt * 3.5f * Mathf.InverseLerp(14f, 40f, angle) - dt * 0.5f));
            }
            visor.rotation = Quaternion.LookRotation(visorForward, rig.IsXR ? Vector3.up : head.up);

            subtitleGroup.alpha = Mathf.MoveTowards(subtitleGroup.alpha, Time.unscaledTime < subtitleUntil ? 1 : 0, dt * 3f);
            toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, Time.unscaledTime < toastUntil ? 1 : 0, dt * 2.5f);
            toast.rectTransform.localScale = Vector3.one * Mathf.MoveTowards(toast.rectTransform.localScale.x, 1f, dt * 1.5f);
            titleGroup.alpha = Mathf.MoveTowards(titleGroup.alpha, Time.unscaledTime < titleUntil ? 1 : 0, dt * (Time.unscaledTime < titleUntil ? 1.4f : 0.9f));
            topShown = Mathf.MoveTowards(topShown, TopHidden ? 0 : 1, dt * 4f);
            objectiveGroup.alpha = string.IsNullOrEmpty(Objective) ? 0 : topShown;
            promptGroup.alpha = string.IsNullOrEmpty(Prompt) ? 0 : topShown;
            UpdateCompass(head);
            UpdateComm(dt);
            UpdateWrist(head, dt);
        }

        void UpdateCompass(Transform head)
        {
            bool show = false;
            if (compassTarget.HasValue)
            {
                Vector3 local = Quaternion.Inverse(visor.rotation) * (compassTarget.Value - head.position);
                float ang = Vector3.Angle(Vector3.forward, local);
                var r = (RectTransform)compassArrow.transform.parent;
                show = true; compassLabel.text = compassName;
                if (ang > 28f)
                {
                    // off to the side: an arrow on the edge of the view
                    Vector2 dir = new Vector2(local.x, local.y); if (dir.sqrMagnitude < 1e-4f) dir = Vector2.right; dir.Normalize();
                    r.anchoredPosition = new Vector2(dir.x * 420f, dir.y * 220f);
                    compassArrow.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                    compassGlyph.text = "→"; compassGlyph.fontSize = 96;
                }
                else
                {
                    // in view: a marker sitting on the target, with its distance
                    float z = Mathf.Max(local.z, 0.1f);
                    Vector2 p = new Vector2(local.x, local.y) / z * visorDistance / 0.0018f;
                    r.anchoredPosition = Vector2.Lerp(r.anchoredPosition, p, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 20f));
                    compassArrow.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 3f) * 6f);
                    compassGlyph.text = "◇"; compassGlyph.fontSize = 70;
                    compassLabel.text = $"{compassName}  {local.magnitude:0} м";
                }
            }
            compassGroup.alpha = Mathf.MoveTowards(compassGroup.alpha, show ? 1 : 0, Time.unscaledDeltaTime * 4f);
        }

        void UpdateWrist(Transform head, float dt)
        {
            var hand = rig.leftHand;
            if (rig.IsXR && hand)
            {
                wrist.SetParent(hand, false);
                wrist.localPosition = new Vector3(0.0f, 0.045f, -0.11f); wrist.localRotation = Quaternion.Euler(62, 0, 0);
                float facing = Vector3.Dot(-wrist.forward, (head.position - wrist.position).normalized);
                float look = Mathf.InverseLerp(0.35f, 0.8f, facing);
                wristGroup.alpha = Mathf.Lerp(0.35f, 1f, look);
                wrist.localScale = Vector3.one * 0.0005f * Mathf.Lerp(0.9f, 1.25f, look);
            }
            else
            {
                wrist.SetParent(head, false);
                // desktop: bottom-left corner of the screen, whatever the aspect ratio
                var cam = rig.head; float d = 1f, halfH = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * d, halfW = halfH * cam.aspect;
                wrist.localPosition = new Vector3(-halfW + 0.2f, -halfH + 0.13f, d); wrist.localRotation = Quaternion.identity; wrist.localScale = Vector3.one * 0.00052f;
                wristGroup.alpha = 0.92f;
            }
            if (!organism) return;
            float sys = organism.systolic;
            SetGauge(pressure, Mathf.InverseLerp(60, 180, sys), organism.PressureText, sys < 90 || sys > 150 ? Bvr2Style.Danger : sys > 135 || sys < 100 ? Bvr2Style.Knowledge : Bvr2Style.Good, dt);
            float hr = organism.heartRate;
            var hrc = hr > 120 || hr < 50 ? Bvr2Style.Danger : hr > 95 ? Bvr2Style.Knowledge : Bvr2Style.Good;
            SetGauge(pulse, Mathf.InverseLerp(40, 180, hr), Mathf.RoundToInt(hr).ToString(), hrc, dt);
            pulse.value.rectTransform.localScale = Vector3.one * (1f + organism.Pulse * 0.18f);
            float q = organism.LocalFlow;
            SetGauge(flow, Mathf.InverseLerp(0, 1.6f, q), Mathf.RoundToInt(q * 100) + "%", q < 0.6f ? Bvr2Style.Danger : q < 0.85f || q > 1.3f ? Bvr2Style.Knowledge : Bvr2Style.Good, dt);
            if (showTemperature)
            {
                float tc = organism.temperature;
                oxygen.label.text = "ТЕМПЕРАТУРА";
                SetGauge(oxygen, Mathf.InverseLerp(35.5f, 40.5f, tc), tc.ToString("0.0") + "°", tc >= 38.5f ? Bvr2Style.Danger : tc >= 37.2f ? Bvr2Style.Knowledge : Bvr2Style.Good, dt);
            }
            else
            {
                float o2 = organism.spo2;
                oxygen.label.text = "КИСЛОРОД";
                SetGauge(oxygen, Mathf.InverseLerp(80, 100, o2), Mathf.RoundToInt(o2) + "%", o2 < 90 ? Bvr2Style.Danger : o2 < 95 ? Bvr2Style.Knowledge : Bvr2Style.Good, dt);
            }
        }

        static void SetGauge(Gauge g, float t, string value, Color c, float dt)
        {
            g.shown = Mathf.Lerp(g.shown, Mathf.Clamp01(t), 1f - Mathf.Exp(-dt * 6f));
            g.fill.fillAmount = g.shown * 0.75f; g.fill.color = c; g.value.text = value;
        }
    }
}
