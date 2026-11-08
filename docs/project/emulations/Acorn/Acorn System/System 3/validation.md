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
