# AcornEmu — Acorn System

## Références déjà identifiées

- Manuel Acorn System 5 : https://chrisacorns.computinghistory.org.uk/docs/Acorn/Manuals/Acorn_System5Handbook.pdf
- Cartes et cartes mémoire 6502/6502A/6809 : https://github.com/mamedev/mame/blob/master/src/mame/acorn/acrnsys.cpp
- INS8154, référence à confronter au manuel (modes strobed non implémentés dans cette source, décodage set-bit à vérifier) : https://github.com/mamedev/mame/blob/master/src/devices/machine/ins8154.cpp

## Blocs suivants conservés

- [ ] Atom puis Electron : créer les suivis `docs/tasks/machines/AcornAtom/implementation.md` et `docs/tasks/machines/AcornElectron/implementation.md` avec leurs fichiers concrets avant développement.
- [ ] Archimedes : créer `docs/tasks/machines/AcornArchimedes/implementation.md`, comparer les cartes et options réelles, notamment A440/A440/1 et A5000/Alpha, avant de définir les modèles.
- [ ] Risc PC et A7000 : créer `docs/tasks/acornemu-riscpc-a7000.md`, comparer A7000/A7000+ avant de définir des modèles séparés.

ABC, Communicator, stations Unix et Pocket Book exclus de la demande actuelle. Le document Google lié exige une authentification actuellement indisponible ; la liste explicite de l’objectif reste conservée ici et sera confrontée au document dès que son accès sera possible.

INS8154 : l'ancien constat limité aux ports statiques est périmé. Le composant partagé `Chips/IO/NationalSemiconductor/INS815x/8154` couvre également les modes strobed, les handshakes, les masques trois états et INTR ; voir [la fiche complète et ses preuves](../../../chips/IO/Ins8154.md). Les cartes System utilisent les modes programmés par leur logiciel et le câblage réel, sans raccorder arbitrairement INTR aux CPU. Les reprises du 8 novembre sont suivies par modèle dans `system1-validation.md` à `system5-validation.md`.

VDU : mémoires à 0400 (40 colonnes) ou 1000/F000 (80 colonnes), CRTC à 0800 ou 1840/E840 avec miroirs de la carte 80 colonnes. Tampon 480 × 250, caractères 12/6 pixels, curseur fourni par le CRTC partagé. Sources exactes : `src/devices/bus/acorn/system/vdu40.cpp` et `vdu80.cpp` de MAME ; port 8154 contrôlé contre la fiche National Semiconductor : https://www.theoddys.com/acorn/semiconductor_datasheets/INS8154%20National%20Semiconductor.pdf . Raccordement terminé ; entrelacement vérifié par ROM synthétique puis captures des ROM réelles.

6809 : instructions officielles, adressage indexé, deux piles et interruptions intégrés. Moniteur réel démarré, contrôles autonomes pile, adressage et FIRQ réussis. Cela ne constitue pas une certification cycle par cycle.

Assemblage terminé : CPU, bus, RAM, INS8154/VIA, VDU, clavier, tape et 8271 raccordés aux profils. Aucun PSG BBC ajouté aux System. Les cartes réseau et les périphériques non émulés ne sont pas annoncés comme disponibles.

Firmware installés sans écrasement : moniteur System 1 B987354C (512 octets), COS C587DA5D (2048), DOS System 3 4CB322DA (4096), moniteur 6809 B14A884B (2048), système 5F/NOS80 E5882982 (8192) et caractères VDU80 04C326A7 (2048). Empreintes complètes confrontées aux références MAME. Variantes DOS40A 3BECC10F, DOS40B 8D492106 et DOS5/80 3F787AB8 classées compatibles distinctement des ROM officielles. Les variantes DOS80A du System 3 ne sont pas annoncées compatibles avec la carte CPU System 5 à VIA.

## Build Debug demandé

Build demandé réussi (code 0). Exécutable : build/Debug/Retro Media Lab/retromedialab.exe. Les 24 modules et leurs DLL sont présents ; SHA-256 Acorn identique au fichier compilé. Avertissements CA1416 sans erreur bloquante. Validation fonctionnelle System encore distincte du build.

## Livraison du bloc System

Validation finale du code : 391 contrôles Acorn autonomes réussis après suppression du diagnostic. Auparavant, neuf scénarios avec ROM externes ont réussi : six démarrages (System 3 avec chacun des deux CPU) et trois CAT via clavier et 8271, avec lecture du catalogue synthétique RML TEST. Captures réelles inspectées dans artifacts/acorn-system ; aucun dossier rml-system-boot restant. Correctifs : VDU entrelacé, polarité ASCII et repos clavier, persistance LED, absence de configuration HDD quand aucun HDD raccordé. Build Debug All terminé avec code 0 : 24 modules et exécutable présents, empreinte Acorn identique. Chemin : build/Debug/Retro Media Lab/retromedialab.exe.

## Revue statique des horloges et liaisons — 8 novembre 2026

Les chiffres de validation ci-dessus sont historiques et n'incluent pas les corrections de cette reprise. La demande actuelle porte sur le code et les fiches existantes, sans nouveaux tests/builds. Strobe PB7 du clavier Standard6502 raccordé à STB du 8154 ; HSYNC/RA du CRTC raccordés par notifications au SAA5050 ; complétion des broches et phases MC6809 en cours de validation différée. Aucune nouvelle clôture fonctionnelle ou performance déduite de ces seules lectures.

Le clavier ASCII ne reçoit plus d'appel par période quand aucune échéance ne tombe. Il conserve son temps absolu et publie chaque front de strobe dû ; la carte retient seulement la date notifiée. REPT raccourcit le délai au prochain tick comme auparavant. La matrice du System 1 ne prépare plus d'état ASCII/repeat, et le codeur ASCII ne reconstruit plus cette matrice absente. Ces changements sont écrits et relus, sans nouvelle compilation ni exécution de test pendant cette revue ; aucun gain chiffré ou nouveau verdict de machine complète annoncé.

## Continuité de la revue — 9 novembre 2026

- [ ] Revoir System 1 puis les cartes System suivantes, sans réécrire les composants déjà vérifiés sans défaut établi.
  - [ ] Modifier ce suivi : consigner la revue des composants et raccordements System 2, puis System 3 avec chacun de ses CPU, System 4 et System 5 ; ajouter avant réalisation les corrections établies avec leurs fichiers.

System 1 : composants et bus relus (`Cpu6502.Clock.cs`, `.Pins.cs`, `.Interrupts.cs`, `RamIo8154.cs`, `.Ports.cs`, `SystemTapeInterface.cs`, `AcornSystemMachine.cs`, `.Bus.cs`, `SystemHexDisplay.cs`). Le CPU possède cycles, phases et entrées ; SystemTape possède temps, bascules, compteurs et échéance ; l’INS8154 est statique et commandé par les strobes, sans horloge interne fictive. La carte conserve les raccordements et les prochaines échéances notifiées, sans refaire les calculs internes. L’afficheur intègre les durées des sorties sans cadencer le matériel. Correction : fréquence déjà prévue par le profil transmise au constructeur 6502 (valeur déclarée auparavant nulle), et contrat technique SystemTape renommé en Tape. Aucun changement de fréquence réelle, de cycle, de bypass ni d’ordonnanceur commun. Résultats : 10 tests SystemTape Debug, 87 ms ; 72 tests System/RAM-E/S, 1 s ; zéro échec/ignoré. Fiches Mos6502, Ins8154, SystemTape et validation System 1 actualisées. Les performances historiques System 1 restent celles de leur passage, aucune nouvelle mesure globale revendiquée.

### Suite ordonnée de la revue et des mesures

- [ ] Terminer la revue System 2, puis System 3 avec ses deux CPU, System 4 et System 5.
  - [ ] Modifier `docs/project/emulations/Acorn/Acorn System/System 5/validation.md` : consigner ensuite la revue de la carte rapide et du générateur physique, après réalisation des corrections de propriété temporelle définies avant modification.

Correction télétexte du 9 novembre : le niveau TR6 dupliqué dans SystemVideo est supprimé. Le SAA5050 conserve sa phase et restitue les douze RGB successifs par ClockHalfDots ; les coordonnées de présentation restent dans le module. Groupe existant AcornEmuSystemTests/AcornEmuSystemRamIoTests : **75 réussites Debug, zéro échec ni ignoré, environ 1 s**. Ce temps est celui des tests fonctionnels, pas une mesure de débit. Aucun nouveau chronométrage de machine ou gain chiffré revendiqué.

Avancement VDU du 9 novembre : suppression de `_videoPhase` et de sa boucle dans `AcornSystemMachine.Tick`. Pour les profils configurés, `VideoClock` est toujours égal à `_clock` : PH2/1 MHz sur VDU40, 2 MHz sur VDU80/carte rapide. Un appel direct conserve donc exactement un caractère par période de bus, sans accumulateur ni division artificiels. Les compteurs CRTC et la phase TR6 restent dans leurs composants. Les **75 tests System existants réussissent en Debug, zéro échec ni ignoré, durée 1 s** après cette modification. Aucun gain de débit n'est déduit de cette durée. Compilation des dépendances de tests seulement, sans livraison dans build/Debug ni ouverture de RML.

Débit System 2 actuel : trois fenêtres de 160 champs, 10,184747 / 10,293092 / 10,201109 ms par champ, soit 194,305–196,372 % du profil 1 MHz. Rendu et drainage audio inclus, COS40 au moniteur ; tableau complet et limites dans la fiche System 2. Ce passage ne clôture pas la revue des autres composants ni celle des autres machines.

Nettoyage du diagnostic terminé : source supprimée, projet de tests régénéré sans dépendances, code 0 (quatre avertissements existants, aucune erreur). Aucune livraison de l’application effectuée.

Circuit de répétition déplacé dans Chips, REPEAT sans AKD raccordé : 5 tests propres et 76 tests System réussis. Fiche `docs/chips/Controllers/SystemKeyboardRepeat.md`, valeurs nominales RC et portée explicites. Revue globale System 2 et mesures isolées des autres puces restent ouvertes.

Débits séparés obtenus et consignés dans les cinq fiches : 6502 18,2–18,6 Mcycles/s ; HD6845S 20,3–21,4 Mcaractères/s ; SAA5050 5,81–5,83 Mcaractères/s ; INS8154 69,0–72,4 Mtransactions/s ; SystemTape 2647–2774 Mcycles PH2 de référence/s. Trois fenêtres par composant, scénarios isolés, pas de gain avant/après revendiqué.

Diagnostic de débit supprimé ; projet Chips.Tests régénéré sans dépendances, code 0, un avertissement xUnit préexistant. Revue du câblage System 2 consignée : aucune nouvelle réécriture des puces déjà validées ; corrections temporelles ciblées et vérifiées. Suite de la revue : System 3 en 6502, puis 6809, puis System 4 ; générateur System 5 toujours non raccordé.

System 3/6502 actuel : chargement BASIC par 8271 vérifié, empreinte exacte, PRINT 12+34=46, trois fenêtres Debug 182,947–189,285 % au prompt. Deux attentes/assertions du diagnostic corrigées après observation, aucun défaut de production établi. Résultats détaillés dans la fiche machine.

Source temporaire System3NativeProbe supprimée ; projet régénéré sans dépendances, code 0, quatre avertissements préexistants et aucune erreur. Le bloc de revue 6502 est consigné, sans annoncer la clôture de la carte 6809.

Mesure actuelle System 3/6809, après retrait de l’instance 8154 absente : 14,572009 / 14,172370 / 13,689057 ms par champ, 137,249–146,102 % à 1 MHz E. Programme invité 2010=5A, ratio EXTAL/E=4 vérifié. Bootstrap actuel encore à vérifier séparément.

Nettoyage du diagnostic 6809 terminé, assembly régénérée sans dépendances : code 0, aucune erreur, quatre avertissements préexistants. Aucun build de livraison de RML.

Bootstrap actuel réussi et capture inspectée : deux secteurs FM, 512 octets, programme 6809 exécuté, 2010=5A et BOOT OK. Premier diagnostic corrigé pour transmettre D majuscule au moniteur ; aucun changement de production nécessaire.

Diagnostic de bootstrap supprimé et assembly régénérée sans dépendances, code 0 (quatre avertissements préexistants, aucune erreur). Les deux preuves BMP finales restent conservées hors Git. Revue System 3 des deux cartes effectuée ; suite dans l’ordre : System 4, puis générateur physique System 5.

System 4 actuel : chargement exact BASIC, calcul 46, CAT1 après déplacement à chaud du média, 512 octets lus sur le second port. Trois fenêtres Debug 10,562988 / 10,471156 / 11,107172 ms par champ (180,064–191,001 %). Capture native actuelle inspectée. Aucun nouveau changement de production nécessaire.

