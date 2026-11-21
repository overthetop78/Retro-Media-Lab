# Generation globale des modifications du code SDRAM 32X

- [x] Fournir la barriere d'ecriture utilisee par le SH-2
  - [x] Modifier src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/ThirtyTwoX/Hardware/Mega32X.cs : ajouter un compteur SDRAM derive non serialise et l'incrementer au reset et apres la copie IPL de demarrage.
  - [x] Modifier src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/ThirtyTwoX/Hardware/Mega32X.Bus.cs : exposer CodeEpoch non nul pour boot/ROM/SDRAM, nul pour les regions non suivies ; incrementer le compteur sur chaque ecriture SDRAM sans changer les versions de ligne existantes ni les horloges.
  - [x] Modifier src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/ThirtyTwoX/Hardware/Mega32X.State.cs : invalider cette generation lors d'une restauration SDRAM sans modifier la taille ou le format du contexte.

- [x] Limiter la barriere SMC aux mots de code effectivement traduits
  - [x] Modifier src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/ThirtyTwoX/Hardware/Mega32X.cs et Mega32X.Bus.cs : ajouter un bitset derive de 131072 mots SDRAM, le remplir par MarkCode et n'incrementer CodeEpoch lors des ecritures ordinaires que si un mot marque est effectivement modifie ; conserver toutes les versions de ligne et tous les reveils.
  - [x] Modifier src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/ThirtyTwoX/Hardware/Mega32X.State.cs : vider les marqueurs apres restauration des deux caches CPU ; les vider aussi apres reset/demarrage des deux CPU sans les serialiser.

- [x] Exposer la borne superieure des attentes du bus 32X
  - [x] Modifier src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/ThirtyTwoX/Hardware/Mega32X.Bus.cs : fournir MaximumWaitStates egal a 6, borne exacte du switch WaitStates existant (ROM 6, framebuffer 4 en lecture, SDRAM 2 en lecture, autres 0), sans modifier les attentes appliquees.
