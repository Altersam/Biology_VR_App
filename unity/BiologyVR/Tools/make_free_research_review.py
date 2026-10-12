import json
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parent.parent
reports = root / 'Assets/BiologyVR/ArteryJourney/Reports/Polish'
dest = reports / 'FreeBloodResearch'
pc = json.loads((reports / 'FlowPickup/Free_Blood_Research_PC_Input.json').read_text(encoding='utf-8-sig'))
xr = (reports / 'VR_INPUT_PLAYTHROUGH_V7.md').read_text(encoding='utf-8-sig')
if not pc['passed'] or not any('twenty-four permutations passed' in line for line in pc['checks']) or 'Status: FULL SIMULATOR PASS' not in xr or 'Free first-stage research completes' not in xr:
    raise SystemExit('All-order PC and current sequential XR checks required')
frames = [('01_Leukocyte_First.png', 'FREE RESEARCH / leukocyte selected first'),
          ('02_Molecular_Details_Before_RBC.png', 'MOLECULAR DETAILS / gases, Hb and heme independent'),
          ('03_Four_Components_Complete.png', 'FOUR REQUIRED COMPONENTS / no automatic transition'),
          ('04_XR_Leukocyte_First.png', 'XR / existing grip and contextual object theory'),
          ('05_XR_Plasma_Details.png', 'XR / plasma proteins and electrolytes, actual scroll'),
          ('06_XR_Research_Complete.png', 'XR / preserved journal and free repeated inspection')]
sheet = Image.new('RGB', (1440, 1470), (10, 28, 38))
draw = ImageDraw.Draw(sheet)
font = ImageFont.truetype('arial.ttf', 20)
for i, (name, caption) in enumerate(frames):
    with Image.open(dest / name) as image:
        image = image.convert('RGB');image.thumbnail((720, 450))
        x, y = i % 2 * 720, i // 2 * 490
        sheet.paste(image, (x, y + 40));draw.text((x + 12, y + 8), caption, font=font, fill=(155, 239, 255))
sheet.save(dest / 'Gameplay_Review.jpg', quality=94)
(dest / 'Capture.json').write_text(json.dumps({
    'utc': datetime.now(timezone.utc).isoformat(), 'pcUtc': pc['utc'],
    'pcAll24Orders': True, 'xrSequential': 'FULL SIMULATOR PASS',
    'method': 'Actual mouse, keyboard, XR InputActions/XRI and Unity camera renders',
    'frames': [name for name, _ in frames], 'hardwareVerified': False,
}, indent=2), encoding='utf-8')
print(dest / 'Gameplay_Review.jpg')
