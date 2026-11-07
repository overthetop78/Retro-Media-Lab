# Demandes RAM et horloges AIC — raccordement en cours

## Fonctionnement existant à conserver

- Acb4000BufferController possède ses latches de données et requêtes périphériques (lecture/écriture), ses latches hôte et les identités PeripheralCycleEpoch/HostCycleEpoch. Une annulation incrémente l'identité même si le niveau de demande revient ensuite à vrai avant le prochain front.
- Acb4000Clocks possède ses phases système/référence et produit CLKB : attente de demande, début de l'impulsion avant le front descendant CLKA, relâchement après ce front. pending et requestActive couvrent ensemble TransferPending. Un changement de source peut relâcher requestActive sans perdre pending ; ResetPhase efface les deux.
- Acb4000AicRegisters.Clocks possède les validations d'adresses A/B. Premier front : prélèvement de l'adresse ; front accordé suivant : complétion RAM. Les accès périphériques A sont prioritaires sur les accès hôte B.
- Annuler/remplacer une demande invalide uniquement son adresse échantillonnée. L'impulsion d'horloge déjà engagée poursuit sa phase propre ; une annulation de requête ne remet pas arbitrairement les oscillateurs à zéro.
- Pendant une complétion périphérique, Sequencer.PollBufferTransfers peut consommer le latch puis redemander un accès. Le raccordement doit recevoir ces changements dans leur ordre, avant de choisir la prochaine adresse au même front.

## Modification prévue

Publier les niveaux et identités depuis Request/Complete/Cancel du tampon, et les changements de TransferPending depuis les mutations du générateur. Le raccordement conserve ces entrées ; AdvanceBufferClock ne va plus relire les propriétés du tampon et du générateur à chaque arrivée de demi-cycle pour découvrir une demande. Initialiser les entrées lors du branchement, y compris si le tampon emprunté possède déjà une requête. Aucun événement périodique ajouté, aucune suppression de front CLKA/CLKB, aucune exécution RAM dans les callbacks.

Fichiers concernés : Acb4000BufferController.cs, Acb4000BufferController.HostCycles.cs, Acb4000Clocks.cs, Acb4000AicRegisters.cs et Acb4000AicRegisters.Clocks.cs, sous src/RetroMediaLab.Chips/Controllers/Adaptec/ACB4000/ACB4000/Services/. Les chronologies existantes sont couvertes par Acb4000ClockedBufferTests et Acb4000ClocksTests ; les mesures viendront de la carte BBC entière, pas d'un coût isolé par getter.

## Premier passage de validation — 7 novembre 2026

Les requêtes A/B et TransferPending sont publiées depuis leurs mutations. Acb4000AicRegisters branche les destinataires une fois, initialise les entrées présentes et conserve niveaux/identités ; les annulations invalident uniquement l'adresse du port concerné. La boucle d'horloge utilise ces entrées mémorisées, sans relire les getters des deux autres composants. Aucun cycle RAM n'est déplacé dans les notifications.

797 tests ACB-4000 Debug réussis, 39 s, aucun échec, dont quatre cas nouveaux : requêtes/latches/identités sans doublon identique, requête hôte déjà présente avant branchement AIC et notifications du générateur pour les deux sources, relâchement d'impulsion, reset et changement de source. Les contrôles existants de priorité A/B, annulation/replacement entre deux fronts, haute impédance, phases groupées/unitaires et zéro allocation sont conservés. CostTests isolés exclus ; le coût global vient du BBC complet.

Référence de carte complète avant modification : repos 17,430939 ms/image ; fenêtre HDD 45,184130 ms/image. Scénario Model A 32 Kio, ADFS 1.30 et huit LOAD dans la fenêtre fixe de 640 images, comme dans clock-cell-profile.md. Cycles 3 993 602 / 25 559 045, PC E469/8056, 286 blocs cumulatifs et empreintes finales connues inchangées. Mesure candidate encore à réaliser après les régressions intégrées.

Raccordement Acorn : 66 tests Winchester/SCSI Debug réussis en 37 s, sans échec. Le même groupe contient les accès physiques, changements de support, deux ports, lectures après changement de tête/cylindre/reset et équivalences d'horloges. Aucun changement supplémentaire n'a été nécessaire après les deux groupes.

Premier candidat complet : repos 17,328222 ms/image ; fenêtre HDD 45,696468 contre 45,184130 de référence. Même progression matérielle mais aucun gain établi sur ce couple. La simple conservation des trois requêtes n'élimine pas encore le dialogue par demi-cycle : AIC demande au générateur sa prochaine frontière, avance celui-ci puis inspecte CLKB. Le bloc suivant inscrit au suivi confie ces fronts au générateur déjà propriétaire des phases ; il avertira le raccordement aux fronts utiles, sans agenda global ni suppression de la progression des sorties.

## Fronts utiles produits par le générateur — intégration en cours

La première forme conservait les requêtes mais cherchait encore périodiquement la prochaine frontière depuis AIC ; aucun gain n'était établi. La nouvelle forme retire cette boucle : Acb4000Clocks reçoit les demandes de service du tampon et produit BufferClockFell sur les fronts descendants CLKA sélectionnés et effectivement pilotés. Il garde ses phases propres et avance arithmétiquement les plages sans événement ; les sorties physiques restent calculables même sans demande RAM.

