# Z80 — cœur d'exécution par les broches

## Réalisation du 11 novembre 2026

Premier cœur **inachevé**, assemblant les registres, le séquenceur de bus et les bascules déjà réalisés. Il n'est pas raccordé à SegaEmu/SG-1000. L'objectif reste la puce complète : aucun opcode, signal, mode ou comportement manquant n'est supprimé du périmètre pour cette étape.

Sources constructeur dans la [fiche principale](../Z80.md) : Zilog UM008011-0816, transferts pages 71–119, échanges 124–126, JP 262–265, RET 285, RETN 290, IN/OUT 295/306 ; bascules et modes dans la [fiche interruptions](interrupts.md). Les fiches LD dd,(nn) et LD (nn),dd incluent BC/DE/HL/SP et leurs transferts ED de 20 T. Les chronogrammes de bus viennent de la fiche NEC archivée. Pas de code ni de banc d'un autre émulateur utilisé comme preuve matérielle.

## Fichiers et fonctionnement

Racine `src/RetroMediaLab.Chips/CPU/Zilog/Z80/Z80/` :

| Fichier | Responsabilité |
| --- | --- |
| `Interfaces/ICpuZ80.cs` | Entrées CLK, données et WAIT, sorties du bus, échéances, observation des registres et capture/restauration. Le contrat devra être complété avec les autres entrées/sorties avant clôture du CPU. |
| `Services/CpuZ80.cs` | Assemblage des blocs, réception de l'horloge extérieure et des niveaux, frontières et compte d'instructions, publication des échéances sans notification provisoire à chaque sous-opération. Aucun thread, oscillateur ou appel mémoire de carte. |
| `Services/CpuZ80.Bus.cs` | Chaînage des fetchs, opérandes et transferts au front de frontière courant ; PC et compteur R ; une seule progression canonique, sans file logicielle d'instructions. |
| `Services/CpuZ80.Instructions.cs` | Décodage des instructions déjà réalisées et effets sur les registres, la mémoire et les ports via le bus. |
| `Services/CpuZ80.Arithmetic.cs` | Huit opérations ALU 8 bits, INC/DEC registre, rotations d'accumulateur, CPL et NEG ; raccordement des indicateurs, dont CP depuis l'opérande pour F3/F5. |
| `Services/CpuZ80.Prefixes.cs` | Décodage CB des rotations/décalages documentés et RES/SET sur registres ; les autres formes restent ouvertes. |
| `Services/CpuZ80.Internal.cs` | États internes en clocks reçues, arithmétique 16 bits, pile, conditions et lectures-modifications-écritures ; aucune transaction fictive. |
| `Services/CpuZ80.Indexed.cs`, `Enums/CpuZ80Index.cs` | Sélection DD/FD dans les registres réels, déplacements signés, continuations indexées et DDCB/FDCB. |
| `Services/CpuZ80.Extended.cs` | Transferts I/R hors prise d'interruption, E/S registre par C, rotations de chiffres RLD/RRD. |
| `Enums/CpuZ80Flow.cs`, `Enums/CpuZ80Operation.cs` | Position de progression et opération en cours, conservées lors d'une reprise. |
| `Constants/CpuZ80Constants.cs` | Version du format d'état. |
| `Contracts/CpuZ80State.cs`, `Services/CpuZ80.State.cs` | Registres, entrées, bus et propagations, bascules, octets temporaires et progression. Validation avant remplacement ; mémoire et autres composants restent au propriétaire. |

La construction exige le profil temporel, les registres initiaux, l'état des bascules et le niveau initial des données. Elle ne prétend pas reproduire une mise sous tension ou une impulsion RESET. Le cœur reçoit les fronts de sa vraie source ; il ne décide pas d'une cadence propre à la SG-1000. Le profil choisi par les tests est celui des maxima NEC à 4 MHz, pas un profil implicitement choisi pour la console.

### Instructions réalisées à cette étape

- NOP ; LD r,r', LD r,n, LD r,(HL), LD (HL),r/n ; LD A,(BC)/(DE) et inverse ; LD A,(nn) et inverse.
- LD BC/DE/HL/SP,nn ; LD HL,(nn), LD (nn),HL ; les transferts ED LD dd,(nn) et LD (nn),dd pour les quatre paires.
- EX AF,AF', EXX, EX DE,HL ; POP BC/DE/HL/AF ; RET et RETN.
- JP nn, les huit conditions JP cc,nn, JP (HL).
- IN A,(n), OUT (n),A ; DI, EI et IM0/1/2 par leurs encodages documentés.
- ADD/ADC/SUB/SBC/AND/XOR/OR/CP : 64 encodages registre/(HL) et huit immédiats ; INC/DEC des sept registres ; RLCA/RRCA/RLA/RRA, CPL, NEG ED44.
- CB sur les sept registres : rotations/décalages dont SLL (56 encodages), RES/SET des huit bits (112 encodages), soit 168 encodages ; 24 formes mémoire correspondantes. BIT reste à réaliser avec MEMPTR.
- INC/DEC BC/DE/HL/SP ; LD SP,HL ; ADD HL,dd et ADC/SBC HL,dd ED ; PUSH des quatre paires ; CALL, les huit CALL/RET conditionnels, les huit RST ; JR, les quatre JR conditionnels et DJNZ ; INC/DEC (HL).
- DD/FD : variantes IX/IY des transferts, paires, octets, ALU, pile et sauts ci-dessus ; déplacements signés, préfixes successifs, exceptions H/L et neutralisation par ED. DDCB/FDCB : 192 encodages de rotation/décalage/RES/SET par préfixe, avec copies registre ; BIT reste ouvert. EX (SP),HL/IX/IY est réalisé avec maintien des dernières données.
- ED : LD I,A, LD R,A, LD A,I, LD A,R hors prise d'interruption ; IN des sept registres et IN flags,(C), OUT (C) des sept registres documentés ; RLD/RRD. OUT (C),0 reste lié au profil de silicium non encore terminé.

Les transferts sans effet sur les indicateurs conservent le registre F complet ; POP AF et EX AF échangent les octets réels. Les helpers arithmétiques fournissent les six flags documentés ; leur raccordement ajoute F3/F5 depuis le résultat, sauf CP qui les reçoit de l'opérande. Références : UM008011-0816 pour les opérations, les cycles et les six flags ; Young v0.91, sections 2.3 et tables 8.4–8.8 pour les bits 3/5. Cette transcription du compendium ne constitue pas une caractérisation physique exhaustive sur un µPD780C-1 identifié ; la qualification de variante reste ouverte.

