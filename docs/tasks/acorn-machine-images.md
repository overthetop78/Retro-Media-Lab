# Images des machines Acorn

- [x] Compléter les images des machines déjà proposées et préparer les modèles suivants.
  - [x] Ajouter les photographies réelles.
    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Assets/Machines/AcornSystem1.jpg`, `AcornSystem2.jpg`, `AcornSystem3.jpg`, `AcornSystem4.jpg`, `AcornSystem5.jpg`, `AcornAtom.jpg` et `AcornElectron.jpg` : photographies correspondant à chaque modèle, téléchargées depuis leurs pages de référence ; aucun fichier factice ou image d'une autre machine.
    - [x] Créer les images `AcornArchimedes.jpg`, `AcornRiscPC.jpg` et `AcornA7000.jpg` dans le même dossier lorsque les modèles représentés sont identifiés ; les variantes distinctes recevront leurs propres images lors de leur intégration.
    - [x] Modifier `THIRD-PARTY-NOTICES.md` : source, auteur et licence indiqués par chaque page, sans inventer une autorisation absente.
  - [x] Raccorder les images au module existant.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Common/Constants/EmulationModuleConstants.cs` et `Common/Machines/Common/Dictionaries/MachineCatalog.cs` : chemins explicites des photographies System, Atom et Electron, en conservant ceux des BBC ; supprimer la branche qui impose une image nulle à ces modèles.
  - [x] Ajuster la présentation et la vérification existante.
    - [x] Modifier `Assets/Machines/AcornSystem3.jpg` du module et `THIRD-PARTY-NOTICES.md` : préférer la vue de face de la machine seule à la photographie contenant l'écran et le bureau.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/Acorn/AcornModuleTests.cs` : le contrôle d'images préexistant doit également refuser une machine sans chemin d'image ; conserver les vérifications des fichiers partagés BBC.
  - [x] Vérifier et livrer.
    - [x] Modifier les sorties `build/Debug/Retro Media Lab` avec `scripts/local-building.cmd --building=debug --modules=A` : vérifier le lanceur, les 24 modules, les images copiées et leur décodage ; visualiser les photographies ajoutées. Aucun nouveau test autonome nécessaire pour un raccordement de fichiers.
    - [x] Modifier ce suivi avec le résultat et consigner la reprise du bloc Archimedes.

- [x] Remplacer les photographies rectangulaires refusées par des détourages transparents.
  - [x] Créer `Assets/Machines/AcornSystem1.png` à `AcornSystem5.png`, `AcornAtom.png`, `AcornElectron.png`, `AcornArchimedes.png`, `AcornRiscPC.png` et `AcornA7000.png` dans le module : détourages produits par l'outil imagegen, canal alpha présent ; le nettoyage final et le gabarit sont traités ci-dessous.
  - [x] Modifier ces dix PNG : gabarit transparent de 512 × 512 pixels, identique à `BbcMicro.png` et `BbcMaster128.png`, proportions conservées et machine centrée. Créer `artifacts/acorn-machine-images/transparent-preview.png` : aperçu sur fonds clair et sombre pour vérifier les contours et les marges.
  - [x] Modifier `Common/Constants/EmulationModuleConstants.cs`, `THIRD-PARTY-NOTICES.md` et ce suivi ; supprimer les dix JPEG remplacés : utiliser les PNG transparents et conserver les crédits des sources.
  - [x] Modifier les aides des tests ST506 et terminer leur vérification groupée conformément à `docs/tasks/acornemu-archimedes.md` avant de reconstruire les sorties Debug ; livrer les PNG dans le build avec tous les modules, sans conserver les anciens JPEG. 660 contrôles Acorn réussis en 10 s ; build Debug All code 0, 24 manifestes avec DLL, 12 PNG Acorn identiques aux sources, aucun JPEG dans le dossier livré, lanceur présent.

## Résultat

Correction demandée : les dix JPEG ont été remplacés par des PNG transparents de 512 × 512 pixels, centrés sans déformation, comme les deux images BBC existantes. Aperçu clair/sombre : `artifacts/acorn-machine-images/transparent-preview.png`. Les résultats ci-dessous décrivent la livraison antérieure en JPEG, désormais remplacée.

Retouche : outil intégré imagegen, une image source par appel. Consigne commune : détourer la machine photographiée, préserver son matériel et ses proportions, retirer décor et ombre portée, produire un PNG avec alpha véritable. Consignes spécifiques : System 1 sans les fils hors cadre ; System 2 conserve le rack et ses rails ; Archimedes conserve boîtier et clavier sans moniteur, tapis ni souris ; A7000 conserve le boîtier sans main, souris, clavier ni câbles environnants. Redimensionnement final déterministe en 512 × 512 avec marges transparentes ; aucune dépendance ajoutée.

Sept photographies ajoutées et raccordées : System 1, 2, 3, 4, 5, Atom et Electron. Photographies A410/1, Risc PC 600 et A7000 préparées pour les modèles en cours ou à venir ; elles ne créent pas de faux modèles utilisables.

Contrôle existant `MachineArtworkUsesExistingSharedFiles` réussi (1 cas, 41 ms). Chaque machine actuellement proposée possède un chemin non vide vers un fichier existant. Les douze images Acorn sont décodables et ont été inspectées visuellement. Aucun nouveau diagnostic ni fichier temporaire.

Build Debug All terminé avec code 0 : `build/Debug/Retro Media Lab/retromedialab.exe`, 24 manifestes et leurs DLL, 12 photographies Acorn copiées et identiques aux sources (SHA256). Le bloc Archimedes continue dans `docs/tasks/acornemu-archimedes.md` : chevauchement des transferts ST506 entre secteurs, échéances DMA en écriture, formatage, ECC et contrôleurs des cartes ultérieures restent à terminer. Aucun commit anticipé du bloc incomplet.
