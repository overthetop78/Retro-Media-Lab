# AcornEmu — famille BBC Master

## Revue rapide demandée le 9 novembre 2026

Revue statique des fichiers courants, composant par composant puis raccordements. Les constats portent sur le code consulté et ne remplacent pas les validations fonctionnelles historiques. Aucun nouveau build de livraison demandé. Le Compact reste hors de cette reprise selon la consigne précédente.

- [x] Revoir les composants du Master 128 puis son câblage.
  - [x] Modifier ce suivi : consigner la revue du 65C12 et de son moteur 6502 partagé, horloges et entrées d'interruption.
  - [x] Modifier ce suivi : consigner successivement la revue VIA6522, ACIA6850, Serial ULA, ADC7002 et HD146818.
  - [x] Modifier ce suivi : consigner successivement la revue CRTC6845, Video ULA, SAA5050 et SN76489.
  - [x] Modifier ce suivi : consigner successivement la revue WD1770, MC6854 et contrôleurs Winchester optionnels.
  - [x] Corriger l'horloge WD pendant le reset de carte.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/Common/Services/Disks.cs` : fournir les clocks au WD même lorsque /MR est maintenu, sans modifier le silicium.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuMasterTests.cs` : vérifier l'avancement 8 MHz pendant /MR, l'absence d'activité et la reprise après libération ; exécuter les tests Master existants avec ce cas.
  - [x] Modifier ce suivi : consigner la vérification des fichiers de carte Master et BBC partagés, des entrées, de la vidéo, des sons et des médias.
- [ ] Revoir les cartes coprocesseur Master déjà traitées.
  - [x] Corriger le reset Turbo par l'entrée existante du CPU.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMaster/Tube/Services/TurboBoard.cs` : raccorder la notification RESET à `SetReset` et laisser le CPU gérer son maintien et la séquence de libération, sans geler son horloge dans la carte.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuMasterTests.cs` : vérifier par une écriture Tube que le CPU reste au reset puis effectue sa séquence réelle avant reprise ; exécuter le groupe Master.
  - [x] Modifier ce suivi : consigner la revue Tube ULA, 65C102 et câblage Turbo.
  - [ ] Modifier ce suivi : consigner la revue 80186 et câblage Master 512.

Toute correction nécessaire sera ajoutée ici avec ses fichiers avant modification, immédiatement après la dernière action achevée, et réalisée avant de reprendre le composant suivant.

Tube ULA (`TubeUla.cs`, `.Bus.cs`) et 65C102 (`Cpu65C102.cs`, moteur CMOS partagé) relus : FIFOs bornées par les capacités matérielles, transactions et IRQ/NMI/DMA/reset notifiés à mutation ; diviseur et phases du CPU locaux. Correction Turbo : la carte raccorde désormais RESET à `SetReset` au lieu de seulement geler l'horloge. Le test de libération contrôle la séquence CPU avant la première écriture Tube. **33 tests Master Debug réussis, zéro échec ni ignoré, 750 ms.** Le 80186 et la carte 512 restent en revue ; aucun verdict de clôture globale de la famille n'est déduit de ce groupe.

### Constats de la revue des composants

