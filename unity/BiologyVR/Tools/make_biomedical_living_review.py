import json
import shutil
from datetime import datetime, timezone
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root=Path(__file__).resolve().parent.parent
reports=root/'Assets/BiologyVR/ArteryJourney/Reports'
dest=reports/'BiomedicalLivingPass'
source=reports/'Polish/BiomedicalLivingPass'
if not dest.is_dir():raise SystemExit('Authoring reports missing')
pc=json.loads((reports/'Polish/FlowPickup/NativeCursor_PC_Input.json').read_text(encoding='utf-8-sig'))
continuous=json.loads((reports/'Polish/FlowPickup/Continuous_Random_Flow.json').read_text(encoding='utf-8-sig'))
if not pc['passed'] or not continuous['passed']:raise SystemExit('Gameplay/continuous validation not passed')
frames=[('Stage1/01_Scanner_In_Hand.png','01_Scanner_In_Hand.png','SCANNER / controller-mounted'),
        ('Stage1/02_BioTool_In_Hand.png','02_BioTool_In_Hand.png','BIOTOOL / controller-mounted'),
        ('Stage1/03_Devices_And_UI.png','03_Devices_And_UI.png','DEVICES + UI / actual scene'),
        ('Stage2/04_Living_Bloodstream_With_UI.png','04_Living_Bloodstream_With_UI.png','LIVING BLOOD / main + background'),
        ('Stage2/05_Flow_Into_Depth.png','05_Flow_Into_Depth.png','FORWARD / continuous stream'),
        ('Stage2/06_Continuous_Backward_View.png','06_Continuous_Backward_View.png','BACKWARD / continuous stream')]
sheet=Image.new('RGB',(1440,1470),(10,28,38));draw=ImageDraw.Draw(sheet);font=ImageFont.truetype('arial.ttf',22)
for i,(original,name,caption) in enumerate(frames):
    shutil.copy2(source/original,dest/name)
    with Image.open(dest/name) as image:
        image=image.convert('RGB');image.thumbnail((720,450));x=(i%2)*720;y=(i//2)*490;sheet.paste(image,(x,y+40));draw.text((x+14,y+8),caption,font=font,fill=(155,239,255))
sheet.save(dest/'LivingPass_Review.jpg',quality=94)
(dest/'Capture.json').write_text(json.dumps({'utc':datetime.now(timezone.utc).isoformat(),'pcVerifiedUtc':pc['utc'],'continuousVerifiedUtc':continuous['utc'],'frames':[name for _,name,_ in frames],'method':'Actual Unity controller/input captures, not concept images','hardwareVerified':False},indent=2),encoding='utf-8')
print(str(dest/'LivingPass_Review.jpg'))
