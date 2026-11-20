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
### Lot natif MD/MCD du 16 novembre : invalidations Z80 et bus 68000

Voir la fin de `SegaEmu-tests-autres-jeux.md` et `attempts/native-protected-memory/summary.json`.

À conserver : variantes natives Z80 par adresse/premier opcode, cache borné globalement à 4 096 ; cycles en registre hôte avec publication/relecture autour des callbacks ; timings du compilateur réutilisés ; lectures PC-relatives mappées ; ordre bas/haut MOVE.L pré-décrémenté ; avancement intermédiaire PC/préfetch MOVE mémoire vers mémoire et LINK ; N/Z logiques depuis résultat tronqué. Run interprétées inchangées. 9 737 alternances EXX/JR à 0x02B7 expliquaient la majorité des constructions Aladdin : 10 527 → 781 blocs sur le passage Debug. Aucune éviction : agrandir ou réorganiser le cache n'aurait pas traité cette cause. Seulement 14 invalidations de suffixe dans le profil ; ne pas les désigner comme les 9 765 invalidations totales.

Essai rejeté : service externe SKIP_BUS_REFRESH à chaque point d'exécution, au lieu de l'échantillonnage, ne corrige pas le compteur secondaire. Retiré, ne pas refaire à l'identique. L'écart refresh_cycles Mega-CD +11 264 reste réel et nouveau ; images/audio égaux et autres champs CPU identiques ne le suppriment pas. Reprendre l'interaction principal natif/secondaire natif, car principal interprété/secondaire natif correspond. Ne pas certifier l'identité complète des états MCD.

Dernier passage : Debug Aladdin I31,82/N36,70, Batman I29,88/N36,66 ; Release Aladdin I547,88/N670,24, Batman I328,03/N390,75. L'ordre inversé et les témoins DLL antérieures sont consignés dans le rapport, sans cacher leur variation. GPU complet toujours absent ; 32X différé. Aucun commit.

Contrôle des DLL Sega finales reconstruites : sorties des deux routes inchangées par rapport aux relevés précédents. Derniers FPS Debug Aladdin I32,08/N34,35, Batman I29,04/N36,32 ; Release Aladdin I496,27/N603,89, Batman I399,06/N404,30. Les témoins variables et les empreintes exactes sont conservés dans module-final-* et summary.json/finalModuleRows. Ne pas substituer aux DLL finales les chiffres d'une étape précédente.

# Correction du compteur Mega-CD +11 264 — SetIrqDelay et IR final

La cause prouvée était le coût de l'instruction principale après le callback SetIrqDelay, pas l'échantillonnage SKIP. Le service exécute le prochain opcode et laisse IR modifié. À la trame 4, MOVE.W 0x33C1 suivi de BRA produisait +42 cycles natifs : le bloc facturait encore le MOVE au lieu de IR final comme Run. Le bus natif conserve maintenant le coût final et le bloc applique l'ajustement seulement après tous ses accès. Ne pas l'appliquer dans le callback : les accès suivants d'une instruction longue verraient les cycles avancés trop tôt.

Après correction : 15 514 points d'instruction de la trame 4 identiques ; 61 fins de trame principales identiques ; compteur secondaire trame 61 = 1 024 dans les deux routes. 96 cas SetIrqDelay autonomes passent en Debug/Release, avec tailles, plusieurs writes, préfetch, multiplicateurs et ratios. Aladdin et Batman Returns ont images/audio/échantillons et trois états CPU normalisés identiques dans les deux configurations. Voir `irq-fixed-*` et `interaction-first-difference.json`. L'avertissement antérieur sur refresh_cycles est résolu ; conserver les anciens essais comme historique et ne pas refaire le service SKIP rejeté.

Derniers FPS : Debug Aladdin I32,10/N33,68, Batman I28,85/N32,73 ; Release Aladdin I522,51/N646,67, Batman I311,98/N356,28. Empreintes finales vérifiées ; Run/SetIrqDelay interprétés inchangés ; instrumentation retirée ; aucun commit. L'opcode imbriqué du service partagé utilise encore le handler existant : son portage natif et le GPU complet restent à terminer.

# Service IRQ natif et sorties rares IR — 16 novembre

À conserver : SetIrqDelayNative, un bloc x64 dédié à une instruction avec coût de table différé, sans budget/arrêt/prev_pc/refresh de boucle, cache borné 512 et libération différée. Premier opcode préfetché respecté ; SetIrqDelay et Run interprétés inchangés. 65 536 opcodes dans chaque configuration et 927 contrôles ciblés finaux passent. Les deux jeux MD/MCD ont images, PCM, échantillons et états CPU normalisés identiques aux témoins ; le service IRQ n'a aucun repli dans ces passages. Les replis ordinaires pour code modifié restent distincts.

Rejeté : sorties froides mutualisées par coût de table. MCD Release 337,80/365,52 contre 405,60/390,21 avant dans les deux ordres ; le gain Debug Aladdin ne suffit pas. Remplacées par une seule sortie rare, delta calculé dans le callback puis appliqué après le corps. Le delta antérieur est conservé pour remplacer correctement le changement net d'un callback imbriqué plutôt que le compter deux fois. Ne pas réintroduire les stubs par coût à l'identique.

Attention aux conclusions FPS : Aladdin Debug 18,68 dans une passe revient à 31,47 avec la même DLL avant le déplacement des sorties. Ne pas attribuer toute cette variation au correctif ni au PC hôte. Derniers relevés : Debug Aladdin I31,63/N35,88, Batman I28,87/N31,74 ; Release Aladdin I566,39/N625,03, Batman I286,63/N380,99. Les campagnes/ordres témoins restent séparés sous `attempts/native-delayed-irq` et dans le rapport détaillé.

Banc : forcer les références par HintPath dans AssemblySearchPaths. Les DLL témoins copiées dans un sous-dossier du projet avaient été candidates à la compilation d'un rebuild ; les erreurs de méthode absente ne venaient pas des fichiers source courants. Corrigé et SHA des DLL chargées vérifiés. Supprimer le banc et ses bin/obj après usage. Aucun commit ; GPU complet non implémenté, 32X différé.

## 16 novembre — gardes natives, préfetch et contrôle des baisses

- Retenu pour correction fonctionnelle : compiler le premier opcode réellement préfetché et relancer le natif après garde périmée. 1 152 contrôles par configuration, sorties/CPU MD/MCD identiques ; replis MCD main/sub 1/1 → 0/0 Debug, 3/9 → 0/0 Release.
- Ne pas conclure un gain de vitesse : la première passe MCD Release 363,79 → 310,92 ne se répète pas dans le contrôle inversé 357,09 → 366,23. Conserver les deux dans le rapport ; aucune attribution à la charge hôte ni à un seul détail du code.
- La première limite du faux bus à 256 consultations était incorrecte : initialisation de 256 pages ; limite du banc portée à 1 024, tests réussis.
- Ne pas réintroduire les sorties froides par coût déjà rejetées. Ce correctif conserve la sortie froide unique du service IRQ natif.
- Preuves : `artifacts/Emulation/segaemu-autres-jeux/attempts/native-fetch-guards/summary.json` et rapport SegaEmu-tests-autres-jeux.md. La route GPU complète reste absente.

## 16 novembre — offsets CLR du compilateur 68000

- Rejeté : FrozenDictionary pour StateOffset. Contrôle Release 199,18 → 323,82 ms sur 2,9 millions de consultations. Retiré avant les jeux ; ne pas refaire cette variante comme gain acquis.
- Retenu : M68000StateField et tableau des 29 offsets CLR calculés une fois, tous les émetteurs adaptés. Offsets et couverture des champs vérifiés dans les deux configurations ; accès seuls Debug 97,81 → 16,64 ms, Release 76,45 → 6,08 ms, zéro allocation du chemin typé. Cela concerne le compilateur et ne constitue pas un facteur FPS machine.
- Même image/PCM/échantillons/CPU MD/MCD avant/après. Dynarec Debug Aladdin 37,22 → 36,79, Batman 34,81 → 34,59 ; Release 650,94 → 672,51 et 375,96 → 380,79. Les témoins interprétés varient aussi : ne pas attribuer toutes les différences à ce changement. Les fichiers interpréteurs restent inchangés.
- Profils séparés : compilation secondaire MCD Release 139,66 → 103,77 ms, mais Debug 110,52 → 121,10 ms. Garder les deux ; ne pas promettre une baisse générale. Détails et tableaux complets dans SegaEmu-tests-autres-jeux.md ; preuves sous attempts/native-runtime-cost. GPU complet toujours absent, 32X différé, aucun commit.

## 16 novembre — contrôle natif sur quatre jeux supplémentaires

- Sonic 2 / Streets of Rage 2 : 1 200 trames ; Final Fight CD / Dragon Lair : 2 400 trames, dans chaque configuration. À chaque trame : CPU principal/secondaire/Z80 normalisés, image et PCM identiques entre interpréteur et natif ; zéro repli 68000 ordinaire/IRQ. Les checkpoints et PCM correspondent aussi entre Debug/Release.
- Debug I/N : Sonic 31,40/35,19 ; Streets 32,11/35,21 ; Final Fight 23,77/29,24 ; Dragon Lair 28,43/33,62. Release I/N : 399,13/435,41 ; 509,94/552,10 ; 394,88/400,68 ; 442,92/499,78. Le +1,5 % Final Fight Release reste une faible marge mesurée, pas une garantie.
- Aucun nouveau défaut du cœur découvert ; pas de modification de production dans ce groupe. Les mêmes entrées sont appliquées aux deux machines séparées d’un processus, seuls RunFrame sont chronométrés. Ne pas réattribuer ces FPS au protocole ancien ni à une autre DLL.
- Hypothèse initiale de blocage Sonic non confirmée : processus terminé normalement avant collecte, aucune trace. Pour dotnet-trace collect Windows version 10, profil dotnet-sampled-thread-time ; cpu-sampling est réservé à collect-linux. Ne pas reprendre cette hypothèse comme un défaut SegaEmu.
- Preuves : attempts/native-game-coverage/summary.json et tableaux du rapport. Banc supprimé après finally ; GPU complet absent, 32X différé, aucun commit.

## Passe de code seule : registres invités et liens directs

À la demande explicite de l’utilisateur, cette passe se termine sans nouvelle compilation ni campagne de vérification après les dernières modifications. Aucune vitesse ou réussite fonctionnelle nouvelle n’est déclarée.

Écriture dans dix fichiers du backend : option UseNativeGuestRegisters, D0/D1/A0/A7 conservés dans R12/RDI/RSI/RBX sur les chaînes ; publication/relecture aux callbacks et sorties ; adaptation du préfetch, des accès partiels et de la frame Win64, y compris les blocs IRQ imbriqués. La convention native précédente reste la valeur par défaut. Le changement de convention est refusé pendant une exécution et vide les deux caches avant un changement autorisé.

Les destinations connues utilisent des slots de pointeurs stables, invalidés avant libération des buffers. Les pages sont désormais vidées lors des invalidations si des liens directs les empruntent ; garder le simple effacement paresseux de la racine aurait laissé des pointeurs périmés. Les branches directes préfèrent leur destination au fallthrough ; les sauts inconnus conservent la table générale.

Référence : BlastEm m68k_core_x86.c, allocation des registres et save/load context, et sa carte de destinations natives. Adaptation au contexte SegaEmu : quatre registres invités conservés, pas les huit de BlastEm. Coûts déplacés à mesurer : PC/préfetch reviennent en mémoire, lectures du code avec adresse native explicite et contexte publié plus large aux callbacks. Aucun gain présumé. Avant toute activation par défaut, les validations CPU et jeux Debug/Release restent à effectuer. Sources et DLL précédentes conservées sous attempts/native-guest-registers/before* ; aucun commit.

## Passe de code seule : gardes ROM et invalidation de contenu

Nouvelle option CPU UseNativeStableCode, indépendante de UseNativeGuestRegisters, désactivée par défaut. Le contrat ICpu68000StableCodeBus impose une version couvrant contenu/mapping/politique ; les blocs certifiés sont supprimés et recompilés à tout changement de version, même si la fenêtre mémoire est identique. Le premier opcode conserve sa garde pour respecter un préfetch issu du mapping précédent ; les gardes répétées des opcodes suivants et des extensions sont omises uniquement pour une fenêtre certifiée.

SegaEmu certifie les tableaux ROM appartenant au CartridgeState courant, sans alias directement inscriptible. Les identités sont enregistrées dans une ConditionalWeakTable ; le retrait ou redimensionnement du tableau invalide la garantie. Les copies/remplissages des mappers, resets et restaurations de contexte notifient la version. Flash, SVP, lock-on et matériels spéciaux non certifiés restent gardés ; les BIOS et RAM Mega-CD ne sont pas déclarés immuables par cette passe. Les blocs IRQ dédiés restent gardés également. La RAM conserve les contrôles par instruction.

Un compteur NativeStableBlockBuildCount permet une future mesure de la couverture réellement obtenue. Les classifications des alias inscriptibles sont réutilisées tant que la version ne change pas. Aucun build ni test de cette passe : aucune conformité ou accélération nouvelle annoncée. Le contrôle Debug antérieur de 132 352 cas concernait les registres invités/liens, avant ces modifications ROM, et ne valide pas celles-ci. Il a été lancé contrairement à la demande de code seul ; aucune nouvelle vérification ne sera lancée sans reprise autorisée.

Le banc NativeGuestRegisters et ses bin/obj sont supprimés après la fin déjà confirmée de son processus ; les JSON de résultats et DLL de référence restent sous attempts/native-guest-registers. Pas de nouveaux FPS, pas de commit. Validation future requise : transitions ROM/RAM et préfetch de mapping précédent, copie in situ sans changement de fenêtre, alias inscriptible, restauration/reset, instructions dont un callback change le mapping, deux options séparées et combinées, puis machines MD/MCD Debug/Release.

Complément de la même passe ROM, sans build/test : le préfetch émis avant les accès données est maintenant une valeur immédiate pour les fenêtres certifiées stables. Les lectures tardives effectuées par les émetteurs après un callback restent des lectures réelles ; aucune constante ne remplace ces complétions de fetch. Le préfetch existant du premier opcode garde son contrôle. Pas de nouvelle mesure de vitesse.

Complément firmware MD/Mega-CD, toujours sans build/test : les chargements BIOS sont entourés par un propriétaire de publication FirmwareWrite. Il retire la certification avant écriture, puis la rétablit seulement après Complete et Dispose dans le using/finally. Une exception, un chargement incomplet ou refusé laisse la fenêtre non certifiée. Cela couvre le BIOS CD chargé par le fournisseur, le BIOS CD fourni comme ROM et le BIOS de démarrage Mega Drive, après conversion des mots et miroir. Les RAM programme/Word RAM Mega-CD restent gardées ; aucun gain FPS déclaré. Un BIOS ne doit pas être assimilé à un jeu « stable » : seule la stabilité des octets de sa fenêtre mémoire entre deux révisions est engagée.
## Validation neuve ROM/registres — 16 novembre

659 contrôles réussis par configuration ; CPU/image/PCM correspondent sur Aladdin MD et Batman Returns MCD, quatre combinaisons natives, interpréteur actuel remesuré. Tableaux détaillés et contre-contrôles : section « Validation neuve des routes ROM et registres » de SegaEmu-tests-autres-jeux.md ; preuves sous attempts/native-rom-routes.

Ne pas recommencer sans diagnostic : registres seuls ralentissent légèrement la boucle CPU Debug ; combiné ne bat pas registres seuls sur Batman Release. Aucune variante ne gagne partout. La chute initiale Aladdin natif Release 353,22 FPS ne se reproduit pas : 619,39 avec les mêmes DLL au contre-contrôle. Ne pas présenter cette chute comme une régression établie, ni effacer sa mesure initiale. Les témoins interpréteur Debug MCD montrent une petite baisse persistante, pas une invariance parfaite.

Le cœur isolé est fortement accéléré ; le gain machine ne suit pas ce facteur. Profils corps/finalisation séparés, temps CPU inclusifs : pas de cause vidéo exclusive démontrée. Investiguer les coûts machine restants plutôt que répéter les mêmes petites retouches CPU. Les options restent désactivées par défaut, GPU complet absent, 32X différé. Aucun commit/push.


# Toutes les machines SegaEmu — 17 novembre 2026

Nouvelle campagne sur les DLL du raccordement Z80/SH-2, sans ancien FPS reutilise. Dix configurations, un jeu chacune ; cartouches SG compatibles pour SC-3000, cartouche SMS pour Mark III. Deux machines fraiches creees par passage, interpreteur puis route native a chaque trame. 60 trames de chauffe, puis 180 mesurees Debug et 900 Release. Seul RunFrame est chronometre ; chargement, comparaison et hashes sont exclus. Start pendant cinq trames tous les 120 a partir de 120. Pas de fenetre, limiteur ni sortie audio hote. Les passages Debug et Release ont donc des longueurs differentes ; ils ne prouvent pas le cout du meme segment entre configurations.

Chaque variante a son propre temoin interpreteur neuf, conserve dans les JSON. La colonne Interpreteur ci-dessous reprend le temoin du passage natif existant ; les autres colonnes sont des passages distincts et ne doivent pas etre compares uniquement a ce temoin pour annoncer un pourcentage. Night Trap Release utilise un passage interpreteur seul complet distinct, les quatre comparaisons ayant echoue. Les options Registres/ROM stable/Combine concernent les 68000 ; sans objet sur les machines a Z80 seul. GPU complet non implemente sur toutes les lignes.

## Debug — FPS

| Jeu | Plateforme | Interpréteur | Dynarec existant | Registres | ROM stable | Combiné |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 166,89 | 84,86 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 192,59 | 91,47 | Sans objet | Sans objet | Sans objet |
| Sonic the Hedgehog | Mark III | 151,67 | 54,93 | Sans objet | Sans objet | Sans objet |
| Sonic the Hedgehog | Master System | 167,28 | 58,44 | Sans objet | Sans objet | Sans objet |
| Sonic the Hedgehog | Game Gear | 242,98 | 59,32 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 36,16 | 39,41 | 42,08 | 44,25 | 40,99 |
| Aladdin | Mega Drive | 29,47 | 33,20 | 32,58 | 35,73 | 36,12 |
| Batman Returns | Mega-CD | 31,21 | 37,26 | 34,84 | 38,20 | 35,45 |
| Doom | 32X | 20,02 | 15,82 | 16,81 | 16,93 | 16,35 |
| Night Trap, disque 1 | Mega-CD + 32X | 30,10 | 32,83 | 33,04 | 31,27 | 31,53 |

## Release — FPS

| Jeu | Plateforme | Interpréteur | Dynarec existant | Registres | ROM stable | Combiné |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 3175,02 | 851,74 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 2947,00 | 856,22 | Sans objet | Sans objet | Sans objet |
| Sonic the Hedgehog | Mark III | 2490,22 | 518,35 | Sans objet | Sans objet | Sans objet |
| Sonic the Hedgehog | Master System | 2475,49 | 544,61 | Sans objet | Sans objet | Sans objet |
| Sonic the Hedgehog | Game Gear | 2396,01 | 425,07 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 553,28 | 608,58 | 681,62 | 619,53 | 621,09 |
| Aladdin | Mega Drive | 526,74 | 529,43 | 485,25 | 575,91 | 576,49 |
| Batman Returns | Mega-CD | 342,27 | 364,85 | 347,99 | 413,68 | 369,14 |
| Doom | 32X | 123,80 | 5,96 | 5,99 | 5,89 | 5,72 |
| Night Trap, disque 1 | Mega-CD + 32X | 130,39 | Échec : trame 720 | Échec : trame 720 | Échec : trame 720 | Échec : trame 720 |

## Resultats, echecs et limites

46 passages compares reussissent ; les quatre variantes Night Trap Release echouent au controle CPU/RAM de la trame 720. Images et PCM correspondaient jusque-la ; cela ne permet pas de valider le passage complet. Les nombres FPS partiels des JSON en echec ne sont pas des mesures de 900 trames et ne sont pas reutilises dans les tableaux. Le passage interpreteur seul Night Trap Release termine les 960 trames.

Toutes les trames comparees sont controlees pour pixels et PCM, et les etats CPU/RAM sont verifies tous les 60 frames : Z80, registres/indicateurs utiles des 68000 et contextes SH-2, RAM principale/Z80 et SDRAM 32X. Ce ne sont pas des validations completes de jeux, sauvegardes, entree utilisateur ou tous les peripheriques. Le court passage Night Trap Debug precede le demarrage des SH-2 : ne pas le presenter comme une validation des deux SH-2 CD32X. Ils fonctionnent ensuite dans le passage Release en echec.

Le raccordement utilise les backends existants : 68000 et Z80 x64, SH-2 a blocs CIL avec petit prefixe x64. Il ne constitue pas un SH-2 entierement traduit par le backend x64. Sur Doom Release, le master construit 32 421 blocs, execute zero prefixe x64 ; le slave 158 blocs et un prefixe x64. Le 68000 principal releve 11 184 526 instructions de repli dans le passage natif existant. Night Trap natif Release avant echec : master 1 321 blocs et 28 949 prefixes x64, slave 22 blocs et zero prefixe ; main 6 710 550 replis. Les options 68000 ne corrigent pas le cout SH-2.

A retenir : raccordement selectionnable et interpreteur conserve par defaut, controles concordants des passages reussis, aucune promotion automatique des nouvelles variantes. A corriger : forte perte des routes Z80 sur SG/SC/MarkIII/SMS/GG ; tres forte perte 32X Release ; divergence CD32X au demarrage des SH-2. Ne pas presenter cette campagne comme une acceleration generale.

Diagnostic du processus Doom Release existant, sans relance : environ 98,17 % du temps echantillonne exclusif dans CompileBlock. Trace bornee de cinq secondes et rapport topN conserves. Cela prouve le cout observe de compilation dans cet echantillon, pas une cause de tous les ralentissements ni un gain theorique apres correction.

Isolement Night Trap : SH-2 compiles sans petit x64 divergent encore a 720 ; SH-2 interpretes avec les autres CPU natifs correspondent a 720. Le probleme est donc dans la voie SH-2 compilee sur ce passage. L essai de decodage uniquement dans les fenetres mappees, sans Fetch16 anticipe, ne corrige pas seul le probleme : divergence plus fine relevee a 677, CPU master/cache/SDRAM. Cet essai est retire, sa source et ses JSON sont conserves ; ne pas le refaire seul comme une correction complete. Les sources/DLL des tableaux sont revenues exactement aux empreintes mesurees, verifiees dans les deux configurations.

Travail suivant concret : mesurer les couts de callbacks et remappages Z80 (le bus huit bits ne fournit actuellement aucune version rapide ni fenetre de donnees native) ; distinguer constructions uniques, invalidations et evictions SH-2 avant de modifier le cache de blocs ; corriger le decodage et les limites temporelles des blocs SH-2 ensemble, sans effets du compilateur sur le cache materiel ; exposer les fenetres de code 68000 transparentes hors registres/overlay 32X pour eviter le repli de banques entieres. Ces pistes restent a realiser et a valider, pas des gains annonces. Aucun changement de vitesse retenu sur une simple hypothese.

Builds cibles Chips/Sega Debug et Release reussis. Empreintes chargees controlees pour les 50 passages contre les DLL finales. Preuves : attempts/native-all-machines/summary.json, JSON par configuration/machine/route, diagnostic-*, cache-fix-*, first-difference-*, cache-decode-trial.cs.txt, trace et logs. Les premieres donnees sont aussi conservees sous before-cache-decode. Aucun commit/push.

# Projection memoire native Z80 — 17 novembre 2026

Correction du raccordement huit bits : le Z80 natif ne disposait ni de version rapide des mappings ni de fenetres de donnees. Chaque callback reappliquait les 64 projections fetch/read/write ; les lectures et ecritures simples traversaient aussi les handlers. BytePageMap conserve son constructeur public existant et ajoute un callback au remplacement effectif de fenetre. Les deux vues cartouche et la vue de la carte notifient la meme revision. Le token natif inclut aussi le materiel et la politique de handlers ; une bascule vers une lecture speciale retire donc les fenetres directes meme si les pages ne changent pas.

Lectures simples directement mappees ; bus ouvert conserve hors RAM. Stores directs uniquement pour le handler sans mapper et les pages Sega hors page 63, qui contient FFFC..FFFF. Autres mappers et lectures specialisees conservent les handlers. La RAM mutable conserve les gardes de code existantes ; son contenu ne provoque pas une reconstruction de toutes les projections. Les methodes Run interpretees ne sont pas modifiees. Aucun changement de selection par defaut.

Deux machines fraiches par titre/configuration, interpreteur puis natif a chaque trame, memes medias et entrees que la campagne native-all-machines : 60 trames de chauffe puis 180 mesurees Debug ou 900 Release. Seuls les appels RunFrame sont chronometres ; pixels et PCM compares a chaque trame, CPU normalises/RAM/LastFetch/EA tous les 60. Les chiffres suivants sont tous remesures, y compris les temoins interpreteurs. Cette passe compare la route native existante ; les options 68000 registres/ROM/combine ne sont pas modifiees ni remesurees.

## Debug — FPS

| Jeu | Plateforme | Interpreteur neuf | Natif | Ecart natif / interpreteur |
| --- | --- | ---: | ---: | ---: |
| Choplifter | SG-1000 | 189,38 | 256,89 | +35,6 % |
| Choplifter | SC-3000 | 179,80 | 240,67 | +33,9 % |
| Sonic | Mark III | 181,57 | 222,28 | +22,4 % |
| Sonic | Master System | 195,25 | 244,09 | +25,0 % |
| Sonic | Game Gear | 228,80 | 294,77 | +28,8 % |
| Aladdin | Mega Drive | 30,04 | 33,80 | +12,5 % |
| Batman Returns | Mega-CD | 31,06 | 37,66 | +21,2 % |

## Release — FPS

| Jeu | Plateforme | Interpreteur neuf | Natif | Ecart natif / interpreteur |
| --- | --- | ---: | ---: | ---: |
| Choplifter | SG-1000 | 1773,64 | 1803,81 | +1,7 % |
| Choplifter | SC-3000 | 2588,41 | 2607,95 | +0,8 % |
| Sonic | Mark III | 2253,34 | 1812,59 | -19,6 % |
| Sonic | Master System | 1972,63 | 2178,78 | +10,5 % |
| Sonic | Game Gear | 1931,03 | 1197,34 | -38,0 % |
| Aladdin | Mega Drive | 572,09 | 629,48 | +10,0 % |
| Batman Returns | Mega-CD | 324,25 | 348,96 | +7,6 % |

## Resultats et points encore ouverts

Les 14 passages principaux correspondent en CPU/RAM, images et PCM aux points verifies. Les 32 controles synthetiques passent dans chaque configuration : banques Sega, registre de controle conserve, bank switch identique sans changement de version inutile, RAM miroir, programme changeant de banque, code RAM puis immediat modifie, handler special et bus ouvert. Le premier banc avait un budget absolu inferieur au compteur de reset : aucun opcode ne tournait dans le programme synthetique. Le banc remet les deux compteurs a zero avant ce test ; aucune panne du coeur ne doit etre deduite de checks-initial-clock-Debug.json.

A conserver : les projections natives et leur invalidation, les handlers speciaux, les sorties controlees. Fort gain face a la route native de la campagne precedente, dont les nombres restent historiques : SG Debug 84,86 -> 256,89 ; SMS 58,44 -> 244,09 ; GG 59,32 -> 294,77. Ces anciens nombres ne remplacent aucun interpreteur actuel. Les cinq titres huit bits sont plus rapides que leur interpreteur neuf en Debug. En Release, les mesures principales gagnent sur SG/SC/SMS, et restent en retrait sur Mark III/GG. La correction du bus ne prouve donc pas que tout le dynarec est maintenant plus rapide dans tous les cas.

Contre-controles Release, fichiers distincts : GG interpreteur/natif 2951,54/1752,90 ; Mark III 1840,50/1856,63. Ne pas effacer les premiers relevés. Le retrait GG persiste ; Mark III reste indecis entre passages. Aucune cause de cette dispersion n est attribuee a la machine hote ou supposee prouvee. Les temoins MD/MCD correspondent et restent plus rapides en natif sur ces passages ; cela ne valide pas toutes les machines ou autres variantes.

Profils instrumentes separes des FPS principaux, 900 trames mesurees apres chauffe. GG : temps machine natif 0,6142046 s ; CPU natif inclusif 0,4194508 ; execution 0,2030015 ; bus 0,0861895 inclus dans execution ; compilation 0,1650903. Mark III : machine 0,5161463 ; CPU 0,1891470 ; execution 0,1230996 ; bus 0,0490503 inclus ; compilation 0,0194893. Ne pas sommer les temps inclusifs. GG construit 3170 blocs au total, Mark III 1095 ; zero eviction dans les deux, zero invalidation de suffixe, 16 invalidations de premier opcode GG et zero Mark III. Agrandir le cache sans autre preuve ne traite donc pas un churn demontre ; ne pas proposer cette piste comme correction etablie. Emission/allocation executable et execution/interop restent a distinguer pour GG.

Limites : un titre par machine, controle automatique des sorties, pas de validation complete des jeux ni de reprise d etat. Les trois modes 68000 ne sont pas compares dans cette passe. Le SH-2, le 32X et le CD32X ne sont pas modifies ici ; leurs pertes et la divergence Night Trap deja consignees restent ouvertes. Le GPU complet reste absent. Aucun nouveau gain SH-2/32X ni resultat ancien n est presente comme une nouvelle mesure.

Preuves : artifacts/Emulation/segaemu-autres-jeux/attempts/native-z80-maps, JSON principaux, checks, reverse, profils, summary.json, source-sha.json et logs. SHA charges conformes aux sorties finales Chips/Sega Debug et Release. Builds cibles reussis, aucun commit/push.

# SH-2 : revalidation des attentes de fetch et campagne complete — 17 novembre 2026

Correction fonctionnelle du compilateur SH-2 : le controle comparait les mots de code lors de la reutilisation, mais ne comparait pas les attentes de fetch incorporees aux prefixes fusionnes. Un bloc pouvait donc conserver les anciens couts apres changement de mode de cache ou mapping, alors que ses octets etaient identiques. La trace Night Trap avait 23 314 acces identiques dans la trame 677 avant une lecture a 0600043C effectuee deux cycles trop tard en compile ; le code precedent contient 6112 / 3210 / 8B02. L ecart temporel se propageait ensuite aux CPU et RAM, detecte au controle grossier a 720.

