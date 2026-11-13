# Validation finale des médias et du matériel

Cette liste ne contient que le travail encore actif. Les tâches achevées et leurs résultats sont
conservés dans validation-completed.md.

Les corpus à auditer en lecture seule sont exactement `F:\Rétro` pour les disquettes,
`F:\86Box\Isos` pour les CD et images optiques, `C:\Users\overt\Documents\Retro Media Lab\Emulation\HDD`
et `C:\Users\overt\86Box VMs` pour les disques durs. Les fichiers restent à leur emplacement :
l'audit ne les copie, ne les déplace et ne les modifie pas.

`RetroMediaLab.LocalDiskImageTests` reste absent de `RetroMediaLab.sln` et des workflows GitHub. L'utilisateur
lance manuellement les audits dépendant de ses médias après la préparation et la compilation du
projet. Les résultats locaux servent ensuite à ajouter les corrections reproductibles à cette liste.

- [ ] 1. Valider les corpus locaux en lecture seule
  - [ ] 1.3.21 Terminer la préparation piste par piste et l'analyse des formats
    - [ ] Auditer en lecture seule le corpus local de disquettes et corriger ses régressions
      - [ ] Affiner ultérieurement les icônes et types de fichiers pour toutes les familles explorées
        - [ ] Modifier `src/RetroMediaLab.App/Dictionaries/Explorer/ExplorerFileTypeProfileCatalog.cs` pour structurer, par famille partageant les mêmes conventions, les extensions connues, le libellé de type, la catégorie d'icône et les métadonnées utiles ; afficher `Fichier <EXT>` pour une extension sans type spécial connu.
        - [ ] Modifier `src/RetroMediaLab.App/Functions/Explorer/ExplorerFileContentClassifier.cs` et `ExplorerFileIconClassifier.cs` afin d'utiliser le catalogue pour les icônes et libellés ; réserver l'identification par signature binaire à une étape ultérieure et ne jamais transmettre ces résultats à la reconnaissance du format du média.
        - [ ] Modifier les ressources communes et traduites sous `src/RetroMediaLab.App/Resources` afin de fournir les libellés de types supplémentaires dans toutes les langues sans dupliquer les valeurs invariantes.
        - [ ] Modifier les scénarios ciblés sous `tests/RetroMediaLab.Tests/Interface/ExplorerViews` pour vérifier les icônes et types de chaque famille ainsi que l'absence d'effet du catalogue d'extensions sur la détection des formats.
      - [ ] Corriger les lecteurs, décodeurs et règles de classement à partir des échecs reproductibles de `F:\Rétro`, sans modifier les images sources
        - [ ] Créer le lecteur de système de fichiers ACIDOS et son test ciblé sous `src/RetroMediaLab.MediaEngine/FileSystems` et `tests/RetroMediaLab.Tests/Media/FileSystems`, puis modifier les constantes, catalogues, compositions et `tests/RetroMediaLab.LocalDiskImageTests/MediaCorpusAuditTests.cs` pour nommer le format physique et restituer les fichiers et dossiers de l'image TeleDisk ACIDOS au lieu de la classer par défaut comme UCSD IBM MFM.
      - [ ] Modifier `tests/RetroMediaLab.LocalDiskImageTests/MediaCorpusAuditTests.cs` avec des assertions autonomes couvrant les familles de disquettes réparées et les transitions successives de formats dans le Visualiseur.
    - [ ] Auditer en lecture seule les corpus locaux de CD et d'images de disques durs
      - [ ] Modifier `tests/RetroMediaLab.LocalDiskImageTests/MediaCorpusAuditTests.cs` pour analyser directement les CD sous `F:\86Box\Isos` et les images de disques durs sous `C:\Users\overt\Documents\Retro Media Lab\Emulation\HDD` et `C:\Users\overt\86Box VMs`, sans copie ni modification des sources.
      - [ ] Modifier les lecteurs et fournisseurs de visualisation optique ou bloc concernés sous `src/RetroMediaLab.MediaEngine` et `src/RetroMediaLab.App` à partir des échecs reproductibles des corpus CD/HDD.
      - [ ] Modifier `tests/RetroMediaLab.LocalDiskImageTests/MediaCorpusAuditTests.cs` avec les assertions autonomes des formats CD/HDD corrigés et la validation de leur affichage dans le Visualiseur.
  - [ ] 1.4 Faire exécuter et exploiter manuellement l'audit local
    - [ ] Modifier `docs/project/testing.md` avec la commande ciblant `MediaCorpusAuditTests`, les racines réellement utilisées, le nombre d'images examinées et les totaux de reconnaissance, visualisation, exploration, formats non pris en charge et erreurs lus dans `build/validation/media-corpus-audit.json`.
    - [ ] Modifier `docs/reference/media-formats.md` avec les résultats réels regroupés par format et famille de machines, en distinguant reconnaissance, représentation Flux ou Sectors, Visualisateur, système de fichiers exploré, absence normale de système de fichiers et erreur à corriger.
    - [ ] Modifier `docs/tasks/validation.md` avant toute correction découverte pour ajouter, après la dernière action cochée et avant la reprise de l'audit, une action concrète par fichier de code, traduction ou test à modifier avec le défaut exact observé.
  - [ ] 1.6 Supprimer le test et le rapport temporaires après exploitation
    - [ ] Supprimer `tests/RetroMediaLab.LocalDiskImageTests/MediaCorpusAuditTests.cs` après consignation de tous les résultats.
    - [ ] Supprimer `build/validation/media-corpus-audit.json` et `build/validation/media-corpus-audit.checkpoint.jsonl` après consignation de tous les résultats.
    - [ ] Modifier `docs/project/testing.md` pour indiquer que l'audit temporaire et ses rapports ont été retirés après validation et que `RetroMediaLab.LocalDiskImageTests` reste hors de la solution et des publications.

