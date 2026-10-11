# Biology VR App — 0.4.0 / Hemodynamic Experiment

Актуальная игра и самостоятельный Unity-проект **bio_v0.2**.
Сайт концепта [Altersam/Biology_VR](https://github.com/Altersam/Biology_VR) хранится отдельно.

**[Запустить в браузере](https://altersam.github.io/Biology_VR_App/play/)** ·
[Страница приложения](https://altersam.github.io/Biology_VR_App/) ·
[APK в Releases](https://github.com/Altersam/Biology_VR_App/releases)

## Эта версия — Unity, WebGL и PICO 4 APK 0.4.0

- Coral V6: более гладкий, кораллово-красный сосуд, крупные мягкие endothelial cells.
- Контрастные красные RBC, холодные светлые WBC и тёплые yellow/orange platelets.
- Один основной свет, лёгкий градиент глубины и визуальная пульсация 1,2% / 72 BPM.
- Без постоянного cyan-декора V5 и per-cell realtime displacement.
- Новая компактная Unity-геометрия Scanner и BioTool, общая cream/graphite/cyan/amber система.
- Краткая теория/задача и отдельная прокручиваемая справка; монитор из пяти карточек.
- Непрерывный случайный кровоток впереди/позади и во всех трёх ветвях: общий пул 360, пять учебных слотов сохранены.
- Бляшка: 30 начальных элементов + случайный приток 1–2 липидов в секунду; стрельба по летящим и прикреплённым элементам, окклюзия/локальный повтор.
- Шесть самостоятельных газовых эмболов: каждый надо захватить и удерживаемым полем перенести в ловушку; выполненные размещения сохраняются при отпускании.
- Правые **A/B** — режимы BioTool вперёд/назад, **левая Y** — Continue. Импульс, захват, анимации корпуса и фиксация объекта имеют разные эффекты.
- Сохранены текущий маршрут, остальные взаимодействия, InputActions и состояния мира.
- **Scene05:** новый гемодинамический эксперимент внутри артерии: Scanner FLOW,
  измерения A/B, скорость и диаметр; ручной TONUS; три ситуации и физические карточки гипотез.
- **Scene10:** самостоятельная диагностика низкого давления без повторного упражнения на сужение сосуда.
- **WALL_A:** продольный разрыв; утечка отводит существующие клетки основного пула.
  Удерживаемый CAPTURE доставляет тромбоциты, адгезия автоматическая. 11/11 — первичная
  пробка с остаточной утечкой; карточки активации/агрегации/тромбин-фибрина запускают постепенную стабилизацию.
- Насыщенный фон продолжается во всех артериальных сценах, включая финальные;
  UI больше не отключает весь ввод, режимы анимированы и направляющие сохраняются.
- Футуристичные голографические экраны с Bold-текстом; показатели правее и выше.
- **[Скачать PICO 4 APK 0.4.0](https://github.com/Altersam/Biology_VR_App/releases/download/v0.4.0/BiologyVR_Pico4_v0.4.0.apk)** ·
  [Release и SHA-256](https://github.com/Altersam/Biology_VR_App/releases/tag/v0.4.0).
  Android versionCode **5**; ARM64 / IL2CPP / OpenXR / PICO.
- Исторические Android-файлы и их проверки сохранены отдельно; эта публикация обновляет PICO APK.

## Структура

- `play/` — свежая WebGL-сборка с gzip + decompression fallback, single-thread.
- `unity/BiologyVR/` — актуальные Assets/.meta, Packages, ProjectSettings и Tools.
- `ReleaseFiles/` — локальные APK для Releases; папка исключена из Git.
- `VERSION.json` — версия Unity/WebGL и отдельно версия ранее опубликованных APK с SHA-256.
- `RELEASE_0_4_0_RU.md` внутри Unity-проекта — проверенные сборки, параметры и изменения.
- `КАК_ЗАГРУЗИТЬ.txt` — короткая инструкция обновления через GitHub Desktop.

Unity: **6000.1.9f1**. Открывать в Unity Hub `unity/BiologyVR`, не корень репозитория.
Entry point: `Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
Данные до/после visual pass и скриншоты — `ARTERY_CORAL_V6_RU.md` внутри проекта.

## Управление ПК

Мышь — указатель; ЛКМ удерживать — захват/перенос; отпускание — размещение;
колесо — глубина; F удерживать — Scanner; E — BioTool/поле; ПКМ — осмотр;
G — альтернативный захват; Z — увеличение; → — продолжить завершённый этап.
C — смена режима BioTool; Tab / «Подробнее» — полная теория; колесо над ней — скролл.

В Scene07 удерживайте E, направьте указатель к стабилизационному кольцу и
колесом подберите глубину. Интерфейс использует встроенный Inter с кириллицей.
Повторите размещение для всех шести пузырьков. Scene06: E — импульс, удержание — очередь;
очистите исходную массу и прилетающие липиды до перекрытия просвета.
Scene05: F — локальные измерения; удерживать E в TONUS над регулятором,
колесо — радиус; ЛКМ — карточки. Scene12: удерживать CAPTURE, подвести
тромбоцит к повреждению до автоматической адгезии, затем собрать цепочку стабилизации.

## Управление VR

Левый Trigger — Scanner, правый Trigger — выбранное действие BioTool.
Правая A — следующий режим, B — предыдущий; левая Y — продолжить завершённый этап.
Grip — захват руками, две руки — учебное увеличение; правый стик над справкой — скролл.

## Обновление на GitHub

В GitHub Desktop выбрать **Biology_VR_App**, проверить Changes, Summary:
**Update hemodynamic experiment and PICO 0.4.0**, затем **Commit to main → Push origin**.
Повторный Publish repository не нужен. GitHub Pages: main → /(root).

PICO APK публикуется в release **v0.4.0** из проверенного файла в ReleaseFiles.
Эти файлы не входят в обычный commit. Все файлы `play/` хранятся обычным Git,
Git LFS применяется только к Unity-ресурсам вложенной .gitattributes.

## Проверки

WebGL собран и проверен загрузкой в Chromium без специальных HTTP headers.
APK Quest/Pico: ARM64/IL2CPP/OpenXR, manifest и подпись PASS, сборки без ошибок.
Editor: PC-input PASS, приток липидов и локальный повтор PASS, размещение всех шести
эмболов PASS, sequential simulated-XR Scenes03–16 + VERY FAR PASS.
Описание: `unity/BiologyVR/HEMODYNAMICS_EXPERIMENT_RU.md`, `LACERATION_HEMOSTASIS_RU.md`,
`PERSISTENT_BLOOD_ALL_SCENES_RU.md`, `XR_CONTROLLER_LIFECYCLE_RU.md`, `FUTURISTIC_HOLOGRAMS_RU.md`.
Аппаратные FPS/GPU/comfort на шлемах и полный ручной browser run пока не измерены.
