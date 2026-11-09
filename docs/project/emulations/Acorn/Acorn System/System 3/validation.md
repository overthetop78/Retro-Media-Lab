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


## Revue actuelle — System 3 en 6502, 9 novembre 2026

Les composants communs CPU/8154/CRTC/SAA5050/Tape et le circuit de répétition reprennent les corrections vérifiées sur System 2. Le 8271 supplémentaire conserve une référence d'entrée 1 MHz et son horloge mini 2 MHz ; phases, délais, buffers et échéances restent dans la puce. Le lecteur relaie les transitions physiques, synchronise avant accès et reçoit les prochaines échéances par notification. La carte présente NMI au CPU au raccordement, puis transmet ses changements directement. Aucun défaut nouveau établi dans les fichiers Clock/Bus/DriveSignals et leur câblage ; aucune modification du matériel ou de son fonctionnement pour cet essai.

Résultats des groupes existants : **155 tests Chips 8271 Debug hors sauvegardes d'état**, zéro échec/ignoré, 611 ms ; **87 tests de raccordement Intel8271 Debug**, zéro échec/ignoré, environ 4 s. Leurs durées ne constituent pas un benchmark de débit.

### Essai invité actuel hors RML

ROM `Acorn System 3 DOS.bin`, caractères `saa5050`, carte CPU Standard6502 1 MHz, cartes RAM déterminées par `SystemFunctions.RamCards` du profil System 3, image `F:/Retro/Acorn System/System BASIC.ssd` ouverte **en lecture seule par EmulationSdk.MediaProcessing.OpenDiskAsync**. Le diagnostic saisit CAT, LOAD BASIC, BASIC et PRINT 12+34 dans le contrat clavier existant ; aucun octet du logiciel invité n'est injecté en RAM. Après LOAD, les 4096 octets à C000 ont bien SHA1 `6571FCA3E86E6DC99C2B56481979FBC24615BFF2` ; compteur lecteur 4608 octets lus à ce stade. L'écran RAM contient ensuite `>PRINT 12+34` et `46>` : calcul effectivement exécuté et retour à l'invite. Le diagnostic ne remplace pas une nouvelle capture graphique ; les captures datées du 8 novembre restent distinctes.

Premier essai non compilé : le diagnostic employait le nom Enter au lieu du membre Return du contrat. Premier passage exécuté : attente de 30 champs insuffisante après BASIC, qui recharge le fichier ; les trois premières lettres de PRINT étaient perdues. Attente corrigée à 150 champs. Le passage suivant montre déjà 46, mais l'assertion attendait une nouvelle ligne avant l'invite ; elle est corrigée pour reconnaître `46>` réellement produit. Ces corrections concernent uniquement le diagnostic, sans changement de production pour masquer un échec.

### Cadence Debug actuelle

Après exécution du calcul, quarante champs de chauffe, puis trois fenêtres de 160 champs. RunFrame et DrainAudio inclus, lecture du compteur hors chronométrage ; aucune compilation ni autre test concurrents pendant les fenêtres, RML fermé.

| Fenêtre | ms/champ | champs/s sans limite | Cycles | MHz effectifs | Vitesse réelle | Cadence matérielle Hz |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 10,566098 | 94,642322 | 3 200 001 | 1,892847 | 189,284703 % | 49,999984 |
| 2 | 10,932135 | 91,473440 | 3 199 998 | 1,829468 | 182,946767 % | 50,000031 |
| 3 | 10,904064 | 91,708928 | 3 200 001 | 1,834179 | 183,417914 % | 49,999984 |

Cette mesure confirme la marge actuelle au prompt BASIC après chargement ; elle ne mesure pas un transfert disquette actif pendant toute la fenêtre et ne remplace pas les essais d'écriture historiques. Aucun gain avant/après attribué à une correction particulière. Aucun FPS présenté par une fenêtre déduit du banc.

Passage final du diagnostic réussi, zéro échec/ignoré. Dans finally : entrées relâchées, média détaché, bande éjectée, audio drainé, callbacks externes supprimés, puis image possédée libérée même en cas d'erreur. Aucun fichier ni processus ni fenêtre créé par le diagnostic ; source temporaire supprimée après consignation et assembly régénérée sans le test externe. La carte 6809 reste la prochaine revue ; ces résultats ne clôturent pas les deux variantes du System 3 ensemble.
