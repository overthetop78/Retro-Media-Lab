# Master 128 — médias natifs, 7 novembre 2026

- [x] Vérifier la persistance DFS avec MOS 3.50, sans fenêtre RML ni modification des médias utilisateur.
  - [x] Créer `artifacts/.tmp-tube/MasterMediaValidation.cs` : construire le Master 128 existant, créer une disquette SSD possédée via le SDK, exécuter SAVE/LOAD par le clavier invité, comparer les 4 Kio transférés après fermeture et réouverture du média ; mesurer les fenêtres complètes Debug avec vidéo et drainage audio, capturer le verdict réellement affiché. Détacher les supports avant leur libération et nettoyer les ressources dans `finally`.
  - [x] Supprimer `artifacts/.tmp-tube/MasterMediaValidation.cs` et sa disquette après consignation des résultats ; reconstruire le projet temporaire sans cette source pour retirer le diagnostic de son assembly.

- [x] Vérifier ADFS/Winchester avec le Master 128 réel émulé.
  - [x] Créer `artifacts/.tmp-tube/MasterHddValidation.cs` : créer un DAT/DSC dédié par le SDK, géométrie 306 cylindres/2 têtes/33 secteurs de 256 octets déjà documentée ; exécuter CAT/SAVE/LOAD sous ADFS du MOS 3.50 sur chacun des deux ports physiques successivement, comparer les données après réouverture, mesurer la carte Debug et capturer les résultats. Présenter une disquette ADFS au lecteur de démarrage pour ne pas confondre l'attente d'index sans média avec un accès HDD. Conserver les horloges et les mécaniques réelles, détacher avant fermeture et supprimer les seuls médias possédés dans finally.
  - [x] Supprimer `artifacts/.tmp-tube/MasterHddValidation.cs` et ses médias après consignation ; reconstruire le banc avec sa source antérieure.

- [x] Exécuter un logiciel de jeu sur le Master 128 assemblé.
  - [x] Créer `artifacts/.tmp-tube/MasterSoftwareValidation.cs` : ouvrir la SSD Boulder Dash déjà identifiée en lecture seule via le SDK, démarrer par DFS/!BOOT, actionner les entrées du jeu, capturer plusieurs étapes et mesurer la carte Debug avec rendu/drainage audio. Comparer l'empreinte du média avant/après, détacher et libérer en finally ; ne pas ouvrir RML.
  - [x] Supprimer `artifacts/.tmp-tube/MasterSoftwareValidation.cs` après consignation puis reconstruire le banc avec sa source antérieure.

- [x] Vérifier l'enregistrement et le chargement cassette du Master 128.
  - [x] Créer `artifacts/.tmp-tube/MasterTapeValidation.cs` : enregistrer un programme BASIC par SAVE et les circuits série réels, transmettre uniquement le signal obtenu à une nouvelle carte Master, exécuter CHAIN et vérifier le texte/calcul invités. Mesurer les images Debug avec rendu/drainage, capturer les deux étapes et libérer les cartes dans finally sans fichier média utilisateur.
  - [x] Supprimer `artifacts/.tmp-tube/MasterTapeValidation.cs` après consignation puis reconstruire le banc avec sa source antérieure.

- [x] Livrer le bloc Master 128 pour l'essai en direct.
  - [x] Reconstruire `build/Debug/Retro Media Lab/retromedialab.exe`, `lib/retromedialab.chips.dll` et les 24 modules par `scripts/local-building.cmd --building=debug --modules=A` ; vérifier sortie 0, manifestes et DLL livrées, sans interrompre de session utilisateur.

Résultats des médias : [DFS persistant](../../../project/emulations/Acorn/BBC%20Master/Master%20128/dfs-persistence.md), [ADFS et les deux ports Winchester](../../../project/emulations/Acorn/BBC%20Master/Master%20128/adfs-persistence.md), [Boulder Dash](../../../project/emulations/Acorn/BBC%20Master/Master%20128/boulderdash.md), [SAVE/CHAIN cassette](../../../project/emulations/Acorn/BBC%20Master/Master%20128/cassette.md). Ces parcours passent hors RML ; les budgets Debug restant dépassés et l'essai en direct empêchent de déclarer la machine entièrement close.






Livraison du 7 novembre après ces essais : `scripts/local-building.cmd --building=debug --modules=A` terminé avec code 0. Exécutable `F:\GW GUI\build\Debug\Retro Media Lab\retromedialab.exe` présent ; 24 manifestes et 24 DLL d'entrée présents, aucune manquante. Les DLL Acorn et Chips livrées correspondent exactement aux assemblies Debug compilées (empreintes vérifiées). Aucun processus RML ouvert avant cette livraison ; aucune session utilisateur interrompue. Essai en direct demandé, résultat encore attendu. Aucun nouveau commit créé pour ces seuls documents et bancs temporaires.

- [x] Contrôler la livraison dans la vraie fenêtre RML.
  - [x] Créer `artifacts/TestsMachines/Acorn/BBC Master 128/20261107-rml-master128-idle.png` depuis la capture native de la session Master 128 du build livré, avec compteur lisible ; conserver la même preuve dans `artifacts/TestsChips/Mos65C12/BBC Master 128/`. Consigner ce qui est réellement observé, puis fermer l'instance d'essai possédée et vérifier la disparition de sa fenêtre dans finally.


RML Debug observé ensuite : démarrage du Master 128 réussi, relevés au repos de 49,2 à 50,1 FPS et 1,96 à 2,00 MHz ; capture nominale conservée, instance d'essai fermée et disparition fenêtre/processus vérifiée. La saisie automatisée n'a pas été reçue ; aucune validation du calcul ou d'un chargement en interface n'est revendiquée. [Déroulement, preuve et limites](../../../project/emulations/Acorn/BBC%20Master/Master%20128/rml-validation.md). Le retour utilisateur reste attendu avant clôture.

Validation utilisateur reçue le 7 novembre : baisse jugée minime en Debug, autorisation explicite de continuer. Bloc Master 128 accepté avec les résultats/limites ci-dessus ; passage au Master Turbo. Cette décision remplace l'attente de retour sans modifier les mesures ni annoncer les autres variantes terminées.
