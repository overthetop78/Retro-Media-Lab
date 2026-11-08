# Acorn System 2 — reprise après System 1

System 1 clôturé et committé dans `f3aa3bbea`. Traiter System 2 avant System 3 ; réutiliser les puces déjà vérifiées, puis corriger seulement les défauts établis du montage.

- [x] Terminer le System 2.
  - [x] Établir les références matérielles et la référence native de carte complète.
    - [x] Créer les sources utiles sous `artifacts/.tmp-archives/machines/Acorn/Acorn System 2/` : manuel Acorn 200.002 VDU 40, schéma PDF et pages rendues, `vdu40.cpp`, COS et BASIC System (source et binaire 4 Kio).
    - [x] Créer `artifacts/.tmp-system2/System2.csproj` et `Program.cs` : banc Debug sans fenêtre, sorties isolées, vraie ROM COS40 et SAA5050 externes, montage System 2 existant sans BASIC initialement, capture native de démarrage et commande TEST réellement exécutée ; libérer les entrées dans finally.
    - [x] Modifier `artifacts/.tmp-system2/Program.cs` pour relever les registres CRTC, les frontières de champs et les caractères réellement reçus par le COS ; conserver les premières captures et le journal dans `artifacts/TestsMachines/Acorn/Acorn System 2/`.
  - [x] Compléter le montage et ses logiciels compatibles à partir des défauts établis.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/AcornSystemMachine.cs` : cadencer la VDU40 par PH2 de la carte CPU, conformément au schéma 200.002 page 1, au lieu des 2 MHz de la VDU80 ; conserver la cadence de cette dernière.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Constants/SystemVideoConstants.cs` et `Services/SystemVideo.cs` dans le même dossier Common : décoder les miroirs CRTC de toute la page 08, sans absorber la page 09 ni modifier les miroirs VDU80.
    - [x] Modifier `SystemVideo.cs`, `SystemVideo.Teletext.cs` et `AcornSystemMachine.cs` dans ce dossier Services : mémoriser la parité du champ terminé, transmettre un seul champ via les métadonnées existantes, publier sur les frontières CRTC et conserver le temps CPU réellement écoulé ; raccorder VSYNC au SAA5050 par sa notification existante.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemTests.cs` : vérifier miroirs, limites de décodage, alternance des champs et conservation du mode progressif, avec caractères synthétiques autonomes ; utiliser le banc temporaire pour la cadence COS réelle et BASIC, sans média externe dans les tests permanents.
    - [x] Modifier `artifacts/.tmp-system2/Program.cs` : vérifier la cadence de chaque champ et ses métadonnées, le BASIC System à C000 puis SAVE/LOAD du COS par les signaux réels de bande. Résultat : champs et BASIC corrects, SAVE terminé ; LOAD bloqué sur NHITONE absent. Cet échec reste à corriger avant clôture.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Common/Machines/Common/Dictionaries/AcornFirmware.Basic.cs` et `Common/Machines/AcornSystem/Dictionaries/AcornSystemModel.cs` : identifier la reconstruction BASIC System vérifiée par son SHA-1 et la déclarer compatible dans le rôle BASIC de System 2 ; conserver le rejet des rôles et modèles incompatibles.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemTests.cs` : vérifier cette compatibilité et les rejets de mauvais rôle et du System 1, puis exécuter ces seuls cas après le groupe vidéo déjà passé.
    - [x] Créer dans le dossier Firmware existant la ROM BASIC System 2 correcte si une source identifiée et compatible est trouvée, sans écraser d'image ni modifier silencieusement la configuration utilisateur. La configuration actuelle place DOS80 dans BASIC : ne pas utiliser ce montage incorrect comme banc de référence.
    - [x] Modifier `artifacts/.tmp-system2/Program.cs` : établir le défaut LOAD et archiver les schémas originaux et la retranscription propre. IC1 est remis à zéro sur Q5·Q4·Q2 (26), IC6 reçoit les descendants Q3, Q7 alimente NHITONE et D du 4013 ; le comparateur cadence cette bascule avant la remise à zéro d'IC6. Les actions concrètes de carte suivent dans le suivi SystemTape.
    - [x] Créer `src/RetroMediaLab.Chips/Controllers/Acorn/SystemTape/Constants/SystemTapeConstants.cs` et `Services/SystemTapeInterface.cs` : reproduire les trois compteurs 4024 et les bascules du montage, les entrées CASOUT/comparateur, les sorties audio/CASIN/NHITONE et les échéances horodatées ; avancer entre échéances par arithmétique exacte, publier uniquement les changements.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/AnalogTape.cs` : notifier les changements du niveau PCM comparé, y compris arrêt/reset, et conserver ses traitements FSK existants pour leurs consommateurs ; la carte System utilise le mode waveform déjà disponible.
    - [x] Modifier `AcornSystemMachine.cs` dans Common/Services : raccorder la carte cassette aux PA6/PA7/PA5, à PH2 et au transport waveform, conserver l'échéance reçue localement ; réutiliser cette carte pour System 1 et les cartes Standard6502, préserver les autres montages en cours.
    - [x] Modifier `SystemKeyboard.cs` dans ce dossier Services : présenter le strobe physique ASCII actif bas dans DataWithStrobe, comme le niveau déjà notifié par StrobeChanged. Le COS RDC attend PB7 haut au repos puis bas à l'appui ; LEADER attend son retour haut après Espace.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemTests.cs` : couvrir PB7 haut au repos, bas durant l'appui et haut au relâchement, en conservant les données ASCII et les notifications existantes.
    - [x] Modifier `SystemKeyboardConstants.cs` pour nommer PA5/NHITONE et `AcornSystemMachine.cs` pour conserver les compteurs cassette lors du RESET CPU chaud et l'échéancier cumulé de présentation des LED System 1 ; seul le cycle d'alimentation remet la carte cassette à zéro.
    - [x] Créer `tests/RetroMediaLab.Chips.Tests/Controllers/Acorn/SystemTapeTests.cs` : vérifier division/fréquences, fronts Q3/Q7, échantillonnage avant reset, changements CASOUT et notifications sans doublons, équivalence avancement groupé/unitaire.
    - [x] Modifier `artifacts/.tmp-system2/Program.cs` : exécuter le vrai COS SAVE/LOAD, comparer les huit octets et mesurer la carte complète en Debug, avec captures et sorties conservées. Refaire le transfert System 1 après raccordement commun ; retirer ensuite les bancs externes.
  - [x] Vérifier puis livrer le bloc System 2.
    - [x] Modifier `AcornSystemMachine.cs` pour conserver le dernier niveau CASOUT raccordé et supprimer ses retransmissions identiques lors du balayage des touches System 1 ; reconstruire cette entrée dérivée au cycle d'alimentation avant le RESET des ports.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemTests.cs` pour contrôler les notifications du transport waveform, y compris arrêt et silence, puis vérifier le groupe System partagé après intégration de la carte cassette et du clavier.
    - [x] Modifier `artifacts/.tmp-system2/Program.cs` avec les contrôles finaux du logiciel, de la bande et des cadences après les corrections effectivement nécessaires ; produire les captures finales dans les dossiers machine et puces, distinguer les preuves natives de celles de RML.
    - [x] Créer puis vérifier les fichiers livrés sous `build/Debug/Retro Media Lab/` avec `scripts/local-building.cmd --building=debug --modules=A` : exécutable, Chips et les 24 modules. Livraison regroupée avec System 3/4/5 à la demande de l'utilisateur ; fournir le montage System 2 pour l'essai réel.
    - [x] Supprimer `artifacts/.tmp-system2/Program.cs`, `System2.csproj` et leurs seules sorties temporaires après consignation et vérification du chemin résolu ; conserver sources de référence et captures utiles ignorées par Git.