Sh2.Blocks.cs conserve maintenant les attentes de chaque fetch dans le bloc et les verifie avec les mots lors de la reutilisation ; une variante de couts incompatible n est pas reutilisee. Le premier fetch garde son cout dynamique, les suivants alimentent les couts prevalides. Le decodage et la validation lisent aussi les fenetres deja mappees au lieu de remplir/remplacer les lignes du cache materiel en compilant du code futur. Ce dernier changement seul avait echoue auparavant : il est maintenant retenu dans la correction combinee, pas presente comme une solution suffisante isolee.

Le test autonome CompiledBlocksRevalidateFetchTimingsWhenCodeBytesStayIdentical fait varier les attentes avec des budgets courts et compare registres/contexte/horloges a l interpreteur. Les 34 cas du fichier Sh2Tests passent sur les DLL normales Debug et Release. Le projet Chips.Tests global a echoue CS0234 sur CPU/NEC/UPD780C/UPD780C1/Upd780C1Tests.cs et sa reference CPU.Common ; aucune source NEC n est modifiee. Un projet temporaire ne compile que le fichier SH-2 concerne. Ce resultat ne signifie pas que tout Chips.Tests a ete execute.

Diagnostic conditionnel temporaire : CPU/cache/RAM compares a chaque fin de trame et acces memoire des deux SH-2 compares de 600 a 960 ; 361 trames, aucun ecart avec la correction combinee, maximum 506 372 evenements dans une trame, sous la limite de collecte. Un premier arret a 637 avec etats identiques venait de la limite du journal de sondes ; ce n etait pas une nouvelle panne de l emulation. Les sondes Sh7095 et le drapeau de projet sont retires ; leurs sources correspondent exactement aux empreintes conservees avant instrumentation. Aucun DiagnosticAccess dans les DLL finales.

## Protocole de la nouvelle campagne

Toutes les machines SegaEmu sont remesurees, un titre chacune : aucun ancien FPS reutilise. Deux cartes fraiches par passage, interpreteur puis candidat a chaque trame, memes medias et entrees. 60 trames de chauffe puis 180 mesurees Debug, sauf CD32X prolonge a 900 pour lancer les deux SH-2 ; 900 mesurees Release partout. Les quatre courts passages Debug CD32X de 180 trames sont conserves mais remplaces dans les tableaux par les passages longs distincts, sans effacement de leurs donnees. Seuls les appels RunFrame sont chronometres ; chargement, checks et hashes exclus. Start cinq trames tous les 120 a partir de 120, sans fenetre/limiteur/sortie audio hote.

Pixels et PCM compares chaque trame ; CPU normalises, RAM et caches/peripheriques SH-2 controles tous les 60. Les 50 passages retenus, ainsi que les quatre passages courts supplementaires, correspondent aux points verifies. Chaque variante a son propre interpreteur neuf dans le JSON ; la colonne Interpreteur affiche le temoin du passage natif existant. Les autres colonnes correspondent a des processus distincts ; ne pas calculer leurs pourcentages exclusivement a partir de cette premiere colonne. Sans objet signifie option 68000 sur machine a Z80 seul. GPU complet non implemente pour toutes les lignes.

## Debug — FPS

| Jeu | Plateforme | Interpréteur | Dynarec existant | Registres | ROM stable | Combiné |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 209,18 | 295,87 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 189,95 | 263,58 | Sans objet | Sans objet | Sans objet |
| Sonic | Mark III | 193,35 | 241,61 | Sans objet | Sans objet | Sans objet |
| Sonic | Master System | 171,66 | 206,31 | Sans objet | Sans objet | Sans objet |
| Sonic | Game Gear | 232,23 | 304,90 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 40,32 | 44,51 | 40,69 | 45,97 | 43,46 |
| Aladdin | Mega Drive | 33,03 | 37,21 | 35,51 | 33,15 | 32,01 |
| Batman Returns | Mega-CD | 30,37 | 36,59 | 34,37 | 36,60 | 38,11 |
| Doom | 32X | 20,65 | 16,54 | 16,67 | 17,09 | 16,42 |
| Night Trap, disque 1 | Mega-CD + 32X | 24,80 | 21,35 | 21,08 | 20,26 | 19,87 |

## Release — FPS

| Jeu | Plateforme | Interpréteur | Dynarec existant | Registres | ROM stable | Combiné |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 2609,40 | 2801,80 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 2654,15 | 3146,01 | Sans objet | Sans objet | Sans objet |
| Sonic | Mark III | 2340,48 | 1842,44 | Sans objet | Sans objet | Sans objet |
| Sonic | Master System | 2307,33 | 1891,24 | Sans objet | Sans objet | Sans objet |
| Sonic | Game Gear | 3061,04 | 1754,55 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 623,42 | 705,15 | 550,04 | 658,17 | 559,58 |
| Aladdin | Mega Drive | 537,51 | 612,67 | 645,04 | 658,42 | 630,52 |
| Batman Returns | Mega-CD | 425,31 | 450,48 | 341,19 | 393,65 | 406,34 |
| Doom | 32X | 125,39 | 5,95 | 6,15 | 6,20 | 5,97 |
| Night Trap, disque 1 | Mega-CD + 32X | 166,06 | 69,78 | 67,72 | 69,24 | 70,60 |

## Decision et travail restant

La divergence Night Trap observee a 677/720 est corrigee sur les passages compares : les quatre variantes atteignent 960 trames en Debug normal et Release normal, avec CPU/cache/RAM/image/PCM correspondants. Les deux SH-2 sont effectivement lances dans ces passages longs ; ne plus presenter les 180 trames du debut comme une validation de leur execution. La trace diagnostique plus fine de 600 a 960 correspond aussi pour tous les acces collectes. Cela ne valide pas tous les jeux, toutes les opcodes ou toute la suite d une partie.

La correction ne resout pas la vitesse 32X. Doom Release natif existant reste a 5,95 FPS contre 125,39 pour son interpreteur neuf, avec 32 421 blocs SH-2 master construits. Le profil anterieur placait environ 98 % de son echantillon dans CompileBlock ; aucun nouveau pourcentage de profil n est invente ici. Night Trap Release termine maintenant, mais ses 67–71 FPS natifs restent sous les 159–173 FPS des interpreteurs des passages correspondants. Ne pas confondre correction fonctionnelle et acceleration.

Le chemin SH-2 reste hybride : blocs CIL et petit prefixe x64 ; les delay slots/frontieres refusant un bloc peuvent encore emprunter le chemin interprete. Zero CompiledBlockFallbackCount indique absence d erreur IL ayant desactive le compilateur, pas absence de toute instruction interpretee. Doom n execute aucun prefixe x64 sur le master et un sur le slave. Les compteurs 68000 relevent encore 11 184 526 instructions de repli sur Doom et 6 712 230 sur Night Trap. Les trois options 68000 ont tres peu de chemin natif a influencer dans Doom ; leurs ecarts ne prouvent pas un gain SH-2.

A conserver : correction des projections Z80 deja validee, decodage sans remplissage anticipe couple a la signature des attentes, regression autonome de changement de timings, interpreteur original et selections disponibles. A poursuivre : cout de construction/reconstruction SH-2 et parcours natif incomplet, fenetres de code 68000 transparentes hors effets 32X, retrait Game Gear Release deja constate. Ne pas agrandir un cache sans mesurer ses evictions/reutilisations. Le GPU complet reste a implementer ; aucun resultat GPU n est invente.

Sondes temporaires et drapeau de projet retires ; Sh7095.cs et Chips.csproj retrouvent exactement leurs empreintes avant instrumentation. Les DLL finales ne contiennent pas DiagnosticAccess. Les SHA charges concordent avec les sorties normales pour les 50 passages retenus ; DLL conservees sous final/Debug et final/Release pour les prochaines comparaisons. Aucun commit/push. Preuves sous artifacts/Emulation/segaemu-autres-jeux/attempts/sh2-first-access : traces initiales/corrigees, code et sondes archives, logs des 34 tests par configuration, 54 JSON machine, summary.json, SHA sources/DLL.

## 2026-11-17 — Cause mesuree du cout de compilation SH-2 de Doom

Diagnostic distinct des FPS, trames 1 a 960 : 32 421 constructions master pour 9 778 adresses uniques ; 22 643 reconstructions apres eviction et 28 325 evictions, capacite 4 096. Aucun changement de code/timing ni eviction de variante. Preparation JIT : 137,5189252 s sur 138,6946262 s de construction. Ne pas attribuer ce cout a la machine hote. Preuve : attempts/sh2-cache-profile/doom-Release-before.json.

Correction candidate : capacite de cache parametrable dans Sh2.Blocks.cs, valeur de reference 4 096 conservee, essai 16 384 sur les deux SH-2 uniquement dans le banc. Changer la capacite libere les anciens blocs et liens ; une valeur identique les conserve ; zero refuse. 36 controles Sh2Tests passent dans chaque configuration, dont deux cas utiles d eviction/reutilisation/changement de capacite. Le premier lancement du projet temporaire a echoue car les proprietes globales de dossier obj se propageaient au ProjectReference Chips ; le banc utilise ensuite la DLL explicitement construite, pas une modification des sources pour contourner cet echec.

Attention chemins build : le premier build Chips sans Platform=x64 avait mis a jour bin/Release, alors que le banc reference bin/x64/Release. Invocation corrigee avant tout diagnostic execute. Ne pas reutiliser ce premier build comme preuve de DLL chargee. Les SHA charges sont inclus dans les JSON.

Comparaison machine complete en cours, interpreteur neuf / natif4096 / natif16384, Debug puis Release, Doom et Night Trap, 60 chauffe + 900 mesurees. Aucun gain declare avant resultats. Interpreteur conserve ; GPU complet et dynarec complet restent inacheves. Aucun commit.

### Resultats du cache SH-2 parametrable — controles termines

Deux titres, trois machines fraiches par passage (interpreteur, natif4096, natif16384), 60 trames de chauffe puis 900 mesurees dans chaque configuration. Seuls RunFrame chronometres ; images/PCM compares chaque trame, et registres/contexte SH-2, caches/peripheriques, SDRAM et RAM principales verifies tous les 60. Les quatre passages correspondent. Le profil detaille est desactive pendant les FPS. Ces Debug longs incluent le gameplay et ses compilations, contrairement au precedent Doom Debug de 180 trames : ne pas comparer directement leurs FPS pour annoncer une regression de l interpreteur.

Debug — FPS

| Jeu | Plateforme | Interpreteur neuf | Natif cache 4096 | Natif cache 16384 |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 19,35 | 4,68 | 10,30 |
| Night Trap disque 1 | Mega-CD + 32X | 24,99 | 21,66 | 21,67 |

Release — FPS

| Jeu | Plateforme | Interpreteur neuf | Natif cache 4096 | Natif cache 16384 |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 130,87 | 5,61 | 20,99 |
| Night Trap disque 1 | Mega-CD + 32X | 178,87 | 71,89 | 72,61 |

Doom construit 9778 blocs master contre 32421 ; slave 158 dans les deux cas. Night Trap reste a 1835 master et 22 slave. Retenir la capacite 16384 pour les deux SH-2 de la selection native de Mega32X uniquement : elle elimine les reconstructions constatees et ameliore fortement le passage Doom. Ne pas annoncer un gain Night Trap : ses blocs tenaient deja dans 4096. Le defaut de la puce reste 4096, reglable pour reproduire la reference ; interpreteur/dynamicBlocks non natif inchanges. Aucun autre CPU ou bus n est modifie dans cette correction.

Limites : les deux routes natives restent sous leur interpreteur neuf. Le cache ne supprime pas le cout des premieres compilations de 9778 adresses ; le JIT du code CIL reste un probleme majeur, la voie SH-2 n est pas un dynarec entierement x64. Ne pas presenter ce correctif comme la fin de la reparation. La consommation memoire peut augmenter car davantage de blocs restent residents ; aucun chiffre memoire isole par route n est mesure ici. GPU complet toujours absent.

36 controles autonomes SH-2 reussis dans chaque configuration, dont eviction et changement de capacite. Builds cibles Chips et Sega Debug/Release reussis ; apres raccordement de la politique dans Mega32X, wiring-Debug.json et wiring-Release.json verifient les capacites reelles des deux SH-2 avant toute surcharge du banc (4096 interpreteur, 16384 natif). Les FPS ci-dessus mesurent les memes capacites appliquees explicitement dans le banc avant ce raccordement ; ces controles de cablage ne sont pas de nouveaux FPS.

Preuves : attempts/sh2-cache-profile/summary.json, games-*.json avec SHA charges et checkpoints, diagnostic doom-Release-before.json, logs et sources du banc archives. Aucun commit/push.

Banc temporaire .work/Sh2CacheProfile et ses bin/obj supprimes apres fin des processus. Sources du banc et preuves conservees dans attempts/sh2-cache-profile.


## 2026-11-17 — Corps SH-2 reutilisables entre adresses, selection experimentale

Profil Doom cache16384 : 9778 constructions master mais 7489 signatures code/timing (7488 sequences d opcodes) ; 32,5993964 s de preparation JIT sur 32,9434215 s de compilation. Des opcodes uniques sont reconstruits a plusieurs adresses : 4F22 106 fois, 4F26 98, 2F86 96, 2F96 82, RTS 80. Preuve doom-profile.json. Ce profil n est pas un nouveau FPS.

Modification : extraction du generateur de corps dans CompileBlockBody, selection UseSharedCompiledBodies (false par defaut). La voie de reference conserve ses constantes d adresse. La variante charge l adresse du bloc en cours, puis reutilise le delegate pour les sequences exactement identiques avec memes attentes de fetch, delay slot, prefixe x64 et borne maximale de bus. Les versions, invalidations, adresses, liens et allocations x64 restent propres au bloc logique. Cache des corps borne a la capacite des blocs, vide au reset/changement de capacite/nettoyage ; pas de partage entre CPU. Le corps ne capture aucun board ni peripherique.

38 controles Sh2Tests reussis Debug et Release. Deux cas supplementaires controlent les corps relocalises avec lecture PC-relative, arithmetique/prefixe x64, branchements, budgets courts, attente 2/0/2, code modifie, et cache vide. Le compteur distingue corps generes et reuses. Aucun changement de Sh2.cs ni de l interpreteur.

Machine complete : interpreteur neuf, natif16384 reference, natif16384 corps partages. 60 trames de chauffe et 900 mesurees, memes entrees, sans fenetre ni limiteur ; RunFrame seul chronometre. Pixels/PCM a chaque trame ; registres/contexte SH-2, caches/peripheriques, SDRAM et RAM principales tous les 60. Quatre passages reussis. Pas de donnees historiques substituees aux interpreteurs.

Debug — FPS

| Jeu | Plateforme | Interpreteur neuf | Natif16384 reference | Corps partages |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 20,35 | 10,78 | 11,13 |
| Night Trap disque 1 | Mega-CD + 32X | 24,10 | 20,71 | 21,44 |

Release — FPS

| Jeu | Plateforme | Interpreteur neuf | Natif16384 reference | Corps partages |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 133,04 | 21,83 | 23,64 |
| Night Trap disque 1 | Mega-CD + 32X | 185,86 | 75,19 | 84,54 |

Doom : blocs logiques 9778 master / 158 slave dans les deux routes ; corps de 9778/158 a 7489/139, reuses 2289/19. Night Trap : blocs logiques 1835/22 inchanges ; corps de 1835/22 a 1264/17, reuses 571/5. La reduction des premieres compilations identiques est prouvee ; ces passages ne mesurent pas un nouveau seuil de vitesse apres une chauffe complete du gameplay. La voie reste hybride CIL et petit prefixe x64.

Decision : conserver la selection experimentale et l original, sans activer la nouvelle variante par defaut dans Mega32X. Gains observes limites (+3,3 % et +3,5 % Debug ; +8,3 % et +12,4 % Release), loin de l interpreteur et de la demande de forte acceleration. Ne pas prolonger des retouches CIL en les presentant comme la solution du dynarec complet. Restent le parcours natif incomplet, les replis 68000 et le cout des corps encore distincts ; ces points ne sont pas declares corriges. GPU complet absent, aucun nouveau test MD/MCD sans32X puisque cette passe ne modifie que les blocs SH-2. Aucun commit/push.

Preuves sous attempts/sh2-body-sharing : summary.json, games-*.json, SHA charges, checkpoints, doom-profile.json, logs de builds/38 tests et sources de banc archivees.

Banc .work/Sh2BodyProfile et ses bin/obj supprimes apres fin des processus ; sources, JSON et logs conserves dans attempts/sh2-body-sharing.


## 2026-11-17 — Backend SH-2 x64, tous encodages et contexte resident

Sources consultees localement : References/Machines/Sega/32X/PicoDrive/cpu/sh2/compiler.c et cpu/drc/emit_x86.c, plus les handlers existants de Sh2.Instructions.cs pour conserver leur comportement observable. Aucun nouveau telechargement. Les nouveaux fichiers Chips sont Compilation/Sh2X64Context.cs, Sh2X64Block.cs, Sh2X64Block.Memory.cs, Sh2X64Block.Control.cs et Sh2.NativeBlocks.cs. Contrat independant de Sega : seize registres empruntes, contexte architectural explicite, protocole de bus/horloge ; aucun appel d un payload a un handler gere d opcode. Reference Sh2.cs non modifiee.

Toutes les instructions entieres, T/Q/M, retenues/debordements, multiplication/division, MAC sature, transferts memoire, PC relatif, postincrement/predecrement, registres de controle, branches/delay slots, TRAPA/RTE/SLEEP et illegal/illegal-slot ont un payload x64. Le test parcourt les 65536 encodages et verifie que tous sont couverts. 18902 encodages sans service de bus/horloge : 9677824 cas ; 46634 autres : 2984576 cas avec traces d acces et chronologie. Les deux categories totalisent tous les encodages, y compris les invalides qui executent le trap natif. Cela ne constitue pas une validation exhaustive de toutes les combinaisons de memoire/peripheriques.

47 tests SH-2 (38 preexistants/corrections precedentes et 9 cas/suites nouveaux) reussissent Debug et Release. Apres la derniere optimisation de capture/restauration, les cinq cas affectes d execution complete et d erreur ont ete repasses dans chaque configuration. Ils controlent budgets courts, code RAM change, attente modifiee, IRQ, idle loops, captures de registres et caches vides pendant un callback ; une exception du bus revient au gere avant l instruction suivante et permet une reprise. Le banc initial utilisait PC 06000104 pour une RAM mappee a 00000000 : les sorties correspondaient mais aucune plage pure mappee n etait testee ; PC corrige a 00000104, assertion de fusion conservee. Ne pas attribuer ce premier echec de couverture au coeur.

Defauts corriges pendant ce portage : le delegate declare dans Sh2<TBus> etait refuse par Marshal.GetFunctionPointerForDelegate (type generique implicite), il est maintenant hors de la classe generique ; les decodes LDS.L 4006/4016/4026 et LDC.L 4007/4017/4027 etaient inverses, corriges selon la table existante. Les TRX d echec sont conserves.

Premiere machine complete : Doom Debug, 900 trames mesurees apres 60 chauffe, interpreteur neuf 19,48 FPS, ancien natif CIL cache16384 10,89, nouveau SH-2 x64 7,05. Sorties controlees concordantes mais ralentissement majeur : ne pas presenter cet essai comme un succes de vitesse. JSON games-Debug-doom.json conserve.

Profil separe de cinq secondes sur le nouveau parcours seul apres la trame 660 : dotnet-sampled-thread-time, top30 exclusif, environ 25,65 % dans Marshal.GetFunctionPointerForDelegate et 42,88 % attribues a X64CodeBuffer.ExecuteLinked. Cette derniere attribution inclut le temps situe sous la frontiere native, pas uniquement les controles du SafeHandle ; ne pas inventer une decomposition de ces 42,88 %. Le premier profil cpu-sampling etait reserve a collect-linux dans l outil installe ; invocation corrigee vers dotnet-sampled-thread-time, erreur initiale conservee. Le runner final s arrete cooperativement et est attendu/detruit dans finally. Le profil n est pas un nouveau FPS.

Correction : pointeur de callback cache ; contexte natif et registres pins pendant une tranche CPU entiere ; coeur de boucle et services de contexte/bus optimises dans le meme runtime genere que l interpreteur SH-2 existant. Le cache contient des traductions natives par opcode et des plages d instructions pures. Les retraits de code pendant une execution sont differes jusqu au retour de tous les appels actifs ; les tests vident effectivement les caches au milieu d un callback et apres faute. Le passage ABI direct est autorise seulement pendant ce bail, sans SuppressGCTransition (les payloads peuvent rappeler le gere). L execution native ne genere aucun corps CIL par bloc et ne revient pas a ExecuteOpcode. Les services du bus, de l horloge, des interruptions et des intrinseques idle restent des services hotes, pas des handlers d opcode.

Selection UseFullX64Blocks (avec UseCompiledBlocks) disponible sur la puce ; false par defaut, ni l interpreteur ni la selection actuelle de Mega32X ne sont remplaces. Les blocs CIL et les anciens prefixes restent comparables. Windows x64 et code dynamique requis explicitement, pas de fallback silencieux sur cette selection.

### Campagne apres correction du contexte resident

Trois machines fraiches par passage : interpreteur, natif existant cache16384, puis autres CPU natifs existants avec les deux SH-2 sur UseFullX64Blocks. 60 chauffe + 900 mesurees dans les deux configurations ; memes medias et entrees que precedemment ; RunFrame seul chronometre. Pixels/PCM chaque trame ; registres/contexte SH-2, caches/peripheriques, SDRAM/RAM, registres architecturaux et cycles 68000 main/sub controles tous les 60. Les quatre passages correspondent. Chaque interpreteur est remesure, sans substitution de valeur historique.

Debug — FPS

| Jeu | Plateforme | Interpreteur neuf | Natif existant CIL | SH-2 x64 resident |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 19,48 | 10,88 | 13,70 |
| Night Trap disque 1 | Mega-CD + 32X | 24,21 | 21,06 | 19,79 |

Release — FPS

| Jeu | Plateforme | Interpreteur neuf | Natif existant CIL | SH-2 x64 resident |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 131,26 | 23,38 | 55,43 |
| Night Trap disque 1 | Mega-CD + 32X | 182,53 | 74,41 | 84,85 |

Les compteurs SH-2 resident sont identiques entre configurations sur ces scenarios. Doom : master 57832216 instructions natives, 3177 traductions par opcode, 2190 plages pures et 65435 executions de plages ; slave 57009323 instructions, 143 traductions, 10 plages et deux executions de plages. Night Trap : master 81520759 instructions, 681 traductions, 356 plages et 2028979 executions de plages ; slave 6765750 instructions, 29 traductions, une plage/une execution. Zero corps CIL master/slave dans chaque passage de la nouvelle selection. Les intrinseques idle peuvent coalescer des boucles ; le compteur d instructions natives compte les dispatchs reels, pas les iterations coalescees.

Decision : conserver le portage et sa selection experimentale, ne pas le promouvoir par defaut. Doom gagne face au CIL existant, et le defaut majeur du premier essai Debug est reduit ; Night Trap perd environ 6 % Debug et gagne environ 14 % Release. Les deux jeux restent sous leur interpreteur neuf. La demande de forte acceleration n est pas satisfaite. Le passage ABI demeure par opcode pour les instructions non fusionnees, notamment memoire/branches : poursuivre de vrais blocs multi-instructions avec sorties budget/evenement/code modifie, pas d autres micro-retouches CIL. Le 68000 conserve aussi ses replis actuels et le GPU complet reste absent. Ne pas appeler cette campagne un dynarec complet de toute la machine.

Les sources n ont change que dans le SH-2 et ses tests ; aucun controleur MD/MCD hors SH-2 ou allocateur commun n est modifie. Les autres machines ne sont pas remesurees pour cette passe. Les commentaires du nouvel emetteur ont ete actualises apres mesures, sans changement executable. Les bancs profiler/mesure utilisent des dossiers de sortie distincts et Rebuild explicite pour eviter de reutiliser le mauvais exe de meme nom.

Preuves : attempts/sh2-full-native/summary.json, resident-games-*.json (SHA charges et checkpoints), premier games-Debug-doom.json, TRX de couverture/runner/resident, trace doom-full-Debug.nettrace et top30, logs et sources de banc archivees. Aucun commit/push.

Banc .work/Sh2FullNative, projets de profilage/tests/mesure et leurs bin/obj supprimes apres fin des processus. Les sources de banc archivees et les resultats restent sous attempts/sh2-full-native.


## Nouvelle campagne complete du 17 novembre 2026 - cinq colonnes

Demande : toutes les lignes remesurees, y compris 32X et Mega-CD + 32X. Aucun FPS historique reutilise. Aucun changement du code de production pendant cette campagne, aucun commit. Builds cibles Chips/Sega Debug et Release reussis, zero erreur (62 avertissements au build module).

Par configuration et machine : un interpreteur neuf et quatre cartes candidates neuves pour les machines a 68000, ou un interpreteur neuf et une carte native pour les machines a Z80 seul. Toutes les routes tournent successivement a chaque trame, avec memes entrees. Soit 35 cartes par configuration, 70 cartes liberees dans finally. Seul RunFrame est chronometre : compilation de blocs pendant cet appel incluse, chargement/checks exclus. Aucun affichage ni limiteur ni peripherique audio hote.

60 trames de chauffe puis 180 mesurees Debug, sauf Night Trap : 900 mesurees afin de lancer effectivement les SH-2 ; 900 mesurees Release partout. Start cinq trames tous les 120 a partir de 120. Images completes et PCM compares chaque trame ; RAM, champs architecturaux CPU et, sur 32X, SDRAM/caches/peripheriques SH-2 compares tous les 60. Les 20 passages multi-routes terminent sans ecart a ces points controles. Cela ne valide pas toute une partie ni tous les jeux.

Colonnes : Dynarec existant = selection UseNative68000Blocks de la carte, avec Z80 natif et parcours SH-2 compile existant lorsqu ils sont presents ; Registres ajoute UseNativeGuestRegisters sur les 68000 main/sub ; ROM stable ajoute UseNativeStableCode ; Combine active les deux. Le nouveau SH-2 UseFullX64Blocks et le parcours multi-instructions inacheve ne sont pas selectionnes ici. Cette campagne compare exactement les cinq colonnes demandees, pas toutes les autres experiences SH-2. Le 68000 et le SH-2 existants gardent leurs limites/replis : ne pas appeler ces routes un dynarec integral de toute la machine. Sans objet = aucune puce 68000 sur cette machine ; GPU complet toujours non implemente.

### Debug - FPS

| Jeu | Plateforme | Interpreteur | Dynarec existant | Registres | ROM stable | Combine |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 190,18 | 261,68 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 183,72 | 251,10 | Sans objet | Sans objet | Sans objet |
| Sonic | Mark III | 169,27 | 206,29 | Sans objet | Sans objet | Sans objet |
| Sonic | Master System | 180,63 | 221,81 | Sans objet | Sans objet | Sans objet |
| Sonic | Game Gear | 234,39 | 311,30 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 39,95 | 43,84 | 43,85 | 44,07 | 44,15 |
| Aladdin | Mega Drive | 27,35 | 30,78 | 30,48 | 30,45 | 30,30 |
| Batman Returns | Mega-CD | 32,35 | 39,47 | 39,17 | 39,39 | 38,99 |
| Doom | 32X | 20,71 | 16,49 | 16,41 | 16,46 | 16,50 |
| Night Trap, disque 1 | Mega-CD + 32X | 25,20 | 21,68 | 21,50 | 21,76 | 21,70 |

### Release - FPS

| Jeu | Plateforme | Interpreteur | Dynarec existant | Registres | ROM stable | Combine |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 1724,66 | 1746,72 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 4547,62 | 4216,80 | Sans objet | Sans objet | Sans objet |
| Sonic | Mark III | 1633,31 | 1450,67 | Sans objet | Sans objet | Sans objet |
| Sonic | Master System | 3631,12 | 3081,86 | Sans objet | Sans objet | Sans objet |
| Sonic | Game Gear | 3785,06 | 2557,68 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 818,83 | 944,20 | 915,20 | 968,78 | 958,34 |
| Aladdin | Mega Drive | 738,22 | 749,13 | 744,41 | 747,09 | 744,35 |
| Batman Returns | Mega-CD | 681,12 | 678,28 | 668,27 | 690,59 | 668,21 |
| Doom | 32X | 134,17 | 18,66 | 18,38 | 18,12 | 18,07 |
| Night Trap, disque 1 | Mega-CD + 32X | 224,24 | 56,47 | 56,10 | 56,81 | 56,89 |

Bilan : sorties controlees concordantes dans les deux configurations. A conserver comme preuves fonctionnelles limitees aux scenarios. La selection native reste plus lente sur Doom/Night Trap et sur plusieurs machines Z80 en Release ; ne pas promouvoir ces routes pour leur vitesse. Les variantes 68000 apportent peu ou perdent suivant le passage ; ne pas presenter leurs faibles ecarts comme une acceleration generale. Les valeurs sont celles des passages courts/longs explicites ci-dessus, et non une comparaison directe a des scenarios historiques differents.

Preuves : artifacts/Emulation/segaemu-autres-jeux/attempts/fresh-all-20261117 : 20 JSON, summary.json, empreintes SHA256 des DLL chargees dans chaque JSON, sources du banc archivees et logs des builds. Les bancs temporaires et leurs bin/obj sont supprimes apres sortie des processus.

## SH-2 : blocs x64 multi-instructions avec memoire et branches - 17 novembre 2026

Apres le tableau complet fresh-all, terminer le travail deja commence dans Sh2X64Block.Execution.cs : gardes budget/IRQ/PC, fetchs, fin d instruction/delay slot, sortie code change. Selection distincte UseMultiInstructionX64Blocks, avec UseFullX64Blocks et UseCompiledBlocks ; false par defaut. Le parcours resident par opcode reste disponible. Aucun changement de Sh2.cs, des bus MD/MCD, des autres CPU ou du chemin GPU dans cette passe.

Les blocs incluent les transferts memoire, operations de controle et leur delay slot mappe. Les callbacks gardent les services du bus/horloge ; aucun handler gere d opcode. Le contexte temporaire du bloc survit aux captures/restaurations des services. Apres acces ou evenement, verifier epoch puis mots/timings si necessaire ; quitter apres l instruction en cours si code change ou cache vide. Les baux existants gardent le code executable vivant pendant les callbacks. Les cas non mappables gardent le payload natif par opcode, sans retour a un handler interprete.

14 controles autonomes passent Debug et Release : parcours originaux et nouveaux, budgets courts, IRQ, changements de timings, idle, automodification, lecture/ecriture changeant l instruction suivante, delay slot normal/illegal, cache vide et faute du bus pendant execution puis reprise. Ils couvrent aussi les coeurs generes optimises. Les anciennes suites exhaustives de semantique par opcode n ont pas ete repassees : le payload autonome conserve sa branche originale ; cette validation concerne les nouvelles frontieres multi-instructions.

