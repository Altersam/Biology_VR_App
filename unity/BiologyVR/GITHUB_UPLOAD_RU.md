# GitHub — обновление Biology_VR_App до 0.2.0 / Coral V6

Подготовленная папка репозитория:
`Z:\YandexDisk\ПетПроекты\Biology_VR\3д\Unity\Biology_VR_App`.
Она уже подключена к https://github.com/Altersam/Biology_VR_App.
Исходный концепт-сайт https://github.com/Altersam/Biology_VR остаётся отдельным.

## Что обновляется

- `unity/BiologyVR/` — актуальные Assets, .meta, Packages, ProjectSettings, Tools и документы bio_v0.2.
- `play/` — новая WebGL-сборка Coral V6, gzip + decompression fallback, без threads.
- `ReleaseFiles/` — APK Quest/Pico версии 0.2.0 (versionCode 3).
- `README.md`, стартовая страница, VERSION.json и инструкция — актуальная версия.

## Загрузить обновление

1. Открыть GitHub Desktop и выбрать **Biology_VR_App**.
2. В Changes проверить изменения. Library/Temp/Logs и APK там быть не должны.
3. Summary: **Update to bio_v0.2 Coral V6**.
4. **Commit to main**.
5. **Push origin**. Создавать новый репозиторий/Publish повторно не нужно.

GitHub Pages остаётся: Settings → Pages → Deploy from a branch → main → /(root).
После публикации игра: https://altersam.github.io/Biology_VR_App/play/.
Если браузер показывает старую версию, обновить страницу Ctrl+F5.

APK не входят в commit. Для новых загрузок в шлемах: Releases → Draft a new release →
Tag **v0.2.0**, прикрепить обе APK из ReleaseFiles и Publish release.

## Правила файлов

WebGL `play/` хранится обычными Git-файлами без LFS. Бинарные Unity-ресурсы
обрабатывает вложенная `unity/BiologyVR/.gitattributes`; все .meta сохраняются.
Не переносить .git из других проектов. Unity-кэши, ключи подписи, bridge-token,
machine-local MCP settings и APK исключены из Git.

Команда сборки WebGL: **Biology VR → Build WebGL For GitHub Pages**.
Результат — `Builds/WebGL/play` в текущем Unity-проекте. APK строятся отдельными
меню Meta Quest / Pico 4 Enterprise. Для Android используется ASCII junction
**Z:\BiologyVR_bio_v02_build**, относящийся к текущему bio_v0.2.

Проверки: `GITHUB_UPDATE_VERIFICATION.json`, Reports/WebGLBrowserSmoke.json,
Reports/MetaQuestApkVerification.json, Reports/PicoApkVerification.json.
Проверки APK/браузерной загрузки не заменяют физический hardware FPS/comfort test.
