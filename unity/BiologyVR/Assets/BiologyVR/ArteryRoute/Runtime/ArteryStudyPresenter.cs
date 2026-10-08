using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BiologyVR.ArteryRoute
{
    public sealed class ArteryStudyPresenter : MonoBehaviour
    {
        public static ArteryStudyPresenter Instance {get;private set;}
        public ArteryRouteController route;
        public Text titleText,lessonText,vitalsText,scanText;
        public GameObject[] studyTemplates;
        public Transform rightController;
        GameObject deck;
        bool previousTrigger;
        public int ScanCount {get;private set;}
        void Awake(){Instance=this;}
        void Start(){route.StationChanged+=Refresh;Refresh(route.station);}
        void OnDestroy(){if(route)route.StationChanged-=Refresh;if(Instance==this)Instance=null;}
        void Refresh(int station)
        {
            titleText.text=$"{station+3:00} / {ArteryRouteController.Titles[station]}";
            lessonText.text=ArteryRouteController.Lessons[station]+"\n\nУЧЕБНОЕ УВЕЛИЧЕНИЕ\nСначала наблюдаем, потом делаем вывод.";
            vitalsText.text="ЧСС | АД | SpO₂ | T\n"+ArteryRouteController.Vitals[station]+"\nСценарные значения";
            scanText.text="Grip: взять учебную модель\nTrigger / F: сканировать";
            if(deck)Destroy(deck);
            deck=new GameObject("Near-Hand Educational Models");
            var camera=route.viewCamera.transform;
            for(int i=0;i<studyTemplates.Length;i++)
            {
                var source=studyTemplates[i];if(!source)continue;
                var instance=Instantiate(source,deck.transform);instance.name=source.name+"_XR_Study";instance.SetActive(true);
                instance.transform.localScale=Vector3.one*(i==0?.12f:i==1?.17f:.34f);
                instance.transform.position=camera.position+camera.forward*1.05f+camera.right*((i-1)*.28f)-camera.up*.40f;
                instance.transform.rotation=camera.rotation*Quaternion.Euler(65,0,0);
                var bound=instance.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
                var collider=instance.AddComponent<SphereCollider>();collider.center=bound.center;collider.radius=Mathf.Max(bound.extents.x,bound.extents.y,bound.extents.z);
                var body=instance.AddComponent<Rigidbody>();body.useGravity=false;body.isKinematic=true;
                var grab=instance.AddComponent<XRGrabInteractable>();grab.throwOnDetach=false;grab.movementType=XRBaseInteractable.MovementType.Kinematic;grab.useDynamicAttach=true;
                var inspect=instance.AddComponent<CellInspectable>();
                inspect.title=i==0?"Эритроцит":i==1?"Лейкоцит":"Тромбоцит";
                inspect.description=i==0?"Закрытый двояковогнутый диск без ядра. Гемоглобин обратимо связывает кислород.":i==1?"Клетка иммунной системы. В учебном срезе можно рассмотреть ядро, гранулы и органоиды.":"Безъядерный клеточный фрагмент. При активации образует отростки и участвует в гемостазе.";
            }
        }
        public void ShowScan(CellInspectable cell){ScanCount++;scanText.text="СКАНИРОВАНИЕ ЗАВЕРШЕНО\n"+cell.title+"\n"+cell.description;}
        public void Scan()
        {
            Transform ray=rightController&&XRSettings.isDeviceActive?rightController:route.viewCamera.transform;
            if(Physics.Raycast(ray.position,ray.forward,out RaycastHit hit,30))
            {
                var cell=hit.collider.GetComponentInParent<CellInspectable>();if(cell){ShowScan(cell);return;}
                var node=hit.collider.GetComponentInParent<ArteryNode>();if(node&&!string.IsNullOrEmpty(node.presetId)){scanText.text="СКАНИРОВАНИЕ\n"+node.DisplayName;ScanCount++;return;}
            }
            if(deck){var cells=deck.GetComponentsInChildren<CellInspectable>();if(cells.Length>0)ShowScan(cells[0]);}
        }
        void Update()
        {
            if(Keyboard.current!=null&&Keyboard.current.fKey.wasPressedThisFrame)Scan();
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton,out bool trigger);
            if(trigger&&!previousTrigger)Scan();previousTrigger=trigger;
            if(deck)deck.SetActive(!route.IsTravelling);
        }
    }
}
