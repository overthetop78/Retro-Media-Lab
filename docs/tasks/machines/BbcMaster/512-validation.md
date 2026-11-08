# BBC Master 512 — reprise après validation du Turbo

- [x] Vérifier le démarrage DOS Plus avec la carte 80186 actuelle, hors RML.
  - [x] Créer `artifacts/.tmp-tube/Dos512.cs` à partir du diagnostic DOS existant : lecture seule du média, capture native dans `artifacts/TestsMachines/Acorn/BBC Master 512/`, nettoyage garanti dans `finally` et interception des erreurs au niveau du programme.
  - [x] Modifier `docs/chips/CPU/Intel80186.md` avec le résultat du démarrage et les preuves obtenues ; distinguer les défauts observés des hypothèses.
  - [x] Supprimer `artifacts/.tmp-tube/Dos512.cs` et ses sorties de compilation temporaires après utilisation ; reconstruire le banc existant avec `Program.cs`.

Résultat : défaut reproduit, capture examinée (`Invalid Opcode Error` / `Program terminated`). Le banc est reconstruit avec sa source initiale après suppression du diagnostic. La reproduction est terminée ; la correction et la validation du Master 512 restent ouvertes.

Le Turbo précédent est accepté par l'utilisateur si la Release tient la cadence : 4,52 à 5,25 ms/champ mesurées, cible 19,97 ms. Commit de clôture : `1fd6c923b`.

L'ancien résultat de démarrage DOS Plus dans `implementation.md` est historique. Une régression ultérieure a été observée dans `artifacts/.tmp-tube/dos-long-debug.txt` : `Invalid Opcode Error`, puis impossibilité de charger COMMAND.COM. Le premier défaut reste identique avec l'ancienne et la nouvelle ULA Tube ; cela ne démontre pas sa cause.

Ne modifier aucune instruction sur la seule base d'une analogie avec un autre processeur. En particulier, les références consultées sur PUSH SP se contredisent : conserver le comportement actuel jusqu'à vérification propre au 80186.

- [x] Comparer le démarrage avec une autre image DOS Plus, à la demande de l'utilisateur.
  - [x] Créer `artifacts/.tmp-archives/machines/Acorn/BBC Master 512/` avec l'archive source de la nouvelle disquette et son image extraite ; conserver les empreintes de l'image précédente et de la nouvelle dans la fiche machine.
  - [x] Créer `artifacts/.tmp-tube/Dos512Alternative.cs` : garder ROM, matériel, nombre de champs et lecture seule identiques, remplacer uniquement le chemin de disquette et enregistrer les captures natives datées.
  - [x] Modifier `artifacts/.tmp-tube/Dos512Alternative.cs` après la comparaison initiale : prolonger à 3 000 champs et relever le PC parasite pour déterminer si la 2.1 achève son démarrage après le titre, sans changer les ROM ni le matériel.
  - [x] Supprimer `artifacts/.tmp-tube/Dos512Alternative.cs` et reconstruire le banc avec `Program.cs` après consignation des résultats.

- [x] Vérifier l'initialisation sans disquette, à la demande de l'utilisateur.
  - [x] Créer `artifacts/.tmp-tube/Master512Empty.cs` : démarrer avec les mêmes ROM sans monter de média, produire une capture native après initialisation et après BREAK, sans fenêtre RML.
  - [x] Supprimer `artifacts/.tmp-tube/Master512Empty.cs` et reconstruire le banc existant après consignation du résultat.

- [x] Comparer l'autre édition ancienne de 8BS avec le même matériel.
  - [x] Créer `artifacts/.tmp-tube/Dos512Early.cs` : remplacer uniquement l'image par `Gem Disc 1 Early version.640`, conserver le démarrage en lecture seule et 1 500 champs, puis enregistrer son écran natif.
  - [x] Supprimer `artifacts/.tmp-tube/Dos512Early.cs` et son journal après consignation, puis reconstruire le banc initial avec `Program.cs`.

- [x] Comparer les fichiers système des deux images 1.2 après constat du démarrage de l'image 8BS.
  - [x] Créer `artifacts/.tmp-tube/CompareDos12.ps1` : analyser en lecture seule les entrées FAT des quatre fichiers système et leur ordre logique secteur/piste/face documenté, extraire leurs octets sous `artifacts/.tmp-archives/machines/Acorn/BBC Master 512/` et écrire leurs empreintes et différences dans `dos12-file-comparison.json` au même endroit.
  - [x] Supprimer `artifacts/.tmp-tube/CompareDos12.ps1` après consignation de la différence effectivement trouvée ; ne modifier aucune disquette ni aucun format de production.

