using UnityEngine;
namespace BiologyVR.ArteryRoute
{
    public sealed class BloodCellFollower : MonoBehaviour
    {
        public float startY,minimumY,maximumY,laneX,laneZ,speed=1.5f;
        float age;
        Quaternion originalRotation,initialFrame;
        Renderer[] visuals;
        Camera viewer;
        void Awake() {originalRotation=transform.rotation;initialFrame=Quaternion.LookRotation(ArteryRouteController.Forward(startY),ArteryRouteController.Up(startY));visuals=GetComponentsInChildren<Renderer>();viewer=Camera.main;}
        void Update()
        {
            age+=Time.deltaTime;
            float radius=Mathf.Min(.95f,Mathf.Sqrt(laneX*laneX+laneZ*laneZ)/3.85f);
            float distance=speed*(.7f+.3f*(1-radius*radius))*(age+.1f*(1-Mathf.Cos(age*Mathf.PI*2*1.2f))/(Mathf.PI*2*1.2f));
            float y=minimumY+Mathf.Repeat(startY-minimumY+distance,Mathf.Max(1,maximumY-minimumY));
            transform.position=ArteryRouteController.Centre(y)+ArteryRouteController.Right(y)*laneX+ArteryRouteController.Up(y)*(laneZ+.025f*Mathf.Sin(age*.8f+startY));
            var frame=Quaternion.LookRotation(ArteryRouteController.Forward(y),ArteryRouteController.Up(y));
            transform.rotation=frame*Quaternion.Inverse(initialFrame)*originalRotation*Quaternion.AngleAxis(Mathf.Sin(age*.5f)*7,Vector3.up);
            bool visible=!viewer||(transform.position-viewer.transform.position).sqrMagnitude>4.4f;
            foreach(var r in visuals)r.enabled=visible;
        }
    }
}
