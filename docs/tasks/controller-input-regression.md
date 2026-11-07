# Réception des manettes et souris — corrections du 7 novembre

- [ ] Réparer les entrées physiques observées dans RML.
  - [x] Identifier la lecture native des deux manettes avant de modifier son fonctionnement.
    - [x] Créer temporairement `tests/RetroMediaLab.LocalEmulationTests/Emulation/ControllerReadingDiagnostic.cs` : relever les lectures détaillées, les codes de résultat et le runtime GameInput sélectionné ; arrêter le moniteur dans finally.
    - [x] Modifier `src/RetroMediaLab.App/Services/Input/GameInput/Monitoring/GameInputDeviceMonitor.cs` : conserver une référence IGameInputDevice plutôt que IUnknown pour les appels de lecture natifs ; vérifier les deux périphériques réels avec le diagnostic temporaire.
    - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/ControllerReadingDiagnostic.cs` après exploitation et reconstruire l’assembly de tests pour retirer le diagnostic.
  - [x] Corriger les noms des sticks.
    - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Emulation/Input/InputBindingEditorAnalogFunctions.cs` : employer le nom produit et le profil déjà utilisés dans les préférences, conserver les identifiants physiques des associations.
  - [ ] Corriger la réception et les saccades à partir du défaut établi ; ajouter les actions concrètes identifiées avant leur exécution.
  - [ ] Livrer le bloc corrigé.
    - [ ] Remplacer `build/Debug/Retro Media Lab/retromedialab.exe`, ses bibliothèques et les modules avec `scripts/local-building.cmd --building=debug --modules=A` ; vérifier le résultat et les 24 modules.

Constats utilisateur : vibrations actives sur Xbox Rematch et Xbox 360, axes/boutons sans réaction dans les préférences et lors de l’assignation. Souris AMX : arrêt correct et mouvements lents corrects, saccades rapides sur A, B et B+. Aucun de ces défauts n’est encore attribué au thème ou à GameInput par une mesure.

Diagnostic natif : IUnknown et IGameInputDevice ont des adresses différentes (écart de 8 octets dans ce runtime). GetCurrentReading échoue avec IUnknown (0x838A0003), réussit avec IGameInputDevice (S_OK), sur Xbox 360 et Rematch. Cette mesure isole le défaut de réception indépendamment des thèmes.

Après correction : le diagnostic réel passe en Debug pour les deux manettes ; timestamp non nul, état Gamepad présent, rapport brut de 18 octets lu. Runtime C:/Windows/system32/GameInputRedist.dll. Banc temporaire supprimé puis assembly reconstruit avec succès. L’historique du pointeur erroné remonte au refactoring GameInput du 16 septembre (`d655a42c5`), antérieur au déplacement des vues dans les thèmes. Les appuis physiques et mouvements rapides dans RML restent à confirmer dans la livraison.