Les opcodes encore inachevés produisent un arrêt diagnostique explicite `NotSupportedException`, sans exécution fictive de NOP. De même, une interruption éligible dont la réponse n'est pas intégrée arrête ce cœur incomplet au lieu d'être ignorée silencieusement. **Ces arrêts ne sont pas des comportements du matériel** et devront disparaître à la réalisation des fonctions concernées ; ils interdisent de présenter le cœur comme terminé. Ils ne justifient ni le passage au VDP ni la validation de la console.

### États et événements

Une seule instance du bus traite les cycles, échantillons et propagations ; le cœur ne recopie ni sa clock ni son agenda. Les méthodes de données/WAIT transmettent des changements au bloc propriétaire. Les rappels n'autorisent pas une progression réentrante qui déplacerait le temps. La capture ne possède pas de mémoire de carte ; le banc restaure séparément sa mémoire puis reprend avec les entrées et sorties sauvegardées.

La représentation de progression reste calculée : elle conserve les états nécessaires à la continuation des accès sans prétendre reproduire chaque bascule interne du silicium. Aucun débit ni gain d'optimisation n'est déduit de ce choix d'architecture. La qualification et les mesures restent à mener sur le CPU entier puis sur la machine complète.

## Passage Debug — 11 novembre 2026

`tests/RetroMediaLab.Chips.Tests/CPU/Zilog/Z80/Z80/ExecutionTests.cs` ajoute **15 cas** exécutant des programmes synthétiques par CLK, RD/WR, MREQ/IORQ, adresses et données. Le banc possède sa mémoire de 64 Kio et ses ports numériques ; le CPU ne reçoit aucun accès direct à leurs tableaux. Les sorties d'écriture sont acquittées depuis leurs transitions, pas depuis une lecture artificielle des registres. Les rappels sont détachés dans `Dispose`, appelé par `using` ; aucune fenêtre, application, DLL externe ou tâche de fond n'est créée.

Vérifications :

- Programme de 17 instructions : résultat mémoire, ordre des sept écritures, registres et F conservé ; **164 T**, soit **41 000 ns** sous l'horloge de banc de 4 MHz.
- Échanges et huit transferts ED de mots : banques et ordre little-endian ; **176 T**. R ne compte que les M1, y compris les préfixes ; il conserve son bit 7.
- Huit JP conditionnels, sous six combinaisons d'indicateurs : les deux opérandes sont réellement lus dans chaque cas, branche prise ou non, **10 T**.
- POP des quatre paires et rebouclage SP FFFF→0000 ; RET **10 T** et RETN **14 T**, adresse et bascules de retour.
- E/S immédiates : A sur A8–A15, données et indicateurs ; LD A,n puis OUT/IN : **29 T**.
- EI/DI et modes ED : une seule instruction retirée par préfixe + opcode, bascules et **32 T** pour la séquence.
- Reprise pendant WAIT et à **21 dates** d'un programme multi-octets comprenant lectures et écritures : mêmes sorties datées, mémoire, registres et progression. Les timings testés ne sont pas une mesure de débit hôte.
- Rejet d'états incompatibles avant remplacement et contrôle provisoire d'un opcode non encore intégré.

Commande : `dotnet test tests/RetroMediaLab.Chips.Tests/RetroMediaLab.Chips.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~RetroMediaLab.Chips.Tests.CPU.Zilog.Z80.Z80" --logger "trx;LogFileName=20261111-execution-transfer-bus.trx" --results-directory artifacts/TestsChips/Z80/SG-1000`.

Résultat : **66 réussis, 0 échec, 0 ignoré**, code 0, durée du résumé 1 s : les 15 cas d'exécution et les 51 cas antérieurs passent ensemble. Preuve : `artifacts/TestsChips/Z80/SG-1000/20261111-execution-transfer-bus.trx`. Un avertissement xUnit2031 du nouveau banc concernait `Assert.Single` après `Where` ; il a été corrigé après le passage en employant l'overload avec prédicat. Le même avertissement antérieur dans `Shared/ArchitectureTests.cs:33` reste hors du chantier. Les deux échecs d'architecture générale déjà consignés ne sont pas déclarés corrigés par ce passage ciblé.

## Ce qui reste à réaliser

Consigne du 11 novembre 2026 : achever désormais le CPU entier avant la validation d'ensemble ; ne plus relancer un petit groupe après chaque famille. Les cases de réalisation du code ne valent jamais validation matérielle.

Sources complémentaires acquises : rapport expérimental boo_boo/Kladov de 2006, `BooBoo-MEMPTR-2006.txt` (tests physiques crédités à Wlodek, CHRV, icebear, molodcov_alex et goodboy), pour WZ et BIT ; `Banks-Undocumented-Flags.html` pour SCF/CCF selon silicium, Q et indicateurs des blocs répétés ; `MEMPTR-reference-20260107.html` comme synthèse de pistes et liens vers les expériences. La copie HTTP de la discussion Stardot sur les blocs a échoué sur une protection JavaScript : aucun fichier de cette discussion n'est déclaré archivé. Sa lecture Web reste une piste à qualifier, pas un résultat d'essai du CPU local. Les anciennes règles de répétition INIR/OTIR du texte de 2006 sont contredites par les expériences ultérieures : ne pas les reprendre sans cette confrontation.

Toutes les autres instructions et combinaisons restantes, notamment décimal, BIT/MEMPTR, SCF/CCF et variantes, OUT (C),0, les blocs et comportements non documentés établis ; contrôle temporel des interruptions et leurs transferts, particularité P/V de LD A,I/R, HALT, RESET, BUSREQ/BUSACK, sorties de contrôle et haute impédance. Les frontières de concession à l'intérieur des suites d'états internes doivent notamment être raccordées à l'arbitrage réel ; le compteur interne seul ne les réalise pas. Compléter ensuite les états et leur validation pour ces fonctions, les contrôles d'ensemble, les mesures et optimisations du CPU puis de la SG-1000. La réalisation de cette étape n'en clôture aucun élément manquant.

