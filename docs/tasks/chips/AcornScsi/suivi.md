# Suivi — AcornScsi

- [x] Relais Winchester : ne pas relire la sélection de cible pendant un repos déjà qualifié à vitesse normale.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/WinchesterController.cs` : retenir le cas repos/vitesse normale à l'entrée dans cet état et accumuler directement ses périodes, invalider ce cas sur toute synchronisation d'horloge avant accès, configuration, reset ou changement de média ; garder l'avancement exact des phases par le mécanisme de repos déjà existant.
  - [x] Modifier `docs/chips/Controllers/AcornScsi.md` avec les invariants du relais et les résultats des contrôles Winchester existants, en Debug et Release.
  - [x] Créer temporairement `artifacts/bbc-winchester-idle/` pour la comparaison du BBC complet avant/après en Debug et Release, modifier `docs/chips/CPU/Mos6502.md` avec les cycles, sorties et performances et retirer l'essai sans gain global.
  - [x] Supprimer `artifacts/bbc-winchester-idle/` après consignation.

[Index des puces](../README.md) · [Machines](../../machines/README.md)
