"""Update only the prepared app repository. Never commit, push or touch the concept repo."""
import hashlib
import json
import shutil
import subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent
REPO=ROOT.parent/'Biology_VR_App'
EXCLUDED={'McpUnitySettings.json','prepared_project.json','TRANSFER_VERIFICATION.json','RECOVERY_VERIFICATION.json','CURRENT_PROJECT_TRANSFER.json'}

def git(*args):
    result=subprocess.run(['git',*args],cwd=REPO,capture_output=True,text=True,encoding='utf-8',errors='replace')
    if result.returncode:raise RuntimeError(result.stderr)
    return result.stdout.strip()

def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda:stream.read(1024*1024),b''):h.update(block)
    return h.hexdigest()

def main():
    remote=git('remote','get-url','origin')
    if remote.rstrip('/').removesuffix('.git').lower()!='https://github.com/altersam/biology_vr_app':
        raise SystemExit('Unexpected remote; refuse to update this repository')
    before=git('rev-parse','HEAD')
    backup=json.loads((ROOT/'GITHUB_UPDATE_BACKUP.json').read_text(encoding='utf-8'))
    if not backup['passed'] or not Path(backup['backup']).is_dir():raise SystemExit('Verified pre-update backup required')
    web_report=json.loads((ROOT/'Assets/BiologyVR/ArteryJourney/Reports/WebGLBuildReport.json').read_text(encoding='utf-8'))
    if web_report['result']!='Succeeded':raise SystemExit('Current WebGL build did not succeed')
    browser=json.loads((ROOT/'Assets/BiologyVR/ArteryJourney/Reports/WebGLBrowserSmoke.json').read_text(encoding='utf-8'))
    if not browser['passed'] or 'productVersion: "0.2.0"' not in (ROOT/'Builds/WebGL/play/index.html').read_text(encoding='utf-8'):
        raise SystemExit('Current version 0.2.0 browser smoke-test required')
    project=REPO/'unity/BiologyVR'
    expected=set();copied=[]
    for folder in ('Assets','Packages','ProjectSettings','Tools'):
        for source in (ROOT/folder).rglob('*'):
            if not source.is_file() or '__pycache__' in source.parts or source.name in EXCLUDED:continue
            relative=source.relative_to(ROOT);expected.add(relative.as_posix())
            target=project/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
            copied.append((source,target))
    for name in ('.gitignore','.gitattributes','README.md','READY_TO_OPEN_RU.md','WORKING_VERSION_RU.md','ARTERY_CORAL_V6_RU.md','GITHUB_UPLOAD_RU.md','META_QUEST_RU.md','PICO4_ENTERPRISE_RU.md'):
        if (ROOT/name).is_file():
            shutil.copy2(ROOT/name,project/name);copied.append((ROOT/name,project/name))
    # These are managed subtrees. Obsolete files are preserved in the verified backup.
    for folder in ('Assets','Packages','ProjectSettings','Tools'):
        for target in (project/folder).rglob('*'):
            if target.is_file() and target.relative_to(project).as_posix() not in expected:target.unlink()
    web=ROOT/'Builds/WebGL/play';web_files=set()
    for source in web.rglob('*'):
        if not source.is_file():continue
        relative=source.relative_to(web);web_files.add(relative.as_posix());target=REPO/'play'/relative
        target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target);copied.append((source,target))
    for target in (REPO/'play').rglob('*'):
        if target.is_file() and target.relative_to(REPO/'play').as_posix() not in web_files:target.unlink()
    releases=[]
    for kind,name,check in (('MetaQuest','BiologyVR_MetaQuest.apk','MetaQuestApkVerification.json'),('Pico4Enterprise','BiologyVR_Pico4Enterprise.apk','PicoApkVerification.json')):
        proof=json.loads((ROOT/'Assets/BiologyVR/ArteryJourney/Reports'/check).read_text(encoding='utf-8'))
        source=ROOT/'Builds'/kind/name
        if not proof['passed'] or sha(source)!=proof['sha256'] or "versionName='0.2.0'" not in proof['badging']:
            raise SystemExit('Current APK verification required: '+kind)
        target=REPO/'ReleaseFiles'/name;target.parent.mkdir(exist_ok=True);shutil.copy2(source,target);copied.append((source,target))
        releases.append({'file':name,'bytes':target.stat().st_size,'sha256':proof['sha256']})
    failures=[str(target.relative_to(REPO)) for source,target in copied if sha(source)!=sha(target)]
    upload=[p for p in REPO.rglob('*') if p.is_file() and '.git' not in p.relative_to(REPO).parts and p.relative_to(REPO).parts[0]!='ReleaseFiles']
    forbidden=[]
    for file in upload:
        relative=file.relative_to(REPO)
        if file.name in EXCLUDED or file.name=='bridge-token' or file.suffix in ('.apk','.jks','.keystore'):forbidden.append(relative.as_posix())
        if relative.parts[:2]==('unity','BiologyVR') and relative.parts[2] in ('Library','Temp','Logs','UserSettings','GitHubUpload'):forbidden.append(relative.as_posix())
    oversized=[p.relative_to(REPO).as_posix() for p in upload if p.stat().st_size>=100*1024*1024]
    after=git('rev-parse','HEAD')
    report={'utc':datetime.now(timezone.utc).isoformat(),'version':'0.2.0','art':'Coral V6','repository':str(REPO),'remote':remote,
            'passed':not failures and not forbidden and not oversized and before==after,'head_unchanged':before==after,'committed':False,'pushed':False,
            'verified_copies':len(copied),'upload_files':len(upload),'upload_bytes':sum(p.stat().st_size for p in upload),
            'mismatches':failures,'forbidden':forbidden,'oversized':oversized,'release_files':releases,'backup':backup['backup']}
    (ROOT/'GITHUB_UPDATE_VERIFICATION.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    (REPO/'VERSION.json').write_text(json.dumps({'version':'0.2.0','unity':'6000.1.9f1','art':'Coral V6','updated_utc':report['utc'],'releases':releases},ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(report,ensure_ascii=False,indent=2))
    return 0 if report['passed'] else 1

if __name__=='__main__':raise SystemExit(main())
