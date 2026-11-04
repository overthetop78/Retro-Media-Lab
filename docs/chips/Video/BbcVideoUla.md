# ULA vidéo BBC — circuit 5C094 / VIDPROC

## Périmètre et références

Le code existant `Video/Acorn/BbcMicro/VideoULA/Services/Video.cs` réunit actuellement le propriétaire vidéo BBC, le CRTC et le SAA5050, la traduction mémoire de carte, la palette et un rendu par caractère. Cette classe n'est donc pas encore une description isolée de toutes les broches de l'ULA. Les deux autres générateurs ont leurs propres fiches et restent distincts.

Référence principale : [Acorn, BBC Microcomputer Service Manual, section 3.3 et schéma](https://acorn.huininga.nl/pub/unsorted/manuals/BBC%20Microcomputer%20Service%20Manual-HTML/BBCServiceManual.html), confrontée au [schéma BBC](https://chrisacorns.computinghistory.org.uk/docs/Acorn/Manuals/Acorn_BBCCctDiagram.pdf). Le schéma IC6 montre D0–D7, A0, sélection CS, horloge 16 MHz, sorties 8/4/2/1 MHz et CRTC CLK, DISEN, CURSOR, INVERT, entrées RGB SAA5050 et sorties RGB. Le lien S26 sélectionne la vidéo normale/inversée. La ROM, le calcul d'adresse RAM et le PAL encodeur ne sont pas internes à cette ULA.

Comparaisons : [b2 VideoULA.cpp](https://github.com/tom-seddon/b2/blob/master/src/beeb/src/VideoULA.cpp), [B-Em video.c](https://github.com/stardot/b-em/blob/master/src/video.c), [JSBeeb video.js](https://github.com/mattgodbolt/jsbeeb/blob/main/src/video.js), [MAME bbc_v.cpp](https://github.com/mamedev/mame/blob/master/src/mame/acorn/bbc_v.cpp). Les extensions VideoNuLA de ces projets ne sont pas des fonctions de la puce d'origine et ne doivent pas être ajoutées par confusion. Le [remplacement matériel ouvert de Gert van der Knokke](https://github.com/gertk64/BBC_Video_ULA) fournit également les équations de ses GAL ; il s'agit d'une réalisation de remplacement, pas du masque Ferranti original. Ses différences de curseur ne sont pas reprises comme preuve du silicium.

## Écarts établis avant reprise

| Fonction | État actuel | Vérification à réaliser |
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

## Livraison

Le script `scripts/local-building.cmd --building=debug --modules=A` termine avec le code 0. L’exécutable `build/Debug/Retro Media Lab/retromedialab.exe`, les 24 DLL de modules attendues et `lib/retromedialab.chips.dll` sont présents. La DLL Chips livrée correspond exactement à celle construite dans le projet : SHA-256 `05E3582B0836D0002744F85EDFC43078C10E065C9070D23E0D48633A741C8D85`. Les captures et temporaires ne sont pas inclus dans le commit.
