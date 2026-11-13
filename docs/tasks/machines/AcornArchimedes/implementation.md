# AcornEmu — Archimedes

## Reprise prioritaire : terminer le premier assemblage avant les cartes suivantes

- [ ] Finaliser la première carte Archimedes enregistrée, puis A3000, avant A5000/ARM250/A540/A4.
  - [ ] Vérifier les performances et les sorties du premier assemblage courant.
    - [ ] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornArchimedesPerformanceDiagnostic.cs` : mesurer un nombre fixe de frames avec ROM réelle, durée émulée, instructions, ticks et empreintes vidéo/audio ; ne créer aucun fichier de résultat ni fenêtre et libérer la carte dans finally.
    - [ ] Modifier `Components/Vidc/Services/ArchimedesVidc*.cs` et `Machines/Archimedes/Common/Services/ArchimedesMachine*.cs` seulement selon les coûts établis, en conservant les événements DMA/IRQ et les sorties ; compléter les tests autonomes existants pour les changements temporels concernés avant le contrôle groupé.
    - [ ] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornArchimedesPerformanceDiagnostic.cs` après comparaison, puis modifier ce suivi avec les mesures et les limites restantes.

Les chemins `Components` et `Machines` sont relatifs à `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu`. Les anciens chemins Services/Constants/Enums des composants, conservés dans l'historique ci-dessous, ont été rangés dans Components par le commit cbaa7056a.

Mesure avant reprise de l'audit CPU/chipset demandée par l'utilisateur : Arthur 1.20, 300 frames, 19,245939 s hôte pour 5,972412 s émulées, 143337891 ticks et 12162013 instructions. Vidéo SHA256 277D556669229C5E1363BFC34BA43760204B7567C597D006D14DB77A50BAB6F9 ; audio 62A764CC8432E372D53EA8A977A8E242A45EE97FA07862693A998F14BD5E5697. Le diagnostic a été supprimé ; aucun fichier de résultat créé. L'optimisation n'est pas déclarée terminée. L'audit global des composants est prioritaire avant la reprise des machines, suivant la dernière demande.

