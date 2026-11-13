# ULA série BBC — SERPROC (Acorn 201602 / 2C199)

## Aller-retour tape par les ROM — 7 novembre 2026

Complément : la même méthode passe ensuite par les quatre conteneurs proposés (WAV, UEF, CSW, RAW). Les quatre programmes sont rechargés par le MOS et exécutés, avec 11 381 762 cycles identiques sur la portion mesurée ; 17,95–19,24 ms/image Debug. [Protocole, tailles, limites et captures par format](<../../project/emulations/Acorn/BBC Micro/Model A/tape-formats-validation.md>). Aucune modification du SERPROC n'est nécessaire pour ce passage.

Deux Model A 16 Kio distincts avec MOS 1.20, BASIC II et caractères SAA5050 externes ; aucun lecteur disque, réseau, gestionnaire direct ni sortie Windows. La première carte saisit `10 PRINT"TAPE ROUNDTRIP PASS"`, `20 PRINT 12+34`, puis `SAVE"RML"`, transport REC activé et réponse Return à l'invite MOS. Le signal obtenu (530 315 échantillons mono à 48 kHz) est transmis en mémoire comme seul contenu tape à la seconde carte. Celle-ci exécute `CHAIN"RML"` et produit effectivement le texte attendu et 46. Le programme n'est ni injecté ni recopié dans la RAM du récepteur.

Cette preuve sollicite la sérialisation MC6850, la modulation SERPROC, le transport enregistré puis sa lecture/démodulation sous contrôle du MOS. Elle complète les 193 tests de fronts/états existants ; elle ne les remplace pas par un simple aller-retour dont les deux côtés pourraient partager une erreur. Aucune puce ni cadence modifiée dans ce passage.

| Passage Debug | SAVE ms/image | CHAIN ms/image | Cycles SAVE / CHAIN |
| --- | ---: | ---: | --- |
| Premier | 17,3291 | 18,6517 | 21 844 991 / 11 461 633 |
| Captures après stabilisation | 17,1037 | 18,2199 | 21 844 991 / 11 461 633 |

547 images SAVE et 287 images CHAIN dans chaque fenêtre mesurée, rendu et drainage audio inclus. La saisie et le début de la commande précèdent la fenêtre : il ne s'agit pas de durées totales de bande. CHAIN inclut aussi la recherche du verdict dans la mémoire télétexte après chaque image, ce qui est une surcharge de diagnostic ; aucun gain avant/après n'est revendiqué. Second passage : 58,47 / 54,89 FPS calculés libres, 2,335 / 2,192 MHz, 116,75 / 109,59 % de 2 MHz ; PAL matériel inchangé, environ 50,1 Hz.

Preuves inspectées : `20261107-054903-tape-roundtrip-save-stable.bmp` et `20261107-054909-tape-roundtrip-chain-stable.bmp`, sous `artifacts/TestsChips/BbcSerialUla/BBC Micro Model A/` et `artifacts/TestsMachines/Acorn/BBC Micro Model A/`. Les premières trames `054732` et `054738`, prises pendant la mise à jour entrelacée, restent conservées comme étapes intermédiaires. Six images hors mesure suffisent ici à stabiliser les captures finales ; aucun désentrelacement ni retouche de pixels n'a été effectué.

Pas de fichier média créé ni modifié, pas de configuration utilisateur écrite. Les supports et notifications sont détachés dans finally. Ce passage ne valide ni les conteneurs UEF/CSW/WAV, ni l'écoute des bruitages Windows, ni la vitesse des autres périphériques.

## Vérification Model A — 7 novembre 2026

Le groupe SERPROC/MC6850 passe **193 tests Debug en 3 s**, sans échec ni test ignoré. Les tests `ClockGroupingTests` comparent les fronts, niveaux, notifications et phase à l'avancement unitaire, y compris séparateur actif, silence, changements de registre et d'entrées, à 1 230 770, 2 000 000 et 3 000 001 Hz. Le MC6850 publie déjà TXD depuis ses changements et RTS/diviseur depuis les écritures de contrôle : l'ancien sondage généralisé des sorties n'est plus présent dans son émission.

Carte complète Model A, 16 Kio, MOS `os12.rom`, BASIC `basic2.rom`, caractères `saa5050`, aucun disque ni réseau, horloge 2 MHz. Exécution sans RML ni sortie audio Windows, drainage du PCM inclus, aucune option Release activée en Debug. Après 120 trames de chauffe et `PRINT "MODEL A PASS":PRINT 12+34`, 80 trames mesurées : **23,050 ms/image, 43,38 FPS calculés, 1,733 MHz, 86,6 %**, 3 194 879 cycles. Lecture matérielle de l'UEF Arcade Action v2.5 par `*TAPE` puis `*CAT` : **27,920 ms/image, 35,82 FPS calculés, 1,430 MHz, 71,5 %**, 3 194 882 cycles sur 80 trames. Ce sont des coûts de carte complète, pas du SERPROC isolé ; aucun gain avant/après n'est revendiqué pour ce passage.

Le catalogue affiche réellement `INDEX 00`. La pause maintient la position 80 470 échantillons pendant 20 trames. Les trames calculées, inspectées visuellement, sont conservées sous `artifacts/TestsChips/BbcSerialUla/BBC Micro Model A/20261107-005054-basic-carte-sans-interface.bmp` et `20261107-005057-catalogue-index-carte-sans-interface.bmp`, et également dans `artifacts/TestsMachines/Acorn/BBC Micro Model A/`. Il ne s'agit pas de captures de RML ; elles prouvent le catalogue, pas l'exécution du jeu ni toutes les fonctionnalités de la machine. Le média et les paramètres utilisateur ne sont pas modifiés ; entrées et tape sont détachées dans `finally`.

Le budget de vitesse réelle n'est pas atteint dans ce passage. Le surcoût sous lecture nécessite encore une attribution avant correction ; ne pas en déduire que l'ACIA ou le séparateur matériel peut être simplifié.

### Transport : autorisation conservée aux changements — 7 novembre 2026

Défaut logiciel identifié : `Tape.Tick` repassait par `TapeActive`, qui relisait moteur et sélection du SERPROC, deux fois par cycle. Ces entrées ne peuvent changer que lors des écritures du registre ULA déjà acheminées par `Tape.WriteUla`/`ReadUla`, des commandes du lecteur, du reset ou des changements de support. Toutes ces opérations appellent déjà `UpdateClockRequest`. Le transport réutilise désormais l'activité qu'il y conserve et publie ; il ne consulte plus ces sorties série dans son tick. L'écriture du registre résout le nouvel état avant le retrait éventuel du niveau d'entrée. Aucun nouveau cache d'état sauvegardé, thread ou ordonnanceur n'est ajouté. `RecordTick` réutilise également l'incrément d'échantillon recalculé lors des changements de fréquence de support ou de vitesse.

Après ce bloc : **53 tests Debug tape/série/audio réussis, 11 s**. Le scénario de transitions compare maintenant à chaque cycle l'activité conservée à la formule matérielle indépendante du moteur, de la sélection, du transport et de la recherche ; il couvre aussi l'éjection et la fin de bande. Les autres contrôles comprennent écriture puis relecture FSK, formats ACIA, latences et PCM. Ces résultats ne constituent pas une nouvelle écoute de la sortie Windows.

Comparaison complète sur le même Model A 16 Kio, mêmes ROM/UEF, 120 trames de chauffe puis 120 trames par scénario ; drainage audio inclus, calcul SHA-256 des séquences vidéo et PCM hors chronométrage. Assemblies antérieures conservées séparément, séquence de processus référence/candidat/référence/candidat, uniquement Debug sans changement d'optimisation de compilation.

