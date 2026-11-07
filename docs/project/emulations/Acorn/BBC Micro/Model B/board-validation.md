# Model B — carte assemblée, 7 novembre 2026

## Périmètre et conditions

Réutilisation des circuits communs du Model A ; aucun changement de production pendant ce passage. Carte `BbcMicroMachine`, `expandedIo: true`, RAM 32 Kio, MOS 1.20 (`os12.rom`), BASIC II (`basic2.rom`) et caractères SAA5050 externes. Aucun lecteur ni réseau raccordé. Les différences propres au B vérifiées ici sont le VIA utilisateur et les quatre entrées analogiques du µPD7002.

Le banc temporaire charge les fichiers firmware en lecture seule et injecte les touches par le chemin d'entrée de la carte. Il ne modifie aucune configuration utilisateur. Les entrées et rappels sont détachés dans finally. Aucun changement de cadence, option Release en Debug, fenêtre RML, compilation ou autre test concurrent pendant les mesures.

## Résultats invités

Le MOS reçoit `*FX16,4` pour convertir les quatre canaux. Deux contrôleurs fournissent des axes opposés, puis inversés. Le programme BASIC vérifie les quatre valeurs : chaque extrémité haute doit atteindre au moins 65000 et chaque extrémité basse zéro. Il exécute réellement `ADC PASS`, puis `ADC SWAP PASS`. Cela vérifie les extrémités numériques et le raccordement des quatre canaux ; aucune nouvelle précision analogique ni résolution des limites documentaires du convertisseur n'est revendiquée.

Le programme arme ensuite T1 du VIA utilisateur à &FE60, désactive sa source IRQ, constate le bit 6 d'IFR, lit le compteur bas puis vérifie son acquittement : `VIA FLAG PASS` et `VIA ACK PASS`. L'IRQ et son instant sont couverts séparément par les tests autonomes existants ; la preuve BASIC ne prétend pas chronométrer ce front.

Le premier banc échouait à retrouver le second verdict VIA après défilement : sa lecture fixe de 1000 octets à &7C00 ne suivait pas la nouvelle adresse de début d'écran. Un `CLS` avant la séquence VIA évite ce défilement dans le diagnostic ; la séquence a ensuite réussi sans modifier le VIA ni le MOS. Les premières captures ADC restent des étapes conservées.

Les dix tests existants `AcornEmuModelBTests` réussissent en Debug : zéro échec, zéro ignoré, 345 ms. Ils couvrent aussi l'enregistrement du modèle, les choix multiples de cœur, la RAM, l'IRQ/acquittement du VIA et les canaux ADC. Aucun nouveau test permanent ni changement de puce n'était nécessaire.

## Carte complète en Debug, sans limite hôte

Chaque scénario comporte dix images de chauffe puis 120 images chronométrées, avec rendu et drainage du PCM inclus. Les quatre conversions ADC restent activées. Les six images de stabilisation des captures et la saisie des commandes sont hors mesure. Un seul passage : ces chiffres ne constituent pas une comparaison avant/après.

| Scénario | ms/image hôte | Images émulées/s | MHz émulés | Vitesse réelle | Cycles | Échantillons PCM non nuls |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Repos, ADC quatre canaux | 18,287654 | 54,6817 | 2,183770 | 109,19 % | 4 792 323 | 0 |
| `SOUND 1,-15,80,255` | 17,918353 | 55,8087 | 2,228775 | 111,44 % | 4 792 318 | 105 671 |
| MODE 1, CLS alternés en boucle | 20,499876 | 48,7808 | 1,948109 | 97,41 % | 4 792 320 | 105 671 |

La cadence matérielle programmée reste d'environ 50,08 Hz (39 936 cycles à 2 MHz par image). Les FPS ci-dessus sont la capacité de calcul de la carte sans limite, pas les FPS présentés par RML. Le son lancé au scénario précédent continue pendant le redessin. Le redessin dépasse encore les 19,97 ms visées ; ce passage ne prouve donc pas ce budget dans tous les scénarios.

## Captures et limites

Captures du vrai framebuffer inspectées, sous `artifacts/TestsMachines/Acorn/BBC Micro Model B/` et copiées dans le dossier de la puce concernée sous `artifacts/TestsChips/<Chip>/BBC Micro Model B/` :

- `20261107-092010-model-b-adc-first.bmp` et `20261107-092020-model-b-adc-swapped.bmp` — Upd7002, résultats BASIC des quatre canaux.
- `20261107-092029-model-b-user-via.bmp` — Mos6522, flag T1 puis acquittement.
- `20261107-092032-model-b-idle-adc4.bmp` — BbcVideoUla, écran de repos après les verdicts.
- `20261107-092036-model-b-sound-adc4.bmp` — Sn76489, commande SOUND exécutée ; le nombre PCM mesuré complète la capture, qui ne prouve pas à elle seule le son.
- `20261107-092042-model-b-redraw-adc4.bmp` — BbcVideoUla, phase noire de la boucle d'effacement ; ne pas la présenter comme une image de jeu ni une preuve du contenu de toutes les trames.

