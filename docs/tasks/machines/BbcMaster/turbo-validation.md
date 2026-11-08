# Master Turbo — validation du coprocesseur et du Tube

## Reprise après acceptation utilisateur du Master 128

Le Master 128 est accepté fonctionnel par l'utilisateur. Le modèle en cours est le Master Turbo ; aucun lancement de RML n'est demandé pour cette reprise. Réutiliser les preuves Tube, RAM privée, médias et entrées déjà consignées ci-dessous. Ne pas répéter la candidate d'avance CPU rejetée après comparaison.

- [x] Actualiser la vérification du Turbo depuis le code courant, puis compléter les preuves manquantes après résolution de la fusion Git en cours.
  - [x] Modifier `docs/project/emulations/Acorn/BBC Master/Master Turbo/board-validation.md` : distinguer l'ancienne observation vidéo du chemin actuel transmettant les champs et leur parité ; identifier les fichiers vérifiés et les limites de cette inspection, sans inventer de mesure.
  - [x] Créer temporairement `artifacts/.tmp-tube/TurboCurrent.cs` : réutiliser la configuration et les ROM existantes en mémoire, vérifier le logiciel invité et les champs vidéo du Turbo sans RML, mesurer la carte complète Debug avec rendu et drainage audio, relever ms/image, cycles hôte/parasite, FPS, Hz et pourcentage nominal ; conserver les captures natives dans les dossiers TestsMachines/TestsChips et libérer les ressources possédées dans finally. Ne lancer qu'après résolution des conflits affectant Acorn et les tests.
  - [x] Supprimer `artifacts/.tmp-tube/TurboCurrent.cs` et ses seules sorties jetables après consignation ; reconstruire le banc Native sur son Program.cs antérieur (remplacer son ancien nom EjectCassette par EjectTape pour le code actuel), sans supprimer les captures utiles ignorées par Git.

- [x] Vérifier les circuits existants dans le Master Turbo assemblé, sans reprendre les puces déjà validées sans défaut établi.
  - [x] Créer `artifacts/.tmp-tube/MasterTurboValidation.cs` : charger MOS 3.50 et le client 65C102 externes, assembler le Turbo à ses fréquences réelles, exécuter un programme BASIC allouant 40 000 octets pour distinguer la RAM parasite du BASIC hôte, puis SAVE/LOAD d'un motif RAM via les appels MOS/Tube et une SSD dédiée créée par le SDK. Vérifier les verdicts exécutés après CLS, relever les cycles des deux CPU et les cadences Debug de carte complète, conserver les captures machine/Tube ; détacher les images et entrées avant libération dans finally, sans modifier la configuration utilisateur.
  - [x] Supprimer `artifacts/.tmp-tube/MasterTurboValidation.cs` et ses seuls médias temporaires après consignation ; reconstruire le banc avec sa source antérieure.

Références et circuits : [Tube ULA](../../chips/TubeUla/suivi.md), [65C12/65C102](../../chips/Mos65C12/suivi.md). La baisse mineure acceptée du Master 128 ne vaut pas validation du Turbo ; ne pas masquer une erreur fonctionnelle par une mesure de cadence.