- [ ] Réaliser le bloc Archimedes après le commit Electron cd5243b6d.
  - [ ] Processeur ARM de première génération et bus de la carte.
  - [ ] Contrôleurs et carte Archimedes.
    - [ ] Réduire le coût du rendu VIDC sans changer les événements matériels.
      - [ ] Modifier `AcornEmuArchimedesControllersTests.cs` pour couvrir les changements de temporisation/profondeur en cours d'affichage, puis confronter les empreintes et les cadences du diagnostic après optimisation.
    - [ ] Raccorder les sorties réelles de la carte au contrat commun avant les modèles utilisateur.
      - [ ] Modifier `AcornEmuArchimedesBoardTests.cs` pour vérifier la façade IAcornMachine, le CMOS et l'absence de tape ; reconstruire et exécuter les cas Acorn regroupés après l'ensemble des modifications.
    - [ ] Réaliser les contrôleurs périphériques des cartes suivantes.
      - [ ] Réaliser le contrôleur NEC intégré, sans remplacer le WD1772 des premières cartes.
          Résultat : les cas runtime CPCEMU/IMD ont passé (2 cas, 448 ms) après raccordement du format CPCEMU neutre à l'écrivain existant. Le diagnostic temporaire et ses dossiers ont été supprimés. Les tests autonomes couvrent aussi FM/128 octets ; les limites physiques indiquées ci-dessous restent ouvertes.
        - [ ] Modifier les contrôleurs et leurs contrats de champs physiques pour confronter TC partiel, détection de débit et positions angulaires aux octets réellement enregistrés, y compris gaps et tailles discordantes ; ne pas considérer ces points vérifiés par les seuls secteurs réguliers. Le contrôleur et le câblage doivent être terminés avant d'enregistrer une nouvelle machine.
    - [ ] Terminer le contrôleur ST506 et sa validation utilisateur.
      - [ ] Modifier `Hd63463.Commands.cs`, `Hd63463.Transfers.cs`, `Hd63463.cs` et les constantes/phases associées : compléter les commandes de buffer, Read/Find ID, formatage, ECC, Test, polling, mode PIO/DMA, chevauchement des deux tampons et cadences, après lecture de la fiche constructeur complète. Les extraits constructeur sont maintenant accessibles sans convertisseur local ; aucun modèle utilisateur n'active encore le contrôleur.
    - [ ] Créer `Machines/Archimedes/Common/Services/ArchimedesMachine.cs` et ses partiels Bus/Media/Audio : assemblage des composants et raccordement des médias réellement présents selon la carte ; compléter les contrôleurs distincts des cartes ultérieures avant leur implémentation.
  - [ ] Modèles, variantes et configuration.
    - [ ] Créer `Common/Machines/Archimedes/Constants/ModelConstants.cs`, `Dictionaries/ModelCatalog.cs` et `Dictionaries/AcornArchimedesModel.cs` : familles réellement distinctes, équipements configurables et compatibilité des ROM par emplacement ; confronter les regroupements aux manuels Acorn.
    - [ ] Modifier les catalogues de catégories `Common/Machines/Common/Dictionaries/AcornFirmware.*.cs` et ajouter les ROM réelles nécessaires dans le dossier Firmware Acorn utilisateur, après contrôle des empreintes et sans remplacement d’un fichier différent.
    - [ ] Modifier les enums et catalogues Common existants, la fabrique AcornEmu et le catalogue natif ; créer `Functions/ArchimedesFunctions.cs` et ses partiels pour injecter les réglages du modèle sans chemin spécial dans App.
    - [ ] Modifier `Interfaces/IAcornMachine.cs`, les contrats de profil et le cœur uniquement si requis pour les canaux audio VIDC et les sorties réelles ; préserver tous les modèles précédents.
    - [ ] Ajouter les photographies des modèles Archimedes dans `src/RetroMediaLab.Emulation.Acorn/Assets/Machines`, puis modifier `Common/Machines/Common/Dictionaries/MachineCatalog.cs` et `THIRD-PARTY-NOTICES.md` : chaque modèle intégré possède son image réelle dans les listes et ses crédits.
    - [ ] Modifier les ressources communes et linguistiques nécessaires : invariants partagés, nouveaux textes traduits par Argos dans toutes les langues.
  - [ ] Validation et livraison groupées.
    - [ ] Modifier `Services/CpuArm26.Cache.cs` et le bus `Machines/Archimedes/Common/Services/ArchimedesMachine.Bus.cs` pour confronter les cycles de remplissage, le redémarrage anticipé ARM3 et la synchronisation CPU/MEMC aux horloges de la carte intégrée ; compléter les cas temporels autonomes dans `AcornEmuArm26Tests.cs`.
    - [ ] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuArchimedesTests.cs` : MMU, IOC, vidéo et transferts médias.
    - [ ] Créer puis supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornArchimedesBootDiagnostic.cs` : ROM réelles jusqu’au bureau, BASIC, clavier/souris, disque et son ; conserver les PNG dans `artifacts/acorn-archimedes`, supprimer médias temporaires et libérer dans finally les ressources possédées.
    - [ ] Modifier les fichiers livrés sous `build/Debug` par `scripts/local-building.cmd --building=debug --modules=A` et vérifier les 24 modules ainsi que le lanceur.
    - [ ] Modifier ce suivi avec les résultats et créer le commit du bloc avant Risc PC / A7000.

## Périmètre conservé

### Firmware Archimedes ajouté

Source : archive `bios/Acorn/Archimedes/aa310.zip` du dépôt https://github.com/Abdess/retrobios ; ordre et empreintes des puces confrontés à `src/mame/acorn/aa310.cpp` de MAME. Ces fichiers sont dans le dossier utilisateur Firmware, pas embarqués dans l'assembly.

