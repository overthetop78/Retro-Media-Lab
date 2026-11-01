# Reconstruction des présentations

État : travail en cours. Le dépôt de départ est `e2255446f`, avec Base et Studio validés. Aucune ancienne proposition Orbit/OS/Matrix/Atlas n’est reprise. Aucun nouveau thème ne sera déclaré fini sur la seule base d’un build ou de tests de navigation.

## Exigences communes

Le comportement métier reste celui de l’application : acquisition, écriture, conversion, rendu, exploration, outils, émulation et gestion de leurs configurations. La présentation complète change : entrée dans les fonctions, regroupement des paramètres, fenêtres, dialogues, menus, état des opérations, journaux, accès matériels et raccourcis. Les sept onglets, les cartes, la console et la barre d’état de Base ne constituent pas le gabarit des nouveaux thèmes.

Les extensions, les émulateurs et leurs fenêtres dynamiques font partie du périmètre. Les textes utilisent les ressources existantes ; les libellés nouveaux seront traduits dans toutes les langues avec Argos puis revus. Les noms invariants restent dans la base commune. Les différentes familles de médias doivent pouvoir être présentées sans figer l’interface autour des seules disquettes.

## Inventaire établi

L’inventaire initial, temporaire et désormais supprimé, recensait 64 vues XAML de Base, leurs contrôles nommés et événements, et 133 fichiers construisant des surfaces en code dans App et les modules. C’est une liste de contrôle initiale ; elle ne prouve pas qu’une fonction est correctement reconstruite. Les surfaces de rendu et les couleurs propres aux données vidéo doivent être distinguées des couleurs d’interface.

Fenêtres XAML identifiées :

- `src/GWGUI.App/Theme/Base/Views/Dialogs/Conversion/ConversionConflictWindow.xaml`
- `src/GWGUI.App/Theme/Base/Views/Dialogs/Explorer/ExplorerIssuesWindow.xaml`
- `src/GWGUI.App/Theme/Base/Views/Dialogs/Hardware/HardwareUnavailableWindow.xaml`
- `src/GWGUI.App/Theme/Base/Views/Dialogs/Profiles/ProfileNameWindow.xaml`
- `src/GWGUI.App/Theme/Base/Views/Dialogs/Read/ReadConflictWindow.xaml`
- `src/GWGUI.App/Theme/Base/Views/Windows/About/AboutWindow.xaml`
- `src/GWGUI.App/Theme/Base/Views/Windows/Conversion/FileMigrationWindow.xaml`
- `src/GWGUI.App/Theme/Base/Views/Windows/EmulationModuleOptions/EmulationModuleOptionsWindow.xaml`
- `src/GWGUI.App/Theme/Base/Views/Windows/EmulationPreferences/EmulationPreferencesWindow.xaml`
- `src/GWGUI.App/Theme/Base/Views/Windows/Logs/LogHistoryWindow.xaml`
- `src/GWGUI.App/Theme/Base/Views/Windows/Preferences/PreferencesWindow.xaml`
- `src/GWGUI.App/Theme/Base/Views/Windows/Shell/MainWindow.xaml`
- `src/GWGUI.App/Theme/Base/Views/Windows/Tools/GwToolWindow.xaml`
- `src/GWGUI.App/Theme/Base/Views/Windows/Updates/UpdatesWindow.xaml`

Les fenêtres créées en code comprennent notamment la sélection des firmwares, les options du cœur, la création et l’association des supports, les lecteurs cassette/disquette/disque dur/optique/cartouche/carte mémoire, ainsi que les commandes et réglages des machines. Elles devront être parcourues explicitement ; repeindre leur cadre ne les valide pas.

## Raccordement au métier

Les contrôleurs existants utilisent des champs, sélections, actions et surfaces de rendu déjà connectés. Ces éléments fonctionnels peuvent être présentés dans un nouvel arbre visuel sans reconstruire le métier. Le raccordement n’accepte ni les cartes, ni les grilles, ni les UserControl de Base comme élément de formulaire. Une zone documentaire ne doit accepter qu’une véritable surface de contenu ou de rendu.

Un emplacement thématique référence un élément fonctionnel et son rôle : entrée, action, information ou document. Son hôte conserve le contexte de données et la disponibilité venant du parent original. La visibilité d’un ancien conteneur de présentation ne doit pas réintroduire implicitement l’ancien découpage ; une dépendance métier nécessaire est déclarée explicitement. L’hôte ne devient jamais propriétaire du moteur ou du document emprunté.

