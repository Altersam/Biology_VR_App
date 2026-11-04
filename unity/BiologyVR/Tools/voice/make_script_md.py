"""PROFESSOR_LINES_RU.md: every professor line by chapter / episode for a live recording session (usage: python make_script_md.py [project root])."""
import re, json, os, sys, collections
HERE=os.path.dirname(os.path.abspath(__file__)); ROOT=sys.argv[1] if len(sys.argv)>1 else os.path.abspath(os.path.join(HERE,'..','..'))
RT=os.path.join(ROOT,'Assets/BiologyVR/BiologyVR2/Runtime')
man=json.load(open(os.path.join(ROOT,'Assets/BiologyVR/BiologyVR2/Audio/Voice/voice_manifest.json'),encoding='utf-8'))['lines']
bytext={m['text']:m for m in man}
# reuse the literal parser from extract_lines
src=open(os.path.join(HERE,'extract_lines.py'),encoding='utf-8').read()
ns={}; exec(src.split("lines, interp = {}, []")[0].replace("root = sys.argv[1]; out = sys.argv[2]",""), ns)
literals, call_args = ns['literals'], ns['call_args']
MOOD={'Neutral':'спокойно','Smile':'с улыбкой, тепло','Serious':'серьёзно, собранно','Surprised':'удивлённо, с напором','Sad':'с сожалением','Angry':'резко','Thinking':'задумчиво'}
EPI={'Intro':'Начало','Lab':'Лаборатория','Tools':'Приборы','Scan':'Сканирование','FirstScan':'Первое сканирование','Shrink':'Уменьшение','Release':'Выпуск в кровоток','SwimTraining':'Обучение плаванию','Blood':'Кровь','Wall':'Стенка','Lumen':'Просвет и кровоток','Spasm':'Спазм','Debrief':'Итог','Diagnose':'Диагностика','MeltCore':'Импульс по бляшке','Reseat':'Перезакрепиться','Rupture':'Разрыв бляшки','Chase':'Погоня за эмболом','Occlusion':'Закупорка','Discover':'Чужак','OpenVirus':'Стоп-кадр: вирус','Antibodies':'Антитела','SignalBeat':'Сигнал','Waves':'Волны (руки-фагоциты)','Synchronise':'Добивание','Find':'Поиск разрыва','Plug':'Тромбоцитарная пробка','Cascade':'Каскад свёртывания','Stitch':'Прошивка фибрином','Cleanup':'Лишние сгустки','Recovery':'Заживление','Rollback':'Шок','Hold':'Оборона (руки-фагоциты)','Key':'Ключ','Traitors':'Предатели','Surge':'Последний выброс','Victory':'Победа','Hook':'Реакции','Sensors':'Датчики','Feedback':'Обратная связь','Exit':'Выход','Report':'Отчёт','Run':'Ход главы','Update':'Реакции','Flow':'Кровоток','H':'Подсказки','ScanPatient':'Сканирование пациента','SensorsB':'Датчики','Tutorial':'Обучение'}
CH={'Prelude':'Пролог «Лаборатория»','Chapter1':'Глава 1 «Поток»','Chapter2':'Глава 2 «Бляшка»','Chapter3':'Глава 3 «Первый контакт»','Chapter4':'Глава 4 «Прорыв»','Chapter5':'Глава 5 «Очаг»','Chapter6':'Глава 6 «Гомеостаз»','ChapterBase':'Общее для всех глав','Campaign':'Меню и переходы','Codex':'Моменты знания (справочник)','Game':'Общее','Hud':'Общее'}
order=['Prelude','Chapter1','Chapter2','Chapter3','Chapter4','Chapter5','Chapter6','ChapterBase','Campaign','Game','Codex']
info={}  # text -> (chapter key, episode, mood)
pcvar=set(); vrvar=set()
for path in sorted(os.listdir(RT)):
    if not path.endswith('.cs'): continue
    s=open(os.path.join(RT,path),encoding='utf-8').read(); key=path[4:-3]
    for m in re.finditer(r'\bC\s*\(', s):
        lits=[l for l in literals(call_args(s, m.end()-1)) if not isinstance(l,tuple)]
        if len(lits)>=2: vrvar.add(lits[0]); pcvar.add(lits[1])
    for m in re.finditer(r'\b(Say|WhatNext)\s*\(', s):
        args=call_args(s, m.end()-1)
        moods=sorted(set(re.findall(r'Mood\.(\w+)',args)))
        head=s[:m.start()]; em=list(re.finditer(r'(?:IEnumerator|void|bool|float)\s+(\w+)\s*\(',head))
        epi=em[-1].group(1) if em else ''
        # inside a lambda in Hook(): keep Hook
        for lit in literals(args):
            if isinstance(lit,tuple): continue
            if lit in bytext and lit not in info: info[lit]=(key,epi,moods)
    if key=='Codex':
        for m in re.finditer(r'\bAdd\s*\(', s):
            lits=[l for l in literals(call_args(s, m.end()-1)) if not isinstance(l,tuple)]
            if len(lits)>=5 and lits[3] in bytext: info.setdefault(lits[3],('Codex',lits[1],[]))
