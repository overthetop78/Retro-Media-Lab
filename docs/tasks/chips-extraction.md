# Extraction des puces Acorn dans RetroMediaLab.Chips

Périmètre : déplacer les implémentations existantes et leurs tests propres, conserver le comportement matériel et les chemins critiques, intégrer la bibliothèque à la distribution RML. Aucune publication distante.

- [x] Inventaire du déplacement.
  - [x] Créer `docs/architecture/chips-acorn-inventory.md` : correspondance des fichiers actuels vers Type/Fabricant/Famille/Puce, dépendances à extraire, distinction puces/câblage et tests unitaires/intégration.
  - [x] Créer temporairement `artifacts/.tmp-chips-extraction/` : outils de déplacement syntaxique uniquement pour cette migration.
- [x] Projets et implémentations partagées.
  - [x] Créer `src/RetroMediaLab.Chips/RetroMediaLab.Chips.csproj` et `tests/RetroMediaLab.Chips.Tests/RetroMediaLab.Chips.Tests.csproj`, puis modifier `RetroMediaLab.sln` pour les intégrer aux configurations existantes.
  - [x] Classer la Video ULA dans `Video/Acorn/BbcMicro/VideoULA` et Tube ULA dans `IO/Acorn/Tube/ULA`, puis déplacer les fichiers sous `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Components/` vers `src/RetroMediaLab.Chips/<Type>/<Fabricant>/<Famille>/<Puce>/`, adapter les namespaces et créer les interfaces propres à chaque puce.
  - [x] Déplacer les circuits intégrés aux dossiers `Machines/` recensés dans l’inventaire vers leurs dossiers Chips ; modifier leurs fichiers Acorn appelants pour conserver les réglages, le câblage et les adaptations des entrées/médias dans le module.
  - [x] Créer les contrats matériels nécessaires dans les dossiers Chips concernés ; modifier les interfaces et adaptateurs existants d’Emulation pour les raccorder sans dépendance de Chips vers Emulation ou Acorn.
  - [x] Déplacer les tests autonomes des puces depuis `tests/RetroMediaLab.LocalEmulationTests/Emulation/` vers l’arborescence miroir `tests/RetroMediaLab.Chips.Tests/`, adapter les références des tests d’intégration conservés et retirer les anciens chemins.
- [x] Références et distribution.
  - [x] Modifier les projets App, Emulation et Acorn concernés, les chargeurs d’assemblies et les listes de dépendances partagées : une bibliothèque Chips fournie par l’hôte, accessible aux modules, sans installation de module Chips.
  - [x] Modifier les workflows sous `.github/workflows/` concernés pour exécuter les tests Chips et vérifier leur présence dans les builds et paquets.
  - [x] Modifier les scripts existants sous `scripts/local-building/`, `scripts/release/package/`, `scripts/module-release/` et les contrôles d’installation sous `scripts/tests/` concernés : contrôler l’inclusion de Chips dans l’application, le ZIP et l’installation, et sa résolution par Acorn.
- [x] Validation groupée et nettoyage.
  - [x] Modifier les tests utiles de `RetroMediaLab.Chips.Tests` et des projets de tests existants pour vérifier contrats, indépendance et chargement partagé ; exécuter Debug/Release et vérifier la résolution du module Acorn. Les anciens diagnostics externes ne sont pas poursuivis, à la demande de l’utilisateur.
  - [x] Modifier `docs/architecture/chips-acorn-inventory.md` avec les résultats des compilations et tests, les commandes et les éventuelles limites ; mettre à jour `docs/architecture/chips.md` avec l’organisation effectivement réalisée.
  - [x] Modifier les artefacts de build via `scripts/local-building.cmd --building=debug --modules=A` et les scripts de packaging existants : valider l’exécutable, les 24 modules, les paquets et le scénario d’installation autorisé par l’environnement, sans publier de release.
  - [x] Supprimer `artifacts/.tmp-chips-extraction/` et les fichiers temporaires de validation appartenant à cette migration ; reconstruire les assemblies ayant contenu un diagnostic supprimé.

- [x] Finalisation demandée.
  - [x] Modifier l’index Git avec les fichiers de cette extraction et créer le commit de déplacement validé, sans inclure `docs/prompts-objectifs.md` préexistant hors périmètre.
  - [x] Supprimer uniquement les dossiers réellement vides sous `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/` après le commit, en conservant tous les fichiers.
