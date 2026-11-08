# Master Turbo — 7 novembre 2026

## ROM, RAM et exécution réelle

Carte Master avec MOS 3.50/BASIC IV, SAA5050, WD1770, interface Winchester sans HDD, coprocesseur ADC06/65C102 et sa ROM externe `Acorn Master Turbo 65C102.rom`. Aucune configuration utilisateur modifiée. Le vrai démarrage affiche `Acorn TUBE 65C102 Co-Processor`, DFS et BASIC.

Le BASIC invité exécute après CLS/RUN un programme qui alloue 40 000 octets, écrit 17 à sa première adresse et 93 à la dernière, vérifie ces deux valeurs puis affiche **TURBO RAM PASS** et **46** pour 12+34. L'allocation dépasse la RAM disponible au BASIC hôte : cette preuve complète le simple affichage d'un bandeau Tube. Aucun programme injecté dans la RAM parasite par le banc, saisie par le clavier émulé.

## Horloges et cadence Debug de la carte complète

160 images après le verdict, avec rendu et drainage PCM, sans RML/audio Windows ni compilation/test concurrent. Les compteurs du CPU parasite sont lus par réflexion dans le seul banc temporaire, sans nouvelle API de production.

| ms/image | Images/s | Cycles hôte | Cycles parasite | MHz hôte | MHz parasite | Rapport |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 31,541715 | 31,7040 | 6 389 758 | 12 779 516 | 1,266132 | 2,532265 | 2 exactement |

Fréquences matérielles configurées : hôte 2 MHz et parasite 4 MHz ; cadence vidéo matérielle environ 50,08 Hz. La capacité libre mesurée vaut environ 63,31 % du temps réel. Aucun timing ou réglage Debug changé ; le budget 19,97 ms n'est pas tenu et la validation utilisateur du Master 128 ne vaut pas acceptation de cette baisse Turbo.

## Fichier depuis la RAM parasite

Une SSD dédiée est créée par le SDK (`acorn.dfs.ss`), ouverte en écriture et montée sur le lecteur 0. Le BASIC parasite remplit 256 octets du tableau avec leurs indices, puis exécute `OSCLI"SAVE TUBERAM "+STR$~T%+" +100"`. Il efface ensuite ces octets et les recharge par `OSCLI"LOAD TUBERAM "+STR$~T%`. Une boucle BASIC compare chacun des 256 octets ; après CLS, le verdict **TUBE FILE PASS** est affiché seulement si toutes les comparaisons réussissent. Compteurs physiques cumulés : 1 280 octets lus, 768 écrits.

Ce passage utilise les appels MOS hôte/parasite et le contrôleur de disquette réel ; il n'écrit pas les octets attendus dans la RAM hôte pour simuler une lecture. Il vérifie un aller-retour en session, pas la persistance après redémarrage d'une machine neuve ni chaque mode du Tube pris isolément.

Captures de framebuffer réellement exécuté, inspectées sous `artifacts/TestsMachines/Acorn/BBC Master Turbo/` et `artifacts/TestsChips/TubeUla/BBC Master Turbo/` : `20261107-105914-turbo-private-ram.bmp` et `20261107-110019-turbo-tube-file.bmp`. Code de sortie 0. Dans finally : entrées et callbacks détachés, tape éjectée, SSD détachée avant Dispose puis supprimée. Aucun média utilisateur écrit. Source temporaire supprimée et banc reconstruit avec son Program.cs préexistant après consignation.

Les puces existantes n'ont pas été modifiées pour ce passage. Les 20 tests Tube déjà consignés restent réutilisés ; aucune nouvelle conformité analogique revendiquée. Le Master 512 conserve séparément son défaut DOS Plus antérieur au bloc Tube ; sa validation ne se déduit pas du Turbo.

## Bus auxiliaire Turbo : comparaison Debug du 7 novembre

`TurboBoard` héritait de `ICpuBus.ControlSignals = All`, alors que ses entrées auxiliaires utilisaient uniquement les valeurs constantes par défaut et que `SetSync` ne faisait rien. Le bus déclare désormais `None` par le contrat existant. Les notifications IRQ/NMI, le maintien RESET par le Tube, les transferts et le rapport des horloges restent inchangés. Aucun code du CPU partagé n'est modifié. Cette déclaration décrit le raccordement émulé existant ; elle ne prétend pas établir un nouveau brochage physique sans schéma.

