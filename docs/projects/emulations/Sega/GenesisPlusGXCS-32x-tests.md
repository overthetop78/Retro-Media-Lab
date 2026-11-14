# GenesisPlusGXCS — vérification préalable 32X

Vérification locale du 14 novembre 2026. **Campagne de performance non exécutée : le cœur actuel ne prend pas en charge le 32X.**

Les ROM sont présentes sous `G:/Retro/Cartouches/Sega/32X/`, notamment Doom, Knuckles' Chaotix, Virtua Racing Deluxe et After Burner Complete avec extension `.32x`. Le dossier contient aussi des ROM Mega Drive `.gen` : son nom ne suffit donc pas à identifier le matériel des fichiers.

Constats dans le code actuel :

- `SystemHardware.cs` définit SG-1000, Master System, Game Gear, Mega Drive, Pico et Mega-CD, sans matériel 32X.
- `Cpu/` contient les cœurs 68000 et Z80 ; aucune implémentation des deux SH-2 du 32X n'est présente dans GenesisPlusGXCS.
- `SystemCore.RunFrame` sélectionne les boucles Mega-CD, Mega Drive ou SMS, sans boucle 32X. Aucun sous-système vidéo ou PWM 32X n'est présent.
- Dans `LoadRom.cs`, l'extension `.32x` tombe dans le cas par défaut Mega Drive ; réussir un chargement par cette voie ne démontrerait pas une émulation 32X.
- Le README de la source Genesis Plus GX conservée localement énumère Mega Drive, Mega-CD, Master System, Game Gear et SG-1000 parmi les machines prises en charge, sans 32X.

Aucune ROM ni aucun fichier du cœur n'a été modifié. Aucun FPS, pourcentage de vitesse ou screenshot n'est présenté comme résultat 32X. Une campagne comme les précédentes nécessiterait d'abord un cœur disposant du matériel 32X ; son ajout constitue un travail distinct du test demandé.
