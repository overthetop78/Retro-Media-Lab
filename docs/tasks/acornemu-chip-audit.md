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

    - [ ] Modifier `Services/Intel8271*.cs`, `Wd1770*.cs`, `Upd765*.cs` et `Upd765DriveControl.cs` selon les écarts confirmés : variantes, commandes, recherche d'identifiants, flux, index, DMA/PIO, reset, erreurs, protections et cadences. Reprendre les limites NEC encore ouvertes dans `docs/tasks/acornemu-archimedes.md`, sans considérer les seuls secteurs réguliers comme preuve de fidélité complète.

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

- [ ] Auditer et corriger le MC6809 avant son commit indépendant.
  - [x] Modifier `Components/Mc6809/Services/Cpu6809.cs`, `Cpu6809.Interrupts.cs` et `Cpu6809.Addressing.cs` : mémoriser la NMI avant LDS, autoriser sa prise après toute instruction chargeant S, et distinguer l'attente SYNC de CWAI selon les masques.
  - [x] Modifier `Components/Mc6809/Services/Cpu6809.Addressing.cs`, `Cpu6809.Instructions.cs` et les constantes : placer les cycles internes et lectures transitoires dans les séquences d'adressage et de lecture-modification-écriture ; vérifier les registres/indicateurs et codes physiques documentés par les sources.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu6809Tests.cs` : régressions autonomes NMI/pile/attentes, indicateurs, séquences mémoire et modes indexés, en conservant les tests System existants.
  - [ ] Modifier ce suivi avec les corrections, références et résultats groupés ; construire Debug All et committer le 6809 séparément avant une autre puce.

  - [x] Modifier `Components/Mc6809/Services/Cpu6809.Instructions.cs`, `Cpu6809.Interrupts.cs`, `Cpu6809.Addressing.cs` et `Constants/Cpu6809Constants.cs` : séquencer PSH/PUL avant les accès pile, ajouter les lectures finales RTS/RTI et les prélectures CWAI/ABX/MUL ; nommer les délais ajoutés.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu6809Tests.cs` : vérifier les cycles des accès pile PSHS/PULS/RTS/RTI et CWAI avec un bus autonome.
  - [x] Modifier `Components/Mc6809/Services/Cpu6809.Instructions.cs`, `Cpu6809.Addressing.cs`, `Cpu6809.cs` et `Constants/Cpu6809Constants.cs` : reproduire les alias unaires et NOP, le mode de test HCF jusqu'à RESET et les formes indexées non documentées, sans exception hôte pour ces codes physiques ; ajouter leurs régressions dans `AcornEmu6809Tests.cs`.
  - [x] Modifier `Components/Mc6809/Services/Cpu6809.Instructions.cs` et `AcornEmu6809Tests.cs` : reproduire CC/DP sur les deux octets lors de TFR et EXG avec source 8 bits, conserver la différence EXG avec source 16 bits et calculer V après DAA selon les mesures matérielles publiées.

Références MC6809 de ce complément : manuel Motorola, annexe instructions https://www.maddes.net/m6809pm/appendix_a.htm ; séquences mémoire https://github.com/mamedev/mame/blob/master/src/devices/cpu/m6809/base6x09.lst et m6809.lst ; lecture TFR/EXG dans m6809.cpp ; mesures originales des codes non documentés https://github.com/hoglet67/6809Decoder/wiki/Undocumented-6809-Behaviours. Les accès invalides VMA restent des cycles internes du contrat de bus actuel.

Points encore ouverts à ce stade intermédiaire : décodage des préfixes multiples et alias des pages 2/3, opcodes X18/XANDCC/XRES et stores immédiats (leurs indicateurs dépendent de l'instruction précédente), puis contrôle des séquences RESET. Leur clôture figure dans le complément ci-dessous.

Vérification groupée du complément : 1 066 tests AcornEmu réussis, aucun échec (18 s). Le premier passage a détecté un mauvais emplacement de la garde HCF dans RESET au lieu de Step ; cette erreur introduite dans le complément a été corrigée et les trois variantes HCF passent désormais. Aucun test ni fichier externe temporaire ajouté. Aucun build de livraison ni commit MC6809 effectué à ce stade.

- [ ] Achever le décodage MC6809 dans le groupe en cours.
  - [x] Créer `Components/Mc6809/Services/Cpu6809.Unofficial.cs` et modifier `Cpu6809.cs`, `Cpu6809.Instructions.cs`, `Cpu6809.Interrupts.cs`, `Cpu6809Constants.cs` : préfixes répétés, alias de pages, X18/XANDCC/XRES, stores immédiats avec état des indicateurs issu de l'instruction précédente et RESET avec lectures du vecteur après les cycles internes.
  - [x] Modifier `AcornEmu6809Tests.cs` : régressions autonomes des préfixes et cycles, vecteurs, stores immédiats et séquence RESET ; modifier ce suivi avec les résultats du contrôle groupé.
  - [ ] Modifier `build/Debug` par le script Debug All, puis ce suivi avec le résultat ; modifier l'index Git pour la puce MC6809, ses tests et ce suivi et créer son commit indépendant.

Clôture des points de décodage identifiés : première page de préfixe conservée, préfixes suivants consommés chacun sur un cycle, repli des alias vers l'instruction de base et compléments spécifiques XLBRA/XADDD/XADDU/XRES/XSWI2/XFIRQ. Un flux sans fin de préfixes rend la main à l'hôte après un tour d'adressage sans accepter d'interruption au milieu de l'instruction. Les stores immédiats lisent l'octet haut et écrivent uniquement l'octet bas pour les formes 16 bits ; NZV dépend du groupe d'instruction précédent pour les formes non préfixées. RESET conserve les indicateurs arithmétiques, force I/F et DP=0 ; le vecteur est lu aux cycles 5 et 6 d'une entrée de 7 cycles.

Résultat final groupé : 1 088 tests AcornEmu réussis en 18 s, dont 57 cas MC6809 autonomes. Le contrôle de décodage des 256 codes des trois pages prouve l'absence d'exception hôte pour ces codes ; les autres cas vérifient les états et séquences précisés dans leurs assertions, sans prétendre couvrir toutes les combinaisons d'opérandes ni toutes les révisions physiques. Aucun corpus, firmware ou processus externe temporaire utilisé dans ces tests. Références supplémentaires : mesures de bus et groupes d'indicateurs de https://github.com/hoglet67/6809Decoder/blob/master/src/em_6809.c. Debug All en cours avant le commit séparé.

Livraison MC6809 : `scripts/local-building.cmd --building=debug --modules=A` terminé avec code 0. Exécutable `build/Debug/Retro Media Lab/retromedialab.exe` présent et 24 DLL de modules vérifiées. `git diff --check` ne signale aucune erreur de contenu. Le commit suivant porte exclusivement sur la puce MC6809, ses régressions et ce suivi.
