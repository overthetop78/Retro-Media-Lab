# Suivi — BbcMaster

- [ ] Acorn BBC Master : terminer toutes les puces et valider chaque machine exposée.
  - [ ] Modifier `docs/tasks/machines/BbcMaster/implementation.md` avec les actions concrètes encore nécessaires après inventaire des dépendances et réalisation des puces communes ; consigner les validations natives des variantes et médias après réalisation, avec captures et build des modules.


- [Implémentation et résultats existants](implementation.md)
- [Ralentissement Debug et nouveaux essais individuels](cadence-debug.md)
- [Puces communes](../../chips/README.md)

- [Reprise ciblée Master 128 du 7 novembre](validation-128.md) : contrôle invité BASIC/RTC et mesures Debug, sans clôture des budgets ni reprise des autres variantes.

- [Synchronisation ADC sur le bus Master](adc-sync.md) : défaut reproduit puis corrigé ; délais et IRQ vérifiés par tests autonomes, quatre canaux contrôlés sous MOS réel.

- [Médias natifs Master 128](media-128.md) : DFS persistant, ADFS sur les deux ports Winchester, SAVE/CHAIN tape et Boulder Dash vérifiés hors RML ; livraison Debug All contrôlée.

- [Observation RML Debug](../../../project/emulations/Acorn/BBC%20Master/Master%20128/rml-validation.md) : cadence nominale constatée au repos, capture native conservée ; saisie automatisée et validation en charge non acquises dans l'interface.

Point de passage validé le 7 novembre : l'utilisateur juge la baisse minime en Debug et autorise explicitement la poursuite. Le bloc Master 128 est accepté avec les mesures et limites consignées, sans revendiquer une amélioration non mesurée ni étendre cette acceptation aux autres modèles. La suite est le Master Turbo, en commençant par les preuves encore ouvertes du Tube et du 65C102. Turbo/512/ET/Compact ne sont pas clôturés par les résultats du Master 128. Les sauvegardes/chargements d'état restent reportés.

Point de passage du 8 novembre : bloc Master Turbo accepté par l'utilisateur sous condition de cadence Release, satisfaite à 4,52–5,25 ms/champ (190–221 champs/s sans limite). Les limites Debug restent consignées. [Suivi et preuves Turbo](turbo-validation.md). La machine suivante est le Master 512 ; ses défauts de démarrage ne sont pas clôturés par cette acceptation.

- [Reprise Master 512](512-validation.md) : la correction RETF du 80186 rétablit DOS Plus 2.1 ; l'image 1.2 de 8BS démarre également. La comparaison des fichiers isole dans l'image 1.2 initiale deux octets de comparaison mémoire 896/512 Kio. [Configuration, images et preuves](../../../project/emulations/Acorn/BBC%20Master/Master%20512/board-validation.md). Création/relecture du fichier DOS et CHKDSK vérifiés ; 31 tests de raccordement Master passent. Release mesurée à 6,76–6,82 ms/champ, Debug autour de 40 ms/champ. Les autres contrôles de la carte et sa clôture restent ouverts.

- [DOS Plus 800 et GEM sur Master 512](../../../project/emulations/Acorn/BBC%20Master/Master%20512/dos-plus-800.md) : format physique corrigé dans le moteur média, bureau GEM affiché, brochage souris Acorn raccordé et compteurs X/Y vérifiés. 20 tests format, 34 régressions capacité/Master et 27 tests pointeur Debug passent. Le clic gauche est reçu ; une sélection applicative explicite, les contrôles HDD propres au 512 et la clôture de cette machine restent ouverts.
