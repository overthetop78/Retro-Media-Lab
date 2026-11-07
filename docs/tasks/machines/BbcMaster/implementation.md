# AcornEmu — famille BBC Master

- [x] Implémenter les Master et leurs équipements
  - [x] Références et firmware
    - [x] Créer puis supprimer les références temporaires dans `%TEMP%/rml-master-reference/` : fichiers matériels publics B-Em, JSBeeb et b2, utilisés uniquement pour vérifier les registres et protocoles.
    - [x] Ajouter les ROM Master identifiées dans `%APPDATA%/Retro Media Lab/Emulation/Machines/acorn/Firmware/` ; modifier les dictionnaires de reconnaissance Acorn correspondants, sans écraser de fichier existant.
  - [x] Matériel commun Master
    - [x] Créer les constantes et services sous `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMaster/Common/` : CPU CMOS, mémoire ACCCON/ROMSEL, RAM privée et écran, horloge/CMOS, bus et contrôleur disque Master, d’après les spécifications Acorn et les sources b2, B-Em et JSBeeb.
    - [x] Modifier `Emulators/AcornEmu/Services/Cpu6502*.cs` : mode CMOS 65C12/65C102 distinct du NMOS existant, instructions et indicateurs correspondants.
  - [x] Structure des définitions Master
    - [x] Créer `Common/Machines/BbcMaster/Constants/ModelConstants.cs` et `Dictionaries/ModelCatalog.cs` ; déplacer les définitions Master hors du catalogue BBC Micro et réunir les catalogues dans `Common/Machines/Common/Dictionaries/HardwareModelCatalog.cs`.
    - [x] Déplacer `Common/Machines/BbcMicro/Dictionaries/AcornBBCMicroModel.Master.cs` dans `Common/Machines/BbcMaster/Dictionaries/AcornMasterModel.cs` ; garder la résolution générale existante et les catégories de firmware.
    - [x] Créer `Emulators/AcornEmu/Machines/BbcMaster/Common/Dictionaries/AcornEmuMasterModelCatalog.cs` ; modifier le catalogue AcornEmu pour assembler les définitions Master, sans y écrire les profils en doublon.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/EmulationGlobalUsings.cs` et les catalogues concernés : importer les espaces internes Master et supprimer les noms entièrement qualifiés dans les expressions.
    - [x] Modifier les constantes Master, Tube et CPU nouvelles : remplacer les libellés et valeurs matérielles écrits en brut par leurs constantes ou ressources existantes.
  - [x] Modèles et réglages
    - [x] Modifier les catalogues modèles, fabriques, firmware et réglages Acorn ; créer les profils Master 128, ET et Compact dans leurs dossiers spécifiques ; conserver Turbo et 512 comme cartes du Master 128.
    - [x] Modifier les ressources Acorn : invariants dans 00-Base, traductions dans toutes les langues via Argos ; exposer les ROM et périphériques réellement disponibles.
  - [x] Coprocesseurs
    - [x] Créer les services Tube et cartes sous `Emulators/AcornEmu/Machines/BbcMaster/` : coprocesseur Turbo 65C102 avec RAM 64 Kio et Master 512 avec 80186 et RAM 512 Kio ; exécuter leurs ROM utilisateur et les échanges Tube.
  - [x] Périphériques des variantes
    - [x] Modifier `Emulators/AcornEmu/Dictionaries/FloppyDriveModels.cs`, `Constants/StorageConstants.cs`, `Functions/StorageFunctions*.cs` et la fabrique : profil 3½ pouces Compact, premier lecteur permanent, contrôleur Master fixe ; retirer tape et Winchester des profils qui ne les possèdent pas.
  - [x] Joystick Compact
    - [x] Créer `Emulators/AcornEmu/Machines/BbcMaster/Common/Services/CompactJoystick.cs` ; modifier les ports et associations Common pour le joystick numérique unique du Compact et raccorder PB0–PB4 du VIA utilisateur.
  - [x] Broches VIA Compact
    - [x] Modifier `Emulators/AcornEmu/Services/Via6522.cs` et le câblage de la carte partagée : notifier les changements des sorties et directions PB pour libérer correctement SDA lors des lectures EEPROM ; conserver les callbacks existants des autres modèles.
  - [x] Mémoire non volatile
    - [x] Modifier `Emulators/AcornEmu/Interfaces/IAcornMachine.cs`, `Services/EmulatorCore.cs` et la carte BBC partagée : conserver la CMOS Master et l’EEPROM Compact dans le répertoire de sauvegarde de la machine et les relire à l’ouverture, avec fermeture des fichiers garantie.
  - [x] Transferts Tube Master 512
    - [x] Modifier `Emulators/AcornEmu/Machines/BbcMaster/Tube/Services/TubeUla.cs`, `TurboBoard.cs` et `Services/Cpu6502.cs` : reproduire les indicateurs persistants du FIFO R3 et retirer l’acquittement NMI logiciel ajouté au CPU.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuMasterTests.cs` : vérifier qu’un octet Tube reçu produit un seul transfert DMA et que les indicateurs R3 restent actifs jusqu’à vidage du FIFO.
  - [x] Décodage mémoire de la carte 80186
    - [x] Modifier `Emulators/AcornEmu/Machines/BbcMaster/Tube/Constants/TubeConstants.cs` et `Services/Master512Board.cs` : miroir RAM 80000–BFFFF vers 40000–7FFFF, ROM C0000–FFFFF, conformément au décodage de la carte Acorn.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuMasterTests.cs` : vérifier les miroirs RAM et ROM, puis supprimer le diagnostic externe et ses captures après vérification du démarrage DOS.
  - [x] ROM et géométries finales
    - [x] Modifier `Common/Machines/Common/Dictionaries/AcornFirmware.Coprocessor.cs`, `Common/Machines/BbcMaster/Dictionaries/AcornMasterModel.cs` et `Emulators/AcornEmu/Factories/AcornEmuMachineFactory.cs` : distinguer ROM officielles et variantes compatibles ; ajouter le client 80186 v1.01 testé dans le dossier Firmware Acorn utilisateur sans écrasement.
    - [x] Modifier `Emulators/AcornEmu/Constants/FloppyDriveConstants.cs` et `Dictionaries/FloppyDriveModels.cs` : autoriser les géométries DD 360/720 Kio et 800 Kio reconnues par le WD1770, sans modifier le filtrage 8271 ; une constante par ligne.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuMasterTests.cs` : couvrir les géométries DOS et la classification des ROM de coprocesseur.
    - [x] Créer `artifacts/acorn-master/master512-dosplus21.png` : capture réelle du tampon vidéo de l’émulation après démarrage DOS Plus 2.1, avant suppression des diagnostics temporaires.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuTests.cs` : adapter les attentes Master aux ROM désormais reconnues incompatibles, sans changer les attentes BBC Micro.
    - [x] Modifier `Common/Machines/Common/Dictionaries/AcornFirmware.Application.cs` : déplacer l’identité Client86 déjà existante vers la catégorie coprocesseur, sans doublon SHA-1.
    - [x] Modifier `Emulators/AcornEmu/Functions/MasterFunctions.cs` : valider la taille de la ROM du coprocesseur avec le diagnostic firmware traduit existant.
  - [x] Validation et livraison
    - [x] Créer puis supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornMasterBootDiagnostic.cs` et ses artefacts : vérifier les ROM externes en lecture seule, aucun réglage utilisateur modifié.
    - [x] Créer les tests autonomes `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuMasterTests.cs` pour les instructions, banques, registres, horloge et cartes ; supprimer tout diagnostic externe temporaire et ses artefacts après utilisation.
    - [x] Modifier ce suivi avec les résultats ; compiler le Debug application et tous les modules par `scripts/local-building.cmd --building=debug --modules=A` et vérifier les fichiers livrés.

