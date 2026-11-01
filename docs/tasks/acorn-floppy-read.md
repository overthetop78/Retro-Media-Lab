# Lecture disquette Acorn

Diagnostic final : doublon de logiciel réseau avec DNFS, pas uniquement une ROM Torch inadaptée. Couple séparé DFS 0.9H + Acorn NFS 3.34 validé puis appliqué avec accord explicite.

- [x] Corriger la lecture de disquette bloquée sur *CAT.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornDiskReadDiagnosticTests.cs` : reproduire le démarrage MOS/BASIC/DFS et *CAT avec les fichiers actuellement sélectionnés, tous ouverts en lecture seule ; détacher et libérer l’image dans finally.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornDiskReadDiagnosticTests.cs` : vérifier le catalogue affiché puis reproduire avec le cœur et les extensions de la configuration, sans sortie audio ni socket réseau et avec supports en lecture seule.
  - [x] Modifier ce suivi pour consigner la cause établie et les fichiers exacts à corriger avant leur modification.
  - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornDiskReadDiagnosticTests.cs` après utilisation et ses éventuels artefacts temporaires.
  - [x] Modifier ce suivi avec la suppression de la classe temporaire des DLL de tests par recompilation, puis la décision de configuration. Aucun build applicatif nécessaire sans changement de son code.

Premier diagnostic : carte BBC avec MOS 1.2, BASIC II, DNFS 1.20 et image actuelle, *CAT transfère au moins les 512 octets du catalogue. Aucun correctif matériel décidé sur ce seul constat.

Cause isolée : MOS 1.2, BASIC II, DNFS 1.20 et la même DSD affichent le catalogue (26 fichiers, !Boot inclus). Configuration complète : blocage reproduit en ROM 14, PC 9F5C, attente de la RAM 0D0C/0D0D, aucun octet disque lu. Désactiver Econet résout le blocage ; désactiver uniquement le HDD ne le résout pas. Econet et Winchester actifs avec DNFS seul, sans la ROM Torch supplémentaire : catalogue complet réussi. La ROM configurée acornemu.nfs est eco_3.35k.rom, SHA1 7462ced7b83d74b822815bc00ed40a89f84e0276, identifiée Torch NET 3.35K et incompatible par le catalogue existant.
Aucun correctif du contrôleur n’est justifié. Aucune ROM Acorn NFS séparée compatible présente dans Firmware. Choix demandé à l’utilisateur : remplacer la ROM réseau ou désactiver Econet ; aucun fichier utilisateur modifié en attendant sa réponse. Touches assignables reportées à sa demande.

Nettoyage terminé : source de diagnostic externe supprimée et DLL de tests recompilée sans cette classe. Les 47 tests préexistants du contrôleur disque passent. Seul ce suivi est nouveau dans Git ; code applicatif, modules, ROM et configuration utilisateur inchangés. Décision de configuration encore attendue.


- [x] Remplacer la ROM réseau suivant le choix explicite de l’utilisateur.
  - [x] Créer `artifacts/acorn-nfs-check/NFS-3.34.rom` depuis l’archive Acorn NVG et vérifier son SHA1 connu 8564d944fedd17caf2023454ba5a7c3947aedc1f avant utilisation.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornNfsReplacementDiagnosticTests.cs` : vérifier *CAT avec la configuration actuelle, Econet actif et la ROM candidate, sans modification des fichiers utilisateur ; image en lecture seule et détachée dans finally.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornNfsReplacementDiagnosticTests.cs` : vérifier le couple DFS 0.9H déjà présent et Acorn NFS 3.34, uniquement en mémoire, avant de proposer le changement supplémentaire de ROM disque.
  - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornNfsReplacementDiagnosticTests.cs` et recompiler les tests pour enlever la classe temporaire de la DLL.
  - [x] Créer `%APPDATA%/Retro Media Lab/Emulation/Machines/acorn/Firmware/NFS-3.34.rom` après validation ; modifier uniquement acornemu.nfs et acornemu.floppy.rom8271 dans `Configurations/997e0d119f684356beaa266efeadc0e6/machine.json`, conserver une sauvegarde préalable dédiée ; vérifier le fichier relu. Aucun fichier ROM intégré au dépôt ni remplacement du fichier Torch.
  - [x] Supprimer `artifacts/acorn-nfs-check/NFS-3.34.rom` et son dossier temporaire vide après application ou refus du remplacement.
  - [x] Modifier ce suivi avec le résultat de validation et la configuration effectivement remplacée.

Rectification du diagnostic : NFS Acorn 3.34 authentique (8192 octets, SHA1 attendu) bloque aussi avec DNFS 1.20. Le manuel Acorn DNFS indique que cette ROM remplace les ROM DFS et NFS séparées ; le problème est donc le doublon NFS, pas uniquement l’identité Torch. Aucun remplacement utilisateur effectué.

Couple testé avec succès : DFS 0.9H existant + Acorn NFS 3.34 téléchargé, tous les autres réglages conservés, Econet actif, image DSD en lecture seule ; *DISC puis *CAT affiche !Boot et le catalogue. Le remplacement NFS seul avec DNFS a échoué. L’utilisateur doit confirmer aussi le changement de la ROM disque ; aucune configuration utilisateur modifiée.

Accord reçu : appliquer DFS 0.9H + Acorn NFS 3.34. Source temporaire de test supprimée ; recompilation réussie (0 erreur), classe temporaire retirée de la DLL.

Application terminée : NFS-3.34.rom installé dans le Firmware utilisateur (8192 octets, SHA1 vérifié) ; seuls acornemu.nfs et acornemu.floppy.rom8271 ont changé dans machine.json. Econet, contrôleur, supports et tous les autres paramètres conservés. Sauvegarde machine.json.before-nfs-20261101-163602 conservée auprès du fichier. Configuration relue et comparée : aucune autre valeur modifiée.
Tous les fichiers temporaires ajoutés (ROM téléchargée dans artifacts, test de diagnostic, dossier artifacts vide) ont été supprimés ; la DLL de tests a été recompilée sans le test temporaire. Aucun changement du code applicatif, aucun nouveau build applicatif et aucun commit. Relancer l’application pour recharger la configuration.
Source ROM : https://rk.nvg.ntnu.no/bbc/rom/Acorn/fs/NFS-3.34.rom
Manuel Acorn confirmant le remplacement des ROM DFS/NFS séparées par DNFS : https://chrisacorns.computinghistory.org.uk/docs/Acorn/Tech/Acorn_TheDNFS-ib.pdf
