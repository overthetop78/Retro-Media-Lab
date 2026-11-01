# AcornEmu — standard vidéo des Model A et B

- [x] PAL / NTSC sans nouveau modèle
  - [x] Détection MOS et priorité du choix utilisateur
    - [x] Modifier `Common/Machines/Common/Contracts/FirmwareContracts.cs` et `Dictionaries/FirmwareCatalog.cs` dans `src/RetroMediaLab.Emulation.Acorn` : associer les MOS identifiées à leur standard vidéo.
    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Constants/VideoStandardConstants.cs` et `Functions/VideoStandardFunctions.cs` : choix automatique/PAL/NTSC, détection par contenu et conservation du standard précédent pour une MOS inconnue.
  - [x] Configuration et matériel vidéo
    - [x] Modifier `Emulators/AcornEmu/Factories/AcornEmuMachineFactory.cs` : exposer le réglage Vidéo, normaliser le standard mémorisé et demander le redémarrage pour ce réglage.
    - [x] Modifier `Common/Machines/BbcMicro/Constants/ModelConstants.cs`, `Emulators/AcornEmu/Machines/BbcMicro/Common/Constants/VideoConstants.cs`, `Services/Video.cs`, `Services/BbcMicroMachine.cs`, `Emulators/AcornEmu/Dictionaries/ModelCatalog.cs` et `Services/EmulatorCore.cs` : appliquer cadence et hauteur PAL/NTSC en conservant le CPU à 2 MHz.
  - [x] Traductions
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Resources/*/Emulation.resx` avec Argos pour l’aide du mode automatique ; réutiliser les libellés communs existants et retirer PAL du libellé UHF désormais commun aux deux standards.
  - [x] Validation et livraison locale
    - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuVideoStandardTests.cs` : détection MOS, priorité manuelle, ROM inconnue, cadence et dimensions.
    - [x] Modifier ce suivi avec les résultats des tests Acorn et du build Debug de tous les modules ; vérifier l’exécutable et les 24 modules produits.

## Résultats

- 339 tests AcornEmu et raccourcis clavier réussis, aucun échec.
- Build `scripts\local-building.cmd --building=debug --modules=A` terminé avec le code 0.
- Exécutable : `build/Debug/Retro Media Lab/retromedialab.exe`.
- 24 manifestes et leurs DLL présents ; empreinte de la DLL Acorn livrée identique à la compilation.
- Dimensions testées : PAL 640 × 256, NTSC 640 × 200 ; budgets 39 936 / 33 536 cycles à 2 MHz, audio inchangé à 44,1 kHz.
- Modèles A et B conservés ; aucun modèle régional ajouté. Les registres CRTC restent programmés par le logiciel de la machine.
- Libellés du choix réutilisés depuis les ressources communes ; nouvelle aide traduite avec Argos, version française relue.
