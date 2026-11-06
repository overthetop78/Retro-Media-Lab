# Puces communes — tâches

Chaque puce possède un seul suivi, réutilisé par les machines qui la câblent.

Procédure commune : [puces et machines](../../directives/Emulations/procedure-puces-machines.md). Terminer et vérifier chaque puce avant le câblage final et les mesures de la machine complète. Les sauvegardes d'état sont laissées de côté pour le moment.

## Accès direct

- [Mos65C12 / Mos65C102](Mos65C12/suivi.md)
- [Mos6522](Mos6522/suivi.md)
- [Sn76489](Sn76489/suivi.md)
- [Mc6850](Mc6850/suivi.md)
- [Mos6551](Mos6551/suivi.md)
- [Upd7002](Upd7002/suivi.md)
- [Ns16450](Ns16450/suivi.md)
- [Ins8154](Ins8154/suivi.md)
- [Intel8255](Intel8255/suivi.md)
- [Mc6854](Mc6854/suivi.md)
- [Intel8271](Intel8271/suivi.md)
- [Wd1770](Wd1770/suivi.md)
- [Upd765](Upd765/suivi.md)
- [Crtc6845](Crtc6845/suivi.md)
- [Saa5050](Saa5050/suivi.md)
- [BbcVideoUla](BbcVideoUla/suivi.md)
- [Mc6847](Mc6847/suivi.md)
- [ElectronUla](ElectronUla/suivi.md)
- [Hd146818](Hd146818/suivi.md)
- [BbcSerialUla](BbcSerialUla/suivi.md)
- [AdaptecAcb4000](AdaptecAcb4000/suivi.md)
- [AcornScsi](AcornScsi/suivi.md)

## Suivi antérieur conservé

- [ ] Puces, une par une ; aucun passage à la suivante avant clôture effective.

- [x] Modifier ce fichier après clôture du 6522 pour détailler les actions concrètes de la puce suivante, en conservant toute la file ci-dessous et les preuves requises.

## File conservée, à détailler sans générer de fiches vides

- CPU : variantes MOS 65C12/65C102 du moteur partagé, Intel 80186 et périphériques intégrés, Motorola 6809, ARM2/ARM3/ARM250 du moteur ARM26.
- Vidéo : MC6845, SAA5050, ULA vidéo BBC, MC6847 Atom, vidéo de l’ULA Electron, VIDC et sa FIFO.
- Audio : SN76489 ; sortie sonore intégrée aux ULA/VIDC lors de leur examen complet.
- E/S : 8255, 8154, 6850, 6551, NS16450, uPD7002, IOC, protocole du clavier Archimedes, Tube ULA, configuration Super-I/O 82C711.
- Contrôleurs : Intel 8271, WD1770/1772, uPD765 et signaux des lecteurs, HD63463, interface SCSI Winchester, ATA PIO.
- Mémoire et horloges : MEMC, EEPROM I2C Compact, HD146818, PCF8583.
- Circuits multifonctions et logique spécifique : ULA Electron complète, rafraîchissement Master512 ; compléter cet inventaire si un circuit réellement émulé est encore découvert dans les machines.
- Machines : vérifier les machines Acorn existantes, achever les machines autorisées dans les consignes ; ABC, Communicator, Unix et Pocket Book restent hors du travail demandé pour le moment.
- Médias : parcourir les formats et images de `F:\Retro` ; vérifier lecture/écriture, visualisation des secteurs et explorateur, création de médias vierges formatés/non formatés, cassette/HDD/autres médias concernés. Les traitements de conteneurs restent dans les bibliothèques médias propriétaires.