Machines : trois cartes neuves par passage, interpreteur / x64 resident par opcode / x64 multi-instructions, chaque route a chaque trame. 60 chauffe + 900 mesurees en Debug et Release. Seul RunFrame chronometre, compilation pendant les trames incluse. Pixels/PCM chaque trame ; registres/cycles 68000, SH-2, caches/peripheriques, RAM et SDRAM tous les 60. Les quatre passages terminent avec sorties controlees concordantes. Aucun ancien FPS utilise. Ces colonnes comparent les nouveaux parcours SH-2, pas les quatre options 68000 du tableau fresh-all.

### Debug - FPS

| Jeu | Plateforme | Interpreteur neuf | SH-2 x64 par opcode | SH-2 x64 multi-instructions |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 20,12 | 13,93 | 15,66 |
| Night Trap disque 1 | Mega-CD + 32X | 24,01 | 19,34 | 22,38 |

### Release - FPS

| Jeu | Plateforme | Interpreteur neuf | SH-2 x64 par opcode | SH-2 x64 multi-instructions |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 132,04 | 53,48 | 63,67 |
| Night Trap disque 1 | Mega-CD + 32X | 188,67 | 85,10 | 113,56 |

Compteurs identiques Debug/Release : Doom master 57832216 instructions, 9088 blocs construits et 24601355 appels de bloc ; slave 57009323 instructions, 145 blocs et 30409327 appels. Night Trap master 81520759 instructions, 2091 blocs et 23144647 appels ; slave 6765750 instructions, 18 blocs et 4957794 appels. Zero construction par opcode et zero corps CIL dans la route multi sur ces passages. Les compteurs d instructions correspondent au resident ; ils excluent les iterations coalescees par les intrinseques idle.

Resultat : face au resident par opcode, gain Debug Doom +12,47 %, Night Trap +15,71 % ; Release +19,06 % et +33,45 %. A conserver comme route experimentale et tests de frontieres. Ne pas promouvoir par defaut : les deux machines restent sous l interpreteur, le but de forte acceleration n est pas atteint. Limite concrete : encore 55,01 millions d appels de blocs sur Doom (environ 2,09 instructions par appel), 28,10 millions sur Night Trap. Le decoupage a diminue les appels mais conserve beaucoup de transitions ; les services memoire/horloge rappellent aussi le gere. Cela ne mesure pas leur temps propre et ne prouve pas qu un seul de ces couts explique toute la perte. Ne pas annoncer un dynarec complet de toute la machine : les 68000 gardent leurs replis precedents, GPU complet absent.

Sources et mesures : artifacts/Emulation/segaemu-autres-jeux/attempts/sh2-multi-native : 4 JSON machine + SHA des DLL chargees/checkpoints/compteurs, summary.json, deux TRX finaux, builds, sources du banc archivees. Les projets temporaires et bin/obj sont supprimes apres sortie des processus ; aucun commit/push.

## SH-2 : regions natives liees - 17 novembre 2026

Selection experimentale UseLinkedX64Regions ajoutee, false par defaut, avec UseFullX64Blocks et UseMultiInstructionX64Blocks. Les regions contiennent les mots deja mappes contigus (limite partagee 64 instructions). Les branches avant/arriere internes utilisent une table relative de sauts x64 en temps constant ; les destinations externes reviennent au repartiteur existant. Les sorties budget/IRQ/code/timing/sommeil/yield et les baux de liberation restent actifs. Aucune modification de Sh2.cs, de MD/MCD hors SH-2 ou du GPU.

Cas traite avant machines : un fetch a froid remplissant le cache peut couter plus cher que le fetch mappe des tours suivants. Le cout dynamique initial est charge une fois ; les retours internes sur le premier mot utilisent son timing mappe compile. Les remappages/changements de ce premier timing invalident aussi la region, meme si les mots restent identiques. Un premier mot non mappable ou change pendant le fetch refuse la liaison et garde le bloc natif precedent. Les compteurs Executed/LinkedHops et le timing initial survivent aux captures de contexte des callbacks.

27 controles autonomes reussissent Debug puis Release : parcours historiques et nouveaux, budgets courts, IRQ, idle, faute et cache retire pendant callback, lecture/ecriture modifiant le code suivant, branches avant/arriere, delay-slot normal/illegal, timing change pendant execution, premier fetch non mappe devenant mappe puis timings 0/2/0 entre budgets, coeurs normaux/generes optimises. Avant ajout des deux derniers cas, 25 controles passaient deja ; ne pas presenter cela comme couverture du fetch a froid. Premier build du projet temporaire : xUnit1009 apres ajout d un parametre a la theorie de faute ; attributs InlineData corriges avant execution. Aucun echec de puce attribue a cette erreur de harnais.

Mesures neuves : trois cartes par passage (interpreteur / blocs multi-instructions precedents / regions liees), 60 chauffe + 900 mesurees Debug et Release pour Doom et Night Trap. Seul RunFrame chronometre, compilation dans les trames incluse ; images/PCM chaque trame ; CPU/cache/peripheriques/RAM/SDRAM et registres/cycles 68000 main/sub tous les 60. Les quatre passages terminent sans ecart aux points controles. Aucun chiffre historique remplace un interpreteur neuf. Les colonnes sont ces trois parcours SH-2, pas les options 68000 du tableau fresh-all.

### Debug - FPS

| Jeu | Plateforme | Interpreteur neuf | Blocs x64 precedents | Regions x64 liees |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 18,11 | 14,11 | 13,82 |
| Night Trap disque 1 | Mega-CD + 32X | 24,44 | 22,75 | 22,85 |

### Release - FPS

| Jeu | Plateforme | Interpreteur neuf | Blocs x64 precedents | Regions x64 liees |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 128,17 | 60,98 | 57,42 |
| Night Trap disque 1 | Mega-CD + 32X | 184,07 | 111,56 | 111,52 |

Compteurs concordants Debug/Release : Doom appels de blocs master 24601355 -> 18730770, slave 30409327 -> 18029858, soit total 55010682 -> 36760628 (-33,18 %). 5322296 sauts internes master et 11638314 slave ; constructions 9088 -> 8908 master et 145 -> 142 slave. Night Trap appels master 23176664 -> 16903572, slave 4957794 -> 4075667 (total 28134458 -> 20979239, -25,43 %) ; sauts internes 4450199 et 882126. Les instructions natives executees correspondent entre les deux routes : Doom 57832216/57009323, Night Trap 81520759/6765750 master/slave. Zero corps CIL dans les deux routes. Les intrinseques idle excluent les iterations coalescees de ces compteurs.

Resultat negatif : reduire les retours au repartiteur ne suffit pas. Doom perd environ 2,06 % Debug et 5,84 % Release ; Night Trap reste a +0,47 % Debug et -0,04 % Release, aucune acceleration nette. Conserver la selection pour diagnostic/reproduction et les tests de frontieres ; laisser false par defaut et ne pas remplacer les blocs precedents. Ne pas refaire cette liaison comme nouveaute ni proposer plus de regions/caches sans preuve nouvelle. Les 68000 gardent leurs replis et le GPU complet reste absent : forte acceleration machine non atteinte.

Profil diagnostique distinct, cinq secondes de Doom lie seul apres la trame 780 (pas un autre FPS) : 48,03 % exclusifs attribues a InvokeLeasedNativePayload, 7,73 % Monitor.Enter_Slowpath ; YM2612 chan_calc 5,25 %, Vdp.merge 4,02 % et Compose32XLine 4,02 %. Le symbole de frontiere native inclut le code dessous et ne permet pas d isoler appel, payload et callbacks ; ne pas appeler ces 48,03 % un cout prouve de l ABI. Le slowpath Monitor ne prouve pas a lui seul de la contention. Le prochain diagnostic doit separer ces couts avant une nouvelle modification de liaison ou de gardes.

Le profil se termine cooperativement, enfant attendu puis Dispose dans finally ; erreur de trace declenche aussi arret/attente/kill de secours. Trace/top30, JSON/SHA et progression conserves sous attempts/sh2-linked-regions. Sources de bancs, TRX et DLL charges archives ; projets temporaires/bin/obj supprimes apres fin des processus. Aucun commit/push.


## SH-2 : tables Windows de deroulement de pile - 17 novembre 2026

Omission confirmee dans les payloads Sh2X64Block : prologue qui sauvegarde RBX/RSI/RDI et reserve 48 octets, appels de callbacks, mais aucune table Windows associee. X64CodeBuffer possede maintenant une surcharge avec UNWIND_INFO v1 optionnel : code, informations et RUNTIME_FUNCTION dans la meme allocation, enregistrement RtlAddFunctionTable avant publication, RtlDeleteFunctionTable avant VirtualFree. Les leases/SafeHandle existants gardent ces ressources vivantes pendant les appels. Create(code) reste sans table pour les autres utilisateurs. Le payload SH-2 fournit ses quatre operations de pile aux offsets 7/3/2/1, prologue de 16 octets. Aucune instruction emise, semantique de bus ou selection par defaut changee.

References primaires : https://learn.microsoft.com/en-us/cpp/build/exception-handling-x64 et https://learn.microsoft.com/en-us/windows/win32/api/winnt/nf-winnt-rtladdfunctiontable ; retrait https://learn.microsoft.com/en-us/windows/win32/api/winnt/nf-winnt-rtldeletefunctiontable. Cette correction concerne le contrat Windows des fonctions non feuilles, pas une promesse de gain FPS ni une preuve sur les anciens plantages dotnet.

43 controles reussis Debug et Release : 27 SH-2 existants de frontieres (budget/IRQ/code/timing/delay slot/fautes/retirement) et 16 nouveaux cas Windows. RtlLookupFunctionEntry retrouve le code reel emis par Sh2X64Block ; RtlVirtualUnwind restaure les registres non volatils, RSP et RIP du caller depuis les prologues partiels, corps et epilogues reels. Les tests de retrait observent la disparition de la table, les allocations ordinaires restent non enregistrees et les entetes invalides sont refuses. Ressources des tests liberees dans finally, y compris si une creation censee echouer reussissait.

Campagne neuve : interpreteur / blocs SH-2 multi-instructions / regions liees, trois cartes fraiches par passage, Doom et Night Trap, 60 chauffe + 900 mesurees Debug et Release. RunFrame seul chronometre, compilation pendant les trames incluse ; pixels/PCM chaque trame, CPU/cache/peripheriques/RAM/SDRAM et registres/cycles 68000 tous les 60. Les quatre passages sont concordants aux points controles. Interpreteur remesure dans chaque passage ; pas de FPS historique reutilise. Pas de comparaison avant/apres controlee permettant d attribuer un gain a la table Windows.

### Debug - FPS

| Jeu | Plateforme | Interpreteur neuf | Blocs x64 | Regions x64 liees |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 20,07 | 15,62 | 15,29 |
| Night Trap disque 1 | Mega-CD + 32X | 24,34 | 22,80 | 22,85 |

### Release - FPS

| Jeu | Plateforme | Interpreteur neuf | Blocs x64 | Regions x64 liees |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 132,13 | 60,83 | 58,10 |
| Night Trap disque 1 | Mega-CD + 32X | 185,33 | 109,09 | 107,50 |

Profil de cinq secondes du lie seul apres trame 780 : collecte/retrait enfant cooperatif et attente/Dispose reussis. Toujours 49,78 % exclusifs attribues a InvokeLeasedNativePayload (contre 48,03 % dans le profil anterieur), sans decomposition fiable de son payload et de ses callbacks. La table Windows est verifiee par les API, mais ce profileur ne fournit toujours pas la separation recherchee ; ne pas presenter le probleme de diagnostic comme termine. Autres lignes : Compose32XLine 5,55 % exclusifs, Monitor.Enter_Slowpath 4,02 %, Vdp.merge 3,70 %, Ym2612.chan_calc 3,39 %. Ces profils ne constituent pas une comparaison statistique des petits pourcentages ni une preuve de contention.

A conserver : enregistrement/retrait des tables SH-2 et tests Windows. Ne pas promouvoir les regions liees ni annoncer une acceleration : elles restent sous les blocs precedents sur Doom Release, et les deux routes restent sous l interpreteur. Le diagnostic doit maintenant horodater directement le payload natif et le callback, dans une selection instrumentee distincte sans assimiler ses temps aux FPS normaux ; tache ajoutee, non realisee. Ne pas refaire une modification de liaison sur la seule baisse du nombre d appels. Les autres generateurs x64 ne recoivent pas automatiquement une table : ils doivent decrire leur propre pile s ils sont non feuilles ; aucune validation de leur deroulement n est revendiquee ici.

Preuves : attempts/sh2-unwind : 4 JSON machines avec SHA/checkpoints/compteurs, summary.json, deux TRX 43 cas, trace/top30 et JSON de profil. Sources et DLL chargees archivees, projets temporaires/bin/obj supprimes apres fin des processus. Aucun commit/push. Les autres backends CPU, GPU complet et replis 68000 restent a traiter dans le but global.

## SH-2 : intervalles natifs et callbacks mesures directement - 17 novembre 2026

UseNativeTimingProfile (false par defaut) genere LFENCE/RDTSC autour du payload et de ses callbacks. Le descripteur emprunte contient un pointeur vers les compteurs locaux a Execute, hors contexte architectural ; capture/restauration ne les efface pas, retrait de code et fautes restent couverts par les leases. Aucune instruction de chronometrage emise quand desactive. Categories read/write/clock/idle/other ; mesures inclusives en ticks TSC, pas cycles de la console ni temps propre CPU. Les synchronisations executees a l interieur d un callback read/write restent comptees dans cette categorie, les appels geres hors payload ne font pas partie du denominateur.

49 controles passent Debug et Release : parcours historiques et instrumentes, budgets/IRQ/cache/fautes, egalite registres/horloges/traces, 576 cas de payload instrumente avec MAC et pile, somme des categories et remise a zero des compteurs, controles Windows de pile. Les observations de jeu instrumentees comparent les sorties natives multi/liees, sans les presenter comme FPS normaux. Calibration de 200000 appels apres 1000 chauffe : payload vide environ 67,81 ticks Debug et 64,62 Release ; intervalle callback vide environ 114,37 et 87,74 ticks. Ces couts et les compteurs/fences perturbent la route instrumentee ; ne pas soustraire ces moyennes comme une decomposition exacte. Les stackalloc de calibration ont ete sortis de la boucle bornee avant son build Release.

Profils : 780 trames de chauffe puis 60 instrumentees, Doom et Night Trap, Debug/Release, cartes multi et liee neuves ; images/PCM chaque trame et CPU/cache/RAM tous les 60 concordants. Part des callbacks dans les intervalles des payloads multi instrumentes :

| Configuration | Jeu | Master | Slave | Categorie dominante master |
| --- | --- | ---: | ---: | --- |
| Debug | Doom | 75,27 % | 76,43 % | clock |
| Debug | Night Trap | 75,03 % | 63,47 % | read |
| Release | Doom | 69,23 % | 69,82 % | clock |
| Release | Night Trap | 68,21 % | 57,42 % | read |

Doom : 3682197 callbacks master dont 3329337 clock sur 60 trames ; ce sont principalement les echeances emises par le bloc, pas un cout demontre de son seul appel ABI. Night Trap master : 11468910 callbacks dont 8121896 read ; slave presque uniquement idle. Les appels du callback vide sont bien moins couteux que le callback complet, mais ne constituent pas une preuve que tous les couts restants viennent de la copie du contexte.

Controle horloge supplementaire Release Doom, JSON distinct clock-detail-Release-doom.json : aux trames 780 et 840, master/slave deferent les horloges, DMA0/DMA1/DMAOR a zero, FRT select0, OCRA1, flags15 (trois drapeaux deja positionnes et remise a zero automatique), masque IRQ0, compteur0 ; prochaines echeances a 4..8 horloges. Source SchedulePeripheralEvent continue a choisir la comparaison A toutes les huit horloges, meme quand tous les drapeaux sont deja positionnes. SynchronizePeripherals ne rappelle UpdateInterrupt que si les drapeaux changent. Cible concrete suivante : regrouper les reveils FRT sans transition observable, en gardant materialisation aux acces et sauvegardes, effacement/rearmement et route originale. Aucun gain de cette correction encore mesure dans cette section.

FPS normaux, instrumentation desactivee, interpreteur neuf / multi / lie : 60 chauffe + 900 mesurees Debug puis Release ; RunFrame seul chronometre, pixels/PCM chaque trame et etats tous les 60 concordants.

### Debug - FPS

| Jeu | Plateforme | Interpreteur neuf | Blocs x64 | Regions liees |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 19,37 | 14,70 | 14,49 |
| Night Trap disque 1 | Mega-CD + 32X | 24,67 | 22,87 | 22,82 |

### Release - FPS

| Jeu | Plateforme | Interpreteur neuf | Blocs x64 | Regions liees |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 132,72 | 60,72 | 57,76 |
| Night Trap disque 1 | Mega-CD + 32X | 186,59 | 108,52 | 108,53 |

Les regions restent sans avantage general ; aucune promotion. Le chronometrage direct a identifie deux cibles distinctes (echeances FRT et lectures memoire), sans modifier l interpreteur ni annoncer un gain a partir de donnees instrumentees. Sources/JSON/SHA/TRX/calibrations sous attempts/sh2-native-timing, bancs temporaires archives et supprimes apres fermeture des proprietaires. Aucun commit.

## FRT SH7095 : regroupement des reveils sans transition - 17 novembre 2026

Correction fondee sur les intervalles natifs mesures : Doom utilisait OCRA=1, flags FRT=15 et masque IRQ=0, avec DMA desactive, mais SchedulePeripheralEvent imposait encore un reveil toutes les huit horloges alors que tous les flags periodiques etaient deja positionnes. UseCoalescedLatchedTimerEvents conserve le chemin original (false, defaut de la puce reutilisable). Si true et flags 0x0e tous positionnes, le prochain checkpoint FRT est repousse d un tour de compteur 16 bits ; serial/watchdog/DMA restent dans le minimum des echeances. Les lectures/ecritures et SaveContext materialisent toujours le compteur, le changement de selection synchronise les horloges, l effacement des flags rearme les echeances normales. Aucun flag ou compteur retire.

40 controles passent Debug et Release : sept nouveaux cas FRT (six configurations de diviseur, comparaisons, remise a zero et compteur ; cas huit horloges/rearmement) et 33 controles SH-2 de frontieres/instrumentation. Contextes materialises, flags, compteur et IRQ concordants, sauvegarde/restauration et retour a la selection originale. Erreur initiale du harnais : NextEventCycle demande directement au controleur au lieu de son CpuBus ; invocation corrigee sans ajouter une API de production inutile.

Quatre cartes fraiches par passage : interpreteur original / interpreteur regroupe / dynarec multi original / dynarec multi regroupe. Doom et Night Trap, Debug puis Release, 60 chauffe + 900 mesurees. Seul RunFrame chronometre ; images et PCM chaque trame, etats CPU/cache/peripheriques/RAM/SDRAM et main/sub68000 tous les 60. Avant comparaison, Cpu.SynchronizeBusClock et Sh7095.SaveContext materialisent les peripheriques des quatre cartes. Les seules echeances privees _busDeadline/_nextPeripheralEvent sont exclues de l egalite : leur changement est le but de la selection ; registres, cycles, compteurs, flags, caches, RAM et audio/video compares restent concordants dans les quatre passages. Aucun ancien FPS reutilise.

### Debug - FPS

| Jeu | Plateforme | Interpreteur original | Interpreteur regroupe | Dynarec original | Dynarec regroupe |
| --- | --- | ---: | ---: | ---: | ---: |
| Doom | 32X | 18,68 | 19,75 | 14,25 | 16,36 |
| Night Trap disque 1 | Mega-CD + 32X | 23,55 | 23,53 | 21,62 | 21,77 |

### Release - FPS

| Jeu | Plateforme | Interpreteur original | Interpreteur regroupe | Dynarec original | Dynarec regroupe |
| --- | --- | ---: | ---: | ---: | ---: |
| Doom | 32X | 128,63 | 191,25 | 60,47 | 98,47 |
| Night Trap disque 1 | Mega-CD + 32X | 195,00 | 202,74 | 108,93 | 110,04 |

Doom : +5,72 % interpreteur et +14,85 % dynarec Debug ; +48,68 % et +62,83 % Release. Night Trap : -0,12 % interpreteur et +0,71 % dynarec Debug ; +3,97 % et +1,01 % Release. Ne pas transformer les tres petits ecarts en gains certains. Les deux parcours dynarec restent sous leur interpreteur, et la forte acceleration globale n est pas terminee.

### Fenetre finale Release - trames 781 a 960

Releves tires des differences des runningTicks des checkpoints 780/960 du meme passage (Stopwatch.Frequency du meme hote consigne dans late-window.json), pas une autre campagne ni une estimation Debug. Cette fenetre evite que la moyenne Night Trap masque sa phase plus lente apres lancement des SH-2.

| Jeu | Interpreteur original | Interpreteur regroupe | Dynarec original | Dynarec regroupe |
| --- | ---: | ---: | ---: | ---: |
| doom | 128,96 | 190,63 | 62,56 | 104,27 |
| nighttrap | 97,70 | 98,94 | 45,88 | 46,44 |

La compilation des blocs SH-2 sur tout le passage Release coute environ 0,70 -> 0,66 seconde Doom et 0,15 -> 0,15 Night Trap (master+slave), avec 9088 et 2032 constructions master identiques. Cela ne justifie pas de revenir a l ancienne explication du CIL dont le profil etait domine par le JIT : ce backend est different. La priorite restante est le cout du parcours natif et de ses lectures/callbacks, particulierement le passage actif CD32X a ~46 FPS.

Decision : retenir le regroupement. Active sur les deux SH7095 dans le constructeur SegaEmu Mega32X, donc 32X et Mega-CD + 32X en interpreteur ou selections compilees ; faux reste disponible pour reprendre le chemin original et le defaut de la puce reutilisable ne change pas. Aucun changement de Sh2.cs, de GPU ni de MD/MCD sans 32X. Constructeur Mega32X protege par finally en cas d initialisation incomplete ; Dispose detache les actions et libere les caches des deux coeurs meme si un retrait echoue, sans detruire les ressources empruntees.

Builds cibles Sega Debug/Release reussis. Apres activation : quatre JSON wiring (32X/CD32X x Debug/Release), deux cartes I/natif chacun, flags master/slave vrais et contextes initiaux identiques a une selection explicite false puis true. Les SHA post-cablage sont dans ces JSON. Les FPS precedents utilisaient deja exactement cette selection true sur les cartes candidates ; les DLL du benchmark et celles du cablage final sont archivees separement. Les regions liees et l instrumentation restent desactivees par defaut.

Preuves : attempts/sh2-frt-events : 4 JSON machines, 4 JSON wiring, late-window.json, summary.json, TRX et logs, bancs et DLL archives ; banc temporaire/bin/obj supprimes apres fermeture des proprietaires. Aucun commit/push.

## Toutes les lignes remesurees apres corrections SH-2 - 17 novembre 2026

Nouvelle campagne : 20 passages, 70 cartes neuves possedees/liberees dans finally, aucun ancien FPS reutilise. Aucun code de production modifie pendant cette campagne, aucun commit. Builds cibles du module Sega et ses dependances Debug puis Release : zero erreur ; builds du banc : zero erreur/avertissement. Application et autres modules non construits.

Seul RunFrame est chronometre, sans fenetre/limiteur/sortie audio hote, compilation survenant dans RunFrame incluse. Chargement et controles exclus. 60 trames de chauffe puis 180 mesurees Debug, sauf Night Trap : 900 ; 900 mesurees Release pour chaque ligne. Les differences de longueur interdisent de comparer directement les ratios Debug/Release comme une charge identique. Entrees identiques, Start cinq trames tous les 120 a partir de la trame 120. Un interpreteur remesure et quatre candidates neuves lorsque le 68000 existe ; deux cartes sur les machines Z80 seul.

Colonnes : Dynarec existant selectionne UseNative68000Blocks, donc le Z80 natif lorsqu il existe, avec les deux SH-2 UseFullX64Blocks + UseMultiInstructionX64Blocks sur les candidates 32X/CD32X. Registres ajoute UseNativeGuestRegisters sur les 68000 main/sub ; ROM stable ajoute UseNativeStableCode ; Combine active les deux. Sans objet = absence de 68000, pas une mesure manquante a recopier. Par rapport a la precedente campagne fresh-all, la selection SH-2 change : les chiffres ne constituent donc pas un avant/apres de code a selection identique. Regions liees, lectures cache natives experimentales et instrumentation desactivees ; regroupement FRT actif sur toutes les cartes 32X, y compris l interpreteur. Les replis 68000 et autres limites subsistent : ce n est pas un dynarec integral de toute la machine. Le GPU complet n est toujours pas implemente et ne fait pas partie des colonnes demandees ici.

Images completes et PCM compares apres chaque trame, champs CPU/RAM suivis tous les 60 ; sur 32X, contexte des peripheriques materialise avant comparaison, avec registres/cycles/cache/IO/SDRAM verifies. Echeances privees _busDeadline/_nextPeripheralEvent exclues comme dans les derniers bancs FRT. Les 20 passages terminent sans difference aux points controles ; cela ne prouve pas la compatibilite de toute une partie ni de tous les jeux.

### Debug - FPS

| Jeu | Plateforme | Interpreteur | Dynarec existant | Registres | ROM stable | Combine |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 208,03 | 289,80 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 200,09 | 278,25 | Sans objet | Sans objet | Sans objet |
| Sonic | Mark III | 189,07 | 232,69 | Sans objet | Sans objet | Sans objet |
| Sonic | Master System | 182,94 | 229,76 | Sans objet | Sans objet | Sans objet |
| Sonic | Game Gear | 242,14 | 325,97 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 41,18 | 45,84 | 46,11 | 46,19 | 45,73 |
| Aladdin | Mega Drive | 30,92 | 35,02 | 34,90 | 34,85 | 34,87 |
| Batman Returns | Mega-CD | 31,39 | 37,98 | 37,66 | 37,98 | 37,76 |
| Doom | 32X | 21,88 | 17,82 | 18,06 | 17,96 | 17,97 |
| Night Trap, disque 1 | Mega-CD + 32X | 24,49 | 22,73 | 22,62 | 22,78 | 22,67 |

### Release - FPS

| Jeu | Plateforme | Interpreteur | Dynarec existant | Registres | ROM stable | Combine |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 2456,11 | 2628,28 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 4198,39 | 3916,54 | Sans objet | Sans objet | Sans objet |
| Sonic | Mark III | 1709,26 | 1500,07 | Sans objet | Sans objet | Sans objet |
| Sonic | Master System | 3162,04 | 2649,35 | Sans objet | Sans objet | Sans objet |
| Sonic | Game Gear | 3766,52 | 2457,66 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 799,99 | 864,68 | 864,40 | 892,42 | 883,75 |
| Aladdin | Mega Drive | 710,24 | 709,63 | 713,31 | 717,51 | 715,98 |
| Batman Returns | Mega-CD | 533,85 | 528,30 | 515,78 | 526,99 | 519,32 |
| Doom | 32X | 188,70 | 94,46 | 94,58 | 93,01 | 93,81 |
| Night Trap, disque 1 | Mega-CD + 32X | 197,41 | 107,64 | 107,00 | 107,23 | 106,71 |

### Dernieres 180 trames des passages 32X

| Configuration | Jeu | Interpreteur | Dynarec existant | Registres | ROM stable | Combine |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Debug | Doom | 21,88 | 17,82 | 18,06 | 17,96 | 17,97 |
| Debug | Night Trap, disque 1 | 18,80 | 15,95 | 15,93 | 15,97 | 15,98 |
| Release | Doom | 189,66 | 102,69 | 102,78 | 101,27 | 102,52 |
| Release | Night Trap, disque 1 | 90,79 | 45,12 | 44,57 | 44,41 | 44,23 |

Bilan et decisions :
- Bon sur ces passages : concordance des sorties/champs compares ; gain Debug de la selection native sur les cinq machines Z80, Pico, MD et MCD. Pico gagne aussi en Release. Conserver ces preuves fonctionnelles et ces observations limitees aux scenarios.
- Mauvais en Release : SC-3000, Mark III, SMS et GG perdent avec le Z80 natif. MD est presque a egalite ; MCD perd legerement. Ne pas presenter les variantes Registres/ROM stable comme une acceleration generale ni retenir les petits ecarts comme acquis.
- Mauvais sur 32X/CD32X : les quatre candidates restent sous l interpreteur dans les deux configurations. La moyenne Night Trap masque encore une phase active bien plus lente, visible dans la fenetre finale. La reparation de performance demandee n est pas terminee ; ne pas promouvoir ces routes pour leur vitesse.
- Diagnostic du banc : en Debug les compteurs SH-2 ont ete collectes apres Dispose, qui detache l adaptateur : le champ diagnostics contient donc des null, sans effet sur les durees ni les controles deja executes. Capture corrigee avant Dispose pour Release, sans autre changement du scenario ou du chronometrage. Program-Debug.cs.txt et Program.cs.txt conservent exactement les deux sources. Les quatre candidates Release attestent des instructions SH-2 natives et zero corps CIL SH-2 construits ; cela ne signifie pas zero replis 68000.

Preuves : artifacts/Emulation/segaemu-autres-jeux/attempts/fresh-all-after-sh2-20261117 : vingt JSON avec ticks/checkpoints/SHA des DLL chargees, summary.json, logs, deux sources du banc, projet/cas/script et DLL archives. Banc temporaire/bin/obj supprimes apres fin des processus, aucun commit/push.

## Lectures de donnees du cache SH7095 dans les payloads natifs - 17 novembre 2026

Cause traitee : une lecture deja presente dans le cache SH7095 repassait par le callback natif -> C# et les copies de contexte. Ajout d une vue empruntee ISh2NativeReadCacheBus (cache/tags/validite/controle/politique live) et d une selection UseNativeCachedReads, fausse par defaut. Le payload lit directement les hits purs zero attente ; garde de mode, echeance, plage, taille/limite de ligne et voies. Les misses, IO, acces avec attente et toutes les ecritures gardent le callback original. Cache RAM et lecture big-endian/signe conserves. Les tableaux sont epingles seulement pendant l appel natif, y compris les callbacks/GC ; pas de ressource native durable ajoutee par cette vue. Aucun changement de Sh2.cs ni de MD/MCD sans 32X dans cette passe.

