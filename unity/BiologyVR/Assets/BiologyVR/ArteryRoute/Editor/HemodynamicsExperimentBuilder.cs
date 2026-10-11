using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class HemodynamicsExperimentBuilder
    {
        const string Folder=JourneyGeometry.Root+"/Generated/HemodynamicsExperiment";
        public const string Reports=JourneyGeometry.Root+"/Reports/Polish/HemodynamicsExperiment";
        [MenuItem("Biology VR/Install In Vessel Hemodynamics Experiment")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();Directory.CreateDirectory(Folder);Directory.CreateDirectory(Reports);
            EditorSceneManager.SaveScene(world.gameObject.scene);string backup=Reports+"/Before_Hemodynamics_Experiment.unity";if(!File.Exists(backup)&&!AssetDatabase.CopyAsset(world.gameObject.scene.path,backup))throw new IOException("Experiment backup failed");
            var old=world.transform.Find("In-vessel hemodynamics AR experiment");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("In-vessel hemodynamics AR experiment");root.transform.SetParent(world.transform,false);
            var experiment=world.GetComponent<JourneyHemodynamicsExperiment>();if(!experiment)experiment=world.gameObject.AddComponent<JourneyHemodynamicsExperiment>();experiment.world=world;experiment.arRoot=root;world.hemodynamics=experiment;
            float arc=world.mover.path.Anchor(5)+3.2f;root.transform.SetPositionAndRotation(world.mover.path.Centre(arc),world.mover.path.Frame(arc));
            var cyan=Material("AR cyan",new Color(.18f,.88f,1));var dark=Material("Hypothesis graphite",new Color(.03f,.13f,.19f));var projection=ProjectionMaterial();
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Sphere);var sphere=primitive.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(primitive);
            string[] ids={"hdx-p1","hdx-p2","hdx-speed","hdx-diameter","hdx-wall","hdx-check1","hdx-check2","hdx-final","hdx-forward"};
            Vector3[] positions={new Vector3(2.45f,0,-1.4f),new Vector3(2.45f,0,1.4f),new Vector3(2.45f,-.65f,0),new Vector3(2.45f,.65f,0),new Vector3(2.45f,1.2f,0),new Vector3(2.45f,0,1.4f),new Vector3(2.45f,-.65f,0),new Vector3(2.45f,.65f,0),new Vector3(2.45f,0,2.0f)};
            experiment.probes=new JourneyTarget[ids.Length];
            for(int i=0;i<ids.Length;i++)experiment.probes[i]=Probe(root.transform,ids[i],positions[i],world,cyan,sphere);
            experiment.toneContact=Probe(root.transform,"hdx-tone",new Vector3(2.5f,-.2f,0),world,cyan,sphere);
            world.targets=world.targets.Where(t=>t&&!t.targetId.StartsWith("hdx-")).Concat(experiment.probes).Append(experiment.toneContact).ToArray();
            experiment.readings=new Text[5];string[] headings={"A / давление до","B / давление после","v / скорость","Ø / диаметр","Стенка / осмотр"};
            for(int i=0;i<5;i++){var text=WorldText(root.transform,headings[i],headings[i],26);text.transform.localPosition=positions[i]+Vector3.left*.16f+Vector3.up*.20f;experiment.readings[i]=text;}
            experiment.sectionCaption=WorldText(root.transform,"Section instructions","УЧЕБНЫЙ УЧАСТОК / A → B",23);experiment.sectionCaption.transform.localPosition=new Vector3(2.2f,1.6f,0);
            experiment.formulaCaption=WorldText(root.transform,"Discovered resistance relation","R ∝ 1/r⁴",25);experiment.formulaCaption.transform.localPosition=new Vector3(2.2f,-1.2f,.65f);
            experiment.rings=new LineRenderer[3];for(int i=0;i<3;i++)experiment.rings[i]=Line(root.transform,"Measurement ring "+i,projection,.005f,true);
            experiment.streamlines=new LineRenderer[8];for(int i=0;i<8;i++)experiment.streamlines[i]=Line(root.transform,"Scanner diagnostic streamline "+i,projection,.003f,false);
            experiment.tracers=new Transform[7];for(int i=0;i<7;i++){var go=Mesh(root.transform,"Diagnostic tracer "+i,sphere,cyan);go.transform.localScale=Vector3.one*.033f;go.SetActive(false);experiment.tracers[i]=go.transform;}
            BuildCards(experiment,root.transform,sphere,dark,cyan);
            BuildInterface(experiment);
            EditorUtility.SetDirty(world);EditorUtility.SetDirty(experiment);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);EditorSceneManager.SaveScene(world.gameObject.scene);
            File.WriteAllText(Reports+"/Authoring.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,early="in-vessel experiment / fixed Q for narrow and wide cases / reduced supply mystery",late="independent local diagnosis / no automatic vasoconstriction",model="Rrel=r^-4; fixed-Q cases ΔP=8Rrel and vrel=r^-2; normal-diameter mystery ΔP=5, Qrel=5/8",physicalCards=3,scannerFlowTracers=7,streamlines=8,legacyFlowHologramHidden=true,compactTheory=true,globalMetrics=3,pool=world.GetComponent<JourneyBloodFlow>().cells.Length,registeredTargets=world.targets.Length,hardwareVerified=false},Formatting.Indented));
        }
        static JourneyTarget Probe(Transform parent,string id,Vector3 point,JourneyWorld world,Material material,Mesh mesh)
        {
            var go=new GameObject("AR measurement contact / "+id);go.transform.SetParent(parent,false);go.transform.localPosition=point;go.layer=LayerMask.NameToLayer("JourneyTarget");var contact=go.AddComponent<SphereCollider>();contact.radius=.20f;contact.isTrigger=true;
            var target=go.AddComponent<JourneyTarget>();target.targetId=id;target.label=id;target.mission=world.mission;var marker=Mesh(go.transform,"Small AR contact",mesh,material);marker.transform.localScale=Vector3.one*.065f;return target;
        }
        static void BuildCards(JourneyHemodynamicsExperiment experiment,Transform parent,Mesh mesh,Material dark,Material cyan)
        {
            var board=new GameObject("Physical hemodynamic hypothesis cards");board.transform.SetParent(parent,false);board.transform.localPosition=new Vector3(.75f,.85f,-.9f);board.transform.localRotation=Quaternion.identity;experiment.cardBoard=board;
            var slot=Mesh(board.transform,"Hypothesis submission slot",mesh,cyan);slot.transform.localPosition=new Vector3(0,.20f,0);slot.transform.localScale=new Vector3(.28f,.20f,.025f);experiment.cardSlot=slot.transform;
            var caption=WorldText(board.transform,"Hypothesis instructions","СОПОСТАВЬ ДАННЫЕ / ВЫБЕРИ ГИПОТЕЗУ",20);caption.transform.localPosition=new Vector3(0,.53f,0);
            experiment.cards=new JourneyHemodynamicsCard[3];int[] codes={1,2,0};
            for(int i=0;i<3;i++)
            {
                var go=new GameObject("Physical hypothesis "+codes[i]);go.transform.SetParent(board.transform,false);go.transform.localPosition=new Vector3((i-1)*.40f,-.24f,-.08f);
                var chip=Mesh(go.transform,"Hypothesis chip",mesh,dark);chip.transform.localScale=new Vector3(.33f,.19f,.045f);
                var body=go.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;var collider=go.AddComponent<SphereCollider>();collider.radius=.15f;
                var grab=go.AddComponent<XRGrabInteractable>();grab.movementType=XRBaseInteractable.MovementType.Kinematic;grab.useDynamicAttach=true;grab.throwOnDetach=false;grab.colliders.Add(collider);
                var card=go.AddComponent<JourneyHemodynamicsCard>();card.owner=experiment;card.code=codes[i];card.grab=grab;card.home=go.transform.localPosition;experiment.cards[i]=card;
                var label=WorldText(go.transform,"Hypothesis caption",codes[i]==0?"СУЖЕНИЕ\nУЧАСТКА":codes[i]==1?"РАСШИРЕНИЕ\nУЧАСТКА":"ПРИЧИНА\nНЕ ЗДЕСЬ",21);label.transform.localPosition=new Vector3(0,0,-.035f);label.transform.localScale=Vector3.one*.00155f;
            }
            board.SetActive(false);
        }
        static void BuildInterface(JourneyHemodynamicsExperiment experiment)
        {
            var camera=experiment.world.mover.viewCamera;var screens=experiment.world.mission.GetComponent<JourneyHolographicScreens>();var old=camera.transform.Find("Compact hemodynamic experiment UI");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("Compact hemodynamic experiment UI");root.transform.SetParent(camera.transform,false);
            var display=experiment.GetComponent<JourneyHemodynamicsInterface>();if(!display)display=experiment.gameObject.AddComponent<JourneyHemodynamicsInterface>();display.experiment=experiment;display.screens=screens;experiment.display=display;
            var theory=Panel(root.transform,"Compact hemodynamic task",camera,new Vector2(600,290),new Vector3(-.61f,.14f,1.78f));var vitals=Panel(root.transform,"Three systemic metrics",camera,new Vector2(680,180),new Vector3(.67f,.49f,1.8f));display.compactTheory=theory.gameObject;display.compactVitals=vitals.gameObject;
            display.title=Text(theory,"Experiment title",22,new Vector2(560,35),new Vector2(0,112));display.explanation=Text(theory,"Two line theory",25,new Vector2(550,75),new Vector2(0,53));display.task=Text(theory,"Current experiment task",26,new Vector2(550,82),new Vector2(0,-36));display.progress=Text(theory,"Experiment progress",18,new Vector2(240,25),new Vector2(-145,-116));
            var next=new GameObject("Continue experiment",typeof(RectTransform),typeof(CanvasRenderer),typeof(JourneyRoundedGraphic),typeof(Button));next.transform.SetParent(theory,false);var rect=(RectTransform)next.transform;rect.sizeDelta=new Vector2(180,36);rect.anchoredPosition=new Vector2(180,-114);var image=next.GetComponent<JourneyRoundedGraphic>();image.color=new Color(.05f,.32f,.4f,.6f);image.raycastTarget=true;image.radius=7;var button=next.GetComponent<Button>();button.targetGraphic=image;UnityEventTools.AddPersistentListener(button.onClick,screens.hud.ContinueJourney);Text(rect,"Continue label",20,new Vector2(175,32),Vector2.zero).text="Далее →";display.continueButton=next;
            display.values=new Text[3];for(int i=0;i<3;i++){Text(vitals,"Metric label "+i,20,new Vector2(180,28),new Vector2((i-1)*218,48)).text=new[]{"АД / мм рт. ст.","ПУЛЬС / уд/мин","ПОТОК / Q₀"}[i];display.values[i]=Text(vitals,"Metric value "+i,45,new Vector2(195,62),new Vector2((i-1)*218,-5));}
            foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))graphic.material=screens.theoryTitle.material;
            screens.hud.inputGuard.surfaces=screens.hud.inputGuard.surfaces.Where(s=>s&&s.name!="Compact hemodynamic task").Append(theory).ToArray();EditorUtility.SetDirty(screens.hud.inputGuard);EditorUtility.SetDirty(display);theory.gameObject.SetActive(false);vitals.gameObject.SetActive(false);
        }
        static RectTransform Panel(Transform parent,string name,Camera camera,Vector2 size,Vector3 position)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;rect.sizeDelta=size;rect.localScale=Vector3.one*.001f;rect.localPosition=position;var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;canvas.overrideSorting=true;canvas.sortingOrder=61;go.GetComponent<CanvasScaler>().dynamicPixelsPerUnit=2.5f;go.AddComponent<ArteryTrackedRaycaster>();
            var glass=new GameObject("Compact holographic glass",typeof(RectTransform),typeof(CanvasRenderer),typeof(JourneyRoundedGraphic));glass.transform.SetParent(rect,false);var gr=(RectTransform)glass.transform;gr.anchorMin=Vector2.zero;gr.anchorMax=Vector2.one;gr.offsetMin=gr.offsetMax=Vector2.zero;var image=glass.GetComponent<JourneyRoundedGraphic>();image.color=new Color(.02f,.08f,.13f,.4f);image.radius=8;image.raycastTarget=false;
            var frame=new GameObject("Projection brackets",typeof(RectTransform),typeof(CanvasRenderer),typeof(JourneyHolographicFrameGraphic));frame.transform.SetParent(rect,false);var fr=(RectTransform)frame.transform;fr.anchorMin=Vector2.zero;fr.anchorMax=Vector2.one;fr.offsetMin=fr.offsetMax=Vector2.zero;frame.GetComponent<JourneyHolographicFrameGraphic>().color=HolographicStyle.Cyan;frame.GetComponent<JourneyHolographicFrameGraphic>().raycastTarget=false;return rect;
        }
        static Text Text(Transform parent,string name,int fontSize,Vector2 size,Vector2 position)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;rect.sizeDelta=size;rect.anchoredPosition=position;var text=go.GetComponent<Text>();text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular.ttf");text.fontSize=fontSize;text.fontStyle=FontStyle.Bold;text.color=HolographicStyle.White;text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;return text;
        }
        static Text WorldText(Transform parent,string name,string caption,int size)
        {var text=ArteryAnnotations.Label(parent,name,caption);text.fontSize=size;text.fontStyle=FontStyle.Bold;text.rectTransform.sizeDelta=new Vector2(420,100);text.transform.localScale=Vector3.one*.0022f;text.gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit=2.5f;return text;}
        static LineRenderer Line(Transform parent,string name,Material material,float width,bool loop)
        {var go=new GameObject(name,typeof(LineRenderer));go.transform.SetParent(parent,false);var line=go.GetComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;line.loop=loop;line.widthMultiplier=width;line.shadowCastingMode=ShadowCastingMode.Off;line.enabled=false;return line;}
        static GameObject Mesh(Transform parent,string name,Mesh mesh,Material material)
        {var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;return go;}
        static Material Material(string name,Color color)
        {var material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name=name,enableInstancing=true};material.SetColor("_BaseColor",color);JourneyGeometry.Save(material,Folder+"/"+name.Replace(" ","_")+".mat");return AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+name.Replace(" ","_")+".mat");}
        static Material ProjectionMaterial()
        {
            var material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="Thin diagnostic AR projection",enableInstancing=true,renderQueue=3000};
            material.SetColor("_BaseColor",new Color(.18f,.88f,1,.38f));material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");JourneyGeometry.Save(material,Folder+"/AR_Projection.mat");return AssetDatabase.LoadAssetAtPath<Material>(Folder+"/AR_Projection.mat");
        }
    }
}