| Scénario | Référence, ms/image | Candidat, ms/image | Moyenne avant → après |
| --- | --- | --- | --- |
| Repos | 16,3537 / 16,2199 | 16,0603 / 15,9710 | 16,2868 → 16,0157 |
| Lecture UEF | 19,1682 / 19,5619 | 17,4388 / 17,2451 | 19,3651 → 17,3420 |

Gain de temps de lecture : **10,45 %** dans ce scénario, environ **57,66 FPS calculés, 2,30 MHz et 115 %** de la vitesse réelle en exécution sans limite. Les quatre passages gardent exactement 4 792 317 cycles, PC FAD6 et la position 137 154 après la lecture. Empreinte de la séquence vidéo : `86E0510A8158A8669B27D10C3EF0A41C22639C9320EE2AD624A1565F80AB0123` ; PCM : `EAD7887929FACA48BF0D1F614870E41D3C3E706B4505B13FA91198B89A6A2478`. Au repos : 4 792 320 cycles, PC E57A, mêmes empreintes vidéo/PCM dans les quatre passages. Le changement est conservé pour son gain global et l'équivalence des sorties couvertes. Les mesures du premier diagnostic avec saisie PRINT ne sont pas utilisées comme référence avant/après de cette comparaison.

### Chargement du logiciel réel après modification

Même Model A 16 Kio, même UEF, `*TAPE` puis `CHAIN ""`, sans mode direct. Le programme INDEX est entièrement chargé et affiche le menu Arcade Action à la position 910 430. Le banc attend la question `Which program do you want ?` avant d'envoyer `1` et Entrée ; la tape reprend alors. Invaders atteint sa question `1 OR 2 PLAYERS ?`, avec scores, bases et crédits affichés, position de bande 4 802 830 stable. Les captures de progression sont conservées dans les deux dossiers de preuves. Points inspectés : `20261107-010959-index-result-carte-sans-interface.bmp`, `20261107-011037-invaders-loading-carte-sans-interface.bmp` (bloc `0E`), `20261107-011137-invaders-loading-carte-sans-interface.bmp` (programme chargé).

Limites du banc : le premier essai a cherché le titre avec une casse incorrecte ; le suivant l'a reconnu avant que le menu soit prêt à recevoir une touche. L'essai final charge bien le logiciel, mais sa condition automatique attend à tort la désélection télétexte. Invaders utilise encore ce mode à son écran de choix ; le banc termine donc avec code 1 malgré le chargement visible. Cette erreur de critère n'est pas attribuée à l'émulateur. Aucune partie active, aucun tir et aucune écoute audio Windows ne sont revendiqués par ce passage ; ils restent à valider lors des essais de machine. Les boucles physiques et le contenu du logiciel n'ont pas été modifiés pour accélérer le chargement.

## Accès internes et réutilisation — 5 novembre 2026

Les sélections moteur/RS423 sont décodées aux écritures du registre, et les accès internes aux sorties de la puce utilisent leurs champs, sans modifier l'interface publique ni les horloges. Le module conserve la référence du composant série et l'override de débit dont il possède les écritures ; il évite de redemander ces valeurs à chaque accès. Une tape absente est vérifiée avant de consulter son moteur, mais l'électronique série continue de recevoir tous ses cycles.

L'essai de calcul différé du synthétiseur a été retiré faute de gain isolé établi : sa phase et sa sortie sont toujours calculées à chaque Tick comme précédemment. Le groupe de puces SAA5050/VIDPROC/SERPROC compte **210 réussites en Debug et Release** après ces changements. Les essais supplémentaires temporaires liés au synthétiseur différé sont retirés avec cette tentative ; aucune optimisation propre au seul BBC n'est intégrée au modèle du SERPROC.

## Périmètre et constat initial

Circuit des BBC Micro A/B/B+, réutilisé dans le Master équipé du port série. L’ULA ne possède ni fichier tape, ni index de blocs, ni pilote série Windows. Elle fournit les horloges et le multiplexage TTL autour du MC6850, le séparateur tape, la détection de porteuse et le modulateur de sortie. Les buffers RS423, l’amplificateur d’entrée, le relais et les composants RC sont externes.

Avant reprise, `Tape.WriteUla` ignorait les bits 0–5 et l’ACIA utilisait une horloge fixe de 19 200 Hz. Le bit RS423 bloquait CTS sans entrée distante. La démodulation et la modulation étaient incorporées au transport des échantillons. Ce constat porte sur le raccordement, pas sur une absence de fonctions du MC6850 déjà vérifié.

## Sources confrontées