Diagnostic System 4 supprimé, assembly régénérée sans dépendances : code 0, aucune erreur, quatre avertissements préexistants. Suite : réalisation du générateur physique System 5, avant ses nouveaux essais de carte.

Générateur 24 MHz réalisé dans Chips, avec trois 74S163 et ses sept sorties matérielles ; 21 contrôles Debug passent (12 compteur, neuf générateur), 58 ms. Le raccordement System 5 et la suppression du calcul de phase VIA restent à réaliser après WAIT/READY ; aucun résultat de cadence machine actuel n’est encore annoncé pour ce réseau.

Câblage courant : 32 contrôles Clock/RAM/E/S System passent en Debug (678 ms). Première compilation du test corrigée pour lever la collision de nom Intel8271, sans changement du matériel. Source libre, WAIT/READY, PH0 CPU, phases VIA, vidéo/disque et adaptation du bus sont raccordés ; la validation avec les vraies images reste le bloc suivant.

Validation native du montage courant effectuée : CAT, BASIC exact, calcul 46, SAVE et deux LOAD exacts dont réouverture réelle de fichier. 177 contrôles permanents passent. Diagnostic externe supprimé, assembly régénérée sans dépendances : zéro erreur, quatre avertissements préexistants. Copie temporaire et compagnons absents. Dernier débit Debug 23,213584–23,585582 % ; la machine n'est pas clôturée, l'examen des coûts logiciels source reste ouvert avant la décision de performance et la livraison.

Mesures effectuées sur le code de production 2480c530d : six configurations au moniteur, trois fenêtres de 80 présentations après chauffe. Débits Debug actuels consignés dans chaque validation : System 1 979,7–1 052,5 %, System 2 187,4–200,9 %, System 3/6502 183,1–184,1 %, System 3/6809 186,8–195,0 %, System 4 175,3–182,9 %, System 5 22,2–22,8 %. Générateur 24 MHz isolé : 7,878–8,278 millions de périodes/s, coût limitant établi avant ses récepteurs. Pas de mesure Release dans ce passage, aucun gain comparatif déduit des anciens scénarios BASIC. Diagnostic temporaire supprimé ; assembly régénérée sans lui, zéro erreur, quatre avertissements préexistants. Première invocation dotnet test corrigée (no-dependencies appartient à dotnet build), import Contracts du banc corrigé avant son exécution. Premier passage réussi mais journal normal sans chiffres ; second passage avec journal détaillé utilisé pour les valeurs consignées. Aucune modification de production dans ce bloc ; optimisation System 5 toujours ouverte.

Opérations de fronts factorisées et vérifiées, aucune modification du fonctionnement matériel. System 5 au même moniteur après modifications : 76,259704 / 75,102638 / 76,012375 ms par présentation, 26,184209–26,587615 % réels en Debug, environ 14,6 % de durée hôte en moins. BASIC exact, calcul 46 et même capture native, 177 contrôles de carte/disque/audio réussis. Fiches compteur, générateur, CPU et System 5 actualisées ; diagnostics supprimés et assemblies régénérées sans eux. La revue restante des coûts du réseau et de ses destinataires demeure ouverte avant décision de performance et livraison ; la clôture historique du bloc initial ne couvre pas cette reprise.

Les deux circuits manquants de capture/sérialisation VDU80 sont réalisés : SN74LS374 (10 contrôles), SN74LS165 historique (12 contrôles) ; ensemble logique/générateur/WAIT-READY 67 réussites. Chacun dépasse sa cadence nécessaire dans les scénarios isolés documentés. Sources TI archivées, variante 165 sans suffixe vérifiée dans le livre historique, aucune substitution d'émulateur prise comme preuve. Les diagnostics sont retirés et l'assembly régénérée ; preuves de raccordement à produire au bloc de carte suivant. Les performances System 5 précédentes ne sont pas déclarées comme celles d'une carte utilisant déjà ces deux nouveaux composants.

Phases de la carte 400,019 consignées dans System 5/vdu80.md après inspection des figures 10/14/15 et du schéma PDF 93. Prérequis supplémentaire corrigé dans le CRTC : MA physique, DE et CU disposent désormais de notifications au changement, sans sondage de VDU à ajouter au rythme des dots ; 88 contrôles CRTC réussissent, et la trace de référence ne change pas. Mesures isolées documentées dans sa fiche, diagnostic supprimé. Le raccordement de la capture et de la sérialisation reste l'action suivante ; aucun nouveau débit System 5 ni signal vidéo corrigé annoncé dans ce passage.

Prérequis ROM du trajet VDU80 : composant `src/RetroMediaLab.Chips/Memory/NationalSemiconductor/MM52116/Services/Mm52116.cs` et contrat/constantes réalisés suivant le suivi Mm52116, avec délais explicites et sans valeur par défaut de carte. Neuf contrôles dédiés et groupe final de 164 contrôles réussis ; banc autonome supprimé. Les références acquises donnent des maxima, pas la mesure d'un exemplaire : question sur le profil temporel en attente. Poursuivre le raccordement matériel après cette décision, sans inventer le délai réel à partir d'une borne constructeur.

Pendant l'attente du choix de délais VDU80, correction indépendante du raccordement de données : SN74LS165 reçoit des bits parallèles partiellement inconnus sans en faire des zéros connus. Broches A/B/H constantes préservées, C–G résolues à la réponse MM52116. Fixture autonome de raccordement vérifiée, 28 contrôles ROM/registre réussis ; aucune durée imposée à la carte réelle, aucune nouvelle cadence machine revendiquée. Les détails et fichiers modifiés sont dans le suivi Sn74LS165. Le choix entre plafonds documentés et mesures électriques reste demandé avant le câblage temporel de la carte.

### VDU80, reprise après clarification utilisateur — 9 novembre 2026

La question des délais nominaux non publiés ne bloque plus le trajet : profil numérique explicite NL 20 ns typiques, ROM 450/120/100 ns maximums, sans les attribuer à une mesure de carte. `RetroMediaLab.Chips/Controllers/Acorn/Vdu80CharacterGenerator/` assemble les trois composants vérifiés, conserve les échéances locales et reçoit ses clocks par notifications. Sept contrôles propres et groupe final de 56 contrôles réussis en Debug (80 ms), banc temporaire retiré et assembly régénérée. Le suivi du bloc et les mesures sont dans `docs/tasks/chips/Vdu80CharacterGenerator/implementation.md` et `docs/chips/Controllers/Vdu80CharacterGenerator.md`.

Le montage source + trajet vidéo isolé atteint 18,88–19,10 % des 24 MHz nécessaires en Debug, sans CPU/CRTC. Ce n'est pas une nouvelle mesure de la machine complète. La conservation de la prochaine échéance n'a pas établi de gain significatif. Reste à raccorder ce circuit au rendu de la machine, puis réduire les coûts logiciels du montage sans supprimer les fronts matériels et vérifier l'ensemble. System 5 reste en cours ; les anciennes annonces de cadence ne couvrent pas ce nouveau raccordement.

Complément VDU80 : source seule environ 268–272 ms et circuit seul 204–209 ms pour 100 ms internes. Conservation directe des niveaux vidéo et de leur connaissance : circuit seul 171–182 ms (réduction médiane environ 12,3 % dans ce scénario). Une garde d'appel supplémentaire sans gain établi a été retirée. Groupe final 56 contrôles réussis, 78 ms ; banc supprimé. Ces résultats n'actualisent pas la cadence machine complète. Raccordement au rendu et coût du générateur toujours à traiter.

Priorité utilisateur confirmée : réduire les coûts des puces avant de poursuivre le câblage VDU80. Profil du réseau 24 MHz relevé ; tentatives de pré-décodage, forme arithmétique et propagation conditionnelle sans gain significatif, retirées. Le contrôle supplémentaire des 256 combinaisons compte/commande du compteur reste ; groupe final 26 réussites, 58 ms, banc et traces nettoyés. Aucune résolution de la régression ni nouvelle vitesse complète revendiquée. Étape suivante : chemin de réception des fronts du SN74LS165, où SetClock repasse par la capture/résolution générale même sur le front descendant.

Gain établi sur le SN74LS165 : chemin complet SetClock simplifié en conservant le contrat de captures partagées. Scénario isolé 2,4 millions de périodes : 184–196 ms avant, 156–161 ms après (environ −16,3 % en durée moyenne). 37 contrôles registre/ROM/trajet VDU80 réussis, 73 ms ; banc supprimé et assembly régénérée. Cette mesure ne résout ni ne remesure encore le coût global de System 5 ; priorité maintenue aux puces.

Générateur 24 MHz optimisé : placement direct des bits et parcours des seuls destinataires changés. Réseau isolé avec 12/2 MHz : 2749–2795 ms avant, 2449–2485 ms après pour 24 millions de périodes (environ −10,9 % en durée moyenne). Sept sorties observées : gain global environ 3,6 %, moins favorable que la seule transformation des bits ; détail conservé dans SystemClock.md. Groupe permanent compteur/générateur/VDU80 : 34 réussites, 59 ms ; banc retiré et assembly régénérée. Aucun front, compte ou état perdu, aucune nouvelle cadence machine complète revendiquée.

Contrôle initial du raccordement VDU80 : compilation module Debug réussie. Groupe de dix contrôles carte/horloges : huit réussis, deux échecs curseur/inversion à mi-caractère. Les bords 80 colonnes, RA, RAM, champs progressifs/entrelacés et les quatre contrôles d'horloge passent. Le manuel identifie un réseau RC CU omis du profil initial ; clarification utilisateur demandée pour un profil nominal calculé/documenté. Référence constructeur acquise : HD46505SP-2 renommé HD68B45SP ; les sorties suivent le descendant CLK avec plafonds séparés. Ne pas contourner cet écart dans les assertions ni le framebuffer ; le bloc reste ouvert.

Contrôle supplémentaire autonome du reset CPU : l'instance VDU80 conserve et avance ses propres comptes pendant Reset(false), soit +9 captures caractère, +54 clocks dot et +4500 ns, source +108 périodes. Groupe ciblé hors les deux cas de curseur déjà échoués : neuf réussites, 204 ms ; ne pas présenter ce groupe comme validation du curseur ou de la machine entière. Les deux assertions de curseur restent présentes et inchangées. Aucun RML ouvert, aucune livraison Debug utilisateur lancée.

RC du curseur réalisé selon l'accord utilisateur, faute de mesure réelle disponible : R12/R13/C6 exacts du schéma, seuil relatif de référence 50 % documenté comme convention calculée, charge conservée, franchissements locaux à 1 ns. Sept contrôles RC plus trajet/registre/ROM : 44 réussites, 79 ms ; groupe System/8271/audio/volumes hors sauvegardes différées : 185 réussites, 6 s. Les deux anciens cas de curseur sont vérifiés suivant le profil électrique calculé et un contrôle indépendant des notifications CU à 167/135 ns ; la supposition initiale d'un curseur aligné exactement sur six dots était non établie. Aucun déplacement correcteur dans le framebuffer, aucun changement du skew. La demande de profil en attente est résolue. Reste l'essai natif réel et la mesure complète du System 5 raccordé.

Raccordement VDU80 et preuve native réalisés : démarrage NOS80, CAT/chargement BASIC depuis le SSD SDK en lecture seule, empreinte exacte C000, PRINT 12+34 = 46. Captures actuelles inspectées et conservées séparément sous TestsMachines et TestsChips/Vdu80CharacterGenerator, passage 20261109-094546. Tableau complet dans la validation System 5 : 115,806415–116,049232 ms/champ, 17,206490–17,242568 % en Debug, ratio source/CPU exactement 12. La première assertion de banc confondait le résultat 46 avec le prompt > sur la même ligne ; seul le banc est corrigé puis l'essai logiciel repassé, sans répéter les mesures déjà valides. Nettoyage des constantes inutilisées, suppression du banc, disque détaché/libéré dans finally, assembly régénérée. Groupe final 185 contrôles réussis (7 s). Raccordement terminé ; machine toujours en cours pour performance insuffisante.

Essai table SN74S163 retiré : compteur isolé plus rapide mais réseau 12/2 MHz environ 4,5 % plus lent. Production rétablie ; contrôle TI étendu à 4096 combinaisons, 27 contrôles compteur/générateur réussis. Détails dans la fiche Sn74S163. Aucune nouvelle mesure machine.

Publication SN74LS165 optimisée, sans modifier les décalages : montage synthétique source/VDU 10,7 % plus court pour motifs nuls, 6,9 % pour motifs changeants ; empreintes identiques et 71 contrôles réussis après nettoyage. Fiches SN74LS165/VDU actualisées. Une mesure native complète reste nécessaire pour quantifier ce gain sur la machine.

Mesure native actuelle : 114,56–116,84 ms/champ, 17,09–17,43 % Debug, ratio source/CPU 12 exact. Aucun gain global significatif établi malgré les gains synthétiques. Capture du moniteur inspectée et conservée, banc supprimé et assembly régénérée. 131 contrôles System/audio réussis (3 s), 71 contrôles de composants réussis (98 ms). Quatre avertissements de tests préexistants, module sans erreur ni avertissement. Machine toujours en cours ; aucun nouveau petit commit créé.

