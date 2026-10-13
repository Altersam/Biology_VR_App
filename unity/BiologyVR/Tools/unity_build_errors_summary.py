"""Only print build diagnostics; Unity's environment dump is deliberately omitted."""
import json
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parent.parent
result = subprocess.run(['node', str(root / 'Tools/unity_ws_call.cjs'), 'get_console_logs',
                         str(root / 'Tools/console_errors_ws.json')], cwd=root,
                        capture_output=True, text=True, encoding='utf-8')
if result.returncode:
    raise SystemExit('Unity console transport unavailable')
body = json.loads(result.stdout)
for entry in body.get('result', {}).get('logs', []):
    text = entry.get('message', '')
    if 'Environment Variables:' in text:
        text = text.split('Environment Variables:')[0].splitlines()[0] + '\n' + (text.split('stderr[', 1)[1].split(']\nstdout[', 1)[0] if 'stderr[' in text else '')
    print(text)
