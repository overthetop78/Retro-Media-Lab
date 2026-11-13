# Six thèmes : Base et cinq interfaces nouvelles

Les commits de thèmes `410ad59e0` et `e6325cc48` ont été retirés de la branche locale sans supprimer le travail. Aucun nouveau commit. Référence conservée : `42a173148`.

Validation : 149 contrôles réussis (thèmes, préférences et navigation). Le retour à Base est comparé pixel par pixel avant/après tous les thèmes ; les formulaires conservent les instances métier et le modèle initial. Contrôle supplémentaire réussi après mise en conformité des captures Base avec le style Window réel de l’application. Audit XML : six designs exactement, clés et traductions présentes, anciens noms retirés, noms invariants uniquement en base commune.

Build final réussi (code 0) : `scripts/local-building.cmd --building=debug --modules=0`. Exécutable et DLL vérifiés : `F:\Retro Media Lab\build\Debug\Retro Media Lab\retromedialab.exe` et `lib/retromedialab.app.dll`. Les huit aperçus embarqués correspondent exactement aux PNG sources (comparaison SHA-256). Aucun commit supplémentaire ; la branche reste sur `42a173148`.

# Corrections et séparation thème / déclinaison / style

Demande : corriger tous les défauts constatés sur les vraies fenêtres, différencier réellement les organisations et livrer avec Acorn (build de tous les modules). Les exemples historiques ne sont pas une liste imposée. Thème = organisation ; déclinaison = habillage interne au thème ; style = clair/sombre. Aucun commit.

# Livraison de Base seule avant tout nouveau thème

Ordre demandé : supprimer les cinq thèmes refusés, terminer Base, tester, construire tous les modules, attendre la validation utilisateur puis seulement committer. Les prochains thèmes seront réalisés un à un après cette validation.

## Résultat de la livraison Base seule

- Les cinq thèmes refusés sont supprimés, y compris leurs gabarits, leurs références et leurs traductions. Le dossier Theme contient uniquement Base.

- Base rassemble les fenêtres, dialogues, contrôles et constructeurs de présentation : 64 vues XAML et 377 fichiers C#. Les commandes et moteurs restent inchangés.

- Fenêtres dérivées ouvertes après sélection de Base clair/sombre, changement à chaud et fermeture effective contrôlés sans appliquer manuellement leur style dans le test d'ouverture. Captures des sept fonctions et de Préférences à 1120×650 et 980×560 examinées ; aperçus Base mis à jour.

- 170 contrôles ciblés application réussis, dont quatre tests temporaires de rendu et cycle de vie. 296 contrôles Acorn réussis. Le test WD1770 a été corrigé pour tenir compte du filtrage déjà présent dans le moteur ; aucun changement au moteur Acorn.

- 97 fichiers XML valides ; ressources des cinq thèmes retirées dans toutes les langues. Test temporaire et artefacts de capture supprimés.

- Script officiel terminé avec le code 0 : `scripts/local-building.cmd --building=debug --modules=A`.

- Vérifiés après assemblage : `F:\Retro Media Lab\build\Debug\Retro Media Lab\retromedialab.exe`, `lib/retromedialab.app.dll`, les 24 manifestes et leurs DLL déclarées, dont Acorn et Amstrad. Les versions livrées correspondent aux manifestes sources.

- Aucun commit créé : HEAD reste `42a173148`. Validation utilisateur de Base en attente ; le commit Base puis le prochain thème restent conditionnés à cette validation.

Validation explicite de Base reçue de l'utilisateur le 30 octobre 2026. Le commit Base est maintenant autorisé, suivi du prochain thème seulement.

# Premier nouveau thème : Studio

Base validé et committé : `38ec52d63`. Studio propose un véritable espace de travail à fenêtres internes, avec ses propres cadres, contrôles et styles clair/sombre. Aucun commit Studio avant validation utilisateur. Les déclinaisons restent des choix internes futurs, pas des thèmes supplémentaires.

## Livraison Studio pour validation

- Base validé et committé dans `38ec52d63` ; aucun commit Studio créé.

- Studio est le seul nouveau thème proposé. Ses styles clair/sombre, palettes, contrôles et cadres sont définis dans `Theme/Studio/`.

- Les sept fonctions restent accessibles ; les fenêtres des outils se déplacent, se redimensionnent, se réduisent et se restaurent, et Ctrl+Tab conserve le passage entre les fonctions. Les vues et commandes existantes sont conservées.

- L'émulation utilise l'espace central lorsqu'elle est active afin que les HWND vidéo natifs ne recouvrent pas les fenêtres WPF. Les autres outils restent ouverts et retrouvent leur position au retour.

- 155 contrôles ciblés réussis : navigation, préférences, persistance et Studio. Les tests temporaires ont contrôlé les sept vues empruntées, les interactions, le retour à Base identique pixel par pixel, les tailles réduites, l'échec de sauvegarde suivi d'une nouvelle tentative et la destruction des handles des fenêtres dérivées.

- Deux aperçus réels 1360×900 incorporés et vérifiés ; traductions Argos revues dans toutes les langues. Le nom invariant Studio reste dans la base commune. Tests et captures temporaires supprimés.

- Build officiel terminé avec le code 0 : `scripts/local-building.cmd --building=debug --modules=A`. Exécutable `F:\Retro Media Lab\build\Debug\Retro Media Lab\retromedialab.exe`, DLL application et 24 manifestes/DLL de modules vérifiés après assemblage, dont Acorn et Amstrad.

- Studio attend la validation utilisateur avant son commit et avant le thème suivant.

Studio validé par l'utilisateur le 30 octobre 2026 : commit autorisé avant le thème suivant.

# Reconstruction complète après refus des thèmes — 31 octobre 2026

Objectif actif : recommencer Orbit, OS, Matrix et Atlas intégralement. Base et Studio validés restent conservés. Les nouvelles présentations doivent reconstruire leurs menus, formulaires, dialogues, journaux et états ; déplacer ou repeindre les blocs Base ne suffit pas. Aucun commit sans validation. Les déclinaisons OS doivent reproduire les conventions et interactions du système concerné, avec références et comparaison visuelle.

