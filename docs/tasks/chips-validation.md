# Index du suivi matériel

Le suivi détaillé a été réparti par machine et par puce. Ne plus ajouter de blocs dans ce fichier.

- [Machines : ordre et suivi par machine](machines/README.md)
- [Puces communes : un dossier par composant](chips/README.md)
- [BBC Micro A](machines/BbcMicroModelA/README.md) — validé par l'utilisateur.
- [BBC Micro B](machines/BbcMicroModelB/README.md) — validé par l'utilisateur.
- [BBC Micro B+ 64/128 Kio](machines/BbcMicroModelBPlus/README.md) — suite en cours.
- [Opérations Git historiques](project/git-history-cleanup.md)
- [Entretien documentaire historique](project/material-documentation.md)

## Référence conservée

Validation complète des puces puis des machines Acorn.

Objectif conservé : puces fidèles et performantes, une à la fois, preuves par fonctions et en émulation, puis machines Acorn restantes et prise en charge des médias dans MediaEngine/MediaFileSystems/MediaAnalysis. Consignes détaillées : texte importé `fa914d36-2c1c-4232-95eb-e5d3376d8505/pasted-text-1.txt` et `docs/prompts-objectifs.md`.

Le déplacement est livré dans `afeceb134`. Le MOS 6502 NMOS déjà traité reste exclu de la nouvelle réécriture ; les variantes CMOS ne sont pas déclarées validées par extension.

Précision de l'utilisateur : traiter entièrement les fonctions documentées ; lorsqu'aucune documentation trouvée ne permet d'établir un comportement, conserver explicitement cette limite et considérer la puce terminée pour l'instant. Ne pas inventer le comportement ni laisser ce manque de source bloquer les autres puces.
