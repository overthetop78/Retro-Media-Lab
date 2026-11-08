# Formats Acorn — fonctions natives demandées

- [x] TAP Atom
  - [x] Créer `src/RetroMediaLab.MediaEngine/Images/Formats/Tape/AtomTap/AtomTapCodec.cs` : réutiliser les segments du lecteur existant, conserver les fichiers et leurs adresses, reconstruire les en-têtes natifs sans injection RAM.
  - [x] Créer `AtomTapWriter.cs` dans le même dossier et les codecs séquentiels `AtomTapeDecoder.cs` / `AtomTapeEncoder.cs` dans les dossiers Acorn de lecture/écriture : extraction des fichiers et écriture TAP, validation des blocs et limites.
  - [x] Modifier `Images/Formats/Tape/SequentialMediaComposition.cs`, `Images/Writing/MediaWritingComposition.cs` et les constantes nécessaires : enregistrer les codecs dans les parcours communs.
  - [x] Créer `tests/RetroMediaLab.Tests/Media/AtomTapMediaTests.cs` : cas autonomes de fichiers successifs, limites, intégrité, extraction et aller-retour par les registres ; corriger ensemble les défauts établis. Sept cas passent en Debug, 198 ms, avec exploration réelle par `SequentialContentDecoderAdapter`.
- [x] JFD
  - [x] Créer `artifacts/.tmp-archives/formats/JFD/jfd_dsk.cpp` avec la spécification publiée dans MAME ; extraire sous `F:/Retro/TestsFormats/JFD/` un échantillon existant de l'archive TOSEC, sans écraser de média.
  - [x] Créer les fichiers du codec natif sous `src/RetroMediaLab.MediaEngine/Images/Formats/Floppy/Jfd/` : gzip JFDI, tables, secteurs et attributs d'intégrité, lecture et écriture.
  - [x] Modifier les constantes, catalogues et compositions de lecture/écriture existants et les capacités Acorn appropriées : raccorder JFD aux modes communs sans le confondre avec ADF.
  - [x] Modifier `src/RetroMediaLab.MediaFileSystems/Constants/MediaImageFormatIds.cs`, `FileSystems/Acorn/Adfs/AcornAdfsFileSystemReader.cs` et créer `src/RetroMediaLab.MediaEngine/Images/Visualization/Policies/JfdVisualizationPolicy.cs` : exposer les systèmes ADFS reconnaissables et le codage sectoriel natif ; conserver explicitement les cas à traitement physique spécifique.
  - [x] Créer `tests/RetroMediaLab.Tests/Media/JfdMediaTests.cs` : tables, protections, limites et aller-retour ; supprimer après usage tout diagnostic ajouté dépendant de médias externes.
  - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Media/JfdActualImageTests.cs` après un essai en lecture seule de l'image TOSEC extraite ; ne conserver aucun test dépendant de `F:/Retro` dans les sources.
- [x] HXCStream
  - [x] Créer `artifacts/.tmp-archives/formats/HXCStream/` avec les sources publiques du format HxC utilisées pour contrôler en-têtes, LZ4, CRC et index.
  - [x] Modifier les fichiers sous `src/RetroMediaLab.MediaEngine/Images/Formats/Floppy/HxcStream/` : compléter l'écriture native des flux, index, métadonnées et fichiers de pistes associés.
  - [x] Modifier les compositions, catalogues et capacités des lecteurs concernés : utiliser le codec commun pour reconnaissance, écriture et exploration.
  - [x] Créer `tests/RetroMediaLab.Tests/Media/HxcStreamMediaTests.cs` : flux, index, CRC, pistes multiples et aller-retour ; supprimer après usage tout diagnostic ajouté dépendant de médias externes. Cinq cas autonomes Debug réussis, 150 ms.
  - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Media/HxcStreamActualImageTests.cs` : lire une capture HXCStream existante sous `F:/Retro/A Trier/` sans l'écrire ; vérifier ses index et son export natif en mémoire. Capture Peter Pan CPC vérifiée, 109 ms, sortie identique.

