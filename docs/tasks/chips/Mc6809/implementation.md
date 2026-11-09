# MC6809 — entrées matérielles conservées

- [x] Terminer les liaisons d'interruption sans sondage.
  - [x] Modifier les entrées et leurs propriétaires existants.
    - [x] Modifier `src/RetroMediaLab.Chips/CPU/Motorola/M6800/6809/Services/Cpu6809.cs` : ajouter SetIrq/SetNmi/SetFirq et les niveaux conservés ; Tick échantillonne NMI localement, Reset lit ce niveau mémorisé, supprimer le callback Firq.
    - [x] Modifier `Cpu6809.Interrupts.cs` dans le même dossier : arbitrer sur les niveaux locaux, conserver masques, vecteurs, piles, SYNC/CWAI et cycles.
    - [x] Modifier `src/RetroMediaLab.Chips/CPU/Motorola/M6800/6809/Interfaces/ICpu6809.cs` : exposer SetIrq/SetNmi/SetFirq à la place du fournisseur Firq, en conservant les autres membres du contrat.
    - [x] Modifier le propriétaire `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/AcornSystemMachine.cs` pour raccorder les sorties réelles VIA/8271 et initialiser IRQ/NMI ; aucun nouveau routage par le SDK.
  - [x] Vérifier le CPU et sa carte réelle.
    - [x] Modifier `tests/RetroMediaLab.Chips.Tests/CPU/Motorola/M6800/6809/6809Tests.cs` et `SystemInstructionTests.cs` : notifications des bus de tests, FIRQ par entrée, contrôle d'absence de lecture des fournisseurs, attentes matérielles de cycles et piles conservées.
    - [x] Créer `artifacts/.tmp-system3/Program.cs` avec le vrai moniteur 6809 et ses commandes de mémoire/exécution ; conserver captures, données et durée de la carte complète ; nettoyage suivi dans le bloc System 3.
  - [x] Corriger le décompte CWAI établi par le démarrage disque.
    - [x] Modifier `src/RetroMediaLab.Chips/CPU/Motorola/M6800/6809/Services/Cpu6809.Instructions.cs` et `Cpu6809.Interrupts.cs` : séparer les 16 cycles précédant l'attente des quatre cycles de vectorisation ; les 20 cycles minimum comprennent déjà la sortie d'attente, sans ajouter sept cycles à vingt.
    - [x] Modifier `tests/RetroMediaLab.Chips.Tests/CPU/Motorola/M6800/6809/6809Tests.cs` : vérifier séparément empilement, vectorisation et total minimum de vingt cycles, puis relancer le bootstrap disque réel du banc existant.

## Revue statique de complétude — 8 novembre 2026

La revue des six fichiers de service et du contrat établit une absence réelle : aucun HALT, DMA/BREQ, MRDY ni sortie BA/BS ou phases E/Q n'est exposé. IRQ/FIRQ/NMI et CWAI ne couvrent pas ces broches. Référence locale : `artifacts/.tmp-archives/chips/CPU/Motorola/MC6809/MC6809-Motorola.txt`, rubriques HALT, BUS AVAILABLE/BUS STATUS, XTAL/EXTAL, E/Q et MRDY. Le CPU n'est donc pas entièrement réalisé. Ne pas présenter le seul démarrage du moniteur comme preuve de complétude.

- [x] Ajouter le code des commandes et sorties matérielles absentes du MC6809, sans changer la carte System pour masquer ces absences.
  - [x] Créer `src/RetroMediaLab.Chips/CPU/Motorola/M6800/6809/Services/Cpu6809.Pins.cs`, modifier `Interfaces/ICpu6809.cs` et `Services/Cpu6809.cs` : entrée HALT retenue, arrêt à la fin de l'instruction, cycle mort de reprise, poursuite E/Q et échantillonnage NMI ; BA/BS établis à E bas et valides à Q montant, phases E/Q propres au MC6809 à quatre périodes source par cycle bus. Conserver le chemin direct lorsque ces sorties ne sont pas raccordées.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Motorola/M6800/6809/Services/Cpu6809.Interrupts.cs`, `Cpu6809.Unofficial.cs`, `Cpu6809.Instructions.cs` et `Cpu6809.cs` : publier BA/BS pour les deux cycles de vecteur et pour SYNC ; conserver octets/priorités et compléter le cycle mort de sortie SYNC décrit par la reprise BA.
  - [x] Réaliser les entrées de maintien et l'arbitrage du MC6809, sans les confondre avec les broches différentes du MC6809E.
    - [x] Créer `src/RetroMediaLab.Chips/CPU/Motorola/M6800/6809/Interfaces/ICpu6809ClockBus.cs` et modifier `Services/Cpu6809.Pins.cs` : fournir l'avancement de la référence EXTAL au câblage nécessitant les quarts de période, conserver le chemin bus existant à périodes complètes pour les cartes sans ce raccordement ; ne pas appeler les deux chemins pour la même période.
    - [x] Modifier `Services/Cpu6809.Pins.cs`, `Services/Cpu6809.cs` et `Interfaces/ICpu6809.cs` dans le même dossier de puce : strobage des sorties de bus à Q montant, RESET maintenu et annulation de transfert, suspension DMA avant chaque cycle CPU avec cycles morts et rafraîchissement, MRDY étirant E/Q sans rejouer les transferts ; séparer les cycles CPU utiles du temps passé en bus libéré pour ne pas supprimer le reste de l'instruction. `Cpu6809.Addressing.cs` n'a pas à être modifié : ses accès utilisent déjà les méthodes centrales corrigées.