| Fichier | Octets | SHA1 |
| --- | ---: | --- |
| Acorn Archimedes Arthur 0.30.bin | 524288 | 6aebd686d97dfdf6726fa5f3246ef35b840b286d |
| Acorn Archimedes Arthur 1.20.bin | 524288 | 1181ff9c2c2f3d6d414054ec33b2260404bafc81 |
| Acorn Archimedes RISC OS 2.00.bin | 524288 | b82a78830dac386f9b649b6d32a34f9c6910546d |
| Acorn Archimedes RISC OS 2.01.bin | 524288 | 9ed7dee8553d96a6d58dd23d31682234f57beb62 |
| Acorn Archimedes RISC OS 3.00.bin | 2097152 | b4fa494d51dd704c9da9653c2acc6acd08cc8cf4 |
| Acorn Archimedes RISC OS 3.10.bin | 2097152 | 68533e0a93657a879c1697c04f32d517345b1a61 |
| Acorn Archimedes RISC OS 3.11.bin | 2097152 | 3487729e87bebc9cb51665838c27beff22f7b3bd |

Acorn System et Atom sont committés ; Electron est committé et livré. Archimedes est le bloc courant. Risc PC et A7000 restent ensuite à réaliser. ABC, Communicator, stations Unix et Pocket Book sont exclus par la demande. Aucun périphérique non implémenté ne sera annoncé comme fonctionnel.

## Références déjà trouvées pour les regroupements

- MOS6551 : fiche MOS/Commodore 1982 https://www.retrodocs.fr/wp-content/uploads/pdf/MOS-6551.pdf ; quatre registres, bits de statut/commande/contrôle, horloges 16×, quartz 1,8432 MHz, modem et acquittement IRQ. Confrontation du décodage de carte à `aa310.cpp` et `acorn_ioc.h` de MAME : banque 3, RxC depuis IOC, IRQ sur IL2 / IRQB bit 2.
- CMOS Archimedes : profils de référence `cmos_arthur.bin` SHA1 `f0eae9a535505d82ba3488ddb7895434df940d73`, `cmos_riscos2.bin` `96163285797e0d54017d8d4ae87835328a4658bd`, `cmos_riscos3.bin` `9dab30b4c3305e1142819687889fca334b532679`, archive aa310 citée ci-dessus. Les constantes partagées reconstruisent exactement leurs 240 octets de RAM ; les 16 registres horloge/alarme ne sont pas importés. Affectations et année complète confrontées à https://www.riscos.com/support/developers/prm/memoryman.html ; l'année actuelle est injectée seulement à la création du CMOS, avec ajustement du checksum RISC OS 3. Aucune modification automatique des sauvegardes utilisateur.
- PCF8583 : fiche constructeur NXP https://www.nxp.com/docs/en/data-sheet/PCF8583.pdf ; registres CMOS/calendrier, capture commune au début d'une lecture, bus I²C, alarmes et minuterie. Calendrier, modes de référence externe et compteur avec minuterie/alarme implémentés et vérifiés par transactions I²C.
- WD1770/1772 : manuel constructeur Western Digital 1986 https://www.bitsavers.org/components/westernDigital/_dataBooks/1986_Storage_Management_Products_Handbook.pdf ; cadences WD1772 6/12/2/3 ms. Le câblage Archimedes emploie quatre sélections externes et une commande de moteur par verrou de carte ; ne pas confondre cette commande avec la sortie moteur du contrôleur.
- Architecture ARM/MEMC/VIDC/IOC : https://riscos.com/support/developers/prm/hardware.html .
- MEMC : https://www.bitsavers.org/pdf/acorn/MEMC_Datasheet_Sep86.pdf (protection, séquence ROM de reset et câblage DRAM) ; confronter `src/devices/machine/acorn_memc.cpp` de MAME pour l'annexe des permutations lignes/colonnes. Les lectures ROM sont permises en mode User ; la vérification globale Supervisor du pilote MAME ne doit pas être recopiée.
- VIDC : https://chrisacorns.computinghistory.org.uk/docs/Acorn/Misc/Acorn_VIDC_Datasheet.pdf ; registres 5.1–5.23, données faible bit en premier, curseur 32 pixels 2 bpp, palette 256 couleurs par substitution de bits RGB. Le curseur se réinitialise en flyback avant le début des pixels vidéo, et non à VDSR. Les délais propres aux révisions VIDC1A sont à confronter au démarrage ROM.
- IOC et clavier : https://chrisacorns.computinghistory.org.uk/docs/Acorn/Manuals/Acorn_TechnicalPublishingSystemTRMPt1.pdf, chapitres 5 et 9 : IOC 2 MHz, compteurs latch+1, KART 8N2, acquittement distinct des deux octets des paquets, compteurs souris signés 7 bits saturés/verrouillés ensemble ; aucune écriture directe dans la mémoire RISC OS pour fournir les entrées.
- Instructions et modes ARM26 : https://www.riscos.com/support/developers/armlang/chap03.htm et chap07.htm ; cache ARM3/CP15 : https://www.riscos.com/support/developers/prm/arm3.html ; organisation 4 Kio, 64 voies et lignes de 16 octets : manuel de formation Acorn `AcornTraining_RISCTechModule.pdf`. Le remplissage du cache est actuellement synchrone : l'arrêt/redémarrage anticipé du CPU pendant les quatre lectures reste à confronter aux horloges de la carte lors de l'intégration MEMC.
- Carte A440 : https://chrisacorns.computinghistory.org.uk/docs/Acorn/Manuals/A440_Service_Manual.pdf .
- MEMC1A sur les séries 400/1 : bulletin technique Acorn 1/89, https://chrisacorns.computinghistory.org.uk/docs/Acorn/CSN/Acorn_CSNews6.pdf . La révision mémoire doit réellement être modélisée, pas seulement renommer la machine.
- A5000 Alpha : bulletin Acorn octobre 1993, https://chrisacorns.computinghistory.org.uk/docs/Acorn/CSN/Acorn_CSNews19.pdf ; carte et comparaison photographique https://chrisacorns.computinghistory.org.uk/Computers/A5000.html . ARM3 à 33 MHz au lieu de 25 MHz, MEMC1A sur support permettant une carte à deux MEMC pour 8 Mo ; conserver une configuration A5000 avec les choix matériels appropriés si les cartes comparées confirment ce regroupement.
- Sources d’émulateurs à confronter : https://github.com/mamedev/mame/blob/master/src/mame/acorn/aa310.cpp ; https://github.com/sarah-walker-pcem/arculator . La présence d’une machine dans ces sources ne prouve pas son fonctionnement ; le pilote MAME marque lui-même ses variantes non fonctionnelles.
- A7000+ à examiner dans le bloc suivant : https://chrisacorns.computinghistory.org.uk/docs/Acorn/Brochures/Acorn_AGP247904_Acorn7000%2B.pdf ; ne pas confondre ARM7500 et ARM7500FE avec FPA intégrée.

