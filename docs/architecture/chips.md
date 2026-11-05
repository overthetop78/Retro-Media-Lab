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

## Proposition de synchronisation événementielle — examen du 5 novembre 2026

Statut : proposition préalable aux prochains changements de code. Les interfaces et classes proposées ci-dessous ne sont pas ajoutées par cet examen. L'objectif reste la fidélité temporelle, avec un coût global réduit en Debug et Release.

### Trois responsabilités distinctes

1. **État et comportement du circuit, dans Chips.** Chaque instance possède ses registres, compteurs, phases, verrous et sorties. Elle reçoit ses entrées et les fronts qui la concernent. Elle ne cherche pas les autres puces et ne connaît ni machine, ni module, ni interface graphique. Une horloge propre est un domaine temporel émulé ; elle ne demande pas un thread ou un timer Windows par puce.
2. **Câblage et temps de la machine, dans Emulation.xxx.** La carte possède les instances, les rapports d'horloge, les phases des bus et le raccordement des broches. Elle relie une sortie à ses destinataires réels et résout les lignes partagées. Les phases et priorités simultanées sont celles de cette machine, pas un ordre universel imposé à toutes les machines.
3. **Cadence hôte et publication, dans Emulation.** EmulationFrameTimer et la publication vidéo/audio existants restent distincts du temps matériel. App consomme ces sorties par les contrats Emulation. Elle ne cadence ni ne câble les puces. Aucun changement du fonctionnement du circuit ne dépend du focus, du rafraîchissement de l'écran hôte ou d'un appel Dispatcher.

### Deux transports matériels complémentaires

**Notifications ciblées de changement de broche.** Une sortie notifie ses destinataires uniquement lorsque son niveau change. Le raccordement donne au récepteur son état initial, puis les transitions successives. Une entrée asynchrone agit selon la logique asynchrone du récepteur ; une entrée échantillonnée conserve son niveau jusqu'à son front d'échantillonnage. La notification ne lance pas arbitrairement une instruction CPU, un décalage série ou un compteur.

**Échéances de traitement.** Un compteur ou une opération peut annoncer son prochain instant observable : timeout, fin d'impulsion, événement de pipeline, bit série, conversion, transition de phase de contrôleur. Lorsqu'un accès ou un changement d'entrée survient avant cette échéance, le propriétaire ramène d'abord le circuit à l'instant requis, puis présente l'entrée à sa phase correcte. L'ancienne échéance est invalidée ou remplacée si cette entrée la modifie.

Ces deux mécanismes n'impliquent ni bus global de messages diffusés à toutes les puces, ni allocation d'objet pour chaque transition, ni file prioritaire pour chaque point vidéo. Les liaisons locales utilisent des appels typés directs ; une échéance peut être un simple compteur fixe lorsque c'est moins coûteux. Le mécanisme de calendrier commun éventuel ne porte que le temps et l'ordre, jamais les fréquences ou le câblage d'une machine particulière. Aucun nouveau framework de calendrier n'est imposé avant mesure de plusieurs raccordements réels.

### Horloges et simultanéité

Les fréquences et phases sont représentées exactement par des comptes entiers et des rapports rationnels. Aucune accumulation en secondes flottantes ne remplace les diviseurs matériels. Chaque carte choisit une représentation capable de distinguer ses fronts ; les autres cartes ne sont pas forcées à utiliser son oscillateur maître.

Deux événements au même instant sont ordonnés selon leurs phases matérielles : présentation des entrées, changement de front, capture, propagation et acquittement, en respectant le comportement propre du circuit. Il n'existe pas de règle générique du type « toutes les IRQ avant tous les accès mémoire ». Les délais de propagation et de pipeline déjà modélisés restent présents. Une notification ne doit ni anticiper ce délai ni le doubler.

### Interruptions et bus

Le propriétaire résout une ligne IRQ partagée depuis les contributions mémorisées de chaque source ; une désactivation retire seulement la contribution concernée. Le CPU échantillonne sa broche locale aux phases prévues. Supprimer la consultation répétée de toutes les sources ne signifie pas supprimer l'échantillonnage du CPU ou ses règles d'acceptation d'interruption.

Pour NMI, les transitions et le verrou de détection de front sont distincts du niveau de la ligne. Une suite de changements ne doit pas être remplacée par sa seule dernière valeur lorsque cela supprimerait un front pertinent. Les règles RESET, RDY, SO, DBE et SYNC restent également propres au CPU et à son raccordement.

