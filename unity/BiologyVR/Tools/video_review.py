"""Extract a timestamped overview of the supplied QA recording."""
import argparse
import json
from pathlib import Path

import cv2
from PIL import Image, ImageDraw

parser = argparse.ArgumentParser()
parser.add_argument("video")
parser.add_argument("output")
parser.add_argument("--start", type=float, default=0)
parser.add_argument("--end", type=float)
parser.add_argument("--samples", type=int, default=16)
args = parser.parse_args()
capture = cv2.VideoCapture(args.video)
if not capture.isOpened():
    raise SystemExit("Cannot open video")
fps = capture.get(cv2.CAP_PROP_FPS)
count = capture.get(cv2.CAP_PROP_FRAME_COUNT)
duration = count / fps
print(json.dumps({"fps": fps, "frames": count, "duration_seconds": duration,
                  "width": capture.get(cv2.CAP_PROP_FRAME_WIDTH),
                  "height": capture.get(cv2.CAP_PROP_FRAME_HEIGHT)}, indent=2))
end = min(args.end if args.end is not None else duration, duration - 1 / fps)
columns = 4
width, height = 480, 294
sheet = Image.new("RGB", (columns * width, ((args.samples + 3) // 4) * height), "#18202b")
draw = ImageDraw.Draw(sheet)
for index in range(args.samples):
    time = args.start + (end - args.start) * index / max(1, args.samples - 1)
    capture.set(cv2.CAP_PROP_POS_MSEC, time * 1000)
    ok, frame = capture.read()
    if not ok:
        continue
    image = Image.fromarray(cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))
    image.thumbnail((width, height - 24))
    x, y = index % columns * width, index // columns * height
    sheet.paste(image, (x, y))
    draw.text((x + 6, y + height - 21), f"{int(time // 60):02}:{time % 60:05.2f}", fill="white")
capture.release()
output = Path(args.output)
if not output.parent.is_dir():
    raise SystemExit("Output parent must already exist")
sheet.save(output)
print(str(output))
