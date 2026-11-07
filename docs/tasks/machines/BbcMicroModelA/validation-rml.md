# BBC Micro Model A — essais progressifs dans RML Debug

## Retour utilisateur — 7 novembre 2026

L'utilisateur confirme « ça fonctionne » en réponse explicite à la vérification des clics cassette et de l'effet des deux volumes, machine et lecteur. Ces deux points d'écoute sont donc validés par son essai du build livré, sans prétendre avoir enregistré sa sortie Windows ou obtenu une nouvelle capture. Ne pas lui redemander le même essai ni lancer le banc audio Windows envisagé uniquement pour obtenir cette confirmation.

Cette validation audio ne remplace pas les autres preuves matérielles et ne clôt pas les budgets Debug WD/HDD documentés dans le README. Les anciennes mesures, captures et cases ci-dessous restent leur historique ; aucun nouveau FPS utilisateur n'a été fourni dans ce retour.

## Ordre de réalisation

- [ ] Valider le Model A dans RML avant de passer à l'icône suivante.
  - [ ] Équipement minimum réellement disponible.
    - [x] Créer `artifacts/.tmp-rml-input/SendKeys.ps1` pour la saisie Windows autorisée par l'utilisateur : contrôler la fenêtre au premier plan, maintenir et relâcher chaque touche dans `finally`, sans modifier RML.
    - [x] Modifier ce document avec la configuration minimale sélectionnée dans RML : CPU, RAM, ROM, interfaces, lecteurs imposés ou retirés, rendu vidéo ; relever la cadence au repos et sous charge graphique avec captures sous `artifacts/TestsChips/<Chip>/BBC Micro Model A/`.
  - [ ] Ajouter la cassette, puis la disquette.
    - [ ] Modifier ce document avec le lecteur cassette ajouté, le média compatible réellement exécuté, les commandes de chargement, les FPS/MHz pendant le chargement et pendant le jeu actif, le rendu, le son et les éventuels défauts.
    - [ ] Modifier ce document avec le contrôleur et le lecteur de disquette ajoutés en conservant la cassette : démarrage d'un logiciel exigeant compatible, accès disque, jeu actif, cadence, commandes, résultats et captures réelles.
    - [x] Modifier ce document avec un essai de la même disquette après sélection de 32 Kio dans RAM : distinguer le retour au menu observé en 16 Kio du résultat réellement obtenu en 32 Kio, sans attribuer de cause non démontrée.
  - [ ] Ajouter le disque dur et les extensions disponibles.
    - [x] Modifier `artifacts/.tmp-rml-input/SendKeys.ps1` pour envoyer également les touches de navigation Windows autorisées à une fenêtre RML du même processus, sans activation forcée et avec relâchement dans `finally` ; consigner la sélection effective du support ROM dans ce document.
    - [x] Créer `artifacts/.tmp-rml-media/ModelA-HardDisk0.dat` et `ModelA-HardDisk0.dsc` par copie des deux fichiers `Documents/Retro Media Lab/Emulation/HDD/Acorn/HardDisk0.*`, pour les écritures invitées sans modifier les originaux ; consigner la ROM ADFS sélectionnée dans ce document.
    - [x] Modifier `artifacts/.tmp-rml-input/SendKeys.ps1` pour saisir les chemins des dialogues Windows en Unicode, sans dépendre des modificateurs du clavier invité ; conserver les appuis physiques pour les commandes BBC.
    - [x] Créer `artifacts/.tmp-tube/Hdd.cs` : reproduire en Debug la commande ADFS avec les ROM sélectionnées et la copie DAT ouverte en lecture seule, relever le PC et les états SCSI sans sortie audio Windows, détacher et libérer l'image dans `finally` ; écrire les résultats ici puis supprimer ce diagnostic et ses sorties temporaires après utilisation.
    - [x] Modifier ce document avec l'interface et le disque dur ajoutés, son accès réel et une copie de fichier depuis disquette : fichiers source/destination, vérification de lecture, cadence et anomalies ; utiliser une copie de travail pour les écritures de test.
    - [ ] Modifier ce document avec les options matérielles réellement proposées, dont l'extension RAM : combinaison testée, logiciel exigeant compatible exécuté et mesures en jeu avec son et accès médias.
  - [ ] Clore les essais du modèle.
    - [ ] Modifier ce document et `README.md` avec le bilan réel de chaque configuration, les problèmes corrigés ou encore ouverts et les captures conservées ; supprimer les médias de travail, `artifacts/.tmp-rml-input/SendKeys.ps1` et les fichiers temporaires de test après utilisation, restaurer les réglages empruntés et libérer les seules ressources possédées dans `finally`.