MM52116 : minimum d'échéances réutilisé, réarmements depuis notifications conservés. Gain ROM seule 18–19 % vérifié par A/B/A, aucune amélioration du montage complet établie. 75 contrôles de composants réussis (115 ms), banc retiré ; détail et limites dans Mm52116.md. Le coût de la machine complète doit maintenant être profilé pour cibler les frais restants.

Profil machine actuel consigné dans system5-validation.md et SystemClock.md : publication compteur 9,52 %, publication réseau 8,71 %, NextCount 6,45 %, get_Carry 2,28 %. Capture native et ratio 12 vérifiés, banc/traces supprimés, assembly régénérée ; 131 contrôles System/audio réussis (3 s). Le coût principal reste le réseau 24 MHz ; prochaine évaluation locale dans Sn74S163/implementation.md.

Résultat natif actuel consigné : 19,54–19,63 % Debug, 101,74–102,21 ms/champ ; durée réduite de 11,6 % et images identiques. Banc supprimé, 131 contrôles System/audio et 75 contrôles de composants réussis. Le parcours événementiel/groupé et les portions vidéo stables restent ouverts suivant la demande utilisateur.

- [ ] Remplacer les parcours systématiques du System 5 par des avances bornées aux événements, sans JIT/Dynarec ni saut arbitraire.
  - [ ] Raccorder les avances groupées à la machine réelle.
    - [ ] Modifier les contrôles System/vidéo concernés pour comparer chronologies, comptes, bus, RAM, vidéo et audio ; supprimer les seuls bancs temporaires après mesure native avant/après et consigner les gains significatifs dans le tableau Application aux composants. Construire Debug avec modules à la clôture du bloc complet pour l'essai utilisateur.

Bloc des intervalles stables raccordé et vérifié : 74,69–76,98 ms/champ et 25,94–26,73 % Debug sur la machine entière ; durée moyenne réduite de 25,5 % face au bloc précédent, images et ratio source/CPU identiques. Détails dans system5-validation.md. Banc natif supprimé, assemblies régénérées, 96 contrôles composants et 135 contrôles System/audio réussis. Le profil ancien n'établit pas la distribution actuelle du coût restant ; System 5 demeure en cours. Livraison Debug globale différée jusqu'à la fin des optimisations.

Bloc vérifié : 73,052154 ms/champ en moyenne, 27,34 % Debug ; durée réduite de 3,9 % face au bloc stable précédent. CPU réellement mesuré et référence égaux ici, libellé de la référence précisé dans la validation. Images inchangées, 96 contrôles composants et 135 System/audio réussis. Bancs temporaires supprimés, aucun build de livraison. Machine toujours en cours.

Passage local vidéo retenu : 60,067840 ms/champ, 33,24 % Debug ; réduction de durée 17,8 % face au passage précédent. Images et cycles réels CPU/source identiques, 98 contrôles composants et 135 System/audio réussis. Banc retiré et assembly régénérée ; preuves et conditions dans system5-validation.md. Performance et clôture machine encore ouvertes.

Bascule compacte vérifiée : 59,137233 ms/champ, 33,77 % Debug au dernier passage ; comparaison rapprochée 3,2 % de durée en moins, avec variation initiale documentée. Images/cycles identiques ; 118 contrôles composants et 135 System/audio réussis, bancs et traces supprimés.

Validation actuelle réussie : CAT, BASIC exact et 46 ; SAVE/LOAD puis fermeture et relecture dans une machine neuve, motif exact et 768 octets écrits/relus. Les deux premières erreurs étaient des assertions/séquences du banc corrigées (détachement pendant une commande et exigence de lecture pour un catalogue en cache). Source inchangée, copie et compagnons supprimés, zéro résidu ; banc retiré et assembly régénérée. Production inchangée pendant ce passage.

Premier composant du générateur READY réalisé : SN74LS393, voir docs/tasks/chips/Sn74LS393/implementation.md et sa fiche. Profil calculé paramétrable choisi après réponse utilisateur, 22 contrôles propres ; bloc logique 109 réussis, 307 ms. Gain isolé QD seul 75,7 %, aucune mesure machine nouvelle. 4020/4013 et câblage restent ouverts. Deux derniers commits retirés localement/distant, changements conservés ; aucun nouveau commit.

Mesure raccordée SN74LS393 complétée après demande utilisateur : 6,445 MHz équivalents Debug avec QA/QD/cascade, 30,6 % de durée en moins que le passage long à 4,473 MHz ; 5,944 MHz toutes sorties/cascade. Chronologies identiques, 23 contrôles propres ; bloc logique 110 réussis en 286 ms après nettoyage. Aucun câblage machine nouveau. CD4020B : source et fiche acquises, code non commencé pendant cette reprise du compteur.

CD4020B réalisé et vérifié après SN74LS393 : 14 étages avec Q2/Q3 internes, reset et propagations locales, profils 5/10/15 V. 23 contrôles propres, bloc logique 133 réussis / 322 ms après nettoyage. Toutes sorties physiques observées : 8,798 MHz équivalents Debug pour 15,625 kHz configurés ; Q13 seul : un million de périodes en 0,134333 ms, chronologie identique au parcours détaillé. Sources, profil et preuves dans docs/chips/Logic/Cd4020B.md. Aucun nouveau câblage machine ; prochain composant READY : 4013. Aucun nouveau commit.

CD4013B réalisé et vérifié : deux canaux indépendants, capture montante, SET/RESET hauts, deux sorties avec propagations distinctes, profils 5/10/15 V, événements locaux. 24 contrôles propres ; bloc logique 157 réussis / 308 ms après nettoyage. Cascade stressée à 2 MHz/1 MHz : capacité moyenne premier canal 5,469 MHz équivalents Debug, 29,2 % de durée en moins que 3,872 MHz ; mêmes dates/comptes/empreintes. Fiche docs/chips/Logic/Cd4013B.md, gain réutilisable au tableau Application aux composants. Aucun commit ni nouveau câblage machine. Il reste à terminer la comparaison des autres composants de la carte 400,004 puis son circuit READY complet avant toute validation machine.

SN74LS00 terminé le 9 novembre 2026 : quatre portes NAND sans horloge fictive, délais locaux 9/10 ns du profil typique TI, sorties et inconnus conservés. 17 contrôles propres ; bloc logique reconstruit après nettoyage : 174 réussis, zéro échec/ignoré, 229 ms. Gains isolés scalaires 10,2 % pour une porte, 24,9 % pour quatre, empreintes datées identiques. Transaction parallèle conservée pour un vrai changement simultané, sans gain supplémentaire revendiqué. Voir docs/chips/Logic/Sn74LS00.md. Banc/traces supprimés, aucun nouveau commit ni câblage machine ; 7438, 74LS138 et 74LS93 restent à terminer dans cet ordre.

SN7438 terminé après le 74LS00 : quatre NAND à collecteur ouvert avec états libéré/bas/inconnu, profils de charge 14/11 et 22/18 ns, événements et propagation locaux, aucune horloge fictive. 17 contrôles propres dont ligne partagée avec/sans pull-up ; après nettoyage puis reconstruction, 191 contrôles de logique réussis / 273 ms. Débit isolé scalaire une porte 6,340 MHz équivalents de source, quatre portes fortement sollicitées 2,0205 MHz. Optimisations précédentes réutilisées ; tentative supplémentaire mesurée puis retirée car plus lente, aucun nouveau gain revendiqué. Fiche docs/chips/Logic/Sn7438.md. Aucun nouveau commit ni câblage machine ; restent 74LS138 puis 74LS93 avant la carte.

SN74LS138 terminé ensuite : six entrées, huit sorties actives basses, décodeur/démultiplexeur complet, délais deux/trois niveaux et directions de la variante LS, événements locaux sans clock fictive. Causalité simultanée corrigée avant clôture : premier terme bloquant pour la désélection, derniers termes requis pour la sélection.15 contrôles propres ; après nettoyage/reconstruction,206 contrôles de logique réussis /230 ms. Dernier débit isolé4,457M changements d'adresse/s,7,662M changements G1/s ; durées224,3618/130,514133 ms par million. Gain combiné premier/dernier28,3 %/29,0 % ; qualification des trajets confirmée par retour rapproché17,2 %/13,8 %, toutes dates/empreintes identiques. Fiche docs/chips/Logic/Sn74LS138.md et tableau Application aux composants actualisés. Aucun nouveau commit ni câblage ; reste74LS93 avant la carte.

SN74LS93 terminé après le décodeur : sections /2-/8 indépendantes, cascade externe /16, reset AND et propagations locales ; aucune horloge ni liaison QA→CKB interne fictive.14 contrôles propres, dont toutes phases/masques, cascade sur16 phases,32/16 MHz datés, carries/reset et feedback. Après suppression des bancs/traces puis reconstruction :220 contrôles de logique réussis /240 ms. Retour rapproché cascade174,643967→144,6408 ms par million, -17,2 % ; capacité6,914 MHz équivalents pour source4 MHz et section2 MHz raccordée. CKB observé -28,7 %, QD seul -11,9 %. Fiche docs/chips/Logic/Sn74LS93.md et exemple réutilisable du tableau mis à jour. Tous les types manquants inventoriés pour400,004 sont maintenant réalisés et vérifiés isolément ; définir puis réaliser le câblage de carte avant validation machine et correction finale RESET/READY/INDEX. Aucun nouveau commit.

Préparation du raccordement4 MHz terminée dans le générateur : conservation du regroupement12 MHz jusqu'à la prochaine frontière4/2 MHz raccordée.45 contrôles compteur/générateur après nettoyage et reconstruction,210 ms ; gain isolé7,5 % puis10,2 % dans les séries consignées, chronologies et empreintes identiques. Fiche SystemClock.md et tableau Application aux composants actualisés. Le relais mécanique, le raccordement de la machine et ses nouvelles mesures restent ouverts ; aucun nouveau commit.

Reprise demandée du74LS138 avant poursuite mécanique : la clôture précédente ne prouvait pas l'épuisement des optimisations utiles. Voir son suivi pour les actions concrètes, puis vérifier successivement les six autres nouveaux composants. Cible utilisateur : capacité hôte2× la cadence matérielle au minimum,150× idéalement, avec charge et sorties raccordées, et contrôle final de la machine entière. Pour les circuits sans clock, mesurer changements d'entrée et propagations sans inventer une fréquence. Le nouveau câblage mécanique reste ouvert pendant cette reprise.

Reprise393 en cours : apres suppression d'un mot logiciel redondant, traitement direct des notifications et minimum integre au parcours des propagations,539,75 ms pour1 s materielle sur fronts explicites contre735,90 ms en comparaison alternee,498,17 ms viahelper. Marge du chemin explicite1,85× ; ne pas passer au composant suivant sur la seule moyenne du helper.23 controles propres passes,239 communs apres suppression des diagnostics, module compile et13 controles de la carte400,004 passes. Resultats/fenetres/diff consignes ; profil restant defini dans le suivi du393 avant les prochaines modifications. Aucun nouveau commit ni cadence native actualisee.

## Resolution des canaux actifs - 9 novembre2026

Le profil propre place Resolve27,83 % et ResolvePending26,07 % en tete. La programmation des carries est traitee directement depuis l etage concerne : pas de selection de direction pour les delais inter-etages symetriques du profil. QA conserve sa direction12/13 ns et ses validations. Le parcours des slots est ensuite reuni dans ResolvePending, avec un seul minimum commun et les deux mots captures avant toute resolution, puis publication apres les deux canaux. Aucun compteur, front ou delai modifie. Le regroupement de propagations non observees et les gardes explicites essayes sans gain confirme sont retires. Un controle permanent conserve la borne d avance au milieu du ripple et le clear asynchrone.

Confirmation de la resolution reunie :12 paires alternees de4M periodes source4 MHz, QA/QD/cascade et oracle date independant, apres1M periodes de chauffe pour chaque version. Reference precedente deja optimisee512.77 -> candidat446.31 ms pour1 s materielle, soit12.96 % de temps en moins et2.24x la cadence ; toutes les12 paires favorisent le candidat. Les deux premieres series plus courtes, dont une sans gain, restent dans la preuve.24 controles permanents et le banc passent avant nettoyage. La cible utilisateur50 ms/s n est pas atteinte (cout encore8.93x plus haut) ; reprise ouverte, aucun gain machine deduit, aucun commit.

Preuve complete, fenetres, profils, diffs et tentatives retirees : `artifacts/TestsChips/Sn74LS393/Acorn System 5/20261109-one-second-budget.md`.

