# AcornEmu — Acorn Atom

État de la reprise du11 novembre2026 : **Atom en cours**, après System5 confirmé terminé par l'utilisateur. La revue6502 est vérifiée dans le périmètre accepté, avec ses limites explicitement conservées/reportées ; commit du bloc avant le composant suivant. La revue8255 suivante est vérifiée après publication GPIO C ciblée,30 contrôles Debug/Release et mesures comparées ; commit avant le VIA. Les revues VIA et MC6847 sont également vérifiées :155 puis76 contrôles Debug/Release, avances stables comparées et mesurées, limites VDG conservées sur décision utilisateur. La revue8271 est vérifiée avec165 contrôles Debug/Release,540 fenêtres comparées et la simplification des avances unitaires ; limites nominales conservées sur décision utilisateur. Le raccordement effectif des nouveaux parcours, les performances de carte, les médias et l’essai RML restent dans le suivi de reprise. Les cases historiques ci-dessous ne déclarent pas cette nouvelle revue terminée.

## Références matérielles retenues

- Source MAME : https://github.com/mamedev/mame/blob/master/src/mame/acorn/atom.cpp
- Carte mémoire : RAM CPU 0000–03FF ; cartes RAM texte 2800–3BFF ; vidéo 8000–97FF ; extension ROM A000–AFFF ; PPI B000–B003 avec miroirs ; VIA B800–B80F ; ROM C000–DFFF et F000–FFFF.
- `abasic.ic20` : 8192 octets, SHA-1 0072c83458a9690a3ea1f6094f0f38cf8e96a445, deux moitiés à C000 et F000. `afloat.ic21` : 4096 octets, SHA-1 ebcde5b36cb3a3344567cbba4c7b9fde015f4802, à D000.
- PPI A : sélection clavier bits 0–3 ; vidéo A/G et GM0–2 bits 4–7. PPI C : tape, buzzer et CSS en sortie basse ; horloge 2400 Hz, entrée tape, REPT et synchronisation vidéo en entrée haute.
- Le lecteur d’images Atom DOS existe dans MediaEngine (`AcornAtomDskReader`) : réutiliser ce format, ne pas recréer un catalogue matériel dans MediaEngine.
- Cadence MC6847, montage PAL et variantes matérielles à confronter aux schémas et à une seconde source avant de fixer le profil vidéo.

Référence MC6847 (ROM interne extraite du silicium) : https://github.com/mamedev/mame/blob/master/src/devices/video/mc6847.cpp ; 280 octets, SHA-1 5e9d68e55e73cae3d28adaff34fe115e00029009. Le décodeur devra utiliser ce fichier dans Firmware, comme le SAA5050 BBC, sans intégrer le binaire à l’assembly. Cadence du circuit de base : 3579545 / (228 × 262) Hz. Palette et organisation 5 × 7 relevées dans cette source ; carte PAL encore à confronter au manuel.

Carte Atom raccordée à la fabrique : CPU, bus, clavier, vidéo, buzzer, tape, Disc Pack, réglages et sélection des firmwares. Démarrage réel et programme BASIC 123+456=579 vérifiés ; sauvegarde/relecture par les broches tape et par le DOS sur disquette vérifiées.

Firmware installés et identifiés : Acorn Atom BASIC OS.bin (8192), Acorn Atom Floating Point.bin (4096), Acorn Atom DOS.bin (4096), MC6847.bin (280). SHA-1 conformes aux références MAME listées ci-dessus. Archives utilisées : https://github.com/Abdess/retrobios/tree/main/bios/Acorn/BBC%20Micro et https://colorcomputerarchive.com/repo/ROMs/MAME-MESS/mc6847.zip ; téléchargement et extraction en mémoire, aucun ZIP temporaire conservé.

## Résultats

404 contrôles Acorn autonomes réussis ; trois diagnostics ROM réelles réussis (BASIC/graphismes, tape, disquette). La séquence disque sélectionne *DOS, lit CAT puis effectue SAVE/NEW/LOAD/RUN sur une image ouverte en écriture. Les séquences tape confirment RECORD TAPE et PLAY TAPE par RETURN, suivant Atomic Theory and Practice, chapitre 1.10.

Références : https://chrisacorns.computinghistory.org.uk/docs/Acorn/Tech/Acorn_Atom_Disc_Pack.pdf ; https://www.theoddys.com/acorn/acorn_system_computers/atom/Atomic%20Theory%20and%20Practice.pdf .

