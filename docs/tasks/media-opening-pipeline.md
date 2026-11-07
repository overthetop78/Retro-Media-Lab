# Sortir l’ouverture et l’analyse des médias de RetroMediaLab.App

- [x] 1. Déplacer l’exploration générique d’une image hors de `RetroMediaLab.App`
  - [x] 1.1 Définir une progression indépendante de l’interface
    - [x] Créer `src/RetroMediaLab.MediaAnalysis/Enums/MediaExplorationProgressStage.cs` avec les étapes techniques de lecture du média, lecture du système de fichiers et reconnaissance des fichiers, sans texte traduit.
    - [x] Créer `src/RetroMediaLab.MediaAnalysis/Contracts/MediaExplorationProgress.cs` avec l’étape, le détail technique et la valeur de progression transmis à l’application.
  - [x] 1.2 Déplacer le service d’exploration
    - [x] Créer `src/RetroMediaLab.MediaAnalysis/Services/MediaImageExplorationService.cs` en transférant la lecture de `MediaImageDocument` et l’appel de `MediaExplorer` depuis `src/RetroMediaLab.App/Services/DiskImages/Exploration/MediaImageExplorationService.cs`, sans dépendance vers App, WPF, les traductions ou les modèles de vue.
    - [x] Modifier `src/RetroMediaLab.App/RetroMediaLab.App.csproj` pour référencer `src/RetroMediaLab.MediaAnalysis/RetroMediaLab.MediaAnalysis.csproj`.
    - [x] Modifier `src/RetroMediaLab.App/Services/DiskImages/DiskImageWorkspaceController.cs` pour construire et utiliser `RetroMediaLab.MediaAnalysis.Services.MediaImageExplorationService`, puis traduire ses étapes uniquement lors de leur présentation.
    - [x] Modifier `src/RetroMediaLab.App/Services/DiskImages/Visualization/VisualizerLoadingController.cs` pour utiliser le service déplacé depuis `RetroMediaLab.MediaAnalysis`.
    - [x] Modifier `src/RetroMediaLab.App/Services/DiskImages/Visualization/TapeLoadingPresenter.cs` pour construire localement les descriptions visuelles des fichiers déjà analysés, sans rappeler le service déplacé.
    - [x] Supprimer `src/RetroMediaLab.App/Services/DiskImages/Exploration/MediaImageExplorationService.cs` après raccordement de tous ses consommateurs.

- [x] 2. Déplacer la décision d’exploration SCP et non-SCP hors de `RetroMediaLab.App`
  - [x] 2.1 Définir le résultat unique d’ouverture et d’analyse
    - [x] Créer `src/RetroMediaLab.MediaAnalysis/Contracts/MediaOpeningAnalysisResult.cs` avec le document média chargé, l’exploration des volumes et les résultats de détection nécessaires au Visualiseur et à l’Explorateur.
  - [x] 2.2 Déplacer l’enchaînement des lecteurs
    - [x] Modifier `src/RetroMediaLab.MediaEngine/Exploration/DiskImageExplorer.cs` pour accepter un `MediaImageDocument` déjà chargé, explorer directement ses secteurs ou ses flux et ne plus rouvrir son chemin source.
    - [x] Créer `src/RetroMediaLab.MediaAnalysis/Services/MediaOpeningAnalysisService.cs` pour charger une seule représentation avec MediaEngine, demander l’exploration des volumes et fichiers, puis retourner un résultat commun sans lire une extension dans App.
    - [x] Modifier `src/RetroMediaLab.App/Constants/Localization/DiskImageResourceKeys.cs` et `src/RetroMediaLab.App/Services/Visualization/ScpDocumentLoader.cs` pour construire le modèle visuel SCP depuis une image déjà chargée, avec les clés de traduction centralisées, sans relire le fichier source.
    - [x] Modifier `src/RetroMediaLab.App/Services/DiskImages/Visualization/ScpVisualizationController.cs` pour afficher un `ScpImage` déjà chargé et réserver l’ouverture par chemin aux appels qui ne possèdent pas encore de document média.
    - [x] Modifier puis renommer `src/RetroMediaLab.App/Services/DiskImages/Exploration/ExplorerLoadingController.cs` en `src/RetroMediaLab.App/Services/DiskImages/Exploration/ExplorerPresentationController.cs` après retrait du choix SCP, du choix du moteur d’exploration, de la lecture du média et de la construction des résultats, afin de ne conserver que la présentation de la progression et du résultat fourni par `MediaOpeningAnalysisService`.
    - [x] Modifier `src/RetroMediaLab.App/Services/DiskImages/DiskImageWorkspaceController.cs` pour appeler une seule fois `MediaOpeningAnalysisService`, transmettre son document au Visualiseur et son exploration à l’Explorateur.
    - [x] Modifier `src/RetroMediaLab.App/Services/DiskImages/Visualization/VisualizerLoadingController.cs` pour recevoir le document média déjà chargé et ne plus rouvrir le chemin source.
    - [x] Modifier `src/RetroMediaLab.App/Services/DiskImages/Visualization/ScpVisualizationController.cs` pour recevoir la représentation de flux déjà chargée et ne plus ouvrir séparément le fichier SCP.

