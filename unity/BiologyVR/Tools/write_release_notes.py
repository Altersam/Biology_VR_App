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
pc = json.loads((reports / 'Polish/FlowPickup/Hemodynamics_PC_Input.json').read_text(encoding='utf-8-sig'))
mobile = json.loads((reports / 'Polish/FlowPickup/Mobile_All_Scenes_Persistent_Blood.json').read_text(encoding='utf-8-sig'))
xr = (reports / 'Polish/VR_INPUT_PLAYTHROUGH_V7.md').read_text(encoding='utf-8-sig')
if web.get('version') != version or web['result'] != 'Succeeded' or not browser['passed'] or pico['result'] != 'Succeeded' or apk.get('version') != version or not apk['passed'] or not pc['passed'] or not mobile['passed'] or 'Status: FULL SIMULATOR PASS' not in xr or 'All three early experiments complete' not in xr:
    raise SystemExit('Current build and gameplay proofs required')
name = f'BiologyVR_Pico4_v{version}.apk'
body = f'''# Biology VR {version} — гемодинамический эксперимент

[Играть в браузере](https://altersam.github.io/Biology_VR_App/play/) ·
[Скачать PICO APK](https://github.com/Altersam/Biology_VR_App/releases/download/v{version}/{name}) ·
[GitHub Release](https://github.com/Altersam/Biology_VR_App/releases/tag/v{version})

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
- ПК-гемодинамика: **PASS**, UTC `{pc['utc']}`.
- Циркуляция с мобильным пулом 180: **PASS**, UTC `{mobile['utc']}`.
- WebGL: **Succeeded**, {web['errors']} errors / {web['warnings']} warnings,
  UTC `{web['utc']}`; Chromium loading smoke **PASS**, UTC `{browser['utc']}`.
- PICO: **Succeeded**, {pico['errors']} errors / {pico['warnings']} warnings,
  UTC `{pico['utc']}`. APK manifest, package ID, version, ARM64/IL2CPP/OpenXR,
  PICO/controller metadata and signature: **PASS**.

## PICO APK

- Файл: `{name}`; версия **{version}**, Android versionCode **{release['android_version_code']}**.
- Пакет: `com.biologyvr.arteryjourney`.
- Android SDK: min **29**, target **34**; ARM64 / IL2CPP / OpenGLES3 / PICO OpenXR.
- Release build (`BuildOptions.None`), Single Pass Instanced, render scale 0.82,
  MSAA 2, без HDR/post-processing/realtime shadows.
- Размер: **{apk['size_bytes']:,} байт**.
- SHA-256: `{apk['sha256']}`.
- Локально: `Builds/Pico4/{name}`.

Левый Trigger — Scanner; правый — BioTool; правая A/B — режимы;
Grip — предметы/карточки; левая Y — Continue. В TONUS движение вверх расширяет,
вниз сужает участок. В CAPTURE тромбоцит прикрепляется автоматически у раны.

Unity-проект: `unity/BiologyVR`, Unity **6000.1.9f1**.
Стартовая сцена: `Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
ПК: F/E, C, ЛКМ/колесо, ПКМ для осмотра, → для Continue.

Проверки выполнены в Editor, браузере и по собранному APK. Физический PICO/Quest,
FPS, haptics и аппаратная потеря tracking отдельно не проверены.
'''
(root / 'RELEASE_0_4_0_RU.md').write_text(body, encoding='utf-8')
print(root / 'RELEASE_0_4_0_RU.md')
