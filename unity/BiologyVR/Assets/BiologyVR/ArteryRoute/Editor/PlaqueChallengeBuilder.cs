using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class PlaqueChallengeBuilder
    {
        public const string Folder=JourneyGeometry.Root+"/Generated/PlaqueChallenge";
        public const string Reports=JourneyGeometry.Root+"/Reports/PlaqueChallenge";
        [MenuItem("Biology VR/Install Growing Destructible Plaque Challenge")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();Directory.CreateDirectory(Folder);Directory.CreateDirectory(Reports+"/Backup");
            EditorSceneManager.SaveScene(world.gameObject.scene);string backup=Reports+"/Backup/ArteryNarrativeVR_before_growing_plaque.unity";
            if(!File.Exists(backup)&&!AssetDatabase.CopyAsset(world.gameObject.scene.path,backup))throw new IOException("Plaque scene backup failed");
            if(world.plaqueState){world.plaqueState.enabled=false;if(world.plaqueState.LegacyGeometryRoot)world.plaqueState.LegacyGeometryRoot.gameObject.SetActive(false);EditorUtility.SetDirty(world.plaqueState);}
            if(world.plaqueDetail){world.plaqueDetail.enabled=false;if(world.plaqueDetail.LegacyDetailRoot)world.plaqueDetail.LegacyDetailRoot.SetActive(false);EditorUtility.SetDirty(world.plaqueDetail);}
            var location=world.locationRoots[world.mover.path.locationIds[6-3]].transform;var old=location.Find("Scene06 — growing destructible plaque");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            for(int i=location.childCount-1;i>=0;i--){var child=location.GetChild(i);if(child.name.StartsWith("Reusable lipid impact shard ")||child.name=="Plaque impact expanding ring")UnityEngine.Object.DestroyImmediate(child.gameObject);}
            var challenge=world.GetComponent<JourneyPlaqueChallenge>();if(!challenge)challenge=world.gameObject.AddComponent<JourneyPlaqueChallenge>();challenge.world=world;world.plaqueChallenge=challenge;
            var site=new GameObject("Scene06 — growing destructible plaque");site.transform.SetParent(location,false);float arc=world.mover.path.Anchor(6)+5.2f;
            site.transform.SetPositionAndRotation(world.mover.path.Offset(arc,.55f,0),world.mover.path.Frame(arc));challenge.site=site.transform;challenge.occlusionSeconds=55;
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Sphere);var sphere=primitive.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(primitive);
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var cubeMesh=cube.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(cube);
            var lipid=Material("Lipid mass — warm cholesterol",new Color(.91f,.65f,.25f),.35f);
            var fibrous=Material("Fibrous plaque tissue",new Color(.72f,.40f,.30f),.12f);
            var repair=Material("Cleared intimal residue",new Color(.78f,.40f,.39f),.10f);
            challenge.bulk=Visual(site.transform,"Integrated fibrous lipid mound",sphere,fibrous,new Vector3(1.1f,0,.65f),new Vector3(4.3f,5.0f,1.9f)).transform;
            challenge.repairResidue=Visual(site.transform,"Restored open-lumen intimal patch",sphere,repair,new Vector3(3.0f,0,.6f),new Vector3(.18f,4.4f,2.0f)).transform;challenge.repairResidue.gameObject.SetActive(false);
            challenge.lipids=new JourneyPlaqueLipid[JourneyPlaqueChallenge.LipidCapacity];var random=new System.Random(61027);int layer=world.Find("lipid").gameObject.layer;
            for(int i=0;i<challenge.lipids.Length;i++)
            {
                float a=i*2.399963f,r=i<30?Mathf.Sqrt((i+.5f)/30)*2.25f:1.35f+(float)random.NextDouble()*1.85f;
                float x=Mathf.Cos(a)*r,y=Mathf.Sin(a)*r,z=i<20?-.45f+(float)random.NextDouble()*.42f:.30f+(float)random.NextDouble()*.60f;
                float size=.84f+(float)random.NextDouble()*.35f;
                var cell=new GameObject("Destructible cholesterol lobule "+i);cell.transform.SetParent(site.transform,false);cell.transform.localPosition=new Vector3(x,y,z);cell.transform.localRotation=Quaternion.Euler((float)random.NextDouble()*16-8,(float)random.NextDouble()*18-9,(float)random.NextDouble()*70);cell.transform.localScale=Vector3.one*size;cell.layer=layer;
                var target=cell.AddComponent<JourneyTarget>();target.targetId="lipid";target.label=i<30?"Липидный элемент "+(i+1):"Липид из кровотока";target.mission=world.mission;
                var contact=cell.AddComponent<SphereCollider>();contact.radius=.46f;contact.isTrigger=true;contact.enabled=false;
                var visual=Visual(cell.transform,"Opaque lobule surface",sphere,lipid,Vector3.zero,new Vector3(1,1,.58f));visual.layer=layer;
                var piece=cell.AddComponent<JourneyPlaqueLipid>();piece.owner=challenge;piece.index=i;piece.target=target;piece.contact=contact;piece.surface=visual.GetComponent<Renderer>();challenge.lipids[i]=piece;
            }
            var diagnosis=new GameObject("Actual growing plaque diagnosis contact");diagnosis.transform.SetParent(site.transform,false);diagnosis.layer=layer;var proxy=diagnosis.AddComponent<JourneyTarget>();proxy.targetId="plaque";proxy.label="Растущая стенозирующая бляшка";proxy.mission=world.mission;
            var diagnosisCollider=diagnosis.AddComponent<BoxCollider>();diagnosisCollider.center=new Vector3(0,0,.15f);diagnosisCollider.size=new Vector3(5.0f,5.0f,1.8f);diagnosisCollider.isTrigger=true;diagnosisCollider.enabled=false;challenge.diagnosisContact=diagnosisCollider;
            var originals=new[]{world.Find("lipid"),world.Find("cap"),world.Find("plaque")};challenge.legacyContacts=originals.SelectMany(t=>t.GetComponentsInChildren<Collider>(true)).ToArray();challenge.legacyVisuals=originals.SelectMany(t=>t.GetComponentsInChildren<Renderer>(true)).ToArray();
            foreach(var contact in challenge.legacyContacts)contact.enabled=false;foreach(var visual in challenge.legacyVisuals)visual.enabled=false;
            challenge.measurementContacts=world.Find("plaque-flow").GetComponentsInChildren<Collider>(true);challenge.modeContacts=world.Find("pulse-mode").GetComponentsInChildren<Collider>(true);
            challenge.measurementVisuals=world.Find("plaque-flow").GetComponentsInChildren<Renderer>(true);challenge.modeVisuals=world.Find("pulse-mode").GetComponentsInChildren<Renderer>(true);
            challenge.shards=new Transform[72];
            for(int i=0;i<challenge.shards.Length;i++){var shard=Visual(location,"Reusable lipid impact shard "+i,cubeMesh,lipid,Vector3.zero,Vector3.one*.07f);shard.SetActive(false);challenge.shards[i]=shard.transform;}
            var lineObject=new GameObject("Plaque impact expanding ring",typeof(LineRenderer));lineObject.transform.SetParent(location,false);var line=lineObject.GetComponent<LineRenderer>();line.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(ActiveInstrumentBuilder.Folder+"/TurquoiseVFX.mat");line.useWorldSpace=false;line.loop=true;line.positionCount=32;for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;line.SetPosition(i,new Vector3(Mathf.Cos(a)*.5f,Mathf.Sin(a)*.5f,0));}line.enabled=false;line.shadowCastingMode=ShadowCastingMode.Off;challenge.impactRing=line;
            site.SetActive(false);EditorUtility.SetDirty(world);EditorUtility.SetDirty(challenge);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);EditorSceneManager.SaveScene(world.gameObject.scene);
            File.WriteAllText(Reports+"/Authoring.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,initialLipids=30,reusableLipidSlots=challenge.lipids.Length,randomArrivalsPerSecond="1–2",occlusionSeconds=challenge.occlusionSeconds,reusableShards=challenge.shards.Length,rigidbodiesAdded=0,individualRealRayContacts=true,missionTargetsRegistry=world.targets.Length,phase="Flow-fed destructible plaque; original lobules do not enlarge",hardwareVerified=false},Formatting.Indented));
        }
        static Material Material(string name,Color color,float smoothness)
        {var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",smoothness);string path=Folder+"/"+name.Replace(" ","_")+".mat";JourneyGeometry.Save(mat,path);return AssetDatabase.LoadAssetAtPath<Material>(path);}
        static GameObject Visual(Transform parent,string name,Mesh mesh,Material mat,Vector3 position,Vector3 scale)
        {var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return go;}
    }
}