Références ED reprises le 11 novembre 2026 : UM008011-0816 pages 94–97 pour I/R et IFF2, 238–241 pour RLD/RRD, 296–297 et 307 pour les E/S par C. Le manuel précise que P/V est effacé si une interruption survient pendant LD A,I/R ; cet effet doit être intégré et vérifié avec le traitement temporel des interruptions, pas considéré réalisé par les transferts seuls. Les [captures ED](https://baltazarstudios.com/webshare/ed.html), archivées sous `Devic-Z80-ED-traces.html`, montrent les quatre états internes entre lecture et écriture RLD/RRD et les E/S par BC. Elles conservent les limites de résolution et de variante déjà indiquées. Young complète les bits 3/5 ; OUT (C),0 ne reçoit aucun choix implicite de variante dans ce lot.

## Extension ALU et CB — 11 novembre 2026

Les nouveaux calculs réutilisent les helpers et le bus existants, sans autre moteur d'exécution, mémoire hôte directe, table reconstruite ou objet alloué par instruction. Les calculs de bits sont directs ; les sorties et leurs délais restent ceux du séquenceur canonique. Les deux M1 du CB incrémentent R séparément et ne constituent qu'une instruction pour EI et les frontières. L'état de continuation utilise les champs existants et la nouvelle valeur de progression CB, sans second état parallèle.

Deux passages groupés Debug : **70 succès** après ALU, puis **72 succès, 0 échec, 0 ignoré**, code 0 après CB ; durée de chaque résumé 1 s. Le dernier englobe les 51 cas antérieurs et 21 cas d'exécution. Preuves : `artifacts/TestsChips/Z80/SG-1000/20261111-execution-alu.trx` et `20261111-execution-alu-cb.trx`. Commande identique au passage précédent avec `LogFileName=20261111-execution-alu-cb.trx`. Aucun échec d'architecture Acorn n'est requalifié par ces tests ciblés.

Les six nouveaux cas regroupent : les huit familles ALU sur tous les sélecteurs et les trois parcours, frontières de signe/demi-retenue et retenues entrantes ; INC/DEC registre aux frontières ; rotations/CPL/NEG pour les 256 valeurs et deux états d'indicateurs ; reprise d'opérande à quatre dates ; les 161 encodages CB sur dix motifs et deux états d'indicateurs, registres non ciblés conservés ; reprise entre les deux M1 sans décrément prématuré du délai EI. Les oracles utilisent arithmétique signée, chiffres hexadécimaux, divisions et parité scalaire, sans appeler les helpers de production. Timings vérifiés : 4 T registre simple, 7 T ALU mémoire/immédiate, 8 T NEG et CB registre. Ce sont des contrôles fonctionnels autonomes, pas des relevés matériels ni des mesures de vitesse hôte.

Recherche conservée pour la suite : David Banks, [Undocumented Flags](https://github.com/hoglet67/Z80Decoder/wiki/Undocumented-Flags) et [Results NEC NMOS](https://github.com/hoglet67/Z80Decoder/wiki/Results-NEC-NMOS). Il rapporte des captures physiques et distingue notamment SCF/CCF NEC NMOS de Zilog NMOS. Ces différences interdisent d'ajouter une règle unique de SCF/CCF au cœur générique sans profil de silicium et provenance précisés ; aucune règle SCF/CCF n'a été choisie implicitement dans ce lot. Aucune question utilisateur nécessaire à ce stade.

## Cycles internes — sources acquises le 11 novembre 2026

Goran Devic publie des [traces d'un Z80 physique](https://baltazarstudios.com/zilog-z80-undocumented-behavior/), avec le [montage Arduino de capture](https://baltazarstudios.com/arduino-zilog-z80/). Son guide A-Z80 révision 1.6 du 4 mars 2020, page 18, confirme l'origine matérielle des tables ; le FPGA n'est pas utilisé comme preuve. Archives `Devic-Z80-physical-bus.html`, `Devic-Z80-traces-provenance.html`, `Devic-Z80-base-traces.html`, `Devic-Z80-CB-traces.html` et `Devic-A-Z80-UsersGuide-20200304.pdf` sous le dossier Zilog déjà utilisé.

Les traces [DD](https://baltazarstudios.com/webshare/dd.html) et [DDCB](https://baltazarstudios.com/webshare/ddcb.html), archivées sous `Devic-Z80-DD-traces.html` et `Devic-Z80-DDCB-traces.html`, complètent les accès indexés : déplacement signé lu par le bus, puis états internes conservant l'adresse d'opérande avant l'accès aux données. LD (IX+d),n place ses deux états internes après l'immédiat. Le manuel Zilog couvre les formes IX/IY ; Young v0.91 sections 3.2/3.3 et 3.7 décrit les exceptions H/L, ED, EXX et EX DE,HL ainsi que le dernier préfixe DD/FD actif. Ces règles issues du compendium restent à qualifier exhaustivement sur la variante NEC ; elles ne deviennent pas une preuve physique par accord entre émulateurs.

Les [traces de base](https://baltazarstudios.com/webshare/xx.html) montrent les sorties inactives après le rafraîchissement pendant les états internes d'INC dd, LD SP,HL, ADD HL,dd, PUSH/RST, RET conditionnel, CALL et JR. INC/DEC (HL) comporte un état interne entre lecture et écriture. Elles observent les phases hautes et seulement huit lignes d'adresse : elles ne prouvent ni les délais nanosecondes des transitions, ni A8–A15, ni toutes les particularités NEC. Les timings et cycles documentés restent confrontés au manuel constructeur ; les délais de propagation existants restent explicitement ceux du profil NEC choisi par le banc. Aucune équivalence exhaustive des variantes n'est déduite.

### Réalisation et validation des états internes

Le CPU conserve le nombre de clocks restantes et la continuation, reçoit chaque vrai front de sa source et laisse le séquenceur unique terminer les propagations du dernier accès. Il ne génère ni accès, ni rafraîchissement, ni échantillonnage WAIT supplémentaires pendant ces états. Les opérations utilisent les helpers arithmétiques existants ; aucune file ou allocation par instruction. Format d'état interne porté à **2** : compteur et continuation cohérents sont validés avant mutation. Les états de version 1 de ce cœur en cours ne sont pas déclarés compatibles.

| Groupe | Timings vérifiés, sans attente externe |
| --- | --- |
| INC/DEC paires ; LD SP,HL | 6 T |
| ADD HL,dd ; ADC/SBC HL,dd | 11 T ; 15 T |
| PUSH ; RST | 11 T |
| CALL pris / non pris | 17 / 10 T |
| RET conditionnel pris / non pris | 11 / 5 T |
| JR pris / non pris ; DJNZ pris / non pris | 12 / 7 T ; 13 / 8 T |
| INC/DEC (HL) ; CB (HL) réalisé | 11 T ; 15 T |

`InternalExecutionTests.cs` partage le banc autonome existant via une classe partielle. Sept nouveaux cas regroupés vérifient les quatre paires, retenues/signe/demi-retenue 16 bits, pile haute puis basse et rebouclage, toutes les conditions, déplacements signés et rebouclage PC, les 23 formes CB mémoire et INC/DEC mémoire, une seule écriture par modification, les sorties inactives pendant les états internes et l'absence d'étirement par WAIT. Les 11 scénarios de reprise couvrent **266 dates**, à chaque demi-période, avec comparaison du CPU, des sorties datées, des écritures et de la mémoire.

Premier passage : **79 succès Debug**, aucun échec ou ignoré, code 0, durée du résumé 1 s. Preuve : `artifacts/TestsChips/Z80/SG-1000/20261111-execution-internal-stack.trx`. Le parcours de test emploie les maxima NEC à 4 MHz ; il ne mesure pas la vitesse hôte et ne prouve pas l'arbitrage encore absent.

### Correction du rafraîchissement du nouveau cœur

La confrontation révèle un décalage d'une unité dans `CpuZ80.Bus.cs` : le nouveau cœur fournissait au bus la valeur de R déjà incrémentée. Les relevés Devic montrent IR nul après reset puis le premier rafraîchissement à zéro. L'adresse du M1 courant est désormais formée avant l'incrément destiné au suivant. Le [relevé détaillé](https://baltazarstudios.com/webshare/Z80-undocumented-behavior.htm) est archivé sous `Devic-Z80-undocumented-behavior.htm` ; il ne change pas le constat de variante et de résolution temporelle limitées des captures.

Un cas supplémentaire vérifie trois M1 dont un préfixe, les adresses successives et le rebouclage des sept bits de R sans modifier R7. Le passage groupé final réussit **80 cas Debug, 0 échec, 0 ignoré**, code 0, durée du résumé 1 s. Preuve : `artifacts/TestsChips/Z80/SG-1000/20261111-execution-internal-refresh.trx`. Même commande de filtre que précédemment, avec ce nom de TRX. L'avertissement préexistant `Shared/ArchitectureTests.cs:33` demeure ; aucun contrôle général précédemment en échec n'est déclaré corrigé.

## DD/FD, DDCB/FDCB et échange de pile — 11 novembre 2026

`CpuZ80.Indexed.cs` fournit une vue sur les registres IX/IY réellement stockés, sans recopier HL. Le préfixe actif, les déplacements et les continuations appartiennent au CPU et à son état de version **3**. Chaque préfixe DD/FD reçoit son M1 ; le dernier sélectionne IX/IY et ED neutralise cette sélection. Les exceptions H/L, EXX et EX DE,HL sont conservées. Les formes non modifiées par un préfixe gardent leurs effets et leur coût de fetch supplémentaire. Aucune sélection dépend du nom d'une machine.

Les accès indexés passent par le bus unique : déplacement signé puis états internes, lectures et écritures aux adresses rebouclées sur 16 bits. LD (IX/IY+d),n place l'immédiat avant ses deux états internes. DDCB/FDCB lit déplacement et dernier opcode comme opérandes : seuls les deux premiers octets produisent M1 et rafraîchissement. Les rotations/décalages/RES/SET écrivent la mémoire et copient vers le registre réel sélectionné lorsque demandé, y compris H/L. **192 encodages par préfixe**, hors BIT. SLL s'appuie sur Young et le relevé physique CB36 où 06 devient 0D ; la qualification exhaustive NEC des comportements non documentés reste ouverte.

EX (SP),HL/IX/IY lit bas puis haut, écrit haut puis bas, conserve SP et F et maintient les dernières données pendant les deux états internes finaux. `Z80BusSequencer` conserve ce maintien dans son propre état, puis libère les données au front de fin, sans transaction ajoutée. Les propagations et la mémoire restent dans leurs propriétaires existants.

Timings contrôlés à 4 MHz sans WAIT : DD/FD registre 8 T ; octet immédiat 11 T ; paire immédiate 14 T ; absolu 16 bits 20 T ; INC/DEC paire 10 T ; ADD indexé 15 T ; PUSH 15 T, POP 14 T, LD SP 10 T, JP indexé 8 T ; mémoire indexée 19 T, INC/DEC indexé 23 T ; DDCB/FDCB réalisé 23 T ; EX (SP) 19 T et 23 T avec index. Ces valeurs sont des temps matériels du banc, pas des mesures de vitesse hôte.

Sept cas regroupés dans `IndexedExecutionTests.cs` couvrent les deux index, sélecteurs registre, vues octets, transferts, sources ADD, huit ALU, déplacements signés/rebouclés, exceptions, préfixes en chaîne, WAIT et reprise à chaque demi-période de neuf programmes (**340 dates**). Premier passage : **86 succès, un échec** dans le nouvel oracle ADD indexé, qui oubliait F3 provenant de l'octet haut ; correction de cette assertion, sans changement de production, puis **87 succès**, aucun échec ou ignoré. Preuves `20261111-execution-indexed.trx` et `20261111-execution-indexed-confirmation.trx` sous `artifacts/TestsChips/Z80/SG-1000/`.

Trois cas supplémentaires regroupent les 192 encodages DDCB et FDCB sur cinq motifs et deux retenues, flags, copies registre, adresses, cinq lectures dont les deux opérandes hors M1, une écriture et compte R ; EX (SP), les trois paires, pile FFFF→0000 et maintien des données ; reprise à **222 dates** de cinq programmes et WAIT pendant le dernier opcode indexé. Le groupe CB existant inclut désormais SLL, soit **168 encodages registre et 24 mémoire**.

Passage final : **90 succès Debug, 0 échec, 0 ignoré**, code 0, durée du résumé 1 s. Preuve `artifacts/TestsChips/Z80/SG-1000/20261111-execution-ddcb-stack-exchange.trx`, même filtre Z80 que les passages précédents. Aucune fidélité globale ni vitesse de puce n'est déduite ; BIT/MEMPTR, instructions restantes, contrôles matériels et variantes restent ouverts dans la fiche générique. Le seul avertissement affiché est l'antérieur `Shared/ArchitectureTests.cs:33`.

## Transferts I/R, E/S par C et RLD/RRD — 11 novembre 2026

Les quatre transferts I/R réutilisent les deux M1 puis un état interne, soit **9 T**. Les écritures conservent F ; les lectures règlent S/Z, F3/F5 depuis A et P/V depuis IFF2, conservent C et effacent H/N. Le cas de prise d'interruption qui efface P/V demeure explicitement inachevé, avec le séquenceur d'interruptions. R inclut les deux fetchs et conserve R7 pendant son incrément ; LD R,A remplace l'octet entier, puis le fetch suivant reprend cet incrément.

IN r,(C), y compris l'encodage 110 affectant seulement les indicateurs, et OUT (C),r documentés présentent **BC avant le transfert** sur l'adresse et emploient le cycle E/S avec son Tw automatique : **12 T**. IN règle S/Z/parité et F3/F5 depuis les données, conserve C, efface H/N ; OUT conserve les indicateurs. Aucune constante NMOS/CMOS n'est choisie pour OUT (C),0. Le banc sait désormais fournir une valeur d'entrée de port variable sans créer une autre mémoire ou un autre moteur CPU.

RLD/RRD passent par les lectures-modifications-écritures canoniques avec **quatre états internes**, soit **18 T**, une lecture et une écriture de données. La permutation des trois chiffres ne change pas l'octet haut d'A ; les indicateurs proviennent du nouvel A, C est conservé. DD/FD suivi d'ED ne redirige aucune opération vers IX/IY. Toutes les continuations utilisent les états déjà capturés ; aucun état parallèle ou agenda commun n'est ajouté.

Six nouveaux cas regroupés dans `ExtendedExecutionTests.cs` couvrent les quatre transferts I/R sous huit valeurs limites, deux états F et deux IFF2 ; le changement de I au rafraîchissement suivant ; les huit encodages IN avec les **256 valeurs** et les deux retenues ; les sept sorties documentées et l'adresse BC antérieure ; RLD/RRD sous neuf A, dix données mémoire et deux F ; la reprise à chaque demi-période de dix programmes (**268 dates**) et WAIT au transfert IN avec modification du niveau d'entrée avant capture. Les oracles réutilisent la référence mathématique indépendante déjà écrite ou permutent les chiffres par division et reste en base 16 ; ils n'appellent pas les calculs de production.

Commande : `dotnet test tests/RetroMediaLab.Chips.Tests/RetroMediaLab.Chips.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~RetroMediaLab.Chips.Tests.CPU.Zilog.Z80.Z80" --logger "trx;LogFileName=20261111-execution-ed-transfers-io-digits.trx" --results-directory artifacts/TestsChips/Z80/SG-1000`.

Résultat : **96 succès Debug, 0 échec, 0 ignoré**, code 0, durée du résumé 1 s. Preuve `artifacts/TestsChips/Z80/SG-1000/20261111-execution-ed-transfers-io-digits.trx`. Profil du banc : maxima NEC et horloge extérieure 4 MHz, aucun matériel externe ni logiciel invité chargé. Le seul avertissement affiché reste `Shared/ArchitectureTests.cs:33`. Ce passage ne mesure pas la vitesse hôte et ne termine ni l'intégration des interruptions ni la qualification matérielle des variantes.


## Décodeur entier et contrôle matériel — 11 novembre 2026

Conformément à la demande utilisateur, les familles restantes ont été écrites ensemble avant tout nouveau passage de tests. `CpuZ80.Flags.cs` et `CpuZ80.Memptr.cs` centralisent les indicateurs et leurs effets internes : DAA, SCF/CCF avec Q selon le silicium, BIT registre/(HL)/indexé, adresses WZ des transferts, sauts, E/S et accès indexés. `CpuZ80.Blocks.cs` réalise les 16 encodages LDI/LDD/LDIR/LDDR, CPI/CPD/CPIR/CPDR, INI/IND/INIR/INDR et OUTI/OUTD/OTIR/OTDR. Chaque itération répétée refait réellement les deux M1 et reste interruptible ; les cinq états de répétition et flags intermédiaires ne sont pas escamotés. ED comprend ses aliases NEG/RETN/RETI/IM et les encodages non assignés sans effet établi, toujours avec leurs deux M1. OUT (C),0 fournit zéro pour les deux profils NMOS réalisés. Aucun opcode base manquant n'est masqué par un NOP.

Sources supplémentaires archivées sous la racine Zilog : `BooBoo-MEMPTR-2006.txt` ([rapport de mesures](https://raw.githubusercontent.com/floooh/emu-info/master/z80/memptr_eng.txt)), `Banks-Undocumented-Flags.html` ([observations physiques et différences de silicium](https://github.com/hoglet67/Z80Decoder/wiki/Undocumented-Flags)), `MEMPTR-reference-20260107.html` ([synthèse reliant les expériences](https://github.com/redcode/Z80/wiki/MEMPTR)). La dernière est une synthèse, pas une mesure sur chaque variante. L'acquisition directe du fil Stardot sur les blocs NEC a rencontré sa protection web ; ne pas prétendre qu'une archive locale de ce fil existe. Les observations constructeur et relevés matériels déjà acquis restent les références des temps de bus. Les généralisations insuffisamment prouvées pour NEC restent ouvertes.

`CpuZ80.Control.cs` et `CpuZ80.Interrupts.cs` raccordent les entrées reçues et mémorisées. INT est échantillonné au montant du dernier T ; NMI utilise son verrou de front. NMI passe avant INT, BUSREQ suspend leur acceptation. IM0 exécute l'opcode acquis sans incrémenter PC pour cet octet ; IM1 pousse le retour puis vise 0038 ; IM2 pousse le retour et lit les deux octets de table avec le vecteur entier, y compris FF. NMI dure 11 T, IM1 13 T et IM2 19 T hors WAIT. L'effet P/V de LD A,I/R est appliqué lors de l'acceptation INT. EI protège l'instruction complète suivante, y compris ses préfixes ; RETI/RETN recopie IFF2 dans IFF1, tandis que la chaîne périphérique voit les octets M1 réels.

HALT conserve PC à l'instruction suivante et continue ses lectures M1 et refresh, sans compter de nouvelles instructions. RESET normal interrompt notamment WAIT, efface PC/I/R, IFF et IM, conserve les autres registres documentés comme non initialisés ; la récupération conserve ses états d'horloge. Le reset spécial M1T2 suit le rapport Brewer : instruction terminée, fetch supplémentaire ignoré et reprise à zéro ; pendant HALT, le fetch courant devient exécutable sans incrément PC. **La caractérisation physique du cas spécial sur le NEC identifié et des extensions de préfixes non mesurées reste ouverte.**

BUSREQ est pris aux frontières M, y compris les M1 prolongés, lectures-modifications-écritures, écritures maintenues, IO de bloc et les deux cycles internes 4+3 des additions 16 bits. BUSACK libère adresse/données/commandes trois états ; le séquenceur et sa continuation restent suspendus. Les entrées IRQ/NMI demeurent mémorisées. La reprise ne répète aucune écriture. `Z80ControlState` et l'état CPU **version 5** conservent niveaux, échantillonnages, reset, HALT, concession et reprise différée ; les caches de propagation sont reconstruits depuis leurs délais canoniques.

Validation regroupée : le premier appel a trouvé trois erreurs de compilation dans le nouveau banc (nom `MachineCycleOneHigh` et visibilité de sa méthode de front), corrigées ensemble sans modification matérielle. Ensuite **106 succès Debug**, puis **110 succès** après couverture complémentaire des fronts INT, du reset spécial et de la reprise des contrôles. Même résultat après les deux étapes de refonte des échéances. Les passages à 110 cas incluent 3 584 essais de décodage (7 × 256 × 2), 20 000 paires BCD valides, les variantes BIT et 594 dates supplémentaires de reprise blocs/HALT/DMA/NMI/reset ; ils n'établissent pas la totalité des phénomènes analogiques ou non documentés pour toutes les révisions. Les contrôles de cycles, adresses, données et signaux des précédents groupes restent exécutés ensemble.

Preuves sous `artifacts/TestsChips/Z80/SG-1000/` : `20261111-full-cpu-compiled.trx` (106), `20261111-full-cpu-control.trx`, `20261111-full-cpu-event-cache.trx`, `20261111-full-cpu-active-events.trx` (110 chacun). Aucun ignoré ni échec d'exécution ; l'avertissement antérieur `Shared/ArchitectureTests.cs:33` reste hors modification. La compilation initialement refusée n'a pas produit de preuve TRX. Les mesures isolées et leurs limites sont dans la fiche principale. Aucun firmware, média local, fenêtre ou appareil externe n'est utilisé.


## Transport de l'état et décision reset NEC

Les champs du nouveau `Z80ControlState` n'auraient pas été conservés par le sérialiseur JSON standard. Ils sont maintenant exposés comme propriétés à stockage unique, comme les registres déjà présents. Un contrôle autonome supplémentaire sérialise en mémoire puis restaure WAIT, DMA, reset et NMI et compare la continuation au CPU original. Aucun fichier temporaire ni image externe. Passage Debug `20261111-full-cpu-state-transport.trx` : **111 succès, aucun échec ou ignoré**.

Réponse utilisateur du 11 novembre 2026 : **« Conserver le modèle avec réserve »**, portée explicitement rectifiée ensuite par l'utilisateur : **Zilog seulement, pas NEC**. Elle n'autorise aucun report du reset spécial NEC et ne transforme pas les mesures Zilog/Mostek/SGS en caractérisation physique NEC. Question complète et rectification conservées dans la fiche principale.


## Moteur unique et profils — 11 novembre 2026

`Z80HardwareProfile` configure les seuls comportements spécifiques, `Z80BusTiming` conserve les délais. Le dossier NEC possède `Upd780C1Profile`, sans CPU, interface, décodeur ou séquenceur supplémentaire. L'état version 6 conserve le profil immuable et rejette tout remplacement incompatible. Le Zilog est le modèle d'origine du constructeur à quatre paramètres ; le second constructeur reçoit explicitement le profil. Les particularités NEC non encore caractérisées par l'utilisateur restent dans sa propre fiche et ne bloquent pas le Zilog.

La fiche Zilog 03-0027-02 (mars 1978) a permis d'ajouter le profil Z80 2,5 MHz et de qualifier le Z80A 4 MHz indépendamment de NEC. Pour le 2,5 MHz, RD et IORQ ne libèrent pas avec le même délai depuis les deux fronts : le bus et son état distinguent maintenant ces cas. Deux contrôles regroupés couvrent les temps du programme complet dans les deux grades et la séparation des profils au sein de la même classe/du même contrat. Résultats complets : **113 succès Debug et 113 Release**, zéro échec/ignoré, preuves `20261111-zilog-and-nec-profiles.trx` et `20261111-zilog-and-nec-profiles-release.trx`. Aucun temps de test n'est transformé en débit CPU.


## Réception d'un train fini de fronts — 11 novembre 2026

`ICpuZ80.ReceiveClockEdges(firstEdgeNanoseconds, halfPeriodNanoseconds, edgeCount)` reçoit des fronts alternés de la source réellement extérieure. Le CPU ne possède aucun oscillateur supplémentaire et aucun état de générateur. Le propriétaire borne chaque appel avant une nouvelle entrée extérieure non livrée par les callbacks du bus. Les fronts ordinaires et les trains utilisent le même `ApplyClockEdge` et le même séquenceur bus ; le train qualifie ses bornes et intervalles avant toute mutation.

Le seul regroupement de plusieurs clocks concerne un préfixe d'états internes sans transaction active ni propagation pendante, sans reset, concession/reprise ou sortie HALT à livrer. Il s'arrête avant le front d'échantillonnage du dernier T et avant les frontières M, y compris le découpage 4+3 de l'arithmétique 16 bits. Comptes, niveaux de CLK, dernier front et dernier montant sont représentés exactement depuis le train reçu. Les propagations entre fronts sont toujours résolues et publiées à leur date ; elles ne sont pas reportées en fin d'appel. Aucun état supplémentaire de snapshot n'est nécessaire (version 6 inchangée).

Passage fonctionnel `20261111-clock-trains-equivalence.trx` : **115 tests réussis, aucun échec, ignoré ou abandonné dans le TRX**. Le nouveau banc compare **1 215 scénarios** : neuf programmes (NOP, calcul 16 bits, saut relatif, RLD, CPIR, INIR, accès indexé, échange de pile, HALT), trois profils Zilog/Z80A/NEC, neuf préfixes de 1 à 257 fronts et cinq scénarios d'entrées. Il compare le snapshot entier, chaque broche datée, les notifications d'échéance, RAM, lectures/écritures mémoire et ports. Les entrées NMI/BUSREQ/RESET/WAIT sont également modifiées depuis les callbacks, aux mêmes instants dans les deux parcours. Durées trop courtes, débordements et réentrance sont refusés avant mutation. Le code de sortie de cet appel n'a pas été capturé par le pilote ; ne pas lui attribuer artificiellement un code, le TRX terminal est la preuve des 115 résultats.

Premier diagnostic isolé `20261111-clock-trains-performance.trx`, sept paires alternées de 800 000 fronts à 4 MHz, empreinte complète des dix sorties datées incluse : réduction appariée médiane **2,26 / 4,76 / 2,18 / 11,97 %** pour NOP/JP, mixte mémoire/E/S, DDCB/JP et ADD HL/JR. Les médianes séparées du premier trajet ne montrent pas de gain : aucune amélioration générale n'est annoncée.

Second diagnostic `20261111-clock-trains-performance-cpu.trx` : les signaux sont d'abord comparés avec empreinte sur 10 000 fronts, puis la mesure exclut le coût logiciel de cette empreinte et conserve CPU, RAM et E/S par les broches. Les snapshots et la RAM sont comparés en dehors du chronométrage pour chaque paire. Sept paires alternées, 800 000 fronts, Debug, Zilog Z80A 4 MHz, délais maximaux :

| Programme | Scalaire médian (ms) | Train médian (ms) | Réduction médiane des paires | Débit train (MHz émulés) |
| --- | ---: | ---: | ---: | ---: |
| NOP/JP | 330,714 | 302,858 | 5,32 % | 1,321 |
| Mémoire/E/S | 315,125 | 337,190 | 1,75 % | 1,186 |
| DDCB/JP | 295,159 | 274,253 | 8,66 % | 1,459 |
| ADD HL/JR | 242,690 | 212,260 | 12,01 % | 1,884 |

Le pourcentage est la médiane des réductions **par paire**, et non le rapport des deux médianes indépendantes. Leur désaccord sur le trajet mixte signale une dispersion importante ; aucun gain garanti n'est retenu pour ce trajet. Le gain d'environ **12 %** est retrouvé sur le trajet à états internes et justifie ce regroupement qualifié. Les deux diagnostics isolés réussissent, code 0, avec états et données identiques. Cela ne prouve aucun gain SG-1000 et ne clôture pas l'optimisation Debug du CPU.


Rectification utilisateur : les décisions « conserver le modèle avec réserve » rapportées plus haut concernaient le **Zilog**. Leur attribution au NEC était une erreur. Le NEC reste à caractériser et terminer selon son matériel ; ses marqueurs d'inconnu ne valent ni conformité ni report accepté.

## État natif et candidat retiré — 11–12 novembre 2026

Les sorties et cibles sont désormais des mots natifs avec masques de pilotage ; les tableaux ne sont construits qu'à la capture d'état. `Z80BusPins` conserve constructeur, propriétés init, déconstruction et JSON avec un stockage canonique 64 bits. Registres et contrôles internes : champs uniques avec `[JsonInclude]`, sans changer le format d'état. La chaîne d'échéances n'est raccordée que lorsqu'un destinataire existe. Le groupe à 117 tests vérifie notamment 11 664 combinaisons de broches et les abonnements modifiés dans les callbacks. L'ancien échec d'un nouvel oracle d'échéances a été corrigé dans le test : absence d'échéance à 110 ns, programmation de 210 ns au descendant 125 ns. Aucun délai de production n'a été déplacé pour satisfaire cet oracle.

Mesures Debug isolées, sept paires alternées, 800 000 fronts d'une source 4 MHz, RAM/E/S par broches, empreinte hors chronomètre. Parcours train, en MHz émulés ; passages successifs, pas une comparaison appariée entre versions :

| Preuve TRX dans `artifacts/TestsChips/Z80/SG-1000/` | NOP/JP | Mémoire/E/S | DDCB/JP | ADD HL/JR |
| --- | ---: | ---: | ---: | ---: |
| `20261111-native-pins-deadlines-performance.trx` | 1,901 | 1,972 | 2,076 | 2,880 |
| `20261111-packed-pins-performance.trx` | 2,363 | 2,585 | 2,742 | 3,546 |
| `20261111-native-state-masked-events-performance.trx` | 2,677 | 2,900 | 2,966 | 4,281 |

Dernières durées train : 149,408 / 137,931 / 134,855 / 93,440 ms pour 400 000 montants. Preuve fonctionnelle `20261111-native-state-masked-events.trx` : 117 succès Debug. Aucun gain machine déduit. **La cible 16 MHz minimum, objectif 32 MHz Debug, n'est pas atteinte.**

Profilage actuel du testhost réel : `20261112-current.nettrace` et `.speedscope.json`, cinq secondes avec `dotnet-sampled-thread-time`, 3 735,595 ms sous Run sur le thread 60772. Coûts propres après attribution des pseudo-feuilles CPU_TIME/UNMANAGED_CODE_TIME au dernier cadre réel : AdvanceTo 20,41 %, Schedule 14,61 %, PublishDeadline bus 7,14 %, ApplyClockEdge 7,07 %, AdvanceClockEdge 6,91 %, AdvanceMemory 6,64 %, callback de banc 5,07 %. Échantillons de temps de pile, pas des coûts inclusifs à additionner ni des débits. Processus possédés fermés, attendus et libérés en finally. `20261112-current-profile.trx` réussit, code 0 ; ses durées sous profiler sont exclues des mesures autonomes.

Candidat essayé puis **retiré** : qualification facultative mémoire/E/S et résolution regroupée des transitions masquées par ses strobes, même moteur. `20261112-qualified-bus-functional.trx` : 122 succès Debug, dont 324 scénarios × vingt frontières comparant snapshots datés sélectionnés par un oracle typé indépendant, WAIT/DMA/RESET/NMI/INT, échéances, données et états.

Mesure isolée `20261112-qualified-bus-performance.trx` : deux tests réussis, code 0, sept paires alternées, mêmes conditions et états comparés hors mesure :

| Programme | Complet médian (ms) | Qualifié médian (ms) | Réduction médiane des paires | Débit qualifié (MHz) |
| --- | ---: | ---: | ---: | ---: |
| NOP/JP | 166,589 | 158,193 | 3,12 % | 2,529 |
| Mémoire/E/S | 166,709 | 162,287 | 0,31 % | 2,465 |
| DDCB/JP | 161,053 | 157,426 | 2,07 % | 2,541 |
| ADD HL/JR | 92,618 | 87,767 | 4,36 % | 4,558 |

Gains faibles et dispersés, avec aussi des paires défavorables : enum, propriété, projection, résolveur qualifié et tests dépendant du candidat supprimés. Résolveur natif complet rétabli. `20261112-qualified-bus-withdrawn.trx` : **120 succès Debug, zéro échec/ignoré, code 0** ; les trois cas supplémentaires proviennent du reset normal Zilog/NEC depuis WAIT/HALT. Aucun code du candidat conservé, aucune directive d'optimisation nouvelle déduite. Le travail restant porte toujours sur l'architecture du moteur commun et la qualification NEC, sans passage prématuré au VDP.

## Étapes de bus préparées — 12 novembre 2026

`Z80BusWaveforms` prépare au montage les masques, cibles et délais fixes du profil dans des `Z80BusStep`. Le séquenceur unique consomme ces données au vrai front reçu, capture les entrées, programme ensemble ses slots et conserve leur résolution datée. Les trois anciennes fonctions AdvanceMemory/AdvanceIo/AdvanceInterruptAcknowledge sont remplacées ; aucun deuxième décodeur/séquenceur, aucune suppression d'horloge ou de sortie. Le cache d'étapes est reconstruit à la restauration depuis le cycle existant, sans modifier le format d'état.

Une empreinte figée avant refonte couvre 84 scénarios de bus (six cycles, trois timings, quatre positions d'attente, écriture maintenue ou non), états complets à quarante fronts, broches et échéances datées, HALT/reset/concession/libération. Elle prouve l'équivalence au code antérieur, pas le silicium ; les oracles constructeur du groupe restent indépendants. `20261112-waveform-baseline.trx` a servi à enregistrer les empreintes ; sa mesure concurrente est exclue. La référence de débit a été relancée seule dans `20261112-waveform-before-isolated.trx`.

Après refonte : `20261112-waveform-functional.trx`, **121 succès Debug**, zéro échec/ignoré, code 0, trois empreintes identiques. `20261112-waveform-after-isolated.trx` : un test de débit réussi, code 0. Mesures isolées successives, sept paires scalaire/train par version, 800 000 fronts/400 000 clocks avec RAM/E/S, empreinte hors mesure :

| Programme | Train avant (ms) | Train après (ms) | Après (MHz émulés) |
| --- | ---: | ---: | ---: |
| NOP/JP | 152,547 | 145,341 | 2,752 |
| Mémoire/E/S | 160,389 | 133,769 | 2,990 |
| DDCB/JP | 133,686 | 128,386 | 3,116 |
| ADD HL/JR | 88,001 | 87,083 | 4,593 |

Comparaisons de versions successives, pas de paires avant/après dans le même processus : ne pas en déduire un pourcentage de gain général ou machine. Les médianes scalaire après sont 156,123 / 148,764 / 138,090 / 104,950 ms. La refonte est conservée ; elle ne clôture pas la cible de vitesse. À la demande explicite de l'utilisateur, les prochains ciblages de performance attendent la SG-1000 complète ; priorité aux composants manquants et à leur câblage, sans masquer les inconnues NEC.

### Échanges de séquences de bus aux dépendances — 12 novembre 2026

`AdvanceBusRangeUntil` utilise la source rationnelle reçue et le séquenceur canonique. Les sorties résolues sont transportées dans des `Z80BusRange` empruntées et bornées ; chacune conserve les mots de broches et leurs dates électriques. La livraison a lieu dans les opérations canoniques qui consomment DATA/WAIT et dans SampleControlInputs, y compris les extensions internes et les reprises de WAIT. Cela évite de dupliquer la connaissance des phases dans un deuxième décodeur. Les opérations restent exécutées à leurs fronts actuels ; le calcul de préfixes de phases sans effet n'est pas encore refondu. Cette entrée prépare le remplacement du transport du circuit, elle ne clôture pas à elle seule la refonte globale d'exécution.

Condition explicite : le propriétaire doit garantir RESET/NMI/BUSRQ stables avant la borne demandée, ou terminer la plage avant leur changement et appliquer celui-ci à sa date. Le receveur résout les réponses DATA/WAIT/INT avant leur consommation ; il ne renvoie pas un octet par un fournisseur mémoire. Une source qui ne peut pas garantir ces conditions doit utiliser les frontières explicites. Aucun retour vers une date CPU antérieure, aucune fréquence ou particularité de profil changée. Les callbacks scalaires doivent être détachés ; raccorder les composants à ces séquences reste à faire dans le module. L'ancien parcours conserve ses callbacks. Le transport temporaire n'est pas une FIFO matérielle et est détaché dans finally, même en cas d'échec du receveur.

Vérifications : 146 contrôles Debug CPU Zilog/NEC et logique réussis dans `20261112-range-cpu-logic-full-debug.trx` ; un contrôle de refus/nettoyage a ensuite été ajouté, les huit contrôles ciblés de source et plages réussissent dans `20261112-bus-range-guards-debug.trx`. Les 26 contrôles ciblés CPU/plages/logique réussissent aussi en Release, `20261112-range-foundation-release.trx`. Comparaisons de tous les mots de broches datés, lectures, écritures mémoire/E/S, RAM du banc et états CPU complets à chaque borne, quatre programmes, deux profils et deux phases de source ; contrôles WAIT/INT changeant au sein d'une plage, RESET/NMI/BUSRQ aux frontières. Les programmes sont synthétiques, en mémoire ; aucun jeu local ni composant mémoire de la machine n'est substitué dans le module. Aucune mesure de débit et aucun gain machine attribués à ces fondations non raccordées.

### Moteur raccordé et fenêtres reçues — passage global du 12 novembre 2026

AdvanceBusRangeUntil réutilise les mêmes opérations du CPU et les portions internes déjà qualifiées, bornées aux phases de contrôle et aux horloges reçues. ReceiveBusInputWindow décrit la stabilité extérieure de WAIT/contrôle ; les cycles dépendants invalident cette garantie jusqu'aux dernières propagations réellement pendantes, et les entrées DATA restent une frontière obligatoire. Les échantillonnages ne sont pas supprimés. Les fenêtres/statistiques sont du transport, invalidé à la restauration, sans second profil ni état de silicium ; les particularités NEC restent dans son profil. La validation globale du montage comprend les trois états finaux/36 observations ROM de référence et le programme synthétique RAM/VDP/PSG/manettes/NMI/reprise, trois scénarios réussis en Debug. Les tests CPU indépendants supprimés ne sont pas annoncés comme rejoués, aucun MHz isolé n'est déduit de la machine. Détails et limites dans `docs/project/emulations/Sega/segaemu-performance-architecture.md`, section moteur unique du 12 novembre.
