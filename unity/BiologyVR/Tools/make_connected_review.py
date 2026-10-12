"""Contact sheet from actual Unity captures; all output remains on Z:."""
import json
from datetime import datetime, timezone
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parent.parent
folder = root / 'Assets/BiologyVR/ArteryJourney/Reports/Polish/LivingOrganismJourney'
if root.drive.upper() != 'Z:' or not folder.is_dir():
    raise SystemExit('Existing capture folder on Z: required')
captions = [
    ('01_Live_Blood_Background.png', '01 / Ordinary living bloodstream'),
    ('02_Selected_Pool_Cell_Research.png', '02 / Researching a selected pool instance'),
    ('03_Deep_Scan_RBC_Contents.png', '03 / Nested RBC content analysis'),
    ('04_First_Flowing_Virus.png', '04 / One first viral particle'),
    ('05_Scanner_Activated_Phagocyte.png', '05 / Scanner-activated phagocyte'),
    ('06_BioTool_Holds_Phagocyte.png', '06 / Capture field holds the real cell'),
    ('07_Pulled_Cell_And_Trajectory.png', '07 / Pull charge and advection preview'),
    ('08_Gradual_Phagocytosis.png', '08 / Gradual engulfment in progress'),
    ('09_Later_Wave_Within_Blood.png', '09 / Later wave inside circulating blood'),
    ('14_XR_Captured_Pulled_Cell.png', '10 / Actual XR hand-pull input'),
]
sheet = Image.new('RGB', (1312, 2240), '#132026')
draw = ImageDraw.Draw(sheet)
draw.text((18, 12), 'Biology VR / Connected living blood / actual Unity Game-view captures', fill='white')
sources = []
for i, (name, caption) in enumerate(captions):
    source = folder / name
    if not source.is_file():
        raise SystemExit('Required real capture missing: ' + name)
    with Image.open(source) as image:
        preview = image.convert('RGB')
        preview.thumbnail((640, 400), Image.Resampling.LANCZOS)
        x, y = 12 + (i % 2) * 656, 48 + (i // 2) * 438
        sheet.paste(preview, (x, y))
        draw.text((x, y + 408), caption, fill='#a8ebed')
        sources.append({'file': name, 'size': list(image.size), 'caption': caption})
sheet.save(folder / 'Connected_Gameplay_Review.jpg', quality=90)
(folder / 'Capture_Review.json').write_text(json.dumps({
    'utc': datetime.now(timezone.utc).isoformat(), 'sources': sources,
    'method': 'Only actual Unity PC/XR captures, resized into a contact sheet; no concept imagery',
}, indent=2), encoding='utf-8')
print(folder / 'Connected_Gameplay_Review.jpg')
