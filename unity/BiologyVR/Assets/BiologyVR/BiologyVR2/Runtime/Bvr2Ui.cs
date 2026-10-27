using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.V2
{
    /// <summary>Small factory for world-space UI. Text draws on top of tissue (overlay material)
    /// so captions are never cut by the wall. Sizes are in canvas units; scale sets metres per unit.</summary>
    public static class Bvr2Ui
    {
        public static Font Font;
        public static Material Overlay;
        public static Sprite Rounded, RingSprite, Dot, Glow;
        public static Material HoloBackdrop; public static Mesh QuadMesh;

        /// <summary>A parallax hologram grid behind a world-space board (Sci-Fi Hologram Screen shader, if present).</summary>
        public static GameObject Backdrop(RectTransform canvas, float depthUnits = 30f, float margin = 40f)
        {
            if (!HoloBackdrop || !QuadMesh) return null;
            var go = new GameObject("Holo Backdrop"); go.transform.SetParent(canvas, false);
            go.transform.localPosition = new Vector3(0, 0, depthUnits);
            go.transform.localScale = new Vector3((canvas.sizeDelta.x + margin) * 0.5f, (canvas.sizeDelta.y + margin) * 0.5f, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = QuadMesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = HoloBackdrop; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        public static RectTransform Canvas(string name, Transform parent, Vector2 size, float metresPerUnit, int order = 60)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var c = go.AddComponent<UnityEngine.Canvas>(); c.renderMode = RenderMode.WorldSpace; c.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>(); scaler.dynamicPixelsPerUnit = 3.2f;
            var rt = (RectTransform)go.transform; rt.sizeDelta = size; rt.localScale = Vector3.one * metresPerUnit;
            return rt;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform; r.anchorMin = anchorMin; r.anchorMax = anchorMax; r.offsetMin = offsetMin; r.offsetMax = offsetMax; return r;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal, string name = "Text")
        {
            var r = Rect(parent, name, Vector2.zero, Vector2.one);
            var t = r.gameObject.AddComponent<Text>();
            t.font = Font; t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = style; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.supportRichText = true;
            if (Overlay) t.material = Overlay;
            return t;
        }

        /// <summary>Shrink-to-fit text (best fit only works with wrapping + truncation).</summary>
        public static Text Fit(Text t, int min, int max)
        {
            t.resizeTextForBestFit = true; t.resizeTextMinSize = min; t.resizeTextMaxSize = max;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; return t;
        }

        public static Image Panel(Transform parent, Color color, string name = "Panel", Sprite sprite = null)
        {
            var r = Rect(parent, name, Vector2.zero, Vector2.one);
            var i = r.gameObject.AddComponent<Image>(); i.color = color; i.raycastTarget = false;
            i.sprite = sprite ? sprite : Rounded; if (i.sprite && i.sprite.border.sqrMagnitude > 0) i.type = Image.Type.Sliced;
            if (Overlay) i.material = Overlay;
            return i;
        }

        public static Image Fill(Transform parent, Color color, Sprite sprite, float amount, string name = "Fill")
        {
            var i = Panel(parent, color, name, sprite);
            i.type = Image.Type.Filled; i.fillMethod = Image.FillMethod.Radial360; i.fillOrigin = (int)Image.Origin360.Bottom; i.fillClockwise = true; i.fillAmount = amount;
            return i;
        }

        public static void Place(RectTransform r, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(0.5f, 0.5f); r.sizeDelta = size; r.anchoredPosition = pos;
        }

        public static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
    }
}
