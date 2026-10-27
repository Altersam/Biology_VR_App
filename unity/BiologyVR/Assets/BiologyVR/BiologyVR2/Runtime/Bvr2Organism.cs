using System;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// Simplified physiology shared by every system: one source of truth for the wrist
    /// gauges, the heartbeat (sound, light, haptics), the flow speed and fail states.
    /// MAP ≈ cardiac output × resistance; baroreflex speeds the heart when pressure falls;
    /// local conductance of the studied segment ∝ r⁴ (−20 % radius → ×0.41 flow).
    /// </summary>
    public sealed class Bvr2Organism : MonoBehaviour
    {
        [Header("Baseline")]
        public float baseHeartRate = 72f;
        public float baseStrokeVolume = 70f;   // ml
        public float baseMap = 93f;            // mmHg
        public float baroGain = 0.9f;          // bpm per mmHg per second (scaled)

        [Header("Live state (read-only at runtime)")]
        public float heartRate = 72f;
        public float strokeVolume = 70f;
        public float systemicResistance = 1f;  // relative
        public float map = 93f;
        public float systolic = 120f, diastolic = 80f;
        public float spo2 = 98f;
        public float temperature = 36.6f;
        public float viralLoad;                // 0..1: infected cells (chapter 3+)
        [Tooltip("Blood glucose, mmol/l (shown by the chapter 6 sensor; norm 3.3–5.5).")] public float glucose = 4.9f;
        [Tooltip("Blood acidity (chapter 6 sensor; norm 7.35–7.45).")] public float pH = 7.4f;
        [Tooltip("Fever follows the viral load (36.6 °C … ~39.4 °C).")]
        public bool feverFromVirus = true;
        [Tooltip("Blood lost through a wound, litres per minute (chapter 4).")]
        public float leakRate;
        [Tooltip("Litres per minute the body restores once bleeding stops (fluid from tissues, later new blood).")]
        public float refillRate = 0f;
        public float bloodVolume = 5f;         // litres
        /// <summary>Conductance set by the vascular field (r⁴ law), 1 = normal.</summary>
        public float fieldConductance = 1f;
        /// <summary>Conductance left by lesions in this artery: plaque, thrombus, embolus (1 = none).</summary>
        public float lesionConductance = 1f;
        /// <summary>Relative conductance of the player's artery (field × lesions).</summary>
        public float localConductance => fieldConductance * lesionConductance;
        /// <summary>Extra heart drive requested by a scenario (stress, alarm), bpm.</summary>
        public float stressDrive;

        public float HeartPhase { get; private set; }
        public float Pulse { get; private set; }
        public int Beats { get; private set; }
        public float CardiacOutput => heartRate * strokeVolume / 1000f;            // l/min
        /// <summary>Flow through the player's segment relative to normal (1 = 100 %).</summary>
        public float LocalFlow => Mathf.Clamp((map / baseMap) * localConductance, 0f, 2.5f);
        /// <summary>Mean flow speed in the lumen at the reference radius, world m/s.</summary>
        public float MeanSpeed => 2.6f * LocalFlow * (0.82f + 0.36f * Pulse);
        public event Action OnBeat;

        float baseCo;

        void Awake() { baseCo = baseHeartRate * baseStrokeVolume / 1000f; ResetState(); }

        public void ResetState()
        {
            heartRate = baseHeartRate; strokeVolume = baseStrokeVolume; systemicResistance = 1f; fieldConductance = 1f; lesionConductance = 1f;
            stressDrive = 0; spo2 = 98f; temperature = 36.6f; viralLoad = 0; bloodVolume = 5f; leakRate = 0; refillRate = 0; map = baseMap; HeartPhase = 0; Beats = 0;
            Recompute();
        }

        /// <summary>Jump to the steady state for a blood volume (a retry after shock: "the professor gave fluids").</summary>
        public void Settle(float volume)
        {
            bloodVolume = Mathf.Clamp(volume, 1.5f, 5f);
            float v = bloodVolume / 5f;
            strokeVolume = baseStrokeVolume * Mathf.Lerp(0.2f, 1f, v * v * v);
            float localResistance = 1f / Mathf.Max(localConductance, 0.05f);
            float r = systemicResistance * (0.82f + 0.18f * localResistance);
            for (int i = 0; i < 80; i++)
            {
                map = baseMap * (heartRate * strokeVolume / 1000f / baseCo) * r;
                float targetHr = baseHeartRate + (baseMap - map) * baroGain + stressDrive + (temperature - 36.6f) * 10f;
                heartRate = Mathf.Clamp(Mathf.Lerp(heartRate, targetHr, 0.3f), 45f, 175f);
            }
            Recompute();
        }

        void Update()
        {
            float dt = Bvr2Time.Delta;
            if (dt <= 0) return;
            // Local constriction raises total resistance a little (the segment is one of many vessels),
            // so the gauges show "narrower → pressure up, flow here down".
            float localResistance = 1f / Mathf.Max(localConductance, 0.05f);
            float r = systemicResistance * (0.82f + 0.18f * localResistance);
            bloodVolume = Mathf.Clamp(bloodVolume - leakRate * dt / 60f + refillRate * dt / 60f, 1.5f, 5f);
            float volumeFactor = Mathf.Clamp01(bloodVolume / 5f);
            // less blood → the heart fills less → smaller stroke volume (≈ shock after losing a third of the blood)
            strokeVolume = Mathf.Lerp(strokeVolume, baseStrokeVolume * Mathf.Lerp(0.2f, 1f, volumeFactor * volumeFactor * volumeFactor), dt * 0.8f);
            float co = heartRate * strokeVolume / 1000f;
            float targetMap = baseMap * (co / baseCo) * r;
            map = Mathf.Lerp(map, targetMap, dt * 1.6f);
            // Baroreflex: heart speeds up when pressure falls, slows when it rises.
            float targetHr = baseHeartRate + (baseMap - map) * baroGain + stressDrive + (temperature - 36.6f) * 10f;
            heartRate = Mathf.Clamp(Mathf.Lerp(heartRate, targetHr, dt * 0.5f), 45f, 175f);
            // Fever: the body heats up while infected cells release signals (pyrogens).
            if (feverFromVirus) temperature = Mathf.Lerp(temperature, 36.6f + 2.8f * viralLoad, dt * 0.25f);
            // Downstream tissue oxygen: falls when the local flow is poor.
            float spo2Target = LocalFlow < 0.6f ? Mathf.Lerp(86f, 98f, LocalFlow / 0.6f) : 98f;
            spo2 = Mathf.Lerp(spo2, spo2Target, dt * 0.35f);
            Recompute();
            float before = HeartPhase;
            HeartPhase += heartRate / 60f * dt;
            if (Mathf.Floor(HeartPhase) > Mathf.Floor(before)) { Beats++; OnBeat?.Invoke(); }
            Pulse = Bvr2Math.PulseShape(HeartPhase);
        }

        void Recompute()
        {
            float pp = 40f * (strokeVolume / baseStrokeVolume) * Mathf.Lerp(1f, 1.25f, Mathf.Clamp01((map - baseMap) / 40f));
            systolic = map + pp * 2f / 3f; diastolic = map - pp / 3f;
        }

        public string PressureText => $"{Mathf.RoundToInt(systolic)}/{Mathf.RoundToInt(diastolic)}";
    }
}
