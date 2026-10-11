import hashlib
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

root=Path(__file__).resolve().parent.parent
report=root/'Assets/BiologyVR/ArteryJourney/Reports/PicoApkVerification.json'
proof=json.loads(report.read_text(encoding='utf-8-sig'))
release=json.loads((root/'Tools/release_settings.json').read_text(encoding='utf-8-sig'))
version=release['version']
source=root/'Builds/Pico4Enterprise/BiologyVR_Pico4Enterprise.apk'
if not proof['passed'] or not proof['checks'].get('version_matches') or proof.get('version')!=version:raise SystemExit('Current Pico release verification required')
if not (root/'Builds').is_dir():raise SystemExit('Existing Builds parent required')
folder=root/'Builds/Pico4';folder.mkdir(exist_ok=True);dest=folder/f'BiologyVR_Pico4_v{version}.apk'
shutil.copy2(source,dest)
if hashlib.sha256(dest.read_bytes()).hexdigest()!=proof['sha256']:raise SystemExit('APK copy mismatch')
manifest={'utc':datetime.now(timezone.utc).isoformat(),'version':version,'version_code':release['android_version_code'],'device':'PICO 4 / PICO 4 Enterprise','file':dest.name,'local_path':str(dest),'bytes':dest.stat().st_size,'sha256':proof['sha256'],'verification':proof['checks'],'hardware_verified':False,'github_release_uploaded':False}
(folder/'APK_VERIFICATION.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
(root/f"PICO4_APK_{version.replace('.','_')}.json").write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'apk':str(dest),'bytes':manifest['bytes'],'sha256':manifest['sha256'],'passed':True},ensure_ascii=False,indent=2))
