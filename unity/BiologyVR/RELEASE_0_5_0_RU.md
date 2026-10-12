# Biology VR 0.5.0 — свободное исследование состава крови

[Играть в браузере](https://altersam.github.io/Biology_VR_App/play/) ·
[Скачать PICO APK](https://github.com/Altersam/Biology_VR_App/releases/download/v0.5.0/BiologyVR_Pico4_v0.5.0.apk) ·
[Скачать Meta Quest / Oculus APK](https://github.com/Altersam/Biology_VR_App/releases/download/v0.5.0/BiologyVR_MetaQuest_v0.5.0.apk) ·
[GitHub Release](https://github.com/Altersam/Biology_VR_App/releases/tag/v0.5.0)

## Изменения

- Первый артериальный этап — свободная лаборатория, без порядка RBC → WBC → platelet → plasma.
- Восемь существующих объектов доступны одновременно: эритроцит, лейкоцит, тромбоцит,
  плазма, гемоглобин, гем/Fe²⁺, O₂ и CO₂. У каждого независимые статусы исследования.
- Захват/BioTool открывают контекстную карточку; Scanner завершает исследование.
  Предварительный grab не обязателен. Теория — короткие страницы, отдельные для каждого объекта.
- Профессор реагирует на первое действие; повторное чтение не даёт новый зачёт.
  Журнал и страницы сохраняются при переключениях и возврате. Continue доступен после 4/4,
  автоматического перехода нет. Кровь и пульсация продолжаются во время чтения.
- Молекулярные темы доступны до эритроцита; сохранены XRI grab, O₂↔гем и двуручное увеличение.
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
- Свободное исследование: **все 24 порядка PASS**, UTC `2026-10-11T19:44:44.5670239Z`;
  optional-темы до RBC, повторы, partial re-entry, кнопки страниц и нормальный Continue.
- ПК-гемодинамика: **PASS**, UTC `2026-10-11T14:53:06.284476Z`.
- Циркуляция с мобильным пулом 180: **PASS**, UTC `2026-10-11T14:58:53.0870741Z`.
- WebGL: **Succeeded**, 0 errors / 7 warnings,
  UTC `2026-10-11T20:44:10.2740826Z`; Chromium loading smoke **PASS**, UTC `2026-10-12T04:38:47.996695+00:00`.
- PICO: **Succeeded**, 0 errors / 14 warnings,
  UTC `2026-10-12T04:44:58.5754236Z`. APK manifest, package ID, version, ARM64/IL2CPP/OpenXR,
  PICO/controller metadata and signature: **PASS**.
- Meta Quest / Oculus: **Succeeded**, 0 errors / 12 warnings,
  UTC `2026-10-12T04:47:18.5532094Z`. Версия/пакет, ARM64/IL2CPP/OpenXR, Meta supported devices,
  VR category/headtracking, отсутствие PICO runtime и подпись: **PASS**.

## PICO APK

- Файл: `BiologyVR_Pico4_v0.5.0.apk`; версия **0.5.0**, Android versionCode **6**.
- Пакет: `com.biologyvr.arteryjourney`.
- Android SDK: min **29**, target **34**; ARM64 / IL2CPP / OpenGLES3 / PICO OpenXR.
- Release build (`BuildOptions.None`), Single Pass Instanced, render scale 0.82,
  MSAA 2, без HDR/post-processing/realtime shadows.
- Размер: **77,256,609 байт**.
- SHA-256: `0dede3a705c09620280706680ad0f7a5a024aa62396be97ec0a717e3001e98a9`.
- Локально: `Builds/Pico4/BiologyVR_Pico4_v0.5.0.apk`.

## Meta Quest / Oculus APK

- Файл: `BiologyVR_MetaQuest_v0.5.0.apk`; версия **0.5.0**, Android versionCode **6**.
- Устройства: Meta Quest **2 / Pro / 3 / 3S**; Oculus Touch / Touch Plus / Touch Pro.
- ARM64 / IL2CPP / OpenGLES3 / Meta OpenXR; min SDK **29**, target **34**.
- Release build (`BuildOptions.None`), тот же мобильный профиль рендера.
- Размер: **76,372,394 байт**.
- SHA-256: `faf3f11d6efa0a9a5f5fc69062681c03847b363aab955eec1e86cf524d11a959`.
- Локально: `Builds/MetaQuest/BiologyVR_MetaQuest_v0.5.0.apk`.
- Это отдельная сборка для Meta runtime; PICO-плагины исключены из неё при сборке.

Левый Trigger — Scanner; правый — BioTool; правая A/B — режимы;
Grip — предметы/карточки; левая Y — Continue. В TONUS движение вверх расширяет,
вниз сужает участок. В CAPTURE тромбоцит прикрепляется автоматически у раны.

Unity-проект: `unity/BiologyVR`, Unity **6000.1.9f1**.
Стартовая сцена: `Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
ПК: F/E, C, ЛКМ/колесо, ПКМ для осмотра, → для Continue.

Проверки выполнены в Editor, браузере и по собранному APK. Физический PICO/Quest,
FPS, haptics и аппаратная потеря tracking отдельно не проверены.