Deux paires alternées sur carte complète, MOS/client identiques, rendu et drainage PCM inclus, 160 images mesurées après 10 d'échauffement pour chaque scénario ; aucun build ni autre test pendant les mesures. Redessin : boucle BASIC MODE 1/CLS ; SOUND reste actif après son lancement.

| Scénario | Avant 1 / 2 (ms/image) | Après 1 / 2 (ms/image) | Gain moyen |
| --- | ---: | ---: | ---: |
| Repos | 31,069701 / 31,373883 | 29,252626 / 28,565589 | 7,41 % |
| SOUND | 31,958687 / 30,505657 | 29,290609 / 27,709161 | 8,75 % |
| Redessin | 33,924628 / 34,537976 | 31,641221 / 32,216153 | 6,73 % |

Cycles hôte identiques dans les quatre passages : 6 389 763 au repos et en redessin, 6 389 760 avec son. PC et empreintes SHA256 vidéo/PCM identiques par scénario ; le BASIC exécute TUBE SOFTWARE PASS et 46 dans chaque variante. Le gain est retenu ; la cible 19,97 ms reste non atteinte sur ce Turbo Debug, aucune cadence nominale revendiquée.

Le banc a d'abord refusé la substitution d'une ancienne DLL livrée avec une identité de dépendance différente (`retromedialab.chips` 2.0.0.0). Ce passage n'est pas une mesure. La seconde paire a été reconstruite depuis chaque source avec les mêmes dépendances du banc, puis terminée avec code 0 ; aucune modification du chargeur RML pour contourner ce problème de banc.

Régressions de fin de bloc : 20 tests Tube réussis (143 ms), puis 30 tests AcornEmuMasterTests réussis (850 ms), en Debug. Aucun échec. Les avertissements de compilation existants ne sont pas des résultats de performance.

Livraison Debug All du 7 novembre après ce bloc : script terminé avec code 0 ; exécutable et lib/retromedialab.chips.dll présents, 24 modules avec leur DLL d'entrée, empreinte de la DLL Acorn livrée identique à la compilation win-x64. Chemin : build/Debug/Retro Media Lab/retromedialab.exe. L'essai utilisateur du Turbo reste à faire ; aucun nouveau commit partiel du Turbo.

## Essai de sélection dans RML et reprise manuelle

La livraison Debug a été lancée par CUA, aucune instance RML n'étant présente avant cet essai. Le Master 128 enregistré a été ouvert sans démarrer le cœur. Ses paramètres Extensions indiquaient « Aucune » pour la carte du coprocesseur. Le menu proposait bien Master Turbo (65C102, 4 MHz, 64 KiB) et Master 512. Les tentatives automatisées au clavier puis par coordonnées du menu n'ont pas conservé le choix Turbo : la dernière observation affiche encore « Aucune ». Aucun démarrage Turbo, compteur Turbo ou capture de verdict Turbo dans RML n'est revendiqué pour ce passage. La cause de cette non-sélection n'est pas attribuée au code de la machine sans reproduction indépendante du contrôle.

Les paramètres ont été fermés et l'instance possédée a été fermée dans le bloc de nettoyage ; l'inventaire CUA suivant et Get-Process ne contiennent plus RML. Aucun autre logiciel utilisateur fermé. L'utilisateur propose de réaliser la manipulation lui-même ; la main lui est laissée pour sélectionner le Turbo et démarrer. Ne pas lancer de build, de mesure concurrente ou de clic dans sa session pendant cet essai. Les mesures natives précédentes restent distinctes.

## Turbo réellement ouvert par l'utilisateur — 7 novembre

L'utilisateur a ouvert une nouvelle instance de la livraison Debug et sélectionné le coprocesseur manuellement. Le contrôle reste en observation seule : aucun clic, saisie, fermeture, build ou banc concurrent dans cette session empruntée. Le démarrage affiche réellement « Acorn TUBE 65C102 Co-Processor », Acorn 1770 DFS et BASIC. Premier relevé visible : 640 × 512, 50,1 Hz matériels, 34,2 FPS, 1,37 MHz hôte et 68 %. Ce relevé ponctuel confirme le ralentissement déjà mesuré hors interface ; il ne mesure pas directement le CPU parasite à 4 MHz.

