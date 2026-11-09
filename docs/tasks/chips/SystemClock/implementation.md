# Générateur partagé de la carte Acorn 400,005

- [x] Réaliser le générateur physique avant son raccordement aux récepteurs System 5.
  - [x] Créer `src/RetroMediaLab.Chips/Controllers/Acorn/SystemClock/Constants/SystemClockConstants.cs`, `Interfaces/ISystemClockGenerator.cs` et `Services/SystemClockGenerator.cs` : raccorder trois SN74S163 sur CLK 24 MHz, IC2/QB inversé vers CLR, IC2/QA vers ENP/ENT IC3, IC3/RCO inversé vers CLR IC4 ; conserver les états et captures dans chaque compteur, résoudre un même front partagé avant publication des sorties 12/8/6/4/3/2/1 MHz ; ne créer ni ordonnanceur global ni nouvelle horloge indépendante.
  - [x] Créer `tests/RetroMediaLab.Chips.Tests/Controllers/Acorn/SystemClockGeneratorTests.cs` : comparer les niveaux et comptes au chronogramme constructeur pendant deux périodes de 48 clocks source, vérifier le rapport cyclique réel du 8 MHz, la simultanéité des niveaux résolus, les notifications sans doublon et les avancements groupés/unitaires.

Référence : Acorn System 5 Handbook archivé, carte CPU 400,005, §2.3, figure 9 (PDF pages 48/49, imprimées 7/8). Le circuit est le générateur avant sélection de l'horloge CPU et avant les circuits WAIT/READY IC5 ; ceux-ci ne doivent pas être remplacés par des fonctions de ce générateur. Les comptes initiaux choisis alignent une phase déterministe du chronogramme ; ils ne constituent pas une garantie de power-on des compteurs.

Résultat actuel : 21 tests Debug réussis, zéro échec/ignoré, 58 ms (12 compteur, neuf générateur). Le circuit de génération est réalisé et vérifié ; son raccordement aux récepteurs et aux circuits WAIT/READY reste distinct. Référence, propriétés, fichiers et limites consignés dans `docs/chips/Controllers/SystemClock.md`.

- [x] Réduire les traitements sans effet du front descendant, sans retirer les fronts reçus par les compteurs.
  - [x] Créer `tests/RetroMediaLab.Chips.Tests/Controllers/Acorn/SystemClockThroughputProbe.cs` : mesurer le générateur actuel sur trois passages identiques de 2 400 000 périodes, après chauffe, avec et sans récepteurs externes ; conserver les comptes et niveaux vérifiables.
  - [x] Modifier `src/RetroMediaLab.Chips/Controllers/Acorn/SystemClock/Services/SystemClockGenerator.cs` : sur le front descendant, transmettre CLK bas aux trois compteurs mais supprimer les appels ResolveClock et PublishOutputs sans effet ; conserver toute la capture/résolution partagée du front montant.
  - [x] Modifier `tests/RetroMediaLab.Chips.Tests/Controllers/Acorn/SystemClockGeneratorTests.cs` : vérifier explicitement la conservation des trois CLK au front descendant, sans changement des comptes ou sorties, puis exécuter les contrôles compteur/générateur et le même banc de débit.
  - [x] Supprimer `tests/RetroMediaLab.Chips.Tests/Controllers/Acorn/SystemClockThroughputProbe.cs` après consignation des résultats dans la fiche du générateur, puis régénérer l'assembly sans ce diagnostic.

Résultat du bloc local : 22 contrôles fonctionnels réussis, deux scénarios de mesure réussis ; durée hôte moyenne réduite de 10,5 % sans récepteurs et 11,7 % avec sorties 1/2 MHz observées. Tous les relevés figurent dans la fiche. Cadence 24 MHz toujours non tenue ; aucune clôture System 5. Banc supprimé, assembly régénérée avec zéro erreur et un avertissement xUnit2031 préexistant.