Nettoyage du passage : bancs de profilage/comparaison et deux traces possedes supprimes, PID deja supprime dans finally. Apres reconstruction sans bancs :240 controles permanents de logique/horloges passent en234 ms (dont24 propres au393) ; bibliotheque Acorn0 avertissement/0 erreur ;13 controles SystemFloppyBoard passent en1 s. Seul avertissement preexistant xUnit2031 du projet Chips.Tests. Aucun commit ni livraison/application ouverte.

## Cout393 apres regroupement arithmetique borne - 9 novembre2026

Le393 conserve ses deux canaux et toutes les reponses observees ; les seuls calculs QB/QC sans destinataire sont regroupes jusqu a QD si aucune autre reponse n existe a la capture de la date et si la cible le permet. Une erreur de la tentative concernant une notification simultanee du premier canal a ete reproduite puis corrigee avant retention.29 controles propres passent.

Source4 MHz avec QA/QD/cascade,12 paires alternees :430,27 ->410,90 ms par seconde materielle avec callbacks minimaux (-4,50 %),476,92 ->449,02 ms avec hash date (-5,85 %). Ne pas comparer ces valeurs a un passage pris dans un autre processus pour attribuer un gain. Aucun gain de machine mesure ; aucun nouveau raccordement, commit ou livraison. Cible50 ms non atteinte. Details, profils, tests, tentatives retirees et preuves dans docs/chips/Logic/Sn74LS393.md et artifacts/TestsChips/Sn74LS393/Acorn System 5/20261109-one-second-budget.md.

Nettoyage et controles permanents du prefixe retenu : Sn74Ls393SingleReferenceProbe.cs et Sn74Ls393SingleBudgetProbe.cs supprimes avant reconstruction ;245 controles de logique/horloges reussis en282 ms (dont29 propres au393), bibliotheque Acorn0 avertissement/0 erreur,13 controles SystemFloppyBoard reussis. Avertissements preexistants : xUnit2031 dans Chips.Tests ; CS8604, deux CS0649 et xUnit1031 dans LocalEmulationTests. Aucun avertissement sur la construction de la bibliotheque Acorn. Aucun nouveau raccordement, commit ou livraison RML. Cible50 ms et machine System5 restent ouvertes.

##393 - nouveau parcours periodique borne

AdvanceClocks avec ecouteur de date=False :364.57 ->289.68 ms pour1 s source4 MHz (-20.54%),12 paires favorables sur12. AdvanceClocks avec ecouteur de date=True :462.78 ->341.99 ms pour1 s source4 MHz (-26.10%),12 paires favorables sur12. 33 controles propres passent. Les fronts explicites restent un controle sans gain annonce ; le raccordement actuel utilise encore ces fronts et aucun gain machine n est deduit. Cible50 ms non atteinte, puce et machine ouvertes. Algorithme, limites, profils, oracles et preuves dans docs/chips/Logic/Sn74LS393.md et artifacts/TestsChips/Sn74LS393/Acorn System 5/20261109-one-second-budget.md. Aucun commit ni livraison.

Nettoyage et verification du parcours periodique retenu : reference/banc de comparaison et banc de profilage supprimes, deux traces possedees supprimees, PID deja supprime en finally.249 controles permanents de logique/horloges passes en266 ms apres reconstruction (dont33 propres au393) ; bibliotheque Acorn0 avertissement/0 erreur ;13 controles SystemFloppyBoard passes en1 s. Puis le controle de profil asymetrique a ete etendu au helper sur les deux canaux/graines7 et15, en plus des avances directes et bornees :4 cas passes en49 ms. Le code de production n a pas change apres le passage249. Avertissements preexistants xUnit2031 dans Chips.Tests et CS8604/deux CS0649/xUnit1031 dans LocalEmulationTests. Aucun commit, livraison RML ou nouveau cablage ; la cible50 ms et System5 restent ouverts.

## Sélection locale des échéances de carte —10 novembre2026
Essai ordonné retiré : +3,8538 % de temps sur les six fenêtres natives. Carte rétablie byte-identique et reconstruite ;136 contrôles permanents passent. Diagnostic et quatre DLL/source supprimés, assembly locale reconstruite sans diagnostic :0 erreur,4 avertissements préexistants. Nouveau contrôle autonome des priorités conservé ; aucune modification de carte issue de cet essai, aucun commit ni livraison.

## Changement de média sous signaux de lecteur maintenus —10 novembre2026
Défauts reproduits puis corrigés :6/8 cas insertion en échec avant correction ; pulse joint6 transitions au lieu de2. Résultat final :147 contrôles System/carte/audio passent en11 s, dont29 carte. Source et preuves détaillées consignées dans fdc400004.md et les validations machine. Pas de modification d'horloge, reset ou matériel ; aucun gain de vitesse ni clôture System5 revendiqués.
## Détachement du relais pendant READ DATA —10 novembre2026
Défaut confirmé par2 échecs avant correction. Repos à3200 ns, registre5A conservé, premier pulse du nouveau relais à5200 ns et idempotence vérifiés après correction.149 contrôles System/carte/audio passent en12 s, bibliothèque Acorn0 erreur/0 avertissement. Résultats et ordre de fermeture consignés dans la fiche400,004 et les validations. Aucun fichier temporaire ajouté, aucun gain de performance ou clôture machine annoncé.

## Revalidation native après corrections du relais — 10 novembre 2026
Diagnostic supprimé et projet local reconstruit sans lui :0 erreur,4 avertissements préexistants. Capture BMP et rapport de preuves conservés sous artifacts, ignorés par Git. Logiciel46 et cadence13,236 % confirmés sur le code actuel ; aucune clôture machine, livraison RML ou commit de ce seul bloc.

## Persistance depuis le raccordement400,004 actuel — 10 novembre 2026
Diagnostic externe supprimé, copie SSD absente après finally, projet local reconstruit sans le banc :0 erreur,4 avertissements préexistants. Aucun changement de production ni de cadence pendant ce bloc.

## Statut des raccordements déjà réalisés — 10 novembre 2026

Raccordements rapprochés du code actuel : huit anciennes cases de capture/sérialisation VDU80 et câblage400,004 sont cochées sur la base des fichiers présents, des contrôles datés et des validations natives de calcul/persistance du10 novembre. Le relais propre à cette carte est bien SystemFloppyDrive ; Intel8271Drive reste utilisé par les autres profils. Les sections initiales des fiches carte/VDU sont explicitement historiques, sans effacer leurs étapes ni leurs limites de profil. Les autres cases, notamment optimisation/livraison du bloc et revue globale, restent ouvertes : cette mise à jour ne clôture pas System5.

## Publication extérieure pendant la propagation de carte — 10 novembre 2026
Essai retiré : seulement0,1775 % d’écart face au retour à la référence ; aucun gain confirmé.153 contrôles réussis sur la candidate, empreintes natives identiques. Carte rétablie et reconstruite, diagnostic et copie source supprimés, projet local reconstruit sans eux :0 erreur,4 avertissements préexistants. Rapports conservés sous artifacts, aucune modification de production retenue ni commit.

## FORM80 sur le raccordement400,004 actuel — 10 novembre 2026
FORM80 réussi sur les80 pistes du raccordement actuel : confirmation lecteur1, Formatted/Verified et catalogue vide inspectés, donnéesA5 remplacées parE5 sur798 secteurs après fermeture. Original utilitaire inchangé. Diagnostic8 min15 s, aucun changement de production ; fiche8271/carte et validations mises à jour. Diagnostic, SSD possédé et progression jetable supprimés ; rapport et trois BMP conservés. Projet local reconstruit sans diagnostic :0 erreur,4 avertissements préexistants. Aucune clôture globale, écoute humaine ou nouvelle mesure de cadence annoncée.

## Sélection de face FAULT/OP0 réellement utilisée par System5 — 10 novembre 2026
Correction de sélection de face réalisée et vérifiée :159 contrôles System/carte/audio, dont41 carte, puis DOS natif par la vraie fabrique sur volumes1/3 ;1536 octets écrits et1536 relus dans un nouvel invité, motifs et catalogues indépendants. Source d’origine inchangée, DSD possédé supprimé en finally. Diagnostic et capture d’échec de préparation retirés ; deux captures finales et rapport conservés. Reconstruction sans diagnostic :0 erreur,4 avertissements préexistants. Aucune nouvelle cadence mesurée ni clôture globale System5 ; pas de modification App/SDK ou interne des puces.

## Bilan des exigences System5 sur le code retenu — 10 novembre 2026
Bilan par exigence consigné dans la validation machine ;1140 composants,246 System/disque/audio et40 médias passent sans échec. Rapports conservés sous artifacts. Performance, couverture complète des parcours de médias et livraison/essai RML restent ouverts ; aucune clôture globale ni nouveau gain annoncé.

## Parcours natifs des formats de disquette proposés — 10 novembre 2026

Six parcours natifs réussis en18 s ; correction du lecteur DFS double face JFD vérifiée par41 régressions réussies. Diagnostic retiré, aucun dossier media-pipeline possédé restant ; reconstruction locale sans diagnostic réussie (0 erreur,4 avertissements préexistants). Rapports ignorés conservés. Plan physique et descripteur graphique ne prouvent ni support physique ni rendu RML ; aucune nouvelle cadence annoncée. Détails dans system5-validation.md et la fiche commune des formats.

## Comparaison du minimum local de la carte400,004 — 10 novembre2026

Candidat retiré :162,36053 ms/champ contre146,48434 initialement et148,92039 au retour ; empreintes et durées natives identiques,159 contrôles réussis. Aucune optimisation de production conservée. Mesure actuelle du moniteur :13,4085 % nominal. Diagnostic et copie source supprimés, Acorn reconstruit0 erreur/0 avertissement et projet local sans diagnostic0 erreur/4 avertissements préexistants. Résultats consignés dans la fiche carte et les validations ; performance et clôture de System5 toujours ouvertes.

## Logiciel invité depuis les conteneurs natifs —10 novembre2026

Chargements invités JFD/SCP/HFE/HxC réussis après correction du flux physique commun ;437 contrôles médias/physiques réussis,1 échec cartouche reproduit sans ces modifications. Les limites et l’écart vidéo sous BASIC sont consignés ; aucune clôture de la machine. Diagnostic et dossiers possédés supprimés, projet local reconstruit sans eux :0 erreur,4 avertissements préexistants.

## Vérification des champs VDU80 sous BASIC —alerte d’aperçu infirmée

Hypothèse de champs partiels infirmée : les quatre BMP initiaux sont identiques ; les16 champs du nouveau diagnostic natif/isolé, groupé/scalaires sont aussi tous identiques. Aucune correction de puce ou de présentation nécessaire pour cet écart apparent. Diagnostic et deux binaires temporaires supprimés, callbacks et ressources libérés ; projet reconstruit sans diagnostic0 erreur/4 avertissements préexistants. Rapports/captures et rectification conservés dans les fiches. Performance et livraison System5 restent ouvertes.

## Validation et livraison du bloc System5 —10 novembre2026
Livraison réussie : script0, exécutable et24 entrées de modules vérifiés. RML fermé et laissé au contrôle utilisateur. Cadence13,8834 % explicitée ; essai RML encore attendu, aucune clôture globale Acorn.

## Préparation de l’essai RML System5 —10 novembre2026
- [ ] Préparer et observer le profil System5 du Debug livré, sans clôture anticipée de la validation utilisateur.
  - [ ] Modifier `docs/project/emulations/Acorn/Acorn System/System 5/validation.md` et la validation de référence avec les observations réelles de RML : ouvrir le Debug livré si aucune instance existante, vérifier le profil, ROM et médias déjà configurés avant toute modification ; afficher System5 et son état via les commandes UI existantes. Conserver les observations/captures effectivement obtenues, distinguer test automatique et retour utilisateur encore attendu. Fermer et attendre la fin de toute instance possédée après observation, y compris en cas d’erreur, sans interrompre une instance empruntée. Ajouter toute correction établie à la liste avant modification de code.

## Réduction des réveils de propagation sans sortie raccordée —10 novembre2026
- [ ] Réduire les passages de coordination disquette, puis mesurer la machine complète en Debug.

## Trains d’horloge dans les composants —10 novembre2026
- [ ] Terminer les récepteurs d’horloge requis avant de modifier leur raccordement.

## Reprise structurelle de la chaîne horloges–disquette —10 novembre2026
- [ ] Remplacer le transport front par front entre couches par la description exacte des horloges périodiques, conserver les composants et leurs états locaux.
  - [ ] Modifier `docs/tasks/chips/Sn74LS93/implementation.md`, puis créer `Services/Sn74Ls93.PeriodicOutput.cs` et compléter le service/interface93 : représenter la sortie QA périodique par sa phase et ses délais réels, annuler sa description aux changements de source/reset et différer les calculs non observés ; publier séparément les échéances raccordées. Compléter `Sn74Ls93Tests.cs` pour comparer l'aval périodique à toutes les transitions explicites, reset, remplacement et reprises ; consigner les résultats.
  - [ ] Modifier `SystemFloppyBoard.cs`, `.Separator.cs` et `SystemFloppyBoardTests.cs` dans les dossiers déjà identifiés : prolonger le transport périodique IC14/QA→IC7/CKA avec annulation et niveau mémorisé ; laisser les autres fils explicites, comparer les commandes réelles. Modifier temporairement `SystemFiveClockChainTiming.cs` pour isoler la référence scalaire complète, comparer la machine entière et conserver le candidat seulement après un gain établi. Consigner résultats et limites dans les fiches93/carte/machine.

