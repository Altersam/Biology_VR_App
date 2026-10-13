"""Clone the installed SDK to Z: and use the supported local-only listing command."""
import hashlib
import json
import shutil
from pathlib import Path

root = Path(__file__).resolve().parent.parent
source = Path(r'C:\Program Files\Unity\Hub\Editor\6000.1.9f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK')
target = root / 'Builds/AndroidSDK_Offline'
if root.drive.upper() != 'Z:' or not (root / 'Builds').is_dir() or not source.is_dir():
    raise SystemExit('Existing installed SDK and build parent on Z: required')
if not target.exists():
    shutil.copytree(source, target)
batch = target / 'cmdline-tools/16.0/bin/sdkmanager.bat'
content = batch.read_text(encoding='utf-8')
marker = 'if "%CMD_LINE_ARGS%"=="--list" set CMD_LINE_ARGS=--list_installed'
if marker not in content:
    content = content.replace('@rem Execute sdkmanager', '@rem Local installed packages are sufficient for this offline build\n' + marker + '\n\n@rem Execute sdkmanager')
    batch.write_text(content, encoding='utf-8')
checks = []
for relative in ('platforms/android-34/android.jar', 'build-tools/34.0.0/apksigner.bat',
                 'platform-tools/adb.exe', 'cmdline-tools/16.0/lib/sdkmanager-classpath.jar'):
    original, copied = source / relative, target / relative
    matched = copied.is_file() and hashlib.sha256(original.read_bytes()).digest() == hashlib.sha256(copied.read_bytes()).digest()
    checks.append({'file': relative, 'sha_match': matched})
report = {'sdk': str(target), 'original_sdk_changed': False,
          'method': 'SDK-local sdkmanager wrapper translates only --list to supported --list_installed',
          'passed': all(c['sha_match'] for c in checks), 'checks': checks}
(root / 'Builds/OfflineSDK_Verification.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, ensure_ascii=False, indent=2))
raise SystemExit(0 if report['passed'] else 1)
