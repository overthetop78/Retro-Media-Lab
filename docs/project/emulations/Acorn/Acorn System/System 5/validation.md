# System 5 — DRAM et contrôle natif du 8 novembre 2026

## Montage et défaut établi

6502A E=2 MHz, VIA et VDU80 existants, deux lecteurs 8271. Firmware Acorn System 5 NOS80.bin, SHA1 E588298239B5360B5D1E15D5CD9F7FE2B1693E5D ; caractères Acorn System VDU80.bin, SHA1 04C326A745BA6A78185EBC2ADEECB53A166A6AB3.

Le profil plaçait 48 Kio de DRAM à 0000–BFFF : le catalogue fonctionnait, mais LOAD BASIC à C000 ne pouvait pas conserver les octets. Le manuel System 5, section carte 400,010, §3.1, donne la carte 32 Kio à 2000–7FFF et C000–DFFF ; l'option 16 Kio, §2.2.1 et figure 5(c), occupe 8000–BFFF. Les 48 Kio de cartes sont maintenant à 2000–DFFF ; la RAM CPU 1 Kio reste à 0000–03FF, total 49 Kio, et les trous I/O ne sont pas transformés en RAM. ROM de ce montage E000–FFFF inchangée. Aucun changement de CPU/FDC pour compenser l'erreur de décodage.

