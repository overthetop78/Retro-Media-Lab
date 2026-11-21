# Detection de polling SH-2 reprise de PicoDrive

Source locale : `artifacts/References/Machines/Sega/32X/PicoDrive/pico/32x/memory.c`, `p32x_sh2_poll_detect` vers la ligne 118 et `p32x_sh2_poll_event` vers la ligne 159.

La source retire l'alias non cache `20000000`, considere les adresses situees dans les quatre octets a partir de l'adresse suivie, et conserve la detection si au plus 20 cycles ont passe. Elle n'incremente le compteur que lorsque plus de deux cycles separent les lectures et que le marqueur `no_polling` n'est pas positionne. Deux moities d'un acces long ne doivent donc pas remettre a zero le compteur. DT positionne `no_polling`, que chaque appel de detection efface ensuite.

L'ancien `Mega32X.PollRead` imposait en plus une adresse, un PC et une valeur strictement identiques ; toute lecture voisine ou espacee de deux cycles ou moins remettait le compteur a zero. Une boucle lisant deux mots voisins avec deux instructions MOV.W ne pouvait pas etre detectee, meme si elle satisfaisait les conditions de PicoDrive.

Le nouveau chemin reprend les conditions de fenetre et de cadence de la source. `PollingEpoch`, incremente par DT dans la puce, inhibe l'increment du compteur jusqu'au prochain appel de detection comme le marqueur de la source. PC et valeur restent releves pour les diagnostics et le format de sauvegarde existant, mais ne conditionnent plus la detection. Aucun champ d'etat n'est ajoute ni retire.

Les reveils existants sur interruption et changement du VDP restent inchanges. Le reveil sur ecriture accepte maintenant aussi toute adresse de la fenetre suivie, y compris lorsqu'elle commence sur le second mot d'un groupe aligne de quatre octets ; l'ancien reveil par groupe aligne reste conserve.

Validation ciblee confiee au parent : une boucle lisant deux mots voisins par deux MOV.W distincts doit atteindre la detection sans remise a zero par la seconde lecture ; une lecture espacee de deux cycles ou moins ne doit pas incrementer le compteur ; DT doit inhiber la detection suivante ; une ecriture au premier ou second mot doit reveiller le processeur. Les seuils definis par les appelants, le quantum CPU et les cycles des instructions ne sont pas modifies dans cette correction. Aucun gain FPS n'est annonce avant mesure.
