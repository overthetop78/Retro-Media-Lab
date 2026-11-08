# Carte cassette Acorn 200.001 / 100.001

- [x] Terminer le bloc matériel partagé System 1 / System 2.
  - [x] Implémenter le circuit documenté, sans sondage entre composants.
    - [x] Créer `src/RetroMediaLab.Chips/Controllers/Acorn/SystemTape/Constants/SystemTapeConstants.cs` et `Services/SystemTapeInterface.cs` : compteurs IC1 modulo 26, IC2 et IC6 sept bits, bascules et niveaux conservés, entrées horodatées, notifications et échéance suivante ; arithmétique exacte des fronts Q3 et des sorties.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/AnalogTape.cs` pour publier les changements de comparateur PCM seulement ; câbler la carte et ses échéances dans `Machines/AcornSystem/Common/Services/AcornSystemMachine.cs` via les API existantes de transport et d'INS8154.
  - [x] Vérifier le bloc et ses propriétaires.
    - [x] Créer `tests/RetroMediaLab.Chips.Tests/Controllers/Acorn/SystemTapeTests.cs` avec les attentes de schéma indépendantes du code : diviseur 26, sélection /16 ou /32, fronts réception/Q7, reset, événements et avancement groupé.
    - [x] Modifier les bancs temporaires sous `artifacts/.tmp-system2/` pour SAVE/LOAD COS et la régression du moniteur System 1, avec ROM externes et nettoyage en finally ; conserver captures et résultats sous TestsMachines et TestsChips.
    - [x] Supprimer les seuls bancs externes et leurs sorties de compilation après consignation et vérification des chemins ; conserver les preuves utiles ignorées par Git.
