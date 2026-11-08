# Formats des médias Acorn — vérification du 8 novembre 2026

Périmètre demandé : SSD, DSD, ADL (SDL était une faute de frappe), 640, 800, JFD, ADF, DSK, HXCStream, UEF, CSW, TAP et WAV. Cette fiche distingue le raccordement présent dans le code d'un essai effectivement exécuté. Un format présent en lecture n'est pas déclaré complet en écriture ou en exploration par déduction.

## Chemins communs

`MediaRecognitionComposition` fournit les lecteurs et leurs extensions aux services de lecture. `MediaWritingComposition` fournit les écrivains et leurs destinations aux parcours Écriture/Conversion. La visualisation consomme les représentations sectorielles, les pistes ou les flux produits par ces lecteurs ; elle ne doit pas redécoder les conteneurs dans App. `MediaImageExplorationService` transmet le document à `MediaExplorer`, puis aux lecteurs de MediaFileSystems. Les bandes utilisent `SequentialMediaComposition` pour leurs fichiers décodés.

Le sélecteur du lecteur d'émulation reprend `EmulationMediaDevice.AcceptedExtensions`, défini dans Acorn depuis les capacités du lecteur et le service MediaProcessing. Un lecteur SD ne doit pas annoncer des disquettes DD ; ce contrôle reste distinct de la reconnaissance globale des fichiers par MediaEngine.

## Inventaire du code

| Extension / conteneur | Lecture du conteneur | Écriture du conteneur | Exploration des fichiers | Réserves |
| --- | --- | --- | --- | --- |
| SSD / DSD | `BbcDfsReader` | `BbcDfsImageWriter`, enregistré | `BbcDfsFileSystemReader` | FM, géométrie du lecteur à respecter ; pas de nouveau passage réel dans cet audit. |
| ADL / 640 | `AdfReader`, profil ADFS L 640 Kio | `AcornAdfWriter`, alias `.640` ajouté au registre commun | `AcornAdfsFileSystemReader` | Deux images `.640` et une `.adl` réelles vérifiées dans ce passage. |
| 800 DOS Plus | `AdfReader`, FAT FD FF FF, 80 × 2 × 5 × 1024, R=1–5 | `AcornAdfWriter`, `.img` / `.800` | FAT12 native à secteurs de 1024, raccordée dans ce passage | Ne pas confondre avec ADFS D à R=0–4 ou IBM 800 à secteurs de 512. Trois images GEM réelles vérifiées. |
| ADF | `AdfReader`, profils Acorn et Amiga distincts | Écrivains Acorn/Amiga selon le profil | ADFS pour les profils Acorn, AmigaDOS pour Amiga | L'extension seule ne permet pas de choisir la machine ; conserver taille, profil et contenu. Pas de nouvel essai Archimedes ici. |
| DSK Acorn Atom | `AcornAtomDskReader`, enregistré | Writer Atom enregistré dans `MediaWritingComposition` | Lecteur DFS/Atom existant | DSK est partagé avec d'autres familles : ne pas traiter toute extension DSK comme un disque Atom. Pas de nouvel essai Atom ici. |
| HXCStream | `HxcStreamReader`, enregistré, groupe `trackNN.S.hxcstream`, flux multipiste | `HxcStreamWriter`, publication atomique du jeu de pistes | Reconstruction des secteurs et lecteurs de systèmes de fichiers communs | Capture CPC réelle vérifiée en lecture/export natif ; aucun nouvel essai invité Acorn HXCStream revendiqué. |
| JFD | `JfdReader`, JFDI brut ou gzip | `JfdWriter`, tables et données natives conservées | ADFS raccordé ; image TOSEC explorée | La reconstruction physique des secteurs ordinaires est présente ; les pistes DiscOp 3, positions explicites et stubs restent conservés nativement et ne sont pas assimilés à une piste ordinaire. |
| UEF | `UefReader`, enregistré | `UefWriter`, enregistré ; enregistrement de signal par `TapeSignalFunctions` | `AcornTapeDecoder` pour les blocs BBC compatibles | La conformité de toutes les variantes Atom/UEF ne se déduit pas du décodeur BBC. Pas de nouveau passage cassette dans cet audit. |
| CSW | `CswReader`, enregistré | `CswWriter`, enregistré ; enregistrement de signal commun | Décodeur Acorn sur le signal compatible | Conteneur d'impulsions, pas une liste universelle de fichiers BBC. Pas de nouveau passage réel ici. |
| WAV | `WavTapeReader`, enregistré | `WavTapeWriter`, enregistré ; enregistrement de signal commun | Décodeur Acorn FSK lorsque le signal contient ce protocole | Un WAV quelconque n'est pas nécessairement une bande Acorn. Pas de nouveau passage réel ici. |
| TAP Atom | `AtomTapReader`, enregistré ; nom/adresses/longueur Atom validés puis génération des blocs | `AtomTapWriter`, enregistré | `AtomTapeDecoder`, enregistré dans `SequentialMediaComposition` | Extraction, encodage et écriture native vérifiés par les sept cas autonomes ci-dessous. |

