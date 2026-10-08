// RECOVERY: Reconstructed from this session's captured source and patches; Z: source read failed.
using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Texture-free rounded uGUI fill or outline. Uses the existing UI material/shader.</summary>
    [AddComponentMenu("Biology VR/UI/Rounded Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class JourneyRoundedGraphic : MaskableGraphic
    {
        public float radius = 16f;
        public float borderWidth;
        const int SegmentsPerCorner = 8;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0 || rect.height <= 0) return;
            float r = Mathf.Clamp(radius, 0, Mathf.Min(rect.width, rect.height) * .5f);
            const int count = 4 * (SegmentsPerCorner + 1);
            float width = Mathf.Clamp(borderWidth, 0, Mathf.Min(rect.width, rect.height) * .5f);
            if (width <= 0)
            {
                AddFilledShape(mesh, rect, r, count, color);
            }
            else
            {
                // The previous implementation emitted only the annulus here. That made every
                // panel/card with a border look transparent in the saved XR preview. Keep the
                // border, then add a second fan for the interior so borderWidth never means
                // "outline only".
                Rect inner = new Rect(rect.x + width, rect.y + width, rect.width - 2 * width, rect.height - 2 * width);
                for (int i = 0; i < count; i++)
                {
                    mesh.AddVert(Point(rect, r, i), color, Vector2.zero);
                    mesh.AddVert(Point(inner, Mathf.Max(0, r - width), i), color, Vector2.zero);
                }
                for (int i = 0; i < count; i++)
                {
                    int a = 2 * i, b = 2 * ((i + 1) % count);
                    mesh.AddTriangle(a, b, a + 1);
                    mesh.AddTriangle(a + 1, b, b + 1);
                }

                if (inner.width > 0 && inner.height > 0)
                {
                    int center = mesh.currentVertCount;
                    int innerStart = 1;
                    mesh.AddVert(inner.center, color, Vector2.zero);
                    for (int i = 0; i < count; i++)
                    {
                        int current = 2 * i + innerStart;
                        int next = 2 * ((i + 1) % count) + innerStart;
                        mesh.AddTriangle(center, current, next);
                    }
                }
            }
        }

        static void AddFilledShape(VertexHelper mesh, Rect rect, float radius, int count, Color vertexColor)
        {
            int center = mesh.currentVertCount;
            mesh.AddVert(rect.center, vertexColor, Vector2.zero);
            for (int i = 0; i < count; i++) mesh.AddVert(Point(rect, radius, i), vertexColor, Vector2.zero);
            for (int i = 0; i < count; i++) mesh.AddTriangle(center, i + 1, (i + 1) % count + 1);
        }

        static Vector2 Point(Rect rect, float radius, int index)
        {
            int corner = index / (SegmentsPerCorner + 1);
            float angle = (corner * 90f + (index % (SegmentsPerCorner + 1)) * 90f / SegmentsPerCorner) * Mathf.Deg2Rad;
            Vector2 center = new Vector2(corner == 0 || corner == 3 ? rect.xMax - radius : rect.xMin + radius,
                corner < 2 ? rect.yMax - radius : rect.yMin + radius);
            return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
#if UNITY_EDITOR
        protected override void OnValidate() { base.OnValidate(); SetVerticesDirty(); }
#endif
    }
}
