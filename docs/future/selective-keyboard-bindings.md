# Sélection des touches clavier à réaffecter

Décisions utilisateur du 7 novembre 2026. Réalisation reportée après la finalisation Acorn en cours.

- Chaque machine, reliée à ses émulateurs, fournit par les contrats Emulation existants la liste de ses touches réellement disponibles et leurs associations hôtes par défaut. App ne définit pas le clavier matériel à la place du module.
- Fournir un sélecteur commun à toutes les marques et tous les modules ; ajouter au tableau uniquement les touches que l'utilisateur choisit de modifier.
- Afficher l'association par défaut de la touche ajoutée et permettre sa réaffectation par l'éditeur existant.
- Enregistrer les binds dans la configuration de cette machine ; reconstruire la liste des touches choisies au chargement de cette configuration.
- Retirer une ligne supprime son bind de la configuration. L'absence d'override laisse le défaut de la machine s'appliquer ; ne pas enregistrer artificiellement une réaffectation vers ce défaut.
- Permettre ainsi de modifier quelques touches ou toutes les touches disponibles, sans imposer l'affichage permanent du clavier complet.
- Conserver séparément la correction actuelle de saisie logique : les symboles produits par la disposition hôte doivent parvenir au clavier invité. Ne pas exiger une réaffectation manuelle pour saisir un guillemet ou un point d'exclamation sur AZERTY.

Points existants à réutiliser : `EmulationInputSettings`, `EmulationInputBindingSet`, `InputBindingDefinition`, `EmulationInputSettingsController` et `InputBindingEditor`. Leur présence n'implique pas que tous les modules décrivent déjà l'intégralité de leur clavier : vérifier leurs définitions lors de la réalisation.
