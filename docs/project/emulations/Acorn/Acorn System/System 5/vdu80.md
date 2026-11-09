# VDU80 400,019 — capture et sérialisation

Source acquise : *Acorn System 5 Handbook*, carte 400,019, §2.3–2.6, figures 10, 14, 15 et 18. PDF local `artifacts/.tmp-archives/machines/Acorn/Acorn System 5/System5Handbook.pdf`. Extraits inspectés : `System5-vdu-clock-084.png`, `System5-vdu-character-timing-87.png` et `System5-vdu-schematic-93.png` dans le même dossier. La page 85 donne le réglage des oscillateurs locaux ; ces oscillateurs ne doivent pas être inventés lorsque les clocks du bus sont sélectionnées.

- Avec LK6/LK8, la carte sélectionne les clocks externes 2 et 12 MHz. Les inversions IC15a/IC18c rendent 2 MHz' de même polarité que le 2 MHz du bus. Le 12 MHz sélectionné va au CLK du 74LS165. Le schéma connecte également 2 MHz' au 74LS374 et au CRTC.
- IC17d et R8/C3 produisent NL, impulsion basse typique de 20 ns sur la montée 2 MHz'. NL charge les entrées parallèles du 74LS165. Ce n'est ni une période entière ni un registre à avance logicielle fixe.
- IC12, 74LS374, capture D0–D6 du caractère et DE retardé en D7 sur la montée 2 MHz'. R5/R6/C1 retardent DE de manière que sa capture soit décalée d'un caractère selon le texte §2.5.1. L'adresse du caractère vient de MA du CRTC, pas de son adresse rendue après skew.
- IC13 est la ROM de caractères MM52116 indiquée au schéma. Les sept bits du caractère vont à A4–A10 ; RA0–RA3 vont à A0–A3. D3–D7 de la ROM vont à C–G du sérialiseur ; A, B et H sont à zéro. SER et CLK INH du 74LS165 sont à zéro.
- La figure 15 conserve le motif ROM du caractère n pendant NL, alors que la sortie d'IC12 passe au caractère n+1 : charger le motif précédent avant de propager le nouveau caractère vers la ROM. Une affectation C# immédiate du nouveau motif pendant NL bas ne reproduirait pas cet ordre.
- Le registre émet H puis G, F, E, D, C : un espace suivi des cinq points du caractère, avant la charge suivante. QH est validé par DE conservé dans IC12, puis inversé par le NAND IC18d. CU et LK2 commandent l'inversion du signal vidéo par les XOR ; un curseur inverse donc le motif, il ne remplace pas systématiquement le caractère par six points blancs.

Le chemin actuel `DrawMonochromeCell` ne représente pas ces captures et clocks. Les composants SN74LS374 et SN74LS165 sont réalisés séparément et vérifiés ; le raccordement est encore en cours. Les phases de capture, de charge et d'émission doivent être vérifiées sur les chronogrammes avant le remplacement. Les sorties MA, DE et CU du CRTC doivent être reçues par notifications, sans ajouter leur sondage au rythme des dots. Les coordonnées du framebuffer restent une responsabilité du rendu et ne doivent pas devenir des compteurs matériels dupliqués.

## ROM et choix de profil temporel

La fiche détaillée National MM52116, *1980 Memory Data Book* pages 7-30 à 7-32, fournit tA maximum 450 ns, tAC maximum 120 ns et tOFF maximum 100 ns, sans valeurs typiques dans ces colonnes. Elle distingue adresse avant sélection et adresse après sélection. Voir `docs/chips/Memory/Mm52116.md` pour les sources et conditions. Le composant paramétrable existe désormais dans Chips et gère ces trois délais localement, sans oscillateur fictif, mais aucune valeur de carte n'est imposée. L'utilisateur est interrogé sur l'emploi des bornes documentées ou l'exigence de mesures électriques réelles ; le raccordement qui dépend de cette décision reste en attente. Les durées des fixtures autonomes sont explicitement synthétiques et ne sont pas des mesures du montage.

## Données avant la réponse de la ROM

Le raccordement peut désormais transmettre au SN74LS165 les cinq broches C–G inconnues, tout en gardant A/B/H connus à zéro : masque 83 hex tant que la ROM ne fournit pas de mot connu. Une réponse connue place D3–D7 du mot sur C–G (décalage d'un bit vers la droite, masque 7C hex). La charge propage cette connaissance dans le registre ; les données reçues pendant SH/LD haut ne rechargent pas son contenu. Ce trajet est vérifié avec une ROM synthétique autonome, 28 contrôles ROM/registre réussis. Le profil temporel de la carte reste en attente de décision ; ce contrôle ne constitue pas encore la capture de l'écran de la carte raccordée.

## Clarification et circuit numérique — 9 novembre, après échange utilisateur

La demande de mesure électrique évoquée plus haut ne bloque plus le travail : la valeur nominale non publiée n'est pas nécessaire pour poursuivre. Le circuit utilise explicitement NL 20 ns typiques et les plafonds ROM 450/120/100 ns ; ces valeurs ne deviennent pas des mesures réelles de la carte. Le trajet latch/ROM/sérialiseur est maintenant réalisé dans `RetroMediaLab.Chips/Controllers/Acorn/Vdu80CharacterGenerator/`. Sept contrôles du trajet et 56 contrôles du groupe réussissent. Les conditions, limites et mesures figurent dans `docs/chips/Controllers/Vdu80CharacterGenerator.md`. Le remplacement de DrawMonochromeCell et le contrôle de la carte assemblée restent à réaliser avant clôture de System 5.

## Phase du curseur révélée par le raccordement

Le manuel §2.6.2/figure 18 contient un réseau RC de CU avant le XOR IC16a : R12 1 kΩ, R13 2,7 kΩ, C6 330 pF (lecture du schéma archivé). Il est absent du profil initial, qui publie CU sans délai. Deux contrôles de carte sur l'inversion du curseur échouent à mi-caractère ; conserver ce défaut ouvert et le corriger dans le composant, sans changer le skew R8 documenté à 40 hex. Le manuel ne chiffre pas ce délai RC ; question utilisateur sur un profil nominal calculé/documenté en cours. La table Hitachi 1981 confirme HD46505SP-2 = HD68B45SP et les sorties au front descendant ; voir Crtc6845.md pour la référence acquise et ses plafonds de propagation, distincts du RC externe.

## Profil calculé réalisé après accord utilisateur

Faute de mesure électrique disponible, l'utilisateur autorise le profil calculé. R12 série 1 kΩ, R13 shunt 2,7 kΩ et C6 330 pF donnent un gain 27/37 et τ=240,8108 ns. Le seuil relatif de référence est explicitement fixé à 50 % du niveau haut établi ; c'est une convention du modèle calculé, pas une caractéristique TTL garantie. Le service analytique `Vdu80CursorCircuit` conserve la charge entre transitions et notifie ses franchissements à la résolution de 1 ns. La fiche de composant décrit ses paramètres, équations, limites et validations.

Les anciennes questions de profil sont résolues par cet accord. Les contrôles de carte mesurent 167 ns de retard montant et 135 ns de retard descendant après une impulsion CU de 500 ns. Le curseur est vérifié par sa trace et le XOR du motif selon ce profil, sans supposer une frontière parfaite de six dots non établie par les seuls composants RC. 44 contrôles du groupe de composants et 185 contrôles System/disque/audio réussissent. L'essai natif et ses captures/performances restent à produire.