61 controles cibles passent en Debug et Release, dont matrice 5760 combinaisons de taille/alignement/voie/CCR/mode/plage, echeances dues et changement de mode avec GC compactant en callback, execution complete avec MAC, fautes/reprise et vidage des caches en callback. Les tests verifies incluent aussi les suites SH-2 natives, FRT et unwind Windows ; ce n est pas toute la suite Chips ni toute la compatibilite jeux.

Trois cartes neuves par passage : interpreteur / dynarec multi-instructions existant avec callbacks / meme dynarec avec lectures cache natives. FRT regroupe actif sur les trois ; regions liees et instrumentation desactivees dans les mesures FPS. 60 trames de chauffe puis 900 mesurees dans les deux configurations. Seul RunFrame chronometre, chargement/controles exclus, sans fenetre, limiteur ni audio hote. Entrees identiques, Start comme dans les derniers bancs. Images/PCM verifies chaque trame ; CPU/cache/IO/RAM/SDRAM et main/sub68000 controles tous les 60 apres materialisation des peripheriques. Les echeances privees _busDeadline/_nextPeripheralEvent sont exclues comme dans le banc FRT. Les quatre passages terminent sans divergence aux points controles, pas une preuve de compatibilite de toute une partie.

### Debug - FPS

| Jeu | Plateforme | Interpreteur neuf | Dynarec callbacks | Dynarec lectures cache natives |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 20,44 | 16,85 | 15,91 |
| Night Trap disque 1 | Mega-CD + 32X | 23,87 | 22,20 | 22,93 |

### Release - FPS

| Jeu | Plateforme | Interpreteur neuf | Dynarec callbacks | Dynarec lectures cache natives |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 188,66 | 96,64 | 92,15 |
| Night Trap disque 1 | Mega-CD + 32X | 186,47 | 108,81 | 127,55 |

### Fenetre finale - trames 781 a 960

| Configuration | Jeu | Interpreteur | Dynarec callbacks | Dynarec cache natif |
| --- | --- | ---: | ---: | ---: |
| Debug | doom | 19,82 | 16,80 | 15,85 |
| Debug | nighttrap | 18,64 | 15,60 | 17,01 |
| Release | doom | 189,77 | 103,10 | 99,47 |
| Release | nighttrap | 99,47 | 48,78 | 63,65 |

Diagnostic distinct, uniquement Release, 60 trames apres 780, deux cartes natives instrumentees par jeu. Les deux profils terminent sans divergence aux controles sorties/etats ; ne pas utiliser leur duree instrumentee comme FPS normaux. Night Trap master : lectures callbacks 8 121 896 -> 535 898 ; 7 585 998 hits natifs, exactement la difference, soit 93,40 % des lectures de cette fenetre. Callbacks totaux master 11 468 910 -> 3 882 912. Slave : aucun hit, callbacks inchanges. Doom : aucun hit sur master/slave dans cette fenetre, memes comptes de callbacks ; les gardes/pins/volume de code du chemin ajoute n y evitent aucun callback. Le profil etablit l absence de travail evite, pas la part exacte de chacun de ces surcouts dans la perte FPS.

Decisions :
- Bon mais seulement sur une partie des cas : Night Trap gagne +3,28 % Debug et +17,23 % Release sur la moyenne ; +8,99 % Debug et +30,48 % Release sur les 180 dernieres trames. Conserver la selection et ses preuves ; gain local prouve sur ce passage, pas un gain general ni une victoire sur l interpreteur.
- Mauvais : Doom perd -5,61 % Debug et -4,65 % Release sur la moyenne. Ne pas activer globalement UseNativeCachedReads ni remplacer la voie originale. Ne pas refaire cette meme proposition de lookup inline universel comme une nouvelle idee sans traiter le chemin sans hit.
- Limite persistante : meme avec cette lecture native, Night Trap reste sous l interpreteur et sa phase active Release ne donne que 63,65 FPS contre 99,47. Les autres couts de parcours natif demeurent. Ce resultat ne valide pas un dynarec integral ni un GPU complet.

Preuves : artifacts/Emulation/segaemu-autres-jeux/attempts/sh2-cached-reads : quatre JSON FPS, deux JSON diagnostiques, summary.json, late-window.json, TRX Debug/Release, sources/projets/scripts/logs et DLL/SHA archives. Banc temporaire .work/Sh2CachedReads et ses bin/obj supprimes apres fin des processus ; tests autonomes utiles conserves dans tests. Aucun commit/push.


## Contexte CPU SH-2 direct, 17 novembre 2026

Constat : le bus natif restaurait puis recapturait le contexte architectural a chaque callback. Le Z80 natif existant montre deja comment travailler directement dans les champs CPU avec des offsets CLR et un pin interieur. Nouvelle selection UseLiveX64Context false par defaut : offsets derives par references gerees Unsafe.As/ByteOffset, CPU epingle par PC pendant tout Execute et R epingle separement ; mots 64 bits/booleens de largeur exacte. Aucun offset d objet CLR code en dur. Les callbacks voient et modifient directement les vrais registres, IRQ, cycles, echeances et flags. Les donnees de traduction supplementaires ne sont pas serialisees. La selection reste hors Execute et invalide les blocs.

Premier essai : vue commune NativeX64Frame a base de tableau d offsets dans les boucles gerees. Il modifiait aussi inutilement le parcours original. Archive separee initial/ : quatre JSON, logs, DLL et deux sources. Ne pas refaire cette vue commune comme une nouvelle optimisation. Correction : driver et callback Span originaux restaures depuis le snapshot precedent ; nouveau driver/callback distincts a champs CPU types. Emetteur des instructions, compilateur de blocs, validation/coherence et services de bus restent partages. Le petit dispatch de callback choisit le helper correspondant au pointeur courant, y compris les payloads autonomes a contexte logique. Les tables d offsets ne sont utilisees que pour emettre les instructions natives et non pour parcourir le contexte dans le driver gere.

Validation finale : 67 tests cibles passes Debug et Release. Nouvelles comparaisons interpreteur/contexte copie/direct sur familles d instructions et 1024 encodages tires avec graine fixe, en CPU simple et helpers specialises ; callbacks observant/modifiant registres/IRQ, GC compactant, fautes/reprise/retirement, bascule direct -> original. La suite cache native ajoute les deux cas de contexte direct avec MAC/changement de politique/fautes. Les autres controles concernent budgets/frontieres/timings/regions liees/FRT/unwind ; ce n est pas toute la suite Chips.

Quatre passages machines definitifs : trois cartes neuves par jeu/configuration, 60 trames de chauffe et 900 mesurees, seul RunFrame chronometre, pas de fenetre/limiteur/sortie audio hote. Meme scenario Start que les autres campagnes. Images/PCM chaque trame ; CPU/cache/IO/RAM/SDRAM et main/sub68000 tous les 60 avec peripheriques materialises. Les quatre passages et les deux profils finissent sans divergence aux points controles. NativeFull+Multi selectionnes sur les candidates, lecture cache native false pour isoler le contexte ; FRT regroupe true sur toutes. GPU complet absent ; replis 68000 persistants. MD/MCD sans 32X non touches par les sources de cette passe.

### Debug - FPS definitifs

| Jeu | Plateforme | Interpreteur neuf | Dynarec contexte copie | Dynarec contexte direct |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 19,23 | 15,59 | 16,08 |
| Night Trap disque 1 | Mega-CD + 32X | 23,96 | 21,94 | 22,66 |

### Release - FPS definitifs

| Jeu | Plateforme | Interpreteur neuf | Dynarec contexte copie | Dynarec contexte direct |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 174,26 | 87,01 | 93,48 |
| Night Trap disque 1 | Mega-CD + 32X | 174,94 | 100,35 | 111,26 |

### Dernieres 180 trames - 781 a 960

| Configuration | Jeu | Interpreteur | Contexte copie | Contexte direct |
| --- | --- | ---: | ---: | ---: |
| Debug | doom | 18,78 | 15,69 | 16,15 |
| Debug | nighttrap | 18,17 | 14,81 | 15,84 |
| Release | doom | 179,26 | 94,13 | 102,69 |
| Release | nighttrap | 89,63 | 43,90 | 50,11 |

### Premier essai conserve, sans melanger ses temoins

| Configuration | Jeu | Interpreteur | Vue commune, copie | Vue commune, direct |
| --- | --- | ---: | ---: | ---: |
| Debug | doom | 20,27 | 16,24 | 16,38 |
| Debug | nighttrap | 24,80 | 22,07 | 22,52 |
| Release | doom | 182,13 | 88,86 | 93,90 |
| Release | nighttrap | 181,74 | 102,42 | 112,42 |

Profil Release distinct des FPS : 60 trames apres 780, instrumentation TSC des deux SH-2 dans les deux selections, memes controles. Nombre de callbacks identique : Doom 1 699 224, Night Trap 12 065 988 (master+slave). Intervalles callbacks inclusifs Doom 610 583 352 -> 513 438 945 ticks TSC (-15,91 %) ; Night Trap 3 546 137 317 -> 2 933 614 645 (-17,27 %). Ne pas appeler ces ticks du temps CPU propre ni une fraction de toute la machine. Le cout des callbacks baisse, mais le gain machine reste limite.

Compteurs 68000 du meme profil, differences 780 -> 840 : Doom main 693 960 instructions de repli, zero nouvelle entree dans un bloc natif (une seule entree cumulee), identiques pour les deux routes SH-2. Night Trap main 420 replis, sub zero ; des blocs 68000 sont bien executes. Ce constat pointe un travail distinct et deja liste : les banques 32X a handlers bloquent les fenetres de code 68000. Il ne prouve pas leur part en temps et n autorise pas a supprimer leur synchronisation sans verification.

Decisions/limites :
- Retenir la selection directe comme candidate fonctionnelle, sans l activer par defaut : +3,10 % Doom / +3,30 % Night Trap Debug, +7,43 % / +10,87 % Release dans la campagne definitive. C est un gain limite, insuffisant pour valider l acceleration globale ou depasser l interpreteur.
- Rejeter la vue commune a offsets dans le driver gere ; conserver le parcours Span d origine pour comparaison et les deux jeux de preuves. Les methodes Span de boucle et callback sont comparees textuellement au snapshot precedent dans original-driver-preservation.json.
- Sh2.cs est inchange. Cela ne prouve pas a lui seul l absence de regression de vitesse de l interpreteur : la classe recoit des champs de backend et NativeRuntimeBuilder copie des helpers supplementaires. Les FPS interpreteur definitifs sont sous ceux d une campagne precedente ; aucune cause de cet ecart n est etablie. Gate restant avant promotion : comparer anciennes DLL et DLL actuelles dans un passage apparie avec meme scenario, pas recycler les anciennes valeurs.
- Aucun resultat GPU, aucune affirmation de dynarec integral. La correction globale reste en cours ; prochaine priorite de code apres controle de la reference : replis 68000 Doom et synchronisation des fenetres ROM 32X.

Preuves : attempts/sh2-live-context : initial/ conserve, quatre JSON FPS definitifs, deux profils, summary.json, late-window.json, TRX, logs, sources/projets/scripts et DLL/SHA archives. Banc .work/Sh2LiveContext et ses bin/obj supprimes apres sortie des processus, nouveaux tests autonomes conserves. Aucun commit/push.

## Interpreteur : comparaison appariee des DLL avant/apres contexte direct - 17 novembre 2026

Objectif : verifier la reference interpreteur apres l ajout du backend SH-2 direct, sans recycler les anciens FPS. Anciennes DLL : attempts/sh2-cached-reads/dll/Benchmark ; actuelles : DLL construites pour sh2-live-context. Meme worker binaire pour les deux versions, aucune API du nouveau backend appelee. MachineResources native false ; les deux SH-2 verifies UseCompiledBlocks=false. Aucun changement du code de production pendant cette verification.

Deux AssemblyLoadContext independants dans un meme processus par configuration. Les DLL effectivement chargees sont verifiees sous leurs dossiers respectifs et leurs SHA256 releves. Ordre d execution alterne chaque trame. 60 chauffe + 900 mesurees pour chacun des quatre passages Doom/Night Trap x Debug/Release. Chronometre dans le worker autour de RunFrame seulement : reflection, chargement et controles exclus ; pas de fenetre/limiteur/audio hote. Images et PCM compares chaque trame ; champs CPU/RAM/cache/SDRAM/peripheriques materialises compares tous les 60, selon les snapshots des derniers bancs. Les huit cartes sont fermees dans finally ; les huit contextes sont decharges et leur collecte verifiee par WeakReference apres retour des frames proprietaires.

Le protocole a deux versions isolees differe des precedentes campagnes a plusieurs cartes dans le meme contexte. Les valeurs absolues de ces protocoles ne sont pas un avant/apres ; les deux colonnes ci-dessous constituent la comparaison appariee. Aucun FPS ancien reutilise.

### Debug - FPS interpreteur

| Jeu | Plateforme | Anciennes DLL | DLL actuelles | Ecart |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 20,75 | 19,78 | -4,69 % |
| Night Trap disque 1 | Mega-CD + 32X | 24,66 | 24,22 | -1,81 % |

### Release - FPS interpreteur

| Jeu | Plateforme | Anciennes DLL | DLL actuelles | Ecart |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 162,30 | 166,86 | +2,81 % |
| Night Trap disque 1 | Mega-CD + 32X | 200,46 | 198,33 | -1,06 % |

### Fenetre finale, 180 trames - 781 a 960

| Configuration | Jeu | Anciennes DLL | DLL actuelles |
| --- | --- | ---: | ---: |
| Debug | doom | 20,23 | 19,29 |
| Debug | nighttrap | 18,97 | 18,74 |
| Release | doom | 169,55 | 174,76 |
| Release | nighttrap | 95,10 | 92,88 |

Bilan : quatre passages complets sans divergence aux points controles, huit contextes effectivement liberes. La baisse generale de l interpreteur supposee depuis les valeurs des campagnes precedentes n est pas confirmee : Doom Release +2,81 %, Night Trap -1,06 %. Le retrait Debug Doom -4,69 % et Night Trap -1,81 % est conserve, y compris la fenetre finale ; ne pas le presenter comme zero perte. Un passage par jeu/configuration ne suffit pas a en attribuer la cause a la disposition des champs, au JIT ou a autre chose. Aucune cause de charge hote invoquee. Cela ne valide pas toutes les machines ou tous les jeux, et ne transforme pas le dynarec plus lent en acceleration validee.

Echec du banc conserve sous unload-failed/ : premiere comparaison Doom terminee sans divergence, mais les contextes restaient retenus ; JSON Success=false/Fps=null, pas un resultat de performance retenu. Le worker serialisait ses metadonnees avec des types anonymes definis dans son assemblee collectible, a travers le cache JSON par defaut. Remplacement par des dictionnaires de types primitifs ; passage court Debug de 61 trames avec collecte verifiee sur Doom et Night Trap (smoke/), puis quatre nouveaux passages complets avec collecte verifiee. Ne pas reutiliser le modele de metadata anonyme dans ce banc. Aucune modification de SegaEmu pour corriger ce probleme du banc.

Decision : reference fonctionnelle controlee, absence de grande baisse generale dans ce passage, petits retraits conserves dans les notes. Le backend direct reste selectionnable et non active par defaut. Le verrou prioritaire constate dans le profil SH-2 reste le 68000 de Doom presque entierement en repli, a traiter sans supprimer aveuglement les effets des handlers 32X.

Preuves : attempts/sega-interpreter-reference : quatre JSON complets, summary.json, SHA des DLL reellement chargees, DLL/worker/runner/sources/scripts/logs archives ; unload-failed/ et smoke/ conserves. .work/SegaInterpreterReference et bin/obj/staged supprimes apres fermeture des cartes, collecte des contextes et fin des processus. Aucun commit/push.


## Nouvelle campagne complete a la demande utilisateur - 17 novembre 2026

Vingt nouveaux passages, dix plateformes, cinq cartes independantes par passage : 100 cartes creees et liberees dans finally. Tous les FPS ci-dessous proviennent des JSON de cette campagne, interpreteur compris. Aucun FPS historique repris. Builds cibles Chips/Sega et dependances Debug puis Release : zero erreur, 62 avertissements par configuration ; banc : voir logs archives. Aucun code de production modifie pendant cette campagne, aucun commit/push.

Protocole : machine emulee complete sans fenetre ni limiteur, chargement hors chronometre ; 60 trames de chauffe puis 180 trames mesurees en Debug et 900 en Release. Seul RunFrame est chronometre. Ordre des cinq routes tourne a chaque trame. Memes images/audio/entrees par jeu ; image et PCM compares a chaque trame, etats CPU/RAM/cache/peripheriques compares tous les 60 apres materialisation des horloges SH7095. Vingt passages sans divergence aux points controles. Il ne s agit pas d une validation de partie complete. Les deux configurations ne couvrent pas la meme longueur : ne pas comparer directement leur moyenne comme un facteur de vitesse Debug/Release.

Selections : Interpreteur = parcours interprete neuf ; Dynarec existant = backend natif disponible, avec replis 68000 restants ; Registres = UseNativeGuestRegisters ; ROM stable = UseNativeStableCode ; Combine = les deux options. Pour 32X/CD32X, SH-2 Full+Multi actif sur les quatre candidates ; linked regions, native cached reads, live native context et instrumentation temporelle desactives. FRT regroupe actif sur toutes les cartes. La fenetre ROM 32X experimentale en cours de correction reste desactivee. Z80 natif selectionne sur les candidates. Registres/ROM stable/Combine sont des options 68000 : sans objet sur SG/SC/MarkIII/SMS/GG ; les quatre repetitions natives independantes de ces lignes restent archivees mais ne sont pas quatre routes Z80 distinctes. Aucun FPS GPU : cette colonne ne fait pas partie du tableau demande et l emulation complete GPU n est pas implementee.

### Debug - FPS

| Jeu | Plateforme | Interpreteur | Dynarec existant | Registres | ROM stable | Combine |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 161,80 | 209,21 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 158,76 | 210,62 | Sans objet | Sans objet | Sans objet |
| Sonic | Mark III | 148,64 | 178,29 | Sans objet | Sans objet | Sans objet |
| Sonic | Master System | 151,77 | 181,56 | Sans objet | Sans objet | Sans objet |
| Sonic | Game Gear | 202,00 | 269,40 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 37,15 | 40,85 | 40,95 | 40,96 | 41,10 |
| Aladdin | Mega Drive | 28,81 | 32,27 | 32,29 | 32,64 | 32,66 |
| Batman Returns | Mega-CD | 30,36 | 36,61 | 36,19 | 36,57 | 36,52 |
| Doom | 32X | 16,53 | 13,05 | 13,75 | 13,57 | 11,72 |
| Night Trap, disque 1 | Mega-CD + 32X | 30,77 | 33,50 | 33,53 | 33,42 | 32,76 |

### Release - FPS

| Jeu | Plateforme | Interpreteur | Dynarec existant | Registres | ROM stable | Combine |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 3708,68 | 3207,83 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 4760,50 | 4433,03 | Sans objet | Sans objet | Sans objet |
| Sonic | Mark III | 3833,51 | 2991,47 | Sans objet | Sans objet | Sans objet |
| Sonic | Master System | 4155,92 | 3226,87 | Sans objet | Sans objet | Sans objet |
| Sonic | Game Gear | 4114,49 | 2258,34 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 773,27 | 858,53 | 849,73 | 877,78 | 880,58 |
| Aladdin | Mega Drive | 708,62 | 682,66 | 650,17 | 706,86 | 698,55 |
| Batman Returns | Mega-CD | 420,29 | 542,48 | 542,62 | 551,08 | 538,26 |
| Doom | 32X | 177,03 | 83,53 | 83,22 | 83,08 | 83,84 |
| Night Trap, disque 1 | Mega-CD + 32X | 208,57 | 107,98 | 107,54 | 102,71 | 106,29 |

### Bilan de cette campagne

- A conserver comme observations : gain natif Debug sur les machines Z80, Pico, MD et MCD ; Pico et Batman Returns gagnent aussi en Release. Ces constats concernent les passages mesures, sans attribuer les petits ecarts a une cause non mesuree.
- A corriger : pertes natives Release sur les cinq machines Z80 et Aladdin ; Doom reste moins rapide en natif, et Night Trap Release passe de 208,57 FPS interpreteur a 102,71-107,98 FPS selon la candidate. Les quatre repetitions natives Z80 ne doivent pas etre interpretees comme des options Registres ou ROM stable ayant un effet sur ce processeur.
- Night Trap Debug ne couvre que les 240 premieres trames, chauffe comprise, et donc davantage le demarrage que les anciens passages de 960 trames. Sa moyenne ne prouve pas un gain dans la phase active ; le passage Release plus long contient cette phase. Les checkpoints et ticks cumules permettent d examiner cette difference.
- Rien ne justifie de promouvoir une route comme acceleration generale. L interpreteur reste disponible. La correction de performance globale du dynarec n est pas declaree terminee ; ce point livre la campagne complete demandee.

Preuves : artifacts/Emulation/segaemu-autres-jeux/attempts/fresh-all-now-20261117 : vingt JSON, summary.json, sources du banc en .txt, cas/scripts, logs builds/execution, DLL et SHA. Les SHA des DLL effectivement chargees figurent aussi dans chaque JSON. Aucun test externe permanent ajoute ; banc temporaire supprime apres fin effective des processus.



## Fenetres de code 68000 32X avec synchronisation de fetch - 17 novembre 2026

Correction experimentale selectionnable, originale conservee. Les aliases ROM 88-9F etaient refuses au compilateur 68000 a cause des handlers de banque. Ajout de fenetres ROM bornee/mirroirs et invalidation enable/banque/reset/restauration. Les fetchs purs sans synchronisation ont echoue : divergence native a la trame 6, interpreteur pur des la trame 4 (horloge/peripheriques SH-2). Ces echecs sont archives ; ne pas refaire cette suppression aveugle.

Correction : contrat reutilisable ICpu68000ObservedCodeBus et observations aux transitions de fetch emises par le bloc natif, y compris refresh/extensions differees. Les lectures de donnees gardent les handlers, notamment PC-relatives ; les BIOS/IO/framebuffer restent exclus. Selection UseMapped32XRomFetch false par defaut ; SynchronizeMapped32XRomFetch true quand la fenetre est choisie. L invalidation initialement detachee au reset a ete rebranchee dans Map32XBanks.

Validation : 20 controles ciblés reussis en Debug et Release (banques/miroirs/limites/invalidation, synchronisation et bascules, traces horloge originales/natives avec/sans extensions et registres caches). Quatre controles courts de 61 trames, puis huit passages longs de 60 chauffe + 900 mesurees. Images/PCM par trame et CPU/RAM/cache/peripheriques tous les 60 concordent. Trois cartes neuves par passage, detruites dans finally ; SHA des DLL chargees dans chaque JSON. Aucun ancien FPS. Ce controle cible les quatre machines affectees ; la derniere campagne toutes machines reste separee.

Selections natives : SH-2 Full+Multi actifs, linked/cache reads/live context false ; quatre machines complètes sans fenetre ni limiteur. La nouvelle fenetre 68000 est seule activee sur la troisieme carte. Les deux colonnes natives sur MD/MCD sans 32X sont des temoins identiques de code, leur difference ne mesure pas un effet de la fenetre 32X. Aucun changement de production pendant les mesures.

### Debug - FPS

| Jeu | Plateforme | Interpreteur neuf | Natif actuel | Natif + fenetre synchronisee |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 19,90 | 15,99 | 17,50 |
| Night Trap, disque 1 | Mega-CD + 32X | 22,60 | 20,63 | 21,99 |
| Aladdin | Mega Drive | 27,72 | 30,96 | 30,80 |
| Batman Returns | Mega-CD | 28,99 | 33,36 | 33,43 |

### Release - FPS

| Jeu | Plateforme | Interpreteur neuf | Natif actuel | Natif + fenetre synchronisee |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 157,90 | 80,28 | 81,13 |
| Night Trap, disque 1 | Mega-CD + 32X | 150,20 | 88,49 | 89,77 |
| Aladdin | Mega Drive | 549,22 | 584,87 | 583,08 |
| Batman Returns | Mega-CD | 368,47 | 394,36 | 391,77 |

Doom sur les 960 trames execute 11 184 526 instructions 68000 en repli dans le natif actuel, une seule entree native ; avec la fenetre synchronisee : zero repli 68000, 326 251 entrees et 745 blocs construits, identiques en Debug/Release. Cela ne signifie pas zero callback : les synchronisations sont maintenant des services appeles depuis les blocs emis.

Bilan : gain natif Doom Debug +9,44 %, Night Trap Debug +6,58 % ; en Release seulement +1,06 % et +1,45 %. Ces faibles gains ne reparent pas le deficit global face a l interpreteur. Garder la fenetre comme route de diagnostic/correction du refus de code ROM, sans la promouvoir comme solution de performance complete. Les temoins MD/MCD ne divergent pas. La suite doit isoler les sorties vers le C#, acces bus et synchronisations, comme demande par l utilisateur ; pas multiplier les opcodes natifs sans mesurer ces couts.

Preuves : attempts/m68k-32x-code : before-lifecycle-fix, unsynced-native, interpreter-window, observed/smoke et observed (huit JSON longs, deux TRX, DLL/SHA/sources). Le premier essai de compilation des nouveaux tests utilisait Cpu68000 sans parametre generique : test corrige en Cpu68000<MegaDriveCpuBus> avant execution. Banc temporaire/bin/obj supprimes apres fin effective des processus. Aucun commit/push.


## Diagnostic des passages natifs et synchronisations 32X - 17 novembre 2026

Suite a la piste utilisateur : profiler bus, synchronisations et sorties natives, plutot que poursuivre seulement les instructions SH-2. Deux nouveaux diagnostics Release Doom/Night Trap, chacun avec deux cartes natives (actuel / fenetre 68000 synchronisee), 780 trames de preparation puis 60 instrumentees. Images/audio compares par trame et etats materialises CPU/RAM/cache/peripheriques tous les 60 : aucune divergence. Aucun code de production modifie pendant ce diagnostic.

TSC : intervalles mesures depuis les payloads natifs, callbacks inclusifs regroupes lecture/ecriture/horloge/attente/autre. Compteurs distincts de payloads et instructions. Stopwatch : temps inclusif des handlers ROM 68000 88-9F et observations de fetch, avec detection des horloges identiques consecutives. Ces observations peuvent executer les SH-2 : ne pas additionner leur duree aux intervalles SH-2, ni les appeler cout propre de l interface. Pas de FPS normal calcule a partir de ce diagnostic instrumente.

| Jeu | Route | Instructions SH-2 | Appels payload | Callbacks | Part callbacks dans payload | Lectures | Ecritures | Horloge | Attente |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| doom | native-current | 6189965 | 3021174 | 1699224 | 48,79 % | 667783 | 351749 | 0 | 679692 |
| doom | native-observed-rom | 6189965 | 3021174 | 1699224 | 52,91 % | 667783 | 351749 | 0 | 679692 |
| nighttrap | native-current | 17410178 | 5384831 | 12065988 | 69,25 % | 8121896 | 1005447 | 2331852 | 597044 |
| nighttrap | native-observed-rom | 17410178 | 5384831 | 12065988 | 68,69 % | 8121896 | 1005447 | 2331852 | 597044 |

| Jeu | Route | Handlers ROM 68000 | Horloges handlers repetees | Observations fetch | Horloges observations repetees | Temps inclusif (ms) |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| doom | native-current | 1732680 | 1038480 | 0 | 0 | 786,57 |
| doom | native-observed-rom | 0 | 0 | 694320 | 120 | 715,14 |
| nighttrap | native-current | 0 | 0 | 0 | 0 | 0,00 |
| nighttrap | native-observed-rom | 0 | 0 | 0 | 0 | 0,00 |

Constats et prochaine correction a cibler :
- Doom : la route nouvelle supprime 1 038 360 appels de fetch ROM sur cette fenetre et reduit les horloges repetees de 1 038 480 a 120. Les callbacks SH-2 restent strictement 1 699 224. Il ne reste pas de volume significatif de doublons de meme horloge a supprimer dans ce nouvel observateur ; ne pas reprendre la route pure rejetee ni sacrifier les synchronisations utiles.
- Night Trap : 12 065 988 callbacks SH-2, dont 8 121 896 lectures, 2 331 852 horloges, 1 005 447 ecritures et 597 044 services d attente. Environ 69 % du temps interne aux payloads est dans ces callbacks. Les 5 384 831 entrees de payload pour 17 410 178 instructions montrent aussi des blocs courts (environ 3,23 instructions par entree). Les lectures et leur transfert de contexte sont prioritaires ; le cout du retour au driver doit etre isole, pas assimile au temps des instructions emises.
- Doom : 3 021 174 entrees pour 6 189 965 instructions (environ 2,05 instructions par entree), environ 49-53 % du temps interne des payloads dans les callbacks, dont 679 692 services d attente. Un zero dans la categorie horloge ne signifie pas absence de synchronisation : celle-ci existe aussi dans les lectures, attentes et observes 68000.
- La part callbacks mesure un cout inclusif C#/bus/services et passages ABI. Elle ne prouve pas que tout ce temps soit gaspille dans les interfaces. Ne pas presenter un pourcentage propre de surcout d interface sans comparaison ou mesure specifique. Les compteurs de lecture cache native et la route de contexte direct existent deja : leurs essais/gains/pertes restent dans le journal et ne doivent pas etre refaits comme des idees nouvelles.
- Conserver la concordance fonctionnelle et les reductions de replis deja obtenues ; la route native complete reste plus lente que l interpreteur sur 32X, et cette campagne ne declare pas la reparation terminee. Aucun commit/push.

Preuves : attempts/sh2-bus-boundaries : deux JSON bruts, summary.json, sources en .txt, logs, DLL et SHA. Banc temporaire/bin/obj supprimes apres fin effective du processus et liberation de ses cartes dans finally.


## Attentes SH-2 certifiees emises en x64 - 17 novembre 2026

Cause ciblee apres le diagnostic des raccordements : les boucles BRA/NOP, NOP/BRA/NOP et DT/BF passent encore dans un helper C# pour calculer les iterations sautables. Ajout de UseNativeInlineIdle false par defaut et de ISh2NativeIdleBus : seuls les mots deja mappes, sans effet de bord et au timing certifie peuvent utiliser le calcul x64. Les autres cas conservent le helper. Aucun changement de Sh2.cs ni des instructions interpretees.

Calcul natif : borne min(budget, evenement), IRQ dont NMI, division entiere et maximum du compteur, maintien des registres/PC/PPC/PollingEpoch et horloges pendantes ; synchronisation a l echeance existante. Un evenement qui change les mots pendant le step courant fait abandonner le plan et relire via le helper. Le booléen AccelerateIdleLoops reste dynamique, y compris apres callback et avec contexte CPU direct. Aucun nouveau pointeur durable ou ressource native possedee.

