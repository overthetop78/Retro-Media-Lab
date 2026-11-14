# SegaEmu

État du 14 novembre 2026. SegaEmu 1.0.0 est une reconstruction indépendante à partir des sources locales de GenesisPlusGXCS 1.7.6+2499cd3. À la fin de cette reconstruction, les 963 empreintes de départ de GenesisPlusGXCS et des composants Chips préexistants étaient identiques. L'audit des options demandé ensuite modifie aussi GenesisPlusGXCS ; ses corrections sont détaillées en fin de document. Les composants Chips ne sont pas modifiés par cet audit.

La comparaison A/B contemporaine en Debug et Release, détaillée en fin de document, relève des pertes de vitesse et une divergence audio reproductible sur Final Fight CD. Cette divergence reste à corriger ; la présence d'audio et les captures identiques des premiers essais ne prouvaient pas son égalité PCM.

## Architecture et intégration

Le code propre à l'émulateur se trouve dans `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/`. Chaque machine possède une classe sous `Machines/` : SG-1000, SC-3000, Mark III, Master System, Game Gear, Mega Drive, Mega-CD et Pico. La fabrique choisit une composition ; chaque classe définit son initialisation, son câblage et sa boucle de trame. La base abstraite partage bus, cartouches, mémoires et services nécessaires aux cartes. Les composants optionnels Mega Drive/Mega-CD ne sont pas créés sur les machines 8 bits.

Les 126 nouveaux fichiers de composants réutilisables appartiennent à Chips : M68000, Z80, SSP1601, VDP, entrées Sega, PSG et FM, RF5C164, CDC/CDD, accélérateur graphique CD, EEPROM, flash et contrôleur YX5200. Leurs contrats de bus, mémoires et médias sont indépendants de l'émulateur. Les mappers, bases de cartouches et assemblages de cartes restent dans SegaEmu.

MediaEngine assure la lecture des images optiques, secteurs, sous-canaux et pistes audio. SegaEmu adapte ces données au contrat du lecteur CDD. Les lecteurs MediaEngine conservent CUE/BIN, ISO et CHD, y compris les décodeurs CHD compressés transférés dans MediaEngine. La lecture Ogg, les pistes WAV/Ogg associées, leurs pregaps et les métadonnées LOOP/NOLOOP passent également par MediaEngine. Le précache est optionnel. La signature publique préexistante du constructeur optique à 22 paramètres est préservée.

SegaEmu est enregistré séparément dans le catalogue commun. Sa fabrique et son adaptateur utilisent les services existants de session, vidéo, audio, clavier, manettes, pointeurs, firmware, disques, sauvegardes persistantes et états. Les constantes, options, BIOS et préparations de contenu lui appartiennent ; aucune référence à un autre émulateur n'a été relevée dans ses 87 fichiers source. Les ressources communes de Sega et les contrats Emulation restent partagés.

Les options comprennent variantes matérielles, région et fréquence, BIOS, cartouches et extensions, overclock, rendu, filtres, limite de sprites, curseurs, mixage et volumes par canal, RAM Mega-CD et précache. Les variantes restent éditables ; les options exigeant une reconstruction de la machine sont signalées. Les 693 clés propres comprennent 184 textes localisés dans les 29 cultures et 509 valeurs invariantes dans la base commune. Les nouvelles traductions utilisent Argos. Les autres ressources préexistantes sont inchangées.

Le format d'état propre est `SEGAEMU 00000002`. Il contient le contexte complet des deux M68000 lorsque présents. Les états ne sont pas interchangeables avec GenesisPlusGXCS. Le contrôle réel a révélé un registre d'identification reconstruit comme pour une Mega Drive au chargement d'un état Mega-CD : la correction conserve l'identification matérielle de la machine active.

## Validation

- 34 tests autonomes passent : processeurs, périphériques, volumes, compositions des machines, états, manette, variantes éditables, métadonnées et compatibilité du constructeur optique.
- Les essais temporaires de médias ont validé quatre profils CHD synthétiques (v3/v4 zlib, v5 compressé et v5 non compressé), secteurs et audio, Ogg silencieux synthétique, CUE, WAV, pistes associées, boucles, précache et annulation. Le transformateur Vorbis a aussi été comparé sur huit tailles de bloc.
- La campagne Debug exécute 29 scénarios : six SG-1000, six Master System, six Game Gear, six Mega Drive, trois Mega-CD, un SC-3000 et un Mark III. Les 87 captures comparables sont identiques pixel par pixel aux références locales. Les trois différences d'état Mega-CD ont été corrigées, puis les trois scénarios ont repassé en Debug avec états binaires et images identiques après reprise.
- La campagne Release finale repasse les 29 scénarios : chargement, exécution, états binaires et vidéo, adaptateur de session, présence d'audio, éjection/réinsertion CD et intégrité des médias. Les 87 captures comparables sont identiques. Aucun scénario n'a levé d'erreur. L'égalité PCM avec GenesisPlusGXCS est contrôlée séparément dans la comparaison A/B ci-dessous.
- Les scripts complets `scripts/local-building.cmd --building=debug --modules=A` et `scripts/local-building.cmd --building=release --modules=A` réussissent ; l'application et les 24 DLL de modules sont présentes dans chaque distribution.

