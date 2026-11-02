# Rangement des composants et commits demandés



- [ ] Ranger les composants avant les commits, sans mélanger composants et assemblages de machines.

  - [ ] Déplacer les composants communs.

    - [x] Déplacer les fichiers `Cpu6502*`, `Cpu6809*`, `Cpu80186*`, `CpuArm26*`, `Via6522*`, `Acia6850*`, `Mc6854*`, `Mos6551*`, `Ns16450*`, `Intel8271*`, `Wd1770*`, `Upd765*`, `Upc82C711*`, `Hd63463*`, `AtaPio*`, `AcornScsi*`, `Crtc6845*`, `Sn76489*` et `TeletextSaa5050*` de `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services` vers `Components/<composant>/Services`. Déplacer leurs constantes/enums/interfaces associés des dossiers racine vers `Components/<composant>/{Constants,Enums,Interfaces}`. Conserver `IAcornMachine`, les transports cassette/réseau et `EmulatorCore` dans leurs dossiers actuels.

    - [x] Déplacer les composants MEMC/IOC/VIDC/PCF8583 et leurs constantes/enums depuis `Machines/Archimedes/Common` vers `Components/{Memc,Ioc,Vidc,Pcf8583}` ; conserver les machines, leur clavier et le câblage sous `Machines`.

    - [x] Modifier les namespaces des fichiers déplacés et les références C# dans `src` et `tests` pour correspondre à l'arborescence, sans compatibilité parallèle ni classes dupliquées. Mettre les nouveaux imports uniquement dans les fichiers qui référencent les types déplacés.

  - [ ] Terminer et valider les corrections VIA engagées.

    - [x] Modifier `Components/Mos6522/Services/Via6522*.cs` et créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuVia6522Tests.cs` : vérifier et corriger le seuil de T2 en mode impulsions, les acquittements indépendants, les sorties et les latches.

    - [x] Modifier ce suivi avec les résultats de compilation et du contrôle Acorn groupé ; corriger les références de déplacement restantes ensemble avant les commits.

  - [ ] Réduire les fichiers en attente par des commits séparés.

    - [x] Modifier l'index Git et créer un commit des corrections cassette Atom TAP/UEF et du libellé BREAK, avec leurs tests autonomes et les seuls raccordements nécessaires.

    - [ ] Modifier l'index Git et créer un commit des machines Archimedes/A3000 et de leur infrastructure actuelle, portant explicitement la mention `in-progress`, comme demandé par l'utilisateur ; conserver les limites du suivi et ne pas les déclarer terminées.

    - [ ] Modifier l'index Git et créer les commits indépendants des corrections de démarrage et du VIA ; vérifier l'historique et les fichiers restant en attente après les commits. Les machines System/Atom/Electron sont déjà dans les commits 8224c1751/0ac0700d4/cd5243b6d et ne doivent pas être recréées.



Rangement réalisé : 118 fichiers déplacés, références ajustées dans les sources et tests. Compilation corrigée pour la référence MEMC aux constantes de carte. Contrôle groupé : 972 tests AcornEmu réussis en 16 s. Commit cassette/BREAK : 910a25983.



Commits créés : 910a25983 (cassette/BREAK), cbaa7056a (Archimedes/A3000 et rangement, explicitement in-progress), 7ff372e6f (démarrage et contrôleurs installés). Le correctif VIA est préparé séparément après les 972 tests réussis. Les points matériels encore ouverts restent dans le suivi Archimedes et l'audit des composants.
