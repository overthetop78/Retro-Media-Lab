# Suivi — BbcMicro

- [x] Relais 8271 : conserver le temps sans retraiter un lecteur arrêté (essai mesuré puis retiré).
  - [x] Créer temporairement `artifacts/bbc-drive-idle/Intel8271Drive.before.cs`, `Benchmark.csproj` et `Program.cs` : conserver le relais et les DLL de référence reconstruites explicitement Debug/Release, préparer deux scénarios BBC complets identiques (prompt sans média et *CAT sur une DSD ouverte en lecture seule).
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/Intel8271Drive.cs` : accumuler les périodes du relais seulement sans moteur, impulsion, écriture, changement de sortie ou échéance du contrôleur ; actualiser le temps fractionnaire avant tout réveil/synchronisation et borner l'accumulation à une seconde d'entrée ; ne modifier ni la puce ni la rotation.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/Intel8271/Intel8271DriveTimingTests.cs` : vérifier les horodatages aux synchronisations observables, ajouter les intervalles irréguliers, dépassements d'une seconde et réveils média/reset ; exécuter les contrôles de raccordement/transfert 8271 en Debug/Release après le bloc.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/Intel8271Drive.cs` par sa copie avant essai et `tests/RetroMediaLab.LocalEmulationTests/Emulation/Intel8271/Intel8271DriveTimingTests.cs` pour retirer seulement les adaptations de cet essai rejeté ; reconstruire Acorn Debug et Release après restauration.
  - [x] Modifier `docs/chips/Controllers/Intel8271.md` et `docs/chips/CPU/Mos6502.md` avec les invariants, tests et mesures du BBC complet au repos et en lecture ; conserver uniquement un gain global sans régression fonctionnelle, sinon restaurer le relais de référence et reconstruire les assemblies utilisées.
  - [x] Supprimer `artifacts/bbc-drive-idle/` et ses bancs, sorties et fichiers temporaires après consignation.




- [x] Relais 8271 : actualiser la piste seulement après changement des entrées connues.
  - [x] Créer temporairement `artifacts/bbc-track-signals/` avec le relais et les six DLL de référence Debug/Release, puis un banc BBC complet au prompt et pendant *CAT, sans écriture utilisateur.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/Intel8271Drive.cs` pour invalider la piste sur notification lecteur, média, reset et écriture ; conserver le chargement au même tick moteur et les phases de rotation, sans recherche répétée de piste stable.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/Intel8271/Intel8271DriveTimingTests.cs` avec une vérification des consultations en rotation stable, puis des changements de face, de média et du reset ; exécuter le groupe Intel8271 Debug/Release après le bloc.
  - [x] Modifier `artifacts/bbc-track-signals/Program.cs` pour saisir chaque lettre avec sa touche physique, vérifier le catalogue réellement affiché, puis remplacer les mesures catalogue invalidées ; conserver les résultats au repos déjà valides.
  - [x] Modifier temporairement `artifacts/bbc-track-signals/Program.cs` pour exporter la trame finale réelle de *CAT en Debug, créer `artifacts/TestsChips/Intel8271/BbcMicroModelB/catalogue-track-signals.png` sans retouche et vérifier son contenu ; supprimer le tampon brut après conversion.
  - [x] Modifier `docs/chips/Controllers/Intel8271.md` et `docs/chips/CPU/Mos6502.md` avec les résultats et les mesures BBC complètes Debug/Release ; retirer le candidat si son gain global ne se confirme pas et reconstruire les assemblies restaurées.
  - [x] Supprimer `artifacts/bbc-track-signals/` et ses sources, DLL, exécutables et résultats temporaires après consignation.
