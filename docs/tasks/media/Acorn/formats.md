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

- [ ] Parcours physiques et conversions dans les deux sens
  - [ ] Modifier `src/RetroMediaLab.MediaEngine/Images/Formats/Floppy/Jfd/JfdCodec.cs`, `JfdWriter.cs` et les compositions existantes pour créer une nouvelle image JFD depuis les secteurs lus, sans exiger un fichier JFD préalable.
  - [ ] Modifier les codecs Atom et `src/RetroMediaLab.MediaEngine/Images/Conversion/Sequential/TapeSignalRepresentationConverter.cs` pour décoder le protocole Atom depuis un signal enregistré et convertir TAP Atom vers les conteneurs de signal existants.
  - [ ] Compléter les tests autonomes dans `tests/RetroMediaLab.Tests/Media/JfdMediaTests.cs`, `AtomTapMediaTests.cs` et `HxcStreamMediaTests.cs` par les parcours de conversion des registres communs ; supprimer les diagnostics externes temporaires après usage.

La consignation des sources, limites et résultats dans la fiche des formats accompagne chaque bloc. Construire la livraison Debug avec tous les modules après intégration des blocs ; ne pas annoncer les modes non vérifiés comme validés. La demande physique/conversion/visualisation/exploration concerne aussi les bandes et leur signal, avec les limites propres à chaque conteneur et protocole.
