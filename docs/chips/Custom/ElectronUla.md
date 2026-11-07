# ULA Acorn Electron — examen du circuit multifonctions

## État actuel consolidé — 4 novembre 2026

L’ULA Electron est validée et committée dans les limites documentaires explicites ci-dessous. Le build Debug de livraison avec les 24 modules est vérifié. Les paragraphes d’historique décrivent des états successifs ; leurs anciennes listes de manques et anciens nombres de tests ne constituent pas l’état actuel.

### Fonctions réalisées et vérifications actuelles

Les fichiers de tests cités ci-dessous sont sous `tests/RetroMediaLab.Chips.Tests/Custom/Acorn/Electron/ULA/`, sauf les contrôles natifs dans `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuElectronTests.cs`.

| Fonction | État réalisé et éléments vérifiés |
|---|---|
| Registres FE00–FE0F | Miroirs, réservés, POR lu une fois, cinq causes/masques IRQ indépendants, acquittements FE04/FE05, moteur/Caps Lock/mode et pagination interne. `RegisterTests`, `ResetTests`. |
| Accès CPU et NMI | Adresse présentée avant PHI, domaines 2/1 MHz, attente RAM selon les slots vidéo, priorité mémorisée NMI et acquittement FE05 bit 7, maintien du reste du circuit pendant l’attente. `BusTests`, `CpuClockTests`, contrôles natifs. |
| Broches DRAM | Multiplexage des adresses et quatre lignes de données, deux groupes pair/impair réellement pris à leurs strobes, écritures partielles, annulation et maintien des écritures déjà strobbées. `RamAddressTests`, `RamDataTests`, `RamSequencerTests`, `RamIntegrationTests`. |
| Transactions de fond et rafraîchissement | Transactions conservées hors pixels visibles, maintien de l’adresse d’un transfert pendant les changements de mode, couverture des 128 lignes DRAM dans deux millisecondes par la vidéo. `RamBackgroundTests`, `RamRefreshTests`. |
| Temps vidéo et IRQ | Champs alternés 312/313 lignes, phase RTC indépendante du champ, DEND selon le mode, niveaux numériques de synchronisation comparés aux acquisitions décrites dans l’historique. `TimingTests`, `VideoTimingTests`. |
| Adressage et pixels | Huit sélections de mode, plans de bits et répétitions pour tous les octets, chaque fil de palette, changements au point suivant, lignes de texte cachées, prélecture, start latched et wrap ; alias bas documentés du mode 0. `MemoryTests`, `PixelTests`, `LowStartTests`, `FrameStorageTests`. |
| Périodes alignées | Les huit actions RAM/vidéo sont ordonnées explicitement ; le chemin par points conserve les mêmes actions. Comparaison des champs dans les huit modes alignés/désalignés ; 48 cas comparent aussi les broches, PHI, couleur et effets secondaires de changements de mode/annulations en cours de strobe. `ClockSequenceTests` et `PixelTests`. |
| Son numérique | Compteur partagé, tous les diviseurs, période nominale PD+1, maintien de phase et du latch selon les changements de mode. `SoundTests`, `CommsCounterTests`. Aucune fidélité analogique du haut-parleur n’est déduite de ces tests. |
| Tape numérique | Émission start/données LSB/stop et porteuses, rechargement FE04 sans file d’attente, octet reçu visible pendant son décalage, prise au milieu du dernier bit, demi-stop/stop bas, causes IRQ et maintien des états selon les modes ; transferts des 256 valeurs entre broches et retours PCM natifs. `TapeTests` et ses fichiers partiels, `CommsCounterTests`, contrôles natifs. |
| Entrée stable et horloges croisées | Continuation nominale sans média/front, durée longue saturée, inhibition du mode inutilisé, trames TX et RX cadencées par les modes son/sortie sans inventer une réception immédiate à FE07. Tests `QuietInput`, `ModeClocks` et compteur. Les équations physiques restant incertaines sont distinguées ci-dessous. |

Dernier passage du code retenu : **2 364 tests de la bibliothèque Chips et 25 contrôles natifs Electron réussis dans chacune des configurations Debug et Release**, aucun ignoré. La suite Chips comprend les **291 contrôles propres à l’ULA**, ainsi que les contrôles du 6502 et de son contrat de présentation précoce du bus. Ce total ne remplace pas la portée précise de chaque famille de tests.

### Preuves invitées conservées hors Git

Les captures sont dans `artifacts/TestsChips/ElectronUla/AcornElectron/` et ne sont pas indexées.

- Les captures `final-selector-0.png` à `final-selector-7.png` proviennent d’une ROM de banc 6502 qui exécute ses contrôles puis dessine dans la RAM vidéo ; leur portée et leurs empreintes sont consignées dans l’historique.
- `comms-counter.png` et `comms-counter-fail.png` montrent deux chemins réellement exécutés par une ROM de banc : A5/étapes 1–4 après les contrôles ou E0/aucune étape quand l’attente POR est volontairement fausse. SOUND PASS y désigne l’horloge de réception en mode sonore.
- `mos-tape-boot.png`, `mos-tape-save.png`, `mos-tape-load.png` montrent le MOS/BASIC Electron existant, *SAVE puis *LOAD via les signaux tape PCM. Les 256 octets sont comparés par l’hôte et BASIC recalcule leur somme avant d’afficher TAPE PASS. Le firmware et les données sont identifiés par empreinte dans l’historique ; aucun média/configuration utilisateur n’a été modifié.

Les captures historiques ci-dessus précèdent le dernier ordonnancement direct des phases. Les trois captures `final-mos-boot.png`, `final-mos-save.png` et `final-mos-load.png` sont renouvelées après les dernières modifications du compteur et du calcul temporel : le MOS effectue SAVE/LOAD via PCM, les 256 octets sont vérifiés et BASIC affiche TAPE PASS après calcul de leur somme. Le passage et ses empreintes sont détaillés en fin de document. Ce test natif ne lance aucune fenêtre WPF ni sortie audio Windows.

### Performances retenues et points ouverts

Mesures retenues : chauffe de trois secondes par chemin et trois séries de quarante champs. Ce tableau regroupe les derniers passages de chaque chemin ; ce ne sont pas des mesures appariées simultanées. Les comparaisons avant/après et leurs empreintes sont détaillées dans l’historique :

| Chemin | Debug | Release |
|---|---:|---:|
| Vidéo mode 0 | 18,215 ms/champ | 1,895 ms/champ |
| Vidéo mode 4 | 17,002 ms/champ | 1,817 ms/champ |
| Carte native, boucle ROM | 25,187 ms/champ | 3,325 ms/champ |

Le gain de l’ordonnancement direct est établi sur les trois chemins dans son passage comparatif. Les dernières valeurs ci-dessus comprennent la représentation des latches et le rendu à couleur unique. Leurs bancs appariés sont décrits en fin de document ; les variations hôte interdisent de sommer les pourcentages des passages successifs. Le cache des colonnes, le regroupement précédent de l’étape maître, l’essai de blanking et les tables de permutations sans gain global sont retirés. Aucun réglage Optimize/Debug n’est utilisé pour masquer leur coût.

La cadence de la carte complète **n’atteint pas encore 20 ms/champ en Debug**. Les coûts de banc ne sont pas assimilés aux FPS de présentation de l’application ni à un débit universel pour tous les programmes.

La reproduction électrique porte par porte de CASSCK/S15 et des portes barrées du compteur partagé n’est pas établie par les sources récupérées. La réception et le High Tone utilisent un modèle numérique nominal documenté, notamment pour l’entrée sans front et la qualification de porteuse ; l’aller-retour du MOS ne prouve pas tous les comportements analogiques ou transitoires. Les temps de propagation et les cas de fronts simultanés non caractérisés ne sont pas présentés comme mesurés. Ces limites documentaires ne sont pas remplacées par une demande de mesures à l’utilisateur.

## Historique des examens et passages

## Périmètre et références examinées

Le composant est `Custom/Acorn/Electron/ULA` dans Chips ; son propriétaire est `ElectronMachine` dans Emulation.Acorn. L’examen inclut le circuit entier : bus et arbitrage RAM, registres, ROM/clavier, interruptions, vidéo, audio et tape. Il ne concerne pas uniquement `ElectronVideo`.

Références :

