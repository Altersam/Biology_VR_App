using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// BioTool ability "Сигнал": point and pull the trigger — the ally phagocytes swim there
    /// (in the body this is done by signalling molecules: chemotaxis). On the heartbeat the signal is
    /// stronger and the cells hurry.
    /// </summary>
    public sealed class Bvr2Signal : MonoBehaviour
    {
        public Bvr2Rig rig; public Bvr2Input input; public Bvr2Vessel vessel; public Bvr2Immune immune; public Bvr2Fx fx; public Bvr2Hud hud;
        public Transform tip;
        public bool active;
        public float range = 30f, cone = 7f, cooldown = 0.35f;
        public int Signals { get; private set; }
        public int OnBeatSignals { get; private set; }
        float next;

        void Update()
        {
            if (!active || !input || !rig || !rig.rightHand || !immune) return;
            if (input.ToolDown && Time.time >= next) Send();
        }

        public Vector3 Send(Vector3? direction = null)
        {
            var hand = rig.rightHand; Vector3 origin = tip ? tip.position : hand.position, dir = direction ?? hand.forward;
            next = Time.time + cooldown;
            Vector3 point = origin + dir * 12f;
            // aimed at a virus? signal right there
            Bvr2Virus best = null; float bestA = cone;
            foreach (var v in immune.viruses)
            {
                if (!v.gameObject.activeSelf || !v.Alive) continue;
                Vector3 to = v.transform.position - origin; if (to.magnitude > range) continue;
                float a = Vector3.Angle(dir, to); if (a < bestA) { bestA = a; best = v; }
            }
            if (best) point = best.transform.position;
            else if (vessel && vessel.RaycastWall(new Ray(origin, dir), range, out var hit, out _, out _)) point = hit - dir * 1.5f;
            bool beat = immune.OnTheBeat;
            Signals++; if (beat) OnBeatSignals++;
            immune.Signal(point, beat);
            var beam = fx ? fx.GetBeam() : null;
            if (beam != null) { Bvr2Fx.ShapeBeam(beam, origin, dir, point, beat ? new Color(1f, 0.85f, 0.35f, 0.8f) : new Color(0.45f, 0.93f, 1f, 0.7f), 0.02f, 0.02f); StartCoroutine(Release(beam)); }
            if (beat) hud?.Toast("В РИТМ!", Bvr2Style.Knowledge, 0.7f);
            Bvr2Haptics.Pulse(false, beat ? 0.6f : 0.3f, 0.06f);
            return point;
        }

        System.Collections.IEnumerator Release(Bvr2Fx.Beam b) { yield return new WaitForSeconds(0.18f); fx.ReleaseBeam(b); }
    }
}
