# OSD pour l’émulation en plein écran

Spécification en cours. Cette fiche réunit les décisions sur les menus et les pistes précédemment notées pour l’affichage des informations d’émulation.

## Menu principal

L’OSD fonctionne uniquement en plein écran. Le menu principal reste à gauche par-dessus le jeu. Un seul sous-menu s’affiche à droite. Ouvrir un niveau remplace le contenu du panneau droit. Un retour réaffiche le niveau précédent avec sa sélection, puis ferme le panneau droit en revenant au menu principal. La couche OSD est indépendante du rendu du jeu et s’affiche après le traitement vidéo.

Les trois captures de la discussion servent de référence visuelle.

1. Menu principal seul, avec le nom de la machine et du jeu.
2. Menu principal avec le sous-menu **Sauvegarde / Chargement**.
3. Menu principal avec le sous-menu **Réglages audio**.

Les images originales restent dans la discussion. Leur copie dans `artifacts` reste à faire à partir des fichiers originaux.

Captures prises dans RML en plein écran

- [Menu principal sur Sonic 2](../../artifacts/osd/osd-menu-principal.png)
- [Sous-menu Sauvegarde / Chargement](../../artifacts/osd/osd-sauvegarde-chargement.png)
- [Sous-menu Réglages audio](../../artifacts/osd/osd-reglages-audio.png)
- [Indication du chargement de Lanczos](../../artifacts/osd/osd-shader-lanczos-chargement.png)
- [Réglage de la mémoire Axlon sur Atari 800](../../artifacts/osd/osd-atari-axlon.png)

### Ouverture et entrées

- **Ouvrir l’OSD avec pause** appelle la commande Pause de RML si l’émulation est démarrée. Fermer le menu reprend l’émulation par la commande Reprendre de RML.
- **Ouvrir l’OSD sans pause** laisse l’émulation tourner.
- Les deux actions figurent dans le tableau des touches de RML. Chaque action d’ouverture ou de navigation dispose d’une association principale et d’une association alternative. Chacune accepte les mêmes entrées du clavier de l’hôte, de la souris, d’une manette ou d’un clavier MIDI. Une combinaison peut mélanger plusieurs périphériques, par exemple une touche clavier, un bouton de souris et un bouton de manette. L’ouverture doit consommer le raccourci côté RML, y compris quand la machine émulée utilise un clavier.
- Les raccourcis principaux par défaut sont `Ctrl+Maj+/` au pavé numérique avec pause et `Ctrl+Maj+*` au pavé numérique sans pause. Les mêmes raccourcis ferment l’OSD lorsqu’il est ouvert. Les quatre flèches, Entrée et Échap sont les associations principales de navigation. Les associations alternatives sont libres par défaut.
- Une entrée liée à un périphérique précis se configure après le choix de ce périphérique. Aucune combinaison manette n’est préaffectée.
- L’OSD reçoit le clavier, la manette et la souris tant qu’il est ouvert. La souris peut cliquer directement dans le menu. À la fermeture, l’émulation récupère immédiatement les nouvelles entrées. Les touches et boutons maintenus restent filtrés jusqu’au relâchement. Le filtrage des axes de manette prend fin au retour dans la zone neutre ou au changement de direction.

### Navigation et édition

| Action | Navigation | En édition |
| --- | --- | --- |
| Haut / Bas | Déplacer la sélection | Selon le contrôle |
| Gauche / Droite | Revenir au niveau précédent ou ouvrir le sous-menu | Ajuster la valeur, par exemple un curseur |
| Valider | Ouvrir le sous-menu, activer la commande ou entrer en édition | Enregistrer la valeur et quitter l’édition |
| Annuler | Remonter d’un niveau, puis fermer l’OSD depuis le menu principal | Abandonner la modification sans enregistrer et revenir à la valeur d’avant l’édition |

La luminosité, le contraste et la couleur s’appliquent en direct à la dernière trame en pause ou aux trames en cours si l’émulation tourne. Un changement de shader démarre uniquement à la validation. Dès que le shader est prêt, il s’applique à l’image courante, même en pause.

