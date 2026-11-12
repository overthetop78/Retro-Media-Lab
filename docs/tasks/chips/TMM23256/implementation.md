# Toshiba TMM23256P — ROM32K×8

- [x] Réaliser la ROM à capture d’adresse constructeur.
  - [x] Réaliser les broches et la propagation locale.
    - [x] Créer `src/RetroMediaLab.Chips/Memory/Toshiba/TMM23xx/TMM23256/Interfaces/ITmm23256.cs`, `Contracts/Tmm23256State.cs`, `Models/Tmm23256Timing.cs`, `Constants/Tmm23256Constants.cs`, `Services/Tmm23256.cs` :32768octets immuables fournis au composant,15adresses capturées au CE descendant,OE indépendant,sortie pilotée/inconnue/Hi-Z distincte,accès150ns et OE70ns/libération70ns,hold adresse30ns,contraintes CE/cycle et initialisation100µs selon Toshiba1983p255–259 ; événements locaux,snapshot autonome des phases et demandes,identité du contenu ROM et refus atomique d’un état incompatible. Aucun tableau de cartouche lu directement par la machine pour remplacer la puce.
  - [x] Vérifier la ROM complète.
    - [x] Créer `tests/RetroMediaLab.Chips.Tests/Memory/Toshiba/TMM23xx/TMM23256/Tmm23256Tests.cs` :toutes les32768adresses,même adresse maintenue malgré les entrées modifiées sous CE bas,OE sans recapture,délais/pilotage,initialisation par les deux méthodes,contraintes de hold,notifications sans répétition,JSON pendant accès et refus atomiques ; exécuter le groupe avant de raccorder les cartouches.

  - [x] Mesurer le coût du bus complet sans assertions dans la boucle.
    - [x] Compléter `tests/RetroMediaLab.Chips.Tests/Memory/Toshiba/TMM23xx/TMM23256/Tmm23256Tests.cs` :échauffement,mesures répétées de65536transactions CE/OE complètes avec et sans écouteur de sortie,contrôle global des octets et nombres de fronts hors chronométrage ; reporter accès/s et durée, sans en déduire une vitesse machine ni annoncer un gain sans référence. Exécuter le groupe Debug puis sa validation Release.

- [x] Étendre la géométrie du mécanisme ROM partagé avant le raccordement des cartouches.
  - [x] Réaliser les capacités explicitement portées par le profil.
    - [x] Modifier `src/RetroMediaLab.Chips/Memory/Toshiba/TMM23xx/TMM23256/Models/Tmm23256Timing.cs`, `Constants/Tmm23256Constants.cs` et `Services/Tmm23256.cs` : ajouter la capacité8/16/32K et la qualification électrique au profil immuable, conserver32K qualifié pour les profils constructeur existants, valider contenu/adresses/restauration selon cette capacité sans padding, sans second moteur ni changement des délais ; refuser les anciens états sans cette identité de profil. Une géométrie générique ne vaut pas identification d’un boîtierToshiba/NEC/Fujitsu.
  - [x] Vérifier les géométries et la conservation des profils existants.
    - [x] Créer `tests/RetroMediaLab.Chips.Tests/Memory/Common/ParallelRomGeometryTests.cs` : parcourir toutes les adresses8/16/32K dans un profil logique explicitement non caractérisé, vérifier limites, contenu immuable,OE/CE,étatJSON et refus d’une autre capacité/qualification ; vérifier aussi la voie de capture d’adresse et le groupeROM complet existant Debug/Release après ce lot, sans profilage local.

Extension du mécanisme partagé terminée :16contrôlesROM communs/Toshiba/NEC/Fujitsu Debug (291ms), puis16Release (229ms),0échec/ignoré, codes0. Captures `artifacts/TestsChips/ParallelROM/SG-1000/20261112-shared-rom-geometries-debug.trx` et `20261112-shared-rom-geometries-release.trx`. Aucun bancThroughput rejoué. Géométries logiques8/16K non caractérisées distinctes des profils fabricant32K, défauts existants conservés, étatsformat4. Le raccordement des cartouches à ces composants reste une action machine, pas un résultat revendiqué par ces tests de puce.

- [x] Refondre le parcours local des transactions ROM identifié dans le profil SG-1000.
  - [x] Garder un seul moteur et ses profils, sans reconstruire les contrats dans la boucle interne.
    - [x] Créer `src/RetroMediaLab.Chips/Memory/Toshiba/TMM23xx/TMM23256/Services/Tmm23256LocalTiming.cs` et modifier `Services/Tmm23256.cs`, `Interfaces/ITmm23256.cs` : champs locaux des entrées, mot de sortie/cible et dates scalaires, paramètres constants du profil projetés une fois, résolveur unique pour ReceivePins et ReceiveParallelPins ; conserver captures d'adresse, lecture statique, invalidation/hold, startup, délais nuls et positifs, publication et réentrée, signatures/versions/restauration publiques inchangées. Ne résoudre à nouveau en fin de transaction que si une échéance est effectivement à la date courante.
  - [x] Vérifier tous les profils du moteur partagé avant raccordement.
    - [x] Modifier `tests/RetroMediaLab.Chips.Tests/Memory/Common/ParallelRomGeometryTests.cs` : ajouter une comparaison autonome des deux transports avec profils statiques/capturés, délais nuls et positifs, phases CE/OE, restauration et retours de notifications ; réutiliser les groupes Toshiba/NEC/Fujitsu et géométries existants, sans rejouer leurs bancs de débit isolés.
