# Adaptec ACB-4000 — contrôleur SCSI vers ST-506

## Périmètre et état

État du 5 novembre 2026 : **modèle numérique documenté raccordé et validé, 861 tests puces/hôte réussis en Debug et Release ; FORMAT/READ/WRITE et diagnostic 61h exécutés par le 6502 du BBC B ; livraison Debug et 24 modules vérifiés**. Les octets non pilotés du dump suivent une convention numérique explicitée en fin de fiche, pas une mesure du bus 8085 réel. L'ACB-4000 est une carte contrôleur comportant plusieurs circuits et un firmware ; cette fiche décrit son modèle numérique de bus, commandes et canaux. Elle ne constitue pas une preuve d'émulation individuelle de chaque circuit interne.

Code : `src/RetroMediaLab.Chips/Controllers/Adaptec/ACB4000/ACB4000/`, interfaces `IAcb4000` et `IAcb4000Bus`. Tests : même arborescence dans `tests/RetroMediaLab.Chips.Tests/`. Carte hôte : [AcornScsi.md](AcornScsi.md). Câblage Acorn, conteneurs DAT/DSC et EMU, preuves de machine : [AcornWinchester.md](../../machines/AcornWinchester.md).

La carte documentée dessert **deux ports disque**. Les quatre unités de l'ancien montage Acorn ne sont pas une capacité prouvée de cette carte. Le service BBC est substitué dans les sources et 58 tests du relais Acorn passent en Release. Les captures 6502 récentes montrent FORMAT/READ/WRITE/READ avec RAM horlogée A/B. Le build complet de livraison et la clôture de la carte restent à réaliser. Les sections historiques ci-dessous conservent leurs résultats datés ; leurs anciens raccordements « ouverts » ne remplacent pas cet état actuel ni les résultats récents en fin de fiche.

## Références examinées