Les ROM, fichiers associés et trois BIOS Mega-CD fournis ont les mêmes SHA-256 avant et après les essais. Les cœurs et accès possédés sont libérés en finally ; les fichiers synthétiques et répertoires de session temporaires sont supprimés. Les tests temporaires dépendant des ROM et leur projet de compilation sont supprimés après exploitation ; les tests autonomes, captures et journaux sont conservés.

## Performances observées

Mesures de `RunFrame`, sans limitation de vitesse, sur .NET 10.0.15, 16 processeurs logiques. Temps de captures, injection des entrées, reprise d'état et contrôle de l'adaptateur exclus du temps moteur. Les séquences reprennent le protocole historique des références : SG-1000 180 trames sans chauffe ; Master System 600, Game Gear/Mega Drive 1 200 et Mega-CD 1 800 après 60 trames de chauffe. SC-3000 et Mark III utilisent une cartouche compatible du corpus.

| Machine | Scénarios | FPS Debug | FPS Release |
| --- | ---: | ---: | ---: |
| SG-1000 | 6 | 126–189 | 308–4 393 |
| SC-3000 | 1 | 187 | 4 832 |
| Mark III | 1 | 171 | 4 161 |
| Master System | 6 | 83–172 | 1 138–2 350 |
| Game Gear | 6 | 204–246 | 2 858–3 565 |
| Mega Drive | 6 | 27–33 | 487–917 |
| Mega-CD | 3 | 26–32 | 598–864 |

Les valeurs Debug proviennent de la campagne complète ; le contrôle ciblé ultérieur Mega-CD vérifie la correction d'état. Le démarrage à froid et la compilation progressive du JIT pèsent fortement sur les courtes premières séquences SG-1000 : ces plages ne sont pas des moyennes représentatives d'une session longue. Les mesures historiques et présentes ne constituent pas un A/B simultané ; elles ne démontrent pas un gain de vitesse sur GenesisPlusGXCS.

Les JSON contiennent, par ROM, FPS nominaux et effectifs, pourcentage de temps réel, temps moteur et CPU du processus, initialisation, mémoire, allocations, audio, images et empreintes. Les cycles et fréquences CPU effectives sont calculés depuis les cycles d'horloge de trame du processeur principal : ce ne sont pas des compteurs indépendants d'instructions ni un profil des composants. Les allocations mesurées portent sur le thread d'exécution ; celles d'éventuels travailleurs MediaEngine ne sont pas incluses. Les maxima observés par trame en Release sont nuls sur Master System/Game Gear, environ 3,7 Ko sur Mega Drive et 63,2 Ko sur Mega-CD. Aucun profil propre/inclusif n'a été produit pour cette reconstruction.

## Preuves et limites

| Exigence | Éléments vérifiés |
| --- | --- |
| Toutes les machines de GenesisPlusGXCS | Même ensemble de huit identifiants ; huit classes et fabrique propres ; programmes synthétiques et scénarios disponibles |
| Puces réutilisables | 126 nouveaux fichiers Chips sans référence à Emulation ou à un émulateur ; essais de bus et périphériques |
| Machines séparées | Composition, initialisation et trames par classe ; absence des composants 16 bits sur les machines 8 bits contrôlée |
| Intégration Emulation | Catalogue, fabrique, contrats communs et adaptateur exercé dans les 29 scénarios |
| Intégration MediaEngine | Sessions optiques/PCM, formats, médias synthétiques et trois disques réels ; changement de disque et précache |
| Options indépendantes | Définitions et préparation propres, variantes éditables, gains réellement câblés, ressources de toutes les cultures |
| Préservation lors de la reconstruction | 963 empreintes identiques, aucune ressource étrangère modifiée, aucune référence à un autre émulateur dans SegaEmu ; l'audit ultérieur des options a son propre périmètre ci-dessous |

Pico est vérifié avec un programme en mémoire, sans ROM commerciale disponible dans le corpus fourni. SC-3000 utilise une cartouche SG-1000 compatible : cet essai ne démontre pas le fonctionnement complet du clavier ou des logiciels BASIC. Les séquences automatisées et captures ne prouvent pas la compatibilité intégrale des jeux ou de chaque périphérique. Certaines captures correspondent à un écran noir ou à une transition déjà présente dans les références. Les profils CHD testés ne couvrent pas toutes les combinaisons de codecs ; l'essai Ogg synthétique silencieux ne constitue pas une validation exhaustive du décodeur. Les CHD nécessitant une image parente restent hors des formats acceptés. Le 32X n'appartient pas aux machines proposées par GenesisPlusGXCS et n'est pas ajouté.

