# Inventaire de l’extraction des puces Acorn

Les chemins concernent les implémentations existantes ; leur déplacement ne signifie pas que toutes leurs fonctions silicium sont déjà implémentées.

| Origine AcornEmu | Destination dans RetroMediaLab.Chips |
|---|---|
| `Components/AcornScsi/` | `Controllers/Acorn/Winchester/Scsi/` |
| `Components/Arm26/` | `CPU/ARM/ARMv2/ARM2/` |
| `Components/Ata/` | `Controllers/Generic/ATA/PIO/` |
| `Components/Hd63463/` | `Controllers/Hitachi/HDC/HD63463/` |
| `Components/Intel80186/` | `CPU/Intel/MCS80x86/80186/` |
| `Components/Intel8271/` | `Controllers/Intel/FDC/8271/` |
| `Components/Ioc/` | `IO/Acorn/Archimedes/IOC/` |
| `Components/Mc6809/` | `CPU/Motorola/M6800/6809/` |
| `Components/Mc6845/` | `Video/Motorola/M6845/6845/` |
| `Components/Mc6850/` | `IO/Motorola/M6800/6850/` |
| `Components/Mc6854/` | `Network/Motorola/M6800/6854/` |
| `Components/Memc/` | `Memory/Acorn/Archimedes/MEMC/` |
| `Components/Mos6502/` | `CPU/MOS/MCS6500/6502/` |
| `Components/Mos6522/` | `IO/MOS/MCS6500/6522/` |
| `Components/Mos6551/` | `IO/MOS/MCS6500/6551/` |
| `Components/Ns16450/` | `IO/NationalSemiconductor/UART/NS16450/` |
| `Components/Pcf8583/` | `Timers/Philips/PCF85xx/PCF8583/` |
| `Components/Saa5050/` | `Video/Philips/SAA50xx/SAA5050/` |
| `Components/Sn76489/` | `Audio/TexasInstruments/SN764xx/SN76489/` |
| `Components/Upc82C711/` | `IO/ChipsAndTechnologies/SuperIO/82C711/` |
| `Components/Upd765/` | `Controllers/NEC/UPD765/UPD765/` |
| `Components/Vidc/` | `Video/Acorn/Archimedes/VIDC/` |
| `Components/Wd1770/` | `Controllers/WesternDigital/WD177x/WD1770/` |
| `Components/Common/` | `CPU/Common/` |
| `Machines/AcornAtom/Common/Services/AtomPpi8255.cs` | `IO/Intel/MCS80/8255/Services/AtomPpi8255.cs` |
| `Machines/AcornAtom/Common/Services/AtomVideo.cs` | `Video/Motorola/M6847/6847/Services/AtomVideo.cs` |
| `Machines/AcornAtom/Common/Constants/AtomPpiConstants.cs` | `IO/Intel/MCS80/8255/Constants/AtomPpiConstants.cs` |
| `Machines/AcornAtom/Common/Constants/AtomVideoConstants.cs` | `Video/Motorola/M6847/6847/Constants/AtomVideoConstants.cs` |
| `Machines/AcornAtom/Common/Dictionaries/AtomCharacterGenerator.cs` | `Video/Motorola/M6847/6847/Dictionaries/AtomCharacterGenerator.cs` |
| `Machines/AcornSystem/Common/Services/RamIo8154.cs` | `IO/NationalSemiconductor/INS815x/8154/Services/RamIo8154.cs` |
| `Machines/AcornSystem/Common/Constants/RamIoConstants.cs` | `IO/NationalSemiconductor/INS815x/8154/Constants/RamIoConstants.cs` |
| `Machines/AcornElectron/Common/Services/ElectronUla.cs` | `Custom/Acorn/Electron/ULA/Services/ElectronUla.cs` |
| `Machines/AcornElectron/Common/Services/ElectronUla.Serial.cs` | `Custom/Acorn/Electron/ULA/Services/ElectronUla.Serial.cs` |
| `Machines/AcornElectron/Common/Services/ElectronVideo.cs` | `Custom/Acorn/Electron/ULA/Services/ElectronVideo.cs` |
| `Machines/AcornElectron/Common/Constants/ElectronVideoConstants.cs` | `Custom/Acorn/Electron/ULA/Constants/ElectronVideoConstants.cs` |
| `Machines/BbcMaster/Common/Services/MasterClock.cs` | `Timers/Hitachi/HD1468xx/HD146818/Services/Hd146818.cs` |
| `Machines/BbcMaster/Common/Constants/MasterClockConstants.cs` | `Timers/Hitachi/HD1468xx/HD146818/Constants/MasterClockConstants.cs` |
| `Machines/BbcMaster/Common/Services/CompactEeprom.cs` | `Memory/Generic/I2C/EEPROM128/Services/CompactEeprom.cs` |
| `Machines/BbcMaster/Tube/Services/TubeUla.cs` | `IO/Acorn/Tube/ULA/Services/TubeUla.cs` |
| `Machines/BbcMaster/Tube/Constants/TubeConstants.cs` | `IO/Acorn/Tube/ULA/Constants/TubeConstants.cs` |
| `Machines/BbcMaster/Tube/Services/Master512Refresh.cs` | `Custom/Acorn/Master512/Refresh/Services/Master512Refresh.cs` |
| `Machines/BbcMicro/Common/Services/Adc7002.cs` | `IO/NEC/UPD700x/UPD7002/Services/Adc7002.cs` |
| `Machines/BbcMicro/Common/Services/Video.cs` | `Video/Acorn/BbcMicro/VideoULA/Services/Video.cs` |
| `Machines/BbcMicro/Common/Constants/VideoConstants.cs` | `Video/Acorn/BbcMicro/VideoULA/Constants/VideoConstants.cs` |

