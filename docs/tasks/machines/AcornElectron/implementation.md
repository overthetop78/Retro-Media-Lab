# AcornEmu — Acorn Electron

- [x] Réaliser le bloc Electron après le commit Atom 0ac0700d4.
  - [x] Construire la carte Electron avec le CPU existant et son ULA propre.
    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornElectron/Common/Constants/ElectronConstants.cs` : horloges, mémoire, broches et registres ULA nommés, une constante par ligne.
    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornElectron/Common/Constants/ElectronVideoConstants.cs` : géométries des modes, cadence, palette et masques vidéo documentés.
    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornElectron/Common/Dictionaries/ElectronKeyboardMap.cs` : matrice électrique des quatorze lignes du clavier Electron.
    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornElectron/Common/Services/ElectronKeyboard.cs` : lecture active haute des lignes sélectionnées par les adresses, modificateurs et BREAK.
    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornElectron/Common/Services/ElectronVideo.cs` : raster ULA, modes bitmap, palette immédiate et bouclage RAM vidéo ; tampons gérés sans surface native.
    - [x] Modifier `Emulators/AcornEmu/Services/AnalogTape.cs` : commande moteur sans déplacement de la tête à l’arrêt, rembobinage indépendant et présence du signal pour le récepteur ULA.
    - [x] Modifier `Machines/AcornElectron/Common/Constants/ElectronConstants.cs` : constantes de sérialisation et de détection de porteuse utilisées par l’ULA.
    - [x] Créer `Services/ElectronUla.cs` et ses partiels dans ce composant : interruptions, commutation ROM, son et sérialisation tape réelle ; réutiliser le transport analogique existant.
    - [x] Créer `Services/ElectronMachine.cs` et ses partiels Bus/Media dans ce composant : CPU6502, contention RAM, mémoire, ULA et médias ; aucune interception des commandes MOS.
  - [x] Définir le modèle et raccorder les contrats existants.
    - [x] Créer `Common/Machines/AcornElectron/Constants/ModelConstants.cs` et `Dictionaries/ModelCatalog.cs`, `Dictionaries/AcornElectronModel.cs` : capacités, emplacements et compatibilité ROM Electron.
    - [x] Modifier les catégories `Common/Machines/Common/Dictionaries/AcornFirmware.*.cs` nécessaires ; ajouter les firmware Electron manquants dans le dossier Firmware utilisateur après contrôle des tailles et empreintes, sans écraser un fichier différent.
    - [x] Modifier `Common/Machines/Common/Enums/MachineEnums.cs`, `Contracts/ConfigurationContracts.cs`, `Dictionaries/HardwareModelCatalog.cs`, `Dictionaries/MachineCatalog.cs` et `EmulationGlobalUsings.cs` : ajouter Electron sans modifier les identités existantes.
    - [x] Créer `Emulators/AcornEmu/Functions/ElectronFunctions.cs` et ses partiels Firmware/Storage, ainsi que `Machines/AcornElectron/Common/Dictionaries/AcornEmuElectronModelCatalog.cs` ; modifier la fabrique, le catalogue et le cœur existants pour les réglages et les médias réellement disponibles.
    - [x] Modifier `Resources/00-Base/Model.resx` et les ressources linguistiques nécessaires : invariants dans la base commune, libellés traduits via Argos.
  - [x] Valider et livrer le bloc complet.
    - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuElectronTests.cs` : tests autonomes utiles du bus, de l’ULA, du clavier, de la contention et de la vidéo.
    - [x] Créer puis supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornElectronBootDiagnostic.cs` : démarrage ROM, BASIC, vidéo, son et aller-retour tape ; conserver les PNG de revue dans `artifacts/acorn-electron`, supprimer médias temporaires et nettoyer les ressources dans finally.
    - [x] Modifier les deux fichiers ElectronKeyboardMap.cs et ElectronKeyboard.cs : employer LeftControl et RightControl du contrat clavier existant, puis regrouper les corrections observées au diagnostic.
    - [x] Modifier les fichiers livrés sous `build/Debug` avec `scripts/local-building.cmd --building=debug --modules=A`, vérifier les 24 modules et le lanceur.
    - [x] Modifier ce suivi avec les preuves finales et créer le commit Electron avant le bloc Archimedes.

## Références de réalisation

- https://github.com/mamedev/mame/blob/master/src/mame/acorn/electron.cpp : clavier, ROM, bus et configuration Electron.
- https://github.com/mamedev/mame/blob/master/src/mame/acorn/electron_ula.cpp et `.h` : registres, palette, modes et interruptions ; les TODO de cette source ne constituent pas des fonctions déjà réalisées ici.
- https://github.com/dmcoles/elkulator/blob/master/src/ula.c : seconde implémentation indépendante des registres et des modes vidéo.
- Cadence de référence : CPU/ULA 2 MHz, 128 cycles par ligne, 312 lignes ; RAM synchronisée à 1 MHz, vidéo prioritaire en modes 0–3 pendant ses prélèvements. Modes 3/6 : caractères espacés, huit rasters affichés sur dix.
- ROM Electron combinée BASIC/MOS : 32768 octets, SHA-1 `4a66c83aba07d0a8e76ed8a5545af04e11c19fdc`. Les ROM BBC ne remplacent pas le MOS Electron.

- [x] Modifier tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornElectronBootDiagnostic.cs : qualifier les constantes Acorn pour lever le conflit avec les imports globaux Atari du projet de tests.

- [x] Modifier tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornElectronBootDiagnostic.cs : confirmer RECORD then RETURN avec RETURN après SAVE avant de vérifier le signal enregistré et son rechargement.

- [x] Modifier tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornElectronBootDiagnostic.cs : faire contrôler le WAV enregistré par le décodeur tape existant du SDK avant sa réception ULA ; supprimer le WAV temporaire dans finally.

- [x] Modifier Machines/AcornElectron/Common/Services/ElectronUla.Serial.cs et Constants/ElectronConstants.cs : signaler le registre émetteur disponible après les neuf bits start/données, au début du stop, conformément au compteur série Elkulator ; vérifier nom de fichier et CRC du signal par le décodeur indépendant.

- [x] Modifier tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuElectronTests.cs : vérifier le statut émetteur libre au repos, occupé après écriture et disponible au bit stop, pour couvrir le défaut observé avec le MOS.

- [x] Supprimer tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornElectronBootDiagnostic.cs après les deux diagnostics réels réussis ; reconstruire les tests sans ce diagnostic et vérifier les contrôles Acorn autonomes. Conserver uniquement les captures PNG dans artifacts/acorn-electron.

## Résultats du bloc

- 418 contrôles Acorn autonomes réussis après les corrections, sans diagnostic externe conservé.
- ROM réelle : démarrage Acorn Electron, calcul A%=123+456 et affichage 579, modes 0 à 6 et son vérifiés.
- SAVE/LOAD : signal enregistré contrôlé par le décodeur BBC existant (bloc et CRC valides), puis rechargé par le récepteur ULA et exécuté par le BASIC réel. Le statut émetteur devient disponible au stop, et au repos avant la première écriture.
- Firmware installé dans le dossier utilisateur : Acorn Electron BASIC MOS.bin, 32768 octets, SHA-1 4a66c83aba07d0a8e76ed8a5545af04e11c19fdc. Archive extraite en mémoire : https://raw.githubusercontent.com/Abdess/retrobios/main/bios/Acorn/BBC%20Micro/electron.zip .
- Captures : artifacts/acorn-electron/*.png. WAV temporaire supprimé dans finally ; diagnostic externe supprimé, assembly de tests reconstruit.
- La carte décrite est l’Electron 32K ; aucune extension Plus 1/Plus 3 ni disque inexistant n’est annoncée comme installée.


Debug All terminé (code 0), 24 manifestes et DLL vérifiés, lanceur présent et empreinte du module Acorn livré identique au module compilé. Exécutable : build/Debug/Retro Media Lab/retromedialab.exe.

