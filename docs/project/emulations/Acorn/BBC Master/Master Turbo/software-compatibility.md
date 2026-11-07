# Master Turbo — compatibilité logicielle

## AMX/Turbo : montage du banc corrigé le 7 novembre 2026

**Conclusion après comparaison des RAM : erreur du banc, pas défaut démontré d'une puce.** Remplacer VIEW en banque 14 retirait les données de relocation du BASIC 4r32 de MOS 3.50. BASIC était bien transféré à B800, mais gardait ses adresses absolues d'origine au lieu de recevoir le décalage de 3800. L'observation des services ROM confirme aussi la réservation normale d'une page par AMX (service 2 : Y=14→15) ; cela n'était pas la cause du blocage.

Le montage corrigé conserve toutes les banques du bundle MOS, notamment VIEW 14, et ajoute AMX 2.1 variant 4 en banque d'extension 8, réseau absent. BASIC démarre ; OSWORD 64 donne 640,512 puis 688,544 après déplacement +24/−16. ADVAL donne 0/65520/65520/0, l'allocation parasite de 40 000 octets retrouve 123. Captures natives inspectées `20261107-160207-amx-tube-coordinates.png` et `20261107-160218-analog-tube-values.png` sous TestsMachines/Acorn et TestsChips/TubeUla/BBC Master Turbo. Aucun changement de puce, de fréquence, de ROM ni de configuration utilisateur. Mesure globale Debug sans limite, 60 images avec rendu et drainage PCM : 28,047818 ms/image, 35,6534 images/s, 1,42385 MHz hôte, 71,19 % de 2 MHz. Le fonctionnement des entrées est vérifié, mais le budget de cadence Turbo reste distinct et ouvert.

