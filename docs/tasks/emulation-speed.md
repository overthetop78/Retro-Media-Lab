# Mesures de vitesse communes

## À réaliser plus tard : vitesse dans Emulation

À réaliser plus tard. Le fonctionnement des cœurs reste inchangé. Étendre les contrats existants après examen de leur contenu ; ne pas créer de système parallèle.

- [ ] Fournir des mesures communes utilisables par les modules qui le souhaitent.
  - [ ] Définir les données disponibles sans imposer un compteur interne aux cœurs.
    - [ ] Modifier `src/RetroMediaLab.Emulation/Interfaces/IEmulationRuntime.cs` et les contrats de statut qu'il utilise : capacité facultative décrivant le temps émulé cumulé, la fréquence cible et, si disponible, les cycles cumulés et l'identité du processeur. Employer les contrats propriétaires existants si ces valeurs y sont déjà définies.
  - [ ] Calculer les mesures hors du cœur.
    - [ ] Modifier les services d'exécution existants sous `src/RetroMediaLab.Emulation/Services/` : calculer la progression du temps émulé par seconde réelle, le pourcentage et les MHz effectifs ; identifier mesure directe et valeur calculée ; traiter pauses, arrêt, RESET, changement de fréquence et accélération tape. Ne pas déduire les cycles des seules images présentées.
  - [ ] Raccorder les modules compatibles sans rendre les mesures obligatoires.
    - [ ] Modifier `src/RetroMediaLab.Emulation.Acorn/Common/Services/Machine.cs` et son adaptateur de statut existant : fournir les informations réellement disponibles ; vérifier que le temps fourni inclut toutes les frames internes accélérées. Afficher indisponible si elles ne suffisent pas ; ne pas inventer de valeur.
    - [ ] Modifier les adaptateurs de statut existants sous `src/RetroMediaLab.Emulation.*/` uniquement pour les modules disposant des informations nécessaires ; séparer les processeurs multiples et la vitesse globale de la machine.
  - [ ] Afficher et traduire les mesures.
    - [ ] Modifier `src/RetroMediaLab.App/Functions/Machine/MachinePresentationFunctions.cs` et les présentations de statut des thèmes : distinguer Hz vidéo cible, FPS reçus, vitesse en pourcentage et MHz effectifs du processeur identifié ; conserver l'affichage des autres machines sans mesures.
    - [ ] Modifier les fichiers de langues existants sous `src/RetroMediaLab.App/Resources/` et leur base commune : traduire les libellés dans toutes les langues avec Argos ; conserver MHz et les valeurs invariantes dans la base commune.
  - [ ] Vérifier les calculs et documenter leur portée.
    - [ ] Créer des tests autonomes dans `tests/RetroMediaLab.LocalEmulationTests/Emulation/EmulationSpeedTests.cs` : horloge contrôlée, pauses, compteurs remis à zéro, accélération, changement de fréquence, mesures absentes et processeurs multiples ; adapter le chemin au propriétaire existant des tests communs.
    - [ ] Modifier ce fichier avec les résultats des tests et de l'affichage une fois cette fonctionnalité demandée et réalisée.