- [Adaptec ACB-4000 OEM Manual, mars 1984](https://theoddys.com/acorn/acorn_system_filing_systems/winchester_disks/Adaptec%20ACB4000%20OEM%20Manual.pdf) : bus, commandes, listes de paramètres, erreurs et annexes.
- [Acorn Winchester Disc 110/130 Service Manual](https://theoddys.com/acorn/acorn_system_filing_systems/winchester_disks/Acorn%20Winchester%20Disc%20Service%20Manual.pdf) : composition et raccordement officiel.
- Firmware original `400108-00`, 8192 octets, SHA-1 `58f5aaaeaa20bd5c93dcd641fd0d69f3c0f5f7a8` : examen des séquences de calibration, formatage et paramètres physiques. Les copies de travail restent hors Git. L'autre révision examinée, `400510-01h`, ne justifie pas de mélanger silencieusement les comportements de versions.
- [Projet MFM de David Gesswein](https://github.com/dgesswein/mfm) : comparaison du codage physique et du conteneur EMU ; le format de fichier demeure dans MediaEngine.
- [Seagate ST506/ST412 OEM Manual, avril 1982](https://minuszerodegrees.net/manuals/Seagate/Seagate%20ST506%20ST412%20-%20OEM%20Manual.pdf) : signaux et délais du mécanisme, distincts des commandes du contrôleur.

## Fonctions et validation actuelle

Les passages datés des sections suivantes sont l'historique de réalisation. Le groupe ACB-4000/hôte courant réussit 861 cas en Debug et Release ; les captures invitées et les limites numériques sont détaillées en fin de fiche.

| Domaine | Réalisation | Couverture / état de validation |
|---|---|---|
| Bus cible | IDs 0–7, sélection, phases commande/données/état/message, REQ/ACK, ATN et RESET. | IDs, abandons, ACK prématuré, transitions et délais à plusieurs horloges ; drivers manuels et BOE DMA à ACK. |
| Commandes et tampons | CDB six/dix octets, RAM partagée de 1 Kio, pointeurs, STOP/DONE, Read/Write Buffer, Read Capacity. | Limites, compteurs et données ; arbitrage RAM A/B, RESET entre fronts et absence d'allocation. |
| Statuts et Sense | CHECK, EQUAL, BUSY ; sense par LUN, adresse valide et acquittement. | Commandes, états mécaniques, remplacements, erreurs de média et interruptions dans le groupe courant. |
| Paramètres | MODE SELECT/SENSE, géométrie, disposition et acquisition physique. | Champs ID, collecte après calibration et reprise de commande ; relais natif activé. |
| Lecture MFM | Préambule, A1 à clock manquante, ID/DATA, ECC, tailles 256/512/1024. | Champs corrompus, faux motifs, retard NRZ et contrôle des 65 536 mots physiques pour les deux bits précédents ; preuve READ invitée. |
| Reprises et correction | Six lectures défectueuses avant correction ; VERIFY sans correction ; politique 65h. | Modes avec/sans reprise, erreurs récupérables/irrécupérables et contexte par port. |
| ECC | Polynôme 41044185, résidu, rafale jusqu'à huit bits puis nouvelle vérification. | Vecteurs indépendants, restauration après correction refusée, PATCH des registres et effet sur READ. |
| FORMAT sériel | Champs ID/DATA, gaps, entrelacement, défauts, INDEX et sortie MFM. | Canal et commande, interruptions et persistance DAT/EMU ; FORMAT de 33 secteurs exécuté par le 6502. |
| Paramètres physiques | Neuf drapeaux de sept bits dans les premiers ID C0/H0, distincts des DATA. | Collecte et relecture natives ; réinitialisation 60h depuis les champs réels. |
| REZERO/SEEK | STEP/direction, READY/SEEK COMPLETE/TRACK ZERO, calibration et BUSY par port. | Séquences physiques et relais ST-412 ; refus des signaux incohérents, échéances et RESET. |
| Positionnement implicite | READ/VERIFY/WRITE/chaque piste FORMAT attendent déplacement et SEEK COMPLETE. | Dernier STEP, conservation du tampon, remplacement, défauts et abandon de commande. |
| WRITE ordinaire | Réception, positionnement, recherche d'ID puis DATA MFM/ECC. | Pas d'écriture logique de remplacement ; WRITE AND VERIFY après le segment ; écriture/relecture CPU et réouverture DAT/EMU. |
| Diagnostics et ASIC | 60h–65h, micro-RAM, polynômes, GPIO, horloges, tampon et interface hôte. | Accès et effets natifs, deux contextes, lecture partielle, PATCH, RESET et dump 61h par le 6502 ; convention des lectures flottantes explicitée. |

## Signaux et timings

Le propriétaire fournit l'horloge et les entrées ; le contrôleur ne possède pas les supports. `ClockReadCell` reçoit les cellules, `ClockWriteCell` expose celles émises avec INDEX ; `ReadGate`/`WriteGate` encadrent les canaux. READY, SEEK COMPLETE, TRACK ZERO et WRITE FAULT restent des entrées physiques, conservées comme telles lors du RESET. Les fronts STEP et la direction sont exposés séparément par port.

| Élément | Modèle et limite |
|---|---|
| Stabilisation / deskew | Minima 450 ns / 45 ns, convertis selon l'horloge fournie. |
| Attente REQ/ACK | Échéance de 250 ms représentée et contrôlée dans les essais de bus antérieurs. |
| RESET | Seuil numérique accepté 50 ns ; maintien recommandé constructeur 25 µs. Ne pas confondre seuil du modèle et recommandation de carte. |
| Autres contraintes du bus | Réponse BSY au plus 200 µs et libération au plus 650 ns dans le texte OEM ; certaines figures divergent. Leur clôture doit vérifier les transitions du montage complet. |
| Pas | Cadences nominales 3 ms, 28 µs et 12 µs ; accumulation rationnelle sans dérive d'arrondi par pas. Front numérique représenté ; largeur électrique exacte non revendiquée. |
| Calibration | Si zéro initial actif : seize pas vers l'intérieur puis contrôle de désactivation ; recherche vers zéro bornée à 2048 pas. Signaux incohérents : erreur 06h. |
| SEEK COMPLETE | SEEK explicite rend le bus après le train de pas ; l'unité reste occupée. Les transferts attendent le signal. Expiration fournie explicitement ; durée de panne non inventée. |
| Mécanisme ST-412 | Référence 3600 tr/min et INDEX typique 200 µs ; ces valeurs de mécanisme ne constituent pas une cadence universelle de toutes les cartes/disques. Le relais natif reste à achever. |

Le firmware conserve une particularité dans le quatrième couple des paramètres physiques : réutilisation d'une valeur déjà transformée. Le codec préserve ce comportement observé plutôt que de le corriger par supposition. La séquence de recherche complète sur piste, les délais de panne et la mise en route physique ne sont pas déclarés clos.

## Matrice de validation finale

### Câblage du module par les registres hôte

`WinchesterController` relie désormais `ScsiHostAdapter`, `Acb4000` et `WinchesterTrackSignals` dans le module Acorn. La résolution bidirectionnelle des broches à chaque accès hôte conserve les acquittements asynchrones et l'horloge propre du séquenceur. RESET publie les impulsions encore en attente et nettoie les curseurs en finally ; aucune image empruntée n'est détruite par le montage.

Le 4 novembre 2026, **27 essais du module passent en Debug/Release**, zéro échec (3 s / 1 s), dont quatre nouveaux cas de transactions via FC40–FC43 : MODE SENSE et READ sur les deux unités avec pistes MFM en mémoire, unité invalide/sense, IRQ et RESET. Projet `RetroMediaLab.LocalEmulationTests`, filtre `FullyQualifiedName~AcornEmuWinchesterStorageTests`, mêmes options de configuration/x64/no-restore que les passages précédents. Ce sont des transactions du câblage réel dans un banc en mémoire, pas des instructions BBC exécutant ADFS.

Le service BBC utilise maintenant le nouveau montage dans les sources, avec ses compteurs, vitesses, deux ports documentés et acceptation EMU. Les accès invités et la persistance avec captures restent à prouver. Les diagnostics et échéances encore ouverts ne sont pas validés par ce raccordement.

### SEND DIAGNOSTIC 60h — réinitialisation du lecteur

L'option 60h réutilise le séquencement physique de START : calibration par STEP/READY/SEEK COMPLETE/TRACK ZERO, tête zéro, acquisition INDEX/ID puis restauration de la table courante. Le cache est invalidé avant cette acquisition ; ni le simple retour à zéro ni les paramètres DSC ne remplacent la relecture. Les politiques de lecture 65h ne sont pas réinitialisées par cette opération.

Le firmware 400108-00, 1AC0–1AD0, exige le qualificateur 30h pour cette option ; 1B01–1B08 appelle la routine 0285 et convertit ses échecs en erreur 1Dh. Cette restriction observée est conservée même si le texte OEM présente le qualificateur comme nécessaire aux seuls patchs. Un qualificateur incorrect renvoie 24h avant déplacement ; absence de READY, WRITE FAULT, zéro bloqué, remplacement ou format absent après expiration de recherche renvoient 1Dh. RESET abandonne la séquence sans déplacer artificiellement les têtes ni effacer une entrée physique empruntée. Les deux derniers octets de la liste de quatre octets ne deviennent pas une géométrie implicite.

Le 4 novembre 2026, le premier groupe Debug donne **532 réussites et un échec sur 533**, 19 s. Le cas sans format utilisait le banc manuel, qui ne fournissait pas la fin de recherche pendant l'attente suivant la liste de diagnostic ; le passage ciblé a confirmé « Target did not request a byte ». Ce cas utilise maintenant le mécanisme automatique existant, qui signale explicitement la recherche infructueuse. **533 cas passent en Release**, zéro échec, 3 s ; le cas corrigé passe en Debug, 133 ms. Filtres groupés habituels ; filtre Debug complémentaire `FullyQualifiedName~ReinitializeWithoutAFormatDoesNotInventAParameterTable`. Les onze nouveaux cas couvrent les deux unités, les paramètres restaurés, le qualificateur, les défauts, le remplacement et RESET, sans écriture de média.

L'expiration automatique native sans champs, les dumps 61h/62h et patchs 63h/64h restent ouverts. Les dumps exposent les zones de registres et RAM internes du contrôleur ; aucun bloc rempli de valeurs supposées n'est présenté comme leur réalisation. Aucun nouveau démarrage natif ou capture n'est revendiqué par ce groupe.

### SEARCH DATA EQUAL

La commande 31h reçoit d'abord son argument de vingt octets, puis un motif d'un bloc. Le contrôleur vérifie la taille d'enregistrement (taille du bloc ou zéro selon le manuel), le décalage initial nul, le nombre d'enregistrements non nul et borné par le CDB, la longueur d'argument égale au motif plus six, le déplacement de champ nul et la longueur de motif égale au bloc. Les bits CDB réservés restent refusés ; le bit 4 inverse la comparaison.

Le motif est conservé dans un tampon de 1 Kio alloué avec le contrôleur ; aucun tampon par secteur ni lecture logique directe n'est ajouté. Les blocs sont positionnés puis lus via les cellules ID/DATA et comparés par `SequenceEqual`. La première égalité, ou inégalité demandée, arrête la commande avec Equal (04h). Le sense conserve l'adresse valide du bloc trouvé ; les données du disque ne sont pas retournées sur le bus. Sans résultat, le statut est bon et le sense reste invalide. Le nombre d'enregistrements de l'argument borne la recherche, même si le CDB demande davantage de blocs.

Le canal SEARCH reprend les trois tentatives ECC de 17AF–18BC et ne lance pas la correction de rafale de READ. Après trois données incorrectes, il renvoie CHECK/11h avec adresse valide. Une troisième lecture correcte peut satisfaire la recherche. READY, remplacement et RESET interrompent l'opération ; une image de remplacement n'est pas explorée avec le motif de l'ancien support. Le séquenceur existant et son budget de recherche ID sont réutilisés.

Références : manuel OEM §6.3.2.6/tableau 6-4 et firmware 400108-00, 17AF–18BC/1737–173E. Le texte du manuel emploie le vocabulaire « Sense Key Equal », mais le firmware étudié écrit 80h (adresse valide, code zéro) et le statut 04h dans son ancien sense de quatre octets : cette représentation est conservée, sans ajouter un nouveau sense moderne supposé.

Le 4 novembre 2026, **522 cas réussissent en Debug et Release**, zéro échec (18 s / 2 s), avec les commandes groupées ACB-4000/hôte habituelles. Les 22 nouveaux cas couvrent les trois tailles et les deux sens de comparaison, le premier résultat, la limite d'enregistrements, les deux ports, la taille implicite, les sept erreurs d'argument, les champs CDB réservés, les trois tentatives ECC, RESET et remplacement pendant réception. Les octets des supports restent inchangés ; aucun média externe ni capture native n'est utilisé. L'attente sans événement et les reprises finales de recherche ID restent les limites communes décrites ci-dessus. Les preuves natives et l'optimisation du montage complet restent à produire.

### Cavalier I–J et jeu de commandes alternatif

L'entrée `SetExpandedCommandSet` représente la connexion physique I–J de la carte, retirée par défaut. Elle est échantillonnée par le traitement des commandes et conservée lors du RESET SCSI. Aucun choix global d'App ni dépendance à un conteneur n'est ajouté.

| Opcode | Sans cavalier | Avec cavalier |
|---|---|---|
| 05h | Refus | Succès sans vérification de piste, comme le prescrit l'annexe B. |
| 0Ch | Refus | Réception des huit octets INITIALIZE puis succès ; leur contenu ne remplace pas MODE SELECT. |
| 0Dh | Refus | Un octet de réponse, valeur huit pour la longueur maximale de rafale ECC. |
| 0Fh | TRANSLATE | Réception d'un bloc dans le tampon. |
| 10h | Refus documenté pour l'usage normal | Envoi d'un bloc depuis le tampon. |
| E0h/E3h/E4h | Refus | Succès sans répétition des diagnostics exécutés à la mise sous tension. |
| 13h/14h | Tampon 1 Kio | Même tampon 1 Kio, inchangé par cette option. |

La taille des commandes alternatives de tampon provient de la table courante du lecteur (256/512/1024), ou de la taille initiale si aucune table n'a encore été établie. Le tampon reste commun à la carte ; un transfert plus court ne remplace pas sa partie restante. Les supports, même protégés en écriture, ne sont pas modifiés par ces commandes de tampon. Les bits de contrôle réservés/vendor sont acceptés avec le cavalier, y compris dans le décodeur FORMAT ; le chaînage non pris en charge reste refusé.

Références : [manuel OEM, annexe B](https://www.bitsavers.org/pdf/adaptec/ACB-4000/500504-00_ACB-4000_OEM_Manual_Mar84.pdf), et firmware 400108-00, 1037–107F/10B6/1125 pour le branchement et les transferts. Le tableau annexe imprime une ligne TRANSLATE 0Eh alors que la description principale et la table de répartition du firmware étudié utilisent 0Fh ; cette divergence documentaire ne crée pas un alias 0Eh supposé. L'usage interne non documenté de 10h sans cavalier observé dans ce firmware n'est pas exposé comme commande alternative standard.

Le 4 novembre 2026, **499 cas passent en Debug**, zéro échec, 17 s. La comparaison du firmware a ensuite corrigé la réception des huit octets INITIALIZE (le succès immédiat initial était incomplet), avec ajout du contrôle de conservation de MODE SELECT. **500 cas passent en Release**, zéro échec, 2 s ; les six cas concernés passent aussi en Debug, 144 ms. Les filtres groupés habituels s'appliquent ; complément Debug : `FullyQualifiedName~InitializeConsumesItsParameterBlockWithoutOverridingModeSelect|FullyQualifiedName~AppendixBNoOpCommandsReturnGoodWithoutAccessingAMedium`. Les 26 cas ajoutés couvrent le refus normal, les commandes sans action sur le disque, le transfert INITIALIZE, les tailles et les deux ports, le tampon commun, le cavalier dynamique, RESET, les contrôles réservés/chaînage, FORMAT et le retour de 0Fh à TRANSLATE. Tests autonomes en mémoire, sans média externe ni capture native.

Ce groupe ne termine pas les diagnostics SEND/RECEIVE hors option 65, SEARCH DATA EQUAL, les échéances sans événement ni l'activation du montage dans les machines. La carte reste en cours.

### Budget de recherche des identités

`Acb4000SearchBudget` reprend le compteur chargé en 15E1–15E6 et décrémenté en 1697–16AA : quatre fois le nombre de secteurs de la piste. Le compteur est réarmé au début de chaque lecture/vérification ou recherche avant écriture, et abandonné à RESET. Les commandes READ, VERIFY, WRITE et TRANSLATE le consomment pour les identités reçues. À épuisement d'une recherche infructueuse, le statut de contrôle de la dernière identité détermine le sense : 10h pour le contrôle ID, 14h pour le secteur introuvable. Un ID cible valide à la dernière tentative peut encore fournir la réponse ou ouvrir le transfert prévu. WRITE n'ouvre pas sa porte lorsqu'aucun ID cible n'a été trouvé ; aucun accès logique ne remplace cette recherche.

Les nouveaux cas autonomes couvrent les budgets des cinq géométries de 9/17/18/32/33 secteurs, leur réarmement et leurs bornes sans mutation en cas d'argument invalide, la distinction des deux erreurs et la dernière tentative utilisable. Les cas du contrôleur couvrent READ/VERIFY/TRANSLATE avec ID incorrect ou autre adresse et WRITE sans identité cible, sans publication de piste.

Le 4 novembre 2026, le premier passage Debug donne **472 réussites et 2 échecs sur 474**, 16 s : les deux nouveaux cas VERIFY utilisaient un CDB court au lieu du CDB long attendu. Après correction de ce banc, **474 cas réussissent en Release**, zéro échec, 2 s ; les six cas `PhysicalSearchReturnsSenseWhenItsIdBudgetIsExhausted` passent aussi en Debug (318 ms). Avant cela, une erreur de typage de l'assertion nullable empêchait la compilation et a été corrigée sans exécution fonctionnelle. Commandes : projet Chips.Tests et filtre groupé ACB-4000/hôte habituels ; filtre Debug complémentaire `FullyQualifiedName~PhysicalSearchReturnsSenseWhenItsIdBudgetIsExhausted`. Aucun fichier externe ou artefact temporaire n'est créé par ces tests.

Ce compteur ne remplace pas les temporisations du firmware sans événement AIC-010 : absence d'INDEX, absence totale de champs ou d'un champ DATA après l'ID. Leur conversion temporelle exige encore l'horloge du montage et les états du séquenceur ; elle n'est pas remplacée par une durée arbitraire déduite de l'horloge du BBC. Les reprises/calibrations finales d'une recherche restent également ouvertes. Il ne s'agit donc pas de la clôture de toutes les échéances ni d'une preuve native.

### TRANSLATE normal et disposition physique

La commande 0Fh normale effectue le positionnement puis attend INDEX et recherche l'ID demandé sur les cellules. Elle compte les ID physiques, y compris les emplacements marqués défectueux, sans confondre leur ordinal et le LBA. Un ID dont le contrôle est incorrect ne fournit pas une réponse ; un nouveau front INDEX remet le compteur à zéro. Le champ DATA n'est pas demandé. La réponse de huit octets reprend le format de descripteur de défaut FORMAT : cylindre sur trois octets, tête, puis position sur quatre octets, en ordre fort d'abord.

La position restituée reproduit 10C7–1108 du firmware 400108-00 : **150 + ordinal × (taille du bloc + 54 ou 64)**. C'est le calcul nominal du contrôleur après recherche physique, pas un chronométrage exact des cellules de la piste. La disposition séquentielle est reconnue par l'ordre des premiers ID lors de l'acquisition et conservée par port ; FORMAT la met à jour à partir de sa disposition. Le remplacement et RESET l'invalident. Aucun nom de conteneur ni calcul du seul LBA ne détermine l'ordinal.

Le 4 novembre 2026, **454 cas réussissent en Debug**, zéro échec, 15 s. Après ajout du contrôle de cylindre/tête non nuls, **455 réussissent en Release**, zéro échec, 2 s ; ce dernier cas passe également seul en Debug (78 ms), sans relancer les 454 cas inchangés. Le filtre groupé reste `FullyQualifiedName~Acb4000|FullyQualifiedName~ScsiHostAdapter` ; le filtre Debug complémentaire est `FullyQualifiedName~TranslateReportsTheSelectedCylinderAndHeadAfterPhysicalPositioning`, mêmes projet/options que ci-dessous. Les cas ajoutés couvrent les trois tailles, les deux dispositions, l'ordinal avec défauts, INDEX, ID incorrect, réponse, CDB/adresse invalides, RESET et remplacement. Aucun média externe, fichier ou capture native n'est utilisé.

Les recherches sans ID acceptable attendent encore leur échéance automatique : les erreurs finales de recherche et reprises d'ID ne sont pas déclarées terminées. La sélection du cavalier qui transforme 0Fh en commande de tampon reste à réaliser. Ce passage ne vaut pas activation native ni clôture de toutes les commandes du composant.

### START/STOP et parking — validation du contrôleur

La commande 1Bh utilise le même séquenceur mécanique : STOP sélectionne la tête zéro et émet les pas vers `LandingCylinder` de la table courante, éventuellement modifiée par MODE SELECT, y compris au-delà du dernier cylindre de données ou avant zéro. Aucun accès logique à un secteur hors géométrie ni arrêt de moteur supposé n'est effectué. La fin attend SEEK COMPLETE après le dernier front. START force la calibration et une nouvelle acquisition des paramètres par INDEX/ID, même si le cache était valide, puis restaure la table courante depuis les champs physiques. READY, remplacement et RESET suivent les contrôles du séquenceur existant.

Références : manuel OEM §6.3.1.13 et description du parking dans MODE SELECT ; firmware 400108-00, 114D–1199 (START appelle 0285 : calibration puis lecture physique ; STOP calcule le déplacement signé). Le sens du parking et sa distance sont ceux du codec existant. Ces résultats ne constituent pas une preuve native et ne clôturent pas les délais de panne encore ouverts.

Le groupe passe maintenant **441 cas en Debug et Release**, zéro échec (13 s / 2 s), dont 14 cas START/STOP : six positions de parking, reprise après parking, table volatile puis restauration, trois CDB invalides, perte READY/remplacement et RESET. Le passage précédent de 440 cas passait également (14 s / 2 s), avant ajout du contrôle de table volatile. Aucun accès secteur hors géométrie ni aucune écriture de paramètres ou de données n'est admis par ces tests de parking. Commandes : même filtre ACB-4000/hôte que ci-dessous, configurations Debug puis Release.

### Acquisition des paramètres sur les cellules — validation du raccordement

`Acb4000ParameterChannel` attend INDEX puis assemble les neuf premiers ID physiques de C0/H0. Il utilise le canal MFM et le codec de paramètres déjà présents ; aucun accès au DSC ou au média n'est effectué dans cette classe. Un contrôle ID incorrect ou une perte de champ empêche de décaler les drapeaux sur les positions suivantes. La routine 1576 sélectionne un ordinal physique : elle ne rejette pas une adresse logique dupliquée ou son bit de défaut pour choisir les paramètres. Le collecteur conserve cette distinction avec la lecture de données ordinaire, qui sélectionne un LBA non défectueux. Un nouveau tour recommence un groupe incomplet. La fin de recherche peut être signalée explicitement sans produire de paramètres supposés ; RESET abandonne l'acquisition.

Le séquencement du firmware examiné en 0379–03F3 et 1576–1670 lit les drapeaux par ordinal physique et vérifie les erreurs des ID. Le moteur est raccordé à la calibration, à la sélection de tête zéro et à la reprise de commande ; son ancien chargement direct des métadonnées est retiré. MODE SELECT peut préparer un support non lisible sans déclarer son format valide, et ses valeurs volatiles restent distinctes des paramètres du format physique. RESET et remplacement invalident le cache. Le relais transmet INDEX et la sélection de tête. Ces raccordements passent les essais groupés ci-dessous ; l'échéance automatique de recherche reste ouverte, avec seulement une expiration explicitement fournie. Aucun démarrage natif complet n'est encore revendiqué.

Le 4 novembre 2026, le groupe ACB-4000/hôte donne **427 réussites en Debug et en Release**, zéro échec (12 s / 1 s). Le premier passage donnait 397 réussites et 30 échecs : la reprise interne de la commande après acquisition conservait à tort BUSY malgré la calibration achevée et SEEK COMPLETE reçu. La transition efface maintenant cet état au démarrage de l'acquisition ; les commandes explicites conservent leur attente mécanique. Le test FORMAT qui masque INDEX amorce d'abord les paramètres pour isoler l'attente du formatage.

Les **23 essais du stockage et du relais passent en Debug et Release**, zéro échec (1 s / 814 ms). Ils emploient des pistes MFM générées en mémoire, avec les paramètres sur C0/H0, INDEX et une géométrie cohérente. Les bancs du contrôleur refusent tout appel direct à `ReadParameters` ; les cas couvrent les trois tailles de bloc, le cache, RESET, le remplacement et la calibration. Les images sont empruntées et libérées par leur propriétaire. Ce résultat ne constitue ni une réouverture de fichier ni une preuve native dans un BBC.

Commande complémentaire : `dotnet test tests/RetroMediaLab.LocalEmulationTests/RetroMediaLab.LocalEmulationTests.csproj --no-restore --configuration Release -p:Platform=x64 --filter "FullyQualifiedName~AcornEmuWinchesterStorageTests" --verbosity quiet` ; le même filtre a été exécuté en Debug. Le résultat Release a été récupéré par un nouveau passage ciblé après disparition du handle précédent, dont le résultat final n'était plus accessible.

### Propriété de la projection graphique

Le branchement EMU et l'import de son espace `Images.Formats` sont retirés de `MediaVisualizationController` dans App. La préparation du document passe par `MediaVisualizationProviderRegistry.PrepareDocument` et le fournisseur flux existant. Le fournisseur utilise la vue logique EMU déjà définie dans MediaEngine ; les autres fournisseurs conservent leur document par défaut. App ne choisit plus de traitement en fonction de ce format. Les essais du registre couvrent la projection formatée avec lecture effective d'un bloc, la conservation du document physique vierge et le maintien d'une vue déjà en blocs.

Le passage ciblé du 4 novembre 2026 donne **5 tests réussis en Debug, zéro échec, 195 ms**, avec compilation d'App réussie. Au premier passage, quatre tests passaient ; une nouvelle assertion comparait les objets `MediaVolumeInfo` recréés plutôt que les descripteurs de volumes conservés par la projection. Elle vérifie maintenant ces descripteurs et leur nombre. Commande : `dotnet test tests/RetroMediaLab.Tests/RetroMediaLab.Tests.csproj --no-restore --configuration Debug -p:Platform=x64 --filter "FullyQualifiedName~MfmEmuLogicalViewTests" --verbosity minimal`. Aucun test des nouvelles commandes en cours d'adaptation n'a été exécuté dans ce passage.

### Relais mécanique ST-412 du module

`WinchesterTrackSignals` fournit maintenant READY, TRACK ZERO et SEEK COMPLETE séparément aux deux ports. Les fronts STEP sont consommés immédiatement après le tick du contrôleur, avant un éventuel RESET de l'appelant. Le stockage conserve sa position physique ; sélectionner l'adresse de transfert ne déplace plus instantanément la tête lorsque ce relais est utilisé. La stabilisation nominale de 15 ms du ST-412 est comptée dans l'horloge propriétaire, sans délai Windows ni arrondi ajouté à chaque tick. RESET conserve la position et la stabilisation en cours ; le disque est modélisé comme un équipement externe déjà sous tension lors de son attachement. Ce raccordement ne prétend pas simuler sa séquence électrique de mise sous tension.

Les sources de stockage sont échantillonnées par tick puis réutilisées pour ses cellules ; les appels de moteur et d'activité ne sont émis que lors des changements. Le relais emprunte les images et n'en détruit aucune. Les cinq tests autonomes du module ajoutés vérifient absence de lecteur, LUN invalide, SEEK et délai, calibration par trente-deux fronts, RESET et indépendance des ports. Avec les essais préexistants de stockage, **23 cas passent en Debug (822 ms) puis Release (1 s)**, zéro échec, le 4 novembre 2026. Le cas de LUN invalide a aussi conduit à borner la propriété de porte d'écriture avant d'indexer les deux ports physiques.

Commande : `dotnet test tests/RetroMediaLab.LocalEmulationTests/RetroMediaLab.LocalEmulationTests.csproj --no-restore --configuration Debug -p:Platform=x64 --filter "FullyQualifiedName~AcornEmuWinchesterStorageTests" --verbosity minimal`, puis la même en `Release`. Les images du banc sont en mémoire et les mécaniques n'ouvrent aucune sortie audio Windows ; les images possédées sont libérées après le nettoyage du relais dans `finally`. Les durées de suites ne sont pas un benchmark du relais. Les avertissements de compilation des autres modules/anciens tests ne sont pas présentés comme des erreurs de ce raccordement.

Ce relais reste non activé dans le service `Winchester` livré ; le chargement initial des paramètres physiques et les commandes restantes précèdent cette activation. Aucune capture native n'est déduite de ces essais sans interface.

### Segment DATA d'écriture ajouté

La projection des images DAT/DSC produit maintenant les neuf drapeaux de paramètres sur C0/H0 à partir du descripteur existant : géométrie, courant réduit, cadence de pas, parking et quatrième couple particulier du firmware. La taille nulle des anciens DSC conserve son sens de 256 octets. Les limites non représentables par l'ACB-4000 sont refusées avant mutation ; les paramètres du descripteur ne sont pas modifiés par la lecture de piste. Les autres pistes conservent leur compteur de défauts nul pour cette projection sans défauts physiques.

La projection inverse contrôle les neuf positions physiques, à partir de l'ID zéro même si les champs sont entrelacés, puis exige un compteur nul sur les autres ID. Elle compare aux paramètres DSC actuellement persistables : des paramètres physiques différents restent en piste et ne sont pas silencieusement perdus lors de la publication DAT/DSC. Le contrôleur consomme ces ID pendant son acquisition initiale ; aucun appel direct aux métadonnées du backend ne remplace cette lecture.

Le 4 novembre 2026, **46 cas Winchester/Adaptec réussissent en Debug puis Release**, zéro échec, durées de suite 513/486 ms. Ils comprennent les nouveaux vecteurs DSC nominaux/maximaux, la taille nulle historique, les trois tailles, l'ordre physique entrelacé et les refus avant mutation, ainsi que les essais existants du lecteur/écrivain sectoriel et de géométrie Adaptec. Commande : `dotnet test tests/RetroMediaLab.Tests/RetroMediaLab.Tests.csproj --no-restore --configuration Debug -p:Platform=x64 --filter "FullyQualifiedName~AcornWinchesterFormatTests|FullyQualifiedName~AdaptecMfm" --verbosity minimal`, puis la même en `Release`. L'avertissement CS0649 dans `Application/TestInfrastructure/SyntheticData.cs` ne concerne pas ces codecs. Ce groupe ne prouve pas une réouverture de fichiers DAT/DSC, ni une publication d'écriture interrompue via le runtime, ni un démarrage invité ; ces validations d'intégration restent requises.

`Acb4000WriteChannel` émet désormais le segment DATA du séquenceur : quinze octets de préambule, A1 à horloge manquante, F8, données de 256/512/1024 octets, ECC et deux octets de postambule. Le contenu fourni est copié à la préparation ; l'horloge consomme ensuite un tampon borné sans allocation par cellule. La porte reste active pour la dernière cellule et tombe à l'appel suivant. RESET abandonne immédiatement les cellules restantes sans inventer de sauvegarde.

Les tests comprennent le vecteur ECC indépendant du motif 6C, les trois tailles, la validité des horloges, l'intégrité des données, la propriété du tampon, les interruptions au milieu de chaque portion, le refus sans mutation et les allocations. Ils passent dans le groupe de 415 cas ci-dessous. Ce segment ne choisit pas lui-même un secteur.

Le moteur WRITE prépare le segment sans ouvrir la porte, recherche l'ID reçu au canal de lecture et n'arme la sortie que pour une identité valide, non défectueuse et correspondant au LBA. Après son ECC ID, il émet le préambule DATA et le champ ; il ne réécrit pas l'identité. L'appel direct à `IScsiStorage.Write` est retiré. READY, remplacement du support, protection et WRITE FAULT interrompent la sortie ; RESET abandonne les cellules restantes. Le bloc suivant ou la vérification ne commence qu'après le postambule. Le banc associe les données émises à l'ID observé sur les cellules de lecture et interdit tout contournement logique. Les essais exécutés couvrent aussi les ID incorrects, les trois tailles et les fenêtres interrompues. Les délais de recherche restent ouverts.

| Ensemble | Vérifications requises pour la clôture |
|---|---|
| Bus / reset | Toutes les phases, échéances, ACK maintenu/prématuré, abandon, RESET pendant commande, données et sortie physique. |
| Paramètres / adressage | Tailles 256/512/1024, dispositions séquentielles/entrelacées, limites, zéro historique DSC, deux ports et LUN invalides. |
| Lecture / ECC | ID/DATA valides et défectueux, absence de marque, doublons, mauvais ordre, reprises, chaque politique d'erreur, absence de données empruntées au mauvais ID. |
| Écriture / formatage | Cellules et ECC réellement émis, fenêtres interrompues, défauts physiques, WRITE FAULT, RESET, remplacement et protection du support ; réouverture après sauvegarde. |
| Positionnement | Trois cadences, horloges non multiples, calibration, zéro bloqué, READY absent/perdu, SEEK COMPLETE tardif, deux ports indépendants, absence de pas superflus. |
| Démarrage / commandes restantes | Chargement des paramètres depuis les ID, START/STOP, diagnostics et options documentées, recherche/translation et conditions d'erreur associées. |
| Machine / médias | Montage officiel hôte/cible/mécanisme, ADFS, DAT/DSC et EMU, accès invités et conservation des dommages représentables ; captures de résultats natifs. |
| Performances | Mesures regroupées Debug/Release du coût réel et des allocations ; vérifier que l'optimisation conserve signaux, échéances et vitesse matérielle. |

Cette matrice définit les comportements à prouver ; elle ne remplace pas le suivi des actions dans `docs/tasks/chips-validation.md`.

## Résultats déjà obtenus et limites des preuves

| Passage historique | Résultat | Portée |
|---|---|---|
| Port cible + carte hôte | 104 cas Debug/Release, zéro échec, 93/90 ms. | Inclut les 72 cas hôte ; sélection, phases, horloges, ACK, timeout et RESET. |
| Politiques de lecture / diagnostic 65 | 345 cas Debug/Release, zéro échec, 415/238 ms. | Total cumulé hôte/ACB-4000 à cette étape. |
| Canal FORMAT isolé | 359 cas Debug/Release, zéro échec, 436/274 ms. | Dernière exécution fonctionnelle rapportée du groupe ; **antérieure** au nouveau raccordement FORMAT et au positionnement. |
| Positionnement implicite et nouveaux cas | Compilation Debug/x64 réussie, 2,35 s. | Compilation seule ; aucun nouveau succès fonctionnel déduit de ce passage. |
| WRITE physique et raccordements mécaniques/FORMAT | **415 cas réussis en Debug puis Release**, zéro échec, durée rapportée 1 s par suite. | Passage groupé du 4 novembre 2026 ; inclut les canaux WRITE, FORMAT, paramètres physiques et états mécaniques ajoutés depuis le passage de 359 cas. Tests autonomes, pas un montage natif. |

Le premier passage de ce groupe a donné 376 réussites/39 échecs, puis 408/7 après adaptation des bancs manuels : ils ne fournissaient pas READY et certains recréaient une nouvelle image à chaque consultation du port. Le second passage a aussi isolé une erreur réelle du séquenceur : sur le dernier STEP, un transfert pouvait accepter l'ancien niveau SEEK COMPLETE avant que le propriétaire réagisse au front. Les transferts attendent maintenant cette réaction ; SEEK explicite conserve sa libération anticipée documentée. Les attentes de compteurs du banc tiennent désormais compte de la lecture physique des ID avant WRITE. Après ces corrections, les deux configurations passent. L'avertissement xUnit2031 dans `Shared/ArchitectureTests.cs` reste hors de ce bloc.

Commandes exécutées : `dotnet test tests/RetroMediaLab.Chips.Tests/RetroMediaLab.Chips.Tests.csproj --no-restore --configuration Debug -p:Platform=x64 --filter "FullyQualifiedName~Acb4000|FullyQualifiedName~ScsiHostAdapter" --verbosity minimal`, puis la même commande en `Release`. Aucun média externe ni nouvelle capture native n'est employé par ce groupe.

Les durées ci-dessus sont des durées de suites, pas des ns par accès ni un débit d'émulation. Aucun nouveau benchmark complet ou résultat natif n'est revendiqué. Les canaux utilisent des buffers bornés ; le séquenceur de pas et le canal FORMAT sont conçus sans allocation par tick/cellule après construction. Leur coût dans le montage final reste à mesurer.

Lors de ce passage antérieur, les preuves natives du nouveau montage et leurs captures étaient absentes ; destination prévue : `artifacts/TestsChips/<Chip>/<Machine>/`, hors Git. L'ancien `AcornScsi` n'est plus instancié par le service BBC `Winchester` dans les sources. Il reste notamment à terminer les commandes/diagnostics et échéances ouverts, valider depuis le CPU invité le WRITE sériel, la relecture des paramètres physiques et les signaux du relais dans les machines, puis à exécuter la matrice native et les mesures d'optimisation. La carte n'est pas déclarée terminée.

## Substitution dans le service BBC — 4 novembre 2026

Le service `Winchester` raccorde désormais la carte hôte, l'ACB-4000 et les signaux physiques ST-412. Les deux ports réels remplacent les quatre unités de l'ancien raccourci. Les compteurs exposent les fronts STEP et les champs DATA reçus/émis ; les diagnostics tampon ne comptent pas comme des accès disque. Les réglages de vitesse sont relayés par lecteur ; à 100 %, les délais physiques sont conservés. Les anciens réglages de latence logique sont retirés des BBC, en conservant les champs Archimedes.

Le détachement publie la fenêtre d'écriture en attente avant la sauvegarde finale puis libère les références empruntées. Il ne supprime pas le fichier image. La remise à jour des signaux de présence ne fait pas avancer le temps. Lorsque le bus est libre et sans écriture en attente, la rotation et le temps des cellules avancent arithmétiquement, avec conservation de la fraction d'horloge.

Validation groupée après cette substitution et cette optimisation : **40 cas réussis en Debug (7 s) et Release (2 s), zéro échec** dans `AcornEmuWinchesterStorageTests`. Les cas nouveaux couvrent le service BBC à 100/200/800 % et Maximum, lecture via les registres, compteurs, RESET, limite des deux ports, arrêt du moteur au détachement et conservation de l'image empruntée. Ils contrôlent aussi l'acceptation DAT et les caractéristiques des conteneurs EMU, dont un support physique non formaté. Ce sont des pistes en mémoire, pas une preuve de réouverture de fichier ni de fonctionnement invité ADFS.

Le groupe puces ACB-4000/hôte donne **534 réussites en Release (2 s), zéro échec**. Le nouveau cas des statistiques avait également réussi seul en Debug (254 ms). Ces durées sont celles des suites, pas une mesure de vitesse native. Les diagnostics internes, échéances, fichiers médias, captures et performances du montage restent ouverts.

### Bornes des profils Winchester après substitution

Les profils personnalisés proposés avec des secteurs de 256 octets sont limités à 2048 cylindres × 16 têtes × 33 secteurs, soit **276 824 064 octets (264 Mio)**. Le plafond de 512 Mio provenait de l'adressage logique de l'ancien contrôleur, sans garantir une géométrie réalisable. Les images DAT doivent aussi respecter les bornes de cylindres/têtes, les secteurs de 256 octets et les dispositions de 32/33 secteurs par piste ; le contrôle de capacité existant est conservé. Le chemin EMU conserve l'acceptation des pistes physiques non formatées.

Les neuf cas ciblés (bornes DAT, plafond des six profils et conservation de DAT/refus d'un autre conteneur) passent en **Debug, 122 ms, puis Release, 141 ms**, zéro échec. Ce contrôle d'acceptation ne remplace pas la validation de la création avec géométrie explicite et la réouverture des fichiers, encore requises dans l'intégration médias.

## RECEIVE DIAGNOSTIC et mémoire interne — examen du 4 novembre 2026

Le refus sans dump préparé passe désormais avant le chemin disque : pas de calibration, pas d'acquisition des paramètres ni de données inventées. Le firmware teste son autorisation en 1B34–1B37 et appelle 175A (sense 20h) en son absence. Les huit cas ciblés passent en **Debug (36 ms) puis Release (33 ms)**, zéro échec : chacun des deux ports avec lecteur prêt, non prêt, non formaté ou absent. La commande RECEIVE valide avec un dump, les dumps 61h/62h et patchs 63h/64h restent à réaliser.

### Adresses établies dans le firmware 400108-00

| Adresse / séquence | Observation directe | Conséquence pour la réalisation |
|---|---|---|
| 0055–005A, routine 00ED | Efface E0h octets à partir de 8000h ; SP est fixé à 8100h. | Le bas de RAM et la zone de pile n'ont pas la même politique d'initialisation. Un dump systématiquement rempli de zéro serait faux. |
| 80AF/80B0 et 015E–0176 | Pointeur du contexte lecteur ; copie de 2Fh octets de 8080h vers le contexte désigné. À l'initialisation, les bases désignées sont 8000h puis 8040h. | Les contextes des deux lecteurs et la zone de travail doivent partager leur état avec les commandes ; pas une mémoire de diagnostic isolée. |
| 8098/8099, 1AE9–1AFA | Le dump mémorise le pointeur 4000h ou 8000h et positionne le bit 04h à 809Ch. | La réponse doit lire la zone réellement sélectionnée. |
| 8084/8085, 1B76–1BAF | Compteur restant de réception/émission ; chaque octet décrémente le mot. | Le transfert dépend de la longueur annoncée, avec fin sans lecture d'octets excédentaires. |
| 1B0B–1B1F | Patch à partir de 4000h ou 8000h, octet bas fourni ; incrément du pointeur après chaque écriture. Le compteur de huit bits nul parcourt 256 écritures. | Le débordement de page suit l'adresse incrémentée ; ne pas rabattre artificiellement chaque écriture dans une page de 256 octets. |
| 1B46–1B5B | Réponse : 01h, 04h puis octets haut/bas du pointeur mémorisé. | En-tête distinct des octets lus dans les registres/RAM. |
| 1B5F–1B69 | D est mis à zéro, décrémenté avant le test de fin et avant chaque émission depuis M. | Cette séquence paraît émettre 255 octets de mémoire ; le manuel décrit une réponse de 104h octets. Cette divergence doit être tranchée avant de figer un test de longueur du dump. |
| 1AC7–1AD0 | Le firmware vérifie 30h pour toutes les options sauf 65h, alors que le texte OEM rattache le qualificateur aux patchs. | Ne pas fusionner silencieusement les politiques de versions ni copier uniquement la description du manuel. |

Ces observations viennent du désassemblage des séquences indiquées de la copie dont l'empreinte figure dans les références. Elles ne suffisent pas encore à décrire le décodage et les effets des registres matériels 4000h–40FFh, ni tous les octets de pile/contexte. Le modèle actuel traduit les commandes en états numériques sans exécuter l'8085 du contrôleur ; fournir deux tableaux détachés de ces états ne réaliserait donc pas les diagnostics matériels. Le raccordement de cette mémoire reste ouvert, avec ses effets sur les vraies commandes.

## Preuve invitée BBC B obtenue après substitution

Le 4 novembre 2026, le vrai CPU BBC B avec MOS 1.20, BASIC II et ADFS 1.30 a exécuté CAT, SAVE et LOAD sur le nouveau montage physique, pour les conteneurs **DAT/DSC et EMU**. Après détachement et sauvegarde, une nouvelle machine a rechargé le fichier depuis l'image rouverte en lecture seule. Chaque octet du motif de 256 octets est vérifié, ainsi que des compteurs physiques de lecture/écriture non nuls dans la première session et zéro bloc écrit dans la seconde.

Après retrait des résolutions SCSI répétées sans changement d'entrée, le groupe stockage/relais et ces scénarios donne **50 réussites en Release**, zéro échec, 29,62 s ; EMU termine en 16 s, DAT/DSC en 11 s. En Debug, **49 cas sur 50 passent** : les 48 cas du relais et le scénario DAT/DSC complet (1 min 23 s). EMU atteint encore la limite de sécurité de 90 s au catalogue final, après le rechargement dans la seconde session. L'optimisation n'est donc pas déclarée close ; cette limite et les étapes mesurées sont détaillées dans [AcornWinchester.md](../../machines/AcornWinchester.md).

Captures inspectées, hors Git : `artifacts/TestsChips/Acb4000/BbcMicroModelB/Release/adfs-native-dat-reopened.png` et `adfs-native-emu-reopened.png`. Elles montrent le catalogue invité contenant CHIPTEST après réouverture. Les captures Debug sont séparées, et un état affiché lors d'un délai dépassé n'est pas présenté comme le succès du scénario complet.

Le diagnostic temporaire est supprimé, ses seize fichiers médias de travail également ; les assemblies de tests sont reconstruites en Debug et Release, zéro erreur (trois avertissements préexistants hors de ce composant). Aucune configuration ni ROM utilisateur n'a été modifiée. Ces builds de tests ne sont pas un build Debug All livré. Les autres modèles, le deuxième port depuis ADFS, les interruptions avec réouverture, les fonctions App, les diagnostics de mémoire interne, les échéances et les mesures d'optimisation finales restent à terminer.

### Cumul des horloges sans événement — 4 novembre 2026

Le relais peut désormais différer les ticks seulement lorsque le contrôleur n'a ni commande/bus ni positionneur actif et que les lecteurs n'ont ni stabilisation ni fenêtre d'écriture en attente. Il cumule les ticks matériels après application de la vitesse du lecteur. Avant lecture/écriture de registre, changement de vitesse/lecteur ou RESET, il restitue les fractions d'horloge, la phase de rotation et le temps des cellules par arithmétique entière. Un plafond d'une seconde virtuelle borne le cumul ; ce plafond ne change pas la cadence matérielle. Toute sélection nouvelle ou activité mécanique interdit le cumul.

Les **53 cas du stockage/relais passent en Debug (11 s) et Release (2 s), zéro échec**. Les cinq nouveaux cas comparent les octets et le nombre exact de ticks d'une transaction après repos exécuté pas à pas ou cumulé : 1, 23333 et 100001 ticks, deux ports et horloge de 2000003 Hz. Le cumul est aussi refusé pendant une commande et pendant la stabilisation après SEEK, puis autorisé une fois celle-ci réellement terminée. Ces durées de suites ne sont pas encore une mesure du gain dans le CPU BBC invité.

### Vérification invitée après cumul des cycles au repos

Le parcours de deux sessions précédemment utilisé est rejoué après cette optimisation, avec les mêmes commandes, attentes en frames, motif index XOR 5Ah et limite de sécurité de 90 s. **Les deux cas passent maintenant en Debug et Release.** Les durées ci-dessous sont les temps jusqu'à la fin des assertions invitées, démarrant après création du conteneur ; elles n'incluent pas la publication/fermeture finale ni les autres coûts du runner.

| Conteneur | Debug, première session | Debug, après réouverture | Release, première session | Release, après réouverture |
|---|---:|---:|---:|---:|
| DAT/DSC | 40,512 s | 66,186 s | 7,056 s | 10,259 s |
| EMU | 39,938 s | 65,824 s | 5,041 s | 8,398 s |

Dans chaque format/configuration, la première session lit 45 blocs et en écrit huit ; la seconde lit 43 blocs, sans écriture. Les 256 octets rechargés correspondent tous au motif enregistré. Le cas EMU Debug dure 1 min 22 s au total lorsque la création/publication/fermeture sont incluses, contre 65,824 s pour les opérations invitées chronométrées. Le groupe des deux cas dure 2,51 min en Debug et 23,83 s en Release. Le précédent dépassement Debug de 90 s n'est plus reproduit dans ce passage ; aucune limite n'a été relevée.

Les captures Debug et Release de `adfs-native-dat-reopened.png` et `adfs-native-emu-reopened.png` sous `artifacts/TestsChips/Acb4000/BbcMicroModelB/` sont actualisées. Elles montrent le vrai catalogue ADFS après rechargement dans la seconde machine. Ces preuves restent limitées à BBC B et au premier port ; les diagnostics internes, échéances de panne, autres modèles/ports, interfaces App et mesures complètes d'optimisation restent ouverts. Les temps de scénarios ne sont pas annoncés comme une fréquence CPU ou une cadence FPS générale.

### Recherche d'une référence d'horloge complémentaire

La compilation de documents Atari disponible sous le nom `atari-sm144-service-manual.pdf` contient, page PDF 48, les diagnostics de la carte ACB-4000 et identifie notamment le sous-système 8085, la RAM 8156 et AIC-010/AIC-300. Elle ne donne pas la fréquence de l'8085 de la révision 400108-00. L'examen visuel des pages PDF 36/37 montre une carte d'imprimante laser et son RESET, pas l'ACB-4000 : ces schémas ne peuvent pas fournir l'horloge recherchée. Source constructeur conservée en copie de travail hors Git : https://oldcrap.org/wp-content/uploads/2023/04/atari-sm144-service-manual.pdf . Aucun nombre de cycles BBC n'est substitué à une fréquence interne non établie.

L'examen du firmware 400108-00 en 18BF–1931 établit en revanche la reprise DATA : 1906 appelle 15D1 à chaque nouvelle tentative ; 15D1–15E6 recharge le budget des ID à quatre fois le nombre de secteurs. Ce budget n'est donc pas commun à toutes les tentatives ECC d'un bloc. Les tests de piste doivent inclure les autres ID entre deux occurrences du secteur visé pour couvrir ce cas.

### Budget d'ID rechargé à chaque reprise DATA

La correction recharge désormais le budget de recherche à chaque reprise DATA de READ et SEARCH DATA. Pour READ, le retour firmware 1906 vers 15D1 est identifié : la recherche d'identité est réarmée pour la nouvelle lecture. Une tentative ECC ne consomme plus le budget laissé par la précédente. Les six lectures défectueuses prévues restent possibles lorsque les autres identités de la piste sont réellement reçues entre les occurrences du secteur visé.

À l'autre borne, le dernier ID cible autorisé peut encore fournir son DATA. Si un nouvel ID remplace ce champ DATA absent alors que le budget est déjà épuisé, la commande se termine avec le statut de recherche correspondant ; répéter indéfiniment le même ID cible ne maintient plus la commande en lecture. Cette correction est fondée sur les champs reçus : elle n'implémente pas l'expiration en l'absence totale de cellules/marques ou de front INDEX, toujours ouverte.

Validation groupée : **549 cas passent en Debug (20 s) et Release (2 s), zéro échec**. Sept cas ajoutés couvrent READ court/long avec les 32 autres ID entre chaque reprise, SEARCH DATA après erreur DATA au dernier essai d'ID puis réussite de sa troisième lecture, et READ/VERIFY avec DATA présente ou absente après le dernier ID cible. Le cas d'absence a ensuite été renforcé en répétant exactement le même ID cible plutôt qu'un autre : les quatre cas de bornes repassent en Debug (229 ms), et la suite complète Release de 549 cas inclut ce renforcement. Les essais emploient les cellules MFM du banc, sans fichier ni accès logique de remplacement. Aucun nouveau test invité sur fichier endommagé ni nouvelle capture n'est déduit de ce groupe.

### Compteurs temporels des boucles de polling — composant écrit, raccordement ouvert

`Acb4000SearchDeadline` représente les échéances à l'entrée des boucles 15BF et 15EE, après chargement des registres. Le compteur exige deux cadences explicites : ticks du propriétaire et T-states du séquenceur. Aucune fréquence de carte n'est choisie par défaut. La fréquence du BBC n'est donc pas assimilée à celle de l'8085.

Décompte des chemins sans événement du firmware 400108-00 :

- 15BF : LDA 13 + ANI 7 + JNZ non pris 7 + DCX 6 + MOV 4 + ORA 4 + JNZ pris 10 = **51 T-states par itération**. Le dernier JNZ non pris économise trois T-states. Avec 2100h itérations : **430845 T-states**.
- 15EE : MOV depuis mémoire 7 + MOV registre 4 + ANI 7 + JNZ non pris 7 + DCR 4 + JNZ pris 10 = **39 T-states par itération**. Après économie du dernier saut : **621 T-states** pour les seize premiers polls, **9981 T-states** pour 256 polls.

Les instructions sont identifiées dans le firmware ; la table 8085 et le traitement des sauts de [MAME i8085.cpp](https://raw.githubusercontent.com/mamedev/mame/master/src/devices/cpu/i8085/i8085.cpp) ont servi de comparaison pour les coûts 7/10 des sauts. Cet accord ne constitue pas une mesure indépendante de la carte. Les attentes externes de mémoire/bus, les instructions précédant/suivant la boucle et le moment de reconnaissance du statut ne sont pas inclus dans ces comptes ; ils appartiennent au raccordement du séquenceur.

Le calcul conserve le temps sous forme entière à deux cadences. Une expiration est consommée une seule fois. Après une fenêtre FIELD expirée, la fenêtre suivante conserve le dépassement fractionnaire ; un tick grossier peut ainsi couvrir plusieurs fenêtres, consommées sans avancer une seconde fois le temps. Un nouveau Begin ou RESET abandonne la précédente attente. Les ticks n'allouent pas.

**12 cas réussissent en Debug (278 ms)** ; le groupe complet **561 cas réussit en Release (3 s)**, zéro échec. Les vecteurs vérifient les frontières INDEX/premier champ/champs suivants, une horloge propriétaire de 2000003 Hz, l'absence de dérive sur dix secondes avec plusieurs expirations par tick, les transitions/refus et RESET, et zéro allocation sur 100000 ticks après échauffement. Les fréquences 1/2/3 MHz employées dans ces vecteurs sont des entrées de banc et ne sont pas annoncées comme des mesures de la carte.

Ce compteur n'est pas encore appelé par le moteur ACB-4000 ou le relais natif : la fin automatique sans événement n'est donc pas annoncée fonctionnelle. Restent le raccordement des états INDEX/ID/DATA, les durées des instructions intermédiaires et la cadence de la révision de carte. Les preuves invitées de lecture/écriture déjà obtenues ne prouvent pas ces nouvelles expirations.


## Horloge identifiée sur une ACB-4000 originale

Les photographies Bitsavers `ACB4000.JPG` et `ACB-4000_t.jpg`, obtenues le 4 novembre 2026 via leur miroir, distinguent deux cartes originales, pas une ACB-4000A. La première porte **ACB 4000, ASSY 400006-03, REV-B** : le quartz Y1 placé au 8085 porte **5.000**, soit 5 MHz. L'horloge interne du 8085 divise cette fréquence par deux : la référence de cette révision est donc **2,5 MHz en T-states**, indépendante du bus propriétaire BBC à 2 MHz. La seconde porte ASSY 400106-00, révision G ; son oscillateur Y2 à 10 MHz se trouve au circuit de données AIC-250 et ne prouve pas la cadence du 8085 de cette autre révision.

Sources photographiques : [ACB4000.JPG](https://ftpmirror.your.org/pub/misc/bitsavers/pdf/adaptec/ACB-4000/ACB4000.JPG), [ACB-4000_t.jpg](https://ftpmirror.your.org/pub/misc/bitsavers/pdf/adaptec/ACB-4000/ACB-4000_t.jpg). Copies examinées sous `artifacts/References/Machines/Acorn/Acorn Winchester/acb4000-board.jpg` et `acb4000-top.jpg`, hors Git. La carte de référence est explicitement la révision B ; cela ne démontre pas la fréquence de toutes les variantes produites.

Le compteur INDEX 15BF représente 430845 T-states hors LXI D initial ; la première boucle de champ 15EE représente 621 T-states, les suivantes 9981. Le chemin INDEX détecté 15E1–15EB ajoute 51 T-states avant la première boucle (LDA/RAL/RAL/STA/MVI/LXI). La préparation INDEX 15BC ajoute 10 T-states. Les autres instructions du firmware et les attentes AIC ne sont pas assimilables à ces seules boucles ; leur fidélité cycle par cycle n'est pas démontrée par le modèle de commandes actuel.


## Raccordement des recherches sans événement — 4 novembre 2026

La cible possède désormais son `Acb4000SearchDeadline`, avec horloge propriétaire et cadence du séquenceur distinctes. Le propriétaire BBC transmet explicitement 2,5 MHz pour la carte de référence révision B. Les recherches de paramètres et TRANSLATE arment INDEX ; après INDEX, la première fenêtre inclut les 51 T-states de préparation établis. READ, VERIFY et la recherche d'ID avant WRITE arment la fenêtre de champ. Les champs effectivement décodés réarment l'attente ; les fenêtres expirées sans champ consomment le budget fini de recherche, avec conservation du dépassement fractionnaire. DATA tardif ne peut pas utiliser une ancienne identité ayant expiré.

RESET, fin de commande et transfert vers l'hôte désarment le compteur. Les phases de déplacement ne consomment pas une échéance de champ ; le prochain lancement de lecture réarme la recherche. L'acquisition de paramètres sans INDEX termine via le résultat de format invalide existant ; TRANSLATE sans INDEX restitue le sense 01h. Les paramètres ne sont pas récupérés directement dans le conteneur pour contourner cette recherche.

Validation : **568 cas ACB-4000/hôte réussis en Release (6 s)** et **57 cas de configuration/relais Acorn réussis en Debug (11 s)**. Les sept nouveaux cas réussissent aussi en Debug (837 ms) : READ/VERIFY sans champs, TRANSLATE sans INDEX, acquisition sans INDEX, DATA tardif, réarmement par un champ et attente ACK hôte, RESET. Le passage Debug groupé précédant la correction finale du banc donnait 566/567 : le banc sans INDEX renouvelait d'abord son image à chaque accès, puis n'avançait pas les signaux de calibration ; il conserve maintenant une image unique et coupe INDEX seulement après la calibration. Le nouveau test d'ACK attend aussi la propagation REQ du port au lieu de la supposer instantanée.

Ces essais ne démontrent pas une exécution cycle par cycle de toutes les instructions du firmware 8085 : les temps des boucles et les préparations précisées sont raccordés, mais les chemins d'acquittement AIC et toutes les instructions intermédiaires ne sont pas encore représentés. L'attente INDEX de FORMAT n'est pas encore raccordée à ce compteur. Les acquisitions avec erreurs répétées d'ID, les autres cas de reprise/recalibration et les diagnostics mémoire 61h–64h doivent rester ouverts jusqu'à vérification complète. Les preuves invitées antérieures ne sont pas présentées comme une nouvelle validation native de ces modifications ; aucun nouveau screenshot invité ni build Debug All livré à cette étape.


## FORMAT et reprises d'identités : chemins établis dans le firmware

Le chemin FORMAT 0B00–0CB2 appelle la préparation 146F et le distributeur 0DA6. Le démarrage de piste passe par 1498 : **14A9–14AC attend le bit INDEX 20h sans compteur**, contrairement à 15BF utilisé pour la recherche. La fin de piste attend aussi le séquenceur en 0C5C–0C61 sans compteur. Aucune expiration arbitraire de recherche ne doit donc être appliquée à FORMAT tant que le lecteur reste prêt ; RESET et les défauts matériels restent les moyens d'interrompre ces attentes. Le canal d'émission existant conserve ce comportement. Ce n'est pas une raison de masquer une perte READY ou WRITE FAULT.

Pour les paramètres, 1576 mémorise l'ordinal demandé en 80BB. 1599–15A5 initialise **quatre tentatives** en 80BA. 163C incrémente l'ordinal physique avant 1654–1658 ; le CRC n'est examiné que pour l'ordinal demandé (165B–165E). Après CRC incorrect, 1666–166A décrémente ce compteur et retourne en 15A8 pour rechercher à nouveau depuis INDEX. À la quatrième erreur, 166D renvoie 90h. Les ID précédant l'ordinal demandé ne doivent pas invalider cette reprise à cause de leur CRC : leur position est comptée, sans relire les paramètres déjà acquis. Le canal actuel, qui abandonne tout le groupe après un ID erroné sans limite de reprises, doit être corrigé sur ces points.


### Résultat des reprises CRC et des attentes FORMAT

Le canal conserve les drapeaux des paramètres déjà acquis et reprend l'ordinal manquant depuis INDEX ; les ID antérieurs sont comptés sans exiger à nouveau leur CRC. Chaque paramètre reçoit quatre vérifications indépendantes ; la quatrième erreur termine l'acquisition invalide. Une réussite réarme le compteur et le budget de recherche pour l'ordinal suivant. Les ID contenant des valeurs de paramètres invalides restent distincts des ID dont le CRC est incorrect.

**574 cas ACB-4000/hôte passent en Debug (24 s) et Release (5 s), zéro échec.** Les six cas ajoutés couvrent : quatrième erreur CRC et nouveau Begin, neuf ordinals chacun réussi au quatrième essai avec CRC précédents invalides, FORMAT maintenu sans INDEX au-delà de deux délais de recherche puis repris ou réinitialisé, perte READY et WRITE FAULT pendant cette attente. Aucun délai artificiel n'a été ajouté au chemin FORMAT. Ces tests sont autonomes ; ils ne constituent pas de nouvelles captures invitées.


## Parcours invité après raccordement des échéances et reprises CRC

Le 4 novembre 2026, le diagnostic temporaire a exécuté sur le vrai CPU BBC B, avec MOS 1.20/BASIC II/ADFS 1.30 et le fichier de caractères SAA5050 existants : démarrage, `*ADFS`, `*CAT`, `*SAVE CHIPTEST 3000 +100` et `*LOAD CHIPTEST 3100`. Les 256 octets chargés ont été comparés au motif `index XOR 5Ah`, avec compteurs physiques de lecture et écriture non nuls. Après détachement, publication et fermeture du DAT/DSC, une nouvelle machine a rouvert l'image en lecture seule, exécuté `*LOAD CHIPTEST 3200` puis `*CAT` ; les 256 octets correspondent, avec des lectures physiques et zéro bloc écrit dans cette seconde session.

**Le cas natif Release passe en 10 s** (11,664 s pour la série VSTest). Ce temps est celui du scénario, pas une mesure de MHz ni une preuve générale d'optimisation. Conteneur de travail indépendant : 306 cylindres, quatre têtes, 33 secteurs de 256 octets. Aucun fichier ROM, configuration ou média utilisateur n'a été modifié ; aucune sortie audio Windows n'a été ouverte.

La capture inspectée `artifacts/TestsChips/Acb4000/BbcMicroModelB/Release/adfs-search-timing-reopened.png` montre le catalogue ADFS contenant CHIPTEST après réouverture ; `adfs-search-timing-first-load.png` correspond à la première session réussie. Les vérifications d'octets sont effectuées par le diagnostic, pas déduites de la capture. Une première tentative s'était arrêtée sur un nom de fichier SAA5050 incorrect, puis le banc de saisie envoyait toutes les lettres sur la touche A (`*A`, Bad command) : ces défauts du diagnostic ont été corrigés, sans changer le clavier émulé ni le contrôleur pour contourner le test. La capture du premier LOAD a été actualisée au passage réussi.

Cette nouvelle preuve concerne DAT/DSC, BBC B, port 0 et Release. Elle ne remplace pas les validations restantes des autres modèles/ports, EMU après les dernières modifications, écritures interrompues avec réouverture, ni les diagnostics internes mémoire. Les captures restent hors Git. Le diagnostic temporaire et ses deux fichiers `search-timing-native.dat`/`.dsc` sont supprimés après ce passage ; les assemblies Debug et Release sont reconstruites pour retirer son code.


### Niveau INDEX déjà actif au début de TRANSLATE

Le démarrage du canal TRANSLATE reconnaît désormais le niveau INDEX déjà actif à son entrée, comme le test du bit 20h en 15BF. Un niveau relâché avant l'entrée n'est pas utilisé ; le maintien d'un niveau haut n'est pas traité comme un nouveau front à chaque échantillon et ne remet pas l'ordinal physique à zéro. RESET abandonne toujours le descripteur en cours.

**19 cas ciblés de traduction/INDEX passent en Debug (1 s), zéro échec.** Le premier passage donnait 18/19 : le nouveau banc attendait 470 pour le deuxième ID entrelacé, au lieu de 150 + 256 + 54 = 460. Cette attente a été corrigée ; l'espacement de production n'a pas été changé. Le passage Release groupé de ces trois nouveaux cas reste à intégrer à la validation finale.


## Transferts de diagnostic mémoire raccordés au bus de carte

Les commandes 61h–64h utilisent désormais un bus mémoire emprunté `IAcb4000MemoryBus` : le propriétaire fournit les lectures/écritures de la RAM et des registres avec leurs effets réels. Aucun tableau de zéros n'est substitué à une carte absente. Sans ce bus, la cible refuse ces commandes ; **le montage natif doit encore fournir son espace mémoire avant que ces diagnostics puissent être déclarés fonctionnels sur BBC**.

SEND prépare le dump matériel 4000h ou RAM 8000h, après vérification du qualifiant 30h du firmware de référence. RECEIVE exige le dump immédiatement précédent du même LUN ; une autre commande, RESET ou une consommation antérieure l'invalide. L'allocation CDB limite la réponse. Les adresses matérielles sont lues seulement à mesure des ACK de l'hôte, après l'en-tête, sans lecture spéculative des registres non demandés.

La longueur suit explicitement le firmware **400108-00** : l'en-tête annonce 0104h, mais la boucle 1B5F–1B69 initialise D à zéro puis fait DCR D / RZ avant chaque émission. Elle émet donc 255 octets mémoire au maximum, soit 259 octets en comptant l'en-tête. La documentation OEM décrit 260 octets ; le modèle conserve ici le comportement de ce binaire, sans le présenter comme une mesure sur matériel ni l'attribuer à toutes les révisions. L'allocation hôte peut encore tronquer cette réponse.

PATCH interprète le compteur nul comme 256 octets, incrémente l'adresse complète de 16 bits en franchissant la page et ne demande pas les octets excédant son compteur ou la longueur CDB. Chaque octet accepté est écrit immédiatement sur le bus fourni. RESET arrête les octets restants sans annuler les écritures déjà réalisées ; la politique de réinitialisation de la RAM elle-même appartient à son propriétaire.

**603 cas ACB-4000/hôte passent en Debug (22 s) et Release (3 s), zéro échec.** Les 26 nouveaux cas couvrent zones matériel/RAM, allocations 0/1/4/5/259/260/1024, ordre des effets de lecture après ACK, modification d'un registre avant sa lecture, longueur de PATCH, franchissement de page, compteur nul, RESET partiel, invalidation et refus sans bus ou avec qualifiant incorrect. L'ajout des diagnostics n'est pas confondu avec la réalisation de leur espace mémoire natif : les espaces RAM/ASIC de la carte et leurs effets sur les commandes doivent encore être raccordés. Aucun nouveau screenshot invité n'est revendiqué pour ces bancs autonomes.

Le parcours 193F n'est pas une recalibration du lecteur : 01FD incrémente un compteur d'erreur et 0276 copie quatre octets de la table 1FC7 vers 4074–4077. Aucun appel REZERO n'apparaît dans ce chemin. Il faut conserver cette distinction pour le raccordement des registres AIC, au lieu d'ajouter des mouvements mécaniques supposés.


Après recompilation du module avec ce contrat mémoire, les **58 cas de configuration/relais Acorn passent en Release (3 s)**, zéro échec. Le groupe de 603 cas Release comprend aussi les trois nouveaux cas TRANSLATE/INDEX dont la validation Release était encore indiquée comme à faire dans la section précédente. Les avertissements de compilation des autres modules et des anciens bancs restent distincts du résultat de ces tests.


## Décodage mémoire établi dans le firmware 400108-00

Ces adresses appartiennent au processeur de la carte ACB, et non au bus mémoire du BBC. Les références ci-dessous sont les adresses des instructions dans le binaire de référence déjà conservé pour l'examen.

| Zone | Fonction effectivement établie | Référence du binaire |
| --- | --- | --- |
| 8000h–802Eh, 8040h–806Eh | Contextes des deux unités ; copie des 47 octets de travail depuis 8080h | 015Eh–0176h ; pointeur en 80AFh |
| 8080h–80AEh | Contexte de travail de l'unité courante | 0164h–0176h |
| 8084h–8085h | Compteur de transfert restant, décrémenté avant chaque octet des diagnostics | 1B76h et 1B93h |
| 8098h–8099h | Adresse de départ préparée par SEND pour RECEIVE DIAGNOSTIC | 1AF2h, 1B46h |
| 809Ah, bits 3–4 | Politique de reprise/correction DATA, modifiée par l'option 65h sans écraser les autres bits | 1B22h–1B30h ; lecture en 18C5h |
| 809Ch, bit 2 | Disponibilité du dump préparé | 1AF5h ; contrôle en 1B34h |
| 80C5h | Compteur des tentatives de relecture DATA | 18D2h, 192Ah |
| 80C7h, 80CEh | Seuil de journalisation et compteur d'erreurs utilisé après épuisement des tentatives | 01FDh–0214h, 193Fh |
| 4053h | Commande/acquittement du transfert hôte ; le firmware attend le bit 20h | 01C8h–01D3h, 01F3h–01FAh |
| 4059h | Écriture préalable aux échanges d'octets hôte | 01C1h, 01DAh, 01E8h |
| 4070h | Port de données hôte ; sa valeur antérieure est conservée puis rétablie autour du transfert | 01BFh–01FAh |
| 4071h | Contrôle du traitement de champ/ECC ; séquences C0h/C4h/C8h | 18CDh–18E7h, 194Bh–1962h |
| 4074h–4077h | Quatre octets de configuration chargés par la routine 0276h | 0279h–0281h ; table 1FC7h au retour 193Fh |
| 4079h | État du séquenceur ; bit INDEX 20h et bit de contrôle d'intégrité 04h effectivement exploités | 15BFh, 190Ch–1919h |
| 407Eh | Commande de phase du port hôte | 0178h–018Dh, 1B3Ah–1B3Ch |

Au démarrage froid, 0055h–005Ah efface E0h octets à partir de 8000h et initialise SP à 8100h. Ce n'est pas la preuve que tous les octets de 8000h–80FFh sont nuls à tout instant, ni qu'un RESET du bus équivaut à ce démarrage froid.

Conséquence pour le raccordement : une copie de diagnostic indépendante des compteurs, contextes et commandes ne serait pas fidèle. L'espace de carte doit appartenir à Chips ; le module Acorn doit seulement raccorder les fils hôte et lecteurs. Les comportements non encore décodés des registres 4000h ne sont pas remplacés par des valeurs arbitraires. La création de cet espace vivant et le remplacement des états parallèles restent à réaliser ; ce tableau ne clôture pas les diagnostics natifs.


## RAM possédée par la carte et contexte DATA réellement utilisé

La carte possède maintenant sa page RAM 8000h–80FFh. Les diagnostics RAM 62h/64h n'exigent plus un bus externe ; ils accèdent à cette RAM, dont l'octet de politique DATA est directement utilisé lors des lectures ECC. L'ancien tableau parallèle de politiques par unité est supprimé. L'interface empruntée devient `IAcb4000RegisterBus` : elle concerne les registres matériels, pas une RAM de substitution.

Le décodage 05FAh–0615h précise la distinction entre deux routines : 015Eh sauvegarde 47 octets depuis 8080h, mais le changement d'unité échange les **32 octets 808Fh–80AEh**, en conservant les octets de commande 8080h–808Eh. Le pointeur RAM 80AFh commande cet échange ; il n'est pas doublé par un index indépendant. PATCH peut ainsi modifier un contexte sauvegardé, qui est réellement rechargé quand l'unité correspondante est sélectionnée. L'option 65h préserve les autres bits de 809Ah ; les deux bits de politique sont interprétés séparément, y compris si PATCH active les deux.

Le RESET du bus ne passe pas par l'entrée froide 0055h : il ne réinitialise plus artificiellement les politiques DATA. Les deux unités les conservent après RESET. Le test qui attendait leur effacement a été corrigé à partir de l'entrée firmware 0024h–0049h. Le démarrage froid initialise séparément les valeurs établies par 0055h–0075h et le pointeur de contexte.

Le premier passage de ce bloc donnait 604/609 : trois attentes de dump RAM avaient semé une valeur arbitraire dans le pointeur de contexte 80AFh, provoquant légitimement un échange au prochain accès ; un banc fournissait encore deux octets après un refus anticipé ; l'ancien cas RESET attendait un effacement absent du chemin firmware. Après correction de ces attentes et ajout du cas PATCH activant les deux bits : **610/610 cas ACB-4000/hôte réussis en Debug, durée de série 26,63 s**.

Ce résultat ne clôture pas la carte : les registres AIC natifs restent à raccorder. Sans leur bus, les diagnostics matériels 61h/63h sont refusés ; un PATCH RAM débordant au-delà de 80FFh est également refusé avant les données, au lieu d'inventer les registres suivants. Les autres compteurs/variables firmware et les effets de pile doivent encore être raccordés avant de déclarer tout le dump fidèle au firmware. Aucun screenshot supplémentaire n'est attribué aux tests autonomes de ce bloc.


Le passage groupé suivant réussit également : **610/610 en Release (3 s)** et **58/58 tests de configuration/relais Acorn en Release (2 s)** après recompilation du module contre le nouveau contrat de registres. Les seuls changements après le passage Debug sont le nom du banc de registres et la mise en constantes des offsets déjà testés. Aucun média utilisateur, processus graphique ou sortie audio Windows n'a été ouvert par ces séries. Aucun commit de composant terminé n'est effectué à ce stade.


### Registres internes du séquenceur et ports de carte

La planche constructeur AIC-010 Rev B (pages PDF 11, 13–25) distingue :

- 71h : entrée série ECC (01h), impulsion de décalage (02h), désactivation du retour (04h), effacement maintenu (08h), branchement secteur (10h), RESET maintenu (20h), longueur (C0h).
- 72h/73h : lecture du syndrome ; 74h–77h : programmation du polynôme, conservée au RESET matériel.
- 78h/79h : branchement, lancement et état ; 7Ah : commande et événements INDEX/SECTOR/NRZ, avec acquittement à la lecture.
- 7Bh–7Fh : marques, GPIO, horloges et dépilage.
- 80h–97h, A0h–B7h, C0h–D7h et E0h–F7h : les quatre colonnes des 24 instructions RAM du séquenceur.

Les adresses 50h/51h/70h sont des ports externes décodés : leur contenu n'est pas un registre interne AIC-010. Il ne faut donc pas remplir toute la page 4000h avec un simple tableau de registres. Les données ECC et les statuts doivent être ceux des canaux actifs.

La configuration 74h–77h C2h/20h/82h/20h donne le polynôme 41044185h par décalage des taps et ajout du terme constant. Le mode de longueur 10b est explicitement anormal dans la documentation ; son résultat exact n'y est pas spécifié.


### Bloc de registres ECC réalisé, raccordement natif encore ouvert

`Acb4000EccRegisters` réalise les écritures de contrôle, les quatre registres de taps, les lectures du syndrome et les horloges de données/de décalage logiciel. Le polynôme survit au RESET externe ; ce RESET maintient le bloc arrêté jusqu'à sa libération par le contrôle. L'effacement reste actif tant que son bit est maintenu. Les impulsions logicielles attendent une horloge RD/REF, avec portes RG/WG inactives ; un deuxième front sans nouvelle demande ne décale pas de nouveau.

**22/22 tests du bloc réussissent en Debug (50 ms de série)**. Les vecteurs indépendants ACB-4000 A1FE00000000, A1FE00123480 et 123456789 correspondent aux résultats déjà établis ; les modes documentés 16/24 bits sont également confrontés aux vecteurs CRC initialisés à zéro. Le chargement/déchargement série vérifie les bits faibles, le bit OR du registre 72h, les taps non réinitialisés et le bit inutilisé de 77h. La boucle d'horloge testée n'alloue aucun objet.

Ce bloc ne modifie pas encore les canaux physiques : il doit devenir leur état ECC, puis être accessible via les registres de la carte. Il ne constitue donc pas une preuve de diagnostics ECC natifs sur BBC. Le mode de longueur 10b est signalé comme non documenté ; les méthodes `TryClock` le refusent explicitement sans calculer un syndrome supposé. Le traitement matériel exact de ce mode, les fronts interdits et le raccordement au séquenceur restent distingués des modes nominaux prouvés.

Les trois rendus temporaires de la documentation AIC-010 utilisés pour relever les positions des bits ont été supprimés après transcription. Les tests permanents de ce bloc n'utilisent aucun fichier, DLL externe, fenêtre ou média temporaire.


### Registres ECC raccordés aux champs physiques

Les lecteurs de champs et d'ID de paramètres partagent maintenant le bloc ECC de la carte. Les octets réellement décodés depuis les cellules alimentent son syndrome ; les contrôles des commandes utilisent cet état au lieu de refaire un contrôle indépendant à polynôme fixe. Les codecs séparent le décodage d'un champ et son intégrité. La correction, les sorties DATA et FORMAT acceptent les taps programmés. Le polynôme nominal conserve la table de calcul partagée, sans nouvelle allocation par octet.

PATCH matériel accepte nativement les écritures 4071h–4077h ; les écritures dans 4072h/4073h, registres de lecture du syndrome, n'altèrent pas celui-ci. Le contrôle RESET de l'AIC bloque les portes de lecture/écriture. La routine carte 1D9Eh–1DC1h, atteinte après RESET du bus, réinitialise l'AIC puis recharge sa table de taps ; ceci est distinct du RESET de la puce seule, qui conserve ses registres de polynôme. Les impulsions logicielles sont traitées par l'horloge fournie à la cible ; leur placement fin sur l'horloge RD/REF reste à intégrer avec les autres registres du séquenceur.

Le premier passage groupé donne **637/638 cas Debug (25 s)**. L'unique attente obsolète imposait un refus d'un PATCH sans donnée à écrire : les quatre déclinaisons de ce cas passent après correction ciblée (**4/4, 36 ms**). Le passage complet suivant réussit en **Release : 638/638 (3 s)** ; le relais/configuration Acorn réussit **58/58 (3 s)** après recompilation. Les nouveaux cas vérifient notamment le chargement des taps par PATCH, une lecture physique utilisant un autre polynôme et le retour à la configuration firmware après RESET. Ces résultats n'établissent pas encore le fonctionnement de tous les registres AIC ni du dump matériel complet 61h.


### Preuve BBC après raccordement de l'ECC

Les scénarios invités DAT/DSC et EMU passent en Release (**2/2, 21 s**), avec sauvegarde ADFS de CHIPTEST, relecture, destruction de la première session, réouverture dans une nouvelle machine en lecture seule et comparaison des 256 octets. Les deux catalogues réouverts ont été inspectés dans `artifacts/TestsChips/Acb4000/BbcMicroModelB/Release/adfs-ecc-registers-{dat|emu}-reopened.png` ; ils affichent le fichier chargé. Le détail du banc et ses limites figure dans `docs/machines/AcornWinchester.md`. Il s'agit d'une preuve du parcours nominal natif utilisant désormais les registres ECC partagés, pas de la clôture des registres matériels encore absents.


Le diagnostic temporaire `AcornEmuWinchesterNativeDiagnostic.cs` est supprimé après les deux réussites. Ses médias `ecc-register-native.dat`, `.dsc` et `.emu` ne subsistent pas ; la reconstruction Release de l'assembly de tests sans ce fichier réussit (0 erreur, trois avertissements préexistants hors de ce bloc). Les quatre PNG de preuve sont conservés uniquement dans artifacts, hors Git.

### Diagnostics conservés dans les contextes RAM et compteur réellement modifiable

Le décodage de 0719h–0720h montre que le firmware prélève le drapeau de préparation du contexte courant avant de l'effacer. Les chemins 073Bh–074Ch sauvegardent les indicateurs de ce contexte ; le changement d'unité échange notamment 8098h–809Ch. Un appel sur une autre unité ne consomme donc pas la préparation de la première. L'entrée RESET 0024h–0049h ne l'efface pas non plus. L'ancienne invalidation globale au RESET et au changement d'unité était incorrecte pour le binaire 400108-00 et a été retirée.

Le pointeur de dump et son drapeau sont maintenant les octets RAM 8098h–8099h/809Ch, sans champs parallèles. La CDB et sa normalisation sont mémorisées dans la zone de travail. Le compteur 8084h–8085h est décrémenté avant chaque demande d'octet, comme 1B76h/1B93h. Un PATCH de ce compteur agit immédiatement, y compris s'il prolonge le transfert au-delà de la longueur initiale. Pour un dump, MOV A,M précède cette décrémentation : la valeur déjà prélevée est conservée pour l'octet envoyé.

Le pointeur et le drapeau peuvent être modifiés par PATCH RAM. Le cas natif de test charge 12345678h en série dans l'ECC, prépare ensuite un dump ciblé de 4072h/4073h et reçoit les valeurs 35h/12h depuis les registres utilisés par le contrôleur. Les lectures hors des espaces natifs raccordés restent refusées avant le transfert, plutôt que de produire un contenu inventé.

**641/641 tests puce/hôte réussissent en Release (3 s)** après ce bloc. Ces preuves ajoutent les contextes indépendants, RESET, CDB/compteur visibles dans le dump, auto-modification du compteur et accès natif au syndrome. Elles ne clôturent pas les autres registres, le programme du séquenceur ni les vérifications finales de la carte.

### Compteur du microprogramme AIC

La page 23 du constructeur distingue les cinq bits bas du compteur et les trois bits hauts. Sans transfert de tampon, 20h sélectionne AM, 40h ECC et 80h active SEB ; ces indicateurs se combinent (A0h pour AM+SEB). Avec transfert, les huit bits sont le compteur. Le programme de carte 1F67h emploie 00h pour chacun des octets d'identité, 43h pour les quatre octets ECC et FFh pour les 256 octets DATA : le nombre d'octets émis est inclusif, valeur chargée plus un. Un modèle interprétant 00h comme 32 ou 256 octets serait incorrect.

Le rendu temporaire de la page a été supprimé après transcription. Le séquenceur programmable et son raccordement restent ouverts.

### Séquenceur programmable : validation autonome

`Acb4000Sequencer` possède les 24 instructions de quatre octets et partage le bloc ECC. Son interface empruntée fournit les cycles de tampon ; ses méthodes exposent les entrées physiques et les horloges de bit. Les compteurs inclusifs, conditions de branchement, portes, comparaison, pile, événements acquittés et RESET sont couverts par 53 cas autonomes, dont les 32 combinaisons de conditions après ECC. Ces 53 cas passent en Debug (67 ms).

Après mise en cache des quatre octets de l'instruction active, le groupe ACB-4000 et hôte SCSI passe en Release : **694 réussites, zéro échec, 3 secondes**. Une écriture de l'instruction active actualise ce cache sans recharger le compteur déjà lancé. Cette modification n'a pas encore fait l'objet d'une mesure de gain de temps ; aucun gain chiffré n'est revendiqué.

Ces tests ne constituent pas le raccordement natif : les canaux existants n'exécutent pas encore ce microprogramme. Le décodage complet des registres, la sélection des horloges, la synchronisation aux marques et les échanges de tampon de la carte doivent encore être raccordés. Le débordement de pile n'est pas établi par une preuve matérielle. Les captures ADFS précédentes ne prouvent donc pas ces fonctions nouvelles ; la carte reste non clôturée.

### Décodage GPIO et horloges AIC-010

Pages 18–20 : 7Dh sélectionne les quatre directions GPIO par ses bits 0–3 ; les bits 4–7 remplacent respectivement ces sorties par les impulsions W6E, R6E, W6F et R6F, uniquement lorsque leur direction est sortie. La lecture 7Eh prélève les niveaux des broches GPIO 0–3, BRANCH en bit 4 et OUTPUT du microprogramme en bit 5. Les bits 6–7 ne sont pas utilisés. Les bascules de sortie GPIO restent distinctes des entrées échantillonnées.

7Fh en écriture porte le début de comparaison NRZ en bits 0–2, la haute impédance des horloges A/B en bit 3, le choix deux/quatre cycles par octet de transfert en bit 4 et le choix d'horloge hors transfert en bits 6–7. Le choix 11b est décrit « messy » par le constructeur : aucune fréquence stable ne peut en être déduite. 7Fh en lecture dépile la pile de huit octets ; ce n'est pas une lecture du contrôle d'horloge. 7Bh et 7Ch sont les contrôles WAM/AMD en écriture seule. Les ports 50h/51h/70h sont externes à l'AIC et ne doivent pas être inventés dans sa RAM. Le rendu de travail utilisé pour lire ces diagrammes a été supprimé.

### Horloge de lecture et syndrome

Le canal MFM ne reporte plus l'évolution du syndrome à la fin de l'octet décodé. Après la synchronisation A1, chaque cellule DATA alimente directement l'ECC partagé ; une cellule CLOCK ne le fait pas avancer. La détection des marques, l'assemblage des octets et la vérification des horloges restent dans le canal. Le lecteur de paramètres réutilise ce même chemin.

Deux cas supplémentaires observent les registres après chacune des seize cellules d'une marque FE ou F8, avec un état attendu mis à jour uniquement aux cellules DATA. Le groupe vérifie aussi les champs complets et leurs ECC. Résultats : **696/696 en Debug (23 s), 696/696 en Release (3 s)**. Ce sont des durées de validation, pas des mesures de débit de la carte. Cette correction ne clôture pas le séquenceur natif ni ses autres registres.

### Bloc de registres appartenant à la carte

La carte possède maintenant `Acb4000AicRegisters`, qui regroupe son ECC et son séquenceur. Les diagnostics natifs décodent ses registres et sa micro-RAM, avec validation sans lecture anticipée pour préserver les acquittements. Les adresses non décodées restent refusées. Une pile vide ne produit pas artificiellement la valeur du contrôle d'horloge. Le bus externe optionnel conserve la responsabilité de ses propres registres ; il n'est pas nécessaire pour accéder aux registres AIC réalisés.

L'initialisation charge les quatre colonnes de 24 octets de la table 1F67h du firmware 400108-00. Elle programme WAM=10h, AMD=A1h, direction GPIO=0Eh et contrôle d'horloge=07h (chemin sans cavalier d'horloge rapide de 1DD7h–1DE2h). Le séquenceur emprunte le tampon dual-port de 1 Kio déjà utilisé par les commandes SCSI ; il ne possède pas une copie de ce tampon. INDEX est transmis à son entrée physique. RESET réinitialise le moteur, recharge les taps et le microprogramme comme la routine de carte ; l'écriture du bit RESET ECC conserve la longueur réellement écrite au contrôle 71h.

**709/709 tests ACB-4000/hôte et 58/58 tests du relais Acorn passent en Release (3 s par groupe)**. Les ajouts vérifient le sens des registres, les trous de décodage, GPIO et impulsions externes, les sélections d'horloge, RESET, le patch SCSI de la micro-RAM et sa restauration, l'acquittement du branchement INDEX par la lecture du statut et l'identité du tampon écrit par le séquenceur puis lu par SCSI. Le test de tampon fournit directement les horloges NRZ au séquenceur : il ne prouve pas leur génération par le lecteur natif.

Le remplacement des canaux de commande existants par l'exécution du microprogramme, le raccordement des compteurs externes du tampon et des signaux WAM/AMD/horloges restent ouverts. Ces contrôles sont mémorisés et décodés, mais leur seule présence ne constitue pas une réalisation de leurs formes d'onde. Aucun résultat de ce bloc n'est présenté comme une validation complète de la carte.

### Pointeurs du tampon : fiche constructeur retrouvée

La fiche [Adaptec AIC-300 Dual Port Buffer Controller](https://ftpmirror.your.org/pub/misc/bitsavers/pdf/adaptec/asic/AIC-300_Dual_Port_Buffer_Controller.pdf), pages 9–12 et 16, confirme RAP=5Ah/5Bh, WAP=5Ch/5Dh et STOP=5Eh/5Fh. Ce ne sont pas un pointeur CPU et un pointeur disque : ce sont les pointeurs de lecture et d'écriture du tampon, utilisés par les deux ports. Le bit ROP/WOP de 53h sélectionne le sens des transferts. Le port périphérique écrit avec WAP en ROP et lit avec RAP en WOP ; le port hôte fait l'inverse. Les cycles de transfert incrémentent le pointeur concerné. STOP borne les transferts hôte, pas le port périphérique.

L'accès CPU par 70h n'incrémente aucun pointeur. Il utilise RAP en WOP et WAP en ROP, indépendamment du sens de l'accès CPU. Cela concorde avec le diagnostic 1C4Dh qui charge explicitement 405Ch/405Dh avant chaque accès 4070h. Toute écriture à 59h réinitialise RAP, WAP et STOP ; son bit 0 maintient en outre les registres en RESET. 54h programme la limite du tampon (03h pour 1 Kio). Les bits DMA de 53h sont WRITE LATCH=04h, READ LATCH=08h, ROP=10h, DONE=20h en lecture seule, INITIATOR=40h et TARGET=80h. Le rechargement de STOP efface DONE et permet de poursuivre les transferts. Le rendu de travail a été supprimé après transcription.

`Acb4000BufferController` remplace le curseur unique du port séquenceur. Il emprunte la même RAM, possède les trois pointeurs 16 bits, applique la limite programmée aux incréments et le câblage physique de la RAM aux accès. Les ports natifs 53h/54h/59h/5Ah–5Fh/70h sont décodés. Les accès CPU ne modifient pas le pointeur ; les cycles périphériques ignorent STOP. Les cycles hôte achevés sont bornés par STOP inclusif et DONE ne peut pas être écrit directement. RESET préserve les données RAM. L'état Idle ne doit pas suspendre un séquenceur ou un transfert hôte encore actif.

Résultat du bloc : **723/723 tests ACB-4000/hôte en Release, 4 s**. Les 14 cas ajoutés couvrent pointeurs, modes, tailles, miroir physique, STOP/DONE, RESET, absence d'allocation et accès au tampon par les diagnostics SCSI natifs. Les méthodes de cycles hôte sont à ce stade vérifiées isolément ; leur appel lors des phases REQ/ACK de la carte et l'arbitrage des deux ports ne sont pas encore démontrés par ces cas.

### DMA natif des commandes de tampon

READ BUFFER et WRITE BUFFER exécutent maintenant leurs cycles dans ce contrôleur de tampon, conformément aux routines 1083h–10B5h. Le cycle RAM avance le pointeur ; le dernier cycle reste en attente tant que l'acquittement SCSI n'est pas terminé. DONE est publié après le relâchement d'ACK, puis le firmware de carte désarme la commande DMA. Les commandes de diagnostic effectuées manuellement par le processeur de carte ne sont pas transformées en DMA.

**729/729 cas du groupe passent en Release (4 s) et Debug (36 s), et 58/58 cas du relais Acorn passent en Release (3 s)**. Les six ajouts vérifient le dernier ACK maintenu, l'absence de DONE anticipé, l'avancement des pointeurs et RESET pendant un transfert. Les durées Debug ont été obtenues pendant la construction parallèle du relais et ne servent pas de mesure de débit. Le transfert SCSI complet de 1 Kio est aussi conservé. Cela ne prouve pas encore le remplacement des canaux disque par le microprogramme ni l'arbitrage à chaque horloge des ports.

Le chemin de libération du bus après expiration de REQ annule maintenant le cycle DMA en attente et désarme les latches. Il préserve les pointeurs et la RAM déjà écrite, sans produire DONE. Le contrôle étendu a donné 730 réussites sur 732 cas en Release ; les deux échecs étaient dans les nouveaux scénarios, qui construisaient la carte avec une horloge de 1 kHz refusée par son positionneur. Le banc a été corrigé à 2 MHz, sans modification de cette contrainte matérielle : les deux scénarios passent ensuite en Release (62 ms et 39 ms). Les trois nouveaux cas d'annulation passent aussi en Debug (131 ms). Les scénarios vérifient la nouvelle sélection et le transfert de 1 Kio après l'expiration, dans les deux sens. Aucun passage unique de 732 cas tous verts n'est revendiqué ici.

### Informations firmware conservées pour le raccordement des canaux

La routine 1541h choisit l'état de démarrage 05h ou 15h suivant le bit 0 de 809Ch ; le second chemin programme également le branchement SECTOR au contrôle ECC D0h. Le chemin d'acquisition d'ID 1596h–15BCh remplace NEXT[08h] par 1Fh, NEXT[0Ch] par 68h, place 05h dans le registre de branchement et démarre en 15h pour attendre INDEX. Après un ID, le firmware lit le statut puis dépile le drapeau et l'adresse aux accès 407Fh de 167Dh–168Eh. Pour les blocs supérieurs à 256 octets, 1612h–1627h arme et attend l'effacement d'INHIBIT CARRY à chaque prolongation. Le microprogramme doit recevoir ces écritures et ces acquittements ; il ne suffit pas de lancer sa table initiale inchangée pour remplacer le lecteur de champs.

RESET est maintenant appliqué au groupe de registres AIC concerné, pas seulement au moteur du séquenceur. Les bascules de sortie GPIO et les contrôles WAM/AMD sont effacés ; les écritures de contrôle/sortie ne survivent pas au maintien de RESET. Les niveaux externes empruntés restent présents. La micro-RAM, les taps et le contrôle 7Fh sont conservés, conformément aux groupes de registres décrits pages 4 et 13. Le firmware de carte recharge ensuite explicitement ses contrôles. Les trois nouveaux cas et le groupe entier passent en Release : **735/735, 4 s**, y compris les scénarios d'expiration corrigés.

### Preuve CPU du tampon SCSI de 1 Kio

Le diagnostic temporaire `AcornEmuWinchesterBufferDiagnostic` a démarré un BBC Model B avec MOS 1.20, BASIC II et le générateur de caractères existants. Un programme 6502 chargé en RAM a été lancé par la commande BASIC `CALL &2000`. Il sélectionne la cible via FC40h–FC43h, envoie WRITE BUFFER 13h, écrit quatre pages de 256 octets, lit statut/message, puis envoie READ BUFFER 14h et compare les 1024 octets dans le 6502 lui-même. Le motif est l'octet d'index XOR 5Ah, répété sur les quatre pages. Le programme affiche son propre résultat par OSWRCH ; le banc vérifie aussi les deux statuts SCSI nuls, l'absence de divergence et la fin effective du programme. Aucun appel MOS n'est intercepté et aucune sortie audio Windows n'est ouverte.

**1/1 diagnostic CPU réussi en Release, 2 s.** La capture effectivement examinée montre « ECRITURE : 1024 OCTETS », « LECTURE : 1024 OCTETS » et « COMPARAISON : OK » : `artifacts/TestsChips/Acb4000/BbcMicroModelB/Release/dma-buffer-1024-cpu.png`. Elle prouve les commandes du tampon, le chemin CPU/carte hôte/SCSI/DMA et le retour au BASIC. Aucune image disque n'est nécessaire pour ces deux commandes indépendantes des lecteurs. Elle ne prouve ni le séquenceur de lecture/écriture de piste encore à raccorder, ni les formes d'onde de son décodeur.

Le source temporaire a été supprimé et l'assembly de tests Release reconstruite avec succès (zéro erreur, trois avertissements préexistants hors de ce diagnostic). Aucun programme binaire ni média de travail n'a été créé. Seule la capture est conservée hors Git ; les ROM et la configuration utilisateur n'ont pas été modifiées.

### Lecture physique par le séquenceur partagé — contrôle du 5 novembre 2026

Les canaux de lecture et d'acquisition des paramètres utilisent maintenant le bloc AIC de la carte. La synchronisation attend AMD et applique les bits de comparaison programmés ; les ID sont dépilés par le chemin matériel. Les octets DATA entrent dans la RAM par les cycles du séquenceur, puis sortent par le DMA hôte. La copie directe d'un secteur décodé n'est plus le chemin normal de réception. Les prolongations INHIBIT CARRY couvrent les blocs de 512 et 1024 octets. Un microprogramme dont le compteur DATA est raccourci ne peut pas être contourné par la copie du secteur complet. Le décodeur autonome est conservé pour les bancs de signaux.

Le premier passage du bloc a révélé 61 échecs sur 739. Les corrections ont porté sur les préambules et l'avancement d'horloge des sérialiseurs manuels, le réarmement après INDEX/expiration, le rechargement du pointeur d'écriture avant chaque DATA et la distinction entre DATA absent et mauvais contrôle d'ID. Le banc d'échéance conserve une marge de 64 ticks pour recevoir réellement le champ, au lieu de l'injecter instantanément. **Passage groupé final : 739/739 en Release, 3 s.** Les lectures 256/512/1024, les politiques ECC, les reprises, les diagnostics et le tampon SCSI font partie de ce groupe.

Cette validation ne clôture pas l'ACB-4000. Les formes d'onde des horloges/ENDEC, l'arbitrage fin des ports, le raccordement des canaux d'écriture et de formatage au microprogramme et la nouvelle preuve invitée de lecture restent à terminer. Les captures précédentes précèdent ce raccordement et ne sont pas présentées comme sa preuve.

### Sortie série d'écriture : références établies

La fiche constructeur [AIC-250 Encoder/Decoder](https://ftpmirror.your.org/pub/misc/bitsavers/pdf/adaptec/asic/AIC-250_Encoder_Decoder.pdf), page 5, décrit l'entrée NRZ série, l'entrée WAM qui supprime une horloge pour la marque A1 et le retard de trois bits de la sortie MFM numérique. Les sorties avec précompensation utilisent en plus le réseau analogique externe ; un flux de cellules nominal ne démontre pas ces décalages analogiques. La page 18 AIC-010 précise que chaque bit de 7Bh sélectionne le temps de bit où WAM est actif pendant une opération AM. Le firmware initialise 7Bh à 10h.

Les routines de la ROM 400108-00 ne préparent pas un champ DATA entier encodé : 1430h écrit NEXT[0Dh]=14h, CONTROL[04h]=05h, NEXT[0Ch]=69h ; 144Bh écrit la continuation de fin de champ, puis NEXT[08h]=17h et NEXT[14h]=05h (mode sans secteur dur). Le chemin d'écriture choisit ainsi l'état 09h après un ID correct, tandis que la lecture choisit 08h. FORMAT (146Fh) utilise NEXT[0Ch]=11h, NEXT[16h]=56h et NEXT[0Dh]=94h, avec SUPPRESS TRANSFER=20h. Les états et leurs données sont partagés ; l'ancien canal qui préencode ses octets/ECC ne représente pas ces accès programmables.

Le nouvel encodeur nominal conserve trois bits de pipeline, sans allocation pendant ses fronts. WAM supprime l'horloge suivante ; avec 7Bh=10h, la sortie de A1 correspond à 4489h. Les 512 combinaisons octet/bit précédent sont comparées à l'encodage MFM existant. Le premier passage ciblé a donné 72/73 : un indice de position CLOCK du nouveau test était faux ; sa correction donne **73/73 en Release, 67 ms**, incluant les cas séquenceur/registres. Ce résultat valide ce bloc isolé, pas encore son raccordement au lecteur. La page 22 AIC-010 confirme que WG se ferme au bit ring 4 après ECC (NRZ non bloqué) : les trois derniers bits retardés de l'encodeur doivent donc rester dans la fenêtre écrite.

L'écriture DATA de la carte utilise désormais ce chemin : état 09h, marque A1/7Bh, F8h, transfert du tampon dans l'état 04h, ECC série dans 0Dh puis fermeture WG retardée. Le canal propriétaire reçoit chaque cellule ; il ne reçoit plus un tableau DATA/ECC préencodé. Le compteur est prolongé par INHIBIT CARRY pour 512/1024 octets. Pour 1024 octets, le pointeur RAP reboucle au début du dernier octet ; l'extension doit donc se baser sur le nombre de transferts et non prendre ce zéro pour un tampon encore entier à traiter. Ce cas a été corrigé pendant le contrôle.

Les fenêtres WRITE normales comportent neuf octets de préambule et le champ DATA complet, sans les huit octets supplémentaires de l'ancien générateur. Les vérifications comparent les octets réellement reçus et les cellules émises. Le groupe passe **744/744 en Release, 4 s**. Trois nouveaux essais de PATCH passent ensuite en Release : C9h raccourcit le préambule à quatre octets ; 7Bh=00h produit A1 sans suppression d'horloge (44A9h) et aucun secteur n'est reconnu ; C4h=00h limite le transfert à un octet, sans remplacement par une copie complète du secteur. Le statut WRITE seul ne détecte pas ces modifications physiques volontaires. **Passage groupé Debug : 747/747, 28 s.** Ces essais ne constituent pas encore la preuve invitée après ce raccordement. FORMAT utilise encore son ancien générateur et reste ouvert.

Le relais de stockage Acorn a également été reconstruit et contrôlé après ce changement : **54/54 cas `AcornEmuWinchesterStorageTests` en Release, 3 s**. Il ne s'agit pas d'un build Debug All livré à l'utilisateur.

### FORMAT par les mêmes registres et sorties

Le chemin natif FORMAT programme désormais les continuations de 146Fh, attend INDEX dans 15h, place adresse/drapeaux dans DATA[00h–03h], puis laisse le séquenceur générer ID et DATA/ECC. Le remplissage vient de DATA[04h] avec SUPPRESS TRANSFER ; il n'est plus préencodé en tableau. Le compteur de champ permet de prolonger les blocs 512/1024 sans dépendre d'un pointeur de RAM qui ne doit pas avancer en mode supprimé. Après le dernier slot, le branchement sélectionne 13h/16h et termine sur INDEX.

Le premier contrôle a révélé l'absence de WAM sur les ID : SEB seul (80h, état 06h) doit également ouvrir la fenêtre de marque, comme AM/SEB A0h du DATA. Le défaut a été corrigé et les trois types de compte AM/SEB sont contrôlés. Les six combinaisons taille/entrelacement de FORMAT passent, puis **750/750 cas ACB-4000/hôte en Release, 4 s**. Les champs reçus doivent aussi avoir des horloges MFM valides. Le banc conserve l'ID entre les fenêtres WG d'une même piste, regroupe ses observations par piste et compte les fenêtres distinctes. Le PATCH CEh=87h réduit réellement le préambule d'ID à huit octets ; le contenu E5h est reçu dans les 33 secteurs et RAP reste inchangé. Cette validation de banc ne remplace pas la preuve CPU avec un conteneur réel.

### Preuve CPU sur conteneurs : un cas réussi, un écart encore ouvert

Le diagnostic temporaire `AcornEmuWinchesterSequencerDiagnostic` charge un programme 6502 en RAM du BBC B 32 Kio (MOS 1.20/BASIC II), puis le lance par `CALL &2000`. Le programme utilise FC40h–FC43h : MODE SELECT pour 1 cylindre/1 tête, FORMAT de 33 secteurs remplis d'E5h, READ du secteur 0 et comparaison de 256 octets, WRITE du secteur 1 avec motif index XOR 5Ah, READ et comparaison des 256 octets. Les statuts et messages SCSI sont incorporés au résultat. Le CPU imprime lui-même OK ou ! selon ses comparaisons. Aucun appel MOS n'est intercepté, aucune sortie audio Windows n'est ouverte et aucun média/configuration utilisateur n'est modifié.

Le premier banc oubliait le bit de sélection de la cible dans le latch de données : l'attente sans bus actif était un défaut du diagnostic, corrigé dans son programme 6502. L'essai réel a ensuite révélé deux manques : FORMAT tentait de positionner une image EMU vierge depuis une taille sectorielle encore inconnue, et son runtime média refusait la préparation du formatage. FORMAT est maintenant positionné par le cylindre/tête de sa commande ; un déplacement déjà effectué par STEP n'effectue plus de division sectorielle dans le relais. Le runtime EMU valide la géométrie physique sans effacer de piste lors de la préparation. Les paramètres transmis en fin de FORMAT sont confrontés aux ID effectivement enregistrés, sans inventer de fichier DSC pour EMU.

**Dernier passage : EMU réussi, 2 s ; DAT échoué à la publication. Série totale : 1/2, 6,7 s.** La capture EMU inspectée, `artifacts/TestsChips/Acb4000/BbcMicroModelB/Release/sequencer-format-write-read-emu.png`, montre les opérations et « COMPARAISON : OK ». Elle prouve ce scénario CPU et ce support, pas tous les secteurs relus, la réouverture ou tous les modèles/ports.

Sur DAT, le CPU termine aussi ses comparaisons des secteurs 0/1 et sa capture `sequencer-format-write-read-dat.png` affiche OK, mais Flush/Dispose refuse la projection physique en secteurs. Cette capture ne prouve donc pas une sauvegarde réussie. Le premier décodage donnait 35 ID dont deux invalides. La routine 14B7h reprogramme le branchement à 12h après le premier INDEX : les deux octets post-DATA de cet état avaient été omis dans le canal natif. Après correction, 33 ID sont présents et leurs contrôles d'en-tête sont tous valides, mais DATA n'est pas retrouvé pour les secteurs 8, 28, 13, 30, 15 et 32 dans ce scénario. L'origine des marques intermédiaires/gaps et des fenêtres conservées doit encore être établie avant de modifier le décodeur ou la projection. Aucune suppression de champs ni réussite forcée à Flush n'a été ajoutée. Les passages de 750 cas précèdent les derniers changements de positionnement/runtime et ne valident pas ces derniers.

Le diagnostic externe a été supprimé après ces mesures et l'assembly LocalEmulationTests Release reconstruite : zéro erreur, trois avertissements préexistants. Les fichiers `sequencer-native.dat`, `.dsc` et `.emu` ont été retirés par les finally, y compris lors des échecs. Seules les deux captures restent hors Git. Aucun commit ni build Debug All de livraison n'a été effectué sur cet état incomplet.

### Qualification de la synchronisation de lecture

Les deux contre-épreuves isolées passent avant modification : 1 024 combinaisons adresse/drapeaux conservent leur ID dans le canal brut (17 ms), et la piste synthétique de 33 secteurs E5h conserve ID/DATA dans le codec média (134 ms). Une troisième épreuve reproduit l'absence de DATA sans conteneur ni machine : deux mots 4489h/2AAAh injectés immédiatement après l'ID, sans préambule, bloquent le DATA pourtant valide qui suit.

La fiche AIC-250, pages 6–7 effectivement examinées, décrit la qualification absente de ce chemin : après RG, 16 zéros consécutifs transfèrent la source VFO vers READ DATA ; 48 temps supplémentaires sans ONE'S RESET atteignent 64 et activent la recherche de marque. Ensuite les uns n'annulent plus ce latch. Sans marque au compte 128, le compteur et les latches reviennent à l'oscillateur pendant quatre octets (32 bits), puis recommencent. Une marque trouvée verrouille AMD et arrête les contributions du compteur jusqu'à la fin de RG. Ces conditions distinguent une signature fortuite dans un gap de la synchronisation après le préambule de phase. Les valeurs analogiques de CLAMP et de VFO ne sont pas déduites de ce modèle numérique.

Le canal natif applique maintenant cette qualification dans `Acb4000ReadSynchronizer` : acquisition après 16 zéros, ouverture de recherche à 64, expiration à 128, retour oscillateur de 32 bits, latch AMD jusqu'à RG bas ou réarmement. Les bancs natifs fournissent des préambules physiques au lieu de commencer après deux octets. Le décodeur autonome brut conserve son usage de banc. Le codec média exige le préambule zéro avant de retenir une signature A1 ; il ne supprime ni les ID qualifiés dont le contrôle échoue ni les mauvais ECC des données. Sa reconnaissance hors ligne n'est pas une simulation du VFO analogique ni de toutes les positions possibles de RG.

**Validation regroupée : 761/761 cas ACB-4000/hôte Release, 4 s ; 124/124 cas médias Adaptec/Winchester/EMU, 683 ms ; 54/54 cas du relais Acorn dans le premier passage CPU, 7 s avec les deux preuves.** Un banc RESET supposait WG encore actif exactement au septième secteur terminé ; il attend désormais l'ouverture de la fenêtre suivante avant de déclencher RESET et contrôle toujours l'arrêt sans écriture supplémentaire.

**Nouvelle preuve CPU : DAT et EMU réussis, 2/2, 4 s.** Le 6502 effectue MODE SELECT, FORMAT, READ du remplissage E5h, WRITE/READ du motif index XOR 5Ah. Les deux captures `sequencer-format-write-read-dat.png` et `sequencer-format-write-read-emu.png` ont été inspectées et montrent COMPARAISON : OK. Après détachement, Flush, Dispose et réouverture, les 33 secteurs contiennent leurs 256 octets attendus : motif au secteur 1, E5h aux 32 autres. EMU conserve des contrôles ID/DATA valides pour tous les secteurs. DAT conserve les octets logiques ; ses contrôles physiques sont absents, ce que le diagnostic doit respecter. Le premier passage de réouverture exigeait à tort ces contrôles sur DAT, puis cette assertion de banc a été corrigée. L'ancien échec de publication DAT est résolu sans assouplir sa projection ni ignorer de secteur. Ce résultat ne clôture pas les autres écarts matériels encore décrits dans la fiche.

Le diagnostic externe de ce nouveau passage a été supprimé, son assembly Release reconstruite (zéro erreur, trois avertissements préexistants) et ses DAT/DSC/EMU supprimés dans les finally. Les captures restent hors Git.

### Réception WRITE par le tampon natif

Les commandes WRITE ordinaires utilisent désormais les cycles DMA hôte de l'AIC-300, comme les commandes de tampon : WAP avance au prélèvement ACK, STOP borne le bloc, DONE attend ACK bas et le DMA se termine avant le positionnement. Il n'y a plus de copie d'entrée parallèle dans le gestionnaire SCSI pour ces commandes. Les quatre nouveaux cas vérifient les blocs 256/512/1024, le wrap de WAP à 1 Kio, STOP/DONE, l'écriture physique obtenue et RESET après ACK sans écriture du disque ; les octets déjà acceptés restent dans la RAM. **Passages groupés : Release 765/765, 5 s ; Debug 765/765, 37 s.** La preuve CPU 2/2 ci-dessus précède ce dernier raccordement DMA ; elle n'est pas présentée comme une nouvelle exécution après celui-ci. Aucun build Debug All de livraison ni commit du composant incomplet n'est annoncé.

### Latence NRZ et détection AMD

La figure ADDRESS MARK DETECT TIMING de l'AIC-250, page 5, distingue AMD et NRZ OUT (4-BIT DELAY). L'AIC-010, page 18, compare son registre AMD 7Ch aux bits du registre de décalage NRZ autorisés par 7Fh, en présence d'AMD. Le canal natif conserve maintenant huit cellules en pipeline : AMD est acquis depuis les transitions physiques qualifiées, puis le registre NRZ retardé est comparé à chaque temps DATA tant que le séquenceur attend SYNC. La valeur reçue est transmise à l'ECC et au tampon ; A1 n'est plus injecté comme valeur supposée dans ce chemin. Avec le réglage firmware 7Fh=07h, la synchronisation se produit lorsque A1 est effectivement disponible après les quatre bits de retard.

Les nouveaux essais contrôlent la fin ID/DATA exactement après huit cellules supplémentaires, les octets reçus et leur ECC, ainsi que l'abandon du pipeline à StopRead/RESET et l'obligation d'un nouveau préambule. Le premier passage groupé révèle 59 échecs sur 768 : le sérialiseur manuel s'arrêtait immédiatement au dernier bit du champ, sans fournir les horloges du retard de sortie. Il fournit maintenant un octet post-champ ; les tests contrôlent toujours les champs et erreurs d'origine. **Passage regroupé Release : 768/768, 4 s.** Les trois nouveaux cas de latence passaient déjà avant cette correction de banc. Les captures CPU antérieures ne valident pas encore ce dernier changement de chronologie.

Après raccordement du retard et du DMA, la preuve 6502 a été réexécutée : **DAT et EMU 2/2, 5 s**, sauvegarde puis réouverture des 33 secteurs comprises. Les captures actualisées portent le texte invité « DMA / NRZ +4 BITS » et doivent être distinguées des captures précédentes.

### Mesure ciblée du codage MFM

Avant remplacement des boucles par des opérations groupées sur les bits : Release .NET 10.0.14, 100 000 appels de chauffe, puis médiane de trois séries de dix millions, **Encode 6,746 ns/appel ; Decode 10,518 ns/appel**. Sommes observées `00000032DCA6F880` et `000000004BFED4C0`, zéro horloge déclarée erronée sur les mots valides. Le banc varie les 256 octets et les deux valeurs du bit précédent au décodage. Ces coûts sont ceux des fonctions de codage, pas un débit de carte complète ni une vitesse du BBC.

Le regroupement des cellules de données et le calcul parallèle des cellules d'horloge remplacent les boucles de huit itérations, sans table allouée ni état partagé. Avant modification, Debug donnait **69,818 ns Encode / 72,157 ns Decode**. Après modification, mêmes conditions et mêmes sommes : **Release 1,659 ns / 2,213 ns ; Debug 14,246 ns / 14,961 ns**, soit environ 4 à 5 fois moins de coût pour ces fonctions. Les 65 536 mots physiques sont confrontés à un calcul sériel indépendant pour les deux valeurs du bit précédent : octet, état de sortie et validité de toutes les horloges identiques, y compris les combinaisons de défauts. Les autres cas du canal contrôlent les marques à horloge manquante, les ECC et les limites de champ. Le premier filtre regroupant le canal et la mesure passe 21/21 en Release. Aucune fréquence émulée, temporisation mécanique ou durée de bit n'a été accélérée.

Après retrait des diagnostics temporaires, **770/770 cas ACB-4000/hôte réussissent en Release (4 s) et Debug (31 s)**. Les assemblies Chips.Tests ont été reconstruites sans le diagnostic de performances ; LocalEmulationTests Release est également reconstruite sans le programme CPU externe (zéro erreur, trois avertissements préexistants). Les deux captures « DMA / NRZ +4 BITS » ont été inspectées. Aucun média `sequencer-native` ne reste dans leur dossier et les captures ne sont pas suivies par Git. La carte entière reste ouverte pour la clôture des horloges/échanges matériels internes, les mesures du montage complet et la validation finale de livraison ; ce passage ne déclare aucune autre puce ni machine terminée.

### Impulsions des ports externes sur GPIO

Les pages 3, 18–19 et 25 du [manuel AIC-010](https://manuals.plus/m/dca1bda0678258f6603a401b02e522094a72c157e96644465a3cadff810c070b.pdf) distinguent les latches GPIO et les sorties de décodage actives à zéro : W6E, R6E, W6F et R6F. Le calcul du décodage existait mais ne rendait pas ces niveaux observables pendant l'appel au bus externe. `PinOutputEnable` et `PinOutputLevels` décrivent maintenant les quatre broches, y compris le repos haut des sorties de décodage ; lire 7Eh échantillonne ces niveaux. Les propriétés `GpioOutputEnable/Levels` restent propres aux latches GPIO ordinaires.

Les lectures et écritures de diagnostic sur le bus emprunté commencent l'impulsion avant l'appel au périphérique et la terminent dans finally. Les directions et bits de décodage restent nécessaires. Le bus n'est ni réinitialisé ni libéré par la carte. Le premier passage regroupé donne **775/775 cas Release, 4 s**, couvrant notamment les quatre couples adresse/sens, leur observation depuis le périphérique, le retour au repos, les lectures GPIO et RESET ; le cas d'erreur de lecture contrôle aussi le finally. Le contrôle symétrique d'erreur d'écriture est ajouté pour le passage suivant. Un cycle d'API représente ici l'accès externe complet ; aucune largeur électrique arbitraire d'impulsion n'est inventée et ce passage ne valide pas encore les formes d'onde CLKA/CLKB.

Le filtre ciblé AIC/GPIO et accès externes passe ensuite **20/20 en Debug, 53 ms**, avec les deux variantes d'exception lecture/écriture. Ce dernier passage ajoute un cas au jeu de tests ; le résultat Release de 775 cas précède ce cas supplémentaire et n'est pas renommé en 776 sans exécution.

### Sources CLKA/CLKB et tampon : faits établis et générateur numérique

Les pages PDF 20, 47 et 49 du manuel AIC-010 ont été examinées en image. 7Fh programme deux ou quatre cycles CLKA par octet en transfert sériel ; son bit 3 rend CLKA et CLKB haute impédance. Les bits 6–7 sélectionnent les trois rapports système, puis une combinaison décrite « messy », sans forme d'onde garantie. Le tableau écrit « 1/4 ... 1/2 ... system clock period » : ce libellé ne suffit pas à justifier une multiplication de fréquence. Le modèle de diviseur numérique retient SYSCLK/4, /2 et direct ; la correspondance exacte du libellé au câblage doit encore être confrontée au raccordement de carte, sans présenter cette déduction comme une mesure matérielle.

Le chronogramme de données impose CLKA période minimale 200 ns et états haut/bas minimaux 95 ns, CLKB descendant au moins 90 ns avant CLKA descendant, puis CLKB montant au moins 100 ns après ce front. Ce sont des minima : le 100 ns n'est pas un maximum. La fiche AIC-300 page 4 donne la priorité au port A et limite ses requêtes à une tous les deux cycles CLK pour permettre l'entrelacement du port B.

La [note d'application constructeur d'août 1984](https://ftpmirror.your.org/pub/misc/bitsavers/pdf/adaptec/asic/Adaptec_Chipset_App_Note_Aug84.pdf), pages imprimées 5 et 8, confirme CLKA vers CLK AIC-300 et CLKB vers la requête A, transferts A synchrones et B par REQ/ACK asynchrones. Son exemple AIC-100 utilise CLK 400 ns et un octet A tous les 1,6 µs en mode 5 MHz, ou 800 ns en mode 10 MHz ; il ne faut pas substituer automatiquement ces réglages d'exemple à ceux du firmware ACB-4000.

`Acb4000Clocks` représente les deux sources en demi-cycles fournis par le propriétaire, les divisions et une requête CLKB chevauchant le front descendant CLKA. Aucun sommeil ni temps hôte n'est employé. Les longues plages sans requête avancent arithmétiquement ; une requête ne demande d'examiner que son début et sa fin. Les sources inactives conservent leur propre phase. Haute impédance et sélection non définie n'inventent pas de niveau logique stable. 7Fh et le RESET du bloc AIC utilisent ce même générateur ; le contrôle 7Fh reste conservé au RESET. **Premier filtre AIC/horloges : 28/28 Release, 64 ms.** Les essais confrontent aussi progression groupée et fronts successifs pour 256 durées. Le RESET de source est précisé ensuite pour revenir à SYSCLK quand le séquenceur s'arrête.

Ce générateur est une étape du raccordement : il n'horloge pas encore les accès RAM natifs ni les ACK hôte, et sa requête abstraite n'est pas présentée comme l'arbitrage AIC-300 achevé. Il reste à raccorder son avancement aux sources de la carte, les requêtes du séquenceur et le service prioritaire A/B. Les essais CPU et mesures MFM antérieurs ne prouvent pas ce raccordement. Les rendus temporaires de consultation sont supprimés après transcription ; la note PDF demeure dans les références de travail hors Git jusqu'au nettoyage final du composant.

Après retour explicite à la source système au RESET du bloc AIC, **790/790 cas ACB-4000/hôte passent en Release, 6 s**. Ce passage englobe le nouveau générateur, les registres et les cycles GPIO précédents. Aucun passage Debug du générateur ni nouvelle capture CPU n'est annoncé ici ; le montage RAM horlogé décrit ci-dessus reste ouvert.

### Demandes périphériques distinctes des cycles RAM achevés

Le contrat `IAcb4000SequencerBus` permet maintenant demande de lecture/écriture, réception de leur complétion et annulation. Ses implémentations de cycle complet existantes conservent leurs méthodes et leurs effets ; l'activation horlogée est explicite. `Acb4000BufferController` conserve la même RAM empruntée : demander un accès ne modifie ni octet ni pointeur. Le propriétaire accorde ensuite le cycle périphérique, qui échantillonne le latch, accède à la RAM et avance le pointeur. Une arrivée sans horloge ne crée pas une file de secteurs cachée : le latch d'entrée reçoit la dernière valeur fournie. RESET et annulation suppriment les demandes non servies sans effacer la RAM.

En mode horlogé, le séquenceur prépare la lecture du prochain octet avant son premier bit WRITE, dans le dernier octet d'overhead et pendant l'émission du précédent octet DATA. Il consomme le latch reçu, sans arrêter le flux sériel pour attendre la RAM ni remplacer le contenu par une copie du bloc demandé. Les octets READ sont déposés seulement au cycle RAM accordé et le compteur de transferts suit les complétions. Le dernier accès déjà demandé survit à l'arrêt normal du moteur ; RESET l'annule. Le mode de cycles complets reste celui du montage natif tant que les sources et l'arbitre ne sont pas raccordés : ces essais ne prétendent pas encore remplacer le câblage natif.

Le premier passage regroupe **797 réussites et un échec sur 798**, au contrôle d'absence d'allocation du séquenceur ; ce contrôle passe ensuite isolément. Cette observation ne suffit pas à attribuer une cause à l'échec. Sa chauffe initiale de 1 000 bits ne parcourait pas un rebouclage complet du compteur de 256 octets ; elle couvre maintenant deux boucles complètes avant de conserver l'exigence stricte zéro octet alloué. Après ajout du contrôle du dernier cycle à l'arrêt, **799/799 cas passent en Release, 6,96 s au total**. Neuf nouveaux cas contrôlent les demandes différées, les pointeurs/RAM avant et après accord, le latch sans horloge, la préparation des octets WRITE, une réponse tardive, SUPPRESS TRANSFER, la fin du dernier octet, RESET et annulation. Aucun fichier externe ni média n'est utilisé par ces essais autonomes.

### Échantillonnage du port A par CLKA/CLKB

L'activation explicite du tampon horlogé dans `Acb4000AicRegisters` raccorde maintenant les deux entrées d'horloge au tampon emprunté. La requête CLKB est acceptée sur un front descendant CLKA ; le cycle RAM se termine au front descendant suivant. Entre ces fronts, ni le pointeur ni la RAM ne sont modifiés. Les entrées de source restent fournies par le propriétaire : aucune fréquence de carte non établie n'est déduite de la fréquence CPU. Une source non sélectionnée avance sa phase sans servir la RAM. Les sorties haute impédance ne provoquent pas d'accès ; RESET abandonne le cycle échantillonné. Les plages sans demande avancent arithmétiquement, sans boucle par demi-cycle.

**Validation ciblée Release : 17/17, 68 ms**, dont huit nouveaux cas : trois divisions système, deux divisions référence, équivalence des avancements groupés/unitaires pour 128 durées sur les deux sources, haute impédance et RESET entre les fronts. Les autres cas conservent les contrôles de latch, préchargement et dernier octet. Le mode de cycle complet reste inchangé tant que le montage natif n'active pas ce raccordement ; ce résultat ne constitue pas une nouvelle preuve CPU ni la clôture de l'arbitrage A/B.

### Activation dans les canaux natifs et entrelacement A/B

Le montage natif active désormais le tampon horlogé. Chaque cellule physique avance un demi-cycle RD/REF dans les canaux READ, WRITE et FORMAT. La sélection dépend de RG/WG, conformément à la description de CLKA page 3 du manuel AIC-010, et non du seul bit TRANSFER : les préambules et les lectures anticipées WRITE ont aussi leur horloge. SYSCLK avance depuis la cadence fournie au montage, avec fraction entière conservée entre ticks ; la configuration de carte utilise la valeur nominale de 2,5 MHz déjà fournie au séquenceur. La fiche donne une entrée SYSCLK de 1,5 à 3 MHz. Cette configuration numérique n'est pas présentée comme une mesure supplémentaire des pistes de la carte.

La prolongation du compteur 256 octets suit maintenant `FieldDataByteCount`. Le premier passage après activation donnait **793 réussites et 14 échecs sur 807** : suivre le pointeur RAM retardé prolongeait à tort les blocs 512/1024. Après correction du propriétaire du compteur et avancement de SYSCLK après l'arrêt sériel, **807/807 passent en Release, 5 s**. Le dernier accès demandé continue d'interdire l'avancement au repos jusqu'à sa complétion.

Les demandes du port hôte B sont désormais distinctes des accès RAM terminés. Sur un front CLKA, le retour du cycle précédent est achevé puis l'adresse du prochain cycle est choisie : A synchrone prioritaire, sinon B en attente. Les données hôte sont conservées dans un latch ; aucun tampon sectoriel parallèle n'est créé. READ n'affirme REQ qu'après réception de l'octet RAM. WRITE conserve REQ jusqu'à la fin du cycle RAM, puis retire REQ en présence d'ACK ; DONE attend encore ACK bas. RESET avant cette complétion abandonne le latch sans enregistrer l'octet, tandis qu'un octet dont REQ a déjà été retiré reste dans la RAM. Le bus SCSI partagé conserve ses quatre fronts et ses délais ; c'est le propriétaire de la RAM qui fournit maintenant son acquittement.

Le premier raccordement B a révélé une erreur de condition ajoutée dans l'arbitre : la demande CLKB dépendait à tort de B. Le passage donnait **727 réussites et 80 échecs sur 807**. La correction et les quatre contrôles autonomes A/B donnent ensuite **743 réussites et 68 échecs sur 811** : les auxiliaires des bancs imposaient encore le retrait instantané de REQ à l'affirmation d'ACK. L'auxiliaire Transfer maintient maintenant ACK jusqu'à REQ bas. Le passage suivant donne **784 réussites et 27 échecs** ; DrainRead examinait la phase avant la disponibilité de REQ initial. Il attend maintenant REQ avant de décider DATA/STATUS, sans changer les octets attendus ni les assertions de statut. **Le passage suivant réussit 811/811 en Release, 5 s.**

Deux contrôles natifs supplémentaires vérifient la haute impédance pendant une vraie réception de champ, puis la reprise sans remplacement du contenu, ainsi que RESET entre ACK hôte et fin RAM. **813/813 passent en Debug, 40 s.** Ces essais couvrent la chaîne cible ; ils ne sont pas des captures d'exécution par le CPU BBC.

L'avancement cumulé déjà utilisé au repos conserve maintenant aussi SYSCLK et sa fraction dans l'ACB-4000, par `IAcb4000.AdvanceIdleTicks`. Le relais Acorn transmet ses ticks cumulés au même contrôleur avant toute nouvelle activité. La méthode refuse un contrôleur non inactif, notamment une demande RAM en attente ; elle avance les longues plages par arithmétique entière, sans boucle par tick. Trois essais comparent les phases, les pointeurs et la chronologie du premier accès après 1, 23 333 et 100 001 ticks, avec une horloge propriétaire de 2 000 003 Hz. **816/816 passent en Release, 6 s ; 58/58 tests du relais Acorn passent en Release, 3 s.** Le passage Debug de 813 cas précède ces trois derniers essais. Ces tests d'intégration ne constituent pas une nouvelle preuve CPU : les captures précédentes restent antérieures au raccordement horlogé.

### Preuve 6502 après activation des horloges et de l'arbitrage

Le diagnostic temporaire a exécuté MODE SELECT (1 cylindre, 1 tête, 33 secteurs de 256 octets), FORMAT avec remplissage E5h, READ/contrôle de ce remplissage, WRITE d'un motif index XOR 5Ah au secteur 1, puis READ/comparaison des 256 octets. Le programme est placé en RAM à 2000h et lancé en tapant `CALL &2000` sur le clavier émulé ; aucune interception de ROM ni modification du PC du CPU n'est utilisée. Les commandes passent par les registres FC40h–FC43h et les nouveaux cycles RAM A/B. Les messages sont écrits par le 6502 via OSWRCH.

**DAT/DSC et EMU : 2/2 réussis, 4 s.** Après détachement, sauvegarde, fermeture et réouverture par le service média existant, les 33 secteurs sont comparés octet par octet : motif au secteur 1, E5h dans les 32 autres. Le premier rendu superposait les lignes, car le diagnostic utilisait CR sans LF ; les seuls retours de son affichage sont corrigés et deux frames sont laissées au balayage vidéo. Le passage reproduit alors **2/2, 4 s**, sans modification du contrôleur.

Les captures `artifacts/TestsChips/Acb4000/BbcMicroModelB/Release/clocked-arbitration-dat.png` et `clocked-arbitration-emu.png` ont été inspectées : les deux montrent « RAM HORLOGEE A/B », FORMAT, lecture, écriture/relecture et « COMPARAISON : OK ». Ces PNG restent hors Git. Le diagnostic externe est supprimé ; l'assembly LocalEmulationTests Release est reconstruite sans ce fichier, zéro erreur et trois avertissements préexistants. Les médias `clocked-native.dat/.dsc/.emu` ont été supprimés dans les finally ; les ROM, configurations et médias de l'utilisateur n'ont pas été modifiés. Cette preuve clôt le passage de ces commandes par le raccordement horlogé, pas l'audit final de toute la carte ni les autres machines.

### Annulation entre les fronts et coût du tampon horlogé

RESET du tampon suivi immédiatement d'une nouvelle demande entre deux fronts ne doit pas conserver l'adresse échantillonnée de la requête annulée. Chaque port possède maintenant une génération d'annulation, distincte de celle de l'autre port. L'arbitre invalide l'échantillon ancien avant de sélectionner le nouveau cycle. Les deux nouveaux essais A/B imposent l'absence de mutation au premier front suivant, puis la complétion du nouvel octet au cycle suivant. Le passage ciblé donne **26/26 réussites, 73 ms**.

Le banc autonome de coût effectue 50 000 transferts de chauffe puis un million d'écritures de latch, cycles CLKA/CLKB et lectures de complétion. Il vérifie les pointeurs, leur bouclage, le dernier octet et zéro allocation ; il n'impose aucun seuil de temps dépendant de l'hôte. Mesures initiales isolées : **Release 120,097 ns/octet (8,327 Mo/s), Debug 1 507,152 ns/octet (0,664 Mo/s)**, zéro octet alloué. Ce ne sont ni des MHz CPU BBC ni un débit de machine complète.

Le premier saut direct au front CLKA manquait la libération CLKB avant une nouvelle requête annulée/remplacée : **40/41 essais ciblés réussissaient**, et cette optimisation n'était pas valide. La libération CLKB fait désormais partie des événements à traiter. Recalculer les diviseurs à chaque événement dégradait aussi le coût Release (227,771 ns/octet sur le passage observé). Les diviseurs sont maintenant recalculés uniquement à la programmation 7Fh et lors du changement de source ; les phases, les fronts CLKA et les libérations CLKB restent conservés. Le banc retenu mesure **Release 138,937 ns/octet (7,197 Mo/s), Debug 391,143 ns/octet (2,557 Mo/s)**, zéro allocation. Le gain observé Debug est d'environ 3,85 ; les mesures Release isolées ne montrent pas de gain établi sur la valeur initiale et ne sont pas présentées comme une amélioration statistiquement démontrée.

Après ces corrections et optimisations, **819/819 cas ACB-4000/hôte passent en Release (6 s) et Debug (41 s)**. Les captures CPU précédentes valident le raccordement avant cette dernière optimisation ; les essais regroupés contrôlent les champs, les annulations et les chronologies du code optimisé.

L'audit de clôture identifie toujours une limite fonctionnelle explicite : `EndSendDiagnostic` refuse le dump matériel complet 61h sans `IAcb4000RegisterBus`, car la page 4000h–40FFh n'est pas entièrement décodée. Les commandes RAM 62h/64h et PATCH 63h des registres décodés sont réalisées ; cela ne réalise pas le dump de toute la page. Les parties plus anciennes de cette fiche décrivent des étapes historiques et ne doivent pas faire disparaître ce reste. La carte n'est donc pas annoncée entièrement terminée ni committée.

### Relevé moderne de la carte 4000A : correspondances et limites

L'index Bitsavers contient un [relevé manuscrit moderne ACB4000A, ARD 2025](https://bitsavers.org/pdf/adaptec/ACB-4000/ACB4000A_Schematic_ARD_2025.pdf), neuf feuilles examinées en rendu. Ce n'est pas un schéma constructeur de la révision B 400108-00. La feuille 1 indique un quartz CPU de **6 MHz**, différent des **5 MHz** lisibles sur la carte de référence déjà examinée. Elle ne doit donc pas remplacer silencieusement sa cadence de 2,5 MHz en T-states. La feuille 2 montre 8085, 8156 RAM/E/S et EPROM 2764/27128. La feuille 5 montre AIC-010 et AIC-300, SYSCLK relié à CPUclk, CLKA vers CLK du tampon et CLKB vers AReq ; la feuille 7 montre l'AIC-250 et son quartz de 10 MHz. Ces correspondances confortent la séparation des sources et des circuits, sans prouver l'identité de toutes les adresses entre les révisions.

Les feuilles 3–4 montrent les latches et drivers SCSI, la feuille 6 les signaux de sélection, tête, STEP et les entrées lecteur, et les feuilles 8–9 le séparateur/VCO analogique. Aucun tableau complet de lecture 4000h–40FFh de la révision 400108-00 n'est fourni par ce relevé. Il ne justifie pas de remplir ses ports inconnus avec des constantes supposées ni de prétendre le dump 61h réalisé. La copie PDF reste une référence de travail hors Git. Les neuf rendus temporaires sont supprimés après transcription.

### Interface hôte AIC-300 : décodage constructeur

Les pages 8 à 12 de la fiche AIC-300 distinguent les ports externes 50h/51h du contrôle interne 52h. Les premiers donnent accès au bus de données hôte ; une écriture 50h produit le strobe LO du latch externe. 51h sert à l'octet supérieur dans un montage 16 bits. Ce ne sont pas des alias de RAM supplémentaire. La fiche ne définit pas pour autant une valeur constante de lecture de toute la page 4000h–40FFh.

52h conserve les latches BIE (bit 2), BOE (bit 3), SEL-OUT (bit 6) et BSY-OUT (bit 7). SEL-IN et BSY-IN sont échantillonnés en lecture aux bits 4 et 5. Le bit 1 est inutilisé. Une écriture du bit 0 demande l'arbitrage : attendre SEL-IN et BSY-IN inactifs, puis 100 ns, avant de positionner BSY-OUT et BOE. En lecture, ce bit reflète BSY-OUT, pas une demande encore en attente. Les commandes manuelles BIE/BOE s'ajoutent aux enables DMA. BOE DMA est retiré à réception d'ACK.

Le registre 59h remet RAP/WAP/STOP à zéro à chaque écriture ; son bit 0 maintient RESET et efface alors les autres latches internes. Les broches d'entrée empruntées au montage restent échantillonnées : réinitialiser le composant ne doit pas effacer l'état électrique fourni par son propriétaire.

L'implémentation ajoute ce contrôle 52h et son attente numérique au tampon existant. Le montage transmet SEL, BSY et ACK et avance le délai depuis sa cadence propriétaire. Cela ne prétend pas achever les ports externes 50h/51h ni le dump matériel complet 61h. Les cinq rendus de lecture sont temporaires et sont supprimés après cette transcription.

**Validation regroupée Debug : 828/828 réussites, 41 s**, dont neuf nouveaux cas autonomes : bits lus/écrits, trois combinaisons SEL/BSY occupées, intervalle interrompu, annulation et durée maximale, RESET, BOE à ACK et BIE manuel après DMA. Les 819 cas précédents passent dans le même lancement. Ce résultat porte sur le contrôle 52h ajouté ; il ne valide pas encore ses sorties manuelles sur le câblage SCSI complet ni les accès externes 50h/51h. Aucun nouveau passage Release ni nouvelle capture invitée n'est revendiqué pour cette étape.

### Port externe 50h et sorties hôte raccordées

La carte huit bits possède désormais son latch de sortie externe LO. PATCH 50h charge ce latch sans copier la RAM ; le DMA READ charge le même latch après la complétion RAM. BOE rend sa valeur visible sur le bus. RESET désactive les drivers mais ne réinitialise pas ce latch externe, qui n'a pas de broche RESET. BIE autorise la lecture des données présentes sur les lignes ; sans récepteur actif, ce port n'est pas annoncé lisible par le diagnostic. La voie supérieure 51h des montages 16 bits n'est pas inventée sur ce montage huit bits.

SEL/BSY manuels et données BOE sont raccordés aux sorties du bus existant, puis relus comme entrées physiques par 52h. Les données à collecteur ouvert combinent les bits assertés ; le latch initiateur inactif n'est pas lu comme un second driver pendant une phase cible→hôte. La connexion de commande automatique est distincte : positionner manuellement BSY ne déclenche pas la réception d'un CDB. Les transitions de fin de message sont transmises immédiatement au tampon, avant l'intervalle d'arbitrage suivant.

Premier passage regroupé Release : **834/835 réussites, 7 s**. Le nouvel essai d'arbitrage a révélé que BSY libéré en fin de message n'était transmis au tampon qu'au tick suivant. Après synchronisation en sortie du traitement de phase : **835/835 réussites, 5 s**. Les sept nouveaux essais couvrent les drivers manuels, la résolution des données, RESET, le latch partagé avec le DMA et l'arbitrage natif après un vrai échange de diagnostic.

Un huitième essai vérifie 50h sans/avec BIE et le rejet du port supérieur 51h. Le passage Debug donne **835/836 réussites, 46 s** : le contrôleur rejetait 51h après les quatre paramètres comme attendu, mais le banc exigeait à tort l'envoi du cinquième octet de PATCH. Le test corrige uniquement cette attente et passe ensuite isolément en Release : **1/1, 24 ms**. Il lit bien la dernière donnée de l'en-tête encore présente sur le bus, pas une valeur préparée dans une seconde RAM. Aucun passage groupé 836/836 ni capture CPU après ces changements n'est revendiqué ici.

Le dump matériel complet 61h reste distinct de ces accès décodés : la lecture des ports non pilotés et des autres périphériques de la page n'est pas résolue par l'ajout de 50h/52h. Le refus natif de ce dump reste donc un écart ouvert, sans le masquer avec un remplissage constant de toute la page.

### Dump 61h natif et lectures non pilotées

Le manuel OEM, §6.3.1.14–15, indique que SEND DIAGNOSTIC prépare le transfert puis RECEIVE DIAGNOSTIC fournit le préfixe alloué. La préparation n'effectue aucune lecture : le refus de 61h fondé sur l'absence d'un bus externe était une restriction logicielle, supprimée. Le transfert utilise maintenant la même page matérielle que PATCH, les vrais pointeurs et les registres AIC ; il n'existe pas de tableau parallèle de 256 registres simulés.

Les registres pilotés renvoient leur valeur et conservent leurs effets : notamment l'acquittement de 79h n'a lieu que si le préfixe demandé atteint ce registre. Les lectures de registres non pilotés, d'une pile vide et de 50h sans récepteur actif ne produisent pas une erreur de commande. Pour ces seules lectures, le modèle numérique conserve la dernière donnée explicitement pilotée lors d'un accès mémoire de diagnostic. Les écritures sans décodeur dans la page ne créent aucun état de registre mais pilotent cette donnée numérique. RESET des ASIC ne remplit pas cette page ni ne réinitialise ce latch de convention.

**Limite précise :** cette convention est déterministe, sans allocation et sans résistances fictives, mais ce n'est ni une mesure des tensions flottantes ni une émulation des fetchs et du bus interne complet du 8085. Les octets de tels ports n'ont aucune valeur constructeur garantie dans les documents obtenus. Les tests de cette convention ne prouvent donc pas leur identité octet par octet avec une carte physique. Le remplacement explicite par `IAcb4000RegisterBus` conserve la priorité sur la page pour un propriétaire fournissant ces cycles. Les lectures pilotées ne sont jamais remplacées par cette convention.

Le firmware 400108-00 transfère 255 octets de mémoire après ses quatre octets d'en-tête, malgré la longueur annoncée 0104h. Ce comportement déjà établi reste conservé. Les allocations 0, 1, 3, 4, 5, 83, 126, 259, 260 et 1024, la consommation du dump, sa conservation à RESET et les effets de statut en lecture partielle/complète sont testés dans le modèle natif. **849/849 cas ACB-4000/hôte passent en Release, 5 s.** Ce résultat remplace les anciens refus natifs décrits dans les étapes historiques précédentes ; il ne remplace pas les preuves invitées ou le build de livraison.

### Preuve 6502 du diagnostic et validation groupée

Le BBC Model B 32 Kio démarre avec les fichiers existants `os12.rom`, `basic2.rom` et `saa5050`, sans image disque et sans sortie audio Windows. Un programme chargé en RAM 2000h est lancé en tapant `CALL &2000` dans BASIC : aucun changement du PC ni interception des appels MOS. Les commandes et octets de données passent par FC40h–FC43h. Le 6502 prépare le dump 61h, reçoit les 259 octets, compare l'en-tête et le contrôle hôte piloté, vérifie le statut d'erreur d'une nouvelle réception sans préparation puis prépare un second dump limité à ses quatre octets d'en-tête.

Le diagnostic assembleur temporaire avait initialement un saut relatif trop long dans son affichage : le premier essai échoue avant l'exécution du programme. Ce saut est remplacé par un saut absolu, puis **le scénario invité passe 1/1 en Release, 4 s**. La capture `artifacts/TestsChips/Acb4000/BbcMicroModelB/Release/hardware-diagnostic-61.png` a été examinée ; elle affiche les contrôles et « RESULTAT : OK ». Les assertions hôtes contrôlent aussi le marqueur de fin écrit par le CPU, son indicateur d'erreur nul, l'en-tête et 52h dans le dump reçu. Ce contrôle de données pilotées ne prouve aucune valeur analogique des octets flottants.

Le fichier temporaire `AcornEmuWinchesterHardwareDiagnostic.cs` est supprimé. L'assembly LocalEmulationTests Release est reconstruite sans lui et les essais existants du relais Winchester/SCSI réussissent **58/58, 4 s**. La machine est réinitialisée et son entrée clavier vidée dans finally ; aucun média, configuration ou firmware utilisateur n'est écrit. La capture est conservée hors Git.

Le passage regroupé final de cette étape réussit **849/849 en Debug, 50 s**, en plus des **849/849 Release, 5 s**. Ces validations ne sont pas le script de livraison Debug All et ne ferment pas à elles seules les autres exigences d'audit de la carte.

### Coût du contrôleur complet après raccordement hôte

Le banc autonome `Acb4000SchedulingCostTests` utilise cinq contrôleurs indépendants par état, une horloge propriétaire de 2 MHz, 50 000 ticks de chauffe puis 200 000 ticks mesurés. Il vérifie zéro allocation et les signaux finaux, sans seuil de temps. Le deuxième état conserve REQ en attente d'ACK sans atteindre l'expiration de 250 ms. Ces durées incluent le contrôleur et ses horloges, pas le CPU BBC, le rendu, un média ou le relais Acorn.

Avant optimisation des publications hôte : **Debug repos médiane 254,056 ns/tick (237,818–264,378), attente ACK 292,479 (278,206–301,306)** ; **Release repos 192,060 (182,686–227,888), attente ACK 144,028 (140,118–151,113)**. Zéro allocation dans les vingt lots mesurés. La différence repos/ACK en Release dépend aussi de la chauffe et du JIT ; elle n'est pas interprétée comme un coût physique supplémentaire du repos. Le premier lancement Debug au niveau normal n'affichait pas les sorties du banc ; seul ce banc a été relancé avec le logger détaillé pour relever ces valeurs.

Les entrées et sorties hôte sont désormais publiées seulement lorsqu'elles changent. ACK est transmis à l'ASIC avant le calcul de BOE ; une modification de SEL/BSY en sortie est immédiatement rebouclée sur les entrées. Le DMA ne possède plus un second driver de données automatique indépendant de BOE. L'ACK reçu est mémorisé jusqu'au prochain cycle : son retrait ne doit pas réactiver l'ancien octet avant consommation du cycle. Le BOE manuel reste prioritaire par son OR matériel. Les octets initiateur sont aussi résolus avec les drivers manuels au moment de leur prélèvement, y compris avant le cycle RAM WRITE.

L'horloge système conserve la même fraction et le même nombre de fronts : quotient entier et reste du rapport sont calculés à la construction, puis une addition et un report remplacent la division à chaque tick. Les avancements au repos conservent la division unique par bloc et la même phase. Une cadence propriétaire nulle ou négative reste rejetée avant ce calcul ; elle ne produit pas une division par zéro.

Après ces changements, mesures isolées : **Debug repos 217,218 ns/tick (211,657–217,617), attente ACK 261,259 (260,125–373,612)** ; **Release repos 19,051 (18,761–24,015), attente ACK 35,330 (32,687–35,801)**, zéro allocation. Le banc comporte maintenant une chauffe préalable de dix millions de ticks pour laisser le JIT Release sortir de son premier palier. Les premiers relevés Release changeaient de palier pendant les mesures (un minimum observé de 20,641 ns contre une médiane de 198,642 ns) ; **aucun gain Release de facteur dix n'est déduit de ces valeurs non comparables**. En Debug, les baisses de médiane observées sont d'environ 14,5 % et 10,7 %, sans changement des options d'optimisation du projet. Ces coûts ne sont ni des FPS ni des MHz de la machine entière.

Validation après corrections regroupées : **861/861 en Release, 6 s ; 861/861 en Debug, 44 s**. Le groupe comprend les nouveaux contrôles de driver unique, ACK haut/bas, collisions de données COMMAND/DATA OUT, octet résolu réellement enregistré et horloges fractionnaires. Les essais médias finaux DAT/DSC, Adaptec MFM et EMU passent **124/124 en Release, 917 ms**. Ils portent sur les lecteurs/écrivains et projections du moteur média, pas sur un nouveau lancement de l'interface graphique.

### Audit de clôture du modèle numérique

Les états de commande et de contexte résident dans la RAM de carte ; microprogramme, ECC, GPIO, horloges, pointeurs et interface hôte appartiennent aux objets AIC possédés par le contrôleur. PATCH atteint ces objets et leurs effets, sans copie de registres dans le module Acorn. Le compteur de champ sériel est distinct de la complétion RAM retardée ; NRZ conserve son pipeline de quatre bits et les deux fronts RAM nécessaires. Le raccordement couvre les deux ports documentés et les canaux série READ/WRITE/FORMAT, sans voie de lecture logique de remplacement.

Les traces et chiffres des étapes précédentes constituent leur historique ; les anciens diagnostics « ouverts » ne décrivent plus l'état présent. Les limites conservées sont celles déclarées : conventions numériques pour entrées/lectures non garanties, délais nominaux et variantes de firmware identifiées, pas reproduction analogique ou exécution d'un CPU 8085 interne. Les autres machines BBC et les autres familles Acorn ne sont pas déclarées entièrement finalisées par cette clôture de contrôleur. Les captures restent hors Git et les diagnostics dépendant de fichiers externes ont été retirés après leurs essais.

La livraison Debug complète avec les 24 modules est construite et vérifiée le 5 novembre 2026, code de sortie 0. L'exécutable se trouve dans `build/Debug/Retro Media Lab/retromedialab.exe` ; la DLL Chips de `lib` correspond par SHA-256 au binaire courant compilé. Le détail de l'inventaire livré est dans la fiche machine Winchester. Aucun paquet GitHub ou NuGet n'est publié par cette livraison locale.

### Raccordement Winchester sans consultations de repos — 6 novembre 2026

Le composant ACB-4000 de Chips est inchangé par ce bloc. Le relais de carte Winchester conserve désormais son compteur de périodes sources et annonce une prochaine échéance seulement lorsque son traitement actif le nécessite. La carte BBC conserve cette échéance localement ; elle ne rappelle plus le relais à chaque cycle lorsqu'il est au repos. Avant les accès bus, changements de vitesse, reset ou changement de média, le relais restitue exactement les périodes écoulées par le chemin d'avance inactive déjà existant. Les périodes actives restent traitées dans leur ordre antérieur ; les horloges, fractions, phases de rotation et délais de commande ne sont pas modifiés. Aucun thread ni agenda central n'est ajouté.

Validation du relais et des images : 59 tests réussis en Debug (35 s), puis 59 en Release (5 s). Les nouveaux scénarios comparent les données et délais de commande après des avances de 1 à 2000003 périodes, le reset et plusieurs vitesses, ainsi que la suppression du réveil au repos et sa reprise après un accès. Les tests préexistants des transactions, deux unités, interruptions, écriture, formatage et mécanique font partie du groupe. Ce résultat n'est pas une nouvelle preuve exhaustive de toutes les puces du Master.

Banc complet Master 128 avec EmulatorCore, MOS 3.50, HDD monté protégé et mélange sonore : comparaison alternée Debug 19,989 / 20,124 ms par image avant, 19,247 / 19,130 après, soit environ 4,3 % de temps en moins. Même configuration, même DLL Chips dans les deux processus, 6389758 cycles CPU mesurés, PC E9C1 et empreinte vidéo identiques. En Release, le passage après changement donne 3,592 ms/image ; le précédent passage donnait 3,961 ms, mais la comparaison alternée Release n'a pas été validée à cause d'une incompatibilité de version dans le banc : aucun pourcentage Release confirmé n'est revendiqué. Conditions et limites dans `docs/tasks/machines/BbcMaster/hdd-performance.md`.

Ces mesures couvrent la carte, EmulatorCore et le mélange audio, pas la sortie audio Windows ni la présentation WPF. Aucun build de livraison ni manipulation de la session utilisateur n'a été effectué pendant ce bloc. La baisse liée à la perte de focus reste à expliquer séparément.
### Reprise des notifications du port cible — 7 novembre 2026

Avant modification : 783 tests fonctionnels ACB-4000 Debug réussis, zéro échec ni ignoré, 43 s (bancs CostTests exclus). Le raccordement actif relisait encore BSY/REQ/phase/données dans deux passes de Synchronize à chaque tick, même sans mutation. Le port publie désormais OutputsChanged depuis ses mutations ; WinchesterController retient cette notification et effectue ses deux passes de rebouclage à la même fin de traitement matériel qu'auparavant, uniquement si les sorties ont changé. Les accès CPU conservent leurs synchronisations explicites ; aucune cadence, phase REQ/ACK ou période d'attente n'est retirée.

Premier contrôle du nouveau test : 39 réussites et deux échecs (62 ms), car les transitions de SEL reçu n'étaient pas publiées sur les chemins sans transfert de SetInitiator. La publication du niveau résolu est complétée à la sortie de ces chemins, après résolution des mutations, sans état intermédiaire au reset. Les deux variantes du test couvrent 2 et 10 MHz, phases, données, ACK, drivers manuels, timeout et absence de notification durant les clocks stables. La validation regroupée après correction est en cours ; ne pas déclarer ce bloc terminé avant ses résultats et la comparaison de carte.

Le premier montage du banc de comparaison utilisait ADFS 1.33 et n'atteignait pas *ADFS (Bad command, zéro bloc lu) : ses chiffres ne sont pas une mesure d'accès HDD valide. La sélection est remise sur ADFS 1.30 variant 1 et Hope DFS, conformément au scénario Model A déjà consigné ; les assemblies de référence restent celles d'avant modification. Aucune correction du contrôleur n'est déduite de ce montage de firmware incorrect.

Après correction des publications : 785 tests fonctionnels ACB-4000 Debug réussis, zéro échec ni ignoré, 44 s. Le groupe comprend les deux nouveaux contrôles de notifications, les phases de bus, transferts, diagnostics, mécanique et canaux physiques déjà couverts. Les 63 contrôles `AcornEmuScsiTests` / `AcornEmuWinchesterStorageTests` réussissent en Debug, zéro échec ni ignoré, 34 s : reset, deux unités, lecture/écriture/formatage, interruptions, avancement groupé et phases temporelles. Les tests de coût isolé ne sont pas relancés ; la décision de conservation attend la carte complète. Les avertissements de compilation CA1416, CS8604 et CS0649 des autres modules/infrastructures sont conservés, sans erreur.

Première référence valide sur la carte Model A 32 Kio Debug : MOS 1.20, BASIC II, SAA5050, Hope DFS, ADFS 1.30 variant 1 en banques 8/12, contrôleur WD1770 présent sans disquette, Winchester monté en lecture seule depuis la copie DAT/DSC de travail préexistante. 120 images de chauffe, *ADFS, 120 images, puis 100 mesurées au repos ; huit LOAD du fichier INVADER avec leurs temps de saisie émulée et 20 images supplémentaires chacun forment 640 images mesurées. RunFrame et drainage audio inclus, pas de fenêtre ni sortie Windows, aucun test/build concurrent. Le rendu final est haché après la mesure.

Référence : repos 18,063316 ms/image, 55,36 FPS calculés, 2,211 MHz, 110,5 %, 3 993 602 cycles, PC E469, zéro bloc lu ; vidéo SHA256 10FB3F302C1435E781176CFCCDB92B231A34022EB5484D56B4C934B347987921. Lectures : 58,494673 ms/image, 17,10 FPS, 0,683 MHz, 34,1 %, 25 559 045 cycles, PC 8056, 265 blocs lus ; vidéo C5D9E065E894E7AF47F2FEF49C68746BF1D23B3FFD4B2A2916432E8E4DFEBEEB. Le catalogue affiche réellement INVADER WR et les LOAD s'exécutent sans erreur. Le budget de 19,97 ms n'est pas tenu pendant ce scénario actif ; ne pas le masquer par la valeur au repos.

Premier candidat : repos 17,026189 ms/image, 58,73 FPS, 2,346 MHz ; lectures 53,508710 ms/image, 18,69 FPS, 0,746 MHz, 37,3 %. Cycles, PC, compteurs de blocs et empreinte finale vidéo identiques à la référence dans les deux scénarios. Gain actif de ce premier couple : environ 8,52 % de temps en moins. La variabilité du repos n'est pas attribuée au changement, qui cible surtout les phases actives. Le budget matériel reste non atteint pendant les lectures.

Les huit LOAD sont demandés dans une fenêtre fixe de 640 images ; le dernier transfert n'est pas nécessairement achevé au dernier relevé (PC 8056 dans la ROM, 265 blocs lus). Ce passage compare donc exactement la même progression active, pas huit latences de chargement complet. La capture 20261107-015702-32k-catalogue-hdd-carte-sans-interface.bmp est conservée dans TestsChips/Acb4000/BBC Micro Model A et TestsMachines/Acorn/BBC Micro Model A ; elle montre le catalogue et les commandes réellement émises, sans constituer une preuve de fin des huit chargements.

Deuxième couple alterné référence/candidat, sans compilation ni tests concurrents : référence repos 17,316997 ms/image et lectures 56,997739 ; candidat repos 17,727165 et lectures 52,545768. Les quatre passages conservent les mêmes cycles, PC, blocs lus et empreintes finales. Moyenne des deux références actives 57,746206 ms/image ; moyenne des candidats 53,027239, soit environ 8,17 % de temps en moins. Le repos n'établit pas de gain (variabilité et plages croisées). Ce résultat justifie le retrait du sondage, mais pas la clôture de l'optimisation du contrôleur : la lecture active demeure très au-dessus des 19,97 ms.

Profil du BBC complet après ce bloc : 47 406,97 ms sous RunFrame ; Winchester.AdvanceTo 55,14 % inclusifs, ClockCell 38,39 %, ReadChannel.Feed 15,47 %, vidéo 20,96 %. Les parts s'imbriquent. Analyse, méthodes, consultations établies et fichiers à traiter dans [clock-cell-profile.md](AdaptecAcb4000/clock-cell-profile.md). Le bloc de notifications du port est conservé pour son gain global confirmé et ses contrôles de séquence ; la poursuite porte sur les sorties de lecteur et les cellules physiques, pas sur une nouvelle modification CPU.

La capture du candidat a été inspectée : catalogue INVADER WR et huit commandes LOAD visibles. Le diagnostic, la référence de DLL et les deux fichiers de trace ont été supprimés après consignation ; le banc temporaire préexistant est reconstruit avec Program.cs (code 0). Les nouvelles assertions du bus sont autonomes et restent dans Chips.Tests. Aucun RML lancé/fermé, aucun média ou réglage utilisateur écrit, aucun artefact destiné au commit. Cette étape ne termine pas l'optimisation ACB-4000/Winchester ni le Model A.

## 7 novembre — notifications mécaniques STEP et entrées ST-412

Les pas sont publiés depuis leur émission effective ; le relais conserve les supports montés et ne propage les retours READY/SEEK COMPLETE/TRACK ZERO que lors des changements, en conservant le retour au front suivant et le délai de stabilisation. 64 tests Winchester/SCSI Debug réussis (34 s). Comparaison alternée de carte Model A complète : moyenne HDD 54,93 → 52,16 ms/image, mêmes cycles, lectures et empreinte finale ; la référence est variable et le budget matériel reste non atteint. Détails, première assertion de test corrigée, conditions et capture réelle : [profil du chemin cellules et mécanique](AdaptecAcb4000/clock-cell-profile.md). Les portes de lecture/écriture restent à traiter ; ce résultat ne clôture pas le contrôleur.

## 7 novembre — portes et sélection ST-412 reçues par notification

RG/WG sont résolues aux mutations des phases, entrées READY/FAULT, RESET ECC et portes des canaux. Le relais retient les sorties reçues ; aucune publication globale ajoutée par tick. Les horloges, cellules MFM et prises en compte en cours de champ sont conservées. 793 tests ACB-4000 Debug (41 s), puis 64 Winchester/SCSI Debug (27 s) réussis sans échec. Carte Model A complète, moyenne alternée active 51,36 → 45,34 ms/image (11,73 % en moins), mêmes cycles/blocs/trame finale ; budget de 19,97 ms encore non tenu. Capture réelle et résultats détaillés dans [le profil du chemin cellules](AdaptecAcb4000/clock-cell-profile.md). La sélection répétée du curseur de piste reste à traiter ; ne pas déclarer le contrôleur terminé.

## 7 novembre — sélection de piste : tentative non retenue

Deux formes de sélection par invalidation ont été comparées sur le BBC complet. Les tests fonctionnels passaient, mais le gain global n'a pas été confirmé par les passages alternés ; les sources du relais sont donc restaurées à dc2e151a8. Les deux nouveaux tests de changement de tête/cylindre et reset restent conservés et passent sur cette référence (10 s). Les gains STEP/portes précédents sont préservés ; aucun nouveau gain de cadence n'est annoncé. Les captures de la tentative restent hors Git, notamment 20261107-025230-32k-hdd-selection-piste.bmp, rendu réel inspecté avec les mêmes données.

Un nouveau profil de la carte restaurée situe ClockCell à 34,80 % inclusifs, dont ReadChannel.Feed à 16,82 %, et TrackCursor.Select à seulement 0,95 %. Ne pas additionner ces coûts imbriqués ni les interpréter comme des durées par appel. [Conditions, passages et suites prioritaires](AdaptecAcb4000/clock-cell-profile.md). La chaîne d'horloges RAM et de lecture reste à terminer ; aucune machine clôturée.

## 7 novembre — demandes RAM et fronts internes AIC

Les latches de demande RAM publient leurs changements ; le raccordement retient les niveaux et identités, et reçoit les fronts utiles depuis le générateur propriétaire des phases. Il ne recherche plus périodiquement sa prochaine frontière dans une autre instance. Les phases sans demande, CLKA/CLKB, priorités et annulations restent couvertes : 799 tests ACB-4000 et 66 cas Winchester/SCSI Debug réussis. BBC complet, moyenne active alternée 45,79 → 43,87 ms/image, 4,19 % de temps en moins, mêmes cycles, lectures et trame finale. Le budget matériel reste non atteint avec HDD actif. [Chronologie, sources modifiées, mesures et capture](AdaptecAcb4000/aic-buffer-requests.md).

## 7 novembre — paramètres ECC résolus aux écritures des registres

Le masque de longueur, le polynôme effectif et l'autorisation de progression sont conservés depuis leurs mutations, au lieu d'être recalculés pour chaque bit. Aucun front sériel ni syndrome intermédiaire supprimé. 801 tests ACB-4000 et 66 Winchester/SCSI Debug réussis. Carte complète : moyenne alternée HDD 43,68 → 42,21 ms/image (3,37 % de temps en moins, avec variation notable entre couples), mêmes cycles/blocs/trame finale. Capture réelle inspectée et [détails du bloc ECC](AdaptecAcb4000/clock-cell-profile.md). Budget actif encore non atteint ; aucun nouveau statut terminé pour le contrôleur ou la machine.

## 7 novembre — phase ST-412 rejetée et budget actif reporté

Le cache de phase nanoseconde est retiré : les passages alternés du BBC complet n'établissent aucun gain (références actives 42,56 / 42,70 ms/image ; candidats 41,77 / 43,73). Les quatre nouveaux contrôles de fin de lecture restent conservés et passent sur le relais restauré, en 1 s ; 70 cas Winchester/SCSI avaient passé sur le candidat. Aucun changement de Chips conservé dans ce bloc. Captures, collecte des références et conditions dans [le profil du chemin cellules](AdaptecAcb4000/clock-cell-profile.md).

Report explicite selon la directive de durée : le budget Debug actif 19,97 ms/image reste incomplet, autour de 42,6 ms/image sur la dernière référence. Les recherches locales prolongées et les tentatives sans gain ne doivent plus bloquer les autres points du Model A. Conserver les gains STEP/portes/AIC/ECC et les preuves fonctionnelles ; ne pas déclarer le contrôleur finalisé pour ce budget. La suite reste sur les composants ouverts de la même machine ; aucune nouvelle machine n'est validée par ce report.

## Master 128 — deux ports natifs, 7 novembre 2026

Sans modification du contrôleur, MOS 3.50 et son ADFS exécutent CAT/SAVE/LOAD sur chacun des deux ports successivement. Un motif de 256 octets est enregistré dans un DAT/DSC dédié puis intégralement comparé après fermeture et réouverture en lecture seule : deux réussites, huit blocs écrits et quinze lus par parcours. Captures réelles inspectées sous `artifacts/TestsChips/Acb4000/BBC Master 128/` : `20261107-102425-master128-adfs-persist-pass.bmp` et `20261107-102537-master128-adfs-port1-persist-pass.bmp`, également dans TestsMachines.

Port 0 Debug, fenêtres montage/SAVE/après LOAD : 26,764500 / 20,587888 / 19,062177 ms/image. Port 1 : 28,527359 / 22,560068 / 20,993935. Les dernières fenêtres sont au repos après une lecture déjà achevée pendant la saisie ; elles ne prouvent pas le coût actif. Fréquences, cycles, compteurs, conditions et état d'attente initial sans disquette sont consignés dans [la preuve Master](<../../project/emulations/Acorn/BBC Master/Master 128/adfs-persistence.md>). Aucun défaut silicium établi, aucun comportement matériel modifié ; le budget Debug actif reste ouvert.

## Master 512 / DOS Plus — contrôle intégré du 8 novembre 2026

Contrôleur existant, sans changement de code, de fréquence ou de délais. DAT/DSC ADFS 306 × 2 × 33 × 256 créé par le SDK ; HDISK 1.01 crée la partition DOS 4 Mo et la rend amorçable. COPY depuis la disquette vers C:, TYPE et comparaison indépendante des 512 octets publiés dans le DAT réussissent. Nouvelle instance avec seule disquette ADFS vierge : DOS Plus 2.1 atteint C> depuis le HDD et relit le témoin ; 718 puis 724 blocs lus, zéro écriture. Captures sous TestsChips/Acb4000/BBC Master 512. [Montage, phases, compteurs et mesures](<../../project/emulations/Acorn/BBC Master/Master 512/hdd-dos-plus.md>). Le relevé 38,46–39,24 ms/champ porte sur la carte entière au repos, pas le seul contrôleur ni un transfert actif. Aucun gain nouveau annoncé.

## 8 novembre 2026 — revue statique du raccordement hôte interne

SynchronizeHostInterface était appelé deux fois par Tick, puis relisait SEL/BSY, ACK et HostOutputState pour comparer les sorties du tampon. Le tampon publie maintenant HostOutputsChanged aux mutations de ses sorties (programmation/reset, ACK, arbitrage achevé, début/fin/annulation des transferts hôte). Aucune publication n'est ajoutée dans un tick périodique ; son niveau est comparé uniquement à ces opérations.

L'ACB reçoit cette notification et celle du bus. Les changements locaux de latch de données, ACK, DMA et stage marquent également la synchronisation nécessaire. SynchronizeHostInterface retourne directement lorsqu'aucune de ces causes n'a changé. Lorsqu'il est nécessaire, le calcul et l'ordre ACK→ASIC→BOE→bus restent à leurs points précédents : ni avancement supplémentaire d'horloge, ni décalage d'un tick, ni mise à jour immédiate d'un chemin auparavant cadencé ne sont ajoutés. Une notification causée par cette propagation reste disponible pour le point suivant ; elle n'est pas effacée après un callback.

Revue des sites de mutation effectuée dans les services concernés ; source seulement, non compilée et non testée pendant la revue demandée. Aucun gain global mesuré et aucune clôture du contrôleur revendiqués. Les validations antérieures ne couvrent pas cette modification.
