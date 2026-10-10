# Голографический UI — второй проход читаемости

Рабочий проект на Z: `bio_v0.2`; master-сцена `ArteryNarrativeVR_VisualRework.unity`.

## Улучшения

- Стекло: alpha 0,30; контентные карточки 0,24, зона задачи 0,30.
  Сосуд остаётся видимым, текст получает более спокойный фон.
- Заголовок темы — крупнее и жирнее; основной текст, подписи, кнопки и значения
  мониторинга увеличены. Теория и схема занимают отдельные зоны; задача вмещает
  до трёх строк без наложения на подсказку.
- Левый экран: **0,64 × 0,82 м**, на прежнем расстоянии **1,7 м** со смещением
  **0,43 м** влево. Правый монитор увеличен примерно на 12 %.
- Тонкие контуры чуть выразительнее; amber сохраняет роль сигнала состояния.
- Общий лёгкий UI-material выводит панели перед кровяными клетками: объекты
  просвечивают сквозь стекло, но не перекрывают буквы и цифры.
- Сохранены masking/stencil, RectMask2D, скролл и стандартные UI-взаимодействия.
- Scanner получил тонкий активный sweep-контур на вращающемся роторе.
  BioTool — небольшой энергетический контур передней короны в активных режимах;
  при CAPTURE он выключен, остаются hook-движение и удерживаемый tether.
- Cream-корпуса немного приглушены для сохранения формы и снижения белого блика.

## Лёгкость и сохранение механики

`HolographicOverlayUI.shader` — простой unlit UI shader с одним texture sample,
поддержкой clipping/stencil/stereo, alpha blend, без depth write и post effects.
Крупные поверхности имеют два прозрачных слоя; glow — только тонкий контур.
Новые индикаторы инструментов — LineRenderer, без частиц, света и коллайдеров.

`Reports/CleanHolographicSecondPass/SecondPass.json` фиксирует одинаковую геометрию
до/после, сохранённые aim transforms и InputActions. Targets 67, кровяной пул 360.
Scene/assets backup — в `Reports/CleanHolographicSecondPass/Backup`.

## Просмотр

Папка: `Assets/BiologyVR/ArteryJourney/Reports/CleanHolographicSecondPass/`.

- `01_Overview.png` — оба экрана в сцене;
- `02_Full_Reference.png` — полная справка после скролла;
- `03_Scanner_Working.png` — диагностический sweep;
- `04_BioTool_Capture.png`, `05_BioTool_Tonus.png`, `06_BioTool_Impulse.png`;
- `Before_After.jpg` — сравнение с первым проходом;
- `SecondPass_Review.jpg` — общий обзор.

Актуальные статусы ПК/XR — `Reports/Polish/FlowPickup/NativeCursor_PC_Input.json`
и `Reports/Polish/VR_INPUT_PLAYTHROUGH_V7.md`. Физический шлем/FPS не проверены.

Управление справкой прежнее: «Подробнее» / Tab; колесо или правый стик над справкой
— прокрутка; «Назад» / «К практике» — краткий вид.