## Résultats vérifiés

- Démarrage des ROM Master 128, Turbo, ET et Compact vérifié ; DOS Plus 1.2 et 2.1 atteignent l’invite A> sur la carte 80186 de 512 Kio.
- Capture conservée : `artifacts/acorn-master/master512-dosplus21.png`.
- 353 tests Acorn réussis dans la sélection de régression. Les anciens scénarios `AcornModuleTests` et `AcornFloppyDialogAppliesControllerAndBootToItsDrive` sont exclus de cette sélection ; ce résultat ne représente pas la suite Acorn intégrale.
- Diagnostic utilisant les fichiers externes supprimé, assembly de tests reconstruit ; répertoire temporaire `rml-master-reference` supprimé.

- Livraison Debug terminée : script All réussi (code 0), 24 manifestes et DLL présents, exécutable présent ; empreinte du module Acorn livré identique à celle du module compilé. Chemin : `build/Debug/Retro Media Lab/retromedialab.exe`.


## Clôture du bloc avant commit

- [x] Actualiser les anciens scénarios sans modifier le comportement de production.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/Acorn/AcornModuleTests.cs` : choisir explicitement B2 dans ses scénarios natifs, compter BREAK parmi les touches assignables et vérifier les modèles par leur identité plutôt que par un ancien total fixe.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Interface/EmulationViews/EmulationViewsTests.cs` : construire explicitement la configuration B2 du scénario de contrôleur Challenger.
  - [x] Modifier ce suivi avec le résultat de la régression Acorn complète et de la revue des constantes Master ; conserver les avertissements et limites réels, puis committer le bloc Master compilé.

Validation finale : 377 tests Acorn réussis, zéro échec et zéro exclusion. Les anciennes attentes B2 sont désormais explicitement rattachées à B2 ; les touches assignables comprennent BREAK. Revue des constantes : adresses, tailles, registres et libellés Master sont nommés dans les constantes matérielles ou les ressources ; les encodages des instructions restent dans le décodage CPU. Le dernier build Debug All reste à jour : seuls les tests et ce suivi ont changé depuis sa réussite.
