# Master 512 — observation RML Debug du 8 novembre 2026

Prise de main explicitement autorisée pour confirmer les ROM : carte Master 512 / Intel 80186 10 MHz 512 Kio, client 80186 Tube Client 1.01.rom et MOS 3.20 (128K), sélections confirmées dans leurs dialogues puis paramètres refermés. Le catalogue initialement vide finit par se charger ; il ne s'agissait pas d'une absence confirmée de ROM.

L'utilisateur reprend les manipulations ; observation seule, aucun clic ni touche envoyé pendant ce passage. RML affiche Acorn TUBE 80186 512K, Acorn 1770 DFS, BASIC et le curseur clignotant. Sur plusieurs captures espacées, le compteur indique 24,8–25,3 FPS, 0,99–1,01 MHz et 49–50 %, signal vidéo annoncé 50,1 Hz, taille 640 × 512. DOS Plus n'est pas apparu pendant cette observation. Aucun verdict de démarrage DOS ou de validation globale n'est déduit de cette bannière.

Capture originale JPEG du contrôle Windows, sans retouche ni conversion, conservée sous `artifacts/TestsMachines/Acorn/BBC Master 512/20261108-master512-rml-debug.jpg`. Aucun média monté ni commande de démarrage envoyé par l'observateur ; les paramètres des lecteurs n'ont pas été vérifiés dans cette observation. Aucun build ni autre banc exécuté pendant l'essai utilisateur.

## Référence de démarrage pour la reprise

Le chapitre Firmware du Master 512 Technical Guide est archivé dans `artifacts/References/Machines/Acorn/BBC Master 512/m512tech03.htm` ; source https://acorn.huininga.nl/pub/mirrors/ftp.nvg.org/pub/bbc/doc/Master%20512%20Technical%20Guide/m512tech03.htm . Il distingue hard break (chargement DOS) et soft break (moniteur). Les commandes DOS/DOSBOOT du moniteur permettent de relancer le chargement. Le chargeur requiert ADFS pour son premier fichier ; le format DOS Plus 800 ne peut pas servir à cet amorçage. Le guide signale des différences entre versions lors du passage automatique à ADFS : ne pas en déduire que tout écran de démarrage impose une correction CPU ou Tube.

Différence concrète à conserver lors de la reprise : le banc natif initialise le bas de CMOS[19] à 8 ; l'initialisation par défaut du Master dans BbcMicroMachine/MasterClock emploie 9 lorsqu'une interface disquette est présente. La session RML observée affiche DFS. Cette différence de contexte doit être contrôlée pendant l'essai, avant d'attribuer l'absence de DOS au matériel. Aucune modification de configuration CMOS ou de code n'a été effectuée à ce stade dans la session utilisateur.

## Reprise interactive et emplacement des disquettes

Le 8 novembre, l'écran réel affiche ensuite `Drive not ready` et l'invite du moniteur `*`. La commande `ADFS` reste en attente pendant les observations ; Échap produit `Escape` puis rend l'invite. Ce passage ne prouve ni une panne du contrôleur ni le montage de la bonne disquette.

La configuration enregistrée `ce6ad37a3a8749ebba0e37584376bc96/machine.json` contient alors encore `F:\Retro\AMX Super Art Master.ssd`, avec le port HDD 0 connecté mais sans image HDD dans la liste des médias. Il s'agit du fichier enregistré, pas d'une preuve du média actuellement détenu par la session. Le remplacement par la disquette DOS doit être observé avant de poursuivre l'amorçage.

À la demande de l'utilisateur, les six images ont été déplacées du sous-dossier d'archives `8bs-512gemdiscs` vers `F:\Retro`, sans écrasement et avec vérification SHA-256 avant/après : `Gem Disc 1 Early version.640`, `Gem Disc 1 v21.640`, `Gem Disc 1 v21.adl`, `GEM2.800`, `GEM3.800`, `GEM4.800`. Pour l'essai RML, utiliser `F:\Retro\Gem Disc 1 v21.adl` comme disquette de démarrage DOS Plus 2.1. Les chemins d'archives cités dans les passages antérieurs décrivent leur emplacement lors de ces passages ; ils ne sont plus les chemins de montage actuels.

Le dialogue de remplacement est observé dans `F:\Retro` avec un filtre « Support associé » n'affichant que les images SSD/DSD présentes. Le chemin complet ADL a été donné pour le champ « Nom du fichier » ; aucun montage réussi n'est encore observé à ce stade. L'observateur ne clique pas et n'envoie pas de touches pendant les manipulations de l'utilisateur.

La vérification du code explique ce filtre : `StorageFunctions.DriveExtensions` reprend les extensions du modèle de lecteur sélectionné. La configuration enregistrée porte `acornemu.floppy.0.model = ds80`, profil double face 80 pistes simple densité. Dans `Dictionaries/FloppyDriveModels.cs`, seuls les profils double densité incluent les formats ADFS et DOS Plus. Pour cette disquette de démarrage, sélectionner le profil `ds80-dd` avant de la monter ; ne pas ajouter un filtre universel pour contourner la compatibilité physique. `MachineController.InsertMediaAsync` crée actuellement un seul filtre à partir d'AcceptedExtensions : l'option « Tous les fichiers » suggérée pendant l'échange n'existe pas. Aucun code App ou matériel n'a été modifié pour ce constat.
