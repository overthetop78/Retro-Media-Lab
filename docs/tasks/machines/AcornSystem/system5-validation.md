# Acorn System 5 — carte rapide 6502A et VDU80

- [x] Terminer le montage System 5 et la livraison groupée.
  - [x] Corriger le décodage DRAM établi par le manuel.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Functions/SystemFunctions.cs` : déplacer les 48 Kio DRAM de 0000–BFFF vers 2000–DFFF ; préserver RAM CPU et I/O.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Common/Machines/AcornSystem/Constants/ModelConstants.cs` : total 48 Kio DRAM + 1 Kio RAM CPU.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemRamIoTests.cs` : vérifier limites DRAM, BASIC C000, trou hors RAM, total configuré, sans ROM externe.
  - [x] Vérifier la carte complète et terminer le banc commun.
    - [x] Modifier `artifacts/.tmp-system3/Program.cs`, option --system5 : vrais firmwares, catalogue, BASIC chargé/exécuté, SAVE/LOAD, CAT1 sur second port, mesure globale Debug et captures ; libération en finally.
    - [x] Créer `artifacts/TestsMachines/Acorn/AcornSystem/systems-final-debug.trx` par les tests System existants et la régression DRAM ajoutée, contrôler les résultats.
    - [x] Supprimer `artifacts/.tmp-system3/` après vérification du chemin et consignation ; conserver captures, archives sources et médias de référence.
  - [x] Livrer la famille pour l'essai groupé demandé.
    - [x] Créer les fichiers de `build/Debug/Retro Media Lab/` par `scripts/local-building.cmd --building=debug --modules=A` hors bac à sable ; vérifier exécutable, Chips et les 24 modules.

- [x] Vérifier System 5 dans RML Debug.
  - [x] Vérifier le montage VDU80 et les logiciels invités.
    - [x] Créer les captures `artifacts/TestsMachines/Acorn/Acorn System 5/20261108-rml-*` : NOS80/VDU80, lecteurs ss80, catalogue et BASIC chargé depuis la disquette, calculs clavier/pavé, second port et cadence de 2 MHz ; distinguer le signal progressif de la VDU40 et conserver l'image de référence.

- [x] Compléter les sons des Acorn System après les essais de la famille.
  - [x] Vérifier le raccordement existant des lecteurs et les directives applicables.
    - [x] Modifier les fichiers audio et de raccordement Acorn System concernés seulement si la comparaison établit un manque : compléter uniquement les sons manquants, déclenchés par les événements, avec volumes et muet effectifs ; consigner les vérifications. Le contrôle ne révèle pas de manque de raccordement des lecteurs : aucune modification de production justifiée.
    - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuAudioTests.Systems.cs` : contrôler sans firmware externe les canaux Audio effectivement présents pour System 1–5 et la variante 6809, les valeurs initiales et les commandes volume/muet transmises à chaud aux mécaniques des lecteurs ; vérifier l'absence de reconfiguration pendant les ticks internes.
    - [x] Créer `artifacts/TestsMachines/Acorn/AcornSystem/system-audio-audit-debug.trx` par les tests System et Audio utiles, après le bloc ; conserver les tests autonomes et vérifier les résultats.

Contrôle du 8 novembre après essais System 3–5 : 82 tests Debug réussis, zéro échec, dont 10 nouveaux cas autonomes. Les deux premiers échecs provenaient de la fixture qui raccordait à tort des disquettes aux System 1/2 ; fixture corrigée, sans changer la validation de production. Les canaux suivent les équipements réellement présents ; chaque volume initial vaut 10. Les commandes volume/muet atteignent séparément les deux mécaniques sur System 3 (6502 et 6809), 4 et 5, sans reconfiguration pendant 1 000 clocks de carte.

Les raccordements existants ont été suivis : `EmulatorCore.Initialize` branche `TapeTransport.ActivityChanged`, `EmulatorCore.TapeSound.cs` transmet les changements au lecteur hôte, `EmulatorCore.Disks.cs` transmet les mécaniques aux images et `Intel8271Drive.Synchronize` émet moteur/accès/seek aux changements de sorties. Dix fichiers FLAC Tape et disquette attendus sont présents dans le build livré. Aucun manque de raccordement des lecteurs n'est établi par ce contrôle ; aucun son fictif n'est ajouté. Le son propre au boîtier demandé reste à préciser avec l'utilisateur, et l'écoute physique de chaque lecteur n'est pas prétendue vérifiée par ces tests.

Les deux cas existants `TapeAndHardDiskHotVolumesNotifyOnlyOnCommandsAndPreserveChipPcm` passent aussi en Debug, rapport `system-audio-common-options-debug.trx` : volume/muet communs ne réinjectent pas de mécanique dans le PCM invité.