- [x] Parcours physiques et conversions dans les deux sens
  - [x] Modifier `src/RetroMediaLab.MediaEngine/Images/Formats/Floppy/Jfd/JfdCodec.cs`, `JfdWriter.cs` et les compositions existantes pour créer une nouvelle image JFD depuis les secteurs lus, sans exiger un fichier JFD préalable.
    - [x] Modifier `src/RetroMediaLab.MediaEngine/Images/Formats/Floppy/Scp/Decoding/ScpSectorDecodingComposition.cs` pour décoder les secteurs ISO d'une capture destinée à JFD sans imposer une géométrie ADF.
  - [x] Modifier les codecs Atom et `src/RetroMediaLab.MediaEngine/Images/Conversion/Sequential/TapeSignalRepresentationConverter.cs` pour décoder le protocole Atom depuis un signal enregistré et convertir TAP Atom vers les conteneurs de signal existants.
  - [x] Modifier `src/RetroMediaLab.MediaEngine/Images/Reading/DiskImageExplorer.cs` et `PhysicalMedia/Writing/FloppyMediaWritePlanningService.cs` : préparer l'écriture physique directement depuis les flux reconnus, sans décoder puis réencoder HXCStream ou HFE en secteurs.
  - [x] Modification provisoire de `src/RetroMediaLab.App/Services/PhysicalDiskReading/PhysicalDiskReadService.cs` annulée à la demande de l'utilisateur ; son code initial est rétabli. La garantie du chemin produit appartient au writer, voir le bloc de correction ci-dessous.
  - [x] Adaptation provisoire de `src/RetroMediaLab.MediaEngine/Images/Reading/MediaExplorer.cs` et `src/RetroMediaLab.MediaEngine/MediaEngineComposition.cs` annulée : le parcours existant `MediaOpeningAnalysisService` effectue déjà le décodage par `DiskImageExplorer` avant l'exploration sectorielle.
  - [x] Modifier `src/RetroMediaLab.MediaFileSystems/FileSystems/Acorn/BbcDfs/BbcDfsFileSystemReader.cs` et les fichiers `FileSystems/Fat12/Fat12{DosPlusLayout,LayoutReader,FileSystemReader,FormatCatalog}.cs` : reconnaître également les contenus DFS et FAT12 d'un JFD, en validant leur structure réelle.
  - [x] Compléter les tests autonomes dans `tests/RetroMediaLab.Tests/Media/JfdMediaTests.cs`, `AtomTapMediaTests.cs` et `HxcStreamMediaTests.cs` par les parcours de conversion des registres communs ; supprimer les diagnostics externes temporaires après usage.

- [x] Corrections avant livraison
  - [x] Rétablir `src/RetroMediaLab.App/Services/PhysicalDiskReading/PhysicalDiskReadService.cs` sans les variables `savedPath`/`converted` ajoutées dans ce passage : conserver son contrat existant.
  - [x] Modifier `src/RetroMediaLab.MediaEngine/Images/Formats/Floppy/HxcStream/HxcStreamWriter.cs` et `tests/RetroMediaLab.Tests/Media/HxcStreamMediaTests.cs` : exiger un chemin natif NN.S correspondant à la première piste et garantir que le chemin demandé est effectivement publié, sans renommage implicite.
  - [x] Modifier `src/RetroMediaLab.MediaEngine/Images/Formats/Tape/AtomTap/AtomTapReader.cs`, `AtomTapCodec.cs`, les codecs Atom et `tests/RetroMediaLab.Tests/Media/AtomTapMediaTests.cs` : sérialiser explicitement le débit 300 bauds dans l'UEF, conserver le décodage natif et contrôler les octets exportés.
