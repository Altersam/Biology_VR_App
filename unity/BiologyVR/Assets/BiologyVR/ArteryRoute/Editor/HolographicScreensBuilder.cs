using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class HolographicScreensBuilder
    {
        const string Folder=JourneyGeometry.Root+"/Generated/HolographicScreens";
        static readonly Color Cyan=new Color(.30f,.86f,1);
        [MenuItem("Biology VR/Install Holographic Theory And Vitals Screens")]
        public static void Apply()
        {CleanHolographicScreensBuilder.Apply();}
        static void InstallLegacyReference()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var hud=UnityEngine.Object.FindFirstObjectByType<JourneyHud>();var camera=hud.mission.mover.viewCamera;
            string backup=ActiveInstrumentBuilder.Reports+"/ArteryNarrativeVR_before_holographic_screens.unity";
            EditorSceneManager.SaveScene(hud.gameObject.scene);if(!File.Exists(backup)&&!AssetDatabase.CopyAsset(hud.gameObject.scene.path,backup))throw new IOException("HUD backup failed");
            var old=camera.transform.Find("Active holographic screens");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("Active holographic screens");root.transform.SetParent(camera.transform,false);
            var screens=hud.GetComponent<JourneyHolographicScreens>();if(!screens)screens=hud.gameObject.AddComponent<JourneyHolographicScreens>();screens.hud=hud;
            screens.theoryRoot=CanvasRoot(root.transform,"Theory — supplied portal rim",camera,new Vector2(640,926),new Vector3(-.43f,.025f,1.70f),.0010f,true);
            screens.vitalsRoot=CanvasRoot(root.transform,"Vitals — supplied display rim",camera,new Vector2(780,361),new Vector3(.43f,.34f,1.65f),.00085f,false);
            screens.taskRoot=CanvasRoot(root.transform,"Task — compact holographic strip",camera,new Vector2(760,185),new Vector3(0,-.61f,1.85f),.0010f,true);
            foreach(var rect in new[]{screens.theoryRoot,screens.vitalsRoot,screens.taskRoot})
            {var field=Image(rect,"Transparent holographic glass",new Color(.025f,.22f,.32f,.24f));Stretch(field.rectTransform,18,18,18,18);field.raycastTarget=false;}
            Frame(screens.theoryRoot,"Theory",640);Frame(screens.vitalsRoot,"Vitals",780);
            screens.theoryTitle=Label(screens.theoryRoot,"Theory heading","ТЕОРИЯ / БИОЛОГИЯ",24,Cyan);Bounds(screens.theoryTitle.rectTransform,46,-84,-46,-36);
            var viewport=new GameObject("Theory scroll viewport",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(RectMask2D));viewport.transform.SetParent(screens.theoryRoot,false);
            var viewRect=(RectTransform)viewport.transform;Stretch(viewRect,44,112,52,100);viewport.GetComponent<Image>().color=new Color(.006f,.06f,.10f,.38f);viewport.GetComponent<Image>().raycastTarget=true;
            screens.theoryText=Label(viewRect,"All theory and formulas","",30,new Color(.90f,.98f,1));var content=screens.theoryText.rectTransform;
            content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.offsetMin=new Vector2(18,0);content.offsetMax=new Vector2(-18,0);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            screens.theoryScroll=screens.theoryRoot.gameObject.AddComponent<ScrollRect>();screens.theoryScroll.viewport=viewRect;screens.theoryScroll.content=content;screens.theoryScroll.horizontal=false;screens.theoryScroll.vertical=true;screens.theoryScroll.movementType=ScrollRect.MovementType.Clamped;screens.theoryScroll.scrollSensitivity=38;
            var scrollbar=MakeScrollbar(screens.theoryRoot);screens.theoryScroll.verticalScrollbar=scrollbar;screens.theoryScroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.Permanent;
            var hint=Label(screens.theoryRoot,"Read controls","Колесо / стик • ↑ ↓ • Tab — план",18,Cyan);Bounds(hint.rectTransform,46,-886,-46,-856);
            var up=Button(screens.theoryRoot,"↑",new Vector2(230,-416),new Vector2(44,44));UnityEventTools.AddPersistentListener(up.onClick,screens.ScrollUp);
            var down=Button(screens.theoryRoot,"↓",new Vector2(280,-416),new Vector2(44,44));UnityEventTools.AddPersistentListener(down.onClick,screens.ScrollDown);
            var practice=Button(screens.theoryRoot,"План",new Vector2(100,-416),new Vector2(160,44));UnityEventTools.AddPersistentListener(practice.onClick,screens.TogglePractice);
            var vitalsTitle=Label(screens.vitalsRoot,"Live vitals heading","BIO / КРОВЬ • УЧЕБНЫЕ ПОКАЗАТЕЛИ",21,Cyan);Bounds(vitalsTitle.rectTransform,48,-74,-48,-38);
            screens.vitalsText=Label(screens.vitalsRoot,"Blood pressure pulse oxygen temperature","",29,Color.white);Bounds(screens.vitalsText.rectTransform,48,-228,-48,-102);
            screens.pulseTrace=PulseLine(screens.vitalsRoot);
            screens.taskText=Label(screens.taskRoot,"Current action","",28,Color.white);Bounds(screens.taskText.rectTransform,20,-82,-20,-12);
            screens.taskHint=Label(screens.taskRoot,"Input hint","",22,Cyan);Bounds(screens.taskHint.rectTransform,20,-154,-190,-90);
            var next=Button(screens.taskRoot,"Далее →",new Vector2(300,-62),new Vector2(140,38));UnityEventTools.AddPersistentListener(next.onClick,hud.ContinueJourney);hud.continueButton=next;
            screens.groups=new[]{screens.theoryRoot.GetComponent<CanvasGroup>(),screens.vitalsRoot.GetComponent<CanvasGroup>(),screens.taskRoot.GetComponent<CanvasGroup>()};
            // Existing HUD remains the input/presentation authority; the new two
            // screens display its live mission data with theory visible by default.
            hud.panelRoot.GetComponent<Canvas>().enabled=false;
            hud.inputGuard.surfaces=new[]{screens.theoryRoot,screens.taskRoot};
            EditorUtility.SetDirty(hud);EditorUtility.SetDirty(hud.inputGuard);EditorUtility.SetDirty(screens);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);EditorSceneManager.SaveScene(hud.gameObject.scene);
            File.WriteAllText(ActiveInstrumentBuilder.Reports+"/HolographicScreens.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,sources=new[]{"futuristic+display+panel+3d+model.glb","futuristic+portal+frame+3d+model.glb"},realSourceRims=true,opaqueCenterRemoved=true,glassAlpha=.24f,textBackdropAlpha=.38f,theoryOpenByDefault=true,scroll="mouse wheel / right stick on panel / arrow buttons / PageUp PageDown",theoryPosition=screens.theoryRoot.localPosition.ToString(),theoryWidthMetres=.64f,hardwareVerified=false},Formatting.Indented));
        }
        static RectTransform CanvasRoot(Transform parent,string name,Camera camera,Vector2 size,Vector3 position,float scale,bool interactive)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;rect.sizeDelta=size;rect.localPosition=position;rect.localScale=Vector3.one*scale;
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;canvas.overrideSorting=true;canvas.sortingOrder=60;
            if(interactive){go.AddComponent<GraphicRaycaster>();go.AddComponent<ArteryTrackedRaycaster>();}else go.GetComponent<CanvasGroup>().blocksRaycasts=false;
            return rect;
        }
        static void Frame(RectTransform parent,string kind,float width)
        {
            string folder=Folder+"/"+kind;var d=JObject.Parse(File.ReadAllText(folder+"/FrameGeometry.json"));
            var mesh=new Mesh{name=kind+" supplied hollow rim"};mesh.vertices=((JArray)d["vertices"]).Select(v=>new Vector3((float)v[0],(float)v[1],(float)v[2])).ToArray();mesh.normals=((JArray)d["normals"]).Select(v=>new Vector3((float)v[0],(float)v[1],(float)v[2])).ToArray();mesh.uv=((JArray)d["uv"]).Select(v=>new Vector2((float)v[0],(float)v[1])).ToArray();mesh.triangles=((JArray)d["triangles"]).Select(v=>(int)v).ToArray();mesh.RecalculateBounds();JourneyGeometry.Save(mesh,folder+"/Frame.asset");
            string atlas=folder+"/FrameAtlas.png";AssetDatabase.ImportAsset(atlas);var importer=(TextureImporter)AssetImporter.GetAtPath(atlas);importer.mipmapEnabled=true;importer.maxTextureSize=1024;var android=importer.GetPlatformTextureSettings("Android");android.overridden=true;android.format=TextureImporterFormat.ASTC_6x6;android.maxTextureSize=1024;importer.SetPlatformTextureSettings(android);importer.SaveAndReimport();
            var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name=kind+" original GLB rim atlas",enableInstancing=true};mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(atlas));mat.SetColor("_BaseColor",new Color(.80f,1.05f,1.15f));mat.SetFloat("_Cull",0);JourneyGeometry.Save(mat,folder+"/Frame.mat");
            // Keep the rim's face outline but make its thickness shallow and
            // behind the UI plane, so it cannot depth-occlude headings/buttons.
            var frame=new GameObject("Actual GLB peripheral frame",typeof(MeshFilter),typeof(MeshRenderer));frame.transform.SetParent(parent,false);frame.transform.localScale=new Vector3(width,width,width*.08f);frame.transform.localPosition=Vector3.forward*12;frame.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/Frame.asset");var r=frame.GetComponent<MeshRenderer>();r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Frame.mat");r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
        }
        static Text Label(Transform parent,string name,string caption,int size,Color color)
        {var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(parent,false);var text=go.GetComponent<Text>();text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular.ttf");text.text=caption;text.fontSize=size;text.color=color;text.alignment=TextAnchor.UpperLeft;text.supportRichText=true;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;}
        static Image Image(Transform parent,string name,Color color)
        {var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.color=color;return image;}
        static Button Button(RectTransform parent,string label,Vector2 position,Vector2 size)
        {var image=Image(parent,label,new Color(.025f,.20f,.28f,.60f));image.rectTransform.anchoredPosition=position;image.rectTransform.sizeDelta=size;var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;var caption=Label(image.transform,"Caption",label,20,Cyan);Stretch(caption.rectTransform,6,3,6,3);caption.alignment=TextAnchor.MiddleCenter;return button;}
        static Scrollbar MakeScrollbar(RectTransform parent)
        {var track=Image(parent,"Theory scrollbar",new Color(.05f,.22f,.30f,.30f));track.rectTransform.anchorMin=new Vector2(1,0);track.rectTransform.anchorMax=new Vector2(1,1);track.rectTransform.offsetMin=new Vector2(-42,116);track.rectTransform.offsetMax=new Vector2(-26,-100);var bar=track.gameObject.AddComponent<Scrollbar>();var handle=Image(track.transform,"Scroll handle",new Color(.30f,.86f,1,.80f));bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;bar.direction=Scrollbar.Direction.BottomToTop;return bar;}
        static LineRenderer PulseLine(RectTransform parent)
        {var go=new GameObject("Live model pulse trace",typeof(LineRenderer));go.transform.SetParent(parent,false);var line=go.GetComponent<LineRenderer>();var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="Holographic pulse cyan"};mat.SetColor("_BaseColor",Cyan);JourneyGeometry.Save(mat,Folder+"/Pulse.mat");line.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Pulse.mat");line.useWorldSpace=false;line.widthMultiplier=.0014f;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return line;}
        static void Stretch(RectTransform rect,float left,float top,float right,float bottom){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(left,bottom);rect.offsetMax=new Vector2(-right,-top);}
        static void Bounds(RectTransform rect,float left,float bottom,float right,float top){rect.anchorMin=new Vector2(0,1);rect.anchorMax=new Vector2(1,1);rect.offsetMin=new Vector2(left,bottom);rect.offsetMax=new Vector2(right,top);}
    }
}