- [ ] Reconstruire OS à partir des systèmes historiques.

  - [ ] Construire le bureau et les applications OS.

    - [ ] Reconstruire la consultation des médias Windows 3.1.

    - [ ] Raccorder les formulaires au bureau interne Windows 3.1.

    - [ ] Reprendre l’acquisition Windows 3.1 dont la disposition initiale a été refusée.

- [ ] Ajouter les palettes de couleurs à la variante Windows 3.1 du sélecteur de thèmes, puis reprendre l’explorateur.

  - [ ] Raccorder les choix thème / déclinaison / apparence au sélecteur existant, sans déclarer OS terminé.

- [ ] Reprendre l’explorateur indépendant Windows 3.1 après le raccordement des apparences.

- [ ] Raccorder les machines au bureau Windows 3.1 avec des fenêtres indépendantes.

- [ ] Raccorder le bureau Windows 3.1 à la fenêtre principale et conserver les accès fonctionnels.

- [ ] Reconstruire les préférences Windows 3.1 avec leurs sources fonctionnelles existantes.

  - [ ] Terminer toutes les pages de préférences et leurs accès réels.

    - [ ] Reconstruire les manettes avec leur rendu et leurs actions réels.

    - [ ] Raccorder les préférences au bureau par le contrat de présentation existant.

    - [ ] Créer `tests/RetroMediaLab.Tests/Interface/Themes/Windows31PreferencesTests.cs` : vérifier les pages et leur navigation, les modifications/sauvegardes et leur fermeture avec les dépendances simulées existantes ; inscrire les fichiers de capture avant toute création et supprimer les scénarios qui utilisent des fenêtres ou fichiers externes.

  - [ ] Reconstruire les préférences globales d’émulation et leurs accès Système.
    - [ ] Modifier `Theme/OS/Editions/Windows31/Views/EmulationPreferencesView.xaml` et `.xaml.cs` : remplacer les symboles de périphériques issus des polices Base par les pictogrammes Windows31 et les données matérielles existantes, avec libellés accessibles ; prévoir leurs fichiers exacts si un nouvel actif est nécessaire avant de le créer.
    - [ ] Modifier `Services/Windows/WpfWindowNavigationService.cs`, `Services/Theming/ThemeManager.cs` et `Theme/Base/Views/Windows/Preferences/PreferencesWindow.xaml.cs` : appliquer la présentation du nouveau thème aux fenêtres déjà ouvertes en préservant leurs données et le cycle modal ; détailler le mécanisme et les autres fichiers concernés après lecture avant toute modification de contrat.
  - [ ] Reconstruire les options des modules depuis leurs descriptions et contrôleurs existants.
    - [ ] Reconstruire les panneaux spécialisés avant le raccordement des options de modules.

- OS reste incomplet : Macintosh OS, Atari GEM et Amiga Workbench ne sont pas réalisés. Windows 98 est reporté à une éventuelle réalisation ultérieure à la demande de l’utilisateur ; il ne compte pas dans les manques actuels. Demande utilisateur du 1 novembre : ne pas poursuivre ces déclinaisons ; terminer Orbit puis arrêter le travail sur les thèmes.

  - [ ] Construire la déclinaison Windows XP, avec son bureau et ses fenêtres propres.
    - [ ] Construire les ressources et le cadre XP avant le bureau.

  - Matrix et Atlas : ne pas engager leur réalisation. Demande utilisateur du 1 novembre : arrêt du travail sur les thèmes après Orbit. Les compositions documentées restent des propositions non réalisées.

- [ ] Valider la totalité du résultat.

  - [ ] Modifier ce suivi après validation explicite de Windows XP par l’utilisateur, puis enregistrer les fichiers XP et leurs raccordements dans Git ; aucun commit XP avant cette validation. Ce point différé ne bloque pas la réalisation indépendante des autres thèmes.

  - [ ] Modifier ce suivi avec les fichiers de scénarios et captures à produire pour toutes les fonctions, tous les dialogues et tous les thèmes, avant leur réalisation ; prévoir la suppression des tests et sessions temporaires, puis un build officiel Debug tous modules et la préparation des commits sans les créer.

Contrôles de fondation du 31 octobre : 164 tests ciblés réussis (thèmes, navigation, préférences, lecture, historique et raccordement). Aucun nouveau thème déclaré terminé ou livré.

Contrôles après le premier formulaire indépendant : 173 tests ciblés réussis. Source Base/Studio conservée, champs nouveaux vérifiés, aucune réutilisation des contrôles Base dans ce formulaire ; aucun thème OS ou autre déclaré achevé.

Options d’acquisition indépendantes : trois tests réussis le 31 octobre ; saisies et événements métier conservés, listes traduites distinctes et désabonnement contrôlé. Compilation de test réussie ; aucun build livré ni nouveau thème déclaré terminé.

Contrôles après options et placement Windows 3.1 : 181 tests ciblés réussis, dont la restauration normal/agrandi depuis une icône, les bornes de déplacement et la fermeture terminale. Aucun nouveau thème enregistré.

Cadre OS : trois tests supplémentaires réussis après correction de l’accessibilité des ressources x:Static ; commandes de titre, activation, réduction et détachement du document emprunté vérifiés. Comparaison visuelle, polices et menus non achevés.

Inspection visuelle des deux formulaires : rendus réels examinés à 940×600 et 780×540, sans fenêtre native. Le bouton des options avancées était tronqué ; sa colonne s’adapte maintenant à son libellé et le nouveau rendu confirme la correction. Les captures de contrôle et leur test temporaire ont été supprimés. Les polices historiques, les autres contrôles, le menu système et les interactions de déplacement restent explicitement à réaliser ; le rendu courant n’est pas validé comme reproduction exacte.

Validation du lot courant : 187 contrôles ciblés réussis après chargement des gabarits OS, correction de largeur et suppression du scénario visuel temporaire. `git diff --check` réussi. Aucun thème supplémentaire offert dans le sélecteur, aucun commit et aucun build officiel livré dans ce lot. Prochaine action : compléter les familles de contrôles restantes puis réaliser le bureau et les menus système, avant l’intégration des autres fonctions et déclinaisons ; Orbit, Matrix et Atlas restent dans le périmètre intégral.

Contrôles étendus : sept scénarios réussis pour les primitives, les deux orientations, le défilement réel, les commandes de plage et les sélections/expansions. La compilation a d’abord révélé des noms de commandes Slider incorrects dans les nouveaux gabarits ; corrigés avant validation.

