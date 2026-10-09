"""Inspect the built artifact (manifest, ARM64 runtime and APK signature)."""
import hashlib
import json
import subprocess
import sys
import zipfile
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ANDROID = Path(r"C:\Program Files\Unity\Hub\Editor\6000.1.9f1\Editor\Data\PlaybackEngines\AndroidPlayer")
APK = ROOT / "Builds/Pico4Enterprise/BiologyVR_Pico4Enterprise.apk"
REPORT = ROOT / "Assets/BiologyVR/ArteryJourney/Reports/PicoApkVerification.json"


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
            "min_sdk_29": "sdkVersion:'29'" in badging,
            "target_sdk_34": "targetSdkVersion:'34'" in badging,
            "arm64_only": "native-code: 'arm64-v8a'" in badging and all("/arm64-v8a/" in name for name in native),
            "unity_runtime": "lib/arm64-v8a/libunity.so" in native,
            "il2cpp_runtime": "lib/arm64-v8a/libil2cpp.so" in native,
            "openxr_runtime": any("openxr" in name.lower() for name in native),
            "pico_vr_manifest": "pvr.app.type" in xml and '"vr"' in xml,
            "pico_sdk_manifest": "pvr.sdk.version" in xml,
            "controller_manifest": '"controller"' in xml,
            "signature_valid": signcode == 0,
        }
    digest = hashlib.sha256()
    with APK.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    result = dict(utc=datetime.now(timezone.utc).isoformat(), passed=all(checks.values()), path=str(APK), size_bytes=APK.stat().st_size,
                  sha256=digest.hexdigest(), checks=checks, native_libraries=native, badging=badging, signature=signature, headset_verified=False)
    REPORT.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({key: result[key] for key in ("passed", "path", "size_bytes", "sha256", "checks")}, ensure_ascii=False, indent=2))
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    sys.exit(main())
