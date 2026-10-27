using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// A clot where none is needed (chapter 4): glue painted on healthy wall or too much fibrin grows a
    /// lump into the lumen — a thrombus that narrows the artery. The Impulse cuts it away.
    /// A pulsing red ring marks it so it is easy to find.
    /// </summary>
    public sealed class Bvr2StrayClot : Bvr2ImpulseTarget
    {
        Bvr2Clot clot; Transform marker; Renderer markerR; float growth, dissolve, size = 1f; bool dying; MaterialPropertyBlock mpb;
        public float Growth => growth;
        public float Dissolve => dissolve;
        public override bool Alive => gameObject.activeSelf && !dying;
        public int Cut { get; private set; }

        public void Build(Mesh red, Mesh platelet, Material redMat, Material plateletMat, Material fibrinMat)
        {
            var go = new GameObject("Clot"); go.transform.SetParent(transform, false);
            go.SetActive(false);
            clot = go.AddComponent<Bvr2Clot>();
            clot.redMesh = red; clot.plateletMesh = platelet; clot.redMaterial = redMat; clot.plateletMaterial = plateletMat; clot.fibrinMaterial = fibrinMat;
            clot.size = 1.5f; clot.dome = true; clot.redCount = 14; clot.plateletCount = 16; clot.fibrinCount = 16; clot.seed = Random.Range(1, 999);
            go.SetActive(true); clot.Build();
            mpb = new MaterialPropertyBlock();
        }

        /// <summary>Attach a ring marker (mesh + material from the wound).</summary>
        public void SetMarker(Mesh ring, Material mat)
        {
            if (marker || !ring) return;
            var go = new GameObject("Marker"); go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = ring; markerR = go.AddComponent<MeshRenderer>(); markerR.sharedMaterial = mat;
            markerR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; marker = go.transform;
        }

        public void Place(Vector3 wallPoint, Vector3 inward, float scale, Bvr2Fx fx, Bvr2Audio audio)
        {
            transform.position = wallPoint; transform.rotation = Quaternion.LookRotation(inward);
            size = scale; growth = 0.05f; dissolve = 0; dying = false; radius = 0.6f * scale;
            transform.localScale = Vector3.one * scale;
            gameObject.SetActive(true);
            fx?.Burst(wallPoint + inward * 0.2f, Bvr2Style.Danger, 16, 1.2f, 0.04f, 0.6f);
            audio?.PlayAt("BVR2_Stick", wallPoint, 0.6f);
        }

        public override void Hit(bool open)
        {
            if (dying) return;
            dying = true; Cut++;
            var fx = Bvr2Game.Instance ? Bvr2Game.Instance.fx : null;
            fx?.Burst(transform.position + transform.forward * 0.4f, new Color(1f, 0.85f, 0.5f), 30, 2.4f, 0.05f, 0.7f);
            Bvr2Game.Instance?.sound?.PlayAt("BVR2_Melt", transform.position, 0.8f);
        }

        void Update()
        {
            float dt = Bvr2Time.Delta;
            if (dying)
            {
                dissolve = Mathf.MoveTowards(dissolve, 1f, dt / 0.8f);
                if (dissolve >= 1f) { gameObject.SetActive(false); return; }
            }
            else growth = Mathf.MoveTowards(growth, 1f, dt / 9f);
            if (clot) { clot.growth = Mathf.Max(growth, 0.3f); clot.dissolve = dissolve; }
            if (marker)
            {
                marker.localPosition = Vector3.forward * 0.6f; marker.localRotation = Quaternion.identity;
                marker.localScale = Vector3.one * (0.9f + 0.1f * Mathf.Sin(Time.time * 5f)) * (1f - dissolve);
                mpb.SetColor("_Color", new Color(1f, 0.3f, 0.2f, 0.7f * (1f - dissolve))); markerR.SetPropertyBlock(mpb);
            }
        }
    }
}
