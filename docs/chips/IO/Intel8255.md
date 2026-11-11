# 8255 / 8255A — interface parallèle programmable

## Revue Atom — 11 novembre 2026

### Reprise après commit6502

Chemins relus : `Ppi8255.cs`, `.Bus.cs`, `.Handshake.cs`, `.Ports.cs`. Les prescriptions et références constructeur détaillées ci-dessous sont réutilisées ; aucune règle issue d'un autre émulateur ne fixe une attente matérielle. Correspondances de tests autonomes dans `tests/RetroMediaLab.Chips.Tests/IO/Intel/MCS80/8255/` :

| Domaine | Contrôles existants réutilisables |
|---|---|
| Mode0, directions et GPIO | `Ppi8255BasicTests` :16 configurations×256 valeurs, écritures/entrées séparées, BSR et bits ignorés, tous mots de mode |
| Bus et reset | Tests RD maintenu, capture à la fin WR, CS/combinaisons illégales, reset pendant écriture ou lecture par fournisseur, callbacks relisant l'état complet |
| Modes1 A/B | `Ppi8255HandshakeTests` : STB/IBF/INTR/INTE, ACK/OBF, acquittements sur fronts distincts et indépendance des groupes |
| Mode2 | Six ordres matériels permis dans `Ppi8255BusTests`, deux encodages du mode dans `Ppi8255HandshakeTests`, deux requêtes et pilote A limité à ACK bas |
| C/BSR | Accès aux statuts documentés, INTR forcées conservées devant événement sans rapport, GPIO supérieurs nécessitant BSR en mode strobed A |

Redondance établie à examiner : `EndWrite(PortC)` ne modifie que `_outputC` sur les GPIO autorisés, après le contrôle RESET ; `BeginWrite(PortC)` n'arme aucun handshake. Aucune entrée STB/ACK, direction, donnée A/B ou INTR n'est modifiée par cette écriture. Pourtant elle appelle la publication complète qui reconstruit A/B/C et les deux interruptions. Une publication C analogue à celles de A/B peut conserver son état complet avant callback et sa notification de transaction, sans recalculer les ports invariants. Les changements de mode, handshakes et BSR restent dans leur parcours complet. Le gain reste à mesurer avant rétention ; aucun gain Atom déduit de cette seule lecture.

### Résultat de la publication C limitée au port concerné

`Ppi8255.cs` appelle désormais `PublishPortC` pour cette seule écriture GPIO. Le helper conserve la même expression de valeur/masque, les actualise avant le premier callback, ne notifie DriveChanged qu'en cas de changement et conserve PinsChanged pour chaque transaction, même identique. L'examen du chemin établit l'invariance des autres ports et des deux INTR ; la publication complète reste utilisée pour les causes qui peuvent les modifier. Technique déjà prescrite par le tableau commun : limiter les publications aux sorties réellement concernées, sans retirer de transaction ni activité matérielle.

Quatre paires de processus Debug en ordre alterné, sans build/test concurrent, .NET10 ordinaire. Six commandes, avec ou sans les observateurs A/B/C de transaction et de pilotage ;100000 écritures C de chauffe, cinq fenêtres de1000000 écritures variables par processus. Les lectures de contrôle et états sont hors temps. Les callbacks observés comptent leurs appels et cumulent valeurs/masques ; leur coût est inclus.

| Commande / mode | Sans observateur, avant→après ns/écriture | Réduction médiane appariée | Avec observateurs, avant→après ns/écriture | Réduction médiane appariée |
|---|---:|---:|---:|---:|
| 80, mode0 sorties | 41,37→23,23 | 43,62 % | 47,32→32,08 | 31,94 % |
| 8A, mode0 câblage logique Atom | 40,94→23,43 | 43,42 % | 47,91→31,07 | 35,14 % |
| A0, A mode1 sortie | 59,57→38,34 | 36,12 % | 65,88→46,39 | 29,67 % |
| B0, A mode1 entrée | 61,55→37,81 | 38,58 % | 66,43→46,90 | 28,42 % |
| C0, A mode2 | 59,12→36,64 | 37,78 % | 64,56→44,52 | 31,58 % |
| 84, B mode1 sortie | 47,83→30,61 | 36,14 % | 52,50→35,18 | 32,87 % |

