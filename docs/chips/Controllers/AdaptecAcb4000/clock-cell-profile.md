# Carte BBC complète : coût du Winchester actif — 7 novembre 2026

## Conditions et portée

Même montage et mêmes commandes que la comparaison décrite dans `../AdaptecAcb4000.md` : Model A 32 Kio, MOS 1.20/BASIC II, SAA5050, Hope DFS, ADFS 1.30 variant 1, copie DAT/DSC de travail ouverte en lecture seule. Code Debug non optimisé ; pas de fenêtre, sortie audio Windows, build ou test concurrent. `dotnet-trace collect --profile dotnet-sampled-thread-time --format Speedscope` lance le diagnostic complet. Ce profil comprend l'amorçage, le repos et les huit demandes de LOAD ; ses parts ne sont pas celles des seules lectures isolées.

L'analyse des événements Speedscope ouvre/ferme les piles et additionne leurs intervalles seulement sous `BbcMicroMachine.RunFrame`. **47 406,97 ms échantillonnées**, résolution d'environ 100 Hz. Les marqueurs CPU_TIME/UNMANAGED sont exclus du choix du dernier cadre utile. Les durées instrumentées ne remplacent pas les mesures comparatives sans profileur.

## Résultats

| Chemin | Part inclusive | Part exclusive |
| --- | ---: | ---: |
| Winchester.AdvanceTo | 55,14 % | — |
| WinchesterTrackSignals.Tick | 52,22 % | 1,59 % |
| WinchesterTrackSignals.ClockCell | 38,39 % | 8,55 % |
| Acb4000.ClockReadCell | 19,30 % | 2,20 % |
| Acb4000ReadChannel.Feed | 15,47 % | 4,80 % |
| Acb4000.Tick | 8,29 % | — |
| Video.Tick | 20,96 % | 4,33 % |
| Acb4000.ReadGate | — | 2,39 % |
| Acb4000.WriteGate | — | 1,49 % |
| Acb4000._eccRegisters | — | 1,25 % |
| WinchesterTrackSignals.UpdateDriveSignals | — | 1,44 % |
| TrackCursor.Sample | — | 1,41 % |

Les parts inclusives se recouvrent et ne s'additionnent pas. `Cpu6502.Step` apparaît à 99,30 % inclusifs parce qu'il appelle l'avancement de la carte sur chaque cycle : ce chiffre contient les périphériques, il n'attribue pas 99 % du coût au calcul CPU. Les cadres asynchrones MediaEngine/File.ReadAllBytes qui entourent la continuation contiennent également la boucle entière ; ne pas les interpréter comme un coût de lecture de fichier de 100 %.

## Consultations établies et prochaine correction

- `WinchesterTrackSignals.ClockCell` relit Unit, FormatCylinder/Head, SelectedHead et les portes ReadGate/WriteGate à chaque cellule. Ces sorties appartiennent à l'ACB-4000 ; les lecteurs doivent retenir leurs changements reçus, sans les rechercher continuellement. Préserver les changements de porte au milieu d'un champ et les pipelines NRZ/RAM existants.
- `WinchesterTrackSignals.Mechanics.cs` demande les deux supports au fournisseur et republie Ready/Seek Complete/Track Zero dans chaque tick. Le remplacement du média passe déjà par `RefreshDrives` ; les changements de position et échéances de stabilisation sont connus du mécanisme. Examiner ces chemins de mutation pour publier seulement leurs transitions, en conservant la phase d'application existante.
- `ApplyDriveSteps` consulte les sorties de pas/direction par unité. Les impulsions doivent être reçues depuis leur production réelle, sans perdre un déplacement déjà émis avant RESET.
- Ne pas remplacer le canal MFM par des lectures de secteurs logiques, supprimer des cellules ou ralentir les horloges pour obtenir un gain. Les transitions physiques et arbitrages restent les critères des tests.

Ces constats définissent le prochain bloc ; ils ne décrivent pas des corrections déjà réalisées. La notification du port SCSI, elle, est implémentée et vérifiée séparément : 785 tests ACB-4000 et 63 tests Winchester Debug réussis. Moyenne active de la carte complète 57,746206 → 53,027239 ms/image sur deux couples alternés, avec mêmes cycles, PC, blocs lus et empreintes finales. Le budget de 19,97 ms/image demeure non atteint. Les traces temporaires sont supprimées après consignation de ces résultats.

