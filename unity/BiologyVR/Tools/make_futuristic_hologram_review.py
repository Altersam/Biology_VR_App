import json
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parent.parent
reports = root / 'Assets/BiologyVR/ArteryJourney/Reports/Polish'
dest = reports / 'FuturisticHolograms'
authoring = json.loads((dest / 'Authoring.json').read_text(encoding='utf-8-sig'))
controller = (reports / 'XRControllers/Input_Lifecycle.md').read_text(encoding='utf-8-sig')
xr = (reports / 'VR_INPUT_PLAYTHROUGH_V7.md').read_text(encoding='utf-8-sig')
if 'Status: PASS' not in controller or 'Status: FULL SIMULATOR PASS' not in xr:
    raise SystemExit('XR lifecycle and sequential gameplay verification required')
frames = [('XRControllers/Persistent_Aiming_Guides.png', 'HOLOGRAPHIC UI / Bold text, clear centre, upper-right vitals'),
          ('ActiveInstruments/08_Holographic_Theory_Actual_XR_Scroll.png', 'FULL REFERENCE / actual XR thumbstick scroll'),
          ('V7Input/Scene12_Spatial_Fibrin_Complete.png', 'HEMOSTASIS / same holographic containers during gameplay'),
          ('V7Input/Scene16_Actual_Homeostasis_Checks.png', 'FINAL SCENE / preserved UI and persistent blood flow')]
sheet = Image.new('RGB', (1440, 980), (10, 28, 38))
draw = ImageDraw.Draw(sheet)
font = ImageFont.truetype('arial.ttf', 20)
for i, (name, caption) in enumerate(frames):
    with Image.open(reports / name) as image:
        image = image.convert('RGB')
        image.thumbnail((720, 450))
        x, y = i % 2 * 720, i // 2 * 490
        sheet.paste(image, (x, y + 40))
        draw.text((x + 12, y + 8), caption, font=font, fill=(155, 239, 255))
sheet.save(dest / 'Gameplay_Review.jpg', quality=94)
(dest / 'Capture.json').write_text(json.dumps({
    'utc': datetime.now(timezone.utc).isoformat(),
    'authoringUtc': authoring['utc'],
    'method': 'Actual Unity XR InputActions and camera renders',
    'xrSequential': 'FULL SIMULATOR PASS',
    'controllerLifecycle': 'PASS', 'hardwareVerified': False,
    'frames': [name for name, _ in frames],
}, indent=2), encoding='utf-8')
print(dest / 'Gameplay_Review.jpg')
