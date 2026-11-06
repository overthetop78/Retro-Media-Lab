# BBC Micro Model A — corrections après essais des lecteurs

- [ ] Compléter le diagnostic cassette sans interrompre la reprise en main utilisateur.
  - [x] Conserver les constats du chemin actif avant toute nouvelle correction.
    - [x] Modifier ce document avec le branchement exact de `BbcMicroMachine.cs`, `Cassette.cs` et `Serial.cs`, les consultations restantes et les limites des observations ; ne pas changer les touches, le logiciel livré ou la session utilisateur.

- [ ] Corriger les défauts constatés sans changer les timings matériels.
  - [x] Reprise des médias dans une session déjà ouverte.
    - [x] Modifier `src/RetroMediaLab.App/Services/Emulation/MachineSession.cs` pour reprendre les médias insérés de la configuration actualisée, filtrés par les lecteurs présents, sans recréer une machine allumée pendant la fermeture des réglages.
    - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Emulation/Machine/MachineController.cs` pour transmettre les médias du runtime actualisé à la session.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/EmulationContracts/SessionLifecycleScenarios.cs` pour conserver les scénarios existants avec le contrat actualisé ; compléter `tests/RetroMediaLab.Tests/Application/Emulation/MachineSessionMediaPersistenceTests.cs` avec ajout, remplacement, retrait et filtrage des médias pendant une session ouverte.
    - [x] Modifier ce document avec le résultat du groupe de tests de session après les modifications du bloc.
  - [ ] Son, volume et ralentissements cassette/disquette.
    - [x] Modifier ce document avec le chemin réel du mélange audio, des volumes et des traitements exécutés pendant les transferts ; inscrire les fichiers de correction identifiés avant leur modification.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Common/Services/Machine.cs` et `Machine.Lifecycle.cs` : exposer le volume déjà existant comme option à chaud et appliquer sa valeur au contrôleur audio commun, sans transmettre cette préférence au silicium.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/EmulatorCore.MechanicalSoundSettings.cs` et `EmulatorCore.cs` : annoncer les réglages sonores des lecteurs comme options à chaud, réutiliser leurs constantes et ressources, configurer les mécaniques à la modification effective plutôt qu'à chaque image.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/EmulatorCore.FloppySound.cs`, `EmulatorCore.HardDiskSound.cs`, `EmulatorCore.Disks.cs` et `EmulatorCore.HardDisks.cs` pour configurer les volumes au branchement et lors des changements, sans relire les options à chaque image.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuAudioTests.cs` avec les changements de gain appliqués sur une sortie mémoire et l'exposition des réglages des lecteurs ; aucun firmware ni périphérique externe requis.
    - [x] Modifier `src/RetroMediaLab.Chips/IO/Acorn/SerialUla/BbcSerialUla/Services/BbcSerialUla.Clock.cs` : conserver les intervalles du séparateur entre deux fronts ou échéances, puis appliquer exactement les compteurs écoulés avant un changement d'entrée ; ne changer ni seuil ni horloge.
    - [x] Modifier `tests/RetroMediaLab.Chips.Tests/IO/Acorn/SerialUla/BbcSerialUla/ClockGroupingTests.cs` : comparer les fronts, IRQ indirectes et niveaux avec le chemin unitaire sur une cassette active et aux échéances de silence.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/Common/Services/Cassette.cs` et `Constants/CassetteConstants.cs` : lire le PCM seulement lorsque la tête change d'échantillon, recalculer son incrément seulement aux changements de paramètres ; conserver le front de présentation et le résidu de phase.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuCassetteTests.cs` : comparer chaque niveau et chaque position à l'algorithme unitaire de lecture pour plusieurs vitesses de bande, y compris des échantillons nuls.
  - [x] Lecture des bruitages à la cadence hôte.
    - [x] Modifier `src/RetroMediaLab.MediaEngine/Interfaces/IMediaRuntimePreparation.cs`, `Services/MediaRuntimePreparationService.cs` et `src/RetroMediaLab.App/Services/Audio/MediaDriveAudioController.cs` pour raccorder les mécaniques d'émulation au mélangeur hôte existant ; ne pas arrêter les moteurs commandés par les contrôleurs matériels.
    - [x] Modifier `src/RetroMediaLab.MediaEngine/Interfaces/IMediaDriveMechanics.cs`, `Services/MediaDriveMechanics.cs` et `Services/MediaDriveMechanics.Audio.cs` pour conserver une opération cassette continue et rendre son PCM à la demande de la sortie hôte.
    - [x] Modifier `src/RetroMediaLab.Emulation/Interfaces/IEmulationDriveMechanics.cs`, `Interfaces/IEmulationMediaProcessing.cs` et `Services/EmulationMediaProcessingService.Drives.cs` pour relayer ces commandes et le choix de lecture hôte sans dépendance MediaEngine dans Acorn.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Common/Interfaces/IEmulatorCassetteTransport.cs`, `Emulators/AcornEmu/Machines/BbcMicro/Common/Services/Cassette.cs` et `Emulators/AcornEmu/Services/AnalogCassette.cs` pour notifier les changements d'activité du transport.
    - [x] Modifier les fichiers `EmulatorCore.cs`, `EmulatorCore.TapeSound.cs`, `EmulatorCore.StartupAudio.cs`, `EmulatorCore.Disks.cs`, `EmulatorCore.HardDisks.cs`, `EmulatorCore.FloppySound.cs` et `EmulatorCore.HardDiskSound.cs` dans `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/` pour envoyer les commandes aux mécaniques, arrêter leur mélange dans le PCM du silicium et réinitialiser leurs états à l'arrêt.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuAudioTests.cs` avec l'arrêt, les volumes et la conservation du PCM matériel ; créer `tests/RetroMediaLab.Tests/Application/Emulation/MechanicalAudioPlaybackTests.cs` pour vérifier la lecture hôte et les commandes cassette sans sortie native ; consigner les résultats dans ce document.
  - [x] Corriger l'alimentation de la sortie hôte après le signalement de clics répétés.
    - [x] Modifier `src/RetroMediaLab.App/Constants/Audio/MediaDriveAudioConstants.cs` avec une réserve de fragments audio et un intervalle de réapprovisionnement nommés.
    - [x] Modifier `src/RetroMediaLab.App/Services/Audio/MediaDriveAudioController.cs` pour remplir la réserve selon les frames effectivement consommées, sans ajouter une attente complète après chaque rendu ; conserver une cadence absolue pour les sorties sans mesure de tampon.
    - [x] Modifier `tests/RetroMediaLab.Tests/Application/Emulation/MechanicalAudioPlaybackTests.cs` avec une sortie mémoire consommant un tampon et vérifier son remplissage initial, la borne des écritures, les commandes et la libération ; consigner les résultats dans ce document.
  - [x] Corriger la lecture concurrente du tampon audio signalée par le dispatcher.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/EmulatorCore.cs` : conserver un compteur atomique de frames en attente, actualisé à l'ajout, au retrait et à l'effacement ; ne plus énumérer la file depuis l'interface.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuAudioTests.cs` avec un lecteur concurrent du compteur pendant l'exécution du cœur synthétique, en vérifiant aussi les totaux après retrait et arrêt.
  - [ ] Livraison et vérification réelle groupée.
    - [ ] Modifier `docs/tasks/machines/BbcMicroModelA/validation-rml.md` avec les résultats du build Debug tous modules, de la nouvelle session, des transferts et des réglages audio ; conserver les preuves réelles sous `artifacts/TestsChips/<Chip>/BBC Micro Model A/`.


