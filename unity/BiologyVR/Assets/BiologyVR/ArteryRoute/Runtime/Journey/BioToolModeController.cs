using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace BiologyVR.ArteryRoute.Journey
{
        public enum BioToolMode{Impulse,Capture,Tonus,Bio,Reverse}
    /// <summary>One mode state shared by existing actions and all model/VFX views.</summary>
    public sealed class BioToolModeController:MonoBehaviour
    {
        public JourneyInput input;
        public BioToolMode CurrentMode{get;private set;}
        public BioToolMode RequiredMode{get;private set;}
        public int ModeChanges{get;private set;}
        public float ModePopupUntil{get;private set;}
        public float HeldSeconds{get;private set;}
        public string Caption=>CurrentMode==BioToolMode.Impulse?"ИМПУЛЬС":CurrentMode==BioToolMode.Capture?"ПРИТЯЖЕНИЕ":CurrentMode==BioToolMode.Tonus?"СОСУДИСТОЕ ПОЛЕ":CurrentMode==BioToolMode.Reverse?(input&&input.mission.SceneNumber==10&&input.world.immunePatrol?"РЕВЕРС · ИММУННЫЙ СИГНАЛ":"РЕВЕРС · ЗАПУСК КЛЕТКИ"):"АКТИВАЦИЯ";
        InputAction nextMode,previousMode;
        StudyGoal lastGoal;
        int lastScene=-1;
        bool previousButton,previousBack;
        static readonly BioToolMode[] FourModes={BioToolMode.Impulse,BioToolMode.Capture,BioToolMode.Tonus,BioToolMode.Reverse};
        static readonly BioToolMode[] FiveModes={BioToolMode.Impulse,BioToolMode.Capture,BioToolMode.Tonus,BioToolMode.Bio,BioToolMode.Reverse};
        void Awake(){if(!input)input=GetComponent<JourneyInput>();nextMode=new InputAction("BioTool next mode A",InputActionType.Button,"<XRController>{RightHand}/primaryButton");previousMode=new InputAction("BioTool previous mode B",InputActionType.Button,"<XRController>{RightHand}/secondaryButton");}
        void OnEnable(){nextMode?.Enable();previousMode?.Enable();}
        void OnDisable(){nextMode?.Disable();previousMode?.Disable();HeldSeconds=0;previousButton=previousBack=false;}
        void OnDestroy(){nextMode?.Dispose();previousMode?.Dispose();}
        public static BioToolMode For(StudyGoal goal)
        {
            if(goal==null)return BioToolMode.Impulse;
            if(goal.target=="patrol-focus")return BioToolMode.Reverse;
            if(goal.target=="immune-engulf")return BioToolMode.Reverse;
            if(goal.target=="phagocyte"&&goal.action==StudyAction.Activate)return BioToolMode.Bio;
            if(goal.action==StudyAction.Capture||goal.action==StudyAction.Deposit)return BioToolMode.Capture;
            if(goal.action==StudyAction.Direct&&goal.target=="platelet")return BioToolMode.Capture;
            if(goal.action==StudyAction.Radius||goal.action==StudyAction.Pressure)return BioToolMode.Tonus;
            if(goal.action==StudyAction.Measure)return BioToolMode.Bio;
            if(goal.action==StudyAction.Direct||goal.action==StudyAction.Balance||goal.action==StudyAction.Activate)return BioToolMode.Bio;
            return BioToolMode.Impulse;
        }
        public void RefreshGoal()
        {
            if(!input||!input.mission||input.mission.Goals==null)return;
            var goal=input.mission.Current;
            if(lastGoal==goal&&lastScene==input.mission.SceneNumber)return;
            lastGoal=goal;lastScene=input.mission.SceneNumber;RequiredMode=For(goal);
            // Existing mission mode selection remains authoritative at each new step.
            if(!input.world.freeTools)Select(RequiredMode,false);
        }
        void Update()
        {
            RefreshGoal();if(!input||!input.mission||input.mission.Goals==null)return;
            bool button=nextMode!=null&&nextMode.IsPressed();
            bool back=previousMode!=null&&previousMode.IsPressed();
            bool keyboard=Keyboard.current!=null&&Keyboard.current.cKey.wasPressedThisFrame;
            int count=input.world.plateletLaunch?4:5;
            var cycle=input.world.plateletLaunch?FourModes:FiveModes;
            int index=System.Array.IndexOf(cycle,CurrentMode);if(index<0)index=0;
            if(keyboard||(button&&!previousButton))Select(cycle[(index+1)%count],true);
            else if(back&&!previousBack)Select(cycle[(index+count-1)%count],true);
            previousButton=button;previousBack=back;
            HeldSeconds=input.ToolHeld?HeldSeconds+Time.deltaTime:0;
        }
        public void Select(BioToolMode mode,bool userSelected)
        {
            if(CurrentMode==mode&&ModeChanges>0)return;
            if(CurrentMode==BioToolMode.Capture&&mode!=CurrentMode&&input&&input.world.EmbolusCaptured){input.world.ReleaseEmbolus();lastGoal=input.mission.Current;lastScene=input.mission.SceneNumber;RequiredMode=For(lastGoal);}
            if(CurrentMode==BioToolMode.Capture&&mode!=CurrentMode&&input&&input.plateletCapture)input.plateletCapture.Release();
            if(CurrentMode==BioToolMode.Capture&&mode!=CurrentMode&&input&&input.world.immunity&&!input.world.freeTools)input.world.immunity.CancelHold();
            CurrentMode=mode;ModeChanges++;ModePopupUntil=Time.time+1.1f;HeldSeconds=0;
            if(input&&input.world.freeTools)input.world.freeTools.ModeChanged(mode);
            if(input){var rig=input.GetComponent<JourneyInstrumentRig>();if(rig)rig.ModeChanged();}
            if(userSelected&&input)input.mission.FeedbackMessage(Caption);
            if(userSelected&&input){var feedback=input.GetComponent<JourneyToolFeedback>();if(feedback&&feedback.audioSource&&feedback.modeSound)feedback.audioSource.PlayOneShot(feedback.modeSound,.22f);}
            if(userSelected&&XRSettings.isDeviceActive)
            {var device=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);if(device.isValid)device.SendHapticImpulse(0,.07f,.025f);}
        }
        public bool AllowsCurrentGoal()
        {RefreshGoal();return CurrentMode==RequiredMode||input.world.plateletLaunch&&RequiredMode==BioToolMode.Bio&&CurrentMode==BioToolMode.Reverse;}
    }
}