## Build Debug demandé maintenant

Référence ST506 maintenant accessible par extraits indexés : fiche Hitachi HD63463, pages 418 (paramètres), 422 (DTM), 427 (POFF : bit 15 et offset pair), 428 (résultats), 436 (OPBR/OPBW, MTB/BTM, DONE et temporisations) : https://datasheet4u.com/pdf/1285972/HD63463.pdf . Aucun convertisseur local supplémentaire requis pour ces passages.

ST506, points constructeur déjà retrouvés pour la suite :
- p. 435 : RID/FID remplissent les buffers avant DMA, puis transfèrent 512 octets au maximum ; DONE permet un transfert abrégé. WFM reçoit les identifiants dans les deux buffers avant formatage ; DONE peut arrêter leur collecte et commencer le formatage. Le contenu de DATA est un motif fixe du contrôleur, pas le flux d'identifiants.
- p. 427 : RID/FID/WFM ne changent pas de piste ; SCNT maximal ST506 1024 secteurs (128 × 8 têtes), formatage soft-sector au maximum 128 identifiants. OFFSET saute les premiers identifiants après index. Le codage précis des identifiants et du motif de remplissage reste à relever avant implémentation.
- p. 428 : les résultats normaux de RED n'ont pas les mêmes champs que RD ; corriger cette différence avec la nouvelle commande RID/FID plutôt que copier les paramètres RD.

