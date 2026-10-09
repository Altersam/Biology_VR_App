from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root=Path(__file__).resolve().parent.parent
folder=root/'Assets/BiologyVR/ArteryJourney/Reports/Polish/BioWorldV6'
font=ImageFont.truetype(str(root/'Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular.ttf'),25)
frames=[
 ('01_Player_Overview.png','1. Общий вид · реальная стартовая камера'),
 ('02_Wall_Close.png','2. Стенка крупно · мягкий normal/AO'),
 ('03_Tunnel_Depth.png','3. Вид вдаль · кораллово-красная глубина'),
 ('04_RBC_Contrast.png','4. Контраст эритроцитов и других клеток'),
 ('07_Scanner_Actual_XR_Input.png','5. Scanner · реальные XR InputActions'),
 ('06_BioTool_Actual_PC_Input.png','6. BioTool · удерживаемое поле на ПК'),
]
sheet=Image.new('RGB',(1920,1770),(17,29,36));draw=ImageDraw.Draw(sheet)
for i,(name,caption) in enumerate(frames):
 x=(i%2)*960;y=(i//2)*590
 draw.text((x+18,y+12),caption,font=font,fill=(229,240,242))
 image=Image.open(folder/name).convert('RGB');image.thumbnail((960,540))
 sheet.paste(image,(x+(960-image.width)//2,y+48))
sheet.save(folder/'FirstPass_ContactSheet.jpg',quality=92)
print(folder/'FirstPass_ContactSheet.jpg')