Defaut de la premiere certification introduite dans cette passe : avec certains CCR, le fetch non cache et les attentes de donnees du helper different. Le test sur un vrai SH7095, 16 combinaisons basses du CCR, a reproduit l erreur (ccr-before-fix.trx). Restriction des aliases caches avec inhibition des instructions, sans retirer le chemin original. Les premiers quatre passages et deux profils reussis sont conserves sous pre-ccr-guard, mais ne sont pas les FPS finaux du code corrige. La premiere compilation avait aussi une erreur de visibilite du champ interne de l emetteur, corrigee ; log conserve.

Validation finale : 128 tests passes dans chaque configuration, dont comparaison interpreteur/natif inline sur cinq formes de boucle, acceleration active/inactive, deux echeances, contexte copie/direct, refus et bascule de selection, NOP modifie des la premiere horloge, IRQ, changement de politique, DT aux bornes et horloge au-dela de 32 bits ; controles existants SH-2/cache/FRT/unwind inclus. Quatre nouveaux passages longs du code final, 60 trames chauffe + 900 mesurees, trois cartes neuves par passage ; interpreteur remesure, images/audio par trame et etats CPU/RAM/cache/peripheriques aux points de 60 trames concordent. Fermeture dans finally, sans fenetre ni limiteur.

Colonnes : natif precedent = 68000 avec fenetre synchronisee, SH-2 Full+Multi ; nouveau = meme selection + UseNativeInlineIdle. NativeCachedReads, LiveNativeContext, regions liees et instrumentation temporelle false dans les mesures FPS. Les deux colonnes natives gardent donc les memes autres reglages. SH-2 ne concerne ici que 32X/CD32X : aucun test d une machine qui n utilise pas cette puce dans cette passe.

### Debug - FPS finaux

| Jeu | Plateforme | Interpreteur neuf | Natif precedent | Natif + attentes x64 | Gain natif |
| --- | --- | ---: | ---: | ---: | ---: |
| Doom | 32X | 18,30 | 15,76 | 16,23 | 2,97 % |
| Night Trap disque 1 | Mega-CD + 32X | 19,20 | 17,81 | 18,01 | 1,09 % |

### Release - FPS finaux

| Jeu | Plateforme | Interpreteur neuf | Natif precedent | Natif + attentes x64 | Gain natif |
| --- | --- | ---: | ---: | ---: | ---: |
| Doom | 32X | 154,88 | 78,72 | 82,43 | 4,71 % |
| Night Trap disque 1 | Mega-CD + 32X | 149,10 | 85,33 | 86,90 | 1,83 % |

### Diagnostic distinct - 60 trames actives Release (781-840)

| Jeu | Route | Callbacks attente | Tous callbacks SH-2 | Appels de payload |
| --- | --- | ---: | ---: | ---: |
| doom | native-observed-rom | 679692 | 1699224 | 3021174 |
| doom | native-observed-rom-inline-idle | 26316 | 1045848 | 3021174 |
| nighttrap | native-observed-rom | 597044 | 12065988 | 5384831 |
| nighttrap | native-observed-rom-inline-idle | 0 | 11468951 | 5384831 |

Bilan :
- Bon : concordance aux points controles, elimination de 96,13 % des callbacks attente Doom et de tous ceux de Night Trap sur cette fenetre. Le nombre total de callbacks baisse de 1 699 224 a 1 045 848 sur Doom et de 12 065 988 a 11 468 951 sur Night Trap. Garder cette route comme candidat mesurable, originale conservee et nouvelle option non activee par defaut.
- Limite/mauvais : les gains FPS finaux restent faibles et les deux routes natives restent sous l interpreteur. Ne pas transformer la reduction d une categorie de callbacks en validation du dynarec complet ni en gain de meme pourcentage sur la machine. Les lectures restent nombreuses ; les appels de payload ne baissent pas (3 021 174 et 5 384 831 sur la fenetre). Les retours au pilote C# entre blocs et les lectures restent a traiter, plutot que prolonger seulement les instructions arithmetiques.
- A ne pas refaire : certification generale du fetch sans verifier les attentes du helper et le changement de code pendant le step ; suppression de fetchs non certifies ; publier les chiffres pre-ccr-guard comme ceux du code final. Les essais contexte direct/cache natif/regions liees deja consignes restent des precedents a consulter.
- Les profils TSC/compteurs instrumentes sont separes des FPS normaux ; aucune addition de temps inclusifs imbriques ni attribution de tout le temps callback a l interface. Ce point ne termine pas le chantier global de performance. Aucun commit/push.

Preuves : attempts/sh2-inline-idle : quatre JSON finaux, deux profils finaux, summary.json, deux TRX finaux, echec CCR et premiers essais separes, builds/logs, sources/DLL/SHA archives. Banc temporaire/bin/obj supprimes apres sortie des processus et liberation des proprietaires dans finally.


## Essai SIMD du cache SH7095 retire - passage a Saturn, 17 novembre 2026

Prototype UseNativeVectorCachedReads : index entrelace des tags valides compare par SSE2, rejet precoce des plages non cachees, maintenance des tags et reconstruction a la restauration. 137 tests passes dans chaque configuration, incluant les matrices scalaires/SIMD, priorite/validite, purges/chargements/etats et GC. Quatre passages complets 60 chauffe + 900 mesurees, cinq cartes neuves, images/PCM et etats compares sans divergence aux points controles. Les chiffres ci-dessous sont ceux du prototype, pas des DLL restaurees.

### Debug - FPS prototype rejete

| Jeu | Interpreteur neuf | Natif precedent | Cache scalaire | SIMD | SIMD + contexte direct |
| --- | ---: | ---: | ---: | ---: | ---: |
| doom | 17,47 | 15,39 | 14,27 | 14,29 | 14,46 |
| nighttrap | 21,96 | 21,11 | 21,95 | 21,93 | 21,63 |

### Release - FPS prototype rejete

| Jeu | Interpreteur neuf | Natif precedent | Cache scalaire | SIMD | SIMD + contexte direct |
| --- | ---: | ---: | ---: | ---: | ---: |
| doom | 153,68 | 80,63 | 76,99 | 76,18 | 77,38 |
| nighttrap | 154,50 | 91,14 | 110,86 | 110,70 | 106,72 |

Diagnostic distinct Release, 60 trames actives : Doom zero hit natif de cache dans les deux routes, 1 045 848 callbacks. Night Trap : 7 585 998 hits dans les deux, 3 882 953 callbacks. SIMD ne supprime donc aucun callback supplementaire au cache scalaire. Aucun gain de vitesse etabli : Night Trap Release scalaire 110,86 contre SIMD 110,70 ; la combinaison SIMD+contexte direct baisse a 106,72. Ne pas generaliser le gain du contexte direct seul a son association au cache.

Decision : retirer uniquement le prototype SIMD ajoute dans cette passe, son index derive et son test autonome. Conserver interpreteur, cache scalaire, contexte direct, attentes x64 et fenetre 68000 precedents. Sources et DLL prototype archives sous prototype/ avec SHA. Restauration depuis les fichiers archives avant cette passe, puis builds Chips/Sega Debug/Release et 128 controles existants passes dans chaque configuration. Aucun nouveau FPS apres restauration : l utilisateur demande de passer maintenant a la machine Saturn ; ne pas poursuivre une campagne 32X supplementaire.

A ne pas refaire sans nouvelle preuve : remplacer la recherche scalaire par cet index SIMD ou activer globalement cache+contexte direct en supposant leurs gains additifs. Le cache natif garde un interet sur Night Trap et penalise Doom dans ces scenarios. Les millions de retours entre blocs et les couts hors payload restent ouverts ; aucune reparation globale annoncee. Aucun commit/push. Banc temporaire/bin/obj supprimes apres fermeture des proprietaires et fin des processus.

## Saturn — réveil et attente du rendu logiciel, 20 novembre 2026

Sega Rally Championship Europe dans l’interpréteur SegaEmu, cache SH‑2 désactivé. Un processus neuf par passage, 120 trames PAL hors mesure (2,4 secondes émulées), puis 120 secondes réelles. Les empreintes vidéo et PCM prises aux mêmes trames concordent.

### Debug

| Variante | FPS à 60 s | FPS à 120 s |
| --- | ---: | ---: |
| Réveils collectifs, attente bloquante | 21,22 | 17,24 |
| Réveils individuels, attente bloquante | 20,34 | 17,00 |
| Réveils individuels, attente active de Yabause | 19,68 | 16,25 |

### Release

| Variante | FPS à 60 s | FPS à 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | ---: | ---: | ---: |
| Réveils collectifs, attente bloquante | 75,67 | 68,24 | 34/60 |
| Réveils individuels, attente bloquante | 78,08 | 74,04 | 12/60 |
| Réveils individuels, attente active de Yabause | 67,79 | 59,92 | 50/60 |

L’attente active a été transcrite depuis vidsoft.c et titan.c. Elle ralentit ce scénario. Les attentes du VDP1, des plans VDP2 et de Titan changent ensemble. Chaque variante a été mesurée une fois par configuration. La version à réveils individuels et attente bloquante fournit le témoin de performance. Le banc temporaire a été supprimé après la fin des processus.

## Saturn — signal individuel du VDP1, 20 novembre 2026

Le fil VDP1 dort sur un événement à réinitialisation automatique, comme dans thr-windows.c. Sega Rally Championship Europe, BIOS EUR, interpréteur SegaEmu. Deux secondes réelles de chauffe par passage. Builds Debug et Release à zéro erreur et zéro avertissement.

| Configuration | Cache SH‑2 | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS de 60 à 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Désactivé | 25,97 | 30,69 | 26,95 | 19,34 | 14,91 | 60/60 |
| Debug | Activé | 16,96 | 16,38 | 15,82 | 13,81 | 11,42 | 60/60 |
| Release | Désactivé | 99,76 | 72,05 | 61,69 | 51,40 | 51,27 | 55/60 |
| Release | Activé | 74,48 | 62,44 | 57,73 | 52,64 | 51,74 | 59/60 |

Les empreintes sans cache concordent avec celles du passage précédent aux mêmes trames. Le cache modifie les empreintes, et aucune équivalence entre réglages n’est établie. Le protocole précédent utilisait 120 trames de chauffe. La comparaison des deux campagnes ne mesure pas le coût propre du signal VDP1. Le banc a été supprimé après la fin des processus.

## Saturn — tableaux imbriqués pour le cache SH‑2, 20 novembre 2026

Comparaison de la représentation précédente et d’une transcription à tableaux en ligne imbriqués de sh2cache.h. Sega Rally Championship Europe, interpréteur SegaEmu, cache activé. Processus séparés, 120 trames de chauffe et 120 secondes réelles ou 1 200 trames si ce seuil prend plus longtemps.

| Configuration | Version | 10 s | 20 s | 30 s | 60 s | Fin | Durée finale |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Témoin | 16,03 | 16,32 | 16,46 | 13,80 | 11,80 | 120,036 s |
| Debug | Tableaux imbriqués | 5,52 | 5,42 | 5,41 | 5,46 | 4,11 | 291,918 s |
| Release | Témoin | 75,51 | 62,75 | 58,34 | 53,46 | 54,42 | 120,006 s |
| Release | Tableaux imbriqués | 85,39 | 70,24 | 63,29 | 56,15 | 51,92 | 120,025 s |

Entre 60 et 120 secondes, le témoin Release passe 51 secondes sous 60 FPS, contre 57 pour les tableaux imbriqués. Les empreintes aux mêmes trames concordent entre les deux builds. Ne pas réutiliser ces indexations InlineArray imbriquées telles quelles. Le profil de leur coût précis reste à établir. Prototype retiré, compilations restaurées à zéro avertissement et zéro erreur, DLL Chips identiques aux témoins par SHA-256 dans les deux configurations. Banc temporaire supprimé après la fin des processus.

## Mega Drive + 32X — comparaison avant et après intégration Saturn, 20 novembre 2026

Doom (JU), trois BIOS 32X réels, même banc sur la carte SegaEmu sans publication vidéo ou audio de ManagedCore. Témoin c5b0120b8 compilé depuis une archive du dépôt, comparé au worktree. L’interpréteur utilise les réglages par défaut. Le mode dynarec utilise UseNative68000Blocks avec les blocs SH‑2 que la carte sélectionne par défaut pour ce mode. Passages séparés, 120 trames de chauffe, puis au moins 120 secondes et 1 200 trames. Les registres PC des SH‑2, la vidéo et le PCM concordent aux trames mesurées 600 et 1 200.

| Configuration | Mode | Témoin FPS | Worktree FPS | Durée témoin | Durée worktree |
| --- | --- | ---: | ---: | ---: | ---: |
| Debug | Interpréteur | 18,11 | 18,49 | 120,028 s | 120,052 s |
| Debug | Dynarec existant | 9,72 | 8,48 | 123,501 s | 141,520 s |
| Release | Interpréteur | 68,14 | 65,10 | 120,005 s | 120,015 s |
| Release | Dynarec existant | 25,77 | 23,02 | 120,015 s | 120,001 s |

De 60 à 120 s, l’interpréteur Release tombe sous 60 FPS 2 secondes sur le témoin et 11 sur le worktree. Le dynarec Release est sous le seuil les 60 secondes dans les deux versions. Un passage FPS par combinaison. Ce test ne couvre qu’une cartouche et n’explique pas encore l’écart entre commits.

Diagnostic Release séparé sur 820 trames après chauffe. À la trame 678, le témoin passe 6 712,2 ms sur 6 742,6 ms à construire 2 301 blocs SH‑2. Le worktree passe 6 621,1 ms sur 6 650,3 ms à construire les mêmes 2 301 blocs. Sur la fenêtre entière, les deux versions construisent 11 869 blocs, avec zéro repli compté par ce compteur SH‑2. Ce résultat établit la cause des longues pauses communes aux deux versions. Il ne donne pas la cause de la différence de débit sur 120 secondes, ni le nombre de replis 68000. Le banc et l’archive temporaires ont été supprimés après la fin des processus. Aucun changement du code de production.

## Mega Drive + 32X — pauses de compilation hybride et voie SH‑2 x64, 20 novembre 2026

Doom (JU), mêmes trois BIOS 32X et même scénario sans entrée que la comparaison précédente. SegaEmu worktree, carte 32X mesurée sans publication audio/vidéo. Processus séparés, 120 trames de chauffe, puis au moins 120 secondes et 1 200 trames. La voie x64 active Full+Multi et les attentes natives SH‑2 existantes en gardant la sélection 68000/Z80 du dynarec hybride. Les PC SH‑2, images et PCM concordent aux trames mesurées 600 et 1 200 pour les six passages. Builds à zéro erreur et zéro avertissement.

| Configuration | Mode | FPS finaux | Durée | Trame la plus lente | Corps CIL | Secondes sous 60 FPS de 60 à 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Debug | Interpréteur | 17,86 | 120,017 s | 87,35 ms | 0 | 60/60 |
| Debug | Hybride CIL/x64 | 9,60 | 125,002 s | 6 291,89 ms | 15 261 | 60/60 |
| Debug | SH‑2 x64 | 13,00 | 120,049 s | 183,37 ms | 0 | 60/60 |
| Release | Interpréteur | 77,38 | 120,007 s | 26,92 ms | 0 | 0/60 |
| Release | Hybride CIL/x64 | 28,21 | 120,009 s | 6 838,78 ms | 18 798 | 60/60 |
| Release | SH‑2 x64 | 35,27 | 120,041 s | 108,35 ms | 0 | 60/60 |

La voie SH‑2 x64 supprime les pauses de plusieurs secondes observées dans l’hybride, et reste plus lente que l’interpréteur. La trame la plus lente des voies compilées est la 678. En Release, le x64 exécute 641 666 975 appels de blocs natifs, chauffe comprise. Le témoin interprété a varié de 65,10 à 77,38 FPS sur le même worktree entre deux campagnes. Les variations entre séries ne permettent pas d’attribuer la baisse avant/après Saturn à un changement de code précis.

Le premier passage x64 Release a terminé les 120 secondes mais le banc a confondu le compteur commun de blocs compilés avec celui des corps CIL. L’assertion a été corrigée et les six résultats ci-dessus proviennent du banc corrigé. Aucun réglage de production changé. Banc supprimé après la fin des processus.

## Saturn — option de build YAB_OPTIMIZED_DMA, 20 novembre 2026

L’option du CMake de Yabause est raccordée dans RetroMediaLab.Chips.csproj avec la valeur par défaut désactivée. Le build avec -p:YAB_OPTIMIZED_DMA=true compile la branche OPTIMIZED_DMA du port de scu.c. Les deux variantes compilent en Debug et Release sans erreur ni avertissement. La présence de la table DmaMemoryType a confirmé la sélection de chaque DLL.

Sega Rally Championship Europe, BIOS EUR, interpréteur SegaEmu, cache SH‑2 désactivé, 120 trames de chauffe avant une mesure de 120 secondes. Les PC et empreintes vidéo/PCM concordent aux points contrôlés dans chaque configuration.

| Configuration | DMA optimisé | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS de 60 à 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Désactivé | 28,75 | 30,21 | 26,25 | 19,52 | 15,92 | 60/60 |
| Debug | Activé | 28,86 | 30,34 | 26,49 | 18,82 | 14,72 | 60/60 |
| Release | Désactivé | 104,87 | 75,14 | 64,36 | 53,08 | 46,21 | 58/60 |
| Release | Activé | 104,40 | 75,20 | 64,58 | 53,32 | 47,07 | 59/60 |

L’option reste désactivée par défaut, comme dans Yabause. Un passage par combinaison ne permet pas d’établir un gain général. Le banc vérifie la compilation de la branche et les sorties du scénario. Il ne compte pas les transferts qui prennent la copie optimisée. Banc temporaire supprimé après les quatre passages.

## Saturn — raccordement du SCSP2, 20 novembre 2026

Le projet Sega reprend l’option de compilation YAB_USE_SCSP2 de Yabause, désactivée par défaut. L’option active scsp2.c transcrit dans Chips. SegaEmu utilise son mode audio cadencé et protège le transfert PCM du fil SCSP2 vers les trames hôtes. Le banc confirme que le fil audio est actif dans les deux builds SCSP2.

Sega Rally Championship Europe, BIOS EUR, interpréteur, cache SH‑2 désactivé, 120 trames de chauffe et 120 secondes mesurées. Un passage par moteur et configuration. Les builds passent sans erreur ni avertissement.

| Configuration | Moteur audio | FPS à 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS de 60 à 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | SCSP | 29,44 | 30,49 | 26,47 | 18,76 | 14,65 | 60/60 |
| Debug | SCSP2 | 28,44 | 30,08 | 26,19 | 19,59 | 16,03 | 60/60 |
| Release | SCSP | 105,03 | 74,91 | 64,22 | 54,16 | 54,14 | 54/60 |
| Release | SCSP2 | 109,00 | 87,06 | 77,54 | 71,84 | 64,18 | 27/60 |

Les PC et empreintes vidéo concordent aux mêmes trames. Les empreintes PCM diffèrent. Sur 1 200 trames mesurées, le SCSP publie 1 058 400 échantillons par canal dans les deux configurations, le SCSP2 1 059 182 en Debug et 1 059 168 en Release. La comparaison porte sur la sortie produite et la cadence. Elle ne valide pas la qualité du son à l’écoute. Conserver le build optionnel SCSP2 pour poursuivre la validation. Le seuil de 60 FPS stables reste à atteindre. Le banc temporaire a été supprimé après la fin des processus.

## Saturn — réveil du fil SCSP2 selon Yabause, 20 novembre 2026

Transcription de la séquence de scsp2.c et du signal individuel de thr-windows.c. Le fil principal publie son horloge cible, réveille le fil SCSP2 et lui cède la main lorsque le retard dépasse la borne de la source. Le fil SCSP2 traite les écritures puis avance l’horloge avant de céder la main ou de dormir. Le signal est libéré après attente de l’arrêt du fil.

Sega Rally Championship Europe, BIOS EUR, interpréteur SegaEmu, cache SH‑2 désactivé, option YAB_USE_SCSP2=true dans les deux builds. 120 trames de chauffe, 120 secondes mesurées et un processus par mesure. Les quatre builds passent sans erreur ni avertissement.

| Configuration | Synchronisation | FPS à 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS de 60 à 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Moniteur | 28,38 | 30,18 | 26,34 | 19,67 | 16,09 | 60/60 |
| Debug | Signal individuel | 28,37 | 30,22 | 26,30 | 19,61 | 15,96 | 60/60 |
| Release | Moniteur | 108,48 | 86,06 | 76,64 | 61,78 | 50,81 | 59/60 |
| Release | Signal individuel | 109,13 | 84,10 | 73,93 | 64,75 | 59,86 | 47/60 |

PC et empreintes vidéo identiques aux trames contrôlées. En Release, de 60 à 120 s, le débit passe de 39,84 à 54,96 FPS environ. Un passage par combinaison. Les blocs PCM remis à chaque trame diffèrent. Sur 1 200 trames mesurées, le moniteur remet 1 059 154 échantillons par canal en Debug et 1 059 152 en Release, contre 1 059 210 et 1 059 230 avec le signal individuel. La qualité à l’écoute et l’équivalence du PCM en flux continu restent à vérifier. Banc temporaire supprimé après la fin des processus.

## Saturn — pointeur de fonction pour les lignes VDP1, 20 novembre 2026

Capture Release avec SCSP2 pendant 18 secondes à partir de la trame 3 600. Le profil échantillonne le temps mural de tous les fils, y compris les attentes. Dans ce rapport, IterateOverLine reçoit 3,66 % des échantillons exclusifs et DrawLineCallback 2,64 %. Les méthodes de boucle des fils VDP2 et Titan incluent du temps d’attente et leurs pourcentages ne doivent pas être lus comme du temps CPU de dessin.

SaturnVideoSoftware.cs reprend l’appel par pointeur de fonction de iterateOverLine dans vidsoft.c avec un pointeur managé et le même parcours de pixels. Sega Rally Championship Europe, BIOS EUR, interpréteur SegaEmu, SCSP2 actif, cache SH‑2 désactivé. 120 trames de chauffe, 120 secondes mesurées, un processus par passage. Les quatre builds passent sans erreur ni avertissement.

| Configuration | Appel du tracé | FPS à 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS de 60 à 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Délégué | 27,99 | 29,74 | 25,91 | 19,50 | 15,96 | 60/60 |
| Debug | Pointeur managé | 29,15 | 30,65 | 26,60 | 19,58 | 15,76 | 60/60 |
| Release | Délégué | 109,43 | 83,77 | 73,65 | 60,19 | 49,51 | 59/60 |
| Release | Pointeur managé | 108,78 | 81,99 | 71,55 | 61,61 | 56,84 | 51/60 |

PC et empreintes vidéo identiques aux trames contrôlées. Pendant la dernière minute Release, le débit passe de 38,84 à 52,06 FPS environ. Les blocs PCM du SCSP2 restent découpés différemment d’un passage à l’autre. Le résultat de vitesse provient d’un seul passage par combinaison. Le banc et la trace temporaires ont été supprimés après la fin des processus.

## Mega-CD + 32X — Night Trap et amorçage de Fahrenheit, 20 novembre 2026

Night Trap Europe disque 1, BIOS Mega-CD régional et trois BIOS 32X. Bande de test directe sur la carte SegaEmu, 120 trames de chauffe, puis 120 secondes mesurées. Start pendant cinq trames tous les 120 à partir de la trame 120. La voie SH‑2 x64 sélectionne Full+Multi et les attentes natives, comme dans la campagne Doom. Le 68000 et le Z80 ont la même sélection dans les deux variantes compilées. Builds Debug et Release sans erreur ni avertissement.

| Configuration | Mode | FPS à 60 s | FPS à 120 s | Trame la plus lente | Secondes sous 60 FPS de 60 à 120 s |
| --- | --- | ---: | ---: | ---: | ---: |
| Debug | Interpréteur | 20,72 | 19,40 | 81,17 ms | 60/60 |
| Debug | Hybride CIL/x64 | 18,39 | 17,30 | 2 299,37 ms | 60/60 |
| Debug | SH‑2 x64 | 15,63 | 14,82 | 364,73 ms | 60/60 |
| Release | Interpréteur | 82,29 | 80,58 | 47,19 ms | 0/60 |
| Release | Hybride CIL/x64 | 52,47 | 51,40 | 2 506,32 ms | 60/60 |
| Release | SH‑2 x64 | 43,43 | 38,99 | 110,57 ms | 60/60 |

Le 32X est actif et les deux SH‑2 exécutent du code en SDRAM aux points contrôlés. Leurs PC, la vidéo et le PCM concordent entre modes aux trames communes. Ce sont les trames 600 et 1 200 en Debug, et 600, 1 200, 1 800 et 3 000 en Release. Le SH‑2 x64 n’émet aucun corps CIL et évite la pause de plusieurs secondes. Il réduit la moyenne par rapport à l’hybride sur Night Trap, contrairement à Doom. La même sélection ne résout donc pas les vitesses des deux machines.

Fahrenheit USA a été amorcé avec le disque clé et échangé contre le disque 32X à la trame 600. Le 32X reste inactif et les deux PC restent à 00000140 aux trames 1 200 et 1 800 dans les deux configurations. Ce scénario ne permet pas de mesurer Fahrenheit en jeu. Aucun réglage de production changé. Banc temporaire supprimé après la fin des processus.

## Mega Drive + 32X — contre-mesure de la régression sur Doom

Doom (JU), trois BIOS 32X, aucun bouton, même banc sur la carte SegaEmu. Témoin c5b0120b8 compilé depuis une archive locale, comparé au worktree. Un processus à la fois, 120 trames de chauffe, puis 120 secondes réelles avec au moins 1 200 trames. L’ordre des versions est inversé à la seconde paire Release. Le Debug cible le dynarec hybride, qui avait le plus baissé dans le premier relevé.

| Configuration | Mode | Paire | Témoin FPS | Worktree FPS | Trame la plus lente témoin / worktree |
| --- | --- | ---: | ---: | ---: | --- |
| Release | Interpréteur | 1 | 70,42 | 76,41 | 44,37 / 29,47 ms |
| Release | Interpréteur | 2 | 76,43 | 76,54 | 27,47 / 27,64 ms |
| Release | Dynarec hybride | 1 | 28,23 | 27,55 | 6 635,87 / 6 806,00 ms |
| Release | Dynarec hybride | 2 | 28,13 | 28,08 | 6 779,29 / 6 811,39 ms |
| Debug | Dynarec hybride | 1 | 9,78 | 9,75 | 6 088,02 / 6 897,57 ms |

PC des deux SH‑2, empreintes vidéo et empreintes PCM concordants aux trames communes. La baisse initiale de l’interpréteur Release (68,14 à 65,10 FPS) ne se reproduit pas, et celle du dynarec est bien plus faible dans cette série (au plus 0,68 FPS en Release et 0,03 FPS en Debug). Ces mesures ne suffisent pas à attribuer le petit écart restant à une modification particulière.

La trame 678 reste la plus lente dans les six passages dynarec. En Debug, les deux versions émettent exactement 15 261 corps CIL à 1 200 trames. Le profil antérieur attribue l’essentiel de cette trame à la compilation des blocs SH‑2 dans les deux versions. Le dynarec reste sous 60 FPS chaque seconde entre 60 et 120 s. Le worktree compile sans avertissement ni erreur en Debug et Release. Aucun changement du code de production.

Le diff avec `c5b0120b8` ne montre aucune modification de la carte `Machines/ThirtyTwoX` ni des fichiers du moteur `Sh2*.cs` utilisé par le 32X. Côté bus partagé avec la Mega Drive, les changements portent sur les délégués du mapper de cartouche et de la fenêtre mémoire du Z80. Leur coût propre n’a pas été isolé. Les contre-mesures sur Doom ne justifient pas de retirer ces changements pour retrouver les FPS. Le coût confirmé dans le dynarec hybride reste la compilation en cours de jeu, observée sur les deux versions.

## Saturn — pointeur de fonction pour le rappel couleur VDP2

SaturnVideoSharedTypes.cs et SaturnVideoSoftware.cs reprennent le pointeur PostPixelFetchCalc de vidshared.h et ses appels dans vidsoft.c. Les fonctions DoNothing et DoColorOffset et leurs neuf sites d’appel gardent leur logique. Sega Rally Championship Europe, BIOS EUR, interpréteur SegaEmu, SCSP2 actif, cache SH‑2 désactivé, 120 trames de chauffe puis 120 secondes par passage.

| Configuration | Rappel couleur | Paire | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Délégué | 1 | 28,87 | 30,64 | 26,58 | 19,91 | 16,16 | 60/60 |
| Debug | Pointeur | 1 | 29,93 | 31,44 | 27,05 | 18,61 | 14,12 | 60/60 |
| Debug | Délégué | 2 | 28,96 | 30,50 | 26,52 | 18,76 | 14,54 | 60/60 |
| Debug | Pointeur | 2 | 29,44 | 31,04 | 26,80 | 20,21 | 16,32 | 60/60 |
| Release | Délégué | 1 | 104,39 | 79,97 | 70,45 | 63,22 | 59,73 | 38/60 |
| Release | Pointeur | 1 | 106,34 | 80,92 | 71,27 | 63,53 | 60,71 | 39/60 |

Les empreintes vidéo concordent aux trames communes. En Debug, l’écart change de signe quand l’ordre des deux passages est inversé. En Release, le seul passage par variante donne +0,98 FPS à 120 s et une seconde de plus sous 60 FPS entre 60 et 120 s. Le pointeur est conservé pour reprendre la forme de la source C. Le gain stable reste à établir. Les quatre builds passent à zéro erreur et zéro avertissement.

## Saturn — paramètres de ligne et table de mosaïque VDP2

SaturnVideoSharedTypes.cs et SaturnVideoSoftware.cs reprennent les pointeurs PlaneAddr et LoadLineParams du contexte de dessin de vidshared.h. Le parcours des plans sélectionne deux lignes de la table de mosaïque avant le rendu, comme dans vidsoft.c. Les fonctions d’adresse des deux paramètres rotatifs conservent la sélection par numéro déjà présente. Sega Rally Championship Europe, BIOS EUR, interpréteur SegaEmu, SCSP2 actif, cache SH‑2 désactivé, 120 trames de chauffe puis 120 secondes par passage.

