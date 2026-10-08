using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryAnnotations
    {
        public static Text Label(Transform parent,string name,string caption)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(parent,false);
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.overrideSorting=true;canvas.sortingOrder=15;
            var rect=go.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(260,45);rect.localScale=Vector3.one*.0022f;
            var text=go.GetComponent<Text>();text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular.ttf")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize=17;text.text=caption;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(.75f,.98f,1);text.raycastTarget=false;text.supportRichText=false;
            return text;
        }
    }
}
