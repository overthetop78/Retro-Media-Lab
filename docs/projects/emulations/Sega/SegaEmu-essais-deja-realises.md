# SegaEmu : essais déjà réalisés et décisions

Journal du 15 novembre 2026, demandé par l’utilisateur pour éviter de refaire les mêmes essais. Les valeurs ci-dessous décrivent leurs scénarios précis, pas tous les jeux. La référence binaire conservée contient déjà le 32X ; elle ne représente pas le code d’avant tout ajout 32X. Les documents et JSON liés donnent le protocole complet.

## Variantes à ne pas relancer à l’identique

| Essai | Périmètre et résultats observés | Décision et preuves |
| --- | --- | --- |
| ROM SH-2 empruntée, ordre little-endian et Span local de fetch | Chaotix, 600 trames : candidat complet 10,45 FPS Debug. Après retrait du Span local : 19,19 Debug / 86,08 Release ; état conservé antérieur : 19,70 / 90,79. Ensemble de changements, pas une preuve que tous les spans seraient lents. | Groupe retiré. `parallel-corrected-Debug.json`, `mapped-fetch-{Debug,Release}.json` ; [recherche](SegaEmu-32X-recherche.md). |
| Forçage AggressiveInlining de Sh2.step, propagé dans le runtime natif | Virtua Racing, 600 trames Debug : 11,02 FPS avec le forçage ; 19,01 après son seul retrait, référence 18,80. Les deux montages de DLL mixtes étaient également autour de 19,7–19,8. | Attribut retiré. `check-current-virtua-Debug.json`, `check-isolate-old-{0,1}-Debug.json`, `check-clock-inline-virtua-Debug.json`. Ne pas réintroduire ce forçage global comme si seuls les blocs étaient concernés. |
| Tables 68000 dans un détenteur non générique, tableaux readonly et atex_table VDP en tableau | Comparaison de l’ensemble courant : Aladdin Release 561,27 → 494,75 ; Batman Returns 308,02 → 294,30. Cette comparaison ne prouve pas que les tables seules causaient la perte. Leurs avantages Debug ne justifient pas de présenter le groupe comme validé. | Tables restaurées. `Cpu68000.Tables.cs` et `Vdp.Tables.cs` correspondent aux sources antérieures. `check-{baseline,current}-{md,mcd}-Release.json`. Attention aux mesures invalides après restauration : voir plus bas. |
| Sortie vidéo 32 bits par spans + suppression de RemapLineCore | Aladdin Debug 23,07 → 29,39 ; Batman Returns 23,04 → 26,23 ; Chaotix 15,39 → 16,56. En Release MCD : 255,94 → 242,05 (-5,43 %), puis essai d’ordre inversé 298,58 → 248,65 (-16,73 %). | Candidat retiré, malgré les passages Debug favorables. `video-output-{before,after}-*-{Debug,Release}.json`, `video-output-reverse-{before,after}-mcd-Release.json`. |
| Suppression de RemapLineCore seule, boucle de pixels originale conservée | Batman Returns : Debug 24,62 → 23,98 ; Release 308,75 → 288,61 (-6,52 %). Images et états sauvegardés identiques. | Variante retirée aussi. `video-output-retained-{before,after}-mcd-{Debug,Release}.json`. Vdp.Output.cs et Vdp.Rendering.cs remis exactement comme avant cette tentative. Pas de troisième variante lancée. |

La tentative vidéo concernait la conversion des pixels. L’ancien essai de Span local concernait le fetch SH-2 : ce ne sont pas le même code. Les deux essais ont néanmoins déjà été réalisés et rejetés dans les formes décrites ci-dessus. Cela ne justifie pas de supprimer les spans préexistants du projet.

## Variantes expérimentales encore présentes, sans validation de vitesse

| Variante | Résultats disponibles et statut |
| --- | --- |
| Switch / DelegateTable / IndirectTable SH-2 | Comparées ; aucun remplacement global du défaut Switch validé. Les trois routes restent dans Sh2.NativeRuntime.cs / Sh2.DirectDispatch.cs. `dispatch-*`, `polling-*`. Leur présence ne signifie pas qu’elles sont activées. |
| Modèle DirectMapped et blocs compilés | Chaotix, 300 trames de chauffe + 600 mesurées : DirectMapped sans blocs 18,38 Debug / 53,61 Release ; avec blocs liés 15,77 / 44,89. `linked-GensDirect-*` et `linked-GensDirectBlocks-*`. Plus lent sur ce cas. Défaut toujours Cached et UseCompiledBlocks=false. Ne pas relancer cette même comparaison en la présentant comme une idée neuve. |
| Préfixes compilés, barrière de code, liens de blocs, échéances MaximumWaitStates | Code présent, mais tous les chemins rapides ne sont pas déclarés intégralement validés. Les 55 tests de puces ne prouvent pas un gain de vitesse de ces routes. Voir les tâches encore ouvertes dans `docs/tasks/emulation/segaemu-block-recompiler.md`. Le scan en mode Cached peut préremplir le cache : ne pas annoncer des cycles identiques à l’interpréteur sans preuve. |

