# Correction des médias conservés après retrait d’un lecteur

- [x] Synchroniser la session avec les lecteurs de la nouvelle configuration.
  - [x] Modifier `src/RetroMediaLab.App/Services/Emulation/MachineSession.cs` : recevoir les lecteurs configurés lors du remplacement de la fabrique et retirer de la sélection en mémoire les médias dont le couple emplacement/type n’existe plus.
  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Emulation/Machine/MachineController.cs` : transmettre les lecteurs du runtime à la session.
- [x] Vérifier et livrer la correction.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/EmulationContracts/SessionLifecycleScenarios.cs` et `EmulationContractsTests.cs` : couvrir les lecteurs retirés ou de type différent, conserver les autres médias et le fonctionnement de la machine déjà allumée.
  - [x] Modifier ce document avec les résultats des tests ciblés et du build Debug livré.

Résultats : 30 tests EmulationContractsTests réussis en Debug, dont quatre cas de retrait/changement de type de lecteur et deux cas conservant les médias compatibles. Build Debug All terminé avec code 0 ; exécutable et 24 modules présents. Aucun essai dans la session utilisateur pendant son diagnostic HDD.
