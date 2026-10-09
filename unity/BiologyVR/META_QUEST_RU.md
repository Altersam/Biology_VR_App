# Biology VR для Meta Quest

Профиль сборки: **Quest 2 / Quest Pro / Quest 3 / Quest 3S**,
ARM64, IL2CPP, OpenXR, OpenGLES3, Single Pass Instanced.
Включены Meta Quest Support и Touch / Touch Pro / Touch Plus profiles.
Pico runtime исключается из этой отдельной Android-сборки.

Файл: `Builds/MetaQuest/BiologyVR_MetaQuest.apk`.
Актуальная сборка Coral V6 **0.2.0**, versionCode **3**, UTC
**2026-10-09T05:47:50.1622173Z**, **88 034 621 bytes**.
Artifact verification PASS: Quest manifest, ARM64/IL2CPP/OpenXR, valid signature,
без Pico manifest/native libraries. Включён Inter с кириллицей для интерфейса.
SHA-256: `4d884fa7a2c7e630fe4548cf3ba25755d4c49ac639a7a21e1e2ff9f0cc0de5c8`.
Повторная сборка: меню **Biology VR → Build Meta Quest APK**.
Для Android используйте ASCII-путь проекта (локальный junction или clone
в C:\Projects\Biology_VR\unity\BiologyVR).

## Установка

1. Включите Developer Mode для Quest через Meta Horizon/настройки разработчика.
2. Подключите Quest USB-кабелем с передачей данных.
3. В шлеме подтвердите разрешение USB debugging для этого компьютера.
4. Из папки проекта выполните:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\Tools\install_quest_apk.ps1"
```

Приложение устанавливается локально; после установки доступно в разделе
**Unknown Sources / Неизвестные источники** библиотеки Quest.
Графическая альтернатива — установка этого же APK через SideQuest.

## Контроллеры

- Grip — взять и удерживать объект; отпускание размещает его.
- Оба Grip на модели, развести руки — учебное увеличение.
- Левый Trigger удерживать — Scanner.
- Правый Trigger — BioTool; удерживать для поля/заряда.
- Правая B — продолжить завершённый этап.
- Луч + Trigger по кнопке панели — UI/справка.

Эмбол переносится правой рукой при удерживаемом Trigger; удержите его внутри
кольца стабилизации. Сканирование/контакты/placement сохраняют игровые условия.

Подпись и manifest проверяются `python Tools/verify_quest_apk.py`.
Отчёты: `Assets/BiologyVR/ArteryJourney/Reports/MetaQuest*.json`.
Физическая проверка изображения, контроллеров и производительности Quest
требует подключённого устройства; standalone APK-проверка её не заменяет.

Актуальный BuildReport: Succeeded, **0 errors**, 10 warnings.
Отдельная проверка настоящего APK PASS; это не физический headset test.
