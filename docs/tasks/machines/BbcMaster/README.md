# Suivi — BbcMaster

- [ ] Acorn BBC Master : terminer toutes les puces et valider chaque machine exposée.
  - [ ] Modifier `docs/tasks/machines/BbcMaster/implementation.md` avec les actions concrètes encore nécessaires après inventaire des dépendances et réalisation des puces communes ; consigner les validations natives des variantes et médias après réalisation, avec captures et build des modules.


- [Implémentation et résultats existants](implementation.md)
- [Ralentissement Debug et nouveaux essais individuels](cadence-debug.md)
- [Puces communes](../../chips/README.md)

- [Reprise ciblée Master 128 du 7 novembre](validation-128.md) : contrôle invité BASIC/RTC et mesures Debug, sans clôture des budgets ni reprise des autres variantes.

- [Synchronisation ADC sur le bus Master](adc-sync.md) : défaut reproduit puis corrigé ; délais et IRQ vérifiés par tests autonomes, quatre canaux contrôlés sous MOS réel.