Captures demandées par l’utilisateur : `artifacts/themes-rebuild/current/acquisition.png` (1100×660) et `options.png` (900×580), rendues depuis les fenêtres WPF natives actuelles et inspectées, avec données d’exemple. Quatre scénarios temporaires réussis : les fenêtres se masquent sans réduction hôte, restaurent le même handle, ferment sur état terminal et détruisent leurs handles après erreur/fermeture ; les documents empruntés restent intacts. Les deux tests temporaires ont été supprimés ; les PNG demandés restent conservés. Le bureau, les menus complets et les polices historiques ne sont pas terminés.

Bureau interne après les références utilisateur : quatre scénarios temporaires réussis, dont ouverture unique, double-clic, réduction/restauration, agrandissement à l’intérieur du bureau et libération après erreur. Rendu du gestionnaire et des groupes inspecté ; fond blanc du groupe corrigé dans le nouveau ScrollViewer, puis rendu à nouveau et vérifié. Scénario de capture et PNG temporaires supprimés ; trois scénarios autonomes conservés. Les icônes sont provisoires et les polices ne sont pas historiques : aucune fidélité exacte n’est revendiquée. `fontTools` manque pour leur conversion ; demande d’installation envoyée, travail sur les autres éléments poursuivi.

Polices : fontTools 4.68.0 installé avec l’autorisation utilisateur. Trois faces incorporées, 672 glyphes vérifiés contre les masques sources, métriques et familles WPF vérifiées. La perte de traits venait du mode Display ajouté au thème ; la comparaison Ideal/Display/géométrie l’a isolée, puis Ideal a corrigé le rendu du bureau. Capture corrigée inspectée dans `artifacts/themes-rebuild/current/windows31-desktop.png`. Le test de diagnostic et son image ont été supprimés ; le contrôle autonome des ressources incorporées est conservé. Les icônes génériques restent à remplacer à la demande explicite de l’utilisateur.

Icônes originales Windows 3.1 : dix ressources incorporées, neuf images différentes pour les outils et la véritable icône PROGMAN pour les groupes. Quatre contrôles ciblés réussis ; capture du bureau à 1280×800 inspectée dans `artifacts/themes-rebuild/current/windows31-desktop.png`. Test de capture, planche de sélection et cache de l’installation supprimés après conservation des ressources retenues et de leur manifeste.

Interactions du cadre : quatorze contrôles ciblés réussis. Menu système interne, commandes dépendantes de l’état, géométries ancrées sur les bords opposés et prévisualisation sans modification du document vérifiés. Validation/annulation et retrait des surfaces à la fermeture contrôlés dans des arbres WPF autonomes ; la manipulation avec capture souris en fenêtre réelle reste à vérifier avant livraison.

Contrôle après intégration des ressources originales et des interactions : 202 tests ciblés réussis (thèmes, lecture, navigation, préférences), aucun test temporaire conservé et `git diff --check` réussi. Aucun build officiel livré ni commit créé ; OS reste en cours de reconstruction et n’est pas enregistré dans le sélecteur.

Raccordement natif commun : trois scénarios temporaires réussis avec de vrais HWND synthétiques. Régions initiales présentes/absentes restaurées, exclusions et contour creux vérifiés avec le DPI réel, changement de couche et masquage contrôlés, clic natif observé sans consommation. Le service laisse les HWND empruntés vivants ; les propriétaires de test ont fermé leurs fenêtres et confirmé leur destruction. Test temporaire supprimé, aucun autre artefact de fenêtre créé. Ces contrôles ne constituent pas encore une validation visuelle de tous les moteurs GPU dans le thème intégré.

Écriture Windows 3.1 : dix contrôles ciblés réussis (raccordement commun et nouvelle vue), dont menus/actions, saisie reliée au modèle, exclusivités existantes, choix indépendants et détachement. Le conflit Options/espace de noms a été corrigé dans le nouveau formulaire avant validation. Captures françaises inspectées à 740×360 et 620×530 dans `artifacts/themes-rebuild/current/{write,write-options}.png` ; aucun débordement constaté sur ces tailles. Test de rendu temporaire supprimé. L’intégration au bureau et les dialogues globaux restent à faire avant de proposer OS.

Synchronisation native : scénario temporaire réussi avec le service construit avant le HWND, déplacement réel traité automatiquement et destruction pendant une mise à jour en attente. Coordonnées écran/placement natif concordants, région restaurée et handles des fenêtres détruits ; test temporaire supprimé. Le lot écriture et raccordement commun totalise 206 contrôles de thèmes, lecture, navigation et préférences réussis, en plus des quatre scénarios natifs temporaires. Aucun thème supplémentaire enregistré ni commit créé.

Conversion : 35 contrôles ciblés réussis (nouvelle vue, contrôleur commun, chemin de sélection Base, lecture/écriture/options et raccordement). Les premiers rendus ont révélé le mauvais libellé et le faux répertoire de certains caractères, puis le bouton hors vue à 600×480 ; ces défauts ont été corrigés et les captures régénérées et inspectées. Le contrôle de rendu confirme aussi la commande dans les limites de la vue. Traduction de Read.Extension revue dans toutes les langues après Argos, glyphes/métriques conservés et test temporaire supprimé. Aucun thème déclaré fini.

Validation du lot conversion, champs partagés et typographie : 265 contrôles réussis (thèmes, lecture, navigation, conversion Base, préférences et localisation). `git diff --check` réussi. Les captures finales de conversion sont conservées ; aucun scénario temporaire ajouté dans ce lot ne subsiste. OS, Orbit, Matrix et Atlas restent à terminer intégralement ; aucune publication ni aucun commit.

Consultation des informations Windows 3.1 : quatre contrôles réussis, dont le rendu à 540×440 inspecté dans `artifacts/themes-rebuild/current/inspector.png`. Champs et unités complets, choix de surface et détachement vérifiés ; test de capture temporaire supprimé. Le raccordement aux représentations graphiques reste en cours.

Raccordement des cinq représentations média : 89 contrôles ciblés réussis (visualisation existante, emprunt des feuilles et nouveaux événements). Surfaces Skia et commandes d’origine identifiées sans copie des panneaux, parents restaurés après emprunt, sélection séquentielle routée vers ses adresses existantes. Changement de viewport et déchargement annulent les opérations de zoom possédées. La nouvelle fenêtre de visualisation et son intégration restent à construire ; aucun thème déclaré fini, aucun commit ni build officiel livré.