## 7 novembre — pas ST-412 et entrées mécaniques (en cours)

Référence Debug avant ce bloc, même carte Model A 32 Kio, ADFS 1.30 et fenêtre de huit commandes LOAD du passage précédent : repos 17,543411 ms/image ; fenêtre HDD 52,685393 ms/image. Les compteurs cumulatifs incluent ici les 21 blocs lus avant la mesure : 286 blocs à la fin, soit 265 blocs supplémentaires sur le parcours comparé. Cycles et empreintes identiques au passage précédent. Ce ne sont toujours pas huit chargements complètement achevés.

Le relais relisait son fournisseur de supports deux fois à chaque tick, republiait READY/SEEK COMPLETE/TRACK ZERO et consultait STEP/direction, même sans changement. Le contrôleur publie maintenant ses fronts STEP ; le relais les consomme après le tick du contrôleur, puis fournit le retour mécanique au front suivant comme auparavant. Les montages sont retenus jusqu'au RefreshDrives explicite ; le compteur de stabilisation continue à son horloge.

Premier groupe Winchester Debug : 63 succès, 1 échec sur 64, durée 34 s. Le nouvel essai d'absence de consultation du fournisseur réussit ; l'échec concerne l'assertion ajoutée au scénario de positionnement existant. Bloc non validé tant que son détail n'est pas établi.

Détail du cas isolé : échec de mon assertion supposant tous les pas dirigés vers le centre. Le scénario existant commence par calibrer la piste zéro : 33 pas dont 16 dans l'autre direction. Le test doit couvrir ces deux directions, pas supprimer la calibration du contrôleur. Correction de l'assertion et contrôle de correspondance entre le front notifié et STEP/direction au moment de l'émission. L'essai isolé reproduisait uniquement cette assertion (660 ms).

Après correction de l'assertion : 64 tests Winchester/SCSI Debug réussis, 34 s. Les lectures/écritures/formatages physiques et l'équivalence des horloges groupées/unitaire sont conservés. L'essai supplémentaire garde exactement deux consultations initiales du fournisseur, aucune pendant les ticks, le positionnement et le reset, puis deux lors du remplacement explicitement notifié.

### Comparaison de carte complète et preuve

| Passage Debug | Repos, ms/image | Fenêtre HDD, ms/image |
| --- | ---: | ---: |
| Référence 1 | 17,543411 | 52,685393 |
| Candidat 1 | 17,478316 | 52,123028 |
| Référence 2 | 18,638583 | 57,166557 |
| Candidat 2 | 18,107163 | 52,188469 |

Ordre alterné référence/candidat/référence/candidat, aucun build ni test concurrent, même configuration que décrite ci-dessus. Moyennes actives : 54,925975 → 52,155748 ms/image (5,04 % de temps en moins). La référence varie sensiblement ; ne pas présenter 5,04 % comme une garantie générale. Les deux candidats sont proches et plus rapides que les références correspondantes. Le repos ne démontre aucun gain significatif.

Chaque passage conserve 3 993 602 cycles au repos, PC E469, et 25 559 045 cycles dans la fenêtre HDD, PC 8056. Compteur cumulatif identique de 286 blocs ; SHA256 final repos 10FB3F302C1435E781176CFCCDB92B231A34022EB5484D56B4C934B347987921 et actif C5D9E065E894E7AF47F2FEF49C68746BF1D23B3FFD4B2A2916432E8E4DFEBEEB. Aucun hachage PCM complet n'a été ajouté à ce diagnostic. Les régressions physiques restent couvertes par les 64 tests intégrés.

Cadence libre calculée sur la moyenne active : 19,17 FPS, environ 0,766 MHz et 38,3 % de 2 MHz ; cadence matérielle conservée autour de 50,08 Hz. Toujours très au-dessus du budget de 19,97 ms/image. Ce bloc retire effectivement les consultations mécaniques mais ne termine ni l'optimisation du contrôleur ni la machine.

