# Z80 — acquittement et bascules d'interruption

## Sources et limites

Sources constructeur archivées et références dans la [fiche Z80](../Z80.md). NEC µPD780, page imprimée **4-12**, chronogramme Interrupt Request/Acknowledge ; Zilog UM008011-0816, **table 1 pages 18–19**, DI/EI pages **182–183**, IM pages **184–186**, RETN page **290**. La fiche RETI page **288** distingue également le signal de fin destiné aux périphériques. Celui-ci reste à réaliser via les transactions matérielles ; ne pas le remplacer par un appel hôte fictif.

La fiche EI précise que le retard concerne les interruptions **masquables**. Les conditions publiées pour NMI sont indépendantes d'IFF1. Les tests de ce bloc vérifient ces règles fonctionnelles ; ils ne valident pas une fenêtre d'échantillonnage sur la SG-1000.

## Acquittement INT

`Z80BusCycle.InterruptAcknowledge` et `Z80BusSequencer` réutilisent le même stockage, les mêmes propagations et la même capture/restauration que les transactions précédentes. Ils réalisent M1 avec **deux Tw automatiques**, puis les Tw demandés par WAIT. IORQ est commandé au descendant du premier Tw ; WAIT est échantillonné au descendant du second Tw et des Tw ajoutés. Le vecteur est capturé au montant T3 avant la désélection, RD reste inactif et le rafraîchissement suit.

Ce cycle élémentaire représente **six T**, sans inventer le traitement de l'opcode ou les cycles supplémentaires des réponses IM0/IM1/IM2. Leur décodage, leurs temps internes, la pile et la lecture de la table IM2 appartiennent encore au CPU à réaliser. Aucun masque du bit 0 du vecteur n'est introduit dans la capture ; le point IM2 consigné dans la fiche principale reste ouvert.

Le banc à **4 MHz** utilise explicitement les maxima du profil NEC : IORQ bas à 710 ns depuis T1=0, capture à 1000 ns, IORQ haut à 1085 ns ; adresse refresh à 1110 ns, RFSH bas à 1130 ns et MREQ refresh bas à 1210 ns. Ces dates caractérisent le profil numérique du banc, pas une mesure physique sur une console.

## Bascules internes

Racine `src/RetroMediaLab.Chips/CPU/Zilog/Z80/Z80/` :

| Fichier | Rôle |
| --- | --- |
| `Enums/Z80InterruptKind.cs` | Absence d'interruption, INT ou NMI accepté ; aucun appel de routine hôte. |
| `Enums/Z80InterruptMode.cs` | IM0, IM1, IM2. |
| `Contracts/Z80InterruptState.cs` | IFF1/IFF2, mode, inhibition EI restante et niveaux/verrou d'entrée. |
| `Services/Z80InterruptControl.cs` | Effets fonctionnels internes ; n'avance pas le temps, ne lit aucun périphérique et ne réalise aucun transfert de pile. |

DI efface IFF1/IFF2 ; EI les établit avec une inhibition couvrant sa propre fin puis l'instruction suivante. Chaque instruction entièrement exécutée consomme une étape ; consulter la décision ou lire un préfixe ne consomme rien. Les EI successifs réarment l'inhibition. INT accepté efface IFF1/IFF2.

NMI utilise un verrou unique : un nouveau front d'assertion le positionne, plusieurs fronts pendants ne créent pas une file. L'acceptation NMI efface IFF1 en conservant IFF2, y compris lors d'entrées imbriquées ; RETN recopie IFF2 dans IFF1. BUSRQ interdit l'acceptation sans effacer la demande NMI pendante. Le CPU doit fournir les frontières et priorités temporelles réelles lors de l'intégration ; appeler cette logique arbitrairement entre deux phases ne constitue pas une implémentation fidèle des interruptions.

`ApplyNormalResetEffects` ne qualifie pas une impulsion RESET : il efface les bascules et le mode une fois le reset normal reconnu par le CPU. Il ne modifie pas les fils reçus. La détection du reset, ses particularités NMOS, ses délais et ses effets sur les autres registres/bus restent à réaliser. La restauration des bascules valide les modes et l'inhibition avant toute mutation. Les méthodes courantes n'allouent aucun objet, n'ajoutent aucun thread ni ordonnanceur, et ne recopient pas de champs dans la carte machine.

## Vérification du 11 novembre 2026 — Debug

Tests dans `tests/RetroMediaLab.Chips.Tests/CPU/Zilog/Z80/Z80/` : le banc bus existant couvre maintenant INTACK avec WAIT, attente trop tardive, reprise et progression dense/groupée. Les captures de reprise couvrent **50 dates** autour des fronts et propagations pour chacun des six cycles. `InterruptControlTests.cs` couvre EI, EI successifs, DI, NMI imbriqués, priorité, verrou unique, inhibition par BUSRQ, trois modes, effets du reset sur les bascules et restauration.

Passage `20261111-intack-iff-register-bus.trx` : **51 cas réussis, 0 échec, 0 ignoré**, code 0, durée du résumé 1 s ; répartition **33 cas bus, 10 cas bascules et 8 cas registres/arithmétique**. Preuve : `artifacts/TestsChips/Z80/SG-1000/20261111-intack-iff-register-bus.trx`. Le passage a signalé un avertissement xUnit2000 dans le nouveau test : les arguments constant/résultat d'une assertion étaient inversés ; leur ordre a été corrigé sans changement de logique. Le warning xUnit2031 antérieur dans `Shared/ArchitectureTests.cs:33` reste hors de ce lot.

Commande : `dotnet test tests/RetroMediaLab.Chips.Tests/RetroMediaLab.Chips.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~RetroMediaLab.Chips.Tests.CPU.Zilog.Z80.Z80" --logger "trx;LogFileName=20261111-intack-iff-register-bus.trx" --results-directory artifacts/TestsChips/Z80/SG-1000`.

Les contrôles sont autonomes, sans fichier média, application ou DLL externe. Ils ne prouvent ni l'exécution des instructions par un CPU intégré, ni le traitement complet des interruptions, du reset, du HALT ou de la concession de bus. Aucun débit CPU ni gain d'optimisation n'est encore annoncé.


## Intégration CPU du 11 novembre 2026

Le bloc décrit ci-dessus était initialement isolé. Il est maintenant raccordé aux phases du CPU : NMI, IM0/1/2, pile, table IM2 sans suppression du bit 0, EI, RETI/RETN, particularité P/V de LD A,I/R, HALT, reset et concession de bus. Le contrôle conserve les échantillonnages et les continuations en état version 5. Dernier passage groupé : **110 succès Debug, aucun échec ou ignoré**, preuve `20261111-full-cpu-active-events.trx`. Sources, phases, couverture et réserves de qualification des variantes sont détaillées dans [l'exécution](execution.md). Les limitations d'intégration décrites dans les premiers passages sont historiques ; la réserve de fidélité générale reste applicable.
