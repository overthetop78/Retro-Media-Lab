# Six thèmes : Base et cinq interfaces nouvelles

Les commits de thèmes `410ad59e0` et `e6325cc48` ont été retirés de la branche locale sans supprimer le travail. Aucun nouveau commit. Référence conservée : `42a173148`.

- [x] Conserver Base et remplacer les cinq propositions refusées.

  - [x] Modifier `src/RetroMediaLab.Infrastructure/Settings/AppThemeDesign.cs`, `AppSettings.cs` et `src/RetroMediaLab.App/Dictionaries/Theming/ThemeCatalog.cs` : Base, Dock, Flow, Desktop, Instrument et Index ; identifiants numériques stables, design distinct de la variante.

  - [x] Supprimer les définitions et captures de `src/RetroMediaLab.App/Theme/{Graphite,Cobalt,Paper,Phosphor,Prism}` et l’ancienne navigation partagée.

  - [x] Restaurer `src/RetroMediaLab.App/Views/Controls/Common/MainTabHeader.xaml` à sa version avant les thèmes ; les nouveaux thèmes dessinent leurs propres en-têtes.

- [x] Mettre les présentations dans les dossiers des thèmes.

  - [x] Créer `src/RetroMediaLab.App/Theme/Dock/{Workspace,Forms,Window,Layout,Dark}.xaml` : atelier à panneaux ancrés et inspecteur séparé.

  - [x] Créer `src/RetroMediaLab.App/Theme/Flow/{Workspace,Forms,Window,Layout,Light}.xaml` : formulaires organisés en étapes distinctes.

  - [x] Créer `src/RetroMediaLab.App/Theme/Desktop/{Workspace,Forms,Window,Layout,Light,Dark}.xaml` : cadre de document interne et barre des opérations.

  - [x] Créer `src/RetroMediaLab.App/Theme/Instrument/{Workspace,Forms,Window,Layout,Dark}.xaml` : réglages compacts en lignes et cadre de fenêtre avec commandes en bas.

  - [x] Créer `src/RetroMediaLab.App/Theme/Index/{Workspace,Forms,Window,Layout,Light}.xaml` : pages indexées, champs en lignes et cadre latéral.

  - [x] Créer `src/RetroMediaLab.App/Theme/SharedControls.xaml`, `WindowChrome.xaml` et `.xaml.cs` : contrôles, focus, cases, panneaux repliables et commandes système ; chaque thème possède son gabarit de fenêtre.

  - [x] Créer `src/RetroMediaLab.App/Contracts/Theming/ThemeFormContent.cs`, `Functions/Theming/ThemeFormLayout.cs` et `ThemeFieldLayout.cs` : fournir les mêmes contrôles métier aux gabarits, conserver leurs modèles et commandes, restaurer les parents et positions de Base.

  - [x] Modifier `src/RetroMediaLab.App/Services/Theming/ThemeManager.cs`, `Constants/Theming/*`, `Theme/ThemeTokens.xaml`, `App.xaml` et `.xaml.cs` : priorité correcte des styles, application à chaud et restauration du design initial.

  - [x] Modifier `src/RetroMediaLab.App/Views/Windows/Shell/MainWindow.xaml` et ses fichiers Theming, les contrôles Read/Write/Conversion, `Services/Terminal/TerminalPanelController.cs` et les réglages de console : compositions sans duplication des écrans ou commandes ; largeur et hauteur de console indépendantes.

- [x] Choisir et conserver le thème.

  - [x] Créer `src/RetroMediaLab.App/Views/Controls/Options/OptionsThemesSection.xaml` et `.xaml.cs` : liste, aperçu, informations, variantes autorisées et action Choisir ; sélectionner seul ne sauvegarde pas.

  - [x] Modifier `src/RetroMediaLab.App/Views/Windows/Preferences/PreferencesWindow.xaml` et ses fichiers C# : onglet Thèmes, navigation propre au design, sauvegarde explicite et restauration après erreur.

  - [x] Modifier `src/RetroMediaLab.App/Views/Controls/Options/OptionsGeneralSection.xaml`, `.xaml.cs` et `Options/Controllers/GeneralOptionsController.cs` : retirer l’ancien sélecteur sans écraser le nouveau choix.

  - [x] Modifier `src/RetroMediaLab.App/Resources/*/Options.resx` : traductions Argos puis revue des 28 langues ; noms invariants en base commune, aide cohérente avec Choisir.

  - [x] Modifier `src/RetroMediaLab.App/RetroMediaLab.App.csproj` et créer les huit `src/RetroMediaLab.App/Theme/*/*Preview.png` : captures réelles incorporées, au maximum 1600×900.

- [x] Vérifier et nettoyer.

  - [x] Modifier les scénarios existants de `tests/RetroMediaLab.Tests/Interface/SettingsViews/` et créer `Interface/Themes/ThemeDesignTests.cs` : sauvegarde, reprise après erreur et rechargement du couple design/variante dans le stockage mémoire.

  - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryThemeRedesignTests.cs` : parcours des sept fonctions et des étapes Flow, identité des contenus et modèles, retour à Base identique pixel par pixel, aperçus embarqués et véritables dialogues à 1120×650 et 980×560 ; fermeture des fenêtres et libération de leurs handles.

  - [x] Régénérer les aperçus Base avec le style Window de l’application, absent du fixture de test, puis restaurer le style emprunté en finally ; nouveau contrôle de rendu réussi.

  - [x] Supprimer `artifacts/theme-redesign/` et les journaux temporaires de traduction/validation, en conservant les aperçus livrés dans Theme.

- [x] Livrer sans commit.

  - [x] Modifier ce document avec le résultat final du script officiel Debug et vérifier `build/Debug/Retro Media Lab/retromedialab.exe`, `lib/retromedialab.app.dll` et les huit aperçus incorporés.

Validation : 149 contrôles réussis (thèmes, préférences et navigation). Le retour à Base est comparé pixel par pixel avant/après tous les thèmes ; les formulaires conservent les instances métier et le modèle initial. Contrôle supplémentaire réussi après mise en conformité des captures Base avec le style Window réel de l’application. Audit XML : six designs exactement, clés et traductions présentes, anciens noms retirés, noms invariants uniquement en base commune.

Build final réussi (code 0) : `scripts/local-building.cmd --building=debug --modules=0`. Exécutable et DLL vérifiés : `F:\Retro Media Lab\build\Debug\Retro Media Lab\retromedialab.exe` et `lib/retromedialab.app.dll`. Les huit aperçus embarqués correspondent exactement aux PNG sources (comparaison SHA-256). Aucun commit supplémentaire ; la branche reste sur `42a173148`.

# Corrections et séparation thème / déclinaison / style

Demande : corriger tous les défauts constatés sur les vraies fenêtres, différencier réellement les organisations et livrer avec Acorn (build de tous les modules). Les exemples historiques ne sont pas une liste imposée. Thème = organisation ; déclinaison = habillage interne au thème ; style = clair/sombre. Aucun commit.

- [x] Corriger l’application réelle des habillages.

  - [x] Modifier `src/RetroMediaLab.App/Services/Theming/ThemeManager.cs`, `Constants/Theming/ThemeConstants.cs`, `App.xaml` et `App.xaml.cs` : appliquer le thème à chaque fenêtre dérivée lors de son ouverture sans dépendre du style implicite Window ; conserver les surfaces explicitement sans bordure.

  - [x] Créer `src/RetroMediaLab.App/Theme/Base/Inventory.md` : inventaire de toutes les fenêtres, dialogues, vues et des 24 modules ; distinguer interface et rendu matériel après audit des émulateurs.

  - [x] Déplacer `src/RetroMediaLab.App/Views/` vers `src/RetroMediaLab.App/Theme/Base/Views/` : rassembler la présentation d’origine et les interfaces génériques des modules sans modifier leurs namespaces ou leurs commandes.

  - [x] Modifier `src/RetroMediaLab.App/App.xaml`, `Constants/Emulation/EmulationNetworkLayoutConstants.cs`, le dialogue de création de média déplacé et `tests/RetroMediaLab.Tests/Application/TestInfrastructure/StaExecutionScenarios.cs` : corriger les URI des ressources Base déplacées.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Styles/ApplicationStyles.xaml` : boutons et états désactivés lisibles en Base sombre ; fonds et textes cohérents.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Light.xaml`, `Dark.xaml`, `ThemeConstants.cs`, `Services/Theming/ThemeManager.cs` et les vues Base de visualisation/terminal : centraliser les couleurs de surface et de danger en ressources, conserver les couleurs matérielles.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Common/DiskClassificationSelector.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Common/FileEntryIcon.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Common/MediaDocumentIdentity.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Common/MediaRecognitionProgressCard.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Emulation/Input/InputBindingEditor.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Explorer/ExplorerSection.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Options/OptionsGeneralSection.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Options/OptionsHardwareSection.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Tools/ToolsTabSection.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Visualization/BlockMediaView.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Visualization/MediaInspectorPanel.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Visualization/OpticalMediaView.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Visualization/ScpDiskView.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Visualization/SectorMediaView.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Visualization/SequentialMediaView.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Visualization/VisualizerHeaderSection.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Visualization/VisualizerLegend.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Visualization/VisualizerTabSection.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Write/WriteAdvancedSection.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Dialogs/Explorer/ExplorerIssuesWindow.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Windows/About/AboutWindow.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Windows/Tools/GwToolWindow.xaml` : remplacer les pinceaux d’habillage fixes par des ressources sémantiques Base/thème.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryThemeRepairTests.cs` : test de fenêtres dérivées ; première compilation arrêtée par une qualification de namespace à corriger. La demande suivante remplace les cinq thèmes.

# Livraison de Base seule avant tout nouveau thème

Ordre demandé : supprimer les cinq thèmes refusés, terminer Base, tester, construire tous les modules, attendre la validation utilisateur puis seulement committer. Les prochains thèmes seront réalisés un à un après cette validation.

- [x] Retirer les cinq thèmes et leur code de présentation.

  - [x] Supprimer `src/RetroMediaLab.App/Theme/{Dock,Flow,Desktop,Index,Instrument}/` et leurs traductions `Options.Themes.<nom>.*` de `src/RetroMediaLab.App/Resources/*/Options.resx` ; ne garder que Base dans `AppThemeDesign.cs` et `ThemeCatalog.cs`.

  - [x] Modifier `Services/Theming/ThemeManager.cs`, `Constants/Theming/ThemeConstants.cs` et `Contracts/Theming/ThemeDefinition.cs` : conserver le chargement clair/sombre Base et le traitement automatique des fenêtres ; retirer les chargements des thèmes supprimés.

  - [x] Supprimer `Theme/{SharedControls,WindowChrome}.xaml`, `Theme/WindowChrome.xaml.cs`, `Functions/Theming/{ThemeFormLayout,ThemeFieldLayout}.cs`, `Contracts/Theming/ThemeFormContent.cs`, `Constants/Theming/ThemeShellConstants.cs` et les deux fichiers `Theme/Base/Views/Windows/{Shell/MainWindow,Preferences/PreferencesWindow}.Theming.cs` ; retirer leurs appels des vues et restaurer `Services/Terminal/TerminalPanelController.cs` et `Infrastructure/Settings/Window/ConsolePanelSettings.cs` avant ces thèmes.

  - [x] Modifier les tests `Interface/Themes/{ThemeDesignTests,TemporaryThemeRepairTests}.cs`, `Interface/SettingsViews/{SettingsEditingScenarios,SettingsFailureScenarios}.cs` : exercer Base et ses styles, le repli des anciens identifiants, la sauvegarde et l'ouverture de fenêtres sans style imposé par le test.

- [x] Terminer les ressources de Base et vérifier l'affichage.

  - [x] Modifier `Theme/Base/Views/Controls/Common/{MediaRecognitionProgressCard.xaml,FileEntryIcon.xaml.cs}` et `Theme/Base/Views/Windows/About/AboutWindow.xaml` : supprimer les dernières couleurs fixes des surfaces et icônes d'état.

  - [x] Modifier `Theme/Base/Inventory.md` : inventaire de la présentation conservée et résultat détaillé de l'audit des émulateurs ; aucune palette matérielle modifiée.

  - [x] Déplacer `src/RetroMediaLab.App/Functions/Views/` et `Factories/Views/` vers `Theme/Base/{Functions,Factories}/Views/`, ainsi que `Functions/Emulation/Shortcuts/EmulationShortcutViewFunctions.cs` vers `Theme/Base/Functions/Emulation/Shortcuts/` : regrouper aussi les constructeurs de présentation C# sans changer leurs namespaces.

  - [x] Modifier `Theme/Base/Functions/Views/Emulation/Storage/StorageDialogUi.cs`, `Theme/Base/Views/Styles/ApplicationStyles.xaml`, `Theme/ThemeTokens.xaml`, `Theme/Base/Views/Controls/Options/OptionsUpdatesSection.xaml.cs`, ses modèles et son XAML : ressources des panneaux d'aide, boutons du terminal et états de mise à jour ; couleurs dynamiques lors du changement clair/sombre.

  - [x] Modifier `Theme/Base/Functions/Views/Emulation/Settings/{EmulationFirmwareSettingsLayout,EmulationMachineChoiceLayout}.cs`, `ViewModels/Input/InputBindingRow.cs` et `Theme/Base/Views/Controls/Emulation/Input/InputBindingEditor.xaml` : badges et contrôles de configuration utilisant les couleurs dynamiques Base.

  - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Navigation/ControlContractScenarios.cs` : vérifier les états typés des mises à jour, les couleurs appartenant désormais aux vues et non aux modèles.

  - [x] Modifier `Interface/Themes/TemporaryThemeRepairTests.cs` si nécessaire : vérifier les fenêtres réelles Base clair/sombre à taille limitée, produire des captures sous `artifacts/theme-repair/`, fermer et libérer chaque fenêtre dans finally.

  - [x] Modifier `tests/RetroMediaLab.Tests/Interface/SettingsViews/SettingsFailureScenarios.cs` : retrouver l'option sombre courante après le rafraîchissement de la vue, sans conserver une ancienne case retirée de l'arbre.

  - [x] Compléter `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryThemeRepairTests.cs` : captures des sept fonctions, Préférences à 980×560 et changement clair/sombre à chaud ; restaurer les vues empruntées et fermer les fenêtres possédées.

  - [x] Déplacer `Theme/ThemeTokens.xaml` vers `Theme/Base/Tokens.xaml`, modifier `App.xaml` et `Constants/Theming/ThemeConstants.cs` : conserver les ressources de référence dans Base.

  - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/ThemeDesignTests.cs` : garantir le repli vers Base pour chacun des cinq identifiants supprimés et conserver le style enregistré.

  - [x] Modifier `Theme/Base/Inventory.md` : corriger les chemins déplacés et les comptes de fichiers après le retrait des gabarits.

  - [x] Remplacer `Theme/Base/{LightPreview,DarkPreview}.png` par les nouveaux aperçus vérifiés ; supprimer `Interface/Themes/TemporaryThemeRepairTests.cs` et `artifacts/theme-repair/` après validation des tests.

- [x] Livrer Base pour validation, sans commit préalable.

  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuWd1770Tests.cs` : le test conserve une attente antérieure au filtrage demandé ; vérifier le refus du modèle double densité avec 8271 et son acceptation après sélection du WD1770, sans modifier le moteur Acorn inchangé.

  - [x] Modifier ce document avec le résultat des contrôles de navigation, préférences, Base et Acorn, puis du script `scripts/local-building.cmd --building=debug --modules=A` ; vérifier l'exécutable et les 24 modules.

  - [x] Après validation explicite de l'utilisateur : créer le commit Base ; conserver les prochains thèmes en attente jusqu'à cette validation.

## Résultat de la livraison Base seule

- Les cinq thèmes refusés sont supprimés, y compris leurs gabarits, leurs références et leurs traductions. Le dossier Theme contient uniquement Base.

- Base rassemble les fenêtres, dialogues, contrôles et constructeurs de présentation : 64 vues XAML et 377 fichiers C#. Les commandes et moteurs restent inchangés.

- Fenêtres dérivées ouvertes après sélection de Base clair/sombre, changement à chaud et fermeture effective contrôlés sans appliquer manuellement leur style dans le test d'ouverture. Captures des sept fonctions et de Préférences à 1120×650 et 980×560 examinées ; aperçus Base mis à jour.

- 170 contrôles ciblés application réussis, dont quatre tests temporaires de rendu et cycle de vie. 296 contrôles Acorn réussis. Le test WD1770 a été corrigé pour tenir compte du filtrage déjà présent dans le moteur ; aucun changement au moteur Acorn.

- 97 fichiers XML valides ; ressources des cinq thèmes retirées dans toutes les langues. Test temporaire et artefacts de capture supprimés.

- Script officiel terminé avec le code 0 : `scripts/local-building.cmd --building=debug --modules=A`.

- Vérifiés après assemblage : `F:\Retro Media Lab\build\Debug\Retro Media Lab\retromedialab.exe`, `lib/retromedialab.app.dll`, les 24 manifestes et leurs DLL déclarées, dont Acorn et Amstrad. Les versions livrées correspondent aux manifestes sources.

- Aucun commit créé : HEAD reste `42a173148`. Validation utilisateur de Base en attente ; le commit Base puis le prochain thème restent conditionnés à cette validation.

Validation explicite de Base reçue de l'utilisateur le 30 octobre 2026. Le commit Base est maintenant autorisé, suivi du prochain thème seulement.

# Premier nouveau thème : Studio

Base validé et committé : `38ec52d63`. Studio propose un véritable espace de travail à fenêtres internes, avec ses propres cadres, contrôles et styles clair/sombre. Aucun commit Studio avant validation utilisateur. Les déclinaisons restent des choix internes futurs, pas des thèmes supplémentaires.

- [x] Définir Studio et ses ressources.

  - [x] Modifier `src/RetroMediaLab.Infrastructure/Settings/AppThemeDesign.cs`, `src/RetroMediaLab.App/Contracts/Theming/ThemeDefinition.cs`, créer `Contracts/Theming/IThemeWorkspace.cs` et modifier `Dictionaries/Theming/ThemeCatalog.cs` : Studio avec identifiant neuf, fabrique d'espace de travail et styles clair/sombre.

  - [x] Créer `src/RetroMediaLab.App/Theme/Studio/{StudioConstants.cs,Light.xaml,Dark.xaml,Controls.xaml,Window.xaml,StudioCaptionBar.cs}` : dimensions et glyphes nommés, palettes, gabarits des contrôles et cadres de toutes les fenêtres du thème.

  - [x] Modifier `src/RetroMediaLab.App/Resources/*/Options.resx` avec Argos puis revue : description de Studio et commandes Réduire, Agrandir/restaurer ; nom invariant en base commune.

- [x] Construire les fenêtres internes.

  - [x] Créer `src/RetroMediaLab.App/Theme/Studio/{StudioWorkspace.cs,StudioPane.cs}` : conserver les sept vues, déplacer/redimensionner/réduire/restaurer les fenêtres, coordonner la sélection et préserver l'affichage natif des émulateurs ; détacher les événements et restituer les vues dans finally.

  - [x] Créer `src/RetroMediaLab.App/Theme/Base/Views/Windows/Shell/MainWindow.Theming.cs`, modifier `MainWindow.xaml.cs`, `MainWindow.EventsAndCommands.cs` et `Services/Theming/ThemeManager.cs` : installation et libération de l'espace, chargement des ressources Studio, application aux dialogues, restauration exacte de Base.

- [x] Vérifier Studio et livrer pour validation.

  - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/ThemeDesignTests.cs` : persistance de Studio ; créer temporairement `TemporaryStudioTests.cs` : sept fonctions, déplacements, réduction, redimensionnement, changements de thème, fenêtres dérivées et fermeture effective ; produire les captures dans `artifacts/studio-validation/`.

  - [x] Modifier `Theme/Studio/{Controls.xaml,Light.xaml,Dark.xaml,StudioCaptionBar.cs,StudioWorkspace.cs}` : finition des onglets et boutons d'action, commandes des fenêtres modales, activation après réduction et restitution complète même après erreur ; compléter `TemporaryStudioTests.cs` avec les comparaisons Base et les tailles minimales.

  - [x] Modifier `TemporaryStudioTests.cs` : établir la capture Base après application des ressources à la fenêtre du fixture non affichée, comme le fait le chargement réel ; contrôler aussi l'échec de sauvegarde suivi d'une nouvelle tentative Studio.

  - [x] Modifier `Theme/Studio/{Dark.xaml,StudioWorkspace.cs}` et `TemporaryStudioTests.cs` : harmoniser les surfaces sombres et conserver le passage clavier Ctrl+Tab et la sélection externe des fonctions.

  - [x] Modifier `Theme/Studio/{Controls.xaml,StudioConstants.cs}` : conserver le contraste des commandes de titre au survol et une hauteur minimale suffisante ; vérifier les aperçus incorporés dans `TemporaryStudioTests.cs` avant sa suppression.

  - [x] Créer `src/RetroMediaLab.App/Theme/Studio/{LightPreview,DarkPreview}.png` à partir des captures contrôlées ; supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryStudioTests.cs` et `artifacts/studio-validation/`.

  - [x] Modifier ce document avec les résultats des tests et du script officiel Debug `scripts/local-building.cmd --building=debug --modules=A` ; vérifier l'exécutable et les 24 modules et attendre la validation avant tout commit Studio.

## Livraison Studio pour validation

- Base validé et committé dans `38ec52d63` ; aucun commit Studio créé.

- Studio est le seul nouveau thème proposé. Ses styles clair/sombre, palettes, contrôles et cadres sont définis dans `Theme/Studio/`.

- Les sept fonctions restent accessibles ; les fenêtres des outils se déplacent, se redimensionnent, se réduisent et se restaurent, et Ctrl+Tab conserve le passage entre les fonctions. Les vues et commandes existantes sont conservées.

- L'émulation utilise l'espace central lorsqu'elle est active afin que les HWND vidéo natifs ne recouvrent pas les fenêtres WPF. Les autres outils restent ouverts et retrouvent leur position au retour.

- 155 contrôles ciblés réussis : navigation, préférences, persistance et Studio. Les tests temporaires ont contrôlé les sept vues empruntées, les interactions, le retour à Base identique pixel par pixel, les tailles réduites, l'échec de sauvegarde suivi d'une nouvelle tentative et la destruction des handles des fenêtres dérivées.

- Deux aperçus réels 1360×900 incorporés et vérifiés ; traductions Argos revues dans toutes les langues. Le nom invariant Studio reste dans la base commune. Tests et captures temporaires supprimés.

- Build officiel terminé avec le code 0 : `scripts/local-building.cmd --building=debug --modules=A`. Exécutable `F:\Retro Media Lab\build\Debug\Retro Media Lab\retromedialab.exe`, DLL application et 24 manifestes/DLL de modules vérifiés après assemblage, dont Acorn et Amstrad.

- Studio attend la validation utilisateur avant son commit et avant le thème suivant.

Studio validé par l'utilisateur le 30 octobre 2026 : commit autorisé avant le thème suivant.

# Reconstruction complète après refus des thèmes — 31 octobre 2026

Objectif actif : recommencer Orbit, OS, Matrix et Atlas intégralement. Base et Studio validés restent conservés. Les nouvelles présentations doivent reconstruire leurs menus, formulaires, dialogues, journaux et états ; déplacer ou repeindre les blocs Base ne suffit pas. Aucun commit sans validation. Les déclinaisons OS doivent reproduire les conventions et interactions du système concerné, avec références et comparaison visuelle.

- [x] Établir les surfaces et le raccordement fonctionnel.

  - [x] Créer `artifacts/themes-rebuild/surface-inventory.json` : lister toutes les vues XAML, leurs contrôles nommés et événements, les fenêtres et surfaces créées en code dans App et les modules ; produire les éléments nécessaires au contrôle des omissions.

  - [x] Créer `docs/design/themes-rebuild.md` : exigences par thème, inventaire des fonctions et dialogues, références historiques primaires, comportement attendu et preuves nécessaires ; préciser la séparation entre commandes métier conservées et arbres visuels reconstruits.

  - [x] Créer `src/RetroMediaLab.App/Contracts/Theming/{ThemeElementRole.cs,ThemeElementSlot.cs}` et `src/RetroMediaLab.App/Theme/Common/{ThemeElementLease.cs,ThemeElementHost.cs}` : permettre aux nouvelles vues de disposer les éléments fonctionnels et surfaces de rendu, sans reprendre les UserControl, cartes ou grilles de Base ; préserver données, disponibilité, visibilité métier et disposition d’origine lors du détachement.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/ThemeElementLeaseTests.cs` : vérifier des interactions fonctionnelles, les changements de données et de disponibilité, les rejets des blocs visuels et la restitution après erreur, sans fichier externe ni cœur d’émulation.

  - [x] Modifier `Theme/Common/ThemeElementLease.cs` et `ThemeElementLeaseTests.cs` : contrôler une sélection à liaison bidirectionnelle pendant le changement de parent et préserver sans interruption son contexte si nécessaire ; confirmer le retour des ressources dynamiques et l’absence de régression Base/Studio.

- [x] Rendre l’historique de lecture présentable sans les cartes Base.

  - [x] Créer `src/RetroMediaLab.App/ViewModels/Operations/ReadCaptureViewModel.cs`, modifier `Theme/Base/Views/Controls/Read/{ReadCaptureCard.xaml.cs,ReadCompletionBanner.xaml.cs}` : exposer une collection observable en lecture seule des captures et leurs états, ainsi que les actions existantes de consultation ; conserver le rendu et les appels de Base.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/ReadViews/ReadCaptureHistoryTests.cs` : vérifier l’historique dynamique, les notifications de fin/archivage/suppression et les actions, sans fichier externe ; compléter ce suivi avec les contrôles ciblés Base/Studio et lecture.

- [x] Raccorder des contrôles nouvellement créés aux fonctions existantes.

  - [x] Créer `src/RetroMediaLab.App/Theme/Common/ThemeBindingScope.cs` : synchroniser des champs nouveaux avec les données/sélections existantes sans remplacer les liaisons d’origine, relayer les actions et leur disponibilité ; supprimer les abonnements dans finally à la sortie de la présentation.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/ThemeBindingScopeTests.cs` : vérifier les saisies, sélections interdépendantes, actions, erreurs et désabonnements, sans fichier externe ; confirmer les contrôles déjà réalisés.

