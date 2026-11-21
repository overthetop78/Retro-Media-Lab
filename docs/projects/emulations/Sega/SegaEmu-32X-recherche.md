# SegaEmu — références pour ajouter le 32X

Recherche du **15 novembre 2026**, préparée pour une implémentation ultérieure. Aucun changement du moteur SegaEmu, aucune compilation et aucun lancement de jeu n'ont été effectués pendant cette recherche. Les observations de compatibilité ci-dessous viennent des projets cités ; elles ne constituent pas une campagne de tests locale.

## Émulateurs confirmés

| Projet | Prise en charge vérifiée | Intérêt pour SegaEmu |
|---|---|---|
| [PicoDrive](https://github.com/irixxxx/picodrive) | 32X ; existe en autonome et en cœur libretro. | Bus, deux SH-2, PWM, composition vidéo et ordonnanceur avec réveils sur événements. |
| [ares](https://ares-emu.net/compatibility/sega-32x) | Catalogue de compatibilité 32X publié par le projet. | Découpage du processeur SH-2 et des périphériques, implantation du 32X dans la Mega Drive, sérialisation. |
| [jgenesis](https://github.com/jsgroth/jgenesis/blob/master/README.md) | Le 32X figure dans les systèmes pris en charge ; un cœur `s32x-core` et une bibliothèque `sh2-emu` sont présents. | Organisation des bus, registres, vidéo et audio, commentaires sur les délais d'accès. |
| [BlastEm 1.0.0](https://www.retrodev.com/blastem/announce_1.0.0.html) | 32X et CD 32X annoncés le **11 septembre 2026**. | Implémentation indépendante, SH-2 décrit dans une DSL et périphériques SH7095 séparés. |
| [Kega Fusion](https://www.carpeludum.com/kega-fusion/) | Le site de distribution de Steve Snake énumère explicitement le 32X. | Comparaison du comportement ultérieure ; le site ne fournit pas le code du cœur à étudier. |
| [Gens et Gens/GS](https://github.com/lutris/gens/blob/master/doc/history.txt) | L'historique annonce l'ajout du 32X dans Gens 2.00 ; le [ChangeLog Gens/GS](https://github.com/lutris/gens/blob/master/ChangeLog.txt) décrit ses corrections 32X. | Référence historique ; forte présence d'assembleur x86, moins directement exploitable pour le découpage C#. |
| [Gens Rerecording](https://github.com/TASEmulators/gens-rerecording) | Le projet déclare Genesis, Sega CD et 32X. | Variante de la famille Gens, utile pour les outils d'observation et de comparaison. |
| [MAME](https://github.com/mamedev/mame/blob/f52a78c1d691ae8abfba02ce3eceef4144c0b116/src/mame/sega/mdconsole.cpp) | Matériel 32X et configurations USA/Europe/Japon, avec variantes CD, présents ; **les six configurations sont marquées `MACHINE_NOT_WORKING`** à la révision consultée. | Référence complémentaire de registres et de câblage, pas témoin de bon fonctionnement des jeux. |

[RetroArch avec PicoDrive](https://docs.libretro.com/library/picodrive/) et [BizHawk avec PicoDrive](https://github.com/TASEmulators/BizHawk/issues/4852) permettent également de lancer du 32X. Ce sont ici des intégrations du même cœur ; le fait que BizHawk soit écrit en C# ne signifie pas qu'il possède un autre moteur SH-2 C# indépendant.

La [FAQ historique de BlastEm](https://www.retrodev.com/blastem/trac/wiki/FAQ) annonce encore le 32X comme un développement futur : l'annonce 1.0.0 plus récente et son archive source établissent son ajout. Ne pas reprendre une ancienne liste de compatibilité pour l'exclure. Ne pas confondre non plus Gens/GS et sa réécriture Gens/GS II : les preuves ci-dessus concernent les variantes identifiées, pas toute application portant le nom Gens.

## Références conservées localement

Le [manifeste des sources](../../../../artifacts/References/Machines/Sega/32X/source-index.json) donne les chemins, URL, révisions et SHA-256 des copies locales. Les fichiers sont des références de travail, sans dépendance ajoutée au runtime SegaEmu. Les copies GitHub peuvent avoir leurs fins de ligne normalisées ; le SHA de blob amont et le SHA-256 local sont des identifiants distincts.

| Ensemble | Révision retenue | Fichiers prioritaires |
|---|---|---|
| PicoDrive / irixxxx | `9f46ee4e9680d108e548fb427e7c7e1aa1bb9d5a` | `pico/32x/{32x,memory,draw,pwm,sh2soc}.c`, `cpu/sh2/sh2.{c,h}`, interpréteur sous `cpu/sh2/mame/`, définitions `pico_int.h`. |
| ares | `0b928930af4cc59ed99e942996f0d3c815379388` | `ares/md/m32x/`, `ares/component/processor/sh2/`, adaptation SH7604 et carte `mega-32x.cpp`. |
| jgenesis | `237545e536ad9f2cccfea2dbc19adb386ce1fb69` | `backend/s32x-core/src/{api,bus,registers,vdp,pwm,audio}.rs`, `cpu/sh2-emu/src/`. |
| MAME | `f52a78c1d691ae8abfba02ce3eceef4144c0b116` | `src/mame/shared/mega32x.{cpp,h}`, déclaration `mdconsole.cpp`, processeurs `src/devices/cpu/sh/`. |
| BlastEm | `2b2a182f45226deb055979659ca844d63c49b462`, tag `v1.0.0` | `32x.{c,h}`, `32x_video.{c,h}`, `sh2.cpu`, `sh7095.{c,h}`, décodage/utilitaires SH-2 et raccordement `genesis.c`. |

L'archive BlastEm entière a servi uniquement à sélectionner ces fichiers. Les sources retenues et son empreinte restent conservées ; l'archive et le reste du dépôt ne sont pas gardés. Aucune source collectée n'a été compilée ou intégrée à SegaEmu.

Les manuels se trouvent dans [manuals](../../../../artifacts/References/Machines/Sega/32X/manuals) : manuel d'instructions Hitachi SH-1/SH-2 et manuel matériel SH7604 en PDF, transcriptions Sega des informations matérielles et du manuel 32X, et transcription annotée du supplément 2. Les annotations de transcription doivent être distinguées du texte Sega et confrontées aux autres sources.

Le [catalogue Exodus](https://techdocs.exodusemulator.com/Console/SegaMegaDrive/Documentation.html) recense aussi le manuel Sega révision 8, les suppléments 1/2/3 et les bulletins techniques. Les deux téléchargements essayés pour le PDF Sega ont été refusés comme copie locale utilisable : protection HTTP chez ConsoleMods, page HTML chez le miroir Drive. Aucun faux PDF n'est conservé. La transcription Sega et les deux PDF Hitachi sont disponibles hors ligne ; les scans Sega complémentaires restent référencés par URL. Aucun BIOS ni jeu n'est ajouté à ce dossier.

## Matériel à fabriquer

Le 32X s'ajoute à une Mega Drive qui continue d'exécuter son 68000, son Z80, sa vidéo et son audio. Il faut une machine composée ; remplacer le 68000 par un SH-2 empêcherait d'émuler le fonctionnement réel des jeux.

| Élément | Travail indispensable |
|---|---|
| Deux SH-2 | Deux états CPU distincts, instructions entières, branchements retardés, exceptions, interruptions et `SLEEP`. Le manuel Sega nomme les processeurs du 32X **SH7095** ; plusieurs références utilisent un modèle SH7604. Séparer le cœur d'instructions du profil de périphériques et vérifier leurs différences. |
| Périphériques internes CPU | Cache de 4 Kio avec configuration cache/RAM, contrôleur de bus, DMA à deux canaux, unité de division, FRT, watchdog, SCI et registres de priorité/vecteurs. Ne pas supposer qu'un interpréteur d'opcodes suffit. |
| SDRAM | 256 Kio partagés par les deux SH-2, avec arbitrage et accès temporisés. |
| Vidéo 32X | Deux banques de framebuffer de 128 Kio, palette de 512 octets, table d'adresses de lignes, accès normal/overwrite, autofill, basculement de banque et états d'occupation. |
| Modes vidéo | Désactivé, pixels indexés 8 bits, couleur directe RGB555 et RLE. La vidéo 32X doit être composée avec celle de la Mega Drive selon les priorités et transparences. |
| Audio PWM | Deux canaux, échantillons de largeur d'impulsion sur 12 bits, deux FIFO de trois mots, période programmable, sortie mono/stéréo, requêtes DMA et interruptions. Mélanger ce signal aux autres sources audio. |
| Interface Mega Drive | Activation/reset, droits d'accès VDP, fenêtres ROM et sélection de banque, huit mots de communication, transfert DREQ/FIFO et interruptions. |
| Démarrage | Vecteurs côté 68000, ROM de démarrage master SH-2 et ROM slave ; reset et libération des processeurs dans le bon ordre. La présence d'un remplacement de BIOS dans une référence est une stratégie propre à cette référence, pas une fonctionnalité déjà disponible dans SegaEmu. |

La description matérielle de [jgenesis](https://github.com/jsgroth/jgenesis/blob/237545e536ad9f2cccfea2dbc19adb386ce1fb69/backend/s32x-core/README.md) est une bonne entrée en matière. Sa phrase ancienne sur l'absence de CD 32X ne doit pas servir à conclure sur toutes les versions : vérifier le code et la version correspondant à chaque essai.

### Carte mémoire de départ

Les fenêtres suivantes sont recoupées avec le manuel Sega et les bus ares/jgenesis. Cette table ne remplace pas les masques de bits, les miroirs et les règles d'accès du manuel.

| Côté | Adresse | Fonction |
|---|---|---|
| 68000 | `000000–0000FF` | Surcouche des vecteurs lorsque l'adaptateur est activé. |
| 68000 | `840000–85FFFF` | Fenêtre normale du framebuffer accessible au processeur. |
| 68000 | `860000–87FFFF` | Écritures overwrite du framebuffer. Les octets nuls n'ont pas le même comportement qu'une écriture RAM ordinaire. |
| 68000 | `880000–8FFFFF` | Fenêtre ROM fixe. |
| 68000 | `900000–9FFFFF` | Fenêtre ROM de 1 Mio sélectionnée par banque. |
| 68000 | `A15100–A1512F` | Contrôle, interruptions, banque, DREQ et communication. |
| 68000 | `A15130–A15138` | Contrôle/période PWM et ports gauche, droit, mono. |
| 68000 | `A15180–A1518B` | Registres VDP 32X, autofill et framebuffer. |
| 68000 | `A15200–A153FF` | Palette 32X. |
| SH-2 | `00000000–00003FFF` | Fenêtre de démarrage ; contenu master/slave distinct et miroirs à préciser. |
| SH-2 | `20004000–200043FF` | Vue sans cache des registres système, PWM, VDP et palette. |
| SH-2 | `02000000…` | Vue cartouche, avec miroirs, droits et délais d'accès. |
| SH-2 | `04000000–0403FFFF` | Fenêtres normale/overwrite du framebuffer ; vues sans cache correspondantes dans la région `24xxxxxx`. |
| SH-2 | `06000000–0603FFFF` | SDRAM ; vue sans cache `26000000–2603FFFF`. |

Sources de lecture prioritaires : [bus externe ares](https://github.com/ares-emulator/ares/blob/0b928930af4cc59ed99e942996f0d3c815379388/ares/md/m32x/bus-external.cpp), [bus interne ares](https://github.com/ares-emulator/ares/blob/0b928930af4cc59ed99e942996f0d3c815379388/ares/md/m32x/bus-internal.cpp), [registres jgenesis](https://github.com/jsgroth/jgenesis/blob/237545e536ad9f2cccfea2dbc19adb386ce1fb69/backend/s32x-core/src/registers.rs). Leurs copies locales sont indexées dans le manifeste.

### Synchronisation et pièges à prévoir

- Les SH-2 tournent nominalement à trois fois l'horloge du 68000, soit `3/7` de l'horloge maître Mega Drive. Conserver une conversion entière avec reste selon le standard PAL/NTSC, sans arrondir indépendamment chaque tranche.
- Chaque CPU doit avancer jusqu'à une échéance cohérente. Une lecture de registre partagé, une écriture de communication, une requête DMA ou une interruption peut imposer une synchronisation plus précoce qu'une fin de ligne vidéo.
- Les niveaux externes des SH-2 sont PWM 6, commande 8, HINT 10, VINT 12 et reset 14. Les masques et acquittements sont distincts pour les deux CPU ; ils ne sont pas ceux du 68000.
- `ADEN`, reset, `FM`, `RV`, les demandes DREQ et les états d'occupation ont des effets de bord. Le bit d'activation n'est pas présenté au même emplacement dans toutes les vues de registres. Le droit d'accès au framebuffer et les blocages sur ROM ne peuvent pas être remplacés par un tableau RAM toujours accessible.
- Respecter octet/mot/long, ordre des octets, lanes actives et alignement. Le stockage local de la Mega Drive est déjà adapté à ses accès : éviter un deuxième échange d'octets en raccordant les SH-2.
- Implémenter la table de lignes du framebuffer avant de considérer l'image correcte. Un framebuffer de 128 Kio ne contient pas une image de 320 × 224 pixels directs tous différents sur 16 bits ; les jeux peuvent réutiliser des lignes.
- Conserver les deux vidéos jusqu'au mélange des priorités. Tenir compte des modes H32/H40, du PAL, des bordures et du basculement de banque. Une simple surimpression opaque d'une image 32X sur la sortie finale ne suffit pas.
- Les FIFO PWM et DREQ sont différentes. Le PWM doit être horodaté, alimenter la sortie audio existante et conserver ses demandes d'interruption/DMA, même quand le rendu vidéo est sauté.
- Le supplément Sega et les bulletins couvrent notamment le reset/RV, le FRT, `TAS`, les accès aux registres internes et des problèmes d'interruptions : lire ces corrections avant d'attribuer un comportement particulier à un jeu.

PicoDrive contient des mécanismes de détection de boucles d'attente et de réveil sur événement. ares et jgenesis contiennent aussi des compromis et des `TODO` de temporisation. Ce sont des pistes d'optimisation à comparer, pas des preuves qu'une suppression d'attente préservera le fonctionnement ou améliorera automatiquement notre vitesse.

## Raccordement proposé à SegaEmu

**Constat local au commit `21fee6927` :** `BoardFactory` construit SG-1000, SC-3000, Mark III, Master System, Game Gear, Mega Drive, Mega-CD et Pico, sans carte 32X. Aucun SH-2 n'est présent sous `src/RetroMediaLab.Chips`. La boucle Mega Drive, les bus, les points de synchronisation audio et les hooks d'état existent, mais n'ordonnancent pas deux SH-2. La case générale Activer 32X ne crée pas ces composants.

La séparation ci-dessous est une **proposition de fabrication**, pas des classes déjà ajoutées :

| Emplacement proposé | Responsabilité |
|---|---|
| `Chips/CPU/Hitachi/SH2/` | Cœur d'instructions indépendant, état/contextes, bus typé, exécution jusqu'à une échéance, interruptions et exceptions. Aucun modèle Sega ni service d'application. |
| `Chips/Controllers/Hitachi/SH7095/` | Périphériques internes et configuration du SoC autour du SH-2 ; factoriser les éléments communs avec un futur profil SH7604 au lieu de mélanger leurs particularités. |
| `Chips/Video/Sega/VDP32X/` | Registres vidéo, palette, banques, autofill et production de pixels avec priorité. Le mélange final avec le VDP Mega Drive appartient au raccordement machine. |
| `Chips/Audio/Sega/PWM32X/` | FIFO, cadence, routage, signal audio, interruption et DREQ, avec contexte sauvegardable. Réutiliser le rééchantillonnage commun déjà présent dans Chips. |
| `SegaEmu/Machines/ThirtyTwoX/Hardware/` | Adaptateur 32X : deux SoC, SDRAM, registres partagés, arbitrage, BIOS et bus vers la carte Mega Drive. |
| `SegaEmu/Machines/ThirtyTwoX/` | Carte complète et sélection du scheduler ; composer la base Mega Drive et son extension. Concevoir la même extension pour le Mega-CD + 32X sans dupliquer le matériel Mega-CD. |

Points existants à faire évoluer :

1. `Constants/CoreConstants.cs`, `SystemHardware.cs`, `Factories/BoardFactory.cs` et `Media/Loading/LoadRom.cs` : déclarer le profil et les formats, reconnaître le matériel, puis vérifier les masques d'identification avant de choisir un nouvel identifiant matériel. Une extension `.32x` chargée comme Mega Drive ne vaut pas support 32X.
2. `Buses/MegaDriveCpuBus.cs`, `CartridgeAddressSpace` et les raccordements mémoire : ajouter le décodage de l'extension, l'activation, les vues ROM/framebuffer et les latences ; vérifier également les chemins DMA du VDP et les accès via Z80.
3. `Scheduling/MegaDriveFrameScheduler.cs` et le scheduler Mega-CD : introduire des échéances communes pour le 68000, les deux SH-2, le DMA, le PWM et la vidéo. Garder des compteurs séparés et rendre le matériel absent sans coût de travail 32X sur les autres machines.
4. `Boards/SegaBoard.cs` et les sorties vidéo/audio : réutiliser les services de sortie et de rééchantillonnage ; compléter les points de raccordement pour la vidéo composite et les événements 32X.
5. `State/SegaBoard.State.cs` : versionner un contexte machine incluant CPU, caches, DMA, SDRAM, framebuffer, palette, FIFO, IRQ, banques et échéances. Un état qui ne restaure que les registres visibles ne permet pas une reprise déterministe.
6. `FirmwareConstants.cs`, options et services de contenu : déclarer les ressources propres à SegaEmu et utiliser les services communs Sega/Emulation. Ne faire aucun appel aux contrats ou constantes des adaptateurs PicoDrive, ares ou GenesisPlusGXCS.
7. Les entrées restent celles de la Mega Drive et de ses ports : raccorder les manettes, souris, clavier configuré et autres périphériques par les contrats existants, sans créer un chemin d'entrées parallèle pour le 32X.

Le cœur SH-2 doit être utilisable sans une Mega Drive. Les composants de bus et de coordination doivent porter les particularités Sega. Le code ares mêlant `Node`/`Thread` à l'adaptation 32X et les globaux PicoDrive ne sont donc pas des unités à recopier telles quelles dans les puces réutilisables.

## Ordre conseillé et validations futures

1. Fabriquer le SH-2 et son contexte, puis les périphériques internes du SoC ; tester instructions, delay slots, exceptions, cache/RAM et DMA sur mémoire synthétique.
2. Fabriquer les registres, fenêtres et arbitrages 32X ; vérifier les deux vues CPU, les tailles d'accès, reset/activation, communication, DREQ et interruptions sans interface graphique.
3. Fabriquer et tester le VDP 32X et le PWM séparément, puis leur horodatage et leur mélange avec les sorties existantes.
4. Raccorder une machine 32X complète et observer le démarrage 68000/master/slave, puis tester plusieurs jeux et les états sauvegardés. Le Mega-CD + 32X doit utiliser la même extension une fois celle-ci validée.
5. Optimiser seulement ensuite : accès mémoire spécialisés, exécution par blocs et réveil des boucles d'attente sur écriture/IRQ/DMA. Garder une route de référence pour comparer chaque optimisation sur le même travail.

Les [sources testpico de notaz](https://github.com/notaz/megadrive/tree/master/testpico) constituent une référence de tests matériels, notamment `main.c`, `sh2_main.c` et `sh2_test.sh2`. Leur révision est enregistrée dans le manifeste ; elles n'ont été ni téléchargées comme ROM, ni construites, ni exécutées ici. Les jeux présents sous `G:/Retro/Cartouches/Sega/32X` fournissent aussi de futurs scénarios ; ce dossier contient des extensions diverses, et les noms `[b1]` ou autres variantes ne doivent pas être choisis par défaut comme témoins.

Pour les futures mesures, distinguer explicitement cœur CPU seul, machine complète sans limiteur et application complète. Comparer Debug à Debug et Release à Release, avec les mêmes ROM, entrées et nombres de cycles/trames. Publier FPS, temps CPU, durée, configuration, route et contrôles vidéo/audio/état. Aucun FPS ni gain de vitesse 32X n'est déduit de cette recherche de sources.

Pour commencer le code, **ares et jgenesis sont les références les plus utiles au découpage**, et **PicoDrive à l'étude des chemins de synchronisation et des optimisations**. BlastEm apporte une autre implantation indépendante et un modèle SH7095 explicite ; MAME fournit un recoupement supplémentaire des registres. Cette recommandation porte sur le contenu inspecté et l'organisation du futur portage, pas sur un classement mesuré de leurs performances.

## Confrontation des chemins rapides pendant le portage

La date des sources ne sert pas de classement. Les choix suivants sont verifies dans les fichiers collectes ; aucun chiffre de vitesse de ces emulateurs n est extrapole a SegaEmu.

| Reference | Execution et synchronisation effectivement inspectees | Consequence pour SegaEmu |
| --- | --- | --- |
| Gens / Gens-GS, coeur SH2 ASM 1.60 de 2002 | sh2a.asm : opcode lu directement depuis le PC hote, saut dans OP_Table, budget dans EDI ; IRQ par sortie forcee et watchdog/FRT au retour SH2_Exec. cpu_sh2.c declare les plages de fetch ROM, SDRAM et cache-RAM directement. g_32x.cpp alterne les deux SH-2, le 68000 et le PWM par portions de ligne. | Supprimer les callbacks generiques et les recalculs de peripheriques du chemin de chaque instruction ; conserver les horloges aux acces temporels et aux frontieres utiles. |
| PicoDrive | Interpreteur et backend DRC ; memory.c detecte le polling et reveille sur evenement ; sh2.c contient les raccourcis DT/BF et BRA/NOP. | Adapter ces raccourcis et leurs reveils ; conserver une execution de reference pour les comparaisons. |
| ares | sh2.cpp active un recompiler selon Accuracy::Recompiler et alloue son code executable ; semantiques des instructions separees. | Le portage des seules semantiques et du decodeur ne reprend pas ce chemin rapide. |
| jgenesis | sh2-emu/src/lib.rs execute les instructions par lots et expose tick_peripherals separement pour watchdog et SCI. | La puce ne doit pas recalculer tous ses peripheriques a chaque operation interne. |
| BlastEm | sh2.cpu decrit un dispatch genere ; sh2_run borne l execution, traite SLEEP jusqu a int_cycle puis appelle periph_run. sh7095_run avance les peripheriques depuis leur dernier horodatage. | Mettre a jour le temps ecoule et les echeances, sans une boucle de tous les peripheriques par micro-operation CPU. |
| MAME | sh2.cpp selectionne execute_run_drc ou la boucle interpretee ; ses machines 32X sont encore marquees non fonctionnelles dans la revision collectee. | Recouper les registres et les exceptions ; ne pas en deduire une validation des jeux. |
| Kega Fusion | Comportements et support documentes ; aucune source interne obtenue dans cette collecte. | Aucun choix interne de son SH-2 ne peut etre presente comme inspecte ou porte. |

Les huit sources Gens sont conservees dans artifacts/References/Machines/Sega/32X/Gens avec leur blob Git, URL et SHA-256. Les huit blobs ont ete verifies octet pour octet. Le manuel Gens indique un minimum de 800 MHz pour le 32X et rapporte le plein debit sur un Duron 1 GHz, en 320 x 240 avec audio 44,1 kHz : il ne faut donc pas supposer que le 32X exige un PC moderne. Ce temoignage ne constitue pas une mesure realisee ici.

Sources Gens : [execution ASM](https://github.com/lutris/gens/blob/master/src/gens/gens_core/cpu/sh2/sh2a.asm), [plages de fetch](https://github.com/lutris/gens/blob/master/src/gens/gens_core/cpu/sh2/cpu_sh2.c), [ordonnanceur 32X](https://github.com/lutris/gens/blob/master/src/gens/emulator/g_32x.cpp), [manuel](https://github.com/lutris/gens/blob/master/doc/gens.txt).


## Stabilisation apres les regressions du 15 novembre 2026

Knuckles' Chaotix est un jeu 32X utilise comme scenario de mesure ; ce nom ne designe ni un composant ni un nouvel emulateur.

Les changements de tables partages du 68000 et du VDP ont ete retires : Cpu68000.Tables.cs, Vdp.Tables.cs et Vdp.cs retrouvent exactement leur contenu HEAD. Leur modification n'avait donne aucun gain mesure et etendait inutilement le perimetre aux autres machines. Les raccordements 32X de Vdp.Output.cs sont conserves.

Apres ce retrait : compilation ciblee Chips Debug reussie ; 26 tests puces et 26 tests d'integration machine reussis, aucun echec. Journal : artifacts/Emulation/segaemu-32x/stabilisation-tests-Debug.log. Ces tests ne prouvent ni un gain de vitesse ni le fonctionnement de tous les jeux. Aucun nouveau resultat FPS n'est attribue a ce retrait.

Les dernieres mesures precedant ce retrait n'atteignent pas l'objectif : 73,54 FPS Release et 12,72 FPS Debug sur 600 trames de Knuckles' Chaotix pour le runtime direct, contre un ancien passage Release a 89,17 FPS. Les versions intermediaires plus lentes ne constituent pas des ameliorations. La performance du SH-2 et le fonctionnement en jeu du CD32X restent a valider ; le travail 32X n'est pas termine.


## Correction groupee des acces et de l'ordonnancement

Sources locales utilisees, sans nouvelle collecte :

| Implementation | Organisation observee | Rapprochement |
|---|---|---|
| Gens/GS | cpu_sh2.c mappe directement les plages de fetch ROM, SDRAM, cache-RAM ; sh2a.asm conserve PC et budget dans des registres x86. | Chemin direct, different du modele complet de cache ares. |
| PicoDrive | Tables read8/read16/read32 avec acces direct ou gestionnaire ; interpreteur et DRC ; memory.c utilise un correctif WWF Raw pour certains effets de cache. | Proche de Gens pour les acces directs ; le DRC ajoute une traduction des blocs. |
| ares | Cache::read gere lignes, tags et remplacement ; lectures directes mot/long et remplissage par quatre longs ; moteur interprete ou recompile. | Cache materiel modelise. |
| jgenesis | cache.rs gere quatre voies, pseudo-LRU et write-through ; instructions.rs utilise une table de 4096 fonctions. | Proche d'ares pour le cache, decodeur different. |
| BlastEm | sh2_util.c gere tags/LRU et remplissage burst_read ; sh2.cpu genere l'execution et precharge un ou deux opcodes par lectures 16/32 bits. | Proche d'ares/jgenesis pour le cache ; moteur genere distinct. |
| MAME | sh2.cpp utilise les acces rapides du moteur memoire pour les opcodes et selectionne interpreteur ou DRC. Le cache logiciel du moteur memoire n'est pas une preuve de simulation du cache materiel SH-2. | Distinction necessaire entre cache d'acces hote et cache materiel ; ne pas le classer automatiquement avec ares. |
| Kega Fusion | Sources internes non obtenues. | Aucun fonctionnement interne attribue. |

Modifications effectives : SH7095 lit les mots/longs d'une ligne directement, remplit par quatre lectures longues sans activer le polling des donnees, synchronise une seule fois les lectures IO de largeur 16/32 bits et isole le finally DMA. Le cache, les deux voies optionnelles, les IRQ et les peripheriques sont conserves. Le choix de remplacement circulaire preexistant reste different du pseudo-LRU des sources : ce point n'est pas pretendu porte.

Mega32X.Scheduling reprend le quantum Gens p_i=84 cycles 68000 (588 horloges maitre, 252 SH-2), borne aussi les changements HBlank/ligne et conserve les echeances du SoC. Cela ne reprend pas encore les plages de fetch directes Gens dans le processeur ; le runtime genere actuel n'est pas son interpreteur ASM porte integralement.

Mesure du meme passage initial de Knuckles' Chaotix, 600 trames sans limiteur : Debug avant 12,9306 FPS (46,4016 s), apres 18,9726 FPS (31,6246 s), soit +46,73 %. Release apres : 76,1237 FPS (7,8819 s). La valeur Release anterieure du runtime direct etait 73,54 FPS ; elle n'a pas ete remesuree avant cet ensemble. Le meilleur ancien passage Release 89,17 FPS n'est donc toujours pas retrouve.

Image finale SHA256 identique avant/apres en Debug et apres en Release : E3628F9A4E69D4B76ABF0CD3C4535838A53AB71F6FA0D955B32D1076D0454991. Audio non nul : 882762 echantillons dans les trois passages. Reprise etat/video exacte ; premier bloc audio restaure different, deja observe avant cette correction. Cela ne constitue ni une validation de tout le jeu ni du CD32X.

Validation groupee : 28 tests puces et 26 tests machine reussis en Debug, puis les memes en Release, aucun echec. Les deux nouveaux tests verifient mots/longs de cache aux frontieres, masques code/donnees et equivalence du runtime natif avec l'interpreteur sur interruptions/reprise. Journaux source-alignment-* sous artifacts/Emulation/segaemu-32x. Aucun commit ni publication. La demande de retrouver des performances satisfaisantes et de valider le CD32X reste non terminee.


## Fetch directement mappe

Gens cpu_sh2.c SH2_Set_Fetch_Reg a servi pour la separation plages directement lisibles / gestionnaires. Le bus SH-2 expose maintenant des plages de memoire empruntee pour le fetch ; le SoC fournit cache-RAM et lignes presentes, et le bus 32X fournit SDRAM et ROM de boot. Les misses restent traites par le cache ; les changements de tags, purges, CCR, reset et restauration invalident les plages. Aucune instruction ni fonction de peripherique n'a ete retiree. Le mapping ROM cartouche avec representation propre au chargeur n'est pas encore porte en plage directe.

Sur les memes 600 trames : Debug 19,7022 FPS (30,4534 s), Release 90,7911 FPS (6,6086 s). Image SHA256 et nombre d'echantillons non nuls identiques aux passages precedents ; reprise etat/video exacte, premier bloc audio restaure toujours different. 28 tests puces et 26 tests machine passent dans chaque configuration. Journaux mapped-fetch-* sous artifacts/Emulation/segaemu-32x. Ces mesures couvrent ce passage de demarrage, sans validation de tout le jeu ni du CD32X. La vitesse demandee n'est pas atteinte et la reparation n'est pas declaree complete.


## Integration parallele et rejet des candidats

Trois agents ont travaille sur des perimetres distincts : bus direct SegaEmu, boucle/mapping SH-2, comparaison des variantes Gens. Le rapport detaille est SegaEmu-32X-variantes.md. Sur les fichiers compares, 277 handlers et quatre macros sont identiques dans GS/Rerecording/Plus ; le moteur ASM de Rerecording et Plus est integralement identique apres normalisation. Les variantes ne sont donc pas ecartees par principe, mais aucun autre moteur plus rapide n'y a ete trouve. Gens/GS ajoute un traitement DREQ1/PWM absent des chemins compares des deux autres variantes. Les sources utiles sont conservees sous artifacts/References/Machines/Sega/32X/GensVariants avec leurs index. Gens/GS II, dont EmuContextFactory n'accepte que MD/Pico dans la revision consultee, n'est pas presente comme un autre moteur 32X fonctionnel.

Le candidat bus ROM empruntee/little-endian et Span local a ete mesure a 10,4474 FPS Debug sur Chaotix. Apres retrait du Span local : 19,1907 FPS Debug, 86,0786 FPS Release, contre l'etat conserve 19,7022 et 90,7911. Aucune cause precise de cette difference n'est etablie. La variante complete a ete retiree des fichiers de production des deux agents, sans restaurer HEAD ni effacer les corrections precedentes. Aucun gain de cette variante n'est revendique.

Le candidat rejeté a aussi execute 600 trames de Doom et Virtua Racing en Debug et Release, avec reprise etat/video exacte. Les images correspondent entre configurations. Ses mesures et logs parallel-* sont conserves comme resultats de candidat, pas comme mesures du code finalement conserve. Le premier bloc audio restaure differe toujours.

Apres retrait : compilation ciblee Chips/Sega reussie dans les deux configurations ; 30 tests puces et 26 tests machine reussis en Debug, puis les memes en Release, zero echec. Deux nouveaux cas permanents controlent changement de mapping et code automodifie dans la meme tranche, interpreteur et runtime natif. Journaux retained-tests-*.log. Aucun nouveau benchmark du contenu identique restaure n'a ete ajoute : les chiffres de l'etat conserve sont ceux du passage mapped-fetch-* anterieur. Une capture de cet etat est mapped-fetch-retained.png. L'application complete n'a pas ete reconstruite ni ses DLL distribuees remplacees.

Le delai demande n'a pas permis d'obtenir la reparation complete : vitesse encore insuffisante, CD32X non valide en jeu, reprise du premier bloc audio non exacte. Aucun commit/push.


## Controle Debug demande — MD/MCD sans 32X en priorite

Les documents SegaEmu-audit.md (bilan de refonte) indiquaient en machine seule 29,0 FPS pour Aladdin MD et 27,0 FPS pour Batman Returns MCD ; avec publication par l’adaptateur, 28,4 et 33,2 FPS. Les nouvelles mesures sont celles de la machine seule : ne pas melanger les deux colonnes. Le banc historique restaurait un etat pour sa mesure machine seule ; le controle actuel part directement d’une machine neuve et chauffe 60 trames. Ces valeurs historiques restent donc des reperes, pas une mesure A/B exacte avec le nouveau banc.

Pour isoler les changements recents, les DLL Debug conservees avant les nouvelles routes ont aussi ete reexecutees avec exactement le meme banc que les DLL actuelles. Cette reference binaire contient deja le 32X ; elle ne remplace pas la reference historique avant ajout du 32X. Deux passages par cas (un par version), aucun mediana ni gain statistiquement etabli. Chronometrage uniquement de RunFrame, rendu et generation audio actifs, sans fenetre, peripherique audio ni limiteur. MD/MCD : 60 trames mesurees apres 60 de chauffe, sans entree. 32X : 600 trames depuis le demarrage, sans chauffe ni entree sur cette fenetre. Aucun test SG-1000/SMS/Game Gear attribue au SH-2.

| Machine / jeu | DLL conservees, FPS | Code retenu, FPS | Ecart observe | Image finale identique | Etat sauvegarde identique octet par octet |
| --- | ---: | ---: | ---: | --- | --- |
| Mega Drive — Aladdin | 29.38 | 30.07 | +2.36 % | oui | oui |
| Mega-CD — Batman Returns | 31.22 | 32.93 | +5.50 % | oui | oui |
| 32X — Knuckles’ Chaotix | 18.09 | 17.30 | -4.35 % | oui | oui |
| 32X — Doom | 19.94 | 18.84 | -5.53 % | oui | oui |
| 32X — Virtua Racing Deluxe | 18.80 | 19.01 | +1.13 % | oui | oui |

MD/MCD sans 32X : aucune perte de vitesse observee sur ces deux passages. Les images et etats complets correspondent aux DLL conservees. Les differences de hachage avec l’ancien rapport ne sont pas utilisees pour conclure : celui-ci ne hachait pas necessairement le meme tampon et le meme parcours de restauration. Cela ne valide pas tous les jeux ni tous les reglages.

### Regression detectee et corrigee sur la route par defaut

Virtua Racing avait chute a 11,02 FPS avec les deux DLL actuelles, contre 18,80 FPS avec les deux DLL conservees. Les assemblages de diagnostic ancien Chips + Sega actuel (19,82) et Chips actuel + ancien Sega (19,70) ont isole une interaction entre les modifications ; ces montages ne constituent pas une distribution. Le seul retrait de l’attribut AggressiveInlining ajoute a Sh2.step retablit 19,01 FPS. L’image, le nombre d’echantillons non nuls et l’etat sauvegarde restent identiques. Le runtime copie propageait cet attribut sur un helper de 98 octets IL qui n’etait pas force inline avant. Ce changement touchait donc Switch meme avec les blocs desactives. La nature precise du mauvais code machine (registres, taille ou autres) n’a pas ete etablie.

Chaotix et Doom ont ete remesures apres ce retrait : leurs ecarts sont affiches sans les masquer. Les mesures precedant le retrait etaient 19,78 FPS pour Chaotix et 18,95 pour Doom. Le retrait corrige la forte perte Virtua ; il ne constitue pas un gain uniforme sur les jeux. Les routes blocs et memoire directe demeurent desactivees par defaut.

45 tests de puces et 28 tests machine passent en Debug apres correction, aucun echec. La reprise video est exacte sur les cinq cas. Le controle de reprise binaire MCD echoue avec les deux versions ; les quatre autres passent. Le premier bloc audio restaure differe avec les deux versions (888/887 echantillons stereo sur MD/MCD, 736/735 sur ces passages 32X). Cela ne provient pas d’un acces exclusif au peripherique audio : le banc ne l’ouvre pas. Ce controle ne prouve pas la correction generale des sauvegardes ou du mixeur.

Releves : artifacts/Emulation/segaemu-32x/check-{baseline,current,isolate-old-*}-*-Debug.json et check-clock-inline-*-Debug.json ; journaux associes. Chaque JSON indique les SHA256 des DLL effectivement chargees. MD/MCD ne sont pas remesures apres le retrait d’un attribut du SH-2, CPU qu’ils n’utilisent pas sans 32X. Aucune nouvelle mesure Release dans ce controle demande en Debug. Aucun commit, aucun changement des ROM ni des DLL de la distribution de l’application.


### Retrait des modifications de tables a la demande utilisateur

Le controle Release MD/MCD a releve 561,27 vers 494,75 FPS pour Aladdin et 308,02 vers 294,30 FPS pour Batman Returns avec le candidat de constantes. Le candidat a ete retire : Cpu68000.Tables.cs et Vdp.Tables.cs sont exactement identiques aux versions anterieures, blobs Git 1644595dedc757d59487c52d99123316140ef800 et ba86131842f1e5c70088359adda9f1d2c361b2ed. Les tableaux precedents de ce controle decrivent les versions intermediaires mesurees, pas une certification des performances du code final. Les derniers fichiers check-retained restent des releves bruts ; aucune conclusion de vitesse retrouvee n’est annoncee. L’utilisateur demande l’arret des modifications et des essais : aucun nouvel essai ni optimisation lance apres cette demande. Aucun commit.


## Retest demande : deux versions, Debug puis Release

La premiere relance a ete interrompue et archivee sous invalid-incremental-retest-*. Les fichiers de tables restaures portaient des dates anciennes et le build incremental avait reutilise les DLL precedentes. Ces releves ainsi que check-retained-* ne valident pas les sources restaurees. Reconstruction forcee --no-incremental de Chips avant le retest ci-dessous : SHA256 Debug 2A58E37658310B014CE5CBDCDCAAE8436596705F8B777B942358618058369793 ; Release 514203E6258D0C231B529416DF7D36A88EFCC742195F0EB7B9B96218E7699B94. Les DLL chargees sont verifiees avant chaque passage. Aucun code de production modifie pendant ce retest.

Reference : DLL conservees avant les nouvelles routes, contenant deja le 32X. Actuel : sources avec tables 68000/VDP restaurees et retrait de l’inlining force de Sh2.step. Ce n’est pas une comparaison avec GenesisPlusGXCS ni avec le SegaEmu d’avant tout ajout 32X. Vingt passages sequentiels, un par version/jeu/configuration : reference puis actuel pour chaque jeu, Debug entier puis Release entier. Aucun resultat n’est une mediane.

Machine complete avec generation audio/video, sans fenetre ni limiteur ; FPS calcules sur RunFrame uniquement. MD/MCD : 60 trames de chauffe puis 60 mesurees en Debug ou 900 en Release. 32X : 600 trames depuis demarrage dans les deux configurations. Meme ROM, BIOS et entrees pour les deux versions. Le mode repeat-start emet cinq trames Start apres la trame 900 : aucun Start sur les passages Debug et 32X, un pulse sur MD/MCD Release. Le temps CPU de retest-executions.json inclut initialisation et controles : ce n’est pas le seul temps CPU de la fenetre mesuree.

| Machine / jeu | Debug : reference → actuel | Release : reference → actuel |
| --- | ---: | ---: |
| Mega Drive - Aladdin | 24.11 → 23.28 (-3.46 %) | 612.79 → 607.32 (-0.89 %) |
| Mega-CD - Batman Returns | 26.68 → 23.68 (-11.23 %) | 316.55 → 252.64 (-20.19 %) |
| 32X - Knuckles Chaotix | 15.31 → 15.37 (+0.37 %) | 67.92 → 66.03 (-2.79 %) |
| 32X - Doom | 16.12 → 14.22 (-11.82 %) | 62.23 → 35.86 (-42.38 %) |
| 32X - Virtua Racing Deluxe | 17.42 → 18.04 (+3.55 %) | 26.56 → 31.84 (+19.88 %) |

Pertes observees : Batman Returns -11,23 % Debug et -20,19 % Release ; Doom -11,82 % et -42,38 %. Aladdin : -3,46 % / -0,89 %. Chaotix : +0,37 % / -2,79 %. Virtua Racing : +3,55 % / +19,88 %. Ces passages uniques ne permettent ni une attribution de cause ni une conclusion generale sur tous les jeux. Les pertes restent affichees ; aucune nouvelle optimisation ou modification du code n’a ete effectuee pour les masquer.

Image et etat sauvegarde sont compares entre les deux versions au meme instant. Les colonnes de reprise comparent la trame suivant une sauvegarde avant/apres rechargement dans chaque version, ce qui est un autre controle.

| Machine / jeu | Configuration | Image finale ref/actuel identique | Etat sauvegarde ref/actuel identique | Reprise binaire ref / actuel | Reprise video ref / actuel | Reprise audio ref / actuel |
| --- | --- | --- | --- | --- | --- | --- |
| Mega Drive - Aladdin | Debug | True | True | True / True | True / True | False / False |
| Mega Drive - Aladdin | Release | True | True | True / True | True / True | False / False |
| Mega-CD - Batman Returns | Debug | True | True | False / False | True / True | False / False |
| Mega-CD - Batman Returns | Release | True | True | True / True | True / True | False / False |
| 32X - Knuckles Chaotix | Debug | True | True | True / True | True / True | False / False |
| 32X - Knuckles Chaotix | Release | True | True | True / True | True / True | False / False |
| 32X - Doom | Debug | True | True | True / True | True / True | False / False |
| 32X - Doom | Release | True | True | True / True | True / True | False / False |
| 32X - Virtua Racing Deluxe | Debug | True | True | True / True | True / True | False / False |
| 32X - Virtua Racing Deluxe | Release | True | True | True / True | True / True | False / False |

Les images finales et les etats sauvegardes correspondent dans les dix couples. Ce controle porte sur ces sequences de demarrage/attract, pas sur une partie complete. Les reprises audio restent non exactes ; les echecs binaires sont affiches. Le banc ne sollicite aucun peripherique audio. Chaque JSON conserve le SHA256 de l’etat sauvegarde pour permettre la suppression des fichiers temporaires.

Sources des releves : artifacts/Emulation/segaemu-32x/retest-{baseline,current}-*-{Debug,Release}.json et journaux associes ; retest-executions.json ; retest-force-build-*.log. Le banc et chaque processus sont liberes dans finally, et les DLL temporaires restaurees. Aucune ROM, aucun parametre utilisateur, aucune DLL de distribution de l’application modifies ; aucun commit.


## Tentative sur Vdp.Output abandonnee

Deux variantes ont ete essayees uniquement sur la sortie video, sans modification des CPU : suppression de RemapLineCore et conversion 32 bits par spans, puis retrait des spans en gardant le dispatch simplifie. La premiere a donne des gains Debug sur les trois passages, mais Batman Returns MCD Release perd 5,43 % (255,94 → 242,05 FPS), puis 16,73 % en ordre inverse (298,58 → 248,65 FPS). La simplification seule perd aussi sur le controle MCD : 24,62 → 23,98 FPS Debug et 308,75 → 288,61 Release (-6,52 %). Ces releves ponctuels ne prouvent pas une cause interne precise. Aucune des variantes n’est conservee et aucun gain n’est revendique.

Vdp.Output.cs et Vdp.Rendering.cs ont ete restaures octet par octet depuis les sauvegardes prises avant cette tentative, puis Chips a ete reconstruit avec --no-incremental en Debug et Release pour realigner DLL/PDB. Aucun troisieme candidat ni nouvelle mesure apres ce retour. Les raccordements 32X preexistants restent en place.

55 tests de puces et 28 tests d’integration ont passe dans chaque configuration pour les variantes testees. Les dix nouveaux tests VdpOutputTests.cs (tampons en memoire) restent : couleurs fixes, LCD, bords, entrelacement, profondeurs 8/15/16/32 et notification apres ecriture. Les images finales et etats sauvegardes correspondent entre versions sur les passages compares. Cela ne transforme pas une perte de vitesse en amelioration validee.

Journaux et releves video-output-* conserves ; chaque JSON contient le SHA256 de l’etat sauvegarde avant suppression des donnees brutes. Sources et DLL temporaires video-before et script VideoCompare.ps1 supprimes apres retour. Aucun commit ni mise a jour des DLL de distribution de l’application.