Le projet global Chips.Tests conserve une erreur préexistante d'import `CPU.Common.Z80` dans les tests UPD780C1 ; les nouveaux tests ont été exécutés dans un projet ciblé temporaire, sans modifier ces tests existants. Une tentative de build s'est heurtée à une compilation concurrente de tests ; la reprise séquentielle Debug puis Release réussit.

- [Résultats Debug complets](../../../../artifacts/SegaEmu/results/Debug/results.json)
- [Contrôle Debug Mega-CD après correction](../../../../artifacts/SegaEmu/results/Debug-diagnostic/results.json)
- [Résultats Release complets](../../../../artifacts/SegaEmu/results/Release/results.json)
- [Audit de préservation, ressources et distributions](../../../../artifacts/SegaEmu/preservation-check.json)
- [Journal des 34 tests autonomes](../../../../artifacts/SegaEmu/integration-final-tests.log)
- Captures sous `artifacts/SegaEmu/results/{Debug,Release}/<machine>/` ; journaux et outils de fabrication sous `artifacts/SegaEmu/`.

Application à tester : `build/Debug/Retro Media Lab/retromedialab.exe` ou `build/Release/Retro Media Lab/retromedialab.exe`.

## Résultats par ROM

| Machine | ROM | FPS Debug | FPS Release |
| --- | --- | ---: | ---: |
| Sg1000 | Champion Tennis (Japan) | 154,1 | 308,1 |
| Sg1000 | 007 James Bond (Japan) (v2.7) (Othello Multivision) | 141,5 | 531,0 |
| Sg1000 | Congo Bongo (Japan) | 134,6 | 4393,0 |
| Sg1000 | Bank Panic (Japan) | 126,5 | 2844,7 |
| Sg1000 | 007 James Bond (Japan) (v2.6) (Othello Multivision) | 138,1 | 3615,2 |
| Sg1000 | Bomberman Special (Taiwan) (DahJee) | 188,6 | 3411,4 |
| MasterSystem | Alex Kidd in Miracle World (USA, Europe) | 167,9 | 1259,7 |
| MasterSystem | Sonic The Hedgehog (USA, Europe) | 167,7 | 1137,6 |
| MasterSystem | Wonder Boy III - The Dragon's Trap (USA, Europe) | 90,2 | 1197,5 |
| MasterSystem | R-Type (World) | 82,5 | 1701,2 |
| MasterSystem | Phantasy Star (USA, Europe) (Rev 3) | 171,8 | 2349,8 |
| MasterSystem | After Burner (World) | 86,9 | 1750,4 |
| GameGear | Sonic The Hedgehog (World) (Rev 1) | 204,3 | 2858,4 |
| GameGear | Shinobi (USA, Europe) | 209,9 | 3008,4 |
| GameGear | Columns (USA, Europe) | 246,4 | 3468,7 |
| GameGear | Aladdin (USA, Europe) | 211,7 | 3248,3 |
| GameGear | Ristar - The Shooting Star (World) | 228,2 | 3564,9 |
| GameGear | GG Aleste (Japan) | 237,0 | 3390,9 |
| MegaDrive | Sonic The Hedgehog (USA, Europe) | 29,6 | 486,5 |
| MegaDrive | Golden Axe (World) | 32,8 | 917,1 |
| MegaDrive | Streets of Rage 2 (USA) | 32,1 | 851,6 |
| MegaDrive | Gunstar Heroes (USA) | 33,1 | 871,4 |
| MegaDrive | Thunder Force III (Japan, USA) | 27,8 | 737,1 |
| MegaDrive | Aladdin (Europe) | 27,0 | 765,5 |
| MegaCd | Batman Returns (Europe) | 28,9 | 597,6 |
| MegaCd | Final Fight CD (Europe) | 25,7 | 619,6 |
| MegaCd | Dragon's Lair (Japan) (En,Ja,Fr,De,It) | 31,7 | 864,3 |
| Sc3000 | Champion Tennis (Japan) | 187,0 | 4832,4 |
| MarkIII | Alex Kidd in Miracle World (USA, Europe) | 170,7 | 4161,4 |

## Comparaison A/B contemporaine — Debug et Release

Le 14 novembre 2026, les deux implémentations sont exécutées successivement dans le même processus et le même assembly de production, sans débogueur. La configuration du build et l'état du JIT sont vérifiés. Chaque ROM passe un parcours complet de chauffe pour chaque cœur, puis trois passages mesurés sur des machines recréées ; l'ordre des cœurs alterne par passage et par ROM. Tous les cœurs et ressources possédés sont libérés en finally.

