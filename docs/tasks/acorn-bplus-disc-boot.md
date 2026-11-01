# Blocage BBC B+ sur BREAK et *DISC

- [x] Diagnostiquer et corriger le démarrage disque
  - [x] Reproduction isolée
    - [x] Créer puis supprimer après utilisation `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornBPlusBootDiagnostic.cs` : reproduire la configuration utilisateur en lecture seule, comparer ANFS 4.18, NFS 3.34 et l’absence de ROM réseau, tester *DISC et Maj+BREAK, conserver uniquement les résultats dans ce suivi ; libérer les ressources en finally et supprimer les artefacts du diagnostic.
  - [x] BREAK réaffectable
    - [x] Modifier `Common/Machines/BbcMicro/Constants/KeyboardConstants.cs`, `Dictionaries/KeyboardDictionary.cs` et `Functions/BbcMicroInputSettingsFunctions.cs` du module Acorn : ajouter BREAK aux actions clavier réaffectables, avec Pause comme valeur existante par défaut.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Resources/00-Base/Emulation.resx` pour le nom invariant BREAK ; modifier les tests clavier BBC pour vérifier la définition, la sauvegarde et la transmission de l’association personnalisée.
  - [x] Correction suivant le résultat
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/Common/Services/Disks.cs` : autoriser l’accès aux registres WD1770 pendant la détection, même lorsque le contrôleur est maintenu en reset ; conserver le reset et son arrêt d’horloge. La DFS 2.26 teste le registre de piste avant de lever le reset.
    - [x] Ajouter le test autonome de détection WD1770 aux tests Acorn : écrire puis relire le registre de piste au reset, vérifier qu’aucune commande disque n’est lancée pendant le reset.

    - [x] Conserver les règles de compatibilité et les ROM utilisateur : aucune modification nécessaire, le défaut est limité à l’accès aux registres WD1770.
  - [x] Livraison
    - [x] Modifier ce suivi avec les résultats, puis vérifier le build Debug de l’application et des modules avec `scripts/local-building.cmd --building=debug --modules=A`.

Résultat du diagnostic en lecture seule : la DFS 2.26 ne détectait pas le WD1770 car son écriture de détection dans FE85 était ignorée au reset. Après correction, *DISC rend la main normalement. Avec Econet installé, Maj+D+BREAK sélectionne DFS et lit les 512 octets du catalogue ; Maj+BREAK seul sélectionne le réseau. La disquette Boulder Dash possède une option de démarrage 0 (pas d’exécution automatique) et un !BOOT texte contenant *BASIC puis CHAIN"MBOL1". Aucun fichier utilisateur modifié. Diagnostic temporaire supprimé ; sa DLL est remplacée lors de la recompilation des tests.

Validation : 376 tests AcornEmu et associations clavier réussis, dont la sauvegarde/relecture JSON de BREAK sur A, B, B+, ancien identifiant B+ 128 et Master ; Maj reste transmis avec la nouvelle touche. Le libellé BREAK est invariant et reste uniquement dans la base commune.

Livraison : scripts/local-building.cmd --building=debug --modules=A terminé avec code 0. Exécutable build/Debug/Retro Media Lab/retromedialab.exe présent, 24 manifestes et DLL de modules vérifiés ; empreinte SHA256 du module Acorn livré identique à celle du module compilé.