## Préfixes CPU privés et replay indépendant vidéo/source —10 novembre2026
- [ ] Raccorder la refonte macro, en conservant le parcours de référence pour toute interaction non représentable.
  - [ ] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/SystemFloppyBoard.Ready.cs` et `SystemFloppyBoard.cs` : mémoriser l'activité temporelle8271 depuis NextClockEventChanged et fournir la borne du connecteur de lecteurs depuis les échéances reçues. La qualification de préfixe ne doit pas interroger les puces en boucle ni franchir un effet pouvant changer NMI/READY. Conserver le chemin ordinaire pour contrôleur actif et toutes les entrées non qualifiées. Tester l'invalidation par commande, capteur et événement de lecteur dans `SystemFloppyBoardTests.cs`.
  - [ ] Modifier `AcornEmuSystemClockTests.cs` et les tests vidéo existants, créer si nécessaire `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemPrivateClockTests.cs` autonome : comparer le parcours normal et les préfixes sur mémoire privée puis accèsVDU/VIA/8271, interruptionsVIA, IRQ/NMI, WAIT/DMA, champs pairs/impairs, écritures aux frontières et ticks/audio. Prévoir invalidation avant toute entrée non représentée ; si l'équivalence d'un cas n'est pas établie, conserver son parcours ordinaire sans supprimer la fonctionnalité.
  - [ ] Créer seulement après intégration un diagnostic natif temporaire (chemins à préciser avant création) : comparer le bloc complet à a9245eb4b dans des contextes isolés, ROM/programmes identiques, vidéo/audio/CPU/RAM et durées comparés,20 champs de chauffe puis3×20 appariés ; mesurer aussi un scénario logiciel avec opérations de périphériques. Consigner le gain réel et les limites dans les fiches des composants et de System5, supprimer les diagnostics et reconstruire sans eux. Aucun gain50 % ou100 % nominal annoncé sans preuve.

### Précisions du raccordement macro avant code
Les nouveaux fichiers de production de ce groupe incluent aussi `Constants/SystemFloppyBoardConstants.cs` (bit d'activation physiqueG1 du décodeur), `SystemFloppyBoard.Bus.cs` (preuve d'inhibition et stabilité des pins de sélection), `Constants/SystemConstants.cs` (taille maximale d'un préfixe de calcul), et la qualification de cycles complets dans SystemClock. La classification des accès privés réutilise les fenêtres VDU/VIA et prouve l'inhibition du décodeur8271 par ses enables, sans créer de deuxième décodeur. Les sorties CS/DACK doivent être connues inactives et leurs propagations terminées avant tout préfixe. Les événements du connecteur et les échéances internes8271 non indépendantes conservent le parcours ordinaire. Aucune mesure isolée entre ces raccordements ; la preuve finale porte sur la machine assemblée.

### État vérifié après mesure et retrait du diagnostic
8 comparaisons autonomes de préfixes réussies ;143 contrôles System/carte réussis avant l'ajout des3 cas de frontière. Diagnostic externe supprimé, DLL de tests reconstruite sans ce fichier (0 erreur,4 avertissements existants). Les actions de qualification des capteurs/lecteurs et les essais de périphériques plus complets restent ouverts : ne pas cocher l'ensemble du raccordement sur la seule lecture du statut8271. Le comparatif par contextes isolés envisagé ci-dessus a été remplacé par les deux chemins de la même DLL via le sélecteur interne existant ; son protocole réellement exécuté est décrit dans la section de mesure et les validations.
NOS80 :111,08373→92,10335 ms/champ,17,087 % de réduction,21,680 % nominal. Machine toujours ouverte. Les états CPU, RAM, vidéo/audio et comptes correspondent ; aucune fin de développement ni cadence100 % déduite.

## Séparateur verrouillé : représentation des entrées masquées —10 novembre2026

### Résultat du bloc séparateur
23 contrôles93 réussis(605 ms),153 contrôles System/carte réussis(18 s),3031 scénarios de pulses aux mêmes dates DATA WINDOW. Comparaison native3×20 champs après20 de chauffe :85,85126→66,06334 ms/champ,23,049 % de moins,30,226 % nominal. Diagnostic temporaire retiré et tests reconstruits sans lui :0 erreur,4 avertissements préexistants. Le bloc de représentation est retenu ; System5 reste ouvert, la suite concerne les coûts vidéo et les autres coûts restants. Aucun résultat100 % ni clôture machine déduit.

## Caractères constants complets —10 novembre2026

### Statut de fin du passage caractères
Essai de paquets dot retiré après comparaison ; seules les périodes complètes de caractères établis sont conservées.50 contrôles composants et155 contrôles System/carte passent. Diagnostic externe supprimé et DLL de tests reconstruite sans lui (0 erreur,4 avertissements préexistants). Mesure globale retenue65,38816→58,78570 ms/champ,10,097 % de moins,33,967 % nominal. Les autres points de qualification des préfixes et la cadence machine restent ouverts ; aucun passage à la machine suivante ni nouvelle livraison RML déduit.

## Liaison QD établie — 10 novembre 2026
Le premier canal du 74LS393 décrit son signal QD au second canal par le raccordement extérieur de la carte. Chaque canal conserve ses phases, compteurs et échéances. Les clears et changements de source révoquent la description et rétablissent les fronts explicites jusqu’à requalification ; les demandes QA du 8271 restent datées.

Validation : 54 contrôles autonomes du 393 réussis, y compris remplacement de période source et reprise explicite ; 157 contrôles System/carte réussis. TRX final : `artifacts/TestsChips/Sn74LS393/Acorn System 5/20261110-qd-periodic-wire-final.trx`.

Comparaison native Debug, mêmes DLL/ROM NOS80/VDU80 et horloges, 20 champs de chauffe puis trois paires alternées de 20 champs :

| Fenêtre | QD scalaire, ms/champ | QD décrit, ms/champ |
| --- | ---: | ---: |
| 1 | 55,89058 | 48,50899 |
| 2 | 57,47379 | 49,64622 |
| 3 | 56,67139 | 49,60304 |
| Moyenne | 56,67859 | 49,25275 |

Temps réduit de 13,102 %, soit 40,542 % du nominal et 20,303 champs/s hors interface. Vidéo, audio, durée, registres CPU, RAM et compteurs identiques pour les trois paires. Ce résultat concerne la machine entière, pas un débit isolé du 393. System 5 reste sous la cadence nominale et non terminé. Rapport : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-qd-divider-native.md`.

Diagnostic temporaire supprimé ; reconstruction des tests sans ce fichier réussie : 0 erreur, 4 avertissements préexistants.

## Portions vidéo linéaires retenues —10 novembre2026
Le CRTC conserve ses compteurs et calcule une portion sans autre changement de signal. Son destinataire accepte les adresses successives uniquement si la RAM présente les mêmes données et si le VDU peut représenter les périodes complètes avant ses échéances. Le rendu conserve les coordonnées des demi-cellules et les mêmes RGB. Les frontières de ligne, DE/CU, synchronisations, changements de données, LP et reset reprennent le parcours ordinaire.
110 contrôles CRTC et164 contrôles System/carte réussis, zéro échec. Les nouveaux tests comparent états CRTC complets, traces datées, phases et comptes VDU, champs, audio, CPU et RAM ; skews0..3, progressif/entrelacé et écritures vidéo inclus. Rapports TRX `20261110-linear-characters.trx` sous TestsChips/MC6845 et `20261110-linear-video-integration.trx` sous TestsMachines/Acorn/Acorn System 5.

Comparaison Debug native NOS80/VDU80, RAM2000–DFFF, carte400004 sans média, mêmes DLL et horloges, tous les parcours déjà retenus actifs ; seul GroupLinearCharacters diffère.20 champs de chauffe puis3×20 en ordre alterné. RunFrame/DrainAudio chronométrés ; calcul des hashes hors chronomètre.

| Fenêtre | Caractères unitaires, ms/champ | Portion linéaire, ms/champ |
| --- | ---: | ---: |
| 1 | 51,26419 | 47,17495 |
| 2 | 50,50883 | 46,79031 |
| 3 | 51,23617 | 43,49262 |
| Moyenne | 51,00306 | 45,81929 |

Temps réduit de10,164 %, cadence43,580 % nominal,21,82487 champs/s hors interface.2 039 859 caractères effectivement représentés pendant chauffe/mesure. Les trois hashes vidéo/audio/durée, registres CPU/cycles, RAM, ticks/source, état CRTC et comptes VDU sont identiques. Deux cœurs possédés libérés par using ; aucun lancement RML ni changement de compilation Debug. Rapport `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-linear-video-native.md`. Comparaison appariée de ce bloc uniquement : les valeurs de passages antérieurs ne constituent pas sa référence. Gain retenu, System5 reste incomplet sur sa cadence ; aucun passage à la machine suivante.

Diagnostic externe supprimé après consignation ; reconstruction sans lui réussie,0 erreur et4 avertissements préexistants. Bloc conservé, cadence globale encore ouverte.

## Portions privées bornées par les interactions —10 novembre2026

### Décision de l’essai
168 contrôles System/carte réussis. Mesure appariée43,78620→43,98451 ms/champ, soit0,453 % plus lent.463993→463812 portions cumulées sur80 champs (20 de chauffe+60 mesurés),181 évitées seulement. Aucun gain établi ; retrait des changements de production, des seuls tests de cette tentative et du diagnostic externe. Le plafond16 n’est pas retenu comme goulet établi.

## Causes des arrêts de portions —10 novembre2026
Retour intégral au code de production du point bfa9f8e14 après l’essai sans gain du plafond16. Sonde temporaire ensuite appliquée sans changement des bornes, avec comparaison native NOS80/VDU80/carte400004 sans média :20 champs de chauffe exclus, puis20 champs comparés avec et sans sonde en ordre alterné. Vidéo/audio/durée, CPU, RAM, ticks et compte source identiques ; les deux diagnostics ont réussi et leurs cœurs possédés ont été libérés par using.

345874 tentatives sur20 champs, dont116777 portions exécutées (5838,85/champ) et229097 refus (11454,85/champ). Les463993 portions de l’essai précédent étaient cumulées sur80 champs, et non par champ.

| Cause | Nombre sur20 champs | Par champ | Vérification ou portion complète, ms/champ | Parcours ordinaire après refus, ms/champ |
| --- | ---: | ---: | ---: | ---: |
|executed/next/via/read/r1|113211|5660.55|20.89793|0.00000|
|refused/prepared/via/read/r1|114103|5705.15|2.58478|13.23084|
|refused/bus-propagation-pending/decode-unconnected-outputs-only|114103|5705.15|2.60431|11.60355|
|executed/line-boundary|3566|178.30|0.63385|0.00000|
|refused/short/line-boundary|891|44.55|0.01175|0.10248|

Les temps de portion entière incluent son CPU/VIA/audio, sa vidéo et ses remises à jour ; ils ne sont pas le coût intrinsèque de la cause d’arrêt. Les sous-domaines détaillés dans les rapports ne s’ajoutent pas à leurs parents. Sonde active1078,6248 ms contre913,7078 ms désactivée sur20 champs :18,049 % de surcoût. Les mesures instrumentées servent à classer le travail, pas à annoncer une cadence ni une économie récupérable intégralement. Vérifications refusées104,0166 ms cumulées (5,20083 ms/champ), incluant la construction des classifications par la sonde ; cette valeur ne mesure pas le seul coût de la vérification dans le code sans sonde.

Conclusions établies :
- Les lectures VIA du registre1 (portA, clavier) sont de véritables transactions. Les acquittements et IRQ de Via6522.Read/AcknowledgePort doivent rester à leur phase CPU ; leur fréquence ne prouve pas que le VIA soit coûteux.
- Le refus suivant « bus-propagation-pending » a été affiné sans modifier le matériel :114103 cas sur114103 avec seulement des sorties non raccordées du138 en attente, aucun pendingY1, aucune échéance du contrôle, CS et DACK déjà hauts. `SystemFloppyBoard.PrivateBusPinsSettled` attend actuellement la prochaine échéance interne globale du138. Cette attente est une condition logicielle trop large ; elle n’établit aucune interaction physique à attendre sur les fils raccordés.
- Le parcours ordinaire après ce seul refus représente232,0709 ms instrumentées,11,60355 ms/champ ; il contient encore du travail matériel réel (CPU/vidéo/horloges) à représenter, donc ne pas promettre sa suppression intégrale. La suite doit distinguer les échéances internes et celles des sorties raccordées dans la puce, puis raccorder la carte à ces dernières, sans supprimer aucun délai interne.
-3566 portions se terminent à une frontière de ligne et891 tentatives y sont trop courtes ; ces limites protègent le début de champ. Aucune activité de lecteur, attente/DMA ou bande active n’a causé d’arrêt dans ce scénario au repos ; cela ne vaut pas couverture de ces autres scénarios.