Les réglages sont identiques : matériel et ROM, BIOS, entrées, 32 bits, stéréo 44 100 Hz, filtre passe-bas par défaut, horloge à 100 %, aucune trame sautée et aucun précache CD. Le chronométrage et les allocations couvrent `RunFrame`, avec rendu et synthèse audio émulés, hors initialisation, injection des entrées, hachages et présentation Windows.

Deux comparaisons utilisent exactement les mêmes 180 trames mesurées après 60 trames de chauffe : Debug et Release court. Leurs sorties par cœur sont également identiques entre configurations. La campagne Release longue conserve 3 600 trames pour SG-1000/SC-3000/Game Gear, 1 800 pour Master System/Mark III/Mega-CD et 1 200 pour Mega Drive. Les scènes mesurées diffèrent donc entre parcours court et long ; leurs écarts ne doivent pas être attribués uniquement au mode de compilation. Les très courtes mesures Release durent parfois quelques dizaines de millisecondes ; les petites variations de quelques pourcents restent à interpréter avec les min/max conservés dans les JSON.

Les pourcentages ci-dessous sont les variations de FPS de SegaEmu par rapport à GenesisPlusGXCS : négatif = perte, positif = gain. Pour chaque ROM, le rapport utilise les médianes des trois passages. L'agrégat par machine est la moyenne géométrique de ces rapports, avec un poids égal par ROM, sans mélanger les FPS de jeux différents.

| Machine | ROM | Debug, 180 trames | Release, 180 trames | Release long |
| --- | ---: | ---: | ---: | ---: |
| SG-1000 | 6 | -7,1 % | +3,1 % | -5,5 % |
| SC-3000 | 1 | -10,6 % | -6,4 % | -5,0 % |
| Mark III | 1 | -9,3 % | -12,0 % | -2,6 % |
| Master System | 6 | -6,5 % | -2,0 % | -4,7 % |
| Game Gear | 6 | -5,0 % | -5,8 % | -0,9 % |
| Mega Drive | 6 | +3,6 % | -5,8 % | -2,7 % |
| Mega-CD | 3 | -6,4 % | -7,3 % | -19,3 % |

La perte la plus nette sur les parcours longs concerne Mega-CD : Batman Returns −25,8 % (854,2 → 633,6 FPS), Final Fight CD −18,3 % (717,3 → 586,2 FPS) et Dragon's Lair −13,2 % (827,1 → 717,9 FPS). Game Gear est proche sur ces parcours longs (−0,9 % en agrégat), tandis que SG-1000 perd 5,5 %, Master System 4,7 % et Mega Drive 2,7 %. Les séquences courtes montrent que le résultat dépend du contenu exécuté ; elles ne caractérisent pas une session entière.

Les allocations sur le thread du cœur Mega-CD augmentent aussi : Batman Returns passe d'environ 2,3 à 63,2 Ko/trame ; Final Fight CD de 1,2 à 54,3 Ko/trame ; Dragon's Lair de 1,1 à 6,7 Ko/trame. Il s'agit d'un surcoût mesuré, sans profil de composants permettant d'en isoler la contribution exacte à la perte de vitesse.

### Images et audio

Les 29 scénarios terminent sans exception dans les trois campagnes. Les images échantillonnées sont identiques entre les deux cœurs ; les nombres de trames audio et fréquences nominales correspondent. Les fichiers ROM, pistes associées et BIOS sont inchangés avant/après chaque campagne. Les 963 fichiers protégés restent identiques après les essais. Aucun cœur de production n'a été modifié pour cette comparaison.

**Final Fight CD présente une divergence PCM réelle sur le parcours long.** Les trois passages de chaque cœur sont déterministes, mais les hachages audio diffèrent entre cœurs. Le diagnostic trame par trame sur 1 800 trames, après les 60 trames de chauffe, reproduit exactement les mêmes différences en Debug et Release : première différence à la trame mesurée 1 070, 1 296 399 échantillons différents sur 3 194 274 (40,585 %), écart absolu maximal 36 068 et écart RMS 2 559,42 unités PCM 16 bits. Le nombre d'échantillons reste identique. Ce n'est pas un simple écart d'arrondi ; sa cause n'est pas établie par ces tests. Les comparaisons courtes de 180 trames s'arrêtent avant ce passage et ne permettent donc pas de l'écarter.

Pico n'a pas de ROM disponible dans le corpus et n'est pas benchmarké. SC-3000 et Mark III ne représentent qu'une cartouche compatible chacun. Les hachages vidéo concernent des images échantillonnées, pas toutes les trames ni une validation matérielle exhaustive.

### Écarts par ROM

