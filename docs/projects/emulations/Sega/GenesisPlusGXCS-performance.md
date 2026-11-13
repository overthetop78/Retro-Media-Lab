# GenesisPlusGXCS — premier contrôle SG-1000 en Debug

Contrôle anticipé demandé par l'utilisateur après la suppression des dépendances vers les autres adaptateurs. Le module Sega compile ; l'intégration et la validation du portage intégral restent en cours. Ces résultats concernent le cœur SG-1000 directement, sans fenêtre et sans limitation à la cadence réelle.

Six ROM originales de `G:/Retro/Cartouches/Sega/SG-1000/`, de 8 à 48 Kio, ont été ouvertes en lecture seule. Une seule exécution du test a effectué 180 trames par ROM, soit 1 080 trames au total. Les boucles mesurées prennent 8,510 s, soit **126,91 FPS et 211,79 % de la cadence nominale**. La cadence nominale est de 59,922743 FPS et l'horloge Z80 nominale de 3 579 545 Hz.

| ROM | Kio | Durée réelle (s) | FPS | Vitesse nominale | Allocations (Mio) | Observation finale |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Champion Tennis (Japan) | 8 | 1,504 | 119,70 | 199,8 % | 100,44 | Écran de titre lisible ; pas de son relevé pendant ce scénario. |
| 007 James Bond v2.7, Othello Multivision | 16 | 1,382 | 130,26 | 217,4 % | 131,16 | Texte et scène visibles ; quatre trames avec son non nul. |
| Congo Bongo (Japan) | 24 | 1,416 | 127,08 | 212,1 % | 117,19 | Scène de jeu visible ; 102 trames avec son non nul. |
| Bank Panic (Japan) | 32 | 1,493 | 120,53 | 201,1 % | 147,20 | Écran de titre lisible ; 147 trames avec son non nul. |
| 007 James Bond v2.6, Othello Multivision | 40 | 1,380 | 130,46 | 217,7 % | 130,89 | Texte et scène visibles ; quatre trames avec son non nul. |
| Bomberman Special (Taiwan), DahJee | 48 | 1,335 | 134,86 | 225,1 % | 101,59 | **Image grise**, sans exception et sans son relevé. Anomalie fonctionnelle à examiner. |

Les six chargements et les six boucles de 180 trames terminent sans exception. Les images échantillonnées présentent deux à quatre empreintes distinctes selon la ROM. Cela n'établit ni la compatibilité complète des jeux ni la fidélité matérielle.

Les données brutes sont conservées dans `artifacts/GenesisPlusGXCS/sg1000/results.json` et les captures dans `artifacts/GenesisPlusGXCS/sg1000/frames.png`. L'ordre des captures, de gauche à droite puis de haut en bas, est : James Bond v2.6, James Bond v2.7, Bank Panic, Bomberman Special, Champion Tennis, Congo Bongo. La sortie xUnit est conservée dans `artifacts/GenesisPlusGXCS/test-sg1000.log`.

## Interprétation des mesures

Le scénario produit les images et l'audio, échantillonne les empreintes vidéo toutes les 30 trames et envoie une impulsion Start aux trames 60–62, puis le bouton 1 aux trames 120–139. Sur SG-1000, Start correspond à Pause/NMI ; les résultats ne représentent donc pas une partie entière jouée en continu. Les durées comprennent la compilation JIT des premiers appels et l'observation des sorties. Les captures sont enregistrées après la boucle chronométrée.

Le compteur de temps CPU émulé vaut 10 752 480 cycles par ROM : il est calculé depuis les cycles maître des trames, divisés par 15. La fréquence effective dérivée varie de 7,15 à 8,06 MHz. Il s'agit d'un débit de temps matériel simulé, pas d'un comptage indépendant des instructions exécutées. Le temps CPU hôte mesuré est celui du processus xUnit, incluant ses autres threads et le JIT ; il ne constitue pas un profil du seul thread Z80.

Les allocations atteignent **763 853 112 octets au total**, soit environ **707 ko par trame**. Ce volume est élevé et doit être distingué de la mémoire résidente. Les relevés `GC.GetTotalMemory` sont ceux du processus partagé entre les six essais ; ils n'isolent pas chaque cœur et ne prouvent pas une fuite. Aucun profil de temps propre/inclusif CPU, VDP, PSG, bus ou mémoire n'a encore été instrumenté : aucune attribution précise du coût ou des allocations n'est établie par ce contrôle.

## État restant

L'intégration de présentation, les ressources de traduction propres au portage et les options/fonctions de frontend restantes ne sont pas entièrement validées. Aucun test des autres machines, périphériques ou formats de disque n'a été exécuté. L'image grise de Bomberman Special est conservée comme anomalie, sans correction ni deuxième exécution dans ce premier contrôle. Le portage intégral ne doit pas être présenté comme terminé ou validé sur la seule base de ces six boucles SG-1000.