La capture conservée montre ensuite les commandes utilisateur *TAPE puis *CAT, avec BARBARIAN 00 lu sur la tape et son transport en lecture. Relevé à cet instant : 32,1 FPS, 1,28 MHz hôte, 64 %, 50,1 Hz vidéo. Aucun verdict de chargement complet ni exécution du jeu n'est encore acquis. Fichier : artifacts/TestsMachines/Acorn/BBC Master Turbo/20261107-rml-turbo.png, copie identique sous TestsChips/TubeUla/BBC Master Turbo. La fenêtre appartient à l'utilisateur et doit rester ouverte pendant son essai.

La lecture du catalogue progresse ensuite de BARBARIAN 00 vers BARBARIAN 04 046E, Barb 05 0600 et Screen (03 puis 0C). Le passage suivant affiche Escape puis l'invite BASIC ; le contrôle n'a envoyé aucune touche. La capture 20261107-rml-turbo-tape.png conserve cette dernière étape dans les deux dossiers de preuves machine/Tube. Compteurs visibles : 34,3 FPS, 1,37 MHz hôte, 68 %, 50,1 Hz, 640 × 512. La saisie utilisateur et le retour du catalogue sont observés ; aucun jeu exécuté ni verdict de transfert HDD dans RML revendiqué. Ces captures ne constituent pas une acceptation utilisateur de la vitesse Turbo.

SimCity : l'utilisateur confirme le même jeu incorrect avec coprocesseur et correct sans. L'inspection en lecture seule du chargeur identifie MODE 5 puis *L.SIMSCR1 5800, visant la RAM parasite avec le Tube actif alors que l'écran reste sur l'hôte. Voir [compatibilité logicielle et preuves](software-compatibility.md). Aucun changement des puces ni de la présentation pour masquer cette incompatibilité de chargement.

## Présentation des champs vidéo : clarification pendant l'essai

**Actualisation depuis le code courant, reprise Turbo du 7 novembre :** le paragraphe historique ci-dessous ne décrit plus le transfert actuel. `Machines/BbcMicro/Common/Services/Video.Fields.cs` expose désormais `OutputHeight => Height` et `ScanMode` progressif ou champ pair/impair selon `_completedParity`. `Video.Rendering.cs` convertit uniquement `_completed` sur `Height` lignes ; il ne compose pas le champ précédent avec le nouveau. `Services/EmulatorCore.cs` transmet `machine.VideoScanMode` dans la trame. L'instantané reste une copie BGRA indépendante, mais celui du champ natif et non une image entrelacée assemblée de deux champs. Cette inspection établit le chemin présent dans les sources ; elle ne constitue ni une nouvelle mesure de gain, ni une validation de l'affichage dans RML. Les mesures historiques ci-dessous restent datées et ne doivent pas être présentées comme une mesure du code courant.

Pour le BBC/Master actuel, Video.Fields conserve les deux derniers champs et leurs parités ; le CRTC continue son balayage matériel. Video.Rendering réutilise les lignes inchangées après comparaison, mais renvoie encore un instantané BGRA complet par ToArray. EmulatorCore transmet les dimensions de la machine (640 × 256 PAL, 640 × 512 entrelacé), jamais la résolution de l'écran hôte. L'agrandissement appartient à la présentation RML.

En 640 × 512 et 4 octets/pixel, un tampon fait 1 310 720 octets ; à 50 envois/s, le seul volume théorique d'une copie vaut 65 536 000 octets/s. Ce calcul n'est pas une mesure du coût CPU ni une attribution de la baisse de cadence Turbo. Transmettre seulement le champ nouveau réduirait ce volume ; un gain global reste à mesurer et une fidélité visuelle supérieure exige un traitement des champs/parités/instants par la présentation, pas seulement un tampon plus petit. Aucun contrat vidéo ni rendu modifié pendant cette discussion ; rester sur le Master/Turbo en cours.

