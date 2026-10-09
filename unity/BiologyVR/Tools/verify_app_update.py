"""Verify the final app repo after documentation edits, without staging or committing."""
import hashlib
import json
import re
import shutil
import subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent
REPO=ROOT.parent/'Biology_VR_App'
PROJECT=REPO/'unity/BiologyVR'
EXCLUDED={'McpUnitySettings.json','prepared_project.json','TRANSFER_VERIFICATION.json','RECOVERY_VERIFICATION.json','CURRENT_PROJECT_TRANSFER.json'}

def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda:stream.read(1024*1024),b''):h.update(block)
    return h.hexdigest()

def git(*args):
    result=subprocess.run(['git',*args],cwd=REPO,capture_output=True,text=True,encoding='utf-8',errors='replace')
    if result.returncode:raise RuntimeError(result.stderr)
    return result.stdout.strip()

def main():
    head_before=git('rev-parse','HEAD')
    shutil.copy2(Path(__file__),PROJECT/'Tools/verify_app_update.py')
    mismatches=[];count=0
    for folder in ('Assets','Packages','ProjectSettings','Tools'):
        for original in (ROOT/folder).rglob('*'):
            if not original.is_file() or original.name in EXCLUDED or '__pycache__' in original.parts:continue
            relative=original.relative_to(ROOT);target=PROJECT/relative
            if not target.is_file() or sha(original)!=sha(target):mismatches.append(relative.as_posix())
            count+=1
    for original in (ROOT/'Builds/WebGL/play').rglob('*'):
        if not original.is_file():continue
        relative=original.relative_to(ROOT/'Builds/WebGL/play');target=REPO/'play'/relative
        if not target.is_file() or sha(original)!=sha(target):mismatches.append('play/'+relative.as_posix())
    version=json.loads((REPO/'VERSION.json').read_text(encoding='utf-8'))
    apk_checks=[]
    for item in version['releases']:
        file=REPO/'ReleaseFiles'/item['file']
        apk_checks.append(file.is_file() and sha(file)==item['sha256'])
    html=(REPO/'play/index.html').read_text(encoding='utf-8')
    loader_files=[name.lstrip('/') for name in re.findall(r'"([^"\s]+\.(?:loader\.js|unityweb))"',html)]
    files=[p for p in REPO.rglob('*') if p.is_file() and '.git' not in p.relative_to(REPO).parts and p.relative_to(REPO).parts[0]!='ReleaseFiles']
    forbidden=[]
    for file in files:
        relative=file.relative_to(REPO)
        if file.name in EXCLUDED or file.name=='bridge-token' or file.suffix in ('.apk','.jks','.keystore'):forbidden.append(relative.as_posix())
        if relative.parts[:2]==('unity','BiologyVR') and relative.parts[2] in ('Library','Temp','Logs','UserSettings','GitHubUpload'):forbidden.append(relative.as_posix())
    oversized=[p.relative_to(REPO).as_posix() for p in files if p.stat().st_size>=100*1024*1024]
    attrs=git('check-attr','filter','--','play/Build/play.data.unityweb','play/Build/play.wasm.unityweb','unity/BiologyVR/Assets/BiologyVR/ArteryJourney/Generated/Artery_Coral_V6/Endothelium_Base_V6.png')
    ignored=subprocess.run(['git','check-ignore','ReleaseFiles/BiologyVR_MetaQuest.apk','ReleaseFiles/BiologyVR_Pico4Enterprise.apk','unity/BiologyVR/Library/McpUnity/bridge-token'],cwd=REPO,capture_output=True,text=True).stdout.splitlines()
    checks={'source_matches':not mismatches,'webgl_version': 'productVersion: "0.2.0"' in html,
            'loader_references':len(loader_files)>=4 and all((REPO/'play/Build'/name).is_file() for name in loader_files),
            'apks_match':all(apk_checks),'no_prohibited':not forbidden,'sizes_ok':not oversized,
            'play_not_lfs':attrs.count('filter: unset')==2,'unity_texture_lfs':'Endothelium_Base_V6.png: filter: lfs' in attrs,
            'local_binaries_and_tokens_ignored':len(ignored)==3,
            'remote_unchanged':git('remote','get-url','origin')=='https://github.com/Altersam/Biology_VR_App.git',
            'head_unchanged_during_verification':git('rev-parse','HEAD')==head_before}
    report={'utc':datetime.now(timezone.utc).isoformat(),'path':str(REPO),'passed':all(checks.values()),'checks':checks,
            'compared_source_files':count,'mismatches':mismatches,'forbidden':forbidden,'oversized':oversized,
            'upload_files':len(files),'upload_bytes':sum(p.stat().st_size for p in files),'local_head':head_before,'agent_committed':False,'agent_pushed':False}
    (ROOT/'GITHUB_FINAL_VERIFICATION.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(report,ensure_ascii=False,indent=2))
    return 0 if report['passed'] else 1

if __name__=='__main__':raise SystemExit(main())
