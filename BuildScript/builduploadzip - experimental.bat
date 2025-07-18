@echo off
rmdir "../bin/BepInEx" /s /q
mkdir "../bin/BepInEx/plugins/VirusTLNR-LethalIntelligence"
powershell Copy-Item -Path "../bin/Debug/netstandard2.1/LethalIntelligence.dll" -Destination "../bin/BepInEx/plugins/VirusTLNR-LethalIntelligence/LethalIntelligence.dll"
powershell Copy-Item -Path "mapdot.bundle" -Destination "../bin/BepInEx/plugins/VirusTLNR-LethalIntelligence/mapdot.bundle
powershell Copy-Item -Path "maskedanimations.bundle" -Destination "../bin/BepInEx/plugins/VirusTLNR-LethalIntelligence/maskedanimations.bundle
powershell Compress-Archive^
    -Force^
    -Path "../bin/BepInEx",^
          "./experimentalmanifest/manifest.json",^
          "./icon.png",^
          "../README.md",^
          "../CHANGELOG.md",^
          "../LICENSE.txt"^
    -DestinationPath "../bin/LethalIntelligenceExperimental.zip"