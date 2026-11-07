# WD1770/1772 — clocks de rotation inactive, 7 novembre 2026

## Scénario de carte complète

Référence de production : `f85fbb5ee`. Carte BBC Micro Model A 32 Kio, MOS 1.20, BASIC II, SAA5050, DFS 2.26 variant 1 en banque 14, WD1770 à 8 MHz pour une référence BBC 2 MHz. SSD IJK Model A Invaders ouvert explicitement en lecture seule par le SDK ; aucun changement de configuration ni de média original. Debug sans optimisation Release, sans RML, avec RunFrame et drainage audio, sans compilation ni tests concurrents pendant les mesures.

Le 6502 exécute `10 FOR I=1 TO 100:OSCLI "LOAD INV/A 3000":NEXT`. La fenêtre principale couvre 640 images : 25 559 041 cycles CPU et 63 488 octets lus. La commande Escape est ensuite présentée pendant deux images, puis relâchée pendant deux images. La fenêtre suivante de 120 images comprend la fin du LOAD en cours, la prise en compte d'Escape puis la rotation résiduelle du lecteur : 4 792 321 cycles et 6656 octets lus. Son ancien libellé de diagnostic `post-load-idle` n'est donc pas une mesure de repos pur. Après cette fenêtre, le programme est interrompu à la ligne 10 ; le vrai `PRINT I` retourne 9, soit huit itérations terminées.

## Chemin identifié et modification retenue

Le profil échantillonné des piles à l'intérieur de RunFrame, incluant préparation et repos, donne 28,83 % inclusif dans Wd1770Drive.Tick, 24,32 % inclusif / 9,09 % propre dans TickSilicon et 10,97 % inclusif / 6,61 % propre dans Wd1770.Tick. EnsureTrack représente 3,60 % inclusif. Les inclusifs se recouvrent : ils ne doivent pas être additionnés. CPU_TIME et UNMANAGED_CODE_TIME sont des pseudo-frames du convertisseur, pas des méthodes qui coûtent 99 % du temps propre.

Un cache de projection de piste a été essayé puis rejeté : moyenne de référence 33,984420 ms/image contre 33,809687 pour le candidat, paires de sens opposé et seulement 0,51 % d'écart. Les sources de ce cache sont retirées ; aucun contrôle de position supplémentaire par clock n'est conservé. Les tests de données restent, sans imposer ce cache.

La modification retenue est `Wd1770Drive.TryAdvanceIdleRotation`. Elle concerne seulement le contrôleur **inactif dont le moteur est encore actif**. L'appel existant fournit les clocks WD correspondantes ; lorsque toutes les entrées restent stables sur l'intervalle, Chip.Advance conserve exactement le compteur du silicium et le raccordement avance son temps et sa rotation par la même quantité. Aucun changement dans Chips, les commandes, la fréquence, les délais ni le canal actif.

Le chemin unitaire reste utilisé si un front est possible : nouvelle piste à présenter, impulsion de raccord d'index, index montant/descendant, flux arrivant sur la tête ou fin de niveau RD, écriture, passage de révolution ou reste de conversion nanoseconde non nul. Il n'est ajouté ni délai d'un tick, ni file, ni agenda périodique. Les mutations de sortie et interruptions restent au même instant. Les sauvegardes/chargements d'état ne sont pas développés ; leur fichier est revenu à sa version initiale après retrait du premier candidat.

## Comparaisons alternées sans profileur

Même exécutable de diagnostic et seules DLL Acorn de référence/candidat alternées ; mêmes programmes, cycles, octets, PC et empreintes dans chaque fenêtre.

| Fenêtre, ms/image | Référence 1 | Candidat 1 | Référence 2 | Candidat 2 | Moyenne référence → candidat |
| --- | ---: | ---: | ---: | ---: | ---: |
| Repos avant lecture | 17,445463 | 17,112161 | 17,014658 | 18,347115 | 17,230061 → 17,729638 |
| LOAD continu, 640 images | 32,444250 | 31,960290 | 32,005184 | 30,694310 | 32,224717 → 31,327300 |
| Fin de LOAD et rotation résiduelle, 120 images | 32,295918 | 26,387901 | 29,683258 | 25,466168 | **30,989588 → 25,927034** |

Le gain retenu est établi sur la troisième fenêtre dans les deux paires : **16,34 % de temps économisé en moyenne**, capacité de calcul 32,27 → 38,57 images/s et 1,289 → 1,540 MHz, soit 64,43 → 77,02 % des 2 MHz. La cadence vidéo matérielle reste environ 50,1 Hz. Ce sont des capacités sans limite hors interface, pas les FPS présentés par RML.

Le LOAD continu gagne environ 2,78 % dans ces deux paires, mais reste autour de 31 ms/image ; ce bloc ne permet donc pas d'annoncer 19,97 ms/image Debug en lecture active. Le repos ne dispose pas d'un gain : ce chemin n'est pas celui regroupé et les durées fluctuent. Aucun résultat Release n'est substitué au Debug.

Empreintes communes aux paires :

- LOAD vidéo : `5843571514F855E9FBAA4AA734385C76C89AB823CD85E57F0283A7BFBAF1D2C0` ; PC 36452.
- Fin de LOAD / rotation vidéo : `79D9D7066F3E20EE7DFFC8D2AD9A96DC3A76041579A91EA028BE2F15A5BBA00E` ; PC 58594.
- RAM chargée, 9216 octets à partir de &3000 : `51138505BF4641C824AABE4E85CFBD7A30577A21D478E1937949B7941C3CE11A`.

## Vérifications et capture

Passage final : **38 tests Debug réussis en 9 s**, zéro échec ni ignoré. Les deux nouveaux cas IdleRotationTests comparent WD1770 puis WD1772 à un raccordement source 16 MHz, fournissant au plus une clock WD par appel : mêmes demi-clocks, octets, DRQ, horodatages NMI, neuf index jusqu'à l'arrêt moteur puis nouvelle lecture. Les tests sont autonomes, utilisent des médias en mémoire et détachent leurs rappels avant reset/libération. Les contrôles de changement de face/tête, remplacement, écriture/relecture et sources non divisibles restent verts. Les 183 tests de silicium du bloc précédent ne sont pas rejoués : Chips n'a pas changé.

Capture inspectée : `artifacts/TestsChips/Wd1770/BBC Micro Model A/20261107-043657-wd1770-load-clock-validation.bmp`, copiée dans `artifacts/TestsMachines/Acorn/BBC Micro Model A/`. C'est le vrai framebuffer du 6502 : programme de LOAD, Escape à la ligne 10 et PRINT I retournant 9. Aucun verdict ni compteur n'est dessiné dans l'image par le banc. Le diagnostic, traces, DLL de comparaison et TRX sont temporaires ; conserver la capture hors Git et les résultats ci-dessus dans la documentation.
