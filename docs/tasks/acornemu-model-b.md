# AcornEmu : BBC Micro Model B

- [x] Ajouter le Model B en réutilisant le Model A.
  - [x] Déplacer `Machines/BbcMicro/ModelA/Services/ModelAMachine.cs` et `MemoryExtension.cs` vers `Machines/BbcMicro/Common/Services/BbcMicroMachine.cs` et `MemoryExtension.cs` sous `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu` : conserver le fonctionnement du A, partager la carte et la mémoire ; adapter les références et alias dans les tests existants.
  - [x] Modifier `Common/Machines/BbcMicro/Functions/MemoryFunctions.cs`, `Constants/ModelConstants.cs`, `Constants/ModelAConstants.cs` et les fonctions mémoire AcornEmu, `Common/Machines/BbcMicro/Dictionaries/FirmwareCompatibilityCatalog.cs` : 16/32 Kio pour A, 32 Kio pour B, chronologie PAL commune ; ajouter l’entrée B dans `Emulators/AcornEmu/Dictionaries/ModelCatalog.cs` et ses valeurs initiales/réglages dans `Factories/AcornEmuMachineFactory.cs` ; réutiliser les libellés communs existants dans toutes les langues.
  - [x] Modifier la carte partagée et `Emulators/AcornEmu/Services/Via6522.cs`, créer les constantes et le composant ADC sous `Machines/BbcMicro/Common` : raccorder le second VIA, les registres analogiques et les joysticks du B ; ne pas installer de contrôleur disque, Econet ni coprocesseur par défaut, ces extensions restant disponibles.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuTests.cs` et créer `AcornEmuModelBTests.cs` : enregistrement du B distinct, RAM, second VIA/IRQ, ADC et entrées joystick, maintien du A ; tests autonomes sans ROM externe.
  - [x] Modifier ce suivi avec les résultats groupés des tests Acorn et de la compilation Debug officielle tous modules ; vérifier les fichiers livrés, sans commit.

Référence matérielle : manuel de maintenance Acorn, section 1.2.2 et circuits VIA/ADC. Le Model B fournit 32 Kio et les interfaces utilisateur/imprimante/analogique/Tube/bus 1 MHz ; interface disque et Econet sont des extensions. Le code des périphériques existants du A reste partagé.

Validation groupée : 281 tests AcornEmu réussis, dont les sept cas Model B (profil, RAM et VIA, ADC échantillonné avec IRQ, quatre canaux des deux ports). Les tests A existants gardent leurs vérifications via alias vers la carte déplacée en Common. Aucun texte traduit ajouté : noms et choix mémoire invariants communs existants. Build officiel Debug tous modules réussi (code 0).

Livraison vérifiée : `build/Debug/Retro Media Lab/retromedialab.exe` présent, 24 manifestes et leurs DLL présents, empreinte du module Acorn livré identique à la compilation. Diff sans erreur. Aucune configuration utilisateur modifiée ; aucun commit. Relancer l’application pour voir AcornEmu disponible sur BBC Micro Model B.
Référence ADC : https://raw.githubusercontent.com/mamedev/mame/master/src/devices/machine/upd7002.cpp ; registres, échantillonnage au départ, conversions 4/10 ms et résultat 12 bits.


- [x] Corriger la gestion des choix multiples révélée par l’ajout du B.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Common/Services/Engine.cs` : résoudre les opérations de gestion et le runtime depuis le cœur de la configuration ; conserver tous les adaptateurs compatibles ; remplacer la résolution unique du chemin ancien sans configuration par le choix initial déjà défini dans ConfigurationFunctions.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Modules/AcornEmulationModule.cs` : exposer les surcharges de gestion prenant la configuration pour transmettre le choix au moteur.
  - [x] Modifier `src/RetroMediaLab.Emulation.Apple/Common/Services/Engine.cs` et `src/RetroMediaLab.Emulation.Bandai/Common/Services/Engine.cs` : supprimer la même hypothèse de cardinalité dans leurs anciens appels sans configuration ; conserver les surcharges par configuration déjà présentes.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuModelBTests.cs` : vérifier le choix public entre AcornEmu et b2 et la création du runtime AcornEmu sur une machine offrant plusieurs cœurs, sans démarrer de cœur ni charger de ROM externe.
  - [x] Modifier ce suivi avec les tests groupés et la livraison du nouveau Debug tous modules.

Erreur observée dans le journal utilisateur : Engine.PreferredAdapter appelle SingleOrDefault sur les adaptateurs compatibles ; le contrat de gestion par configuration retombe sur cette méthode faute de surcharge dans le module. Le runtime présentait la même erreur. Aucun émulateur compatible ne doit être retiré de la liste.

Audit des autres modules demandé : leurs surcharges par configuration existent déjà ; seuls Apple et Bandai conservaient le même SingleOrDefault sur la liste compatible dans les anciens appels sans configuration. Les recherches Single sur un identifiant unique ne sont pas des restrictions du nombre de cœurs compatibles.

Correction de sélection validée : 324 tests réussis (AcornEmu et projections clavier communes). Les tests publics gardent AcornEmu et b2 dans la liste et vérifient séparément chaque sélection ; le runtime AcornEmu se prépare sur B sans ROM chargée ni cœur démarré. SDK et App inchangés, aucun émulateur retiré. Nouveau build tous modules terminé avec code 0.

Livraison de la correction : exécutable présent, 24 modules et leurs DLL vérifiés ; empreintes Acorn, Apple et Bandai livrées conformes aux compilations. Aucun répertoire temporaire de test créé, diff sans erreur. Relancer l’application pour décharger les anciens modules. Aucun commit effectué.