## Mesures invalides et erreurs à ne pas répéter

- Après copie des sources de tables sauvegardées, leurs dates anciennes ont laissé le build incrémental réutiliser les anciennes DLL. `check-retained-*` et la première relance `invalid-incremental-retest-*` ne valident donc pas les sources restaurées. La relance valide est `retest-*`, après reconstruction `--no-incremental`, avec SHA256 des DLL effectivement chargées vérifié dans chaque JSON.
- Des unités qui passent et des images identiques ne prouvent pas l’absence de perte de vitesse. Les états sauvegardés identiques avant/après ne prouvent pas non plus la reprise exacte de l’audio.
- Comparer le même jeu, même entrée, même fenêtre et même périmètre. Les campagnes de 60, 180, 600 ou 900 trames ne se mélangent pas ; séparer Debug/Release, machine seule/publication et froid/chaud.
- Ne pas attribuer une variation au SH-2 sur une machine sans SH-2. Le VDP partagé, lorsqu’il est modifié, concerne aussi les machines qui l’utilisent.
- Ne pas expliquer les pertes par le PC hôte sans mesure démontrant cette cause.

## État à conserver en tête

- Les raccordements 32X de Vdp.Output.cs (notification, largeur externe, indices et géométrie) sont utilisés par SegaBoard.32X.cs. Ils ne constituent pas à eux seuls des restes expérimentaux à supprimer. Les deux variantes vidéo ci-dessus ont été retirées et les DLL/PDB Chips reconstruites en Debug et Release.
- VdpOutputTests.cs ajoute dix contrôles autonomes en mémoire : couleurs fixes, LCD, bords, entrelacement, profondeurs et notification après écriture. Avec les tests existants : 55 puces + 28 intégration par configuration lors des validations des variantes. Aucun gain n’est déduit de ce nombre.
- Le CD32X Corpse Killer a déjà été exécuté jusqu’à 9 000 trames, avec captures de la vidéo 32X et composition MD, puis pause/reprise. Cela ne valide pas une partie complète ni les tirs. Voir [diagnostic CD32X](SegaEmu-CD32X-diagnostic.md) et les relevés `cd32x-*`. Ne pas repartir de « aucun CD32X disponible ».
- Le banc calcule le PCM en mémoire et n’ouvre pas le périphérique audio. L’usage exclusif audio d’un autre logiciel ne démontre pas la cause des différences de reprise PCM.
- Les régressions du dernier retest global restent consignées dans [le rapport détaillé](SegaEmu-32X-recherche.md#retest-demande--deux-versions-debug-puis-release). Les tentatives vidéo ultérieures n’ont pas réparé ces régressions.

Avant un nouvel essai, retrouver son entrée ici et les JSON associés. Une nouvelle variante doit identifier concrètement ce qui diffère d’un candidat déjà rejeté ; changer seulement son nom ou relancer les mêmes mesures ne constitue pas une nouvelle solution.

Les preuves citées sont sous `artifacts/Emulation/segaemu-32x/`. Aucun commit réalisé pour ce journal.


## Reprise en cours : horloges et acces cache

- Le groupe IO de largeur native a egalement deja ete essaye et retire : Switch Debug 15,57 FPS contre ancienne DLL 18,79. Cela incluait les acces IO mot/long et stores cache avec un seul lookup ; ne pas le reproposer comme nouveau. Preuve : docs/tasks/emulation/segaemu-soc-io.md.
- Nouveau bypass des lectures ROM 68000 de 32X sans synchronisation : Doom Debug 19,66 → 21,04, mais phase d’animation differente sur Chaotix. Les captures rom-sync-*-chaotix-Debug.png montrent ce decalage. Bypass retire pour ne pas confondre changement de calendrier et gain de cout. Seul le retour lorsque la cible egale l’horloge deja atteinte est conserve a ce stade.
- Nouveau chemin FRT avant echeance avec WDT/SCI/DMA inactifs : images et etats sauvegardes identiques. Doom Debug 17,47 → 18,10, Release 106,54 → 115,27 ; Chaotix 17,11 → 17,05 et 82,98 → 83,33 ; Virtua 19,76 → 15,29 et 85,63 → 84,46. Pas de gain global valide, notamment perte Virtua Debug. Preuves peripheral-*.json. Aucun reglage global d’inlining ni Span local ajoute.
- La nouvelle recherche hote memoisee du cache n’est pas le groupe IO mot/long retire : elle conserve les memes acces et operations, reutilise seulement un lookup si adresse de ligne et version de mapping sont inchangees. A mesurer en coeur seul puis machine avant toute decision.

Premier prototype lookup avec helper LookupLine : coeur seul 20 millions de cycles, deux passages d’ordre inverse. Debug avant 76,95/75,43 Mcycles/s, apres 76,42/70,36 ; Release avant 80,41/78,12, apres 76,35/83,21. Aucun gain net etabli ; aucune campagne machine lancee pour cette forme. Releves archives cache-core-first-*.json. La forme suivante retire cet appel ajoute ; elle doit etre mesuree avant toute conclusion.

La seconde forme du lookup, sans helper ajoute, ne montre pas de gain net non plus : Debug avant 67,08/75,15 contre apres 73,15/73,42 Mcycles/s ; Release avant 89,61/94,45 contre apres 88,20/86,39. Aucun jeu lance pour ces lookup. Les deux formes et le raccourci FRT sont retires ; Sh7095.cs restaure depuis peripheral-before-Sh7095.cs. Les tests de comportement restent.

Nouvelle vue ROM SH-2 preparee au chargement : elle se distingue du candidat ROM little-endian empruntee/Span local de fetch deja retire. Ici, la boucle CPU et son mapping big-endian a quatre arguments restent inchanges. Une copie big-endian avec les memes miroirs elimine le callback par instruction ; cout memoire prevu 4 Mio pour une cartouche 32X paire, pas pour MD/MCD seuls ni CD32X sans cartouche. Validation a faire avant tout gain annonce.


### Vue ROM big-endian : premiere forme non validee globalement

Les octets, attentes, images et etats sauvegardes sont identiques entre versions. Les releves rom-view-* donnent, avant → apres : Doom Debug 19,08 → 19,66 et Release 83,12 → 94,68 ; Chaotix 16,89 → 16,96 et 62,17 → 61,75 ; Virtua Racing 18,48 → 13,06 et 74,73 → 71,50. La perte Virtua Debug de 29,33 % interdit de presenter cette forme comme une amelioration globale. Ne pas relancer cette forme identique.

Cette forme utilisait la vue preparee aussi dans Read8, Read16Core, Read32 et Fetch32. Les tailles IL relevees dans rom-view-il-sizes.json : Fetch32 88 → 175, Read32 151 → 245, TryMapFetch 158 → 241. Le seuil de forçage 64 octets n’est pas franchi dans Fetch32 (deja 88 avant) ; aucune cause d’inlining n’est donc prouvee par ce seul constat. Il reste a isoler le mapping d’instructions des chemins donnees/remplissage de cache avant tout autre essai, sans toucher a la boucle CPU ni aux spans locaux.

Les raccourcis FRT et les deux formes de lookup hote ont bien ete retires du code de production ; le fichier Sh7095.cs est restaure. Aucun de leurs gains locaux ne doit etre cite comme une amelioration de la version courante.

## Diagnostic des constantes Debug et isolation de la vue ROM

Le profil Doom attribue aux piles RuntimeHelpers.CreateSpan environ 44,26 % des echantillons du thread principal via InstructionMap du 68000 et 11,17 % via atex_table du VDP. Ce sont des echantillons de profil, pas une prediction du gain FPS. Les threads finalizer et les noms dynamiques ambigus ne servent pas a attribuer une perte a un composant.

Correction en Debug seulement : NativeLiteralAccessor compile une fois un getter pur vers le meme champ RVA immutable. Il accepte uniquement la forme IL ldtoken/CreateSpan/ret, verifie toutes les valeurs et garde le getter original en repli. Aucun tableau transforme ou copie. Les deux getters natifs font chacun 24 octets, sans appel CreateSpan par acces. Les chemins Release restent originaux. Preuves : literal-access-probe-Debug.*, literal-native-Debug.asm et LiteralAccessTests.cs. Cela differe du candidat tableaux alloues/non generiques deja retire.

### Campagne avec accesseurs Debug et vue ROM limitee au fetch

Machine complete, sans fenetre ni limiteur, PCM calcule en memoire. 32X : 600 trames depuis demarrage ; MD/MCD : 180 trames Debug ou 900 Release apres 60 de chauffe. Meme jeu, entree et nombre de trames pour chaque paire. DLL chargees verifiees.

| Jeu | Debug avant → apres | Release avant → apres |
| --- | --- | --- |
| Doom 32X | 18,68 → 21,51 | 113,28 → 126,80 |
| Knuckles Chaotix | 17,84 → 19,78 | 81,44 → 83,51 |
| Virtua Racing Deluxe | 13,88 → 17,62 | 89,59 → 70,43 |
| Aladdin MD | 28,90 → 30,24 | 590,60 → 524,81 |
| Batman Returns MCD | 25,79 → 29,41 | 328,28 → 242,46 |

Images finales et fichiers de sauvegarde avant/apres identiques dans les dix paires. La reprise PCM ne devient pas exacte : ecart 736/735 echantillons sur certains jeux, deja consigne avant. Ne pas presenter ces controle video/etat comme validation complete de la reprise audio.

### Retrait temporaire de la vue pour isoler les accesseurs

Meme reference reutilisee, memes scenarios, accesseurs seuls, sans vue ROM et avec bus/ordonnanceur restaure :

| Jeu | Debug avant → apres | Release avant → apres |
| --- | --- | --- |
| Doom 32X | 18,68 → 13,86 | 113,28 → 109,61 |
| Knuckles Chaotix | 17,84 → 16,28 | 81,44 → 85,70 |
| Virtua Racing Deluxe | 13,88 → 21,94 | 89,59 → 86,40 |
| Aladdin MD | 28,90 → 32,53 | 590,60 → 612,22 |
| Batman Returns MCD | 25,79 → 31,13 | 328,28 → 209,46 |

Toutes les images et sauvegardes restent identiques. Ces releves ne permettent pas de promettre les gains de la premiere campagne pour les seuls accesseurs. Ils sont conserves sans cacher les baisses Doom/Chaotix de cette passe.

Controle executable Release : les 13 954 methodes Chips et les 8 793 methodes Sega sans vue ont exactement les memes signatures, corps IL et attributs que la reference. Les variations Release de cette passe ne doivent donc pas etre attribuees aux accesseurs Debug ou a une route ROM absente. Cela ne designe pas une cause de variation et ne constitue pas un diagnostic du PC hote. Preuves literal-release-il-comparison.json et literal-release-sega-il-comparison.json.

### Vue ROM active/inactive avec exactement les memes DLL

La premiere conclusion de retrait sur le seul 89,59 → 70,43 etait prematuree. Controle Virtua Racing Release sur 600 trames, memes DLL Chips et Sega, seul le champ de vue est vide avant la premiere trame dans le cas inactif. Taille effective 0 contre 4 194 304 octets verifiee dans chaque JSON. Second passage dans ordre inverse :

| Passage | Vue inactive | Vue active | Ecart actif/inactif |
| --- | --- | --- | --- |
| inactive puis active | 89,13 | 90,14 | +1,14 % |
| active puis inactive | 93,12 | 90,90 | -2,38 % |

Les quatre images et sauvegardes sont identiques. La baisse de 21,39 % ne se reproduit pas a activation/desactivation de la vue. Ce controle isole son activation ; il ne retire pas le champ et la branche du code compile, donc il ne prouve pas que leur integration ne coute rien. Aucun profil supplementaire lance puisque cette perte marquee ne se repete pas dans ce controle. Ne pas refaire le premier rejet sans tenir compte de cette distinction.

Etat final : variante fetch seule remise, callbacks et attentes originaux conserves, methodes Read8/Read16Core/Read32/Fetch32 originales ; raccourci d'horloge identique deja atteint conserve. La vue coute 4 Mio par cartouche 32X paire, sans copie pour MD/MCD seuls ou CD32X sans cartouche. Pas de suppression de fonctionnalite. Les corps IL de Sega Release reconstruit sont identiques aux 8 795 methodes du candidat effectivement mesure : literal-restored-il-comparison.json. La variante reste a juger sur ses scenarios ; aucune acceleration globale stable n'est declaree.

Validations finales : 62 tests puces Debug et 60 Release, 30 tests integration dans chaque configuration. Les deux tests supplementaires Debug verifient toutes les valeurs et la creation effective des getters natifs. Les tests de lectures ROM/mirroirs/attentes et mapping sont conserves.

Preuves consolidees : literal-comparisons.json contient toutes les paires, les SHA256 des DLL, les images/etats identiques et les empreintes des fichiers bruts avant nettoyage. Les JSON et journaux literal-*, literal-only-* et rom-view-isolated-* restent. Aucun commit ni push.