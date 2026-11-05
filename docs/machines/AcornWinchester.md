# Winchester Acorn BBC — examen du matériel et du raccordement

Les fiches des composants sont [Acorn SCSI Host Adapter](../chips/Controllers/AcornScsi.md) et [Adaptec ACB-4000](../chips/Controllers/AdaptecAcb4000.md). Elles regroupent leurs fonctions, timings, couverture et limites propres. Le présent document conserve l'examen du montage Winchester, le raccordement aux machines et aux conteneurs, ainsi que l'historique détaillé des résultats. Les raccordements récents seulement compilés n'y valent pas validation fonctionnelle.

## Matériel identifié

Le manuel de service Acorn distingue trois ensembles : disque ST-412, carte contrôleur **Adaptec ACB-4000**, et carte **Acorn Host Adapter** raccordée au bus 1 MHz. Ce ne sont pas trois noms d’une même puce. La carte hôte est construite avec buffers, verrous et bascules TTL : 74LS244/240/373, 74LS138, 74LS74, portes et inverseurs. Le détail des fonctions des circuits de la carte ACB-4000 doit être distingué de son protocole SCSI documenté.

La classe actuelle `Chips/.../AcornScsi` regroupe l’adaptateur hôte, la cible, les commandes, les données de conteneur et les compteurs mécaniques. Elle n’est donc pas déjà une reproduction de toutes les puces de ces cartes. Le service Acorn `Winchester` fournit les images empruntées ; leur possession demeure au cœur.

## Références de travail

