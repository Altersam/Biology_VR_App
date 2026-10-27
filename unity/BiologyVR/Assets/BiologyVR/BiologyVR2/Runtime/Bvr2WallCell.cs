using System;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// One endothelial cell of the infection focus (chapter 5). An infected cell is swollen, buds new virions
    /// and shows "flags" — pieces of viral proteins on its surface (MHC I). A T-killer that reaches an infected
    /// cell makes it die by apoptosis: it shrinks and falls apart into vesicles. A T-killer sent to a healthy
    /// cell damages the wall.
    /// </summary>
    public sealed class Bvr2WallCell : MonoBehaviour
    {
        public enum State { Alive, Dying, Dead, Damaged }
        public Renderer body; public Transform flags;
        public bool infected;
        public float s, angleDeg;
        public State Current { get; private set; } = State.Alive;
        public bool InfectedAlive => infected && Current == State.Alive;
        public bool Targeted { get; set; }
        public bool releasing;
        [Tooltip("Infected cells show their flags (pieces of viral proteins) once the T-killers are in play.")] public bool flagsShown;
        public float releaseEvery = 10f;
        public event Action<Bvr2WallCell> OnApoptosis, OnDamaged;

        Bvr2Vessel vessel; Bvr2Immune immune; Bvr2Fx fx; Bvr2Audio audioDirector;
        float nextRelease, dieT, phase; Vector3 baseScale = new Vector3(1.5f, 0.7f, 1.5f); MaterialPropertyBlock mpb;
        static readonly Color Healthy = new Color(1f, 0.62f, 0.6f), Sick = new Color(0.72f, 0.5f, 0.82f), Hurt = new Color(0.45f, 0.18f, 0.16f);

        void Awake() { mpb = new MaterialPropertyBlock(); phase = UnityEngine.Random.value * 10f; }

        public void Setup(Bvr2Vessel v, Bvr2Immune imm, Bvr2Fx f, Bvr2Audio a, float sPos, float angle, bool isInfected)
        {
            vessel = v; immune = imm; fx = f; audioDirector = a; s = sPos; angleDeg = angle; infected = isInfected;
            Current = State.Alive; Targeted = false; releasing = false; dieT = 0;
            Vector3 p = v.SurfacePoint(s, angleDeg, -0.1f);
            transform.position = p; transform.rotation = Quaternion.LookRotation(v.Tangent(s), (v.Centre(s) - p).normalized);
            baseScale = infected ? new Vector3(1.25f, 0.8f, 1.25f) : new Vector3(1.05f, 0.42f, 1.05f);
            transform.localScale = baseScale;
            if (body) body.gameObject.SetActive(true);
            flagsShown = false; if (flags) flags.gameObject.SetActive(false);
            nextRelease = Time.time + UnityEngine.Random.Range(2f, releaseEvery);
            gameObject.SetActive(true);
        }

        /// <summary>A T-killer has reached this cell.</summary>
        public void Hit()
        {
            if (Current != State.Alive) return;
            if (infected)
            {
                Current = State.Dying; dieT = 0;
                audioDirector?.PlayAt("BVR2_Apoptosis", transform.position, 0.9f);
                OnApoptosis?.Invoke(this);
            }
            else
            {
                Current = State.Damaged;
                fx?.Burst(transform.position, Bvr2Style.Danger, 26, 2f, 0.05f, 0.7f);
                fx?.Shock(transform.position, transform.up, Bvr2Style.Danger, 0.2f, 2f, 0.6f);
                audioDirector?.PlayAt("BVR2_Crack", transform.position, 0.8f);
                OnDamaged?.Invoke(this);
            }
        }

        void Update()
        {
            float dt = Bvr2Time.Delta, t = Time.time + phase;
            switch (Current)
            {
                case State.Alive:
                {
                    float swell = infected ? 1f + 0.06f * Mathf.Sin(t * 3.1f) : 1f + 0.02f * Mathf.Sin(t * 1.3f);
                    transform.localScale = baseScale * swell;
                    // an infected cell buds new virions into the lumen
                    if (infected && releasing && immune && Time.time >= nextRelease && immune.AliveViruses < 16)
                    {
                        nextRelease = Time.time + releaseEvery * UnityEngine.Random.Range(0.8f, 1.2f);
                        // the new virion buds off into the lumen first, then looks for a cell to infect
                        var v = immune.Spawn(s + UnityEngine.Random.Range(-0.5f, 0.5f), angleDeg + UnityEngine.Random.Range(-6f, 6f), 0.45f, 0.75f, 7f);
                        if (v) fx?.Burst(v.transform.position, new Color(0.65f, 1f, 0.35f), 12, 1.4f, 0.04f, 0.5f);
                    }
                    Tint(infected ? Sick : Healthy, infected ? 0.25f + 0.1f * Mathf.Sin(t * 3.1f) : 0.05f);
                    break;
                }
                case State.Dying:
                {
                    // apoptosis: the cell shrinks, wrinkles and falls apart into vesicles
                    dieT += dt;
                    float k = Mathf.Clamp01(dieT / 1.6f);
                    transform.localScale = baseScale * Mathf.Lerp(1f, 0.3f, k) * (1f + 0.08f * Mathf.Sin(dieT * 30f) * (1f - k));
                    if (flags) flags.gameObject.SetActive(flagsShown && k < 0.5f);
                    Tint(Color.Lerp(Sick, new Color(0.9f, 0.85f, 1f), k), 0.3f * (1f - k));
                    if (k >= 1f)
                    {
                        Current = State.Dead;
                        fx?.Burst(transform.position, new Color(0.95f, 0.85f, 1f), 22, 1.1f, 0.08f, 1f);
                        if (immune) for (int i = 0; i < 3; i++) immune.SpawnDebris(transform.position + transform.up * 0.5f + UnityEngine.Random.insideUnitSphere * 0.5f);
                        if (body) body.gameObject.SetActive(false);
                    }
                    break;
                }
                case State.Damaged:
                    transform.localScale = baseScale * 0.85f;
                    Tint(Hurt, 0.15f + 0.1f * Mathf.Sin(t * 8f));
                    break;
            }
            if (flags && Current == State.Alive) flags.gameObject.SetActive(flagsShown && infected);
            if (flags && flags.gameObject.activeSelf)
                for (int i = 0; i < flags.childCount; i++) flags.GetChild(i).localRotation = Quaternion.Euler(Mathf.Sin(t * 2f + i) * 12f, i * 72f, 0);
        }

        void Tint(Color c, float emission)
        {
            if (!body) return;
            mpb.Clear(); mpb.SetColor("_BaseColor", c); mpb.SetColor("_Emission", c * emission); body.SetPropertyBlock(mpb);
        }
    }
}
