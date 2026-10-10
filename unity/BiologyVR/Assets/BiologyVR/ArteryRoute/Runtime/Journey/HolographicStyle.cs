using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    public static class HolographicStyle
    {
        public static readonly Color Cyan=new Color(92/255f,235/255f,1);
        public static readonly Color Blue=new Color(73/255f,216/255f,1);
        public static readonly Color SoftGlow=new Color(155/255f,239/255f,1);
        public static readonly Color Amber=new Color(243/255f,181/255f,74/255f);
        public static readonly Color White=new Color(234/255f,251/255f,1);
        public static readonly Color Secondary=new Color(191/255f,239/255f,1);
        public static Color Glass(float alpha)=>new Color(10/255f,40/255f,55/255f,alpha);
    }
}
