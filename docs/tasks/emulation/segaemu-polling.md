# Reprise de la detection de polling SH-2 de PicoDrive

- [x] Raccorder les lectures adjacentes sans modifier le format d'etat
  - [x] Modifier src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/ThirtyTwoX/Hardware/Mega32X.Polling.cs : reprendre la fenetre d'adresses de quatre octets de p32x_sh2_poll_detect ; conserver le compteur lors des lectures espacees de deux cycles ou moins, inhiber son increment apres DT via PollingEpoch, retirer PC et valeur des conditions ; conserver les metadonnees serialisees et les reveils sur ecriture/IRQ/video.
  - [x] Creer docs/projects/emulations/Sega/SegaEmu-32X-polling.md : documenter les conditions exactes de la source, le changement et la validation ciblee laissee au parent.