Fenêtre de visualisation Windows 3.1 : 352 contrôles réussis (thèmes, visualisation, lecture, conversion Base, navigation, préférences et localisation). Les cinq représentations changent de surface avec restitution des parents ; mode géométrique et largeur simple face contrôlés. Rendus réels à 1000×660 et 640×480 inspectés dans `artifacts/themes-rebuild/current/{visualization,visualization-narrow}.png`, après correction de l’espace réservé par les options masquées. Le rendu hors écran sans chargement ne déclenchait pas Skia : les captures finales utilisent une fenêtre réelle, fermée dans finally avec destruction du HWND confirmée. Scénario temporaire supprimé et diff vérifié. Les dialogues de classification, l’intégration au bureau et les autres écrans/éditions restent à terminer ; aucune validation globale de thème ni livraison officielle dans ce lot.

Classification Windows 3.1 : 49 contrôles ciblés réussis, dont les choix machine/format/protection, les événements d’origine et le détachement. La capture a révélé le sélecteur de template absent dans le gabarit ComboBox ; corrigé et vérifié par un contrôle autonome du texte rendu. Classification.Format revue dans les 30 catalogues après Argos, notamment les confusions avec disque musical et les mots restés anglais. Formulaire corrigé inspecté dans `artifacts/themes-rebuild/current/classification.png`, scénario temporaire supprimé. Le raccordement des dialogues au bureau et les autres interfaces restent à réaliser ; aucun commit ni livraison officielle.

Dialogues internes Windows 3.1 : 361 contrôles de thèmes, visualisation, lecture, conversion, navigation, préférences et localisation réussis. Options modales et inspecteur non modal, activation sans duplication, restitution de la disponibilité, fermeture du propriétaire et nettoyage après exceptions vérifiés. Les dialogues n’exposent plus les commandes de réduction/agrandissement des applications. Une fermeture en arrière-plan conserve l’application active. Le raccordement aux fabriques des fonctions et les autres écrans/éditions restent à réaliser ; aucun nouveau thème déclaré livré, aucun commit.

Applications média raccordées au bureau : onze contrôles ciblés réussis pour les fabriques, dialogues et bureau ; après ajout du bouton Fermer, quatre contrôles incluant le rendu intégré réussis. Lecture, écriture, conversion et visualisation utilisent leurs vues indépendantes ; options, classification et inspecteur restent internes. Les menus et modèles sont vérifiés jusqu’au bureau ; fermeture puis rappel d’un ancien menu ne recréent pas l’application. Capture de progression à 1280×800 inspectée dans `artifacts/themes-rebuild/current/windows31-media-desktop.png`. Scénario temporaire supprimé, diff vérifié. La disposition d’acquisition refusée, l’explorateur, les outils, l’émulation, les réglages et les autres éditions/thèmes restent à terminer ; OS n’est pas encore proposé comme livré et aucun commit n’a été créé.

Acquisition Windows 3.1 reprise : 33 contrôles ciblés réussis, dont le rendu temporaire et les contrôles existants de lecture. Menus Fichier/Options, onglets brut/format connu, révolutions accessibles uniquement dans le brut, destination, profil et historique complet reconstruits. Choix d’onglet bidirectionnel, indisponibilité source, modèle des révolutions, consultation et fermeture vérifiés. Rendus à 850×560 et 600×430 inspectés dans `artifacts/themes-rebuild/current/{acquisition,acquisition-known,acquisition-narrow}.png` ; le contraste des trois colonnes de la ligne sélectionnée a été corrigé et contrôlé. Bouton d’exécution maintenu dans les limites, scénario temporaire supprimé. Explorer, outils, émulation, réglages, autres éditions et autres thèmes restent à réaliser avant une livraison complète.

Lot palettes et sélecteur : 386 contrôles groupés réussis. Trois captures de bureau supplémentaires inspectées à 1280×800 dans artifacts/themes-review/screens/OS/Windows31. Convertisseurs temporaires, cache Python, scénario de capture et artifacts/themes-rebuild supprimés ; aucune publication de OS incomplet, aucun commit et pas encore de build officiel.

Explorateur indépendant : validation groupée de 291 contrôles réussie après correction des textes générés, y compris navigation, propriétés vivantes, actions, palettes, préférences et visualisation. Captures Explorer, ExplorerInformation et ExplorerNarrow examinées ; scénario temporaire supprimé. Les pictogrammes de fichiers/dossiers historiques, les autres applications et dialogues OS, puis l’intégration complète de Windows 3.1 restent à terminer avant le build Debug tous modules.

Pictogrammes : 218 contrôles groupés réussis. FileEntryIcon conservé, fournisseur d’habillage local à Windows 3.1 vérifié contre la ressource de 1992, pixels et transparence contrôlés, retour aux images Base après détachement vérifié. Captures ExplorerIcons Default/Bordeaux inspectées. Archive, script d’extraction, bitmap source temporaire et scénario de capture supprimés ; aucun nouvel outil conservé dans Tools.

Outils Windows 3.1 : 128 contrôles groupés thèmes/maintenance réussis, puis scénario de capture réussi. Effacer/Nettoyer examinés à 660×420 dans themes-review/screens/OS/Windows31/Default ; textes et commandes visibles, sources Base conservées. Scénario temporaire supprimé et diff vérifié. Émulation et réglages restent à reconstruire ; aucun commit ni build officiel livré.

Gestionnaire des machines Windows 3.1 : 126 contrôles groupés thèmes/émulation réussis, dont le scénario temporaire de capture. Configurations des modules existants, réactivation sans second cœur, actualisation, disponibilité et détachement contrôlés. Capture EmulationManager à 700×400 examinée après correction des ressources simulées ; scénario temporaire supprimé. Les fenêtres de rendu des machines et leurs commandes restent à reconstruire, ainsi que les réglages et l’intégration OS ; aucun commit ni build officiel livré.

Fondation de présentation des machines : 125 contrôles groupés thèmes/émulation réussis après séparation de la vue du rendu et des entrées. Les scénarios existants de rendu masqué et de tape passent ; disposition Base conservée. Le nettoyage libère entrées, vidéo, cœur et présentation avec finally. Aucun scénario temporaire Windows31 restant ; diff vérifié. Les vues propres aux machines Windows 3.1, l’échange de thème des sessions ouvertes et l’intégration restent à réaliser.

