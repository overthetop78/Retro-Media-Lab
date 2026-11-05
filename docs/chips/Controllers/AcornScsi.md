# Acorn Winchester — adaptateur hôte SCSI

## Périmètre

État du 5 novembre 2026 : logique numérique de la carte hôte réalisée, raccordée au contrôleur natif et validée par les transactions CPU documentées. Cette carte du bus BBC 1 MHz n'est pas une puce SCSI monolithique. Elle assemble notamment des buffers, verrous, bascules et décodeurs TTL. Cette fiche décrit le comportement de carte représenté par `ScsiHostAdapter`, sans prétendre documenter séparément chaque 74LS.

Sources : `src/RetroMediaLab.Chips/Controllers/Acorn/Winchester/Scsi/`, interface `IAcornScsiHostAdapter`, service `ScsiHostAdapter`. La cible est décrite dans [AdaptecAcb4000.md](AdaptecAcb4000.md). Le raccordement aux machines et aux supports reste dans [AcornWinchester.md](../../machines/AcornWinchester.md).

## Références examinées

- [Acorn Winchester Disc 110/130 Service Manual, août 1984](https://theoddys.com/acorn/acorn_system_filing_systems/winchester_disks/Acorn%20Winchester%20Disc%20Service%20Manual.pdf), sections 5.1, 5.3 et 7.2–7.3.
- [Schéma Acorn Host Adapter](https://theoddys.com/acorn/acorn_system_filing_systems/winchester_disks/Acorn%20SCSI%20Host%20Adapter%20Schematic.jpg), référence des bascules et polarités.
- [BeebSCSI hostAdapter.v](https://github.com/simoninns/BeebSCSI/blob/master/BeebSCSI_CPLD/hostAdapter.v), [b2 scsi.cpp](https://github.com/tom-seddon/b2/blob/master/src/beeb/src/scsi.cpp), [B-Em scsi.c](https://github.com/stardot/b-em/blob/master/src/scsi.c) et [MAME scsi.cpp](https://github.com/mamedev/mame/blob/master/src/devices/bus/bbc/1mhzbus/scsi.cpp), comparaisons avec le schéma ; leur accord ne constitue pas une preuve indépendante du constructeur.

## Fonctions et couverture

| Fonction | Comportement représenté | Vérification isolée |
|---|---|---|
| FC40 lecture/écriture | Données reçues ou verrou de sortie ; acquittement ACK autorisé par REQ. | Lecture, écriture, conservation du verrou et séquence des quatre fronts REQ/ACK. |
| FC41 lecture | MSG sur D0, BSY D1, IRQ D4, REQ D5, I/O D6, C/D D7 ; D2/D3 nuls. | Combinaisons des signaux et état de repos. |
| FC42 écriture | Déclenche SEL ; BSY ou RESET le retire. | Sélection, acquittement par BSY, absence de sélection depuis les autres registres. |
| FC43 écriture | D0 autorise le verrou IRQ. REQ actif déclenche et mémorise IRQ lorsque l'autorisation est présente. | Toutes les valeurs de D0, activation pendant REQ, maintien après retrait de REQ, désactivation. |
| RESET | Retire SEL et l'autorisation/verrou IRQ ; ne réinitialise pas artificiellement le verrou de données ou la bascule ACK non câblés ainsi. | Reset avec signaux actifs et conservation des états concernés. |
| Registres hors décodage | Aucun effet de bord inventé. | Accès non décodés. |

Les signaux de l'interface sont des assertions logiques ; les broches physiques SCSI sont actives basses. Le manuel nomme D4 NIRQ, tandis que le schéma avec inverseur conduit à un état logique IRQ actif haut dans le registre. Cette distinction est conservée dans l'examen matériel.

## Horloge, ressources et optimisation

La carte est passive entre les accès et changements d'entrées : aucun `Tick` artificiel et aucun délai de commande synthétique. Le propriétaire fournit les accès de bus déjà qualifiés et les signaux de la cible. Les délais SCSI appartiennent à la cible et à son bus ; ils ne sont pas remplacés ici par deux cycles arbitraires.

Le zéro initial du verrou LS373 est une initialisation déterministe, pas une valeur de mise sous tension garantie par la documentation. Le composant ne possède aucun fichier, média, thread ou périphérique audio. Les coûts par accès sont mesurés ci-dessous ; les durées de suites ne sont pas des mesures de performance du composant.

## Résultats et intégration

`WinchesterController` raccorde maintenant la carte hôte Acorn, le port cible ACB-4000 et le relais ST-412 du module. Les accès aux registres propagent SEL/ACK/données puis les retraits asynchrones par BSY/REQ ; cette résolution ne fait pas avancer les horloges. Les ticks du propriétaire avancent séparément cible et mécanismes. RESET publie d'abord la fenêtre d'écriture interrompue et vide les curseurs, puis réinitialise la cible et les sorties hôte dans des blocs finally ; les images restent empruntées.

Le 4 novembre 2026, **27 cas du stockage/relais/câblage passent en Debug et Release**, zéro échec (3 s / 1 s). Les quatre nouveaux cas utilisent uniquement les registres de la carte hôte pour leurs transactions : MODE SENSE puis lecture d'un secteur sur chacun des deux ports à partir de pistes MFM en mémoire, sense d'une unité invalide, verrou IRQ et RESET suivi d'une nouvelle commande. Aucun appel direct au port cible ne réalise ces transactions ; la construction des pistes reste dans le banc MediaEngine existant. Les images sont détruites par leur propriétaire après le nettoyage du contrôleur en finally. Commande : `dotnet test tests/RetroMediaLab.LocalEmulationTests/RetroMediaLab.LocalEmulationTests.csproj --no-restore --configuration Debug -p:Platform=x64 --filter "FullyQualifiedName~AcornEmuWinchesterStorageTests" --verbosity quiet`, puis Release.

Ce passage de 27 cas précédait la substitution dans `Winchester`, réalisée ensuite dans les sources et décrite ci-dessous. Ces cas ne prouvent ni un accès ADFS invité, ni une réouverture de fichier, ni une nouvelle performance native ; aucune capture d'émulation n'est déduite des pistes en mémoire.

`tests/RetroMediaLab.Chips.Tests/Controllers/Acorn/Winchester/Scsi/ScsiHostAdapterTests.cs` : **72 cas réussis en Debug et Release** lors du passage déjà consigné dans le document Winchester. Ils incluent 64 combinaisons de signaux/autorisation et les séquences de verrous ci-dessus. Ils sont également inclus dans les groupes ultérieurs ACB-4000/hôte ; ces totaux ne sont pas des tests supplémentaires de la carte seule.

Le service BBC `Winchester` utilise maintenant `WinchesterController` dans les sources. Aucun build complet livré ni essai CPU/ADFS invité de ce montage n'est encore revendiqué. Restent à prouver ensemble : raccordement FC40–FC43/RESET/IRQ à la cible ACB-4000, accès invités ADFS, erreurs et arrêts, conservation des médias, timings et performances du montage complet. Aucune capture native n'était encore disponible à ce passage ; voir les résultats invités ajoutés ci-dessous. Les captures attendues vont dans `artifacts/TestsChips/<Chip>/<Machine>/`, hors Git.

## Substitution dans le service BBC — 4 novembre 2026

Le service `Winchester` raccorde désormais la carte hôte, l'ACB-4000 et les signaux physiques ST-412. Les deux ports réels remplacent les quatre unités de l'ancien raccourci. Les compteurs exposent les fronts STEP et les champs DATA reçus/émis ; les diagnostics tampon ne comptent pas comme des accès disque. Les réglages de vitesse sont relayés par lecteur ; à 100 %, les délais physiques sont conservés. Les anciens réglages de latence logique sont retirés des BBC, en conservant les champs Archimedes.

Le détachement publie la fenêtre d'écriture en attente avant la sauvegarde finale puis libère les références empruntées. Il ne supprime pas le fichier image. La remise à jour des signaux de présence ne fait pas avancer le temps. Lorsque le bus est libre et sans écriture en attente, la rotation et le temps des cellules avancent arithmétiquement, avec conservation de la fraction d'horloge.

Validation groupée après cette substitution et cette optimisation : **40 cas réussis en Debug (7 s) et Release (2 s), zéro échec** dans `AcornEmuWinchesterStorageTests`. Les cas nouveaux couvrent le service BBC à 100/200/800 % et Maximum, lecture via les registres, compteurs, RESET, limite des deux ports, arrêt du moteur au détachement et conservation de l'image empruntée. Ils contrôlent aussi l'acceptation DAT et les caractéristiques des conteneurs EMU, dont un support physique non formaté. Ce sont des pistes en mémoire, pas une preuve de réouverture de fichier ni de fonctionnement invité ADFS.

Le groupe puces ACB-4000/hôte donne **534 réussites en Release (2 s), zéro échec**. Le nouveau cas des statistiques avait également réussi seul en Debug (254 ms). Ces durées sont celles des suites, pas une mesure de vitesse native. Les diagnostics internes, échéances, fichiers médias, captures et performances du montage restent ouverts.

## Preuve invitée BBC B obtenue après substitution

Le 4 novembre 2026, le vrai CPU BBC B avec MOS 1.20, BASIC II et ADFS 1.30 a exécuté CAT, SAVE et LOAD sur le nouveau montage physique, pour les conteneurs **DAT/DSC et EMU**. Après détachement et sauvegarde, une nouvelle machine a rechargé le fichier depuis l'image rouverte en lecture seule. Chaque octet du motif de 256 octets est vérifié, ainsi que des compteurs physiques de lecture/écriture non nuls dans la première session et zéro bloc écrit dans la seconde.

Après retrait des résolutions SCSI répétées sans changement d'entrée, le groupe stockage/relais et ces scénarios donne **50 réussites en Release**, zéro échec, 29,62 s ; EMU termine en 16 s, DAT/DSC en 11 s. En Debug, **49 cas sur 50 passent** : les 48 cas du relais et le scénario DAT/DSC complet (1 min 23 s). EMU atteint encore la limite de sécurité de 90 s au catalogue final, après le rechargement dans la seconde session. L'optimisation n'est donc pas déclarée close ; cette limite et les étapes mesurées sont détaillées dans [AcornWinchester.md](../../machines/AcornWinchester.md).

Captures inspectées, hors Git : `artifacts/TestsChips/Acb4000/BbcMicroModelB/Release/adfs-native-dat-reopened.png` et `adfs-native-emu-reopened.png`. Elles montrent le catalogue invité contenant CHIPTEST après réouverture. Les captures Debug sont séparées, et un état affiché lors d'un délai dépassé n'est pas présenté comme le succès du scénario complet.

Le diagnostic temporaire est supprimé, ses seize fichiers médias de travail également ; les assemblies de tests sont reconstruites en Debug et Release, zéro erreur (trois avertissements préexistants hors de ce composant). Aucune configuration ni ROM utilisateur n'a été modifiée. Ces builds de tests ne sont pas un build Debug All livré. Les autres modèles, le deuxième port depuis ADFS, les interruptions avec réouverture, les fonctions App, les diagnostics de mémoire interne, les échéances et les mesures d'optimisation finales restent à terminer.

### Résultats actualisés après cumul des cycles au repos

Le relais diffère maintenant les ticks sans événement uniquement lorsque la cible, les positionneurs et les mécanismes sont au repos ; la phase de rotation et la fraction d'horloge sont restituées avant tout nouvel accès. Les 53 cas du stockage/relais passent en Debug et Release, dont les comparaisons exactes des octets et durées en ticks après repos cumulé ou pas à pas.

Les deux scénarios invités DAT/DSC et EMU passent désormais aussi **dans les deux configurations**, sans relever la limite de 90 s. Après les deux sessions avec sauvegarde puis réouverture en lecture seule, DAT atteint les assertions finales en 66,186 s Debug / 10,259 s Release ; EMU en 65,824 s Debug / 8,398 s Release, hors création et fermeture finale. Le précédent échec EMU Debug décrit plus haut n'est donc plus observé avec ce cumul. Les compteurs et les 256 octets rechargés restent identiques. Les détails de mesure et les limites restantes figurent dans [AcornWinchester.md](../../machines/AcornWinchester.md). Les captures par configuration sont actualisées, hors Git ; la carte complète n'est pas déclarée clôturée à partir de ce seul parcours.


## Retrait de la cible logique remplacée — 4 novembre 2026

L'ancienne classe `AcornScsi`, ses commandes/transferts, son interface, son énumération de phases et ses constantes sont supprimées. Aucun code de production ne l'instanciait plus après le raccordement de `ScsiHostAdapter`, `Acb4000` et du relais physique Winchester. Le décodage des adresses FC40–FC43 appartient maintenant aux constantes du propriétaire BBC ; les profils de géométrie utilisent les paramètres ACB-4000, sans dépendance au contrôleur supprimé.

Les anciens essais de quatre unités, commandes logiques de dix octets et délais artificiels ne prouvaient pas le comportement de cette carte : ils sont retirés, sans les présenter comme des fonctionnalités ACB-4000. Les tests de configuration persistent, adaptés aux deux ports réels et aux six profils DAT/DSC et EMU. Les essais physiques des commandes et des signaux restent dans les suites ACB-4000, carte hôte et relais.

Le groupe `AcornEmuScsiTests` et `AcornEmuWinchesterStorageTests` donne **57 réussites en Debug (12,21 s) et 57 en Release (3 s), zéro échec**. Le premier passage Debug avait donné 55/57 : deux assertions demandaient encore la restitution de `writeLatency`, retirée des réglages BBC par le passage aux délais physiques. Elles vérifient désormais l'absence des latences artificielles et la conservation de la vitesse. Compilation réussie dans les deux configurations, avec trois avertissements préexistants hors de ce composant.

Ce passage ne refait pas les scénarios invités déjà documentés et ne clôture pas les diagnostics internes, échéances sans événement, essais des autres modèles/ports ou la validation finale de la carte. Aucun nouveau build Debug All livré ni commit du composant incomplet.


## Relais exclusif par Emulation pour MediaEngine

La référence projet directe à `RetroMediaLab.MediaEngine` ajoutée au module Acorn est retirée. `WinchesterStorage` utilise maintenant les paramètres binaires de `IEmulationDiskImage` ; le relais SDK traduit la clé de métadonnées et son encodage de stockage. `WinchesterTrackSignals` demande un encodage physique typé au contrat SDK, qui traduit l'identifiant destiné à MediaEngine. La durée de cellule reste une caractéristique de l'horloge du mécanisme dans le module. Les opérations de piste existantes, les impulsions, les durées et la possession des images sont conservées.

**58 cas de configuration/stockage Acorn passent en Debug (10 s), zéro échec.** Ils comprennent une vérification des références de l'assembly Acorn compilée : MediaEngine n'y figure pas. Les tests de paramètres binaires, données invalides, pistes physiques et relais passent par les nouveaux accès du SDK. La recherche des références projet MediaEngine dans les projets `Emulation.*` et de ses imports/types dans le code Acorn ne retourne plus de résultat. Les accès au moteur média restent dans Emulation, qui le connaît déjà.

## Mesure des strobes hôte, 5 novembre 2026

`ScsiHostAdapterCostTests` effectue dix millions d'accès de chauffe puis cinq lots d'un million de lectures ou écritures FC40, avec REQ et direction stables. Les données reçues ou conservées et ACK sont vérifiés, ainsi que zéro allocation. Les médianes sont **Debug lecture 13,461 ns/accès, écriture 8,868 ns ; Release lecture 0,463 ns, écriture 0,391 ns**. Les plages observées sont respectivement 12,713–14,493 ; 8,354–9,938 ; 0,460–0,882 ; 0,379–0,401 ns.

Ce sont des répétitions à entrées constantes : le JIT peut simplifier ce chemin, et ces valeurs ne représentent ni un débit physique SCSI ni tous les changements de pins. Aucun délai de matériel n'est raccourci ; le modèle reste une logique passive de quelques opérations sur bits, sans allocation ni boucle d'horloge propre. Les deux cas passent séparément en Debug et Release. La cible, ses horloges, ses médias et la machine complète sont mesurés ou vérifiés séparément dans leurs fiches.