| Configuration | Version | Paire | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Témoin | 1 | 28,18 | 29,90 | 26,01 | 20,00 | 16,51 | 60/60 |
| Debug | Pointeurs et mosaïque | 1 | 28,28 | 29,59 | 25,44 | 18,96 | 15,41 | 60/60 |
| Release | Témoin | 1 | 109,39 | 82,58 | 72,64 | 64,82 | 60,18 | 38/60 |
| Release | Pointeurs et mosaïque | 1 | 107,80 | 81,79 | 71,95 | 59,12 | 47,88 | 59/60 |
| Release | Témoin | 2 | 108,59 | 82,22 | 72,00 | 63,78 | 59,92 | 42/60 |
| Release | Pointeurs et mosaïque | 2 | 106,86 | 81,49 | 71,67 | 63,70 | 60,22 | 46/60 |

Les empreintes vidéo concordent aux trames communes. La baisse Release du premier passage ne se reproduit pas lors de la paire inversée. Le Debug repose sur une seule paire. Aucun gain stable n’est établi. La transcription est conservée pour sa correspondance avec les sources. Les quatre builds passent à zéro erreur et zéro avertissement. La comparaison porte sur les deux changements réunis et ne mesure pas leur coût individuel.

## Saturn — pointeurs dans les tables de l’interpréteur SH‑2, essai retiré

Sega Rally Championship Europe, BIOS EUR, interpréteur de SegaEmu, SCSP2 actif et cache SH‑2 désactivé. Un processus par passage, 120 trames de chauffe puis 120 secondes. Le candidat transcrit les pointeurs des tables de lecture d’instructions et de sélection des gestionnaires de sh2int.c dans le port C#. Les gestionnaires et le décodage restent les mêmes. Les PC des deux SH‑2 et les empreintes vidéo concordent aux trames communes.

| Configuration | Tables | Paire | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Délégués | 1 | 29,78 | 30,98 | 26,70 | 19,63 | 15,77 | 60/60 |
| Debug | Pointeurs | 1 | 30,38 | 31,46 | 27,14 | 19,85 | 15,93 | 60/60 |
| Release | Délégués | 1 | 106,63 | 81,29 | 71,42 | 63,67 | 60,83 | 42/60 |
| Release | Pointeurs | 1 | 111,80 | 84,53 | 73,57 | 59,88 | 48,83 | 58/60 |
| Release | Délégués | 2 | 108,62 | 82,56 | 72,41 | 64,24 | 60,68 | 43/60 |
| Release | Pointeurs | 2 | 111,19 | 83,80 | 73,35 | 59,35 | 48,29 | 59/60 |

La baisse Release se répète quand l’ordre des variantes est inversé. La moyenne baisse de 60,83 à 48,83 puis de 60,68 à 48,29 FPS. La paire Debug donne +0,16 FPS. Les mesures concernent les deux tables ensemble. Elles ne permettent pas d’attribuer la baisse à une table ni à un mécanisme du JIT. Les deux fichiers ont été restaurés depuis le témoin. Les builds du témoin, du candidat et de la version restaurée donnent zéro erreur et zéro avertissement. Les DLL Chips et Sega restaurées ont les mêmes SHA‑256 que le témoin dans les deux configurations.

## Saturn — débordements de l’unité de division SH‑2

SaturnSh2Core.cs reprend les cas INT32_MIN / −1 et INT64_MIN / −1 de Mednafen, src/ss/sh7095.inc. Ces deux divisions signées peuvent lever OverflowException dans le code C# antérieur. Les autres opérations de cette unité conservent la transcription de sh2core.c. Les deux builds vérifient les écritures via le bus, les résultats, les miroirs et le drapeau de débordement. Le 32 bits donne 0x80000000 et un reste nul sans lever le drapeau. Le 64 bits lève le drapeau, donne 0x7FFFFFFF si le signal est désactivé, ou le résultat partiel 0x00000004 et l’interruption attendue si le signal est activé. Le registre supérieur vaut 0x0000000A dans les deux contrôles 64 bits.

Sega Rally Championship Europe, BIOS EUR, interpréteur SegaEmu, SCSP par défaut et cache SH‑2 désactivé. 120 trames de chauffe puis 120 secondes, sans limiteur. Les images et les PC des SH‑2 concordent aux trames communes aux deux configurations.

| Configuration | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | 30,06 | 31,09 | 26,60 | 19,82 | 16,07 | 60/60 |
| Release | 109,50 | 83,17 | 72,89 | 64,63 | 60,32 | 38/60 |

Cette passe corrige deux cas de calcul. Les relevés décrivent le build corrigé et ne constituent pas une comparaison de vitesse avant/après. Les deux builds passent sans erreur ni avertissement.

## Saturn — coordonnées signées des tables de rotation VDP2

SaturnVideoShared.cs utilise le masque 0xFFFFC000 pour étendre le signe des coordonnées Px, Py, Pz, Cx, Cy et Cz avant la conversion 16.16. Le chemin flottant de Kronos et Yaba Sanshiro applique ce masque, et Mednafen vdp2.cpp lit ces six champs avec une extension de signe sur 14 bits. Le masque 0xFFF80000 du chemin fixe de Yabause perdait le signe après le décalage. Le contrôle couvre les six champs de chacun des paramètres A et B, les bornes négative et positive et le masquage des bits 14 et 15.

Sega Rally Championship Europe, BIOS EUR, interpréteur SegaEmu, SCSP par défaut et cache SH‑2 désactivé. 120 trames de chauffe, puis 120 secondes. Images et PC des SH‑2 concordants entre configurations aux trames communes. Les deux builds passent à zéro erreur et zéro avertissement.

| Configuration | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | 28,19 | 29,98 | 26,09 | 19,43 | 15,82 | 60/60 |
| Release | 108,88 | 82,58 | 71,97 | 63,67 | 60,39 | 43/60 |

Les FPS décrivent le code corrigé. Le contrôle des paramètres vérifie le décodage des coordonnées négatives. La campagne Sega Rally ne valide pas tous les modes de rotation.

## Saturn — motif hors carte des plans rotatifs VDP2

Le mode 1 utilise OVPNRA ou OVPNRB pour les coordonnées hors carte d’un plan en tuiles, d’après TileFetch::Fetch dans Mednafen vdp2_render.cpp. Le motif est décodé sur un mot, même si les entrées ordinaires de la carte occupent deux mots. Le changement est raccordé à la rotation avec coefficients, à la rotation simple et au parcours sans rotation qui réutilise le défilement. Le test ciblé vérifie le décodage et la limite de la carte, les deux motifs, leurs palettes et leurs retournements. Il n’est pas une capture d’un jeu employant ce mode.

Sega Rally Championship Europe, BIOS EUR, interpréteur SegaEmu, SCSP par défaut, cache SH‑2 désactivé. 120 trames de chauffe, 120 secondes par passage. Les images et les PC des deux SH‑2 concordent aux trames communes. Les deux builds passent sans erreur ni avertissement.

| Configuration | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | 32,72 | 31,54 | 26,52 | 19,69 | 16,05 | 60/60 |
| Release | 108,58 | 82,45 | 72,37 | 63,95 | 60,07 | 41/60 |


## Saturn — limites des plans rotatifs VDP2 en modes 2 et 3

SaturnVideoSoftware.cs applique les masques de Mednafen vdp2_render.cpp aux parcours avec coefficients, sans coefficients et au défilement utilisé en l’absence de rotation effective. Le mode 2 coupe aux limites de la carte ou du bitmap. Le mode 3 coupe au-delà de 511 sur chacun des axes. La coordonnée est ensuite ramenée aux dimensions du bitmap ou de la carte pour la lecture. Le contrôle ciblé des bornes passe dans les deux configurations. Il vérifie les coordonnées et ne couvre pas toutes les branches de rendu.

Sega Rally Championship Europe, BIOS EUR, interpréteur SegaEmu, SCSP par défaut, cache SH‑2 désactivé. 120 trames de chauffe puis 120 secondes sans limiteur. Les images et compteurs ordinaux des deux SH‑2 concordent aux trames 600 et 1 200. Les builds passent sans erreur ni avertissement.

| Configuration | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | 29,30 | 29,87 | 26,16 | 18,74 | 14,67 | 60/60 |
| Release | 109,11 | 82,69 | 72,37 | 64,08 | 60,96 | 41/60 |

Les FPS décrivent le code actuel. Cette mesure seule n’isole pas le coût du changement. En Release, les 3 470 trames des 60 dernières secondes donnent 57,84 FPS.


## Saturn — état des tuiles des paramètres rotatifs A et B

Mednafen conserve un TileFetcher par paramètre rotatif dans vdp2_render.cpp. Dans SaturnVideoSoftware.cs, les deux caches de coordonnées pouvaient retrouver la même tuile chacun à leur tour, alors que le dernier décodage avait écrasé les informations communes de palette, de caractère et de retournement. Le parcours en tuiles maintient désormais son état par paramètre et lit la taille du plan secondaire dans le champ PLSZ correspondant. Cette passe conserve le chemin bitmap existant.

Sega Rally Championship Europe, BIOS EUR, interpréteur, 120 trames de chauffe puis 120 secondes sans limiteur. Images et PC des deux SH‑2 concordants entre configurations aux trames 600, 1 200 et 1 800. Les deux compilations passent à zéro erreur et zéro avertissement, avec la vérification de vulnérabilité NuGet désactivée pour la restauration du banc local après l’échec de son accès réseau.

| Configuration | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | 26,89 | 29,89 | 26,29 | 19,49 | 15,64 | 60/60 |
| Release | 103,89 | 73,19 | 62,02 | 50,72 | 54,18 | 44/60 |

Le relevé Release est inférieur à celui de la passe précédente (60,96 FPS). La mesure compare des passages distincts et n’isole pas le coût du changement. L’alternance des tuiles A et B n’a pas été vérifiée directement dans une image dédiée.


### Comparaison directe du rendu A et B sur Sega Rally

Les quatre builds utilisent le même worktree. MSBuild substitue uniquement une copie de SaturnVideoSoftware.cs où la séparation des états A et B est annulée pour le témoin. Chacun des quatre builds compile sans erreur ni avertissement. La restauration du banc temporaire utilise les paquets locaux avec l’audit NuGet désactivé. Un processus à la fois. Scénario, BIOS et durée identiques à la mesure précédente. Les empreintes vidéo et les PC des SH‑2 concordent dans chaque paire, aux trames 600 et 1 200 en Debug et jusqu’à 4 800 en Release.

| Configuration | Variante | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Témoin | 26,38 | 28,82 | 25,36 | 18,38 | 14,32 | 60/60 |
| Debug | Correction | 28,85 | 30,25 | 26,34 | 18,80 | 14,70 | 60/60 |
| Release | Témoin | 103,34 | 72,64 | 61,49 | 50,04 | 43,85 | 58/60 |
| Release | Correction | 103,25 | 73,45 | 62,13 | 50,58 | 44,37 | 58/60 |

La baisse Release signalée entre campagnes ne se reproduit pas dans cette paire. Les deux variantes restent sous la cible, et le faible écart d’une seule paire par configuration ne suffit pas à établir un gain stable. L’alternance des tuiles A et B reste à vérifier sur une scène qui l’exerce explicitement.


## Saturn — profil des attentes vidéo après la trame 3 600

Sega Rally Championship Europe, BIOS EUR, interpréteur SegaEmu, SCSP par défaut, cache SH‑2 désactivé. Deux captures EventPipe dotnet-sampled-thread-time de 18 secondes, une par configuration, déclenchées après la trame 3 600. Les deux builds passent à zéro erreur et zéro avertissement. Le tableau répartit le temps mural échantillonné sur les piles ManagedCore.RunFrame, environ 17 993 ms par configuration. Les catégories sont exclusives. Les temps des travailleurs s’exécutent en parallèle et ne sont pas additionnés à ceux du fil d’émulation.

| Circuit du fil d’émulation | Debug (ms) | Debug (%) | Release (ms) | Release (%) |
| --- | ---: | ---: | ---: | ---: |
| Attente active du VDP1 identifiée dans sa propre méthode | 13 496,4 | 75,01 | — | — |
| Attente active des plans VDP2 identifiée dans sa propre méthode | 1 381,5 | 7,68 | — | — |
| Attente active de Titan identifiée dans sa propre méthode | 154,5 | 0,86 | — | — |
| VDP1 lancement et attente intégrée | 47,1 | 0,26 | 6 133,9 | 34,14 |
| HBlank/VBlank et appels intégrés | 192,1 | 1,07 | 7 647,3 | 42,56 |
| VDP2 et Titan, dessin attribué aux méthodes visibles | 803,7 | 4,47 | 830,9 | 4,62 |
| SH‑2 et bus sur les piles visibles | 648,5 | 3,60 | 800,5 | 4,46 |
| SCSP et C68K | 612,2 | 3,40 | 472,1 | 2,63 |
| SCU et interruptions | 234,9 | 1,31 | 469,8 | 2,61 |
| Bloc CD et SMPC | 110,6 | 0,61 | 93,5 | 0,52 |

Les tirets en Release indiquent que les piles ne séparent pas la méthode d’attente de son appelant. Les chiffres Release du VDP1 et de HBlank/VBlank ne distinguent donc pas précisément attente, copies et rendu. En Debug, VidsoftWaitForVdp1Thread représente à lui seul 75,01 % du temps mural du fil d’émulation. La boucle vide de cette méthode est celle de vidsoft.c. La comparaison antérieure des réveils et attentes avait changé VDP1, VDP2 et Titan ensemble. Ce profil identifie le VDP1 en premier pour isoler la différence entre attente active et attente bloquante. Aucune modification du moteur et aucun relevé FPS sous instrumentation.


## Saturn — attente bloquante de fin de dessin du VDP1

SaturnVideoSoftware.cs adapte au VDP1 l’attente sur condition du fil SCSP de Kronos. Le fil de dessin signale la fin sous verrou, y compris si le dessin a échoué. Le fil d’émulation bloque uniquement si le dessin est encore en cours, avec le même contrôle de fin et aux mêmes endroits. Il s’agit d’une adaptation de la synchronisation hôte. Le code vidsoft.c de Yabause utilise une attente active à cet endroit.

Sega Rally Championship Europe, BIOS EUR, interpréteur, SCSP par défaut, cache SH‑2 désactivé. Même worktree pour les quatre builds et seule l’attente VDP1 varie. Une paire par configuration, ordre inversé entre Debug et Release. 120 trames de chauffe, puis 120 secondes sans limiteur. Empreintes vidéo et PC des deux SH‑2 concordants aux trames communes, jusqu’à 1 200 en Debug et 6 000 en Release. Les quatre builds passent à zéro avertissement et zéro erreur. L’audit NuGet a été désactivé pour la restauration du banc temporaire à partir des paquets locaux.

| Configuration | VDP1 | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Attente active | 28,56 | 30,22 | 26,28 | 18,66 | 14,50 | 60/60 |
| Debug | Attente bloquante | 28,59 | 30,09 | 26,29 | 20,27 | 16,59 | 60/60 |
| Release | Attente active | 103,23 | 72,97 | 61,73 | 50,23 | 50,39 | 57/60 |
| Release | Attente bloquante | 110,55 | 86,61 | 77,03 | 69,29 | 63,74 | 42/60 |

L’attente bloquante est conservée. La moyenne Release entre 60 et 120 s passe de 50,55 à 58,20 FPS. La campagne ne valide pas encore une cadence stable de 60 FPS et ne couvre ni le PCM ni d’autres jeux.


## Saturn — attente bloquante des plans VDP2

SaturnVideoSoftware.cs applique aux plans VDP2 le mécanisme d’attente sur condition repris du SCSP de Kronos. Chaque travailleur signale la fin de son dessin sous le verrou existant, lors d’une fin normale, d’une exception ou du chemin d’arrêt. Le fil principal attend le même indicateur aux mêmes points et relâche le verrou pendant l’attente. Le témoin et la variante utilisent déjà l’attente bloquante du VDP1. La synchronisation Titan reste identique dans les deux builds.

Sega Rally Championship Europe, BIOS EUR, interpréteur, cache SH‑2 désactivé, SCSP par défaut. Une paire par configuration, ordre inversé entre Debug et Release, 120 trames de chauffe puis 120 secondes sans limiteur. Les quatre builds compilent à zéro erreur et zéro avertissement. Les images, les PC des deux SH‑2 et l’empreinte cumulée du PCM concordent à chaque point contrôlé, jusqu’aux trames 1 800 en Debug et 6 000 en Release.

| Configuration | Attente VDP2 | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Active | 29,18 | 30,33 | 26,40 | 20,19 | 16,46 | 60/60 |
| Debug | Bloquante | 30,23 | 31,23 | 26,83 | 20,36 | 16,56 | 60/60 |
| Release | Active | 111,75 | 87,17 | 77,65 | 69,37 | 64,11 | 40/60 |
| Release | Bloquante | 111,62 | 87,08 | 77,19 | 70,82 | 68,07 | 24/60 |

Variante conservée pour le gain Release observé, environ 6 %. La moyenne des 60 dernières secondes passe de 58,85 à 65,33 FPS et la cadence descend encore sous 60 FPS pendant 24 secondes. Le petit écart Debug ne démontre pas un gain reproductible. L’audit NuGet a été désactivé pour la restauration du banc temporaire avec les paquets locaux.


## Saturn — attente bloquante du compositeur Titan

SaturnTitan.cs applique aux bandes de composition le mécanisme d’attente sur condition du SCSP de Kronos. Les travailleurs publient la fin sous le verrou existant et réveillent les attentes, y compris en cas d’erreur ou d’arrêt. Les points de synchronisation et les bandes rendues sont les mêmes. Le témoin et la variante utilisent tous deux l’attente bloquante pour le VDP1 et les plans VDP2.

Sega Rally Championship Europe, BIOS EUR, interpréteur, SCSP par défaut, cache SH‑2 désactivé. 120 trames de chauffe puis 120 secondes sans limiteur, une paire par configuration et ordre inversé entre Debug et Release. Les quatre builds finaux passent à zéro erreur et zéro avertissement. Les images, le PCM cumulé et les PC des SH‑2 concordent aux trames contrôlées, jusqu’à 1 800 en Debug et 6 000 en Release.

| Configuration | Attente Titan | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Active | 29,33 | 30,38 | 26,50 | 20,20 | 16,47 | 60/60 |
| Debug | Bloquante | 28,65 | 30,16 | 26,23 | 20,00 | 16,38 | 60/60 |
| Release | Active | 111,98 | 87,14 | 77,60 | 69,73 | 65,01 | 33/60 |
| Release | Bloquante | 109,86 | 85,98 | 76,55 | 70,59 | 68,20 | 19/60 |

Variante conservée pour la meilleure cadence Release en jeu. La moyenne des 60 dernières secondes passe de 60,29 à 65,80 FPS. Le recul Debug de 0,09 FPS provient d’une seule paire, insuffisante pour confirmer une régression reproductible. Il reste 19 secondes sous 60 FPS en Release sur cette fenêtre. Le premier build du banc a échoué sur un contrôle de rotation hérité et retiré avant les mesures. La restauration du banc a utilisé les paquets locaux avec l’audit NuGet désactivé.


## Saturn — coefficients de rotation en RAM couleur

SaturnVideoSoftware.cs sélectionne la RAM couleur lorsque RAMCTL.CRKTE (bit 15) est actif. Comme dans GetCoeffAddr, ReadCoeff et SetupRotVars de Mednafen vdp2_render.cpp, la lecture est alignée et limitée aux 2 derniers Kio de CRAM et ce mode autorise la lecture par pixel. SaturnVideoShared.cs et SaturnVideoSharedTypes.cs conservent les quatre modes de coefficients, avec le format de mots déjà utilisé par les accès CRAM du VDP2. Le contrôle ciblé vérifie une lecture VRAM, deux formats CRAM, le retour d’adresse, le signe, le bit de transparence et l’indice de couleur de ligne.

Sega Rally Championship Europe, BIOS EUR, interpréteur, 120 trames de chauffe puis 120 secondes sans limiteur. Les compilations Debug et Release passent à zéro erreur et zéro avertissement, avec l’audit NuGet désactivé pour la restauration temporaire depuis les paquets locaux. Les images et PC des SH‑2 concordent aux trames communes 600, 1 200 et 1 800.

| Configuration | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | 27,27 | 30,02 | 26,26 | 19,88 | 16,21 | 60/60 |
| Release | 109,88 | 86,11 | 76,82 | 69,13 | 63,79 | 38/60 |

Les FPS décrivent cette version. Une mesure par configuration sans témoin simultané ne permet pas d’attribuer la variation au changement. Le contrôle des coefficients ne valide pas l’ensemble du rendu d’une scène utilisant CRKTE.


## Saturn — coût CPU du dessin VDP1 après les corrections d’attente

Sega Rally Championship Europe, BIOS EUR, interpréteur SegaEmu, cache SH‑2 désactivé et SCSP par défaut. Les attentes de VDP1, VDP2 et Titan sont bloquantes dans les deux builds. Les deux builds passent à zéro avertissement et zéro erreur. Deux captures séparées de 18 secondes après la trame 3 600. Les temps CPU viennent des compteurs Windows par identifiant de fil, sur 18 803 ms murales en Debug et 18 858 ms en Release. L’association aux composants utilise les signatures des boucles de travail dans les piles EventPipe. Les pourcentages CPU ont pour dénominateur la somme des temps CPU du processus. Les quatre lignes ci-dessous laissent de côté 62,5 ms et 78,1 ms CPU pour les autres fils.

| Fil ou groupe de fils | Debug CPU (ms) | Debug (%) | Release CPU (ms) | Release (%) |
| --- | ---: | ---: | ---: | ---: |
| Travailleur VDP1 | 18 203,1 | 56,33 | 16 125,0 | 46,11 |
| Travailleurs VDP2 | 7 734,4 | 23,94 | 7 421,9 | 21,22 |
| Travailleurs Titan | 3 109,4 | 9,62 | 5 750,0 | 16,44 |
| Fil d’émulation | 3 203,1 | 9,91 | 5 593,8 | 16,00 |

Dans la trace Debug, le fil d’émulation passe environ 13 192 ms murales (73,22 %) à attendre le VDP1. Le fil est bloqué à cet endroit. Le dessin reste sur le chemin critique. Sur les piles de dessin du travailleur VDP1 en Release, DrawLineCallback (7 600 ms échantillonnées) et IterateOverLine (7 156 ms) cumulent 90,50 % du temps mural attribué aux méthodes visibles sous DrawCommands. En Debug, GetPixel (4 218 ms), DrawLineCallback (3 924 ms), PutPixel (2 696 ms) et IterateOverLine (2 360 ms) dominent cette même répartition. Ces chiffres par méthode incluent les fonctions intégrées et les interruptions du fil.

Le changement d’attente a réduit le travail du fil principal, et le coût du dessin reste concentré dans la rasterisation. La référence Mednafen sépare dans vdp1_common.h le tracé texturé, le tracé sans texture et les modes de dessin par instanciation de DrawLine. Le pointeur de fonction par pixel porté de Yabause a déjà été mesuré. Le prochain essai doit isoler le coût de cette boucle spécialisée sans modifier les réglages d’attente. Aucun FPS sous instrumentation.

## Saturn — spécialisation de l’appel par pixel du VDP1, essai écarté

Une copie temporaire de SaturnVideoSoftware.cs instancie le tracé Yabause pour trois opérations statiques C# : dessin, mémorisation de bord et comptage. L’essai isole le remplacement du rappel indirect, sur le principe des instanciations de DrawLine dans vdp1_common.h de Mednafen. Les calculs et l’ordre des pixels restent identiques.

Sega Rally Championship Europe, BIOS EUR, interpréteur, cache SH‑2 désactivé, SCSP par défaut, 120 trames de chauffe et 120 secondes sans limiteur. Une paire par configuration, ordre inversé entre Debug et Release. Les quatre compilations passent sans avertissement ni erreur. Images, PCM cumulé et PC des deux SH‑2 concordent aux trames communes, jusqu’à 1 800 en Debug et 6 000 en Release.

| Configuration | Tracé VDP1 | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS de 60 à 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Pointeur managé | 29,68 | 30,11 | 26,40 | 20,21 | 16,49 | 60/60 |
| Debug | Opération statique | 30,05 | 31,20 | 26,90 | 20,05 | 16,08 | 60/60 |
| Release | Pointeur managé | 109,32 | 85,08 | 75,62 | 67,75 | 62,71 | 43/60 |
| Release | Opération statique | 108,08 | 86,40 | 77,67 | 70,35 | 64,29 | 41/60 |

Variante écartée. La moyenne Release de la dernière minute passe de 57,66 à 58,24 FPS et le Debug perd 0,41 FPS sur 120 secondes. Le fichier de production conserve le pointeur managé issu de Yabause. Mednafen spécialise aussi les modes texture, transparence, Gouraud et mélange avant la boucle. Cette partie n’était pas incluse dans l’essai. La restauration du banc a utilisé les paquets locaux avec l’audit NuGet désactivé.


## Saturn — tracé sans texture du VDP1

SaturnVideoSoftware.cs reprend la séparation Textured de Mednafen vdp1_common.h. Pour les commandes sans texture, le dessinateur prend CMDCOLR directement et conserve le masque de visibilité de Yabause. Le parcours texturé conserve ses lectures et ses codes de fin. Un polygone sans texture ni Gouraud évite aussi le comptage intermédiaire de pixels, qui ne servait alors à aucune interpolation. Les appels d’écriture conservent les règles existantes de clipping, transparence, mélange, et le pas de couleur du mode Gouraud.

Sega Rally Championship Europe, BIOS EUR, interpréteur, cache SH‑2 désactivé et SCSP par défaut. Une paire par configuration, ordre inversé entre configurations, 120 trames de chauffe puis 120 secondes sans limiteur. Les quatre builds passent à zéro erreur et zéro avertissement. Images, PCM cumulé et PC des deux SH‑2 concordent aux trames communes, jusqu’à 1 800 en Debug et 6 000 en Release.

| Configuration | Tracé VDP1 | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Témoin | 28,88 | 30,23 | 26,35 | 20,13 | 16,46 | 60/60 |
| Debug | Sans lecture de texture pour les aplats | 28,33 | 30,21 | 26,35 | 20,28 | 16,60 | 60/60 |
| Release | Témoin | 109,30 | 85,56 | 76,27 | 68,44 | 63,36 | 40/60 |
| Release | Sans lecture de texture pour les aplats | 110,55 | 86,87 | 77,57 | 70,46 | 65,34 | 36/60 |

Variante conservée. La moyenne Release de la dernière minute passe de 58,28 à 60,23 FPS. Il reste 36 secondes sous 60 FPS dans cette fenêtre. Une seule paire par configuration sur un seul jeu. La restauration du banc temporaire a utilisé les paquets locaux avec l’audit NuGet désactivé.


## Saturn — formats des lignes texturées du VDP1

SaturnVideoSoftware.cs sélectionne le lecteur par format avant le tracé de la ligne, comme le fait TexFetchTab de Mednafen. La version C# spécialise le rappel par format et prépare l’adresse de ligne et le retournement dans son contexte. Les décodages, codes de fin et écritures de pixels du parcours SegaEmu restent les mêmes. Cet essai se distingue du remplacement isolé du rappel par une opération statique, écarté plus haut.

Sega Rally Championship Europe, BIOS EUR, interpréteur, SCSP par défaut et cache SH‑2 désactivé. Une paire par configuration, ordre inversé entre configurations. 120 trames de chauffe puis 120 secondes sans limiteur. Les quatre builds passent sans erreur ni avertissement. Images, PCM cumulé et PC des SH‑2 concordent aux trames communes, jusqu’à 1 800 en Debug et 6 000 en Release.

| Configuration | Lecture de texture | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Sélection par pixel | 28,65 | 30,18 | 26,35 | 20,25 | 16,69 | 60/60 |
| Debug | Sélection par ligne | 28,59 | 30,17 | 26,33 | 20,58 | 16,99 | 60/60 |
| Release | Sélection par pixel | 109,23 | 86,15 | 76,92 | 71,46 | 70,17 | 9/60 |
| Release | Sélection par ligne | 114,74 | 94,65 | 85,76 | 80,47 | 80,01 | 3/60 |

Variante conservée. La moyenne Release de la dernière minute passe de 68,88 à 79,56 FPS. La cadence reste sous 60 FPS pendant trois secondes dans cette fenêtre. Une seule paire par configuration sur un seul jeu. Les formats peu utilisés par ce scénario demandent une validation séparée. La restauration du banc temporaire a utilisé les paquets locaux avec l’audit NuGet désactivé.


## Saturn — réutilisation du dernier texel VDP1, essai écarté

Le banc reprend la conservation du texel dans la boucle DrawLine de Mednafen vdp1_common.h. Le candidat conserve la dernière couleur décodée et son code de fin quand la coordonnée de texture ne change pas. La sélection se fait par ligne agrandie. Les deux versions utilisent déjà la sélection du format par ligne. La variante est restée dans une copie temporaire de SaturnVideoSoftware.cs.

Sega Rally Championship Europe, BIOS EUR, interpréteur, cache SH‑2 désactivé et SCSP par défaut. 120 trames de chauffe puis 120 secondes sans limiteur. Une paire par configuration, ordre inversé en Release. Les quatre builds passent sans erreur ni avertissement. Images, PCM cumulé et PC des SH‑2 concordent aux trames contrôlées, jusqu’à 1 800 en Debug et 6 000 en Release.

| Configuration | Lecture du texel | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS de 60 à 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Par pixel | 29,85 | 30,88 | 26,74 | 20,76 | 17,06 | 60/60 |
| Debug | Réutilisation | 28,34 | 30,01 | 26,28 | 21,10 | 17,88 | 60/60 |
| Release | Par pixel | 114,17 | 93,86 | 85,26 | 79,26 | 78,75 | 3/60 |
| Release | Réutilisation | 112,19 | 93,29 | 85,43 | 79,95 | 77,25 | 3/60 |

Variante écartée pour l’instant. Le Debug gagne 0,82 FPS, le Release perd 1,50 FPS et le nombre de secondes sous 60 FPS reste identique dans chaque paire. Une seule paire par configuration. Ne pas refaire cette réutilisation à l’identique sans nouvelle preuve. La restauration a utilisé les paquets locaux avec l’audit NuGet désactivé pour le banc temporaire.


## Saturn — codes de fin des textures VDP1 64 et 128 couleurs

SaturnVideoSoftware.cs détecte 0xFF sur l’octet brut avant de masquer la couleur, comme dans TexFetch de Mednafen vdp1.cpp. Le parcours précédent assimilait 0x3F au code de fin en 64 couleurs et cherchait un 0xFF inaccessible après le masque en 128 couleurs. Le contrôle de dessin passe dans les deux configurations sur deux lignes dans chaque mode. Il vérifie les couleurs valides 0x3F et 0x7F, le premier code ignoré, l’arrêt au second code, la reprise à la ligne suivante et l’option ECD qui désactive les codes.

