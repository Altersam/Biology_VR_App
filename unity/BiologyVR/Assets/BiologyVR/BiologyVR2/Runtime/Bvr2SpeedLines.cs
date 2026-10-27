using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// Plasma streaks rushing past the head when the player moves fast (boost, strong current):
    /// the sense of speed without moving the camera's FOV (VR comfort). One dynamic mesh of
    /// camera-facing stretched quads, drawn with the additive particle material.
    /// </summary>
    public sealed class Bvr2SpeedLines : MonoBehaviour
    {
        public Bvr2Swimmer swimmer;
        public Transform head;
        public Material material;
        public int count = 70;
        public float innerRadius = 0.7f, outerRadius = 2.6f, depth = 7f;
        public float startSpeed = 4.5f, fullSpeed = 10f;

        Vector3[] pos; float[] len; Mesh mesh; Vector3[] verts; Color[] cols; float shown; Vector3 lastDir = Vector3.forward;

        void Start()
        {
            pos = new Vector3[count]; len = new float[count];
            for (int i = 0; i < count; i++) Respawn(i, Vector3.forward, true);
            mesh = new Mesh { name = "BVR2_SpeedLines" }; mesh.MarkDynamic();
            verts = new Vector3[count * 4]; cols = new Color[count * 4];
            var uv = new Vector2[count * 4]; var tris = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                uv[i * 4] = new Vector2(0, 0); uv[i * 4 + 1] = new Vector2(1, 0); uv[i * 4 + 2] = new Vector2(0, 1); uv[i * 4 + 3] = new Vector2(1, 1);
                int v = i * 4, t = i * 6; tris[t] = v; tris[t + 1] = v + 2; tris[t + 2] = v + 1; tris[t + 3] = v + 1; tris[t + 4] = v + 2; tris[t + 5] = v + 3;
            }
            mesh.vertices = verts; mesh.uv = uv; mesh.colors = cols; mesh.triangles = tris;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
        }

        void Respawn(int i, Vector3 dir, bool anywhere)
        {
            // a point in a hollow cylinder around the head, ahead of it (streaks fly toward and past the player)
            Vector3 side = Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.right).normalized, up = Vector3.Cross(side, dir);
            float a = Random.value * Mathf.PI * 2f, r = Mathf.Lerp(innerRadius, outerRadius, Mathf.Sqrt(Random.value));
            float along = anywhere ? Random.Range(-depth, depth) : Random.Range(depth * 0.6f, depth);
            pos[i] = (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * r + dir * along; // relative to the head
            len[i] = Random.Range(0.5f, 1.4f);
        }

        void LateUpdate()
        {
            if (!swimmer || !head || !material || mesh == null) return;
            Vector3 v = swimmer.Velocity; float speed = v.magnitude;
            float target = Mathf.InverseLerp(startSpeed, fullSpeed, speed) + (swimmer.Boosting ? 0.35f : 0f);
            shown = Mathf.MoveTowards(shown, Mathf.Clamp01(target), Time.deltaTime * 3f);
            if (shown <= 0.001f) return;
            Vector3 dir = speed > 0.5f ? v / speed : lastDir; lastDir = dir;
            Vector3 hp = head.position; float dt = Time.deltaTime;
            for (int i = 0; i < count; i++)
            {
                pos[i] -= v * dt * 1.35f; // the world streams past
                if (Vector3.Dot(pos[i], dir) < -depth * 0.6f || pos[i].sqrMagnitude > (depth + outerRadius) * (depth + outerRadius)) Respawn(i, dir, false);
                Vector3 w = hp + pos[i];
                Vector3 toCam = (hp - w).normalized;
                Vector3 width = Vector3.Cross(dir, toCam).normalized * 0.012f;
                float L = len[i] * (0.4f + speed * 0.12f);
                Vector3 a = w - dir * L * 0.5f, b = w + dir * L * 0.5f;
                int k = i * 4;
                verts[k] = a - width; verts[k + 1] = a + width; verts[k + 2] = b - width; verts[k + 3] = b + width;
                float d = pos[i].magnitude; float alpha = shown * Mathf.Clamp01((d - 0.5f) / 0.8f) * Mathf.Clamp01((depth - d) / 2f) * 0.55f;
                var c = new Color(1f, 0.85f, 0.8f, alpha); cols[k] = cols[k + 1] = cols[k + 2] = cols[k + 3] = c;
            }
            mesh.vertices = verts; mesh.colors = cols;
            Graphics.RenderMesh(new RenderParams(material) { shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows = false, worldBounds = new Bounds(hp, Vector3.one * 40f) }, mesh, 0, Matrix4x4.identity);
        }
    }
}