Capture réelle du candidat inspectée : `20261107-022306-32k-hdd-notifications-mecaniques.bmp`, dans `artifacts/TestsChips/Acb4000/BBC Micro Model A/` et `artifacts/TestsMachines/Acorn/BBC Micro Model A/`. Elle montre le catalogue ADFS, INVADER WR et les commandes LOAD émises ; le dernier chargement reste en cours à l'instant comparé. Aucun média utilisateur ni réglage n'a été écrit. Aucun processus RML n'a été ouvert ou fermé.

Nettoyage effectué : WinchesterComparison.cs et la référence de DLL temporaires supprimés ; Native reconstruit en Debug avec son Program.cs préexistant, 0 erreur. L'avertissement NU1900 concerne le service de données de vulnérabilités NuGet inaccessible ; aucune restauration de dépendance ni modification d'option Debug. Les captures restent ignorées par Git. La réécriture des portes reste dans le groupe suivant, non coché.

## 7 novembre — portes résolues et sélection notifiées (en cours)

Référence Debug du bloc suivant, carte complète et scénario inchangés : repos 17,113119 ms/image ; fenêtre HDD 51,355888 ms/image. 3 993 602 et 25 559 045 cycles ; PC E469/8056, compteur cumulatif 286 blocs, mêmes empreintes finales que le bloc mécanique.

Le contrôleur recalcule maintenant RG/WG depuis les mutations de phase, READY, WRITE FAULT, RESET ECC et portes des canaux. Les canaux conservent leurs horloges et leurs cellules ; seules les modifications effectives de WG sont publiées, notamment la fermeture INDEX entre les deux demi-cellules du formatage. Le relais retient unité, têtes, coordonnées et portes ; les variables locales de cylindre/tête sont toujours échantillonnées avant ClockWriteCell, comme auparavant, même lorsqu'une fin de piste modifie la sélection pendant cet appel. L'activité mécanique est actualisée seulement après un événement pertinent.

Les nouveaux essais couvrent les notifications immédiates READY/ECC/FAULT/reset, la stabilité sans doublons, les deux ports et les deux têtes du formatage, ainsi que les fronts de portes des canaux et le reset ECC. Validation et comparaison globale en cours ; aucun gain annoncé avant leur résultat.

Premier groupe après intégration : 793 tests ACB-4000 Debug réussis en 41 s, dont les huit nouveaux cas de notification. Aucun échec. Régressions physiques et de séquencement existantes conservées ; groupe CostTests exclu, la mesure de performance attend le BBC complet.

Raccordement Acorn : 64 tests Winchester/SCSI Debug réussis en 27 s. Les scénarios conservent les accès physiques, les deux ports, le formatage, les changements de support et la comparaison horloges groupées/unitaires. Aucune correction supplémentaire nécessaire après les deux groupes.

Premier candidat complet Debug : repos 17,476097 ms/image ; fenêtre HDD 45,728285 ms/image contre 51,355888 de référence (10,96 % de temps en moins sur ce couple). Même progression : cycles 3 993 602 / 25 559 045, PC E469/8056, 286 blocs cumulatifs et empreintes finales inchangées. La fenêtre active reste au-dessus de 19,97 ms ; le repos ne démontre pas de gain. Comparaison alternée complémentaire en cours.

### Comparaison alternée après notifications des portes

| Passage Debug | Repos, ms/image | Fenêtre HDD, ms/image |
| --- | ---: | ---: |
| Référence 1 | 17,113119 | 51,355888 |
| Candidat 1 | 17,476097 | 45,728285 |
| Référence 2 | 17,018763 | 51,364395 |
| Candidat 2 | 16,977931 | 44,943382 |

Moyennes actives : 51,360141 → 45,335834 ms/image, soit 11,73 % de temps en moins. Référence/candidat/référence/candidat, aucun test/build concurrent. Les quatre passages gardent les cycles, PC, blocs et empreintes finales indiqués ci-dessus. Cadence libre active calculée : 22,06 FPS, environ 0,881 MHz et 44,0 % de 2 MHz ; cadence matérielle inchangée autour de 50,08 Hz. Le budget de 19,97 ms reste non tenu pendant cette fenêtre de lectures. Le repos ne démontre toujours pas de gain significatif.

