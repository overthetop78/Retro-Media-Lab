# Fujitsu MB8118-10/-12

- [x] Réaliser la DRAM générique avant le raccordement VRAM.
  - [x] Réaliser le bus multiplexé et les états.
    - [x] Créer `src/RetroMediaLab.Chips/Memory/Fujitsu/MB81xx/MB8118/Interfaces/IMb8118.cs`, `Models/Mb8118Timing.cs`, `Contracts/Mb8118State.cs`, `Services/Mb8118.cs` :16K×1,adresses7+7, RAS/CAS/WE/DIN mémorisés ; lectures, écritures précoces et tardives,RMW,page mode,rafraîchissement RAS seul/caché,délais tRAC/tCAC/tOFF et sortie conservée jusqu’à CAS haut, cellules inconnues et rétention2ms par128lignes ; initialisation200µs et8cycles, contraintes numériques de hold/précharge, échéances locales et notifications datées ; snapshot indépendant validé avant mutation.
  - [x] Vérifier le composant complet.
    - [x] Créer `tests/RetroMediaLab.Chips.Tests/Memory/Fujitsu/MB81xx/MB8118/Mb8118Tests.cs` : parcours lecture/écriture de tout l’espace, modes page/RMW/rafraîchissement caché, délais, rétention et bus inconnu, reprise JSON pendant accès ; exécuter le groupe.

- [x] Réduire le coût de gestion locale des réponses DRAM identifié dans le profil machine.
  - [x] Conserver la propagation et ses retours avec une représentation interne directe.
    - [x] Modifier `src/RetroMediaLab.Chips/Memory/Fujitsu/MB81xx/MB8118/Services/Mb8118.cs` : conserver l'échéance et la dernière échéance annoncée comme dates scalaires avec absence explicite, convertir uniquement au contrat public et au snapshot ; conserver toutes les contraintes RAS/CAS/WE, les transitions et la réentrée des callbacks, sans substituer de mémoire à la puce.
  - [x] Vérifier le bloc et son intégration.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/Sega/Sg1000BoardTests.cs` en réutilisant la comparaison complète existante après la refonte ; exécuter les tests MB8118 existants puis la mesure complète du lot avec le banc commun.

- [x] Réaliser le parcours interne des broches sans les accesseurs de snapshots dans la boucle de transactions.
  - [x] Séparer les contrats immuables de la représentation utilisée par le même moteur.
    - [x] Créer `src/RetroMediaLab.Chips/Memory/Fujitsu/MB81xx/MB8118/Services/Mb8118Registers.cs` et modifier `Services/Mb8118.cs` : niveaux d'entrée et délais immuables du profil conservés en champs locaux directs ; conversion aux limites du contrat public, égalité des niveaux et toutes les contraintes conservées. Aucun second moteur, aucun changement de profil, d'ordre ou de date.
  - [x] Vérifier les transactions et la machine.
    - [x] Modifier `tests/RetroMediaLab.Chips.Tests/Memory/Fujitsu/MB81xx/MB8118/Mb8118Tests.cs` uniquement si les comparaisons existantes ne couvrent pas un comportement modifié ; réutiliser les groupes complets de tests et le profil machine trois-ROM après ce lot.

- [x] Éviter les reconstructions de contrats dans la livraison parallèle des broches.
  - [x] Donner deux entrées de transport au même résolveur de niveaux.
    - [x] Modifier `src/RetroMediaLab.Chips/Memory/Fujitsu/MB81xx/MB8118/Interfaces/IMb8118.cs` et `Services/Mb8118.cs` : ajouter ReceiveMultiplexedPins avec les niveaux primitifs, faire déléguer ReceivePins au même résolveur interne et garder les validations, délais et notifications inchangés.
    - [x] Modifier `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/SG1000/Services/Sg1000Board.Bus.cs` : décoder les niveaux de la sortie VDP une fois par notification puis les livrer aux huit instances indépendantes, sans changer leurs états, les données bit à bit ou l'ordre des callbacks.
  - [x] Vérifier l'ensemble après ce raccordement.
    - [x] Modifier `tests/RetroMediaLab.Chips.Tests/Memory/Fujitsu/MB81xx/MB8118/Mb8118Tests.cs` si les entrées partagées révèlent une couverture manquante ; exécuter les groupes composants/machine et le même profil complet avant de conclure.

- [ ] Livrer une transition commune aux DRAM raccordées sans reconstruire ses contrôles pour chaque destinataire.
  - [ ] Ajouter un transport groupé au même résolveur de chaque puce.
    - [ ] Créer `src/RetroMediaLab.Chips/Memory/Fujitsu/MB81xx/MB8118/Services/Mb8118.SharedInputs.cs` et modifier `Services/Mb8118.cs`, `Services/Mb8118Registers.cs` : recevoir les niveaux communs, un masque de destinataires et les huit DIN ; valider la forme commune une fois, conserver un seul jeu temporaire de niveaux de contrôle, puis appliquer dans le même ordre le résolveur canonique de chaque instance avec son DIN. Aucun état de puce partagé, aucun délai commun substitué et aucune publication fusionnée ; conserver les retours imbriqués avant le destinataire suivant.
  - [ ] Vérifier la livraison commune indépendamment de la machine.
    - [ ] Créer `tests/RetroMediaLab.Chips.Tests/Memory/Fujitsu/MB81xx/MB8118/SharedInputTests.cs` : comparer aux huit livraisons explicites, états complets et toutes les notifications datées, écritures/lectures/entrées inconnues et masque partiel ; conserver et rejouer les tests matériels MB8118 existants avec le lot.
Essai ReceiveSharedPins ci-dessus réalisé et vérifié, puis retiré avec le transport vidéo passif : aucune amélioration utile globale confirmée (moins de 1,2 % sur deux ROM, régression 3,73 % Castle). MB8118 reste exactement sur le code c1b074fb6 ; les huit instances et leurs livraisons individuelles sont conservées. Ce groupe ouvert décrit un transport non livré, pas une fonction matérielle manquante ; ne pas réimplémenter cet essai en l’état. Archive et preuves dans la fiche de performance Sega.
