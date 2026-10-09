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
