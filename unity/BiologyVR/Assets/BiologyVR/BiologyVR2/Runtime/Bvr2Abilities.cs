using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.V2
{
    /// <summary>
    /// BioTool abilities on the right hand: "Притяжение" (attract) and, from chapter 2, "Импульс".
    /// B (right controller) or Q switches; the scenario can force a mode. The tool's little screen
    /// and colour show which one is active.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public sealed class Bvr2Abilities : MonoBehaviour
    {
        public enum Mode { Attract, Impulse, Signal, Factor }
        public Bvr2Input input; public Bvr2BioTool attract; public Bvr2Impulse impulse; public Bvr2Signal signal; public Bvr2FactorTool factor; public Bvr2Hud hud; public Bvr2Audio audioDirector;
        public Text screenText; public Renderer[] tint;
        public bool impulseUnlocked, signalUnlocked, factorUnlocked;
        public Mode Current { get; private set; } = Mode.Attract;
        MaterialPropertyBlock mpb;

        public static string Name(Mode m) => m == Mode.Impulse ? "ИМПУЛЬС" : m == Mode.Signal ? "СИГНАЛ" : m == Mode.Factor ? "ФАКТОР" : "ПРИТЯЖЕНИЕ";
        public static Color ColorOf(Mode m) => m == Mode.Impulse ? new Color(1f, 0.78f, 0.25f) : m == Mode.Signal ? new Color(0.7f, 1f, 0.45f) : m == Mode.Factor ? Bvr2FactorTool.PaintColor : Bvr2Style.Ally;
        bool Unlocked(Mode m) => m == Mode.Attract || (m == Mode.Impulse && impulseUnlocked) || (m == Mode.Signal && signalUnlocked) || (m == Mode.Factor && factorUnlocked);

        public void SetMode(Mode m, bool announce = true)
        {
            if (!Unlocked(m)) m = Mode.Attract;
            bool changed = m != Current; Current = m;
            if (changed && announce)
            {
                hud?.Toast("BIOTOOL · " + Name(m), ColorOf(m), 1.4f);
                audioDirector?.Play("UI_Panel", 0.6f, m == Mode.Impulse ? 1.2f : m == Mode.Factor ? 1.1f : 0.95f);
                Bvr2Haptics.Pulse(false, 0.25f, 0.05f);
            }
        }

        void Awake() { mpb = new MaterialPropertyBlock(); }

        void Update()
        {
            if (input && input.AbilityDown && (impulseUnlocked || signalUnlocked || factorUnlocked))
            {
                var m = Current;
                for (int i = 0; i < 4; i++) { m = (Mode)(((int)m + 1) % 4); if (Unlocked(m)) break; }
                SetMode(m);
            }
            if (attract) attract.suspended = Current != Mode.Attract;
            if (impulse) impulse.active = Current == Mode.Impulse && impulseUnlocked;
            if (signal) signal.active = Current == Mode.Signal && signalUnlocked;
            if (factor) factor.active = Current == Mode.Factor && factorUnlocked;
            if (screenText) { screenText.text = Name(Current); screenText.color = ColorOf(Current); }
            if (tint != null && tint.Length > 0)
            {
                mpb.SetColor("_Color", ColorOf(Current));
                foreach (var r in tint) if (r) r.SetPropertyBlock(mpb);
            }
        }
    }
}
