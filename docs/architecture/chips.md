# Organisation retenue pour RetroMediaLab.Chips

Ce document décrit la bibliothèque partagée et son organisation.

## Objectif et intégration

`RetroMediaLab.Chips` est une bibliothèque intégrée à la distribution de Retro Media Lab, comme les autres bibliothèques du projet. Ce n’est pas un module à installer séparément.

Les modules d’émulation la référencent directement afin de réutiliser les puces. Les corrections et optimisations d’une implémentation commune profiteront à tous les modules qui l’utilisent. Chaque machine conservera ses propres instances et son propre état matériel.

`RetroMediaLab.Chips.Tests` est un projet de tests indépendant, consacré aux puces.

## Classement

La hiérarchie retenue est :

**Type → Fabricant → Famille → Puce → Composants internes**

Les catégories de types prévues sont `CPU`, `Video`, `Audio`, `Controllers`, `IO`, `Memory`, `DMA`, `Network`, `Timers` et `Custom`. D’autres catégories pourront être ajoutées si un type manque.

Arborescence de référence fournie pour l’organisation ; les puces citées ne constituent pas une liste de fonctionnalités déjà implémentées :

```text
RetroMediaLab.Chips/
├── CPU/
│   ├── MOS/
│   │   └── MCS6500/
│   │       ├── 6502/
│   │       ├── 6502A/
│   │       ├── 65C02/
│   │       └── 65C12/
│   ├── Intel/
│   │   └── MCS80x86/
│   │       ├── 8086/
│   │       ├── 80186/
│   │       └── 80286/
│   ├── Motorola/
│   │   ├── M6800/
│   │   │   └── 6809/
│   │   └── M68000/
│   │       └── 68000/
│   ├── Zilog/
│   │   └── Z80Family/
│   │       └── Z80/
│   └── ARM/
│       └── ARMv2/
│           ├── ARM2/
│           └── ARM3/
├── Video/
├── Audio/
├── Controllers/
├── IO/
├── Memory/
├── DMA/
├── Network/
├── Timers/
└── Custom/
```

## Organisation interne d’une puce

```text
6502/
├── Constants/
├── Enums/
├── Interfaces/
└── Services/
```

Les dossiers peuvent porter un nom numérique comme `6502`. Les namespaces C# doivent employer des identifiants valides, par exemple `Mos6502` pour ce niveau.

## Organisation des tests

Le projet de tests reproduira exactement le classement **Type → Fabricant → Famille → Puce**. Les fichiers de tests seront regroupés sous la puce concernée selon les fonctions vérifiées.

```text
RetroMediaLab.Chips.Tests/
└── CPU/
    └── MOS/
        └── MCS6500/
            └── 6502/
                ├── InstructionsTests.cs
                ├── BusTests.cs
                ├── InterruptTests.cs
                └── SignalTests.cs
```

Les tests d’assemblage et de fonctionnement d’une machine resteront dans les tests de son module d’émulation.

## Variantes et partage du code

Des dossiers distincts pour les variantes ne nécessitent pas de recopier entièrement leur implémentation. Un 6502 et un 6502A peuvent partager le même moteur avec leurs caractéristiques respectives.

Le partage doit conserver les différences matérielles réelles entre les variantes. L’emplacement du moteur partagé sera défini en respectant le classement de sa famille, sans dupliquer son code dans chaque puce.

## Responsabilités

- `RetroMediaLab.Chips` contient les implémentations des puces, leurs constantes, leurs énumérations et leurs interfaces matérielles propres.
- `RetroMediaLab.Emulation` conserve les contrats d’intégration utilisés par les modules `Emulation.xxxx`.
- Chaque module conserve l’assemblage de ses machines : câblage des puces, décodage des adresses, fréquences, ROM et périphériques raccordés.

## Préservation des performances

Le déplacement dans une bibliothèque commune doit préserver les chemins d’exécution existants. Il ne doit pas introduire systématiquement de relais supplémentaires par cycle, accès mémoire ou pixel.

Les interfaces publiques des puces ne doivent pas imposer des appels indirects supplémentaires dans les traitements critiques. Les modules peuvent utiliser directement les implémentations concrètes pour ces traitements.

Lors de l’extraction, comparer les performances avant et après en Debug et Release, avec les mêmes charges, et vérifier les résultats matériels. Ne pas confondre le déplacement du code avec une réécriture de son fonctionnement.

## Dépendances effectives

- Les modules `Emulation.xxxx` référencent Chips ; Chips ne référence aucun module, ni App.
- App ne connaît pas Chips et ne référence aucun type de puce. Le chargeur de modules utilise la résolution des dépendances partagées fournie par l’hôte.
- Les interfaces matérielles sont propres à chaque puce. Le bus et l'état sont fournis par la machine qui possède ses instances.
- La DLL partagée est livrée une fois sous `lib/retromedialab.chips.dll`. Le SDK transporte ses contrats et sa DLL pour les auteurs de modules, sans paquet Chips distinct à installer.
- Les Video ULA sont sous Video ; l'ULA Tube est sous IO. Custom accueille les circuits multifonctions ou la logique spécifique ne relevant pas d'un type plus précis.

## Informations affichées par l’application

Un module `Emulation.xxxx` transmet à Emulation les informations qu’il veut exposer (noms des puces, fréquences, etc.). App affiche les données des contrats d’Emulation ; elle n’inspecte pas Chips et ne dépend pas de sa bibliothèque.
