# Biology VR App — 0.2.0 / Coral V6

Актуальная игра и самостоятельный Unity-проект **bio_v0.2**.
Сайт концепта [Altersam/Biology_VR](https://github.com/Altersam/Biology_VR) хранится отдельно.

**[Запустить в браузере](https://altersam.github.io/Biology_VR_App/play/)** ·
[Страница приложения](https://altersam.github.io/Biology_VR_App/) ·
[APK в Releases](https://github.com/Altersam/Biology_VR_App/releases)

## Эта версия

- Coral V6: более гладкий, кораллово-красный сосуд, крупные мягкие endothelial cells.
- Контрастные красные RBC, холодные светлые WBC и тёплые yellow/orange platelets.
- Один основной свет, лёгкий градиент глубины и визуальная пульсация 1,2% / 72 BPM.
- Без постоянного cyan-декора V5 и per-cell realtime displacement.
- Сохранены текущий маршрут, коллайдеры, интерактивы, Scanner/BioTool и переходы.
- Версия APK **0.2.0**, versionCode **3**; WebGL содержит тот же Coral V6.

## Структура

- `play/` — свежая WebGL-сборка с gzip + decompression fallback, single-thread.
- `unity/BiologyVR/` — актуальные Assets/.meta, Packages, ProjectSettings и Tools.
- `ReleaseFiles/` — локальные APK для Releases; папка исключена из Git.
- `VERSION.json` — версия и SHA-256 APK.
- `КАК_ЗАГРУЗИТЬ.txt` — короткая инструкция обновления через GitHub Desktop.

Unity: **6000.1.9f1**. Открывать в Unity Hub `unity/BiologyVR`, не корень репозитория.
Entry point: `Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
Данные до/после visual pass и скриншоты — `ARTERY_CORAL_V6_RU.md` внутри проекта.

## Управление ПК

Мышь — указатель; ЛКМ удерживать — захват/перенос; отпускание — размещение;
колесо — глубина; F удерживать — Scanner; E — BioTool/поле; ПКМ — осмотр;
G — альтернативный захват; Z — увеличение; → — продолжить завершённый этап.

В Scene07 удерживайте E, направьте указатель к стабилизационному кольцу и
колесом подберите глубину. Интерфейс использует встроенный Inter с кириллицей.

## Обновление на GitHub

В GitHub Desktop выбрать **Biology_VR_App**, проверить Changes, Summary:
**Update to bio_v0.2 Coral V6**, затем **Commit to main → Push origin**.
Повторный Publish repository не нужен. GitHub Pages: main → /(root).

Для APK создать release **v0.2.0** и прикрепить обе APK из ReleaseFiles.
Эти файлы не входят в обычный commit. Все файлы `play/` хранятся обычным Git,
Git LFS применяется только к Unity-ресурсам вложенной .gitattributes.

## Проверки

WebGL собран и проверен загрузкой в Chromium без специальных HTTP headers.
APK Quest/Pico: ARM64/IL2CPP/OpenXR, manifest и подпись PASS, сборки без ошибок.
Editor: PC-input PASS и sequential simulated-XR Scenes03–16 + VERY FAR PASS.
Аппаратные FPS/GPU/comfort на шлемах и полный ручной browser run пока не измерены.
