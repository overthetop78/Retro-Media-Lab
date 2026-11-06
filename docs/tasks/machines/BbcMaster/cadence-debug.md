# BBC Master — cadence Debug dans RML

- [ ] Résoudre le ralentissement signalé le 6 novembre, puis retester les machines séparément.
  - [ ] Établir la cause sur une session réelle, sans autre banc ni compilation concurrente.
    - [x] Modifier ce fichier avec les relevés du Master dans le build Debug All, la configuration réellement ouverte, les FPS/MHz, le rendu et l'état du focus ; comparer avec les mesures de carte complète déjà consignées dans `docs/chips/IO/TubeUla.md`.
    - [x] Créer `artifacts/.tmp-tube/Modes.cs` : reprendre le banc de carte complète et mesurer les MODE BASIC 0 à 7 en Debug, avec mêmes ROM/équipements, cycles et sorties ; ajouter les résultats à ce document avant de désigner une cause.
    - [ ] Créer les traces temporaires sous `artifacts/.tmp-tube/` : profiler d'abord le même banc complet, puis la session RML possédée, distinguer calcul du cœur, publication vidéo/audio et attente de cadence ; fermer et attendre les processus possédés dans `finally`, sans arrêter une session utilisateur.
    - [ ] Supprimer `artifacts/.tmp-tube/Modes.cs`, les traces et fichiers dérivés temporaires après consignation des résultats ; reconstruire le banc sans ce diagnostic avant suppression de son répertoire complet à la clôture du Tube.
    - [ ] Modifier ce fichier avec les causes établies et les fichiers exacts à corriger avant de réaliser ces corrections ; ne pas changer une puce ni un timing pour compenser un coût de présentation ou d'attente.
  - [ ] Valider les corrections et consigner chaque machine individuellement.
    - [ ] Modifier ce fichier et `docs/tasks/machines/BbcMaster/README.md` avec les résultats Master, les médias/logiciels utilisés et les captures réelles ignorées par Git ; conserver les problèmes non résolus ouverts.
    - [ ] Modifier les suivis `docs/tasks/machines/BbcMicroModelA/README.md`, `BbcMicroModelB/README.md`, `BbcMicroModelBPlus/README.md`, `AcornSystem/README.md`, `AcornAtom/README.md` et `AcornElectron/README.md` avec le passage Debug séparé de chaque modèle et ses fréquences matérielles configurées, sans extrapoler la validation d'un modèle aux autres.

## Signalement utilisateur

Environ 1,50 MHz au lieu de la vitesse attendue sur BBC Micro A, B, B+, Master, System 1 à 5, Atom et Electron. Les paramètres et fréquences de chaque modèle restent à relever dans RML ; le symptôme commun ne suffit pas à désigner une puce ou le service de cadence.

Les mesures se concentrent désormais sur Debug. Les passages Release déjà acquis restent documentés ; aucun contrôle Release répétitif pendant le diagnostic.

## Observations dans RML

Build Debug All du 6 novembre livré avec code 0, exécutable et 24 manifestes de modules présents. Pendant les manipulations de l'utilisateur sur le Master 128 : MOS 3.50, sans coprocesseur, cassette/disquette/HDD configurés ; écran BASIC 640 × 512 affichant 50,0–50,2 FPS, 2,00–2,01 MHz, 100 %. Les commandes `*HELP` puis `*DISC` sont visibles. Après passage en 640 × 256, l'écran est noir et les compteurs affichent 30,0 FPS, 1,20 MHz, 60 %. Aucune compilation ni aucun banc parallèle n'est alors lancé. L'utilisateur confirme le même symptôme sur les autres machines ; elles n'ont pas encore été reproduites individuellement dans ce passage.

Les anciennes mesures « redessin » du banc Tube exécutent PRINT en mode télétexte : elles ne couvrent pas ce changement vers un mode graphique. Il faut donc ajouter cette comparaison au lieu d'utiliser le résultat télétexte pour valider tous les modes.

## Carte complète en Debug — modes BASIC

Sans interface, sans compilation concurrente ; même carte Master sans coprocesseur, ROM MOS 3.50, WD1770 et interface Winchester, rendu vidéo et drainage audio inclus. 120 images d'amorçage ; commande MODE pour chaque cas, 10 images de chauffe puis 80 images chronométrées. La session RML est arrêtée pendant le banc (CPU du processus pratiquement inchangé entre les deux relevés).

| Mode | Sortie | ms/image |
| --- | --- | ---: |
| 7 | 640 × 512 | 18,321 |
| 0 | 640 × 256 | 21,076 |
| 1 | 640 × 256 | 21,322 |
| 2 | 640 × 256 | 21,243 |
| 3 | 640 × 256 | 21,110 |
| 4 | 640 × 256 | 18,131 |
| 5 | 640 × 256 | 18,169 |
| 6 | 640 × 256 | 19,429 |

Chaque scénario exécute environ 3 194 880 cycles hôte sur les 80 images ; les variations de quelques cycles proviennent de la limite d'instruction. Les MODE 0–3 dépassent déjà le budget matériel dans la carte seule. Le jeu observé n'est pas reproduit par ces écrans BASIC statiques : ces valeurs ne suffisent pas à attribuer son coût supplémentaire ni à annoncer la cause exacte.