## Conditions

Essais dans le build Debug avec tous les modules livré le 6 novembre 2026. Une seule machine émulée et aucun autre banc ni build pendant les relevés. Le logiciel et ses exigences mémoire/matériel doivent correspondre à la configuration testée ; un logiciel incompatible avec le matériel minimum ne constitue pas une preuve de panne.

Les jeux sont testés en fonctionnement actif, avec animations/défilement, entrées utilisateur et son. Un écran de titre ou le seul démarrage de la ROM ne valide pas la charge réelle. Les résultats sont ajoutés après chaque passage, dans ce fichier propre au modèle.

## Résultats

### Premier scénario : 16 Kio, aucun lecteur

Configuration modifiée dans l'interface puis vérifiée dans sa sauvegarde : RAM 16 Kio ; cassette, Floppy0 et Econet désactivés ; Solidisk désactivé ; aucun disque dur connecté, aucune extension mémoire et aucun média monté. ROM MOS 1.20, BASIC II et générateur SAA5050 conservés. La ROM DFS 0.9H précédemment sélectionnée est encore annoncée au démarrage, bien que le lecteur ait été retiré ; ce point est constaté, pas considéré comme une validation d'interface disque.

Le démarrage atteint `BBC Computer 16K`, `Acorn DFS`, `BASIC` puis l'invite. Relevés affichés : 50,0–50,2 FPS, 2,00–2,01 MHz, 100 %, sortie 640 × 512, Direct3D 11. Aucun build ni banc parallèle pendant ces relevés.

Capture réelle : `artifacts/TestsChips/Mos6502/BBC Micro Model A/16k-no-devices-idle-debug.jpg` (ignorée par Git). Elle prouve ce démarrage et ce relevé au repos seulement. Les commandes graphiques, jeux et essais de lecteurs ne sont pas encore validés. La validation utilisateur antérieure ne remplace pas ce nouveau passage demandé.

### Saisie et charge graphique du scénario minimum

La saisie Windows maintenue fonctionne : `PRINT 12+34` renvoie `46`. Après `MODE 7`, `PRINT HIMEM,PAGE,TOP` renvoie `15360,6400,6400`. Les premières commandes combinées ont produit `Bad MODE`, `Syntax error` ou `Bad program` ; elles ne constituent pas des essais graphiques réussis et aucune cause matérielle n'est attribuée à ces messages.

Après les commandes séparées `PAGE=3584:NEW`, puis `NEW`, puis `MODE 4`, le dessin continu `FOR I=0 TO 1000000:DRAW RND(1280),RND(1024):NEXT` s'exécute réellement et remplit l'écran de lignes. Relevé visible sous charge : **50,1 FPS, 2,00 MHz, 100 %**, sortie 640 × 256. Capture réelle : `artifacts/TestsChips/Mos6502/BBC Micro Model A/16k-no-devices-draw-debug.jpg`. Ce résultat valide ce dessin BASIC ; il ne remplace pas les prochains essais de jeux et de médias.

### Cassette seule, 16 Kio : chargement en cours

Lecteur Cassette0 ajouté depuis Stockage ; toujours aucun lecteur disquette ni HDD, RAM 16 Kio. Média inséré par le sélecteur natif : `F:\Retro\ef_tosec_backup_2024\Acorn BBC - Games - [UEF] (TOSEC-v2023-08-29)\Arcade Action v2.5 (19xx)(Acornsoft)\Arcade Action v2.5 (19xx)(Acornsoft).uef`.

Après `*TAPE`, puis `CHAIN ""`, l'écran affiche `Searching`, `Loading`, puis `INDEX 02` : les blocs cassette sont réellement lus. Relevé sous chargement : **36,6 FPS, 1,46 MHz, 73 %**. Après insertion mais avant la demande de chargement, un relevé stabilisé affichait encore 50,0 FPS et 2,00 MHz. La capture d'insertion isolée affichait temporairement 40,3 FPS ; elle n'est pas retenue comme cadence stabilisée.

