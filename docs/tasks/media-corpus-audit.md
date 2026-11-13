# Audit autonome du corpus de médias

Source par défaut : `F:\Retro`

Script : `scripts/temp/analyze-media-data.ps1`

Validateur : `tests/RetroMediaLab.LocalDiskImageTests`

Résultats : `artifacts/media-audit`

## Contrôles appliqués à chaque image

Le validateur contrôle :

- la reconnaissance du format et de la famille du média ;
- les informations et la structure de la représentation décodée : secteurs, flux, blocs, pistes optiques ou contenu séquentiel ;
- les blocs absents, invalides, vides ou de taille incohérente ;
- les bornes, les capacités et les dépassements numériques des volumes ;
- l'espace libre, qui ne peut être négatif ni dépasser la capacité annoncée ;
- les nombres de dossiers et de fichiers, en parcourant toute l'arborescence ;
- la somme des tailles logiques et occupées des fichiers, qui ne peut dépasser la capacité du volume ;
- les métadonnées, les attributs, les dates et les diagnostics obtenus pour chaque entrée ;
- l'extraction complète du contenu de chaque fichier et sa conformité avec la taille déclarée ;
- les informations ajoutées par MediaAnalysis, dont le type et l'icône ;
- les contenus qui restent inconnus après analyse.

L'absence de volume est enregistrée comme avertissement, car elle peut être normale. Un système de fichiers reconnu sans fichier, ou une disquette contenant un volume mais aucun fichier extrait, est une erreur à examiner.

## Classement des erreurs

Cette feuille ne contient aucune erreur en attente au début de la nouvelle campagne.

Lorsqu'une erreur est rencontrée, créer un titre de niveau 2 portant le type de l'image concernée, par exemple `## ATR`, `## ATX`, `## SCP` ou `## HFE`. Toutes les tâches concernant ce type restent sous ce titre. Ne créer le titre qu'au premier arrêt rencontré pour ce type.

Sous ce titre, créer une tâche pour l'image fautive, puis des sous-tâches concrètes indiquant chaque fichier à créer, modifier, déplacer, renommer ou supprimer. Consigner le chemin du rapport et le défaut exact observé avant de corriger le code.

## Fin de la campagne

    - Image différée : `F:\Retro\A Trier\Atari 400-800\Atari 8bit - Applications - [ATR] (TOSEC-v2023-08-29)\Big Asembler (1990)(Cygert, Henryk)(pl)(Side B)\Big Asembler (1990)(Cygert, Henryk)(pl)(Side B).atr`.
    - Fichiers à analyser ultérieurement avec leur application : `KOALA1`, `KOALA2`, `PIC12`, `PIC13` et `PIC2`. Leurs préfixes et leurs tailles observées ne constituent pas une signature de format.
    - La campagne a validé les index 163 et 164, puis s'est arrêtée à l'index 165 sur `Black Magic Composer (1992)(L.K. Safari)(Side A).atr`.

    - Black Magic Composer Side A est validée. La campagne a poursuivi jusqu'à l'index 172 et s'est arrêtée sur les fichiers `HELP1` à `HELP4` de `Blazing Paddles (1986)(Baudville)(US).atr`.

    - Image différée : `F:\Retro\A Trier\Atari 400-800\Atari 8bit - Applications - [ATR] (TOSEC-v2023-08-29)\Blazing Paddles (1986)(Baudville)(US)\Blazing Paddles (1986)(Baudville)(US).atr`.
    - Fichiers à analyser ultérieurement : `HELP1`, `HELP2`, `HELP3` et `HELP4`. Ils correspondent à des images d'aide Micro-Painter brutes, mais ne possèdent ni extension ni signature autonome permettant une reconnaissance fiable sans contexte.
    - La campagne a validé les index 173 à 191, puis s'est arrêtée à l'index 192 sur `MESSAGE.ISM` et `SURVEY1.SRV` de `Bullentin Board Construction Set (1985)(Antic Publishing)(Side A).atr`.

    - Bulletin Board Construction Set Side A est validée. La campagne a poursuivi jusqu'à l'index 213 et s'est arrêtée sur quinze fichiers de `CardStax v2.1 (1993)(Paterson, David A.)(PD)(Side A)[BASIC].atr`.

- [ ] Reconnaître entièrement le grand volume SpartaDOS de BBS et représenter correctement son adressage logique.
  - [ ] Afficher les secteurs d'un grand ATR logique sans inventer des pistes ou des faces physiques.
  - [ ] Lire le système de fichiers SpartaDOS et toutes ses entrées.
      - BBS est validé comme ATR logique de 8 192 secteurs de 128 octets contenant le volume SpartaDOS `APE_BBS`, avec 4 répertoires et 168 fichiers.
      - La campagne a poursuivi jusqu'à l'index 156 et s'est arrêtée sur `Bibo Menu Makers (1985)(Bibosoft).atr`.

