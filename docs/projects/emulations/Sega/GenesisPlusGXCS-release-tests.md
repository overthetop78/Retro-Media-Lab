# GenesisPlusGXCS — campagnes Release des cinq machines

Campagne locale du 14 novembre 2026 : **27 ROM/disques distincts, plus Sonic Master System prolongé, tous exécutés sans exception en Release**. Médias et BIOS intacts après exécution ; les empreintes concordent aussi avec les campagnes Debug lorsqu'elles avaient été enregistrées. Le premier contrôle SG-1000 ne contenait pas d'empreintes.

## Configuration et construction

Construction réussie avec `scripts/local-building.cmd --building=release --modules=A`. Présence vérifiée de `F:/GW GUI/build/Release/Retro Media Lab/retromedialab.exe` et des 24 modules. Le test et ses références ont ensuite été recompilés avec `dotnet test -c Release`.

L'assembly Sega effectivement chargé porte `AssemblyConfiguration=Release` et `IsJITOptimizerDisabled=false`, contrôlés par assertions avant les essais. Chemin, SHA-256, MVID et environnement sont enregistrés dans [runtime.json](../../../../artifacts/GenesisPlusGXCS/release/runtime.json). Environnement : .NET 10.0.15, x64, 16 processeurs logiques, GC station de travail. Aucun changement du code de production pour ces essais.

## Résumé du débit moteur

| Machine | FPS Release | Vitesse nominale | Gain par rapport aux valeurs Debug historiques |
|---|---:|---:|---:|
| SG-1000 | 298–4 393 | 498–7 331 % | ×2,23–12,85, comparaison indicative avec observations |
| Master System, 600 trames | 1 668–3 653 | 2 783–6 097 % | ×9,87–32,92 |
| Game Gear | 4 204–6 034 | 7 015–10 070 % | ×28,00–32,73 |
| Mega Drive | 522–1 042 | 872–1 740 % | ×16,64–30,55 |
| Mega-CD | 754–1 010 | 1 516–1 686 % | ×27,31–33,75 |

Sonic Master System prolongé sur 1 800 trames atteint 4 363,99 FPS, soit 7 282,69 % et ×32,26 par rapport au contrôle prolongé Debug.

## Protocole et réserves de comparaison

Même sélection de ROM et mêmes entrées que les campagnes Debug. Chemins adaptés aux dossiers renommés `Game Gear` et `Mega Drive - Genesis`.

- SG-1000 : 180 trames sans chauffe ; Start/Pause aux trames 60–62, bouton 1 de 120 à 139. Capture finale après la boucle chronométrée.
- Master System : 60 trames de chauffe puis 600 mesurées ; bouton 1 à 180–183, bouton 2 à 240–243 et droite à 300–419. Sonic est aussi rejoué sur 1 800 trames.
- Game Gear et Mega Drive : 60 trames de chauffe puis 1 200 mesurées ; Start à 180–183 et 480–483, droite de 540 à 839 avec le premier bouton d'action pendant quatre trames toutes les 60 trames.
- Mega-CD : 60 trames de chauffe puis 1 800 mesurées ; Start à 180–183, 480–483 et 900–903, droite de 1080 à 1379 avec B pendant quatre trames toutes les 60 trames. BIOS européen v1.00 pour Batman Returns et Final Fight CD ; japonais 1.00S pour Dragon's Lair. Corpse Killer Mega-CD 32X reste exclu.

Vidéo et audio stéréo 44 100 Hz actifs, aucun saut de trame ni limite de vitesse ; région et manette automatiques. Les FPS moteur et allocations couvrent uniquement `RunFrame`, y compris les lectures CD éventuelles. Les PNG, empreintes vidéo, analyse audio et SHA-256 des médias sont exclus du temps moteur. Les FPS « avec observations » incluent les traitements des sorties pendant la boucle, dont l'encodage PNG hors SG-1000 ; ils ne représentent pas les performances de l'application.

