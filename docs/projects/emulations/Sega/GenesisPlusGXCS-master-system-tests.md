# GenesisPlusGXCS — premier contrôle Master System

Campagne locale Debug du 13 novembre 2026. Six ROM chargées et exécutées sans exception ; leurs SHA-256 sont identiques avant et après exécution. Aucun changement du cœur n'a été effectué pour ces essais.

## Protocole

Appel direct du cœur C# `SystemCore`, Master System II sans BIOS, région automatique : NTSC pour les six ROM, fréquence nominale de 59,922743 trames/s. Vidéo et audio stéréo 44 100 Hz actifs, aucun saut de trame et aucune limitation de vitesse. Après 60 trames de chauffe, 600 trames mesurées par ROM, soit environ 10 secondes émulées. Bouton 1 aux trames mesurées 180–183, bouton 2 aux trames 240–243, droite aux trames 300–419 ; aucune autre entrée. Les numéros des captures excluent la chauffe.

Le temps moteur et les allocations couvrent seulement `RunFrame`. L'encodage PNG, les empreintes vidéo et les observations sont exclus de ce temps. Le temps CPU du processus inclut les observations et peut dépasser le temps moteur. Ces débits ne mesurent pas la présentation, la synchronisation audio ni le routage clavier/manette de l'application. Les entrées sont injectées dans le cœur.

## Débit et observations

| ROM | FPS moteur | Vitesse nominale | Temps moteur, 600 trames | Observation visuelle |
|---|---:|---:|---:|---|
| Alex Kidd in Miracle World | 168,91 | 281,89 % | 3,552 s | Personnage et plateformes en jeu |
| Sonic The Hedgehog | 176,38 | 294,34 % | 3,402 s | Titre puis Green Hill ; transition noire à 600 trames |
| Wonder Boy III — The Dragon's Trap | 90,37 | 150,82 % | 6,639 s | Menu Continue / New Game |
| R-Type | 85,58 | 142,82 % | 7,011 s | Vaisseau, décor et affichage des scores |
| Phantasy Star, Rev 3 | 167,14 | 278,92 % | 3,590 s | Introduction avec planète ; exploration non vérifiée |
| After Burner | 86,80 | 144,86 % | 6,912 s | Avion et écran Ready Stage 1 |

Sonic a ensuite été rejoué seul sur 1 800 trames mesurées, soit environ 30 secondes émulées : **135,26 FPS, 225,73 %**, en 13,307 s de temps moteur. Les captures aux trames 900, 1 200 et 1 800 montrent le jeu dans Green Hill : le noir à 600 trames ne persiste pas. La baisse de débit par rapport à la campagne courte souligne que le coût dépend de la scène exécutée.

## CPU, mémoire et audio

Mo = 1 000 000 octets ; les allocations sont cumulées et ne représentent pas la mémoire conservée. Le pic résident est échantillonné toutes les 30 trames dans un même processus de test pour les six jeux : il n'isole pas la consommation propre à chaque ROM. La mémoire managée est lue sans forcer de collecte.

| ROM | Temps CPU processus | Allocations moteur cumulées | ko alloués / trame | Mémoire managée avant → après | Pic résident observé | Trames avec audio non nul / 600 |
|---|---:|---:|---:|---:|---:|---:|
| Alex Kidd | 4,141 s | 417,48 Mo | 695,80 | 87,37 → 86,88 Mo | 185,02 Mo | 570 |
| Sonic | 3,750 s | 394,02 Mo | 656,70 | 101,63 → 111,10 Mo | 199,48 Mo | 600 |
| Wonder Boy III | 6,906 s | 352,16 Mo | 586,94 | 128,19 → 129,12 Mo | 210,54 Mo | 0 |
| R-Type | 7,281 s | 323,21 Mo | 538,69 | 153,95 → 158,59 Mo | 224,71 Mo | 385 |
| Phantasy Star | 3,875 s | 414,81 Mo | 691,36 | 111,06 → 107,83 Mo | 225,93 Mo | 599 |
| After Burner | 7,219 s | 390,08 Mo | 650,13 | 132,15 → 135,89 Mo | 237,05 Mo | 532 |

Les allocations restent importantes : environ 0,54 à 0,70 Mo par trame. Le prolongement Sonic alloue 1 124,36 Mo cumulés, avec 77,44 → 81,51 Mo managés et un pic résident observé de 183,89 Mo dans son processus distinct. Ce contrôle court ne permet pas de conclure sur la stabilité mémoire à long terme.

Les six jeux produisent chacun 441 569 trames audio stéréo pendant les 600 trames vidéo. Le comptage d'audio non nul confirme une activité numérique, pas la fidélité du son ni sa lecture sur le périphérique. Wonder Boy III reste silencieux sur le scénario du menu ; la cause n'a pas été diagnostiquée. Sonic prolongé produit de l'audio non nul sur les 1 800 trames.

Le débit temporel CPU calculé depuis la durée des trames et l'horloge maître est de 5,11 à 10,54 MHz équivalents pour un Z80 nominal à 3,579545 MHz ; Sonic prolongé atteint 8,08 MHz équivalents. Il s'agit d'un débit de temps simulé, pas d'un comptage indépendant des instructions exécutées.

## Captures et données

Captures brutes du framebuffer, sans redimensionnement. Les captures intermédiaires restent dans le même dossier.

- [Alex Kidd, trame 600](../../../../artifacts/GenesisPlusGXCS/master-system/Alex%20Kidd%20in%20Miracle%20World%20%28USA%2C%20Europe%29-frame-600.png)
- [Sonic en jeu, trame 1800](../../../../artifacts/GenesisPlusGXCS/master-system/sonic-followup/Sonic%20The%20Hedgehog%20%28USA%2C%20Europe%29-frame-1800.png)
- [Wonder Boy III, trame 600](../../../../artifacts/GenesisPlusGXCS/master-system/Wonder%20Boy%20III%20-%20The%20Dragon%27s%20Trap%20%28USA%2C%20Europe%29-frame-600.png)
- [R-Type, trame 600](../../../../artifacts/GenesisPlusGXCS/master-system/R-Type%20%28World%29-frame-600.png)
- [Phantasy Star, trame 600](../../../../artifacts/GenesisPlusGXCS/master-system/Phantasy%20Star%20%28USA%2C%20Europe%29%20%28Rev%203%29-frame-600.png)
- [After Burner, trame 600](../../../../artifacts/GenesisPlusGXCS/master-system/After%20Burner%20%28World%29-frame-600.png)
- [Résultats détaillés des six ROM](../../../../artifacts/GenesisPlusGXCS/master-system/results.json) et [journal du test réussi](../../../../artifacts/GenesisPlusGXCS/master-system/test.log)
- [Résultats Sonic prolongé](../../../../artifacts/GenesisPlusGXCS/master-system/sonic-followup/results.json) et [journal du test réussi](../../../../artifacts/GenesisPlusGXCS/master-system/sonic-followup-test.log)

Les artefacts sont locaux et ignorés par Git. Le fichier de campagne temporaire est supprimé après exploitation, conformément aux règles de travail. Ces essais courts montrent un démarrage et un débit supérieur au temps réel dans les scènes observées ; ils ne constituent pas une validation complète de compatibilité, des sauvegardes, des périphériques ou de la fidélité matérielle.
