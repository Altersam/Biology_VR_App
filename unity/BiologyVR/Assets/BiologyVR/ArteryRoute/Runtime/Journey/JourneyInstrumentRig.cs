using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Distinct diagnostic beam, travelling pulse, converging capture and tonus field.</summary>
    [DefaultExecutionOrder(1800)]
    public sealed class JourneyInstrumentRig:MonoBehaviour
    {
        public JourneyInput input;
        public BioToolModeController modes;
        public JourneyInstrumentVisual scannerVR,bioVR,scannerPC,bioPC;
        public LineRenderer scannerBeam,impulseBeam,captureTether,fieldRing,pulseRing;
        public LineRenderer captureLockRing;
        public LineRenderer[] captureRays;
        public LineRenderer scannerGuide,bioGuide;
        public bool ScannerGuideVisible=>scannerGuide&&scannerGuide.enabled;
        public bool BioGuideVisible=>bioGuide&&bioGuide.enabled;
        public bool ScannerBeamVisible=>scannerBeam&&scannerBeam.enabled;
        public bool CaptureBeamVisible=>captureTether&&captureTether.enabled;
        public bool PulseVisible=>pulseRing&&pulseRing.enabled;
        public JourneyInstrumentVisual ActiveScanner{get;private set;}
        public JourneyInstrumentVisual ActiveBioTool{get;private set;}
        public int ImpulseBursts{get;private set;}
        float shotAge=1,shotDuration=.24f;
        float cellReleaseAge=1;
        Vector3 shotEnd;
        Vector3 bioPCMount;
        void Awake(){if(bioPC)bioPCMount=bioPC.transform.localPosition;scannerGuide=CreateGuide("Persistent Scanner aim guide");bioGuide=CreateGuide("Persistent BioTool aim guide");}
        LineRenderer CreateGuide(string name)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=scannerBeam.sharedMaterial;line.useWorldSpace=true;line.positionCount=2;line.widthMultiplier=.0012f;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;line.enabled=false;return line;
        }
        public void ModeChanged(){if(bioVR)bioVR.BeginModeTransition();if(bioPC)bioPC.BeginModeTransition();}
        public void PhagocyteReleased(){cellReleaseAge=0;if(UnityEngine.XR.XRSettings.isDeviceActive){var device=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);if(device.isValid)device.SendHapticImpulse(0,.09f,.03f);}}
        void LateUpdate()
        {
            if(!input||!input.mission||!input.mover||!input.mover.viewCamera)return;
            bool tracked=input.useExplicitAimSources||input.mover.UsingTrackedInput;
            bool scan=input.ScannerHeld&&input.enabled;bool fire=input.ToolHeld&&input.enabled;
            var goal=input.mission.Goals!=null?input.mission.Current:null;
            bool showScanner=scan||(!fire&&goal!=null&&goal.action==StudyAction.Scan);
            scannerVR.gameObject.SetActive(tracked);bioVR.gameObject.SetActive(tracked);
            scannerPC.gameObject.SetActive(!tracked&&showScanner);bioPC.gameObject.SetActive(!tracked&&!showScanner);
            ActiveScanner=tracked?scannerVR:scannerPC;ActiveBioTool=tracked?bioVR:bioPC;
            // Aiming is independent of trigger hold and objective acceptance.
            if(tracked)
            {
                var scannerAimEnd=input.GuideEndpoint(input.scanner,out bool scanContact);var bioEnd=input.GuideEndpoint(input.bioTool,out bool bioContact);
                Draw(scannerGuide,input.scanner.position,scannerAimEnd,true,scanContact?.0017f:.0012f);
                Draw(bioGuide,input.bioTool.position,bioEnd,true,bioContact?.0017f:.0012f);
                ColorGuide(scannerGuide,input.ScannerBlockedByUi?HolographicStyle.Amber:HolographicStyle.Cyan);
                ColorGuide(bioGuide,input.ToolBlockedByUi||modes&&!modes.AllowsCurrentGoal()?HolographicStyle.Amber:HolographicStyle.Cyan);
            }
            else{scannerGuide.enabled=false;bioGuide.enabled=false;}
            if(!tracked)
            {
                var camera=input.mover.viewCamera.transform;var aim=input.DesktopControls?input.DesktopControls.AimTransform:camera;
                var direction=aim?aim.forward:camera.forward;
                if(scannerPC.gameObject.activeSelf)scannerPC.transform.rotation=Quaternion.Slerp(scannerPC.transform.rotation,Quaternion.LookRotation(direction,camera.up),1-Mathf.Exp(-Time.deltaTime*12));
                if(bioPC.gameObject.activeSelf)bioPC.transform.rotation=Quaternion.Slerp(bioPC.transform.rotation,Quaternion.LookRotation(direction,camera.up),1-Mathf.Exp(-Time.deltaTime*12));
            }
            var mode=modes?modes.CurrentMode:BioToolMode.Impulse;bool plateletHeld=input.plateletCapture&&input.plateletCapture.Holding;bool phagHeld=input.world.immunity&&input.world.immunity.Held;bool captured=input.world.EmbolusCaptured||plateletHeld||phagHeld;
            string caption=modes?modes.Caption:"IMPULSE";bool popup=modes&&Time.time<modes.ModePopupUntil;
            ActiveScanner.Present(scan,false,input.ScanProgress,mode,input.world.hemodynamics&&input.world.hemodynamics.Active?"SCANNER · FLOW":"SCANNER · диагностика",scan);
            ActiveBioTool.Present(fire,captured,input.ChargeProgress,mode,caption,popup);
            FaceCaption(ActiveScanner,input.mover.viewCamera.transform);
            FaceCaption(ActiveBioTool,input.mover.viewCamera.transform);
            var target=input.LockedTarget;var cameraRef=input.mover.viewCamera;
            Vector3 start=ActiveScanner.muzzle.position;
            Vector3 scanEnd=target?target.transform.position:start+ActiveScanner.muzzle.forward*.40f;
            scanEnd=start+(scanEnd-start).normalized*Mathf.Min(.45f,Vector3.Distance(start,scanEnd));
            Draw(scannerBeam,start,scanEnd,scan&&ActiveScanner.gameObject.activeInHierarchy,.0015f);
            bool capture=fire&&mode==BioToolMode.Capture;
            Vector3 origin=ActiveBioTool.muzzle.position;
            Vector3 end=phagHeld?input.world.immunity.Held.transform.position:plateletHeld?input.plateletCapture.HeldPiece.transform.position:input.world.EmbolusCaptured?input.world.bubble.position:target?target.transform.position:origin+ActiveBioTool.muzzle.forward*.45f;
            captureTether.enabled=capture&&captured;
            if(captureTether.enabled)
            {
                var safe=origin;if(JourneyToolFeedback.ClipBeamStart(cameraRef,ref safe,end))
                {
                    captureTether.positionCount=24;float tension=Mathf.Clamp01(Vector3.Distance(origin,end)/5);captureTether.widthMultiplier=.0035f;
                    for(int i=0;i<24;i++){float t=i/23f;float wave=Mathf.Sin(t*Mathf.PI)*(.018f+.025f*(1-tension))*Mathf.Sin(Time.time*7-t*8);captureTether.SetPosition(i,Vector3.Lerp(safe,end,t)+cameraRef.transform.up*wave);}
                }
                else captureTether.enabled=false;
            }
            for(int i=0;i<captureRays.Length;i++)
            {
                var tip=ActiveBioTool.prongs[i].position+ActiveBioTool.muzzle.forward*.055f;
                var offset=Quaternion.AngleAxis(i*90,cameraRef.transform.forward)*cameraRef.transform.up*.05f;
                Draw(captureRays[i],tip,end+offset,capture,.0020f);
            }
            if(captureLockRing){captureLockRing.enabled=capture&&captured;captureLockRing.transform.SetPositionAndRotation(end,cameraRef.transform.rotation);captureLockRing.transform.localScale=Vector3.one*(.33f+.018f*Mathf.Sin(Time.time*7));captureLockRing.widthMultiplier=.0035f;}
            bool wide=fire&&(mode==BioToolMode.Tonus||mode==BioToolMode.Bio);
            fieldRing.enabled=wide;
            if(fieldRing.enabled)
            {fieldRing.transform.SetPositionAndRotation(end,cameraRef.transform.rotation);fieldRing.transform.localScale=Vector3.one*(mode==BioToolMode.Tonus?.4f:.22f);}
            shotAge+=Time.deltaTime;bool shot=shotAge<shotDuration;float phase=Mathf.Clamp01(shotAge/shotDuration);
            cellReleaseAge+=Time.deltaTime;float cellRecoil=cellReleaseAge<.24f?Mathf.Sin(cellReleaseAge/.24f*Mathf.PI):0;
            ActiveBioTool.AnimateHousing(Mathf.Max(shot?Mathf.Sin(phase*Mathf.PI):0,cellRecoil),fire);ActiveScanner.AnimateHousing(0,scan);
            if(!tracked)bioPC.transform.localPosition=bioPCMount-Vector3.forward*(shot?.016f*Mathf.Sin(phase*Mathf.PI):0);
            Draw(impulseBeam,origin,Vector3.Lerp(origin,shotEnd,phase),shot&&phase<.45f,.0048f*(1-phase));
            pulseRing.enabled=shot;
            if(shot){pulseRing.transform.SetPositionAndRotation(Vector3.Lerp(origin,shotEnd,phase),cameraRef.transform.rotation);pulseRing.transform.localScale=Vector3.one*Mathf.Lerp(.055f,.18f,phase);pulseRing.widthMultiplier=.006f*(1-phase);}
        }
        void Draw(LineRenderer line,Vector3 start,Vector3 end,bool visible,float width)
        {
            line.enabled=visible;if(!visible)return;
            if(!JourneyToolFeedback.ClipBeamStart(input.mover.viewCamera,ref start,end)){line.enabled=false;return;}
            line.widthMultiplier=width;line.positionCount=2;line.SetPosition(0,start);line.SetPosition(1,end);
        }
        MaterialPropertyBlock guideColors;
        void ColorGuide(LineRenderer line,Color color){guideColors??=new MaterialPropertyBlock();line.GetPropertyBlock(guideColors);guideColors.SetColor("_BaseColor",color);line.SetPropertyBlock(guideColors);}
        static void FaceCaption(JourneyInstrumentVisual view,Transform camera)
        {if(view.modeCaption&&view.modeCaption.enabled)view.modeCaption.transform.rotation=camera.rotation;}
        public void Confirm(StudyAction action,Vector3 position)
        {
            if(input.world.immunity&&input.world.immunity.Active)return;
            if(action==StudyAction.Scan||action==StudyAction.Capture||action==StudyAction.Deposit||action==StudyAction.Activate)return;
            if(modes&&modes.CurrentMode==BioToolMode.Tonus)return;
            FirePulse(position);
        }
        public void FirePulse(Vector3 position)
        {shotEnd=position;shotAge=0;ImpulseBursts++;}
        void OnDisable()
        {
            foreach(var line in new[]{scannerBeam,impulseBeam,captureTether,fieldRing,pulseRing,captureLockRing,scannerGuide,bioGuide})if(line)line.enabled=false;
            if(captureRays!=null)foreach(var line in captureRays)if(line)line.enabled=false;
        }
    }
}