Les fichiers des corrections établies, leurs tests autonomes et la livraison sont ajoutés dans l'ordre avant modification. Les informations acquises et résultats restent dans la fiche System 2, sans tâches documentaires séparées.

- [x] Vérifier System 2 dans RML avec les manipulations utilisateur.
  - [x] Observer le montage livré avant de poursuivre les autres System.
    - [x] Créer les captures réelles de RML sous `artifacts/TestsMachines/Acorn/Acorn System 2/20261108-rml-*` : démarrage COS40, BASIC et calcul, cadence affichée et essai Tape ; conserver les étapes et distinguer ces observations des preuves natives déjà acquises.

Reprise : dépôt propre au début de l'essai, contrôle Windows rétabli après réinitialisation de sa session ; aucune fenêtre RML ouverte lors de l'inventaire. Les trois firmwares demandés sont présents (COS40 2048 octets, saa5050 960 octets, BASIC reconstruit 4096 octets). Demande envoyée à l'utilisateur pour ouvrir le Debug et afficher System 2. Aucun résultat RML de ce nouveau passage acquis à cet instant.

- [x] Corriger la saisie ASCII System 2–5 signalée pendant l'essai.
  - [x] Raccorder les caractères hôtes existants et le pavé numérique.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Dictionaries/SystemKeyboardMap.cs` : ajouter chiffres, opérateurs, séparateur et Entrée du pavé au tableau ASCII existant, sans changer la matrice System 1.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/SystemKeyboard.cs` : consommer HostCharacters pour les touches imprimables à chaque changement d'entrée ; conserver contrôles, fallback physique, répétition et strobe ; ignorer les caractères non représentables plutôt que les tronquer en ASCII.
  - [x] Vérifier le bloc avant livraison.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemTests.cs` : cas AZERTY guillemets, chiffres avec Maj, ponctuation, lettres, relâchement et caractère non représentable ; pavé complet et Enter, fallback sans HostCharacters, régression matrice hexadécimale existante.
    - [x] Créer le rapport `artifacts/TestsMachines/Acorn/AcornSystem/system-keyboard-debug.trx` avec les tests System utiles ; conserver les captures actuelles avant la reprise de l'essai.
    - [x] Créer la livraison corrigée sous `build/Debug/Retro Media Lab/` par `scripts/local-building.cmd --building=debug --modules=A` hors bac à sable ; vérifier exécutable et les 24 DLL de modules avant les nouveaux essais RML.

L'utilisateur autorise maintenant l'agent à effectuer lui-même toutes les manipulations de RML. L'essai reste à terminer, avec captures, avant de passer à une autre famille.

  - [x] Achever les preuves dans la vraie fenêtre.
    - [x] Modifier le helper préexistant `artifacts/.tmp-rml-input/SendKeys.ps1` : option de touche étendue pour envoyer l'Entrée E0 du pavé, avec relâchement garanti en finally ; tenir compte du verrouillage majuscules Windows dans la saisie de texte demandée, afin de ne pas inverser les lettres du test.
    - [x] Créer les captures `artifacts/TestsMachines/Acorn/Acorn System 2/20261108-rml-keyboard-fixed-*` : calcul 46 par clavier principal et pavé, ponctuation AZERTY et Entrée étendue ; conserver les cadences affichées.
    - [x] Créer via le parcours REC existant une bande de test sous `F:/Retro/Acorn System/`, enregistrer un programme BASIC, puis vérifier sa relecture après effacement du programme invité ; captures sous `artifacts/TestsMachines/Acorn/Acorn System 2/20261108-rml-tape-*`.
    - [x] Modifier le helper préexistant `artifacts/.tmp-rml-input/SendKeys.ps1` : raccourci de touches virtuelles maintenues pour cibler les champs du dialogue Windows dont les index UIA ne sont pas utilisables ; vérifier le focus du propriétaire et libérer les touches en finally.

- [x] Corriger les défauts de présentation signalés pendant l'essai.
  - [x] Rétablir le contraste du bouton par défaut de création.
    - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Styles/StorageCreationStyles.xaml` : IsDefault utilise ControlBrush et TextBrush déjà fournis par le thème, avec bordure AccentBrush ; supprimer le couple fond AccentBrush / texte blanc qui devient blanc sur blanc avec l'accent courant.
    - [x] Créer la capture `artifacts/TestsMachines/Acorn/Acorn System 2/20261108-rml-create-button-fixed.png` après livraison corrigée et vérification dans le même dialogue.