- [x] Supprimer les consultations des broches auxiliaires constantes du bus Turbo, sans modifier le CPU.
  - [x] Créer `artifacts/.tmp-tube/TurboCompare.cs`, `turbo-bus-reference.dll` et `turbo-bus-reference.cs` depuis le banc et l'assembly/source Acorn courants : limiter la comparaison au Turbo, aux mêmes scénarios repos/SOUND/MODE 1 et aux empreintes/cycles, avec ressources libérées dans finally.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMaster/Tube/Services/TurboBoard.cs` : déclarer par le contrat `ControlSignals` existant les entrées auxiliaires sans source variable dans ce bus. Conserver les niveaux par défaut déjà fournis par ICpuBus, les notifications IRQ/NMI/RESET existantes, le rapport 4/2 MHz et les transferts à chaque cycle ; ne pas retirer de fonctionnalité au 65C102 partagé.
  - [x] Créer `artifacts/.tmp-tube/turbo-bus-<0..3>.log` et `.err` par deux paires alternées Debug sur carte complète ; reconstruire chaque variante depuis la source avec les mêmes dépendances du banc (ne pas substituer la DLL livrée, dont la version d'assembly est différente). Exécuter après le bloc les régressions Turbo/Tube existantes. Ne retenir le changement que si les cycles/sorties restent identiques et le coût global baisse ; sinon restaurer la source de référence et reconstruire.
  - [x] Supprimer les nouvelles copies, source et journaux temporaires après consignation ; restaurer l'assembly du banc correspondant à la source retenue puis reconstruire son Program.cs antérieur.

- [x] Livrer le bloc Turbo pour l'essai dans RML, sans annoncer le budget Debug atteint.
  - [x] Créer/remplacer `build/Debug/Retro Media Lab/retromedialab.exe`, ses bibliothèques et les 24 modules par `scripts/local-building.cmd --building=debug --modules=A` ; vérifier la réussite du script et les fichiers livrés avant communication à l'utilisateur.

Livraison du 7 novembre : script terminé avec code 0, exécutable et Chips présents, 24 manifestes et leurs DLL d'entrée présents, DLL Acorn livrée identique à celle compilée. Essai utilisateur du Turbo et budget Debug encore ouverts ; ne pas passer au Master 512 sur la seule acceptation du Master 128.

- [x] Vérifier le Turbo dans la livraison RML en restaurant les réglages de l'essai.
  - [x] Créer `artifacts/TestsMachines/Acorn/BBC Master Turbo/20261107-rml-turbo.png` et sa copie dans `artifacts/TestsChips/TubeUla/BBC Master Turbo/` depuis la fenêtre réellement exécutée : ouvrir une configuration Turbo existante si disponible ; sinon sélectionner temporairement le coprocesseur Turbo dans le Master 128, noter les valeurs antérieures et les restaurer après extinction. Observer le démarrage, les compteurs et un verdict BASIC si la saisie l'atteint ; fermer et attendre la disparition de la seule instance lancée pour cet essai dans finally. Ne pas présenter un simple démarrage comme une validation complète.
  - [x] Créer `artifacts/TestsMachines/Acorn/BBC Master Turbo/20261107-rml-turbo-tape.png` et sa copie sous `artifacts/TestsChips/TubeUla/BBC Master Turbo/` depuis la session utilisateur empruntée : conserver la progression réelle du catalogue tape et le retour BASIC après Escape, sans clic ni fermeture de cette session.

Preuve RML obtenue après ouverture manuelle par l'utilisateur : démarrage 65C102, catalogue tape progressif et retour BASIC/Escape, 32,1–34,3 FPS et 1,28–1,37 MHz hôte. La première instance possédée est fermée ; la nouvelle instance utilisateur reste empruntée, sans clic ni modification/fermeture. Aucun jeu RML ni budget nominal Turbo validé par ces seules captures ; voir la fiche machine.



Reprise manuelle proposée par l'utilisateur : la sélection automatisée reste sur Aucune ; instance de test fermée, aucun démarrage Turbo ni capture RML Turbo acquis. Attendre l'ouverture manuelle avant observation ; laisser cette action non cochée tant que la preuve demandée n'existe pas.

- [x] Identifier le défaut SimCity signalé pendant l'essai utilisateur, sans modifier une puce sur une hypothèse de compatibilité.
  - [x] Créer `artifacts/TestsMachines/Acorn/BBC Master Turbo/20261107-rml-simcity-defect.png` depuis l'écran observé ; identifier en lecture seule le média SimCity enregistré et son identité interne, puis distinguer la version Electron de la version BBC avant de décider d'une correction.
  - [x] Créer `artifacts/.tmp-tube/SimCityInspect.cs` et `SimCityInspect.csproj` : ouvrir uniquement en lecture le SSD identifié par le service MediaEngine existant dans ce banc indépendant, réutiliser le lecteur DFS existant pour inspecter le chargeur BASIC et ses adresses ; libérer l'image dans finally, comparer son empreinte avant/après et ne modifier aucune configuration, ROM ou puce. Confronter le résultat aux cartes mémoire du guide Acorn 65C102. Aucun ajout de référence MediaEngine aux modules d'émulation.
  - [x] Créer `artifacts/.tmp-archives/machines/Acorn/BBC Master Turbo/Acorn_65C102CoProUG.pdf` depuis le guide constructeur consulté et conserver ses références/page utiles dans la fiche existante.
  - [x] Supprimer `artifacts/.tmp-tube/SimCityInspect.cs`, `SimCityInspect.csproj` et les seuls fichiers de compilation propres à ce diagnostic après consignation ; reconstruire le banc avec son Program.cs préexistant pour retirer le diagnostic de l'assembly.

SimCity clôturé comme incompatibilité logicielle de cette version avec le Tube actif : chargeur inspecté, séparation mémoire confirmée par Acorn, comportement avec/sans coprocesseur confirmé par l'utilisateur, puis conclusion explicitement acceptée. Aucune puce modifiée pour contourner le jeu. Le budget de cadence Turbo reste distinct et ouvert.

- [x] Localiser le coût restant du Turbo sur carte complète en Debug.
  - [x] Créer `artifacts/.tmp-tube/TurboProfile.cs` depuis le banc existant, limité au Turbo au repos, avec rendu et drainage audio, cycles et empreintes ; conserver le nettoyage finally et un traitement d'erreur sans fenêtre native.
  - [x] Créer `artifacts/.tmp-tube/turbo-profile.nettrace`, `turbo-profile.speedscope.json` et `turbo-profile.log` par échantillonnage du seul banc Debug ; produire `turbo-profile-summary.json` avec coûts propres et inclusifs des chemins réellement exécutés, sans additionner parents et descendants.
  - [x] Supprimer `artifacts/.tmp-tube/TurboProfile.cs` et les sorties jetables de ce profil après consignation, puis reconstruire le banc avec son `Program.cs` antérieur.

Collecte réussie après reconstruction du manifeste de dépendances : 29,098589 ms/image pour 1 200 images au repos. Vidéo 33,16 % et exécution parasite 30,41 % inclusifs du temps RunFrame échantillonné, démarrage inclus dans le profil. Détails et limites dans la fiche Master Turbo ; budget Debug non atteint. Sources/sorties temporaires supprimées, Native reconstruit sur son Program.cs antérieur avec code 0. Aucun changement de puce ni nouveau commit pendant ce diagnostic.

- [x] Évaluer l'avance de l'oscillateur Turbo par l'entrée groupée existante du CPU.
  - [x] Créer `artifacts/.tmp-tube/turbo-clock-reference.cs` depuis le TurboBoard courant et `TurboClockCompare.cs` depuis le banc existant : limiter au Turbo, 160 images par scénario repos/SOUND/redessin MODE 1, mêmes ROM/cycles/empreintes, gestion d'erreur et nettoyage finally ; réserver les mesures à l'absence de RML.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMaster/Tube/Services/TurboBoard.cs` : calculer une fois le nombre de périodes d'entrée par cycle hôte depuis les fréquences existantes et transmettre ces périodes à AdvanceInputClocks ; retirer le compteur cible et les deux validations publiques Clock répétées, conserver tous les cycles CPU et le maintien RESET Tube.
  - [x] Modifier `tests/RetroMediaLab.Chips.Tests/CPU/MOS/MCS6500/65C12/ClockPhaseTests.cs` : comparer deux appels Clock à huit périodes d'entrée groupées pour tous les opcodes, avec/sans sorties d'horloge observées, entrées IRQ/NMI/RDY/RESET/BE changeant pendant le bloc ; vérifier traces, états, RAM et périodes. Exécuter ce groupe et les tests Master/Tube existants après le changement.
  - [x] Créer `artifacts/.tmp-tube/turbo-clock-candidate.cs` depuis la candidate vérifiée et `turbo-clock-run.ps1` pour piloter la comparaison ; attendre la fermeture effective de RML sans le fermer ni manipuler sa fenêtre avant les mesures.
  - [x] Créer `artifacts/.tmp-tube/turbo-clock-<0..3>.log` et `.err` par deux paires alternées Debug de carte complète, reconstruites depuis les sources avec les mêmes dépendances ; conserver la candidate seulement si les sorties/cycles restent identiques et le coût global baisse, sinon restaurer TurboBoard depuis sa référence.
  - [x] Supprimer les seuls fichiers temporaires de ce bloc et reconstruire Native sur son Program.cs antérieur après consignation et restauration de la source retenue.

Comparaison en attente de la fermeture de RML (PID 72656 encore présent lors de trois reprises successives). Aucun passage avant/après exécuté. La surveillance possédée a été interrompue et son processus s'est terminé (code 1 après Ctrl+C) ; aucune fermeture ni action sur RML. Les sources de référence/candidate et le banc restent disponibles pour reprendre la comparaison après fermeture utilisateur ; aucun démarrage automatique ne subsiste. Candidate fonctionnellement testée, gain toujours non établi : ne pas livrer ni committer ce changement comme optimisation validée.

Comparaison reprise après fermeture utilisateur : quatre passages terminés avec code 0, invariants identiques. Candidate rejetée (+1,29 à +2,63 %), TurboBoard restauré à sa référence ; tests conservés. Diagnostics supprimés et Native reconstruit sur Program.cs avec code 0.

- [x] Implémenter et vérifier entièrement les contrôleurs et interfaces d'entrée, raccordés aux joysticks existants et à la souris hôte déjà gérée, avant clôture du Master Turbo ; ne pas créer de nouveaux types de souris/joysticks ou pilotes hôtes.
  - [x] Créer `docs/project/emulations/Acorn/BBC Master/peripheral-coverage.md` avec les raccordements matériels identifiés, les fichiers et capacités réellement proposés par RML pour BBC Micro/Master, leurs limites de validation, et les autres extensions à différer ; distinguer prise en charge du port et prise en charge du périphérique, en utilisant les sources acquises. Ajouter les actions concrètes nécessaires après cet inventaire, avant toute modification de code.
  - [x] Créer temporairement `artifacts/.tmp-master-input/TurboInput.ps1` pour les appuis Windows maintenus autorisés, la libération de capture et les trois boutons, avec contrôle du premier plan et relâchement dans finally.
  - [x] Modifier temporairement le choix coprocesseur de `C:/Users/overt/AppData/Roaming/Retro Media Lab/Emulation/Machines/acorn/Configurations/ce6ad37a3a8749ebba0e37584376bc96/machine.json` depuis les paramètres RML (None vers Turbo) ; conserver les autres réglages et redémarrer seulement la machine émulée pour l’essai autorisé.
  - [x] Créer `artifacts/TestsMachines/Acorn/BBC Master Turbo/20261107-rml-turbo-input-*.jpg` depuis le démarrage, un calcul BASIC, les deux symboles hôtes et la lecture OSWORD des boutons AMX après chargement du pilote par le boot existant ; consigner les compatibilités réellement observées sans détourner les mémoires hôte/parasite.
  - [x] Conserver le choix Turbo dans la configuration existante à la demande explicite de l’utilisateur ; ne pas rétablir None après cet essai.
  - [x] Supprimer `artifacts/.tmp-master-input/TurboInput.ps1` après les essais et conserver les seules preuves utiles ignorées par Git.

Essai RML Debug du 7 novembre : calcul BASIC 12+34 = 46, guillemet et exclamation hôtes corrects, trois boutons AMX lus par OSWORD 64 via le Tube (repos 224, gauche 192, milieu 160, droite 96, relâchements 224). Captures consignées dans la fiche Turbo et copies des boutons dans TestsChips/Mos6522. Programme arrêté par Escape, RML laissé ouvert à BASIC ; Turbo conservé à la demande explicite de l’utilisateur. Le raccordement joystick et la cadence Turbo restent à traiter ; ce passage ne clôture pas le groupe complet.

- [x] Vérifier les deux joysticks analogiques sur le Turbo assemblé, sans modifier les réglages utilisateur.
  - [x] Créer temporairement `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornMasterTurboJoystickDiagnostic.cs` : copier la configuration Master en mémoire, conserver ses ROM, assembler le Turbo sans médias ni sortie audio Windows, transmettre des entrées par le projecteur existant puis vérifier les quatre ADVAL et les deux boutons depuis BASIC parasite via le Tube. Conserver les entrées pendant la frappe, produire les champs vidéo natifs sous TestsMachines et TestsChips/Upd7002, nettoyer les entrées et raccordements dans finally.
  - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornMasterTurboJoystickDiagnostic.cs` et ses seules sorties temporaires après consignation, puis reconstruire l’assembly Debug du projet pour retirer ce diagnostic. Conserver les captures demandées ignorées par Git.

Reprise du 8 novembre après fusion : verdict BASIC et deux parités vérifiés, cycles parasite/hôte exactement 2. Debug complet : 27,37 / 26,97 / 27,97 ms par champ au repos/SOUND/défilement BASIC. Budget de 19,97 ms non atteint ; aucune optimisation nouvelle ni clôture globale du Turbo revendiquée. Captures natives conservées, diagnostic et journaux jetables supprimés, Native reconstruit sur Program.cs avec code 0. Conditions et limites dans la fiche de la machine.

## Vérification Release utile à la décision utilisateur — 8 novembre

- [x] Confirmer la marge de cadence du Turbo complet sur le code conservé, sans transformer ce résultat en validation du budget Debug.
  - [x] Créer `artifacts/.tmp-tube/TurboFinalCadence.cs` depuis le banc existant, limité au Turbo : mêmes ROM, commandes BASIC et scénarios repos/SOUND/défilement que les comparaisons Debug de 160 champs ; construire les dépendances Release depuis les sources conservées, vérifier le verdict invité, cycles, PC et empreintes puis consigner les cadences en ms/champ, champs/s, MHz et pourcentage de vitesse réelle. Ne pas lancer RML ou une compilation concurrente pendant la mesure.
  - [x] Supprimer `artifacts/.tmp-tube/TurboFinalCadence.cs` et ses sorties jetables après consignation, reconstruire Native Release sur Program.cs pour retirer ce diagnostic ; conserver les captures natives existantes et ne pas toucher aux réglages utilisateur.

Release du 8 novembre : 4,52/4,74/5,25 ms par champ repos/SOUND/défilement, 190–221 champs/s sans limite et 380–441 % nominal. Verdict BASIC, cycles, PC et empreintes finales concordent avec Debug. Diagnostic retiré, Native Release reconstruit sur Program.cs, code 0. Budget Debug toujours distinct et décision de poursuite demandée à l'utilisateur.

## Corriger les preuves visuelles des positions joystick — 8 novembre

Les anciens fichiers ONE/TWO examinés montrent le texte du programme, pas un verdict après CLS. REST montre bien son verdict exécuté. Ne pas utiliser ONE/TWO comme preuve visuelle d'un PASS ; conserver ces étapes et compléter les captures sans modifier les composants.

- [x] Vérifier visuellement les verdicts réellement exécutés pour les deux positions et le repos sur le Turbo.
  - [x] Créer `artifacts/.tmp-tube/TurboJoystickProof.cs` depuis le banc existant, limité au Turbo et aux ROM existantes ; maintenir deux entrées analogiques normalisées pendant la saisie, exécuter les lectures ADVAL(1..4) et ADVAL(0) via BASIC parasite/Tube, comparer extrema et tirs, puis le repos. Effacer l'écran avant les verdicts, attendre les champs nécessaires et vérifier le texte effectivement affiché. Conserver chaque champ BMP natif sous `artifacts/TestsMachines/Acorn/BBC Master Turbo/20261108-joystick-*.bmp` avec copies sous `artifacts/TestsChips/Upd7002/BBC Master Turbo/` ; nettoyer les entrées et branchements dans finally, sans configuration écrite ni sortie Windows.
  - [x] Supprimer `artifacts/.tmp-tube/TurboJoystickProof.cs` et ses sorties jetables après consignation, reconstruire Native Debug sur Program.cs pour retirer le diagnostic ; garder les captures utiles ignorées par Git.

Preuves complémentaires du 8 novembre : ONE/TWO/REST PASS exécutés puis inspectés dans leurs champs natifs ; quatre axes et tirs corrects, copies machine/puce identiques par SHA256. Diagnostic et journaux jetables supprimés, Native Debug reconstruit sur Program.cs (code 0). La vérification hors RML est acquise ; elle ne ferme pas la validation physique en interface ni le budget Debug.

## Acceptation du bloc Turbo — 8 novembre 2026

L'utilisateur choisit explicitement « Valider si la Release tient la cadence ». La vérification Release du code conservé tient 190–221 champs/s sans limite, soit 380–441 % de la cadence matérielle : critère satisfait. Le bloc Turbo est accepté et la suite est le Master 512. Conserver les mesures et limites Debug (~28 ms/champ), les compatibilités logicielles et la distinction entre entrées normalisées et périphériques physiques ; ne pas présenter cette acceptation comme une tenue de 19,97 ms en Debug ni comme une validation des variantes suivantes. L'utilisateur ne demande pas RML pour ce passage.

- [x] Clôturer et livrer le bloc Turbo accepté avant de commencer le Master 512.
  - [x] Remplacer `build/Debug/Retro Media Lab/retromedialab.exe`, ses bibliothèques et les 24 modules avec `scripts/local-building.cmd --building=debug --modules=A` ; vérifier le code de sortie, l'exécutable et chaque manifeste/DLL d'entrée, sans lancer RML.
  - [x] Modifier `.git/index` pour indexer les suivis et résultats terminés du bloc, sans artefacts ni fichiers ignorés, puis créer le commit de clôture Turbo ; ne pas pousser sans demande.