## Profil global Debug du Turbo — 7 novembre, après clôture SimCity

Le banc Native est limité au Turbo, avec les mêmes ROM, carte WD1770/Winchester activée sans média, rendu RunFrame et DrainAudio. Après démarrage, saisie de PRINT, verdict invité TUBE SOFTWARE PASS / 46 et dix images de chauffe, 1 200 images au repos prennent **29,098589 ms/image**, soit **34,366 images/s**, **1,372438 MHz hôte** et environ **68,62 %** de la cadence hôte nominale de 2 MHz. Cycles hôte : **47 923 202**. PC final E9C1 ; SHA256 vidéo `4B1A21870E413491F328906BFD6B62C687ECCD2B6BB92BEB1A45D1174BC21261`, PCM `52E28210C0BE3BE4E312D3EC209B5C49FC69ABDD4C4AED923F1CE29AD25B0350`. Le passage instrumenté se termine avec code 0 ; aucune comparaison avant/après ni gain nouveau n'est revendiqué.

Collecte par dotnet-trace, profil **dotnet-sampled-thread-time**, conversion Speedscope. Les 43 511,73 ms retenues correspondent aux piles contenant BbcMicroMachine.RunFrame ; elles comprennent aussi le démarrage et la saisie, hors de la fenêtre chronométrée de 1 200 images. Il s'agit d'une estimation du temps de thread échantillonné, pas d'un comptage d'appels ni d'une mesure isolée du CPU hôte. Aucun RML, compilation ou autre test ne tourne pendant la collecte. Les pourcentages inclusifs et propres ne doivent pas être additionnés.

| Chemin | Part inclusive de RunFrame | Part propre |
| --- | ---: | ---: |
| Vidéo complète Video.Tick | 33,16 % | 7,22 % |
| TurboBoard.RunHostCycle, incluant le CPU parasite | 30,41 % | 2,57 % |
| CPU parasite Cpu6502.Clock | 27,55 % | 1,69 % |
| SAA5050.ClockHalfDots, inclus dans la vidéo | 8,54 % | 3,91 % |
| MC6845.Tick, inclus dans la vidéo | 5,99 % | 0,99 % |
| PrepareTeletext, inclus dans la vidéo | 5,65 % | 1,72 % |
| Série complète Serial.Tick | 3,69 % | — |

Les méthodes du moteur CMOS sont partagées par les deux processeurs : leurs coûts propres agrégés incluent ExecuteCmosPhase 8,85 %, ReadCmosCycle 5,28 %, ClockCmos 4,39 % et Tick 4,13 %. StepCmos atteint 98,41 % inclusif parce qu'il avance aussi toute la carte ; ce chiffre n'attribue pas 98 % au seul calcul des instructions. Le profil ne démontre pas que les getters IRQ/NMI, le disque ou la copie BGRA expliquent la baisse. Les prochains changements doivent cibler une redondance établie dans ces chemins dominants et être comparés sur la carte complète, sans retirer une phase de CPU ou de balayage.

Incident de banc : le premier lancement a échoué avant Main, avec boîte d'erreur Windows, car le fichier deps.json restait celui de l'ancien projet d'inspection SimCity utilisant le même nom d'assembly et dossier de sortie. La DLL Acorn était présente mais non déclarée dans ce manifeste. Une reconstruction Native complète a régénéré les dépendances Acorn/Chips ; leur présence a été vérifiée avant le seul lancement réussi. Aucun défaut de chargement de RML ni de puce ne se déduit de cet incident.

## Candidate d'avance de l'oscillateur — vérification fonctionnelle

TurboBoard calcule une fois InputClockFrequency / fréquence hôte, soit huit périodes par cycle hôte ; RunHostCycle transmet cet intervalle au CPU via AdvanceInputClocks. L'entrée groupée conserve le diviseur dans le CPU, chaque accès et notification intermédiaires, et le maintien RESET Tube empêche toujours l'avance. Aucun regroupement d'instructions ni transfert Tube différé. Le CPU partagé n'a pas été modifié.

