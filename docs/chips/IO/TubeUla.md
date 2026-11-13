# Acorn Tube ULA

Le contrôle AMX/Turbo du 7 novembre est désormais résolu : le banc remplaçait VIEW 14, qui porte la relocation de BASIC MOS 3.50. Avec VIEW conservé et AMX en extension 8 (réseau absent), le BASIC parasite reçoit les coordonnées 640,512 puis 688,544 par OSWORD 64, ainsi que les quatre valeurs ADVAL attendues. Captures inspectées `20261107-160207-amx-tube-coordinates.png` et `20261107-160218-analog-tube-values.png` sous `artifacts/TestsChips/TubeUla/BBC Master Turbo/`. Aucun code du Tube modifié. [Cause et montage exact](<../../project/emulations/Acorn/BBC Master/Master Turbo/software-compatibility.md>).

## Entrées analogiques du Master Turbo — 7 novembre 2026

MOS 3.50 et client 65C102 externes, sans AMX : BASIC parasite alloue 40 000 octets, retrouve 123 dans son dernier octet, puis affiche ADVAL(1…4) = 0/65520/65520/0 pour les deux contrôleurs hôtes aux extrémités opposées. Capture native inspectée `artifacts/TestsChips/TubeUla/BBC Master Turbo/20261107-155540-baseline-tube-analog.png`, également rangée dans TestsMachines. Aucun code du Tube, fréquence ou registre modifié. Cette preuve concerne les appels MOS servis par l'hôte ; elle ne valide pas toutes les extensions. Le démarrage avec les ROM AMX essayées reste anormal, y compris sans nouvel adaptateur de pointage : [conditions et défaut ouvert](<../../project/emulations/Acorn/BBC Master/Master Turbo/software-compatibility.md>).

## Références et fonctionnement matériel

