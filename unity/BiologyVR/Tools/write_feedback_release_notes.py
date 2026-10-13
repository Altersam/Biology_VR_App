"""Write release notes from current 0.7.0 build and feedback-rework proofs."""
import json
from pathlib import Path

root = Path(__file__).resolve().parent.parent
reports = root / 'Assets/BiologyVR/ArteryJourney/Reports'
settings = json.loads((root / 'Tools/release_settings.json').read_text(encoding='utf-8-sig'))
version = settings['version']

def read(name):
    return json.loads((reports / name).read_text(encoding='utf-8-sig'))

pc = read('Polish/PlayerFeedbackRework/PC_Input.json')
xr = read('Polish/PlayerFeedbackRework/XR_Input.json')
obstacles = read('Polish/PlayerFeedbackRework/Obstacles_PC.json')
web, browser = read('WebGLBuildReport.json'), read('WebGLBrowserSmoke.json')
pico, pico_apk = read('PicoBuildReport.json'), read('PicoApkVerification.json')
quest, quest_apk = read('MetaQuestBuildReport.json'), read('MetaQuestApkVerification.json')
if not all(item.get('passed') and not item.get('running') for item in (pc, xr, obstacles)):
    raise SystemExit('Current feedback-rework PC/XR/obstacle input proofs required')
if any(item.get('result') != 'Succeeded' for item in (web, pico, quest)):
    raise SystemExit('All current player builds must succeed')
if web.get('version') != version or not browser.get('passed') or browser['utc'] < web['utc']:
    raise SystemExit('Current WebGL build and browser smoke proof required')
if any(not apk.get('passed') or apk.get('version') != version for apk in (pico_apk, quest_apk)):
    raise SystemExit('Current verified headset artifacts required')
notes = f'''# Biology VR {version} — переработка по реальному прохождению

[Играть в браузере](https://altersam.github.io/Biology_VR_App/play/) ·
[PICO 4 APK](https://github.com/Altersam/Biology_VR_App/releases/download/v{version}/BiologyVR_Pico4_v{version}.apk) ·
[Meta Quest / Oculus APK](https://github.com/Altersam/Biology_VR_App/releases/download/v{version}/BiologyVR_MetaQuest_v{version}.apk)

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

- Feedback PC input: **PASS**, UTC `{pc['utc']}`.
- Plaque / impulse / all six emboli PC input: **PASS**, UTC `{obstacles['utc']}`.
- XR Trigger / field hand / six emboli / three launches: **PASS**, UTC `{xr['utc']}`.
- WebGL: **Succeeded**, {web['errors']} errors / {web['warnings']} warnings,
  UTC `{web['utc']}`; Chromium loading smoke **PASS**, UTC `{browser['utc']}`.
- PICO: **Succeeded**, {pico['errors']} errors / {pico['warnings']} warnings,
  UTC `{pico['utc']}`; manifest/runtime/signature **PASS**.
- Meta Quest: **Succeeded**, {quest['errors']} errors / {quest['warnings']} warnings,
  UTC `{quest['utc']}`; Meta devices/category/headtracking, no PICO runtime, signature **PASS**.

## APK

Оба APK: версия **{version}**, Android versionCode **{settings['android_version_code']}**,
`com.biologyvr.arteryjourney`, ARM64 / IL2CPP / OpenXR / OpenGLES3,
min SDK 29 / target 34, release build.

| Файл | Байты | SHA-256 |
|---|---:|---|
| BiologyVR_Pico4_v{version}.apk | {pico_apk['size_bytes']} | `{pico_apk['sha256']}` |
| BiologyVR_MetaQuest_v{version}.apk | {quest_apk['size_bytes']} | `{quest_apk['sha256']}` |

Левый Trigger — Scanner; правый — BioTool; правая A/B — режимы;
левая Y — Continue. На ПК F/E, C, колесо для поля/глубины, ПКМ для осмотра.

Unity 6000.1.9f1, проект `unity/BiologyVR`, master
`Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
Подробное описание: `PLAYER_FEEDBACK_REWORK_RU.md`, реальные Game-view кадры:
`Reports/Polish/PlayerFeedbackRework/`.

Проверки InputActions/XRI выполнены в Editor fixtures. Физические PICO/Quest,
FPS/thermal и читаемость на шлемах отдельно не измерялись. Исторические FULL SIMULATOR
и 120-orders отчёты 0.6.0 не подменяют текущие проверки этой версии.
'''
path = root / f"RELEASE_{version.replace('.', '_')}_RU.md"
path.write_text(notes, encoding='utf-8')
print(path)
