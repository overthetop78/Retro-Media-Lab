# Double bascule D 74LS74A — circuit IC5 Acorn 400,005

- [x] Réaliser les deux bascules avant leur raccordement WAIT/READY.
  - [x] Créer `artifacts/.tmp-archives/chips/Logic/TexasInstruments/SN74xx/SN74LS74A/sn74ls74a.pdf`, son extraction texte et le rendu `sn74ls74a-01.png` depuis la fiche officielle TI SDLS119 ; créer `artifacts/.tmp-archives/machines/Acorn/Acorn System 5/System5-cpu-schematic-62.png` depuis la figure 21 du manuel existant pour vérifier IC5, ses entrées asynchrones et les horloges raccordées.
  - [x] Créer `src/RetroMediaLab.Chips/Logic/TexasInstruments/SN74xx/SN74LS74A/Constants/Sn74Ls74Constants.cs`, `Interfaces/ISn74Ls74.cs` et `Services/Sn74Ls74.cs` : deux canaux indépendants, D/CLK/PR/CLR mémorisés, fronts montants comptés par canal et capture atomique des deux CLK lorsqu’ils partagent un front, preset/clear asynchrones actifs bas, sorties Q et complémentaire notifiées uniquement aux changements ; représenter l'état interdit documenté sans inventer la résolution analogique à sa libération simultanée.
  - [x] Créer `tests/RetroMediaLab.Chips.Tests/Logic/TexasInstruments/SN74xx/SN74LS74A/Sn74Ls74Tests.cs` : vérifier toute la table constructeur des deux canaux, fronts/mêmes niveaux, priorité asynchrone, maintien et indépendance, puis exécuter ces contrôles.
  - [x] Créer `tests/RetroMediaLab.Chips.Tests/Logic/TexasInstruments/SN74xx/SN74LS74A/CurrentClockThroughputProbe.cs` : mesurer trois passages isolés du code actuel en Debug, double bascule avec changements observés et générateur 24 MHz existant ; conserver des compteurs vérifiables, sans ressources externes ni fenêtre.
  - [x] Créer `docs/chips/Logic/Sn74Ls74A.md` : consigner la référence TI, la table numérique complète, les états indéterminés, les entrées/sorties et clocks propriétaires, les 11 contrôles actuels et le débit mesuré ; compléter `docs/chips/Controllers/SystemClock.md` avec son débit actuel et sa portée avant raccordement.
  - [x] Supprimer `tests/RetroMediaLab.Chips.Tests/Logic/TexasInstruments/SN74xx/SN74LS74A/CurrentClockThroughputProbe.cs` après consignation, puis régénérer l'assembly de tests sans ce banc temporaire.

Le câblage complet WAIT/READY sera défini après lecture du schéma, sans supposer les connexions PR/CLR ou l'instant de reprise à partir du seul résumé textuel.

Pagination vérifiée par les sauts de page de l’extraction : la figure 21 imprimée page 23 est en page PDF 62. Le rendu 64 était un index de la carte DRAM et doit être supprimé, sans être utilisé comme référence de câblage.


Résultats actuels : 11 tests réussis (59 ms), débit 11,761373–12,053621 millions de fronts partagés/s. Banc temporaire supprimé ; assembly régénérée, zéro erreur et un avertissement xUnit2031 préexistant. Le débit du générateur est consigné séparément dans sa fiche.

- [x] Consigner la limite actuelle du circuit d'horloge avant la suite du raccordement.
  - [x] Modifier `docs/project/emulations/Acorn/Acorn System/System 5/validation.md` : ajouter le débit actuel isolé du générateur et de la double bascule, leur scénario et leurs limites, sans reprendre l'ancien résultat de machine.
