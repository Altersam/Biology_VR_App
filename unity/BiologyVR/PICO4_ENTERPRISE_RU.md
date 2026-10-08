# Biology VR — ПК и Pico 4 Enterprise

## Запуск в Unity на ПК

Открой `Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
Нажми Play, проверь, что Pause выключена, затем кликни в окно **Game**.
Настройки ниже относятся к этому master-маршруту Scenes03–16.

| Управление | Действие |
|---|---|
| Мышь без нажатой ПКМ | Указатель для объектов, Scanner и BioTool |
| ЛКМ удерживать над объектом | Захват и перенос через настоящий XRI interactor |
| Отпустить ЛКМ | Отпустить клетку в поток / разместить физический объект |
| Колесо при захвате | Ближе / дальше; один шаг — 0,15 м |
| G | Альтернативный захват/отпускание учебной клетки или O₂ |
| F удерживать | Scanner по указателю; раннее отпускание прерывает сканирование |
| E | BioTool по указателю; для заряда/поля удерживать |
| Z | Учебное увеличение текущей модели |
| ПКМ + движение мыши | Повернуть взгляд; во время осмотра прицел в центре |
| → | Продолжить завершённый этап маршрута |
| F1 / Tab | Панель / справка |

### Scene07: газовый эмбол

1. Наведи мышь на эмбол и удержи F для сканирования.
2. Наведи на выбор attraction mode и нажми E.
3. Наведи на эмбол и удерживай E до захвата.
4. **Не отпуская E**, перемести указатель к кольцу стабилизации.
5. Колесом подбери глубину. Подсказка показывает расстояние в метрах.
6. Удержи эмбол внутри кольца до завершения стабилизации.

Камеру для переноса поворачивать не обязательно. Отпускание E до стабилизации
возвращает эмбол в поток; можно повторить захват.

Исправлена ошибка единиц: Input System 1.15 по умолчанию выдаёт **1**, а не
120 на шаг колеса. Код учитывает normalized и platform-specific режимы.
Ранние тесты с искусственным raw-scroll 120 не подтверждали удобство реального
колеса; актуальная проверка использует значение 1 и целые шаги.

### Физические puzzles

Тромбоциты, fibrin endpoints и антитела берутся удержанием ЛКМ,
переносятся указателем с регулировкой глубины колесом и размещаются
отпусканием ЛКМ. Условия контакта/слота остаются геометрическими.
В Scene10: наведи на регулятор, удерживай E и меняй тонус колесом;
для успеха требуется устойчивое умеренное сужение.

## Подготовка APK

**Финальная сборка выполнена 2026-10-08**, UTC `09:51:45.7541152Z`.
`Succeeded`, 0 errors; Development APK — **113 067 505 bytes** (113 МБ).
Проверены manifest, ARM64/IL2CPP/OpenXR/Pico native libraries и APK v2 signature.
SHA-256:
`318e54082968b8482dad69f6b043f2c2c7f9b8c1bba54a58e4603f24528c64a5`.

Выход сборки: `Builds/Pico4Enterprise/BiologyVR_Pico4Enterprise.apk`.
Меню Unity: **Biology VR → Build Pico 4 Enterprise APK**.

Android Tools требуют ASCII-путь проекта. Для текущей папки с кириллицей создан
junction `C:\Users\alter\AppData\Local\Temp\opencode\BiologyVRAndroid`.
Это ссылка на исходный проект, а не отдельная копия. Для повторной сборки:

1. Выполни `Tools/create_ascii_build_alias.ps1`, если ссылка отсутствует.
2. Меню **Biology VR → Reopen At ASCII Build Alias** сохранит проект и откроет
   его через ASCII-путь.
3. Выполни **Build Pico 4 Enterprise APK**.
4. Для дальнейшей работы на ПК — **Return Editor To Desktop Target**, затем
   **Return To Original Project Path**.

APK доступен в исходной папке `Builds/Pico4Enterprise/` независимо от alias.
Проверить artifact: `python Tools/verify_pico_apk.py`.

На этом ПК установлены:
- Android Build Support для Unity **6000.1.9f1**;
- OpenJDK **17.0.9**;
- Android SDK, Build Tools **34.0.0**, Platform Tools **34.0.5**;
- Android NDK **r27c**, CMake **3.22.1**;
- официальный **PICO OpenXR Plugin 1.4.1** (Git release_1.4.0).

Профиль: ARM64 / IL2CPP / OpenGLES3 / OpenXR Single Pass Instanced,
min SDK 29, target SDK 34, Activity entry point, контроллеры PICO4.
В Android включены PICO Support, PICO4 Touch Controller Profile и требуемый
установленным пакетом Composition Layers Support.
Приложение: `com.biologyvr.arteryjourney`, версия 1.0.1, versionCode 2.
Первая сборка — Development APK для проверки на шлеме.
PICO Store appID для локальной установки не требуется; startup entitlement
check выключен. Регистрация приложения в магазине не нужна для этого APK.

## Что сделать на Pico

1. Обнови **Pico 4 Enterprise** через официальные настройки устройства.
   Для выбранной версии SDK производитель указывает систему **5.13.0 или новее**:
   https://github.com/Pico-Developer/PICO-Unity-OpenXR-SDK/tree/release_1.4.0
2. Включи режим разработчика и **USB debugging**. Путь пунктов зависит от
   версии Enterprise-прошивки; обычно Developer options появляется после
   нескольких нажатий на Software version в About.
3. Подключи шлем к ПК **USB-кабелем с передачей данных**.
4. Надень шлем и подтверди запрос разрешения USB debugging для этого ПК.
5. Выполни из папки Unity-проекта:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\Tools\install_pico_apk.ps1"
```

