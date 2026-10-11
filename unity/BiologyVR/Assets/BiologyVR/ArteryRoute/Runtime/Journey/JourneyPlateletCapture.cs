using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Held BioTool field guides a platelet; damaged endothelium owns adhesion.</summary>
    public sealed class JourneyPlateletCapture:MonoBehaviour
    {
        public JourneyInput input;
        public JourneyHemostasisPuzzle puzzle;
        public JourneyHemostasisPiece HeldPiece{get;private set;}
        public bool Holding=>HeldPiece;
        public float FieldDistance{get;private set;}=1.15f;
        public float CaptureProgress{get;private set;}
        public int Captures{get;private set;}
        public int Adhesions{get;private set;}
        public bool JustAdhered{get;private set;}
        JourneyHemostasisPiece candidate;
        Vector3 velocity;
        public bool UpdateField(float delta,bool held,Transform aim,Collider hit,bool desktop)
        {
            JustAdhered=false;var mission=puzzle.world.mission;
            bool lesson=mission.SceneNumber==12&&puzzle.flowLayering&&mission.Current?.action==StudyAction.Direct&&mission.Current.target=="platelet";
            if(!lesson){if(Holding)Release();CaptureProgress=0;candidate=null;return false;}
            if(!held||!aim){if(Holding)Release();CaptureProgress=0;candidate=null;return true;}
            if(!Holding)
            {
                var piece=hit?hit.GetComponentInParent<JourneyHemostasisPiece>():null;
                if(!piece||piece.puzzle!=puzzle||piece.strand||piece.Placed||piece.Held||(piece.transform.position-aim.position).sqrMagnitude>12*12){candidate=null;CaptureProgress=0;return true;}
                if(candidate!=piece){candidate=piece;CaptureProgress=0;}
                CaptureProgress=Mathf.Clamp01(CaptureProgress+delta/.20f);
                if(CaptureProgress<1)return true;
                HeldPiece=piece;HeldPiece.SetFieldHeld(true);velocity=Vector3.zero;Captures++;
                FieldDistance=desktop?Mathf.Clamp(Vector3.Distance(aim.position,piece.transform.position),.45f,7):1.15f;
            }
            if((HeldPiece.transform.position-aim.position).sqrMagnitude>12*12){Release();return true;}
            if(desktop)FieldDistance=Mathf.Clamp(FieldDistance+JourneyDesktopControls.WheelSteps*JourneyDesktopControls.DepthPerWheelStep,.45f,7);
            var point=aim.position+aim.forward*FieldDistance;
            HeldPiece.transform.position=Vector3.SmoothDamp(HeldPiece.transform.position,point,ref velocity,.18f,3.2f,Mathf.Clamp(delta,0,.05f));
            if(puzzle.TryAdhereFromField(HeldPiece))
            {HeldPiece.SetFieldHeld(false);HeldPiece=null;candidate=null;CaptureProgress=0;Adhesions++;JustAdhered=true;}
            return true;
        }
        public void Release()
        {
            if(HeldPiece){var piece=HeldPiece;HeldPiece=null;piece.SetFieldHeld(false);puzzle.RejoinFlow(piece);}
            candidate=null;CaptureProgress=0;velocity=Vector3.zero;
        }
        void OnDisable(){Release();}
    }
}
