# ULA série BBC — SERPROC (Acorn 201602 / 2C199)

## Accès internes et réutilisation — 5 novembre 2026

Les sélections moteur/RS423 sont décodées aux écritures du registre, et les accès internes aux sorties de la puce utilisent leurs champs, sans modifier l'interface publique ni les horloges. Le module conserve la référence du composant série et l'override de débit dont il possède les écritures ; il évite de redemander ces valeurs à chaque accès. Une cassette absente est vérifiée avant de consulter son moteur, mais l'électronique série continue de recevoir tous ses cycles.

L'essai de calcul différé du synthétiseur a été retiré faute de gain isolé établi : sa phase et sa sortie sont toujours calculées à chaque Tick comme précédemment. Le groupe de puces SAA5050/VIDPROC/SERPROC compte **210 réussites en Debug et Release** après ces changements. Les essais supplémentaires temporaires liés au synthétiseur différé sont retirés avec cette tentative ; aucune optimisation propre au seul BBC n'est intégrée au modèle du SERPROC.

## Périmètre et constat initial

Circuit des BBC Micro A/B/B+, réutilisé dans le Master équipé du port série. L’ULA ne possède ni fichier cassette, ni index de blocs, ni pilote série Windows. Elle fournit les horloges et le multiplexage TTL autour du MC6850, le séparateur cassette, la détection de porteuse et le modulateur de sortie. Les buffers RS423, l’amplificateur d’entrée, le relais et les composants RC sont externes.

Avant reprise, `Cassette.WriteUla` ignorait les bits 0–5 et l’ACIA utilisait une horloge fixe de 19 200 Hz. Le bit RS423 bloquait CTS sans entrée distante. La démodulation et la modulation étaient incorporées au transport des échantillons. Ce constat porte sur le raccordement, pas sur une absence de fonctions du MC6850 déjà vérifié.

## Sources confrontées

