"""Prepare learning/flow/controller release notes from verified reports, then sync with update_app_repository.py."""
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parent.parent
repo = root.parent / 'Github_VR' / 'Biology_VR_App'
reports = root / 'Assets/BiologyVR/ArteryJourney/Reports'
settings = json.loads((root / 'Tools/release_settings.json').read_text(encoding='utf-8-sig'))
version = settings['version']
def read(name):
    return json.loads((reports / name).read_text(encoding='utf-8-sig'))
flow = read('Polish/FlowLearningConsistency/Continuity.json')
pc = read('Polish/ImmunePatrol/PC_Input.json')
prelude = read('Polish/PatrolWoundIntegration/XR_Prelude.json')
xr = read('Polish/PatrolWoundIntegration/XR_Input.json')
pico, quest = read('PicoApkVerification.json'), read('MetaQuestApkVerification.json')
web, browser = read('WebGLBuildReport.json'), read('WebGLBrowserSmoke.json')
if not all(value.get('passed') and not value.get('running') for value in (flow, pc, prelude, xr, pico, quest, browser)):
    raise SystemExit('Current successful flow/PC/XR/APK/browser verification required')
if pico['version'] != version or quest['version'] != version or web.get('version') != version or web['result'] != 'Succeeded' or browser['utc'] < web['utc']:
    raise SystemExit('Current builds and browser proof required')
