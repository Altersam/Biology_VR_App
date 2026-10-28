using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.V2
{
    /// <summary>
    /// Prologue «Лаборатория» (scene 1): a short lab before the journey — meet the professor and his rule,
    /// take the two instruments from the desk, scan the patient's hologram (the first knowledge: the internal
    /// environment), record the starting vital signs, step into the shrinking capsule — the lab grows huge
    /// around you, then you dive through skin, fat and the artery wall straight into the bloodstream, where
    /// chapter 1 begins.
    /// </summary>
    public sealed class Bvr2Prelude : Bvr2ChapterBase
    {
        public override int Number => 0;
        public override string Title => "Лаборатория";

        public Transform labRoot, desk, toolReplicas, patientMap, capsule, capsuleRings, monitor;
        [Tooltip("Where the professor stands in the lab (full height): by the patient, at the bench, at the capsule door.")]
        public Transform profIntro, profTools, profCapsule;
        public Bvr2Scannable patientScan;
        public Text monitorText;
        public Light labLight;
        public Mesh plateMesh; public Material skinMaterial, fatMaterial, wallMaterial;
        public float arriveS = 14f;

        public bool ToolsTaken { get; private set; }
        public override string StatusLine => base.StatusLine + $" · tools {ToolsTaken} · room {game.swimmer.roomMode}";

        Color ambientBefore; bool ambientSaved; readonly List<Transform> plates = new List<Transform>();
        Vector3 labHome; bool homeSaved;

        [Tooltip("Player start in labRoot local space (eye height included).")]
        public Vector3 startLocal = new Vector3(0, 1.55f, -3f);
        [Tooltip("Walkable rectangles (x, z) in labRoot space; empty = the old round room.")]
        public Rect[] walkable = new Rect[0];

        void Start() { if (labRoot) { labHome = labRoot.position; homeSaved = true; labRoot.gameObject.SetActive(false); } if (labLight) labLight.enabled = false; }

        protected override void ResetProps()
        {
            ResetShared();
            LabMode(false);
            foreach (var p in plates) if (p) Destroy(p.gameObject);
            plates.Clear();
            ToolsTaken = false;
        }

        /// <summary>Switch the world between the lab (room, light, walking) and the artery.</summary>
        void LabMode(bool on)
        {
            if (labRoot)
            {
                if (homeSaved) { labRoot.position = labHome; labRoot.localScale = Vector3.one; }
                labRoot.gameObject.SetActive(on);
            }
            if (labLight) labLight.enabled = on;
            if (on && !ambientSaved) { ambientBefore = RenderSettings.ambientLight; ambientSaved = true; RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.68f); }
            if (!on && ambientSaved) { RenderSettings.ambientLight = ambientBefore; ambientSaved = false; }
            game.blood.enabled = !on;
            if (!on) Prof.LeaveStage();
            game.swimmer.roomMode = on; game.swimmer.frozen = false;
            if (on && labRoot) { game.swimmer.roomCentre = labRoot.position; game.swimmer.roomRadius = 5.2f; }
            game.swimmer.roomRects = on && walkable != null && walkable.Length > 0 ? walkable : null; game.swimmer.roomSpace = labRoot;
            ShowTools(!on || ToolsTaken);
            if (toolReplicas) toolReplicas.gameObject.SetActive(on && !ToolsTaken);
        }

        void ShowTools(bool on)
        {
            if (game.scanner) game.scanner.gameObject.SetActive(on);
            if (game.bioTool) game.bioTool.gameObject.SetActive(on);
        }

        void Update()
        {
            if (!Running || !labRoot || !labRoot.gameObject.activeSelf) return;
            float t = Time.time;
            if (toolReplicas) { toolReplicas.Rotate(Vector3.up, 30f * Time.deltaTime, Space.World); toolReplicas.localPosition = new Vector3(toolReplicas.localPosition.x, 1.2f + 0.04f * Mathf.Sin(t * 1.6f), toolReplicas.localPosition.z); }
            if (patientMap) patientMap.Rotate(Vector3.up, 12f * Time.deltaTime, Space.Self);
            if (capsuleRings) capsuleRings.Rotate(Vector3.up, 25f * Time.deltaTime, Space.Self);
            if (monitorText && game.rig && game.rig.head)
            {
                // the vitals panel turns to whoever looks at it (no mirrored text from behind)
                var c = monitorText.canvas.transform; Vector3 d = c.position - game.Head; d.y = 0;
                if (d.sqrMagnitude > 0.01f) c.rotation = Quaternion.Slerp(c.rotation, Quaternion.LookRotation(d), 1f - Mathf.Exp(-Time.unscaledDeltaTime * 3f));
            }
            if (monitorText && Time.frameCount % 10 == 0)
            {
                var o = game.organism;
                monitorText.text = $"ПАЦИЕНТ · ПОКАЗАТЕЛИ\n\nПульс   {Mathf.RoundToInt(o.heartRate)} уд/мин\nДавление   {o.PressureText}\nКислород   {Mathf.RoundToInt(o.spo2)} %\nТемпература   {o.temperature:0.0} °C";
            }
        }

        // ---------------- the prologue -------------------------------------------------------
        protected override IEnumerator Run()
        {
            ResetProps();
            game.organism.ResetState();
            game.scanner.locked = true;
            LabMode(true);
            game.swimmer.Teleport(labRoot.TransformPoint(startLocal), labRoot.forward);
            game.rig.Fade(Color.black, 1f, 50f); game.rig.FadeTo(Color.black, 0f, 0.8f);
            Prof.StandAt(startBeat <= 0 || startBeat == 2 ? profIntro : startBeat == 1 ? profTools : profCapsule);
            if (startBeat <= 0) yield return Intro();
            if (startBeat <= 1) yield return Tools(); else TakeTools(false);
            if (startBeat <= 2) yield return ScanPatient(); else game.codex.Discover("internal");
            yield return Shrink();
        }

        IEnumerator Intro()
        {
            Beat = 0;
            Hud.Title("ЛАБОРАТОРИЯ", "пролог · путешествие начинается здесь", 3.5f);
            Prof.Appear(true); Prof.StandAt(profIntro);
            yield return Wait(2f);
            yield return Say("Добро пожаловать в лабораторию! Наш пациент — живой организм. Тебе предстоит отправиться внутрь его артерии.", Mood.Smile);
            yield return Say("Моё правило: сначала наблюдаем. Потом делаем вывод.", Mood.Neutral);
        }

        IEnumerator Tools()
        {
            Beat = 1;
            Prof.StandAt(profTools);
            Objective("Возьми приборы со стола");
            Prompt("Подойди к столу и нажми хват (grip)", "Подойди к столу (W A S D) и нажми E");
            while (!ToolsTaken)
            {
                float d = Vector3.Distance(Flat(game.Head), Flat(toolReplicas.position));
                Hud.SetCompass(Vector3.Angle(game.rig.head.transform.forward, toolReplicas.position - game.Head) > 30f ? toolReplicas.position : (Vector3?)null, "приборы");
                if ((d < 2.2f && game.input.AnchorDown) || autoplay) TakeTools(true);
                else if (game.input.AnchorDown) Hud.Toast("Подойди ближе к столу", Bvr2Style.TextDim, 1.2f);
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            yield return Say(C("В левой руке — сканер: держи курок на объекте, и он расскажет, что это. В правой — BioTool, у него будут разные режимы.", "Сканер — правая кнопка мыши или F: держи на объекте, и он расскажет, что это. BioTool — левая кнопка, у него будут разные режимы."), Mood.Smile);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        void TakeTools(bool fx)
        {
            ToolsTaken = true;
            if (toolReplicas) { if (fx) game.fx.Burst(toolReplicas.position, Bvr2Style.Ally, 30, 2f, 0.04f, 0.6f); toolReplicas.gameObject.SetActive(false); }
            ShowTools(true);
            if (fx) { game.sound.Play("SFX_Capture_Lock", 0.9f); Bvr2Haptics.Both(0.5f, 0.12f); Hud.Toast("+ СКАНЕР · + BIOTOOL", Bvr2Style.Ally, 2.2f); }
        }

        IEnumerator ScanPatient()
        {
            Beat = 2;
            Prof.StandAt(profIntro);
            game.scanner.locked = false; game.scanner.allowPlasma = false;
            Objective("Отсканируй голограмму пациента");
            Prompt("Левый курок — держать на голограмме", "ПКМ / F — держать на голограмме");
            while (!game.codex.Has("internal"))
            {
                Hud.SetCompass(Vector3.Angle(game.rig.head.transform.forward, patientMap.position - game.Head) > 30f ? patientMap.position : (Vector3?)null, "пациент");
                if (autoplay) game.scanner.ForceScan("internal", patientMap.position);
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            yield return WaitKnowledge();
            if (monitor) game.fx.Shock(monitor.position + Vector3.up * 1.7f, game.Head - monitor.position, Bvr2Style.Good, 0.2f, 1.2f, 0.6f);
            game.sound.Play("SFX_Success_Step", 0.8f);
            Hud.Toast("ИСХОДНЫЕ ПОКАЗАТЕЛИ ЗАПИСАНЫ", Bvr2Style.Good, 2.6f);
            yield return Say("Исходные показатели записаны: пульс 72, давление 120 на 80, кислород 98 процентов, температура 36,6. В конце пути сравним.", Mood.Neutral);
        }

        IEnumerator Shrink()
        {
            Beat = 3;
            Prof.StandAt(profCapsule);
            Objective("Встань в капсулу уменьшения");
            Prompt("Иди в светящийся круг", "Иди в светящийся круг (W A S D)");
            yield return Say("Теперь — в капсулу. Мы уменьшимся в десятки тысяч раз, до размеров клетки.", Mood.Smile);
            while (Vector3.Distance(Flat(game.Head), Flat(capsule.position)) > 0.9f)
            {
                Hud.SetCompass(capsule.position + Vector3.up, "капсула");
                if (autoplay) game.swimmer.Teleport(capsule.position + Vector3.up * 1.55f, labRoot.forward);
                yield return null;
            }
            Hud.SetCompass(null, null); NoPrompt();
            game.swimmer.frozen = true;
            Objective("Уменьшение");
            // countdown, the rings spin up
            for (int i = 3; i >= 1; i--)
            {
                Hud.Title(i.ToString(), "уменьшение", 0.9f);
                game.sound.Play("UI_Panel", 0.9f, 1f + (3 - i) * 0.15f);
                Bvr2Haptics.Both(0.3f, 0.08f);
                for (float t = 0; t < (autoplay ? 0.05f : 1f); t += Time.unscaledDeltaTime) { if (capsuleRings) capsuleRings.Rotate(Vector3.up, (200f + (3 - i) * 200f) * Time.unscaledDeltaTime, Space.Self); yield return null; }
            }
            // the lab grows around you: you are shrinking
            game.sound.Play("BVR2_Sync", 1f, 0.8f);
            Vector3 pivot = game.Head; Vector3 start = labRoot.position;
            float dur = autoplay ? 0.3f : 2.8f;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                float k = t / dur, scale = Mathf.Pow(60f, k * k);
                labRoot.localScale = Vector3.one * scale;
                labRoot.position = pivot + (start - pivot) * scale; // scaled about the head: the floor drops away
                if (k > 0.75f) game.rig.Fade(Color.white, (k - 0.75f) / 0.25f, 50f);
                yield return null;
            }
            // dive: skin → fat → the artery wall → the lumen
            LabMode(false); game.blood.enabled = false;
            game.swimmer.roomMode = false; game.swimmer.frozen = true;
            game.rig.FadeTo(Color.white, 0f, 3f);
            yield return Dive();
            // into the bloodstream: chapter 1 starts here
            game.swimmer.frozen = false;
            game.blood.enabled = true;
            var v = game.vessel; game.swimmer.Teleport(v.Centre(arriveS), v.Tangent(arriveS));
            game.rig.Fade(Color.white, 1f, 50f); game.rig.FadeTo(Color.white, 0f, 1.2f);
            Finished = true;
            Objective("");
            if (autoplay) yield break;
            onNextChapter?.Invoke();
        }

        /// <summary>Debug: build the dive layers in front of the camera and leave them still.</summary>
        public void DebugDiveStill(float distance) { StartCoroutine(Dive(distance, true)); }

        IEnumerator Dive() => Dive(10f, false);

        IEnumerator Dive(float firstAt, bool still)
        {
            var head = game.rig.head.transform;
            Vector3 f = head.forward;
            string[] names = { "КОЖА", "ПОДКОЖНАЯ ЖИРОВАЯ КЛЕТЧАТКА", "СТЕНКА АРТЕРИИ" };
            string[] subs = { "эпидермис и дерма", "запас жира, теплоизоляция", "три оболочки — и мы внутри" };
            Material[] mats = { skinMaterial, fatMaterial, wallMaterial };
            plates.Clear();
            // each layer: a wide sheet plus its own cells in front of it — flat skin cells, round fat cells, long wall cells
            Vector3[] cellShape = { new Vector3(0.9f, 0.9f, 0.12f), new Vector3(1.3f, 1.3f, 1.1f), new Vector3(1.6f, 0.45f, 0.18f) };
            var mpb = new MaterialPropertyBlock();
            for (int i = 0; i < 3; i++)
            {
                var layer = new GameObject("Dive Layer " + names[i]).transform; layer.position = head.position + f * (firstAt + i * 9f); layer.rotation = Quaternion.LookRotation(-f);
                var sheet = new GameObject("Sheet"); sheet.transform.SetParent(layer, false); sheet.transform.localScale = new Vector3(30f, 30f, 0.3f);
                sheet.AddComponent<MeshFilter>().sharedMesh = plateMesh; var r = sheet.AddComponent<MeshRenderer>(); r.sharedMaterial = mats[i]; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var rnd = new System.Random(11 + i);
                for (int c = 0; c < 70; c++)
                {
                    var cell = new GameObject("Cell"); cell.transform.SetParent(layer, false);
                    float a = (float)rnd.NextDouble() * Mathf.PI * 2f, rr = Mathf.Sqrt((float)rnd.NextDouble()) * 9f;
                    cell.transform.localPosition = new Vector3(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr, 0.25f + (float)rnd.NextDouble() * 0.4f);
                    cell.transform.localRotation = Quaternion.Euler(0, 0, (float)rnd.NextDouble() * (i == 2 ? 20f : 360f));
                    cell.transform.localScale = cellShape[i] * (0.7f + 0.6f * (float)rnd.NextDouble());
                    cell.AddComponent<MeshFilter>().sharedMesh = plateMesh; var cr = cell.AddComponent<MeshRenderer>(); cr.sharedMaterial = mats[i]; cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mpb.Clear(); mpb.SetColor("_BaseColor", mats[i].GetColor("_BaseColor") * (0.85f + 0.3f * (float)rnd.NextDouble())); cr.SetPropertyBlock(mpb);
                }
                plates.Add(layer);
            }
            if (still) yield break;
            // slow at first (the skin is seen clearly), then faster and faster
            int shown = -1;
            for (float t = 0; t < (autoplay ? 0.5f : 5f); t += Time.unscaledDeltaTime)
            {
                float speed = autoplay ? 80f : 4f + 1.6f * t;
                for (int i = 0; i < plates.Count; i++)
                {
                    if (!plates[i]) continue;
                    plates[i].position -= f * speed * Time.unscaledDeltaTime;
                    float ahead = Vector3.Dot(plates[i].position - head.position, f);
                    if (ahead < 9f && shown < i) { shown = i; Hud.Title(names[i], subs[i], 1.3f); game.sound.Play("BVR2_Boost", 0.6f, 0.8f + i * 0.15f); Bvr2Haptics.Both(0.25f, 0.06f); }
                    if (ahead < -1f) { Destroy(plates[i].gameObject); plates[i] = null; }
                }
                yield return null;
            }
            foreach (var p in plates) if (p) Destroy(p.gameObject);
            plates.Clear();
        }

        // ---------------- debug hooks -------------------------------------------------------
        public void DebugLookAt(string what)
        {
            Transform t = what == "desk" ? toolReplicas : what == "map" ? patientMap : what == "capsule" ? capsule : what == "monitor" ? monitor : what == "prof" ? game.professor.headPivot : null;
            if (what == "profbody") { game.rig.LookAt(game.professor.transform.position + Vector3.up * 0.95f * game.professor.transform.lossyScale.y); return; }
            if (what == "profhands") { game.rig.LookAt(game.professor.transform.position + Vector3.up * 0.85f * game.professor.transform.lossyScale.y); return; }
            if (t) game.rig.LookAt(t.position);
        }
        public void DebugTake() { TakeTools(true); }
        /// <summary>Tests: walk (teleport) to a lab-local floor point, clamped to the walkable rooms like real walking.</summary>
        public void DebugWalkTo(float x, float z)
        {
            Vector3 p = labRoot.TransformPoint(new Vector3(x, startLocal.y, z));
            if (walkable != null && walkable.Length > 0) { game.swimmer.roomRects = walkable; game.swimmer.roomSpace = labRoot; p = game.swimmer.ClampToRooms(p); }
            game.swimmer.Teleport(p, labRoot.forward);
        }
        public void DebugToCapsule() { if (capsule) game.swimmer.Teleport(capsule.position + Vector3.up * 1.55f, labRoot.forward); }
    }
}