Fiche complète extraite par pdftotext (Poppler déjà installé). Figure 7, p. 421, inspectée : ID soft-sector = LCA sur 2 octets, LHA et LSA sur 1 octet chacun ; longueur des données pilotée par RL, pas par un octet de taille dans ID. RID/FID : six octets de paramètres US/PHA/0/OFFSET/0/SCNTL (p. 418). Le motif exact de DATA lors de WFM reste à établir ; aucune valeur arbitraire ne sera substituée.

À conserver pour la suite ST506 : erreur RTS = 0x14 pour TST après SPC (table des erreurs ADFS Acorn). TST met les sorties disque en haute impédance en 60 CLK et exige RESET pour en sortir (Hitachi p. 436). Le codage des identifiants est relevé ; le motif DATA de WFM et la conservation des identifiants non standard dans les formats de support doivent encore être établis avant le formatage.

Source Hitachi complète mise en cache de session (`hd63463_datasheet_text`, 290074 caractères). p. 416 : POL reste BSY et POL actifs, prend fin sur recherche terminée ou erreur disque ; POD peut arrêter cette attente. p. 423/425 : cadences STEP low/high suivant SL/SH/PSK ; SL=FF interdit en mode normal. p. 430 : VUL suit les recherches réellement terminées, pas la simple liste CUL.

Recherche ST506 : génération des pas selon SL/SH/PSK et séparation de la fin des impulsions et de la fin mécanique. La latence de fin mécanique conserve le réglage déjà utilisé par le contrôleur ; elle ne provient pas des compteurs SL/SH. POL maintient BSY/POL jusqu’à un événement et POD est accepté pendant cette attente. Les deux temporisations mécaniques évoluent indépendamment de la commande active. Le cache de session constructeur reste disponible pour les commandes restantes ; ne pas retélécharger le PDF pour les mêmes passages.

Contrat média courant : EmulationDiskSector transmet les données, IntegrityValid et un DiagnosticCode spécifique au format, sans octets ECC enregistrés. Ne pas interpréter ce code comme un syndrome HD63463 ni inventer des octets de correction. Les modes BRST et DONE sont désormais séparés et vérifiés ; l’adaptation DACK/DONE explicite prépare les futurs contrôleurs DMA sans changer les appels existants.

Figure 10 Hitachi p. 435 inspectée dans cette passe, puis PDF/PNG supprimés dans finally. Le FIFO utilise désormais exactement deux RAM de 256 octets avec pointeurs disque et hôte séparés. Un champ DATA commencé ne peut plus suspendre indéfiniment l'écriture pour attendre le DMA. Les vérifications antérieures à ce changement qui observaient une écriture immédiate après le dernier DACK ont été corrigées pour attendre la sortie disque. Le build Debug livré pour les photographies précède cette dernière modification du contrôleur ; pas de nouvelle livraison ni commit du bloc Archimedes incomplet.

## Reprise du document utilisateur, 2 novembre 2026

Document relu par le connecteur Google Docs : https://docs.google.com/document/d/1YNQZeVmV0fKFfnvm1RYX4aJaRAqJEn-_mswaLMgl9_E/edit . La liste Archimedes contient A305, A310, A410, A420, A440, A410/1, A420/1, A440/1, A540, A3000, A3010, A3020, A4000, A5000, A5000 Alpha et A4. L'existence et la désignation des A410/A420 sans suffixe doivent être confrontées aux sources matérielles avant toute revendication de prise en charge.

Le nouvel assemblage configurable AcornArchimedes est raccordé au parcours utilisateur existant et au constructeur ARM/MEMC/VIDC/IOC ; ARM2 8 MHz ou ARM3 25 MHz, MEMC1/MEMC1A, RAM, firmware système, disquettes DD et ST506 optionnel. Il ne prouve pas encore toutes les variantes de cette liste, ni les cartes à 82C710/711, SCSI ou matériel portable. Ces variantes et la fidélité/performance restante appartiennent toujours au bloc courant.

Le bloc suivant reste Risc PC 600 / 700 / StrongARM et A7000 / A7000+. Unix et Pocket Book restent exclus, de même qu'ABC et Communicator. Aucun de ces points n'est considéré terminé par le seul démarrage du premier assemblage.
