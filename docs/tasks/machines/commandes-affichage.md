# Commandes et affichage des machines

- [ ] Terminer les commandes demandées avant la livraison groupée.
  - [x] Paramètres et raccourcis.
    - [x] Modifier `src/RetroMediaLab.App/Contracts/Machine/MachineControllerOptions.cs`, `MachineCommandActions.cs` et `Theme/Base/Views/Controls/Emulation/Machine/EmulationSectionMachineFunctions.cs` : fournir une action ouvrant les paramètres existants avec la configuration de la machine sélectionnée, et fermer le dialogue dans finally.
    - [x] Modifier `src/RetroMediaLab.Infrastructure/Settings/Emulation/EmulationShortcutDefaults.cs`, `src/RetroMediaLab.App/Theme/Base/Views/Controls/Emulation/Options/EmulationPreferencesSectionSettingsFunctions.cs` et `Theme/Base/Views/Controls/Emulation/Machine/MachineController.cs` : ajouter un raccourci configurable F4 vers ces paramètres et retirer l'ancien dialogue du bouton.
    - [x] Modifier `src/RetroMediaLab.App/Resources/*/Emulation/EmulationMachine.resx` : ajouter le libellé des paramètres de la machine dans toutes les langues avec Argos.
    - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Emulation/Machine/MachineCommandBar.cs` et `Theme/Base/Functions/Emulation/Shortcuts/EmulationShortcutViewFunctions.cs` : retirer les groupes de texte et afficher les raccourcis configurés dans les infobulles des boutons.
  - [x] Indicateurs stables.
    - [x] Modifier `src/RetroMediaLab.App/Constants/Machine/MachinePresentationConstants.cs` et `Theme/Base/Views/Controls/Emulation/Machine/MachineView.cs` : afficher les mesures dans un cadre fixe semi-transparent en haut à droite sous la barre, sans déplacer les commandes lors des changements de valeur.
  - [ ] Vérifications finales groupées.
    - [x] Modifier les scénarios d'interface existants dans `tests/RetroMediaLab.Tests/Interface/EmulationViews/` pour vérifier les raccourcis personnalisés, les commandes et le cadre de mesures.
    - [ ] Modifier ce document avec les résultats des vérifications et du build Debug tous modules, uniquement après la réalisation de tous les correctifs audio, médias, diagnostics et commandes.

Résultats Debug : 65 tests locaux réussis, dont les commandes, les raccourcis personnalisés, le cadre fixe, la console et les diagnostics mémoire. Les 17 tests session/mélangeur réussissent également. Le raccourci des paramètres est F4 par défaut et reste configurable. Aucune session RML n'est ouverte avant livraison.

Livraison Debug All réussie, 24 modules vérifiés. Dans RML, le bouton paramètres et F4 ouvrent les réglages de la configuration Model A ; le cadre des mesures reste sous la barre et les libellés longs sont retirés. Captures conservées dans le dossier de preuves Model A, hors Git. L'utilisateur refuse F4 et reprend la main pour discuter des touches : cette affectation n'est donc pas validée. Aucune nouvelle affectation n'est décidée ni appliquée.