Présentation de machine Windows 3.1 : 129 contrôles groupés réussis, capture temporaire incluse. Menus reliés aux commandes existantes et à leurs disponibilités/états, supports et tape issus des modèles existants, remplacement de surface et chargement shader vérifiés. Création asynchrone : un échec de présentation attend la libération effective du cœur et retire la session. Capture Machine.png à 820×540 examinée avec surface de démonstration explicitement identifiée, sans cœur exécuté ; scénario temporaire supprimé. Les fenêtres des machines ne sont pas encore raccordées au bureau OS et le changement de présentation des sessions ouvertes reste à faire. Base reste la présentation par défaut. Aucun commit ni build officiel livré.

Transfert de présentation : 131 contrôles groupés thèmes/émulation réussis. Même surface et même abonnement vidéo conservés aller/retour ; refus de surface rétabli dans la vue précédente. Les commandes déplacées restent fonctionnelles après libération de leur ancien habillage. Les événements de focus suivent le nouvel hôte sans interrogation de matériel dans le scénario autonome. Diff vérifié. Le raccordement des fenêtres au bureau, sa validation avec les sessions réelles et les dialogues restent à réaliser ; aucun commit ni build officiel livré.

Hébergement des machines et notifications : 135 contrôles groupés thèmes/émulation réussis. Tests sur le contrôleur complet : même cœur, même surface et même abonnement vidéo après transfert/restitution, hôte occupé préservé, fermeture détachée sans redémarrage. Deux machines sélectionnées et fermées par les actions source, fermeture générale et retrait des onglets contrôlés. Diff vérifié. Le raccordement des cadres au bureau OS reste à faire ; le suivi du handle natif est inscrit avant ce raccordement pour les surfaces pouvant reconstruire leur HWND. Aucun commit ni build officiel livré.

Cadres de machines raccordés : 137 contrôles groupés réussis, puis capture intégrée réussie à 1280×800 dans DesktopMachines.png. Deux contrôleurs simulés présentés en fenêtres indépendantes, restauration après réduction, agrandissement, fermeture et retrait du thème avec restitution de la session restante vérifiés. Handle de focus lu auprès du présentateur courant. Capture examinée : groupe des opérations mal nommé Outils et titre de groupe encore actif quand le gestionnaire est inactif ; corrections inscrites à la suite. Scénario temporaire supprimé ; aucune preuve de fonctionnement de tous les cœurs GPU réels n’est revendiquée. Aucun commit ni build officiel livré.

Préférences intégrées au bureau : 151 contrôles de thèmes et de navigation existants réussis, puis trois scénarios permanents pour les sept pages, les accès Système et la modalité globale. Le scénario temporaire de cycle de vie a vérifié une vraie fenêtre fonctionnelle non affichée : la sauvegarde asynchrone est attendue avant fermeture, une seule écriture et aucune surface native source conservée. Capture IntegratedPreferences.png examinée ; scénario temporaire supprimé. Les autres dialogues et la validation des sessions réelles restent à réaliser ; aucun thème déclaré terminé ni build officiel livré.

Dialogues métier intégrés : validation groupée de 190 contrôles réussie (thèmes, conversion, navigation et préférences), dont deux scénarios temporaires sur les fenêtres réelles. Nom obligatoire sans boîte native, décisions lecture/matériel, choix individuels et communs de conversion, annulation et imbrication ont été vérifiés ; propriétaires fonctionnels séparés des HWND, fermeture et destruction des handles contrôlées. Cinq captures examinées dans themes-review/screens/OS/Windows31/Default ; taille Profil/Conflit réduite et titre du gestionnaire inactivé après inspection. Scénario temporaire supprimé ; contrôle de diff réussi. Les dialogues d’émulation, les autres fenêtres et les autres thèmes restent à réaliser, sans livraison officielle ni commit.

Préférences d’émulation Windows31 : 181 contrôles groupés réussis, puis six contrôles ciblés réussis après correction des contrastes des éléments imbriqués. Dossiers reliés, filtres de configurations et édition, capture/effacement/restauration des raccourcis, annulation à la libération, tableau réseau non sélectionnable et notifications du registre vérifiés. Chargement initial partagé et attente de sa fin contrôlés sans modules externes. Vraie fenêtre fonctionnelle non affichée : sauvegarde asynchrone attendue et fermeture effective vérifiées. Quatre captures examinées à 1280×800 dans themes-review/screens/OS/Windows31/Default ; scénario temporaire supprimé. Les pictogrammes de périphériques, la transition des fenêtres déjà ouvertes, les options des modules et les autres thèmes restent à terminer ; aucun commit ni build officiel livré.

Options des modules, première reconstruction indépendante : 164 contrôles de thèmes/émulation réussis, puis sept contrôles des feuilles et de la fenêtre réussis. Texte, nombre/IPv4 contraints, informations, choix, radios, cases, curseurs avec unités et automatique, chemins et boutons utilisent les points fonctionnels existants sans déplacement des conteneurs ; modification, création de configuration, catégories réelles, lecture seule et détachement vérifiés avec stockage en mémoire. Trois captures à 1280×800 et 800×600 examinées dans themes-review/screens/OS/Windows31/Default ; scénario temporaire supprimé. Les panneaux spécialisés sont détaillés et restent à reconstruire avant intégration du module au bureau ; aucune validation de tous les modules réels, aucun thème déclaré fini, aucun commit ni build officiel livré.

Panneaux cœurs et sélection ROM Windows 3.1 : 18 contrôles ciblés réussis puis 169 contrôles groupés de thèmes/firmware réussis. Recherche, installation, progression, annulation, verrouillage, compatibilité, sélection et attente de fermeture vérifiés. Captures CoreManagement.png et FirmwareSelection.png inspectées dans themes-review/screens/OS/Windows31/Default ; bouton tronqué corrigé et scénario temporaire supprimé. Les autres panneaux spécialisés et leur intégration complète restent à terminer ; aucun commit ni build officiel livré.

