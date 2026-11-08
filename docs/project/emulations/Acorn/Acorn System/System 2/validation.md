# Acorn System 2 — références de reprise

## Revue statique du 8 novembre — liaisons et codeur ASCII

Le strobe PB7 est désormais aussi transmis à l'entrée STB du 8154, en plus de sa lecture de port. Le VDU reçoit HSYNC et RA du CRTC par notifications, vers GLR et CRS du SAA5050 : plus de relecture de ces sorties à chaque caractère. Initialisation de tous ces niveaux au raccordement/reset ; les fronts de balayage, données mémoire et horloges de points restent présents.

Le codeur ASCII conserve son horloge et ses dates de répétition (500 ms puis demi-périodes de 50 ms). `NextClockEventChanged` actualise la date gardée dans la carte ; celle-ci appelle `AdvanceTo` à l'échéance ou avant un changement d'entrée/reset, au lieu d'appeler le codeur à chaque cycle. Une avancée groupée visite chaque front nécessaire. REPT est résolu à la réception des touches et conserve son action au tick suivant. Le System 1 sélectionne explicitement sa matrice passive ; les System ASCII ne recalculent plus cette matrice absente. Même raccordement commun aux autres cartes System, avec leur fréquence propre.

Ces corrections sont issues de la lecture du code existant. Aucun nouveau test/build ou gain mesuré à ce stade ; les preuves et durées historiques ci-dessous ne valident pas ces modifications.

## Montage actuellement fourni par le code

`SystemFunctions.Create` assemble `AcornSystemMachine` avec `Standard6502`, 1 MHz déclaré, 1 Kio sur la carte CPU et une carte RAM de 8 Kio à 2000. La carte System 2 possède l'INS8154, le clavier ASCII, le transport AnalogTape et la VDU 40 ; le VIA absent et le FDC absent ne sont pas instanciés. Les chips MOS6502, INS8154, MC6845 et SAA5050 ont déjà leurs fiches et tests ; ce passage porte d'abord sur leur câblage de System 2. Aucun modèle matériel n'est déclaré vérifié sur ces seules lectures.

La VDU actuelle utilise `SystemVideo.cs` et `SystemVideo.Teletext.cs` : RAM 0400, CRTC 0800, douze demi-points par période caractère, alimentation de données RAM, VSYNC vers DEW, HSYNC vers GLR et RA0 inversé vers CRS. `AcornSystemMachine` lui fournit actuellement 2 MHz de périodes caractère. Le tampon est 480×250 et `RunFrame` avance par tranches CPU de 20 ms. Vérifier fréquence, géométrie, parités et cadence de publication contre le manuel et le vrai COS, sans considérer les chiffres du code comme une spécification.

## Références déjà acquises à réutiliser

