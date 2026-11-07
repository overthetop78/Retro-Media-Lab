# BBC Micro Model B

Validation utilisateur reçue le 6 novembre 2026. Ne pas reprendre leur optimisation sans nouveau défaut établi.

- [Implémentation propre au modèle](implementation.md)
- [Contrôle de la carte assemblée](validation-board.md) — suivi du passage du 7 novembre ; ne clôture pas les budgets WD/HDD actifs.
- [Résultats avec MOS/BASIC réels](../../../project/emulations/Acorn/BBC%20Micro/Model%20B/board-validation.md)
- [Validation commune BBC Micro](../BbcMicro/README.md)
- [Puces réutilisées](../../chips/README.md)

## Passage au B+ — 7 novembre 2026

Les contrôles propres au B sont réalisés : les quatre canaux ADC, leur inversion et T1/acquittement du VIA utilisateur passent sous MOS/BASIC, et Boulder Dash progresse en partie en Debug. Les transferts cassette et disquette disposent déjà de leurs validations communes référencées ; ils ne sont pas rejoués après chaque contrôle du modèle. La validation utilisateur antérieure est conservée.

La clôture globale reste refusée pour les budgets communs ouverts : lecture WD autour de 26,11 ms/image, HDD autour de 42,6 ms/image ; les tentatives prolongées et caches sans gain sont consignés dans les fiches des contrôleurs et explicitement reportés selon la directive de durée. Le redessin continu mesuré à 20,50 ms est également au-dessus de 19,97 ms ; les reprises vidéo communes précédentes ont été retirées faute de gain global, aucune nouvelle solution établie n'est revendiquée. Réutiliser ces résultats et reports sans les transformer en réussite ; poursuivre les différences B+ déjà prévues, puis le Master dans l'ordre convenu.
