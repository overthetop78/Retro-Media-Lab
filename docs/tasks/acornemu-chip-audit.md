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

## Reprise de toutes les puces demandée par l’utilisateur

- [ ] Reprendre toutes les puces Acorn depuis les BBC Micro, une par une, sans passer à la suivante tant que la précédente n’est pas terminée.
  - [ ] Retirer les fichiers génériques refusés.
    - [x] Supprimer les 39 fichiers créés dans `docs/chips/acorn/` et ce dossier ; supprimer également `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu6502ConformanceDiagnostic.cs` après arrêt de son exécution, qui écrit dans ce dossier.
  - [ ] Réaliser le travail demandé pour le MOS 6502.
    - [ ] Créer un document MOS 6502 indiquant si tout est implémenté et listant ce qui doit être testé, sans liste de tâches ; préciser le chemin avant création.
    - [ ] Modifier les fichiers du composant et des tests selon les écarts constatés ; détailler chaque chemin et contenu attendu avant modification.
    - [ ] Créer les captures des essais réalisés dans `artifacts/TestsChips/Mos6502/<Machine>/`, puis supprimer les diagnostics et fichiers externes temporaires utilisés.
    - [ ] Modifier le document avec les résultats réels et committer le composant terminé avant de passer au suivant.

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
- [ ] Livrer le bloc MOS 6502 terminé, puis s’arrêter avant toute autre puce.
  - [x] Modifier `docs/chips/CPU/Mos6502.md` : clôturer chaque domaine seulement après ses contrôles et préciser les variantes et comportements indéfinis, les preuves et les mesures finales.
  - [ ] Modifier `docs/tasks/acornemu-chip-audit.md` avec les résultats finaux du contrôle Acorn, du script Debug modules A et des fichiers de sortie ; indexer uniquement les fichiers MOS 6502, contrat de bus, raccordement cassette, tests et fiche/captures concernés, puis créer leur commit indépendant.

Validation finale du séquenceur MOS 6502 : 1 260 tests permanents AcornEmu réussis, zéro échec, 22 s ; diagnostic externe 80186 exclu. Capture RESET/DBE inspectée, diagnostic supprimé et assembly reconstruit. Script Debug modules A terminé avec code 0 ; exécutable build/Debug/Retro Media Lab/retromedialab.exe présent et 24 DLL de modules vérifiées.