## Frontières conservées

- Les services de transport cassette et réseau hôte, les conteneurs de médias, les cartes mémoire, la carte mère et le décodage des adresses restent dans le module.
- Le rendu sept segments du panneau System et les tables de correspondance du clavier sont des présentations/câblages de machine, pas des puces supplémentaires.
- Le contrôleur de protocole du clavier Archimedes sera séparé de sa traduction des touches hôtes et extrait également.
- Les éléments ULA BBC encore combinés avec le balayage constituent un circuit vidéo personnalisé ; ils sont rangés sous Video, sans prétendre émuler une nouvelle puce distincte.
- La logique de rafraîchissement Master512 correspond au câblage existant de bascules ; elle est classée comme circuit personnalisé, pas comme une implémentation complète inventée de la famille 74LS.
- ATA PIO et EEPROM I2C 128 octets restent identifiés Generic lorsque le code ne distingue pas un modèle de silicium précis.
- Le moteur MOS couvre actuellement les variantes NMOS/CMOS sélectionnées par ses paramètres ; le moteur ARM couvre ARM2/ARM3 et WD1770 couvre aussi WD1772. Pas de copies de moteurs ni de variantes annoncées sans implémentation.

## Contrats à découpler

- Bus CPU et signaux électriques : contrats propres à Chips ; les classes de machine les implémentent.
- Disques : broches/secteurs/mécanique consommés par les contrôleurs dans Chips ; propriété, ouverture, conversion et persistance restent dans Emulation.
- ADC : valeurs électriques des canaux dans Chips ; conversion des manettes hôtes et boutons de tir dans Acorn.
- ULA Electron : broches cassette dans Chips ; magnétophone et réglages utilisateur dans Acorn/Emulation.
- Constantes matérielles utilisées par les puces : déplacer leur définition propriétaire ; conserver les configurations des machines et leurs références côté module.

## Validation

Les résultats des tests et de packaging seront ajoutés après leur exécution.


### Extraction complémentaire

- Le contrôleur clavier Archimedes, ses états et constantes : `IO/Acorn/Archimedes/Keyboard/` ; la correspondance des touches hôtes reste dans le module.
- Le calcul CRC/ECC du HD63463 : `Controllers/Hitachi/HDC/HD63463/Functions/`.
- Le HD146818 est séparé du câblage CMOS Master et de ses valeurs d'usine, qui restent dans Acorn.
- Les valeurs des canaux analogiques sont fournies à l'ADC ; les manettes hôtes et boutons sont traités par la carte BBC.
- La bibliothèque ne référence aucun autre projet RML. App n'a aucune référence ni connaissance de Chips ; Emulation transporte les contrats de média nécessaires aux modules.

### Tests déplacés

Les tests autonomes sont regroupés par puce dans `tests/RetroMediaLab.Chips.Tests` : CPU MOS/Intel/Motorola/ARM, VIA/ACIA/UART/PPI/RAM-I/O, Tube, EEPROM, CMOS, IOC/MEMC/VIDC, clavier, contrôleurs ATA/NEC/HD63463 et générateur audio. Les tests de machines, de ROM et des formats de fichiers restent dans les projets d’intégration existants. Les aides NEC communes aux tests d’intégration sont liées depuis un fichier unique, sans référence vers une assembly de tests.

Debug : 769 tests Chips réussis. Release : 769 tests réussis également. Les anciens diagnostics dépendant de firmwares et d’images externes ne sont pas repris dans cette extraction, conformément à la demande.

### Build et distribution vérifiés

- `scripts/local-building.cmd --building=debug --modules=A` : succès ; exécutable et 24 DLL de modules vérifiés. Une seule DLL Chips, sous `build/Debug/Retro Media Lab/lib/`.
- Chargement du module Acorn avec le chargeur existant : ses 569 types sont résolus ; le bus CPU provient de l’assembly Chips du contexte par défaut, située dans `lib`. Aucun changement au chargeur App ni référence directe App → Chips.
- `scripts/release/package/package.ps1 -Version 0.4.1 -Configuration Release` avec un dossier temporaire isolé : ZIP portable et installateur Inno Setup construits. Une seule DLL Chips dans le ZIP, à l’emplacement `lib/retromedialab.chips.dll`.
- `scripts/module-release/package-module.ps1 -Module acorn -Configuration Release` : ZIP Acorn construit ; aucune copie privée de Chips embarquée.
- `dotnet pack src/RetroMediaLab.Emulation/RetroMediaLab.Emulation.csproj --configuration Release` : paquet SDK créé ; DLL Chips présente pour compilation et exécution, sans dépendance NuGet vers un paquet Chips non publié.
- Test local de l’installateur : arrêté par sa protection contre le remplacement d’une installation déjà enregistrée. Aucun remplacement effectué. L’installateur est compilé ; les contrôles de présence de Chips sont ajoutés aux tests d’installation et de mise à niveau existants, exécutés en CI sur un environnement propre.
- Workflows release, module et SDK : tests Chips ajoutés avant le packaging. Aucun workflow distant ni publication déclenché.
- Les outils de déplacement, bancs et paquets de vérification temporaires sont supprimés après validation. Les anciens diagnostics matériels externes restent hors de ce travail.
