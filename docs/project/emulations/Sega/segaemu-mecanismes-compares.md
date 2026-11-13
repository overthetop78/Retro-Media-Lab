# Mécanismes des moteurs comparés — SG-1000, SMS et GG

Relevé du 13 novembre 2026, demandé pour reprendre **l'ensemble** de l'exécution de SegaEmu : CPU, vidéo, mémoires, logique, horloges, bus, son, entrées, états, reset et variantes. Consigner les mécanismes rencontrés même lorsqu'ils paraissent inutiles, moins précis ou incompatibles avec RML. Leur utilisation éventuelle est une décision distincte de ce relevé.

Ce document complète [l'étude et les mesures d'architecture](segaemu-performance-architecture.md). Il conserve les informations de fonctionnement logiciel; ce n'est pas une référence de preuve du silicium. Aucun de ces moteurs n'a été exécuté ici pour mesurer sa vitesse. Aucune conformité intégrale d'un moteur, d'un opcode ou d'un périphérique n'est déduite de sa réputation ou d'une inspection partielle. « Acquis » signifie fichier téléchargé, pas fichier entièrement analysé.

## Archives et reprise sans nouvelle recherche Web

Racine : `artifacts/.tmp-archives/machines/Sega/SG-1000/Comparison/`. Chaque sous-dossier ci-dessous contient `source-manifest.json` (dépôt, chemins, empreintes Git des fichiers et arbre), `source-sha256.json` et les sources sous `Source/` avec leurs chemins d'origine. Les SHA ci-dessous identifient des **arbres Git**, pas des commits. Les blobs des fichiers acquis ont été vérifiés contre les empreintes de leur manifeste; une URL HEAD seule ne serait pas une identité stable.

Les archives précédentes restent présentes avec leurs origines. Consulter d'abord ce relevé, puis le fichier local correspondant pour une information manquante. Ne retélécharger qu'une source manquante ou une révision expressément nécessaire. Les archives ne sont pas committées; les informations techniques persistantes sont ici.

