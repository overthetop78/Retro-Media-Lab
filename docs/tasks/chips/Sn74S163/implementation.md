# SN74S163 — compteurs du générateur System 6502A

- [x] Réaliser le compteur physique avant son raccordement à la carte.
  - [x] Créer `artifacts/.tmp-archives/machines/Acorn/Acorn System 5/System5-clock-timing-49.png` depuis la page 49 du manuel déjà archivé : consulter la figure 9 avec les équations de la section 2.3 et les trois 74S163 de la nomenclature.
  - [x] Créer `artifacts/.tmp-archives/chips/Logic/TexasInstruments/SN74xx/SN74S163/sn74s163.pdf` et son extraction texte depuis la fiche TI officielle déjà identifiée ; consulter les priorités de la table de fonctionnement.
  - [x] Créer les images `sn74s163-02.png` et `sn74s163-11.png` dans cette archive pour lire la description et la table/chronogrammes dont le texte PDF est incorporé en images.
  - [x] Créer `src/RetroMediaLab.Chips/Logic/TexasInstruments/SN74xx/SN74S163/Constants/Sn74S163Constants.cs`, `Interfaces/ISn74S163.cs` et `Services/Sn74S163.cs` : état 4 bits local, CLK retenu, CLR et LOAD synchrones prioritaires, entrées A–D et ENP/ENT retenues, retenue combinatoire RCO, notifications de sorties à mutation ; aucun compteur déporté dans la machine.
  - [x] Créer `tests/RetroMediaLab.Chips.Tests/Logic/TexasInstruments/SN74xx/SN74S163/Sn74S163Tests.cs` : vérifier les seize valeurs, priorités, niveaux de CLK inchangés, deux validations ENP/ENT, rebouclage et retenue combinatoire suivant la table TI ; exécuter ce groupe autonome.

Références : Acorn *System 5 Handbook*, CPU board 400,005, section 2.3 et figure 9 ; TI *Synchronous 4-Bit Counters*, SN74S163, https://www.ti.com/lit/ds/symlink/sn74s163.pdf. Le raccordement du générateur sera ajouté avec ses fichiers après réalisation et vérification du compteur. Ne pas déplacer les compteurs de carte dans les puces réceptrices VIA/CRTC : ils appartiennent aux 74S163 réellement présents.

Contrôle actuel du 9 novembre : 11 tests Debug réussis, zéro échec/ignoré, 69 ms. Le bloc de puce est terminé indépendamment du raccordement System 5, qui demeure un travail distinct du circuit matériel.
