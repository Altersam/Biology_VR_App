using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyPlateletActivationToken:MonoBehaviour
    {
        public JourneyPlateletActivation owner;
        public int code;
        public XRGrabInteractable grab;
        public Collider contact;
        public Vector3 home;
        public bool Placed{get;private set;}
        void OnEnable(){if(grab){grab.selectExited.RemoveListener(Released);grab.selectExited.AddListener(Released);}}
        void OnDisable(){if(grab)grab.selectExited.RemoveListener(Released);}
        void Released(SelectExitEventArgs args){if(!args.isCanceled&&grab&&!grab.isSelected&&owner)owner.Place(this);}
        public void MarkPlaced(){Placed=true;contact.enabled=false;grab.enabled=false;}
        public void ResetToken(){Placed=false;transform.localPosition=home;if(contact)contact.enabled=true;if(grab)grab.enabled=true;}
    }
}
