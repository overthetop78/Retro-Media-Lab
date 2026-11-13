# Révision du cadre, des commandes et des raccourcis

- [ ] Appliquer la présentation et les touches demandées.
  - [ ] Raccourcis et commandes.
  - [ ] Cadre des mesures.
  - [ ] Vérification groupée.
    - [ ] Modifier ce document avec les résultats des tests Debug et les points restant à confirmer avant une livraison ; conserver les captures hors Git.

- [ ] Implémenter les états AcornEmu avec les contrats Emulation déjà existants.
  - [ ] Inventorier précisément les états persistants de la machine et de ses composants.
  - [ ] Sauvegarde et restauration matérielles complètes.
    - [ ] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/ModelBPlus/Services/ModelBPlusMemory.cs`, créer `Contracts/ModelBPlusMemoryState.cs` et `Services/ModelBPlusMemory.State.cs`, puis compléter `BbcMicroState.cs` et `BbcMicroMachine.State.cs` dans le dossier Common : inclure la mémoire privée/shadow du B+ dans les données du parcours commun.
    - [ ] Compléter `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuStateTests.cs` et `AcornEmuMemoryStateTests.cs` : vérifier la reprise des B+ 64/128 Kio et les verrous de sélection mémoire, puis consigner les résultats dans ce document.
    - [ ] Modifier les implémentations des composants dans `src/RetroMediaLab.Chips/`, leurs assemblages dans `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/` et `Services/EmulatorCore.cs` avec des états explicites conservant registres, mémoires, horloges, signaux, périphériques et opérations en cours ; ne déclarer le support qu'après restauration complète validée.

## Résultats intermédiaires

Six tests locaux Debug réussis : commandes et mesures, redémarrage à chaud conservant la RAM et reprenant le vecteur RESET, redémarrage à froid effaçant la RAM. Les 23 tests de stockage des réglages passent : migration unique vers les combinaisons demandées, suppression de l'avance rapide et des raccourcis médias, puis conservation des choix ultérieurs de l'utilisateur. Les quatre tests d'état CPU passent ; les états de l'assemblage complet restent à implémenter.

Le chemin existant est `MachineQuickStates` → `IEmulationSavedStates.SaveAsync/LoadAsync` → `Common.Services.Machine.SaveStateAsync/LoadStateAsync` → `EmulatorCore.SaveState/LoadState`. Le fichier, son en-tête et ses contrôles de compatibilité existent dans Emulation. AcornEmu renvoie encore `SupportsSaveStates = false` et ses deux méthodes sont absentes. L'intégration doit compléter ce cœur et ses assemblages : RAM principale et paginée, CPU, VIA, CRTC/VIDPROC/télétexte, PSG/audio, ACIA/SERPROC/tape, ADC, contrôleurs disques et Winchester, Econet, Tube et coprocesseurs si présents, mémoires non volatiles et phases de carte. Les images et ressources hôtes restent possédées par les services existants.

Bloc VIA : 3 tests Debug réussis (354 ms), 384 reprises à deux fronts PHI2, modes série et compteur d'impulsions. Bloc CPU committé séparément dans 5cbd77e99. Aucun support d'état complet encore annoncé par AcornEmu.

Bloc ADC : 5 tests Debug réussis (124 ms). Les conversions, la charge en cours, les canaux et les notifications temporelles continuent à l'identique après restauration. Bloc VIA committé séparément dans cef3e23e3.

Bloc ACIA : 3 tests Debug réussis (424 ms), 432 reprises TX/RX avec signaux modem. Bloc ADC committé dans 571650d0c. Les méthodes du cœur AcornEmu ne sont pas encore activées : l'assemblage matériel reste à finir.

Bloc SERPROC : 3 tests Debug réussis (567 ms), commit 34b30cb37. Bloc SN76489 : 3 tests Debug réussis (161 ms). Les méthodes de sauvegarde du cœur restent désactivées tant que les autres composants de la carte ne sont pas raccordés.

Bloc CRTC : 3 tests Debug réussis (290 ms), commit 60e440717. Bloc VIDPROC : 3 tests Debug réussis (139 ms). Bloc PSG committé dans be0704218. Aucun build livré pendant cette intégration incomplète.

Bloc SAA5050 : 3 tests Debug réussis (138 ms). VIDPROC committé dans fb50dfb36. Les pipelines vidéo se restaurent individuellement ; le support global AcornEmu reste désactivé.

Assemblages audio/vidéo BBC : 9 tests Debug réussis (1 s), incluant les tests de câblage série existants. Les continuations PCM et images correspondent après copie puis réinjection des états à un instant T, y compris le champ entrelacé en cours. SAA5050 committé dans 08faf34b2. Les périphériques et l’état de carte complet restent à raccorder avant activation du support.

Reprise série/tape : 4 tests Debug réussis (455 ms), 12 instantanés puis 5 000 cycles de continuation chacun, lecture/pause/bobinage/enregistrement. Les données, la position et les états électroniques correspondent sans notification artificielle pendant la réinjection.

Bloc 8271 : 3 tests Debug réussis (573 ms), lectures et écritures complètes après reprise en cours de transfert. L’utilisateur précise que la sauvegarde doit rester centralisée dans le cœur, commune aux modèles ; ne pas créer de formats de fichier ou de commandes de sauvegarde propres à chaque machine.

Bloc WD1770/1772 : 8 tests Debug réussis, reprise en FM/MFM après modification puis réinjection de l’état. Le mécanisme de fichier restera unique dans AcornEmu, conformément à la réponse de l’utilisateur ; les instantanés par puce restent réutilisables entre machines.

Parcours commun du cœur : 2 tests Debug réussis avec un assemblage synthétique, mêmes données et séquences après chargement, aucun RunFrame ni reset lors du chargement. Mémoire/clavier : 4 tests Debug réussis (121 ms), RAM paginée, extensions Aries/Watford et contacts restaurés. Le raccordement de la machine réelle reste incomplet et ne déclare pas encore le support.

Lecteurs : 2 tests Debug réussis (306 ms), reprise d’une lecture après les 17 premiers octets, avec position de tête, rotation et impulsions restaurées. Les images synthétiques restent empruntées et ouvertes chez leur propriétaire. Le mécanisme central du cœur et les états de lecteur sont intégrés en source ; les autres périphériques de la carte restent à raccorder avant activation.

Bloc ADLC : 3 tests Debug réussis (137 ms), données et signaux identiques après reprise en NRZ/NRZI. Bloc WD1770/1772 committé dans 5fe5e99aa. La sauvegarde des modèles n’est toujours pas annoncée disponible tant que leur assemblage complet ne fournit pas son état.

Reprise de carte BBC : 4 tests Debug réussis (892 ms), dont deux avec la carte et un programme 6502 synthétique, avec/sans E/S étendues et ADLC présent. Après modification puis chargement, continuation et comparaison intégrale de la RAM/CPU/pixels/PCM. Ce n’est pas une validation de logiciel réel ni de l’interface livrée. Les configurations HDD, B+, Master/Compact et coprocesseurs restent explicitement indisponibles tant que leurs états manquants ne sont pas raccordés.
