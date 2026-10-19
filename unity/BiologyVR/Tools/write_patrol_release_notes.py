"""Prepare current release notes/site only from completed build and input reports."""
import json
from pathlib import Path

root = Path(__file__).resolve().parent.parent
repo = root.parent / 'Github_VR' / 'Biology_VR_App'
reports = root / 'Assets/BiologyVR/ArteryJourney/Reports'
settings = json.loads((root / 'Tools/release_settings.json').read_text(encoding='utf-8-sig'))
version = settings['version']
def read(name):
    return json.loads((reports / name).read_text(encoding='utf-8-sig'))
xr = read('Polish/PatrolWoundIntegration/XR_Input.json')
focused = read('Polish/PatrolWoundIntegration/XR_Focused.json')
prelude = read('Polish/PatrolWoundIntegration/XR_Prelude.json')
pc = read('Polish/ImmunePatrol/PC_Input.json')
pico, apk = read('PicoBuildReport.json'), read('PicoApkVerification.json')
quest, quest_apk = read('MetaQuestBuildReport.json'), read('MetaQuestApkVerification.json')
web, browser = read('WebGLBuildReport.json'), read('WebGLBrowserSmoke.json')
if not all(p.get('passed') and not p.get('running') for p in (xr, focused, prelude, pc, apk, quest_apk, browser)):
    raise SystemExit('Passing current input, artifact and browser reports required')
if pico['result'] != 'Succeeded' or quest['result'] != 'Succeeded' or web['result'] != 'Succeeded' or apk['version'] != version or quest_apk['version'] != version or web.get('version') != version:
    raise SystemExit('Current player builds required')
