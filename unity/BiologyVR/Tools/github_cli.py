"""Portable official gh on Z; existing Git credential used only in child-process memory."""
import hashlib
import os
import subprocess
import sys
import urllib.request
import zipfile
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent
REPO=ROOT.parent/'Biology_VR_App'
TOOLS=ROOT/'Builds/Utilities/GitHubCLI'
EXE=TOOLS/'bin/gh.exe'
if not EXE.is_file():
    if not (ROOT/'Builds').is_dir():raise SystemExit('Existing Builds parent required')
    TOOLS.mkdir(parents=True,exist_ok=True)
    archive=TOOLS/'gh.zip'
    urllib.request.urlretrieve('https://github.com/cli/cli/releases/download/v2.102.0/gh_2.102.0_windows_amd64.zip',archive)
    if hashlib.sha256(archive.read_bytes()).hexdigest()!='ae64e556ecc240b200f7eba60d550e4bb60d78e860e69dd88c449405b86067f4':raise SystemExit('Official gh checksum mismatch')
    with zipfile.ZipFile(archive) as package:
        for name in package.namelist():
            if Path(name).is_absolute() or '..' in Path(name).parts:raise SystemExit('Invalid archive path')
        package.extractall(TOOLS)
env=os.environ.copy()
if not env.get('GH_TOKEN') and not env.get('GITHUB_TOKEN'):
    credential=subprocess.run(['git','credential','fill'],cwd=REPO,input='protocol=https\nhost=github.com\n\n',text=True,capture_output=True)
    if credential.returncode==0:
        values=dict(line.split('=',1) for line in credential.stdout.splitlines() if '=' in line)
        if values.get('password'):env['GH_TOKEN']=values['password']
env['GH_PROMPT_DISABLED']='1'
raise SystemExit(subprocess.run([str(EXE),*sys.argv[1:]],cwd=REPO,env=env).returncode)