Source : [Acorn System 5 Handbook](https://chrisacorns.computinghistory.org.uk/docs/Acorn/Manuals/Acorn_System5Handbook.pdf). PDF et texte archivés sous artifacts/.tmp-archives/machines/Acorn/Acorn System 5/. Les commandes CAT0/CAT1 sont définies dans DOS §2.6 ; le chargement et lancement BASIC par son nom dans §2.9.2.

## Résultats réels

Image F:/Retro/Acorn System/System BASIC.ssd, modèle simple face 80 pistes. Catalogue, LOAD BASIC (empreinte exacte 6571FCA3E86E6DC99C2B56481979FBC24615BFF2), exécution et PRINT 12+34=46 passent. SAVE PROBE écrit 768 octets ; effacement RAM puis LOAD restitue les huit octets attendus. Après déplacement de l'image vers le second lecteur, CAT1 relit au moins 512 octets et montre PROBE. Ces tests utilisent les vrais signaux FDC et le fournisseur disque du SDK ; aucune lecture invitée remplacée par une injection dans la RAM.

Carte complète Debug, 160 champs : 13,391210 ms/champ, 74,676 champs/s sans limite, 2,982255 MHz effectifs (149,113 %). Ce firmware programme un affichage progressif natif 480×250, 50,080128 Hz ; l'application reçoit ce mode, sans entrelacement imposé. Captures vérifiées visuellement : BASIC calcule 46 et catalogue du lecteur 1 avec PROBE.

Preuves natives : artifacts/TestsMachines/Acorn/Acorn System 5/20261108-system5-*.bmp et 20261108-system5-native-debug.log. Essai RML groupé après livraison Debug ; aucun essai utilisateur présenté comme déjà effectué.
Régression finale commune : 46 tests System passent en Debug (1 seconde), dont le nouveau contrôle des liens DRAM et des zones non équipées. Rapport artifacts/TestsMachines/Acorn/AcornSystem/systems-final-debug.trx.
Livraison groupée du 8 novembre : scripts/local-building.cmd --building=debug --modules=A terminé avec code 0. Exécutable build/Debug/Retro Media Lab/retromedialab.exe, Chips dans lib et 24 manifestes/DLL de modules vérifiés. Acorn SHA256 1B5B4CD5E25AA48EDEC7A63C2B641409223319A1FDC7F2E548320B94172E11E2, identique à la sortie win-x64. Essais utilisateur dans RML à effectuer.

## Vérification dans RML Debug du 8 novembre

Essai effectué par l'agent avec l'autorisation de l'utilisateur, dans la livraison corrigée du clavier et du monitor Tape (Acorn SHA256 A578762690DAC8CCA506B0000C344C16FDEF124CAEAFB542A34A18C17A8C13C0). NOS80 et VDU80 sélectionnés ; deux lecteurs ss80 à 100 %. L'image de référence `F:/Retro/Acorn System/System BASIC.ssd` est seulement lue.

CAT donne le catalogue du lecteur 0 ; BASIC charge et exécute le programme sur disque. PRINT 12+34 donne 46. Les touches physiques du pavé 7, multiplication, 6 et Entrée étendue donnent 42. Une première invocation incorrecte du helper avait saisi les caractères 13 au lieu d'envoyer Entrée ; l'Error 29 visible dans l'historique vient de cette saisie de diagnostic, corrigée avant le contrôle réussi, et ne constitue pas un défaut établi de la machine.

Après éjection du lecteur 0 et montage de la même image sur le lecteur 1, sans redémarrage, *CAT1 affiche bien le catalogue « drive 1 ». Le signal progressif natif 480×250 est présenté à 50,0–50,1 FPS, 50,1 Hz et 2,00 MHz / 100 %. Les valeurs limitées de RML ne remplacent pas la mesure libre native ci-dessus.

Captures : `artifacts/TestsMachines/Acorn/Acorn System 5/20261108-rml-basic-keypad.png` et `20261108-rml-second-drive-catalog.png`. Le contrôle des sons et des directives demandé ensuite reste un bloc distinct à terminer pour la famille.

## Contrôle commun Audio et orchestration, System 1–5

Le 8 novembre, 82 tests System/Audio réussissent en Debug, dont dix nouveaux cas autonomes sans ROM externe. Le mélangeur est contrôlé sur System 1, 2, 3/6502, 3/6809, 4 et 5 : seuls les lecteurs raccordés apparaissent, avec volume initial 10. Les commandes volume et muet sont transmises séparément aux deux mécaniques des System 3–5, sans réinitialisation de machine ni reconfiguration pendant 1 000 clocks internes. Deux tests existants du volume/muet Tape et HDD communs passent également, avec PCM invité inchangé. Ces contrôles valident le raccordement et les commandes, pas une écoute humaine de tous les profils.

Le chemin lecteur est déjà commun : `EmulatorCore.Initialize` branche les notifications d'activité Tape et crée les mécaniques avec `hostAudio: true`. `EmulatorCore.TapeSound.cs` transmet les changements d'opération ; `EmulatorCore.Disks.cs` confie les mécaniques aux images empruntées par le contrôleur. `Intel8271Drive.Synchronize` transmet moteur, accès et déplacements seulement après un changement des sorties. `MediaRuntimePreparationService.AudioAvailable` rejoint `App/Services/Audio/MediaDriveAudioController`, dont la lecture utilise le temps hôte, indépendamment du PCM et des clocks invités. Les dix fichiers FLAC contrôlés du magnétophone et du lecteur 5,25 pouces sont présents dans la livraison Debug. Le profil magnétophone réel disponible est de référence commerciale inconnue ; le profil de disquette enregistré est celui du lecteur BBC 5,25 pouces disponible, avec replis communs par opération. Ne pas les présenter comme des enregistrements spécifiques aux boîtiers System.

Vérification des directives sur le montage actuel : compteurs/phases conservés dans CPU, VIA, contrôleur Tape, 8271 et vidéo ; IRQ/NMI raccordées par notifications au CPU, échantillonnées selon ses phases ; changement de comparateur Tape notifié à l'interface, prochaine échéance conservée dans la carte ; horloges VIA/VDU avancées aux cadences câblées. Les clocks de balayage et les lectures RAM vidéo utiles restent des traitements matériels. La VDU publie le mode progressif ou la parité du champ, avec dimensions natives, sans mise à l'échelle hôte dans le cœur. Les mécaniques des lecteurs utilisent leurs événements hôtes, sans sondage sonore ajouté au tick de carte. Aucun thread par puce ni changement de fréquence ajouté lors de ce contrôle.

Aucun manque de raccordement sonore des lecteurs n'est établi ; aucun bruit fictif ajouté. Les profils de matériel ne déclarent pas de PSG System. Le sens des sons propres aux machines demandé par l'utilisateur (boîtier/ventilateur ou son électronique précis) fait l'objet d'une question avant de déterminer un éventuel matériel manquant. Ce point reste ouvert et n'est pas présenté comme réalisé. Les rapports sont conservés dans `artifacts/TestsMachines/Acorn/AcornSystem/system-audio-audit-debug.trx` et `system-audio-common-options-debug.trx`.
