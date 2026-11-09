# System 4 — contrôle natif du 8 novembre 2026

Carte 6502 1 MHz, VDU40 et deux lecteurs sur le 8271 existant ; réutilisation des corrections System 2/3. Firmware Acorn System 3 DOS.bin (4CB322DADCFBA9C452797D6CC2096F0C92E8792C), caractères saa5050, image F:/Retro/Acorn System/System BASIC.ssd et modèle lecteur simple face 80 pistes.

Le banc SystemFunctions.Create sans fenêtre passe le catalogue réel, LOAD BASIC avec empreinte 6571FCA3E86E6DC99C2B56481979FBC24615BFF2 à C000, exécution et PRINT 12+34=46. SAVE PROBE écrit 768 octets ; après effacement RAM, LOAD restitue les huit octets 00 55 AA FF 12 34 56 78. La copie temporaire est possédée et détruite en finally ; le média de référence n'est pas modifié.

Relevé Debug sans RML : 15,520111 ms/champ, 64,433 champs/s sans limite, 1,288651 MHz effectifs (128,865 %). Champs natifs 480×250 pairs/impairs, fréquence programmée environ 50 Hz. Aucune activation des optimisations Release en Debug.

Captures de progression et journaux : artifacts/TestsMachines/Acorn/Acorn System 4/20261108-system4-*.bmp et *.log. Ce sont des sorties du banc natif ; essai RML regroupé avec System 2/3/5 après livraison.
CAT1 vérifié sur le second port, au moins 512 octets relus et PROBE présent. Le passage complémentaire avec compilation concurrente mesurait 23,785812 ms/champ ; cette mesure chargée ne remplace pas le relevé initial sans concurrence.

## Vérification dans RML Debug

Le 8 novembre, firmware DOS System 3 et saa5050 conservés ; les deux lecteurs sont réglés sur ss80, vitesse 100 %. Montage de `F:/Retro/Acorn System/System BASIC.ssd` dans Floppy0, `CAT` correct puis `BASIC` chargé et exécuté depuis la disquette. `PRINT 12+34` donne 46 ; `PRINT 7*6` saisi au pavé numérique, avec Entrée étendue, donne 42. Éjection de Floppy0 puis montage du même fichier dans Floppy1, sans extinction : `*CAT1` affiche le catalogue et « drive 1 ». Cadence affichée pendant ces essais : environ 49,9–50,1 FPS, 49,9–50,1 Hz, 1,00 MHz et 100 %. Captures `20261108-rml-basic-keypad.png` et `20261108-rml-second-drive-catalog.png` dans le dossier TestsMachines de System 4. Aucun changement de code supplémentaire nécessaire ; les écritures SAVE/LOAD restent couvertes par le contrôle natif ci-dessus, sans réécrire l'image de référence pendant l'essai UI.


## Revue et contrôle actuels — 9 novembre 2026

Fichiers relus : SystemModelCatalog, SystemFunctions/Create/RamCards/Storage/Firmware et le raccordement commun AcornSystemMachine/Bus/Media. Le profil System 4 conserve le Standard6502 1 MHz, l'INS8154, la VDU40 CRTC/SAA5050 et le 8271 mini 2 MHz ; les deux lecteurs sont connectés par défaut, contrairement au seul premier lecteur du profil System 3. Les horloges et composants sont ceux vérifiés sur System 3/6502, sans nouvelle variante de CPU ni VIA ajouté. RAM à 2000 et C000 déterminée par la fabrique existante, ROM DOS System 3 déjà identifiée, caractères saa5050. NMI 8271 et niveaux vidéo/clavier suivent les raccordements notifiés communs. Aucun défaut supplémentaire établi ni modification de production pendant cette revue.

Diagnostic actuel construit par **SystemFunctions.Create** avec MachineModel.AcornSystem4. L'image BASIC existante est ouverte en lecture seule par EmulationSdk.MediaProcessing : catalogue sur port 0, LOAD BASIC, vérification des 4096 octets à C000 (SHA1 `6571FCA3E86E6DC99C2B56481979FBC24615BFF2`), lancement BASIC et PRINT 12+34=46. Compteur port 0 après CAT/LOAD : **4608 octets**. Le média est ensuite détaché du port 0 puis attaché au port 1 sans reset ; la commande invitée *CAT1 lit **512 octets** sur ce port et affiche drive 1 avec BASIC/FP/ONLINE. Le média de référence n'est pas écrit. Les écritures du contrôleur restent couvertes par les 87 contrôles de raccordement actuels consignés dans la fiche 8271 ; ce diagnostic invité ne les présente pas comme de nouvelles écritures de fichier.

