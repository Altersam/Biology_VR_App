import hashlib
import json
import shutil
from pathlib import Path

SOURCE=Path(r"C:\Users\alter\AppData\Local\Temp\opencode\BiologyVR_bio_v02_baseline")
DEST=Path(__file__).resolve().parent.parent.parent/"bio_v0.2_before_art_verified"

def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for data in iter(lambda:f.read(1024*1024),b''):h.update(data)
    return h.hexdigest()

if DEST.exists():raise SystemExit('Inspect existing baseline before replacing it')
DEST.mkdir()
errors=[];count=0
for source in SOURCE.rglob('*'):
    if not source.is_file():continue
    target=DEST/source.relative_to(SOURCE)
    try:
        target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)
        if sha(source)!=sha(target):errors.append(str(source.relative_to(SOURCE)))
        count+=1
    except Exception as error:errors.append({'path':str(source),'error':str(error)})
report={'path':str(DEST),'passed':not errors,'files':count,'errors':errors}
(DEST/'BASELINE_VERIFICATION.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))
raise SystemExit(0 if report['passed'] else 1)