| Machine | ROM | Debug, 180 trames | Release, 180 trames | Release long |
| --- | --- | ---: | ---: | ---: |
| SG-1000 | Champion Tennis (Japan) | -7,4 % | +0,1 % | -5,5 % |
| SG-1000 | 007 James Bond (Japan) (v2.7) (Othello Multivision) | -5,9 % | +7,0 % | -5,2 % |
| SG-1000 | Congo Bongo (Japan) | -5,2 % | -4,2 % | -6,0 % |
| SG-1000 | Bank Panic (Japan) | -6,3 % | +13,7 % | -5,6 % |
| SG-1000 | 007 James Bond (Japan) (v2.6) (Othello Multivision) | -8,2 % | +9,2 % | -6,9 % |
| SG-1000 | Bomberman Special (Taiwan) (DahJee) | -9,9 % | -5,5 % | -4,1 % |
| Master System | Alex Kidd in Miracle World (USA, Europe) | -6,6 % | +7,8 % | -7,5 % |
| Master System | Sonic The Hedgehog (USA, Europe) | -8,9 % | -7,5 % | -4,1 % |
| Master System | Wonder Boy III - The Dragon's Trap (USA, Europe) | -4,5 % | +4,4 % | -3,1 % |
| Master System | R-Type (World) | -8,2 % | +2,0 % | -5,3 % |
| Master System | Phantasy Star (USA, Europe) (Rev 3) | -5,0 % | -11,8 % | -2,8 % |
| Master System | After Burner (World) | -5,4 % | -5,3 % | -5,3 % |
| Game Gear | Sonic The Hedgehog (World) (Rev 1) | -6,5 % | -5,8 % | +2,2 % |
| Game Gear | Shinobi (USA, Europe) | -7,0 % | -2,2 % | -0,5 % |
| Game Gear | Columns (USA, Europe) | -6,8 % | -18,1 % | -2,9 % |
| Game Gear | Aladdin (USA, Europe) | -4,0 % | +3,0 % | -1,1 % |
| Game Gear | Ristar - The Shooting Star (World) | -4,1 % | -2,8 % | +0,9 % |
| Game Gear | GG Aleste (Japan) | -1,7 % | -7,8 % | -3,7 % |
| Mega Drive | Sonic The Hedgehog (USA, Europe) | +3,5 % | -4,0 % | -4,9 % |
| Mega Drive | Golden Axe (World) | +1,9 % | -2,8 % | +2,3 % |
| Mega Drive | Streets of Rage 2 (USA) | +4,4 % | -10,9 % | -1,0 % |
| Mega Drive | Gunstar Heroes (USA) | +6,2 % | -7,2 % | -0,3 % |
| Mega Drive | Thunder Force III (Japan, USA) | +5,6 % | -0,4 % | -6,0 % |
| Mega Drive | Aladdin (Europe) | +0,3 % | -9,1 % | -5,8 % |
| Mega-CD | Batman Returns (Europe) | -7,9 % | -9,0 % | -25,8 % |
| Mega-CD | Final Fight CD (Europe) | -3,4 % | -9,6 % | -18,3 % |
| Mega-CD | Dragon's Lair (Japan) (En,Ja,Fr,De,It) | -7,7 % | -3,1 % | -13,2 % |
| SC-3000 | Champion Tennis (Japan) | -10,6 % | -6,4 % | -5,0 % |
| Mark III | Alex Kidd in Miracle World (USA, Europe) | -9,3 % | -12,0 % | -2,6 % |

### Résultats conservés

Les JSON contiennent les six passages chronométrés par ROM, leurs FPS et min/max, allocations, durées CPU, nombre de trames audio et hachages. Chaque `runtime.json` précise la configuration, le SHA-256 du module et le protocole. Le code et les assemblages du banc temporaire sont supprimés après exploitation.

- [Comparaison Debug courte](../../../../artifacts/SegaEmu/ab-debug/results.json) et [protocole Debug](../../../../artifacts/SegaEmu/ab-debug/runtime.json).
- [Comparaison Release courte](../../../../artifacts/SegaEmu/ab-release-short/results.json) et [protocole Release court](../../../../artifacts/SegaEmu/ab-release-short/runtime.json).
- [Comparaison Release longue](../../../../artifacts/SegaEmu/ab-release/results.json) et [protocole Release long](../../../../artifacts/SegaEmu/ab-release/runtime.json).
- [Diagnostic audio Release](../../../../artifacts/SegaEmu/ab-release/audio-diagnostic.json) et [diagnostic audio Debug](../../../../artifacts/SegaEmu/ab-debug/audio-diagnostic.json).
- Journaux `comparison.log`, diagnostics et empreintes `integrity.json` dans chacun des répertoires de résultats.

