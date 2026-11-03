# 8255 / 8255A — interface parallèle programmable

## Implantation existante et utilisation réelle

L’Acorn Atom instancie actuellement `AtomPpi8255` depuis `RetroMediaLab.Chips/IO/Intel/MCS80/8255/`. Ses accès B000–B003 utilisent les trois ports et le registre de commande. A sélectionne les lignes du clavier et le mode vidéo ; B reçoit les colonnes du clavier ; C reçoit les entrées de la carte et pilote notamment cassette, haut-parleur et choix des couleurs. Le nom Atom n’a donc pas à désigner le composant générique dans Chips : ce câblage reste dans le module Acorn.

Le composant initial possède trois latches de sortie, le mot de commande 9B au reset, les directions du mode 0 et les commandes BSR. Une écriture de sélection des modes avec les bits 64 actifs lève `NotSupportedException` : les modes 1 et 2 ne sont pas émulés. Aucun strobe périphérique, masque de pilotage trois états, sortie INTR, inhibition sous RESET maintenu ou cycle physique CS/RD/WR n’est exposé. Le test autonome préexistant ne vérifie qu’une configuration statique de directions et quelques BSR.

## Références examinées

- [National Semiconductor INS8255, avril 1978](https://datasheet4u.com/pdf/1529854/INS8255.pdf) : 12 pages ; brochage et reset p. 5, registres et BSR p. 6, mode 0 p. 7, entrée strobed p. 8, sortie strobed p. 9, bidirectionnel p. 10 et tableau des fonctions des broches p. 12. Ce composant est nommé dans le manuel Atom.
- [Intel 8255A-5, référence 231308-004, septembre 1993](https://deramp.com/downloads/intel/8255.pdf) : scans lus, table du bus p. 3, commande/reset p. 5–6, entrée mode 1 p. 10–11, sortie p. 12, mode 2 p. 13–14, statut C p. 16–17, caractéristiques et chronogrammes p. 19–24. La révision distingue les caractéristiques électriques de vitesse ; elle ne justifie pas d’importer les fonctions de lecture de commande propres à des dérivés CMOS.
- [Acorn Atom Technical Manual](https://www.vintageacorn.com/Acorn_AtomTechnicalManual.pdf) : nomenclature IC25 INS8255 ; fonctions de port C et description clavier/cassette dans la description de la carte. Le CPU est cadencé à 1 MHz ; le PPI ne possède pas d’horloge autonome à accélérer.
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

Les 18 tests d’intégration Atom passent (401 ms), dont un nouveau test observant l’horloge cassette native sur PC4 lorsqu’elle sert de STB A. Les 30 cas autonomes PPI passent après correction d’un défaut de lecture sous RESET : le bus retournait encore l’octet échantillonné si le fournisseur d’entrée activait RESET pendant la lecture ; il revient désormais en haute impédance. Le dernier passage ciblé dure 67 ms. Un test supplémentaire a reproduit l’effacement indu d’une INTR forcée par BSR lors d’un événement sans rapport. La correction limite le recalcul au groupe réellement concerné ; les broches GPIO libres, les BSR libres et les accès à l’autre groupe préservent cette interruption jusqu’à son propre acquittement.

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

Un programme ROM de diagnostic est exécuté par le 6502 avec le bus, l’horloge et la vidéo Atom. Un 8255 de banc est explicitement décodé en A000–A003 ; ce montage temporaire permet de piloter STB/ACK et les données indépendamment des signaux cassette/clavier de la carte native. Il n’est pas présenté comme un périphérique Atom commercialisé ni ajouté au module. Le PPI natif conserve la vidéo en mode texte ; son raccordement et ses entrées sont testés séparément par les 18 tests d’intégration.

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