Référence principale : [Acorn, Application Note 004](https://chrisacorns.computinghistory.org.uk/docs/Acorn/AN/004.pdf), spécification électrique et registres, pages 11–17. Comparaison de réalisation : [JSBeeb, tube.js](https://github.com/mattgodbolt/jsbeeb/blob/main/src/tube.js). La spécification Acorn prime en cas de divergence.

Le Tube est asynchrone. L'interface hôte reçoit PHI2 et R/W ; l'autre côté reçoit les strobes de lecture/écriture. Il ne possède pas d'oscillateur périodique autonome à inventer. Ses entrées et les transactions des deux domaines restent distinctes. R1 contient 24 octets du parasite vers l'hôte, les autres latches un octet, R3 deux octets par sens. Les demandes IRQ, NMI, DMA et RESET proviennent des états internes. Les indicateurs R3 restent actifs jusqu'à achèvement du transfert simple ou double. T maintient les registres en reset jusqu'à effacement ; P commande séparément PRST. HRST réinitialise les contrôles et transmet RESET au parasite. DACK permet les accès DMA R3 sans sélection normale d'adresse.

## État initial constaté après clôture des CPU CMOS

`TubeUla.cs` contient les files et quatre canaux, les notifications de changement IRQ/NMI et un rappel de reset parasite. Il n'exécute aucun tick au repos. En revanche T est traité comme une impulsion, HRST n'a pas d'entrée maintenue, DRQ n'est pas exposé et les fronts de bus sont remplacés par des transactions directes. Le statut parasite R3 initial et certains changements de mode méritent une vérification explicite. Les cartes Turbo et 512 consultent encore `ResetHeld` à chaque cycle hôte ; la 512 consulte aussi IRQ/NMI à chaque instruction parasite pour alimenter les entrées CPU.

## Vérifications du bloc

Comparer chaque direction et chaque canal : remplissage, vidage, ordre et limites ; préserver les deux octets R3 et leurs indicateurs intermédiaires. Vérifier chaque masque de contrôle, reset matériel/logiciel maintenu et relâché, causes d'interruption combinées et absence de notification sans changement. Vérifier les fronts hôte et parasite, désélection simultanée et DMA. Confirmer ensuite les ROM et échanges de logiciels sur les deux cartes, ainsi que les cadences et performances globales. Aucune nouvelle conformité n'est revendiquée à ce stade.

## Résultats du bloc du 6 novembre 2026

Les 20 tests Tube passent en Debug et Release : capacités et ordre des canaux, derniers octets lus, masques de contrôle, T maintenu, HRST, indicateurs R3 simples/doubles, causes IRQ/NMI combinées, notifications uniquement au changement, fronts hôte/parasite, désélection simultanée et sens DMA. Les 25 tests Master et régressions de performances passent également dans les deux configurations. Ils couvrent notamment le transfert DMA 80186 d'un octet sans duplication. Aucun de ces résultats ne constitue une mesure analogique des temps de propagation du silicium.

Le banc de carte complète utilise le MOS Master 3.50, le générateur SAA5050, le WD1770 et l'interface Winchester sans image montée. Le Turbo utilise son client 65C102 ; le 512 son client 80186 1.01. Après 120 images d'amorçage, chaque scénario comporte 10 images de chauffe puis 80 images mesurées, avec rendu vidéo et drainage du PCM, sans interface graphique ni sortie audio Windows. Les commandes BASIC produisent `TUBE SOFTWARE PASS` puis `46` sur le Master seul et le Turbo. Le 512 attend son système sur disque : ce passage ne vérifie que son amorçage.

Comparaison avec les cinq fichiers antérieurs extraits du HEAD avant le bloc Tube ; compilation Debug normale, sans activation d'optimisation Release :

| Carte complète / scénario | Debug avant, ms/image | Debug après, ms/image | Release avant | Release après |
| --- | ---: | ---: | ---: | ---: |
| Master seul, repos | 18,013 | 17,202 | 3,227 | 3,128 |
| Master seul, SOUND | 18,104 | 17,229 | 3,177 | 3,024 |
| Master seul, redessin | 18,437 | 17,969 | 3,201 | 3,232 |
| Master Turbo, repos | 29,110 | 29,153 | 5,640 | 5,562 |
| Master Turbo, SOUND | 30,782 | 28,817 | 5,720 | 5,639 |
| Master Turbo, redessin | 30,429 | 29,913 | 5,972 | 6,063 |
| Master 512, amorçage au repos | 35,774 | 34,926 | 5,446 | 5,505 |

Pour chaque paire, le nombre de cycles hôte, le PC final et les empreintes SHA-256 de la dernière image et du dernier bloc PCM sont identiques. Il s'agit d'une paire de passages séquentiels, sans estimation de dispersion : aucune amélioration globale statistiquement établie n'est revendiquée. Le Turbo et le 512 dépassent encore le budget d'une image matérielle en Debug. La priorité de poursuite est Debug ; les mesures Release déjà disponibles ne seront pas répétées à chaque modification.

Les nouvelles entrées de bus représentent des transactions numériques respectant les conditions d'utilisation du constructeur ; elles ne détectent pas les violations analogiques de setup/hold. Les comportements non documentés lors d'un changement de mode au milieu d'un transfert restent distincts des séquences normales vérifiées. La preuve sur logiciel DOS Plus et les captures dans RML restent à terminer avant clôture du bloc.

Le premier essai DOS Plus en Debug utilise le MOS 3.20, le client 80186 1.01 et l'image ADL de 655 360 octets ouverte en lecture seule. Après 80 images puis un SHIFT-RESET suivi de 650 images, le chargeur affiche `Boot strap loader [19-Mar-86]` et `Loading DOS PLUS`. Il a lu 47 872 octets ; le PC hôte est BCEB. Le même essai avec l'ancien Tube donne ces mêmes valeurs et exactement la même empreinte de l'image rendue. Cela vérifie une interaction réelle disque/hôte/parasite, mais ne prouve pas encore une invite DOS fonctionnelle. Le média et la configuration utilisateur n'ont pas été modifiés.

L'essai prolongé avec le nouveau Tube atteint 103 936 octets lus à la 1 000e image après RESET. Il signale ensuite une instruction invalide à 0000:008C puis à 1260:DA01 ; à la 3 000e image, 119 814 octets ont été lus et l'écran indique `Invalid Opcode Error` puis `Unable to load COMMAND.COM`. Ce résultat interdit de valider le Master 512 comme fonctionnel sur DOS Plus. La comparaison prolongée avec l'ancien Tube est nécessaire pour attribuer l'écart au bloc modifié ou à une autre partie du système ; elle n'est pas remplacée par les seuls tests unitaires réussis.

Cette comparaison prolongée est ensuite effectuée en Debug sur 1 500 images avec l'ancien Tube. Les relevés à 500 et 1 000 images sont identiques (22 016 puis 103 936 octets lus ; PC hôte 0044 puis 0039). La première instruction invalide est également 0000:008C, avec les mêmes octets, AX=3A12, SP=82AF et les mêmes registres DMA/timer ; la séquence suivante d'erreurs 1260:DA01 est identique. Le défaut de chargement précède donc le bloc Tube actuel. Sa cause précise reste à établir dans le Master 512 ; cette comparaison ne désigne pas à elle seule une puce fautive. Après le passage ancien, les assemblies du banc sont reconstruites en Debug avec le code courant, sans substitution des sources du HEAD.

## Master Turbo — preuve native du 7 novembre 2026

MOS 3.50 et client 65C102 existants : démarrage natif, programme BASIC allouant 40 000 octets avec vérification aux deux extrémités, verdict TURBO RAM PASS et calcul 46. Un motif de 256 octets est ensuite enregistré depuis la RAM parasite via OSCLI, effacé puis rechargé et comparé intégralement : TUBE FILE PASS, 1 280 octets physiques lus et 768 écrits. Captures inspectées sous `artifacts/TestsChips/TubeUla/BBC Master Turbo/20261107-105914-turbo-private-ram.bmp` et `20261107-110019-turbo-tube-file.bmp`, également sous TestsMachines. Aucun octet injecté dans la RAM parasite par le banc, aucun code Tube modifié.

160 images Debug avec rendu/drainage : 31,541715 ms/image, 31,7040 images/s ; 6 389 758 cycles hôte et 12 779 516 cycles parasite, rapport exactement 2, fréquence parasite configurée 4 MHz. Le budget n'est pas tenu. [Configuration, programme, compteurs et limites](<../../project/emulations/Acorn/BBC Master/Master Turbo/board-validation.md>). Cette preuve clôt le manque de logiciel natif Turbo, pas le défaut DOS Plus du Master 512 ni sa validation machine.

Compatibilité SimCity observée le 7 novembre : le chargeur du SSD utilisateur charge son écran à 5800 sans adresser l'hôte FFFF5800 ; cette commande vise donc la RAM parasite avec le Tube actif. Le guide constructeur explique cette séparation. Ne pas fusionner les espaces mémoire ni rediriger les chargements pour faire fonctionner un logiciel non adapté. [Version, inspection et limites de conclusion](<../../project/emulations/Acorn/BBC Master/Master Turbo/software-compatibility.md>).

## Reprise intégrée Turbo — 8 novembre 2026

BASIC parasite exécuté après reconstruction Debug : verdict TURBO CURRENT PASS puis calcul 12+34 = 46. Le parcours MOS/Tube vers l'affichage hôte fonctionne dans ce scénario ; les deux parités natives sont observées. Captures nouvelles sous artifacts/TestsChips/TubeUla/BBC Master Turbo/20261108-current-basic.bmp et -basic-next-field.bmp. Les mesures de carte complète restent à 71–74 % de vitesse nominale ; aucune modification du Tube et aucun nouveau gain revendiqué. Ce contrôle ne reteste ni tous les canaux du Tube ni le Master 512. Détails : [fiche Master Turbo](<../../project/emulations/Acorn/BBC Master/Master Turbo/board-validation.md>).