Revue statique demandée : aucun test/build lancé. Les résultats déjà consignés restent des preuves des fonctions exercées, pas de ces signaux absents.

- [x] Vérifier le raccordement des phases ajouté dans les fichiers de la puce contre la référence locale.
  - [x] Créer les images `artifacts/.tmp-archives/chips/CPU/Motorola/MC6809/MC6809-Motorola-timing-13.png` et `MC6809-Motorola-timing-14.png` depuis les pages correspondantes de la fiche archivée : conserver les chronogrammes MRDY/DMA/rafraîchissement consultés avec leur source, sans téléchargement ni banc temporaire.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Motorola/M6800/6809/Constants/Cpu6809Constants.cs` et `Services/Cpu6809.Pins.cs` : fixer le diviseur source, la fenêtre de rafraîchissement et les séquences aux fronts réellement décrits. La lecture visuelle de la figure 12 a corrigé la candidate qui maintenait Q haut : l'étirement réel conserve E haut/Q bas. La figure 14 maintient quatorze périodes DMA entre deux cycles morts.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Motorola/M6800/6809/Services/Cpu6809.cs`, `Cpu6809.Pins.cs`, `Cpu6809.Interrupts.cs` et `Constants/Cpu6809Constants.cs` : synchroniser IRQ/FIRQ/NMI à Q descendant avec la période de synchronisation Motorola ; inhiber leur reconnaissance/mémorisation entre RESET descendant et BS montant d'acquittement ; mémoriser les étages et n'avancer leurs niveaux que lorsqu'un changement reçu reste à propager, sans consulter le bus.

- [x] Vérifier les broches ajoutées après la revue statique en cours.
  - [x] Modifier les tests autonomes `tests/RetroMediaLab.Chips.Tests/CPU/Motorola/M6800/6809/6809Tests.cs` et `SystemInstructionTests.cs`, puis créer `Cpu6809PinsTests.cs` dans ce dossier : tenir compte de la phase d'arrivée IRQ/FIRQ et couvrir HALT, RESET, DMA/rafraîchissement, MRDY, niveaux du bus, acquittement et synchronisation, sans ROM externe. Reprise de validation : compilation réussie, premier passage 71/72 ; l'équivalence des chemins E/Q révèle un décalage d'une période EXTAL.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Motorola/M6800/6809/Services/Cpu6809.Pins.cs` : placer l'appel de carte du chemin optimisé à la même période EXTAL que le chemin détaillé, avant Q descendant ; conserver les niveaux E/Q observés et le nombre de cycles.
  - [x] Compléter `tests/RetroMediaLab.Chips.Tests/CPU/Motorola/M6800/6809/Cpu6809PinsTests.cs` : vérifier la préparation de transactions identiques malgré l'absence de changement de broches, puis exécuter le bloc MC6809 et conserver son rapport dans `artifacts/TestsChips/MC6809/Acorn System 3/`.

État après reprise de validation : code compilé ; 73 tests autonomes MC6809 réussis en Debug (79 ms), dont douze nouveaux cas de broches/phases. Rapport `artifacts/TestsChips/MC6809/Acorn System 3/mc6809-pins-debug.trx`. Les tests n'utilisent aucune ROM ni application externe. Cette vérification ne clôture pas la preuve en machine et ne mesure aucun gain de performance. Aucun commit de puce terminée ; le travail Atom conservé dans l'arbre n'est pas déclaré clos.