Capture inspectée : `20261107-023718-32k-hdd-notifications-portes.bmp` dans `artifacts/TestsChips/Acb4000/BBC Micro Model A/` et `artifacts/TestsMachines/Acorn/BBC Micro Model A/`. Même catalogue et commandes ; le dernier transfert est encore en cours à l'instant comparé. Aucun test RML ni sortie audio Windows dans ce passage, aucun fichier média ou configuration utilisateur écrit.

### Chemins restants identifiés avant le bloc suivant

Le curseur de piste est encore sélectionné à chaque cellule : identité image/cylindre/tête comparée dans TrackCursor.Select, avec lecture répétée de WinchesterStorage.Cylinder. Le relais connaît déjà tous les changements pertinents : RefreshDrives, STEP appliqué après le tick du contrôleur, DriveSelectionChanged pour la tête, Reset et PublishWindow. Ces deux derniers chemins effacent les curseurs ; toute sélection conservée doit donc être invalidée aussi à ces endroits, pas seulement au changement de tête. Le cache ne doit être rechargé qu'au premier échantillonnage utile, à la même phase qu'actuellement.

INDEX est également envoyé à chaque cellule à SetIndex et au canal d'écriture. Ne pas remplacer aveuglément ces appels par un simple filtre de doublons : Acb4000ParameterChannel.Begin appelle Reset et remet son latch d'entrée à zéro. Une absence de nouveau front peut alors empêcher la reprise du niveau déjà présent ; ce contrat d'entrée doit être examiné avant toute suppression. Ce constat n'est pas une correction réalisée dans ce bloc.

Nettoyage du diagnostic et de sa référence de DLL terminé ; Native reconstruit avec son Program.cs original, zéro erreur (NU1900 inchangé). Captures conservées hors Git. Aucun autre build de livraison lancé, aucune fenêtre RML manipulée. Le prochain groupe de sélection de piste est inscrit dans le suivi avant toute modification.

## 7 novembre — sélection du curseur par invalidation (en cours)

Référence Debug avant ce bloc : repos 17,364426 ms/image ; fenêtre HDD 45,944536 ms/image. Scénario, cycles, blocs et empreintes inchangés par rapport au bloc des portes.

Le relais ne rappelle plus TrackCursor.Select à chaque cellule. Un état local indique qu'une sélection doit être résolue au premier échantillonnage après RefreshDrives, STEP appliqué, changement d'unité/tête, Reset ou PublishWindow. Le dernier cas est indispensable : la publication d'une écriture efface les impulsions mémorisées du curseur. Le redémarrage du parcours de flux lors d'une reprise de lecture reste distinct et conserve son ancienne phase. Aucun nouveau chargement de piste anticipé avant l'échantillonnage.

Deux nouveaux cas autonomes vérifient chaque port : secteurs 1, 34, 67, 100 et 1 avec contenu propre à chaque secteur, répétés après reset ; comparaison des données et du nombre de ticks entre progression unitaire et repos groupé. Les tests de formatage/écriture physique existants sont conservés dans le même groupe Winchester. Aucun fichier externe ajouté aux tests permanents.

Groupe Winchester/SCSI Debug après modification : 66 tests réussis, 38 s, aucun échec. Les deux cas supplémentaires gardent exactement les mêmes données et ticks entre horloges unitaires/groupées avec les changements de têtes/cylindres et le reset. Les tests physiques préexistants restent réussis. Aucun fichier de Chips modifié dans ce bloc ; son groupe de 793 tests inchangés n'a pas été relancé.

Premier candidat de carte complète : repos 17,497721 ms/image ; fenêtre HDD 46,532566 ms/image contre 45,944536 de référence. Même résultat matériel final, mais aucun gain établi (environ 1,28 % de temps en plus sur ce couple). Ne pas valider cette optimisation sur la seule diminution des appels. Un second couple alterné doit distinguer la variabilité d'une régression avant de conserver ou retirer le changement.