Rapports exhaustifs conservés : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-prefix-causes.{json,md,trx}` et `20261110-prefix-causes-refined.{json,md,trx}`. Les deux passages donnent les mêmes effectifs ; la seconde sonde distingue les sorties non raccordées. Aucun gain de performance produit par ce diagnostic ni modification d’architecture conservée.

Instrumentation et diagnostics retirés ; sources de production restaurées exactement. Reconstructions Acorn et tests sans sonde réussies :0 erreur,0 puis4 avertissements préexistants. Aucun changement de comportement conservé de cette analyse.

## Coordination du décodeur limitée aux sorties raccordées —10 novembre2026

Inventaire des autres machines/puces reporté explicitement par l’utilisateur : ne pas le poursuivre maintenant. La règle générique doit figurer au tableau des directives pour les prochaines implémentations.

## Échéances raccordées du décodeur retenues —10 novembre2026
Le schéma400,004 figure8/PDF108 et le brochage TI page1 ont été inspectés : IC4/Y1 (broche14) seul est relié au8271/24 et à IC2/12 ; les sept autres sorties ne sont pas exportées. Le tableau complet des destinations se trouve dans fdc400004.md. Aucun oubli de raccordement de ce boîtier établi.
Le138 conserve toutes ses propagations internes et offre séparément NextOutputEventNanoseconds. La carte reçoit ce dernier écouteur pour son masqueY1 et synchronise toujours la puce avant chaque nouvelle entrée de bus. Les changements de CS/DACK et leurs effets restent datés ; l’attente liée uniquement aux sorties non raccordées ne bloque plus les périodes indépendantes. Le chemin global reste sélectionnable en interne pour comparaison, sans nouvelle option utilisateur ni matériel. Les écouteurs possédés sont détachés à Dispose.

19 contrôles138 réussis (519 ms) puis165 contrôles System/carte réussis (20 s), zéro échec/ignoré. Le nouveau contrôle compare CS/DACK/READY/NMI/compte8271 nanoseconde par nanoseconde, sur sélections/désélections et miroirs, et distingue la propagation interne non raccordée du bus réellement disponible. Les contrôles de transferts préexistants sont conservés.
Comparaison native Debug sans sonde, mêmes DLL, NOS80/VDU80, RAM2000–DFFF,400004 sans média ; seul le transport d’échéance globale/observée diffère.20 champs de chauffe puis3×20 appariés en ordre alterné, RunFrame/DrainAudio chronométrés et hashes hors temps.

| Fenêtre | Échéance globale, ms/champ | Échéance raccordée, ms/champ |
| --- | ---: | ---: |
| 1 | 45,15266 | 33,90353 |
| 2 | 44,78374 | 34,06753 |
| 3 | 47,62626 | 34,41513 |
| Moyenne | 45,85422 | 34,12873 |

Temps réduit de25,571 %, vitesse×1,34357. Champ matériel19,968 ms,50,08013 Hz ; candidat29,30083 champs/s,58,508 % nominal, environ1,170 MHz émulé pour le CPU2 MHz. Vidéo/audio/durée, CPU/cycles, RAM, ticks/source et niveaux/connus des huit sorties138 identiques sur les trois fenêtres. Les sorties internes différées sont matérialisées avant cette observation, hors chronométrage. Chaque cœur est libéré via using. Aucun changement des options Debug ni des fréquences ; aucune capture RML ni mesure Release déduite. Rapport `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-decoder-deadline-native.md` et TRX associé. Gain retenu ; System5 reste sous sa cadence nominale.
La règle générale est ajoutée au tableau Application aux composants pour les prochaines machines et puces. L’inventaire des autres machines et leurs corrections sont reportés à la demande explicite de l’utilisateur ; aucune modification de ces autres machines réalisée.

Diagnostic natif externe supprimé après consignation ; reconstruction des tests sans lui réussie,0 erreur et4 avertissements préexistants. Ce bloc est terminé et vérifié ; la cadence et la clôture System5 restent ouvertes.

## Lecture VIA par le mécanisme de périodes indépendantes —10 novembre2026
Le VIA conserve sa lecture effective du portA, ses acquittements et ses IRQ dans le cycle CPU normal. `SystemFastCpuBus` qualifie uniquement une lecture préparée du portA ; `AcornSystemMachine` reçoit la propriété des sorties de contrôle par ControlOutputsChanged, initialisée au raccordement et détachée à Dispose. Aucun sondage de registre VIA n’est ajouté. La représentation est refusée si une sortie de contrôle est pilotée, si WAIT/DMA ne sont pas qualifiés, si la bande est active, si le contrôleur/bus disquette a une échéance ou si une interaction de lecteur arrive dans ce cycle.
Le même mécanisme de portions indépendantes est réutilisé, borné ici à un seul cycle : phases CPU/PH2-VIA, clavier et audio effectivement exécutés ; helpers vidéo et réseau d’horloges existants pour les récepteurs indépendants ; prochaine adresse présentée après cette période au bon temps source. Ne pas regrouper plusieurs changements d’adresse VIA : les propagations internes non raccordées du138 doivent également rester retrouvables. Aucun changement de CPU, VIA, fréquences ou options Debug. Le compteur historique de cycles représentés inclut ce domaine CPU/VIA ; RepresentedViaReadPeriods en distingue les lectures seules.
Le schémaCPU400,005 PDF62, détail `System5-cpu-via-clock-62.png`, confirme PH2 du CPU vers VIA et IRQ vers CPU ; CA/CB aboutissent au connecteurPL1. La qualification conservative sans sorties de contrôle pilotées ne prétend pas rendre ces broches inutilisées pour toutes les configurations ; les modes de sortie conservent le parcours ordinaire complet. Les fonctions et callbacks VIA restent celles du composant existant.

167 contrôles System/carte réussis (19 s), puis les2 cas progressif/entrelacé renforcés passent (362 ms). Ils confrontent les états VIA complets, champs/CPU/RAM/audio, changements clavier, compteurs VDU/source, notifications vidéo à leurs dates locales, et refus/reprise après programmation de CA2 en sortie impulsionnelle. Les scénarios WAIT/DMA et transferts précédents sont conservés. TRX `20261110-via-read-integration.trx` et `20261110-via-read-video-events.trx`, sous les captures System5.

Mesure native Debug, NOS80/VDU80, RAM2000–DFFF,400004 sans média, même DLL et seul GroupViaReadPeriods différent.20 champs de chauffe puis3×20 alternés ; RunFrame/DrainAudio chronométrés, hashes hors temps.

| Fenêtre | Lecture par parcours ordinaire, ms/champ | Cycle indépendant, ms/champ |
| --- | ---: | ---: |
| 1 | 33,80010 | 31,39595 |
| 2 | 34,27853 | 30,11957 |
| 3 | 33,28066 | 30,01901 |
| Moyenne | 33,78643 | 30,51151 |

Temps réduit de9,693 %,65,444 % nominal,32,77451 champs/s hors interface (champ19,968 ms,50,08013 Hz), environ1,309 MHz CPU équivalents pour les2 MHz réels.453088 lectures représentées pendant80 champs chauffe/mesure. Les trois hashes vidéo/audio/durée, CPU/cycles, RAM, ticks/source, état VIA et comptes VDU sont identiques. Cœurs libérés via using, aucune fenêtre RML ni compilation Release. Gain modéré mais retrouvé dans les trois fenêtres et conservé ; ce passage ne termine pas System5 ni sa cadence. Rapport `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-via-read-native.md`. Le parcours ordinaire demeure la référence interne et traite intégralement les cas non qualifiés. Aucune reprise d’autres machines ou puces.

Diagnostic externe retiré après consignation ; reconstruction des tests sans lui réussie :0 erreur,4 avertissements préexistants. Bloc conservé et vérifié ; les autres coûts et la clôture de System5 restent ouverts.

## Qualification mémoire conservée pendant le cycle préparé —10 novembre2026
- [ ] Évaluer la suppression du redécodage logiciel des accès mémoire privés, sans mettre leurs données en cache.
  - [ ] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/AcornSystemMachine.Bus.cs` et `SystemFastCpuBus.cs` : extraire les seules lectures/écritures RAM-ROM déjà existantes en opérations communes ; conserver la qualification privée de l’adresse lors de PrepareRead/PrepareWrite et la réutiliser durant la portion indépendante déjà validée. Lire les données à la phase réelle, conserver les écritures ROM ignorées, les trous et la priorité du vrai bus hors de ces portions. Aucun nouveau décodeur physique, nouvelle copie de mémoire ni changement de transaction. Garder un témoin interne désactivant cette réutilisation.
  - [ ] Compléter `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemPrivateClockTests.cs` : comparer mémoire/CPU/champs/audio/VIA au témoin, vérifier lecture après mutation RAM entre préparation et transfert, ROM/trou en écriture et exclusion des accès périphériques ; conserver les cas WAIT/DMA/IRQ et transferts précédents.
  - [ ] Créer temporairement `tests/RetroMediaLab.LocalEmulationTests/Emulation/SystemFivePreparedMemoryTiming.cs` : deux cœurs NOS80/VDU80 identiques, qualification réutilisée false/true,20 champs de chauffe puis3×20 alternés ; comparer CPU/RAM/vidéo/audio/durée/comptes et libérer les cœurs via using. Écrire `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-prepared-memory-native.md`, consigner les fenêtres et décider selon le gain réel ; retirer l’essai si faible ou instable. Supprimer le diagnostic externe après consignation et reconstruire sans lui. Ajouter au tableau seulement un gain significatif retenu.

### Essai de qualification mémoire interrompu avant mesure
À la nouvelle demande utilisateur, remettre les deux sources modifiées au point26a88f94f et mesurer d’abord les parts vidéo et les coûts autour du VIA. Le code de cet essai n’a été ni compilé ni testé ni mesuré ; aucun gain attribué et aucune nouvelle optimisation retenue.

## Répartition vidéo et coût des cycles VIA —10 novembre2026
À la demande utilisateur, l’essai de réutilisation du décodage mémoire a été retiré sans compilation, tests ni mesure. Les deux sources de cet essai ont été rétablies depuis26a88f94f avant ce diagnostic. Aucun gain mémoire annoncé.
Profil natif NOS80/VDU80, RAM2000–DFFF,400004 sans média, Debug inchangé (DisableOptimizations=true).20 champs de chauffe puis8 champs appariés avec/sans sonde, vidéo/audio/durée/CPU/RAM/état VIA identiques ; puis245 champs complets sur8009,2153 ms de RunFrame/DrainAudio. Unités Speedscope milliseconds contrôlées. Calibration8 champs :256,7001 ms avec sonde,256,8216 ms sans ;−0,047 % est une variation à cette précision, pas un gain démontré. Les temps de collecte ne remplacent pas la dernière cadence native appariée30,51151 ms/champ. Deux cœurs et le processus de collecte possédés fermés/attendus/libérés via using/finally.

| Vidéo sur245 champs | Périodes | Part |
| --- | ---: | ---: |
| Caractères en portions linéaires | 7549421 | 77.15836 % |
| Autres caractères complets établis | 1902283 | 19.44216 % |
| Caractères hors de ces deux représentations | 332616 | 3.39948 % |
| Total caractères physiques | 9784320 |100 %|
| Points représentés par les caractères complets | 56710224 | 96.60052 % |
| Autres périodes dot stables | 1734025 | 2.95375 % |
| Montées dot traitées individuellement | 261671 | 0.44573 % |
| Total périodes dot physiques | 58705920 |100 %|

La somme calculée des points est exactement58705920, égale au compte physique VDU, sans tolérance. Les caractères complets incluent des traitements d’un seul caractère :96,60052 % ne signifie donc pas96,6 % de longues portions. Le sous-total linéaire77,15836 % identifie les portions de plusieurs caractères. Dans le récepteur indépendant,9773400 caractères ont été offerts,321696 ont suivi la boucle par caractère ;1990945 offres linéaires et2223979 offres de caractère établi ont été évaluées. Les offres ne comptent pas des acceptations de portions ; les compteurs de périodes acceptées sont distincts.

| Partie de l’exécution | Temps propre % | ms/champ collecté |
| --- | ---: | ---: |
|PrivateMemory|65.836061|21.519689|
|VIA|29.159054|9.531156|
|Other|5.004885|1.635936|
|Total|100,000000||

