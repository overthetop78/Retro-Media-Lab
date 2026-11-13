# ULA vidéo BBC — circuit 5C094 / VIDPROC

## Périmètre et références

Le VIDPROC est isolé dans `src/RetroMediaLab.Chips/Video/Acorn/BbcMicro/VideoULA/Services/BbcVideoUla.cs` et ses fichiers partiels. Il ne référence aucun modèle de CRTC ni de générateur télétexte. La carte BBC qui assemble ces circuits, traduit les adresses RAM et possède le framebuffer est dans `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/Common/Services/Video.cs` ; ses constantes de câblage restent dans le module. Les deux autres générateurs ont leurs propres fiches et restent distincts.

Référence principale : [Acorn, BBC Microcomputer Service Manual, section 3.3 et schéma](https://acorn.huininga.nl/pub/unsorted/manuals/BBC%20Microcomputer%20Service%20Manual-HTML/BBCServiceManual.html), confrontée au [schéma BBC](https://chrisacorns.computinghistory.org.uk/docs/Acorn/Manuals/Acorn_BBCCctDiagram.pdf). Le schéma IC6 montre D0–D7, A0, sélection CS, horloge 16 MHz, sorties 8/4/2/1 MHz et CRTC CLK, DISEN, CURSOR, INVERT, entrées RGB SAA5050 et sorties RGB. Le lien S26 sélectionne la vidéo normale/inversée. La ROM, le calcul d'adresse RAM et le PAL encodeur ne sont pas internes à cette ULA.

Comparaisons : [b2 VideoULA.cpp](https://github.com/tom-seddon/b2/blob/master/src/beeb/src/VideoULA.cpp), [B-Em video.c](https://github.com/stardot/b-em/blob/master/src/video.c), [JSBeeb video.js](https://github.com/mattgodbolt/jsbeeb/blob/main/src/video.js), [MAME bbc_v.cpp](https://github.com/mamedev/mame/blob/master/src/mame/acorn/bbc_v.cpp). Les extensions VideoNuLA de ces projets ne sont pas des fonctions de la puce d'origine et ne doivent pas être ajoutées par confusion. Le [remplacement matériel ouvert de Gert van der Knokke](https://github.com/gertk64/BBC_Video_ULA) fournit également les équations de ses GAL ; il s'agit d'une réalisation de remplacement, pas du masque Ferranti original. Ses différences de curseur ne sont pas reprises comme preuve du silicium.

## Écarts établis avant reprise

| Fonction | État initial | Vérification à réaliser |
|---|---|---|
| Deux registres | A0 distingue contrôle/palette, toutes les autres adresses sont miroirs | Les 256 valeurs et les alias ; aucune lecture de registre inventée |
| Contrôle | Flash, télétexte, fréquence CRTC et mode existent sous forme de rendu | Bits indépendants, commutation pendant le balayage et état initial |
| Palette | 16 entrées et complément RGB présents | 16×16 écritures, les deux phases flash et sélection sans modifier la RAM |
| Sérialisation | Table de 8 pixels avec un mode ramené par Math.Min | Octet décalé vers la gauche, insertion de 1, prélèvements 7/5/3/1 ; toutes les combinaisons de cadence CRTC et pixels, y compris non standard |
| Horloges | Tick CPU 2 MHz, pas de sorties divisées propres | Divisions 16 MHz vers 8/4/2/1 et CRTC 1/2, fronts maintenus, continuité au changement de contrôle |
| Écriture mi-cellule | La cellule lente entière est dessinée en une fois | Une palette/commande écrite entre deux cycles CPU agit seulement sur les pixels restants |
| Curseur | Tableau de largeur continue, zéro pour bits 101 | Segments : bit7 premier caractère, bit6 second, bit5 troisième et quatrième ; les huit motifs, y compris celui avec un trou, et passage après DE |
| Télétexte | RGB SAA5050 natif mais curseur ajouté par CursorForCell | Multiplexage RGB dans l'ULA, curseur commun appliqué aux mêmes positions, sans recréer le SAA5050 |
| DISEN / INVERT | Blanking et inversion globale non exposés comme entrées | Sortie hors fenêtre, niveau du lien normal/inversé, interaction curseur et source |
| Adresse vidéo | Choix télétexte selon contrôle ULA et base 7C00 fixe | Le schéma choisit la traduction RAM par MA13 ; MA11 et le modèle affectent 3C00/7C00, RA0–2 et défilement matériel dans la voie bitmap |
| RA3 | Suppression bitmap quand RA>=8 | Le circuit de blanking externe doit être confronté au schéma, sans le déduire de la taille d'une police |
| Sorties | Frame immuable 640×256 ou 200, monochrome calculé | Garder ces contrats de présentation, séparer leur conversion des sorties RGB matérielles |

Le manuel Acorn décrit le chargement des données à la fin de l'accès RAM CRTC (250 ns). b2 et B-Em réalisent le même registre à décalage à insertion de 1, tandis que leurs chemins de curseur diffèrent du tableau de largeur simplifié actuellement présent dans RML. JSBeeb documente explicitement l'effet des écritures sur la deuxième moitié d'une cellule à 1 MHz. Les divergences ne sont pas encore corrigées ni annoncées conformes à ce stade.

## Couverture et preuves attendues

Les vérifications autonomes doivent prélever les sorties du composant sur des horloges et entrées synthétiques, avec un résultat indépendant de sa table interne : palette/flash, 256 octets dans les huit combinaisons de cadence, segments de curseur, source RGB, blanking, inversion, reset et changements de registre pendant une cellule. L'intégration native devra vérifier les adresses lues, le défilement, les deux moitiés d'une cellule lente et l'absence de modification rétroactive des frames publiées. Un programme invité BBC devra produire les cartes et verdicts contrôlés, conservés uniquement dans `artifacts/TestsChips/BbcVideoUla/<Machine>/`. Aucun test ni débit nouveau n'est annoncé par cette fiche initiale.

## Circuit autonome réalisé et premiers résultats

`BbcVideoUla` et son interface sont maintenant séparés de `Video`, qui demeure le propriétaire du CRTC, SAA5050 et framebuffer. Le nouveau composant porte la palette brute et sa conversion, les quatre prises du registre à décalage, l'insertion de 1, les cadences 2/4/8/16 MHz et CRTC 1/2 MHz, les trois segments indépendants de curseur et le multiplexage RGB. Les changements de palette, contrôle, RGB télétexte, DISEN et inversion recalculent les sorties existantes ; ils ne modifient pas les pixels précédemment publiés par une carte.

`Write` représente l'écriture de bus déjà sélectionnée et synchronisée par le propriétaire ; il ne prétend pas simuler les temps analogiques de setup/hold des broches CPU. `SetMasterClock` préserve les fronts et diviseurs ; la phase zéro est une origine numérique relative, à aligner aux accès RAM lors du raccordement natif. `Reset` est une mise sous tension déterministe du logiciel, pas une broche RESET du boîtier. L'entrée logique `Inverted` exprime le choix normal/inversé du lien de carte, sans lui attribuer arbitrairement une tension électrique.

**20/20 tests autonomes passent en Debug et Release**. Les huit cas de sérialisation parcourent chacun les 256 octets et deux périodes caractère, avec attente calculée par position des bits source et remplissage à 1, indépendamment de la table existante. Les tests palette parcourent 16 entrées × 16 codes × deux phases flash. Les huit motifs de curseur sont vérifiés au pas de chaque pixel dans les deux fréquences CRTC ; le cas 101 conserve son trou. Les diviseurs et niveaux maintenus, le changement de sélection CRTC sans reset, les données remplacées à mi-cellule, une palette écrite après huit périodes 16 MHz, le blanking et le multiplexage sont couverts. Le propriétaire BBC utilise encore son ancien circuit interne : ces résultats autonomes ne prouvent donc pas le raccordement natif ni la livraison finale.

## Raccordement natif et écarts corrigés

`Video` pilote maintenant le VIDPROC pendant huit périodes 16 MHz par cycle CPU, et le SAA5050 pendant six transitions de demi-point réparties sur ces périodes. Le CRTC avance à la fin du caractère, après les deux moitiés lorsqu'il tourne à 1 MHz. Le recadrage télétexte de présentation tient compte du changement d'origine du propriétaire ; les délais propres du SAA5050 restent inchangés. Le curseur vient de la broche CURSOR du CRTC, et non d'une comparaison d'adresse privée au renderer.

Premier passage natif : **7 réussites, 14 échecs sur 21**. Deux écarts de mon raccordement ont été corrigés : une demi-période CPU initiale retardait la publication de la frame, et la palette brute initialisée à zéro produisait du blanc au lieu du noir déterministe précédemment utilisé par RML. La palette est maintenant initialisée au code matériel 7, donnant le même noir initial, sans prétendre que les bascules du silicium ont un état de mise sous tension garanti.

Les anciennes empreintes des huit combinaisons bitmap provenaient du décodeur simplifié, avec notamment un mode borné par Math.Min. Elles ne constituent pas une référence matérielle : leur test est remplacé par le calcul indépendant des bits physiques source sur une ligne complète, pour chacun des huit cas. Le test du curseur configure DE retardé d'une cellule et CURSOR de deux ; l'attente porte désormais sur la cellule donnée par ces broches, une cellule après l'origine, plutôt que sur l'ancien helper qui annulait ce décalage.

Après ces corrections, **35/35 contrôles locaux BBC/System/CRTC Debug passent**. Les tests Chips de raccordement vérifient aussi le trou du motif curseur 101, les deux moitiés rouge/bleue d'une cellule lente après une écriture CPU intermédiaire, et les adresses obtenues par le réseau de NAND/additionneur : 13 adresses de frontière × quatre réglages C0/C1 × RAM 16/32 Kio × câblage banque libre/fixe.

La confrontation supplémentaire de `BBCMicro_Update.inl` de b2 et du schéma confirme deux points : MA et DE retardé sont distincts ; le latch télétexte IC15 est alimenté comme caractère quand MA13 sélectionne cette voie. Le propriétaire utilise maintenant MA physique pour ses accès RAM, sans le faire dépendre du retard DE demandé dans R8. Un nouveau test prélève le bus pour les quatre retards : les adresses restent 0, 8 et 16. **24/24 tests ULA Debug passent** avec ce cas supplémentaire. Les preuves invitées et les mesures de cette version ne sont pas encore produites ; aucune livraison ni commit final ULA n'est annoncé.

Les anciennes tables de pixels bitmap, masques de resampling et largeurs continues de curseur sont retirées de `VideoConstants` après contrôle de leurs utilisateurs : le propriétaire ne conserve plus un second décodeur parallèle à la puce. Les positions des quatre prises et leurs décalages sont des constantes nommées. Ce nettoyage ne change pas le modèle de sortie.

## Preuves invitées des modes MOS

Le diagnostic invité Debug réussit : le 6502 du BBC Model B, démarré avec MOS 1.20, BASIC II et la définition SAA5050 existants du dossier Firmware, appelle OSWRCH pour les modes 0 à 7 et écrit les textes/contrôles de couleur. Le CPU écrit le témoin de fin après ses appels VDU ; le banc hôte attend ensuite deux frames natives et contrôle qu'elles contiennent des pixels allumés et plusieurs couleurs. Ce témoin prouve l'exécution du programme invité ; les tables exhaustives palette/décalage et les écritures mi-cellule sont prouvées par les tests de broches, pas par une fausse lecture des pixels par le CPU.

Les huit PNG ont été inspectés : titres/textes lisibles, largeurs de caractères selon mode, deux/quatre/seize couleurs et télétexte coloré. Les modes bitmap trois et six conservent leurs interlignes. Ces captures sont des surfaces vidéo natives 640×256, sans mise à l'échelle ni modification. Elles restent hors Git dans `artifacts/TestsChips/BbcVideoUla/BbcMicroModelB/`.

| Capture | SHA-256 PNG |
|---|---|
| mode-0.png | BC9A1CFF012D46B6776F66B311ED2AD5A6BA3A229208FE37CB3C38304B49473B |
| mode-1.png | 0C6FC39D7CD572BA78A979BD824750C241AB3834BDFD04624CCAB8A388616824 |
| mode-2.png | 20CD1F79A2E396EDB65CA98CD8F9D9A9254D424E7185B6773B5E94DAF5DC87EE |
| mode-3.png | 6F23F0630CA29366FCCF88E46A59E36F25A7E92432D9254FDB8C5E7A657C2286 |
| mode-4.png | 2A1A80BEA28914FCD14C79CE33AAF79183888E5798754BD50C2DDB6F272CC8CD |
| mode-5.png | 1A7F21AE59442721FCC3E8A5DC8B7464BC63838F807DECCC0BE7728045F208B5 |
| mode-6.png | DDEC24734531ECAE640A5F2F160365FD257E86C7BEFAC2CDB2F4CCAB4D8E7235 |
| mode-7.png | 1885D60C682C979258E266932DF5A854E356F2632F21B6C6FD11DB9CA906171B |

## Mesure du premier raccordement complet

Banc autonome compilé avec des copies isolées des trois générateurs courants, sans ROM externe. Chaque charge programme son CRTC, attend deux frames d'échauffement puis mesure huit frames par passage ; médiane de cinq passages. Le tiering du JIT est désactivé uniquement dans le processus de banc. Les options Debug du produit sont inchangées. Le temps comprend le propriétaire, CRTC, ULA et SAA5050, et exclut la copie Render/empreinte effectuée ensuite.

| Charge | Debug ms/image | Release ms/image | Allocations de balayage/image | SHA-256 frame |
|---|---:|---:|---:|---|
| Bitmap 2 MHz CRTC | 37,9484 | 4,5145 | 0 | E74E6D2618DC1A629166B869EC594C27A6CC217BAA093DE7B3B24054F9A306C3 |
| Bitmap 1 MHz CRTC | 34,0949 | 4,1248 | 0 | 70D505ED784EFD981498B0C6B7AA232DA3C6C6D4C7B39C294A048BF04CC56436 |
| Télétexte | 36,5310 | 4,4253 | 0 | 672497C57480497E413EF1CA0D4DF3FCBA7E9A7AE8DB773589CDA2EB7204EA61 |

Ce coût Debug du chemin strict est trop élevé pour la cadence nominale. Le code réévalue les portes de sortie après chaque master clock et à chaque écriture de RGB télétexte, même lorsque ni les prises de palette ni le curseur ni les entrées n'ont changé ; les quatre prises binaires sont aussi recalculées pour chaque point. La correction à mesurer mémorise ces fonctions combinatoires tout en exécutant chacun des chargements, décalages et transitions d'horloge.

Premier passage d'optimisation : calcul des quatre prises précalculé pour les 256 valeurs, sorties RGB recalculées uniquement lors d'un changement pertinent, modes décodés à l'écriture et périodes master déjà complètes transmises directement par le propriétaire. Toutes les empreintes précédentes restent identiques, allocation de balayage nulle. Médianes Debug : **33,2486 / 27,0828 / 29,2794 ms** (bitmap rapide / lent / télétexte) ; Release : **3,7609 / 3,1226 / 3,4002 ms**. Ce résultat améliore le coût mais reste trop lent en Debug : l'optimisation n'est pas clôturée. L'étape suivante regroupe le passage des huit sorties vers le propriétaire, en conservant leur séquence et en comparant cette voie à chaque transition des broches.

## Optimisation finale et couverture obtenue

Le propriétaire stocke directement ses pixels dans deux tableaux uint réutilisés, puis convertit seulement la copie publique Render en octets. Les huit RGB de chaque période CPU sont transmis dans 24 bits ; toutes les sorties intermédiaires restent représentées. Les séquences purement combinatoires des quatre prises sont précalculées pour chaque octet et cadence. L'état final du registre à décalage, les diviseurs et le curseur restent ceux des transitions unitaires ; les départs hors frontière passent par le chemin unitaire. Ce regroupement n'accepte pas une écriture CPU au milieu d'une période CPU déjà traitée : ces écritures sont séparées par les appels Tick du propriétaire, comme sur son bus existant.

Le test `BulkClockTests` compare désormais **65 536 couples contrôle/octet**, avec changements de palette, DISEN, inversion, CURSOR et les huit entrées RGB de chaque appel. Des décalages de trois périodes master injectés régulièrement couvrent les départs hors frontière. Les RGB point par point et les états finaux des quatre diviseurs, de CRTC CLK et du curseur coïncident avec les broches unitaires.

Le schéma Acorn confirme aussi le blanking par la ligne RA3, et non une comparaison numérique RA>=8. Le contrôle natif vérifie les trente-deux lignes : visible 0–7, masqué 8–15, de nouveau visible 16–23, masqué 24–31. DISEN, commun au curseur, reste câblé ainsi même lorsque la source RGB télétexte contourne la palette.

Dernière comparaison autonome identique avant/après (même banc, échauffement, médiane de cinq passages) :

| Charge | Debug avant/après ms/image | Release avant/après ms/image | Réduction Debug / Release |
|---|---:|---:|---:|
| Bitmap CRTC 2 MHz | 36,6019 / 20,9200 | 4,5794 / 2,7669 | 42,8 % / 39,6 % |
| Bitmap CRTC 1 MHz | 33,2903 / 17,9532 | 4,2047 / 2,6032 | 46,1 % / 38,1 % |
| Télétexte | 36,2526 / 19,0066 | 4,5080 / 2,6661 | 47,6 % / 40,9 % |

Les empreintes des trois charges restent celles du premier tableau, identiques en Debug/Release et avant/après ; aucune allocation n'a lieu dans la boucle de balayage. Les passages intermédiaires variaient jusqu'à 16,6–20,9 ms pour la version finale Debug ; ces durées dépendent de la charge de l'hôte. Le banc n'inclut ni le CPU BBC, ni l'audio, ni la présentation WPF : il ne prouve pas 50 FPS de l'application complète ni une cadence maximale garantie. Il n'emploie aucune optimisation de compilation ajoutée au Debug.

Après réalisation et optimisation : **2 019/2 019 tests Chips Debug et Release**, dont **26 cas ULA**, et **36/36 contrôles locaux dans chacune des configurations**, comprenant le diagnostic invité des huit modes. Les huit PNG régénérés après optimisation conservent leurs empreintes. Le constructeur public historique à trois arguments de Video est conservé ; le câblage de banque B+/Master passe par une surcharge, sans supprimer cette entrée binaire existante.

Les registres et tous les chemins numériques décrits par les sources examinées sont réalisés et vérifiés. Les délais électriques setup/hold, la distribution analogique exacte des horloges du masque et les défauts propres à des révisions non identifiées ne sont pas caractérisés par cette émulation numérique. La sélection du bus doit être synchronisée par son propriétaire ; aucune broche RESET ni lecture de palette absente du circuit n'est inventée. Ces limites documentaires ne sont pas présentées comme des tests matériels effectués.

## Validation après nettoyage

Le diagnostic invité et son répertoire de références/banc temporaires sont supprimés. Les assemblies locales sont reconstruites en Debug et Release ; leur découverte de tests ne contient plus BbcVideoUlaGuestProof. Les suites définitives passent : **2 019 tests Chips** et **35 contrôles natifs** (33 vidéo/System et 2 raccordement CRTC), dans chacune des deux configurations. Les huit captures demeurent dans artifacts/TestsChips/BbcVideoUla/BbcMicroModelB, ignorées par Git.

## Livraison initiale

Le script `scripts/local-building.cmd --building=debug --modules=A` termine avec le code 0. L’exécutable `build/Debug/Retro Media Lab/retromedialab.exe`, les 24 DLL de modules attendues et `lib/retromedialab.chips.dll` sont présents. La DLL Chips livrée correspond exactement à celle construite dans le projet : SHA-256 `05E3582B0836D0002744F85EDFC43078C10E065C9070D23E0D48633A741C8D85`. Les captures et temporaires ne sont pas inclus dans le commit.

## Séparation du câblage et réutilisation — 5 novembre 2026

La classe de carte `Video`, ses deux fichiers partiels et ses constantes sont déplacés hors de Chips vers le module Acorn. L'ancienne interface `IVideo`, inutilisée hors de cette classe de carte, est retirée ; `IBbcVideoUla` reste le contrat du véritable circuit. Les cinq contrôles de câblage sont déplacés dans `AcornEmuBbcVideoWiringTests` du projet local d'émulation, sans référence du projet de tests Chips vers Acorn.

`ClockMasterPeriods` reçoit les huit valeurs RGB externes correspondant à huit périodes maître, et rend les huit sorties. Il ne choisit ni CPU, ni cadence de carte, ni SAA5050, ni CRTC. La comparaison avec les transitions unitaires du VIDPROC conserve les 256 contrôles, changements de palette et de phase, curseur et RGB, y compris une entrée de paquet décalée. La carte BBC choisit seule de consommer un paquet par cycle de son CPU et de lui relier son SAA5050. Le chemin télétexte exploite le paquet RGB directement au lieu de redécoder huit couleurs ; registres, décalage, horloges et curseur restent inchangés.

Après séparation : **210 contrôles de puces ciblés réussis en Debug et Release**. Les contrôles locaux regroupant vidéo, standards PAL/NTSC, câblage et diagnostic de débit donnent **42 réussites Debug** (une variante de débit) et **43 Release** (deux variantes). La correction de débit du BBC reste ouverte ; ces résultats ne la déclarent pas terminée.

Build après séparation : `scripts/local-building.cmd --building=debug --modules=A` terminé avec code 0. Exécutable et 24 DLL d'entrée de modules présents ; `lib/retromedialab.chips.dll` livré correspond au build Debug, SHA-256 `72226A840E682CFBFEE713839810DABC22D3B8DD5BE00C63AF6B6312C154B4CB`.

Essai de raccordement du 5 novembre : partage des valeurs CRTC et de la lecture RAM lorsqu'IC15 et VIDPROC échantillonnent la même adresse. Les 28 contrôles vidéo Debug ont réussi, dont deux cas comptant les lectures aux cadences lente/rapide. Sur 500 images la variante donne 30,10 FPS / 1,20 MHz avec la configuration utilisateur et 34,35 FPS / 1,37 MHz à vide ; le raccordement précédent donne 30,40 FPS / 1,21 MHz et 32,42 FPS / 1,29 MHz. Les cycles, PC et empreintes vidéo concordent. Aucun gain utile n'est établi sur la configuration utilisateur ; la modification et ses deux tests spécifiques sont retirés. Le câblage antérieur est conservé, sans modification des circuits.

Essai suivant dans le tampon de la carte, sans changement de VIDPROC : les huit pixels d'un paquet RGB constant sont remplis par Span.Fill, les paquets variables restant scalaires. Les 26 contrôles vidéo Debug passent, mais le banc de 500 images donne 29,18 FPS / 1,17 MHz avec la configuration utilisateur, 32,46 FPS / 1,30 MHz à vide, contre 30,40/32,42 FPS sur le raccordement précédent. Le chemin Span.Fill et sa constante de répétition sont retirés faute de gain établi ; l'écriture scalaire reste utilisée.

Le profil suivant attribue 4 620,78 ms inclusives au raccordement vidéo sur 10 019,26 ms du thread, dont 1 321,18 ms dans PrepareTeletext. Les sorties du CRTC, possédé exclusivement par la carte, sont désormais lues une fois après chaque horloge caractère, écriture de registre ou reset et conservées au niveau du raccordement. Les mêmes niveaux sont consommés pendant les cycles CPU intermédiaires, sans modifier le CRTC, les lectures RAM, les fronts F1/TR6 ni l'instant des mises à jour VIA. Le nouveau test vérifie les huit valeurs après 4 096 cycles entrecoupés de changements de cadence, écritures de registre et resets.

Les 27 contrôles vidéo passent en Debug et Release. Banc de 500 images : 32,0661 ms/image, 31,19 FPS / 1,25 MHz, CPU 31,2188 ms/image ; à vide 29,1522 ms/image, 34,30 FPS / 1,37 MHz, CPU 28,3125 ms/image. Cycles, PC et empreintes vidéo identiques aux passages de même longueur. L'amélioration observée est modeste ; elle ne prouve pas que la fenêtre réelle tient les 50 FPS.

Build Debug complet livré avec les 24 modules et contrôlé par empreintes. La capture native suivante reste à 27,2 FPS / 1,09 MHz ; elle est conservée sous `artifacts/TestsChips/Mos6502/BbcMicroModelB/application-debug-crtc-signals.png`, hors Git. La différence entre l'ancien rendu par caractère et le nouveau balayage par transitions reste la cible principale ; les optimisations ponctuelles ne démontrent pas de rétablissement de la cadence native.

### Tampon de balayage compact — 5 novembre 2026

Le raccordement conserve huit pixels déjà résolus dans un uint, puis développe le BGRA à la publication. Les index conservent séparément le pixel absent, les couleurs et les gris appliqués lors du balayage. Les écritures superposées et découpées restent exactes, sans relire la RAM, rejouer une puce ni réappliquer une palette ultérieure. Les deux tampons de travail passent de 640 × hauteur uint à 80 × hauteur uint ; le tampon livré reste un tableau BGRA immuable de même dimension.

Huit contrôles autonomes de raccordement Debug passent, dont la comparaison pixel à pixel de 8 192 écritures avec 4 096 motifs, deux palettes et découpes/chevauchements. Le banc vidéo seul passe de 9,1911 à 8,6967 ms sur 500 tranches ; le cœur de 32,1227 à 31,4865 ms, soit 31,76 FPS / 1,27 MHz. Les empreintes finales sont identiques. Ce gain limité ne constitue pas une validation à 50 FPS dans l'application.

Validation regroupée : 44 contrôles vidéo/raccordement réussis en Debug et Release. Build Debug All réussi, 24 modules présents et empreintes Chips/Acorn vérifiées. Capture native `artifacts/TestsChips/Mos6502/BbcMicroModelB/application-debug-compact-scanout.png` à 28,4 FPS / 1,13 MHz, après une observation à 27,4 / 1,10 ; cadence toujours insuffisante. Fenêtre et processus possédés fermés et disparition vérifiée.

### Paquets de palette et décodage invariant — 5 novembre 2026

Livraison : build Debug avec les 24 modules terminé code 0, exécutable et toutes les entrées présents ; Chips livré/source SHA-256 `0028D0A74776D364D0BC4891343D76C9D702426206B5ED752D05739B737AA3A1`, Acorn `FE8B05113098547E042E70BA5BA6BB323A2DAFADA4E85087F38D618B942D5B74`. Fenêtre réelle au prompt BASIC : observation 31,0 FPS / 1,24 MHz, puis capture `artifacts/TestsChips/BbcVideoUla/BbcMicroModelB/application-debug-palette-cache.png` à 29,4 FPS / 1,17 MHz / 59 %. Cette variation et le banc complet ne prouvent pas un gain global stable. La vitesse normale reste non rétablie. Session possédée PID 27016 fermée en finally, sortie effective vérifiée ; capture ignorée par Git. Le banc temporaire de comparaison, sa référence externe et ses journaux sont supprimés ; assemblies de tests Debug et Release reconstruites sans ce banc.

Le contrôle prépare désormais le masque de chargement caractère, les segments de curseur, la base du motif et le nombre de décalages/remplissage d'un paquet. Les méthodes internes accèdent aux champs, sans repasser par leurs propriétés publiques ; les contrats de broches restent inchangés. Chaque motif bitmap possède un cache RGB associé à la révision des couleurs résolues. Une écriture palette ou flash qui change une couleur invalide les anciens paquets immédiatement ; les paquets sont recalculés à leur prochaine utilisation, pas tous à chaque écriture. Le retour à zéro du compteur de révision vide les entrées avant de réutiliser la première révision. Le cache ajoute 8 Kio par composant, sans allocation dans l'horloge. DISEN, curseur, inversion et RGB télétexte sont appliqués à chaque appel ; le décalage et les diviseurs continuent toujours, même lorsque la sortie est masquée. Aucun CPU, SAA5050 ou CRTC n'est référencé par ce circuit.

Les **23 tests VIDPROC passent en Debug et Release**, avec 65 536 couples contrôle/octet comparés point par point au chemin des broches, et une seconde séquence de 65 536 paquets réutilisés couvrant palette/flash, reset, masquage, inversion et curseur. Le débordement du compteur purement logiciel est forcé dans un test autonome pour vérifier qu'aucun ancien paquet n'est réutilisé. Les **29 contrôles de vidéo et raccordement natifs passent dans chaque configuration** ; les diagnostics temporaires expliquent les totaux 32 Debug et 31 Release, ils ne sont pas des tests matériels supplémentaires.

Comparaison temporaire appariée : ancien build livré et nouveau build de même configuration chargés chacun dans un contexte collectible distinct, même delegate et mêmes entrées ; déchargement des deux contextes vérifié après finally. 100 000 paquets d'échauffement, sept passages de 2 000 000 paquets, ordre alterné, médiane. DISEN actif, curseur inactif, données 5A, palette stable préparée sur les seize entrées ; ce banc mesure la réutilisation chaude, pas des écritures palette à chaque période. Un premier passage Release avec contextes de chargement différents a été écarté ; les chiffres ci-dessous proviennent des deux contextes identiques.

| Contrôle | Debug avant / après (ns/paquet) | Release avant / après (ns/paquet) |
|---|---:|---:|
| 1C, bitmap rapide | 58,233 / 22,462 | 14,500 / 8,150 |
| 08, bitmap lent | 55,031 / 19,779 | 13,791 / 7,062 |
| 02, télétexte | 21,241 / 14,415 | 5,511 / 4,932 |

Debug conserve bien DisableOptimizations dans les deux DLL ; Release ne le porte pas. Le gain est local au VIDPROC. Le banc complet BBC B Debug reste à **31,4953 ms/tranche, 31,75 FPS, 1,27 MHz**, vidéo seule 8,9767 ms. Release : **4,6133 ms/tranche, 216,76 FPS, 8,66 MHz**. Dans chaque cas, les 19 967 997 cycles des 500 tranches, PC E460 et empreinte vidéo `12C94B65ECCF8B9650F2210900471F90D4C08E41476E0ACFAD6AFA5703436BE0` restent identiques. Ces valeurs de banc ne prouvent pas encore 50 FPS/2 MHz dans la fenêtre Debug.

## Présentation groupée des entrées DATA/CURSOR/DE — 5 novembre 2026

`SetCharacterInputs` présente les trois entrées dans le même ordre que leurs setters successifs et recalcule la sortie combinatoire seulement si DE change, comme le setter individuel. L'appel ne charge pas le registre à décalage et ne produit aucun front : les périodes restent assurées par les fonctions d'horloge existantes. Les trois accès individuels restent disponibles. Le raccordement BBC utilise ce point d'entrée au même emplacement que les trois écritures précédentes ; aucune lecture RAM n'est retirée ni déplacée.

Contrôle autonome : 256 valeurs de contrôle, 256 données, quatre combinaisons CURSOR/DE, écritures de palette, phases non alignées et resets. Comparaison des entrées, RGB, diviseurs et curseur avant horloge, puis des huit sorties sérialisées après horloge. Les 24 contrôles VIDPROC permanents passent en Debug et Release.

Mesure isolée des entrées suivies de huit périodes, deux instances du même composant ; 20 000 cellules de chauffe puis médiane de sept passages alternés de 500 000 cellules. Sommes de paquets identiques. Compilation tiercée désactivée uniquement dans le processus de mesure, aucune option du produit modifiée.

| Contrôle | Debug setters → groupé (ns/cellule) | Release setters → groupé (ns/cellule) |
|---|---:|---:|
| 00 | 31,827 → 27,689 | 9,045 → 8,793 |
| 02 | 27,799 → 24,053 | 7,229 → 7,433 |
| 10 | 31,777 → 28,690 | 9,343 → 9,348 |
| 1C | 31,348 → 28,741 | 8,889 → 8,978 |
| FF | 29,757 → 25,521 | 7,453 → 7,468 |

Le gain local Debug est mesuré dans les cinq charges. Les faibles variations Release incluent des ralentissements : aucun gain Release général n'est revendiqué. Le coût local épargné ne permet pas à lui seul de déduire un gain de cadence de la machine complète. Le premier passage Debug avec les autres tests sert uniquement à la validation ; seules les mesures isolées figurent ci-dessus.

Raccordement : 32 contrôles vidéo passent en Debug et Release. Banc BBC B, 80 tranches de chauffe puis 500 mesurées avec les réglages JIT ordinaires : Debug 28,3522 ms / 35,27 FPS / 1,41 MHz ; Release 4,8987 ms / 204,14 FPS / 8,15 MHz. PC E460, 19 967 997 cycles et SHA-256 vidéo `12C94B65ECCF8B9650F2210900471F90D4C08E41476E0ACFAD6AFA5703436BE0` identiques. Ces passages complets ne sont pas une comparaison appariée et ne prouvent pas une hausse globale stable. L'accès groupé reste conservé pour son coût Debug local inférieur et son équivalence vérifiée ; le déficit de cadence global reste ouvert. Banc de coût temporaire supprimé après consignation.

## Bus RAM partagé par les récepteurs vidéo — 6 novembre 2026

Le module vidéo lisait deux fois la même adresse pendant le créneau qui alimentait IC15 et VIDPROC : PrepareTeletext lisait pour le latch télétexte, puis Tick relisait pour les entrées du VIDPROC. Le bus RAM physique présente un octet, vu par les récepteurs raccordés ; ce ne sont pas deux demandes indépendantes de lecture. La donnée est désormais lue une seule fois et distribuée aux deux captures.

L'ordre de la chaîne télétexte est conservé : DATA du SAA5050 reçoit l'ancien latch, le front F1 est traité, LOSE est présenté, puis IC15 reçoit le nouvel octet si MA13 sélectionne sa voie. VIDPROC reçoit ce même nouvel octet dans le créneau courant. Les phases 1/2 MHz, les huit périodes 16 MHz par cycle de carte, les fronts du SAA5050 et les cycles CPU ne sont pas déplacés. La valeur d'un accès RAM est rééchantillonnée au créneau suivant ; aucun cache de contenu ne masque les écritures CPU.

Référence primaire : manuel de maintenance Acorn, sections RAM et vidéo, décrivant l'alternance des accès CPU/CRTC à 250 ns, la donnée mémorisée par IC6 en fin de créneau CRTC et le générateur de caractères IC5 : https://acorn.huininga.nl/pub/unsorted/manuals/BBC%20Microcomputer%20Service%20Manual-HTML/BBCServiceManual.html. Autre implémentation consultée : JSBeeb lit un octet vidéo puis le transmet au générateur de caractères et au rendu ; ses approximations de bordure sont explicitement documentées et ne sont pas reprises ici : https://github.com/mattgodbolt/jsbeeb/blob/main/src/video.js.

Validation : les 16 contrôles AcornEmuBbcVideoWiringTests réussissent en Debug (954 ms) et Release (environ 1 s). Ils incluent tous les modes ULA et palettes, entrelacement PAL/NTSC, adresses mémoire, skew et RA, clipping, fronts CRTC, partage du bus IC15/VIDPROC, conservation du latch précédent au front F1, effet d'une écriture CPU seulement au prochain créneau, et poursuite des trames par l'horloge de carte sans exécution d'instructions CPU. Ce dernier cas ne prétend pas simuler un CPU arrêté physiquement par un nouveau signal : il isole l'avancement des composants de la carte de l'exécution d'instructions.

La carte appelle déjà la vidéo avant le transfert CPU dans le cycle du bus. Aucun accès simultané CPU/vidéo n'a été démontré dans le chemin examiné ; la correction porte sur la duplication de la lecture dans le créneau vidéo. Les résultats globaux de performances et les empreintes du BBC complet sont consignés séparément dans docs/chips/CPU/Mos6502.md après mesure.

Mesure du BBC complet avant/après ce changement, trois passages alternés par variante : médiane Debug 24,0878 → 22,2160 ms/image (−7,77 %), Release 3,8651 → 3,7630 ms/image (−2,64 %). Les douze passages conservent cycles, PC et empreintes vidéo/audio ; conditions et valeurs individuelles dans docs/chips/CPU/Mos6502.md. Le déficit Debug à la vitesse réelle reste ouvert.

Sorties du cœur à ROM réelles : `artifacts/TestsChips/VideoULA/BbcMicroModelB/video-bus-debug.png` et `video-bus-release.png`, hors Git. Elles montrent le prompt BBC/DFS/BASIC rendu après le scénario et non une fenêtre native. La capture Debug a été inspectée ; les images Debug et Release ont la même empreinte de pixels. Les tests de bus restent les preuves des valeurs et phases, pas le seul écran de démarrage.

## Conservation des lignes déjà converties — 6 novembre 2026

Le cache ajouté est dans la présentation du module BBC, pas dans les circuits. Video.Tick, les accès RAM, CRTC, VIDPROC et SAA5050 continuent aux mêmes phases. Render compare chaque ligne compacte à sa dernière présentation, conserve sa conversion si elle est identique et reconvertit toute ligne modifiée. Les parités entrelacées, champs absents, pixels redevenus nuls et changements de hauteur sont pris en compte. Chaque appel retourne un nouveau tableau possédé : le cache privé ne peut pas être altéré par son destinataire. À 640 × 512, il conserve 1 310 720 octets RGBX et 163 840 octets de mots source, hors snapshot retourné. Ce gain ne prétend pas supprimer les cycles matériels de lecture vidéo.

Banc BBC Model B complet, configuration utilisateur copiée en mémoire sans écrire ses médias/configuration, 80 trames de chauffe puis 200 mesurées, trois processus par variante en ordre référence/candidat/candidat/référence/référence/candidat. Debug non optimisé et Release vérifiés sur les DLL chargées. Durée de RunFrame et drainage/contrôle PCM ; empreinte de toute la séquence vidéo calculée hors chronométrage, identiquement avant/après. Pas de fenêtre native, transport UDP ou sortie audio Windows.

| Scénario | Debug avant → après (ms/image, médianes) | Release avant → après |
|---|---:|---:|
| Prompt | 20,898818 → 19,585513 | 3,416035 → 3,322309 |
| SOUND 1,-15,80,255 | 20,775063 → 19,626455 | 3,408457 → 3,256607 |
| MODE 1 / COLOUR / CLS en boucle | 23,419610 → 22,889696 | 3,359576 → 3,309870 |

Plages Debug avant/après : prompt 20,456045–21,135869 / 19,558394–19,792764 ; son 20,753542–21,189253 / 19,541304–20,116634 ; redessin 23,122288–23,468322 / 22,635590–23,010422. Plages Release : prompt 3,363253–3,545209 / 3,303903–3,323117 ; son 3,398897–3,444160 / 3,215841–3,261448 ; redessin 3,289708–3,365845 / 3,257679–3,362550. Le faible gain Release en redessin présente un chevauchement des plages ; le gain global au repos est plus net. Le candidat est conservé.

Les 36 passages conservent, par scénario, cycles, PC, image finale, séquence vidéo complète et PCM. Prompt : 7 987 198 cycles, E464 ; son : 7 987 201 cycles, E539 et 176 118 échantillons non nuls ; redessin : 7 987 203 cycles, CCBC. Empreintes SHA256 des séquences vidéo : prompt 5C652BCA342C8DADA14321504153791EC979573CA3A258FCA4814DFE508941C0 ; son CD0A605BD87B5C9042B17577750315A8F807465CF6FE9BAFC1B1619EA3369280 ; redessin 4AF7B78BA07ED19155E5563DFBFB9064194AD8569F42AA0AB7A2B8FE62596056. PCM : prompt EDEBE61CAC85A9B25B1FFEDB4676F434112D5D8900958D01F910507534159485 ; son 6B1AE9928C6BD5D1E723D7A5CE7C270853F36DC643B3624A85FD38EE57108C98 ; redessin 3C7700D5B155CCCB980C220182E82858A332451FB727344329DB2B62C4DBB68E.

Validation vidéo : 56 cas réussis dans chaque compilation, dont six comparaisons indépendantes pixel par pixel couvrant PAL/NTSC, entrelacement, réinitialisation et possession des tableaux. Deux autres assertions télétexte échouent aussi avec la DLL de référence antérieure : NativeTeletextUsesLatchedMemoryAndRetainsTheFortiethCharacter et NativeTeletextLineCounterFollowsLoseRatherThanTheCrtcRasterRegister. Elles restent ouvertes ; leurs attentes ne sont pas affaiblies. Ces échecs empêchent de déclarer toute la vidéo finalisée.

Capture réelle du cœur Debug : artifacts/TestsChips/VideoULA/BbcMicroModelB/render-lines-drawing.png, inspectée, montre le remplissage MODE 1 en cours. Son passage supplémentaire donne 23,859569 ms/image ; il est distinct de la série comparative et ne remplace pas ses valeurs. Le budget 19,97 ms est atteint sur les trois passages au prompt, pas sur tous les scénarios ; la cadence native RML reste à mesurer.

Livraison de la conservation des lignes : scripts/local-building.cmd --building=debug --modules=A terminé avec code 0 ; 24 manifestes et leurs DLL présents. Exécutable : build/Debug/Retro Media Lab/retromedialab.exe. Les SHA256 des DLL Acorn et Chips livrées correspondent aux sorties Debug source. Aucun test natif n'est déduit de cette réussite de compilation. Les avertissements CA1416 signalés dans des modules externes sont distincts du chemin vidéo modifié.

Observation native du build livré : au prompt BBC/DFS/BASIC, compteurs visibles 640 × 512 / 50,1 Hz, puis 46,5 FPS / 1,86 MHz / 93 % et 47,8 FPS / 1,91 MHz / 96 % au moment de la capture. Ce sont deux observations, pas une moyenne instrumentée ni une comparaison native avant/après contrôlée. Le seuil 50 FPS / 2 MHz n'est pas validé. Capture : artifacts/TestsChips/VideoULA/BbcMicroModelB/native-render-lines.png. Le dialogue dotnet.exe déjà présent avant le lancement apparaît superposé ; il appartient à une autre fenêtre préexistante et n'a pas été fermé. La fenêtre RML lancée pour ce contrôle a été fermée, avec sortie du processus 46720 et disparition vérifiées. Le JPEG intermédiaire est supprimé.

Clôture des deux assertions télétexte : le diagnostic a montré un premier champ opposé pas encore disponible et des coordonnées de lignes encore exprimées en résolution progressive. Les tests attendent désormais des champs terminés et vérifient explicitement progressif/entrelacé et les deux parités. Aucun code de circuit n'a été changé. Les 45 contrôles vidéo BBC ciblés réussissent dans chaque compilation Debug/Release, après suppression du diagnostic temporaire et reconstruction des assemblies. Cause, valeurs et références détaillées dans docs/chips/Video/Saa5050.md ; les mentions antérieures « deux échecs ouverts » décrivent l'état avant cette correction des tests.

### Fixtures MODE 4 : palette matérielle complète — 6 novembre 2026

Les trois échecs supplémentaires trouvés dans AcornEmuTests concernent ModelARamExpansionChangesCpuAndVideoAddressing (16/32 Kio) et ModeFourExpandsEachMemoryBitToTwoDisplayPixels. Le diagnostic confirme une RAM contenant 0x80, deux champs terminés, une sortie de 256 lignes, mais aucun pixel éclairé. La fixture n'écrivait que 0xf0 dans FE21 : seule l'entrée matérielle 15 devenait blanche, toutes les autres restaient noires après l'initialisation.

En MODE 4, le bit 7 détermine la couleur binaire, mais les prises 5/3/1 du registre à décalage restent connectées aux trois autres bits d'adresse de la palette. Pour 0x80, l'adresse initiale est donc 8, non 15. Il faut programmer les huit entrées 0–7 en noir et 8–15 en blanc pour cette fixture deux couleurs. Cette lecture des quatre prises est également explicite dans le [générateur de table de JSBeeb](https://github.com/mattgodbolt/jsbeeb/blob/main/src/video.js#L404-L430) ; son [registre de palette](https://github.com/mattgodbolt/jsbeeb/blob/main/src/video.js#L131-L144) utilise les quatre bits hauts comme sélecteur et le complément des trois bits bas comme couleur physique. Le correctif porte sur la préparation des tests, sans modifier le VIDPROC, le CRTC, la RAM ni les horloges.

Les deux tests partagent désormais une programmation bitmap complète (y compris synchronisations), programment tous les alias de palette et attendent explicitement le troisième champ terminé, avec une borne de cycles calculée à partir des registres de cette fixture. La vérification des miroirs 16/32 Kio et de l'effacement RAM au reset est conservée. La vérification de doublement couvre six octets 00/01/80/55/AA/FF : les seize pixels, les trois canaux RGB et la cellule suivante noire. Ces cas comparent à l'expansion binaire attendue, indépendamment de la table du VIDPROC.

Validation finale : 90 cas réussis en Debug et 90 en Release, sans exclusion dans les groupes AcornEmuTests, AcornEmuVideoTests, AcornEmuBbcVideoWiringTests et AcornEmuVideoRenderTests. Les trois avertissements de compilation déjà présents sont sans rapport avec ces fixtures. Le journal temporaire est supprimé et les messages diagnostiques ne restent pas dans les tests. Aucune hausse de performance ni nouveau rendu natif n'est revendiqué pour cette correction de tests ; le budget global 19,97 ms reste à atteindre sur tous les scénarios.

## Capture et restauration des pixels en cours

`BbcVideoUlaState` préserve les 21 champs mutables, la palette, les couleurs décodées et leur cache. La restauration conserve les phases maître, le caractère partiellement décalé, le curseur, les entrées RGB externes et la sortie courante, sans faire avancer l'horloge ni dépendre de l'identité du composant raccordé. Les tableaux sont copiés dans les deux sens.

Trois tests Debug réussis (139 ms) : 126 reprises couvrant neuf contrôles, sept phases et les chemins maître unitaire/groupé. Les pixels, horloges divisées, curseurs, changements de palette et bascules de sélection correspondent après reprise. La mémoire vidéo et le framebuffer de la carte restent à sauvegarder par leur propriétaire.

## Carte Model B, quatre conversions ADC actives — 7 novembre 2026

Rendu complet et drainage audio inclus, 120 images Debug après dix de chauffe, MOS 1.20/BASIC II, aucune modification de puce : repos 18,287654 ms/image (54,6817 images/s, 2,183770 MHz), boucle MODE 1/CLS alternés 20,499876 ms/image (48,7808 images/s, 1,948109 MHz). Cadence matérielle environ 50,08 Hz inchangée. Un seul passage sans référence alternée : ne pas revendiquer un gain, ni le respect de 19,97 ms en redessin. Captures inspectées `artifacts/TestsChips/BbcVideoUla/BBC Micro Model B/20261107-092032-model-b-idle-adc4.bmp` et `20261107-092042-model-b-redraw-adc4.bmp` ; cette dernière montre une phase noire réelle de l'effacement, pas toutes les trames. [Détails et limites du passage](<../../project/emulations/Acorn/BBC Micro/Model B/board-validation.md>).

## Profil du Master 128 — 7 novembre 2026

Video.Tick représente 51,40 % du poids échantillonné de la carte Debug, incluant CRTC (13,01 %), ClockHalfDots SAA5050 (10,51 %) et PrepareTeletext (8,10 %). Ces parts imbriquées ne s'additionnent pas. Le nom PublishOutputs du CRTC désigne notamment des décalages matériels réels ; aucun appel n'est retiré sans preuve d'équivalence. Aucun changement de production, nouvelle optimisation ni nouveau résultat fonctionnel revendiqué. [Profil complet et sources examinées](<../../project/emulations/Acorn/BBC Master/Master 128/profile-20261107.md>).
