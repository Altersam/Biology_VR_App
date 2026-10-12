import json
from pathlib import Path

root = Path(__file__).resolve().parent.parent
reports = root / 'Assets/BiologyVR/ArteryJourney/Reports'
release = json.loads((root / 'Tools/release_settings.json').read_text(encoding='utf-8-sig'))
version = release['version']
web = json.loads((reports / 'WebGLBuildReport.json').read_text(encoding='utf-8-sig'))
browser = json.loads((reports / 'WebGLBrowserSmoke.json').read_text(encoding='utf-8-sig'))
pico = json.loads((reports / 'PicoBuildReport.json').read_text(encoding='utf-8-sig'))
apk = json.loads((reports / 'PicoApkVerification.json').read_text(encoding='utf-8-sig'))
quest = json.loads((reports / 'MetaQuestBuildReport.json').read_text(encoding='utf-8-sig'))
quest_apk = json.loads((reports / 'MetaQuestApkVerification.json').read_text(encoding='utf-8-sig'))
pc = json.loads((reports / 'Polish/FlowPickup/Hemodynamics_PC_Input.json').read_text(encoding='utf-8-sig'))
research = json.loads((reports / 'Polish/FlowPickup/Connected_Research_Orders_PC.json').read_text(encoding='utf-8-sig'))
organism = json.loads((reports / 'Polish/FlowPickup/Connected_Organism_PC.json').read_text(encoding='utf-8-sig'))
mobile = json.loads((reports / 'Polish/FlowPickup/Mobile_All_Scenes_Persistent_Blood.json').read_text(encoding='utf-8-sig'))
xr = (reports / 'Polish/VR_INPUT_PLAYTHROUGH_V7.md').read_text(encoding='utf-8-sig')
if web.get('version') != version or web['result'] != 'Succeeded' or not browser['passed'] or browser['utc'] < web['utc'] or pico['result'] != 'Succeeded' or apk.get('version') != version or not apk['passed'] or quest['result'] != 'Succeeded' or quest_apk.get('version') != version or not quest_apk['passed'] or not pc['passed'] or not research['passed'] or not organism['passed'] or not any('All 120 five-population orders' in line for line in research['checks']) or not mobile['passed'] or 'Status: FULL SIMULATOR PASS' not in xr or 'First XR grip promotes a real background instance' not in xr or 'Final immune population uses the same phagocyte mechanism' not in xr:
    raise SystemExit('Current build and gameplay proofs required')