- [x] Déterminer et compléter les sons propres aux machines demandés.
  - [x] Identifier le son attendu et son matériel avant toute implémentation.
    - [x] Compléter `docs/project/emulations/Acorn/Acorn System/System 5/validation.md` avec la réponse sur le bruit de boîtier/ventilateur ou signal électronique attendu, les modèles concernés et le raccordement réel à réutiliser ; ajouter les actions de production précises si un manque est établi.

- [x] Confronter la synthèse de famille aux preuves actuelles.
  - [x] Vérifier le rendu mécanique existant sans nouveau banc.
    - [x] Créer `artifacts/TestsMachines/Acorn/AcornSystem/system-mechanical-render-debug.trx` par le test existant `DriveVolumeIsAHotOptionAndMechanicalPlaybackDoesNotAlterChipPcm` : silence au volume zéro, PCM mécanique non nul au volume actif, PCM invité inchangé et arrêt du bruit après Stop.
  - [x] Retirer les constats périmés de la synthèse.
    - [x] Modifier `docs/tasks/machines/AcornSystem/implementation.md` et `README.md` : remplacer le constat ancien d'INS8154 limité aux ports statiques par le lien vers la puce complète effectivement réutilisée, et relier les preuves actuelles System 1–5 et 6809 sans clore le signalement sonore restant.

  - [x] Conserver la référence du matériel sonore et la directive clarifiée.
    - [x] Créer `artifacts/.tmp-archives/machines/Acorn/Acorn System 1/System1-UsersManual.pdf` depuis le manuel Acorn déjà consulté ; consigner sa page 15 (page PDF 16) et le haut-parleur externe à ajouter pour le métronome, distinct d'un buzzer intégré.
    - [x] Modifier `docs/directives/emulation-development.md` et la fiche `docs/project/emulations/Acorn/Acorn System/System 5/validation.md` : seuls les sons électroniques produits par un buzzer/haut-parleur réellement monté sont attendus ; sans ce matériel, conserver le silence. Aucun bruit de ventilation ou de boîtier demandé.

- [x] Contrôler le silence de lecture disquette signalé par l'utilisateur.
  - [x] Suivre une lecture invitée complète jusqu'à la sortie audio hôte existante.
    - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/SystemMechanicalHostProbe.cs` temporairement : reprendre la configuration System 5 et son firmware, monter la disquette de référence en lecture seule, exécuter CAT par le clavier invité, observer les notifications des mécaniques et le PCM remis au contrôleur audio App existant avec une sortie mémoire ; fermer le lecteur audio et le cœur en finally.
    - [x] Créer `artifacts/TestsMachines/Acorn/Acorn System 5/20261108-mechanical-host-probe.log` et le rapport Debug du diagnostic : nombre de lectures physiques, notifications, blocs et crête PCM réellement remis à la sortie ; identifier le premier maillon manquant avant correction.
    - [x] Modifier le diagnostic `SystemMechanicalHostProbe.cs` pour déléguer la sortie mesurée au `WasapiAudioOutput` existant et vérifier la consommation réelle du tampon Windows au volume configuré, sans capture d'autres sons du PC ; libérer la sortie dans finally.
    - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/SystemMechanicalHostProbe.cs` après consignation ; régénérer les sorties des tests sans le diagnostic et conserver seulement les preuves ignorées par Git. Compilation Debug sans dépendances terminée avec zéro erreur après retrait.
    - [x] Créer `artifacts/TestsMachines/Acorn/Acorn System 5/20261108-rml-audio-read-*` : catalogue puis chargement BASIC dans RML, volume 10. L'utilisateur confirme « oui jai entendu » après le chargement et demande une opération plus longue ; le formatage ci-dessous est réalisé à sa demande. Instance possédée fermée, processus absent après fermeture. Cette confirmation porte sur le chargement, pas sur l'écoute du formatage.