Vidéo Windows 3.1 : 172 contrôles groupés de thèmes/firmware réussis avec le scénario de capture ; après inspection, libellés CRT corrigés à partir du catalogue existant et curseurs verticaux regroupés en colonnes. Sept contrôles ciblés réussis après ces corrections. Toutes les technologies déclarées conservent leurs points fonctionnels, les choix modifient le profil et les éditions de curseurs attendent leur fin pour sauvegarder. Trois captures inspectées (VideoDisplay, VideoCrt, VideoImage), scénario temporaire supprimé. Entrées, stockage et leurs dialogues restent à reconstruire avant intégration complète ; aucun build officiel ni commit.

Entrées Windows 3.1 : 11 contrôles ciblés réussis puis 166 contrôles groupés des thèmes réussis avec le scénario temporaire. Clavier/souris, choix des ports et types, champs conditionnels, conservation des sorties, capture et détachement vérifiés ; les dessins reprennent les ressources existantes, le suivi est arrêté à la fermeture. Captures ModuleKeyboard, ControllerPorts et ControllerPortsNarrow inspectées ; hauteur de table réduite et boutons vérifiés dans le viewport à 800×600. Scénario temporaire supprimé. Stockage et dialogues spécialisés restent à reconstruire, puis intégration de tous les modules et sessions réelles ; aucun build officiel ni commit.

Stockage Windows 3.1 : huit contrôles ciblés réussis puis 168 contrôles des thèmes réussis avec la capture. La liste conserve les résumés déjà calculés, la sélection par emplacement et les capacités fournies par le module ; les périphériques permanents restent protégés et l’application de la configuration conserve les supports. Captures ModuleStorage et ModuleStorageNarrow inspectées, y compris liste longue défilante et boutons visibles ; scénario temporaire supprimé. Les neuf dialogues spécialisés et leur intégration dans le bureau restent à reconstruire. Aucun commit ni build officiel.

Options du cœur et ajout de supports Windows 3.1 : 171 contrôles des thèmes réussis avec quatre scénarios temporaires. Valeurs texte/choix, types disponibles, résultat modal, titre vivant et fermeture des fenêtres fonctionnelles non affichées vérifiés ; le pied de dialogue a aussi été vérifié dans une vraie fenêtre native invisible et ses handles détruits. Captures CoreOptions et AddStorageDevice inspectées. Titre ambigu corrigé via Argos et revue dans les 30 catalogues, une seule clé modifiée par fichier ; 41 contrôles dialogues/localisation réussis après correction. Scénario temporaire supprimé. Les autres dialogues spécialisés restent à reconstruire ; aucun build officiel, commit ni thème déclaré terminé.

Messages Windows 3.1 : 324 contrôles groupés de thèmes, navigation et localisation réussis. Les sept jeux de réponses du framework, le choix par défaut, Échap, le repli natif et deux messages de même type imbriqués sont vérifiés. Quatre captures historiques inspectées dans `artifacts/themes-review/screens/OS/Windows31/Default/Message*.png` ; défilement superflu des messages courts corrigé par mesure réelle après application des styles. Test temporaire supprimé. Les sélecteurs de fichiers/dossiers et la migration des appels directs restent à faire ; aucun commit ni build officiel.

Sélecteurs Windows 3.1 : 324 contrôles groupés thèmes/navigation/localisation réussis, puis quatre scénarios ciblés réussis après la mesure finale et le suivi de fermeture. Ouverture/enregistrement/dossier, filtres Win32, erreurs, noms sans extension, extensions multiples et DefaultExt, refus/accord d’écrasement, lecture seule, annulation pendant le parcours et repli natif sur les objets d’origine vérifiés. Trois captures FileSelection inspectées dans themes-review/screens/OS/Windows31/Default ; colonne vide du dossier supprimée. Scénario et données temporaires supprimés. La migration des appels directs commence ensuite ; pas de livraison globale ni commit.

Migration des 22 fichiers d’appels directs : 334 contrôles groupés de thèmes, navigation, localisation, options, émulation et HDD réussis après ajout de l’import Window requis. Paramètres et valeurs par défaut des confirmations conservés ; insertion/enregistrement tape et cartes mémoire utilisent aussi le relais des dialogues spécialisés. Les fenêtres complètes de modules et les autres fenêtres globales restent à intégrer ; aucun commit ni build officiel.

Fenêtre complète des modules raccordée au bureau Windows 3.1 : 338 contrôles groupés thèmes/navigation/localisation/options/émulation/HDD réussis, dont trois scénarios temporaires sur la fenêtre fonctionnelle. Configuration initiale, chargement partagé, dossiers fonctionnels et exclusion des dossiers par défaut vérifiés. Fermeture pendant le chargement attendue avec ou sans ContentRendered ; contrôleurs et champs libérés, aucun HWND source créé. Capture IntegratedModuleSettings inspectée avec métadonnées synthétiques, scénario temporaire supprimé. La validation de tous les modules réels est inscrite séparément et reste à faire, ainsi que les fenêtres globales et autres thèmes ; pas de build officiel ni commit.

Parcours réel des 24 modules : 213 machines et 1 830 onglets de réglages construits sans champs déclarés manquants ni requête réseau ni lancement de cœur. Nintendo : repli matériel sans adaptateur corrigé, 96 tests de catalogue réussis ; son assembly recompilé a été contrôlé sans remplacer le paquet du build utilisateur. Captures représentatives dans themes-review/screens/OS/Windows31/Modules, scénario et dossiers temporaires supprimés. Inspection complémentaire : résumé total RAM natif absent de la nouvelle vue, raccordement ajouté à la suite avant de déclarer la fenêtre complète terminée. Aucun commit ni build officiel.

Résumé matériel : total RAM raccordé au calcul existant, choix radio suivis dans Base et dans Windows 3.1, liaison libérée au changement de page. Quatre tests autonomes de réglages réussis, puis 337 contrôles groupés avec le contrôle réel Acorn. Capture Modules/acorn/Ram.png inspectée : passage à 32 KiB et total visible correspondant ; scénario et espace de travail temporaire supprimés. Les fenêtres globales (erreurs détaillées, À propos, journaux, mises à jour, outils et migration) restent à reconstruire/raccorder avant la livraison Windows 3.1. Aucun commit ni build officiel.