download = f'https://github.com/Altersam/Biology_VR_App/releases/download/v{version}/BiologyVR_Pico4_v{version}.apk'
quest_download = f'https://github.com/Altersam/Biology_VR_App/releases/download/v{version}/BiologyVR_MetaQuest_v{version}.apk'
notes = f'''# Biology VR {version} — иммунный патруль и работа с разрывом

## Что изменилось

- В этапе 10 единой артериальной сцены — иммунный надзор: Scanner стенки → повторное сканирование/cutaway → шесть очагов в произвольном порядке → направляющий сигнал Reverse → миграция и контакт лейкоцитов → апоптоз. Повторное задание на растягивание артерии отсутствует.
- Переиспользованы JourneyMission, BloodFlow, пул WBC, Scanner, Reverse, главный экран и существующая послойная инспекция. Группы 3/4/3/5/4/5, суммарно 24 WBC, максимум пять активных респондентов.
- Исправлено застревание притока клеток при мобильном бюджете 180: недавно введённая клетка получает время доплыть до игрового участка.
- Три диагностических действия в этапе 11 выполняются Scanner на видимой ране; между измерениями нужно отпустить курок.
- Остановка существующего перехода ближе к ране; во всех её этапах сохраняется одна позиция. Снижение АД задаётся самой раной, независимо от предыдущего сосудистого урока.
- Светлый контур показывает место прикрепления; при правильном наведении он становится мягко-зелёным. Экран последовательно объясняет захват, активацию, выбор Reverse и отпускание курка. Короткая подпись сопровождает подходящий тромбоцит общего потока или удерживаемый образец.
- Прикреплённые тромбоциты развёрнуты лицевой стороной к просвету и окрашены тёплым янтарным материалом; пробка и фибрин читаются крупнее. Есть отдельная обратная связь о промахе и прикреплении.
- Тромбоцит из общего потока: ПРИТЯЖЕНИЕ → Scanner активирует → РЕВЕРС → удержать и отпустить в видимый разрыв. Не требуется знать скрытые точки размещения. Промах и неактивированный тромбоцит не дают прогресс.
- Одиннадцать попаданий формируют существующую пробку и фибрин; утечка уменьшается постепенно.
- Баланс той же пробки: существующее поле ПРИТЯЖЕНИЕ снимает тромбоциты на расстоянии и возвращает их при отпускании у раны. Исправлена пограничная проверка оптимального количества.
- Существующие информационные панели в тканевых эпизодах разнесены по сторонам, освобождая центральную область. Устранена конкуренция старых текстов/телеметрии с подсказками текущего этапа.
- Сборка проверяет наличие шести очагов и слоя гемостаза в фактически включённой мастер-сцене. Версия выводится на экране.
- Предпросмотр и запуск фагоцитов используют одинаковый расчёт движения: узкая помощь при наведении учитывает движение видимого вируса и замедление клетки. После запуска траектория не самонаводящаяся.
- Финальный Reverse обрабатывается один раз за нажатие; уже удаляемый комплекс не даёт повторного зачёта. Исправлена XRI-маска захвата антитела существующим полем.

## Управление PICO

- Левый Trigger — Scanner: навести и удерживать, отпускать между измерениями.
- Правый Trigger — выбранное действие BioTool; A/B — следующий/предыдущий режим.
- Режимы: ИМПУЛЬС, ПРИТЯЖЕНИЕ, СОСУДИСТОЕ ПОЛЕ, РЕВЕРС. Активация клеток — Scanner.
- Патруль: РЕВЕРС, короткое удержание на очаге; клетки выполняют ответ автоматически.
- Рана: зарядить активированный тромбоцит и отпустить правый курок, направив в разрыв.
- Баланс пробки: ПРИТЯЖЕНИЕ, удерживать и перемещать луч, отпустить у раны для прикрепления.
- Левая Y — продолжить завершённый этап. Внутри патруля нет принудительного движения/поворота XR Origin.

## Проверки

- PC patrol/platelet regression: PASS, {pc['utc']}.
- XR ранние механики 03–08: PASS, {prelude['utc']}.
- XR патруль/рана/баланс/восстановление: PASS, {focused['utc']}.
- XR последовательный маршрут 09–18 до победы, шести измерений гомеостаза и итогового отчёта: PASS, {xr['utc']}.
- PICO build: {pico['result']}, ошибок {pico['errors']}, предупреждений {pico['warnings']}.
- Meta Quest / Oculus build: {quest['result']}, ошибок {quest['errors']}, предупреждений {quest['warnings']}; Meta profiles/headtracking/VR category и отсутствие PICO-библиотек проверены.
- Подпись, ARM64/IL2CPP/OpenXR, манифест и версия APK: PASS.
- WebGL build: {web['result']}; загрузка в Chromium: PASS.
- XR-прогоны — симулированные события устройств с реальными action bindings. Эта сборка ещё не проверена на физическом PICO; FPS/термика/комфорт на шлеме не измерены. Автоматизированные прогоны не являются ручной проверкой на шлеме.

## APK

[Скачать PICO 4 / PICO 4 Enterprise {version}]({download})

- Android versionCode: {settings['android_version_code']}.
- Package: `com.biologyvr.arteryjourney`.
- Размер: {apk['size_bytes']} байт.
- SHA-256: `{apk['sha256']}`.
- [Скачать Meta Quest / Oculus {version}]({quest_download}); размер {quest_apk['size_bytes']} байт; SHA-256: `{quest_apk['sha256']}`. Поддерживаемый профиль: Quest 2 / Pro / 3 / 3S.

Отчёты: `Assets/BiologyVR/ArteryJourney/Reports/Polish/PatrolWoundIntegration/`.
Unity **6000.1.9f1**, мастер-сцена `Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
'''
(root / f"RELEASE_{version.replace('.', '_')}_RU.md").write_text(notes, encoding='utf-8')
readme = f'''# Biology VR App — {version}

Образовательная VR-игра внутри единой артериальной сцены. Текущий рабочий проект — **bio_0.3**.

[Играть в браузере](https://altersam.github.io/Biology_VR_App/play/) · [PICO APK {version}]({download}) · [Quest / Oculus APK {version}]({quest_download}) · [Релиз и SHA-256](https://github.com/Altersam/Biology_VR_App/releases/tag/v{version})

## Иммунный патруль и гемостаз

Вместо повторной коррекции диаметра в этапе 10: исследование изменённой стенки, существующий cutaway, шесть патологических очагов и управляемый сигналом Reverse ответ лейкоцитов. До пяти активных WBC одновременно; движение игрока внутри эпизода не требуется.

Разрыв: остановка ближе к ране, контур области прикрепления, три измерения Scanner, затем захват тромбоцитов из крови, активация Scanner и запуск Reverse. Экран показывает одно следующее действие; контур и траектория подтверждают правильное наведение, а подпись помогает найти тромбоцит. 11 попаданий постепенно останавливают утечку и формируют фибрин. Баланс пробки доступен дистанционным полем BioTool.

Сохранены свободное исследование пяти популяций крови, связанный груз RBC/Hb, ранний сосудистый эксперимент, бляшка, шесть эмболов и иммунный маршрут. Учебный вирус требует одного успешного контакта, поздние — трёх.

## Управление

**VR:** левый Trigger — Scanner; правый — BioTool; A/B — режимы; левая Y — продолжить. Клетки захватываются ПРИТЯЖЕНИЕМ, активируются Scanner. РЕВЕРС в патруле задаёт сигнал, при удерживаемой клетке заряжает её запуск; отпускание запускает клетку.

**ПК:** F — Scanner, E — BioTool, C — режим, ПКМ — осмотр, колесо — глубина поля/диаметр, → — продолжить. Для баланса пробки удерживать E в ПРИТЯЖЕНИИ и отпускать у видимого разрыва.

## Состав публикации

- `unity/BiologyVR/` — Assets/.meta, Packages, ProjectSettings, Tools.
- `play/` — WebGL {version}; загрузка проверена в Chromium.
- `ReleaseFiles/` — локальные APK для Releases, исключены из Git.
- `VERSION.json` — отдельные версии WebGL/PICO/Quest и контрольные суммы.
- `unity/BiologyVR/RELEASE_{version.replace('.', '_')}_RU.md` — изменения, управление, проверенные сценарии и ограничения.

PICO и Meta Quest / Oculus **{version}**, versionCode **{settings['android_version_code']}**, ARM64/IL2CPP/OpenXR/OpenGLES3. APK раздельные: PICO-профиль для PICO 4/Enterprise; Meta-профиль для Quest 2/Pro/3/3S.

Unity **6000.1.9f1**. В Unity Hub открывать `unity/BiologyVR`. Единственная стартовая сцена: `Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.

## Проверено

PC-патруль/тромбоциты и XR-прогоны 03–08 и 09–18 до итогового отчёта прошли. Прогоны XR используют симулированные устройства и штатные привязки ввода. APK-манифест и подпись проверены. Проверка этой версии на физическом PICO остаётся отдельной проверкой.

[Концепт проекта](https://github.com/Altersam/Biology_VR) хранится отдельно.
'''
(repo / 'README.md').write_text(readme, encoding='utf-8')
working = root / 'WORKING_VERSION_RU.md'
history = working.read_text(encoding='utf-8-sig')
header = f'''# bio_0.3 — текущее состояние / {version}

Рабочий проект: `{root}`. GitHub-комплект: `{repo}`.
Android ASCII alias: `Z:\\BiologyVR_bio_03` (Tools/create_ascii_build_alias.ps1).
Unity 6000.1.9f1; мастер-сцена ArteryNarrativeVR_VisualRework.unity.

Актуальные механики, управление, результаты PC/XR и параметры PICO/WebGL — **RELEASE_{version.replace('.', '_')}_RU.md**.
Патруль: Scanner → cutaway → Reverse → шесть очагов. Рана: притяжение → Scanner → Reverse в видимый разрыв; баланс — дистанционным полем.
Версия PICO, Meta Quest / Oculus и WebGL — {version}.

## История прежних итераций (пути и управление ниже могут быть устаревшими)

'''
if not history.startswith(f'# bio_0.3 — текущее состояние / {version}'):
    working.write_text(header + history, encoding='utf-8')