Second couple de la version à drapeau par lecteur : référence repos 16,599054 / HDD 48,924912 ms/image ; candidat repos 21,342547 / HDD 57,369024. La hausse du repos montre une variabilité hôte importante ; aucune régression matérielle ni cause système précise n'en est déduite. Les deux candidats n'établissent aucun gain : cette forme n'est pas conservée. Correction inscrite : réutiliser la condition de reprise déjà testée dans chaque cellule, sans rajouter un autre accès DriveState/indicateur avant elle. Les invalidations rendront cette reprise nécessaire ; les niveaux/cellules restent inchangés.

Forme corrigée sans drapeau supplémentaire : sélection et redémarrage sont réunis dans la condition existante `!reading || readUnit != unit`. Les événements pertinents invalident ce parcours ; reading/readUnit sont définis avant l'appel ClockReadCell pour qu'une notification pendant cet appel ne soit pas écrasée ensuite. Régression après correction : 66 tests Winchester/SCSI Debug réussis, 39 s, aucun échec. Comparaison fraîche de carte complète à suivre ; aucun gain attribué à la première forme rejetée.

Premier couple frais de la forme corrigée : référence repos 17,989282 / HDD 47,684408 ms/image ; candidat repos 17,988525 / HDD 45,022468. Environ 5,58 % de temps actif en moins, repos pratiquement identique. Cycles, PC, blocs et empreintes finales inchangés. Ce passage est distinct des essais de la forme à indicateur par lecteur, retirée ; dernière comparaison alternée en cours avant décision.

Dernier couple : référence repos 17,562420 / HDD 47,092496 ms/image ; forme corrigée repos 17,451626 / HDD 47,237192. Le gain du couple précédent n'est pas reproduit ; les plages actives se recouvrent. Les cycles, blocs, PC et empreintes sont restés identiques, mais aucun gain global suffisamment stable ne justifie de conserver ce changement. Les deux formes sont retirées ; les sources du relais sont restaurées à dc2e151a8, qui garde les notifications STEP/portes et leurs gains déjà validés. Les deux nouveaux tests de données distinctes après changement de tête/cylindre/reset restent utiles et sont conservés. Aucun gain sélectionné à partir du seul meilleur passage n'est annoncé.

Après restauration du relais dc2e151a8 : les deux nouveaux cas autonomes passent sur cette version, 10 s. Ils restent dans les tests permanents ; aucune des deux formes d'optimisation du curseur n'est conservée. Les 64 autres cas de raccordement déjà validés sur dc2e151a8 ne sont pas relancés. Le prochain profil porte sur cette carte restaurée pour réorienter le travail vers les coûts globaux dominants, pas sur une nouvelle supposition concernant la sélection du curseur.

### Nouveau profil global sur dc2e151a8 restauré

`dotnet-trace` sampled-thread-time, conversion Speedscope, 41 923,082 ms échantillonnées sous BbcMicroMachine.RunFrame. Le profil inclut chauffe, repos et fenêtre HDD ; ce n'est pas une attribution au seul chargement actif et ses durées instrumentées ne sont pas comparées aux mesures libres.

| Chemin | Inclusif | Propre |
| --- | ---: | ---: |
| Winchester.AdvanceTo | 48,78 % | 0,33 % |
| WinchesterController.TickHardware | 46,83 % | 0,57 % |
| WinchesterTrackSignals.Tick | 45,72 % | 1,55 % |
| WinchesterTrackSignals.ClockCell | 34,80 % | 8,28 % |
| Video.Tick | 24,01 % | 5,01 % |
| Acb4000.ClockReadCell | 19,26 % | 2,07 % |
| Acb4000ReadChannel.Feed | 16,82 % | 5,21 % |
| Acb4000.Tick | 8,81 % | 1,29 % |
| Acb4000.SetIndex | 2,77 % | 0,83 % |
| TrackCursor.Select | 0,95 % | 0,55 % |

Autres coûts propres : AdvanceBufferClock 1,95 %, TrackCursor.Sample 1,82 %, ClockReferenceCell 1,27 %. Les lignes inclusives sont imbriquées et ne s'additionnent pas. Cpu6502.Step porte 99,14 % inclusifs parce que le tick de carte et ses périphériques sont exécutés dans sa pile ; cela ne démontre pas un CPU consommant seul 99 %. De même, les parents ReadAsync/File occupent toute la continuation du diagnostic : ce n'est pas 100 % de lecture du fichier.

