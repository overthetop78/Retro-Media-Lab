# Diagnostic CD32X Corpse Killer

- [x] Confronter le demarrage CD32X aux sources disponibles
  - [x] Creer docs/projects/emulations/Sega/SegaEmu-CD32X-diagnostic.md : comparer bootstrap, registres et echanges observes dans les journaux Corpse Killer aux sources PicoDrive/Gens deja collectees ; distinguer faute prouvee et hypothese, sans annoncer une validation en jeu.
  - [x] Modifier src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/ThirtyTwoX/Hardware/Mega32X.Registers.cs : permettre a une ecriture 68000 de zero dans le controle CMD d'effacer la demande correspondante, comme p32x_reg_write8/p32x_update_cmd_irq de PicoDrive ; ne pas attribuer le demarrage de Corpse Killer a cette seule correction sans essai.
