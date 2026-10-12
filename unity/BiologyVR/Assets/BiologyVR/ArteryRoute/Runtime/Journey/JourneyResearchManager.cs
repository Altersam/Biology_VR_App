using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    [Serializable] public sealed class JourneyResearchSnapshot
    {public string id;public JourneyResearchState state;public int page;}
    /// <summary>One journal for the first arterial stage, integrated with the existing mission and input.</summary>
    [DefaultExecutionOrder(-300)]
    public sealed class JourneyResearchManager:MonoBehaviour
    {
        public JourneyWorld world;
        public JourneyResearchableObject[] objects;
        public Text hoverLabel;
        [SerializeField] JourneyResearchableObject focused;
        [SerializeField] int introductions,uniqueScans;
        [SerializeField] bool completionReported;
        readonly Dictionary<string,JourneyResearchableObject> registry=new Dictionary<string,JourneyResearchableObject>();
        JourneyResearchableObject hovered;
        JourneyTarget selectedInstance;
        JourneyTarget focusedInstance;
        public bool Active=>world&&world.mission&&world.mission.SceneNumber==3;
        public JourneyResearchableObject Focused=>focused;
        public JourneyResearchableObject Hovered=>hovered;
        public int RequiredCount=>objects==null?0:objects.Count(o=>o&&o.theory&&o.theory.requiredForCompletion);
        public int RequiredInvestigated=>objects==null?0:objects.Count(o=>o&&o.theory&&o.theory.requiredForCompletion&&o.State==JourneyResearchState.Investigated);
        public int OptionalInvestigated=>objects==null?0:objects.Count(o=>o&&o.theory&&!o.theory.requiredForCompletion&&o.State==JourneyResearchState.Investigated);
        public int Introductions=>introductions;
        public int UniqueScans=>uniqueScans;
        public bool Complete=>RequiredCount>0&&RequiredInvestigated==RequiredCount;
        public string GoalLabel=>Complete?"Исследование состава крови завершено. Можно продолжить или изучать дальше.":focused&&focused.State!=JourneyResearchState.Investigated?$"Рассмотри {focused.theory.displayName.ToLowerInvariant()} и удержи Scanner. Остальные объекты доступны.":"Выбери любой объект крови: рассмотри или просканируй его. Порядок свободный.";
        void Awake(){RebuildRegistry();}
        void RebuildRegistry()
        {
            registry.Clear();if(objects==null)return;
            foreach(var item in objects)if(item&&item.theory&&item.target){registry[item.theory.id]=item;item.manager=this;}
        }
        public JourneyResearchableObject Find(string id)
        {if(string.IsNullOrEmpty(id))return null;if(registry.Count==0)RebuildRegistry();return registry.TryGetValue(id,out var item)?item:null;}
        public JourneyTarget FocusedTarget=>focusedInstance?focusedInstance:focused?focused.target:null;
        public void SelectInstance(JourneyTarget target){if(CanInvestigate(target))selectedInstance=target;}
        public bool CanInvestigate(JourneyTarget target)=>Active&&world.mission.Ready&&target&&target.isActiveAndEnabled&&Find(target.targetId)!=null&&(!world.analysis||world.analysis.Available(target.targetId))&&(!world.bloodEntities||world.analysis&&world.analysis.IsMolecular(target.targetId)||world.bloodEntities.Owns(target));
        public void ResetRun()
        {RebuildRegistry();foreach(var item in objects)if(item)item.ResetRun();focused=hovered=null;selectedInstance=focusedInstance=null;introductions=uniqueScans=0;completionReported=false;}
        public void EnterStage()
        {RefreshAvailability();world.mission.RefreshResearchJournal(false,focused?"Карточка исследования открыта снова.":"Осмотрись: все объекты доступны для исследования.","");}
        public void RefreshAvailability()
        {
            if(!Active||objects==null)return;
            foreach(var item in objects)if(item&&item.target)
            {
                bool available=world.analysis&&world.analysis.IsMolecular(item.theory.id)?world.analysis.Available(item.theory.id):!world.bloodEntities||world.bloodEntities.Owns(item.target);
                if(item.gameObject.activeSelf!=available)item.gameObject.SetActive(available);foreach(var collider in item.target.GetComponentsInChildren<Collider>(true))collider.enabled=available;
            }
        }
        public bool Interact(StudyAction action,string id)
        {
            if(action!=StudyAction.Scan&&action!=StudyAction.Grab&&action!=StudyAction.Enlarge&&action!=StudyAction.Compare&&action!=StudyAction.Measure&&action!=StudyAction.Detach&&action!=StudyAction.Bind)return false;
            var item=Find(id);var instance=selectedInstance&&selectedInstance.targetId==id?selectedInstance:item?item.target:null;if(!item||!CanInvestigate(instance))return false;
            if(world.analysis&&world.analysis.Opened&&!world.analysis.IsMolecular(id)&&world.analysis.Subject!=instance)world.analysis.Close();
            bool changed=focused!=item;bool first=item.Begin();if(first)introductions++;focused=item;focusedInstance=instance;
            bool fresh=action==StudyAction.Scan&&item.CompleteResearch();if(fresh)uniqueScans++;
            if(action==StudyAction.Compare&&item.theory.pages.Length>1)item.SetPage(1);
            if(id=="platelet"){var platelet=instance.GetComponent<JourneyPlateletEntity>();if(platelet)platelet.SetState(action==StudyAction.Compare?JourneyPlateletState.Activated:platelet.State==JourneyPlateletState.Flowing?JourneyPlateletState.Inactive:platelet.State);}
            bool finished=Complete&&!completionReported;if(finished)completionReported=true;
            string professor=finished?"Отлично. Теперь ты знаешь, кто движется вместе с нами. Можно продолжить путешествие или рассмотреть молекулярные детали.":first?item.theory.professorComment:!changed&&(fresh||item.State==JourneyResearchState.Investigating)?world.mission.ProfessorLine:"";
            string feedback=finished?"Исследование состава крови завершено.":fresh?$"Изучено: {item.theory.displayName}":item.State==JourneyResearchState.Investigated?$"Открыта изученная карточка: {item.theory.displayName}":$"Исследование: {item.theory.displayName}";
            if(fresh)world.mission.RecordResearchScan(id);
            world.mission.RefreshResearchJournal(finished,feedback,professor);
            return true;
        }
        public void TurnPage(int direction)
        {if(!Active||!focused)return;focused.SetPage((focused.Page+direction+focused.theory.pages.Length)%focused.theory.pages.Length);world.mission.RefreshResearchJournal(false,"Карточка: "+focused.theory.displayName,"");}
        public void ShowOverview(){if(!Active)return;focused=null;world.mission.RefreshResearchJournal(false,"Выбери любой объект. Уже изученное сохраняется.","");}
        public void SetHovered(JourneyTarget target){hovered=CanInvestigate(target)?Find(target.targetId):null;if(hovered)selectedInstance=target;}
        public JourneyResearchSnapshot[] Capture()=>objects.Where(o=>o&&o.theory).Select(o=>new JourneyResearchSnapshot{id=o.theory.id,state=o.State,page=o.Page}).ToArray();
        void LateUpdate()
        {
            if(!hoverLabel)return;var item=hovered?hovered:focused&&focused.target&&(focused.target.grabbed||focused.target.DesktopHeld)?focused:null;
            var physical=FocusedTarget;bool show=Active&&item&&physical&&Vector3.Distance(world.mover.viewCamera.transform.position,physical.transform.position)<9;
            hoverLabel.gameObject.SetActive(show);if(!show)return;
            hoverLabel.text=item.theory.displayName+"\n"+(item.State==JourneyResearchState.Investigated?"[Изучено]":"[Исследовать / Scanner]");
            var camera=world.mover.viewCamera.transform;hoverLabel.transform.SetPositionAndRotation(physical.transform.position+camera.up*.24f,camera.rotation);
        }
    }
}
