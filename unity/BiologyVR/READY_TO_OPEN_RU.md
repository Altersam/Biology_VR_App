# Проверенный перенос Bio v0.1

## Какой проект открыть

Unity Hub → Add project → `Bio_v.0.1` → Unity **6000.1.9f1**.
Текущая сцена автоматически назначена entry point:
`Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
Стенка использует `Artery_Concept_V4/Coral_Endothelium_Concept_V4.mat`.

## Что было обработано перед переносом

- В текущем проекте сохранена master-сцена и назначен единственный Build Settings entry.
- Убраны dangling obsolete xrSystemData GUID в URP renderer presets.
- Unity AssetDatabase собрал текущие scene/config dependencies; код, shaders,
  input/XR settings, Resources и .meta сохранены.
- Старые сцены/prototype assets/capture backups, не входящие в комплект
  зависимостей, оставлены в исходном проекте и резервной копии.
- Перенесены 678 dependency assets; перенос файлов проверен SHA-256.
- Предыдущая полная копия сохранена отдельно:
  `Bio_v.0.1_before_cleanup_20261008_151718`.
- Исправлена иерархия desktop XRI hand: находится под XR Origin, как требует XRI.

Часть материалов имеет V2-имена, но всё ещё используется актуальными клетками,
UI и инструментами. Они не удалялись только по возрасту/имени.

## Реальные проверки нового проекта

- Новый Library импортирован Unity; мастер-сцена открылась в новом проекте.
- Compile: 0 errors / 0 warnings.
- `CURRENT_PROJECT_TRANSFER.json`: missingScripts=[], unresolved=[], current V4.
- PC input `NativeCursor_PC_Input.json`, UTC **2026-10-08T12:43:23.9982008Z**:
  PASS, LMB drag/release, Scanner, эмбол, normalized wheel, platelet placement,
  tone excessive/correction/settle, dormant fibrin references.
- `VR_INPUT_PLAYTHROUGH_V7.md`, UTC **2026-10-08T12:53:48.7975642Z**:
  FULL SIMULATOR PASS, Scenes03–16 + VERY FAR. Console в ходе run: 0 errors.

Перенос — самостоятельный стартовый runtime/build-проект; повторная генерация
из старого исходного narrative source относится к полной authoring-папке.

## GitHub

Эта копия собрана в отдельный комплект **Biology_VR_App**. Используйте
`КАК_ЗАГРУЗИТЬ.txt` в корне комплекта для создания нового репозитория.
Существующий https://github.com/Altersam/Biology_VR остаётся без изменений.
Git LFS настроен вложенной .gitattributes. APK публикуются через Releases.

## Meta Quest / WebGL

Quest: `Builds/MetaQuest/BiologyVR_MetaQuest.apk`, инструкция `META_QUEST_RU.md`.
Artifact checks — `Reports/MetaQuestApkVerification.json`.
WebGL/Pages: отдельный профиль **Build WebGL For GitHub Pages** и
`GITHUB_UPLOAD_RU.md`; browser build помещается в `play/` сайта.
XR Android runtime не является WebXR; браузерный профиль использует ПК-input.
WebGL build UTC **2026-10-08T14:41:55.176007Z**: Succeeded.
`WebGLBrowserSmoke.json`: PASS, Chromium, обычный HTTP без compression/COOP/COEP
headers; просмотрен кадр с отображаемой кириллицей. Шрифт Inter с Cyrillic glyphs
включён в интерфейс и runtime onboarding, а не берётся из системного fallback.

Build reports могут содержать сообщения от служебного MCP editor test-файла
без .meta. Это известная проблема установленного tooling package; файл не
включён в player. Компиляция игрового кода проходит; artifact/load checks
записаны отдельно. Сторонние package-cache файлы не правились.
