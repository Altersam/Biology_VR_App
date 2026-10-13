# PICO 4 — готовая сборка 0.7.0

APK: `Builds/Pico4/BiologyVR_Pico4_v0.7.0.apk`.
Версия **0.7.0**, Android versionCode **8**.
Пакет: `com.biologyvr.arteryjourney`; ARM64 / IL2CPP / PICO OpenXR / OpenGLES3.
SDK min 29 / target 34; release build (`BuildOptions.None`).

Сборка: **Succeeded**, UTC `2026-10-13T14:37:00.0163091Z`,
0 ошибок / 12 предупреждений. Размер **77 591 775 байт**.

SHA-256:
`c16fa012a2dccac3f1ff1bd22fc63e05ac7580d7204c5c7ee084e25ed6609f8b`

Manifest/версия/ARM64/Unity/IL2CPP/OpenXR/PICO-контроллеры/подпись: **PASS**.
Проверки APK: `Assets/BiologyVR/ArteryJourney/Reports/PicoApkVerification.json`.
Артефакт: `PICO4_APK_0_7_0.json`.

## Управление

- Левый Trigger — Scanner.
- Правый Trigger — действие BioTool.
- Правая A/B — следующий/предыдущий режим.
- Пять режимов: ИМПУЛЬС, ПРИТЯЖЕНИЕ, СОСУДИСТОЕ ПОЛЕ, АКТИВАЦИЯ, РЕВЕРС.
- Клетки крови: ПРИТЯЖЕНИЕ + удержание правого Trigger; отпускание возвращает в поток.
- Фагоцит: захват → Scanner → АКТИВАЦИЯ → РЕВЕРС → навести → отпустить Trigger.
- Сосудистое поле: удерживать Trigger и поднимать/опускать руку.
- Эмбол: удерживать Trigger и не терять наведение; перенести в ловушку.
- Левая Y — продолжить завершённый этап.

Переработка и проверки: `PLAYER_FEEDBACK_REWORK_RU.md`.
Экраны ближе и обращены к центру, Bold/outline, растеризация 5;
мобильный рендер scale 1.0, MSAA 4, без realtime shadows/HDR/post-processing,
180 видимых фоновых клеток. Аппаратные FPS/thermal/читаемость на PICO отдельно
не измерялись. APK подготовлен локально на Z:; старые GitHub APK 0.6.0 относятся
к предыдущей механике.
