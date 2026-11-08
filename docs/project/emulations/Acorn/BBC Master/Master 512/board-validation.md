# Master 512 — vérifications du 8 novembre 2026

## Configuration commune

Banc natif hors RML, assemblages Debug actuels, MOS 3.20, ROM coprocesseur `80186 Tube Client 1.01.rom`, 512 Kio de RAM parasite, hôte à 2 MHz et 80186 à 10 MHz. Banque ADFS choisie dans la CMOS locale du banc ; aucune configuration utilisateur modifiée. Images ouvertes en lecture seule, disque détaché puis libéré dans `finally`.

Pour les démarrages sur disquette : 80 champs d'initialisation, SHIFT-RESET, 1 500 champs avec rendu et drainage audio. Relâchement de SHIFT après 120 champs. Ces passages de diagnostic ne mesurent pas la capacité maximale de la machine.

## Identification de la disquette initiale

Le fichier temporaire préexistant `RetroMediaLab-80186-boot.img` contient 655 360 octets. SHA-1 `2df6155aecfd4fc9595cd95359b153d158c313a3`, SHA-256 `f8f4a2de37424dda4c327fc5824d1232a24d9a1bd39b604b0aa71da84891b103`.

Son SHA-1 correspond exactement à **DOS Plus Boot v1.2** dans le [catalogue source MAME](https://github.com/mamedev/mame/blob/master/hash/bbc_flop_80186.xml), et non à la 2.1. Une seconde copie récupérée dans [Disc1.zip chez G7JJF](https://www.g7jjf.com/512/Disc1.zip) est identique octet pour octet par son empreinte. Le catalogue et l'archive sont conservés sous `artifacts/.tmp-archives/machines/Acorn/BBC Master 512/`.

Ce rapprochement identifie l'image de référence et écarte une modification locale du fichier comparé ; il ne démontre pas, à lui seul, sa compatibilité avec toutes les ROM ni la fidélité de l'émulation.

## Résultat DOS Plus 1.2

Le chargement atteint 103 936 octets lus au champ 1 000, puis une instruction invalide à `0000:008C` et des erreurs répétées. Au champ 1 500, 115 206 octets lus. Capture examinée : `artifacts/TestsMachines/Acorn/BBC Master 512/20261108-dos-baseline-debug.bmp`.

Le diagnostic d'instructions localise un saut vers zéro à `5A00:5FFE`. Le chargeur écrit les bons octets autour de 5FFFF/60000 ; une routine de détection mémoire à `6012:4645` et `6012:4648` écrit ensuite les mots 5000 et AFFF dans 5FFFC–5FFFF, ce qui altère les instructions chargées. Cela établit le mécanisme observé, pas encore la cause matérielle ou logicielle. Les diagnostics temporaires ont été retirés des sources et les assemblages reconstruits.

## Comparaison DOS Plus 2.1

À la demande de l'utilisateur, comparaison avec `Gem Disc 1 v21.adl`, issu de [512gemdiscs.zip chez 8BS](https://8bs.com/othrdnld/utilities/512gemdiscs.zip). Taille 655 360 octets, SHA-1 `17a6195bdcdc1b6fb7b2d7b106b2225a857caefc`. Les fichiers `.640` et `.adl` de cette archive ont le même contenu. Cette image est distincte de la 1.2 initiale ; elle diffère également du SHA-1 de la 2.1 répertoriée par MAME (`89ad16c97842f94809cf05eb9b079867944ca8d8`). Ne pas les présenter comme des copies identiques.

L'essai conserve ROM, carte et scénario ; seul le chemin de disquette est changé. À 500/1 000/1 500 champs : 22 016 / 105 984 / 143 191 octets lus. Aucune trace d'opcode invalide pendant ce passage. La capture native examinée affiche `Master 512 80186 Xios Ver 1.03` et `DOS Plus Version 2.1`, avec les copyrights Acorn/Digital Research ; aucune invite A> n'apparaît encore. Capture : `artifacts/TestsMachines/Acorn/BBC Master 512/20261108-dosplus21-debug.bmp`. La lecture de la RAM télétexte 7C00 n'est pas pertinente ici : le système est passé en mode bitmap, d'où un relevé textuel vide malgré l'image affichée.

La 2.1 progresse donc davantage que la 1.2 dans la même fenêtre ; son démarrage complet n'est pas encore prouvé. Un passage prolongé à 3 000 champs relève aussi le PC parasite pour préciser l'attente après le titre.

Résultat prolongé avant correction CA : 145 158 octets lus à 2 500 puis 3 000 champs ; PC parasite final 06BB:48BB, SP=2FCD, FLAGS=0002, 615 014 413 cycles parasite cumulés. Le premier opcode invalide relevé est `07A6:DD2B` (`0F FC 0E 03 01 80 18 00 00 4E 49 4B`), suivi d'erreurs `06BB:01EE`. La capture `20261108-dosplus21-long-debug.bmp` affiche `Single Step Int Error`. La 2.1 ne démarre donc pas complètement non plus.

## Correction CA et comparaison avec les résultats historiques

La première implémentation `0ccab7fe1` conservait l'immédiat de RETF avant de dépiler IP/CS. Le commit `59702c180` a écrasé ce même temporaire par l'IP de retour. La correction actuelle rétablit des valeurs distinctes et conserve le chemin de prélecture/timings existant. Les 127 tests autonomes 80186 passent en Debug, dont trois nouveaux cas de retour et d'ajustement de pile.

La comparaison de `Master512Board.ReadMemory/WriteMemory` au premier bloc retrouve les mêmes plages : RAM 00000–7FFFF, miroir 80000–BFFFF vers 40000–7FFFF, ROM C0000–FFFFF. D'autres éléments de cette ancienne carte étaient encore des modèles simplifiés de DMA/interruptions ; ils ne constituent pas un état de référence complet à restaurer aveuglément. L'ancienne mention A> est conservée comme historique dans `implementation.md`, avec un avertissement daté.

Après la seule correction CA : DOS Plus 1.2 reste identique, y compris le premier opcode invalide à 0000:008C et 115 206 octets lus. La correction est nécessaire pour RETF, mais n'explique pas ce défaut de démarrage.

**DOS Plus 2.1 atteint A> après correction CA.** Relevés à 500/1 000/1 500 champs identiques au passage précédent : 22 016 / 105 984 / 143 191 octets lus. À 2 000/2 500/3 000 champs : 172 294 octets lus ; aucune instruction invalide signalée. Capture native `20261108-retf-dosplus21-debug.bmp` inspectée : titres XIOS 1.03, DOS Plus 2.1 et invite A> visibles. Copie de la preuve sous TestsChips/Intel80186/BBC Master 512.

Le résultat identifie une régression réelle du CPU qui empêchait le démarrage 2.1. Il ne valide pas encore les commandes DOS, tous les médias/entrées ni le fonctionnement de la 1.2. La carte et son décodage n'ont pas été modifiés pour obtenir A>.

## Chemin de la routine mémoire de la 1.2

La trace des transferts après correction CA confirme l'entrée normale du noyau `5A00:0063` vers `6012:0000`, puis les branches de la routine `6012:461A–4678`. La boucle écrit ses motifs dans FFFC/FFFE des segments F000, D000, B000, 9000, 7000, 5000, 3000 et 1000. La recherche retrouve le motif du segment 7000 après rejet des fenêtres ROM et des miroirs ; elle retrouve donc bien la limite 512 Kio, pas une extension mémoire inventée.

Le code disque de la routine se termine par une restauration des deux mots sauvegardés en `1000:FFFC/FFFE`, pas de tous les emplacements testés. Le noyau continue ensuite ses appels normaux entre 5A00 et 6012 avant d'atteindre les octets altérés dans le segment 5A00. Cela écarte un branchement accidentel vers la routine de détection, sans établir encore si cette édition nécessite une autre disposition du système ou si un traitement préalable du chargement reste incorrect.

Autre image ancienne disponible dans l'archive 8BS : `Gem Disc 1 Early version.640`, SHA-1 `484e181ffef52504c3334d4e914610023d99651e`. Elle diffère de l'image G7JJF/MAME 1.2 de 21 627 octets ; ne pas la considérer comme une simple copie identique ni lui attribuer une sous-version avant identification au démarrage.

## DOS Plus 1.2 — autre image démarrée et différence identifiée

L'image ancienne de 8BS affiche `Acorn Master 512 DOS Plus 1.2` et `80186 Xios Ver 1.00`, puis **A>**, sans instruction invalide pendant 1 500 champs. Lecture cumulée : 22 016 octets au champ 500, 103 936 au champ 1 000, 136 966 au champ 1 500. Capture native inspectée `artifacts/TestsMachines/Acorn/BBC Master 512/20261108-dosplus-early-debug.bmp`, copiée sous TestsChips/Intel80186/BBC Master 512. Le code de la carte et le processeur sont les mêmes que pour l'image initiale, avec la seule correction CA déjà intégrée.

Comparaison en lecture seule des fichiers du système via leurs chaînes FAT : `6502.SYS`, `LOGO.SYS` et `COMMAND.COM` sont identiques dans les deux images. Les deux `CPM3.SYS` ont 80 000 octets et ne diffèrent que de **deux octets**, aux offsets de fichier **A7B0–A7B1** :

| Image | Octets de l'immédiat | Instruction | Valeur comparée |
| --- | --- | --- | --- |
| G7JJF / catalogue MAME 1.2 | 80 03 | `CMP WORD PTR [2C97],0380` | 896 Kio |
| Image ancienne 8BS qui démarre | 00 02 | `CMP WORD PTR [2C97],0200` | 512 Kio |

Le code précédent lit la taille mémoire depuis la zone BIOS **0040:0013**, la copie en [2C97], puis effectue cette comparaison. Une égalité saute le test mémoire destructif ; le code teste aussi 0400 (1 024 Kio). Dans le passage initial avec 512 Kio, la comparaison 0200–0380 produit les indicateurs 0087 observés à `6012:461A`, et la routine de sondage est exécutée. Avec l'immédiat 0200 de l'autre image, elle est évitée. Cela explique pourquoi le même matériel atteint A> avec une image et altère son code avec l'autre. Il n'est pas nécessaire de modifier les puces ou le décodage mémoire pour masquer cette différence du programme.

Ce constat établit les octets et leur effet ; il n'identifie pas l'auteur, la date ni la destination matérielle historique de cette modification. Ne pas présenter une origine Solidisk ou un défaut de copie comme confirmé sans source.

Archives persistantes : `dos12-g7jjf-files/`, `dos12-8bs-files/` et `dos12-file-comparison.json` sous `artifacts/.tmp-archives/machines/Acorn/BBC Master 512/`. SHA-256 de CPM3.SYS initial : `4240dfa09059b9fc0f61e683fd7004b4a89d56c5310529efc9fff1a532d10564` ; image 8BS : `3ec27c29886953ae36b2a22d1a77b2c9ac8a7b2b8da0ac8020724dd556095bc1`. Les images sources n'ont pas été modifiées.

## Premier passage de commandes DOS 2.1

Copie temporaire accessible en écriture, même matériel et ROM, 2 200 champs après RESET avant saisie. L'invite A> puis le catalogue DIR sont visibles ; le catalogue annonce 38 fichiers, 112 640 octets libres. Après `ECHO RML512PASS>CHECK.TXT`, les captures de création puis de TYPE restent sur la ligne de création, sans preuve du contenu du fichier. Le média signale des changements, 182 540 octets lus, la copie change effectivement après FlushAsync et la référence reste identique. Ces éléments ne suffisent pas à valider la lecture après écriture du témoin.

Le relevé pris après cette commande inachevée donne 52,9488975 ms/champ, 18,886 champs/s, 3 194 881 cycles hôte et 15 974 406 cycles parasite pour 80 champs ; 0,754237 MHz hôte, 3,771185 MHz parasite, 37,7118 % nominal. Cette fenêtre n'est **pas validée comme repos** malgré le nom initial du scénario : une opération ou attente peut être encore active. Ne pas la comparer à un repos Turbo ni en déduire le seul coût CPU. Les captures ready/directory/write/read sont conservées sous TestsMachines, préfixe `20261108-dos21-`.

Le propriétaire a détaché et fermé le média puis supprimé sa copie temporaire dans finally. La reprise sépare la mesure à A> des commandes et relève PC/activité à chaque étape ; elle doit établir la fin de l'opération avant de considérer le fichier témoin comme validé.

## Sans média

Aucune image attachée, zéro octet lu : le titre `Acorn TUBE 80186 512K` et `Acorn ADFS` s'affichent. Après RESET à chaud, le banc reste également dans l'amorçage ADFS ; aucune invite de moniteur n'a encore été obtenue. Ne pas qualifier ce seul titre de validation fonctionnelle du Master 512 sans disque. Captures natives `20261108-without-disk-debug.bmp` et `20261108-without-disk-monitor-debug.bmp` sous TestsMachines/Acorn/BBC Master 512.

Un premier essai du banc conservait SHIFT pendant RESET à chaud ; la touche a ensuite été relâchée explicitement avant RESET. Le même écran d'amorçage persiste. Aucun défaut matériel attribué à ce seul essai.

## Références utiles

Le [guide technique de la machine, chapitre Firmware](https://rk.nvg.ntnu.no/bbc/doc/Master%20512%20Technical%20Guide/m512tech03.htm) décrit le bootstrap en ROM, DOSBOOT chargé en 0400:0000 puis déplacé en 3800:0000, le chargement de DOS Plus depuis la disquette et l'accès au moniteur après BREAK. Le [modèle MAME de la carte](https://github.com/mamedev/mame/blob/master/src/devices/bus/bbc/tube/tube_80186.cpp) reproduit également le miroir 80000–BFFFF vers 40000–7FFFF ; cet accord constitue une comparaison de code, pas une preuve de câblage constructeur.

## Commandes DOS 2.1 — second passage borné

ECHO READY512 affiche bien le texte puis revient à A>. DIR lit le catalogue. La création `ECHO RML512PASS > CHECK.TXT` revient à A> après l'attente prolongée : 1 536 octets écrits et 185 106 octets lus ; activité disque retombée à faux. FlushAsync modifie la copie et laisse la référence inchangée. La capture TYPE prise après 200 champs ne montre pas encore le contenu : la relecture du fichier reste donc non validée. Captures natives inspectées : `20261108-dos21-echo-console-v2-debug.bmp`, `20261108-dos21-write-v2-debug.bmp`, `20261108-dos21-read-v2-debug.bmp`.

Mesure à l'invite avant toute commande et sans activité disquette : **41,130375 ms/champ** sur 80 champs ; 3 194 880 cycles hôte, 15 974 384 cycles parasite, fréquence matérielle 50,080128 Hz, mode progressif. Cela représente environ 24,31 champs/s et 48,55 % de la cadence nominale. Ce constat ne localise pas le coût et ne valide pas les performances de la machine. Le relevé suivant TYPE reste associé à une activité disque et ne constitue pas une mesure au repos.

Le diagnostic temporaire est supprimé après consignation ; ses captures de preuve restent dans artifacts et les images sources sont intactes.

## Profil Debug du démarrage DOS 2.1

Échantillonnage dotnet-trace de 10 secondes du processus du banc de relecture, avant son message BOOT complete : phase de démarrage, et non repos à A>. Sur le seul thread exécutant la machine (10 019,2 ms échantillonnées), attribution exclusive : 80186 3 414,95 ms (34,1 %), vidéo 2 339,97 ms (23,4 %), chemin WD/disquette 1 282,60 ms (12,8 %), 65C12 758,47 ms (7,6 %), autres 2 223,22 ms. Les attentes des threads système sont exclues et les pseudo-feuilles sont attribuées à leur dernier appelant réel.

Premiers coûts propres 80186 : AdvanceTimers 554,3 ms, ExecuteStep 540,3 ms, SelectBusTargets 186,0 ms, FetchPrefetched 157,7 ms, IncrementTimer 156,1 ms, Execute 155,8 ms. Ce profil localise les chemins pendant l'amorçage ; il ne prouve pas encore le potentiel d'une modification, ni la répartition à l'invite. Aucune fréquence ni notification modifiée pour cette collecte. Le profil temporaire et son export sont supprimés après consignation.

## Lecture après écriture confirmée — DOS Plus 2.1

Passage borné avec attente de 1 200 champs après ECHO RML512PASS > CHECK.TXT, puis 1 000 champs après TYPE CHECK.TXT. La capture native finale montre le contenu **rml512pass** puis le retour à **A>**. Fin d'écriture : activité fausse, 1 536 octets écrits. Fin de lecture : activité fausse, 190 226 octets lus depuis le démarrage. Le marqueur ASCII est aussi présent dans les secteurs de la copie après FlushAsync. Capture inspectée : `artifacts/TestsMachines/Acorn/BBC Master 512/20261108-dos21-readback-final-debug.bmp`, également conservée sous TestsChips/Intel80186/BBC Master 512.

Ce passage valide la création et la relecture d'un fichier DOS par les deux processeurs, Tube et contrôleur disquette assemblés. Il ne valide pas le HDD, les entrées utilisateur ni toutes les commandes DOS. Aucun autre changement de production depuis la correction CA. La copie temporaire est détachée, fermée et supprimée dans finally ; les sources restent intactes. Le temps de ce passage incluant profilage et attentes fonctionnelles n'est pas une comparaison de performance.

Comparaison des autorisations de timers internes à A> : 39,688415 ms/champ avant, 39,880200 après, cycles et empreintes identiques. Tentative retirée, aucun gain validé. Détails dans docs/chips/CPU/Intel80186.md ; les deux tests autonomes de transitions sont conservés. La cadence Debug demeure insuffisante ; cette comparaison ne clôture pas la machine.

## Appels MOS accessibles au logiciel 80186

La note Acorn *Master 512: Technical Information and Monitor Documentation* réserve les interruptions 40h à 4Ch aux services MOS. Les appels utiles aux prochains essais d'entrées sont OSWORD par INT 4Ah (AL = numéro, DS:BX = bloc) et OSBYTE par INT 4Bh (AL = numéro, BL/BH = paramètres et résultats selon la section détaillée OSBYTE). La phrase d'introduction inverse BH/BL ; réutiliser les descriptions détaillées et vérifier le programme effectif plutôt que recopier cette ambiguïté. OSCLI utilise INT 4Ch et une chaîne DS:BX terminée par CR ; OSWRCH utilise INT 49h avec le caractère dans AL.

Source constructeur consultée le 8 novembre : https://acorn.huininga.nl/pub/unsorted/manuals/Master%20512%20-%20Technical%20Information%20and%20Monitor%20Documentation.htm . Ces services sont fournis par la ROM/logiciel de la machine, pas par une interception ajoutée dans le processeur émulé.

## Utilitaire CHKDSK et cadence Release — 8 novembre

Même MOS 3.20, client 80186 1.01 et image DOS Plus 2.1 de référence montée en lecture seule. CHKDSK affiche le volume Master 512, 647 168 octets au total, 534 528 octets dans les fichiers utilisateur et 112 640 disponibles, puis revient à A>. Activité disquette fausse, 195 858 octets lus depuis le démarrage, HasChanges faux. Capture native inspectée : `artifacts/TestsMachines/Acorn/BBC Master 512/20261108-dos21-chkdsk-release.bmp`, copiée sous TestsChips/Intel80186/BBC Master 512.

Trois fenêtres successives de 80 champs au repos avant CHKDSK, rendu et drainage audio inclus : **6,82403375 / 6,7557875 / 6,78495875 ms/champ**, soit 146,54–148,02 champs/s sans limite, hôte 5,85–5,91 MHz et parasite 29,26–29,56 MHz, 292,6–295,6 % de la vitesse nominale. Les clocks matérielles visées restent 2/10 MHz et la vidéo 50,08 Hz ; ces fréquences supérieures expriment seulement la capacité hôte sans limite.

Les trois empreintes vidéo et audio finales concordent avec les fenêtres Debug précédentes ; les cycles de fenêtre diffèrent de quelques unités (hôte 3 194 880 / 3 194 879 / 3 194 882, parasite 15 974 419 / 15 974 391 / 15 974 415). Ne pas revendiquer une identité cycle par cycle entre ces deux initialisations distinctes. Le coût Debug (~40 ms/champ) demeure séparé. Décision demandée sur le critère de cadence de clôture ; aucune acceptation du Master 512 déduite de celle du Turbo.

## Régression autonome des raccordements Master — 8 novembre

Sélection AcornEmuMasterTests en Debug : **31 réussites, 0 échec, 0 ignoré**, durée du groupe 717 ms. La compilation du projet de tests complet réussit après suppression de l'ancien diagnostic dépendant du compteur privé. Ces cas couvrent notamment les miroirs RAM/ROM du 512, la sélection PCS1 et sa voie octet basse, la consommation unique d'un octet Tube en DMA, les entrées des timers et le retour du bus de rafraîchissement avant NMI. Les autres cas du groupe concernent les configurations et variantes Master ; ne pas présenter les 31 cas comme 31 essais DOS ou comme un essai de manette physique.

Avertissements existants observés pendant cette compilation : CA1416 dans plusieurs modules Windows, CS8604 dans Sony SettingsPresentationTests, CS0649 dans des doubles de tests et xUnit1031 dans AcornSessionInputRouteTests. Aucun de ces avertissements n'est un échec de cette sélection. Journal TRX temporaire supprimé après consignation.

## Programme DOS de lecture joystick — diagnostic encore ouvert

Banc temporaire Master512Joystick.cs : JOYTEST.COM est ajouté à une copie de la disquette 2.1 par une entrée FAT12 libre, chargé depuis DOS sans injection dans les registres ou le code des processeurs. Le programme appelle OSBYTE 16 par INT 4Bh, attend par une boucle CPU, puis doit lire OSBYTE 128 et afficher quatre voies et les tirs. Source et conventions INT 4Bh consignées ci-dessus. Les entrées utilisent les snapshots existants, sans nouveau pilote.

Premier passage : après 600 champs suivant JOYTEST, aucun résultat écrit, capture `20261108-joystick-0-debug.bmp` montrant seulement la commande. Second passage prolongé à 1 600 champs : même absence de résultats, PC 80186 **F000:197A**, AX=107F, BX=0004, CX=0000, DS=1679, PC hôte **2A11**, 180 748 octets lus, activité disque fausse. Les deux signatures du programme présentes en RAM ont encore leurs cinq mots FFFF initiaux. La ROM autour de cette adresse effectue une attente de données du canal 2 Tube (lecture du statut 84h, test du bit 80h, puis lecture de 86h).

Le blocage intervient donc avant la première valeur joystick ; il ne prouve pas une panne ADC ou une erreur du contrôleur. Vérifier l'appel effectif, le protocole Tube et le contexte DOS côté hôte avant toute modification de production. La première préparation de copie ne trouvait aucun cluster libre dans sa plage trop courte de 250 clusters ; le banc a été corrigé pour parcourir toute la zone de données, sans modifier le format de production. Les deux exécutions possédant une image se terminent par le nettoyage finally ; copie temporaire absente après fermeture.

Le second passage écrase la capture de même scénario du premier ; aucune des deux ne constitue une preuve de joystick validé. Pour la suite, utiliser des noms d'étapes différents et conserver les preuves qui apportent une information nouvelle. Le diagnostic reste en cours et ne permet pas de clôturer le Master 512.

## Octets réellement transférés avant l'attente — 8 novembre

Trace temporaire limitée aux transferts DATA du canal 2, sans lecture ajoutée. Après la fin des paquets DOS (octet FF), la ROM 80186 envoie **04**, **04**, **10** depuis F000:1970 ; les trois sont effectivement lus par le 65C12 (PC respectifs 283F, 2A09, 2A16). Il ne manque donc pas de livraison de ces octets par le Tube. Le 80186 attend ensuite à F000:1978, AX=107F, BX=0004, DS=1679 ; l'hôte attend en 2A11, disque inactif, 180 748 octets lus.

Le code hôte autour de 2A00 lit un octet vers la page haute d'un pointeur, attend un autre octet pour l'index puis un octet de contrôle avant de transférer des données depuis le canal 1. La requête de la ROM tombe donc dans cette routine DOS de transfert au lieu de produire la réponse MOS attendue. Ce constat localise un désaccord de contexte/protocole ; il ne détermine pas encore pourquoi le logiciel utilise ce trajet et ne justifie pas un changement du Tube ou de l'ADC.

Le premier relevé borné à 120 champs après JOYTEST était trop tôt : l'image était encore active et le programme pas encore chargé. La reprise s'arrête sur huit observations consécutives de l'attente ROM, après fin d'activité disque. Capture distincte `20261108-joystick-0-tube-request-debug.bmp` ; les anciennes captures d'attente sont conservées comme étapes, sans verdict de réussite.

Vérification documentaire complémentaire : chapitre 6 du Master 512 Technical Guide, source https://acorn.huininga.nl/pub/mirrors/8bs.com/othrdnld/manuals/512/512tech/m512tech06.htm , archivé dans `artifacts/.tmp-archives/machines/Acorn/BBC Master 512/m512tech06.htm`. Il décrit explicitement les interruptions 40h–4Ch sous DOS Plus et les paramètres BL/X, BH/Y dans la section OSBYTE ; ne pas classer ces appels comme incompatibles avec DOS sans résoudre l'écart observé. Le texte rappelle également le rôle de l'événement MOS 4 pour la communication DOS. Aucun événement n'a été désactivé par ce banc.

## Deux joysticks depuis le 80186 — DOS Plus 1.2, Debug

Le programme JOYTEST.COM collecte maintenant ses cinq résultats par INT 4Bh avant les sorties DOS. Les valeurs sont observées dans la RAM hôte du coprocesseur pendant leur durée de validité, avant que la fin du COM permette à DOS de réutiliser cette mémoire. Le précédent verdict automatique faux provenait de la recherche trop tardive de cette signature : la capture montrait déjà les bonnes valeurs. Aucun composant modifié pour ce passage.

Trois scénarios réussissent :

| Entrées normalisées | Voies 1–4 | Retour brut OSBYTE 128,0 | Tirs (bits 0–1) |
| --- | --- | --- | --- |
| Position 1, tir 1 | 65520 / 0 / 0 / 65520 | 513 (0201h) | 1 |
| Position opposée, tir 2 | 0 / 65520 / 65520 / 0 | 514 (0202h) | 2 |
| Repos | 32752 / 32752 / 32752 / 32752 | 512 (0200h) | 0 |

La capture `20261108-joystick-2-dos12-verified-debug.bmp` montre les trois lignes réellement affichées et le retour à A>. La première capture ne montre que le titre : ne pas la citer seule comme preuve du premier verdict ; la dernière contient aussi cette première ligne. Copies de la preuve complète conservées dans TestsChips/Upd7002/BBC Master 512 et TestsChips/Intel80186/BBC Master 512. Ce passage vérifie les snapshots normalisés, le contrôleur analogique et la lecture par le programme 80186 via le Tube ; il ne remplace pas un essai de manette physique dans RML.

Sous DOS Plus 1.2, le programme initial entrelaçant chaque appel MOS avec l'affichage DOS s'arrêtait après la première voie correcte. Sous 2.1, la première requête restait en attente. Le programme collectant les valeurs sépare ces faits : son succès sous 1.2 ne clôture pas le trajet restant sous 2.1.

La correction DHLT du 80186 ne change pas le constat sous DOS 2.1 : après démarrage et chargement du COM, l'appel MOS reste à F000:1978 (AX=107F, BX=0004), hôte 2A0E, 180 748 octets lus, disque inactif. Capture distincte `20261108-joystick-0-dos21-dhlt-debug.bmp`. La réussite des trois états sous 1.2 demeure une preuve de fonctionnement des voies/tirs jusqu'au logiciel 80186 ; elle ne clôture pas le contexte d'appel MOS sous 2.1. Le banc temporaire est supprimé après consignation, les copies de média ont été détachées/fermées/supprimées dans finally.

Le contrôle par STAR FX16,4 puis ECHO MOSRETURN sous DOS 2.1 revient à A>. Le vecteur 4Bh reste F000:1E10. La définition des appels XIOS et la piste du sémaphore sont désormais réunies dans [Appels MOS depuis DOS](dos-mos-calls.md), avec les sources archivées et les captures ; y consigner la suite du diagnostic plutôt que répéter cette recherche.

Le contexte MOS 2.1 est maintenant résolu : avec acquisition/libération documentée du sémaphore XIOS dans le seul programme COM de contrôle, les deux positions joystick, les deux tirs et le repos passent, avec retour à A>. [Cause, interface XIOS et preuves](dos-mos-calls.md). Aucune modification du Tube, de l'ADC ou du câblage pour corriger le banc. Le Master 512 entier n'est pas clôturé par cette seule validation des entrées.

## Cadence Release actuelle — 8 novembre 2026

Banc natif reconstruit depuis les sources courantes après RETF/DHLT, sans modification de production : MOS 3.20, client 80186 1.01, WD1770, deux médias en lecture seule (DOS Plus 2.1 ADFS 640 et GEM2.800 DOS Plus 800), souris Acorn. Aucune interface Winchester montée dans ce banc. Rendu RunFrame et DrainAudio inclus, sans autre émulation, compilation ou test pendant les mesures. Les fenêtres de 160 champs ne sont pas des FPS présentés par RML.

| Scénario | ms/champ | Champs/s sans limite | Cycles CPU hôte | MHz hôte effectifs | Vitesse réelle |
| --- | ---: | ---: | ---: | ---: | ---: |
| DOS repos 1 | 6,596672 | 151,591579 | 6 389 759 | 6,053960 | 302,698017 % |
| DOS repos 2 | 6,236651 | 160,342475 | 6 389 759 | 6,403436 | 320,171805 % |
| DOS repos 3 | 5,963293 | 167,692578 | 6 389 761 | 6,696972 | 334,848593 % |
| CHKDSK, 1 200 champs | 7,476566 | 133,751251 | 47 923 198 | 5,341490 | 267,074487 % |
| GEM repos 1 | 7,103306 | 140,779514 | 6 389 760 | 5,622171 | 281,108533 % |
| GEM repos 2 | 6,562553 | 152,379719 | 6 389 758 | 6,085435 | 304,271727 % |
| GEM repos 3 | 6,512878 | 153,541949 | 6 389 761 | 6,131852 | 306,592611 % |

DOS atteint A> après 172 294 octets lus ; CHKDSK termine et affiche 647 168 octets totaux / 534 528 utilisés / 112 640 disponibles, puis A>. La fenêtre CHKDSK comprend 23 564 octets de disquette lus et la fin au repos, pas uniquement des cycles CPU actifs. GEM atteint son bureau après 293 894 octets lus sur B et deux fenêtres stables. Les repos n'ajoutent aucune lecture de disque.

SHA256 vidéo DOS identique sur les trois fenêtres : `A963E48AED77D07DB5243D49DD42890DD34FDBE49958D1293F2E6CBF5A538ECC`. GEM identique sur ses trois fenêtres : `D67DDA2118D66032330F529E1231D6FC9E237AA3CCB6830E29362696F76A9F37`. CHKDSK : `EC70151B5194F5353998247D6E6EB9253FC51AE8D9738C570520C397A5B3CF5E`. Captures natives inspectées, préfixe `20261108-master512-cadence-`, suffixes dos-release, chkdsk-release et gem-release, sous TestsMachines/Acorn/BBC Master 512.

La marge Release dépasse la cadence matérielle d'environ 50,0801 Hz dans ces scénarios. Ce constat ne constitue ni une amélioration avant/après ni une validation de 19,97 ms en Debug ; les mesures HDD Debug de 38,46–39,24 ms/champ portent sur un autre montage. Le banc se termine avec code 0, détache et libère ses deux images en finally ; source supprimée puis Native reconstruit avec Program.cs sans l'exécuter. Aucun nouveau test unitaire CPU n'a été ajouté ou relancé pour ce seul contrôle de carte.

## Livraison Debug complète du 8 novembre 2026

`scripts/local-building.cmd --building=debug --modules=A` exécuté avec accès restauration NuGet, RML fermé avant lancement. Script terminé avec **code 0**, exécutable présent : `F:\GW GUI\build\Debug\Retro Media Lab\retromedialab.exe`. Les **24 modules** des manifestes source, Example compris, sont présents dans Modules/<id> ; chaque DLL livrée a le même SHA256 que sa compilation Debug win-x64. `lib/retromedialab.chips.dll` correspond également à la sortie Debug courante : `311B89EEDA297D61CE80CA9EB9042498D84FC6DC67F9FFB2CA755204A7E8B734`. Le build n'est pas un essai en direct et RML n'a pas été relancé.

Captures Release DOS/CHKDSK/GEM copiées aussi sous TestsChips/Intel80186 et Wd1770/BBC Master 512. Aucune capture ni diagnostic ajouté à Git. La décision utilisateur sur le critère de cadence propre au 512 et l'essai final dans RML restent attendus ; ne pas considérer l'acceptation conditionnelle du Turbo comme celle du 512.

## Vérification Debug et Release après le bloc média — 8 novembre

À la demande de l'utilisateur, les deux configurations sont vérifiées sur les sources courantes après le commit média `cc1fc4e44`. Montage inchangé : MOS 3.20, client 80186 1.01, 512 Kio, WD1770, `F:/Retro/Gem Disc 1 v21.adl` ouvert en lecture seule par le SDK, sans Winchester. Initialisation ADFS par CMOS[19] bas = 8, Shift pendant les 121 premiers champs, puis 3 000 champs de démarrage. Aucune fenêtre RML ouverte, aucune compilation ou autre banc pendant l'exécution chronométrée. La compilation de chaque configuration précède son lancement, l'autre processus est déjà terminé.

Le premier passage limité à 1 280 champs atteint seulement le titre XIOS (109 574 octets lus). Sa mesure 46,83503125 ms/champ porte sur le chargement, pas sur le repos DOS : elle ne démontre aucune régression par rapport aux repos antérieurs. Capture intermédiaire conservée avec le suffixe `current-media-xios-debug.bmp`. Le délai du banc est ensuite aligné sur les 3 000 champs déjà documentés, sans changement matériel.

Les deux configurations atteignent **DOS Plus Version 2.1 / A>**, après **172 294 octets lus**, stables dès le relevé à 2 000 champs. Fenêtre de repos finale de 80 champs, `RunFrame` et `DrainAudio` inclus :

| Configuration | Total hôte ms | ms/champ | Champs/s sans limite | Cycles hôte | MHz hôte effectifs | Vitesse nominale |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | 3 139,6376 | 39,245470 | 25,480648 | 3 194 882 | 1,017596 | 50,879789 % |
| Release | 449,1834 | 5,6147925 | 178,100972 | 3 194 879 | 7,112638 | 355,631909 % |

Cadence matérielle de référence : environ 50,0801 Hz, hôte nominal 2 MHz, coprocesseur configuré à 10 MHz. Les MHz ci-dessus sont les cycles hôte mesurés, pas une mesure séparée du coprocesseur. Les trois cycles d'écart entre fenêtres résultent des frontières de traitement ; aucune fréquence n'est changée. Aucun gain avant/après ni FPS de présentation RML revendiqué. La Release possède la marge ; le Debug ne tient toujours pas 19,97 ms/champ.

SHA256 vidéo identique dans les deux configurations : `5A8F49C9CF191F35971F3E3707DF0A832A29E1B107A30C00C9FC79B80451889C`. Capture Debug inspectée visuellement, invitant A> ; capture Release identique. Fichiers `20261108-master512-current-media-{debug,release}.bmp` sous `artifacts/TestsMachines/Acorn/BBC Master 512/`, copies sous `artifacts/TestsChips/Intel80186/BBC Master 512/`. Les bancs terminent avec le code 0, détachent le média et le ferment dans finally. Ce contrôle ne clôture pas l'essai final RML ni la décision de cadence propre au 512.

L'utilisateur accepte ensuite ces résultats pour poursuivre : « ok c'est bon, passe a la suite alors les Acorn System ». Le bloc 512 est accepté avec les limites de cadence Debug et les observations RML existantes ; aucun nouvel essai direct n'est inventé. ET/Compact ne sont pas inclus dans cette acceptation.

## Revue du 9 novembre 2026 — composants et reset

Revue statique du 80186 (instructions et prélecture, DMA, timers, bus), de l’ULA Tube et de `Master512Refresh.cs`, puis de `Master512Board.cs`. Les demandes Tube sont reçues par le CPU, Timer0/HLDA pilotent la logique de rafraîchissement et Timer1 la NMI ; les sélections mémoire et l’octet bas du Tube restent décodés dans la carte. La revue détaillée des composants communs du Master 128 est consignée dans `docs/tasks/machines/BbcMaster/implementation.md`.

Correction : la carte gelait le CPU pendant le reset Tube. La nouvelle entrée `Cpu80186.SetReset` reçoit désormais ce niveau et gère le maintien et la récupération dans la puce. Les constantes, contrat, nouveau `Cpu80186.Reset.cs` et branche de `Step` conservent le temps écoulé sans exécuter d’instruction pendant RESET, puis les 6,5 CLKOUT avant prélecture selon Intel figure 50 (voir `docs/chips/CPU/Intel80186.md`). `Master512Board` ne possède plus de booléen arrêtant l’horloge. Les notifications DMA/IRQ/timers et le décodage RAM/ROM n’ont pas été remplacés.

Le test autonome ajouté à `AcornEmuMasterTests.cs` maintient le Tube en reset pendant 100 cycles hôtes, vérifie l’absence d’écriture du programme de démarrage puis contrôle l’écriture attendue après libération. Les tests CPU couvrent séparément les délais et la réassertion. Les mesures Debug/Release et démarrages DOS ci-dessus restent ceux du 8 novembre ; aucun nouveau résultat de vitesse ou d’essai utilisateur n’est inventé.

Résultats finaux de ce bloc : **138 tests CPU 80186 Debug réussis, 143 ms ; 34 tests Master Debug réussis, 855 ms ; zéro échec/ignoré**. Les avertissements de compilation existants hors bloc restent présents ; aucune fenêtre, ressource native ou média externe ouvert pour ces tests.
