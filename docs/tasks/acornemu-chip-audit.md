# Audit matériel AcornEmu — composants et câblages



- [ ] Confronter tous les composants utilisés aux documents constructeur et aux sources d'émulateurs, puis corriger les écarts.

  - [x] Compléter le VIA 6522 en cours.

    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Components/Mos6522/Constants/Via6522Constants.cs` : constantes distinctes des registres, masques, modes et broches, une déclaration par ligne.

    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Components/Mos6522/Enums/ViaControlMode.cs` : huit modes PCR nommés, distincts des bits encodés dans chaque moitié du registre.

    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Components/Mos6522/Services/Via6522.cs` et créer `Components/Mos6522/Services/Via6522.Ports.cs` : séparer registres et broches, entrées CA2/CB2, modes indépendants d'interruption, impulsions/handshake, latches d'entrée et PB6 de T2 ; conserver les callbacks des cartes existantes et leur propriété des périphériques.

    - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuVia6522Tests.cs` : cas autonomes des modes PCR/ACR, fronts, acquittements, broches et comptage PB6. Contrôler ce groupe après les modifications regroupées ; aucun fichier externe.

  - [ ] CPU et bus des machines.
    - [x] Créer `Components/Mos6502/Constants/Cpu6502Constants.cs` et modifier `Components/Mos6502/Services/Cpu6502.cs`, `Cpu6502.Instructions.cs`, `Cpu6502.Interrupts.cs`, `Cpu6502.Cmos.cs` : cycles de bus explicites pour les adressages indexés, branchements, pile et vecteurs ; reconnaissance IRQ de CLI/SEI/PLP conforme au NMOS et différences CMOS conservées.
    - [x] Créer `Components/Mos6502/Services/Cpu6502.Bus.cs` : aides communes d'adressage et accès transitoires NMOS, en distinguant la lecture supplémentaire d'indexation des cycles de lecture-modification-écriture ; ne pas appliquer les instructions illégales NMOS aux variantes CMOS.
    - [x] Créer `Components/Mos6502/Services/Cpu6502.Unofficial.cs` : instructions NMOS combinées, NOP, instructions immédiates et blocage matériel JAM, sans exception hôte pour un opcode physique.
    - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu6502BusTests.cs` : traces autonomes des accès, cycles de pile/vecteur, adressages et opcodes NMOS ; confronter les cas utiles aux sources de référence avant le contrôle groupé.

    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/Cpu6502*.cs` : compléter les accès transitoires, instructions NMOS, IRQ/NMI/RESET et particularités CMOS selon les variantes réellement utilisées ; compléter ce fichier de suivi avec les références et les contrôles utiles avant les changements.

    - [ ] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/Cpu6809*.cs`, `Cpu80186*.cs` et `CpuArm26*.cs` selon les écarts établis : jeu d'instructions, exceptions, accès mémoire, cycles, banque de registres, cache et coprocesseurs propres aux variantes installées ; consigner les limites restantes ici.

    - [ ] Modifier les services des cartes sous `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines` pour corriger les décodages d'adresses, arbitrages, horloges, attentes, interruptions et liaisons réellement câblées, suivant leurs manuels. Ne pas déduire la fidélité d'une carte de tests isolés de ses puces.

  - [ ] Affichage, mémoire et audio.

    - [ ] Modifier `Services/Crtc6845.cs`, `TeletextSaa5050.cs`, `Sn76489.cs` et les services vidéo/audio des cartes sous `Machines` selon les écarts confirmés. Distinguer le MC6845 original (VSYNC fixe) des autres variantes ; vérifier les ULA Atom/Electron/BBC et leurs accès partagés, couleurs, cadences et son.

    - [ ] Modifier les services MEMC/IOC/VIDC, mémoire shadow/sideways, ULA Tube, RTC/EEPROM, clavier et contrôleurs des cartes sous `Machines` selon leurs registres, masques, interruptions, resets et câblages documentés ; inscrire ici chaque composant effectivement examiné et les corrections nécessaires.

  - [ ] Contrôleurs et interfaces.

    - [ ] Modifier `Services/Intel8271*.cs`, `Wd1770*.cs`, `Upd765*.cs` et `Upd765DriveControl.cs` selon les écarts confirmés : variantes, commandes, recherche d'identifiants, flux, index, DMA/PIO, reset, erreurs, protections et cadences. Reprendre les limites NEC encore ouvertes dans `docs/tasks/machines/AcornArchimedes/implementation.md`, sans considérer les seuls secteurs réguliers comme preuve de fidélité complète.

    - [ ] Modifier `Services/Hd63463*.cs`, `AtaPio*.cs`, `AcornScsi*.cs` et leurs adaptateurs de cartes pour corriger les écarts de commandes, statuts, buffers, DMA, géométries et durées suivant les fiches constructeur ; conserver les médias sous la responsabilité de leur propriétaire.

    - [ ] Modifier `Services/Acia6850.cs`, `Mos6551*.cs`, `Ns16450*.cs`, `Mc6854*.cs`, `Upc82C711Configuration.cs` et les autres composants d'E/S effectivement présents sous `Machines` pour corriger les écarts documentés de broches, registres, débits, interruptions et resets.

  - [ ] Validation et livraison par groupe terminé.

    - [ ] Modifier ce suivi avec les références précises, les écarts corrigés et les limites de chaque composant ; ajouter avant exécution les chemins de tout nouveau test nécessaire et la suppression de tout diagnostic externe temporaire.

    - [ ] Modifier les sorties `build/Debug` avec `scripts/local-building.cmd --building=debug --modules=A` après validation du groupe à livrer, puis modifier ce suivi avec le résultat. Ne pas déclarer l'audit global terminé sur la base de la compilation ou d'un sous-ensemble de tests.



Les chemins `Services/...` ci-dessus sont relatifs à `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/`. Les groupes de machines Archimedes, Risc PC et A7000 restent dans l'objectif initial ; leurs composants ajoutés rejoindront cet audit.



Références de départ : fiche MOS 6522 (https://www.princeton.edu/~mae412/HANDOUTS/Datasheets/6522.pdf), sources jsbeeb, b-em, b2, Atomulator et MAME. Les sources d'émulateurs permettent de comparer des détails d'implémentation ; les variantes et valeurs du matériel restent à établir avec les documents correspondants.



Groupe VIA ports/T2 : code déplacé dans Components/Mos6522 ; interruptions indépendantes, fronts CA2/CB2, modes manuel/handshake/impulsion, latch CA1/CB1 et comptage PB6 couverts par les tests autonomes. Les modes de décalage série, PB7 de T1 et les détails complets de retard des minuteries restent à vérifier/corriger dans cet audit ; le groupe ne constitue pas une affirmation de VIA entièrement fidèle.



À poursuivre pour le VIA complet : modifier Components/Mos6522/Services/Via6522*.cs et ses constantes pour le registre à décalage, PB7/T1, les délais des compteurs et les différences de lecture/latch entre ports A et B, après confrontation détaillée aux fiches constructeur. Les autres chemins Services/<puce> de ce document se trouvent désormais sous Components/<puce>/Services après le rangement demandé.


- [ ] Validation indépendante du groupe NMOS avant livraison.
  - [x] Créer `artifacts/acorn-6502-reference/*.json` : jeux SingleStepTests pour les adressages, pile, branchements, ADC/SBC et instructions combinées ; fichiers temporaires.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu6502ReferenceDiagnostic.cs` : confronter les états et chaque accès du bus aux vecteurs indépendants.
  - [x] Modifier les fichiers `Components/Mos6502/Services/Cpu6502*.cs` pour corriger les écarts reproduits, puis ajouter les régressions autonomes utiles dans `AcornEmu6502BusTests.cs`.
  - [x] Supprimer `AcornEmu6502ReferenceDiagnostic.cs` et `artifacts/acorn-6502-reference` après utilisation, puis modifier ce suivi avec le résultat et les limites.

Résultat du groupe NMOS, 2 novembre 2026 : accès transitoires des indexations, branchements, pile, RESET et entrées IRQ/NMI ; délai CLI/SEI/PLP ; décodage des 256 opcodes, dont JAM et opérations combinées. Correction ADC décimal pour des chiffres BCD invalides. Tests autonomes : 17 nouveaux cas. Le diagnostic temporaire SingleStepTests a comparé 13 500 vecteurs (500 pour chacun de 27 opcodes) : états, mémoire et séquences du bus conformes après correction ADC. Source et corpus temporaires supprimés après utilisation.

Références : https://www.pagetable.com/94 (analyse transistorielle de RESET/BRK/IRQ/NMI), https://www.modelb.bbcmicro.com/tech-6502interrupts.html (CLI/SEI), https://github.com/SingleStepTests/65x02/tree/main/6502 (vecteurs NMOS), https://github.com/mattgodbolt/jsbeeb/blob/main/src/6502.js (ADC et interruptions).

Limites non levées par ce groupe : synchronisation d'interruption aux frontières précises des branchements, variantes CMOS (65C12/65C02) et dépendance analogique des instructions instables XAA/LAX/AHX/SHX/SHY/TAS. Les instructions immédiates instables utilisent un masque déterministe 0xEE, ce qui ne prétend pas reproduire toutes les révisions physiques. La comparaison porte sur les 27 opcodes sélectionnés, pas sur tous les états des 256 opcodes. Le groupe CPU complet reste ouvert, comme le VIA complet et les autres composants.

Validation après suppression du diagnostic : 989 tests AcornEmu réussis. `scripts/local-building.cmd --building=debug --modules=A` terminé avec code 0 ; exécutable `build/Debug/Retro Media Lab/retromedialab.exe` et 24 modules présents. Il s'agit de la livraison des corrections actuelles, pas de la clôture de l'audit du 6502.

- [ ] Terminer la puce en cours avant toute correction d'une autre puce, puis faire un commit indépendant (consigne utilisateur du 2 novembre).
  - [x] Modifier `Components/Mos6502/Services/Cpu6502.cs` et `Cpu6502.Bus.cs` : compléter les sondages IRQ des branches NMOS d'après les séquences documentées ; ajouter les régressions autonomes dans `AcornEmu6502BusTests.cs`.
  - [x] Modifier `Components/Mos6502/Services/Cpu6502.Cmos.cs` et les aides du décodeur : vérifier et corriger les séquences 65C12/65C102 réellement utilisées, sans appliquer les instructions Rockwell ou WDC non présentes ; ajouter les seuls tests autonomes utiles au dossier des tests Acorn.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu65C12Tests.cs` : sauts indirects, cycles RMW indexés, décimal CMOS et séparation des NOP du NMOS ; compléter les cas IRQ de `AcornEmu6502BusTests.cs`.
  - [x] Créer `artifacts/acorn-65c12-reference/*.json` et `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu65C12ReferenceDiagnostic.cs` : confronter les instructions communes CMOS aux vecteurs Synertek de SingleStepTests, en distinguant cette référence des particularités Hitachi 65C12.
  - [x] Modifier `Components/Mos6502/Services/Cpu6502*.cs` et `AcornEmu65C12Tests.cs` avec les corrections des accès de bus confirmés par ces vecteurs et les sources des émulateurs BBC.
  - [x] Supprimer le diagnostic `AcornEmu65C12ReferenceDiagnostic.cs` et le dossier temporaire `artifacts/acorn-65c12-reference` après utilisation, puis modifier le suivi avec les résultats.
  - [x] Modifier `Components/Mos6502/Services/Cpu6502.cs`, `Cpu6502.Interrupts.cs` et `Cpu6502.Bus.cs` : séparer le front NMI mémorisé de son acceptation au sondage, appliquer le sondage IRQ au CMOS et conserver le détournement du vecteur matériel ; compléter les régressions dans `AcornEmu6502BusTests.cs` et `AcornEmu65C12Tests.cs`.
  - [x] Modifier l'index Git et créer le commit dédié au 6502 après validation de ses corrections, avant de reprendre le VIA ou un autre composant.

Complément du 2 novembre : sondages IRQ NMOS des branches pris/croisés, JMP indirect CMOS six cycles, décalages indexés CMOS 6/7 cycles et SBC décimal invalide corrigés. Contrôle groupé : 1004 tests AcornEmu réussis. Pas de nouveau build de livraison ni de commit tant que la puce reste en cours.

Comparaison CMOS : 9 000 vecteurs SingleStepTests Synertek, 18 opcodes. Résultats des registres, mémoire et durées conformes. Traces de bus comparées pour 14 opcodes ; ADC/SBC décimaux et STA/DEC absolus indexés sont exclus de la comparaison des adresses uniquement, car les cycles décimaux Hitachi ne sont pas établis ici et les lectures sans retenue du 65C12 ne se déduisent pas du Synertek. Ne pas déclarer 9 000 traces Hitachi vérifiées. Corrections supplémentaires : reprise de l'opérande d'instruction lors de la retenue, STA indirect Y, lectures transitoires JMP indirect/indirect X et branchement CMOS avec changement de page. Diagnostic et corpus supprimés.

Références CMOS : manuel Acorn Master, partie 2, chapitre P (https://retrosix.wiki/uploads/Master_Reference_Manual_Part_2-1.pdf) ; générateur 65C12 de jsbeeb (https://github.com/mattgodbolt/jsbeeb/blob/main/src/6502.opcodes.js) et ses références aux observations matérielles ; vecteurs https://github.com/SingleStepTests/65x02/tree/main/synertek65c02.

Restent sur la même puce : sondage IRQ CMOS, latence NMI aux limites d'instruction et adresses de correction décimale CMOS à établir sur la variante Hitachi. Aucun autre composant n'a été corrigé pendant ce groupe. Ne pas committer le 6502 comme terminé avant résolution de ces points.

Après nettoyage du diagnostic CMOS : 1 006 tests AcornEmu réussis en 14 s ; `git diff --check` sans erreur. Ces dernières corrections ne sont pas encore dans le build de livraison, qui précède ce complément.

Clôture des corrections identifiées du groupe 6502/65C12/65C102 : 1 014 tests AcornEmu réussis. Acceptation NMI séparée du front mémorisé, sondage IRQ CMOS, priorité NMI et détournement IRQ, distinction BRK CMOS/NMOS, conservation de D dans l'état empilé et effacement CMOS à l'entrée, décodage des 256 opcodes dans les deux modes couverts. Les points IRQ/NMI annoncés ouverts plus haut sont maintenant corrigés.

Le cycle supplémentaire BCD du 65C12 reste un cycle interne, sans appel de lecture d'E/S : même traitement dans les macros `INTERP_MODE_65c12_BCD_*` et dans l'arithmétique immédiate de https://github.com/scarybeasts/beebjit/blob/master/interp.c. Il ne reste pas un accès d'E/S identifié à reproduire dans ce groupe. Cela ne constitue pas une mesure des signaux analogiques ni une validation de toutes les révisions physiques ; les opcodes NMOS instables gardent leur modèle déterministe explicite et la portée des corpus demeure celle indiquée plus haut.

- [ ] Livrer et committer ce groupe CPU avant tout autre composant.
  - [x] Modifier `build/Debug` avec `scripts/local-building.cmd --building=debug --modules=A`, puis modifier ce suivi avec le résultat et les 24 modules vérifiés.
  - [x] Modifier l'index Git pour les seuls fichiers 6502, ses tests et ce suivi ; créer le commit CPU indépendant et modifier le suivi avec son identifiant avant de passer au composant suivant.

Livraison CPU du 2 novembre : script Debug All terminé avec code 0, exécutable présent et 24 DLL de modules vérifiées. Les corrections CPU sont prêtes pour leur commit séparé.

Commit CPU créé : `c2fa3f8ac` — `fix(acorn): correct 6502 and 65C12 bus cycles and interrupt polling`. Les modifications de puces sont exclusivement celles du groupe 6502. Le prochain composant peut maintenant être repris.

- [x] Terminer le VIA 6522 avant son commit séparé.
  - [x] Modifier `Components/Mos6522/Constants/Via6522Constants.cs` et créer `Components/Mos6522/Enums/ViaShiftMode.cs` : modes ACR, bits PB7/SR et délais nommés, une constante par ligne.
  - [x] Créer `Components/Mos6522/Services/Via6522.Timers.cs` et modifier `Via6522.cs` : minuteries continues après l'interruption unique, retard de chargement, rechargement T1 et sortie PB7, T2 bas utilisé pour l'horloge série et conservation du compteur haut.
  - [x] Créer `Components/Mos6522/Services/Via6522.Shift.cs` et modifier `Via6522.cs` et `Via6522.Ports.cs` : huit modes, broches CB1/CB2, décalage entrant/sortant, interruption SR et acquittement ; lecture des broches du port A distincte du latch de sortie B.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuVia6522Tests.cs` et les attentes VIA préexistantes concernées : régressions autonomes des registres, temporisations, PB7 et huit modes série ; ne pas utiliser de firmware externe.
  - [ ] Modifier ce suivi avec les résultats groupés, références et éventuelles limites après correction ; livrer par Debug All et créer le commit VIA indépendant avant toute autre puce.

  - [x] Modifier `Components/Mos6522/Services/Via6522.Timers.cs`, `Via6522.cs` et les tests VIA : préserver l'interruption déclenchée sur le cycle même d'un acquittement et conserver les compteurs/SR à RESET ; vérifier ces deux séquences avant livraison.

  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuModelBTests.cs` : avancer une horloge VIA avant l'acquittement effectué directement sur le bus de carte, pour distinguer ce cas de la collision d'interruption désormais testée séparément.

VIA 6522 : 1 031 tests AcornEmu réussis. Modes série ACR 0 à 7 raccordés : réception CB2, émission MSB d'abord, CB1 externe ou généré, horloge système ou T2 bas, recirculation continue sans IRQ SR, IRQ de fin et relance/acquittement par accès SR. Le compteur haut de T2 reste décrémenté quand son compteur bas fournit l'horloge série. T1 continue après son interruption unique, recharge en continu et pilote PB7 ; durée initiale N+2 et collision d'acquittement vérifiées. Lecture des broches A distincte du latch de sortie B ; RESET conserve compteurs/SR et efface contrôle/interruptions. Aucun diagnostic ou artefact de test externe ajouté.

Références VIA : fiche MOS 6522 (https://www.princeton.edu/~mae412/HANDOUTS/Datasheets/6522.pdf), séquences MAME du registre série (https://github.com/mamedev/mame/blob/master/src/devices/machine/6522via.cpp), comparaisons BBC jsbeeb (https://github.com/mattgodbolt/jsbeeb/blob/main/src/via.js) et beebjit (https://github.com/scarybeasts/beebjit/blob/master/via.c). Tick reste une horloge VIA ; les cartes gardent leur rapport CPU/VIA et leurs périphériques.

- [x] Livrer et committer le VIA avant toute autre puce.
  - [x] Modifier `build/Debug` avec `scripts/local-building.cmd --building=debug --modules=A`, puis modifier le suivi avec la vérification de l'exécutable et des 24 modules.
  - [x] Modifier l'index Git pour `Components/Mos6522`, les tests VIA concernés et ce suivi ; créer le commit VIA indépendant et consigner son identifiant.

Livraison VIA : Debug All terminé avec code 0, exécutable et 24 modules présents. Les corrections et leurs tests sont prêts pour le commit VIA indépendant.

Commit VIA créé : `98af9043b` — `fix(acorn): complete VIA timers, PB7 output and serial shift modes`. Livraison Debug All et contrôle groupé de 1 031 tests réussis avant le commit ; aucune autre puce corrigée entre la fin du VIA et ce commit.

- [x] Auditer et corriger le MC6809 avant son commit indépendant.
  - [x] Modifier `Components/Mc6809/Services/Cpu6809.cs`, `Cpu6809.Interrupts.cs` et `Cpu6809.Addressing.cs` : mémoriser la NMI avant LDS, autoriser sa prise après toute instruction chargeant S, et distinguer l'attente SYNC de CWAI selon les masques.
  - [x] Modifier `Components/Mc6809/Services/Cpu6809.Addressing.cs`, `Cpu6809.Instructions.cs` et les constantes : placer les cycles internes et lectures transitoires dans les séquences d'adressage et de lecture-modification-écriture ; vérifier les registres/indicateurs et codes physiques documentés par les sources.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu6809Tests.cs` : régressions autonomes NMI/pile/attentes, indicateurs, séquences mémoire et modes indexés, en conservant les tests System existants.
  - [x] Modifier ce suivi avec les corrections, références et résultats groupés ; construire Debug All et committer le 6809 séparément avant une autre puce.

  - [x] Modifier `Components/Mc6809/Services/Cpu6809.Instructions.cs`, `Cpu6809.Interrupts.cs`, `Cpu6809.Addressing.cs` et `Constants/Cpu6809Constants.cs` : séquencer PSH/PUL avant les accès pile, ajouter les lectures finales RTS/RTI et les prélectures CWAI/ABX/MUL ; nommer les délais ajoutés.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu6809Tests.cs` : vérifier les cycles des accès pile PSHS/PULS/RTS/RTI et CWAI avec un bus autonome.
  - [x] Modifier `Components/Mc6809/Services/Cpu6809.Instructions.cs`, `Cpu6809.Addressing.cs`, `Cpu6809.cs` et `Constants/Cpu6809Constants.cs` : reproduire les alias unaires et NOP, le mode de test HCF jusqu'à RESET et les formes indexées non documentées, sans exception hôte pour ces codes physiques ; ajouter leurs régressions dans `AcornEmu6809Tests.cs`.
  - [x] Modifier `Components/Mc6809/Services/Cpu6809.Instructions.cs` et `AcornEmu6809Tests.cs` : reproduire CC/DP sur les deux octets lors de TFR et EXG avec source 8 bits, conserver la différence EXG avec source 16 bits et calculer V après DAA selon les mesures matérielles publiées.

Références MC6809 de ce complément : manuel Motorola, annexe instructions https://www.maddes.net/m6809pm/appendix_a.htm ; séquences mémoire https://github.com/mamedev/mame/blob/master/src/devices/cpu/m6809/base6x09.lst et m6809.lst ; lecture TFR/EXG dans m6809.cpp ; mesures originales des codes non documentés https://github.com/hoglet67/6809Decoder/wiki/Undocumented-6809-Behaviours. Les accès invalides VMA restent des cycles internes du contrat de bus actuel.

Points encore ouverts à ce stade intermédiaire : décodage des préfixes multiples et alias des pages 2/3, opcodes X18/XANDCC/XRES et stores immédiats (leurs indicateurs dépendent de l'instruction précédente), puis contrôle des séquences RESET. Leur clôture figure dans le complément ci-dessous.

Vérification groupée du complément : 1 066 tests AcornEmu réussis, aucun échec (18 s). Le premier passage a détecté un mauvais emplacement de la garde HCF dans RESET au lieu de Step ; cette erreur introduite dans le complément a été corrigée et les trois variantes HCF passent désormais. Aucun test ni fichier externe temporaire ajouté. Aucun build de livraison ni commit MC6809 effectué à ce stade.

- [x] Achever le décodage MC6809 dans le groupe en cours.
  - [x] Créer `Components/Mc6809/Services/Cpu6809.Unofficial.cs` et modifier `Cpu6809.cs`, `Cpu6809.Instructions.cs`, `Cpu6809.Interrupts.cs`, `Cpu6809Constants.cs` : préfixes répétés, alias de pages, X18/XANDCC/XRES, stores immédiats avec état des indicateurs issu de l'instruction précédente et RESET avec lectures du vecteur après les cycles internes.
  - [x] Modifier `AcornEmu6809Tests.cs` : régressions autonomes des préfixes et cycles, vecteurs, stores immédiats et séquence RESET ; modifier ce suivi avec les résultats du contrôle groupé.
  - [x] Modifier `build/Debug` par le script Debug All, puis ce suivi avec le résultat ; modifier l'index Git pour la puce MC6809, ses tests et ce suivi et créer son commit indépendant.

Clôture des points de décodage identifiés : première page de préfixe conservée, préfixes suivants consommés chacun sur un cycle, repli des alias vers l'instruction de base et compléments spécifiques XLBRA/XADDD/XADDU/XRES/XSWI2/XFIRQ. Un flux sans fin de préfixes rend la main à l'hôte après un tour d'adressage sans accepter d'interruption au milieu de l'instruction. Les stores immédiats lisent l'octet haut et écrivent uniquement l'octet bas pour les formes 16 bits ; NZV dépend du groupe d'instruction précédent pour les formes non préfixées. RESET conserve les indicateurs arithmétiques, force I/F et DP=0 ; le vecteur est lu aux cycles 5 et 6 d'une entrée de 7 cycles.

Résultat final groupé : 1 088 tests AcornEmu réussis en 18 s, dont 57 cas MC6809 autonomes. Le contrôle de décodage des 256 codes des trois pages prouve l'absence d'exception hôte pour ces codes ; les autres cas vérifient les états et séquences précisés dans leurs assertions, sans prétendre couvrir toutes les combinaisons d'opérandes ni toutes les révisions physiques. Aucun corpus, firmware ou processus externe temporaire utilisé dans ces tests. Références supplémentaires : mesures de bus et groupes d'indicateurs de https://github.com/hoglet67/6809Decoder/blob/master/src/em_6809.c. Debug All en cours avant le commit séparé.

Livraison MC6809 : `scripts/local-building.cmd --building=debug --modules=A` terminé avec code 0. Exécutable `build/Debug/Retro Media Lab/retromedialab.exe` présent et 24 DLL de modules vérifiées. `git diff --check` ne signale aucune erreur de contenu. Le commit suivant porte exclusivement sur la puce MC6809, ses régressions et ce suivi.

Commit MC6809 créé : `d1b0ee482` — `fix(acorn): correct MC6809 bus sequences and physical opcode decoding`. Aucun autre composant corrigé entre ce groupe et son commit.

- [ ] Auditer et corriger le 80186 avant son commit indépendant.
  - [x] Créer `Components/Intel80186/Constants/Cpu80186Constants.cs` et modifier les quatre fichiers `Components/Intel80186/Services/Cpu80186*.cs` : déplacer les constantes existantes dans leur dossier et nommer les masques, vecteurs et valeurs ajoutés sans changer les contrats de carte.
  - [x] Modifier `Cpu80186.cs`, `Cpu80186.Instructions.cs` et `Cpu80186.Arithmetic.cs` : corriger les interruptions/traps et l'inhibition des chargements de segments, les formes ModRM invalides, les lectures BOUND et les décalages de compte nul d'après les sources Intel et MAME ; corriger PUSH SP suivant la documentation constructeur plutôt que conserver le test préexistant erroné.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu80186Tests.cs` : régressions autonomes des exceptions, pile de retour, trap/inhibition, ModRM et accès des décalages, sans firmware ni DLL externe ; modifier ce suivi avec les résultats groupés et les points restant à auditer.
  - [ ] Modifier les services 80186 et ce suivi après l'audit restant : timings, préfixes/REP, I/O et périphériques intégrés réellement câblés sur la carte Master 512 ; ajouter les actions concrètes supplémentaires avant leur réalisation.
  - [ ] Modifier `build/Debug` par Debug All une fois le groupe terminé, puis ce suivi avec le résultat ; créer le commit 80186 indépendant avant toute autre puce.
  - [x] Modifier `AcornEmuMasterTests.cs` : remplacer l'attente 8086 de PUSH SP par la valeur SP d'avant l'instruction, conformément au manuel MASM Microsoft ; conserver le contrôle des bits réservés de PUSHF.
  - [x] Modifier `Cpu80186.cs` et les lectures BOUND/LES/LDS/CALL FAR/JMP FAR dans `Cpu80186.Instructions.cs` : effectuer les mots et pointeurs sur des adresses physiques contiguës avec rebouclage 20 bits, sans reboucler chaque octet sur le début du segment ; ajouter leurs régressions dans `AcornEmu80186Tests.cs`.

Premier complément 80186 : 1 104 tests AcornEmu réussis en 20 s, dont 16 nouvelles régressions 80186 autonomes ; aucun artefact externe temporaire. PUSH SP, dans ses deux encodages, empile SP avant décrémentation. Les exceptions de division/BOUND/opcode inutilisé retournent après l'instruction consommée (exception ESC à traiter avec son registre de relocalisation). Le trap déjà armé survit à POPF qui efface TF, mais une interruption logicielle n'empile pas un second trap ; chargements de segments et WAIT suppriment le trap immédiat. Les chargements de segments inhibent les interruptions jusqu'après l'instruction suivante, suivant la fiche 80186 originale. BOUND lit les deux bornes sans court-circuit ; un décalage de compte masqué nul n'écrit pas son opérande. Les formes de groupe non autorisées ne réalisent plus MOV/POP. Les lectures de mots et pointeurs franchissent physiquement la fin du segment.

Références : fiche Intel 210451-002 (https://www.bitsavers.org/components/intel/80186/210451-002_iAPX186_Datasheet_Dec82.pdf), note Intel AP-186 (https://www.bitsavers.org/components/intel/80186/210973-001_AP-186_Introduction_to_the_80186_Microprocessor_Mar83.pdf), manuel MASM Microsoft pour PUSH SP (https://www.pcjs.org/documents/books/mspl13/masm/mref/), source MAME i186.cpp/i86.cpp pour les séquences et table des durées. Les PDF Intel refusés à l'ouverture directe ont fourni leurs passages indexés via la recherche ; aucune inspection visuelle de leurs pages n'est prétendue.

Le 80186 n'est pas terminé : la table de durées montre encore des écarts (notamment PUSHA/POPA, BOUND, MUL/DIV, appels et chaînes). Préfixes interrompables/REP, WAIT/TEST, NMI et périphériques intégrés restent à vérifier. Sur Master512Board, le masque d'interruption et un canal DMA sont actuellement gérés par la carte avec une logique simplifiée ; ne pas annoncer l'intégralité des périphériques 80186 émulée. Aucun nouveau build de livraison ni commit 80186 à ce stade ; aucune autre puce commencée.

- [x] Corriger les durées 80186 avec les colonnes constructeur propres au bus 16 bits.
  - [x] Créer `Components/Intel80186/Constants/Cpu80186Timings.cs` et modifier `Cpu80186.Instructions.cs` : constantes de durées par forme registre/mémoire, opérations de pile, BOUND/ENTER, sauts/appels, I/O, multiplication/division et décalages ; utiliser la colonne 80186 et non la colonne 80188.
  - [x] Modifier `Cpu80186.cs`, `Cpu80186.Instructions.cs` et `Cpu80186.Strings.cs` : majorer les transferts de mots non alignés, distinguer durée d'exception et coût INT/INTO, appliquer une fois le coût de démarrage de REP et ensuite le coût de chaque itération ; conserver un point d'interruption entre itérations.
  - [x] Modifier `AcornEmu80186Tests.cs` : régressions de durées attendues issues du constructeur, formes registre/mémoire, mots non alignés, REP sans itération et REP interrompu ; modifier ce suivi avec les résultats groupés et les limites restantes.

Complément des durées : 1 129 tests AcornEmu réussis en 18 s, dont 41 cas 80186. `git diff --check` sans erreur. Les tests d'INT ont détecté une erreur introduite dans ce complément : `Cycles += Execute(...)` reprenait l'ancienne valeur de Cycles et écrasait le coût ajouté par Interrupt pendant Execute. Le résultat d'Execute est désormais calculé avant son addition ; INT3 et INT imm8 donnent respectivement 45 et 47 cycles dans leurs régressions.

Source constructeur privilégiée face à la table MAME : fiche Intel 80186/80188, pages 26–32. La colonne 80186 indique PUSHA=36 et POPA=51 ; les valeurs MAME 68/83 appartiennent au 80188. Transferts de mots à adresses impaires majorés de 4 cycles par transfert réel ; les appels, retours, ENTER et accès pile utilisent donc aussi cette pénalité. REP MOVS/INS/OUTS : 8+8n ; CMPS : 5+22n ; SCAS : 5+15n ; LODS : 6+11n ; STOS : 6+9n. Le démarrage est payé une seule fois, puis de nouveau après une reprise via IRET. Une interruption conserve l'adresse du préfixe et les index/CX déjà avancés.

Références détaillées : https://www.alldatasheet.net/html-pdf/66058/INTEL/80186/3478/27/80186.html ; https://www.alldatasheet.net/html-pdf/66058/INTEL/80186/3865/30/80186.html ; https://www.alldatasheet.es/html-pdf/66058/INTEL/80186/3994/31/80186.html ; tableaux communs 80186/80188 des pages 28–29 référencées sur les variantes A80188/80188 dans la même fiche Intel. Les durées variables MUL/IMUL/IDIV restent leurs minima constructeur explicites ; la file de prélecture, ses attentes et les états HOLD ne sont pas encore simulés. Ce complément ne clôt donc pas l'audit 80186 ni celui de ses périphériques intégrés. Aucun build livré ni commit 80186 à ce stade.

- [x] Raccorder les entrées et attentes du cœur 80186.
  - [x] Modifier `Components/Intel80186/Constants/Cpu80186Constants.cs`, `Services/Cpu80186.cs` et `Cpu80186.Instructions.cs` : entrée NMI sur front montant mémorisé, priorité avant INTR indépendamment d'IF, réveil HLT ; entrée TEST active basse représentée par l'état occupé du coprocesseur et reprise de WAIT à son adresse jusqu'à disponibilité, sans trap immédiat.
  - [x] Modifier `Cpu80186.cs` : limiter le travail hôte lors d'un flux de préfixes sur tout l'espace d'offset, conserver le décodage et l'adresse du premier préfixe entre pas sans accepter d'interruption au milieu de l'instruction ; RESET efface cet état.
  - [x] Modifier `AcornEmu80186Tests.cs` : NMI mémorisée/priorité/HLT, WAIT interrompu puis repris et attente TEST, suites de préfixes et remise à zéro ; modifier ce suivi avec les résultats groupés et les limites restantes.

Complément entrées 80186 : 1 138 tests AcornEmu réussis en 19 s, dont 50 cas 80186. La NMI conserve une impulsion entre deux pas, passe avant INTR, réveille HLT avec IF effacé et ne se redéclenche pas tant que le niveau reste haut. L'inhibition après chargement de segment est respectée. WAIT reprend à son adresse tant que TEST indique occupé ; une interruption puis IRET reprend cette attente, et TEST disponible laisse passer l'instruction suivante. Les suites de préfixes sont découpées uniquement pour rendre la main à l'hôte après 65 536 octets ; l'état de décodage est conservé et aucune interruption émulée n'est acceptée entre ces octets. RESET annule cet état et la NMI en attente. Les préfixes REP isolés du flux comptent désormais eux aussi des cycles et ne peuvent donc plus laisser la boucle de carte sans progression de l'horloge.

Références : manuel Intel 210911-001, section WAIT et différences 8086/80186 (https://www.bitsavers.org/components/intel/80186/210911-001_iAPX86_88_186_188_Programmers_Reference_1983.pdf) ; manuel matériel Intel 210912-001, section TEST*/LOCK* (https://www.bitsavers.org/components/intel/8086/210912-001_iAPX_86_88_186_188_Users_Manual_1985.pdf) ; priorité NMI et frontières d'instruction comparées à `src/devices/cpu/i86/i186.cpp` de MAME. Aucun artefact externe de test ajouté. Prélecture/bus et périphériques intégrés restent à terminer avant le build et le commit 80186 ; aucune autre puce entamée.

- [x] Remplacer la logique DMA simplifiée du Master 512 par les canaux intégrés au 80186.
  - [x] Créer `Components/Intel80186/Constants/Dma80186Constants.cs` et `Services/Cpu80186.Dma.cs` : deux canaux, registres source/destination 20 bits, compteur 16 bits, contrôle CHG/ST, mémoire ou I/O, incrément/décrément/fixe, octet/mot, synchronisation et priorités ; nommer chaque masque et décalage.
  - [x] Modifier `Cpu80186.cs`, `Cpu80186.Instructions.cs` et `Cpu80186.Strings.cs` : décoder les registres DMA dans les accès I/O existants, exécuter un transfert DMA par pas avec coût de bus et exposer les demandes DRQ au câblage de carte ; conserver les fonctions externes d'I/O pour les autres ports.
  - [x] Modifier `Machines/BbcMaster/Tube/Services/Master512Board.cs` : supprimer le tableau DMA et l'heuristique basée sur la valeur de l'adresse ; raccorder la demande Tube à DRQ et laisser le processeur décoder ses registres ; modifier le programme synthétique DMA dans `AcornEmuMasterTests.cs` pour programmer les vrais registres et bits CHG/SYN/MIO.
  - [x] Modifier `AcornEmu80186Tests.cs` : tests autonomes des deux canaux, contrôle CHG, compteur zéro/arrêt, espaces mémoire/I/O, transferts de mots, pointeurs et arbitrage ; modifier ce suivi avec les résultats et les raccordements encore manquants.

- [x] Compléter les accès et la restitution du bus DMA dans le même bloc 80186.
  - [x] Modifier `Cpu80186.Dma.cs`, `Cpu80186.Instructions.cs` et `Master512Board.cs` : accès octet aux registres internes suivant le bus 16 bits, OUT AL présentant AX sur le bus interne, restitution du bus en synchronisation destination et voie haute Tube non câblée ; compléter `AcornEmu80186Tests.cs` et `AcornEmuMasterTests.cs` avec leurs régressions, puis ce suivi avec le contrôle groupé.

Complément DMA 80186 : 1 157 tests AcornEmu réussis en 20 s. Deux canaux avec pointeurs physiques 20 bits, compteur 16 bits (zéro initial = 65 536 transferts), CHG/ST, sélection mémoire/I/O indépendante de la valeur de l'adresse, modes fixe/incrément/décrément, octet/mot, arrêt TC et arbitrage des priorités. Les transferts destination synchronisée restituent le bus entre transferts ; deux cycles de restitution sont comptés. Le séquencement reste au pas d'instruction, sans file BIU ni phases de bus détaillées. Les régressions vérifient également le rebouclage physique, les mots non alignés, les accès internes impairs avec inversion de voies et OUT AL présentant AX sur le bus interne. Le programme synthétique Master512 utilise maintenant les vrais registres DMA avec CHG/SYN/MIO, et le Tube n'occupe que la voie basse du bus. `git diff --check` sans erreur de contenu ; seuls les avertissements de conversion LF/CRLF des deux autres suivis préexistants sont présents.

Sources : Intel AP-186, sections DMA et source/destination synchronized (https://www.bitsavers.org/components/intel/80186/210973-001_AP-186_Introduction_to_the_80186_Microprocessor_Mar83.pdf) ; manuel Intel 80186/80188, bus interne et accès octet (https://0x04.net/~mwk/doc/intel/80186-80188_manual.pdf) ; comparaison des registres avec https://github.com/mamedev/mame/blob/master/src/devices/cpu/i86/i186.cpp et câblage DRQ0/voie basse Tube avec https://github.com/mamedev/mame/blob/master/src/devices/bus/bbc/tube/tube_80186.cpp.

Le bloc DMA n'est pas clôturé : la demande d'interruption de fin de compteur est mémorisée mais doit encore entrer dans le contrôleur intégré, la demande Timer2 n'est pas encore produite, et le décodage utilise encore l'adresse de remise à zéro du bloc périphérique. Ces points font partie de la suite du même 80186 avant son commit ; aucun autre composant commencé et aucun build de livraison annoncé.

- [x] Raccorder le bloc périphérique et les interruptions intégrées du 80186.
  - [x] Créer `Components/Intel80186/Constants/Peripheral80186Constants.cs` et `Services/Cpu80186.Peripherals.cs`, modifier `Cpu80186.cs`, `Cpu80186.Dma.cs`, `Cpu80186.Instructions.cs` et `Cpu80186.Strings.cs` : relocalisation du bloc dans l'espace I/O ou mémoire, accès du cœur et du DMA passant par le même décodage interne, exception ESC sélectionnable et lecture des opérandes ESC mémoire ; compléter les régressions dans `AcornEmu80186Tests.cs`.
  - [x] Créer `Components/Intel80186/Constants/Interrupt80186Constants.cs` et `Services/Cpu80186.Interrupts.cs`, modifier `Cpu80186.cs`, `Cpu80186.Peripherals.cs`, `Cpu80186.Dma.cs` et `Master512Board.cs` : contrôles, masques, priorités, requêtes/en-service, acquittement/EOI/poll et raccordement INT0 Tube, suppression du masque parallèle de la carte ; compléter les régressions autonomes et ce suivi.
  - [x] Créer `Components/Intel80186/Constants/Timer80186Constants.cs` et `Services/Cpu80186.Timers.cs`, modifier le décodage périphérique et `Master512Board.cs` : trois compteurs, modes et entrées/sorties documentés, requêtes Timer2 vers DMA et sorties de carte, avec régressions autonomes et résultats dans ce suivi.

Complément relocalisation/interruptions : 1 173 tests AcornEmu réussis en 20 s. Le bloc périphérique est relocalisable en I/O et en mémoire (adresse de RESET 20FFh), avec inversion des voies pour les accès de mots impairs. Les accès CPU et DMA utilisent ce décodage et l'ancienne adresse I/O redevient externe après relocalisation. ESC mémoire effectue une lecture de mot ignorée ; ET provoque l'exception 7 avec retour au premier préfixe, avant lecture du ModRM. Le contrôleur intégré gère masques, priorités, requêtes, en-service, acquittements, EOI spécifique/non spécifique et poll. Les demandes DMA arrivent désormais à ce contrôleur ; INT0 Tube remplace le masque parallèle de la carte. Modes front/niveau, cascade et mode esclave sont couverts par des régressions ciblées. Les entrées INT doivent rester actives jusqu'à leur acquittement suivant la fiche Intel ; une désactivation retire la requête.

Complément timers : 1 181 tests AcornEmu réussis en 20 s, `git diff --check` sans erreur de contenu. Trois compteurs, division par quatre, comptage externe sur fronts, porte d'entrée, redéclenchement, alternance A/B, mode continu/unique, bit INH/EN, indicateur MC et interruptions. RESET conserve ALT=1 pour les timers 0/1 et leurs sorties hautes, conformément à la fiche Intel originale (la valeur initiale de MAME diffère). Les impulsions de sortie sont programmées sur un cycle CPU après la fin du compteur. Timer2 préscale les autres timers et fournit un verrou de demande DMA unique partagé par les deux canaux, consommé par un seul transfert. Les sorties de carte sont raccordées au mécanisme d'arrêt et à NMI ; le détail du raccordement HOLD/rafraîchissement et les entrées physiques de timer restent à confronter au schéma de carte. Les entrées ne sont pas arbitrairement forcées hautes.

Références complémentaires : fiche Intel, pages 5 et 12 (https://www.alldatasheet.co.kr/html-pdf/66058/INTEL/80186/640/5/80186.html ; https://www.alldatasheet.jp/html-pdf/66058/INTEL/80186/1543/12/80186.html), manuel matériel Intel 270788-001, sections contrôle périphérique, timers, DMA et ESC (https://www.bitsavers.org/components/intel/80186/270788-001_80186_80188_80C186_80C188_Hardware_Reference_Manual_1990.pdf), AP-186 et comparaison des modes dans `i186.cpp` de MAME. Les tests n'utilisent ni firmware ni fichier externe. Aucun build Debug All de livraison ni commit 80186 à ce stade.

- [ ] Achever les raccordements de bus du même 80186 avant sa livraison indépendante.
  - [x] Modifier `Components/Intel80186/Services/Cpu80186.cs`, `Cpu80186.Timers.cs` et `Machines/BbcMaster/Tube/Services/Master512Board.cs` : représenter la demande de bus de rafraîchissement par HOLD, arrêter CPU et DMA pendant cette demande tout en laissant les timers avancer ; compléter les cas utiles dans `AcornEmu80186Tests.cs` et ce suivi avec les résultats.
  - [x] Modifier `Master512Board.cs` et ce suivi après lecture du schéma de carte : confirmer les entrées physiques des timers et le raccordement HOLD/NMI ; créer uniquement `%TEMP%/RetroMediaLab-80186-schematic.gif` pour la lecture puis supprimer cette copie après inspection. Les deux entrées sont maintenant raccordées au niveau haut constaté sur le schéma.
  - [x] Modifier `Master512Board.cs` et `Cpu80186.cs`, créer les fichiers de constantes/services nécessaires dans le même dossier Tube après identification des signaux du circuit : représenter les deux bascules 74LS113 de rafraîchissement et le retour HLDA plutôt qu'une simple inversion du niveau Timer0 ; compléter leurs régressions autonomes dans `AcornEmuMasterTests.cs` et ce suivi. Détailler les nouveaux chemins avant leur création.
  - [ ] Modifier les constantes et services du même composant après confrontation des chip-select/READY, prélecture et durées variables MUL/DIV avec la documentation Intel ; détailler les fichiers et actions avant les modifications correspondantes, puis compléter les régressions nécessaires et ce suivi.

Complément HOLD et sorties de timer : 1 184 tests AcornEmu réussis en 22 s, aucun échec. HOLD empêche aussi bien les accès DMA que CPU et laisse avancer les compteurs internes ; un transfert reprend après restitution du bus. Une impulsion de sortie Timer1 conserve sa NMI même si les deux fronts se produisent pendant un pas de CPU. Effacer les deux bits TDRQ supprime la demande Timer2 non consommée. Le raccordement de la sortie Timer0 utilise maintenant le nom HOLD et non HALT. Les phases détaillées HOLD/HLDA et BIU restent au groupe de bus restant ; ces régressions vérifient des frontières de pas, pas des signaux électriques synchronisés cycle par cycle.

Le lien historique MDFS du schéma renvoie 404, y compris après correction du répertoire. L'index primaire de remplacement a été lu : https://acorn.huininga.nl/pub/docs/circuit%20diagrams/Acorn/80186%20Second%20Processor/ ; il contient `80186 Co-Processor.tif`. La conversion n'a pas été exécutée : `PIL` manque dans Python. Installation de Pillow demandée à l'utilisateur ; aucune copie temporaire n'a été créée ni laissée sur disque. Les autres points du 80186 restent réalisables indépendamment de cette lecture de schéma.

Pillow 13.0.0 installé à la demande explicite de l'utilisateur. Le schéma Acorn 0143,200/C a été téléchargé depuis le fichier TIFF exact de l'index puis inspecté, avec agrandissement de la zone des timers. TMRIN0 (broche 20) et TMRIN1 (broche 21) sont reliés au +5 V ; `Master512Board.Reset` les positionne maintenant au niveau haut. Timer1 passe par LK3 puis un inverseur vers NMI dans la position S du schéma. Timer0 attaque une bascule 74LS113 dont Q barré commande HOLD ; le circuit contient une seconde bascule et le retour HLDA, donc la simple inversion actuelle reste à remplacer par cette logique de rafraîchissement. Le fichier GIF temporaire a été supprimé après inspection. Régression de carte ajoutée : le programme synthétique configure le timer 1 et son interruption NMI atteint le gestionnaire en ROM sans firmware externe. Résultat groupé : 1 185 tests AcornEmu réussis en 20 s. Aucun autre processeur commencé ; aucun commit 80186 prématuré.

- [x] Réaliser le circuit de rafraîchissement de la carte Master512 dans le groupe de bus en cours.
  - [x] Créer `Machines/BbcMaster/Tube/Services/Master512Refresh.cs` et `Components/Intel80186/Services/Cpu80186.Bus.cs` d'après les signaux identifiés du schéma ; modifier `Master512Board.cs`, `Cpu80186.cs` et `Cpu80186.Strings.cs` pour transmettre HOLD/HLDA et les transitions d'horloge, sans changer la mémoire installée. Pour relire le schéma, créer `%TEMP%/RetroMediaLab-80186-schematic.gif`, puis supprimer ce fichier après inspection.
  - [x] Modifier `AcornEmuMasterTests.cs` et `AcornEmu80186Tests.cs` : demande mémorisée, acquittement, fin du rafraîchissement, répétition et remise à zéro ; modifier ce suivi avec le résultat du contrôle groupé et les limites restantes.

Détail avant réalisation du circuit : `Master512Refresh.cs` contiendra les deux états Q de IC7 (74LS113), le front descendant TMR0, le preset asynchrone de la demande par HLDA, le preset de la seconde bascule par UCS et son échantillonnage sur CLKOUT. `Cpu80186.Bus.cs` portera HOLD, HLDA, leurs événements et le comptage de cycles transmis à la carte ; `Cpu80186.cs` utilisera ces signaux avant tout accès CPU/DMA et préservera LOCK sur les chaînes REP, avec effacement lors d'une interruption. `Master512Board.cs` raccordera les broches sans modifier les octets RAM/ROM. Aucun fichier de constantes vide ou rempli de simples alias de booléens n'est nécessaire ; les constantes numériques existantes restent utilisées.

Rafraîchissement Master512 : 1 191 tests AcornEmu réussis en 20 s ; `git diff --check` sans erreur de contenu. Le détail agrandi du schéma confirme IC7B J=0/K=1, horloge sur le front descendant Timer0, Q barré vers HOLD et preset actif bas relié à /HLDA. IC7A reçoit J=/HLDA, K=HLDA, horloge CLKOUT et preset actif bas /UCS. `Master512Refresh` mémorise les deux Q ; le retour HLDA acquitte la demande même si Timer0 est déjà remonté. Le processeur signale HLDA avant le pas sans accès au bus, puis le retire avant reprise. Les accès ROM activent le preset UCS dans un finally qui restitue cette ligne. LOCK reste conservé entre itérations REP et diffère HOLD/DMA ; une interruption libère ce verrou, et RESET retire HLDA. Les nouveaux cas contrôlent la mémorisation/acquittement, le preset UCS, la remise à zéro et la chaîne verrouillée. Le cas de carte existant programme maintenant Timer0 puis Timer1 : le rafraîchissement restitue le bus et la NMI atteint le gestionnaire.

La simulation des broches reste liée aux frontières des pas du cœur : `AdvanceClocks` échantillonne le second Q avec les entrées stables pendant le pas ; elle ne prouve pas les phases T1/T2/T3/T4 ni les demi-périodes de CLKOUT. L'activation UCS de carte utilise encore la fenêtre ROM existante ; le prochain groupe doit la raccorder aux registres chip-select plutôt que considérer cette fenêtre comme un signal programmé. La copie temporaire GIF a été supprimée après lecture ; aucun firmware ou artefact de test externe ajouté. Le 80186 reste le seul composant en cours, sans build de livraison ni commit prématuré.

- [x] Raccorder les sélections et attentes programmables du 80186 dans le même groupe.
  - [x] Créer `Components/Intel80186/Enums/Cpu80186ChipSelect.cs`, `Constants/ChipSelect80186Constants.cs` et `Services/Cpu80186.ChipSelect.cs` : registres UMCS/LMCS/PACS/MMCS/MPCS, états de RESET, masques de bits, activation après programmation, limites UCS/LCS/MCS/PCS, fonctions alternatives A1/A2 et nombre d'attentes par cycle ; signaux typés et restitution dans finally.
  - [x] Modifier `Cpu80186.cs` et `Cpu80186.Peripherals.cs` : décodage des cinq registres et passage des accès mémoire/I/O externes, octet/mot et DMA, par les cycles sélectionnés ; compter séparément les deux cycles des mots impairs et préserver la priorité des registres internes.
  - [x] Modifier `Machines/BbcMaster/Tube/Services/Master512Board.cs` : activer UCS depuis le signal du CPU et sélectionner le Tube par PCS1, en mémoire comme en I/O, avec ses miroirs physiques ; conserver l'API de lecture des octets installés et ne pas ajouter de RAM. Modifier `AcornEmuMasterTests.cs` avec un amorçage synthétique qui programme réellement UCS/PACS/MPCS avant d'accéder à ces périphériques.
  - [x] Modifier `AcornEmu80186Tests.cs` et `AcornEmuMasterTests.cs` : limites des sélections, activation/RESET, fonctions alternatives des pins, attentes par cycle et accès réels de carte ; modifier ce suivi avec les résultats groupés et les points restant au BIU.

Complément chip-select/attentes : 1 205 tests AcornEmu réussis en 22 s. Le premier passage avait quatre échecs d'attentes : trois nouveaux tests utilisaient à tort 12 cycles au lieu des 8 cycles constructeur pour MOV mémoire vers accumulateur ; un cas DMA préexistant ne comptait pas les trois attentes UCS du premier cycle à FFFFF. Les attentes des tests ont été corrigées suivant la fiche Intel, sans modifier l'instruction pour satisfaire ces tests. `git diff --check` sans erreur de contenu.

UMCS vaut FFFBh à RESET et sélectionne seulement FFC00h–FFFFFh ; LMCS, MMCS et PCS deviennent actifs après leurs écritures requises. Les bits fixes et limites sont définis par registre ; les quatre MCS partagent la taille one-hot programmée et les PCS occupent des blocs de 128 octets. EX laisse les broches PCS5/6 aux adresses A1/A2 ou active les sept sélections. Les attentes sont comptées par cycle externe, deux fois pour les mots impairs ; les cycles sur registres intégrés activent les signaux de sélection mais ignorent les attentes externes. La restitution des sélections est assurée en finally, y compris si un délégué externe échoue.

Le Master512 utilise maintenant le signal UCS du cœur pour sa ROM et le preset de rafraîchissement. Le Tube dépend de PCS1 et répond à tous les miroirs de ses trois bits d'adresse dans le bloc sélectionné, en mémoire comme en I/O. Les lectures de la mémoire installée utilisées par les tests restent distinctes des lectures de bus CPU : cette API ne crée pas de ROM sélectionnée par défaut. Les programmes synthétiques commencent par étendre UCS depuis le vecteur RESET, puis programment PACS/MPCS avant de lire le Tube. Aucun firmware externe utilisé par les tests.

Références : manuel Intel 210912-001, figures 2-68 à 2-70 et tables UMCS/LMCS (https://www.bitsavers.org/components/intel/8086/210912-001_iAPX_86_88_186_188_Users_Manual_1985.pdf), AP-186 section chip-select et fiche Intel p.11/p.27, également disponible sous https://www.pcjs.org/documents/datasheets/intel/INTEL_80186_CPU.pdf ; comparaison des masques fixes avec `i186.cpp` de MAME. Les programmations MCS non one-hot ou les bases non alignées sont indéfinies matériellement ; la simulation ne les présente pas comme valides.

Le BIU reste à terminer : les lectures de code utilisent encore le chemin octet du cœur, sans file de prélecture six octets ni chevauchement des accès et de l'exécution. Les durées variables de multiplication/division sont encore les minima documentés. L'assertion des sélections ne signifie donc pas que les phases T1/T2/T3/T4 et les attentes de prélecture sont entièrement reproduites. Les entrées READY externes ne sont pas encore modélisées comme des cycles suspendus. Aucun autre composant commencé, aucun build de livraison ni commit du 80186 annoncé.

- [ ] Achever la prélecture et les durées du même 80186.
  - [x] Créer `Components/Intel80186/Constants/Prefetch80186Constants.cs` et `Services/Cpu80186.Prefetch.cs`, modifier `Cpu80186.cs`, `Cpu80186.Bus.cs`, `Cpu80186.Peripherals.cs` et `Cpu80186.Instructions.cs` : file de six octets et transferts de mots du bus 16 bits, remise à zéro/rechargement lors des transferts de contrôle et interruptions, effets des écritures de code sur les octets déjà préchargés, attentes de code et chevauchement des opérations suivant les séquences Intel ; ajouter les cas autonomes pertinents dans `AcornEmu80186Tests.cs` et ce suivi.
  - [x] Modifier `Components/Intel80186/Constants/Cpu80186Constants.cs` et `Services/Cpu80186.Strings.cs` : ajuster séparément AL et AH pour AAA/AAS sur le 80186, au lieu de propager une retenue sur AX comme à partir du 80286 ; compléter les limites de registres et indicateurs définis dans `AcornEmu80186Tests.cs`, puis modifier ce suivi avec les résultats.
  - [x] Modifier `Master512Board.cs` et les constantes/services de bus 80186 si nécessaire après identification de ARDY/SRDY sur le schéma Acorn : expliciter les niveaux ou attentes réellement câblés et compléter ce suivi. Créer uniquement `%TEMP%/RetroMediaLab-80186-ready.png` pour inspecter les broches READY du TIFF primaire, puis supprimer cette copie temporaire après lecture.
  - [ ] Modifier `Components/Intel80186/Constants/Cpu80186Constants.cs`, `Services/Cpu80186.cs` et `Cpu80186.Instructions.cs` : restaurer la valeur SP décrémentée de PUSH SP, distincte du SP original de PUSHA ; corriger `AcornEmu80186Tests.cs` et restaurer l'attente matérielle originale de `AcornEmuMasterTests.cs`. Le diagnostic réel révèle que l'ancienne modification suivait une indication MASM erronée et empêche le client Acorn de programmer les chip-select.
  - [ ] Modifier temporairement `Components/Intel80186/Services/Cpu80186.cs` : journal borné des six premières exceptions opcode invalide pour identifier le défaut réel du chargement COMMAND.COM ; supprimer ce journal et son compteur avant le contrôle final, sans conserver d’API de diagnostic dans le composant.
  - [x] Modifier temporairement `Cpu80186.cs` et `Cpu80186.Prefetch.cs` : comparer le démarrage DOS Plus sans prélecture avec le même 80186 corrigé, puis rétablir immédiatement les fonctions de prélecture ; inscrire le résultat dans ce suivi pour isoler la régression sans conserver de commutateur de diagnostic dans le cœur.
  - [ ] Créer temporairement `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu80186BootDiagnostic.cs` : démarrer le Master 512 avec MOS 3.20 et client 80186 1.01 existants, média DOS Plus lu sans écriture, budget borné de trames, détachement des images et remise à zéro en finally ; créer `artifacts/acorn-master/master512-80186-audit.png` seulement pour le rendu réel obtenu, et modifier ce suivi avec le résultat. Télécharger uniquement l'image de démarrage de l'archive publique Disc1.zip vers `%TEMP%/RetroMediaLab-80186-boot.img`, après inspection de son nom et de sa longueur, sans extraction de chemins d'archive.
  - [ ] Supprimer `AcornEmu80186BootDiagnostic.cs`, `%TEMP%/RetroMediaLab-80186-boot.img` et l'aperçu `%TEMP%/RetroMediaLab-80186-ready.png` après utilisation ; reconstruire l'assembly de tests pour retirer le diagnostic et modifier ce suivi avec les contrôles effectivement réussis.
  - [ ] Modifier `Cpu80186.Arithmetic.cs`, `Cpu80186.Instructions.cs` et `Cpu80186Timings.cs` après vérification des durées dépendant des opérandes de MUL/IMUL/DIV/IDIV et de leur séquencement ; compléter `AcornEmu80186Tests.cs` et ce suivi avec les résultats et les limites effectivement constatées.

Détail de la prélecture avant raccordement : modifier aussi `Cpu80186.ChipSelect.cs` pour distinguer les attentes des lectures anticipées des cycles de données, et `Cpu80186.Strings.cs` pour reprendre REP sans relire ses préfixes. Le même cache de décodage dans `Cpu80186.cs` conserve WAIT pendant TEST actif ; les interruptions l’annulent. Les durées de BIU resteront explicitement distinctes d’une simulation des phases électriques T1–T4.

Prélecture raccordée : file de six octets, lectures de deux octets sur adresses paires et d'un octet sur cible impaire, remplissage seulement si deux places sont libres, attentes chip-select, conservation des octets préchargés après écriture CPU/DMA et vidage explicite sur tous les transferts de contrôle pris (y compris déplacement zéro), RESET et interruptions. Une prélecture déjà engagée se termine avant l'acquittement HOLD ou un accès de données ; si un saut l'a rendue inutile, ses octets sont jetés. REP et WAIT gardent leur instruction décodée et ne la relisent pas lors de chaque reprise interne. Le passage CS:FFFF vers CS:0000 recharge la bonne adresse segmentée.

Validation groupée : 1 213 tests AcornEmu exécutés, 1 212 réussis et un nouveau test de prélecture mal positionné (l'assertion précédait la fin du transfert). Correction du séquencement de ce test, puis 116 tests du 80186 réussis en 1 s. Les deux anciens tests REP comptaient uniquement les minima EU : ils comptent maintenant également les deux cycles nécessaires pour terminer le transfert anticipé avant le premier accès de données. Les autres tests du groupe Acorn étaient réussis, aucun nouveau changement du cœur depuis cette série. `git diff --check` sans erreur de contenu.

Limites explicites : le chevauchement utilise les cycles libres d'une instruction, sans décomposition microcodée de toutes ses opérations EU ni phase T1/T2/T3/T4. Ce contrôle ne prouve donc pas un modèle électrique cycle par cycle. Les durées variables MUL/IMUL/IDIV restent à établir ; DIV non signé a une durée fixe documentée (29/38 cycles registre). La table Intel indique MUL 26–28/35–37, IMUL 25–28/34–37 et IDIV 44–52/53–61 sans règle complète par opérande dans les pages consultées. MAME utilise aussi les minima et ne fournit donc pas une justification permettant de déduire ces écarts. Ne pas inventer de formule à partir d'une autre génération x86. Références : fiche Intel 80186 p.26–29 (https://www.pcjs.org/documents/datasheets/intel/INTEL_80186_CPU.pdf), manuel Intel 210912-001 et source du constructeur de l'émulateur MAME (https://raw.githubusercontent.com/mamedev/mame/master/src/devices/cpu/i86/i186.cpp). Le 80186 reste en cours ; aucun commit ni build de livraison annoncé pour ce composant.

Correction AAA/AAS : AL reçoit +/-6 et AH +/-1 séparément, au lieu de l'opération AX +/-0106h employée à partir du 80286. Ainsi AAA sur AX=12FAh donne 1300h, et AAS sur AX=1200h/AF=1 donne 110Ah. Les huit cas ajoutés couvrent aussi le débordement d'AH, l'absence d'ajustement et CF/AF ; DF est conservé, les indicateurs arithmétiques indéfinis ne sont pas utilisés comme preuve matérielle. Contrôle groupé : 124 tests 80186 réussis en 1 s. La dernière modification de l'assertion ne modifie pas le cœur et retire seulement une exigence arbitraire sur les indicateurs indéfinis.

Référence de comportement par génération : commentaire de l'implémentation Bochs `cpu/bcd.cc`, branche CPU<286 explicitant les opérations AL puis AH (https://raw.githubusercontent.com/bochs-emu/Bochs/master/bochs/cpu/bcd.cc), confronté à la description Intel ASM86 de 1983. MAME reprend une opération avec propagation sur AX pour ces cas limites ; ce point n'a pas été copié. Les durées dépendant des opérandes et la synchronisation READY externe restent ouvertes ; aucun autre composant n'est commencé.

READY de la carte Master 512 vérifié sur l'original Acorn 0143,200/C : ARDY, broche 50, et SRDY, broche 49, sont reliés ensemble au +5 V. Les deux lignes sont donc toujours prêtes ; aucun périphérique de cette carte ne suspend ces broches. Le commentaire du câblage est ajouté à `Master512Board.cs`. Les attentes programmables UCS/LCS/MCS/PCS du cœur demeurent actives. Une suspension externe READY n'est pas simulée par ce cœur ; elle ne doit pas être présentée comme une fonction réalisée, mais elle ne constitue pas un circuit variable manquant sur cette carte Acorn. Une future carte 80186 qui exploiterait ces entrées demanderait ce raccordement et son ordonnanceur de bus. L'aperçu temporaire reste uniquement jusqu'au nettoyage déjà prévu après le diagnostic de démarrage.

Comparaison isolée : désactiver temporairement la prélecture ne supprime pas les exceptions DOS Plus observées ; la prélecture est rétablie immédiatement. Le défaut de chargement restant ne doit donc pas être attribué à cette file sur cette seule hypothèse. Le diagnostic et son journal borné restent à nettoyer après identification du défaut.

## Reprise de toutes les puces demandée par l’utilisateur

- [ ] Reprendre toutes les puces Acorn depuis les BBC Micro, une par une, sans passer à la suivante tant que la précédente n’est pas terminée.
  - [x] Retirer les fichiers génériques refusés.
    - [x] Supprimer les 39 fichiers créés dans `docs/chips/acorn/` et ce dossier ; supprimer également `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu6502ConformanceDiagnostic.cs` après arrêt de son exécution, qui écrit dans ce dossier.
  - [x] Réaliser le travail demandé pour le MOS 6502.
    - [x] Créer un document MOS 6502 indiquant si tout est implémenté et listant ce qui doit être testé, sans liste de tâches ; préciser le chemin avant création.
    - [x] Modifier les fichiers du composant et des tests selon les écarts constatés ; détailler chaque chemin et contenu attendu avant modification.
    - [x] Créer les captures des essais réalisés dans `artifacts/TestsChips/Mos6502/<Machine>/`, puis supprimer les diagnostics et fichiers externes temporaires utilisés.
    - [x] Modifier le document avec les résultats réels et committer le composant terminé avant de passer au suivant.

### MOS 6502 : fichiers du bloc en cours

- [x] Consigner la couverture dans l’emplacement demandé.
  - [x] Créer `docs/chips/CPU/Mos6502.md` : identifier les fonctions présentes dans les six fichiers CPU existants, les signaux absents du contrat ICpuBus et les vérifications par domaine ; citer les sources MOS, MAME et SingleStepTests, sans créer d’autre fiche générique.
- [x] Corriger le bus des opcodes JAM après comparaison de leur séquence externe.
  - [x] Modifier `Components/Mos6502/Constants/Cpu6502Constants.cs`, `Services/Cpu6502.cs` et `Cpu6502.Unofficial.cs` : séquence d’entrée JAM puis adresse de lecture maintenue, sans accepter IRQ/NMI, et RESET rétablit le décodage.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu6502BusTests.cs` : vérifier les douze opcodes JAM, leur trace de bus et la reprise RESET ; conserver la vérification des autres opcodes.

- [x] Raccorder les signaux SO et SYNC du même 6502.
  - [x] Modifier `Components/Common/Interfaces/ICpuBus.cs`, `Components/Mos6502/Services/Cpu6502.cs` et `Cpu6502.Interrupts.cs` : entrée SO active logiquement avec détection de front, signal SYNC autour des lectures d’opcode y compris le cycle abandonné d’entrée d’interruption, désassertion en finally ; valeurs par défaut pour les cartes existantes.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu6502BusTests.cs` : SO actif/maintenu/relâché, CLV et JAM, SYNC sur opcode et sur entrée IRQ, absence de SYNC sur données et RESET.
- [x] Produire une preuve fonctionnelle dans le BBC Micro émulé.
  - [x] Créer temporairement `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu6502MachineDiagnostic.cs` : télécharger en mémoire le binaire et le listing officiels de Klaus Dormann ; exécuter ses instructions dans `BbcMicroMachine` avec sa ROM de test et RAM 32 Kio ; brancher uniquement son point de succès vers un affichage produit par le CPU sur la vraie vidéo de la machine. Enregistrer `artifacts/TestsChips/Mos6502/BbcMicroModelB/klaus-functional.png`, sans modifier les fichiers utilisateur ni prétendre tester RDY/SO/SYNC avec ce programme.
  - [x] Modifier `docs/chips/CPU/Mos6502.md` avec les résultats du groupe de tests et la configuration exacte de cette capture.
  - [x] Supprimer `AcornEmu6502MachineDiagnostic.cs` après l’essai, puis reconstruire les tests pour retirer le diagnostic ; aucun binaire, listing ou JSON externe conservé.

Exigence utilisateur transversale, valable également pour les futurs cœurs Nintendo 64, PlayStation/PS2/PS3 et Dreamcast : reproduire le comportement original et les cycles sans limiter les performances hôte à la seule fréquence temps réel ; mesurer le débit non bridé et optimiser les coûts réels sans dégrader la fidélité.

- [x] Mesurer les performances du même 6502 lors de sa validation.
  - [x] Modifier `docs/chips/CPU/Mos6502.md` : consigner le débit de cycles non bridé réellement obtenu avec sa configuration et la portée de la mesure ; distinguer exécution CPU seule, machine complète et rendu. Aucune vitesse supposée ni extrapolation aux futures consoles.

MOS 6502 : JAM, SO et SYNC corrigés et 46 cas ciblés réussis, dont le programme fonctionnel Klaus Dormann exécuté par le CPU dans le BBC Micro Model B. Capture réelle inspectée : `artifacts/TestsChips/Mos6502/BbcMicroModelB/klaus-functional.png`. Diagnostic temporaire supprimé puis assembly reconstruit ; 1 237 tests permanents AcornEmu réussis en 21 s, diagnostic externe 80186 exclu. La fiche réelle est `docs/chips/CPU/Mos6502.md`. État historique avant la réécriture RDY décrite ci-dessous ; les résultats finaux figurent en fin de section.

### Suspension RDY du 6502 NMOS sans bloquer l’hôte

- [x] Remplacer le séquencement NMOS indivisible par des phases de bus mémorisées, sans allocation par instruction.
  - [x] Créer `Components/Mos6502/Enums/Cpu6502Operation.cs`, `Cpu6502AddressMode.cs`, `Cpu6502BusPhase.cs` et `Services/Cpu6502.Decode.cs` : opérations, modes et phases nommés, table immuable des 256 décodages NMOS.
  - [x] Créer `Components/Mos6502/Services/Cpu6502.Nmos.cs` et `Cpu6502.NmosOperations.cs` : sélection et accomplissement d’un cycle, conservation des phases lors d’une lecture RDY inactive, poursuite des écritures NMOS, pile/branches/vecteurs/JAM et application des opérations sans allocations ni répétition d’écritures.
  - [x] Modifier `Components/Common/Interfaces/ICpuBus.cs`, `Components/Mos6502/Services/Cpu6502.cs` et `Machines/BbcMicro/Common/Functions/CassetteFunctions.cs` : entrée Ready par défaut active, Step NMOS utilisant le séquenceur, RESET annulant une instruction suspendue, interception cassette seulement à la frontière d’instruction ; conserver le chemin CMOS existant jusqu’à sa propre reprise.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu6502BusTests.cs` : attentes à l’opcode, aux opérandes, aux données, dans les accès RMW et la pile ; écritures non répétées, SYNC maintenu, interruptions et RESET pendant RDY.
  - [x] Modifier `docs/chips/CPU/Mos6502.md` après le contrôle groupé avec les fonctions effectivement vérifiées et leurs limites ; détailler les preuves de machine et mesures avant de les ajouter.

- [x] Retirer les anciens chemins NMOS devenus inutilisés.
  - [x] Modifier `Components/Mos6502/Services/Cpu6502.Instructions.cs` et `Cpu6502.Cmos.cs` : nommer explicitement le décodeur de base CMOS et retirer l’appel à l’ancien décodeur NMOS.
  - [x] Déplacer la seule opération ARR encore partagée de `Cpu6502.Unofficial.cs` vers `Cpu6502.NmosOperations.cs`, puis supprimer `Cpu6502.Unofficial.cs` et ses anciens décodeurs/adressages NMOS ; conserver toute leur sémantique dans la table et les phases déjà créées.
  - [x] Modifier `AcornEmu6502BusTests.cs` : remplacer les deux assertions de collection filtrée par `Assert.DoesNotContain` sans changer leur portée.
- [x] Confronter les 256 décodages NMOS complets à un corpus indépendant.
  - [x] Créer temporairement `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu6502ConformanceDiagnostic.cs` : lire les jeux SingleStepTests en mémoire à une révision identifiée, quatre téléchargements anticipés au maximum, vérifier les 10 000 cas de chaque opcode et chaque cycle ; pour JAM, comparer la fenêtre de blocage demandée par le corpus sans le traiter comme une instruction terminée. Écrire le seul bilan dans `docs/chips/CPU/Mos6502.md`, sans conserver de données externes.
  - [x] Supprimer `AcornEmu6502ConformanceDiagnostic.cs` après utilisation puis reconstruire l’assembly ; annuler/attendre tous les téléchargements en finally, aucun fichier externe temporaire conservé.

- [x] Vérifier la reprise RDY de tous les décodages, pas seulement quelques instructions.
  - [x] Modifier `AcornEmu6502BusTests.cs` : pour les 256 opcodes avec index franchissant une page, suspendre séparément chacune de leurs lectures pendant deux cycles ; comparer tous les registres et la trace complète au même programme sans attente, avec uniquement les deux répétitions de lecture attendues.
- [x] Mesurer le coût d’exécution et d’allocation du séquenceur.
  - [x] Créer temporairement `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu6502PerformanceDiagnostic.cs` : boucle CPU seule non bridée, échauffement JIT, mesure cycles/secondes et octets alloués par thread sur cinq millions d’instructions ; écrire la mesure et sa configuration dans `docs/chips/CPU/Mos6502.md` après la fin du corpus pour éviter deux rédacteurs concurrents.
  - [x] Supprimer `AcornEmu6502PerformanceDiagnostic.cs` après mesure puis reconstruire l’assembly ; ne conserver que le résultat documentaire.

- [x] Capturer les résultats du séquenceur complet dans la machine émulée.
  - [x] Créer temporairement `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu6502ScreenDiagnostic.cs` : refaire le programme Klaus sur le nouveau séquenceur et produire `artifacts/TestsChips/Mos6502/BbcMicroModelB/klaus-functional.png` ; ajouter un banc numérique de test connecté au bus BBC, contrôlé par le programme 6502, pour RDY/SO/SYNC/IRQ/NMI et produire `pins-rdy-so-sync-irq-nmi.png`. Le programme doit brancher lui-même vers échec ou succès et écrire son résultat en RAM télétexte. Aucune assertion de couverture hors de ces scénarios.
  - [x] Modifier `docs/chips/CPU/Mos6502.md` avec la configuration et les résultats de ces captures, puis supprimer `AcornEmu6502ScreenDiagnostic.cs` et reconstruire l’assembly pour retirer tous les diagnostics temporaires.

- [x] Retirer l’état JAM de l’ancien interpréteur, désormais porté uniquement par la phase NMOS.
  - [x] Modifier `Components/Mos6502/Services/Cpu6502.cs`, `Cpu6502.Nmos.cs` et `Constants/Cpu6502Constants.cs` : supprimer `_jammed`, son test CMOS inaccessible et la constante de durée inutilisée ; conserver la phase JAM et toutes ses lectures.

- [x] Conserver la métadonnée d’instruction utilisée par le décodage mémoire des cartes.
  - [x] Modifier `Components/Mos6502/Services/Cpu6502.Nmos.cs` et `Cpu6502.cs` : publier InstructionAddress avant la lecture d’opcode, comme avant la refonte, afin que le bus B+ puisse choisir la banque au bon cycle ; préciser que Step peut rendre la main sur RDY.
  - [x] Modifier `AcornEmu6502BusTests.cs` : vérifier, dans le rappel de lecture du bus, l’adresse publiée sur les opcodes avant et après un saut, en NMOS et CMOS.

- [x] Fermer la fenêtre RESET/NMI identifiée dans la comparaison du séquenceur.
  - [x] Modifier `Components/Mos6502/Services/Cpu6502.cs` : à la fin du RESET NMOS, conserver le sondage NMI du cycle précédent pour accepter un front suffisamment précoce avant la première instruction ; laisser un front du dernier cycle attendre le sondage suivant.
  - [x] Modifier `AcornEmu6502BusTests.cs` : injecter une impulsion NMI séparément à chacun des sept cycles RESET et vérifier le vecteur, l’instruction non exécutée/exécutée selon la fenêtre, le retour RTI, ainsi qu’IRQ masquée et conservation de D/V au RESET NMOS.

- [x] Confronter la durée de RESET aux signaux de la fiche MOS originale.
  - [x] Créer temporairement `%TEMP%/RetroMediaLab-6502-signals.png` : aperçu des pages de signaux de la fiche MCS6500 pour distinguer la séquence numérique du CPU et le maintien externe de RESET ; supprimer l’aperçu après lecture et inscrire la conclusion vérifiable dans `docs/chips/CPU/Mos6502.md`.

Instruction utilisateur : après achèvement et commit du MOS 6502, s’arrêter pour discuter avec l’utilisateur avant toute autre puce. Ne commencer aucun autre circuit entre-temps.

- [x] Lire la fiche constructeur avec la chaîne de certificats Windows.
  - [x] Créer temporairement `%TEMP%/RetroMediaLab-6502-signals.pdf` avec `Invoke-WebRequest` et TLS normal : le téléchargement Python échoue sur la chaîne de certificats du site, aucun contrôle TLS ne doit être désactivé ; produire l’aperçu déjà prévu avec PyMuPDF puis supprimer PDF et aperçu après lecture.

- [x] Représenter le maintien externe de RESET, confirmé page 6 de la fiche Synertek SY6500.
  - [x] Modifier `Components/Common/Interfaces/ICpuBus.cs`, `Components/Mos6502/Enums/Cpu6502BusPhase.cs`, `Services/Cpu6502.cs` et `Cpu6502.Nmos.cs` : entrée logique ResetRequested, horloges sans transfert pendant le maintien, séquence de sept lectures après libération, annulation de l’instruction interrompue, phases RESET conservées si RDY retient une lecture, sondage NMI final ; chemin CMOS conservé.
  - [x] Modifier `AcornEmu6502BusTests.cs` : RESET au milieu d’un STA, absence de transfert tant que la ligne est maintenue, reprise au vecteur et conservation des registres, puis RDY retenant une lecture de pile du RESET sans double décrément de S.

- [x] Raccorder DBE, décrit page 6 de la fiche SY6500 et présent sur le 6502, absent du 6512.
  - [x] Modifier `Components/Common/Interfaces/ICpuBus.cs` et `Components/Mos6502/Services/Cpu6502.Nmos.cs` : entrée DataBusEnabled active par défaut ; signaler les écritures haute impédance via WriteUndriven sans inventer un octet ni une règle mémoire ; conserver adresse/RW et effets internes du CPU. Les cartes 6512 n’exploitent pas cette broche inexistante.
  - [x] Modifier `AcornEmu6502BusTests.cs` : écriture mémoire et empilement sans pilotage des données, lectures inchangées, reprise du pilotage sans blocage du CPU.

- [x] Utiliser l’opération décodée dans le sondage d’interruption.
  - [x] Modifier `Components/Mos6502/Services/Cpu6502.Nmos.cs` : remplacer les trois valeurs d’opcode brutes CLI/SEI/PLP par les valeurs de l’enum et supprimer le champ d’opcode devenu inutile.
- [x] Compléter la preuve vidéo de RESET et DBE.
  - [x] Créer temporairement `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu6502ResetScreenDiagnostic.cs` : un programme en ROM de test BBC pilote un maintien RESET de quatre cycles, compare sa reprise au vecteur, le décrément de pile, I et D, puis vérifie une écriture haute impédance DBE et le retour du pilotage. Le banc externe signale la haute impédance sans écrire de donnée à sa mémoire de test ; les comparaisons et l’affichage sont exécutés par le CPU. Capturer `artifacts/TestsChips/Mos6502/BbcMicroModelB/reset-dbe.png`.
  - [x] Supprimer `AcornEmu6502ResetScreenDiagnostic.cs` et les deux fichiers `%TEMP%/RetroMediaLab-6502-signals.pdf` et `.png` après inspection ; modifier la fiche avec le résultat, puis reconstruire les tests pour retirer le diagnostic.
- [x] Livrer le bloc MOS 6502 terminé, puis s’arrêter avant toute autre puce.
  - [x] Modifier `docs/chips/CPU/Mos6502.md` : clôturer chaque domaine seulement après ses contrôles et préciser les variantes et comportements indéfinis, les preuves et les mesures finales.
  - [x] Modifier `docs/tasks/acornemu-chip-audit.md` avec les résultats finaux du contrôle Acorn, du script Debug modules A et des fichiers de sortie ; indexer uniquement les fichiers MOS 6502, contrat de bus, raccordement cassette, tests et fiche/captures concernés, puis créer leur commit indépendant.

Validation finale du séquenceur MOS 6502 : 1 260 tests permanents AcornEmu réussis, zéro échec, 22 s ; diagnostic externe 80186 exclu. Capture RESET/DBE inspectée, diagnostic supprimé et assembly reconstruit. Script Debug modules A terminé avec code 0 ; exécutable build/Debug/Retro Media Lab/retromedialab.exe présent et 24 DLL de modules vérifiées.

Commit indépendant MOS 6502 : `60c1d94dd`. Arrêt à cette frontière conformément à la demande utilisateur ; discussion requise avant toute autre puce.
