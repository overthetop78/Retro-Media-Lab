# Générateur partagé Acorn 400,005

## Référence physique

Acorn System 5 Handbook, carte CPU 6502A 400,005, §2.3, figure 9, pages imprimées 7–8 (PDF 48–49). [Manuel constructeur archivé](https://chrisacorns.computinghistory.org.uk/docs/Acorn/Manuals/Acorn_System5Handbook.pdf) sous `artifacts/.tmp-archives/machines/Acorn/Acorn System 5/`. Chronogramme inspecté : `System5-clock-timing-49.png`. Nomenclature : IC2, IC3 et IC4 sont trois 74S163 ; l'oscillateur X1/IC1 fournit 24 MHz. La fiche TI et les règles du compteur sont dans [SN74S163](../Logic/Sn74S163.md).

| Liaison réelle | Effet |
| --- | --- |
| CLK 24 MHz commun aux trois compteurs | Capture des trois ensembles d'entrées avant résolution des sorties du même front. |
| IC2/QB inversé vers CLR d'IC2 | Division par trois ; QA fournit 8 MHz avec un état haut d'une période source et un état bas de deux périodes, pas une onde artificielle à 50 %. |
| IC2/QA vers ENP et ENT d'IC3 | Un comptage sur trois clocks ; QA/QB/QC d'IC3 fournissent 4/2/1 MHz. |
| IC3/RCO inversé vers CLR d'IC4 | Synchronisation toutes les 2 µs, avec la validation ENT réelle de RCO. |
| QA/QB/QC d'IC4 | Sorties 12/6/3 MHz. |

Les liens LK2/LK3/LK1 sélectionnent ensuite respectivement 1/2/3 MHz pour le CPU. Le circuit WAIT/READY utilise IC5, un 74LS74 distinct ; les signaux décrits ici sont **avant ces circuits**. Le générateur n'est pas un ordonnanceur commun des puces de la machine.

## Réalisation du 9 novembre 2026

`RetroMediaLab.Chips/Controllers/Acorn/SystemClock/Services/SystemClockGenerator.cs` raccorde les trois instances de SN74S163. Chaque instance conserve son CLK, son compte, ses entrées et sa capture interne. Le générateur conserve le niveau de la source 24 MHz, son nombre de fronts et les niveaux du bus d'horloges. Les sorties des compteurs modifient leurs destinataires par notifications ; aucune lecture répétée d'une autre puce n'est utilisée pour rechercher des changements.

À chaque front source, toutes les captures sont réalisées avant résolution d'IC2, IC3 et IC4. Les sorties externes sont publiées lorsque les niveaux raccordés de ce front sont résolus ; les mutations de calcul ne deviennent pas des impulsions RCO fictives. SetOscillator reçoit le niveau physique de CLK ; Clock avance une période complète ; AdvanceClocks conserve actuellement chaque front unitaire. Aucune optimisation arithmétique nouvelle, aucun thread ni délai systématique d'un tick ajouté. Les sept sorties ont des notifications distinctes, sans destinataire imposé pour une sortie non raccordée.

La graine déterministe IC2=2, IC3=0, IC4=0 définit l'origine de phase du modèle ; TI ne garantit pas ces valeurs au power-on. Le constructeur interne utilisé par les contrôles permet d'exercer une phase IC4 initialement différente, puis sa remise en phase par la vraie liaison CLR. Aucun reset CPU n'est raccordé aux compteurs libres. Le modèle numérique ne prétend pas caractériser les délais analogiques de l'oscillateur ou les violations de setup/hold.

## Résultats actuels

**21 contrôles Debug réussis, zéro échec/ignoré, 58 ms** : 12 cas propres au SN74S163 et neuf cas du générateur. Les contrôles parcourent 96 fronts contre les équations du chronogramme, comparent les trois comptes et leurs clocks reçus, vérifient les sept sorties, leurs nombres de fronts et leurs phases, le rapport cyclique 8 MHz, la resynchronisation d'IC4 initialement décalé et la visibilité simultanée des niveaux finaux chez les récepteurs. Le maintien d'un niveau CLK n'ajoute pas de front. Les avances groupées sont comparées aux unitaires, avec toutes les notifications et leurs horodatages.

Ces durées sont celles des tests fonctionnels, pas une mesure du débit de la carte ou du générateur. Le composant n'est pas encore raccordé dans AcornSystemMachine : la suppression du calcul de phase VIA dans la carte et les essais actuels System 5 sont à réaliser après les circuits WAIT/READY. Ne pas présenter les anciennes mesures System 5 comme issues de ce générateur.
