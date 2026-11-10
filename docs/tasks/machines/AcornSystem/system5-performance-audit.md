# System5 — bilan Debug et Release du10 novembre2026

## Version et portée

Production identique au commit `c1a3621111cfbb417d03cefe5ee496da0c274ece` ; aucun changement de production retenu pendant cet audit. Les candidats connecteur et décodage préparé sont retirés après mesure. Windows, SDK .NET10.0.401, configurations Debug/Release ordinaires des projets, sans changement des options d’optimisation ni dynarec. Le modèle du CPU hôte n’a pas été collecté : accès CIM refusé ; ne pas lui attribuer une fréquence supposée. Les durées sont celles de cette session, pas des garanties de débit sur un autre hôte.

La configuration native mesurée est NOS80/VDU80, CPU2 MHz, deux lecteurs simple face80 pistes, SSD System BASIC en lecture seule. L’inventaire ci-dessous couvre les types instanciés par ce parcours et leurs sous-composants ; il ne constitue pas une certification électrique indépendante de toute variante ou extension System5.

## Tests et débits des composants

Chaque ligne utilise les tests autonomes actuels de sa famille. Les605 cas6502 couvrent aussi des variantes : ce nombre n’est pas605 programmes invités propres au6502A du System5. Les20 types totalisent1396 tests réussis par configuration, zéro échec/ignoré. Le banc de performance est un test distinct, réussi dans chaque configuration, avec21 scénarios (8271 au repos et actif).

Durées réellement chronométrées :10 000 unités puis trois fenêtres de1 000 000 unités de chauffe non chronométrées, puis cinq fenêtres de1 000 000 unités chronométrées sur la même instance, médiane ci-dessous. Les bornes min/max sont également publiées. Les sorties décrites dans chaque scénario sont raccordées à des observateurs consommant leurs valeurs ; coût des appels, boucle et observateurs inclus, aucun chronométrage par appel ni soustraction hypothétique. Les scénarios sont isolés ; ils ne sont ni des coûts exclusifs de la machine ni un modèle exhaustif de tous les modes. Ne pas additionner ces durées ou les comparer directement aux historiques dont le protocole diffère.

| Composant | Tests Debug réussis/échoués | Tests Release réussis/échoués | Debug ms/1M unités, médiane [min–max] | Release ms/1M unités, médiane [min–max] | Unité et scénario |
|---|---:|---:|---:|---:|---|
|6502A|605/0|605/0|85,02 [82,82–97,33]|41,57 [39,42–46,43]|cycle : NOP, bus RAM sans sorties de phase raccordées|
|6522|145/0|145/0|17,23 [16,58–17,87]|4,69 [4,50–5,89]|cycle : PH2, T1 libre, IRQ raccordée|
|6845|110/0|110/0|64,40 [59,37–68,86]|47,44 [47,13–48,92]|caractère : balayage80 colonnes, MA/RA/DE/CU observés|
|74LS374|10/0|10/0|44,76 [44,40–47,90]|30,59 [29,14–30,93]|capture : données alternées, sortie observée|
|74LS165|36/0|36/0|69,74 [65,56–77,82]|27,86 [27,73–30,20]|cycle : série alternée, QH observé|
|MM52116|13/0|13/0|158,97 [149,22–178,63]|10,14 [9,92–105,69]|transaction : adresses variables, sortie après450 ns|
|VDU80|14/0|14/0|1063,05 [1042,65–1088,47]|116,02 [109,68–124,98]|caractère : 6 dots par caractère, glyphe variable, vidéo observée|
|Curseur RC|7/0|7/0|117,82 [112,73–122,95]|23,74 [22,32–26,16]|transition : entrées alternées, réponse calculée, sortie observée|
|74S163|18/0|18/0|38,86 [36,10–45,32]|4,92 [4,48–5,91]|cycle : comptage continu, toutes sorties observées|
|74LS74A|11/0|11/0|175,81 [169,08–186,65]|16,15 [14,22–24,14]|période double : deux canaux, données alternées, Q observées|
|SystemClock|60/0|60/0|126,17 [118,31–143,88]|17,29 [16,87–39,12]|cycle source : réseau24 MHz, sorties2/4 MHz raccordées|
|SystemCpuClock|13/0|13/0|112,51 [97,50–117,73]|12,52 [10,35–13,91]|cycle : 2 MHz, CPU et READY raccordés, sans WAIT|
|74LS393|54/0|54/0|103,06 [99,79–109,82]|19,76 [19,20–20,55]|cycle source : QA/QD observés, QD cascade externe|
|74LS93|32/0|32/0|236,21 [224,08–257,00]|33,20 [30,79–41,84]|cycle source : QA vers CKB externe, QB/QD observés|
|74LS00|17/0|17/0|284,10 [264,84–299,03]|35,54 [35,36–48,22]|transaction4 portes : 4 portes alternées, sorties observées|
|7438|17/0|17/0|262,72 [253,73–265,72]|32,70 [31,71–43,60]|transaction4 portes : 4 portes alternées, sorties open-collector observées|
|74LS138|19/0|19/0|185,90 [171,10–201,23]|22,03 [21,86–22,44]|adresse : adresses variables, Y1 seule raccordée|
|CD4020B|23/0|23/0|114,67 [113,41–119,65]|18,33 [17,50–20,47]|cycle : toutes sorties observées, source15625 Hz|
|CD4013B|24/0|24/0|253,55 [237,35–287,42]|35,20 [31,88–38,67]|période double : deux canaux, données alternées, sorties observées|
|8271|168/0|168/0|23,50 [22,48–30,09]|3,91 [3,91–4,28]|cycle : Tick au repos, aucune synthèse mécanique|
|8271 actif|mêmes168/0|mêmes168/0|91,56 [84,67–100,93]|18,19 [16,79–40,58]|cycle : recherche série sans index, NRDATA500 ns/4 us et DataWindow|

