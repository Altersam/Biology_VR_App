# Biology VR — Bio v0.1

Актуальный самостоятельный Unity-проект артериального маршрута Scenes03–16.
Подготовлен из текущей сцены и её проверенных зависимостей, с сохранением GUID/.meta.

## Открыть и запустить

1. Установить **Unity 6000.1.9f1** в Unity Hub.
2. Add project → выбрать эту папку, где находятся Assets, Packages, ProjectSettings.
3. Дождаться импорта и восстановления пакетов (нужны интернет и Git).
4. Открыть **Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity**.
5. Play → Pause выключена → кликнуть в Game.

Editor startup также назначает эту сцену стартовой для Play Mode. В Build Settings
оставлен единственный актуальный entry point. Library восстанавливается Unity.

## ПК

Мышь — указатель; ЛКМ удерживать — взять/перенести; отпустить — разместить;
колесо — глубина (0,15 м/tick); F удерживать — Scanner; E — BioTool/поле;
ПКМ + мышь — осмотр; G — альтернативный захват; Z — увеличение; → — продолжить.
Scene07: удерживать E, перенести указатель к зоне, колесом подобрать глубину.

## Сборки

- Meta Quest: Biology VR → Build Meta Quest APK.
- Pico: Biology VR → Build Pico 4 Enterprise APK.
- WebGL: Biology VR → Build WebGL For GitHub Pages.

Android Build Support/SDK/NDK/OpenJDK и WebGL Build Support устанавливаются через
Unity Hub. Для Android использовать ASCII-путь проекта; удобнее клонировать
репозиторий в `C:\Projects\Biology_VR\unity\BiologyVR`.

## Состав

Assets — текущая сцена, используемые модели/материалы/текстуры, код, shaders,
XR/input configs и Resources. Packages и ProjectSettings обязательны.
Это подготовленный runtime/build-проект; исходная полная authoring-папка и
история предыдущих версий сохранены отдельно. Генератор старого narrative source
не является способом запуска этой версии: используйте готовую master-сцену.

Некоторые используемые материалы имеют исторические V2-имена; они сохранены,
потому что на них ссылается актуальная сцена. Стенка — Artery_Concept_V4.

Инструкция GitHub/Pages: **GITHUB_UPLOAD_RU.md**. Содержимое Builds публикуется
отдельно: APK через GitHub Releases, WebGL через Pages, исходники — Git + Git LFS.
Эта копия находится в комплекте для **нового репозитория Biology_VR_App**:
в корне комплекта лежат инструкция загрузки и готовая браузерная play/.
Подтверждения запуска/переноса —
**READY_TO_OPEN_RU.md** и отчёты в Assets/BiologyVR/ArteryJourney/Reports.
