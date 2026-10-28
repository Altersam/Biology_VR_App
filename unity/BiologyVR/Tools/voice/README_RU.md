# Голос профессора (Biology VR 2)

Озвучка делается заранее, вне Unity, из тех же строк, что стоят в коде. В игре реплика ищется по тексту субтитра: строка без записи показывается только субтитром.

## Что нужно

- Python 3.10+; `pip install piper-tts praat-parselmouth scipy numpy`; ffmpeg (по желанию — тогда файлы будут .ogg).
- Голосовая модель Piper `ru_RU-ruslan-medium` (мужской голос) в `models/vits-piper-ru_RU-ruslan-medium/` — архив `vits-piper-ru_RU-ruslan-medium.tar.bz2` из релиза `tts-models` проекта k2-fsa/sherpa-onnx на GitHub (там же лежат denis / dmitri).

## Как обновить озвучку после правки реплик

1. `python extract_lines.py ../../Assets/BiologyVR/BiologyVR2/Runtime out/lines.json` — собирает все реплики: аргументы `Say(...)`, `Prof.Say(...)`, `WhatNext(...)` (обе ветки тернарников и оба варианта `C(VR, ПК)`) и «фразы профессора» из справочника. Строки с `$"…{…}"` в репликах не допускаются — числа выводятся тостом на визоре.
2. `python gen_voice.py out/lines.json ../../Assets/BiologyVR/BiologyVR2/Audio/Voice` — синтез (Piper «Руслан», чуть медленнее), обработка (Praat: форманты ×0,965, средний тон 104 Гц — голос учёного 40–50 лет, тёплый низ, мягче верх), обрезка тишины, выравнивание громкости и кривые губ (открыт рот / «о-у» / «и-э», 50 кадров в секунду) в `voice_manifest.json`.
3. Unity: `Biology VR 2/Voice/Import Voice Bank` (или `Biology VR 2/Build Scene` — делает то же) → `Generated/Voice/ProfessorVoice.asset`.

Произношение: числа и латиница в озвучке заменяются словами (таблица `SPOKEN` в `gen_voice.py`; новые числа/латиница — дописать туда, иначе скрипт остановится). Субтитры при этом не меняются.