Le gain compare les FPS moteur aux valeurs historiques Debug, sauf SG-1000 : son ancien compteur incluait les observations et les premiers appels JIT ; le gain utilise donc ici les FPS avec observations. **La comparaison SG-1000 est indicative.** Ces six essais très courts et sans chauffe ne caractérisent pas un débit stabilisé ; les écarts entre les premiers et derniers jeux ne constituent pas un classement propre aux ROM.

Ces campagnes uniques et successives ne forment pas un benchmark statistique alternant Debug et Release. La chauffe de 60 trames est courte : JIT, compilation par paliers, caches et charge hôte peuvent influer sur les mesures. Plusieurs fenêtres moteur Release ne durent que quelques dixièmes de seconde. Les chiffres caractérisent le scénario observé, sans garantir un débit soutenu.

## Captures et fonctionnement

**83 captures communes aux deux campagnes sont identiques octet pour octet**, comparaison SHA-256 des PNG. [Détail des comparaisons](../../../../artifacts/GenesisPlusGXCS/release/visual-comparison.json). 92 captures Release sont conservées au total ; celles du SG-1000 ont été inspectées visuellement, la campagne Debug n'ayant conservé qu'une planche composite.

Les observations précédentes se retrouvent : scènes de jeu pour plusieurs titres, menus ou introductions pour d'autres. Sonic Master System prolongé montre Green Hill. L'image grise de Bomberman Special SG-1000 reste présente ; le menu Wonder Boy III reste silencieux ; Streets of Rage 2 Mega Drive garde une capture finale noire. Les disques Mega-CD dépassent le BIOS, mais une partie complète n'est pas validée : introductions pour Batman Returns et Final Fight CD, sélection des langues pour Dragon's Lair. La Release n'établit pas une compatibilité supplémentaire.

## Allocations, mémoire et audio

Les allocations mesurées dans le thread de `RunFrame` chutent fortement : **0 octet par trame dans plusieurs scénarios Master System et Game Gear**, contre environ 0,54 à 0,71 Mo en Debug. Ce zéro exclut les observations, l'initialisation et les autres threads. Mega Drive : environ 1 à 3 690 octets par trame, contre 2,34 à 2,86 Mo en Debug ; Mega-CD : environ 1 125 à 2 254 octets, contre 2,98 à 3,98 Mo. Aucun profil JIT ne permet d'attribuer précisément ces réductions à une optimisation particulière.

Mo = 1 000 000 octets. Mémoire managée relevée sans collecte forcée ; pics résidents échantillonnés toutes les 30 trames dans un même processus accueillant les 28 scénarios. Ces valeurs n'isolent pas chaque cœur et ne constituent pas un test de fuite. Le temps CPU couvre le processus et ses autres threads. Les fréquences CPU équivalentes du JSON dérivent du temps matériel simulé, sans comptage indépendant des instructions.

L'audio est produit et son activité numérique comptée ; fidélité et lecture sur un périphérique non validées. Présentation, synchronisation, clavier et manettes de l'application ne sont pas mesurés : les entrées sont injectées dans le cœur.

## Artefacts

- [Construction Release complète](../../../../artifacts/GenesisPlusGXCS/release/build.log).
- [Journal xUnit : 28 scénarios dans un test réussi de 35 secondes](../../../../artifacts/GenesisPlusGXCS/release/test.log).
- [Configuration du cœur exécuté](../../../../artifacts/GenesisPlusGXCS/release/runtime.json).
- [Comparaison des captures et bilan des médias intacts](../../../../artifacts/GenesisPlusGXCS/release/visual-comparison.json).

Les résultats, PNG et journaux sont locaux et ignorés par Git. Les artefacts Debug sont conservés. Le fichier de campagne et ses assemblages temporaires sont supprimés après exploitation ; les binaires Release de l'application et des modules sont conservés.

## Mesures détaillées par ROM

Chaque nom renvoie à la dernière capture Release. La vitesse nominale tient compte de la région, environ 59,92 FPS NTSC ou 49,70 FPS PAL. Réserve SG-1000 : le gain utilise la colonne avec observations.

### sg1000

