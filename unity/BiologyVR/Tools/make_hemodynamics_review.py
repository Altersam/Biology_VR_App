import json
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parent.parent
reports = root / 'Assets/BiologyVR/ArteryJourney/Reports/Polish'
dest = reports / 'HemodynamicsExperiment'
pc = json.loads((reports / 'FlowPickup/Hemodynamics_PC_Input.json').read_text(encoding='utf-8-sig'))
xr = (reports / 'VR_INPUT_PLAYTHROUGH_V7.md').read_text(encoding='utf-8-sig')
if not pc['passed'] or 'Status: FULL SIMULATOR PASS' not in xr or 'All three early experiments complete' not in xr:
    raise SystemExit('Current PC and sequential XR experiment verification required')
frames = [('01_Narrow_In_Vessel_AR.png', 'EXPERIMENT 1 / measure, adjust and verify'),
          ('02_Wide_AR_And_Discovered_Relation.png', 'EXPERIMENT 2 / wide vessel and lower resistance'),
          ('03_Outside_Cause_Conclusion.png', 'EXPERIMENT 3 / normal diameter, reduced supply'),
          ('04_Late_Independent_Diagnosis.png', 'LATE EPISODE / evidence-based independent diagnosis'),
          ('05_XR_Narrow_Experiment.png', 'XR / held TONUS control inside the artery'),
          ('06_XR_Late_Diagnosis.png', 'XR / measured data and physical hypothesis cards')]
sheet = Image.new('RGB', (1440, 1470), (10, 28, 38))
draw = ImageDraw.Draw(sheet)
font = ImageFont.truetype('arial.ttf', 20)
for i, (name, caption) in enumerate(frames):
    with Image.open(dest / name) as image:
        image = image.convert('RGB')
        image.thumbnail((720, 450))
        x, y = i % 2 * 720, i // 2 * 490
        sheet.paste(image, (x, y + 40))
        draw.text((x + 12, y + 8), caption, font=font, fill=(155, 239, 255))
sheet.save(dest / 'Gameplay_Review.jpg', quality=94)
(dest / 'Capture.json').write_text(json.dumps({
    'utc': datetime.now(timezone.utc).isoformat(), 'pcVerifiedUtc': pc['utc'],
    'xrSequential': 'FULL SIMULATOR PASS',
    'method': 'Actual Unity mouse, keyboard, XR InputActions/XRI and camera renders',
    'frames': [name for name, _ in frames], 'hardwareVerified': False,
}, indent=2), encoding='utf-8')
print(dest / 'Gameplay_Review.jpg')
