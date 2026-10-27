using System;
using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.V2
{
    /// <summary>
    /// Left-hand scanner. Hold the trigger on a cell, the plasma or a structure: a light cone locks on,
    /// the ring fills, and the first scan opens a knowledge moment. Moving cells are easy to keep in
    /// the cone (sticky lock with a generous angle).
    /// </summary>
    public sealed class Bvr2Scanner : MonoBehaviour
    {
        public Bvr2Rig rig; public Bvr2Input input; public Bvr2Blood blood; public Bvr2Codex codex; public Bvr2Audio audioDirector; public Bvr2Fx fx;
        public Transform tip; public Renderer cone; public Image ring; public Text screenText;
        public float range = 22f, cellCone = 5.5f;
        public bool allowPlasma;
        public bool locked; // disabled by the chapter while not yet introduced
        /// <summary>Set by <see cref="Bvr2Antibodies"/> while the trigger is spraying antibodies.</summary>
        public bool suspended;

        public enum Kind { None, Cell, Thing, Plasma }
        public Kind TargetKind { get; private set; }
        public string TargetId { get; private set; } = "";
        public float Progress { get; private set; }
        public Vector3 TargetPoint { get; private set; }
        /// <summary>id, world point, first time.</summary>
        public event Action<string, Vector3, bool> OnScan;

        int cellIndex = -1; Bvr2Scannable thing; float plasmaHold; MaterialPropertyBlock mpb;

        void Awake() { mpb = new MaterialPropertyBlock(); }

        public static string CellId(CellKind k) => k == CellKind.RedCell ? "rbc" : k == CellKind.WhiteCell ? "wbc" : "platelet";

        void Update()
        {
            var hand = rig ? rig.leftHand : null; if (!hand) return;
            Vector3 origin = tip ? tip.position : hand.position; Vector3 dir = hand.forward;
            bool held = input && input.Scan && !locked && !suspended;
            if (!held) { Clear(); return; }
            var ray = new Ray(origin, dir);
            // keep a moving cell locked while it stays inside a wider cone
            if (TargetKind == Kind.Cell && cellIndex >= 0 && !blood.IsHidden(cellIndex))
            {
                Vector3 to = blood.PositionOf(cellIndex) - origin;
                if (Vector3.Angle(dir, to) > 12f || to.magnitude > range * 1.2f) SetNone();
            }
            else if (TargetKind == Kind.Thing && thing)
            {
                if (!Physics.Raycast(ray, out var hit, range * 1.3f) || hit.collider.GetComponentInParent<Bvr2Scannable>() != thing) SetNone();
            }
            if (TargetKind == Kind.None || TargetKind == Kind.Plasma)
            {
                if (Physics.Raycast(ray, out var hit, range) && hit.collider.GetComponentInParent<Bvr2Scannable>() is Bvr2Scannable s && s.scannable)
                { if (thing != s) { thing = s; cellIndex = -1; TargetKind = Kind.Thing; TargetId = s.codexId; Progress = 0; } }
                else if (blood && blood.Pick(ray, range, cellCone, out int idx, out var kind, out _))
                { cellIndex = idx; thing = null; TargetKind = Kind.Cell; TargetId = CellId(kind); Progress = 0; }
                else if (allowPlasma)
                {
                    if (TargetKind != Kind.Plasma) { TargetKind = Kind.Plasma; TargetId = "plasma"; Progress = 0; }
                }
                else SetNone();
            }
            float seconds = TargetKind == Kind.Cell ? 0.85f : TargetKind == Kind.Thing ? (thing ? thing.scanSeconds : 1.1f) : 1.6f;
            if (TargetKind != Kind.None)
            {
                TargetPoint = TargetKind == Kind.Cell ? blood.PositionOf(cellIndex) : TargetKind == Kind.Thing ? thing.transform.position : origin + dir * 3.5f;
                if (TargetKind == Kind.Cell) blood.Highlight(cellIndex, 1f);
                Progress += Time.unscaledDeltaTime / seconds;
                if (Progress >= 1f) Complete();
            }
            if (audioDirector) audioDirector.ScanLevel = TargetKind == Kind.None ? 0.01f : Mathf.Max(0.01f, Progress);
            Visuals(origin, dir);
        }

        void Complete()
        {
            string id = TargetId; Vector3 p = TargetPoint;
            bool first = codex && !codex.Has(id);
            if (TargetKind == Kind.Thing && thing) thing.Fire();
            Bvr2Haptics.Pulse(true, 0.6f, 0.12f);
            fx?.Burst(p, first ? Bvr2Style.Knowledge : Bvr2Style.Ally, first ? 34 : 14, 2.4f, 0.07f, 0.8f);
            audioDirector?.Play("SFX_Scan_Complete", 0.9f);
            OnScan?.Invoke(id, p, first);
            SetNone(); Progress = 0;
            locked = true; Invoke(nameof(Unlock), 0.6f); // short cooldown so one press is one scan
        }

        void Unlock() { locked = false; }

        void SetNone() { TargetKind = Kind.None; TargetId = ""; cellIndex = -1; thing = null; Progress = 0; }

        void Clear()
        {
            if (TargetKind != Kind.None) SetNone();
            if (audioDirector) audioDirector.ScanLevel = 0;
            if (cone) cone.enabled = false;
            if (ring) ring.fillAmount = 0;
            if (screenText) screenText.text = locked ? "" : "СКАНЕР";
        }

        void Visuals(Vector3 origin, Vector3 dir)
        {
            if (cone)
            {
                cone.enabled = true;
                float d = TargetKind == Kind.None ? 6f : Vector3.Distance(origin, TargetPoint);
                cone.transform.position = origin;
                cone.transform.rotation = Quaternion.LookRotation(TargetKind == Kind.None || TargetKind == Kind.Plasma ? dir : (TargetPoint - origin).normalized);
                float w = TargetKind == Kind.Cell ? 0.55f : TargetKind == Kind.Thing ? 1.1f : 0.9f;
                cone.transform.localScale = new Vector3(w, w, d);
                var c = TargetKind == Kind.None ? new Color(0.45f, 0.93f, 1f, 0.15f) : Color.Lerp(Bvr2Style.Ally, Bvr2Style.Knowledge, Progress);
                c.a = TargetKind == Kind.None ? 0.15f : 0.35f + 0.3f * Progress;
                cone.GetPropertyBlock(mpb); mpb.SetColor("_Color", c); cone.SetPropertyBlock(mpb);
            }
            if (ring) ring.fillAmount = Progress;
            if (screenText)
            {
                string name = TargetKind == Kind.None ? "наведи на объект" : NameOf(TargetId);
                screenText.text = name;
            }
        }

        public string NameOf(string id)
        {
            var e = codex ? codex.Get(id) : null;
            if (e == null) return id;
            return codex.Has(id) ? e.title : "? " + e.title.Substring(0, 1) + "…";
        }

        /// <summary>Test / autopilot hook: complete a scan of an id at a point.</summary>
        public void ForceScan(string id, Vector3 point)
        {
            TargetId = id; TargetPoint = point; TargetKind = Kind.Thing;
            bool first = codex && !codex.Has(id);
            OnScan?.Invoke(id, point, first); SetNone();
        }
    }
}