- [x] Vérifier les commandes DOS et les accès disquette au-delà de A>.
  - [x] Créer `artifacts/.tmp-tube/Dos512Functional.cs` : démarrer la 2.1 sur une copie temporaire accessible en écriture, saisir DIR et un fichier témoin depuis le clavier émulé, afficher son contenu, capturer les étapes et mesurer la carte complète en Debug avec rendu/drainage.
  - [x] Modifier `artifacts/.tmp-tube/Dos512Functional.cs` après le premier passage : mesurer avant les commandes à A>, relever les PC et états d'activité après chaque étape, vérifier ECHO sans redirection et borner l'attente de l'écriture avant TYPE pour distinguer saisie, attente et transfert disque.
  - [x] Supprimer `artifacts/.tmp-tube/Dos512Functional.cs`, sa copie de disquette et son journal après consignation ; garantir détachement, fermeture et suppression dans finally, puis reconstruire le banc initial avec `Program.cs`.

Passage de commandes consigné dans la fiche machine : ECHO simple et création du témoin reviennent à A>, mais la capture TYPE ne prouve pas encore son contenu. Le groupe reste ouvert pour cette raison. Diagnostic et journaux temporaires supprimés, copie de disquette absente après finally, banc initial reconstruit avec Program.cs (0 erreur ; avertissement NU1900 de consultation NuGet).

- [x] Terminer la relecture du témoin DOS 2.1 après création.
  - [x] Créer `artifacts/.tmp-tube/Dos512Readback.cs` depuis le montage DOS existant : copier l'image 2.1, attendre la fin de création puis de TYPE, enregistrer la capture native finale et vérifier le marqueur dans les secteurs sauvegardés, avec nettoyage dans finally et erreurs interceptées.
  - [x] Supprimer `artifacts/.tmp-tube/Dos512Readback.cs`, sa copie de média et son journal après consignation ; reconstruire le banc avec Program.cs.

- [x] Localiser le coût global du Master 512 pendant le passage DOS en cours, sans changer les composants.
  - [x] Créer `artifacts/.tmp-tube/master512-dos-debug.nettrace` par échantillonnage du processus de diagnostic identifié, puis son export speedscope ; consigner la phase observée et les piles principales sans additionner les coûts inclusifs.
  - [x] Supprimer les deux profils temporaires après consignation des résultats dans la fiche machine.

- [x] Vérifier un utilitaire DOS fourni et mesurer la marge Release de la carte complète.
  - [x] Créer `artifacts/.tmp-tube/Master512Software.cs` : même montage MOS 3.20/client 1.01/DOS 2.1 en lecture seule, trois fenêtres de 80 champs à A> avec cycles et empreintes, puis exécuter CHKDSK et capturer son résultat natif ; détacher et fermer les ressources dans finally, intercepter les erreurs.
  - [x] Supprimer `artifacts/.tmp-tube/Master512Software.cs` et son journal après consignation, reconstruire Native sur Program.cs pour retirer le diagnostic.

- [x] Vérifier les raccordements Master existants après les corrections 80186.
  - [x] Créer `artifacts/.tmp-tube/master-regression.trx` en exécutant les tests autonomes AcornEmuMasterTests en Debug : mémoire, voies Tube, DMA, timers, rafraîchissement et configuration des variantes.
  - [x] Supprimer `artifacts/.tmp-tube/master-regression.trx` après consignation du verdict et des limites de couverture.

