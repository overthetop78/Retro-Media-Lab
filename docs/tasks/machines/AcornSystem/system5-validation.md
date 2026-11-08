# Acorn System 5 — carte rapide 6502A et VDU80

- [x] Terminer le montage System 5 et la livraison groupée.
  - [x] Corriger le décodage DRAM établi par le manuel.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Functions/SystemFunctions.cs` : déplacer les 48 Kio DRAM de 0000–BFFF vers 2000–DFFF ; préserver RAM CPU et I/O.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Common/Machines/AcornSystem/Constants/ModelConstants.cs` : total 48 Kio DRAM + 1 Kio RAM CPU.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemRamIoTests.cs` : vérifier limites DRAM, BASIC C000, trou hors RAM, total configuré, sans ROM externe.
  - [x] Vérifier la carte complète et terminer le banc commun.
    - [x] Modifier `artifacts/.tmp-system3/Program.cs`, option --system5 : vrais firmwares, catalogue, BASIC chargé/exécuté, SAVE/LOAD, CAT1 sur second port, mesure globale Debug et captures ; libération en finally.
    - [x] Créer `artifacts/TestsMachines/Acorn/AcornSystem/systems-final-debug.trx` par les tests System existants et la régression DRAM ajoutée, contrôler les résultats.
    - [x] Supprimer `artifacts/.tmp-system3/` après vérification du chemin et consignation ; conserver captures, archives sources et médias de référence.
  - [x] Livrer la famille pour l'essai groupé demandé.
    - [x] Créer les fichiers de `build/Debug/Retro Media Lab/` par `scripts/local-building.cmd --building=debug --modules=A` hors bac à sable ; vérifier exécutable, Chips et les 24 modules.

- [x] Vérifier System 5 dans RML Debug.
  - [x] Vérifier le montage VDU80 et les logiciels invités.
    - [x] Créer les captures `artifacts/TestsMachines/Acorn/Acorn System 5/20261108-rml-*` : NOS80/VDU80, lecteurs ss80, catalogue et BASIC chargé depuis la disquette, calculs clavier/pavé, second port et cadence de 2 MHz ; distinguer le signal progressif de la VDU40 et conserver l'image de référence.

- [ ] Compléter les sons des Acorn System après les essais de la famille.
  - [ ] Vérifier le raccordement existant et les directives complètes.
    - [ ] Modifier les fichiers audio et de raccordement Acorn System concernés après comparaison des chemins existants : compléter uniquement les sons de machine et de lecteurs manquants, déclenchés par les événements, avec volumes et muet effectifs ; consigner les vérifications et corrections dans les fiches des System concernés.