Les durées sont les médianes des quatre médianes de cinq fenêtres ; les pourcentages sont calculés par paire, pas depuis le quotient de ces colonnes. Toutes les paires sont plus rapides dans ces charges : réduction comprise entre23,77 et47,08 % selon le passage et le scénario. Les240 fenêtres appariées conservent lectures A/B/C, masques, interruptions, commande, comptes et sommes des notifications. Ces sommes ne sont pas une trace exhaustive d'ordre ; les tests de fronts restent distincts. Le banc maintient STB/ACK hauts, INTE désactivées ; ses écritures C ne mesurent pas le débit d'un protocole strobed actif ni les callbacks réels du module Atom. Le PPI n'a pas d'horloge : ns/écriture est un coût hôte, pas une fréquence matérielle modifiée.

**30 tests PPI Debug et30 Release réussis**, zéro échec/ignoré, hors états ; avertissement xUnit2031 partagé préexistant. Les matrices déjà conservées vérifient les mots de mode, directions, BSR, statuts et handshakes ; aucun nouveau test miroir du helper ajouté. Rapports `artifacts/TestsChips/Intel8255/AcornAtom/20261111-c-publication-{debug,release}.trx`, fichiers des quatre paires et `20261111-c-publication-summary.json` dans le même dossier. Aucun gain Atom complet annoncé, aucun build d'application ni nouvelle validation de carte dans ce passage.

Les quatre captures historiques référencées plus bas sont présentes et inspectées :48P, sans F, dans les scénarios indiqués. Ce sont les résultats invités du3 novembre, réutilisés comme tels, pas de nouvelles captures après optimisation. Les limites électriques et le domaine des conditions légales documentés restent explicites ; l'optimisation ne change aucun comportement de mode ni choix de variante. Le composant reste entièrement événementiel, sans Tick/oscillateur ni sondage périodique ajouté. Candidat conservé après comparaison ; banc temporaire et copies de référence supprimés après terminaison code zéro des mesures. Revue8255 vérifiée dans le domaine numérique et les conditions légales décrits ; commit du bloc avant le VIA. Les limites électriques ne sont pas présentées comme une simulation analogique réalisée.

Production inchangée. **30 cas propres au PPI passent en Debug et 30 en Release**, zéro échec/ignoré, hors états. Rapports `artifacts/TestsChips/AcornAtomReview/20261111-chips-{debug,release}.trx`. Les matrices et chronologies couvertes restent celles décrites ci-dessous ; les preuves invitées historiques ne sont pas réattribuées à un nouveau diagnostic matériel.

Banc isolé actuel, mode 0, commande 8A, C reçu A0, sans observateurs de sortie : 1 million d'accès de chauffe puis cinq fenêtres de 1 million, médiane avec boucle. Lecture C **19,1505 ns Debug / 1,3778 ns Release** ; écriture A changeante **21,5978 / 11,8858 ns**. La boucle de lecture inclut un accumulateur ; l'écriture change les niveaux sans destinataire, donc ne mesure pas le câblage Atom. Paramètres ordinaires .NET 10 ; aucune option Debug modifiée. Données `artifacts/TestsChips/AcornAtomReview/20261111-speed-{Debug,Release}.json`. Pas de mesure actuelle des performances strobed ni de gain avant/après démontré.

Le PPI ne possède pas de Tick ni d'oscillateur : l'avance aux échéances ne doit pas lui en inventer un. Les notifications de niveaux/directions sont déjà conditionnelles ; les captures sur accès bus et handshakes demeurent nécessaires. Les limites électriques ci-dessous restent ouvertes à toute nouvelle conclusion de fidélité. [Bilan de revue](../../tasks/machines/AcornAtom/chip-review.md).

