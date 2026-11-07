# Validation BBC sans interface RML — 6 novembre 2026

- [ ] Vérifier les BBC Micro A, B, B+ et Master 128, Turbo, 512, ET, Compact en Debug sans fenêtre.
  - [ ] Exécuter les vérifications existantes et examiner le câblage.
    - [x] Créer `docs/tasks/machines/validation-bbc-sans-interface.md` avec le périmètre, les actions et la distinction entre signal électrique, échantillonnage et tampon matériel.
    - [x] Modifier ce document avec les résultats des tests autonomes BBC/Master, interruptions, vidéo, série, disquettes et Tube, leurs échecs éventuels et leur couverture réelle.
    - [x] Modifier ce document avec les chemins des horloges et notifications actuellement utilisés et les consultations évitables constatées, sans déclarer complète une puce sur la seule base de tests verts.
  - [ ] Appliquer la procédure commune aux puces avant les nouvelles mesures de machines (ordre confirmé par l'utilisateur).
    - [x] Créer `docs/procedure-puces-machines.md` : procédure identique pour chaque puce et machine, horloges et phases internes, entrées mémorisées, notifications ciblées, fidélité, optimisation structurelle avant tests, vérification unique du bloc puis mesures de machine complète.
    - [x] Modifier `docs/tasks/chips/README.md` et ce document avec le lien vers cette procédure et l'ordre de reprise ; laisser les sauvegardes d'état de côté.
    - [x] Modifier `docs/procedure-puces-machines.md` avec la consignation obligatoire des directives d'amélioration, leur réutilisation immédiate sur les puces suivantes et la comparaison après modification, sans répéter les recherches déjà consignées.
    - [x] Modifier ce suivi pour enregistrer la réalisation de cette mise à jour documentaire, sans lancer de test ou de build pour un changement de documentation.
    - [x] Modifier `docs/procedure-puces-machines.md` pour appliquer une amélioration découverte aux autres puces concernées, y compris déjà traitées, avec actions listées, corrections successives, tests utiles et consignation de la directive et des résultats.
    - [x] Modifier `docs/procedure-puces-machines.md` pour présenter son contenu comme des directives permanentes obligatoires, retirer toutes les cases à cocher et distinguer ces règles du suivi des tâches.
    - [x] Déplacer `docs/procedure-puces-machines.md` dans `docs/directives/Emulations/procedure-puces-machines.md` et modifier les liens de `docs/tasks/chips/README.md` et de ce suivi vers son nouvel emplacement.
    - [x] Modifier `docs/directives/Emulations/procedure-puces-machines.md` pour appliquer aussi `docs/directives/emulation-development.md`, exclure `emulation-uniformity.md` du présent travail, imposer les captures machines dans `artifacts/TestsMachines/<Marque>/<Machine>/` et maintenir les preuves puces dans `TestsChips`.
    - [x] Modifier ce suivi avec les dossiers créés sous `artifacts/TestsMachines/Acorn/` pour A, B, B+, Master 128, Turbo, 512, ET et Compact, et préciser qu'aucune capture n'est encore produite pour ce nouveau passage.
    - [x] Modifier `docs/directives/Emulations/procedure-puces-machines.md` avec le classement des sources et extractions sous `artifacts/.tmp-archives/`, la documentation thématique persistante et son enrichissement après chaque recherche utile ; créer les dossiers racines `chips` et `machines` de cette archive.
    - [ ] Modifier `docs/chips/IO/TubeUla.md` et ce document avec le résultat Debug des tests autonomes Tube après examen de son fonctionnement par strobes et de ses notifications ; conserver la validation machine pour la fin du bloc.
  - [ ] Mesurer chaque assemblage complet avec ses ROM existantes, après les puces et leur câblage.
    - [ ] Créer `artifacts/.tmp-tube/BbcMatrix.cs` pour exécuter successivement les huit assemblages (B+ 64 et 128 Kio), programme BASIC, audio, vidéo et cycles, sans fenêtre ni sortie audio Windows ; conserver les ressources empruntées et libérer celles possédées dans `finally`.
    - [ ] Modifier ce document avec les mesures Debug, l'identité des ROM et les sorties réellement obtenues par assemblage ; distinguer succès logiciel, performance et fonctions non couvertes.
    - [ ] Supprimer `artifacts/.tmp-tube/BbcMatrix.cs` et ses fichiers temporaires après consignation, puis reconstruire le banc avec sa source préexistante pour retirer le diagnostic.

## Règles matérielles vérifiées

Dossiers de preuves créés sous `artifacts/TestsMachines/Acorn/` : `BBC Micro Model A`, `BBC Micro Model B`, `BBC Micro Model B+`, `BBC Master 128`, `BBC Master Turbo`, `BBC Master 512`, `BBC Master ET`, `BBC Master Compact`. Aucune capture n'est encore produite dans ces nouveaux dossiers pour ce passage. Les captures des puces restent sous `artifacts/TestsChips/` ; les captures de machine doivent montrer les étapes successives des essais.

Une notification mémorise le niveau d'une entrée. Son effet dépend du circuit : front montant, descendant, phase de bus ou entrée asynchrone documentée. Elle ne déclenche pas arbitrairement une instruction CPU. Une file logicielle ne remplace pas une absence de tampon physique : FIFO, registre, attente et débordement doivent correspondre au matériel.

Les sauvegardes/restaurations d'état sont laissées de côté à la demande de l'utilisateur. Aucun lancement de RML n'est prévu dans ce passage.

Procédure commune : [puces et machines](../../directives/Emulations/procedure-puces-machines.md). Les mesures de carte complète ci-dessous attendent la clôture des composants du bloc en cours ; ne pas lancer un banc sur chaque petite modification.

## Premier passage Debug

## Câblage examiné

`BbcMicroMachine.Interrupts.cs` raccorde IRQ/NMI par notifications aux entrées CPU et conserve un masque agrégé. Les consultations initiales sont limitées au raccordement ; elles ne sont pas répétées à chaque cycle. `TurboBoard.cs` reçoit IRQ/NMI/RESET via callbacks et cadence le 65C102 dans `RunHostCycle` selon le rapport matériel. `Master512Board.cs` reçoit IRQ/DMA/RESET par notifications et raccorde les sorties timers/HOLD au rafraîchissement. Les lectures Tube de son reset initialisent les niveaux, sans sondage dans `RunHostCycle`.

`BbcMicroMachine.Tick` continue d'avancer audio, série, contrôleur de disquettes, Econet, vidéo et VIA, et compare les échéances ADC/RTC/Winchester. Cela ne prouve pas que tous ces appels soient évitables : leurs traitements internes doivent être examinés composant par composant. La ligne clavier est évaluée lors du balayage matériel, qui change de colonne. Aucun gain global ni conformité complète n'est déduit de cette seule lecture.

`TubeUla.cs` et `TubeUla.Bus.cs` sont pilotés par les transferts, HRST, HPHI2 et strobes parasites : pas de tick périodique ni d'oscillateur artificiel. Les FIFO correspondent aux canaux du Tube ; les notifications IRQ/NMI/DMA/RESET sont émises aux changements. La procédure ne doit pas imposer une horloge inventée à ce circuit asynchrone.

## Résultat du groupe initial

`RetroMediaLab.LocalEmulationTests`, filtre BBC/Master, interruptions, vidéo, série, tape et Tube : **128 réussis, zéro échec, 24,91 s**. Les tests sélectionnés vérifient notamment le bus DMA du Master 512, les accès Tube à leur cycle réel, l'horloge RTC, les lignes IRQ/NMI agrégées, les horloges vidéo indépendantes des instructions et les traces série/tape. Le filtre ne sélectionne pas de classe nommée `AcornEmuDiskTests` ni `AcornEmuTube` dans l'état actuel : ce passage ne constitue donc pas une validation exhaustive des disquettes ni du Tube. Les tests Master et les vérifications de câblage effectivement exécutés sont comptés dans les 128.