13 tests d'horloge, 30 tests Master et 20 tests Tube passent en Debug ; détails dans la fiche CPU Mos65C12. Les mesures alternées avant/après restent à faire en l'absence de session RML concurrente. La source candidate n'est pas encore retenue comme optimisation et aucun nouveau build livré ni commit effectué. La référence de TurboBoard est conservée dans le dossier temporaire jusqu'à cette décision.

### Décision après deux paires Debug alternées

RML fermé, passages référence/candidate/référence/candidate reconstruits avec les mêmes dépendances. 160 images mesurées après dix de chauffe par scénario, rendu et drainage audio inclus. Cycles, PC, SHA256 vidéo et PCM strictement identiques entre les quatre passages, par scénario.

| Scénario | Référence 1 / 2 ms/image | Candidate 1 / 2 ms/image | Moyenne référence → candidate | Surcoût |
| --- | ---: | ---: | ---: | ---: |
| Repos | 27,665595 / 28,151998 | 28,735837 / 28,536643 | 27,908796 → 28,636240 | +2,61 % |
| SOUND | 27,351973 / 27,447717 | 27,754053 / 27,750634 | 27,399845 → 27,752343 | +1,29 % |
| MODE 1 redessin | 30,180645 / 29,669288 | 30,627301 / 30,796089 | 29,924966 → 30,711695 | +2,63 % |

Candidate rejetée : l'entrée AdvanceInputClocks réintroduit ses propres contrôles de phase et une boucle de périodes ; moins d'appels publics n'a pas amélioré le chemin global. TurboBoard est restauré exactement depuis sa référence avant cette tentative ; il conserve les déclarations ControlSignals déjà validées précédemment. Les deux Clock unitaires par cycle hôte restent en place. Les nouveaux tests d'équivalence sont conservés. Aucun changement de fréquence, de comportement de puce ni de compilation Debug ; aucun build livré ou commit de cette tentative.

## Entrées dans RML Debug — 7 novembre 2026

Configuration Master utilisateur empruntée, coprocesseur temporairement sélectionné Turbo puis conservé à sa demande explicite. MOS 3.50 et client 65C102 existants, disque `F:\Retro\AMX Super Art Master.ssd` inchangé. Le démarrage affiche le coprocesseur 65C102 ; `PRINT 12+34` renvoie 46. Les touches physiques AZERTY D3 et Oem8 sans Maj produisent respectivement guillemet et exclamation. Captures réelles sous `artifacts/TestsMachines/Acorn/BBC Master Turbo/20261107-rml-turbo-input-basic.jpg` et `-symbols.jpg`.

Après `*EXEC !BOOT`, les quatre chargements SWRAM arrivent au bout ; Ctrl+BREAK, `*POINTER ON`, puis Ctrl+BREAK permettent de lire le pilote depuis BASIC parasite. Aucun accès détourné entre les RAM hôte et parasite. Le programme en RAM est :

```basic
10 A%=64:X%=&70:Y%=0:C%=-1
20 CALL &FFF1:B%=?&76 AND &E0:IF B%<>C% THEN PRINT B%:C%=B%
30 GOTO 20
```

OSWORD 64 renvoie 224 au repos, 192 pendant le bouton gauche, 160 au milieu et 96 à droite ; chaque relâchement revient à 224. Captures `20261107-rml-turbo-input-buttons-left.jpg`, `-middle.jpg`, `-right.jpg` dans le même dossier, copies sous `artifacts/TestsChips/Mos6522/BBC Master Turbo/`. Les appuis gauche et droit sont maintenus 900 ms ; le milieu est un clic de l’outil Windows. Un premier clic droit bref n’a pas été observé par ce relevé BASIC, puis l’appui maintenu a été reçu : ne pas en déduire une validation des impulsions plus courtes que l’échantillonnage.

Les compteurs RML dans ce scénario affichent environ 33–35,5 FPS, 1,31–1,42 MHz hôte, 66–71 %, pour une cadence vidéo matérielle de 50,1 Hz. Ce sont des observations limitées dans RML, pas une nouvelle mesure comparative hors interface. Le budget nominal Debug reste non atteint. Aucun code de puce modifié pour ce contrôle ; aucune nouvelle mesure Release. Programme arrêté avec Escape, helper supprimé, application empruntée laissée ouverte à BASIC, coprocesseur Turbo conservé. Ces preuves ne couvrent ni le joystick, ni les mouvements rapides en dessin, ni tous les modes du VIA.

