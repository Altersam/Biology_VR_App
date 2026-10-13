# Biology VR 0.7.0 — переработка по реальному прохождению

[Играть в браузере](https://altersam.github.io/Biology_VR_App/play/) ·
[PICO 4 APK](https://github.com/Altersam/Biology_VR_App/releases/download/v0.7.0/BiologyVR_Pico4_v0.7.0.apk) ·
[Meta Quest / Oculus APK](https://github.com/Altersam/Biology_VR_App/releases/download/v0.7.0/BiologyVR_MetaQuest_v0.7.0.apk)

## Изменения

- Клетки захватываются BioTool в ПРИТЯЖЕНИИ через правый Trigger, а не Grip.
  Отпускание возвращает конкретный экземпляр в движущийся фон.
- В потоке есть пустые RBC, RBC с O₂, RBC с CO₂ и учебный Hb со связанным гемом/Fe²⁺.
  Автоматическое раскрытие всей молекулярной схемы при захвате отключено.
- Пять ручных режимов: ИМПУЛЬС, ПРИТЯЖЕНИЕ, СОСУДИСТОЕ ПОЛЕ, АКТИВАЦИЯ, РЕВЕРС.
  Импульс стреляет вне боевых целей и может разрушать образцы потока.
- Сосудистый эпизод: Scanner → расширение полем → критическое изменение →
  безопасный диаметр → повторная проверка. Точки/карточки/отдельный AR HUD убраны;
  динамические значения и формулы находятся на двух основных экранах.
- Бляшка повышает давление до участка, замедляет кровь и создаёт скопление.
  Density refill не переносит поток через препятствие. Улучшены след импульса и отдача.
- Эмболы получают случайный дрейф. Захват только курком в притяжении;
  потеря наведения приостанавливает силу и устойчивость в ловушке.
- Фагоцит: захват → Scanner → АКТИВАЦИЯ → РЕВЕРС → навести → отпустить.
  Первый медленный вирус требует трёх контактов свежих клеток; фагоциты встречаются чаще.
- Панели обращены к центру, текст Bold/outline и более контрастный фон,
  Canvas raster 5. Монитор динамический; помощь под правым экраном после 60 с без прогресса.
- Мобильный профиль: render scale 1.0 / MSAA 4, без HDR/post-processing/realtime shadows,
  180 видимых фоновых клеток и ограниченные интерактивные proxies.

## Проверки

- Feedback PC input: **PASS**, UTC `2026-10-13T12:00:54.106705Z`.
- Plaque / impulse / all six emboli PC input: **PASS**, UTC `2026-10-13T13:08:54.1266506Z`.
- XR Trigger / field hand / six emboli / three launches: **PASS**, UTC `2026-10-13T13:11:23.5159652Z`.
- WebGL: **Succeeded**, 0 errors / 8 warnings,
  UTC `2026-10-13T17:19:55.8699179Z`; Chromium loading smoke **PASS**, UTC `2026-10-13T17:21:33.672215+00:00`.
- PICO: **Succeeded**, 0 errors / 12 warnings,
  UTC `2026-10-13T14:37:00.0163091Z`; manifest/runtime/signature **PASS**.
- Meta Quest: **Succeeded**, 0 errors / 12 warnings,
  UTC `2026-10-13T17:08:29.9205505Z`; Meta devices/category/headtracking, no PICO runtime, signature **PASS**.

## APK

Оба APK: версия **0.7.0**, Android versionCode **8**,
`com.biologyvr.arteryjourney`, ARM64 / IL2CPP / OpenXR / OpenGLES3,
min SDK 29 / target 34, release build.

| Файл | Байты | SHA-256 |
|---|---:|---|
| BiologyVR_Pico4_v0.7.0.apk | 77591775 | `c16fa012a2dccac3f1ff1bd22fc63e05ac7580d7204c5c7ee084e25ed6609f8b` |
| BiologyVR_MetaQuest_v0.7.0.apk | 76707544 | `7c08bcdaf3e97156203af654f7a4111a1dd5111f282cb8a006a2962b571b0cd4` |

Левый Trigger — Scanner; правый — BioTool; правая A/B — режимы;
левая Y — Continue. На ПК F/E, C, колесо для поля/глубины, ПКМ для осмотра.

Unity 6000.1.9f1, проект `unity/BiologyVR`, master
`Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
Подробное описание: `PLAYER_FEEDBACK_REWORK_RU.md`, реальные Game-view кадры:
`Reports/Polish/PlayerFeedbackRework/`.

Проверки InputActions/XRI выполнены в Editor fixtures. Физические PICO/Quest,
FPS/thermal и читаемость на шлемах отдельно не измерялись. Исторические FULL SIMULATOR
и 120-orders отчёты 0.6.0 не подменяют текущие проверки этой версии.
