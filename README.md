# Biology VR App

Отдельный репозиторий игры. Концепт-сайт и готовый репозиторий
[Altersam/Biology_VR](https://github.com/Altersam/Biology_VR) сохраняются отдельно.

- `play/` — готовая браузерная Unity WebGL-сборка.
- `unity/BiologyVR/` — проверенный Unity-проект Bio v0.1, Unity **6000.1.9f1**.
- `index.html` — страница запуска игры на GitHub Pages.
- `ReleaseFiles/` — локальные APK для прикрепления к GitHub Releases; папка исключена из Git.

## Первичная загрузка

Следуйте файлу **КАК_ЗАГРУЗИТЬ.txt**. Имя нового репозитория — **Biology_VR_App**.
Сначала запустите `1_PREPARE_UPLOAD.cmd`, затем добавьте эту папку в GitHub Desktop,
сделайте Commit и Publish repository. Выберите Public для GitHub Pages.

Settings → Pages → Deploy from a branch → main → /(root).

После публикации:
- сайт: https://altersam.github.io/Biology_VR_App/
- игра: https://altersam.github.io/Biology_VR_App/play/

## Управление

Мышь — указатель; ЛКМ удерживать — захват и перенос; отпускание — размещение;
колесо — глубина; F удерживать — Scanner; E — BioTool/поле; ПКМ — осмотр;
G — альтернативный захват; Z — увеличение; → — продолжить завершённый этап.

В Scene07 продолжайте удерживать E, направьте указатель к стабилизационному
кольцу и колесом подберите глубину. Русский интерфейс использует встроенный Inter.

## Unity и APK

Открывайте в Unity Hub папку `unity/BiologyVR`, не корень репозитория.
Entry point: `Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
Для повторной Android-сборки clone лучше разместить в ASCII-пути:
`C:\Projects\Biology_VR_App\unity\BiologyVR`.

Для Quest/Pico — соответствующие инструкции внутри Unity-проекта.
WebGL проверен загрузкой в Chromium; полный Editor XR simulator run Scenes03–16
пройден. Проверка физических шлемов и полного ручного browser-прохождения отдельно.

Все файлы `play/` хранятся обычными Git-файлами. Git LFS применяется только
к бинарным ресурсам Unity через вложенную `.gitattributes`.