- [x] Vérifier les deux joysticks depuis un programme DOS exécutant les appels MOS du 80186 (DOS 1.2 ; trajet 2.1 distinct encore ouvert).
  - [x] Créer `artifacts/.tmp-tube/Master512Joystick.cs` : ajouter JOYTEST.COM à une copie temporaire FAT12 de la disquette connue, charger ce programme normalement depuis DOS, appeler INT 4Bh/OSBYTE 16 puis 128 pour quatre voies et les tirs, comparer deux positions opposées puis le repos et capturer leurs valeurs ; aucune modification des ROM ou du matériel.
  - [x] Modifier temporairement `src/RetroMediaLab.Chips/IO/Acorn/Tube/ULA/Services/TubeUla.cs` et `artifacts/.tmp-tube/Master512Joystick.cs` : tracer uniquement les octets réellement transférés sur le canal 2 après armement du programme DOS, avec les PC hôte/parasite, sans lecture ajoutée ni changement de signal ; conserver une sauvegarde pour restauration.
  - [x] Restaurer `src/RetroMediaLab.Chips/IO/Acorn/Tube/ULA/Services/TubeUla.cs` après consignation et reconstruire les dépendances pour retirer le traceur.
  - [x] Modifier `artifacts/.tmp-tube/Master512Joystick.cs` : comparer le même appel avec la version DOS Plus 1.2 de 8BS déjà démarrée, en remplaçant seulement la source de la copie et les noms de captures, sans traceur dans les puces.
  - [x] Modifier `artifacts/.tmp-tube/Master512Joystick.cs` : collecter les cinq résultats MOS avant les sorties DOS, après observation de la première voie correcte sous DOS 1.2 puis du blocage après affichage ; vérifier les trois états et conserver leurs captures distinctes.
  - [x] Supprimer `artifacts/.tmp-tube/Master512Joystick.cs`, la copie de disquette et son journal après consignation ; garantir détachement/fermeture/suppression dans finally et reconstruire Native avec Program.cs.

- [x] Résoudre le contexte des appels MOS sous DOS Plus 2.1.
  - [x] Créer `artifacts/.tmp-tube/Master512MosContext.cs` depuis le montage DOS connu : exécuter la commande officielle STAR FX16,4, relever son retour et les vecteurs MOS en mémoire avant/après, comparer son trajet au premier appel INT 4Bh du programme de contrôle ; conserver les captures natives et les faits sans modifier le matériel par hypothèse.
  - [x] Modifier `artifacts/.tmp-tube/Master512MosContext.cs` : entourer les appels MOS du programme COM par les sous-fonctions XIOS 81h/82h via INT E0h, CL=32h et un descripteur de cinq octets documenté ; vérifier les quatre voies et les tirs sous DOS 2.1 sur une copie temporaire nettoyée dans finally.
  - [x] Supprimer `artifacts/.tmp-tube/Master512MosContext.cs` et ses journaux après consignation ; détacher/fermer les médias dans finally et reconstruire Native avec Program.cs.

