import json
import shutil
from datetime import datetime, timezone
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root=Path(__file__).resolve().parent.parent
reports=root/'Assets/BiologyVR/ArteryJourney/Reports'
dest=reports/'CleanHolographicSecondPass'
source=reports/'Polish/ActiveInstruments'
if not dest.is_dir():raise SystemExit('Second-pass authoring report is missing')
pc=json.loads((reports/'Polish/FlowPickup/NativeCursor_PC_Input.json').read_text(encoding='utf-8-sig'))
authored=json.loads((dest/'SecondPass.json').read_text(encoding='utf-8-sig'))
if not pc['passed'] or pc['utc']<authored['utc']:raise SystemExit('Fresh second-pass input validation required')
frames=[('05_Holographic_Screens_Default.png','01_Overview.png','READABLE HUD / actual scene'),
        ('06_Holographic_Theory_Scrolled.png','02_Full_Reference.png','CLIPPED REFERENCE / actual scroll'),
        ('01_Scanner_Actual_Input.png','03_Scanner_Working.png','SCANNER / diagnostic sweep'),
        ('03_Capture_Hook_Actual_Input.png','04_BioTool_Capture.png','CAPTURE / hook + tether'),
        ('04_Tonus_Actual_Input.png','05_BioTool_Tonus.png','TONUS / held field'),
        ('07_Rapid_Viral_Assault_PC.png','06_BioTool_Impulse.png','IMPULSE / active crown')]
font=ImageFont.truetype('arial.ttf',22)
sheet=Image.new('RGB',(1440,1470),(10,28,38));draw=ImageDraw.Draw(sheet)
for i,(original,name,caption) in enumerate(frames):
    shutil.copy2(source/original,dest/name)
    with Image.open(dest/name) as image:
        image=image.convert('RGB');image.thumbnail((720,450));x=(i%2)*720;y=(i//2)*490
        sheet.paste(image,(x,y+40));draw.text((x+14,y+8),caption,font=font,fill=(155,239,255))
sheet.save(dest/'SecondPass_Review.jpg',quality=94)
comparison=Image.new('RGB',(1920,640),(10,28,38));draw=ImageDraw.Draw(comparison)
for i,(path,title) in enumerate([(reports/'CleanHolographicFirstPass/01_Overview.png','FIRST PASS'),(dest/'01_Overview.png','SECOND PASS / readability')]):
    with Image.open(path) as image:
        image=image.convert('RGB');image.thumbnail((960,600));comparison.paste(image,(i*960,40))
        draw.text((i*960+18,8),title,font=font,fill=(155,239,255))
comparison.save(dest/'Before_After.jpg',quality=94)
(dest/'Capture.json').write_text(json.dumps({'utc':datetime.now(timezone.utc).isoformat(),'pcInputVerifiedUtc':pc['utc'],'method':'Actual input captures from the scene; no mockups','frames':[name for _,name,_ in frames],'comparison':'Before_After.jpg','hardwareVerified':False},indent=2),encoding='utf-8')
print(str(dest/'Before_After.jpg'))
