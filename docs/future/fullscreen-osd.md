# Idée future : OSD pour l’émulation en plein écran

Statut : idée à conserver pour plus tard. Aucune implémentation n’est demandée pour le moment.

## Objectif

Proposer un affichage superposé à l’écran d’émulation en plein écran, afin de voir les mesures et l’activité des lecteurs sans quitter le jeu ou la machine émulée. Cette fonctionnalité concerne l’émulation en général, pas seulement Acorn.

## Informations affichées

- FPS de l’émulation.
- MHz effectifs, lorsque cette mesure est disponible.
- Activité de lecture et d’écriture des lecteurs : disquettes, tapes, disques durs, disques optiques et autres médias pris en charge par la machine.
- Pour les lecteurs, choix entre le nom accompagné d’une LED et de l’icône du média, ou une présentation réduite avec seulement la LED et l’icône du média.

Réutiliser les mesures et les états d’activité déjà disponibles dans l’émulation, sans créer de compteurs concurrents ni inventer les informations absentes d’un module.

## Présentations

Prévoir un OSD minimal et un OSD complet. Le contenu exact de chaque présentation sera défini lors de la conception.

Deux formes sont envisagées :

- Un cadre semi-transparent, positionnable dans chacun des quatre coins de l’écran hôte.
- Un bandeau en haut ou en bas de l’écran hôte.

L’utilisateur pourra choisir le niveau de transparence du cadre. La disposition devra rester lisible avec plusieurs lecteurs et tenir dans l’écran hôte.

## Menu OSD

Envisager un menu OSD pour accéder aux réglages des shaders vidéo et du son pendant le plein écran.

L’emplacement, l’ouverture et la navigation de ce menu restent à décider. Ne pas déduire leur fonctionnement des positions proposées pour le cadre d’informations.

## Réglages dans l’application

Regrouper ces réglages dans un onglet **OSD** de la partie **Émulation** : choix de la présentation minimale ou complète, informations visibles, affichage des lecteurs, position et transparence.

Les réglages du futur menu OSD seront précisés avec sa conception. La portée des préférences, commune ou propre à une machine, reste à décider.

## Autre possibilité à étudier

Afficher brièvement dans l’OSD les touches de fonction appuyées. Cette possibilité n’est pas encore arrêtée ; son contenu et sa présentation restent à définir.

## Intégration à prévoir lors de la réalisation

Conserver les fonctions existantes de l’émulation et de VideoPresentation. L’OSD sera une présentation de leurs informations et commandes, commune aux machines et utilisable avec les thèmes et moteurs de rendu pris en charge.

Lors de la réalisation, traiter les libellés dans toutes les langues en conservant les unités et autres valeurs invariantes dans la base commune.
