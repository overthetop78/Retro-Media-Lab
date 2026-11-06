# Répartition du suivi matériel

- [x] Répartir le suivi existant sans perdre les actions ni leur état.
  - [x] Créer temporairement `artifacts/task-layout/reorganize.ps1` et la sauvegarde du suivi source pour effectuer le déplacement en un bloc et vérifier sa conservation.
  - [x] Créer les fichiers `docs/tasks/chips/<Chip>/suivi.md` et `docs/tasks/machines/<Machine>/*.md` à partir des blocs existants de `docs/tasks/chips-validation.md` ; conserver le contenu, les cases et l'ordre de chaque bloc, avec les liens aux travaux communs.
  - [x] Déplacer les feuilles `docs/tasks/acornemu-model-{a,b,b-plus}.md`, `acorn-bplus-disc-boot.md`, `acornemu-master.md`, `acornemu-system.md`, `acornemu-atom.md`, `acornemu-electron.md` et `acornemu-archimedes.md` vers leurs dossiers de machines ; corriger les références Markdown et les chemins de suivi concernés.
  - [x] Modifier `docs/tasks/chips-validation.md` en index court et créer `docs/tasks/chips/README.md`, `docs/tasks/machines/README.md` et les index des machines ; ajouter les liens de navigation dans `docs/tasks/README.md`.
  - [x] Créer temporairement `artifacts/task-layout/verification.json` avec la couverture de toutes les lignes et la conservation de chaque case, puis vérifier les liens locaux des nouveaux fichiers.
  - [x] Supprimer `artifacts/task-layout/` après vérification ; modifier ce suivi avec le résultat effectif du rangement.

Résultat : 1 610 lignes réparties, 1 498 cases conservées dont 1 449 cochées ; 46 documents de répartition et 9 feuilles de machines déplacées. Aucun lien local cassé détecté. Scripts, sauvegarde et rapport temporaires supprimés. Aucun code ni build modifié.
