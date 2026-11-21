# Dispatch direct SH-2 depuis OP_Table Gens

- [ ] Remplacer le second dispatch de l'interpreteur natif
  - [x] Creer `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Sh2.DirectDispatch.cs` : generer depuis les 142 cases du decodeur actuel et ILLEGAL les 143 handlers conservant leurs appels et extractions d'operandes exactement.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Sh2.NativeRuntime.cs` : construire une fois les handlers natifs et une table directe de 65536 entrees, puis remplacer dans la copie native de la boucle l'appel au decodeur classe/switch par l'appel du handler de l'opcode ; conserver le decodeur interprete de reference.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Sh2.NativeRuntime.cs` : conserver la route switch precedente et rendre selectionnables table de delegates et table de pointeurs managed `calli`, sur les memes handlers, avec cache de types par route. Switch reste la route par defaut jusqu'aux mesures.
  - [ ] Validation groupee par le parent : comparer instructions/etats et scenarios Debug puis Release ; ne retenir le dispatch que s'il fonctionne sans regression.

- [ ] Porter la seconde attente active de Gens
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Sh2.Instructions.cs` : reprendre `SH2I_BRAfast2` pour `NOP / BRA -3 / NOP`, conserver le cycle complet de quatre horloges plus les trois attentes de fetch, borner via `IdleIterations` et laisser `AccelerateIdleLoops=false` inchange.
  - [ ] Validation groupee par le parent : comparer ce motif avec la reference non acceleree aux budgets irreguliers, puis aux memes scenarios machine Debug/Release.
