# GenesisPlusGXCS — premier contrôle Game Gear

Campagne locale du 14 novembre 2026, **Debug exclusivement**. Six ROM exécutées sans exception sur 1 200 trames chacune ; leurs SHA-256 avant/après confirment qu'elles sont intactes. Aucun changement du cœur d'émulation.

## Protocole

Même méthode que les contrôles Master System et Mega Drive : appel direct de `SystemCore`, Game Gear sans BIOS, région et manette automatiques, vidéo et audio stéréo 44 100 Hz actifs, sans saut de trame ni limitation de vitesse. Détection NTSC pour les six jeux, fréquence nominale de 59,922743 trames/s. 60 trames de chauffe puis 1 200 trames mesurées, soit environ 20 secondes émulées par jeu.

Start aux trames mesurées 180–183 puis 480–483 ; droite de 540 à 839 avec bouton 1 pendant les quatre premières trames de chaque période de 60. Captures aux trames mesurées 120, 300, 600 et 1 200, après la chauffe. La sortie native est de 160 × 144 pixels : la fenêtre Game Gear du framebuffer est déjà positionnée à l'origine par le rendu du cœur ; les captures sont enregistrées sans redimensionnement.

Le temps moteur et les allocations couvrent uniquement `RunFrame`. L'encodage PNG, les empreintes vidéo et l'analyse audio sont exclus du temps moteur. Le temps CPU du processus inclut les observations. Ces essais ne mesurent pas la présentation dans l'application, la sortie audio sur le périphérique ni le routage clavier/manette de l'application : les entrées sont injectées dans le cœur.

## Vitesse et observations

| ROM | FPS moteur | Vitesse nominale | Temps moteur / 1 200 trames | Observation de la capture finale |
|---|---:|---:|---:|---|
| Sonic The Hedgehog (World), Rev 1 | 150,13 | 250,53 % | 7,993 s | Scène de jeu Green Hill, anneaux et compteur visibles |
| Shinobi (USA, Europe) | 163,23 | 272,39 % | 7,352 s | Scène de jeu avec personnage et camion |
| Columns (USA, Europe) | 192,75 | 321,67 % | 6,226 s | Plateau, pièces, score et indication Next visibles |
| Aladdin (USA, Europe) | 193,19 | 322,40 % | 6,211 s | Illustration de présentation avec le Génie ; jeu effectif non vérifié |
| Ristar — The Shooting Star (World) | 171,65 | 286,45 % | 6,991 s | Écran titre ; jeu effectif non vérifié |
| GG Aleste (Japan) | 183,79 | 306,71 % | 6,529 s | Scène de jeu avec vaisseau et décor |

**Les six scénarios dépassent le temps réel en Debug**, avec un débit de 2,51 à 3,22 fois la vitesse nominale. Aucune mesure Release n'a été effectuée. Le coût dépend des scènes exécutées ; cette campagne courte ne permet pas d'affirmer que ce débit se maintient dans toutes les situations de chaque jeu.

## CPU, mémoire et audio

Mo = 1 000 000 octets. Les allocations sont cumulées et ne représentent pas la mémoire conservée. La mémoire managée est observée sans collecte forcée. Le pic résident est échantillonné toutes les 30 trames dans un même processus accueillant successivement les six ROM ; il n'isole pas la consommation propre de chaque jeu.

| ROM | Temps CPU processus | Allocations moteur cumulées | ko alloués / trame | Mémoire managée avant → après | Pic résident observé | Trames avec audio non nul / 1 200 |
|---|---:|---:|---:|---:|---:|---:|
| Sonic | 9,172 s | 824,83 Mo | 687,36 | 76,67 → 82,18 Mo | 179,27 Mo | 1 093 |
| Shinobi | 8,109 s | 769,95 Mo | 641,62 | 113,74 → 114,39 Mo | 193,08 Mo | 1 084 |
| Columns | 6,844 s | 700,87 Mo | 584,06 | 142,56 → 141,05 Mo | 205,66 Mo | 968 |
| Aladdin | 6,766 s | 850,29 Mo | 708,57 | 159,81 → 156,26 Mo | 220,01 Mo | 628 |
| Ristar | 7,719 s | 750,31 Mo | 625,26 | 111,28 → 109,04 Mo | 225,41 Mo | 1 192 |
| GG Aleste | 7,250 s | 722,50 Mo | 602,08 | 137,87 → 139,56 Mo | 236,58 Mo | 988 |

Les allocations restent importantes : **0,58 à 0,71 Mo par trame**. L'essai ne localise pas leur origine et ne constitue pas une mesure de stabilité mémoire à long terme.

Chaque ROM produit 883 137 trames audio stéréo pendant les 1 200 trames vidéo. Toutes produisent des échantillons non nuls ; ce contrôle ne valide ni la fidélité du son ni son écoute sur le périphérique.

Le débit temporel Z80 équivalent calculé depuis les cycles maîtres par trame divisés par 15 s'étend de 8,97 à 11,54 MHz, pour une fréquence nominale de 3,579545 MHz. Il s'agit d'un débit de temps simulé et non d'un comptage indépendant des instructions exécutées ou d'un profil par composant.

## Captures et résultats

24 captures PNG natives de 160 × 144 pixels sont conservées avec les données dans le dossier local d'artefacts, ignoré par Git.

- [Sonic, trame 1200](../../../../artifacts/GenesisPlusGXCS/game-gear/Sonic%20The%20Hedgehog%20%28World%29%20%28Rev%201%29-frame-1200.png)
- [Shinobi, trame 1200](../../../../artifacts/GenesisPlusGXCS/game-gear/Shinobi%20%28USA%2C%20Europe%29-frame-1200.png)
- [Columns, trame 1200](../../../../artifacts/GenesisPlusGXCS/game-gear/Columns%20%28USA%2C%20Europe%29-frame-1200.png)
- [Aladdin, trame 1200](../../../../artifacts/GenesisPlusGXCS/game-gear/Aladdin%20%28USA%2C%20Europe%29-frame-1200.png)
- [Ristar, trame 1200](../../../../artifacts/GenesisPlusGXCS/game-gear/Ristar%20-%20The%20Shooting%20Star%20%28World%29-frame-1200.png)
- [GG Aleste, trame 1200](../../../../artifacts/GenesisPlusGXCS/game-gear/GG%20Aleste%20%28Japan%29-frame-1200.png)
- [JSON détaillé avec empreintes et liste des captures](../../../../artifacts/GenesisPlusGXCS/game-gear/results.json)
- [Journal xUnit : campagne réussie, test de 48 secondes](../../../../artifacts/GenesisPlusGXCS/game-gear/test.log)

Le fichier de campagne et ses assemblages temporaires sont supprimés après exploitation. Ces essais montrent un démarrage, une activité audio et des images reconnaissables, avec des scènes de jeu pour quatre titres. Ils ne valident pas la compatibilité complète, la fidélité matérielle, les sauvegardes ni les périphériques.
