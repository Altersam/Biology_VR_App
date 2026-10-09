# Готовая версия bio_v0.2 / Coral V6 / 0.2.0

Unity **6000.1.9f1**. Master scene:
`Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity`.
В этом репозитории открывать в Unity Hub папку **unity/BiologyVR**.

Материалы, исходники, WebGL и APK соответствуют текущему Coral V6.
Читайте WORKING_VERSION_RU.md, ARTERY_CORAL_V6_RU.md, META_QUEST_RU.md,
PICO4_ENTERPRISE_RU.md и GITHUB_UPLOAD_RU.md.

Подтверждения: dependency audit без missing scripts/GUID, PC input PASS,
sequential simulated-XR Scenes03–16 + VERY FAR FULL SIMULATOR PASS.
Свежая WebGL загрузилась в Chromium; Quest/Pico APK 0.2.0/code3 проверены
по manifest, ARM64/IL2CPP/OpenXR, SHA-256 и подписи.

Hardware FPS/GPU/comfort на шлемах не измерены. Кэши Library/Temp/Logs не
загружены; Unity создаёт их при импорте. Старые отчёты обозначают историю,
актуальные build/verification JSON — Assets/BiologyVR/ArteryJourney/Reports.
