# Renommage en Retro Media Lab

- [x] Terminer le renommage global demandé.
  - [x] Appliquer les noms confirmés dans le dépôt.
    - [x] Modifier les sources, ressources de toutes les langues, scripts, documentation, workflows, SDK et installateur : nom affiché « Retro Media Lab », namespaces/projets `RetroMediaLab`, DLL/exécutable `retromedialab.*`, dépôt `Retro-Media-Lab`.
    - [x] Renommer `GWGUI.sln` en `RetroMediaLab.sln`, les 37 dossiers et les 42 fichiers techniques concernés ; mettre à jour les références. Les 39 projets n’ont aucune référence de projet manquante.
    - [x] Modifier `.codex/config.toml` : actualiser les noms et chemins du projet. Le dossier de travail ouvert reste `F:\GW GUI` pendant les opérations.
  - [x] Préparer le SDK et les modules 2.0.0, conformément à la réponse utilisateur.
    - [x] Modifier `src/RetroMediaLab.Emulation/Constants/EmulationHostApi.cs`, `RetroMediaLab.Emulation.csproj`, tous les `src/RetroMediaLab.Emulation.*/module.json` et le modèle SDK : API 2.0, SDK/modules 2.0.0 et refus des générations antérieures.
    - [x] Modifier les fixtures des tests de compatibilité et mises à jour pour la génération 2.0, sans changer les scénarios historiques indépendants de comparaison des versions.
    - [x] Créer `.github/release-notes/sdk/v2.0.0.md` et `.github/release-notes/modules/<id>/v2.0.0.md` ; modifier `src/RetroMediaLab.Emulation/README.md` et le modèle SDK. Aucune nouvelle version publiée.
  - [x] Préserver les données existantes par migration au prochain démarrage.
    - [x] Créer `src/RetroMediaLab.App/Constants/Storage/ApplicationBrandMigrationConstants.cs` et `Services/Storage/ApplicationBrandMigration.cs`, modifier `App.xaml.cs` et `StoragePaths.cs` : renommer les anciens dossiers Roaming/Documents avant la découverte des modules, adapter les préfixes des chemins JSON, préserver conflits et originaux, prendre en compte le mode portable et la reprise après interruption.
    - [x] Modifier `installer/RetroMediaLab.iss` : garder l’AppId et nettoyer les anciens exécutables/raccourcis via des constantes réservées à la migration.
    - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Application/SettingsStorage/TemporaryBrandMigrationTests.cs` : quatre scénarios réussis ; données temporaires supprimées, aucun répertoire utilisateur réel modifié par ces tests.
  - [x] Actualiser les aperçus embarqués.
    - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Interface/Themes/TemporaryBrandPreviewTests.cs` : 33 captures des présentations Base/Studio/Orbit et des styles Windows 3.1/95/XP, enregistrées dans `src/RetroMediaLab.App/Theme/**/Preview*.png` et `Assets/Previews/*.png` ; captures utiles dans `artifacts/themes-review/screens/Rename/`.
    - [x] Modifier le montage de capture Studio pour fournir ses vrais `MainTabHeader`, sans changer le code de production Studio ; les 33 scénarios passent. Présentations libérées dans finally.
  - [x] Finaliser les vérifications et la livraison.
    - [x] Modifier les résultats dans ce suivi : suite principale 1 074 réussites / 2 échecs ; manifestes 42 réussites ; quatre migrations et 33 aperçus réussis.
    - [x] Modifier ce suivi après comparaison des échecs avec les binaires antérieurs : cartouches (unités octets/blocs) et sélection des formats d’écriture (3 choix attendus, 6 proposés). Le test cartouche, son lecteur et le fournisseur sont identiques hors marque au code précédent ; le test d’écriture est également inchangé hors marque et échoue isolément.
    - [x] Livrer `build/Debug/Retro Media Lab/retromedialab.exe` via `scripts/local-building.cmd --building=debug --modules=A`, contrôler les 24 manifestes et DLL et inscrire le résultat dans ce fichier. Le build est en cours.
  - [x] Terminer la nouvelle identité GitHub.
    - [x] Modifier le dépôt en `overthetop78/Retro-Media-Lab` et l’URL `origin`, puis inscrire les résultats ici ; identité distante vérifiée et droits ADMIN confirmés.
    - [x] Modifier les titres/descriptions des 64 publications concernées, sans changer les tags ni les fichiers. L’utilisateur confirme de conserver les anciens fichiers publiés afin de garder leurs liens valides ; les futurs paquets portent le nouveau nom.
    - [x] Modifier `scripts/wiki/publish-wiki.ps1` pour reprendre l’ancien manifeste de publication, puis publier les 31 pages du wiki renommées. Publication `91b271c` réussie ; checkout temporaire supprimé dans finally.
    - [x] Modifier ce suivi après vérification distante finale des titres, du wiki, des workflows préparés et du maintien des fichiers historiques.
  - [x] Corriger le problème d’encodage signalé.
    - [x] Réécrire `docs/tasks/retro-media-lab-rename.md` en UTF-8 explicite : remplacer le suivi corrompu sans perdre les décisions ni les résultats. Utiliser `python -X utf8` et `encoding='utf-8'` pour les commandes suivantes.
    - [x] Modifier le commentaire concerné dans `src/RetroMediaLab.Emulation/RetroMediaLab.Emulation.csproj` et vérifier les autres fichiers traités par les mêmes commandes ; ne pas modifier les accents légitimes des traductions.

