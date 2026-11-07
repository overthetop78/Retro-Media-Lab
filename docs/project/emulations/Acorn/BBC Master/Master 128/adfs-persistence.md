# Master 128 — Winchester/ADFS, 7 novembre 2026

## Configuration et parcours

Même carte et ROM externes que le contrôle DFS : Master 128 sans coprocesseur, MOS 3.50/BASIC IV, ROM ADFS intégrée au fichier combiné, WD1770 et Winchester. Banc Debug sans RML ni audio Windows, rendu et drainage PCM inclus. Aucun changement de code de production ni de timings.

Le SDK crée un DAT/DSC dédié, `hdd.acorn-winchester`, préparé en `acorn-adfs`, avec la géométrie documentée 306 cylindres, 2 têtes, 33 secteurs de 256 octets. Une disquette ADFS 160 Kio créée par le SDK est présentée au lecteur 0. Ces fichiers sont possédés uniquement par le banc et supprimés après détachement/fermeture dans finally ; aucune configuration ni média utilisateur n'est écrit.

Le CPU exécute `*ADFS`, `*MOUNT 0`, `*CAT`, puis `*SAVE HDDCHECK 3000 +100`. Le motif est `index XOR 5Ah` sur 256 octets. Après détachement/Flush/fermeture du HDD, le même fichier est rouvert en lecture seule et remonté. Le CPU exécute `*MOUNT 0` et `*LOAD HDDCHECK 3100`. Les 256 octets sont comparés individuellement : aucun écart. Huit blocs physiques écrits et quinze blocs lus cumulés, dont huit après réouverture. Ce passage rouvre le média dans la même machine ; il ne prétend pas tester un démarrage à froid depuis ce HDD.

Captures inspectées sous `artifacts/TestsMachines/Acorn/BBC Master 128/`, également sous `artifacts/TestsChips/Acb4000/BBC Master 128/` : `20261107-102407-master128-adfs-mounted.bmp` (catalogue du lecteur 0) et `20261107-102425-master128-adfs-persist-pass.bmp` (verdict invité). Le verdict est affiché seulement après réussite de la comparaison RAM dans le banc.

## Mesures du premier port

| Fenêtre Debug | Images | ms/image | Images/s | MHz | Vitesse réelle | Cycles | Blocs lus/écrits dans la fenêtre |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Après montage/catalogue | 180 | 26,764500 | 37,3629 | 1,492126 | 74,61 % | 7 188 480 | 7 / 0 |
| Après saisie SAVE | 240 | 20,587888 | 48,5722 | 1,939781 | 96,99 % | 9 584 637 | 0 / 6 |
| Après saisie LOAD, média rouvert | 240 | 19,062177 | 52,4599 | 2,095039 | 104,75 % | 9 584 641 | 0 / 0 |

Les commandes commencent pendant leur saisie ; ces fenêtres incluent leur fin et l'attente au prompt, pas seulement les transferts actifs. La dernière ne comprend aucune lecture physique, déjà achevée pendant la saisie. Elle ne valide donc pas le budget HDD actif. Cadence matérielle conservée à environ 50,08 Hz, CPU nominal 2 MHz ; capacité sans limite hôte, pas FPS de présentation RML. Pas de comparaison avant/après ni gain attribué à une modification.

## Attente initiale sans disquette : ne pas modifier le silicium

Le premier banc n'avait aucun média dans le lecteur de disquette. `*ADFS` attendait avant tout accès HDD, les commandes suivantes n'étaient pas exécutées et les compteurs HDD restaient nuls. Un relevé de 10 000 instructions a situé l'attente en ROM 9, principalement BC56/BC58/BC59/BC5C/BC5F/BC61, boucle sur A2 et sur le bit 10h de 0D5D. Aucun résultat SAVE/LOAD n'est revendiqué pour ces essais échoués.

Le [comportement WD déjà documenté](../../../../../chips/Controllers/Wd1770.md) conserve la recherche sans index, potentiellement indéfinie en l'absence de média ; le WD1770 n'a pas l'entrée Ready du WD1773. Avec une disquette ADFS présente, la même ROM termine cette étape puis accède correctement au HDD. Aucune temporisation artificielle, fausse impulsion index ou sortie Ready n'a été ajoutée. Captures des états d'attente conservées comme étapes : `20261107-102019-master128-adfs-mounted.bmp` et `20261107-102149-master128-adfs-mounted.bmp`. Leurs noms ne constituent pas une preuve de montage réussi ; aucun catalogue n'y est affiché.

Le budget HDD Debug actif reste ouvert comme dans les précédentes mesures BBC ; cette validation de persistance ne clôture pas l'ensemble du Master 128.

## Second port physique

Même parcours sur une nouvelle image dédiée attachée uniquement au port 1 : `*MOUNT 1`, catalogue `Drive:1`, SAVE, fermeture/réouverture en lecture seule, LOAD et comparaison des 256 octets réussis. Huit blocs écrits et quinze lus cumulés, comme au port 0. Les deux ports sont testés successivement, pas en accès simultané.

| Fenêtre Debug port 1 | Images | ms/image | Images/s | MHz | Vitesse réelle | Cycles | Blocs lus/écrits dans la fenêtre |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Montage/catalogue | 180 | 28,527359 | 35,0541 | 1,399919 | 70,00 % | 7 188 479 | 7 / 0 |
| Après SAVE | 240 | 22,560068 | 44,3261 | 1,770208 | 88,51 % | 9 584 645 | 0 / 6 |
| Après LOAD, média rouvert | 240 | 20,993935 | 47,6328 | 1,902264 | 95,11 % | 9 584 643 | 0 / 0 |

Captures `20261107-102517-master128-adfs-port1-mounted.bmp` et `20261107-102537-master128-adfs-port1-persist-pass.bmp`, dans les mêmes dossiers machine et puce. Le verdict `MASTER ADFS PORT1 PASS` est inspecté. Les mêmes limites de fenêtres et de réouverture dans une même machine s'appliquent ; les variations entre ports ne constituent pas une comparaison alternée de performances. Les deux exécutions réussies sortent avec code 0, détachent et libèrent les images, suppriment DAT/DSC/ADF dédiés. Diagnostic supprimé puis assembly temporaire reconstruit avec sa source antérieure.
