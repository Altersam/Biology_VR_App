using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyToolFeedback : MonoBehaviour
    {
        public JourneyMission mission;
        public LineRenderer ring;
        public LineRenderer shot;
        public LineRenderer lockRing,scanBeam,emitter;
        public Text modeCaption;
        public AudioSource audioSource;
        public AudioClip scanSound,pulseSound,modeSound,warningSound;
        string lastMode;
        MaterialPropertyBlock lockBlock;
        MaterialPropertyBlock pulseBlock;
        public string CurrentMode{get;private set;}="Базовый";
        public int CompletedScans{get;private set;}
        public void Warn(bool scanner)
        {
            if(audioSource&&warningSound)audioSource.PlayOneShot(warningSound,.22f);
            if(ring&&mission)
            {if(pulseBlock==null)pulseBlock=new MaterialPropertyBlock();pulseBlock.SetColor("_BaseColor",new Color(1,.62f,.16f));ring.SetPropertyBlock(pulseBlock);ring.transform.SetPositionAndRotation(mission.SceneNumber>=12&&mission.SceneNumber<=14?mission.world.woundSite.position:mission.mover.viewCamera.transform.position+mission.mover.viewCamera.transform.forward*1.5f,mission.mover.viewCamera.transform.rotation);elapsed=0;ring.enabled=true;}
            if(!XRSettings.isDeviceActive)return;var device=InputDevices.GetDeviceAtXRNode(scanner?XRNode.LeftHand:XRNode.RightHand);
            if(device.isValid)device.SendHapticImpulse(0,.09f,.06f);
        }
        public void PresentLock(JourneyTarget target,float progress,bool correct,bool scanning,Transform source)
        {
            if(lockRing)
            {
                lockRing.enabled=target&&source;
                if(lockRing.enabled)
                {
                    lockRing.transform.SetPositionAndRotation(target.transform.position+(source.position-target.transform.position).normalized*.055f,mission.mover.viewCamera.transform.rotation);
                    lockRing.transform.localScale=Vector3.one*(.70f+.20f*Mathf.Sin(Time.time*4));
                    lockRing.loop=progress<=0;lockRing.positionCount=progress<=0?48:Mathf.Max(2,Mathf.CeilToInt(progress*48));
                    if(lockBlock==null)lockBlock=new MaterialPropertyBlock();lockBlock.SetColor("_BaseColor",correct?new Color(.20f,.91f,.98f):new Color(.95f,.56f,.20f));lockRing.SetPropertyBlock(lockBlock);
                }
            }
            if(scanBeam)
            {
                scanBeam.enabled=scanning&&target&&source;scanBeam.widthMultiplier=.0012f;
                if(scanBeam.enabled)
                {
                    var start=source.position;var end=target.transform.position;
                    scanBeam.enabled=ClipBeamStart(mission.mover.viewCamera,ref start,end);
                    if(scanBeam.enabled){scanBeam.SetPosition(0,start);scanBeam.SetPosition(1,end);}
                }
            }
        }
        public static bool ClipBeamStart(Camera camera,ref Vector3 start,Vector3 end)
        {
            if(!camera)return false;var view=camera.transform;
            float near=Mathf.Max(.18f,camera.nearClipPlane+.05f);
            float from=Vector3.Dot(start-view.position,view.forward),to=Vector3.Dot(end-view.position,view.forward);
            if(to<=near)return false;
            if(from<near)start=Vector3.Lerp(start,end,(near-from)/(to-from));
            return true;
        }
        float elapsed=1;
        public int ConfirmedActions{get;private set;}
        public void Confirm(StudyAction action,string id)
        {
            if(!mission||!ring)return;
            var target=mission.world.Find(id);var camera=mission.mover.viewCamera.transform;
            ring.transform.position=target?target.transform.position:camera.position+camera.forward*1.8f;
            ring.transform.rotation=camera.rotation;elapsed=0;ring.enabled=true;ConfirmedActions++;
            if(pulseBlock==null)pulseBlock=new MaterialPropertyBlock();pulseBlock.SetColor("_BaseColor",new Color(.70f,.98f,.88f));ring.SetPropertyBlock(pulseBlock);
            if(action==StudyAction.Scan)CompletedScans++;
            var cue=action==StudyAction.Scan?scanSound:pulseSound;if(audioSource&&cue)audioSource.PlayOneShot(cue,.34f);
            if(shot)
            {
                shot.enabled=action==StudyAction.Pulse||action==StudyAction.Mark||action==StudyAction.Scan;
                var input=mission.GetComponent<JourneyInput>();var tool=action==StudyAction.Scan?input.scanner:input.bioTool;
                shot.SetPosition(0,XRSettings.isDeviceActive&&tool?tool.position:camera.position+camera.right*.25f-camera.up*.18f);
                shot.SetPosition(1,mission.world.LastInteractionPoint.sqrMagnitude>0?mission.world.LastInteractionPoint:ring.transform.position);
            }
            if(XRSettings.isDeviceActive)
            {
                var device=InputDevices.GetDeviceAtXRNode(action==StudyAction.Scan?XRNode.LeftHand:XRNode.RightHand);
                if(device.isValid)device.SendHapticImpulse(0,.16f,.035f);
            }
        }
        void LateUpdate()
        {
            UpdateMode();
            if(!ring||elapsed>=.34f)return;
            elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/.34f);
            ring.transform.localScale=Vector3.one*Mathf.Lerp(.65f,1.45f,t);
            ring.widthMultiplier=.012f*(1-t);ring.enabled=t<1;
            if(shot&&elapsed>.14f)shot.enabled=false;
        }
        void UpdateMode()
        {
            if(!mission||mission.Goals==null)return;var goal=mission.Current;
            CurrentMode=goal==null?"Исследование":goal.action==StudyAction.Scan?"Сканер":goal.action==StudyAction.Pulse?"Импульс":goal.action==StudyAction.Mark?"Иммунное управление":goal.action==StudyAction.Capture||goal.action==StudyAction.Deposit?"Притяжение":goal.action==StudyAction.Radius||goal.action==StudyAction.Pressure?"Сосудистое поле":goal.action==StudyAction.Direct||goal.action==StudyAction.Balance?"Управление":"Базовый";
            var input=mission.GetComponent<JourneyInput>();if(mission.SceneNumber==15&&goal?.action==StudyAction.Mark&&input&&input.LockedTarget)
            {var timing=input.LockedTarget.GetComponent<JourneyViralTiming>();if(timing&&timing.Shielded)CurrentMode="Импульс";}
            if(lastMode==CurrentMode)return;lastMode=CurrentMode;
            if(modeCaption)modeCaption.text=CurrentMode;
            if(emitter)
            {
                emitter.positionCount=CurrentMode=="Импульс"?3:CurrentMode=="Притяжение"?32:6;
                for(int i=0;i<emitter.positionCount;i++){float a=i*Mathf.PI*2/emitter.positionCount;emitter.SetPosition(i,new Vector3(Mathf.Cos(a)*.032f,Mathf.Sin(a)*.032f,0));}
                var colors=new MaterialPropertyBlock();colors.SetColor("_BaseColor",CurrentMode=="Импульс"?new Color(1,.64f,.28f):CurrentMode=="Иммунное управление"?new Color(.78f,.57f,1):new Color(.20f,.90f,.97f));emitter.SetPropertyBlock(colors);
            }
            if(audioSource&&modeSound)audioSource.PlayOneShot(modeSound,.14f);
        }
    }
}