Les portes74LS00/7438, le décodeur74LS138, la ROM MM52116 et le circuit RC n’ont pas d’oscillateur interne. Leur unité est une transaction/transition, pas un cycle inventé. Une transaction de portes dans ce banc sollicite quatre portes ; un million de transactions représente quatre millions de mises à jour de portes. Une période double de74LS74A/CD4013B sollicite les deux canaux. Un caractère VDU80 contient six périodes de dot. La ROM reçoit une adresse toutes les500 ns avec délai d’accès450 ns du profil existant.

Les fréquences du montage restent : réseau24 MHz,74S16324 MHz, dot12 MHz, CPU/VIA/caractère2 MHz ;3934 MHz puis250 kHz ;93 IC144 MHz puis IC72/1 MHz ;402015625 Hz ;4013B index5 Hz nominal (fronts reçus du lecteur). Les74LS74A sont dans le contrôle WAIT/READY du CPU. Aucun débit hôte ne modifie ces cadences émulées.

Le8271 actif est mesuré en recherche série sans index, avec NRDATA500 ns toutes les4 µs et DataWindow ; BUSY reste vérifié sur chaque fenêtre. Ce n’est pas un débit de lecture de fichiers ni le coût de la carte400,004 complète. Le benchmark SystemClock utilise ses sorties2/4 MHz explicites ; la machine utilise aussi ses descriptions et regroupements qualifiés. Le VDU80 isolé traite des glyphes variables à chaque caractère ; la vraie image NOS80 contient des portions stables. Ces différences expliquent pourquoi ces coûts ne prédisent pas directement la cadence assemblée.

Variabilité : une première collecte20 scénarios avant ajout du8271 actif donnait notamment73,88 ms pour6502A et145,44 ms pour74LS74A en Debug. La collecte finale ci-dessus est différente sans modification de leur production. Cette variation entre collectes n’est ni un gain ni une régression de code établie ; les comparaisons avant/après valides restent les relevés appariés du même protocole. Les min/max de cinq fenêtres ne mesurent pas à eux seuls toute la variation entre sessions. La collecte finale avec chauffe renforcée remplace ici les médianes de la collecte courte, conservée dans les captures. Des fenêtres hautes restent visibles en Release (ROM, générateur et8271 notamment) ; elles ne sont pas supprimées des min/max. La cause exacte de la dispersion n’a pas été isolée ; ne pas l’attribuer entièrement au JIT ni en déduire un gain de code.

## Machine complète

