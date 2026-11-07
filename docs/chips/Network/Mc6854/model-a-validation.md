# MC6854 — validation du raccordement Model A, 7 novembre 2026

## Périmètre et conditions

Validation des notifications IRQ et RTS/activité déjà codées, sans nouvelle modification de production. Le 6854 conserve ses phases/fraction d'horloge et ses entrées ; Econet reçoit les niveaux et applique son verrou NMI sans relire les sorties après chaque tick. Les sauvegardes/reprises d'état sont hors périmètre. Le Model A complet n'est pas déclaré terminé par cette validation.

Carte BbcMicroMachine : 32 Kio, MOS 1.20, BASIC II et police SAA5050 externes existants, econetInterface=true, station=42, econetCyclesPerByte=128, expandedIo=false. L'horloge réseau fournie correspond à 125 kbit/s sur la source BBC 2 MHz. Aucun socket, serveur, fichier média ou configuration utilisateur écrit ; aucune sortie audio Windows ni fenêtre RML. RunFrame et DrainAudio sont inclus dans les mesures ; callbacks détachés et entrées relâchées dans finally.

## Tests autonomes et raccordement

- 76 tests MC6854 Debug réussis, 73 ms : filtre Network.Motorola.M6800.Chip6854 hors StateTests, zéro échec/ignoré.
- 27 AcornEmuEconetTests Debug réussis, 681 ms : zéro échec/ignoré.
- Les résultats TRX confirment notamment RTS relâché après le flag final et deux clocks, absence de notifications stables, CTS/DCD indépendants, fronts TxC/RxC, phases DMA, reprise après mark idle, fréquence des bits et reset sans redémarrage arbitraire de l'horloge externe. Le raccordement vérifie le masque NMI réseau, les registres répétés, la fin de RTS/CTS et les options matérielles.
- Les deux groupes ont été exécutés avant les mesures libres ; aucun test ou build concurrent au banc de vitesse.

## Programme réellement exécuté par le 6502

Le contrôle est chargé en RAM à 2000h puis lancé par la commande BASIC CALL &2000. Le CPU attend le niveau de départ en 2E05h ; la mesure commence ensuite. Le CPU répète lui-même 4096 fois les neuf contrôles suivants, avec une attente bornée en cas d'absence de réponse :

1. station FE18h = 42 (cette lecture masque la NMI Econet) ;
2. CTS non accordé avant RTS ;
3. CTS accordé après activation RTS/ClearTransmit ;
4. longueur réellement émise = un octet ;
5. octet réellement émis = A5h ;
6. retour CTS non accordé après le flag de fin et relâchement RTS ;
7. premier octet reçu = 31h ;
8. second octet reçu = 42h ;
9. état FrameValid reçu.

Le CPU programme CR4=1Eh pour les mots de huit bits, utilise LastData/FrameCompleteMode puis ClearReceive. Les attentes font avancer toute la carte. Les 27 tests de raccordement complètent ce banc : la preuve invitée masque la NMI réseau et ne prétend pas exercer son service MOS/NFS.

Le transport de banc observe uniquement les trames réellement émises et en copie la longueur et le premier octet dans 2F00h/2F01h. Le callback NetworkPoll existant injecte une trame 31h/42h à la demande invitée de 2E10h, à la phase de transport existante de 128 cycles. Il ne calcule aucun verdict et ne dessine aucun pixel. Le compteur PASS est en 2E00h ; AA en 2E01h indique le succès du tour, AA en 2E04h le succès de tous les tours, EE l'échec. Le code annule les IRQ CPU pendant le contrôle et les réautorise avant RTS.

Résultat : 4096 trames émises, 4096 reçues ; neuf contrôles réussis à chaque tour ; compteur de tours restant nul, 2E01h=AA et 2E04h=AA. BASIC exécute ensuite IF ?&2E00=9 AND ?&2E04=&AA THEN PRINT ... et affiche réellement MC6854 PASS 9/9. L'hôte ne fabrique pas ce texte.

## Carte complète Debug, vitesse libre

| Scénario | Images | Cycles CPU | Durée hôte | ms/image |
| --- | ---: | ---: | ---: | ---: |
| Horloge réseau absente | 240 | 9 584 640 | 4 240,0665 ms | 17,666944 |
| Horloge 125 kbit/s, repos | 240 | 9 584 639 | 4 323,9170 ms | 18,016321 |
| 4096 échanges invités | 184 | 7 348 224 | 3 533,3919 ms | 19,203217 |

