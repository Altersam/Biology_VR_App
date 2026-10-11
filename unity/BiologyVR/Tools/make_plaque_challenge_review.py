import json
import shutil
from datetime import datetime, timezone
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root=Path(__file__).resolve().parent.parent
reports=root/'Assets/BiologyVR/ArteryJourney/Reports';source=reports/'Polish/PlaqueChallenge';dest=reports/'PlaqueChallenge'
if not dest.is_dir():raise SystemExit('Authoring report required')
pc=json.loads((reports/'Polish/FlowPickup/Plaque_Challenge_PC.json').read_text(encoding='utf-8-sig'))
if not pc['passed']:raise SystemExit('Actual-input plaque check has not passed')
frames=[('01_Initial_Growing_Mass.png','GROWING MASS / timed challenge'),('02_Occlusion_And_Monitor.png','OCCLUSION / local retry'),('03_Actual_Fragmentation_And_Shards.png','ACTUAL INPUT / fragments + impact'),('04_Cleared_Lumen_And_Recovery.png','CLEARED LUMEN / recovered vitals')]
sheet=Image.new('RGB',(1440,980),(10,28,38));draw=ImageDraw.Draw(sheet);font=ImageFont.truetype('arial.ttf',22)
for i,(name,caption) in enumerate(frames):
    shutil.copy2(source/name,dest/name)
    with Image.open(dest/name) as image:
        image=image.convert('RGB');image.thumbnail((720,450));x=i%2*720;y=i//2*490;sheet.paste(image,(x,y+40));draw.text((x+14,y+8),caption,font=font,fill=(155,239,255))
if (source/'05_Actual_XR_Cleared_Lumen.png').is_file():shutil.copy2(source/'05_Actual_XR_Cleared_Lumen.png',dest/'05_Actual_XR_Cleared_Lumen.png')
sheet.save(dest/'Plaque_Gameplay_Review.jpg',quality=94)
(dest/'Capture.json').write_text(json.dumps({'utc':datetime.now(timezone.utc).isoformat(),'pcVerifiedUtc':pc['utc'],'frames':[name for name,_ in frames],'method':'Actual Unity input/gameplay captures; no concept overlays','hardwareVerified':False},indent=2),encoding='utf-8')
print(str(dest/'Plaque_Gameplay_Review.jpg'))