Erreurs détaillées et À propos Windows 3.1 : 337 contrôles groupés réussis. Message, toutes les paires détail/valeur, types de supports requis, version de l’application et noms/versions des composants conservés ; fermeture par bouton et Échap vérifiée sans HWND source. Captures DetailedError et About inspectées ; erreur dimensionnée d’après la mesure des contrôles, liste des supports titrée, Common.MediaKind.Tape corrigée dans toutes les cultures après Argos et revue. Scénario temporaire supprimé et diff vérifié. Restent journaux, mises à jour, outils avancés, migration et vérification complète des accès avant la livraison Windows 3.1 ; autres déclinaisons et thèmes toujours dans le périmètre. Aucun commit ni build officiel.

Outils avancés Windows 3.1 : 211 tests groupés réussis, dont les neuf verbes, liaisons des paramètres, validation, sortie depuis un thread de fond et fermeture après annulation du runner. Captures Tool-align, Tool-rpm et Tool-info inspectées ; scénario temporaire supprimé. Aucun commit ni build officiel.

Migration et avertissements Windows 3.1 : dix contrôles ciblés réussis, puis 42 contrôles thèmes/localisation réussis ; lecture d’une véritable image DFS vide, initialisation partagée, choix de destination, liste des pertes et intégralité des avertissements vérifiés. Deux captures inspectées après mise en page ; en-têtes historiques et traduction de la description corrigés dans toutes les langues. Scénario et répertoire temporaires supprimés. Aucun commit ni build officiel.

Mises à jour Windows 3.1 : 347 tests groupés réussis, dont recherche annulée et attendue, quatre catégories, versions, catalogue et URL manuelle. Quatre captures inspectées ; bouton du catalogue conservé et mise en page attendue après modification des données du scénario. Trois contrôles ciblés réussis après la dernière correction. Scénario temporaire supprimé ; aucun réseau ni installateur réel lancé. Aucun commit ni build officiel.

Contrôle final des fenêtres : 26 types secondaires Base disposent de leur relais Windows 3.1 ; MainWindow reste le propriétaire natif du bureau. Les menus Options, diagnostics, matériel, journaux, mises à jour, modules et aide reprennent les actions et états existants, y compris les modifications de la liste des modules. 28 contrôles de bureau/menus/palettes réussis et 23 aperçus réels de 1280×800 générés. Le contrôle des 24 modules et 213 machines concerne leurs réglages ; aucun nouveau démarrage de cœur ni validation de tous les GPU n’est affirmé.

Windows 3.1 sélectionnable dans OS : 369 contrôles groupés réussis avant adaptation de l’ancien garde-fou limitant le catalogue à Base/Studio ; ce garde-fou est remplacé par la vérification explicite de Base, Studio et OS avec sa déclinaison Windows 3.1. Les deux contrôles de sélection et le chargement/retour de la palette OS ont été relancés séparément. Aperçus des palettes inspectés, scénario temporaire supprimé avant build officiel.

Build officiel Debug réussi avec `scripts/local-building.cmd --building=debug --modules=A` (code de sortie 0). Exécutable vérifié : `build/Debug/Retro Media Lab/retromedialab.exe`. Les 24 manifestes et leurs 24 assemblies de modules sont présents et correspondent exactement aux identifiants des modules source. Windows 3.1 se choisit dans OS avec ses 23 palettes ; Base et Studio restent proposés. Aucun commit créé. Les autres déclinaisons OS, puis les refontes Orbit/Matrix/Atlas, restent à réaliser dans l’objectif global.

Premier cadre Windows 95 : fenêtre native réelle contrôlée, réduction/agrandissement/restauration exécutés, refus puis accord de fermeture vérifiés. Le HWND est détruit et le contenu emprunté reste non disposé et détaché. Capture Window.png inspectée dans themes-review/screens/OS/Windows95 ; scénario temporaire supprimé. Le cadre ne constitue pas une déclinaison terminée : ressources d’époque, bureau, lancement et vues restent à réaliser, sans ajout au sélecteur ni remplacement du build Windows 3.1 livré.

Bureau Windows 95 : lanceurs au clavier et détachement contrôlés, suivi des titres et absence de destruction des fenêtres empruntées vérifiés. Cinq contrôles groupés réussis puis trois après correction du contraste et de la capture. Capture Desktop.png inspectée avec l’image BBC existante ; scénario temporaire supprimé. Bureau encore indépendant, sans ajout de Windows 95 au catalogue ni remplacement du build Windows 3.1 livré.

Menu Démarrer Windows 95 : modèle propre gris/bleu chargé, sous-menus et action existante vérifiés sans fenêtre native ; deux contrôles ciblés réussis après la dernière modification. Aucun commit, aucune publication ni remplacement du build officiel. Les vues fonctionnelles et les ressources historiques Windows 95 restent à intégrer avant enregistrement de la déclinaison.

Lecture Windows 95 : trois contrôles groupés réussis avec le bureau ; édition aller-retour, séparation Brut/Format, commande d’exécution et libération testées. Capture Read.png à 720×480 inspectée ; fenêtre de présentation non affichée et aucun HWND créé, scénario temporaire supprimé. Les commandes avancées restent à raccorder à leur dialogue Windows 95 avant intégration de la déclinaison.

Écriture Windows 95 : trois contrôles groupés réussis avec le bureau. Chemin, profil, choix de format, commandes existantes, état désactivé et détachement vérifiés. Capture Write.png à 680×390 inspectée ; scénario temporaire supprimé, aucun HWND créé. Aucun ajout au catalogue Windows 95, aucun remplacement du build Windows 3.1 et aucun commit ; restent notamment les dialogues avancés, conversion, exploration, visualisation et émulation Windows 95.

Propriétés avancées et conversion Windows 95 : 18 contrôles groupés réussis, incluant les trois consommateurs Windows 3.1 après déplacement du seul relais générique vers Theme/Common. Six scénarios Windows 95 réussis après correction des intitulés et du défilement ; contrôle ciblé supplémentaire du défilement exécuté avant fermeture de la fenêtre, puis capture de chaque onglet. Huit captures inspectées dans screens/OS/Windows95 (Conversion, ReadProperties, ReadNumbering, ReadExpert, WriteProperties, WriteExpert, ConversionProperties, ConversionExpert). Scénario temporaire supprimé ; aucune fenêtre native créée ni session d’émulation lancée. Les vues restent à intégrer au bureau avec les autres fonctions avant de proposer Windows 95 ; aucun build officiel remplacé, aucun commit.