- [ ] Reconstruire OS à partir des systèmes historiques.

  - [x] Compléter `docs/design/themes-rebuild.md` avec la composition du premier écran et créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/AcquisitionView.xaml` et `AcquisitionView.xaml.cs` : créer les contrôles de lecture et l’historique dans un nouvel arbre visuel, sans déplacement des blocs ni champs Base ; conserver toutes les entrées/sélections/actions de la lecture ; l’intégration du cadre historique et des options avancées reste une étape distincte avant mise à disposition du thème.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31AcquisitionViewTests.cs` : contrôler que les entrées et les actions sont des contrôles nouveaux, que les éléments Base restent dans leurs parents, que la lecture et la consultation utilisent les commandes existantes et que les abonnements sont supprimés à la fermeture.

  - [x] Modifier `Theme/Common/{ThemeBindingScope.cs,ThemeElementLease.cs}` et `ThemeBindingScopeTests.cs` : interdire aussi la copie ultérieure d’un contenu visuel dans une action et poursuivre tous les détachements/restaurations après une erreur ; reprendre les contrôles concernés.

  - [x] Créer `artifacts/themes-rebuild/typography.json` et modifier `docs/design/themes-rebuild.md` : relever les ressources bitmap historiques réellement installées (faces, tailles, métriques, empreintes), distinguer leur disponibilité de la preuve de fidélité et préparer les cadres/interactions sans substituer arbitrairement des polices modernes.

  - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/{AcquisitionOptionsView.cs,AcquisitionOptionsFields.cs}` : reconstruire toutes les options de lecture dans un formulaire indépendant compact, synchroniser les champs et choix sans reprendre les conteneurs Base ; détacher toutes les liaisons à la fermeture.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31AcquisitionOptionsTests.cs` : vérifier les options numériques, les choix traduits, les notifications métier, l’exclusivité existante et la libération sans dépendance externe ; inscrire les résultats dans ce suivi.

  - [x] Modifier `docs/design/themes-rebuild.md` et ce suivi avec les chemins exacts des autres vues, dialogues, cadres, menus et déclinaisons OS avant leur implémentation ; comparer chaque vue au système de référence, y compris menus système, réduction en icône, activation, clavier, polices et contrôles.

  - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/{Windows31WindowState.cs,Windows31WindowPlacement.cs}` : transitions normal/agrandi/icône/fermé, géométrie de restauration, activation et contraintes de déplacement ; aucune réduction déléguée à la barre des tâches moderne.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31WindowPlacementTests.cs` : contrôler restauration après réduction/agrandissement, bornes de déplacement, redimensionnement et fermeture terminale ; exécuter les contrôles ciblés et inscrire les résultats ici.

  - [x] Modifier ce suivi avec les actions concrètes de création des cadres, contrôles, bureau et gestionnaire de programmes nommés dans `docs/design/themes-rebuild.md`, avant de les implémenter.

  - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/{Windows31Metrics.cs,Windows31Frame.xaml,Windows31Frame.xaml.cs}` : cadre indépendant avec titre centré, case système, réduction en icône, agrandissement/restauration et activation ; raccorder le placement et les raccourcis, détacher le contenu emprunté et les événements dans finally. Les menus et interactions de déplacement restent à compléter avant livraison.

  - [x] Modifier `Theme/OS/Editions/Windows31/Windows31Metrics.cs` : rendre les membres utilisés par x:Static accessibles au chargeur XAML ; corriger l’erreur constatée dans le nouveau cadre avant de reprendre ses tests.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31FrameTests.cs` : vérifier commandes, état actif/inactif et restitution du contenu après fermeture sans fenêtre native ni fichier externe.

  - [x] Modifier `Theme/OS/Editions/Windows31/Windows31Metrics.cs` et `docs/design/themes-rebuild.md` : définir le gris VGA de référence explicitement plutôt qu’utiliser Colors.LightGray, dont la valeur moderne diffère ; conserver la référence Microsoft de palette.

  - [x] Construire les premiers contrôles OS et leur raccordement au cadre.

    - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Controls.xaml` : gabarits indépendants des boutons, cases, choix, champs, listes, groupes et menus ; couleurs locales sans réutilisation des styles Base.

    - [x] Modifier `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Frame.xaml` : charger les contrôles de sa déclinaison dans ses ressources locales sans modifier Base/Studio.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31ControlsTests.cs` : charger les gabarits dans une surface WPF, vérifier leur instanciation et les états sélectionnés/désactivés ; détacher l’arbre visuel dans finally sans fenêtre externe.

  - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31VisualTests.cs` : rendre les formulaires d’acquisition et d’options avec le cadre et les contrôles OS dans `artifacts/themes-rebuild/visual-check/`, libérer les arbres dans finally et vérifier les images réelles avant de poursuivre les gabarits.

  - [x] Modifier `Theme/OS/Editions/Windows31/Views/AcquisitionView.xaml` : laisser les commandes dimensionner leur colonne plutôt qu’imposer 110 pixels, responsable de la troncature constatée de « Paramètres avancés » ; rendre de nouveau les deux formulaires pour contrôler cette correction.

  - [x] Supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31VisualTests.cs` et `artifacts/themes-rebuild/visual-check/` après vérification ; inscrire les constats et les corrections nécessaires dans ce suivi avant de les réaliser.

  - [x] Compléter les familles de contrôles OS.

    - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Controls/{ScrollControls.xaml,NavigationControls.xaml}` et modifier `Windows31Controls.xaml` : gabarits locaux des barres horizontales/verticales, curseurs, onglets et arbres ; aucune dépendance aux modèles visuels de Base.

    - [x] Modifier `Controls/ScrollControls.xaml` : utiliser les commandes publiques réelles Slider.DecreaseLarge et Slider.IncreaseLarge ; corriger les noms invalides détectés à la compilation avant de reprendre les contrôles.

    - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/Windows31ControlsTests.cs` : vérifier les parties Track, défilement, sélection, expansion et les deux orientations avec des éléments WPF autonomes ; inscrire les résultats dans ce suivi.

    - [x] Modifier `docs/design/themes-rebuild.md` et ce suivi : définir les fichiers précis du texte bitmap et les captures de comparaison avant leur réalisation.

  - [ ] Construire le bureau et les applications OS.

    - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31ApplicationWindow.cs` : héberger un document emprunté dans une fenêtre native à cadre OS, synchroniser position/état, masquer à la réduction, fermer sur état terminal et détacher le document dans finally ; attendre la disparition effective du handle à la libération.

    - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31WindowTests.cs` : typer explicitement l’action synchrone de l’assertion d’erreur pour que xUnit ne choisisse pas sa surcharge asynchrone ; reprendre la vérification des fenêtres.

    - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31ScreenshotTests.cs` : rendre les fenêtres OS courantes dans `artifacts/themes-rebuild/current/{acquisition,options}.png` pour la demande de capture utilisateur, sans retouche ; fermer les fenêtres dans finally et vérifier leurs handles.

    - [x] Supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31ScreenshotTests.cs` après production et inspection ; conserver les deux PNG demandés par l’utilisateur.

    - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31WindowTests.cs` : vérifier fenêtre native, réduction/restauration, fermeture après erreur et disparition des handles avec des documents synthétiques ; supprimer le test et ses artefacts après utilisation.

    - [x] Modifier `Resources/*/Options.resx` avec Argos puis revue : libellés du menu système et du gestionnaire ; valeurs invariantes dans la base commune, sans texte traduit en dur.

    - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/{Windows31ApplicationId.cs,Windows31ApplicationDefinition.cs,Windows31Document.cs}` et modifier `Windows31Metrics.cs` : déclarer les applications/groupes OS, les fabriques de présentations, leur propriété explicite et les métriques du bureau, sans nouvelle logique métier.

    - [x] Modifier `docs/design/themes-rebuild.md` et ce suivi avec les références utilisateur : bureau et fenêtres internes, groupes d’icônes, double-clic, menus propres aux outils, agrandissement dans la zone de l’application ; reprendre la disposition et la fidélité visuelle signalées comme insuffisantes.

    - [x] Supprimer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31ApplicationWindow.cs` : retirer l’hébergement autonome hors du bureau, incompatible avec la précision utilisateur ; le remplacement sera interne à l’application.

    - [x] Modifier `Windows31Metrics.cs` : fond de bureau gris, tailles du gestionnaire, groupes et icônes cohérentes avec les références fournies ; ajouter les constantes de ressources nécessaires à la composition.

    - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/{Windows31Desktop.cs,Windows31ProgramManager.xaml,Windows31ProgramManager.xaml.cs}` : groupes d’applications, ouverture unique par identifiant, icônes réduites et activation ; fermer les fenêtres et libérer uniquement les présentations créées par la fabrique dans finally.

    - [x] Modifier `Windows31Frame.xaml.cs`, `Windows31Desktop.cs` et `Windows31ApplicationId.cs` : rendre la fermeture du gestionnaire annulable par l’application, permettre sa restauration depuis une icône et réactiver une fenêtre visible après fermeture d’un outil ; préserver les documents empruntés.

    - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31DesktopTests.cs` : vérifier ouverture unique, icônes, groupes et agrandissement interne, produire `artifacts/themes-rebuild/desktop-check/desktop.png` et inspecter le rendu.

    - [x] Modifier `Controls/ScrollControls.xaml` : propager le fond du ScrollViewer à sa racine visuelle, car le groupe d’icônes reste gris malgré son fond blanc ; rendre de nouveau le bureau et contrôler la correction.

    - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31DesktopTests.cs` : conserver les trois scénarios autonomes d’ouverture, de navigation et de propriété, sans création de fichiers ni fenêtre native ; supprimer le scénario de capture temporaire après inspection.

    - [x] Supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31DesktopTests.cs` et `artifacts/themes-rebuild/desktop-check/` après inspection ; conserver les captures demandées précédemment dans `artifacts/themes-rebuild/current/`.

    - [x] Créer les fichiers de `fontTools` dans le site Python utilisateur (`C:/Users/overt/AppData/Roaming/Python/Python311/site-packages/fontTools/`) avec pip, après autorisation utilisateur explicite, pour produire les ressources de police du thème.

    - [x] Créer `scripts/tools/build-windows31-fonts.py`, les fichiers `Theme/OS/Editions/Windows31/Assets/Fonts/*.ttf` et `Windows31Typography.xaml` ; modifier `RetroMediaLab.App.csproj`, `Windows31Frame.xaml`, `Windows31Controls.xaml`, `Windows31Desktop.cs` et `Windows31Metrics.cs` : convertir les glyphes bitmap installés en contours alignés sur leurs pixels, conserver leurs métriques, déclarer les ressources et le repli pour les alphabets absents ; vérifier le rendu sans prétendre encore à une identité avec les disquettes d’origine.

    - [x] Modifier `scripts/tools/build-windows31-fonts.py` : fusionner les pixels adjacents en contours de glyphes sans arêtes partagées, car le rendu WPF des petits caractères perd des traits ; vérifier les contours rechargés contre les masques sources, puis rendre le bureau à nouveau.

    - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31TypographyTests.cs` : produire `artifacts/themes-rebuild/typography-debug.png` pour comparer le texte WPF à la géométrie des glyphes, identifier les traits manquants et supprimer cette image de diagnostic après correction.

    - [x] Modifier `Windows31Frame.xaml` et `Windows31Desktop.cs` : retirer le mode Display ajouté au thème, qui altère les glyphes fins convertis ; utiliser Ideal avec leurs métriques entières et rendre de nouveau le bureau pour vérifier les lettres.

    - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31TypographyTests.cs` : vérifier les familles incorporées, les métriques et les glyphes, rendre le bureau avec les nouvelles polices dans `artifacts/themes-rebuild/current/windows31-desktop.png` et inspecter l’image ; supprimer le test après utilisation en conservant la capture de progression pour l’utilisateur.

    - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31TypographyTests.cs` avec le contrôle autonome des familles incorporées ; supprimer `TemporaryWindows31TypographyTests.cs` et `artifacts/themes-rebuild/typography-debug.png`, conserver la capture de progression du bureau.

    - [x] Créer `artifacts/themes-rebuild/windows31-source/PCDOS330-WIN310-VGA.json` depuis l’installation Windows 3.1 archivée sur PCjs, puis `scripts/tools/extract-windows31-resources.py` : extraire sans exécution les exécutables et polices nécessaires, leurs groupes d’icônes et les ICO ; documenter les sources et empreintes dans `docs/design/themes-rebuild.md` ; supprimer le cache d’installation après extraction et vérification.

    - [x] Modifier `scripts/tools/build-windows31-fonts.py` pour exiger explicitement le dossier source, puis régénérer `Assets/Fonts/*.ttf` depuis les FON Windows 3.1 extraites de l’installation de 1992 ; conserver le rapport dans `artifacts/themes-rebuild/typography-original-conversion.json` et les références dans `docs/design/themes-rebuild.md`.

    - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31IconTests.cs` : afficher les ressources ICO originales dans `artifacts/themes-rebuild/icon-preview.png`, choisir les icônes adaptées aux fonctions et vérifier transparence et palette sans redessiner leurs pixels.

    - [x] Créer `Theme/OS/Editions/Windows31/Assets/Icons/*.ico` à partir de ressources Windows 3.1 identifiées ; créer `Windows31Icons.xaml` et modifier `Windows31ProgramManager.xaml`, `Windows31ProgramManager.xaml.cs`, `Windows31Metrics.cs`, `RetroMediaLab.App.csproj` : attribuer une icône d’époque distincte à chaque outil et une icône de groupe appropriée ; supprimer l’image générique répétée et documenter les sources dans `docs/design/themes-rebuild.md`.

    - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31IconTests.cs` : vérifier les dix ressources incorporées, leurs dimensions et transparence, ainsi que neuf images d’outils distinctes ; compléter le scénario temporaire avec un rendu du bureau dans `artifacts/themes-rebuild/current/windows31-desktop.png` pour inspection avant suppression du cache.

    - [x] Supprimer `TemporaryWindows31IconTests.cs`, `artifacts/themes-rebuild/icon-preview.png` et `artifacts/themes-rebuild/windows31-source/` après copie des seules icônes retenues et des polices converties ; conserver le manifeste des ressources retenues et le rendu du bureau dans `artifacts/themes-rebuild/current/`.

    - [x] Compléter les interactions du cadre Windows 3.1.

      - [x] Modifier `Windows31WindowPlacement.cs`, `Windows31Metrics.cs` et créer `Windows31ResizeEdges.cs` : calculer des géométries de déplacement et redimensionnement contraintes, sans déplacer réellement le contenu pendant la prévisualisation ; appliquer atomiquement la géométrie validée.

      - [x] Créer `Windows31SystemMenu.cs` : menu système interne au Canvas, libellés traduits existants, disponibilité des commandes selon l’état, navigation clavier et fermeture extérieure sans fenêtre native supplémentaire.

      - [x] Créer `Windows31DragOutline.cs`, `Windows31Frame.Interactions.cs` et modifier `Windows31Frame.xaml`, `Windows31Frame.xaml.cs` : déplacement par titre et redimensionnement par bord, contour interne, commandes clavier et annulation ; détacher et libérer menus et contours dans finally, préserver le document emprunté.

      - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/{Windows31WindowPlacementTests.cs,Windows31FrameTests.cs}` et créer `Windows31SystemMenuTests.cs` : vérifier géométries ancrées, disponibilité des commandes, validation/annulation et suppression des surfaces possédées, avec des arbres WPF autonomes ; inscrire les résultats dans ce suivi.

    - [x] Conserver tous les moteurs et surfaces des modules dans le bureau interne.

      - [x] Modifier `docs/design/themes-rebuild.md` : remplacer la création de fenêtres natives de remplacement par la conservation des HwndHost existants et la limitation de leurs régions visibles selon les fenêtres superposées ; documenter les API et leur propriété, sans dépendance à Acorn.

      - [x] Créer `src/RetroMediaLab.App/Theme/Common/ThemeNativeWindowRegion.cs` : emprunter le HWND existant, conserver sa région initiale, appliquer les intersections et exclusions, restaurer la région à la sortie et libérer seulement les régions possédées dans finally.

      - [x] Créer `src/RetroMediaLab.App/Theme/Common/ThemeNativeWorkspaceHost.cs` : suivre les HwndHost visibles de tout module dans le Canvas, mettre à jour les régions selon les couches, le défilement et le DPI ; observer le clic natif avant son traitement sans l’intercepter ni remplacer le clavier/souris existants.

      - [x] Modifier `Windows31Desktop.cs` et `Windows31DragOutline.cs` : raccorder le service commun à l’activation des applications et représenter le contour comme bord creux, sans masquer son intérieur.

      - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryThemeNativeWorkspaceTests.cs` : vérifier avec des HWND synthétiques les exclusions, régions originales, changement de couche, réduction et restitution sans destruction du HWND emprunté ; fermer les fenêtres et attendre leur destruction dans finally.

      - [x] Supprimer `TemporaryThemeNativeWorkspaceTests.cs` et tout artefact temporaire après contrôle ; inscrire les résultats et limites dans ce suivi avant de proposer OS.

    - [x] Reconstruire l’application d’écriture Windows 3.1 et son formulaire d’options.

      - [x] Modifier `Theme/Common/ThemeBindingScope.cs` et `tests/RetroMediaLab.Tests/Interface/Themes/ThemeBindingScopeTests.cs` : relayer les mêmes actions métier vers des MenuItem nouveaux, avec disponibilité et légende scalaire, sans transférer de contenu visuel Base.

      - [x] Créer `Theme/OS/Editions/Windows31/Views/{WriteView.xaml,WriteView.xaml.cs}` : menus propres Fichier/Options, source, format détecté et choix explicite existant, profils et état matériel ; créer tous les contrôles et conserver les commandes d’écriture, consultation et modification du format.

      - [x] Créer `Theme/OS/Editions/Windows31/Views/{WriteOptionsFields.cs,WriteOptionsView.cs}` : paramètres complets d’écriture dans un formulaire indépendant, champs reliés aux événements existants et désabonnement dans finally.

      - [x] Modifier `WriteOptionsView.cs` : qualifier la liste WriteOptionsFields.Options pour résoudre le conflit avec l’espace de noms App.Options détecté à la compilation.

      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31WriteViewTests.cs` : vérifier menus et entrées neufs, propagation des données/options et des exclusivités métier, disponibilité et détachement ; inscrire les résultats ici.

      - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31WriteVisualTests.cs` : rendre et inspecter `artifacts/themes-rebuild/current/{write,write-options}.png` sans retouche, puis supprimer le test ; conserver les captures de progression demandées pour les thèmes.

    - [x] Synchroniser les régions avec le placement natif réel.

      - [x] Modifier `Theme/Common/ThemeNativeWorkspaceHost.cs` : reporter la mise à jour automatique après les gestionnaires LayoutUpdated des HwndHost, coalescer les demandes et annuler l’opération possédée au déchargement/à la destruction.

      - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryThemeNativePlacementTests.cs` : vérifier un déplacement automatique lorsque le service a été créé avant le HWND, puis sa fermeture avec une mise à jour en attente ; détruire les fenêtres dans finally et confirmer leurs handles.

      - [x] Supprimer `TemporaryThemeNativePlacementTests.cs` après contrôle et inscrire le résultat dans ce suivi.

    - [x] Reconstruire l’application de conversion Windows 3.1.

      - [x] Modifier `Theme/Base/Views/Controls/Conversion/{ConversionFormatsSection.xaml.cs,ConversionTabSection.xaml.cs,ConversionAdvancedSection.xaml}` et `Controllers/MainWindow/ConversionTabController.cs` : exposer les points de sélection existants, transmettre la présentation de format déjà existante au contrôleur plutôt qu’un contrôle visuel, nommer les entrées avancées et exposer la commande de migration sans modifier leur comportement.

      - [x] Créer `Theme/OS/Editions/Windows31/Views/{ConversionView.xaml,ConversionView.xaml.cs,ConversionFormatRow.cs}` : menus Fichier/Options, source/sorties/profil, familles et liste de formats avec extensions, sélection globale et actions ; construire les contrôles nouveaux et réutiliser les événements métier existants.

      - [x] Créer `Theme/OS/Editions/Windows31/Views/Windows31BoundFields.cs` et modifier `AcquisitionOptionsView.cs`, `WriteOptionsView.cs` : partager la construction des champs indépendants et leurs liaisons au sein de la déclinaison OS, sans recopier les conteneurs Base ni dupliquer les branchements des options.

      - [x] Créer `Theme/OS/Editions/Windows31/Views/{ConversionOptionsFields.cs,ConversionOptionsView.cs}` : reconstruire toutes les options de conversion dans un formulaire compact relié aux entrées existantes ; libérer les abonnements dans finally.

      - [x] Modifier `Theme/Common/ThemeBindingScope.cs` et `ThemeBindingScopeTests.cs` : ne demander le descripteur de la propriété cible que pour une liaison bidirectionnelle ; corriger l’échec de la propriété de présentation interne constaté pendant les tests de conversion.

      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31ConversionViewTests.cs` et modifier le suivi : vérifier le choix des formats/extensions avec le modèle existant, les refus incompatibles, les menus et options, la restitution et la non-régression des vues de conversion Base.

      - [x] Vérifier et corriger le rendu de la conversion.

        - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31ConversionVisualTests.cs` et `artifacts/themes-rebuild/current/{conversion,conversion-options}.png` : rendre les formulaires français, inspecter leur disposition et relever les défauts dans ce suivi.

        - [x] Modifier `ConversionView.xaml` : employer le libellé existant Read.Extension pour le panneau des extensions, à la place de Classification.Format mal traduit et inadapté à ce panneau.

        - [x] Modifier `scripts/tools/build-windows31-fonts.py`, `Assets/Fonts/*.ttf` et `Windows31TypographyTests.cs` : ne pas déclarer les caractères C1 qui ne sont que le glyphe manquant de la police source ; actualiser uniquement les tables Unicode des polices incorporées, conserver glyphes/métriques et produire `artifacts/themes-rebuild/typography-unicode-map.json`.

        - [x] Modifier `Resources/*/Read.resx` avec Argos : traduire Read.Extension avec le contexte File extension dans toutes les langues et revoir les résultats, le libellé français existant Prolongation étant incorrect.

        - [x] Modifier `ConversionView.xaml` : ancrer profil et commande hors du défilement, et donner une hauteur contrainte aux listes pour conserver les actions visibles à 600×480.

        - [x] Modifier `TemporaryWindows31ConversionVisualTests.cs` et les captures de conversion : régénérer les images après correction, ajouter `conversion-narrow.png` à 600×480, vérifier le texte et la disposition, puis supprimer le test temporaire.

        - [x] Modifier `docs/design/themes-rebuild.md` : documenter la correction du répertoire Unicode et le maintien exact des glyphes et métriques d’origine.

    - [ ] Reconstruire la consultation des médias Windows 3.1.

      - [x] Créer `Theme/OS/Editions/Windows31/Views/{InspectorView.xaml,InspectorView.xaml.cs}` : nouvelle fenêtre de consultation de tous les champs du modèle MediaInspectorModel existant, choix de surface et mise à jour après sélection, sans reprendre les panneaux Base.

      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31InspectorViewTests.cs` : vérifier les données et unités complètes, le changement de surface et de modèle, la disponibilité et le détachement des abonnements.

      - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31InspectorVisualTests.cs` : rendre et inspecter `artifacts/themes-rebuild/current/inspector.png`, puis supprimer le test temporaire en conservant la capture de progression.

      - [x] Modifier `Theme/Base/Views/Controls/Visualization/{VisualizerTabSection.xaml.cs,VisualizerHeaderSection.xaml.cs,VisualizerTrackOverview.xaml.cs,VisualizerLegend.xaml.cs}` : exposer les contrôles fonctionnels, vues enregistrées et données de progression existants, notifier les changements de présentation et fournir la même requête de sélection depuis les nouvelles vues.

      - [x] Modifier `SequentialMediaView.xaml.cs` : permettre un viewport de présentation emprunté, conserver les coordonnées et commandes de zoom/défilement, annuler les repositionnements différés lors du changement de viewport sans détruire celui fourni par l’appelant.

      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/MediaVisualizationPresentationTests.cs` : vérifier les notifications après enregistrement/chargement, les données de progression et le routage séquentiel, puis le viewport emprunté, son zoom et l’annulation de ses opérations différées ; aucun fichier externe ni fenêtre native requis.

      - [x] Créer `Theme/Common/MediaVisualizationElements.cs` et modifier les noms des commandes dans les XAML des vues média : identifier les SKElement purs et leurs commandes existantes pour les cinq représentations ; conserver les panneaux de Base et gérer explicitement le viewport séquentiel.

      - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/MediaVisualizationPresentationTests.cs` : vérifier pour les cinq représentations que seules les surfaces Skia sont empruntées, que les commandes sont celles d’origine et que chaque surface retrouve son parent après restitution ; exécuter aussi les contrôles existants de visualisation.

      - [x] Créer `Theme/OS/Editions/Windows31/Views/{VisualizationView.xaml,VisualizationView.xaml.cs,VisualizationOverviewView.cs}` et modifier `Theme/Base/Views/Controls/{Common/MediaRecognitionProgressCard.xaml.cs,Visualization/VisualizerTabSection.xaml.cs}` pour exposer l’état indéterminé de la progression : menus, sélecteurs, nouvelles commandes et progression, hôtes neufs empruntant seulement les surfaces de dessin, restitution complète dans finally ; afficher les cinq représentations et leur sélection avec les modèles existants.

      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31VisualizationViewTests.cs` : vérifier les menus et actions, les cinq représentations et leurs parents restaurés, les options de géométrie et le viewport tape, les données d’ensemble et les abonnements après destruction.

      - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31VisualizationVisualTests.cs` : rendre les secteurs et le mode étroit dans `artifacts/themes-rebuild/current/{visualization,visualization-narrow}.png`, inspecter et corriger les débordements ; supprimer ce test après usage et conserver les captures de progression.

      - [x] Modifier `VisualizationView.xaml.cs` et `Windows31VisualizationViewTests.cs` : annuler la largeur des colonnes de surfaces indisponibles pour qu’un média simple face utilise tout l’espace ; vérifier les transitions simple/double face et le maintien du mode géométrique.

      - [x] Modifier `docs/tasks/application-themes.md` après exécution des contrôles de thèmes et de visualisation : consigner les résultats et les limites de l’intégration, sans déclarer OS terminé.

      - [x] Créer `Theme/OS/Editions/Windows31/Views/{ClassificationView.xaml,ClassificationView.xaml.cs}` : formulaire indépendant de choix machine/format/protection utilisant les points fonctionnels existants du sélecteur, sans ouvrir le popup Base ; prévoir les contrôles et le rendu avant l’intégration au bureau.

      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31ClassificationViewTests.cs` : vérifier le changement de machine/format, la détection automatique et le détachement, en conservant les listes et règles existantes.

      - [x] Modifier `Theme/OS/Editions/Windows31/Windows31Controls.xaml` et `tests/RetroMediaLab.Tests/Interface/Themes/Windows31ControlsTests.cs` : transmettre ItemTemplateSelector et SelectionBoxItemStringFormat au contenu sélectionné des ComboBox, vérifier le rendu de DisplayMemberPath sans ToString de l’objet.

      - [x] Modifier `Resources/*/Classification.resx` avec Argos et revue des traductions : corriger Classification.Format avec le contexte Disk format dans toutes les langues.

      - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31ClassificationVisualTests.cs` : rendre `artifacts/themes-rebuild/current/classification.png`, inspecter le formulaire, puis supprimer le scénario en conservant la capture.

    - [ ] Raccorder les formulaires au bureau interne Windows 3.1.

      - [x] Modifier `Theme/OS/Editions/Windows31/Windows31Desktop.cs` et créer `Windows31Desktop.Dialogs.cs`, `Windows31DialogKind.cs`, `Windows31DialogMode.cs`, puis modifier `Windows31Frame.xaml.cs` et `Windows31Frame.Interactions.cs` : ouvrir les dialogues internes appartenant à une application, conserver activation et ordre des fenêtres, rétablir la disponibilité de leur propriétaire et libérer les seuls documents transférés dans finally, y compris à la fermeture de l’application ou du bureau.

      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31DialogTests.cs` : vérifier ouverture unique, activation, fermeture indépendante et fermeture du propriétaire, restitution des objets empruntés et nettoyage après erreur, sans fichiers externes.

      - [x] Modifier `Windows31Desktop.cs` : permettre la fabrication différée des définitions avec l’instance du bureau et fournir la fermeture d’une application déjà ouverte, sans recréer celle-ci.

      - [x] Créer `Theme/OS/Editions/Windows31/Windows31MediaApplications.cs` : raccorder lecture, écriture, conversion et visualisation indépendantes à leurs sources métier et au bureau ; raccorder les options, la classification et l’inspecteur aux dialogues, avec des durées de vie explicites et des tailles nommées.

      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31MediaApplicationsTests.cs` : vérifier les vraies commandes de menus et de formulaires jusqu’aux fenêtres internes, les échanges avec les contrôles métier conservés et la libération des vues à la fermeture.

      - [x] Créer `Theme/OS/Editions/Windows31/Windows31DialogForm.cs`, modifier `Windows31MediaApplications.cs` et `Windows31MediaApplicationsTests.cs` : ajouter aux options un bouton Fermer traduit, détacher leur contenu et libérer son document dans finally, vérifier la fermeture depuis ce bouton sans refermer l’application.

      - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31MediaDesktopVisualTests.cs` : rendre le gestionnaire, l’écriture, la conversion et les options ouvertes par leur menu dans `artifacts/themes-rebuild/current/windows31-media-desktop.png` à 1280×800, inspecter la composition et supprimer le scénario temporaire.

    - [ ] Reprendre l’acquisition Windows 3.1 dont la disposition initiale a été refusée.

      - [x] Modifier `Theme/OS/Editions/Windows31/Views/{AcquisitionView.xaml,AcquisitionView.xaml.cs}` ainsi que `AcquisitionOptionsFields.cs` et `Windows31MediaApplications.cs` : placer les révolutions dans l’onglet brut et reconstruire la fenêtre avec ses menus propres Fichier/Options, onglets brut/format connu, destination et historique complet, conserver toutes les actions et ajouter la fermeture de l’application depuis son menu.

      - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/{Windows31AcquisitionViewTests.cs,Windows31AcquisitionOptionsTests.cs,Windows31MediaApplicationsTests.cs}` : vérifier les menus, les changements d’onglet et la synchronisation des choix avec les contrôles métier existants ; inscrire les captures exactes avant leur création.

      - [x] Modifier `AcquisitionView.xaml` et `Windows31AcquisitionViewTests.cs` : faire suivre aux colonnes de l’historique la couleur du conteneur sélectionné et vérifier les trois textes rendus sur le fond de sélection.

      - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31AcquisitionVisualTests.cs` : rendre les onglets brut/format et la taille compacte dans `artifacts/themes-rebuild/current/{acquisition,acquisition-known,acquisition-narrow}.png`, vérifier les commandes visibles et les débordements, puis supprimer le scénario temporaire.

