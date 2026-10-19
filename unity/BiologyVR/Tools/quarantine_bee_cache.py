"""Move only regenerable Bee compiler state aside, retaining it on Z:."""
from datetime import datetime
from pathlib import Path

root = Path(__file__).resolve().parent.parent
source = root / 'Library/Bee'
if root.drive.upper() != 'Z:' or not (root / 'Builds').is_dir():
    raise SystemExit('Existing build parent on Z: required')
if not source.is_dir():
    raise SystemExit('Bee compiler cache already absent')
destination = root / 'Builds' / ('BeeCompilerCache_' + datetime.now().strftime('%Y%m%d_%H%M%S'))
source.rename(destination)
print('Preserved Bee compiler cache: ' + str(destination))
