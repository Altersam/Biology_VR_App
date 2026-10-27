using UnityEngine;
using UnityEngine.XR;

namespace BiologyVR.V2
{
    /// <summary>World time for the bloodstream. The player's head and hands always run in real
    /// time; cells, flow and the heart can be slowed for scan moments without touching Time.timeScale.</summary>
    public static class Bvr2Time
    {
        public static float Scale { get; private set; } = 1f;
        static float target = 1f, holdUntil;
        public static float Delta => Time.deltaTime * Scale;
        public static float Now { get; private set; }

        /// <summary>Slow the world to <paramref name="scale"/> for <paramref name="seconds"/> of real time.</summary>
        public static void SlowMo(float scale, float seconds) { target = scale; holdUntil = Time.unscaledTime + seconds; }
        public static void Release() { holdUntil = 0; }
        public static void Reset() { Scale = target = 1f; holdUntil = 0; Now = 0; }

        public static void Tick()
        {
            float goal = Time.unscaledTime < holdUntil ? target : 1f;
            Scale = Mathf.MoveTowards(Scale, goal, Time.unscaledDeltaTime * (goal < Scale ? 6f : 1.6f));
            Now += Delta;
        }
    }

    public static class Bvr2Style
    {
        // One colour language for the whole game (see concept: white/cyan = ours, green/magenta = threats,
        // orange = damage, gold = knowledge).
        public static readonly Color Ally = new Color(0.45f, 0.93f, 1f);
        public static readonly Color AllySoft = new Color(0.62f, 0.86f, 0.95f);
        public static readonly Color Knowledge = new Color(1f, 0.8f, 0.32f);
        public static readonly Color Danger = new Color(1f, 0.52f, 0.18f);
        public static readonly Color Threat = new Color(0.68f, 1f, 0.25f);
        public static readonly Color Good = new Color(0.45f, 1f, 0.6f);
        public static readonly Color Text = new Color(0.96f, 0.97f, 1f);
        public static readonly Color TextDim = new Color(0.72f, 0.8f, 0.86f);
        public static readonly Color Panel = new Color(0.02f, 0.05f, 0.08f, 0.86f);
        public static readonly Color PanelEdge = new Color(0.45f, 0.93f, 1f, 0.55f);
        public static readonly Color Lumen = new Color(0.16f, 0.02f, 0.04f);
    }

    public static class Bvr2Haptics
    {
        public static bool Enabled = true;
        public static void Pulse(bool left, float amplitude, float seconds)
        {
            if (!Enabled || !XRSettings.isDeviceActive) return;
            var device = InputDevices.GetDeviceAtXRNode(left ? XRNode.LeftHand : XRNode.RightHand);
            if (device.isValid) device.SendHapticImpulse(0, Mathf.Clamp01(amplitude), seconds);
        }
        public static void Both(float amplitude, float seconds) { Pulse(true, amplitude, seconds); Pulse(false, amplitude, seconds); }
    }

    public static class Bvr2Math
    {
        public static float Damp(float current, float target, float lambda, float dt) => Mathf.Lerp(current, target, 1f - Mathf.Exp(-lambda * dt));
        public static Vector3 Damp(Vector3 current, Vector3 target, float lambda, float dt) => Vector3.Lerp(current, target, 1f - Mathf.Exp(-lambda * dt));
        public static Quaternion Damp(Quaternion current, Quaternion target, float lambda, float dt) => Quaternion.Slerp(current, target, 1f - Mathf.Exp(-lambda * dt));
        public static float Hash(int n) { n = (n << 13) ^ n; return 1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f * 0.5f; }
        /// <summary>Systolic pulse shape for a heart phase (cycles); same curve as the shaders.</summary>
        public static float PulseShape(float phase)
        {
            float x = phase - Mathf.Floor(phase);
            return Mathf.SmoothStep(0, 1, Mathf.Clamp01(x / 0.07f)) * Mathf.Exp(-x * 5.5f);
        }
    }
}
