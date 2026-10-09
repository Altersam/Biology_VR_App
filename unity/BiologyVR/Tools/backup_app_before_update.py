import hashlib
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

root=Path(__file__).resolve().parent.parent
source=root.parent/'Biology_VR_App'
dest=root.parent/('Biology_VR_App_before_v02_'+datetime.now().strftime('%Y%m%d_%H%M%S'))
if not source.is_dir() or not dest.parent.is_dir():raise SystemExit('Repository or backup parent missing')
dest.mkdir();count=0
for entry in source.iterdir():
    if entry.name=='.git':continue
    if entry.is_dir():shutil.copytree(entry,dest/entry.name,ignore=shutil.ignore_patterns('Library','Logs','__pycache__'))
    elif entry.is_file():shutil.copy2(entry,dest/entry.name)
errors=[]
for file in dest.rglob('*'):
    if not file.is_file():continue
    original=source/file.relative_to(dest)
    if hashlib.sha256(file.read_bytes()).digest()!=hashlib.sha256(original.read_bytes()).digest():errors.append(str(file.relative_to(dest)))
    count+=1
report={'utc':datetime.now(timezone.utc).isoformat(),'backup':str(dest),'files':count,'passed':not errors,'errors':errors,'git_copied':False}
(root/'GITHUB_UPDATE_BACKUP.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))
raise SystemExit(0 if report['passed'] else 1)