Le bus conserve ses lectures, écritures, cycles transitoires, attentes et acquittements. Les écritures de registres et de mémoire vidéo sont des événements causaux : les composants concernés doivent voir l'ancien état jusqu'à l'accès matériel, puis le nouvel état au moment approprié. Les notifications ne remplacent pas le bus d'adresses et de données.

### Traitements denses et périodes sans événement

Un flux matériel dense reste dense. Les fronts de pixels, les échantillons audio, les bits série et les accès mémoire requis ne disparaissent pas parce que les sorties semblent visuellement immobiles. Les paquets déjà équivalents à une séquence de fronts restent utilisables ; leur taille ne permet pas de franchir une écriture ou une entrée intermédiaire.

À l'inverse, un circuit stable peut conserver son état sans recalcul jusqu'à son prochain événement. Son temps continue d'avancer exactement : compteurs, fractions et échéances sont restitués avant toute observation ou nouvelle entrée. Aucun regroupement n'est accepté sur la seule absence d'activité visible.

### Preuve exigée pour une migration

Pour chaque raccordement remplacé, comparer la séquence ancienne et la séquence proposée : instant, phase, broche, niveau, accès bus et résultat. Couvrir notamment les événements simultanés, impulsions courtes, changements de diviseur, accès sur le front, reset et reprogrammation avant échéance. Les sorties périodiques requises ne sont ni fusionnées ni perdues.

La comparaison finale reste celle de la machine complète, avec les mêmes scénarios, cycles et sorties, en Debug non optimisé et Release. Les traces prouvent la fidélité ; les durées globales prouvent le gain. Une notification plus élégante mais plus coûteuse n'est pas retenue comme optimisation.

### Ce qui empêche l'attente mutuelle

Les notifications entre puces ne sont pas l'unique moteur. La machine démarre avec des fronts d'horloge et des échéances déjà connus, notamment les cycles CPU. Un timer armé ou une opération en cours a une prochaine échéance, même si aucune autre puce ne lui parle. Le moteur temporel avance jusqu'au prochain événement autorisé par les dépendances, exécute ce qui est dû, propage les transitions, puis détermine les prochaines échéances.

Exemple : le CPU programme un timer VIA et poursuit ses instructions. L'échéance du timer reste programmée indépendamment de ces instructions. Lorsqu'elle arrive, le VIA publie sa sortie IRQ à sa phase matérielle ; le CPU en tient compte à son point d'échantillonnage. Ni le VIA ni le CPU n'attendent une demande réciproque pour faire avancer leur horloge.

Une puce peut donc n'avoir aucun travail entre deux événements, mais elle ne doit pas perdre son temps écoulé ou sa phase. Si elle possède encore une opération périodique utile, cette opération fait partie des prochaines échéances. Il ne s'agit pas de faire tourner chaque puce sur un thread indépendant.

### Vérification de la portée au-delà du BBC

L'examen des raccordements existants confirme que le mécanisme commun ne doit pas imposer les phases BBC :

- Atom cadence VIA, disquette et cassette depuis son cycle CPU, avec un accumulateur pour l'horloge vidéo. Il évite déjà le sondage des handshakes PPI en mode ordinaire. Ces exceptions existantes sont conservées ; une entrée de handshake changeante reste à présenter à son instant effectif.
- Electron fait avancer son ULA depuis le cycle CPU et produit l'audio à une autre cadence. L'ULA multifonction reste propriétaire de ses états internes ; le module ne la découpe pas artificiellement en événements entre pseudo-puces.
- Archimedes possède déjà AdvanceMasterTicks dans son raccordement bus. L'avance est bornée par les prochaines limites IOC, audio, mémoire et DMA, et le VIDC reçoit le nombre de clocks écoulées. Cette logique constitue un point de réutilisation réel. Elle doit préserver l'arbitrage bus/MEMC et ses limites ; le remplacement par un scheduler universel ne serait pas une amélioration acquise. Les lectures IRQ/FIQ de l'ARM ont leurs propres points d'observation, différents du chemin 6502.

Ce constat porte sur ces chemins de synchronisation, pas sur une validation complète de toutes les fonctionnalités de ces machines. Les modules d'autres marques restent libres de câbler les mêmes puces avec leurs propres domaines et phases.