## Résultats du bloc de reprise des médias

`dotnet test tests/RetroMediaLab.Tests/RetroMediaLab.Tests.csproj -c Debug --no-restore --filter FullyQualifiedName~MachineSessionMediaPersistenceTests` : 14 tests réussis, aucun échec. Ajout, remplacement, filtrage des lecteurs absents et exclusion des médias éjectés sont vérifiés sur session allumée et arrêtée ; les tests de persistance des éjections restent verts. L'application ouverte n'a pas été remplacée ni fermée par cette commande. La machine déjà allumée reste intacte ; le prochain démarrage utilise les médias actualisés.


## Constats audio

Le gain commun est appliqué dans EmulationAudioOutputController.Write, mais la liste Acorn des options à chaud n'expose ni ce volume, ni les volumes mécaniques. Les changements enregistrés pendant une session ne sont donc pas transmis par le filtrage App. Le cœur AcornEmu appelle également Configure sur chaque mécanique à chaque image. Les bruitages sont mélangés dans RunFrame et suivent ainsi la cadence ralentie de la machine. Le découplage doit réutiliser le service audio mécanique existant plutôt que créer un second mélangeur périodique dans les puces.




Validation intermédiaire après correction du volume et du séparateur : 14 tests de session, 116 tests SERPROC et 42 tests audio/cassette Debug réussis. Les tests de gain utilisent les échantillons réellement écrits dans une sortie mémoire : 25 %, zéro et 100 %. Aucun gain global de ms/image n'est déduit de ces tests.

## Bloc audio intégré

Les bruitages cassette, disquette et HDD sont désormais rendus par le mélangeur hôte existant, réveillé par leurs notifications. Ils ne sont plus calculés ni mélangés dans `EmulatorCore.RunFrame`. Le PCM du silicium reste produit par les cycles de la machine, sans changement de fréquence ni optimisation Release en Debug. Les moteurs des mécaniques émulées restent commandés par leurs contrôleurs ; le mélangeur hôte ne les arrête pas comme ceux des outils de fichiers. Les mécaniques sont muettes pendant la restauration initiale et réinitialisées à l'arrêt et à la libération.