### Joysticks dans BASIC parasite, hors RML

Le 7 novembre, un test Debug réussit (cas 57 s, série 1,0659 min). Configuration utilisateur copiée en mémoire, ROM existantes, aucun média ni son Windows. Deux associations clavier→joysticks par les contrats existants alimentent les quatre axes et les deux tirs ; la frappe conserve ces entrées. Après `*FX16,4`, BASIC parasite appelle ADVAL(1…4) et ADVAL(0) AND 3 via le Tube. Premier jeu : axes 0/haute/haute/0 et tir 1 ; second jeu inversé et tir 2 ; repos : quatre centres entre 32000 et 33500 et aucun tir. Chaque comparaison produit son verdict après CLS, donc la présence du texte dans la commande saisie ne peut pas valider le résultat à tort.

Verdicts `TURBO JOY ONE PASS`, `TURBO JOY TWO PASS`, `TURBO JOY REST PASS`. Champs vidéo natifs conservés sous `artifacts/TestsMachines/Acorn/BBC Master Turbo/20261107-turbo-joystick-<one|two|rest>-field-<0|1>.png`, copies sous `artifacts/TestsChips/Upd7002/BBC Master Turbo/`. REST inspecté visuellement. Aucun changement de puce ou de configuration sur disque. Diagnostic supprimé et assembly de tests Debug reconstruit sans dépendances : zéro erreur, trois avertissements préexistants, 12,80 s. Aucune mesure de performance pendant cette exécution concurrente de RML ; la lecture d’une manette physique dans RML reste distincte de ces associations en mémoire.

## Reprise hors RML — 8 novembre 2026, après fusion Git

Fusion autorisée terminée : `3831b3ab7`. Le code de production résolu est identique au premier parent ; le repli souris Windows supprimé ne revient pas, la capture selon les capacités et les associations à chaud sont conservées. Aucun nouveau correctif de puce n'est revendiqué.

Banc Native reconstruit depuis les projets courants en Debug, sans optimisation Release, sans RML ni compilation concurrente pendant l'exécution. MOS 3.50, SAA5050 et client Turbo externes existants ; hôte 2 MHz, parasite 4 MHz, WD1770 et interface Winchester présents sans média. Aucune configuration utilisateur modifiée. La construction réussit avec un avertissement NU1900 de consultation NuGet et zéro erreur ; le manifeste de dépendances Acorn/Chips est vérifié avant lancement. L'exécution se termine avec code 0.

Après 120 champs de démarrage, `CLS:PRINT"TURBO CURRENT PASS":PRINT 12+34` affiche réellement le verdict puis 46. La capture BMP native inspectée et son champ suivant sont conservés sous `artifacts/TestsMachines/Acorn/BBC Master Turbo/20261108-current-basic.bmp` et `20261108-current-basic-next-field.bmp`, avec copies identiques dans `artifacts/TestsChips/TubeUla/BBC Master Turbo/`. Chaque fichier contient 640 × 256 pixels BGRA, soit 655 360 octets de pixels (655 414 avec en-tête BMP). Le balayage observé passe du progressif d'initialisation aux deux parités entrelacées. Les captures sont des champs natifs hors RML ; elles ne prouvent pas le désentrelacement dans l'application.

80 champs chronométrés après dix de chauffe par scénario ; rendu RunFrame, drainage PCM et collecte des modes compris. Captures, lecture de l'écran et accès de diagnostic aux compteurs en dehors du chronométrage. Le programme de défilement est une boucle BASIC en mode courant, pas le redessin MODE 1 de l'ancienne comparaison : ne pas comparer directement leurs durées.

| Scénario | ms/champ | champs/s sans limite | MHz hôte / parasite | vitesse nominale | cycles hôte / parasite |
| --- | ---: | ---: | ---: | ---: | ---: |
| Repos | 27,367514 | 36,5397 | 1,45925 / 2,91850 | 72,9624 % | 3 194 881 / 6 389 762 |
| SOUND | 26,971506 | 37,0762 | 1,48067 / 2,96135 | 74,0337 % | 3 194 879 / 6 389 758 |
| Défilement BASIC | 27,966514 | 35,7570 | 1,42799 / 2,85599 | 71,3997 % | 3 194 881 / 6 389 762 |

