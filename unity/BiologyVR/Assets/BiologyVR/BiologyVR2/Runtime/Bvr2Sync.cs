using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// "Синхронизация": for a short time the player becomes a macrophage. The view is seen through a
    /// membrane, the hands turn into pseudopods, and any virus that comes close to a hand (or the head)
    /// is swallowed. The tools are put away meanwhile.
    /// </summary>
    public sealed class Bvr2Sync : MonoBehaviour
    {
        public Bvr2Rig rig; public Bvr2Immune immune; public Bvr2Fx fx; public Bvr2Audio audioDirector; public Bvr2Swimmer swimmer;
        public GameObject[] hideWhileSynced;
        public Mesh blobMesh; public Material blobMaterial;
        public float reachHand = 1.4f, reachHead = 1.1f;
        [Tooltip("Chapter 5: the player becomes a T-killer — faster, kills infected cells (the chapter handles the touch), does not eat virions.")]
        public bool tKiller;
        public bool Active { get; private set; }
        public float TimeLeft { get; private set; }
        public int Eaten { get; private set; }
        Transform blobL, blobR; MaterialPropertyBlock mpb; float maxSwimBefore;

        void Awake() { mpb = new MaterialPropertyBlock(); }

        Transform Blob(string name, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = blobMesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = blobMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; go.transform.localPosition = new Vector3(0, 0, 0.08f); go.transform.localScale = Vector3.one * 0.16f;
            go.SetActive(false); return go.transform;
        }

        public void Begin(float seconds)
        {
            if (!blobL) { blobL = Blob("Pseudopod L", rig.leftHand); blobR = Blob("Pseudopod R", rig.rightHand); }
            Active = true; TimeLeft = seconds; Eaten = 0;
            foreach (var g in hideWhileSynced) if (g) g.SetActive(false);
            blobL.gameObject.SetActive(true); blobR.gameObject.SetActive(true);
            if (swimmer) { maxSwimBefore = swimmer.maxSwim; swimmer.maxSwim *= tKiller ? 1.5f : 0.9f; }
            rig.Fade(Color.white, 0.9f, 50f); rig.FadeTo(Color.white, 0f, 1.2f);
            audioDirector?.Play("BVR2_Sync", 1f);
        }

        public void End()
        {
            if (!Active) return;
            Active = false; TimeLeft = 0;
            foreach (var g in hideWhileSynced) if (g) g.SetActive(true);
            if (blobL) { blobL.gameObject.SetActive(false); blobR.gameObject.SetActive(false); }
            if (swimmer && maxSwimBefore > 0) swimmer.maxSwim = maxSwimBefore;
            rig.SetOverlay(Color.clear, 0f);
            rig.Fade(Color.white, 0.8f, 50f); rig.FadeTo(Color.white, 0f, 1.5f);
        }

        void Update()
        {
            if (!Active) return;
            TimeLeft -= Time.deltaTime;
            float t = Time.time;
            // the membrane around the view breathes
            rig.SetOverlay(tKiller ? new Color(0.55f, 0.65f, 1f, 1f) : new Color(0.85f, 0.9f, 0.95f, 1f), 0.55f + 0.08f * Mathf.Sin(t * 2.2f));
            foreach (var b in new[] { blobL, blobR })
            {
                b.localScale = Vector3.one * 0.16f * (1f + 0.15f * Mathf.Sin(t * 5f + b.GetInstanceID()));
                mpb.Clear(); mpb.SetVector("_Inst", new Vector4(0.6f, tKiller ? 0.6f : 0.2f, 0.8f, 0.3f));
                if (tKiller) mpb.SetColor("_BaseColor", new Color(0.7f, 0.78f, 1f));
                b.GetComponent<Renderer>().SetPropertyBlock(mpb);
            }
            if (tKiller) { if (TimeLeft <= 0) End(); return; } // a T-killer does not eat virions: the chapter handles its targets
            Vector3 head = rig.head.transform.position;
            foreach (var v in immune.viruses)
            {
                if (!v.gameObject.activeSelf || !v.Alive || v.Engulfer) continue;
                Vector3 p = v.transform.position; Transform into = null;
                if ((p - blobL.position).sqrMagnitude < reachHand * reachHand) into = blobL;
                else if ((p - blobR.position).sqrMagnitude < reachHand * reachHand) into = blobR;
                else if ((p - head).sqrMagnitude < reachHead * reachHead) into = blobR;
                if (into)
                {
                    v.Absorb(into); Eaten++;
                    audioDirector?.Play("BVR2_Gulp", 0.9f, Random.Range(0.9f, 1.15f));
                    Bvr2Haptics.Pulse(into == blobL, 0.5f, 0.08f);
                }
            }
            if (TimeLeft <= 0) End();
        }
    }
}
