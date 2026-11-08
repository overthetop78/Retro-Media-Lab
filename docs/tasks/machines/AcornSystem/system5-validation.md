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
  - [x] Vérifier le raccordement existant des lecteurs et les directives applicables.
    - [x] Modifier les fichiers audio et de raccordement Acorn System concernés seulement si la comparaison établit un manque : compléter uniquement les sons manquants, déclenchés par les événements, avec volumes et muet effectifs ; consigner les vérifications. Le contrôle ne révèle pas de manque de raccordement des lecteurs : aucune modification de production justifiée.
    - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuAudioTests.Systems.cs` : contrôler sans firmware externe les canaux Audio effectivement présents pour System 1–5 et la variante 6809, les valeurs initiales et les commandes volume/muet transmises à chaud aux mécaniques des lecteurs ; vérifier l'absence de reconfiguration pendant les ticks internes.
    - [x] Créer `artifacts/TestsMachines/Acorn/AcornSystem/system-audio-audit-debug.trx` par les tests System et Audio utiles, après le bloc ; conserver les tests autonomes et vérifier les résultats.

Contrôle du 8 novembre après essais System 3–5 : 82 tests Debug réussis, zéro échec, dont 10 nouveaux cas autonomes. Les deux premiers échecs provenaient de la fixture qui raccordait à tort des disquettes aux System 1/2 ; fixture corrigée, sans changer la validation de production. Les canaux suivent les équipements réellement présents ; chaque volume initial vaut 10. Les commandes volume/muet atteignent séparément les deux mécaniques sur System 3 (6502 et 6809), 4 et 5, sans reconfiguration pendant 1 000 clocks de carte.

Les raccordements existants ont été suivis : `EmulatorCore.Initialize` branche `TapeTransport.ActivityChanged`, `EmulatorCore.TapeSound.cs` transmet les changements au lecteur hôte, `EmulatorCore.Disks.cs` transmet les mécaniques aux images et `Intel8271Drive.Synchronize` émet moteur/accès/seek aux changements de sorties. Dix fichiers FLAC Tape et disquette attendus sont présents dans le build livré. Aucun manque de raccordement des lecteurs n'est établi par ce contrôle ; aucun son fictif n'est ajouté. Le son propre au boîtier demandé reste à préciser avec l'utilisateur, et l'écoute physique de chaque lecteur n'est pas prétendue vérifiée par ces tests.

Les deux cas existants `TapeAndHardDiskHotVolumesNotifyOnlyOnCommandsAndPreserveChipPcm` passent aussi en Debug, rapport `system-audio-common-options-debug.trx` : volume/muet communs ne réinjectent pas de mécanique dans le PCM invité.

- [ ] Déterminer et compléter les sons propres aux machines demandés.
  - [ ] Identifier le son attendu et son matériel avant toute implémentation.
    - [ ] Compléter `docs/project/emulations/Acorn/Acorn System/System 5/validation.md` avec la réponse sur le bruit de boîtier/ventilateur ou signal électronique attendu, les modèles concernés et le raccordement réel à réutiliser ; ajouter les actions de production précises si un manque est établi.
