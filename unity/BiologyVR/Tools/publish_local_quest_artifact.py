import hashlib
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

root = Path(__file__).resolve().parent.parent
release = json.loads((root / 'Tools/release_settings.json').read_text(encoding='utf-8-sig'))
proof = json.loads((root / 'Assets/BiologyVR/ArteryJourney/Reports/MetaQuestApkVerification.json').read_text(encoding='utf-8-sig'))
version = release['version']
if not proof['passed'] or proof.get('version') != version or not proof['checks'].get('version_matches'):
    raise SystemExit('Current verified Meta Quest artifact required')
folder = root / 'Builds/MetaQuest'
if not folder.is_dir():
    raise SystemExit('Quest build folder required')
dest = folder / f'BiologyVR_MetaQuest_v{version}.apk'
shutil.copy2(folder / 'BiologyVR_MetaQuest.apk', dest)
if hashlib.sha256(dest.read_bytes()).hexdigest() != proof['sha256']:
    raise SystemExit('Quest artifact copy mismatch')
manifest = {'utc': datetime.now(timezone.utc).isoformat(), 'version': version,
            'version_code': release['android_version_code'], 'device': 'Meta Quest 2 / Pro / 3 / 3S',
            'file': dest.name, 'local_path': str(dest), 'bytes': dest.stat().st_size,
            'sha256': proof['sha256'], 'verification': proof['checks'],
            'hardware_verified': False, 'github_release_uploaded': False}
(folder / 'APK_VERIFICATION.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
(root / f"QUEST_APK_{version.replace('.', '_')}.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({'apk': str(dest), 'bytes': manifest['bytes'], 'sha256': manifest['sha256'], 'passed': True}, ensure_ascii=False, indent=2))
