# Documentation de Retro Media Lab

La documentation est organisée par responsabilité. Son [mode d’organisation et d’entretien](project/documentation.md)
explique où placer l’architecture actuelle, les procédures, les références, l’interface, les idées
différées et les seules tâches encore ouvertes.

## Projet

- [Règles permanentes](project/rules.md)
- [Décisions produit et explications confirmées](project/decisions.md)
- [Préparation et publication du wiki](project/wiki.md)
- [Développement avec .NET et organisation du dépôt](project/development.md)
- [Procédure de réalisation des modules](project/emulation-module-development.md)
- [Publication des releases et snapshots](project/release.md)
- [Tests disponibles et contrôles de release](project/testing.md)
- [Scripts du dépôt](project/scripts.md)
- [Organisation et entretien de la documentation](project/documentation.md)
- [Images de disque dur](project/hard-disk-images.md)
- [Catalogue des formats de disque dur](project/hard-disk-format-catalog.md)
- [Intégration du module d’émulation Amstrad](project/amstrad-emulation.md)

## Architecture actuelle

- [Vue d’ensemble](architecture/overview.md)
- [Architecture modulaire de l’émulation](architecture/emulation.md)
- [Raccordement des modules d’émulation dynamiques](architecture/emulation-modules.md)
- [Guide autonome de développement d’un module](architecture/emulation-module-authoring.md)
- [Traduction des modules](architecture/emulation-module-localization.md)
- [Installation et mise à jour des modules](architecture/emulation-module-updates.md)
- [Versionnement du SDK](architecture/emulation-sdk-versioning.md)
- [Affichage des processus d’émulation](architecture/emulation-process-display.md)
- [Suspension du rendu des machines masquées](architecture/hidden-machine-rendering.md)
- [Médias et images de disquette](architecture/media.md)
- [Versionnement et compilation](architecture/versioning.md)

## Références techniques

- [Machines et composants d’émulation](reference/emulation-machines.md)
- [Cœurs Atari Libretro](reference/atari-libretro.md)
- [Catalogue des moteurs et DLL d’émulation](reference/emulation-core-catalog.md)
- [GameInput](reference/gameinput.md)
- [Formats de médias et couverture SCP](reference/media-formats.md)
- [Filtres et technologies vidéo](reference/emulation-video-filters.md)

## Puces, machines et directives

- [Architecture des puces](architecture/chips.md)
- [Fiches des composants](chips/)
- [Résultats et références des machines](project/emulations/)
- [Procédure puces et machines](directives/Emulations/procedure-puces-machines.md)
- [Développement des émulateurs](directives/emulation-development.md)
- [Uniformisation des modules](directives/emulation-uniformity.md)
- [Prompts d’objectifs conservés](directives/objective-prompts.md)

## Interface

- [Vue générale](ui/overview.md)
- [Lecture, écriture et conversion](ui/operations.md)
- [Visualisation, exploration et paramètres](ui/tools-and-settings.md)
- [Émulation et gestion des modules](ui/emulation.md)
- [Contrôleurs et associations](ui/controllers.md)

## Travaux à réaliser

Les suivis encore ouverts sont dans [`tasks/`](tasks/). Les procédures permanentes et les
catalogues sont rangés dans leurs dossiers respectifs ; les suivis terminés sont supprimés.

## Évolutions différées

- [Évolutions possibles des modules](future/emulation-plugins.md)
- [Organisation interne des moteurs : base commune et adaptations par moteur](future/emulation-engine-organization.md)
- [Habillages d’écran](future/screen-bezels.md)
- [Récupération adaptative des flux](future/adaptive-flux-recovery.md)

## Guide utilisateur

Le guide utilisateur est désormais préparé dans [wiki](../wiki/Home.md), avec une entrée par langue et des images partagées. L’aide de l’application ouvre le Wiki GitHub en ligne.
