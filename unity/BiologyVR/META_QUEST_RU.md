# Biology VR для Meta Quest

Профиль сборки: **Quest 2 / Quest Pro / Quest 3 / Quest 3S**,
ARM64, IL2CPP, OpenXR, OpenGLES3, Single Pass Instanced.
Включены Meta Quest Support и Touch / Touch Pro / Touch Plus profiles.
Pico runtime исключается из этой отдельной Android-сборки.

Файл: `Builds/MetaQuest/BiologyVR_MetaQuest.apk`.
Финальная сборка UTC **2026-10-08T14:47:18.5292796Z**, **89 505 864 bytes**.
Artifact verification PASS: Quest manifest, ARM64/IL2CPP/OpenXR, valid signature,
без Pico manifest/native libraries. Включён Inter с кириллицей для интерфейса.
SHA-256: `b4e944be72b4a42578ccb409965e2ddeafe3b9390103b391c2d03c22aa2460f3`.
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

В BuildReport оставлены 6 сообщений от MCP editor test-файла без .meta;
они не относятся к игровому player и не скрыты в отчёте. Сборка Succeeded,
отдельная проверка настоящего APK PASS.
