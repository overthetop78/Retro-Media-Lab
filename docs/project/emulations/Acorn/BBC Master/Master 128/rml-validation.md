# Master 128 — fenêtre RML Debug, 7 novembre 2026

Livraison Debug All vérifiée dans `media-128.md`. L'inventaire Windows ne montrait aucune instance RML avant l'essai. Lancement du fichier `F:\GW GUI\build\Debug\Retro Media Lab\retromedialab.exe`, ouverture Émulation/Acorn/BBC Master 128, puis mise sous tension de la configuration enregistrée existante. Aucun réglage matériel ni média remplacé dans cet essai ; cassette, Floppy0 et HardDisk0 sont présents dans la barre, la cassette indique Aucun média, les indicateurs disquette/HDD sont verts. Ces indicateurs seuls ne décrivent pas les chemins ou formats des images montées.

Le MOS démarre et affiche ACORN MOS, Acorn 1770 DFS, BASIC et son prompt. Affichage Direct3D 11, 640×512, cadence annoncée 50,1 Hz. Relevés successifs visibles : 49,2 FPS / 1,96 MHz / 98 %, puis **50,1 FPS / 2,00 MHz / 100 %**, puis 49,7 FPS / 1,98 MHz / 99 %. Il s'agit d'observations au repos, pas d'une trace continue ni d'une validation du débit pendant un chargement.

La capture native non retouchée est conservée sous `artifacts/TestsMachines/Acorn/BBC Master 128/20261107-rml-master128-idle.png` et `artifacts/TestsChips/Mos65C12/BBC Master 128/20261107-rml-master128-idle.png`. Elle montre le relevé 50,1 FPS / 2,00 MHz / 100 %.

Le contrôle principal @oai/sky renvoyait native pipe unavailable. L'inventaire et le lancement par le contrôle CUA fonctionnent. La première capture CUA montrait la fenêtre Chrome au premier plan avec l'arbre d'accessibilité de RML : aucune action par coordonnées n'a été effectuée depuis cette image contradictoire. L'action Raise explicitement disponible sur la fenêtre RML a permis d'obtenir sa vraie surface. Les clics ont ensuite ouvert la session et allumé la machine ; les arbres d'accessibilité retournés immédiatement après clic restent parfois antérieurs au rendu et sont vérifiés par une capture fraîche avant le clic suivant.

Une tentative de saisie `PRINT 12+34` par typeText et Return n'a produit aucun caractère dans le clavier émulé malgré son focus. Aucun calcul dans RML n'est donc revendiqué. Le BASIC, le calcul 46, les médias et le jeu restent prouvés par les bancs natifs distincts. Les appuis clavier trop brefs du contrôle ne sont pas présentés comme un défaut établi de la machine.

Après conservation de la preuve, fermeture de l'instance possédée dans finally : Alt+F4 n'a pas produit de fermeture observable ; le bouton Fermer de la fenêtre a ensuite été utilisé. L'inventaire suivant ne contient plus RML et aucun processus `retromedialab*` ne reste. Aucun autre logiciel utilisateur fermé. Aucune commande d'écriture média invitée envoyée pendant cet essai ; la fermeture normale peut conserver la mémoire non volatile par le parcours habituel du logiciel.

La validation en charge dans RML et le retour utilisateur restent ouverts, de même que les budgets Debug explicitement consignés. Cette observation confirme la cadence nominale possible au repos dans la livraison ; elle ne clôture pas toutes les conditions d'utilisation du Master.

## Retour utilisateur et passage à la suite

Le 7 novembre, après son essai, l'utilisateur confirme : « la baisse est minime, et c'est en debug, tu peux continuer donc, c'est bon ». Le point de passage Master 128 est accepté ; la légère baisse Debug observée n'est plus bloquante pour continuer au Master Turbo. Les chiffres précédents restent conservés tels que mesurés. Cette décision ne constitue ni une nouvelle mesure, ni une validation de tous les chargements par l'automatisation, ni une clôture des limites spécifiques des autres machines ou contrôleurs.