else:
    working.write_text(history.replace(f'Версия PICO/WebGL — {version}; отдельная Meta Quest остаётся 0.7.0.', f'Версия PICO, Meta Quest / Oculus и WebGL — {version}.'), encoding='utf-8')
(root / 'READY_TO_OPEN_RU.md').write_text(f'''# Biology VR / bio_0.3 / {version}

Unity **6000.1.9f1**. В рабочем каталоге открыть **bio_0.3**, в репозитории — **unity/BiologyVR**.
Master scene: `Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
Для Android использовать `Z:\\BiologyVR_bio_03`, созданный `Tools/create_ascii_build_alias.ps1`.

Изменения и проверки: **RELEASE_{version.replace('.', '_')}_RU.md**.
PICO и Meta Quest / Oculus APK {version}, code {settings['android_version_code']}; WebGL {version}.
PC patrol/platelets и XR-маршруты 03–08, 09–18 прошли. Проверка XR — симуляция контроллеров, не физический шлем.
Готовый APK: `Builds/Pico4/BiologyVR_Pico4_v{version}.apk`.
Library/Temp/Logs восстанавливаются Unity и не включаются в GitHub-комплект.
''', encoding='utf-8')
html_path = repo / 'index.html'
html = html_path.read_text(encoding='utf-8')
html = html.replace('Player Feedback Rework · 0.7.0', f'Иммунный патруль · {version}')
html = html.replace('Версия 0.7.0 · Переработка по реальному прохождению', f'Версия {version} · Иммунный патруль и работа с разрывом')
html = html.replace('Фагоцит: притяжение → Scanner → активация → реверс → прицелиться → отпустить Trigger. Первый медленный вирус требует трёх контактов свежих клеток. Режимы работают вручную; импульс может разрушать объекты кровотока.', 'Фагоцит: притяжение → Scanner активирует → реверс → прицелиться → отпустить Trigger. Учебный вирус требует одного попадания, поздние — трёх. В иммунном патруле Reverse направляет лейкоциты к шести очагам за стенкой сосуда.')
html = html.replace('Постоянный насыщенный кровоток, захват тромбоцитов с автоматической адгезией, отдельная биохимическая стабилизация, голографические экраны и исправленные XR-направляющие.', 'Разрыв: Scanner измеряет рану, притяжение захватывает тромбоцит, Scanner активирует его, Reverse запускает в видимый разрыв. Одиннадцать попаданий формируют пробку и фибрин. Баланс пробки регулируется дистанционным полем BioTool.')
html = html.replace('https://github.com/Altersam/Biology_VR_App/releases/download/v0.7.0/BiologyVR_Pico4_v0.7.0.apk', download)
html = html.replace('Скачать APK для PICO 4</a>', f'Скачать APK {version} для PICO 4</a>')
html = html.replace('Скачать APK для Meta Quest / Oculus</a>', 'Meta Quest / Oculus — предыдущая 0.7.0</a>')
html = html.replace('https://github.com/Altersam/Biology_VR_App/releases/download/v0.7.0/BiologyVR_MetaQuest_v0.7.0.apk', quest_download)
html = html.replace('Meta Quest / Oculus — предыдущая 0.7.0</a>', f'Скачать APK {version} для Meta Quest / Oculus</a>')
html_path.write_text(html, encoding='utf-8')
print(f'Prepared release notes, README and site for {version}')