| Validation actuelle | Debug réussis/échoués | Release réussis/échoués | Portée |
|---|---:|---:|---|
|Composants autonomes|1396/0|1396/0|Familles des20 types ci-dessus|
|Régression System et carte|191/0|191/0|48 System,28 RAM/IO partagés avec les autres System,4 clocks,12 monochrome,25 préfixes,11 transferts disquette,63 carte400,004|
|Banc natif NOS80/BASIC|1/0|1/0|Trois paires neuves, résultats invités et états comparés ; aucune interface RML|

Le sous-ensemble RAM/IO concerne notamment les cartes System1–4 : les191 cas sont une régression de famille et de carte, pas191 essais spécifiques du seul System5. Les11 transferts vérifient lecture/écriture/relecture un/dix secteurs, comparaisons NMI/phase CPU et variantes progressif/entrelacé. Les tests de préfixes vérifient notamment les IRQ/timers, clavier, retours de modes de contrôle et frontières vidéo.

Protocole natif :40 champs de chauffe ; CAT512 octets, BASIC4096 octets puis PRINT12+34=46 par clavier ; dix champs de stabilisation et un champ séparé pour les annonces IC7 ;120 champs chronométrés par instance, trois paires de versions strictement identiques. Ordre alterné. RunFrame et DrainAudio inclus ; comparaisons CPU/registres/cycles, RAM, vidéo/audio et états des deux lecteurs hors chronométrage.4608 octets lus, zéro écrit, empreinte SSD inchangée après libération. Les720 échantillons par configuration ci-dessous regroupent les deux instances identiques ; leurs colonnes reference/current dans les captures ne sont pas un avant/après optimisation.

La durée en ms/champ est mesurée. FPS, MHz équivalents et vitesse nominale sont **calculés**, pas mesurés indépendamment : FPS=1000/ms ; vitesse=19,968/ms ; MHz équivalents=2×vitesse, soit39936 périodes CPU/champ sur ce montage. La référence matérielle est50,080 Hz/19,968 ms par champ. Ces MHz expriment un débit hôte équivalent en calcul libre, pas une modification de l’horloge CPU2 MHz. Ni synthèse mécanique ni worker/WASAPI ne sont présents dans le banc natif ; les FPS présentés par RML ne sont pas mesurés ici.

| État lecteur | Champs/mode | Debug ms/champ | FPS calculés | MHz équivalents | Nominal | Release ms/champ | FPS calculés | MHz équivalents | Nominal |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
|Ensemble mixte|720|28,624|34,94|1,395|69,76 %|3,875|258,04|10,305|515,26 %|
|Moteur actif|288|40,781|24,52|0,979|48,96 %|5,763|173,52|6,930|346,48 %|
|Moteur arrêté|426|20,446|48,91|1,953|97,66 %|2,605|383,81|15,328|766,39 %|
|Arrêt pendant le champ|6|25,671|38,95|1,556|77,78 %|3,421|292,28|11,673|583,63 %|

**Cadence Debug insuffisante moteur actif.** La Release tient le budget19,968 ms dans ce scénario avec une marge importante ; cela ne valide pas automatiquement la présentation RML ni tous les logiciels. Le mélange actif/arrêté dépend du protocole et ne doit pas remplacer les deux cas séparés.

## Comparaisons avant/après établies

Les paires ci-dessous sont des mesures historiques **Debug** sur les protocoles décrits dans les fiches et captures. Elles ne s’additionnent pas et ne définissent pas une accélération cumulée. Pas de comparaison historique Release avant/après équivalente établie pour ces optimisations ; la colonne Release de ce bilan est un relevé actuel, pas la preuve d’un gain de code.

| Bloc conservé | Avant | Après | Gain de durée | Conditions |
|---|---:|---:|---:|---|
|74LS393|223,576 ms|155,154 ms|103,06 [99,79–109,82]|19,76 [19,20–20,55]|
|74LS93|174,644 ms|144,641 ms|236,21 [224,08–257,00]|33,20 [30,79–41,84]|
|CD4013B|258,286 ms|182,836 ms|253,55 [237,35–287,42]|35,20 [31,88–38,67]|
|74LS138 Y1|171,027 ms|136,727 ms|20,1 %|1M changements, sortieY1 seule, historique de la fiche|
|System5 : échéances des seules sorties138 raccordées|45,854 ms/champ|34,129 ms/champ|25,571 %|Banc historique natif, protocole dans system5-validation|
|System5 : portions mémoire/VIA mixtes|42,78939 ms/champ|32,90128 ms/champ|23,109 %|NOS80/BASIC apparié360 champs|
|System5 : périodes93 et masque NAND combinés|33,36078 ms/champ|30,67188 ms/champ|8,060 %|NOS80/BASIC apparié360 champs ; gain non attribué à une sous-partie|
|System5 : borne de ligne plutôt que plafond16|30,89337 ms/champ|28,44413 ms/champ|7,928 %|NOS80/BASIC apparié360 champs|