- [ ] Vérifier la souris par le trajet natif Master 512 sous DOS Plus, sans ouvrir RML.
  - [x] Créer `artifacts/.tmp-archives/machines/Acorn/BBC Master 512/m512tech05.htm` : archiver le chapitre technique du système 6502 et identifier le protocole souris réellement fourni par DOS/GEM avant tout programme de contrôle.
  - [x] Créer `artifacts/.tmp-tube/Master512Mouse.cs` : reprendre le montage DOS 2.1 connu, activer l'interface de pointeur existante et exécuter le logiciel invité approprié ; injecter mouvements et boutons par le contrat normal, conserver des captures natives comparables et intercepter les erreurs ; détacher et libérer les médias dans finally.
  - [x] Modifier `artifacts/.tmp-tube/Master512Mouse.cs` : corriger le format du disque GEM en 800 Kio à secteurs de 1024 octets sous le profil ADFS à base zéro (piste intermédiaire testée puis rejetée pour DOS Plus), puis répéter le lancement ; le premier montage IBM 512 octets ne lance pas GEM et ne valide aucune entrée souris.
  - [x] Modifier `artifacts/.tmp-tube/Master512Mouse.cs` : afficher DIR et TYPE GEM.BAT avant le lancement afin de distinguer lecture du répertoire, montage et commande ; conserver les sorties natives sans changer les composants.
  - [x] Modifier temporairement `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/Common/Services/Disks.cs` et `artifacts/.tmp-tube/Master512Mouse.cs` : conserver les octets effectivement lus dans le registre de données WD pour le lecteur 1 et la RAM parasite après DIR, sans lecture matérielle supplémentaire ; restaurer Disks.cs dans finally après le passage et comparer au répertoire source.
  - [x] Modifier `artifacts/.tmp-tube/Master512Mouse.cs` : contrôler directement la géométrie et les secteurs retournés par le relais média avant de poursuivre le diagnostic ; ne pas confondre ces lectures hors circuit avec les transferts WD observés.
  - [x] Modifier temporairement `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/Common/Services/Disks.cs` et `artifacts/.tmp-tube/Master512Mouse.cs` : tracer les seules écritures de commandes/piste/secteur du lecteur B et leurs positions dans le flux déjà lu, arrêter après DIR ; restaurer la source dans finally pour déterminer pourquoi les données du secteur 1 sont reçues avant le répertoire.
  - [x] Intégrer le format DOS Plus 800 Kio distinct avant de poursuivre GEM.
    - [x] Modifier `src/RetroMediaLab.MediaEngine/Constants/AcornDiskImageFormatIds.cs` et `Images/Formats/Floppy/Adf/AcornAdfGeometry.cs` : déclarer acorn.dosplus.800 et sa géométrie 80 × 2 × 5 × 1024, identifiants 1–5, sans changer ADFS.
    - [x] Modifier `src/RetroMediaLab.MediaEngine/Images/Formats/Floppy/Adf/AdfReader.cs` et `Images/Formats/Detection/AdfImageFormatDetectionRule.cs` : reconnaître le préfixe FAT FD FF FF sur une image de 819200 octets, reconstruire la bonne numérotation et respecter le format explicite.
    - [x] Modifier `src/RetroMediaLab.MediaEngine/Images/Formats/ImageFormatCatalog.cs`, `Images/Conversion/Acorn/AcornAdfConversionService.cs`, `Images/Writing/MediaWritingComposition.cs`, `Images/Writing/Encoding/SectorImageTrackTimingCatalog.cs`, `Images/Creation/BlankSectorImageFactory.cs` et `src/RetroMediaLab.App/Resources/00-Base/Formats.resx` : déclarer le format invariant, ses extensions, l'écriture et la cadence DD via les services existants, sans dépendance MediaEngine dans Acorn.
    - [x] Créer `tests/RetroMediaLab.Tests/Media/AcornDosPlus800Tests.cs` : contrôler en mémoire la détection, les secteurs 1–5 et la conservation ADFS à base zéro ; exécuter ce groupe utile en Debug.
  - [x] Modifier `artifacts/.tmp-tube/Master512Mouse.cs` : retirer toute instrumentation, utiliser acorn.dosplus.800 et reprendre le lancement de GEM puis les mouvements/boutons avec captures natives.
  - [x] Terminer les raccordements du nouveau format dans les services communs avant de relancer GEM.
    - [x] Modifier `src/RetroMediaLab.MediaEngine/Images/Visualization/SectorImageVisualizationPolicyRegistry.cs`, `Images/Formats/Floppy/Scp/Decoding/ScpSectorDecodingComposition.cs` et `Images/Reading/Metadata/DiskSystemResolver.cs` : associer le format DOS Plus à ISO MFM et au système BBC, afin que ReadTrack produise effectivement ses impulsions.
    - [x] Modifier `src/RetroMediaLab.MediaEngine/Constants/DiskImageFileExtensions.cs`, `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Constants/FloppyDriveConstants.cs` et `Dictionaries/FloppyDriveModels.cs` : exposer l'extension 800 et la création physique 80 × 2 × 5 × 1024 via le relais existant, sans référence à MediaEngine dans Acorn.
    - [x] Modifier `tests/RetroMediaLab.Tests/Media/AcornDosPlus800Tests.cs` : contrôler aussi l'encodage physique MFM et la réécriture en mémoire via le writer existant ; exécuter le groupe Debug après le bloc.
    - [x] Modifier `artifacts/.tmp-tube/Master512Mouse.cs` : relancer les mêmes commandes DOS/GEM avec les politiques de flux raccordées ; conserver les résultats et captures réellement observés.
  - [x] Modifier `artifacts/.tmp-tube/Master512Mouse.cs` : attendre la fin des lectures GEM par fenêtres bornées de 400 champs (deux fenêtres stables, maximum 6000 champs), puis seulement envoyer les déplacements et boutons ; capturer les étapes sans confondre le curseur d'attente avec le bureau terminé.
  - [x] Modifier `artifacts/.tmp-tube/Master512Mouse.cs` : après le bureau chargé, placer le pointeur sur File et comparer l'effet des trois boutons avec captures du menu, en revenant par Escape entre les essais ; ne pas qualifier un clic dans une zone vide de validation des boutons.
  - [x] Modifier `src/RetroMediaLab.MediaEngine/Images/Creation/BlankSectorImageFactory.cs`, `Images/Formats/Floppy/Adf/AdfReader.cs` et `tests/RetroMediaLab.Tests/Media/AcornDosPlus800Tests.cs` : initialiser les entrées réservées FD FF FF du FAT vierge pour qu'il reste identifiable après réouverture, respecter les identifiants explicites sans distinction de casse et vérifier ces deux cas en mémoire.
  - [x] Modifier `artifacts/.tmp-tube/Master512Mouse.cs` : conserver le bureau natif ouvert dans le banc avec un contrôle console synchrone move/button/key/cap/quit, afin de choisir une action visible du menu GEM sans redémarrer à chaque ajustement ; quitter normalement pour exécuter finally.
  - [x] Créer `artifacts/.tmp-archives/machines/Acorn/BBC Master 512/m512tech09.htm` : conserver le chapitre de référence du format DOS Plus 800 utilisé pour cette correction.
  - [x] Créer `artifacts/.tmp-archives/machines/Acorn/BBC Master 512/b-em-mouse.c` et `smallymouse2.html` : conserver les sources de comparaison du câblage et des phases pour expliquer les anomalies de sens rapide et de bouton primaire, sans modifier les puces sur une hypothèse.
  - [x] Créer `artifacts/.tmp-tube/master512-media-regression.trx` en exécutant les tests existants AcornEmuMediaCapacityTests et AcornEmuMasterTests en Debug après le raccordement du nouveau format ; supprimer ce journal après consignation.
  - [x] Raccorder le brochage de la souris Acorn fourni avec le Master 512.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/Common/Services/BbcMicroMachine.cs` et `Services/UserPortPointer.cs` : choisir pour Master 512 le brochage Acorn déjà géré pour Compact (PB3/PB4, boutons PB0/PB2/PB1), garder AMX pour les autres cartes ; conserver phases, échéances, notifications et VIA sans sondage.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuPointerInputTests.cs` : vérifier les trois boutons via le VIA réel selon le coprocesseur et conserver les cas AMX/Compact existants ; exécuter le groupe pointeur en Debug.
    - [x] Modifier `artifacts/.tmp-tube/Master512Mouse.cs` : reprendre le bureau GEM, relever les compteurs du pilote dans la RAM hôte sans accès de bus supplémentaire, vérifier les deux sens X/Y, le retour après deux impulsions opposées et la réception du clic gauche ; conserver les captures puis quitter normalement. Le clic observé ferme le menu : il ne valide pas Exit to DOS, car la cible a été dépassée.
  - [x] Modifier `src/RetroMediaLab.MediaEngine/Images/Conversion/Acorn/AcornAdfConversionService.cs`, `Images/Formats/Floppy/Adf/AdfReader.cs`, `Images/Formats/Floppy/Adf/AcornAdfWriter.cs` et `tests/RetroMediaLab.Tests/Media/AcornDosPlus800Tests.cs` : conserver la même sélection des extensions avec un identifiant de format en majuscules et aligner les commentaires sur le support DOS Plus ; vérifier les extensions IMG/800 et l'exclusion ADF pour ce profil.
  - [x] Créer `artifacts/TestsChips/Wd1770/BBC Master 512/20261108-master512-mouse-dos800-mfm-directory-debug.bmp` et les copies des preuves GEM sous `artifacts/TestsChips/Mos6522/BBC Master 512/` : conserver les images natives observées, sans les modifier.
  - [x] Supprimer `artifacts/.tmp-tube/Master512Mouse.cs` et ses journaux jetables après consignation ; conserver seulement les captures et références utiles, puis reconstruire Native avec Program.cs.

La validation souris native reste ouverte pour une action applicative explicite ; aucun essai physique RML n'a été effectué pendant ce bloc.

  - [ ] Vérifier une action applicative avec la souris Acorn dans GEM.
    - [ ] Créer `artifacts/.tmp-tube/Master512GemApplication.cs` : reprendre le montage et le contrôle console consignés dans dos-plus-800.md, lancer une application GEM et vérifier une sélection ou un tracé avec le bouton gauche, sans attribuer au matériel un clic qui manque sa cible.
    - [ ] Supprimer `artifacts/.tmp-tube/Master512GemApplication.cs` et ses journaux après consignation, fermer les médias dans finally et conserver seulement les captures natives utiles.