Ce profil confirme que TrackCursor.Select n'était pas une priorité de coût global ; ne pas reprendre sa suppression sans nouvelle preuve. La chaîne de cellules/lecture et son horloge RAM reste prioritaire. Dans Feed, ClockReferenceCell avance la source référence puis le synchroniseur et le retard sériel ; ClockBit n'est appelé que sur la phase DATA correspondante. Aucun double avancement matériel n'est établi par cette lecture. AdvanceBufferClock reconsulte les requêtes périphérique/hôte et TransferPending à chaque arrivée de source ; examiner les notifications de ces demandes et l'avancement sans requête, en gardant les fronts RAM, les remplacements de demandes et l'arbitrage A/B déjà testés. Ne pas supprimer ces horloges ni supposer leur inutilité.

Nettoyage effectué : diagnostic, référence de DLL, nettrace et conversion Speedscope supprimés après consignation. Native reconstruit avec son Program.cs préexistant, zéro erreur (NU1900 inchangé). Les captures de la tentative restent ignorées par Git, dont `20261107-024707-32k-hdd-selection-piste.bmp` pour la première forme et `20261107-025230-32k-hdd-selection-piste.bmp` pour la forme corrigée ; cette dernière a été inspectée. Ce sont des preuves de rendu identique, pas de gain retenu. Les deux sources WinchesterTrackSignals sont identiques à dc2e151a8.

### 7 novembre — ECC : valeurs de registres dérivées conservées (en cours)

Dans Acb4000EccRegisters.Advance, chaque bit recalculait Polynomial, VisibleMask (deux fois) et LowBit depuis les mêmes registres. TryClockDataBit et CheckPassed recomposaient également les autorisations RESET/longueur. Ces causes ne changent que depuis WritePolynomial, WriteControl ou Reset. Le candidat conserve le masque, le polynôme visible effectif et l'autorisation aux mutations ; le syndrome reste avancé à chaque bit, et les chemins SHIFT, CLEAR, feedback désactivé et longueur non documentée gardent leurs règles. Aucun bit ni phase regroupé en traitement d'octet.

Référence BBC complète avant modification (après 87440ea49) : repos 17,239871 ms/image ; fenêtre HDD 43,392583 ms/image. Cycles 3 993 602 / 25 559 045, PC E469/8056, 21/286 blocs cumulatifs, empreintes finales habituelles inchangées. Cette nouvelle référence ne se compare pas à un ancien meilleur passage choisi isolément. Mesures sans compilation ni tests concurrents, rendu et drainage audio compris.

Deux nouveaux cas balayent les 256 contrôles pour deux polynômes, changent chacun des quatre registres de taps en cours de champ et vérifient les sorties de syndrome et série à chaque bit contre une récurrence numérique indépendante. RESET conserve les taps ; les vecteurs connus et tests de portes/SHIFT préexistants restent dans le passage regroupé. Aucun gain annoncé avant la mesure de carte complète.

Validation du candidat ECC : 801 tests ACB-4000 Debug réussis en 36 s, zéro échec, CostTests exclus ; les deux nouveaux balayages par bit passent avec les cas existants SHIFT/RESET/longueurs, lecture et écriture physiques. Reconstruction du banc BBC Debug réussie, zéro erreur, avertissement NU1900 inchangé. Le passage Winchester/SCSI et la mesure globale restent à faire.

Raccordement après le candidat ECC : 66 tests Winchester/SCSI Debug réussis en 34 s, zéro échec ; les autres modules ont été recompilés par les références du projet de tests, sans livraison de build ni manipulation de RML. Les mesures libres commencent après la fin de ce processus.

Premier candidat ECC, carte complète Debug : repos 17,421850 ms/image ; HDD actif 43,009365 contre 43,392583 de référence, environ 0,88 % de temps en moins. Cycles/PC/blocs/empreintes finales identiques. Écart faible : aucun gain établi sur ce seul couple ; une dernière comparaison alternée décide de conserver ou retirer la modification, sans multiplier les variantes.

