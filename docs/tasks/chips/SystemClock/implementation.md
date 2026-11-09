# Générateur partagé de la carte Acorn 400,005

- [x] Réaliser le générateur physique avant son raccordement aux récepteurs System 5.
  - [x] Créer `src/RetroMediaLab.Chips/Controllers/Acorn/SystemClock/Constants/SystemClockConstants.cs`, `Interfaces/ISystemClockGenerator.cs` et `Services/SystemClockGenerator.cs` : raccorder trois SN74S163 sur CLK 24 MHz, IC2/QB inversé vers CLR, IC2/QA vers ENP/ENT IC3, IC3/RCO inversé vers CLR IC4 ; conserver les états et captures dans chaque compteur, résoudre un même front partagé avant publication des sorties 12/8/6/4/3/2/1 MHz ; ne créer ni ordonnanceur global ni nouvelle horloge indépendante.
  - [x] Créer `tests/RetroMediaLab.Chips.Tests/Controllers/Acorn/SystemClockGeneratorTests.cs` : comparer les niveaux et comptes au chronogramme constructeur pendant deux périodes de 48 clocks source, vérifier le rapport cyclique réel du 8 MHz, la simultanéité des niveaux résolus, les notifications sans doublon et les avancements groupés/unitaires.

Référence : Acorn System 5 Handbook archivé, carte CPU 400,005, §2.3, figure 9 (PDF pages 48/49, imprimées 7/8). Le circuit est le générateur avant sélection de l'horloge CPU et avant les circuits WAIT/READY IC5 ; ceux-ci ne doivent pas être remplacés par des fonctions de ce générateur. Les comptes initiaux choisis alignent une phase déterministe du chronogramme ; ils ne constituent pas une garantie de power-on des compteurs.

Résultat actuel : 21 tests Debug réussis, zéro échec/ignoré, 58 ms (12 compteur, neuf générateur). Le circuit de génération est réalisé et vérifié ; son raccordement aux récepteurs et aux circuits WAIT/READY reste distinct. Référence, propriétés, fichiers et limites consignés dans `docs/chips/Controllers/SystemClock.md`.