Capture conservée : `artifacts/TestsChips/Mos6502/BBC Micro Model A/16k-cassette-loading-debug.jpg`. La baisse sous lecture cassette est reproduite, sa cause n'est pas encore isolée. Le jeu actif, son exigence mémoire et le son restent à vérifier ; le scénario cassette n'est donc pas coché comme terminé.

### Invaders exécuté après chargement cassette

Le menu Arcade Action est atteint. Choix `1` (Invaders), confirmation Entrée, puis `1` joueur et difficulté `9` : partie réellement démarrée, rangées d'ennemis, projectiles, bases et progression jusqu'à Game Over. Relevés visibles pendant la partie : **49,9–50,0 FPS, 1,99–2,00 MHz, 100 %**. La baisse à environ 37–38 FPS apparaît pendant les blocs cassette, puis la cadence réelle revient une fois le programme chargé.

Capture de partie active : `artifacts/TestsChips/Mos6502/BBC Micro Model A/16k-cassette-invaders-level9-debug.jpg`. Des appuis `Z`, `X` et espace ont été envoyés, mais leur correspondance aux commandes du jeu et l'efficacité des tirs ne sont pas établies par ces seules captures. Aucun constat auditif n'est affirmé. Cet essai prouve l'exécution animée d'un jeu en 16 Kio après lecture UEF ; il ne valide pas encore le son ni tous les contrôles.

### Cassette et disquette, 16 Kio : accès DFS et logiciel

Floppy0 ajouté (profil `ds80`), Cassette0 conservé. Disquette : `F:\Retro\ef_tosec_backup_2024\Acorn BBC - Games - [SSD] (TOSEC-v2024-01-15)\Model A Invaders (1982)(IJK)[INVA]\Model A Invaders (1982)(IJK)[INVA].ssd`.

`*CAT` lit bien le catalogue `model a`, lecteur 0, fichier `INV/A`, puis rend l'invite. `*RUN INV/A` rend l'invite ; `CHAIN "INV/A"` lance effectivement le menu du programme BASIC et ses instructions. Cadence affichée autour de 50,0–50,2 FPS et 2,00 MHz. Les options 4 puis 1 atteignent les instructions ; une touche à l'écran « Press any key to continue » ramène au menu sans partie observée. **Le jeu sur cette disquette n'est pas validé en 16 Kio.** La mémoire occupée par DFS est une piste à vérifier, pas une cause prouvée. Le prochain passage compare ce même média en 32 Kio.

### Comparaison de la même disquette en 32 Kio

Seul le choix de mémoire principale est passé de 16 à 32 Kio dans RAM, puis arrêt/allumage : la ROM annonce bien `BBC Computer 32K`. Même disquette et même commande `CHAIN "INV/A"`, option `4` (fast missiles + invaders), puis espace après les instructions : **la partie démarre cette fois**, avec ennemis, bases et projectile visibles. Cadence sur la capture de partie : **50,1 FPS, 2,00 MHz, 100 %**. Le programme atteint ensuite son écran de fin de partie. Le résultat diffère donc selon la quantité de RAM avec cet environnement DFS ; aucune modification de code n'a été faite pour ce comparatif.

Captures : `artifacts/TestsChips/Mos6502/BBC Micro Model A/16k-floppy-catalog-debug.jpg` et `artifacts/TestsChips/Mos6502/BBC Micro Model A/32k-floppy-invaders-active-debug.jpg`. La correspondance Z/X/espace est indiquée par le jeu. Des appuis ont été envoyés ; le score est resté nul et le tir efficace n'est pas considéré comme démontré. Son et volume non validés auditivement.


### Préparation du HDD et de sa ROM

Le HDD a été ajouté par l'utilisateur pendant les essais. Le support BooBip 32K RAM / 32K ROM × 1 est ensuite sélectionné dans Extensions pour disposer du support IC52 destiné à la ROM ADFS, en conservant DFS 0.9H et le contrôleur 8271. Les appuis Windows maintenus fonctionnent aussi pour la navigation des listes ; ils sont limités au processus RML actif et les touches sont relâchées dans finally.