| Dépôt | Archive relative | Fichiers acquis | Arbre source |
|---|---|---:|---|
| [xdanieldzd/Essgee](https://github.com/xdanieldzd/Essgee) | `Essgee/Source` | 14 | `16e8ffdf1b44f00fbf21d2d97130c260e887dff2` |
| [drhelius/Gearsystem](https://github.com/drhelius/Gearsystem) | `GearWhole/Source` | 39 | `5bcc4289b365d5c5401c937f4c909a158264def8` |
| [ekeeke/Genesis-Plus-GX](https://github.com/ekeeke/Genesis-Plus-GX) | `GenesisWhole/Source` | 49 | `2499cd3e3d1d8b7d0130d8b2825adb4cc608b2c0` |
| [jsgroth/jgenesis](https://github.com/jsgroth/jgenesis) | `jgenesisWhole/Source` | 52 | `386db855ec3aaa307df3321bb85df8b5cc20ac95` |
| [ares-emulator/ares](https://github.com/ares-emulator/ares) | `aresWhole/Source` | 92 | `0b928930af4cc59ed99e942996f0d3c815379388` |
| [ocornut/meka](https://github.com/ocornut/meka) | `MEKAWhole/Source` | 87 | `4c00b68ed08f2745d5ad3eeb05f5e1f258b198be` |
| [TomHarte/CLK](https://github.com/TomHarte/CLK) | `CLK/Source` | 35 | `5eae9b61015221b53135de6128bf4e2bbdb78dad` |
| [leniad/dsp-emulator](https://github.com/leniad/dsp-emulator) | `DSP/Source` | 8 | `36180b612c4c91ac03a3f7ce799037fd6099a2ee` |
| [finalburnneo/FBNeo](https://github.com/finalburnneo/FBNeo) | `FBNeo/Source` | 15 | `7279e9001ec3c9e7b07fd2f43316a77af99b297a` |
| [mamedev/mame](https://github.com/mamedev/mame) | `MAMEWhole/Source` | 9 | `9cf98d883e72e2ab8d4d73a7104989b4b306dda7` |
| [SourMesen/Mesen2](https://github.com/SourMesen/Mesen2) | `MesenWhole/Source` | 52 | `b9fa69ddc6d0a331fb103fdb5eef6904305703c2` |
| [libretro/blastem](https://github.com/libretro/blastem) | `BlastEmWhole/Source` | 19 | `ec73825ece2a134beb7825a5fd6fd21a398ff12e` |

Les 471 fichiers acquis couvrent les sous-systèmes ciblés et les dépendances retrouvées; ce ne sont pas douze dépôts entiers. Les interfaces graphiques hôtes et toutes les autres machines de ces dépôts ne sont pas déclarées examinées. Les révisions, options et exceptions restent séparées : un chemin SMS ne prouve pas le montage SG-1000.

## Grille de couverture à conserver pour chaque moteur

Chaque relevé doit retrouver : boucle principale; unité temporelle et reste de budget; reprise et suspension; CPU/décodeur/préfixes/flags; lecture et écriture mémoire; E/S et bus ouvert; interruptions et attentes; vidéo et accès VRAM; fond/sprites/palettes/caches; PSG et échantillonnage; manettes et pause; reset/démarrage; cartouches/BIOS/variantes; capture et restauration; sorties hôtes; erreurs, options, débogage et limites. Une absence d'examen est indiquée, jamais remplacée par « idem » ou par une déduction sur le matériel.

## Gearsystem — C++

Sources principales : `GearWhole/Source/src/GearsystemCore.cpp`, `Processor.cpp`, `Video.cpp`, `Memory.cpp`, `SG1000MemoryRule.cpp`, `SmsIOPorts.cpp`, `Audio.cpp`, `Input.cpp`, `Cartridge.cpp`, et les implémentations sous `src/audio/`. [Dépôt et sources](https://github.com/drhelius/Gearsystem/tree/master/src).

| Mécanisme rencontré | Fonctionnement et points de reprise |
|---|---|
| Pilotage | `RunToVBlank` répète `RunCycle` jusqu'au retour de VBlank ou à un arrêt de débogage; `EndFrame` termine la sortie audio. Les chemins avec/sans débogueur sont distincts. |
| CPU | `RunFor` répète l'exécution d'opcodes et cumule leurs T-states. Les préfixes et l'arithmétique sont dans les fichiers Processor/opcodes acquis. Les cycles de HALT, interruptions et options de pas modifient la progression; ne pas confondre instruction complète et contrat de bus par broches. |
| Répartition du temps | La quantité de temps exécutée par le CPU est transmise à la vidéo, à l'audio et aux entrées dans la boucle de machine. Le compteur maître sert également aux traces et aux périphériques. |
| Mémoire | `SG1000MemoryRule::PerformRead/PerformWrite` sélectionne les régions par adresse; petites ROM répétées selon la taille, espace d'extension conditionné par ROM/RAM présente, RAM système répétée par masque de1Kio. `GetPage/GetBank` expose les banques au reste du moteur. `Memory` détient le stockage fonctionnel. |
| Cartouche et variantes | Les propriétés de cartouche déterminent présence de RAM et règles de mapping. Bootrom et autres familles de mappers ont leurs objets; le chemin SG n'est pas leur unique cas. Les options SG-1000/SG-1000II interviennent aussi dans la palette et le reset du PSG. |
| E/S | `SmsIOPorts` répartit les lectures/écritures entre vidéo, audio et entrées. L'accès mémoire direct ne restitue pas, à lui seul, CE/OE/RD/WR et leurs propagations RML. |
| Vidéo | `Video::Tick` accumule le temps et traite des seuils de ligne; `ScanLine` sélectionne les rendus TMS/SMS. La VRAM/CRAM appartiennent à `Video`; palettes et formats de sortie sont préparés. Sprites, collision, dépassement, bordures et modes sont traités par les routines vidéo acquises. Les options de limite de sprites/couches et le chemin de débogage doivent rester identifiés comme options. |
| Audio | `Audio` cumule les cycles; les écritures alimentent l'APU et `EndFrame` clôt les buffers Blip, extrait les échantillons, puis combine éventuellement PSG/FM. Des canaux de débogage et enregistrement VGM existent. Le reset choisit un comportement APU suivant la variante SG. |
| Entrées | `Input` conserve les états de touches et des périphériques; le pilote les avance avec le temps reçu. Les chemins optiques et autres accessoires restent distincts des deux manettes simples. Les méthodes SaveState/LoadState conservent aussi des états d'entrée. |
| États et reprise | `GearsystemCore::SaveState` sérialise Memory, Processor, Audio, Video, Input, règle courante, règle bootrom et ports; l'en-tête est versionné et la restauration possède un chemin historique V1. Le buffer Blip et l'APU ont leur propre sérialisation. |
| Ressources | Constructeurs/Init créent les composants et buffers; destructeurs les libèrent. Les flux peuvent être fichiers ou mémoire. Aucune vitesse ni équivalence de toutes les phases électriques n'est déduite de cette organisation. |

## Genesis Plus GX — C

Sources : `GenesisWhole/Source/core/system.c`, `state.c`, `z80/`, `sound/`, fichiers VDP et `input_hw/` acquis. [Source du pilotage](https://github.com/ekeeke/Genesis-Plus-GX/blob/master/core/system.c).

| Mécanisme rencontré | Fonctionnement et points de reprise |
|---|---|
| Pilotage SMS | `system_frame_sms` sépare blanking, zone active, bordures et fin d'image. `z80_run` reçoit des cibles temporelles; `mcycles_vdp` conserve la position du cadre. Ce n'est pas une avance de toute l'image sans interaction. |
| Interruptions | Des frontières de ligne/HINT/VINT sont explicites; une avance CPU supplémentaire existe pour certaines coïncidences avec une frontière d'instruction avant assertion de l'IRQ. Ne pas transposer ces constantes ou compensations au NEC sans preuve matérielle. |
| Rendu | `render_line` est demandé pour une ligne active non encore produite. Les composants de rendu possèdent tables, caches, palettes et fonctions de modes; la mémoire vidéo y est un stockage fonctionnel, pas huit objets MB8118. Le saut de présentation reste une option distincte du temps CPU. |
| CPU et bus | Le cœur Z80 exécute vers une échéance; les accès sont dispatchés par les fonctions et mappings du système. Les fichiers de CPU acquis permettent de retrouver instructions/préfixes/interruptions, sans prouver les détails électriques des clones. |
| Entrées | `input_refresh` intervient aux frontières de traitement; `input_end_frame` clôt le temps d'entrée du cadre. Les périphériques de `input_hw` sont séparés; leur protocole ne se réduit pas au seul état de boutons. |
| Son | La couche sound et les PSG acquis séparent progression des canaux, écriture des registres et production des sorties. Les modules audio MD et les options du projet ne sont pas le PSG matériel SG par définition. L'étude détaillée des buffers et de leur sérialisation reste à compléter localement. |
| Variantes/reset/états | `system_init/reset` et `state.c` sont acquis; les fonctions SMS/MD et les variantes ne doivent pas être mélangées. Le contenu exact de l'état d'un cycle interrompu, des filtres et du reste temporel audio reste à relever dans ces fichiers avant d'en reprendre un mécanisme. |

## jgenesis — Rust

Sources : `jgenesisWhole/Source/backend/smsgg-core/src/{api,bus,memory,input,audio,vdp}.rs`, `vdp/tms9918.rs`, mappers, cœur Z80 et bibliothèques communes/PSG/configuration acquises. [API](https://github.com/jsgroth/jgenesis/blob/master/backend/smsgg-core/src/api.rs), [bus](https://github.com/jsgroth/jgenesis/blob/master/backend/smsgg-core/src/bus.rs).

| Mécanisme rencontré | Fonctionnement et points de reprise |
|---|---|
| Pilotage | `tick` reçoit les entrées, exécute une instruction Z80 par `execute_instruction`, convertit les cycles reçus, puis rattrape PSG et VDP avec leurs compteurs. Les restes sont conservés séparément. |
| Horloges | Dans cette représentation commune SMS/GG/SG : `VDP_DIVIDER=10`, `PSG_DIVIDER=15`; le diviseur CPU vient de la configuration. Ces nombres appartiennent à son domaine maître et ne sont pas nos demi-fronts de quartz. L'overclock configuré est explicitement distinct de la vitesse maximale hôte. |
| Bus | Un `Bus` emprunte mutuellement mémoire, VDP, PSG, entrée et FM optionnelle. `read_memory/write_memory` délèguent directement au propriétaire mémoire; les E/S sont décodées par bits d'adresse. Il n'y a pas de journal de broches pour chaque destinataire dans ce trajet. |
| Ports et variantes | Pour SG, les lectures H/V normalement présentes en SMS renvoient une valeur de lecture invalide. GG possède des registres spécifiques et un TODO de port série. Le contrôle mémoire comporte aussi des bits encore marqués TODO; ne pas les décrire comme réalisés. |
| Interruptions/arbitrage | `int` retourne la ligne VDP; `nmi` dépend de pause pour SMS/SG. Les entrées `busreq` et `reset` de ce Bus retournent false; cela n'est pas un modèle complet d'arbitrage/reset physique du Z80. |
| Vidéo | `vdp.tick` est répété jusqu'au rattrapage; son effet FrameComplete provoque la présentation. Les modes TMS sont séparés dans `tms9918.rs`. Le traitement `process_events` déjà relevé dans l'étude d'architecture contient des compensations de l'entrelacement par instruction; ne pas les oublier dans une comparaison de chronologie. |
| Audio | Le PSG et l'YM optionnelle avancent pendant le rattrapage; un effet Clocked produit les valeurs gauche/droite. `audio_resampler` collecte puis envoie les échantillons. Les autorisations PSG/FM sont conservées dans la mémoire/contrôle du système. |
| Entrées et persistance | L'entrée hôte est reçue au début du tick. À FrameComplete, reset logiciel est décrémenté et une SRAM à batterie modifiée est sauvegardée par SaveWriter; cela ne signifie pas que chaque cartouche SG possède une batterie. |
| Reset et états | Soft reset SMS maintient le bit reset pendant cinq images; hard reset recrée les composants et remet les compteurs temporels à zéro. `to_save_state` utilise `partial_clone`; au chargement, la ROM est reprise de l'instance actuelle avant remplacement de l'état. Les dérivations de sérialisation/clone et les aides communes sont archivées. |
| Erreurs | Les erreurs de rendu, audio et écriture de sauvegarde sont propagées par le résultat de tick. Aucune cadence électrique ne doit dépendre du succès ou du délai de la sortie hôte dans RML. |

## Essgee — C#

Sources : `Essgee/Source/Essgee/Emulation/Machines/SG1000.cs`, `CPU/Z80A*.cs`, `Video/TMS99xxA.cs`, `Audio/SN76489.cs`, `Cartridges/Sega/SegaSGCartridge.cs`, et SaveStateHandler acquis. [Machine](https://github.com/xdanieldzd/Essgee/blob/master/Essgee/Emulation/Machines/SG1000.cs).

| Mécanisme rencontré | Fonctionnement et points de reprise |
|---|---|
| Assemblage | Initialize crée Z80A avec quatre délégués mémoire/E/S, VDP, PSG, RAM et cartouche. Les événements vidéo/audio sont relayés. EndOfScanline déclenche une réception d'entrée hôte. |
| Cadence | RunFrame répète RunStep et soustrait le budget d'image en conservant son dépassement. RunStep exécute CPU.Step, multiplie les cycles par trois pour le domaine maître, puis avance VDP, IRQ, pause/NMI, PSG et cartouche. Les conversions utilisent double/Math.Round. |
| CPU | Step traite le délai EI, interruptions, HALT et dispatch d'opcodes/préfixes avec tables de cycles. Les lectures/écritures appellent directement les délégués. Le source indique qu'IM0 n'émule pas le véritable bus de données et choisit FF; ce n'est pas notre référence IM0. |
| Mémoire | RAM système par adresse masquée; cartouche par méthode Read/Write. SegaSGCartridge construit un masque de ROM arrondi à une puissance de deux et un masque de RAM; son Step ne fait rien. Les valeurs hors de la taille et configurations atypiques ne sont pas qualifiées par la seule existence du masque. |
| VDP | Step cumule les cycles jusqu'à une ligne; EndOfScanline, dépassement sprites, RenderLine, VBlank, avancement vertical et préparation des sprites sont ordonnés dans cette fonction. Quatre rendus de fond et sprites/bordures écrivent un framebuffer; PrepareRenderScreen clone ce framebuffer. VRAM et états du port sont dans le VDP. |
| Vidéo initiale et options | Le reset choisit notamment une ligne initiale aléatoire. Couches de fond, sprites et bordures ont des options de visibilité. Ces choix doivent être conservés comme faits de ce logiciel, sans les transposer automatiquement à RML. |
| PSG | Step cumule des compteurs de cycles, diviseur16, échéance de sample et de frame. Trois tonalités et le bruit ont leurs compteurs; le bruit met à jour un LFSR selon ses taps. Le latch sélectionne registre/canal et les données complètent fréquence/atténuation. Les buffers contiennent sorties séparées et mixées; l'événement de sortie les transforme en tableaux puis les vide. |
| Entrées | Clavier et XInput sont traduits en bits de ports conservés. Pause utilise un état/toggle dans la machine; la NMI est demandée dans RunStep. Il existe un TODO sur les ports C0–FF et l'absence de PPI. |
| Configuration et états | SetConfiguration/ReconfigureSystem fixent horloges, standard TV, taux audio et viewport. SaveStateHandler capture cartouche, RAM, CPU, VDP et PSG; SetState restaure ces objets puis recalcule la configuration. Ne pas en déduire une reprise arbitraire au milieu d'un front. |
| Reset et sortie | Reset appelle les composants, fixe SP à DFF0 et réinitialise les états d'entrée. Shutdown délègue aux composants. Les allocations de buffers, copies et polling à la ligne sont des mécanismes constatés, pas des recommandations automatiques de performance. |

## ares — C++

Sources : `aresWhole/Source/ares/ms/` (CPU/mémoire, VDP/fond/sprites/DAC/IRQ, PSG, contrôleurs, système et serialization), cœur Z80 et PSG partagés acquis. [Sources MS](https://github.com/ares-emulator/ares/tree/master/ares/ms).

| Mécanisme rencontré | Fonctionnement et points de reprise |
|---|---|
| Portée de modèle | Les fichiers examinés sont ceux du moteur MasterSystem/GG/SystemE. La présence d'un rendu TMS ou d'une compatibilité de cartouche ne prouve pas une machine SG-1000 complète. Dans system.hpp, Model énumère MarkIII, MasterSystemI, MasterSystemII, GameGear et SystemE; aucun modèle SG n'y figure. Ce moteur MS ne doit donc pas être présenté comme une machine SG distincte sur la seule liste utilisateur. |
| CPU | CPU::main consulte NMI puis IRQ, appelle le débogueur et l'instruction. step met à jour le temps du Thread puis synchronize. read/fetch peuvent appliquer un cheat et un déchiffrement différent pour opcode/donnée. |
| Mémoire et bus | readMapped prend MDR/pullup/pulldown, puis les mémoires réellement activées (RAM, BIOS, cartouche) selon modèle; write conserve MDR et répartit les écritures. Les régions de BIOS diffèrent entre GG et SMS. |
| E/S | Décodage par adresses/masques, ports GG parallèles/série, accès VDP, contrôle audio/FM, ports des manettes et directions/niveaux TR/TH. Une réserve explicite existe sur le décodage japonais MarkIII/SMS. |
| VDP | main prépare DAC, fond et sprites par ligne; sur256pixels actifs il calcule fond/sprite/DAC puis step(2). VBlank utilise step(512), puis une portion172; step boucle les clocks, avance H/V, évalue IRQ et synchronise le CPU. Il combine donc portion externe et travail interne fin. |
| Interruptions | Compteurs H/V et IRQ ligne/image sont gérés dans le VDP; irq.poll reste dans la progression. La fin de présentation appelle screen.frame puis sort du scheduler. Le scheduler coopératif est un mécanisme du projet, pas autorisé comme agenda commun RML. |
| Audio | PSG::main appelle SN76489::clock, convertit les quatre niveaux via une table préparée, mixe mono ou stéréo GG selon autorisations et mute, produit un frame audio puis step(1). Sa fréquence de Thread est colorburst/16; un filtre passe-haut est ajouté à la sortie. |
| Contrôleurs | Les ports ont leurs objets et sérialisations. Gamepad, palettes, sports pad, light phaser, souris et pads MD sont des classes distinctes dans les sources acquises. Leur présence dans le moteur MS n'établit pas leur compatibilité physique SG. Le détail de chaque protocole reste à relever localement. |
| Reset et ressources | power initialise CPU, bus et RAM; certains octets sont fixés pour compatibilité GG. load/unload créent/libèrent noeuds, buffers et ressources vidéo/audio. Les révisions VDP, dimensions, aspect et viewport sont configurés selon modèle/région. |
| États | SerializerVersion/signature sont contrôlés. Le mode synchronize entre dans le scheduler avant capture; la restauration peut power avant la désérialisation. CPU, VDP, PSG, BIOS, cartouche et ports sont sérialisés conditionnellement au modèle, ainsi que les équipements SystemE/FM. |

## MEKA — C/C++

Sources : `MEKAWhole/Source/meka/srcs/`, dont `sg1ksc3k.cpp`, CPU, VDP, rendu, palettes, entrées, mappers, son, machine et sauvegardes acquis. [Boucle SG/SC](https://github.com/ocornut/meka/blob/master/meka/srcs/sg1ksc3k.cpp).

- `Loop_SG1000_SC3000` est appelée au rythme du budget de ligne; elle cumule le temps sonore et avance la ligne modulo le standard TV.
- À la ligne zéro, elle traite les opérations diverses de cadre et l'arrêt du CPU; les hooks du débogueur ont un chemin conditionnel.
- Pendant les192lignes actives, les collisions sprites sont recherchées tant que le flag n'est pas déjà présent.
- À la ligne192, le rendu des modes0–3 est demandé selon frameskip, VBlank est positionné, les logs VGM peuvent être clôturés puis la présentation est appelée. Le commentaire avertit que la présentation peut provoquer un reset; l'ordre des mutations compte dans ce code.
- L'IRQ dépend ensuite du flag et de l'autorisation VBlank. Les variantes de cœur CPU sont sélectionnées à la compilation. Une branche NMI forcée existe avec une réserve du source sur sa priorité.
- Les mappings, ports, PSG, caches vidéo, configuration des machines et format de snapshot sont acquis mais leur relevé détaillé n'est pas terminé. Ne pas assimiler le rendu à ligne192 à l'absence d'écritures vidéo intermédiaires.

## FinalBurn Neo — C/C++

Sources : `FBNeo/Source/src/burn/drv/sg1000/`, cœur/interface Z80, TMS9928A et SN76496 acquis. [Pilote](https://github.com/finalburnneo/FBNeo/blob/master/src/burn/drv/sg1000/d_sg1000.cpp).

- Init mappe ROM/RAM avec ZetMapMemory, y compris extensions et miroirs SG (cas Othello et TheCastle distincts); des fonctions d'accès traitent les adresses non directement mappées.
- Les ports appellent le PSG, le port données/registres VDP, ou les deux octets d'entrées. Le callback VDP pilote l'IRQ Z80.
- Le driver configure SN76489A à3579545Hz avec sortie bufferisée datée par ZetTotalCycles; le TMS reçoit sa mémoire vidéo fonctionnelle.
- DrvFrame réinitialise au besoin, compile les entrées actives bas, traite le front pause/NMI puis exécute256tranches CPU entrelacées avec TMS9928AScanline. Son budget est3579545/60; cette division n'est pas notre cadence exacte NTSC.
- Audio et dessin sont produits seulement si les sorties hôtes correspondantes sont demandées. Le switch de limite de sprites porte explicitement une option hack.
- DrvScan visite la mémoire et les états CPU/PSG/VDP; une note signale que certains jeux emploient l'espace ROM comme RAM. Le chargement de ROM peut donc modifier la carte choisie; ce n'est pas un câblage unique universel.
- Le détail interne des reprises Z80, des buffers audio et des timings TMS reste à extraire des fichiers acquis avant toute réutilisation.

## MAME — C++

Sources : `MAMEWhole/Source/src/mame/sega/sg1000.cpp`, `sg1000a.cpp`, et devices Z80/TMS99xx/SN76496 acquis. [Machine](https://github.com/mamedev/mame/blob/master/src/mame/sega/sg1000.cpp), [VDP](https://github.com/mamedev/mame/blob/master/src/devices/video/tms9928a.cpp).

- Le pilote décrit séparément programme et E/S, miroirs et ports de cartouche/périphériques; entrées pause déclenchées par changement, modèles SG/SC/Othello configurés séparément.
- CPU, PSG et VDP sont des devices configurés; le READY du PSG est raccordé au WAIT CPU avec inversion. Il ne faut pas reprendre d'un autre pilote un PSG qui omettrait ce fil.
- La variante PAL SC change le VDP et utilise des oscillateurs CPU/PSG externes, explicitement parce que cette variante VDP n'a pas CPUCLK. Cela reste une donnée du pilote à confronter aux schémas, pas une preuve indépendante de silicium.
- Le VDP possède son espace VRAM et un timer `update_line`; `clock_grom` est un événement distinct. Ses registres, latches, buffers, masques et sélections de tables sont enregistrés pour la sauvegarde.
- Les TODO de ce VDP concernent notamment certains signaux externes, effets intra-ligne et fonctions particulières; ce sont les réserves de **leur device**, pas des omissions déduites pour RML.
- Le PSG a ses compteurs, registres, LFSR et stream; READY est géré par un timer séparé de sound_stream_update. L'écriture remet READY puis programme son retour, et le callback ne republie que les changements.
- L'agenda global MAME et ses adresses map/device delegates font partie du framework. Les fichiers du pilote ne décrivent pas à eux seuls le coût de cet agenda ni tous les microétats CPU. Le relevé de execute_run et des snapshots interrompus reste à compléter depuis les sources CPU acquises.

## Clock Signal / CLK — C++

Sources : `CLK/Source/Machines/MasterSystem/`, `Processors/Z80/`, `Components/9918/`, `Components/SN76489/`. [Machine](https://github.com/TomHarte/CLK/blob/master/Machines/MasterSystem/MasterSystem.cpp).

- La machine reçoit `run_for(cycles)` et délègue au Z80. Le CPU utilise des programmes de micro-opérations; une PartialMachineCycle contient opération, durée en demi-cycles, adresse, valeur et qualification de requête.
- Les cycles lecture/opcode/écriture/E/S sont divisés en début, phase d'attente et fin; rafraîchissement et opérations internes ont leurs séquences. Le curseur du programme est conservé si le budget ne suffit pas. WAIT peut répéter sa phase; BUSRQ possède sa boucle de concession. Ne pas remplacer cette description par « un appel par instruction ».
- `perform_machine_cycle` ajoute la durée au VDP différé et au temps PSG. Si un point de séquence vidéo est atteint, la ligne IRQ et son décalage sont reportés au CPU. Seules les opérations terminales effectuent les transferts mémoire/E/S.
- Mémoire par pages de1Kio avec pointeurs de lecture et d'écriture distincts; la carte gère les registres de mapper puis reconstruit les mappings seulement lors de changements. Les miroirs SG/SMS ont des tailles différentes.
- Les accès aux ports VDP obtiennent l'objet différé à jour; ceux du PSG appellent update_audio avant l'écriture. Les entrées utilisent objets joystick et directions TR/TH; les transitions TH peuvent verrouiller H/V.
- `flush_output` pousse la vidéo et, pour le son, actualise le speaker puis exécute la file audio. Un filtre/queue de sortie n'est pas un cycle électrique supplémentaire de PSG.
- Le fichier contient des TODO de contrôle mémoire/E/S; leur précision ne doit pas être inventée. Le détail de l'objet différé VDP, de ses points de séquence et de la capture CPU partielle reste à approfondir dans les sources acquises.

## BlastEm — C

Sources : `BlastEmWhole/Source/{sms.c,z80*,vdp.c,psg.c,io.c,serialize.c,...}`. [Machine](https://github.com/libretro/blastem/blob/libretro/sms.c).

- `run_sms` fixe une cible puis demande l'exécution CPU; VDP/PSG sont rattrapés et les limites peuvent changer. Les interruptions V/H fournissent leurs prochaines dates via update_interrupts.
- Les ports VDP sont synchronisés à la date CPU avant lecture/écriture; la lecture status modifie les flags pendants et recalcule les interruptions. Certains contrôles emploient le rattrapage VDP complet. Une écriture PSG appelle psg_run avant psg_write.
- Les entrées I/O portent les dates CPU; directions/niveaux TH et latchH/V sont séparés. Le code inclut aussi PPI, clavier et cassettes SC; les conserver dans le relevé n'autorise pas à ajouter ces périphériques à la SG en cours.
- Les configurations identifient explicitement TMS9918A, SMS, GG ou Genesis. Les mappings et RAM de cartouche sont reconstruits depuis les registres; le contrôle mémoire possède un TODO.
- Deux familles de cœur sont conditionnées par NEW_Z80. Le chemin historique conserve un PC natif et invalide le code traduit lors des écritures/remappings/restaurations concernés. La présence de traduction native ne prouve pas sa rentabilité dans notre cas.
- Les états sont divisés en sections enregistrées par handlers : Z80, VDP, PSG, I/O, RAM, mapper, RAM cartouche. Des chargements peuvent être différés jusqu'à un point de CPU accepté; les plages de code RAM sont invalidées après restauration.
- Audio, logs VGM, scope et mises à jour de configuration ont leurs chemins propres. Les détails de codegen, bus modifiables, interruption d'une traduction et tous les états de filtres ne sont pas encore relevés exhaustivement; sources acquises.

## DSP Emulator — Pascal

Sources : `DSP/Source/src/consolas/sg1000.pas`, `cpu/z80/`, `devices/tms99xx.pas`, `snd/sn_76496.pas`, `misc/snapshot.pas`, `misc/qsnapshot.pas`. [Pilote](https://github.com/leniad/dsp-emulator/blob/master/src/consolas/sg1000.pas).

- sg_principal reçoit les entrées une fois avant ses262lignes, appelle z80.run avec un budget, conserve le dépassement par tframes/contador, puis tms.refresh_ntsc. Après les lignes, copie de la zone vidéo et video_sync.
- Lectures mémoire directement dans memoria; écritures sélectionnées par régions et options RAM8K/extension. Les ports appellent TMS données/registres, SN.write ou les octets d'entrée.
- Pause conserve un état; la branche observée demande une NMI pulsée au relâchement après appui. Ne pas transposer cette polarité sans contrôle matériel.
- Le callback TMS pilote l'IRQ CPU; le callback sonore appelle le PSG update. Reset remet composants, budget, ports et état pause.
- Le chargeur borne la taille de ROM, accepte un format snapshot DSP et possède des choix d'extension/mapping liés à des CRC de jeux. Une copie de ROM spécifique existe pour SafariHunting avec une réserve dans le commentaire.
- La sauvegarde passe par snapshot_main_write(SSG1000); les formats et les états temporels internes CPU/TMS/PSG sont acquis mais leur examen détaillé reste à faire localement.

## Mesen 2 — C++

Sources : `MesenWhole/Source/Core/SMS/` et Serializer/Emulator/configuration acquis. [Console](https://github.com/SourMesen/Mesen2/blob/master/Core/SMS/SmsConsole.cpp), [gestion mémoire](https://github.com/SourMesen/Mesen2/blob/master/Core/SMS/SmsMemoryManager.cpp).

- **Présence SG vérifiée dans les sources acquises** : SmsModel::Sg et branches isSg1000 du gestionnaire mémoire. Cela remplace l'incertitude initiale sur la présence d'un chemin SG, sans déclarer le montage matériel entièrement fidèle.
- RunFrame répète CPU.Exec jusqu'au changement d'image; Exec utilise les opcodes/préfixes, HALT, NMI et IRQ. Le source porte un TODO pour IM0 et utilise RST38 dans le cas correspondant; ne pas le substituer à notre CPU générique.
- Les instructions appellent ExecCycles; le gestionnaire ajoute clocks à _masterClock puis fait VDP.Run jusqu'à cette date. Il n'y a donc pas seulement un rattrapage aveugle à la fin de l'instruction.
- Mémoire par pages256octets, pointeurs distincts lecture/écriture et drapeaux de registres; mappings actualisés lorsque configuration/mapper le demande. OpenBus, cheats et notifications de débogage sont gérés dans le même accès, distinctement des octets stockés.
- Le PSG se rattrape jusqu'au temps maître, par diviseur16; les tonalités et bruit mettent à jour leurs compteurs. Seuls les changements gauche/droite ajoutent un delta Blip. Une écriture Run d'abord, puis modifie le latch/registres; PlayQueuedAudio clôt le temps et extrait les samples.
- Le contrôle des périphériques reconstruit les appareils uniquement si la configuration change. Les lectures combinent les appareils connectés et appliquent directions/niveaux TR/TH; les transitions deTH peuvent demander un latchH. Les objets SMS, lightphaser et Coleco sont distincts.
- Serialize du PSG actualise l'état avant capture; au chargement il vide les buffers Blip et le compteur de sortie. Les états de canaux et temps sont sérialisés. Cela ne prouve pas une continuité PCM identique au format d'état RML.
- Le VDP possède ses modes/rendus et sérialise aussi des temps et buffers; le détail des accès VRAM, événements intra-ligne, reset et reprise CPU interrompue reste à relever dans les fichiers acquis, sans supposer une identité avec le TMS physique.


### CLK : acquisition et sortie vidéo séparées

Détail complémentaire relevé dans `Components/9918/Implementation/9918.cpp`, `9918Base.hpp`, `Fetch.hpp`, `Draw.hpp`, `LineBuffer.hpp`, `LineLayout.hpp`, `ClockConverter.hpp`, `Storage.hpp` et `PersonalityTraits.hpp` :

- `run_for` convertit une fois le budget de demi-cycles vers le domaine interne; des pools distincts portent le travail d'acquisition et de sortie. Il y a deux curseurs, pas une demande de présentation qui cadencerait le CPU.
- L'acquisition borne sa portion à la fin de ligne. Les fenêtres mémoire initiale/finale sont calculées depuis les colonnes; la fonction de fetch est choisie par mode (texte, caractères, SMS, refresh, Yamaha). Les templates distinguent portion interrompue et portion terminant la fenêtre. Les accès alimentent des buffers de ligne.
- Les registres à latch, par exemple le scrolling Sega, sont capturés lorsque le curseur traverse leur seuil. Les commandes Yamaha, IRQ ligne et image ont leurs points propres. Le source conserve une réserve sur l'origine d'une constante de latch Sega; cette constante n'est pas une mesure acquise pour RML.
- La sortie prend sa propre portion à la fin de ligne. Une occurrence CRAM dot peut couper cette portion; elle n'est pas effacée par le regroupement.
- Sync, blanking, burst et bordures sont émis comme durées ou intersections de régions. Le source peut produire une séquence entière lorsqu'un seuil est franchi; son contrat de sortie doit donc être distingué d'un observateur qui demanderait l'état à tout instant intermédiaire.
- Le dessin actif reçoit un intervalle relatif début/fin et écrit directement dans la zone fournie par le CRT. `begin_data` et `output_data` encadrent cette zone; les pointeurs sont ensuite invalidés. Le choix de rendu dépend du fetch_mode du buffer acquis, pas d'une réinterprétation systématique de toutes les broches pour chaque pixel.
- Les deux curseurs peuvent avancer séparément à l'intérieur de run_for; le code vérifie en Debug que l'étape de sortie ne déplace pas le curseur d'acquisition et réciproquement. Le décalage `output_lag` est une donnée de cette implémentation; ne pas l'identifier automatiquement au pipeline RML.
- `next_sequence_point` prédit l'IRQ de ligne/image depuis compteurs, autorisations et état courant. Il retourne une absence de point si aucune IRQ future n'est possible ou si la ligne est déjà active. La prévision est convertie dans le domaine reçu. Une écriture ou lecture qui modifie les conditions doit donc actualiser la synchronisation du propriétaire.
- La version TMS du projet déclare plusieurs variantes à travers une personnalité commune et un standard TV; Yamaha, SMS, GG et MD ont des branches distinctes. Cela ne justifie pas d'omettre les différences physiques des variantes RML.
- Les deux pointeurs et buffers sont une représentation logicielle des données et du temps. Ce ne sont pas des lignes RAM physiques à ajouter à une carte. L'application éventuelle à EmuChip doit démontrer conservation des lectures, des latches, des événements status/IRQ, du retard de pipeline et des observations avant chaque interaction.

### CPU : unités de travail différentes, à ne pas confondre

- Dans CLK, le programme Z80 conserve un pointeur de micro-opération. Un cycle de bus trop long pour le budget fait reculer ce pointeur avant retour; une phase WAIT peut être répétée. Les accès terminaux sont signalés au raccordement de machine. Cela réduit le nombre d'interactions par rapport à un journal par front, sans signifier que toutes les broches et sous-phases sont exposées.
- Dans jgenesis `cpu/z80-emu/src/core.rs`, `execute_instruction` consulte RESET/BUSRQ avant son instruction. Le RESET réinitialise I/R/PC/IFF/mode/délai/HALT; BUSREQ renvoie un minimum de T-cycles avec état stalled. Son entrée `tick` ne déroule pas la puce en T-cycles : elle exécute l'instruction complète puis décrémente un temps d'attente. Une observation d'état au milieu de tick n'est donc pas équivalente par construction à celle d'un séquenceur physique partiel.
- Dans Essgee `CPU/Z80A.cs`, Step vérifie le délai EI, puis les demandes INT/NMI, avant de lire et dispatcher le prochain opcode. Les méthodes des préfixes choisissent des tables de délégués; IX/IY sont transmis par référence. Le compteur R est mis à jour par les chemins de fetch/préfixes et le coût provient des tables de cycles, avec compléments conditionnels dans les instructions. Un logger optionnel fait disassembler et écrire sur fichier à chaque instruction; ce chemin ne doit pas être confondu avec le coût du moteur sans journal.
- Dans Mesen `SmsCpu::Exec`, HALT consomme quatre cycles, sinon ReadOpCode/ExecOpCode est exécuté; les demandes NMI/IRQ sont traitées ensuite, avec gestion du délai EI et du mode. IM2 lit le vecteur depuis mémoire/open bus; le TODO IM0 est explicite. Le gestionnaire temps fait avancer le VDP pendant les ExecCycles des accès/instructions.

Ces différences sont consignées même lorsqu'elles ne pourront pas être reprises telles quelles. Un nom `tick`, `cycle`, `step` ou `run` ne prouve ni la granularité ni le contenu matériel conservé.

### Mesen : activité vidéo fine malgré un raccordement compact

`SmsVdp::Exec` choisit selon Cycle et RenderingEnabled les chargements de fond SG/SMS, évaluations de sprites, chargements de tuiles sprites, rendu du pixel et créneaux CPU VRAM inutilisés. Les événements de ligne ont leurs colonnes; les dernières colonnes traitent les premières entrées de sprites et la fin de ligne. ForcedBlank conserve des travaux différents selon mode et colonne. Une entrée dans ce moteur par une plage temporelle ne signifie donc pas qu'il dessine toute la ligne d'un seul calcul.

`UpdateDisplayMode` prépare les dimensions, masques de name table et adresses effectives selon révision, standard et bits de mode. Les lectures du compteur vertical ont une fonction de repli selon standard et hauteur; elles ne relisent pas une position hôte. Ces préparations aux changements et ces règles de compteurs font partie du relevé, même sans preuve de leur applicabilité à un TMS SG donné.

### Genesis Plus GX : restauration et reconstitution des connexions

`core/state.c::state_load` contrôle signature/version puis reset le système. Il restaure RAM, registres I/O, contexte VDP et audio, registres/cycles du Z80 et callback IRQ. Les mappings et options PSG sont reconstitués selon le système courant; des branches MD/CD existent. Restaurer un pointeur de callback sauvegardé brut serait incorrect, et ce code le réassigne. Les états de la couche audio et du VDP sont délégués à leurs fonctions context_load; le détail des filtres et d'une coupure à l'intérieur de leurs calculs reste à relever localement.

### Détails supplémentaires : son, ports et états des moteurs historiques

**MEKA**, `srcs/sound/psg.cpp`, `srcs/z80marat/Z80.cpp`, `srcs/saves.cpp` :

- Le cœur Marat emploie des fonctions mémoire/E/S et leurs variantes sans hooks. Des tables préparent des flags zéro/signe/parité et les macros d'ALU les combinent avec les calculs de retenue et autres flags; les pages d'opcodes ont leurs fichiers Codes*.h. Le hook de boucle de machine est un pointeur distinct de ces accès. Les variantes de gestion d'interruptions portent des options de compilation et une option explicitement WIP.
- Le PSG porte une structure globale et une autorisation Active. La génération écrit un buffer par samples; les valeurs gauche/droite sont combinées ou saturées selon la sortie mono/stéréo. Le mécanisme déclare une interpolation des positions fractionnaires de transitions. Les atténuations proviennent ici d'une table2dB; des mesures SMS2 alternatives apparaissent en commentaire et ne sont pas la table active.
- Le journal WAV est une sortie du buffer sonore, le VGM un mécanisme de journalisation distinct. Aucun n'est un circuit de cassette à ajouter à la machine RML.
- PSG_Save conserve huit registres, latch, stéréo, LFSR, puis fréquence/position/intermediate position des quatre canaux. La sauvegarde conserve donc des données d'implémentation en plus des registres publics.
- `Load_Game_Fixup` refait les mappings depuis les registres restaurés. Certains remappings passent par des écritures CPU sans hooks; le code préserve explicitement les octets RAM aux adresses de registres, puis rétablit des miroirs. Ce mécanisme de reconstruction est à distinguer du stockage direct de pointeurs d'hôte.

**FinalBurn Neo**, `src/burn/snd/sn76496.cpp`, `src/burn/devices/tms9928a.cpp` :

- `SN76496SetBuffered` installe le fournisseur de cycles CPU et la cadence. `SyncInternal` convertit la position CPU vers une position de samples dans le cadre à l'aide de la longueur et du FPS du framework.
- Avant une écriture sonore, UpdateStream produit seulement la portion non encore écrite. SN76496Update complète jusqu'à la fin du buffer puis copie/mixe avec saturation et remet la position à zéro. Le mode bufferisé exige une clôture par frame de longueur attendue. Ce contrat n'est pas celui d'une suite de transitions analogiques datées arbitrairement.
- Les canaux conservent période, compte et sortie; le LFSR et son couplage à la tonalité sont dans l'état du chipset. Les types SN ont des paramètres de bruit/retour propres; les champs ne doivent pas être transposés à une autre variante sur la seule ressemblance du nom.
- Le port TMS renvoie ReadAhead puis recharge la donnée VRAM et incrémente l'adresse. Une écriture VRAM actualise aussi ReadAhead. La lecture status conserve FifthSprite, réévalue l'IRQ et remet le latch de commande à zéro. Le second octet de commande construit l'adresse ou change un registre, avec prélecture pour la commande lecture. Ces mécanismes sont fonctionnels et immédiats dans le code lu, sans résolution de RAS/CAS des huit MB8118.
- Le rendu TMS par ligne parcourt les sprites, gère taille, magnification, cinquième sprite, priorité et collision via des marques par position. L'option de limite modifie ce chemin. DrvScan et TMS9928AScan conservent les états propres déclarés; une reprise à une sous-phase RAS/CAS n'est pas établie.

**DSP Emulator**, `src/cpu/z80/nz80.pas`, `src/devices/tms99xx.pas`, `src/snd/sn_76496.pas` :

- Le CPU exécute dans run jusqu'à son compteur/budget. HALT et états d'interruption ont des branches propres; le coût est ajouté via table d'instructions et états supplémentaires. Un callback après instruction reçoit la différence du compteur; il peut lui-même modifier ce compteur, raison pour laquelle le code la recalcule.
- Le snapshot CPU copie les registres puis HALT, daisy, after_ei, IRQ/NMI demandées, état NMI, contador et vecteur IRQ. Il ne faut pas assimiler ce format à la liste des phases et propagations du CPU RML.
- Le snapshot TMS copie registres et16Kio de VRAM, puis mode, status, ReadAhead, adresse, IRQ, état du second octet, tables/masques, couleurs, cinquième sprite et attente de lecture. Le chargement relit ces champs dans le même ordre. Les buffers et registres internes sont donc visibles dans le format, même si le rendu est piloté à la ligne.
- `refresh_ntsc`, `register_r/w`, `vram_r/w` sont des entrées séparées; les helpers de snapshots PSG et TMS sont acquis et ne doivent pas être remplacés par une supposition sur la sérialisation des autres machines du dépôt.

**MAME**, `src/devices/cpu/z80/z80.cpp` et `z80.lst` :

- execute_run inclut un exécuteur généré `z80.hxx`; les sources de génération et descriptions d'instructions doivent être distinguées du fichier compilé. Le CPU conserve des service-attention bits, états WAIT/BUSREQ et demandes d'IRQ/NMI; execute_set_input les modifie suivant ligne et front.
- Le source sauvegarde également des états internes, pas seulement les registres publiés. L'identité complète du contexte de reprise généré n'est pas établie par la seule liste save_item de z80.cpp; consulter la description locale et, si nécessaire, acquérir le générateur manquant plutôt que conclure à une reprise complète non vérifiée.

**CLK**, `Processors/Z80/State/State.hpp` et `State.cpp` :

- Le format State sépare registres, entrées IRQ/NMI/BUSRQ/WAIT et ExecutionState. Ce dernier contient HALT, demandes présentes/précédentes, temporaires, opcode, historique d'ajustement de flags, incrément PC, adresse de refresh, phase, nombre de demi-cycles dans le pas, pas dans la phase et page d'instructions.
- Les phases de reprise nomment reset, IRQ modes0/1/2, NMI, fetch/decode, opération et appel conditionnel non pris. L'application de cet état doit reconstruire le curseur de programme, pas restaurer une adresse de code hôte.
- Cette classe de réflexion est un adaptateur optionnel au CPU, pas une dépendance obligatoire de son moteur de progression. Le commentaire qui évoque encore un6502 dans une sous-section est une erreur rédactionnelle du fichier, pas une propriété du Z80.


### CLK : calcul différé, accès et prédiction de la prochaine interaction

Les neuf fichiers de `CLK/Source/ClockReceiver/` sont acquis en complément. `JustInTime.hpp` contient un mécanisme distinct d'un compilateur de code machine, malgré le nom JustInTime :

- L'objet conserve le composant et le temps reçu depuis sa dernière mise à jour. L'ajout de temps ne provoque pas systématiquement run_for.
- L'accès par operator-> commence par flush; un garde de fin d'accès actualise ensuite la prochaine date de séquence. Ce garde ne détruit pas le composant emprunté. L'accès brut get ne fait pas de flush : ce sont deux contrats d'observation différents.
- Si le composant fournit next_sequence_point, l'ajout de temps décrémente une borne locale et peut provoquer flush. Le wrapper mémorise aussi le dépassement de ce point dans la portion reçue, puis la machine transmet cet offset à l'IRQ CPU. RML doit conserver sa coupure exacte avant dépendance; copier ce mécanisme de dépassement sans son contrat CPU déplacerait la date de l'IRQ.
- Le facteur de conversion local/cible est paramétré; les restes non consommés restent conservés par les opérations flush/divide. Les chemins unité/unité évitent les conversions; la valeur max de l'absence d'événement est traitée sans multiplication qui déborderait.
- Le composant peut notifier une préférence de cadence : aucune, temps réel, ou différé. Le wrapper la mémorise par observation de ClockingHint; il n'appelle pas le composant en boucle pour découvrir s'il veut être avancé.
- La classe expose did_flush, will_flush, le temps depuis flush, la borne de flush implicite et la conversion inverse back_map. Ces méthodes ont des rôles distincts : un temps reçu n'est pas un transfert mémoire, et une prévision n'exécute pas encore l'événement physique.
- Des variantes asynchrones sont mentionnées par le fichier. Aucun thread de puce ni file d'exécution hôte n'est décidé pour RML à partir de cette mention.

### PSG : autres représentations et différences de variante

**Genesis Plus GX**, `core/sound/psg.c`, `sound.c` : le PSG conserve des dates de prochaines transitions par canal, des polarités, périodes et deltas de volume pendants. psg_update parcourt les transitions jusqu'à la date demandée, au lieu de reconstruire chaque clock source. Le bruit décale le LFSR sur le front de polarité prévu et utilise son masque/largeur de variante. Les deltas alimentent Blip avec un chemin qualité normale/rapide configurable. psg_end_frame soustrait le temps de cadre aux dates locales et conserve les restes. sound_update clôt le temps Blip; les contextes PSG/FM et le départ temporel FM ont leurs fonctions save/load. Le reset choisit PSG_DISCRETE pour SYSTEM_SG et PSG_INTEGRATED pour d'autres machines : cette distinction existe réellement dans ce moteur.

**jgenesis**, `backend/ti-sn76489/src/lib.rs` : registres de tonalité et compteurs sont séparés; une tonalité nulle recharge1dans le générateur carré. Le bruit possède son propre compteur, polarité de clock, LFSR et sortie mémorisée; son mode choisit une période fixe ou la tonalité2. Le polynôme dépend de Sn76489Version (Standard/MasterSystem2/Discrete) et le latch de bruit remet le LFSR à l'état initial. Deux tables de volume existent, dont un écrêtage SMS2 dont la valeur est explicitement arbitraire dans le source. Les types dérivent Encode/Decode; stéréo, mute de canaux et mélange sont des états distincts des registres physiques.

Ces algorithmes et réserves restent documentés même lorsqu'ils ne seront pas appliqués au SN76489AN de la carte courante. Une boucle par transition sonore, une boucle par clock divisée et une génération par sample sont trois mécanismes différents.

## État du moteur RML confronté aux sources

Vérification locale du13novembre2026, sans changement de comportement :

- `EmuZ80` conserve déjà la sémantique des instructions, le contrôle et le séquenceur dans des objets distincts; `EmuZ80.Bus.cs` est organisé autour de transactions MemoryRead/Write, OpcodeFetch et E/S et de leur continuation. Il n'est donc pas nécessaire de recopier un décodeur tiers pour obtenir cette séparation.
- `Z80BusWaveforms.cs` prépare **déjà** les six tableaux de phases depuis le profil de timing, une fois par séquenceur. Proposer seulement de précompiler ces chronogrammes serait répéter une optimisation présente. Ce qui reste coûteux est leur interprétation par front, la propagation des sorties et le transport reconstruit après cette interprétation.
- `EmuZ80.BusRange.cs` avance encore par ApplyClockEdge hors des portions internes stables. Le séquenceur accumule jusqu'à128mots de sorties, puis ExchangeRangeInputs les remet au câblage avant capture DATA/WAIT/contrôle. Les lectures/écritures fonctionnelles du décodeur ne sont pas le seul coût.
- `EmuMb8118ReadPort` prépare déjà huit vues de cellules distinctes et un programme de lecture partagé; il peut prolonger un epoch qualifié, conserve refresh et dates de changement communes, et matérialise les huit états lors d'un retrait. Recréer un port groupé ou remplacer les cellules par un tableau maître serait une répétition ou un changement de responsabilité.
- Le port DRAM construit encore les huit programmes de sortie de chaque lecture. Le VDP et la carte consomment ces programmes; les hypothèses de qualification (timing, connaissance, fraîcheur et succession des accès) demeurent et ne peuvent être retirées au nom d'un accès direct fonctionnel.
- `EmuSn76489.ClockSource.cs` dispose déjà de progression périodique par préfixes stables et de bornes READY; son analogie avec un PSG rattrapé par date n'est pas un travail absent à réimplémenter. Le conserver dans la conception globale n'implique pas de réécrire cette optimisation déjà qualifiée.

La reprise doit donc changer l'unité réellement exécutée et transportée, pas ajouter un autre cache, une autre table de phases ou un autre wrapper portant le même travail. Les chiffres et échecs antérieurs doivent rester consultables au même endroit.

## Informations encore à compléter avant choix d'une réalisation globale

Le relevé ci-dessus ne sélectionne ni ne retire aucun mécanisme parce qu'il semble inutile. Les sources et les points de reprise permettent de poursuivre sans relire les sites. Les zones marquées non examinées doivent être remplies dans ce document depuis les archives : détails des caches/rendus et invalidations; reprises CPU partielles; chronologie des accès et attentes; buffers sonores et restauration; protocoles de chaque périphérique; variantes/mappers et reset. Une fonction trouvée par recherche de texte ne vaut pas examen de son algorithme.

Le choix de réalisation EmuChip doit être formulé ensuite sur **l'ensemble du parcours**. À ce stade, aucun nouveau gain n'est annoncé et aucune nouvelle abstraction de transport n'est créée. Les originaux RML restent des références de comparaison; les limitations des moteurs tiers ne deviennent pas les nôtres. Les résultats négatifs Q/R, les profils réels et leurs limites restent dans l'étude d'architecture.
