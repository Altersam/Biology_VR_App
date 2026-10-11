# Biology VR 0.4.0 — гемодинамический эксперимент

[Играть в браузере](https://altersam.github.io/Biology_VR_App/play/) ·
[Скачать PICO APK](https://github.com/Altersam/Biology_VR_App/releases/download/v0.4.0/BiologyVR_Pico4_v0.4.0.apk) ·
[GitHub Release](https://github.com/Altersam/Biology_VR_App/releases/tag/v0.4.0)

## Изменения

- Scene05: измерения A/B, скорости и диаметра прямо в артерии; Scanner FLOW,
  тонкие AR-кольца/линии и семь диагностических трассеров.
- Три головоломки: узкий участок, широкий участок, низкое давление при нормальном
  диаметре. Ручной TONUS требует стабильного удержания и повторной проверки;
  третья ситуация решается физической карточкой по измеренным данным.
- Scene10: самостоятельная диагностика низкого АД без повторного упражнения на тонус;
  обоснованный вывод ведёт к поиску повреждения дальше по маршруту.
- Продольный разрыв WALL_A, отвод части настоящего фонового пула, захват тромбоцитов
  удерживаемым CAPTURE и автоматическая адгезия. 11/11 — первичная пробка;
  отдельная цепочка активация/агрегация/тромбин-фибрин запускает постепенную стабилизацию.
- Постоянная насыщенная циркуляция на ранних и последних артериальных этапах.
- XR: live polling Trigger, раздельная блокировка рук интерфейсом, явные анимации
  режимов/активации и постоянные направляющие.
- Голографическое оформление, Bold-текст с повышенной растеризацией; показатели
  правее/выше, компактный трёхпоказательный HUD в гемодинамических опытах.

## Проверки

- Полный маршрут Scenes03–16 + VERY FAR: **FULL SIMULATOR PASS**, XR InputActions/XRI.
- ПК-гемодинамика: **PASS**, UTC `2026-10-11T14:53:06.284476Z`.
- Циркуляция с мобильным пулом 180: **PASS**, UTC `2026-10-11T14:58:53.0870741Z`.
- WebGL: **Succeeded**, 0 errors / 7 warnings,
  UTC `2026-10-11T15:16:06.4765725Z`; Chromium loading smoke **PASS**, UTC `2026-10-11T15:16:44.599665+00:00`.
- PICO: **Succeeded**, 0 errors / 14 warnings,
  UTC `2026-10-11T15:21:29.8200744Z`. APK manifest, package ID, version, ARM64/IL2CPP/OpenXR,
  PICO/controller metadata and signature: **PASS**.

## PICO APK

- Файл: `BiologyVR_Pico4_v0.4.0.apk`; версия **0.4.0**, Android versionCode **5**.
- Пакет: `com.biologyvr.arteryjourney`.
- Android SDK: min **29**, target **34**; ARM64 / IL2CPP / OpenGLES3 / PICO OpenXR.
- Release build (`BuildOptions.None`), Single Pass Instanced, render scale 0.82,
  MSAA 2, без HDR/post-processing/realtime shadows.
- Размер: **77,238,537 байт**.
- SHA-256: `eebbf0396a197c01399db0e69dff5dd641703de0b512ceab5f1062513ce2f596`.
- Локально: `Builds/Pico4/BiologyVR_Pico4_v0.4.0.apk`.

Левый Trigger — Scanner; правый — BioTool; правая A/B — режимы;
Grip — предметы/карточки; левая Y — Continue. В TONUS движение вверх расширяет,
вниз сужает участок. В CAPTURE тромбоцит прикрепляется автоматически у раны.

Unity-проект: `unity/BiologyVR`, Unity **6000.1.9f1**.
Стартовая сцена: `Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
ПК: F/E, C, ЛКМ/колесо, ПКМ для осмотра, → для Continue.

Проверки выполнены в Editor, браузере и по собранному APK. Физический PICO/Quest,
FPS, haptics и аппаратная потеря tracking отдельно не проверены.
