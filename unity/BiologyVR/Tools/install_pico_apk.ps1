param([string]$Serial, [string]$Apk = "$PSScriptRoot\..\Builds\Pico4Enterprise\BiologyVR_Pico4Enterprise.apk")
$ErrorActionPreference = 'Stop'
$adb = 'C:\Program Files\Unity\Hub\Editor\6000.1.9f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
if (-not (Test-Path -LiteralPath $adb)) { throw 'ADB not found. Install Android Build Support in Unity Hub, or edit the adb path in this script.' }
if (-not (Test-Path -LiteralPath $Apk)) { throw "APK not found: $Apk" }
$deviceArgs = @()
if ($Serial) { $deviceArgs = @('-s', $Serial) }
& $adb devices -l
& $adb @deviceArgs install -r $Apk
if ($LASTEXITCODE -ne 0) { throw 'Installation failed. Confirm USB debugging authorization inside Pico; if multiple devices are connected, pass -Serial.' }
& $adb @deviceArgs shell am start -n 'com.biologyvr.arteryjourney/com.unity3d.player.UnityPlayerActivity'
if ($LASTEXITCODE -ne 0) { throw 'APK installed, but ADB launch failed. Open Biology VR in the Pico app library.' }
