# Circuit de répétition du clavier Acorn 400,013

## Référence et périmètre

[Acorn System 5 Handbook](https://chrisacorns.computinghistory.org.uk/docs/Acorn/Manuals/Acorn_System5Handbook.pdf), chapitre Keyboard Interface 400,013, §2.3, pages imprimées 3–4. Archive locale : `artifacts/.tmp-archives/machines/Acorn/Acorn System 5/System5Handbook.pdf` et texte adjacent. Cette carte clavier est utilisée par les System ASCII ; la matrice hexadécimale du System 1 n'a pas ce circuit.

L'entrée AKD indique qu'un caractère est maintenu. Les sept bits ASCII restent valides après son relâchement, jusqu'au caractère suivant. Le circuit RC de répétition forme NSTROBE actif bas à partir de AKD et de REPEAT. Le manuel décrit 0,5 s avant répétition automatique et une oscillation nominale 10 Hz ; REPEAT supprime le retard initial et permet de répéter le dernier code même après relâchement. Le codeur AY-3-4592 et son balayage matriciel sont distincts de ce circuit : l'adaptateur hôte fournit ici les données ASCII décodées et AKD, comme avant cette correction. Cette fiche ne présente pas le circuit de répétition comme une émulation du codeur AY complet.

## Réalisation — 9 novembre 2026

`RetroMediaLab.Chips/Controllers/Acorn/SystemKeyboard/Services/SystemKeyboardRepeat.cs` possède le temps de référence, les entrées mémorisées AKD/REPEAT, l'état de strobe et les échéances. La fréquence de référence sert à dater les délais RC ; ce n'est pas un oscillateur supplémentaire inventé sur la carte. Les constantes temporelles auparavant dans le module sont transférées dans `SystemKeyboardRepeatConstants.cs`.

Le retard nominal reste 500 ms, puis les deux demi-périodes nominales de 50 ms du modèle existant. Le modèle numérique ne caractérise ni les tolérances des condensateurs ni les seuils analogiques réels. Il n'ajoute pas un délai d'un tick lors d'un changement REPEAT : l'inhibition du strobe prend effet à l'instant de réception ; les fronts suivants conservent leur échéance nominale. Les notifications portent le niveau physique NSTROBE, alors que la propriété Strobe indique son assertion logique.

`SystemKeyboard.cs` conserve la traduction hôte AZERTY/autre, les modificateurs, le code ASCII retenu et la matrice passive System 1. Il raccorde AKD et REPEAT au composant même quand aucune touche de caractère n'est maintenue. Les sorties du composant sont transmises aux raccordements 8154/VIA existants. La carte conserve seulement l'échéance notifiée et avance le composant à cette échéance ou avant une nouvelle entrée. Aucune interrogation périodique du clavier ni consultation d'une autre puce ajoutée.

Reset annule les entrées, désactive le strobe et son échéance sans remettre le temps de référence en arrière. Un relâchement sans REPEAT annule la répétition ; un relâchement avec REPEAT la conserve. Les mêmes niveaux reçus ne réarment pas le circuit. Les avances groupées visitent chaque front observable, avec son horodatage propre.

## Vérifications obtenues

- **5 tests de circuit Debug réussis, zéro échec/ignoré, 70 ms** : limite exacte du délai initial, niveaux actifs bas, absence de doublon, répétition sans AKD, relâchement, reset, réaction sans tick artificiel et comparaison groupé/unitaire à 1 et 2 MHz.
- **76 tests System/RAM-E/S Debug réussis, zéro échec/ignoré, environ 1 s** : comprend la conservation du dernier ASCII, REPEAT après relâchement, lecture DataWithStrobe et les contrôles existants clavier, vidéo et bande.

Ces durées sont celles des groupes fonctionnels, pas une mesure de débit. Le débit System 2 de 194–196 % consigné avant cette extraction n'est pas réattribué au code actuel comme s'il avait été remesuré. Aucun build de livraison ni essai RML ajouté pendant ce passage.
