# Reconstruction des présentations

État : travail en cours. Le dépôt de départ est `e2255446f`, avec Base et Studio validés. Aucune ancienne proposition Orbit/OS/Matrix/Atlas n’est reprise. Aucun nouveau thème ne sera déclaré fini sur la seule base d’un build ou de tests de navigation.

## Exigences communes

Le comportement métier reste celui de l’application : acquisition, écriture, conversion, rendu, exploration, outils, émulation et gestion de leurs configurations. La présentation complète change : entrée dans les fonctions, regroupement des paramètres, fenêtres, dialogues, menus, état des opérations, journaux, accès matériels et raccourcis. Les sept onglets, les cartes, la console et la barre d’état de Base ne constituent pas le gabarit des nouveaux thèmes.

Les extensions, les émulateurs et leurs fenêtres dynamiques font partie du périmètre. Les textes utilisent les ressources existantes ; les libellés nouveaux seront traduits dans toutes les langues avec Argos puis revus. Les noms invariants restent dans la base commune. Les différentes familles de médias doivent pouvoir être présentées sans figer l’interface autour des seules disquettes.

## Inventaire établi

L’inventaire initial, temporaire et désormais supprimé, recensait 64 vues XAML de Base, leurs contrôles nommés et événements, et 133 fichiers construisant des surfaces en code dans App et les modules. C’est une liste de contrôle initiale ; elle ne prouve pas qu’une fonction est correctement reconstruite. Les surfaces de rendu et les couleurs propres aux données vidéo doivent être distinguées des couleurs d’interface.

Fenêtres XAML identifiées :

- `src/RetroMediaLab.App/Theme/Base/Views/Dialogs/Conversion/ConversionConflictWindow.xaml`
- `src/RetroMediaLab.App/Theme/Base/Views/Dialogs/Explorer/ExplorerIssuesWindow.xaml`
- `src/RetroMediaLab.App/Theme/Base/Views/Dialogs/Hardware/HardwareUnavailableWindow.xaml`
- `src/RetroMediaLab.App/Theme/Base/Views/Dialogs/Profiles/ProfileNameWindow.xaml`
- `src/RetroMediaLab.App/Theme/Base/Views/Dialogs/Read/ReadConflictWindow.xaml`
- `src/RetroMediaLab.App/Theme/Base/Views/Windows/About/AboutWindow.xaml`
- `src/RetroMediaLab.App/Theme/Base/Views/Windows/Conversion/FileMigrationWindow.xaml`
- `src/RetroMediaLab.App/Theme/Base/Views/Windows/EmulationModuleOptions/EmulationModuleOptionsWindow.xaml`
- `src/RetroMediaLab.App/Theme/Base/Views/Windows/EmulationPreferences/EmulationPreferencesWindow.xaml`
- `src/RetroMediaLab.App/Theme/Base/Views/Windows/Logs/LogHistoryWindow.xaml`
- `src/RetroMediaLab.App/Theme/Base/Views/Windows/Preferences/PreferencesWindow.xaml`
- `src/RetroMediaLab.App/Theme/Base/Views/Windows/Shell/MainWindow.xaml`
- `src/RetroMediaLab.App/Theme/Base/Views/Windows/Tools/GwToolWindow.xaml`
- `src/RetroMediaLab.App/Theme/Base/Views/Windows/Updates/UpdatesWindow.xaml`

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

Tous les chemins suivants sont relatifs à `src/RetroMediaLab.App/Theme/OS/`. Les sources Base ne deviennent jamais le contenu visuel des nouvelles fenêtres.

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

