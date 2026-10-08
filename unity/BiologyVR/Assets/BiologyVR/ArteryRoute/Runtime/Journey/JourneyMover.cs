using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyMover : MonoBehaviour
    {
        public ArteryJourneyPath path;
        public XROrigin origin;
        public Camera viewCamera;
        public JourneyMission mission;
        public Transform teachingOverlay;
        public float speed=1.2f;
        public bool debugFreeJump;
        public float Distance{get;private set;}
        public bool Moving{get;private set;}
        public float TransitionProgress{get;private set;}=1f;
        public bool UsingTrackedInput{get;private set;}
        public event Action Arrived;
        float from,to,time,duration,yaw,pitch;
        Vector3 headOffset;
        bool xr,paused,lastNext;
        Behaviour tracker;
        InputAction continueInput;
        void OnEnable()
        {
            if(continueInput==null)continueInput=new InputAction("Continue completed artery stage",InputActionType.Button,"<XRController>{RightHand}/secondaryButton");
            continueInput.Enable();
        }
        void OnDisable(){continueInput?.Disable();lastNext=false;}
        void OnDestroy(){continueInput?.Dispose();}
        void Start()
        {
            Distance=path.Anchor(3);
            foreach(var b in viewCamera.GetComponents<Behaviour>())if(b.GetType().Name=="TrackedPoseDriver")tracker=b;
            Tracking();Place();
        }
        void Tracking()
        {
            xr=XRSettings.isDeviceActive;UsingTrackedInput=xr;
            foreach(var device in InputSystem.devices)if(device.layout=="XRSimulatedHMD")UsingTrackedInput=true;
            if(tracker)tracker.enabled=UsingTrackedInput;
            headOffset=origin.transform.InverseTransformPoint(viewCamera.transform.position);
        }
        public void TravelTo(int scene,bool instant=false)
        {
            from=Distance;to=path.Anchor(scene);time=0;
            float length=Mathf.Abs(to-from);duration=length<6?3.5f:length<20?6f:9f;
            Moving=!instant;
            TransitionProgress=Moving?0f:1f;
            if(!Moving){Distance=to;Place();Arrived?.Invoke();}
        }
        public void Continue()=>mission?.Continue();
        public void TogglePause()=>paused=!paused;
        public void ToggleOverlay(){if(teachingOverlay)teachingOverlay.gameObject.SetActive(!teachingOverlay.gameObject.activeSelf);}
        void Update()
        {
            bool tracked=XRSettings.isDeviceActive;foreach(var device in InputSystem.devices)if(device.layout=="XRSimulatedHMD")tracked=true;
            if(xr!=XRSettings.isDeviceActive||tracked!=UsingTrackedInput)Tracking();
            var k=Keyboard.current;
            if(k!=null){if(k.rightArrowKey.wasPressedThisFrame)Continue();if(k.spaceKey.wasPressedThisFrame)TogglePause();if(k.oKey.wasPressedThisFrame)ToggleOverlay();
                if(k.pageDownKey.wasPressedThisFrame)mission.DebugSetScene(Mathf.Min(15,mission.SceneNumber+1));}
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton,out bool physicalNext);
            bool next=physicalNext||(continueInput!=null&&continueInput.IsPressed());if(next&&!lastNext)Continue();lastNext=next;
            if(Moving&&!paused){time+=Time.deltaTime;float a=Mathf.Clamp01(time/duration);TransitionProgress=a;Distance=Mathf.Lerp(from,to,a*a*(3-2*a));Place();if(a>=1){Moving=false;TransitionProgress=1f;Arrived?.Invoke();}}
            if(!UsingTrackedInput&&Mouse.current!=null&&Mouse.current.rightButton.isPressed){var d=Mouse.current.delta.ReadValue();yaw+=d.x*.12f;pitch=Mathf.Clamp(pitch-d.y*.12f,-70,70);viewCamera.transform.localRotation=Quaternion.Euler(pitch,yaw,0);}
        }
        void Place()
        {
            var eye=path.Centre(Distance)+path.Up(Distance)*.15f;
            bool inLab=mission&&mission.SceneNumber>=17&&mission.world&&mission.world.labRoom&&mission.world.labRoom.activeInHierarchy;
            if(inLab)
            {
                var room=mission.world.labRoom.transform.position;eye=room+new Vector3(0,1.6f,-2);
                origin.transform.position=eye-origin.transform.TransformVector(headOffset);return;
            }
            // Arterial travel is translation only. Head/controller tracking and
            // player-selected heading remain untouched through every episode.
            origin.transform.position=eye-origin.transform.TransformVector(headOffset);
        }
        public void RecenterInLaboratory(){Place();}
        // Guidance is drawn by JourneyHud so the player sees one consistent instruction panel.
    }
}
