"""Create a GitHub upload overlay without caches, APKs or local transport settings."""
import hashlib
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OVERLAY = ROOT / "GitHubUpload"
PROJECT = OVERLAY / "unity/BiologyVR"
EXCLUDED = {"McpUnitySettings.json", "SOURCE_SNAPSHOT.json", "TRANSFER_VERIFICATION.json", "CURRENT_PROJECT_TRANSFER.json"}


def main():
    PROJECT.mkdir(parents=True, exist_ok=True)
    for folder in ("Assets", "Packages", "ProjectSettings", "Tools"):
        for source in (ROOT / folder).rglob("*"):
            if not source.is_file() or source.name in EXCLUDED or "__pycache__" in source.parts:
                continue
            target = PROJECT / source.relative_to(ROOT)
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, target)
    for name in (".gitignore", ".gitattributes", "README.md", "GITHUB_UPLOAD_RU.md", "PICO4_ENTERPRISE_RU.md", "META_QUEST_RU.md", "READY_TO_OPEN_RU.md"):
        if (ROOT / name).is_file():
            shutil.copy2(ROOT / name, PROJECT / name)
    (OVERLAY / ".nojekyll").write_text("", encoding="utf-8")
    if (ROOT / "Builds/WebGL/play/index.html").is_file():
        shutil.copytree(ROOT / "Builds/WebGL/play", OVERLAY / "play", dirs_exist_ok=True)
    shutil.copy2(ROOT / "GITHUB_UPLOAD_RU.md", OVERLAY / "GITHUB_UNITY_WEBGL.md")
    (OVERLAY / "UPLOAD_README.md").write_text(
        "# GitHub upload overlay\n\nCopy these files into a fresh clone of Altersam/Biology_VR.\n"
        "Existing site files stay at the repository root; the Unity project is under unity/BiologyVR.\n"
        "Read GITHUB_UNITY_WEBGL.md before adding/committing. Install Git LFS before git add.\n"
        "APKs are in the source project's Builds folder and belong in GitHub Releases.\n", encoding="utf-8")
    files = [p for p in OVERLAY.rglob("*") if p.is_file()]
    over_limit = [{"path": p.relative_to(OVERLAY).as_posix(), "bytes": p.stat().st_size} for p in files if p.stat().st_size >= 100 * 1024 * 1024]
    forbidden = [p.relative_to(OVERLAY).as_posix() for p in files if (p.relative_to(OVERLAY).parts[:2] == ("unity", "BiologyVR") and p.relative_to(OVERLAY).parts[2] in {"Library", "Temp", "Logs", "UserSettings", ".git"}) or p.name in EXCLUDED or p.suffix in {".apk", ".keystore", ".jks"}]
    verification = dict(path=str(OVERLAY), files=len(files), bytes=sum(p.stat().st_size for p in files), forbidden=forbidden, over_github_file_limit=over_limit,
                        webgl_included=(OVERLAY / "play/index.html").is_file(), remote_changed=False, passed=not forbidden and not over_limit)
    (ROOT / "GITHUB_OVERLAY_VERIFICATION.json").write_text(json.dumps(verification, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(verification, ensure_ascii=False, indent=2))
    return 0 if verification["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
