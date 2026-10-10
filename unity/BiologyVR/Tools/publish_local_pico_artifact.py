import hashlib
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

root=Path(__file__).resolve().parent.parent
report=root/'Assets/BiologyVR/ArteryJourney/Reports/PicoApkVerification.json'
proof=json.loads(report.read_text(encoding='utf-8-sig'))
source=root/'Builds/Pico4Enterprise/BiologyVR_Pico4Enterprise.apk'
if not proof['passed'] or not proof['checks'].get('version_0_3_0'):raise SystemExit('Pico 0.3.0 verification required')
if not (root/'Builds').is_dir():raise SystemExit('Existing Builds parent required')
folder=root/'Builds/Pico4';folder.mkdir(exist_ok=True);dest=folder/'BiologyVR_Pico4_v0.3.0.apk'
shutil.copy2(source,dest)
if hashlib.sha256(dest.read_bytes()).hexdigest()!=proof['sha256']:raise SystemExit('APK copy mismatch')
manifest={'utc':datetime.now(timezone.utc).isoformat(),'version':'0.3.0','version_code':4,'device':'PICO 4 / PICO 4 Enterprise','file':dest.name,'local_path':str(dest),'bytes':dest.stat().st_size,'sha256':proof['sha256'],'verification':proof['checks'],'hardware_verified':False,'github_release_uploaded':False}
(folder/'APK_VERIFICATION.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
(root/'PICO4_APK_0_3_0.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'apk':str(dest),'bytes':manifest['bytes'],'sha256':manifest['sha256'],'passed':True},ensure_ascii=False,indent=2))