## Implantation existante et utilisation réelle

L’Acorn Atom instancie actuellement `AtomPpi8255` depuis `RetroMediaLab.Chips/IO/Intel/MCS80/8255/`. Ses accès B000–B003 utilisent les trois ports et le registre de commande. A sélectionne les lignes du clavier et le mode vidéo ; B reçoit les colonnes du clavier ; C reçoit les entrées de la carte et pilote notamment tape, haut-parleur et choix des couleurs. Le nom Atom n’a donc pas à désigner le composant générique dans Chips : ce câblage reste dans le module Acorn.

Le composant initial possède trois latches de sortie, le mot de commande 9B au reset, les directions du mode 0 et les commandes BSR. Une écriture de sélection des modes avec les bits 64 actifs lève `NotSupportedException` : les modes 1 et 2 ne sont pas émulés. Aucun strobe périphérique, masque de pilotage trois états, sortie INTR, inhibition sous RESET maintenu ou cycle physique CS/RD/WR n’est exposé. Le test autonome préexistant ne vérifie qu’une configuration statique de directions et quelques BSR.

## Références examinées

- [National Semiconductor INS8255, avril 1978](https://datasheet4u.com/pdf/1529854/INS8255.pdf) : 12 pages ; brochage et reset p. 5, registres et BSR p. 6, mode 0 p. 7, entrée strobed p. 8, sortie strobed p. 9, bidirectionnel p. 10 et tableau des fonctions des broches p. 12. Ce composant est nommé dans le manuel Atom.
- [Intel 8255A-5, référence 231308-004, septembre 1993](https://deramp.com/downloads/intel/8255.pdf) : scans lus, table du bus p. 3, commande/reset p. 5–6, entrée mode 1 p. 10–11, sortie p. 12, mode 2 p. 13–14, statut C p. 16–17, caractéristiques et chronogrammes p. 19–24. La révision distingue les caractéristiques électriques de vitesse ; elle ne justifie pas d’importer les fonctions de lecture de commande propres à des dérivés CMOS.
- [Acorn Atom Technical Manual](https://www.vintageacorn.com/Acorn_AtomTechnicalManual.pdf) : nomenclature IC25 INS8255 ; fonctions de port C et description clavier/tape dans la description de la carte. Le CPU est cadencé à 1 MHz ; le PPI ne possède pas d’horloge autonome à accélérer.
- [MAME i8255.cpp](https://github.com/mamedev/mame/blob/master/src/devices/machine/i8255.cpp) : modes et affectation des broches C, lecture des statuts INTE, BSR et rappels des ports. Certaines opérations y sont atomiques ; la ligne initialisant IBF A en sortie contient elle-même un commentaire d’incertitude pour un périphérique SAM Coupé. Ces accommodements ne remplacent pas les chronogrammes constructeur.
- [86Box kbc_xt.c](https://github.com/86Box/86Box/blob/master/src/device/kbc_xt.c) : intégration PPI/clavier/haut-parleur du PC XT, centrée sur le câblage PC. Ce code n’est pas une implémentation générique complète des trois modes et ne doit pas fournir les règles de carte Atom.

## Fonctions numériques implémentées et vérifiées

| Domaine | Comportement établi par les fiches |
|---|---|
| Bus | CS actif bas, A1/A0 sélectionnant A/B/C/commande, RD et WR actifs bas ; commande uniquement en écriture. Une lecture du registre de commande est une condition illégale, sans valeur de registre lisible à inventer. RESET actif haut libère les ports et inhibe les transferts. |
| Mode 0 | A/B à huit bits ; C haut et bas programmables indépendamment. Sorties mémorisées, entrées non latchées. Données d’écriture valides à la fin de WR ; entrée statique visible pendant RD. Le changement de mode efface latches de sortie et statuts. |
| Entrée mode 1 A | PC4=STB, PC5=IBF, PC3=INTR. Le latch est chargé pendant STB bas, capture à sa fin ; IBF monte sur STB bas. INTR dépend de STB haut, IBF et INTE. RD bas acquitte INTR ; RD haut efface IBF. |
| Entrée mode 1 B | Même séquence avec PC2=STB, PC1=IBF et PC0=INTR. A et B possèdent leurs états indépendants et peuvent fonctionner dans des modes différents. |
| Sortie mode 1 A | PC7=OBF actif bas, PC6=ACK actif bas, PC3=INTR. WR bas acquitte INTR ; WR haut mémorise la donnée et abaisse OBF. ACK bas relève OBF ; INTR exige ACK haut, OBF haut, INTE et WR relâché. |
| Sortie mode 1 B | Même séquence avec PC1=OBF, PC2=ACK, PC0=INTR. Les bits de C non réservés conservent leurs fonctions E/S suivant les directions et restrictions d’accès constructeur. |
| Mode 2 | A bidirectionnel ; PC3=INTR, PC4=STB, PC5=IBF, PC6=ACK, PC7=OBF. Pilote A actif seulement pendant ACK bas. Les chemins entrée et sortie ont leurs activations distinctes et leur réunion produit INTR. B reste utilisable en mode 0 ou 1. |
| BSR / INTE | BSR sélectionne l’un des huit bits C et set/clear, indépendamment des bits non utilisés du mot BSR. En mode 1, INTE A dépend de PC4 en entrée ou PC6 en sortie, INTE B de PC2 ; mode 2 avec deux activations séparées sur PC4/PC6. Les statuts C lisent INTE à la place des entrées physiques de handshake correspondantes. |
| Reset / directions | Toutes les lignes deviennent entrées au reset ; les niveaux externes restent sous responsabilité de la carte. Une notification doit permettre de distinguer une ligne libérée d’une sortie haute. Un callback ne doit pas relire un état partiellement mis à jour. |

Les validations autonomes couvrent les mots de mode, directions, bits BSR, groupes et combinaisons décrits ci-dessous. Les preuves invitées utilisent le 6502 et le rendu Atom avec des niveaux de banc explicitement identifiés pour exercer les signaux indépendamment du câblage natif.

## État des vérifications

Le composant a été réécrit sous le nom générique `Ppi8255` et raccordé à l’Atom. Les broches C sont actualisées lors des changements d’entrée, du reset et, lorsque des modes strobed sont sélectionnés, au rythme du câblage de carte ; le mode 0 évite ce traitement de handshake. La sortie INTR du PPI n’est pas ajoutée arbitrairement à l’IRQ CPU.

Les 18 tests d’intégration Atom passent (401 ms), dont un nouveau test observant l’horloge tape native sur PC4 lorsqu’elle sert de STB A. Les 30 cas autonomes PPI passent après correction d’un défaut de lecture sous RESET : le bus retournait encore l’octet échantillonné si le fournisseur d’entrée activait RESET pendant la lecture ; il revient désormais en haute impédance. Le dernier passage ciblé dure 67 ms. Un test supplémentaire a reproduit l’effacement indu d’une INTR forcée par BSR lors d’un événement sans rapport. La correction limite le recalcul au groupe réellement concerné ; les broches GPIO libres, les BSR libres et les accès à l’autre groupe préservent cette interruption jusqu’à son propre acquittement.

Les boucles et cas couvrent les 16 configurations de directions du mode 0 avec les 256 valeurs, tous les mots de mode, tous les BSR et leurs bits ignorés, les deux groupes en entrée/sortie strobed, la lecture des INTE, l’acquittement séparé sur les fronts RD/WR, les pilotes A trois états et les six ordres permis du mode 2 (WR avant ACK, STB avant RD). Ils vérifient aussi le reset pendant une écriture, les lectures d’entrées statiques non latchées et les callbacks relisant l’état complet après reset.

La suite complète Chips réussit avec 1 300 tests en Debug (850 ms) et Release (1 s), sans échec ni test ignoré. Le dernier passage d’intégration Atom réussit avec 18 tests en 468 ms, après la correction BSR. Le build final est vérifié ci-dessous ; les fichiers temporaires sont retirés avant le commit.

## Banc initial avant modification

Les trois fichiers initiaux sont figés dans un namespace isolé sous `artifacts/.tmp-i8255/Benchmark/Original/`. Le banc configure A en sortie, C haut en entrée et C bas en sortie, sans notification de sortie ; trois millions d’accès par série, médiane de cinq séries après la première écartée. Avant toute modification fonctionnelle :

| Configuration | Lecture C initiale / actuelle (ns) | Écriture A initiale / actuelle (ns) |
|---|---:|---:|
| Debug | 20,325 / 18,640 | 8,654 / 8,957 |
| Release | 3,442 / 3,514 | 0,321 / 2,559 |

Lors de ce passage initial, les deux classes avaient encore le même code fonctionnel et relisaient BF en A à la fin. Les écarts de ce passage, notamment l’écriture Release, montrent l’effet possible des appels délégués et des optimisations du JIT sur ce microbanc ; ils ne représentent aucun gain ni perte fonctionnelle. Les mesures finales devront être interprétées avec cette limite et avec des observations de l’intégration réelle.

## Mesures pendant la réécriture

| Passage | Lecture C initiale / nouvelle (ns) | Écriture A initiale / nouvelle (ns) |
|---|---:|---:|
| Debug avant réduction des publications | 18,296 / 21,569 | 7,986 / 69,685 |
| Debug après traitement du seul port concerné | 17,680 / 21,016 | 7,921 / 24,327 |
| Release, même version | 3,155 / 3,703 | 0,560 / 4,975 |
| Debug final, après indépendance BSR | 18,335 / 20,597 | 7,598 / 23,617 |
| Release final, après indépendance BSR | 3,558 / 3,718 | 0,449 / 4,499 |

Le mot final A reste BF dans les deux versions. La réduction supprime des traitements hôte redondants ; elle ne supprime ni mode ni front matériel et ne change pas les options de compilation Debug. Le coût reste supérieur à celui du composant statique incomplet, et le microbanc ne démontre pas à lui seul la vitesse d’une machine complète.

## Preuves invitées réellement exécutées

Un programme ROM de diagnostic est exécuté par le 6502 avec le bus, l’horloge et la vidéo Atom. Un 8255 de banc est explicitement décodé en A000–A003 ; ce montage temporaire permet de piloter STB/ACK et les données indépendamment des signaux tape/clavier de la carte native. Il n’est pas présenté comme un périphérique Atom commercialisé ni ajouté au module. Le PPI natif conserve la vidéo en mode texte ; son raccordement et ses entrées sont testés séparément par les 18 tests d’intégration.

Les adresses du banc exposent les niveaux externes, les sorties INTR et le masque de pilotage. Le CPU invité écrit les commandes, compare les lectures et écrit lui-même P ou F dans la RAM écran. Il efface également la RAM vidéo avant affichage. La capture est encodée à partir du tampon réellement rendu par le MC6847 avec la ROM `MC6847.bin` existante, sans verdict dessiné par l’hôte.

| Capture locale hors Git | Verdicts et couverture |
|---|---|
| `artifacts/TestsChips/Intel8255/AcornAtom/basic-ports-reset.png` | 12 PASS : reset, directions et lectures A/B, latches de sortie, BSR set/clear PC7, directions mixtes de C, commande non lisible, libération des pilotes et écritures inhibées sous RESET. |
| `artifacts/TestsChips/Intel8255/AcornAtom/strobed-inputs.png` | 12 PASS : groupes A/B en entrée, IBF à STB bas, absence d’interruption avant sa fin, capture mémorisée, acquittement par lecture, statuts INTE relus à la place des broches STB. |
| `artifacts/TestsChips/Intel8255/AcornAtom/strobed-outputs.png` | 12 PASS : groupes A/B en sortie, état prêt initial, OBF et requête après écriture, ACK bas puis haut et lecture de la donnée de sortie. |
| `artifacts/TestsChips/Intel8255/AcornAtom/bidirectional.png` | 12 PASS : A libéré hors ACK, pilotage pendant ACK, chemins de données et requêtes indépendants, lecture conservant une sortie prête, effacement lors du changement de mode. |

Les quatre images ont été ouvertes et relues après le dernier passage : 48 PASS. Leurs pixels sont ceux du rendu Atom, pas une preuve de toutes les caractéristiques analogiques du composant. Les chronologies CS/RD/WR, bus non sélectionné et reset au milieu d’un transfert sont vérifiées par les tests de signaux autonomes.

## Limites du modèle

Le modèle représente les états numériques et les fronts des signaux, pas les tensions, la contention électrique ni les délais analogiques de propagation, setup/hold et charges capacitives. Les minima/maxima électriques Intel et National sont conservés dans les références d’audit ; aucune valeur nominale inventée ne leur est substituée. Le câblage appelant fournit des accès respectant ces contraintes. La puce ne possède pas d’oscillateur indépendant : aucune fréquence ou attente artificielle n’a été ajoutée.

Les commandes et handshakes documentés sont réalisés ; les conditions déclarées illégales par le constructeur ne reçoivent pas un comportement logiciel inventé. Le bus est libéré pour les lectures de commande et les transferts invalides. Les opérations atomiques de la carte résolvent un bus non piloté à FF, sans prétendre qu’il s’agit d’une valeur lisible du registre de commande.

La lecture du mot de commande propre à certains dérivés CMOS et les contournements spécifiques de MAME pour d’autres machines ne font pas partie du 8255/INS8255/8255A NMOS traité ici. Les noms Atom ont été retirés du composant, de ses constantes et de son interface ; le raccordement machine reste dans le module Acorn.

## Livraison Debug

Le script `scripts/local-building.cmd --building=debug --modules=A` est terminé avec code zéro après la correction finale BSR. Les 24 manifestes ont chacun leur DLL d’entrée présente. L’exécutable est `build/Debug/Retro Media Lab/retromedialab.exe` (345 600 octets), avec `lib/retromedialab.chips.dll` (409 088 octets). Cette DLL correspond au fichier compilé, SHA-256 `3FC7C3BB251BC86E091019DDF70CBDC9596D280B68ED1054AD3A5733064D4984`.

Les références téléchargées, bancs, programmes invités et journaux temporaires sous `artifacts/.tmp-i8255/` sont supprimés après usage. Les tests conservés sont autonomes ; le nouveau test de raccordement Atom n’utilise aucun fichier externe. Les quatre PNG restent ignorés par Git et ne sont pas inclus dans le commit.

## 8 novembre 2026 — revue statique des échanges

Le code actuel du Ppi8255 couvre les groupes indépendants, les modes 0/1/2, les fronts STB/ACK/RD/WR, BSR/INTE, reset et masques des pilotes ; il n'a pas de Tick ni d'oscillateur artificiel. Les lectures de fournisseurs correspondent aux accès bus ou à la capture d'une entrée strobed, pas à une boucle d'interrogation périodique. Les sorties DriveChanged comparent déjà niveaux et directions avant notification. Les callbacks PinsChanged conservent aussi l'observation des transactions, distincte d'une transition électrique.

Le raccordement Atom employait PinsChanged pour ses fils A/C, et recalculait sélection clavier, mode/couleur vidéo et sortie Tape lors des écritures identiques. Il utilise désormais les callbacks DriveChanged existants. Les niveaux reçus restent identiques (sorties libérées résolues haut) ; le reset initialise explicitement A/C même si les pilotes étaient déjà libérés. Les contrats et modes du PPI sont conservés. La vidéo poursuit son balayage et le générateur Tape ses fronts indépendamment des écritures CPU.

Revue et modification de source seulement : aucun test/build supplémentaire ni gain global mesuré pendant ce passage. La description initiale d'AtomPpi8255 et des modes manquants en tête de fiche décrit l'état historique avant la réécriture, pas le composant actuel.
