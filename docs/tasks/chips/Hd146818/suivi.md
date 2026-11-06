# Suivi — Hd146818

- [ ] HD146818 : réaliser l’horloge complète et son raccordement BBC Master après le commit ULA Electron.
  - [ ] Établir les fonctions, variantes utilisées et écarts du composant existant.
    - [x] Créer temporairement `artifacts/.tmp-hd146818/references/` avec les fiches Hitachi HD146818/HD146818A et les sources RTC de MAME, B-Em et JSBeeb ; créer `docs/chips/Timers/Hd146818.md` avec le comportement réel initial, les registres/signaux manquants, la distinction puce/câblage Master et la matrice de vérifications fondée sur ces références.
    - [ ] Modifier `docs/tasks/chips-validation.md` avec les fichiers précis du circuit, de ses interfaces, constantes, horloges, calendrier, bus, tests autonomes et raccordement Master, après établissement des exigences ; prévoir les preuves invitées, mesures, suppression des diagnostics et build de tous les modules avant le commit distinct.

[Index des puces](../README.md) · [Machines](../../machines/README.md)