Référence primaire complémentaire : [Tom Seddon, désassemblage Master MOS, messages des 8 et 25 janvier 2024](https://stardot.org.uk/forums/viewtopic.php?start=30&t=27904) : banque 14, bitmap EDIT BBC4–BD80 et bitmap BASIC BD9F–BFFF. Source de relocation archivée hors Git : `artifacts/.tmp-archives/machines/Acorn/BBC Master Turbo/mos-470dded5.s65`, révision `470dded5a9d1b17308c2537620fdd2733b49ab53`, 426 624 octets, dépôt `tom-seddon/acorn_mos_disassembly`. Conserver cette dépendance du bundle ; ne pas appliquer aveuglément au Master la recommandation de banque haute du guide BBC AMX.

Les observations suivantes retracent les essais de montage incorrects ; elles ne constituent plus un défaut ouvert de la connexion AMX.

Assemblage Debug : MOS 3.50 complet, BASIC banque 12, client externe `Acorn Master Turbo 65C102.rom`, WD1770 sans média. Sans remplacement d'une ROM système par AMX, BASIC démarre et `PRINT 12+34` donne 46 ; capture `artifacts/TestsMachines/Acorn/BBC Master Turbo/20261107-154909-baseline-basic.png`, également sous TestsChips/TubeUla.

Avec `AMX Mouse Support v2.1 [variant 4].rom` en banque 14, le client reste à l'invite `*` au lieu de BASIC. Les commandes BASIC du banc donnent Bad name ou n'aboutissent pas : elles ne valident ni coordonnées, ni ADC, ni performance. Même résultat avec `AMX Mouse Support v3.31 [variant 2].rom` en banque 14. Cette dernière en banque 15 laisse l'écran observé vide. Les copies ROM restent inchangées ; ne pas attribuer cet écran à une panne vidéo sans preuve.

Contrôle conservant AMX 2.1/14 mais désactivant uniquement `userPortPointer` : même invite `*`, pas de résultat 46. Le défaut se reproduit donc sans le nouvel adaptateur. Cela ne prouve pas une incompatibilité normale avec le matériel réel : cause encore à déterminer avant de valider le pointage Turbo. Le guide AMX, page PDF 11, annonce la compatibilité avec le second processeur 6502 ; il ne confirme pas chaque révision ROM avec MOS 3.50/65C102. Ne pas modifier une puce ou rediriger les transferts pour contourner cette anomalie sans établir le défaut.

Captures des scénarios échoués, sous TestsMachines et TestsChips/TubeUla/BBC Master Turbo : `20261107-154759-amx-tube-coordinates.png` (2.1/14), `20261107-155015-amx-tube-coordinates.png` (3.31/14), `20261107-155124-amx-tube-coordinates.png` (3.31/15). Leurs noms ne sont pas des verdicts réussis. Le premier banc réutilisait un encodeur PNG WPF et échouait à sa deuxième sauvegarde ; corrigé en créant un encodeur par fichier, sans modification de l'émulation.

Sans AMX, connexion de pointage activée mais immobile : BASIC du coprocesseur alloue 40 000 octets, écrit/lit son dernier octet et affiche 123. ADVAL(1…4) affiche 0/65520/65520/0 pour les deux contrôleurs hôtes aux extrémités opposées, maintenus pendant la frappe. Le programme invité utilise les appels MOS/Tube ; le banc ne lit pas la RAM hôte à la place de la RAM parasite. Capture native inspectée `20261107-155540-baseline-tube-analog.png` sous TestsMachines/Acorn et TestsChips/TubeUla/BBC Master Turbo. Ce passage ne mesure pas de nouveau gain de cadence et ne valide pas le pointage AMX.

## Espaces mémoire et écran : référence constructeur

Acorn, *65C102 co-processor user guide*, partie 0443,030, issue 2, avril 1986, pages imprimées 9–11 (pages PDF 10–12) :

- Le coprocesseur possède sa RAM, numérotée de 0000 à FFFF, distincte de celle du processeur d'entrée/sortie.
- Pour les systèmes de fichiers, les adresses sans préfixe FFFF désignent le coprocesseur ; le préfixe FFFF désigne la mémoire hôte.
- L'écran reste dans la mémoire hôte. Le guide explique explicitement que certains programmes accédant directement à l'écran ne fonctionnent donc pas avec le coprocesseur.
- BASIC et ses opérateurs d'indirection ne donnent pas un accès direct à la RAM hôte par simple préfixe FFFF ; les appels OSWORD appropriés assurent les accès distants.

Source : https://chrisacorns.computinghistory.org.uk/docs/Acorn/Manuals/Acorn_65C102CoProUG.pdf . Archive : `artifacts/.tmp-archives/machines/Acorn/BBC Master Turbo/Acorn_65C102CoProUG.pdf` (1 645 906 octets).

## SimCity : version effectivement testée le 7 novembre 2026

Média : `F:\Retro\Image Disquette\Acorn Electron - Games - [SSD] (TOSEC-v2013-10-23)\Simcity (1990)(Acornsoft - Superior Software)\Simcity (1990)(Acornsoft - Superior Software).ssd`.

SHA256 avant/après inspection : `35EEC4AD13894258C9899643F2414728D2B41071CC9124DC5DAA9F903CE96258`, inchangé. L'inspection ouvre l'image en lecture seule avec MediaEngine et réutilise le lecteur DFS de MediaFileSystems dans un banc indépendant ; aucun module ne reçoit de nouvelle référence. L'image tronquée contient 246 secteurs présents ; son catalogue annonce 800 secteurs. La projection de catalogue utilise donc 80 pistes nominales, sans inventer les secteurs absents ni modifier le fichier. Le premier essai de projection 40 pistes avait été rejeté par le lecteur ; ce rejet du banc ne prouve pas un défaut du jeu ou du contrôleur.

L'entrée BASIC `SIMCITY` contient l'identité `SIM CITY/E 1.00`, puis charge `SIM_CIT`. Le nom du répertoire et cette identité distinguent cette version ; ils ne permettent pas de déclarer le Master incompatible, puisque l'utilisateur confirme qu'elle fonctionne sans coprocesseur.

Dans `SIM_CIT` :

- ligne 50 : `MODE 5` et commandes VDU établissant notamment un fond jaune ;
- ligne 190 : `*L.SIMSCR1 5800` ;
- `SIMSCR1` contient 10 240 octets, avec adresse de chargement DFS `005800` ;
- le chargeur inclut aussi de l'assembleur utilisant directement les vecteurs bas et les adresses FE00/FE08/FE09 ; la présence de ces routines n'établit pas à elle seule quelles branches sont exécutées dans chaque modèle.

Le chargement explicite à 5800 n'indique pas la RAM hôte FFFF5800. Avec le Tube actif, il vise la RAM parasite, tandis que le VDU a configuré l'écran hôte. L'écran attendu n'y est donc pas chargé par cette commande. Cette incompatibilité de chargement avec les espaces mémoire séparés explique le fond jaune observé sans nécessiter une modification des puces vidéo. Ne pas contourner ce comportement en fusionnant les RAM ou en redirigeant artificiellement les chargements du coprocesseur vers l'hôte.

Preuve utilisateur : même jeu incorrect avec le coprocesseur, correct sans lui. Capture directement observée du défaut : `artifacts/TestsMachines/Acorn/BBC Master Turbo/20261107-rml-simcity-defect.png` (fond jaune, petite bande colorée à gauche, 640 × 256, 32,2 FPS, 1,29 MHz hôte, 64 %). Le fonctionnement sans coprocesseur est confirmé par l'utilisateur ; aucune capture de cette seconde étape n'a été obtenue avant qu'il ferme RML. Aucun clic ni fermeture envoyé par le contrôle dans cette session empruntée.

Recherche Internet : aucune déclaration explicite de compatibilité SimCity/65C102 Turbo trouvée. La fiche https://www.bbcmicro.co.uk/game.php?id=934 indique le BBC B compatible et le Master non testé ; ce n'est pas une preuve concernant le Tube. Le guide Acorn et le chargeur du média sont les preuves techniques utilisées ici. Conclusion limitée à cette version/ce chargement ; ne pas généraliser à toutes les éditions de SimCity ni déclarer toutes les fonctions du Turbo validées par cet essai.

Conclusion acceptée par l'utilisateur : « oui c'est juste incompatible avec le 128 turbo, rien d'anormal ». Le diagnostic et son projet temporaire sont supprimés, leurs seuls caches nommés sont retirés et le banc Native préexistant est reconstruit en Debug avec son Program.cs, code 0. Aucun changement de production lié à SimCity.
