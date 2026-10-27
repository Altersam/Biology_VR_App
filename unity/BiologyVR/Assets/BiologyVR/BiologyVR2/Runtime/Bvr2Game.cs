using System.Collections;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>Composition root of the new scene: holds references, wires knowledge moments
    /// (scan → codex → diorama → professor line) and exposes small helpers for chapters and tests.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class Bvr2Game : MonoBehaviour
    {
        public static Bvr2Game Instance { get; private set; }
        public Bvr2Organism organism; public Bvr2Vessel vessel; public Bvr2Environment environment; public Bvr2Blood blood;
        public Bvr2Input input; public Bvr2Rig rig; public Bvr2Swimmer swimmer; public Bvr2Audio sound; public Bvr2Fx fx;
        public Bvr2Hud hud; public Bvr2Professor professor; public Bvr2Codex codex; public Bvr2Diorama diorama;
        public Bvr2Scanner scanner; public Bvr2BioTool bioTool; public Bvr2VascularField field; public Bvr2Question question;
        public Bvr2Impulse impulse; public Bvr2Abilities abilities; public Bvr2Campaign campaign;
        public Bvr2Immune immune; public Bvr2Antibodies antibodies; public Bvr2Signal signal; public Bvr2Sync sync; public Bvr2FactorTool factor;
        public Font font; public Material uiOverlay; public Sprite rounded, ringSprite, dot, glow;
        public Material holoBackdrop; public Mesh quadMesh;
        public bool autoDiorama = true;

        public string LastScanned { get; private set; } = "";
        public int ScanCount { get; private set; }

        void Awake()
        {
            Instance = this;
            Bvr2Time.Reset();
            Bvr2Ui.Font = font; Bvr2Ui.Overlay = uiOverlay; Bvr2Ui.Rounded = rounded; Bvr2Ui.RingSprite = ringSprite; Bvr2Ui.Dot = dot; Bvr2Ui.Glow = glow;
            Bvr2Ui.HoloBackdrop = holoBackdrop; Bvr2Ui.QuadMesh = quadMesh;
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
            Bvr2MobileQuality.Attach(gameObject); // headset builds only
        }

        void Start()
        {
            if (scanner) scanner.OnScan += HandleScan;
        }

        void HandleScan(string id, Vector3 at, bool first)
        {
            LastScanned = id; ScanCount++;
            if (!codex) return;
            var e = codex.Get(id);
            if (e == null) return;
            if (first && codex.Discover(id))
            {
                // the diorama itself announces the new knowledge; a toast would cover it
                if (!(autoDiorama && diorama)) hud?.Toast("+ ЗНАНИЕ · " + e.title.ToUpperInvariant(), Bvr2Style.Knowledge, 2.6f);
                sound?.Play("BVR2_Knowledge", 0.8f);
                if (autoDiorama && diorama) StartCoroutine(Knowledge(e, at));
            }
            else hud?.Toast(e.title, e.color, 1.4f);
        }

        IEnumerator Knowledge(Bvr2Codex.Entry e, Vector3 at)
        {
            while (diorama.Showing) yield return null;
            professor?.Interrupt();
            professor?.Say(e.line, Mood.Smile);
            var show = StartCoroutine(diorama.Show(e, at));
            professor?.Present(diorama.Anchor, diorama.duration, diorama.PresenterSide);
            yield return show;
        }

        /// <summary>Discover an entry without scanning (an observed event) and show it.</summary>
        public void Reveal(string id, Vector3 at)
        {
            var e = codex ? codex.Get(id) : null; if (e == null || !codex.Discover(id)) return;
            if (!diorama) hud?.Toast("+ ЗНАНИЕ · " + e.title.ToUpperInvariant(), Bvr2Style.Knowledge, 2.6f);
            sound?.Play("BVR2_Knowledge", 0.8f);
            StartCoroutine(Knowledge(e, at));
        }

        /// <summary>Stops any knowledge moment in progress (used when a chapter restarts).</summary>
        public void CancelKnowledge() { StopAllCoroutines(); if (diorama) diorama.Cleanup(); }

        public float PlayerS => swimmer ? swimmer.S : 0;
        public Vector3 Head => rig && rig.head ? rig.head.transform.position : transform.position;
        public bool KnowledgeBusy => diorama && diorama.Showing;
    }
}