Après intégration : 95 tests Debug audio/cassette/contrôleurs Acorn réussis ; 16 tests Debug session/mélangeur hôte réussis. Après l'ajustement des commandes cassette transitoires, les deux tests de mélangeur sont rejoués : deux réussites. Les comparaisons vérifient la continuité du PCM cassette entre fragments, zéro volume, arrêt et libération de la sortie, ainsi que l'absence de bruit mécanique dans le PCM matériel. Les 116 tests SERPROC précédents restent la preuve du regroupement de ses horloges ; aucune nouvelle modification du SERPROC après ce passage.

La cadence complète pendant les transferts et l'écoute effective des sorties Windows restent à observer dans le build livré. Les tests de signaux et de sortie mémoire ne sont pas présentés comme une écoute humaine.

Livraison : premier build Debug All réussi (24 modules). La dernière modification de traitement Pause dans `MediaDriveMechanics.cs` a été écrite après la publication de l'application dans ce passage : les deux tests correspondants passent, mais le SHA256 de MediaEngine dans `build/Debug/Retro Media Lab/lib` diffère encore du binaire source compilé. Ne pas annoncer cette dernière modification livrée. Réaliser la livraison cohérente après la fin de l'essai utilisateur ; ne pas remplacer le build chargé et ne pas fermer sa fenêtre.

## Clics répétés signalés pendant la lecture cassette

L'utilisateur distingue des clics répétés, pas un sifflement de données ni simplement un volume élevé. Le profil reste le même enregistrement `Standard` du magnétophone non identifié. Le mélangeur hôte alimentait la sortie avec 20 ms de PCM puis attendait 20 ms après le calcul, sans réserve ni correction selon la consommation. Ce défaut d'alimentation peut introduire des silences ; ce constat ne prouve pas encore à lui seul l'origine de tous les clics entendus.

Correction : réserve de quatre fragments (80 ms) selon le tampon réellement consommé ; cadence absolue de repli pour les sorties sans compteur. Aucun changement de bande, fréquence du silicium ou profil sonore. Trois tests Debug réussis (331 ms) : sortie hôte indépendante des frames, continuité/volume/arrêt cassette et réserve bornée. Le test garde le consommateur arrêté pendant 60 ms : aucune écriture supplémentaire après remplissage, puis réapprovisionnement après consommation explicite de deux fragments. L'ancienne alimentation continue aurait dépassé cette borne. La sortie mémoire et la tâche sont libérées en fin de test.

Le nouveau correctif attend sa livraison ; demande envoyée à l'utilisateur pour fermer sa session, car le script de build ferme RML. Aucun build ni remplacement de DLL n'est lancé pendant son essai.

Vérification groupée finale avant livraison : 65 tests locaux Debug audio/cassette/mémoire/commandes/console réussis (25 s), dont la lecture concurrente du compteur audio ; 17 tests de session et mélangeur hôte réussis (424 ms). Le compteur UI n'énumère plus Queue. La livraison cohérente doit maintenant remplacer le build antérieur ; l'application a été fermée par l'utilisateur.

## Lecture ciblée du chemin cassette pendant la reprise en main utilisateur

`BbcMicroMachine` choisit soit `_cassette.Tick()`, soit `_serial.Tick()`. Ce sont deux branches exclusives : le chemin cassette appelle déjà `serial.Tick()` une seule fois. Le ralentissement ne peut donc pas être attribué à deux avancements série simultanés dans cette méthode.

Le transport actif est néanmoins traité à chaque cycle de carte : `Cassette.Tick()` consulte deux fois `CassetteActive` (moteur/RS423/état), plusieurs fois la longueur et l'état, puis avance l'accumulateur d'échantillons. Le cache `_presentedPosition` supprime les lectures PCM répétées mais ne supprime pas ce chemin par cycle. Le SERPROC conserve déjà ses intervalles internes dans `_clockCountdown` et `_pendingClockTicks` ; le transport, lui, ne conserve pas encore une échéance de prochain échantillon. Ceci identifie des opérations restantes, sans quantifier leur coût ni affirmer qu'elles expliquent toute la baisse observée.

La seule comparaison complète disponible dans ce passage est celle de la vraie fenêtre : environ 50 FPS au repos et 47–48 FPS pendant `*CAT`. Elle inclut le programme invité différent, la démodulation active, les changements de signaux et la présentation. Aucun gain ni cause unique n'est déduit d'une simple lecture de code. Aucune modification du moteur, aucune compilation et aucune action dans RML pendant la reprise en main utilisateur. Les touches attendent la discussion demandée.
