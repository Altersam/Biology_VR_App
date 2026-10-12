using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class FreeBloodResearchBuilder
    {
        const string Folder=JourneyGeometry.Root+"/Generated/FreeBloodResearch";
        public const string Reports=JourneyGeometry.Root+"/Reports/Polish/FreeBloodResearch";
        [MenuItem("Biology VR/Install Free First Blood Research")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();if(!world)throw new InvalidOperationException("Existing journey master required");
            Directory.CreateDirectory(Folder);Directory.CreateDirectory(Reports);EditorSceneManager.SaveScene(world.gameObject.scene);
            string backup=Reports+"/Before_Free_Blood_Research.unity";if(!File.Exists(backup)&&!AssetDatabase.CopyAsset(world.gameObject.scene.path,backup))throw new IOException("Research backup failed");
            var manager=world.GetComponent<JourneyResearchManager>();if(!manager)manager=world.gameObject.AddComponent<JourneyResearchManager>();manager.world=world;world.research=manager;
            var data=Data();manager.objects=new JourneyResearchableObject[data.Length];
            for(int i=0;i<data.Length;i++)
            {
                var target=world.Find(data[i].id);if(!target)throw new InvalidOperationException("Existing research target missing: "+data[i].id);
                string path=Folder+"/"+data[i].id+".asset";JourneyGeometry.Save(data[i],path);
                var item=target.GetComponent<JourneyResearchableObject>();if(!item)item=target.gameObject.AddComponent<JourneyResearchableObject>();item.theory=AssetDatabase.LoadAssetAtPath<JourneyResearchTheory>(path);item.target=target;item.manager=manager;item.ResetRun();manager.objects[i]=item;target.hoverEmission=true;EditorUtility.SetDirty(item);EditorUtility.SetDirty(target);
            }
            var old=world.transform.Find("Free research hover caption");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            manager.hoverLabel=ArteryAnnotations.Label(world.transform,"Free research hover caption","");manager.hoverLabel.fontStyle=FontStyle.Bold;manager.hoverLabel.fontSize=22;manager.hoverLabel.rectTransform.sizeDelta=new Vector2(340,74);manager.hoverLabel.transform.localScale=Vector3.one*.0018f;manager.hoverLabel.gameObject.SetActive(false);
            // Keep existing molecular models on their existing carrier, but give
            // each contact a readable, non-overlapping teaching offset from entry.
            var hb=world.Find("hemoglobin").GetComponent<JourneyBloodCargo>();if(hb){hb.displayOffset=new Vector3(.46f,.38f,0);EditorUtility.SetDirty(hb);}
            var heme=world.Find("heme").GetComponent<JourneyBloodCargo>();if(heme){heme.displayOffset=new Vector3(-.46f,.38f,0);EditorUtility.SetDirty(heme);}
            var oxygen=world.Find("oxygen").GetComponent<JourneyBloodCargo>();if(oxygen){oxygen.displayOffset=new Vector3(0,.31f,0);EditorUtility.SetDirty(oxygen);}
            var screens=world.mission.GetComponent<JourneyHolographicScreens>();
            screens.referenceButton.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(screens.referenceButton.onClick,screens.NextTheoryPage);
            var practice=screens.practiceButton.GetComponent<Button>();practice.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(practice.onClick,screens.ShowResearchOverview);
            screens.taskProgress.rectTransform.offsetMin=new Vector2(340,-42);screens.taskProgress.resizeTextMinSize=16;
            EditorUtility.SetDirty(screens.referenceButton);EditorUtility.SetDirty(practice);EditorUtility.SetDirty(screens.taskProgress);EditorUtility.SetDirty(screens);
            manager.ResetRun();EditorUtility.SetDirty(manager);EditorUtility.SetDirty(world);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);EditorSceneManager.SaveScene(world.gameObject.scene);
            File.WriteAllText(Reports+"/Authoring.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,firstArterialEpisodeInternalNumber=3,required=manager.objects.Where(o=>o.theory.requiredForCompletion).Select(o=>o.theory.id),optional=manager.objects.Where(o=>!o.theory.requiredForCompletion).Select(o=>o.theory.id),data="ScriptableObject pages / existing targets",states="NotInvestigated / Investigating / Investigated",scannerWithoutPriorGrab=true,linearDependenciesRemoved=true,completionRequiresAllMandatory=true,noAutomaticTransition=true,progressLifetime="current run, including normal stage re-entry",existingTargets=world.targets.Length,bloodPool=world.GetComponent<JourneyBloodFlow>().cells.Length,timeScaleFrozen=false,hardwareVerified=false},Formatting.Indented));
        }
        static JourneyResearchPage Page(string heading,string text,string connection)=>new JourneyResearchPage{heading=heading,text=text,visualConnection=connection};
        static JourneyResearchTheory Theory(string id,string name,bool required,string professor,params JourneyResearchPage[] pages)
        {var data=ScriptableObject.CreateInstance<JourneyResearchTheory>();data.name=name;data.id=id;data.displayName=name;data.requiredForCompletion=required;data.professorComment=professor;data.pages=pages;return data;}
        static JourneyResearchTheory[] Data()=>new[]
        {
            Theory("rbc","Эритроцит",true,"Посмотри на углубления с обеих сторон. Эта форма помогает клетке обмениваться газами.",
                Page("Строение","• Зрелый эритроцит человека не имеет ядра.\n• Двояковогнутый диск увеличивает площадь обмена.\n• Гибкая клетка проходит узкие капилляры.","ФОРМА → ПОВЕРХНОСТЬ → ОБМЕН"),
                Page("Перенос кислорода","• Эритроцит переносит O₂ благодаря гемоглобину.\n• В лёгких O₂ связывается с Hb.\n• В тканях связь обратимо ослабевает.","ЛЁГКИЕ → Hb–O₂ → ТКАНИ"),
                Page("Связь с газообменом","• Эритроцит участвует и в переносе части CO₂.\n• Большая часть CO₂ переносится как HCO₃⁻.\n• Hb, гем и O₂ рядом — увеличенные учебные модели.","ТКАНИ → CO₂ / HCO₃⁻ → ЛЁГКИЕ")),
            Theory("leukocyte","Лейкоцит",true,"Это участник иммунной защиты. Заметь ядро: лейкоциты встречаются намного реже окружающих эритроцитов.",
                Page("Строение","• Лейкоциты имеют ядро, в отличие от зрелых эритроцитов.\n• Многие из них крупнее эритроцитов.\n• Их значительно меньше в нормальной крови.","ЯДРО · РЕДКАЯ КЛЕТКА · ЗАЩИТА"),
                Page("Иммунный ответ","• Распознают чужеродные агенты.\n• Фагоциты поглощают микроорганизмы.\n• Лимфоциты участвуют в специфическом ответе.\n• Разные клетки координируют воспаление.","РАСПОЗНАВАНИЕ → ИММУННЫЙ ОТВЕТ"),
                Page("Кровь и ткани","• Лейкоцит может выйти через стенку сосуда в ткани.\n• Сигналы воспаления направляют его к очагу.\n• Этот механизм пригодится при изучении защиты.","КРОВЬ → ВЫХОД В ТКАНИ → ОЧАГ")),
            Theory("platelet","Тромбоцит",true,"Этот маленький фрагмент помогает остановить кровотечение. Позже ты увидишь, как важен его контакт со стенкой.",
                Page("Что это","• Тромбоцит — безъядерный фрагмент мегакариоцита.\n• Обычно он движется в крови в покое.\n• Его задача — участие в гемостазе.","ТРОМБОЦИТ → ГЕМОСТАЗ"),
                Page("Первичная пробка","• Адгезия: прикрепление к повреждённой поверхности.\n• Активация: изменение формы и сигналы.\n• Агрегация: объединение тромбоцитов.","АДГЕЗИЯ → АКТИВАЦИЯ → АГРЕГАЦИЯ"),
                Page("Связь со стабилизацией","• Тромбоциты создают первичную пробку.\n• Фибрин затем делает её устойчивее.\n• Пробка и восстановление эндотелия — разные процессы.","ПЕРВИЧНАЯ ПРОБКА → ФИБРИН")),
            Theory("plasma","Плазма крови",true,"Перед тобой условный образец среды, а не отдельная клетка. Именно в этой жидкости путешествуют клетки и растворённые вещества.",
                Page("Жидкая среда","• Плазма — жидкая часть крови, преимущественно вода.\n• В ней перемещаются форменные элементы.\n• Она переносит растворённые вещества.","ПЛАЗМА = СРЕДА ТРАНСПОРТА"),
                Page("Транспорт веществ","• Питательные вещества идут к тканям.\n• Продукты обмена — к органам выведения.\n• Электролиты поддерживают водный и ионный баланс.","ПИТАНИЕ · ЭЛЕКТРОЛИТЫ · ОБМЕН"),
                Page("Белки плазмы","• Альбумин удерживает воду и переносит вещества.\n• Глобулины участвуют в транспорте и защите.\n• Фибриноген — предшественник фибрина.","АЛЬБУМИН · ГЛОБУЛИНЫ · ФИБРИНОГЕН"),
                Page("Связь с клетками","• Плазма связывает клеточный и общий уровень организма.\n• Часть газов растворена; HCO₃⁻ переносится в плазме.\n• Видимые частицы — условные учебные образцы.","КЛЕТКИ ↔ ПЛАЗМА ↔ ОРГАНЫ")),
            Theory("hemoglobin","Гемоглобин",false,"Эта модель связана с эритроцитом. Посмотри, где белок удерживает кислород, а не воспринимай его как свободную клетку.",
                Page("Связь с эритроцитом","• Гемоглобин — белок внутри эритроцита.\n• Основная функция — обратимое связывание O₂.\n• Модель здесь увеличена для исследования.","ЭРИТРОЦИТ → Hb → O₂ → ТКАНИ"),
                Page("Четыре участка связывания","• Обычный взрослый HbA имеет четыре субъединицы.\n• Каждая несёт гем с Fe²⁺.\n• Одна молекула Hb может связать четыре O₂.","4 СУБЪЕДИНИЦЫ · 4 ГЕМА · 4 O₂"),
                Page("Обратимый транспорт","• В лёгких Hb насыщается O₂.\n• В тканях кислород отдаётся по условиям среды.\n• Часть CO₂ связывается с белковой частью Hb.","Hb + O₂ ⇄ HbO₂")),
            Theory("heme","Гем и железо Fe²⁺",false,"Рассмотри центр гема. Здесь важно железо в составе белка, а не свободный металлический атом в потоке.",
                Page("Центр гема","• Гем — порфириновое кольцо с Fe²⁺ в центре.\n• Он входит в состав гемоглобина.\n• O₂ обратимо связывается с этим центром.","Hb → ГЕМ → Fe²⁺ ↔ O₂"),
                Page("Почему степень окисления важна","• Для нормального связывания O₂ нужен Fe²⁺.\n• Окисление до Fe³⁺ образует метгемоглобин.\n• Такой центр не переносит O₂ обычным способом.","Fe²⁺: СВЯЗЫВАНИЕ O₂")),
            Theory("oxygen","Кислород O₂",false,"Проследи путь от лёгких к тканям. Связь с гемом обратима — кислород нужен клеткам, а не должен оставаться на переносчике навсегда.",
                Page("Путь кислорода","• В лёгких O₂ переходит в кровь.\n• Большая часть переносится гемоглобином.\n• В тканях O₂ выходит из крови к клеткам.","ЛЁГКИЕ → КРОВЬ → ТКАНИ"),
                Page("Клеточное использование","• O₂ участвует в аэробном получении энергии.\n• Потребление в тканях поддерживает градиент переноса.\n• Небольшая часть O₂ растворена в плазме.","O₂ → КЛЕТОЧНОЕ ДЫХАНИЕ → ЭНЕРГИЯ"),
                Page("Учебная модель связывания","• Модель O₂ можно взять отдельно и вернуть к гему.\n• Это показывает обратимость связи с Fe²⁺.\n• Размеры молекул увеличены условно.","Fe²⁺ + O₂ ⇄ Fe²⁺–O₂")),
            Theory("co2","Углекислый газ CO₂",false,"Этот газ движется в обратном направлении. Его основной путь переноса отличается от кислорода.",
                Page("Путь от тканей","• CO₂ образуется при обмене веществ в тканях.\n• Он переходит из тканей в кровь.\n• В лёгких CO₂ выходит из крови и выдыхается.","ТКАНИ → КРОВЬ → ЛЁГКИЕ"),
                Page("Основной способ переноса","• Большая часть CO₂ переносится как HCO₃⁻.\n• Эритроциты помогают этому превращению.\n• Карбоангидраза ускоряет реакцию в клетке.","CO₂ + H₂O ⇄ H₂CO₃ ⇄ H⁺ + HCO₃⁻"),
                Page("Другие формы","• Часть CO₂ связана с белками, включая Hb.\n• Часть растворена в плазме.\n• CO₂ не связывается с железом гема так, как O₂.","CO₂: HCO₃⁻ · БЕЛКИ · РАСТВОР"))
        };
    }
}