Comparaison alternée finale du bloc ECC : référence 1 repos 17,239871 / HDD 43,392583 ms/image ; candidat 1 repos 17,421850 / HDD 43,009365 ; référence 2 repos 17,771490 / HDD 43,971866 ; candidat 2 repos 16,994916 / HDD 41,410393. Moyennes actives 43,682224 → 42,209879 ms/image, soit 3,37 % de temps en moins ; 23,69 images émulées/s, 0,946 MHz, 47,3 % de la cadence matérielle. Les deux candidats sont inférieurs aux deux références, mais le gain varie fortement entre couples (environ 0,88 % puis 5,83 %) ; ne pas présenter ce pourcentage moyen comme une garantie indépendante de la charge hôte. Aucun test ou build concurrent aux mesures.

Tous les passages conservent les 3 993 602 / 25 559 045 cycles, PC E469/8056, 21/286 blocs cumulatifs et empreintes finales 10FB3F302C1435E781176CFCCDB92B231A34022EB5484D56B4C934B347987921 / C5D9E065E894E7AF47F2FEF49C68746BF1D23B3FFD4B2A2916432E8E4DFEBEEB. La fenêtre comporte 640 images actives et huit commandes LOAD, pas huit latences de chargement complet. Le drainage audio est inclus ; aucun hash de séquence PCM complète n'est revendiqué. Le changement est retenu, sans toucher aux fréquences, phases ou réglages Debug.

Capture réelle hors interface inspectée : 20261107-033806-32k-hdd-ecc-registres.bmp dans artifacts/TestsChips/Acb4000/BBC Micro Model A/ et artifacts/TestsMachines/Acorn/BBC Micro Model A/. Catalogue et LOAD visibles, même rendu final ; cette image n'est pas une preuve de cadence présentée par RML. Les 801 tests puce et les 66 raccordements Debug sont réussis. La finalisation ACB-4000/Winchester et le budget actif 19,97 ms restent ouverts.

Diagnostic ECC et référence DLL supprimés après consignation ; banc Native préexistant reconstruit avec Program.cs, code 0, zéro erreur (NU1900 inchangé). Les captures restent ignorées par Git. Aucun RML lancé/fermé ni fichier utilisateur écrit.

### 7 novembre — phase ST-412 conservée entre cellules (en cours)

ClockWriteCell retourne déjà immédiatement hors WRITE/FORMAT ; aucun nouveau mécanisme de notification n'est ajouté pour ce seul test peu coûteux. ClockCell reconvertissait néanmoins chaque rotation en nanosecondes par multiplication/division 64 bits, même lorsque la phase n'avait avancé que d'une cellule fixe.

Pour ce montage, ReadClock = 10 MHz et RevolutionsPerSecond = 60 ; CellNanoseconds = 100 est exact. Tant que la rotation ne reboucle pas, floor((r + 60) × 10^9 / (10^7 × 60)) = floor(r × 10^9 / (10^7 × 60)) + 100. Le candidat conserve cette phase dérivée par lecteur et la rétablit par la division rationnelle d'origine au rebouclage et après chaque repos groupé. Il n'utilise pas un tour arrondi constant de 16 666 667 ns : les restes de rotation restent conservés et empêchent la dérive. Aucun SetIndex, ClockWriteCell, ClockReadCell, pas ou front supprimé.

Avant changement du relais, quatre nouvelles assertions temporelles ont été étalonnées sur le parcours en mémoire existant : repos 1 / horloge 2 000 000 / port 0 → 636 953 ticks de transaction ; 23 333 / 2 000 000 / port 1 → 646 956 ; 100 001 / 2 000 003 / port 0 → 636 954 ; 333 334 / 2 000 003 / port 1 → 636 956. Ce sont des références de non-régression du séquencement fonctionnel antérieur, pas des mesures matérielles nouvelles. La première collecte utilisait volontairement zéro comme valeur à relever ; le journal quiet ne montrait pas les valeurs, le passage no-build à journal normal les a obtenues. Ces échecs de collecte ne signalent pas un défaut de lecture. Les valeurs exactes sont maintenant inscrites, avant validation du candidat.