| ROM / région | Debug FPS | Release FPS moteur | FPS avec observations | Vitesse nominale | Gain |
|---|---:|---:|---:|---:|---:|
| [Champion Tennis (Japan)](../../../../artifacts/GenesisPlusGXCS/release/sg1000/Champion%20Tennis%20%28Japan%29-frame-180.png) / NTSC | 119,70 | 298,13 | 266,85 | 497,52 % | ×2,23 |
| [007 James Bond (Japan) (v2.7) (Othello Multivision)](../../../../artifacts/GenesisPlusGXCS/release/sg1000/007%20James%20Bond%20%28Japan%29%20%28v2.7%29%20%28Othello%20Multivision%29-frame-180.png) / NTSC | 130,26 | 366,11 | 323,53 | 610,97 % | ×2,48 |
| [Congo Bongo (Japan)](../../../../artifacts/GenesisPlusGXCS/release/sg1000/Congo%20Bongo%20%28Japan%29-frame-180.png) / NTSC | 127,08 | 355,79 | 315,36 | 593,75 % | ×2,48 |
| [Bank Panic (Japan)](../../../../artifacts/GenesisPlusGXCS/release/sg1000/Bank%20Panic%20%28Japan%29-frame-180.png) / NTSC | 120,53 | 403,67 | 347,81 | 673,66 % | ×2,89 |
| [007 James Bond (Japan) (v2.6) (Othello Multivision)](../../../../artifacts/GenesisPlusGXCS/release/sg1000/007%20James%20Bond%20%28Japan%29%20%28v2.6%29%20%28Othello%20Multivision%29-frame-180.png) / NTSC | 130,46 | 4356,12 | 1676,54 | 7269,56 % | ×12,85 |
| [Bomberman Special (Taiwan) (DahJee)](../../../../artifacts/GenesisPlusGXCS/release/sg1000/Bomberman%20Special%20%28Taiwan%29%20%28DahJee%29-frame-180.png) / NTSC | 134,86 | 4393,09 | 1691,16 | 7331,26 % | ×12,54 |

| ROM | Temps moteur | CPU processus | Octets alloués / trame | Mémoire managée avant → après | Pic résident observé | Audio non nul |
|---|---:|---:|---:|---:|---:|---:|
| Champion Tennis (Japan) | 0,604 s | 0,828 s | 1464,40 | 75,25 → 75,53 Mo | 164,68 Mo | 0/180 trames |
| 007 James Bond (Japan) (v2.7) (Othello Multivision) | 0,492 s | 0,719 s | 0,00 | 101,11 → 101,13 Mo | 180,60 Mo | 4/180 trames |
| Congo Bongo (Japan) | 0,506 s | 0,656 s | 0,00 | 126,39 → 126,41 Mo | 195,84 Mo | 102/180 trames |
| Bank Panic (Japan) | 0,446 s | 0,812 s | 0,00 | 151,67 → 151,70 Mo | 212,70 Mo | 147/180 trames |
| 007 James Bond (Japan) (v2.6) (Othello Multivision) | 0,041 s | 0,094 s | 0,00 | 188,53 → 100,86 Mo | 221,32 Mo | 4/180 trames |
| Bomberman Special (Taiwan) (DahJee) | 0,041 s | 0,109 s | 0,00 | 126,35 → 126,38 Mo | 232,57 Mo | 0/180 trames |

[JSON détaillé](../../../../artifacts/GenesisPlusGXCS/release/sg1000/results.json).

### master-system

