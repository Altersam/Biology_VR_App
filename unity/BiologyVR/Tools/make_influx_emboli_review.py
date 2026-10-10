import json
import shutil
from datetime import datetime, timezone
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root=Path(__file__).resolve().parent.parent
reports=root/'Assets/BiologyVR/ArteryJourney/Reports'
dest=reports/'InfluxAndEmboli'
if not dest.is_dir():raise SystemExit('Authoring report required')
pc=json.loads((reports/'Polish/FlowPickup/NativeCursor_PC_Input.json').read_text(encoding='utf-8-sig'))
plaque=json.loads((reports/'Polish/FlowPickup/Plaque_Challenge_PC.json').read_text(encoding='utf-8-sig'))
xr=(reports/'Polish/VR_INPUT_PLAYTHROUGH_V7.md').read_text(encoding='utf-8-sig')
if not pc['passed'] or not plaque['passed'] or 'Status: FULL SIMULATOR PASS' not in xr:raise SystemExit('Input verification required')
frames=[('Polish/PlaqueChallenge/06_Lipids_Entering_From_Blood.png','01_Flow_Fed_Lipids.png','LIPIDS / arriving from bloodstream'),
        ('Polish/PlaqueChallenge/03_Actual_Fragmentation_And_Shards.png','02_Lipid_Fragmentation.png','IMPULSE / actual hit + fragments'),
        ('Polish/InfluxAndEmboli/01_Six_Bubbles_Actual_PC_Start.png','03_Six_Gas_Bubbles.png','SIX GAS BUBBLES / independent targets'),
        ('Polish/ActiveInstruments/03_Capture_Hook_Actual_Input.png','04_Capture_Tether_And_Lock.png','CAPTURE / hook + tether + lock'),
        ('Polish/ActiveInstruments/01_Scanner_Actual_Input.png','05_Scanner_Actual_Input.png','SCANNER / real diagnostic input'),
        ('Polish/InfluxAndEmboli/03_All_Six_Neutralized.png','06_All_Six_Completed.png','SIX DEPOSITS / preserved progress')]
sheet=Image.new('RGB',(1440,1470),(10,28,38));draw=ImageDraw.Draw(sheet);font=ImageFont.truetype('arial.ttf',22)
for i,(source,name,caption) in enumerate(frames):
    shutil.copy2(reports/source,dest/name)
    with Image.open(dest/name) as image:
        image=image.convert('RGB');image.thumbnail((720,450));x=i%2*720;y=i//2*490;sheet.paste(image,(x,y+40));draw.text((x+14,y+8),caption,font=font,fill=(155,239,255))
sheet.save(dest/'Gameplay_Review.jpg',quality=94)
(dest/'Capture.json').write_text(json.dumps({'utc':datetime.now(timezone.utc).isoformat(),'pcVerifiedUtc':pc['utc'],'plaqueVerifiedUtc':plaque['utc'],'xrSequential':'FULL SIMULATOR PASS','method':'Actual Unity mouse/keyboard/controller gameplay captures','frames':[name for _,name,_ in frames],'hardwareVerified':False},indent=2),encoding='utf-8')
print(str(dest/'Gameplay_Review.jpg'))
