"""Verify the signed Meta Quest build and reject a mistakenly packaged Pico runtime."""
import hashlib
import json
import subprocess
import zipfile
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ANDROID = Path(r"C:\Program Files\Unity\Hub\Editor\6000.1.9f1\Editor\Data\PlaybackEngines\AndroidPlayer")
APK = ROOT / "Builds/MetaQuest/BiologyVR_MetaQuest.apk"


def run(*args):
    result = subprocess.run([str(arg) for arg in args], capture_output=True, text=True, encoding="utf-8", errors="replace")
    return result.returncode, result.stdout + result.stderr


def main():
    if not APK.is_file():
        raise SystemExit(f"APK missing: {APK}")
    tools = ANDROID / "SDK/build-tools/34.0.0"
    code, badging = run(tools / "aapt.exe", "dump", "badging", APK)
    xmlcode, xml = run(tools / "aapt.exe", "dump", "xmltree", APK, "AndroidManifest.xml")
    signcode, signature = run(ANDROID / "OpenJDK/bin/java.exe", "-jar", tools / "lib/apksigner.jar", "verify", "--verbose", APK)
    with zipfile.ZipFile(APK) as archive:
        native = [name for name in archive.namelist() if name.startswith("lib/") and name.endswith(".so")]
    checks = {
        "manifest_readable": code == 0 and xmlcode == 0,
        "application_id": "package: name='com.biologyvr.arteryjourney'" in badging,
        "version_0_2_0": "versionName='0.2.0'" in badging and "versionCode='3'" in badging,
        "arm64_only": "native-code: 'arm64-v8a'" in badging and all("/arm64-v8a/" in name for name in native),
        "unity_il2cpp": all("lib/arm64-v8a/" + name in native for name in ("libunity.so", "libil2cpp.so")),
        "openxr_loader": "lib/arm64-v8a/libopenxr_loader.so" in native,
        "quest_supported_devices": "com.oculus.supportedDevices" in xml and all(name in xml for name in ("quest2", "eureka", "quest3s", "cambria")),
        "quest_vr_category": "com.oculus.intent.category.VR" in xml,
        "headtracking_feature": "android.hardware.vr.headtracking" in xml,
        "no_pico_manifest": "pvr.app.type" not in xml,
        "no_pico_libraries": not any("pico" in name.lower() or "pxr" in name.lower() or "pvr" in name.lower() for name in native),
        "signature_valid": signcode == 0,
    }
    value = hashlib.sha256()
    with APK.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(chunk)
    report = dict(utc=datetime.now(timezone.utc).isoformat(), passed=all(checks.values()), path=str(APK), size_bytes=APK.stat().st_size,
                  sha256=value.hexdigest(), checks=checks, native_libraries=native, badging=badging, signature=signature, headset_verified=False)
    (ROOT / "Assets/BiologyVR/ArteryJourney/Reports/MetaQuestApkVerification.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({key: report[key] for key in ("passed", "path", "size_bytes", "sha256", "checks")}, ensure_ascii=False, indent=2))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