- [x] Compléter la présentation physique du bus pendant RESET.
  - [x] Créer `artifacts/.tmp-archives/chips/CPU/Motorola/MC6809/MC6809-Motorola-timing-07.png` depuis la page 7 du PDF archivé : inspecter la figure 6 et ses cycles FFFE avant les deux octets du vecteur. La note 3 impose trois cycles FFFE après la libération physique de RESET, puis les deux cycles de vecteur et le cycle FFFF/VMA ; l'initialisation directe existante inclut un cycle de plus avant cette libération.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Motorola/M6800/6809/Services/Cpu6809.Pins.cs`, `Cpu6809.cs` et `Constants/Cpu6809Constants.cs` selon ce chronogramme : présenter l'adresse RESET sur les périodes requises, sans transformer les cycles sans transfert en lectures mémoire ; conserver la libération du bus sous HALT.
  - [x] Modifier `tests/RetroMediaLab.Chips.Tests/CPU/Motorola/M6800/6809/Cpu6809PinsTests.cs` et les attentes RESET concernées dans `6809Tests.cs` : vérifier adresses et BA/BS, maintien, libération et absence de transferts supplémentaires ; conserver le rapport du passage dans le dossier MC6809/System 3. Deux cas ajoutés dans `Cpu6809PinsTests.cs` suffisent ; les attentes de l'initialisation directe dans `6809Tests.cs` restent valides. Passage 75/75, 80 ms ; `mc6809-reset-bus-debug.trx`.

- [x] Compléter les preuves fonctionnelles des familles ALU, chargements/écritures, branchements et adressages documentés ci-dessous.
  - [x] Créer `tests/RetroMediaLab.Chips.Tests/CPU/Motorola/M6800/6809/Cpu6809InstructionMatrixTests.cs` : exercer les opérations ALU/chargements/écritures 8 et 16 bits dans leurs quatre adressages, les pages 10/11, les cas de retenue/signe/débordement et tous les branchements avec indicateurs contrôlés ; vérifier résultats, registres préservés, octets mémoire et cycles contre les tables Motorola archivées.
  - [x] Compléter ce même fichier avec les adressages indexés documentés : offsets signés, auto-incréments/décréments, D/A/B, PC relatif, indirection et rebouclage 16 bits ; contrôler adresse et cycles supplémentaires.

Passage suivant : 80/80 tests autonomes MC6809 réussis en Debug, 95 ms ; `artifacts/TestsChips/MC6809/Acorn System 3/mc6809-instruction-matrix-debug.trx`. Les cinq nouveaux cas de matrice exécutent plusieurs combinaisons matérielles chacun. Cette étape couvre les familles nommées, pas encore une clôture de toutes les instructions et broches ni la preuve en machine. La comparaison BBC Micro/Master demandée ensuite est une revue de câblage ; elle ne clôture pas ce CPU et ne justifie pas de reprendre le Compact.


## Reprise System 3/6809 — 9 novembre 2026

- [x] Terminer la revue des fonctions encore ouvertes avant l'essai de carte.
  - [x] Modifier `tests/RetroMediaLab.Chips.Tests/CPU/Motorola/M6800/6809/Cpu6809InstructionMatrixTests.cs` : ajouter les 256 masques des piles S/U (ordre big-endian, contenu, nombre de cycles et remontée du pointeur) et toutes les paires documentées de registres de même largeur pour TFR/EXG, avec PC après lecture du post-octet ; réutiliser le bus autonome existant et exécuter le groupe MC6809 hors états.
  - [x] Modifier `docs/chips/CPU/Mc6809.md` : consigner les fichiers d'instructions/adressages/interruptions/broches relus, les références locales utilisées, les fonctions vérifiées et les résultats obtenus, sans assimiler le nombre de tests à une preuve de toutes les combinaisons électriques possibles.

Lecture de référence : Motorola MC6809/MC6809E, rubriques MRDY et E/Q, page imprimée 12. MRDY n'étire pas les cycles VMA sans accès valide ; le prédicat existant ne doit donc pas être remplacé par un étirement systématique. Temps EXTAL, périodes E et cycles actifs restent distincts dans la puce. Aucun défaut de production établi dans les fichiers relus à ce stade.

Passage actuel : 84 contrôles MC6809 réussis en Debug, 99 ms ; programme invité au moniteur System 3 exécuté, trois relevés après correction de carte consignés dans la fiche. La vérification actuelle du bootstrap reste séparée du moniteur et des preuves historiques.

Bootstrap actuel System 3 confirmé : deux secteurs, 512 octets lus, programme chargé vérifié, 2010=5A et BOOT OK. Capture native actuelle inspectée, détail et limites du média dans les fiches CPU/machine. Les preuves historiques ne sont pas utilisées à la place de ce passage.
