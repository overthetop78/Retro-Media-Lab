# Extensions mémoire du Model A

## Périmètre

Le Model A existant reste propriétaire de son implémentation. Les éléments réutilisables seront déplacés dans des Common spécifiques au démarrage du Model B, sans fusionner les deux machines.

Le premier bloc Shadow RAM propose Aucune, Aries B20 (20 Kio) et Watford Electronics 32K RAM (32 Kio). Ce sont des matériels distincts ; on ne les cumule pas dans le même bloc. Les deux demandent une machine équipée de 32 Kio principaux et MOS 1.20. La carte choisie occupe une banque ROM réservée à sa ROM de gestion utilisateur. BASIC, DFS et NFS conservent leurs banques ; les ROM de gestion identifiées seront fournies comme assets externes, avec possibilité de choisir une autre version compatible.

## Décodage

Le framebuffer reste dans la RAM BBC. La carte fournit une RAM parallèle au CPU, ce qui libère pour le programme la zone normalement utilisée par l'écran. La ROM de gestion commute les accès ; il ne faut pas remplacer arbitrairement le framebuffer par la RAM de la carte.

Aries B20 : écrire à FFFE sélectionne la RAM BBC ; écrire à FFFF sélectionne la RAM Aries dans 3000–7FFF. La valeur écrite est indifférente. La RAM basse reste celle du BBC. Une mise sous tension sélectionne la RAM BBC ; BREAK conserve la sélection et les données.

Watford 32K : écrire à FFFC sélectionne les 32 Kio de la carte ; FFFE sélectionne la RAM BBC ; FFFF sélectionne la carte dans 3000–7FFF seulement. FFFD correspond à une sélection sans RAM répondante. La vidéo reste raccordée à la RAM BBC dans tous les cas. Les commandes RAMON, RAMOFF et les buffers sont exécutés par la ROM de gestion ; elles ne sont pas remplacées par des interceptions de commandes BASIC.

Les ROM Aries B20 et Watford existent en 8 Kio. Leur chargement utilise la normalisation de banque existante. Le choix Aucune conserve le chemin précédemment choisi mais retire la carte et sa ROM du bus.

## ROM et conflits

Une extension ROM ajoute des supports et du décodage. Les ROM existantes peuvent être déplacées sur son support sans être remplacées. Une capacité totale de 256 Kio correspond à seize banques de 16 Kio ; la disposition des supports dépend de la carte. Cette explication n'autorise pas l'ajout d'une nouvelle carte ROM non demandée. La carte Shadow RAM réserve ici la banque 12 ; BASIC, DFS et NFS restent respectivement en 15, 14 et 13. Un conflit de banque est refusé plutôt que d'écraser silencieusement une ROM.

## Sources

