# Diagnostics de configuration et notifications de console

- [ ] Afficher la cause réelle et ne plus ouvrir la console à la réception d'un journal.
  - [x] Diagnostic de ROM mémoire Acorn.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Constants/MemoryExtensionConstants.cs` et `Functions/MemoryExtensionFunctions.cs` : remplacer le diagnostic de support incompatible par un diagnostic de ROM incompatible précisant le nom, la banque et la carte attendue, sans modifier la configuration ni les contraintes matérielles.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Common/Machines/Common/Functions/RuntimeFunctions.cs` : conserver ce diagnostic localisé avec le code public de firmware et la sévérité avertissement pour les prérequis de configuration ; conserver les erreurs d'exécution comme erreurs.
    - [x] Modifier les catalogues `src/RetroMediaLab.Emulation.Acorn/Resources/*/Emulation.resx` avec le nouveau diagnostic, traduit dans toutes les langues par `scripts/tools/translate-resx-argos.py` ; conserver les paramètres et les noms matériels invariants.
  - [x] Console fermée et pastille de messages non lus.
    - [x] Modifier `src/RetroMediaLab.App/Services/Terminal/TerminalPanelController.cs` : conserver le panneau fermé, mémoriser la sévérité non lue (erreur prioritaire sur avertissement), la notifier et l'acquitter à l'ouverture volontaire.
    - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Shell/ApplicationStatusBar.xaml` et `ApplicationStatusBar.xaml.cs` : pastille rouge ou orange sur le bouton Console, sans sélectionner ni ouvrir le panneau.
    - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Windows/Shell/MainWindow.ComponentConnections.cs` et `MainWindow.xaml.cs` pour raccorder la notification à la barre commune utilisée par les thèmes, après création du contrôleur de console.
    - [x] Modifier `src/RetroMediaLab.App/Presenters/Common/ControlErrorPresenter.cs` pour conserver le diagnostic localisé précis et journaliser les avertissements dans le journal dédié.
  - [ ] Vérifications groupées et livraison après la fin de l'essai utilisateur.
    - [x] Modifier `tests/RetroMediaLab.Tests/Interface/EmulationViews/EmulationInteractionScenarios.cs` : console fermée, couleur/priorité des notifications, acquittement, maintien des messages et détachement.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuMemoryExtensionTests.cs` : diagnostic, banque et sévérité de la ROM incorrecte, sans dépendance à un fichier ROM externe.
    - [ ] Modifier ce document avec les résultats réels des tests et de la livraison Debug tous modules ; conserver les captures hors Git.

Vérifications groupées Debug : 65 tests locaux réussis, avec la cause ROM précise conservée comme avertissement et la console restant fermée ; priorité rouge sur orange et acquittement vérifiés. Les clés sont présentes dans les 30 catalogues Acorn et les paramètres dans les 30 catalogues App.

Le correctif est inclus dans la livraison Debug All réussie avec 24 modules. Le diagnostic ROM et la priorité/acquittement des pastilles sont vérifiés par les tests ; la configuration réelle du dernier essai démarre et n'a pas été volontairement rendue invalide pour créer une capture d'avertissement.
