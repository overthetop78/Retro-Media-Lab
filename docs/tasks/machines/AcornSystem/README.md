# Suivi — AcornSystem

11 novembre2026 : l’utilisateur demande d’arrêter le travail sur System5 et de passer à Atom. Derniers résultats réels et limites dans [la validation](system5-validation.md) et [le bilan](system5-performance-audit.md). Les50 FPS historiques ci-dessous concernent des écrans peu chargés : le texte après FORM80 a montré31,6 FPS, Ctrl+L rétablissant50,2 FPS. Correction clavier testée dans le code mais non livrée à la session utilisateur ; candidat de caractère VDU testé isolément, non raccordé ni mesuré sur machine. Ces limites sont conservées au passage à Atom, pas présentées comme résolues.

- [x] Acorn System : terminer toutes les puces et valider les variantes exposées.
  - [x] Modifier `docs/tasks/machines/AcornSystem/implementation.md` avec les actions concrètes restantes, notamment la carte CPU 6809, après clôture du bloc Master ; consigner les tests de tous les modèles/équipements, médias et captures réellement obtenus.
  - [x] Terminer le contrôle sonore demandé dans `system5-validation.md` : chemin Windows vérifié, chargement BASIC entendu par l'utilisateur au volume 10 ; formatage prolongé du lecteur 1 terminé et vérifié, puis persistance des 800 secteurs contrôlée après fermeture. L'écoute de ce formatage précis reste distincte du chargement déjà entendu.


- [Implémentation et résultats existants](implementation.md)
- [Puces communes](../../chips/README.md)

- [Reprise System 1](system1-validation.md) : référence de carte complète, suppression des activités de composants absents, notifications et validation du clavier/moniteur/bande avant livraison.

- [Reprise System 2](system2-validation.md) : montage COS/VDU40, clavier ASCII, bande et BASIC compatible, après clôture System 1 dans `f3aa3bbea`.

- [System 3](system3-validation.md) : 6502 et 6809 vérifiés hors interface puis dans RML ; catalogue/BASIC et moniteur mémoire/exécution, à 1 MHz et 50 FPS dans les essais limités.
- [System 4](system4-validation.md) : BASIC, pavé numérique et catalogue sur chacun des deux ports vérifiés dans RML Debug, à 1 MHz et 50 FPS.
- [System 5](system5-validation.md) : VDU80 progressive 480×250, BASIC et les deux ports vérifiés dans RML Debug, à 2 MHz et 50 FPS. Contrôle commun des canaux Audio et du volume/muet à chaud sur les System 1–5, variante 6809 comprise ; le rendu d'un son ne constitue pas à lui seul une preuve d'écoute dans la fenêtre utilisateur.

- [Preuves System 1 du 8 novembre](../../../project/emulations/Acorn/Acorn%20System/System%201/validation.md) : clavier/pavé numérique, écriture/relecture 0200=5A et barre visible vérifiés dans RML à 50 FPS, 1,01 MHz arrondis et 100 %. Programme saisi par le moniteur puis sauvegardé/rechargé depuis le signal bande et réexécuté hors interface ; GO vérifié jusqu'au PC FEF3 avec G et X. Carte complète Debug : 2,12 ms/présentation dans le dernier scénario ; tests et corrections du rendu LED détaillés dans la fiche. Les contrôles temporaires dépendant du firmware sont supprimés, les tests autonomes sont conservés.
