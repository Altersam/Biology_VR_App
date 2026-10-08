using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BiologyVR.ArteryRoute
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class CellInspectable : MonoBehaviour
    {
        public string title;
        public string description;
        public bool IsHeld {get;private set;}
        Vector3 homePosition;Quaternion homeRotation;
        Renderer[] renderers;
        MaterialPropertyBlock block;
        void Start()
        {
            homePosition=transform.position;homeRotation=transform.rotation;
            renderers=GetComponentsInChildren<Renderer>();block=new MaterialPropertyBlock();
            var grab=GetComponent<XRGrabInteractable>();
            grab.selectEntered.AddListener(_=>SetHeld(true));grab.selectExited.AddListener(_=>SetHeld(false));
        }
        void SetHeld(bool value)
        {
            IsHeld=value;
            foreach(var r in renderers){if(value){r.GetPropertyBlock(block);block.SetColor("_BaseColor",new Color(.14f,.78f,.95f));r.SetPropertyBlock(block);}else r.SetPropertyBlock(null);}
            if(value) ArteryStudyPresenter.Instance?.ShowScan(this);
            else {transform.SetPositionAndRotation(homePosition,homeRotation);var rb=GetComponent<Rigidbody>();if(rb&&!rb.isKinematic){rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;}}
        }
        void Update(){if(!IsHeld)transform.rotation=homeRotation*Quaternion.AngleAxis(Mathf.Sin(Time.time*.5f)*9,Vector3.up);}
    }
}
