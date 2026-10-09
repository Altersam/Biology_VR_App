"""Attach APK release files and verify the independent upload package."""
import hashlib
import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DEST = ROOT.parent / "Biology_VR_App"


def sha(path):
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(chunk)
    return value.hexdigest()


def main():
    releases = DEST / "ReleaseFiles"
    releases.mkdir(exist_ok=True)
    apks = []
    for directory, name in (("MetaQuest", "BiologyVR_MetaQuest.apk"), ("Pico4Enterprise", "BiologyVR_Pico4Enterprise.apk")):
        source = ROOT / "Builds" / directory / name
        target = releases / name
        shutil.copy2(source, target)
        apks.append(dict(path=target.relative_to(DEST).as_posix(), bytes=target.stat().st_size, sha256=sha(target), copy_matches=sha(source) == sha(target)))
    files = [p for p in DEST.rglob("*") if p.is_file() and ".git" not in p.relative_to(DEST).parts]
    upload = [p for p in files if p.relative_to(DEST).parts[0] != "ReleaseFiles"]
    prohibited = []
    for path in upload:
        relative = path.relative_to(DEST)
        if path.name in {"bridge-token", "McpUnitySettings.json"} or path.suffix in {".apk", ".keystore", ".jks"}:
            prohibited.append(relative.as_posix())
        if relative.parts[:2] == ("unity", "BiologyVR") and relative.parts[2] in {"Library", "Temp", "Logs", "UserSettings"}:
            prohibited.append(relative.as_posix())
    referenced_build_files = []
    html = (DEST / "play/index.html").read_text(encoding="utf-8")
    for name in re.findall(r'"([^"\s]+\.(?:loader\.js|unityweb))"', html):
        file = DEST / "play/Build" / name.lstrip("/")
        referenced_build_files.append(dict(name=name, exists=file.is_file()))
    checks = dict(webgl_index=(DEST / "play/index.html").is_file(), build_files=len(list((DEST / "play/Build").iterdir())) >= 4,
                  landing_links_to_game='href="play/"' in (DEST / "index.html").read_text(encoding="utf-8"),
                  loader_references=len(referenced_build_files) >= 4 and all(item["exists"] for item in referenced_build_files),
                  unity_project=(DEST / "unity/BiologyVR/ProjectSettings/ProjectVersion.txt").is_file(),
                  no_prohibited=not prohibited, sizes_ok=all(p.stat().st_size < 100 * 1024 * 1024 for p in upload),
                  apk_copies_match=all(item["copy_matches"] for item in apks), instructions=(DEST / "КАК_ЗАГРУЗИТЬ.txt").is_file())
    report = dict(path=str(DEST), passed=all(checks.values()), checks=checks, upload_files=len(upload), upload_bytes=sum(p.stat().st_size for p in upload),
                  largest_upload_files=[dict(path=p.relative_to(DEST).as_posix(), bytes=p.stat().st_size) for p in sorted(upload, key=lambda p: p.stat().st_size, reverse=True)[:5]],
                  prohibited=prohibited, apks=apks, referenced_build_files=referenced_build_files, existing_remote_changed=False)
    (ROOT / "SEPARATE_REPOSITORY_VERIFICATION.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