Les autres lignes sans paire comparable ne reçoivent pas un gain inventé. Les résultats complets, variantes, essais négatifs et conditions historiques restent dans les fiches de chaque composant et dans system5-validation.md.

Candidats récents retirés : fin de pulse connecteur31,69173→32,67212 ms/champ (+3,094 %) ; cible mémoire préparée28,24036→27,08960 (−4,075 %, actif−2,967 %) ; extension mémoire/VIA33,46680→32,10921 (−4,057 %, actif−3,052 %). Ces réductions de décodage n’ont pas apporté le gain utile visé et ne sont pas présentes dans le code conservé. Les40 contrôles qui leur étaient propres passaient, mais sont retirés avec leurs flags/helpers ; les191 validations actuelles portent sur la production conservée.

## Fidélité et points ouverts

| Domaine | Évidence disponible | Limite ou travail restant |
|---|---|---|
|CPU/VIA et bus|Tests de cycles/transactions, IRQ/NMI, préfixes, contrôles mémoire et horloges ; même exécution invitée|Les bancs ne certifient pas tous les logiciels ou montages physiques ; essai RML actuel restant|
|Clocks/TTL et propagation|Captures partagées, compteurs, ripple, reset, sorties observées et phases couverts par les tests ; aucune fréquence changée|Délais QB/QC et certains profils sont calculés/documentés, pas des mesures électriques de carte réelle|
|VDU80|Progressif/entrelacé, phases, CRTC, pixels, délais ROM/NL/curseur couverts par les validations|MM52116 utilise un accès maximal450 ns ; NL20 ns typiques et seuil RC nominal calculé, non calibration analogique réelle|
|Carte et disquettes|Lecture/écriture/relecture, flux et fenêtres, NMI datés, têtes, index, retrait et protections testés|Cadence Debug moteur actif environ49 % sur le scénario natif ; essai RML lecture/écriture/son demandé avant clôture|
|Sons mécaniques|Worker commun distinct ; ancien comparatif de génération0,13915 ms/bloc actif|Verrou partagé avec les commandes et horodatages émulés absents ; aucune contention chiffrée. N’explique pas le profil natif sans rendu mécanique|
|Validation utilisateur|Essais antérieurs documentés ; aucun RML lancé pendant cet audit|FPS présentés, écoute réelle et interactions de la livraison Debug à contrôler avec l’utilisateur|

Le dernier profil exhaustif antérieur, sur la même production, donnait moteur actif CPU/VIA12,521 ms, vidéo7,129 ms, résolution source0,060 ms, carte18,557 ms, résiduel0,232 ms, total instrumenté38,500 ms. Catégories exclusives de temps mural ; pas des temps CPU propres des seules puces, ni des coûts obtenus en additionnant parents/callbacks. Le sans-son mécanique rendait déjà ces18,557 ms : le rendu audio n’est pas leur cause. Ce profil localise la charge mais ne prouve pas qu’une méthode parente puisse être supprimée à hauteur de son attribution.

La machine reste ouverte pour son essai Debug et sa décision de clôture. Aucun manque fonctionnel n’est masqué par la tolérance de performance, aucune puce n’est déclarée électriquement certifiée à100 % par la seule réussite des tests. Sauvegarde/chargement d’état toujours exclus du périmètre.

## Preuves et nettoyage