- [ ] Refaire la reconstruction des fichiers Atari DOS à partir du répertoire complet et des chaînes sectorielles partagées.
  - [ ] Séparer la description des entrées, la lecture bornée des chaînes et l'analyse des allocations.
  - [ ] Construire les fichiers après l'analyse de toutes les entrées du volume.

    - Image valide et vierge : `F:\Retro\A Trier\Atari 400-800\Atari 8bit - Applications - [ATR] (TOSEC-v2023-08-29)\Bullentin Board Construction Set (1985)(Antic Publishing)(Side C)\Bullentin Board Construction Set (1985)(Antic Publishing)(Side C).atr`.
    - Le catalogue Atari DOS est vide, le VTOC déclare tous les secteurs de données libres et les trois secteurs d'amorçage sont présents. L'absence de fichier est normale pour cette disquette vierge.

    - BW Tape System est validé : ses quatre fichiers `.rd` sont classés comme configurations binaires Atari 8 bits. La campagne a validé les index 197 à 212, puis s'est arrêtée à l'index 213 sur le fichier sans extension `MENU` de CardStax Side A.

    - CardStax Side A est validé : `MENU` est reconnu comme programme Atari BASIC tokenisé, tandis que les classifications `.crd`, `.art`, `.gr8` et `.v` restent valides. La campagne s'est arrêtée au média suivant, Cartridge Dumper, sur le fichier sans extension `ROM16`.

    - Cartridge Dumper est validé : le fichier sans extension `ROM16` est reconnu comme fichier ROM de cartouche Atari 16 Kio. La campagne s'est poursuivie jusqu'à l'index 231 et s'est arrêtée sur `CONTROL` dans `Centro de Costos (1987)(Telematica SA)(CL).atr`, dont le type reste inconnu.

    - `CONTROL` est un fichier Atari DOS sans extension de 16 octets : `02` répété sept fois, `01` répété sept fois, puis `1D 9B`. `EXIS140` contient la référence `D2:CONTROL`. Il s'agit donc de données binaires propres à l'application, mais ces valeurs ne constituent pas une signature de format réutilisable. La catégorie justifiée est `Data`; le catalogue ne doit pas transformer cette valeur particulière ou le nom `CONTROL` en règle générale.
    - Le média reste documenté pour une reprise ultérieure. Aucune règle particulière n'a été ajoutée. La campagne a repris au média suivant et s'est arrêtée sur `MAP` dans `Character Set Display Utility (1989)(ANALOG Computing)[a].atr`.

    - `MAP` contient un écran Micro-Painter brut : 7 680 octets de pixels suivis des quatre registres couleur `1E AF 19 E1`, soit 7 684 octets. Comme les fichiers différés de Blazing Paddles, il ne possède aucun en-tête autonome. Il reste associé au travail ultérieur sur les images Micro-Painter sans extension ; aucune règle fondée uniquement sur sa taille n'est ajoutée.
    - La campagne a repris à l'index 244 et s'est arrêtée sur l'entrée `--------.---` de `Code3 Cruncher v2.2d (1993)(Bienias, Adam)(PL)(en)(SW).atr`.

    - La première entrée `--------.---` contient 4 250 octets de code, commence au secteur 4 immédiatement après les trois secteurs d'amorçage et contient le système BDOS : c'est le fichier système chargé au démarrage dont le nom de catalogue a été remplacé. Les deux autres entrées homonymes sont vides et participent, avec `DISK`, `DON'T PA.CK`, `DON'T SA.VE` et `ON THI.S`, à un message affiché dans le catalogue.
    - Une reconnaissance propre doit provenir du contexte d'amorçage fourni par le lecteur Atari DOS. Aucune règle fondée sur le nom falsifié ou sur une séquence particulière de ce programme n'est ajoutée.
    - La campagne a repris à l'index 256 et s'est arrêtée sur `XSYSTEXT` dans `CodeBuster (19xx)(Wells, Tom).atr`.