## Audit des options et de la présentation vidéo — 14 novembre 2026

Cet audit concerne les 24 modules, y compris GenesisPlusGXCS, à la demande de l'utilisateur. Les campagnes de performances ci-dessus précèdent ces corrections ; elles n'ont pas été rejouées dans cet audit.

### Continuité de la partie et redémarrage

La sauvegarde d'un réglage vidéo repassait toutes les options au chemin runtime, même lorsque leurs valeurs étaient inchangées. Les deux cœurs gérés SegaEmu et GenesisPlusGXCS reconstruisaient alors leur système pour certaines de ces valeurs. Le chemin commun ne transmet désormais que les options à chaud réellement modifiées ; chaque cœur ignore aussi une valeur identique à celle déjà appliquée.

Les rapports 4:3 et sans correction, bordures, overscan, affichage étendu Game Gear et effets de présentation s'appliquent sans recréer le cœur. Le changement de renderer remplace seulement la surface graphique et reconnecte sa vue et son handle aux entrées ; le traitement vidéo conserve la machine. Aucun de ces chemins ne relance Retro Media Lab.

Les contraintes de démarrage restantes concernent la configuration du matériel : modèle, BIOS, région/fréquence matérielle, cartouche additionnelle, implantation FM, taille de RAM CD ou changement d'identité de la sauvegarde CD. L'application conserve ces changements pour le prochain démarrage du cœur ; elle ne les lui envoie pas à chaud. Un appel direct à SetOption sur un tel réglage reconstruit seulement le cœur. La politique de précache, elle, est modifiable à chaud et s'applique au prochain montage du disque.

Sept modules remplaçaient l'indicateur de contrainte du cœur par celui de leur politique de backend. Leur projection conserve maintenant les deux exigences. Cela ne demande jamais de redémarrer l'application. La pertinence de chaque option dynamique d'un cœur natif externe n'a pas été vérifiée par son exécution.

### Options et traductions raccordées

