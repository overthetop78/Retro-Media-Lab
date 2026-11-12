# SN76489AN — raccordement SG-1000

Réutiliser le moteur sonore existant. Référence : fiche TI neuf pages archivée sous artifacts/.tmp-archives/chips/Audio/TexasInstruments/SN764xx/SN76489. La photo D.Lo de carte SG-1000 171-5046 permet de lire SN76489AN ; elle ne prouve pas que tous les indices IC du relevé Enri appartiennent à cette même révision.

- [x] Compléter le contrat de bus et de clock pour READY→WAIT sans sondage.
  - [x] Réaliser les signaux datés dans le moteur commun.
    - [x] Modifier `src/RetroMediaLab.Chips/Audio/TexasInstruments/SN764xx/SN76489/Services/Sn76489.cs`, `Interfaces/ISn76489.cs` et `Constants/Sn76489Constants.cs` ; créer `Services/Sn76489.Pins.cs` : notifications READY et échéance, temps/compte des clocks dans le composant, entrées clock/bus datées réutilisant les mécanismes existants, délai CE→READY configurable 0 pour le contrat historique et 90/150 ns explicites selon profil, libération CE sans clock selon table TI ; résoudre toutes les mutations du front avant notifications et borner les avances groupées aux fins de transfert.
    - [x] Modifier `Contracts/Sn76489State.cs` et `Services/Sn76489.State.cs` : capture/restauration des broches, compte de clock, temps, délai READY encore en cours et compatibilité du profil ; copie indépendante, validation atomique et absence de faux fronts.
  - [x] Vérifier le groupe complet.
    - [x] Créer `tests/RetroMediaLab.Chips.Tests/Audio/TexasInstruments/SN764xx/SN76489/Sn76489SignalTests.cs` et compléter `Sn76489ClockTests.cs`, `StateTests.cs`, `Sn76489BusTests.cs` si les contrôles existants ne couvrent pas les nouveaux états : READY daté/clock arrêtée/CE relâché/transfert de 32 clocks, sortie audio conservée, niveaux sans répétition, reprise pendant le délai, refus atomique des états invalides, comparaison Tick/AdvanceClocks avec callbacks ; exécuter tous les tests SN76489.