Un prêt capture les parents et indices de tous les éléments avant de les détacher. Il est atomique, restaure leur disposition et leurs propriétés de présentation après fermeture ou erreur, et conserve leurs liaisons et événements. Le code ne duplique pas les commandes métier et n’emploie pas de réflexion sur les champs privés pour construire les vues livrées.

Les nouveaux thèmes fourniront leurs propres compositions pour les formulaires, leurs propres menus et leurs propres présentations du journal et de l’état. Réutiliser une entrée TextBox déjà reliée au contrôleur ne signifie pas réutiliser son ancien PathSection ou sa carte. Les surfaces de rendu restent reliées à leurs moteurs ; leurs commandes et leur entourage seront reconstruits.

## Identités distinctes à réaliser

| Thème | Organisation propre | Éléments à reconstruire |
|---|---|---|
| OS | Interface et interactions du système historique choisi | Gestion des fenêtres, applications/groupes, menus, dialogues, typographie, icônes, contrôles et états selon chaque système ; déclinaison séparée de l’apparence |
| Orbit | Émulation comme espace principal, outils médias contextuels autour de la machine et des documents | Commandes contextuelles, accès aux opérations, formulaires orientés tâche, états et journal intégrés à la tâche ; aucun ruban des sept onglets Base |
| Matrix | Espace de travail terminal à panneaux textuels, commandes et sélection au clavier | Navigation, formulaires, listes, messages et journal comme interface de terminal cohérente ; pas de boutons Base simplement entourés de crochets |
| Atlas | Atelier documentaire regroupant les opérations sur un support et ses documents | Présentation du support, séquences lecture/écriture/conversion, exploration et inspection coordonnées ; menus, réglages, fenêtres et journal propres à cet atelier |

Ces identités reprennent le périmètre des quatre thèmes refusés, pas leur code ni leur composition. Elles devront être matérialisées et comparées sur les mêmes fonctions avant validation.

## Fidélité OS