Скрипт покажет ADB devices, установит APK с `install -r` и запустит приложение.
Если подключено несколько Android-устройств, добавь `-Serial "серийный_номер"`.

ADB уже установлен здесь:

`C:\Program Files\Unity\Hub\Editor\6000.1.9f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`

Если список devices пуст: проверь кабель, USB debugging и разрешение в шлеме.
Для Windows USB-драйвера/графической установки можно использовать официальный
PICO Developer Center: https://developer.picoxr.com/resources/
Если Developer options/установка приложений скрыты на управляемом Enterprise-
устройстве, их должен разрешить администратор этого устройства.

## Управление двумя контроллерами Pico

| Контроллер | Действие |
|---|---|
| Боковой Grip | Захват/удержание клетки, тромбоцита, нити или антитела |
| Оба Grip на модели + развести руки | Учебное увеличение |
| Левый Trigger удерживать | Scanner: направить на цель и удерживать |
| Правый Trigger | BioTool; для charge/attraction удерживать |
| Правая B | Продолжить после завершения этапа |
| Луч контроллера + Trigger по UI | Кнопки панели и справка |

Эмбол в VR переносится удерживаемым полем: продолжай удерживать правый Trigger
и перемещай руку, помести эмбол в стабилизационное кольцо и удержи до успеха.
На старте поднеси руку/луч к эритроциту и возьми Grip.

## Отчёты проверки

- `Assets/BiologyVR/ArteryJourney/Reports/PicoConfiguration.json` — настройки XR/Android.
- `Assets/BiologyVR/ArteryJourney/Reports/PicoBuildReport.json` — результат сборки.
- `Assets/BiologyVR/ArteryJourney/Reports/PicoApkVerification.json` — проверка
  manifest, ARM64 библиотек и подписи готового APK.
- `Assets/BiologyVR/ArteryJourney/Reports/Polish/FlowPickup/NativeCursor_PC_Input.json`
  — реальные mouse/keyboard events, normalized wheel step, захват, перенос,
  отпускание, эмбол, physical platelet placement, настройка тонуса и безопасный
  вход в recovery с неактивными fibrin strands; последний PASS UTC `09:44:24`.
- `Assets/BiologyVR/ArteryJourney/Reports/Polish/VR_INPUT_PLAYTHROUGH_V7.md`
  — sequential simulated-controller playthrough.

Pico ещё не подключался к ПК: проверка запуска, изображения обоих глаз,
контроллеров, FPS и комфорта непосредственно на устройстве предстоит после установки.
