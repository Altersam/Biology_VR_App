# Unity-проект bio_v0.2 — Coral V6 / 0.2.0

Откройте эту папку (`unity/BiologyVR`) в Unity Hub с **Unity 6000.1.9f1**.
Library восстанавливается при импорте. Packages/manifest.json и packages-lock.json
содержат зависимости; нужен интернет/Git для восстановления Git packages.

Master scene: `Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
Она назначается entry point и стартовой сценой Play Mode.

Все актуальные Assets/.meta, код, shaders, XR/input settings, материалы Coral V6,
нормальные карты и Inter с кириллицей включены. Механика и collider/path
signatures подтверждены неизменными при visual-only переработке.

ПК: мышь — указатель, ЛКМ — удерживать/перенести, колесо — глубина, F/E —
Scanner/BioTool, ПКМ — осмотр, G — альтернативный захват, Z — увеличение, → — переход.

Документы: **WORKING_VERSION_RU.md**, **ARTERY_CORAL_V6_RU.md**,
**META_QUEST_RU.md**, **PICO4_ENTERPRISE_RU.md**, **GITHUB_UPLOAD_RU.md**.
Отчёты и скриншоты — Assets/BiologyVR/ArteryJourney/Reports.

Сборки через меню Biology VR: Build WebGL For GitHub Pages, Build Meta Quest APK,
Build Pico 4 Enterprise APK. Для Android используйте ASCII-путь clone,
например `C:\Projects\Biology_VR_App\unity\BiologyVR`.
APK должны иметь 0.2.0 / versionCode 3. Готовые бинарные файлы находятся в корне
репозитория в play/ и локальной ReleaseFiles/; APK публикуются через Releases.

Это самостоятельный текущий runtime/build-проект. Для восстановления всей
исторической authoring-цепочки генераторов нужны отдельные исходные версии;
обычный запуск и сборка используют готовую master-сцену.
