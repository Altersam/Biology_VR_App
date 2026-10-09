# Biology VR — Pico 4 Enterprise, 0.2.0 / Coral V6

Актуальный APK: `Builds/Pico4Enterprise/BiologyVR_Pico4Enterprise.apk`.
Build UTC **2026-10-09T05:51:29.5189366Z**, Succeeded, **0 errors**,
**102 963 647 bytes**. Version **0.2.0**, versionCode **3**.
SHA-256: `8cfbb22c2b83fe1ed7ce9e4bbffeaba159b75121d781421530c32cf49db5c6f4`.
Manifest, ARM64/IL2CPP/OpenXR/Pico libraries и APK signature: verification PASS.

## Установка

1. Обновить Pico 4 Enterprise; выбранный PICO OpenXR SDK указывает OS 5.13.0+.
2. Включить Developer options и USB debugging.
3. Подключить USB-кабель с передачей данных, подтвердить отладку в шлеме.
4. Из папки Unity-проекта выполнить:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\Tools\install_pico_apk.ps1"
```

Для управляемого Enterprise-устройства разрешение отладки/установки приложений
может задаваться администратором устройства. Графическая установка и драйверы:
PICO Developer Center https://developer.picoxr.com/resources/.

## Контроллеры

- Grip — взять/удерживать; отпускание размещает объект.
- Оба Grip на модели + развести руки — учебное увеличение.
- Левый Trigger удерживать — Scanner.
- Правый Trigger — BioTool; удерживать для заряда/поля и переноса эмбола.
- Правая B — продолжить завершённый этап.

## Повторная сборка

Unity 6000.1.9f1, Android Build Support/SDK/NDK/OpenJDK.
Профиль ARM64/IL2CPP/OpenGLES3/Single Pass Instanced, minSdk 29/targetSdk 34.
PICO Support/PICO4 controller profile, Composition Layers Support.
Menu **Biology VR → Build Pico 4 Enterprise APK**.
Android ASCII-путь на этом ПК: **Z:\BiologyVR_bio_v02_build**;
создать `Tools/create_ascii_build_alias.ps1`, затем **Reopen At ASCII Build Alias**.
Для обычного clone удобен ASCII-путь C:\Projects\Biology_VR_App\unity\BiologyVR.

PICO Store appID для локального sideload не используется; startup entitlement
check выключен. Hardware запуск/FPS/GPU/комфорт пока не измерены.
Подробные отчёты: Assets/BiologyVR/ArteryJourney/Reports/Pico*.json.

## ПК-управление этой версии

Мышь — указатель; ЛКМ удерживать — перенос; колесо — глубина (0,15 м/tick);
F удерживать — Scanner; E — BioTool/поле; ПКМ — осмотр; G — альтернативный захват;
Z — увеличение; → — продолжить. Scene07 переносится указателем + колесом при
удержании E. VR-перенос использует положение руки и правый Trigger.