- `docs/chips/Video/Saa5050.md`, section « Raccordement de la carte VDU System » : manuel Acorn 200.002, oscillateur de 6 MHz et schéma retranscrit de la carte. [Manuel VDU 40](https://www.bighole.nl/pub/mirror/homepage.ntlworld.com/kryten_droid/Acorn/system2/200_002.htm).
- `docs/tasks/machines/AcornSystem/implementation.md` : historique, chemins et firmwares. Les anciennes cases cochées ne remplacent pas les contrôles de ce passage. Le commentaire ancien sur les modes strobed INS8154 est dépassé par la fiche actuelle de la puce.
- MAME : `src/devices/bus/acorn/system/vdu40.cpp`, `src/mame/acorn/acrnsys.cpp`. Comparaison d'implémentation à confronter au manuel matériel ; conserver les pages/fichiers utilisés sous `.tmp-archives/machines/Acorn/Acorn System 2/`.

## Firmwares et configuration

Le montage enregistré de System 2 (`Configurations/678a2ea32418478186c9c6195607edb2/machine.json` sous le dossier Acorn de l'application) désigne `Acorn System COS40.bin` comme moniteur et `saa5050` comme caractères. COS40 est identifié par SHA-1 `C587DA5DCC6878DCD0BC823C508472D38296003E` ; la ROM SAA5050 du catalogue par `6C8DABA70374E5AA3A6402F24CDC5F8677D58A0F`. Recontrôler les fichiers effectivement chargés dans le banc avant de revendiquer leur utilisation.

Le champ BASIC contient actuellement `Acorn System 5 DOS80.bin`, qui n'est pas une ROM BASIC System 2. `AcornSystemModel.Get` distingue les rôles et rejette une identité connue d'un autre rôle. Le banc initial omet ce champ optionnel ; identifier le véritable BASIC compatible avant l'essai de cette option et avant de proposer à l'utilisateur un montage final. La configuration utilisateur n'a pas été modifiée pendant cette lecture.

Le banc précédent SAA5050 avait validé une mire sur une ROM synthétique de diagnostic, sous `artifacts/TestsChips/Saa5050/AcornSystem2/native-diagnostic-rom.png`. Cette preuve ne constitue pas un démarrage logiciel commercial COS/BASIC du présent passage.

## Contrôle natif du 8 novembre 2026 et schéma vérifié

Le vrai COS40 démarre, affiche `Acorn Cos` puis exécute `TEST` et répond `Com?`. Captures `20261108-system2-cos-boot-baseline-debug.bmp` et `20261108-system2-cos-keyboard-baseline-debug.bmp`, journal de même préfixe dans TestsMachines/Acorn/Acorn System 2. Ce passage est fonctionnel : aucune performance hôte n'est mesurée avec RML ouvert.

Les registres COS relevés sont `3F 28 33 44 1E 02 19 1B 03 12 72 13 04 00 04 51 00 00` : R8=3 active l'entrelacement. Le montage initial réalise 159 frontières CRTC en environ 1,6 seconde CPU, soit environ 100 champs/s. La page 1 du schéma retranscrit 200.002 (2004, archivé avec le manuel) raccorde directement PH2 à CLK du 6845 ; la page 2 raccorde PH2 inversé à F1 du SAA5050 et son oscillateur réglable 6 MHz à TR6. Sur cette carte CPU 1 MHz, CLK doit donc être 1 MHz. Le 2 MHz choisi par MAME ne constitue pas la référence matérielle de ce montage. Le manuel Applications décrit également le décodage de 0800/0801 répété sur toute la page 08 ; le code initial n'accepte que les deux premières adresses.

Le BASIC téléchargé sur la [page System BASIC de Chris Oddy](https://www.theoddys.com/acorn/acorn_system_software/system_basic/system_basic.html) fait 4096 octets, SHA-1 `6571FCA3E86E6DC99C2B56481979FBC24615BFF2`, implantation C000. La source ca65 et le manuel sont archivés à côté du binaire. C'est une reconstruction publiée avec sa source, pas une empreinte de ROM d'origine établie. Sa compatibilité reste à vérifier dans le banc avant installation.

## Résultats du bloc VDU et BASIC

`AcornSystemMachine.VideoClock` fournit maintenant PH2 aux VDU40, et conserve 2 MHz sur la carte rapide VDU80. `SystemVideo` décode toute la page 08 et mémorise séparément la parité du champ terminé ; le cœur reçoit un seul champ 480×250 et sa métadonnée pair/impair, soit une image native entrelacée de 480×500 à recomposer côté application. RunFrame termine au champ CRTC suivant et expose le temps CPU réellement exécuté ; aucun timing interne du CRTC/SAA5050 n'est changé. VSYNC arrive par VSyncChanged, supprimant sa relecture par caractère.

Sur vingt champs COS, le banc mesure 49,999875 champs/s de temps émulé (CPU 1 MHz), au lieu d'environ 100 avant correction. Les champs successifs ont 19 968 ou 20 032 caractères d'horloge, soit respectivement 19,968 et 20,032 ms : la paire fait 40 ms. La valeur instantanée FramesPerSecond décrit le dernier champ ; ce n'est pas une mesure de débit hôte. Les 16 tests System passent en Debug en 943 ms, incluant les miroirs, les deux parités, le mode progressif et les anciens contrôles de glyphes/clavier/bande. Le premier passage avait 15 réussites et un échec dans la nouvelle borne de test à 20 000 caractères : le champ long légal fait 20 032 ; seule cette attente a été corrigée. TRX initial et final conservés.

Le BASIC reconstruit exécute `PRINT 12+34` et affiche `46`, puis son invite `>`. Capture native `20261108-system2-basic-print-ph2-fields-debug.bmp`, également conservée dans TestsChips/Crtc6845 et TestsChips/Saa5050. Le catalogue l'identifie comme `Acorn System BASIC [Chris Oddy rebuild]`, compatible pour le rôle BASIC System 2 ; les trois contrôles de compatibilité passent en 178 ms. Le fichier externe a été installé sous ce nom dans Firmware, SHA-1 revérifié ; aucune configuration utilisateur modifiée.

## Bande : défaut établi à terminer

Le COS exécute `SAVE "PROBE" 2800 2808`, enregistre les huit octets `00 55 AA FF 12 34 56 78` et revient au prompt. L'enregistrement contient 589 440 échantillons à 48 kHz, soit 12,28 secondes. Après effacement des octets et relecture du signal enregistré, LOAD reste en attente : ce test échoue et la bande System 2 n'est pas validée.

La [source COS de Chris Oddy](https://www.theoddys.com/acorn/acorn_system_software/cassette_operating_system_(cos)/COS.ca65), archivée en COS.ca65, identifie CASIN sur PA7, CASOUT sur PA6 et NHITONE sur PA5 (adresse bit 0E05). LEADER attend PA5 bas pendant son contrôle de porteuse. Le callback ReadPortA actuel ne raccorde que PA7 et laisse PA5 à 1. Le schéma constructeur 100.001 issue 3, archivé et rendu avec ses détails, montre NHITONE sur Q7 d'IC6/CD4024, qui alimente aussi D de la bascule de décodage ; CASIN est la sortie complémentée de cette bascule. NHITONE n'est donc pas un simple alias inversé de CASIN. La phase/source d'horloge d'IC6 et sa remise à zéro restent à suivre avant correction ; aucune liaison supposée n'a été ajoutée. Schémas 200.001 issue 2 et 100.001 issue 3 et manuel System 1 archivés à côté du COS.

RML est resté ouvert pendant ces essais fonctionnels ; aucune comparaison de performance hôte ni livraison complète System 2 n'est revendiquée. La demande de fermeture pour la future mesure reste en attente.

## Clôture du bloc natif — carte cassette et clavier corrigés

Après fermeture de RML par l'utilisateur, la carte cassette SystemTapeInterface a été réalisée dans Chips d'après les schémas archivés. IC1 divise PH2 par 26, ses fronts Q3 cadencent IC6 ; Q7/NHITONE et CASIN sont transmis séparément. La réception échantillonne Q7 avant le reset différentié, les compteurs avancent entre leurs échéances observables. CASOUT identique n'est plus retransmis pendant le balayage du clavier hexadécimal. Le RESET CPU chaud laisse la carte continuer ; l'alimentation froide réinitialise ses compteurs. Détails et dix contrôles autonomes : `docs/chips/Controllers/SystemTape.md`.

Le port clavier avait également PB7 inversé : RDC recevait au relâchement, puis LEADER restait bloqué sur l'attente de fin d'appui. DataWithStrobe expose maintenant le même niveau actif bas que StrobeChanged. Le groupe System intégré passe : **43 tests Debug**, zéro échec/ignoré, environ une seconde, journal `system2-integrated-circuit-debug.trx`. Le transport waveform publie ses transitions et son retour bas à l'arrêt, couvert par ces tests.

Le passage natif final réussit COS, clavier TEST/Com?, vingt champs alternés à 50,000250 champs/s de temps émulé, BASIC `12+34=46`, SAVE et LOAD après effacement des huit octets en RAM. Signal SAVE : **584 638 échantillons à 48 kHz**. Les deux anciens échecs LOAD restent consignés historiquement ; ils sont résolus par ces raccordements, pas par injection de données. Captures finales `20261108-system2-circuit-final-*.bmp`, journal `20261108-system2-final-casout-debug.log`.

Sur **160 champs**, RunFrame + DrainAudio : **15,788811 ms/champ**, **63,336 champs/s** sans limite, **1,266720 MHz** effectifs / **126,672 %** du profil à 1 MHz. Cadence CRTC : **49,999984 champs/s** de temps émulé. Le passage précédent donnait 15,396809 ms ; cette variation n'est pas un gain revendiqué. Aucune optimisation Release activée en Debug. La régression System 1 réussit édition, GO, SAVE/LOAD réels et nouveau GO, avec 86 400 échantillons comme précédemment.

Le code System 2 est prêt pour l'essai dans RML. À la demande de l'utilisateur, la livraison Debug et les essais visuels seront regroupés avec System 3 (6502/6809), System 4 et System 5. La configuration enregistrée conserve son choix BASIC incorrect DOS80 ; pour l'essai, sélectionner **Acorn System BASIC [Chris Oddy rebuild].rom**, moniteur **Acorn System COS40.bin**, caractères **saa5050**. La ROM BASIC vérifiée est déjà présente dans Firmware.
Livraison groupée du 8 novembre : scripts/local-building.cmd --building=debug --modules=A terminé avec code 0. Exécutable build/Debug/Retro Media Lab/retromedialab.exe, Chips dans lib et 24 manifestes/DLL de modules vérifiés. Acorn SHA256 1B5B4CD5E25AA48EDEC7A63C2B641409223319A1FDC7F2E548320B94172E11E2, identique à la sortie win-x64. Essais utilisateur dans RML à effectuer.

## Essai RML du 8 novembre — démarrage interrompu avant exécution

Après éjection de Tape0 par l'utilisateur : démarrage COS réussi dans RML, `Acorn Cos` et invite visibles. Relevé affiché : 480×500 (présentation entrelacée), 50,0 FPS, 49,9 Hz, 1,00 MHz, 100 %. Capture `20261108-rml-cos-boot-debug.png`. BASIC démarre ensuite mais le calcul saisi échoue avec Error 174 ; le clavier principal ne suit pas correctement la ponctuation AZERTY et le pavé numérique ne répond pas. Capture `20261108-rml-keyboard-before-fix.png`.

Correction du clavier ASCII partagé : SystemKeyboard consomme maintenant HostCharacters du contrat existant et possède le fallback des chiffres, opérateurs, séparateur et Entrée du pavé numérique. Les caractères hors ASCII représentable ne sont pas tronqués. Aucun changement App ni matrice System 1. Dans le build corrigé, calcul 46 par clavier principal puis pavé, ponctuation `?,` et calcul 42 validé avec Entrée étendue du pavé ; cadence affichée proche de 50 FPS, 1 MHz et 100 %. Captures `20261108-rml-keyboard-fixed-*`.

Retour audio utilisateur : sifflement continu en REC, plus aigu pendant l'écriture réelle. AnalogTape.MonitorSample mélangeait la sortie CASOUT d'enregistrement dans le son invité. Correction : écoute limitée à PLAY ; les échantillons enregistrés, phases et bruitages mécaniques restent inchangés. Contrôle autonome des deux polarités écrites, silence du moniteur en REC/STOP puis restitution exacte en PLAY. Groupe System : 47 tests Debug réussis, rapport `system-keyboard-audio-debug.trx`. Livraison de cette correction et contrôle visuel du bouton par défaut encore à effectuer.

Livraison corrigée terminée : `scripts/local-building.cmd --building=debug --modules=A`, code 0, exécutable, Chips et 24 modules vérifiés. SHA256 Acorn livré et sortie de compilation identiques : `A578762690DAC8CCA506B0000C344C16FDEF124CAEAFB542A34A18C17A8C13C0`. Contrôle dans le dialogue réel : bouton « Créer et monter » sombre et texte lisible, capture `20261108-rml-create-button-fixed.png`, annulation sans création. Le correctif audio est couvert par les échantillons et la régression transport ; aucune nouvelle écoute humaine revendiquée.

Essai réel Tape terminé : fichier `F:/Retro/Acorn System/System 2 RML PROBE 20261108.wav` créé par REC, sauvegarde `SAVE "PROBE"`, arrêt, rembobinage, `NEW`, puis `LOAD "PROBE"`. LIST retrouve `10 PRINT 42` ; RUN affiche 42. Le programme de contrôle sans END produit ensuite Error 94 ; l'ajout `20 END` fait terminer RUN normalement. Aucun correctif matériel tiré de cette absence de END. Capture `20261108-rml-tape-load-run.png` et `20261108-rml-tape-run-ended.png`. Le COS attend une touche après « Record drive 0 » et « Play drive 0 » ; respecter cette confirmation opérateur. Cadence réelle autour de 50 FPS, 1 MHz, 100 %. Les 33 tests AnalogTape existants passent aussi en Debug après la séparation du moniteur REC (`analog-tape-monitor-debug.trx`).

Capture initiale : artifacts/TestsMachines/Acorn/Acorn System 2/20261108-rml-start-failed-missing-tape.png. Les trois firmwares de la configuration 678a2ea3 sont corrects. Le démarrage du 16:44:40 échouait dans InsertMedia : FileNotFoundException sur une ancienne image Barbarian II.uef absente. L'entrée média gardait isInserted=true malgré « Aucune cassette ». Après éjection dans RML, le COS démarre et les essais ci-dessus aboutissent. La présentation générique de cette erreur de fichier reste distincte du fonctionnement matériel vérifié.
