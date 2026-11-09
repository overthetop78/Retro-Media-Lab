# Horloge CPU 400,005 — WAIT et READY

- [x] Réaliser IC5 et les portes réellement raccordées avant les essais de la machine.
  - [x] Créer `src/RetroMediaLab.Chips/Controllers/Acorn/SystemCpuClock/Constants/SystemCpuClockConstants.cs`, `Interfaces/ISystemCpuClockControl.cs` et `Services/SystemCpuClockControl.cs` : raccorder une double bascule SN74LS74A, D2=MR et CLK2=source sélectionnée 1/2/3 MHz, PH0=source OU /Q2, CLK1=PH0 et D1=NBRq OU NON(R/W) ; Q1=RDY/NBGt, /Q1 inhibe les buffers. Retenir les entrées, conserver les clocks dans les bascules, publier les sorties uniquement à mutation et après résolution du même front. Aucune commande de reset fictive, aucun arrêt des horloges indépendantes de la carte.
  - [x] Créer `tests/RetroMediaLab.Chips.Tests/Controllers/Acorn/SystemCpuClockControlTests.cs` : vérifier les trois liens de fréquence, MR aux deux phases et reprise au front prévu, demande DMA pendant lecture/écriture, retenue de READY pendant WAIT, ordre des notifications et absence de doublons ; vérifier le branchement au générateur libre, qui continue pendant WAIT/DMA.

Source acquise : System 5 Handbook §2.4/2.5, figure 21 (PDF 62), archive déjà inspectée ; fiche SN74LS74A. PRE/CLR des deux bascules maintenus inactifs sur la carte. Le raccordement aux véritables broches du CPU et aux récepteurs de la machine reste le bloc suivant, après ces contrôles de circuit.

- [x] Mesurer le circuit actuel et consigner le bloc avant le raccordement CPU.
  - [x] Créer `tests/RetroMediaLab.Chips.Tests/Controllers/Acorn/SystemCpuClockThroughputProbe.cs` : mesurer trois passages isolés de 2 400 000 périodes normales avec la sortie PH0 observée, après 24 000 périodes de chauffe, puis conserver les relevés dans la fiche du circuit.
  - [x] Créer `docs/chips/Controllers/SystemCpuClock.md` : décrire les fils et phases confirmés par le manuel, les compteurs appartenant aux bascules, les vérifications et les mesures actuelles ; compléter la fiche System 5 avec la portée du bloc.
  - [x] Supprimer `tests/RetroMediaLab.Chips.Tests/Controllers/Acorn/SystemCpuClockThroughputProbe.cs` après consignation, puis régénérer l'assembly sans le diagnostic.

Bloc actuel : neuf tests autonomes réussis, puis banc isolé réussi à 7,590152–7,757862 millions de périodes/s. Diagnostic supprimé et assembly régénérée, zéro erreur et un avertissement xUnit2031 préexistant. Fiche `docs/chips/Controllers/SystemCpuClock.md` et fiche System 5 complétées. Le bloc de circuit est distinct du raccordement CPU restant.