- [Acorn BBC Microcomputer Service Manual, section 3.5 et figure 9](https://acorn.huininga.nl/pub/unsorted/manuals/BBC%20Microcomputer%20Service%20Manual-HTML/BBCServiceManual.html) : deux générateurs, entrée 16/13 MHz, division 1024 du ton bas, multiplexeurs RXD/RXC/DCD/TXD/RTS/CTS ; synthèse activée par RTS ; réseau RC de porteuse externe et dépendant des révisions.
- [Advanced User Guide, sections 20.6 et 20.9, pages imprimées 388–394](https://stardot.org.uk/mirrors/www.bbcdocs.com/filebase/essentials/BBC%20Microcomputer%20Advanced%20User%20Guide.pdf) : table non linéaire des sélections, division ACIA 64 en RS423 ; sélection ULA 300 avec ACIA /16 ou /64 pour cassette 1200/300.
- [b2 serproc](https://github.com/tom-seddon/b2/blob/master/src/beeb/src/serproc.cpp) et `serproc.inl` : table nominale non linéaire ; le générateur simplifié ne constitue pas une preuve du circuit et b2 n’émule pas la cassette.
- [B-Em serial.c](https://github.com/stardot/b-em/blob/master/src/serial.c) : registre, sélection, lecture ayant l’effet d’une écriture FE ; ne pas reproduire la dépendance du relais à la présence d’un fichier.
- [JSBeeb serial.js](https://github.com/mattgodbolt/jsbeeb/blob/main/src/serial.js) et `acia.js` : sélection des entrées et lecture FE. La table linéaire de `serial.js` diffère du guide technique ; retenir la table documentée, pas une majorité d’émulateurs.

## Registre et horloges

Le décodage FE10–FE17 appartient à la carte. D0–D2 choisissent TXC, D3–D5 le générateur RXC RS423, D6 choisit RS423 (1) ou cassette (0), D7 commande le moteur indépendamment du média. Une lecture est modélisée avec l’effet d’une écriture FE et une valeur retournée nulle, conformément aux deux implémentations cassette consultées ; la valeur électrique du bus non conduit n’est pas garantie par la documentation.

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

Le débit physique de la première ligne est 16 000 000 / 13 / 64, soit environ 19 230,77 bauds. Ne pas arrondir l’oscillateur à 1,23 MHz. TXC reste programmable en cassette ; RXC cassette provient du séparateur de données et non du générateur RS423. Le MC6850 conserve la qualification de start, les bits, la parité, les arrêts, les erreurs et IRQ.

## Signaux et limites du modèle numérique

Les broches RS423 sont représentées après les buffers externes, en niveaux logiques TTL. En cassette, CTS vers l’ACIA est bas, DCD vient du détecteur de ton haut, RXD/RXC du séparateur ; les sorties RS423 sont inactives. En RS423, DCD est bas, RXD/CTS viennent du périphérique externe ; TXD/RTS sortent de l’ACIA. Le moteur demeure commandé par D7 dans les deux modes.

Le schéma ne donne pas le réseau de portes interne du séparateur, la table des niveaux de synthèse ni les tolérances des différentes révisions. Une réalisation numérique devra donc expliciter sa qualification des demi-ondes et son délai RC nominal, conserver les fréquences et la modulation, et ne pas prétendre reproduire une amplitude analogique mesurée. Aucun relevé matériel n’est demandé à l’utilisateur pour ces informations non publiées.

## Vérifications à réaliser

Toutes les sélections RX/TX séparément et ensemble ; fréquence et phase sur des intervalles non entiers ; changement de registre sans remise arbitraire à zéro de l’oscillateur ; lecture avec effet FE ; moteur sans média ; isolation et polarités des deux chemins ; RTS, CTS, DCD ; tonalités, silence et signal invalide ; réception et émission à 300/1200 via un véritable MC6850 ; conservation des octets et erreurs ; reset de carte sans rembobinage. Confirmer par programmes invités BBC A/B/B+ et captures ; mesurer la boucle de carte sans modifier les options du compilateur.

## Réalisation

`IO/Acorn/SerialUla/BbcSerialUla` fournit l’interface de broches et les services du circuit. Les deux générateurs partagent un compteur libre alimenté par un accumulateur entier 16 000 000 / 13 ; les écritures changent les prises du diviseur sans remettre son compteur à zéro. RX cassette conserve sa cadence de 19 230,77 fronts/s, indépendante de la sélection RX RS423. Les broches d’horloge appellent ClockReceive/ClockTransmit du MC6850 : aucune livraison directe d’octets ni réception imposée par l’hôte.

Le service `Serial` dans le module Acorn raccorde les broches ; `Cassette` conserve les touches, positions, échantillons, index, sauvegardes et latences du magnétophone. L’ACIA de la carte utilise le mode horloge externe, sans générateur interne parallèle. Sa nouvelle propriété de lecture ClockDivider expose le diviseur déjà programmé, sans modifier ses séquences : elle permet de conserver l’option utilisateur de débit cassette comme adaptation de carte, distincte du circuit physique nominal. Les 23 essais de transport existants ont été conservés et adaptés aux vraies sélections ULA.

La sortie cassette utilise une sinusoïde numérique nominale à seize niveaux de phase, aux fréquences physiques 16 MHz / 13 / 1024 et /512 ; RTS et la sélection RS423 la coupent. Le séparateur qualifie les demi-ondes dans la fenêtre numérique documentée ici, avec seuil de 300 µs, retour mark après silence et impulsion de porteuse après 836 demi-ondes hautes (environ 174 ms). Le détail des portes internes, du DAC et du réseau RC de chaque révision n’étant pas publié dans les sources obtenues, ces paramètres sont un modèle nominal explicite, pas des mesures de toutes les révisions 2C199. L’amplitude analogique et les filtres externes ne sont pas présentés comme une simulation électrique.

## Résultats du 4 novembre 2026

### Vérifications autonomes

99 cas propres au SERPROC : 64 combinaisons RX/TX avec compte exact des fronts sur une durée fractionnaire ; 24 boucles sur un vrai MC6850 (huit sélections et divisions 1/16/64) ; fréquence des deux tons et coupure RTS ; sélection des broches et moteur sans média ; phase conservée et lecture FE ; horloge cassette indépendante de RX RS423 ; porteuse et silence ; état initial et entrées externes conservées ; CTS bloque puis libère réellement un octet ACIA.

La suite complète Chips donne **2 463 réussites, aucun échec ni test ignoré en Debug et en Release**. La suite cassette du module donne **23 réussites dans chaque configuration**, dont émission/relecture 300/1200, formats série et parité, cadences PCM 22 050/44 100/48 000, latences, protection, positions, enregistrement non destructif et chemin direct validé.

### Preuves exécutées par les BBC

Une ROM de banc synthétique exécute le 6502, configure CRTC/ULA vidéo et les registres FE08–FE10, attend la réception matérielle, compare chacun des 64 octets à sa table en ROM et écrit elle-même PASS ou FAIL dans la RAM vidéo. Le verdict ne vient pas du dessinateur PNG : celui-ci exporte seulement le BGRA produit par la chaîne vidéo native et le SAA5050. Le test ne remplace aucun firmware de la configuration utilisateur.

| Carte | Réception FSK 300 / 1200 | Enregistrement par le 6502 et relecture par un autre BBC du même modèle |
|---|---|---|
| Model A 16 Kio | 64/64 et PASS aux deux débits | 1200 bauds, 48 473 échantillons PCM à 48 kHz, 64/64 et PASS |
| Model B 32 Kio | 64/64 et PASS aux deux débits | 300 bauds, 122 754 échantillons PCM à 48 kHz, 64/64 et PASS |
| B+ 64 Kio | 64/64 et PASS aux deux débits | 1200 bauds, 48 472 échantillons PCM à 48 kHz, 64/64 et PASS |
| B+ 128 Kio | 64/64 et PASS aux deux débits | 300 bauds, 122 753 échantillons PCM à 48 kHz, 64/64 et PASS |

Captures natives 640 × 256 conservées hors Git sous `artifacts/TestsChips/BbcSerialUla/BbcMicroModelA/`, `BbcMicroModelB/`, `BbcMicroModelBPlus64/`, `BbcMicroModelBPlus128/` : `receive-300.png`, `receive-1200.png`, et, pour le débit d’enregistrement du tableau, `transmit-<débit>.png` et `record-replay-<débit>.png`. Les essais RX/TX RS423 aux 24 combinaisons de sélection/division sont prouvés par le banc autonome de broches, pas attribués à ces seules captures cassette.

Les 12 scénarios invités, les 23 contrôles cassette et le relevé de performances donnent **36 réussites en Debug et 36 en Release**. Les huit empreintes vidéo de réception sont identiques entre les deux configurations ; les quatre relectures d’enregistrements retrouvent la même image PASS que la réception synthétique du modèle et débit correspondants. Ce résultat ne déclare pas les machines BBC complètes : Winchester et la validation finale des équipements restent dans leur bloc propre.

### Optimisation mesurée

Banc temporaire en processus de test, 100 000 cycles d’échauffement puis trois passages de 4 000 000 Tick par contrôle 00/85/40/FF, changement périodique de TXD, aucune sortie audio Windows. Compilation Debug normale : médiane initiale 140,03 ns/Tick pour 00, 137,10 pour 85, 51,33 pour 40 et 52,01 pour FF. Après table de synthèse stockée une seule fois et seuils temporels précalculés, dernière passe : **58,71 / 52,76 / 53,37 / 51,36 ns/Tick** respectivement. Pas d’allocation par Tick ; aucun cycle de carte, signal ni filtre supprimé. Les différences inférieures à quelques ns entre passages ne sont pas interprétées comme des gains certains.

Ces coûts sont ceux de l’ULA isolée, pas les FPS de toute la machine. Les captures synthétiques ne constituent pas un test de tous les jeux ou médias BBC ; ceux-ci relèvent de la validation de machine demandée ensuite.

Release normal, même banc final : médianes **4,96 / 4,86 / 5,39 / 6,55 ns/Tick** pour 00/85/40/FF. Les boucles de réception invitées de 64 octets prennent environ 189–200 ms à 1200 bauds et 552–581 ms à 300 bauds en exécution non bridée ; ce coût inclut le CPU, toute la carte, le rendu et trois champs supplémentaires après verdict. Ce n’est pas une mesure de latence du magnétophone réel ni un résultat de l’application avec affichage Windows.

## Livraison et clôture

### Réouverture de l’intégration cassette, 5 novembre 2026

Le scénario MOS réel `*SAVE RML 2000 2200`, puis relecture WAV physique, reproduit `Data?` sur les deux blocs. Les cinq régressions autonomes couvrant les 256 octets, en rafale et avec pauses jusqu’à 50 003 cycles, passent : elles ne suffisent donc pas à prouver le protocole MOS complet. La trace du bus montre les données source intactes et deux octets de synchronisation successifs avant chaque en-tête.

Un défaut est établi dans le détecteur : après 836 demi-ondes hautes, `_carrierPulse` était armé une seule fois, puis expirait même si le ton haut continuait. La section 3.5 du [manuel de maintenance Acorn](https://acorn.huininga.nl/pub/unsorted/manuals/BBC%20Microcomputer%20Service%20Manual-HTML/BBCServiceManual.html) décrit la détection du ton entrant et sa temporisation RC ; les [routines MOS de lecture](https://tobylobster.github.io/mos/mos/S-s19.html) attendent effectivement la disparition de DCD après l’amorce avant de chercher la synchronisation. Une impulsion unique au début du ton ne respecte pas ce contrat.

La correction réalisée réarme le maintien nominal à chaque demi-onde haute valide après qualification initiale, avec arrondi du délai au cycle supérieur. Elle conserve les seuils et la retombée sur ton bas/silence. Les **100 tests ULA Release passent**, dont la nouvelle vérification continue après amorçage puis retombée sur ton bas ; les **cinq régressions d’émission/relecture des 256 octets passent** avec pauses de 0, 37, 1 021, 20 003 et 50 003 cycles.

Le banc MOS B réel reproduit toutefois encore `Data?` après cette correction, avec la DLL Chips effectivement recopiée et son empreinte vérifiée. Le maintien DCD est un défaut corrigé, mais n’est pas établi comme la cause suffisante de l’échec SAVE/LOAD. La trace des écritures CPU vers FE09 contient deux synchronisations 2A avant le nom, les données source intactes et des CRC d’en-tête cohérents lorsqu’on exclut le 2A supplémentaire ; le rôle des horloges/interruptions dans cette séquence reste à isoler. Aucune correction du MC6850 ou du CPU n’est déduite de cette observation seule.

La clôture précédente est historique : l’intégration d’enregistrement MOS a été réouverte après le résultat `Data?`. La capture `record-replay-wav.png` du Model B est renouvelée par chaque passage ; elle ne constitue donc pas une archive de cet échec intermédiaire.

Le passage final du 5 novembre réussit les 16 combinaisons A/B/B+ 64/B+ 128 × WAV/UEF/CSW/RAW : les 512 octets enregistrés par SAVE sont restitués par LOAD en lecture physique, et les blocs indexés ont leur intégrité valide et leurs données exactes. Outre le maintien de porteuse ULA, ce résultat nécessite la correction distincte de phase de transmission MC6850 (`6b9ee07bb`) et le recalage UART de l’indexeur MediaEngine. Les preuves par machine sont décrites dans [AcornBBCMicro.md](../../machines/AcornBBCMicro.md). Cette réussite ne doit pas être attribuée à la seule ULA.

Le passage Debug des **100 tests ULA réussit également (658 ms)**. La correction autonome du maintien de porteuse est validée dans les deux configurations ; l’écart MOS restant est traité séparément, sans déclarer que cette correction suffit au chargement d’un enregistrement complet.

Le diagnostic temporaire `AcornEmuBbcSerialDiagnostic.cs` a été supprimé après les mesures et captures. Les assemblies de tests ont été reconstruites en Debug et Release ; elles ne contiennent plus cette classe. Les 99 tests autonomes de puce et les 23 tests cassette utiles sont conservés, sans dépendance aux ROM ou fichiers de diagnostic externes.

`scripts/local-building.cmd --building=debug --modules=A` est terminé avec code de sortie zéro : exécutable `build/Debug/Retro Media Lab/retromedialab.exe`, DLL Chips dans `lib`, 24 manifestes de modules avec chacune leur DLL d’entrée présente. L’empreinte de la DLL Chips livrée correspond à celle du build Debug testé. Les références et journaux temporaires sont supprimés ; les seize captures restent hors Git.

Les fonctions numériques décrites sont réalisées et validées ; les seules limites retenues sont celles du détail analogique/interne non publié, explicitement décrites plus haut. La clôture de ce circuit ne ferme pas les points machine encore ouverts.

### Essai des invariants de calcul, 5 novembre 2026

Essai groupé : périodes des deux tons et masques de diviseur préparés hors Tick, masque de phase à seize pas, sortie anticipée du séparateur déconnecté après silence établi. Les cinq nouveaux cas vérifient la phase exacte de synthèse à 1 230 769, 2 000 000, 2 000 003 et 4 000 000 ticks/s, en changeant TXD, RTS et les 256 contrôles, ainsi que la reprise du séparateur après une longue déconnexion. Les 105 contrôles série passent en Debug avec l'essai ; les cinq contrôles de raccordement passent également.

Le banc Debug de carte donne 31,0414 ms/tranche, 32,21 FPS, 1,29 MHz ; cassette/série isolée 3,1710 ms pour 39 936 cycles, contre 3,1501–3,3050 ms dans les passages précédents. Aucun gain global n'est établi. L'essai de production a donc été retiré, sans réintroduire d'ancien code antérieur aux corrections de porteuse et de câblage. Les cinq tests autonomes supplémentaires restent utiles pour les phases fractionnaires et la reprise d'entrée. Les 105 cas passent sur le code restauré en Debug (926 ms) et Release (441 ms) ; les cinq raccordements passent dans chaque configuration. Les journaux temporaires du passage sont supprimés après consignation. L'échec à améliorer les performances ne constitue pas une correction du déficit de vitesse de la machine.

## Essai retiré : instantané des sorties SERPROC — 5 novembre 2026

Le regroupement de TXCLK/RXCLK/CTS/DCD dans une structure retournée après le Tick existant a été comparé au module Debug livré, dans deux contextes collectables utilisant exactement la même assembly Chips. Sept passages alternés de 200 000 cycles après 20 000 cycles de chauffe, compilation tiercée désactivée uniquement pour ce processus de mesure ; aucune option du produit modifiée. Entrée cassette déconnectée, quatre commandes et deux débits forcés. Les deux contextes ont été déchargés en finally et leur disparition vérifiée après collecte.

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

109 contrôles autonomes réussis, dont quatre comparaisons cycle par cycle à 1 230 769 / 2 000 000 / 2 000 003 / 4 000 000 Hz : toutes commandes, signaux RS423, entrée cassette nullable, synthèse et remise sous tension. Les cinq contrôles du raccordement ACIA et la comparaison appariée passent également. Le regroupement est néanmoins plus lent dans six charges sur huit : API, structure, utilisation et quatre tests spécifiques retirés intégralement. Le banc temporaire et la copie Release ont été supprimés ; mesure Release annulée après ce rejet. Les tests préexistants et les optimisations antérieures restent conservés. Aucun nouveau résultat de cadence native ne découle de cet essai.

## Essai retiré : intervalles stables et raccordement BBC — 5 novembre 2026

**État actuel : essai retiré du circuit, du contrat et du module. Les descriptions et mesures ci-dessous sont historiques. Aucun cycle série n’est différé dans la version restaurée.**

`Advance(cycles, TXD, RTS, out TXC, out RXC)` avance les accumulateurs de source, diviseurs, séparateur et synthèse avec les mêmes états finaux que les appels unitaires. Le premier cycle conserve la reconnaissance d'une éventuelle transition cassette. La suite compte les périodes et les échéances sans répéter chaque appel. Les nombres de fronts transmis sont complets ; les propriétés de fronts décrivent le dernier cycle. L'appelant reste responsable de découper avant une modification d'entrée ou une échéance qu'un autre composant doit observer.

La limite basse de fréquence acceptée reste strictement équivalente : le modèle unitaire ne produit qu'un front source par cycle, même lorsque son petit reliquat de phase s'accumule. L'avancement groupé conserve cette borne. Les tests comparent tous les champs internes et les nombres de fronts aux fréquences 1 230 769, 2 000 000 et 3 000 000 Hz, tous les contrôles, les changements de niveaux, TXD/RTS, la qualification porteuse, son impulsion et son expiration. Un intervalle de 5 400 000 cycles vérifie le reliquat à la fréquence minimale.

Le module BBC regroupe au plus 64 cycles seulement lorsque RS423 est sélectionné, RXD haut, TX/RX au repos et les broches modem stables. Les accès ACIA/SERPROC et l'observation du composant synchronisent d'abord les cycles écoulés. Tout état actif et toute entrée cassette restent sur le chemin unitaire. Aucun circuit ne connaît une autre puce : c'est le câblage du module qui applique ces conditions. 182 contrôles MC6850/SERPROC et six raccordements BBC réussissent dans chaque configuration Debug/Release.

Après retrait : groupes série/vidéo sélectionnés 271 cas réussis par configuration, raccordements BBC 158 cas réussis par configuration. Les horloges de chaque cycle sont de nouveau traitées immédiatement. Les anciennes captures du regroupement ne décrivent plus le chemin actuellement conservé.