- [ ] Ajouter les palettes de couleurs à la variante Windows 3.1 du sélecteur de thèmes, puis reprendre l’explorateur.

  - [x] Créer temporairement `artifacts/themes-rebuild/windows31-color-source/` depuis l’archive Windows 3.1 déjà utilisée pour les icônes, extraire CONTROL.INI sans exécution et conserver ses palettes vérifiées dans `Theme/OS/Editions/Windows31/Assets/ColorSchemes.json` ; documenter la provenance dans `docs/design/themes-rebuild.md`, puis supprimer le cache source.

  - [x] Créer `Theme/OS/Editions/Windows31/{Windows31ColorScheme.cs,Windows31ColorRole.cs,Windows31Palette.cs,Windows31PaletteCatalog.cs,Windows31PaletteResources.cs}` et modifier `RetroMediaLab.App.csproj` et `Resources/00-Base/Options.resx` : valeurs d’origine, noms invariants en base commune et ressources sémantiques pour chaque couleur système.

  - [x] Modifier les XAML et constructeurs de `Theme/OS/Editions/Windows31/` et `Windows31Metrics.cs` : remplacer les pinceaux d’habillage figés par les ressources de palette dynamiques, appliquer les palettes à chaque bureau et à ses fenêtres sans changer les couleurs des données ni des rendus émulés.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31PaletteTests.cs`, modifier `Windows31ControlsTests.cs` et les contrôles XAML de cette édition : vérifier toutes les palettes et leurs rôles distincts sur fenêtres, sélections, menus et défilement ; corriger les associations texte/fond et prévoir les captures temporaires avant leur réalisation.

  - [ ] Raccorder les choix thème / déclinaison / apparence au sélecteur existant, sans déclarer OS terminé.

    - [x] Créer `RetroMediaLab.Infrastructure/Settings/ThemeEditionSettings.cs`, modifier `AppSettings.cs`, créer `Contracts/Theming/{ThemeEditionDefinition.cs,ThemeStyleDefinition.cs}`, modifier `ThemeDefinition.cs`, créer `Theme/OS/Editions/Windows31/Windows31Edition.cs` et modifier `Resources/00-Base/Options.resx` : conserver les choix par déclinaison et définir les 23 palettes de Windows 3.1 comme apparences internes à OS, sans ajout au catalogue des thèmes principaux.

    - [x] Déplacer `artifacts/themes-rebuild/current/*.png` vers `artifacts/themes-review/screens/OS/Windows31/Default/`, modifier `docs/design/themes-rebuild.md` pour y conserver les empreintes utiles des ressources et retirer les références aux rapports temporaires, supprimer `scripts/tools/{build-windows31-fonts.py,extract-windows31-resources.py}` et leur cache Python ajouté, puis supprimer `artifacts/themes-rebuild/` après contrôle des chemins absolus.

    - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31ReviewCaptureTests.cs` : capturer le bureau et ses applications ouvertes avec les palettes WindowsDefault, Bordeaux et HotdogStand dans `artifacts/themes-review/screens/OS/Windows31/{Default,Bordeaux,HotdogStand}/Desktop.png`, examiner les images, puis supprimer ce scénario et ses sorties temporaires non visuelles ; conserver uniquement les PNG.

    - [x] Modifier `Theme/Base/Views/Controls/Options/OptionsThemesSection.xaml` et `.xaml.cs` : choisir une déclinaison puis son apparence dans le sélecteur existant, conserver les boutons Clair/Sombre pour Base/Studio et le bouton Choisir ; ne pas ajouter de sélecteur dans le gestionnaire de programmes.

    - [x] Modifier `Theme/Base/Views/Windows/Preferences/PreferencesWindow.xaml.cs`, `App.xaml.cs` et `Services/Theming/ThemeManager.cs` : sauvegarder et restaurer les apparences par thème/déclinaison, appliquer au démarrage et aux fenêtres nouvelles, restaurer les réglages après erreur de sauvegarde.

    - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/ThemeEditionSelectionTests.cs` : vérifier la hiérarchie, les changements de déclinaison, la conservation des choix, le bouton Choisir et la persistance avec les fichiers simulés existants ; ne pas enregistrer OS incomplet dans le catalogue public.

- [ ] Reprendre l’explorateur indépendant Windows 3.1 après le raccordement des apparences.

  - [x] Modifier `Theme/OS/Editions/Windows31/Windows31Frame.xaml`, `Windows31Frame.xaml.cs`, `Windows31Controls.xaml` et `Windows31PaletteTests.cs` : appliquer également les rôles Bordure active/inactive et les couleurs des glyphes sur les boutons, vérifier dans la prochaine validation groupée.

  - [x] Créer `Theme/Base/Views/Controls/Explorer/Sections/ExplorerSection.Presentation.cs`, modifier `ExplorerSection.Navigation.cs` et `ExplorerDetailsPanel.xaml.cs` : exposer les requêtes de navigation et la présentation des propriétés déjà calculée ; conserver les modèles existants et les événements, sans déplacer les contrôles Base ni ouvrir ses fenêtres depuis OS.

  - [x] Créer `Theme/OS/Editions/Windows31/Views/{ExplorerView.xaml,ExplorerView.xaml.cs,ExplorerDetailsView.cs,ExplorerIssuesView.cs}` et modifier `Windows31DialogKind.cs` et `Windows31MediaApplications.cs` : reconstruire les menus, l’arbre et les fichiers en fenêtres internes ; conserver sélection, double-clic, sessions/volumes, pistes optiques, classification, progression et avertissements avec les services existants.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31ExplorerViewTests.cs` : vérifier ensemble navigation, propriétés, changements de média et nettoyage des abonnements ; prévoir les scénarios visuels exacts puis les supprimer après usage et conserver leurs PNG uniquement dans `artifacts/themes-review/screens/OS/Windows31/`.

  - [x] Corriger les contrastes révélés par les captures de l’explorateur.

    - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31ExplorerCaptureTests.cs` : rendre les trois scènes dans `artifacts/themes-review/screens/OS/Windows31/Default/{Explorer,ExplorerInformation,ExplorerNarrow}.png` ; l’inspection a révélé des textes générés blancs sur blanc.

    - [x] Créer `Theme/OS/Editions/Windows31/Windows31ContentPresenter.cs`, modifier les modèles XAML et `Windows31SystemMenu.cs`, puis modifier `Windows31ControlsTests.cs` : donner aux textes générés leurs styles locaux suivant le contrôle propriétaire pour empêcher la couleur du thème Base de prendre priorité dans les menus, boutons et lignes.

    - [x] Modifier `Theme/OS/Editions/Windows31/Windows31MediaApplications.cs` : dimensionner la fenêtre Informations suivant les lignes réellement présentes, avec bornes et défilement, au lieu de réutiliser la taille de l’inspecteur de visualisation.

    - [x] Modifier `TemporaryWindows31ExplorerCaptureTests.cs` : capturer à nouveau les trois scènes après correction, vérifier les contrastes et supprimer ce fichier temporaire ; ne conserver aucun log ou JSON temporaire.

  - [x] Incorporer les pictogrammes originaux de fichiers et dossiers.

    - [x] Créer temporairement `C:/Users/overt/AppData/Local/Temp/RmlWindows31ExplorerResources/archive.json` : récupérer l’archive Windows 3.10 de référence et vérifier son SHA-256 avant extraction, sans exécuter de programme historique.

    - [x] Créer temporairement `C:/Users/overt/AppData/Local/Temp/RmlWindows31ExplorerResources/extract.py` : extraire uniquement les ressources bitmap de WINFILE.EXE et les examiner ; modifier ce suivi avec les fichiers définitifs et leur raccordement avant de les incorporer.

    - [x] Modifier ce suivi avec les ressources identifiées, leurs chemins exacts et les fichiers de présentation à modifier avant de les incorporer ; prévoir une seule validation du lot et ses captures dans themes-review/screens.

    - [x] Créer `Theme/OS/Editions/Windows31/Assets/Images/FileManager.bmp`, modifier `RetroMediaLab.App.csproj` et `docs/design/themes-rebuild.md` : incorporer la ressource bitmap 100 de WINFILE.EXE avec ses pixels d’origine et consigner sa provenance.

    - [x] Créer `Contracts/Explorer/IFileEntryImageProvider.cs`, modifier `Theme/Base/Views/Controls/Common/FileEntryIcon.xaml.cs`, créer `Theme/OS/Editions/Windows31/Windows31FileImages.cs` et modifier `Windows31Controls.xaml` et `Views/ExplorerView.xaml` : conserver le contrôle FileEntryIcon et les catégories existantes fournies par le moteur, sélectionner seulement leur habillage historique par ressource du thème ; conserver Base et ses icônes actuelles hors de cette déclinaison.

    - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31FileImagesTests.cs` : vérifier en un lot la fidélité des pixels, la transparence de fond, les catégories existantes et le retour aux icônes Base après détachement ; inscrire le scénario visuel exact avant sa création et le supprimer après usage.

    - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31FileImagesCaptureTests.cs` : rendre les pictogrammes depuis les modèles existants et les palettes dans `artifacts/themes-review/screens/OS/Windows31/Default/ExplorerIcons.png` et `Bordeaux/ExplorerIcons.png`, puis supprimer ce scénario et conserver uniquement les captures.

    - [x] Supprimer `C:/Users/overt/AppData/Local/Temp/RmlWindows31ExplorerResources/` et tous les scénarios temporaires ajoutés à ce lot après contrôle des chemins ; conserver seulement les ressources nécessaires à l’application, leur provenance et les PNG demandés.

- [x] Reconstruire les outils Windows 3.1.

  - [x] Créer `Theme/OS/Editions/Windows31/Views/{ToolsView.xaml,ToolsView.xaml.cs}` : formulaires indépendants Effacer/Nettoyer avec menus, paramètres complets, avertissements et état matériel ; utiliser les propriétés et actions de ToolsTabSection, conserver les contrôles Base dans leurs parents et détacher les abonnements dans finally.

  - [x] Modifier `Theme/OS/Editions/Windows31/Windows31MediaApplications.cs` : ajouter les outils au groupe Système en réutilisant les services de maintenance existants ; ouvrir et fermer une seule application Outils avec ses propres formulaires.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31ToolsViewTests.cs` : vérifier ensemble les commandes construites à partir des saisies, la sélection des outils, les commandes Exécuter/Arrêter et le détachement, avec les dépendances simulées existantes.

  - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31ToolsCaptureTests.cs` : produire `artifacts/themes-review/screens/OS/Windows31/Default/{Erase,Clean}.png`, examiner les tailles et contrastes puis supprimer le scénario sans conserver de log ou JSON temporaire.

- [x] Reconstruire le gestionnaire des machines Windows 3.1 avant leurs fenêtres de rendu.

  - [x] Modifier `Theme/Base/Views/Controls/Emulation/Machine/EmulationSection.cs`, `EmulationSectionConfigurationFunctions.cs` et créer `EmulationSectionPresentation.cs` : exposer les configurations déjà chargées et leurs notifications, ainsi que la requête d’ouverture existante ; conserver les règles et les onglets Base, permettre les dépendances de modules contrôlées dans les tests.

  - [x] Créer `Theme/OS/Editions/Windows31/Views/{EmulationView.xaml,EmulationView.xaml.cs}` : gestionnaire indépendant des configurations avec liste, menu Fichier, ouverture au double-clic/Entrée, actualisation et fermeture ; utiliser les noms et traductions existants et détacher les abonnements, sans déplacer les blocs Base.

  - [x] Modifier `Theme/OS/Editions/Windows31/Windows31MediaApplications.cs` : proposer le gestionnaire dans le groupe Émulation quand sa source est fournie ; ne pas déclarer les fenêtres des machines terminées avant reconstruction de leurs commandes et surfaces.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31EmulationViewTests.cs` : vérifier les configurations de modules simulés, sélection/actualisation et détachement sans charger de DLL ni lancer de cœur externe ; valider avec les scénarios existants de thèmes et d’émulation.

  - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31EmulationCaptureTests.cs` : rendre le gestionnaire dans `artifacts/themes-review/screens/OS/Windows31/Default/EmulationManager.png`, examiner son affichage puis supprimer le test temporaire ; conserver uniquement le PNG.

- [x] Séparer la présentation des machines de leurs moteurs pour les fenêtres propres à chaque thème.

  - [x] Créer `Theme/Common/EmulationMachinePresentation.cs` et modifier `Theme/Base/Views/Controls/Emulation/Machine/MachineView.cs` : définir les points visuels réellement utilisés par vidéo/entrées/périphériques, conserver la présentation Base et libérer ses abonnements/timer quand son propriétaire la ferme.

  - [x] Modifier `Presenters/Emulation/Machine/MachineVideoPresenter.cs`, `Controllers/Emulation/Machine/MachineInputController.cs` et `Theme/Base/Views/Controls/Emulation/Machine/MachineController.cs` : dépendre de cette présentation pour le rendu et les entrées, libérer la présentation possédée dans finally après arrêt du cœur ; conserver les surfaces et comportements existants.

  - [x] Modifier `docs/design/themes-rebuild.md` : décrire les points de présentation partagés et la reconstruction indépendante des commandes, périphériques et fenêtres, sans réutilisation du panneau Base ni changement de compatibilité des moteurs.

  - [x] Modifier `docs/tasks/application-themes.md` après validation groupée des scénarios existants d’entrées/vidéo/thèmes/émulation : consigner le résultat et les fichiers exacts de la présentation Windows 3.1 à construire ensuite avant leur création.

- [x] Reconstruire la présentation d’une machine Windows 3.1 avec les mêmes actions et surfaces.

  - [x] Créer `Contracts/Machine/{MachineCommandId.cs,MachineCommandPresentation.cs}`, modifier `Theme/Base/Views/Controls/Emulation/Machine/MachineCommandBar.cs` : identifier les commandes existantes par enum et exposer leurs points fonctionnels et états sans recopier les actions ni les règles de disponibilité.

  - [x] Modifier `Theme/Common/EmulationMachinePresentation.cs` et `Theme/Base/Views/Controls/Emulation/Machine/MachineView.cs` : permettre le raccordement des points de commande à une présentation indépendante, garder la barre d’origine pour Base.

  - [x] Créer `Theme/OS/Editions/Windows31/Views/{MachineView.xaml,MachineView.xaml.cs}` : menus propres Machine/Supports/Affichage, écran, progression des shaders et périphériques ; reconstruire les commandes tape à partir des modèles existants, prévoir désabonnement et arrêt du timer dans Dispose, conserver les couleurs des données et pixels du moteur.

  - [x] Modifier `Contracts/Machine/MachineControllerOptions.cs` et `Theme/Base/Views/Controls/Emulation/Machine/MachineController.cs` : fournir la présentation à la construction, raccorder les mêmes commandes/vidéo/entrées et conserver Base par défaut ; prendre en charge toutes les machines sans dépendance Acorn.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31MachineViewTests.cs` : vérifier actions, disponibilité, médias, tape, surface et nettoyage avec des dépendances contrôlées sans DLL externe ; prévoir ensuite les captures exactes et l’échange de présentation des sessions existantes avant intégration au bureau.

- [x] Terminer la gestion des ressources et examiner la nouvelle fenêtre de machine.

  - [x] Modifier `Theme/Base/Views/Controls/Emulation/Machine/MachineController.cs`, `EmulationSectionMachineFunctions.cs` et `Services/Emulation/MachineSession.cs` : fabriquer le contrôleur de façon asynchrone avec nettoyage en finally si la présentation échoue, libérer le cœur et retirer la session après une erreur ; conserver le même chemin de démarrage.

  - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/Windows31MachineViewTests.cs` : vérifier que la création attend la libération effective du cœur après un échec de présentation et ne laisse aucune session enregistrée.

  - [x] Modifier `Theme/OS/Editions/Windows31/Views/MachineView.xaml` : utiliser le libellé traduit Couper le son pour la commande cochée quand le son est coupé, puis créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31MachineCaptureTests.cs` : rendre la fenêtre avec une surface synthétique explicitement de démonstration dans `artifacts/themes-review/screens/OS/Windows31/Default/Machine.png`, examiner le rendu puis supprimer le test temporaire.

  - [x] Modifier `docs/tasks/application-themes.md` après une validation groupée : consigner les vérifications et maintenir le raccordement au bureau et le changement de présentation des sessions ouvertes comme travaux restant à réaliser.

- [x] Changer la présentation des machines ouvertes sans recréer leur cœur.

  - [x] Modifier `Theme/Common/EmulationMachinePresentation.cs`, `Theme/Base/Views/Controls/Emulation/Machine/MachineView.cs` et `Theme/OS/Editions/Windows31/Views/MachineView.xaml.cs` : détacher explicitement la surface vidéo empruntée afin de la restituer ou de la raccorder à une autre présentation sans la détruire.

  - [x] Modifier `Presenters/Emulation/Machine/MachineVideoPresenter.cs` et `Controllers/Emulation/Machine/MachineInputController.cs` : changer les hôtes de présentation, leurs événements et le focus en conservant la même surface, le même traitement vidéo et les mêmes entrées ; restaurer la présentation précédente si le transfert vidéo échoue.

  - [x] Modifier `Theme/Base/Views/Controls/Emulation/Machine/MachineCommandBar.cs` et `MachineController.cs` : transférer les points fonctionnels de commande et changer la vue possédée, conserver le cœur et ses médias, rétablir la vue précédente après erreur et libérer seulement la vue remplacée dans finally.

  - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/Windows31MachineViewTests.cs` : vérifier les transferts aller/retour de commandes et surface, les erreurs et les nettoyages avec les dépendances existantes ; valider le lot avec les scénarios de thèmes et de machines.

  - [x] Modifier `docs/tasks/application-themes.md` : consigner les résultats et inscrire les fichiers exacts du raccordement au bureau avant leur modification.

- [ ] Raccorder les machines au bureau Windows 3.1 avec des fenêtres indépendantes.

  - [x] Modifier `Theme/Base/Views/Controls/Emulation/Machine/MachineController.cs` et créer `Theme/Common/MachinePresentationLease.cs` : transférer la vue possédée vers un hôte neuf, conserver le contrôleur fonctionnel dans son emplacement source et restituer la présentation et son activation quand le thème se ferme ; détacher la vue à la destruction de son propriétaire.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31MachineWorkspaceTests.cs` : vérifier le transfert et la restitution sur le contrôleur complet, la conservation du cœur et de sa surface, un hôte déjà occupé et la fermeture du propriétaire, sans lecture de fichier ni cœur externe ; valider ce lot avant le raccordement des notifications.

  - [x] Modifier `Theme/Base/Views/Controls/Emulation/Machine/EmulationSectionPresentation.cs`, `EmulationSectionMachineFunctions.cs` et `EmulationSectionConfigurationFunctions.cs` : exposer les machines réellement ouvertes et leurs notifications, réutiliser leurs actions de sélection/fermeture et appliquer la présentation choisie aux ouvertures suivantes ; identifier les instances par module et configuration, sans règle spécifique Acorn.

  - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/Windows31MachineWorkspaceTests.cs` : vérifier les notifications et les identités de deux machines, la sélection et la fermeture via les contrôleurs existants, puis la fermeture générale, sans démarrer de cœur externe.

  - [x] Modifier `Controllers/Emulation/Machine/MachineInputController.cs`, `Theme/Base/Views/Controls/Emulation/Machine/MachineController.cs` et `tests/RetroMediaLab.Tests/Interface/Themes/Windows31MachineViewTests.cs` : résoudre le handle natif courant du présentateur au moment du focus et de la capture, afin de suivre un HWND reconstruit lors du changement de parent ou de moteur ; vérifier avec un fournisseur de handles simulé, sans créer de fenêtre native.

  - [x] Créer `Theme/OS/Editions/Windows31/Windows31Desktop.Machines.cs`, modifier `Windows31Desktop.cs` et `Windows31ProgramManager.xaml.cs` : créer les cadres des machines avec déplacement, réduction, agrandissement, activation et fermeture asynchrone, intégrer les couches natives et restituer les vues empruntées lors du retrait du thème sans arrêter leurs cœurs.

  - [x] Modifier `Theme/OS/Editions/Windows31/Windows31MediaApplications.cs` : raccorder la source de machines au bureau après son initialisation et ouvrir leurs fenêtres depuis le gestionnaire existant.

  - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/Windows31MachineWorkspaceTests.cs` : vérifier plusieurs machines, leur activation et fermeture, le retour aux sources et le nettoyage après erreur avec les dépendances contrôlées ; inscrire les captures exactes avant de les produire puis supprimer les scénarios temporaires.

  - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31MachineDesktopCaptureTests.cs` : rendre à 1280×800 le gestionnaire et deux fenêtres raccordées à leurs contrôleurs simulés dans `artifacts/themes-review/screens/OS/Windows31/Default/DesktopMachines.png`, identifier la démonstration dans les vues, examiner le rendu puis supprimer ce scénario sans conserver de journal ni de JSON.

  - [x] Corriger les détails de groupe révélés par la capture intégrée.

    - [x] Modifier `Theme/OS/Editions/Windows31/Windows31Metrics.cs` : utiliser le libellé Médias déjà traduit par `Explorer.Category.Media` pour le groupe des opérations, au lieu du libellé Outils.

    - [x] Modifier `Theme/OS/Editions/Windows31/Windows31ProgramManager.xaml.cs`, `Windows31ProgramManager.xaml`, `Windows31PaletteResources.cs` et `Windows31Desktop.cs` : faire suivre aux cadres des groupes l’activation du gestionnaire sans oublier le groupe précédemment actif ; utiliser les couleurs de palette pour les titres des icônes et leurs sélections.

    - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/Windows31DesktopTests.cs` et `Windows31PaletteTests.cs` : vérifier le retour au groupe choisi après activation d’une autre application et les couleurs de ses icônes dans les palettes ; recréer temporairement le scénario de capture `TemporaryWindows31MachineDesktopCaptureTests.cs` pour actualiser DesktopMachines.png après correction, puis le supprimer.

- [x] Corriger les accès et contrôles Windows 3.1 demandés dans les dernières captures.

  - [x] Afficher les machines configurées directement dans le groupe Émulation.

    - [x] Modifier `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31ProgramManager.xaml.cs` : créer le groupe même sans lanceur générique, afficher chaque configuration avec son image existante redimensionnée en icône en mémoire, conserver la sélection et ouvrir la machine au double-clic ou avec Entrée.

    - [x] Modifier `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Desktop.Machines.cs` et `Windows31MediaApplications.cs` : alimenter les icônes depuis les configurations existantes et appeler leur ouverture/réactivation sans fenêtre intermédiaire.

    - [x] Modifier `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31ProgramManager.xaml.cs` : conserver le déclenchement du double-clic existant et utiliser l’icône Émulation si une machine ne fournit aucune image.

    - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/Windows31MachineWorkspaceTests.cs` : vérifier les icônes des configurations arrêtées, leur mise à jour et la réactivation sans recréer de cœur ; vérifier le redimensionnement de l’image en conservant ses proportions et sa transparence.

  - [x] Corriger l’arbre des dossiers et les nombreux périphériques.

    - [x] Modifier `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31FileImages.cs` et créer `Windows31FolderBranch.cs` : utiliser les variantes de dossiers avec signes de la planche historique et dessiner les traits des branches selon les lignes visibles, sans bouton supplémentaire.

    - [x] Modifier `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ExplorerView.xaml` et `ExplorerView.xaml.cs` : remplacer les carrés gris séparés par les signes d’expansion intégrés aux icônes et les traits de l’arbre Windows 3.1, sans signe pour les feuilles.

    - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/Windows31ExplorerViewTests.cs` : vérifier les variantes des branches, l’absence de boutons gris et le développement via les actions existantes.

    - [x] Modifier `src/RetroMediaLab.App/Contracts/Machine/MachineViewDevice.cs` et `Theme/Base/Views/Controls/Emulation/Machine/MachineController.cs` : transmettre le type de support existant à la présentation pour regrouper ses menus sans déduire le type depuis le nom.

    - [x] Modifier `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/MachineView.xaml`, `MachineView.xaml.cs` et `Windows31Desktop.Machines.cs` : limiter la barre aux indicateurs compacts défilants, conserver les commandes dans des panneaux détachés et notifier leur ouverture/nettoyage ; créer `Windows31Desktop.Devices.cs` et modifier `Windows31Desktop.cs` pour héberger leurs cadres dans le bureau avec activation, bornage et nettoyage avec la machine.

    - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/Windows31MachineViewTests.cs` et `Windows31MachineWorkspaceTests.cs` : vérifier de nombreux types de périphériques, leurs actions et états, l’ouverture unique des panneaux et leur destruction avec la machine, puis valider le lot.

  - [x] Modifier `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Desktop.Devices.cs` : réduire la hauteur minimale propre aux petits panneaux de lecteurs et aligner leurs commandes en haut, puis actualiser leur capture.

  - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31CorrectionsCaptureTests.cs` : rendre les icônes des configurations avec l’image existante du module, les branches des dossiers et les nombreux lecteurs dans `artifacts/themes-review/screens/OS/Windows31/Default/EmulationIcons.png`, `ExplorerTree.png` et `DevicePanels.png` ; examiner les images puis supprimer ce scénario et ses éventuels fichiers temporaires.

- [ ] Raccorder le bureau Windows 3.1 à la fenêtre principale et conserver les accès fonctionnels.

  - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/TerminalView.xaml` et `.xaml.cs`, modifier `Windows31ApplicationId.cs` et `Windows31Icons.xaml` : présenter la console dans sa propre fenêtre avec menus Copier/Fermer, commande et sortie indépendantes suivant les données existantes, conserver les styles sémantiques des messages et libérer les abonnements à la fermeture.

  - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Workspace.cs` : utiliser le contrat IThemeWorkspace existant, raccorder les sources lecture/écriture/conversion/visualisation/exploration/outils/machines au bureau, fournir l’accès Console dans Système, remplacer l’affichage de la coque Base et restaurer ses éléments dans finally ; suivre la navigation fonctionnelle des onglets conservés sans reprendre leurs conteneurs.

  - [x] Modifier `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Desktop.cs` et `Windows31ProgramManager.xaml.cs` : exposer l’ouverture du groupe Émulation depuis la navigation conservée sans fenêtre de liste intermédiaire et notifier l’activation des applications pour synchroniser leurs contrôleurs.

  - [x] Modifier `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/TerminalView.xaml` et `Windows31Desktop.cs` : placer Copier dans le menu Fichier sans réutiliser le libellé Écriture et vider les événements publics du bureau dans son nettoyage final.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31WorkspaceTests.cs` : vérifier la navigation complète des sources, la console et la restitution après changement de thème ou erreur ; inscrire les captures avant leur réalisation, sans inscrire OS comme terminé.

  - [x] Modifier `src/RetroMediaLab.App/Contracts/Theming/ThemeEditionDefinition.cs`, `Theme/OS/Editions/Windows31/Windows31Edition.cs`, `Services/Theming/ThemeManager.cs` et `Theme/Base/Views/Windows/Shell/MainWindow.Theming.cs` : faire créer le bureau par sa déclinaison, le reconstruire lorsqu’elle change et conserver les fabriques Base/Studio ; ne pas publier OS tant que ses fenêtres manquent.

  - [x] Donner à la console son icône Windows 3.1 propre, après l’échec du contrôle des doublons.

    - [x] Créer temporairement `artifacts/themes-review/tmp-terminal/extract-terminal.py` et `source.json` : télécharger l’archive PCjs déjà référencée et préparer la lecture de TERMINAL.EXE sans l’exécuter.

    - [x] Modifier ce script temporaire et créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Icons/terminal.ico` : extraire l’icône du programme historique ; modifier `Windows31Icons.xaml` et `docs/design/themes-rebuild.md` pour l’utiliser et conserver sa provenance et ses empreintes.

    - [x] Supprimer `artifacts/themes-review/tmp-terminal/extract-terminal.py`, `source.json` et leur dossier vide après incorporation, puis relancer le contrôle groupé.

  - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/Windows31WorkspaceTests.cs` : créer le bureau par la fabrique réelle de Windows31Edition et vérifier la restitution de la coque par le contrat existant.

  - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/Windows31WorkspaceTests.cs` pour rendre son montage réutilisable, créer temporairement `TemporaryWindows31WorkspaceCaptureTests.cs` : rendre la coque entièrement remplacée et la console dans `artifacts/themes-review/screens/OS/Windows31/Default/IntegratedWorkspace.png`, examiner l’image puis supprimer le scénario temporaire.

- [ ] Reconstruire les préférences Windows 3.1 avec leurs sources fonctionnelles existantes.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Options/OptionsGeneralSection.xaml.cs` : exposer les requêtes existantes de parcours, exemple suivant, activation de l’historique et fin d’édition pour la présentation indépendante, sans déplacer les contrôles Base.

  - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/GeneralOptionsView.xaml` et `.xaml.cs` : reconstruire les choix généraux et les tags de conversion en onglets compacts avec les données, validations, actions et traductions existantes ; libérer les abonnements dans finally.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31GeneralOptionsTests.cs` : vérifier les choix, textes, historique, actions et sauvegardes à la fin d’édition sans stockage ni matériel externe ; prévoir une capture avant de la produire.

  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Options/OptionsThemesSection.xaml.cs` : exposer les choix d’apparence et la notification de renouvellement des choix pour la vue indépendante, sans modifier son affichage ni son enregistrement.

  - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ThemeOptionsView.xaml` et `.xaml.cs` : reconstruire le choix des thèmes en panneau d’affichage Windows 3.1 avec aperçu, liste déroulante de thème, déclinaison et apparence internes, conserver le bouton Choisir et les actions de la source.

  - [x] Modifier `src/RetroMediaLab.Infrastructure/Settings/AppThemeDesign.cs`, créer `src/RetroMediaLab.App/Theme/OS/OSDefinition.cs` et ajouter le nom invariant OS à `Resources/00-Base/Options.resx` : définir le thème principal OS et sa déclinaison Windows31 avec sa fabrique réelle, sans l’ajouter au catalogue public avant la fin des fenêtres.

  - [x] Modifier `src/RetroMediaLab.App/Resources/*/Options.resx` avec Argos pour les libellés Description OS et Déclinaison dans toutes les langues ; utiliser ces clés dans `OSDefinition.cs`.

  - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31ThemeOptionsTests.cs` : vérifier les choix d’apparence, le thème OS avec déclinaison Windows31 et les palettes, la sauvegarde existante et le détachement des abonnements.

  - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31OptionsCaptureTests.cs` : rendre les onglets Général/Conversion et le sélecteur indépendant dans `artifacts/themes-review/screens/OS/Windows31/Default/{GeneralOptions,ConversionTagsOptions,ThemeOptions}.png`, examiner les images puis supprimer le scénario et ses fichiers temporaires.

  - [x] Modifier `docs/tasks/application-themes.md` : détailler les fichiers des préférences Journaux, Matériel, Moteurs, Profils et Manettes, leur raccordement à la navigation existante et les dialogues restants avant de les implémenter ; prévoir les aperçus propres à chaque déclinaison et apparence d’OS.

  - [ ] Terminer toutes les pages de préférences et leurs accès réels.

    - [x] Modifier `Theme/Base/Views/Controls/Options/OptionsLogsSection.xaml.cs` et créer `Theme/OS/Editions/Windows31/Views/LogsOptionsView.xaml` et `.xaml.cs` : reprendre les lignes de journaux, tailles validées, archives et ouverture du dossier depuis les données et actions existantes.

    - [x] Modifier `Theme/Base/Views/Controls/Options/OptionsHardwareSection.xaml.cs` et créer `Theme/OS/Editions/Windows31/Views/HardwareOptionsView.xaml` et `.xaml.cs` : reconstruire tous les réglages des contrôleurs/lecteurs et des outils hôtes, scans, téléchargements et progression sans appeler du matériel lors des vérifications isolées.

    - [x] Utiliser les points fonctionnels de `Theme/Base/Views/Controls/Options/OptionsEnginesSection.xaml.cs` et créer `Theme/OS/Editions/Windows31/Views/EnginesOptionsView.xaml` et `.xaml.cs` : reconstruire tous les champs du choix des moteurs et leurs actions existantes.

    - [x] Modifier `Theme/Base/Views/Controls/Options/OptionsProfilesSection.xaml.cs` et créer `Theme/OS/Editions/Windows31/Views/ProfilesOptionsView.xaml` et `.xaml.cs` : reprendre les profils Lecture/Écriture/Conversion, sélections et actions existantes dans un formulaire propre à Windows 3.1.

    - [ ] Reconstruire les manettes avec leur rendu et leurs actions réels.

      - [x] Modifier `Theme/Base/Views/Controls/Options/ControllerVisualizer.cs`, `OptionsControllersSection.xaml.cs` et `Controllers/OptionsControllersSection.Devices.cs` : notifier les changements des données dessinées et faire suivre la surveillance à la présentation visible, restaurer son propriétaire à la fermeture et exposer l’enregistrement des zones mortes.

      - [x] Créer `Theme/OS/Editions/Windows31/Views/ControllersOptionsView.xaml` et `.xaml.cs` : reconstruire choix du périphérique/modèle, rendu physique indépendant, entrées, zones mortes, vibration, capacités et identité, avec les données et actions existantes ; libérer les notifications et arrêter le suivi de cette présentation dans finally.

    - [x] Modifier `Theme/OS/Editions/Windows31/Views/ControllersOptionsView.xaml` : conserver le choix du modèle hors de l’onglet analogique, pour les manettes numériques qui n’exposent pas de zones mortes.

    - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31OptionsPagesTests.cs` : vérifier ensemble les journaux, actions de matériel simulées, moteurs, profils et transfert du rendu des manettes, sans démarrer de surveillance matérielle réelle ni lire de profil externe.

    - [x] Modifier `Theme/OS/Editions/Windows31/Views/LogsOptionsView.xaml` et `Resources/fr-FR/Options.resx` : remplacer les légendes répétées et tronquées par des en-têtes de colonnes partagées, conserver les noms accessibles des cases et corriger la phrase française du dossier de journaux ; actualiser la capture avant de supprimer le scénario temporaire.

    - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31RemainingOptionsCaptureTests.cs` : rendre les pages Journaux, Matériel, Moteurs, Profils et Manettes avec des données simulées à 1280×800 dans `artifacts/themes-review/screens/OS/Windows31/Default/{LogsOptions,HardwareOptions,EnginesOptions,ProfilesOptions,ControllersOptions}.png`, examiner les images puis supprimer le scénario sans conserver de fichier temporaire.

    - [ ] Raccorder les préférences au bureau par le contrat de présentation existant.

      - [x] Modifier `src/RetroMediaLab.App/Contracts/Theming/IThemeWorkspace.cs`, `Theme/Base/Views/Windows/Shell/MainWindow.Theming.cs` et `Services/Windows/WpfWindowNavigationService.cs` : proposer la fenêtre fonctionnelle à la présentation du thème avant le dialogue natif, conserver le chemin Base/Studio.

      - [x] Modifier `Theme/Base/Views/Windows/Preferences/PreferencesWindow.xaml.cs` et créer `Theme/OS/Editions/Windows31/Views/PreferencesView.cs` : exposer la navigation fonctionnelle et reconstruire un sélecteur de pages propre, relier les sept vues sans déplacer les contrôles Base, libérer chaque vue dans finally.

      - [x] Modifier `Theme/OS/Editions/Windows31/Windows31Desktop.Dialogs.cs`, `Windows31Desktop.cs`, `Windows31DialogKind.cs` et créer `Windows31Desktop.Preferences.cs` : héberger les préférences modales depuis le gestionnaire, désactiver les autres cadres, attendre la fermeture et la sauvegarde de la fenêtre source, libérer les abonnements et les documents en finally.

      - [x] Modifier `Theme/OS/Editions/Windows31/Windows31Workspace.cs` et `Windows31ProgramManager.xaml.cs` : ouvrir les préférences depuis Système et le menu existant, présenter la fenêtre créée par la navigation avec sa source fonctionnelle réelle.

    - [x] Modifier `Theme/Base/Views/Windows/Preferences/PreferencesWindow.xaml.cs` : empêcher la libération du dialogue interne de relancer une sauvegarde lorsque la fenêtre source est déjà fermée.

    - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31PreferencesTests.cs` : parcourir les sept vues indépendantes avec sources simulées, contrôler les accès Système, la modalité globale et la libération.

    - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31PreferencesLifecycleTests.cs` : vérifier avec la fenêtre fonctionnelle réelle non affichée la sauvegarde asynchrone et sa fermeture effective, capturer la présentation intégrée dans `artifacts/themes-review/screens/OS/Windows31/Default/IntegratedPreferences.png`, examiner puis supprimer le test temporaire.

    - [x] Modifier `Theme/Base/Views/Windows/Preferences/PreferencesWindow.xaml.cs`, `Services/Windows/WpfWindowNavigationService.cs`, `Theme/OS/Editions/Windows31/Windows31Workspace.cs` et créer `Theme/OS/Editions/Windows31/Views/PreferencesView.cs` : raccorder toutes les vues de préférences aux contrôleurs et au cycle de sauvegarde existants, exposer leurs accès dans Système et libérer leurs fenêtres effectivement dans finally ; compléter la liste avec le mécanisme de présentation retenu avant de créer un contrat supplémentaire.

    - [ ] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31PreferencesTests.cs` : vérifier les pages et leur navigation, les modifications/sauvegardes et leur fermeture avec les dépendances simulées existantes ; inscrire les fichiers de capture avant toute création et supprimer les scénarios qui utilisent des fenêtres ou fichiers externes.

  - [x] Reconstruire et raccorder les dialogues fonctionnels de profils, conflits et matériel.
    - [x] Modifier `docs/tasks/application-themes.md` : inventorier les accès réels restants ; navigation : préférences d’émulation, options des modules, mises à jour, historique, à propos, outils ; dialogues métier : profils, conflits lecture/conversion, matériel ; autres : supports, firmware, options de machines, fichiers et erreurs. Conserver ces accès dans le périmètre avant inscription publique d’OS.
    - [x] Créer `src/RetroMediaLab.App/Theme/Common/ThemeDialogCompletion.cs` et modifier `Services/Windows/WpfWindowNavigationService.cs`, `Services/Dialogs/WpfBusinessDialogService.cs` : partager le résultat et la présentation d’un dialogue avec le thème actif, parcourir les propriétaires et conserver le dialogue natif pour Base/Studio.
    - [x] Modifier `Theme/Base/Views/Dialogs/Profiles/ProfileNameWindow.xaml.cs`, `Read/ReadConflictWindow.xaml.cs`, `Conversion/ConversionConflictWindow.xaml.cs`, `Hardware/HardwareUnavailableWindow.xaml.cs` : conserver les choix et validations existants en exposant leurs actions et en terminant par le relais commun ; notifier les changements de choix de conversion pour toutes les présentations.
    - [x] Créer `Theme/OS/Editions/Windows31/Views/BusinessDialogView.cs` : partager uniquement les primitives Windows31 des dialogues, les liaisons, les actions et le détachement ; gérer Échap dans la fenêtre interne sans fermer la fenêtre Windows hôte.
    - [x] Créer `Theme/OS/Editions/Windows31/Views/ProfileNameView.cs`, `ReadConflictView.cs`, `ConversionConflictView.cs`, `HardwareUnavailableView.cs` : reconstruire chaque dialogue avec contrôles, tailles, disposition et traductions Windows31, reprendre les champs et actions réels et libérer les abonnements dans finally.
    - [x] Modifier `Theme/OS/Editions/Windows31/Windows31Desktop.Dialogs.cs` et `Options/Controllers/ProfileOptionsController.cs` : permettre un dialogue enfant des préférences, préserver la modalité et l’activation du parent puis router le renommage existant vers le relais commun.
    - [x] Déplacer `Theme/OS/Editions/Windows31/Windows31Desktop.Preferences.cs` en `Windows31Desktop.Windows.cs` et modifier son contenu, `Windows31DialogKind.cs` et `Windows31Workspace.cs` : partager l’hébergement modal et la fermeture avec les quatre dialogues, attendre la libération des sources, restaurer le contexte du dialogue propriétaire et fermer les documents dans finally.
    - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31ConversionConflictRowsTests.cs` : vérifier les choix de conversion et leurs notifications de données sans fenêtre ni fichier externe.
    - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31BusinessDialogsTests.cs` : vérifier les quatre fenêtres réelles non affichées, leurs choix, validations, annulation et fermeture, y compris un dialogue imbriqué ; capturer à 1280×800 dans `artifacts/themes-review/screens/OS/Windows31/Default/{ProfileName,ReadConflict,ConversionConflict,MissingHardware,NestedDialogs}.png`, examiner puis supprimer le scénario temporaire et vérifier la destruction des handles possédés.
    - [x] Modifier `Theme/Common/ThemeDialogCompletion.cs`, `Services/Windows/WpfWindowNavigationService.cs`, `Services/Dialogs/WpfBusinessDialogService.cs`, `Options/Controllers/ProfileOptionsController.cs`, `Theme/OS/Editions/Windows31/Windows31Desktop.Windows.cs` et le scénario temporaire : conserver le propriétaire fonctionnel séparément du propriétaire HWND ; WPF refuse un propriétaire natif non affiché, conserver la chaîne sans afficher la source et vérifier le dialogue imbriqué réel.
    - [x] Modifier `Theme/OS/Editions/Windows31/Windows31Desktop.Dialogs.cs` et `Windows31Desktop.Windows.cs` : désactiver visuellement le titre du gestionnaire quand un dialogue est actif et donner aux petits dialogues Profil/Conflit une hauteur adaptée, sans minimum de 240 pixels imposé ; régénérer les captures du scénario temporaire avant sa suppression.
  - [ ] Reconstruire les préférences globales d’émulation et leurs accès Système.
    - [x] Modifier `Theme/Base/Views/Controls/Emulation/Options/EmulationPreferencesSection.cs`, `EmulationPreferencesSectionConfigurationFunctions.cs`, utiliser `EmulationPreferencesSectionSettingsFunctions.cs` pour exposer les champs, les données de configurations, les choix de filtres et leurs requêtes existantes ; notifier les mises à jour et permettre le chargement depuis une présentation indépendante.
    - [x] Créer `Theme/Base/Views/Controls/Emulation/Input/InputBindingEditor.Presentation.cs` et modifier `InputBindingEditorCaptureFunctions.cs` : transférer les événements de saisie et le hook de capture vers la vraie présentation sans déplacer l’éditeur Base ; annuler la capture, arrêter son minuteur, retirer le hook et restaurer la présentation source dans finally.
    - [x] Créer `Theme/OS/Editions/Windows31/Views/InputBindingsView.xaml` et `.xaml.cs`, `NetworkView.cs` : reconstruire les raccourcis avec recherche, affectation, effacement, valeurs par défaut, conflits et saisie existants ; reconstruire le tableau réseau informatif avec toutes les colonnes, sans sélection ni édition et avec libération de ses notifications.
    - [x] Créer `Theme/OS/Editions/Windows31/Views/EmulationPreferencesView.xaml` et `.xaml.cs` : reconstruire dossiers, raccourcis, configurations et réseau en réutilisant les données et actions des sources, conserver les noms des machines et les commandes modifier/supprimer, disposer toutes les vues dans finally.
    - [x] Modifier `Theme/Base/Views/Controls/Emulation/Options/EmulationPreferencesSection.cs` et `Services/Windows/WpfWindowNavigationService.cs` : attendre le chargement de configurations possédé avant fermeture et résoudre un propriétaire natif visible uniquement pour les dialogues non encore reconstruits ; conserver le propriétaire fonctionnel du thème séparé.
    - [x] Modifier `Theme/Base/Views/Windows/EmulationPreferences/EmulationPreferencesWindow.xaml.cs`, `Theme/OS/Editions/Windows31/Windows31Desktop.Windows.cs`, `Windows31DialogKind.cs`, `Windows31Desktop.cs`, `Windows31ProgramManager.xaml`, `Windows31ProgramManager.xaml.cs`, `Windows31Workspace.cs` : présenter les réglages d’émulation dans le bureau et le groupe Système, attendre leur sauvegarde à la fermeture et conserver les accès aux options des modules dans la suite du suivi.
    - [x] Modifier `Theme/Base/Views/Windows/EmulationPreferences/EmulationPreferencesWindow.xaml.cs` et `Theme/OS/Editions/Windows31/Views/EmulationPreferencesView.xaml` et `.xaml.cs` : ajouter l’import manquant de la section, utiliser la clé existante Common.Modify, fournir le contexte d’erreur requis et résoudre les symboles des lecteurs par une ressource locale accessible au XAML.
    - [x] Modifier `Theme/Base/Views/Controls/Emulation/Options/EmulationPreferencesSection.cs` pour injecter la liste existante de modules dans les vérifications sans découverte de DLL, `Theme/OS/Editions/Windows31/Views/InputBindingsView.xaml.cs` pour conserver le compte à rebours de capture et `NetworkView.cs` pour réutiliser les constantes de notification existantes.
    - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31EmulationPreferencesTests.cs` : vérifier données, filtres, dossiers, raccourcis et réseau avec les dépendances simulées existantes ; inscrire le scénario temporaire et les captures avant création puis les supprimer après utilisation.
    - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31EmulationPreferencesTests.cs` : capturer les quatre pages réelles avec configurations simulées et interface réseau en mémoire dans `artifacts/themes-review/screens/OS/Windows31/Default/{EmulationFolders,EmulationShortcuts,EmulationConfigurations,EmulationNetwork}.png` ; vérifier la sauvegarde asynchrone et la fermeture effective d’EmulationPreferencesWindow, examiner puis supprimer le scénario temporaire et tout handle possédé.
    - [x] Modifier `Theme/OS/Editions/Windows31/Views/InputBindingsView.xaml`, `NetworkView.cs` et `EmulationPreferencesView.xaml` : corriger les couleurs héritées des ItemsControl imbriqués constatées dans les captures, faire suivre les touches de raccourci à la couleur de leur ligne sélectionnée et ajouter les en-têtes Machine/CPU/RAM ; régénérer les captures avant de supprimer le scénario temporaire.
    - [x] Modifier ce suivi avant la livraison d’OS pour les pictogrammes des périphériques des configurations et pour la transition des fenêtres déjà ouvertes lors d’un choix de thème ; conserver ces exigences dans la validation complète.
    - [ ] Modifier `Theme/OS/Editions/Windows31/Views/EmulationPreferencesView.xaml` et `.xaml.cs` : remplacer les symboles de périphériques issus des polices Base par les pictogrammes Windows31 et les données matérielles existantes, avec libellés accessibles ; prévoir leurs fichiers exacts si un nouvel actif est nécessaire avant de le créer.
    - [ ] Modifier `Services/Windows/WpfWindowNavigationService.cs`, `Services/Theming/ThemeManager.cs` et `Theme/Base/Views/Windows/Preferences/PreferencesWindow.xaml.cs` : appliquer la présentation du nouveau thème aux fenêtres déjà ouvertes en préservant leurs données et le cycle modal ; détailler le mécanisme et les autres fichiers concernés après lecture avant toute modification de contrat.
  - [ ] Reconstruire les options des modules depuis leurs descriptions et contrôleurs existants.
    - [x] Créer `Theme/Base/Views/Controls/Emulation/Options/ModuleSettings/EmulationModuleSettingsSection.Presentation.cs` et modifier `EmulationModuleSettingsSection.Persistence.cs`, utiliser `EmulationModuleSettingsSection.Layout.cs` et modifier `EmulationModuleSettingsSection.cs` pour exposer module, descriptions, champs et navigation fonctionnels, notifier la reconstruction, reprendre les commandes de sauvegarde/activation existantes et libérer les références de présentation à la fermeture.
    - [x] Créer `Theme/OS/Editions/Windows31/Views/SettingsFieldView.cs` : reconstruire les feuilles texte, nombres et IPv4 contraints, informations, choix, boutons radio, cases, curseurs et chemins avec les valeurs et actions existantes ; conserver les validations, états, unités, règles et désabonnements sans emprunter les conteneurs Base.
    - [x] Modifier `Theme/Base/Views/Controls/Emulation/Options/ModuleSettings/EmulationModuleSettingsSection.Presentation.cs`, `EmulationModuleSettingsSection.Layout.cs` et `EmulationModuleSettingsSection.cs` : exposer la liste des catégories réellement construites et la libérer ; corriger `Theme/OS/Editions/Windows31/Views/SettingsFieldView.cs` pour qualifier DockPanel.SetDock et borner la largeur des curseurs horizontaux avant compilation.
    - [x] Créer `Theme/OS/Editions/Windows31/Views/ModuleSettingsView.xaml` et `.xaml.cs` : reconstruire la navigation par machine et catégorie avec les champs décrits, leur traduction et la commande de création, dans une disposition propre à Windows31 ; préserver les contrôleurs et disposer toutes les feuilles dans finally.
    - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31SettingsFieldsTests.cs` : vérifier ensemble toutes les familles de champs et leurs actions avec des sources WPF autonomes, sans disque, DLL ni fenêtre externe.
    - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31ModuleSettingsTests.cs` : vérifier les catégories réelles, la modification, la création et la reconstruction des champs avec un module et un stockage de profils en mémoire ; ne démarrer aucun cœur ni fenêtre native.
    - [x] Créer temporairement `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31ModuleSettingsCaptureTests.cs` : rendre la fenêtre de paramètres depuis ces sources simulées à 1280×800 et 800×600 dans `artifacts/themes-review/screens/OS/Windows31/Default/{ModuleSettings,ModuleCpu,ModuleSettingsNarrow}.png`, examiner puis supprimer le scénario temporaire et libérer sa section dans finally.
    - [x] Modifier `docs/tasks/application-themes.md` après inventaire des contrôleurs spécialisés pour détailler leurs fichiers : gestion des cœurs, firmware, vidéo, entrées, stockage et dialogues de supports ; compléter toutes ces fonctions avant le raccordement public du module au bureau et le build.
    - [ ] Reconstruire les panneaux spécialisés avant le raccordement des options de modules.
      - [x] Modifier `Controllers/Emulation/Options/EmulationEmulatorManagementController.cs`, `Theme/Base/Views/Controls/Emulation/Options/EmulationCoreManagementPanel.cs` et `ModuleSettings/EmulationModuleSettingsSection.Presentation.cs`, créer `Theme/OS/Editions/Windows31/Views/CoreManagementView.xaml` et `.xaml.cs` et modifier `ModuleSettingsView.xaml.cs` : reprendre le choix des cœurs, versions, recherche, installation, annulation, progression et indisponibilités depuis le panneau fonctionnel existant dans Général, sans réutiliser son conteneur.
      - [x] Modifier `Theme/Base/Views/Controls/Emulation/Options/EmulationCoreManagementPanel.cs` : exposer la propriété CLR de statut au descripteur WPF utilisé par le pont de présentation.
      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31CoreManagementTests.cs` : vérifier le vrai contrôleur avec un gestionnaire simulé en mémoire, choix et verrouillage, recherche, versions, progression, installation, annulation et détachement sans réseau, fichiers ni fenêtre externe ; prévoir la capture temporaire séparément.
      - [x] Modifier `Theme/Base/Views/Dialogs/Emulation/FirmwareSelectionDialog.cs` et `Controllers/Emulation/Firmware/EmulationFirmwareManagementController.cs`, créer `Theme/OS/Editions/Windows31/Views/FirmwareSelectionView.xaml` et `.xaml.cs`, modifier `Windows31Desktop.Windows.cs` et `Windows31DialogKind.cs` : reconstruire la liste commune de ROM, états de compatibilité, sélection, actualisation, ouverture du dossier et validation, préserver les opérations annulables et attendre leur fin dans le finally.
      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31FirmwareTests.cs` : vérifier sélection autorisée/interdite, actualisation et ouverture du dossier, fermeture avec attente de la recherche, puis rendre les panneaux ROM/cœurs dans le bureau à 1280×800 vers `artifacts/themes-review/screens/OS/Windows31/Default/{FirmwareSelection,CoreManagement}.png` ; inspecter ces images.
      - [x] Supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31FirmwareTests.cs` après validation et supprimer ses éventuels artefacts temporaires hors captures demandées.
      - [x] Modifier `Theme/Base/Views/Controls/Emulation/Options/EmulationVideoProcessingSettingsSection.cs` et ses fichiers `Fields`, `Layout`, `Technologies`, `Updates`, créer `Theme/OS/Editions/Windows31/Views/VideoProcessingView.xaml` et `.xaml.cs` et modifier `ModuleSettingsView.xaml.cs` : reconstruire tous les choix de rendu et de traitements avec le profil existant, états shader et notifications, sans recopier les groupes Base ; préciser les autres fichiers de feuilles après lecture avant édition.
        - [x] Créer `Theme/Base/Views/Controls/Emulation/Options/EmulationVideoProcessingSettingsSection.Presentation.cs` et modifier `EmulationVideoProcessingSettingsSection.cs`, `ModuleSettings/EmulationModuleSettingsSection.Presentation.cs` : exposer les pages et points fonctionnels vidéo existants avec leurs libellés, signaler leur reconstruction et conserver la fin des éditions de curseurs.
        - [x] Créer `Theme/OS/Editions/Windows31/Views/VideoProcessingView.xaml` et `.xaml.cs`, modifier `SettingsFieldView.cs` et `ModuleSettingsView.xaml.cs` : afficher les pages vidéo en formulaire compact indépendant, relier les valeurs, règles, états shader et sauvegardes sans réutiliser les conteneurs Base.
        - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31VideoProcessingTests.cs` : vérifier les pages et paramètres de toutes les technologies déclarées, modifications du profil, reconstruction conditionnelle, chargement shader, sauvegarde des curseurs et détachement sans fenêtre, DLL ni fichier externe.
      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31VideoCaptureTests.cs` : rendre les pages affichage, CRT et image du panneau vidéo dans le bureau à 1280×800 vers `artifacts/themes-review/screens/OS/Windows31/Default/{VideoDisplay,VideoCrt,VideoImage}.png`, puis inspecter les images.
      - [x] Supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31VideoCaptureTests.cs` après inspection, avec ses artefacts temporaires hors images demandées.
      - [x] Modifier `Controllers/Emulation/Input/EmulationInputSettingsController.cs`, créer `Theme/OS/Editions/Windows31/Views/ModuleInputView.xaml` et `.xaml.cs` et modifier `ModuleSettingsView.xaml.cs` : exposer les modèles d’entrées et éditeurs existants, reconstruire clavier/souris/ports de manettes, dessins, zones analogiques et captures avec InputBindingsView ; compléter les fichiers précis des feuilles de dessins avant création.
        - [x] Modifier `Controllers/Emulation/Input/EmulationInputSettingsController.cs` et `Theme/Base/Views/Controls/Emulation/Options/ModuleSettings/EmulationModuleSettingsSection.Presentation.cs` : exposer les éditeurs clavier/souris/ports et leurs dessins existants, sans changer les règles ni la persistance.
        - [x] Modifier `Theme/Base/Views/Controls/Emulation/Input/EmulationControllerOutputEditor.cs`, créer `Theme/OS/Editions/Windows31/Views/ControllerOutputsView.cs` : reconstruire les choix de sorties, actualiser les périphériques à l’ouverture et conserver les associations existantes.
        - [x] Créer `Theme/OS/Editions/Windows31/Views/InputDrawingView.cs`, modifier `InputBindingsView.xaml.cs` : reproduire la feuille de dessin depuis ses données, relier les zones à la capture visible et arrêter le suivi des entrées dans finally à la fermeture.
        - [x] Créer `Theme/OS/Editions/Windows31/Views/ModuleInputView.xaml` et `.xaml.cs`, `ControllerPortView.xaml` et `.xaml.cs`, modifier `ModuleSettingsView.xaml.cs` : reconstruire clavier et souris, choix des ports/types/modèles, retours de disponibilité, sorties et réglages analogiques, dans une présentation indépendante ; conserver tous les champs décrits des modules.
        - [x] Modifier `Theme/Base/Views/Controls/Emulation/Input/InputBindingEditor.Presentation.cs` et `Theme/OS/Editions/Windows31/Views/InputBindingsView.xaml` et `.xaml.cs` : conserver l’en-tête fonctionnel des actions/touches et la légende des associations dans la nouvelle table compacte.
        - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31ModuleInputTests.cs` : vérifier choix des types/modèles/ports, persistance des associations, captures et détachement avec des gestionnaires en mémoire sans matériel ni fichier externe ; inscrire séparément les captures temporaires avant création.
      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31InputCaptureTests.cs` : afficher les panneaux clavier et ports avec un dessin existant, vérifier les contraintes à 1280×800 et 800×600, produire `artifacts/themes-review/screens/OS/Windows31/Default/{ModuleKeyboard,ControllerPorts,ControllerPortsNarrow}.png`, puis inspecter les captures.
      - [x] Modifier `Theme/OS/Editions/Windows31/Views/ControllerPortView.xaml` et `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31InputCaptureTests.cs` : réduire la hauteur de la table des associations pour conserver ses commandes visibles à 800×600, corriger la clé de titre de la scène et régénérer les trois images.
      - [x] Supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31InputCaptureTests.cs` après inspection et ses artefacts temporaires hors PNG demandés.
      - [x] Modifier `Controllers/Emulation/Storage/EmulationStorageSettingsController.cs` et `Theme/Base/Views/Controls/Emulation/Storage/EmulationStorageDeviceList.cs`, créer `Theme/OS/Editions/Windows31/Views/ModuleStorageView.xaml` et `.xaml.cs` et modifier `ModuleSettingsView.xaml.cs` : reconstruire la liste des périphériques, modèles, supports et commandes ajouter/configurer/retirer avec les mêmes possibilités matérielles.
        - [x] Modifier `Theme/Base/Views/Controls/Emulation/Storage/EmulationStorageDeviceList.cs`, `Controllers/Emulation/Storage/EmulationStorageSettingsController.cs` et `Theme/Base/Views/Controls/Emulation/Options/ModuleSettings/EmulationModuleSettingsSection.Presentation.cs` : exposer les lignes et résumés déjà calculés et leurs commandes autorisées, conserver les règles matérielles des modules et les valeurs de configuration.
        - [x] Créer `Theme/OS/Editions/Windows31/Views/ModuleStorageView.xaml` et `.xaml.cs`, modifier `ModuleSettingsView.xaml.cs` : présenter les périphériques en liste compacte avec sélection et commandes communes ajouter/configurer/retirer, conserver les informations de support et les capacités réelles.
        - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31ModuleStorageTests.cs` : vérifier disponibilité des commandes, changement de sélection et des lignes, conservation des supports, refus du retrait d’un périphérique permanent et application de la configuration via un gestionnaire en mémoire ; prévoir les captures et dialogues séparément.
      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31StorageCaptureTests.cs` : rendre une liste de supports variés et une liste longue en fenêtre étroite, produire `artifacts/themes-review/screens/OS/Windows31/Default/{ModuleStorage,ModuleStorageNarrow}.png`, vérifier les boutons visibles et inspecter les captures sans lancer de dialogue ni de cœur.
      - [x] Supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31StorageCaptureTests.cs` après inspection avec ses artefacts temporaires hors PNG demandés.
      - [x] Modifier ce suivi dans `docs/tasks/application-themes.md` après lecture des neuf dialogues réels de `Theme/Base/Views/Dialogs/Emulation/{CoreOptionsDialog.cs,Storage/AddStorageDeviceDialog.cs,Storage/ManagedStorageConfigurationDialog.cs,Storage/FloppyDriveConfigurationDialog.cs,Storage/HardDiskDriveConfigurationDialog.cs,Storage/CompactDiscDriveConfigurationDialog.cs,Storage/CartridgeSlotConfigurationDialog.cs,Storage/MemoryCardDirectoryConfigurationDialog.cs,Storage/MediaCreationDialog.cs}` : inscrire les fichiers exacts des présentations et leurs contrôles, y compris tape et options RAW gérées par ManagedStorage, avant leur reconstruction.
        - [x] Modifier `Theme/Common/ThemeDialogCompletion.cs`, `Theme/Base/Functions/Views/Emulation/Storage/StorageDialogUi.cs` et `Theme/OS/Editions/Windows31/Windows31Desktop.Windows.cs` : exposer les commandes fonctionnelles des pieds de dialogue et terminer les fenêtres non affichées par le relais modal existant ; conserver les valeurs et la fermeture native pour Base/Studio.
        - [x] Modifier `Theme/Base/Views/Dialogs/Emulation/CoreOptionsDialog.cs` et `Storage/AddStorageDeviceDialog.cs`, créer `Theme/OS/Editions/Windows31/Views/CoreOptionsView.cs` et `AddStorageDeviceView.cs`, modifier `Windows31DialogKind.cs` et `Windows31Desktop.Windows.cs` : reconstruire les catégories/options du cœur et le choix des seuls types de périphériques disponibles à partir des contrôles et libellés existants.
        - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31CoreDialogTests.cs` : vérifier les options texte/choix et les types de supports autorisés, résultat du relais, détachement et fermeture de vraies fenêtres fonctionnelles non affichées ; vérifier aussi le pied de dialogue en modalité native sans fenêtre visible et la disparition des handles ; capturer `artifacts/themes-review/screens/OS/Windows31/Default/{CoreOptions,AddStorageDevice}.png` puis inspecter.
        - [x] Modifier `src/RetroMediaLab.App/Resources/*/Emulation/EmulationCore.resx`, clé `Emulation.Core.NameOptions` : remplacer l’ambigu « Core options » traduit en « Options de base » par « Emulation core options » dans la base anglaise et ses traductions Argos revues dans les 28 autres cultures ; régénérer les deux captures du dialogue avec le titre français « Options du cœur d’émulation ».
        - [x] Supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31CoreDialogTests.cs` après validation avec ses artefacts temporaires hors captures demandées.
        - [x] Créer `Theme/OS/Editions/Windows31/Views/StorageFieldsView.cs` : construire les lignes compactes des champs déclarés avec leurs libellés traduits et contrôles fonctionnels, puis détacher leurs liaisons ; réutiliser ce formulaire OS pour les dialogues de supports.
        - [x] Modifier `Theme/Base/Views/Dialogs/Emulation/Storage/ManagedStorageConfigurationDialog.cs`, créer `Theme/OS/Editions/Windows31/Views/ManagedStorageView.cs` : reconstruire les champs et groupes déclarés, y compris tape/RAW, respecter VisibleWhenFieldId/VisibleWhenValue et les valeurs des contrôles d’origine, sans déplacer les cartes Base.
        - [x] Modifier `Theme/Base/Views/Dialogs/Emulation/Storage/CompactDiscDriveConfigurationDialog.cs` et `CartridgeSlotConfigurationDialog.cs`, créer `Theme/OS/Editions/Windows31/Views/CompactDiscSettingsView.cs` et `CartridgeSettingsView.cs` : conserver vitesse/cache/erreurs, choix supplémentaires et options propres au module dans des formulaires OS indépendants.
        - [x] Modifier `Theme/OS/Editions/Windows31/Views/SettingsFieldView.cs` : présenter le DisplayName des choix de module dans les listes propres à OS, sans reprendre leur ItemTemplate Base.
        - [x] Modifier `Theme/OS/Editions/Windows31/Views/ManagedStorageView.cs` : remplacer l’expandeur moderne par un groupe à case de dévoilement Windows 3.1 et conserver sa visibilité conditionnelle.
        - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31StorageDialogTests.cs` : vérifier les liaisons et groupes conditionnels tape/RAW, les options CD/cartouche, leurs résultats et leur détachement ; produire les captures `artifacts/themes-review/screens/OS/Windows31/Default/{ManagedStorage,CompactDisc,Cartridge}.png`, inspecter puis supprimer ce test temporaire.
        - [x] Modifier `Theme/Base/Views/Dialogs/Emulation/Storage/MemoryCardDirectoryConfigurationDialog.cs`, créer `Theme/OS/Editions/Windows31/Views/MemoryCardDirectoryView.cs` : relier le dossier, son parcours, sa validation et le retrait du support aux actions existantes ; conserver le contrôle et la création du chemin dans la fenêtre fonctionnelle.
        - [x] Réorganiser les vues Windows 3.1 selon les catégories de Base avant de reprendre les dialogues de disquette.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/AcquisitionOptionsFields.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Read/AcquisitionOptionsFields.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/AcquisitionOptionsView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Read/AcquisitionOptionsView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/AcquisitionView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Read/AcquisitionView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/AcquisitionView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Read/AcquisitionView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/AddStorageDeviceView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Emulation/Storage/AddStorageDeviceView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/BusinessDialogView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Common/BusinessDialogView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/CartridgeSettingsView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Emulation/Storage/CartridgeSettingsView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ClassificationView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Classification/ClassificationView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ClassificationView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Classification/ClassificationView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/CompactDiscSettingsView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Emulation/Storage/CompactDiscSettingsView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ControllerOutputsView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Input/ControllerOutputsView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ControllerPortView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Input/ControllerPortView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ControllerPortView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Input/ControllerPortView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ControllersOptionsView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Options/ControllersOptionsView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ControllersOptionsView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Options/ControllersOptionsView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ConversionConflictView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Conversion/ConversionConflictView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ConversionFormatRow.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Conversion/ConversionFormatRow.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ConversionOptionsFields.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Conversion/ConversionOptionsFields.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ConversionOptionsView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Conversion/ConversionOptionsView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ConversionView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Conversion/ConversionView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ConversionView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Conversion/ConversionView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/CoreManagementView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Options/CoreManagementView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/CoreManagementView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Options/CoreManagementView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/CoreOptionsView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Emulation/CoreOptionsView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/EmulationPreferencesView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Windows/EmulationPreferences/EmulationPreferencesView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/EmulationPreferencesView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Windows/EmulationPreferences/EmulationPreferencesView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/EmulationView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Machine/EmulationView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/EmulationView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Machine/EmulationView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/EnginesOptionsView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Options/EnginesOptionsView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/EnginesOptionsView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Options/EnginesOptionsView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ExplorerDetailsView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Explorer/ExplorerDetailsView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ExplorerIssuesView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Explorer/ExplorerIssuesView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ExplorerView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Explorer/ExplorerView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ExplorerView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Explorer/ExplorerView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/FirmwareSelectionView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Emulation/FirmwareSelectionView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/FirmwareSelectionView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Emulation/FirmwareSelectionView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/GeneralOptionsView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Options/GeneralOptionsView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/GeneralOptionsView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Options/GeneralOptionsView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/HardwareOptionsView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Options/HardwareOptionsView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/HardwareOptionsView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Options/HardwareOptionsView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/HardwareUnavailableView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Hardware/HardwareUnavailableView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/InputBindingsView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Input/InputBindingsView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/InputBindingsView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Input/InputBindingsView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/InputDrawingView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Input/InputDrawingView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/InspectorView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Inspector/InspectorView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/InspectorView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Inspector/InspectorView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/LogsOptionsView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Options/LogsOptionsView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/LogsOptionsView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Options/LogsOptionsView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/MachineView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Machine/MachineView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/MachineView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Machine/MachineView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ManagedStorageView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Emulation/Storage/ManagedStorageView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/MemoryCardDirectoryView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Emulation/Storage/MemoryCardDirectoryView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ModuleInputView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Input/ModuleInputView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ModuleInputView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Input/ModuleInputView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ModuleSettingsView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Windows/EmulationModuleOptions/ModuleSettingsView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ModuleSettingsView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Windows/EmulationModuleOptions/ModuleSettingsView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ModuleStorageView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Storage/ModuleStorageView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ModuleStorageView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Storage/ModuleStorageView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/NetworkView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Network/NetworkView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/PreferencesView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Windows/Preferences/PreferencesView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ProfileNameView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Profiles/ProfileNameView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ProfilesOptionsView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Options/ProfilesOptionsView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ProfilesOptionsView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Options/ProfilesOptionsView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ReadConflictView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Read/ReadConflictView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/SettingsFieldView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Options/SettingsFieldView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/StorageFieldsView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Storage/StorageFieldsView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/TerminalView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Console/TerminalView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/TerminalView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Console/TerminalView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ThemeOptionsView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Options/ThemeOptionsView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ThemeOptionsView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Options/ThemeOptionsView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ToolsView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Tools/ToolsView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/ToolsView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Tools/ToolsView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/VideoProcessingView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Video/VideoProcessingView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/VideoProcessingView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Emulation/Video/VideoProcessingView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/VisualizationOverviewView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Visualization/VisualizationOverviewView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/VisualizationView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Visualization/VisualizationView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/VisualizationView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Visualization/VisualizationView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Windows31BoundFields.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Common/Windows31BoundFields.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/WriteOptionsFields.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Write/WriteOptionsFields.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/WriteOptionsView.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Write/WriteOptionsView.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/WriteView.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Write/WriteView.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/WriteView.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Write/WriteView.xaml.cs` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Controls/NavigationControls.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Common/Styles/NavigationControls.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Controls/ScrollControls.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Common/Styles/ScrollControls.xaml` en conservant le contenu, les classes et leurs liaisons.
          - [x] Modifier `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Controls.xaml` : actualiser les chemins des deux dictionnaires de styles déplacés dans Views/Controls/Common/Styles.
          - [x] Modifier `docs/tasks/application-themes.md` : actualiser les chemins des vues restantes vers leurs catégories et consigner la vérification groupée de compilation/navigation après déplacement, sans créer de commit. Vérification : compilation des vues déplacées et 167 tests de thèmes réussis.
        - [x] Réorganiser aussi les fichiers de la racine Windows 3.1 par responsabilité.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31ApplicationDefinition.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Contracts/Windows31ApplicationDefinition.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Document.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Contracts/Windows31Document.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Palette.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Contracts/Windows31Palette.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31ApplicationId.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Enums/Windows31ApplicationId.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31ColorRole.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Enums/Windows31ColorRole.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31ColorScheme.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Enums/Windows31ColorScheme.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31DialogKind.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Enums/Windows31DialogKind.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31DialogMode.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Enums/Windows31DialogMode.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31ResizeEdges.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Enums/Windows31ResizeEdges.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31WindowState.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Enums/Windows31WindowState.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Metrics.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Constants/Windows31Metrics.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31PaletteCatalog.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Dictionaries/Windows31PaletteCatalog.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31PaletteResources.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Services/Appearance/Windows31PaletteResources.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31FileImages.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Services/Images/Windows31FileImages.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31WindowPlacement.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/ViewModels/Windows31WindowPlacement.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31MediaApplications.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Factories/Windows31MediaApplications.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31ContentPresenter.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Common/Windows31ContentPresenter.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31FolderBranch.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Common/Windows31FolderBranch.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Frame.Interactions.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Windows/Windows31Frame.Interactions.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Frame.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Windows/Windows31Frame.xaml` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Frame.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Windows/Windows31Frame.xaml.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31DragOutline.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Windows/Windows31DragOutline.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31SystemMenu.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Controls/Windows/Windows31SystemMenu.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31DialogForm.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Dialogs/Common/Windows31DialogForm.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Desktop.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Windows/Desktop/Windows31Desktop.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Desktop.Devices.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Windows/Desktop/Windows31Desktop.Devices.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Desktop.Dialogs.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Windows/Desktop/Windows31Desktop.Dialogs.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Desktop.Machines.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Windows/Desktop/Windows31Desktop.Machines.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Desktop.Windows.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Windows/Desktop/Windows31Desktop.Windows.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31ProgramManager.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Windows/Desktop/Windows31ProgramManager.xaml` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31ProgramManager.xaml.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Windows/Desktop/Windows31ProgramManager.xaml.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Workspace.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Views/Windows/Desktop/Windows31Workspace.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Controls.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Styles/Windows31Controls.xaml` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31DefaultColors.cs` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Styles/Windows31DefaultColors.cs` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Icons.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Styles/Windows31Icons.xaml` en conservant les classes et leur contenu.
          - [x] Déplacer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Windows31Typography.xaml` vers `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Styles/Windows31Typography.xaml` en conservant les classes et leur contenu.
          - [x] Modifier `Theme/OS/Editions/Windows31/{Constants/Windows31Metrics.cs,Styles/Windows31Controls.xaml,Styles/Windows31Icons.xaml,Styles/Windows31Typography.xaml,Views/Controls/Windows/Windows31Frame.xaml,Views/Windows/Desktop/Windows31ProgramManager.xaml}` : actualiser les références WPF aux dictionnaires, icônes et polices après déplacement.
          - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/Windows31IconTests.cs` : actualiser le chemin du dictionnaire des icônes déplacé dans Styles.
          - [x] Modifier `docs/tasks/application-themes.md` : actualiser les chemins restants et consigner le résultat de la vérification groupée du chargement des ressources, des fenêtres et de la navigation. Vérification : 166 scénarios réussis au premier passage ; le test des icônes a été corrigé vers son nouveau dictionnaire, puis réussi séparément.
        - [x] Modifier `Theme/Base/Views/Dialogs/Emulation/Storage/FloppyDriveConfigurationDialog.cs` et `Theme/Base/Views/Controls/Emulation/Storage/FloppyCreationSettingsView.cs`, créer `Theme/OS/Editions/Windows31/Views/Dialogs/Emulation/Storage/FloppySettingsView.cs` et `Theme/OS/Editions/Windows31/Views/Controls/Emulation/Storage/FloppyCreationView.cs`, modifier `Theme/OS/Editions/Windows31/Views/Controls/Emulation/Storage/StorageFieldsView.cs` : reprendre les modèles autorisés, comportement et champs du module, puis séparer format de disquette, format d’image et destination avec actualisation et parcours d’origine.
        - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31FloppyDialogTests.cs` : vérifier les modèles et capacités déclarés, vitesse et champs propres au module, séparation des formats et actualisation de destination, puis validation/retrait du dossier mémoire ; capturer `artifacts/themes-review/screens/OS/Windows31/Default/{FloppySettings,FloppyCreation,MemoryCardDirectory}.png`, inspecter, supprimer le test et son dossier temporaire après utilisation.
        - [x] Modifier `Theme/Base/Views/Dialogs/Emulation/Storage/MediaCreationDialog.cs`, créer `Theme/OS/Editions/Windows31/Views/Dialogs/Emulation/Storage/MediaCreationView.cs`, modifier `Theme/OS/Editions/Windows31/Views/Dialogs/Common/BusinessDialogView.cs` pour permettre le contrôle de l’annulation pendant la création : conserver créer/créer-et-monter, état occupé, verrouillage après création, chemin et statut ; attendre la création en cours avant fermeture et détacher toutes les présentations dans finally.
        - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31MediaCreationTests.cs` : vérifier création unique, montage distinct, état occupé, fermeture différée et dialogue natif Base sans affichage visible ; rendre `artifacts/themes-review/screens/OS/Windows31/Default/MediaCreation.png`, inspecter puis supprimer ce test temporaire.
        - [x] Modifier `Theme/Base/Views/Dialogs/Emulation/Storage/HardDiskDriveConfigurationDialog.cs`, créer `Theme/OS/Editions/Windows31/Views/Dialogs/Emulation/Storage/HardDiskSettingsView.cs`, modifier `Theme/OS/Editions/Windows31/Views/Controls/Emulation/Options/SettingsFieldView.cs` : reconstruire utilisation/création, formats, préparations, tailles autorisées/personnalisées, préallocation, géométrie et champs du module ; relier aux actions et calculs existants, conserver les choix uniques en informations.
        - [x] Modifier `Theme/Base/Views/Dialogs/Emulation/Storage/HardDiskDriveConfigurationDialog.cs` : remplacer le libellé Présentation par le libellé partagé de format de fichier image déjà traduit dans toutes les cultures.
        - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31HardDiskDialogTests.cs` : vérifier contraintes de formats, tailles et préparations, affichage informatif des choix uniques, géométrie et préallocation, création/retrait et fermeture après suppression asynchrone ; produire `artifacts/themes-review/screens/OS/Windows31/Default/{HardDiskSettings,HardDiskGeometry,FixedMemoryCard}.png`, inspecter, supprimer le test et ses fichiers temporaires après utilisation.
        - [x] Modifier `Controllers/Emulation/Storage/EmulationStorageSettingsController.cs`, `Theme/Base/Views/Controls/Emulation/Machine/MachineController.cs`, `Theme/Base/Views/Dialogs/Emulation/Storage/FloppyDriveConfigurationDialog.cs`, `Theme/Base/Views/Dialogs/Emulation/Storage/MediaCreationDialog.cs`, `Services/Windows/WpfWindowNavigationService.cs`, `Theme/OS/Editions/Windows31/Views/Windows/Desktop/Windows31Desktop.Windows.cs` et `Theme/OS/Editions/Windows31/Enums/Windows31DialogKind.cs` : acheminer les neuf fenêtres et la création tape par le relais commun, préserver leur propriétaire fonctionnel et l’imbrication, attendre la fin des opérations et la destruction effective.
        - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31StorageRoutingTests.cs` : ouvrir les neuf dialogues via le bureau, vérifier leurs vues propres, résultat et fermeture sans HWND natif ; exercer une création imbriquée et le nettoyage du relais pendant une opération en cours, puis supprimer ce test temporaire.
        - [x] Modifier `docs/tasks/application-themes.md` après inspection des fabriques de fichiers/dossiers/messages : conserver les interfaces de services existantes et étendre leur relais de fenêtres ; les appels directs supplémentaires à migrer sont inscrits ci-dessous. Les neuf dialogues et le relais passent 170 tests de thèmes ; les 116 scénarios de navigation existants passent également.
        - [x] Reconstruire les messages système Windows 3.1 par le relais existant, avec repli natif inchangé pour Base/Studio.
          - [x] Créer `Theme/Base/Views/Dialogs/Common/MessageDialog.cs` : conserver message, titre, boutons, icône et réponse par défaut des appels existants ; terminer la requête par ThemeDialogCompletion et rendre sa réponse au service sans afficher cette fenêtre source.
          - [x] Modifier `Services/Dialogs/WpfMessageDialogService.cs` : acheminer les requêtes vers TryShowThemeWindow avec leur propriétaire logique, conserver les surcharges utilisées et MessageBoxResult ; appeler MessageBox natif uniquement lorsque le thème ne prend pas la requête en charge, fermer la fenêtre source dans finally.
          - [x] Incorporer les quatre icônes de messages depuis les ressources originales Windows 3.10.
            - [x] Créer `artifacts/themes-review/temporary-win31-messages.json` : télécharger l’image PCjs `https://harddisks.pcjs.org/pcx86/20mb/PCDOS330-WIN310-VGA.json` déjà utilisée pour les icônes historiques.
            - [x] Créer `Theme/OS/Editions/Windows31/Assets/Icons/{message-information,message-warning,message-error,message-question}.ico` : reconstruire les ressources originales depuis les secteurs de l’image, sans exécuter les binaires.
            - [x] Modifier `docs/design/themes-rebuild.md` : consigner origine, identifiants de ressources et empreintes des icônes.
            - [x] Supprimer `artifacts/themes-review/temporary-win31-messages.json` après extraction et vérification.
          - [x] Modifier `Resources/*/{Actions,Controllers}.resx`, `Presenters/Explorer/ExplorerDetailsPresenter.cs`, `Theme/Base/Views/Controls/Options/ControllerPresentation/{GameInputDisplayFormatter,GameInputDescriptorPresenter}.cs` : déplacer les traductions Oui/Non existantes vers Common.Yes/Common.No dans Actions et actualiser les trois consommateurs ; ajouter Common.OK invariant dans la base commune uniquement, sans dupliquer ces entrées.
          - [x] Créer `Theme/OS/Editions/Windows31/Views/Dialogs/Common/MessageView.cs`, modifier `Theme/OS/Editions/Windows31/{Styles/Windows31Icons.xaml,Views/Windows/Desktop/Windows31Desktop.Windows.cs,Views/Controls/Windows/Windows31Frame.xaml.cs,Views/Controls/Windows/Windows31Frame.Interactions.cs,Enums/Windows31DialogKind.cs}` : reproduire le dialogue historique, ses boutons, touche Entrée/Échap et réponse par défaut ; gérer l’imbrication et les images d’origine.
          - [x] Modifier `Theme/Base/Views/Dialogs/Common/MessageDialog.cs`, `Theme/OS/Editions/Windows31/Views/Dialogs/Common/MessageView.cs` et `Resources/*/Actions.resx` : prendre aussi en charge AbortRetryIgnore, RetryCancel et CancelTryContinue effectivement présents dans le framework .NET 10 installé ; traduire Abort/Retry/Ignore/TryAgain avec Argos puis revoir les libellés dans toutes les cultures, dimensionner les boutons égaux selon leur texte et conserver les annulations permises.
          - [x] Modifier `Theme/OS/Editions/Windows31/{Views/Dialogs/Common/MessageView.cs,Views/Windows/Desktop/Windows31Desktop.Windows.cs}` : ajuster la taille à la mesure réelle des contrôles après application des styles, supprimer le défilement des messages courts et conserver celui des messages longs bornés au bureau.
          - [x] Modifier `Theme/OS/Editions/Windows31/Views/Windows/Desktop/Windows31Desktop.Dialogs.cs` et `Windows31Desktop.Windows.cs` : identifier les fenêtres fonctionnelles par leur instance afin de permettre deux messages imbriqués de même type, conserver les dialogues directs uniques et restaurer exactement le parent à la fermeture.
          - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31MessageTests.cs` : vérifier résultats, annulation/default, imbrication et repli natif sans affichage visible, rendre les captures `artifacts/themes-review/screens/OS/Windows31/Default/Message*.png`, inspecter puis supprimer le test et ses artefacts temporaires.
        - [x] Reconstruire les sélecteurs de fichiers et dossiers Windows 3.1 en conservant les objets et paramètres de parcours existants.
          - [x] Modifier `Resources/*/Common.resx` : ajouter FileDialog.Open, SaveAs, SelectFolder, Directories, Drives, FileTypes, AllFiles, OverwriteConfirm et InvalidName avec Argos puis revue dans les 30 catalogues ; réutiliser Read.FileName, Emulation.Storage.ReadOnly et les actions/erreurs existantes. Les filtres, chemins et extensions restent des données des appels existants.
          - [x] Créer `Enums/Services/Dialogs/FileSelectionKind.cs`, `Contracts/Services/Dialogs/FileSelectionFilter.cs` et `Theme/Base/Views/Dialogs/Common/FileSelectionDialog.cs` : exploiter les paramètres OpenFileDialog/SaveFileDialog/OpenFolderDialog déjà fournis, filtrer et naviguer dans les dossiers/lecteurs, valider noms/extensions/lecture seule/écrasement, publier les valeurs sélectionnées sans remplacer les services média ; suivre et terminer toute énumération en cours avant fermeture.
          - [x] Modifier `Services/Dialogs/WpfFileDialogService.cs` et `Services/Windows/WpfWindowNavigationService.cs` : présenter la requête dans le thème par le relais existant, reporter chemin/index de filtre/lecture seule dans les objets déjà utilisés, conserver le repli natif et les interfaces IFileDialogService/OpenFileRequest/SaveFileRequest/SelectFolderRequest, attendre les opérations et fermer dans finally.
          - [x] Créer `Theme/OS/Editions/Windows31/Views/Dialogs/Common/FileSelectionView.xaml` et `.xaml.cs`, modifier `Theme/OS/Editions/Windows31/{Views/Windows/Desktop/Windows31Desktop.Windows.cs,Enums/Windows31DialogKind.cs}` : reproduire les listes fichiers/dossiers, lecteurs, nom, filtre et commandes du dialogue historique ; conserver navigation clavier/double-clic et détachement de tous les abonnements.
          - [x] Modifier `Theme/OS/Editions/Windows31/Views/Dialogs/Common/FileSelectionView.xaml` et `.xaml.cs`, et `Views/Windows/Desktop/Windows31Desktop.Windows.cs` : après inspection des captures, donner toute la largeur au parcours de dossier sans colonne de fichiers vide, afficher le chemin sélectionné et adapter sa taille ; conserver les deux listes séparées pour ouvrir/enregistrer.
          - [x] Modifier `Contracts/Services/Dialogs/FileSelectionFilter.cs` et `Theme/Base/Views/Dialogs/Common/FileSelectionDialog.cs` : conserver les règles WPF d’extensions (DefaultExt en premier, recherche des extensions du filtre pour un fichier existant, noms sans extension), employer les motifs Win32 et rendre la confirmation d’écrasement injectable comme les autres dialogues fonctionnels pour vérifier refus/accord sans boîte native ; conserver le chemin de production vers le service de messages thémé.
          - [x] Modifier `Theme/Base/Views/Dialogs/Common/FileSelectionDialog.cs`, `Contracts/Services/Dialogs/FileSelectionFilter.cs` et `Theme/OS/Editions/Windows31/Views/Dialogs/Common/FileSelectionView.xaml` : attendre aussi la tâche de fermeture différée, retirer l’accès d’extension devenu inutile et empêcher le chemin courant de renvoyer à la ligne ; compléter puis supprimer le scénario temporaire après le contrôle final.
          - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31FileSelectionTests.cs` : couvrir parcours et filtres, ouverture/enregistrement/dossier, lecture seule et extensions, confirmations, erreurs et annulation, fermeture pendant une énumération et repli natif ; rendre les captures `artifacts/themes-review/screens/OS/Windows31/Default/FileSelection*.png`, inspecter puis supprimer test et arborescence temporaire.
        - [x] Acheminer tous les appels natifs existants par ces façades avant de considérer le thème complet.
          - [x] Modifier `App.xaml.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Options/Controllers/GeneralOptionsController.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Options/Controllers/HostToolsOptionsController.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Options/Controllers/HardwareOptionsController.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Options/Controllers/ProfileOptionsController.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Options/Controllers/Updates/UpdateOptionsController.Installation.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Services/Emulation/HardDiskDeletionService.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Theme/Base/Views/Windows/Updates/UpdatesWindow.xaml.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Theme/Base/Views/Windows/Logs/LogHistoryWindow.xaml.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Theme/Base/Views/Windows/Tools/GwToolWindow.xaml.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Theme/Base/Views/Windows/Preferences/PreferencesWindow.xaml.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Theme/Base/Views/Windows/EmulationPreferences/EmulationPreferencesWindow.xaml.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Theme/Base/Views/Dialogs/Profiles/ProfileNameWindow.xaml.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Theme/Base/Views/Dialogs/Emulation/Storage/MemoryCardDirectoryConfigurationDialog.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Theme/Base/Views/Dialogs/Emulation/Storage/MediaCreationDialog.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Theme/Base/Views/Dialogs/Emulation/Storage/HardDiskDriveConfigurationDialog.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Theme/Base/Views/Dialogs/Emulation/Storage/FloppyDriveConfigurationDialog.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Theme/Base/Views/Controls/Emulation/Storage/FloppyCreationSettingsView.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Theme/Base/Views/Controls/Emulation/Machine/MachineController.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Theme/Base/Views/Controls/Emulation/Options/EmulationPreferencesSectionSettingsFunctions.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Theme/Base/Views/Controls/Emulation/Options/EmulationPreferencesSectionConfigurationFunctions.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Theme/Base/Views/Controls/Emulation/Options/ModuleSettings/EmulationModuleSettingsSection.Fields.cs` : remplacer ses appels natifs directs par les façades communes, en conservant paramètres, confirmations, résultats et traitements existants ; garder le propriétaire logique et le repli natif sans thème actif.
          - [x] Modifier `Theme/Base/Views/Controls/Emulation/Options/EmulationPreferencesSectionSettingsFunctions.cs` : ajouter explicitement l’import System.Windows requis par le propriétaire de la sélection de dossier ; corriger la compilation avant le contrôle groupé.
          - [x] Modifier `docs/tasks/application-themes.md` : consigner les contrôles groupés, supprimer les scénarios et fichiers temporaires utilisés, puis reprendre le raccordement de la fenêtre complète des modules sans déclarer les autres thèmes terminés.
        - [x] Modifier `docs/tasks/application-themes.md` avant création des scénarios des neuf dialogues : détailler les tests utiles et leurs fichiers temporaires, données et captures à supprimer, couvrir choix/annulation/validation/création et fermeture effective sans présenter un contrôle restreint comme une validation de tous les modules.
      - [x] Modifier `Theme/Base/Views/Controls/Emulation/Options/EmulationModuleSettingsSection.cs`, `ModuleSettings/EmulationModuleSettingsSection.Layout.cs` et `Theme/OS/Editions/Windows31/Views/Windows/EmulationModuleOptions/ModuleSettingsView.xaml.cs` : partager le chargement initial entre Loaded natif et présentation propre, exposer exactement la règle de champs affichés existante (dossiers par défaut exclus du Général), et l’appliquer dans la nouvelle vue sans déclarer ces champs manquants.
      - [x] Modifier `Theme/Base/Views/Windows/EmulationModuleOptions/EmulationModuleOptionsWindow.xaml.cs`, `Services/Windows/WpfWindowNavigationService.cs`, `Theme/OS/Editions/Windows31/Views/Windows/Desktop/Windows31Desktop.Windows.cs`, `Theme/OS/Editions/Windows31/Enums/Windows31DialogKind.cs` et `ModuleSettingsView.xaml.cs` : raccorder la fenêtre complète au bureau, charger la configuration initiale sans ContentRendered natif, attendre la destruction de ses contrôleurs et ne laisser aucun panneau ni champ non présenté ; inscrire les scénarios de validation réelle et leurs captures avant création.
        - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/Windows31ModuleSettingsTests.cs` : vérifier que les dossiers fonctionnels restent affichés et modifiables et que les dossiers par défaut exclus du Général ne sont ni affichés ni signalés manquants, avec les configurations en mémoire existantes.
        - [x] Modifier `Theme/Base/Views/Controls/Emulation/Options/EmulationModuleSettingsSection.cs` : attendre son chargement initial propre dans DisposeAsync avant de libérer les contrôleurs, y compris si Loaded a démarré mais que ContentRendered de la fenêtre n’a pas encore eu lieu ; couvrir les deux chemins dans le scénario temporaire.
        - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31ModuleWindowTests.cs` : ouvrir la fenêtre fonctionnelle complète via le bureau, vérifier configuration initiale et chargement partagé, catégories, fermeture pendant un chargement bloqué, attente de libération et absence de HWND source ; capturer `artifacts/themes-review/screens/OS/Windows31/Default/IntegratedModuleSettings.png`, inspecter puis supprimer le scénario temporaire et ses données éventuelles.

        - [x] Valider les réglages des modules réels et corriger les défauts bloquant leur présentation.
          - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31AllModulesTests.cs` : charger les 24 modules présents avec espaces de travail isolés, parcourir machines et onglets, vérifier les champs manquants et rendre les captures dans `artifacts/themes-review/screens/OS/Windows31/Modules/`. Premier passage : 23 modules réussis, Nintendo échoue dans Engine.DescribeStorageSettings lorsqu’aucun adaptateur n’est sélectionné ; le chemin est inchangé par rapport au code d’origine.
          - [x] Modifier `src/RetroMediaLab.Emulation.Nintendo/Common/Services/Engine.cs` : utiliser le repli matériel StorageSettingsFunctions.Describe(current) déjà prévu avec adaptateur facultatif, comme les autres modules, au lieu de forcer Adapter(current) sur une configuration sans cœur ; conserver le chemin de l’adaptateur sélectionné.
          - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/Nintendo/NintendoEmulatorCatalogTests.cs` : ajouter un contrôle autonome du stockage de chaque modèle sans cœur sélectionné, sans fichiers ni cœur lancé, puis compiler les projets concernés par ce test local.
          - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31AllModulesTests.cs` : pour Nintendo, charger l’assembly recompilé dans `src/RetroMediaLab.Emulation.Nintendo/bin/Debug/net10.0/` sans remplacer le paquet du build utilisateur ; reprendre le parcours de ses machines et inspecter les captures représentatives.
          - [x] Supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31AllModulesTests.cs` et ses données temporaires après validation, conserver uniquement les captures demandées et consigner les comptes de machines/onglets effectivement parcourus ; ne pas présenter ce contrôle de réglages comme un démarrage des cœurs.

      - [x] Raccorder les informations calculées des onglets matériels sans reprendre leurs conteneurs Base.
        - [x] Modifier `Theme/Base/Views/Controls/Emulation/Options/EmulationModuleHardwareSettingsSection.cs`, `ModuleSettings/EmulationModuleSettingsSection.Presentation.cs`, `ModuleSettings/EmulationModuleSettingsSection.Layout.cs` et `Theme/OS/Editions/Windows31/Views/Windows/EmulationModuleOptions/ModuleSettingsView.xaml.cs` : exposer le total RAM déjà calculé, le réinitialiser au changement de machine, le relier dans une ligne propre au thème et suivre aussi les choix radio via AttachValueChanged existant ; libérer la liaison avec les autres champs.
        - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/Windows31ModuleSettingsTests.cs` : vérifier que le total calculé reste visible et évolue avec un choix RAM radio, sans nouveau calcul propre au thème et avec des données en mémoire.
        - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31MemoryReviewTests.cs` : contrôler le total d’Acorn après changement de capacité réelle, régénérer `artifacts/themes-review/screens/OS/Windows31/Modules/acorn/Ram.png`, inspecter puis supprimer ce scénario et son espace de travail isolé.
  - [x] Reconstruire les fenêtres globales restantes dans Windows 3.1.
    - [x] Reconstruire les erreurs détaillées et À propos en conservant toutes les données existantes.
      - [x] Modifier `Theme/Base/Views/Dialogs/Common/CommonErrorDialog.cs` et `Theme/Base/Views/Windows/About/AboutWindow.xaml.cs` : exposer leurs données calculées, acheminer l’erreur par le propriétaire logique et ThemeDialogCompletion, conserver la présentation native Base et fermer dans finally.
      - [x] Créer `Theme/OS/Editions/Windows31/Views/Dialogs/Common/ErrorView.cs` et `Views/Windows/About/AboutView.cs` : reconstruire message, détails, types de supports requis, version et composants sans reprendre les conteneurs Base ; utiliser icône historique, textes traduits existants et commandes clavier.
      - [x] Modifier `Theme/OS/Editions/Windows31/Views/Windows/Desktop/Windows31Desktop.Windows.cs` et `Enums/Windows31DialogKind.cs` : présenter ces deux fenêtres par le relais modal, avec leur titre et fermeture réels.
      - [x] Modifier `Resources/*/Common.resx` : revoir Common.MediaKind.Tape dans toutes les langues avec le contexte Argos « Magnetic tape » ; corriger notamment « Rubrique » en français et conserver une seule clé commune.
      - [x] Modifier `Theme/OS/Editions/Windows31/Views/Dialogs/Common/BusinessDialogView.cs`, `ErrorView.cs` et `Views/Windows/Desktop/Windows31Desktop.Windows.cs` : dimensionner la hauteur de l’erreur d’après ses contrôles réellement mesurés, borner les messages longs et titrer la liste des supports avec Emulation.Error.RequiredMedia existant.
      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31InformationWindowsTests.cs` : vérifier données intégrales, fermeture sans HWND source, clavier et captures `artifacts/themes-review/screens/OS/Windows31/Default/{DetailedError,About}.png`, inspecter puis supprimer le scénario temporaire et ses données éventuelles.
    - [x] Reconstruire le lecteur de journaux.
      - [x] Modifier `Theme/Base/Views/Windows/Logs/LogHistoryWindow.xaml.cs` : exposer liste, contenu et export existants, suivre les lectures/écritures possédées, annuler les lectures puis attendre toutes les opérations avant fermeture ; conserver filtres, ordre, sélection et export.
      - [x] Créer `Theme/OS/Editions/Windows31/Views/Windows/Logs/LogHistoryView.xaml` et `.xaml.cs` : reconstruire le lecteur en listes/texte à chasse fixe avec menu Fichier et export, relier les feuilles existantes et libérer leurs liaisons.
      - [x] Modifier `Theme/OS/Editions/Windows31/Views/Windows/Desktop/Windows31Desktop.Windows.cs`, `Enums/Windows31DialogKind.cs` et `Services/Windows/WpfWindowNavigationService.cs` : présenter le lecteur dans son cadre propre, conserver titre et fermeture et attendre ses opérations dans le nettoyage.
      - [x] Modifier `Theme/Base/Views/Windows/Logs/LogHistoryWindow.xaml.cs` : injecter le service IFileDialogService déjà existant pour l’export, avec WpfFileDialogService par défaut ; conserver SaveFileRequest et ses paramètres pour tester le chemin d’export sans boîte native.
      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31LogHistoryTests.cs` : vérifier ordre/filtre, lecture et changement de sélection, export raccordé, annulation/fermeture et absence de HWND source, produire `artifacts/themes-review/screens/OS/Windows31/Default/LogHistory.png`, inspecter puis supprimer scénario et journaux temporaires. Deux scénarios réussis ; capture examinée et scénario supprimé.
    - [x] Reconstruire les fenêtres des outils avancés.
      - [x] Modifier `Theme/Base/Views/Windows/Tools/GwToolWindow.xaml.cs` : exposer champs, cases, résumé, commande, sortie et bouton existants, suivre l’exécution possédée et attendre sa fin lors de la fermeture sans détruire le runner emprunté.
      - [x] Créer `Theme/OS/Editions/Windows31/Views/Windows/Tools/ToolView.cs` : reconstruire paramètres, sortie à chasse fixe et commandes indépendantes, préserver toutes les variantes de verbes et les confirmations.
      - [x] Modifier `Theme/OS/Editions/Windows31/Views/Windows/Desktop/Windows31Desktop.Windows.cs`, `Enums/Windows31DialogKind.cs` et `Services/Windows/WpfWindowNavigationService.cs` : raccorder les outils et attendre l’arrêt effectif des opérations au nettoyage.
      - [x] Modifier `Theme/OS/Editions/Windows31/Views/Windows/Tools/ToolView.cs` : qualifier la propriété de police par Control.FontFamilyProperty, car le conteneur DockPanel ne possède pas cette propriété ; reprendre la validation groupée après correction de compilation.
      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31ToolWindowTests.cs` : vérifier tous les verbes et champs, commande/validation, sortie, annulation et fermeture avec runner simulé, produire les captures utiles dans `artifacts/themes-review/screens/OS/Windows31/Default/Tool*.png`, inspecter puis supprimer le scénario temporaire.
    - [x] Modifier ce suivi après inspection de `Theme/Base/Views/Windows/{Updates/UpdatesWindow,Conversion/FileMigrationWindow}.xaml.cs` : détailler les fichiers de présentation et relais à créer, suivre et attendre les opérations avant fermeture, puis inscrire contrôles et captures nécessaires avant création.
    - [x] Reconstruire la migration de fichiers et les avertissements de l’explorateur.
      - [x] Modifier `Theme/Base/Views/Windows/Conversion/FileMigrationWindow.xaml.cs` : exposer les feuilles fonctionnelles, partager le chargement initial natif/thématisé, suivre lecture et migration avec leur annulation, puis attendre leur fin avant fermeture ; conserver le coordinateur et toutes les validations.
      - [x] Créer `Theme/OS/Editions/Windows31/Views/Windows/Conversion/FileMigrationView.cs` et modifier `Theme/Base/Views/Dialogs/Explorer/ExplorerIssuesWindow.xaml.cs`, puis créer `Theme/OS/Editions/Windows31/Views/Dialogs/Explorer/ExplorerIssuesView.cs` : reconstruire les sélecteurs, pertes, acceptation et liste complète des avertissements avec les mêmes données et actions.
      - [x] Modifier `Controllers/MainWindow/ConversionTabController.cs`, `Theme/Base/Views/Controls/Explorer/Sections/ExplorerSection.Navigation.cs`, `Theme/OS/Editions/Windows31/Views/Windows/Desktop/Windows31Desktop.Windows.cs`, `Enums/Windows31DialogKind.cs` et `Services/Windows/WpfWindowNavigationService.cs` : remplacer les deux ShowDialog directs par le relais existant, raccorder les vues et attendre les opérations de migration à la fermeture.
      - [x] Modifier `Theme/OS/Editions/Windows31/Factories/Windows31MediaApplications.cs` : raccorder aussi le raccourci des avertissements de l’explorateur à la fenêtre fonctionnelle et à sa nouvelle vue, puis la fermer dans finally.
      - [x] Modifier `Theme/OS/Editions/Windows31/Styles/Windows31Controls.xaml` : donner aux en-têtes GridView le relief et les couleurs de Windows 3.1 au lieu du dégradé natif moderne.
      - [x] Modifier `Resources/*/Migration.resx` et `Resources/00-Base/Migration.resx` : corriger Migration.Description dans le contexte des pertes de migration, avec Argos pour toutes les langues ; vérifier le rendu final après mise en page.
      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31MigrationWindowsTests.cs` : vérifier toutes les données et liaisons, chargement initial partagé, annulation, fermeture et absence de HWND source ; produire puis inspecter `artifacts/themes-review/screens/OS/Windows31/Default/{FileMigration,ExplorerIssues}.png`, supprimer scénario et données temporaires.
    - [x] Reconstruire les mises à jour.
      - [x] Modifier `Options/Controllers/UpdateOptionsController.cs` et ses fichiers `Updates/{UpdateOptionsController.Search,UpdateOptionsController.Installation}.cs`, ainsi que `Theme/Base/Views/Windows/Updates/UpdatesWindow.xaml.cs` : suivre les recherches/préparations existantes, annuler et attendre les opérations avant libération, détacher tous les événements sans changer les confirmations et le lancement de l’installateur.
      - [x] Créer `Theme/OS/Editions/Windows31/Views/Windows/Updates/UpdatesView.xaml` et `.xaml.cs`, modifier `Theme/Base/Views/Controls/Options/OptionsUpdatesSection.xaml.cs` : reconstruire les quatre catégories, versions sélectionnables, catalogue et installation manuelle avec les objets et événements existants, conserver tous les textes et progressions, libérer les liaisons.
      - [x] Modifier `Theme/OS/Editions/Windows31/Views/Windows/Desktop/Windows31Desktop.Windows.cs`, `Enums/Windows31DialogKind.cs` et `Services/Windows/WpfWindowNavigationService.cs` : raccorder les mises à jour, leur fin différée et les propriétés de pied de fenêtre.
      - [x] Modifier `Options/Controllers/Updates/UpdateOptionsController.Installation.cs` : restaurer les états de commandes après préparation lorsque la fenêtre fonctionnelle est présentée sans HWND ; utiliser l’état de fermeture du contrôleur au lieu de IsVisible.
      - [x] Modifier `Theme/OS/Editions/Windows31/Views/Windows/Updates/UpdatesView.xaml` : déclarer x:ClassModifier="internal" pour correspondre à sa classe de présentation.
      - [x] Modifier `Theme/OS/Editions/Windows31/Views/Windows/Updates/UpdatesView.xaml` : conserver la clé Updates.ModuleDirectoryInstall du bouton d’installation du catalogue, distincte de la commande globale de mise à jour.
      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31UpdatesWindowTests.cs` : contrôler les quatre catégories, commandes, résultats, choix de versions, état de téléchargement et fermeture sans réseau ni installateur réels, produire les captures `artifacts/themes-review/screens/OS/Windows31/Default/Updates*.png`, inspecter puis supprimer le scénario et ses données temporaires. Les tests groupés existants des mises à jour restent conservés.


  - [x] Terminer les aperçus et les autres dialogues avant livraison.

    - [x] Créer `Theme/OS/Editions/Windows31/Views/Controls/Common/Windows31MenuBindings.cs`, modifier `Views/Windows/Desktop/{Windows31ProgramManager.xaml,Windows31ProgramManager.xaml.cs,Windows31Desktop.cs,Windows31Workspace.cs}` : conserver les quatre menus historiques tout en raccordant dans Options les commandes existantes de mises à jour, journaux, diagnostics, matériel et modules ; suivre les ajouts/retraits des modules et libérer les liaisons à la fermeture.
    - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31MenuBindingsTests.cs` : contrôler sans fichiers ni fenêtre native les commandes originales, états, menus imbriqués, mises à jour dynamiques et détachement.
    - [x] Modifier `Contracts/Theming/ThemeEditionDefinition.cs`, `Theme/Base/Views/Controls/Options/OptionsThemesSection.xaml.cs` et `Theme/OS/Editions/Windows31/Windows31Edition.cs` : sélectionner l’aperçu propre à la déclinaison et à son apparence ; conserver le chemin Base/Studio et l’inclusion PNG récursive déjà existante dans RetroMediaLab.App.csproj.
    - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows31FinalReviewTests.cs` : construire les sept vues avec leurs sources existantes, parcourir toutes les palettes, ouvrir les applications et produire les aperçus ainsi que les captures dans themes-review/screens ; libérer les vues dans finally, puis supprimer le scénario temporaire après la vérification finale.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/WindowsDefault.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/WindowsDefault.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/Arizona.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/Arizona.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/BlackLeatherJacket.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/BlackLeatherJacket.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/Bordeaux.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/Bordeaux.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/Cinnamon.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/Cinnamon.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/Designer.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/Designer.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/EmeraldCity.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/EmeraldCity.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/Fluorescent.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/Fluorescent.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/HotdogStand.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/HotdogStand.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/LcdDefault.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/LcdDefault.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/LcdReversedDark.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/LcdReversedDark.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/LcdReversedLight.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/LcdReversedLight.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/Mahogany.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/Mahogany.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/Monochrome.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/Monochrome.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/Ocean.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/Ocean.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/Pastel.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/Pastel.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/Patchwork.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/Patchwork.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/PlasmaPowerSaver.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/PlasmaPowerSaver.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/Rugby.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/Rugby.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/TheBlues.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/TheBlues.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/Tweed.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/Tweed.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/Valentine.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/Valentine.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
      - [x] Créer `src/RetroMediaLab.App/Theme/OS/Editions/Windows31/Assets/Previews/Wingtips.png` et `artifacts/themes-review/screens/OS/Windows31/Palettes/Wingtips.png` : capturer le vrai bureau et la vue Lecture à 1280×800 avec cette palette.
    - [x] Modifier `docs/tasks/application-themes.md` : consigner le contrôle des accès de toutes les fenêtres Base, des actions de menus et des rendus conservés ; inscrire toute correction réellement manquante avant sa réalisation.
    - [x] Modifier `Dictionaries/Theming/ThemeCatalog.cs`, créer `Theme/OS/Light.xaml` et modifier les tests autonomes de thèmes si nécessaire : rendre OS/Windows 3.1 sélectionnable après les contrôles de présentation ; conserver Base/Studio et les choix de palettes existants.
    - [x] Modifier `docs/tasks/application-themes.md` après le build officiel `scripts/local-building.cmd --building=debug --modules=A` : consigner le résultat, l’exécutable et la présence des 24 modules ; aucun commit avant validation de l’utilisateur.

    - [x] Modifier `docs/tasks/application-themes.md` : inscrire les fichiers de présentation restants pour préférences d’émulation, options des modules et machines, dialogues des supports, mises à jour, journaux, aide et erreurs après inventaire des fabriques réellement utilisées ; ne pas inscrire OS au catalogue avant leur réalisation et les contrôles complets.

- OS reste incomplet : Macintosh OS, Atari GEM et Amiga Workbench ne sont pas réalisés. Windows 98 est reporté à une éventuelle réalisation ultérieure à la demande de l’utilisateur ; il ne compte pas dans les manques actuels. Demande utilisateur du 1 novembre : ne pas poursuivre ces déclinaisons ; terminer Orbit puis arrêter le travail sur les thèmes.
  - [x] Construire Windows 95 comme déclinaison distincte, sans modifier le build Windows 3.1 livré.
    - [x] Réaliser le cadre des fenêtres.
      - [x] Modifier `docs/design/themes-rebuild.md` : préciser les références Microsoft/RTM et la composition bureau, lancement et fenêtres natives, en conservant les commandes métier existantes.
      - [x] Créer `Theme/OS/Editions/Windows95/Constants/Windows95Metrics.cs` : centraliser dimensions du cadre, barre de titre et boutons.
      - [x] Créer `Theme/OS/Editions/Windows95/Styles/Windows95Chrome.xaml` : dessiner le relief, la barre de titre active/inactive et les commandes rectangulaires de Windows 95, sans reprendre le cadre Windows 3.1.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Windows/Common/Windows95Window.cs` : héberger le contenu fourni, relier fermeture/réduction/agrandissement/restauration aux commandes de la vraie fenêtre et libérer les liaisons à la fermeture ; ne pas détruire le contenu emprunté.
      - [x] Modifier `Theme/OS/Editions/Windows95/Constants/Windows95Metrics.cs` et `Views/Windows/Common/Windows95Window.cs` : nommer le point de présentation du contenu et fournir la bordure de redimensionnement comme Thickness utilisée par le style.
      - [x] Modifier `Theme/OS/Editions/Windows95/Constants/Windows95Metrics.cs` : rendre publics les membres référencés par x:Static, en conservant leur classe interne ; résoudre leur accès lors du chargement BAML.
      - [x] Modifier `Theme/OS/Editions/Windows95/Constants/Windows95Metrics.cs` et `Styles/Windows95Chrome.xaml` : utiliser une GridLength pour la ligne de titre ; x:Static fournit une valeur typée et ne convertit pas un double en GridLength.
      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95WindowTests.cs` : vérifier commandes, états, annulation de fermeture et destruction native effective, capturer `artifacts/themes-review/screens/OS/Windows95/Window.png`, puis supprimer scénario et données temporaires.
    - [x] Réaliser le bureau et la barre des tâches Windows 95.
      - [x] Créer `Theme/OS/Editions/Windows95/Enums/Windows95ProgramGroup.cs` et `Contracts/Windows95LauncherDefinition.cs` : décrire les groupes et les lanceurs fournis par les fonctions existantes, sans créer de service métier.
      - [x] Créer `Theme/OS/Editions/Windows95/Constants/Windows95DesktopMetrics.cs` et `Styles/Windows95Desktop.xaml` : centraliser dimensions et ressources du bureau, icônes, menu de lancement et boutons de tâches.
      - [x] Modifier `Resources/*/Options.resx` : ajouter les libellés Démarrer et Programmes dans toutes les langues avec Argos.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Desktop/Windows95Desktop.cs` : lancer les actions fournies au double clic et depuis le menu, afficher les fenêtres existantes dans la barre des tâches, suivre leurs titres et états, détacher les événements sans détruire les fenêtres empruntées.
      - [x] Modifier `Windows95Desktop.cs` et `Resources/*/Options.resx` : traiter Entrée malgré le traitement clavier natif du bouton et corriger les traductions Argos ambiguës des deux libellés.
      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows95DesktopTests.cs` : vérifier en mémoire les lanceurs et la libération des abonnements ; créer puis supprimer un scénario temporaire pour la capture du bureau dans `artifacts/themes-review/screens/OS/Windows95/Desktop.png`.
      - [x] Modifier `Windows95Desktop.cs`, `Windows95DesktopMetrics.cs` et `TemporaryWindows95DesktopReviewTests.cs` : corriger le contraste des noms d’icônes, centraliser les clés de pinceaux et la police, puis utiliser l’image BBC existante dans la capture et supprimer le scénario temporaire après inspection.
      - [x] Modifier `Styles/Windows95Desktop.xaml`, `Views/Controls/Desktop/Windows95Desktop.cs` et `Constants/Windows95DesktopMetrics.cs` : remplacer le menu contextuel hérité par le menu Démarrer carré gris Windows 95, avec sous-menus et sélection bleue, et détacher ses ressources à la libération.
      - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/Windows95DesktopTests.cs` : vérifier le chargement des modèles du menu et l’exécution de son action existante sans fenêtre native.
    - [x] Réaliser la fenêtre de lecture Windows 95.
      - [x] Créer `Theme/OS/Editions/Windows95/Styles/Windows95Controls.xaml` : définir les commandes, champs, choix et listes propres à Windows 95, sans importer les styles Windows 3.1 ni modifier Base.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Read/AcquisitionView.xaml` et `.xaml.cs` : composer menus, barre de commandes, modes Brut/Format, destination et liste des acquisitions ; relier les contrôles fonctionnels existants et détacher leurs abonnements à la fermeture.
      - [x] Modifier `Views/Controls/Read/AcquisitionView.xaml.cs`, `Styles/Windows95Controls.xaml` et `Styles/Windows95Desktop.xaml` : utiliser les points nommés existants pour les révolutions et compléter les cases et les menus horizontaux Windows 95.
      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95AcquisitionTests.cs` : vérifier édition aller-retour, mode, exécution et libération ; générer `artifacts/themes-review/screens/OS/Windows95/Read.png`, puis supprimer le scénario temporaire.
    - [x] Réaliser la fenêtre d’écriture Windows 95.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Write/WriteView.xaml` et `.xaml.cs` : reconstruire la fenêtre autour du fichier, du profil et du format détecté, avec menus et barre de commande Windows 95 reliés aux actions existantes.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95WriteTests.cs` : vérifier fichier, profil, choix de format, commandes et libération, puis capturer `artifacts/themes-review/screens/OS/Windows95/Write.png`.
    - [x] Réaliser les propriétés avancées et la conversion Windows 95.
      - [x] Déplacer `Theme/OS/Editions/Windows31/Views/Controls/Common/Windows31BoundFields.cs` vers `Theme/Common/ThemeBoundFields.cs` : conserver son comportement générique, renommer la classe et modifier ses trois consommateurs Windows 3.1 dans `Views/Dialogs/{Read/AcquisitionOptionsView.cs,Write/WriteOptionsView.cs,Conversion/ConversionOptionsView.cs}` sans changement de rendu.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Dialogs/Read/AcquisitionOptionsFields.cs`, `Write/WriteOptionsFields.cs`, `Conversion/ConversionOptionsFields.cs` et `Views/Controls/Common/Windows95PropertyPages.cs` : définir les points des champs et la composition des propriétés Windows 95 en onglets, avec les styles locaux existants.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Dialogs/Read/AcquisitionOptionsView.cs`, `Write/WriteOptionsView.cs` et `Conversion/ConversionOptionsView.cs` : relier tous les réglages existants, séparer acquisition/nommage/expert et écriture/conversion/expert sans emprunter les conteneurs Base.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Conversion/ConversionView.xaml`, `.xaml.cs` et `ConversionFormatRow.cs` : disposer source, commandes et formats de sortie dans une fenêtre Windows 95, conserver les choix multiples et extensions réels, puis libérer les liaisons.
      - [x] Modifier `Resources/*/Options.resx` et `Windows95PropertyPages.cs` : ajouter un titre d’onglet Numérotation concis dans toutes les langues ; modifier le scénario temporaire pour laisser les liaisons s’initialiser avant la capture et afficher les libellés réels du catalogue.
      - [x] Modifier `Views/Controls/Conversion/ConversionView.xaml`, `Views/Controls/Read/AcquisitionView.xaml` et `Styles/Windows95Controls.xaml` : laisser défiler les listes dans l’espace disponible, maintenir les commandes visibles et donner aux barres de défilement leur relief et leurs flèches Windows 95 ; vérifier le défilement dans le scénario temporaire.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95PropertiesTests.cs` : vérifier les champs avancés, leurs événements, la conversion et les libérations ; capturer les propriétés et la conversion dans `artifacts/themes-review/screens/OS/Windows95/`, et vérifier les consommateurs Windows 3.1 touchés par le déplacement du seul relais générique.
    - [x] Réaliser l’explorateur Windows 95 et ses propriétés.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Explorer/Windows95FolderBranch.cs` : dessiner les branches et les cases plus/moins du navigateur Windows 95, sans case pour les feuilles, en laissant les images de dossiers au fournisseur existant.
      - [x] Créer `Theme/OS/Editions/Windows95/Styles/Windows95Explorer.xaml` : définir le tableau de fichiers, les en-têtes redimensionnables et la sélection Windows 95.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Explorer/ExplorerView.xaml` et `.xaml.cs` : reconstruire menus, sélecteurs de support, arborescence et détails des fichiers ; relier navigation, lecture, ouverture, progression, propriétés et avertissements existants, puis détacher les événements.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Explorer/ExplorerDetailsView.cs` : afficher toutes les propriétés existantes dans la page Général Windows 95 et suivre leur mise à jour sans modifier les modèles métier.
      - [x] Modifier `Styles/Windows95Desktop.xaml` et `Styles/Windows95Explorer.xaml` : rendre visibles les coches des menus et le remplissage segmenté de progression ; vérifier également les flèches gauche/droite de l’arborescence et les coches dans le scénario temporaire.
      - [x] Modifier `Views/Controls/Explorer/Windows95FolderBranch.cs` : limiter aussi la zone cliquable à la hauteur de la case plus/moins, et vérifier que le haut de la ligne ne déclenche pas son ouverture dans le scénario temporaire.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95ExplorerTests.cs` : vérifier branches, navigation, commandes, propriétés et libération ; capturer `artifacts/themes-review/screens/OS/Windows95/Explorer.png` et `ExplorerProperties.png`, puis nettoyer le scénario temporaire.
    - [x] Réaliser la visualisation et les outils Windows 95.
      - [x] Créer `Theme/OS/Editions/Windows95/Constants/Windows95VisualizationMetrics.cs` et `Styles/Windows95Visualization.xaml` : définir les ressources des cadres de dessin et la sélection visible des pistes tout en conservant les couleurs des données.
      - [x] Modifier `Resources/*/Visualizer.resx` : ajouter le libellé Mode d’affichage dans toutes les langues pour le sélecteur de représentation, sans employer un intitulé limité aux disquettes.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Visualization/VisualizationView.xaml`, `.xaml.cs` et `VisualizationOverviewView.cs` : reconstruire menus, commandes, choix et aperçu ; emprunter seulement les feuilles de rendu existantes pour les cinq représentations et les restituer à leurs propriétaires dans les finally.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Tools/ToolsView.xaml` et `.xaml.cs` : présenter la liste des deux outils et leurs paramètres Windows 95, en conservant les constructeurs de commandes, l’exécution et l’arrêt existants.
      - [x] Modifier `Styles/Windows95Controls.xaml` : laisser les textes hériter de la couleur de sélection et appliquer le sélecteur de modèle/string format des ComboBox ; modifier le scénario temporaire de visualisation pour capturer les dessins dans une fenêtre réelle invisible hors écran, fermée dans finally avec destruction native vérifiée.
      - [x] Modifier `Styles/Windows95Controls.xaml` : relier explicitement le texte généré au Foreground du contrôle propriétaire ; vérifier dans les captures temporaires la couleur des lignes sélectionnées et le libellé simple des choix de surface.
      - [x] Créer `Styles/Windows95Text.xaml` et modifier les ContentPresenter de `Windows95Controls.xaml`, `Windows95Desktop.xaml` et `Windows95Visualization.xaml` : fournir le style de texte dans la portée des modèles, car le texte généré par ContentPresenter reprend sinon le style App avant d’atteindre les ressources de la vue.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95VisualizationTests.cs` et `TemporaryWindows95ToolsTests.cs` : vérifier les cinq rendus et leur restitution, les zooms/sélections, les commandes et la libération ; produire puis inspecter les captures `Visualization.png`, `VisualizationBlocks.png`, `VisualizationGeometry.png`, `ToolsErase.png` et `ToolsClean.png` dans `artifacts/themes-review/screens/OS/Windows95/`, puis nettoyer les scénarios temporaires.
    - [x] Réaliser la présentation des machines émulées Windows 95.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Emulation/Machine/MachineView.xaml` et `.xaml.cs` : présenter menus, commandes principales, écran et liste latérale défilante des périphériques ; réutiliser les points de commande, surfaces et états existants pour tous les modules, sans recréer de cœur.
      - [x] Modifier `Theme/OS/Editions/Windows95/Styles/Windows95Desktop.xaml` : afficher les boutons à état désactivés avec leur couleur Windows 95, pour la barre de commandes de la machine.
      - [x] Modifier `Styles/Windows95Controls.xaml` : respecter HorizontalContentAlignment dans le modèle de bouton, centrer les commandes par défaut et aligner à gauche les libellés des périphériques ; vérifier également le défilement et le chargement des shaders dans le scénario temporaire.
      - [x] Créer `Styles/Windows95Machine.xaml` et modifier `MachineView.xaml` et `.xaml.cs` : utiliser des boutons de commande dont l’apparence suit l’état fourni par le contrôleur, sans basculement autonome avant le résultat de Marche/Pause ; vérifier ce comportement dans le scénario temporaire.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95MachineViewTests.cs` : vérifier transfert/restitution de la surface et des entrées, commandes et états, périphériques nombreux, transport tape, chargement des shaders et libération ; capturer `artifacts/themes-review/screens/OS/Windows95/Emulation.png` sans présenter une simulation comme un démarrage Acorn réel, puis nettoyer le scénario temporaire.
      - [x] Modifier `Styles/Windows95Controls.xaml`, `Windows95Machine.xaml`, `Windows95Visualization.xaml` et les dictionnaires des vues `MachineView.xaml` et `VisualizationView.xaml` : nommer explicitement le style de bouton Windows 95 et résoudre les styles dérivés dans leur propre dictionnaire pour supprimer le repli involontaire vers les tailles/couleurs de Base.
      - [x] Modifier `Constants/Windows95Metrics.cs` et `Styles/Windows95Machine.xaml` : utiliser une valeur booléenne typée pour le déclencheur d’état de commande, sans dépendre du contexte de liaison du modèle.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95ButtonStylesTests.cs` : vérifier les boutons Marche/Pause et les boutons de pistes à partir du style Windows 95, puis réactualiser les captures Emulation et VisualizationGeometry dans `artifacts/themes-review/screens/OS/Windows95/`.
    - [x] Réaliser la bibliothèque des configurations Windows 95.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Emulation/Machine/EmulationView.xaml` et `.xaml.cs` : présenter les configurations en liste compacte Windows 95 avec menus, ouverture au double clic/Entrée et actualisation, en conservant les actions et la sélection existantes.
      - [x] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows95EmulationViewTests.cs` : vérifier en mémoire ouverture, actualisation, sélection et détachement en réutilisant les scénarios fonctionnels existants.
    - [x] Réaliser le sélecteur de firmware Windows 95.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Dialogs/Emulation/FirmwareSelectionView.xaml` et `.xaml.cs` : afficher les ROM dans un tableau compact à colonnes redimensionnables Windows 95, conserver compatibilité, destination, dossier, actualisation et validation existants ; détacher les événements et les liaisons.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95FirmwareTests.cs` : contrôler le sélecteur et capturer son affichage avec la bibliothèque de configurations dans `artifacts/themes-review/screens/OS/Windows95/` ; supprimer le scénario après les contrôles groupés.
    - [x] Réaliser les champs de réglages et propriétés des moteurs Windows 95.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Emulation/Options/SettingsFieldView.cs` : relier les champs décrits aux points fonctionnels existants, conserver saisies contraintes, choix, validations et disponibilité sans emprunter leur arbre visuel ; adapter les dimensions au rendu Windows 95.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Dialogs/Common/BusinessDialogView.cs` : centraliser les commandes de validation/annulation et le détachement dans une présentation Windows 95 locale.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Dialogs/Emulation/CoreOptionsView.cs` : présenter les catégories existantes en pages de propriétés Windows 95, relier tous les champs et libérer leurs abonnements.
      - [x] Modifier `Styles/Windows95Controls.xaml` : reconstruire boutons radio, curseurs et cadres de groupes Windows 95 nécessaires aux réglages sans repli vers Base.
      - [x] Modifier `Views/Dialogs/Common/BusinessDialogView.cs` : appliquer les propriétés de police héritées via TextElement, car le conteneur DockPanel ne déclare pas les propriétés de Control.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95SettingsTests.cs` : vérifier les champs, les commandes et leur détachement ; capturer les propriétés des moteurs dans `artifacts/themes-review/screens/OS/Windows95/` et nettoyer le scénario.
    - [x] Réaliser le panneau de gestion des moteurs Windows 95.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Emulation/Options/CoreManagementView.xaml` et `.xaml.cs` : présenter moteur et versions dans un formulaire compact, téléchargement et recherche dans une barre d’actions, progression et état dans le cadre Windows 95 ; conserver toutes les commandes et détacher les liaisons.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95CoreManagementTests.cs` : vérifier choix, actions, progression et détachement, capturer le panneau dans `artifacts/themes-review/screens/OS/Windows95/` puis supprimer le scénario.
    - [x] Réaliser les propriétés des modules Windows 95 et leurs panneaux spécialisés.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Emulation/Storage/ModuleStorageView.xaml` et `.xaml.cs` : reconstruire la liste des périphériques sous forme de tableau Windows 95 redimensionnable, avec ajout/configuration/suppression reliés aux commandes existantes.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Emulation/Video/VideoProcessingView.xaml` et `.xaml.cs` : présenter les pages vidéo dans une liste latérale et leurs réglages dans un panneau défilant, avec les champs Windows 95 et le chargement des shaders existant.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Emulation/Input/ControllerOutputsView.cs` : présenter les sorties des contrôleurs avec les listes Windows 95 et conserver leur actualisation et leur disponibilité.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Emulation/Input/InputBindingsView.xaml`, `.xaml.cs`, `ControllerPortView.xaml`, `.xaml.cs` et `InputDrawingView.cs` : reconstruire les tableaux de commandes et les ports dans le style Windows 95 ; conserver capture des entrées, dessins et validations existants.
      - [x] Modifier `Views/Controls/Emulation/Input/ControllerPortView.xaml` : présenter les liaisons dans le cadre traduit existant et les sorties sous leur propre libellé fourni, sans clé de traduction inventée.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Emulation/Input/ModuleInputView.xaml` et `.xaml.cs` : relier clavier, souris et ports aux nouvelles présentations, restituer les ressources empruntées et détacher les événements.
      - [x] Modifier `Views/Controls/Emulation/Input/ModuleInputView.xaml.cs` et `ControllerPortView.xaml.cs` : limiter le nettoyage des ressources à la fin de Dispose, afin que le changement de port conserve les styles locaux.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Windows/EmulationModuleOptions/ModuleSettingsView.xaml` et `.xaml.cs` : présenter les catégories et groupes de réglages avec les panneaux spécialisés Windows 95, sans omettre les champs déclarés par les modules ni modifier Base.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95ModuleSettingsTests.cs`, `TemporaryWindows95VideoTests.cs` et `TemporaryWindows95InputTests.cs` : vérifier les champs, résumés, ports, liaisons et libérations sur les scénarios existants ; capturer les réglages dans `artifacts/themes-review/screens/OS/Windows95/` et nettoyer ces scénarios après le contrôle groupé.
    - [x] Réaliser les dialogues des supports Windows 95.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Emulation/Storage/StorageFieldsView.cs` : composer les champs compacts avec les contrôles Windows 95, leurs contraintes et leurs actions existantes, puis libérer leurs liaisons.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Dialogs/Emulation/Storage/AddStorageDeviceView.cs`, `CompactDiscSettingsView.cs`, `CartridgeSettingsView.cs` et `MemoryCardDirectoryView.cs` : reconstruire les formulaires simples avec le cadre et les commandes Windows 95 sans modifier les règles des supports.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Dialogs/Emulation/Storage/FloppySettingsView.cs` : répartir propriétés du lecteur et groupes de réglages dans des onglets Windows 95, conserver les actions de création et le filtrage existants.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Dialogs/Emulation/Storage/HardDiskSettingsView.cs` : reconstruire association/création, géométrie et comportement en pages de propriétés Windows 95, avec les tailles et validations existantes.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Dialogs/Emulation/Storage/ManagedStorageView.cs` : reconstruire les groupes de paramètres des supports gérés en pages de propriétés Windows 95, avec leur disponibilité réelle et le détachement des liaisons.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Emulation/Storage/FloppyCreationView.cs` et `Views/Dialogs/Emulation/Storage/MediaCreationView.cs` : séparer format d’image, géométrie et destination dans la présentation Windows 95 et conserver les actions créer/créer et monter existantes.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95StorageTests.cs` : vérifier les commandes et validations des supports, leur création et les détachements ; capturer leurs dialogues dans `artifacts/themes-review/screens/OS/Windows95/`, puis supprimer le scénario et ses données temporaires.
    - [x] Réaliser les préférences Windows 95.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Options/GeneralOptionsView.xaml` et `.xaml.cs` : relier dossiers, langue et modèles de noms dans des propriétés Windows 95 compactes ; détacher les liaisons à la fermeture.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Options/LogsOptionsView.xaml` et `.xaml.cs` : présenter la configuration des journaux et leurs limites avec les contrôles Windows 95 ; détacher les liaisons à la fermeture.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Options/HardwareOptionsView.xaml` et `.xaml.cs` : présenter lecteurs physiques et outils hôtes en pages de propriétés Windows 95, sans modifier leurs actions ; détacher les liaisons à la fermeture.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Options/EnginesOptionsView.xaml` et `.xaml.cs` : présenter les quatre choix de moteurs dans un formulaire compact Windows 95 ; détacher les liaisons à la fermeture.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Options/ProfilesOptionsView.xaml` et `.xaml.cs` : présenter les profils par catégorie avec les listes et les commandes Windows 95 ; détacher les liaisons à la fermeture.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Options/ControllersOptionsView.xaml` et `.xaml.cs` : présenter détection, entrées, réglages et capacités des manettes dans les pages Windows 95, en conservant le dessin et le suivi existants ; détacher les liaisons à la fermeture.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Options/ThemeOptionsView.xaml` et `.xaml.cs` : présenter thème, déclinaison et style avec aperçu Windows 95 et conserver le choix appliqué par le contrôleur existant ; détacher les liaisons à la fermeture.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Windows/Preferences/PreferencesView.cs` : composer les sept rubriques en pages de propriétés Windows 95, synchroniser la navigation existante et libérer les présentations créées.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Emulation/Network/NetworkView.cs` : afficher le tableau réseau non sélectionnable dans le rendu Windows 95 et suivre uniquement les notifications du registre existant.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Windows/EmulationPreferences/EmulationPreferencesView.xaml` et `.xaml.cs` : reconstruire dossiers, raccourcis, configurations et réseau en propriétés Windows 95 avec tableau compact, tout en conservant les actions des configurations.
      - [x] Modifier `Views/Controls/Emulation/Network/NetworkView.cs` : inclure le relief des en-têtes dans la largeur des colonnes afin de conserver leur alignement avec les cellules.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95PreferencesTests.cs` : vérifier les sept rubriques, leurs liaisons et libérations, les configurations et le tableau réseau ; capturer les préférences dans `artifacts/themes-review/screens/OS/Windows95/`, puis supprimer les données temporaires et le scénario.
    - [x] Réaliser les dialogues globaux Windows 95.
      - [x] Extraire les icônes historiques des dialogues Windows 95.
        - [x] Créer `Theme/OS/Editions/Windows95/Assets/Icons/system-100.ico` à `system-105.ico` et `artifacts/themes-review/screens/OS/Windows95/DialogIconResources.png` : extraire les six groupes du USER.EXE archivé par PCjs et vérifier leur contenu visuellement, sans exécuter les binaires.
        - [x] Renommer les quatre ressources retenues en `message-information.ico`, `message-warning.ico`, `message-error.ico`, `message-question.ico`, puis supprimer les deux fichiers `system-*.ico` inutilisés dans ce même dossier.
        - [x] Créer `Theme/OS/Editions/Windows95/Styles/Windows95DialogIcons.xaml` : relier les quatre icônes aux types de messages et conserver les fichiers système hors du dépôt.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Dialogs/Common/MessageView.cs` et `ErrorView.cs` : reconstruire les messages, choix et détails avec le cadre Windows 95 et les commandes existantes, sans dépendre du thème Windows 3.1.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Dialogs/Read/ReadConflictView.cs`, `Dialogs/Conversion/ConversionConflictView.cs`, `Dialogs/Profiles/ProfileNameView.cs`, `Dialogs/Hardware/HardwareUnavailableView.cs` et `Dialogs/Explorer/ExplorerIssuesView.cs` : reconstruire les dialogues de conflit, nommage et avertissement avec les contrôles Windows 95 et conserver leurs actions.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Windows/About/AboutView.cs` et `Windows/Tools/ToolView.cs` : présenter les informations de l’application et les outils dans les fenêtres Windows 95, sans reprendre les polices ni le cadre Windows 3.1.
      - [x] Modifier `src/RetroMediaLab.App/RetroMediaLab.App.csproj` : embarquer les fichiers ICO propres à Windows 95, sans changer les ressources Windows 3.1.
      - [x] Modifier `Views/Dialogs/Common/MessageView.cs` et `ErrorView.cs` : corriger le chemin des ressources ICO pour pointer sur le dossier Windows95, sans référence résiduelle au dossier Windows31.
      - [x] Modifier `Views/Windows/About/AboutView.cs` : charger le logo embarqué immédiatement avec BitmapCacheOption.OnLoad pour que son rendu soit disponible dès la première présentation.
      - [x] Modifier `Views/Windows/About/AboutView.cs` : utiliser une URI pack absolue pour le logo construit en C#, au lieu d’une URI relative interprétée comme un chemin de disque.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95GlobalDialogsTests.cs` : vérifier les choix de messages, le nommage, les commandes de conflit et l’instanciation des dialogues, capturer les messages dans `artifacts/themes-review/screens/OS/Windows95/`, puis supprimer le scénario.
      - [x] Modifier ce suivi après les premiers dialogues globaux pour inscrire les sélecteurs de fichiers, classification, inspection, historique, mise à jour, migration et console avant leur intégration au bureau.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Dialogs/Common/FileSelectionView.xaml` et `.xaml.cs` : reconstruire le sélecteur de fichiers Windows 95 avec navigation, filtres et validation existants, sans réutiliser le cadre Windows 3.1.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Dialogs/Classification/ClassificationView.xaml` et `.xaml.cs` : présenter les choix de classification dans le formulaire Windows 95 en conservant la sélection fonctionnelle et ses contraintes.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Dialogs/Inspector/InspectorView.xaml` et `.xaml.cs` : reconstruire l’inspecteur en pages Windows 95 et relier tous les points d’inspection existants.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Windows/Logs/LogHistoryView.xaml` et `.xaml.cs` : reconstruire les choix et l’affichage de l’historique des journaux avec les contrôles Windows 95 et le détachement des abonnements.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Windows/Updates/UpdatesView.xaml` et `.xaml.cs` : reconstruire recherche, choix et progression des mises à jour avec les commandes existantes et le cadre Windows 95.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Windows/Conversion/FileMigrationView.cs` : présenter la migration des fichiers dans une fenêtre Windows 95 et conserver ses validations et traitements asynchrones.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Controls/Console/TerminalView.xaml` et `.xaml.cs` : présenter la console dans une fenêtre propre Windows 95 avec police fixe, commande et historique existants ; libérer ses abonnements.
      - [x] Modifier `Views/Dialogs/Common/FileSelectionView.xaml.cs` : limiter la liste aux dossiers en mode sélection de dossier, tout en conservant la liste mixte pour ouvrir/enregistrer des fichiers.
      - [x] Modifier `Styles/Windows95Controls.xaml` : aligner en haut le contenu des champs multilignes pour les journaux et sorties d’outils, en conservant le centrage des champs simples.
      - [x] Modifier `Views/Dialogs/Inspector/InspectorView.xaml` et `Views/Dialogs/Common/FileSelectionView.xaml.cs` : conserver un bouton Fermer compact et utiliser Ouvrir/Enregistrer/OK selon le mode réel du sélecteur.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95RemainingDialogsTests.cs` : vérifier les nouveaux dialogues et leurs détachements, produire leurs captures dans `artifacts/themes-review/screens/OS/Windows95/`, puis supprimer le scénario et ses données temporaires.
    - [x] Raccorder les présentations Windows 95 aux fenêtres fonctionnelles et au bureau.
      - [x] Créer `Theme/OS/Editions/Windows95/Windows95WindowRouter.cs` : associer les fenêtres fonctionnelles existantes aux vues Windows 95, conserver leurs résultats et fermetures asynchrones, posséder et libérer uniquement les fenêtres natives de présentation.
      - [x] Créer `Theme/OS/Editions/Windows95/Windows95WindowRouter.Routes.cs` : déclarer les relais des fenêtres globales et des réglages, avec leurs dimensions et leurs actions existantes.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95WindowRouterTests.cs` : vérifier le refus des fenêtres inconnues, le résultat et la fermeture native des messages, ainsi que le détachement après erreur de création ; supprimer le scénario après vérification, sans laisser de fenêtre native.
      - [x] Modifier ce suivi pour inscrire les fichiers du raccordement du bureau, des menus, des applications et des sessions avant leur réalisation.
      - [x] Créer `Theme/OS/Editions/Windows95/Views/Dialogs/Common/Windows95PropertyWindow.cs` : présenter les panneaux avancés dans une fenêtre native Windows 95 possédée, avec fermeture et libération du panneau dans finally.
      - [x] Créer `Theme/OS/Editions/Windows95/Windows95Workspace.cs` et `.Applications.cs` : composer le bureau sur les sources fonctionnelles retenues, relier lecture/écriture/conversion/exploration/visualisation/outils/console et menus existants, restaurer l’arbre source à la libération.
      - [x] Créer `Theme/OS/Editions/Windows95/Windows95Workspace.Machines.cs` : ouvrir les sessions dans des fenêtres Windows 95 et emprunter leurs présentations via MachinePresentationLease, restaurer toutes les surfaces et entrées à la fermeture du thème.
      - [x] Modifier `Views/Controls/Desktop/Windows95Desktop.cs` : actualiser les lanceurs des configurations et menus déclarés sans perdre le suivi des fenêtres ouvertes.
      - [x] Modifier `Windows95Workspace.Machines.cs` : qualifier explicitement MachineView Windows 95 pour éviter son homonyme fonctionnel de Base.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95WorkspaceTests.cs` : vérifier ouverture, navigation et restauration des sources, générer une capture du bureau intégré puis supprimer le scénario et ses fichiers temporaires.

    - [x] Compléter les ressources et la présentation du bureau Windows 95 avant sa livraison.
      - [x] Créer `Theme/OS/Editions/Windows95/Assets/Icons/shell-*.ico` et `artifacts/themes-review/screens/OS/Windows95/ShellIcons.png` : extraire les icônes Shell32 de l’archive Windows 95 déjà utilisée, sans exécuter ni conserver le binaire source, et vérifier leurs rôles sur une planche.
      - [x] Supprimer les `Assets/Icons/shell-*.ico` non retenues dans la planche, en conservant uniquement les seize pictogrammes utilisés par les lanceurs et les menus Windows 95.
      - [x] Créer `Theme/OS/Editions/Windows95/Windows95ApplicationIcons.cs` : charger les icônes historiques retenues pour les fonctions et menus, conserver les images propres aux machines, puis modifier `Windows95Workspace.Applications.cs` pour les transmettre aux lanceurs et fenêtres.
      - [x] Créer `Views/Controls/Desktop/Windows95MenuMirror.cs`, puis modifier `Windows95Desktop.cs`, `Windows95Workspace.cs` et `.Applications.cs` : conserver les sous-menus et états des commandes existantes dans Démarrer, avec mise à jour des modules, au lieu de lanceurs inopérants pour leurs en-têtes.
      - [x] Modifier `Windows95Workspace.cs` et `Windows95Workspace.Machines.cs` : compléter les gardes de fermeture des fenêtres possédées et la restitution des contenus en cas d’échec d’affichage, sans arrêter les cœurs empruntés au changement de thème.
      - [x] Modifier `Views/Controls/Desktop/Windows95Desktop.cs` et `tests/RetroMediaLab.Tests/Interface/Themes/Windows95DesktopTests.cs` : faire défiler les lanceurs en conservant le suivi des tâches, vérifier la mise à jour des listes et l’accessibilité des configurations nombreuses.
      - [x] Modifier ce suivi pour inscrire l’activation au catalogue et la vérification finale de Windows 95 après intégration des ressources.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95IntegratedDesktopTests.cs` : vérifier en groupe les menus imbriqués, leur actualisation, les icônes historiques et l’ouverture/restitution du bureau ; produire `artifacts/themes-review/screens/OS/Windows95/IntegratedDesktop.png` après correction puis supprimer le scénario.
    - [x] Proposer Windows 95 dans OS après la vérification finale.
      - [x] Créer `Theme/OS/Editions/Windows95/Windows95Edition.cs` et `Assets/Previews/Default.png`, puis modifier `Resources/00-Base/Options.resx`, `Theme/OS/OSDefinition.cs`, `Dictionaries/Theming/ThemeCatalog.cs` et `tests/RetroMediaLab.Tests/Interface/Themes/ThemeEditionSelectionTests.cs` : déclarer Windows 95 comme déclinaison distincte et son aperçu, conserver les autres thèmes et déclinaisons.
      - [x] Modifier ce suivi avant les dernières corrections et le build, avec les fichiers exacts restant à corriger après la revue visuelle et fonctionnelle du lot Windows 95.
      - [x] Modifier `Windows95Workspace.cs` et `Windows95Workspace.Machines.cs` : positionner les fenêtres relativement au propriétaire et ne pas voler l’activation quand le bureau n’est pas visible.
      - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/Windows31ThemeOptionsTests.cs` : remplacer l’ancienne attente d’une seule déclinaison OS par les deux déclinaisons déclarées, en conservant la vérification des 23 palettes Windows 3.1.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95MachineWorkspaceTests.cs` : ouvrir deux contrôleurs existants dans les fenêtres Windows 95, fermer une machine, changer de thème et vérifier la conservation de l’autre session ; exécuter ensuite les contrôles thèmes/localisation en groupe et supprimer le scénario.
      - [x] Modifier `docs/tasks/application-themes.md` après ces vérifications pour inscrire les corrections éventuelles et la livraison Debug avec tous les modules.
      - [x] Modifier `docs/tasks/application-themes.md` avec le résultat du build officiel `scripts/local-building.cmd --building=debug --modules=A`, la vérification de `build/Debug/Retro Media Lab/retromedialab.exe` et des 24 modules, puis laisser le commit en attente de validation utilisateur.


    - [x] Conserver l’obligation de réponse des confirmations sans annulation.
      - [x] Modifier `Views/Windows/Common/Windows95Window.cs` et `Windows95WindowRouter.cs` : désactiver Fermer pour les messages sans choix d’annulation et refuser la fermeture native jusqu’à une réponse, comme le traitement existant le prévoit.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95RequiredChoiceTests.cs` : vérifier qu’une confirmation Oui/Non reste ouverte après Fermer et restitue ensuite le choix explicite, sans résidu natif.
      - [x] Modifier `docs/tasks/application-themes.md` avec le résultat du rafraîchissement Debug de l’application seule : le script a réussi mais remplace les modules livrés par Example ; les 23 autres ne sont donc pas conservés.
      - [x] Modifier `docs/tasks/application-themes.md` avec le résultat du nouveau build officiel `scripts/local-building.cmd --building=debug --modules=A`, puis vérifier de nouveau l’exécutable et les 24 manifestes/DLL avant livraison ; ne pas créer de commit.


  - [ ] Construire la déclinaison Windows XP, avec son bureau et ses fenêtres propres.
    - [x] Préparer les contrôles Luna réels sans modifier les déclinaisons livrées.
      - [x] Modifier `docs/design/themes-rebuild.md` : fixer les références Microsoft des trois palettes Luna et distinguer les contrôles fournis par WPF des cadres, du menu Démarrer et des dispositions XP restant à construire.
      - [x] Créer `Theme/OS/Editions/WindowsXP/Enums/WindowsXPColorScheme.cs` : nommer les trois palettes Luna Blue, Metallic et Homestead sans chaînes libres dans les choix internes.
      - [x] Créer `Theme/OS/Editions/WindowsXP/Services/Appearance/WindowsXPResources.cs` : charger les dictionnaires Luna inclus dans WPF, isoler leurs styles et couleurs dans la présentation XP et restaurer les ressources empruntées à la libération.
      - [x] Créer `Theme/OS/Editions/WindowsXP/Styles/WindowsXPControls.xaml` : définir la typographie et les contraintes des champs XP en s’appuyant sur les modèles Luna officiels ; conserver le contraste et le positionnement des contenus générés.
      - [x] Modifier `WindowsXPResources.cs` et `WindowsXPControls.xaml` : appliquer aussi les métriques héritées des styles de réglage et faire suivre la couleur des textes générés à leur contrôle, y compris en sélection.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPLunaTests.cs` : vérifier les trois palettes, les contrôles et leur isolation ; produire une planche `artifacts/themes-review/screens/OS/WindowsXP/LunaControls.png`, puis supprimer le scénario.
      - [x] Modifier `docs/tasks/application-themes.md` avec les chemins du cadre et du bureau XP après validation des contrôles ; conserver Windows 98, Macintosh, GEM et Workbench dans le périmètre OS restant.
    - [ ] Construire les ressources et le cadre XP avant le bureau.
      - [x] Créer temporairement `artifacts/themes-review/.work/xp-luna/luna.ms_` et `luna.msstyles` : lire uniquement le fichier de style dans le média XP original par plages HTTP, puis décompresser ses données sans exécuter le fichier de style.
      - [x] Modifier `docs/design/themes-rebuild.md` avec l’inventaire des bitmaps et des métriques du fichier Luna.msstyles d’origine, lu sans exécution ; définir ensuite les chemins exacts des images à extraire dans `Theme/OS/Editions/WindowsXP/Assets/Images/`.
      - [x] Compléter `docs/design/themes-rebuild.md` : relever les glyphes de titre, le panneau Démarrer et la bande des tâches dans les paramètres du média XP, puis inscrire les chemins d’extraction des images utiles avant nettoyage du fichier temporaire.
      - [x] Créer `Theme/OS/Editions/WindowsXP/Assets/Images/{Blue,Silver,Olive}/{FrameCaption,FrameMaximized,FrameLeft,FrameRight,FrameBottom,CaptionButton,CloseButton,CloseGlyph,MinimizeGlyph,MaximizeGlyph,RestoreGlyph,StartButton,TaskbarBackground,TaskbandButton,StartUserPanel,StartProgramsBackground,StartPlacesBackground,StartFooter,StartMorePrograms,StartArrow,StartArrowHot,StartLogoffButtons}.png` : convertir les ressources bitmap d’origine en PNG RGBA avec leurs états, couleur transparente et alpha, sans conserver de binaire exécutable.
      - [x] Créer `artifacts/themes-review/screens/OS/WindowsXP/OriginalSprites.png` : rendre une planche des cadres, boutons, glyphes et du panneau Démarrer extraits, puis l’inspecter avant de supprimer les fichiers temporaires.
      - [x] Créer `Theme/OS/Editions/WindowsXP/Assets/Metrics/ImageParts.json` : conserver les seuls paramètres natifs des 22 images retenues pour les trois palettes (classe, dimensions, nombre et disposition des états, marges et glyphes), pour découper les ressources sans valeurs inventées.
      - [x] Supprimer `artifacts/themes-review/.work/xp-luna/luna.ms_`, `luna.msstyles` et leur dossier temporaire après extraction des seules images retenues ; ne conserver aucun média d’installation ni binaire de style.
      - [x] Modifier `docs/tasks/application-themes.md` après cet inventaire pour détailler les images et les classes de découpe nécessaires avant création de `Views/Windows/Common/WindowsXPWindow.cs` et `Styles/WindowsXPChrome.xaml`.
      - [x] Créer `Enums/WindowsXPImagePart.cs`, `WindowsXPImageLayout.cs`, `WindowsXPImageSizing.cs` et `WindowsXPImageState.cs` : nommer les 22 ressources, leurs dispositions, leurs modes de découpe et leurs états sans chaînes libres dans les vues.
      - [x] Créer `Contracts/WindowsXPImageDefinition.cs` et `Services/Images/WindowsXPImageCatalog.cs`, puis modifier `src/RetroMediaLab.App/RetroMediaLab.App.csproj` : embarquer les métriques, charger et figer les images, vérifier leurs dimensions et découper leurs états en respectant les données originales.
      - [x] Modifier `Services/Appearance/WindowsXPResources.cs` et `Services/Images/WindowsXPImageCatalog.cs` : exposer la palette locale aux images, nommer les indices des états natifs et mettre en cache les seules découpes figées réutilisables.
      - [x] Créer `Views/Controls/Common/WindowsXPImage.cs` : dessiner les états et les neuf zones des images d’origine, avec tuilage ou taille naturelle quand les paramètres le demandent, sans étirer les glyphes ni modifier les autres thèmes.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPImageTests.cs` : vérifier les 66 définitions, tous leurs états et les dimensions de rendu ; produire `artifacts/themes-review/screens/OS/WindowsXP/FrameSlices.png`, puis supprimer le scénario.
      - [x] Modifier `docs/tasks/application-themes.md` pour préciser le cadre natif XP après validation des découpes, sans enregistrer la déclinaison tant que son bureau et ses vues ne sont pas raccordés.
      - [x] Créer temporairement `artifacts/themes-review/.work/xp-system-metrics/luna.ms_` et `luna.msstyles`, puis créer `Assets/Metrics/SystemMetrics.json` : conserver les polices, couleurs et dimensions système des trois paramètres Luna d’origine avant d’écrire le cadre.
      - [x] Supprimer les deux fichiers de `artifacts/themes-review/.work/xp-system-metrics/` et leur dossier après export des métriques système ; ne garder aucun média ni fichier de style binaire.
      - [x] Modifier `docs/tasks/application-themes.md` avec les classes de polices et dimensions exactes issues de ces métriques, puis inscrire le cadre natif et son contrôle avant de les écrire.
      - [x] Créer `Contracts/WindowsXPFont.cs` et `Services/Appearance/WindowsXPSystemMetrics.cs`, puis modifier `WindowsXPResources.cs` : utiliser les couleurs système réelles et Trebuchet MS 10 points gras pour les titres, Tahoma 8 points pour les champs, ainsi que les ombres actives et textes inactifs des trois palettes.
      - [x] Créer `Constants/WindowsXPMetrics.cs` et `Views/Windows/Common/WindowsXPWindow.cs` : posséder une fenêtre native avec son contenu emprunté, ses commandes système, sa palette et son nettoyage dans finally ; conserver les refus de fermeture.
      - [x] Créer `Styles/WindowsXPChrome.xaml` : composer le cadre avec les découpes originales, les glyphes, les titres et ombres appropriés, les commandes actives/inactives et les dimensions natives, sans importer le cadre Windows 95.
      - [x] Modifier `src/RetroMediaLab.App/Theme/OS/Editions/WindowsXP/Views/Controls/Common/WindowsXPImage.cs` : rendre le constructeur public pour son instanciation XAML ; reconstruire le Debug avec tous les modules.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPWindowTests.cs` : vérifier fenêtre native, commandes, refus/accord de fermeture, changement de palette et détachement du contenu ; capturer les trois cadres dans `artifacts/themes-review/screens/OS/WindowsXP/NativeWindows.png`, puis supprimer le scénario.
      - [x] Créer `Views/Controls/Windows/WindowsXPSystemMenu.cs`, puis modifier `Views/Windows/Common/WindowsXPWindow.cs` : remplacer le menu système du Windows hôte par le menu Luna local, relié aux commandes et traductions existantes ; ouvrir avec Alt+Espace ou le bouton système et fermer/détacher dans finally.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPSystemMenuTests.cs` : vérifier les commandes, traductions, palettes et détachement du menu sans lancer les commandes natives Déplacer/Dimensionner ; supprimer le scénario après contrôle.
      - [x] Modifier `docs/tasks/application-themes.md` après le cadre pour détailler `Views/Controls/Desktop/WindowsXPDesktop.cs`, son menu Démarrer à deux colonnes, sa barre des tâches et les vues applicatives à reconstruire.
      - [x] Créer `Contracts/WindowsXPLauncherDefinition.cs`, `Enums/WindowsXPProgramGroup.cs` et `Constants/WindowsXPDesktopMetrics.cs`, puis modifier `Services/Appearance/WindowsXPSystemMetrics.cs` : décrire les lanceurs fournis par le propriétaire et les dimensions du bureau, utiliser sa couleur originale, sans posséder les fenêtres ni les sessions empruntées.
      - [x] Créer `Views/Controls/Desktop/WindowsXPDesktop.cs` et `Styles/WindowsXPDesktop.xaml` : bureau avec double clic et clavier, menu Démarrer à deux colonnes utilisant les panneaux originaux, barre des tâches Luna et accès permanents aux thèmes ; détacher les événements, les fenêtres empruntées et le menu dans finally.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPDesktopTests.cs` : contrôler les lanceurs, la liste longue, la restitution des fenêtres empruntées et les trois palettes ; modifier `WindowsXPSystemMetrics.cs` et `WindowsXPDesktop.cs` pour appliquer les polices et couleurs de texte originales après inspection ; produire les captures du bureau et supprimer les artefacts temporaires du scénario.
      - [x] Créer `artifacts/themes-review/.work/xp-icons/extract.py` : lire par plages le répertoire ISO et les fichiers SHELL32.DL_/USER32.DL_ du média XP identifié, décompresser sans exécuter les DLL et exporter leurs groupes d’icônes dans ce dossier temporaire pour sélection.
      - [x] Créer les planches temporaires `artifacts/themes-review/.work/xp-icons/shell-*.png` et `user.png` : identifier visuellement les groupes conservés avant copie, puis les supprimer avec le dossier temporaire.
      - [x] Modifier `artifacts/themes-review/.work/xp-icons/extract.py` : extraire aussi EXPLORER.EX_ en conservant les deux téléchargements précédents, exporter ses bitmaps pour identifier le drapeau Démarrer original ; supprimer ces intermédiaires après sélection.
      - [x] Créer `Assets/Icons/`, `Assets/Images/StartLogo.png`, `Enums/WindowsXPIcon.cs` et `Services/Images/WindowsXPApplicationIcons.cs`, puis modifier `src/RetroMediaLab.App/RetroMediaLab.App.csproj` : conserver les seules icônes XP nécessaires et le bitmap 143 du bouton Démarrer, charger les images figées, conserver les images de machines et de contenus fournies par les modules.
      - [x] Modifier `docs/design/themes-rebuild.md` : consigner les empreintes des trois binaires XP utilisés et les identifiants des seules images retenues, avant suppression des intermédiaires.
      - [x] Supprimer `artifacts/themes-review/.work/xp-icons/` après sélection des icônes et de leurs empreintes, y compris le script, les archives, DLL et exports inutilisés.
      - [x] Modifier `Views/Controls/Desktop/WindowsXPDesktop.cs`, `Constants/WindowsXPDesktopMetrics.cs` et `Views/Windows/Common/WindowsXPWindow.cs` : compléter le bouton Démarrer avec le drapeau original, les lanceurs et fenêtres avec leurs icônes appropriées avant intégration, conserver l’accès Thèmes visible.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPIconsTests.cs` : charger les vingt icônes et le drapeau, vérifier dimensions/transparence et cache figé, modifier `WindowsXPDesktop.cs` et `WindowsXPImageDefinition.cs` pour dimensionner Démarrer selon son libellé traduit et ses marges natives après inspection, capturer le bureau et Démarrer avec les icônes XP puis supprimer le scénario.
      - [x] Modifier `docs/tasks/application-themes.md` après le bureau : détailler les vues Lecture, Écriture, Conversion, Explorateur, Visualisation et Émulation XP avant leur reconstruction et leur raccordement.
      - [x] Créer `Views/Controls/Read/AcquisitionView.xaml` et `.xaml.cs`, puis `Views/Controls/Write/WriteView.xaml` et `.xaml.cs` : reconstruire les pages XP avec onglets Brut/Format et historique séparé, groupes destination/profil et barre d’actions ; reprendre les points fonctionnels existants et les ressources Luna locales, sans reprendre les conteneurs visuels Windows 95.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPAcquisitionTests.cs` : contrôler ensemble Lecture/Écriture, leurs sélections/commandes et détachement ; modifier `Services/Appearance/WindowsXPResources.cs` pour isoler aussi les contrôles sans modèle spécifique Luna du style global Base, capturer les pages à 800×600, puis supprimer le scénario.
      - [x] Modifier `docs/tasks/application-themes.md` après Lecture/Écriture : détailler les fichiers Conversion, Explorateur, Visualisation et Émulation ainsi que leurs dialogues avant de les reconstruire.
      - [x] Modifier `Styles/WindowsXPControls.xaml`, `Services/Appearance/WindowsXPResources.cs` et `WindowsXPSystemMetrics.cs` : terminer les contours et couleurs de groupes hors onglets avec les métriques XP, vérifier leur rendu avec les vues de conversion avant livraison.
      - [x] Créer `Enums/AcquisitionOptionField.cs`, `Enums/WriteOptionField.cs`, `Enums/ConversionOptionField.cs`, les trois listes `Constants/*OptionsFields.cs` et `Views/Controls/Common/WindowsXPPropertyPages.cs` : décrire les champs existants, organiser les pages de propriétés XP et conserver la portée des ressources dans chaque propriétaire.
      - [x] Créer `Views/Controls/Conversion/ConversionView.xaml`, `.xaml.cs` et `ConversionFormatRow.cs`, puis `Views/Dialogs/Conversion/ConversionOptionsView.cs` : reconstruire source, formats de sortie multiples, destination et options avancées avec les points fonctionnels existants, sans déplacer les conteneurs Base.
      - [x] Créer `Views/Dialogs/Read/AcquisitionOptionsView.cs` et `Views/Dialogs/Write/WriteOptionsView.cs` : reconstruire les champs avancés XP à partir des relais de valeurs existants, conserver disponibilités et commandes.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPConversionTests.cs` : contrôler ensemble Conversion et les trois dialogues avancés, rendre leurs captures et les contours des groupes dans les trois palettes, puis supprimer le scénario.
      - [x] Créer `Views/Controls/Explorer/ExplorerView.xaml`, `.xaml.cs` et `WindowsXPFolderBranch.cs` : reconstruire l’exploration XP sur son modèle et ses icônes de contenu existants, conserver navigation, consultation et commandes, utiliser les styles de barre d’outils Luna et ne dessiner aucune case sur les dossiers feuilles.
      - [x] Créer `Views/Controls/Explorer/ExplorerDetailsView.cs` : présenter les propriétés vivantes dans les pages XP, traduire les intitulés avec les ressources existantes et détacher leurs abonnements à la fermeture.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPExplorerTests.cs` : contrôler navigation, propriétés, progression, commandes et détachement en un lot ; ajuster les colonnes de `ExplorerView.xaml` et l’alignement natif dans `WindowsXPControls.xaml` après inspection des captures, puis supprimer le scénario.
      - [x] Créer `Views/Controls/Visualization/VisualizationView.xaml`, `.xaml.cs`, `VisualizationOverviewView.cs`, `Constants/WindowsXPVisualizationMetrics.cs` et `Styles/WindowsXPVisualization.xaml` : reconstruire les commandes XP autour des dessins et surfaces empruntés au visualiseur existant, conserver les couleurs de qualité et restituer chaque surface à la fermeture.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPVisualizationTests.cs` : contrôler les cinq représentations, commandes et restitutions, capturer un rendu de pistes et une carte de blocs avec les dessins existants, puis supprimer le scénario.
      - [x] Créer `Views/Controls/Emulation/Machine/EmulationView.xaml` et `.xaml.cs` : bibliothèque XP avec images des machines existantes, nom et marque, ouverture et actualisation par le contrôleur existant, ressources Luna et détachement.
      - [x] Créer `Views/Controls/Emulation/Machine/MachineView.xaml` et `.xaml.cs` : menus et barre d’outils Luna autour des surfaces vidéo existantes, liste des périphériques en bas avec défilement, commandes et états conservés pour tous les modules, nettoyage dans finally.
      - [x] Créer `Views/Controls/Emulation/Machine/WindowsXPCommandToggleButton.cs` et modifier `MachineView.xaml` : conserver l’état Marche/Pause fourni par le contrôleur pendant un clic Luna, sans basculement visuel anticipé.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPEmulationTests.cs` : contrôler ensemble la bibliothèque, les surfaces empruntées et les périphériques nombreux ; capturer les vues XP et supprimer le scénario après contrôle.
      - [x] Modifier `docs/tasks/application-themes.md` après ces vues : détailler leurs dialogues spécialisés, préférences, stockage et intégration au bureau XP avant de les créer ; inclure l’accès direct aux thèmes signalé comme manquant dans Windows 3.1/95 et la propagation de la palette XP aux fenêtres et vues déjà ouvertes, sans changer le choix de thème enregistré de l’utilisateur.
      - [x] Créer `Views/Dialogs/Common/BusinessDialogView.cs` : portée Luna locale, commandes métier existantes, pied de dialogue XP, gestion Échap et détachement garanti sans fermer la source empruntée.
      - [x] Créer `Views/Controls/Emulation/Options/SettingsFieldView.cs` : reconstruire les éditeurs XP décrits par les modules, y compris nombres contraints, IPv4, ROM, listes et curseurs ; conserver les points métier et leurs validations.
      - [x] Créer `Views/Dialogs/Emulation/CoreOptionsView.cs` : pages de propriétés XP par catégorie sur les options du cœur existantes, boutons de confirmation et annulation d’origine.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPSettingsTests.cs` : vérifier valeurs, choix, contraintes et détachement des éditeurs et options du cœur en un lot, capturer les pages XP et supprimer le scénario.
      - [x] Modifier `docs/design/themes-rebuild.md` : supprimer l’espace final de la métrique XP TextColor signalé par Git, sans changer sa valeur.
      - [x] Modifier ce suivi avec les chemins de stockage XP avant leur réalisation ; conserver les dialogues Windows95 comme inventaire fonctionnel.
      - [x] Créer `Views/Controls/Emulation/Storage/StorageFieldsView.cs` : lignes de propriétés XP sur les éditeurs des modules, visibilité vivante, choix existants et détachement des liaisons.
      - [x] Créer `Views/Controls/Emulation/Storage/FloppyCreationView.cs` : séparer le format de disquette du format de fichier, afficher destination et parcours espacés avec les contrôles Luna.
      - [x] Créer `Views/Dialogs/Emulation/Storage/MediaCreationView.cs` : reprendre création, montage, annulation et état occupé des points fonctionnels existants, dans une présentation XP.
      - [x] Créer `Views/Dialogs/Emulation/Storage/FloppySettingsView.cs` : pages XP du lecteur et de ses options autorisées par le module, commande de création conservée.
      - [x] Créer `Views/Dialogs/Emulation/Storage/HardDiskSettingsView.cs` : pages XP du disque associé, création et géométrie, contraintes et tailles conservées, champs et commandes détachés dans finally.
      - [x] Créer `Views/Dialogs/Emulation/Storage/ManagedStorageView.cs` : pages XP des groupes de réglages décrits par les modules, visibilité suivie et abonnement libéré.
      - [x] Créer `Views/Dialogs/Emulation/Storage/CompactDiscSettingsView.cs` et `CartridgeSettingsView.cs` : propriétés XP utilisant les champs et validations existants du CD et des cartouches.
      - [x] Créer `Views/Dialogs/Emulation/Storage/AddStorageDeviceView.cs` et `MemoryCardDirectoryView.cs` : choix des types autorisés et configuration du dossier carte mémoire, commandes d’origine et espacements XP.
      - [x] Modifier `Views/Controls/Emulation/Storage/FloppyCreationView.cs` : retirer l’en-tête de groupe répétant le libellé du format de disquette ; conserver les deux lignes de choix et leur destination séparée.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPStorageTests.cs` : contrôler en un lot choix de formats, parcours, réglages et restitution, produire les captures XP, fermer les sources et supprimer les données temporaires.
      - [x] Modifier ce suivi avec les chemins des préférences globales, réglages de modules et sélection de firmware XP avant leur réalisation ; conserver toutes les fenêtres du registre Windows95 comme inventaire fonctionnel.
      - [x] Créer `Views/Dialogs/Emulation/FirmwareSelectionView.xaml` et `.xaml.cs` : sélecteur XP compact avec nom, fichier, compatibilité et destination, parcours du dossier et actualisation existants, sélection et enregistrement d’origine, nettoyage de la présentation.
      - [x] Modifier `Views/Dialogs/Emulation/FirmwareSelectionView.xaml` et `.xaml.cs` : lier directement le chemin sélectionné au champ XP visible, supprimer le relais texte masqué qui retarde son affichage.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPFirmwareTests.cs` : contrôler liste et sélection, états d’enregistrement et actualisation, produire une capture et attendre l’arrêt du dialogue source dans finally.
      - [x] Créer `Views/Controls/Emulation/Options/CoreManagementView.xaml` et `.xaml.cs`, puis `Views/Controls/Emulation/Storage/ModuleStorageView.xaml` et `.xaml.cs` : présentations XP de la gestion des cœurs et périphériques avec les commandes et capacités existantes.
      - [x] Créer `Views/Controls/Emulation/Network/NetworkView.cs` : tableau XP d’observation réseau avec toutes les colonnes existantes, cellules centrées verticalement, aucune modification ni sélection de lignes.
      - [x] Modifier `Views/Controls/Emulation/Options/CoreManagementView.xaml` : isoler le style du texte d’état du style global Base tout en conservant le gras sur erreur et la police XP.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPPanelsTests.cs` : contrôler les choix, commandes et états des cœurs, les capacités des périphériques et le tableau réseau vivant ; capturer les panneaux XP et supprimer le scénario sans lancer de téléchargement ni de cœur.
      - [x] Créer `Views/Controls/Emulation/Input/InputBindingsView.xaml`, `.xaml.cs`, `InputDrawingView.cs`, `ControllerPortView.xaml`, `.xaml.cs`, `ControllerOutputsView.cs`, `ModuleInputView.xaml` et `.xaml.cs` : contrôles Luna des entrées et ports existants, dessins empruntés et actions conservées, détachement garanti.
      - [x] Créer `Views/Controls/Emulation/Video/VideoProcessingView.xaml` et `.xaml.cs` : onglets XP sur les réglages et effets vidéo existants, sans changer les moteurs ou shaders.
      - [x] Créer `Views/Windows/EmulationModuleOptions/ModuleSettingsView.xaml` et `.xaml.cs` : choix de machine et catégories XP, totalité des champs et panneaux spécialisés ci-dessus, total RAM vivant et ordre firmware/extension conservés.
      - [x] Modifier `Views/Controls/Emulation/Video/VideoProcessingView.xaml` : aligner en haut la zone des propriétés limitée en hauteur pour supprimer le vide vertical visible sur la capture XP.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPModuleSettingsTests.cs` : contrôler les panneaux spécialisés et les champs des modules en un lot, capturer les pages, attendre et libérer tout chargement et supprimer les scénarios temporaires.
      - [x] Créer dans `Views/Controls/Options/` les paires `.xaml`/`.xaml.cs` `GeneralOptionsView`, `ThemeOptionsView`, `LogsOptionsView`, `HardwareOptionsView`, `EnginesOptionsView`, `ProfilesOptionsView` et `ControllersOptionsView` : propriétés XP sur les contrôleurs existants, accès Thèmes visible, choix conservés.
      - [x] Créer `Views/Windows/Preferences/PreferencesView.cs` et `Views/Windows/EmulationPreferences/EmulationPreferencesView.xaml`/`.xaml.cs` : fenêtres de propriétés XP comprenant toutes les pages, réseau et raccourcis, sans modifier les réglages enregistrés.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPPreferencesTests.cs` : contrôler les accès, liaisons et fermeture des pages, capturer et supprimer le scénario.
      - [x] Modifier ce suivi avec les fichiers d’intégration XP au bureau, d’accès direct aux thèmes Windows31/95 et de propagation de palette aux vues déjà ouvertes avant réalisation ; conserver la configuration de thème enregistrée de l’utilisateur.
      - [x] Créer `Views/Dialogs/Profiles/ProfileNameView.cs`, `Views/Dialogs/Read/ReadConflictView.cs`, `Views/Dialogs/Conversion/ConversionConflictView.cs` et `Views/Dialogs/Hardware/HardwareUnavailableView.cs` : dialogues XP de saisie et confirmation sur les résultats, choix et disponibilités existants.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPBusinessDialogsTests.cs` : contrôler les valeurs et résultats des quatre dialogues, leurs raccourcis et leur détachement, capturer et supprimer le scénario.
      - [x] Modifier `Views/Dialogs/Profiles/ProfileNameView.cs` : masquer l’avertissement de nom vide dès qu’un nom valable est saisi et détacher l’événement à la fermeture, après le défaut visible sur la capture.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPProfileValidationTests.cs` : vérifier le retour visuel après saisie, le résultat et le détachement, actualiser la capture du nom de profil et supprimer le scénario.
      - [x] Créer `Views/Dialogs/Common/MessageView.cs`, `ErrorView.cs` et `WindowsXPPropertyWindow.cs` : messages XP avec icônes historiques et boutons natifs, détails complets et garde des confirmations, fermeture et nettoyage garantis.
      - [x] Créer `Views/Dialogs/Common/FileSelectionView.xaml` et `.xaml.cs` : sélection XP de fichier/dossier avec navigation, filtres, chemin, erreurs et résultat du service existant, sans sélecteur historique d’un autre thème.
      - [x] Modifier `Constants/WindowsXPMetrics.cs` et `Styles/WindowsXPChrome.xaml` : libérer la place du titre lorsque Réduire/Agrandir sont masqués en mode dialogue ; conserver le cadre normal des fenêtres redimensionnables.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPCommonDialogsTests.cs` : contrôler tous les jeux de réponses, défaut/Échap, détails et contenu emprunté, sélection de fichiers/dossiers, filtres et fermeture attendue ; capturer les dialogues XP et supprimer le scénario et ses données.
      - [x] Créer `Views/Dialogs/Classification/ClassificationView.xaml` et `.xaml.cs`, `Views/Dialogs/Inspector/InspectorView.xaml` et `.xaml.cs`, puis `Views/Dialogs/Explorer/ExplorerIssuesView.cs` : classification, inspection et avertissements XP conservant toutes les données existantes.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPMediaDialogsTests.cs` : contrôler classification, inspection des deux faces et avertissements, capturer ces trois dialogues XP, libérer les sources et supprimer le scénario.
      - [x] Créer `Views/Controls/Tools/ToolsView.xaml` et `.xaml.cs`, `Views/Controls/Console/TerminalView.xaml` et `.xaml.cs`, puis `Views/Windows/Tools/ToolView.cs` : commandes des outils et journal XP, annulation et attente du runner existant à la fermeture.
      - [x] Créer `Views/Windows/About/AboutView.cs`, `Views/Windows/Logs/LogHistoryView.xaml` et `.xaml.cs`, `Views/Windows/Updates/UpdatesView.xaml` et `.xaml.cs`, puis `Views/Windows/Conversion/FileMigrationView.cs` : fenêtres XP de toutes les fonctions globales restantes, opérations et fermeture existantes conservées.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPSecondaryWindowsTests.cs` : vérifier en un lot les dialogues communs et fenêtres secondaires ci-dessus, produire les captures, attendre leur fermeture et supprimer le scénario et ses artefacts de travail. Sept scénarios XP réussis après correction du faux runner (état inactif avant son lancement), dont annulation et attente réelle ; trois scénarios Windows 3.1 associés réussis. Captures examinées : outils, console, À propos, historique, migration et mises à jour. Données temporaires supprimées dans finally, HWND détruits vérifiés ; scénario supprimé après usage.
      - [x] Modifier `Services/Appearance/WindowsXPResources.cs` et `Views/Windows/Common/WindowsXPWindow.cs` : propager un changement de palette aux ressources XP des vues déjà ouvertes sans recréer leurs contrôleurs ni perdre les sélections.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPPaletteTests.cs` : changer Bleu/Argent/Olive avec des pages actives et inactives, conserver sélection et saisies, vérifier détachement et restauration des ressources empruntées, capturer et fermer les fenêtres natives. Deux scénarios réussis ; mêmes instances conservées, ressources du demandeur restituées, HWND détruit après fermeture, captures Palette-Blue/Silver/Olive produites, Olive inspectée. Scénario temporaire supprimé.
      - [x] Créer `Services/Presentation/WindowsXPWindowRouter.cs` et `.Routes.cs` : relier les 26 fenêtres fonctionnelles à leurs présentations XP, conserver leur ResizeMode et les résultats/refus de fermeture, attendre et libérer les fenêtres natives dans finally.
      - [x] Créer `Views/Controls/Desktop/WindowsXPMenuMirror.cs` et modifier `Views/Controls/Desktop/WindowsXPDesktop.cs` : raccorder les menus existants dans Démarrer sans déplacer leurs contrôles ; détacher tous les relais à la fermeture.
      - [x] Modifier `Services/Appearance/WindowsXPResources.cs` : exposer la palette héritée aux propriétaires des fenêtres pour relier leurs cadres au choix courant.
      - [x] Créer `Services/Presentation/WindowsXPWorkspace.cs`, `.Applications.cs` et `.Machines.cs` : bureau XP sur les actions existantes de l’application, les sessions des machines et leurs surfaces ; conserver tous les accès aux fonctions et gérer leur fermeture. Compilation et trois scénarios existants Windows 3.1 réussis ; validation d’intégration XP toujours prévue ci-dessous, aucune validation globale des modules déduite de ces trois scénarios.
      - [x] Modifier `Views/Controls/Desktop/WindowsXPDesktop.cs` et `Services/Presentation/WindowsXPWorkspace.cs` : relier les images, le texte de Démarrer et la barre des tâches à la palette du bureau déjà ouvert ; conserver ses lanceurs et ses fenêtres sans reconstruction.
      - [x] Modifier `Theme/Base/Views/Controls/Shell/MainMenu.xaml.cs`, `Controllers/MainWindow/MainWindowLifecycleController.cs`, `Theme/Base/Views/Windows/Shell/MainWindow.ComponentConnections.cs` et `MainWindow.EventsAndCommands.cs` : réutiliser le parcours Préférences existant avec la section Thèmes, sans nouveau réglage ni changement du thème enregistré.
      - [x] Modifier `Views/Controls/Desktop/WindowsXPDesktop.cs`, `Services/Presentation/WindowsXPWorkspace.cs`, `Theme/OS/Editions/Windows31/Views/Windows/Desktop/Windows31ProgramManager.xaml.cs`, `Theme/OS/Editions/Windows95/Views/Controls/Desktop/Windows95Desktop.cs` et `Windows95Workspace.cs` : rendre l’accès direct Thèmes visible, le relier au parcours Préférences et détacher les événements à la fermeture.
      - [x] Créer `WindowsXPEdition.cs`, puis modifier `Theme/OS/OSDefinition.cs`, `Dictionaries/Theming/ThemeCatalog.cs`, `Resources/00-Base/Options.resx` et les `Resources/*/Options.resx` localisés : enregistrer XP comme déclinaison OS avec ses palettes internes, traduire Bleu/Argent/Olive avec Argos et conserver le nom Windows XP dans la base commune ; conserver Base, Studio, Windows31 et Windows95.
      - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/ThemeEditionSelectionTests.cs` : inclure la nouvelle déclinaison XP dans la vérification du catalogue OS tout en conservant les vérifications de choix et persistance existantes.
      - [x] Modifier `Views/Controls/Desktop/WindowsXPDesktop.cs` et `Constants/WindowsXPDesktopMetrics.cs` : réduire les boutons des tâches à la largeur disponible et proposer deux flèches de défilement en cas de débordement, sans barre horizontale recouvrant les boutons ; conserver une hauteur de barre des tâches de 30 pixels.
      - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/Windows31ThemeOptionsTests.cs` : attendre les trois déclinaisons OS publiées dans le catalogue, sans modifier les contrôles de choix et de persistance du panneau Windows 3.1.
      - [x] Modifier `Views/Controls/Emulation/Options/SettingsFieldView.cs` : conserver l’état activé et la visibilité des TextBox imbriquées, notamment la taille HDD désactivée lorsque la capacité prédéfinie est sélectionnée ; vérifier cette liaison dans le scénario d’intégration existant.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindowsXPIntegrationTests.cs` : parcourir les accès, dialogues, thèmes et palettes à chaud, restituer les contrôles et surfaces de tous les modules, capturer les fenêtres, fermer/attendre chaque handle et supprimer les artefacts temporaires. Parcours effectif des 24 modules livrés, 213 machines et 1 830 onglets XP ; huit applications ouvertes sans duplication ; 26 routages natifs ouverts/fermés ; trois confirmations et leurs refus de fermeture ; trois palettes sans reconstruction ; trente boutons de tâches avec défilement ; deux contrôleurs/surfaces restitués sans arrêt du cœur emprunté. Six scénarios d’intégration plus les trois cas de confirmation exécutés avec succès après correction des attentes de vue (la fabrique est restaurée, la vue transitoire est recréée). HWND détruits vérifiés, dossiers de données supprimés dans finally et scénario temporaire supprimé. Les cœurs matériels réels et tous les GPU ne sont pas démarrés par ces scénarios.
      - [x] Créer `Assets/Previews/Blue.png`, `Silver.png` et `Olive.png` à partir des captures vérifiées du bureau XP, puis modifier `WindowsXPEdition.cs` : fournir ces aperçus au sélecteur OS.
      - [x] Livrer Windows XP pour validation utilisateur.
        - [x] Modifier `docs/design/themes-rebuild.md` et ce suivi avec les limites constatées, les captures finales et la validation : 213 contrôles groupés thèmes/localisation réussis après suppression des scénarios temporaires, neuf scénarios d’intégration XP exécutés avant suppression, trois palettes et 26 routages contrôlés.
        - [x] Remplacer `build/Debug/Retro Media Lab/retromedialab.exe` et les DLL livrées avec `scripts/local-building.cmd --building=debug --modules=A` : script terminé avec le code 0 ; les 24 manifestes et leurs assemblies sont présents ; l’assembly App livrée correspond exactement par SHA-256 à celle compilée.



