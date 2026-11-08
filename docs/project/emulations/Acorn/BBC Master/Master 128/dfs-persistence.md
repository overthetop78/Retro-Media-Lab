# Master 128 — persistance DFS, 7 novembre 2026

## Parcours effectivement exécuté

Carte Master 128, MOS 3.50/BASIC IV et Acorn 1770 DFS du fichier combiné externe existant, WD1770 et interface Winchester activés, sans coprocesseur ni HDD monté. Aucun changement de production. Banc Debug sans fenêtre RML ; rendu et drainage PCM à chaque image.

Le SDK a créé une SSD DFS 40 pistes dédiée (`acorn.dfs.ss`). Le vrai CPU a exécuté `*DISC`, puis `*SAVE CHECK 3000 4000` pour un motif déterministe de 4 Kio. Retour au prompt constaté ; compteur de 4 608 octets physiques écrits, comprenant les données et le catalogue. Après détachement, Flush, fermeture et réouverture en lecture seule, la RAM 3000–3FFF a été effacée puis `*LOAD CHECK 3000` exécuté. Les 4 096 octets ont été comparés individuellement avec le motif initial : aucun écart. Lecture physique cumulée : 5 120 octets, dont 512 avant la réouverture. L'image dédiée a été supprimée dans finally après détachement et fermeture ; aucun média utilisateur n'a été modifié.

Le verdict `MASTER DFS PERSIST PASS` a ensuite été affiché par le BASIC. Capture réelle du framebuffer inspectée : `artifacts/TestsMachines/Acorn/BBC Master 128/20261107-101543-master128-dfs-persist-pass.bmp`, également sous `artifacts/TestsChips/Wd1770/BBC Master 128/`. Il s'agit d'une preuve hors interface, pas d'une capture RML.

## Mesures et limites

Chaque fenêtre comprend 400 images après saisie de la commande, soit environ 7,99 secondes matérielles ; le transfert peut commencer pendant la saisie. Ces fenêtres comprennent également l'attente après transfert et ne mesurent pas seulement les accès disque. Cadence matérielle inchangée : environ 50,08 Hz, CPU nominal 2 MHz. Pas de compilation ni de test concurrent.

| Fenêtre Debug | ms/image | Images/s | MHz | Vitesse réelle | Cycles |
| --- | ---: | ---: | ---: | ---: | ---: |
| Après SAVE | 24,721672 | 40,4503 | 1,615425 | 80,77 % | 15 974 400 |
| Après LOAD du média rouvert | 23,535461 | 42,4891 | 1,696844 | 84,84 % | 15 974 401 |

Les 19,97 ms/image ne sont pas atteintes. Ce passage valide l'écriture/relecture persistante par le système invité ; il ne clôture ni le budget WD actif déjà signalé, ni les HDD, ni les autres variantes Master.

Après comparaison RAM réussie et enregistrement de l'image, le banc temporaire a tenté de copier le BMP avant fermeture de son BinaryWriter : IOException de partage, sans exception WPF ni défaut du contrôleur. Le finally a fermé le média et supprimé la SSD. La capture valide a été recopiée après fermeture du processus puis inspectée. La mesure finale prévue après capture n'a pas été exécutée et n'est pas revendiquée. Source de diagnostic supprimée après consignation, projet temporaire remis à sa source antérieure.

## Revue du 9 novembre 2026 — raccordement WD sous reset

La carte commune BBC/Master ne coupait plus seulement le séquenceur par /MR : `Disks.Tick` supprimait aussi la fourniture des clocks au raccordement. Cette condition est retirée. Le chemin existant moteur arrêté avance ses demi-clocks par addition arithmétique ; les optimisations de lecture et rotation du WD restent conservées. Dix cycles hôtes sous reset donnent 80 demi-clocks WD, sans moteur/IRQ/DRQ ; la libération conserve la reprise. Fichiers : `Disks.cs`, cas autonome dans `AcornEmuMasterTests.cs` et fiche `docs/chips/Controllers/Wd1770.md`. Groupe Master après cette correction : 32 réussites Debug, 878 ms. Cette revue ne répète pas la persistance DFS ni ses mesures historiques.
