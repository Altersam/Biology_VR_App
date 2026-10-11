"""Prepare verified release files and metadata; never commit or upload."""
import hashlib
import json
import shutil
from pathlib import Path

root = Path(__file__).resolve().parent.parent
repo = root.parent / 'Biology_VR_App'
release = json.loads((root / 'Tools/release_settings.json').read_text(encoding='utf-8-sig'))
version = release['version']
artifact_path = root / f"PICO4_APK_{version.replace('.', '_')}.json"
artifact = json.loads(artifact_path.read_text(encoding='utf-8-sig'))
if artifact['version'] != version or not all(artifact['verification'].values()):
    raise SystemExit('Verified current PICO artifact required')
source = Path(artifact['local_path'])
digest = hashlib.sha256(source.read_bytes()).hexdigest()
if digest != artifact['sha256']:
    raise SystemExit('Artifact hash changed')
dest = repo / 'ReleaseFiles'
if not repo.is_dir() or not dest.is_dir():
    raise SystemExit('Existing app repository and release parent required')
apk = dest / artifact['file']
shutil.copy2(source, apk)
if hashlib.sha256(apk.read_bytes()).hexdigest() != digest:
    raise SystemExit('Release copy mismatch')
(dest / 'SHA256SUMS_PICO_0_4_0.txt').write_text(f"{digest}  {apk.name}\n", encoding='utf-8')
manifest_path = repo / 'VERSION.json'
manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
manifest['pico_release_version'] = version
manifest['android_release_version'] = version
manifest['android_version_code'] = release['android_version_code']
entries = [item for item in manifest['releases'] if item['file'] != apk.name]
entries.append({'file': apk.name, 'bytes': apk.stat().st_size, 'sha256': digest,
                'version': version, 'device': 'PICO 4 / PICO 4 Enterprise', 'tag': release['tag']})
manifest['releases'] = entries
manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
report = {'version': version, 'file': str(apk), 'bytes': apk.stat().st_size,
          'sha256': digest, 'verified': True, 'uploaded': False}
(root / 'PICO_RELEASE_PREPARATION.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
