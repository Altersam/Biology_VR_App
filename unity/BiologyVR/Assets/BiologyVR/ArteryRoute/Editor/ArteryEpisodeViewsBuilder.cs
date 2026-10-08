using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryEpisodeViewsBuilder
    {
        static string Folder=>ArteryScenePolish.Folder;
        public static void Apply(JourneyWorld w,Transform parent)
        {
            var bubbleShader=Shader.Find("BiologyVR/Mobile Gas Bubble");if(!bubbleShader)throw new System.InvalidOperationException("Gas bubble shader missing");
            var bubble=new Material(bubbleShader){name="Embolus_PearlShell",enableInstancing=true};bubble.SetColor("_BaseColor",new Color(.68f,.92f,1,.14f));bubble.SetColor("_RimColor",new Color(.87f,.96f,1,.90f));bubble.SetFloat("_RimPower",2.25f);
            JourneyGeometry.Save(bubble,Folder+"/Embolus_PearlShell.mat");bubble=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Embolus_PearlShell.mat");
            foreach(var r in w.bubble.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.name="Pearl gas embolus shell";shell.transform.SetParent(w.bubble,false);shell.transform.localRotation=Quaternion.identity;
            shell.transform.localScale=Vector3.one*(.48f/Mathf.Max(.001f,w.bubble.lossyScale.x));UnityEngine.Object.DestroyImmediate(shell.GetComponent<Collider>());
            shell.GetComponent<Renderer>().sharedMaterial=bubble;shell.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            var bubbleCollider=w.bubble.GetComponent<SphereCollider>();if(bubbleCollider){bubbleCollider.center=Vector3.zero;bubbleCollider.radius=.26f/Mathf.Max(.001f,w.bubble.lossyScale.x);}
            var dock=new GameObject("Scene07 stabilisation field");dock.transform.SetParent(parent,false);
            var ring=Ring(dock.transform,"Safe capture ring",.29f,new Color(.22f,.90f,.94f),true);
            var collider=dock.AddComponent<SphereCollider>();collider.isTrigger=true;collider.radius=.29f;
            if(w.trap)w.trap.gameObject.SetActive(false);w.trap=dock.transform;
            var tether=Ring(parent,"BioTool attraction tether",.1f,new Color(.20f,.80f,.96f),false);tether.loop=false;tether.positionCount=2;tether.useWorldSpace=true;tether.enabled=false;tether.widthMultiplier=.009f;
            var label=ArteryAnnotations.Label(dock.transform,"Capture field label","Стабилизационная зона");
            var view=parent.gameObject.AddComponent<JourneyEmbolusView>();view.world=w;view.dock=dock.transform;view.ring=ring;view.tether=tether;view.label=label;
            BuildVirus(w,parent);
            var feedback=w.mission.GetComponent<JourneyToolFeedback>();if(!feedback)feedback=w.mission.gameObject.AddComponent<JourneyToolFeedback>();
            feedback.mission=w.mission;feedback.ring=Ring(parent,"Confirmed study action",.13f,new Color(.40f,.96f,.90f),true);feedback.ring.enabled=false;
            feedback.shot=Ring(parent,"Scientific instrument pulse",.1f,new Color(.35f,.86f,.99f),false);feedback.shot.useWorldSpace=true;feedback.shot.loop=false;feedback.shot.positionCount=2;feedback.shot.widthMultiplier=.005f;feedback.shot.enabled=false;
            feedback.lockRing=Ring(parent,"Scanner target progress",.14f,new Color(.2f,.91f,.98f),true);feedback.lockRing.enabled=false;
            feedback.scanBeam=Ring(parent,"Scanner thin beam",.1f,new Color(.18f,.7f,.83f),false);feedback.scanBeam.useWorldSpace=true;feedback.scanBeam.loop=false;feedback.scanBeam.positionCount=2;feedback.scanBeam.widthMultiplier=.0025f;feedback.scanBeam.enabled=false;
            var input=w.mission.GetComponent<JourneyInput>();
            feedback.emitter=Ring(input.bioTool,"BioTool mode emitter",.032f,new Color(.20f,.90f,.97f),true);
            feedback.modeCaption=ArteryAnnotations.Label(input.bioTool,"BioTool mode caption","Базовый");feedback.modeCaption.fontSize=18;feedback.modeCaption.transform.localPosition=new Vector3(0,.06f,.04f);feedback.modeCaption.transform.localScale=Vector3.one*.00065f;
            feedback.audioSource=w.mission.GetComponent<AudioSource>();if(!feedback.audioSource)feedback.audioSource=w.mission.gameObject.AddComponent<AudioSource>();
            feedback.audioSource.playOnAwake=false;feedback.audioSource.spatialBlend=0;feedback.audioSource.volume=.45f;
            feedback.scanSound=AudioCue("ScanConfirm",640,.11f);feedback.pulseSound=AudioCue("ScientificPulse",310,.085f);feedback.modeSound=AudioCue("ToolModeClick",440,.045f);feedback.warningSound=AudioCue("SoftTargetWarning",190,.10f);
        }
        static AudioClip AudioCue(string name,float frequency,float duration)
        {
            const int rate=22050;int count=Mathf.CeilToInt(rate*duration);string path=Folder+"/"+name+".wav";
            using(var memory=new System.IO.MemoryStream())
            using(var writer=new System.IO.BinaryWriter(memory))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                for(int i=0;i<count;i++){float t=i/(float)rate,phase=i/(float)count;float envelope=Mathf.Sin(Mathf.PI*phase)*Mathf.Exp(-phase*3);writer.Write((short)(Mathf.Sin(Mathf.PI*2*frequency*t+t*t*frequency)*envelope*6000));}
                System.IO.File.WriteAllBytes(path,memory.ToArray());
            }
            AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        static void BuildVirus(JourneyWorld w,Transform parent)
        {
            var root=new GameObject("Scene08 persistent virus inspection");root.transform.SetParent(parent,false);
            var full=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Normalized_POLISH_Virus_LOD2.asset");
            var cut=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Normalized_POLISH_VirusCutaway_LOD0.asset");
            if(!full||!cut)throw new System.InvalidOperationException("Polished virus meshes required");
            var violet=Lit("Virus_Capsid_Lavender",new Color(.60f,.38f,.82f),.34f);
            var coral=Lit("Virus_Genome_Coral",new Color(.98f,.45f,.38f),.34f);
            var gold=Lit("Virus_Epitope_Gold",new Color(.99f,.77f,.33f),.36f);
            var intact=Visual(root.transform,"Intact study virion",full,violet,.36f);
            var section=Visual(root.transform,"Cutaway study capsid",cut,violet,.36f);
            var library=AssetDatabase.LoadAssetAtPath<GameObject>(BuildArteryVrScene.Root+"/Imported/Visual/HybridArteryVisual.prefab");
            var filters=library.GetComponentsInChildren<MeshFilter>(true);
            var genetic=filters.FirstOrDefault(f=>f.name=="AB_S08_Genome_RNA"||f.name=="AB_S08_RNA"||f.name.Contains("S08_RNA")&&!f.name.Contains("Label"));
            if(!genetic)genetic=filters.FirstOrDefault(f=>f.name=="AB_S08_Virion_Cutaway");
            if(!genetic)throw new System.InvalidOperationException("Imported Scene08 cutaway model required");
            var genomeLine=Ring(root.transform,"RNA teaching strand",.10f,new Color(.99f,.62f,.42f),false);
            genomeLine.loop=false;genomeLine.positionCount=72;genomeLine.widthMultiplier=.012f;
            for(int i=0;i<72;i++){float t=i/71f;float a=t*Mathf.PI*8;genomeLine.SetPosition(i,new Vector3(Mathf.Sin(a)*.075f,(t-.5f)*.20f,Mathf.Cos(a)*.075f-.035f));}
            Renderer genome=genomeLine;
            var antigen=filters.FirstOrDefault(f=>f.name=="AB_S08_Antigen_0");if(!antigen)throw new System.InvalidOperationException("Imported antigen model required");
            var epitope=Visual(root.transform,"Highlighted antigen epitope",antigen.sharedMesh,gold,.09f);epitope.transform.localPosition=new Vector3(.16f,.04f,-.03f);
            var targets=new[]{"virus-study","genome","capsid","epitope"}.Select(id=>w.targets.First(t=>t.targetId==id)).ToArray();
            foreach(var t in targets)foreach(var r in t.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            var caption=ArteryAnnotations.Label(root.transform,"Virus inspection caption","Учебная модель");
            var inspection=parent.gameObject.AddComponent<JourneyVirusInspection>();inspection.Configure(w,root.transform,intact,section,genome,epitope,targets,caption);w.virusInspection=inspection;
            foreach(var t in targets)
            {
                var c=t.GetComponent<SphereCollider>();if(c){c.center=Vector3.zero;c.radius=.22f/Mathf.Max(.001f,t.transform.lossyScale.x);}
            }
        }
        static Renderer Visual(Transform parent,string name,Mesh mesh,Material mat,float diameter)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=Enumerable.Repeat(mat,mesh.subMeshCount).ToArray();r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            float scale=diameter/Mathf.Max(mesh.bounds.size.x,Mathf.Max(mesh.bounds.size.y,mesh.bounds.size.z));go.transform.localScale=Vector3.one*scale;go.transform.localPosition=-mesh.bounds.center*scale;return r;
        }
        internal static Material Lit(string name,Color color,float smooth)
        {var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);JourneyGeometry.Save(m,Folder+"/"+name+".mat");return AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+name+".mat");}
        internal static LineRenderer Ring(Transform parent,string name,float radius,Color color,bool loop)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=loop;line.positionCount=48;line.widthMultiplier=.015f;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            string path=Folder+"/"+name.Replace(" ","_")+".mat";var m=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name=name};m.SetColor("_BaseColor",color);JourneyGeometry.Save(m,path);line.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(path);
            for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));}return line;
        }
    }
}