La visualisation et l'écriture sur un support physique restent subordonnées aux représentations réellement fournies et à la géométrie cible. La conversion vers SCP/HFE ne signifie pas qu'un écrivain natif HXCStream existe.

## Corrections et vérifications du bloc 640 / DOS Plus 800

`.640` est déclaré dans `DiskImageFileExtensions`, le catalogue, la détection, `AdfReader`, la sélection des conversions et `MediaWritingComposition`. La capacité attendue est 655360 octets ; les autres capacités ne sont pas identifiées comme ce format. Le module Acorn reprend cet alias depuis MediaProcessing pour ses lecteurs DD capables de lire ADFS L.

Le lecteur FAT12 commun utilise la taille des blocs fournis par le moteur média. `Fat12DosPlusLayout` décrit la FAT native de deux blocs à l'offset zéro, les six blocs du répertoire racine, puis 792 clusters de 1024 octets. Le premier bloc n'est jamais interprété comme un BPB IBM ; le nom de volume vient du répertoire et non d'octets d'allocation pris pour une étiquette de boot. Les profils IBM restent à 512 octets et conservent leurs copies FAT et leurs validations.

Tests autonomes conservés : `AcornDosPlus800Tests` et `AcornDosPlusFileSystemTests`. Les 25 premiers cas passent en Debug (423 ms), puis le cas de non-régression IBM ajouté passe (44 ms) : 26 cas autonomes au total. Ils couvrent aussi les fichiers fragmentés, sous-répertoires, secteur manquant, espace libre, sélection du writer `.640`, limites de capacité et flux MFM. Les huit cas de migration préexistants exécutés dans le passage associé passent également. Aucun gain de cadence d'émulation n'est revendiqué pour cette correction de médias.

Diagnostic externe temporaire : lecture seule des six images dans `F:\Retro`, par `MediaRecognitionComposition.ReadingService`, vérification des destinations du registre d'écriture, encodabilité physique et exploration par `SectorFileSystemRegistry`. Les six cas passent (875 ms). Les fichiers vérifiés sont DOSBOOT sur les trois images de démarrage, GEM.BAT sur GEM2, DOCUMENT sur GEM3 et GEMSETUP.APP sur GEM4. Deux noms attendus initialement erronés dans le diagnostic ont été corrigés d'après leurs répertoires réels ; aucune correction matérielle n'en a été déduite. Le diagnostic `AcornMediaActualImagesTests.cs` a ensuite été supprimé, car il dépend des images externes. Aucun média de l'utilisateur n'a été modifié.

Les images restent sous `F:\Retro`, comme demandé. Les références techniques restent dans `artifacts/.tmp-archives/machines/Acorn/BBC Master 512/`, dont `m512tech09.htm` pour DOS Plus.

## TAP Atom — écriture et exploration natives

`AtomTapCodec` reconstitue les fichiers depuis les blocs série existants : synchronisation, nom, continuation, numéros, adresses et somme de contrôle. Le décodeur produit un fichier nommé complet ; le lecteur de systèmes de fichiers séquentiels commun l'expose à l'explorateur. `AtomTapeEncoder` reconstruit les segments, `AtomTapWriter` écrit les en-têtes natifs de 22 octets et les données via le mécanisme atomique existant. Les données incomplètes ou invalides restent signalées et sont refusées en écriture ; aucune injection RAM ni assimilation à TAP Spectrum n'est ajoutée.