- [x] Revue des raccordements et corrections demandées
  - [x] Rétablir `src/RetroMediaLab.MediaEngine/Images/Reading/MediaExplorer.cs` et `src/RetroMediaLab.MediaEngine/MediaEngineComposition.cs` dans leur fonctionnement initial ; conserver le décodage dans le parcours d'ouverture déjà existant.
  - [x] Modifier `Images/Formats/Floppy/Scp/Decoding/Sectors/ScpFormatSelection.cs`, `ScpCandidateRegistry.cs` et `Images/Formats/Floppy/Scp/Decoding/ScpSectorDecodingComposition.cs` : enregistrer HXCStream comme conteneur sans géométrie imposée, avec la sélection automatique existante.
  - [x] Corriger `tests/RetroMediaLab.Tests/Media/HxcStreamMediaTests.cs` pour exercer le décodage de `DiskImageExplorer` puis l'exploration sectorielle de `MediaExplorer`, comme le parcours d'ouverture réel.
  - [x] Modifier `Images/Formats/Tape/AtomTap/AtomTapCodec.cs` et le décodeur Atom : signaler directement les octets série non reconnus au lieu de fabriquer un segment destiné à déclencher un diagnostic.
  - [x] Modifier `Images/Formats/Floppy/Jfd/JfdCodec.cs` et `JfdMediaTests.cs` : contrôler la conservation et la modification des états CRC natifs, les tables et leurs limites selon les références du format.
  - [x] Créer `Images/Formats/Floppy/HxcStream/HxcStreamMetadataKeys.cs` et modifier le lecteur, l'encodeur et le writer HXCStream : isoler ses métadonnées natives par piste et refuser les protections qui ne sont pas représentables dans son flux.
  - [x] Compléter les tests des trois codecs sous `tests/RetroMediaLab.Tests/Media/` et les tests ciblés de leurs parcours existants ; supprimer tout diagnostic externe temporaire ajouté durant cette revue. Premier passage : 60 réussites, un défaut établi d'interprétation du format conteneur HXCStream comme format sectoriel.
  - [x] Modifier `Images/Reading/DiskImageExplorer.cs` pour distinguer le format du conteneur flux de la sélection sectorielle ; rétablir `ScpFormatSelection.cs` et `ScpCandidateRegistry.cs` sans la sélection nullable ajoutée, retirer son inscription HXCStream et vérifier le parcours réel dans `HxcStreamMediaTests.cs`. Neuf cas HXCStream passent après correction.
  - [x] Créer `artifacts/.tmp-archives/formats/JFD/source-access.md` avec la référence de l'archive officielle ADFFS et l'échec d'accès Cloudflare ; ne pas prétendre avoir vérifié le code de l'auteur non téléchargé.
  - [x] Modifier `Images/Reading/Decoding/Sequential/Acorn/AtomTapeDecoder.cs` et `tests/RetroMediaLab.Tests/Media/AtomTapMediaTests.cs` : ne jamais ignorer silencieusement un bloc UEF dont le cadrage série n'est pas celui d'Atom.
  - [x] Modifier les assertions de `tests/RetroMediaLab.Tests/Media/` et corriger les fichiers concernés seulement si le dernier passage groupé de régression établit un défaut ; conserver les parcours des formats préexistants. Dernier passage : 79 tests Debug réussis, aucun échec.
- [x] Livraison Debug
  - [x] Générer `build/Debug/Retro Media Lab/retromedialab.exe`, ses bibliothèques dans `lib/` et les 24 DLL de modules dans `modules/` avec `scripts/local-building.cmd --building=debug --modules=A` ; contrôler la réussite du script et chaque sortie attendue. Script terminé avec le code 0 le 2026-11-08 ; exécutable, bibliothèques partagées et chaque DLL vérifiés.

La consignation des sources, limites et résultats dans la fiche des formats accompagne chaque bloc. Ne pas annoncer les modes non vérifiés comme validés. La demande physique/conversion/visualisation/exploration concerne aussi les bandes et leur signal, avec les limites propres à chaque conteneur et protocole.
