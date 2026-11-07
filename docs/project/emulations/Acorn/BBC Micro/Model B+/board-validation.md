# BBC Micro B+ — essais physiques du 7 novembre 2026

Le profil réellement observé est configuré avec **128 Kio**, même si l’instruction initiale de comparaison mentionnait 64 Kio. Ne pas utiliser ce passage comme preuve distincte du B+ 64.

Dans ROM, les supports intégrés sont IC35/IC44/IC57/IC62/IC68. La ROM DFS configurée en IC35 a été conservée ; l’utilisateur a placé `AMX Mouse Support v2.1 [variant 4].rom` dans IC44, les autres supports restant vides. L’écran `*HELP` observé dans RML annonce Advanced NFS 4.18, DFS 2.26, SRAM 1.05, AMX Mouse support V2.1 et OS 2.00. L’indicateur instantané est 640 × 512, environ 50,1 FPS, 50,1 Hz et 2,00 MHz / 100 %. Il ne s’agit pas d’une moyenne ni d’une mesure maximale sans limite.

Après exécution de !BOOT, le redémarrage affiche Econet Station 4 puis `CHAIN "!MENU"` reste sans progression visible. L’utilisateur revient à BASIC et sélectionne explicitement `*DISC`, puis `CHAIN "!MENU"` sans redémarrage intermédiaire. Il confirme ensuite que le logiciel fonctionne et que les mouvements rapides de la souris saccadent comme sur A et B. La seule présence d’Econet à l’écran ne constitue pas une preuve instrumentée du système de fichiers actif au moment du blocage.

Ces observations précèdent `4ab7f04ba`, qui corrige la référence native IGameInputDevice, rétablit les lectures Xbox 360 et Rematch dans le diagnostic et retire le repli souris Windows ajouté. Le résultat physique du nouveau build reste à vérifier ; ne pas déclarer les mouvements rapides corrigés à partir des seuls tests de raccordement. Le test de joystick invité est également en attente : l’essai a révélé l’absence de réception des entrées dans les préférences et l’assignation App avant cette correction.

Les manipulations ont été réalisées par l’utilisateur ; l’agent a observé l’interface. Aucune capture de ces derniers écrans n’a été enregistrée sur disque lors de ce passage ; ne pas inventer de chemin de preuve. Les captures historiques hors RML restent distinctes de ces observations physiques.