Référence de carte complète avant conservation de phase (567b081b7) : repos 17,266236 ms/image ; HDD actif 42,561432 ms/image, 100/640 images, mêmes cycles, PC, lectures et empreintes finales. La mesure s'est achevée sans tests ni compilation concurrents.

Après conservation de phase : 70 tests Winchester/SCSI Debug réussis en 37 s, aucun échec. Les quatre durées de transaction relevées avant modification restent exactes ; les cas préexistants de flux physiques, écriture, FORMAT, repositionnement, reset et horloges groupées/unitaires restent réussis. Aucun fichier Chips modifié ; les 801 cas ACB-4000 inchangés, déjà validés au bloc précédent, ne sont pas relancés. Banc Debug reconstruit, zéro erreur, NU1900 inchangé.

Premier candidat phase conservée, BBC complet Debug : repos 16,975450 ms/image ; HDD actif 41,767904 contre 42,561432 de référence, 1,86 % de temps en moins. Même progression de cycles, PC, blocs et empreintes finales. Ce premier écart faible ne suffit pas à retenir la modification ; dernière comparaison alternée avant décision, sans nouvelle variante.

Dernier couple alterné : référence repos 16,393074 / HDD 42,704683 ms/image ; candidat repos 17,004855 / HDD 43,725975. La première amélioration de 1,86 % n'est pas reproduite : le second candidat est environ 2,39 % plus lent. Moyennes actives 42,633058 (références) / 42,746940 (candidats), soit aucun gain. Les cycles, données, blocs et empreintes finales sont identiques, mais ce cache ajoute deux mises à jour de tableau par cellule ; il n'est pas retenu sur la seule disparition d'une division. Le relais est restauré à 567b081b7 ; les quatre nouveaux contrôles autonomes de ticks restent utiles et sont conservés. Aucun nouveau gain de cadence revendiqué ; pas de troisième variante sur ce calcul peu prometteur.

Après restauration du relais fonctionnel 567b081b7 : les quatre nouvelles références temporelles passent en Debug, 1 s, aucun échec. Les 66 autres cas inchangés étaient déjà validés sur ce relais et ne sont pas relancés. Capture du candidat rejeté inspectée : 20261107-034933-32k-hdd-phase-conservee.bmp, sous TestsChips/Acb4000/BBC Micro Model A et TestsMachines/Acorn/BBC Micro Model A ; elle montre le même catalogue et les LOAD, sans prouver un gain. Cette capture reste hors Git.

### Report explicite du budget HDD actif

Le modèle numérique fonctionnel ACB-4000 possède déjà son audit de clôture, ses tests de commandes/canaux et ses preuves invitées. Le point encore incomplet est le budget Debug de 19,97 ms/image pendant la fenêtre HDD active, pas la lecture du catalogue ni l'exécution des commandes. Les notifications STEP, portes, demandes RAM et paramètres ECC ont apporté des gains mesurés ; les deux tentatives de sélection conservée et celle de phase conservée n'ont pas confirmé de gain et ont été retirées. La dernière référence reste autour de 42,6 ms/image actif, contre environ 17 ms au repos, sans altération des fréquences matérielles.

La recherche sur ce seul budget s'est prolongée au-delà d'une heure et les derniers petits changements n'établissent plus de solution viable pour combler l'écart global. Appliquer maintenant l'exception de la directive permanente : reporter explicitement cette optimisation incomplète, sans annoncer le budget tenu ni le contrôleur pleinement finalisé pour ce critère. Conserver le code fonctionnel et les gains vérifiés, ne pas enchaîner d'autres variantes locales sans nouvelle preuve de coût dominant. Le Model A reste la machine en cours ; poursuivre ses autres puces et raccordements encore ouverts, notamment le MC6854/Econet identifié dans son suivi. Une reprise du budget doit partir d'une attribution globale nouvelle et d'une solution structurelle établie, sans retirer les cellules, phases, interruptions ou fonctionnalités matériels.

Nettoyage du bloc phase effectué : diagnostic et référence DLL supprimés, Native reconstruit avec son Program.cs préexistant, code 0, zéro erreur (NU1900 inchangé). Captures utiles conservées hors Git ; aucun média/configuration utilisateur écrit ni RML lancé/fermé.