- 65C12 (`Cpu65C12.cs`, moteur partagé `Cpu6502.Clock.cs`, `.Pins.cs`, `.Interrupts.cs`, `.cs`) : variante GTE explicite, phases conservées, IRQ/NMI reçues et échantillonnées localement ; pas de consultation du bus d'interruption lorsqu'une entrée explicite est raccordée.
- VIA6522 (`.Clock.cs`, `.Ports.cs`, `.Interrupts.cs`) : fronts PHI2 distincts, compteurs et décalage internes, PB6 mémorisé et IRQ notifiée à mutation. Les fournisseurs de ports servent aux transactions et captures matérielles, pas au sondage PB6 explicitement raccordé.
- MC6850 (`.cs`, `.Outputs.cs`) et Serial ULA (`.Clock.cs`, `.Outputs.cs`) : phases TX/RX et diviseurs locaux, entrées retenues, notifications des lignes et des clocks ; intervalles différés vidés avant changement d'entrée.
- ADC7002 (`.Clock.cs`, `.Conversion.cs`) : phases et échéance internes, notification de prochaine échéance. Les limites de caractérisation analogique déjà consignées restent identifiées ; aucune valeur nouvelle inventée par cette lecture.
- HD146818 (`.Clock.cs`, `.Bus.cs`) : oscillateur converti avec résidu entier, calendrier et sorties périodiques internes, bus à strobes et échéance notifiée.
- CRTC6845 (`.cs`, `.Timing.cs`, `.Outputs.cs`) : compteurs et historiques DE/curseur internes, sorties synchro/RA notifiées ; aucune transition intermédiaire du calcul RA publiée. Video ULA (`.Clock.cs`) : diviseur maître et décalage conservés. SAA5050 (`.Signals.cs`) : F1/TR6, fenêtres et compteurs locaux. SN76489 (`.cs`) : diviseur /16, oscillateurs maintenus, bus et sortie audio notifiée.
- WD1770 (`.cs`, `.Clock.cs`, `.Outputs.cs`) : CLK indépendant de /MR dans le composant, entrées conservées, sorties mutées et horodatées. Défaut trouvé dans `Disks.Tick` : la carte supprimait les clocks pendant /MR ; correction ci-dessus.
- MC6854 (`.Clock.cs`, `.Outputs.cs`) : phases série et repos stable locaux, RTS/activité notifiés. Adaptateur SCSI (`ScsiHostAdapter.cs`) : logique asynchrone à strobes, sans oscillateur inventé, sorties notifiées. ACB4000 (`Acb4000.cs`) : référence locale et causes des changements du tampon/bus raccordées aux phases de synchronisation existantes. Cette revue rapide ne refait pas la caractérisation complète des sous-circuits ACB déjà documentée.

### Carte Master 128 et correction WD

`BbcMicroMachine.cs`, `.Via.cs`, `.Interrupts.cs`, `BbcMicroMachine.Master.cs`, `MasterClock.cs`, `MasterMemory.cs`, `Serial.cs`, `Audio.cs`, `Video.cs`, `.Crtc.cs`, `.Fields.cs`, `Disks.cs` et `Econet.cs` relus. Les IRQ/NMI sont raccordées une fois et agrégées à mutation ; les phases VIA restent de part et d'autre des transactions ; ADC et RTC sont synchronisés avant accès et à leurs échéances. Le bus mémoire reste partagé selon ACCCON, sans passage des signaux matériels par App. Le prélèvement des sorties vidéo après un front caractère correspond à l'échantillonnage du bus vidéo, pas à une attente en boucle d'une réponse. Les champs vidéo sont transmis séparément avec leur parité ; la présentation n'agrandit pas le tampon à la taille hôte. Le PSG fournit le son de la machine ; les parcours `EmulatorCore.FloppySound.cs`, `.HardDiskSound.cs`, `.Disks.cs` et `.TapeSound.cs` raccordent les mécanismes existants, sans nouveau parcours ajouté dans App.

Le WD reçoit désormais les clocks également pendant /MR. Le chemin existant `Wd1770Drive.Tick` moteur arrêté appelle `Wd1770.Advance`, puis `AdvanceIdle` : un ajout arithmétique des demi-clocks, pas une boucle de silicium ni un sondage d'autres composants. Les optimisations documentées de lecture et rotation restent intactes. La condition antérieure figure déjà dans l'historique antérieur à `422205110` ; aucune justification de conservation du temps pendant son blocage n'a été trouvée dans les fiches d'optimisation WD. Le nouveau test contrôle 80 demi-clocks pour dix cycles hôtes sous reset puis huit après un cycle libéré, sans moteur, IRQ ou DRQ pendant le maintien. **32 tests Master Debug réussis, zéro échec ni ignoré, 878 ms.** Il ne s'agit pas d'une nouvelle mesure de vitesse globale, d'une validation physique des médias ni d'un nouveau test RML.

