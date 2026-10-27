using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// Left hand, after the antigen is known: holding the scanner trigger on viruses sprays antibodies.
    /// They stick to the spikes (antigen) like a key in a lock and mark the virus for the phagocytes
    /// (opsonization). With no virus in the cone the trigger scans as usual.
    /// </summary>
    [DefaultExecutionOrder(-15)]
    public sealed class Bvr2Antibodies : MonoBehaviour
    {
        public Bvr2Rig rig; public Bvr2Input input; public Bvr2Scanner scanner; public Bvr2Immune immune; public Bvr2Fx fx; public Bvr2Audio audioDirector;
        public Transform tip;
        public bool unlocked;
        [Tooltip("Chapter 5, after the key: antibodies made for this exact epitope — one spray neutralises a virion, armoured ones too.")]
        public bool specific;
        public float cone = 15f, range = 11f, tagSeconds = 0.6f;
        public bool Spraying { get; private set; }
        public int Tagged { get; private set; }
        float puff;

        void Update()
        {
            Spraying = false;
            if (scanner) scanner.suspended = false;
            if (!unlocked || !input || !input.Scan || !rig || !rig.leftHand || !immune) return;
            var hand = rig.leftHand; Vector3 origin = tip ? tip.position : hand.position, dir = hand.forward;
            float dt = Time.deltaTime; bool any = false;
            foreach (var v in immune.viruses)
            {
                if (!v.gameObject.activeSelf || !v.Alive || v.Tagged) continue;
                Vector3 to = v.transform.position - origin; float d = to.magnitude;
                if (d > range || Vector3.Angle(dir, to) > cone + Mathf.Atan2(v.radius, Mathf.Max(d, 0.1f)) * Mathf.Rad2Deg) continue;
                any = true;
                if (specific) { if (!v.Neutralized) { v.Neutralize(); Tagged++; } continue; }
                if (v.armored) { immune.Glance(v); continue; } // ordinary antibodies slide off the armour
                bool was = v.Tagged;
                v.AddTag(dt / tagSeconds * Mathf.Lerp(1.4f, 0.7f, d / range));
                if (!was && v.Tagged) Tagged++;
            }
            if (!any) return;
            Spraying = true; if (scanner) scanner.suspended = true;
            puff += dt;
            if (puff > 0.05f)
            {
                puff = 0;
                fx?.Burst(origin + dir * 0.1f, specific ? new Color(1f, 0.85f, 0.4f) : new Color(0.55f, 0.95f, 1f), 3, 0f, 0.035f, 0.6f, (dir + Random.insideUnitSphere * 0.25f).normalized * 9f);
            }
            if (Time.frameCount % 10 == 0) audioDirector?.Play("BVR2_Spray", 0.35f, Random.Range(0.95f, 1.05f));
            if (Time.frameCount % 6 == 0) Bvr2Haptics.Pulse(true, 0.15f, 0.03f);
        }
    }
}
