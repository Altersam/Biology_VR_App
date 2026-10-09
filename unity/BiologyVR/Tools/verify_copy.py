"""Verify a standalone Unity source copy before opening it in the editor."""
import argparse
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def sha(path):
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(chunk)
    return value.hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    args = parser.parse_args()
    mismatches, count, total = [], 0, 0
    for folder in ("Assets", "Packages", "ProjectSettings", "Tools"):
        for source in (args.source / folder).rglob("*"):
            if not source.is_file() or "__pycache__" in source.parts:
                continue
            relative = source.relative_to(args.source)
            target = ROOT / relative
            if not target.is_file() or sha(source) != sha(target):
                mismatches.append(relative.as_posix())
            count += 1
            total += source.stat().st_size
    report = dict(utc=datetime.now(timezone.utc).isoformat(), source=str(args.source), destination=str(ROOT),
                  passed=not mismatches, copied_files=count, copied_bytes=total, mismatches=mismatches,
                  method="All Assets, Packages, ProjectSettings and Tools compared by SHA-256 before editor import; .meta included", cache_copied=(ROOT / "Library").exists())
    (ROOT / "TRANSFER_VERIFICATION.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
