"""Verify published gh Release digests, Pages deployment and served WebGL bootstrap."""
import hashlib
import json
import shutil
import subprocess
import sys
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

root = Path(__file__).resolve().parent.parent
repo = root.parent / 'Github_VR' / 'Biology_VR_App'
settings = json.loads((root / 'Tools/release_settings.json').read_text(encoding='utf-8-sig'))
version, repository = settings['version'], settings['repository']
prefixes = settings.get('android_artifacts', ['PICO4', 'QUEST'])

def gh(*args):
    result = subprocess.run([sys.executable, str(root / 'Tools/github_cli.py'), *args],
                            capture_output=True, text=True, encoding='utf-8')
    if result.returncode:
        raise SystemExit(result.stderr)
    return json.loads(result.stdout)

def served(path):
    request = urllib.request.Request('https://altersam.github.io/Biology_VR_App/' + path,
                                     headers={'Cache-Control': 'no-cache'})
    with urllib.request.urlopen(request, timeout=60) as response:
        return response.read()

release = gh('release', 'view', settings['tag'], '--repo', repository,
             '--json', 'tagName,url,isDraft,publishedAt,assets')
head = gh('api', f'repos/{repository}/commits/main')['sha']
runs = gh('run', 'list', '--repo', repository, '--limit', '10', '--json',
          'databaseId,name,status,conclusion,headSha,url')
pages = next((r for r in runs if r['headSha'] == head and r['name'] == 'pages build and deployment'), None)
checks = {
    'release_published': release['tagName'] == settings['tag'] and not release['isDraft'],
    'pages_deployed_current_head': bool(pages and pages['status'] == 'completed' and pages['conclusion'] == 'success'),
}
artifacts = []
for prefix in prefixes:
    proof_path = root / f"{prefix}_APK_{version.replace('.', '_')}.json"
    proof = json.loads(proof_path.read_text(encoding='utf-8-sig'))
    digest = hashlib.sha256(Path(proof['local_path']).read_bytes()).hexdigest()
    asset = next((a for a in release['assets'] if a['name'] == proof['file']), None)
    matched = bool(asset and asset['state'] == 'uploaded' and asset['size'] == proof['bytes']
                   and asset['digest'] == 'sha256:' + digest and digest == proof['sha256'])
    checks[prefix.lower() + '_github_digest_matches'] = matched
    artifacts.append({'file': proof['file'], 'bytes': proof['bytes'], 'sha256': digest,
                      'github_digest': asset['digest'] if asset else None,
                      'url': asset['url'] if asset else None, 'matched': matched})

checksums_name = f"SHA256SUMS_{version.replace('.', '_')}.txt"
checksum_asset = next((a for a in release['assets'] if a['name'] == checksums_name), None)
checks['checksums_uploaded'] = bool(checksum_asset and checksum_asset['digest'] ==
    'sha256:' + hashlib.sha256((repo / 'ReleaseFiles' / checksums_name).read_bytes()).hexdigest())
stamp = datetime.now(timezone.utc)
query = '?verify=' + str(int(stamp.timestamp()))
landing = served('index.html' + query).decode('utf-8-sig')
bootstrap = served('play/index.html' + query).decode('utf-8-sig')
checks['landing_version'] = f'Версия {version}' in landing
checks['served_webgl_version'] = f'productVersion: "{version}"' in bootstrap
for filename in ('play.loader.js', 'play.framework.js.unityweb'):
    local = root / 'Builds/WebGL/play/Build' / filename
    checks['served_' + filename + '_matches'] = hashlib.sha256(served('play/Build/' + filename + query)).digest() == hashlib.sha256(local.read_bytes()).digest()
report = {'utc': stamp.isoformat(), 'version': version, 'passed': all(checks.values()),
          'repository': repository, 'source_commit': head, 'release_url': release['url'],
          'webgl_url': 'https://altersam.github.io/Biology_VR_App/play/',
          'checks': checks, 'pages': pages, 'artifacts': artifacts,
          'hardware_verified': False}
report_name = f"RELEASE_PUBLICATION_{version.replace('.', '_')}.json"
(root / report_name).write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
if report['passed']:
    for prefix in prefixes:
        path = root / f"{prefix}_APK_{version.replace('.', '_')}.json"
        proof = json.loads(path.read_text(encoding='utf-8-sig'))
        proof['github_release_uploaded'] = True
        path.write_text(json.dumps(proof, ensure_ascii=False, indent=2), encoding='utf-8')
        shutil.copy2(path, repo / 'unity/BiologyVR' / path.name)
    shutil.copy2(root / report_name, repo / 'unity/BiologyVR' / report_name)
    shutil.copy2(Path(__file__), repo / 'unity/BiologyVR/Tools' / Path(__file__).name)
print(json.dumps(report, ensure_ascii=False, indent=2))
raise SystemExit(0 if report['passed'] else 1)
