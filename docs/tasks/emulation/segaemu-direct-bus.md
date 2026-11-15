- [x] Evaluer puis retirer les nouveaux acces directs ROM sur le bus SegaEmu
  - [x] Emprunter la ROM existante sans duplication
    - [x] Modifier src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Boards/SegaBoard.32X.cs : transmettre la memoire ROM word-swapped existante a l'adaptateur.
    - [x] Modifier src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/ThirtyTwoX/Hardware/Mega32X.cs : emprunter cette memoire avec SetCartridgeMemory, sans changer sa possession ni les callbacks existants ; actualiser l'emprunt a chaque configuration de contenu.
    - [x] Modifier src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/ThirtyTwoX/Hardware/Mega32X.Bus.cs : mapper directement les plages ROM pour le fetch avec leur ordre d'octets, lire les mots/longs ROM et longs boot en acces de taille native et ecrire les longs SDRAM en une operation en conservant versions et reveils.

- [x] Retirer la variante sans gain mesure
  - [x] Modifier src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Boards/SegaBoard.32X.cs, Machines/ThirtyTwoX/Hardware/Mega32X.cs et Mega32X.Bus.cs : retirer exactement les ajouts ci-dessus, conserver le fetch mappe big-endian preexistant et tous les raccordements 32X anterieurs.
  - [x] Modifier docs/tasks/emulation/segaemu-direct-bus.md : conserver les mesures du parent et signaler la variante rejetee, sans annoncer un gain.

Variante rejetee apres mesure du parent sur le meme passage de Chaotix : Debug 19,19 FPS contre 19,70 ; Release 86,08 FPS contre 90,79. Image identique, aucun gain de vitesse demontre. Les modifications production de cette feuille ont ete retirees exactement ; aucune restauration depuis HEAD, aucun commit et aucun build effectue par cet agent.
