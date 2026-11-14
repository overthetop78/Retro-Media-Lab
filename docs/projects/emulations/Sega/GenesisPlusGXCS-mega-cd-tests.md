# GenesisPlusGXCS — premier contrôle Mega-CD

Campagne locale du 14 novembre 2026, **Debug exclusivement**. Trois images CUE/BIN exécutées sur 1 800 trames chacune, sans exception. Les empreintes SHA-256 de tous les fichiers des trois dossiers de médias et des BIOS utilisés sont identiques avant/après. Aucun changement du cœur.

## Médias et BIOS

Médias sous `G:/Retro/Cartouches/Sega/Mega CD/` :

- Batman Returns (Europe), BIOS `[BIOS] Mega-CD (Europe) (v1.00).md`.
- Final Fight CD (Europe), même BIOS européen.
- Dragon's Lair (Japan) (En,Ja,Fr,De,It), BIOS `[BIOS] Mega-CD (Japan) (1.00S).md`.

Les BIOS sont pris dans `G:/Retro/Cartouches/Sega/Mega Drive - Genesis/`. Ils sont fournis au cœur par le callback de chargement de firmware, selon la région détectée. Les fichiers sont ouverts en lecture seule. L'image Corpse Killer (Europe) (Mega-CD 32X) est exclue car le matériel 32X n'est pas pris en charge par ce cœur.

## Protocole

Appel direct de `SystemCore`, matériel Mega-CD vérifié après chargement du CUE ; vidéo et audio stéréo 44 100 Hz actifs, région et manette automatiques, aucun saut de trame ni limite de vitesse. 60 trames de chauffe puis 1 800 trames mesurées, soit environ 36,2 secondes émulées PAL ou 30,0 secondes NTSC. Le débit inclut les scènes de démarrage du BIOS et les scènes du disque atteintes pendant ce scénario.

Start aux trames mesurées 180–183, 480–483 et 900–903 ; droite de 1080 à 1379 avec B pendant les quatre premières trames de chaque période de 60. Captures aux trames mesurées 120, 600, 1200 et 1800, hors chauffe. Les entrées sont injectées directement dans le cœur ; le routage clavier/manette de l'application n'est pas testé.

Le temps moteur et les allocations couvrent uniquement `RunFrame`, y compris les lectures CD réalisées pendant cet appel. Les empreintes vidéo, l'analyse de l'audio, les PNG et les SHA-256 des fichiers sont exclus du temps moteur. Les lectures peuvent bénéficier du cache de fichiers du système ; il ne s'agit pas d'un test de débit physique du disque. Le temps CPU inclut les observations du processus.

## Vitesse et observations visuelles

| Jeu | Région | FPS moteur | Vitesse nominale | Temps moteur / 1 800 trames | Observation finale |
|---|---|---:|---:|---:|---|
| Batman Returns | PAL | 27,88 | 56,09 % | 64,571 s | Séquence animée après le BIOS |
| Final Fight CD | PAL | 24,47 | 49,24 % | 73,556 s | Introduction illustrée après la carte de Metro City |
| Dragon's Lair | NTSC | 29,93 | 49,94 % | 60,145 s | Écran Select Language avec cinq drapeaux |

Références : 49,701459 trames/s PAL, 59,922743 trames/s NTSC. **Les trois scénarios restent sous le temps réel en Debug**, à environ la moitié de la vitesse nominale. Aucune mesure Release n'a été effectuée.

Les trois jeux dépassent le BIOS et montrent un contenu du disque. Aucune partie jouable complète n'a été validée dans ce scénario : introductions pour les deux premiers, sélection de langue pour Dragon's Lair. L'absence d'exception ne prouve pas la compatibilité complète ou la fidélité de l'émulation.

## CPU, mémoire et audio

Mo = 1 000 000 octets. Les allocations sont cumulées et ne représentent pas la mémoire conservée. La mémoire managée est observée sans collecte forcée. Le pic résident est échantillonné toutes les 30 trames dans un même processus accueillant successivement les trois jeux ; il n'isole pas la consommation de chaque disque.

| Jeu | Temps CPU processus | Allocations moteur cumulées | Mo alloués / trame | Mémoire managée avant → après | Pic résident observé | Trames avec audio non nul / 1 800 |
|---|---:|---:|---:|---:|---:|---:|
| Batman Returns | 65,219 s | 6 030,76 Mo | 3,350 | 84,74 → 77,24 Mo | 185,13 Mo | 1 465 |
| Final Fight CD | 73,516 s | 7 164,39 Mo | 3,980 | 116,54 → 79,21 Mo | 203,08 Mo | 1 164 |
| Dragon's Lair | 60,547 s | 5 358,48 Mo | 2,977 | 112,64 → 76,03 Mo | 205,94 Mo | 317 |

Les allocations sont élevées : environ **2,98 à 3,98 Mo par trame**. Cette campagne ne localise pas leur origine et ne mesure pas la stabilité mémoire à long terme.

Batman Returns et Final Fight CD produisent chacun 1 597 137 trames audio stéréo ; Dragon's Lair en produit 1 324 706. Les trois scénarios contiennent des échantillons non nuls, mais ce comptage ne distingue pas PSG, FM, PCM et CD-DA. Ni la fidélité du mixage ni son écoute sur un périphérique ne sont validées.

Le JSON contient également le débit temporel du 68000 principal équivalent, dérivé des cycles maîtres par trame divisés par 7 ; il ne s'agit pas d'un comptage indépendant des instructions, d'une mesure du 68000 secondaire ou d'un profil par composant.

## Captures et résultats locaux

12 PNG bruts du framebuffer sont conservés dans le dossier local d'artefacts, ignoré par Git, avec les mesures et les empreintes des médias et BIOS.

- [Batman Returns, trame 1800](../../../../artifacts/GenesisPlusGXCS/mega-cd/Batman%20Returns%20%28Europe%29-frame-1800.png)
- [Final Fight CD, trame 1800](../../../../artifacts/GenesisPlusGXCS/mega-cd/Final%20Fight%20CD%20%28Europe%29-frame-1800.png)
- [Dragon's Lair, trame 1800](../../../../artifacts/GenesisPlusGXCS/mega-cd/Dragon%27s%20Lair%20%28Japan%29%20%28En%2CJa%2CFr%2CDe%2CIt%29-frame-1800.png)
- [JSON détaillé, fichiers ouverts, empreintes et liste des captures](../../../../artifacts/GenesisPlusGXCS/mega-cd/results.json)
- [Journal xUnit : test réussi en 3 min 29 s](../../../../artifacts/GenesisPlusGXCS/mega-cd/test.log)

Le fichier de campagne et ses assemblages temporaires sont supprimés après exploitation ; les captures, mesures et journaux sont conservés.
