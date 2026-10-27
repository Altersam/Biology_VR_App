# Biology VR App — 0.11.0 · Biology VR 2 (предпросмотр)

Учебная VR-игра о крови и сосудах для 8–9 классов. Рабочий проект — **bio_0.3**, Unity **6000.1.9f1**.

[PICO APK](https://github.com/Altersam/Biology_VR_App/releases/download/v0.11.0/BiologyVR2_Pico4_v0.11.0.apk) · [Quest / Oculus APK](https://github.com/Altersam/Biology_VR_App/releases/download/v0.11.0/BiologyVR2_MetaQuest_v0.11.0.apk) · [Релиз v0.11.0](https://github.com/Altersam/Biology_VR_App/releases/tag/v0.11.0) · [Прежняя версия 0.10.0](https://github.com/Altersam/Biology_VR_App/releases/tag/v0.10.0)

## Biology VR 2 — новая игра

Полностью переработанная игра: пролог в лаборатории профессора и шесть глав в одной артерии — «Поток», «Бляшка», «Первый контакт», «Прорыв», «Очаг», «Гомеостаз». Свободное плавание, способности BioTool (Притяжение, Импульс, Сигнал, Фактор), опыты с прогнозами, 38 знаний, профессор-голограмма с субтитрами.

- Отдельное приложение **Biology VR 2** (`com.biologyvr.biologyvr2`): ставится рядом с прежней версией и не заменяет её.
- APK **0.11.0**, versionCode **12**, ARM64/IL2CPP/OpenXR; PICO 4 / 4 Enterprise и Meta Quest 2 / Pro / 3 / 3S.
- **В шлеме ещё не проверялась.** Автопрохождение пролога и глав 1–6 в редакторе (симуляция ввода) прошло; управление реальными контроллерами, комфорт и частота кадров — на проверке.
- Исходники: `unity/BiologyVR/Assets/BiologyVR/BiologyVR2/`, сцена `Scenes/BiologyVR2.unity` (меню `Biology VR 2/Build Scene`); описание — [BIOLOGY_VR2_RU.md](unity/BiologyVR/BIOLOGY_VR2_RU.md), [заметки релиза](unity/BiologyVR/RELEASE_0_11_0_BVR2_RU.md). Голо-стол и стойка лаборатории — ассет Sci-Fi Hologram Screen Shader (Asset Store), в репозиторий не входит.

**Управление (VR):** левый стик — плыть; правый стик — вверх/вниз и поворот; A/X — рывок; левый курок — сканер; правый курок — BioTool, B — сменить способность; хват — якорь; оба хвата — сузить/расширить сосуд; Menu или Y — меню.

## Прежняя версия — 0.10.0 (WebGL в `play/`)

Учебная VR-игра внутри единой артерии. Рабочий проект — **bio_0.3**, Unity **6000.1.9f1**.

[Играть в браузере](https://altersam.github.io/Biology_VR_App/play/) · [PICO APK](https://github.com/Altersam/Biology_VR_App/releases/download/v0.10.0/BiologyVR_Pico4_v0.10.0.apk) · [Quest / Oculus APK](https://github.com/Altersam/Biology_VR_App/releases/download/v0.10.0/BiologyVR_MetaQuest_v0.10.0.apk) · [Релиз](https://github.com/Altersam/Biology_VR_App/releases/tag/v0.10.0)

### В этой версии

Непрерывный кровоток на всех артериальных этапах, центрированное плавное вращение частиц, гем с Fe²⁺ вместо ромба, единое положение экранов и исправленные лучи контроллеров.

Scanner открывает полную учебную карточку на левом экране. Для эритроцита доступны строение, функции и связи с гемоглобином, гемом, кислородом и углекислым газом. Теория сохраняется после отпускания клетки; задача остаётся видимой отдельно.

Сохранены свободное исследование крови, слои артерии, гемодинамика, бляшка, эмболы, иммунный патруль, разрыв/пробка/баланс/репарация и финал.

### Управление

**VR:** левый курок — Scanner, правый — BioTool; A/B — режимы; левая Y — продолжить. Правый стик над учебным экраном — прокрутка.

**ПК:** F — Scanner, E — BioTool, C — режим, ПКМ — осмотр, колесо над теорией — прокрутка, → — продолжить. «Вся теория»/«Кратко» и «К действию» переключают чтение.

### Сборки и исходники

- PICO и Quest — отдельные APK **0.10.0**, versionCode **11**, ARM64/IL2CPP/OpenXR.
- `unity/BiologyVR/` — самостоятельный Unity-проект; открывать эту папку в Unity Hub.
- Главная сцена: `Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
- `play/` — WebGL 0.10.0; `VERSION.json` — версии и SHA-256.
- Подробности: [заметки релиза](unity/BiologyVR/RELEASE_0_10_0_RU.md).

Проверки PC, simulated XR 03–08/09–18, непрерывности потока 03–16, APK-подписи и загрузки WebGL прошли. Физические шлемы требуют отдельной проверки.
