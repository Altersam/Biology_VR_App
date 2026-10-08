using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Physical XRI item for the one wound puzzle. Success only on actual release/placement.</summary>
    public sealed class JourneyHemostasisPiece:MonoBehaviour
    {
        public JourneyHemostasisPuzzle puzzle;
        public int index;
        public bool strand;
        public bool Placed{get;private set;}
        XRGrabInteractable grab;
        // Inactive reserve pieces/strands have not run Awake when a later
        // episode configures their interaction state directly.
        public XRGrabInteractable Grab{get{if(!grab)grab=GetComponent<XRGrabInteractable>();return grab;}}
        public bool Held=>Grab&&Grab.isSelected;
        void Awake(){grab=GetComponent<XRGrabInteractable>();}
        void OnEnable()
        {
            if(!Grab)return;
            Grab.selectExited.RemoveListener(Released);Grab.selectExited.AddListener(Released);
            Grab.selectEntered.RemoveListener(PickedUp);Grab.selectEntered.AddListener(PickedUp);
        }
        void OnDisable(){if(Grab){Grab.selectExited.RemoveListener(Released);Grab.selectEntered.RemoveListener(PickedUp);}}
        void PickedUp(SelectEnterEventArgs args){if(puzzle&&puzzle.world.mission.SceneNumber==13&&Placed){Placed=false;puzzle.Removed(this);}}
        void Released(SelectExitEventArgs args)
        {
            if(args.isCanceled||Held||!puzzle||!isActiveAndEnabled)return;
            puzzle.PlaceReleased(this,args.interactorObject.transform);
        }
        public void SetPlaced(bool value){Placed=value;if(Grab)Grab.enabled=!value;}
        public void EnableBalanceGrab(){if(Grab)Grab.enabled=true;}
    }
}
