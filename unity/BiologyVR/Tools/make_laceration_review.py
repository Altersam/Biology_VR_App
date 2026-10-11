import json
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parent.parent
reports = root / 'Assets/BiologyVR/ArteryJourney/Reports'
dest = reports / 'LacerationHemostasis'
pc = json.loads((reports / 'Polish/FlowPickup/Laceration_PC_Input.json').read_text(encoding='utf-8-sig'))
xr = (reports / 'Polish/VR_INPUT_PLAYTHROUGH_V7.md').read_text(encoding='utf-8-sig')
if not pc['passed'] or 'Status: FULL SIMULATOR PASS' not in xr or 'Eleven automatically adherent platelets' not in xr:
    raise SystemExit('Fresh PC and corrected sequential XR verification required')
frames = [
    ('05_Overall_Main_Stream_Before_Tear.png', 'MAIN STREAM / approach to the defect'),
    ('06_Tear_And_Shared_Pool_Diversion.png', 'TEAR / existing pooled cells diverted outward'),
    ('07_Continuing_Downstream_Blood.png', 'DOWNSTREAM / blood continues through the artery'),
    ('02_Platelet_Layers_From_Flow.png', 'CAPTURE / automatic adhesion and primary plug'),
    ('03_Activation_Sequence_Game.png', 'STABILIZATION / activation, aggregation, fibrin'),
    ('08_Stable_Fibrin_Endothelium_Not_Yet_Healed.png', 'STABLE HEMOSTASIS / endothelial repair follows'),
]
sheet = Image.new('RGB', (1440, 1470), (10, 28, 38))
draw = ImageDraw.Draw(sheet)
font = ImageFont.truetype('arial.ttf', 20)
for i, (name, caption) in enumerate(frames):
    with Image.open(reports / 'Polish/LacerationHemostasis' / name) as image:
        image = image.convert('RGB')
        image.thumbnail((720, 450))
        x, y = i % 2 * 720, i // 2 * 490
        sheet.paste(image, (x, y + 40))
        draw.text((x + 12, y + 8), caption, font=font, fill=(155, 239, 255))
sheet.save(dest / 'Gameplay_Review.jpg', quality=94)
(dest / 'Capture.json').write_text(json.dumps({
    'utc': datetime.now(timezone.utc).isoformat(),
    'pcVerifiedUtc': pc['utc'],
    'xrSequential': 'FULL SIMULATOR PASS',
    'method': 'Actual Unity mouse/keyboard/controller gameplay; HUD hidden synchronously only for clear-view captures',
    'frames': [name for name, _ in frames],
    'hardwareVerified': False,
}, indent=2), encoding='utf-8')
print(dest / 'Gameplay_Review.jpg')