Windows 3.1 est la première référence précise. Source primaire consultée : Microsoft, *Getting Started with Microsoft Windows*, avril 1992, document PC21670-0492, chapitre 3, pages imprimées 27–50 ([scan](https://bitsavers.trailing-edge.com/pdf/microsoft/windows_3.1/Microsoft_Windows_3.1/PC21670-0492_Windows_3.1_Getting_Started_Apr92.pdf)). Le manuel utilisateur complet est également disponible dans [le même fonds](https://bitsavers.trailing-edge.com/pdf/microsoft/windows_3.1/Microsoft_Windows_3.1/).

Points relevés dans cette référence : groupes du Gestionnaire de programmes ; lancement par double-clic sur une icône ; menus sous le titre ; activation en cliquant dans une fenêtre ; réduction d’une application en icône ; fermeture par double-clic sur la case système ou par son menu. Ces comportements constituent des exigences, pas une décoration.

Contrôles encore nécessaires pour Windows 3.1 : palette exacte, mesures à résolution de référence, police système et glyphes, contrôles et états actifs/inactifs, flèches, reliefs, sélection/focus, menu système, clavier et fenêtres modales. Aucun remplacement approximatif de police ou d’icône ne devra être présenté comme une fidélité exacte.

Les références précises des autres déclinaisons (Windows 95, Windows XP, macOS, Atari GEM et AmigaOS) restent à fixer et consulter. Leurs comportements ne seront pas déduits arbitrairement de ceux de Windows 3.1. Une déclinaison ne se résume pas à une palette. Les éventuelles variantes clair/sombre doivent rester séparées et ne pas être confondues avec un rendu historique de référence.

## Preuves attendues avant achèvement

- Pour chaque thème : acquisition brute et formatée, écriture, conversion multi-sorties, visualisation chargée, exploration chargée, outils et émulation en marche.
- Pour chaque fenêtre et dialogue inventoriés : accès, champs, validation, erreurs, fermeture et raccourcis accessibles ; ne rien omettre d’un module.
- Pour OS : comparaison visuelle et interaction par interaction avec la référence concernée. Les différences non résolues restent indiquées comme travail restant.
- Navigation à la souris et au clavier ; dimensions usuelles jusqu’à 1080p, textes longs et langues avec écriture de droite à gauche ; pas de dépassement ni de champ tronqué.
- Retour à Base et Studio avec données, commandes, sélection, liaisons et disposition conservées ; fermeture et libération effective des fenêtres, surfaces et cœurs possédés dans finally.
- Captures réelles de plusieurs fonctions et dialogues conservées dans artifacts. Sources et sessions temporaires supprimées. Build Debug officiel tous modules et vérification des 24 manifestes/DLL. Commits préparés, jamais créés sans validation.

## Première vue OS : acquisition

Le nouveau formulaire est construit dans `Theme/OS/Editions/Windows31/Views/AcquisitionView.xaml`, avec ses propres contrôles. Son document central est la liste des captures ; la préparation de l’acquisition est disposée en bas, avec destination, mode et profil. L’historique est alimenté par les données observables de lecture, jamais par les ReadCaptureCard de Base. Les actions de consultation sont celles déjà enregistrées dans l’application.

Les champs nouvellement créés sont synchronisés avec les valeurs fonctionnelles existantes par `ThemeBindingScope`, sans déplacer les widgets Base ni remplacer leurs liaisons au modèle. Le retour des changements programmatiques n’est pas traité comme une nouvelle saisie utilisateur, notamment lors du repeuplement des listes dépendantes. Les sources de rendu nécessitant de conserver un moteur graphique pourront utiliser le prêt de surfaces ; cette possibilité ne sert pas à reprendre les cartes ou formulaires Base.

Ce formulaire constitue une étape de raccordement fonctionnel. Il n’est pas encore enregistré dans le sélecteur de thèmes. Son cadre historique, ses polices exactes, ses menus et son dialogue avancé doivent être réalisés avant sa mise à disposition. Il ne constitue pas une preuve de fidélité Windows 3.1.

## Typographie et interactions historiques : état des preuves

Les fichiers bitmap `sserife.fon`, `vgasys.fon` et `vgafix.fon` sont réellement présents dans les polices Windows de cet hôte. Leurs tables de ressources indiquent respectivement MS Sans Serif (six tailles), System et Fixedsys. Les métriques ont été vérifiées ; le rapport temporaire a été supprimé. À titre d’observation : la ressource MS Sans Serif de 8 points mesure 13 pixels de hauteur, la ressource System 16 pixels, et Fixedsys 15 pixels. Aucun fichier de police n’est copié ni converti à ce stade. Ces observations prouvent la disponibilité de ressources bitmap ; elles ne prouvent pas leur identité avec celles d’une installation Windows 3.1 d’origine.

La reproduction devra donc contrôler les glyphes et métriques réellement utilisés ; substituer Tahoma, Segoe UI ou une police TrueType ressemblante ne satisfait pas la demande de fidélité. Les textes traduits et les alphabets absents d’une ressource historique devront être traités explicitement, pas tronqués ou remplacés silencieusement par des carrés.

Un bouton de réduction Windows 3.1 doit produire une icône d’application selon le comportement de cette version ; déléguer simplement la réduction à la barre des tâches de Windows 11 ne suffit pas. De même, les différences entre déplacement en contour, déplacement du contenu, activation, menu système et modalités doivent être vérifiées et respectées. Un gestionnaire unique imposant à tous les systèmes les commandes Windows Minimize/Maximize ne convient pas.

Autres références primaires identifiées, à lire et versionner avant de valider les déclinaisons :

- Microsoft Press / Microsoft Developer Network, *Programming the Windows 95 User Interface*, 1995 ([scan](https://bitsavers.trailing-edge.com/pdf/microsoft/windows_95/Programming_the_Windows_95_User_Interface_1995.pdf)).
- Commodore, *Workbench 3.0 User’s Guide* ([scan](https://www.retromaniek.pl/wp-content/uploads/2019/04/Commodore_A4000_Users_Guide_Workbench_3.0.pdf)). Cette version ne constitue pas, à elle seule, une preuve pour tous les détails de Workbench 3.1.
- Apple, guide utilisateur macOS : les pages courantes dépendent de la version. La version et les captures de référence devront être fixées avant de reproduire le cadre, les boutons et les menus ; la page courante macOS 27 ne prouve pas le comportement de Ventura ou d’une version classique.
- Les sources primaires Atari TOS/GEM et Windows XP restent à identifier précisément. Les guides communautaires ne seront pas traités comme une spécification officielle exhaustive.

## Architecture des prochains écrans OS

La composition des vues et la gestion des fenêtres sont des responsabilités distinctes. Les nouvelles vues utilisent les données et commandes existantes, avec des champs entièrement nouveaux. Le cadre, les menus système et les interactions devront relever de la déclinaison historique et non des contrôles Windows modernes par défaut. Le même comportement de réduction, zoom, profondeur ou plein écran ne sera pas imposé arbitrairement à toutes les déclinaisons.

Les prochains fichiers seront définis dans le suivi avant implémentation : bureau et gestionnaire d’applications, cadre Windows 3.1, menu système, sélection de fichiers et répertoires, options d’acquisition, écriture/conversion, consultation, émulation, outils, réglages et messages. L’inventaire des fenêtres en code fait partie de cette reconstruction, pas d’une simple recoloration globale.

## Formulaire indépendant des options d’acquisition

`Theme/OS/Editions/Windows31/Views/AcquisitionOptionsView.cs` compose une grille de paramètres de capture et un panneau de numérotation, avec des entrées nouvelles. `AcquisitionOptionsFields.cs` identifie explicitement les points de raccordement existants. Chaque option conserve ses notifications, règles d’exclusion et traductions via sa source fonctionnelle ; les listes littérales fournissent de nouveaux éléments, jamais les ComboBoxItem de Base. Le formulaire est détaché dans Dispose ; son propriétaire futur devra le libérer dans finally. Il reste hors du sélecteur jusqu’à la réalisation du cadre et des contrôles historiques.

## Fichiers de réalisation OS — ordre de construction

Tous les chemins suivants sont relatifs à `src/GWGUI.App/Theme/OS/`. Les sources Base ne deviennent jamais le contenu visuel des nouvelles fenêtres.

1. `Editions/Windows31/Windows31WindowState.cs`, `Windows31WindowPlacement.cs` : états normal, agrandi, réduit en icône et fermé ; géométrie restaurée, déplacement/redimensionnement validés et activation. Les transitions sont propres à Windows 3.1, sans appel à la barre des tâches Windows actuelle.
2. `Editions/Windows31/Windows31Frame.xaml` et `.xaml.cs`, `Windows31Controls.xaml`, `Windows31Metrics.cs` : cadre sans bouton Fermer moderne, case système, commandes de réduction/agrandissement, menus et contrôles historiques ; tracé et typographie comparés aux références. Les polices historiques restent un point ouvert explicite.
3. `Editions/Windows31/Windows31Desktop.cs`, `Windows31ProgramManager.xaml` et `.xaml.cs` : bureau, icônes d’applications réduites, groupes et ouverture des outils ; restaurer une icône réactive son application existante.
4. `Editions/Windows31/Views/{WriteView,ConversionView,InspectorView,ExplorerView,ToolsView,EmulationView}.xaml` et fichiers associés : nouveaux formulaires, commandes et listes ; les surfaces vidéo/document sont empruntées avec restitution dans finally, pas leurs anciens blocs de commande.
5. `Editions/Windows31/Dialogs/{FilePickerView,PreferencesView,MachineConfigurationView,StorageView,MessageView}.xaml` et fichiers associés : composition de chaque dialogue inventorié, variantes selon les données et modules ; aucun formulaire Base inséré dans un cadre historique.
6. `OSWorkspace.cs`, `OSEdition.cs`, `OSDefinition.cs` : raccordement au point d’entrée de thèmes existant, gestion des propriétaires et sélection interne des déclinaisons. Enregistrer OS seulement après validation de la première déclinaison complète.
7. `Editions/{Windows95,WindowsXP,MacOS,AtariGem,AmigaOS}/` : chacune possède `WindowFrame`, `Controls`, `Desktop` et ses vues/dialogues. Lire d’abord la version de référence puis détailler les fichiers de chaque déclinaison dans le suivi avant de les créer ; ne pas dériver son comportement de Windows 3.1 par recoloration.

Pour les autres thèmes, le périmètre reste l’ensemble de l’inventaire. Les dossiers `Theme/Orbit/`, `Theme/Matrix/` et `Theme/Atlas/` auront chacun leur espace, leurs vues d’opérations, menus, journal, états et dialogues ; leurs listes de fichiers seront établies après la composition détaillée correspondante. Ils ne sont pas supprimés du travail attendu pendant la réalisation OS.

La palette utilise des valeurs VGA explicites. Le gris clair VGA (192, 192, 192) ne correspond pas à `System.Windows.Media.Colors.LightGray` (211, 211, 211) ; la confusion constatée dans le premier cadre a été corrigée avant livraison. Référence primaire : Microsoft, article KB65661, table de la palette Windows 16 couleurs ([archive Microsoft](https://ftp.zx.net.nz/pub/Patches/ftp.microsoft.com/MISC/KB/en-us/65/661.HTM)). Les mesures et autres couleurs des cadres restent à comparer aux captures d’époque.

## Texte bitmap et surfaces natives — fichiers de réalisation

Les contrôles WPF conservent leurs interactions de saisie et sélection. Le texte doit employer les métriques et glyphes bitmap historiques, sans substitution par Segoe UI ou Tahoma. Les fichiers prévus sont un convertisseur temporaire (retiré après conversion contrôlée des ressources raster en contours de pixels), `Theme/OS/Editions/Windows31/Assets/Fonts/` (faces nécessaires), `Windows31Typography.xaml` (familles, tailles et repli explicite pour les alphabets absents), et la déclaration de ces ressources dans `GWGUI.App.csproj`. La source primaire du format est Microsoft, *Programmer’s Reference, Volume 4: Resources*, avril 1992, chapitre 4 ([scan SDK](https://bitsavers.trailing-edge.com/pdf/microsoft/windows_3.1/Microsoft_Windows_3.1_SDK_1992/PC30211-0492_Programmers_Reference_Volume_4_199204.pdf)), complétée par [KB65123](https://ftp.zx.net.nz/pub/Patches/ftp.microsoft.com/MISC/KB/en-us/65/123.HTM). La disponibilité des polices locales ne prouve toujours pas leur identité avec les disquettes d’origine : cette comparaison reste un critère d’achèvement.

Précision utilisateur après les premières captures : toutes les applications de cette déclinaison s’ouvrent à l’intérieur du bureau RML, en fenêtre ou agrandies dans ce bureau. L’hébergement autonome dans Windows31ApplicationWindow.cs est retiré. Windows31Desktop.cs gère les cadres internes, leur activation, leur réduction en icône et leur restauration. Le Gestionnaire de programmes est lui-même une fenêtre du bureau et contient des groupes d’icônes ; le double-clic ouvre les applications. La composition et la fidélité visuelle des deux premiers formulaires ont été refusées et doivent être reprises.

Les trois captures utilisateur sont désormais les références de composition : menus sous la barre de titre, case système à gauche, commandes de réduction et d’agrandissement à droite, groupes ouverts ou réduits en icônes, applications superposées avec leur propre menu. L’agrandissement remplit le bureau de l’application, jamais le bureau Windows hôte. Le bureau global est gris ; les grandes surfaces blanches inutiles et commandes tassées du premier formulaire ne constituent pas un modèle validé.

Le raccordement natif conserve les HwndHost existants de tous les modules et moteurs : aucun moteur vidéo n’est remplacé et aucune dépendance à Acorn n’est introduite. `Theme/Common/ThemeNativeWorkspaceHost.cs` suit les surfaces dans le bureau et leurs couches ; `ThemeNativeWindowRegion.cs` limite leur région de dessin aux parties réellement visibles. Les fenêtres, menus et contours WPF devant la vidéo déterminent les exclusions ; un contour creux laisse son intérieur visible. Les coordonnées sont converties vers les pixels de l’écran pour respecter le DPI.

Ce choix évite de réhéberger tout le cadre dans un autre HWND et de modifier les chemins de saisie existants. Les clics natifs sont observés avant leur distribution avec `ComponentDispatcher.ThreadFilterMessage`, puis transmis normalement. Une région originale est sauvegardée et restaurée au détachement ; le thème libère ses HRGN, jamais le HwndHost, le HWND vidéo ou le moteur empruntés. Après réussite de SetWindowRgn, Windows possède la région transmise : elle ne doit pas être supprimée par le thème. Références primaires : [interop WPF/Win32](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/wpf-and-win32-interoperation), [GetWindowRgn](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowrgn), [SetWindowRgn](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowrgn). Les contrôles natifs temporaires doivent vérifier recouvrement, restauration et absence de résidus avant enregistrement OS.

Le bureau n’établit pas de nouvelle logique métier : sa fabrique de documents fournira les nouvelles présentations reliées aux commandes existantes. Les menus, icônes, déplacements en contour et dialogues modaux restent à réaliser et vérifier avant d’enregistrer OS. Les gabarits d’onglets et d’arbres servent aux contrôles applicatifs nécessaires ; leur présence n’est pas une affirmation que ces contrôles étaient des contrôles natifs de Windows 3.1.

## Ressources originales Windows 3.1 retenues

Les icônes répétées provisoires sont remplacées par des ICO extraites des exécutables de l’installation Windows 3.10 datée du 10 mars 1992, archivée par [PCjs](https://www.pcjs.org/software/pcx86/sys/windows/3.10/), image `PCDOS330-WIN310-VGA.json`. Les fichiers reconstruits à partir des secteurs sont vérifiés contre les empreintes de l’archive ; les ressources NE sont lues sans charger ni exécuter les programmes. Les ICO conservent les pixels et masques 32×32 d’origine. Les choix et empreintes utiles sont consignés ci-dessous ; le manifeste temporaire est supprimé.

| Fonction RML | Ressource d’origine |
|---|---|
| Lecture | WINFILE, groupe 202 |
| Écriture | WRITE, MW_ICON |
| Conversion | PACKAGER, groupe 1 |
| Visualisation | PBRUSH, PBRUSH |
| Explorateur | WINFILE, groupe 200 |
| Outils | PIFEDIT, groupe 1 |
| Émulation | TERMINAL, groupe 255 |
| Préférences | CONTROL, groupe 1001 |
| Préférences d’émulation | MAIN.CPL, groupe 29 |
| Gestionnaire et groupes | PROGMAN, groupe 1 |

Les neuf icônes d’outils sont distinctes. Les groupes utilisent l’icône de groupe réelle de PROGMAN, comme dans les captures de référence. Les fonctions RML ne deviennent pas les logiciels d’origine dont leurs icônes sont issues.

Les polices sont également régénérées depuis SSERIFE.FON, VGASYS.FON et VGAFIX.FON de cette installation de 1992 ; la version installée sur l’hôte moderne n’est plus la source des ressources incorporées. Les rapports de conversion et empreintes permettent de contrôler les glyphes et métriques retenus.

## Conversion indépendante et répertoire de caractères

La nouvelle application de conversion fournit ses propres menus et ses propres listes : famille en liste déroulante, formats disponibles, détails et extensions du format courant, récapitulatif des sorties. Les sélections passent par `ConversionFormatPresentation`, déjà utilisé par le présentateur ; le contrôleur ne demande plus un contrôle Base comme expéditeur de l’événement. Le chemin historique de Base utilise cette même requête, vérifiée avec son vrai contrôle et le même contrôleur.

Les paramètres avancés des trois opérations utilisent `Windows31BoundFields<TField>` : seuls des champs neufs sont construits, reliés aux valeurs et événements existants. Les conteneurs et contrôles Base restent dans leurs parents. Cette réutilisation concerne la déclinaison Windows 3.1, pas la composition des autres thèmes.

L’inspection des captures a révélé une déclaration incorrecte de certains caractères C1 : dans les ressources de 1992, leur dessin est exactement celui du caractère manquant. Les annoncer comme des points de suspension ou un tiret Unicode empêchait le repli prévu et produisait des barres. Le convertisseur omet maintenant ces correspondances. La correction des polices déjà converties actualise leurs tables Unicode uniquement ; toutes les autres tables de glyphes et métriques sont comparées avant/après et restent identiques. Les rapports temporaires ont été supprimés après vérification ; les empreintes des fichiers incorporés sont consignées ci-dessous.

Les captures de conversion à 860×580 et 600×480 sont inspectées. Le profil et la commande principale sont ancrés hors du défilement ; les listes conservent leur défilement propre. L’ancien libellé français Prolongation était incorrect pour une extension de fichier : `Read.Extension` a été retraduit avec ce contexte via Argos dans toutes les langues puis revu. Ces captures restent des états de progression ; le thème OS complet n’est pas encore proposé.

## Palettes Windows 3.1

Les 22 entrées originales de CONTROL.INI sont conservées dans `Assets/ColorSchemes.json`, avec les COLORREF BGR et leurs indicateurs hauts intacts. L’archive est celle déjà utilisée pour les icônes et polices ; ses empreintes et celles de CONTROL.INI / MAIN.CPL figurent dans le JSON. L’ordre des 21 couleurs est confirmé par la table WORD de MAIN.CPL à l’offset 17304, comparée intégralement aux indices système. Aucun programme historique exécuté. Windows Default, absent de la liste du fichier, conserve la palette actuelle de la présentation afin de ne pas changer son rendu de référence.

Source : https://harddisks.pcjs.org/pcx86/20mb/PCDOS330-WIN310-VGA.json . Les noms historiques des palettes restent invariants en base commune ; les libellés du sélecteur restent traduits. Le choix doit apparaître dans le sélecteur de thèmes au même endroit que Clair/Sombre, avec conservation par déclinaison.

## Ressources incorporées après nettoyage des outils temporaires

Les scripts de conversion/extraction, leurs caches et les rapports de travail sont supprimés. Les visuels de progression sont regroupés dans `artifacts/themes-review/screens/OS/Windows31/`.

| Ressource | SHA-256 du fichier incorporé |
|---|---|
| `Fonts/FixedVGA.ttf` | `bff5ddf6d31ddc0b15a81290a146f273bc5f3a1b4f9c53a099344720e40c22ab` |
| `Fonts/SansVGA.ttf` | `5e12ffcc80984bc8adb910466ae4adcbb102e367ff678b477f75d0ee85c90fed` |
| `Fonts/SystemVGA.ttf` | `053029d213b6a4a62204f4d8e20037ce4b1846bb5f3c6367d96c71a50e6e8448` |
| `Icons/conversion.ico` | `3dd8e591fd6abe64e0539b6936d868241f91d13add64cbc5c340a50da56b3ae4` |
| `Icons/emulation-preferences.ico` | `c965de2b32346115ca9f9e73c6e45470198f0ad78cdbb058ac6dcef9f5df2b53` |
| `Icons/emulation.ico` | `ff3d6a7d84939a9142a3fac3c2e314f7f2392db9112ee16e8d20c04122eeaf09` |
| `Icons/explorer.ico` | `8968e3f51dd28780f00faee23673dfdd78b1c2bae5994fa9ee9d39bf0711539c` |
| `Icons/group.ico` | `58557cceb9e9810bfcf90754452cd8bed27c1a7f0c2a6a40abe294425303edc2` |
| `Icons/preferences.ico` | `bdadd9e43a6935ba8b216b17a1cfb7515f6925fded3add43011340de253bd2bf` |
| `Icons/read.ico` | `6c26eebf4dec466cdeb2269f721eed457fa32ee161964e8a1a9566ab40cfd050` |
| `Icons/tools.ico` | `c041895fef3d58ada8c263ef3ea994dfcf1ad0c99cabd04d61b77cf312241f0f` |
| `Icons/visualization.ico` | `8eef6a1ba7a617ce42c37d1dd382c05299fc5f479799bad7b48976ee4eb7abed` |
| `Icons/write.ico` | `c62f181cb13efb8ad719f7fd5e1d064ffac10c250ae870c98d35ceb35d09ca8d` |

Archive primaire SHA-256 : `5eecfba38d881f48ab75b4b5e19c5420518836870483f5725aba9564a86fe4c3`. Les ressources de palette JSON incorporées sont des données nécessaires à l’application, conservées dans le thème ; elles ne sont pas des rapports temporaires de artifacts.

## Pictogrammes des entrées de l’explorateur

Le circuit existant est conservé : FileSystemEntry.Analysis.IconId → ExplorerFilePresentation → ExplorerIconCategory → FileEntryIcon. Le fournisseur d’images du thème ne reconnaît aucun fichier et ne modifie aucune catégorie. Hors de Windows 3.1, le contrôle conserve ses ICO et ses couleurs de Base.

La planche incorporée `Assets/Images/FileManager.bmp` provient de RT_BITMAP 100 de WINFILE.EXE du 10 mars 1992, dans la même archive PCjs Windows 3.10. WINFILE.EXE : SHA-256 `fb20b6f0723f1ccd96fc65a6644ca0db450838a4e7151cfd40057c30914e77e7`. DIB original : `a75f8535f13b6871ca2e4586bc64e254fa04ee18850e2fea2fb1b50c80ad4e11`. BMP incorporé avec en-tête BMP : `a47609903fc528a95967300e9d99e0f23e751698c3a2c668103394174517e9aa`.

La planche 208×46 contient les cellules de 16×16 utilisées pour programme (colonne 0), document (1), fichier (2), dossier (5) et dossier ouvert (7). Le bleu de fond réservé à la couleur de fenêtre devient transparent au chargement afin que les fonds et sélections de la palette restent visibles ; tous les autres pixels sont conservés. Le résultat est figé et partagé. Aucun dessin synthétisé ni nouvelle dépendance d’image ajoutée.

## Présentations des machines et moteurs conservés

Le contrôleur vidéo et celui des entrées dépendent maintenant des points de `EmulationMachinePresentation` : hôte d’affichage, écran, surface vidéo et présentation des périphériques. La présentation Base les implémente sans changement de disposition. Le contrôleur de machine reste propriétaire du cœur et de sa présentation ; après arrêt, son bloc finally libère également le timer et les références de la présentation.

Cette séparation prépare une présentation indépendante Windows 3.1 pour toutes les machines. Elle ne constitue pas encore le raccordement des fenêtres de machines au bureau OS. Les surfaces GPU/WPF, calculs de ratio, captures, shaders, commandes d’entrée et règles des médias restent ceux de leurs services existants. Les commandes et périphériques Windows 3.1 seront reconstruits dans leur propre vue ; le panneau Base ne doit pas être déplacé dans une fenêtre OS. Le changement de thème devra aussi conserver la session et restituer ses surfaces.

La console utilise désormais l’icône propre de TERMINAL.EXE du 10 mars 1992, extraite sans exécution de la même archive PCjs Windows 3.10. Source : https://harddisks.pcjs.org/pcx86/20mb/PCDOS330-WIN310-VGA.json . TERMINAL.EXE SHA-256 : `6cf539af3ce4c1a38455856ba0b6b41b52b8357bbd29e193d606db480ed1ef65`. `Icons/terminal.ico` SHA-256 : `fb178ecb40e92c529842a6ed5989d08a8a9e4073116a2193145bf24a15ae1353`.


## Icônes des messages Windows 3.1

Les quatre icônes proviennent des groupes RT_GROUP_ICON 32513 à 32516 de `WINDOWS/SYSTEM/VGA.DRV`, daté du 10 mars 1992, dans l’image PCjs déjà retenue : <https://harddisks.pcjs.org/pcx86/20mb/PCDOS330-WIN310-VGA.json>. Le pilote est reconstruit en mémoire depuis les secteurs attribués au fichier ; les secteurs de remplissage au-delà de sa taille sont exclus. Son MD5 correspond à la table de l’archive (`ab9c8cc0a088e3b09b5e3af12cd5bb18`) ; son SHA-256 est `53312ed013bda513c74e33659f4ee7d1b4f2b1cf1a06aed672432a8fbc4a820a`. Aucun programme historique n’est exécuté.

Chaque ICO conserve les trois images 32×32 originales du groupe : 8 couleurs, monochrome et 16 couleurs, avec leurs masques. Seuls les en-têtes de répertoire ICO sont reconstruits ; les DIB sont copiés à la longueur exacte inscrite dans le groupe, sans le remplissage d’alignement NE.

| Fichier dans `Theme/OS/Editions/Windows31/Assets/Icons/` | Groupe | SHA-256 |
|---|---:|---|
| `message-error.ico` | 32513 | `6469144cc8391a40d7fbc2d0620b8dff1abf35cdbfe38cd1ead07ff175e001b0` |
| `message-question.ico` | 32514 | `6f3e0dd51ca49e510736d56d21aabcb720e51096d99ce2772c38309c2487652e` |
| `message-warning.ico` | 32515 | `fb6d256e0c064083654cb3163517509c8f2d5ff9f0caadfe8c5d28f1947f7663` |
| `message-information.ico` | 32516 | `bc8446c1da29067873e9dc3cea8ebd62a8ad9219a47cfe213d164676563f0f56` |

L’image JSON d’extraction temporaire est supprimée après reconstruction.
