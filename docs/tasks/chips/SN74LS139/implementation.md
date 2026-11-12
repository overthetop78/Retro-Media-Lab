# SN74LS139A — deux décodeurs indépendants

- [x] Réaliser le décodeur complet.
  - [x] Réaliser les signaux et états.
    - [x] Créer `src/RetroMediaLab.Chips/Logic/TexasInstruments/SN74xx/SN74LS139/Interfaces/ISn74Ls139.cs`, `Models/Sn74Ls139Timing.cs`, `Services/Sn74Ls139.cs` : deux décodeurs 2→4, validation basse, inconnus résolus par table, trajets A/B à deux ou trois étages et G à deux étages selon TI SDLS013A pages2–3, délais distincts montée/descente ; réutiliser CombinationalOutputs privé à l’instance, snapshots indépendants et refus atomiques.
  - [x] Vérifier le décodeur.
    - [x] Créer `tests/RetroMediaLab.Chips.Tests/Logic/TexasInstruments/SN74xx/SN74LS139/Sn74Ls139Tests.cs` : table complète à trois états, les deux moitiés et huit sorties, parcours G/A/B à deux/trois niveaux, feedback via le stockage local déjà vérifié dans SN74LS32, reprise pendant propagation et validation des profils ; exécuter le groupe avec SN74LS32.

- [x] Réaliser le noyau de consommation des séquences du nouveau trajet CPU–bus–logique–mémoire.
  - [x] Définir les échanges et exécuter une plage dans le composant propriétaire.
    - [x] Créer `src/RetroMediaLab.Chips/Logic/Common/Contracts/LogicInputRange.cs`, `Contracts/LogicOutputRange.cs`, `Interfaces/ICombinationalRangeModel.cs` et modifier `Services/CombinationalOutputs.cs` : séquences datées bornées, niveaux connus, sorties résolues par mot avec masques de changements ; boucle locale unique sur entrées et propagations, sans appels publics unitaires ni publication par broche dans ce trajet. Réutiliser les mutations canoniques et garder le trajet explicite, les délais, annulations et snapshots ; le transport emprunté ne constitue pas une file matérielle.
    - [x] Modifier `src/RetroMediaLab.Chips/Logic/TexasInstruments/SN74xx/SN74LS139/Services/Sn74Ls139.cs` et `Interfaces/ISn74Ls139.cs` : partager la préparation canonique des entrées entre le trajet explicite et ExecuteInputRange, conserver les profils et états ; nouvelle entrée réservée aux échanges sans callbacks scalaires de feedback dans la plage.
  - [x] Vérifier le noyau et ses sorties avant le raccordement du circuit.
    - [x] Créer `tests/RetroMediaLab.Chips.Tests/Logic/Common/LogicRangeTests.cs` : comparer états complets et chaque sortie datée aux entrées explicites, délais simultanés/distincts, annulations, niveaux inconnus, limites de plages, restauration et validations. Aucun fichier externe ni mesure de vitesse isolée.
