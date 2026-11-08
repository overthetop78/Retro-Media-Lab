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
| HXCStream | `HxcStreamReader`, enregistré, groupe `trackNN.S.hxcstream`, flux multipiste | Aucun écrivain HXCStream enregistré | Dépend de la reconstruction des secteurs et du système de fichiers | Lecture présente ; aucun échantillon Acorn non compressé trouvé sous `F:\Retro` dans cette recherche. Ne pas annoncer une validation Acorn complète ni une écriture native. |
| JFD | Aucun lecteur JFD enregistré | Aucun écrivain JFD enregistré | Non raccordée | Archives JFD Archimedes présentes dans la collection TOSEC ; l'extension n'est pas assimilée à ADF. Support manquant. |
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

Les images restent sous `F:\Retro`, comme demandé. Les références techniques restent dans `artifacts/.tmp-archives/machines/Acorn/BBC Master 512/`, dont `m512tech09.htm` pour DOS Plus. Les fonctions JFD/HXCStream absentes restent à implémenter.

## TAP Atom — écriture et exploration natives

`AtomTapCodec` reconstitue les fichiers depuis les blocs série existants : synchronisation, nom, continuation, numéros, adresses et somme de contrôle. Le décodeur produit un fichier nommé complet ; le lecteur de systèmes de fichiers séquentiels commun l'expose à l'explorateur. `AtomTapeEncoder` reconstruit les segments, `AtomTapWriter` écrit les en-têtes natifs de 22 octets et les données via le mécanisme atomique existant. Les données incomplètes ou invalides restent signalées et sont refusées en écriture ; aucune injection RAM ni assimilation à TAP Spectrum n'est ajoutée.

Références : [Atomulator, format ATM](https://github.com/hoglet67/Atomulator/blob/master/docs/atommmc2.txt), [AtomTapeUtils](https://github.com/joakimoh/AtomTapeUtils). Nom de 16 octets, adresses et longueur sur 16 bits little-endian ; un TAP rassemble successivement ces fichiers.

`AtomTapMediaTests` : sept cas Debug réussis, 198 ms. Tailles 0, 1, 256, 257 et 65535 octets ; deux fichiers homonymes restent distincts ; extraction par `SequentialContentDecoderAdapter`, adresses conservées, aller-retour identique, checksum invalide et troncature refusés. Aucun nouveau test dans la fenêtre RML et aucune mesure de cadence machine ne sont revendiqués pour ce bloc.