L'armement de la demande périphérique reste au début de l'avancement de la source sélectionnée, comme auparavant, et non dans le callback de requête. Pendant un avancement groupé, une complétion peut produire une nouvelle demande, prise en compte avant les fronts suivants du même intervalle. Les changements de source et de contrôle actualisent l'état piloté du générateur ; haute impédance et source non définie n'inventent aucun front RAM. Le raccordement ne contient plus d'avanceur périodique cherchant les états des composants : il reçoit le front, complète le cycle déjà échantillonné puis prélève l'adresse suivante avec la priorité A/B existante.

Deux nouveaux cas (sources système et référence) vérifient absence de notifications sans demande, source inactive et haute impédance, notification au niveau CLKA bas, arrêt des notifications après retrait de la demande et progression des phases inchangée. Régressions du bloc en cours.

Validation des fronts produits par le générateur : 799 tests ACB-4000 Debug réussis en 37 s, aucun échec, CostTests exclus. Le handle du passage précédent avait disparu sans résultat récupérable ; aucune instance de test n'était active, ce passage fournit le résultat vérifiable. Le raccordement Winchester et la comparaison du BBC complet restent à vérifier.

Le passage intégré sélectionnait AcornEmuWinchester et AcornScsi : 62 tests Debug réussis en 34 s. Le nom exact des quatre autres cas est AcornEmuScsiTests, non couvert par ce filtre ; ils seront exécutés séparément après les mesures pour ne pas fausser la charge. Le banc de carte complète a été reconstruit en Debug, zéro erreur ; avertissement NU1900 de consultation du service de vulnérabilités NuGet indisponible, sans nouvelle restauration.

Premier passage du générateur propriétaire des fronts : repos 17,416561 ms/image ; HDD actif 44,165307 ms/image. Les 3 993 602 / 25 559 045 cycles, PC E469/8056, 21/286 blocs cumulatifs et les deux empreintes finales sont identiques à la référence. Le couple de référence initiale donne 45,184130 ms/image actif, soit 2,25 % de temps en moins sur ce passage, gain encore à confirmer par alternance. Le budget de 19,97 ms/image actif reste non atteint.

## Comparaison alternée et résultat retenu — 7 novembre 2026

| Passage Debug, carte BBC complète | Repos, ms/image | HDD actif, ms/image |
| --- | ---: | ---: |
| Référence 1, avant modification | 17,430939 | 45,184130 |
| Générateur propriétaire des fronts 1 | 17,416561 | 44,165307 |
| Référence 2, DLL conservées avant modification | 17,134124 | 46,386879 |
| Générateur propriétaire des fronts 2 | 17,097843 | 43,566990 |

Moyenne active : 45,785505 → 43,866149 ms/image, soit 4,19 % de temps en moins. Les deux candidats sont plus rapides que les deux références sur cette fenêtre. Moyenne candidate : 22,80 images émulées/s et environ 0,910 MHz (45,5 % des 2 MHz matériels). Les 19,97 ms/image ne sont pas tenues pendant les LOAD ; cette optimisation ne clôture donc ni le contrôleur ni le Model A. Aucun réglage Optimize/Release ni changement de fréquence matérielle.

Les quatre passages conservent exactement les mêmes 3 993 602 cycles au repos et 25 559 045 cycles sur les 640 images actives, PC E469/8056, 21/286 blocs cumulatifs et empreintes finales vidéo 10FB3F302C1435E781176CFCCDB92B231A34022EB5484D56B4C934B347987921 / C5D9E065E894E7AF47F2FEF49C68746BF1D23B3FFD4B2A2916432E8E4DFEBEEB. Les huit commandes LOAD sont une fenêtre de progression identique, pas une preuve de huit chargements achevés. Le rendu vidéo et le drainage audio sont inclus dans la mesure ; aucune comparaison des séquences PCM complètes n'a été ajoutée à ce diagnostic.

Après les mesures, les quatre cas AcornEmuScsiTests non sélectionnés par le premier filtre passent en Debug, 257 ms, aucun échec. Avec les 62 Winchester déjà réussis, les 66 cas intégrés sont couverts, sans relancer les 62 inchangés. Les 799 tests ACB-4000 sont également réussis.

Capture réelle hors interface inspectée : 20261107-032615-32k-hdd-demandes-ram.bmp, conservée sous artifacts/TestsChips/Acb4000/BBC Micro Model A/ et artifacts/TestsMachines/Acorn/BBC Micro Model A/. Elle montre le catalogue INVADER WR et les commandes réellement saisies ; ce n'est ni une capture RML ni une preuve de cadence de sa présentation. Aucun fichier média ou réglage utilisateur écrit et aucune fenêtre RML ouverte/fermée.

Nettoyage effectué : WinchesterComparison.cs et winchester-reference supprimés ; Native reconstruit avec son Program.cs préexistant, code 0, zéro erreur. Les captures utiles restent ignorées par Git. Le bloc de raccordement RAM est vérifié ; la finalisation ACB-4000 et la performance du HDD restent ouvertes.
