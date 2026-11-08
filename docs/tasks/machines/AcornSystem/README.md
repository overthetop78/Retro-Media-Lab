# Suivi — AcornSystem

- [ ] Acorn System : terminer toutes les puces et valider les variantes exposées.
  - [ ] Modifier `docs/tasks/machines/AcornSystem/implementation.md` avec les actions concrètes restantes, notamment la carte CPU 6809, après clôture du bloc Master ; consigner les tests de tous les modèles/équipements, médias et captures réellement obtenus.


- [Implémentation et résultats existants](implementation.md)
- [Puces communes](../../chips/README.md)

- [Reprise System 1](system1-validation.md) : référence de carte complète, suppression des activités de composants absents, notifications et validation du clavier/moniteur/bande avant livraison.

- [Preuves System 1 du 8 novembre](../../../project/emulations/Acorn/Acorn%20System/System%201/validation.md) : clavier/pavé numérique, écriture/relecture 0200=5A et barre visible vérifiés dans RML à 50 FPS, 1,01 MHz arrondis et 100 %. Programme saisi par le moniteur puis sauvegardé/rechargé depuis le signal bande et réexécuté hors interface ; GO vérifié jusqu'au PC FEF3 avec G et X. Carte complète Debug : 2,12 ms/présentation dans le dernier scénario ; tests et corrections du rendu LED détaillés dans la fiche. Les contrôles temporaires dépendant du firmware sont supprimés, les tests autonomes sont conservés.
