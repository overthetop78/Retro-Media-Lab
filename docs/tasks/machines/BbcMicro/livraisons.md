# Suivi — BbcMicro

- [x] Livraison du bloc BBC conservé.
  - [x] Modifier `docs/chips/CPU/Mos6502.md` avec le résultat de `scripts/local-building.cmd --building=debug --modules=A`, vérifier l'exécutable `build/Debug/Retro Media Lab/retromedialab.exe`, la DLL Chips et chaque module produit, sans commit automatique.

- [x] Livraison du relais 8271 actualisé sur changement.
  - [x] Modifier `docs/chips/Controllers/Intel8271.md` avec le résultat de `scripts/local-building.cmd --building=debug --modules=A`, la vérification de l'exécutable, de Chips dans lib et des 24 modules livrés ; distinguer le build de la mesure native.

- [x] Livraison de la conversion vidéo conservée.
  - [x] Modifier `docs/chips/Video/BbcVideoUla.md` avec le résultat du build Debug All, la présence des 24 modules et DLL Acorn/Chips correspondantes, ainsi que le chemin de l'exécutable livré.