Debug All : script terminé avec code 0, 24 manifestes et DLL présents ; empreinte du module Acorn livré identique à celle de la compilation. Exécutable : build/Debug/Retro Media Lab/retromedialab.exe. Aucune modification de production depuis ce build ; seuls les diagnostics temporaires et le suivi ont évolué.

# Reprise du 8 novembre — carte complète après les System

- [ ] Terminer les raccordements Atom avec les composants partagés déjà réalisés.
  - [ ] Modifier les fichiers générés de l'assembly de tests pour en retirer le diagnostic externe, puis les fichiers sous `build/Debug/Retro Media Lab/` avec tous les modules à la clôture du bloc Atom pour son essai live. Reprise autorisée le 11 novembre 2026 : réaliser d'abord le bloc ci-dessous ; l'ancienne attente de revue statique ne clôturait pas la machine.

Résultats du raccordement : 18 contrôles Atom Debug réussis. Comparaison de carte complète à ROM réelle : 24,301 → 24,016 ms/champ au repos ; 25,753 → 25,934 avec bande. Cycles et empreintes vidéo/audio identiques dans chaque passage avant/après ; aucun gain global significatif annoncé. Diagnostic externe supprimé et assembly de tests régénéré avec zéro erreur. Voir `docs/project/emulations/Acorn/Acorn Atom/validation.md`. La machine reste ouverte, notamment pour la saisie hôte et les essais invités/live.

## Reprise du 11 novembre 2026 — Atom après System 5

L'utilisateur confirme System 5 terminé et demande maintenant l'Atom. La procédure puces/machines et les directives de développement sont relues. Arbre Git propre au départ. Les validations antérieures du 6502, 8255, 6522, MC6847 et 8271 sont réutilisées ; elles ne prouvent pas une nouvelle mesure de carte complète. Le montage documenté reste celui de base, avec Disc Pack lorsqu'il est configuré ; la carte couleur PAL distincte n'est pas déclarée réalisée. Sauvegarde et chargement d'état exclus.

- [ ] Terminer le bloc Atom sur les composants validés, avant toute machine suivante.
  - [ ] Terminer les raccordements et interactions de carte après les cinq revues de puces.
  - [ ] Valider les logiciels invités et les parcours de médias/entrées/sons avant livraison.
    - [ ] Modifier `artifacts/.tmp-atom-final/Program.cs` : vérifier lecture/écriture puis fermeture/réouverture des médias, formats bande déclarés, conversion, exploration et visualisation par leurs services existants ; relever les filtres et géométries effectivement exposés, les protections, l'activité mécanique et le son électronique séparément. Réutiliser les tests autonomes fiables et les données consignées ; ajouter au suivi tout fichier de correction nécessaire avant modification, sans conclure à une couverture physique à partir d'un fichier ouvert.
  - [ ] Livrer la carte vérifiée pour l'essai utilisateur.
    - [ ] Modifier les produits sous `build/Debug/Retro Media Lab/` avec `scripts/local-building.cmd --building=debug --modules=A` hors bac à sable ; vérifier code retour, exécutable et chaque module annoncé. Consigner les résultats effectivement acquis et les limites restantes ; ne cocher la clôture qu'après les validations matérielles et l'essai RML coordonné.

  - Historique du chantier bande physique, **hors du travail Atom actuel sur décision utilisateur** : infrastructure conservée sans raccordement supplémentaire, aucun développement ni essai sur appareil demandé. Les actions réalisées ci-dessous ne constituent pas une autorisation de poursuivre ce chantier.

    - Historique des fournisseurs de capture/restitution audio dans Infrastructure, conservés sans nouvelle intégration.

Les actions de correction ou de validation révélées par les mesures seront inscrites après la dernière action réalisée, avant leur exécution ; aucune clôture n'est déduite de cet inventaire.

- [ ] Corriger ou qualifier la perte de frappes rapides signalée pendant l'essai RML.
  - [ ] Vérifier le transport et son raccordement avant livraison.
    - [ ] Compléter `AcornEmuAtomKeyboardTimelineTests.cs` pour qualifier le transport lors des accélérations et changements de vitesse, puis modifier `KeyboardInputTimeline.cs` et son raccordement selon les défauts reproduits ; ne pas déclarer ces modes validés par les 86 contrôles actuels.

Objectif Acorn déjà en pause sur demande utilisateur. Les manques et leurs raisons sont consignés en tête de `docs/project/emulations/Acorn/Acorn Atom/validation.md` : carte couleur non réalisée, interfaces souris/joystick non recensées et non réalisées, correctif clavier encore en cours de qualification/livraison. Le commit de suspension conserve cet état sans déclarer l'Atom terminé.
