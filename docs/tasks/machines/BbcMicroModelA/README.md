# BBC Micro Model A

## Décision de passage au Model B — 7 novembre 2026

Les preuves acquises couvrent l'exécution de programmes, les circuits communs, le SAVE/CHAIN tape et ses quatre conteneurs, les disquettes, les transferts réseau, les montages mémoire testés et les accès HDD décrits ci-dessous. L'utilisateur confirme maintenant les clics tape corrigés et les deux volumes. Ne pas répéter ces validations sans nouveau défaut établi.

Le Model A reste **incomplet sur le budget Debug des transferts actifs WD et HDD**. Les deux optimisations sont explicitement reportées : recherches déjà prolongées, caches sans gain rejetés et aucune nouvelle solution viable établie. Le passage au Model B applique cette exception de la directive permanente ; il ne transforme ni ces limites en réussite ni les essais optionnels non couverts en validations acquises. Réutiliser les composants vérifiés et examiner d'abord les différences réelles du Model B.

Validation utilisateur reçue le 6 novembre 2026. Ne pas reprendre leur optimisation sans nouveau défaut établi.

- [Implémentation propre au modèle](implementation.md)
- [Nouveaux essais progressifs dans RML Debug](validation-rml.md)
- [Validation commune BBC Micro](../BbcMicro/README.md)
- [Puces réutilisées](../../chips/README.md)

## Point ouvert au 7 novembre — HDD actif

Le budget de 19,97 ms/image Debug reste non atteint pendant les LOAD continus depuis le HDD : dernière référence de carte complète autour de 42,6 ms/image, contre environ 17 ms au repos. Après recherche prolongée et rejet des caches sans gain confirmé, cette optimisation est explicitement reportée selon la directive permanente ; elle reste incomplète et ne valide pas la clôture globale de la machine. Conserver les corrections vérifiées, terminer les autres composants encore ouverts du Model A, puis son câblage et ses validations. [Essais, résultats et raisons du report](../../../chips/Controllers/AdaptecAcb4000/clock-cell-profile.md).


MC6854/Econet vérifié le 7 novembre : 76 tests puce et 27 raccordements Debug réussis ; contrôle réellement exécuté par le 6502, 4096 échanges dans chaque sens et verdict 9/9. Carte complète sous ce trafic : 19,203 ms/image Debug avec rendu et drainage audio. [Conditions et captures](../../../chips/Network/Mc6854/model-a-validation.md). Ce résultat ne clôture ni le budget HDD reporté ni les essais finaux de toutes les configurations du Model A.

WD1770/1772 : sorties et cadencement vérifiés (183 tests puce + 34 raccordements Debug). Model A 32 Kio : catalogue et chargement SSD, Invaders option rapide exécutée à 19,363 ms/image sans interface. Le chargement reste à 29,706 ms/image ; performance active encore ouverte, aucune clôture globale. [Résultats et captures](../../../chips/Controllers/Wd1770.md).

Rotation WD inactive : regroupement à entrées stables conservé après 38 tests et deux comparaisons de carte complète. Fin de LOAD + rotation résiduelle : 30,99 → 25,93 ms/image Debug, mêmes cycles et données. La lecture continue reste proche de 31 ms/image ; le budget Debug reste ouvert. [Détail](../../../chips/Controllers/Wd1770/idle-clock-validation.md).

WD actif : primitive interne bornée par les échéances, utilisée uniquement avec entrées physiques stables. LOAD continu : 30,83 → 26,11 ms/image Debug sur deux paires (15,30 %), mêmes cycles/données/vidéo. 42 raccordements finaux passent. Le budget Debug en lecture reste non atteint ; aucune clôture globale. [Détail](../../../chips/Controllers/Wd1770/active-clock-validation.md).

Montages mémoire : onze démarrages MOS/BASIC contrôlés sans interface, RAM principale 16/32 Kio, B20, Watford, BooBip ×1–4 et B12 0/8/16 Kio ; 16,77–18,01 ms/image Debug au repos et 45 tests de bus/configuration réussis. Aucun défaut établi ni réécriture des puces. Les ROM de gestion et les transferts médias ne sont pas validés par ce banc. [Mesures, portée et captures](../../../project/emulations/Acorn/BBC%20Micro/Model%20A/memory-extensions.md).

Tape enregistrement/lecture : SAVE par MOS sur une carte 16 Kio, CHAIN du signal enregistré sur une seconde carte, programme et résultat 46 contrôlés. Dernier passage Debug : 17,10 ms/image SAVE, 18,22 ms/image CHAIN (rendu/drainage inclus, contrôle écran supplémentaire pendant CHAIN), sans changement des puces. [Cycles, limites et captures stabilisées](../../../chips/IO/BbcSerialUla.md). Les 16 tests audio passent également ; l'écoute et les volumes de la sortie Windows restent à confirmer, le contrôle Windows étant indisponible à ce passage.

Formats enregistrables : WAV, UEF, CSW et RAW sauvegardés puis relus via le SDK ; CHAIN exécuté sur une nouvelle carte par format, texte et résultat 46 contrôlés. Même nombre de cycles, 17,95–19,24 ms/image Debug. [Conditions, tailles et captures](../../../project/emulations/Acorn/BBC%20Micro/Model%20A/tape-formats-validation.md). L'essai Windows reste ouvert : inventaire rétabli, mais lancement refusé par `GetCursorPos` et contrôle de l'Explorateur expiré à l'approbation, sans action dans RML.
