"""Export the audited current scene and compilation/config dependencies, preserving .meta."""
import argparse
import hashlib
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path


def sha(path):
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(chunk)
    return value.hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("destination", type=Path)
    args = parser.parse_args()
    source = Path(__file__).resolve().parent.parent
    destination = args.destination.resolve()
    audit = json.loads((source / "CURRENT_PROJECT_TRANSFER.json").read_text(encoding="utf-8-sig"))
    if not audit["passed"]:
        raise SystemExit("Fix the Unity dependency audit before transferring")
    if not destination.parent.is_dir() or destination == source:
        raise SystemExit("Destination must be a separate folder in an existing parent")
    paths = set(audit["assets"])
    for relative in list(paths):
        asset = Path(relative)
        if (source / (relative + ".meta")).is_file():
            paths.add(relative + ".meta")
        for parent in asset.parents:
            if parent.as_posix() in (".", "Assets"):
                continue
            meta = parent.as_posix() + ".meta"
            if (source / meta).is_file():
                paths.add(meta)
    for folder in ("Packages", "ProjectSettings", "Tools"):
        paths.update(path.relative_to(source).as_posix() for path in (source / folder).rglob("*") if path.is_file() and "__pycache__" not in path.parts)
    paths.update(path.name for path in source.glob("*.md"))
    paths.add("CURRENT_PROJECT_TRANSFER.json")
    for path in (source / "Builds").rglob("*.apk"):
        paths.add(path.relative_to(source).as_posix())
    backup = None
    if destination.exists() and any(destination.iterdir()):
        backup = destination.with_name(destination.name + "_before_cleanup_" + datetime.now().strftime("%Y%m%d_%H%M%S"))
        try:
            destination.rename(backup)
        except PermissionError:
            # Explorer/Yandex can hold the directory itself open. Preserve the
            # source files first, then remove only audited non-required assets.
            shutil.copytree(destination, backup, ignore=shutil.ignore_patterns("Library", "Temp", "Logs", ".git"))
            for existing in (destination / "Assets").rglob("*"):
                if existing.is_file() and existing.relative_to(destination).as_posix() not in paths:
                    saved = backup / existing.relative_to(destination)
                    if not saved.is_file() or sha(existing) != sha(saved):
                        raise SystemExit(f"Backup verification failed: {existing}")
                    existing.unlink()
    destination.mkdir(exist_ok=True)
    mismatches, total, missing = [], 0, []
    for relative in sorted(paths):
        original = source / relative
        if not original.is_file():
            missing.append(relative)
            continue
        target = destination / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(original, target)
        total += target.stat().st_size
        if sha(original) != sha(target):
            mismatches.append(relative)
    report = dict(utc=datetime.now(timezone.utc).isoformat(), source=str(source), destination=str(destination), backup=str(backup) if backup else None,
                  passed=not mismatches and not missing, copied_files=len(paths) - len(missing), copied_bytes=total,
                  current_scene=audit["startup"]["scene"], current_wall=audit["startup"]["currentWallMaterial"],
                  asset_count=audit["assetCount"], missing=missing, mismatches=mismatches, cache_copied=False)
    (destination / "TRANSFER_VERIFICATION.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
