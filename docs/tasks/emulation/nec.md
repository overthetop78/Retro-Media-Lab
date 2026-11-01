# Travail restant : nec


- [ ] Options NEC complètes, propres à chaque machine et au cœur sélectionné
    - [ ] Corriger le blocage de la fenêtre NEC à l'ouverture
      - [ ] Modifier `src/RetroMediaLab.App/Views/Windows/EmulationModuleOptions/EmulationModuleOptionsWindow.xaml.cs` ou `src/RetroMediaLab.App/Views/Controls/Emulation/Options/ModuleSettings/EmulationModuleSettingsSection.Persistence.cs` : corriger la cause précise du blocage de fermeture une fois son déclencheur identifié, puis compléter le scénario de régression dans `tests/RetroMediaLab.Tests/Interface/SettingsViews/EmulationModuleSettingsNavigationScenarios.cs`.
  - [ ] Contrôleurs et pointage
    - [ ] Modifier `src/RetroMediaLab.Emulation.Nec/Common/Machines/Common/Dictionaries/ControllerCatalog.cs` : retirer la liste unique PC Engine et les sélections par nom ; prendre les contrôleurs réellement compatibles dans chaque définition de machine et limiter les choix à ceux gérés par le cœur.
    - [ ] Modifier `src/RetroMediaLab.Emulation.Nec/Common/Machines/Common/Functions/InputFunctions.Settings.cs` et `InputFunctions.Visuals.cs` : afficher les contrôleurs et commandes issus de la machine et du cœur sélectionnés.
    - [ ] Modifier `src/RetroMediaLab.Emulation.Nec/Emulators/BeetlePce/Services/ExternalHostCallbacks.Input.cs` et `src/RetroMediaLab.Emulation.Nec/Emulators/BeetlePcfx/Services/ExternalHostCallbacks.Input.cs` : vérifier que chaque choix proposé correspond à un périphérique d'entrée réellement émulé.
  - [ ] Audit et validation
    - [ ] Modifier `docs/tasks/emulation/nec-machines.md` : consigner l'audit des clés de traduction NEC et App après contrôle des catalogues localisés ; les noms de machines, d'émulateurs et `Memory Card` doivent rester dans la base commune lorsqu'ils sont invariants.
    - [ ] Modifier les fichiers C# déjà touchés sous `src/RetroMediaLab.Emulation.Nec`, `src/RetroMediaLab.Emulation` et `src/RetroMediaLab.App` : retirer les littéraux nouveaux inutiles et conserver chaque constante dans son propriétaire ; ne pas étendre cet audit au dépôt entier.
  - [ ] Multitap et port de la souris physique
    - [ ] Modifier `src/RetroMediaLab.Emulation.Nec/Common/Machines/Common/Functions/SettingsFunctions.cs` et `src/RetroMediaLab.Emulation.Nec/Common/Machines/Common/Functions/InputFunctions.Settings.cs` : ajouter le choix multitap des machines PC Engine compatibles, conserver ce choix dans leur configuration et afficher un seul onglet Port lorsqu'il est éteint ou cinq lorsqu'il est activé.
    - [ ] Modifier `src/RetroMediaLab.Emulation.Nec/Common/Machines/Common/Functions/SettingsFunctions.cs`, `src/RetroMediaLab.Emulation.Nec/Common/Machines/Common/Functions/InputFunctions.Settings.cs` et `src/RetroMediaLab.Emulation.Nec/Emulators/BeetlePce/Services/ExternalCore.cs` : permettre dans l'onglet Souris de choisir un port compatible et transmettre au cœur la souris physique sur ce port, sans la proposer comme modèle de manette.
    - [ ] Modifier `src/RetroMediaLab.Emulation.Nec/Resources/en-US/Emulation.resx` et `src/RetroMediaLab.Emulation.Nec/Resources/*/Emulation.resx` : ajouter les libellés de ces réglages puis les traduire dans toutes les langues avec Argos ; conserver les noms de périphériques invariants dans `00-Base`.
- [ ] Compléter les écrans encore en japonais.
  - [ ] Identifier leurs ressources dans le BIOS.
    - [ ] Créer `artifacts/pce-translation/pcfx-bios-screens.json` avec, pour chaque libellé visible de la maintenance des fichiers, du lecteur CD et de l'invite de changement de disque, sa source exacte dans `pcfx.rom`, son format et les octets ou ressources à modifier, après examen des encodages et représentations indiqués ci-dessus.
  - [ ] Traduire et reconstruire la ROM française.
    - [ ] Modifier `C:\Users\overt\AppData\Roaming\Retro Media Lab\Emulation\Machines\Nec\Firmware\pcfx-fr.rom` pour remplacer les ressources identifiées par leurs équivalents français, en conservant les traductions déjà fonctionnelles du menu principal et sans modifier `pcfx.rom`.
  - [ ] Vérifier les écrans dans l'émulateur.
    - [ ] Créer `artifacts/pce-translation/pcfx-bios-screen-validation.md` avec les captures et le résultat de la vérification de ces trois écrans dans la ROM française chargée, ainsi que la taille et les empreintes des deux ROM.
