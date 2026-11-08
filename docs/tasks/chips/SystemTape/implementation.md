# Carte cassette Acorn 200.001 / 100.001

- [x] Terminer le bloc matériel partagé System 1 / System 2.
  - [x] Implémenter le circuit documenté, sans sondage entre composants.
    - [x] Créer `src/RetroMediaLab.Chips/Controllers/Acorn/SystemTape/Constants/SystemTapeConstants.cs` et `Services/SystemTapeInterface.cs` : compteurs IC1 modulo 26, IC2 et IC6 sept bits, bascules et niveaux conservés, entrées horodatées, notifications et échéance suivante ; arithmétique exacte des fronts Q3 et des sorties.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/AnalogTape.cs` pour publier les changements de comparateur PCM seulement ; câbler la carte et ses échéances dans `Machines/AcornSystem/Common/Services/AcornSystemMachine.cs` via les API existantes de transport et d'INS8154.
  - [x] Vérifier le bloc et ses propriétaires.
    - [x] Créer `tests/RetroMediaLab.Chips.Tests/Controllers/Acorn/SystemTapeTests.cs` avec les attentes de schéma indépendantes du code : diviseur 26, sélection /16 ou /32, fronts réception/Q7, reset, événements et avancement groupé.
    - [x] Modifier les bancs temporaires sous `artifacts/.tmp-system2/` pour SAVE/LOAD COS et la régression du moniteur System 1, avec ROM externes et nettoyage en finally ; conserver captures et résultats sous TestsMachines et TestsChips.
    - [x] Supprimer les seuls bancs externes et leurs sorties de compilation après consignation et vérification des chemins ; conserver les preuves utiles ignorées par Git.
  - [x] Compléter le contrat public imposé aux composants de la bibliothèque.
    - [x] Créer `src/RetroMediaLab.Chips/Controllers/Acorn/SystemTape/Interfaces/ISystemTapeInterface.cs` avec les horloges, entrées, sorties et notifications publiques existantes, puis modifier SystemTapeInterface.cs pour l'implémenter ; vérifier les tests d'architecture communs.

Contrat SystemTape vérifié : les tests globaux signalent encore 14 classes ACB-4000/SCSI inchangées sans interface dédiée ; aucun défaut SystemTape restant dans ce contrôle.

- [x] Supprimer les appels au transport arrêté, sans arrêter la carte Tape matérielle.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/AcornSystemMachine.cs` : mémoriser l'activité via `AnalogTape.ActivityChanged` et appeler son transport uniquement pendant lecture, enregistrement ou déplacement ; conserver les échéances SystemTapeInterface.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornAtom/Common/Services/AtomMachine.cs` : recevoir la même activité et supprimer les appels du transport arrêté ; conserver le générateur Tape, la vidéo et l'audio matériels.

## Noms techniques — 9 novembre 2026

- [x] Respecter le nom Tape dans le contrat ajouté après le renommage général.
  - [x] Modifier `src/RetroMediaLab.Chips/Controllers/Acorn/SystemTape/Services/SystemTapeInterface.cs`, `Interfaces/ISystemTapeInterface.cs`, `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/AcornSystemMachine.cs` et `tests/RetroMediaLab.Chips.Tests/Controllers/Acorn/SystemTapeTests.cs` : renommer CassetteInput, CassetteOutput et CassetteOutputChanged en TapeInput, TapeOutput et TapeOutputChanged, sans alias de compatibilité et sans changement matériel.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemRamIoTests.cs` : adapter les quatre lectures de TapeOutput au contrat renommé.
  - [x] Modifier les résultats générés des tests existants en exécutant le groupe SystemTape puis les contrôles System concernés ; consigner leurs résultats dans les fiches.

Résultats : 10 contrôles SystemTape Debug réussis (87 ms), puis 72 contrôles System/RAM-E/S (1 s), zéro échec/ignoré. Aucun nouveau test ajouté.