base = f'https://github.com/Altersam/Biology_VR_App/releases/download/v{version}'
pico_url = base + f'/BiologyVR_Pico4_v{version}.apk'
quest_url = base + f'/BiologyVR_MetaQuest_v{version}.apk'
notes = f'''# Biology VR {version} — учебный экран, непрерывный поток и точное прицеливание

## Исправления

- Семь смещённых моделей частиц центрированы относительно оси вращения; вращение ограничено и сглажено. Материалы и общий пул переиспользованы.
- Вместо ромба/октаэдра — округлая учебная модель гема с центральным Fe²⁺. Гем закреплён на гемоглобине и не меняет положение вслед за взглядом.
- Возврат клеток в общий пул больше не блокируется при взгляде против потока. На дальних краях циркуляции клетки плавно появляются/исчезают; рядом с игроком сохраняют движение. Мобильный лимит — 180 видимых слотов.
- Замедление перед бляшкой сохранено, чрезмерное опустошение потока после неё устранено.
- Левый экран отвечает за учебную информацию, задача закреплена отдельным блоком. Положение экранов едино на всех артериальных этапах.
- Успешный Scanner автоматически открывает полную прокручиваемую теорию. Она остаётся после отпускания образца; эритроцит связан с Hb, гемом/железом, O₂ и CO₂. Для остальных эпизодов добавлены объяснения наблюдаемого механизма.
- Кнопка «Вся теория»/«Кратко», «К действию», колесо и правый стик над экраном управляют чтением. Получение информации не подменяет выполнение задания.
- Лучи прицеливания независимы для двух контроллеров, направлены по их осям и заканчиваются на пересечении луча с поверхностью. Привязка к центру прежней цели убрана.
- Несколько сходящихся линий захвата заменены одной тонкой связью только с удерживаемым объектом. Старые XRI-линии отключены при наличии собственных направляющих; XRI-ввод и UI сохранены.
- Сохранены иммунный патруль, видимая область раны, 11 тромбоцитов, баланс гемостаза и финал из 0.9.0.

## Проверки

- Кровоток и стенка на станциях 03–16, в том числе взгляд вперёд/назад и переходы, 180 клеток: PASS (`{flow['utc']}`). Это постановочные переходы, не прохождение заданий.
- PC Scanner, полная связанная теория и реальная прокрутка колесом; патруль и тромбоциты: PASS (`{pc['utc']}`).
- XR 03–08, сохранение полной теории, связь гема с Hb при повороте головы, коллинеарность двух лучей и очистка захвата: PASS (`{prelude['utc']}`).
- XR 09–18 до итогового отчёта: PASS (`{xr['utc']}`).
- Подписи, манифесты и ARM64/IL2CPP/OpenXR обеих APK: PASS. WebGL и загрузка в Chromium: PASS.
- XR-проверки используют симулированные устройства со штатными action bindings. Новая версия ещё не проверена на физических PICO/Quest; частота кадров и комфорт на шлемах не измерены.

## Скачать

- [PICO 4 / Enterprise {version}]({pico_url}) — {pico['size_bytes']} байт.
  SHA-256: `{pico['sha256']}`.
- [Meta Quest / Oculus 2 / Pro / 3 / 3S {version}]({quest_url}) — {quest['size_bytes']} байт.
  SHA-256: `{quest['sha256']}`.
- [WebGL](https://altersam.github.io/Biology_VR_App/play/).

Версия {version}, Android versionCode {settings['android_version_code']}. Unity 6000.1.9f1, `com.biologyvr.arteryjourney`.
Отчёты: `Assets/BiologyVR/ArteryJourney/Reports/Polish/FlowLearningConsistency/` и `PatrolWoundIntegration/`.
'''
name = f"RELEASE_{version.replace('.', '_')}_RU.md"
(root / name).write_text(notes, encoding='utf-8')
(repo / 'README.md').write_text(f'''# Biology VR App — {version}

Учебная VR-игра внутри единой артерии. Рабочий проект — **bio_0.3**, Unity **6000.1.9f1**.

[Играть в браузере](https://altersam.github.io/Biology_VR_App/play/) · [PICO APK]({pico_url}) · [Quest / Oculus APK]({quest_url}) · [Релиз](https://github.com/Altersam/Biology_VR_App/releases/tag/v{version})

## В этой версии

Непрерывный кровоток на всех артериальных этапах, центрированное плавное вращение частиц, гем с Fe²⁺ вместо ромба, единое положение экранов и исправленные лучи контроллеров.

Scanner открывает полную учебную карточку на левом экране. Для эритроцита доступны строение, функции и связи с гемоглобином, гемом, кислородом и углекислым газом. Теория сохраняется после отпускания клетки; задача остаётся видимой отдельно.

Сохранены свободное исследование крови, слои артерии, гемодинамика, бляшка, эмболы, иммунный патруль, разрыв/пробка/баланс/репарация и финал.

## Управление

**VR:** левый курок — Scanner, правый — BioTool; A/B — режимы; левая Y — продолжить. Правый стик над учебным экраном — прокрутка.

**ПК:** F — Scanner, E — BioTool, C — режим, ПКМ — осмотр, колесо над теорией — прокрутка, → — продолжить. «Вся теория»/«Кратко» и «К действию» переключают чтение.

## Сборки и исходники

- PICO и Quest — отдельные APK **{version}**, versionCode **{settings['android_version_code']}**, ARM64/IL2CPP/OpenXR.
- `unity/BiologyVR/` — самостоятельный Unity-проект; открывать эту папку в Unity Hub.
- Главная сцена: `Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
- `play/` — WebGL {version}; `VERSION.json` — версии и SHA-256.
- Подробности: [заметки релиза](unity/BiologyVR/{name}).

Проверки PC, simulated XR 03–08/09–18, непрерывности потока 03–16, APK-подписи и загрузки WebGL прошли. Физические шлемы требуют отдельной проверки.
''', encoding='utf-8')
html_path=repo/'index.html';html=html_path.read_text(encoding='utf-8')
html=html.replace('0.9.0',version).replace('Иммунный патруль и работа с разрывом','Полная теория, непрерывный поток и прицеливание').replace('Иммунный патруль ·','Учебный кровоток ·')
paragraph='<p>Scanner открывает полную учебную карточку на левом экране; теория остаётся после отпускания образца. Эритроцит связан с Hb, гемом/Fe²⁺, O₂ и CO₂. Экраны расположены одинаково на всём маршруте, кровоток непрерывен, лучи следуют осям контроллеров.</p>'
html=re.sub(r'<p>Выберите конкретный экземпляр потока:.*?</p>', paragraph, html)
html_path.write_text(html,encoding='utf-8')
working=root/'WORKING_VERSION_RU.md';history=working.read_text(encoding='utf-8-sig')
header=f'# bio_0.3 — текущее состояние / {version}\n\nАктуальные изменения и проверки: **{name}**.\nPICO / Quest / WebGL {version}; единый учебный экран, полный текст после Scanner, непрерывный кровоток и исправленные направляющие.\n\n## История предыдущих итераций\n\n'
if not history.startswith(f'# bio_0.3 — текущее состояние / {version}'):
    working.write_text(header+history,encoding='utf-8')
ready=root/'READY_TO_OPEN_RU.md';ready.write_text(ready.read_text(encoding='utf-8-sig').replace('0.9.0',version).replace('code 10',f"code {settings['android_version_code']}").replace('RELEASE_0_9_0_RU.md',name),encoding='utf-8')
print(f'Prepared {version} notes and website')
