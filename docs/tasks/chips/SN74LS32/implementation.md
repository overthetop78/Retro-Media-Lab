# SN74LS32 — quatre portes OU

- [x] Réaliser les quatre portes avec propagation locale et reprise exacte.
  - [x] Réaliser les sorties combinatoires et la puce générique.
    - [x] Créer `src/RetroMediaLab.Chips/Logic/Common/Enums/LogicPinState.cs`, `Contracts/CombinationalState.cs` et `Services/CombinationalOutputs.cs` : représentation explicite inconnu/bas/haut/haute impédance ; aide privée à chaque instance, sorties et échéances locales, annulation nominale des pulses plus courts que la propagation, résolution simultanée avant publication, feedback à la même date, capture et restauration atomiques sans faux fronts. Aucun ordonnanceur partagé ni oscillateur.
    - [x] Créer `src/RetroMediaLab.Chips/Logic/TexasInstruments/SN74xx/SN74LS32/Interfaces/ISn74Ls32.cs`, `Models/Sn74Ls32Timing.cs` et `Services/Sn74Ls32.cs` : quatre portes OU indépendantes, entrées connues/inconnues, absence de calcul sans mutation, profil TI LS page 4 typique14/14 ns maximum22/22 ns (5 V,25°C,15 pF,2 kΩ), événements de changement et état complet.
  - [x] Vérifier la puce et le contrat commun.
    - [x] Créer `tests/RetroMediaLab.Chips.Tests/Logic/TexasInstruments/SN74xx/SN74LS32/Sn74Ls32Tests.cs` : toutes les combinaisons connues/inconnues, quatre portes, délais/fronts/annulations/feedback, reprise JSON et états invalides atomiques ; exécuter ce groupe et les contrôles d’architecture.
