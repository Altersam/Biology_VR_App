"""Prepare the verified PICO release in the relocated app repository; no Git operations."""
import hashlib
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
REPO = ROOT.parent / 'Github_VR' / 'Biology_VR_App'
settings = json.loads((ROOT / 'Tools/release_settings.json').read_text(encoding='utf-8-sig'))
version = settings['version']
artifact = json.loads((ROOT / f"PICO4_APK_{version.replace('.', '_')}.json").read_text(encoding='utf-8-sig'))
reports = ROOT / 'Assets/BiologyVR/ArteryJourney/Reports'
checks = {}
for file in ('XR_Input.json', 'XR_Focused.json', 'XR_Prelude.json'):
    result = json.loads((reports / 'Polish/PatrolWoundIntegration' / file).read_text(encoding='utf-8-sig'))
    checks[file] = result.get('passed') is True and not result.get('running')
pc = json.loads((reports / 'Polish/ImmunePatrol/PC_Input.json').read_text(encoding='utf-8-sig'))
checks['PC_Input.json'] = pc.get('passed') is True and not pc.get('running')
if not all(checks.values()) or artifact['version'] != version or not all(artifact['verification'].values()):
    raise SystemExit('Current passing PC/XR tests and verified PICO artifact required')
source = Path(artifact['local_path'])
digest = hashlib.sha256(source.read_bytes()).hexdigest()
if digest != artifact['sha256']:
    raise SystemExit('PICO artifact changed after verification')
folder = REPO / 'ReleaseFiles'
if not REPO.is_dir():
    raise SystemExit('Existing app repository required')
folder.mkdir(exist_ok=True)
dest = folder / artifact['file']
shutil.copy2(source, dest)
if hashlib.sha256(dest.read_bytes()).hexdigest() != digest:
    raise SystemExit('Release copy mismatch')
(folder / f'SHA256SUMS_{version.replace(".", "_")}.txt').write_text(f'{digest}  {dest.name}\n', encoding='utf-8')
manifest_path = REPO / 'VERSION.json'
manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
manifest.update(version=version, pico_release_version=version, android_release_version=version,
                android_version_code=settings['android_version_code'])
manifest['releases'] = [entry for entry in manifest['releases'] if entry['file'] != dest.name]
manifest['releases'].append(dict(file=dest.name, bytes=dest.stat().st_size, sha256=digest,
                                 version=version, device=artifact['device'], tag=settings['tag']))
manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(dict(apk=str(dest), sha256=digest, verified=True, checks=checks), ensure_ascii=False, indent=2))