- [Notice Watford Electronics 32K RAM](https://chrisacorns.computinghistory.org.uk/docs/Watford/WE_32KRAMcard.pdf), installation p.4, contrôle direct p.21 : MOS 1.20, ROM de gestion, commandes FFFC/FFFE/FFFF et conservation sur BREAK.
- [Manuel de maintenance Acorn](https://acorn.huininga.nl/pub/docs/manuals/Acorn/BBC%20B/BBC%20Microcomputer%20Service%20Manual.pdf), conversion A/B et sélection ROM.
- [Publicité constructeur Aries B20, Acorn User avril 1984](https://device.report/m/395c44a675c920300e28038e3130d552e47f1b9b5ab732916978585c4a296cc7) : prérequis Model B/MOS 1.2, support CPU et support ROM.
- Références de décodage et d'identification croisées avec [Aries dans MAME](https://github.com/mamedev/mame/blob/master/src/devices/bus/bbc/internal/aries.cpp) et [Watford dans MAME](https://github.com/mamedev/mame/blob/master/src/devices/bus/bbc/internal/we32kram.cpp), sans reprise de leur code.

## Inventaire complémentaire du 27 octobre 2026

Le choix représente le matériel installé. Une ROM de gestion est son logiciel, pas son identité matérielle : elle ne justifie ni un changement silencieux de carte ni un choix arbitraire de capacité. Certaines cartes sideways fonctionnent sans ROM de gestion dédiée ; ne pas rendre ce champ obligatoire pour tous les modèles.

| Matériel | Mémoire et banques | Commandes et points à traiter |
| --- | --- | --- |
| Aries B12 | Extension ROM, RAM sideways 16 Kio sur la banque 0 | Supports ROM ajoutés ; les quatre supports d'origine sont repris sur la carte. Les écritures dans la fenêtre sideways atteignent aussi la RAM 0. |
| Aries B20 | 20 Kio de mémoire parallèle | FFFE/FFFF déjà implémentés. La ROM 2.4 est identifiée par son empreinte. B12 est une carte ROM distincte ; ne pas présenter un cumul de mémoire fictif. |
| Aries B32 | 32 Kio partagés entre mémoire parallèle et deux fenêtres sideways | FE30 sélection ROM, FE32 sélection écriture RAM, FE3E/FE3F mémoire principale/parallèle ; banques 13 et 14 sur la même RAM physique. La ROM porte 1.00 dans la bibliothèque ; MAME nomme ce même fichier 2.2, empreinte identique. Retenir l'identité interne vérifiée, pas le nom d'archive. |
| Watford 32K | 32 Kio parallèles, dont 20 Kio utilisables comme Shadow et 12 Kio de buffers | FFFC–FFFF déjà implémentés ; ROM 2.40 identifiée. |
| Watford ROM/RAM | RAM sideways banques 0–7, supports ROM 12/13/15 et RAM sauvegardée 14 | Choix de capacité selon les composants réellement installés à vérifier dans la notice ; sélection lecture/écriture distincte. Les supports d'origine sont réaffectés aux banques 8–11. Ne pas confondre la capacité de la carte et celle du montage de référence MAME. |
| ATPL Sidewise | 12 positions logiques supplémentaires, dont RAM sauvegardée 16 Kio en banque 15 | La protection matérielle bloque les écritures RAM ; l'écriture de la fenêtre ROM alimente aussi la RAM 15. Les ROM occupantes doivent être réaffectées selon les supports réels. |
| ATPL Sidewise+ | Extension ROM à dix supports | Autre carte que Sidewise ; aucun gain RAM à inventer. Les supports de carte mère 0/1 et 12/13 restent particuliers. |
| Watford 12 ROM / 13 ROM | Extensions ROM avec RAM sauvegardée 16 Kio en banque 14 / 15 | Deux répartitions de supports différentes, à représenter sans superposer BASIC/DFS/NFS sur la RAM. |
| Ramamp RAM/ROM | 32 Kio sideways sur deux banques 16 Kio, 10 et 13 | Six supports ROM complémentaires ; protection en écriture des banques RAM. |
| GCC Romex 13 | Treize supports ROM sur carte | Le support 2 de carte mère est repris par la carte ; les supports 0/1/3 ne deviennent pas libres pour une seconde carte. |
| Solidisk Twomeg 128K | 128 Kio partagés : banques RAM 4–7 et 12–15, ROM 8–11 | FE30 ROMSEL ; FE34 contrôle Shadow à partir de 2000, avec deux paires de banques possibles. Même RAM physique dans les fenêtres principale et sideways. Manager est normalement chargé depuis disquette ; sa présence ne doit pas devenir un faux prérequis ROM physique. |
| Solidisk Fourmeg | Famille associant mémoire et fonctionnement CPU accéléré | Plusieurs variantes : ne pas confondre le modèle 32K présent dans MAME et les cartes 128/256 Kio de la notice 2/4 Meg. Documenter chaque décodage et le changement d'horloge avant de proposer ces capacités comme opérationnelles. |
| Computech Integra-B | Carte intégrée Shadow/sideways, mémoire privée et horloge | FE30/FE34, masque Shadow dépendant de deux registres, accès mémoire privée dans la fenêtre ROM ; capacité et persistance à vérifier dans la notice. Ne pas l'émuler comme une simple allocation Shadow. ROM IBOS 1.20 identifiée. |

Cet inventaire ne déclare pas ces matériels déjà émulés. La sélection des capacités, le partage physique des octets, le décodage des écritures, les banques occupées et la persistance font partie de chaque implémentation, ainsi que leurs conflits avec les autres cartes.

### Références complémentaires

- [Manuel Solidisk 2/4 Meg](https://chrisacorns.computinghistory.org.uk/docs/Solidisk/Solidisk_2M4MRAMROMExpansions.pdf).
- [Manuel Watford RAM/ROM](https://chrisacorns.computinghistory.org.uk/docs/Watford/WE_RAMROMBoard.pdf).
- [Descriptions matérielles MAME BBC internal](https://github.com/mamedev/mame/tree/master/src/devices/bus/bbc/internal) : Aries, ATPL, Watford ROM/RAM, Watford ROM, Ramamp, Romex, Solidisk Twomeg/Fourmeg et Integra-B ; références de registres à croiser avec les notices, sans reprise de code.
- [Bibliothèque BBC ROM](https://tobylobster.github.io/rom_library/) et son manifeste public : identités, versions et empreintes des ROM, dont le désaccord de nommage Aries B32.

## Modules BooBip : supports physiques et décodage

La documentation constructeur décrit les montages A/B sans soudure suivants :

- RAM/ROM 32 Kio : de un à quatre modules, donc 32/64/96/128 Kio de RAM sideways et autant de capacité ROM. Pour un support physique d'indice s (0–3), les banques RAM sont s et s+4 ; les banques ROM s+8 et s+12. Les commandes FE30 de forme 2x protègent la banque RAM x, et 3x enlèvent sa protection, tout en sélectionnant x. Chaque banque a son verrou indépendant.
- Adaptateur EPROM 64 Kio : de un à quatre modules, jusqu'à seize banques ROM (256 Kio au total). Un support reçoit une puce de 32 ou 64 Kio, qui contient plusieurs images distinctes ; ce n'est pas une ROM 16 Kio normalisée arbitrairement.
- Les supports physiques sont IC52 (banque native 12), IC58 (13), IC100 (14), IC101 (15). Une puce ajoutée dans un support en définit le contenu : BASIC, DFS, NFS et le gestionnaire mémoire ne peuvent pas occuper une seconde puce fictive au même emplacement. Leurs chemins séparés sont conservés et rendus non modifiables lorsque le support est défini par une puce sélectionnée.

La mémoire Shadow des cartes CPU et la mémoire sideways sont différentes ; leur total physique s'additionne lorsqu'elles sont réellement installées et compatibles. Les nouvelles RAM ne doivent jamais être ajoutées au tableau de RAM principale ni effacées à chaque BREAK.

Références constructeur : [RAM/ROM](https://www.boobip.com/hardware/32kb-ram-32kb-rom), [décodage et protection](https://www.boobip.com/hardware/32kb-ram-32kb-rom/32kb-ramrom-technical), [adaptateur EPROM](https://www.boobip.com/hardware/64kb-eprom).

La [publicité constructeur SWR256](https://chrisacorns.computinghistory.org.uk/docs/Solidisk/Solidisk_OfficeSystemSWR256Ad.pdf) décrit une autre carte Solidisk, avec 256 Kio physiques et des possibilités d'extension annoncées. Ne pas attribuer cette capacité aux Fourmeg-32K, ni proposer une extension annoncée sans son décodage documenté.

## Aries B12 et distinction avec B32

Le B12 possède douze supports ROM 1–12 et deux emplacements SRAM de 8 Kio pour la banque 0. Le montage peut donc ne contenir aucune SRAM, une moitié de 8 Kio ou les 16 Kio complets. Les quatre ROM préexistantes sont reprises parmi les supports de la carte ; le montage conserve leurs fichiers choisis et les affecte aux banques 9–12. Les écritures de la fenêtre sideways atteignent la RAM 0 présente indépendamment de la banque sélectionnée pour la lecture. Une moitié non équipée reste sans réponse, elle n'est pas inventée ni répétée comme une puce ROM.

B12 et les modules BooBip sont des montages alternatifs sur les mêmes supports. Les choix ne doivent pas les superposer implicitement. La carte Shadow B20 peut utiliser le B12 ; sa ROM choisie occupe une des positions conservées, pas une seconde ROM fictive sur la banque BASIC.

Le [manuel B32](https://chrisacorns.computinghistory.org.uk/docs/Aries/Aries_B32Manual.pdf) distingue clairement B32 seul et B32+B12. Les sockets natifs du B32 seul sont renumérotés 12/9/10/11 ; la ROM de la carte est 15. La ROM de gestion utilise aussi des commandes indexées dans FE30–FE3F. Le décodage B32 doit être complet avant de proposer ses configurations 16/20 Kio Shadow ; ne pas recopier une simulation ne traitant que FE3E/FE3F et déclarer les autres modes réalisés.

Références : [B12 et ses emplacements SRAM](https://chrisacorns.computinghistory.org.uk/8bit_Upgrades/Aries_B12.html), [notice B32, organisation des supports](https://chrisacorns.computinghistory.org.uk/docs/Aries/Aries_B32Manual.pdf), et description du bus Aries dans MAME déjà citée.

## Validation des montages présents — 7 novembre 2026

Sans fenêtre RML, sans modification des configurations utilisateur : MOS `os12.rom`, BASIC `basic2.rom` et générateur `saa5050` externes. Onze cartes Model A distinctes, `expandedIo:false`, aucun lecteur/réseau/HDD. Pour chaque montage : 120 images de démarrage, saisie par le clavier émulé de `PRINT"<montage> PASS":PRINT 12+34`, contrôle du résultat 46 dans la mémoire télétexte, 10 images de stabilisation puis 80 images chronométrées. Chaque image comprend `RunFrame()` et `DrainAudio()`. Aucun test ni build concurrent, compilation Debug ordinaire. Les captures sont les trames effectivement rendues par la carte, pas une fenêtre RML.

| Montage | ms/image | FPS calculés libres | MHz émulés | % des 2 MHz | Capture du 7 novembre |
| --- | ---: | ---: | ---: | ---: | --- |
| RAM principale 16 Kio | 17,2822 | 57,86 | 2,3108 | 115,54 | `20261107-052955-memory-base16.bmp` |
| RAM principale 32 Kio | 17,1087 | 58,45 | 2,3343 | 116,71 | `20261107-053001-memory-base32.bmp` |
| Aries B20 + principale 32 Kio | 16,9729 | 58,92 | 2,3529 | 117,65 | `20261107-053007-memory-b20.bmp` |
| Watford 32K + principale 32 Kio | 17,1134 | 58,43 | 2,3336 | 116,68 | `20261107-053013-memory-watford32.bmp` |
| BooBip RAM/ROM ×1, RAM sideways 32 Kio | 16,7902 | 59,56 | 2,3785 | 118,93 | `20261107-053019-memory-boobip1.bmp` |
| BooBip RAM/ROM ×2, RAM sideways 64 Kio | 17,3656 | 57,59 | 2,2997 | 114,99 | `20261107-053025-memory-boobip2.bmp` |
| BooBip RAM/ROM ×3, RAM sideways 96 Kio | 18,0068 | 55,53 | 2,2178 | 110,89 | `20261107-053031-memory-boobip3.bmp` |
| BooBip RAM/ROM ×4, RAM sideways 128 Kio | 17,4232 | 57,39 | 2,2921 | 114,61 | `20261107-053037-memory-boobip4.bmp` |
| Aries B12 sans SRAM | 17,7604 | 56,31 | 2,2486 | 112,43 | `20261107-053042-memory-b12-0.bmp` |
| Aries B12 SRAM 8 Kio | 16,7695 | 59,63 | 2,3815 | 119,07 | `20261107-053048-memory-b12-8.bmp` |
| Aries B12 SRAM 16 Kio | 17,2300 | 58,04 | 2,3178 | 115,89 | `20261107-053054-memory-b12-16.bmp` |

Chaque fenêtre chronométrée représente 3 194 878 à 3 194 883 cycles CPU ; le très faible écart vient de la fin d'instruction. Cadence PAL matérielle inchangée : environ 50,1 Hz. Le dépassement des 2 MHz est la marge de calcul du banc non limité, pas une reprogrammation du CPU. Un seul passage par montage : ces chiffres ne constituent pas une comparaison de gain entre les cartes.

Captures conservées sous `artifacts/TestsMachines/Acorn/BBC Micro Model A/` et `artifacts/TestsChips/MemoryExtensions/BBC Micro Model A/`, hors Git. Les rendus B20, BooBip ×4 et B12 16 Kio ont également été inspectés visuellement.

Portée : démarrage et fonctionnement de BASIC avec les montages installés, notamment la relocalisation de BASIC sur B12. La commande ne sollicite pas les banques supplémentaires : leurs protections, alias et sélections sont vérifiés séparément par les tests de bus existants. Aucun gestionnaire ROM B20/Watford n'est chargé dans ce banc ; son absence n'est pas présentée comme la validation de ses commandes. Aucun nouveau matériel, oscillateur ou service n'est ajouté. Les budgets de lecture WD/HDD et l'écoute Windows restent distincts et ouverts.

Tests complémentaires après les onze mesures : filtre `AcornEmuMemoryExtensionTests|AcornEmuSidewaysExpansionTests` du projet local, Debug, **45 réussites, zéro échec/ignoré, 766 ms**. Les cas couvrent aussi le programme CPU de commutation B20/Watford, les commandes FE30 et leur protection, les quatre nombres de modules BooBip, les moitiés absentes de B12, les conflits et les descriptions/capacités proposées. Aucun changement des sources de production ni des tests n'a été nécessaire. Les avertissements de compilation d'autres modules ne sont pas des échecs du groupe.

## Aries B20 et logiciels accédant à l’écran — source retrouvée le 7 novembre

La revue originale d’Ian Rowlings, « A 20K RAM boost for the 'B' with a Unique Paging System », Acorn User avril 1984, confirme pour le B20 lui-même les commandes `*XON` et `*XOFF`. Un hard BREAK réactive l’extension ; un soft BREAK conserve son état. La revue explique que la ROM intercepte les accès au-dessus de &3000 et que certains logiciels/ROM ne fonctionnent pas avec le B20 actif ; elle préconise de le désactiver avant leur chargement. Elle ne cite pas AMX Art : ne pas présenter ce texte comme une certification d’incompatibilité AMX.

La sélection de zone de données est documentée par OSBYTE 111 (`*FX111,0` / `*FX111,1`) ; les routines d’interruption doivent rester sous &3000. Ces commandes appartiennent à la ROM de gestion, sans ajout de fonction fictive dans la carte émulée. Les chaînes XON et XOFF sont également présentes dans le fichier externe Aries B20 v2.4 utilisé lors de l’essai ; leur seule présence n’est pas une preuve de leur exécution correcte.

Source : [revue originale numérisée](https://chrisacorns.computinghistory.org.uk/docs/Mags/AU/AU_Apr84_AriesB20.pdf), page unique. PDF et transcription conservés dans `artifacts/.tmp-archives/machines/Acorn/BBC Micro/Memory/Aries/AU_Apr84_AriesB20.pdf` et `.txt`. Il s’agit d’une observation contemporaine, à distinguer du manuel constructeur B32 et de ses commandes supplémentaires. Le prochain essai utile est AMX avec B20 physiquement monté mais désactivé par sa ROM ; aucun résultat de cet essai n’est encore acquis.