- [x] Corriger le signal d'enregistrement rendu audible comme bruit de lecteur.
  - [x] Séparer la donnée enregistrée de l'écoute.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/AnalogTape.cs` : réserver MonitorSample à la lecture ; conserver intégralement le signal écrit par Record et les bruitages mécaniques du parcours existant.
  - [x] Vérifier le signal et son écoute.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemTests.cs` : vérifier que REC conserve les deux polarités enregistrées sans les émettre dans MonitorSample, puis que PLAY restitue ces mêmes échantillons.

Retour utilisateur : sifflement continu en REC, plus aigu pendant la sauvegarde. Cause établie dans AnalogTape.MonitorSample : la sortie électrique d'enregistrement était injectée dans le mélange audio invité. REC n'alimente plus cette écoute ; Record conserve ses échantillons et ses timings. Le groupe System passe en Debug : 47 tests, zéro échec, rapport system-keyboard-audio-debug.trx ; 33 tests du transport Tape passent également. Livraison Debug tous modules réussie, code 0 ; 24 manifestes et assemblages vérifiés. Bouton « Créer et monter » lisible sur fond sombre dans le dialogue livré, capture conservée ; dialogue annulé sans créer de nouveau fichier. La suppression du sifflement est vérifiée au niveau des échantillons, pas par une écoute humaine de ce build.
