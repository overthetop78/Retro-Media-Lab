# Acorn System 1 — reprise du 8 novembre 2026

## Montage et références conservées

Carte `Keypad6502`, horloge du profil existant **1 008 000 Hz**, RAM CPU de 1 Kio et INS8154 avec ses 128 octets de RAM, moniteur `Acorn System 1 Monitor.bin` de 512 octets (SHA1 `B987354C55BEB5E2CED761970C3339B895A8C09D`), matrice hexadécimale et afficheur multiplexé à huit chiffres, interface de bande. Aucun VIA, VDU, FDC, HDD ou PSG ajouté au montage standard. Les extensions Eurocard et le deuxième 8154 optionnel ne sont pas ajoutés automatiquement par ce passage ; leur réalisation éventuelle reste distincte du montage contrôlé.

Sources réutilisées : [moniteur original désassemblé et commenté](https://theoddys.com/acorn/acorn_system_software/system_1_monitor/monitor.html), archive `artifacts/.tmp-archives/machines/Acorn/Acorn System 1/Monitor.ca65` ; [composition des cartes 6502, 6502A et 6809](https://github.com/mamedev/mame/blob/master/src/mame/acorn/acrnsys.cpp), copie `acrnsys.cpp` au même endroit. Le 8154 statique ne reçoit aucun oscillateur inventé : ses fronts de bus et de handshake sont ceux de sa [fiche et de ses tests existants](../../../../../chips/IO/Ins8154.md).

## Défauts de raccordement corrigés

- `AcornSystemMachine` instancie maintenant le VIA uniquement pour les cartes qui le décodent (6502A rapide et 6809) ; les cartes 6502 à 8154 ne le cadencent plus. Le clavier ASCII n'avance plus sur le System 1 à matrice passive.
- Le 6502 reçoit les changements IRQ/NMI des composants raccordés et mémorise leurs niveaux par ses entrées existantes. L'échantillonnage aux phases CPU est conservé. Les broches auxiliaires constantes sont déclarées par `ICpuBus.ControlSignals`, sans désactiver un signal réellement variable.
- `SystemKeyboard` résout les contacts de la matrice lors des changements de l'entrée hôte. La lecture de la rangée demandée conserve les lignes de sélection et le niveau bande courants ; aucun balayage, front ou accès RAM du moniteur n'est supprimé.

Aucune instruction, fréquence, phase interne ou fonction de puce n'est modifiée. Le 6502 et l'INS8154 sont réutilisés ; ce bloc corrige leur assemblage. Les tests de présence du VIA et de réception/acquittement de son IRQ sur la carte rapide protègent le code partagé, sans prétendre valider tous les modèles System en ROM réelle.

## Comparaison Debug de la carte complète

Même ROM, 20 présentations d'échauffement puis trois fenêtres de 160 présentations ; `RunFrame` et `DrainAudio` inclus, aucune compilation/test/émulation concurrente. Une présentation dure 20 ms émulées. Le System 1 n'a pas de CRT : **50 présentations/s est la cadence de présentation logicielle de ses LED, pas une fréquence vidéo matérielle inventée**.

| Fenêtre | Avant ms/présentation | Après ms/présentation | Présentations/s sans limite après |
| --- | ---: | ---: | ---: |
| 1 | 2,912085625 | 2,032464375 | 492,013544 |
| 2 | 2,850624375 | 1,993667500 | 501,588153 |
| 3 | 2,877123125 | 2,022210000 | 494,508483 |

Coût moyen réduit d'environ **30 %**. Les empreintes vidéo de chaque fenêtre sont identiques avant/après : `36EFF9827C1207EDBCA5481EEF2864B88A2094F6250EEAC2BDF23833C4C0ADB1`, `4425A7E49E9E707D4953684B1A189413A33AF1C4F1EADBDCF11D2FBD2A6BAE38`, `CF27553B8F288BB60CAAF88C9F141E2F888117A5328E847FD097CF4FEB4C0814`. RAM 0000–03FF : `1229F94572BB17068B2927D22F4214C4CEF75D062608BA2C2CBDF2CE226F3AB1` dans les six fenêtres.

Un passage final relève aussi les cycles réellement consommés, lus hors du chronométrage :

| Fenêtre | ms/présentation | Cycles pour 160 présentations | MHz effectifs | Vitesse du profil |
| --- | ---: | ---: | ---: | ---: |
| 1 | 2,078518125 | 3 225 599 | 9,699215 | 962,223690 % |
| 2 | 2,052366875 | 3 225 600 | 9,822805 | 974,484642 % |
| 3 | 2,017500000 | 3 225 600 | 9,992565 | 991,325898 % |

Ce sont des capacités sans limite hors RML. La cadence limitée reste 1,008 MHz pour le profil ; aucun FPS présenté dans la fenêtre ni gain Release n'est déduit. Le dernier passage fonctionnel avait mesuré 1,94–1,98 ms/présentation ; ce meilleur passage ne remplace pas la comparaison initiale ci-dessus.

## Programme réellement exécuté et bande

Le banc utilise seulement les entrées normales du clavier pour le moniteur : M est porté par `EmulationKey.Minus`, G par `EmulationKey.X` dans la correspondance existante ; les autres commandes S/L et les chiffres utilisent leurs touches normales. Il maintient chaque appui et relâchement pendant cinq présentations. Ce passage valide l'entrée normalisée du cœur, pas le clavier physique dans la fenêtre RML.

1. M, adresse 0200, terminaison par une commande non hexadécimale ; saisie de chaque octet avec la commande suivante ↑. Vérification de la RAM après chaque écriture du moniteur.
2. Programme saisi : `A9 5A 8D 00 03 6C FC FF` — `LDA #$5A; STA $0300; JMP ($FFFC)`. G 0200 l'exécute ; 0300 vaut 5A, puis le moniteur revient par son vecteur RESET.
3. S 0200, fin exclusive 0208, enregistrement sur une bande vierge writable de 48 kHz ; moteur réglé sans latence pour isoler le protocole. Le moniteur génère réellement les quatre octets d'adresses et les huit octets de programme. Signal conservé : **86 400 échantillons à 48 kHz**, soit 1,8 s avec attente finale incluse.
4. Après arrêt/éjection, le banc efface explicitement la RAM 0200–0207 et le témoin 0300 pour établir le contrôle. Il réinsère le signal enregistré, lance L puis Play et laisse avancer la carte. Les huit octets sont rétablis par le code GETBYTE de la ROM, sans injection de données lors du chargement.
5. G 0200 exécute de nouveau le programme rechargé : 0300 vaut 5A. Verdicts console `MONITOR EDIT + GO PASS` et `TAPE RECORD + SIGNAL LOAD + GO PASS`, sortie 0.

Le code source du moniteur confirme le débit logiciel 300 bauds, l'envoi des adresses par PUTBYTE, la réception par GETBYTE et la fin exclusive de zone. La restitution du lecteur reste le signal, pas un chargement direct de RAM.

## Tests et captures

**19 tests `AcornEmuSystem*` réussis en Debug**, après les corrections. Les nouveaux contrôles couvrent contacts simultanés, rangées indépendantes, relâchement/reset, entrée bande dynamique, présence réelle du VIA selon la carte et réception/acquittement d'une IRQ timer par un programme CPU. L'ancien test d'options totalement vides a été aligné sur l'ajout antérieur des options audio masquées ; la production n'a pas été changée pour satisfaire cette attente périmée. Les erreurs initiales des nouveaux tests venaient de tailles de ROM de caractères synthétiques inadéquates ; les fixtures utilisent 960 octets pour le télétexte et 2048 pour la carte 80 colonnes.

Captures natives 480 × 160, sans retouche, préfixe `20261108-system1-` : `monitor`, `memory-edited`, `program-executed`, `tape-recorded`, `tape-loaded`, `tape-program-executed`, suffixe `-debug.bmp`. Dossiers `artifacts/TestsMachines/Acorn/Acorn System 1/` et copies `artifacts/TestsChips/Ins8154/Acorn System 1/`. Les étapes mémoire modifiée et programme rechargé/exécuté ont été ouvertes et inspectées. Elles représentent les segments réellement commandés, aucun texte PASS ajouté par l'hôte.

Le banc n'ouvre aucune fenêtre et possède uniquement ses flux BMP, fermés par using. Il relâche les entrées, détache les callbacks et éjecte la bande dans finally. Nettoyage du banc temporaire et livraison Debug avec tous les modules suivis dans `docs/tasks/machines/AcornSystem/system1-validation.md`. Essai final RML distinct, non revendiqué à ce stade.

Livraison Debug du 8 novembre : script `--building=debug --modules=A` terminé avec code 0. Exécutable, Chips dans lib et DLL des 24 manifestes vérifiés. SHA256 Acorn livré/compilé win-x64 identique : `25081D852529F99AB81B0CE9531D459E6DB25E5ACE3C402E084CE1EB40B23E9C`. Le dossier temporaire `.tmp-system1` et ses sorties sont supprimés ; aucun test dépendant du firmware externe n'est ajouté aux projets permanents. RML n'est pas lancé par le build.

## Essai utilisateur dans RML et corrections clavier en attente de livraison

Dans la fenêtre Debug livrée, X ouvre la saisie d'adresse, les chiffres de la rangée principale permettent 0200, puis FEF3 et X ramènent aux points du moniteur. Le vecteur RESET FEF3 provient du fichier ROM utilisé. Aucun compteur de cadence n'est visible dans cette fenêtre ; aucune mesure RML supplémentaire n'est revendiquée.

L'utilisateur signale que les chiffres du pavé numérique restent sans effet. L'appui physique sur M laisse aussi les points affichés : la matrice associait la commande mémoire à Minus uniquement, et l'exécution à X. Le module reçoit désormais les alias Numpad0–9 et M/G sur les contacts existants, en conservant les anciennes sources et leurs relâchements indépendants. Résolution uniquement à SetInput, aucun traitement ajouté au Tick, aucune modification des circuits ou du moniteur.

Les tests de ces alias et relâchements sont ajoutés dans AcornEmuSystemRamIoTests.cs. **34 tests AcornEmuSystem réussis en Debug**, aucun échec, erreur, abandon ou expiration, passage terminé le 8 novembre à 13:42:15. Le fichier `system1-keyboard-debug.trx` conserve le résultat dans le dossier des preuves machine. Les 19 tests et mesures ci-dessus précèdent cette correction ; ce passage ajoute 15 cas de clavier. Le test recompile ses sorties sous bin/Debug, sans script de livraison ni fermeture de RML. À la demande de l'utilisateur, pavé numérique et M/G corrigés restent à vérifier physiquement au prochain build livré utile.

Captures natives de fenêtre, sans retouche : `20261108-system1-rml-debug.jpg`, `20261108-system1-rml-address-debug.jpg`, `20261108-system1-rml-memory-key-debug.jpg` dans `artifacts/TestsMachines/Acorn/Acorn System 1/`. La dernière montre l'absence d'ouverture mémoire après M ; elle ne représente pas une validation du correctif encore non livré.

Nouvelle livraison autorisée ensuite par l'utilisateur : `scripts/local-building.cmd --building=debug --modules=A` terminé avec code 0. Exécutable, Chips/lib et 24 modules contrôlés. La DLL Acorn livrée correspond à la compilation win-x64, SHA256 `D289D4B4A693EB9875DEDF9AE9286970D5AB9359A9E48E9009BD1FF38E0C7EAB`. Les alias M/G et le pavé numérique sont désormais dans le build livré ; leur vérification physique reste la prochaine étape, distincte des 34 tests hors interface.

Reprise de l'essai physique après livraison : l'utilisateur démarre System 1 et appuie sur M, sans clic de focus (automatique au démarrage). L'afficheur ouvre bien 0000. Il saisit ensuite 0200 au pavé numérique : l'adresse 0200 est constatée dans RML. Un nouvel appui M ouvre le contenu mémoire. Captures `20261108-system1-rml-memory-key-fixed-debug.jpg` et `20261108-system1-rml-numpad-debug.jpg` conservées dans le dossier machine. Les entrées physiques M et chiffres du pavé numérique sont ainsi vérifiées ; écriture/relecture mémoire et G restent à observer.

## Correction du rendu multiplexé après la saisie physique de 5A

L'utilisateur a effectué M, 0200, M, 5A, ↑, ↓. Les captures RML mélangent ou omettent des segments ; ce constat ne prouve pas un défaut d'écriture RAM. La reproduction hors interface avec le même moniteur et les mêmes entrées confirme 0200=5A. Le défaut est dans la présentation : `WriteSegments` accumulait par OR les nouvelles lignes B sur l'ancienne sélection A. Or la ROM exécute STA PIB puis STX PIA ; la transition de quatre clocks se retrouvait peinte à pleine intensité sur l'ancien chiffre. En outre, la boucle de balayage contient 256 itérations par chiffre (LDA indirect indexé, DEY, BNE) : le balayage des huit chiffres dépasse une présentation de 20 ms. L'effacement à chaque présentation pouvait omettre un chiffre entier.

`SystemHexDisplay` reçoit maintenant les changements des sorties réellement pilotées A/B et leur instant `_ticks`, conserve les niveaux et cumule les durées des segments entre événements. Les broches relâchées ne pilotent pas de LED. Aucun appel par Tick n'est ajouté. Le clavier conserve seulement sa matrice et ses entrées. Le rendu prend l'exposition des deux dernières présentations (environ 40 ms au débit nominal), avec un gain graphique de référence 1/8 ; il s'agit de présentation optique, pas d'une constante électrique, d'une nouvelle horloge ou d'une modification du moniteur. Les transitions courtes gardent leur durée réelle au lieu de devenir des caractères parasites à pleine intensité. Les états et périodes CPU/8154/bande restent inchangés.

La reproduction temporaire vérifie le contenu RAM, l'adresse MAP=0200 et les sept segments des huit chiffres contre les sorties préparées par la ROM. Les octets D sont `F7 3F 5B 3F 3F 80 6D 77`. Le rendu 480×160 montre A, 0200 et 5A ; capture `20261108-system1-memory-led-exposure-debug.bmp` ouverte et inspectée, sans retouche. Le premier rendu limité à une présentation est conservé séparément sous `20261108-system1-memory-led-duration-debug.bmp` et montre le chiffre manquant ; il n'est pas présenté comme réussi.

Tests : le dernier passage commun contient 37 cas réussis et un échec de fixture (début d'exposition supposé nul malgré RESET CPU). Le dernier cas INS8154 est ensuite réexécuté seul avec l'instant réel après RESET : 1 réussi, sortie 0. Les résultats réunis couvrent 35 cas System autonomes, 2 cas de barre commune et 1 contrôle temporaire du vrai moniteur. Le contrôle ne dépendant que du firmware externe est supprimé après usage. Journaux distincts `system1-display-final-debug.trx` et `system1-display-port-debug.trx` ; ne pas présenter le premier comme une série entièrement verte. Le premier essai du moniteur avait identifié le chiffre absent, ce qui a motivé la correction de l'exposition.

Relevé Debug du moniteur en modification mémoire, 160 présentations, CPU et périphériques complets avec rendu et drainage audio : **2,121809 ms/présentation**, **471,296 présentations/s** sans limite, **9,501331 MHz**, **942,592 %** du profil 1,008 MHz. Le scénario diffère du relevé initial au repos ; ce passage confirme la marge, sans annoncer un gain comparatif. Aucun FPS dans RML n'est déduit.

Le cadre statistique WPF était placé sur la surface vidéo, alors que les moteurs Direct3D/OpenGL possèdent un HwndHost natif pouvant passer au-dessus. À la demande de l'utilisateur, les cinq indicateurs existants (moteur dynamique, dimensions, FPS, fréquence, MHz/%) occupent maintenant une ligne compacte distincte au-dessus du viewport. Bindings, icônes et couleurs conservés ; dimensions du viewport données au calcul d'aspect inchangées. Les deux tests de commandes partagés vérifient la séparation des rangées et les valeurs liées, sans fenêtre supplémentaire. Vérification visuelle de la livraison encore nécessaire.

Livraison du correctif LED et de la barre : script Debug `--modules=A` terminé code 0 ; exécutable, Chips/lib et 24 modules contrôlés. SHA256 Acorn compilé/livré identique : `7F122A103813519219CF2BE0B5FCF7F70D84EC6BE0CAA32FE0DBA6DBB93C65A2`. Le diagnostic externe System1MonitorDisplayProbe.cs est supprimé, puis la DLL/PDB des tests sont régénérées sans dépendances et sans nouvelle exécution, code 0. La vérification visuelle RML de cette livraison reste ouverte.

Essai visuel du dernier build : System 1 est ouvert par l'utilisateur, séquence M/0200/M/5A/↑/↓ effectuée. La capture native `20261108-system1-rml-memory-written-debug.jpg` montre clairement le préfixe A, l'adresse 0200 et la valeur 5A ; aucun segment parasite ni chiffre manquant dans cette capture. La ligne Direct3D 11 / 480×160 / 50,0 FPS / 50,0 Hz / 1,01 MHz / 100 % est visible au-dessus de l'image, sans la recouvrir. Les 50 Hz de ce profil sont la cadence de présentation des LED, pas un signal CRT matériel. Ces valeurs sont cette fois observées dans RML, distinctement du banc sans limite. Dernier contrôle G FEF3 G en attente.

### Contrôle GO et correction du critère de test

L'essai G/FEF3/G laissait un mode de saisie affiché. Cela a d'abord été interprété à tort comme une touche ignorée. Le diagnostic avec la vraie ROM, la projection SystemFunctions.MapInput et le CPU de carte confirme pour G et X le symbole de commande F5 et GAP=FEF3. Un appui prolongé conduit effectivement à RESET, mais RESET réinitialise EXEC et relit la touche GO encore maintenue : le mode GO peut donc immédiatement se rouvrir. Le premier test exigeant les huit points après 100 ms d'appui échouait pour cette raison avec les deux associations. Il ne prouvait pas un défaut matériel.

Le contrôle corrigé observe PC après chaque instruction, sans lecture de bus ajoutée, jusqu'à son arrivée réelle à FEF3. L'entrée est alors relâchée avant l'exécution de RESET, puis le moniteur affiche ses huit points. **Deux cas réussis**, G et X, code 0, 731 ms dans `system1-go-reset-observed-debug.trx`. Aucun code de production n'est modifié pour obtenir ce résultat. Le diagnostic temporaire est ensuite supprimé et ses sorties de tests régénérées. Le critère de capture RML porte sur l'état de commande réellement visible, sans présenter une réouverture normale de GO comme un échec d'exécution.

Clôture technique System 1 : moniteur, modification/exécution de programme, signal bande enregistrement/rechargement, matrice et pavé numérique, notifications des circuits réellement présents, rendu LED et barre commune ont leurs preuves ci-dessus. Dans RML : 0200=5A relu après ↑/↓, 50 FPS, 1,01 MHz arrondis, 100 %. GO est prouvé par l'observation de PC à FEF3 avec G et X. La capture GO supplémentaire envisagée est écartée comme critère d'exécution, car le maintien de touche peut réouvrir normalement la commande après RESET ; elle ne constitue plus un travail bloquant. Aucun diagnostic externe ajouté n'est conservé dans les tests permanents. La comparaison physique supplémentaire M/G proposée reste facultative ; les données et les tests établis ne nécessitent pas de nouvelle modification. Le bloc est prêt pour son commit avant passage au System 2.

## Continuité du 9 novembre 2026 — revue des composants et du raccordement

Le 6502 partagé conserve ses cycles et phases ; les interruptions explicitement raccordées sont mémorisées dans le CPU. L’INS8154 reste un circuit statique piloté par les strobes de bus : pas d’oscillateur ajouté. Ses notifications de niveaux/pilotes alimentent directement sélection et segments de l’afficheur, dont l’exposition est une présentation optique séparée. SystemTape conserve ses compteurs, bascules et prochaine échéance ; les transitions de bande et de sortie lui sont transmises, ses sorties sont reçues par notification. La matrice est lue lors des transactions du moniteur, pas par un sondage inter-puces périodique. Aucun VIA ni codeur ASCII absent de la carte System 1 ne reçoit de Tick.

Corrections de ce passage : le constructeur 6502 reçoit la fréquence du profil existant (ClockFrequency était zéro) ; les identifiants du contrôleur SystemTape sont renommés de CassetteInput/Output/OutputChanged vers TapeInput/Output/OutputChanged dans son contrat, son implémentation, la carte et les tests. Les cycles, la fréquence effective et les signaux ne sont pas accélérés. Voir les fiches Mos6502 et SystemTape pour les chemins et la portée détaillée.

Contrôles existants : 10 tests SystemTape Debug réussis en 87 ms, puis 72 tests System/RAM-E/S en 1 s ; zéro échec/ignoré. Aucun nouveau test ajouté ni nouvelle fenêtre RML, média externe ou mesure de vitesse ; les preuves invitées et les performances ci-dessus restent celles de leur passage daté.