ROM sélectionnée dans IC52 : Acorn ADFS 1.30, fichier Acorn ADFS v1.30 [variant 1].rom. Les copies DAT/DSC de travail ont été créées ; le HDD ajouté par l'utilisateur référence encore son image habituelle lors du premier essai de lecture. Aucune écriture invitée ne sera lancée sur cet original.

Premier redémarrage avec cassette, disquette et HDD connecté : 50,1 FPS, 2,00 MHz, 100 %. `*ADFS` reste en attente sans rendre l'invite, puis Échap rend bien `Escape` et l'invite ; le catalogue HDD n'est pas validé. Capture `artifacts/TestsChips/Mos6502/BBC Micro Model A/32k-hdd-adfs-wait-debug.jpg`. Comparaison suivante : sélectionner le WD1770 déjà proposé, en conservant les deux ROM sélectionnées, pour distinguer le problème de sélection du contrôleur de l'accès Winchester. Aucune cause de l'attente n'est encore établie.
Le diagnostic de carte Debug, avec les mêmes ROM (ADFS banques 8/12, Hope DFS banque 14) et la copie DAT en lecture seule, lit 21 blocs HDD et rend l'invite après `*ADFS`. Au repos : cible non occupée, aucun REQ/SEL/ACK et aucune échéance Winchester armée. La fenêtre RML, elle, finit par afficher `Drive not ready`. Il faut donc vérifier le média réellement repris par la session ouverte lors de l'ajout du HDD ; aucune panne de la puce n'est attribuée à cette différence. Le premier lancement du diagnostic avait une erreur dans les banques de son montage de test (0/1 au lieu de 8/12), corrigée avant le résultat ci-dessus.
Après fermeture du seul onglet de session et réouverture par double-clic sur le Model A, sans changement de puce ni de ROM supplémentaire, `*ADFS` rend immédiatement l'invite puis `*CAT` affiche le catalogue du lecteur 0 : répertoire `$`, option 00, catalogue vide. Cadence : 50,0–50,2 FPS, 2,00 MHz, 100 %. Capture `artifacts/TestsChips/Mos6502/BBC Micro Model A/32k-hdd-catalog-debug.jpg`. Le média ajouté depuis les réglages n'était pas repris par la liste des médias de la session déjà ouverte ; ce défaut de rafraîchissement reste à corriger. La lecture native et la vraie fenêtre concordent après réouverture.



### Copie disquette vers HDD et exécution depuis HDD

La copie DAT/DSC `artifacts/.tmp-rml-media/ModelA-HardDisk0.*` est sélectionnée dans Stockage, puis le seul onglet Model A est fermé et rouvert pour reprendre les médias configurés. Le sélecteur Windows nécessite une saisie Unicode des chemins : les modificateurs du helper physique donnaient un chemin incorrect ; cette erreur d'outillage ne concerne pas le chargement des médias dans RML.

Commandes réellement exécutées : `*DISC`, `LOAD "INV/A"`, `*ADFS`, `SAVE "INVADER"`, `*CAT`. Le catalogue ADFS du lecteur 0 affiche désormais `INVADER WR`. Puis `NEW` efface le programme BASIC en RAM ; `CHAIN "INVADER"` relit le HDD et ouvre le menu du jeu. Option 4, Entrée, puis espace : partie démarrée, ennemis et projectiles animés. Cadence de la capture en partie : **50,2 FPS, 2,00 MHz, 100 %**. Les captures prises après LOAD/SAVE montrent temporairement 47,3 FPS / 1,89 MHz ; celle du catalogue 48,6 FPS / 1,94 MHz. Ces relevés ponctuels ne mesurent pas isolément le débit de transfert.

Preuves : `artifacts/TestsChips/Mos6502/BBC Micro Model A/32k-hdd-copy-catalog-debug.jpg` et `32k-hdd-invaders-active-debug.jpg`. Les écritures de cet essai visent la copie de travail ; aucun test d'écriture n'a été exécuté sur l'image HDD originale. Ce passage valide la copie du programme BASIC et son exécution depuis ADFS ; son, volume et persistance après fermeture restent à vérifier.
Après arrêt normal du Model A, la copie DAT est bien écrite sur le disque hôte : SHA256 940B91116DF077B0C2F0040A2960193314E46D7B4614A9C07BA5A65FA1A2CF0E. L'original conserve son SHA256 initial 7A6F34131AF3C7F7EE66CD2F2F519000703D3968A48E62CEB5AC5CEBDEA80CAA.

