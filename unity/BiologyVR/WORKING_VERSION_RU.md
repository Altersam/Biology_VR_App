# bio_v0.2 — рабочая копия для дальнейшей разработки

Проект: `Z:\YandexDisk\ПетПроекты\Biology_VR\3д\Unity\bio_v0.2`.
Unity: **6000.1.9f1** (версия закреплена в ProjectSettings/ProjectVersion.txt).
Источник: актуальный `Bio_v.0.1`, сохранённый перед копированием.

Скопированы все Assets с .meta, Packages/manifest.json и packages-lock.json,
ProjectSettings, Tools и документация. SHA-256 проверка 1711 файлов PASS.
Library, Temp и IDE-файлы восстанавливаются Unity при открытии проекта.
`TRANSFER_VERIFICATION.json` фиксирует сравнение до импорта; дальнейшие
редакторские файлы и результаты тестирования появляются уже в bio_v0.2.

Master scene:
`Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
Она назначена entry point и стартовой сценой Play Mode. Текущая стенка — Coral V6,
мышь/клавиатура, XRI, Inter с кириллицей и Android/WebGL build tools сохранены.
Android ASCII alias для этой версии: `Z:\BiologyVR_bio_v02_build`;
создаётся через Tools/create_ascii_build_alias.ps1, затем меню
Biology VR/Reopen At ASCII Build Alias. Он относится к этой копии.

## Запуск

Unity Hub → Add project → выбрать `bio_v0.2` → Unity 6000.1.9f1.
После импорта открыть master scene и нажать Play. Кликнуть в Game.
Мышь — указатель; ЛКМ — перенос; колесо — глубина; F/E — инструменты;
ПКМ — осмотр; Z — увеличение; G — альтернативный захват; → — продолжить.
C — режим BioTool; для фагоцита удерживать E/CAPTURE → колесом натяжение → навести → отпустить.
Краткая теория и задача видны сразу; Tab / «Подробнее» — полная справка.
Колесо / PageUp/PageDown — её скролл; в VR — правый стик над открытой справкой.

## Проверка

Результаты новой проверки записаны после visual-only прохода V6:
- `CURRENT_PROJECT_TRANSFER.json` — зависимости этой копии;
- `Assets/BiologyVR/ArteryJourney/Reports/Polish/FlowPickup/NativeCursor_PC_Input.json`;
- `Assets/BiologyVR/ArteryJourney/Reports/Polish/VR_INPUT_PLAYTHROUGH_V7.md`.

Отчёты, датированные до создания bio_v0.2, являются историей версии 0.1.
Полный маршрут проверяется simulated XR device events, а не физическим шлемом.

Последние подтверждения:
- Compile: 0 errors / 0 warnings.
- PC input UTC 2026-10-08T20:43:55.0063958Z: PASS, Console 0 errors.
- Sequential XR UTC 2026-10-08T20:52:38.3254711Z: FULL SIMULATOR PASS, Console 0 errors.
- Dependency audit UTC 2026-10-08T20:56:31.4969328Z: PASS, no missing GUID/scripts.

Текущий арт-проход и кадры: **ARTERY_CORAL_V6_RU.md**.
Стенка более гладкая/красно-коралловая; realtime cell displacement и постоянный
cyan-декор V5 выключены. Геометрия, игровые контакты и input-пути сохранены.
Backup до V6 — Assets/BiologyVR/ArteryJourney/Reports/Polish/BioWorldV6/Backup.
Проверенный самостоятельный pre-art backup на Z: — bio_v0.2_before_art_verified.

GitHub-комплект обновляется отдельно в `Biology_VR_App`. Сборки этой итерации
имеют версию **0.2.0**, Android versionCode **3**, текущий Coral V6.
Актуальные даты/результаты — Reports/WebGLBuildReport.json, MetaQuestBuildReport.json,
PicoBuildReport.json и соответствующие artifact verification reports.

## Активная итерация — 9 октября 2026

Установлены пользовательские Scanner/BioTool и рамки двух голографических экранов.
Четыре детали BioTool обращаются вокруг общей оси; первый эпизод требует
извлечения движущихся объектов, Scene09 содержит 24 цели и быстрый выстрел/очередь.
Основная теория открыта по умолчанию, прокручивается и размещена ближе к центру VR-обзора.

Описание: **ACTIVE_TOOLS_AND_HOLOGRAMS_RU.md**.
Последние статусы PC/XR — в тех же отчётах выше; проверяйте их UTC, а не
исторические даты V6 в разделе «Последние подтверждения».
APK/WebGL из предыдущего раздела ещё относятся к версии до этой итерации.

## Чистый голографический первый проход — 10 октября 2026

Визуальное описание и актуальные кадры: **CLEAN_HOLOGRAPHIC_FIRST_PASS_RU.md**.
Главный вид — краткая теория/схема/задача; Tab или «Подробнее» открывает
полную прокручиваемую справку. Показатели разделены на пять карточек.
Подвижные части и условия действий инструментов сохранены; их визуал
приведён к общей cream / graphite / cyan / amber системе.
Дальнейшая полировка — после оценки этого первого прохода.

## Второй проход читаемости

По следующему запросу выполнена локальная полировка: контрастнее стекло,
крупнее текст и значения, foreground UI без перекрытия букв кровяными клетками,
активные sweep/crown индикаторы инструментов. Описание и сравнение —
**HOLOGRAPHIC_SECOND_PASS_RU.md**. Gameplay/InputActions сохранены.

## Биомедицинские приборы и непрерывная циркуляция

По следующему запросу действующие модели заменены компактной Unity-геометрией.
Фон переиспользует 355 слотов, пять учебных слотов сохранены. Циркуляция случайная
и не ограничена одним прохождением: локальный поток впереди/позади + глобальный
поток во всей артерии и ветвях. Смотрите **BIOMEDICAL_LIVING_PASS_RU.md** и
`Reports/Polish/FlowPickup/Continuous_Random_Flow.json`.

## Растущая бляшка — Scene06

По запросу пользователя прежняя трёхзонная задача заменена растущей объёмной массой
из 30 отдельных элементов. На бой 55 секунд; окклюзия вызывает локальный повтор,
полная очистка восстанавливает локальные показатели. Вход/сканирование/выбор режима
и остальной маршрут сохранены. Эффекты и проверки — **GROWING_PLAQUE_CHALLENGE_RU.md**.

## Продольный разрыв и гемостаз — 11 октября 2026

Описание текущей механики: **LACERATION_HEMOSTASIS_RU.md**.
BioTool CAPTURE удерживает тромбоцит из потока; подход к повреждённой поверхности
вызывает автоматическую адгезию и изменение формы. 11/11 — первичная пробка
с остаточной утечкой. Отдельная цепочка ADP → GPIIb/IIIa + фибриноген → тромбин/фибрин
запускает постепенную стабилизацию; EDTA отвергается как ингибитор коагуляции.
Утечка отводит часть существующего основного пула, поток после раны продолжается.
Баланс пробки и восстановление эндотелия остаются отдельными этапами 13 и 14.

Последние результаты этой итерации — `Reports/Polish/FlowPickup/Laceration_PC_Input.json`
и `Reports/Polish/VR_INPUT_PLAYTHROUGH_V7.md`; ориентируйтесь на UTC и статус отчёта.
Опубликованные WebGL/PICO 4 0.3.0 пока содержат предыдущую итерацию.

Последующее уточнение: **PERSISTENT_BLOOD_ALL_SCENES_RU.md** — сохранять насыщенный
движущийся поток с той же палитрой на всех артериальных этапах, включая 15 и 16.
Визуальная циркуляция отделена от игрового FlowMultiplier; существующий пул
восполняет локальную плотность только через невидимые места входа.
Проверка: `Reports/Polish/FlowPickup/All_Scenes_Persistent_Blood.json`.

XR-контроллеры: **XR_CONTROLLER_LIFECYCLE_RU.md**. UI больше не отключает
весь JourneyInput; блокировка раздельна по рукам, нажатие читается из live action,
направляющие сохраняются без Trigger. Смена режима и активация анимированы явно.
Изолированный XR lifecycle PASS UTC 2026-10-11T12:53:13.4333497Z;
отчёт — `Reports/Polish/XRControllers/Input_Lifecycle.md`.

После прерванной перезагрузки проверена сохранность критичных исходников,
ссылок master-сцены, материалов/геометрии разрыва и документации.
SHA-проверенная точка на Z: — `RecoveryCheckpoints/20261011_131549` (151 файл);
манифест — `SESSION_CHECKPOINT_VERIFICATION.json`. Дальнейшие исправления
и свежие runtime-отчёты проверяются отдельно; последнюю резервную точку
можно создать `Tools/verify_session_checkpoint.py`.

Следующее оформление экранов — **FUTURISTIC_HOLOGRAMS_RU.md**: геометрические
голографические рейки/скобы, Inter Bold и CanvasScaler 2.5. Панель теории
смещена немного наружу; показатели правее и выше. Контейнеры, ScrollRect,
runtime callbacks и master-сцена сохранены. Свежий XR lifecycle PASS
UTC 2026-10-11T13:32:44.3218172Z включает новое расположение UI.

Полный маршрут с новым гемостазом/циркуляцией/XR/UI: **FULL SIMULATOR PASS**
UTC **2026-10-11T13:40:13.9199015Z**, Scenes03–16 + VERY FAR.
Проверка сохранённой master-сцены после перезагрузки: **PASS**
UTC **2026-10-11T13:48:54.5858628Z** — 630 зависимостей сцены,
без missing scripts/assets, 67 targets, пул 360, все новые ссылки/UI/шрифты валидны.
Отчёт — `Assets/BiologyVR/ArteryJourney/Reports/SessionRecovery/SavedScene.json`.

Свежий ПК-гемостаз **PASS** UTC **2026-10-11T13:50:54.2223607Z**;
бляшка/окклюзия/повтор **PASS** UTC **2026-10-11T13:53:15.2044818Z**.
В последнем тесте подтверждена непрерывная насыщенная циркуляция даже при
неудачной попытке с условной окклюзией. Console после прогона — 0 errors.
Сводные кадры: `Reports/Polish/FuturisticHolograms/Gameplay_Review.jpg`,
`Reports/LacerationHemostasis/Gameplay_Review.jpg`,
`Reports/Polish/PersistentBlood/All_Scenes_Review.jpg`.

## Новый гемодинамический эксперимент — Scenes05/10

**HEMODYNAMICS_EXPERIMENT_RU.md** описывает замену отдельной картинки потока
измерениями A/B, диаметра и скорости прямо на артерии. Ручной TONUS,
две геометрические ситуации и третья загадка с физическими карточками.
Позднее падение давления стало самостоятельной диагностикой, которая
ведёт к существующей ране без необоснованного изменения местного радиуса.
Компактный HUD — короткая задача и АД/пульс/расход. Поток остаётся постоянным.
ПК fixture **PASS** UTC **2026-10-11T14:35:01.0056828Z**;
Финальный ПК после полировки: **PASS** UTC **2026-10-11T14:53:06.284476Z**.
Полный XR с новым экспериментом и поздней диагностикой: **FULL SIMULATOR PASS**
UTC **2026-10-11T15:07:32.1062382Z**. Мобильный пул 180 в Scenes03–16:
**PASS** UTC **2026-10-11T14:58:53.0870741Z**. Console по завершении — 0 errors.
Кадры — `Reports/Polish/HemodynamicsExperiment/Gameplay_Review.jpg`.
По запросу пользователя исходники/WebGL и PICO APK публикуются как **0.4.0**;
фактические результаты — **RELEASE_0_4_0_RU.md** и отчёты сборки.

## Публикация 0.4.0

WebGL 0.4.0 — Succeeded, 0 errors / 7 warnings; загрузка в Chromium PASS.
PICO 4 APK 0.4.0 / code 5 — Succeeded, 0 errors / 14 warnings;
manifest/ARM64/IL2CPP/OpenXR/PICO metadata/signature PASS.
Файл — `Builds/Pico4/BiologyVR_Pico4_v0.4.0.apk`, 77 238 537 байт.
SHA-256 — `eebbf0396a197c01399db0e69dff5dd641703de0b512ceab5f1062513ce2f596`.

Исходники и WebGL опубликованы commit `f2ff9eb5c2b15addfff26d87e3f301119f778f4c`
в `Altersam/Biology_VR_App`. GitHub Pages deployment **success**,
run **38151833857**; опубликованная страница игры подтверждает `productVersion: 0.4.0`.
PICO APK и SHA256SUMS прикреплены к
https://github.com/Altersam/Biology_VR_App/releases/tag/v0.4.0 .
GitHub asset digest совпадает с локальным APK. Unity возвращена к оригинальному
проекту `bio_v0.2` на Z: и Desktop target; master сохранён, Console 0 errors.

## Свободное первое исследование — следующая рабочая итерация

**FREE_BLOOD_RESEARCH_RU.md**: первый артериальный эпизод (внутренний Scene03)
больше не задаёт порядок RBC → WBC → platelet → plasma. Все восемь существующих
объектов доступны с начала; четыре основных компонента обязательны, Hb,
гем/Fe²⁺, O₂ и CO₂ независимы. ScriptableObject страницы, индивидуальные статусы,
контекстный профессор, повторное открытие без зачёта и обычный Continue после 4/4.
В рамках прохождения состояния и страницы сохраняются; кровь не останавливается.
Свежие PC/XR результаты — `Free_Blood_Research_PC_Input.json` и
`VR_INPUT_PLAYTHROUGH_V7.md`. APK/WebGL 0.4.0 — предыдущая опубликованная итерация.

Подтверждения свободного первого исследования:
- ПК: все 24 порядка **PASS**, UTC 2026-10-11T19:44:44.5670239Z.
- XR: полный маршрут + VERY FAR **FULL SIMULATOR PASS**,
  UTC 2026-10-11T19:59:45.4835926Z, включая O₂↔гем и двуручное увеличение.
- Saved scene **PASS**, UTC 2026-10-11T20:06:53.24966Z:
  647 зависимостей, journal/theory ссылки валидны, нет missing scripts/assets.
- Compile 0 errors / 0 warnings, Console 0 errors.
Обзор — `Reports/Polish/FreeBloodResearch/Gameplay_Review.jpg`.

## Сборки свободного исследования — 0.5.0

По запросу пользователя обновлены WebGL и два отдельных Android-профиля:
PICO 4 / PICO 4 Enterprise и Meta Quest 2 / Pro / 3 / 3S (Oculus).
Версия **0.5.0**, Android versionCode **6**.
Обе сборки — release (`BuildOptions.None`), ARM64 / IL2CPP / OpenXR / OpenGLES3.
PICO: 77 256 609 байт, 0 errors / 14 warnings, manifest/signature/runtime PASS.
Quest: 76 372 394 байта, 0 errors / 12 warnings, Meta devices/category/headtracking,
подпись и отсутствие PICO runtime PASS. WebGL: 0 errors / 7 warnings, Chromium PASS.
Ссылки, параметры и SHA-256 — **RELEASE_0_5_0_RU.md**.
Локальные APK: `Builds/Pico4/BiologyVR_Pico4_v0.5.0.apk` и
`Builds/MetaQuest/BiologyVR_MetaQuest_v0.5.0.apk`.

Публикация: source/build commit `e89446b073bd9bc00680ca2525174e7ebff4ad21`
отправлен в `Altersam/Biology_VR_App`. Release **v0.5.0** содержит оба APK
и `SHA256SUMS_0_5_0.txt`; GitHub asset digest обоих APK совпадает с локальными.
Pages deployment **success**, run **38206216380**; опубликованный WebGL
подтверждает `productVersion: 0.5.0`. Unity возвращена к оригинальному проекту
на Z: / Desktop target; master сохранён, Console 0 errors.

## Связанный живой организм — текущая итерация 0.6.0

Описание и 18-пунктный отчёт: **CONNECTED_LIVING_ORGANISM_RU.md**.
Пять независимых популяций общего потока: RBC, WBC, фагоцит, тромбоцит, плазма.
Ограниченная promotion конкретного экземпляра в XRI proxy; фон 360 / mobile 180
без массовой физики. Hb/гем/Fe²⁺/O₂/CO₂ открываются во вложенном анализе.
Первый вирус — один учебный цикл; позже используются те же capture/pull/launch,
дрейф крови и постепенный фагоцитоз в Scenes09/15.

Подтверждения этой итерации:
- Connected PC **PASS**, UTC `2026-10-12T16:15:29.1420189Z`.
- Все **120 порядков** пяти типов **PASS**, UTC `2026-10-12T16:00:12.2225607Z`.
- Полный connected XR03–16 + VERY FAR **FULL SIMULATOR PASS**,
  UTC `2026-10-12T13:04:38.8496225Z`.
- Mobile 180 / все сцены / 90 с финального ожидания **PASS**,
  UTC `2026-10-12T16:02:22.5442223Z`.
- Saved scene **PASS**, UTC `2026-10-12T16:26:52.0774834Z`, 653 зависимости,
  без missing scripts/assets; connected references валидны.
- SHA checkpoint: `RecoveryCheckpoints/20261012_162746`, 385 файлов.

Версия новых сборок: **0.6.0 / Android versionCode 7**. Фактический статус
сборки и публикации — актуальные build reports и `RELEASE_0_6_0_RU.md`.
Предыдущие разделы описывают историю развития проекта.

Публикация 0.6.0 проверена UTC `2026-10-12T18:59:12.179267+00:00`:
- Source/WebGL commit `f80a9dbe34f76b27e5912fe9614815d4da78a225` отправлен в приложение.
- GitHub Release `v0.6.0` содержит PICO APK, Meta Quest APK и SHA256SUMS.
- GitHub digest обоих APK совпадает с проверенными локальными файлами.
- Pages deployment `38294283709` — success; публичный WebGL сообщает версию 0.6.0,
  loader/framework совпадают с локальной сборкой.
- `RELEASE_PUBLICATION_0_6_0.json` содержит ссылки и результаты проверки.

## Переработка после прохождения — 0.7.0 / Android code 8

Текущая версия механик: **PLAYER_FEEDBACK_REWORK_RU.md**. Клетки захватываются
полем BioTool; газы и железо являются грузом носителя, автоматический анализ
отключён. Сосудистая сцена использует два основных экрана; режимы выбираются
вручную. Эмболы уходят с наведённого поля, вирус требует трёх контактов свежих
фагоцитов через захват → Scanner → активацию → реверс. Помощь появляется после минуты.
Актуальные PC/XR/obstacle проверки — `Reports/Polish/PlayerFeedbackRework/`.
Пользователь запросил отдельную сборку APK для PICO 4 этой итерации.

PICO APK **0.7.0 готов**, UTC `2026-10-13T14:37:00.0163091Z`:
0 errors / 12 warnings; версия, manifest, ARM64/OpenXR и подпись PASS.
Файл: `Builds/Pico4/BiologyVR_Pico4_v0.7.0.apk`, 77 591 775 байт.
Инструкция/управление/SHA: **PICO4_BUILD_0_7_0_RU.md**.

Meta Quest / Oculus APK **0.7.0 готов**, UTC `2026-10-13T17:08:29.9205505Z`:
0 errors / 12 warnings; Meta devices/headtracking/подпись PASS, PICO runtime отсутствует.
Файл: `Builds/MetaQuest/BiologyVR_MetaQuest_v0.7.0.apk`, 76 707 544 байта.
WebGL **0.7.0 Succeeded**, UTC `2026-10-13T17:19:55.8699179Z`:
0 errors / 8 warnings; Chromium loading smoke PASS.
Общий релиз и контрольные суммы — **RELEASE_0_7_0_RU.md**.

Публикация **v0.7.0** подтверждена UTC `2026-10-13T17:32:52.824695+00:00`:
source/WebGL commit `057ee4f6c833c5ae8c827f9e3e49329656a14433`, оба APK загружены,
GitHub SHA-256 совпадают с локальными. Pages run `38439296701` — success;
публичный WebGL сообщает 0.7.0, loader/framework совпадают со сборкой.
Ссылки и результаты — `RELEASE_PUBLICATION_0_7_0.json`.
