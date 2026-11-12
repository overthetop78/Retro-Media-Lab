# TMM2009 — RAM statique du montage

- [x] Réaliser le bus fonctionnel avec qualification électrique ouverte.
  - [x] Réaliser les entrées et les cellules.
    - [x] Créer `src/RetroMediaLab.Chips/Memory/Toshiba/TMM20xx/TMM2009/Interfaces/ITmm2009.cs`, `Contracts/Tmm2009State.cs`, `Services/Tmm2009.cs` :1K×8,A0–A9,CE1 bas,CE2 de polarité explicite au montage,OE bas,WE bas,sortie Hi-Z ou données avec masque connu ; capturer les écritures au premier relâchement CE/WE, conserver les données de dernière phase stable, inconnus sans mémoire initiale fabriquée ; notifications à mutation, états indépendants et validation atomique. Délais nominaux nuls explicitement non caractérisés faute de fiche primaire, pas une revendication de conformité électrique.
  - [x] Vérifier le bus SRAM.
    - [x] Créer `tests/RetroMediaLab.Chips.Tests/Memory/Toshiba/TMM20xx/TMM2009/Tmm2009Tests.cs` : toutes les1024adresses et256valeurs,polarités CE2,OE/CE/WE,écriture terminée par CE/WE,byte partiellement inconnu, snapshot pendant écriture et refus atomiques ; exécuter le groupe.

- [x] Consommer les séquences du nouveau bus dans la SRAM propriétaire.
  - [x] Réutiliser le même modèle de lecture/écriture et les réserves électriques existantes.
    - [x] Modifier `src/RetroMediaLab.Chips/Memory/Toshiba/TMM20xx/TMM2009/Services/Tmm2009.cs`, créer `Services/Tmm2009.SignalRange.cs` et modifier `Interfaces/ITmm2009.cs` : appliquer les entrées typées du contrat MemorySignalRange dans un noyau local, factoriser les mutations de lecture/écriture et la résolution de sortie, conserver cellules/known, écritures interrompues et polarité CE2 ; aucun appel unitaire public ni FIFO matérielle ajoutée.
  - [x] Vérifier le modèle et ses deux transports avant raccordement.
    - [x] Compléter `tests/RetroMediaLab.Chips.Tests/Memory/Toshiba/TMM20xx/TMM2009/Tmm2009Tests.cs` : comparer réponses datées et états complets, données partielles, fins d'écriture par WE/CE, inconnus, découpages/continuations et restauration ; réutiliser les tests existants et vérifier les refus avant mutation.
