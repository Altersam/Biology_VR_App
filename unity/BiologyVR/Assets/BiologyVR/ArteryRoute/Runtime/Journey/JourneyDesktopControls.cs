using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Native mouse cursor drives the same XRI grab/release lifecycle as a VR hand.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed class JourneyDesktopControls:MonoBehaviour
    {
        public JourneyInput input;
        JourneyDesktopInteractor hand;
        Transform aim;
        float depth=1.5f;
        bool mouseGrab;
        readonly RaycastHit[] hits=new RaycastHit[64];
        public bool Active=>input&&input.mover&&!Application.isMobilePlatform&&!XRSettings.isDeviceActive&&!input.mover.UsingTrackedInput&&!input.useExplicitAimSources;
        public bool GrabHeld=>Active&&mouseGrab;
        public float GrabDepth=>depth;
        public const float DepthPerWheelStep=.15f;
        public static float WheelUnitsPerStep=>InputSystem.settings.scrollDeltaBehavior==InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange&&(Application.platform==RuntimePlatform.WindowsEditor||Application.platform==RuntimePlatform.WindowsPlayer)?120f:1f;
        public static float WheelSteps=>Mouse.current!=null?Mouse.current.scroll.ReadValue().y/WheelUnitsPerStep:0;
        public bool Holding=>hand&&hand.hasSelection;
        public Transform AimTransform{get{UpdateAim();return aim;}}
        public Vector2 PointerPosition=>Mouse.current!=null&&!Mouse.current.rightButton.isPressed?Mouse.current.position.ReadValue():new Vector2(Screen.width*.5f,Screen.height*.5f);
        void Awake()
        {
            if(!input)input=GetComponent<JourneyInput>();
            aim=new GameObject("Desktop cursor instrument aim").transform;aim.SetParent(transform,false);
            var go=new GameObject("Desktop mouse XRI hand");go.transform.SetParent(input.mover.origin.transform,false);
            hand=go.AddComponent<JourneyDesktopInteractor>();hand.controls=this;
            hand.interactionManager=FindFirstObjectByType<XRInteractionManager>();
        }
        public Ray PointerRay()
        {
            var camera=input.mover.viewCamera;
            var point=PointerPosition;point.x=Mathf.Clamp(point.x,0,Screen.width);point.y=Mathf.Clamp(point.y,0,Screen.height);
            return camera.ScreenPointToRay(point);
        }
        void UpdateAim()
        {
            if(!aim||!input||!input.mover||!input.mover.viewCamera)return;
            var ray=PointerRay();aim.SetPositionAndRotation(input.mover.viewCamera.transform.position,Quaternion.LookRotation(ray.direction,input.mover.viewCamera.transform.up));
        }
        void Update()
        {
            if(!Active){mouseGrab=false;return;}
            UpdateAim();var mouse=Mouse.current;if(mouse==null)return;
            // Like a VR grip, a held mouse button may acquire a moving object
            // when it enters the pointer ray; acquisition is not one-frame-only.
            if(mouse.leftButton.isPressed&&!mouseGrab)
            {
                var target=HoveredGrab();
                if(target){depth=Mathf.Clamp(Vector3.Distance(input.mover.viewCamera.transform.position,target.transform.position),.35f,10);mouseGrab=true;}
            }
            if(!mouse.leftButton.isPressed)mouseGrab=false;
            if(Holding)depth=Mathf.Clamp(depth+WheelSteps*DepthPerWheelStep,.35f,10);
            var ray=PointerRay();hand.transform.SetPositionAndRotation(input.mover.viewCamera.transform.position+ray.direction*depth,aim.rotation);
        }
        public XRGrabInteractable HoveredGrab()
        {
            if(!Active||!input.mission||!input.mission.Ready)return null;
            foreach(var target in input.world.targets)if(target&&target.DesktopHeld)return null;
            var ray=PointerRay();int count=Physics.RaycastNonAlloc(ray,hits,35,~0,QueryTriggerInteraction.Collide);
            if(count==hits.Length)return null;
            System.Array.Sort(hits,0,count,Comparer<RaycastHit>.Create((a,b)=>a.distance.CompareTo(b.distance)));
            for(int i=0;i<count;i++)
            {
                var collider=hits[i].collider;if(!collider||collider.transform.IsChildOf(input.mover.origin.transform))continue;
                var grab=collider.GetComponentInParent<XRGrabInteractable>();
                if(grab&&grab.isActiveAndEnabled)return grab;
                if(!collider.isTrigger)return null;
            }
            return null;
        }
        public bool ReleaseMouseGrab(){if(!Holding&&!mouseGrab)return false;mouseGrab=false;return true;}
        void OnApplicationFocus(bool focused){if(!focused)mouseGrab=false;}
        void OnDisable(){mouseGrab=false;}
    }

    public sealed class JourneyDesktopInteractor:XRBaseInteractor
    {
        public JourneyDesktopControls controls;
        public override bool isSelectActive=>controls&&controls.GrabHeld;
        public override bool isHoverActive=>controls&&controls.Active;
        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            targets.Clear();if(!controls||!controls.Active)return;
            if(hasSelection){foreach(var selected in interactablesSelected)targets.Add(selected);return;}
            var grab=controls.HoveredGrab();if(grab)targets.Add(grab);
        }
    }
}
