# Audit et documentation des constantes

L’audit initial de `src` et `tests` a parcouru 1 454 fichiers de constantes : 39 fichiers contenaient des déclarations regroupées et 18 316 déclarations n’avaient pas de documentation XML immédiatement associée. Les trois fichiers signalés ont été corrigés en premier. La portée des corrections historiques est en attente de la réponse à la question de périmètre.

- [x] Corriger les fichiers du travail en cours pendant la clarification du périmètre historique.
  - [x] Modifier `src/RetroMediaLab.Emulation/Constants/EmulationMechanicalSoundConstants.cs` : séparer et documenter les relais SDK des constantes MediaEngine.
  - [x] Modifier `src/RetroMediaLab.Emulation/Constants/EmulationTapeSettingsConstants.cs` : séparer et documenter les champs, groupes et présélections du lecteur de bande.
  - [x] Modifier `src/RetroMediaLab.App/Constants/Emulation/StorageConfigurationLayoutConstants.cs` : séparer et documenter les dimensions, rapports et limites de présentation.
  - [x] Modifier `src/RetroMediaLab.App/Constants/Emulation/MediaCreationConstants.cs` : séparer et documenter les dimensions et clés de traduction du dialogue.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Constants/StorageConstants.cs` : séparer et documenter les identifiants, valeurs, limites et unités de stockage.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Amstrad/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Apple/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Atari/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Bandai/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Coleco/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Commodore/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Epoch/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Example/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Fairchild/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Mattel/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Microsoft/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Nec/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Nintendo/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Panasonic/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Philips/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Sega/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Sharp/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Sinclair/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Snk/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Sony/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Thomson/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Watara/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Wenquxing/Common/Constants/EmulationModuleConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation/Constants/EmulationAssetPaths.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Common/Constants/MediaConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Amstrad/Common/Machines/Common/Constants/MachineCatalogConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.MediaEngine/Constants/DiskImageFileExtensions.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier `src/RetroMediaLab.Emulation.Commodore/Common/Machines/Common/Constants/MachineCatalogConstants.cs` : documenter les constantes de chemins, identifiants ou formats et conserver leurs valeurs.
  - [x] Modifier ce document après vérification : consigner la conservation exacte des identifiants, types et expressions et la vérification XML, sans build de Retro Media Lab.

Vérification effectuée : 37 fichiers de constantes du travail en cours, 539 déclarations individuelles documentées, XML valide. Les transformations ont vérifié la conservation des noms, types et expressions avant/après écriture. Aucun build de Retro Media Lab. La reprise des fichiers historiques du dépôt attend la réponse à la question de périmètre.
