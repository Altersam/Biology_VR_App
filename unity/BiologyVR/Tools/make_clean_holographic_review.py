"""Publish actual input captures for the first-pass review, without staging gameplay."""
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root=Path(__file__).resolve().parent.parent
reports=root/'Assets/BiologyVR/ArteryJourney/Reports'
dest=reports/'CleanHolographicFirstPass'
source=reports/'Polish/ActiveInstruments'
if not dest.is_dir():raise SystemExit('Run the scene first-pass authoring menu first')
pc=json.loads((reports/'Polish/FlowPickup/NativeCursor_PC_Input.json').read_text(encoding='utf-8-sig'))
if not pc['passed']:raise SystemExit('Actual input validation has not passed')
frames=[('05_Holographic_Screens_Default.png','01_Overview.png','CLEAN OVERVIEW / live scene'),
        ('06_Holographic_Theory_Scrolled.png','02_Full_Reference.png','REFERENCE / actual scroll'),
        ('01_Scanner_Actual_Input.png','03_Scanner_Working.png','MEDICAL SCANNER / actual F input'),
        ('03_Capture_Hook_Actual_Input.png','04_BioTool_Capture.png','BIOTOOL / hook + tether'),
        ('04_Tonus_Actual_Input.png','05_BioTool_Tonus.png','BIOTOOL / held field'),
        ('07_Rapid_Viral_Assault_PC.png','06_BioTool_Impulse.png','BIOTOOL / actual impulse input')]
sheet=Image.new('RGB',(1440,1470),(10,28,38));draw=ImageDraw.Draw(sheet);font=ImageFont.truetype('arial.ttf',22)
for i,(original,filename,caption) in enumerate(frames):
    shutil.copy2(source/original,dest/filename)
    with Image.open(dest/filename) as image:
        image=image.convert('RGB');image.thumbnail((720,450));x=(i%2)*720;y=(i//2)*490
        sheet.paste(image,(x,y+40));draw.text((x+14,y+8),caption,font=font,fill=(155,239,255))
sheet.save(dest/'FirstPass_Review.jpg',quality=94)
(dest/'Capture.json').write_text(json.dumps({'utc':datetime.now(timezone.utc).isoformat(),'pcInputVerifiedUtc':pc['utc'],'method':'Actual keyboard/mouse/XRI input captures, not a mockup','frames':[name for _,name,_ in frames],'review':'FirstPass_Review.jpg','hardwareVerified':False,'phase':'FIRST PASS — stop for assessment'},indent=2),encoding='utf-8')
print(str(dest/'FirstPass_Review.jpg'))
