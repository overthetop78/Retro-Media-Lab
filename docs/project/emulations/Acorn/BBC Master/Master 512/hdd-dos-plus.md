# Master 512 — Winchester depuis DOS Plus

## Parcours à vérifier

Source primaire : Master 512 Technical Guide, annexe G, section Formatting, https://acorn.huininga.nl/pub/mirrors/ftp.nvg.org/pub/bbc/doc/Master%20512%20Technical%20Guide/m512techg.htm ; archive locale `artifacts/.tmp-archives/machines/Acorn/BBC Master 512/m512techg.htm`.

Préparer le disque en ADFS, puis exécuter HDISK depuis la disquette DOS : il crée une partition DOS sous forme de fichier ADFS verrouillé DRIVE_C. Le disque C: reste servi par l'ADFS hôte et le contrôleur Winchester existant ; ne pas ajouter de contrôleur PC ni détourner les appels logiciels. La copie des fichiers système pour le rendre amorçable est proposée séparément. Le système accepte ensuite les copies DOS de disquette vers C:.

La disquette 2.1 déjà utilisée contient HDISK.CMD 1.01 et HDINSTAL.BAT. Les chaînes du programme proposent les tailles 4, 10, 20 et 30 Mo ; le banc commencera avec le disque ADFS 306 × 2 × 33 × 256 déjà employé sur Master 128 et l'option 4 Mo. Ces informations ne constituent pas encore une preuve de fonctionnement du HDD dans le Master 512.

## Premier passage Debug, 8 novembre 2026

Le SDK crée le DAT/DSC ADFS dédié de 5 170 176 octets. Le Master 512 démarre DOS Plus 2.1 avec ce disque déjà monté et la disquette système sur le lecteur 0. HDISK.CMD s'exécute et reconnaît le matériel : menu natif 1.01, confirmation puis création de la partition 4 Mo et initialisation de sa FAT. Compteurs cumulés à ce stade : 170 blocs lus, 25 écrits. Captures `20261108-master512-hdd-boot-debug.bmp`, `...-hdisk-loaded-debug.bmp`, `...-confirm-four-debug.bmp`, `...-four-progress-debug.bmp` sous TestsMachines/Acorn/BBC Master 512. La phase Initialising File Allocation Table est une étape, pas encore une partition validée. Aucun changement de production pour ce passage.

La création avance ensuite dans Clearing Root directory : 238 lectures / 93 écritures cumulées après les fenêtres d'attente. Les captures intermédiaires avec un corps noir ne sont pas des verdicts ; le même écran d'étape est de nouveau visible. Les traitements matériels continuent, sans modification des latences, fréquences ni du mode Debug.

## Copie et persistance établies

Après retour au menu HDISK, DIR C: affiche le volume Master 512 vide. COPY A:HDINSTAL.BAT C:CHECK.BAT retourne **1 File(s) copied** (350 lectures / 135 écritures cumulées). TYPE C:CHECK.BAT affiche le batch attendu. ECHO HDD512PASS > C:CHECK.TXT puis TYPE C:CHECK.TXT affichent hdd512pass ; compteurs 352 lectures / 163 écritures. Le banc détache le disque, FlushAsync, ferme puis rouvre le même DAT/DSC et le rattache ; TYPE affiche encore le témoin. Cette relecture DOS n'augmente pas le compteur HDD : ne pas la présenter comme une nouvelle lecture physique, DOS conserve son cache.

La preuve de persistance indépendante du cache DOS provient des octets du DAT après Flush/fermeture/réouverture : les **512 octets complets** de HDINSTAL.BAT, présents à l'offset 284672 de la disquette source, se retrouvent à l'offset **23552 du DAT, sans aucune différence**. Le témoin se trouve à l'offset **25600** : `68 64 64 35 31 32 70 61 73 73 20 0D 0A`, soit hdd512pass, espace et CR/LF, suivis du remplissage 1Ah de DOS. Aucun état CPU injecté et aucune lecture de bus supplémentaire ; les comparaisons portent sur les fichiers hôtes après publication du média.