- [x] Formater une disquette vierge dans le lecteur 1 à la demande de l'utilisateur.
  - [x] Préparer le média et l'utilitaire réels sans modifier le disque source.
    - [x] Créer `F:/Retro/Acorn System/System-format-test.ssd` par le dialogue de création existant de Floppy1, simple face 80 pistes, et la monter dans ce lecteur.
    - [x] Créer si nécessaire le média d'utilitaires Acorn sous `F:/Retro/Acorn System/`, à partir de la source déjà référencée, pour charger le programme FORM80 officiel depuis Floppy0. `DOS Utilities.ssd` monté ; `DRIVE1`, puis `FORM80` affichent la confirmation du lecteur 1.
  - [x] Exécuter et vérifier le formatage invité.
    - [x] Modifier uniquement `F:/Retro/Acorn System/System-format-test.ssd` par FORM80 dans le System 5 émulé ; créer les captures `artifacts/TestsMachines/Acorn/Acorn System 5/20261108-rml-format-drive1-*` montrant résultat Verified, pistes 00–4F et catalogue vide après formatage. Retour sonore humain encore attendu ; ne pas le confondre avec la réussite de FORM80.
  - [x] Contrôler l'attente observée après YES avant de déclarer le formatage terminé.
    - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/SystemFormatProbe.cs` temporairement : préparer le relevé de l'attente avec NOS80. Diagnostic abandonné avant exécution : erreur de compilation du nom de type Intel8271, puis le vrai FORM80 termine dans RML ; aucun relevé ni correctif matériel ne peut être attribué à ce banc.
    - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/SystemFormatProbe.cs` après diagnostic et consigner le résultat établi dans la fiche System 5. Le programme réel affiche Formatted puis Verified, pistes 00–4F ; aucune modification de production.
  - [x] Contrôler la persistance du formatage après libération du média.
    - [x] Créer `artifacts/TestsMachines/Acorn/Acorn System 5/20261108-format-persistence.json` depuis le fichier SSD fermé : taille 204800, catalogue sans fichier, capacité 800 secteurs, 798 secteurs de données remplis E5 et empreinte de l'image. Contrôles réussis ; processus RML absent.

## Reprise du 9 novembre — VDU80 matérielle raccordée

Trajet latch/ROM/sérialiseur et RC du curseur réalisés, raccordés et vérifiés : 44 contrôles de composants et 185 contrôles de carte/disque/audio réussis après nettoyage. Essai natif actuel : NOS80, catalogue du SSD, chargement BASIC exact et calcul 46, captures réellement inspectées du passage 09:42–09:46. Détails, empreintes et tableau complet dans `docs/project/emulations/Acorn/Acorn System/System 5/validation.md` ; captures sous TestsMachines et TestsChips/Vdu80CharacterGenerator, hors Git.

Mesure Debug complète actuelle : 115,8–116,0 ms/champ, 8,62–8,64 champs/s sans limite, environ 0,344 MHz émulé et 17,2 % des 2 MHz réels ; vidéo matérielle 50,080128 Hz et ratio source/CPU 12 conservés. Le réseau 24 MHz et le trajet série coûtent déjà trop indépendamment du CPU. La machine demeure ouverte pour optimisation ; les anciennes mesures au-dessus de 100 % ou vers 26 % ne couvraient pas ce trajet complet.

### Mesure actuelle après publication série — 9 novembre 2026, passage 10:17

System 5 natif sans RML, NOS80 et VDU80 externes déjà identifiés, 48 Kio RAM 2000–DFFF, 8271 présent, aucun disque monté. 40 champs de chauffe puis trois fenêtres de 80 champs, RunFrame et drainage audio inclus, sans compilation/test concurrent. Ce passage mesure le moniteur ; il ne répète pas l'essai BASIC/disquette déjà effectué.

| Fenêtre | ms/champ | Champs/s sans limite | CPU MHz émulés | Vitesse réelle | Périodes source | Cycles CPU |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 114,775827 | 8,712636 | 0,347948 | 17,397391 % | 38 338 560 | 3 194 880 |
| 2 | 116,842544 | 8,558526 | 0,341793 | 17,089666 % | 38 338 560 | 3 194 880 |
| 3 | 114,562517 | 8,728858 | 0,348596 | 17,429785 % | 38 338 560 | 3 194 880 |

Cadence physique 50,080128 Hz, ratio source/CPU exactement 12. La publication série apporte 6,9–10,7 % sur son montage synthétique mesuré, mais ce passage complet reste proche du précédent 17,2 % : aucun gain global significatif n'est établi par cette comparaison, et aucun 100 % n'est annoncé. Les trois compteurs 24 MHz et le trajet vidéo conservent leur activité complète. Les empreintes de framebuffer diffèrent entre fenêtres ; ce passage ne vérifie pas leur égalité entre champs. La capture finale est inspectée : moniteur « Acorn Dos », prompt * lisible.

Capture réelle `20261109-101751-serial-publication-monitor-debug.bmp`, SHA256 du fichier 162FD4E97CB8FF4D3D07BE20A79393C087D912C1559AEFE60DB9F951DFB9DA12, sous TestsMachines/Acorn/Acorn System 5 et copie TestsChips/Sn74LS165/Acorn System 5. Aucun RML ni ressource graphique native créé ; cœur réinitialisé et bande éjectée dans finally. La performance complète reste ouverte.

Nettoyage terminé : banc temporaire supprimé, assembly régénérée ; 131 contrôles System/audio réussis (3 s), sans sauvegardes reportées. Aucun build de livraison utilisateur lancé à ce stade.

### Profil actuel de la machine complète — 9 novembre 2026

