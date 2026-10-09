"""Package a NEW repository: Unity source + WebGL, separate from the concept website."""
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DEST = ROOT.parent / "Biology_VR_App"


def main():
    overlay = ROOT / "GitHubUpload"
    if not (overlay / "play/index.html").is_file():
        raise SystemExit("Prepared WebGL upload folder is missing")
    if not DEST.parent.is_dir():
        raise SystemExit("Destination parent is missing")
    if DEST.exists():
        raise SystemExit("Destination exists; inspect it before replacing anything")
    DEST.mkdir()
    for name in ("unity", "play"):
        shutil.copytree(overlay / name, DEST / name)
    (DEST / ".nojekyll").write_text("", encoding="utf-8")
    files = [p for p in DEST.rglob("*") if p.is_file()]
    verification = dict(path=str(DEST), repository_name="Biology_VR_App", original_repository="Altersam/Biology_VR",
                        original_repository_changed=False, files=len(files), bytes=sum(p.stat().st_size for p in files),
                        over_limit=[p.relative_to(DEST).as_posix() for p in files if p.stat().st_size >= 100 * 1024 * 1024],
                        webgl=(DEST / "play/index.html").is_file(), unity=(DEST / "unity/BiologyVR/ProjectSettings/ProjectVersion.txt").is_file())
    (ROOT / "SEPARATE_REPOSITORY_VERIFICATION.json").write_text(json.dumps(verification, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(verification, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
