import hashlib
import json
import shutil
from pathlib import Path

root=Path(__file__).resolve().parent.parent
dest=root/'Assets/BiologyVR/ArteryJourney/Reports/Polish/BioWorldV6/Backup'
if not dest.is_dir():raise SystemExit('Backup directory must exist')
files={
 'Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity':'ArteryNarrativeVR_before_CoralV6.unity',
 'Assets/BiologyVR/ArteryRoute/Shaders/ArteryVisualV2.shader':'ArteryVisualV2.shader.txt',
 'Assets/BiologyVR/ArteryRoute/Runtime/Journey/ArteryVisualEnvironmentV8.cs':'ArteryVisualEnvironmentV8.cs.txt',
 'Assets/BiologyVR/ArteryRoute/Runtime/Journey/JourneyArteryPresentation.cs':'JourneyArteryPresentation.cs.txt',
}
checks=[]
for source,name in files.items():
 target=dest/name
 if target.exists():raise SystemExit('Backup exists: '+name)
 shutil.copyfile(root/source,target)
 same=hashlib.sha256((root/source).read_bytes()).hexdigest()==hashlib.sha256(target.read_bytes()).hexdigest()
 checks.append({'file':name,'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'verified':same})
(dest/'Verification.json').write_text(json.dumps(checks,indent=2),encoding='utf-8')
print(json.dumps(checks,indent=2))
raise SystemExit(0 if all(c['verified'] for c in checks) else 1)