- GenesisPlusGXCS possède maintenant ses propres clés de ressources manquantes, avec les traductions existantes copiées statiquement lorsque le texte est commun. Aucun accès runtime aux constantes ou données d'un autre émulateur n'est ajouté.
- Les nouveaux textes et les libellés partiellement traduits sont traités avec Argos dans toutes les langues. Les valeurs invariantes restent en base commune. Les ressources de module restent dans leur module ; les libellés communs de l'application restent dans App.
- Les volumes des quatre canaux PSG, des six canaux Mega Drive et des neuf canaux FM Master System sont raccordés dans les implémentations audio propres à GenesisPlusGXCS. La limite de sprites, le curseur de pistolet, la taille de RAM CD et le précache sont également raccordés.
- Le sélecteur audio avancé affiche réellement les volumes par canal dans les trois ports qui le déclarent, avec le rafraîchissement de champs prévu par le SDK. Les valeurs masquées sont conservées.
- Les valeurs par défaut de l'overclock des quatre ports sont normalisées à `100`, valeur réellement acceptée par leurs listes, au lieu de `100%`.
- Les défauts PicoDrive FM et Kronos langue deviennent respectivement `ym3438` et `English`. Cela conserve le comportement de repli de leurs sources natives tout en employant des choix valides : [PicoDrive](https://raw.githubusercontent.com/libretro/picodrive/master/platform/libretro/libretro.c), [Kronos](https://raw.githubusercontent.com/libretro/yabause/kronos/yabause/src/libretro/libretro.c).

### Vérifications et limites

L'audit statique des 24 modules ne relève plus de clé référencée ni de traduction manquante dans les catalogues examinés, ni de projection supprimant l'indicateur de contrainte. Le contrôle des listes déclarées examine 2 727 options sans défaut invalide ni erreur de chargement. Certains modules ne déclarent pas leurs options sous cette forme : ce décompte ne couvre donc pas leurs champs personnalisés ni toutes les options fournies dynamiquement par une DLL native.

Les 22 contrôles ciblés réussissent en Release : filtrage commun des changements, conservation du cœur et de son état pour les valeurs inchangées, options de présentation, rapports d'image, dimensions Game Gear, localisation par le module dans les 29 langues, visibilité des volumes avancés, configuration à chaud, signal produit par les gains PSG et YM2612, propriété et contenu des flux précachés, et audit des valeurs par défaut. Ces contrôles ne constituent pas une campagne de compatibilité de chaque jeu ou cœur natif.

La construction `scripts/local-building.cmd --building=debug --modules=A` réussit. L'application `build/Debug/Retro Media Lab/retromedialab.exe` et les 24 assemblages d'entrée des modules sont présents. Le compilateur émet encore des avertissements, consignés dans le journal. Les projets de vérification temporaires et leurs binaires ont été supprimés ; les tests autonomes en mémoire sont conservés.

Résultats conservés :

- [Audit des ressources et projections](../../../../artifacts/Emulation/options-audit.json).
- [Audit des valeurs par défaut](../../../../artifacts/Emulation/option-defaults.json).
- [Journal des 22 contrôles](../../../../artifacts/Emulation/options-tests.final.log).
- [Construction Debug complète](../../../../artifacts/Emulation/options-build-debug.log).

## BIOS Sega, cartouches auxiliaires et démarrage sans jeu — 14 novembre 2026

### Identification dans les champs ROM

Le sélecteur utilisait auparavant un inspecteur qui classait chaque fichier comme inconnu et le proposait dans plusieurs champs sans reconnaître son contenu. Le catalogue existant n'était pas raccordé à cet inspecteur. L'inspection calcule maintenant MD5, SHA-1 et SHA-256 pendant une seule lecture annulable, puis utilise les empreintes connues et les rôles déclarés pour affecter les candidats aux champs de chaque émulateur Sega.

Le catalogue est enrichi de 128 empreintes MD5/SHA-1 vérifiées, dont plusieurs désignent le même fichier. Les identités proviennent des métadonnées [Libretro System.dat](https://raw.githubusercontent.com/libretro/libretro-database/master/dat/System.dat) et des sources MAME conservées dans les résultats. Un nom de fichier seul ne prouve jamais une identité officielle. Un BIOS reconnu pour une autre machine ou un autre emplacement est exclu de la liste du champ ; un fichier inconnu reste explicitement inconnu et sélectionnable manuellement selon le fonctionnement existant. Cela permet aussi de conserver les firmwares personnalisés sans les déclarer officiels.

Les BIOS avec jeux intégrés ont une identité distincte du BIOS de démarrage simple. Les versions US/Europe réellement communes sont proposées aux deux champs correspondants ; les autres variantes gardent leurs destinations régionales. Les cartouches Game Genie et Action Replay sont identifiées comme compatibles, sans être déclarées BIOS officiels Sega. Les champs Sonic & Knuckles, UPMEM, Game Genie et Action Replay manquants ont été ajoutés aux trois ports Genesis Plus GX ; SegaEmu les possédait déjà. Leurs libellés techniques invariants restent dans la base commune des ressources.

L'inventaire a examiné 110 fichiers : les candidats trouvés dans `G:\Retro\Cartouches\Sega` et ceux du répertoire de firmware Sega déjà utilisé par l'application. 72 fichiers, doublons compris, sont identifiés ; 62 ont au moins une destination déclarée dans les émulateurs actuels. Le catalogue couvre des empreintes connues, pas tous les dumps historiques, archives recompressées ou personnalisations possibles. Un fichier non reconnu ne devient donc pas incompatible sur la seule base de son nom.

### Fichiers locaux utilisables

| Matériel ou rôle | Fichiers trouvés / utilisation |
| --- | --- |
| Master System | BIOS v1.3 US/Europe, v2.0 Europe, v2.1 Japon et variantes identifiées, dans les champs régionaux correspondants |
| Master System avec jeu intégré | Alex Kidd, Hang-On, Hang-On & Safari Hunt, Missile Defense 3-D et Sonic : sélection comme BIOS SMS, distincte de la cartouche de jeu |
| Game Gear | BIOS Majesco de 1 Kio, dans le champ Game Gear |
| Mega Drive / Genesis | TMSS, ROM système sans jeu intégré ; Sonic & Knuckles est une cartouche lock-on distincte du TMSS |
| Mega-CD / Sega CD | Plusieurs révisions régionales identifiées, dans le champ CD correspondant ; aucune cartouche de jeu intégrée au BIOS n'est annoncée |
| Cartouches auxiliaires Mega Drive | `Sonic & Knuckles (World).md`, `Game Genie (USA, Europe) (Rev A) (Program).md`, `Action Replay (Europe) (Program) (Unl).md` correspondent aux empreintes des champs dédiés |
| UPMEM Sonic & Knuckles | L'emplacement `sk2chip.bin` est disponible ; aucun fichier autonome correspondant à cette empreinte n'a été identifié dans les candidats examinés |
| SG-1000 / Othello Multivision | Le fichier `[BIOS] Othello Multivision (Japan).sg` est identifié, mais ne produit qu'un écran noir dans le modèle SG-1000 actuel ; il n'est pas proposé comme BIOS compatible SG-1000 |

Les [sources MAME SMS](https://raw.githubusercontent.com/mamedev/mame/master/src/mame/sega/sms.cpp) documentent les jeux intégrés et leurs empreintes. Le [matériel Multivision décrit par MAME](https://raw.githubusercontent.com/mamedev/mame/master/src/mame/sega/sg1000.cpp) possède une carte mémoire et des ports d'entrée différents du SG-1000. L'identification de son dump ne constitue pas une validation de son exécution comme cartouche SG-1000. Aucun nouveau modèle Multivision n'est ajouté dans ce travail.

Les fichiers de `G:\Retro` restent à leur emplacement d'origine. Leurs chemins complets et affectations sont consignés dans `selection-results.json` ; ils peuvent être renseignés dans les champs de chemin existants. Les fichiers identiques déjà présents dans le dossier BIOS de l'application sont reconnus lors de l'actualisation du sélecteur. Aucun média source n'est renommé, extrait, importé ou modifié automatiquement.

### Démarrage sans cartouche ou disque

SegaEmu et GenesisPlusGXCS levaient une erreur lorsque leur liste de médias était vide, avant d'initialiser la console. Leurs chargeurs demandaient aussi obligatoirement un fichier de cartouche. Ces deux obstacles sont supprimés : une liste vide représente un port cartouche vide, une taille de ROM nulle et un bus ouvert. Aucun faux fichier de jeu ni message de console dessiné par l'application n'est utilisé.

Sans cartouche, le BIOS configuré est exécuté. Lorsque la région est automatique, le cœur choisit parmi les BIOS présents, dans l'ordre US, Europe, Japon ; une région explicitement configurée reste prioritaire. Le réglage de saut du BIOS continue de s'appliquer aux jeux insérés ; il ne supprime pas la seule ROM système disponible lorsque le port est vide. L'insertion et l'éjection de cartouche recréent le matériel avec le bon média ou sans média, après sauvegarde sous l'ancienne identité. Les sauvegardes sans contenu utilisent une identité système stable.

Résultats observés dans les deux cœurs C# :

- SMS BIOS v1.3 : écran réel demandant l'insertion d'une carte ou cartouche.
- SMS avec BIOS intégré : les cinq jeux cités ci-dessus atteignent leur écran de jeu ou de titre sans cartouche.
- Mega-CD : écran système du BIOS européen sans disque.
- SG-1000, SC-3000, Mark III, SMS, Game Gear, Mega Drive et Pico sans BIOS/média : initialisation et émission de trames sans refus de média absent. Une machine dépourvue de logiciel système peut rester sur un écran vide ; aucun écran d'erreur artificiel n'est ajouté.
- Cycle insertion d'une cartouche SMS, éjection, retour au port vide et réinitialisation : vérifié sur les deux cœurs.

Les cœurs natifs externes gardent leur propre limite d'API. Les sources actuelles de [Genesis Plus GX](https://raw.githubusercontent.com/ekeeke/Genesis-Plus-GX/master/libretro/libretro.c) et [Gearsystem](https://raw.githubusercontent.com/drhelius/Gearsystem/master/platforms/libretro/libretro.cpp) refusent un chargement sans contenu. Leur DLL n'est pas modifiée ici et leur démarrage sans jeu n'est pas annoncé comme corrigé. Le wrapper existant transmet déjà une demande sans jeu aux backends qui déclarent la prendre en charge.

### Vérifications conservées

37 contrôles réussissent en Release, dont les contrôles précédents de présentation vidéo. Le parcours local comprend 17 scénarios de démarrage par cœur : dix avec BIOS et sept sans BIOS, avec images identiques entre SegaEmu et GenesisPlusGXCS pour chaque scénario. Le contrôle Multivision conserve l'écran noir comme limite observée, sans le compter comme un jeu fonctionnel. Les empreintes des 110 fichiers source restent identiques après les essais.

La distribution Debug complète est construite avec `scripts/local-building.cmd --building=debug --modules=A` : application et 24 assemblages d'entrée vérifiés. Les projets de tests temporaires, sessions et binaires sont supprimés après validation ; les tests autonomes en mémoire, captures et journaux restent disponibles. Les avertissements du compilateur sont conservés dans le journal.

- [Identités et destinations réelles](../../../../artifacts/Emulation/sega-firmware/selection-results.json), [inventaire et sources](../../../../artifacts/Emulation/sega-firmware/inventory.json), [intégrité des médias](../../../../artifacts/Emulation/sega-firmware/integrity.json).
- [37 contrôles](../../../../artifacts/Emulation/sega-firmware/final-tests.log) et [construction Debug complète](../../../../artifacts/Emulation/sega-firmware/build-debug.log).
- [SMS sans cartouche](../../../../artifacts/Emulation/sega-firmware/segaemu-sms-v13.png), [Alex Kidd intégré](../../../../artifacts/Emulation/sega-firmware/segaemu-sms-alex-kidd.png), [Sonic intégré](../../../../artifacts/Emulation/sega-firmware/segaemu-sms-sonic.png), [Mega-CD sans disque](../../../../artifacts/Emulation/sega-firmware/segaemu-cd-europe.png).
