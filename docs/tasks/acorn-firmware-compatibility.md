# ROM Acorn A/B/B+ — identification et démarrage

- [x] Corriger la sélection et fournir les ROM compatibles demandées
  - [x] Informations et compatibilité
    - [x] Étape remplacée à la demande de l’utilisateur : le JSON et son chargeur ont été retirés ; les identités nécessaires utilisent directement `FirmwareCatalog.cs`, sans nouveau catalogue ni nouvelle ressource embarquée.
    - [x] Créer puis supprimer `artifacts/acorn-rom-import/` pour télécharger les métadonnées et l’archive source, vérifier leurs empreintes et préparer les ROM externes ; supprimer ce répertoire temporaire après import.
    - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Dialogs/Emulation/FirmwareSelectionDialog.cs` pour créer les lignes visuelles à leur affichage et conserver une liste réactive avec la bibliothèque complète.
    - [x] Modifier `src/RetroMediaLab.Emulation/Contracts/EmulationFirmwareCandidate.cs`, les contrats/catalogues/fonctions firmware dans `src/RetroMediaLab.Emulation.Acorn/Common/Machines` et `Emulators/AcornEmu/Functions` : conserver l’identification et le statut pour chaque support, préciser les rôles et prérequis, interdire les ROM vocales/système incompatibles dans un champ erroné.
    - [x] Modifier `src/RetroMediaLab.App/Contracts/Emulation/Firmware/EmulationFirmwareSettingsContent.cs`, `Controllers/Emulation/Firmware/EmulationFirmwareManagementController.cs` et `Theme/Base/Views/Dialogs/Emulation/FirmwareSelectionDialog.cs` : afficher les explications localisées du candidat sélectionné dans le sélecteur commun.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Functions/FirmwareFunctions.cs` et `Resources/*/Emulation.resx` : erreur firmware explicite et traduite, au lieu du message générique de démarrage.
  - [x] ROM externes et documentation
    - [x] Créer les fichiers ROM identifiés et vérifiés dans `C:/Users/overt/AppData/Roaming/Retro Media Lab/Emulation/Machines/acorn/Firmware/` : versions compatibles pour A/B/B+, sans remplacement des fichiers existants et sans changement des sélections utilisateur.
    - [x] Modifier ce suivi avec les sources, empreintes, usages et versions des fichiers ajoutés ; conserver les ROM hors des sources du projet.
  - [x] Vérification
    - [x] Supprimer de la bibliothèque JSON et du dossier Firmware uniquement les fichiers ajoutés pendant cet import qui portent explicitement le signalement `[bad…]`, après contrôle de leur empreinte et de leur provenance dans le rapport temporaire.
    - [x] Modifier les tests firmware Acorn autonomes dans `tests/RetroMediaLab.LocalEmulationTests/Emulation` : conserver les statuts connus dans les supports B+, refuser une ROM vocale comme MOS et vérifier les explications.
    - [x] Créer puis supprimer après usage un test temporaire de démarrage B+ utilisant les ROM externes récupérées, dans `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornBPlusFirmwareStartupDiagnostic.cs` ; supprimer ses éventuels artefacts.
    - [x] Modifier les entrées B+/Aries/Integra dans `src/RetroMediaLab.Emulation.Acorn/Common/Machines/Common/Dictionaries/FirmwareCatalog.cs` pour écrire directement SHA-1, nom et version, sans alias de constantes pour ces entrées.
    - [x] Modifier `Common/Machines/Common/Dictionaries/FirmwareCatalog.cs` et `Common/Machines/Common/Contracts/FirmwareContracts.cs` du module Acorn : intégrer uniquement empreinte, nom, version, rôle et contraintes matérielles utiles dans le dictionnaire C# existant ; retirer les URL, années, notes, identifiants et noms en doublon.
    - [x] Supprimer `Common/Machines/Common/Dictionaries/AcornRomLibrary.json` et `AcornRomLibrary.cs` du module Acorn ; retirer la ressource embarquée du projet et raccorder les fonctions et les tests uniquement à `FirmwareCatalog.cs`.
    - [x] Ajouter dans le dossier Firmware Acorn et dans `FirmwareCatalog.cs` les dix images de 64 Kio acceptées par les supports EPROM BooBip existants ; modifier la limite de reconnaissance de `BbcMicroFirmwareFunctions.ScanAsync` pour ces supports, sans nouveau format de données.
    - [x] Modifier ce suivi avec les résultats des tests et du build Debug `--modules=A`, puis vérifier l’exécutable et les DLL livrés.