- [x] Terminer Orbit puis arrêter le travail sur les thèmes (demande utilisateur du 1 novembre 2026).

  - [x] Définir les trois compositions et reconstruire Orbit autour des machines.
    - [x] Modifier `docs/design/themes-rebuild.md` : préciser les compositions distinctes Orbit/Matrix/Atlas, les parcours de toutes les fonctions, les fenêtres et leur comportement ; établir l’inventaire Orbit sans réutiliser les conteneurs Base ni OS.
    - [x] Créer `src/RetroMediaLab.App/Theme/Orbit/Constants/OrbitMetrics.cs`, `Enums/OrbitArea.cs`, `Dark.xaml`, `Light.xaml` et `Styles/OrbitControls.xaml` : métriques compactes, palettes et modèles indépendants pour boutons, entrées, choix, listes, onglets, cases, curseurs, défilement et états de focus/désactivation.
    - [x] Créer `src/RetroMediaLab.App/Theme/Orbit/Services/Appearance/OrbitResources.cs` : portée locale des styles Orbit, restitution exacte des ressources de l’appelant et libération dans finally.
    - [x] Modifier les `src/RetroMediaLab.App/Resources/*/Options.resx` avec Argos : libellés Machines/Médias/Journal/Commandes, recherche et état vide de la palette, choix d’une machine et contexte ; employer ces mêmes clés dans les vues Orbit.
    - [x] Créer `src/RetroMediaLab.App/Theme/Orbit/Contracts/OrbitCommand.cs`, `Services/Navigation/OrbitCommandCatalog.cs` et `Views/Controls/Navigation/OrbitCommandPalette.xaml` / `.xaml.cs` : recherche et exécution des commandes existantes, état et disponibilité dynamiques, détachement des abonnements ; aucune suppression des commandes Options/Émulation/Aide.
    - [x] Créer `src/RetroMediaLab.App/Theme/Orbit/Views/Controls/Shell/OrbitShell.xaml` / `.xaml.cs` : rail Machines/Médias/Journal/Commandes, espace central dédié à la machine, tiroir contextuel, barre d’état et journal repliable ; dessiner les éléments sans héberger les pages Base.
    - [x] Modifier `Theme/Orbit/Styles/OrbitControls.xaml`, `Views/Controls/Shell/OrbitShell.xaml` / `.xaml.cs`, `Views/Controls/Navigation/OrbitCommandPalette.xaml` et `Services/Navigation/OrbitCommandCatalog.cs` : éviter le débordement des curseurs de défilement, détacher la fermeture du journal, nommer visiblement la recherche et exclure les racines de menu vides.
    - [x] Modifier les `Resources/*/Options.resx` avec Argos pour « Activity log » et « Search for a command », puis corriger le libellé français de session : supprimer l’ambiguïté « Journal officiel » et « Commandes de recherche ».
    - [x] Corriger `Theme/Orbit/Styles/OrbitControls.xaml`, `Views/Controls/Shell/OrbitShell.xaml`, `Constants/OrbitMetrics.cs`, `Resources/fr-FR/Options.resx` et `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryOrbitShellTests.cs` : propager la couleur du contrôle au texte généré par les présentateurs, rendre les libellés du rail lisibles et vérifier le contraste du texte effectivement dessiné. Huit scénarios réussis, texte des présentateurs et labels contrôlé, palettes claire/sombre et quatre captures de taille de shell réactualisées.
    - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryOrbitShellTests.cs` : vérifier commandes, recherche, navigation, contrastes, restitution et tailles 1280×800 / 1920×1080, capturer dans `artifacts/themes-review/screens/Orbit/` et supprimer les données de travail.
  - [x] Raccorder les machines et les opérations Orbit.
    - [x] Créer `Theme/Orbit/Views/Controls/Emulation/Machine/EmulationView.xaml` / `.xaml.cs`, `MachineView.xaml` / `.xaml.cs`, `OrbitCommandToggleButton.cs` et `Views/Controls/Console/TerminalView.xaml` / `.xaml.cs` : bibliothèque et sessions, commandes/contextes/périphériques, vidéo et journal ; conserver les images des machines et les propriétaires des surfaces, faire suivre aux boutons leur état confirmé par le contrôleur.
    - [x] Modifier `Theme/Orbit/Views/Controls/Emulation/Machine/MachineView.xaml` : garder les actions du support sélectionné visibles sous la liste défilante, même avec trente périphériques ; limiter et faire défiler séparément les commandes du support.
    - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryOrbitMachineTests.cs` : contrôler commandes et états, périphériques nombreux et tapes, transfert/restitution des surfaces, journal, captures et nettoyage ; aucun cœur matériel réel requis pour ce contrôle de présentation. Cinq scénarios réussis : états confirmés, trente lecteurs avec commandes visibles, tape, deux palettes, surface empruntée et restitution, bibliothèque et journal ; captures inspectées, scénario temporaire supprimé.
    - [x] Reconstruire les opérations de lecture, écriture et conversion.
      - [x] Créer `Theme/Orbit/Views/Controls/Read/AcquisitionView.xaml` / `.xaml.cs` et `Write/WriteView.xaml` / `.xaml.cs` : parcours par source/réglages/destination, brut/format distincts, historique visible, profils et actions avancées conservés, sans menus ou conteneurs OS.
      - [x] Modifier `Theme/Orbit/Styles/OrbitControls.xaml` : disposer les en-têtes Brut/Format sur un véritable TabPanel horizontal, conserver navigation et sélection ; vérifier les captures d’acquisition.
      - [x] Modifier `docs/tasks/application-themes.md` et `docs/design/themes-rebuild.md` : appliquer la demande utilisateur du 1 novembre, terminer uniquement Orbit puis arrêter les thèmes ; laisser explicitement OS incomplet et ne pas engager Matrix/Atlas ni les autres déclinaisons OS.
      - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryOrbitOperationsTests.cs` : vérifier éditions aller-retour, choix brut/format, profils, actions source, historique, captures claire/sombre et détachement. Quatre scénarios réussis, onglets horizontaux et captures inspectés ; scénario temporaire supprimé.
      - [x] Modifier `docs/tasks/application-themes.md` et `docs/design/themes-rebuild.md` : retirer Windows 98 des manques OS actuels ; conserver cette déclinaison comme possibilité ultérieure uniquement, selon la précision utilisateur.
      - [x] Créer `Theme/Orbit/Views/Controls/Conversion/ConversionView.xaml` / `.xaml.cs` et `Conversion/ConversionFormatRow.cs` : source et résultats sur le même écran, formats et extensions distincts, profils, migration et commandes existantes conservés.
      - [x] Créer `Theme/Orbit/Views/Controls/Common/OrbitOptionPages.cs`, `Constants/{AcquisitionOptionsFields.cs,WriteOptionsFields.cs,ConversionOptionsFields.cs}`, `Enums/{AcquisitionOptionField.cs,WriteOptionField.cs,ConversionOptionField.cs}` et `Views/Dialogs/{Read/AcquisitionOptionsView.cs,Write/WriteOptionsView.cs,Conversion/ConversionOptionsView.cs}` : options regroupées dans des vues Orbit à rubriques, liaisons et contraintes des sources conservées.
    - [x] Créer `Theme/Orbit/Views/Controls/Explorer/{ExplorerView.xaml,ExplorerView.xaml.cs,ExplorerDetailsView.cs,OrbitFolderBranch.cs}` : document à deux colonnes, source et informations, fichiers et pistes, actions complètes et chevrons sans cases ; réutiliser les descriptions et icônes existantes.
    - [x] Créer `Theme/Orbit/Views/Controls/Visualization/{VisualizationView.xaml,VisualizationView.xaml.cs,VisualizationOverviewView.cs}`, `Constants/OrbitVisualizationMetrics.cs` et `Styles/OrbitVisualization.xaml` : représentations coordonnées, commandes et propriétés dans un panneau latéral ; réutiliser les décodeurs et rendre les surfaces empruntées, préserver les couleurs des données.
    - [x] Créer `Theme/Orbit/Views/Dialogs/Common/BusinessDialogView.cs` : liaisons des actions métier, contenu Orbit, clavier et détachement ; le propriétaire fonctionnel conserve sa fenêtre.
    - [x] Créer `Theme/Orbit/Views/Controls/Tools/ToolsView.xaml` / `.xaml.cs` et `Views/Windows/Tools/ToolView.cs` : actions d’entretien et paramètres complets, exécution/annulation/attente conservées.
  - [x] Reconstruire toutes les fenêtres et réglages Orbit.
    - [x] Créer `Theme/Orbit/Views/Windows/Common/OrbitWindow.cs`, `Constants/OrbitWindowMetrics.cs` et `Styles/OrbitChrome.xaml` : cadre natif Orbit, commandes de fenêtre, refus de fermeture et restitution des documents.
    - [x] Créer `Theme/Orbit/Views/Dialogs/Common/{MessageView.cs,ErrorView.cs,FileSelectionView.xaml,FileSelectionView.xaml.cs,OrbitPropertyWindow.cs}` : dialogues, saisies et résultats, refus de fermeture, clavier et restitution des documents.
    - [x] Modifier `Theme/Orbit/Views/Controls/Conversion/ConversionFormatRow.cs` et `Styles/OrbitChrome.xaml` : appliquer explicitement le modèle Orbit aux lignes dérivées et rendre le dessin des boutons de titre autonome.
    - [x] Modifier `docs/design/themes-rebuild.md` : consigner les vues écrites dans ce lot, les contrôles volontairement différés, le raccordement et les fenêtres restant à faire ; ne pas annoncer Orbit livré.
    - [x] Créer `Theme/Orbit/Views/Controls/Options/{GeneralOptionsView,ThemeOptionsView,LogsOptionsView,HardwareOptionsView,EnginesOptionsView,ProfilesOptionsView,ControllersOptionsView}.xaml` et leurs `.xaml.cs` et `Views/Windows/Preferences/PreferencesView.cs` : navigation verticale, rubriques dépliables, tous les choix et commandes conservés, sélecteur du thème avec formulaire et aperçu côte à côte.
    - [x] Créer `Theme/Orbit/Views/Controls/Emulation/Input/InputBindingsView.xaml` / `.xaml.cs` et `Emulation/Network/NetworkView.cs` : raccourcis existants et tableau réseau en observation seule, couleurs Orbit et détachement.
    - [x] Créer `Theme/Orbit/Views/Windows/EmulationPreferences/EmulationPreferencesView.xaml` / `.xaml.cs` : chemins, raccourcis, réseau et configurations dans des rubriques dépliables, tous les choix et commandes conservés.
    - [x] Créer `Theme/Orbit/Views/Controls/Emulation/Options/{SettingsFieldView.cs,CoreManagementView.xaml,CoreManagementView.xaml.cs}`, `Storage/{ModuleStorageView.xaml,ModuleStorageView.xaml.cs}`, `Input/{InputDrawingView.cs,ControllerPortView.xaml,ControllerPortView.xaml.cs,ControllerOutputsView.cs,ModuleInputView.xaml,ModuleInputView.xaml.cs}`, `Video/{VideoProcessingView.xaml,VideoProcessingView.xaml.cs}` et `Views/Windows/EmulationModuleOptions/ModuleSettingsView.xaml` / `.xaml.cs` : champs de tous les modules, rubriques dépliables, modèles Orbit explicites pour les entrées contraintes, couleurs des rendus conservées. Les dossiers Storage/Input/Video sont sous `Views/Controls/Emulation/`, conformément aux vues existantes.
    - [x] Créer `Theme/Orbit/Views/Dialogs/Emulation/{CoreOptionsView.cs,FirmwareSelectionView.xaml,FirmwareSelectionView.xaml.cs,Storage/MediaCreationView.cs,Storage/FloppySettingsView.cs,Storage/HardDiskSettingsView.cs,Storage/ManagedStorageView.cs,Storage/CompactDiscSettingsView.cs,Storage/CartridgeSettingsView.cs,Storage/AddStorageDeviceView.cs,Storage/MemoryCardDirectoryView.cs}` et `Views/Controls/Emulation/Storage/{StorageFieldsView.cs,FloppyCreationView.cs}` : toutes les créations et configurations, formats explicites et validations réutilisés.
    - [x] Créer `Theme/Orbit/Views/Dialogs/{Profiles/ProfileNameView.cs,Read/ReadConflictView.cs,Conversion/ConversionConflictView.cs,Hardware/HardwareUnavailableView.cs,Classification/ClassificationView.xaml,Classification/ClassificationView.xaml.cs,Inspector/InspectorView.xaml,Inspector/InspectorView.xaml.cs,Explorer/ExplorerIssuesView.cs}` et `Views/Windows/{About/AboutView.cs,Logs/LogHistoryView.xaml,Logs/LogHistoryView.xaml.cs,Updates/UpdatesView.xaml,Updates/UpdatesView.xaml.cs,Conversion/FileMigrationView.cs}` : toutes les fonctions secondaires et leurs fermetures.
  - [x] Intégrer et livrer Orbit.
    - [x] Créer `Theme/Orbit/Services/Presentation/{OrbitWorkspace.cs,OrbitWorkspace.Applications.cs,OrbitWorkspace.Machines.cs,OrbitWindowRouter.cs,OrbitWindowRouter.Routes.cs}` : relier le shell, les sources fonctionnelles et les sessions, fermer et attendre les propriétaires dans finally.
    - [x] Modifier `Theme/Orbit/Dark.xaml`, `Light.xaml`, créer `Theme/Orbit/Controls.xaml`  : exposer les ressources communes de la palette et inclure les aperçus et contrôles Orbit.
    - [x] Modifier `src/RetroMediaLab.Infrastructure/Settings/AppThemeDesign.cs`, `Dictionaries/Theming/ThemeCatalog.cs`, `Resources/00-Base/Options.resx` et les `Resources/*/Options.resx` traduits avec Argos : enregistrer Orbit seulement après réalisation de toutes ses vues, préserver les identifiants et les thèmes validés ; créer `Theme/Orbit/LightPreview.png` et `DarkPreview.png` à partir des captures vérifiées.
    - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/ThemeEditionSelectionTests.cs` : inclure Orbit dans le catalogue attendu, sans modifier les attentes des déclinaisons OS.
    - [x] Modifier `Theme/Orbit/Views/Controls/Shell/OrbitShell.xaml.cs` : masquer le bouton Contexte quand aucun contexte n’est fourni, au lieu d’un bouton inactif sans contenu.
    - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryOrbitIntegrationTests.cs` et `TemporaryOrbitConversionTests.cs` : une fois toutes les modifications terminées, contrôler formats/extensions/options, tous les accès et dialogues, modules/machines/champs, surfaces et captures ; supprimer les scénarios et données de travail après fermeture effective. Tests et vérifications regroupés à la fin, conformément à la demande utilisateur. Le parcours multi-module doit appeler le dispatcher par machine pour respecter la limite existante de 30 secondes par appel ; ne pas augmenter le délai de l’infrastructure.
    - [x] Modifier `docs/design/themes-rebuild.md` et ce suivi avec les résultats et limites ; remplacer `build/Debug/Retro Media Lab/` par le build Debug officiel `--modules=A`, vérifier exécutable/modules et présenter Orbit pour validation avant commit.
  - Matrix et Atlas : ne pas engager leur réalisation. Demande utilisateur du 1 novembre : arrêt du travail sur les thèmes après Orbit. Les compositions documentées restent des propositions non réalisées.

- [ ] Valider la totalité du résultat.

  - [ ] Modifier ce suivi après validation explicite de Windows XP par l’utilisateur, puis enregistrer les fichiers XP et leurs raccordements dans Git ; aucun commit XP avant cette validation. Ce point différé ne bloque pas la réalisation indépendante des autres thèmes.

  - [x] Commits Windows 3.1 et Windows 95 demandés par l’utilisateur.
    - [x] Créer `artifacts/themes-review/.work/commit-os-themes/stage.py` : préparer deux états d’index séparés, avec les dépendances communes de Windows 3.1 puis l’enregistrement de Windows 95, sans modifier les fichiers de travail XP.
    - [x] Modifier `tests/RetroMediaLab.Tests/Interface/Themes/MediaVisualizationPresentationTests.cs` : supprimer la ligne blanche finale signalée par le contrôle de l’index, sans changer le scénario.
    - [x] Enregistrer dans Git les fichiers Windows 3.1, les relais communs et leurs traductions/tests ; exclure le code Windows 95 et Windows XP de ce premier commit.
    - [x] Modifier `src/RetroMediaLab.App/Theme/OS/Editions/Windows95/Styles/Windows95Chrome.xaml` : centrer explicitement le bouton système, son présentateur et son image de 16 pixels dans le bandeau de titre.
    - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryWindows95CaptionTests.cs` : contrôler le centrage avec plusieurs icônes, capturer le cadre et vérifier la destruction des fenêtres natives dans finally.
    - [x] Modifier les cinq fichiers Windows95 `Views/Controls/Console/TerminalView.xaml`, `Views/Controls/Options/GeneralOptionsView.xaml`, `HardwareOptionsView.xaml`, `ThemeOptionsView.xaml` et `Views/Dialogs/Inspector/InspectorView.xaml` : supprimer les espaces seuls signalés par Git avant commit.
    - [x] Enregistrer dans Git les fichiers et l’enregistrement Windows 95, avec la correction du centrage ; exclure Windows XP.
    - [x] Supprimer `artifacts/themes-review/.work/commit-os-themes/stage.py` et son dossier après contrôle des deux commits ; conserver les travaux XP et le suivi non committés.

  - [x] Livrer le centrage Windows 95 déjà corrigé et contrôlé.
    - [x] Remplacer `build/Debug/Retro Media Lab/retromedialab.exe` et les DLL livrées avec `scripts/local-building.cmd --building=debug --modules=A`, puis vérifier la réussite du script et la présence des modules.
    - [x] Modifier `docs/tasks/application-themes.md` : inscrire le résultat du build et cocher la livraison seulement après vérification. Script terminé avec le code 0 le 1 novembre 2026 ; exécutable livré, 24 manifestes et leurs DLL présents. Le build précédent datait du 30 octobre et ne contenait pas le centrage committé le 1 novembre.

  - [ ] Modifier ce suivi avec les fichiers de scénarios et captures à produire pour toutes les fonctions, tous les dialogues et tous les thèmes, avant leur réalisation ; prévoir la suppression des tests et sessions temporaires, puis un build officiel Debug tous modules et la préparation des commits sans les créer.

Contrôles de fondation du 31 octobre : 164 tests ciblés réussis (thèmes, navigation, préférences, lecture, historique et raccordement). Aucun nouveau thème déclaré terminé ou livré.

Contrôles après le premier formulaire indépendant : 173 tests ciblés réussis. Source Base/Studio conservée, champs nouveaux vérifiés, aucune réutilisation des contrôles Base dans ce formulaire ; aucun thème OS ou autre déclaré achevé.

Options d’acquisition indépendantes : trois tests réussis le 31 octobre ; saisies et événements métier conservés, listes traduites distinctes et désabonnement contrôlé. Compilation de test réussie ; aucun build livré ni nouveau thème déclaré terminé.

Contrôles après options et placement Windows 3.1 : 181 tests ciblés réussis, dont la restauration normal/agrandi depuis une icône, les bornes de déplacement et la fermeture terminale. Aucun nouveau thème enregistré.

Cadre OS : trois tests supplémentaires réussis après correction de l’accessibilité des ressources x:Static ; commandes de titre, activation, réduction et détachement du document emprunté vérifiés. Comparaison visuelle, polices et menus non achevés.

Inspection visuelle des deux formulaires : rendus réels examinés à 940×600 et 780×540, sans fenêtre native. Le bouton des options avancées était tronqué ; sa colonne s’adapte maintenant à son libellé et le nouveau rendu confirme la correction. Les captures de contrôle et leur test temporaire ont été supprimés. Les polices historiques, les autres contrôles, le menu système et les interactions de déplacement restent explicitement à réaliser ; le rendu courant n’est pas validé comme reproduction exacte.

Validation du lot courant : 187 contrôles ciblés réussis après chargement des gabarits OS, correction de largeur et suppression du scénario visuel temporaire. `git diff --check` réussi. Aucun thème supplémentaire offert dans le sélecteur, aucun commit et aucun build officiel livré dans ce lot. Prochaine action : compléter les familles de contrôles restantes puis réaliser le bureau et les menus système, avant l’intégration des autres fonctions et déclinaisons ; Orbit, Matrix et Atlas restent dans le périmètre intégral.

Contrôles étendus : sept scénarios réussis pour les primitives, les deux orientations, le défilement réel, les commandes de plage et les sélections/expansions. La compilation a d’abord révélé des noms de commandes Slider incorrects dans les nouveaux gabarits ; corrigés avant validation.

Captures demandées par l’utilisateur : `artifacts/themes-rebuild/current/acquisition.png` (1100×660) et `options.png` (900×580), rendues depuis les fenêtres WPF natives actuelles et inspectées, avec données d’exemple. Quatre scénarios temporaires réussis : les fenêtres se masquent sans réduction hôte, restaurent le même handle, ferment sur état terminal et détruisent leurs handles après erreur/fermeture ; les documents empruntés restent intacts. Les deux tests temporaires ont été supprimés ; les PNG demandés restent conservés. Le bureau, les menus complets et les polices historiques ne sont pas terminés.

Bureau interne après les références utilisateur : quatre scénarios temporaires réussis, dont ouverture unique, double-clic, réduction/restauration, agrandissement à l’intérieur du bureau et libération après erreur. Rendu du gestionnaire et des groupes inspecté ; fond blanc du groupe corrigé dans le nouveau ScrollViewer, puis rendu à nouveau et vérifié. Scénario de capture et PNG temporaires supprimés ; trois scénarios autonomes conservés. Les icônes sont provisoires et les polices ne sont pas historiques : aucune fidélité exacte n’est revendiquée. `fontTools` manque pour leur conversion ; demande d’installation envoyée, travail sur les autres éléments poursuivi.

Polices : fontTools 4.68.0 installé avec l’autorisation utilisateur. Trois faces incorporées, 672 glyphes vérifiés contre les masques sources, métriques et familles WPF vérifiées. La perte de traits venait du mode Display ajouté au thème ; la comparaison Ideal/Display/géométrie l’a isolée, puis Ideal a corrigé le rendu du bureau. Capture corrigée inspectée dans `artifacts/themes-rebuild/current/windows31-desktop.png`. Le test de diagnostic et son image ont été supprimés ; le contrôle autonome des ressources incorporées est conservé. Les icônes génériques restent à remplacer à la demande explicite de l’utilisateur.

Icônes originales Windows 3.1 : dix ressources incorporées, neuf images différentes pour les outils et la véritable icône PROGMAN pour les groupes. Quatre contrôles ciblés réussis ; capture du bureau à 1280×800 inspectée dans `artifacts/themes-rebuild/current/windows31-desktop.png`. Test de capture, planche de sélection et cache de l’installation supprimés après conservation des ressources retenues et de leur manifeste.

Interactions du cadre : quatorze contrôles ciblés réussis. Menu système interne, commandes dépendantes de l’état, géométries ancrées sur les bords opposés et prévisualisation sans modification du document vérifiés. Validation/annulation et retrait des surfaces à la fermeture contrôlés dans des arbres WPF autonomes ; la manipulation avec capture souris en fenêtre réelle reste à vérifier avant livraison.

Contrôle après intégration des ressources originales et des interactions : 202 tests ciblés réussis (thèmes, lecture, navigation, préférences), aucun test temporaire conservé et `git diff --check` réussi. Aucun build officiel livré ni commit créé ; OS reste en cours de reconstruction et n’est pas enregistré dans le sélecteur.

Raccordement natif commun : trois scénarios temporaires réussis avec de vrais HWND synthétiques. Régions initiales présentes/absentes restaurées, exclusions et contour creux vérifiés avec le DPI réel, changement de couche et masquage contrôlés, clic natif observé sans consommation. Le service laisse les HWND empruntés vivants ; les propriétaires de test ont fermé leurs fenêtres et confirmé leur destruction. Test temporaire supprimé, aucun autre artefact de fenêtre créé. Ces contrôles ne constituent pas encore une validation visuelle de tous les moteurs GPU dans le thème intégré.

Écriture Windows 3.1 : dix contrôles ciblés réussis (raccordement commun et nouvelle vue), dont menus/actions, saisie reliée au modèle, exclusivités existantes, choix indépendants et détachement. Le conflit Options/espace de noms a été corrigé dans le nouveau formulaire avant validation. Captures françaises inspectées à 740×360 et 620×530 dans `artifacts/themes-rebuild/current/{write,write-options}.png` ; aucun débordement constaté sur ces tailles. Test de rendu temporaire supprimé. L’intégration au bureau et les dialogues globaux restent à faire avant de proposer OS.

Synchronisation native : scénario temporaire réussi avec le service construit avant le HWND, déplacement réel traité automatiquement et destruction pendant une mise à jour en attente. Coordonnées écran/placement natif concordants, région restaurée et handles des fenêtres détruits ; test temporaire supprimé. Le lot écriture et raccordement commun totalise 206 contrôles de thèmes, lecture, navigation et préférences réussis, en plus des quatre scénarios natifs temporaires. Aucun thème supplémentaire enregistré ni commit créé.

Conversion : 35 contrôles ciblés réussis (nouvelle vue, contrôleur commun, chemin de sélection Base, lecture/écriture/options et raccordement). Les premiers rendus ont révélé le mauvais libellé et le faux répertoire de certains caractères, puis le bouton hors vue à 600×480 ; ces défauts ont été corrigés et les captures régénérées et inspectées. Le contrôle de rendu confirme aussi la commande dans les limites de la vue. Traduction de Read.Extension revue dans toutes les langues après Argos, glyphes/métriques conservés et test temporaire supprimé. Aucun thème déclaré fini.

Validation du lot conversion, champs partagés et typographie : 265 contrôles réussis (thèmes, lecture, navigation, conversion Base, préférences et localisation). `git diff --check` réussi. Les captures finales de conversion sont conservées ; aucun scénario temporaire ajouté dans ce lot ne subsiste. OS, Orbit, Matrix et Atlas restent à terminer intégralement ; aucune publication ni aucun commit.

Consultation des informations Windows 3.1 : quatre contrôles réussis, dont le rendu à 540×440 inspecté dans `artifacts/themes-rebuild/current/inspector.png`. Champs et unités complets, choix de surface et détachement vérifiés ; test de capture temporaire supprimé. Le raccordement aux représentations graphiques reste en cours.

Raccordement des cinq représentations média : 89 contrôles ciblés réussis (visualisation existante, emprunt des feuilles et nouveaux événements). Surfaces Skia et commandes d’origine identifiées sans copie des panneaux, parents restaurés après emprunt, sélection séquentielle routée vers ses adresses existantes. Changement de viewport et déchargement annulent les opérations de zoom possédées. La nouvelle fenêtre de visualisation et son intégration restent à construire ; aucun thème déclaré fini, aucun commit ni build officiel livré.

Fenêtre de visualisation Windows 3.1 : 352 contrôles réussis (thèmes, visualisation, lecture, conversion Base, navigation, préférences et localisation). Les cinq représentations changent de surface avec restitution des parents ; mode géométrique et largeur simple face contrôlés. Rendus réels à 1000×660 et 640×480 inspectés dans `artifacts/themes-rebuild/current/{visualization,visualization-narrow}.png`, après correction de l’espace réservé par les options masquées. Le rendu hors écran sans chargement ne déclenchait pas Skia : les captures finales utilisent une fenêtre réelle, fermée dans finally avec destruction du HWND confirmée. Scénario temporaire supprimé et diff vérifié. Les dialogues de classification, l’intégration au bureau et les autres écrans/éditions restent à terminer ; aucune validation globale de thème ni livraison officielle dans ce lot.

Classification Windows 3.1 : 49 contrôles ciblés réussis, dont les choix machine/format/protection, les événements d’origine et le détachement. La capture a révélé le sélecteur de template absent dans le gabarit ComboBox ; corrigé et vérifié par un contrôle autonome du texte rendu. Classification.Format revue dans les 30 catalogues après Argos, notamment les confusions avec disque musical et les mots restés anglais. Formulaire corrigé inspecté dans `artifacts/themes-rebuild/current/classification.png`, scénario temporaire supprimé. Le raccordement des dialogues au bureau et les autres interfaces restent à réaliser ; aucun commit ni livraison officielle.

Dialogues internes Windows 3.1 : 361 contrôles de thèmes, visualisation, lecture, conversion, navigation, préférences et localisation réussis. Options modales et inspecteur non modal, activation sans duplication, restitution de la disponibilité, fermeture du propriétaire et nettoyage après exceptions vérifiés. Les dialogues n’exposent plus les commandes de réduction/agrandissement des applications. Une fermeture en arrière-plan conserve l’application active. Le raccordement aux fabriques des fonctions et les autres écrans/éditions restent à réaliser ; aucun nouveau thème déclaré livré, aucun commit.

Applications média raccordées au bureau : onze contrôles ciblés réussis pour les fabriques, dialogues et bureau ; après ajout du bouton Fermer, quatre contrôles incluant le rendu intégré réussis. Lecture, écriture, conversion et visualisation utilisent leurs vues indépendantes ; options, classification et inspecteur restent internes. Les menus et modèles sont vérifiés jusqu’au bureau ; fermeture puis rappel d’un ancien menu ne recréent pas l’application. Capture de progression à 1280×800 inspectée dans `artifacts/themes-rebuild/current/windows31-media-desktop.png`. Scénario temporaire supprimé, diff vérifié. La disposition d’acquisition refusée, l’explorateur, les outils, l’émulation, les réglages et les autres éditions/thèmes restent à terminer ; OS n’est pas encore proposé comme livré et aucun commit n’a été créé.

Acquisition Windows 3.1 reprise : 33 contrôles ciblés réussis, dont le rendu temporaire et les contrôles existants de lecture. Menus Fichier/Options, onglets brut/format connu, révolutions accessibles uniquement dans le brut, destination, profil et historique complet reconstruits. Choix d’onglet bidirectionnel, indisponibilité source, modèle des révolutions, consultation et fermeture vérifiés. Rendus à 850×560 et 600×430 inspectés dans `artifacts/themes-rebuild/current/{acquisition,acquisition-known,acquisition-narrow}.png` ; le contraste des trois colonnes de la ligne sélectionnée a été corrigé et contrôlé. Bouton d’exécution maintenu dans les limites, scénario temporaire supprimé. Explorer, outils, émulation, réglages, autres éditions et autres thèmes restent à réaliser avant une livraison complète.

Lot palettes et sélecteur : 386 contrôles groupés réussis. Trois captures de bureau supplémentaires inspectées à 1280×800 dans artifacts/themes-review/screens/OS/Windows31. Convertisseurs temporaires, cache Python, scénario de capture et artifacts/themes-rebuild supprimés ; aucune publication de OS incomplet, aucun commit et pas encore de build officiel.

Explorateur indépendant : validation groupée de 291 contrôles réussie après correction des textes générés, y compris navigation, propriétés vivantes, actions, palettes, préférences et visualisation. Captures Explorer, ExplorerInformation et ExplorerNarrow examinées ; scénario temporaire supprimé. Les pictogrammes de fichiers/dossiers historiques, les autres applications et dialogues OS, puis l’intégration complète de Windows 3.1 restent à terminer avant le build Debug tous modules.

Pictogrammes : 218 contrôles groupés réussis. FileEntryIcon conservé, fournisseur d’habillage local à Windows 3.1 vérifié contre la ressource de 1992, pixels et transparence contrôlés, retour aux images Base après détachement vérifié. Captures ExplorerIcons Default/Bordeaux inspectées. Archive, script d’extraction, bitmap source temporaire et scénario de capture supprimés ; aucun nouvel outil conservé dans Tools.

Outils Windows 3.1 : 128 contrôles groupés thèmes/maintenance réussis, puis scénario de capture réussi. Effacer/Nettoyer examinés à 660×420 dans themes-review/screens/OS/Windows31/Default ; textes et commandes visibles, sources Base conservées. Scénario temporaire supprimé et diff vérifié. Émulation et réglages restent à reconstruire ; aucun commit ni build officiel livré.

Gestionnaire des machines Windows 3.1 : 126 contrôles groupés thèmes/émulation réussis, dont le scénario temporaire de capture. Configurations des modules existants, réactivation sans second cœur, actualisation, disponibilité et détachement contrôlés. Capture EmulationManager à 700×400 examinée après correction des ressources simulées ; scénario temporaire supprimé. Les fenêtres de rendu des machines et leurs commandes restent à reconstruire, ainsi que les réglages et l’intégration OS ; aucun commit ni build officiel livré.

Fondation de présentation des machines : 125 contrôles groupés thèmes/émulation réussis après séparation de la vue du rendu et des entrées. Les scénarios existants de rendu masqué et de tape passent ; disposition Base conservée. Le nettoyage libère entrées, vidéo, cœur et présentation avec finally. Aucun scénario temporaire Windows31 restant ; diff vérifié. Les vues propres aux machines Windows 3.1, l’échange de thème des sessions ouvertes et l’intégration restent à réaliser.

Présentation de machine Windows 3.1 : 129 contrôles groupés réussis, capture temporaire incluse. Menus reliés aux commandes existantes et à leurs disponibilités/états, supports et tape issus des modèles existants, remplacement de surface et chargement shader vérifiés. Création asynchrone : un échec de présentation attend la libération effective du cœur et retire la session. Capture Machine.png à 820×540 examinée avec surface de démonstration explicitement identifiée, sans cœur exécuté ; scénario temporaire supprimé. Les fenêtres des machines ne sont pas encore raccordées au bureau OS et le changement de présentation des sessions ouvertes reste à faire. Base reste la présentation par défaut. Aucun commit ni build officiel livré.

Transfert de présentation : 131 contrôles groupés thèmes/émulation réussis. Même surface et même abonnement vidéo conservés aller/retour ; refus de surface rétabli dans la vue précédente. Les commandes déplacées restent fonctionnelles après libération de leur ancien habillage. Les événements de focus suivent le nouvel hôte sans interrogation de matériel dans le scénario autonome. Diff vérifié. Le raccordement des fenêtres au bureau, sa validation avec les sessions réelles et les dialogues restent à réaliser ; aucun commit ni build officiel livré.

Hébergement des machines et notifications : 135 contrôles groupés thèmes/émulation réussis. Tests sur le contrôleur complet : même cœur, même surface et même abonnement vidéo après transfert/restitution, hôte occupé préservé, fermeture détachée sans redémarrage. Deux machines sélectionnées et fermées par les actions source, fermeture générale et retrait des onglets contrôlés. Diff vérifié. Le raccordement des cadres au bureau OS reste à faire ; le suivi du handle natif est inscrit avant ce raccordement pour les surfaces pouvant reconstruire leur HWND. Aucun commit ni build officiel livré.

Cadres de machines raccordés : 137 contrôles groupés réussis, puis capture intégrée réussie à 1280×800 dans DesktopMachines.png. Deux contrôleurs simulés présentés en fenêtres indépendantes, restauration après réduction, agrandissement, fermeture et retrait du thème avec restitution de la session restante vérifiés. Handle de focus lu auprès du présentateur courant. Capture examinée : groupe des opérations mal nommé Outils et titre de groupe encore actif quand le gestionnaire est inactif ; corrections inscrites à la suite. Scénario temporaire supprimé ; aucune preuve de fonctionnement de tous les cœurs GPU réels n’est revendiquée. Aucun commit ni build officiel livré.

Préférences intégrées au bureau : 151 contrôles de thèmes et de navigation existants réussis, puis trois scénarios permanents pour les sept pages, les accès Système et la modalité globale. Le scénario temporaire de cycle de vie a vérifié une vraie fenêtre fonctionnelle non affichée : la sauvegarde asynchrone est attendue avant fermeture, une seule écriture et aucune surface native source conservée. Capture IntegratedPreferences.png examinée ; scénario temporaire supprimé. Les autres dialogues et la validation des sessions réelles restent à réaliser ; aucun thème déclaré terminé ni build officiel livré.

- [x] Normaliser docs/tasks/application-themes.md en UTF-8 : corriger les lignes ajoutees avec le mauvais encodage Windows sans changer leur contenu.

Dialogues métier intégrés : validation groupée de 190 contrôles réussie (thèmes, conversion, navigation et préférences), dont deux scénarios temporaires sur les fenêtres réelles. Nom obligatoire sans boîte native, décisions lecture/matériel, choix individuels et communs de conversion, annulation et imbrication ont été vérifiés ; propriétaires fonctionnels séparés des HWND, fermeture et destruction des handles contrôlées. Cinq captures examinées dans themes-review/screens/OS/Windows31/Default ; taille Profil/Conflit réduite et titre du gestionnaire inactivé après inspection. Scénario temporaire supprimé ; contrôle de diff réussi. Les dialogues d’émulation, les autres fenêtres et les autres thèmes restent à réaliser, sans livraison officielle ni commit.

Préférences d’émulation Windows31 : 181 contrôles groupés réussis, puis six contrôles ciblés réussis après correction des contrastes des éléments imbriqués. Dossiers reliés, filtres de configurations et édition, capture/effacement/restauration des raccourcis, annulation à la libération, tableau réseau non sélectionnable et notifications du registre vérifiés. Chargement initial partagé et attente de sa fin contrôlés sans modules externes. Vraie fenêtre fonctionnelle non affichée : sauvegarde asynchrone attendue et fermeture effective vérifiées. Quatre captures examinées à 1280×800 dans themes-review/screens/OS/Windows31/Default ; scénario temporaire supprimé. Les pictogrammes de périphériques, la transition des fenêtres déjà ouvertes, les options des modules et les autres thèmes restent à terminer ; aucun commit ni build officiel livré.

Options des modules, première reconstruction indépendante : 164 contrôles de thèmes/émulation réussis, puis sept contrôles des feuilles et de la fenêtre réussis. Texte, nombre/IPv4 contraints, informations, choix, radios, cases, curseurs avec unités et automatique, chemins et boutons utilisent les points fonctionnels existants sans déplacement des conteneurs ; modification, création de configuration, catégories réelles, lecture seule et détachement vérifiés avec stockage en mémoire. Trois captures à 1280×800 et 800×600 examinées dans themes-review/screens/OS/Windows31/Default ; scénario temporaire supprimé. Les panneaux spécialisés sont détaillés et restent à reconstruire avant intégration du module au bureau ; aucune validation de tous les modules réels, aucun thème déclaré fini, aucun commit ni build officiel livré.

Panneaux cœurs et sélection ROM Windows 3.1 : 18 contrôles ciblés réussis puis 169 contrôles groupés de thèmes/firmware réussis. Recherche, installation, progression, annulation, verrouillage, compatibilité, sélection et attente de fermeture vérifiés. Captures CoreManagement.png et FirmwareSelection.png inspectées dans themes-review/screens/OS/Windows31/Default ; bouton tronqué corrigé et scénario temporaire supprimé. Les autres panneaux spécialisés et leur intégration complète restent à terminer ; aucun commit ni build officiel livré.

Vidéo Windows 3.1 : 172 contrôles groupés de thèmes/firmware réussis avec le scénario de capture ; après inspection, libellés CRT corrigés à partir du catalogue existant et curseurs verticaux regroupés en colonnes. Sept contrôles ciblés réussis après ces corrections. Toutes les technologies déclarées conservent leurs points fonctionnels, les choix modifient le profil et les éditions de curseurs attendent leur fin pour sauvegarder. Trois captures inspectées (VideoDisplay, VideoCrt, VideoImage), scénario temporaire supprimé. Entrées, stockage et leurs dialogues restent à reconstruire avant intégration complète ; aucun build officiel ni commit.

Entrées Windows 3.1 : 11 contrôles ciblés réussis puis 166 contrôles groupés des thèmes réussis avec le scénario temporaire. Clavier/souris, choix des ports et types, champs conditionnels, conservation des sorties, capture et détachement vérifiés ; les dessins reprennent les ressources existantes, le suivi est arrêté à la fermeture. Captures ModuleKeyboard, ControllerPorts et ControllerPortsNarrow inspectées ; hauteur de table réduite et boutons vérifiés dans le viewport à 800×600. Scénario temporaire supprimé. Stockage et dialogues spécialisés restent à reconstruire, puis intégration de tous les modules et sessions réelles ; aucun build officiel ni commit.

Stockage Windows 3.1 : huit contrôles ciblés réussis puis 168 contrôles des thèmes réussis avec la capture. La liste conserve les résumés déjà calculés, la sélection par emplacement et les capacités fournies par le module ; les périphériques permanents restent protégés et l’application de la configuration conserve les supports. Captures ModuleStorage et ModuleStorageNarrow inspectées, y compris liste longue défilante et boutons visibles ; scénario temporaire supprimé. Les neuf dialogues spécialisés et leur intégration dans le bureau restent à reconstruire. Aucun commit ni build officiel.

Options du cœur et ajout de supports Windows 3.1 : 171 contrôles des thèmes réussis avec quatre scénarios temporaires. Valeurs texte/choix, types disponibles, résultat modal, titre vivant et fermeture des fenêtres fonctionnelles non affichées vérifiés ; le pied de dialogue a aussi été vérifié dans une vraie fenêtre native invisible et ses handles détruits. Captures CoreOptions et AddStorageDevice inspectées. Titre ambigu corrigé via Argos et revue dans les 30 catalogues, une seule clé modifiée par fichier ; 41 contrôles dialogues/localisation réussis après correction. Scénario temporaire supprimé. Les autres dialogues spécialisés restent à reconstruire ; aucun build officiel, commit ni thème déclaré terminé.


Messages Windows 3.1 : 324 contrôles groupés de thèmes, navigation et localisation réussis. Les sept jeux de réponses du framework, le choix par défaut, Échap, le repli natif et deux messages de même type imbriqués sont vérifiés. Quatre captures historiques inspectées dans `artifacts/themes-review/screens/OS/Windows31/Default/Message*.png` ; défilement superflu des messages courts corrigé par mesure réelle après application des styles. Test temporaire supprimé. Les sélecteurs de fichiers/dossiers et la migration des appels directs restent à faire ; aucun commit ni build officiel.


Sélecteurs Windows 3.1 : 324 contrôles groupés thèmes/navigation/localisation réussis, puis quatre scénarios ciblés réussis après la mesure finale et le suivi de fermeture. Ouverture/enregistrement/dossier, filtres Win32, erreurs, noms sans extension, extensions multiples et DefaultExt, refus/accord d’écrasement, lecture seule, annulation pendant le parcours et repli natif sur les objets d’origine vérifiés. Trois captures FileSelection inspectées dans themes-review/screens/OS/Windows31/Default ; colonne vide du dossier supprimée. Scénario et données temporaires supprimés. La migration des appels directs commence ensuite ; pas de livraison globale ni commit.


Migration des 22 fichiers d’appels directs : 334 contrôles groupés de thèmes, navigation, localisation, options, émulation et HDD réussis après ajout de l’import Window requis. Paramètres et valeurs par défaut des confirmations conservés ; insertion/enregistrement tape et cartes mémoire utilisent aussi le relais des dialogues spécialisés. Les fenêtres complètes de modules et les autres fenêtres globales restent à intégrer ; aucun commit ni build officiel.


Fenêtre complète des modules raccordée au bureau Windows 3.1 : 338 contrôles groupés thèmes/navigation/localisation/options/émulation/HDD réussis, dont trois scénarios temporaires sur la fenêtre fonctionnelle. Configuration initiale, chargement partagé, dossiers fonctionnels et exclusion des dossiers par défaut vérifiés. Fermeture pendant le chargement attendue avec ou sans ContentRendered ; contrôleurs et champs libérés, aucun HWND source créé. Capture IntegratedModuleSettings inspectée avec métadonnées synthétiques, scénario temporaire supprimé. La validation de tous les modules réels est inscrite séparément et reste à faire, ainsi que les fenêtres globales et autres thèmes ; pas de build officiel ni commit.


Parcours réel des 24 modules : 213 machines et 1 830 onglets de réglages construits sans champs déclarés manquants ni requête réseau ni lancement de cœur. Nintendo : repli matériel sans adaptateur corrigé, 96 tests de catalogue réussis ; son assembly recompilé a été contrôlé sans remplacer le paquet du build utilisateur. Captures représentatives dans themes-review/screens/OS/Windows31/Modules, scénario et dossiers temporaires supprimés. Inspection complémentaire : résumé total RAM natif absent de la nouvelle vue, raccordement ajouté à la suite avant de déclarer la fenêtre complète terminée. Aucun commit ni build officiel.


Résumé matériel : total RAM raccordé au calcul existant, choix radio suivis dans Base et dans Windows 3.1, liaison libérée au changement de page. Quatre tests autonomes de réglages réussis, puis 337 contrôles groupés avec le contrôle réel Acorn. Capture Modules/acorn/Ram.png inspectée : passage à 32 KiB et total visible correspondant ; scénario et espace de travail temporaire supprimés. Les fenêtres globales (erreurs détaillées, À propos, journaux, mises à jour, outils et migration) restent à reconstruire/raccorder avant la livraison Windows 3.1. Aucun commit ni build officiel.


Erreurs détaillées et À propos Windows 3.1 : 337 contrôles groupés réussis. Message, toutes les paires détail/valeur, types de supports requis, version de l’application et noms/versions des composants conservés ; fermeture par bouton et Échap vérifiée sans HWND source. Captures DetailedError et About inspectées ; erreur dimensionnée d’après la mesure des contrôles, liste des supports titrée, Common.MediaKind.Tape corrigée dans toutes les cultures après Argos et revue. Scénario temporaire supprimé et diff vérifié. Restent journaux, mises à jour, outils avancés, migration et vérification complète des accès avant la livraison Windows 3.1 ; autres déclinaisons et thèmes toujours dans le périmètre. Aucun commit ni build officiel.


Outils avancés Windows 3.1 : 211 tests groupés réussis, dont les neuf verbes, liaisons des paramètres, validation, sortie depuis un thread de fond et fermeture après annulation du runner. Captures Tool-align, Tool-rpm et Tool-info inspectées ; scénario temporaire supprimé. Aucun commit ni build officiel.

Migration et avertissements Windows 3.1 : dix contrôles ciblés réussis, puis 42 contrôles thèmes/localisation réussis ; lecture d’une véritable image DFS vide, initialisation partagée, choix de destination, liste des pertes et intégralité des avertissements vérifiés. Deux captures inspectées après mise en page ; en-têtes historiques et traduction de la description corrigés dans toutes les langues. Scénario et répertoire temporaires supprimés. Aucun commit ni build officiel.

Mises à jour Windows 3.1 : 347 tests groupés réussis, dont recherche annulée et attendue, quatre catégories, versions, catalogue et URL manuelle. Quatre captures inspectées ; bouton du catalogue conservé et mise en page attendue après modification des données du scénario. Trois contrôles ciblés réussis après la dernière correction. Scénario temporaire supprimé ; aucun réseau ni installateur réel lancé. Aucun commit ni build officiel.

Contrôle final des fenêtres : 26 types secondaires Base disposent de leur relais Windows 3.1 ; MainWindow reste le propriétaire natif du bureau. Les menus Options, diagnostics, matériel, journaux, mises à jour, modules et aide reprennent les actions et états existants, y compris les modifications de la liste des modules. 28 contrôles de bureau/menus/palettes réussis et 23 aperçus réels de 1280×800 générés. Le contrôle des 24 modules et 213 machines concerne leurs réglages ; aucun nouveau démarrage de cœur ni validation de tous les GPU n’est affirmé.

Windows 3.1 sélectionnable dans OS : 369 contrôles groupés réussis avant adaptation de l’ancien garde-fou limitant le catalogue à Base/Studio ; ce garde-fou est remplacé par la vérification explicite de Base, Studio et OS avec sa déclinaison Windows 3.1. Les deux contrôles de sélection et le chargement/retour de la palette OS ont été relancés séparément. Aperçus des palettes inspectés, scénario temporaire supprimé avant build officiel.

Build officiel Debug réussi avec `scripts/local-building.cmd --building=debug --modules=A` (code de sortie 0). Exécutable vérifié : `build/Debug/Retro Media Lab/retromedialab.exe`. Les 24 manifestes et leurs 24 assemblies de modules sont présents et correspondent exactement aux identifiants des modules source. Windows 3.1 se choisit dans OS avec ses 23 palettes ; Base et Studio restent proposés. Aucun commit créé. Les autres déclinaisons OS, puis les refontes Orbit/Matrix/Atlas, restent à réaliser dans l’objectif global.

Premier cadre Windows 95 : fenêtre native réelle contrôlée, réduction/agrandissement/restauration exécutés, refus puis accord de fermeture vérifiés. Le HWND est détruit et le contenu emprunté reste non disposé et détaché. Capture Window.png inspectée dans themes-review/screens/OS/Windows95 ; scénario temporaire supprimé. Le cadre ne constitue pas une déclinaison terminée : ressources d’époque, bureau, lancement et vues restent à réaliser, sans ajout au sélecteur ni remplacement du build Windows 3.1 livré.

Bureau Windows 95 : lanceurs au clavier et détachement contrôlés, suivi des titres et absence de destruction des fenêtres empruntées vérifiés. Cinq contrôles groupés réussis puis trois après correction du contraste et de la capture. Capture Desktop.png inspectée avec l’image BBC existante ; scénario temporaire supprimé. Bureau encore indépendant, sans ajout de Windows 95 au catalogue ni remplacement du build Windows 3.1 livré.

Menu Démarrer Windows 95 : modèle propre gris/bleu chargé, sous-menus et action existante vérifiés sans fenêtre native ; deux contrôles ciblés réussis après la dernière modification. Aucun commit, aucune publication ni remplacement du build officiel. Les vues fonctionnelles et les ressources historiques Windows 95 restent à intégrer avant enregistrement de la déclinaison.

Lecture Windows 95 : trois contrôles groupés réussis avec le bureau ; édition aller-retour, séparation Brut/Format, commande d’exécution et libération testées. Capture Read.png à 720×480 inspectée ; fenêtre de présentation non affichée et aucun HWND créé, scénario temporaire supprimé. Les commandes avancées restent à raccorder à leur dialogue Windows 95 avant intégration de la déclinaison.

Écriture Windows 95 : trois contrôles groupés réussis avec le bureau. Chemin, profil, choix de format, commandes existantes, état désactivé et détachement vérifiés. Capture Write.png à 680×390 inspectée ; scénario temporaire supprimé, aucun HWND créé. Aucun ajout au catalogue Windows 95, aucun remplacement du build Windows 3.1 et aucun commit ; restent notamment les dialogues avancés, conversion, exploration, visualisation et émulation Windows 95.

Propriétés avancées et conversion Windows 95 : 18 contrôles groupés réussis, incluant les trois consommateurs Windows 3.1 après déplacement du seul relais générique vers Theme/Common. Six scénarios Windows 95 réussis après correction des intitulés et du défilement ; contrôle ciblé supplémentaire du défilement exécuté avant fermeture de la fenêtre, puis capture de chaque onglet. Huit captures inspectées dans screens/OS/Windows95 (Conversion, ReadProperties, ReadNumbering, ReadExpert, WriteProperties, WriteExpert, ConversionProperties, ConversionExpert). Scénario temporaire supprimé ; aucune fenêtre native créée ni session d’émulation lancée. Les vues restent à intégrer au bureau avec les autres fonctions avant de proposer Windows 95 ; aucun build officiel remplacé, aucun commit.

Explorateur Windows 95 : quatre contrôles groupés réussis, puis contrôle ciblé de la zone des cases plus/moins après sa correction. Navigation et développement/repli au clavier, absence de case sur les feuilles, double clic gauche, colonnes redimensionnables, propriétés actualisées, menus cochés, progression et détachement vérifiés. Trois captures Explorer, ExplorerProperties et ExplorerLoading inspectées ; scénario temporaire supprimé et absence de HWND vérifiée pour chaque fenêtre de capture. Les icônes de contenu passent par le fournisseur existant ; les ressources historiques Windows 95 restent à intégrer avant enregistrement de la déclinaison. Aucun commit ni remplacement du build officiel.

Visualisation et outils Windows 95 : dix contrôles groupés réussis après correction des couleurs de sélection dans les modèles, du libellé des ComboBox et de la portée des textes générés. Les cinq représentations restituent leurs feuilles de dessin et le viewport de bande à leurs propriétaires ; carte logique et géométrie réellement dessinées avec le moteur existant dans des fenêtres invisibles hors écran, puis HWND détruits dans finally. Cinq captures inspectées (Visualization, VisualizationBlocks, VisualizationGeometry, ToolsErase, ToolsClean). Commandes des outils et arrêt/captions reliés aux points existants sans opération matérielle lancée. Deux scénarios temporaires supprimés. Restent notamment émulation, réglages, dialogues et ressources historiques avant intégration Windows 95 ; aucun commit ni remplacement du build officiel.

Présentation des machines Windows 95 : neuf contrôles réussis après correction des commandes Marche/Pause, qui suivent exclusivement l’état du contrôleur. Transfert/restitution de surface et des entrées, restauration après rejet, actions et disponibilité, trente périphériques dans la liste latérale, défilement, états tape, chargement shader et nettoyage après erreur vérifiés. Capture Emulation.png inspectée : machine arrêtée, aucun cœur lancé ; aucun démarrage Acorn ou rendu GPU réel affirmé par ce scénario. Scénario temporaire supprimé. Restent bibliothèque et réglages d’émulation, dialogues, ressources historiques et intégration du bureau Windows 95 ; aucun commit ni remplacement du build officiel.

Isolation finale des boutons Windows 95 : deux contrôles ciblés réussis pour le style nommé, les métriques classiques et la conservation des couleurs des pistes ; le déclencheur booléen de Marche/Pause a été corrigé puis revérifié séparément. Captures Emulation et VisualizationGeometry réactualisées et inspectées ; le style dérivé ne reprend plus les tailles/couleurs de Base. Scénario temporaire supprimé, HWND des captures natives détruits. Aucun commit ni remplacement du build officiel ; le travail global sur les thèmes reste en cours.

Windows 95 intégré : 214 contrôles groupés thèmes/localisation réussis, dont un scénario temporaire à deux contrôleurs conservant les propriétaires des sessions. Dialogues natifs, menus imbriqués, icônes Shell32 historiques, défilement de cent lanceurs et restitution des sources vérifiés ; captures dans themes-review/screens/OS/Windows95. Les scénarios temporaires ont été supprimés. Aucun démarrage de cœur réel ni validation de tous les GPU affirmé par ces scénarios. Aucun commit.

Build officiel Debug avec --modules=A terminé sans erreur (code 0) ; exécutable et 24 manifestes/DLL vérifiés. La revue complémentaire a identifié la garde Fermer des confirmations sans annulation, inscrite à la suite avant de livrer ce lot.

Livraison Windows 95 : dernier build officiel --modules=A réussi (code 0), retromedialab.exe et les 24 manifestes/DLL contrôlés après livraison. Les 214 contrôles groupés passent ; six contrôles ciblés supplémentaires passent après ajout de la garde des confirmations. Aucun scénario TemporaryWindows95 ne reste dans les sources. OS propose Windows 3.1 et Windows 95 ; Base et Studio conservés. Aucun commit, validation utilisateur toujours requise avant commit. Les autres déclinaisons OS et Orbit/Matrix/Atlas restent dans le périmètre global.

Contrôles Luna XP : les trois dictionnaires officiels sont chargés localement ; présence des éléments de dessin PresentationFramework.Luna, police Tahoma, métriques et restitution des ressources vérifiées. Planche LunaControls.png inspectée ; scénario temporaire supprimé. Les contrôles seuls ne constituent pas la déclinaison XP et ne sont pas enregistrés au catalogue. Aucun changement du build Windows 95 livré, aucun commit.

Découpes XP : les 66 ressources et chaque état ont été chargés, leurs dimensions contrôlées, images figées et réutilisées. La planche FrameSlices.png a été rendue et inspectée en états actifs/inactifs ; aucun HWND créé par ce contrôle. Scénario temporaire supprimé. Les ressources du média et leurs paramètres ont été conservés en PNG/JSON utiles ; les fichiers de style temporaires ont été supprimés. Le cadre natif, le bureau et les vues XP ne sont pas encore livrés.


Orbit : 176 contrôles existants de thèmes réussis, neuf scénarios temporaires de navigation/conversion et parcours des 24 modules réussis ; six scénarios supplémentaires de préférences, confirmations et transfert de deux sessions réussis. Captures dans artifacts/themes-review/screens/Orbit. Les fenêtres natives sont détruites et les sources empruntées restituées. Les deux scénarios temporaires finaux ont été supprimés. Aucun démarrage de cœur réel ni rendu GPU réel affirmé. Aperçus clair/sombre issus des captures de Lecture. Build officiel tous modules en cours ; aucun commit.

Contrôle final groupé Orbit : 212 tests existants de thèmes et localisation réussis après nettoyage des sources temporaires. Les six contrôles natifs/sessions ont également été relancés et réussissent. Aucun test temporaire Orbit ne reste dans les sources.

Livraison Orbit : scripts/local-building.cmd --building=debug --modules=A terminé avec code 0. Exécutable build/Debug/Retro Media Lab/retromedialab.exe présent ; 24 manifestes et leurs DLL vérifiés. lib/retromedialab.app.dll correspond exactement à la DLL compilée win-x64. 212 tests groupés réussis, plus scénarios temporaires décrits ci-dessus et supprimés. Aucun commit ; validation utilisateur attendue. Travail sur les thèmes arrêté après Orbit selon la demande explicite.

- [x] Enregistrer Orbit après accord utilisateur du 1 novembre 2026.
  - [x] Modifier ce suivi : consigner la validation et préparer dans Git les fichiers `src/RetroMediaLab.App/Theme/Orbit/`, leur enregistrement `ThemeCatalog.cs`, `AppThemeDesign.cs`, les seules ressources Orbit de `Resources/*/Options.resx` et leur attente dans `ThemeEditionSelectionTests.cs` ; conserver les travaux Windows XP hors du commit.
  - [x] Enregistrer les fichiers Orbit et les documents `docs/design/themes-rebuild.md` / `docs/tasks/application-themes.md` dans un commit dédié, après contrôle du contenu indexé.

- [x] Enregistrer tous les travaux restants sur demande explicite de l’utilisateur.
  - [x] Ajouter dans Git tous les fichiers modifiés et nouveaux, notamment `src/RetroMediaLab.App/Theme/OS/Editions/WindowsXP/`, les raccordements, ressources et tests associés, puis créer le commit sans exclure de travaux.