Les repères NoInlining VIA/mémoire appellent le même corps de traitement, avec les mêmes gardes. Chaque intervalle est attribué une seule fois au dernier appelant Chips/Acorn ; la partie VIA comprend tout son CPU/vidéo/horloges/bus, pas le coût intrinsèque de Via6522.Read.1397760 lectures VIA ont été représentées pendant la collecte, soit5705,14 par champ. Les compteurs de référence comptent9784320 cycles ;9773400 sont représentés dans le domaine CPU/VIA, dont ces lectures.

| Coût autour du VIA | ms/champ | Part de cette branche |
| --- | ---: | ---: |
|ClockCatchup|2.130077|22.348565 %|
|CpuViaKeyboardAudio|2.092013|21.949209 %|
|Video|2.061697|21.631133 %|
|FloppyComponents|1.971315|20.682858 %|
|BusWiring|0.702673|7.372380 %|
|CoordinationAndOther|0.573380|6.015854 %|
|Total|9,531156|100 %|

| Composant, coût propre | % du total | ms/champ |
| --- | ---: | ---: |
|Cpu6502|23.815393|7.784485|
|AcornSystemMachine|15.421898|5.040922|
|SystemVideo|9.107868|2.977069|
|Sn74Ls138|6.112459|1.997966|
|SystemFastCpuBus|5.983158|1.955702|
|Sn74S163PeriodicInputs|5.399411|1.764894|
|Crtc6845|4.409419|1.441297|
|Vdu80CharacterGenerator|4.166695|1.361958|
|SystemFloppyBoard|3.898553|1.274311|
|Sn74S163|3.536443|1.155949|
|Sn74Ls374|3.295306|1.077129|
|Sn74Ls393|3.086777|1.008968|
|Via6522|2.261502|0.739212|
|SystemCpuClockControl|1.956130|0.639396|
|SystemClockGenerator|1.890235|0.617857|
|Sn74Ls74|1.603857|0.524249|
|Sn74Ls165|1.574333|0.514599|
|Intel8271|1.118669|0.365657|
|Mm52116|0.543365|0.177608|
|Cd4020B|0.351722|0.114967|
|SystemKeyboard|0.223914|0.073190|
|IIndependentClockPeriodReceiver|0.168206|0.054981|
|Sn74Ls138Timing|0.055962|0.018292|
|SystemKeyboardRepeat|0.018726|0.006121|
|Total|100 %||

Conclusion : les fronts dot unitaires ne représentent plus que0,44573 % des périodes, donc leur nombre ne soutient pas une nouvelle refonte point par point en priorité. Le travail autour du VIA se répartit principalement en remises à jour horloges2,1301 ms, CPU/VIA/clavier/audio2,0920 ms, vidéo2,0617 ms et composants disquette1,9713 ms par champ. La priorité reste l’amortissement des mises à jour indépendantes autour des transactions, en conservant chaque adresse reçue, lecture, acquittement, IRQ et événement à sa date. Aucune économie intégrale de ces9,53 ms n’est promise : ils comprennent du travail matériel utile. La réutilisation du décodage demeure reportée ; aucune architecture supplémentaire n’a été conservée de ce diagnostic.
Limite : scénario au repos sans média. Les durées de lecture/écriture disquette et leurs interactions doivent être mesurées séparément avec les scénarios de transfert existants ; ces valeurs ne leur sont pas attribuées. Aucune reprise des autres machines. Rapports complets persistants `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-video-via-profile.{json,md}` : toutes les méthodes propres, inclusifs séparés et attribution par branche, aucun parent ajouté à ses descendants.

Sondes retirées et sources rétablies exactement au point26a88f94f ; reconstructions Acorn/tests sans diagnostic réussies,0 erreur,0 puis4 avertissements préexistants. Seuls les documents de mesure restent modifiés. Le contrôle des performances avec médias et la réduction des mises à jour autour du VIA restent à réaliser ; aucune nouvelle optimisation de production annoncée.

## Résolution source amortie entre transactions —10 novembre2026

Diagnostic natif externe supprime apres consignation ; reconstruction du projet local sans ce fichier reussie : zero erreur, quatre avertissements preexistants. Aucun processus ni coeur de diagnostic conserve.

## Transferts disquette dans la machine complète — 10 novembre 2026

Banc temporaire supprime apres les deux mesures ; reconstruction locale sans ce fichier reussie : zero erreur, quatre avertissements preexistants. Les deux cas autonomes sont conserves ; aucune modification des composants ou du cablage de production dans ce bloc.

## Domaines indépendants avec contrôleur actif — 10 novembre 2026

Banc temporaire retire apres consignation ; reconstruction locale sans diagnostic reussie : zero erreur et quatre avertissements preexistants. Bloc conserve pour son gain apparie ; System5 reste ouvert pour les performances et sa validation finale dans RML.

## Attribution SetMotor : CPU, appels et attentes — 10 novembre 2026
- [ ] Expliquer la divergence du profil avant toute nouvelle optimisation MediaEngine.

### Contrôle natif des attributions aux deux compteurs, demandé après les premières mesures
- [ ] Vérifier les attributions sans suspension CLR avant de choisir une nouvelle optimisation.
  - [ ] Après consignation, supprimer les diagnostics temporaires prévus ci-dessus, les fichiers ETL/ETLX et le dossier console, vérifier WPR arrêté et reconstruire sans le C# temporaire. Reporter la comparaison complète et la décision ici et dans la conversation ; ne pas appliquer d'optimisation fondée sur les anciens pourcentages non confirmés.

WPR CPU.Verbose.File refuse de démarrer (0xc5585011, politique de profilage système), malgré l'exécution hors bac ; WPR confirmé arrêté. Aucun changement de politique Windows, aucune capture noyau revendiquée. Les étapes d'analyse ETL non exécutables sont remplacées par les mesures directes suivantes, sans installation ni modification de fonctionnalité.

- [ ] Mesurer directement les deux compteurs dans la machine, avec calibration et exclusion de leurs callbacks.

### Nouvelle capture native limitée — 10 novembre 2026
- [ ] Confirmer les attributions sur une collecte moins volumineuse avant toute optimisation.
  - [ ] Compléter les rapports `20261110-motor-native-profile-minimal.{json,md}` et les validations System5 avec les pertes, piles manquantes et attributions exclusives/inclusives distinctes ; comparer aux mesures directes, puis appliquer le nettoyage déjà prévu.
