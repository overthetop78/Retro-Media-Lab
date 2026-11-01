# Dictionnaires firmware Acorn et profils BBC Micro

- [x] Réorganiser les identités et les règles sans catalogue parallèle
  - [x] Dictionnaire d’identités
    - [x] Renommer `src/RetroMediaLab.Emulation.Acorn/Common/Machines/Common/Dictionaries/FirmwareCatalog.cs` en `AcornFirmware.cs` et sa classe en `AcornFirmware` ; conserver une seule définition par empreinte avec nom, version et caractéristiques de ROM existantes.
  - [x] Profils BBC Micro
    - [x] Remplacer `src/RetroMediaLab.Emulation.Acorn/Common/Machines/BbcMicro/Dictionaries/FirmwareCompatibilityCatalog.cs` par `AcornBBCMicroModel.cs` et les fichiers de catégorie `System`, `Basic`, `Characters`, `Disc`, `Network`, `Application` et `ModelRestrictions` : rechercher les listes et règles applicables au rôle demandé, puis au modèle ; conserver les appartenances générales dans `AcornFirmware.ByCategory`.
    - [x] Modifier les références dans les fonctions, fabriques et tests Acorn : utiliser le dictionnaire d’identités et les profils BBC Micro réorganisés, sans modifier les autres modules.
  - [x] Sélection
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Common/Machines/BbcMicro/Functions/FirmwareFunctions.cs` : rendre toute image non identifiée Inconnue sans lui imposer un rôle déduit, conserver le rôle des images reconnues pour refuser les mauvais emplacements.
  - [x] Vérification et livraison
    - [x] Modifier les tests autonomes Acorn dans `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuTests.cs` et `AcornEmuModelBPlusTests.cs` : groupes partagés et particuliers, statuts officiels/possibles, refus des ROM reconnues hors profil ou du mauvais rôle, sélection des inconnues même si leur en-tête suggère un rôle.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuEconetTests.cs` et `AcornEmuVideoTests.cs` pour distinguer une ROM réseau identifiée mais incertaine (compatibilité possible) d’un fichier de caractères non identifié (Inconnue et sélectionnable).
    - [x] Remplacer les profils triés par statut dans `AcornBBCMicroModel*.cs` par des fichiers classés par catégorie, puis par modèle ; retirer les listes massives déduites des catégories et réserver Partiellement compatible aux limitations réellement identifiées.
    - [x] Modifier les projections de rôles de `FirmwareFunctions.cs` et les contrôles des supports dans `AcornEmuMachineFactory.cs` : une ROM reconnue de langage ou d’application ne doit pas être proposée comme BASIC ou DFS ; la compatibilité non établie du bon rôle reste inconnue.
    - [x] Modifier les tests Acorn concernés pour vérifier BASIC contre les autres catégories, les ROM identifiées sans compatibilité établie et les restrictions de modèle.
    - [x] Modifier `AcornFirmware.cs` pour construire les listes générales par catégorie à partir des identités déclarées, avec les appartenances multiples des images combinées et DFS/NFS ; modifier `AcornBBCMicroModel*.cs` pour consulter les listes du modèle puis de la famille avant les listes générales, sans refus anticipé par catégorie principale.
    - [x] Modifier `BbcMicro/Functions/FirmwareFunctions.cs`, `AcornEmu/Functions/FirmwareFunctions.cs` et `AcornEmuMachineFactory.cs` pour produire une candidature par rôle déclaré, avec son statut propre, et garder les inconnues sélectionnables.
    - [x] Modifier les tests Acorn pour vérifier une ROM combinée acceptée comme BASIC mais refusée comme MOS sur un autre modèle, ainsi qu’une ROM DFS/NFS présente dans deux catégories.
    - [x] Déplacer les définitions de `Common/Machines/Common/Dictionaries/AcornFirmware.cs` dans des fichiers `AcornFirmware.<catégorie>.cs` : un dictionnaire par catégorie existante ; modifier le fichier principal pour assembler ces dictionnaires sans répéter les entrées et conserver les appartenances multiples.
    - [x] Supprimer les fichiers de diagnostic temporaires du build explicitement interrompu dans `C:/Users/overt/AppData/Local/Temp/MSBuildTempgmbe3wzp.jyx/`, puis supprimer ce dossier temporaire après vérification de son chemin.
    - [x] Modifier ce suivi avec les résultats des tests regroupés et du build Debug `scripts/local-building.cmd --building=debug --modules=A` ; vérifier l’exécutable, les 24 modules et la DLL Acorn livrée.

## Règles finales

- Une identité SHA-1 est définie une fois dans `AcornFirmware.BySha1`. Les listes générales `ByCategory` utilisent ces identités sans répéter leurs noms et versions.
- Une même image peut appartenir à plusieurs listes : MOS/BASIC combinés et DFS/NFS. La catégorie principale n’est pas un veto.
- Les règles BBC Micro sont rangées par catégorie, avec les exceptions de modèles dans ces catégories. Le refus intervient après recherche dans les listes du modèle, de la famille et générales.
- Un fichier non identifié reste Inconnu et sélectionnable. Une image identifiée présente dans la bonne catégorie mais non évaluée sur le modèle reste de compatibilité inconnue. Une image connue absente des listes du rôle demandé est incompatible avec ce rôle.
- Le classement automatique de milliers de ROM comme partiellement compatibles est supprimé. Aucun statut partiel n’est fabriqué à partir d’une catégorie ou de l’absence de vérification.
- Les langages et applications ne sont plus dirigés automatiquement vers les champs BASIC et DFS. Les supports ROM paginés interrogent leurs rôles autorisés, ce qui respecte les images à plusieurs rôles.
- 370 tests Acorn/clavier réussis, y compris les catégories multiples, les exceptions de modèle, les inconnues et le mauvais rôle.

- Identités réparties dans `AcornFirmware.<catégorie>.cs` ; le fichier principal assemble les dictionnaires et conserve les appartenances multiples.

- Build Debug `--modules=A` réussi (code 0). Présence de l’exécutable et des 24 manifests/DLL vérifiée ; empreinte SHA-256 de la DLL Acorn livrée identique au dernier binaire compilé.
- Exécutable : `F:/GW GUI/build/Debug/Retro Media Lab/retromedialab.exe`.
