using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Readable PC onboarding independent of the camera-mounted XR canvas.</summary>
    public sealed class JourneyDesktopOnboarding:MonoBehaviour
    {
        public JourneyHud hud;
        public Font uiFont;
        Canvas screen,worldCanvas;
        GameObject panel;
        Text title,context,objective,controls,progress,crosshair,briefing;
        int lastStep=-1,lastScene=-1;
        bool lastTracked;
        void Start()
        {
            if(!hud)hud=GetComponent<JourneyHud>();if(!hud||!hud.panelRoot)return;
            if(!uiFont&&hud.title)uiFont=hud.title.font;
            worldCanvas=hud.panelRoot.GetComponent<Canvas>();
            var root=new GameObject("PC onboarding — screen space",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            root.transform.SetParent(transform,false);screen=root.GetComponent<Canvas>();screen.renderMode=RenderMode.ScreenSpaceOverlay;screen.sortingOrder=100;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            panel=new GameObject("PC instruction panel",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));panel.transform.SetParent(root.transform,false);
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(24,-24);rect.sizeDelta=new Vector2(430,270);
            var image=panel.GetComponent<Image>();image.color=new Color(.008f,.045f,.058f,.95f);image.raycastTarget=false;
            title=Text(panel.transform,"Title",18,new Color(.30f,.95f,1));Layout(title,16,18,398,28);
            context=Text(panel.transform,"Where am I",16,new Color(.87f,.92f,.94f));Layout(context,16,54,398,44);
            objective=Text(panel.transform,"What next",21,Color.white);Layout(objective,16,112,398,60);
            controls=Text(panel.transform,"How",17,new Color(.96f,.84f,.64f));Layout(controls,16,182,398,58);
            progress=Text(panel.transform,"Progress",14,new Color(.65f,.82f,.84f));Layout(progress,16,245,398,20);
            crosshair=Text(root.transform,"Centre aim",22,new Color(.70f,1,1));var aim=crosshair.rectTransform;aim.anchorMin=aim.anchorMax=new Vector2(.5f,.5f);aim.sizeDelta=new Vector2(28,28);aim.anchoredPosition=Vector2.zero;crosshair.text="+";crosshair.alignment=TextAnchor.MiddleCenter;
            briefing=Text(root.transform,"Optional briefing",18,Color.white);var br=briefing.rectTransform;br.anchorMin=br.anchorMax=new Vector2(0,1);br.pivot=new Vector2(0,1);br.anchoredPosition=new Vector2(24,-316);br.sizeDelta=new Vector2(430,290);
        }
        Text Text(Transform parent,string name,int size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(parent,false);var text=go.GetComponent<Text>();text.font=uiFont?uiFont:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=size;text.color=color;text.alignment=TextAnchor.UpperLeft;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;
        }
        static void Layout(Text text,float x,float y,float width,float height)
        {var r=text.rectTransform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
        void LateUpdate()
        {
            if(!hud||!hud.mission||hud.mission.Goals==null||!screen)return;
            var m=hud.mission;bool tracked=m.mover.UsingTrackedInput;screen.enabled=!tracked;
            if(worldCanvas)worldCanvas.enabled=tracked;
            if(tracked)return;
            panel.SetActive(!hud.panelGroup||hud.panelGroup.alpha>.1f);
            if(lastStep!=m.StepIndex||lastScene!=m.SceneNumber||lastTracked!=tracked)
            {
                lastStep=m.StepIndex;lastScene=m.SceneNumber;lastTracked=tracked;
                title.text=m.SceneNumber==3?"BIOLOGY VR • СОСТАВ КРОВИ":hud.title.text;
                context.text=m.SceneNumber==3?"Вы внутри артерии. Перед вами учебные клетки; остальные движутся с кровотоком.":m.LearningTerm;
                objective.text=m.SceneNumber==3?m.StepIndex==0?"1. Наведи мышь на эритроцит: ЛКМ удерживать":m.StepIndex==1?"2. Просканируйте: удерживайте F":m.StepIndex==2?"3. Увеличьте модель — нажмите Z":m.CurrentGoalLabel:m.CurrentGoalLabel;
            }
            controls.text=hud.toolHint?hud.toolHint.text:"ПКМ + мышь — осмотр";
            if(m.SceneNumber==7&&m.world.EmbolusCaptured)
            {
                var input=m.GetComponent<JourneyInput>();
                controls.text="Удерживай E • мышь — переноси поле\nКолесо — глубина: "+input.DesktopFieldDistance.ToString("0.0")+" м • удержи внутри зоны";
            }
            var desktop=m.GetComponent<JourneyInput>()?.DesktopControls;
            if(desktop&&desktop.Active)
            {
                var pointer=desktop.PointerPosition;var size=screen.GetComponent<RectTransform>().rect.size;
                crosshair.rectTransform.anchoredPosition=new Vector2((pointer.x/Mathf.Max(1,Screen.width)-.5f)*size.x,(pointer.y/Mathf.Max(1,Screen.height)-.5f)*size.y);
                crosshair.color=desktop.Holding?new Color(.4f,1,.55f):new Color(.7f,1,1);
                if(desktop.Holding)controls.text="ЛКМ удерживать — переноси указателем\nКолесо — глубина: "+desktop.GrabDepth.ToString("0.0")+" м • отпусти ЛКМ для размещения";
            }
            progress.text=hud.progress.text+" • F1 — панель • Tab — справка";
            briefing.gameObject.SetActive(hud.briefingRoot&&hud.briefingRoot.activeSelf);if(briefing.gameObject.activeSelf)briefing.text=hud.info.text;
        }
    }
}