groups=collections.OrderedDict((k,[]) for k in order)
for m in man:
    k,e,md=info.get(m['text'],('Game','',[]))
    groups.setdefault(k,[]).append((m,e,md))
total=sum(m['seconds'] for m in man)
out=[]
out.append('# Biology VR 2 — все реплики профессора (сценарий для записи голоса)\n')
out.append(f'{len(man)} реплик, около {total/60:.0f} минут чистого звука (по длительности нынешнего синтеза). Обновлено 03.11.2026 — включая новые реплики про бой «руками-фагоцитами» (главы 3 и 5) и подсказку к эмболу (глава 2).\n')
out.append('## Как записывать\n')
out.append('- **Голос:** профессор-биолог 40–50 лет, тёплый, спокойный, с улыбкой; объясняет школьнику 8–9 класса. В бою — собранно и энергично, но без крика.')
out.append('- **Одна реплика — один файл.** Имя файла — из колонки «Файл», расширение любое (`.wav` лучше): например `VO_Prelude_001_1a2b3c4d.wav`. По этому имени игра найдёт запись вместо синтеза.')
out.append('- **Формат:** WAV, 44,1 или 48 кГц, моно, 16 или 24 бит. Тишина в начале и конце — не больше полсекунды; без музыки и эффектов (эфирный фильтр «связи» игра накладывает сама).')
out.append('- **Читать как в колонке «Сказать»:** там числа и латиница уже расписаны словами. Субтитр в игре останется как в коде.')
out.append('- **Интонация** — в колонке «Как»: подсказка из сценария (настроение лица профессора в этот момент).')
out.append('- Готовые файлы — в папку `Assets/BiologyVR/BiologyVR2/Audio/Voice/` (или пришлите мне): я пересчитаю движение губ по записи и подключу в банк (`Biology VR 2/Voice/Import Voice Bank`).')
out.append('- Если реплику хочется сказать своими словами — меняйте смело, только пометьте: текст субтитра тогда тоже надо поправить в коде, иначе реплика не найдётся.\n')
n=0
for k,items in groups.items():
    if not items: continue
    out.append(f'## {CH.get(k,k)}\n')
    out.append('| № | Эпизод | Сказать | Как | Файл | с |')
    out.append('|---|---|---|---|---|---|')
    for m,e,md in items:
        n+=1
        epi=EPI.get(e,e) if k!='Codex' else f'Знание: {e}'
        how=(('по ситуации: ' if len(md) > 1 else '') + ', '.join(MOOD.get(x,x) for x in md)) if md else ('вдохновенно, как маленькое открытие' if k=='Codex' else '')
        if m['text'] in pcvar: how=(how+'; ' if how else '')+'вариант для ПК (без шлема)'
        elif m['text'] in vrvar: how=(how+'; ' if how else '')+'вариант для шлема'
        say=m['spoken'].replace('|','/').replace('\n',' ')
        out.append(f"| {n} | {epi} | {say} | {how} | `{m['file'].rsplit('.',1)[0]}` | {m['seconds']:.1f} |")
    out.append('')
out.append('## Произношение\n')
out.append('- «BioTool» — «Био-тул»; «Т-киллер» — «Тэ-киллер»; «ДНК» — «дэ-эн-ка»; ударения: «тромбОциты», «фибрИн», «эритрОцит», «лейкОцит», «опсонизАция», «апоптОз».')
open(os.path.join(ROOT,'PROFESSOR_LINES_RU.md'),'w',encoding='utf-8').write('\n'.join(out)+'\n')
print(n, 'lines written', {k:len(v) for k,v in groups.items()})
