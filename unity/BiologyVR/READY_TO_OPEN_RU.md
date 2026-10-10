# Готовая версия bio_v0.2 / Coral V6 / 0.2.0

Unity **6000.1.9f1**. Master scene:
`Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
В этом репозитории открывать в Unity Hub папку **unity/BiologyVR**.
В рабочем каталоге автора открывать **bio_v0.2**.

Актуальные версии материалов, исходников, WebGL и APK соответствуют Coral V6.
Подробности: WORKING_VERSION_RU.md, ARTERY_CORAL_V6_RU.md,
META_QUEST_RU.md, PICO4_ENTERPRISE_RU.md, GITHUB_UPLOAD_RU.md.

Подтверждения: dependency audit без missing scripts/GUID, PC input PASS,
sequential simulated-XR Scenes03–16 + VERY FAR FULL SIMULATOR PASS.
Свежая WebGL загрузилась в Chromium; Quest/Pico APK 0.2.0/code3 проверены
по manifest, ARM64/IL2CPP/OpenXR, SHA-256 и подписи.

Аппаратная проверка FPS/GPU/комфорта на подключённых шлемах по-прежнему отдельна.
Library/Temp/Logs не входят в GitHub-проект и создаются Unity автоматически.
Исторические отчёты имеют старые timestamps; актуальные build/verification JSON
находятся в Assets/BiologyVR/ArteryJourney/Reports.
