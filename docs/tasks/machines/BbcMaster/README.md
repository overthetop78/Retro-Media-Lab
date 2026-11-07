# Suivi — BbcMaster

- [ ] Acorn BBC Master : terminer toutes les puces et valider chaque machine exposée.
  - [ ] Modifier `docs/tasks/machines/BbcMaster/implementation.md` avec les actions concrètes encore nécessaires après inventaire des dépendances et réalisation des puces communes ; consigner les validations natives des variantes et médias après réalisation, avec captures et build des modules.


- [Implémentation et résultats existants](implementation.md)
- [Ralentissement Debug et nouveaux essais individuels](cadence-debug.md)
- [Puces communes](../../chips/README.md)

- [Reprise ciblée Master 128 du 7 novembre](validation-128.md) : contrôle invité BASIC/RTC et mesures Debug, sans clôture des budgets ni reprise des autres variantes.

- [Synchronisation ADC sur le bus Master](adc-sync.md) : défaut reproduit puis corrigé ; délais et IRQ vérifiés par tests autonomes, quatre canaux contrôlés sous MOS réel.

- [Médias natifs Master 128](media-128.md) : DFS persistant, ADFS sur les deux ports Winchester, SAVE/CHAIN cassette et Boulder Dash vérifiés hors RML ; livraison Debug All contrôlée.

- [Observation RML Debug](../../../project/emulations/Acorn/BBC%20Master/Master%20128/rml-validation.md) : cadence nominale constatée au repos, capture native conservée ; saisie automatisée et validation en charge non acquises dans l'interface.

Point de passage validé le 7 novembre : l'utilisateur juge la baisse minime en Debug et autorise explicitement la poursuite. Le bloc Master 128 est accepté avec les mesures et limites consignées, sans revendiquer une amélioration non mesurée ni étendre cette acceptation aux autres modèles. La suite est le Master Turbo, en commençant par les preuves encore ouvertes du Tube et du 65C102. Turbo/512/ET/Compact ne sont pas clôturés par les résultats du Master 128. Les sauvegardes/chargements d'état restent reportés.