### Cadence du code actuel

Après le catalogue du second lecteur, quarante champs de chauffe puis trois fenêtres de 160 champs, RunFrame et DrainAudio inclus. Debug normal, hors RML, sans compilation ni autre test concurrent pendant les fenêtres. Compteur et contrôles hors chronométrage.

| Fenêtre | ms/champ | champs/s sans limite | Cycles | MHz effectifs | Vitesse réelle | Cadence matérielle Hz |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 10,562988 | 94,670181 | 3 200 001 | 1,893404 | 189,340422 % | 49,999984 |
| 2 | 10,471156 | 95,500443 | 3 199 998 | 1,910008 | 191,000766 % | 50,000031 |
| 3 | 11,107172 | 90,031919 | 3 200 001 | 1,800639 | 180,063895 % | 49,999984 |

Ces relevés sont postérieurs aux modifications communes des System ; aucun chiffre historique ne les remplace. Scénario au prompt après lecture, sans prétendre mesurer un transfert continu ni un gain comparatif particulier. Aucun FPS présenté par l'application déduit de ces valeurs.

Capture native actuelle 480×250 inspectée : `artifacts/TestsMachines/Acorn/Acorn System 4/20261109-system4-current.bmp`. Elle montre le chargement, PRINT 12+34=46 et le catalogue drive 1. Copie identique `artifacts/TestsChips/Intel8271/Acorn System 4/20261109-current.bmp`, même exécution et non un second essai indépendant ; aucun assemblage des champs ni retouche. Le diagnostic final passe. Une première compilation avait une ambiguïté EmulatorConstants avec les imports Atari du projet de tests ; alias Acorn explicite ajouté au diagnostic, sans changement de production. Dans finally : entrées relâchées, deux ports détachés, bande éjectée, audio drainé, callbacks externes supprimés, puis image possédée libérée. Flux BMP fermé par using. Source temporaire supprimée et assembly régénérée sans le diagnostic externe ; preuves finales seules conservées hors Git. Aucun build de livraison ni ouverture de RML pendant ce passage.


### Mesure du code actuel après les corrections communes — 9 novembre 2026

Code de production au commit 2480c530d, incluant les corrections PH0 du 6502, les champs canoniques des compteurs et le câblage physique System 5. Mesure hors RML, compilation Debug normale, aucune compilation ni autre test concurrent. Scénario : vrai moniteur ROM du profil, sans média monté ni commande saisie ; 40 appels RunFrame de chauffe puis trois fenêtres de 80 appels, rendu vidéo et DrainAudio inclus. Ces résultats remplacent les anciennes valeurs pour décrire ce scénario sur le code actuel ; ils ne constituent pas une comparaison de performance avec les anciens essais exécutant BASIC ou un programme invité.

| Carte | ms par présentation, fenêtres 1 / 2 / 3 | Vitesse matérielle en %, fenêtres 1 / 2 / 3 |
| --- | --- | --- |
| 6502 | 10,932148 / 11,405983 / 11,215386 | 182,946786 / 175,346578 / 178,326560 |

Pourcentage = temps de référence réellement écoulé dans la carte / durée hôte × 100, sans limiteur. Fréquence nominale 1 MHz ; les compteurs CPU avancent du même nombre de cycles que la référence dans chaque fenêtre. Environ 1 600 000 cycles par fenêtre (écart de fin d'instruction de zéro ou un cycle pour le 6502). Le compteur System 1 présente son afficheur, les autres cartes leur vidéo ; un appel de présentation n'est pas assimilé à un front d'horloge de puce. Pas de mesure Release dans ce passage. Ce contrôle de débit ne remplace pas les validations fonctionnelles déjà consignées.

Diagnostic temporaire SystemCurrentPerformanceProbe réussi ; entrées relâchées, disques détachés, bande éjectée et audio drainé dans finally. La source temporaire est retirée après consignation, puis l'assembly de tests est régénérée. Aucun fichier média, fenêtre ni processus applicatif créé.
