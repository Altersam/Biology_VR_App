"""Contact sheet of actual input captures; no scene rendering or gameplay staging."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root=Path(__file__).resolve().parent.parent
folder=root/'Assets/BiologyVR/ArteryJourney/Reports/Polish/ActiveInstruments'
if not folder.is_dir():raise SystemExit('Existing capture folder required')
frames=[('01_Scanner_Actual_Input.png','SCANNER / actual F input'),
        ('02_Impulse_Actual_XR_Input.png','ORBITAL IMPULSE / actual XR input'),
        ('03_Capture_Hook_Actual_Input.png','CAPTURE / hook + tether'),
        ('04_Tonus_Actual_Input.png','TONUS / held field'),
        ('05_Holographic_Screens_Default.png','HOLOGRAMS / theory open by default'),
        ('07_Rapid_Viral_Assault_PC.png','24 VIRIONS / rapid-fire wave')]
sheet=Image.new('RGB',(1440,1470),(8,20,30));draw=ImageDraw.Draw(sheet)
font=ImageFont.truetype('arial.ttf',22)
for i,(filename,caption) in enumerate(frames):
    with Image.open(folder/filename) as source:
        source=source.convert('RGB');source.thumbnail((720,450))
        x=(i%2)*720;y=(i//2)*490
        sheet.paste(source,(x,y+40));draw.text((x+14,y+8),caption,font=font,fill=(150,231,250))
sheet.save(folder/'Active_Play_Review.jpg',quality=94)
print('Saved review: '+str(folder/'Active_Play_Review.jpg'))