- [Acorn Electron Service Manual, issue 2](https://www.retro-kit.co.uk/user/custom/Acorn/8bit/Electron/manuals/Acorn_ElectronSM.pdf), sections 5 et 6, schéma et diagramme fonctionnel : description constructeur des broches, horloges et mémoire.
- [Acorn Application Note 015, issue 2, 7 juillet 1992](https://8bs.com/submit/acornappspdf/015.pdf), sections 2 et 4 : signaux du bus d’extension, transition 2/1 MHz, allongement de PHI, rôle de NMI et IRQ.
- [MAME electron_ula.cpp](https://github.com/mamedev/mame/blob/master/src/mame/acorn/electron_ula.cpp) : comparaison des registres et du décodeur ; cette source indique elle-même des limites sur tape en sortie, entrelacement, timing des interruptions et neige vidéo. Elle ne constitue pas une preuve de ces fonctions.
- [Elkulator ula.c](https://github.com/dmcoles/elkulator/blob/master/src/ula.c) et [6502.c](https://github.com/dmcoles/elkulator/blob/master/src/6502.c) : pagination, vidéo et accès mémoire ; champs alternés de 312/313 lignes. Les approximations audio et tape doivent être distinguées des mesures du circuit.
- [ElectronFpga, ElectronULA.vhd](https://github.com/hoglet67/ElectronFpga/blob/master/src/common/ElectronULA.vhd) et ses traces `timings/ferranti_ula`/`synertek_ula` : reconstruction matérielle et mesures disponibles. Ce circuit ajoute aussi des modes VGA, turbo et extensions ; ces ajouts ne doivent pas être attribués à l’ULA d’origine. Le commentaire sur le compteur RTC indique des pertes de cycles propres à ce modèle FPGA : sa constante ne sera pas recopiée comme fréquence constructeur.

Le miroir Stardot du guide avancé est protégé par un défi web. Une copie a ensuite été récupérée sur [le miroir PA3BYB](https://www.pa3byb.nl/wp-content/uploads/2017/12/Advance_user_guide_Acorn_electron.pdf), sections 14.1 et 15, et son texte extrait. Les [schémas Ferranti originaux de 1982, rescannés par Harry Barman](https://www.dropbox.com/s/xmr9h7srqpucj8l/elk_ula_scan_2017_10_17.pdf?dl=0) ont aussi été récupérés ; feuilles 9, 12 et 13 rendues et examinées. Les interprétations du forum ne remplacent pas ces feuilles.

## Broches et fonctions documentées

Le manuel décrit les entrées 16 MHz et division par 13, les adresses/données CPU, R/W, NMI, POR et BREAK ; sorties PHI OUT, RST, IRQ, sélection ROM, CAPS LOCK ; signaux RAM RA0–RA7, données RAM, RAS/CAS/WE ; quatre lignes clavier ; sorties RGB, HS et CSYNC ; tape CAS IN/CAS OUT/CAS RC/CAS MO ; sortie son.

La note Acorn précise un temps bas PHI nominal de 250 ns. Le temps haut vaut 250 ns en accès 2 MHz, 750 ou 1 250 ns lors d’une transition vers le domaine 1 MHz, selon sa phase, et peut être prolongé pendant la lecture vidéo. La fréquence hôte ne doit pas supprimer ces attentes. NMI permet l’accès RAM à 1 MHz quelle que soit la sélection vidéo ; il ne doit donc pas être traité uniquement comme une interruption CPU.

La sélection ROM interne utilise les pages 8/9 pour le clavier et 10/11 pour BASIC. Lorsqu’une page 8–11 est sélectionnée, une sélection 0–7 est ignorée ; une sélection 12–15 permet de sortir de ce groupe. Cette règle concorde dans MAME et la reconstruction FPGA. Les acquittements d’interruption partagent FE05 avec la sélection et doivent agir même quand le changement de page est ignoré.

Les registres palette sont actifs bas. Les bits de couleur sont répartis entre deux registres par groupe ; les bases logiques des paires sont 0, 4, 5 et 1. Les huit bits de chaque registre ne correspondent pas à huit couleurs consécutives.

## État du code initial

`ElectronUla` expose les registres, cinq sources IRQ masquées, le drapeau de mise sous tension acquitté à la première lecture de FE00, la pagination, moteur/LED, un oscillateur sonore et un sérialiseur tape 1 200 bauds. `ElectronVideo` charge les octets RAM et produit huit pixels par appel 2 MHz, avec palette et adresse de début. Le propriétaire ralentit certains accès mémoire dans `SynchronizeBus`.

Les tests actuellement présents sont dans LocalEmulationTests. Ils couvrent notamment FE00, le transmetteur, la pagination, une trame par mode et le moteur du magnétophone. Il n’existe pas encore de suite autonome de l’ULA dans Chips.Tests ; leurs résultats ne prouvent pas toutes les fonctions du circuit.

Écarts ou limites observables :

- PHI/RAS/CAS/WE, HS/CSYNC et les sorties par point ne sont pas exposés. La contention est calculée dans la machine ; NMI est fixé à false et ne participe pas à l’arbitrage.
- La vidéo utilise systématiquement 312 lignes, tandis que la référence Elkulator distingue 312/313. Il faut déterminer les phases exactes depuis les diagrammes/mesures avant modification, sans copier les modes ajoutés par le FPGA.
- `IElectronTapePins.InputBit` fournit déjà un résultat de démodulation FSK ; la détection des périodes analogiques n’appartient donc pas actuellement à cette ULA. Le débit est arrondi à 1 200 au lieu d’être dérivé de l’entrée division par 13 du manuel. Le rôle du diviseur FE06 dans tous les modes reste à établir.
- Le reset logiciel `Reset(false)` remet actuellement à zéro les sélections, masques, communication et oscillateur. Le constructeur distingue POR et RST ; les références logicielles/FPGA ne suffisent pas seules à déclarer ces effets de BREAK corrects ou incorrects. À vérifier avant changement.
- Les changements de mode sont appliqués à des groupes de huit points, et le mode texte ne remet pas immédiatement à zéro le bit VA3 lors du retour graphique décrit dans les commentaires de reconstruction des schémas. La phase du registre de pixels reste à établir.

## Vérifications à couvrir

Registres : toutes les adresses miroirs, bits réservés, masques indépendants des causes, acquittements FE00/FE04/FE05, pagination 16×16 et bits hauts indépendants, LED/moteur/communications, accès d’inspection sans effet si exposé.

Vidéo/mémoire : huit sélections de mode, tous les octets et entrées palette, start bas/haut, wrap, lignes espacées, changements de mode et palette en cours d’octet, registres de chargement, périodes/fenêtres des champs et interruptions, sélection CPU 2/1 MHz, accès bloqués et libérés, NMI, lignes RAM et refresh selon la documentation disponible.

Audio/tape : tous les diviseurs, écriture et changement de mode, phase/fréquence et silence ; démarrage/arrêt moteur, trames et bits série, disponibilité émission, réception/acquittement, détection de porteuse, erreurs de signal, comportement au reset. Vérifier les connexions au magnétophone existant en lecture et écriture plutôt que contourner le matériel par chargement direct.

Preuves : programmes exécutés dans l’Electron, captures effectives sous `artifacts/TestsChips/ElectronUla/AcornElectron/`, résultats précis et empreintes. Les bancs externes et copies de référence seront supprimés après usage. Mesures Debug/Release du circuit complet et de son raccordement, sans changement des optimisations de compilation du produit.

## État de validation

Les registres, la priorité NMI, le premier arbitrage CPU et le compteur vidéo/IRQ ont été repris et vérifiés ci-dessous. La puce entière reste en cours : les phases PHI/RAS/CAS/WE, le sérialiseur, le son, le modem tape et les preuves invitées ne sont pas clôturés. Les points indéterminés restent séparés des écarts prouvés ; un démarrage ne constitue pas une validation complète.

## Banc autonome des registres

Premier passage : **10/10 tests Debug réussis, 87 ms**. Les attentes contrôlent les miroirs FE00 et registres réservés, l’acquittement POR uniquement par FE00, les 256 masques IRQ avec les cinq causes actives, les acquittements FE05 y compris quand la pagination est ignorée, les 4 096 combinaisons page courante/octet écrit, les 256 valeurs de contrôle et leurs champs indépendants, ainsi que l’indépendance receive-full/transmit-empty lors de l’entrée en émission.

Les causes sont produites par le circuit et des broches tape synthétiques, sans réflexion sur ses champs privés ni ROM externe. Ces contrôles ne prouvent pas les timings vidéo/audio/série : ils établissent le comportement du banc de registres avant sa reprise. Aucun écart de registre n’est ressorti de ce passage.

Le même banc passe en Release : **10/10 contrôles, 83 ms**. La remise en forme de la déclaration ElectronUla et le retrait d’imports inutilisés dans ses interfaces ne modifient pas le comportement.

## Précisions établies après récupération des schémas originaux

La feuille 9 « Master timing and control » comporte la bascule croisée NMI/NMIB et NMICLEAR ; les feuilles 12/13 relient le décodage de FE05 et le bit DIX7 à son acquittement. Le guide avancé, p. 212–213, décrit le maintien de la priorité CPU en accès RAM après NMI et sa restitution à l’affichage par FE05 bit 7. La note Acorn 015 confirme l’accès RAM à 1 MHz malgré le mode affiché. Le code actuel ne possède ni cet état de priorité ni cet acquittement : il s’agit d’un manque établi, distinct de la seule entrée NMI du CPU.

Le séquencement exact de cette bascule doit conserver la distinction entre le niveau électrique NMI, sa mémorisation et le signal présenté au 6502. Ne pas remplacer ce circuit par une désactivation permanente de la contention lorsqu’un gestionnaire NMI a été appelé. L’arbitrage doit continuer à avancer vidéo/audio/tape pendant les attentes.

Le guide contient aussi des descriptions à confronter aux schémas : son texte de sortie tape indique « bit 7 first », contrairement aux conventions série des fichiers BBC et aux décodeurs comparés ; sa formule sonore doit être comparée au diviseur réel plutôt que copiée. Ces points ne sont donc pas encore employés comme attentes de tests. La réalisation vidéo/son/tape et les preuves invitées restent ouvertes.

## Décodage retenu pour la priorité et le domaine CPU

La feuille 9 distingue le niveau NMI externe et l’état mémorisé par les deux portes croisées NMI/NMIB. La feuille 13 décode l’écriture FE05 ; le chemin DIX7/FE05 commande NMICLEAR, également forcé par POR. La détection NMI du 6502 reste indépendante : un acquittement de priorité ne doit pas inventer un front sur sa broche. Le modèle conserve donc une entrée logique NMI active, une priorité RAM mémorisée après assertion, et l’acquittement FE05 bit 7. Un niveau NMI toujours actif après l’impulsion d’acquittement maintient la demande ; le retour inactif seul ne rend pas la RAM à l’affichage. Les délais analogiques et la simultanéité exacte set/clear dans la fenêtre de propagation ne sont pas caractérisés.

La note 015 donne 1 ou 1,5 µs au premier accès lent, soit deux ou trois périodes de référence 2 MHz, puis une période 1 MHz synchronisée. Le bus 6502 existant avance déjà une période par Tick avant Read/Write. Le modèle courant ne fait qu’aligner le compteur : une arrivée déjà alignée peut donc terminer en une seule période au lieu de trois. Le séquenceur d’accès doit ajouter au moins une période, attendre l’alignement du domaine 1 MHz et, en RAM haute bande passante sans priorité NMI, la fin des prélèvements vidéo. Les autres circuits continuent d’avancer pendant ces attentes.

Le décodage repris conserve RAM, clavier paginé 8/9 et bus externes FC/FD comme domaines ralentis ; les ROM restent à 2 MHz. Le domaine de FE est conservé tel qu’existant tant que ses portes de sélection d’horloge ne sont pas résolues : la source MAME elle-même désactive son ralentissement FE/FC/FD à cause d’une régression tape. Cette approximation de MAME ne doit pas servir d’attente matérielle.

Le contrôle POR couvre aussi explicitement une lecture FE04 avant la première lecture FE00 ; son résultat sera inclus au passage regroupé suivant.

## Premier raccordement réalisé : bus et priorité NMI

`ElectronUla.Bus.cs` expose le niveau logique NMI, la priorité RAM mémorisée, `BeginCpuAccess` et `CpuWaiting`. La période de référence 2 MHz est comptée dans l’ULA ; une demande lente ajoute au moins une période et attend le prochain alignement du domaine 1 MHz. La contention vidéo est consultée pendant l’attente, pas figée dans une durée calculée à l’avance : NMI peut donc la lever pendant que l’horloge continue. FE05 bit 7 acquitte la priorité indépendamment des masques IRQ et de la pagination. L’entrée NMI maintenue active conserve sa demande ; les cas set/clear simultanés dans les délais électriques restent non caractérisés.

`ElectronMachine.SynchronizeBus` ne contient plus de règle RAM/mode/phase dupliquée. Il présente l’adresse à l’ULA et avance la carte entière tant que l’attente est demandée. Le niveau NMI transmis au CPU provient de l’ULA ; son moteur n’a pas été modifié. La broche est disponible pour les périphériques d’extension futurs ; ceci n’ajoute pas une extension disque inexistante à la machine.

Premier passage regroupé Debug : **29 tests ULA réussis (81 ms)** et **17 tests Electron natifs réussis (419 ms)**. Le bus autonome vérifie RAM, clavier 8/9 versus BASIC 10/11, FC/FD, ROM pendant balayage, les huit sélections vidéo, lignes espacées, arrivée NMI pendant l’attente, acquittement et indépendance de l’IRQ. Les tests natifs mesurent 2/3 périodes de référence pour les accès lents et une pour la ROM ; ils retrouvent l’arrêt jusqu’à la fin du prélèvement vidéo après acquittement NMI. Le temps vidéo et le nombre d’appels tape avancent pendant ces attentes. FE04 est aussi lu avant FE00 dans le contrôle POR, qui reste valide.

Ces résultats ne clôturent pas le séquenceur complet PHI/RAS/CAS/WE, le domaine FE, la neige lors des collisions, la vidéo, l’audio ou le modem tape. Aucun résultat du banc de bus n’est présenté comme une preuve invitée de ces fonctions encore ouvertes.

### Passage Release et traces disponibles

Le bloc passe également en Release : **29 contrôles ULA (72 ms)** et **17 contrôles Electron (374 ms)**. Le test natif a ensuite été étendu aux écritures RAM : passage regroupé **17/17 Debug (381 ms) et Release (368 ms)**. Les écritures vérifient aussi le contenu RAM effectivement enregistré.

Les fichiers `timings/ferranti_ula/run_mode4.log` et `timings/synertek_ula/run_mode4.log` du projet ElectronFpga indiquent 625 lignes pour une image complète, avec interruptions sur les lignes 99 et 256. Les signaux exploités par l’analyseur sont CSYNC (bit 0), IRQ (bit 1) et bleu (bit 2), sans adresses, données CPU ni PHI. Ces traces ne prouvent donc pas les durées d’accès FE ni les sélections de phase du bus CPU.

Attention aux unités : `analyze.c` actuel imprime ses temps divisés par `UNITS=8`, mais les journaux publiés annoncent `frame length: 640000 (625 lines)`. Ces journaux correspondent donc à une autre échelle d’impression que le code actuel ; leurs nombres ne doivent pas être copiés directement dans le compteur 2 MHz. Les données binaires brutes doivent être examinées avant de fixer les phases d’interruption.

### Mesure indépendante des acquisitions binaires

Les deux acquisitions `run_mode4.bin` contiennent 4 194 304 échantillons. Une lecture indépendante détecte les fronts des trois bits, sans compiler ni exécuter le code externe. Les débuts de zone bleue après un intervalle bas prolongé sont espacés alternativement de **319 488 et 320 512 échantillons**, soit **312 et 313 périodes de ligne de 1 024 échantillons**. La somme est **640 000 échantillons pour 625 lignes**. Le changement d’échelle d’impression des journaux est ainsi contourné par les rapports bruts des signaux.

Dans l’acquisition Ferranti stabilisée, en prenant le front bleu 831 626 comme origine de la zone active, les quatre fronts IRQ suivants se trouvent à 101 361, 261 887, 421 361 et 581 375 échantillons. Depuis le champ suivant, origine 1 151 114, les valeurs sont 101 873, 261 887, 421 873 et 582 399. Les traces Synertek donnent les mêmes rapports, avec un écart d’un échantillon sur certains fronts IRQ. La période RTC est donc de 320 000 échantillons ; l’intervalle entre fins d’affichage alterne avec les longueurs des champs. Les positions relatives doivent conserver le décalage de demi-ligne entre champs.

Avec l’horloge 16 MHz documentée par Acorn et la période ligne de 64 µs, ces rapports donnent 20 ms pour RTC et 40 ms pour l’image complète, contrairement au champ fixe de 39 936 périodes 2 MHz du code initial. Les frontières IRQ comportent un décalage par rapport à une simple ligne entière ; les constantes actuelles « ligne 100 » et « ligne 256 » ne suffisent pas à reproduire les traces. Il faut reprendre le séquenceur vidéo et ses événements ensemble, pas corriger uniquement FramesPerSecond.

Les acquisitions ne comportent toujours ni PHI ni bus CPU : aucune conclusion nouvelle n’est tirée pour le domaine FE ou les strobes RAM. Les captures d’écran des diagnostics invités restent à faire après réalisation de l’ULA entière.

### Feuilles Ferranti 6–8 et acquisition en mode texte

Les trois feuilles ont été rendues lisiblement et examinées. La feuille 6 contient le chargement parallèle des huit bits, leurs décalages et multiplexages 1/2/4 bits par pixel, puis la palette et le masquage RGB. La feuille 7 relie les compteurs horizontaux/verticaux, les bascules de champ, le compteur raster et les événements RTC/DISPEND. VA3 est maintenu à zéro en mode graphique : un retour texte vers graphique doit donc effacer ce bit sans attendre une ligne. La feuille 8 sélectionne les adresses RAM CPU/vidéo et la commande d’écriture ; elle ne permet pas à elle seule de dater tous les fronts RAS/CAS. Ces fronts restent à confronter au contrôle de la feuille 9, sans les déduire des seules acquisitions vidéo.

Les captures brutes mode 6 comportent chacune 4 194 304 échantillons. Ferranti : origines bleues stables 704 896 puis 1 024 384 ; IRQ relatives respectives **101 361, 255 743, 421 361, 575 231**, puis **101 873, 255 743, 421 873, 576 256**. Synertek : origines 935 404 et 1 254 892 ; IRQ **101 360, 255 743, 421 360, 575 231**, puis **101 872, 255 743, 421 872, 576 255**. La fin d’affichage texte arrive 6 144 échantillons (six lignes) avant celle du mode 4. RTC ne change pas avec le mode.

En origine premier bleu actif, CSYNC normal descend à +766 et remonte à +830, période 1 024. Les impulsions verticales mesurées commencent à +287 728/+607 728 depuis le champ court, ou +288 240/+608 240 depuis le champ long ; leur largeur est 2 559/2 560 échantillons selon le front mesuré. Le modèle numérique nominal retient 2 560 points et des frontières entières de 16 MHz, sans prétendre reproduire les différences analogiques de propagation d’un échantillon.

Le nouveau séquenceur sera référencé au début de la première zone active (phase zéro conventionnelle, pas à une phase POR mesurée). Une image complète couvre 640 000 points, divisés en champs court 319 488 puis long 320 512. RTC est placé à 101 360 puis 421 360 ; DISPEND à 261 888 après chaque début de champ graphique, ou 255 744 en texte. Les fronts matériels observés diffèrent d’au plus un échantillon de ces positions nominales. Le décalage de phase RTC de 512 points dans le champ long découle du compteur global ; il ne doit pas être remplacé par deux IRQ sur une même ligne entière.

Le séquenceur sera d’abord vérifié indépendamment, puis raccordé aux prélèvements, au sérialiseur et aux interruptions. La sortie existante de groupes de huit pixels et les strobes RAM ne sont pas déclarés corrigés par la seule réalisation de ses compteurs.

### Validation du compteur indépendant

Le banc ULA passe **36/36 contrôles Debug (717 ms) et Release (351 ms)**. Les sept nouveaux cas du séquenceur comparent trois images complètes aux frontières nominales quantifiées des acquisitions, les 640 000 positions de HS/CSYNC et de champ, les changements de sélection texte/graphique, le reset et l’équivalence de deux images avancées par points 16 MHz ou par groupes de huit. Ils n’utilisent ni ROM ni fichier externe. Le séquenceur n’est pas encore raccordé au sérialiseur au moment de ces mesures ; ces résultats ne prouvent pas les pixels, les strobes RAM ni les IRQ de la machine native.

### Raccordement du séquenceur à la vidéo et aux IRQ

`ElectronVideo` utilise maintenant le compteur validé pour ses lignes et changements de champ. Les tampons vidéo sont publiés après 39 936 puis 40 064 périodes de référence 2 MHz, soit 80 000 périodes sur deux champs et une cadence moyenne de **50 Hz**. Les prélèvements et le calcul des groupes de huit pixels existants sont conservés pour leur reprise séparée ; le séquenceur seul n’apporte aucune preuve sur leur exactitude complète.

HS/CSYNC actifs et les impulsions RTC/DISPEND sont exposés par le contrat vidéo. L’ULA mémorise les causes IRQ après l’avancement du compteur, plutôt que les dériver des débuts de ligne. Le banc natif mesure les fronts aux périodes CPU **12 670, 32 736, 52 670 et 72 672** en graphique. En texte, DISPEND intervient à **31 968 et 71 904**. Les masques et l’acquittement FE05 conservent leur rôle ; la phase vidéo continue pendant les accès CPU ralentis.

Résultats regroupés : **45/45 contrôles ULA Debug (835 ms), Release (445 ms)** ; **18/18 contrôles Electron natifs Debug (473 ms), Release (435 ms)**. Le nouveau banc vidéo vérifie les huit modes, les deux champs, chaque cause IRQ et son acquittement, HS/CSYNC et le reset. Le contrôle natif utilise la carte Electron réelle du module, avec ROM synthétique locale et avancement explicite des horloges ; ce n’est ni une preuve de logiciel invité ni une capture d’écran. Aucun build de livraison ni commit de puce complète n’est revendiqué pour ce bloc intermédiaire.

### Compteurs d’adresse et registres de pixels

La feuille 6 sépare les données parallèles mémorisées, le choix des plans et la palette. Les feuilles 7–8 distinguent les bits raster VA0–VA3 et les bits d’adresse du compteur horizontal/rangée. La reconstruction ElectronFpga confirme un compteur d’octets VA14–VA3, un latch de rangée VA14–VA6 et les trois bits raster dans l’adresse finale. Son incrément continue dans les rasters texte masqués ; seul leur RGB est masqué. Ses modes VGA/turbo ne sont pas repris.

La reprise conserve une origine de point conventionnelle issue des acquisitions. Le chargement numérique des données est placé au début de chaque cellule de 8/16 points ; le détail des avances RAS/CAS/chargement par rapport à ce point reste à réaliser, et n’est pas prouvé par les captures CSYNC/IRQ/bleu. Les transferts de rangée sont associés à HS, VA3 est effacé immédiatement en mode graphique, et une écriture de palette doit affecter le prochain point sans relire l’octet. Les variantes analogiques de propagation ne sont pas simulées par ce découpage numérique.

### Chargement, palette et adresses : résultats

`ElectronVideo.Memory.cs` distingue le compteur d’octets et le latch de rangée, actualisés pendant HS, des bits raster VA0–VA3. Le compteur d’octets avance aussi sur les rasters texte 8/9 masqués. Le passage graphique efface VA3 immédiatement tout en préservant VA0–VA2. FE02/FE03 n’affectent le début de champ qu’à son rechargement. Les rebouclages de mémoire conservent la taille propre au mode.

`ElectronVideo.Pixels.cs` prélève un octet, conserve ses données et applique séparément la sélection des plans et la palette à chaque point. `TickPixel` donne accès aux changements à l’intérieur d’un octet ; le pas CPU groupé conserve les événements quand son départ est désaligné. Le pas aligné émet les huit points mais avance les compteurs de temps une seule fois. Les sorties d’adresse/donnée sont celles du dernier chargement numérique et ne doivent pas être interprétées comme un bus RAS/CAS déjà réalisé. La couleur publiée porte rouge dans les bits 0–7, vert 8–15 et bleu 16–23.

Premier passage : **71/71 tests ULA Debug, 884 ms**. Après extension de la comparaison aux départs alignés et désalignés : **73/73 Debug (911 ms), Release (444 ms)**, et **18/18 tests Electron natifs Debug (715 ms), Release (472 ms)**. Les nouveaux cas couvrent tous les octets et huit modes, les 64 fils palette contre les seize indices, les répétitions de pixels, changement de palette/mode sans nouvelle lecture, deux rangées avec masquage texte, quatre rebouclages distincts, start différé et VA3. Les deux voies d’avancement rendent des tampons identiques en haute et basse bande passante. Ces contrôles ne remplacent pas les preuves invitées à venir ni le contrôle des strobes RAM physiques.

### Références restantes et divergence du sélecteur vidéo 7

Les feuilles 2 à 5 ont été rendues et examinées : la feuille 2 décrit la génération d’adresse, la 3 le registre et la modulation de sortie tape, la 4 les modes et compteurs généraux, la 5 les multiplexeurs couleur. Les registres de chargement du compteur sont distincts des étages qui avancent : leur séparation devra être conservée lors de la reprise audio/série. Le facteur sonore actuel 32 périodes CPU par demi-onde concorde avec le code MAME et la reconstruction FPGA (31250/(S+1) Hz) ; la formule imprimée du guide est différente. Les phases des rechargements, le décodage du modem et les effets du reset ne sont pas encore déclarés résolus par ces seules feuilles.

Un autre écart a été établi : le sélecteur matériel FE07=7 est traité comme le mode texte 6 dans le code initial et MAME. Les [mesures directes de Pastraiser sur Electron issue 6](https://pastraiser.com/computers/acornelectron/acornelectron.html) décrivent au contraire un affichage graphique de type mode 4 avec rebouclage à 0x3000. ElectronFpga choisit lui aussi un affichage graphique pour ce sélecteur, mais sa base de rebouclage ne suit pas cette mesure. La commande BASIC MODE 7, que le MOS peut transformer en MODE 6, ne doit pas être confondue avec cette écriture matérielle. La correction du sélecteur doit être testée séparément. Les comportements irréguliers rapportés pour les bases vidéo inférieures à 0x1800 ne sont pas encore reproduits ni déclarés caractérisés pour toutes les variantes d’ULA.

Le sélecteur matériel 7 utilise désormais le raster graphique, la fin d’affichage graphique et le rebouclage 0x3000. Résultats : **74/74 tests ULA Debug (1 s), Release (469 ms)** et **18/18 natifs Debug (747 ms), Release (466 ms)**. Ceci ne transforme pas ce sélecteur en télétexte ; l’adaptation MODE 7 du MOS est une opération logicielle distincte.

### Première preuve invitée de la vidéo

Une ROM de diagnostic synthétique s’exécute sur le 6502 et le bus de la carte Electron native. Elle programme FE02/FE03, les huit registres palette et FE07, puis remplit la mémoire écran via des instructions STA indirectes et écrit un témoin de fin hors écran. Le banc attend ce témoin et deux champs supplémentaires, vérifie chaque composante des **163 840 pixels de chacun des huit sélecteurs** contre un calcul indépendant des adresses/plans/palette, puis encode le tampon natif en PNG. Il ne dessine pas les motifs à la place de l’émulateur. La carte est réinitialisée et sa tape détachée dans finally ; les objets Skia et flux sont fermés par using.

**8/8 diagnostics Debug (6,855 s) et Release (2,827 s)**. Le remplissage invité termine après 23 champs pour les sélecteurs 0/1/2, 15 pour 3, six pour 4/5, cinq pour 6 et douze pour 7. Ce sont des durées d’exécution invitées observées, pas un banc de performances hôte. Les huit captures ont été ouvertes et inspectées ; le résultat numérique complet comporte **1 310 720 pixels par passage**. Elles se trouvent sous `artifacts/TestsChips/ElectronUla/AcornElectron/hardware-selector-<0–7>.png`, hors Git.

Empreintes SHA256 des tampons natifs du passage Release :

| Sélecteur | SHA256 du tampon |
| --- | --- |
| 0 | `7F4540689A61197DEBE1D5CA48EB5F450D21CBDCA541D1E7390D6B163FB0F683` |
| 1 | `C7C4890BEA359A81DFDB66B6C20C47E407F9234117E864DCCFDAEE2E70CAF71E` |
| 2 | `53D60BF6153821AC8867387B9E41DDDB53CFF39703FDB31DDE9570950D6F8296` |
| 3 | `95F48471959221F0871E731920774C3B2B3814F6B489F920A0DD9E209D736203` |
| 4 | `D8CF882A3D5E56B44D82F1867565C4D41028801DFBA6C95DF41105621DF859CB` |
| 5 | `1B14185615392A1B895D0C7D61089FB41FF8A80866218BBB108B96962D098A97` |
| 6 | `58B80F32BDC5A20C7783F6AA507FA360F193BFE8684E4D25C23EEE9D9677B131` |
| 7 | `9A641F0D59FD2D95890E1ACA9AB6B61B4BC1203ABB17AAFA7337C3AAE6C15D77` |

Ces motifs prouvent le chemin CPU→RAM/ULA→tampon vidéo dans les modes statiques concernés. Ils ne prouvent pas le chargement d’un logiciel commercial, les transitions analogiques, les fronts RAM, le son ou le modem ; la puce entière reste ouverte.

Le diagnostic invité temporaire a été supprimé après ces deux passages. Les assemblies locales ont été reconstruites en Debug et Release avec zéro erreur ; la découverte filtrée ne retrouve plus `ElectronVideoGuestProof` dans aucune des deux. Les PNG restent ignorés par Git. Les références temporaires de l’audit ULA restent nécessaires aux travaux encore ouverts et seront retirées à sa clôture.

### Passage du transport tape aux niveaux électriques

Le contrat initial livrait `InputBit` déjà démodulé et recevait `SetOutputBit` pour remodulation par le magnétophone. Ce partage retire une fonction réelle de l’ULA. Le nouveau contrat doit transporter uniquement la présence et le niveau CAS IN/CAS OUT ; le chemin PCM, les commandes du magnétophone et le moteur restent dans la machine. En particulier, un échantillon PCM nul au passage par zéro ne doit pas simuler une déconnexion de tape.

Le générateur de sortie nominal est dérivé de l’entrée 16 MHz divisée par 13 : 13×1024 horloges maître par bit, soit **1664 périodes CPU**, demi-ondes de 832 (zéro) ou 416 (un). Le générateur entier évite l’arrondi précédent à 1200 bauds. Les trames sont départ zéro, huit données LSB, arrêt un ; TDEmpty s’active au début de l’arrêt. La logique d’entrée sera déplacée vers les demi-périodes mesurées entre fronts successifs, avec assemblage 2 demi-ondes longues/4 courtes par bit. Ce modèle numérique doit rester distingué des étages analogiques et de la réponse complète du compteur programmable d’entrée FE06, non résolus par ce seul déplacement.

### Modem nominal raccordé et vérifié

Le contrat Electron utilise maintenant `InputLevel`/`SetOutputLevel`. La génération de demi-ondes est dans `ElectronUla.Serial.cs` ; le magnétophone natif Electron est configuré en forme d’onde directe, sans seconde modulation FSK. Le magnétophone conserve sa voie antérieure pour les autres machines. La présence de signal du contrat Electron suit la lecture et sa latence, indépendamment de la valeur PCM instantanée.

Le récepteur mesure les demi-ondes entre transitions, distingue les durées nominales par le seuil médian de 624 périodes CPU et assemble deux longues ou quatre courtes par bit. Mesurer les demi-ondes évite de confondre les périodes mixtes aux changements de fréquence. Une porteuse nécessite dix bits un ; une entrée constante ne la déclenche plus. Les données sont assemblées LSB, présentées après un arrêt valide et acquittées par FE04. Une perte de présence ou un moteur arrêté abandonne la trame partielle ; les causes déjà mémorisées restent acquittées par leurs registres. Le diviseur sonore FE06 n’altère pas la fréquence de sortie tape.

Résultats : **89/89 tests ULA Debug (6 s), Release (833 ms)**. Le banc transmet les **256 octets entre deux ULA** par leurs seules broches électriques, contrôle la forme d’onde de cinq octets représentatifs, TDEmpty, porteuse/acquittement, absence de transitions, moteur, déconnexion et arrêt invalide. Le raccordement natif enregistre une émission dans le PCM 48 kHz et relit l’octet A5 depuis ce PCM ; un second cas insère des échantillons nuls aux transitions. **20/20 tests natifs Debug (922 ms), Release (667 ms)**, après un premier passage sans insertion de zéros **19/19 Debug (863 ms)**.

Limites encore ouvertes : réponse complète du diviseur d’entrée FE06, seuils/hystérésis et filtrage analogiques CAS IN/CAS RC, phases des écritures au milieu d’une émission, interactions du compteur partagé pendant réception/émission et séquence exacte de mise sous tension/BREAK. Ces résultats numériques nominaux ne les valident pas ; les exemples de bits parfaits ne sont pas une mesure des tolérances du silicium.

### Contrôles du générateur sonore nominal

Le banc contrôle les **256 valeurs FE06**, chacune sur une période complète de 64×(S+1) références CPU, niveaux de sortie compris, ainsi que le silence dans les trois autres modes. Il vérifie aussi qu’une écriture vidéo/moteur/LED conservant le mode sonore ne recommence pas la phase. Le cas de changement de diviseur décrit la phase nominale actuellement retenue ; il ne constitue pas une acquisition électrique de son rechargement. Aucune modification du générateur n’a été faite à partir d’une hypothèse non établie.

Résultats regroupés : **92/92 tests ULA Debug (6 s), Release (884 ms)**. Les résultats natifs de 20/20 restent ceux du passage précédent, aucun code de production n’ayant changé depuis. Les correspondances de niveau analogique du haut-parleur, le filtrage et les séquences fines du compteur partagé restent distincts de cette vérification des périodes numériques.

### Feuilles restantes : données RAM, tape et réinitialisation

Les rendus `ferranti-sheet-10.png` et `ferranti-sheet-11.png` montrent respectivement l’entrée tape et les données RAM ; ce ne sont pas deux feuilles supplémentaires de chronométrage. La feuille 11 distingue les chargements LD0 et LD1 : les quatre entrées RAM alimentent successivement les bits pairs puis impairs de l’octet. DSEL choisit les groupes correspondants à l’écriture. Le prélèvement actuel d’un octet complet ne représente donc pas encore ces deux prises. La feuille 9 porte le séquencement ; les acquisitions disponibles n’exposent ni PHI ni RAS/CAS/WE et ne permettent pas d’en mesurer les phases. Aucun chronogramme issu d’un FPGA de remplacement n’est considéré comme une mesure de l’ULA Ferranti.

La feuille 10 doit être rapprochée du compteur partagé de la feuille 4 : le récepteur nominal à demi-périodes ne reproduit pas encore le circuit complet de comptage et de chargement FE06. Les essais de transport des 256 octets restent valables pour ce modèle nominal, sans prouver les écritures en cours de réception ni les tolérances analogiques.

Pour la réinitialisation, le manuel de maintenance distingue POR, entrée qui réinitialise des parties de l’ULA, de RST, sortie vers le CPU également activée par BREAK. La note Acorn 015 précise explicitement que RST est une sortie uniquement. La feuille 12 montre PORBX sur la bascule de remise sous tension ; elle ne montre pas RST en entrée des registres de sélection/commande. Le raccordement logiciel ne doit donc pas assimiler BREAK à une nouvelle initialisation de ces registres ni du magnétophone. La reprise CPU exécute ensuite le MOS, qui peut les modifier par ses propres écritures. Les tests de conservation devront observer les états avant ces écritures du MOS, et distinguer la poursuite des horloges pendant les cycles de reset CPU d’une remise à zéro des compteurs.

### Conservation des états pendant BREAK

`ElectronUla.Reset(false)` conserve maintenant l’état du circuit ; le reset du transport tape est réservé au chemin de mise sous tension dans la carte native. Six nouveaux cas autonomes comparent un circuit recevant BREAK à un témoin continu : quatre modes de communication, masques et causes IRQ, pagination, priorité NMI, moteur/LED, phases sonore et tape, adresses/pixels vidéo et tampon après deux champs. Les deux autres distinguent le drapeau POR et la remise à zéro à froid. Le banc natif utilise une ROM NOP synthétique sans écriture de registres ; après les cycles de reset CPU, la carte témoin reçoit le même nombre de cycles. Les échantillons enregistrés et les sorties audio doivent rester identiques, ainsi que les causes IRQ.

Résultats : **98/98 tests ULA Debug (7 s), Release (1 s)** ; **21/21 tests natifs Electron Debug (1 s), Release (700 ms)**. Ce passage corrige la destruction d’état au déclenchement de BREAK. Il ne valide pas encore le maintien électrique de RST pendant toute la durée d’appui : l’entrée clavier native déclenche actuellement un reset au front d’appui. Les autres limites du compteur partagé et des strobes RAM restent ouvertes. Aucune nouvelle capture ni mesure de performance n’a été produite pour ce passage.

Le raccordement clavier expose désormais le niveau de BREAK par `ICpuBus.ResetRequested`, en utilisant la prise RESET déjà implémentée du 6502. Il n’appelle plus un reset ponctuel à chaque front d’appui. Pendant deux champs sous BREAK, une ROM synthétique INC/JMP ne modifie ni son compteur en RAM, ni PC, ni la pile, tandis que la vidéo et la position tape progressent. Répéter le même état clavier maintenu ne redéclenche pas une séquence de reset. À la libération, le CPU lit son vecteur, effectue les trois lectures de pile et reprend l’instruction invitée. Résultats : **22/22 tests natifs Debug (1 s), Release (764 ms)**. Le cœur CPU n’a pas été modifié. Ce cas vérifie le maintien logique et sa libération dans la carte, sans acquisition des temps de propagation électriques de la touche.

### Compteur de réception et signal moteur

La feuille Ferranti 10 relie RDRF au compteur de données BITIN et à son acquittement, pas à une validation d’arrêt de type UART. La porteuse et la lecture tape effacent cette indication. CAS MO, issu de la feuille 4, commande le moteur externe ; il ne constitue pas une porte d’activation de l’entrée électrique de la feuille 10. Un signal fourni aux broches peut donc être reçu avec le moteur désactivé, même si le magnétophone de la machine cesse alors normalement d’avancer sa bande.

Le récepteur nominal présente désormais les données après leur huitième bit, conserve ce résultat si l’arrêt suivant est bas, et retire RD Full lors d’une nouvelle porteuse. Le test moteur arrêté injecte réellement des niveaux malgré CAS MO inactif. Deux cas supplémentaires vérifient la frontière des huit données, lecture/acquittement, porteuse ultérieure et perte de présence avant la huitième donnée. Le test de toutes les causes IRQ a dû produire RD Full après la porteuse : son ancien ordre reposait sur l’absence erronée d’effacement.

Premier passage Debug : **94 réussites et six échecs**, tous dans cette préparation des causes IRQ. Après correction du banc : **100/100 tests ULA Debug (7 s), Release (1 s)** ; **22/22 tests natifs Electron Debug (1 s), Release (756 ms)**. L’enregistrement PCM et sa relecture restent couverts. Le modèle ne reproduit toujours pas les strobes internes FRAMECK, le chargement partiel du compteur ni les interactions du compteur entre émission et réception : la correction des causes IRQ ne vaut pas clôture de ce circuit.

Comparaison supplémentaire : [Clock Signal, Tape.cpp](https://github.com/TomHarte/CLK/blob/main/Machines/Acorn/Electron/Tape.cpp) et `SoundGenerator.cpp` séparent également les traitements et laissent le diviseur tape non appliqué. Ils ne résolvent donc pas la transcription du compteur Ferranti. Le lien source du [cœur Replay](https://www.fpgaarcade.com/kb/acorn-electron/) vise `Sector14/acorn-electron-core`, indisponible lors de cet examen ; le code n’a pas été obtenu ni utilisé comme preuve. Les schémas originaux restent la référence de ce travail.


### Registre de réception lu pendant le décalage

La feuille 3 possède le registre d’émission DREG ; la feuille 10 possède un registre distinct CD, présenté au CPU lors de CASSRD. Ce sont les horloges et leur compteur qui doivent être examinés ensemble, pas un registre de données unique. Le texte précédent évoquant un registre de données partagé était incorrect et a été rectifié.

Le code décale désormais directement CD à chaque donnée reçue : entrée par le bit 7, sortie par le bit 0. Les bits de l’octet précédent restent visibles tant que les nouvelles données ne les ont pas remplacés. Le début d’une trame et une lecture FE04 ne mettent pas ce contenu à zéro. Le drapeau RD Full reste distinct ; une lecture l’acquitte sans effacer les bits.

Cinq couples d’octets couvrent chaque état intermédiaire du registre, avec données nulles, pleines, alternées et bits isolés. Les attentes calculent séparément la partie ancienne conservée et les données nouvelles déjà reçues ; deux lectures successives doivent rendre le même contenu. Résultats : **105/105 tests ULA Debug (7 s), Release (1 s)** ; **22/22 tests natifs Electron Debug (1 s), Release (758 ms)**. Le contrôle des 256 octets entre deux ULA et l’enregistrement/relecture PCM restent inclus dans ces suites. Les horloges fines et la transcription du compteur partagé ne sont pas déclarées validées par ce changement.

### Chemin des quatre lignes de données RAM

`ElectronRamData` transcrit les prises de la feuille 11 indépendamment du séquenceur : LD0 charge les bits 0/2/4/6 depuis les quatre entrées ; LD1 charge 1/3/5/7. Une prise ne modifie pas l’autre groupe. DSEL présente à l’écriture le groupe pair ou impair correspondant. Le composant ne possède ni mémoire ni horloge ; ses appels de prise devront être commandés par le séquenceur RAM.

Le banc parcourt les 256 états antérieurs et les 256 valeurs d’entrée pour chaque prise, vérifie les lignes d’écriture de chacun des 256 octets et fait varier les entrées entre deux prises. Les quatre bits supérieurs d’une valeur hôte n’ont aucune ligne physique et sont ignorés. Le calcul attendu répartit chaque ligne individuellement, séparément de la permutation utilisée en production.

Résultats : **108/108 tests ULA Debug (7 s), Release (1 s)**. Ces trois nouveaux tests portent sur le chemin de données uniquement. Il n’est pas encore raccordé aux accès natifs : les anciens prélèvements vidéo restent actifs, et aucune nouvelle preuve invitée n’est annoncée. La fermeture du circuit exige encore le multiplexage d’adresses, les strobes et leur raccordement effectif ; ce composant isolé ne remplace pas ces étapes.

### Multiplexage des adresses, feuille Ferranti 8

La table ci-dessous inclut la correction des groupes ligne/colonne RA0–RA6 révélée par le contrôle de rafraîchissement décrit plus bas. Le rendu haute résolution permet de suivre les commandes complémentaires ; l’ordre des entrées dessinées ne suffit pas à identifier leur rôle. A désigne l’adresse logique choisie, CPU AIX ou vidéo VA ; le libellé de la branche supérieure RA7 est bien VA8. La colonne RA7 ne provient pas d’A15 : PARATE commande la prise de parité des deux groupes de quatre données.

| Broche RAM | Adresse de ligne | Adresse de colonne |
| --- | --- | --- |
| RA0 | A3 | A9 |
| RA1 | A4 | A10 |
| RA2 | A5 | A11 |
| RA3 | A6 | A12 |
| RA4 | A7 | A13 |
| RA5 | A2 | A14 |
| RA6 | A1 | A0 |
| RA7 | A8 | Parité de la prise |

ROWCA et son complément choisissent ligne ou colonne ; CPUDV et son complément choisissent les adresses vidéo ou CPU. La bascule de parité est commandée par PARATE : sa valeur n’est pas un bit d’adresse CPU. Le décodage d’écriture tient aussi compte d’A15 et de R/W ; une adresse CPU hors RAM ou une prise vidéo ne doit pas devenir une écriture DRAM. Ces relations de câblage sont distinctes du placement temporel des commandes. Les chronogrammes précédemment acquis ne contiennent pas ces broches.

La suite doit réaliser cette permutation et la tester sur les 32 Kio et les deux parités, puis réaliser le séquenceur qui commande effectivement les prises de données et ce multiplexeur. Une permutation correcte sans ces strobes n’est pas encore une interface RAM raccordée.

Le multiplexeur `ElectronRamAddress` est maintenant transcrit avec des constantes de câblage séparées. Trois tests autonomes contrôlent chaque fil, les 32 768 adresses et leurs deux groupes (65 536 emplacements physiques distincts), puis l’absence d’effet d’A15 sur RA. Les valeurs attendues viennent de listes de fils indépendantes des masques de production. Résultats : **111/111 tests ULA Debug (7 s), Release (1 s)**. Ces tests ne valident pas encore le séquencement ni le raccordement natif des broches RAM.

### Séquence numérique des prises RAM

Le manuel de maintenance Acorn fixe les polarités : RAS et CAS prennent l’adresse sur leur front descendant ; WE bas demande une écriture. Les feuilles Ferranti 8/11 séparent les deux colonnes de parité et les deux prises de données. La feuille 9 produit PARATE, LD0/LD1 et LOAD depuis les phases du 16 MHz ; ses retards annotés ne sont pas des acquisitions des broches d’une ULA de série.

Pour le premier séquenceur numérique, la transaction de huit points retient : préparation de la ligne avant le point 0 ; RAS bas au point 0 ; première colonne/CAS bas au point 1 ; prise paire puis CAS haut au point 3 ; seconde colonne/CAS bas au point 4 ; prise impaire puis CAS/RAS hauts au point 6 ; récupération au point 7. La transaction conserve son adresse et ses données CPU entre ses deux prises. Le décodage WE interdit l’écriture pour une sélection vidéo ou une adresse CPU A15 haute. Une séquence de rafraîchissement ne descend que RAS, sans CAS ni prise de données.

Cette grille est un **séquencement nominal**, pas une mesure des retards Ferranti. Elle concorde avec le contrôleur DRAM du [remplacement JamSoft](https://github.com/moogway82/JamSoftElectronULA/blob/main/Software/JamSoftElectronULA.vhd), qui fonctionne avec les DRAM externes, mais ce remplacement utilise une permutation d’adresse différente et ne valide pas les effets NMI du circuit original. La permutation Ferranti reste celle décrite ci-dessus. Les essais du séquenceur devront distinguer ses fronts, ses prises et le raccordement de carte ; ils ne prouveront pas seuls l’exactitude des phases originales.

Le séquenceur autonome et `IElectronRamPins` sont maintenant réalisés. La mémoire est empruntée : le circuit ne contient ni tableau RAM ni dépendance au module Acorn. Il présente une ligne puis deux colonnes, conserve les données de transaction et prélève séparément les quatre entrées de chaque groupe. À la fin d’une transaction et lors d’une annulation, RAS/CAS/WE sont relâchés ; une écriture déjà effectuée sur un front CAS n’est pas annulée artificiellement. Les prises de lecture restent distinctes du signal de fin de transaction. Le mode rafraîchissement ne touche ni les colonnes ni le registre de données.

Le périphérique de test possède 65 536 groupes physiques indépendants du séquenceur ; il ne reconstruit aucune adresse par les fonctions de production. Les contrôles couvrent les fronts ordonnés, les deux colonnes, les 256 octets sur quatre adresses extrêmes/représentatives avec voisins inchangés, WE interdit pour la vidéo et hors RAM CPU, des entrées modifiées entre les deux prises, toutes les positions d’interruption et les 256 adresses de ligne en rafraîchissement. Résultats du passage regroupé : **124/124 tests ULA Debug (7 s), Release (1 s)**. Après ajout de la libération WE en fin d’écriture et de son assertion : **13/13 tests du séquenceur Debug (176 ms), Release (174 ms)**. L’avertissement xUnit2031 de `Shared/ArchitectureTests.cs` est préexistant et extérieur à ce passage.

Ce résultat établit le comportement numérique nominal du séquenceur, pas encore son emploi par la carte Electron. Les accès CPU et vidéo directs sont toujours présents : leur remplacement doit partager un seul séquenceur, avancer avec les points vidéo, respecter l’arbitrage 1/2 MHz et la prélecture du prochain octet. Faire défiler les huit phases d’un coup avant de faire avancer la vidéo ne reproduirait pas leurs interactions et n’est pas un raccordement validé. Aucune nouvelle preuve invitée ni livraison complète n’est annoncée pour ce passage.

### Raccordement RAM partagé dans la carte Electron

Le propriétaire de carte implémente maintenant `IElectronRamPins`. Il capture la ligne sur RAS descendant, puis lit ou écrit seulement le groupe de quatre bits choisi sur chaque front CAS. Les accès CPU à `_ram[address]` ont été retirés de `ElectronMachine.Bus.cs` : la donnée retournée provient du registre ULA après les deux prises et la récupération. Les écritures prennent effet par groupe, sans modification des quatre autres bits. L’adaptateur de lecture du constructeur vidéo historique passe par les mêmes broches ; il refuse une demande d’écriture au lieu de la perdre silencieusement.

La prélecture vidéo partage le séquenceur avec les demandes CPU. Ses phases avancent entre les points rendus, et le registre de pixels ne prend que la donnée déjà chargée. Une demande RAM CPU reste mémorisée jusqu’à son créneau 1 MHz autorisé ; les accès ROM gardent leur domaine 2 MHz. NMI peut prendre le créneau vidéo et le registre commun reçoit alors les données CPU. Cette corruption numérique suit le partage des broches ; son motif et sa phase exacts sur une ULA Ferranti restent à confronter à une acquisition matérielle. Le rafraîchissement RAS seul du séquenceur n’est pas encore raccordé à un générateur de requêtes natif.

Quatre nouveaux tests autonomes contrôlent une entrée changée entre les deux colonnes (lecture composite 0xFF depuis 0x55 puis 0xAA), l’écriture séparée 0x55 puis 0xFF, l’attente CPU pendant la vidéo et sa priorité NMI. Les tests de pixels ont été adaptés à la prélecture effective ; ceux d’adresses observent le compteur affiché séparément des deux lectures de broches. L’inverse de permutation est vérifié sur tous les groupes physiques.

Résultats : **128/128 tests ULA Debug (10 s), Release (1 s)**. Le test natif parcourt les 32 768 octets en écriture puis lecture, compare aussi le stockage de carte, et vérifie les 256 valeurs sur quatre adresses avec conservation des voisines. La suite native obtient **23/23 Debug (1 s), Release (1 s)**, avec les tests BREAK, IRQ et tape PCM existants. Ces suites ne mesurent pas encore le coût individuel du nouveau chemin RAM et ne remplacent pas les captures invitées après ce raccordement.

### Preuves invitées après raccordement des broches RAM

Une ROM synthétique 6502 exécute le remplissage de 0x3000–0x7FFF par `(adresse basse xor adresse haute)`, programme la palette et le sélecteur, fixe le début vidéo à 0x7FC0 puis dépose un marqueur de fin et boucle en ROM. Le banc attend ce marqueur, contrôle les 20 480 octets réellement écrits par les instructions invitées, puis laisse passer trois champs. Il compare indépendamment les 163 840 pixels de chaque sélecteur, y compris rebouclage, plans de bits, répétitions et rasters masqués. Aucun fichier ROM du produit ni média utilisateur n’est modifié.

Résultats : **8/8 diagnostics invités Debug (8,0921 s), Release (2,6144 s)**, soit **1 310 720 pixels contrôlés par passage**. Les huit PNG ci-dessous ont été ouverts et inspectés : motifs monochromes/couleur, répétitions horizontales et espacements texte correspondent aux vérifications numériques. Ce sont des captures des frames de la carte native exécutant le diagnostic, pas des captures de l’interface WPF ni la preuve de fonctionnement de tous les logiciels Electron. Elles restent hors Git dans `artifacts/TestsChips/ElectronUla/AcornElectron/`.

| Sélecteur | Capture | SHA-256 du PNG |
| --- | --- | --- |
| 0 | `hardware-ram-selector-0.png` | `FD874555EA730DFF1286D99C98E7E9BB7413909223FBF407C3C63A0A6481CB85` |
| 1 | `hardware-ram-selector-1.png` | `21FB681918B96A4AF046B65D76F7E68D23BD6521880E07DA0E602BA7936B1187` |
| 2 | `hardware-ram-selector-2.png` | `9B6215B131B69387DB2E90A380DD56696FE766E2B6C9CDEB44F8D497C01A29AC` |
| 3 | `hardware-ram-selector-3.png` | `CD2B9B1790817AADE5669F7D2E589948AE4E6F40EF0157EE3112D6A8DE20ABAC` |
| 4 | `hardware-ram-selector-4.png` | `4FD34042B5A5D2151D4C41A679BAFFC3AC974CAE72A38548C1643F5E8B92DC16` |
| 5 | `hardware-ram-selector-5.png` | `28421CE679F1B923D1D9F08D40DFBBC5C5A402913096CA3C3BA7246B4DAF5C8B` |
| 6 | `hardware-ram-selector-6.png` | `398E8E3D96E1F2CE99AC0905CC6386DFBB55685CB97499D8D66E449ED591BA19` |
| 7 | `hardware-ram-selector-7.png` | `EAFB98257956D0461D71B2CADF9E8D6CCE29125D257B4F28A8629370B8C12CD6` |

Le diagnostic invité temporaire a ensuite été supprimé avec son dossier devenu vide. Les assemblies locales ont été reconstruites en Debug (8,53 s) et Release (7,12 s), sans erreur ; `ElectronRamGuestProof` est absent des deux découvertes après reconstruction. Les huit captures sont ignorées par Git et restent disponibles. Les références temporaires de l’examen ULA sont conservées jusqu’à sa clôture.

### PHI et périodes sans requête mémoire

La note constructeur [Acorn 015, section 4](https://8bs.com/submit/acornappspdf/015.pdf) fixe PHI bas à 250 ns, soit quatre périodes 16 MHz. Le niveau haut vaut ensuite 250 ns pour un accès ROM, ou se prolonge jusqu’à la fin du créneau 1 MHz (750/1 250 ns lors du changement de domaine), et pendant l’attente vidéo. Le propriétaire avance déjà ces durées par `CpuWaiting`, mais aucune sortie PHI n’expose leur forme : répéter des créneaux bas/haut pendant une attente représenterait plusieurs cycles CPU au lieu d’un seul cycle étiré. La sortie devra rester haute pendant les périodes d’attente existantes, sans modifier leur nombre ni le séquenceur CPU.

Les reconstructions FPGA examinées effectuent des transactions DRAM récurrentes, y compris hors pixels visibles ; le rendu les masque séparément. Le manuel de maintenance précise que RAS contrôle aussi le rafraîchissement, mais ne donne pas de compteur de rafraîchissement indépendant. La feuille 9 produit les strobes à partir de ses phases et de l’arbitrage CPU/vidéo. Il ne faut donc pas ajouter arbitrairement un compteur de lignes rafraîchies et le présenter comme un élément identifié de l’ULA. Le séquenceur RAS seul est testé, mais son déclenchement natif et la couverture des adresses de ligne restent non établis. Ils ne sont pas déclarés finis par les tests des pixels.

Le changement de mode peut modifier le créneau de prélecture ; le registre déjà chargé demeure distinct de la prochaine adresse. Les tests existants couvrent les données/palette/mode pendant un octet et la priorité NMI, mais pas toutes les combinaisons de changement de mode à chacun des huit points d’une transaction. Ce contrôle doit être ajouté avant clôture, avec les signaux observés et les limites de phase nominale séparées.

### Sortie PHI raccordée

`IElectronUla` expose maintenant le niveau PHI et ses transitions avec leur position 16 MHz dans la ligne. Le calcul s’exécute avant les commandes RAM de chaque point. Le callback optionnel n’alloue aucun objet par point ; le rendu groupé de huit points conserve ses compteurs regroupés. Le signal utilise l’attente de bus réellement présente au début de la période, et ne recrée pas une impulsion basse lors de la dernière période de cette attente.

Les onze cas ajoutés contrôlent 160 cycles ROM, les deux phases de passage 1 MHz pour RAM/E/S/clavier, le maintien haut durant l’accès vidéo bloqué, sa libération NMI, la conservation sous BREAK, l’annulation de la demande sous POR et PHI haut pendant les deux strobes d’une lecture CPU. Le niveau bas dure quatre points (250 ns) ; les transitions lentes ont 12 ou 20 points hauts (750/1 250 ns). Pour la convention d’origine du champ utilisée ici, l’accès RAM demandé après le premier cycle en mode 0 tient PHI haut du point 4 au point 640. Cette position est une sortie du modèle nominal, pas une acquisition de phase absolue sur une ULA de série.

Résultats : **139/139 tests ULA Debug (11 s), Release (2 s)** ; **23/23 tests natifs Electron Debug (1 s), Release (1 s)**. L’événement n’ajoute aucun cycle CPU et les tests d’arbitrage, pixels, IRQ, BREAK et tape restent réussis.

### Commandes RAM hors pixels visibles

Les rendus agrandis de la feuille 9 distinguent le diviseur 16 MHz et ses phases, les strobes RASB/CASB, la commande ROWCAB et les prises LD0/LD1. Le blanking participe aux commandes vidéo et à l’arbitrage, mais ne coupe pas directement le générateur récurrent des strobes RAM. Le code natif actuel ne déclenche une transaction que lorsqu’un prochain octet visible ou une demande CPU existe : cette absence de transaction dans les autres créneaux est un écart du raccordement.

La correction à appliquer est de conserver une transaction de lecture sur l’adresse vidéo courante dans les créneaux autrement inutilisés, sans charger artificiellement le registre des pixels et sans écrire en mémoire. Il ne s’agit pas d’un compteur de rafraîchissement supplémentaire, ni de substituer une séquence RAS seule à chaque lecture récurrente. La couverture physique de toutes les lignes et la phase exacte des multiplexeurs restent à distinguer du simple constat de strobes présents.

Les créneaux sans demande CPU ni prélecture visible exécutent désormais une lecture sur l’adresse vidéo courante. Ils utilisent les mêmes phases ligne/deux colonnes que les accès visibles ; leur donnée ne remplace pas le registre destiné aux pixels. Aucun compteur de rafraîchissement supplémentaire n’a été ajouté.

Six cas contrôlent une ligne complète en modes 0/4, les rasters texte masqués en modes 3/6 et les lignes de suppression verticale : **128 fronts RAS descendants, 128 libérations et 256 fronts CAS descendants par ligne**, toujours en lecture. Un autre cas parcourt les 64 couples de sélections de mode et les huit points après lesquels intervenir : l’adresse de la transaction commencée et ses deux colonnes restent stables ; le créneau suivant existe toujours. Le blanc visuel et la progression des compteurs demeurent séparés des transactions RAM.

Résultats : **146/146 tests ULA Debug (13 s), Release (2 s)** ; **23/23 tests natifs Electron Debug (1 s), Release (1 s)**. L’assertion de taille redondante signalée par xUnit2013 dans le nouveau banc a été retirée ; l’avertissement xUnit2031 préexistant est inchangé. Ces contrôles prouvent les transactions récurrentes et l’absence d’écriture parasite ; ils ne simulent pas la rétention analogique des DRAM et ne prouvent pas encore la couverture de leurs lignes par toutes les phases du multiplexeur original. Aucun nouveau diagnostic temporaire ni capture n’a été créé pour ce passage.

### Contrôle physique du rafraîchissement et correction de lecture du multiplexeur

Le [catalogue constructeur NEC 1982, fiche µPD4164](https://bitsavers.trailing-edge.com/components/nec/_dataBooks/1982_NEC_Microcomputer_Catalog.pdf) spécifie les 128 combinaisons RA0–RA6 dans une fenêtre de 2 ms ; RA7 n’est pas une adresse de rafraîchissement. Le nouveau test enregistre réellement les fronts RAS de deux champs vidéo sans accès CPU. Avant correction, les sept cas échouent : modes 0/1/2 : 80 lignes et intervalle observé maximal 312 832 points ; mode 3 : 80 lignes, 318 984 points ; modes 4/5 : 51 lignes, 312 776 points ; mode 6 : 42 lignes, 319 368 points. La limite à 16 MHz est 32 000 points. Les anciens tests de présence de strobes et d’absence d’alias ne détectaient pas ce défaut.

Le rendu des commandes de la feuille 8 impose de distinguer les branches du multiplexeur, et non d’appeler « ligne » la première entrée dessinée. ROWCA et ROWCAB sont complémentaires ; RA7 possède en plus son chemin particulier VA8/parité. La table écrite précédemment a inversé les groupes des broches RA0–RA6. La transcription à appliquer est : ligne RA0–RA4 = A3–A7, RA5 = A2, RA6 = A1 et RA7 = A8 ; colonne RA0–RA5 = A9–A14, RA6 = A0 et RA7 = parité. Cette organisation permet aux adresses vidéo courantes de parcourir les 128 lignes sans inventer un compteur de rafraîchissement.

La figure DRAM du manuel Acorn confirme une prise de ligne et deux prises de colonne par transaction, avec répétition des strobes hors accès CPU. Les images agrandies sont des références documentaires temporaires, distinctes des captures de tests en émulation. Le diagnostic de rafraîchissement reste en échec tant que la permutation et tous ses périphériques de banc n’ont pas été corrigés et rejoués.

La permutation et son inverse sont corrigés dans les constantes et le multiplexeur. Les tables de fils des périphériques de tests, indépendantes des fonctions de production, suivent la transcription corrigée ; les tests d’absence d’alias et les lectures/écritures natives restent contrôlés.

Après correction, les sept cas parcourent **128/128 lignes**. Les intervalles maximaux mesurés sur deux champs sont : modes 0/1/2 **6 904 points = 431,5 µs** ; mode 3 **11 000 points = 687,5 µs** ; modes 4/5 **7 400 points = 462,5 µs** ; mode 6 **11 496 points = 718,5 µs**. Le contrôle inclut les limites de début et de fin de sa fenêtre, pas uniquement deux rafraîchissements consécutifs déjà observés. Tous sont inférieurs aux 32 000 points de la limite 2 ms. Cette preuve concerne les modes standard, avec le début vidéo par défaut et sans accès CPU concurrent ; elle ne simule pas la dégradation analogique d’une DRAM ni toutes les valeurs de début vidéo non standard.

Résultats regroupés : **153/153 tests ULA Debug (12 s), Release (2 s)** ; **23/23 tests natifs Electron Debug (1 s), Release (1 s)**. Après renforcement des bornes de la fenêtre de mesure : **7/7 contrôles de rafraîchissement Debug (1,6433 s de processus), Release (267 ms de suite)**, mêmes maxima. L’échec initial était réel ; il n’a pas été remplacé par une attente réduite au résultat du modèle.

### Phase relative CPU/RAM encore à examiner

La figure 1 DRAM du manuel Acorn place aussi les strobes RAM par rapport à PHI, pas seulement leurs durées individuelles. Le contrat CPU actuel appelle `Tick` avant de présenter l’adresse à `Read`/`Write` ; `ElectronMachine` ne présente donc la demande ULA qu’après cette période minimale. Les tests PHI précédents prouvent l’étirement haut et les temps de cycle, mais ne prouvent pas cette phase relative du transfert natif. Il faut examiner le raccordement du bus avant de déclarer les signaux physiquement alignés. Cela ne justifie pas de changer les instructions du 6502 ni d’ajouter un cycle CPU : le point concerné est la présentation des broches d’adresse/données dans le cycle existant.


### Présentation des adresses avant PHI — examen du raccordement

Le contrat actuel `ICpuBus` appelle `Tick` avant la lecture/écriture. Dans les deux chemins du 6502, NMOS et CMOS, l'adresse n'est donc transmise à la carte qu'après cette période. `ElectronMachine.SynchronizeBus` crée ensuite la requête RAM. Cette organisation conserve la valeur logique des transferts, mais ne permet pas de placer leur début sur la phase basse de PHI montrée par la figure 1 DRAM du manuel Acorn. Les tests de durée de PHI existants ne prouvent pas cette phase relative.

Le raccordement sera facultatif : un bus qui expose les broches de préparation recevra l'adresse et le sens avant `Tick`, puis conservera le transfert existant après `Tick`. Le CPU ne changera ni ses instructions ni son nombre de cycles. Une préparation d'écriture annonce la donnée prévue ; DBE et RESET restent échantillonnés à l'instant existant du transfert. Elle ne constitue donc pas, à elle seule, une écriture mémoire. RDY répète la présentation d'une lecture retenue. Un cycle interne sans transfert n'annonce pas une adresse fictive.

Cette étape prépare le raccordement ; tant que la carte Electron et l'arbitrage vidéo ne l'utilisent pas, la phase CPU/RAM native reste à corriger. Les contrôles du contrat doivent comparer les traces avec et sans préparation et vérifier l'ordre préparation/horloge/transfert, les lectures retenues et l'écriture non pilotée.


#### Contrat préalable validé — 4 novembre 2026

`ICpuBusCyclePins` expose maintenant `PrepareRead` et `PrepareWrite`, appelés avant `Tick` par les chemins NMOS et CMOS du 6502. Le raccordement est mémorisé à la construction ; les bus existants n'ont pas à l'implémenter. Aucun transfert ni cycle supplémentaire n'est ajouté.

Le filtre autonome `FullyQualifiedName~Chip6502|FullyQualifiedName~Chip65C12` passe **92/92 en Debug (113 ms)** et **92/92 en Release (100 ms)**. Il comprend les six cas de `BusCyclePinsTests` : comparaison avec le bus sans préparation en NMOS/CMOS (traces, cycles, registres et mémoire), répétition RDY, deux états DBE après Tick, RESET annulant un accès préparé puis cycle maintenu en RESET sans adresse fictive. La compilation signale seulement l'avertissement xUnit2031 préexistant dans `Shared/ArchitectureTests.cs`.

Ces résultats prouvent le contrat CPU préalable, pas encore le placement RAS/CAS natif : `ElectronMachine` n'utilise pas encore ce raccordement. Le déplacement des créneaux RAM/vidéo et la suppression de la double présentation sur le bus Electron restent nécessaires avant de revendiquer la correction de phase.


#### Raccordement natif et retrait de l'ancienne entrée après Tick

La carte implémente maintenant `ICpuBusCyclePins`. Une préparation crée la demande ULA avant la période minimale ; `Read`/`Write` attendent sa fin puis libèrent la demande sans la recréer. Les accès directs de banc créent eux-mêmes leur demande ; ils ne simulent pas une préparation CPU déjà exécutée. BREAK inhibant un accès préparé libère la demande avant les strobes. Les transferts déjà effectués ne sont pas annulés rétroactivement.

En basse bande, le créneau CPU est à 0 modulo 16 points et la prélecture VDU à 8 modulo 16. Le premier cycle a quatre points PHI bas, puis une portion haute étirée. Les deux phases de départ donnent deux ou trois périodes de référence pour un accès lent ; en haute bande, la RAM attend le premier créneau CPU de blanking. `BeginCpuAccess`, qui exigeait une présentation après Tick, a été retiré avec sa branche d'arbitrage : après migration, aucun appelant ne l'utilisait et conserver deux conventions aurait maintenu une source d'erreur.

Premier passage ULA après migration : 152/153, une attente du registre de pixels NMI observée un octet trop tard ; observation corrigée. Passage suivant : 153/157, les quatre nouveaux contrôles de prélecture attendaient les prises internes aux points 3/6 au lieu des fronts CAS vus par le périphérique aux points 1/4 ; attentes corrigées. Aucun de ces deux ajustements ne change le séquenceur matériel. Le premier passage natif donne **25/25 Debug, 2 s**, avec les deux nouveaux cas : la RAM passe de 00 à 55 avant PHI montant, puis à FF pendant la même première période, sans réécriture au transfert final ; BREAK avant Tick empêche la demande préparée d'écrire. Les passes finales Debug/Release de ce groupe restent à consigner ci-dessous.


#### Validation du groupe PHI/RAM après raccordement

Après retrait de l'entrée après Tick : **249/249 CPU + ULA en Debug (15 s)** et **249/249 en Release (2 s)**, dont **157 contrôles ULA** et **92 CPU**. Les suites natives donnent **25/25 en Debug (2 s)** et **25/25 en Release (1 s)**. Les comptages ne représentent pas des fonctionnalités supplémentaires de la puce : ils désignent les cas des filtres exécutés.

Les contrôles couvrent les périodes lentes de deux/trois références, les quatre points PHI bas suivis du maintien haut, les deux prises RAM de part et d'autre de PHI montant pour un accès aligné, la prélecture basse bande dans le second créneau, l'attente vidéo haute bande, la priorité NMI, les changements de mode en transaction, les 128 lignes de rafraîchissement et l'intégrité RAM native. Les nouveaux tests natifs montrent effectivement une écriture pendant la première période, avant l'appel final du bus, puis l'absence de nouvelle transaction au transfert final ; ils vérifient aussi l'inhibition avant le premier strobe par RESET.

Ces tests établissent le modèle numérique et son raccordement. Ils ne mesurent pas les délais analogiques de propagation du silicium et ne remplacent pas la nouvelle preuve invitée du circuit entier après correction du compteur partagé.


### Compteur partagé — points établis et transcription encore nécessaire

La feuille Ferranti 4 distingue le registre PD chargé par WC0B et le compteur de travail CNT. Pour chacun des huit bits bas, le chargement par la remise à zéro sélective conserve le bit de travail si PD vaut un et le force à zéro sinon : `CNT_bas_après = CNT_bas_avant & PD`. Ce n'est donc ni une affectation générale à PD ni un effacement de toutes les phases à chaque écriture FE06. Les deux étages supérieurs possèdent leur commande de chargement distincte. Le compteur bas est décrémenté en cascade ; l'horloge de CNT8 vient du NOR de CNT6/CNT7, et non directement du dernier bit de la cascade. Les polarités/fronts et le rechargement asynchrone doivent être transcrits ensemble avant de remplacer les compteurs logiciels actuels.

Le décodeur de mode sélectionne CK3 pour le son, S8M13 pour la sortie tape et CASSCK pour l'entrée. S8M13 dérive du 16 MHz divisé par 26. Les sorties CNT8/CNT9 servent aussi aux séquenceurs tape : conserver des oscillateurs indépendants qui repartent à chaque changement de mode ne reproduit pas cette structure. Le moteur est une sortie distincte et n'arrête pas ces horloges.

La [discussion technique accompagnant les schémas](https://stardot.org.uk/forums/viewtopic.php?t=13818) fournit des hypothèses de décodage S1/S2/S11/S15 et signale des tracés barrés ambigus ; elles restent à confronter aux portes, pas à adopter comme spécification. Sur la [seconde page, analyse de 2018](https://stardot.org.uk/forums/viewtopic.php?start=30&t=13818), l'auteur décrit la division FRAMECK par quatre, la prise au milieu du bit et un arrêt d'une demi-période. Il distingue lui-même une observation restant inexpliquée après l'arrêt du moteur. Ce texte n'est pas une preuve que l'actuel récepteur nominal, ni sa réinitialisation sur absence de signal, correspondent au circuit complet.

La prochaine correction doit donc réunir PD/CNT et leurs sources d'horloge avant les séquenceurs TX/RX ; les tests devront vérifier chargement partiel, écritures en cours de compte, changements de mode, maintien pendant BREAK, phase FRAMECK et arrêts courts. Aucun de ces points n'est déclaré implémenté par les passes PHI/RAM ci-dessus.


#### Détail du chargement bas CNT0–CNT7

Les deux régions supplémentaires du PDF Ferranti montrent les registres PD distincts des bascules CNT et les commandes CNTRES. La remise à zéro sélective justifie `CNT_bas &= PD` au chargement ; une écriture du registre PD seul ne modifie ni CNT ni le niveau sonore. La polarité des cellules de cascade basse correspond au comptage descendant déjà identifié. Les croisements et annotations barrées autour des décodeurs et des deux étages supérieurs demandent encore une transcription séparée ; ils ne justifient pas une simulation arbitraire de leurs fronts.

Le premier raccordement remplace le simple produit durée/diviseur du son par le registre PD et la cascade basse : à chaque impulsion de l'horloge sonore nominale, CNT bas décrémente ; son passage de 00 à FF fournit la frontière de rechargement, puis le chargement sélectif conserve les uns de PD et efface ses zéros. Cette frontière conserve la période sonore nominale existante, mais une écriture FE06 en cours de période prendra effet au rechargement, sans effacer la phase ou la sortie. Ce raccordement ne remplace pas encore les horloges TX/RX ni les étages CNT8/CNT9. Il ne doit pas être présenté comme une transcription complète du compteur partagé.


#### Résultats du registre PD et de la cascade basse

`ElectronCommsCounter` contient PD et CNT0–CNT7. Les trois tests autonomes couvrent les 65 536 couples état/diviseur avec une attente bit par bit indépendante, l'absence de modification du compte lors d'une écriture PD, l'idempotence du chargement, trois parcours complets de la cascade avec emprunt, et l'effet différé d'un nouveau diviseur au rechargement. Le son utilise cette cascade sur son horloge nominale existante de 32 références CPU. La première frontière après mise à zéro de CNT survient après une période de cette horloge ; les 256 valeurs sont vérifiées sur deux demi-périodes complètes après le rechargement. Le contrôle en cours de note vérifie que le niveau et la période courante restent intacts à FE06, puis que la nouvelle valeur est utilisée à la frontière suivante. Les écritures vidéo/moteur/LED conservant le mode sonore ne relancent pas le compte.

Résultats : **160/160 tests ULA Debug (12 s), Release (2 s)**. Le premier build natif a échoué sur deux alias sans appelant dans les constantes de carte, encore liés aux anciens noms `InitialSoundDivider` et `SoundHalfPeriodDivider`. Après retrait de ces alias : **25/25 natifs Debug (2 s), Release (1 s)**. Les avertissements CS8604/CS0649 préexistants des autres bancs restent sans rapport avec ce raccordement. `git diff --check` est exécuté après mise à jour du document.

La partie haute du compteur, ses chargements asynchrones, le choix de ses horloges dans les autres modes, FRAMECK et les interactions TX/RX ne sont pas validés par ces tests. En particulier, le reset du sérialiseur et du niveau sonore au changement de mode reste celui du chemin précédent jusqu'au raccordement commun complet ; la correction de FE06 n'est pas une validation de cette transition.


### Registre d'émission chargé directement par FE04

La feuille Ferranti 3 montre DREG0–DREG7, les deux cellules SYNC et un compteur de bits commandé par WRCASS. Elle ne montre pas un registre d'attente supplémentaire devant DREG. Le code `_pendingByte`/`_transmitPending`, transféré seulement quand l'ancienne trame est terminée, ajoute donc un double tampon absent de ce chemin. Une nouvelle écriture doit charger le registre d'émission courant et redémarrer son compteur de trame, même si l'émetteur était occupé. La remise à zéro de `_outputLevel` et de la phase de porteuse à cette écriture n'est pas reliée à WRCASS sur la feuille du compteur partagé : cette réinitialisation artificielle doit être retirée.

Cette correction porte sur le chargement du registre et la conservation de la porteuse. Le compteur de trame nominal actuel et ses instants TX Empty ne constituent toujours pas une transcription des deux cellules SYNC et des portes CLK/CLKB. Le rattachement de leurs horloges au compteur partagé reste nécessaire ; les tests ne devront pas présenter le retrait du double tampon comme la fin de ce raccordement.


La sélection d'une autre donnée peut légitimement changer CAS OUT si les deux phases de fréquence ont des niveaux différents à cet instant. Le contrôle ne doit donc pas interdire tout front à FE04. La correction conserve une phase nominale libre de 1664 références et sélectionne les niveaux de ses fréquences 1200/2400 Hz ; elle retire la phase demi-onde qui repartait selon la valeur du bit. Le cas sans front forcé écrit à un instant où les deux fréquences sont déjà hautes. Ce modèle nominal de phase reste distinct du raccordement des bascules du compteur Ferranti.


#### Résultats du chargement direct d'émission

Les deux nouveaux cas montrent qu'une réécriture FE04 interrompt l'octet courant au lieu de le mettre en attente, et qu'un start bit sélectionne la phase libre sans forcer un niveau bas. Le second cas écrit pendant une zone où les deux fréquences sont hautes, puis vérifie leur frontière commune suivante ; il n'interdit pas les fronts légitimes de sélection entre deux niveaux différents. Le cas des cinq trames à demi-ondes exactes commence désormais avant la première période de porteuse, pour ne pas imposer une remise à zéro absente du schéma. Les transferts électriques des 256 octets, indépendance FE06, drapeaux et conservation BREAK restent inclus.

Résultats : **162/162 ULA Debug (14 s), Release (2 s)** ; **25/25 natifs Debug (2 s), Release (1 s)**, avec enregistrement et relecture PCM A5. La suppression du double tampon ne valide pas encore CLK/CLKB ni la commutation d'horloge du compteur partagé.

Le sélecteur de mode de la feuille 4 choisit les horloges ; il ne correspond pas à une commande globale effaçant DREG, CD, le compteur bas ou la bascule sonore. `Write(FE07)` appelle encore `ResetSerial` et efface le niveau/phase sonore lors d'un changement de mode. Cette destruction logicielle doit être retirée. Le test sans période d'horloge entre deux changements de mode isolera la conservation des registres de l'effet encore incomplet des horloges sélectionnées.


#### Conservation des registres au sélecteur de mode

FE07 n'appelle plus `ResetSerial` et n'efface plus la bascule ni la phase sonore. Le reset à froid garde ces responsabilités ; les écritures explicites des registres et l'indication RD Full d'entrée en mode sortie sont conservées. Trois cas isolent la sélection de mode sans horloge intermédiaire : un octet A5 chargé avant le mode sortie doit produire la même trame que celui chargé après ; le registre CD reçu reste A5 à travers les quatre modes ; un aller-retour depuis le son conserve son niveau puis ses 300 références suivantes, en comparaison d'un témoin continu.

Résultats après ce groupe : **165/165 ULA Debug (14 s), Release (2 s)** et **25/25 natifs Electron Debug (2 s), Release (1 s)**. Les captures invitées existantes restent celles de la partie vidéo précédente ; aucune nouvelle capture ni mesure de performance complète n'est revendiquée pour ce groupe. Les horloges TX/RX et les compteurs de trame restent nominaux ; en particulier, la progression des séquenceurs dans les autres modes et leurs transitions CLK/CLKB/SYNC ne sont pas prouvées par un test sans horloge. Ces points restent ouverts avant la clôture du circuit.


### Cadence de prise de réception FRAMECK

La feuille Ferranti 10 sépare le compteur de phase à deux bits, le compteur de données et le registre CD. Le décalage est placé au milieu du comptage de phase, pas après l'accumulation d'un bit complet. La transcription nominale doit donc produire quatre pas de phase par bit : une demi-onde courte en fournit un, une longue en fournit deux ; la prise survient à la phase 2. Le start zéro traverse aussi CD ; le compteur de données rend RD Full après les huit données, à leur milieu. Un stop haut maintient la phase réarmée sans décaler un un dans CD. Un stop bas peut jouer le rôle du start suivant. Le début de la nouvelle trame retire RD Full puisque son compteur n'indique plus huit données.

Le décodage précédent attend deux longues ou quatre courtes avant tout décalage et ne décale pas le start. Il présente donc trop tard les bits intermédiaires et RD Full. La correction garde pour ce groupe les seuils nominaux de demi-onde existants : elle ne prétend pas reproduire les portes S1/S2/S11/S15 ni la propagation asynchrone de CASSCK. Les comparaisons en cours de trame et la demi-période de stop doivent observer les instants réellement injectés, pas une valeur privée du compteur.


#### Résultats de prise à mi-bit et arrêt raccourci

Le récepteur nominal avance désormais par quatre pas de phase par bit et décale à la phase 2. Le start zéro entre dans CD ; pour N nouvelles données déjà reçues, les anciens bits conservés proviennent donc de l'octet précédent décalé de N+1, et non N. Le test des cinq couples de données a été corrigé dans ce sens. RD Full arrive à la prise de la huitième donnée ; le start suivant l'efface à sa propre prise. Le stop haut ne décale pas CD et réarme la phase. Un stop bas décale le start zéro de la trame suivante, sans registre d'erreur de trame.

Huit cas supplémentaires injectent les niveaux sans accéder aux champs privés du récepteur : quatre valeurs pour la huitième donnée à l'instant 832/1664 références, trois couples d'octets avec stop d'une demi-période, et un start effaçant RD Full non lu et décalant FF vers 7F. Le contrôle existant de stop bas observe maintenant la donnée et RD Full avant ce nouveau start, puis leur état après celui-ci. Les transferts des 256 octets par broches restent couverts.

Résultats : **173/173 ULA Debug (13 s), Release (3 s)** ; **25/25 natifs Debug (2 s), Release (1 s)**, enregistrement et relecture PCM A5 avec les deux variantes de passages par zéro inclus. Aucun périphérique audio Windows n'est ouvert par ces bancs. Les seuils de classification restent ceux du modèle nominal ; la construction de CASSCK depuis le compteur partagé et les décodeurs S1/S2/S11/S15, le RC de porteuse et les interactions d'horloge inter-modes restent à terminer avant la clôture de l'ULA. Ces tests ne sont pas une mesure des tolérances analogiques du circuit.


#### Source complémentaire examinée pour les horloges

Le dossier [`electron_ula` de Myelin](https://github.com/google/myelin-acorn-electron-hardware/tree/main/electron_ula), branche `main`, contient les notes README, les repères de cellules, un script de traitement d'image et l'outil interactif de tracé de grille. Son README décrit l'extraction de la disposition interne ; les fichiers obtenus ne fournissent pas une netlist fonctionnelle du compteur CNT/CASSCK. Ce dépôt ne lève donc pas les ambiguïtés de fronts restantes et ne sera pas présenté comme une validation de la transcription. La preuve invitée suivante vérifie le chemin de réception actuellement implémenté, indépendamment de cette lacune documentaire.


### Preuve invitée du récepteur PCM après prise à mi-bit

Une ROM de banc a réellement été exécutée par le 6502 de la carte Electron native. Ses instructions effacent la RAM écran, sélectionnent le mode 4 et le start 5800, programment la palette, FE06 et le masque RD Full, activent le moteur, attendent l'octet, comparent A5 puis vérifient que la lecture FE04 a acquitté RD Full. La ROM dessine elle-même PASS ou FAIL en RAM vidéo ; le banc hôte n'écrit ni le résultat ni les pixels dans la machine.

Le PCM fourni à l'entrée contient une porteuse puis une trame A5 à 48 kHz et ±16000. Le second cas remplace les échantillons de transition par zéro. Le banc vérifie le drapeau écrit par la ROM, les 32 octets de glyphes et les 163 840 pixels du cadre 640×256, puis enregistre le cadre vidéo sans ouvrir de fenêtre ou de sortie sonore Windows. Cette preuve ne charge pas un logiciel commercial ni une image de F:/Retro : son signal et sa ROM sont ceux du banc. Elle démontre la réception et l'acquittement par le CPU, le moteur et le rendu configurés par ce programme ; elle ne démontre pas les tolérances analogiques ni le compteur partagé restant incomplet.

Passages : **2/2 Debug**, cas 842 et 592 ms, durée totale de commande de tests 2,8300 s ; **2/2 Release**, cas 684 et 530 ms, durée totale 2,5670 s. Les mesures internes des seules frames invitées Release sont respectivement 403,16 et 422,24 ms ; elles incluent la préparation et l'attente du programme, pas un banc de débit maximal de l'ULA.

Captures inspectées, conservées hors Git sous `artifacts/TestsChips/ElectronUla/AcornElectron/` :

- `receive-midbit-pcm.png`
- `receive-midbit-pcm-zero-crossings.png`

Les deux PNG ont l'empreinte SHA256 `C9B3DF072257FFDBC2ACF0481B43977BE824033757C887EC00331660262EE244`. L'identité avec les captures relues en Release a été vérifiée. Les pixels sont encodés en BGR32 opaque ; les octets alpha non utilisés du tampon natif ne rendent donc pas la capture transparente. Le diagnostic temporaire doit être retiré des sources et des deux assemblies avant livraison ; les captures sont ignorées par Git.


Le fichier `ElectronReceiveGuestProof.cs` et son dossier devenu vide ont été supprimés après les deux passes. Reconstructions locales Debug/Release réussies (respectivement 16,95 et 16,82 s, zéro erreur, trois avertissements préexistants). La découverte filtrée `--list-tests --no-build --filter FullyQualifiedName~ElectronReceiveGuestProof` ne retourne plus aucun cas dans les deux assemblies. Seuls les deux PNG ignorés et les résultats ci-dessus sont conservés pour cette preuve.


### Start vidéo bas documenté sur Electron issue 6

Les [observations matérielles de Pastraiser](https://pastraiser.com/computers/acornelectron/acornelectron.html), en mode 0, décrivent un rechargement initial : 0000–07C0 et 1000–17C0 correspondent à 3000–37C0. Pour 0800–0FC0, le compteur part bien de cette plage puis bascule de 1000 vers 3000. Il faut donc normaliser le chargement de début de champ, pas masquer chaque adresse lue. Les registres start restent bruts et latched au champ suivant. Cette observation n'est pas étendue aux autres modes. L'artefact instable du premier caractère signalé par l'auteur reste non modélisé ; aucun bruit arbitraire ne sera ajouté pour le simuler.


### Compteur de communication partagé : modèle fonctionnel retenu

Le modèle ElectronFpga de hoglet67 fournit un compteur 16 bits commun, alimenté selon le mode ; sa phase survit à FE07 et FE06 ne modifie que le registre de diviseur. En émission, chaque état décrémente de 1, sauf à la frontière des 12 bits bas où la décrémentation vaut 0x301 : 4096 états virtuels occupent alors 3328 horloges 16 MHz. Les sorties des bits 12 et 13 donnent les demi-périodes de 416 et 832 références 2 MHz ; la frontière des 14 bits bas donne 1664 références par bit. Ces relations permettent une avance par événements, sans boucle par horloge maître.

Pour le son, le rechargement FPGA `PD << 8` ne fournit pas PD+1 et donne même une inversion à chaque horloge pour PD=0. Il n'est pas recopié tel quel : le rechargement numérique retenu vaut `(PD << 8) | 255`, soit exactement 256*(PD+1) horloges maître par demi-période, conforme à la période nominale déjà contrôlée sur les 256 valeurs. La phase à froid n'est pas une mesure matérielle. En entrée, le compteur commun mesure la durée entre fronts ; l'assemblage FRAMECK et le registre de données restent des états distincts, comme le compteur de bits de la trame.

Source du modèle : https://github.com/hoglet67/ElectronFpga/blob/master/ElectronULA.vhd . Cette reprise définit un modèle fonctionnel vérifiable, pas une certification des délais de propagation des portes Ferranti ni du filtre RC analogique. Les différences de mode doivent désormais agir sur un état commun au lieu de trois délais indépendants.

Premier passage après raccordement : 168/174 ULA Debug passent (14 s). Les cinq attentes de niveau initial de porteuse et une frontière TDEmpty supposant la remise à zéro à FE04 ne correspondent plus au modèle commun. Les 256 transferts électriques passent. Les attentes sont corrigées selon l’oracle maître indépendant, avec ajout d’un contrôle FE04 hors frontière et d’un changement son→émission avec horloges effectives. Aucun résultat natif après cette modification n’est encore acquis.

Deuxième passage : 176/176 ULA Debug passent en 13 s. Carte native : 24/25 Debug passent en 2 s, avec uniquement une attente TDEmpty obsolète après FE04 écrit juste après la frontière à froid. Le start doit attendre la référence 1665 et le stop commence à 16641 ; le banc est modifié pour contrôler la dernière référence avant cette frontière, puis la suivante. Les réceptions PCM de la carte passent déjà.

Résultats après corrections regroupées : ULA **176/176 Debug (13 s), 176/176 Release (2 s)** ; carte Electron native **25/25 Debug (2 s), 25/25 Release (1 s)**. Les tests nouveaux comparent l'avance par événements à huit pas maîtres explicites par référence, pour les 256 PD, 32 bits de porteuse et les transitions des quatre modes depuis 256 valeurs de diviseur. Ils contrôlent aussi la saturation d'une entrée constante, le start différé à la frontière existante et l'effet réel du passage sonore sur la phase d'émission. Les réceptions PCM natives et les 256 transferts électriques restent validés.

Les temporisations `_tonePhase`, `_transmitPhase`, `_outputWavePhase` et `_inputPeriod` ont été retirées : la durée d'entrée, le décompte sonore et la phase des deux porteuses utilisent maintenant le même état. Le synchroniseur de départ et le compteur de bits restent distincts du compteur de fréquence ; FE04 ne peut plus créer une nouvelle horloge de bit indépendante. Le compteur de réception de trame reste nominal : cette modification ne constitue pas une reproduction analogique du filtre CASSCK/RC ni une mesure de ses seuils. Les captures invitées précédentes ne sont pas présentées comme des captures produites après ce raccordement. Le circuit entier reste en cours.

### Contrôle des starts bas après correction

Les 18 cas ajoutés reprennent les bornes 0000/0040/07C0, 0800/0FC0, 1000/1040/17C0 et 1800, la retenue au milieu d'une rangée répétée sur huit rasters, le prélèvement des deux nibbles à la dernière ligne et la conservation du start brut en passant dans un autre mode. Résultats : **194/194 ULA Debug (13 s), Release (2 s)** ; **25/25 natifs Debug (2 s), Release (1 s)**. Aucun artefact aléatoire n'a été ajouté pour imiter le premier caractère instable décrit par l'observation matérielle ; la règle reste limitée au mode effectivement observé.

### Mesure du circuit avant optimisation du chemin pixel

Banc temporaire autonome : 8 champs de chauffe, puis 40 champs, sans limite de cadence ni périphérique audio Windows ; l'audio natif est drainé à chaque champ. La carte exécute `JMP C000` en ROM 6502. Les valeurs MHz ci-dessous représentent les périodes de référence de la carte par seconde hôte, pas le débit d'instructions.

| Chemin | Debug ms/champ | Release ms/champ |
|---|---:|---:|
| Vidéo mode 0 |47,602|6,704|
| Vidéo mode 4 |47,584|3,842|
| ULA mode communication inutilisé |49,212|4,227|
| ULA réception sans signal |48,824|5,137|
| ULA son PD=0 |49,983|4,196|
| ULA sortie tape |51,085|4,871|
| Carte native + drainage audio |57,852|6,740|

La séquence Release n'est pas une comparaison isolée parfaite : son premier chemin est plus lent que le même séquenceur avec ULA, ce qui indique une phase de chauffe/tier JIT ou de charge hôte encore variable. Le banc est étendu à trois séries par chemin avec médiane et intervalle avant de conclure sur un gain Release. Les empreintes des chemins vidéo mode 0, mode 4 et carte native sont respectivement `066ABDF9C2C604085E2C0FD6DD9DF6783C06D9A233F8574DD47926C936E47CF6`, `C4A8A35FAC5759A123416023174A1272D1495DB297A3866C70304E76FADE28C7` et `FF6335069B6E140EB47149D847AEA80BF7E2B06BD80AE9708AA382EFB3AE21EE`.

La médiane de trois séries Release de 40 champs donne avant optimisation : vidéo mode 0 **3,796 ms** (3,744–8,712), mode 4 **3,729 ms** (3,691–3,735), ULA inutilisée **4,073 ms**, réception **3,860 ms**, son **4,034 ms**, sortie **3,856 ms**, carte native **4,945 ms** (4,879–6,894), soit **8,089 MHz** de référence. Le premier passage lent confirme la nécessité de distinguer chauffe JIT et coût stable. Empreintes inchangées.

Après décodage du mode au registre FE07 et table fixe de 256 répartitions de bits : **194/194 ULA Debug (8 s)** et **25/25 natifs Debug** passent. Médianes Debug sur trois séries : vidéo 0 **28,331 ms** (27,657–32,837), vidéo 4 **26,969 ms**, ULA inutilisée **29,145 ms**, réception **29,178 ms**, son **29,106 ms**, sortie **29,996 ms**, carte native **37,785 ms** (37,405–38,040). Les trois empreintes pixels restent identiques à la référence précédente. La mesure Debug précédente était une seule série de 40 champs ; les nouveaux intervalles ne changent pas l'ordre de grandeur du gain mais ne constituent pas une étude de variance exhaustive. Les tests natifs ont tourné pendant le début du premier chemin : le banc est isolé dans une collection non parallèle pour les prochains passages.

Validation du premier changement : **194/194 ULA Release (3 s), 25/25 natifs Release**, diagnostic isolé de la collection native. Release après optimisation : mode 4 **2,693 ms**, ULA inutilisée **3,017 ms**, réception **2,869 ms**, son **2,939 ms**, sortie **2,992 ms**, carte native **4,018 ms** (3,969–5,316), soit **9,955 MHz**. Empreintes identiques. Le premier chemin vidéo mode 0 reste affecté par la chauffe du JIT (2,690–10,895 ms, médiane 7,941) et n'est pas retenu comme preuve de régression ou de gain stable. La chauffe du prochain passage dure au moins une seconde par chemin, en plus des huit champs minimaux.

### Événements RAM et mesure après chauffe temporelle

Le séquenceur représente désormais son événement instantané par `ElectronRamEvent` : aucune prise, donnée paire, donnée impaire ou transfert terminé. Ces événements étaient déjà mutuellement exclusifs sur les phases matérielles ; leurs trois sorties booléennes restent consultables aux mêmes points. Une affectation d'état remplace les trois remises à zéro répétées et l'appel du helper correspondant à chaque point maître. Les strobes, les broches et la prise des données n'ont pas été supprimés.

Après cette modification : **194/194 ULA Debug (7 s), Release (2 s), 25/25 natifs Debug et Release**. Banc non parallèle, au moins 8 champs et 1 seconde de chauffe par chemin, trois séries de 40 champs.

| Chemin | Debug médiane ms/champ | Release médiane ms/champ |
|---|---:|---:|
| Vidéo mode 0 |24,827|2,955|
| Vidéo mode 4 |24,567|2,893|
| ULA inutilisée |25,731|2,837|
| ULA réception |25,801|2,856|
| ULA son PD=0 |26,126|2,796|
| ULA sortie tape |27,101|2,955|
| Carte native + drainage audio |34,104|3,973|

Carte native : intervalle Debug 33,908–34,289 ms, Release 3,875–4,396 ms ; équivalents de référence **1,173 MHz** et **10,068 MHz**. Les trois empreintes pixels sont toujours identiques. Le premier changement de décodage apporte l'essentiel du gain observé : l'écart Release entre 4,018 et 3,973 ms après la seconde modification est trop faible face aux intervalles pour établir un gain isolé de ce dernier changement. En Debug, l'intervalle 33,908–34,289 est distinct de 37,405–38,040 mesuré avant cette représentation. Le début du premier banc Debug antérieur pouvait coïncider avec les tests natifs, ce qui limite la comparaison fine de son premier chemin vidéo.

Ces mesures sont celles d'une boucle ROM sans activité d'application, d'affichage Windows ou de sortie WASAPI ; elles ne démontrent pas le débit maximal de chaque logiciel. Le chemin Debug reste plus lent que le temps réel de la carte : l'optimisation ne doit donc pas être présentée comme close. Les captures invitées finales restent à produire après stabilisation de ce chemin.

### Isolation du coût des broches RAM

Le banc conserve tous les appels et fronts du séquenceur, mais remplace temporairement son récepteur par des broches sans décodage pour isoler ce coût ; ce récepteur n'est jamais raccordé à la production. Debug : mode 0 **23,777 ms sans décodage, 25,171 ms avec**, mode 4 **23,195/24,654 ms**. Release : mode 0 **2,088/2,860 ms**, mode 4 **2,053/2,861 ms**. Les tampons noirs ont tous l'empreinte `FF6335069B6E140EB47149D847AEA80BF7E2B06BD80AE9708AA382EFB3AE21EE`. Le décodage représente donc environ 1,4 ms Debug et 0,8 ms Release sur ce banc ; il ne suffit pas à expliquer le coût Debug restant. Les prochains changements ciblent les calculs PHI répétés hors des frontières et les écritures des octets de chaque pixel, sans retirer les appels de broches.

Après PHI aux frontières et écriture native des pixels : **204/204 ULA Debug (7 s), Release (2 s), 25/25 natifs dans les deux configurations**. Médianes Debug vidéo 0/4 **23,957/23,731 ms**, ULA inutilisée/réception/son/sortie **24,881/25,459/24,932/26,348 ms**, carte native **33,010 ms** (32,787–33,531). Release vidéo **2,613/2,649 ms**, ULA **2,781/2,863/2,819/2,785 ms**, carte **4,042 ms** (4,019–4,049). Empreintes inchangées. Le gain sur la carte Release n'est pas établi face au passage précédent ; le gain Debug reste modeste. Le regroupement de la retenue PHI conserve explicitement les changements hors frontière et réentrants, et les tests du tampon couvrent ses huit couleurs, huit sélecteurs, rasters masqués et octet réservé.

Après partage du codage parallèle des quatre broches, position ligne/point maintenue directement et sortie anticipée des pixels masqués : **206/206 ULA Debug (6 s), Release (2 s), 25/25 natifs dans les deux configurations**. Debug : vidéo 0/4 **21,144/21,026 ms**, ULA inutilisée/réception/son/sortie **22,494/22,159/22,997/23,510 ms**, carte native **30,550 ms** (30,440–30,707), **1,309 MHz**. Release : vidéo **2,411/2,431 ms**, ULA **2,547/2,595/2,651/2,617 ms**, carte **3,698 ms** (3,690–3,835), **10,816 MHz**. Empreintes identiques à la référence. Le nouveau test de fusion parcourt les 256 anciens octets, 256 valeurs de broches et deux groupes avec attente fil par fil indépendante ; le suivi ligne/point est confronté au calcul arithmétique sur deux images et des pas 1/8 non alignés. Le chemin Debug reste sous le débit nominal de la carte, sans modification des options de compilation.

### Preuves invitées après raccordement et optimisation

Le diagnostic temporaire produit une ROM 6502 de moins de 16 Kio, exécutée sur `ElectronMachine`. Le CPU invité vérifie POR et son acquittement, écrit puis relit les 20 Kio 3000–7FFF avec le motif octet-bas XOR page, contrôle les causes RTC/fin d'affichage, leur masque et leur acquittement, attend TDEmpty après une émission A5, puis acquitte RD Full. Il sélectionne ensuite le mode vidéo et le son PD=31, programme la palette et écrit lui-même le motif de texte `ULA PASS` dans la RAM. En cas d'échec, le chemin invité écrit `ULA FAIL` et un autre octet résultat. L'hôte ne dessine ni le résultat ni le fond d'écran.

Après stabilisation, le banc compare tous les octets 3000–7FFF et les 163 840 pixels à des calculs indépendants, y compris rasters masqués et octet réservé. Le PCM du dernier champ contient 958 échantillons à ±8192 et 39 transitions, cohérentes avec la fréquence nominale PD=31 ; ce contrôle n'est pas une mesure analogique de haut-parleur ni un test d'un périphérique audio Windows. Chaque invité atteint PASS en 20 champs. Premier build du banc corrigé sur l'octet bas du vecteur RESET, puis **8/8 cas Debug (7,8837 s), 8/8 Release (3,7725 s)**. Les huit tampons pixels sont identiques entre configurations. Les huit PNG natifs 640×256 ont été inspectés et sont tous ignorés par Git, avec index vide au contrôle.

Dossier : `artifacts/TestsChips/ElectronUla/AcornElectron/`. Les captures sont les pixels natifs du circuit, avant correction du rapport d'aspect de présentation. Les modes 4 et 7 ont ici la même image : cette plage de mémoire n'atteint pas leur rebouclage différent, qui est vérifié séparément par les tests autonomes. Les preuves ne remplacent pas les traces de PHI/RAS/CAS/NMI ni les tests des cas limites de réception.

| Sélecteur / fichier | SHA256 PNG conservé |
|---|---|
| `final-selector-0.png` | `9162856F05CFEEB01B9DEADD00E62A7444625DE8FBE364C3A553B71A46499F85` |
| `final-selector-1.png` | `2698AE3C0EDA826B02ED98604AAA851C36FD7BD42A708CF591B78D6C2D59083D` |
| `final-selector-2.png` | `ABE3EC6DEDFBFD07E3EF1E5F527836FFF30BD2964F8BB23B79E7B154849D7D36` |
| `final-selector-3.png` | `2B9FD0E60083F03FA9CFC7CCBF0C09C75B81B0FE14E79CB0C6507BE8B766588D` |
| `final-selector-4.png` | `96F5AE68AE0A4F8A50A0D0D5B0E43B0C572CA40BEFDFE30E7EFC1163B4163B9B` |
| `final-selector-5.png` | `D9D9FF7C9509B546C305AF90CC9EA2F362DEF60751B2C70BC13698F63722F1D4` |
| `final-selector-6.png` | `ECE92537EFD88C0BB67BC9BB6D2DC74DD4B37814BD65535FEA0E0F9BB85401C5` |
| `final-selector-7.png` | `96F5AE68AE0A4F8A50A0D0D5B0E43B0C572CA40BEFDFE30E7EFC1163B4163B9B` |

Le circuit n'est pas annoncé clôturé par ces seules captures : la cadence Debug et les limites du modèle numérique de communication restent distinguées dans la fiche. Le banc temporaire et ses entrées de découverte doivent être retirés après conservation de ces résultats.

Le fichier `ElectronUlaFinalDiagnostics.cs` et son dossier devenu vide ont été supprimés après les passages. Reconstructions locales Debug/Release réussies en 8,99/7,80 s, zéro erreur et trois avertissements préexistants. La découverte filtrée sur `ElectronUlaFinalDiagnostics` est vide dans les deux assemblies reconstruites. Les captures PNG conservées restent hors index ; les références matérielles temporaires restent présentes pendant la clôture du circuit.

### Vérification transversale des horloges de trame

Le modèle actuel avance le compteur commun en mode sonore, mais le compteur de trame TX est appelé uniquement depuis le mode émission. Il manque donc encore une connexion : [ElectronFpga](https://github.com/hoglet67/ElectronFpga/blob/master/ElectronULA.vhd), bloc `coutbits`, avance le compteur de trame indépendamment du mode, tout en autorisant le décalage de DREG uniquement en émission. L'[analyse des feuilles Ferranti par hicks](https://stardot.org.uk/forums/viewtopic.php?t=13818) identifie également un cadencement commun et des interruptions possibles hors du mode principal. Le cœur [ElectronULACore d'adbsolutions](https://github.com/adbsolutions/ElectronULA/blob/master/VHDL/MAX%2010%20Board/ElectronULACore.vhd) reprend ce même bloc : ce n'est pas une troisième validation indépendante.

Correction ciblée retenue : produire aussi les frontières nominales de trame à partir du décompte sonore, faire avancer le compteur de bits lors de ces frontières, sans décaler le registre de données sonorement, et geler cet ensemble quand la source d'horloge est inhibée. La suppression du bit start de notre représentation de trame doit rester distincte d'un décalage des huit données. Les conditions exactes de l'entrée CASSCK, le compteur RX hors réception et son drapeau sans front restent à confronter séparément ; cette correction TX ne doit pas les annoncer validés.

### Validation du compteur TX en mode sonore

La correction des horloges inter-modes passe **211/211 tests ULA en Debug (6 s) et Release (2 s)**, puis **25/25 tests de la carte Electron dans chaque configuration (1 s)**. Aucun échec ni cas ignoré. Les avertissements xUnit2031, CS8604 et CS0649 des autres bancs restent inchangés.

Le nouvel oracle avance le compteur maître un front à la fois pour tous les diviseurs ; les cas de changement de mode vérifient la fin de trame et son interruption masquée en mode sonore, la conservation du bit de donnée pendant cette progression, puis la reprise en émission. Le mode inutilisé est comparé à un témoin qui ne reçoit aucune horloge : ses données, niveaux et interruptions doivent rester identiques à la reprise. Ces résultats portent sur le modèle numérique et la carte native, pas sur une mesure électrique de l'ULA originale.

Les huit captures invitées précédentes sont antérieures à cette correction. Elles ne sont pas présentées comme une nouvelle preuve des horloges inter-modes. L'entrée tape sans front, les horloges RX hors réception et le déclenchement RD Full actuellement forcé à l'entrée du mode émission restent ouverts.

### Conservation RX pendant l'inhibition des horloges

`TickSerial` ne réinitialise plus les bascules de réception à chaque cycle passé hors du mode entrée. Cette remise à zéro logicielle effaçait la phase, le comptage de porteuse et le suivi des fronts alors que le mode inutilisé n'apporte aucune horloge de communication. Le changement de sélection FE07 est maintenant distingué du reset à froid. L'absence déclarée de support en mode réception conserve pour l'instant son ancien traitement séparé ; elle n'est pas assimilée à une preuve du comportement électrique sans front.

Huit points d'interruption sont injectés avant/pendant la porteuse, au start et dans les données d'une trame A5. Le récepteur testé reçoit 1 000 cycles en mode inutilisé, puis reprend exactement la même suite de niveaux ; le témoin reste suspendu pendant cette durée. Les causes tape sont comparées à chaque cycle et les deux octets reçus doivent être A5. La première rédaction du banc comparait aussi RTC et fin d'affichage : sept cas échouaient parce que la vidéo continue à tourner pendant cette inhibition. Le masque du banc a été corrigé en 0x70, sans supprimer d'horloge vidéo dans le circuit.

Résultats après correction du banc : **219/219 ULA Debug (7 s), Release (2 s), 25/25 carte native Debug et Release (1 s chacune)**. Aucun cas ignoré. Les avertissements préexistants sont inchangés. Ces tests démontrent la conservation pendant l'inhibition ; ils ne démontrent pas encore le cadencement RX lorsque le mode sonore ou l'émission sélectionne une autre source. Celui-ci et la réception sans front restent à raccorder, ainsi que le retrait du RD Full forcé à FE07 une fois sa cause matérielle reproduite. Aucun nouveau PNG invité n'est associé à ce passage.

### Raccordement numérique des phases RX hors réception

Le chemin son/émission expose maintenant les frontières du compteur sur ses douze bits bas (quatre phases par frontière TX en émission). Ces fronts avancent les mêmes bascules de phase/données RX que les demi-ondes reçues ; CDATA conserve la dernière classification reçue au changement de mode. Une sélection FE07 seule ne force plus RD Full. Cette construction est un modèle numérique nominal des horloges partagées décrit par l'analyse Ferranti de hicks, pas une transcription prouvée des décodeurs S1/S11/S15 ni de CASSCK. En particulier l'entrée sans front conserve encore son ancien traitement, qui doit être repris séparément. Les tests doivent vérifier la cadence produite et distinguer ce modèle des observations électriques originales.

Validation de ce raccordement : **222/222 ULA Debug (7 s), Release (2 s), 25/25 carte native Debug et Release (1 s chacune)**. Le banc maître parcourt tous les diviseurs sonores et les frontières de phase en émission. Les trois nouvelles attentes RX partent du reset numérique : première disponibilité à 1 057 cycles en son PD=0, 16 897 en son PD=31 et 13 729 en émission. Ces délais découlent du modèle retenu ; ils ne constituent pas des mesures sur silicium. Une écriture de sélection FE07 seule ne produit aucune cause RX, puis la cause calculée déclenche son masque IRQ et disparaît à la lecture FE04.

Les tests de registres ne fabriquent plus RD Full par une sélection de mode : ils reçoivent des demi-ondes space après la porteuse. Le premier passage natif a révélé deux attentes de banc devenues invalides : après l'émission, le compteur RX peut être au milieu d'une trame et produire un octet E0 avant synchronisation de la lecture. Le banc attend désormais High Tone puis acquitte l'ancienne réception avant d'attendre A5. Les deux variantes PCM, avec/sans zéros aux transitions, passent après cette correction de protocole. La donnée A5 et la limite totale de durée restent vérifiées ; aucun reset supplémentaire de l'ULA n'est ajouté pour effacer cet état.

Une erreur de compilation initiale du banc maître (remise à zéro de son champ `ReceivePhase` insérée hors de l'objet oracle) a été corrigée avant exécution des suites. Aucun échec ni test ignoré au passage final ; avertissements préexistants inchangés. Les captures invitées antérieures restent des preuves de leur état de code précédent, pas une preuve nouvellement produite de ces délais RX. Le traitement de l'entrée sans front et les équations physiques CASSCK/S* restent ouverts, de même que la cadence Debug native déjà mesurée.

### Entrée stable : continuation nominale des phases

Le branchement logiciel `InputPresent -> ResetReceiver` est retiré : la présence d'un média n'est pas une entrée de reset de l'ULA. Sans signal, le banc présente un comparateur bas stable. Les demi-ondes valides gardent leur traitement existant ; après dépassement de la demi-onde space nominale, des phases espacées d'un quart de bit continuent à avancer RX avec CDATA retenu. La mesure saturée d'une longue impulsion reste invalide, mais n'arrête plus ces phases. Le front suivant réaligne la mesure et la phase.

Cette continuation fonctionnelle couvre l'absence de fronts sans générer de faux High Tone. Elle est explicitement nominale : les sources Ferranti/Hicks établissent le fonctionnement autonome des compteurs mais ne suffisent pas ici à prouver les équations et fronts de CASSCK/S15/DATACNT. Le quart de bit et le seuil retenus ne sont pas présentés comme des délais mesurés sur une ULA réelle. La disponibilité hors réception déjà raccordée ne prouve pas davantage ces équations.

Résultats de l'entrée stable : **228/228 ULA Debug (7 s), Release (2 s), 25/25 carte native dans chacune des configurations (1 s)**. Aucun cas ignoré au passage final. Le nouveau banc de compteur compare 100 000 cycles à un calcul de frontières indépendant, continue après la saturation à 65 535 horloges maîtres, puis vérifie le réalignement au front suivant. Trois états stables (support absent, niveau bas, niveau haut) produisent les événements RX nominaux sans High Tone ; une trame partielle continue sans reset à la disparition du média. Après une porteuse reconnue, CDATA haut reste retenu sans fabriquer de nouveau start.

Premier passage : 227 réussites et un échec dans l'ancien scénario de stop bas. Il supposait que déconnecter le transport effaçait la trame suivante en cours. L'attente est corrigée pour acquitter puis attendre la cause High Tone, avec borne de 24 bits, avant de vérifier la suppression de RD Full. Le banc de réarmement distingue aussi une simple disparition du média d'un vrai start suivi d'une nouvelle porteuse. Les transferts électriques des 256 valeurs et les deux retours PCM natifs restent verts.

Les feuilles Ferranti 4 et 10 ont été réexaminées : les connexions barrées du compteur et la boucle S15/CASSCK ne permettent pas encore une transcription électrique certaine. Les recherches supplémentaires n'ont pas fourni le cœur autonome de Hicks ; les remplacements dérivés d'ElectronFpga ne constituent pas une validation indépendante de ces portes. Aucun nouveau screenshot invité ni mesure de performance n'est associé à ce passage. Les preuves invitées seront à refaire après stabilisation de l'ensemble des communications, et les performances Debug déjà signalées restent à améliorer.

### Référence de coût après continuation RX

Banc temporaire local, sans fenêtre ni sortie audio Windows : chauffe d'au moins 500 ms par chemin, puis trois séries de 20 champs. Vidéo seule avec rappel mémoire adresse XOR page et palette déterministe ; ULA sonore PD=31 ; carte native sur ROM synthétique JMP C000 avec vidage PCM à chaque champ. Les champs vidéo mesurés sont des blocs de 40 000 cycles, donc une moyenne sur les alternances 312/313 lignes. Ces conditions diffèrent du précédent banc de clôture : comparer surtout les mesures avant/après de ce même diagnostic. Le premier lancement sans journal détaillé a terminé sans afficher les sorties du test ; il a été relancé avec le logger détaillé pour obtenir les valeurs.

| Chemin | Debug, trois séries ms/champ | Release, trois séries ms/champ |
|---|---|---|
| Vidéo 0 | 20,259 / 21,019 / 20,247 | 2,536 / 2,529 / 2,609 |
| Vidéo 4 | 23,506 / 21,168 / 21,570 | 2,486 / 2,542 / 2,476 |
| ULA son | 23,237 / 23,043 / 23,087 | 3,265 / 3,189 / 3,275 |
| Carte native | 28,785 / 28,686 / 29,708 | 3,661 / 3,621 / 4,227 |

Médiane native Debug 28,785 ms, soit 1,390 MHz de cycles de référence ; Release 3,661 ms, soit 10,926 MHz. La mesure hôte n'est pas la fréquence physique du CPU lorsque PHI est retenu. Les empreintes pixels sont identiques D/R : vidéo 0 et 4 `1D5398FC2E747683757923EE7D549B9A97E76989D716A38F4433A630BEFE2CDF`, ULA/carte noire `FF6335069B6E140EB47149D847AEA80BF7E2B06BD80AE9708AA382EFB3AE21EE`. Les deux images vidéo égales dans ce motif ne remplacent pas les tests distincts des huit sélecteurs.

Tentative de cache des colonnes DRAM : les 228 tests ULA et 25 natifs passent dans les deux configurations et les empreintes sont identiques. Après changement, Debug vidéo 0 : 22,488 / 23,192 / 21,232 ms ; vidéo 4 : 20,772 / 21,102 / 20,851 ; ULA son : 23,676 / 23,498 / 23,723 ; native : 30,323 / 32,352 / 31,011. Release vidéo 0 : 10,637 / 2,660 / 2,807 (première série atypique), vidéo 4 : 3,028 / 2,881 / 2,724 ; ULA son : 3,134 / 3,150 / 3,408 ; native : 3,857 / 4,015 / 3,773.

Le gain n'est pas établi. Les médianes natives 31,011 ms Debug et 3,857 ms Release sont moins bonnes que la référence 28,785/3,661 ; la variabilité interdit d'attribuer entièrement cet écart au cache. Cette tentative est retirée : pas de changement des options de compilation, de suppression de fronts ou de cache supplémentaire conservé pour un bénéfice non démontré. Le coût Debug reste ouvert.

Le cache `_evenColumn`/`_oddColumn` est effectivement retiré ; `_address` et les trois calculs de colonne aux mêmes phases sont restaurés. Aucun gain ne sera attribué à cette expérience.

Diagnostic `ElectronUlaCostDiagnostic.cs` et dossier temporaire vide supprimés. Assemblies locales reconstruites en Debug (11,29 s) et Release (8,11 s), zéro erreur, trois avertissements préexistants ; découverte filtrée vide pour ce diagnostic dans les deux configurations. Aucun fichier de mesure ni image ajouté à Git.

### Comparaison appariée de l'étape maître

Tentative : réunir arbitrage RAM et rendu d'un point dans `TickPoint`, renvoyer directement l'événement du séquenceur, supprimer des gardes de visibilité redondantes et précalculer l'offset de ligne. Première comparaison isolée après regroupement : Debug vidéo 0 25,258/25,874/24,330 ms ; vidéo 4 23,956/24,372/24,071 ; ULA 24,807/24,539/27,164 ; native 31,062/30,756/30,950, avec empreintes conservées.

Pour distinguer code et variabilité hôte, un banc temporaire compile le chemin précédent dans son propre namespace et le compare au nouveau dans le même processus, après chauffe commune et en alternant l'ordre des séries. La première compilation dans LocalEmulationTests a été refusée par les accès internes de Chips : la référence et sa comparaison ont été déplacées dans Chips.Tests, déjà autorisé, sans modifier les accès de production. Les empreintes des deux chemins sont comparées à chaque série.

Après les gardes/offset, comparaison finale avant/après (ms/champ) :

| Mode | Debug avant / après, trois paires | Release avant / après, trois paires |
|---|---|---|
| 0 | 21,194/21,108 ; 21,576/21,561 ; 21,078/20,975 | 2,792/2,698 ; 2,581/2,639 ; 2,619/2,640 |
| 4 | 22,314/21,293 ; 20,963/21,115 ; 21,858/21,530 | 2,624/2,713 ; 2,599/2,836 ; 2,617/2,692 |

228 tests ULA et la comparaison temporaire passent D/R ; 25 tests natifs et le diagnostic de coût passent D/R. Dernière mesure native du candidat : Debug 30,911/29,564/29,819 ms, Release 3,535/3,614/3,583, mêmes empreintes. La vidéo 0 Release du diagnostic isolé montre une forte variation (11,895/7,912/2,965) ; elle ne permet pas une conclusion de gain. Le résultat apparié n'est pas uniformément meilleur, notamment mode 4 Release : cette tentative est retirée. Aucun gain global n'est annoncé et les options Debug restent inchangées.

Le chemin précédent est restauré : `TickRam` puis `RenderPoint`, retour void de `ElectronRamSequencer.Tick`, sans offset ajouté. `ElectronVideo.Point.cs` est supprimé.

Nettoyage effectif : les deux diagnostics temporaires et les huit fichiers de référence sont supprimés, leurs dossiers devenus vides aussi. Chips.Tests reconstruit D/R en 2,19/1,95 s ; LocalEmulationTests en 18,67/16,06 s ; zéro erreur, avertissements préexistants seulement. La découverte filtrée est vide dans les quatre assemblies après correction d'une invocation qui avait omis le point de `Chips.Tests`. Aucune image ou référence temporaire n'est indexée. Le profileur `dotnet-trace` n'est pas installé ; son installation a été demandée pour la prochaine identification des coûts, sans prétendre qu'un profil a déjà été enregistré.

Le profileur officiel `dotnet-trace` 10.0.750501 est installé globalement après autorisation explicite. Les prochaines traces viseront le PID du testhost exécutant la carte, pas le processus MSBuild ni le lanceur des tests.

### Profilage échantillonné du thread natif

`dotnet-trace` 10.0.750501, fournisseur `Microsoft-DotNETCore-SampleProfiler`, six secondes de capture après chauffe. Le test publie son PID et l'heure de création du processus ; ceux-ci sont confrontés au processus vivant avant attachement. Le processus tracé est bien `testhost.exe` qui exécute `ElectronMachine`, et non dotnet/MSBuild. Le banc possède sa carte, la réinitialise et éjecte son transport en finally ; aucun périphérique audio Windows ni fenêtre n'est ouvert. Les deux tests de charge terminent correctement : 1 004 champs en 30,032 s Debug et 7 639 en 30,001 s Release. Ces débits incluent le coût de collecte et ne remplacent pas les mesures hors profileur.

Le rapport global initial mélangeait les threads de test en attente : il n'est pas utilisé pour attribuer les coûts. Les traces sont converties en Speedscope, les piles contenant `RunNativeBoardForSampling` sont sélectionnées, et les feuilles synthétiques CPU_TIME/BLOCKED_TIME sont retirées avant attribution à la dernière méthode réelle. Durées sélectionnées : 6 018,186 ms Debug et 6 013,232 ms Release ; respectivement 435 et 4 296 intervalles positifs. Il s'agit d'un échantillonnage court, pas d'un chronométrage exact de chaque appel.

| Méthode Debug | Temps propre estimé | Avec ses appels |
|---|---:|---:|
| ElectronVideo.TickRam | 10,11 % | 47,78 % |
| ElectronRamSequencer.Tick | 9,86 % | 25,17 % |
| ElectronVideo.RenderPoint | 8,84 % | 10,14 % |
| ElectronVideo.Tick | 7,79 % | 74,03 % |
| ElectronRamSequencer.Drive | 5,47 % | 12,24 % |
| Broches RAM de la carte | 4,95 % | 6,77 % |
| Cpu6502.ExecuteNmosPhase | 3,64 % | 95,85 % |
| ElectronVideo.TickCpuClock | 3,40 % | 3,92 % |
| ElectronVideoTiming.Advance | 2,87 % | 3,65 % |
| ElectronRamAddress.Column | 1,54 % | 1,54 % |

Les coûts inclusifs se recouvrent et ne doivent pas être additionnés. Le compteur de colonnes tenté précédemment ne représentait qu'une petite partie du coût : le profil cible maintenant le séquencement complet d'un point et ses étapes RAM, sans autoriser de suppression des fronts. L'importance du CPU dans la pile inclusive ne signifie pas que ses instructions absorbent ce temps : il appelle l'ULA via le bus.

En Release, les principaux symboles propres sont ExecuteNmosPhase (40,33 %), Cpu6502.Tick (26,91 %), ElectronRamSequencer.Tick (16,09 %) et StepNmos (13,00 %). Le JIT peut intégrer des méthodes vidéo/ULA dans ces symboles : ce rapport ne permet pas de réattribuer ces pourcentages au seul processeur émulé. Aucun gain ni correction de cadence n'est annoncé à partir de ces deux traces. La preuve invitée avec les dernières communications reste à produire.

Le test de profilage, les fichiers PID, les deux traces, leurs conversions et rapports ont été supprimés après consignation. Reconstructions locales D/R réussies (6,13/5,75 s), zéro erreur, trois avertissements préexistants ; découverte filtrée vide dans les deux assemblies. `dotnet-trace` reste installé comme autorisé.

### Preuve invitée des communications après partage du compteur

Une ROM de banc exécutée par le 6502 de la carte native contrôle POR puis son acquittement, RX différé avec IRQ et acquittement en mode sonore, TX Empty après émission, inhibition du compteur dans le mode inutilisé et réception sans média. Elle efface et dessine elle-même la RAM vidéo avant de publier son résultat. Le contrôle hôte compare séparément les étapes et tous les pixels natifs 640 × 256 ; il ne remplit pas la RAM écran à sa place.

Deux cas passent en Debug (2/2, 2,5710 s au total) et Release (2/2, répétition finale 1,6904 s). Le cas normal atteint A5 et les étapes 1/2/3/4 après 18 champs ; le cas volontairement erroné attend une mauvaise valeur POR et dessine FAIL avec E0 et les étapes 0/0/0/0, également après 18 champs. La première rédaction du banc contenait un déplacement relatif incorrect dans sa boucle d’attente ; ce défaut du programme de preuve a été corrigé avant validation, sans modification de la production.

Captures inspectées dans artifacts/TestsChips/ElectronUla/AcornElectron/ : comms-counter.png et comms-counter-fail.png. Les deux sont ignorées et aucune n’est indexée. Empreintes SHA256 des pixels : PASS 37657C414B4078F8192AA585D9DE2C7A1C02232E6486917CAE777E9924300715 ; FAIL E08695C703EBD6595898368312ED1790F5D61C42F9566E09D436DC4FD637DF46.

Le texte SOUND PASS désigne l’horloge de réception dans le mode sonore, et ne prouve ni la fidélité du haut-parleur ni une sortie audio Windows. Cette preuve ne valide pas les équations physiques encore ambiguës de CASSCK, toutes les fonctions de l’ULA ou ses performances. Aucun périphérique audio Windows, fenêtre ou Dispatcher n’est créé ; la carte est éjectée et réinitialisée en finally, les flux sont libérés par using.

Le banc ElectronCommsGuestProof et son dossier vide sont supprimés. Reconstructions locales Debug/Release réussies en 5,79/5,94 s, zéro erreur et trois avertissements préexistants ; la découverte filtrée est vide dans les deux assemblies. Les deux captures restent ignorées hors Git.

### Référence avant séparation de l’ordonnanceur RAM aligné

Banc temporaire ElectronRamCostDiagnostic, chauffe d’une seconde par chemin, cinq séries de quarante champs, adresse XOR page et palette i × 13 pour la vidéo, ROM JMP C000 et audio drainé pour la carte native. Sans fenêtre ni sortie audio Windows ; carte réinitialisée et transport éjecté en finally.

| Chemin | Debug, ms/champ | Release, ms/champ |
|---|---|---|
| Vidéo 0 | 20,535 / 21,982 / 20,982 / 21,083 / 21,651 | 2,786 / 2,558 / 2,571 / 2,578 / 2,551 |
| Vidéo 4 | 21,734 / 21,763 / 21,262 / 21,469 / 21,259 | 2,991 / 2,615 / 2,566 / 2,636 / 2,637 |
| Carte native | 32,972 / 30,244 / 33,300 / 31,500 / 30,764 | 4,174 / 3,749 / 3,707 / 3,711 / 3,732 |

Empreinte vidéo dans les deux modes et configurations : 1D5398FC2E747683757923EE7D549B9A97E76989D716A38F4433A630BEFE2CDF. Carte native : FF6335069B6E140EB47149D847AEA80BF7E2B06BD80AE9708AA382EFB3AE21EE. Ces deux motifs ne remplacent pas les contrôles des fronts et des changements en cours de cycle.

### Séparation de l’ordonnanceur RAM aligné : résultat

Le groupe aligné exécute directement les huit phases du séquenceur et les huit points vidéo. Le début de transaction n’est plus recherché par masque dans un appel TickRam à chaque point ; la fin est signalée par l’événement renvoyé par Tick. Le chemin scalaire conserve ses contrôles par point. PHI est actualisé aux frontières et dès que son entrée hold est changée par un rappel réentrant. Les événements intermédiaires LD0/LD1, les fronts RAS/CAS/WE et le contenu des latches restent exposés.

Le test réentrant PHI existant couvre désormais aussi le chemin aligné : le rappel du point 0 impose hold, celui du point 1 le retire ; les fronts observés sont 0 bas, 1 haut, 2 bas, 4 haut. Les 229 contrôles ULA et les 25 contrôles natifs passent dans les deux configurations (254 au total, hors banc de mesure). Les contrôles vidéo scalaires/groupés existants restent verts.

| Chemin | Debug après, ms/champ | Release après, ms/champ |
|---|---|---|
| Vidéo 0 | 20,593 / 20,407 / 20,407 / 20,291 / 20,518 | 19,623 / 5,891 / 2,658 / 2,593 / 2,594 |
| Vidéo 4 | 19,894 / 20,459 / 19,915 / 19,855 / 19,712 | 2,649 / 2,586 / 2,553 / 2,639 / 2,497 |
| Carte native | 31,741 / 29,493 / 29,106 / 29,734 / 30,105 | 3,713 / 3,708 / 3,715 / 3,909 / 3,806 |

Les empreintes de pixels sont inchangées pour les trois chemins. Le gain observé Debug est modeste : médianes vidéo 0 de 21,083 à 20,407 ms, vidéo 4 de 21,469 à 19,894 ms, carte de 31,500 à 29,734 ms. Les séries natives se recouvrent et ne démontrent pas un pourcentage universel. Le changement retire effectivement des appels et contrôles répétés et est conservé avec ce gain limité ; aucune baisse de fréquence ou option de compilation n’est utilisée.

En Release, les deux premières séries vidéo 0 montrent que la chauffe temporelle d’une seconde n’a pas suffi à stabiliser cette exécution. Elles sont conservées dans le tableau plutôt que masquées ; les trois suivantes sont proches du coût de référence. La carte reste autour de 3,7 ms. Aucun gain Release significatif ni résolution de la cadence Debug n’est établi. Une comparaison plus précise devra stabiliser la compilation à étages avant d’attribuer de petits écarts.

Le diagnostic ElectronRamCostDiagnostic et son dossier vide sont supprimés après mesure. Reconstructions locales Debug/Release réussies en 5,96/5,87 s, zéro erreur et trois avertissements préexistants ; découverte filtrée vide dans les deux assemblies.

### Référence avant retrait du dessin pendant le blanking horizontal

Banc ElectronBlankingCostDiagnostic, chauffe portée à trois secondes par chemin, trois séries de quarante champs ; mêmes adresses XOR page, palette et ROM de boucle que le banc précédent, sans fenêtre ni audio Windows, nettoyage en finally.

| Chemin | Debug avant, ms/champ | Release avant, ms/champ |
|---|---|---|
| Vidéo 0 | 19,874 / 20,189 / 20,550 | 2,600 / 2,623 / 2,644 |
| Vidéo 4 | 20,144 / 20,667 / 20,453 | 2,685 / 3,129 / 2,752 |
| Carte native | 29,070 / 30,967 / 29,728 | 3,697 / 3,648 / 3,682 |

Empreintes inchangées : vidéo 1D5398FC2E747683757923EE7D549B9A97E76989D716A38F4433A630BEFE2CDF ; carte FF6335069B6E140EB47149D847AEA80BF7E2B06BD80AE9708AA382EFB3AE21EE.

### Essai de blanking : candidat retiré

Le candidat évitait RenderPoint dans les groupes horizontaux entièrement hors des 640 points visibles et remettait PixelColor à zéro après chaque phase RAM. Il conservait toutes les transactions mémoire et notifications PHI. Les comparaisons points/groupes sont étendues aux huit modes, alignés et désalignés ; un nouveau contrôle observe le dernier pixel blanc au rappel PHI du point 640, puis le noir au point 644, dans les deux chemins. Les 243 contrôles ULA et 25 contrôles natifs passent Debug/Release, hors diagnostic.

| Chemin | Debug candidat, ms/champ | Release candidat, ms/champ |
|---|---|---|
| Vidéo 0 | 21,144 / 20,757 / 20,951 | 2,695 / 2,633 / 2,670 |
| Vidéo 4 | 21,202 / 20,869 / 20,616 | 2,635 / 2,651 / 2,624 |
| Carte native | 28,878 / 28,731 / 29,334 | 4,251 / 4,147 / 4,244 |

Toutes les empreintes sont inchangées. Aucun gain global n’est établi : médiane native Debug 29,728 vers 28,878 ms, mais Release 3,682 vers 4,244 ms ; vidéo 0 Debug également plus lente. Le candidat est retiré et le chemin RenderPoint à chaque point restauré. Les contrôles supplémentaires sont conservés car ils couvrent les huit modes et la frontière de blanking indépendamment de cet essai. Aucune modification de compilation n’est conservée.

Après retrait du candidat, les 243 contrôles ULA et 25 contrôles natifs passent de nouveau Debug/Release (268 au total par configuration). Le banc temporaire et son dossier vide sont supprimés ; les assemblies locales sont reconstruites et leur découverte filtrée est vide dans les deux configurations. Aucun nouveau fichier de capture ou de trace n’est ajouté.

### Aller-retour tape avec le MOS/BASIC Electron existant

Le banc temporaire démarre la carte native avec les deux moitiés du fichier existant Acorn Electron BASIC MOS.bin, emprunté sans modification dans le dossier Firmware utilisateur. SHA256 du fichier combiné de 32 Kio : 0BA611B03137E35BCD87F84F2ACFA4426511AD7EDB0B830691BAE3CFF28DD20B. Le banc ne réimplémente pas les routines tape du MOS et ne charge pas directement leur résultat.

Préparation : le banc place une permutation des 256 valeurs d’octet à 2000–20FF, au moyen des accès physiques de la carte. Il présente une tape PCM vide inscriptible, active Record puis saisit au clavier *SAVE TEST 2000 2100 et la confirmation Entrée demandée par le MOS. Après émission, il conserve le signal PCM produit par les broches ULA, efface les 256 octets, réinsère ce signal en lecture et saisit *LOAD TEST 2000. Il compare alors chacun des octets rechargés. Le flux compte 494 694 échantillons à 48 kHz dans les deux configurations ; empreinte SHA256 des octets rechargés : 4039FD7B788657FF51DAA6E50A9110E5C7F87181F931F501D55C3AB17BF5C19B.

BASIC exécute ensuite S=0:FOR I=8192 TO 8447:S=S+?I:NEXT et la condition sur S=32640 pour afficher TAPE PASS ou TAPE FAIL. Les pixels sont ceux de l’écran natif, non un texte dessiné par l’hôte. La vérification de la somme dans BASIC complète la comparaison hôte exhaustive ; la somme seule ne prouverait pas l’ordre des octets.

Résultat final : Release réussi en 10 s de test (11,6793 s au total), Debug réussi en 1 min 12 s de test (1,2270 min au total), mêmes données et même longueur de signal. La première rédaction du banc avait omis la seconde Entrée après RECORD then RETURN ; les commandes suivantes étaient alors consommées par cette attente. Les premières captures ont permis d’identifier et corriger ce défaut du banc, sans changer l’ULA.

Captures inspectées sous artifacts/TestsChips/ElectronUla/AcornElectron/ : mos-tape-boot.png, mos-tape-save.png et mos-tape-load.png ; la dernière montre la sauvegarde, Searching/Loading, le nom TEST, le calcul BASIC et TAPE PASS. Elles sont ignorées et aucune n’est indexée.

Portée : démarrage du firmware, entrée clavier, affichage natif, sortie puis entrée tape nominale, interruptions et routines de bloc/CRC utilisées par cette version du MOS. Ce n’est pas une validation de tous les firmwares, des variations de vitesse de bande, des conteneurs UEF/TAP/WAV, d’un périphérique audio Windows ou des ambiguïtés analogiques restantes. Aucun fichier de configuration ou média utilisateur n’est modifié ; la tape de preuve n’existe qu’en mémoire, la carte est éjectée et réinitialisée en finally, les fichiers PNG et flux compressés sont fermés par using.

Le banc MOS et son dossier vide sont supprimés après ces preuves. Reconstructions locales Debug/Release réussies en 10,90/7,07 s, zéro erreur et trois avertissements préexistants ; découverte filtrée vide dans les deux assemblies. Les captures seules restent hors Git.

### Référence avant tables des permutations RAM

Même banc vidéo 0/4 et carte native, chauffe de trois secondes, trois séries de quarante champs, aucune sortie Windows.

| Chemin | Debug, ms/champ | Release, ms/champ |
|---|---|---|
| Vidéo 0 | 22,457 / 22,768 / 22,075 | 2,808 / 2,583 / 2,693 |
| Vidéo 4 | 21,781 / 23,350 / 20,738 | 2,549 / 2,510 / 2,577 |
| Carte native | 28,657 / 28,650 / 28,907 | 3,961 / 4,018 / 4,041 |

Empreintes vidéo 1D5398FC2E747683757923EE7D549B9A97E76989D716A38F4433A630BEFE2CDF, carte FF6335069B6E140EB47149D847AEA80BF7E2B06BD80AE9708AA382EFB3AE21EE. Les durées de ces séries ne doivent pas être mélangées aux passages antérieurs pour calculer un gain.

### Tables de permutations : candidat retiré

Les petites tables d’adresses et de groupes de données ne stockaient aucun octet RAM ; elles remplaçaient seulement les permutations fixes. Les 243 contrôles ULA et les 25 contrôles natifs passent Debug/Release, notamment les oracles indépendants de câblage et les 65 536 combinaisons physiques.

| Chemin | Debug candidat, ms/champ | Release candidat, ms/champ |
|---|---|---|
| Vidéo 0 | 21,722 / 21,449 / 21,878 | 2,441 / 2,576 / 2,507 |
| Vidéo 4 | 20,587 / 20,409 / 20,383 | 2,650 / 2,507 / 2,494 |
| Carte native | 28,548 / 28,941 / 30,129 | 4,376 / 5,289 / 5,508 |

Empreintes inchangées. Le gain vidéo ne se traduit pas par un gain natif : médiane Debug 28,657 vers 28,941 ms, Release 4,018 vers 5,289 ms. La variabilité interdit d’attribuer tout l’écart aux tables, mais ne permet pas de conserver ce candidat comme une optimisation démontrée. Les trois fichiers de production sont restaurés à leur état avant cet essai ; aucun nouveau cache n’est conservé.

Le diagnostic de permutations et son dossier vide sont supprimés. Les assemblies locales Debug/Release sont reconstruites avec zéro erreur et trois avertissements préexistants ; la découverte filtrée est vide dans les deux configurations.

### Ordonnancement direct des huit phases RAM

Le chemin aligné est maintenant explicite dans ElectronVideo.Clock.cs : PHI bas puis RAS au point 0, colonne paire au point 1, point de maintien 2, chargement pair et relâchement CAS au point 3, PHI haut puis colonne impaire au point 4, maintien 5, chargement impair et relâchement des strobes au point 6, fin de transaction au point 7. Le dessin reste après l’action RAM de chacun des huit points, y compris dans le blanking. Toute modification réentrante de hold est traitée avant le point suivant.

Les actions sont partagées avec le Tick scalaire de ElectronRamSequencer ; aucun décodeur mémoire parallèle, cache de contenu, strobe sauté ou changement de compilation n’est ajouté. Les points de maintien gardent leurs événements logiciels et l’avancement de phase. Les annulations libèrent les broches et empêchent les actions suivantes comme auparavant.

ClockSequenceTests ajoute 48 cas : huit modes, absence d’effet secondaire/changement de mode/annulation CPU, déclenchement sur chacun des deux strobes CAS. Chaque cas compare la totalité de la trace RAS/CAS/WE/adresse/données, PHI et la couleur observée par les broches, puis l’état vidéo/CPU après deux périodes, entre les points individuels et l’ordonnancement aligné. Les oracles indépendants du séquenceur restent également verts.

Résultats : 291 contrôles ULA et 25 contrôles natifs réussis Debug/Release, hors diagnostic (316 par configuration). Le diagnostic reprend exactement les chemins et conditions de la référence avant permutations : trois secondes de chauffe, trois séries de quarante champs, ROM JMP C000 et drainage audio natif, sans fenêtre ni audio Windows ; les propriétaires sont réinitialisés/éjectés en finally. Les trois fichiers de permutations avaient été restaurés avant ce changement.

| Chemin | Debug après, ms/champ | Release après, ms/champ |
|---|---|---|
| Vidéo 0 | 18,214 / 18,168 / 18,110 | 1,886 / 2,145 / 1,969 |
| Vidéo 4 | 18,334 / 18,225 / 18,037 | 1,824 / 1,846 / 1,841 |
| Carte native | 26,365 / 26,414 / 26,885 | 3,388 / 3,353 / 3,883 |

Empreintes inchangées : vidéo 1D5398FC2E747683757923EE7D549B9A97E76989D716A38F4433A630BEFE2CDF ; carte FF6335069B6E140EB47149D847AEA80BF7E2B06BD80AE9708AA382EFB3AE21EE. Les médianes natives passent de 28,657 à 26,414 ms Debug et de 4,018 à 3,388 ms Release ; le gain est aussi présent sur les deux chemins vidéo et le changement est conservé. Ces mesures sont des bancs isolés, pas le FPS affiché dans l’application ni une garantie de gain identique pour chaque logiciel invité.

La carte complète reste au-dessus des 20 ms nominaux en Debug. La vidéo isolée est maintenant en dessous de cette durée, ce qui ne suffit pas à déclarer la cadence native Debug corrigée. Les captures MOS/tape précédentes ne sont pas présentées comme nouvellement produites après cet ordonnancement ; les contrôles de non-régression et empreintes ci-dessus portent bien sur le code actuel.

Le banc ElectronClockCostDiagnostic et son dossier vide sont supprimés. Reconstructions locales Debug/Release réussies en 5,86/6,63 s, zéro erreur et trois avertissements préexistants ; les deux découvertes filtrées sont vides. Aucun fichier de référence ou capture n’est indexé par ce passage.

### Référence fraîche avant représentation des latches de données

Banc vidéo 0/4 et carte native identique, trois secondes de chauffe, trois séries de quarante champs. Les nouvelles séries montrent une charge/variabilité hôte plus élevée que le passage précédent ; aucun gain ne sera calculé en mélangeant ces références.

| Chemin | Debug avant, ms/champ | Release avant, ms/champ |
|---|---|---|
| Vidéo 0 | 20,044 / 22,381 / 22,378 | 2,329 / 2,547 / 2,664 |
| Vidéo 4 | 20,950 / 20,984 / 19,020 | 2,188 / 2,199 / 2,212 |
| Carte native | 30,821 / 29,686 / 29,225 | 3,983 / 3,885 / 3,817 |

Empreintes inchangées : vidéo 1D5398FC2E747683757923EE7D549B9A97E76989D716A38F4433A630BEFE2CDF ; carte FF6335069B6E140EB47149D847AEA80BF7E2B06BD80AE9708AA382EFB3AE21EE.

### Latches de données : représentation des deux groupes physiques

ElectronRamData conserve désormais les deux prises de quatre broches dans un octet privé : groupe pair dans le demi-octet bas, impair dans le haut. Chaque LD remplace uniquement son groupe. Une table privée de 256 octets traduit cette combinaison en octet logique lorsque Data est consulté. Elle est construite une seule fois depuis le câblage existant, ne stocke aucun contenu de la RAM et n’ajoute aucun état temporel. La valeur intermédiaire reste consultable après chacun des deux strobes ; les bits hauts non connectés des entrées sont toujours ignorés. Les phases RAS/CAS/WE, le rafraîchissement, les écritures partielles et leurs annulations ne sont pas modifiés.

Les 291 contrôles ULA et 25 contrôles natifs passent dans les deux configurations. Les oracles de câblage vérifient notamment chaque ancien octet et les 256 valeurs présentées sur chaque prise, la conservation de l’autre groupe et la lecture entre strobes. Aucun nouveau test permanent qui reflète seulement la construction de la table n’est ajouté.

Mesure du même banc natif/vidéo après changement, exécutée séparément des tests fonctionnels :

| Chemin | Debug après, ms/champ | Release après, ms/champ |
|---|---|---|
| Vidéo 0 | 18,309 / 18,022 / 18,841 | 2,052 / 2,761 / 1,921 |
| Vidéo 4 | 18,373 / 20,443 / 18,505 | 1,818 / 1,882 / 1,932 |
| Carte native | 26,839 / 26,237 / 27,028 | 3,286 / 3,287 / 3,286 |

Les empreintes vidéo/native restent identiques à la référence fraîche. La baisse murale native de 29,686 à 26,839 ms Debug et de 3,885 à 3,286 ms Release ne peut pas être attribuée entièrement au code : les durées de découverte et les séries montrent aussi une variation hôte. Aucun pourcentage global n’est déduit de ces seuls passages.

Pour isoler le composant, DataLatchPairedDiagnostic conserve dans le même processus une copie de la représentation précédente. Après une seconde de chauffe par chemin, cinq séries d’un million de transferts alternent l’ordre référence/candidat ; les entrées varient, les sommes doivent être identiques. Deux densités de lecture sont testées : chaque transfert ou un transfert sur deux. Ce banc mesure le composant, pas la carte entière.

| Configuration et lectures | Référence, ms/Mtransferts | Nouveau, ms/Mtransferts | Somme |
|---|---|---|---:|
| Debug, chaque transfert | 22,709 / 23,520 / 23,172 / 24,277 / 24,634 | 7,867 / 8,906 / 7,921 / 8,181 / 7,885 | 127500000 |
| Debug, un sur deux | 22,158 / 22,243 / 23,823 / 21,971 / 21,698 | 6,444 / 6,438 / 6,685 / 6,443 / 6,446 | 63000000 |
| Release, chaque transfert | 3,653 / 3,639 / 3,625 / 3,680 / 3,671 | 3,699 / 3,671 / 3,748 / 3,682 / 3,678 | 127500000 |
| Release, un sur deux | 3,667 / 3,670 / 4,409 / 3,671 / 3,662 | 3,633 / 3,604 / 3,865 / 3,604 / 3,628 | 63000000 |

Deux cas réussis Debug et Release. Le gain du composant est net en Debug ; les petits écarts Release sont pratiquement neutres et ne démontrent pas un gain global. La représentation est conservée pour réduire les opérations et appels Debug, avec seulement 256 octets de table, sans changer les options de compilation ni le contrat des broches. La cadence native Debug reste au-dessus de 20 ms/champ.

Les deux diagnostics de latches et le dossier local vide sont supprimés après consignation. Les assemblies Chips.Tests et LocalEmulationTests sont reconstruites en Debug/Release ; les quatre découvertes filtrées sont vides, zéro erreur de compilation, avertissements préexistants seulement. Aucun artefact de test n’est ajouté à l’index.

### Rendu à couleur unique : comparaison appariée

La couleur déjà émise est conservée dans le même ordre de composantes que le pixel écrit dans le tampon natif. La palette logique redondante est retirée ; PixelColor restitue toujours son ancien ordre logique en convertissant cette valeur latched lors de sa lecture. Une écriture de palette ne change donc pas rétroactivement le point déjà émis. Le contrôle existant PaletteAndModeChangesUseLatchedByteAtNextPoint vérifie maintenant aussi cette lecture avant le point suivant. Les sorties anticipées du rendu garantissent les bornes visibles : les conditions identiques répétées plus bas sont retirées sans changer les points exécutés, les adresses ou les transactions RAM.

Une copie temporaire des six fichiers de l’ancien séquenceur vidéo est compilée dans le projet de tests, partageant les mêmes circuits RAM et temps que le nouveau rendu. PixelPairedDiagnostic compare ancien/nouveau dans un même processus, chauffe une seconde chaque chemin puis alterne trois séries de quarante champs. Chaque paire doit produire exactement le même tampon, pas seulement une couleur ou une taille d’image.

| Configuration / mode | Ancien rendu, ms/champ | Nouveau rendu, ms/champ |
|---|---|---|
| Debug / 0 | 18,907 / 18,750 / 19,276 | 18,373 / 18,215 / 18,106 |
| Debug / 4 | 18,265 / 18,150 / 18,172 | 17,002 / 17,032 / 16,985 |
| Release / 0 | 2,087 / 1,978 / 2,012 | 1,895 / 1,862 / 1,966 |
| Release / 4 | 1,852 / 1,826 / 1,837 | 1,796 / 1,817 / 1,830 |

Deux cas appariés réussissent dans chaque configuration. Le gain est modeste mais présent dans ces quatre comparaisons ; le changement est conservé. Empreinte commune des tampons : 1D5398FC2E747683757923EE7D549B9A97E76989D716A38F4433A630BEFE2CDF.

Les 291 contrôles ULA et 25 contrôles natifs passent séparément Debug/Release, hors diagnostics. Le banc natif utilise la ROM de boucle, trois secondes de chauffe et trois séries de quarante champs, sans fenêtre ni audio Windows, carte libérée en finally : Debug 25,746 / 25,740 / 25,984 ms ; Release 2,955 / 2,970 / 2,860 ms. Empreinte FF6335069B6E140EB47149D847AEA80BF7E2B06BD80AE9708AA382EFB3AE21EE inchangée. Aucune référence native simultanée ancienne n’est contenue dans ce dernier banc ; ces durées ne prouvent donc pas à elles seules un pourcentage natif.

La conversion logique de PixelColor est désormais payée par le lecteur de cette propriété, tandis que l’Electron natif consomme les tampons d’image. Aucun gain universel n’est annoncé pour un consommateur qui lirait PixelColor à chaque point. Le contrat de valeur reste vérifié par les tests de plans de bits, palette, transitions et observations de broches. La cadence native Debug reste supérieure à 20 ms/champ et la puce n’est pas clôturée sur cette seule optimisation.

Les six sources de référence du rendu et les deux diagnostics temporaires sont supprimés, ainsi que leurs dossiers vides. Les assemblies Chips.Tests et LocalEmulationTests sont reconstruites Debug/Release avec zéro erreur ; les quatre découvertes filtrées sont vides. Aucun nouveau fichier de capture ni artefact n’est indexé.

## Compteur partagé : transitions internes sans accesseurs — 4 novembre 2026

Le compteur conserve ses propriétés de lecture pour ses consommateurs. Ses transitions utilisent directement les champs privés ; une durée d’entrée déjà saturée n’est plus réassignée, mais la phase RX continue. Aucun changement des diviseurs, périodes, modes ou seuils de réception.

Comparaison temporaire dans le même processus, ancienne classe copiée avant modification, un million d’appels par série, chauffe de deux secondes puis cinq séries alternant l’ordre. Médianes en millisecondes :

| Source | Debug avant | Debug après | Release avant | Release après |
|---|---:|---:|---:|---:|
| Entrée stable | 11,207 | 6,449 | 1,909 | 2,171 |
| Son, diviseur 31 | 20,468 | 11,615 | 1,663 | 1,787 |
| Sortie | 20,330 | 14,552 | 1,478 | 1,354 |

Les trois états finaux/sommes comparées sont identiques à chaque série : entrée 629669884, son 256129026, sortie 157747200. Le gain Debug du composant est établi sur ce banc. Les petites différences Release ne démontrent aucun gain uniforme ; la dispersion des séries interdit de leur attribuer un pourcentage global fiable.

Carte native seule, boucle JMP C000, trois secondes de chauffe puis trois séries de quarante champs avec DrainAudio, aucune sortie audio Windows : Debug **25,614 / 25,386 / 24,984 ms/champ**, Release **2,733 / 2,775 / 2,682 ms/champ**. Empreinte des pixels dans chaque série : `FF6335069B6E140EB47149D847AEA80BF7E2B06BD80AE9708AA382EFB3AE21EE`. La première mesure Debug lancée avec les tests fonctionnels (26,012 / 26,875 / 29,352) est écartée de la comparaison de coût ; la série citée est exécutée séparément. Les écarts avec les passages antérieurs ne constituent pas une mesure appariée de la carte entière.

Les **291 contrôles ULA et 25 contrôles natifs passent dans chacune des configurations Debug et Release**, sans contrôle ignoré. Aucun nouveau screenshot ni build livré de l’application n’est annoncé pour cette modification. L’ULA reste en cours ; les horloges tape continuent même sans média.

## Calcul des frontières temporelles — 4 novembre 2026

Les six comparaisons de franchissement sont évaluées directement dans Advance ; les compteurs utilisent des champs privés, avec les mêmes propriétés de lecture. Pas de raccourci limité aux points alignés : les pas unitaires et les pas de huit points gardent les mêmes inégalités.

Le banc apparié compare 400 000 états successifs sur des pas de 1/8 points, avec désalignement et changements de mode : PictureClock, ligne, point, nombre de champs, impulsions de fin de champ/RTC/DEND et synchronisation composite sont identiques. Après chauffe, cinq séries alternées de deux millions de périodes CPU donnent, en ms :

| Série | Debug avant | Debug après | Release avant | Release après |
|---|---:|---:|---:|---:|
| 1 | 77,778 | 46,749 | 4,936 | 4,115 |
| 2 | 78,350 | 47,371 | 4,626 | 4,858 |
| 3 | 80,644 | 46,007 | 4,994 | 4,960 |
| 4 | 105,295 | 56,003 | 4,978 | 4,986 |
| 5 | 92,375 | 59,760 | 4,511 | 4,106 |

Le gain Debug du calcul temporel est retenu ; pas de gain Release uniforme annoncé. Carte native seule, même boucle et protocole que le passage précédent : Debug **25,251 / 24,740 / 25,187 ms/champ**, Release **3,521 / 3,325 / 2,983 ms/champ**. Chaque empreinte est `FF6335069B6E140EB47149D847AEA80BF7E2B06BD80AE9708AA382EFB3AE21EE`. La variation entre séries natives, notamment Release, ne permet pas d’attribuer leur différence à ce changement seul. Les **291 tests ULA et 25 tests natifs passent en Debug et Release**, aucun ignoré.

## Preuve MOS renouvelée après les optimisations — 4 novembre 2026

Passage Release sur la carte Electron native, firmware emprunté sans modification : SHA256 `0BA611B03137E35BCD87F84F2ACFA4426511AD7EDB0B830691BAE3CFF28DD20B`. Le banc temporaire saisit au clavier `*SAVE TEST 2000 2100`, acquitte RECORD then RETURN, enregistre les sorties réelles du modem, efface la RAM 2000–20FF puis saisit `*LOAD TEST 2000` en relisant ce même PCM.

- Signal produit : **489 895 échantillons à 48 kHz**. La longueur diffère du précédent banc, dont l’ordonnancement de saisie/attente n’était pas identique ; aucun comparatif bit à bit des deux signaux n’est annoncé.
- Les 256 octets `(index * 73) XOR A5`, tronqués à huit bits, sont tous égaux après rechargement ; SHA256 `4039FD7B788657FF51DAA6E50A9110E5C7F87181F931F501D55C3AB17BF5C19B`.
- BASIC calcule ensuite `S=0:FOR I=8192 TO 8447:S=S+?I:NEXT`, puis teste `S=32640` et affiche **TAPE PASS**. La capture de relecture a été ouverte et vérifiée, ainsi que celles de boot et de sauvegarde.
- Pixels de la dernière capture, tampon natif BGRA : SHA256 `E46BC5698789C7DD37F5BFF4D704022DE557CD022167C7E20C042869C1B58910`.
- Captures PNG natives 640×256 : `artifacts/TestsChips/ElectronUla/AcornElectron/final-mos-boot.png`, `final-mos-save.png`, `final-mos-load.png`. Elles ne sont pas indexées ; elles montrent le MOS/BASIC exécuté, sans légende PASS ajoutée par l’hôte.

Passage réussi en 9 secondes de test (10,89 secondes avec découverte). La carte est éjectée/réinitialisée et son entrée clavier libérée en finally ; aucune configuration ni image média de l’utilisateur n’est écrite. Cette preuve renouvelle le transfert MOS réel, sans prétendre tester à elle seule toutes les fonctions de l’ULA ni ses transitions analogiques inconnues.

## Dernier essai écarté et décision de validation — 4 novembre 2026

L’essai d’état de ligne préactualisé conservait les pixels et adresses sur 640 000 points, avec changement de mode tous les onze points, mais ne montrait pas de gain régulier. Comparaison vidéo appariée en ms/champ (trois séries alternées) :

| Mode | Debug avant | Debug essai | Release avant | Release essai |
|---|---|---|---|---|
| 0 | 15,594 / 16,565 / 16,718 | 16,225 / 16,241 / 16,587 | 1,738 / 1,844 / 1,990 | 1,756 / 1,716 / 1,803 |
| 4 | 15,650 / 15,638 / 17,338 | 17,841 / 16,969 / 16,388 | 1,793 / 1,654 / 1,675 | 1,647 / 1,682 / 1,722 |

Cet essai est entièrement retiré ; les deux fichiers de rendu sont rétablis depuis leur copie immédiatement antérieure, qui correspond à la preuve MOS renouvelée. Aucun banc natif supplémentaire n’est lancé pour un changement déjà rejeté. Le banc natif préparé mais non exécuté est supprimé avec les copies et comparatifs. L’utilisateur autorise explicitement la validation lorsque les optimisations restantes ne donnent pas mieux. Cette décision ne transforme pas les limites analogiques documentées ni les performances Debug en garanties inexistantes.

## Bilan de validation du code retenu — 4 novembre 2026

L’utilisateur autorise la validation de l’état déjà satisfaisant lorsque les derniers essais d’optimisation n’améliorent pas le résultat. Les optimisations sans gain mesuré sont retirées ; les horloges, transactions de fond et signaux matériels ne sont pas supprimés pour accélérer le banc. La compilation Debug n’est pas convertie en compilation optimisée.

Le code retenu correspond à la preuve MOS `final-mos-*`, après restitution exacte des deux fichiers de rendu de l’essai de ligne rejeté. Le dernier passage global donne **2 364/2 364 Chips Debug (8 s), 25/25 Electron Debug (1 s), 2 364/2 364 Chips Release (5 s), 25/25 Electron Release (0,974 s)**. Aucun test ignoré. La découverte des diagnostics retirés est vide dans les quatre assemblies reconstruits ; les avertissements préexistants xUnit2031, CS8604 et CS0649 ne sont pas attribués à ce travail.

Aucune fonction numérique documentée identifiée dans cet examen ne reste marquée comme non réalisée : la matrice initiale couvre registres, reset, bus/attentes/NMI, broches DRAM et rafraîchissement, vidéo/palette/adressage/synchronisations, interruptions, son et modem. La sélection clavier et les ROM restent câblées par la carte, sans dépendance Chips vers Acorn. Les schémas et références exploités sont cités dans l’historique. Les équations analogiques/portes barrées non déterminées ne sont pas déclarées électriquement reproduites ; le modèle nominal et sa portée restent visibles dans cette fiche, conformément à l’acceptation des limites documentaires.

La validation ne promet pas 50 FPS en Debug : la dernière mesure native nominale est de l’ordre de **25 ms/champ Debug**, contre **environ 3 ms/champ Release**, sans limitation de vitesse. Les variations mesurées sont conservées au lieu de choisir seulement le meilleur passage. Les preuves MOS valident une sauvegarde et relecture réelles, pas un inventaire de compatibilité des logiciels ni de tous les formats média : ces vérifications appartiennent au travail machines/médias restant.

## Build de livraison vérifié — 4 novembre 2026

`scripts/local-building.cmd --building=debug --modules=A` se termine avec le code **0** et annonce les 24 modules. Vérification depuis leurs 24 manifests source : exactement un manifest livré par identifiant et présence de son entryAssembly. L’exécutable `build/Debug/Retro Media Lab/retromedialab.exe` est présent (345 600 octets), ainsi que `build/Debug/Retro Media Lab/lib/retromedialab.chips.dll` (527 360 octets). Aucun lancement de l’application utilisateur ni modification de sa configuration n’est nécessaire à cette vérification.

Les diagnostics temporaires sont absents des quatre assemblies de tests reconstruits. Le dernier essai de rendu sans gain est retiré ; la preuve MOS renouvelée correspond au code de ce build. Les avertissements de plateformes des autres modules pendant le build ne sont pas corrigés hors du périmètre de l’ULA.

## Clôture

Le travail de l’ULA est enregistré dans un commit distinct, avec son raccordement natif et le contrat optionnel de présentation du bus nécessaire au 6502. Le cache `artifacts/.tmp-electron-ula/` est supprimé après les examens ; les captures restent dans `artifacts/TestsChips/ElectronUla/AcornElectron/`, hors de l’index Git. Les diagnostics et copies de référence temporaires ne font pas partie du commit. `docs/prompts-objectifs.md`, étranger à ce lot, reste intact et non indexé. Le reste de l’objectif puces/machines/médias n’est pas déclaré terminé.
