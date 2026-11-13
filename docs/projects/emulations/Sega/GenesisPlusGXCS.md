# GenesisPlusGXCS

Portage C# intégral de Genesis Plus GX, indépendant du cœur natif existant.

Sources : [ekeeke/Genesis-Plus-GX](https://github.com/ekeeke/Genesis-Plus-GX), commit `2499cd3e3d1d8b7d0130d8b2825adb4cc608b2c0`, arbre `1ab717d13d2c98f65f5876477ee1b0df6809c89e`. Les 977 fichiers originaux sont conservés séparément dans `artifacts/References/Machines/Sega/GenesisPlusGX/Source/`. `source-manifest.json` enregistre les empreintes Git et SHA-256 ; `source-commit.json` conserve la provenance.

Implémentation : `src/RetroMediaLab.Emulation.Sega/Emulators/GenesisPlusGXCS/`. Les composants propres au portage restent dans ce dossier ; les puces originales de Chips ne sont pas modifiées.

Périmètre : tous les modèles et fonctions de la version source, dont SG-1000/II, SC-3000, Mark III, Master System/II, Game Gear, Mega Drive/Genesis, Power Base Converter, Pico et Mega-CD, avec leurs périphériques, cartouches, sauvegardes et traitements audio/vidéo.

État : les composants du cœur sont écrits et le module Sega compile en Debug. `GenesisPlusGXCS` possède son propre adaptateur, ses constantes, ses options et ses identifiants de ressources/firmwares ; il réutilise les contrats et services communs Sega/Emulation. L'intégration de présentation et les traductions restent en cours.

À la demande de l'utilisateur, un premier contrôle SG-1000 a été anticipé : six ROM de 8 à 48 Kio, 180 trames chacune, sans exception, à 119,70–134,86 FPS en Debug. Cinq captures montrent un titre ou une scène reconnaissable ; Bomberman Special produit une image grise. Les allocations sont élevées et le profil détaillé des composants reste à établir. Voir [les résultats et leurs limites](GenesisPlusGXCS-performance.md). Ce contrôle ne valide ni la compatibilité complète des jeux ni la fidélité des autres machines ; le portage intégral reste en cours.
