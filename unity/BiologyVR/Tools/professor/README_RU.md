# Профессор: модель (волосы, говорящий рот)

Исходник — `3д/Профессор/professor_v4.blend` (до проверки тела — `professor_v3_talking.blend`) (собран из частей «Профессор по частям» и `голова_эмоции.blend`). Экспорт → `Assets/BiologyVR/BiologyVR2/Art/Professor/Professor.fbx`.

Скрипты (Blender 4.2+ / модуль `bpy`, запуск `python скрипт.py` из папки с .blend):

- `hairfix.py` — `professor_relaxed.blend` → `professor_v2.blend`: волосяная «шапка» по черепу (закрывает виски, бакенбарды до бороды, огибает уши, кончается на затылке; текстура `Prof_HairCap_BaseColor.png`) + прежний чуб ниже и уже.
- `face_mouth.py` — `professor_v2.blend` → `professor_v3.blend`: рот разрезан по линии губ, внутри полость и два ряда зубов (в той же сетке головы: материалы `Prof_Head`, `Prof_Mouth`, `Prof_Teeth`); ключи формы `MouthOpen` (челюсть поворачивается в височно-нижнечелюстном суставе), `MouthO`, `MouthWide` на голове, `MouthOpen` на бороде и усах.
- `body_fix.py` — `professor_v3.blend` → `professor_v4.blend`: кисти вставлены в манжеты и продолжают предплечье (опорная точка — запястье), рубашка убрана под халат там, где проступала (спина, плечи; спереди воротник рубашки остаётся), основание шеи сзади уходит в воротник без «тарелки», значения ключей формы сброшены.
- `export_fbx.py` — `professor_v4.blend` → `unity/Professor.fbx`.
- `rmouth.py` — проверочные рендеры рта: `python rmouth.py -- professor_v3.blend m "" "MouthOpen=1" "MouthO=1,MouthOpen=0.35"`.

Кисти без текстуры: цвет кожи задаёт билдер (тон лица). В Unity сетки с ключами формы получают SkinnedMeshRenderer в билдере сцены; рот ведёт `Bvr2Professor` по кривым из `voice_manifest.json` (см. `Tools/voice`).