Profil EventPipe de huit secondes du moniteur NOS80 sur la machine complète après les optimisations de publication série et de ROM. Même RAM 2000–DFFF, VDU80 et 8271 sans média, rendu et drainage audio inclus ; 40 champs de chauffe et 240 champs exécutés. Compteurs vérifiés : 9 584 640 cycles de référence, 115 015 680 périodes source (ratio 12), empreinte framebuffer E9E458B2454C6AAD008F6EA0A02D78D406A9EBC80DA45D7057462F60FA99E76B. Le profil est instrumenté : aucune nouvelle vitesse nominale n'est déduite de sa durée.

Piles evented intégrées par intervalles ; attribution au dernier appelant réel de Chips ou du module, appels runtime inclus, sans additionner les coûts propres et inclusifs. Total attribué 8024,85 ms. Parts propres : SN74S163.PublishOutputs 9,52 %, SystemClockGenerator.PublishOutputs 8,71 %, VDU80.SetDotClock 6,69 %, SN74S163.NextCount 6,45 %, SystemVideo.SetDotClock 4,21 %, générateur.RisingClock 3,89 %, compteur.CaptureRisingClock 3,82 %, SN74LS165.ResolveGatedClock 3,74 %, compteur.ResolveClock 3,04 %, générateur.Ic2Outputs 2,58 %, compteur.get_Carry 2,28 %, générateur.Ic4Outputs 2,14 %. CPU ClockNmos 1,28 %, ExecuteNmosPhase 1,25 %, 8271Drive.Tick 1,17 %, TickReferenceClock 0,85 %. Les coûts faibles ne prouvent pas l'absence de défaut ; ils orientent l'optimisation.

Le coût principal reste donc le réseau des trois compteurs et de ses publications, pas le CPU. La publication du compteur calcule le RCO par un accesseur et compare deux champs séparés de dernière publication ; une représentation directe des broches publiées sera évaluée dans son suivi, en gardant le compte et la phase privés de chaque compteur. Le framebuffer représente 4,21 % dans ce scénario : il n'explique pas seul la lenteur. Aucun mode matériel ni front n'a été supprimé, aucun dynarec ajouté.

### Gain natif du bloc critique — 9 novembre 2026, passage 10:48

Après optimisations du compteur, de son destinataire direct et du routage des sorties, même NOS80/VDU80 externes, RAM 2000–DFFF, 8271 présent sans média, 40 champs de chauffe puis trois fenêtres de 80 champs. Debug normal, rendu et drainage audio inclus, aucun test ni compilation concurrent.

| Fenêtre | ms/champ | Champs/s sans limite | CPU MHz émulés | Vitesse réelle | Périodes source | Cycles CPU |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 102,139359 | 9,790545 | 0,390995 | 19,549760 % | 38 338 560 | 3 194 880 |
| 2 | 102,206670 | 9,784097 | 0,390738 | 19,536885 % | 38 338 560 | 3 194 880 |
| 3 | 101,742130 | 9,828770 | 0,392522 | 19,626088 % | 38 338 560 | 3 194 880 |

Face au passage précédent à 114,775827 / 116,842544 / 114,562517 ms/champ, durée moyenne réduite d'environ 11,6 %. Ce passage inclut aussi la réduction des recalculs ROM : ne pas attribuer le gain entier au seul routage. Cadence matérielle 50,080128 Hz et ratio source/CPU exactement 12. Les trois empreintes de framebuffer sont identiques à celles du passage précédent : E9E458B2454C6AAD008F6EA0A02D78D406A9EBC80DA45D7057462F60FA99E76B, 113FD6C47E8C8E57D4731313BFC2BDCFA42DF867D1FEED1D1CB1A1CB25719108, puis la première de nouveau.

Capture réellement produite et inspectée : `20261109-104807-critical-clock-path-monitor-debug.bmp`, SHA256 fichier 162FD4E97CB8FF4D3D07BE20A79393C087D912C1559AEFE60DB9F951DFB9DA12, sous TestsMachines/Acorn/Acorn System 5 et copie TestsChips/Sn74S163/Acorn System 5. Moniteur Acorn Dos et prompt lisibles, fichier identique à la capture du moniteur avant ce bloc.

Nettoyage du cœur dans finally, aucun RML ni ressource graphique native créé. Banc temporaire supprimé, assembly régénérée ; 131 contrôles System/audio réussis (4 s) et 75 contrôles de composants réussis (99 ms). Aucun build de livraison utilisateur lancé. Le bloc de réduction des coûts par appel est vérifié ; System 5 reste en cours à 19,5–19,6 % Debug. Le générateur traite encore chaque période de 24 MHz et le rendu écrit chaque dot : avancement groupé et portions stables restent à traiter. Le CPU, les fréquences et les cycles matériels n'ont pas été modifiés.
