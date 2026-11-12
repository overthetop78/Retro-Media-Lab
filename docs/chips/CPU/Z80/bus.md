# Z80 — transactions M1, mémoire et E/S

## Réalisation du 11 novembre 2026

Séquenceur interne dans `src/RetroMediaLab.Chips/CPU/Zilog/Z80/Z80/`, sans décodeur CPU et sans montage SG-1000. Sources constructeur et archive : [fiche principale](../Z80.md), NEC µPD780 pages imprimées 4-8 à 4-11. Les deux pages de chronogrammes sont aussi conservées en définition supérieure sous `UPD780C-detail-08.png` et `-09.png` à côté du PDF NEC.

| Fichier | Responsabilité |
| --- | --- |
| `Enums/Z80BusCycle.cs` | Types de transactions M1, lecture/écriture mémoire et E/S ; aucun opcode. |
| `Contracts/Z80BusPins.cs` | Adresse et données pilotées, niveaux des commandes actives bas, haute impédance représentable pour les sorties trois états. |
| `Models/Z80BusTiming.cs` | Délais explicites, limites basses de cadence ; profil `NecUpd780C1Maximum` issu des maxima NEC. Ce profil de limites n'est pas un relevé de la SG-1000 et n'est pas choisi implicitement pour elle. |
| `Constants/Z80BusLines.cs` | Indices locaux des sorties, distincts des numéros des broches physiques. |
| `Services/Z80BusSequencer.cs` | Horloge reçue, transaction active, phase, attente échantillonnée, capture de données et propagations locales. Seuls les niveaux réellement modifiés sont publiés ; les entrées sont reçues et mémorisées. |
| `Contracts/Z80BusState.cs` | Capture/restauration locale, compteurs de fronts, phase, Tw, entrées, sorties et délais restants. Le CPU et les autres puces seront sauvegardés par leurs propriétaires respectifs. |

Le début d'une transaction utilise le front montant T1 courant ; la transaction suivante peut commencer au même front qui termine la précédente, sans insérer de cycle logiciel. I/R sont fournis par le CPU pour l'adresse de rafraîchissement ; leur incrément appartient au CPU. Le séquenceur ne rappelle pas un fournisseur pour lire les données ou WAIT : le câblage devra appeler `SetData` et `SetWait` lors des changements de niveau.

L'horloge et les échantillonnages sont traités à leurs fronts ; les sorties sont publiées après les délais propres au profil. `AdvanceTo` traite directement les échéances locales, sans boucle par nanoseconde. Les publications partagent le minimum d'échéances déjà résolu ; les notifications ne permettent pas de faire avancer le temps ou d'injecter récursivement un nouveau front de clock. Les rappels peuvent modifier les entrées à la date courante. Les objets sont entièrement gérés, sans thread, fenêtre, processus ni surface graphique.

## Vérifications regroupées — Debug

Tests : `tests/RetroMediaLab.Chips.Tests/CPU/Zilog/Z80/Z80/BusSequencerTests.cs`, associés aux tests registres/arithmétique déjà présents. Horloge du banc : 4 MHz, demi-périodes de 125 ns ; profil des maxima µPD780-1 explicitement choisi par le banc ; entrées numériques établies. Aucun média, application ni DLL externe n'est utilisé.

Les chronologies attendues sont calculées depuis les fronts des chronogrammes et les délais NEC, pas depuis le séquenceur de production. Par exemple, sur M1 débutant à 0 ns, la capture est au montant T3 à 500 ns, MREQ/RD remontent à 585 ns, l'adresse refresh apparaît à 610 ns et son MREQ descend à 710 ns. La lecture mémoire ordinaire capture au descendant T3 à 625 ns. Ces dates caractérisent le profil de limites du banc, pas une mesure sur une console.

- Traces datées des cinq types de transactions, niveaux RD/MREQ distincts du rafraîchissement, ordre des données et des fronts d'écriture.
- Tw automatique en E/S, Tw reçu en mémoire/M1 et en E/S, relâchement de WAIT, absence d'attente rétroactive après le front d'échantillonnage.
- Modification des données depuis un rappel de broche, capture à la phase correcte et absence de seconde capture pendant le relâchement différé des sorties.
- Reprise à 34 dates autour des captures, fronts et propagations pour chacun des cinq cycles ; reprise pendant WAIT ; mêmes sorties datées et mêmes états locaux.
- Comparaison des mêmes scénarios avec avancement dense à 1 ns et avancement groupé aux fronts, puis résolution locale des échéances. La boucle dense appartient exclusivement à l'oracle de test.
- Transactions adjacentes sans cycle ajouté, rejets de clocks trop courtes, de retour temporel ou d'états invalides, et protection des notifications.

Premier passage : **36 cas, 35 réussis, 1 échoué**, code 1, durée du résumé 1 s. L'assertion de réentrance du banc présentait parfois `SetClock(true)` alors que CLK était déjà haut : il n'y avait donc aucun nouveau front à refuser. Le banc a été corrigé pour demander le niveau opposé au niveau courant ; aucun changement de fonctionnement du séquenceur n'a été fait pour rendre ce cas passant. Preuve : `artifacts/TestsChips/Z80/SG-1000/20261111-bus-register-arithmetic.trx`.

Deuxième passage : **36 cas réussis, aucun échec ni ignoré**, code 0, durée du résumé 1 s, dont **28 cas bus et 8 cas registres/arithmétique**. Preuve : `artifacts/TestsChips/Z80/SG-1000/20261111-bus-register-arithmetic-corrected.trx`. Commande : `dotnet test tests/RetroMediaLab.Chips.Tests/RetroMediaLab.Chips.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~RetroMediaLab.Chips.Tests.CPU.Zilog.Z80.Z80" --logger "trx;LogFileName=20261111-bus-register-arithmetic-corrected.trx" --results-directory artifacts/TestsChips/Z80/SG-1000`.

Le warning xUnit2031 préexistant dans `Shared/ArchitectureTests.cs:33` demeure ; les contrôles généraux en échec au passage précédent n'ont pas été relancés ni corrigés dans ce lot.

## Limites et suite

Ce séquenceur ne réalise pas encore la libération des drivers pendant RESET/BUSRQ, l'arbitrage, HALT, les cycles d'acquittement d'interruption, les cycles internes des instructions ni leur décodeur. La validation et les états portent sur les seules transactions réalisées, pas le CPU entier. Les contraintes analogiques complètes (charges, marges et setup/hold de toutes les entrées) ne sont pas caractérisées par ces tests numériques. Aucun débit CPU ou gain d'exécution n'est annoncé ; les mesures utiles suivront sur le parcours CPU complet puis la machine complète.