Captures ignorées sous `artifacts/TestsChips/System5Components/Acorn System 5/` : `20261110-audit-components-{Debug,Release}.trx` et `20261110-component-performance-warm-{Debug,Release}.{json,trx}` (collecte finale), avec la collecte courte `20261110-component-performance-{Debug,Release}.{json,trx}` conservée séparément. Captures natives sous `artifacts/TestsMachines/Acorn/Acorn System 5/` : `20261110-audit-machine-{Debug,Release}.trx` et `20261110-performance-audit-{Debug,Release}.{json,md,trx}`. Les médianes/min/max ne sont pas des estimations ; les conversions de débits le sont au sens de valeurs calculées à partir de ces durées, sans nouvelle mesure indépendante.

Les deux bancs et la sélection temporaire de sources Release sont supprimés. Assemblies de tests reconstruits sans banc en Debug/Release ; Debug retrouve la composition complète habituelle. La sélection Release permettait de compiler uniquement les mêmes sources Acorn/Intel8271 nécessaires, sans reconstruire les DLL anciennes Epoch/Amstrad dont les API ne correspondaient plus à leurs tests ; même filtre191 cas et mêmes sources Acorn qu’en Debug. L’erreur initiale de compilation des tests transversaux n’est pas un échec d’un test System5. L’assemblage Release d’audit est limité à ces sources ; aucun fichier de ces autres modules n’a été modifié. Module Acorn reconstruit0 erreur/0 avertissement ; avertissements de tests préexistants :1 Chips,4 Local Debug. Aucun worker audio/WASAPI ou fenêtre native créé par ces bancs.

Livraison Debug construite avec `scripts/local-building.cmd --building=debug --modules=A` : succès du script, exécutable `F:\GW GUI\build\Debug\Retro Media Lab\retromedialab.exe` et24 modules vérifiés contre leurs24 manifestes, aucun manquant. Module Acorn présent dans `Modules/acorn/retromedialab.emulation.acorn.dll`. Le build n’a pas ouvert RML ; l’essai utilisateur reste attendu avant clôture.

## Point ouvert révélé par l’essai RML

Parcours logiciels ajoutés depuis cet audit initial : System BASIC chargé, PRINT12+34=46, programme10 PRINT123456/20 END/RUN et pavé7*6=42 en saisie lente. FORM80 original termine Formatted puis Verified00–4F et CAT1 vide dans RML, mais à9 FPS/18 % pendant l’opération puis31,6 FPS/63 % après ; Ctrl+L rétablit50,2 FPS/100 % sans reset. Captures et conditions dans [la validation](system5-validation.md). Diagnostic du même utilitaire avec carte/horloges ordinaires : environ31,63 s émulées jusqu’à Formatted, puis23,203 s jusqu’à Verified ; aucune durée physique indépendante trouvée sur le Web, ne pas confondre ces chiffres avec les deux minutes par phase rapportées en temps hôte.

Comparaison native Debug sur cinq vrais CAT DOS, moteur arrêté : référence28,855304 ms/champ contre15,374770 après Ctrl+L. Le surcoût du contenu est donc aussi présent sans transmission à App. Paquets dot proposés :28,991930 et15,453189, respectivement+0,473 % et+0,510 % ; candidat retiré. Égalité vidéo/audio/CPU/RAM/parité/durée à chaque champ, source DOS inchangée. Ces deux mesures nouvelles ne remplacent pas la moyenne mixte720 champs et ne décrivent pas le coût d’une méthode isolée. Pas de mesure Release de cet écran chargé ni de FORM80 en performance isolée. La machine demeure ouverte ; aucune comparaison de petits calculs BASIC présentée comme essai de jeu.
La saisie normale de basic donne bc ; la saisie lente permet calculs et programme, mais ne valide pas le clavier normal. SystemKeyboardRepeat, effectivement utilisé, était omis du premier tableau20 types ; ses tests/débits doivent compléter ce bilan, ainsi que le contrôleur de sortie corrigé. Les1396 tests/191 intégrations et mesures ci-dessus portent sur la production avant cette correction. Le raccordement de la nouvelle touche et de la brève transition AKD prévue au manuel est en cours ; ne pas annoncer cette machine terminée sur le seul bilan précédent.

Formatage RML en Debug : l’utilisateur observe9 FPS,0,36 MHz/18 % après YES dans FORM80 sur lecteur1. Instantané d’écriture non représenté par le scénario natif CAT/BASIC précédent ; ajouter un relevé de formatage Debug/Release avant bilan final de toutes les charges. Aucun résultat Release d’écriture inventé.
