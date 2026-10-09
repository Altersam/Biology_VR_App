# bio_v0.2 — первый visual-only проход Coral V6

Рабочий проект: `Z:\YandexDisk\ПетПроекты\Biology_VR\3д\Unity\bio_v0.2`.
Unity **6000.1.9f1**. Master scene:
`Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.

## Направление

Уточнённый пользовательский промпт имеет приоритет над экспериментом V5.
Артерия остаётся биологической, гладкой, читаемой и яркой. Рабочая геометрия
уровня сохранена; этот проход меняет прежде всего материалы/цвет/освещение.

- Палитра стенки: D96763, ED8173, F39A7F, A8444A, с тёплыми красными тенями.
- Большие elongated cells 7×3/tile. Меньше геометрического warping в рисунке,
  менее выражены ядра/жёсткие блики; мягкие тонкие красные границы.
- Normal strength **1,1 → 0,7**; высота offline normal bake 0,044 → 0,034.
  Совокупная нормальная выразительность примерно вдвое ниже V5; это ориентир
  параметров, а не измеренная глубина физической стенки.
- Добавленный V5 per-cell vertex displacement удалён. Wall mesh/colliders не
  перестраивались. Крупные клетки не превращены в отдельную тяжёлую geometry population.
- Сохранено согласование UV по окружности, устраняющее стык V5.
- Постоянный V5 root с cyan-дугами, стрелками и lime-декором выключен.
  Cyan остаётся у инструментов/интерактивов, не как оформление всего сосуда.
- RBC: C51F32, мягкий specular и сохранённая двояковогнутая модель.
  WBC — холодный почти белый F0F4FB; platelet — F1AF54; lipid — D0A34F.
  Другие учебные объекты сохраняют собственные материалы.
- Одна основная Directional Light, без realtime shadows; neutral-warm ambient.
  Простая дистанционная градация и дальняя linear fog, без volumetric/bloom/SSR.
- PulseAmount **0,012**, PulseBpm **72**: визуальный масштаб 1,000–1,012,
  с короткой мягкой transition надбавкой до 1,0135. Tone logic сохранена.
  Поля можно связать с физиологией позже; такой связи сейчас нет.
- Pathology tint локальна возле plaque/wound. Восстановление уменьшает этот
  локальный оттенок; весь тоннель не меняет цвет от номера эпизода.

## Backup

`Assets/BiologyVR/ArteryJourney/Reports/Polish/BioWorldV6/Backup/`:
сцена до V6, исходный shader и два visual runtime-компонента в .txt.
`Verification.json` подтверждает SHA-256 копий перед изменениями.
Дополнительный самостоятельный pre-art проект на Z:
`bio_v0.2_before_art_verified` (1738 files, verification PASS).
Старую `bio_v0.2_working_before_art` не использовать как подтверждённый backup:
она создавалась во время сбоя диска и содержит повреждённые файлы.

## Проверки

`Generated/Artery_Coral_V6/Apply.json`: path/targets/collider signatures
до/после равны. Ни одного нового physics object. InputActions, Scanner/BioTool
действия, cell transforms/flow/pool, переходы и puzzle logic не менялись.

`BioWorldV6/Capture.json`: первый кадр — свежая стартовая камера, без Enter/Accept
или принудительного направления головы. Остальные art-кадры меняют только камеру.
Scanner/BioTool evidence получены от actual input tests; отдельные Scene07/12/10/14
ПК-fixtures стейджируют эпизод один раз, затем действуют мышью/клавиатурой.

Компиляция текущего кода: 0 errors / 0 warnings.
PC input run UTC **2026-10-08T20:43:55.0063958Z**, PASS, Console 0 errors.
Sequential XR report: `Reports/Polish/VR_INPUT_PLAYTHROUGH_V7.md`, UTC
**2026-10-08T20:52:38.3254711Z**, **FULL SIMULATOR PASS**:
Scenes03–16 и VERY FAR, Console 0 errors. Подтверждены реальные actions/XRI
двуручного увеличения, газового эмбола, физического гемостаза, восстановления,
финальной иммунной очистки и шести измерений; состояние plaque/wound сохраняется.
Это simulated-controller run, не аппаратный Quest/Pico test.

Dependency audit этой копии UTC **2026-10-08T20:56:31.4969328Z**:
PASS, missingScripts=[], unresolved=[], 67 authored targets, тот же pool 360.
Текущий основной материал: Artery_Coral_V6/Coral_Endothelium_Coral_V6.mat.

## VR budget / границы проверки

Opaque URP wall shader: **3 texture reads во фрагменте, 0 в вершине**.
Нет tessellation/SSR/volumetric/multilayer/transparency для стенки.
Карты 2048², ASTC 6×6 + mipmaps; biological materials с GPU instancing.
Существующий pool 360 cells, mobile renderer limit 48 сохранён.

В обзоре Editor: 280 draw calls, 129705 triangles; это desktop scene с полным
пулом, не замер standalone Quest/Pico. Headset FPS/GPU/thermal пока не измерены.
Не следует считать shader budget доказательством комфортных аппаратных FPS.

## Кадры первого прохода

В `Assets/BiologyVR/ArteryJourney/Reports/Polish/BioWorldV6/`:
01_Player_Overview.png, 02_Wall_Close.png, 03_Tunnel_Depth.png,
04_RBC_Contrast.png, 05_Scanner_Actual_PC_Input.png,
06_BioTool_Actual_PC_Input.png, 07_Scanner_Actual_XR_Input.png.
Сводный лист: FirstPass_ContactSheet.jpg.

На этом художественный проход остановлен для оценки пользователем. Новые
глобальные перестройки уровня и декоративные системы не добавлялись.