Sega Rally Championship Europe, BIOS EUR, interpréteur, SCSP par défaut, cache SH‑2 désactivé. 120 trames de chauffe depuis le BIOS puis 120 secondes sans limiteur. Les images et PC des deux SH‑2 concordent entre Debug et Release aux trames 600, 1 200 et 1 800. Les deux builds passent à zéro erreur et zéro avertissement. Restauration du banc depuis les paquets locaux avec l’audit NuGet désactivé.

| Configuration | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | 26,34 | 29,80 | 26,28 | 20,72 | 17,10 | 60/60 |
| Release | 114,62 | 94,93 | 86,43 | 85,86 | 87,09 | 0/60 |

Release atteint 88,32 FPS de moyenne entre 60 et 120 secondes. La campagne mesure une fois le code actuel dans chaque configuration. Elle n’isole pas l’effet de la correction sur les FPS et ne suffit pas à généraliser la stabilité au-delà du scénario et de la fenêtre mesurée.


## Saturn — préparation du découpage des lignes VDP1

SaturnVideoSoftware.cs prépare les bornes de découpage et la parité d’entrelacement avant le tracé de la ligne, selon le principe de DrawLine dans vdp1_common.h de Mednafen. La version C# conserve les comparaisons et les modes de découpage de SegaEmu. Pour la fenêtre utilisateur intérieure, le rectangle préparé est l’intersection de cette fenêtre et de la fenêtre système. Pour la fenêtre extérieure, le contrôle du rectangle à exclure reste appliqué par pixel. Les formats 8 et 16 bits conservent leur ordre de filtrage et d’écriture.

Sega Rally Championship Europe, BIOS EUR, interpréteur et cache SH‑2 désactivé. 120 trames de chauffe puis 120 secondes sans limiteur. Une paire par configuration, ordre inversé entre configurations. Les quatre builds passent à zéro erreur et zéro avertissement. Images, PCM cumulé et PC des deux SH‑2 concordent aux trames communes, jusqu’à 1 800 en Debug et 6 000 en Release.

| Configuration | Découpage VDP1 | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Préparé par pixel | 27,86 | 29,89 | 26,08 | 20,43 | 16,93 | 60/60 |
| Debug | Préparé par ligne | 28,40 | 29,90 | 26,25 | 21,22 | 17,75 | 60/60 |
| Release | Préparé par pixel | 112,96 | 93,61 | 85,51 | 80,08 | 76,30 | 4/60 |
| Release | Préparé par ligne | 114,48 | 97,55 | 89,93 | 85,73 | 82,85 | 1/60 |

Variante conservée. La moyenne de la dernière minute en Release passe de 72,53 à 79,98 FPS. Une seconde reste sous 60 FPS dans cette fenêtre. Le test couvre un seul jeu et les empreintes ne valident pas toutes les combinaisons de rectangles et de modes d’entrelacement. La restauration du banc temporaire a utilisé les paquets locaux avec l’audit NuGet désactivé.


## Saturn — mélange VDP1 à 50 % en RGB555

SaturnVideoSoftware.cs reprend les masques de PlotPixel dans vdp1_common.h de Mednafen pour la luminosité du fond divisée par deux et le mélange à 50 %. Les deux opérandes sont ramenés à RGB555 avant la moyenne et l’appelant conserve la gestion du bit 15. Les 68 608 comparaisons du mélange avec le calcul indépendant par canal passent dans chaque configuration, dont toutes les valeurs de chaque canal isolé et des couleurs mixtes avec des bits au-delà de RGB555.

Sega Rally Championship Europe, BIOS EUR, interpréteur, SCSP par défaut, cache SH‑2 désactivé. 120 trames de chauffe depuis le BIOS puis 120 secondes sans limiteur. Les images et PC des deux SH‑2 concordent entre Debug et Release aux trames 600, 1 200 et 1 800. Les deux builds passent à zéro erreur et zéro avertissement. Restauration du banc depuis les paquets locaux avec l’audit NuGet désactivé.

| Configuration | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | 25,78 | 29,53 | 26,05 | 20,98 | 17,58 | 60/60 |
| Release | 114,35 | 97,01 | 89,09 | 84,75 | 81,92 | 1/60 |

La dernière minute Release est à 79,08 FPS de moyenne. Cette campagne mesure la version actuelle et ne permet pas d’isoler l’effet du changement sur la cadence.


## Saturn — comptage direct de la longueur des lignes VDP1

Dans le parcours greedy de iterateOverLine (Yabause, vidsoft.c), chaque pas sur l’axe principal ajoute un pixel, et chaque avance sur l’autre axe ajoute un pixel intermédiaire. Le point final ajoute un pixel. SaturnVideoSoftware.cs utilise donc |dx| + |dy| + 1 pour les appels de comptage qui ne dessinent rien et conserve la limite de 999 par axe. Les appels de dessin gardent leur ordre et peuvent encore s’arrêter sur les codes de fin. Ils disposent désormais tous d’un rappel. Le calcul reproduit le comptage de l’existant sur 8 273 lignes par configuration, dont les diagonales, une longueur nulle et les valeurs 999 et 1 000.

Sega Rally Championship Europe, BIOS EUR, interpréteur, cache SH‑2 désactivé et SCSP par défaut. Une paire par configuration, ordre inversé entre configurations. 120 trames de chauffe puis 120 secondes sans limiteur. Les quatre builds passent à zéro erreur et zéro avertissement. Images, PCM cumulé et PC des SH‑2 concordent aux trames communes, jusqu’à 1 800 en Debug et 6 000 en Release.

| Configuration | Longueur VDP1 | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Parcours de comptage | 29,53 | 30,46 | 26,53 | 21,40 | 17,92 | 60/60 |
| Debug | Calcul direct | 27,35 | 29,64 | 25,96 | 21,39 | 18,23 | 60/60 |
| Release | Parcours de comptage | 115,36 | 97,06 | 89,02 | 84,58 | 86,45 | 1/60 |
| Release | Calcul direct | 116,81 | 99,74 | 92,18 | 95,33 | 92,33 | 0/60 |

Variante conservée. En Release, la dernière minute passe de 88,32 à 89,32 FPS de moyenne. Une paire de mesures par configuration et un seul jeu. Restauration du banc depuis les paquets locaux avec l’audit NuGet désactivé.


## Saturn — amplitude des pas dans le tracé VDP1, essai écarté

Le candidat prépare les amplitudes sur les deux axes comme le fait SetupDrawLine de Mednafen. L’accumulateur de la boucle Yabause reste positif et les appels de dessin conservent leur ordre et leurs conditions. Les deux versions recopiées dans le banc donnent exactement les mêmes coordonnées, indices et tailles sur 1 352 scénarios par configuration, avec et sans pixel intermédiaire et avec différents points d’arrêt.

Sega Rally Championship Europe, BIOS EUR, interpréteur, SCSP par défaut et cache SH‑2 désactivé. Une paire par configuration, ordre inversé entre configurations. 120 trames de chauffe puis 120 secondes sans limiteur. Les quatre builds passent sans erreur ni avertissement. Images, PCM cumulé et PC des SH‑2 concordent aux points communs, jusqu’à 1 800 trames en Debug et 6 000 en Release.

| Configuration | Accumulateur VDP1 | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Signé | 29,75 | 30,68 | 26,51 | 21,65 | 18,35 | 60/60 |
| Debug | Positif | 29,30 | 30,55 | 26,65 | 21,73 | 18,37 | 60/60 |
| Release | Signé | 115,32 | 98,73 | 91,43 | 94,66 | 92,04 | 0/60 |
| Release | Positif | 115,29 | 99,88 | 92,66 | 88,35 | 86,10 | 0/60 |

Variante écartée. Le gain Debug est de 0,02 FPS et le passage Release perd 5,94 FPS. Une seule paire ne permet pas d’établir une régression reproductible, mais ne justifie pas le changement. Le moteur de production n’a pas été modifié. Restauration du banc temporaire avec les paquets locaux et l’audit NuGet désactivé.


## Saturn — Sega Rally Championship sur Desert avec accélération

Sega Rally Championship Europe, BIOS EUR, interpréteur SegaEmu, SCSP par défaut et cache SH‑2 désactivé. Les entrées du port 1 passent par ManagedCore. Impulsions de Start de 12 trames à partir des trames 610, 1 010, 1 310, 1 510, 1 910, 2 210 et 2 530. Le bouton B est maintenu à partir de la trame 1 540, hors impulsions de Start. Les captures montrent la voiture en mouvement à la trame 3 300 (environ 85 km/h), encore en course à la trame 6 000 et hors temps à la trame 6 500. Le chronomètre mural démarre à la trame 3 300. Aucun changement au moteur.

| Configuration | Fenêtre de course | Temps mural | FPS | Secondes complètes sous 60 FPS |
| --- | ---: | ---: | ---: | ---: |
| Debug | 2 700 trames | 147,696 s | 18,28 | 147/147 |
| Release | 2 700 trames | 23,998 s | 112,51 | 0/23 |

| Configuration | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | 17,52 | 17,28 | 17,58 | 17,25 | 18,17 | 60/60 |
| Release | 107,35 | 112,24 | 110,07 | 121,71 | 112,88 | 0/60 |

Les paliers sont cumulatifs. En Release, le passage de 120 secondes inclut la fin du temps réglementaire et les scènes suivantes. Seule la fenêtre de 2 700 trames compare exactement la même portion de course. Les images de départ sont identiques et les empreintes vidéo et PCM cumulé concordent aux trames mesurées 120, 600, 1 200 et 1 800. Les builds passent sans erreur ni avertissement. La restauration du banc temporaire utilisait les paquets locaux avec l’audit NuGet désactivé.


## Saturn — codes de fin des textures VDP1 en couleur directe

SaturnVideoSoftware.cs reprend TexFetch de Mednafen vdp1.cpp pour les formats 5 à 7. La texture RGB555 reconnaît comme marqueurs de fin les mots dont les deux bits de poids fort valent 01. Lorsque ECD désactive les codes de fin, ces mots restent dessinables. SPD contrôle le dessin des mots sous 0x4000. Les formats réservés 6 et 7 lisent le premier mot de VRAM et appliquent les mêmes règles de fin et de transparence. Des sprites de deux lignes vérifient ces cas dans les deux configurations.

Sega Rally Championship Europe, BIOS EUR, interpréteur, SCSP par défaut, cache SH‑2 désactivé. 120 trames de chauffe depuis le BIOS, puis 120 secondes sans limiteur. Les images et PC des deux SH‑2 concordent entre configurations aux trames 600, 1 200 et 1 800. Les deux builds passent sans erreur ni avertissement. Restauration du banc depuis les paquets locaux avec l’audit NuGet désactivé.

| Configuration | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | 25,55 | 28,93 | 25,76 | 21,06 | 17,95 | 60/60 |
| Release | 117,18 | 99,48 | 91,99 | 95,38 | 92,39 | 0/60 |

La dernière minute Release atteint 89,40 FPS de moyenne. Cette campagne mesure une fois le code corrigé dans chaque configuration. Elle n’isole pas le coût de la correction.


## Mega Drive + 32X — Doom via ManagedCore

Doom (JU), trois BIOS 32X configurés, absence d’entrée, 120 trames de chauffe et même point de départ. ManagedCore publie la vidéo et le PCM par ses contrats habituels. Les mesures précédentes sur la carte seules n’incluaient pas ce travail de publication. Le témoin utilise le module courant. Une copie de compilation de ManagedCore choisit UseNative68000Blocks pour exercer le dynarec hybride existant. Les deux SH‑2 utilisent les blocs compilés avec la voie x64 de registres. Aucun changement du moteur et aucune option publique ajoutée.

Un processus à la fois, sans limiteur, au moins 120 secondes et 1 200 trames. Les paliers indiquent la première trame terminée après leur échéance. Les quatre builds compilent sans erreur ni avertissement. Les images, le PCM cumulé et les compteurs ordinaux des SH‑2 concordent entre modes aux trames communes, jusqu’à 1 200 en Debug et 3 000 en Release.

| Configuration | Moteur | 10 s | 20 s | 30 s | 60 s | Fin | Durée finale | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Interpréteur | 20,82 | 20,37 | 20,21 | 19,44 | 18,58 | 120,045 s | 60/60 |
| Debug | Dynarec hybride | 14,40 | 14,20 | 14,16 | 10,88 | 9,45 | 126,990 s | 60/60 |
| Release | Interpréteur | 79,71 | 75,60 | 74,20 | 71,82 | 73,22 | 120,001 s | 0/60 |
| Release | Dynarec hybride | 63,71 | 25,76 | 21,74 | 19,02 | 26,72 | 120,007 s | 60/60 |

Le relevé hybride Release du palier 20 s survient après 26,318 s à la trame mesurée 678. La version Debug de la même voie dépasse 120 secondes pour terminer les 1 200 trames. Les nombres de blocs SH‑2 construits (maître/esclave) sont 15 091/213 en Debug et 18 447/222 en Release, à des durées et nombres de trames différents. Ils restent à zéro en interpréteur. Cette campagne confirme le coût de la voie hybride via le module complet. Elle ne mesure ni la voie SH‑2 x64 intégrale ni Mega-CD + 32X. Restauration du banc temporaire depuis les paquets locaux avec l’audit NuGet désactivé.


## Mega-CD + 32X — Night Trap via ManagedCore

Night Trap Europe, disque 1, BIOS Mega-CD régionaux et trois BIOS 32X. Les trois modes sont exercés via ManagedCore, une copie de compilation réservée au banc sélectionnant les options internes. La voie SH‑2 x64 sélectionne Full, Multi et les attentes natives. La sélection 68000/Z80 reste celle du dynarec hybride pour les deux modes compilés. Le banc publie vidéo et PCM via le module et confirme que le 32X est actif. Les deux builds compilent sans erreur ni avertissement. Aucun changement du moteur de production.

120 trames de chauffe sans entrée, puis au moins 120 secondes et 1 200 trames, sans limiteur. Start est maintenu pendant les cinq premières trames de chaque groupe de 120. Images, PCM cumulé et PC des SH‑2 identiques aux trames communes, jusqu’à 1 800 en Debug et au moins 3 600 en Release. Les captures séparées montrent le logo à la trame mesurée 600, le générique à 1 200 et l’écran du plan des caméras à 1 800. Le plan reste identique aux contrôles vidéo ultérieurs. Les FPS représentent la séquence mesurée, dont le plan fixe, et ne constituent pas un relevé de surveillance continue sur une caméra vidéo.

| Configuration | Moteur | 10 s | 20 s | 30 s | 60 s | 120 s | Trame la plus lente | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Interpréteur | 28,50 | 27,18 | 24,05 | 21,23 | 19,69 | 81,32 ms | 60/60 |
| Debug | Dynarec hybride | 29,60 | 26,94 | 20,78 | 18,81 | 17,57 | 2 185,90 ms | 60/60 |
| Debug | SH‑2 x64 | 29,15 | 27,08 | 22,56 | 18,55 | 16,00 | 303,37 ms | 60/60 |
| Release | Interpréteur | 99,00 | 87,07 | 83,94 | 81,92 | 80,82 | 49,57 ms | 0/60 |
| Release | Dynarec hybride | 60,38 | 57,72 | 55,81 | 53,74 | 52,86 | 2 536,56 ms | 60/60 |
| Release | SH‑2 x64 | 79,38 | 58,54 | 51,66 | 43,67 | 39,53 | 141,63 ms | 60/60 |

La trame mesurée 560 est la plus lente en hybride dans les deux configurations. Le x64 évite une pause de plusieurs secondes et régresse en débit sur ce scénario. En Release, les deux SH‑2 x64 cumulent 668 248 988 entrées de blocs, chauffe comprise. Ce compteur ne ventile pas le coût des sorties ou des accès au bus. Restauration du banc depuis les paquets locaux avec l’audit NuGet désactivé.


## Mega-CD + 32X — réutilisation de la fenêtre de lecture du SH‑2 x64

Night Trap Europe, disque 1, via ManagedCore. Même sélection Full+Multi et attentes natives, mêmes BIOS et mêmes entrées que la campagne précédente. Modification limitée à deux copies de compilation des boucles x64. Elles réutilisent la fenêtre d’instructions tant que ses bornes et la version du bus restent valides. La voie live a été compilée, et le scénario chronométré sélectionne la voie x64 existante sans contexte live.

| Configuration | Route | 10 s | 20 s | 30 s | 60 s | 120 s | Trame la plus lente |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Témoin x64 | 28,99 | 27,01 | 22,35 | 18,35 | 15,85 | 341,61 ms |
| Debug | Fenêtre de lecture réutilisée | 28,19 | 26,76 | 22,18 | 18,03 | 15,75 | 312,03 ms |
| Release | Témoin x64 | 80,95 | 57,64 | 50,81 | 43,12 | 39,29 | 111,84 ms |
| Release | Fenêtre de lecture réutilisée | 80,50 | 58,00 | 50,49 | 42,20 | 38,58 | 112,83 ms |

Images, PCM cumulé et compteurs ordinaux des SH‑2 concordent aux trames communes (600, 1200, 1800 en Debug et 600, 1200, 1800, 2400, 3000, 3600 en Release). Les deux SH‑2 utilisent bien le code x64. Les valeurs décrivent la séquence jusqu’au plan des caméras, qui reste fixe aux points contrôlés.

Variante écartée. La moyenne Release à 120 secondes baisse et chaque route reste sous 60 FPS pendant la dernière minute. Aucun changement de production. Le banc a restauré ses dépendances depuis les paquets locaux avec l’audit NuGet désactivé.


## Saturn — alignement des liens ASSIGN et CALL du VDP1

SaturnVdp1.cs applique le masque de la boucle de commandes de Mednafen vdp1.cpp. Les deux bits de poids faible de CMDLINK sont ignorés avant de former l’adresse. Les parcours de dessin et de mise à jour des registres suivent la même règle et conservent leur gestion du retour. Ce comportement vient de Mednafen. Le vdp1.c de Yabause multiplie le lien brut par huit.

Les 16 cas couvrant ASSIGN, CALL et RETURN associé, les deux parcours et les quatre combinaisons des bits ignorés passent dans chaque configuration. Les deux compilations du banc et du module Sega se terminent sans erreur ni avertissement. La restauration a utilisé les paquets locaux avec l’audit NuGet désactivé.

Sega Rally Championship Europe, BIOS EUR, 120 trames de chauffe depuis le BIOS, aucune entrée, puis 120 secondes sans limiteur via l’interpréteur SegaEmu. Vidéo et PCM cumulé concordent entre Debug et Release aux trames mesurées 600, 1 200 et 1 800.

| Configuration | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | 25,65 | 28,69 | 25,48 | 20,86 | 17,85 | 60/60 |
| Release | 115,79 | 98,97 | 91,58 | 94,54 | 91,77 | 0/60 |

La mesure de jeu porte sur la version corrigée et ne mesure pas isolément le coût du masque. Les cas ciblés exercent les commandes mal alignées.


## Saturn — table Gouraud des lignes et polylignes VDP1

SaturnVideoSoftware.cs charge les couleurs de la commande avant de les transmettre au premier segment, selon l’ordre de Mednafen vdp1_line.cpp. Le port de Yabause évaluait les paramètres de couleur avant de charger la table dans la routine appelée. Le premier segment pouvait donc hériter de la commande précédente. Avec Gouraud désactivé, le tracé passe directement au dessin. Avec Gouraud activé, la table est lue une fois pour la ligne ou la polyligne. Le tracé et l’interpolation existants sont conservés.

Six contrôles ciblent le premier segment d’une ligne et d’une polyligne. Ils vérifient le passage immédiat entre deux tables successives et le dessin à couleur unie. Les contrôles passent dans chaque configuration. Les deux builds du banc et du module Sega passent sans erreur ni avertissement. La restauration du banc a utilisé les paquets locaux avec l’audit NuGet désactivé.

Sega Rally Championship Europe, BIOS EUR, 120 trames de chauffe depuis le BIOS, aucune entrée, puis 120 secondes sans limiteur via l’interpréteur SegaEmu. Vidéo et PCM cumulé concordent entre Debug et Release aux trames mesurées 600, 1 200 et 1 800.

| Configuration | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | 26,54 | 29,80 | 26,12 | 21,49 | 18,18 | 60/60 |
| Release | 119,59 | 100,17 | 92,28 | 86,72 | 83,95 | 0/60 |

Le passage précédent, exécuté séparément, atteignait 17,85 FPS en Debug et 91,77 en Release. Ces deux campagnes n’isolent pas l’effet du changement sur le débit. La correction est conservée pour le résultat des cas Gouraud ciblés.


## Saturn — sélection Gouraud avant le tracé VDP1, essai écarté

Le candidat spécialise les rappels de dessin texturé et uni selon CMDPMOD.Gouraud, comme DrawLine dans Mednafen vdp1_common.h. La voie sans Gouraud évite les trois additions d’interpolation par pixel. Les deux copies conservent la correction de chargement de la table du passage précédent et la même géométrie. Les six contrôles ciblés passent dans les quatre builds. Compilation à zéro avertissement et zéro erreur.

Sega Rally Championship Europe, BIOS EUR, interpréteur, 120 trames de chauffe depuis le BIOS et 120 secondes sans limiteur. Processus séparés. Le témoin précède le candidat en Debug, ordre inversé en Release. Les empreintes vidéo et PCM cumulé concordent jusqu’à la trame mesurée 1 800 en Debug et 6 000 en Release.

| Configuration | Tracé VDP1 | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS de 60 à 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Témoin | 29,14 | 30,18 | 26,24 | 21,35 | 17,98 | 60/60 |
| Debug | Gouraud spécialisé | 28,77 | 30,18 | 26,21 | 21,14 | 17,73 | 60/60 |
| Release | Témoin | 116,26 | 99,53 | 91,98 | 95,08 | 92,32 | 0/60 |
| Release | Gouraud spécialisé | 118,79 | 101,17 | 94,33 | 89,42 | 87,64 | 0/60 |

Variante écartée sur cette comparaison. La moyenne à 120 secondes baisse de 17,98 à 17,73 en Debug et de 92,32 à 87,64 en Release. Le débit Release reste supérieur à 60 FPS chaque seconde de la dernière minute dans les deux passages. Une paire par configuration ne permet pas d’attribuer l’écart à un mécanisme du JIT. Le fichier de production conserve ses rappels. Restauration du banc depuis les paquets locaux avec l’audit NuGet désactivé.


## Saturn — mode MSB du framebuffer VDP1 en 8 bits

SaturnVideoSoftware.cs reprend la lecture du mot de framebuffer dans PlotPixel de Mednafen vdp1_common.h et l’applique au rangement linéaire de SegaEmu. En mode MSB, l’octet pair reçoit son bit 7 et l’octet impair est repris sans modification. En mode rotatif, la lecture vient de la moitié basse et l’écriture vise la ligne demandée. Le masquage du texel, le découpage, la parité et la transparence du parcours SegaEmu restent appliqués avant l’écriture. Le putpixel8 de Yabause ne traite pas le mode MSB.

Douze contrôles vérifient les deux octets en mode normal, en rotation sous la ligne 256 et dans la moitié haute, pour le mode MSB, la couleur ordinaire et les deux réglages de transparence. Les contrôles passent dans les deux configurations. Le premier build du banc a échoué sur deux variables foreach en lecture seule. Le banc corrigé et le module Sega compilent sans erreur ni avertissement. La restauration a utilisé les paquets locaux avec l’audit NuGet désactivé.

Sega Rally Championship Europe, BIOS EUR, interpréteur, 120 trames de chauffe puis 120 secondes sans limiteur, sans entrée. Les empreintes vidéo et PCM cumulé concordent entre Debug et Release aux trames 600, 1 200 et 1 800.

| Configuration | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | 28,64 | 30,11 | 26,25 | 21,48 | 18,24 | 60/60 |
| Release | 117,78 | 100,17 | 93,00 | 87,59 | 92,32 | 0/60 |

Ce relevé de la version corrigée ne mesure pas la fréquence du mode MSB 8 bits dans Sega Rally ni le coût de la correction.


## Saturn — pré-découpage des segments VDP1

SaturnVideoSoftware.cs reprend la réduction du segment à son premier point de SetupDrawLine dans Mednafen vdp1.cpp. La sélection choisit la fenêtre utilisateur en mode intérieur et la fenêtre système sinon. La commande peut désactiver cette réduction par CMDPMOD.PCD. Le port conserve la représentation des coordonnées et le parcours de pixels de SegaEmu. Neuf contrôles vérifient la longueur du tracé et ses pixels, avec les quatre bords, les deux modes de fenêtre utilisateur, la désactivation et le franchissement d’un bord.

Sega Rally Championship Europe, BIOS EUR, interpréteur, 120 trames de chauffe puis 120 secondes sans limiteur, sans entrée. Le témoin est compilé à partir du même worktree en retirant uniquement le pré-découpage. Les deux passages corrigés précèdent les deux témoins. Les empreintes vidéo et PCM cumulé concordent dans chaque paire, aux trames 600, 1 200 et 1 800 en Debug, puis aussi à 3 000, 4 800 et 6 000 en Release.

| Configuration | Tracé VDP1 | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Témoin | 28,96 | 30,12 | 26,20 | 21,16 | 17,91 | 60/60 |
| Debug | Avec pré-découpage | 29,45 | 30,64 | 26,59 | 24,23 | 22,83 | 60/60 |
| Release | Témoin | 115,90 | 98,54 | 90,84 | 86,58 | 84,48 | 0/60 |
| Release | Avec pré-découpage | 126,56 | 115,80 | 116,55 | 122,41 | 113,58 | 0/60 |

Correction conservée. Sur cette comparaison, la moyenne à 120 secondes augmente d’environ 27 % en Debug et 34 % en Release. Les quatre builds mesurés compilent sans avertissement ni erreur. Le premier build témoin a pris la copie de SaturnVideoSoftware.cs dans les sources du banc lui-même. Le projet temporaire corrigé inclut uniquement Program.cs. La restauration du banc a utilisé les paquets locaux avec l’audit NuGet désactivé.


## Saturn — arrêt des segments VDP1 après la fenêtre

SaturnVideoSoftware.cs reprend le drapeau drawn_ac du tracé Mednafen vdp1_common.h. Une ligne continue tant que ses pixels restent avant la fenêtre. Après la première entrée, la sortie de la fenêtre termine le segment. Les limites déjà préparées dans SegaEmu restent utilisées. Le mode utilisateur extérieur rejette encore les pixels dans le rectangle à exclure et reprend ensuite le dessin. Le tracé texturé lit les codes de fin avant d’entrer dans la fenêtre. Le pré-découpage des segments entièrement hors champ est actif dans les deux builds.

Quatorze contrôles ciblés passent dans chaque build. Ils vérifient les deux directions du tracé, les fenêtres système et utilisateur, le trou d’une fenêtre extérieure et la fin de texture hors champ. Les quatre builds compilent sans erreur ni avertissement. Les empreintes vidéo et PCM cumulé concordent aux trames mesurées 600, 1 200 et 1 800 en Debug et aussi à 3 000, 4 800 et 6 000 en Release.

Sega Rally Championship Europe, BIOS EUR, interpréteur, 120 trames de chauffe depuis le BIOS, sans entrée, puis 120 secondes sans limiteur. Un processus à la fois. Le témoin précède la variante en Debug, ordre inversé en Release.

| Configuration | Tracé VDP1 | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Témoin | 28,66 | 29,79 | 26,12 | 23,82 | 22,54 | 60/60 |
| Debug | Arrêt après la fenêtre | 29,67 | 30,41 | 26,45 | 24,57 | 23,73 | 60/60 |
| Release | Témoin | 126,48 | 115,18 | 111,81 | 109,76 | 107,53 | 0/60 |
| Release | Arrêt après la fenêtre | 129,41 | 120,22 | 123,03 | 127,23 | 121,53 | 0/60 |

Variante conservée. La moyenne à 120 secondes monte de 5,3 % en Debug et de 13,0 % en Release dans cette paire par configuration. La copie testée a été reportée dans le fichier de production après comparaison avec le témoin. Le banc a été restauré depuis les paquets locaux avec l’audit NuGet désactivé.


## Saturn — parcours direct des rectangles VDP1, essai écarté

La copie candidate calcule les extrémités de chaque ligne d’un rectangle à bords verticaux au moment de la dessiner, selon le principe d’EdgeStepper dans Mednafen vdp1_common.h et vdp1_sprite.cpp. Pour ces rectangles, le parcours ne remplit plus les quatre tableaux de coordonnées. Le candidat conserve le parcours des autres quadrilatères et les règles de texture, de découpage et de Gouraud du témoin. Les neuf empreintes de dessins ciblés concordent dans les deux configurations, y compris les bords inversés, les textures retournées et les limites de longueur.

Sega Rally Championship Europe, BIOS EUR, interpréteur, 120 trames de chauffe à partir du BIOS, puis 120 secondes sans limiteur et sans entrée. Le témoin précède la variante en Debug, ordre inversé en Release. Vidéo et PCM cumulé concordent aux trames mesurées 600, 1 200 et 1 800 en Debug, et aussi à 3 000, 4 800 et 6 000 en Release. Les quatre builds passent sans erreur ni avertissement.

| Configuration | Bords VDP1 | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Témoin | 27,29 | 29,14 | 25,63 | 23,83 | 23,34 | 60/60 |
| Debug | Bords verticaux directs | 28,79 | 29,99 | 26,15 | 24,24 | 23,56 | 60/60 |
| Release | Témoin | 130,77 | 121,02 | 120,36 | 118,26 | 116,07 | 0/60 |
| Release | Bords verticaux directs | 127,98 | 119,11 | 117,36 | 113,38 | 113,49 | 0/60 |

Variante écartée. Dans cette paire par configuration, le Debug gagne 0,22 FPS à 120 secondes et le Release perd 2,58 FPS. Ces passages ne permettent pas d’établir un gain général ni d’attribuer l’écart à une routine. Le fichier de production conserve les tableaux de coordonnées. La restauration du banc a utilisé les paquets locaux avec l’audit NuGet désactivé.


## Saturn — profil de Desert après les corrections de découpage du VDP1

Sega Rally Championship Europe, BIOS EUR et interpréteur SegaEmu. Les entrées rejouent le scénario sur Desert avec accélération. Les deux captures couvrent les mêmes trames 3 601 à 4 800. Les empreintes vidéo de départ et de fin concordent en Debug et Release. Les deux builds passent sans erreur ni avertissement. Restauration du banc avec les paquets locaux et l’audit NuGet désactivé.