Les budgets WD/HDD actifs déjà ouverts restent communs à cette carte : ce contrôle sans lecteur ne les résout pas et ne suffit pas à déclarer tout le Model B terminé. Les logiciels sur médias, options et essais RML déjà consignés restent des preuves distinctes. Aucune synthèse vocale non instanciée n'est annoncée couverte. Sauvegardes et chargements d'état laissés de côté.

## Boulder Dash via DFS et 8271 — 7 novembre 2026

MOS 1.20, BASIC II, DFS 0.9H, générateur SAA5050 et RAM 32 Kio. La ROM DFS de 8 Kio est passée au constructeur de banque par `BbcMicroFirmwareFunctions.GetRomBytes`, comme dans le chargement normal ; le premier essai du diagnostic lui fournissait directement les 8 Kio et était refusé avant création de la carte. Aucun fichier de production modifié.

SSD `F:\Retro\ef_tosec_backup_2024\Acorn BBC - Games - [SSD] (TOSEC-v2024-01-15)\Boulder Dash (1988)(Tynesoft)\Boulder Dash (1988)(Tynesoft).ssd`, ouvert par `EmulationSdk.MediaProcessing.OpenDiskAsync(writable:false)`. Le CPU reçoit `*DISC` puis `*EXEC !BOOT` via le clavier, sans injection du programme en RAM. Après 800 images de chargement, le compteur physique de lecture donne 28672 octets. La capture montre l'introduction. Un premier appui de deux images sur Espace n'avait pas permis l'entrée en partie : les trois captures `20261107-092720`, `092722` et `092727` sont cette étape, pas une preuve de jeu actif ; ses 20,836273 ms/image ne sont pas une mesure en partie.

Le passage suivant maintient Espace 30 images, laisse avancer 180 images, réitère cet appui puis attend de nouveau 180 images. Les captures inspectées montrent le niveau et le compteur de temps passant de **149 à 146**. La fenêtre chronométrée couvre 240 images, rendu et drainage PCM inclus, sans compilation ni tests concurrents :

| ms/image hôte | Images émulées/s | MHz émulés | Vitesse réelle | Cycles | PCM non nul | Octets disque lus dans la fenêtre |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 19,540560 | 51,1756 | 2,043749 | 102,19 % | 9 584 640 | 0 | 0 |

La cadence matérielle reste environ 50,08 Hz. Cette phase du jeu est silencieuse ; ne pas en déduire une panne audio ni un test d'écoute. L'empreinte de l'image avant/après la fenêtre diffère, en accord avec la progression du compteur. Il s'agit d'une capacité hors limite et hors RML, pas de FPS présentés. Ce passage ne prouve ni tous les niveaux ni le budget pendant le chargement disque actif.

Captures sous `artifacts/TestsMachines/Acorn/BBC Micro Model B/` et `artifacts/TestsChips/Intel8271/BBC Micro Model B/` : `20261107-092828-model-b-boulder-title.bmp`, `20261107-092836-model-b-boulder-start.bmp`, `20261107-092841-model-b-boulder-after-240-frames.bmp`. Les deux dernières ont été inspectées et montrent la partie et son évolution. Toutes restent hors Git.

Le média n'a aucune modification en attente ; SHA-256 source identique avant/après : `C62420CA3D101854685658AD72EF0E84D98B88FE5F728AFC5F5CB553547BF7C5`. L'image est détachée puis libérée dans finally, avec nettoyage des entrées et rappels de la carte. Aucun lecteur utilisateur, configuration, volume Windows ou processus RML manipulé.

## Essai physique AMX dans RML — 7 novembre 2026

L’utilisateur a repris le Model B dans la livraison Debug, avec BooBip ×3 et ROM AMX 2.1 en IC58, sans extension Shadow. Le premier allumage était bloqué avant exécution par une ancienne disquette Barbarian II pointant vers un fichier absent ; le journal identifie précisément FileNotFoundException, pas un défaut du CPU. Après remplacement par `F:/Retro/AMX Art 2.1.ssd`, sélection `*DISC` puis `*EXEC !BOOT`, l’utilisateur confirme le fonctionnement du logiciel et les mêmes saccades souris à vitesse rapide que sur le Model A.

Ce résultat est antérieur à la correction du pointeur d’interface natif GameInput et au retrait du repli Windows dans `4ab7f04ba`. Il prouve la reproduction du défaut sur B, pas sa résolution dans le nouveau build. Les axes/boutons du joystick physique ne sont pas encore validés ici. Aucune nouvelle durée de carte complète n’a été mesurée pendant cet essai utilisateur.