Le rapport des cycles parasite/hôte vaut exactement 2 dans les trois fenêtres. Cadence matérielle déduite des cycles : environ 50,0801 champs/s ; ce chiffre ne représente pas les FPS présentés par RML. Le budget Debug de 19,97 ms/champ reste non atteint. Il n'y a pas de paire avant/après sur une modification, donc aucun gain nouveau établi. Ce passage réutilise les preuves antérieures pour les médias et entrées ; il ne prétend pas les retester.

Empreintes SHA256 (dernier champ et dernier bloc PCM de chaque fenêtre, pas tout le flux) :

| Scénario | Vidéo | PCM |
| --- | --- | --- |
| Repos | DE2DCE98D18A00985606B6C44BCF41F7F447ABA6FC3523189CB34E5B79822DED | 57CA25386B9097A240626500BA5CB92F80C6B486B0572282672BB43F60E7EEDC |
| SOUND | 83E7F2512A27E288AFBF666186C743E6DF78081D5B50CCB42C1D041C18B3E5F1 | 242D75A8A13D21EAC21481F398C6D6DB6F7E822C6B7BC2EA5AD363121A977BEA |
| Défilement | D83829A866985A061DC9D397112672BD7FA734DE7E179A385D2424588EB05828 | 40AA13E5829B26B29336E1D204B59A7FDC93526E8C2C32B97B197732C9861F96 |

### Essai de conservation des points SAA5050 — 8 novembre

Candidate fonctionnellement vérifiée (98 tests) mais retirée : référence/candidate/référence au repos 28,54/34,12/27,56 ms, SOUND 28,24/28,98/27,98 ms, défilement 28,60/30,17/27,80 ms par champ. Cycles, PC et empreintes finales vidéo/PCM concordent. Aucun gain retenu ni modification matérielle. Conditions et décision détaillées dans [SAA5050](../../../../../chips/Video/Saa5050.md).

### Appels du CPU parasite vers le Tick vide du bus — 8 novembre

Candidate fonctionnellement vérifiée : 142 tests CPU MOS passent. Référence/candidate/référence : repos 27,64/27,82/28,08 ms ; SOUND 28,44/27,46/27,73 ms ; défilement 28,04/27,81/29,66 ms par champ. Cycles, PC et empreintes finales concordent ; gain global non établi compte tenu de la dispersion. Contrat supplémentaire et code candidate retirés, aucune nouvelle optimisation conservée. Détails dans [65C12/65C102](../../../../../chips/CPU/Mos65C12.md).

## Marge Release du code conservé — 8 novembre 2026

Vérification unique utile à la décision utilisateur après les candidates Debug retirées. Native et ses dépendances sont reconstruits en Release depuis les sources conservées ; zéro erreur, avertissement NU1900 de consultation NuGet. Aucun RML, test ou compilation concurrente pendant la mesure. MOS 3.50, client 65C102 et SAA5050 externes, WD1770/Winchester sans média. 120 champs de démarrage, BASIC exécuté TUBE SOFTWARE PASS puis 46 ; trois scénarios de 160 champs après dix de chauffe, rendu et drainage PCM compris. Défilement dans le mode BASIC courant, pas MODE 1. Processus terminé avec code 0.

| Scénario | ms/champ | champs/s sans limite | MHz hôte émulés | vitesse nominale |
| --- | ---: | ---: | ---: | ---: |
| Repos | 4,523105 | 221,0871 | 8,82934 | 441,4669 % |
| SOUND | 4,737309 | 211,0903 | 8,43010 | 421,5051 % |
| Défilement BASIC | 5,251061 | 190,4377 | 7,60532 | 380,2660 % |

