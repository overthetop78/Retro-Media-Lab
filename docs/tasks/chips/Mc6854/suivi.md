# Suivi — Mc6854

- [x] Motorola MC6854 ADLC raccordé à Econet BBC.
  - [x] Créer temporairement `artifacts/.tmp-mc6854/references/` : conserver la fiche Motorola MC6854, les schémas Econet BBC pertinents et les sources MAME/B-Em/JSBeeb/autres émulateurs réellement examinées ; établir les registres, protocoles, horloges, signaux, DMA et modes avant modification.
  - [x] Créer `docs/chips/Network/Mc6854.md` : décrire les fonctions présentes et les écarts établis, la frontière entre ADLC, carte Econet et transport hôte, ainsi que les vérifications de la puce entière, sans fiche vide.
  - [x] Créer temporairement `artifacts/.tmp-mc6854/Benchmark/` : figer le code initial dans un namespace isolé et mesurer les accès/FIFO/horloges ; conserver les résultats dans la fiche et prévoir la suppression du banc.
  - [x] Modifier les fichiers sous `src/RetroMediaLab.Chips/Network/Motorola/M6800/6854/` : réaliser les fonctions numériques documentées, registres, FIFO, trames série, FCS, signaux, interruptions, DMA, reset et modes ; préserver les API de trames nécessaires au transport existant sans y déplacer le réseau hôte.
    - [x] Modifier `Constants/Mc6854Constants.cs`, créer `Enums/AdlcFrameField.cs` et `Services/Mc6854.Fields.cs`, modifier `Services/Mc6854.Receive.cs` et `Services/Mc6854.Transmit.cs` sous ce dossier : constantes séparées, champs AEX/CEX/LCF, mots d’information seuls de longueur variable, FIFO fixes sans décalage de liste, maintien du transfert déjà chargé malgré CTS.
    - [x] Modifier `tests/RetroMediaLab.Chips.Tests/Network/Motorola/M6800/6854/AdlcTests.cs`, créer `AdlcFieldTests.cs` et modifier `docs/chips/Network/Mc6854.md` : vérifier ce premier bloc de champs/FIFO/CTS et consigner les résultats avant de poursuivre les fronts série ; conserver les tests autonomes utiles.
    - [x] Modifier `Services/Mc6854.cs` et créer les services série/états/bus nécessaires sous ce dossier, compléter `Interfaces/IMc6854.cs` : fronts et trames, CRC, stuffing, abort/idle, NRZI/boucle, acquittements, sorties et reset matériel documentés.

  - [x] Modifier si nécessaire `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/Common/Services/Econet.cs` et les tests de raccordement : conserver station et porte NMI dans la carte BBC, relier les broches documentées et préserver les transports existants.
  - [x] Modifier et compléter `tests/RetroMediaLab.Chips.Tests/Network/Motorola/M6800/6854/AdlcTests.cs` et les fichiers autonomes nécessaires : vérifier tous les registres, priorités/acquittements, FIFO, protocoles de trame, cas limites, fronts et contrôles ; corriger les écarts constatés par bloc.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuEconetTests.cs` : conserver les contrôles BBC natifs en tenant compte des fronts et des flags/FCS ; retirer les doublons de tests MC6854, conserver leurs vérifications utiles dans `tests/RetroMediaLab.Chips.Tests/Network/Motorola/M6800/6854/AdlcSerialTests.cs` et `AdlcSignalTests.cs`.
    - [x] Modifier `Services/Mc6854.Bus.cs`, `Services/Mc6854.cs` et les tests `AdlcSignalTests.cs` sous leurs dossiers MC6854 : vérifier les demandes DMA pendant les phases E, la temporisation effective des paquets, la boucle et les acquittements non destructifs ; corriger les écarts documentés.
    - [x] Modifier temporairement `artifacts/.tmp-mc6854/Benchmark/Program.cs` et modifier `docs/chips/Network/Mc6854.md` : comparer le code initial figé et le chemin série corrigé ; corriger les coûts hôte établis dans les services MC6854 sans changer les timings, puis consigner mesures, résultats intermédiaires et limites avant les preuves invité.
    - [x] Modifier `Services/Mc6854.Transmit.cs`, `Services/Mc6854.Loop.cs` et `Services/Mc6854.Receive.cs` sous le dossier MC6854, ainsi que `AdlcSerialTests.cs`/`AdlcSignalTests.cs` dans les tests : valider le transfert FIFO avant le dernier bit, le latch FD lu avant flags répétés et la prise de parole de boucle sans données préchargées, selon les séquences Motorola ; conserver les résultats dans la fiche.



  - [x] Créer temporairement `artifacts/.tmp-mc6854/Guest/` : contrôles réellement exécutés par le CPU BBC et captures de verdicts sous `artifacts/TestsChips/Mc6854/<Machine>/`, hors Git ; détacher les rappels et libérer les ressources dans finally.
  - [x] Créer `src/RetroMediaLab.Chips/Network/Motorola/M6800/6854/Services/Mc6854.Crc.cs` et modifier `Services/Mc6854.Receive.cs`, `Services/Mc6854.Transmit.cs`, `Services/Mc6854.Packets.cs` et leurs constantes : calcul CRC par octet quand le nombre de bits le permet, et tampon de trame unique préalloué, sans changer les bits ou leur cadence ; comparer dans le banc temporaire, puis revérifier les tests Chips et les captures invité après ces modifications.

  - [x] Modifier `docs/chips/Network/Mc6854.md` avec les résultats, mesures et limites ; construire le Debug avec modules=A et vérifier les 24 modules, l’exécutable et la DLL Chips dans lib.
  - [x] Supprimer `artifacts/.tmp-mc6854/`, les références, journaux et outils temporaires ; conserver les preuves locales et tests autonomes utiles.
  - [x] Modifier l’index Git et créer le commit séparé du MC6854 terminé, sans capture ni artifact.

- [ ] MC6854 : supprimer le sondage des sorties de transmission dans Econet.
  - [x] Créer `src/RetroMediaLab.Chips/Network/Motorola/M6800/6854/Services/Mc6854.Outputs.cs` ; modifier `Mc6854.cs`, `Mc6854.Status.cs`, `Mc6854.Transmit.cs` et `Interfaces/IMc6854.cs` : marquer les mutations RTS/FIFO/activité et publier leur état final seulement si les niveaux exposés changent, sans état transitoire de transmission au milieu d'une opération.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/Common/Services/Econet.cs` : retenir RTS/activité reçus et actualiser le retour CTS sur événement, retirer leur consultation périodique après Tick et les propagations post-écriture redondantes.
  - [x] Modifier `tests/RetroMediaLab.Chips.Tests/Network/Motorola/M6800/6854/AdlcSignalTests.cs` : ajouter un contrôle des niveaux notifiés et du relâchement RTS après transmission, sans notifications à l'état stable.
  - [x] Modifier `docs/chips/Network/Mc6854.md` avec le câblage effectué et la validation différée.

[Index des puces](../README.md) · [Machines](../../machines/README.md)
