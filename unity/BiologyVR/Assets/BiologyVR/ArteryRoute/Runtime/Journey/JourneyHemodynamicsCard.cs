using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyHemodynamicsCard:MonoBehaviour
    {
        public JourneyHemodynamicsExperiment owner;
        public int code;
        public XRGrabInteractable grab;
        public Vector3 home;
        public bool Placed{get;private set;}
        void OnEnable(){if(!grab)return;grab.selectExited.RemoveListener(Released);grab.selectExited.AddListener(Released);}
        void OnDisable(){if(grab)grab.selectExited.RemoveListener(Released);}
        public void ResetCard(){Placed=false;transform.localPosition=home;grab.enabled=true;}
        void Released(SelectExitEventArgs args)
        {
            if(args.isCanceled||Placed||!owner.Active)return;
            if(Vector3.Distance(transform.position,owner.cardSlot.position)<.25f&&owner.ResolveCard(code))
            {Placed=true;transform.SetPositionAndRotation(owner.cardSlot.position,owner.cardSlot.rotation);grab.enabled=false;}
            else StartCoroutine(Return());
        }
        IEnumerator Return(){yield return null;var from=transform.localPosition;for(float t=0;t<1;t+=Time.deltaTime/.3f){if(grab.isSelected)yield break;transform.localPosition=Vector3.Lerp(from,home,Mathf.SmoothStep(0,1,t));yield return null;}transform.localPosition=home;}
    }
}
