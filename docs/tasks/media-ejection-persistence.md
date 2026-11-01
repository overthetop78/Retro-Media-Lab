# Persistance des supports éjectés

- [x] Enregistrer les changements de supports pour toutes les émulations.
  - [x] Modifier `src/RetroMediaLab.App/Contracts/Machine/MachineControllerOptions.cs`, `Services/Emulation/MachineSession.cs` et `Theme/Base/Views/Controls/Emulation/Machine/MachineController.cs` : transmettre une sauvegarde commune appelée après insertion ou éjection effective, machine allumée ou éteinte.
  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Emulation/Machine/EmulationSectionMachineFunctions.cs` : recharger la configuration courante et sauvegarder les supports via le gestionnaire de stockage existant, sans modifier les autres réglages.
  - [x] Créer `tests/RetroMediaLab.Tests/Application/Emulation/MachineSessionMediaPersistenceTests.cs` : vérifier insertion/éjection persistées pour chaque type de support, machine allumée ou éteinte, sans fichiers externes et avec destruction des sessions.
  - [x] Modifier ce suivi avec la validation et le résultat du build Debug avec tous les modules pour conserver Acorn dans la livraison ; le code du SDK et des modules reste inchangé par cette correction.

Validation : 12 cas autonomes réussis (les six types de supports, machine allumée ou éteinte). Build application réussi, DLL livrée identique à la DLL compilée. Le mode modules=0 recrée la sortie avec Example uniquement ; relivrer tous les modules avec modules=A avant de terminer.

Livraison finale : build Debug modules=A terminé avec code 0 ; exécutable présent, empreinte de la DLL App livrée conforme, 24 manifestes et leurs DLL présents dont Acorn. Diff vérifié sans erreur. Aucune modification des données utilisateur ni commit. Sortie : `build/Debug/Retro Media Lab/retromedialab.exe`.