- [Acorn BBC Microcomputer Service Manual, section 3.5 et figure 9](https://acorn.huininga.nl/pub/unsorted/manuals/BBC%20Microcomputer%20Service%20Manual-HTML/BBCServiceManual.html) : deux générateurs, entrée 16/13 MHz, division 1024 du ton bas, multiplexeurs RXD/RXC/DCD/TXD/RTS/CTS ; synthèse activée par RTS ; réseau RC de porteuse externe et dépendant des révisions.
- [Advanced User Guide, sections 20.6 et 20.9, pages imprimées 388–394](https://stardot.org.uk/mirrors/www.bbcdocs.com/filebase/essentials/BBC%20Microcomputer%20Advanced%20User%20Guide.pdf) : table non linéaire des sélections, division ACIA 64 en RS423 ; sélection ULA 300 avec ACIA /16 ou /64 pour tape 1200/300.
- [b2 serproc](https://github.com/tom-seddon/b2/blob/master/src/beeb/src/serproc.cpp) et `serproc.inl` : table nominale non linéaire ; le générateur simplifié ne constitue pas une preuve du circuit et b2 n’émule pas la tape.
- [B-Em serial.c](https://github.com/stardot/b-em/blob/master/src/serial.c) : registre, sélection, lecture ayant l’effet d’une écriture FE ; ne pas reproduire la dépendance du relais à la présence d’un fichier.
- [JSBeeb serial.js](https://github.com/mattgodbolt/jsbeeb/blob/main/src/serial.js) et `acia.js` : sélection des entrées et lecture FE. La table linéaire de `serial.js` diffère du guide technique ; retenir la table documentée, pas une majorité d’émulateurs.

## Registre et horloges

Le décodage FE10–FE17 appartient à la carte. D0–D2 choisissent TXC, D3–D5 le générateur RXC RS423, D6 choisit RS423 (1) ou tape (0), D7 commande le moteur indépendamment du média. Une lecture est modélisée avec l’effet d’une écriture FE et une valeur retournée nulle, conformément aux deux implémentations tape consultées ; la valeur électrique du bus non conduit n’est pas garantie par la documentation.

| Sélection brute | Division de 16/13 MHz | Débit nominal avec ACIA /64 |
|---|---|---|
| 000 | 1 | 19200 |
| 001 | 16 | 1200 |
| 010 | 4 | 4800 |
| 011 | 128 | 150 |
| 100 | 2 | 9600 |
| 101 | 64 | 300 |
| 110 | 8 | 2400 |
| 111 | 256 | 75 |

Le débit physique de la première ligne est 16 000 000 / 13 / 64, soit environ 19 230,77 bauds. Ne pas arrondir l’oscillateur à 1,23 MHz. TXC reste programmable en tape ; RXC tape provient du séparateur de données et non du générateur RS423. Le MC6850 conserve la qualification de start, les bits, la parité, les arrêts, les erreurs et IRQ.

## Signaux et limites du modèle numérique

Les broches RS423 sont représentées après les buffers externes, en niveaux logiques TTL. En tape, CTS vers l’ACIA est bas, DCD vient du détecteur de ton haut, RXD/RXC du séparateur ; les sorties RS423 sont inactives. En RS423, DCD est bas, RXD/CTS viennent du périphérique externe ; TXD/RTS sortent de l’ACIA. Le moteur demeure commandé par D7 dans les deux modes.

Le schéma ne donne pas le réseau de portes interne du séparateur, la table des niveaux de synthèse ni les tolérances des différentes révisions. Une réalisation numérique devra donc expliciter sa qualification des demi-ondes et son délai RC nominal, conserver les fréquences et la modulation, et ne pas prétendre reproduire une amplitude analogique mesurée. Aucun relevé matériel n’est demandé à l’utilisateur pour ces informations non publiées.

## Vérifications à réaliser

Toutes les sélections RX/TX séparément et ensemble ; fréquence et phase sur des intervalles non entiers ; changement de registre sans remise arbitraire à zéro de l’oscillateur ; lecture avec effet FE ; moteur sans média ; isolation et polarités des deux chemins ; RTS, CTS, DCD ; tonalités, silence et signal invalide ; réception et émission à 300/1200 via un véritable MC6850 ; conservation des octets et erreurs ; reset de carte sans rembobinage. Confirmer par programmes invités BBC A/B/B+ et captures ; mesurer la boucle de carte sans modifier les options du compilateur.

## Réalisation

`IO/Acorn/SerialUla/BbcSerialUla` fournit l’interface de broches et les services du circuit. Les deux générateurs partagent un compteur libre alimenté par un accumulateur entier 16 000 000 / 13 ; les écritures changent les prises du diviseur sans remettre son compteur à zéro. RX tape conserve sa cadence de 19 230,77 fronts/s, indépendante de la sélection RX RS423. Les broches d’horloge appellent ClockReceive/ClockTransmit du MC6850 : aucune livraison directe d’octets ni réception imposée par l’hôte.

Le service `Serial` dans le module Acorn raccorde les broches ; `Tape` conserve les touches, positions, échantillons, index, sauvegardes et latences du magnétophone. L’ACIA de la carte utilise le mode horloge externe, sans générateur interne parallèle. Sa nouvelle propriété de lecture ClockDivider expose le diviseur déjà programmé, sans modifier ses séquences : elle permet de conserver l’option utilisateur de débit tape comme adaptation de carte, distincte du circuit physique nominal. Les 23 essais de transport existants ont été conservés et adaptés aux vraies sélections ULA.

La sortie tape utilise une sinusoïde numérique nominale à seize niveaux de phase, aux fréquences physiques 16 MHz / 13 / 1024 et /512 ; RTS et la sélection RS423 la coupent. Le séparateur qualifie les demi-ondes dans la fenêtre numérique documentée ici, avec seuil de 300 µs, retour mark après silence et impulsion de porteuse après 836 demi-ondes hautes (environ 174 ms). Le détail des portes internes, du DAC et du réseau RC de chaque révision n’étant pas publié dans les sources obtenues, ces paramètres sont un modèle nominal explicite, pas des mesures de toutes les révisions 2C199. L’amplitude analogique et les filtres externes ne sont pas présentés comme une simulation électrique.

## Résultats du 4 novembre 2026

### Vérifications autonomes

99 cas propres au SERPROC : 64 combinaisons RX/TX avec compte exact des fronts sur une durée fractionnaire ; 24 boucles sur un vrai MC6850 (huit sélections et divisions 1/16/64) ; fréquence des deux tons et coupure RTS ; sélection des broches et moteur sans média ; phase conservée et lecture FE ; horloge tape indépendante de RX RS423 ; porteuse et silence ; état initial et entrées externes conservées ; CTS bloque puis libère réellement un octet ACIA.

La suite complète Chips donne **2 463 réussites, aucun échec ni test ignoré en Debug et en Release**. La suite tape du module donne **23 réussites dans chaque configuration**, dont émission/relecture 300/1200, formats série et parité, cadences PCM 22 050/44 100/48 000, latences, protection, positions, enregistrement non destructif et chemin direct validé.

### Preuves exécutées par les BBC

Une ROM de banc synthétique exécute le 6502, configure CRTC/ULA vidéo et les registres FE08–FE10, attend la réception matérielle, compare chacun des 64 octets à sa table en ROM et écrit elle-même PASS ou FAIL dans la RAM vidéo. Le verdict ne vient pas du dessinateur PNG : celui-ci exporte seulement le BGRA produit par la chaîne vidéo native et le SAA5050. Le test ne remplace aucun firmware de la configuration utilisateur.

| Carte | Réception FSK 300 / 1200 | Enregistrement par le 6502 et relecture par un autre BBC du même modèle |
|---|---|---|
| Model A 16 Kio | 64/64 et PASS aux deux débits | 1200 bauds, 48 473 échantillons PCM à 48 kHz, 64/64 et PASS |
| Model B 32 Kio | 64/64 et PASS aux deux débits | 300 bauds, 122 754 échantillons PCM à 48 kHz, 64/64 et PASS |
| B+ 64 Kio | 64/64 et PASS aux deux débits | 1200 bauds, 48 472 échantillons PCM à 48 kHz, 64/64 et PASS |
| B+ 128 Kio | 64/64 et PASS aux deux débits | 300 bauds, 122 753 échantillons PCM à 48 kHz, 64/64 et PASS |

Captures natives 640 × 256 conservées hors Git sous `artifacts/TestsChips/BbcSerialUla/BbcMicroModelA/`, `BbcMicroModelB/`, `BbcMicroModelBPlus64/`, `BbcMicroModelBPlus128/` : `receive-300.png`, `receive-1200.png`, et, pour le débit d’enregistrement du tableau, `transmit-<débit>.png` et `record-replay-<débit>.png`. Les essais RX/TX RS423 aux 24 combinaisons de sélection/division sont prouvés par le banc autonome de broches, pas attribués à ces seules captures tape.

Les 12 scénarios invités, les 23 contrôles tape et le relevé de performances donnent **36 réussites en Debug et 36 en Release**. Les huit empreintes vidéo de réception sont identiques entre les deux configurations ; les quatre relectures d’enregistrements retrouvent la même image PASS que la réception synthétique du modèle et débit correspondants. Ce résultat ne déclare pas les machines BBC complètes : Winchester et la validation finale des équipements restent dans leur bloc propre.

### Optimisation mesurée

Banc temporaire en processus de test, 100 000 cycles d’échauffement puis trois passages de 4 000 000 Tick par contrôle 00/85/40/FF, changement périodique de TXD, aucune sortie audio Windows. Compilation Debug normale : médiane initiale 140,03 ns/Tick pour 00, 137,10 pour 85, 51,33 pour 40 et 52,01 pour FF. Après table de synthèse stockée une seule fois et seuils temporels précalculés, dernière passe : **58,71 / 52,76 / 53,37 / 51,36 ns/Tick** respectivement. Pas d’allocation par Tick ; aucun cycle de carte, signal ni filtre supprimé. Les différences inférieures à quelques ns entre passages ne sont pas interprétées comme des gains certains.

Ces coûts sont ceux de l’ULA isolée, pas les FPS de toute la machine. Les captures synthétiques ne constituent pas un test de tous les jeux ou médias BBC ; ceux-ci relèvent de la validation de machine demandée ensuite.

Release normal, même banc final : médianes **4,96 / 4,86 / 5,39 / 6,55 ns/Tick** pour 00/85/40/FF. Les boucles de réception invitées de 64 octets prennent environ 189–200 ms à 1200 bauds et 552–581 ms à 300 bauds en exécution non bridée ; ce coût inclut le CPU, toute la carte, le rendu et trois champs supplémentaires après verdict. Ce n’est pas une mesure de latence du magnétophone réel ni un résultat de l’application avec affichage Windows.

## Livraison et clôture

### Réouverture de l’intégration tape, 5 novembre 2026

Le scénario MOS réel `*SAVE RML 2000 2200`, puis relecture WAV physique, reproduit `Data?` sur les deux blocs. Les cinq régressions autonomes couvrant les 256 octets, en rafale et avec pauses jusqu’à 50 003 cycles, passent : elles ne suffisent donc pas à prouver le protocole MOS complet. La trace du bus montre les données source intactes et deux octets de synchronisation successifs avant chaque en-tête.

Un défaut est établi dans le détecteur : après 836 demi-ondes hautes, `_carrierPulse` était armé une seule fois, puis expirait même si le ton haut continuait. La section 3.5 du [manuel de maintenance Acorn](https://acorn.huininga.nl/pub/unsorted/manuals/BBC%20Microcomputer%20Service%20Manual-HTML/BBCServiceManual.html) décrit la détection du ton entrant et sa temporisation RC ; les [routines MOS de lecture](https://tobylobster.github.io/mos/mos/S-s19.html) attendent effectivement la disparition de DCD après l’amorce avant de chercher la synchronisation. Une impulsion unique au début du ton ne respecte pas ce contrat.

La correction réalisée réarme le maintien nominal à chaque demi-onde haute valide après qualification initiale, avec arrondi du délai au cycle supérieur. Elle conserve les seuils et la retombée sur ton bas/silence. Les **100 tests ULA Release passent**, dont la nouvelle vérification continue après amorçage puis retombée sur ton bas ; les **cinq régressions d’émission/relecture des 256 octets passent** avec pauses de 0, 37, 1 021, 20 003 et 50 003 cycles.

Le banc MOS B réel reproduit toutefois encore `Data?` après cette correction, avec la DLL Chips effectivement recopiée et son empreinte vérifiée. Le maintien DCD est un défaut corrigé, mais n’est pas établi comme la cause suffisante de l’échec SAVE/LOAD. La trace des écritures CPU vers FE09 contient deux synchronisations 2A avant le nom, les données source intactes et des CRC d’en-tête cohérents lorsqu’on exclut le 2A supplémentaire ; le rôle des horloges/interruptions dans cette séquence reste à isoler. Aucune correction du MC6850 ou du CPU n’est déduite de cette observation seule.

La clôture précédente est historique : l’intégration d’enregistrement MOS a été réouverte après le résultat `Data?`. La capture `record-replay-wav.png` du Model B est renouvelée par chaque passage ; elle ne constitue donc pas une archive de cet échec intermédiaire.

Le passage final du 5 novembre réussit les 16 combinaisons A/B/B+ 64/B+ 128 × WAV/UEF/CSW/RAW : les 512 octets enregistrés par SAVE sont restitués par LOAD en lecture physique, et les blocs indexés ont leur intégrité valide et leurs données exactes. Outre le maintien de porteuse ULA, ce résultat nécessite la correction distincte de phase de transmission MC6850 (`6b9ee07bb`) et le recalage UART de l’indexeur MediaEngine. Les preuves par machine sont décrites dans [AcornBBCMicro.md](../../machines/AcornBBCMicro.md). Cette réussite ne doit pas être attribuée à la seule ULA.

Le passage Debug des **100 tests ULA réussit également (658 ms)**. La correction autonome du maintien de porteuse est validée dans les deux configurations ; l’écart MOS restant est traité séparément, sans déclarer que cette correction suffit au chargement d’un enregistrement complet.

Le diagnostic temporaire `AcornEmuBbcSerialDiagnostic.cs` a été supprimé après les mesures et captures. Les assemblies de tests ont été reconstruites en Debug et Release ; elles ne contiennent plus cette classe. Les 99 tests autonomes de puce et les 23 tests tape utiles sont conservés, sans dépendance aux ROM ou fichiers de diagnostic externes.

`scripts/local-building.cmd --building=debug --modules=A` est terminé avec code de sortie zéro : exécutable `build/Debug/Retro Media Lab/retromedialab.exe`, DLL Chips dans `lib`, 24 manifestes de modules avec chacune leur DLL d’entrée présente. L’empreinte de la DLL Chips livrée correspond à celle du build Debug testé. Les références et journaux temporaires sont supprimés ; les seize captures restent hors Git.

Les fonctions numériques décrites sont réalisées et validées ; les seules limites retenues sont celles du détail analogique/interne non publié, explicitement décrites plus haut. La clôture de ce circuit ne ferme pas les points machine encore ouverts.

### Essai des invariants de calcul, 5 novembre 2026

Essai groupé : périodes des deux tons et masques de diviseur préparés hors Tick, masque de phase à seize pas, sortie anticipée du séparateur déconnecté après silence établi. Les cinq nouveaux cas vérifient la phase exacte de synthèse à 1 230 769, 2 000 000, 2 000 003 et 4 000 000 ticks/s, en changeant TXD, RTS et les 256 contrôles, ainsi que la reprise du séparateur après une longue déconnexion. Les 105 contrôles série passent en Debug avec l'essai ; les cinq contrôles de raccordement passent également.

Le banc Debug de carte donne 31,0414 ms/tranche, 32,21 FPS, 1,29 MHz ; tape/série isolée 3,1710 ms pour 39 936 cycles, contre 3,1501–3,3050 ms dans les passages précédents. Aucun gain global n'est établi. L'essai de production a donc été retiré, sans réintroduire d'ancien code antérieur aux corrections de porteuse et de câblage. Les cinq tests autonomes supplémentaires restent utiles pour les phases fractionnaires et la reprise d'entrée. Les 105 cas passent sur le code restauré en Debug (926 ms) et Release (441 ms) ; les cinq raccordements passent dans chaque configuration. Les journaux temporaires du passage sont supprimés après consignation. L'échec à améliorer les performances ne constitue pas une correction du déficit de vitesse de la machine.

## Essai retiré : instantané des sorties SERPROC — 5 novembre 2026

Le regroupement de TXCLK/RXCLK/CTS/DCD dans une structure retournée après le Tick existant a été comparé au module Debug livré, dans deux contextes collectables utilisant exactement la même assembly Chips. Sept passages alternés de 200 000 cycles après 20 000 cycles de chauffe, compilation tiercée désactivée uniquement pour ce processus de mesure ; aucune option du produit modifiée. Entrée tape déconnectée, quatre commandes et deux débits forcés. Les deux contextes ont été déchargés en finally et leur disparition vérifiée après collecte.

| Contrôle | Débit forcé | Avant ns/cycle | Essai ns/cycle |
|---|---:|---:|---:|
| 00 | 0 | 93,394 | 109,814 |
| 00 | 1200 | 72,129 | 84,379 |
| 85 | 0 | 91,204 | 105,700 |
| 85 | 1200 | 86,585 | 86,519 |
| 40 | 0 | 93,112 | 89,775 |
| 40 | 1200 | 69,477 | 81,389 |
| FF | 0 | 61,346 | 72,060 |
| FF | 1200 | 65,273 | 79,611 |

109 contrôles autonomes réussis, dont quatre comparaisons cycle par cycle à 1 230 769 / 2 000 000 / 2 000 003 / 4 000 000 Hz : toutes commandes, signaux RS423, entrée tape nullable, synthèse et remise sous tension. Les cinq contrôles du raccordement ACIA et la comparaison appariée passent également. Le regroupement est néanmoins plus lent dans six charges sur huit : API, structure, utilisation et quatre tests spécifiques retirés intégralement. Le banc temporaire et la copie Release ont été supprimés ; mesure Release annulée après ce rejet. Les tests préexistants et les optimisations antérieures restent conservés. Aucun nouveau résultat de cadence native ne découle de cet essai.

## Essai retiré : intervalles stables et raccordement BBC — 5 novembre 2026

**État actuel : essai retiré du circuit, du contrat et du module. Les descriptions et mesures ci-dessous sont historiques. Aucun cycle série n’est différé dans la version restaurée.**

`Advance(cycles, TXD, RTS, out TXC, out RXC)` avance les accumulateurs de source, diviseurs, séparateur et synthèse avec les mêmes états finaux que les appels unitaires. Le premier cycle conserve la reconnaissance d'une éventuelle transition tape. La suite compte les périodes et les échéances sans répéter chaque appel. Les nombres de fronts transmis sont complets ; les propriétés de fronts décrivent le dernier cycle. L'appelant reste responsable de découper avant une modification d'entrée ou une échéance qu'un autre composant doit observer.

La limite basse de fréquence acceptée reste strictement équivalente : le modèle unitaire ne produit qu'un front source par cycle, même lorsque son petit reliquat de phase s'accumule. L'avancement groupé conserve cette borne. Les tests comparent tous les champs internes et les nombres de fronts aux fréquences 1 230 769, 2 000 000 et 3 000 000 Hz, tous les contrôles, les changements de niveaux, TXD/RTS, la qualification porteuse, son impulsion et son expiration. Un intervalle de 5 400 000 cycles vérifie le reliquat à la fréquence minimale.

Le module BBC regroupe au plus 64 cycles seulement lorsque RS423 est sélectionné, RXD haut, TX/RX au repos et les broches modem stables. Les accès ACIA/SERPROC et l'observation du composant synchronisent d'abord les cycles écoulés. Tout état actif et toute entrée tape restent sur le chemin unitaire. Aucun circuit ne connaît une autre puce : c'est le câblage du module qui applique ces conditions. 182 contrôles MC6850/SERPROC et six raccordements BBC réussissent dans chaque configuration Debug/Release.

Après retrait : groupes série/vidéo sélectionnés 271 cas réussis par configuration, raccordements BBC 158 cas réussis par configuration. Les horloges de chaque cycle sont de nouveau traitées immédiatement. Les anciennes captures du regroupement ne décrivent plus le chemin actuellement conservé.

## Raccordement série par notifications — modification du 6 novembre 2026

Les phases TxC/RxC restent dans le MC6850 ; phase source, diviseur et synthèse restent dans SERPROC. Le MC6850 publie les changements TXD/RTS et de diviseur après ses mutations existantes. SERPROC retient ces entrées, publie les changements de ses sorties modem et signale les fronts série produits par sa propre horloge. Le module Serial raccorde ces sorties aux entrées de l'autre circuit ; il ne consulte plus leurs propriétés à chaque tick pour découvrir un changement.

Les connexions sont établies à la construction du câblage, avant toute horloge, et les niveaux initiaux sont présentés une fois. L'option de débit tape reste gérée par son accumulateur existant ; son pas est mis à jour sur notification du diviseur.

Aucun test, build ou résultat de performance nouveau n'est revendiqué pendant cette correction. La validation comparée des séquences, fronts et sorties reste à effectuer à l'étape finale autorisée.

## Propagation des entrées RS423 et séparation des notifications — 6 novembre 2026

Le manuel Acorn, section 3.5 et figure 9, décrit le routage des données et signaux de contrôle entre ACIA, tape et RS423, distinct des générateurs d'horloge. Dans le modèle numérique, les propriétés RXD/CTS sont déjà des niveaux multiplexés sans registre d'échantillonnage intermédiaire ; attendre un Tick uniquement pour notifier le récepteur ajoutait un retard absent de ces expressions.

Les setters Rs423ReceiveData et Rs423ClearToSendHigh publient donc maintenant le changement de niveau immédiatement lorsque RS423 est sélectionné. En tape, ils conservent leurs niveaux sans notifier une sortie masquée. Une écriture du registre de sélection présente immédiatement les nouvelles entrées au récepteur. Cela ne crée aucun front RxC : le MC6850 conserve l'échantillonnage de RXD par son propre chemin de réception.

La publication des niveaux est séparée de SerialClocksRaised. Modifier une entrée ou la sélection ne peut donc pas rejouer un front encore présent dans les indicateurs du dernier Tick. Le séparateur tape publie après une mutation possible de donnée ou de porteuse, sans inspection globale des quatre sorties à chaque Tick. L'expiration du silence agit au franchissement du seuil, sans réappliquer indéfiniment le même état de silence. Le raccordement Serial présente les niveaux initiaux lors de sa construction et de PowerOn.

OutputNotificationTests ajoute les contrôles des entrées immédiates, de leur masquage, de la phase inchangée et de l'absence de front rejoué. Ils sont écrits mais non exécutés pendant ce bloc de correction ; aucun nouveau résultat de performances, de régression ou de build n'est annoncé.

## Validation groupée des raccordements — 6 novembre 2026

Les cas de ce composant et les autres cas du bloc horloges/signaux réussissent dans le passage Chips Debug complet. Le passage ciblé des sept composants en Release donne 723 réussites, zéro échec ni ignoré, 710 ms. Les contrôles d'intégration sélectionnés BBC Model B, tape/série, 8271 et WD1770 donnent 166 réussites en Release, 10 s. En Debug, les 160 cas extérieurs au câblage série ont réussi au premier passage ; les six cas de câblage série ont réussi après adaptation de leur relais et de leurs assertions de câblage, 848 ms. Ces comptes sont ceux des groupes, pas un nombre de fonctions propres à cette puce.

Le passage Chips Debug global reste non vert : deux contrôles des contrats publics, notamment sur des classes ACB-4000 déjà présentes et non modifiées, ainsi qu'un contrôle d'allocation SCSI échoué une fois puis réussi isolément. Les exigences correspondantes ne sont pas supprimées. Le détail est consigné dans docs/tasks/chips-validation.md. Ces résultats ne remplacent ni la mesure du BBC complet ni les captures de validation des machines ; aucun nouveau framerate n'est revendiqué.

## Séparateur tape inactif et niveaux retenus — 6 novembre 2026

Le séparateur conserve un état d'activité interne. Il traite encore chaque cycle tant que la mesure d'une demi-onde, le silence ou la porteuse peut évoluer. Après silence établi, compteur saturé et porteuse expirée, aucun champ ne peut changer avec une entrée stable : ce seul traitement cesse. SetTapeInput le réactive sur un changement réel null/bas/haut. Le traitement de cette entrée garde le front existant, sans cycle ajouté. PowerOn rétablit sa période initiale.

Les générateurs libres de source, diviseurs TxC/RxC et synthèse continuent tous leurs cycles. Le module présente seulement les changements de niveau et la déconnexion, séparément de Serial.Tick(). L'ancienne méthode Tick(bool?) reste disponible pour les tests. Pause, moteur arrêté, fin de bande et reset déconnectent l'entrée comme précédemment ; les échantillons nuls gardent le dernier niveau.

Validation groupée : 182 cas SERPROC/MC6850 réussis en Debug (environ 1 s) et Release (653 ms) ; 34 intégrations tape/série BBC réussies en Debug (34 s) et Release (5 s). Les nouveaux cas observent horloges et synthèse pendant le silence stable null/bas/haut, puis réveil, qualification de porteuse et disparition au silence. Les contrôles préexistants de fréquences fractionnaires, taux série, enregistrement et chargement restent exécutés. Aucun gain global n'est revendiqué avant la comparaison du BBC complet.

Comparaison du BBC complet au repos, avant/après ces seuls changements, trois processus alternés par variante : médiane Debug 23,2334 → 22,0819 ms/image (−4,96 %), Release 3,9828 → 3,7215 ms/image (−6,56 %). Cycles, PC et empreintes vidéo/audio identiques dans les douze passages. Conditions et valeurs individuelles dans docs/chips/CPU/Mos6502.md. Aucun gain de chargement tape ou de fenêtre native n'est extrapolé à partir de ce scénario au repos. Le déficit global Debug demeure ouvert.

## Échéances locales des générateurs libres — 6 novembre 2026

Le chemin Tick() possède un compte à rebours et le nombre de périodes d'entrée conservées. Il ne consulte aucun autre circuit et n'utilise aucun ordonnanceur de machine. Tant que le séparateur est actif, ou qu'un diviseur série unitaire rend les fronts denses, il garde le traitement unitaire. Après silence établi, il peut conserver les seules périodes ne produisant ni front TxC/RxC ni nouveau pas du synthétiseur.

Pour un résidu source p, un dénominateur D et k fronts source avant la prochaine sortie du diviseur, la distance est ceil((kD − p) / MasterClock). Le synthétiseur fournit sa propre distance ceil((période − résidu) / incrément). Le minimum fixe l'échéance : seules les périodes STRICTEMENT antérieures sont regroupées. Le front à l'échéance utilise ClockInputs, le même traitement que Tick(bool,bool), avec le même ordre séparateur, synthèse et notification des horloges. Aucun front utile n'est supprimé.

Avant une mutation TXD/RTS, d'entrée tape ou de registre, FlushPendingClock actualise les diviseurs libres et les deux résidus jusqu'au dernier front déjà reçu, puis invalide l'échéance. Le nouveau niveau est traité au front suivant existant, sans retard ajouté. La propriété SourceClockPhase projette également le résidu sur les périodes conservées : une lecture entre événements ne retourne pas une phase ancienne. PowerOn annule les périodes conservées avant de réinitialiser les circuits. Les entrées RXD/CTS restent multiplexées et notifiées immédiatement comme auparavant ; elles ne créent aucun RxC.

ClockGroupingTests compare les traces à chaque période avec le chemin unitaire, pour 1 230 770, 2 000 000 et 3 000 001 périodes d'entrée par seconde, toutes les combinaisons des huit diviseurs Tx/Rx et du multiplexeur. TXD/RTS, RXD/CTS, entrée tape, silence et PowerOn varient au cours des séquences. Les indicateurs de front, notifications, phase source, données et contrôle RS423, RXD/CTS/DCD, moteur, registre et PCM sont comparés. Les 184 cas SERPROC/MC6850 passent en Debug (3 s) et Release (2 s). Les 34 intégrations tape/série BBC passent en Debug (30 s) et Release (5 s).

Ces contrôles démontrent l'équivalence des séquences couvertes ; la conservation du bloc comme optimisation dépend encore de la comparaison du BBC complet. Ils ne remplacent pas les preuves d'émulation des autres puces ou machines.

Décision après comparaison du BBC complet : bloc conservé. Médiane Debug 21,3913 → 20,6511 ms/image (−3,46 %), Release 3,6480 → 3,4744 ms/image (−4,76 %), mêmes cycles et empreintes dans les douze passages. Les valeurs et conditions sont dans docs/chips/CPU/Mos6502.md. Ces résultats restent ceux du banc complet sans affichage Windows.

## Preuve de lecture tape avec les ROM du BBC — 6 novembre 2026

Média lu seulement : `F:\Retro\ef_tosec_backup_2024\Acorn BBC - Applications - [UEF] (TOSEC-v2013-10-16)\Beebcalc Spreadsheet (demo) (1983)(Gemini)\Beebcalc Spreadsheet (demo) (1983)(Gemini).uef`. SHA-256 avant et après : `AC02E21AB188068F86B88B821F44C316FE7711D10FE0275A1BEE0B95C8E22835` ; 9 609 600 échantillons préparés à 48 kHz par EmulationSdk.MediaProcessing. Aucune écriture de média ni de configuration.

Le banc possède une carte BBC Model B créée par ModelCatalog, avec une copie en mémoire des options et les ROM MOS 1.20, BASIC II, DFS et NFS de la configuration. Il exécute normalement le CPU et tous les circuits de la carte. *TAPE puis *CAT sont saisis par la matrice clavier ; la tape est explicitement en mode RealTime, sans injection de données en RAM ni chargement direct.

Le MOS affiche `BCALC 01`. La RAM écran contient ce nom et ce numéro de bloc, sans message `Data?` au point de contrôle. Le support a atteint l'échantillon 197 656 lors de la détection ; après trois trames permettant de terminer les deux champs vidéo, il est à 200 532. La pause conserve exactement cette position pendant 100 trames et désactive TapeActive. La reprise atteint 219 701 après 20 trames, avec TapeActive vrai. Les échantillons audio de la machine sont drainés, sans sortie audio Windows.

Captures inspectées, hors Git :

- `artifacts/TestsChips/BbcSerialUla/BbcMicroModelB/tape-catalogue.png` : trame 640 × 512 réellement calculée par le BBC, catalogue matériel en cours sur BCALC, après stabilisation des deux champs.
- `artifacts/TestsChips/BbcSerialUla/BbcMicroModelB/tape-pause.png` : écran calculé après les 100 trames de pause ; la stabilité de la position et la reprise sont démontrées par les assertions du banc, pas par l'image seule.

Ces images sont des sorties du cœur, pas des captures de la fenêtre Windows. Elles prouvent ce parcours de lecture, pas le chargement complet ni l'exécution de Beebcalc, ni toutes les fonctions du SERPROC. Les autres fonctions reposent sur les contrôles détaillés précédents. Pour cette UEF, le SDK retourne le signal mais pas de blocs décodés dans Blocks ; le mode direct et l'indexation ne sont donc pas validés par ce passage.

Le banc termine avec le code 0. La carte ne possède aucun thread ni fenêtre ; elle est détachée de la tape et des entrées dans finally. Aucun processus d'émulation ou ressource graphique Windows n'est lancé pour ce contrôle.

Livraison : scripts/local-building.cmd --building=debug --modules=A termine avec le code 0. Exécutable build/Debug/Retro Media Lab/retromedialab.exe, manifestes et DLL des 24 modules présents. DLL Chips source/livrée identiques, SHA-256 B30B88C613990BE4D39ABE0BF14FFF4D54599F7CD4B8DEFDE4E0686BE1FAFC93. Cette compilation ne constitue pas une mesure de cadence native.

Observation native du build livré : BBC Model B atteint le prompt ; la capture native-clock-signals.png est dans artifacts/TestsChips/BbcIoLogic/BbcMicroModelB/. Une boîte de dialogue dotnet préexistait au lancement et masque une partie centrale de la capture ; elle ne fait pas partie du rendu BBC et n'a pas été fermée. Les compteurs restent visibles. Les captures tape précédentes proviennent du cœur et ne contiennent pas cette fenêtre. RML a été fermé après observation, disparition de la fenêtre et du processus vérifiée.

## Essai de synthèse coupée sans échéance intermédiaire retiré — 6 novembre 2026

Le candidat ignore les échéances internes de synthèse lorsque RTS ou le multiplexeur RS423 coupe la sortie tape. Le résidu et le pas du synthétiseur sont résolus par quotient/reste dans FlushPendingClock avant toute mutation. La sortie active continue à produire chaque pas normalement ; les fronts TxC/RxC restent inchangés. Trois cas supplémentaires comparent de longues périodes coupées puis la reprise avec un chemin scalaire, aux fréquences 1 230 770, 2 000 000 et 3 000 001 Hz, avec diviseurs 256, changements de TXD/RTS/multiplexeur et PowerOn.

Les 187 contrôles SERPROC/MC6850 et les 34 intégrations tape/série BBC passent dans chaque configuration. Le banc complet reprend les mêmes scénarios, options copiées en mémoire, 80 trames de chauffe puis 200 trames mesurées, trois processus par variante/configuration dans l’ordre référence/candidat/candidat/référence/référence/candidat. Les DLL de référence sont explicitement recopiées après construction du banc ; les options JIT Debug non optimisé/Release sont vérifiées sur Chips et Acorn chargés. Aucun média ni configuration utilisateur modifié. RunFrame et drainage/contrôle PCM sont chronométrés ; le hachage de la séquence vidéo reste hors chronométrage.

| Configuration / scénario | Référence (trois passages ms/image) | Candidat | Médiane avant → essai |
|---|---|---|---|
| Debug/idle | 19.761339 / 20.140427 / 20.332299 | 19.264575 / 20.048009 / 19.967438 | 20.140427 → 19.967438 |
| Debug/sound | 19.884954 / 20.747482 / 20.130310 | 19.865686 / 20.332566 / 19.894571 | 20.130310 → 19.894571 |
| Debug/drawing | 22.213650 / 22.648886 / 22.911724 | 22.896501 / 22.460143 / 22.322632 | 22.648886 → 22.460143 |
| Release/idle | 3.344857 / 3.281834 / 3.321680 | 3.335907 / 3.381275 / 3.403491 | 3.321680 → 3.381275 |
| Release/sound | 3.207461 / 3.186094 / 3.197693 | 3.365053 / 3.210910 / 3.344363 | 3.197693 → 3.344363 |
| Release/drawing | 3.482076 / 3.274377 / 3.233122 | 3.302045 / 3.323609 / 3.245077 | 3.274377 → 3.302045 |

Dans les 36 passages, les cycles, PC, hauteurs, échantillons non nuls et SHA-256 de la séquence vidéo, du PCM et de la dernière image restent identiques par scénario. Prompt : 7 987 198 / E464 ; SOUND : 7 987 201 / E539 et 176 118 échantillons non nuls ; redessin : 7 987 203 / CCBC. Les empreintes correspondent aux séries précédentes.

**Décision : retrait.** Les faibles gains Debug ne démontrent pas un gain global ; les médianes Release prompt et SOUND reculent. Le changement de Clock.cs et ses trois cas spécifiques sont retirés, leurs sources antérieures restaurées. Les projets concernés sont reconstruits pour ne pas conserver de DLL du candidat. Le banc et ses fichiers temporaires sont supprimés après consignation. Il ne s’agit pas d’une livraison ni d’une validation des 19,97 ms sur tous les scénarios ; aucun nouveau screenshot fonctionnel n’est revendiqué pour un essai retiré.

## Avancement absolu et raccordement par échéances : essai retiré — 6 novembre 2026

Essai : le SERPROC conserve InputClockCount, reçoit AdvanceTo et expose sa prochaine échéance. La carte fournit son temps à Serial.TickAt, synchronise avant les accès MPU/changements d’entrée et ne sollicite le circuit qu’à la date reçue. Aucun thread, calendrier générique, changement des fréquences ou dépendance à Emulation dans la puce n’est ajouté. Les API Tick restent couvertes par comparaison au chemin unitaire. Le reset numérique réinitialise les diviseurs mais pas la référence temporelle monotone.

Les notifications de réarmement sont réservées aux mutations externes ; après un avancement explicitement demandé, le propriétaire récupère l’échéance suivante. Un test vérifie qu’une écriture au même instant qu’un retour TXD réarme bien le circuit : filtrer sur une ancienne date notifiée aurait perdu ce réarmement. Les 189 contrôles SERPROC/MC6850 et les 38 scénarios tape/série BBC passent en Debug et Release, y compris les débits forcés 300/1200, accès MPU, reset, niveaux et dates des fronts.

### Comptage réel du BBC complet

Diagnostic distinct du chronométrage, avec compteurs temporaires retirés dans finally puis reconstruction : 200 images par scénario, après le même échauffement et les mêmes commandes. Les durées de ce passage instrumenté ne sont pas utilisées comme performance. Le SERPROC est programmé à 100 décimal (0x64) : RS423 sélectionné, diviseurs Tx et Rx égaux à 2, RTS haut, séparateur inactif.

| Scénario | Cycles carte | AdvanceTo | ClockInputs / Tick internes | Séparateur |
|---|---:|---:|---:|---:|
| idle | 7987198 | 2458990 | 2457599 | 0 |
| sound | 7987201 | 2458990 | 2457600 | 0 |
| drawing | 7987203 | 2458992 | 2457601 | 0 |

Au prompt, cela représente 12 294,95 appels AdvanceTo et 12 287,995 traitements ClockInputs par image. Le nombre de consultations SERPROC passe d’un Tick par cycle carte (7 987 198 sur ces 200 images dans le code de référence) à des intervalles moyens d’environ 3,25 cycles. Ces intervalles restent courts : le relais Synchronize, la boucle AdvanceTo et les consultations de date ajoutées doivent être moins coûteux que les opérations remplacées. Le séparateur ne consomme déjà aucun traitement à ce point ; lui attribuer tout le coût série aurait été incorrect. Les fréquences des horloges série ne doivent pas être supprimées pour gagner du temps.

### Comparaisons complètes

Deux séries indépendantes. Même configuration copiée en mémoire, 80 trames de chauffe, saisie clavier normale de SOUND ou du programme MODE 1 puis 200 trames mesurées ; trois processus par variante dans l’ordre référence/candidat/candidat/référence/référence/candidat. DLL recopiées explicitement après construction du banc et modes JIT vérifiés sur les assemblies chargées. RunFrame et drainage/contrôle PCM chronométrés ; empreinte de la séquence vidéo hors chronométrage. Aucun média ni configuration utilisateur modifié.

#### V1 : avancement avec relais

| Compilation / scénario | Référence (trois passages ms/image) | Candidat | Médiane avant → essai |
|---|---|---|---|
| Debug/idle | 19.802532 / 21.604745 / 19.986037 | 20.507222 / 20.416521 / 20.066122 | 19.986037 → 20.416521 |
| Debug/sound | 20.194966 / 19.950057 / 19.747715 | 20.473009 / 20.051082 / 20.230622 | 19.950057 → 20.230622 |
| Debug/drawing | 22.316704 / 21.993560 / 22.128679 | 25.146660 / 22.559316 / 22.539527 | 22.128679 → 22.559316 |
| Release/idle | 3.925976 / 3.906280 / 3.637904 | 3.632235 / 3.776705 / 3.739492 | 3.906280 → 3.739492 |
| Release/sound | 3.334713 / 3.422638 / 3.288179 | 3.407337 / 3.325525 / 3.302649 | 3.334713 → 3.325525 |
| Release/drawing | 3.262767 / 3.335325 / 3.443174 | 3.234201 / 3.220155 / 3.250970 | 3.335325 → 3.234201 |

#### V2 : avancement sans Tick imbriqué / Math.Min / relais à l’échéance

| Compilation / scénario | Référence (trois passages ms/image) | Candidat | Médiane avant → essai |
|---|---|---|---|
| Debug/idle | 19.388455 / 19.445155 / 19.524392 | 19.614073 / 19.975826 / 22.789797 | 19.445155 → 19.975826 |
| Debug/sound | 19.484925 / 19.686373 / 19.556142 | 19.653155 / 20.032409 / 19.609126 | 19.556142 → 19.653155 |
| Debug/drawing | 21.802094 / 21.998515 / 21.726467 | 22.053108 / 22.461504 / 22.250576 | 21.802094 → 22.250576 |
| Release/idle | 3.794812 / 3.645655 / 3.678164 | 3.605072 / 3.572128 / 3.775584 | 3.678164 → 3.605072 |
| Release/sound | 3.277774 / 3.201680 / 3.288742 | 3.168531 / 3.236518 / 3.321821 | 3.277774 → 3.236518 |
| Release/drawing | 3.276119 / 3.281244 / 3.316971 | 3.219499 / 3.251807 / 3.458856 | 3.281244 → 3.251807 |

Dans les 72 passages, les cycles, PC, hauteurs, nombres d’échantillons non nuls et empreintes vidéo complète/PCM/image finale restent identiques par scénario et correspondent aux séries antérieures : prompt 7 987 198 cycles / E464 ; SOUND 7 987 201 / E539, 176 118 échantillons non nuls ; redessin 7 987 203 / CCBC.

**Décision : retrait du bloc.** La seconde version réduit le surcoût de la première mais les trois médianes Debug restent supérieures à leurs références. Le gain Release ne suffit pas à valider ce changement comme optimisation du BBC Debug. Les nouvelles API de temps/échéance, les raccordements, les cinq cas spécifiques de puce et les quatre cas supplémentaires d’intégration sont retirés ; toutes les sources préexistantes sont restaurées depuis leurs copies antérieures et les assemblies reconstruites. Les 184 contrôles de puce et les 34 intégrations d’origine ne sont pas supprimés. Aucune nouvelle livraison ou preuve native n’est revendiquée ; le build natif précédemment livré reste inchangé. Le banc, ses compteurs et toutes ses sorties temporaires sont supprimés après consignation.

### Magnétophone externe inactif — 6 novembre 2026

Ce bloc ne reprend pas le regroupement SERPROC retiré ci-dessus : aucune source de la puce ni du MC6850 n'est modifiée. Le magnétophone externe notifie son besoin de traitement lors des changements de transport, du moteur, de sélection, de support ou de mode. La carte mémorise cette notification. Sans activité du support ni dernière entrée à retirer, elle appelle directement le câblage série et évite le parcours du magnétophone. Les horloges série continuent au même rythme. Lorsqu'une pause doit retirer une entrée déjà présentée, le dernier appel nécessaire est conservé au cycle antérieur : pas de retrait anticipé du signal.

Deux nouveaux tests autonomes comparent le chemin permanent et le chemin notifié sur 105000 cycles, avec insertion, moteur, pause/reprise, sélection RS423, entrées RS423, recherche, enregistrement, remplacement d'index, reset, éjection et fin de bande. Ils vérifient les positions et états, phases et fronts série, sorties tape et IRQ, ainsi que les enregistrements aux transitions. Le second contrôle empêche les notifications répétées pour un état inchangé et fixe le cycle de retrait de la dernière entrée.

Régression intégrée : 36 tests tape/série réussis en Debug (29 s), puis 36 en Release (5 s). Sur le Master complet, les deux couples alternés Debug donnent en moyenne repos 19,094 → 18,379 ms/image, SOUND 19,4975 → 18,695, redessin 19,731 → 19,1595. Cycles, PC, vidéo et dernier bloc PCM identiques. Gain Debug confirmé dans ces scénarios ; aucune approximation matérielle ajoutée.

Les durées Release sont dispersées et ne démontrent pas un gain global constant : certaines médianes sont légèrement plus élevées, notamment SOUND. Les contrôles complémentaires de cycles du thread hôte sont détaillés dans `docs/tasks/machines/BbcMaster/tape-clock.md`. L'essai NoInlining du câblage série n'a pas apporté de gain et a été retiré. Le changement du magnétophone est conservé pour sa suppression des consultations de repos et le gain global Debug ; aucun pourcentage de gain Release n'est revendiqué. Ces tests ne couvrent pas la présentation WPF ni une nouvelle preuve native dans RML.
## Capture et restauration de l'état

`CaptureState` conserve les 32 champs mutables et le registre Control, avec la configuration temporelle. Les intervalles différés restent différés : la capture ne les vide pas et ne crée aucun front. La restauration ne remplace pas les observateurs et ne notifie aucune sortie ; elle rétablit également les niveaux déjà publiés, le séparateur tape et la phase de synthèse.

Trois tests Debug réussis : 112 reprises (8 diviseurs, tape/RS423, 7 instants) suivies chacune de 4 000 cycles, avec comparaison intégrale des états, du PCM tape, des phases et des notifications. Une horloge ou une version incompatible est refusée avant toute mutation. Ces preuves concernent le SERPROC seul ; le support des états de la machine complète reste à intégrer.

### Clôture des niveaux et fronts — 7 novembre 2026

Les groupes de raccordement restés ouverts dans le suivi sont rapprochés des résultats déjà obtenus : 193 tests SERPROC/MC6850 Debug (3 s) et 53 tests intégrés tape/série/audio (11 s), sans changement de la puce depuis ces passages. `PublishSerialPins` est appelé depuis les mutations de sélection, entrées RS423 et sorties du séparateur ; `SerialClocksRaised` transmet séparément les fronts réellement produits par les compteurs internes. Pas de republication d'un ancien front lors d'un changement de niveau modem, ni de consultation des sorties MC6850 à chaque tick du raccordement. Les états initiaux sont présentés lors du branchement/reset. Les preuves et mesures précédentes restent applicables avec leurs limites ; aucune nouvelle exécution de test ni mesure n'est revendiquée pour la clôture documentaire.

## Master 128 — 7 novembre 2026

SAVE puis CHAIN réalisés par MOS 3.50/BASIC IV sur deux cartes neuves successives : le récepteur reçoit seulement le signal enregistré, affiche MASTER TAPE PASS et calcule 46. 527 308 échantillons à 48 kHz ; Debug complet rendu/drainage : 19,703009 ms/image dans la fenêtre SAVE, 20,836396 ms/image dans la fenêtre CHAIN (283 images, 11 301 888 cycles, 1,916646 MHz). Aucun code SERPROC/ACIA modifié ni gain supposé. Captures inspectées sous `artifacts/TestsChips/BbcSerialUla/BBC Master 128/20261107-103140-master128-tape-save.bmp` et `20261107-103150-master128-tape-chain-pass.bmp`, également sous TestsMachines. [Conditions, limites et protocole](<../../project/emulations/Acorn/BBC Master/Master 128/tape.md>).