Captures inspectées : `...-c-directory-debug.bmp`, `...-copy-readback-debug.bmp` (la copie, avant TYPE), `...-text-readback-debug.bmp`, `...-reopened-content-debug.bmp`, préfixe 20261108-master512-hdd. Une fenêtre libre de 80 champs après ces commandes mesure **3327,559 ms, soit 41,594490 ms/champ en Debug**, vidéo et DrainAudio inclus, sans compilation/test concurrent. C'est le repos avec HDD monté, pas le coût des transferts actifs ni les FPS présentés par RML. Les cycles n'ont pas été relevés dans cette fenêtre ; aucune mesure effective MHz ou gain avant/après n'en est déduit.

## Préparation amorçable en cours

L'option Make Hard Disk Partition Bootable est sélectionnée et confirmée depuis HDISK. Le programme commence Copying SYSTEM to Winchester puis Copying SYSTEM BOOT to Winchester ; 443 lectures / 187 écritures cumulées sur le HDD à cette étape. La capture `20261108-master512-hdd-bootable-progress-debug.bmp` conserve cette phase, sans prétendre que l'amorçage a déjà été testé.

HDISK termine la copie SYSTEM, SYSTEM BOOT, 6502.SYS, système DOS et interpréteur de commandes, puis revient au menu sans erreur affichée. Compteurs cumulés finaux : **554 blocs lus / 695 écrits**. Capture `20261108-master512-hdd-bootable-debug.bmp`. Après sortie vers DOS, le média est détaché, publié par FlushAsync, fermé et rouvert ; ses fichiers DAT/DSC sont copiés pour l'essai de démarrage isolé. Le banc HDISK quitte normalement avec code 0 et supprime sa première paire DAT/DSC en finally. La validation du démarrage réel reste distincte de ce résultat de préparation.

## Amorçage réel sur HDD — Debug

Nouvelle instance du même Master 512, MOS 3.20/client 80186 1.01, après fermeture du banc précédent. HDD publié et rouvert ; **seule disquette montée : ADFS 160 Kio vierge créée par le SDK, sans DOS ni fichiers système**. Reset froid, Shift maintenu pendant 121 champs, puis poursuite normale. DOS Plus 2.1 / XIOS 1.03 atteint directement **C>**, avec **718 blocs HDD lus et zéro écrit**. TYPE CHECK.TXT affiche **hdd512pass**, puis C>, avec **724 lectures HDD**, toujours zéro écriture. Captures `20261108-master512-hdd-cold-boot-debug.bmp` et `...-cold-readback-debug.bmp` inspectées, copiées sous TestsChips/Acb4000 et Intel80186/BBC Master 512.

Trois fenêtres de 80 champs au repos après relecture, sans autre émulation, compilation ou test concurrent ; rendu natif et DrainAudio inclus, aucun changement de cadence ou optimisation Release dans Debug :

| Fenêtre | ms/champ | Champs/s sans limite | Cycles CPU hôte | MHz hôte effectifs | Vitesse réelle |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 39,2438475 | 25,481701 | 3 194 880 | 1,017637 | 50,881861 % |
| 2 | 38,45550625 | 26,004078 | 3 194 879 | 1,038499 | 51,924928 % |
| 3 | 39,2418225 | 25,483016 | 3 194 882 | 1,017690 | 50,884519 % |

Les cycles correspondent à environ 50,0801 Hz matériels et 2 MHz nominaux ; le coprocesseur reste configuré à 10 MHz, sans mesure séparée dans ces fenêtres. Les empreintes vidéo sont `7D56A44D330FE1E7212A95FCAACB2A1B917FC55D4CC70D54CBE59505DEEC265C`, `FE141DD3D6EA81EF35523307B664AA3399ECE846298FB1F55F8E8B951499BF3CF`, puis la première de nouveau ; le curseur de l'invite clignote. Aucun gain avant/après revendiqué. Coûts de carte complète au repos, pas FPS présentés par RML ni budget HDD actif.

Les deux bancs HDD terminent normalement avec code 0. Images détachées et fermées dans finally ; DAT, DSC, copie amorçable, disquette vierge et sources de diagnostic supprimés. Native est reconstruit avec Program.cs sans relancer ses scénarios. Chargement HDD, copie disquette/HDD et persistance sont établis ; la clôture globale du Master 512 et sa livraison restent distinctes.
