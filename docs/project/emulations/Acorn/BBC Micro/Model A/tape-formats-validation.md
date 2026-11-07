# Model A — cassette enregistrée et formats de sauvegarde

## Passage du 7 novembre 2026

Objectif : vérifier sur la machine assemblée le parcours du signal enregistré vers les quatre formats proposés par `EmulationSdk.MediaProcessing.CassetteRecordingExtensions`, puis leur relecture. Aucun changement du code de production n'a été nécessaire.

Configuration : carte Model A 16 Kio, MOS `os12.rom`, BASIC `basic2.rom`, caractères `saa5050`, horloge CPU 2 MHz et vidéo PAL, aucun disque ni réseau. ROM externes existantes ; aucune configuration utilisateur écrite. Tests sans RML ni sortie audio Windows, Debug ordinaire sans optimisation Release.

Une seule carte enregistre en mémoire, via le MOS et les circuits ACIA/SERPROC, les lignes BASIC suivantes :

```basic
10 PRINT"CASSETTE FORMAT PASS"
20 PRINT 12+34
```

Transport REC activé, `SAVE"RML"`, réponse Return à l'invite du MOS. Le signal complet obtenu contient **529 354 échantillons à 48 000 Hz**. Ce signal unique est sauvegardé dans chaque conteneur par `WriteCassetteAsync`, puis relu par `ReadCassetteAsync`. Le banc utilise seulement le relais Emulation, sans dépendance directe aux types de MediaEngine. Profil RAW transmis par `EmulationCassettePcmSettings` : 48 kHz mono, PCM signé 16 bits little-endian.

Pour chaque format, une nouvelle carte démarre pendant 120 images, monte le signal relu et saisit `CHAIN"RML"`. Elle affiche `Searching`, `Loading`, le bloc `RML 00 002A`, puis **CASSETTE FORMAT PASS** et **46**. Aucun octet du programme n'est injecté dans la RAM du récepteur. Les quatre essais terminent sans échec ; ce sont des parcours matériels, pas le mode de chargement direct.

| Format | Taille du fichier (octets) | Échantillons relus | ms/image Debug | FPS calculés libres | MHz émulés | % de 2 MHz |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| WAV | 1 058 752 | 529 354 | 18,5519 | 53,90 | 2,1527 | 107,63 |
| UEF | 16 807 | 529 354 | 17,9520 | 55,70 | 2,2246 | 111,23 |
| CSW | 777 | 529 354 | 19,2408 | 51,97 | 2,0756 | 103,78 |
| RAW | 1 058 708 | 529 354 | 18,5910 | 53,79 | 2,1481 | 107,41 |

Chaque portion de lecture mesurée comprend **285 images et 11 381 762 cycles CPU**, avec rendu vidéo et drainage audio. Le chronomètre entoure uniquement ces opérations de carte ; le contrôle du texte après chaque image, l'encodage, le décodage, la saisie et les six images finales de stabilisation sont exclus. Le début de CHAIN précède la fenêtre : ces nombres ne représentent pas le temps total de chargement de la bande. Cadence vidéo matérielle inchangée, environ 50,1 Hz. Un seul passage par format : ne pas déduire un gain de performance du choix d'un conteneur, puisque les quatre exécutent le même signal et les mêmes cycles.

## Preuves conservées

Trames réellement rendues, stabilisées et inspectées, 640 × 512, sans retouche :

- `20261107-055506-tape-format-wav.bmp`
- `20261107-055516-tape-format-uef.bmp`
- `20261107-055524-tape-format-csw.bmp`
- `20261107-055533-tape-format-raw.bmp`

Dans `artifacts/TestsMachines/Acorn/BBC Micro Model A/` et `artifacts/TestsChips/BbcSerialUla/BBC Micro Model A/`, hors Git. Les fichiers de cassette étaient créés exclusivement sous `artifacts/.tmp-model-a-tape-formats/`, jamais dans `F:\Retro` ni dans les dossiers de l'utilisateur.

## Portée et limites

Ce passage complète [l'aller-retour cassette en mémoire](../../../../../chips/IO/BbcSerialUla.md) par la sauvegarde et relecture des quatre conteneurs. Il ne remplace pas les tests de CRC, de timings, de protection et d'erreurs existants. Un programme d'un bloc ne prouve pas tous les cas de bandes endommagées, multiblocs ou atypiques. Aucun rendu hors interface n'est présenté comme une capture de RML ou comme une écoute des bruitages mécaniques.
