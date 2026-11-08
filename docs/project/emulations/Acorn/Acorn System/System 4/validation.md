# System 4 — contrôle natif du 8 novembre 2026

Carte 6502 1 MHz, VDU40 et deux lecteurs sur le 8271 existant ; réutilisation des corrections System 2/3. Firmware Acorn System 3 DOS.bin (4CB322DADCFBA9C452797D6CC2096F0C92E8792C), caractères saa5050, image F:/Retro/Acorn System/System BASIC.ssd et modèle lecteur simple face 80 pistes.

Le banc SystemFunctions.Create sans fenêtre passe le catalogue réel, LOAD BASIC avec empreinte 6571FCA3E86E6DC99C2B56481979FBC24615BFF2 à C000, exécution et PRINT 12+34=46. SAVE PROBE écrit 768 octets ; après effacement RAM, LOAD restitue les huit octets 00 55 AA FF 12 34 56 78. La copie temporaire est possédée et détruite en finally ; le média de référence n'est pas modifié.

Relevé Debug sans RML : 15,520111 ms/champ, 64,433 champs/s sans limite, 1,288651 MHz effectifs (128,865 %). Champs natifs 480×250 pairs/impairs, fréquence programmée environ 50 Hz. Aucune activation des optimisations Release en Debug.

Captures de progression et journaux : artifacts/TestsMachines/Acorn/Acorn System 4/20261108-system4-*.bmp et *.log. Ce sont des sorties du banc natif ; essai RML regroupé avec System 2/3/5 après livraison.
CAT1 vérifié sur le second port, au moins 512 octets relus et PROBE présent. Le passage complémentaire avec compilation concurrente mesurait 23,785812 ms/champ ; cette mesure chargée ne remplace pas le relevé initial sans concurrence.

## Vérification dans RML Debug

Le 8 novembre, firmware DOS System 3 et saa5050 conservés ; les deux lecteurs sont réglés sur ss80, vitesse 100 %. Montage de `F:/Retro/Acorn System/System BASIC.ssd` dans Floppy0, `CAT` correct puis `BASIC` chargé et exécuté depuis la disquette. `PRINT 12+34` donne 46 ; `PRINT 7*6` saisi au pavé numérique, avec Entrée étendue, donne 42. Éjection de Floppy0 puis montage du même fichier dans Floppy1, sans extinction : `*CAT1` affiche le catalogue et « drive 1 ». Cadence affichée pendant ces essais : environ 49,9–50,1 FPS, 49,9–50,1 Hz, 1,00 MHz et 100 %. Captures `20261108-rml-basic-keypad.png` et `20261108-rml-second-drive-catalog.png` dans le dossier TestsMachines de System 4. Aucun changement de code supplémentaire nécessaire ; les écritures SAVE/LOAD restent couvertes par le contrôle natif ci-dessus, sans réécrire l'image de référence pendant l'essai UI.