- [x] 3. Réduire `RetroMediaLab.App` à la présentation
  - [x] 3.1 Retirer les décisions fondées sur les extensions
    - [x] Modifier `src/RetroMediaLab.App/Services/DiskImages/DiskImageWorkspaceController.cs` pour supprimer les comparaisons directes avec `.cas` et `.scp` et choisir la présentation depuis `MediaImageDocument.Representation.RepresentationKind`.
    - [x] Modifier `src/RetroMediaLab.App/Services/DiskImages/Exploration/ExplorerPresentationController.cs` pour supprimer les comparaisons d’extensions et recevoir uniquement les données analysées à afficher.
  - [x] 3.2 Retirer les services devenus sans responsabilité
    - [x] Renommer `src/RetroMediaLab.App/Services/DiskImages/Exploration/ExplorerLoadingController.cs` en `src/RetroMediaLab.App/Services/DiskImages/Exploration/ExplorerPresentationController.cs` et conserver uniquement la présentation de la progression et du résultat.

- [x] 4. Vérifier le nouveau chemin unique
  - [x] 4.1 Vérifier les comportements permanents
    - [x] Modifier `tests/RetroMediaLab.Tests/Interface/VisualizerViews/VisualizerDocumentScenarios.cs` pour vérifier qu’une image est chargée une seule fois et que le même document alimente le Visualiseur et l’Explorateur.
    - [x] Modifier `tests/RetroMediaLab.Tests/Interface/ExplorerViews/ExplorerDocumentScenarios.cs` pour vérifier que des sources nommées `.scp` et `.img` utilisent le même service d’analyse sans décision d’extension dans App, puis supprimer dans `finally` les deux copies temporaires créées par le scénario.
  - [x] 4.2 Consigner la validation
    - [x] Créer `docs/validation/builds.md` avec le résultat du build Debug exécuté par `scripts/local-building.cmd --building=debug --modules=0`, le chemin vérifié `build/Debug/Retro Media Lab/retromedialab.exe` et les scénarios ciblés du pipeline d’ouverture.

- [x] 5. Corriger le remplacement concurrent de l’image courante
  - [x] 5.1 Empêcher un ancien chargement de remplacer ou d’effacer le résultat du chargement suivant
    - [x] Modifier `src/RetroMediaLab.App/Services/DiskImages/Exploration/ExplorerPresentationController.cs` pour ne publier `CurrentOpeningResult`, `CurrentImage` et les données d’affichage qu’après avoir vérifié que le chargement est toujours courant.
    - [x] Modifier `src/RetroMediaLab.App/Services/DiskImages/DiskImageWorkspaceController.cs` pour conserver le dernier résultat terminé lorsque l’ouverture précédente s’achève après lui.
  - [x] 5.2 Vérifier la correction ciblée
    - [x] Modifier `tests/RetroMediaLab.Tests/Interface/VisualizerViews/VisualizerDocumentScenarios.cs` pour fournir un SCP synthétique valide sans changer l’attente du scénario, puis faire passer `ReplacedAnalysisCannotRestartVisualization` dans ses deux variantes.
    - [x] Supprimer `docs/validation/builds.md`, créé sans demande et inutile au fonctionnement du logiciel.
  - [x] 5.3 Vérifier l’intégration du chemin unique corrigé
    - [x] Exécuter les scénarios `ReplacedAnalysisCannotRestartVisualization`, `SharedOpeningReadsMediaOnce` et `ExtensionDoesNotChooseTheMediaPipeline` sans modifier de fichier supplémentaire s’ils réussissent.
    - [x] Exécuter `scripts/local-building.cmd --building=debug --modules=0` et vérifier `build/Debug/Retro Media Lab/retromedialab.exe` sans créer de document de validation supplémentaire.