Après remise en marche, `*ADFS` puis `CHAIN "INVADER"` retrouve effectivement le programme enregistré et son menu. Capture `32k-hdd-reload-after-power-cycle-debug.jpg` : 50,1 FPS, 2,00 MHz, 100 %. La persistance après arrêt est donc vérifiée. Le diagnostic temporaire Hdd.cs et ses sorties exécutables ont été supprimés après consignation.

### Observation passive de l'essai utilisateur après les corrections audio

Le build Debug tous modules s'est terminé avec succès (24 modules). Après redémarrage de DWM demandé par l'utilisateur, RML est lancé par lui : aucune fermeture, saisie ni commande de build pendant son essai. Model A 32 Kio, Hope DFS Plus, cassette/disquette/HDD visibles : repos 49,9 FPS / 1,99 MHz / 100 %. Last Ninja II affiche d'abord 33,0 FPS / 1,32 MHz / 66 % pendant un menu Émulation ouvert, puis 49,9 FPS / 1,99 MHz / 100 % en jeu, menu fermé. Ces instantanés ne suffisent pas à attribuer la baisse au chargement ou au focus. Captures réelles : `32k-after-audio-fix-idle-debug.png`, `32k-last-ninja-menu-open-debug.png`, `32k-last-ninja-active-debug.png`, dans `artifacts/TestsChips/Mos6502/BBC Micro Model A/`.

Pendant l'observation reprise, l'utilisateur exécute `*TAPE`, puis `*CAT`. Le catalogue cassette affiche `INDEX 06 06D5` et `INVADERS 00`, avec 47,3 FPS / 1,89 MHz / 94 %. Capture `32k-after-audio-fix-tape-catalog-debug.png` conservée. Ce relevé reste inférieur à la vitesse réelle cible ; la lecture cassette n'est pas déclarée entièrement finalisée. L'utilisateur signale également des clics répétés du lecteur : le correctif de réserve audio est écrit et testé, mais n'est pas encore livré ni écouté dans RML.

### Livraison Debug et vérification du bloc de correctifs

Le script `scripts/local-building.cmd --building=debug --modules=A` s'est terminé avec succès : les 24 modules sont présents dans `build/Debug/Retro Media Lab/Modules`. Le binaire MediaEngine livré dans `lib` a la même empreinte que celui compilé depuis les sources du correctif. Les 65 tests locaux et les 17 tests session/mélangeur Debug passent.

Une session de vérification a été ouverte puis fermée proprement, sans processus RML restant. La configuration actuelle annonce BBC Computer 52K / Hope DFS Plus : aucun changement de configuration n'a été fait pendant ce passage. Au repos, le cadre fixe sous la barre montre 50,0 FPS / 2,00 MHz / 100 %. Le bouton paramètres ouvre les réglages Acorn avec le Model A sélectionné. Le raccourci F4 ouvre la même fenêtre, mais l'utilisateur a ensuite refusé cette affectation : elle n'est pas validée et reste présente dans le build livré, en attendant la révision commune des touches.

`*TAPE`, puis `*CAT` lisent INDEX et INVADERS. Pendant le catalogue : 47,0 FPS / 1,88 MHz / 94 %, puis 47,8 FPS / 1,91 MHz / 96 %. Ce ralentissement persiste ; il n'est pas considéré comme corrigé par les tests audio. L'absence de clics à l'écoute reste à confirmer par l'utilisateur. Aucun avertissement ROM n'a été provoqué artificiellement dans sa configuration ; le diagnostic et la pastille sont vérifiés par les tests autonomes.

Captures sous `artifacts/TestsChips/Mos6502/BBC Micro Model A/` : `correctifs-commandes-cadre-debug.png`, `correctifs-f4-parametres-machine-debug.png`, `correctifs-lecture-cassette-debug.png`. Ces images sont ignorées par Git. L'utilisateur reprend la main : aucune autre action dans l'interface ni aucun nouveau build pendant son essai.
