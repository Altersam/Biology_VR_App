# PICO 4 — APK 0.3.0

Файл: `Builds/Pico4/BiologyVR_Pico4_v0.3.0.apk` в рабочем `bio_v0.2` на Z:.
Исходный выход builder: `Builds/Pico4Enterprise/BiologyVR_Pico4Enterprise.apk`.
Это один и тот же проверенный APK; понятное имя скопировано после SHA-256 сверки.

Версия **0.3.0**, versionCode **4**, application ID `com.biologyvr.arteryjourney`.
ARM64 / IL2CPP / OpenXR / PICO4ControllerProfile / OpenGLES3 / SinglePassInstanced.
Минимальный Android API 29, target API 34. Обычная сборка без Development/AllowDebugging.

Мобильный фон ограничен 180 визуальными клетками; глобальные/ветвевые и локальные
слоты чередуются, поэтому меньший бюджет сохраняет все направления потока.
URP: MSAA 2, renderScale 0.82, без HDR/post effects/realtime shadows.

Проверено: APK readable manifest, версия, ARM64, Unity/IL2CPP/OpenXR библиотеки,
PICO VR/SDK/controller metadata и подпись.
Отчёт: `Assets/BiologyVR/ArteryJourney/Reports/PicoApkVerification.json`.
Размер, SHA-256 и сводка: `PICO4_APK_0_3_0.json`.

Управление: правая A — следующий режим BioTool, B — предыдущий;
левая Y — продолжить завершённый этап; левый Trigger — Scanner;
правый Trigger — действие/удерживаемый захват; Grip — ручные взаимодействия.

Установка с компьютера: `adb install -r "Builds/Pico4/BiologyVR_Pico4_v0.3.0.apk"`.
Физический запуск/FPS/haptics на шлеме не проверены этой сборкой.
Существующий GitHub release v0.2.0 остаётся прежним; новый APK создан локально.
