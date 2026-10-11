using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Release real physical cards into a small biochemical sequence, not a debug/button shortcut.</summary>
    public sealed class JourneyPlateletActivation:MonoBehaviour
    {
        public JourneyHemostasisPuzzle puzzle;
        public GameObject panel;
        public JourneyPlateletActivationToken[] tokens;
        public Transform[] slots;
        public Renderer[] slotSurfaces;
        public Text caption;
        public int Progress{get;private set;}
        public int Mistakes{get;private set;}
        public bool Solved{get;private set;}
        MaterialPropertyBlock colors;
        void Awake(){colors=new MaterialPropertyBlock();ResetPuzzle();}
        public void ResetPuzzle(){Progress=0;Solved=false;if(tokens!=null)foreach(var token in tokens)token.ResetToken();}
        void Update()
        {
            if(!puzzle||puzzle.world.mission.Goals==null)return;var mission=puzzle.world.mission;
            bool visible=mission.SceneNumber==12&&puzzle.Attached>=puzzle.RequiredPlatelets&&!puzzle.Activated;
            panel.SetActive(visible);if(!visible)return;
            bool canPlace=mission.Current?.action==StudyAction.Activate&&mission.Current.target=="platelet";
            foreach(var token in tokens){token.contact.enabled=canPlace&&!token.Placed;token.grab.enabled=canPlace&&!token.Placed;}
            if(caption)caption.text=canPlace?$"СТАБИЛИЗАЦИЯ ПЕРВИЧНОЙ ПРОБКИ • {Progress}/3\nАктивация → агрегация → фибрин\nРасположи три этапа слева направо.":"Первичная пробка сформирована, но кровотечение ещё продолжается.";
            for(int i=0;i<slotSurfaces.Length;i++)if(slotSurfaces[i]){slotSurfaces[i].GetPropertyBlock(colors);colors.SetColor("_BaseColor",i<Progress?new Color(.20f,.78f,.65f):i==Progress?HolographicStyle.Amber:HolographicStyle.Cyan*.4f);slotSurfaces[i].SetPropertyBlock(colors);}
        }
        public void Place(JourneyPlateletActivationToken token)
        {
            var mission=puzzle.world.mission;if(mission.SceneNumber!=12||mission.Current?.action!=StudyAction.Activate||Solved||token.Placed)return;
            bool rightSlot=Progress<slots.Length&&Vector3.Distance(token.transform.position,slots[Progress].position)<.25f;
            if(!rightSlot||token.code!=Progress)
            {
                Mistakes++;mission.RecordPhysicalError(token.code==3?"EDTA связывает Ca²⁺ и препятствует коагуляции: эта карточка не стабилизирует пробку.":"Порядок: активация тромбоцитов → GPIIb/IIIa и фибриноген → тромбин/фибрин.");
                StartCoroutine(Return(token));return;
            }
            token.MarkPlaced();token.transform.SetPositionAndRotation(slots[Progress].position,slots[Progress].rotation);Progress++;
            var feedback=mission.GetComponent<JourneyToolFeedback>();if(feedback&&feedback.audioSource&&feedback.modeSound)feedback.audioSource.PlayOneShot(feedback.modeSound,.25f);
            if(Progress==slots.Length){Solved=puzzle.FinishActivationPuzzle();if(Solved)mission.FeedbackMessage("Цепочка стабилизации решена. Фибрин образуется постепенно; наблюдай уменьшение утечки.");}
        }
        IEnumerator Return(JourneyPlateletActivationToken token)
        {yield return null;var start=token.transform.localPosition;for(float t=0;t<1;t+=Time.deltaTime/.25f){if(token.grab.isSelected)yield break;token.transform.localPosition=Vector3.Lerp(start,token.home,Mathf.SmoothStep(0,1,t));yield return null;}token.transform.localPosition=token.home;}
    }
}
