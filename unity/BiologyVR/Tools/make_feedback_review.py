"""Generate a contact sheet using only real feedback-rework Unity captures."""
import json
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parent.parent
folder = root / 'Assets/BiologyVR/ArteryJourney/Reports/Polish/PlayerFeedbackRework'
if root.drive.upper() != 'Z:' or not folder.is_dir():
    raise SystemExit('Existing capture directory on Z: required')
frames = [
    ('01_Bound_Oxygen_RBC.png', 'RBC / carrier-bound O2'),
    ('02_Hb_With_Bound_Iron.png', 'Hb / bound heme and iron'),
    ('03_Critical_Vascular_Field.png', 'Dynamic vascular field / critical state'),
    ('05_Context_Help_And_Readable_Screens.png', 'Readable panels / contextual help'),
    ('07_XR_Bound_Cargo_And_Panels.png', 'Actual XR field capture and Scanner'),
    ('08_XR_Reverse_Hand_Pull.png', 'Actual XR reverse hand pull'),
    ('09_Plaque_Released_Flow.png', 'Plaque clearance / restored blood flow'),
    ('10_All_Evasive_Emboli_Deposited.png', 'All six evasive emboli deposited'),
]
sheet = Image.new('RGB', (1312, 1800), '#10242c')
draw = ImageDraw.Draw(sheet)
draw.text((18, 12), 'Biology VR 0.7.0 / actual Unity gameplay captures', fill='white')
for i, (name, caption) in enumerate(frames):
    with Image.open(folder / name) as source:
        preview = source.convert('RGB')
        preview.thumbnail((640, 400), Image.Resampling.LANCZOS)
        x, y = 12 + i % 2 * 656, 48 + i // 2 * 438
        sheet.paste(preview, (x, y))
        draw.text((x, y + 408), caption, fill='#abf5f3')
sheet.save(folder / 'Feedback_Gameplay_Review.jpg', quality=90)
(folder / 'Feedback_Capture_Review.json').write_text(json.dumps({
    'version': '0.7.0', 'method': 'Only actual Unity PC/XR captures, resized into a contact sheet',
    'sources': [name for name, caption in frames],
}, indent=2), encoding='utf-8')
print(folder / 'Feedback_Gameplay_Review.jpg')