Les seules occurrences historiques nécessaires dans le code servent à la migration des dossiers, exécutables et manifeste du wiki. Greaseweazle, son exécutable `gw` et les propriétés qui le configurent ne changent pas de nom. Les binaires et paquets NuGet déjà publiés ne sont pas réécrits. Le commit et le push du code sont désormais autorisés explicitement ; aucune nouvelle application/SDK/module n’est publié par cette tâche.

Les deux échecs sont reproduits à l’identique avec les DLL de tests et d’application antérieures au renommage (GWGUI.Tests.dll et gwgui.app.dll) : ils sont donc préexistants. Aucun changement hors périmètre ajouté.

- [x] Terminer les métadonnées et le nettoyage du build renommé.
  - [x] Modifier `src/RetroMediaLab.Launcher/RetroMediaLab.Launcher.csproj` et `src/RetroMediaLab.Updater/RetroMediaLab.Updater.csproj` : déclarer le nom de produit/titre Retro Media Lab dans les propriétés de fichiers Windows, avec les noms techniques déjà renommés.
  - [x] Corriger les seuls commentaires altérés de `src/RetroMediaLab.Emulation.Example/RetroMediaLab.Emulation.Example.csproj` et `src/RetroMediaLab.Emulation/Constants/EmulationHostApi.cs` à partir du commit précédent ; vérifier de nouveau l’absence de nouvelle corruption dans les fichiers concernés.
  - [x] Refaire le build Debug officiel, puis supprimer uniquement `build/Debug/GW GUI/` après vérification du nouveau build, de l’absence de processus ancien et de données portables à préserver.

Le premier build officiel renommé a terminé avec code 0. Exécutable nouveau présent, 24 manifestes/DLL en 2.0.0/API 2.0, DLL application livrée identique à la compilation. Les propriétés de produit de l’exécutable de lancement sont complétées avant livraison définitive. La comparaison d’encodage a isolé deux commentaires supplémentaires à corriger ; aucun catalogue de langue n’a de nouvelle séquence suspecte.

- [x] Vérifier le chargement des modules effectivement livrés.
  - [x] Créer puis supprimer `tests/RetroMediaLab.Tests/Application/SettingsStorage/TemporaryRenamedModuleTests.cs` : découvrir les 24 paquets de `build/Debug/Retro Media Lab/Modules`, vérifier l’identité d’assembly/namespace et les manifestes 2.0 ; libérer les ressources des modules qui exposent IDisposable et supprimer le répertoire de données temporaire dans finally.

Le fichier de suivi est réécrit en UTF-8 explicite, ainsi que les trois commentaires de projet/API affectés. Les nouveaux fichiers de notes 2.0.0 ne contiennent pas de séquences d’encodage suspectes. Les liens de comparaison des 25 notes préparées utilisent désormais les tags réellement publiés. L’utilisateur confirme la conservation des fichiers historiques sur GitHub.

Vérification distante : aucun titre de release ne porte encore l’ancien nom ; dépôt et origin confirmés. Les fichiers historiques sont conservés conformément à la réponse utilisateur. Le README, les workflows et les sources locales sont préparés mais ne sont pas encore committés/poussés sur main. Aucun catalogue de langue ne présente le nom technique à la place du nom affiché.

Livraison définitive : second build officiel --modules=A terminé avec code 0 ; nom de produit des exécutables principal et updater « Retro Media Lab », 24 manifestes/DLL valides en 2.0.0/API 2.0 et aucun ancien nom de fichier dans le nouveau build. Les 24 paquets ont été chargés par le registre réel sans erreur ; aucune exécution de cœur. Scénario temporaire et données supprimés. Ancien build Debug supprimé après contrôle de son chemin, de l’absence de processus et de données portables.

- [x] Enregistrer et pousser le renommage après accord explicite utilisateur.
  - [x] Modifier ce suivi et l’index Git avec tous les renommages et modifications, vérifier l’absence de fichiers temporaires et de corruption d’encodage, puis créer le commit sur main.
  - [x] Pousser le commit sur origin/main, vérifier l’identité distante et inscrire le résultat ici ; ne créer aucun tag ni nouvelle release.

Avant commit, les alertes d’espacement liées aux fichiers renommés ont été comparées au contenu précédent : toutes sont héritées (dont des fins de ligne CRCRLF et des blancs de licences tierces). Aucun nettoyage de formatage hors périmètre n’est ajouté.

Le commit complet du renommage est créé. Les anciens blancs de fin de ligne ont été conservés, sans nouvelle alerte d’espacement introduite.

Push principal réussi : commit 803f965c3 sur origin/main, SHA distant vérifié identique et README distant renommé. Les sources et workflows sont maintenant actualisés sur GitHub. Aucun tag ni nouvelle version d’application, SDK ou module créé ; le catalogue de modules peut être actualisé par le workflow habituel de main.