| ROM / région | Debug FPS | Release FPS moteur | FPS avec observations | Vitesse nominale | Gain |
|---|---:|---:|---:|---:|---:|
| [Alex Kidd in Miracle World (USA, Europe)](../../../../artifacts/GenesisPlusGXCS/release/master-system/Alex%20Kidd%20in%20Miracle%20World%20%28USA%2C%20Europe%29-frame-600.png) / NTSC | 168,91 | 1667,56 | 1027,83 | 2782,86 % | ×9,87 |
| [Sonic The Hedgehog (USA, Europe)](../../../../artifacts/GenesisPlusGXCS/release/master-system/Sonic%20The%20Hedgehog%20%28USA%2C%20Europe%29-frame-600.png) / NTSC | 176,38 | 2092,25 | 1163,23 | 3491,58 % | ×11,86 |
| [Wonder Boy III - The Dragon's Trap (USA, Europe)](../../../../artifacts/GenesisPlusGXCS/release/master-system/Wonder%20Boy%20III%20-%20The%20Dragon%27s%20Trap%20%28USA%2C%20Europe%29-frame-600.png) / NTSC | 90,37 | 1834,58 | 1102,71 | 3061,58 % | ×20,30 |
| [R-Type (World)](../../../../artifacts/GenesisPlusGXCS/release/master-system/R-Type%20%28World%29-frame-600.png) / NTSC | 85,58 | 2612,18 | 1327,27 | 4359,24 % | ×30,52 |
| [Phantasy Star (USA, Europe) (Rev 3)](../../../../artifacts/GenesisPlusGXCS/release/master-system/Phantasy%20Star%20%28USA%2C%20Europe%29%20%28Rev%203%29-frame-600.png) / NTSC | 167,14 | 3653,38 | 1531,55 | 6096,81 % | ×21,86 |
| [After Burner (World)](../../../../artifacts/GenesisPlusGXCS/release/master-system/After%20Burner%20%28World%29-frame-600.png) / NTSC | 86,80 | 2857,94 | 1365,68 | 4769,38 % | ×32,92 |

| ROM | Temps moteur | CPU processus | Octets alloués / trame | Mémoire managée avant → après | Pic résident observé | Audio non nul |
|---|---:|---:|---:|---:|---:|---:|
| Alex Kidd in Miracle World (USA, Europe) | 0,360 s | 0,766 s | 0,00 | 100,93 → 101,10 Mo | 238,16 Mo | 570/600 trames |
| Sonic The Hedgehog (USA, Europe) | 0,287 s | 0,734 s | 0,00 | 126,50 → 126,67 Mo | 253,44 Mo | 600/600 trames |
| Wonder Boy III - The Dragon's Trap (USA, Europe) | 0,327 s | 0,578 s | 0,00 | 100,86 → 100,99 Mo | 269,01 Mo | 0/600 trames |
| R-Type (World) | 0,230 s | 0,484 s | 0,00 | 126,46 → 126,59 Mo | 269,08 Mo | 385/600 trames |
| Phantasy Star (USA, Europe) (Rev 3) | 0,164 s | 0,406 s | 0,00 | 101,45 → 101,61 Mo | 261,77 Mo | 599/600 trames |
| After Burner (World) | 0,210 s | 0,453 s | 0,00 | 127,03 → 127,20 Mo | 274,03 Mo | 532/600 trames |

[JSON détaillé](../../../../artifacts/GenesisPlusGXCS/release/master-system/results.json).

### master-system/sonic-followup

| ROM / région | Debug FPS | Release FPS moteur | FPS avec observations | Vitesse nominale | Gain |
|---|---:|---:|---:|---:|---:|
| [Sonic The Hedgehog (USA, Europe)](../../../../artifacts/GenesisPlusGXCS/release/master-system/sonic-followup/Sonic%20The%20Hedgehog%20%28USA%2C%20Europe%29-frame-1800.png) / NTSC | 135,26 | 4363,99 | 1730,23 | 7282,69 % | ×32,26 |

| ROM | Temps moteur | CPU processus | Octets alloués / trame | Mémoire managée avant → après | Pic résident observé | Audio non nul |
|---|---:|---:|---:|---:|---:|---:|
| Sonic The Hedgehog (USA, Europe) | 0,412 s | 1,062 s | 0,00 | 101,40 → 101,79 Mo | 285,60 Mo | 1800/1800 trames |

[JSON détaillé](../../../../artifacts/GenesisPlusGXCS/release/master-system/sonic-followup/results.json).

### game-gear

| ROM / région | Debug FPS | Release FPS moteur | FPS avec observations | Vitesse nominale | Gain |
|---|---:|---:|---:|---:|---:|
| [Sonic The Hedgehog (World) (Rev 1)](../../../../artifacts/GenesisPlusGXCS/release/game-gear/Sonic%20The%20Hedgehog%20%28World%29%20%28Rev%201%29-frame-1200.png) / NTSC | 150,13 | 4203,66 | 1620,82 | 7015,13 % | ×28,00 |
| [Shinobi (USA, Europe)](../../../../artifacts/GenesisPlusGXCS/release/game-gear/Shinobi%20%28USA%2C%20Europe%29-frame-1200.png) / NTSC | 163,23 | 5090,10 | 1782,07 | 8494,44 % | ×31,18 |
| [Columns (USA, Europe)](../../../../artifacts/GenesisPlusGXCS/release/game-gear/Columns%20%28USA%2C%20Europe%29-frame-1200.png) / NTSC | 192,75 | 6034,26 | 1920,05 | 10070,07 % | ×31,31 |
| [Aladdin (USA, Europe)](../../../../artifacts/GenesisPlusGXCS/release/game-gear/Aladdin%20%28USA%2C%20Europe%29-frame-1200.png) / NTSC | 193,19 | 5436,35 | 1803,48 | 9072,27 % | ×28,14 |
| [Ristar - The Shooting Star (World)](../../../../artifacts/GenesisPlusGXCS/release/game-gear/Ristar%20-%20The%20Shooting%20Star%20%28World%29-frame-1200.png) / NTSC | 171,65 | 5618,13 | 1860,40 | 9375,62 % | ×32,73 |
| [GG Aleste (Japan)](../../../../artifacts/GenesisPlusGXCS/release/game-gear/GG%20Aleste%20%28Japan%29-frame-1200.png) / NTSC | 183,79 | 5782,49 | 1883,52 | 9649,90 % | ×31,46 |

| ROM | Temps moteur | CPU processus | Octets alloués / trame | Mémoire managée avant → après | Pic résident observé | Audio non nul |
|---|---:|---:|---:|---:|---:|---:|
| Sonic The Hedgehog (World) (Rev 1) | 0,285 s | 0,766 s | 0,00 | 127,32 → 127,56 Mo | 285,76 Mo | 1093/1200 trames |
| Shinobi (USA, Europe) | 0,236 s | 0,641 s | 0,00 | 101,46 → 101,68 Mo | 271,25 Mo | 1084/1200 trames |
| Columns (USA, Europe) | 0,199 s | 0,656 s | 0,00 | 127,15 → 127,38 Mo | 277,11 Mo | 968/1200 trames |
| Aladdin (USA, Europe) | 0,221 s | 0,656 s | 0,21 | 101,49 → 101,72 Mo | 290,79 Mo | 628/1200 trames |
| Ristar - The Shooting Star (World) | 0,214 s | 0,656 s | 0,00 | 124,36 → 124,58 Mo | 281,26 Mo | 1192/1200 trames |
| GG Aleste (Japan) | 0,208 s | 0,656 s | 0,00 | 78,22 → 78,44 Mo | 270,28 Mo | 988/1200 trames |

[JSON détaillé](../../../../artifacts/GenesisPlusGXCS/release/game-gear/results.json).

### mega-drive

| ROM / région | Debug FPS | Release FPS moteur | FPS avec observations | Vitesse nominale | Gain |
|---|---:|---:|---:|---:|---:|
| [Sonic The Hedgehog (USA, Europe)](../../../../artifacts/GenesisPlusGXCS/release/mega-drive/Sonic%20The%20Hedgehog%20%28USA%2C%20Europe%29-frame-1200.png) / NTSC | 31,40 | 522,41 | 436,49 | 871,81 % | ×16,64 |
| [Golden Axe (World)](../../../../artifacts/GenesisPlusGXCS/release/mega-drive/Golden%20Axe%20%28World%29-frame-1200.png) / NTSC | 34,62 | 978,92 | 705,66 | 1633,63 % | ×28,28 |
| [Streets of Rage 2 (USA)](../../../../artifacts/GenesisPlusGXCS/release/mega-drive/Streets%20of%20Rage%202%20%28USA%29-frame-1200.png) / NTSC | 34,12 | 1042,46 | 748,03 | 1739,67 % | ×30,55 |
| [Gunstar Heroes (USA)](../../../../artifacts/GenesisPlusGXCS/release/mega-drive/Gunstar%20Heroes%20%28USA%29-frame-1200.png) / NTSC | 35,54 | 978,23 | 715,84 | 1632,48 % | ×27,53 |
| [Thunder Force III (Japan, USA)](../../../../artifacts/GenesisPlusGXCS/release/mega-drive/Thunder%20Force%20III%20%28Japan%2C%20USA%29-frame-1200.png) / NTSC | 29,46 | 744,56 | 577,68 | 1242,54 % | ×25,27 |
| [Aladdin (Europe)](../../../../artifacts/GenesisPlusGXCS/release/mega-drive/Aladdin%20%28Europe%29-frame-1200.png) / PAL | 28,37 | 784,44 | 604,65 | 1578,30 % | ×27,65 |

| ROM | Temps moteur | CPU processus | Octets alloués / trame | Mémoire managée avant → après | Pic résident observé | Audio non nul |
|---|---:|---:|---:|---:|---:|---:|
| Sonic The Hedgehog (USA, Europe) | 2,297 s | 3,453 s | 828,16 | 104,09 → 105,31 Mo | 275,83 Mo | 992/1200 trames |
| Golden Axe (World) | 1,226 s | 1,844 s | 1,33 | 78,38 → 78,76 Mo | 255,96 Mo | 976/1200 trames |
| Streets of Rage 2 (USA) | 1,151 s | 1,672 s | 3690,03 | 104,40 → 109,10 Mo | 256,85 Mo | 413/1200 trames |
| Gunstar Heroes (USA) | 1,227 s | 1,750 s | 2399,84 | 78,97 → 82,25 Mo | 253,62 Mo | 637/1200 trames |
| Thunder Force III (Japan, USA) | 1,612 s | 2,078 s | 594,51 | 102,91 → 104,05 Mo | 253,74 Mo | 1006/1200 trames |
| Aladdin (Europe) | 1,530 s | 2,016 s | 416,59 | 78,42 → 79,28 Mo | 230,35 Mo | 905/1200 trames |

[JSON détaillé](../../../../artifacts/GenesisPlusGXCS/release/mega-drive/results.json).

### mega-cd

| ROM / région | Debug FPS | Release FPS moteur | FPS avec observations | Vitesse nominale | Gain |
|---|---:|---:|---:|---:|---:|
| [Batman Returns (Europe)](../../../../artifacts/GenesisPlusGXCS/release/mega-cd/Batman%20Returns%20%28Europe%29-frame-1800.png) / PAL | 27,88 | 761,37 | 589,71 | 1531,89 % | ×27,31 |
| [Final Fight CD (Europe)](../../../../artifacts/GenesisPlusGXCS/release/mega-cd/Final%20Fight%20CD%20%28Europe%29-frame-1800.png) / PAL | 24,47 | 753,57 | 587,98 | 1516,19 % | ×30,79 |
| [Dragon's Lair (Japan) (En,Ja,Fr,De,It)](../../../../artifacts/GenesisPlusGXCS/release/mega-cd/Dragon%27s%20Lair%20%28Japan%29%20%28En%2CJa%2CFr%2CDe%2CIt%29-frame-1800.png) / NTSC | 29,93 | 1010,01 | 740,47 | 1685,52 % | ×33,75 |

| ROM | Temps moteur | CPU processus | Octets alloués / trame | Mémoire managée avant → après | Pic résident observé | Audio non nul |
|---|---:|---:|---:|---:|---:|---:|
| Batman Returns (Europe) | 2,364 s | 3,344 s | 2254,08 | 105,44 → 110,06 Mo | 237,89 Mo | 1465/1800 trames |
| Final Fight CD (Europe) | 2,389 s | 3,188 s | 1138,29 | 78,75 → 81,25 Mo | 246,61 Mo | 1164/1800 trames |
| Dragon's Lair (Japan) (En,Ja,Fr,De,It) | 1,782 s | 2,453 s | 1125,40 | 107,17 → 103,37 Mo | 245,66 Mo | 317/1800 trames |

[JSON détaillé](../../../../artifacts/GenesisPlusGXCS/release/mega-cd/results.json).