En plein écran, l’indication de chargement du shader est affichée par l’OSD, avec le panneau de chargement habituel masqué. L’indication conserve le rendu de l’interface indépendamment du shader appliqué au jeu.

### Réglages en plein écran

- **Vidéo** présente les groupes Affichage, Image et Effets. Le bloc Traitement vidéo Retro Media Lab regroupe le moteur de rendu, l’échantillonnage et la technologie d’affichage. Les réglages propres à la technologie choisie et ceux déclarés par le module restent dans leurs blocs.
- **Audio** présente les blocs déclarés par le module, dont la sortie hôte et les mélangeurs disponibles. Valider enregistre le réglage. L’activation du son et les options que le moteur accepte pendant la session prennent effet dès la validation. Les autres options s’appliquent au prochain démarrage de l’émulation.
- **Paramètres de la machine** présente les groupes du module avec leurs informations, sélecteurs, interrupteurs et curseurs. Les champs qui nécessitent une boîte de dialogue, comme la sélection d’un BIOS, restent accessibles dans les réglages en mode fenêtré.
- Les groupes suivent la visibilité déclarée par le module. Si cette visibilité change, l’OSD ferme les panneaux qui en dépendent. Les règles d’exclusion mutuelle et de visibilité conditionnelle s’appliquent dans les deux modes. Sur Atari 800, choisir Axlon désactive Mosaic et affiche le réglage du miroir de banque.

### Accès rapide aux lecteurs

Un raccourci configurable ouvre un OSD distinct avec un navigateur de fichiers pilotable à la manette, au clavier et à la souris. Il permet de choisir une image pour un lecteur sans quitter le plein écran.

Un seul menu OSD peut être ouvert. Ouvrir les Lecteurs ferme le menu principal et abandonne toute édition en cours. Ouvrir le menu principal ferme les Lecteurs et abandonne la sélection de fichier en cours. Les entrées passent directement au menu qui s’ouvre.

Le bandeau des Lecteurs aura d’abord un emplacement fixe en haut ou en bas. Le placement personnalisable et le choix de l’emplacement de l’onglet OSD sont reportés. D’autres accès rapides programmables pourront être définis ensuite.

### Ordre de réalisation

1. Afficher un panneau latéral coloré et translucide, vide pour commencer. Raccorder les deux raccourcis à la fenêtre plein écran et au tableau des touches. Vérifier l’ouverture, la fermeture, la pause, la reprise et le transfert d’entrées.
2. Remplir le menu progressivement, puis ajouter les sous-menus et la navigation au clavier, à la manette et à la souris.
3. Tester la modification, la validation et l’annulation des réglages quand le sous-menu vidéo et ses éventuels niveaux supplémentaires seront en place. Vérifier le rendu de la dernière trame et l’indicateur de chargement du shader.
4. Reprendre l’OSD Lecteurs plus tard. La pause à son ouverture et le comportement après l’insertion d’une image seront définis à ce moment.

## Affichage des informations d’émulation

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

## Réglages envisagés pour l’affichage des informations

Regrouper ces réglages dans un onglet **OSD** de la partie **Émulation** : choix de la présentation minimale ou complète, informations visibles, affichage des lecteurs, position et transparence.

Cette proposition concerne l’affichage des informations. L’organisation des réglages du menu interactif et la portée des préférences, commune ou propre à une machine, restent à décider.

## Autre possibilité à étudier

Afficher brièvement dans l’OSD les touches de fonction appuyées. Cette possibilité n’est pas encore arrêtée ; son contenu et sa présentation restent à définir.

## Intégration à prévoir lors de la réalisation

Conserver les fonctions existantes de l’émulation et de VideoPresentation. L’OSD sera une présentation de leurs informations et commandes, commune aux machines et utilisable avec les thèmes et moteurs de rendu pris en charge.

Lors de la réalisation, traiter les libellés dans toutes les langues en conservant les unités et autres valeurs invariantes dans la base commune.