## Fichiers récupérés

- Source principale : [BBC Micro ROM Library](https://github.com/tobylobster/rom_library), métadonnées et archive de la branche `main`, consultées le 1 novembre 2026.
- Source de contrôle du système B+ : [b2 — ROMs](https://github.com/tom-seddon/b2/tree/master/etc/roms). BASIC II suivi du MOS B+ reproduit exactement l’image combinée attendue.
- 3 849 identités BBC Micro/B+ de 8, 16, 32 ou 64 Kio retenues ; 92 images explicitement marquées défectueuses exclues. Déduplication SHA-1, y compris avec les fichiers préexistants.
- **3 830 fichiers ajoutés** dans le dossier Firmware Acorn utilisateur. Tous les fichiers de l’archive correspondent aux empreintes publiées. Aucun fichier préexistant remplacé, aucune sélection de configuration modifiée.
- Les empreintes, noms, versions et rôles sont directement dans le dictionnaire existant `FirmwareCatalog.cs`. Les URL, années, notes d’archive, identifiants et noms en doublon ne sont pas intégrés au code. Les catégories larges de l’archive ne prouvent pas un contrôleur ou une configuration compatible ; les règles matérielles précises restent dans `FirmwareCompatibilityCatalog.cs`.
- Les images associées à un matériel supplémentaire restent conditionnelles. Les versions non éprouvées ne sont pas annoncées comme validées par des tests.

### Sélection pour le B+

| Usage | Fichier | SHA-1 |
| --- | --- | --- |
| MOS/BASIC combinés, officiel B+ | `BBC B+ Acorn MOS v2.00 + BASIC II.rom` | `ea7d3a7e3ee1ecfaa1483af994048057362b01f2` |
| DFS WD1770, officiel | `Acorn DFS v2.26 [variant 1] [1770].rom` | `cf2ebc422a8d24ec6f1a0320520c38a0e704109a` |
| ADFS WD1770, nécessite sa configuration correspondante | `Acorn ADFS v1.30 [variant 1].rom` | `301fd05c475a629c4bec70510d4507256a5b00d8` |

La configuration B+ existante sélectionnait `cm62024.bin`, une ROM de données vocales de 16 Kio, comme système. Ce fichier ne convient pas au champ MOS/BASIC combiné. Le sélecteur corrigé l’indique incompatible ; le chargement produit désormais une erreur firmware explicite.

## Vérifications

- 363 tests regroupés réussis, dont deux diagnostics temporaires de démarrage avec les vrais MOS/BASIC B+ et DFS 2.26, pour 64 et 128 Kio : invite BASIC atteinte.
- Le diagnostic dépendant de fichiers externes a été supprimé après usage. Archive, métadonnées de téléchargement et rapport temporaires supprimés de `artifacts/acorn-rom-import/`.
- Build Debug complet `scripts/local-building.cmd --building=debug --modules=A` réussi (code 0). Exécutable et 24 manifests/DLL vérifiés ; la DLL Acorn livrée a la même empreinte SHA-256 que le dernier binaire compilé.
- Exécutable : `F:/GW GUI/build/Debug/Retro Media Lab/retromedialab.exe`.

- Après retrait du JSON et de son chargeur, les 361 tests permanents passent avec le dictionnaire C# existant. Aucun champ supplémentaire n’a été ajouté à `FirmwareDefinition`.