- [x] Implémenter les Master et leurs équipements
  - [x] Références et firmware
    - [x] Créer puis supprimer les références temporaires dans `%TEMP%/rml-master-reference/` : fichiers matériels publics B-Em, JSBeeb et b2, utilisés uniquement pour vérifier les registres et protocoles.
    - [x] Ajouter les ROM Master identifiées dans `%APPDATA%/Retro Media Lab/Emulation/Machines/acorn/Firmware/` ; modifier les dictionnaires de reconnaissance Acorn correspondants, sans écraser de fichier existant.
  - [x] Matériel commun Master
    - [x] Créer les constantes et services sous `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMaster/Common/` : CPU CMOS, mémoire ACCCON/ROMSEL, RAM privée et écran, horloge/CMOS, bus et contrôleur disque Master, d’après les spécifications Acorn et les sources b2, B-Em et JSBeeb.
    - [x] Modifier `Emulators/AcornEmu/Services/Cpu6502*.cs` : mode CMOS 65C12/65C102 distinct du NMOS existant, instructions et indicateurs correspondants.
  - [x] Structure des définitions Master
    - [x] Créer `Common/Machines/BbcMaster/Constants/ModelConstants.cs` et `Dictionaries/ModelCatalog.cs` ; déplacer les définitions Master hors du catalogue BBC Micro et réunir les catalogues dans `Common/Machines/Common/Dictionaries/HardwareModelCatalog.cs`.
    - [x] Déplacer `Common/Machines/BbcMicro/Dictionaries/AcornBBCMicroModel.Master.cs` dans `Common/Machines/BbcMaster/Dictionaries/AcornMasterModel.cs` ; garder la résolution générale existante et les catégories de firmware.
    - [x] Créer `Emulators/AcornEmu/Machines/BbcMaster/Common/Dictionaries/AcornEmuMasterModelCatalog.cs` ; modifier le catalogue AcornEmu pour assembler les définitions Master, sans y écrire les profils en doublon.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/EmulationGlobalUsings.cs` et les catalogues concernés : importer les espaces internes Master et supprimer les noms entièrement qualifiés dans les expressions.
    - [x] Modifier les constantes Master, Tube et CPU nouvelles : remplacer les libellés et valeurs matérielles écrits en brut par leurs constantes ou ressources existantes.
  - [x] Modèles et réglages
    - [x] Modifier les catalogues modèles, fabriques, firmware et réglages Acorn ; créer les profils Master 128, ET et Compact dans leurs dossiers spécifiques ; conserver Turbo et 512 comme cartes du Master 128.
    - [x] Modifier les ressources Acorn : invariants dans 00-Base, traductions dans toutes les langues via Argos ; exposer les ROM et périphériques réellement disponibles.
  - [x] Coprocesseurs
    - [x] Créer les services Tube et cartes sous `Emulators/AcornEmu/Machines/BbcMaster/` : coprocesseur Turbo 65C102 avec RAM 64 Kio et Master 512 avec 80186 et RAM 512 Kio ; exécuter leurs ROM utilisateur et les échanges Tube.
  - [x] Périphériques des variantes
    - [x] Modifier `Emulators/AcornEmu/Dictionaries/FloppyDriveModels.cs`, `Constants/StorageConstants.cs`, `Functions/StorageFunctions*.cs` et la fabrique : profil 3½ pouces Compact, premier lecteur permanent, contrôleur Master fixe ; retirer tape et Winchester des profils qui ne les possèdent pas.
  - [x] Joystick Compact
    - [x] Créer `Emulators/AcornEmu/Machines/BbcMaster/Common/Services/CompactJoystick.cs` ; modifier les ports et associations Common pour le joystick numérique unique du Compact et raccorder PB0–PB4 du VIA utilisateur.
  - [x] Broches VIA Compact
    - [x] Modifier `Emulators/AcornEmu/Services/Via6522.cs` et le câblage de la carte partagée : notifier les changements des sorties et directions PB pour libérer correctement SDA lors des lectures EEPROM ; conserver les callbacks existants des autres modèles.
  - [x] Mémoire non volatile
    - [x] Modifier `Emulators/AcornEmu/Interfaces/IAcornMachine.cs`, `Services/EmulatorCore.cs` et la carte BBC partagée : conserver la CMOS Master et l’EEPROM Compact dans le répertoire de sauvegarde de la machine et les relire à l’ouverture, avec fermeture des fichiers garantie.
  - [x] Transferts Tube Master 512
    - [x] Modifier `Emulators/AcornEmu/Machines/BbcMaster/Tube/Services/TubeUla.cs`, `TurboBoard.cs` et `Services/Cpu6502.cs` : reproduire les indicateurs persistants du FIFO R3 et retirer l’acquittement NMI logiciel ajouté au CPU.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuMasterTests.cs` : vérifier qu’un octet Tube reçu produit un seul transfert DMA et que les indicateurs R3 restent actifs jusqu’à vidage du FIFO.
  - [x] Décodage mémoire de la carte 80186
    - [x] Modifier `Emulators/AcornEmu/Machines/BbcMaster/Tube/Constants/TubeConstants.cs` et `Services/Master512Board.cs` : miroir RAM 80000–BFFFF vers 40000–7FFFF, ROM C0000–FFFFF, conformément au décodage de la carte Acorn.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuMasterTests.cs` : vérifier les miroirs RAM et ROM, puis supprimer le diagnostic externe et ses captures après vérification du démarrage DOS.
  - [x] ROM et géométries finales
    - [x] Modifier `Common/Machines/Common/Dictionaries/AcornFirmware.Coprocessor.cs`, `Common/Machines/BbcMaster/Dictionaries/AcornMasterModel.cs` et `Emulators/AcornEmu/Factories/AcornEmuMachineFactory.cs` : distinguer ROM officielles et variantes compatibles ; ajouter le client 80186 v1.01 testé dans le dossier Firmware Acorn utilisateur sans écrasement.
    - [x] Modifier `Emulators/AcornEmu/Constants/FloppyDriveConstants.cs` et `Dictionaries/FloppyDriveModels.cs` : autoriser les géométries DD 360/720 Kio et 800 Kio reconnues par le WD1770, sans modifier le filtrage 8271 ; une constante par ligne.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuMasterTests.cs` : couvrir les géométries DOS et la classification des ROM de coprocesseur.
    - [x] Créer `artifacts/acorn-master/master512-dosplus21.png` : capture réelle du tampon vidéo de l’émulation après démarrage DOS Plus 2.1, avant suppression des diagnostics temporaires.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuTests.cs` : adapter les attentes Master aux ROM désormais reconnues incompatibles, sans changer les attentes BBC Micro.
    - [x] Modifier `Common/Machines/Common/Dictionaries/AcornFirmware.Application.cs` : déplacer l’identité Client86 déjà existante vers la catégorie coprocesseur, sans doublon SHA-1.
    - [x] Modifier `Emulators/AcornEmu/Functions/MasterFunctions.cs` : valider la taille de la ROM du coprocesseur avec le diagnostic firmware traduit existant.
  - [x] Validation et livraison
    - [x] Créer puis supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornMasterBootDiagnostic.cs` et ses artefacts : vérifier les ROM externes en lecture seule, aucun réglage utilisateur modifié.
    - [x] Créer les tests autonomes `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuMasterTests.cs` pour les instructions, banques, registres, horloge et cartes ; supprimer tout diagnostic externe temporaire et ses artefacts après utilisation.
    - [x] Modifier ce suivi avec les résultats ; compiler le Debug application et tous les modules par `scripts/local-building.cmd --building=debug --modules=A` et vérifier les fichiers livrés.

