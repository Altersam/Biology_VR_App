using UnityEngine;
namespace BiologyVR.ArteryRoute
{
    public sealed class ArteryNode : MonoBehaviour
    {
        public int sceneId;
        public string presetId;
        public string DisplayName => presetId switch
        {
            "erythrocyte"=>"Эритроцит — транспорт кислорода",
            "leukocyte"=>"Лейкоцит — клетка иммунной системы",
            "platelet"=>"Тромбоцит — безъядерный клеточный фрагмент",
            "small-virus"=>"Условная вирусная частица — капсид и генетический материал",
            "hemoglobin"=>"Гемоглобин — четыре белковые субъединицы",
            "antibody"=>"Антитело — специфическое связывание эпитопа",
            "fibrinogen"=>"Фибриноген — растворимый предшественник фибрина",
            "macrophage"=>"Макрофаг — фагоцитоз и очистка",
            "neutrophil"=>"Нейтрофил — ранний иммунный ответ",
            "cytotoxic-t"=>"Цитотоксический T-лимфоцит — распознаёт инфицированную клетку",
            "infected-cell"=>"Инфицированная клетка — источник новых вирионов",
            _=>string.IsNullOrEmpty(presetId)?gameObject.name:presetId
        };
    }
}