name = f'BiologyVR_Pico4_v{version}.apk'
quest_name = f'BiologyVR_MetaQuest_v{version}.apk'
body = f'''# Biology VR {version} — живой кровоток и врождённая защита

[Играть в браузере](https://altersam.github.io/Biology_VR_App/play/) ·
[Скачать PICO APK](https://github.com/Altersam/Biology_VR_App/releases/download/v{version}/{name}) ·
[Скачать Meta Quest / Oculus APK](https://github.com/Altersam/Biology_VR_App/releases/download/v{version}/{quest_name}) ·
[GitHub Release](https://github.com/Altersam/Biology_VR_App/releases/tag/v{version})

## Изменения

- Первый артериальный этап — пять независимых популяций: эритроцит, лейкоцит,
  фагоцит, тромбоцит и плазма. Фагоцит — функциональный архетип лейкоцита.
- Игрок выбирает конкретный экземпляр общего кровотока. Фоновые 360 слотов
  остаются без массовой физики, интерактивных представителей максимум семь.
- Захват/BioTool открывают контекстную карточку; Scanner завершает исследование.
  Предварительный grab не обязателен. Теория — короткие страницы, отдельные для каждого объекта.
- Профессор реагирует на первое действие; повторное чтение не даёт новый зачёт.
  Журнал и страницы сохраняются при переключениях и возврате. Continue доступен после 5/5,
  автоматического перехода нет. Кровь и пульсация продолжаются во время чтения.
- Hb → гем/Fe²⁺ → O₂ раскрываются в локальном анализе выбранного RBC;
  CO₂ изучается в контексте RBC/плазмы и преимущественного переноса HCO₃⁻.
  Крупные самостоятельные Hb/O₂/CO₂ отсутствуют в обычном потоке.
- Первый вирус: одна частица и один цикл обучения. Scanner инициирует реакцию
  в модели; CAPTURE удерживает фагоцит, отвод руки заряжает поле, отпускание
  запускает саму клетку. Preview учитывает дрейф крови; промах допускает повтор.
- Контакт приводит к постепенному окружению и поглощению ~1.8 с. Ёмкость клетки
  ограничена тремя успешными поглощениями. Поздние Scenes09/15 используют тот же
  цикл внутри обычной крови; в Scene09 завершение требует снятой Viral Load.
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
- Свободное исследование: **все 120 порядков пяти типов PASS**, UTC `{research['utc']}`;
  точный независимый прогресс, повторы, partial re-entry и нормальный Continue.
- Связанный ПК fixture: **PASS**, UTC `{organism['utc']}`: вложенные темы,
  первый вирус, промах/повтор, поглощение, поздняя волна и сохранение tutorial.
- ПК-гемодинамика: **PASS**, UTC `{pc['utc']}`.
- Циркуляция с мобильным пулом 180: **PASS**, UTC `{mobile['utc']}`.
- WebGL: **Succeeded**, {web['errors']} errors / {web['warnings']} warnings,
  UTC `{web['utc']}`; Chromium loading smoke **PASS**, UTC `{browser['utc']}`.
- PICO: **Succeeded**, {pico['errors']} errors / {pico['warnings']} warnings,
  UTC `{pico['utc']}`. APK manifest, package ID, version, ARM64/IL2CPP/OpenXR,
  PICO/controller metadata and signature: **PASS**.
- Meta Quest / Oculus: **Succeeded**, {quest['errors']} errors / {quest['warnings']} warnings,
  UTC `{quest['utc']}`. Версия/пакет, ARM64/IL2CPP/OpenXR, Meta supported devices,
  VR category/headtracking, отсутствие PICO runtime и подпись: **PASS**.

## PICO APK

- Файл: `{name}`; версия **{version}**, Android versionCode **{release['android_version_code']}**.
- Пакет: `com.biologyvr.arteryjourney`.
- Android SDK: min **29**, target **34**; ARM64 / IL2CPP / OpenGLES3 / PICO OpenXR.
- Release build (`BuildOptions.None`), Single Pass Instanced, render scale 0.82,
  MSAA 2, без HDR/post-processing/realtime shadows.
- Размер: **{apk['size_bytes']:,} байт**.
- SHA-256: `{apk['sha256']}`.
- Локально: `Builds/Pico4/{name}`.

## Meta Quest / Oculus APK

- Файл: `{quest_name}`; версия **{version}**, Android versionCode **{release['android_version_code']}**.
- Устройства: Meta Quest **2 / Pro / 3 / 3S**; Oculus Touch / Touch Plus / Touch Pro.
- ARM64 / IL2CPP / OpenGLES3 / Meta OpenXR; min SDK **29**, target **34**.
- Release build (`BuildOptions.None`), тот же мобильный профиль рендера.
- Размер: **{quest_apk['size_bytes']:,} байт**.
- SHA-256: `{quest_apk['sha256']}`.
- Локально: `Builds/MetaQuest/{quest_name}`.
- Это отдельная сборка для Meta runtime; PICO-плагины исключены из неё при сборке.

Левый Trigger — Scanner; правый — BioTool; правая A/B — режимы;
Grip — предметы/карточки; левая Y — Continue. В TONUS движение вверх расширяет,
вниз сужает участок. В CAPTURE тромбоцит прикрепляется автоматически у раны.
Для фагоцита: удерживать CAPTURE → отвести руку → навести → отпустить Trigger.
На ПК натяжение задаётся колесом при удерживаемом E.

Unity-проект: `unity/BiologyVR`, Unity **6000.1.9f1**.
Стартовая сцена: `Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
ПК: F/E, C, ЛКМ/колесо, ПКМ для осмотра, → для Continue.

Проверки выполнены в Editor, браузере и по собранному APK. Физический PICO/Quest,
FPS, haptics и аппаратная потеря tracking отдельно не проверены.
'''
notes = root / f"RELEASE_{version.replace('.', '_')}_RU.md"
notes.write_text(body, encoding='utf-8')
print(notes)