## Résultats vérifiés

**Reprise du 8 novembre 2026 : les résultats ci-dessous sont historiques, pas une validation du code courant.** Avant correction, les démarrages DOS Plus 1.2 et 2.1 ont échoué ; voir [configuration et preuves](../../../project/emulations/Acorn/BBC%20Master/Master%20512/board-validation.md). Le décodage miroir est toujours présent. Une régression distincte de RETF imm16, introduite dans `59702c180` après le premier bloc Master, a été identifiée et corrigée : la seule correction permet à DOS Plus 2.1 de retrouver A> dans le banc Debug actuel. Une autre image 1.2 de 8BS atteint également A> ; son CPM3.SYS compare la mémoire à 512 Kio, contre 896 Kio dans l'image initiale qui déclenche le test destructif. Aucun autre composant modifié. Ne pas déduire une machine terminée de l'ancienne invite A> ni de ces seuls nouveaux démarrages.

- Démarrage des ROM Master 128, Turbo, ET et Compact vérifié ; DOS Plus 1.2 et 2.1 atteignent l’invite A> sur la carte 80186 de 512 Kio.
- Capture conservée : `artifacts/acorn-master/master512-dosplus21.png`.
- 353 tests Acorn réussis dans la sélection de régression. Les anciens scénarios `AcornModuleTests` et `AcornFloppyDialogAppliesControllerAndBootToItsDrive` sont exclus de cette sélection ; ce résultat ne représente pas la suite Acorn intégrale.
- Diagnostic utilisant les fichiers externes supprimé, assembly de tests reconstruit ; répertoire temporaire `rml-master-reference` supprimé.