- [Acorn Winchester Disc 110/130 Service Manual, août 1984, sections 5.1, 5.3 et 7.2–7.3](https://theoddys.com/acorn/acorn_system_filing_systems/winchester_disks/Acorn%20Winchester%20Disc%20Service%20Manual.pdf).
- [Adaptec ACB-4000 OEM Manual, mars 1984, sections 4–6 et annexes](https://theoddys.com/acorn/acorn_system_filing_systems/winchester_disks/Adaptec%20ACB4000%20OEM%20Manual.pdf).
- [b2 scsi.cpp](https://github.com/tom-seddon/b2/blob/master/src/beeb/src/scsi.cpp) et [B-Em scsi.c](https://github.com/stardot/b-em/blob/master/src/scsi.c), confrontés aux constructeurs ; leur accord ne suffit pas à établir une polarité ou un délai matériel.
- [Schéma Acorn Host Adapter](https://theoddys.com/acorn/acorn_system_filing_systems/winchester_disks/Acorn%20SCSI%20Host%20Adapter%20Schematic.jpg), examiné directement : le PDF de service conserve le texte mais ses pages de schéma sont vides.
- [BeebSCSI, hostAdapter.v](https://github.com/simoninns/BeebSCSI/blob/master/BeebSCSI_CPLD/hostAdapter.v) : reconstruction matérielle des bascules et portes du circuit hôte, avec adaptations AIV explicitement séparées ; [MAME scsi.cpp](https://github.com/mamedev/mame/blob/master/src/devices/bus/bbc/1mhzbus/scsi.cpp) distingue également l’adaptateur et la cible SCSI.

Les copies et textes de travail restent temporairement dans `artifacts/.tmp-acorn-winchester/references/`, hors Git.

## Registres et séquences établis par Acorn

| Accès | Fonction matérielle |
|---|---|
| FC40 lecture | Données cible vers initiateur, avec génération ACK |
| FC40 écriture | Verrou de données initiateur vers cible, avec génération ACK |
| FC41 lecture | MSG, BSY, zéro, zéro, NIRQ, REQ, I/O, C/D sur D0–D7 |
| FC42 écriture | Bascule SEL ; BSY acquitte et libère la sélection |
| FC43 écriture | D0 mémorise l’autorisation d’interruption ; ce n’est pas une commande qui déclenche directement IRQ |

Le verrou IRQ autorisé capture le prochain front descendant électrique de REQ, donc son assertion logique. Le réseau NAND est sensible au niveau : autoriser alors que REQ est déjà actif déclenche aussi le verrou. IRQ reste mémorisé après retrait de REQ jusqu’au retrait de l’autorisation ou RESET. Les données acquittées à FC40 utilisent une autre bascule, libérée quand REQ devient inactif ; le transfert respecte les quatre étapes REQ puis ACK, retrait REQ puis retrait ACK.

Le tableau textuel du manuel appelle D4 NIRQ, alors que l’inverseur du schéma donne au registre le sens IRQ actif haut ; la reconstruction CPLD distingue bien cette polarité externe de celle de l’adaptateur AIV interne. MAME utilise un autre raccourci de polarité/événement dans son callback REQ et ne constitue pas l’oracle de ce point. Les broches physiques sont actives basses ; les signaux exposés par le modèle numérique seront définis en assertions logiques afin de ne pas mélanger ces conventions.

La bascule SEL est déclenchée seulement par FC42 et retirée par BSY ou RESET. Les écritures FC40/FC41/FC43 ne doivent pas commander SEL. La bascule ACK est déclenchée par un accès de données FC40 si REQ l’autorise ; elle n’est pas directement câblée à RESET. Le registre de données n’a pas de RESET non plus : le reset de carte ne doit pas effacer arbitrairement ce verrou.

Le programme de dépannage Acorn attend un état nul au repos, FF si le câble est absent. Après écriture d’un octet de sélection à FC40 puis strobe FC42, il attend BSY et un état égal à 2. BREAK force la condition RESET et la libération du bus. La carte ne possède pas un registre logiciel de RESET interchangeable avec FC41 écriture.

Le manuel Adaptec distingue BSY/SEL, phases commande/données/état/message, REQ/ACK, ATN et RESET. Les délais de bus sont submicrosecondes pour certains fronts ; le maintien RESET indiqué est de 25 µs et le délai de sélection de 250 ms. Ils doivent être représentés dans la cadence de carte sans faire d’un délai arbitraire de deux cycles une règle universelle du protocole.

Le texte normatif OEM donne notamment 200 µs maximum pour la réponse BSY après détection de sélection, 650 ns maximum pour libérer le bus, 450 ns minimum de stabilisation et 45 ns minimum de deskew. Certaines figures du même manuel portent d’anciennes valeurs différentes ; les divergences devront rester visibles dans la fiche de timing, plutôt que choisies silencieusement.

L’ACB-4000 documenté accepte deux lecteurs, LUN 0 et 1. Le code actuel expose quatre unités d’une cible générique. Ne pas attribuer cette capacité à l’ACB-4000 sans identifier un autre contrôleur ou un raccordement réellement compatible ; cette distinction reste à résoudre dans le raccordement des équipements proposés.

## Écarts établis du code actuel — avant correction

- `Write(3, value)` déclenche immédiatement IRQ uniquement pour FF. Il ignore D0 des autres valeurs et ne capture pas un événement REQ ultérieur. B-Em contient le même raccourci : ce n’est pas une validation constructeur.
- `StatusBits` annonce REQ pendant Free/Selection, donnant 20 au repos, alors que le diagnostic Acorn attend 00. Les tests locaux existants attendent eux aussi 20 ; leur réussite ne prouve donc pas ce point.
- La sélection est intégrée à `WriteData` et au booléen `_select`, au lieu de représenter séparément la bascule SEL, l’acquittement BSY et le verrou de données.
- Les délais REQ de deux cycles sont écrits en brut et ne décrivent ni les délais du bus ni le comportement de l’adaptateur.
- La cible accepte certaines commandes SCSI générales, mais plusieurs commandes explicitement présentes dans le manuel ACB-4000 manquent, notamment les tampons, diagnostics, écriture avec vérification et recherche de données. Leur prise en charge doit être fondée sur le modèle de contrôleur visé, pas ajoutée au hasard.
- Le chemin INQUIRY copie une chaîne plus longue que les 28 octets disponibles après l’offset 8 d’une réponse de 36 octets ; c’est un risque d’exception déductible du code, pas encore un résultat de test reproduit dans cette reprise.
- `AcornScsi.Transfers` utilise directement `IDiskImage`, ses méthodes asynchrones bloquées, métadonnées et mécanique. Il faut conserver les conteneurs et l’adaptation des secteurs chez leur propriétaire, tout en fournissant au contrôleur des signaux/données suffisamment précis.

## État de la reprise

L’ULA série BBC est clôturée et committée avant cet examen. `ScsiHostAdapter` isole maintenant les équations de la carte hôte dans les services du composant existant, avec son interface de broches et sans image attachée : verrou de données, état de bus, ACK et SEL, autorisation et verrou IRQ, RESET distribué aux bascules réellement câblées. Le niveau initial du verrou LS373 n’étant pas spécifié, son zéro initial est seulement une initialisation déterministe du modèle ; RESET conserve ensuite sa valeur.

Les **72 essais autonomes passent en Debug et en Release** : 64 combinaisons de signaux/autorisation, toutes les valeurs de D0 du registre d’autorisation, mémorisation IRQ, SEL/BSY, ACK lecture et écriture, absence de sélection lors de la sonde de données, registres non décodés et RESET. La carte hôte est passive entre changements de signaux : elle ne possède pas de Tick artificiel ni ne génère un retard de commande. Ses méthodes Read/Write représentent un accès de bus déjà qualifié par le propriétaire.

Le service de machine utilise encore l’ancien `AcornScsi` : le nouveau composant n’est pas encore raccordé à la cible et n’est pas annoncé comme livré. Les tests locaux existants restent conservés jusqu’à leur adaptation fondée sur les manuels. Le plan détaillé de reprise de la cible, des interfaces de stockage, des validations invitées, de l’optimisation et du nettoyage doit être écrit avant de modifier cette partie. Aucun commit Winchester terminé n’a été créé à ce stade.

## Adaptation du stockage extraite

Le contrôleur reçoit maintenant `IScsiStorage`, un stockage emprunté adressé en octets. `WinchesterStorage`, dans le module Acorn, conserve les accès aux images MediaEngine, l'encodage Base64 du descripteur `acorn.dsc`, la mécanique et le relais de sauvegarde. Il ne possède ni ne détruit l'image. Les valeurs de configuration et les quatre emplacements précédemment proposés n'ont pas été modifiés par cette extraction ; leur fidélité matérielle reste une question distincte.

Les lectures et écritures peuvent traverser plusieurs secteurs du conteneur. Une écriture partielle conserve les octets voisins ; une écriture de secteurs entiers ne relit pas les données remplacées. Le tampon de lecture/modification/écriture partielle provient du pool et est rendu dans `finally`, même après une erreur. Il s'agit d'une adaptation de conteneur, pas d'une reproduction des impulsions ST-506, de l'ECC ou du firmware interne Adaptec.

Résultats regroupés du 4 novembre 2026 : **39 tests réussis en Debug, puis 39 en Release**, sans échec, respectivement 812 et 690 ms de durée rapportée par le lanceur. Ce total comprend les 22 cas existants inchangés et 17 nouveaux cas autonomes de `AcornEmuWinchesterStorageTests` : secteurs de 128/256/512/1000/1024 octets, transferts non alignés, conservation des voisins, absence de lecture lors d'un remplacement complet, limites et dépassement arithmétique, fin d'image, défaut d'intégrité, erreur de lecture avant écriture, protection, paramètres persistants et déplacement physique. Ces durées sont celles des suites ; elles ne mesurent pas le coût d'un accès et ne prouvent aucune optimisation chronométrée. Aucun fichier, média externe, audio Windows ni capture native n'est utilisé dans ce groupe.

## Capacité réelle : ROM, carte hôte et version du contrôleur

Le [désassemblage ADFS 1.30/1.33 de hoglet67](https://github.com/hoglet67/ADFS130/blob/master/src/ADFS130.asm) contient aussi des variantes IDE : seuls les chemins `ELSE` de `PATCH_IDE` ont été examinés pour le raccordement SCSI original.

- À `L8067`–`L8078`, la ROM attend BSY inactif, charge `01`, écrit FC40 puis FC42 et attend BSY actif. La valeur de sélection est donc fixe ; la ROM ne choisit pas automatiquement un autre identifiant SCSI pour les lecteurs 2/3.
- À `LAAC6`, la ROM émet le CDB avec l'octet haut d'adresse provenant de `L1003,X`, également conservé dans `L1133`. À `L823A`, REQUEST SENSE reprend `L1133 AND E0` : les bits d'unité accompagnent la commande adressée à la même cible.
- À `L9A6C`, la sonde écrit 5A/A5 sur FC40, désactive l'interruption via FC43 et relit le verrou. Une écriture FC40 n'est donc pas à interpréter comme une sélection implicite.

Le [manuel constructeur préliminaire du 16 mai 1983, sections 1.1, 4.2 et 6.2.4](https://www.bitsavers.org/pdf/adaptec/ACB-4000/ACB_4000_and_5000_Disk_Controllers_OEM_Manual_Preliminary_May83.pdf), récupéré sur le [miroir Bitsavers](https://ftpmirror.your.org/pub/misc/bitsavers/pdf/adaptec/ACB-4000/ACB_4000_and_5000_Disk_Controllers_OEM_Manual_Preliminary_May83.pdf), distingue explicitement :

| Matériel documenté | Lecteurs du contrôleur | Portée de la preuve |
|---|---|---|
| ACB-4000 | Deux lecteurs ST-506/412, LUN 0 et 1 | Contrôleur identifié par le manuel de service du Winchester Acorn ; confirmé par son manuel OEM de mars 1984 |
| ACB-5500, famille ACB-5000 | Quatre lecteurs ST-506, LUN 0 à 3 | Autre contrôleur décrit par Adaptec ; pas une option cachée ni un jumper ajoutant deux lecteurs à l'ACB-4000 |
| ACB-5800, famille ACB-5000 | Quatre lecteurs à interfaces SA-1000/Quantum Q2000 | Autre interface de disque ; ne pas la présenter comme quatre ports ST-506 de l'ACB-4000 |

La description ACB-5000 comporte aussi des fonctions de protocole supplémentaires, notamment enchaînement et déconnexion/reconnexion. La présence de quatre LUN dans ce manuel ne prouve pas que l'ACB-5500 était livré dans un Winchester Acorn ni que son fonctionnement complet avec cette ROM a déjà été validé. Aucune carte supplémentaire n'a été ajoutée à la configuration à partir de cette seule possibilité.

Le [manuel utilisateur ACB-5500 d'octobre 1985, référence 500504-00](https://ftpmirror.your.org/pub/misc/bitsavers/pdf/adaptec/ACB-5500/500504-00_ACB-5500_SCSI_to_ST506_Users_Manual_Oct85.pdf) confirme ensuite un produit documenté, pas seulement la description préliminaire : sections 1.0–1.3, jusqu'à quatre disques ST-506 ; section 4.4.4, LUN 0 à 3, pris dans le CDB en l'absence de message IDENTIFY. Il décrit un tampon de **2 Kio**, les circuits Adaptec AIC-250, AIC-010 et AIC-300, et un microprocesseur avec mémoire de contrôle de 16 Kio. Il serait incorrect de présenter son fonctionnement comme celui d'un ACB-4000 avec une seule constante de nombre de disques changée. Cette carte peut faire l'objet d'un modèle matériel distinct ; sa présence dans un équipement Acorn d'origine et les échanges réels avec ADFS restent à établir avant de l'annoncer comme configuration Acorn validée.

Le témoignage de raccordement [Gus Donnachaidh, EUG 35](https://acornelectron.co.uk/eug/35/a-hard.html), distingue ADFS 0–3 des deux disques réellement testés sur un contrôleur et décrit aussi les cartes DTC à deux Winchester et deux disquettes. Ce n'est donc pas une preuve de quatre HDD sur l'ACB-4000. L'examen du firmware original et les essais invités doivent conserver cette séparation entre numéro logique, contrôleur sélectionné et connecteurs disponibles.

## Port cible ACB-4000 réalisé séparément

`Controllers/Adaptec/ACB4000/ACB4000/Services/Acb4000Bus` représente le port de la carte Adaptec, pas une puce interne non identifiée. Son interface expose les fils logiques BSY, REQ, I/O, C/D et MSG, le verrou de données sortantes et les entrées SEL/ACK/RST/ATN. Aucune dépendance envers Acorn, MediaEngine ou un conteneur n'est introduite dans ce port. Le test de raccordement convertit ses sorties vers les broches de `ScsiHostAdapter`.

La sélection répond uniquement à l'identifiant câblé, reste annulable avant réponse et n'autorise pas de transfert tant que SEL est maintenu. Chaque demande d'octet établit d'abord sa phase et ses données ; le délai de stabilisation précède REQ. ACK capture l'octet et retire REQ ; le retrait d'ACK termine ensuite le transfert. La commande suivante ne peut pas écraser la phase ou les données pendant ce cycle. Un ACK prématuré ne termine pas un octet non demandé. ATN est exposé au moteur de commandes et ne transforme pas silencieusement une phase active.

| Temps | Origine et représentation actuelle |
|---|---|
| Stabilisation d'une phase : au moins 450 ns | Minimum OEM, arrondi au tick supérieur de la cadence fournie |
| Stabilisation des données suivantes : au moins 45 ns | Deskew OEM, arrondi au tick supérieur ; ce n'est pas une mesure du débit maximal du firmware |
| Réponse BSY à la sélection | Modèle nominal après stabilisation, 450 ns arrondies ; dans la limite OEM de 200 µs aux cadences natives testées |
| Attente d'ACK après REQ | Modèle nominal de 250 ms, minimum OEM avant expiration ; le délai effectif interne exact n'est pas documenté dans la source consultée |
| RESET | Prioritaire, libère immédiatement les sorties et annule les transferts différés ; aucun filtre analogique sous-tick n'est inventé |

L'annexe A du constructeur précise que l'ACB-4000 accepte une impulsion RESET de **50 ns**, tout en recommandant **25 µs** pour un nouvel adaptateur conforme SCSI. Il ne faut donc pas interpréter la recommandation du bus comme un seuil de rejet de 25 µs propre à cette carte.

Résultats du port et de la carte hôte : **104 tests réussis en Debug et en Release**, zéro échec, durées de suite 93/90 ms. Les 32 nouveaux cas couvrent les huit IDs, deux abandons de sélection, les six phases, les cadences 1/2/10/16/100 MHz avec arrondi attendu, ACK prématuré, timeout après 499 999 puis 500 000 ticks à 2 MHz, RESET pendant sélection/délai/REQ/ACK, enchaînement hôte réel des quatre fronts et ATN. Les 72 cas hôte antérieurs restent inclus. Ce sont des tests de port : ni commande de lecture de disque, ni moteur Adaptec complet, ni optimisation chronométrée, ni capture invitée ne sont revendiqués par cette réussite.

La recherche ciblée ACB-5500 avec BBC Micro/Acorn/ADFS n'a pas fourni de montage BBC documenté. Le composant en cours reste donc l'ACB-4000 du Winchester Acorn. L'existence du 5500 n'a pas été convertie en configuration BBC proposée.

## Moteur de commandes Adaptec — groupe réalisé et vérifié

Le service `Controllers/Adaptec/ACB4000/ACB4000/Services/Acb4000` relie maintenant son propre port à un moteur de CDB à six ou dix octets, puis aux phases de données, statut et message de fin. Il ne remplace pas encore l'ancien `AcornScsi` du module ; il est testé directement par les cycles de broches de sa cible, avec supports numériques autonomes.

Le groupe réalisé comprend TEST UNIT READY, REQUEST SENSE, READ/WRITE courts et longs, WRITE AND VERIFY, VERIFY, READ/WRITE BUFFER et READ CAPACITY. Les deux ports réels sont les LUN 0 et 1 ; les autres LUN obtiennent CHECK puis Invalid Logical Unit Number 25, sans appel au fournisseur de lecteurs hors de 0/1. Le statut ne contient pas le LUN dans ses bits réservés. Le sense conserve erreur et adresse après CHECK, est effacé par la commande suivante et traite une allocation 0–3 comme quatre octets ; REQUEST SENSE ne retourne jamais CHECK. Les CDB et réponses utilisent les constantes de format propres au contrôleur.

Un tampon partagé de 1 Kio est conservé, sans allouer tout un transfert multibloc. Le compte nul signifie 256 blocs en format court et 65 536 en format long. Les accès s'arrêtent à une adresse non disponible après les blocs précédents réellement transférés. Les commandes SCSI génériques INQUIRY, PREVENT et SYNCHRONIZE CACHE ne sont pas inventées pour cette carte.

Le manuel OEM 6.3.1.5 impose le transfert d'un bloc irrécupérable avant le statut final CHECK/11. Le contrat `IScsiStorage.Read` et son adaptateur recopient donc aussi les octets portant un défaut d'intégrité et retournent séparément leur état. Le contrôleur effectue une première lecture et au plus cinq relectures ; en cas d'échec persistant, il transmet les octets obtenus avant l'erreur. Ce mécanisme ne prétend pas avoir calculé/corrigé le code ECC physique : l'intégrité disponible est encore celle fournie par le support emprunté.

WRITE AND VERIFY écrit d'abord **tous** les blocs demandés, puis les vérifie bloc par bloc sans données hôte supplémentaires, conformément à 6.3.2.4. VERIFY contrôle l'intégrité sans comparer à un motif hôte et sans appliquer les relectures de READ. Les opérations de vérification ne sont pas présentées comme un test des champs ECC bruts tant que l'interface de support ne les expose pas.

Résultats regroupés : **139 tests Chips réussis en Debug et en Release** (88/85 ms de durée de suite), dont 35 nouveaux cas de commandes, en plus des 104 cas de port/hôte. Les tests d'adaptation/intégration conservés passent à nouveau : **39 Debug et 39 Release** (913/909 ms) après changement du contrat d'intégrité. Ces durées ne sont pas des benchmarks. L'avertissement d'extension de signe du nouvel octet de sense a été corrigé par utilisation explicite de l'unité non signée avant la validation Release.

Restent notamment à réaliser dans ce moteur : MODE SELECT/MODE SENSE et leur persistance au formatage, FORMAT et défauts/interleave, REZERO/SEEK/START STOP et signaux du mécanisme, TRANSLATE, SEND/RECEIVE DIAGNOSTIC, SEARCH DATA EQUAL, commandes optionnelles et cavalier I–J, timings d'exécution/mécanique et débit du tampon. La présence d'un backend vaut provisoirement disponibilité dans TEST UNIT READY ; elle ne remplace pas encore une modélisation des signaux READY/SEEK COMPLETE, des séquences de mise sous tension et des pannes de lecteur. Ces points, l'ECC physique et le raccordement ADFS restent ouverts. Les captures natives, mesures de performance et clôture ne sont pas acquises par les seuls tests de ce groupe.

## Listes de paramètres : codec vérifié, commandes encore à raccorder

`Acb4000DriveParameters` décode et encode l'extent de 12 octets et sa liste optionnelle de lecteur jusqu'à 22 octets. Il valide les octets réservés, la densité MFM, les blocs 256/512/1024, le code de liste, 1–2048 cylindres, 1–16 têtes, 0–2047 pour le seuil de courant réduit et les trois codes STEP. Une liste tronquée laisse intacte la valeur antérieure ; un extent seul conserve les paramètres du lecteur. La précompensation reçue est conservée pour restitution, sans lui attribuer d'effet physique : le 4000 ignore ce champ et emploie le seuil/cavalier décrit dans le manuel.

Les cadences STEP décodées sont 3 000 000, 28 000 et 12 000 ns. La zone d'atterrissage distingue déplacement après le dernier cylindre et déplacement avant zéro. Le bloc par défaut emploie les valeurs documentées de géométrie 306/2 et de courant réduit 150 ; l'encodage du champ de précompensation inutilisé et le code STEP initial sont des valeurs nominales explicites, pas une mesure d'une ROM interne.

**180 tests passent en Debug et en Release**, zéro échec, durées de suite 92/97 ms ; les 41 nouveaux cas couvrent ce codec et ses limites. Cette réussite ne signifie pas que MODE SELECT/MODE SENSE sont déjà branchés au moteur, que FORMAT persiste ces données, ou que le mécanisme est déjà cadencé par les valeurs décodées. Ces raccordements restent dans le groupe suivant.

## Propriétaire de la clé DSC

`EmulationDiskMetadataKeys.cs` est supprimé du SDK générique. La valeur `acorn.dsc` est définie une seule fois dans `MediaEngine/Images/Formats/HardDisk/Acorn/AcornWinchesterFormat.DescriptorMetadataKey`, désormais accessible au consommateur du format. L'adaptateur Acorn et ses tests emploient directement cette constante ; le projet Acorn référence MediaEngine à la compilation, sans ajout de relais dans Emulation et sans modifier les octets ou la clé des images enregistrées. Les tests ciblés passent après correction : **39 Debug, 39 Release**, zéro échec (880/726 ms de durée de suite).

## MODE SELECT et MODE SENSE — raccordement au moteur

Les commandes passent désormais par les phases et les broches de la cible. Les paramètres persistants sont chargés une fois par support ; RESET ou un remplacement de support invalide ce cache. Les paramètres sélectionnés en RAM restent distincts du format actif. MODE SELECT n'écrit aucune métadonnée et ne réinterprète pas les blocs déjà formatés ; leur persistance appartient à FORMAT, encore à réaliser. MODE SENSE restitue la liste sélectionnée, avec troncature à la longueur demandée et octets supplémentaires nuls. Un support sans paramètres de format valides refuse les accès nécessitant un format établi.

Le groupe compte **203 tests réussis en Debug et en Release**, zéro échec (108/97 ms de durée de suite). Les 23 nouveaux cas couvrent les deux unités indépendantes, RESET, le remplacement du support, les listes courtes/invalides, l'absence d'écriture de paramètres et les accès aux blocs de 256/512/1024 octets. Il s'agit d'échanges sur banc de cible, pas encore d'une preuve ADFS dans une machine BBC. La restitution des valeurs en attente n'est pas présentée comme une mesure du micrologiciel physique ; le format actif est conservé jusqu'au formatage.

## Géométrie choisie par FORMAT

`Acb4000FormatGeometry` applique le tableau A-1 du manuel OEM : 256 octets donnent 32 secteurs par piste en séquentiel, 33 avec interleave ; 512 donnent respectivement 17 et 18 ; 1024 donnent 9 dans les deux cas. L'interleave nul signifie deux, l'octet supérieur doit rester nul et la valeur effective doit être inférieure au nombre de secteurs par piste. Le calcul dépend des paramètres sélectionnés, sans modifier le conteneur ni supposer sa capacité précédente. La capacité maximale de cette géométrie est calculée en entier 64 bits.

**214 tests passent en Debug et en Release**, zéro échec (104/103 ms de durée de suite). Les 11 nouveaux cas vérifient les six lignes du tableau, toutes les valeurs des deux octets d'interleave, les paramètres invalides et les limites de capacité. Ce groupe prépare FORMAT ; ni l'écriture des champs physiques, ni les défauts de piste, ni la persistance du formatage ne sont encore démontrés.

## Options FORMAT et liste physique de défauts

Le décodage de la commande vérifie sa longueur, son opcode, le contrôle, les dépendances entre FMT DATA/CMPLT/options étendues, l'encodage cylindre-tête-octets et l'interleave. Le remplissage sans caractère explicite est `6C`, conformément à 6.3.1.4. Le LUN est extrait sans confondre ses trois bits avec les options ; sa disponibilité reste une décision du moteur de commandes.

La figure 6-6 de la page imprimée 6-7 a été examinée visuellement : deux octets réservés nuls, longueur big-endian des entrées sur deux octets, puis huit octets par défaut. Une entrée comporte un cylindre sur trois octets, une tête et une position depuis l'index sur quatre octets big-endian. `Acb4000FormatDefects` emprunte les données, sans copie ni allocation par défaut. Le décodage refuse une longueur tronquée, non multiple de huit, dépassant le tampon de 1 Kio, des coordonnées hors cylindres/têtes et une liste désordonnée. Les positions identiques sont acceptées sans imposer une règle d'unicité absente du manuel ; les valeurs de position sont conservées sur leurs 32 bits. Leur traduction en secteurs physiques et le rejet d'une position au-delà d'un tour restent à raccorder au modèle de piste, sans inventer une longueur de piste à partir de la seule taille utile des blocs.

**232 tests passent en Debug et Release**, zéro échec (103/105 ms de durée de suite), dont 18 nouveaux cas sur les options et les listes. Le plus grand bloc de défauts accepté par ces contraintes occupe 1 020 octets, dont 127 entrées. Ces tests valident le décodage préalable ; ils ne prouvent pas encore une commande FORMAT exécutée sur disque ni son interruption par RESET.

## Conteneurs DAT/DSC : tailles de blocs et dispositions conservées

MediaEngine lit maintenant la taille de bloc sur les trois octets de l'extent DSC. Une valeur nulle conserve la convention historique des émulateurs BBC : blocs de 256 octets, même si l'en-tête MODE SELECT est absent. Les valeurs explicites prises en charge sont 256, 512 et 1024 ; les cylindres et têtes restent ceux du DSC. L'écrivain renseigne la taille, l'extent et la géométrie en conservant les autres champs du contrôleur et la fin du descripteur.

DSC ne stocke pas l'interleave. Pour une paire complète, une longueur DAT correspondant exactement au nombre de cylindres/têtes et à la disposition séquentielle distingue les pistes de 32/17 secteurs des pistes usuelles de 33/18 secteurs. Les blocs de 1024 octets ont neuf secteurs dans les deux cas. Une image partielle conserve la disposition usuelle ; une image interleavée tronquée exactement à la capacité séquentielle est intrinsèquement ambiguë dans cette convention, sans information supplémentaire. Aucun champ privé n'a été inventé dans DSC pour masquer cette perte d'information. L'ordre physique et les défauts de piste ne sont pas représentés par le seul couple DAT/DSC.

La lecture refuse une fin de fichier à l'intérieur d'un bloc déclaré. Les changements de métadonnées du disque runtime doivent conserver sa capacité **et** sa taille de bloc ; une modification de format doit passer par la reconfiguration dédiée encore à réaliser. La préparation d'une image accepte les cinq géométries distinctes, mais le formatage BBC ADFS demeure réservé aux blocs de 256 octets : une structure ADFS de 256 octets ne doit pas être présentée comme un formatage logique de blocs 512/1024.

Résultats : **42 tests autonomes du groupe média** réussis en Debug (704 ms), puis **48 tests en Debug et Release** (566/753 ms), incluant six diagnostics temporaires sur de vrais fichiers. Les cinq dispositions DAT/DSC distinctes ont été écrites puis relues : octets identiques, taille logique et géométrie conservées, paramètres supplémentaires préservés. Un DSC historique à en-tête nul lit ses octets d'origine. Les fichiers tronqués au milieu d'un bloc 512/1024 sont refusés. Les fichiers temporaires possédés sont supprimés dans finally. Cette validation de conteneurs ne constitue pas encore une preuve de FORMAT exécuté par ADFS ni une capture native BBC.

Le fichier de diagnostic externe a ensuite été supprimé. Les assemblies de test Debug et Release ont été reconstruites avec succès, zéro erreur, pour retirer ce diagnostic des deux sorties ; les tests autonomes permanents restent présents.

## Préparation du support pour les écritures de formatage

`IScsiStorage.PrepareFormat` transmet les dimensions numériques au propriétaire Acorn, puis au contrat Emulation et à `IMediaRuntimeDisk.PrepareBlockFormat`. Chips ne connaît aucun conteneur ni type MediaEngine. La préparation ne remplit pas le disque et n'enregistre pas les paramètres de la carte. Elle conserve les octets retenus et les écritures déjà en attente avant de changer la taille des blocs ; une extension ajoute des plages explicitement indisponibles. Une réduction conserve uniquement les octets dans les nouvelles bornes. Les refus de format ou de protection interviennent avant mutation.

Les validations de ce groupe passent : **40 tests d'adaptation Acorn en Debug/Release** (811/754 ms), puis **48 tests média en Debug/Release** (622/577 ms), dont six diagnostics temporaires. Ceux-ci contrôlent les changements de taille/géométrie, la conservation des écritures précédentes, l'absence de publication à la préparation, les plages ajoutées indisponibles, le remplissage puis la publication/relecture et la réduction ultérieure. Les deux refus testés laissent l'image inchangée. Les fichiers possédés sont retirés dans finally. Le relais Acorn ne ferme pas le support emprunté.

Cette API prépare une représentation de conteneur ; elle ne simule pas les champs MFM physiques ni le passage du temps pendant FORMAT. Le séquencement de la cible, la réception et traduction des défauts, la persistance finale et le comportement lors d'une interruption restent dans les actions suivantes. Le contrôleur livré n'a pas encore été remplacé.

Les diagnostics externes de préparation ont été supprimés après leurs résultats. Les sorties de test Debug et Release ont été reconstruites avec succès, zéro erreur, sans ce diagnostic.

## FORMAT dans la cible : séquencement de base vérifié, clôture non acquise

Le moteur exécute maintenant FORMAT sans défauts et avec une liste vide : il attend la réception de l'en-tête et de la liste demandée avant de préparer le support. Sans format existant, MODE SELECT est requis. Les paramètres sélectionnés fixent la nouvelle géométrie, la taille de bloc et le remplissage ; le contrôleur écrit progressivement les blocs et ne transmet les paramètres à enregistrer qu'après le dernier. Les erreurs de préparation, d'écriture ou d'enregistrement terminent avec CHECK/Write Fault. Une préparation refusée n'écrit aucun bloc.

RESET arrête immédiatement la progression. Une trace du support dont le format a commencé reste dans la cible après RESET, afin que le rechargement de son ancien descripteur ne présente pas le format interrompu comme achevé. Ce suivi reste pour l'instant interne à cette instance de contrôleur : le couple DAT/DSC ne conserve pas encore l'état physique d'un format partiel à travers une destruction/recréation du contrôleur. Ce point doit être traité avant clôture, avec la représentation des champs et défauts plutôt qu'une fausse réussite.

Les listes de défauts non vides sont actuellement refusées, même lorsque leur syntaxe est valide : leur traduction et le saut des secteurs physiques ne sont pas encore réalisés. Elles ne sont pas ignorées silencieusement. La progression d'un bloc par appel du moteur est encore un séquencement provisoire ; elle n'est pas annoncée comme le débit MFM ou la durée mécanique de la carte.

**247 tests Chips passent en Debug/Release**, zéro échec (99/95 ms de durée de suite), dont 15 nouveaux cas. Ils couvrent les six couples taille/interleave, les remplissages `6C`/`E5`, le format existant ou absent, une liste vide reçue avant mutation, les options invalides, la protection, trois erreurs de backend, la persistance après le dernier bloc et l'arrêt par RESET. Le prochain travail conserve les défauts non vides, les timings, l'état du format partiel et la preuve dans les machines BBC comme exigences ouvertes ; cette suite ne clôture pas FORMAT ni l'ACB-4000.

## Sources supplémentaires : format physique du firmware original

Deux références temporaires de 8 192 octets ont été récupérées dans l'archive ACB-4000 de Bitsavers, sans installation comme ROM système BBC :

| Référence | MD5 vérifié | SHA-1 calculé |
|---|---|---|
| 400108-00 | `c48644076fb989d2123e4a16b76ae86c` | `58f5aaaeaa20bd5c93dcd641fd0d69f3c0f5f7a8` |
| 400510-01H | `21eed62a4af3229a5c3a8b60d2a0e505` | `2602a5457d9c1ada2c4c8b479905cba673716cc0` |

Le manuel constructeur [AIC-010](https://archive.org/details/aic-010_programmable_mass_storage_controller), récupéré en PDF, décrit les quatre plans de 24 octets du séquenceur. Dans 400108-00, la routine `1D7D` copie effectivement les 96 octets de `1F67` vers les plans `4080/40A0/40C0/40E0`. Les valeurs relevées ne sont donc pas empruntées à un autre émulateur. La routine `1498` initialise le premier gap à neuf octets ; `0C43–0C4F` choisit ensuite neuf ou dix-neuf octets selon l'interleave. La routine `0232` divise la position du défaut par taille de bloc + `36h` ou `40h` ; `022F` emploie un budget de `2842h` pour le nombre de slots. Ces divisions retrouvent le tableau A-1.

La routine `0DD4–0E51` refuse les défauts du premier cylindre/tête zéro, les positions atteignant `28B1h` et les coordonnées hors lecteur. `0CFE–0DA5` marque les slots défectueux, déduplique les défauts tombant sur un même slot, affecte les numéros logiques par interleave en sautant les slots occupés, puis ajoute le marqueur `40h` au dernier numéro logique. Les défauts tombant après le dernier slot utile sont consommés sans retirer de bloc. Ces observations précisent les validations de syntaxe précédentes ; le calcul uniforme du slot est celui du firmware, même si le premier gap et les suivants diffèrent en séquentiel.

La correspondance des routines est établie ici pour **400108-00**. Le second firmware reste une référence distincte ; sa présence et son empreinte ne prouvent pas que toutes ses routines sont identiques. La conservation des défauts et de l'état de format partiel dans une représentation de média reste à raccorder ; DAT/DSC seul ne contient pas cette carte physique.

Le PDF AIC-010 de référence a pour SHA-1 `d5461693e2c5114bb956fcea4c9126bddc4c8e1f`. Les trois références restent uniquement dans le cache ignoré, à supprimer à la clôture du composant.

## Construction de la carte physique vérifiée

`Acb4000TrackLayout` construit la carte d'une piste dans un tampon de pile de 33 octets : aucun objet ni tableau n'est alloué par secteur ou défaut. Le calcul reprend les diviseurs 310/320, 566/576 et 1078/1088 selon la taille et l'interleave. Les slots défectueux portent `80h`, les numéros logiques sont affectés en sautant ces slots, et le dernier numéro logique reçoit `40h`. Les défauts d'un même slot ne retirent qu'un bloc ; les défauts de la fin inutilisée ne retirent aucun bloc. Une piste entièrement défectueuse, un tampon trop court ou des paramètres incohérents échouent sans modifier la destination.

Le décodeur de défauts applique désormais les limites effectivement présentes dans 400108-00 : pas de défaut sur cylindre zéro/tête zéro, position strictement inférieure à 10 417 octets. Les anciens vecteurs acceptant des positions 32 bits arbitraires ont été corrigés après cette découverte. Le champ reste décodé sur 32 bits, sans tronquer une valeur hors limite en une valeur valide.

**259 tests Chips passent en Debug/Release**, zéro échec (101/128 ms de durée de suite). Les nouveaux cas vérifient la carte physique exacte à interleave deux, tous les interleaves permis pour les trois tailles, les six diviseurs, leurs frontières, les doublons, le bout de piste inutilisé, l'indépendance des pistes et les erreurs sans mutation. Le tampon de carte est prêt pour le raccordement ; le moteur FORMAT refuse encore les listes non vides tant que l'adaptation du média et la conservation de la carte ne sont pas réalisées. Aucune preuve invitée ni mesure de performance du contrôleur complet n'est déduite de ces résultats.

## Conteneur physique EMU : en-tête

L'utilisateur a demandé EMU en complément de DAT/DSC. Le codec suit le format publié dans [emu_tran_file.c du projet MFM de David Gesswein](https://github.com/dgesswein/mfm/blob/master/mfm/emu_tran_file.c). Il distingue les versions historiques, conserve les extensions d'en-tête et les chaînes brutes, valide les dimensions et la marque de fin, et calcule les positions de pistes en 64 bits. Les mots de piste restent des mots MFM bruts, avec le bit 31 en premier ; ils ne deviennent pas des secteurs DAT.

**31 tests autonomes passent en Debug et en Release**, zéro échec, durées de suite 169/160 ms. Ils couvrent un vecteur binaire explicite, les versions 1 et 2, les chaînes et leur terminaison, les extensions, la propriété du tampon, les valeurs invalides, les débordements, les positions au-delà de 2 Gio, les coordonnées et la marque de fin. Cette validation porte sur l'en-tête uniquement : le lecteur/écrivain complet, les accès aux pistes et leur raccordement à FORMAT restent à réaliser. Aucune prise en charge EMU dans la machine n'est annoncée à ce stade.

## Accès borné au conteneur EMU

`MfmEmuContainer` emprunte la source aléatoire commune et ne conserve aucun flux de fichier. L'ouverture vérifie la longueur attendue et la marque de fin. Une lecture vérifie l'adresse physique puis ne demande que la fenêtre de données voulue. La copie valide les adresses avant d'écrire, conserve exactement les en-têtes étendus et mots MFM, et utilise un tampon de 64 Kio rendu au pool en finally. Le flux de destination reste sous la responsabilité de l'appelant.

**42 tests EMU passent en Debug/Release**, zéro échec (214/210 ms), dont 11 cas du conteneur. Le premier passage Debug avait trouvé une adresse attendue incorrecte dans le test d'en-tête étendu : il oubliait quatre octets d'extension ; ce vecteur a été corrigé avant ces résultats. Les tests n'utilisent aucun fichier externe. Le lecteur enregistré, l'écrivain avec publication, la représentation physique et le raccordement du contrôleur restent ouverts ; la copie brute seule ne constitue pas une prise en charge complète dans RML.

## Lecteur et écrivain EMU enregistrés

Le registre par défaut contient désormais `MfmEmuReader` et `MfmEmuWriter`, avec l'identifiant `hdd.mfm-emu` et l'extension `.emu`. Le lecteur reconnaît la signature indépendamment de l'extension et produit une représentation physique à accès bornés. L'écrivain emploie la transaction multifichier commune pour publier un seul EMU, y compris lors de la réécriture de son propre fichier source. DAT/DSC reste enregistré séparément.

**45 tests EMU passent en Debug/Release**, zéro échec (438/432 ms), incluant trois scénarios temporaires sur fichiers : reconnaissance `.emu` et `.bin`, lecture des mots MFM, cycle exact, réécriture de la source, refus d'une adresse de piste incorrecte et annulation sans remplacer une destination existante. Les fichiers possédés sont supprimés en finally. Le diagnostic externe a ensuite été retiré ; les deux sorties de tests sont reconstruites après suppression. Aucun accès runtime, création vierge, décodage de secteurs ou affichage graphique EMU n'est déduit de ce seul cycle de conteneur.

## Pistes EMU modifiables

Les remplacements de pistes possèdent leurs octets et forment de nouveaux états immuables : les anciennes représentations et la source restent inchangées. La publication copie les plages d'origine et intercale les seules pistes remplacées, sans modifier leurs en-têtes, les autres pistes, les extensions ou la marque de fin. La mémoire de copie reste bornée à 64 Kio, en plus des seules pistes effectivement modifiées.

**44 tests permanents EMU passent en Debug/Release**, zéro échec (238/223 ms). Les trois diagnostics de fichiers précédents ont été retirés puis leurs assemblies reconstruites avec succès ; deux nouveaux tests autonomes couvrent les états successifs, la propriété des octets, les fenêtres modifiées, la copie exacte et les refus de dimensions invalides. Les écritures temporelles du contrôleur et sa lecture des champs restent à raccorder.

## Signal temporel des pistes EMU

`MfmEmuTrackSignal` lit les mots little-endian avec le bit 31 en premier. La cadence vient de l'en-tête ; le départ de capture est un décalage depuis l'index, et la période est fournie par le lecteur. Le remplissage au-delà de cette période reste conservé dans le fichier mais ne devient pas un tour supplémentaire. Une impulsion à l'index est représentée en fin de révolution selon le contrat neutre existant. Les temps de lecture sont projetés en nanosecondes par division entière, sans cumul d'erreur cellule par cellule.

Les fenêtres effacent seulement les cellules traversées et appliquent les impulsions effectivement émises. Un passage ultérieur de la porte efface une ancienne impulsion même sans remplacement. Une impulsion entre cellules est échantillonnée à la cellule suivante : c'est la résolution du signal échantillonné EMU, pas une réécriture du format de secteurs. La fréquence supérieure à la résolution nanoseconde du contrat est refusée lors de cette projection, sans empêcher la conservation brute du fichier. Une écriture au-delà des cellules capturées est refusée avant remplacement de l'état.

**58 tests permanents EMU passent en Debug/Release**, zéro échec (328/277 ms), dont 14 cas temporels : ordre des bits, phase de capture, exclusion du remplissage, lecture/réécriture exacte à 10, 8,6 et 15 MHz avec phase nulle ou décalée, fenêtres partielles, passage d'index, tours multiples, instant final, échantillonnage entre cellules, erreurs sans mutation et absence d'impulsions inventées. Ces tests ne démontrent pas encore le raccordement au contrôleur ACB-4000.

## Runtime physique EMU

`MediaRuntimeMfmDisk` réutilise `IMediaRuntimeDisk` et les relais d'Emulation existants. Il reçoit et restitue le signal neutre des pistes, conserve les changements jusqu'à publication, et borne le cache à une seule piste/période. Les nombres de secteurs et leur taille restent zéro tant que les champs ne sont pas décodés ; les accès sectoriels et métadonnées sans représentation sont refusés. Le contrôleur ne reçoit donc pas une géométrie logique inventée.

**61 tests EMU passent en Debug/Release**, zéro échec (540/602 ms), avec trois diagnostics temporaires ouvrant réellement le runtime : cache et période, accès physique, fenêtres écrites, publication/réouverture, protection, refus logique, annulation et verrouillage empêchant le remplacement du fichier sans perdre les changements. Une nouvelle tentative après retrait du verrou aboutit. La fermeture réinitialise seulement le lecteur possédé ; un lecteur emprunté garde son média. Les fichiers sont nettoyés en finally, le diagnostic retiré, et ses assemblies Debug/Release reconstruites avec succès.

L'ACB-4000 livré utilise encore l'ancien raccordement : ce runtime physique ne suffit pas à annoncer le HDD EMU opérationnel dans une machine BBC. Le décodage des champs, les paramètres sur piste, FORMAT/défauts, les délais mécaniques, la création et la visualisation restent ouverts dans la liste ordonnée.

## Création d'un EMU non formaté

`MfmEmuImageRepresentation.CreateUnformatted` reçoit explicitement cylindres, têtes, longueur de piste, cadence et départ de capture. Sa source virtuelle `MfmEmuBlankData` produit seulement les en-têtes et des cellules sans impulsions ; elle n'invente ni identifiants, ni secteurs, ni paramètres ACB. Une lecture en fin d'image de plus de 2 Gio fonctionne sans allouer le disque entier. Le calcul de longueur inclut désormais la marque de fin dans son contrôle de débordement.

`MfmEmuWriter.CreateAsync` emploie la transaction existante en interdisant le remplacement d'un fichier déjà présent, et réserve l'espace si la préallocation est demandée. **69 tests EMU passent en Debug/Release**, zéro échec (616/569 ms), incluant neuf cas autonomes de source virtuelle et deux créations réelles avec/sans préallocation, reconnaissance, ouverture runtime, écriture physique, publication/réouverture et refus d'écrasement. Le diagnostic externe et ses fichiers ont été supprimés après les mesures ; ses sorties Debug/Release ont été reconstruites avec succès.

Cette création est disponible dans le moteur ; son choix dans la fenêtre HDD du module et le formatage invité ACB-4000 restent à raccorder. La visualisation sectorielle et l'explorateur dépendent du décodage des champs physiques ; des pistes vierges ne sont pas assimilées à des secteurs déjà formatés.

## Descriptif des pistes pour la visualisation

Le fournisseur Flux existant décrit aussi les cylindres et têtes EMU, dans leur ordre physique, sans lire les cellules ni convertir le HDD en tableaux de bits. **68 tests permanents EMU passent en Debug/Release**, zéro échec (389/414 ms), dont le nouveau cas à deux cylindres et trois têtes. Ce descriptif ne constitue pas le rendu graphique et ne prouve pas la lecture de secteurs : ces parties dépendent encore du codec physique Adaptec et de leur raccordement à la présentation.

## Comparaison des champs Adaptec : documentation, firmware et MFM

Les sources publiques [wd_mfm_decoder.c](https://github.com/dgesswein/mfm/blob/master/mfm/wd_mfm_decoder.c) et [mfm_decoder.h](https://github.com/dgesswein/mfm/blob/master/mfm/inc/mfm_decoder.h) décrivent l'identité A1/FE, trois octets LBA et un octet de drapeaux, suivis de quatre octets de contrôle. Elles signalent plusieurs variantes de drapeaux ; leur présence dans une même famille ne prouve pas qu'elles correspondent au firmware 400108-00.

Le firmware original tranche plusieurs points : sa table à `1F67` programme A1/FE et A1/F8, et non la marque FB mentionnée dans un commentaire MFM. La routine `0BD1–0C35` place l'adresse sur trois octets, ajoute le bit 7 à l'adresse pour un slot défectueux et convertit le marqueur interne `40h` de dernier secteur logique en bit 7 de l'octet de drapeaux. Les sept bits inférieurs de cet octet reçoivent le compteur de défauts ; leur portée dans les zones du disque reste à raccorder au séquencement complet.

À `1DBE`, le firmware charge vers R74–R77 les quatre octets de `1FC7` : `C2 20 82 20`. Selon le câblage des rétroactions documenté par AIC-010, ils définissent le polynôme normal `41044185h`, également employé par le modèle MFM ACB-4000. Les octets suivants `82 20 82 21` définissent `43044105h` pour l'opération réciproque de correction. Le même motif normal est présent à `1FC7` dans 400510-01H, sans déduire de cette seule concordance l'identité des deux firmwares.

Le manuel OEM ACB-4000 annonce explicitement la correction d'une rafale de huit bits sur les champs ID et données. AIC-010 décrit le syndrome 32 bits et son décalage réciproque ; le modèle MFM cité configure six bits de correction. L'implémentation de la carte doit donc respecter les huit bits constructeur et ne pas adopter cette limitation du modèle secondaire. La remise à zéro de l'ECC et la vérification d'un résidu nul sont documentées et présentes dans le firmware ; les zéros de synchronisation précédant A1 ne changent pas un état initial nul.

Les trois sources récupérées restent dans le cache ignoré et seront retirées à la clôture du composant. Aucune incorporation de leur code ni preuve de piste réelle n'est déduite de cette comparaison.

## Contrôle et correction des champs ACB-4000

`Acb4000ErrorCheck` calcule le contrôle 32 bits au polynôme `41044185h`, état initial nul, puis écrit les quatre octets forts d'abord. Une table de 256 entrées est initialisée une seule fois. La correction parcourt les positions possibles par décalages inverses du syndrome, normalise les représentations équivalentes d'une même rafale, refuse une localisation ambiguë et confirme le résidu nul avant d'accepter la correction. Le tampon reçu reste identique après refus ; aucune piste n'est réécrite par cette opération de correction du tampon.

Les vecteurs de référence ont été calculés séparément bit par bit : `A1 FE 00 00 00 00` donne `99B7F53E`, `A1 FE 00 12 34 80` donne `CFFFCE8F`, et A1/F8 suivi de 256 octets 6C donne `5C89F9FA`. Ces résultats ne remplacent pas une lecture sur disque réel ; ils fixent le polynôme, l'ordre et l'état initial sans réutiliser la table de production comme résultat attendu.

**19 cas ECC passent en Debug** (452 ms), dont des boucles couvrant les 8 240 positions d'erreur simple du champ maximal, les 255 motifs de rafale non nuls pour chacun des blocs 256/512/1024, les frontières de synchronisation/données/contrôle, les champs ID et les refus sans mutation. Après initialisation, le calcul et la correction des champs testés n'allouent aucun octet sur le thread appelant.

Le groupe complet **ACB-4000 et hôte SCSI passe 278 tests en Debug/Release**, zéro échec (395/297 ms). Ce bloc valide le contrôle de champ isolé ; il n'est pas encore appelé par le lecteur physique du contrôleur. La sérialisation MFM, le raccordement aux champs ID/données, les commandes restantes, les timings complets et les preuves invitées restent ouverts.

## Champs ID et données de la carte

`Acb4000FieldCodec` écrit et lit les champs A1/FE, trois octets d'adresse, drapeaux et contrôle 32 bits, ainsi que les champs A1/F8 de 256/512/1024 octets. L'indicateur de slot défectueux reste distinct du LBA utile ; le bit de dernier secteur et les sept bits du compteur sont préservés. L'objet d'identité n'invente aucune adresse CHS à partir d'un LBA.

Les lectures travaillent sur un tampon local : une correction ECC n'écrit pas dans le champ reçu ni sur le média. Un champ de données à marque valide peut être transmis avec une intégrité fausse, ce qu'exige la fin CHECK/11 de la commande READ. Une marque ou longueur invalide laisse la destination intacte. La génération peut préfixer une charge utile sur son propre tampon chevauchant sans détruire ses premiers octets.

**293 tests ACB-4000/hôte SCSI passent en Debug/Release**, zéro échec (490/278 ms), dont 15 cas nouveaux : vecteur ID brut, adresse maximale et défauts, drapeaux, correction d'une marque, les trois tailles, vecteur de remplissage 6C, chevauchement, correction du tampon, données non corrigeables transférables et refus sans mutation. La détection et la sérialisation MFM ainsi que l'appel par le moteur de commandes restent à raccorder.

## Canal sériel MFM de la carte

`Acb4000MfmEncoding` conserve le dernier bit de données pour produire/vérifier les cellules horloge/données. A1 ordinaire donne `44A9h`, tandis que l'adresse utilise `4489h` à horloge manquante. `Acb4000ReadChannel` recherche ce motif sur toute phase, assemble les champs ID et données dans son tampon propre et conserve séparément l'état des horloges. Une nouvelle synchronisation abandonne un champ incomplet ; RESET retire les champs partiels et terminés. La configuration n'accepte que les trois tailles réellement prises en charge.

**305 tests ACB-4000/hôte SCSI passent en Debug/Release**, zéro échec (444/275 ms). Les douze nouveaux cas couvrent notamment les 256 octets et chacune des huit cellules d'horloge pour les deux états précédents, les trois tailles sur les seize phases, A1 ordinaire/synchronisation, les champs tronqués ou inconnus, les défauts d'horloge sans données inventées, un cycle de génération/canal/correction ECC, RESET et reconfiguration. Le canal ne connaît ni IDiskImage ni EMU, ni fichier ; les cellules sont fournies par son propriétaire.

Le canal et les codecs restent des éléments du raccordement en cours : le moteur de commandes utilise encore son ancienne adaptation d'accès à octets. Aucune lecture physique ACB-4000 dans une machine BBC ni aucun chronométrage complet n'est annoncé à partir de ces seuls tests.


## Raccordement READ/VERIFY au canal MFM

Le moteur ACB-4000 expose `ReadGate` et `ClockReadCell` : READ(6/10), VERIFY et la phase de vérification de WRITE AND VERIFY reçoivent maintenant les cellules séparées du canal physique. Ces opérations n'appellent plus `IScsiStorage.Read`. Les commandes d'écriture/FORMAT et le chargement initial des paramètres utilisent encore le contrat transitoire ; le module natif conserve pour l'instant l'ancien contrôleur livré.

Une donnée doit suivre immédiatement un ID de contrôle valide, non défectueux et correspondant au LBA demandé. Le compteur de synchronisations inclut les champs inconnus ou tronqués : un ancien ID ne peut donc pas être réutilisé après un champ intermédiaire abandonné. READ relit jusqu'à six champs de données avant de tenter une correction, puis transfère le bloc avant CHECK/11 en cas d'échec. VERIFY ne corrige ni ne relit une erreur ECC ; le code constructeur est CHECK/19, corrigé dans le banc précédent qui attendait à tort 11.

**317 tests ACB-4000/hôte SCSI passent en Debug/Release**, zéro échec, 390/237 ms. Les douze nouveaux cas utilisent les entrées de cellules du contrôleur : trois tailles, données différentes du stockage logique, attente effective sans cellules, READ court/étendu, correction seulement après six lectures, bloc non corrigeable transféré, VERIFY sans correction, IDs étrangers/défectueux/invalides/tronqués et RESET d'un champ partiel. La méthode de lecture logique du support de banc lève une exception pour déceler tout contournement. Aucun fichier externe, fenêtre ni périphérique audio n'est utilisé.

Ce générateur de banc sérialise les champs et ne représente pas la vitesse d'une broche ST-506. La recherche limitée par INDEX, les erreurs ID/absence de marque, la mécanique, les sorties d'écriture, le relais des pistes EMU et la préparation physique complète restent ouverts. Une entrée sans champs acceptables attend encore les signaux ; aucun timeout matériel non documenté n'a été inventé. Ces tests ne constituent ni un essai natif BBC ni une preuve de clôture du composant.


## Politiques constructeur des erreurs de lecture

SEND DIAGNOSTIC/65 accepte sa liste de quatre octets et conserve son choix séparément pour chacune des deux unités jusqu'à RESET. Le qualificateur et le compteur de patch ne servent pas à cette option ; une longueur supérieure ne provoque pas la demande d'octets supplémentaires. Le réglage de RAM du contrôleur ne dépend pas d'un disque prêt. Les réservés CDB, listes trop courtes et modes hors 0/1/2 sont refusés sans remplacer le choix antérieur.

Le mode 0 conserve la correction silencieuse après six lectures, puis données suivies de CHECK/11 si le défaut n'est pas corrigeable. Le mode 1 arrête READ au premier défaut ECC, sans relecture ni correction, avec CHECK/18 (sense 98 lorsque l'adresse est valide). Le mode 2 signale une correction effectivement effectuée : il transmet le bloc corrigé puis s'arrête avec CHECK/18. Une relecture devenue bonne ne constitue pas une correction à signaler ; une erreur non corrigeable reste CHECK/11. VERIFY conserve son contrôle sans correction et CHECK/19, indépendamment de ces politiques.

**345 tests du groupe ACB-4000/hôte SCSI réussissent en Debug/Release**, zéro échec, 415/238 ms ; 28 nouveaux cas couvrent ces politiques et les phases de bus avec des cellules réellement corrompues. Les autres options SEND/RECEIVE DIAGNOSTIC, le patch matériel/RAM et les temporisations complètes de recherche restent à réaliser.

La relecture des références distingue aussi le rôle d'INDEX de celui des boucles de recherche : AIC-010 pages 18–19 décrit une recherche ID puis DATA avec délai contrôlé par son propriétaire ; le firmware 400108-00 emploie notamment les compteurs en 15D1–172C. Il serait incorrect de remplacer ces boucles par un nombre de révolutions non établi. Leur chronométrage reste ouvert.


## Sortie sérielle FORMAT

`Acb4000FormatChannel` émet une cellule par appel d'horloge et attend INDEX avant d'activer sa porte d'écriture. Le séquenceur n'accède ni à un conteneur ni aux secteurs d'une image. Il copie au début la carte de slots que lui fournit le contrôleur, prépare le champ de remplissage et son ECC dans ses tampons bornés, puis émet les identités et données par MFM. Aucun tableau de disque entier et aucune allocation par cellule ne sont nécessaires.

Les intervalles proviennent de la table du firmware 400108-00 à 1F67 et de la définition des compteurs AIC-010 pages 21–24 : en mode normal, une valeur N émet N+1 octets ; en mode ECC, 43h émet quatre octets de contrôle, pas quatre octets plus trois zéros. Les états sont donc : gap initial neuf 4E (état 10, compteur 08h), douze zéros avant ID (0E/0Bh), A1/FE + LBA/drapeaux + quatre ECC, quinze zéros avant DATA (11/05h puis 09/08h), A1/F8 + bloc + quatre ECC et deux zéros après DATA (12 ou 13/01h). La routine 0C43–0C4F change les gaps suivants en dix-neuf octets si l'interleave vaut un. Le premier gap reste neuf octets.

Cela retrouve indépendamment les pas du firmware : taille + 54 octets en interleavé et taille + 64 en séquentiel, avec dix octets de moins pour le premier secteur séquentiel. Le dernier état 16 émet 4E jusqu'à INDEX. Un index reçu pendant un champ n'est pas transformé en arrêt fictif : la condition d'index ne termine le séquenceur qu'une fois ce dernier état atteint. RESET retire immédiatement la porte et l'état partiel.

Les slots défectueux portent le bit d'adresse 800000h ; leur champ d'adresse utile correspond à la base de piste plus la valeur basse de la carte (zéro pour 80h), conformément à 0BD1–0C35. Le compteur de défauts est incrémenté avant leur ID et conservé dans les suivants ; le drapeau du dernier numéro logique reste distinct de la position physique finale.

**359 tests ACB-4000/hôte SCSI réussissent en Debug/Release**, zéro échec, 436/274 ms ; les quatorze nouveaux cas vérifient le vecteur complet du premier secteur avec ECC indépendants 99B7F53E et 5C89F9FA, les six dispositions taille/gap, interleave et dernier secteur, défauts et possession de la carte, attente/arrêt INDEX, interruptions RESET dans chaque portion, refus sans modification de la sortie et absence d'allocations après construction. Le canal de lecture réinterprète aussi les pistes produites avec ID et DATA valides.

Ce canal de sortie reste à raccorder au moteur de commande FORMAT et au propriétaire des pistes ; il ne remplace pas encore le chemin de formatage logique transitoire. La publication des fenêtres dans EMU, la persistance du format interrompu, les paramètres physiques de démarrage, le chronométrage complet et les preuves natives BBC restent nécessaires avant la clôture.


## Raccordement du moteur FORMAT à la sortie physique — validation finale en attente

La commande FORMAT appelle maintenant le canal sériel, avec les sorties `WriteGate` et `ClockWriteCell`, les coordonnées de piste sélectionnées et l'entrée `SetWriteFault`. Le remplissage ne passe plus par `IScsiStorage.Write`. Les paramètres ne sont enregistrés qu'après l'index terminal de la dernière piste. Les cartes des pistes défectueuses sont validées avant de préparer le support ; les défauts dédoublonnés réduisent le nombre de blocs logiques et sont émis avec leurs identités/drapeaux sur la piste, plutôt qu'ignorés. Le tampon de réception conserve la liste pendant l'opération sans tableau par secteur.

RESET, remplacement du support ou WRITE FAULT ferment la porte ; le niveau WRITE FAULT appartient au lecteur et n'est donc pas effacé par RESET du contrôleur. Les sources du banc capturent et décodent les cellules émises et injectent ce niveau en cas d'échec d'écriture du support. Les cas de défauts valides/dédoublonnés, piste entièrement défectueuse, attente INDEX et arrêt partiel ont été ajoutés. **Ces nouveaux raccordements et cas ne sont pas encore exécutés** : ils sont réservés au passage regroupé de correction/optimisation demandé. Les 359 réussites citées ci-dessus précèdent ce raccordement.

Le contrat transitoire reste utilisé pour la préparation de géométrie et l'enregistrement du descripteur ; le relais des fenêtres physiques vers EMU et le chargement de paramètres physiques au redémarrage restent à terminer. La capacité avec défauts ne doit pas être annoncée persistante dans DAT/DSC, qui ne conserve pas les champs de piste.


## Paramètres conservés dans les identités physiques — réalisation en cours

Le firmware 400108-00, 0B68–0BAF, prépare neuf octets à sept bits. La routine 0E55 partage un mot en sept bits bas et cinq bits hauts, accompagnés de deux bits auxiliaires. Les neuf premiers ID physiques de C0/H0 reçoivent ces octets en 0BF0–0C35 ; le bit 80 du dernier numéro logique reste indépendant. Les autres pistes et les ID suivants retrouvent le compteur ordinaire de défauts. Ce ne sont pas des secteurs de données réservés ni un fichier de métadonnées inventé.

`Acb4000PhysicalParameters` encode maintenant la taille, le code de pas, le dernier cylindre (nombre de cylindres moins un), les quatre bits du dernier numéro de tête, le courant réduit et la zone de parking. La quatrième paire reproduit précisément 0B98–0BA5 : HL transformé par la troisième paire est réutilisé, puis son bit 6 haut reçoit le signe de parking. Le firmware ne recharge pas 80AC à cet endroit ; cette particularité n'est pas remplacée par une valeur supposée. Les octets de précompensation, ignorés par ce contrôleur, ne sont pas ajoutés à cet enregistrement.

Le moteur FORMAT prépare ces neuf drapeaux sur la pile uniquement pour C0/H0 ; son canal en prend une copie bornée avant l'horloge d'écriture. Aucun accès à un conteneur ni allocation par cellule n'est ajouté. Le chargement inverse au redémarrage reste à raccorder. Les nouveaux cas attendent la validation regroupée ; ils ne sont pas inclus dans les anciens résultats de 359 tests.


## Projection des pistes et relais du module — non activés dans les machines

Le codec Adaptec de MediaEngine construit et décode les champs A1/FE et A1/F8, leurs identités et ECC, en réutilisant les primitives de flux. Il ne corrige pas les erreurs de lecture à la place du contrôleur. Le runtime DAT/DSC projette une piste vers les blocs seulement lorsque tous ses champs sont représentables ; une piste autrement modifiée reste en mémoire et sa publication destructive est refusée. La reconnaissance des neuf drapeaux de paramètres reste à intégrer à cette projection : un drapeau de paramètre ne doit pas être confondu avec un compteur de défauts.

`WinchesterTrackSignals`, dans le module Acorn, emprunte les images et relaie les cellules à 10 MHz, les fenêtres d'écriture et la rotation rationnelle nominale. Le cache est borné à une piste par lecteur. Le constructeur donne 3600 tr/min, 16,67 ms par révolution et une impulsion INDEX typique de 200 µs dans le [manuel OEM Seagate ST-506/ST-412, avril 1982, section 4.2.4](https://minuszerodegrees.net/manuals/Seagate/Seagate%20ST506%20ST412%20-%20OEM%20Manual.pdf). L'entrée/sortie du relais conserve ces temps émulés ; leur coût hôte reste à mesurer après le raccordement.

La compilation Debug de Chips, MediaEngine, Emulation et Acorn a réussi avant l'ajout des drapeaux de paramètres, sans erreur ni avertissement. Ce résultat ne valide pas les nouveaux signaux en exécution. L'ancien contrôleur livré reste actif : le chargement physique des paramètres, les commandes restantes et le séquencement mécanique doivent être terminés avant son remplacement. Les tests, la preuve native et le build livré restent à faire ; aucune clôture du composant n'est annoncée.


Le décodeur inverse des neuf drapeaux est écrit dans `Acb4000PhysicalParameters.TryRead` d'après 0379–03CC : restauration de la taille, du code de pas, des cylindres/têtes et du parking signé. Les valeurs absentes, les octets hors sept bits et les paramètres hors bornes restent refusés sans remplacer l'état antérieur. La précompensation n'étant pas enregistrée par ce firmware, le décodeur conserve sa valeur précédente. Les vecteurs inverses et cas invalides sont ajoutés aux sources de tests ; exécution finale encore en attente. Ce décodeur n'est pas encore appelé par la séquence native de démarrage.


## Lecture logique EMU — code ajouté, validation groupée en attente

`AdaptecMfmGeometry` reconnaît la géométrie des neuf drapeaux initiaux uniquement lorsque la piste zéro possède des identités valides, uniques, non défectueuses et un marqueur de dernier bloc cohérent. L'ordre physique des paramètres est conservé, y compris si la capture commence au milieu de la révolution. Une piste vide ou incomplète ne reçoit pas de géométrie supposée.

Le runtime EMU expose maintenant la taille et les secteurs de piste détectés. Sa capacité logique vient du dernier ID valide marqué comme dernier bloc sur la dernière piste enregistrée ; si cette information manque, il conserve zéro plutôt qu'une capacité inventée. Les lectures recherchent l'adresse logique réellement enregistrée, avec prise en compte des slots physiques défectueux qui décalent les LBA vers les pistes suivantes. Les états d'intégrité ID/DATA restent ceux du flux reçu. Un cache d'une seule piste décodée est invalidé après toute écriture physique.

Cette interprétation sectorielle Adaptec utilise sa révolution nominale de 3600 tr/min ; le relais matériel conserve la période fournie par le lecteur. Les captures EMU d'autres codages restent accessibles physiquement sans être annoncées comme des secteurs Adaptec. L'écriture sectorielle, les projections pour l'explorateur/visualiseur et le chargement natif du contrôleur restent à terminer. Les nouveaux tests autonomes vérifient les paramètres maximaux, l'ordre physique/rotation de capture, les pistes incomplètes, les doublons et les identités invalides ; ils ne sont pas encore exécutés.


### Compilation groupée du raccordement en cours

Le 4 novembre 2026, les projets `RetroMediaLab.Chips.Tests` et `RetroMediaLab.Tests` compilent en Debug/x64 avec `--no-restore`, respectivement en 6,76 s et 41,34 s, zéro erreur. Cette compilation couvre les nouveaux paramètres physiques, leur canal FORMAT, la géométrie et le runtime sectoriel EMU, ainsi que les sources de leurs tests. Deux avertissements sont signalés dans d'autres fichiers : xUnit2031 dans `Shared/ArchitectureTests.cs` et CS0649 dans `Application/TestInfrastructure/SyntheticData.cs`. Aucun test n'a été exécuté dans ce passage ; ces compilations ne constituent ni un résultat fonctionnel ni le build Debug livré avec tous les modules.


## Écriture sectorielle EMU — raccordement écrit, essais finaux en attente

`CreateSectorWrite` retrouve le champ DATA associé à un unique ID valide du LBA demandé, construit la nouvelle charge utile et son ECC, puis fournit une fenêtre de cellules au runtime. Les ID, leur ordre, les autres secteurs et les transitions non traversées restent empruntés et inchangés. Le premier bit horloge après DATA est recalculé à partir du nouveau dernier bit et du bit de données suivant ; un A1 à horloge manquante immédiatement suivant est préservé.

`MediaRuntimeMfmDisk.WriteSector` utilise cette fenêtre physique et le mécanisme de publication EMU existant. Un champ DATA corrompu mais localisable peut être remplacé ; une identité ambiguë, un champ DATA absent ou une taille incorrecte sont refusés avant modification de l'image. Les sources de tests vérifient la réparation et l'ECC, la conservation des ID/secteurs voisins et de toutes les impulsions hors fenêtre, ainsi que la dépendance du bit horloge suivant lorsque le dernier bit change. Ces nouveaux cas ne sont pas encore exécutés.


## Relais vers MediaFileSystems — code ajouté

La lecture logique n'est plus dupliquée dans le runtime : `AdaptecMfmSectorReader` conserve la géométrie et un seul décodage de piste. La représentation EMU expose maintenant le contrat existant `IMediaBlockRepresentation` en complément de ses pistes physiques. Les détecteurs de volumes et lecteurs de fichiers peuvent donc demander des octets logiques par leur chemin habituel, sans déplacer la reconnaissance du système de fichiers dans Chips ou recréer un catalogue. Le conteneur et sa représentation de stockage restent EMU/flux.

Le lecteur partagé prend en charge les lectures partielles et entre deux secteurs, avec annulation et contrôle des bornes. Les accès au cache partagé sont sérialisés ; les lecteurs de fichiers refusent les blocs dont l'intégrité est invalide, alors que l'accès sectoriel brut restitue les données et leur état. Une image non reconnue ou non formatée expose zéro octet logique, pour que le détecteur ne prenne pas la longueur physique du fichier EMU pour celle d'un volume.

La géométrie ne reconnaît que 32/33 secteurs de 256 octets, 17/18 de 512 ou neuf de 1024. Les cas autonomes ajoutés vérifient l'ordre LBA malgré l'interleave, les lectures entre secteurs, les erreurs ECC et les bornes/annulation. Ils attendent le passage final. Ce raccordement de contrat n'est pas encore une preuve du rendu de l'explorateur ni de la visualisation graphique native.


## Vue graphique des blocs EMU — raccordement écrit

Le contrôleur de visualisation demande désormais une vue logique au document EMU reconnu, puis utilise le graphique HDD de blocs existant. La projection `CreateLogicalView` conserve le format EMU, la source, les volumes, diagnostics et métadonnées ; elle emprunte le lecteur Adaptec et ne copie pas tout le disque ni ne modifie le conteneur. Une géométrie physique n'est fournie au graphique que si sa capacité nominale correspond à la capacité logique réellement enregistrée. En présence de défauts réduisant cette dernière, la vue reste une carte LBA sans fausse correspondance CHS.

Une image vierge ne reçoit pas cette projection de blocs stockés. La représentation flux originale reste disponible indépendamment de la vue logique. Les tests sans fichier ajoutés couvrent la projection, ses lectures entre secteurs, la conservation des volumes/source et le refus d'un autre document ; ils attendent le passage final. Le raccordement graphique n'est pas encore une preuve visuelle native, et le rendu du flux brut d'une image non formatée reste à compléter.


### Compilation du bloc écriture/lecture/projection graphique

La compilation Debug/x64 de `RetroMediaLab.Tests` a d'abord signalé deux erreurs dans les nouveaux tests : ambiguïté de `MediaVolumeOrigins` et conversion `ImmutableArray<bool>` vers `bool[]`. Elles ont été corrigées ensemble dans leurs fichiers. Le second passage réussit en 3,99 s, zéro erreur ; le seul avertissement CS0649 concerne `SyntheticData.cs`, hors de ce raccordement. MediaEngine, Emulation et App compilent avec le lecteur partagé et la projection graphique. Aucun test n'a été exécuté dans ce passage et aucun build de livraison de tous les modules n'est annoncé.


## Création EMU par le service média — réalisation en cours

`CreateHardDiskAsync` reconnaît maintenant le format EMU existant. Sans système de fichiers demandé, il prépare un conteneur dont les pistes ne contiennent aucune transition ni champ de secteur. Avec ADFS, il demande le préfixe du volume au formateur de MediaFileSystems, puis génère les champs physiques Adaptec, leurs ECC et les neuf paramètres ID de la première piste. La publication utilise le writer atomique EMU, sans écraser une destination existante et avec préallocation optionnelle.

`MfmEmuFormattedData` calcule les pistes à la demande pendant la copie. Il conserve un seul cache de piste et les quelques secteurs système ADFS, plutôt qu'un HDD entier en RAM. Les cellules du conteneur sont dimensionnées à la cadence nominale Adaptec avec alignement des mots EMU. Les paramètres de commande non définis par la géométrie initiale restent nuls, comme dans un DSC minimal ; ce n'est pas une nouvelle variante de lecteur matériel.

Les tests autonomes ajoutés portent sur la géométrie relue et le préfixe ADFS, le dernier secteur de la deuxième tête, les lectures partielles du conteneur et l'annulation/les bornes. Ils ne créent aucun fichier. Leur exécution, la création de fichiers réels, les profils du module et les preuves natives restent à réaliser.


Les profils de création du module Acorn comprennent maintenant les variantes EMU des mécanismes Winchester 110, 130 et de la géométrie configurable, à côté de DAT/DSC. Les noms de mécanismes et identifiants sont définis une fois dans les constantes. Le choix existant Vierge/ADFS est transmis au créateur média ; aucune nouvelle traduction ni commande de formatage invitée n'est inventée pour ce choix. L'extension EMU est ajoutée à la liste de stockage, mais l'ancien contrôleur livré ne devient pas compatible pour autant : son remplacement reste ouvert.

Le bloc création et les nouveaux cas médias compilent en Debug/x64 : `RetroMediaLab.Tests` réussit en 24,85 s, zéro erreur (avertissement CS0649 dans SyntheticData hors de ce bloc) ; `RetroMediaLab.Emulation.Acorn` réussit en 2,75 s, zéro erreur ni avertissement. Aucun test n'a été exécuté dans ces passages. La création réelle de fichiers, le formatage invité, les captures et la mesure des performances attendent la validation finale du composant.


## État du lecteur et commandes mécaniques

La section OEM 6.3.1.1 impose aussi CHECK/03 dans TEST UNIT READY quand WRITE FAULT est actif. Le moteur ignorait cette entrée pour cette commande : il la consulte maintenant. La protection en écriture du support n'est pas assimilée à un défaut matériel. Les sources de tests couvrent le niveau maintenu après RESET, sa libération et le support en lecture seule ; leur exécution reste dans la validation finale.

La lecture groupée des sections 6.3.1.2, 6.3.1.7 et 6.3.1.13 confirme les exigences mécaniques restant ouvertes : REZERO attend l'arrivée à zéro ; SEEK rend le bus après la série de pas, tandis qu'une nouvelle commande visant l'unité encore en déplacement doit renvoyer BUSY ; STOP place les têtes dans la zone de parking. Ces comportements ne sont pas remplacés par un simple succès immédiat ou par l'arrêt supposé de la rotation. Les signaux READY/SEEK COMPLETE/TRACK ZERO et les impulsions restent à raccorder avant activation native.


La lecture ciblée du firmware 400108-00 précise la calibration, au-delà du résumé OEM : 0A7B–0ADC vérifie TRACK ZERO, s'en éloigne de seize pas lorsqu'il est déjà actif, vérifie sa désactivation, puis effectue une recherche vers zéro bornée par 0800h pas. Un signal restant incohérent aboutit à 06h ; ce comportement interdit de remplacer REZERO par un simple `Seek(0)` réussi. La routine 0878–08FD produit les pas sur le bit 40h du port 82h, avec une branche distincte pour le code rapide et une boucle supplémentaire pour le code non bufferisé. 09EF–09F7 distingue ensuite la fin immédiate de SEEK de la continuation des commandes qui doivent attendre. Ces séquences sont identifiées pour le raccordement ; elles ne sont pas encore implémentées par le moteur de commande.


## Séquenceur de positionnement — réalisation séparée du raccordement des commandes

`Acb4000Positioning` est maintenant écrit. Il représente les fronts numériques STEP et leur direction, avec les trois cadences nominales documentées (3 ms, 28 µs, 12 µs). Le temps est accumulé en fraction entière de l'horloge fournie ; l'arrondi ne s'ajoute pas à chaque pas. Il ne crée aucun média et n'alloue pas pendant les ticks. Un front est distingué de la largeur électrique de l'impulsion, qui n'est pas déclarée reproduite par cette abstraction.

Le SEEK termine son séquenceur après le dernier front ; l'attente mécanique du lecteur reste une entrée distincte. La calibration vérifie READY et SEEK COMPLETE, s'écarte de seize pas d'un zéro initialement actif, contrôle sa désactivation, puis cherche zéro en au plus 2048 pas avec contrôle du signal après chaque déplacement. Les signaux incohérents donnent 06h. Le propriétaire peut signaler l'expiration de SEEK COMPLETE (02h) : aucune durée de panne non établie n'a été inventée dans ce séquenceur. RESET retire l'état en cours sans déplacer fictivement un lecteur.

Les cas autonomes ajoutés couvrent les trois cadences, l'absence de dérive sur une horloge non multiple, le SEEK et son dernier front, les calibrations avec/sans zéro initial, les signaux bloqués, les attentes et leur expiration, READY et RESET. Ils attendent l'exécution groupée. Le moteur de commandes, les signaux du module et les délais physiques du lecteur ne sont pas encore raccordés : ce séquenceur n'est pas une validation de REZERO/SEEK dans la machine.


## Commandes REZERO/SEEK et entrées de lecteur — raccordement écrit

Le moteur ACB-4000 reçoit désormais READY, SEEK COMPLETE et TRACK ZERO séparément pour chacun des deux ports et expose leurs fronts STEP/directions. Ces entrées ne sont pas déduites de la seule présence d'une image. Un séquenceur et un compteur de position sont conservés par port ; ils avancent même lorsque le bus SCSI est libre. REZERO et SEEK utilisent maintenant l'état de positionnement, avec leurs contrôles d'arguments et d'adresse. La synchronisation du backend intervient après la séquence réussie, au lieu de remplacer la calibration.

SEEK rend son statut après le train de pas ; l'unité reste occupée jusqu'à réception de SEEK COMPLETE. Le contrôleur renvoie le statut BUSY pour une nouvelle commande visant ce port, tandis que l'autre reste accessible. REQUEST SENSE conserve son traitement propre. RESET arrête les commandes et fronts en attente sans ramener les têtes à zéro ni réinitialiser les entrées physiques. L'expiration de l'attente du séquenceur reste fournie explicitement par le propriétaire ; sa durée n'est pas inventée dans ce raccordement.

Le banc de commandes a reçu des lecteurs numériques qui réagissent aux fronts, maintiennent un délai de déplacement et publient les signaux. Les nouveaux cas portent sur la libération du bus, BUSY sélectif, calibration effective et signal zéro bloqué, READY, RESET et expiration externe. Les sources sont écrites, leur exécution reste groupée en fin de composant. Les recherches implicites READ/WRITE/FORMAT, les délais et signaux du mécanisme natif, START/STOP et l'activation du nouveau contrôleur restent ouverts.


Le raccordement REZERO/SEEK et les nouveaux cas compilent en Debug/x64 : `RetroMediaLab.Chips.Tests` réussit en 6,51 s, zéro erreur (avertissement xUnit2031 dans ArchitectureTests hors de ce bloc) ; `RetroMediaLab.Emulation.Acorn` réussit en 9,03 s, zéro erreur ni avertissement. Aucun de ces passages n'exécute les tests. La présence de l'interface compilée ne prouve pas que le mécanisme natif alimente déjà ses nouveaux signaux.


## Recherches implicites avant transfert — raccordement écrit

READ et VERIFY, les blocs WRITE reçus et chaque piste de FORMAT utilisent maintenant le même séquenceur que SEEK. Leur état conserve le tampon ou la carte pendant le déplacement et n'ouvre le canal qu'après SEEK COMPLETE. Le SEEK explicite conserve sa fin après le train de pas ; cette exception n'est pas appliquée aux transferts. Le compteur de cylindre évite de produire des fronts inutiles entre deux secteurs du même cylindre.

Le support emprunté est identifié pendant le déplacement : remplacement, perte READY, WRITE FAULT pour une écriture, expiration explicite ou RESET empêchent la reprise du transfert en attente. La porte de lecture/formatage tombe avec READY. Les sources des nouveaux tests couvrent attente de lecture/VERIFY/WRITE, passage FORMAT au cylindre suivant, absence de pas superflus et interruptions/remplacement.

Ces sources attendent le passage final. L'écriture WRITE ordinaire utilise encore son backend à octets après le positionnement : elle doit encore rejoindre le canal sériel physique, contrairement à FORMAT déjà sériel. Les signaux et temps du lecteur natif, le démarrage physique et les commandes restantes restent nécessaires avant activation.


Le déplacement implicite et ses sources de tests compilent en Debug/x64 avec `RetroMediaLab.Chips.Tests` en 2,35 s, zéro erreur ; seul l'avertissement xUnit2031 d'ArchitectureTests est signalé. Les suites ne sont pas exécutées dans ce passage. Les preuves natives et la validation finale de toutes les commandes restent ouvertes.

## Essai invité du nouveau montage BBC B — 4 novembre 2026

Le service BBC utilise désormais le nouvel hôte SCSI, l'ACB-4000 et le relais ST-412. Le banc invité temporaire exécute le vrai CPU BBC avec 32 Kio, sans substitution des appels MOS, sur une image neuve indépendante de la configuration utilisateur. Géométrie Winchester 110 : 306 cylindres, quatre têtes, 33 secteurs de 256 octets. Les ROM existantes du dossier Firmware sont lues sans modification :

| ROM | SHA-1 |
|---|---|
| `os12.rom` | `0d9bcaf6a393c9ce2359ed700ddb53c232c2c45d` |
| `basic2.rom` | `4a7393f3a45ea309f744441c16723e2ef447a281` |
| `Acorn ADFS v1.30 [variant 1].rom` | `301fd05c475a629c4bec70510d4507256a5b00d8` |

Parcours réalisé par le clavier émulé : `*ADFS`, `*CAT`, `*SAVE CHIPTEST 3000 +100`, `*LOAD CHIPTEST 3100`, puis catalogue. Le banc initialise les 256 octets à enregistrer avec le motif index XOR 5Ah ; il vérifie chaque octet de la zone chargée et exige des compteurs de lecture et d'écriture physiques non nuls. Il détache ensuite le média, publie les écritures, ferme l'image et réinitialise la machine. Une nouvelle machine ouvre le fichier sauvegardé en lecture seule, exécute `*LOAD CHIPTEST 3200` et `*CAT` : le même motif est vérifié, avec zéro bloc écrit pendant cette seconde session.

**Release : les deux cas passent**, EMU en 17 s et DAT/DSC en 12 s (série complète 30,51 s). Ce sont des durées de scénarios incluant saisie, création et fermeture ; elles ne mesurent pas le débit CPU seul. Le premier essai DAT avait seulement validé le démarrage (21 blocs lus), puis échoué parce que le banc présentait les lettres comme une touche F1 accompagnée d'un caractère : les lettres doivent utiliser leur identité de touche dans le contrat BBC. Après correction du banc, SAVE/LOAD réussissent ; aucune correction du contrôleur n'a été nécessaire pour ce parcours.

Captures réellement produites depuis la sortie vidéo de la machine et inspectées :

- `artifacts/TestsChips/Acb4000/BbcMicroModelB/Release/adfs-native-dat-reopened.png`
- `artifacts/TestsChips/Acb4000/BbcMicroModelB/Release/adfs-native-emu-reopened.png`

Elles montrent le catalogue ADFS contenant CHIPTEST après réouverture. Les images sont hors Git. Ce passage prouve un accès invité en lecture/écriture et sa persistance sur les deux conteneurs, sur BBC B/port zéro ; il ne prouve pas encore les autres modèles, le deuxième port depuis ADFS, les interruptions d'écriture avec réouverture, la visualisation/exploration dans App, les diagnostics internes ni les échéances de panne. Il ne constitue pas un build complet livré à l'utilisateur.

Premier passage Debug du scénario complet : **deux dépassements de la limite de 90 secondes**, série de 3,31 minutes avec création/publication/nettoyage. Sur EMU, les étapes boot, SAVE (huit blocs écrits) et LOAD réussissent, puis le catalogue affiche CHIPTEST ; après réouverture, la limite est atteinte pendant la dernière saisie. Sur DAT, même étape de réouverture atteinte. Ce passage ne valide donc pas la totalité du scénario en Debug et ne constitue pas une régression de données établie. Le relais appelle deux résolutions complètes avant/après chaque tick alors que les sorties hôte sont déjà propagées aux accès et à la fin du tick précédent. Optimisation de ces publications répétées prévue sans supprimer de tick matériel.

### Optimisation de la propagation du bus et second passage Debug

`WinchesterController.TickHardware` ne résout plus avant chaque tick des entrées hôte déjà propagées par le dernier accès ou le tick précédent. Après le tick, les sorties ne sont republiées que si le bus était occupé, qu'une sélection était active ou que le bus est devenu occupé. Les ticks cible, séquenceurs de déplacement, délais mécaniques et cellules ne sont pas supprimés. La fin de sélection et les retraits de BSY/REQ restent propagés.

Second passage Debug après cette modification : **49 réussites sur 50**, avec les 48 cas du relais réussis et le scénario invité DAT/DSC complet réussi en **1 min 23 s**. Le seul échec est le délai de sécurité de 90 s du scénario EMU : il atteint le dernier catalogue après le rechargement dans la seconde machine. Le premier parcours EMU jusqu'au catalogue après SAVE/LOAD passe d'environ 67,60 s à 58,96 s sur ces deux exécutions ; le temps au retour de `*ADFS` de la seconde session passe de 88,20 s à 74,88 s. Ces mesures de scénario incluent la saisie et l'attente, pas seulement le contrôleur ; elles ne démontrent ni une cadence native garantie ni une clôture de l'optimisation. La limite de 90 s n'a pas été relevée pour transformer l'échec en succès.

Les captures suivantes distinguent désormais les configurations : sous-dossiers `Debug/` et `Release/` de `artifacts/TestsChips/Acb4000/BbcMicroModelB/`. Une capture issue d'un scénario interrompu prouve seulement l'état affiché atteint, pas le succès des assertions restantes.

Après optimisation du relais, **les 50 cas passent en Release**, zéro échec, série de 29,62 s : les 48 cas permanents du stockage/relais et les deux scénarios invités (EMU 16 s, DAT/DSC 11 s). Les captures Release actualisées ont été produites après réouverture et ne sont plus remplacées par celles du passage Debug. Le banc temporaire et ses médias sont retirés après ce passage ; aucune ROM ni configuration utilisateur n'a été changée.

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

Le diagnostic de mesure après cumul est supprimé, ainsi que les six fichiers DAT/DSC/EMU de travail créés par ses passages Debug/Release. Les assemblies de tests sont reconstruites en Debug puis Release pour retirer son code : zéro erreur, trois avertissements préexistants hors Winchester. Aucun média temporaire ne subsiste dans le dossier `artifacts/.tmp-acorn-winchester/native/`. Les captures restent hors Git ; aucun build Debug All livré ou commit de composant terminé n'est revendiqué ici.


## Parcours invité après raccordement des échéances et reprises CRC

Le 4 novembre 2026, le diagnostic temporaire a exécuté sur le vrai CPU BBC B, avec MOS 1.20/BASIC II/ADFS 1.30 et le fichier de caractères SAA5050 existants : démarrage, `*ADFS`, `*CAT`, `*SAVE CHIPTEST 3000 +100` et `*LOAD CHIPTEST 3100`. Les 256 octets chargés ont été comparés au motif `index XOR 5Ah`, avec compteurs physiques de lecture et écriture non nuls. Après détachement, publication et fermeture du DAT/DSC, une nouvelle machine a rouvert l'image en lecture seule, exécuté `*LOAD CHIPTEST 3200` puis `*CAT` ; les 256 octets correspondent, avec des lectures physiques et zéro bloc écrit dans cette seconde session.

**Le cas natif Release passe en 10 s** (11,664 s pour la série VSTest). Ce temps est celui du scénario, pas une mesure de MHz ni une preuve générale d'optimisation. Conteneur de travail indépendant : 306 cylindres, quatre têtes, 33 secteurs de 256 octets. Aucun fichier ROM, configuration ou média utilisateur n'a été modifié ; aucune sortie audio Windows n'a été ouverte.

La capture inspectée `artifacts/TestsChips/Acb4000/BbcMicroModelB/Release/adfs-search-timing-reopened.png` montre le catalogue ADFS contenant CHIPTEST après réouverture ; `adfs-search-timing-first-load.png` correspond à la première session réussie. Les vérifications d'octets sont effectuées par le diagnostic, pas déduites de la capture. Une première tentative s'était arrêtée sur un nom de fichier SAA5050 incorrect, puis le banc de saisie envoyait toutes les lettres sur la touche A (`*A`, Bad command) : ces défauts du diagnostic ont été corrigés, sans changer le clavier émulé ni le contrôleur pour contourner le test. La capture du premier LOAD a été actualisée au passage réussi.

Cette nouvelle preuve concerne DAT/DSC, BBC B, port 0 et Release. Elle ne remplace pas les validations restantes des autres modèles/ports, EMU après les dernières modifications, écritures interrompues avec réouverture, ni les diagnostics internes mémoire. Les captures restent hors Git. Le diagnostic temporaire et ses deux fichiers `search-timing-native.dat`/`.dsc` sont supprimés après ce passage ; les assemblies Debug et Release sont reconstruites pour retirer son code.


## Preuve native après raccordement des registres ECC aux champs

Le 4 novembre 2026, deux scénarios Release ont exécuté le CPU BBC B 32 Kio avec les fichiers existants MOS 1.20, BASIC II, ADFS 1.30 et SAA5050. Chaque scénario crée son propre média de géométrie 306/4/33/256, au format DAT/DSC ou EMU, sans modifier les médias/configurations utilisateur.

Première session : démarrage, `*ADFS`, catalogue, écriture du fichier CHIPTEST depuis 3000h, relecture en 3100h, comparaison des 256 octets, compteurs de lecture et d'écriture non nuls. Le support est détaché, sauvegardé et libéré dans finally. Deuxième session : nouvelle machine, réouverture du fichier en lecture seule, chargement en 3200h, comparaison exacte des 256 octets, catalogue et compteur d'écriture nul. **2/2 cas passent, durée de série 21 s**. Le test n'intercepte pas les commandes MOS et n'ouvre aucune sortie audio Windows.

Les captures réouvertes DAT et EMU ont été inspectées : elles affichent le catalogue contenant CHIPTEST et la commande de chargement exécutée. Les comparaisons d'octets proviennent des assertions du scénario, pas d'une interprétation de l'image. Captures conservées hors Git dans `artifacts/TestsChips/Acb4000/BbcMicroModelB/Release/` :

- `adfs-ecc-registers-dat-first-load.png`
- `adfs-ecc-registers-dat-reopened.png`
- `adfs-ecc-registers-emu-first-load.png`
- `adfs-ecc-registers-emu-reopened.png`

Cette preuve concerne le BBC B, le port 0 et la configuration ECC nominale. Elle ne démontre pas encore tous les registres AIC, tous les modèles/ports, le formatage invité, les écritures interrompues, les autres révisions de firmware ni leur optimisation finale.

## Raccordement des ports hôte, 5 novembre 2026

Le contrôle AIC-300 52h est décodé et les drivers BIE/BOE/SEL/BSY sont raccordés au bus du contrôleur. Le port externe huit bits 50h utilise un latch partagé avec les lectures DMA ; RESET coupe les sorties sans effacer ce latch externe. 51h n'ajoute pas un second octet fictif sur la carte huit bits. Le retour de BSY après fin de message est transmis immédiatement au tampon avant son arbitrage de 100 ns.

Les bancs autonomes et diagnostics donnent 835/835 en Release (5 s), puis un cas complémentaire de lecture BIE et rejet 51h passe isolément (1/1, 24 ms) après correction de l'attente du banc. Le dernier passage Debug était de 835/836 (46 s) avant cette correction d'attente ; il n'est pas présenté comme entièrement réussi. Le document du composant conserve le détail des passages et de leurs écarts.

Après ce raccordement, le groupe existant `AcornEmuWinchesterStorageTests` et `AcornEmuScsiTests` réussit **58/58 en Release, 3 s**. La compilation de ce projet reconstruit aussi les dépendances Acorn et App avec le contrat du bus modifié, sans erreur. Ce lancement n'est pas le script de livraison Debug All et n'apporte pas de nouvelle capture invitée. Les captures précédentes restent hors Git. Le dump matériel intégral 61h demeure ouvert ; la carte n'est pas déclarée terminée.

## Dump matériel 61h natif et preuve invitée

Le refus de 61h sans bus externe est supprimé. Les lectures utilisent les registres réels du contrôleur et leurs effets ; les lectures non pilotées ont une convention numérique distincte, exposée avec ses limites dans la fiche ACB-4000. Aucun tableau de registres fictifs n'est ajouté. La voie supérieure 16 bits n'est pas montée sur cette carte huit bits : ses écritures ne créent pas de registre et ne sont plus rejetées comme un CDB invalide.

Un programme 6502 placé en RAM est lancé depuis BASIC par `CALL &2000` sur le BBC B. Il prépare 61h, reçoit 259 octets par l'interface Acorn, compare l'en-tête et les registres pilotés, vérifie le rejet d'une seconde réception sans préparation et réalise un préfixe de quatre octets. **1/1 en Release, 4 s** ; capture examinée et conservée hors Git : `artifacts/TestsChips/Acb4000/BbcMicroModelB/Release/hardware-diagnostic-61.png`. Aucun disque n'est nécessaire à cette commande interne. Cela ne remplace pas les essais de médias réels, les autres machines ou les mesures du bus interne 8085.

Le scénario externe est supprimé et son assembly reconstruite ; le groupe existant Winchester/SCSI passe ensuite **58/58 en Release, 4 s**. Le groupe ACB-4000/hôte passe **849/849 en Release, 5 s, et Debug, 50 s**. Le build de livraison et l'audit de clôture de toute la carte restent distincts de ces résultats.

## Clôture du modèle numérique Winchester, 5 novembre 2026

L'ancien accès SCSI direct aux secteurs est remplacé par la carte hôte, l'ACB-4000 et les mécanismes ST-506 du module. Les paramètres, contextes, microprogramme, ECC, RAM, horloges et drivers sont raccordés aux objets réellement utilisés par READ/WRITE/FORMAT et les diagnostics. Les deux ports physiques sont conservés ; aucun quatrième lecteur ou second contrôleur fictif n'est ajouté.

La validation finale passe **861/861 en Release (6 s) et Debug (44 s)** pour le groupe puces/hôte, **58/58 en Release (3 s)** pour le relais Acorn après les dernières corrections, et **124/124 en Release (917 ms)** pour DAT/DSC, Adaptec MFM et EMU. Elle inclut la réception d'un octet résolu lorsque BOE est forcé, le retrait immédiat de BOE à ACK et son maintien inactif jusqu'au cycle suivant. Ces résultats complètent les preuves invitées et leur persistance déjà décrites ; les captures précédentes ne sont pas présentées comme des captures réalisées après chaque optimisation.

Le coût isolé du contrôleur après optimisation est une médiane Debug de 217,218 ns/tick au repos et 261,259 ns en attente d'ACK ; après chauffe du JIT Release, 19,051 et 35,330 ns. Les mesures portent sur le contrôleur sans CPU BBC ni vidéo ni média. Zéro allocation ; aucune modification des options de compilation Debug et aucun changement de cadence matérielle. Les fractions d'horloge et tous les fronts restent conservés. Les lectures flottantes et les caractéristiques analogiques non garanties restent des conventions ou limites explicitement décrites dans les fiches, pas des propriétés matérielles inventées.

Ce composant est prêt pour la livraison de son modèle numérique documenté. Les passages complets des BBC A/B/B+ et des autres familles Acorn demeurent dans leur suivi propre ; cette clôture ne les annonce pas terminés. Les preuves de CPU conservées concernent le BBC B, les commandes ADFS, FORMAT/READ/WRITE/READ et 61h ; les fichiers de diagnostic externes et leurs médias de travail ont été supprimés après les passages.

## Livraison Debug complète

`scripts\local-building.cmd --building=debug --modules=A` s'est terminé avec le code 0 le 5 novembre 2026. L'exécutable livré est `build/Debug/Retro Media Lab/retromedialab.exe`. Les 24 manifestes et leurs assemblies d'entrée sont présents sous `Modules/` ; versions et noms des assemblies correspondent aux manifestes sources. `lib/retromedialab.chips.dll` est présente et son empreinte SHA-256 correspond à celle de l'assembly Debug courante compilée. Les formats et relais sont intégrés aux assemblies existantes, sans nouvelle dépendance directe de l'application à Chips ni du module Acorn à MediaEngine.

Le script livre application, launcher, updater et modules ; ce n'est pas une publication GitHub ou NuGet. Les avertissements de plateformes des modules externes pendant leur construction ne sont pas présentés comme des erreurs du contrôleur. Aucun média utilisateur n'est écrit par ce build. Le commit de ce bloc doit exclure les captures, rendus, logs, ROM et diagnostics temporaires.
