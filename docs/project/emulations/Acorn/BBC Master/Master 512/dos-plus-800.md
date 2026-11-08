# Master 512 — format physique DOS Plus 800 Kio

## Écart établi le 8 novembre 2026

L'image GEM2.800 de 8BS est identique à l'image GEM Applications du catalogue MAME : SHA-1 `0fd737ab5c8499727e6ec3711e59566e702244e7`. Elle n'est ni un disque IBM 800 Kio à secteurs de 512 octets, ni un ADFS D à identifiants commençant à zéro.

| Propriété | DOS Plus 800 |
| --- | --- |
| Capacité | 819200 octets |
| Cylindres / faces | 80 / 2 |
| Secteurs par piste | 5 |
| Taille / code N | 1024 octets / 3 |
| Identifiants R | 1 à 5 |
| Ordre des données brutes | cylindre, face, secteur |
| Codage / rotation | MFM DD, 300 tr/min |
| Premier bloc logique | FAT, préfixe FD FF FF ; aucun secteur d'amorçage |
| Répertoire racine | bloc logique 2, 192 entrées, 6 blocs de 1024 octets |

Le numéro logique zéro ne désigne pas ici un identifiant physique R=0 : il est enregistré dans R=1. Le [chapitre 9 du guide technique](https://acorn.huininga.nl/pub/mirrors/ftp.nvg.org/pub/bbc/doc/Master%20512%20Technical%20Guide/m512tech09.htm) décrit la capacité, la taille de secteur et l'organisation FAT. La trace du logiciel DOS Plus 2.1 confirme ses demandes effectives au WD : R=1 pour la FAT, puis les lectures du répertoire et des fichiers. Le répertoire brut contient DISK #2 et les noms GEMSYS, GEMDESK, GEMBOOT, GEM.BAT, GEMAPPS et COMMAND.COM.

## Avant / après

- Montage IBM 800 (512 octets) : GEM introuvable ; aucun verdict souris.
- Montage ADFS 800 (1024 octets, R=0–4) : DOS demande R=1 et reçoit le second bloc FAT ; le mauvais profil entraîne une mauvaise interprétation du disque. DIR affiche des noms vides et TYPE GEM.BAT échoue. La trace a conservé les octets effectivement lus, sans lecture de bus ajoutée ; source instrumentée restaurée.
- Format distinct `acorn.dosplus.800` : reconstruction R=1–5, politique de flux ISO MFM et cadence DD. DIR affiche correctement les six entrées et 84992 octets libres. Le batch est lu, ADDMEM 333 exécuté puis GEMVDI chargé. GEM passe en mode graphique et affiche B:\GEMDESK\DESKTOP.APP avec un curseur d'attente. La validation du bureau terminé et des mouvements/boutons reste séparée.

Captures natives : `artifacts/TestsMachines/Acorn/BBC Master 512/20261108-master512-mouse-dos800-mfm-directory-debug.bmp`, `...-batch-debug.bmp` et `...-gem-3-debug.bmp`. Ce sont les images réellement produites hors RML, pas des captures de l'application ni une mesure d'entrées physiques.

## Réalisation

Le format et sa géométrie appartiennent à MediaEngine. Le module Acorn emprunte l'image par Emulation ; aucune référence directe à MediaEngine n'est ajoutée. Le CPU, le Tube et le WD ne sont pas changés pour déplacer les numéros de secteur. Le format est raccordé à la lecture, à l'écriture linéaire, à la détection, au catalogue, aux politiques de flux et au filtre des lecteurs DD.

Tests autonomes en mémoire : 9 cas Debug réussis, 0 échec, 115 ms. Détection IMG/800/ADF, maintien des autres formats, secteurs aux limites de piste/face/cylindre, conservation du contenu, flux MFM de 200 ms et réécriture exacte sans bloc ajouté. Ces tests ne constituent pas une validation complète du Master 512 ni de sa souris. Aucun gain de vitesse n'est revendiqué pour cette correction de format.

## Bureau et contrôle invité

GEM atteint son bureau après chargement normal de GEMVDI et DESKTOP.APP. Capture `20261108-master512-mouse-gem-ready-ready-debug.bmp` ; mouvements et arrêt capturés séparément. Les menus s'ouvrent au survol : leur seule apparition ne prouve pas un bouton. Le contrôle console a permis de sélectionner Exit to DOS ; avec le raccordement AMX utilisé dans ce passage, le bouton droit exécute cette commande et revient à B>. Preuve finale `20261108-master512-mouse-gem-control-after-selection-debug.bmp`. Ne pas déclarer le bouton gauche conforme sur ce résultat.

La comparaison `b-em-mouse.c`, source amont stardot/b-em/master/src/mouse.c archivée le 8 novembre, distingue le Master 512 : données X/Y PB3/PB4, boutons PB0/PB2/PB1, contre PB0/PB2 et PB5/PB7/PB6 pour AMX. Le pilote 6502.SYS archivé détecte également ces deux dispositions : à partir de 2B8C il teste les bits 18h du port B et sélectionne les masques 08h/10h, au lieu de 01h/04h. Le second brochage existe déjà dans le raccordement Compact du module ; le Master 512 l'utilisera pour sa souris Acorn, sans changer le VIA ou ajouter un pilote dans App. La comparaison B-Em ne justifie pas son sondage de l'IFR depuis la souris : ce sondage ne sera pas repris.

Le format complet passe maintenant 14 tests autonomes Debug (336 ms) et 34 tests existants de capacité média/Master (667 ms), sans échec. Le disque vierge reçoit les entrées FAT réservées FD FF FF ; il reste identifiable après réouverture des octets bruts.

## Raccordement Acorn et résultats après correction

La fabrique de carte sélectionne désormais pour le coprocesseur Master512 le brochage Acorn déjà implémenté pour Compact ; les autres cartes conservent AMX. Aucun changement des horloges, des phases, du VIA, des interruptions ou de l'algorithme de progression. Les 27 tests pointeur passent en Debug (745 ms), dont deux cas de carte vérifiant boutons gauche/droit/milieu et relâchement via le véritable VIA selon le coprocesseur.

Le bureau GEM est de nouveau atteint avec ce raccordement. Pendant son fonctionnement, les compteurs du pilote hôte, aux octets 2821–2824, reçoivent :

| Entrée | Résultat relatif observé |
| --- | --- |
| X positif 20 unités réparties | +20 |
| X négatif 10 unités réparties | −10 |
| Y hôte positif 20 unités | −20 dans le compteur matériel, orientation inverse à l'écran |
| Y hôte négatif 10 unités | +10 |
| X positif 20 en une fenêtre | +20 |
| X négatif 20 en une fenêtre | −20, retour exact à la valeur précédente |
| Repos pendant 60 champs | compteurs et image inchangés |

Ces mots sont des compteurs matériels 16 bits, susceptibles de reboucler ; ce ne sont pas les coordonnées absolues du curseur graphique. Ils ont été observés directement dans la RAM hôte pendant l'exécution, sans lecture de bus ajoutée ni injection d'état CPU. Les captures montrent aussi les déplacements réellement affichés. Préfixe `20261108-master512-mouse-gem-acorn-` sous TestsMachines/Acorn/BBC Master 512, avec copies des étapes utiles sous TestsChips/Mos6522/BBC Master 512. La preuve du répertoire DOS 800 est également copiée sous TestsChips/Wd1770/BBC Master 512.

Le bouton gauche referme bien le menu GEM au clic. Le dernier pointage dépassait toutefois la ligne Exit to DOS : ce passage **ne prouve pas** l'exécution de cette commande avec le bouton gauche. La sélection explicite d'une action ou un tracé dans une application GEM restent ouverts dans le suivi ; aucune validation physique RML n'est revendiquée. Le banc est sorti normalement par quit, avec détachement et libération de ses deux images en finally.

Le groupe format final comporte **20 tests Debug réussis**, 0 échec, 334 ms : les six cas supplémentaires vérifient la sélection IMG/800 et l'exclusion ADF, indépendamment de la casse de l'identifiant. Les 34 régressions capacité/Master précédemment consignées passent également ; leur journal TRX jetable a été supprimé.

### Recette du banc pour la suite, sans refaire les recherches

Reprendre le montage de board-validation.md : MOS 3.20, client 80186 1.01, caractères saa5050 ; banques du bundle 9 à 15 sauf 12, CMOS[19] bas=8, Master512 avec expandedIo, WD1770 et userPortPointer activés. Monter en lecture seule la disquette 2.1 connue sur 0 (acorn.adfs.640) et GEM2.800 sur 1 (acorn.dosplus.800). Exécuter 80 champs, maintenir Shift lors du reset froid, relâcher après 121 champs, poursuivre 2079 champs. Saisir B:, GEM depuis le clavier logique existant ; laisser le drainage audio et la vidéo natifs actifs. Après le lancement, attendre deux fenêtres de 400 champs sans lecture nouvelle, après au moins 1600 champs et avec une borne de 6000. Ce critère facilite l'observation ; l'image du bureau doit encore être inspectée.

Conserver ensuite la machine dans une boucle de contrôle console synchrone : move dx dy répétitions, button gauche/droit/milieu, key Escape, wait nombre de champs, cap nom, quit. Avant un mouvement isolé, présenter deux instantanés immobiles espacés d'un champ pour représenter la cadence d'entrée habituelle ; sinon le banc confond sa longue attente sans SetInput avec la durée d'un échantillon de mouvement. Envoyer chaque déplacement une fois, puis l'immobilité. Ne pas déduire la sélection d'un menu de la seule position approximative de la flèche : vérifier la ligne effectivement surlignée avant le clic. Ne quitter le banc qu'après observation du résultat voulu ; libérer dans finally les images possédées et détacher les références empruntées. Cette recette ne crée ni thread par puce, ni nouveau mécanisme dans le produit.
