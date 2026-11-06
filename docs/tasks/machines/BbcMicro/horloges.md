# Suivi — BbcMicro

## Horloges possédées par leurs composants — reprise du 6 novembre 2026

- [ ] Réaliser les horloges et notifications dans chaque puce et son dossier, sans dossier Simulation.
  - [x] Créer `src/RetroMediaLab.Chips/IO/NEC/UPD700x/UPD7002/Services/Adc7002.Clock.cs` ; modifier `Adc7002.cs` et `Interfaces/IAdc7002.cs` : compteur d'entrée et fréquences possédés par la puce, avancement jusqu'au temps reçu, publication de la prochaine échéance seulement lorsqu'elle change, conservation des conversions, EOC et horloge externe.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/Common/Services/BbcMicroMachine.cs` : recevoir l'échéance ADC sans consultation périodique, synchroniser avant accès, reset et entrées analogiques ; retirer le Tick ADC au repos sans posséder son état d'horloge dans la carte.
  - [x] Modifier `docs/chips/IO/Upd7002.md` avec le fonctionnement réellement écrit et les validations différées, sans annoncer un résultat de build, de test ou de performances non exécuté.
  - [ ] Modifier ce suivi avec les fichiers précis du composant suivant avant édition, en conservant toutes les puces et interfaces dans le périmètre.