- [ ] Identifier le fichier Atari 8 bits `XSYSTEXT` de `CodeBuster`.
    - `XSYSTEXT` est un texte ATASCII sans extension de 3 203 octets contenant des équates du système d'exploitation : caractères textuels et séparateurs de lignes `9B`. Sans extension, son rôle précis de source ne peut pas être établi de manière générale ; la classification fiable est `Text` avec l'encodage `Atascii`.
      - La reconnaissance directe devra réutiliser le tableau existant : chaque règle sera évaluée à partir des seuls critères qu'elle renseigne, notamment l'extension, les signatures, les longueurs et le type d'aperçu. Les critères renseignés seront cumulés. Une règle limitée à `PreviewKind`, à `ContentLengths`, ou à leur combinaison devra appeler l'analyse générale correspondante du contenu. Cette refonte est différée afin d'être conçue pour tous les contenus concernés et de ne pas introduire une exception ATASCII propre à `XSYSTEXT`.
      - La campagne a validé les index 259 et 260, puis s'est arrêtée à l'index 261 sur `BETH.PZM` dans `Colorizer v1.1 (1992)(AtariServ).atr`.

    - `BETH.PZM` est l'unique entrée `.PZM` du média. Elle contient 15 360 octets, sans en-tête ni fin de fichier distinctifs.
    - La documentation des suffixes Atari 8 bits identifie `.PZM` comme une image Pryzm non compressée de 80 × 192 pixels et 256 couleurs. Sa longueur correspond exactement aux 15 360 pixels de cette définition. La documentation de Colorizer confirme que l'application manipule des images Atari 8 bits à 256 couleurs composées à partir des modes GTIA.
    - Comme `.PZM` possède ici une signification documentée et propre à la famille Atari 8 bits, la classification proposée est `Image` par extension dans le catalogue Atari 8 bits. La longueur observée reste une confirmation du fichier examiné et ne devient pas un critère général de cette règle.
    - `Colorizer v1.1` est validé. La campagne s'est arrêtée au média suivant, à l'index 262, sur les fichiers `.B`, `.G` et `.R` de `Colorview v2.5`.

    - `COLORS.B`, `COLORS.G`, `COLORS.R`, `SGIRL15.B`, `SGIRL15.G` et `SGIRL15.R` contiennent chacun 7 680 octets. ColorView représente une image 80 × 192 par trois écrans de 7 680 octets, un pour chaque composante rouge, verte et bleue. Les références de formats Atari 8 bits recensent `.B`, `.G` et `.R` comme extensions des fichiers RGB.
    - `UNICRN15.B` commence par `FF FF` et a été classé à tort comme exécutable par la signature Atari Binary Load avant que son extension puisse être consultée. L'ajout des trois extensions ne suffit donc pas à garantir la classification correcte de tous les fichiers du média.
    - `Colorview v2.5` est validé : les fichiers `.B`, `.G` et `.R` sont classés comme images même lorsque leur contenu commence par l'indice faible `FF FF`. La campagne s'est arrêtée à l'index 264 sur `ARTLOAD` dans `Colour Enhancer for Micropainter & Atari Artist`.

    - `ARTLOAD` contient 1 739 octets et commence par `FE FE C7 06`. Le champ little endian `C7 06` vaut 1 735, soit exactement la longueur totale moins les quatre octets de l'en-tête.
    - Les fichiers `ARTIST.M65` et `MYCIO.M65` du même média commencent également par `FE FE`, suivis d'un champ égal à leur longueur totale moins quatre. La documentation MAC/65 définit cette structure comme celle d'un source sauvegardé sous forme tokenisée : marque `FE FE`, longueur du programme sur deux octets, puis lignes tokenisées.
    - `ARTLOAD` est donc un source MAC/65 tokenisé sans extension. Une règle limitée à `FE FE` ne suffit pas : la reconnaissance fiable doit aussi valider le champ de longueur et la structure successive des lignes.
    - `ARTLOAD` est reconnu comme source MAC/65 tokenisé par la combinaison des trois positions fixes. La campagne a validé les médias suivants jusqu'à l'index 273, où elle s'est arrêtée sur `NAN8` dans `Computereyes v1.3`.

    - `NAN8` contient 7 680 octets de données graphiques brutes. Le média et la documentation ComputerEyes l'identifient comme une capture à huit niveaux utilisant la zone bitmap Graphics 8.
    - Le fichier n'a ni extension ni en-tête ou fin de fichier propre au format. Sa taille correspond à la zone graphique, mais elle ne suffit pas à distinguer ce contenu d'une autre image brute de même taille. Aucune règle fondée uniquement sur le nom `NAN8` ou sur sa longueur n'est ajoutée.
    - La campagne a repris à l'index 274 et s'est arrêtée sur `HERMAN9` et `NAN9` dans `Computereyes v2.0`.

    - `HERMAN9` et `NAN9` contiennent chacun 7 680 octets. Ce sont les images brutes Graphics 9 fournies avec ComputerEyes v2.0, sans extension, en-tête ou fin de fichier distinctifs.
    - Comme pour `NAN8`, aucune règle fondée seulement sur leur nom ou leur longueur n'est ajoutée. Ils restent avec les images brutes sans signature à reprendre ultérieurement.
    - La campagne s'est poursuivie jusqu'à `Disk Doctor II` et s'est arrêtée sur `LABELS.LDT`.

    - Le manuel de Disk Doctor II indique que ses fichiers de labels de désassemblage reçoivent automatiquement l'extension `.LDT`. `LABELS.LDT` contient la table des noms associés aux adresses système ; il doit être classé comme fichier de données Atari 8 bits.
    - `Disk Doctor II` est validé. La campagne s'est arrêtée plus loin sur le fichier sans extension `MENU` de `Disk Tool 4`.

    - `MENU` est un programme Atari BASIC tokenisé commençant par `00 00 10 01`. Le fichier `POLYCPY4.BAS` du même média possède le même préfixe et est reconnu grâce à son extension `.BAS`. Cette variante complète l'en-tête `00 00 00 01` déjà pris en charge.
    - `Disk Tool 4` est validé. La campagne s'est arrêtée plus loin sur `SETUP.DC` dans `DOS Control v1.0`.

    - `SETUP.DC` est un fichier binaire de 4 980 octets dont l'en-tête commence par l'identifiant `DC-MOD1`. Il s'agit d'un module chargé par l'application `DC.COM`, à classer comme `Library` dans la famille Atari 8 bits.
    - `DOS Control v1.0` est validé. La campagne s'est arrêtée sur sept fichiers `.DCT` de `DOS Control v2.5`.

    - Les sept fichiers `.DCT` commencent par l'identifiant `DC-MOD20`. Ils représentent les modules de DOS Control 2.0/2.5 et doivent être classés comme `Library` dans la famille Atari 8 bits, comme les modules `.DC` de la version 1.0.
    - `DOS Control v2.5` est validé. La campagne s'est arrêtée sur neuf fichiers `.NLQ` dans `Dot-Magic`.

    - Les fichiers `BLOCK.NLQ`, `BROADWAY.NLQ`, `CURSIVE.NLQ`, `OHIO.NLQ`, `OLDE.NLQ`, `OLDWEST.NLQ`, `ROMAN.NLQ`, `SANSERIF.NLQ` et `SCRIPT.NLQ` sont des polices Near Letter Quality. Les références de formats Atari 8 bits recensent également `.NLQ` comme police Daisy Dot. Ils doivent être classés dans `Font`.
    - `Dot-Magic` est validé. La campagne s'est arrêtée sur les fichiers `.PB`, `.3` et `.7` de `Draw7 XE`.

    - Les cinq fichiers `.PB` partagent l'en-tête `77 02 00 D8 E0 16 03 50 81 00 00 0F`. Ils stockent les commandes de dessin rejouables de Draw7 ; `EAGLE1.PB` référence le fichier suivant `EAGLE2.PB`. Ils doivent être classés dans `Data`.
    - `PIC.3` contient 244 octets, soit 240 octets d'écran Graphics 3 et quatre octets de couleurs. `PIC.7` contient 7 684 octets, soit 7 680 octets d'écran et quatre octets de couleurs. Ces deux fichiers doivent être classés dans `Image` par leurs extensions Atari 8 bits propres à Draw7.
    - `Draw7 XE` est validé. La campagne s'est arrêtée sur `PLANTARY` et `WATCH` sans extension dans `Drawing Board`.

    - `PLANTARY` et `WATCH` font partie des huit images Drawing Board du média. Ces huit fichiers possèdent exactement la même séquence finale de 32 octets, `AE E7 02 AC E8 02 86 80 84 81 A9 00 85 92 85 CA C8 8A A2 82 95 00 E8 94 00 E8 E0 92 90 F6 A2 86`.
    - Certains de ces fichiers étaient classés `Data` à cause de leur préfixe nul. La fin commune constitue une signature de format beaucoup plus précise et doit les classer dans `Image` sans utiliser leur nom ni leur longueur.
    - `Drawing Board` est validé. La campagne s'est arrêtée sur les fichiers `.G15`, `.G9` et `.DEM` de `Easy Scan v2.0`.

    - Les fichiers `.G15` sont des images Graphics 15 de 7 684 octets et `SALLY.G9` est une image Graphics 9. Leur taille confirme les fichiers observés mais ne devient pas un critère du catalogue.
    - `ESCAN20.DEM` commence par `00 00 30 01` et contient un programme Atari BASIC tokenisé. Comme `.DEM` peut désigner plus généralement des données de démonstration, cette classification doit exiger ensemble l'extension et cet en-tête.
    - `Easy Scan v2.0` est validé. La campagne s'est arrêtée plus loin sur le fichier sans extension `PRT` de `Financial Wizard`.

    - La compilation Debug ciblée de `RetroMediaLab.LocalDiskImageTests` réussit sans erreur.
    - Le contrôle autonome crée une vraie fenêtre et un handle natif, puis confirme la destruction du handle, l'arrêt complet du `Dispatcher` et la fin du thread STA.
    - La recherche ciblée dans le projet confirme que le test Explorateur et ce contrôle autonome sont les seuls propriétaires WPF ; tous deux passent désormais par `WpfResourceCleanup`.
    - Le processus exécutant `--self-test` se termine normalement avec le code de sortie `0`.

    - `PRT` contient trois lignes ATASCII de commandes d'imprimante : `1B 21 9B`, `1B 22 9B` et `1B 4E 9B`.
    - Les programmes BASIC du même média ouvrent `D:PRT` et demandent à l'utilisateur de saisir les commandes de contrôle de son imprimante. `PRT` est donc un fichier de configuration d'impression.
    - Les commandes enregistrées peuvent être remplacées par l'utilisateur. La séquence observée et la longueur de neuf octets ne définissent donc pas un format stable et ne doivent pas devenir une signature du catalogue.
    - `Financial Wizard` est validé. La campagne s'est arrêtée sur `KHARM`, `P50` et `P50B` sans extension dans `K3 Wave Table Editor`.

    - `P50B` contient l'interface « PATCH DUMP, SAVE FOR THE K3 », référence `D:KHARM` et effectue ses accès disque par CIO. `P50` est un petit module d'entrée-sortie qui prépare également les blocs IOCB et appelle CIO. `KHARM` contient du code 6502 et ses tables internes.
    - Aucun des trois fichiers ne possède l'enveloppe Atari Binary Load `FF FF`. Ils sont chargés à des adresses imposées par l'application et ne constituent pas des exécutables autonomes ; ils doivent être classés dans `Library`.
    - Leurs tailles et leurs noms ne participent pas à la reconnaissance. Chacun possède en revanche un début de code distinctif suffisamment long pour identifier le module sans confondre les données musicales `.P50`.
    - `K3 Wave Table Editor` est validé. La campagne s'est arrêtée sur `CORE.BIN` dans `Laserteller`.

    - `CORE.BIN`, `LASER.BIN` et `SETUP.BIN` commencent chacun par un point d'entrée 6502, contiennent du code machine et sont chargés par l'application à des adresses imposées. Ils doivent être classés dans `Library` plutôt que comme exécutables Atari Binary Load autonomes.
    - `LASER.BIN` et `SETUP.BIN` étaient classés à tort dans `Text` par la détection générique, car leurs nombreuses chaînes d'interface rendaient plus de 90 % de l'échantillon affichable. Leurs signatures de code doivent être prioritaires sur cette détection.
    - L'extension `.BIN`, le nom et la taille ne suffisent pas à distinguer ces modules des autres fichiers binaires Atari.
    - `Laserteller` est validé. La campagne s'est arrêtée sur `WIZTALK.SPK` dans `Math Wizard II`.

    - `WIZTALK.SPK` est la banque de parole de « Talking Math Wizard ». Le programme BASIC associé appelle `SPEAK`, tandis que le fichier contient une table d'offsets suivie des données vocales encodées.
    - L'extension Atari 8 bits `.SPK` suffit à désigner ces données de parole. Le nom, la taille et les octets particuliers de cette banque ne doivent pas participer à la reconnaissance.
    - `Math Wizard II` est validé. La campagne s'est arrêtée sur huit images Micro-Painter sans extension.

    - `CAR`, `COFEMILL`, `EINSTEIN`, `GUITAR`, `SAILBOAT`, `STILLIFE`, `SUNMOON` et `TIGER` sont huit images Micro-Painter de 7 684 octets.
    - La longueur correspond au format observé, mais elle reste volontairement exclue comme critère unique. Aucun en-tête distinctif commun n'est présent ; les octets graphiques commencent immédiatement.
    - Ce cas rejoint les images Micro-Painter sans extension déjà reportées. Il sera repris avec la reconnaissance générale des contenus graphiques dépourvus de signature.

    - `SHUT` contient 3 072 octets de données graphiques brutes, sans extension ni en-tête, parmi les images `.MIC` du média.
    - Il ne possède pas la structure complète de 7 684 octets des images Micro-Painter. Une signature tirée de ses pixels identifierait seulement ce dessin particulier.

    - L'extension `.MO2` est partagée par plusieurs contenus de Mini Office II : `COM.MO2`, `GRAPHICS.MO2`, `MAINMENU.MO2` et `WORDPROC.MO2` sont des exécutables Atari Binary Load, tandis que `ADDRESS.MO2` est une base d'adresses structurée contenant ses champs et ses textes en ATASCII.
    - `ADDRESS.MO2` possède l'en-tête interne `CD C4 01 0D` suivi de sa table de champs. Il doit être classé dans `Data` par cette structure, sans donner un sens unique à l'extension `.MO2` et sans employer son nom ou sa taille.
    - `Mini Office II` est validé. La campagne s'est arrêtée sur `BOY1.GR9` et `NEWS.PLM` dans `Multi Graph View`.

    - `BOY1.GR9` est une image Atari Graphics 9 et `NEWS.PLM` une image Atari 8 bits PLM de 80 × 96 pixels en 256 couleurs.
    - Les deux fichiers contiennent 7 684 octets, soit les données attendues de ces formats graphiques, mais leur taille ne sera pas utilisée comme critère de reconnaissance.
    - Dans le contexte Atari 8 bits, les extensions `.GR9` et `.PLM` suffisent à identifier ces formats d'image.
    - `Multi Graph View` est validé. La campagne s'est arrêtée sur le fichier graphique sans extension `MAP` de `MultiDOS`.

    - `MAP` contient 7 684 octets de données graphiques brutes sans extension ni en-tête de format distinctif.
    - Son contenu présente des données d'écran et de palette, mais aucun critère structurel ne permet actuellement de distinguer ce fichier des autres images brutes Atari 8 bits sans employer son nom ou sa taille seule.
    - Ce cas rejoint les images Micro-Painter sans extension déjà reportées et sera repris avec leur reconnaissance générale.

    - Chaque morceau possède deux fichiers homonymes : un fichier `.MUS` déjà reconnu et un fichier sans extension. La documentation du logiciel confirme que les morceaux Atari sont stockés par paires.
    - Les dix fichiers sans extension commencent par le marqueur commun `1F 0A 10 00` et contiennent, 32 octets avant leur fin, le bloc terminal commun `1F 0A 00 36 1F 0A 60 36 1F 0A C0 36 1F 0A 20 37 1F 0A 80 37 1F 0A E0 37 1F 0A 40 38`.
    - La reconnaissance combinera ces deux emplacements fixes. Elle n'utilisera ni le nom, ni la longueur variable, ni les notes propres à un morceau.
    - `Music Construction Set` est validé. La campagne s'est arrêtée sur les morceaux et instruments de `The Music Studio`.

    - Les fichiers dont le nom commence par `M` contiennent les morceaux ; ceux dont le nom commence par `S` contiennent les instruments. Les fragments placés après le point proviennent des titres longs stockés dans les champs 8.3 et ne constituent pas des extensions de format fiables.
    - Tous possèdent la même table d'instruments de 256 octets. Trois blocs binaires non textuels sont identiques aux positions 68, 187 et 238 dans les morceaux comme dans les fichiers d'instruments.
    - La reconnaissance utilisera la combinaison de ces trois blocs fixes et ne dépendra ni du nom, ni de la pseudo-extension, ni de la longueur du morceau.
    - `The Music Studio` est validé. La campagne s'est arrêtée sur `PANTHER.FN0` dans `MyDOS 4.50T & Utils`.

    - `PANTHER.FN0` contient une fonte bitmap Atari complète de 1 024 octets, aux côtés de `PANTHER.FNT` et de plusieurs autres fontes `.FNT` sur le même média.
    - Dans ce contexte Atari 8 bits, `.FN0` est une extension de fonte et suffit à la classification ; la longueur ne sera pas utilisée comme critère.
    - `MyDOS 4.50T & Utils` est validé. La campagne s'est arrêtée sur `MEM.SA` dans `The NewsRoom`.

    - `MEM.SA` contient du code machine 6502 brut appelé par les autres modules de `The NewsRoom`. Il ne possède pas l'enveloppe Atari Binary Load et n'est donc pas un exécutable autonome.
    - L'extension `.SA` n'est pas suffisamment générale pour être classée seule. Le module sera reconnu par son en-tête machine de 16 octets et classé comme bibliothèque interne.
    - `The NewsRoom` est validé. La campagne s'est arrêtée sur `TITLE.SCN` dans `Picility v9G`.

    - `TITLE.SCN` contient un écran bitmap Atari suivi de ses octets de couleur. Il est chargé comme écran de titre par l'application graphique `Picility v9G`.
    - Dans la famille Atari 8 bits, l'extension `.SCN` désigne ici un écran et suffit à le classer dans `Image`; la longueur ne sera pas utilisée comme critère.
    - `Picility v9G` est validé. La campagne s'est arrêtée sur les dessins sans extension de `Player-Missile Graphics Tablet`.

    - `LOG` et `TEST1` à `TEST5` sont des dessins de l'éditeur Player-Missile, au même format que `EXAMPLE.DAT` et `EXPRINT.DAT` présents sur le média.
    - Tous commencent par l'en-tête binaire `17 28 CA 94 46 00 70 70 70 4D 60 90 0D 0D 0D 0D`. La longueur identique de 4 206 octets ne sera pas utilisée comme critère.
    - Une règle de contenu prioritaire permettra également de corriger la classification générique `Data` des deux fichiers `.DAT` de ce même format.
    - `Player-Missile Graphics Tablet` est validé. La campagne s'est arrêtée sur les entrées du volume spécialisé `Atari CLK graphics library`.

    - Le volume est correctement reconnu comme `atari-clk-graphics-library` et ses 50 entrées sont correctement extraites avec `NativeTypeId = atari-clk-graphic`, le commentaire `Atari CLK graphic` et l'attribut `graphics`.
    - Le défaut restant concerne uniquement leur classification dans MediaAnalysis : elles restent `Unknown` bien que le système de fichiers ait déjà identifié leur type natif.
    - Ce cas est reporté afin de définir plus tard une utilisation générale des types natifs déjà reconnus, sans ajouter une colonne à toutes les tables pour cette seule disquette.
    - Le média 783 est validé. La campagne s'est arrêtée à l'index 784 sur `The Print Shop - Graphics Library (Disk 1 of 3)`, qui utilise le même système `atari-clk-graphics-library` et présente le même défaut de classification différé.
    - La campagne s'est arrêtée à l'index 785 sur le disque 2/3 de la même bibliothèque CLK ; ce média est rattaché au même cas différé.
    - Le média 786 est validé. La campagne s'est arrêtée à l'index 787 sur `The Print Shop - Icons 01`, encore reconnu comme `atari-clk-graphics-library` avec uniquement ses graphismes classés `Unknown`.
    - Les index 787 à 796, 798, 801 et 802 présentant strictement ce même cas ont été différés ; les index 797, 799, 800, 803 et 804 ont été validés normalement.
    - La campagne s'est arrêtée à l'index 805 sur `PrintPower (1987)(Hi Tech Expressions)(US)(Side B).atr` : le volume Atari DOS contient 17 fichiers extraits, dont 14 fichiers `.001` classés `Unknown`.

    - `.001` est un suffixe commun aux ressources du disque et ne désigne pas un type unique : il ne doit donc pas être ajouté seul au catalogue.
    - `FONTS.001` associe les noms affichés aux fichiers `AVANT.001`, `HEADLINE.001`, `OLDENG.001`, `TIMES.001` et `ZAPF.001`. Ce sont les fontes de PrintPower et elles doivent être classées dans `Font`.
    - `GRAPHICS.001` associe chaque dessin à un fichier `GV1.001` à `GV7.001`, avec un numéro de variante et une catégorie. Ces sept fichiers sont des bibliothèques graphiques de PrintPower et doivent être classés dans `Image`.
    - `BORDERS.001` associe les bordures aux fichiers `B.001` et `B2.001`, avec leur numéro de variante. Ces deux fichiers sont des bibliothèques de bordures et doivent être classés dans `Image`.
    - Les cinq fontes partagent `6B 29` à l'offset 2 puis la même organisation bitmap. Les deux bibliothèques de bordures commencent par `14 00 00 00` et une table de vingt offsets. Les bibliothèques graphiques commencent par `05 00 00 00` et une table de cinq offsets, mais cette seule valeur n'est pas assez distinctive pour être utilisée isolément.
    - La documentation de PrintPower confirme que le produit fournit plusieurs fontes, des graphismes et des bordures ; les fichiers d'index du média établissent précisément la fonction de chaque fichier binaire observé.

    - Les fontes seront reconnues par l'extension `.001` et leur bloc fixe de 16 octets à l'offset 2.
    - Les bibliothèques de bordures seront reconnues par l'extension `.001`, leur en-tête `14 00 00 00` à l'offset 0 et la fin fixe de leur table à l'offset 42.
    - Les bibliothèques graphiques seront reconnues par l'extension `.001` et leur en-tête `05 00 00 00` à l'offset 0.
    - Les trois règles seront prioritaires afin que leur contenu précis soit examiné avant toute classification générique de l'extension.
    - `PrintPower` est validé avec ses cinq fontes, ses deux bibliothèques de bordures et ses sept bibliothèques graphiques reconnues. La campagne a validé les médias suivants jusqu'à l'index 832 et s'est arrêtée à l'index 833 sur `PATTERN1.USR` de `RAMbrandt Utilities`.

    - Le manuel de RAMbrandt décrit cinq motifs utilisateur prédéfinis pouvant être redéfinis puis enregistrés sur une disquette DOS au moyen du module fourni.
    - `PATTERN1.USR` contient ces données de motifs : il commence par la table fixe `0B 00 00 03 00 06 00 09 00 0C`, suivie des données graphiques des motifs.
    - `.USR` ne sera pas classée seule, car cette extension peut également désigner des routines machine appelées par la fonction BASIC `USR`.
    - La combinaison de l'extension `.USR` et de l'en-tête complet classera ce format RAMbrandt dans `Image`.
    - `PATTERN1.USR` est validé comme ressource graphique RAMbrandt. La campagne a validé les médias suivants jusqu'à l'index 847 et s'est arrêtée à l'index 848 sur `Rubber Stamp v1.0`, dont la somme des tailles logiques dépasse la capacité du média.

    - Index 848 : `Rubber Stamp v1.0 (1985)(XLEnt Software)[cr Spiders].atr`.
    - Le lecteur Atari DOS extrait 27 fichiers mais totalise 98 764 octets logiques sur une capacité de 92 160 octets.
    - Les chaînes des fontes `CURSIVE1.FNT`, `ADVEN.FNT`, `ARCHAIC2.FNT`, `FANCY2.FNT`, `FANCY3.FNT`, `STANDARD.FNT`, `STYLISH.FNT` et de `DDOP.PIC` rejoignent des secteurs également attribués à `EDIT16.ASM`.
    - Ce média est reporté afin de reprendre ultérieurement l'analyse complète de ses chaînes Atari DOS sans modifier le lecteur pour ce seul cas.
    - La campagne a repris à l'index 849 et s'est arrêtée à l'index 859 sur `Screen Dump II`.

    - Index 859 : `DRUCKER.PAR` contient 64 octets de paramètres binaires pour l'imprimante `DELTA 15X`, avec plusieurs séquences de contrôle `ESC`.
    - Dans ce média Atari 8 bits, l'extension `.PAR` désigne un fichier de paramètres d'imprimante ; il est classé dans `Configuration` avec un aperçu hexadécimal.
    - `Screen Dump II` est validé. La campagne s'est arrêtée à l'index 871 sur `Sesame Street Print Kit`.

    - Index 871 : les vingt fichiers `.004` sont les graphismes Sesame Street du kit d'impression.
    - `CITOH`, `IMAGEWRI`, `OKIMATE1`, `P321TOSH`, `QUIETJET`, `WIDENB24` et `WIDEQUIE` sont des configurations binaires d'imprimante de 291 octets sans extension.
    - Ces configurations partagent le bloc de contrôle fixe à l'offset 20 et le marqueur terminal à l'offset 274 ; leurs octets de paramètres variables ne sont pas utilisés comme signature.
    - `Sesame Street Print Kit` est validé. La campagne s'est arrêtée à l'index 890 sur `Softsynth`.

    - Index 890 : vingt fichiers musicaux sans extension commencent par `53 59 4E 9B`, soit `SYN` suivi du séparateur ATASCII.
    - `BOOSTER` est une table brute de 256 octets sans extension ni en-tête distinctif. Sa suite de valeurs forme une courbe, mais elle ne fournit pas de signature générale suffisante pour déterminer seule son type ; ce fichier est reporté.
    - La campagne a repris à l'index 891 et s'est arrêtée à l'index 911 sur `SpartaDOS v1.1 HS [b]`.

    - Le volume `UTILS1` annonce une capacité de 184 320 octets alors que sa plage dans l'ATR ne contient que 183 936 octets, soit un dépassement de 384 octets.
    - Le média est marqué `[b]` dans le corpus. Il est reporté comme image incohérente sans assouplir la validation générale ni modifier le lecteur pour l'accepter.
    - La campagne a repris à l'index 912 et s'est arrêtée à l'index 922 sur `SpartaDOS v3.2g [m APE]`.

    - La capacité annoncée est de 16 776 960 octets, tandis que la plage disponible dans l'ATR contient 16 776 576 octets : le dépassement est encore de 384 octets.
    - Cette image modifiée par APE est reportée avec `SpartaDOS v1.1 HS [b]` pour reprendre globalement le calcul de capacité des ATR concernés.
    - La campagne a repris à l'index 923 et s'est arrêtée à l'index 925 sur `SpartaDOS v3.3b [m OS Ram]` avec le même dépassement de 384 octets.

    - Index 925 : `SpartaDOS v3.3b (199x)(-)[m OS Ram].atr`, volume `SPARTA`, capacité 184 320 octets dans une plage de 183 936 octets.
    - Aucun autre cas identique n'a été rencontré avant l'arrêt d'une autre nature à l'index 935 sur `Super 3D Plotter II`.

    - Index 935 : `PICTURE` contient 7 680 octets de données graphiques brutes, sans extension ni en-tête de format distinctif.
    - Index 936 : la version non crackée de `Super 3D Plotter II` contient le même fichier `PICTURE` et rejoint le même cas différé.
    - Index 967 : `Technicolor Dream (Side A)` contient un autre `PICTURE` graphique brut de 7 680 octets sans extension ni en-tête distinctif ; il rejoint ce cas différé.
    - Ce cas rejoint les autres images Atari 8 bits brutes sans signature déjà différées ; le nom et la longueur seuls ne sont pas employés pour inventer une reconnaissance.
    - La campagne a repris aux index 936 puis 968 après les occurrences identiques et s'est arrêtée à l'index 985 sur `The Trick - Cheat Maker`.

    - Index 985 : `DRED` (15 431 octets), `TRIC` (10 391 octets) et `HORROR.SP2` (15 488 octets) sont des données propriétaires du logiciel.
    - Aucun ne porte un en-tête Atari Binary Load ou une signature déjà connue. Leur reconnaissance demande l'analyse du format de données de `The Trick` ; le média est reporté sans règle fondée sur leurs noms ou leurs longueurs.
    - La campagne a repris à l'index 986 et s'est arrêtée à l'index 989 sur les modules `DOS.2` et `DOS.4` de Turbo DOS XE.

    - Index 989 et 998 : les variantes de `DOS.2` et `DOS.4` commencent par l'en-tête stable `01 03 00 07 40 15 4C 16 07 02`, tandis que leur fin varie entre les versions.
    - Ces modules binaires de Turbo DOS XE sont classés dans `System` par cet en-tête ; les extensions générales `.2` et `.4` ne deviennent pas des règles globales.
    - Les deux modules sont validés. La campagne s'est arrêtée à l'index 990 sur `Turbo Paint`.

    - Index 990 : `S` contient 7 680 octets de données graphiques brutes, sans extension ni en-tête distinctif ; il rejoint les autres images Atari 8 bits brutes différées.

    - Le volume `IDE_KMK` annonce 368 640 octets dans une plage de 368 256 octets, soit le même dépassement ATR de 384 octets déjà recensé.
    - `LDRACOS.MAE`, `MASTER.LOG`, `MS_SL.LOG` et `SLAVE.LOG` restent des données propriétaires non identifiées. Leur extension seule ne permet pas de leur attribuer une catégorie sûre dans ce contexte.

    - Index 1022 : `HELP.000`, puis `HELP.100` à `HELP.190`, contiennent des textes d'aide encodés en codes écran Atari après un en-tête variable.
    - Les extensions numériques ne définissent pas un type général et le catalogue ne possède pas encore de reconnaissance du texte en codes écran Atari ; ces onze fichiers sont reportés ensemble.

    - Index 1033 : `HELP.C10`, `HELP.C41` et `HELP.C91` sont des écrans d'aide en codes écran Atari ; ils rejoignent les écrans d'aide de `Video 130XE`.
    - `DISK` et `MASTER` sont des blocs graphiques propriétaires sans extension ni en-tête général identifiable ; ils rejoignent les images brutes différées.

    - Index 1044 : `MASTER.DIC` contient les données du dictionnaire de `The Writer's Tool` ; `.DIC` est classé comme fichier de données commun.
    - `MASTER.DIC` est validé. La campagne s'est arrêtée à l'index 1066 sur un ATX incomplet.

    - Index 1066 : `Atari Macro Assembler v1.0A (1981)(Atari)(US).atx`, 1 bloc manquant sur 720.
    - La campagne s'est arrêtée à l'index 1068 sur un ATX contenant des blocs à intégrité invalide.

    - Index 1068 : `The Music Studio`, 2 blocs invalides.
    - Index 1069 : `SynCalc (1983)`, 1 bloc invalide.
    - Index 1070 : `SynCalc (1985)`, 1 bloc invalide.
    - Index 1071 : `SynChron`, 1 bloc invalide.
    - La campagne s'est arrêtée à l'index 1072 sur un ATX auquel il manque 558 blocs logiques.

    - Index 1072 : `Text Wizard v1.3 (Side A)[OS-B]`, 558 blocs manquants sur 720.
    - Index 1073 : `Text Wizard v1.3 (Side B)[OS-B]`, 612 blocs manquants sur 720.
    - La campagne s'est arrêtée à l'index 1074 sur une tape reconnue sans fichier nommé extractible.

    - Index 1074 : le volume `Adax` est reconnu comme `sequential-content`, avec une capacité de 62 040 octets et un bloc décodé dépourvu de nom stocké.
    - Le lecteur signale `No named file is available for exploration`; aucun fichier nommé ne peut donc être présenté par l'explorateur. Ce cas est reporté sans créer de nom synthétique.

- [ ] Recenser et passer les autres tapes reconnues sans fichier nommé extractible.
  - [ ] Modifier `docs/tasks/media-corpus-audit.md` pour ajouter chaque index, média, volume, capacité et avertissement du lecteur.
  - [ ] Modifier `artifacts/media-audit/checkpoint.json` et supprimer `artifacts/media-audit/failure.json` après chaque erreur strictement limitée à l'absence de fichier extrait.
  - [ ] Modifier `artifacts/media-audit/items` et `artifacts/media-audit/checkpoint.json` en poursuivant jusqu'au prochain défaut d'une autre nature ou à la fin du corpus.

La campagne est terminée lorsque le script atteint la fin du corpus sans erreur et que `artifacts/media-audit/checkpoint.json` contient l'état `complete`.
