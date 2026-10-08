param([string]$Serial, [string]$Apk = "$PSScriptRoot\..\Builds\MetaQuest\BiologyVR_MetaQuest.apk")
$ErrorActionPreference = 'Stop'
$adb = 'C:\Program Files\Unity\Hub\Editor\6000.1.9f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
if (-not (Test-Path -LiteralPath $adb)) { throw 'ADB not found; install Android Build Support in Unity Hub.' }
if (-not (Test-Path -LiteralPath $Apk)) { throw "APK not found: $Apk" }
$deviceArgs = @()
if ($Serial) { $deviceArgs = @('-s', $Serial) }
& $adb devices -l
& $adb @deviceArgs install -r $Apk
if ($LASTEXITCODE -ne 0) { throw 'Confirm USB debugging in Quest. With multiple devices, pass -Serial.' }
& $adb @deviceArgs shell am start -n 'com.biologyvr.arteryjourney/com.unity3d.player.UnityPlayerActivity'
if ($LASTEXITCODE -ne 0) { throw 'Installed; open Biology VR from Unknown Sources in the Quest app library.' }