- [ ] 2. Refaire les essais avec Greaseweazle
  - [ ] 2.1 Valider les opérations physiques disponibles
    - [ ] Modifier `docs/project/testing.md` après les essais réels de Lecture, Écriture sur support sacrifiable, relecture, conversion, Visualisateur, Explorateur et Effacement pour consigner le matériel, les médias et les résultats.
  - [ ] 2.2 Valider plusieurs contrôleurs lorsque le matériel existe
    - [ ] Modifier `docs/project/testing.md` après un essai avec plusieurs Greaseweazle et lecteurs pour consigner leur détection, leur sélection et les commandes produites.

- [ ] 3. Valider progressivement les entrées et sorties physiques internes
  - [ ] 3.1 Raccorder et vérifier l'Écriture interne
    - [ ] Modifier `src/RetroMediaLab.App/Services/PhysicalDiskWriting/PhysicalDiskWriteService.cs` pour placer l'Écriture interne derrière l'option explicite existante et conserver le repli `gw.exe` pour toute famille qui n'a pas encore été validée manuellement.
    - [ ] Modifier `docs/reference/media-formats.md` après les essais Amiga, Atari ST, IBM, MSX, Apple, Commodore, Acorn/BBC, Amstrad, Epson et DEC pour indiquer famille par famille si le repli `gw.exe` peut être retiré.
  - [ ] 3.2 Raccorder et vérifier la Lecture interne
    - [ ] Modifier `src/RetroMediaLab.App/Services/PhysicalDiskReading/PhysicalDiskReadService.cs` pour placer la Lecture interne derrière l'option explicite existante et conserver le repli `gw.exe` tant que les validations manuelles ne sont pas terminées.
    - [ ] Modifier `docs/reference/media-formats.md` après vérification du checksum SCP, des révolutions, pistes, décodages, annulations et reprises pour indiquer précisément les cas où le repli `gw.exe` peut être retiré.
