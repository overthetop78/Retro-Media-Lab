# Moteur actif dans le cadre des statistiques

- [x] Isoler le bloc de présentation terminé pour le commit demandé.
  - [x] Modifier l’index Git pour inclure uniquement la transmission des champs vidéo, leur composition dans App, le moteur dynamique dans les statistiques et leurs tests/documents ; conserver hors de ce commit les changements de souris, des extensions et des icônes de lecteurs encore en cours, sans ajouter de fichier ignoré.

- [x] Déplacer l'indication dynamique du moteur de rendu dans le cadre existant.
  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Emulation/Machine/MachineCommandBar.cs` pour retirer le groupe visuel du moteur de la barre, sans supprimer sa source dynamique `RendererStatus`.
  - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Emulation/Machine/MachineView.cs` et `src/RetroMediaLab.App/Constants/Machine/MachinePresentationConstants.cs` pour ajouter en première ligne du cadre le moteur courant avec icône et couleur communes au texte, garder le cadre carré et y faire tenir les cinq lignes fixes.
  - [x] Modifier `tests/RetroMediaLab.Tests/Interface/EmulationViews/EmulationInteractionScenarios.cs` pour adapter la vérification existante aux cinq lignes et au déplacement de la valeur dynamique ; vérifier les scénarios de commandes concernés en Debug.

7 novembre 2026 : les deux scénarios de commandes passent en Debug (375 ms). La source RendererStatus continue d'être mise à jour par MachineController ; sa liaison affichée est vérifiée avec une valeur OpenGL. Les cinq lignes tiennent dans le carré existant de 164 × 164 DIP (28 DIP par ligne), avec icône et texte de même couleur. Cette modification est compilée dans les tests ; la livraison Debug de l'ensemble reste à construire après les autres corrections demandées.
