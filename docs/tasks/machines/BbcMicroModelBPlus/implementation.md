# AcornEmu — BBC Micro Model B+

- [x] Model B+ configurable en 64 / 128 Kio, sans ROM embarquée
  - [x] Mémoire et carte B+
    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/ModelBPlus/Constants/ModelBPlusConstants.cs` et `Services/ModelBPlusMemory.cs` : décodage FE30/FE34, 20 Kio Shadow, 12 Kio privés, quatre banques de 16 Kio optionnelles.
    - [x] Modifier `Emulators/AcornEmu/Services/Cpu6502.cs`, `Machines/BbcMicro/Common/Services/BbcMicroMachine.cs`, `Video.cs` et `SidewaysRoms.cs` : réutiliser le moteur NMOS pour le 6512, suivre l’adresse d’instruction et raccorder les mémoires B+ au CPU et à la vidéo sans changer A/B.
  - [x] Profil, capacités et ROM utilisateur
    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Functions/ModelBPlusFunctions.cs` : capacités 64/128, cinq supports de 8/16/32 Kio, conflits de banques et image MOS/BASIC combinée.
    - [x] Modifier `Common/Machines/BbcMicro/Functions/MemoryFunctions.cs`, `Constants/ModelConstants.cs`, `Common/Machines/Common/Dictionaries/FirmwareCatalog.cs`, `Common/Machines/BbcMicro/Dictionaries/FirmwareCompatibilityCatalog.cs`, `Emulators/AcornEmu/Dictionaries/ModelCatalog.cs` et `Functions/FirmwareFunctions.cs` et `Functions/SidewaysExpansionFunctions.cs` : profil B+, MOS 2.00 et DFS WD1770 compatibles, chargement sans ROM intégrée.
    - [x] Modifier `Emulators/AcornEmu/Functions/StorageFunctions.cs`, `FloppyExtensionFunctions.cs` et `Factories/AcornEmuMachineFactory.cs` : WD1770 fixe, premier lecteur permanent, RAM totale et blocs mémoire/ROM B+ ; conserver le second lecteur optionnel.
  - [x] Un seul modèle présenté et localisation
    - [x] Modifier `Common/Machines/Common/Dictionaries/MachineCatalog.cs` et les fonctions de modèle/configuration B2 nécessaires : présenter un B+ unique tout en conservant les configurations existantes et les cœurs compatibles.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Resources/*/*.resx` avec Argos pour le nom Model B+ et les descriptions de mémoire intégrée/ROM combinée ; garder les identifiants de supports invariants dans la base commune.
  - [x] Intégration finale
    - [x] Modifier les fonctions firmware/stockage AcornEmu, le câblage des registres et les fonctions B2 déjà raccordées : finaliser la lecture combinée, les miroirs FE30–FE37 et le maintien des configurations B+ existantes.
    - [x] Déplacer les identités firmware B+ de `Emulators/AcornEmu/Machines/BbcMicro/ModelBPlus/Constants/ModelBPlusConstants.cs` vers `Common/Machines/BbcMicro/Constants/BbcMicroBPlusFirmwareConstants.cs` et adapter leurs références : garder le catalogue firmware commun indépendant d’AcornEmu.
  - [x] Validation
    - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuModelBPlusTests.cs` : mémoire Shadow selon provenance des instructions, RAM privée, banques 128K, supports ROM, lecteur permanent, configuration et non-régression A/B.
    - [x] Modifier `Emulators/AcornEmu/Factories/AcornEmuMachineFactory.cs`, `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuTests.cs` et `AcornEmuModelBPlusTests.cs` : préserver le lecteur permanent lors de l’application du stockage et actualiser les assertions de compatibilité désormais implémentée ; contrôler aussi les configurations B2 64/128.
    - [x] Modifier ce suivi avec les résultats des tests et du build Debug `--modules=A` ; vérifier l’exécutable et les 24 modules livrés.

## Résultats

- 356 tests AcornEmu / clavier réussis, aucun échec. Tests autonomes sans ROM téléchargée ni fichier temporaire.
- Construction Debug avec les 24 modules terminée avec le code 0. Exécutable : `build/Debug/Retro Media Lab/retromedialab.exe`.
- 24 manifestes et leurs DLL vérifiés ; la DLL Acorn livrée correspond à la compilation.
- Un B+ présenté ; capacités 64/128 Kio. Les identifiants des anciennes configurations B2 B+128 restent lisibles.
- 6512 NMOS : même moteur d’instructions que le 6502 ; décodage B+ de FE30–FE37, 20 Kio Shadow, 12 Kio privés et quatre banques RAM optionnelles.
- WD1770 fixe, lecteur 0 permanent, second lecteur optionnel. Images MOS/BASIC combinées de 32 Kio et DFS externes choisies par l’utilisateur. Aucune ROM intégrée ou installée.
- Cinq supports 8/16/32 Kio ; découpage des puces et miroirs testés. Libellés traduits avec Argos ; français relu et noms invariants dans 00-Base.
- Le constat initial d'absence de démarrage ROM réel est dépassé : les démarrages MOS/BASIC, catalogues et chargements SSD, tapes UEF/enregistrements, formatages DFS et écritures sur deux lecteurs/deux faces des B+ 64/128 sont consignés dans [l'état BBC Micro](../../../machines/AcornBBCMicro.md). Ce document précise la portée de chaque preuve et les essais ADFS du B+ 64. Les essais de carte ne remplacent pas la validation finale dans l'application ; voir [la finalisation](finalisation.md).

## Références croisées

- Acorn, manuel B+ : https://www.retroisle.com/others/acornbbc/OriginalDocs/modelbplus_servicemanual.php
- b2, décodage ROMSEL/ACCCON et registre Shadow : https://github.com/tom-seddon/b2/blob/master/src/beeb/src/BBCMicro.cpp
- B-Em, sélection de mémoire selon le code exécuté et registres B+ : https://github.com/stardot/b-em/blob/master/src/6502.c et https://github.com/stardot/b-em/blob/master/src/model.c
- JSBeeb, profils consultés : https://github.com/mattgodbolt/jsbeeb/blob/master/src/models.js (B, Master et Atom ; aucun profil B+ trouvé dans cette liste).
- MAME, banques RAM 0/1/12/13 et empreintes MOS/DFS : https://github.com/mamedev/mame/blob/master/src/mame/acorn/bbcbp.cpp