La seconde demande d'élévation, pour la collecte allégée, se termine avant lancement : Start-Process renvoie « L'opération a été annulée par l'utilisateur ». Aucun fichier de seconde capture ni processus de diagnostic lancé par cette demande. La première capture reste disponible et ses résultats provisoires sont consignés. L'analyse définitive et le nettoyage restent ouverts en attendant la collecte de contrôle ; aucun nouvel enregistrement démarré par cette seconde tentative.
La dernière relance est terminale : l'élévation est annulée avant démarrage (session67362, exit1), aucun nouveau diagnostic lancé. Pas de relance automatique supplémentaire. La capture CPU initiale et ses pertes restent documentées ; ses44,8 % restent provisoires. Le choix du travail suivant repose aussi sur les comptes reproductibles et le code : NextClockEvent du8271 réarme QA à chaque cycle série, même entre deux bits ; il ne dépend pas d'une précision supposée de ce pourcentage. Nettoyage des seules sondes avant toute modification de production.
Nettoyage achevé : les deux sources temporaires, les dossiers console et les traces lourdes ont été supprimés après consignation. Services des compteurs et relais mécaniques sans diff fonctionnel par rapport à aecf9e9ba. Reconstruction Chips et Acorn :0 erreur/0 avertissement ; tests locaux :0 erreur/4 avertissements préexistants. WPR confirmé arrêté. La capture allégée reste non réalisée (aucune invite visible selon l'utilisateur) ; aucune relance automatique supplémentaire prévue.

## Mesure appariée du regroupement série — 10 novembre2026
Contrôles finaux sans banc temporaire :200 cas System/carte/relai8271 réussis en7 s, en plus des5 comparaisons de transferts et165 contrôles autonomes. Assemblies locales reconstruites après retrait du banc :0 erreur,4 avertissements préexistants. TRX `20261110-serial-event-integration.trx`. Aucun processus/cœur possédé ni capture WPR en cours. Le bloc d'optimisation est vérifié ; System5 n'est pas déclaré terminé.

## Mesure actuelle avec NOS80 réel — 10 novembre2026
Banc NOS80 retiré après ses trois collectes réussies ; assembly locale reconstruite sans ce C# :0 erreur,4 avertissements préexistants. Aucun changement src/tests par rapport à3d7726b8f. Captures catalogue et46 inspectées. Résultats détaillés dans la validation System5 ; candidat suivant identifié dans le raccordement des préfixes CPU aux événements lecteur lorsque le8271 est inactif et les broches de bus/NMI sont stables. Aucune optimisation de cette qualification encore réalisée ni gain promis. Les seuls documents restent en attente du prochain bloc utile, sans petit commit isolé.

## Indépendance CPU pendant la rotation après commande — 10 novembre2026
Validation finale sans banc temporaire :265 contrôles System/carte/relai8271 réussis en1m11 s (`20261110-idle-drive-integration.trx`), incluant les trois variantes de transfert avec nouvelles traces (un/dix secteurs, progressif et entrelacé), la garde NMI et l'attente d'index. Assemblies locales reconstruites sans diagnostic :0 erreur,4 avertissements préexistants. Aucun changement interne d'une puce, aucun outil de capture ni processus de test conservé ; les cœurs/images possédés ont été libérés. Bloc de coordination retenu pour son gain apparié, sans clôturer System5 ni annoncer les100 %.

## Temps par grandes étapes après7a3c4376a — 10 novembre2026

Relevé par étapes terminé : contrôle apparié réussi en37,848 s, rapports et deux validations complétés avec calibration et limites. Sources de production rétablies à7a3c4376a, diagnostics retirés. Reconstruction Acorn sans sonde :0 erreur/0 avertissement ; tests locaux sans banc :0 erreur/4 avertissements préexistants. Aucun gain de production nouveau revendiqué et aucun petit commit documentaire isolé.

## Portions indépendantes contenant mémoire et lectures VIA — 10 novembre2026
<<<<<<< HEAD
=======
- [ ] Réduire les reprises de coordination entre transactions dont les domaines restent indépendants.
>>>>>>> 435ea50b375ea7459340ccdff24940a7b7ea7cfa

Premier contrôle du découpage :23 cas privés réussis en1 s, dont les comparaisons au parcours scalaire et au découpage précédent, progressive/entrelacée, clavier, états VIA et notifications vidéo datées. Mesure native et transferts disque restent à faire.

<<<<<<< HEAD
=======
  - [ ] Enregistrer le bloc vérifié (coordination CPU/VIA, tests autonomes, validations et tableau) dans Git, sans captures ni banc temporaire ; compléter ce suivi après réussite du commit.
>>>>>>> 435ea50b375ea7459340ccdff24940a7b7ea7cfa

Reconstruction après retrait du banc natif : Acorn0 erreur/0 avertissement ; tests locaux0 erreur/4 avertissements préexistants. Le bloc de coordination est fonctionnellement vérifié ; la machine reste ouverte pour sa cadence.

Couverture du tableau vérifiée pour les blocs conservés : Y1 seul raccordé (8e769e853), portions vidéo linéaires (bfa9f8e14), périodes isolées VIA (26a88f94f), résolutions différées (09b6ced35), notifications disque avant phases CPU (aecf9e9ba), série8271 aux frontières observables (3d7726b8f), rotation indépendante pendant inactivité contrôleur (7a3c4376a), et nouvelles portions mixtes. Les règles générales et exemples restent dans les lignes existantes, avec chiffres et invariants ; aucune reprise de code des autres machines dans ce bloc.
<<<<<<< HEAD

Bloc mémoire/VIA enregistré dans Git après les270 contrôles et la reconstruction sans diagnostic. Les mesures de grandes étapes et la calibration sont incluses dans le même bloc cohérent ; aucune capture ni source temporaire ajoutée. System5 reste en cours, notamment pour la cadence pendant la rotation.
=======
>>>>>>> 435ea50b375ea7459340ccdff24940a7b7ea7cfa

## Coût des réponses périodiques du séparateur —10 novembre2026

### Candidat93 retiré après mesure native —10 novembre2026
La représentation de la dernière propagation sans sortie observée est retirée : trois paires NOS80/BASIC donnent33,15720→33,64582 ;33,19083→33,63758 ;33,01070→33,99626 ms/champ. Moyenne360 champs33,11958→33,75989, soit1,933 % plus lent. Moteur actif48,29542→49,64979 (+2,804 %), arrêté22,52825→22,64195 (+0,505 %). Aucun gain revendiqué ; Services93 rétablis exactement à30af8b3ee et banc natif supprimé. Les7776 comparaisons supplémentaires à l'oracle explicite sont conservées, indépendantes de la tentative. Rapports20261110-separator-tail-native.{json,md,trx}, contrôle natif réussi en1m22 s : CPU/RAM/vidéo/audio égaux et SSD inchangé ; cette équivalence n'a pas justifié de conserver le coût supplémentaire.

## Historique borné par les délais du séparateur —10 novembre2026

Borne candidate54 ns calculée depuis les profils en place (ripple34 ns et NAND max10 ns), sous descriptions CKA/CKB établies seulement ; fenêtre1000 ns conservée sinon.8 contrôles de phases réussis en821 ms :4031 séquences comparées au transport complet, dont1000 phases consécutives à1 ns avec pulses500 ns. Mesure native encore ouverte, aucun gain annoncé.

## Répartition complète du coût de carte —10 novembre2026

Sondes carte retirées : reconstruction Acorn0 erreur/0 avertissement, tests locaux0 erreur/4 avertissements préexistants. Prochaine correction limitée au calcul des échéances raccordées du93, sans changement du contrôleur ou de la machine.

## Réponses observées du93 sur la carte —10 novembre2026

Premier calcul93 :62 428→43 952 annonces IC7/champ, mais35,68105→36,22942 ms/champ (+1,537 %), moteur actif52,11793→53,83714 (+3,299 %). Rapports conservés sous20261110-connected-response-native-initial ; les tableaux de gain ne sont pas complétés pour ce résultat négatif. Coût du calcul d’échéance à reprendre dans93 avant décision de rétention.

Production restaurée à30af8b3ee : aucune des trois pistes de ce passage retenue (projection de queue, fenêtre courte, prédiction/cache des réponses). Cœurs/bancs/sondes possédés libérés et supprimés ; module Acorn reconstruit0 erreur/0 avertissement,29 contrôles93 réussis en951 ms et58 contrôles carte en16 s. Quatre avertissements locaux préexistants. Nouvelles couvertures conservées :7776 cibles nanoseconde,768 combinaisons graines/masques/phases et1000 phases denses du pulse500 ns en plus du balayage existant. Ce bloc documente des résultats négatifs et sécurise les prochaines refontes ; il ne clôture ni la cadence ni la machine. Enregistrement du bloc complet de validation après vérification finale.

Bloc de couverture et de diagnostic enregistré après les29 contrôles93 et58 contrôles de carte. Les huit fichiers comprennent les deux suites de régression et leurs suivis/résultats ; aucune modification de production ni sonde retenue. System5 reste en cours pour la cadence.

## QB masqué par QD dans le NAND du séparateur —10 novembre2026

Combinaison compteur/masque :31 contrôles de puce et58 contrôles de carte réussis, puis5 nouveaux cas de flux continus réussis en362 ms (295 séquences de24 pulses, espacements1/2/4/8/17 microsecondes, phases à17 ns et bascule d’observation pendant la séquence). Toutes les fenêtres datées et les comptes/niveaux finaux correspondent au transport complet. Mesure native ouverte ; aucun gain annoncé avant son résultat.

Premier passage du masque combiné :33,31193→30,34587 ms/champ (-8,904 %), moteur actif49,20792→42,09750 (-14,450 %), annonces IC7 62428→31960. Toutes les paires s’améliorent, équivalence CPU/RAM/vidéo/audio et SSD inchangé. Mesure finale après qualification du premier bit observé encore ouverte ; ces premières preuves sont conservées séparément.

Validation finale du masque QB :31 contrôles de puce et276 contrôles System/carte/8271 réussis, aucun banc temporaire conservé. Détails complets centralisés dans system5-validation.md ; fiches de composant/carte/projet liées au résultat plutôt que recopier les tableaux. Gain retenu8,060 % global,14,048 % moteur actif, aucun gain moteur arrêté ; les trois paires finales s’améliorent. Les solutions sont ajoutées dans le tableau existant. Enregistrement du bloc après vérification finale ; la machine reste ouverte pour sa cadence.

Bloc physique compteur93/NAND enregistré après validation finale. Arbre de travail à vérifier avant la prochaine action de coordination ; cadence System5 encore ouverte.

## Portions indépendantes bornées par la ligne —10 novembre2026

Mesure native terminée : trois paires360 champs,30,89337→28,44413 ms/champ (-7,928 %), arrêté23,11568→20,94980 (95,314 % nominal), actif42,42721→39,53510 (50,507 %).25 contrôles privés réussis en2 s ; banc temporaire supprimé. Validation complète sans banc en cours ; aucun changement de fréquence ou transaction CPU supprimée.

Validation finale :278 contrôles System/carte/8271 réussis en1m28 s après retrait du banc, puis25 contrôles privés renforcés réussis en1 s. Aucun changement de production depuis le relevé natif, hormis le commentaire explicatif. Module Acorn0 erreur/0 avertissement ; quatre avertissements préexistants dans les tests locaux. Aucune sonde ni processus possédé conservé. Résultats complets dans system5-validation.md et règle réutilisable dans la ligne existante du tableau. Bloc prêt pour enregistrement ; System5 reste ouvert pour sa cadence moteur actif.

Bloc de portions bornées par la ligne enregistré après validation et consignation. Les six fichiers réunissent raccordement, contrôles autonomes et résultats ; aucun banc ni capture ajouté au commit.

## Répartition actuelle de la carte après regroupement par ligne —10 novembre2026

Trois relevés terminés et sondes supprimées ; sources carte rétablies exactement àaae18f081. Le coût racine latch inclut surtout ses callbacks de reset, pas la seule table NAND. Nouveau travail défini ci-dessous ; reconstruction sans sondes avec ce bloc.

Candidat en cours dans le suivi93 : coalescer ses annonces d’échéance pendant une mutation d’entrée réentrante, sans retarder les callbacks de sorties ou descriptions.32 contrôles de puce et63 de carte passent ; le gain reste à mesurer.

## Coût des reconstructions sans observation —10 novembre2026

Fin du diagnostic :31 contrôles93 en1 s et63 contrôles carte en16 s sur la référence restaurée. Module Acorn0 erreur/0 avertissement, quatre avertissements préexistants dans les tests locaux. Aucun testhost possédé ni source de sonde/banc restant ; diff de production93 nul contreaae18f081. Les réponses reconstruites sans observation visées ne coûtent que0,08782 ms/champ sous instrumentation, contre1,86168 ms pour les regroupements acceptés ; surcharge du champ actif15,577 %, donc aucune extrapolation en temps CPU propre. Les deux candidats locaux sont retirés. Les six fichiers persistants comprennent les preuves documentées et une vérification autonome de deadline reçue ; pas de commit de seuls documents d’essai. System5 reste ouvert et la prochaine refonte doit porter sur la chaîne complète de propagation, pas une nouvelle variation du compteur.

## Répartition du champ complet après retrait du transport QA candidat —10 novembre2026

Validation finale des sources restaurées :32 contrôles93 réussis en1 s et63 contrôles carte en17 s après suppression de tous les bancs/sondes ; module Acorn0 erreur/0 avertissement, quatre avertissements locaux préexistants. Aucun testhost possédé restant et diff de production nul contreaae18f081. Les32 cas incluent le contrôle autonome d’annulation d’une montée QA avant propagation, conservé sans API expérimentale. Les candidats de transport n’ont apporté aucun gain et sont retirés ; la partition complète et le comparatif audio modifient le diagnostic, pas le fonctionnement matériel.
Vérification audio commune terminée : coût direct0,13915 ms/bloc moteur actif ; le profil natif18,55675 ms était déjà sans bruitages. Worker indépendant présent, mais verrou de rendu partagé avec les commandes mécaniques et horodatages émulés absents, sans contention réelle chiffrée. Détails et limites dans docs/project/mechanical-audio-runtime.md. Aucune modification de production App/SDK/MediaEngine. System5 reste ouvert pour la cadence et la suite doit viser la simulation physique ainsi que CPU/VIA et vidéo, à partir du profil exhaustif.

Bloc complet de diagnostic et d’audit enregistré après les vérifications32/63. Les neuf fichiers persistants comprennent les contrôles autonomes et les preuves documentées ; aucune source de production ni capture/banc temporaire dans le commit. System5 reste ouvert pour la cadence.

## Événements du connecteur de lecteur —10 novembre2026

Candidat évalué et retiré :63 contrôles carte réussis, trois paires natives120 champs égales fonctionnellement,31,69173→32,67212 ms/champ (+3,094 %), actif42,89909→44,96166 (+4,808 %). Aucun gain conservé. Banc supprimé, sources de production et tests rétablis àc1a362111. Module reconstruit0 avertissement/0 erreur et assembly local sans diagnostic reconstruit avec quatre avertissements préexistants. Résultats et conditions dans system5-validation.md et fdc400004.md ; pas de commit de seuls documents de cette tentative.

## Bilan demandé Debug/Release de System5 —10 novembre2026

Bilan mesuré sur la production conservée c1a362111 :1396 composants et191 intégrations par mode, aucun échec. Debug28,624 ms/champ moyen et40,781 actif ; Release3,875 moyen et5,763 actif. Aucun candidat de ce passage conservé. Les chiffres complets sont dans system5-performance-audit.md ; Code de production inchangé, bancs et sélection temporaire de sources retirés. Livraison Debug puis essai RML restent à effectuer avant décision de clôture.

## Stabilisation des mesures isolées Release —10 novembre2026

Livraison Debug réussie, exe et24/24 modules présents. Chauffe renforcée des21 scénarios isolés terminée en Debug29 s et Release6 s ; banc de nouveau supprimé, assemblies Chips.Tests reconstruits sans lui dans les deux modes. Les19 fiches et le bilan utilisent les médianes5 fenêtres après3,01M unités de chauffe ; min/max et variation résiduelle conservés, aucune causalité JIT supposée comme démontrée. Sources de production inchangées ; essai utilisateur RML restant avant clôture de System5.

## Essai RML de la livraison Debug —10 novembre2026
- [ ] Vérifier la livraison avec l’utilisateur qui effectue les manipulations.
  - [ ] Après réponse de l’utilisateur et observation, créer `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-rml-basic-print46.png` et modifier la validation avec le chargement BASIC, le calcul12+34, la saisie et le retour utilisateur sur le bruitage. Ne cocher qu’après résultat constaté.

## Défaut de saisie rapide observé dans RML —10 novembre2026
Le mot basic saisi par l’utilisateur ressort bc ; a,s,i sont perdus. Le maintien d’une touche précédente et l’absence de nouvelle transition AKD lors du chevauchement sont présents dans SystemKeyboard. Le manuel400,013 §2.1 décrit explicitement un nouvel encodage et une désactivation AKD de deux clocks256 kHz. Correction définie dans `docs/tasks/chips/SystemKeyboardOutput/implementation.md` avant modification. BASIC chargé, PRINT12+34=46, programme10 PRINT123456/20 END et RUN validés en saisie lente ; pavé7*6 et Entrée donne42. Aucune clôture de System5 ; bilan courant antérieur à la correction et inventaire à compléter avec SystemKeyboardRepeat et le contrôleur de sortie.

## Ralentissement FORM80 et retour au repos —10 novembre2026
- [ ] Établir puis corriger la cause sur les logiciels invités réels.
  - [ ] Modifier les fichiers responsables du défaut démontré et leurs fiches, après ajout au présent suivi des chemins et changements précis. Examiner notamment les échéances du8271, la qualification des portions CPU et les activités restantes de la carte, sans désactiver des fonctions matérielles pour obtenir la cadence.

## Copie et relecture réelles après FORM80 dans RML
- [ ] Vérifier la copie par le logiciel original et sa conservation après remontage.
  - [ ] Après manipulation et observation du remontage par l’utilisateur, créer `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-rml-infall-remounted.png` et modifier la validation avec CAT1/RUN INFALL depuis l’image remontée ; ne cocher qu’après résultat effectivement constaté.

## Passage à Atom demandé le11 novembre2026
L’utilisateur arrête les travaux et essais System5, demande de passer à Atom et de committer tout avant. Fonctionnement observé : DOS, BASIC4 Kio, System BBC BASIC16 Kio, FORM80/Verified, COPYF/CAT1/RUN INFALL. Correction clavier :12 contrôles isolés Debug/Release,195 intégrations System/carte dans chaque mode ; pas encore de livraison et saisie normale vérifiées dans RML. Écart écran chargé/effacé et cadence en écriture consignés, non corrigés. Primitives du caractère VDU :61 contrôles isolés réussis, non raccordées et non mesurées en machine. Code conservé comme in-progress selon la demande de sauvegarde complète ; arrêt des lignes de travail System5 restantes, sans les cocher comme réalisées. Les noms utilitaires des images de G:\Retro ne prouvent pas leur absence de jeux : ZOMBY/SPACEIN sont effectivement présents dans System3 Utilities, non lancés.