- Livraison Debug terminée : script All réussi (code 0), 24 manifestes et DLL présents, exécutable présent ; empreinte du module Acorn livré identique à celle du module compilé. Chemin : `build/Debug/Retro Media Lab/retromedialab.exe`.


## Clôture du bloc avant commit

- [x] Actualiser les anciens scénarios sans modifier le comportement de production.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/Acorn/AcornModuleTests.cs` : choisir explicitement B2 dans ses scénarios natifs, compter BREAK parmi les touches assignables et vérifier les modèles par leur identité plutôt que par un ancien total fixe.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Interface/EmulationViews/EmulationViewsTests.cs` : construire explicitement la configuration B2 du scénario de contrôleur Challenger.
  - [x] Modifier ce suivi avec le résultat de la régression Acorn complète et de la revue des constantes Master ; conserver les avertissements et limites réels, puis committer le bloc Master compilé.

Validation finale : 377 tests Acorn réussis, zéro échec et zéro exclusion. Les anciennes attentes B2 sont désormais explicitement rattachées à B2 ; les touches assignables comprennent BREAK. Revue des constantes : adresses, tailles, registres et libellés Master sont nommés dans les constantes matérielles ou les ressources ; les encodages des instructions restent dans le décodage CPU. Le dernier build Debug All reste à jour : seuls les tests et ce suivi ont changé depuis sa réussite.

## Comparaison statique des liaisons événementielles — 8 novembre 2026

Revue demandée après les omissions System/Atom ; les clôtures et chiffres ci-dessus sont historiques et ne prouvent pas les modifications présentes dans l'arbre courant. Le Compact est laissé de côté sur instruction explicite de l'utilisateur.

Fichiers effectivement relus : `BbcMicroMachine.cs`, `.Interrupts.cs`, `.Via.cs`, `BbcMicroMachine.Master.cs`, `MasterClock.cs`, `MasterMemory.cs`, `TurboBoard.cs`, `Master512Board.cs` ; côté puces, horloges ADC7002/HD146818/VIA/WD1770, entrées et horloges 6502/65C12, interfaces Tube et entrées DMA/interruptions/timers/bus du 80186.

- Le BBC Master réutilise la carte BBC pour VIA, ACIA, ADC, disquettes, Winchester, réseau et vidéo. `ConnectInterrupts` initialise les niveaux puis reçoit leurs mutations ; les masques IRQ/NMI conservés font notifier le CPU seulement quand le niveau agrégé change.
- L'ADC et le RTC publient `NextClockEventChanged` ; la carte conserve l'échéance et ne relit pas leurs sorties pour rechercher une complétion. Les transactions ADC, y compris le décodage déplacé du Master, synchronisent le temps avant lecture/écriture. `MasterClock` synchronise le HD146818 avant les accès de son bus.
- Le VIA reçoit ses phases distinctes ; la phase descendante attend la fin de la transaction CPU. L'échantillonnage PB6 utilise son entrée retenue lorsqu'elle est explicitement raccordée.
- Les mutations Tube notifient IRQ, NMI/DMA et RESET. Turbo reçoit IRQ/NMI ; Master 512 reçoit DMA/INT0, les sorties de timers et HLDA vers sa logique de rafraîchissement. Les compteurs, demandes et phases des timers restent dans le CPU. Ces parcours observés ne recherchent pas périodiquement une réponse sur les sorties Tube.
- `MasterMemory` notifie l'IRQ ACCCON lors de sa mutation et du reset. Ses lectures RAM restent des transactions demandées ; elles ne constituent pas un sondage de changement d'une autre puce.

Cette lecture confirme ces parcours précis, pas la complétude de tous les modes des puces ou tous les raccordements Master. Aucun nouveau test/build Master, gain chiffré ou verdict global ajouté. Pour `PUSH SP`, réutiliser la contradiction documentaire déjà consignée dans `docs/chips/CPU/Intel80186.md` : aucune régression établie et aucune modification appliquée à cette instruction pendant cette revue.
