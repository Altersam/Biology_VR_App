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
C — режим BioTool; короткое E в Scene09 — выстрел, удержание — очередь.
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

## Приток липидов и шесть эмболов

Бляшка пополняется из потока случайными партиями по 1–2 элемента в секунду,
пул 90 слотов. Победа требует уничтожения исходных и прибывших элементов.
Scene07 содержит шесть реальных пузырьков с отдельным переносом в ловушку.
Режимы: правая A вперёд, B назад; продолжение: левая Y / ПК →.
Динамика приборов и актуальные проверки: **INFLUX_EMBOLI_AND_CONTROLS_RU.md**.

## PICO 4 APK 0.3.0

Собран без ошибок после публикации WebGL/исходников. ARM64/IL2CPP/OpenXR,
versionCode 4, signature/manifest PASS. Файл: `Builds/Pico4/BiologyVR_Pico4_v0.3.0.apk`.
Размер 77 293 791 байт; SHA-256 — в `PICO4_APK_0_3_0.json`.
Физический шлем не проверен. Подробности: **PICO4_BUILD_0_3_0_RU.md**.
