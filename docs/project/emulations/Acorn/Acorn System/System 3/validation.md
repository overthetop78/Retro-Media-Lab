# System 3 — vérification du 8 novembre 2026

Un seul modèle, sélection 6502 ou 6809 dans CPU. VDU40 : PH2 1 MHz, champs natifs 480×250 avec parité, environ 50 champs/s émulés ; pas de redimensionnement hôte dans le cœur.

## 6502

Firmware Acorn System 3 DOS.bin (SHA1 4CB322DADCFBA9C452797D6CC2096F0C92E8792C), caractères saa5050. Image F:/Retro/Acorn System/System BASIC.ssd, lecteur simple face 80 pistes. Catalogue réel, LOAD BASIC avec SHA1 6571FCA3E86E6DC99C2B56481979FBC24615BFF2 à C000, exécution BASIC et PRINT 12+34=46. SAVE PROBE puis effacement RAM et LOAD restaurent les huit octets 00 55 AA FF 12 34 56 78 ; 768 octets écrits par le contrôleur. Le banc laisse finir le chargement avant la prochaine commande clavier.

Premier relevé au repos 15,652684 ms/champ. Passage fonctionnel complet : 17,707856 ms/champ, 56,472 champs/s sans limite, 1,129443 MHz (112,944 %). Ce dernier passage a chevauché une compilation des tests : ne pas présenter la variation comme un changement de performance.

## 6809

Firmware Acorn System 6809 Monitor40.bin (SHA1 B14A884BF82A7A8C23BC03C2E112728DD1A74896), caractères saa5050. R + Entrée affiche les registres ; M2000 permet d'inscrire un programme, G2000 l'exécute et écrit 2010=5A. D charge un bootstrap de test conforme au descripteur original FF42, lit deux secteurs (512 octets), puis exécute ce programme. Ce programme de test est propre au 6809 ; le BASIC 6502 ne lui est pas présenté comme compatible.

Corrections : CB1 initial haut pour recevoir le premier appui, CASIN sur PB7, CASOUT sur la vraie sortie CB2 avec sa propriété et son tirage au repos ; IRQ/NMI par notifications. Correction de CWAI décrite dans [la fiche MC6809](../../../../../chips/CPU/Mc6809.md). Résultat carte complète Debug : 14,063276 ms/champ, 71,107 champs/s, 1,422144 MHz (142,214 %), sans changer la fréquence matérielle de 1 MHz.

24 tests de raccordement System passent, 406 ms ; 61 tests CPU passent. Captures natives et journaux : artifacts/TestsMachines/Acorn/Acorn System 3/20261108-system3-*.bmp et *.log. Elles proviennent du banc sans fenêtre, pas de RML. Essai RML groupé avec System 2/4/5 à effectuer après livraison.

Sources archivées : artifacts/.tmp-archives/machines/Acorn/Acorn System 3/Monitor6809.asm et dos.html ; [moniteur original](https://www.theoddys.com/acorn/acorn_system_software/6809_monitor/Monitor.asm). Le profil bas du moniteur original n'est pas présenté comme un montage FLEX à I/O relocalisées.

## Essai RML Debug — carte 6502

Le 8 novembre, montage configuré dans RML : MOS 6502 à 1 MHz, DOS System 3 et saa5050 ; Floppy0 changé de ss40 à ss80 pour l'image `F:/Retro/Acorn System/System BASIC.ssd` (80 pistes, simple face, 200 Kio). `CAT` lit le catalogue réel ; `LOAD BASIC` puis `BASIC` chargent et exécutent le langage depuis le lecteur. `PRINT 12+34` retourne 46, puis le même calcul saisi avec les chiffres/opérateur du pavé et son Entrée étendue retourne encore 46. La ponctuation hôte est transmise au BASIC. Cadence affichée : environ 50 FPS, 49,9–50,1 Hz, 1,00 MHz et 100 %, présentation entrelacée 480×500. Captures `20261108-rml-6502-catalog.png` et `20261108-rml-6502-basic-keyboard.png` ; elles complètent les transferts et mesures natifs déjà consignés. Carte 6809 dans RML encore à vérifier.

## Essai RML Debug — carte 6809

Après arrêt électrique du 6502, sélection Motorola MC6809 et ROM `Acorn System 6809 Monitor40.bin` dans les paramètres existants, caractères saa5050 conservés. Le moniteur démarre ; `R` affiche les registres. `M2000 86 5A B7 20 10 39;` saisit LDA #$5A / STA $2010 / RTS ; `G2000` exécute et retourne au moniteur ; `M2010` affiche bien `2010 5A`. Cadence affichée : environ 50 FPS, 49,9–50,1 Hz, 1,00 MHz, 100 %. Capture `20261108-rml-6809-memory-execute.png`. Le bootstrap 6809 reste prouvé par le banc natif décrit plus haut, pas par le disque BASIC 6502 resté monté. Aucun nouveau changement des puces nécessaire pendant ces essais. Les deux cartes sont maintenant vérifiées dans RML en complément des tests de circuits et des protocoles natifs.
