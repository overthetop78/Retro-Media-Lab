# Suivi — Mos6551

- [x] MOS 6551 ACIA.
  - [x] Créer `docs/chips/IO/Mos6551.md` à partir de la confrontation effective de `Mos6551*.cs` aux fiches MOS/Rockwell et aux sources MAME/VICE : modes, registres, séquences modem, IRQ, horloges et écarts constatés ; ne pas créer une fiche générique.
  - [x] Créer temporairement `artifacts/.tmp-mos6551/` pour les références et le banc avant/après, avec erreurs interceptées et ressources possédées libérées dans finally ; consigner les mesures dans la fiche.
  - [x] Modifier les services, constantes, énumérations et interface sous `src/RetroMediaLab.Chips/IO/MOS/MCS6500/6551/` : corriger tous les écarts établis sur les quatre registres, formats, parité, stops, horloges internes/externes, modem, IRQ, reset, break et écho ; optimiser les calculs sans masquer le Debug ni retirer de comportement.
  - [x] Modifier et compléter les tests autonomes sous `tests/RetroMediaLab.Chips.Tests/IO/MOS/MCS6500/6551/` : tous formats/débits, phases RX/TX, tampons, erreurs, IRQ, modem, reset et transitions ; ne pas valider des attentes provenant seulement du code courant.
  - [x] Modifier si nécessaire le raccordement série des machines Archimedes sous `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/Archimedes/Common/Services/` et leurs tests locaux pour utiliser les signaux exacts et préserver les contrats entre module et Chips : variante Rockwell conservée, 51 tests de carte et deux tests spécifiques d'IRQ IOC réussis.
  - [x] Créer temporairement `artifacts/.tmp-mos6551/GuestDiagnostic/` : faire exécuter par une machine Archimedes les contrôles du 6551, produire les captures de verdicts invités dans `artifacts/TestsChips/Mos6551/<Machine>/` ; indiquer les sondes de banc et libérer le matériel et les ressources dans finally : 13 PASS invités sur l'A310, réception par timer IOC comprise, et capture VIDC vérifiée.
  - [x] Modifier `docs/chips/IO/Mos6551.md` avec les résultats regroupés Debug/Release, intégration, mesures et preuves ; reconstruire `build/Debug/Retro Media Lab/` avec les 24 modules et contrôler les points d'entrée et Chips dans lib.
  - [x] Supprimer `artifacts/.tmp-mos6551/`, références, journaux et diagnostics temporaires après usage ; conserver les captures demandées et les tests autonomes.
  - [x] Modifier l'index Git et créer le commit séparé du MOS 6551 terminé avant de commencer une autre puce.

[Index des puces](../README.md) · [Machines](../../machines/README.md)