Références : [Atomulator, format ATM](https://github.com/hoglet67/Atomulator/blob/master/docs/atommmc2.txt), [AtomTapeUtils](https://github.com/joakimoh/AtomTapeUtils). Nom de 16 octets, adresses et longueur sur 16 bits little-endian ; un TAP rassemble successivement ces fichiers.

`AtomTapMediaTests` : sept cas Debug réussis, 198 ms. Tailles 0, 1, 256, 257 et 65535 octets ; deux fichiers homonymes restent distincts ; extraction par `SequentialContentDecoderAdapter`, adresses conservées, aller-retour identique, checksum invalide et troncature refusés. Aucun nouveau test dans la fenêtre RML et aucune mesure de cadence machine ne sont revendiqués pour ce bloc.

## JFD — tables natives, erreurs et exploration

La spécification est conservée dans `artifacts/.tmp-archives/formats/JFD/jfd_dsk.cpp`, depuis [MAME / Nigel Barnes](https://github.com/mamedev/mame/blob/master/src/lib/formats/jfd_dsk.cpp). `JfdCodec` valide taille, tables, descripteurs, données et tables delta, avec décompression bornée à 64 Mio. Il conserve l'image logique native dans les métadonnées du document : historique delta, paramètres ADFFS, Obey, stubs, DiscOp 3 et positions ne disparaissent pas lors d'un export. Les modifications de données sont reportées par offset sur cette structure conservée ; un changement incompatible de géométrie ou deux éditions contradictoires d'un même stockage sont refusés.

Les secteurs disponibles exposent séparément CRC ID et DATA, densité, ordre physique et code de taille. Un stub sans données ne devient pas un secteur rempli de zéros. Le lecteur ADFS accepte le conteneur JFD et reconnaît son contenu ; son identité reste JFD. La politique de visualisation utilise les encodeurs FM/MFM existants selon la densité enregistrée pour les pistes ordinaires uniformes. Elle refuse de reconstruire silencieusement les dispositions spéciales comme des pistes ordinaires. Ces cas ne sont donc pas encore validés en écriture physique ; le catalogue ne les annonce pas comme tels.

Sept cas autonomes `JfdMediaTests` passent en Debug : brut/gzip, en-tête et Obey conservés, erreurs ID/DATA, modification de données, stubs, bornes, superposition delta et sélection de l'encodeur. Temps individuels de 3 à 165 ms. Diagnostic réel temporaire : `Alone In the Dark … F1001501.jfd`, extrait sans écrasement dans `F:/Retro/TestsFormats/JFD/`. Les tables source contiennent 795 secteurs, et non 800 : l'attente initiale de 800 dans le diagnostic a été corrigée après comptage indépendant des tables. L'exploration ADFS et la sélection de l'encodeur passent (98 ms), sans modifier le média. Le diagnostic externe a ensuite été supprimé des sources et de l'assemblage de tests.

## HXCStream — écriture native et index

Sources HxC conservées dans `artifacts/.tmp-archives/formats/HXCStream/`, depuis [jfdelnero/HxCFloppyEmulator](https://github.com/jfdelnero/HxCFloppyEmulator/tree/main/libhxcfe/sources/loaders/hxcstream_loader). Le conteneur produit les blocs métadonnées, I/O 16 bits et impulsions à longueur variable, LZ4, alignement 4 octets et CRC du chunk. Le writer publie les fichiers NN.S associés par `AtomicMediaFileSetWriter`.

Le lecteur conserve les index à leurs instants d'échantillonnage, sans les arrondir à une transition magnétique. La durée de révolution et le silence après sa dernière transition restent distincts. Le writer encode à 25 ou 50 MHz uniquement lorsque les instants sont représentables exactement, y compris l'I/O à une mesure pour 16 ticks. Il refuse une source qui exigerait un arrondi. Les captures natives inchangées sont réexportées octet pour octet, en conservant les parties avant/après les révolutions et l'ensemble des états I/O ; une empreinte de l'état du flux empêche de réutiliser une capture après modification de ses révolutions.

Cinq cas autonomes Debug passent (150 ms) : frontières d'index entre deux transitions, quatre longueurs de code d'impulsion, révolution sans transition, pistes associées, conservation native, refus des timings non représentables et CRC corrompu. Diagnostic réel temporaire : `Peter Pan … Side B/track43.0.hxcstream`, de la collection CPC ; décodage et export natif en mémoire identique, 109 ms, média inchangé. Le diagnostic a ensuite été supprimé. Il valide le conteneur partagé, pas un logiciel Acorn invité ni une écriture sur un lecteur physique.
