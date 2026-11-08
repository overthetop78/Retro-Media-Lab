# PCD8572 — revue du composant utilisé par le Master Compact

- [ ] Corriger le composant réel identifié, sans ajouter une horloge étrangère au montage.
  - [x] Créer `artifacts/.tmp-archives/chips/Memory/PCD8572/PCD8572.pdf` depuis la fiche Microchip DS20016E archivée et ses images de pages nécessaires ; conserver les caractéristiques et le protocole constructeur avant modification de `CompactEeprom`.
  - [x] Modifier `src/RetroMediaLab.Chips/Memory/Generic/I2C/EEPROM128/Services/CompactEeprom.cs` : corriger la reconnaissance START/STOP depuis le niveau SDA résolu et conserver SCL/SDA/ACK réellement reçus, selon la référence.
  - [x] Modifier `src/RetroMediaLab.Chips/Memory/Generic/I2C/EEPROM128/Services/CompactEeprom.cs` : déplacer l'incrément du pointeur de lecture de BeginTransmit au front d'ACK maître ; conserver l'adresse sur NACK ou lecture interrompue, conformément à DS20016E page 6.
  - [x] Modifier `src/RetroMediaLab.Chips/Memory/Generic/I2C/EEPROM128/Services/CompactEeprom.cs` : recevoir les trois straps A0–A2 au constructeur, conserver le constructeur sans paramètre et décoder l'adresse physique choisie.
  - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMaster/Common/Constants/CompactNonvolatileMemoryConstants.cs`, modifier `BbcMicroMachine.cs` et `CompactEeprom.cs` : déplacer les 17 octets de configuration Compact hors du composant, initialiser la même mémoire au montage via Import, conserver l'adresse physique zéro.
  - [ ] Réaliser le cycle d'écriture nominal autorisé et son raccordement.
    - [ ] Créer `src/RetroMediaLab.Chips/Memory/Generic/I2C/EEPROM128/Constants/Pcd8572TimingConstants.cs` et `Services/CompactEeprom.Clock.cs`, modifier `Services/CompactEeprom.cs` et `Interfaces/ICompactEeprom.cs` : tampon physique de deux octets, déclenchement STOP, durée nominale configurable de 30 ms par octet, échéance propre et notification ; aucune écriture anticipée dans la mémoire persistante.
    - [ ] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/Common/Services/BbcMicroMachine.cs` : fournir la référence 2 MHz, recevoir les échéances et synchroniser avant les accès VIA à l'EEPROM, sans appeler le composant au repos.
  - [ ] Établir la portée des durées nominale et réelle du montage.
    - [ ] Compléter `docs/chips/Memory/Pcd8572.md` avec les sources et les valeurs du montage R1/C2, la convention nominale explicitement autorisée, les recherches originales et les preuves encore nécessaires pour une durée mesurée.
  - [ ] Vérifier le bloc de puce sans logiciel ni fenêtre externes.
    - [ ] Modifier `tests/RetroMediaLab.Chips.Tests/Memory/Generic/I2C/EEPROM128/EepromTests.cs` : vérifier délai après STOP, limites un/deux octets, lecture/NACK, adresses physiques et notifications d'échéance, adapter le test préexistant à l'écriture différée ; consigner le résultat du passage Debug ciblé.