Cycles (6 389 763 / 6 389 760 / 6 389 760), PC (E9BC / E7A3 / C6F5) et empreintes finales vidéo/PCM identiques aux références Debug de la comparaison des appels de bus. La différence de cadence provient de la configuration de compilation normale ; aucune option Release n'est activée en Debug, aucune accélération matérielle fictive. Ces chiffres sont des champs calculés hors interface, pas une mesure des FPS présentés par RML. Ils confirment la marge Release, sans valider le budget Debug de 19,97 ms/champ. La décision de clôture du Turbo ou de priorité bloquante du budget Debug est demandée à l'utilisateur ; le Master 512 n'est pas commencé sur cette seule mesure.

### Audit des captures joystick — 8 novembre

Les six anciens champs ONE/TWO/REST sont inspectés. ONE et TWO montrent encore les lignes BASIC saisies ; la présence des mots PASS dans ce programme n'est pas une preuve de son exécution. REST montre clairement TURBO JOY REST PASS après effacement. Les anciens fichiers ONE/TWO restent des étapes et doivent être complétés par des verdicts effectivement affichés ; le résultat de test textuel historique n'est pas étendu à une preuve visuelle absente. Aucune puce défectueuse n'est déduite de ce défaut de preuve.

Premier passage du nouveau banc : axes ONE corrects (0/65520/65520/0), mais tir nul et verdict FAIL. Le banc injectait le bit brut 1 au lieu du masque normalisé A obtenu par EmulationSdk.InputProcessing.ControllerButtonIndex. Le raccordement utilise ce dernier ; correction du banc avant toute attribution à la machine. Ce passage échoué ne valide pas le scénario et n'est pas une mesure de performance.

### Verdicts joystick effectivement affichés — 8 novembre

Après correction de l'injection du banc, les trois scénarios se terminent normalement (code 0), sans modification de production. Même Turbo et ROM externes ; deux contrôleurs normalisés injectés directement dans SetInput, conservés pendant la saisie. BASIC parasite lit les quatre canaux et les deux tirs par ADVAL via le Tube. L'écran est effacé avant l'affichage des valeurs/verdicts ; 40 champs sont laissés après RUN puis les deux parités capturées. Les trois captures field-1 sont inspectées visuellement.

| Scénario | ADVAL 1 / 2 / 3 / 4 | Tirs ADVAL(0) AND 3 | Verdict exécuté |
| --- | --- | ---: | --- |
| Premier jeu | 0 / 65520 / 65520 / 0 | 1 | ONE PASS |
| Second jeu | 65520 / 0 / 0 / 65520 | 2 | TWO PASS |
| Repos | 32752 / 32752 / 32752 / 32752 | 0 | REST PASS |

Preuves natives : `artifacts/TestsMachines/Acorn/BBC Master Turbo/20261108-joystick-<1|2|0>-field-<0|1>.bmp`, copies identiques sous `artifacts/TestsChips/Upd7002/BBC Master Turbo/`. Ces nouvelles images remplacent les anciennes ONE/TWO comme preuves de verdict ; les anciennes restent des étapes. Elles prouvent le parcours entrées normalisées→ADC/VIA→Tube→BASIC, sans prétendre tester une manette physique ou l'assignation dans RML. Aucun changement de configuration sur disque et aucune sortie audio Windows. Les appuis sont relâchés et les callbacks réseau détachés dans finally. Aucun relevé de cadence tiré de cette saisie instrumentée.

## Acceptation utilisateur — 8 novembre 2026

L'utilisateur accepte de valider le bloc si la Release tient la cadence. Les mesures du code conservé (4,52–5,25 ms/champ, 190–221 champs/s, 380–441 % nominal) satisfont ce critère. Poursuite autorisée vers le Master 512, en conservant les limites Debug et la portée exacte de toutes les preuves ci-dessus. Pas de nouvelle validation physique RML pendant cette reprise, conformément à sa demande ; pas de nouvelle fonctionnalité ni de gain fictif revendiqués.

Livraison de clôture du 8 novembre : scripts/local-building.cmd --building=debug --modules=A terminé avec code 0. Exécutable, Chips dans lib, 24 manifestes et leurs DLL d'entrée vérifiés ; DLL Acorn livrée identique par SHA256 au build win-x64. Aucun RML lancé et aucune nouvelle mesure de performance pendant cette construction. Chemin : build/Debug/Retro Media Lab/retromedialab.exe.
