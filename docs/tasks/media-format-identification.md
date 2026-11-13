# Identification technique des organisations de médias nommées d’après un logiciel

## Périmètre inventorié dans `src/RetroMediaLab.MediaEngine`

Ce travail concerne les lecteurs qui attribuent à un média entier un système de fichiers ou une organisation portant le nom du logiciel qui a permis de la découvrir. Les détecteurs qui identifient seulement le type d’un fichier contenu dans un média ne sont pas à renommer pour cette seule raison.

Les noms contrôlés et conservés hors des tâches de requalification sont :

- `K-file`, qui désigne une structure technique Atari ;
- `Z-machine`, qui désigne le format technique du contenu extrait ;
- `ATN!/File Imploder`, dont le détecteur contrôle un en-tête `ATN!`, les tailles compressée et décompressée, les limites des membres et leur alignement sur les secteurs ;
- `The Company`, qui constitue une attribution de crack relevée après la mention `CRACKED BY`, et non le nom d’un format de média.

### Apple Inform/XZIP

- `src/RetroMediaLab.MediaEngine/FileSystems/Apple/InformXzip/AppleInformXzipExceptions.cs`
- `src/RetroMediaLab.MediaEngine/FileSystems/Apple/InformXzip/AppleInformXzipFileSystemReader.cs`
- `src/RetroMediaLab.MediaEngine/FileSystems/Apple/InformXzip/AppleInformXzipLayout.cs`
- `src/RetroMediaLab.MediaEngine/FileSystems/Apple/InformXzip/ZMachineV5Header.cs`
- `src/RetroMediaLab.MediaEngine/FileSystems/Definitions/FileSystemDisplayNames.cs`
- `src/RetroMediaLab.MediaEngine/FileSystems/Definitions/FileSystemIds.cs`
- `src/RetroMediaLab.MediaEngine/FileSystems/FileSystemReaderCatalog.cs`

### Atari Print Shop

- `src/RetroMediaLab.MediaEngine/FileSystems/Atari/PrintShop/AtariPrintShopFileSystemReader.cs`
- `src/RetroMediaLab.MediaEngine/FileSystems/Definitions/FileSystemDisplayNames.cs`
- `src/RetroMediaLab.MediaEngine/FileSystems/Definitions/FileSystemIds.cs`
- `src/RetroMediaLab.MediaEngine/FileSystems/FileSystemReaderCatalog.cs`

### Atari Word Magic

- `src/RetroMediaLab.MediaEngine/FileSystems/Atari/WordMagic/AtariWordMagicDictionaryFileSystemReader.cs`
- `src/RetroMediaLab.MediaEngine/FileSystems/Definitions/FileSystemDisplayNames.cs`
- `src/RetroMediaLab.MediaEngine/FileSystems/Definitions/FileSystemIds.cs`
- `src/RetroMediaLab.MediaEngine/FileSystems/FileSystemReaderCatalog.cs`

## Travail à réaliser

    - `artifacts/media-audit/items/00000772-f8592960508afaa4/report.json`
    - `artifacts/media-audit/items/00000773-688870414f73ba8c/report.json`
    - `artifacts/media-audit/items/00000774-e7d2780344f1d870/report.json`
    - `artifacts/media-audit/items/00000775-34467d6898c6dc8c/report.json`
    - `artifacts/media-audit/items/00000776-f6d7f13bb24a4797/report.json`
    - `artifacts/media-audit/items/00000777-e53d146a6f248cfe/report.json`
    - `artifacts/media-audit/items/00000778-b31f3605abcf1cea/report.json`
    - `artifacts/media-audit/items/00000779-40182e6951130acc/report.json`
    - `artifacts/media-audit/items/00000780-b7597d78ab230bff/report.json`
    - `artifacts/media-audit/items/00000781-3db32aa9ec93131e/report.json`
    - `artifacts/media-audit/items/00000782-70253972fccbbd92/report.json`
    - `artifacts/media-audit/items/00000783-9675241d0c1076cb/report.json`

    - `artifacts/media-audit/items/00001110-7873567e641b7494/report.json`
    - `artifacts/media-audit/items/00001111-34704cf9ea63f80b/report.json`
    - `artifacts/media-audit/items/00001112-76b12a957a0aa745/report.json`
    - `artifacts/media-audit/items/00001113-714738afce5c6af8/report.json`

  - Résultat : les seules occurrences appartiennent aux entrées de catalogue `FIRE255.COM` et `FIRE255.DOC`. La première est un exécutable XEX de 255 octets et la seconde sa documentation ATASCII. Aucune structure de compression FIRE n'est présente dans ce média.

      - Résultat : le build Debug se termine sans erreur et produit `build/Debug/Retro Media Lab/retromedialab.exe`.
      - Résultat : l'image `atari.180` est reconnue avec `atari-dos`, ses 34 entrées sont reconstruites sans erreur, `FIRE255.COM` est un exécutable natif de 255 octets, `FIRE255.DOC` un document texte de 256 octets et le rapport ne contient plus `compression-fire`.

## Nettoyage après la requalification de tous les formats

- [ ] Supprimer les outils et sorties temporaires créés pour cette identification.
  - [ ] Modifier `tests/RetroMediaLab.LocalDiskImageTests/RetroMediaLab.LocalDiskImageTests.csproj` pour rétablir la compilation directe de `Program.cs`.
  - [ ] Supprimer `tests/RetroMediaLab.LocalDiskImageTests/TemporaryMediaAuditProgram.cs`.
  - [ ] Supprimer `tests/RetroMediaLab.LocalDiskImageTests/TemporaryNamedMediaFormatIdentification.cs`.
