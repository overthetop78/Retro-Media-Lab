# Acorn System 3 — choix 6502 / 6809

Un seul modèle, deux cartes CPU sélectionnables dans le contrat existant. Après System 2 `0661468ac`, réutiliser VDU40, INS8154, VIA6522 et 8271 ; traiter le 6809 puis le montage. Livraison et essai RML communs avec System 4/5, demandés par l'utilisateur.

- [x] Terminer System 3 avec ses deux cartes CPU.
  - [x] Terminer les entrées du MC6809.
    - [x] Modifier `src/RetroMediaLab.Chips/CPU/Motorola/M6800/6809/Services/Cpu6809.cs` et `Cpu6809.Interrupts.cs` : recevoir IRQ/NMI/FIRQ par Set, conserver les niveaux et échantillonner localement, supprimer lectures bus et callback FIRQ récurrents ; préserver les cycles, masques et prises d'interruption existants.
    - [x] Modifier `tests/RetroMediaLab.Chips.Tests/CPU/Motorola/M6800/6809/6809Tests.cs` et `SystemInstructionTests.cs` : raccorder les signaux des bus de tests aux nouvelles entrées, garder les attentes de cycles et piles, vérifier qu'aucun getter d'interruption du bus n'est appelé.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/AcornSystemMachine.cs` : câbler les sorties VIA et 8271 vers les entrées du 6809, comme pour la carte 6502, avec initialisation des niveaux.
    - [x] Modifier `AcornSystemMachine.cs` et `.Bus.cs` : initialiser CB1 au repos haut du clavier afin que le premier appui produise son front ; raccorder aussi la carte cassette commune sur PB7 et CB2 de la carte 6809, supprimer le décodage logiciel des modes PCR à la place de la sortie physique CB2. Ajouter dans `AcornEmuSystemRamIoTests.cs` les contrôles du premier appui et de la sortie CB2.
    - [x] Modifier `Machines/AcornSystem/Common/Constants/SystemConstants.cs` : supprimer les trois constantes du décodage PCR devenu inutilisé.
    - [x] Modifier `AcornSystemMachine.cs` : écouter ControlOutputsChanged du VIA pour appliquer la résistance de tirage CASOUT quand CB2 est libérée et le niveau Cb2 mémorisé quand elle devient sortie ; filtrer les notifications d'un driver qui ne possède pas la broche. Le premier test CB2 a révélé l'absence de ce raccordement de propriété.
  - [x] Vérifier les ROM et le disque par le montage existant.
    - [x] Créer `artifacts/.tmp-system3/System3.csproj` et `Program.cs` : banc sans fenêtre utilisant SystemFunctions.Create et les deux configurations CPU, captures natives, commandes moniteur/DOS par clavier, mesures globales Debug et libération en finally ; archiver les sources du DOS/moniteur utiles.
    - [x] Créer `F:/Retro/Acorn System/System BASIC.ssd` à partir de la source Chris Oddy déjà identifiée, sans écraser un média différent ; ouvrir les images dans le banc via EmulationSdk.MediaProcessing, tester catalogue/chargement/exécution et écritures sur copie temporaire possédée.
    - [x] Modifier `artifacts/.tmp-system3/Program.cs` pour établir les défauts exacts de chaque carte, avec RAM/PC/cycles, parités et données disque ; ajouter les seuls fichiers de production nécessaires avant de les modifier.
    - [x] Réutiliser le banc commun `artifacts/.tmp-system3/Program.cs` pour System 4/5 ; sa suppression finale est suivie dans System 5, les captures et médias de référence restent conservés.
