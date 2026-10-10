using System;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    /// <summary>Reusable lightweight containers. Only presentation and UI bindings are authored.</summary>
    public static class CleanHolographicScreensBuilder
    {
        const string Folder=JourneyGeometry.Root+"/Generated/HolographicScreens";
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var hud=UnityEngine.Object.FindFirstObjectByType<JourneyHud>();var camera=hud.mission.mover.viewCamera;
            var old=camera.transform.Find("Active holographic screens");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("Active holographic screens");root.transform.SetParent(camera.transform,false);
            var screens=hud.GetComponent<JourneyHolographicScreens>();if(!screens)screens=hud.gameObject.AddComponent<JourneyHolographicScreens>();screens.hud=hud;
            screens.theoryRoot=CanvasRoot(root.transform,"Theory — clean reusable container",camera,new Vector2(640,820),new Vector3(-.43f,.025f,1.70f),.0010f,true);
            screens.vitalsRoot=CanvasRoot(root.transform,"Vitals — clean monitoring grid",camera,new Vector2(780,380),new Vector3(.43f,.34f,1.65f),.00095f,false);
            screens.taskRoot=null;
            Skin(screens.theoryRoot);Skin(screens.vitalsRoot);

            var icon=Label(screens.theoryRoot,"Topic icon","+",28,HolographicStyle.Cyan);Bounds(icon.rectTransform,28,-57,-568,-18);icon.alignment=TextAnchor.MiddleCenter;
            var tag=Label(screens.theoryRoot,"Section label","ТЕОРИЯ / ЗАДАЧА",20,HolographicStyle.Cyan);Bounds(tag.rectTransform,80,-50,-28,-20);
            screens.theoryTitle=Label(screens.theoryRoot,"Topic title","Состав крови",38,HolographicStyle.White);screens.theoryTitle.fontStyle=FontStyle.Bold;screens.theoryTitle.resizeTextForBestFit=true;screens.theoryTitle.resizeTextMinSize=28;screens.theoryTitle.resizeTextMaxSize=38;Bounds(screens.theoryTitle.rectTransform,28,-118,-28,-54);
            screens.pageLabel=Label(screens.theoryRoot,"Reading page","КРАТКО • 01 / 02",18,HolographicStyle.Secondary);Bounds(screens.pageLabel.rectTransform,28,-150,-28,-126);
            screens.overview=Rect(screens.theoryRoot,"Overview — editable content slots").gameObject;Fill((RectTransform)screens.overview.transform);
            var main=Card(screens.overview.transform,"Main content slot",28,-172,584,176);
            screens.shortTheory=Label(main,"Brief theory — 2 to 4 lines","Короткое объяснение темы.\nДанные подставляются из текущего урока.",30,HolographicStyle.White);Inset(screens.shortTheory.rectTransform,18,15,18,12);
            var illustration=Card(screens.overview.transform,"Illustration or diagram placeholder",28,-366,584,122);
            var slotTag=Label(illustration,"Illustration label","СХЕМА / МОДЕЛЬ",16,HolographicStyle.Secondary);Bounds(slotTag.rectTransform,18,-38,-18,-14);
            screens.illustrationText=Label(illustration,"Formula or illustration caption","Hb + O₂ ⇄ HbO₂",30,HolographicStyle.Cyan);Bounds(screens.illustrationText.rectTransform,18,-94,-18,-50);screens.illustrationText.alignment=TextAnchor.MiddleCenter;
            for(int i=0;i<3;i++){var dot=Rounded(illustration,"Diagram node "+i,new Color(HolographicStyle.Cyan.r,HolographicStyle.Cyan.g,1,.48f),true,1.4f,7);dot.rectTransform.anchorMin=dot.rectTransform.anchorMax=new Vector2(.5f,0);dot.rectTransform.sizeDelta=new Vector2(12,12);dot.rectTransform.anchoredPosition=new Vector2((i-1)*24,26);}
            var task=Card(screens.overview.transform,"Task slot",28,-506,584,192);
            var taskTag=Label(task,"Task label","ТЕКУЩАЯ ЗАДАЧА",17,HolographicStyle.Amber);Bounds(taskTag.rectTransform,18,-40,-160,-15);
            screens.taskProgress=Label(task,"Goal progress","0 / 3",22,HolographicStyle.Cyan);screens.taskProgress.resizeTextForBestFit=true;screens.taskProgress.resizeTextMinSize=16;screens.taskProgress.resizeTextMaxSize=22;Bounds(screens.taskProgress.rectTransform,400,-42,-18,-12);screens.taskProgress.alignment=TextAnchor.MiddleRight;
            screens.taskText=Label(task,"Current action","Достань объект и изучи его",29,HolographicStyle.White);Bounds(screens.taskText.rectTransform,18,-153,-18,-45);
            screens.taskHint=Label(task,"Short input cue","Grip / ЛКМ — взять · F — сканировать",19,HolographicStyle.Secondary);Bounds(screens.taskHint.rectTransform,18,-190,-18,-163);

            screens.referenceView=Rect(screens.theoryRoot,"Full reference — scroll view").gameObject;Fill((RectTransform)screens.referenceView.transform);
            var viewport=Rect(screens.referenceView.transform,"Reference viewport");Inset(viewport,28,174,44,120);
            var background=Rounded(viewport,"Reference glass",HolographicStyle.Glass(.26f),false,0,12);Fill(background.rectTransform);background.raycastTarget=true;
            viewport.gameObject.AddComponent<RectMask2D>();
            screens.theoryText=Label(viewport,"Complete theory and formulas","",27,HolographicStyle.White);
            var content=screens.theoryText.rectTransform;content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.offsetMin=new Vector2(18,0);content.offsetMax=new Vector2(-22,0);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            screens.theoryScroll=screens.theoryRoot.gameObject.AddComponent<ScrollRect>();screens.theoryScroll.viewport=viewport;screens.theoryScroll.content=content;screens.theoryScroll.horizontal=false;screens.theoryScroll.vertical=true;screens.theoryScroll.movementType=ScrollRect.MovementType.Clamped;screens.theoryScroll.scrollSensitivity=38;
            var bar=MakeScrollbar(screens.referenceView.transform);screens.theoryScroll.verticalScrollbar=bar;
            screens.referenceView.SetActive(false);screens.theoryScroll.enabled=false;
            var footer=Label(screens.theoryRoot,"Footer reading hint","Tab — справка • колесо / стик — прокрутка",17,HolographicStyle.Secondary);Bounds(footer.rectTransform,28,-733,-28,-709);
            var back=Button(screens.theoryRoot,"Назад",28,754,174,48,false);UnityEventTools.AddPersistentListener(back.onClick,screens.ShowOverview);
            screens.referenceButton=Button(screens.theoryRoot,"Подробнее",214,754,184,48,false);UnityEventTools.AddPersistentListener(screens.referenceButton.onClick,screens.TogglePractice);
            var practice=Button(screens.theoryRoot,"К практике",410,754,202,48,true);UnityEventTools.AddPersistentListener(practice.onClick,screens.ShowOverview);screens.practiceButton=practice.gameObject;
            var next=Button(screens.theoryRoot,"Далее →",410,754,202,48,true);UnityEventTools.AddPersistentListener(next.onClick,hud.ContinueJourney);hud.continueButton=next;next.gameObject.SetActive(false);

            var monitoring=Label(screens.vitalsRoot,"Monitor heading","МОНИТОРИНГ",20,HolographicStyle.Cyan);Bounds(monitoring.rectTransform,28,-54,-440,-20);
            var online=Label(screens.vitalsRoot,"Monitor status","●  МОДЕЛЬ / LIVE",16,HolographicStyle.Secondary);Bounds(online.rectTransform,460,-53,-28,-23);online.alignment=TextAnchor.UpperRight;
            screens.monitorStatus=online;screens.vitalValues=new Text[5];screens.vitalStatuses=new Text[5];screens.vitalLabels=new Text[5];
            string[] labels={"АД • мм рт. ст.","ПУЛЬС • уд/мин","SpO₂","ТЕМПЕРАТУРА","ВИРУС"};
            for(int i=0;i<5;i++)
            {
                float x=i<3?28+i*248:28+(i-3)*372;float y=i<3?-78:-234;float width=i<3?228:352;
                var cell=Card(screens.vitalsRoot,"Vital metric slot "+i,x,y,width,134);
                var label=Label(cell,"Metric label",labels[i],20,HolographicStyle.Secondary);Bounds(label.rectTransform,14,-36,-14,-10);
                screens.vitalLabels[i]=label;
                var glyph=Label(cell,"Small metric icon",new[]{"+","~","O₂","°","V"}[i],20,HolographicStyle.Cyan);Bounds(glyph.rectTransform,width-38,-36,-12,-10);glyph.alignment=TextAnchor.UpperRight;
                screens.vitalValues[i]=Label(cell,"Large numeric value",i==0?"120/80":i==1?"72":i==2?"98 %":i==3?"36,6°":"0 %",i==0?44:49,HolographicStyle.White);screens.vitalValues[i].fontStyle=FontStyle.Bold;Bounds(screens.vitalValues[i].rectTransform,14,-104,-14,-38);
                screens.vitalStatuses[i]=Label(cell,"Metric state","Норма",19,HolographicStyle.Secondary);Bounds(screens.vitalStatuses[i].rectTransform,14,-132,-14,-108);
            }
            screens.vitalsText=null;screens.pulseTrace=PulseLine(screens.vitalsRoot);
            screens.groups=new[]{screens.theoryRoot.GetComponent<CanvasGroup>(),screens.vitalsRoot.GetComponent<CanvasGroup>()};
            hud.panelRoot.GetComponent<Canvas>().enabled=false;hud.inputGuard.surfaces=new[]{screens.theoryRoot};
            var overlay=new Material(Shader.Find("BiologyVR/Holographic Overlay UI")){name="Readable holographic HUD"};JourneyGeometry.Save(overlay,Folder+"/ReadableOverlay.mat");overlay=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/ReadableOverlay.mat");
            foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))graphic.material=overlay;
            SaveTemplate(screens.theoryRoot,"CleanTheoryContainer");SaveTemplate(screens.vitalsRoot,"CleanVitalsContainer");
            EditorUtility.SetDirty(hud);EditorUtility.SetDirty(hud.inputGuard);EditorUtility.SetDirty(screens);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);EditorSceneManager.SaveScene(hud.gameObject.scene);
            File.WriteAllText(ActiveInstrumentBuilder.Reports+"/HolographicScreens.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,style="Clean educational holography — readability second pass",thinRoundedFrames=true,heavySourceRimsVisible=false,glassAlpha=.30f,contentAlpha=.24f,foregroundReadableUI=true,overview="header / short theory / illustration / task / navigation",fullReferenceRetained=true,theoryWidthMetres=.64f,theoryHeightMetres=.82f,theoryPosition=screens.theoryRoot.localPosition.ToString(),metrics=5,materials="shared lightweight clipped overlay UI + one URP Unlit pulse line",hardwareVerified=false},Formatting.Indented));
        }
        static RectTransform CanvasRoot(Transform parent,string name,Camera camera,Vector2 size,Vector3 position,float scale,bool interactive)
        {var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;rect.sizeDelta=size;rect.localPosition=position;rect.localScale=Vector3.one*scale;var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;canvas.overrideSorting=true;canvas.sortingOrder=60;if(interactive){go.AddComponent<GraphicRaycaster>();go.AddComponent<ArteryTrackedRaycaster>();}return rect;}
        static void SaveTemplate(RectTransform source,string name)
        {
            var clone=UnityEngine.Object.Instantiate(source.gameObject);clone.name=name;clone.transform.SetParent(null,false);clone.transform.localPosition=source.localPosition;clone.transform.localRotation=Quaternion.identity;clone.transform.localScale=source.localScale;
            clone.GetComponent<Canvas>().worldCamera=null;
            foreach(var button in clone.GetComponentsInChildren<Button>(true))button.onClick=new Button.ButtonClickedEvent();
            PrefabUtility.SaveAsPrefabAsset(clone,Folder+"/"+name+".prefab");UnityEngine.Object.DestroyImmediate(clone);
        }
        static void Skin(RectTransform root)
        {
            var glass=Rounded(root,"Dark transparent glass",HolographicStyle.Glass(.30f),false,0,20);Fill(glass.rectTransform);
            var edge=Rounded(root,"Thin emissive cyan outline",new Color(HolographicStyle.Cyan.r,HolographicStyle.Cyan.g,1,.80f),true,2.2f,20);Fill(edge.rectTransform);
            var glow=Rounded(root,"Soft edge halo",new Color(HolographicStyle.SoftGlow.r,HolographicStyle.SoftGlow.g,1,.06f),true,6,22);Fill(glow.rectTransform);
            var accent=Rounded(root,"Amber corner signal",HolographicStyle.Amber,false,0,2);accent.rectTransform.anchorMin=accent.rectTransform.anchorMax=new Vector2(1,1);accent.rectTransform.pivot=new Vector2(1,1);accent.rectTransform.anchoredPosition=new Vector2(-28,-12);accent.rectTransform.sizeDelta=new Vector2(28,3);
        }
        static RectTransform Card(Transform parent,string name,float x,float top,float width,float height)
        {var card=Rect(parent,name);card.anchorMin=card.anchorMax=new Vector2(0,1);card.pivot=new Vector2(0,1);card.anchoredPosition=new Vector2(x,top);card.sizeDelta=new Vector2(width,height);var fill=Rounded(card,"Content glass",HolographicStyle.Glass(name=="Task slot"?.30f:.24f),false,0,12);Fill(fill.rectTransform);var outline=Rounded(card,"Quiet content edge",new Color(HolographicStyle.Cyan.r,HolographicStyle.Cyan.g,1,.22f),true,1,12);Fill(outline.rectTransform);return card;}
        static RectTransform Rect(Transform parent,string name){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return (RectTransform)go.transform;}
        static JourneyRoundedGraphic Rounded(Transform parent,string name,Color color,bool outline,float width,float radius)
        {var rect=Rect(parent,name);rect.gameObject.AddComponent<CanvasRenderer>();var graphic=rect.gameObject.AddComponent<JourneyRoundedGraphic>();graphic.outlineOnly=outline;graphic.borderWidth=width;graphic.radius=radius;graphic.color=color;graphic.raycastTarget=false;return graphic;}
        static Text Label(Transform parent,string name,string caption,int size,Color color)
        {var rect=Rect(parent,name);rect.gameObject.AddComponent<CanvasRenderer>();var text=rect.gameObject.AddComponent<Text>();text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular.ttf");text.text=caption;text.fontSize=size;text.color=color;text.alignment=TextAnchor.UpperLeft;text.supportRichText=true;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;}
        static Button Button(RectTransform parent,string caption,float x,float top,float width,float height,bool primary)
        {var graphic=Rounded(parent,caption,primary?new Color(HolographicStyle.Amber.r,HolographicStyle.Amber.g,HolographicStyle.Amber.b,.18f):HolographicStyle.Glass(.30f),false,0,10);var rect=graphic.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-top);rect.sizeDelta=new Vector2(width,height);graphic.raycastTarget=true;var outline=Rounded(rect,"Button edge",primary?HolographicStyle.Amber:new Color(HolographicStyle.Cyan.r,HolographicStyle.Cyan.g,1,.5f),true,1.8f,10);Fill(outline.rectTransform);var button=graphic.gameObject.AddComponent<Button>();button.targetGraphic=graphic;var label=Label(rect,"Caption",caption,24,primary?HolographicStyle.Amber:HolographicStyle.White);Fill(label.rectTransform);label.alignment=TextAnchor.MiddleCenter;return button;}
        static Scrollbar MakeScrollbar(Transform parent)
        {var graphic=Rounded(parent,"Reference scrollbar",new Color(HolographicStyle.Cyan.r,HolographicStyle.Cyan.g,1,.10f),false,0,4);var rect=graphic.rectTransform;rect.anchorMin=new Vector2(1,0);rect.anchorMax=new Vector2(1,1);rect.offsetMin=new Vector2(-36,120);rect.offsetMax=new Vector2(-28,-174);var bar=rect.gameObject.AddComponent<Scrollbar>();var handle=Rounded(rect,"Handle",new Color(HolographicStyle.Cyan.r,HolographicStyle.Cyan.g,1,.65f),false,0,4);Fill(handle.rectTransform);handle.raycastTarget=true;bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;bar.direction=Scrollbar.Direction.BottomToTop;return bar;}
        static LineRenderer PulseLine(Transform parent)
        {var go=new GameObject("Quiet model cardioline",typeof(LineRenderer));go.transform.SetParent(parent,false);var line=go.GetComponent<LineRenderer>();var material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="Soft blue holographic line"};material.SetColor("_BaseColor",HolographicStyle.SoftGlow*.65f);JourneyGeometry.Save(material,Folder+"/Pulse.mat");line.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Pulse.mat");line.useWorldSpace=false;line.widthMultiplier=.00085f;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return line;}
        static void Fill(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        static void Inset(RectTransform rect,float left,float top,float right,float bottom){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(left,bottom);rect.offsetMax=new Vector2(-right,-top);}
        static void Bounds(RectTransform rect,float left,float bottom,float right,float top){rect.anchorMin=new Vector2(0,1);rect.anchorMax=new Vector2(1,1);rect.offsetMin=new Vector2(left,bottom);rect.offsetMax=new Vector2(right,top);}
    }
}
