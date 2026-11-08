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