Explorateur Windows 95 : quatre contrôles groupés réussis, puis contrôle ciblé de la zone des cases plus/moins après sa correction. Navigation et développement/repli au clavier, absence de case sur les feuilles, double clic gauche, colonnes redimensionnables, propriétés actualisées, menus cochés, progression et détachement vérifiés. Trois captures Explorer, ExplorerProperties et ExplorerLoading inspectées ; scénario temporaire supprimé et absence de HWND vérifiée pour chaque fenêtre de capture. Les icônes de contenu passent par le fournisseur existant ; les ressources historiques Windows 95 restent à intégrer avant enregistrement de la déclinaison. Aucun commit ni remplacement du build officiel.

Visualisation et outils Windows 95 : dix contrôles groupés réussis après correction des couleurs de sélection dans les modèles, du libellé des ComboBox et de la portée des textes générés. Les cinq représentations restituent leurs feuilles de dessin et le viewport de bande à leurs propriétaires ; carte logique et géométrie réellement dessinées avec le moteur existant dans des fenêtres invisibles hors écran, puis HWND détruits dans finally. Cinq captures inspectées (Visualization, VisualizationBlocks, VisualizationGeometry, ToolsErase, ToolsClean). Commandes des outils et arrêt/captions reliés aux points existants sans opération matérielle lancée. Deux scénarios temporaires supprimés. Restent notamment émulation, réglages, dialogues et ressources historiques avant intégration Windows 95 ; aucun commit ni remplacement du build officiel.

Présentation des machines Windows 95 : neuf contrôles réussis après correction des commandes Marche/Pause, qui suivent exclusivement l’état du contrôleur. Transfert/restitution de surface et des entrées, restauration après rejet, actions et disponibilité, trente périphériques dans la liste latérale, défilement, états tape, chargement shader et nettoyage après erreur vérifiés. Capture Emulation.png inspectée : machine arrêtée, aucun cœur lancé ; aucun démarrage Acorn ou rendu GPU réel affirmé par ce scénario. Scénario temporaire supprimé. Restent bibliothèque et réglages d’émulation, dialogues, ressources historiques et intégration du bureau Windows 95 ; aucun commit ni remplacement du build officiel.

Isolation finale des boutons Windows 95 : deux contrôles ciblés réussis pour le style nommé, les métriques classiques et la conservation des couleurs des pistes ; le déclencheur booléen de Marche/Pause a été corrigé puis revérifié séparément. Captures Emulation et VisualizationGeometry réactualisées et inspectées ; le style dérivé ne reprend plus les tailles/couleurs de Base. Scénario temporaire supprimé, HWND des captures natives détruits. Aucun commit ni remplacement du build officiel ; le travail global sur les thèmes reste en cours.

Windows 95 intégré : 214 contrôles groupés thèmes/localisation réussis, dont un scénario temporaire à deux contrôleurs conservant les propriétaires des sessions. Dialogues natifs, menus imbriqués, icônes Shell32 historiques, défilement de cent lanceurs et restitution des sources vérifiés ; captures dans themes-review/screens/OS/Windows95. Les scénarios temporaires ont été supprimés. Aucun démarrage de cœur réel ni validation de tous les GPU affirmé par ces scénarios. Aucun commit.

Build officiel Debug avec --modules=A terminé sans erreur (code 0) ; exécutable et 24 manifestes/DLL vérifiés. La revue complémentaire a identifié la garde Fermer des confirmations sans annulation, inscrite à la suite avant de livrer ce lot.

Livraison Windows 95 : dernier build officiel --modules=A réussi (code 0), retromedialab.exe et les 24 manifestes/DLL contrôlés après livraison. Les 214 contrôles groupés passent ; six contrôles ciblés supplémentaires passent après ajout de la garde des confirmations. Aucun scénario TemporaryWindows95 ne reste dans les sources. OS propose Windows 3.1 et Windows 95 ; Base et Studio conservés. Aucun commit, validation utilisateur toujours requise avant commit. Les autres déclinaisons OS et Orbit/Matrix/Atlas restent dans le périmètre global.

Contrôles Luna XP : les trois dictionnaires officiels sont chargés localement ; présence des éléments de dessin PresentationFramework.Luna, police Tahoma, métriques et restitution des ressources vérifiées. Planche LunaControls.png inspectée ; scénario temporaire supprimé. Les contrôles seuls ne constituent pas la déclinaison XP et ne sont pas enregistrés au catalogue. Aucun changement du build Windows 95 livré, aucun commit.

Découpes XP : les 66 ressources et chaque état ont été chargés, leurs dimensions contrôlées, images figées et réutilisées. La planche FrameSlices.png a été rendue et inspectée en états actifs/inactifs ; aucun HWND créé par ce contrôle. Scénario temporaire supprimé. Les ressources du média et leurs paramètres ont été conservés en PNG/JSON utiles ; les fichiers de style temporaires ont été supprimés. Le cadre natif, le bureau et les vues XP ne sont pas encore livrés.

Orbit : 176 contrôles existants de thèmes réussis, neuf scénarios temporaires de navigation/conversion et parcours des 24 modules réussis ; six scénarios supplémentaires de préférences, confirmations et transfert de deux sessions réussis. Captures dans artifacts/themes-review/screens/Orbit. Les fenêtres natives sont détruites et les sources empruntées restituées. Les deux scénarios temporaires finaux ont été supprimés. Aucun démarrage de cœur réel ni rendu GPU réel affirmé. Aperçus clair/sombre issus des captures de Lecture. Build officiel tous modules en cours ; aucun commit.

Contrôle final groupé Orbit : 212 tests existants de thèmes et localisation réussis après nettoyage des sources temporaires. Les six contrôles natifs/sessions ont également été relancés et réussissent. Aucun test temporaire Orbit ne reste dans les sources.

Livraison Orbit : scripts/local-building.cmd --building=debug --modules=A terminé avec code 0. Exécutable build/Debug/Retro Media Lab/retromedialab.exe présent ; 24 manifestes et leurs DLL vérifiés. lib/retromedialab.app.dll correspond exactement à la DLL compilée win-x64. 212 tests groupés réussis, plus scénarios temporaires décrits ci-dessus et supprimés. Aucun commit ; validation utilisateur attendue. Travail sur les thèmes arrêté après Orbit selon la demande explicite.