Capacité calculée sous trafic : 52,07 images émulées/s, 2,080 MHz, 103,98 % de la vitesse matérielle. Au repos avec horloge : 55,51 images/s. Le balayage matériel reste environ 50,1 Hz ; les fréquences n'ont pas été modifiées. Le passage de trafic reste sous 19,97 ms/image. Il s'agit d'un seul passage de validation, pas d'une garantie sur toute charge ni d'un gain avant/après : aucun code de production n'a changé dans ce bloc. Les trois scénarios se déroulent successivement à des instants émulés différents (curseur clignotant), leurs empreintes vidéo ne doivent donc pas être déclarées identiques.

Empreintes finales : horloge absente 4ACC050C7E67D12378F615A4A7D5CC22F1576B5340C4968672DBBCB87EAC8045 ; repos horlogé 41586A12423FFC7A93BE177B24BE9E0B4107D3F3DFEDF00C695FA25517D2DA06 ; fin de trafic D7C9070C27AF861690CFB9C539EF31CF14D89AF57F0F795BF0C3D2219C4EB2FD.

## Captures réelles

Deux étapes conservées dans artifacts/TestsChips/Mc6854/BBC Micro Model A/ et artifacts/TestsMachines/Acorn/BBC Micro Model A/ :

- 20261107-040039-mc6854-idle-clock.bmp : carte au repos avec horloge réseau ;
- 20261107-040049-mc6854-guest-verdict.bmp : verdict exécuté par le 6502 puis imprimé par BASIC, inspecté visuellement.

Ce sont les octets du rendu de la carte sans interface, pas des captures de la fenêtre RML. Les images restent hors Git. Aucun réseau physique, serveur NFS, ni autres machines Acorn ne sont validés par ces seuls résultats.

## Programme de contrôle conservé pour reproduction

Octets chargés à 2000h ; tableaux de résultats et protocole du transport décrits ci-dessus. Ce relevé suffit à réinjecter le même contrôle sans conserver un projet de test dépendant des ROM externes.

```text
78D8AD052EF0FBA9008D022EA9108D032E203520AD012EC9AAD015AD022ED003CE032ECE022EAD022E0D032ED0E3A9AA8D042E5860A9008D002EA9008D012EA9008D102EA9008D002FAD18FE29FFC92AF0034C6C21EE002EA9018DA0FEA91E8DA3FEA9008DA0FEADA0FE2910C910F0034C6C21EE002EA9C08DA1FEADA0FE2910C900F0034C6C21EE002EA9A58DA2FEA9188DA1FEA200A000AD002F29FFC901F009E8D0F4C8D0F14C6C21AD002F29FFC901F0034C6C21EE002EAD012F29FFC9A5F0034C6C21EE002EA200A000ADA0FE2910C910F009E8D0F4C8D0F14C6C21ADA0FE2910C910F0034C6C21EE002EA9288DA1FEA9018D102EA200A000ADA1FE2980C980F009E8D0F4C8D0F14C6C21ADA2FE29FFC931F0034C6C21EE002EA200A000ADA1FE2980C980F009E8D0F4C8D0F14C6C21ADA2FE29FFC942F0034C6C21EE002EA200A000ADA1FE2902C902F009E8D0F4C8D0F14C6C21ADA1FE2902C902F0034C6C21EE002EA9AA8D012E60A9EE8D012E60
```

## Firmware effectivement lu

- os12.rom : 16384 octets ; SHA-256 2D9FEA69017864F6962704481829F95FEE08446C8C3A13826D5D4E44000AC9DE.
- basic2.rom : 16384 octets ; SHA-256 45BD55DC0F6F0F8F1FE9E2481DE7DEF206565EEC8F600BA3068B849CA4132079.
- saa5050 : 960 octets ; SHA-256 9706945B02DD0E30823186FF0D73A49C0D98ED573499A057BAB471ADD7EE28FB.

## Nettoyage

Diagnostic EconetValidation.cs et dossier de résultats TRX supprimés après consignation. Native reconstruit avec son Program.cs préexistant : code 0, zéro erreur, avertissement NU1900 de consultation de vulnérabilités NuGet inchangé. Captures utiles conservées hors Git. Aucun RML lancé/fermé et aucun état utilisateur modifié.
