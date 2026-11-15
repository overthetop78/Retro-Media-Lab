# Chemin d'execution SH-2 : plages empruntees Gens

- [x] Evaluer puis retirer le candidat de fetch endian et Span local
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Contracts/ISh2Bus.cs` : ajouter a `TryMapFetch` l'ordre des octets de la plage empruntee, pour consommer la ROM wordswapped sans copie.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Sh2.cs` : conserver le span de la plage de code dans la boucle comme le PC hote mappe de Gens, choisir sa lecture 16 bits selon son ordre des octets et garder controle de version, attentes, exceptions et delay slots existants.
  - [x] Modifier `src/RetroMediaLab.Chips/Controllers/Hitachi/SH7095/Sh7095.cs` : propager l'ordre des octets externe et conserver les lignes de cache et la cache-RAM en big-endian.
  - [x] Creer artifacts/Emulation/segaemu-32x/parallel-* et retained-tests-*.log : consigner essais Debug/Release, rejet du candidat et validation de l'etat anterieur restaure.

- [x] Retirer la conservation du span local apres regression du banc groupe
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Sh2.cs` : retrouver les vues `ReadOnlyMemory.Span` du chemin precedemment mesure a 19,70 FPS Debug ; conserver seulement le choix de lecture endian exige par la ROM empruntee.

- [ ] Retirer le groupe endian/fetch ROM apres comparaison defavorable
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Contracts/ISh2Bus.cs`, `Sh2.cs` et `src/RetroMediaLab.Chips/Controllers/Hitachi/SH7095/Sh7095.cs` : restaurer exactement le contrat quatre arguments et la lecture big-endian du mapping precedent, sans restaurer HEAD ni retirer le SH-2.

Resultat groupe transmis par l'agent parent : 19,19 FPS Debug et 86,08 FPS Release, contre 19,70 et 90,79 avant ces changements. Groupe rejete pour absence de gain. Aucun test ni build supplementaire lance par cet agent.