Les contrôles WPF conservent leurs interactions de saisie et sélection. Le texte doit employer les métriques et glyphes bitmap historiques, sans substitution par Segoe UI ou Tahoma. Les fichiers prévus sont un convertisseur temporaire (retiré après conversion contrôlée des ressources raster en contours de pixels), `Theme/OS/Editions/Windows31/Assets/Fonts/` (faces nécessaires), `Windows31Typography.xaml` (familles, tailles et repli explicite pour les alphabets absents), et la déclaration de ces ressources dans `RetroMediaLab.App.csproj`. La source primaire du format est Microsoft, *Programmer’s Reference, Volume 4: Resources*, avril 1992, chapitre 4 ([scan SDK](https://bitsavers.trailing-edge.com/pdf/microsoft/windows_3.1/Microsoft_Windows_3.1_SDK_1992/PC30211-0492_Programmers_Reference_Volume_4_199204.pdf)), complétée par [KB65123](https://ftp.zx.net.nz/pub/Patches/ftp.microsoft.com/MISC/KB/en-us/65/123.HTM). La disponibilité des polices locales ne prouve toujours pas leur identité avec les disquettes d’origine : cette comparaison reste un critère d’achèvement.

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


## Windows 95 — déclinaison suivante de OS

Références : le [guide Microsoft de février 1995](https://www.aoisnow.net/blog/wp-content/uploads/2019/10/Microsoft_WindowsGuidelines.pdf), chapitres 3 et 6, décrit bureau, barre des tâches, activation et commandes des fenêtres. Ce document est une préversion : le rendu final doit être comparé à [Windows 95 RTM 4.00.950](https://www.pcjs.org/software/pcx86/sys/windows/win95/4.00.950/), pas déduit uniquement de ses illustrations.

La déclinaison remplacera le gestionnaire de programmes par un bureau avec lancement et barre des tâches. Ses fenêtres utiliseront les états natifs WPF ; leur contenu conservera les commandes, données et services RML. Le premier lot concerne le cadre. Les ressources historiques, le bureau et toutes les vues doivent être terminés avant l’ajout au sélecteur. Le build Windows 3.1 livré reste inchangé pendant cette réalisation.


## Windows XP : contrôles et composition

Windows XP constitue une déclinaison du thème OS. Les palettes Luna bleu, argent et olive sont des styles internes à cette déclinaison. Microsoft fournit les dictionnaires WPF `/PresentationFramework.Luna;component/themes/Luna.NormalColor.xaml`, `Luna.Metallic.xaml` et `Luna.Homestead.xaml` ; l’assembly PresentationFramework.Luna est présent dans le runtime .NET Desktop installé. Référence primaire : https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/styles-templates-overview#using-built-in-themes (table des thèmes intégrés).

Ces dictionnaires reproduisent les contrôles communs ; ils ne fournissent ni le menu Démarrer XP, ni son bureau, ni les cadres non clients historiques. Ces éléments seront construits séparément à partir des ressources et références XP. Leur présence dans WPF ne constitue donc pas une preuve que la déclinaison XP est terminée.

La composition prévue reste celle d’un bureau XP : menu Démarrer à deux colonnes, barre des tâches, fenêtres natives indépendantes et propriétés de type panneau de configuration. Les fonctions métiers et les surfaces de rendu restent aux propriétaires existants. Les vues d’acquisition, d’exploration et d’émulation seront raccordées explicitement ; aucune page Base ne sera affichée telle quelle et Windows 95 ne sera pas recoloré globalement.


### Recherche des ressources Luna originales

Le candidat `pazuzu156/KalebKlein/Windows XP Theme Installer/Themes/Luna/Luna.msstyles` (SHA-256 `fc76fbb00275ce12e433d10027a9b2b9a6e922afb9ab0113c2123bae012aeb85`) est écarté : son arbre de ressources contient IMAGE/VARIANT/STREAM/BCMAP et ne contient ni les bitmaps ni les paramètres TEXTFILE attendus du format XP. Il n’est donc pas utilisé pour représenter les cadres historiques XP. Le fichier a été lu en mémoire uniquement ; aucun binaire ni image n’a été conservé.


### Luna du média XP SP3 : ressources retenues comme référence

Source : `I386/LUNA.MS_` du média `en_windows_xp_professional_with_service_pack_3_x86_cd_vl_x14-73974.iso`, archive WinXPProSP3x86. Seuls 377 943 octets du fichier compressé ont été extraits par plages HTTP ; le média complet n’est pas téléchargé. Décompression de données avec expand.exe, sans exécution de Luna. SHA-256 de Luna.msstyles : `dd8e86e5a7c19ea9fe0e34992babefe592055d68eb8659763c8300558ca5dcb4`.

| Ressource bitmap | Largeur | Hauteur des états | Bits |
|---|---:|---:|---:|
| BLUE_CAPTIONBUTTON_BMP | 21 | 168 | 32 |
| BLUE_CLOSEBUTTON_BMP | 21 | 168 | 32 |
| BLUE_FRAMEBOTTOM_BMP | 49 | 10 | 8 |
| BLUE_FRAMECAPTIONMIN_BMP | 66 | 58 | 8 |
| BLUE_FRAMECAPTIONSIZING_BMP | 66 | 58 | 1 |
| BLUE_FRAMECAPTION_BMP | 66 | 58 | 8 |
| BLUE_FRAMELEFT_BMP | 5 | 62 | 8 |
| BLUE_FRAMEMAXIMIZED_BMP | 66 | 58 | 8 |
| BLUE_FRAMERIGHT_BMP | 5 | 62 | 8 |
| BLUE_MDICAPTIONBUTTON_BMP | 13 | 52 | 8 |
| BLUE_SMALLCLOSEBUTTON_BMP | 13 | 104 | 32 |
| BLUE_SMALLFRAMECAPTIONSIZING_BMP | 66 | 38 | 1 |
| BLUE_SMALLFRAMECAPTION_BMP | 66 | 38 | 8 |
| BLUE_STARTBUTTON_BMP | 99 | 99 | 24 |
| BLUE_TASKBARBACKGROUNDVERTICAL_BMP | 28 | 41 | 8 |
| BLUE_TASKBARBACKGROUND_BMP | 50 | 28 | 8 |
| BLUE_TASKBARCHEVRONVERT_BMP | 5 | 48 | 8 |
| BLUE_TASKBARCHEVRON_BMP | 8 | 72 | 8 |
| BLUE_TASKBARGRIPPERVERTICAL_BMP | 16 | 6 | 8 |
| BLUE_TASKBARGRIPPER_BMP | 6 | 16 | 8 |
| BLUE_TASKBARSIZINGBARBOTTOM_BMP | 15 | 4 | 8 |
| BLUE_TASKBARSIZINGBARLEFT_BMP | 4 | 15 | 8 |
| BLUE_TASKBARSIZINGBARRIGHT_BMP | 4 | 15 | 8 |
| BLUE_TASKBARSIZINGBARTOP_BMP | 15 | 4 | 8 |
| BLUE_TASKBARTRAYVERTICAL_BMP | 28 | 110 | 8 |
| BLUE_TASKBARTRAY_BMP | 110 | 28 | 8 |
| HOMESTEAD_CAPTIONBUTTON_BMP | 21 | 168 | 24 |
| HOMESTEAD_CLOSEBUTTON_BMP | 21 | 168 | 24 |
| HOMESTEAD_FRAMEBOTTOM_BMP | 49 | 10 | 24 |
| HOMESTEAD_FRAMECAPTIONMIN_BMP | 66 | 58 | 24 |
| HOMESTEAD_FRAMECAPTIONSIZING_BMP | 66 | 58 | 1 |
| HOMESTEAD_FRAMECAPTION_BMP | 66 | 58 | 24 |
| HOMESTEAD_FRAMELEFT_BMP | 5 | 62 | 24 |
| HOMESTEAD_FRAMEMAXIMIZED_BMP | 66 | 58 | 24 |
| HOMESTEAD_FRAMERIGHT_BMP | 5 | 62 | 24 |
| HOMESTEAD_MDICAPTIONBUTTON_BMP | 13 | 52 | 8 |
| HOMESTEAD_SMALLCLOSEBUTTON_BMP | 13 | 104 | 32 |
| HOMESTEAD_SMALLFRAMECAPTIONSIZING_BMP | 66 | 38 | 1 |
| HOMESTEAD_SMALLFRAMECAPTION_BMP | 66 | 38 | 24 |
| HOMESTEAD_STARTBUTTON_BMP | 99 | 99 | 24 |
| HOMESTEAD_TASKBARBACKGROUNDVERTICAL_BMP | 28 | 41 | 24 |
| HOMESTEAD_TASKBARBACKGROUND_BMP | 50 | 30 | 24 |
| HOMESTEAD_TASKBARCHEVRONVERT_BMP | 5 | 48 | 8 |
| HOMESTEAD_TASKBARCHEVRON_BMP | 8 | 72 | 8 |
| HOMESTEAD_TASKBARGRIPPERVERTICAL_BMP | 16 | 6 | 24 |
| HOMESTEAD_TASKBARGRIPPER_BMP | 6 | 16 | 24 |
| HOMESTEAD_TASKBARSIZINGBARBOTTOM_BMP | 15 | 4 | 24 |
| HOMESTEAD_TASKBARSIZINGBARLEFT_BMP | 4 | 15 | 24 |
| HOMESTEAD_TASKBARSIZINGBARRIGHT_BMP | 4 | 15 | 24 |
| HOMESTEAD_TASKBARSIZINGBARTOP_BMP | 15 | 4 | 24 |
| HOMESTEAD_TASKBARTRAYVERTICAL_BMP | 28 | 110 | 24 |
| HOMESTEAD_TASKBARTRAY_BMP | 110 | 28 | 24 |
| METALLIC_CAPTIONBUTTON_BMP | 21 | 168 | 24 |
| METALLIC_CLOSEBUTTON_BMP | 21 | 168 | 24 |
| METALLIC_FRAMEBOTTOM_BMP | 49 | 10 | 24 |
| METALLIC_FRAMECAPTIONMIN_BMP | 66 | 58 | 24 |
| METALLIC_FRAMECAPTIONSIZING_BMP | 66 | 58 | 1 |
| METALLIC_FRAMECAPTION_BMP | 66 | 58 | 24 |
| METALLIC_FRAMELEFT_BMP | 5 | 62 | 24 |
| METALLIC_FRAMEMAXIMIZED_BMP | 66 | 52 | 24 |
| METALLIC_FRAMERIGHT_BMP | 5 | 62 | 24 |
| METALLIC_MDICAPTIONBUTTON_BMP | 13 | 52 | 24 |
| METALLIC_SMALLCLOSEBUTTON_BMP | 13 | 104 | 24 |
| METALLIC_SMALLFRAMECAPTIONSIZING_BMP | 66 | 38 | 1 |
| METALLIC_SMALLFRAMECAPTION_BMP | 66 | 38 | 24 |
| METALLIC_STARTBUTTON_BMP | 99 | 99 | 24 |
| METALLIC_TASKBARBACKGROUNDVERTICAL_BMP | 30 | 41 | 24 |
| METALLIC_TASKBARBACKGROUND_BMP | 50 | 30 | 24 |
| METALLIC_TASKBARCHEVRONVERT_BMP | 5 | 48 | 24 |
| METALLIC_TASKBARCHEVRON_BMP | 8 | 72 | 24 |
| METALLIC_TASKBARGRIPPERVERTICAL_BMP | 16 | 6 | 24 |
| METALLIC_TASKBARGRIPPER_BMP | 6 | 16 | 24 |
| METALLIC_TASKBARSIZINGBARBOTTOM_BMP | 15 | 4 | 24 |
| METALLIC_TASKBARSIZINGBARLEFT_BMP | 4 | 15 | 24 |
| METALLIC_TASKBARSIZINGBARRIGHT_BMP | 4 | 15 | 24 |
| METALLIC_TASKBARSIZINGBARTOP_BMP | 15 | 4 | 24 |
| METALLIC_TASKBARTRAYVERTICAL_BMP | 28 | 110 | 24 |
| METALLIC_TASKBARTRAY_BMP | 110 | 28 | 24 |

Paramètres présents : EXTRALARGEBLUE_INI, EXTRALARGEHOMESTEAD_INI, EXTRALARGEMETALLIC_INI, LARGEFONTSBLUE_INI, LARGEFONTSHOMESTEAD_INI, LARGEFONTSMETALLIC_INI, NORMALBLUE_INI, NORMALHOMESTEAD_INI, NORMALMETALLIC_INI, THEMES_INI.

#### NORMALBLUE_INI

```ini
[Window.Caption]
ImageFile = Blue\FrameCaption.bmp
SizingMargins = 28, 35, 9, 17
ContentMargins = 1, 1, 1, 1
CaptionMargins = 4, 3, 0, 0
SizingType = stretch
Imagelayout = vertical
ImageCount=2
Transparent = True
FillColorHint = 0 84 229; Average background color (strong blue)

[Window.Caption(active)]
TextShadowType = Single

[Window.CaptionSizingTemplate]
ImageFile = Blue\FrameCaptionSizing.bmp
SizingMargins = 28, 35, 9, 17
ContentMargins = 18, 18, 26, 0
SizingType = tile
Imagelayout = vertical
ImageCount=2
Transparent = True
TransparentColor = 0 0 0

[Window.FrameLeft]
ImageFile = Blue\frameLeft.bmp
SizingMargins = 2, 2, 0, 0
SizingType = stretch
ImageLayout = vertical
ImageCount = 2
Transparent = FALSE
Borderonly = FALSE

[Window.FrameRight]
ImageFile = Blue\frameRight.bmp
SizingMargins = 2, 2, 0, 0
SizingType = stretch
ImageLayout = vertical
ImageCount = 2
Transparent = FALSE
Borderonly = FALSE

[Window.FrameBottom]
ImageFile = Blue\frameBottom.bmp
SizingMargins = 5, 5, 2, 2
SizingType = stretch
ImageLayout = vertical
ImageCount = 2
Transparent = FALSE
Borderonly = FALSE

[Window.CloseButton]
BgType = ImageFile
SizingMargins = 5, 5, 5, 5
ContentMargins = 2, 2, 2, 2
sizingType = Stretch
ImageFile = Blue\CloseButton.bmp
GlyphType   = ImageGlyph
GlyphTransparent = True
ImageCount = 8
ImageLayout = Vertical
OffsetType = TopRight
Offset = -25, 5
FillColorHint = 228 93 61; Close box average color (brick red)
BorderColorHint = 255 255 255; Edge color (white)

;----- glyphs ----
ImageSelectType = size   ; choose src image by DPI of target DC (vs. theme DPI)
UniformSizing   = true  ; if you must stretch, do width & height equally
ImageFile1=Blue\CloseGlyph6.bmp
ImageFile2=Blue\CloseGlyph9.bmp
ImageFile3=Blue\CloseGlyph.bmp
ImageFile4=Blue\CloseGlyph19.bmp
ImageFile5=Blue\CloseGlyph23.bmp
MinDpi1=96
MinDpi2=126
MinDpi3=170


;Maximize button in window titlebar
[TaskBar.BackgroundBottom]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 0, 0, 15, 11
ImageFile = Blue\TaskbarBackground.bmp
ImageCount  = 1
FillColorHint = 36 94 220; Average background color (blue)

[TaskBar.BackgroundLeft]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 15, 11, 0, 0
ImageFile = Blue\TaskbarBackgroundVertical.bmp
ImageCount  = 1
FillColorHint = 36 94 220; Average background color (blue)

[TaskBar.BackgroundTop]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 0, 0, 15, 11
ImageFile = Blue\TaskbarBackground.bmp
ImageCount  = 1
FillColorHint = 36 94 220; Average background color (blue)

[TaskBar.BackgroundRight]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 11, 13, 0, 0
ImageFile = Blue\TaskbarBackgroundVertical.bmp
ImageCount  = 1
FillColorHint = 36 94 220; Average background color (blue)

```

#### NORMALHOMESTEAD_INI

```ini
[Window.Caption]
ImageFile  = HomeStead\FrameCaption.bmp
SizingMargins = 28, 35, 9, 17
ContentMargins = 1, 1, 1, 1
CaptionMargins = 4, 3, 0, 0
SizingType = stretch
Imagelayout = vertical
ImageCount=2
Transparent = True
FillColorHint = 170 184 134; Average background color (olive green)

[Window.Caption(active)]
TextShadowType = Single

[Window.CaptionSizingTemplate]
ImageFile  = HomeStead\FrameCaptionSizing.bmp
SizingMargins = 28, 35, 9, 17
ContentMargins = 18, 18, 26, 0
SizingType = tile
Imagelayout = vertical
ImageCount=2
Transparent = True
TransparentColor = 0 0 0

[Window.FrameLeft]
ImageFile=HomeStead\frameLeft.bmp
SizingMargins = 2, 2, 0, 0
SizingType = stretch
ImageLayout = vertical
ImageCount = 2
Transparent = FALSE
Borderonly = FALSE

[Window.FrameRight]
ImageFile=HomeStead\frameRight.bmp
SizingMargins = 2, 2, 0, 0
SizingType = stretch
ImageLayout = vertical
ImageCount = 2
Transparent = FALSE
Borderonly = FALSE

[Window.FrameBottom]
ImageFile=HomeStead\frameBottom.bmp
SizingMargins = 5, 5, 2, 2
SizingType = stretch
ImageLayout = vertical
ImageCount = 2
Transparent = FALSE
Borderonly = FALSE

[Window.CloseButton]
BgType = ImageFile
SizingMargins = 5, 5, 5, 5
ContentMargins = 2, 2, 2, 2
sizingType = Stretch
ImageFile=HomeStead\CloseButton.bmp
GlyphType   = ImageGlyph
GlyphTransparent = True
ImageCount = 8
ImageLayout = Vertical
OffsetType = TopRight
Offset = -25, 5
FillColorHint = 228 93 61; Close box average color (brick red)
BorderColorHint = 255 255 255; Edge color (white)

;----- glyphs ----
ImageSelectType = size   ; choose src image by DPI of target DC (vs. theme DPI)
UniformSizing   = true  ; if you must stretch, do width & height equally
ImageFile1=homestead\CloseGlyph6.bmp
ImageFile2=homestead\CloseGlyph9.bmp
ImageFile3=homestead\CloseGlyph.bmp
ImageFile4=homestead\CloseGlyph19.bmp
ImageFile5=homestead\CloseGlyph23.bmp
MinDpi1=96
MinDpi2=126
MinDpi3=170


;Maximize button in window titlebar
[TaskBar.BackgroundBottom]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 0, 0, 17, 12
ImageFile   = HomeStead\TaskbarBackground.bmp
ImageCount  = 1
FillColorHint = 170 184 131; Average background color (olive)

[TaskBar.BackgroundLeft]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 15, 11, 0, 0
ImageFile   = HomeStead\TaskbarBackgroundVertical.bmp
ImageCount  = 1
FillColorHint = 170 184 131; Average background color (olive)

[TaskBar.BackgroundTop]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 0, 0, 15, 11
ImageFile   = HomeStead\TaskbarBackground.bmp
ImageCount  = 1
FillColorHint = 170 184 131; Average background color (olive)

[TaskBar.BackgroundRight]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 11, 13, 0, 0
ImageFile   = HomeStead\TaskbarBackgroundVertical.bmp
ImageCount  = 1
FillColorHint = 170 184 131; Average background color (olive)

```

#### NORMALMETALLIC_INI

```ini
[Window.Caption]
ImageFile = Metallic\Framecaption.bmp
SizingMargins = 28, 35, 9, 17
ContentMargins = 1, 1, 1, 1
CaptionMargins = 4, 3, 0, 0
SizingType = stretch
Imagelayout = vertical
ImageCount=2
Transparent = True
FillColorHint = 164 163 190; Average background color (strong grey)


[Window.Caption(active)]
TextShadowType = Single

[Window.CaptionSizingTemplate]
ImageFile = Metallic\FrameCaptionSizing.bmp
SizingMargins = 28, 35, 9, 17
ContentMargins = 18, 18, 26, 0
SizingType = tile
Imagelayout = vertical
ImageCount=2
Transparent = True
TransparentColor = 0 0 0

[Window.FrameLeft]
ImageFile = Metallic\frameLeft.bmp
SizingMargins = 1, 1, 0, 0
SizingType = stretch
ImageLayout = vertical
ImageCount = 2
Transparent = FALSE
Borderonly = FALSE

[Window.FrameRight]
ImageFile = Metallic\frameRight.bmp
SizingMargins = 1, 1, 0, 0
;ContentMargins = 1, 3, 0, 3
SizingType = stretch
ImageLayout = vertical
ImageCount = 2
Transparent = FALSE
Borderonly = FALSE

[Window.FrameBottom]
ImageFile = Metallic\frameBottom.bmp
SizingMargins = 5, 5, 1, 2
SizingType = stretch
ImageLayout = vertical
ImageCount = 2
Transparent = FALSE
Borderonly = FALSE

[Window.CloseButton]
BgType = ImageFile
SizingMargins = 5, 5, 5, 5
ContentMargins = 2, 2, 2, 2
sizingType = Stretch
ImageFile = Metallic\CloseButton.bmp
GlyphType   = ImageGlyph
GlyphTransparent = True
ImageCount = 8
ImageLayout = Vertical
OffsetType = TopRight
Offset = -25, 5
FillColorHint = 228 93 61; Close box average color (brick red)
BorderColorHint = 255 255 255; Edge color (white)


;----- glyphs ----
ImageSelectType = size   ; choose src image by DPI of target DC (vs. theme DPI)
UniformSizing   = true  ; if you must stretch, do width & height equally
ImageFile1=metallic\CloseGlyph6.bmp
ImageFile2=metallic\CloseGlyph9.bmp
ImageFile3=metallic\CloseGlyph.bmp
ImageFile4=metallic\CloseGlyph19.bmp
ImageFile5=metallic\CloseGlyph23.bmp
MinDpi1=96
MinDpi2=126
MinDpi3=170



;Maximize button in window titlebar
[TaskBar.BackgroundBottom]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 0, 0, 17, 12
ImageFile = Metallic\TaskbarBackground.bmp
ImageCount  = 1
FillColorHint = 168 169 191; Average background color (grey)


[TaskBar.BackgroundLeft]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 17, 12, 0, 0
ImageFile = Metallic\TaskbarBackgroundVertical.bmp
ImageCount  = 1
FillColorHint = 177 183 187; Average background color (grey)


[TaskBar.BackgroundTop]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 0, 0, 17, 12
ImageFile = Metallic\TaskbarBackground.bmp
ImageCount  = 1
FillColorHint = 177 183 187; Average background color (grey)


[TaskBar.BackgroundRight]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 17, 12, 0, 0
ImageFile = Metallic\TaskbarBackgroundVertical.bmp
ImageCount  = 1
FillColorHint = 177 183 187; Average background color (grey)


```


### Glyphes et panneau Démarrer XP

Version du fichier de style extraite de VS_FIXEDFILEINFO : `1.0.0.1`.

| Ressource | Largeur | Hauteur | Bits |
|---|---:|---:|---:|
| BLUE_CLOSEGLYPH19_BMP | 19 | 152 | 32 |
| BLUE_CLOSEGLYPH23_BMP | 23 | 184 | 32 |
| BLUE_CLOSEGLYPH6_BMP | 6 | 48 | 24 |
| BLUE_CLOSEGLYPH9_BMP | 9 | 72 | 24 |
| BLUE_CLOSEGLYPH_BMP | 13 | 104 | 32 |
| BLUE_COMBOBUTTONGLYPH_BMP | 10 | 28 | 8 |
| BLUE_HELPGLYPH19_BMP | 19 | 152 | 32 |
| BLUE_HELPGLYPH23_BMP | 23 | 184 | 32 |
| BLUE_HELPGLYPH6_BMP | 6 | 48 | 24 |
| BLUE_HELPGLYPH9_BMP | 9 | 72 | 24 |
| BLUE_HELPGLYPH_BMP | 13 | 104 | 32 |
| BLUE_MAXIMIZEGLYPH19_BMP | 19 | 152 | 24 |
| BLUE_MAXIMIZEGLYPH23_BMP | 23 | 184 | 4 |
| BLUE_MAXIMIZEGLYPH6_BMP | 6 | 48 | 24 |
| BLUE_MAXIMIZEGLYPH9_BMP | 9 | 72 | 24 |
| BLUE_MAXIMIZEGLYPH_BMP | 13 | 104 | 8 |
| BLUE_MDIGLYPHCLOSE_BMP | 10 | 40 | 8 |
| BLUE_MDIGLYPHMINIMIZE_BMP | 10 | 40 | 8 |
| BLUE_MDIGLYPHRESTORE_BMP | 10 | 40 | 8 |
| BLUE_MINIMIZEGLYPH19_BMP | 19 | 152 | 24 |
| BLUE_MINIMIZEGLYPH23_BMP | 23 | 184 | 4 |
| BLUE_MINIMIZEGLYPH6_BMP | 6 | 48 | 24 |
| BLUE_MINIMIZEGLYPH9_BMP | 9 | 72 | 24 |
| BLUE_MINIMIZEGLYPH_BMP | 13 | 104 | 8 |
| BLUE_RESTOREGLYPH19_BMP | 19 | 152 | 24 |
| BLUE_RESTOREGLYPH23_BMP | 23 | 184 | 4 |
| BLUE_RESTOREGLYPH6_BMP | 6 | 48 | 24 |
| BLUE_RESTOREGLYPH9_BMP | 9 | 72 | 24 |
| BLUE_RESTOREGLYPH_BMP | 13 | 104 | 8 |
| BLUE_SCROLLARROWGLYPHSSMALL_BMP | 7 | 112 | 8 |
| BLUE_SCROLLARROWGLYPHS_BMP | 9 | 144 | 8 |
| BLUE_SMALLCLOSEGLYPH_BMP | 7 | 56 | 8 |
| BLUE_SPINDOWNGLYPH_BMP | 7 | 24 | 8 |
| BLUE_SPINLEFTGLYPH_BMP | 6 | 28 | 8 |
| BLUE_SPINRIGHTGLYPH_BMP | 6 | 28 | 8 |
| BLUE_SPINUPGLYPH_BMP | 7 | 24 | 8 |
| BLUE_STARTPANELLOGOFFBACKGROUND_BMP | 97 | 39 | 8 |
| BLUE_STARTPANELLOGOFFBUTTONSHOT_BMP | 72 | 24 | 32 |
| BLUE_STARTPANELLOGOFFBUTTONS_BMP | 72 | 24 | 32 |
| BLUE_STARTPANELMFUBACKGROUND_BMP | 156 | 4 | 8 |
| BLUE_STARTPANELMOREPROGARROWHOT_BMP | 16 | 24 | 32 |
| BLUE_STARTPANELMOREPROGARROW_BMP | 16 | 24 | 32 |
| BLUE_STARTPANELMOREPROGBACKGROUND_BMP | 4 | 2 | 8 |
| BLUE_STARTPANELPLACESBACKGROUND_BMP | 180 | 6 | 8 |
| BLUE_TASKBANDBACKGROUND_BMP | 28 | 10 | 8 |
| BLUE_TASKBANDBUTTONNOEDGEVERTICAL_BMP | 22 | 90 | 24 |
| BLUE_TASKBANDBUTTONNOEDGE_BMP | 13 | 168 | 24 |
| BLUE_TASKBANDBUTTONVERTICAL_BMP | 28 | 138 | 8 |
| BLUE_TASKBANDBUTTON_BMP | 26 | 168 | 8 |
| BLUE_TASKBANDFLASHBUTTONGROUPMENU_BMP | 30 | 23 | 24 |
| BLUE_TASKBANDFLASHBUTTON_BMP | 26 | 28 | 24 |
| BLUE_TASKBANDHOVER_BMP | 30 | 69 | 8 |
| BLUE_TASKBANDSCROLLARROWGLYPHS_BMP | 10 | 160 | 8 |
| BLUE_TASKBANDSCROLLARROWS_BMP | 17 | 272 | 8 |
| BLUE_TASKBANDSCROLLSHAFTHORIZONTAL_BMP | 17 | 68 | 8 |
| BLUE_TASKBANDSCROLLSHAFTVERTICAL_BMP | 17 | 68 | 8 |
| BLUE_TASKBANDSCROLLTHUMBGRIPPERHORIZONTAL_BMP | 8 | 32 | 8 |
| BLUE_TASKBANDSCROLLTHUMBGRIPPERVERTICAL_BMP | 8 | 32 | 8 |
| BLUE_TASKBANDSCROLLTHUMBHORIZONTAL_BMP | 22 | 60 | 8 |
| BLUE_TASKBANDSCROLLTHUMBVERTICAL_BMP | 15 | 88 | 8 |
| BLUE_TOOLBARBUTTONSSPLITDROPDOWNGLYPH_BMP | 5 | 18 | 8 |

Paramètres complémentaires de NORMALBLUE_INI :

```ini
[Window.MaxButton]
BgType = ImageFile
SizingMargins = 5, 5, 5, 5
ContentMargins = 2, 2, 2, 2
sizingType = Stretch
ImageFile = Blue\CaptionButton.bmp
GlyphType   = ImageGlyph
GlyphTransparent = True
ImageCount = 8
ImageLayout=Vertical
OffsetType = TopRight
Offset = -49, 5
FillColorHint = 74 121 246; Titlebar button color (blue)
BorderColorHint = 255 255 255; Edge color (white)

;----- glyphs ----
ImageSelectType = size   ; choose src image by DPI of target DC (vs. theme DPI)
UniformSizing   = true  ; if you must stretch, do width & height equally
ImageFile1=Blue\MaximizeGlyph6.bmp
ImageFile2=Blue\MaximizeGlyph9.bmp
ImageFile3=Blue\MaximizeGlyph.bmp
ImageFile4=Blue\MaximizeGlyph19.bmp
ImageFile5=Blue\MaximizeGlyph23.bmp
MinDpi1=96
MinDpi2=126
MinDpi3=170


;Restore button in window titlebar
[Window.RestoreButton]
BgType = ImageFile
SizingMargins = 5, 5, 5, 5
ContentMargins = 2, 2, 2, 2
sizingType = Stretch
ImageFile = Blue\CaptionButton.bmp
GlyphType   = ImageGlyph
GlyphTransparent = True
ImageCount = 8
ImageLayout=Vertical
OffsetType = TopRight
Offset = -49, 5
FillColorHint = 74 121 246; Titlebar button color (blue)
BorderColorHint = 255 255 255; Edge color (white)

;----- glyphs ----
ImageSelectType = size   ; choose src image by DPI of target DC (vs. theme DPI)
UniformSizing   = true  ; if you must stretch, do width & height equally
ImageFile1=Blue\RestoreGlyph6.bmp
ImageFile2=Blue\RestoreGlyph9.bmp
ImageFile3=Blue\RestoreGlyph.bmp
ImageFile4=Blue\RestoreGlyph19.bmp
ImageFile5=Blue\RestoreGlyph23.bmp
MinDpi1=96
MinDpi2=126
MinDpi3=170


;Minimize button in window titlebar
[Window.MinButton]
BgType = ImageFile
SizingMargins = 5, 5, 5, 5
ContentMargins = 2, 2, 2, 2
sizingType = Stretch
ImageFile = Blue\CaptionButton.bmp
GlyphType   = ImageGlyph
GlyphTransparent = True
ImageCount = 8
ImageLayout=Vertical
OffsetType = TopRight
Offset = -73, 5
FillColorHint = 74 121 246; Titlebar button color (blue)
BorderColorHint = 255 255 255; Edge color (white)

;----- glyphs ----
ImageSelectType = size   ; choose src image by DPI of target DC (vs. theme DPI)
UniformSizing   = true  ; if you must stretch, do width & height equally
ImageFile1=Blue\MinimizeGlyph6.bmp
ImageFile2=Blue\MinimizeGlyph9.bmp
ImageFile3=Blue\MinimizeGlyph.bmp
ImageFile4=Blue\MinimizeGlyph19.bmp
ImageFile5=Blue\MinimizeGlyph23.bmp
MinDpi1=96
MinDpi2=126
MinDpi3=170

;Help button in window titlebar--normally only appears on dialog windows
[StartPanel]
bgtype = none
DefaultPaneSize = 0, 0, 380, 440

[StartPanel.UserPane]
UserPicture         = True
ImageLayout         = Horizontal
TextColor           = 255 255 255
Font                = Franklin Gothic Medium, 14
DefaultPaneSize     = 0, 0, 380, 64
BgType              = Imagefile
Transparent         = True
ImageFile = Blue\StartUserPanel.bmp
Imagecount          = 1
SizingMargins       = 59, 60, 62, 0
sizingType          = Stretch
TEXTSHADOWOFFSET= 2, 2
textshadowcolor     = 9 66 139
TextShadowType      = continuous
FillColorHint = 31 113 216; Average background (blue)


[StartPanel.UserPicture]
BgType = ImageFile
ImageFile = Blue\UserTileBackground.bmp
Imagecount = 1
SizingType = Stretch
SizingMargins = 6, 10, 6, 10
ContentMargins = 8, 6, 8, 6
Transparent = true
FillColorHint = 204 214 235; Average Background (light blue grey)


[StartPanel.ProgList]
DefaultPaneSize = 0, 0, 190, 306
ContentMargins  = 6 4 9 5
TextColor   = 55 55 56
HotTracking = 48 112 208
CaptionText = 128 128 128
BgType  = ImageFile
SizingType  = Stretch
SizingMargins   = 2, 153, 3, 0
sizingType = Stretch
ImageFile = Blue\StartPanelMFUBackground.bmp
imagecount  = 1
FillColorHint = 204 214 235; Average Background (light blue grey)


[StartPanel.ProgListSeparator]
BgType = ImageFile
SizingType = TrueSize
ImageFile = Blue\StartProgramsSeparator.bmp

[StartPanel.MorePrograms]
DefaultPaneSize = 0, 0, 190, 30
TextColor           = 55 55 56
HotTracking         = 48 112 208
Font                = Tahoma, 8, Bold
BgType              = ImageFile
SizingType          = Stretch
SizingMargins       = 3, 0, 0, 1

;  left and right contentmargins should match proglist
ContentMargins  = 8, 8, 0, 6
ImageFile = Blue\StartPanelMoreProgBackGround.BMP
imagecount  = 1
sizingType = Stretch

[StartPanel.MoreProgramsArrow]
BgType  = ImageFile
SizingType  = TrueSize
ImageFile = Blue\StartPanelMoreProgArrow.bmp
ImageCount  = 1
FillColorHint = 108 207 108; Average color (green)

[StartPanel.MoreProgramsArrow(hot)]
BgType  = ImageFile
SizingType  = TrueSize
ImageFile = Blue\StartPanelMoreProgArrowHot.bmp
ImageCount  = 1
FillColorHint = 107 239 107; Average color (bright green)


[StartPanel.PlacesList]
DefaultPaneSize = 0, 0, 190, 336
ContentMargins  = 4 6 9 5
TextColor           = 10 36 106
HotTracking         = 80 150 248
BgType              = imageFile
ImageFile = Blue\StartPanelPlacesBackground.bmp
imagecount = 1
SizingMargins = 172, 7, 3, 1
sizingType = Stretch
FillColorHint = 211 229 250; Background color (light blue)

[StartPanel.PlacesListSeparator]
BgType = ImageFile
SizingType = TrueSize
ImageFile = Blue\StartPlacesSeparator.bmp

[StartPanel.Logoff]
ContentMargins =  0, 0, 4, 2
DefaultPaneSize = 0, 0, 380, 40
TextColor   = 255 255 255
HotTracking = 255 255 255
bgType = imagefile
ImageFile = Blue\StartPanelLogoffBackground.bmp
imagecount = 1
sizingMargins = 49, 47, 0, 38
sizingType = Stretch
FillColorHint = 37 119 223; Background color (lblue)

;; note that if you want transparent LogoffButtons they MUST be 32 bit
;; images with an alphachanel, not 8 or 24 bit images with a TransparentColor
[StartPanel.LogoffButtons]
BgType  = ImageFile
Transparent = True
SizingType  = TrueSize
ImageFile = Blue\StartPanelLogoffButtons.bmp
ImageCount  = 1

[StartPanel.LogoffButtons(hot)]
BgType  = ImageFile
Transparent = True
SizingType  = TrueSize
ImageFile = Blue\StartPanelLogoffButtonsHot.bmp
ImageCount  = 1


;-------------- Taskbar specific --------------
[TaskBar::Toolbar]
BgType  = None
Font= Tahoma, 8
TextColor   = 255 255 255

[TaskBar::Toolbar.Button]
BgType  = ImageFile
SizingType  = Stretch
SizingMargins   = 8, 3, 18, 8
ContentMargins  = 0, 0, 4, 4
ImageFile = Blue\TaskBandButtonNoEdge.bmp
ImageCount  = 6
ImageLayout = vertical
Transparent = True
FillColorHint = 53 122 238; Average background color (blue)

[TaskBar::Toolbar.DropdownButton]
BgType  = ImageFile

SizingType  = Stretch
SizingMargins   = 8, 3, 18, 8
ContentMargins  = 2, 0, 4, 4
ImageFile = Blue\TaskBandButtonNoEdge.bmp
ImageCount  = 6
ImageLayout = vertical
Transparent = True
FillColorHint = 53 122 238; Average background color (blue)

[TaskBarVert::Toolbar]
BgType  = None
Font= Tahoma, 8
TextColor   = 255 255 255

[TaskBarVert::Toolbar.Button]
BgType  = ImageFile
SizingType  = Stretch
SizingMargins   = 15, 5, 6, 4
ContentMargins  = 1, 1, 1, 1
ImageFile = Blue\TaskBandButtonNoEdgeVertical.bmp
ImageCount  = 6
ImageLayout = vertical
Transparent = True
FillColorHint = 53 122 238; Average background color (blue)

[TaskBarVert::Toolbar.DropdownButton]
BgType  = ImageFile
SizingType  = Stretch
SizingMargins   = 15, 5, 6, 4
ContentMargins  = 1, 1, 1, 1
ImageFile = Blue\TaskBandButtonNoEdgeVertical.bmp
ImageCount  = 6
ImageLayout = vertical
Transparent = True
FillColorHint = 53 122 238; ; Average background color (blue)

[TaskBar::Rebar]
BgType  = None
Font= Tahoma, 8
TextColor   = 123 182 232

[TaskBar::Rebar.Band]
BgType  = None

[TaskBar::Rebar.Gripper]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 0, 0, 6, 4
ContentMargins  = 2, 1, 0, 0
ImageFile = Blue\TaskbarGripper.bmp
ImageCount  = 1
Transparent = True
TransparentColor= 255 0 255
FillColorHint = 6 45 159; Gripper dot color (dark purple)

[TaskBar::Rebar.GripperVert]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 4, 6, 0, 0
ContentMargins  = 0, 0, 2, 1
ImageFile = Blue\TaskbarGripperVertical.bmp
ImageCount  = 1
Transparent = True
TransparentColor= 255 0 255
FillColorHint = 6 45 159; Gripper dot color (dark purple)

[TaskBar::Rebar.Chevron]
BgType  = ImageFile
SizingType  = Stretch
SizingMargins   = 8, 3, 18, 8
ContentMargins  = 0, 0, 0, 10
ImageFile = Blue\TaskBandButtonNoEdge.bmp
ImageCount  = 6
ImageLayout = vertical
Transparent = True
GlyphType   = ImageGlyph
GlyphImageFile = Blue\TaskbarChevron.bmp
GlyphTransparent= true
VAlign = Top
FillColorHint = 6 45 159; Overflow glyph color (baby blue)

[TaskBar::Rebar.ChevronVert]
BgType  = ImageFile
SizingType  = Stretch
SizingMargins   = 15, 5, 6, 4
ContentMargins  = 0, 0, 0, 0
ImageFile = Blue\TaskBandButtonNoEdgeVertical.bmp
ImageCount  = 6
ImageLayout = vertical
Transparent = True
GlyphType   = ImageGlyph
GlyphImageFile = Blue\TaskbarChevronVert.bmp
GlyphTransparent= true
FillColorHint = 6 45 159; Overflow glyph color (baby blue)

[TaskBar.BackgroundBottom]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 0, 0, 15, 11
ImageFile = Blue\TaskbarBackground.bmp
ImageCount  = 1
FillColorHint = 36 94 220; Average background color (blue)

[TaskBar.SizingBarBottom]
BgType  = ImageFile
SizingType  = Tile
ImageFile = Blue\TaskbarSizingBarBottom.bmp
ImageCount  = 1
FillColorHint = 50 128 231; Average color (blue)

[TaskBar.BackgroundLeft]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 15, 11, 0, 0
ImageFile = Blue\TaskbarBackgroundVertical.bmp
ImageCount  = 1
FillColorHint = 36 94 220; Average background color (blue)

[TaskBar.SizingBarLeft]
BgType  = ImageFile
SizingType  = Tile
ImageFile = Blue\TaskbarSizingBarLeft.bmp
ImageCount  = 1
FillColorHint = 50 128 231; Average color (blue)

[TaskBar.BackgroundTop]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 0, 0, 15, 11
ImageFile = Blue\TaskbarBackground.bmp
ImageCount  = 1
FillColorHint = 36 94 220; Average background color (blue)

[TaskBar.SizingBarTop]
BgType  = ImageFile
SizingType  = Tile
ImageFile = Blue\TaskbarSizingBarTop.bmp
ImageCount  = 1
FillColorHint = 50 128 231; Average color (blue)

[TaskBar.BackgroundRight]
BgType  = ImageFile
SizingType  = Tile
SizingMargins   = 11, 13, 0, 0
ImageFile = Blue\TaskbarBackgroundVertical.bmp
ImageCount  = 1
FillColorHint = 36 94 220; Average background color (blue)

[TaskBar.SizingBarRight]
BgType  = ImageFile
SizingType  = Stretch
ImageFile = Blue\TaskbarSizingBarRight.bmp
ImageCount  = 1
FillColorHint = 50 128 231; Average color (blue)

;-------------- Taskband specific --------------
[TaskBand.FlashButton]
BgType  = ImageFile
SizingType  = Stretch
SizingMargins   = 4, 4, 4, 4
ContentMargins  = 0, 0, 4, 4
ImageFile = Blue\TaskBandFlashButton.bmp
ImageCount  = 1
ImageLayout = vertical
Transparent = True
FillColorHint = 56 130 234; Average background color (blue)
AccentColorHint = 255 192 91; Attention hilite color (orange)

[TaskBand.FlashButtonGroupMenu]
BgType  = ImageFile
SizingType  = Stretch
SizingMargins   = 4, 4, 4, 4
ContentMargins  = 0, 0, 4, 4
ImageFile = Blue\TaskBandFlashButtonGroupMenu.bmp
ImageCount  = 1
Transparent = True
FillColorHint = 56 130 234; Average background color (blue)
AccentColorHint = 255 192 91; Attention hilite color (orange)

[TaskBand.GroupCount]
Font= Tahoma, 8, Bold
TextColor   = 255 204 109

[TaskBand::Toolbar]
BgType  = None
Font= Tahoma, 8
TextColor   = 255 255 255

[TaskBand::Toolbar.Button]
BgType  = ImageFile
SizingType  = Stretch
SizingMargins   = 17, 5, 15, 8
ContentMargins  = 0, 4, 4, 4
ImageFile = Blue\TaskBandButton.bmp
ImageCount  = 6
ImageLayout = vertical
Transparent = True
FillColorHint = 54 127 239

[TaskBand::Toolbar.DropdownButton]
BgType  = ImageFile
SizingType  = Stretch
SizingMargins   = 17, 5, 15, 8
ContentMargins  = 0, 4, 4, 4
ImageFile = Blue\TaskBandButton.bmp
ImageCount  = 6
ImageLayout = vertical
Transparent = True
FillColorHint = 54 127 239; Average background color (blue)

[TaskBandVert::Toolbar]
BgType  = None
Font= Tahoma, 8
TextColor   = 255 255 255

[TaskBandVert::Toolbar.Button]
BgType  = ImageFile
SizingType  = Stretch
SizingMargins   = 16, 7, 12, 6
ContentMargins  = 0, 4, 4, 4
ImageFile = Blue\TaskBandButtonVertical.bmp
ImageCount  = 6
ImageLayout = vertical
Transparent = True
FillColorHint = 54 127 239; Average background color (blue)

[TaskBandVert::Toolbar.DropdownButton]
BgType  = ImageFile
SizingType  = Stretch
SizingMargins   = 16, 7, 12, 6
ContentMargins  = 0, 4, 4, 4
ImageFile = Blue\TaskBandButtonVertical.bmp
ImageCount  = 6
ImageLayout = vertical
Transparent = True
FillColorHint = 54 127 239; Average background color (blue)

[TaskBand::ScrollBar.ArrowBtn]
BgType  = ImageFile
SizingMargins = 5, 5, 5, 5
ContentMargins = 0, 0, 0, 0
SizingType = Stretch
ImageFile = Blue\TaskbandScrollArrows.bmp
ImageCount = 16
ImageLayout = Vertical
GlyphType   = ImageGlyph
GlyphImageFile = Blue\TaskBandScrollArrowGlyphs.bmp
GlyphTransparent = True
FillColorHint = 25 66 205; Average background color (dark blue)

[TaskBand::Scrollbar.ThumbBtnHorz]
BgType  = ImageFile
SizingMargins = 5, 5, 5, 5
ContentMargins = 4, 4, 0, 0
ImageFile = Blue\TaskBandScrollThumbHorizontal.bmp
ImageCount  = 4
ImageLayout = Vertical
FillColorHint = 25 66 205; Average background color (dark blue)

;A visual element in the middle of the scrollbar thumb
[TaskBand::Scrollbar.GripperHorz]
BgType  = ImageFile
SizingType  = TrueSize
ImageFile = Blue\TaskBandScrollThumbGripperHorizontal.BMP
imagelayout = vertical
Transparent = True
ImageCount  = 4
FillColorHint = 44 96 226; Thumb gripper lighter color (blue)


; Scrollbar Thumb (vertical)
[TaskBand::Scrollbar.ThumbBtnVert]
BgType  = ImageFile
SizingMargins = 5, 5, 5, 5
ContentMargins = 0, 0, 4, 4
ImageFile = Blue\TaskBandScrollThumbVertical.bmp
ImageCount  = 4
ImageLayout = vertical
FillColorHint = 25 66 205; Average background color (dark blue)

;A visual element in the middle of the scrollbar thumb
[TaskBand::Scrollbar.GripperVert]
BgType  = ImageFile
SizingType  = TrueSize
ImageFile = Blue\TaskBandScrollThumbGripperVertical.BMP
ImageCount  = 4
Transparent = True
ImageLayout = Vertical
FillColorHint = 44 96 226; Thumb gripper lighter color (blue)


; Scrollbar Tracks (horizontal)
[TaskBand::Scrollbar.LowerTrackHorz]
BgType  = imagefile
ImageFile = Blue\TaskBandScrollShaftHorizontal.bmp
SizingMargins = 0, 0, 1, 1
ImageCount = 4
ImageLayout = Vertical
FillColorHint = 37 90 225; Average background color (blue)


[TaskBand::Scrollbar.UpperTrackHorz]
BgType  = imagefile
ImageFile = Blue\TaskBandScrollShaftHorizontal.bmp
SizingMargins = 0, 0, 1, 1
ImageCount = 4
ImageLayout = Vertical
FillColorHint = 37 90 225; Average background color (blue)


; Scrollbar Tracks (vertical)
[TaskBand::Scrollbar.LowerTrackVert]
BgType  = imagefile
ImageFile = Blue\TaskBandScrollShaftVertical.bmp
SizingMargins = 1, 1, 0, 0
ImageCount = 4
ImageLayout = Vertical
FillColorHint = 37 90 225; Average background color (blue)


[TaskBand::Scrollbar.UpperTrackVert]
BgType  = imagefile
ImageFile = Blue\TaskBandScrollShaftVertical.bmp
SizingMargins = 1, 1, 0, 0
ImageCount = 4
ImageLayout = Vertical
FillColorHint = 37 90 225; Average background color (blue)

;-------------- Taskband Group Menu specific --------------
[TaskBandGroupMenu::Toolbar]
BgType  = ImageFile
ImageFile = Blue\TaskBandBackground.bmp
SizingType	= Tile
SizingMargins	= 3, 3, 3, 3
ContentMargins	= 9, 0, 0, 0
Font= Tahoma, 8
TextColor   = 255 255 255
FillColorHint = 33 87 213; Average background color (blue)
AccentColorHint = 255 199 60; Menu top hilite color (orange)


[TaskBandGroupMenu::Toolbar.Button]
BgType  = ImageFile
SizingType  = stretch
SizingMargins   = 3, 3, 0, 0
ContentMargins  = 9, 0, 3, 3
ImageFile = Blue\TaskBandHover.bmp
ImageCount  = 3
ImageLayout	= vertical
Transparent	= True
AccentColorHint = 79 150 255; Rollover hilite color (blue)

;-------------- Tray Notify specific --------------
```

Icônes Windows XP — ressources du même média XP SP3 que Luna :
- SHELL32.DLL : SHA-256 7c201e812138e2b9cbc2aa94e41d5573775d893b8104833a997acc79486d9a3a ; groupes conservés 1, 3, 4, 7, 13, 14, 16, 21, 24, 27, 36, 47, 145, 165, 270, 274.
- USER32.DLL : SHA-256 acd0ae7b4d5f871e148276c6cc4ae3a216e33f67fc78d827c16986e1f945438c ; groupes 101–104 pour les messages.
- EXPLORER.EXE : SHA-256 1e675cb7df214172f7eb0497f7275556038a0d09c6e5a3e6862c5e26885ef455 ; bitmap 143, drapeau Démarrer 25 × 20 avec canal alpha.
Les fichiers ICO conservent les tailles et profondeurs originales. Les images de machines et de contenus restent fournies par les modules ; aucun remplacement du classement MediaEngine. Les trois binaires, les archives compressées, les planches de sélection et le script d’extraction sont temporaires et supprimés après copie des seules images utiles.

### Intégration XP livrable — 1 novembre 2026

OS contient Windows 3.1, Windows 95 et Windows XP. Les styles Bleu, Argent et Vert olive appartiennent à XP ; leurs noms sont traduits avec Argos dans les 29 catalogues localisés. Le nom Windows XP reste dans la base commune. Les aperçus viennent des captures du bureau à 1280 × 800.

Le bureau XP conserve Démarrer à deux colonnes, les lanceurs et les fenêtres natives indépendantes. Les huit applications et les 26 dialogues sont raccordés aux commandes existantes. Les boutons de tâches se réduisent à la largeur disponible puis utilisent deux flèches de défilement, sans masquer leur contenu sous une barre de défilement. Les palettes s'appliquent aux fenêtres, onglets inactifs, textes et images du bureau sans recréer les contrôleurs. Windows 3.1, Windows 95 et XP proposent un accès direct à Thèmes par le parcours Préférences existant.

Validation : parcours des réglages XP des 24 modules livrés, de leurs 213 machines et de 1 830 onglets ; aucun champ déclaré laissé sans présentation. Chaque routage natif est ouvert puis fermé avec vérification de destruction du HWND. Les confirmations sans annulation conservent leur refus de fermeture. Deux contrôleurs de test restituent les mêmes machines, fabriques de présentation et surfaces de vidéo ; les vues transitoires sont détruites suivant le contrat existant. Cela ne constitue pas un démarrage de chaque cœur matériel ni une validation sur tous les GPU.

Les captures vérifiées sont dans `artifacts/themes-review/screens/OS/WindowsXP/` (Desktop, StartMenu, Application, Route, Module, EmulationSession et TaskbarOverflow). Les données de ces captures sont isolées et synthétiques ; les réglages utilisateur ne sont pas modifiés. Les scénarios temporaires et leurs dossiers sont supprimés après usage. XP attend la validation utilisateur avant commit ; les autres déclinaisons OS et Orbit, Matrix et Atlas restent dans le périmètre global.


## Compositions Orbit, Matrix et Atlas — réalisation après XP

### Orbit : la machine et ses documents

Orbit ouvre d'abord la bibliothèque des machines et les sessions en cours. Un rail étroit de quatre espaces remplace le ruban des sept opérations : machines, médias, journal et commandes. Le centre contient soit la machine et son rendu emprunté, soit un document de travail. Les sessions disposent d'un sélecteur à images ; les périphériques et actions de la machine sont présentés dans un tiroir contextuel, sans détour par un écran d'accueil intermédiaire.

Les opérations sur les médias ont un parcours propre : source, réglages, destination et lancement. Brut et format restent deux pages distinctes. Les réglages avancés, profils, sélection de formats multiples, conflits et résultats restent accessibles. Explorer et Visualisation forment l'espace d'examen des documents, avec leurs vues et propriétés coordonnées ; les surfaces de flux, secteurs, blocs, optique et bande restent empruntées aux moteurs existants. Le journal est un panneau repliable du travail, alimenté par le journal global existant : il ne prétend pas attribuer à une machine des messages dépourvus de cette information.

Les commandes globales sont cherchables dans une palette, regroupées par leur chemin fonctionnel ; tous les accès Options, Émulation et Aide y restent présents et actualisés. Les réglages et les dialogues ont un cadre Orbit, une navigation de rubriques et des formulaires indépendants. Aucun menu ni cadre Windows 3.1/95/XP n'est utilisé. Les contrôles ont leur propre dessin, focus, sélection et états désactivés. Deux apparences, claire et sombre, conservent exactement cette composition. Le dessin emploie des surfaces unies, un accent turquoise pour l'action et des indicateurs de contexte, sans effets appliqués aux images ou vidéos émulées.

L'inventaire de fichiers de `Theme/Orbit/` est détaillé dans `docs/tasks/application-themes.md` : shell et palette de commandes ; bibliothèque et présentation des machines ; lecture, écriture, conversion et leurs propriétés ; exploration et visualisation ; outils et journal ; fenêtres communes, préférences, réglages de tous les modules, créations/configurations de tous les supports, conflits, erreurs, firmware, mises à jour, historique, migration et À propos. Les contrôleurs, sources, listes et rendus restent chez leurs propriétaires. Les vues Orbit n'hébergent aucune page visuelle Base ou OS et ne déplacent pas leurs blocs.

### Matrix : interface de terminal structurée

Matrix présente un espace textuel partagé en panneaux, avec une ligne de commande de navigation, une liste de travaux et un panneau de contenu. Les choix et formulaires utilisent des lignes repérées, une sélection au clavier, des raccourcis visibles et une zone de validation. Les confirmations, erreurs, réglages, inventaires et journaux suivent ce même langage visuel. Les fonctions restent accessibles à la souris ; aucune commande libre ne remplace une validation existante. Le rendu graphique des machines occupe un panneau graphique dédié et conserve ses couleurs d'origine. Cette composition n'utilise ni le rail d'Orbit, ni ses tiroirs, ni les menus et fenêtres d'OS.

### Atlas : atelier organisé par support

Atlas prend le support ou l'image comme point d'entrée. Un catalogue de documents et une fiche du support rassemblent acquisition, écriture, conversion, inspection et exploration, sans répartir les informations du même support entre sept pages éloignées. Une séquence de travail expose source, destinations et résultats ; le journal reste attaché à l'espace des opérations existantes. Les réglages sont des fiches et les dialogues des formulaires documentaires. Les machines et sessions sont accessibles dans un espace propre, avec les mêmes contrats de surfaces et périphériques. Atlas ne reprend ni le bureau OS, ni les panneaux de terminal Matrix, ni le cockpit d'Orbit.

Les styles ou apparences ne deviennent pas de nouveaux thèmes. Base et Studio validés sont conservés. Chaque nouveau thème sera ajouté au catalogue après réalisation de toutes ses fonctions, capturé à 1280×800 et 1920×1080, puis livré en Debug avec tous les modules. Aucun commit sans validation utilisateur.


## Périmètre corrigé à la demande utilisateur — 1 novembre 2026

Terminer Orbit puis arrêter le travail sur les thèmes. Ne pas engager Matrix, Atlas ni les déclinaisons OS restantes. OS demeure incomplet : Macintosh OS, Atari GEM et Amiga Workbench ne sont pas réalisés. Windows 98 est reporté à une éventuelle réalisation ultérieure à la demande de l’utilisateur ; il ne compte pas dans les manques actuels. Leurs mentions antérieures sont un inventaire des manques, pas une livraison. Les compositions Matrix et Atlas restent des propositions documentées. La livraison Orbit conserve les contrôles et le build Debug tous modules prévus, avec validation utilisateur avant commit.


### Orbit — livraison pour essai

Orbit est enregistré après reconstruction de ses vues : rail Machines/Médias/Journal/Commandes, documents des six opérations, sessions centrales, préférences, réglages de modules et dialogues propres à Orbit. Les traitements existants sont conservés. Les aperçus LightPreview.png et DarkPreview.png sont issus des captures réelles de la présentation.

Les 176 contrôles existants de thèmes passent. Neuf scénarios temporaires vérifient navigation, conversion et toutes les pages déclarées par les 24 modules livrés ; six autres vérifient préférences, confirmations, fenêtres natives et transfert de deux sessions. Les captures de préférences et de sessions ont été inspectées ; tests temporaires supprimés après fermeture. Ces contrôles de présentation ne démarrent pas les cœurs réels et ne valident pas tous les GPU ni individuellement toutes les routes spécialisées.

Les captures sont dans artifacts/themes-review/screens/Orbit. Le build Debug officiel --modules=A a réussi (code 0) : exécutable, 24 manifestes/DLL et identité de la DLL application livrée vérifiés. Les 212 tests groupés de thèmes et localisation passent. Aucun commit sans validation utilisateur. Arrêt du travail sur les thèmes après Orbit ; autres déclinaisons OS, Matrix et Atlas non engagés.