Le tableau rapporte les différences des compteurs CPU Windows par fil, regroupées à partir des piles EventPipe. Durées murales avec la capture active de 54,17 s en Debug et 9,03 s en Release. Les parts sont rapportées au CPU total du processus. Le CPU des fils de rendu s’additionne même lorsqu’ils travaillent en parallèle.

| Fil ou groupe | Debug CPU | Release CPU |
| --- | ---: | ---: |
| VDP1 | 50 125,0 ms (27,5 %) | 6 484,4 ms (20,8 %) |
| VDP2 | 71 968,8 ms (39,4 %) | 11 250,0 ms (36,1 %) |
| Titan | 22 015,6 ms (12,1 %) | 6 687,5 ms (21,5 %) |
| Fil d’émulation | 37 609,4 ms (20,6 %) | 6 578,1 ms (21,1 %) |
| Autres | 796,9 ms (0,4 %) | 125,0 ms (0,4 %) |

Les durées ci-dessous sont échantillonnées sur les piles actives des travailleurs VDP2. Elles sont attribuées à la méthode gérée la plus profonde visible, qui peut inclure du code intégré par le JIT.

| Méthode VDP2 | Debug échantillonné (ms) | Release échantillonné (ms) |
| --- | ---: | ---: |
| Vdp2DrawScroll | 16 672,9 | 7 442,3 |
| TestBothWindow | 12 165,2 | 1 160,0 |
| TestWindow | 6 758,6 | 0,0 |
| VidsoftDrawSprite | 5 992,2 | 1 840,5 |
| Vdp2MapCalcXY | 5 480,1 | 233,7 |
| Vdp2FetchPixel | 5 162,9 | 44,7 |

Le VDP2 utilise le plus de CPU cumulé. Le VDP1 reste le travailleur individuel le plus coûteux en Debug. Sur les piles échantillonnées du fil d’émulation en Debug, le SH‑2 et son bus représentent 22 410,6 ms sur 54 179,1 ms, l’attente explicite VDP2 7 907,8 ms et l’attente explicite VDP1 7 276,5 ms. Les attentes intégrées peuvent remonter sous leurs appelants. Sur le fil VDP1, la durée échantillonnée reste concentrée dans DrawLineCallback, GetPixel et PutPixel en Debug, et dans IterateOverLine en Release.

Mednafen vdp2_render.cpp découpe la ligne aux bornes WinPieces, où les résultats des fenêtres rectangulaires changent. Cela cible les vérifications répétées de TestBothWindow et TestWindow. Le test de fenêtre sprite reste individuel là où il peut changer la décision. Les fenêtres de couleur, de rotation et de visibilité restent distinctes. Aucun changement de production ni relevé FPS de référence sous instrumentation.


## Saturn — fenêtres VDP2 par portions de ligne

SaturnVideoSoftware.cs applique le découpage WinPieces de Mednafen vdp2_render.cpp au parcours Vdp2DrawScroll. La visibilité et le calcul couleur sont évalués séparément aux bornes des fenêtres rectangulaires. Une portion invisible est sautée. Le masque sprite est relu par pixel uniquement si les décisions diffèrent lorsque le test sprite passe ou échoue. Les combinaisons et particularités des fenêtres SegaEmu sont conservées. Les parcours des sprites et de sélection des paramètres rotatifs conservent le test individuel.

Les 1 966 080 décisions synthétiques concordent dans chaque configuration avec le test par pixel. Les cas couvrent les 256 valeurs de contrôle, des bornes inversées ou dépassant la ligne et deux jeux de fenêtres sur chaque portion. Les quatre builds compilent sans erreur ni avertissement.

Sega Rally Championship Europe, BIOS EUR, interpréteur. Le banc reproduit les entrées sur Desert, puis chronomètre 120 secondes sans limiteur à partir de la trame 3 300. La mesure inclut les scènes atteintes après la course. Le témoin précède la variante en Debug, ordre inversé en Release. Images de départ, vidéo et PCM cumulé concordent aux points comparés jusqu’à la trame mesurée 1 800 en Debug et 6 000 en Release.

| Configuration | Fenêtres VDP2 | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Témoin | 23,44 | 24,33 | 24,70 | 24,72 | 24,95 | 60/60 |
| Debug | Fenêtres par portions | 22,91 | 24,17 | 24,70 | 24,66 | 25,18 | 60/60 |
| Release | Témoin | 110,67 | 111,72 | 111,14 | 123,52 | 122,99 | 0/60 |
| Release | Fenêtres par portions | 141,61 | 142,21 | 149,69 | 145,19 | 143,66 | 0/60 |

Variante conservée. Dans cette paire par configuration, le gain à 120 secondes est de 0,23 FPS en Debug et de 20,67 FPS en Release. Le faible écart Debug ne permet pas de conclure à un gain reproductible dans cette configuration. La copie mesurée a été reportée dans le fichier de production. Le banc a été restauré depuis les paquets locaux avec l’audit NuGet désactivé.


## Saturn — fenêtres du mélange des sprites VDP2 par portions

SaturnVideoSoftware.cs applique les portions d’ApplyWin de Mednafen vdp2_render.cpp au mélange des sprites. Les règles de visibilité et du calcul couleur partagent les bornes préparées. Les pixels qui doivent produire le masque sprite sont encore examinés avant le contrôle de visibilité tardif. Les contrôles qui dépendent du masque lisent sa valeur au même point qu’auparavant. Le découpage des plans de fond est actif dans les deux versions.

Les empreintes de l’image et du masque concordent entre témoin et candidat sur 60 cas synthétiques par configuration, avec framebuffers 8 et 16 bits, mode rotatif, haute résolution et différentes combinaisons de fenêtres. Les quatre builds mesurés passent sans erreur ni avertissement. La première préparation du banc a échoué à la substitution du texte puis à la sélection d’une vue mutable du framebuffer dans le contrôle synthétique. Ces erreurs ont été corrigées dans le banc.

Sega Rally Championship Europe, BIOS EUR, interpréteur. Mesure sur 120 secondes sans limiteur à partir de la trame 3 300 en course sur Desert. Le témoin précède la variante en Debug, ordre inversé en Release. Les images de départ, la vidéo et le PCM cumulé concordent jusqu’à la trame mesurée 3 000 en Debug et 6 000 en Release. Le passage de 120 secondes se poursuit dans les scènes atteintes après la course.

| Configuration | Fenêtres sprite | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS entre 60 et 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Témoin | 23,75 | 24,83 | 25,09 | 25,17 | 25,65 | 60/60 |
| Debug | Fenêtres sprite par portions | 23,89 | 25,23 | 25,43 | 25,58 | 26,06 | 60/60 |
| Release | Témoin | 130,28 | 133,68 | 136,62 | 141,56 | 142,78 | 0/60 |
| Release | Fenêtres sprite par portions | 143,67 | 144,19 | 152,02 | 149,69 | 148,91 | 0/60 |

Variante conservée. Le gain observé à 120 secondes est de 0,41 FPS en Debug et 6,13 FPS en Release, avec une paire par configuration. La copie mesurée a été reportée dans le fichier de production. Le banc a utilisé les paquets locaux et l’audit NuGet était désactivé pour sa restauration.


## Saturn — calcul couleur Gouraud 16 bits du VDP1

Dans SaturnVideoSoftware.cs, le traitement commun hérité du cas par défaut de vidsoft.c est remplacé pour les modes 5, 6 et 7 par les opérations de PlotPixel dans Mednafen src/ss/vdp1_common.h. Le mode 5 écrit zéro pour le pixel visible. Le mode 6 applique Gouraud puis divise la luminance de la source par deux en préservant son bit 15. Le mode 7 applique Gouraud et utilise le bit 15 du fond pour sélectionner le mélange à demi-transparence.

Le contrôle du premier pixel d'une ligne couvre ces trois modes avec les quatre combinaisons des bits 15 de la source et du fond. Les douze résultats attendus concordent dans chaque configuration. Builds Debug et Release sans erreur ni avertissement.

Sega Rally Championship Europe, BIOS EUR, interpréteur SegaEmu, sans limiteur ni entrée, depuis le démarrage avec 120 trames de chauffe hors chronomètre. FPS cumulés de l'unique passage par configuration après modification.

| Configuration | 10 s | 20 s | 30 s | 60 s | 120 s | Durée finale | Secondes sous 60 FPS entre 60 et 120 s |
| --- | ---: | ---: | ---: | ---: | ---: | --- | ---: |
| Debug | 29,05 | 30,60 | 26,49 | 26,24 | 26,70 | 120,039 s | 60/60 |
| Release | 134,40 | 137,00 | 138,57 | 136,04 | 138,85 | 120,002 s | 0/60 |

La vidéo et le PCM concordent entre builds aux trames mesurées 600, 1 200, 1 800 et 3 000. Ce passage vise la fidélité du rendu et ne mesure aucun gain avant/après. Le banc a été restauré depuis les paquets locaux avec l'audit NuGet désactivé.


## Saturn — table de saturation Gouraud du VDP1, essai écarté

Une copie de SaturnVideoSoftware.cs reprend les 64 entrées de gouraud_lut dans Mednafen src/ss/vdp1.cpp. Elle conserve le bornage arithmétique pour les sommes hors table afin de préserver les résultats de l'interpolateur existant. Les 3 072 combinaisons de composante et de coefficient contrôlées concordent. Les quatre compilations passent sans erreur ni avertissement.

Sega Rally Championship Europe, BIOS EUR, interpréteur, même scénario sur Desert après 3 300 trames hors chronomètre. Passages séparés de 120 secondes sans limiteur. Témoin puis table en Debug, ordre inverse en Release. Images de départ et empreintes vidéo et PCM concordantes aux trames contrôlées, jusqu'à 3 000 en Debug et 6 000 en Release.

| Configuration | Saturation Gouraud | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS de 60 à 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Témoin | 24,19 | 25,48 | 25,76 | 26,05 | 26,35 | 60/60 |
| Debug | Table | 23,69 | 24,95 | 25,22 | 25,27 | 25,76 | 60/60 |
| Release | Témoin | 136,99 | 137,48 | 141,03 | 141,30 | 141,64 | 0/60 |
| Release | Table | 140,60 | 140,83 | 147,78 | 146,86 | 143,80 | 0/60 |

Variante écartée pour cette implémentation. À 120 secondes elle perd 0,59 FPS en Debug et gagne 2,16 FPS en Release dans la paire mesurée. Le VDP1 de production garde le bornage arithmétique. Le banc a utilisé les paquets locaux avec l'audit NuGet désactivé.


## Saturn — damier entrelacé du VDP1

SaturnVideoSoftware.cs utilise la coordonnée verticale de dessin pour calculer la parité du damier, comme PlotPixel dans Mednafen src/ss/vdp1_common.h. La division par deux sert à l'adressage du framebuffer en double entrelacement. La logique précédente, issue de Yabause vidsoft.c, utilisait la ligne du framebuffer pour le damier.

Les 48 pixels contrôlés concordent dans chaque configuration. Le contrôle couvre deux lignes de framebuffer, les deux champs et les trois formats concernés, 16 bits, 8 bits et 8 bits avec rotation. Builds Debug et Release à zéro erreur et avertissement.

Sega Rally Championship Europe, BIOS EUR, interpréteur SegaEmu, sans limiteur ni entrée, depuis le démarrage avec 120 trames de chauffe hors chronomètre.

| Configuration | 10 s | 20 s | 30 s | 60 s | 120 s | Durée finale | Secondes sous 60 FPS entre 60 et 120 s |
| --- | ---: | ---: | ---: | ---: | ---: | --- | ---: |
| Debug | 27,99 | 29,86 | 25,79 | 25,00 | 25,44 | 120,026 s | 60/60 |
| Release | 122,95 | 126,49 | 130,65 | 132,33 | 136,80 | 120,002 s | 0/60 |

Aux trames 600, 1 200, 1 800 et 3 000, les empreintes vidéo et PCM concordent entre builds et avec le relevé précédent effectué selon le même scénario. Il s'agit d'une correction de rendu, avec un passage par configuration. Ce relevé ne quantifie pas l'effet de la correction sur la vitesse. Le banc a utilisé les paquets locaux avec l'audit NuGet désactivé.


## Saturn — transparence du mode MSB 16 bits du VDP1

SaturnVideoSoftware.cs applique le contrôle de visibilité avant d'écrire dans le framebuffer 16 bits, comme PlotPixel dans Mednafen src/ss/vdp1_common.h. Un texel visible en mode MSB conserve la couleur du framebuffer et force le bit 15. Cette règle s'applique aussi au texel zéro lorsque SPD autorise son dessin. Le code précédent reprenait le test currentPixel de Yabause vidsoft.c, qui laissait ce cas tomber dans le calcul couleur ordinaire.

Les 16 combinaisons contrôlées concordent en Debug et Release. Elles couvrent des sprites à palette, le texel zéro et un texel visible, deux palettes, SPD actif ou non, et le mode MSB ou le remplacement standard. Builds à zéro erreur et avertissement.

Sega Rally Championship Europe, BIOS EUR, interpréteur, sans limiteur ni entrée, depuis le démarrage avec 120 trames de chauffe hors chronomètre.

| Configuration | 10 s | 20 s | 30 s | 60 s | 120 s | Durée finale | Secondes sous 60 FPS entre 60 et 120 s |
| --- | ---: | ---: | ---: | ---: | ---: | --- | ---: |
| Debug | 33,69 | 31,96 | 27,46 | 26,78 | 26,69 | 120,018 s | 60/60 |
| Release | 134,54 | 138,46 | 139,55 | 135,29 | 137,33 | 120,003 s | 0/60 |

Les images et le PCM concordent entre builds aux trames 600, 1 200, 1 800 et 3 000 et avec le relevé précédent sur ce scénario. Ce passage ne compare pas la vitesse avant et après la correction. Le banc a utilisé les paquets locaux avec l'audit NuGet désactivé.


## Saturn — vue 16 bits du tampon de dessin VDP1

SaturnVideoSoftware.cs prépare une vue Span<ushort> avant le tracé de la ligne, sur le principe du tampon typé de PlotPixel dans Mednafen src/ss/vdp1_common.h. La vue est transmise au tracé plein et texturé, avec les mêmes bornes et la même règle d'adressage que la conversion par pixel. Le stockage reste commun avec le mode 8 bits. Les 16 combinaisons ciblées de transparence et de mode MSB passent en Debug et Release. Les quatre builds compilent sans avertissement ni erreur.

Sega Rally Championship Europe, BIOS EUR, interpréteur SegaEmu, Desert à la trame 3 300, sans limiteur. Une paire de 120 secondes par configuration, témoin puis variante en Debug, ordre inverse en Release. Même image de départ et empreintes vidéo et PCM cumulées identiques aux trames mesurées communes jusqu'à 3 000 en Debug et 6 000 en Release.

| Configuration | Vue du framebuffer | 10 s | 20 s | 30 s | 60 s | 120 s | Secondes sous 60 FPS de 60 à 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | Par pixel | 23,03 | 24,13 | 24,73 | 24,73 | 25,25 | 60/60 |
| Debug | Par ligne | 24,84 | 26,04 | 26,63 | 26,88 | 26,98 | 60/60 |
| Release | Par pixel | 133,10 | 134,89 | 136,49 | 138,02 | 138,83 | 0/60 |
| Release | Par ligne | 137,67 | 138,86 | 143,39 | 142,95 | 143,12 | 0/60 |

Variante conservée. Gains observés à 120 secondes de 1,73 FPS en Debug et 4,29 FPS en Release. Le fichier de production correspond à la variante mesurée. Restauration du banc depuis les paquets locaux avec l'audit NuGet désactivé.


## SG-1000 — campagne SegaEmu Debug sur cinq jeux

Mesure de la carte SegaEmu via BoardFactory, avec VDP et audio actifs et sans limiteur ni présentation hôte. Cartouches locales, nouvelle machine à chaque passage, 120 trames de chauffe et entrées déterministes identiques entre modes. Les paliers indiquent les FPS cumulés. CIL sélectionne UseDynamicCpuBlocks et x64 sélectionne la voie native Z80. Les compteurs confirment l’exécution de ces voies. Compilation Debug sans erreur ni avertissement.

| Machine | Jeu | Interpréteur 10 s | Interpréteur 30 s | Interpréteur 60 s | Interpréteur 120 s | CIL 10 s | CIL 30 s | CIL 60 s | CIL 120 s | x64 10 s | x64 30 s | x64 60 s | x64 120 s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| SG-1000 | Choplifter | 147,60 | 149,98 | 149,15 | 149,90 | 160,67 | 158,29 | 158,56 | 157,98 | 259,24 | 257,34 | 260,09 | 259,47 |
| SG-1000 | Zaxxon | 159,90 | 158,44 | 158,66 | 158,65 | 156,63 | 157,39 | 157,96 | 158,55 | 200,73 | 199,77 | 199,37 | 199,35 |
| SG-1000 | Star Jacker | 152,26 | 151,26 | 150,64 | 150,22 | 149,19 | 153,42 | 154,47 | 154,92 | 232,29 | 233,53 | 233,83 | 233,38 |
| SG-1000 | Girl’s Garden | 147,00 | 146,04 | 145,35 | 144,57 | 164,57 | 165,51 | 165,60 | 164,75 | 198,46 | 199,26 | 197,99 | 197,92 |
| SG-1000 | Champion Boxing | 149,55 | 149,11 | 149,06 | 149,43 | 156,52 | 156,81 | 156,90 | 157,59 | 249,21 | 250,14 | 249,02 | 249,97 |

Chaque passage retenu atteint au moins 60 FPS par seconde entre 60 et 120 s. Les empreintes vidéo et audio concordent entre les trois modes aux trames mesurées 120, 600 et 1 200. Les deux captures par mode sont produites hors chronométrage aux trames absolues 720 et 1 320. Les PNG concordent entre modes pour un même jeu et une même trame.

Gulkave est conservé dans les données brutes. Ses captures aux deux points retenus montrent l’écran des scores. Il a été remplacé dans le tableau par Champion Boxing, dont les captures montrent le combat. Aucune modification du code d’émulation.

Le banc, les relevés et les captures sont conservés sous `artifacts/.segaemu-sg1000-debug/`. Le projet de capture utilise l’encodeur PNG de WPF hors chronométrage. Restauration depuis les paquets locaux avec l’audit NuGet désactivé.

[Relevés bruts](../../../../artifacts/.segaemu-sg1000-debug/results.txt) · [Captures](../../../../artifacts/.segaemu-sg1000-debug/screenshots/)


## SG-1000 — campagne SegaEmu Release sur cinq jeux

Mêmes cartouches, entrées et point d’entrée que la série Debug. Carte SegaEmu via BoardFactory avec VDP et audio actifs, sans limiteur ni présentation hôte. Nouvelle machine par passage, 120 trames de chauffe, puis 120 secondes réelles. FPS cumulés. Build Release avec zéro erreur et zéro avertissement.

### Interpréteur

| Machine | Jeu | 10 s | 30 s | 60 s | 120 s |
| --- | --- | ---: | ---: | ---: | ---: |
| SG-1000 | Choplifter | 3436,14 | 3724,55 | 3760,85 | 3696,68 |
| SG-1000 | Zaxxon | 3172,60 | 3186,10 | 3202,61 | 3478,00 |
| SG-1000 | Star Jacker | 4110,39 | 4242,23 | 4272,97 | 4054,23 |
| SG-1000 | Girl’s Garden | 3916,63 | 3929,97 | 3970,40 | 4009,13 |
| SG-1000 | Champion Boxing | 4164,66 | 4233,85 | 4241,46 | 3828,07 |

### Dynarec CIL

| Machine | Jeu | 10 s | 30 s | 60 s | 120 s |
| --- | --- | ---: | ---: | ---: | ---: |
| SG-1000 | Choplifter | 1972,98 | 2698,15 | 2875,69 | 2905,65 |
| SG-1000 | Zaxxon | 274,10 | 1860,95 | 2406,95 | 2694,57 |
| SG-1000 | Star Jacker | 271,39 | 1602,03 | 2264,05 | 2599,36 |
| SG-1000 | Girl’s Garden | 50,80 | 763,46 | 683,81 | 715,62 |
| SG-1000 | Champion Boxing | 371,20 | 1459,76 | 2071,35 | 2424,70 |

### Dynarec x64

| Machine | Jeu | 10 s | 30 s | 60 s | 120 s |
| --- | --- | ---: | ---: | ---: | ---: |
| SG-1000 | Choplifter | 4124,65 | 4185,29 | 4218,99 | 4216,03 |
| SG-1000 | Zaxxon | 3882,75 | 3993,29 | 4036,97 | 4054,73 |
| SG-1000 | Star Jacker | 4158,04 | 4233,65 | 4225,44 | 4222,79 |
| SG-1000 | Girl’s Garden | 3336,50 | 3492,52 | 3553,61 | 3615,21 |
| SG-1000 | Champion Boxing | 3606,79 | 3750,59 | 3814,89 | 3818,37 |

Le x64 est en tête sur Choplifter, Zaxxon et Star Jacker. L’interpréteur est en tête sur Girl’s Garden et Champion Boxing. Le CIL reste derrière sur les cinq jeux à 120 secondes.

Girl’s Garden en CIL passe 18 secondes sous 60 FPS entre 60 et 120 s. Le second passage donne 63,09 FPS à 10 s, 785,03 FPS à 30 s, 721,70 FPS à 60 s, 720,00 FPS à 120 s et 20 secondes sous 60 FPS sur la dernière minute. Les autres passages restent à 60 FPS ou plus chaque seconde sur cette fenêtre. La récurrence est établie sur ce scénario, et la cause des baisses reste à identifier.

Les empreintes vidéo et audio concordent aux trois trames contrôlées. Les 30 PNG reproduits hors chronométrage concordent entre modes pour chaque jeu et chaque trame.

[Relevés](../../../../artifacts/.segaemu-sg1000-debug/results-release.txt) · [Répétition Girl’s Garden](../../../../artifacts/.segaemu-sg1000-debug/results-release-girls-garden-cil-repeat.txt) · [Captures](../../../../artifacts/.segaemu-sg1000-debug/screenshots-release/)


## SG-1000 — part des puces et des échanges de ports

Girl’s Garden, 120 trames de chauffe puis 4 800 trames identiques par passage. Dans une copie temporaire du module, chronomètres autour du Z80, du rendu et de la sélection de sprites, des ports SG-1000, des appels aux puces et de la sortie audio. Les mesures inclusives sont soustraites pour éviter de compter deux fois les appels imbriqués. Les quatre passages effectuent 936 417 accès ports et concordent en vidéo et audio aux quatre trames contrôlées. Builds Debug et Release à zéro erreur et avertissement.

| Poste | Debug interpréteur | Debug x64 | Release interpréteur | Release x64 |
| --- | ---: | ---: | ---: | ---: |
| Z80 hors gestionnaires de ports | 8 598,6 ms (26,89 %) | 1 264,7 ms (5,13 %) | 513,1 ms (32,68 %) | 633,8 ms (38,42 %) |
| VDP, dessin et traitement de ses ports | 22 942,2 ms (71,75 %) | 22 993,0 ms (93,28 %) | 927,0 ms (59,04 %) | 868,0 ms (52,61 %) |
| PSG et sortie audio | 224,8 ms (0,70 %) | 197,5 ms (0,80 %) | 40,4 ms (2,57 %) | 49,6 ms (3,01 %) |
| Routage des ports hors travail des puces | 49,1 ms (0,15 %) | 48,0 ms (0,19 %) | 37,5 ms (2,39 %) | 39,2 ms (2,37 %) |
| Lecture des entrées | 2,1 ms (0,01 %) | 1,8 ms (0,01 %) | 0,3 ms (0,02 %) | 0,5 ms (0,03 %) |
| Autres, dont séquenceur résiduel | 156,7 ms (0,49 %) | 144,9 ms (0,59 %) | 51,8 ms (3,30 %) | 58,8 ms (3,56 %) |

Les chiffres incluent le coût des chronomètres. Les lectures mémoire directes et les rappels d’accès qui ne passent pas par les gestionnaires de ports SG-1000 restent dans le poste Z80. Le profil EventPipe a servi à repérer le remappage des lignes vidéo et présente de longs intervalles sans attribution fiable côté x64. Le tableau utilise les compteurs directs. Aucune accusation de boucle inutile ni de coût global de tous les bus à partir de ces seuls résultats.

Le choix CIL est désactivé dans la construction de la machine SG-1000. Le banc réutilisable propose Interpreter et x64 et conserve les relevés historiques. Aucun changement de mode pour les autres machines et aucun thread de puce ajouté.


## SG-1000 — expansion des lignes de tuiles TMS par groupes de huit

Vdp.Background.Tms.cs développe les huit indices couleur d'une ligne de tuile en un mot, à partir d'une table de 256 masques. Le texte à six pixels conserve sa boucle. La variante intervient dans le dessin du fond, distinctement du remappage final déjà évalué. Les 65 536 couples motif/couleurs ont produit les mêmes huit indices que la boucle témoin. Builds Debug et Release sans erreur ni avertissement.

Girl’s Garden, interpréteur SegaEmu, mêmes entrées et 120 trames de chauffe. Une paire de passages de 120 secondes par configuration, témoin en premier en Debug et en dernier en Release. Images et audio concordent aux trames mesurées 120, 600 et 1 200. Les quatre passages tiennent au moins 60 FPS chaque seconde sur la dernière minute.

| Configuration | Variante | 10 s | 30 s | 60 s | 120 s |
| --- | --- | ---: | ---: | ---: | ---: |
| Debug | Témoin | 161,82 | 161,51 | 161,64 | 165,57 |
| Debug | Masques TMS | 187,91 | 187,86 | 182,43 | 177,10 |
| Release | Témoin | 3 481,15 | 3 411,18 | 3 438,89 | 3 380,85 |
| Release | Masques TMS | 4 036,67 | 3 950,77 | 3 924,06 | 3 910,00 |

Variante conservée. Le débit de la machine mesurée augmente de 7,0 % en Debug et de 15,7 % en Release à 120 secondes. Une seule paire par configuration. Le gain propre au rendu et celui du mode x64 restent à mesurer. Aucun changement du séquenceur ni nouveau thread.


## SC-3000 — campagne SegaEmu Debug et Release sur cinq jeux

Le banc crée Sc3000Machine via BoardFactory et contrôle son identifiant matériel 0x01. Les cinq cartouches SG compatibles sont les mêmes que dans la campagne SG-1000. Le rendu et l'audio sont actifs, avec 120 trames de chauffe et 120 secondes réelles par passage sans limiteur. FPS cumulés. Deux modes comparés, interpréteur et Z80 x64. Le chemin expérimental CIL n'a pas été mesuré et aucun code d'émulation n'a été modifié. Les builds Debug et Release compilent sans erreur ni avertissement.

### Debug

#### Interpréteur

| Machine | Jeu | 10 s | 30 s | 60 s | 120 s |
| --- | --- | ---: | ---: | ---: | ---: |
| SC-3000 | Choplifter | 179,38 | 183,53 | 185,90 | 193,25 |
| SC-3000 | Zaxxon | 188,25 | 189,12 | 190,25 | 191,23 |
| SC-3000 | Star Jacker | 172,62 | 166,20 | 164,22 | 164,56 |
| SC-3000 | Girl’s Garden | 179,63 | 178,60 | 175,64 | 174,04 |
| SC-3000 | Champion Boxing | 192,43 | 190,53 | 189,55 | 189,98 |

#### Dynarec x64

| Machine | Jeu | 10 s | 30 s | 60 s | 120 s |
| --- | --- | ---: | ---: | ---: | ---: |
| SC-3000 | Choplifter | 282,51 | 284,31 | 280,31 | 275,92 |
| SC-3000 | Zaxxon | 265,07 | 263,29 | 262,23 | 258,68 |
| SC-3000 | Star Jacker | 197,17 | 200,16 | 201,62 | 202,67 |
| SC-3000 | Girl’s Garden | 252,69 | 253,65 | 253,46 | 252,84 |
| SC-3000 | Champion Boxing | 215,33 | 212,68 | 212,57 | 212,87 |

### Release

#### Interpréteur

| Machine | Jeu | 10 s | 30 s | 60 s | 120 s |
| --- | --- | ---: | ---: | ---: | ---: |
| SC-3000 | Choplifter | 5142,97 | 5215,78 | 5244,34 | 5252,80 |
| SC-3000 | Zaxxon | 4657,68 | 4661,69 | 4690,73 | 4637,56 |
| SC-3000 | Star Jacker | 5021,02 | 5190,75 | 4992,16 | 4990,46 |
| SC-3000 | Girl’s Garden | 4808,29 | 4885,99 | 4863,60 | 4887,36 |
| SC-3000 | Champion Boxing | 4996,34 | 5205,31 | 5240,29 | 5244,01 |

#### Dynarec x64

| Machine | Jeu | 10 s | 30 s | 60 s | 120 s |
| --- | --- | ---: | ---: | ---: | ---: |
| SC-3000 | Choplifter | 5307,12 | 5328,42 | 5316,39 | 5360,95 |
| SC-3000 | Zaxxon | 4681,05 | 4775,30 | 4780,90 | 4756,85 |
| SC-3000 | Star Jacker | 4722,57 | 4780,30 | 4775,95 | 4778,86 |
| SC-3000 | Girl’s Garden | 4107,60 | 4371,18 | 4415,43 | 4458,88 |
| SC-3000 | Champion Boxing | 4414,08 | 4663,60 | 4773,93 | 4761,50 |

Les vingt passages restent à au moins 60 FPS chaque seconde sur la dernière minute. À 120 secondes, le x64 est devant sur les cinq jeux en Debug et sur Choplifter et Zaxxon en Release. L'interpréteur est devant en Release sur Star Jacker, Girl’s Garden et Champion Boxing. Le PCM et les images concordent aux trois trames contrôlées. Les 40 PNG reproduits hors chronométrage concordent entre modes et configurations pour chaque jeu et chaque trame.

Le contrôle de révision exécuté séparément a confirmé 0x01 en SG-1000 par défaut pour les cinq mêmes cartouches. Choplifter démarre aussi sous 0x02 et 0x03. Les mesures SC-3000 couvrent la lecture de cartouches compatibles et ne valident pas les périphériques spécifiques de l'ordinateur.

[Relevés Debug](../../../../artifacts/.segaemu-sc3000/results-Debug.txt) · [Relevés Release](../../../../artifacts/.segaemu-sc3000/results-Release.txt) · [Captures Debug](../../../../artifacts/.segaemu-sc3000/screenshots-Debug/) · [Captures Release](../../../../artifacts/.segaemu-sc3000/screenshots-Release/)
