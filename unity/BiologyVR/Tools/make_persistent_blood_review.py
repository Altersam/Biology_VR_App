import json
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parent.parent
reports = root / 'Assets/BiologyVR/ArteryJourney/Reports/Polish'
desktop = json.loads((reports / 'FlowPickup/All_Scenes_Persistent_Blood.json').read_text(encoding='utf-8-sig'))
mobile = json.loads((reports / 'FlowPickup/Mobile_All_Scenes_Persistent_Blood.json').read_text(encoding='utf-8-sig'))
if not desktop['passed'] or not mobile['passed']:
    raise SystemExit('Successful desktop and mobile-pool scene checks required')
dest = reports / 'PersistentBlood'
frames = [('Scene03_Dense_Flow.png', 'SCENE 03 / initial saturated circulation'),
          ('Scene11_Dense_Flow.png', 'SCENE 11 / flow continues at the wound'),
          ('Scene15_Dense_Flow.png', 'SCENE 15 / same circulating composition'),
          ('Scene16_Dense_Flow.png', 'SCENE 16 / persistent final-stage bloodstream')]
sheet = Image.new('RGB', (1440, 980), (10, 28, 38))
draw = ImageDraw.Draw(sheet)
font = ImageFont.truetype('arial.ttf', 21)
for i, (name, caption) in enumerate(frames):
    with Image.open(dest / name) as image:
        image = image.convert('RGB')
        image.thumbnail((720, 450))
        x, y = i % 2 * 720, i // 2 * 490
        sheet.paste(image, (x, y + 40))
        draw.text((x + 12, y + 8), caption, font=font, fill=(155, 239, 255))
sheet.save(dest / 'All_Scenes_Review.jpg', quality=94)
(dest / 'Verification.json').write_text(json.dumps({
    'utc': datetime.now(timezone.utc).isoformat(),
    'desktopUtc': desktop['utc'], 'mobilePoolUtc': mobile['utc'],
    'desktopPassed': True, 'mobilePoolPassed': True,
    'method': 'Actual Unity camera renders and mouse-input scene fixtures; mobile pool budget simulated in Editor',
    'frames': [name for name, _ in frames], 'hardwareVerified': False,
}, indent=2), encoding='utf-8')
print(dest / 'All_Scenes_Review.jpg')
