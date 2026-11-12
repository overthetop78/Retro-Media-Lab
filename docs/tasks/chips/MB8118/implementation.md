# Fujitsu MB8118-10/-12

- [x] Réaliser la DRAM générique avant le raccordement VRAM.
  - [x] Réaliser le bus multiplexé et les états.
    - [x] Créer `src/RetroMediaLab.Chips/Memory/Fujitsu/MB81xx/MB8118/Interfaces/IMb8118.cs`, `Models/Mb8118Timing.cs`, `Contracts/Mb8118State.cs`, `Services/Mb8118.cs` :16K×1,adresses7+7, RAS/CAS/WE/DIN mémorisés ; lectures, écritures précoces et tardives,RMW,page mode,rafraîchissement RAS seul/caché,délais tRAC/tCAC/tOFF et sortie conservée jusqu’à CAS haut, cellules inconnues et rétention2ms par128lignes ; initialisation200µs et8cycles, contraintes numériques de hold/précharge, échéances locales et notifications datées ; snapshot indépendant validé avant mutation.
  - [x] Vérifier le composant complet.
    - [x] Créer `tests/RetroMediaLab.Chips.Tests/Memory/Fujitsu/MB81xx/MB8118/Mb8118Tests.cs` : parcours lecture/écriture de tout l’espace, modes page/RMW/rafraîchissement caché, délais, rétention et bus inconnu, reprise JSON pendant accès ; exécuter le groupe.
