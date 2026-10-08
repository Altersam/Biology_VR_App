using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

namespace BiologyVR.ArteryRoute
{
    public sealed class ArteryRouteController : MonoBehaviour
    {
        public XROrigin origin;
        public Camera viewCamera;
        public bool autoTour;
        public float holdSeconds=12f,travelSeconds=24f;
        public int station;
        public GameObject teachingOverlay;
        public Transform[] episodes;
        public event Action<int> StationChanged;
        public float RouteY => routeY;
        public bool IsTravelling => moving;
        public bool IsPaused => paused;
        public static readonly string[] Titles={"Состав крови","Строение артерии","Кровоток и давление","Атеросклеротическая бляшка","Газовый эмбол","Знакомство с вирусом","Вирусная волна","Падение давления","Повреждение эндотелия","Гемостаз","Баланс гемостаза","Очистка и восстановление","Финальный вирусный очаг","Гомеостаз восстановлен"};
        public static readonly string[] Lessons={"Изучи эритроцит, лейкоцит и тромбоцит. Гемоглобин переносит кислород.","Эндотелий выстилает интиму. Медиа содержит мышечно-эластические структуры; адвентиция — соединительную ткань.","Q = vS. Модель Пуазейля — приближение для стационарного течения.","LDL, липидное ядро, пенистые клетки и фиброзная покрышка. Не повреждай покрышку.","Пузырёк газа может перекрыть более мелкий сосуд. Ловушка — учебная игровая модель.","Условный вирион: капсид, генетический материал, поверхностные структуры и эпитоп.","Распознать → пометить → передать фагоциту. Иммунные поля не разрезают вирусы.","92/60 → временная стабилизация. Сосудистое поле не устраняет причину потери давления.","Дефект стенки + утечка крови + снижение давления: свяжи три наблюдения.","Адгезия → активация → агрегация → фибриновая сеть. Тромбин преобразует фибриноген.","Остановить кровотечение и сохранить просвет. Плазмин участвует в фибринолизе.","Фагоциты удаляют детрит; эндотелий восстанавливает целостность.","Инфицированная клетка ≠ гигантский вирус. Эпитоп → антитело → фагоцит; T-клетка действует на источник.","Целостная стенка, равномерный поток, вирусная нагрузка 0%. Сравни с исходным состоянием."};
        public static readonly string[] Vitals={"72 | 120/80 | 98% | 36.6°C","72 | 120/80 | 98% | 36.6°C","72 | 120/80 | 98% | 36.6°C","78 | 122/82 | 98% | 36.7°C","76 | 118/76 | 97% | 36.6°C","76 | 118/76 | 98% | 36.8°C","82 | 118/76 | 98% | 37.8°C","88 | 92/60 | 97% | 37.1°C","98 | 92/60 | 97% | 37°C","96 | 104/68 | 97% | 36.9°C","82 | 118/76 | 98% | 36.7°C","78 | 112/74 | 98% | 36.7°C","110 | 135/85 | 95% | 38.3°C","72 | 120/80 | 98% | 36.8°C"};
        bool moving,paused,lastNext,lastBack,lastXR;
        float routeY,fromY,toY,elapsed,hold,yaw,pitch;
        Vector3 initialHeadOffset;
        Behaviour trackedPose;
        public static Vector3 Centre(float y)=>new(7.5f*Mathf.Sin(Mathf.PI*y/24f),.7f*Mathf.Sin(Mathf.PI*y/96f),y);
        public static Vector3 Forward(float y)=>new Vector3(7.5f*Mathf.PI/24f*Mathf.Cos(Mathf.PI*y/24f),.7f*Mathf.PI/96f*Mathf.Cos(Mathf.PI*y/96f),1).normalized;
        public static Vector3 Right(float y)=>Vector3.Cross(Vector3.up,Forward(y)).normalized;
        public static Vector3 Up(float y)=>Vector3.Cross(Forward(y),Right(y)).normalized;
        void Start()
        {
            station=Mathf.Clamp(station,0,13); routeY=station*24-4;
            if(!viewCamera)viewCamera=Camera.main;
            if(viewCamera) foreach(var b in viewCamera.GetComponents<Behaviour>()) if(b.GetType().Name=="TrackedPoseDriver") trackedPose=b;
            ConfigureTracking(); Place(); UpdateEpisodes(); StationChanged?.Invoke(station);
        }
        void ConfigureTracking()
        {
            lastXR=XRSettings.isDeviceActive;
            if(trackedPose) trackedPose.enabled=lastXR;
            if(!lastXR&&viewCamera){viewCamera.transform.localPosition=Vector3.zero;viewCamera.transform.localRotation=Quaternion.identity;}
            initialHeadOffset=origin&&viewCamera?origin.transform.InverseTransformPoint(viewCamera.transform.position):Vector3.zero;
        }
        public void GoTo(int id,bool immediate=false)
        {
            id=Mathf.Clamp(id,0,13); fromY=routeY; toY=id*24-4; elapsed=0;hold=0; station=id;
            moving=!immediate&&Mathf.Abs(fromY-toY)>.001f;
            if(immediate)routeY=toY;
            UpdateEpisodes(); StationChanged?.Invoke(station); if(!moving)Place();
        }
        public void NextStation()=>GoTo(station+1);
        public void PreviousStation()=>GoTo(station-1);
        public void TogglePause()=>paused=!paused;
        public void ToggleTour(){autoTour=!autoTour;paused=false;hold=0;}
        public void ToggleOverlay(){if(teachingOverlay)teachingOverlay.SetActive(!teachingOverlay.activeSelf);}
        void UpdateEpisodes()
        {
            if(episodes==null)return;
            int current=Mathf.Clamp(Mathf.RoundToInt((routeY+4)/24),0,13);
            for(int i=0;i<episodes.Length;i++) if(episodes[i]) episodes[i].gameObject.SetActive(Mathf.Abs(i-current)<=1||Mathf.Abs(i-station)<=1);
        }
        void Update()
        {
            if(lastXR!=XRSettings.isDeviceActive)ConfigureTracking();
            var k=Keyboard.current;
            if(k!=null){if(k.spaceKey.wasPressedThisFrame)TogglePause();if(k.rightArrowKey.wasPressedThisFrame)NextStation();if(k.leftArrowKey.wasPressedThisFrame)PreviousStation();if(k.tKey.wasPressedThisFrame)ToggleTour();if(k.oKey.wasPressedThisFrame)ToggleOverlay();if(k.homeKey.wasPressedThisFrame)GoTo(0,true);}
            var right=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);var left=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton,out bool next);left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton,out bool back);
            if(next&&!lastNext)NextStation();if(back&&!lastBack)PreviousStation();lastNext=next;lastBack=back;
            if(!paused){if(moving){elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/Mathf.Max(1,travelSeconds));routeY=Mathf.Lerp(fromY,toY,t*t*(3-2*t));Place();UpdateEpisodes();if(t>=1){moving=false;hold=0;}}
                else if(autoTour&&station<13){hold+=Time.deltaTime;if(hold>=holdSeconds)NextStation();}}
            if(!lastXR&&Mouse.current!=null&&Mouse.current.rightButton.isPressed){var d=Mouse.current.delta.ReadValue();yaw+=d.x*.12f;pitch=Mathf.Clamp(pitch-d.y*.12f,-70,70);Place();}
        }
        void Place()
        {
            if(!origin||!viewCamera)return;
            var eye=Centre(routeY)+Up(routeY)*.10f;
            var f=(Centre(routeY+8)+Up(routeY+8)*.20f-eye).normalized;
            if(lastXR)f=Vector3.ProjectOnPlane(f,Vector3.up).normalized;
            var rotation=Quaternion.LookRotation(f,Vector3.up)*(lastXR?Quaternion.identity:Quaternion.Euler(pitch,yaw,0));
            origin.transform.SetPositionAndRotation(eye-rotation*initialHeadOffset,rotation);
        }
        void OnGUI(){if(lastXR)return;GUI.Box(new Rect(10,10,600,90),$"BIOLOGY VR • {station+3:00}: {Titles[station]}\n← → станции • Space пауза • T автотур • O направляющие\nПКМ+мышь осмотреться • Home начало • F сканировать\nVR: Grip захват • Trigger исследование • B/Y следующая/предыдущая станция");}
    }
}
