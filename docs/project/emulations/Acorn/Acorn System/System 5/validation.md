# System 5 — validation et suivi du raccordement

## Bilan du code retenu — 10 novembre2026

Ce bilan concerne le profil Fast6502A2 MHz, VDU80, RAM2000–DFFF et carte400,004. Les étapes datées ci-dessous restent historiques. **La machine n’est pas clôturée** : cadence Debug insuffisante, vérifications de médias encore à rapprocher de l’ensemble des parcours demandés et build/essai RML du bloc actuel non livrés.

Contrôles existants exécutés successivement sur les sources retenues après correction des faces : **1140 composants réussis en849 ms**, **246 System/disque/audio réussis en16 s**, puis **40 médias réussis en599 ms** ; aucun échec ni ignoré. Le premier groupe inclut aussi des membres de familles logiques non montés sur ce profil et des contrôles existants d’état : son total n’est pas un nombre de tests spécifiques à System5, ni une réalisation de sauvegarde d’état nouvelle. Les tests de médias sont compilés sans reconstruire leurs dépendances. Ces durées sont des durées de tests, pas des débits d’émulation.

| Exigence | Code et preuves examinés | État et limite |
|---|---|---|
| CPU, VIA et contrôle WAIT/READY | Cpu6502, Via6522, SystemCpuClockControl/SN74LS74A ; AcornSystemMachine.Clock.cs et SystemFastCpuBus.cs. Contrôles6502/6522 et WAIT ; le VIA suit PH2 du CPU. | Phases, bus, interruptions et arrêt/reprise couverts par les scénarios existants. Pas de cadence modifiée pour les faire passer. |
| Source24 MHz et divisions | SystemClockGenerator et ses trois SN74S163 ; profils périodiques et contrôles de capture/résolution partagée. | Compteurs/phasages dans Chips ; avances bornées aux frontières raccordées. Les variantes sans gain restent retirées. |
| CRTC et trajet VDU80 | Crtc6845, Vdu80CharacterGenerator, SN74LS374/SN74LS165, MM52116 et Vdu80CursorCircuit ; SystemVideo.Monochrome.cs. | Contrôles champs/parités, curseur, RAM et équivalence scalaire/groupée existants ;16 champs BASIC natifs/isolés, groupés/scalaires identiques octet pour octet. L’alerte de champs partiels issue des aperçus est infirmée ; détails dans le suivi. Profils numériques et RC calculé documentés, sans mesure électrique de carte revendiquée. |
| Carte disquette et connecteur | SystemFloppyBoard et partiels Bus/Ready/Separator/Drive, SystemFloppyDrive ;8271,393,93,4020,4013,00,38,138. | Clocks, READY/index, séparateur, CS/DACK, miroirs, reset/NMI et capteurs couverts par les contrôles et le relevé400,004. Pas de reset à l’éjection sur ce chemin. |
| Faces du lecteur configuré | SystemFunctions.Create transmet les têtes du catalogue à AcornSystemMachine/SystemFloppyDrive ; FAULT/OP0 et deux fenêtres datées. | Correction native vérifiée par la vraie fabrique : volumes1/3 indépendants après fermeture et nouvel invité ; modèle simple face conservé. |
| ROM et logiciel effectif | NOS80/VDU80 externes, chargement BASIC depuis le SSD original ; calcul46 et empreinte du programme. | Scénario réel par les entrées hôtes ; pas d’injection CPU/RAM dans les derniers contrôles natifs. Captures et SHA dans le suivi. |
| Écriture et persistance | SAVE/LOAD, image fermée puis rouverte dans un nouvel invité ; deux faces du DSD vérifiées séparément. | Données exactes et catalogues indépendants. Sources originales en lecture seule et inchangées. |
| Formatage80 pistes | FORM80 officiel, lecteur1, Formatted puis00–4F et Verified ; contrôle du fichier fermé. |800 secteurs, catalogue vide,798 secteursE5 remplaçantA5. Passage simple face effectué avant le dernier correctif de sélection de face ; ne pas le présenter comme un formatage natif double face. |
| Clavier et associations | SystemKeyboard, SystemFunctions.MapInput et AcornEmuMachineFactory ; saisie native des commandes, contrôles de disposition/pavé/Break. | Contrat commun utilisé ; ce profil ne déclare pas d’interface souris/manette. La présente revue ne constitue pas un nouvel essai d’assignation à chaud dans RML. |
| Vidéo et présentation | Signal natif transmis par le cœur, champs et parité conservés ; choix de présentation dans App. |480×250 par champ dans le scénario NOS80 mesuré. Le bandeau au-dessus du view est limité à AcornSystem1 dans les constantes/fonctions App ; contrôle visuel du nouveau build encore à faire. |
| Son | PCM invité séparé des mécaniques ; EmulatorCore.Disks/Audio et mécaniques MediaEngine, contrôles volume/muet et matériel monté. | Pas de bip ou ventilateur inventé. L’utilisateur a entendu le chargement BASIC sur un ancien build ; le formatage natif du10 novembre ne constitue pas une nouvelle écoute physique. |
| Formats et filtres | StorageFunctions/FloppyDriveModels partagés, modèles physiques et ValidateMedia ; AcornDiskFunctions/SDK/MediaEngine. SSD/DSD natifs,40 contrôles DFS/ADFS/JFD/HXCStream. | Lecture/écriture, conversions, exploration/visualisation et plan physique couverts suivant les cas exécutés ; ce groupe n’est pas une capture/écriture de disquette physique ni une preuve de tous les modes de chaque extension annoncée. Six scénarios natifs SSD/DSD/JFD/SCP/HFE/HxC Stream passent après correction DFS double face JFD : export, exploration, descripteur, plan physique, écriture de piste SDK et persistance/retour DFS.41 régressions médias passent. Rendu graphique RML et opérations sur support physique réel restent distincts et non vérifiés par ces scénarios ; détails dans le suivi. |
| Libération | Machine.Dispose : connecteur puis carte ; détachement des images avant leur destruction, flux final publié ; finally des diagnostics. | Tests de pulse relâché et reconnexion ; diagnostics/SSD/DSD possédés supprimés, preuves conservées hors Git. |
| Performance et livraison | Moniteur actuel après les corrections fonctionnelles :148,92039 ms/champ,13,4085 % du nominal,19,968 ms matériels/champ ; retour à la référence après essai du cache fixe retiré. Profil détaillé et essais positifs/négatifs dans le suivi. | Trois fenêtres de20 champs après20 champs de chauffe, sans média ; pas une mesure de lecture/écriture. Performance encore ouverte ; aucun100 % ni gain récent non mesuré annoncé. Build complet et essai utilisateur du bloc actuel encore à livrer. |

Rapports : `artifacts/TestsChips/System5Components/Acorn System 5/20261110-component-gate.trx`, `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-system-disk-gate.trx` et `20261110-media-gate.trx`. Les fichiers de sources et preuves de chaque correction sont détaillés dans [le suivi System5](../../../../../tasks/machines/AcornSystem/system5-validation.md), [la carte400,004](fdc400004.md) et [la VDU80](vdu80.md).

## Montage et défaut établi

6502A E=2 MHz, VIA et VDU80 existants, deux lecteurs 8271. Firmware Acorn System 5 NOS80.bin, SHA1 E588298239B5360B5D1E15D5CD9F7FE2B1693E5D ; caractères Acorn System VDU80.bin, SHA1 04C326A745BA6A78185EBC2ADEECB53A166A6AB3.

Le profil plaçait 48 Kio de DRAM à 0000–BFFF : le catalogue fonctionnait, mais LOAD BASIC à C000 ne pouvait pas conserver les octets. Le manuel System 5, section carte 400,010, §3.1, donne la carte 32 Kio à 2000–7FFF et C000–DFFF ; l'option 16 Kio, §2.2.1 et figure 5(c), occupe 8000–BFFF. Les 48 Kio de cartes sont maintenant à 2000–DFFF ; la RAM CPU 1 Kio reste à 0000–03FF, total 49 Kio, et les trous I/O ne sont pas transformés en RAM. ROM de ce montage E000–FFFF inchangée. Aucun changement de CPU/FDC pour compenser l'erreur de décodage.

Source : [Acorn System 5 Handbook](https://chrisacorns.computinghistory.org.uk/docs/Acorn/Manuals/Acorn_System5Handbook.pdf). PDF et texte archivés sous artifacts/.tmp-archives/machines/Acorn/Acorn System 5/. Les commandes CAT0/CAT1 sont définies dans DOS §2.6 ; le chargement et lancement BASIC par son nom dans §2.9.2.

## Résultats réels

Image F:/Retro/Acorn System/System BASIC.ssd, modèle simple face 80 pistes. Catalogue, LOAD BASIC (empreinte exacte 6571FCA3E86E6DC99C2B56481979FBC24615BFF2), exécution et PRINT 12+34=46 passent. SAVE PROBE écrit 768 octets ; effacement RAM puis LOAD restitue les huit octets attendus. Après déplacement de l'image vers le second lecteur, CAT1 relit au moins 512 octets et montre PROBE. Ces tests utilisent les vrais signaux FDC et le fournisseur disque du SDK ; aucune lecture invitée remplacée par une injection dans la RAM.

Carte complète Debug, 160 champs : 13,391210 ms/champ, 74,676 champs/s sans limite, 2,982255 MHz effectifs (149,113 %). Ce firmware programme un affichage progressif natif 480×250, 50,080128 Hz ; l'application reçoit ce mode, sans entrelacement imposé. Captures vérifiées visuellement : BASIC calcule 46 et catalogue du lecteur 1 avec PROBE.

Preuves natives : artifacts/TestsMachines/Acorn/Acorn System 5/20261108-system5-*.bmp et 20261108-system5-native-debug.log. Essai RML groupé après livraison Debug ; aucun essai utilisateur présenté comme déjà effectué.
Régression finale commune : 46 tests System passent en Debug (1 seconde), dont le nouveau contrôle des liens DRAM et des zones non équipées. Rapport artifacts/TestsMachines/Acorn/AcornSystem/systems-final-debug.trx.
Livraison groupée du 8 novembre : scripts/local-building.cmd --building=debug --modules=A terminé avec code 0. Exécutable build/Debug/Retro Media Lab/retromedialab.exe, Chips dans lib et 24 manifestes/DLL de modules vérifiés. Acorn SHA256 1B5B4CD5E25AA48EDEC7A63C2B641409223319A1FDC7F2E548320B94172E11E2, identique à la sortie win-x64. Essais utilisateur dans RML à effectuer.

## Vérification dans RML Debug du 8 novembre

Essai effectué par l'agent avec l'autorisation de l'utilisateur, dans la livraison corrigée du clavier et du monitor Tape (Acorn SHA256 A578762690DAC8CCA506B0000C344C16FDEF124CAEAFB542A34A18C17A8C13C0). NOS80 et VDU80 sélectionnés ; deux lecteurs ss80 à 100 %. L'image de référence `F:/Retro/Acorn System/System BASIC.ssd` est seulement lue.

CAT donne le catalogue du lecteur 0 ; BASIC charge et exécute le programme sur disque. PRINT 12+34 donne 46. Les touches physiques du pavé 7, multiplication, 6 et Entrée étendue donnent 42. Une première invocation incorrecte du helper avait saisi les caractères 13 au lieu d'envoyer Entrée ; l'Error 29 visible dans l'historique vient de cette saisie de diagnostic, corrigée avant le contrôle réussi, et ne constitue pas un défaut établi de la machine.

Après éjection du lecteur 0 et montage de la même image sur le lecteur 1, sans redémarrage, *CAT1 affiche bien le catalogue « drive 1 ». Le signal progressif natif 480×250 est présenté à 50,0–50,1 FPS, 50,1 Hz et 2,00 MHz / 100 %. Les valeurs limitées de RML ne remplacent pas la mesure libre native ci-dessus.

Captures : `artifacts/TestsMachines/Acorn/Acorn System 5/20261108-rml-basic-keypad.png` et `20261108-rml-second-drive-catalog.png`. Le contrôle des sons et des directives demandé ensuite reste un bloc distinct à terminer pour la famille.

## Contrôle commun Audio et orchestration, System 1–5

Le 8 novembre, 82 tests System/Audio réussissent en Debug, dont dix nouveaux cas autonomes sans ROM externe. Le mélangeur est contrôlé sur System 1, 2, 3/6502, 3/6809, 4 et 5 : seuls les lecteurs raccordés apparaissent, avec volume initial 10. Les commandes volume et muet sont transmises séparément aux deux mécaniques des System 3–5, sans réinitialisation de machine ni reconfiguration pendant 1 000 clocks internes. Deux tests existants du volume/muet Tape et HDD communs passent également, avec PCM invité inchangé. Ces contrôles valident le raccordement et les commandes, pas une écoute humaine de tous les profils.

Le chemin lecteur est déjà commun : `EmulatorCore.Initialize` branche les notifications d'activité Tape et crée les mécaniques avec `hostAudio: true`. `EmulatorCore.TapeSound.cs` transmet les changements d'opération ; `EmulatorCore.Disks.cs` confie les mécaniques aux images empruntées par le contrôleur. `Intel8271Drive.Synchronize` transmet moteur, accès et déplacements seulement après un changement des sorties. `MediaRuntimePreparationService.AudioAvailable` rejoint `App/Services/Audio/MediaDriveAudioController`, dont la lecture utilise le temps hôte, indépendamment du PCM et des clocks invités. Les dix fichiers FLAC contrôlés du magnétophone et du lecteur 5,25 pouces sont présents dans la livraison Debug. Le profil magnétophone réel disponible est de référence commerciale inconnue ; le profil de disquette enregistré est celui du lecteur BBC 5,25 pouces disponible, avec replis communs par opération. Ne pas les présenter comme des enregistrements spécifiques aux boîtiers System.

Vérification des directives sur le montage actuel : compteurs/phases conservés dans CPU, VIA, contrôleur Tape, 8271 et vidéo ; IRQ/NMI raccordées par notifications au CPU, échantillonnées selon ses phases ; changement de comparateur Tape notifié à l'interface, prochaine échéance conservée dans la carte ; horloges VIA/VDU avancées aux cadences câblées. Les clocks de balayage et les lectures RAM vidéo utiles restent des traitements matériels. La VDU publie le mode progressif ou la parité du champ, avec dimensions natives, sans mise à l'échelle hôte dans le cœur. Les mécaniques des lecteurs utilisent leurs événements hôtes, sans sondage sonore ajouté au tick de carte. Aucun thread par puce ni changement de fréquence ajouté lors de ce contrôle.

Aucun manque de raccordement sonore des lecteurs n'était établi par ces contrôles ; aucun bruit fictif ajouté. Les profils de matériel ne déclarent pas de PSG System. L'utilisateur précise ensuite : son de machine signifie les bips électroniques réellement produits ; sans buzzer, aucun son, et aucun bruit de ventilation ou de boîtier. Les montages standard contrôlés ne comportent pas de buzzer/haut-parleur intégré à raccorder. Le [manuel utilisateur System 1](https://speleotrove.com/acorn/acornUserManual.pdf), page PDF 16, explique que son programme métronome exige un haut-parleur externe sur PA6/sortie bande et masse, éventuellement avec circuit additionnel : cette possibilité ne justifie pas d'ajouter un bip de démarrage au montage standard. PDF archivé sous `artifacts/.tmp-archives/machines/Acorn/Acorn System 1/System1-UsersManual.pdf`. Les rapports sont conservés dans `artifacts/TestsMachines/Acorn/AcornSystem/system-audio-audit-debug.trx` et `system-audio-common-options-debug.trx`.

### Lecture disquette inaudible signalée après ces contrôles

L'utilisateur n'a pas entendu de bruit de lecture dans RML. Ce constat reste distinct des tests de configuration : un diagnostic temporaire reprend alors sa configuration System 5, le firmware réel et la disquette en lecture seule, saisit CAT par le clavier invité et utilise le vrai `MediaDriveAudioController` d'App. Résultat vers une sortie mémoire : 512 octets physiquement lus, 7 notifications, 99 blocs audio et crête 621/32767 au volume 10 ; aucune erreur, sortie libérée. Le même parcours délégué à `WasapiAudioOutput` réel remet 101 blocs, dont 85 113 trames déjà consommées par Windows lors du relevé ; même crête, aucune erreur et sortie libérée. Cela prouve la transmission au périphérique Windows dans ce processus de diagnostic ; cela ne remplace pas la confirmation d'écoute dans RML. Journaux `20261108-mechanical-memory-probe.log`, `20261108-mechanical-host-probe.log` et rapports `system-mechanical-*-probe-debug.trx` dans le dossier de preuves System 5. Aucun volume utilisateur augmenté et aucun correctif de production inventé pour masquer une cause non établie.

### Formatage invité du lecteur 1 — 8 novembre

L'utilisateur confirme avoir entendu le lecteur pendant le chargement de BASIC et demande une opération plus longue, puis explicitement le formatage d'une disquette vierge dans le lecteur 1. Création et montage de `F:/Retro/Acorn System/System-format-test.ssd` (204 800 octets, simple face 80 pistes) par le dialogue RML. `DOS Utilities.ssd`, téléchargé depuis la page DOS de Chris Oddy, est chargé dans Floppy0 ; `DRIVE1`, puis `FORM80`, affichent la confirmation du lecteur 1. Après YES, la première tentative reste en attente ; réinsertion puis extinction/allumage, et nouvelle exécution. Le programme termine effectivement : **Formatted**, vérification des pistes **00–4F**, puis **Verified**. `CAT1` affiche ensuite le catalogue vide du lecteur 1. Le contrôle établit le formatage complet de cette seconde exécution, sans établir la cause de l'attente initiale. Le diagnostic temporaire préparé entre-temps ne s'est pas exécuté (erreur de compilation), puis a été supprimé ; aucun correctif matériel ni résultat de diagnostic ne lui est attribué. Captures `20261108-rml-format-drive1-verified.jpg` et `20261108-rml-format-drive1-catalog.jpg`, dans le dossier de preuves System 5. Retour sonore sur cette opération longue encore à recueillir ; le volume reste 10.

Après fermeture effective de RML, lecture indépendante du SSD : 204 800 octets, zéro fichier, capacité 800 secteurs, les 798 secteurs de données intégralement remplis de E5. SHA-256 `2490FE6CFDAF23C74C88FF3CD912EC2B7DFEC9B553B7702F1514898ACD941F85`. Le rapport `20261108-format-persistence.json` confirme ces contrôles et l'absence de processus RML restant. Cette preuve porte sur le fichier sauvegardé, pas seulement sur le cache invité.


## Reprise du générateur physique — 9 novembre 2026

La carte 400,005 emploie une source 24 MHz et trois 74S163 IC2/IC3/IC4 pour les sorties 12/8/6/4/3/2/1 MHz. Le circuit SystemClockGenerator est réalisé dans Chips avec les trois instances et les liaisons du §2.3/figure 9 ; les bascules restent dans les compteurs. Ses neuf contrôles et les 12 du compteur passent en Debug (21 réussites, zéro échec/ignoré, 58 ms). Le 8 MHz conserve son vrai rapport cyclique d'un tiers. Référence et fonctionnement détaillés dans `docs/chips/Controllers/SystemClock.md`.

Ce réseau est en amont des circuits WAIT/READY IC5 (74LS74) et de la sélection LK1/LK2/LK3. Il n'est pas encore raccordé aux récepteurs de la machine ; le calcul de phase VIA du module n'est donc pas encore supprimé. Les mesures antérieures ci-dessus ne sont pas celles de ce nouveau code. Les circuits requis doivent être terminés et raccordés avant les nouveaux essais System 5.


### IC5 WAIT/READY : câblage confirmé, 9 novembre 2026

Figure 21, page imprimée 23 = page PDF 62 (et non 64), rendue et inspectée dans System5-cpu-schematic-62.png. IC5 est un 74LS74 : PRE1/CLR1 et CLR2 sont reliés à +5 V, PRE2 via R8 à +5 V. D2/pin12 reçoit MR ; CLK2/pin11 reçoit l'horloge sélectionnée LK1/LK2/LK3 ; /Q2/pin8 est combiné par OR IC17 avec cette horloge pour former PH0. CLK1/pin3 reçoit PH0 ; D1/pin2 vaut NBRq OU NON(R/W), d'après IC1/IC18. Q1/pin5 pilote RDY et NBGt ; /Q1/pin6 inhibe les buffers adresse/commande lorsque le bus est concédé. MR haut ne force donc pas une sortie asynchrone : le prochain front montant de CLK2 capture son nouveau niveau, puis PH0 recommence à descendre avec la source. Aucun preset/clear inventé pour accélérer la reprise.

La fiche constructeur TI SDLS119, page 1, confirme deux bascules D sur front montant avec PR/CLR asynchrones actifs bas. Elle est archivée sous artifacts/.tmp-archives/chips/Logic/TexasInstruments/SN74xx/SN74LS74A/. L'état PR=CLR=bas et sa sortie simultanée ne doivent pas être présentés comme un état complémentaire stable garanti.

### Mesure du code actuel, après réalisation des composants — 9 novembre

La double bascule est réalisée et ses 11 tests passent (59 ms). Son débit isolé avec les deux sorties changeantes et observées atteint 11,761373 / 12,053621 / 12,000576 millions de fronts partagés par seconde en Debug. Le générateur actuel, avec ses trois compteurs et leurs liaisons internes mais sans récepteurs externes, atteint seulement 4,765839 / 4,729069 / 4,459434 millions de périodes source par seconde, alors que sa source physique fonctionne à 24 MHz. Le chemin unitaire doit être optimisé avant validation de cadence ; aucune suppression de fonction ou de front matériel n'est autorisée.

Ces mesures portent sur les composants actuels, après 20 000 périodes de chauffe et trois passages de 2 400 000 périodes chacun, hors RML et sans compilation/test concurrent. Détails dans les fiches `docs/chips/Logic/Sn74Ls74A.md` et `docs/chips/Controllers/SystemClock.md`. Le générateur et WAIT/READY ne sont pas encore raccordés à la machine : aucun nouveau pourcentage System 5 complet n'est déduit de ces mesures, et le relevé du 8 novembre ne valide pas les modifications présentes. Le banc temporaire a été supprimé, l'assembly régénérée avec zéro erreur.

Optimisation suivante du générateur : seules les résolutions sans effet au front descendant sont retirées, CLK bas est conservé dans les trois compteurs. 22 contrôles fonctionnels passent. Débit actuel sans récepteurs 5,162042–5,374378 millions de périodes/s ; avec notifications 1/2 MHz, 4,554104–5,255364. Mesures comparables détaillées dans la fiche du générateur, baisse moyenne de durée hôte 10,5 % / 11,7 %. La cadence source 24 MHz n'est toujours pas tenue ; le raccordement complet et sa mesure restent à réaliser.

Circuit WAIT/READY réalisé dans Chips : `SystemCpuClockControl` raccorde IC5 et ses portes suivant §2.4/2.5/figure 21. Neuf contrôles actuels passent, dont le générateur libre qui continue pendant WAIT/DMA. Débit isolé du circuit après sélection : 7,590152 / 7,708966 / 7,757862 millions de périodes/s en Debug, sortie PH0 observée ; ce relevé n'inclut pas le générateur ni le CPU. Référence, fils, captures et conditions dans `docs/chips/Controllers/SystemCpuClock.md`. Raccordement au CPU et à la machine à poursuivre avant validation complète.

6502 NMOS complété pour recevoir PH0 par niveaux : les phases, la présentation du bus et la capture restent dans la puce. Le banc de fils générateur/WAIT/READY/CPU prouve l'écriture suspendue puis unique et la concession sans transaction mémoire externe. 659 contrôles CPU passent après consolidation des deux états initiaux des 256 opcodes et correction d'une ancienne broche DBE fictive sur le 6502 (la source Synertek la réserve au SY6512 distinct ; le GTE65SC12 conserve sa propre broche réelle). Mesure finale actuelle : 7,128858–7,285448 millions de cycles/s par fronts PH0 et 12,330463–12,837300 par Clock, CPU et RAM seuls. Références, scénarios et limites dans `docs/chips/CPU/Mos6502.md`. Aucun nouveau relevé de machine entière n'est déduit de ces chiffres. Les autres relevés de machines partageant le CPU précèdent cette dernière modification et devront être confirmés au contrôle assemblé final.

### Fil PH2 du VIA confirmé — 9 novembre

Le détail `System5-cpu-via-clock-62.png` extrait de la figure 21 montre IC7 = 6522A, pin 25 PH2 reliée au PH2 du bus issu du CPU 6502A et du buffer IC15. Ce fil ne vient pas de la sortie 1 MHz du générateur. Sur le profil 2 MHz, le VIA reçoit donc les phases du CPU à 2 MHz et partage son étirement WAIT ; le diviseur logiciel `_viaPhase` à 1 MHz dans la carte était incorrect. La vidéo VDU80 peut recevoir les sorties libres 2/12 MHz disponibles sur le bus (manuel carte 400,019 §2.3, LK4–LK8), distinctes de PH2 CPU ; ces clocks ne doivent pas être arrêtées par WAIT. Pour les cartes standard/6809 à 1 MHz, le tick VIA existant correspond déjà à leur période CPU : aucun diviseur de carte n'est requis.

### Montage complet actuel, avant optimisation des accesseurs internes

NOS80/VDU80 identiques aux empreintes du montage du 8 novembre, profil construit par SystemFunctions.Create ; image `F:/Retro/Acorn System/System BASIC.ssd` empruntée en lecture seule via le SDK. CAT lit le catalogue, BASIC charge exactement les 4096 octets attendus (SHA1 6571FCA3E86E6DC99C2B56481979FBC24615BFF2), PRINT 12+34 donne 46. 4608 octets physiquement lus au total. Image source inchangée, SHA1 vérifié avant/après ; média détaché et fermé dans finally. Capture réelle inspectée : `20261109-system5-clock-wiring-current.bmp`, dossier TestsMachines System 5 ; copie TestsChips/Mos6502/Acorn System 5/20261109-clock-wiring-current.bmp. Signal progressif 480×250, 50,080128 Hz.

Trois fenêtres isolées Debug net10.0 de 40 champs, vidéo et drainage audio inclus : 124,251445 / 122,204553 / 121,527582 ms/champ ; 8,048196 / 8,183001 / 8,228585 champs/s libres ; 0,321413 / 0,326796 / 0,328617 MHz CPU effectifs ; 16,070638 / 16,339817 / 16,430838 % de cadence réelle. Chaque fenêtre compte 19 169 280 clocks source et 1 597 440 cycles CPU, ratio 12 exact. Ces valeurs mesurent ce câblage physique actuel, pas le montage simplifié du 8 novembre. La cadence Debug n'est pas tenue : le coût du générateur 24 MHz reste à réduire sans retirer les clocks matérielles. Le contrôle natif n'ouvre aucune fenêtre RML et n'est pas une mesure des FPS présentés par l'application.

Après accès internes directs du SN74S163 : 97,755810 / 96,275580 / 99,340815 ms/champ ; 10,229571 / 10,386850 / 10,066356 champs/s libres ; 0,408528 / 0,414809 / 0,402010 MHz CPU effectifs ; 20,426407 / 20,740462 / 20,100499 %. Mêmes comptes source/CPU et lectures disque, BASIC et 46 vérifiés. Capture `20261109-system5-clock-wiring-counter-fields.bmp` identique à la référence, SHA256 commun 53D97862D9313589B6DDF4D6F149F094038FE64A8FF6C3C8F17F0D5485613A89. Seul le code C# des accesseurs internes du compteur a changé ; baisse moyenne d'environ 20,3 % de durée hôte. La cadence Debug reste insuffisante.

### Générateur consolidé et persistance disque actuels

Après l'optimisation du générateur, trois fenêtres identiques donnent 84,661892 / 84,751965 / 86,018600 ms/champ, 11,811690 / 11,799136 / 11,625393 champs/s libres, 0,471712 / 0,471210 / 0,464272 MHz CPU effectifs, 23,585582 / 23,560516 / 23,213584 % réels. Chaque fenêtre conserve 19 169 280 clocks source et 1 597 440 cycles CPU ; fréquence vidéo matérielle 50,080128 Hz. Capture source-fields identique aux deux précédentes. Le gain cumulé depuis le premier montage physique est environ 30,6 % de durée hôte, sans changement de clocks ni de fonctions. La cadence Debug n'est toujours pas tenue ; les chemins de garde/appel par période du générateur restent à examiner avant de clore la recherche de coûts logiciels.

Le passage d'écriture utilise exclusivement une copie temporaire créée par FileMode.CreateNew sous `F:/Retro/Acorn System/RML-System5-20261109-write-probe.ssd`, 204 800 octets. L'image BASIC d'origine reste ouverte en lecture seule. Par clavier invité, `*SAVE PROBE 6000 6008` sauvegarde les huit octets 5A00FF0102807FA5 ; le 8271 émulé écrit 768 octets, catalogue et données compris. Après effacement RAM, `*LOAD PROBE` restitue les huit valeurs. Après FlushAsync, détachement, fermeture et réouverture du fichier par le SDK en lecture seule, un second effacement puis LOAD restitue de nouveau les huit octets ; le motif est aussi présent dans le SSD sauvegardé. `*CAT` affiche PROBE. Total lu par le contrôleur dans ce passage : 5120 octets.

L'image d'origine conserve exactement son SHA1 initial. La copie possédée est détachée, fermée et supprimée dans finally ; absence du fichier et de tout compagnon portant son nom vérifiée après le test. Capture native du catalogue/SAVE/LOAD inspectée : `20261109-system5-clock-wiring-write.bmp` dans TestsMachines System 5, copie TestsChips/Mos6502. Ce passage ne répète pas les mesures de débit : il vérifie la persistance réelle du fichier, au-delà du cache. Régression finale du câblage actuel : **177 contrôles System, 8271 module et audio System réussis en Debug, zéro échec/ignoré, 5 s**, hors états. Aucun essai RML ni livraison nouvelle n'est déduit de ces contrôles hors interface.


### Mesure du code actuel après les corrections communes — 9 novembre 2026

Code de production au commit 2480c530d, incluant les corrections PH0 du 6502, les champs canoniques des compteurs et le câblage physique System 5. Mesure hors RML, compilation Debug normale, aucune compilation ni autre test concurrent. Scénario : vrai moniteur ROM du profil, sans média monté ni commande saisie ; 40 appels RunFrame de chauffe puis trois fenêtres de 80 appels, rendu vidéo et DrainAudio inclus. Ces résultats remplacent les anciennes valeurs pour décrire ce scénario sur le code actuel ; ils ne constituent pas une comparaison de performance avec les anciens essais exécutant BASIC ou un programme invité.

| Carte | ms par présentation, fenêtres 1 / 2 / 3 | Vitesse matérielle en %, fenêtres 1 / 2 / 3 |
| --- | --- | --- |
| 6502A | 89,943281 / 87,672650 / 88,595791 | 22,200658 / 22,775632 / 22,538317 |

Pourcentage = temps de référence réellement écoulé dans la carte / durée hôte × 100, sans limiteur. Fréquence nominale 2 MHz ; les compteurs CPU avancent du même nombre de cycles que la référence dans chaque fenêtre. 3 194 880 cycles par fenêtre ; la machine reste loin de 100 %, optimisation du générateur encore ouverte. Le compteur System 1 présente son afficheur, les autres cartes leur vidéo ; un appel de présentation n'est pas assimilé à un front d'horloge de puce. Pas de mesure Release dans ce passage. Ce contrôle de débit ne remplace pas les validations fonctionnelles déjà consignées.

Diagnostic temporaire SystemCurrentPerformanceProbe réussi ; entrées relâchées, disques détachés, bande éjectée et audio drainé dans finally. La source temporaire est retirée après consignation, puis l'assembly de tests est régénérée. Aucun fichier média, fenêtre ni processus applicatif créé.

### Factorisation des opérations de fronts, gain actuel — 9 novembre 2026

Les SN74S163 et le générateur utilisent les mêmes opérations canoniques pour les niveaux explicites et les périodes complètes. Les préconditions publiques restent vérifiées ; aucun front matériel, état, compteur, phase ou notification raccordée n'est supprimé. 34 contrôles fonctionnels compteur/générateur/WAIT-READY passent, ainsi que 177 contrôles System/disque/audio.

Mesure actuelle Debug hors RML au même NOS80, sans média ni commande, 40 présentations de chauffe puis trois fenêtres de 80, vidéo/audio inclus :

| Fenêtre | ms/présentation | présentations/s libres | MHz CPU effectifs | Vitesse matérielle |
| --- | ---: | ---: | ---: | ---: |
| 1 | 76,259704 | 13,113085 | 0,523684 | 26,184209 % |
| 2 | 75,102638 | 13,315112 | 0,531752 | 26,587615 % |
| 3 | 76,012375 | 13,155753 | 0,525388 | 26,269407 % |

Chacune compte 38 338 560 clocks source, 3 194 880 cycles CPU et 50,080128 Hz matériels, ratio source/CPU exactement 12. Référence identique avant les deux factorisations : 89,943281 / 87,672650 / 88,595791 ms par présentation ; baisse moyenne de durée hôte d'environ **14,6 %**. La mesure précédente à BASIC n'est pas utilisée pour cette comparaison. Le réseau isolé avec sorties 1/2 MHz observées atteint 9,246456–9,415594 millions de périodes/s contre 24 MHz requis : son coût de traitement reste limitant. Détails avant/après dans la fiche SystemClock. La revue des coûts reste ouverte, aucune clôture de machine ou prétention de cadence nominale.

Vérification logicielle après la mesure, même instance : disque BASIC ouvert en lecture seule via EmulationSdk, CAT puis BASIC, 4 608 octets lus ; programme C000 de 4 096 octets exactement conforme, SHA1 6571FCA3E86E6DC99C2B56481979FBC24615BFF2 ; PRINT 12+34 affiche 46. Capture réelle inspectée `artifacts/TestsMachines/Acorn/Acorn System 5/20261109-system5-canonical-edges.bmp`, copie `artifacts/TestsChips/Mos6502/Acorn System 5/20261109-canonical-edges.bmp` : même SHA256 53D97862D9313589B6DDF4D6F149F094038FE64A8FF6C3C8F17F0D5485613A89 que les captures précédentes, sans retouche. Source/moniteur et périphériques conservent leur activité. Disque détaché et libéré dans finally, entrées relâchées, bande éjectée. Diagnostics temporaires supprimés après consignation et assemblies régénérées sans eux ; captures finales conservées hors Git. Pas de build de livraison ni de comparaison Release dans ce bloc.

### Omission identifiée dans la VDU80 — 9 novembre 2026

La revue des destinataires du générateur a confronté `SystemVideo.DrawMonochromeCell` au manuel archivé, carte 400,019 §2.5.1–2.5.3, figures 14–15 et nomenclature page imprimée 15. Le code dessine les six points directement sur l'horloge caractère ; IC12 = 74LS374 et IC14 = 74LS165 ne sont pas représentés, et l'horloge série 12 MHz du générateur ne les rejoint pas. Les images et calculs corrects des passages précédents ne prouvent donc pas ce trajet de capture/sérialisation matériel. Cette omission empêche la clôture System 5 et doit être corrigée avant de poursuivre ailleurs.

Première correction : composant complet SN74LS374 dans Chips, encore non raccordé à la carte, 10 contrôles permanents réussis. Débit isolé Debug avec données alternées et récepteur : 19,231694–19,926650 millions de périodes/s, au-delà des 2 MHz requis. Le registre conserve localement horloge, fronts, capture et pilotage trois états. La suite est le 74LS165, puis le câblage VDU80 aux deux horloges et la vérification actuelle de l'écran, des logiciels et de la cadence. Le choix précis des phases et des fils sera pris dans le schéma existant, pas déduit d'une image qui semble fonctionner.

Second composant réalisé : SN74LS165 historique, référence TI DL-S 7611375 d'octobre 1976 confrontée à la carte et au schéma PDF 470 du livre TI 1981. Douze contrôles permanents réussissent ; charge directe, porte CLK/INH/SH-LD, huit étages et inconnues, sorties complémentaires et cascade sont conservés. Après retrait des sous-appels de période redondants, débit isolé Debug 15,469579–15,889751 millions de périodes/s avec données alternées, au-delà des 12 MHz requis. Les deux composants VDU80 sont maintenant vérifiés séparément ; leur raccordement réel et sa validation restent à effectuer, sans réutiliser l'ancienne image correcte comme preuve de ces nouveaux circuits.

### Prérequis de notifications vidéo vérifié — 9 novembre 2026

La figure 18 complète et les chronogrammes utiles sont extraits et inspectés ; phases et fils établis dans `vdu80.md`. Le CRTC conservait MA/DE/CU sans notifications, ce qui aurait imposé leur sondage au nouvel étage vidéo. Correction au composant : trois notifications au changement après résolution des sorties, y compris registre, reset et mode test ; aucun changement des niveaux ou du balayage. Les 88 contrôles CRTC passent, trace MA/DE/CU égale à l'ancienne DLL. Coût isolé avec trois destinataires : 15,288813–15,815253 millions de caractères/s en Debug, au-delà des 2 MHz requis. La mesure est celle du CRTC seul, pas un nouveau résultat System 5. Banc temporaire retiré ; câblage VDU80 encore en cours.

### Propagation de la ROM de caractères — 9 novembre 2026

La figure 15 ne permet pas de charger instantanément le nouveau motif pendant NL bas. La source National détaillée identifie des accès par adresse et par sélection distincts, et un délai de coupure des drivers. Le MM52116 est donc réalisé comme composant numérique paramétrable, non encore raccordé : trois durées obligatoires, sélection active programmée sur trois broches, 2048×8, sorties trois états et échéances locales. Neuf contrôles dédiés réussissent ; groupe ROM/logique/générateur/CRTC 164 réussites. Débit isolé avec observateur : 5,433495–5,538212 millions de transactions/s en Debug, pour des transactions synthétiques espacées de 500 ns. Les plafonds 450/120/100 ns utilisés dans le banc ne sont pas attribués à un exemplaire réel. Profil temporel de carte demandé à l'utilisateur et encore en attente ; aucun nouveau résultat de machine assemblée ou de vidéo corrigée annoncé.

### VDU80 raccordée et logiciel réel — 9 novembre 2026, passage 09:42–09:46

Les composants SN74LS374, MM52116 et SN74LS165 ainsi que le RC du curseur sont désormais raccordés à la carte. MA physique, RA, DE et CU sont reçus par notifications ; les clocks 2/12 MHz proviennent du réseau 24 MHz. La RAM notifie une modification de la donnée actuellement adressée. Le module capture les dots réellement sérialisés et leur cadre natif 480×250 ; le dessin direct des glyphes est supprimé, ainsi que ses huit constantes inutilisées. Les données inconnues restent inconnues dans les composants, le rendu seul choisit un pixel noir pour une valeur non résolue. Champs et parité restent ceux du CRTC, sans assemblage artificiel. Le profil RC calculé autorisé et ses limites sont décrits dans Vdu80CharacterGenerator.md.

Contrôles autonomes : 44 réussites RC/VDU80/registre/ROM (79 ms), puis 185 contrôles System/8271/audio/volumes hors états différés (7 s après nettoyage). Les cas natifs 80 colonnes vérifient bords, rasters, écritures RAM, XOR/inversion, phases CU calculées, champs et clocks libres pendant reset CPU. Le module compile sans erreur ni avertissement ; quatre avertissements préexistants restent dans le projet de contrôles.

Essai natif sans RML : NOS80 et caractères VDU80 externes déjà identifiés, 40 champs de chauffe, puis trois fenêtres de 80 champs, rendu et drainage audio inclus. Aucun autre test/compilation concurrent.

| Fenêtre | ms/champ | Champs/s sans limite | CPU MHz émulés | Vitesse nominale | Périodes source | Cycles CPU |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 116,049232 | 8,617032 | 0,344130 | 17,206490 % | 38 338 560 | 3 194 880 |
| 2 | 115,806415 | 8,635100 | 0,344851 | 17,242568 % | 38 338 560 | 3 194 880 |
| 3 | 116,009844 | 8,619958 | 0,344247 | 17,212332 % | 38 338 560 | 3 194 880 |

Cadence vidéo matérielle 50,080128 Hz, ratio source/CPU exactement 12, CPU nominal 2 MHz. Le passage antérieur autour de 26 % utilisait encore le dessin direct des caractères : il ne mesure pas le coût de cette chaîne complète et n'établit pas son fonctionnement. Les gains isolés SN74LS165 et générateur restent ceux de leurs propres scénarios ; ils ne permettent pas de présenter une amélioration du débit global par rapport à cette ancienne chaîne.

Vérification du logiciel : CAT puis BASIC depuis `F:/Retro/Acorn System/System BASIC.ssd` ouvert en lecture seule via EmulationSdk ; 4 608 octets lus. Les 4 096 octets BASIC à C000 ont le SHA1 6571FCA3E86E6DC99C2B56481979FBC24615BFF2. PRINT 12+34 produit 46, suivi du prompt >. La première assertion de banc exigeait à tort une ligne sans ce prompt : elle échoue après les mesures valides ; l'attente est corrigée dans le banc seulement. Le nouvel essai logiciel passe sans répéter les fenêtres de mesure ni modifier la production.

Captures réellement produites, inspectées et conservées sous `artifacts/TestsMachines/Acorn/Acorn System 5/` et, séparément, `artifacts/TestsChips/Vdu80CharacterGenerator/Acorn System 5/` :

- `20261109-094546-vdu-rc-monitor-debug.bmp`, SHA256 FD50347AC7BB06B1E18C3FC9D626FDF60B80C3B6F15E424A60C12178A457C630 ; même image que le moniteur de la première fenêtre `20261109-094224-vdu-rc-monitor-debug.bmp`.
- `20261109-094546-vdu-rc-disk-basic-debug.bmp`, SHA256 66D1555978655007CC0AFE6BFB34A3380C4E1ABE98845030B8C66E4B72110949 ; catalogue, BASIC et résultat 46 visibles.

Le disque est détaché et libéré dans finally, même si le nettoyage de la carte échoue ; entrées relâchées et bande éjectée. Aucun processus RML ni surface graphique native créé. Banc temporaire supprimé et assembly régénérée sans lui. Captures finales conservées hors Git.

La machine n'est pas clôturée : performance Debug insuffisante. Le réseau 24 MHz seul consomme encore environ 2,45–2,48 s hôtes pour 1 s émulée avec récepteurs 12/2 MHz dans son scénario isolé. Le coût C# des trois compteurs et des notifications demeure établi indépendamment du CPU ; le trajet série ajoute son activité. Poursuivre les optimisations logicielles conservant tous les états/fronts, puis mesurer leur effet complet. Aucune modification de précision, d'options Debug, de cadence ou de matériel n'est autorisée pour atteindre artificiellement 100 %. Aucun dynarec n'a été ajouté à la suite des questions de conception.

### Mesure actuelle après publication série — 9 novembre 2026, passage 10:17

System 5 natif sans RML, NOS80 et VDU80 externes déjà identifiés, 48 Kio RAM 2000–DFFF, 8271 présent, aucun disque monté. 40 champs de chauffe puis trois fenêtres de 80 champs, RunFrame et drainage audio inclus, sans compilation/test concurrent. Ce passage mesure le moniteur ; il ne répète pas l'essai BASIC/disquette déjà effectué.

| Fenêtre | ms/champ | Champs/s sans limite | CPU MHz émulés | Vitesse réelle | Périodes source | Cycles CPU |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 114,775827 | 8,712636 | 0,347948 | 17,397391 % | 38 338 560 | 3 194 880 |
| 2 | 116,842544 | 8,558526 | 0,341793 | 17,089666 % | 38 338 560 | 3 194 880 |
| 3 | 114,562517 | 8,728858 | 0,348596 | 17,429785 % | 38 338 560 | 3 194 880 |

Cadence physique 50,080128 Hz, ratio source/CPU exactement 12. La publication série apporte 6,9–10,7 % sur son montage synthétique mesuré, mais ce passage complet reste proche du précédent 17,2 % : aucun gain global significatif n'est établi par cette comparaison, et aucun 100 % n'est annoncé. Les trois compteurs 24 MHz et le trajet vidéo conservent leur activité complète. Les empreintes de framebuffer diffèrent entre fenêtres ; ce passage ne vérifie pas leur égalité entre champs. La capture finale est inspectée : moniteur « Acorn Dos », prompt * lisible.

Capture réelle `20261109-101751-serial-publication-monitor-debug.bmp`, SHA256 du fichier 162FD4E97CB8FF4D3D07BE20A79393C087D912C1559AEFE60DB9F951DFB9DA12, sous TestsMachines/Acorn/Acorn System 5 et copie TestsChips/Sn74LS165/Acorn System 5. Aucun RML ni ressource graphique native créé ; cœur réinitialisé et bande éjectée dans finally. La performance complète reste ouverte.

Nettoyage terminé : banc temporaire supprimé, assembly régénérée ; 131 contrôles System/audio réussis (3 s), sans sauvegardes reportées. Aucun build de livraison utilisateur lancé à ce stade.

### Gain natif du bloc critique — 9 novembre 2026, passage 10:48

Après optimisations du compteur, de son destinataire direct et du routage des sorties, même NOS80/VDU80 externes, RAM 2000–DFFF, 8271 présent sans média, 40 champs de chauffe puis trois fenêtres de 80 champs. Debug normal, rendu et drainage audio inclus, aucun test ni compilation concurrent.

| Fenêtre | ms/champ | Champs/s sans limite | CPU MHz émulés | Vitesse réelle | Périodes source | Cycles CPU |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 102,139359 | 9,790545 | 0,390995 | 19,549760 % | 38 338 560 | 3 194 880 |
| 2 | 102,206670 | 9,784097 | 0,390738 | 19,536885 % | 38 338 560 | 3 194 880 |
| 3 | 101,742130 | 9,828770 | 0,392522 | 19,626088 % | 38 338 560 | 3 194 880 |

Face au passage précédent à 114,775827 / 116,842544 / 114,562517 ms/champ, durée moyenne réduite d'environ 11,6 %. Ce passage inclut aussi la réduction des recalculs ROM : ne pas attribuer le gain entier au seul routage. Cadence matérielle 50,080128 Hz et ratio source/CPU exactement 12. Les trois empreintes de framebuffer sont identiques à celles du passage précédent : E9E458B2454C6AAD008F6EA0A02D78D406A9EBC80DA45D7057462F60FA99E76B, 113FD6C47E8C8E57D4731313BFC2BDCFA42DF867D1FEED1D1CB1A1CB25719108, puis la première de nouveau.

Capture réellement produite et inspectée : `20261109-104807-critical-clock-path-monitor-debug.bmp`, SHA256 fichier 162FD4E97CB8FF4D3D07BE20A79393C087D912C1559AEFE60DB9F951DFB9DA12, sous TestsMachines/Acorn/Acorn System 5 et copie TestsChips/Sn74S163/Acorn System 5. Moniteur Acorn Dos et prompt lisibles, fichier identique à la capture du moniteur avant ce bloc.

Nettoyage du cœur dans finally, aucun RML ni ressource graphique native créé. Banc temporaire supprimé, assembly régénérée ; 131 contrôles System/audio réussis (4 s) et 75 contrôles de composants réussis (99 ms). Aucun build de livraison utilisateur lancé. Le bloc de réduction des coûts par appel est vérifié ; System 5 reste en cours à 19,5–19,6 % Debug. Le générateur traite encore chaque période de 24 MHz et le rendu écrit chaque dot : avancement groupé et portions stables restent à traiter. Le CPU, les fréquences et les cycles matériels n'ont pas été modifiés.

### État actuel après intervalles stables — 9 novembre 2026

Les mesures ci-dessus précèdent plusieurs optimisations raccordées conservant les fronts et échéances. Dernière comparaison native rapprochée : 61,101054 → 59,137233 ms/champ, 33,77 % Debug au dernier passage ; les variations initiales sont consignées et le gain isolé de la bascule n'est pas extrapolé à la machine. CAT, transfert BASIC exact, calcul 46 et SAVE/LOAD persistant dans une machine neuve sont de nouveau vérifiés après ces changements. Le suivi canonique contient les tableaux, captures, erreurs du banc corrigées et limites : [validation courante System 5](../../../../../tasks/machines/AcornSystem/system5-validation.md). La performance et la clôture restent ouvertes ; le retrait/insertion d'un média pendant une commande doit être confronté séparément au câblage matériel.

### Audit RESET, READY et INDEX — 9 novembre 2026

Historique établi : le reset général dans AcornSystemMachine.AttachFloppy lors d'un retrait en commande active a été ajouté par 08b9c6e90 le 3 novembre, avant les dernières optimisations. La fiche Intel AFN-00223B, page imprimée 8-116 (PDF 2), distingue RESET pin 4, READY0/1 pins 32/5 et INDEX pin 34. RESET force l'état inactif et les sorties basses ; ce n'est pas une notification de changement de disquette. Le manuel System 5, section carte 400,004, page 1 (PDF 96), réserve le reset à NRST et à la commande logicielle ; le schéma figure 8, page 13 (PDF 108), montre NRST à travers IC2, sans connexion au retrait du disque.

Le manuel 400,004 §2.3 et §2.5, pages 4 et 6 (PDF 99/101), et figure 4 décrivent le générateur READY réel : 4 MHz, double compteur IC8 74LS393 /256 vers 15,625 kHz, compteur IC9 4020, seuil de 4096 clocks soit 262,144 ms, et deux circuits IC10/IC11 4013. Chaque lecteur dispose des deux bascules du 4013 : sélection sur Set de la première, D sur le Q12 de la figure (pin 2 du 4020), clock sur l'index inversé ; ses sorties commandent les entrées de la seconde, dont /Q fournit NREADY. Les connexions complètes restent celles de la figure 4, à reporter depuis le schéma et les fiches des composants avant câblage.

Le code Intel8271Drive.Synchronize fournit actuellement Ready=image!=null. Cela ne reproduit pas ce circuit ni sa qualification temporelle. Les types 74LS393/4020/4013 nécessaires ne sont pas présents dans Chips à ce passage ; les réaliser et vérifier successivement avant de raccorder READY. Le sélecteur et la rotation du lecteur doivent informer ce circuit par événements, sans polling entre composants ni horloge déplacée dans la machine.

Important pour INDEX : §2.6.2 page 8 (PDF 103) indique NINDEX actif bas une fois par révolution, mais aussi continuellement actif bas en l'absence de disquette. Ne pas remplacer cette condition par une absence d'assertion supposée. La polarité entre cette ligne, son inverse IC2/6 et les entrées de chaque composant doit être suivie depuis le schéma. Les sources actuelles ne suffisent donc pas à justifier un simple remplacement du reset par Ready=false. Ce bloc est encore en cours ; les anciennes validations de CAT/BASIC/SAVE/LOAD ne terminent pas cette conformité.

Sources conservées : Intel-8271-AFN-00223B.pdf et pages 01–03 sous artifacts/.tmp-archives/chips/Controllers/Intel/FDC/8271/ ; le PDF est scanné et son extraction texte ne contient que des séparateurs de page. Manuel System5Handbook.pdf et rendus System5-fdc-ready-101.png / System5-fdc-schematic-108.png sous les archives machine. Référence Intel : https://www.theoddys.com/acorn/semiconductor_datasheets/D8271.pdf ; manuel Acorn : https://chrisacorns.computinghistory.org.uk/docs/Acorn/Manuals/Acorn_System5Handbook.pdf.
# Complément de revue — SN74LS393, 9 novembre 2026

Le premier composant manquant du générateur READY est maintenant réalisé dans Chips, avec sa gestion locale des fronts et propagations, son profil paramétrable et 22 contrôles permanents. La fiche [SN74LS393](../../../../../chips/Logic/Sn74LS393.md) distingue les délais constructeur QA/QD/CLR et les estimations QB/QC autorisées par le choix utilisateur « le meilleur à faire ». Après retrait du banc temporaire, 109 contrôles de logique passent en Debug (307 ms). QD seul observé, mesure isolée : 57,125133 → 13,870967 ms pour 100000 périodes, soit 75,7 % de durée en moins, comptes et dates identiques. Ce scénario ne mesure pas la machine assemblée et n'établit pas un gain lorsque QA est aussi raccordé.

La carte 400,004, ses composants 4020/4013 et son raccordement au lecteur restent ouverts. Le reset logiciel à l'éjection n'a pas été supprimé avant réalisation du circuit complet. Les deux derniers commits 29dedacdd et 1fdb2abde ont été retirés à la demande de l'utilisateur, localement et sur origin/main ; leurs modifications sont conservées dans les fichiers avec ce travail, sans commit de remplacement.


Complément de coût SN74LS393 : le scénario QD seul ne suffisait pas. Le raccordement autonome QA/QD + cascade atteint maintenant **6,445 MHz équivalents Debug**, moyenne 155,153500 ms pour un million de périodes contre 223,575767 ms avant le bloc profilé (4,473 MHz), soit 30,6 % de durée en moins. Toutes sorties + cascade : 5,944 MHz. Même fréquence configurée 4 MHz, mêmes dates, comptes et empreintes. 23 contrôles propres et bloc logique 110 réussis / 286 ms après retrait des bancs et traces. Aucun nouveau câblage de carte ni gain machine mesuré. Détails dans docs/chips/Logic/Sn74LS393.md ; la réalisation CD4020B/4013 et le circuit READY restent ouverts.

CD4020B ajouté dans Chips, avec 23 contrôles permanents et ses mesures/profil documentés dans docs/chips/Logic/Cd4020B.md. Source de caractérisation TI SCHS030D, profils 5/10/15 V, quatorze étages et reset asynchrone. Après retrait du banc et des traces, 133 contrôles de logique passent en 322 ms. Toutes sorties physiques observées : 8,798 MHz équivalents Debug contre 15,625 kHz reçus ; Q13 seul, calcul différé exact 0,134333 ms par million de périodes. Aucun gain machine annoncé ni raccordement READY effectué ; réalisation et vérification du 4013 encore nécessaires avant câblage complet. Aucun nouveau commit.

CD4013B ajouté dans Chips et vérifié avec 24 contrôles propres, profils constructeur 5/10/15 V et délais séparés des sorties asynchrones. Après nettoyage et reconstruction, 157 contrôles de logique passent en 308 ms. Cascade deux canaux observée : 5,469 MHz équivalents pour le premier configuré à 2 MHz (second 1 MHz), contre 3,872 MHz avant optimisation, 29,2 % de durée en moins, dates et niveaux identiques. Cette capacité isolée ne donne pas la vitesse du System 5 assemblé. Fiche/profil/mesures docs/chips/Logic/Cd4013B.md. Aucun nouveau commit, aucun câblage READY ajouté ; comparaison des autres composants et connexions 400,004 encore à terminer avant raccordement et validation complète.

## Revue complémentaire de la carte 400,004 — 9 novembre 2026

Figure 8 (page imprimée13/PDF108) et nomenclature (page12/PDF107) relues après les trois composants READY. L'inventaire actuel de Chips/Logic ne contient pas les quatre types suivants :

| Circuit | Emplacements | Rôle dans la carte |
| --- | --- | --- |
| 74LS00 | IC2, IC12 | Inversion RESET/INDEX et logique DACK ; portes et latch du séparateur de données |
| 7438 | IC3, IC5, IC6 | NAND collecteur ouvert : NMI et sorties du lecteur, dont écriture/direction/sélection ; lignes résolues avec leurs pull-up |
| 74LS138 | IC4 | Décodage de sélection du FDC depuis le bus |
| 74LS93 | IC7, IC14 | Compteurs du séparateur de données, cadencés depuis4 MHz et réinitialisés par le flux |

Le séparateur réel (§2.4) utilise les compteurs et IC12 pour NDATAWINDOW. Intel8271Drive.ReadPulse applique aujourd'hui un seuil logiciel entre deux impulsions ; ce commentaire parle d'un monostable, contrairement à cette carte. Il faut réaliser le montage System sans imposer son circuit aux autres machines partageant le relais de lecteur. Le décodage est actuellement direct dans AcornSystemMachine.Bus.DiskRegister ; ses adresses et miroirs doivent être comparés au LS138 raccordé.

SystemClockGenerator expose déjà Clock4MHz et Clock4MHzChanged. AcornSystemMachine.WireFastClock raccorde actuellement les sorties2 et12 MHz, pas cette sortie4 MHz au circuit disque réel. La carte physique utilise4 MHz, IC8 fournit2 MHz au FDC ; ses divisions restent dans les puces, pas dans une nouvelle boucle de machine. Les détails de sélection des clocks des autres System restent séparés du montage rapide.

IC13 (74LS13), C5/C6, R9/R10 et RV1 portent l'astérisque « non utilisés dans l'application System5 » : ne pas créer ni faire avancer cet oscillateur local dans cette configuration. La réalisation des autres circuits et leur raccordement restent ouverts ; le reset à l'éjection n'est pas remplacé avant la fin de ce circuit.

### Composants logiques terminés après cet inventaire

SN74LS00 réalisé et vérifié : quatre NAND avec délais locaux 9/10 ns typiques, 17 contrôles propres, 174 contrôles de logique réussis après nettoyage. Optimisations scalaires mesurées 10,2 % de durée en moins pour une porte et 24,9 % pour quatre ; voir docs/chips/Logic/Sn74LS00.md. Aucune horloge ajoutée à une porte combinatoire.

SN7438 réalisé et vérifié ensuite : quatre NAND à collecteur ouvert, libération distincte d'un haut piloté, profils 14/11 et 22/18 ns sous les conditions de charge TI documentées. 17 contrôles propres dont collecteurs partagés et absence de pull-up ; 191 contrôles de logique réussis / 273 ms après retrait des bancs/traces et reconstruction. Moyennes isolées finales 78,8599 ms par million de changements pour une porte, 247,4626 ms pour quatre scalaires ; voir docs/chips/Logic/Sn7438.md pour toutes les fenêtres et la tentative d'optimisation retirée. Ces résultats n'actualisent pas la vitesse de la machine entière. 74LS138 et 74LS93 restent à terminer avant le raccordement du circuit 400,004 ; aucun nouveau commit pendant ces blocs à la demande utilisateur.

74LS138 réalisé et vérifié après le7438 :15 contrôles propres,206 contrôles de logique réussis /230 ms après suppression du banc/traces et reconstruction. Huit sorties et trois validations réalisées avec les trajets et directions LS de TI SDLS014 ; aucune horloge ajoutée. Fonctions, limites du profil numérique, correction causale des entrées simultanées et toutes les mesures dans docs/chips/Logic/Sn74LS138.md. Optimisations confirmées par retour au résolveur général :17,2 % de durée en moins sur les adresses,13,8 % sur G1 ; bloc premier/dernier28,3 %/29,0 %. Débits isolés4,457/7,662M changements/s ; ils ne modifient pas la dernière mesure de la machine assemblée. Aucun raccordement ni nouveau commit ;74LS93 reste à terminer avant la carte400,004.

74LS93 réalisé et vérifié après le décodeur : deux clocks externes, sections /2-/8 indépendantes, cascade /16 uniquement par le fil réel, double reset AND, carries et sorties datés suivant le profil TI documenté.14 contrôles propres ;220 contrôles de logique réussis /240 ms après suppression du banc et des quatre artefacts puis reconstruction. Profil, limites et fenêtres dans docs/chips/Logic/Sn74LS93.md. Retour rapproché cascade -17,2 %, CKB observé -28,7 %, QD seul -11,9 % ; dernière cascade144,6408 ms par million,6,914 MHz équivalents de CKA pour4 MHz configurés et CKB2 MHz raccordé. Les fréquences réelles et tous les délais restent inchangés. L'inventaire des types manquants400,004 est réalisé ; leur câblage complet, ses charges et sa validation restent à faire. Aucun gain machine déduit ni nouveau commit.

Circuit400,004 réalisé et vérifié isolément : voir [relevé et résultats](fdc400004.md). Passage actuel :13 contrôles réussis,2,8288 s. La machine active utilise encore l'ancien relais de lecteur ; raccordement mécanique et préservation du regroupement vidéo avec4 MHz restent ouverts. Aucun nouveau débit machine déduit.

Préservation du chemin vidéo lors de l'abonnement4 MHz maintenant réalisée et vérifiée dans SystemClockGenerator :45 contrôles après nettoyage,210 ms. Avances bornées au prochain front raccordé, périodes12 MHz stables conservées. Banc isolé :7,5 % puis10,2 % de durée en moins dans les deux séries conservées ; détails SystemClock.md. Aucun débit de machine complète actualisé avant raccordement du lecteur.

### État actuel du raccordement et des contrôles —10 novembre2026
Les paragraphes ci-dessus retracent les étapes antérieures au raccordement du lecteur. Le montage actif utilise désormais SystemFloppyBoard, SystemFloppyDrive et les vrais fils4 MHz, bus, mécanique, READY/INDEX et séparateur400,004 ; l'ancien relais de lecteur reste réservé aux autres profils. Le chemin System5 ne réinitialise plus le8271 lors d'une éjection. Source et validations actuelles : [carte400,004](fdc400004.md) et [suivi System5](../../../../../tasks/machines/AcornSystem/system5-validation.md).
Le logiciel disque a été vérifié sur le code avant ce dernier essai : catalogue, chargement BASIC (4608 octets lus, SHA1 des4096 octets àC000 : `6571FCA3E86E6DC99C2B56481979FBC24615BFF2`), PRINT12+34 donnant46, image originale inchangée. La modification165 retenue ensuite conserve exactement le registre connu déjà rempli de SER ;92 contrôles165/VDU/curseur/générateur et135 System/carte/audio passent. Comparaison native157,65817→153,31520 ms/champ (-2,7547 % observés), gain propre du parcours165 :37,26 % ; ne pas transposer ce facteur à la machine.
Le nouvel essai de sélection des échéances de carte est retiré :153,72137→159,64549 ms/champ, régression3,8538 %, sans divergence des60 champs/audio/comptes. Carte rétablie à sa copie initiale et reconstruite,136 contrôles System/carte/audio passent. Les preuves et toutes les fenêtres sont dans le suivi. La cadence Debug reste proche de13 % du nominal, très loin de100 % ; System5 reste en cours, sans nouveau build utilisateur ni commit.

Contrôle logiciel sur le code retenu du10 novembre : SSD original ouvert en lecture seule via SDK, CAT puis BASIC depuis disquette, PRINT12+34 donnant46. Pas de ROM BASIC supplémentaire ni d'injection mémoire. SHA1 du programme4096 octets àC000 : `6571FCA3E86E6DC99C2B56481979FBC24615BFF2`, image source inchangée après fermeture. Capture native réellement inspectée : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-retained-disk-basic.bmp`, rapport dans le même dossier. Une tentative de qualification plus large du registre vidéo est retirée (+2,33 % de temps sur moniteur), sources rétablies et reconstruites,92 contrôles composants réussis. Le scénario BASIC ROM essayé dans ce diagnostic ne démarrait pas BASIC et ne constitue pas une preuve logicielle. Performance toujours ouverte, environ13 % du nominal natif Debug ; aucune nouvelle livraison ni commit.
Après retrait :92 contrôles composants et136 contrôles System/carte/audio passent ; diagnostics et six copies DLL/source supprimés après consignation. Reconstruction sans diagnostic :0 erreur,4 avertissements préexistants. Aucun nouveau gain, commit ou build utilisateur.

Revue du relais10 novembre : [deux corrections de connecteur](fdc400004.md) conservées dans SystemFloppyDrive.cs. L'insertion ou le remplacement réapplique maintenant les niveaux WRITE ENABLE/sélection/moteur présents, et les pulses READ DATA joints ne créent plus de coupure fictive au même instant.147 contrôles System/carte/audio réussissent en11 s, dont29 contrôles carte ; bibliothèque Acorn0 erreur/0 avertissement. Aucun gain de performance ou nouvel essai RML déduit. La validation BASIC native citée plus haut précède ces corrections et n'est pas prétendue réexécutée ici. Les autres exigences et la cadence System5 restent à clôturer.
Relais disquette, complément10 novembre : libération de READ DATA pendant Dispose si un pulse appartenant au relais est encore actif. Les contrôles des deux lecteurs prouvent la remise au repos immédiate, la conservation du8271 et le premier pulse d'un relais reconnecté ; destruction répétée de l'ancien sans effet sur le nouveau.149 contrôles System/carte/audio réussis en12 s, dont31 carte ; source et dates dans [la fiche de carte](fdc400004.md). Pas de changement des clocks ni de nouvelle vitesse mesurée ; les exigences restantes de la machine gardent leur statut ouvert.
Écritures, complément10 novembre : quatre cas supplémentaires réussis sur les deux lecteurs, traversée de l’index sous WRITE ENABLE et STEP pendant une fenêtre ouverte. Ancienne/nouvelle piste, phase angulaire et dates de fermeture contrôlées ; aucun changement de production nécessaire.153 contrôles System/carte/audio passent en12 s, dont35 carte. Voir [conditions et limites de ces fenêtres sans transitions](fdc400004.md). Performance et autres points ouverts inchangés ; aucune nouvelle validation native ou RML déduite.

Revalidation native après les corrections du relais10 novembre : SSD original ouvert en lecture seule par le SDK, CAT/BASIC/PRINT12+34 par les entrées hôtes,4608 octets lus, SHA1 des4096 octets du BASIC àC000 conforme. Capture480×250 réellement inspectée : résultat46 et prompt. SSD inchangé après détachement/fermeture, ressources libérées dans finally. Mesure courante après calcul :150,86567 ms/champ, soit13,236 % du nominal sur dix champs audio drainé ; aucun gain avant/après déduit. Preuves et empreintes dans [le suivi System5](../../../../../tasks/machines/AcornSystem/system5-validation.md), fichiers20261110-current-relay sous artifacts. Le fonctionnement logiciel et la performance insuffisante sont désormais vérifiés après les corrections du relais ; cela ne clôture pas les autres exigences de System5.

Persistance revérifiée sur le code actuel10 novembre : SAVE PROBE C000 C010 écrit768 octets ; image fermée puis rouverte en lecture seule dans une machine neuve, LOAD PROBE relit768 octets et restitue les16 octets exacts. La capture native montre LOAD PROBE, le fichier au catalogue et le prompt. Aucun octet RAM/CPU injecté ; les données proviennent de BASIC chargé depuis le SSD. Original inchangé, copie de test supprimée, ressources fermées. Conditions et corrections du seul banc dans [le suivi System5](../../../../../tasks/machines/AcornSystem/system5-validation.md). Ce contrôle complète le calcul46 et les153 contrôles permanents ; aucune nouvelle mesure de performance ou clôture globale déduite.

Formatage complet revérifié le10 novembre sur le raccordement400,004 actuel : FORM80 invité formate et vérifie les80 pistes du lecteur1, de00 à4F ; Verified et catalogue vide visibles dans les captures natives inspectées. Le fichier fermé contient800 secteurs et798 secteurs de donnéesE5, remplaçant le motifA5 préparé avant montage ; utilitaires source inchangés. Conditions et preuves centralisées dans [le suivi System5](../../../../../tasks/machines/AcornSystem/system5-validation.md). Aucun nouveau changement de production, gain de cadence ou contrôle sonore humain annoncé ; la performance reste ouverte.

Sélection de surface corrigée10 novembre : contrairement au relais tête0 fixe précédent, FAULT/OP0 sélectionne maintenant les faces des lecteurs double tête configurés, conformément au manuel p8. La fabrique transmet les capacités des modèles existants ; les lecteurs simple face continuent à lire leur tête0. Fermeture de l’ancienne fenêtre avant changement, phase angulaire conservée.159 contrôles System/carte/audio passent, dont41 carte.
Validation native après fermeture/réouverture : fichiers distincts sur DRIVE1 et DRIVE3,1536 octets écrits puis1536 relus dans un cœur neuf, données et catalogues indépendants. Captures inspectées et source SSD inchangée, DSD de test supprimé, ressources libérées en finally. Sources et preuves dans [le suivi System5](../../../../../tasks/machines/AcornSystem/system5-validation.md) et [la fiche400,004](fdc400004.md). Les validations SSD/FORM80 précédentes restent des contrôles simple face antérieurs à cette correction ; aucune nouvelle cadence mesurée ici.

Actualisation10 novembre : chargement invité JFD/SCP/HFE/HxC Stream vérifié après correction de la reconstruction physique ISO dans MediaEngine ; BASIC4096 octets exacts et calcul46. Groupe médias élargi437 réussites/1 échec cartouche indépendant reproduit sans ces modifications. Inspection vidéo sous BASIC : alternance complet/partiel également en scalaire ; ne pas clore la VDU80 sur la seule présence du résultat en RAM. Détails et preuves dans le suivi System5 et vdu80.md. Aucun gain de cadence annoncé.

### Rectification de l’alerte vidéo du10 novembre
La comparaison exacte infirme l’alternance supposée à partir des aperçus : les quatre BMP initiaux sont identiques (SHA25660E56635077E1C9D631563E6C83AA17164C528C5179D57C9F8937BADFC2FFA31). Une nouvelle vérification produit16 champs natifs et isolés, groupés et scalaires, tous identiques (SHA25666D1555978655007CC0AFE6BFB34A3380C4E1ABE98845030B8C66E4B72110949). Registres, RAM, MA/DE/RA et chronologie sont stables :19,968 ms,39936 caractères et239616 dots par champ. Aucun défaut vidéo établi ni modification de production. Résultats détaillés et limites dans system5-validation.md, rubrique « Stabilité des champs BASIC : alerte d’aperçu infirmée ». La performance/livraison globale reste ouverte ; les seules descriptions antérieures de champs partiels sont infirmées par ces comparaisons binaires.

## Passage au contrôle utilisateur —10 novembre2026
Les règles machine permettent le passage à la suite de la validation lorsque les recherches devenues improductives dépassent une heure, même loin de100 %, après revue des composants/raccordements et application des gains utiles. Cette condition de performance est documentée ; aucun fonctionnement manquant n’est couvert par cette exception et System5 ne sera pas déclaré validé dans RML avant son essai.
Dernier moniteur complet Debug :143,82669 ms/champ /13,8834 % nominal (19,968 ms matériels), sans média, vidéo/drainage audio inclus. Causes, limites du profil, gains conservés et tentatives retirées détaillés dans system5-validation.md. Groupes finaux :1140 composants,246 System/disque/audio,111 médias/écriture physique, tous réussis. L’échec cartouche indépendant du groupe élargi reste documenté séparément. Build et contrôle utilisateur encore attendus.

### Livraison Debug du10 novembre2026
`scripts/local-building.cmd --building=debug --modules=A` a terminé avec le code0. Application, bibliothèques, launcher/updater et24 modules publiés ; présence de l’exécutable et de chacune des24 DLL vérifiée à partir de son manifeste, dont `Modules/acorn/retromedialab.emulation.acorn.dll`. Chemin utilisateur : `F:/GW GUI/build/Debug/Retro Media Lab/retromedialab.exe`. RML est resté fermé ; aucune session utilisateur interrompue et aucun essai visuel de cette livraison revendiqué.
Le build contient les corrections de sélection de face, les conteneurs à flux physiquement reconstruits et l’exploration JFD double face. La cadence native documentée reste13,8834 % en Debug, sans nouveau gain. Groupes finaux1140/246/111 réussis ; les nombres et limites restent ceux des rubriques précédentes. Essai utilisateur System5 encore requis avant clôture de la machine, puis poursuite des Acorn dans l’ordre convenu. Aucun commit partiel de documents créé pendant ce passage.

### Validation RML en attente d’autorisation —10 novembre2026
Après livraison, contrôle des processus retromedialab et retromedialab.app : aucune instance ouverte. La préparation UI du Debug a été tentée par Computer Use ; l’outil a retourné `Computer Use app approval timed out`. Aucun écran RML ni profil System5 observé ; aucun essai dans l’interface annoncé comme réussi. Le contrôle final des deux noms de processus confirme RML fermé, sans instance laissée ouverte par la tentative. Aucun réglage ou média utilisateur modifié dans ce passage.
La validation RML demeure requise avant clôture et passage à la machine suivante ; l’ouverture/autorisation de contrôle nécessite une intervention utilisateur. La question d’essai envoyée après livraison reste sans réponse. Le build, les1140/246/111 contrôles et les résultats de cadence consignés ne remplacent pas ce contrôle UI. Aucune nouvelle compilation, correction ou mesure n’est nécessaire pour réitérer la demande ; reprendre depuis le Debug livré lorsque le contrôle de l’application sera disponible.

### Cadence refusée par l’utilisateur —10 novembre2026
Le contrôle utilisateur du Debug livré rapporte6 FPS. Ce retour remplace l’attente de confirmation UI : la performance de System5 redevient bloquante, aucune clôture ni passage à une autre machine. Le dernier relevé natif reste143,82669 ms/champ ; aucun nouveau gain mesuré dans cette reprise.
Recherche primaire consultée : Microsoft https://devblogs.microsoft.com/visualstudio/delegate-the-analysis-not-the-performance/ (réduire les invocations fréquentes, non transposer son gain), Verilator https://verilator.org/guide/latest/simulating.html#benchmarking-optimization (réévaluations et coût du modèle), Accellera https://www.accellera.org/images/downloads/standards/systemc/TLM_2_0_LRM.pdf (frontières explicites de synchronisation ; son modèle approximatif ne remplace pas ici les échéances exactes). Les pistes générales figurent déjà au tableau de recherche du9 novembre : cette reprise cible leur réalisation, pas un nouveau catalogue de conseils.
Le profil retenu attribue58,671 % propres à carte disquette + générateur +163 +393 +93. L’essai93 sépare l’échéance interne du prochain effet raccordé : ses étapes internes continuent dans la puce, la carte ne se réveille que pour une sortie nécessaire ou un cas conservateur. Gain à mesurer ; ni la présence d’événements ni un banc isolé ne prouvent un gain global.

### Échéances de sorties seules : candidat retiré —10 novembre2026
Essai93 sur les deux boîtiers de la carte400,004 : annonce distincte du prochain effet raccordé, propagation interne conservée et retenue calculée jusqu’à la prochaine sortie observée. Les16 contrôles du candidat réussissent, dont comparaison de256 combinaisons masque/état avec entrées, resets et feedback ;41 contrôles carte réussissent. Empreintes vidéo/audio et399,360 ms natifs par fenêtre de20 champs identiques.
Mesures Debug du moniteur complet,20 champs de chauffe puis3×20 : référence166,06241 /163,88532 /161,21990 ms/champ (moyenne163,72254) ; candidat168,12456 /166,22949 /167,31000 (167,22135) ; retour142,46934 /141,38683 /147,10956 (143,65524). Variabilité hôte importante entre référence et retour ; le candidat ne démontre aucun gain et est plus lent dans les deux comparaisons. Retrait des modifications93/carte et des deux contrôles propres au candidat. Aucune baisse des timings électriques ni option Debug modifiée ; aucun résultat positif ajouté au tableau des optimisations réutilisables.
Rapports : artifacts/TestsMachines/Acorn/Acorn System 5/20261110-observable93-{reference,candidate,return}.md. La recherche ne doit pas rester limitée à ces compteurs : même une suppression hypothétique des58,671 % propres du groupe dominant multiplierait seulement la cadence par2,42 ;6 FPS deviendraient14,52 FPS, toujours insuffisants. Le budget global pour50,080 Hz est19,968 ms/champ, contre143,65524 ms actuels hors interface ; facteur7,194 requis dans ce scénario. Le retour RML6 FPS reste distinct et impose environ8,35× pour50,080 Hz. CPU, bus, vidéo, audio et coordination restent inclus dans le périmètre d’optimisation ; aucune clôture ni nouveau build applicatif.

### Récepteur indépendant à pixels variables —10 novembre2026
Le contrat `IIndependentClockPeriodReceiver` complète `IStableClockPeriodReceiver` : consommation exacte de périodes, même lorsque les sorties du récepteur changent, uniquement si ces sorties ne consultent ni n’affectent la source ou les autres domaines avant la frontière annoncée. La source conserve ses trois163 et borne l’intervalle aux fronts4/2 MHz selon les dépendances déclarées. Le récepteur vidéo conserve chaque front local et chaque pixel ; seules ses plages déjà stables utilisent le chemin arithmétique existant. Aucune période CPU, aucun délai ou mode matériel changé ; aucun agenda commun ni nouveau réglage utilisateur.
Le générateur essaie d’abord le préfixe stable existant, puis confie le reste au récepteur indépendant. Les liaisons restent protégées pendant cet intervalle. Les autres récepteurs conservent le parcours historique. Les notifications vidéo sont horodatées par la puce, jamais par l’avance temporaire de la source. Un futur raccordement dépendant des pixels doit respecter ou réduire cette frontière avant de bénéficier de ce contrat.
Validation du code retenu :45 contrôles générateur et53 contrôles vidéo/carte réussis. Trois graines supplémentaires comparent les fronts12/4/2 MHz, tous les comptes et les changements de liaison interdits. Les contrôles vidéo préexistants comparent les transitions datées et huit champs successifs, avec inversion et entrelacement. La comparaison native dans un même processus utilise deux cœurs NOS80/VDU80, RAM2000–DFFF, carte400,004 sans média,20 champs de chauffe puis3×20, ordre des mesures alterné. Le témoin désactive seulement le cache de capacité logicielle ; aucune RAM ni horloge matérielle modifiée. Vidéo/audio et durées natives identiques pour chaque fenêtre, mêmes trois SHA256 que la référence.
Mesures appariées ms/champ :144,80774→143,53322 ;144,98950→142,58179 ;144,73069→141,96234. Moyennes144,84264→142,69245, soit1,4845 % de temps en moins. Le premier candidat ajoutant un appel pour toutes les périodes donnait168,82105 ms/champ sur des fenêtres non appariées ; le préfixe stable est donc conservé directement dans la source pour limiter cette surcharge. Les premières fenêtres entre processus variaient fortement et ne servent pas à attribuer le gain.
Le chemin est conservé pour son gain apparié et son regroupement exact des sorties variables, mais ce résultat reste minime et ne clôture pas System5 :7,0081 champs calculés/s,13,9937 % nominal hors interface, champ matériel19,968 ms. Le retour utilisateur6 FPS reste distinct. Aucun gain significatif ajouté au tableau Application aux composants, aucun build applicatif ni commit de ce seul bloc. Rapport : artifacts/TestsMachines/Acorn/Acorn System 5/20261110-independent-periods-paired.md. Diagnostic et copie source supprimés après consignation ; projet local reconstruit sans eux : zéro erreur, quatre avertissements préexistants.


### Qualification des fronts QA par échéance8271 —10 novembre2026
La carte reçoit `NextClockEventChanged` du8271 et demande au393 le numéro de front QA correspondant. Le393 conserve tous ses étages et comptes physiques ; ses échéances raccordées concernent QD et les seuls fronts QA demandés. Les autres étapes de retenue restent traitées localement avant les entrées et transactions. Le contrôleur est rattrapé à partir du nombre réel de fronts QA avant les entrées qui consomment ses cycles et en fin d’avance. Les callbacks n’anticipent pas une autre réponse au même instant : rattrapage strictement antérieur puis ordre existant de la carte ; rattrapage inclusif à la fin. DATA WINDOW demeure une pure entrée de niveau, consommée au front prévu. Les cycles du8271 continuent aux fréquences d’origine, y compris reset/idle/seek ; ses propres chemins groupés gèrent ces périodes sans supprimer un cycle série actif.
Contrôles :39 du393 réussis (108 ms), dont les deux canaux, demandes renouvelées, clears, comptes groupés, dates et retenue jusqu’à Int64.MaxValue. Un débordement intermédiaire du nouveau calcul d’échéance a été reproduit puis corrigé en soustrayant les délais cumulés avant addition.42 contrôles carte réussis, dont lectures128 octets et chronologies des fenêtres/NMI sur les deux lecteurs comparées à l’ancien transport QA ; le témoin rétablit uniquement ses liaisons scalaires. Un contrôle idle conserve les mêmes mots et comptes QA avec plus de4 fois moins d’annonces d’échéances du diviseur.
Les mesures entre processus variaient trop : référence149,86374 ms/champ, premier candidat136,17606, retour142,57642, répétition du candidat168,12723. Ces fenêtres ne permettent pas d’affirmer un gain global de9 % ni d’attribuer la variation au code. Comparaison décisive dans un même processus : deux instantanés Debug d’Acorn ne différant que par les cinq fichiers carte, mêmes dépendances393 avec compte QA, deux contextes collectables et deux cœurs fermés en finally, contextes effectivement déchargés.20 champs de chauffe,3 fenêtres de20 champs, ordre alterné, vidéo et drainage audio inclus. Les durées natives399,360 ms et les trois SHA256 restent identiques.
Fenêtres appariées ms/champ :147,69734→143,81555 ;147,32888→143,49628 ;148,08331→143,97537. Le gain est réel mais reste faible et ne résout pas System5 ; aucune accélération massive annoncée. Rapport artifacts/TestsMachines/Acorn/Acorn System 5/20261110-qualified-clock-paired.md. La qualification évite du transport logiciel, elle ne crée ni fréquence fictive, ni agenda commun, ni option utilisateur. Sources conservées ; aucune compilation RML ni commit partiel de ce bloc.
Moyennes appariées :147,70318→143,76240 ms/champ, soit2,6680 % de temps en moins. Débit du contexte de mesure retenu :6,9559 champs/s,13,8896 % nominal, champ matériel19,968 ms. Contrôles de ce bloc :39 pour le393 et42 pour la carte ; ces nombres ne représentent aucun facteur de gain. Diagnostic, fabrique de mesure, deux instantanés DLL et copies source supprimés ; Acorn reconstruit sans fabrique, zéro erreur/zéro avertissement. Aucun gain massif ni cadence de100 % établi.


## Profil global actuel Debug/Release —10 novembre2026
Configuration NOS80/VDU80, Fast6502A2 MHz, RAM2000–DFFF, carte400,004 sans média ;20 champs de chauffe puis3×20 complets, framebuffer natif et drainage audio inclus, sans RML ni limite. Debug :151,78746 ms/champ,6,58816 champs/s,13,15524 % nominal. Release :24,81150 ms/champ,40,30389 champs/s,80,47881 % nominal. Cible19,968 ms/champ/50,08013 Hz. Empreintes vidéo/audio et durées identiques, flags d’optimisation et SHA256 des assemblages/ROM vérifiés dans les JSON. Les horloges physiques restent inchangées ; ni Debug ni Release ne clôturent la cadence actuelle.
Collecte par méthode sur51 champs Debug et317 Release, environ8 s chacun. Piles hors RunFrame/DrainAudio et attente d’attachement exclues, attribution propre au dernier appelant projet si la feuille est une aide runtime.194 méthodes Debug et47 Release observées ; l’inlining explique une partie du regroupement Release. Les inclusifs ne s’additionnent pas.
| Méthode prioritaire Debug | propre % | propre ms/champ collecté |
|---|---:|---:|
| Sn74Ls93.AdvanceTo |5,0836|8,04588|
| Sn74S163PeriodicInputs.Capture |4,6396|7,34318|
| SystemClockGenerator.PublishOutputs |4,3110|6,82301|
Autres coûts Debug : RememberDeadline3,8648 %,393.ResolvePending3,4705 %,163.CaptureClockPeriods3,1708 %,AdvanceFloppyToSource3,0765 %,163.ResolveClock2,9221 %,93.PublishDeadline2,8030 %,VDU.AdvanceStableDotClockPeriods2,7915 %. La priorité aux trois premières ne restreint pas le périmètre à trois méthodes.
Release : AdvanceWithStableReceiver37,8824 % propres (9,53244 ms/champ collecté),SetFastReferenceClock12,2155 % (3,07383 ms),93.AdvanceTo7,4155 % (1,86599 ms),carte.AdvanceTo6,8628 %,393.ResolvePending5,2819 %. Le coût de routines inlinées reste attribué à leur appelant ; ce classement ne se compare pas signature par signature au Debug.
Classement initial :93.AdvanceTo est également présent dans les trois premiers Release. La reprise demandée vise maintenant toute la chaîne générateur, distribution, carte disquette et récepteurs, pas cette seule méthode. Lecture actuelle confirmée : il résout les retenues locales et leurs notifications ; ces transitions physiques doivent être conservées. Le profil agrège les deux93 de la carte et ne désigne pas encore lequel domine. La table163 utilise déjà un précalcul avec modulo de phase ; la publication source consulte seulement ses abonnés mémorisés. Les110,3 ms inclusives de PublishOutputs ne sont pas du coût propre de publication. Examiner ces chaînes avant d’appliquer la prochaine optimisation, sans revenir aux essais sans profil.
Preuves et détails : artifacts/TestsMachines/Acorn/Acorn System 5/20261110-global-method-summary.md ; classements complets20261110-global-method-debug.methods.md/.json et release.methods.md/.json ; JSON de mesures libres et de collecte. Les temps par méthode sont des estimations échantillonnées pendant collecte, distinctes des mesures libres. Aucune nouvelle optimisation, aucun build applicatif, aucune fenêtre RML ni commit partiel dans ce profil.
Nettoyage effectué : diagnostic console/analyseur et bin/obj possédés supprimés après vérification du chemin absolu ; traces/conversions et signaux temporaires supprimés. Rapports JSON/Markdown de mesure et classement complet conservés, hors commits. Les processus supervisés étaient tous terminés et leurs handles libérés avant suppression.

### Répartition exhaustive du temps propre et périmètre de reprise
Le profil Debug complet attribue100 % du temps échantillonné dans RunFrame/DrainAudio. Les cinq premières méthodes représentent21,369470 % ; les78,630530 % restants sont répartis sur les autres méthodes, et ne constituent pas un coût extérieur inconnu. Les temps inclusifs se recouvrent et ne doivent pas être additionnés.

| Ensemble de méthodes | Temps propre Debug |
| --- | ---: |
| Câblage disquette SystemFloppyBoard |14,58 % |
| Générateur SystemClockGenerator |13,93 % |
| Deux compteurs74LS93 |11,48 % |
|74S163 et représentation de leurs entrées |11,13 % |
|74LS393 |10,12 % |
| Coordination AcornSystemMachine |6,56 % |
| CPU6502 |6,13 % |
| Générateur de caractères VDU80 |5,79 % |
| Autres composants, vidéo, bus et méthodes |20,28 % |
| **Total** | **100,00 %** |

La cible est la chaîne complète de distribution des horloges et de coordination disquette, plutôt qu'une méthode isolée du93. Le classement par méthode reste une preuve de localisation, pas une justification pour limiter le travail à ses5,08 %. Référence complète actuelle :151,78746 ms/champ en Debug,13,15524 % du nominal ;24,81150 ms/champ en Release,80,47881 %. Aucun gain structurel de cette reprise n'est encore mesuré.


### Recherche primaire actuelle sur la réduction d’activité logicielle
Documentation de développement du projet Verilator consultée le10 novembre2026 : https://github.com/verilator/verilator/blob/master/docs/internals.rst , section Scheduling of clocked and combinational logic. Elle décrit la limitation des évaluations aux domaines dont les entrées peuvent changer et l’importance de l’ordre des dépendances. Cette lecture justifie la recherche de travail logiciel répété dans la chaîne complète ; elle ne fournit ni les équations du matériel Acorn ni un gain transposable. Aucun code Verilator, ordonnanceur central, coroutine ni modèle SystemVerilog n'est importé. Les références matérielles restent les schémas Acorn et les fiches constructeur archivées. La description périodique raccordée est vérifiée contre les fronts explicites du code existant.

### Point de reprise avant prolongement du train d’horloge aval
Premier raccordement complet du motif4 MHz aux393/93 :48 contrôles carte/horloges réussis, zéro échec/ignoré,15 s, incluant lecture128 octets5A, écriture128 octetsA6 puis relecture, variante scalaire et périodique, reset et fenêtre du séparateur. Comparaison native Debug dans le même processus,20 champs de chauffe et3×20, ordre alterné, vidéo/audio inclus :166,03954→168,74964 ms/champ, soit **1,632 % de temps supplémentaire**. Les trois empreintes vidéo/audio correspondantes sont identiques entre les transports. Rapport : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-clock-chain-native.md`. Ce candidat n'est pas validé comme optimisation ; le câblage périodique initial est présent dans le point de reprise demandé par l'utilisateur, la refonte reste en cours.
La représentation de la sortie QA du93 et le calcul différé de CKA sont maintenant réalisés dans le composant, mais ne sont pas encore raccordés à IC7 dans la carte.19 contrôles93 réussissent, zéro échec/ignoré,616 ms : le nouveau cas compare le récepteur aval aux fronts explicites sur les seize états initiaux, avec reset asynchrone y compris relâché avant fin de clear, niveaux et traces datées, puis une avance différée jusqu'à50 µs. Aucun gain machine attribué à cette capacité non encore raccordée. La cadence System5 et la reprise globale restent ouvertes ; aucun build applicatif ni essai RML nouveau.

### Première propagation périodique aval —10 novembre2026
Raccordement IC14/QA→IC7/CKA, avec révocation au reset et retour aux niveaux explicites.48 contrôles carte/horloges existants réussis, zéro échec/ignoré,15 s, plus un contrôle de comparaison nanoseconde par nanoseconde sur30 µs : transitions QA/QB/QD de l'aval, fenêtre, NMI, comptes et resets identiques au transport explicite,160 ms.
Comparaison native Debug dans le même processus,20 champs de chauffe puis3×20 alternés, NOS80/VDU80/400004 sans média : référence158,83973 /164,78276 /160,42874 ms/champ, candidat141,89778 /143,09833 /142,65693. Moyennes **161,35041→142,55102 ms/champ, −11,651 %** ; les trois empreintes vidéo/audio et les durées matérielles sont identiques. Champ19,968 ms à50,08013 Hz ; candidat7,01503 champs/s,14,00762 % nominal,280152 cycles émulés/s. Ce gain ne termine pas la cadence. Rapport : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-clock-chain-separator-native.md`.

### Propagation périodique jusqu'au fil IC7/QA→CKB —10 novembre2026
La carte raccorde aussi le fil extérieur QA→CKB d'IC7 par sa description périodique ; QA repasse en notifications explicites lors des resets ou d'une source modifiée. IC14 et IC7 annoncent les seuls événements raccordés nécessaires ; les sorties QB/QD du latch restent datées et explicites.20 contrôles93 passent (690 ms) et49 contrôles carte/horloges passent (15 s), zéro échec/ignoré. Le contrôle de carte compare chaque niveau de fenêtre à la nanoseconde, les traces QB/QD, NMI, resets, comptes CKA/CKB et mots finaux ; les lectures/écritures/relectures réelles128 octets restent couvertes dans les deux transports.
Comparaison native Debug, même scénario et protocole que ci-dessus : référence159,89451 /160,20502 /161,71427 ms/champ ; candidat129,39777 /128,07345 /132,05419. Moyennes **160,60460→129,84181 ms/champ, −19,154 %**. Trois empreintes vidéo/audio correspondantes et durées matérielles identiques. Candidat **7,70168 champs/s,15,37871 % nominal,307574 cycles émulés/s**, face au champ19,968 ms (50,08013 Hz). Rapport : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-clock-chain-cascade-native.md`. Ce gain concerne le raccordement complet, pas le coût isolé d'une méthode. Il ne prouve pas100 % et ne termine pas la machine ; le profil global restant doit guider la suite.

### Profil global actuel après la refonte des fils périodiques
Collecte Debug native de58 champs complets,8068,9954 ms hôte ;8070,138 ms attribuées aux piles RunFrame/DrainAudio, avec intervalle de conversion Speedscope contrôlé et temps d'attente exclu. Le léger écart échantillonnage/chronomètre n'est pas un coût supplémentaire mesuré. Options Debug non optimisées par le JIT confirmées ; Acorn SHA256E60B2C0D0B880A8D1B7A65A01C39B9D8A5748BCDBB889D59B7791295C24884FA. La collecte est instrumentée et ne remplace pas le relevé libre129,84181 ms/champ.

| Groupe | Temps propre actuel % |
| --- | ---: |
|74LS393 |24,576057 |
|74LS93 |11,272918 |
| Autres composants et méthodes |10,955447 |
| Carte disquette |8,708615 |
| Générateur |7,264854 |
| VDU80 caractères |6,939794 |
| CPU6502 |6,462322 |
| Coordination machine |6,258624 |
|74S163 |6,177800 |
|74LS165 |4,147344 |
| Vidéo |3,225735 |
| CRTC |2,591646 |
|8271 |1,418846 |
| **Total avant arrondi** | **100,000000** |

Les trois premières méthodes propres sont maintenant393.ResolvePending4,9996 %,393.AdvancePeriodicTime4,7902 %,393.ConnectedDeadline3,2908 %. Viennent VDU.AdvanceStableDotClockPeriods3,2140 %,93.AdvancePeriodicInputs2,4775 %,93.SkipUnobservedCka2,4521 %,393.PublishDeadline2,3581 %. La liste complète et les inclusifs séparés sont dans `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-clock-chain-profile-methods.{md,json}` ; aucun coût non affiché n'est traité comme inconnu. La chaîne393 est maintenant le premier groupe propre. Lecture du câblage : SynchronizeController matérialise encore tout le ripple393 à chaque synchronisation de carte pour lire le seul compte QA. Une projection locale de ce compte entre deux effets raccordés peut éviter cette matérialisation répétée ; elle doit conserver le traitement des sorties et demandes qualifiées aux échéances annoncées. C'est la prochaine cible établie, pas encore un gain obtenu.

## Comparaison du bloc architectural complet —10 novembre2026
Le générateur résout directement les états canoniques des trois163 depuis les motifs d'entrée déjà établis, puis publie les seuls niveaux externes de la frontière ; aucun fil interne décrit n'est rejoué. Le393 projette localement le compte QA entre deux effets raccordés au lieu de matérialiser chaque ripple lors de la synchronisation8271. Le bus de carte retient ses pins et ne transmet plus les variations à8271 pendant une désélection déjà effective ; la sélection et sa libération restent transmises. Les trains IC14/QA→IC7/CKA et IC7/QA→CKB conservent leurs révocations/reset et repli explicite.
Contrôles :50 cas393 (894 ms),16 cas163 (432 ms),49 cas générateur (171 ms),49 cas carte/horloges (16 s), tous réussis sans échec/ignoré. Le contrôle393 couvre les projections non mutantes et leur refus aux frontières qualifiées ; les contrôles163 et générateur comparent aussi le retour au parcours explicite. Les contrôles de carte conservent les bus/mirroirs, NMI, resets, fenêtre et lectures/écritures/relectures128 octets.
Comparaison native appariée dans le même processus : deux contextes collectables isolent chacun leur couple Acorn/Chips. Référence : les cinq sources source-clock/carte du point63d77fc27 ; les autres sources (dont les nouvelles API inutilisées dans ce témoin) et la fabrique sont identiques. Aucun réglage matériel ni option Debug différent ; DisableOptimizations vérifié dans les quatre DLL.20 champs de chauffe puis3×20, ordre alterné, vidéo/audio inclus et empreintes hors chronométrage.

| Fenêtre | Référence ms/champ | Candidat ms/champ |
| --- | ---: | ---: |
|1 |177,58559 |115,42187 |
|2 |177,71615 |113,45575 |
|3 |178,55178 |114,99553 |
| **Moyenne** | **177,95117** | **114,62438** |

**Temps réduit de35,587 %, accélération1,5525×.** Les trois empreintes vidéo/audio et les durées émulées sont identiques ; les deux cœurs sont fermés et les deux contextes effectivement déchargés après la mesure. Champ matériel19,968 ms à50,08013 Hz ; candidat **8,72415 champs/s et17,42038 % nominal**, sans limite et hors RML. Cette refonte n'atteint ni la réduction50 % demandée ni100 % de cadence. La machine reste ouverte ; les avances entre interactions et les coûts globaux restants nécessitent une poursuite structurelle, pas une déclaration de clôture.
Rapport ignoré : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-architecture-block-native.md`. Les cinq sources courantes ont été sauvegardées en octets avant la construction témoin et restaurées dans finally, puis le candidat a été reconstruit. Fabrique, diagnostics externes et copies DLL sont retirés après consignation ; le rapport reste conservé.

## Profil global du pointa9245eb4b
NOS80/VDU80,400004 sans média,20 champs de chauffe exclus par la pile de mesure.70 champs collectés en8050,2266 ms ; Debug IsJITOptimizerDisabled=true. Les temps échantillonnés ne remplacent pas le comparatif natif libre114,62438 ms/champ. Chaque intervalle est attribué une fois au dernier cadre Chips/Acorn ; les inclusifs restent séparés.

| Groupe | Temps propre % |
| --- | ---: |
|Sn74Ls93|13.142729|
|Autres|12.015584|
|Sn74Ls393|10.304847|
|SystemFloppyBoard|10.211913|
|Vdu80CharacterGenerator|9.215136|
|Cpu6502|8.323591|
|SystemClockGenerator|7.405354|
|AcornSystemMachine|7.318572|
|Sn74S163|7.018723|
|Sn74Ls165|4.784358|
|SystemVideo|4.495358|
|Crtc6845|3.621606|
|Via6522|1.087034|
|Intel8271|1.055194|
| **Total avant arrondi** | **100,000000** |
Méthodes principales :163.ResolveRepresentedClockPeriods4,0013 %,VDU.AdvanceStableDotClockPeriods3,9902 %,93.SkipUnobservedCka3,2391 %,163.Capture2,8481 %,393.TryGetQaRisingClockCountAt2,7910 %,93.AdvancePeriodicInputs2,7788 %. Aucune méthode unique ne porte le retard global. Le coût suit encore les très petites plages d'avance imposées par les frontières de raccordement. La prochaine refonte doit regrouper ces plages entre interactions établies, avec capacités locales de calcul de plages longues et conservation des observables ; aucune accélération supplémentaire déjà obtenue n'est annoncée. Rapports exhaustifs : artifacts/TestsMachines/Acorn/Acorn System 5/20261110-post-refactor-profile-methods.{json,md}.


## Préfixes privés CPU et rattrapage indépendant —10 novembre2026
Le raccordement de la refonte est réalisé : les vrais cycles PH0 du6502 et les phases VIA restent consommés, mais la propagation répétée du bus désélectionné et la résolution du réseau source sont différées sur un préfixe privé. Chaque accès préparé est qualifié avant son exécution ; VDU/VIA/8271, contrôleur actif, WAIT/DMA, bande active, événement lecteur et fin de ligne bornent ou refusent ce préfixe. Le contrôleur WAIT/READY et les trois163 retrouvent leurs états canoniques, puis la carte disquette est actualisée avant le prochain accès. Le rendu rejoue tous les effets dot/caractère/ROM/curseur, sans omettre de pixels.
Les8 tests autonomes de préfixe passent (1 s) : CPU/RAM, vidéo/audio, durées et comptes source/contrôleur, VIA IRQ, NMI déjà reçu, commande8271, WAIT/DMA puis reprise et modes progressif/entrelacé. Le premier passage System/carte a réussi143 contrôles en17 s avant l'ajout des3 cas WAIT/DMA/NMI ; ils sont vérifiés séparément, pas présentés comme un nouveau passage146 complet.
Comparaison native appariée dans le même processus :20 champs de chauffe puis3×20, ordre alterné, vidéo et DrainAudio chronométrés ; empreintes hors chronométrage. Même DLL Debug et même matériel ; GroupPrivateClockPeriods=false conserve exactement le parcours de référence, true sélectionne le préfixe. Aucune fréquence ni option de compilation changée.

| NOS80/VDU80,400004 sans média | Référence ms/champ | Préfixes ms/champ |
| --- | ---: | ---: |
| Fenêtre1 |113,87665|90,56639|
| Fenêtre2 |107,58486|101,05664|
| Fenêtre3 |111,78969|84,68701|
| **Moyenne** | **111,08373** | **92,10335** |

**17,087 % de temps en moins,21,680 % du nominal**, champ matériel19,968 ms, soit10,858 champs/s hors RML.1 708 870 périodes CPU privées représentées sur les60 champs mesurés. Les trois empreintes vidéo/audio/durées sont identiques, ainsi que CPU, RAM, ticks et compte source. Les fenêtres fluctuent ; le résultat moyen ne doit pas être remplacé par la meilleure fenêtre.
Second scénario autonome (vrai code6502 avec lectures statut8271/VIA, écrituresVDU, timerVIA et entrelacement) :1,80676→1,40747 ms/champ,22,100 % de réduction,25 291 périodes représentées. Sa programmation CRTC produit des champs courts de0,252 ms :17,904 % nominal, ce n'est ni un écran50 Hz ni une preuve de vitesse d'un logiciel chargé depuis disquette.
Rapport ignoré : artifacts/TestsMachines/Acorn/Acorn System 5/20261110-private-prefix-native.md. Les deux cœurs sont libérés par using même après assertion. Ce gain reste insuffisant pour terminer System5 : environ4,61 fois trop lent sur le moniteur NOS80. La poursuite doit traiter les coûts restants du parcours complet ; aucun gain attribué isolément au6502, au163, auCRTC ou au8271.
## Profil global après préfixes privés —10 novembre2026
90 champs complets NOS80/VDU80 collectés en8055,1607 ms après20 champs de chauffe exclus par la pile MeasureFields. Debug IsJITOptimizerDisabled=true ; RunFrame et DrainAudio inclus. Les durées profilées ne remplacent pas le comparatif apparié libre92,10335 ms/champ. Les temps propres sont disjoints ; les inclusifs restent séparés.

| Groupe | Temps propre % |
| --- | ---: |
|Sn74Ls93|15.791127|
|Others|15.648810|
|Vdu80CharacterGenerator|10.952126|
|Sn74Ls393|9.455014|
|SystemFloppyBoard|8.774869|
|Cpu6502|8.431855|
|SystemVideo|7.215915|
|AcornSystemMachine|6.647136|
|Sn74Ls165|5.513124|
|Crtc6845|4.222328|
|SystemClockGenerator|3.727101|
|Sn74S163|2.157195|
|Via6522|0.789915|
|Intel8271|0.673485|
| **Total avant arrondi** | **100,000000** |

Principales méthodes : VDU.AdvanceStableDotClockPeriods5,192 %,93.AdvancePeriodicInputs3,926 %,93.SkipUnobservedCka3,591 %,carte.RememberDeadline2,870 %,93.PublishDeadline2,421 %,Video.AdvanceMonochromePeriods2,310 %. Les détails exhaustifs, y compris les15,649 % classés Autres (374, tables163,138,00, bus et autres méthodes), restent dans le JSON/Markdown de preuve : aucun80 % manquant ni addition des inclusifs.
La chaîne93+393+carte représente34,021 % ; vidéo+VDU+165+CRTC27,903 %. Source+163 représente5,884 %, contre14,424 % dans le profil précédent : les préfixes réduisent effectivement ce poste. Les répartitions sont statistiques et les pourcentages relatifs ne sont pas des gains appariés. Aucun poste seul ne suffit à garantir50 % de réduction totale. Prochaine analyse : qualifier les interactions externes réellement nécessaires de la chaîne disquette avant de remplacer son transport interne répétitif ; examiner ensuite le parcours vidéo complet.
Rapports persistants ignorés : artifacts/TestsMachines/Acorn/Acorn System 5/20261110-private-prefix-profile-methods.{json,md}. Cœur et processus possédés fermés/attendus ; dossier de profilage temporaire retiré après analyse.
## Bloc séparateur différé retenu —10 novembre2026
Après correction de la restauration, **153 contrôles System/carte réussissent**, zéro échec/ignoré,18 s. Cela conserve notamment les lectures/écritures/relectures8271 et les horloges/WAIT/DMA ; les7 nouveaux cas balayent3031 chronologies de pulses et comparent chaque transition DATA WINDOW datée. Le contrôle préexistant qui observe explicitement toutes les lignes internes QB/QD conserve le transport complet dans ses deux témoins ; ses assertions de niveaux/dates sont inchangées.
Comparaison native appariée NOS80/VDU80, RAM2000–DFFF et400004 sans média :20 champs de chauffe puis3×20 en ordre alterné, rendu et DrainAudio chronométrés, empreintes hors temps. Même DLL Debug, fréquences et matériel ; seul le sélecteur interne de représentation du séparateur diffère. Les deux chemins conservent les préfixes privés précédemment vérifiés.

| Fenêtre | Transport complet ms/champ | Séparateur différé ms/champ |
| --- | ---: | ---: |
|1|84,69996|67,00028|
|2|87,45087|65,77199|
|3|85,40294|65,41775|
| **Moyenne** | **85,85126** | **66,06334** |

**23,049 % de temps en moins, accélération1,29953×**, champ matériel19,968 ms ; **15,13699 champs/s et30,226 % nominal** en Debug natif libre, hors RML. Les trois empreintes vidéo/audio/durées sont identiques, ainsi que CPU(registres/cycles), RAM, ticks et compte source. Le latch est effectivement en état différé en fin de mesure ; ce n'est pas un chemin non utilisé.
La référence de ce passage inclut déjà la correction de phaseQA et le helper93 courant : les85,85126 ms ne remplacent pas rétroactivement les92,10335 ms du passage précédent. Seul le gain apparié23,049 % est attribué à cette sélection de parcours. Aucune mesure isolée de puce n'est déduite.
Rapport ignoré : artifacts/TestsMachines/Acorn/Acorn System 5/20261110-deferred-separator-native.md. Les deux cœurs sont libérés par using après assertions. Le logiciel de mesure temporaire est ensuite supprimé et les tests reconstruits sans lui. Les opérations disquette sont couvertes par les tests de fonctionnement, mais ce relevé de vitesse concerne le moniteur sans média, pas le formatage ou un chargement. System5 demeure ouvert, environ3,31 fois trop lent ; la suite doit traiter le parcours vidéo et les autres coûts globaux, sans réduire les fonctionnalités.
## Tentative de paquets dot retirée après comparaison native —10 novembre2026
Les méthodes de paquets et le rendu par table ont passé50 contrôles165/VDU et157 contrôles System/carte. La première sélection, appliquée aussi aux mots nuls déjà traités plus simplement par le chemin stable, régresse :65,19597→68,69771 ms/champ (+5,371 %).
Après sélection réservée aux mots non stables, trois fenêtres appariées donnent68,40919/65,67889/65,91233 ms côté référence et65,70939/65,77602/65,00445 ms côté candidat, moyennes66,66680→65,49662 ms (−1,755 %). L'écart dépend principalement de la première fenêtre ; aucun gain important établi.
Les images, l'audio, CPU/RAM et comptes sont égaux, mais cela ne justifie pas de conserver le surcroît de code sans gain utile. La tentative est retirée : sources/test modifiés restaurés depuis15f62862d, deux nouveaux helpers et diagnostic externe supprimés. Les seuls tests ajoutés pour ces méthodes retirées sont également retirés ; tests antérieurs préservés. Les documents conservent le résultat historique des essais, pas une fonctionnalité actuellement livrée.
Rapports ignorés conservés :20261110-dot-packets-native.md,20261110-dot-packets-routed-native.md et TRX correspondants sous artifacts/TestsMachines/Acorn/Acorn System5 ; TRX165/VDU sous TestsChips. La référence retenue reste le bloc séparateur mesuré66,06334 ms/champ,30,226 % nominal, pas les30,487 % non retenus de cette tentative.
Prochaine architecture à examiner : périodes de caractères complètes dont les entrées et la sortie du VDU sont établies, y compris les chargements NL et leurs clocks effectives ; ne plus concentrer le travail sur le transport de seulement trois points.
## Caractères constants complets retenus —10 novembre2026
Le VDU et165 représentent désormais une période de caractère établie dans son ensemble : six clocks dot, capture374 et pulseNL20 ns. La clock effective supplémentaire créée par le relâchement NL avec CLK haut est conservée. Le rendu remplit les mêmes deux demi-cellules aux coordonnées antérieures, puis le CRTC effectue son TickCharacter ordinaire ; aucun changement de glyphes, balayage ou cadence.
50 contrôles de composants réussissent en662 ms ;155 contrôles System/carte en19 s. Les deux nouveaux contrôles machine comparent huit champs avec entrelacement0/3 et interruptionsVIA, CPU/RAM/audio, durées et comptes vidéo égaux ; les caractères représentés sont effectivement utilisés. Preuves TRX :20261110-settled-characters-final.trx sous TestsChips/Vdu80CharacterGenerator/Acorn System5 et20261110-settled-character-integration.trx sous TestsMachines/Acorn/Acorn System5.
Comparatif natif dans la même DLL Debug : NOS80/VDU80, RAM2000–DFFF,400004 sans média, seuls GroupSettledCharacters false/true diffèrent.20 champs de chauffe puis3×20 en ordre alterné ; RunFrame et DrainAudio chronométrés, empreintes hors chronométrage.

| Fenêtre | Référence ms/champ | Caractères établis ms/champ |
| --- | ---: | ---: |
|1|66,24875|59,80845|
|2|64,98711|58,52511|
|3|64,92862|58,02354|
| **Moyenne** | **65,38816** | **58,78570** |

**10,097 % de temps en moins, accélération1,11231×**, champ natif19,968 ms ; **17,01094 champs/s et33,967 % du nominal** en Debug libre hors RML.1 652 598 caractères complets représentés sur les60 champs mesurés. Les trois empreintes vidéo/audio/durée, CPU(registres/cycles), RAM, ticks et compte source sont identiques. Aucun gain isolé attribué au165, aucune fréquence ou option Debug changée. Les deux cœurs sont libérés par using.
Rapport ignoré : artifacts/TestsMachines/Acorn/Acorn System 5/20261110-settled-characters-native.md. Le diagnostic externe est supprimé après consignation et la DLL de tests reconstruite sans lui. Ce résultat concerne le moniteur sans média ; les tests de transferts disquette ne sont pas une mesure de leur débit. La tentative de paquets dot retirée reste documentée séparément. System5 reste ouvert, environ2,94 fois trop lent ; poursuivre la réduction des autres coûts globaux et les validations avant clôture.
## Profil global du point537967385 —10 novembre2026
132 champs NOS80/VDU80 collectés en8022,3786 ms après20 champs de chauffe exclus par MeasureFields ; RunFrame/DrainAudio seuls retenus, Debug IsJITOptimizerDisabled=true. Temps propres attribués une fois au dernier cadre Chips/Acorn ; inclusifs séparés. Le temps échantillonné ne remplace pas le comparatif natif libre58,78570 ms/champ.

| Composant ou raccordement | Temps propre % | ms/champ échantillonné |
| --- | ---: | ---: |
|Cpu6502|13.118722|7.971762|
|Sn74Ls393|13.034641|7.920670|
|AcornSystemMachine|10.137950|6.160457|
|SystemVideo|9.182709|5.579992|
|Vdu80CharacterGenerator|8.428219|5.121517|
|Crtc6845|6.678423|4.058231|
|SystemFloppyBoard|6.256530|3.801862|
|SystemClockGenerator|4.936642|2.999815|
|Sn74Ls374|4.805275|2.919988|
|Sn74Ls165|3.794994|2.306077|
|Sn74Ls138|3.642058|2.213144|
|SystemFastCpuBus|3.294171|2.001746|
|Sn74S163PeriodicInputs|3.193014|1.940276|
|Sn74S163|3.182247|1.933734|
|Sn74Ls74|2.056062|1.249393|
|SystemCpuClockControl|1.534578|0.932506|
|Via6522|1.057170|0.642403|
|Intel8271|0.863826|0.524915|
|Mm52116|0.264393|0.160662|
|Sn74Ls00|0.232401|0.141221|
|Cd4020B|0.231165|0.140470|
|SystemKeyboard|0.074810|0.045459|
| **Total avant arrondi** | **100,000000** | |

Aucune méthode unique ne dépasse3,143 % :393.ResolvePending3,142 %,6502.ExecuteNmosPhase2,749 %,163.ResolveRepresentedClockPeriods2,658 %,374.PublishOutputs2,649 %,6502.ClockNmos2,427 %. Le coût est réparti. Les groupes393(13,035 %) et6502(13,119 %) sont les premiers ; la liaison QD du premier compteur393 vers le second est encore transportée par fronts explicites dans le circuit READY. La prochaine refonte qualifie cette période extérieure dans393, avec ses délais et phases, avant raccordement ; ne pas attribuer13 % de gain sans mesure. Rapports exhaustifs : artifacts/TestsMachines/Acorn/Acorn System 5/20261110-settled-profile-methods.{json,md}.
## Liaison QD établie — 10 novembre 2026
Le premier canal du 74LS393 décrit son signal QD au second canal par le raccordement extérieur de la carte. Chaque canal conserve ses phases, compteurs et échéances. Les clears et changements de source révoquent la description et rétablissent les fronts explicites jusqu’à requalification ; les demandes QA du 8271 restent datées.

Validation : 54 contrôles autonomes du 393 réussis, y compris remplacement de période source et reprise explicite ; 157 contrôles System/carte réussis. TRX final : `artifacts/TestsChips/Sn74LS393/Acorn System 5/20261110-qd-periodic-wire-final.trx`.

Comparaison native Debug, mêmes DLL/ROM NOS80/VDU80 et horloges, 20 champs de chauffe puis trois paires alternées de 20 champs :

| Fenêtre | QD scalaire, ms/champ | QD décrit, ms/champ |
| --- | ---: | ---: |
| 1 | 55,89058 | 48,50899 |
| 2 | 57,47379 | 49,64622 |
| 3 | 56,67139 | 49,60304 |
| Moyenne | 56,67859 | 49,25275 |

Temps réduit de 13,102 %, soit 40,542 % du nominal et 20,303 champs/s hors interface. Vidéo, audio, durée, registres CPU, RAM et compteurs identiques pour les trois paires. Ce résultat concerne la machine entière, pas un débit isolé du 393. System 5 reste sous la cadence nominale et non terminé. Rapport : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-qd-divider-native.md`.

## Portions vidéo linéaires retenues —10 novembre2026
Le CRTC conserve ses compteurs et calcule une portion sans autre changement de signal. Son destinataire accepte les adresses successives uniquement si la RAM présente les mêmes données et si le VDU peut représenter les périodes complètes avant ses échéances. Le rendu conserve les coordonnées des demi-cellules et les mêmes RGB. Les frontières de ligne, DE/CU, synchronisations, changements de données, LP et reset reprennent le parcours ordinaire.
110 contrôles CRTC et164 contrôles System/carte réussis, zéro échec. Les nouveaux tests comparent états CRTC complets, traces datées, phases et comptes VDU, champs, audio, CPU et RAM ; skews0..3, progressif/entrelacé et écritures vidéo inclus. Rapports TRX `20261110-linear-characters.trx` sous TestsChips/MC6845 et `20261110-linear-video-integration.trx` sous TestsMachines/Acorn/Acorn System 5.

Comparaison Debug native NOS80/VDU80, RAM2000–DFFF, carte400004 sans média, mêmes DLL et horloges, tous les parcours déjà retenus actifs ; seul GroupLinearCharacters diffère.20 champs de chauffe puis3×20 en ordre alterné. RunFrame/DrainAudio chronométrés ; calcul des hashes hors chronomètre.

| Fenêtre | Caractères unitaires, ms/champ | Portion linéaire, ms/champ |
| --- | ---: | ---: |
| 1 | 51,26419 | 47,17495 |
| 2 | 50,50883 | 46,79031 |
| 3 | 51,23617 | 43,49262 |
| Moyenne | 51,00306 | 45,81929 |

Temps réduit de10,164 %, cadence43,580 % nominal,21,82487 champs/s hors interface.2 039 859 caractères effectivement représentés pendant chauffe/mesure. Les trois hashes vidéo/audio/durée, registres CPU/cycles, RAM, ticks/source, état CRTC et comptes VDU sont identiques. Deux cœurs possédés libérés par using ; aucun lancement RML ni changement de compilation Debug. Rapport `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-linear-video-native.md`. Comparaison appariée de ce bloc uniquement : les valeurs de passages antérieurs ne constituent pas sa référence. Gain retenu, System5 reste incomplet sur sa cadence ; aucun passage à la machine suivante.

## Profil global courant après portions linéaires —10 novembre2026
167 champs,8025,6878 ms hôte,19,968 ms matériels/champ, Debug DisableOptimizations=true. Chauffe20 champs exclue ; seules les piles RunFrame/DrainAudio de MeasureFields sont retenues.8025,1494 ms attribuées une seule fois au dernier appelant Acorn/Chips ; coûts inclusifs séparés dans le rapport, aucun parent ajouté à ses descendants.

| Groupe | Temps propre % | ms/champ collecté |
| --- | ---: | ---: |
|Cpu6502|16.057170|7.716239|
|AcornSystemMachine|12.765054|6.134220|
|Vdu80CharacterGenerator|8.037419|3.862365|
|SystemVideo|7.893255|3.793087|
|SystemFloppyBoard|7.397564|3.554884|
|SystemClockGenerator|6.321460|3.037764|
|Crtc6845|4.750016|2.282610|
|Sn74Ls138|4.508375|2.166490|
|SystemFastCpuBus|4.477966|2.151877|
|Sn74Ls393|4.382241|2.105877|
|Sn74Ls165|4.066384|1.954092|
|Sn74S163|4.058823|1.950459|
|Sn74S163PeriodicInputs|3.982776|1.913914|
|Sn74Ls374|2.437764|1.171462|
|Sn74Ls74|2.420082|1.162965|
|SystemCpuClockControl|2.352796|1.130631|
|Via6522|1.553167|0.746371|
|Intel8271|1.065553|0.512049|
|Sn74Ls00|0.591102|0.284053|
|Mm52116|0.336189|0.161555|
|Cd4020B|0.210638|0.101222|
|SystemKeyboard|0.201869|0.097008|
|IStableClockPeriodReceiver|0.093748|0.045050|
|IIndependentClockPeriodReceiver|0.019845|0.009537|
|AnalogTape|0.018744|0.009007|
|Total avant arrondi|100,000000||

Premier coût de méthode :163.ResolveRepresentedClockPeriods3,552 % ; CPU.ClockNmos3,135 % ; VDU.AdvanceStableDotClockPeriods2,824 % ; CPU.ExecuteNmosPhase2,751 %. Le coût reste distribué. Le découpage logiciel des portions privées à16 cycles répète les fins de portion ; la prochaine comparaison utilise les bornes réelles déjà fournies par les composants, sans modifier le CPU ni les fréquences. Ces temps collectés ne sont pas un gain apparié. Rapports exhaustifs : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-linear-profile-methods.{json,md}`.

## Causes des arrêts de portions —10 novembre2026
Retour intégral au code de production du point bfa9f8e14 après l’essai sans gain du plafond16. Sonde temporaire ensuite appliquée sans changement des bornes, avec comparaison native NOS80/VDU80/carte400004 sans média :20 champs de chauffe exclus, puis20 champs comparés avec et sans sonde en ordre alterné. Vidéo/audio/durée, CPU, RAM, ticks et compte source identiques ; les deux diagnostics ont réussi et leurs cœurs possédés ont été libérés par using.

345874 tentatives sur20 champs, dont116777 portions exécutées (5838,85/champ) et229097 refus (11454,85/champ). Les463993 portions de l’essai précédent étaient cumulées sur80 champs, et non par champ.

| Cause | Nombre sur20 champs | Par champ | Vérification ou portion complète, ms/champ | Parcours ordinaire après refus, ms/champ |
| --- | ---: | ---: | ---: | ---: |
|executed/next/via/read/r1|113211|5660.55|20.89793|0.00000|
|refused/prepared/via/read/r1|114103|5705.15|2.58478|13.23084|
|refused/bus-propagation-pending/decode-unconnected-outputs-only|114103|5705.15|2.60431|11.60355|
|executed/line-boundary|3566|178.30|0.63385|0.00000|
|refused/short/line-boundary|891|44.55|0.01175|0.10248|

Les temps de portion entière incluent son CPU/VIA/audio, sa vidéo et ses remises à jour ; ils ne sont pas le coût intrinsèque de la cause d’arrêt. Les sous-domaines détaillés dans les rapports ne s’ajoutent pas à leurs parents. Sonde active1078,6248 ms contre913,7078 ms désactivée sur20 champs :18,049 % de surcoût. Les mesures instrumentées servent à classer le travail, pas à annoncer une cadence ni une économie récupérable intégralement. Vérifications refusées104,0166 ms cumulées (5,20083 ms/champ), incluant la construction des classifications par la sonde ; cette valeur ne mesure pas le seul coût de la vérification dans le code sans sonde.

Conclusions établies :
- Les lectures VIA du registre1 (portA, clavier) sont de véritables transactions. Les acquittements et IRQ de Via6522.Read/AcknowledgePort doivent rester à leur phase CPU ; leur fréquence ne prouve pas que le VIA soit coûteux.
- Le refus suivant « bus-propagation-pending » a été affiné sans modifier le matériel :114103 cas sur114103 avec seulement des sorties non raccordées du138 en attente, aucun pendingY1, aucune échéance du contrôle, CS et DACK déjà hauts. `SystemFloppyBoard.PrivateBusPinsSettled` attend actuellement la prochaine échéance interne globale du138. Cette attente est une condition logicielle trop large ; elle n’établit aucune interaction physique à attendre sur les fils raccordés.
- Le parcours ordinaire après ce seul refus représente232,0709 ms instrumentées,11,60355 ms/champ ; il contient encore du travail matériel réel (CPU/vidéo/horloges) à représenter, donc ne pas promettre sa suppression intégrale. La suite doit distinguer les échéances internes et celles des sorties raccordées dans la puce, puis raccorder la carte à ces dernières, sans supprimer aucun délai interne.
-3566 portions se terminent à une frontière de ligne et891 tentatives y sont trop courtes ; ces limites protègent le début de champ. Aucune activité de lecteur, attente/DMA ou bande active n’a causé d’arrêt dans ce scénario au repos ; cela ne vaut pas couverture de ces autres scénarios.

Rapports exhaustifs conservés : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-prefix-causes.{json,md,trx}` et `20261110-prefix-causes-refined.{json,md,trx}`. Les deux passages donnent les mêmes effectifs ; la seconde sonde distingue les sorties non raccordées. Aucun gain de performance produit par ce diagnostic ni modification d’architecture conservée.

## Échéances raccordées du décodeur retenues —10 novembre2026
Le schéma400,004 figure8/PDF108 et le brochage TI page1 ont été inspectés : IC4/Y1 (broche14) seul est relié au8271/24 et à IC2/12 ; les sept autres sorties ne sont pas exportées. Le tableau complet des destinations se trouve dans fdc400004.md. Aucun oubli de raccordement de ce boîtier établi.
Le138 conserve toutes ses propagations internes et offre séparément NextOutputEventNanoseconds. La carte reçoit ce dernier écouteur pour son masqueY1 et synchronise toujours la puce avant chaque nouvelle entrée de bus. Les changements de CS/DACK et leurs effets restent datés ; l’attente liée uniquement aux sorties non raccordées ne bloque plus les périodes indépendantes. Le chemin global reste sélectionnable en interne pour comparaison, sans nouvelle option utilisateur ni matériel. Les écouteurs possédés sont détachés à Dispose.

19 contrôles138 réussis (519 ms) puis165 contrôles System/carte réussis (20 s), zéro échec/ignoré. Le nouveau contrôle compare CS/DACK/READY/NMI/compte8271 nanoseconde par nanoseconde, sur sélections/désélections et miroirs, et distingue la propagation interne non raccordée du bus réellement disponible. Les contrôles de transferts préexistants sont conservés.
Comparaison native Debug sans sonde, mêmes DLL, NOS80/VDU80, RAM2000–DFFF,400004 sans média ; seul le transport d’échéance globale/observée diffère.20 champs de chauffe puis3×20 appariés en ordre alterné, RunFrame/DrainAudio chronométrés et hashes hors temps.

| Fenêtre | Échéance globale, ms/champ | Échéance raccordée, ms/champ |
| --- | ---: | ---: |
| 1 | 45,15266 | 33,90353 |
| 2 | 44,78374 | 34,06753 |
| 3 | 47,62626 | 34,41513 |
| Moyenne | 45,85422 | 34,12873 |

Temps réduit de25,571 %, vitesse×1,34357. Champ matériel19,968 ms,50,08013 Hz ; candidat29,30083 champs/s,58,508 % nominal, environ1,170 MHz émulé pour le CPU2 MHz. Vidéo/audio/durée, CPU/cycles, RAM, ticks/source et niveaux/connus des huit sorties138 identiques sur les trois fenêtres. Les sorties internes différées sont matérialisées avant cette observation, hors chronométrage. Chaque cœur est libéré via using. Aucun changement des options Debug ni des fréquences ; aucune capture RML ni mesure Release déduite. Rapport `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-decoder-deadline-native.md` et TRX associé. Gain retenu ; System5 reste sous sa cadence nominale.
La règle générale est ajoutée au tableau Application aux composants pour les prochaines machines et puces. L’inventaire des autres machines et leurs corrections sont reportés à la demande explicite de l’utilisateur ; aucune modification de ces autres machines réalisée.

## Lecture VIA par le mécanisme de périodes indépendantes —10 novembre2026
Le VIA conserve sa lecture effective du portA, ses acquittements et ses IRQ dans le cycle CPU normal. `SystemFastCpuBus` qualifie uniquement une lecture préparée du portA ; `AcornSystemMachine` reçoit la propriété des sorties de contrôle par ControlOutputsChanged, initialisée au raccordement et détachée à Dispose. Aucun sondage de registre VIA n’est ajouté. La représentation est refusée si une sortie de contrôle est pilotée, si WAIT/DMA ne sont pas qualifiés, si la bande est active, si le contrôleur/bus disquette a une échéance ou si une interaction de lecteur arrive dans ce cycle.
Le même mécanisme de portions indépendantes est réutilisé, borné ici à un seul cycle : phases CPU/PH2-VIA, clavier et audio effectivement exécutés ; helpers vidéo et réseau d’horloges existants pour les récepteurs indépendants ; prochaine adresse présentée après cette période au bon temps source. Ne pas regrouper plusieurs changements d’adresse VIA : les propagations internes non raccordées du138 doivent également rester retrouvables. Aucun changement de CPU, VIA, fréquences ou options Debug. Le compteur historique de cycles représentés inclut ce domaine CPU/VIA ; RepresentedViaReadPeriods en distingue les lectures seules.
Le schémaCPU400,005 PDF62, détail `System5-cpu-via-clock-62.png`, confirme PH2 du CPU vers VIA et IRQ vers CPU ; CA/CB aboutissent au connecteurPL1. La qualification conservative sans sorties de contrôle pilotées ne prétend pas rendre ces broches inutilisées pour toutes les configurations ; les modes de sortie conservent le parcours ordinaire complet. Les fonctions et callbacks VIA restent celles du composant existant.

167 contrôles System/carte réussis (19 s), puis les2 cas progressif/entrelacé renforcés passent (362 ms). Ils confrontent les états VIA complets, champs/CPU/RAM/audio, changements clavier, compteurs VDU/source, notifications vidéo à leurs dates locales, et refus/reprise après programmation de CA2 en sortie impulsionnelle. Les scénarios WAIT/DMA et transferts précédents sont conservés. TRX `20261110-via-read-integration.trx` et `20261110-via-read-video-events.trx`, sous les captures System5.

Mesure native Debug, NOS80/VDU80, RAM2000–DFFF,400004 sans média, même DLL et seul GroupViaReadPeriods différent.20 champs de chauffe puis3×20 alternés ; RunFrame/DrainAudio chronométrés, hashes hors temps.

| Fenêtre | Lecture par parcours ordinaire, ms/champ | Cycle indépendant, ms/champ |
| --- | ---: | ---: |
| 1 | 33,80010 | 31,39595 |
| 2 | 34,27853 | 30,11957 |
| 3 | 33,28066 | 30,01901 |
| Moyenne | 33,78643 | 30,51151 |

Temps réduit de9,693 %,65,444 % nominal,32,77451 champs/s hors interface (champ19,968 ms,50,08013 Hz), environ1,309 MHz CPU équivalents pour les2 MHz réels.453088 lectures représentées pendant80 champs chauffe/mesure. Les trois hashes vidéo/audio/durée, CPU/cycles, RAM, ticks/source, état VIA et comptes VDU sont identiques. Cœurs libérés via using, aucune fenêtre RML ni compilation Release. Gain modéré mais retrouvé dans les trois fenêtres et conservé ; ce passage ne termine pas System5 ni sa cadence. Rapport `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-via-read-native.md`. Le parcours ordinaire demeure la référence interne et traite intégralement les cas non qualifiés. Aucune reprise d’autres machines ou puces.

## Répartition vidéo et coût des cycles VIA —10 novembre2026
À la demande utilisateur, l’essai de réutilisation du décodage mémoire a été retiré sans compilation, tests ni mesure. Les deux sources de cet essai ont été rétablies depuis26a88f94f avant ce diagnostic. Aucun gain mémoire annoncé.
Profil natif NOS80/VDU80, RAM2000–DFFF,400004 sans média, Debug inchangé (DisableOptimizations=true).20 champs de chauffe puis8 champs appariés avec/sans sonde, vidéo/audio/durée/CPU/RAM/état VIA identiques ; puis245 champs complets sur8009,2153 ms de RunFrame/DrainAudio. Unités Speedscope milliseconds contrôlées. Calibration8 champs :256,7001 ms avec sonde,256,8216 ms sans ;−0,047 % est une variation à cette précision, pas un gain démontré. Les temps de collecte ne remplacent pas la dernière cadence native appariée30,51151 ms/champ. Deux cœurs et le processus de collecte possédés fermés/attendus/libérés via using/finally.

| Vidéo sur245 champs | Périodes | Part |
| --- | ---: | ---: |
| Caractères en portions linéaires | 7549421 | 77.15836 % |
| Autres caractères complets établis | 1902283 | 19.44216 % |
| Caractères hors de ces deux représentations | 332616 | 3.39948 % |
| Total caractères physiques | 9784320 |100 %|
| Points représentés par les caractères complets | 56710224 | 96.60052 % |
| Autres périodes dot stables | 1734025 | 2.95375 % |
| Montées dot traitées individuellement | 261671 | 0.44573 % |
| Total périodes dot physiques | 58705920 |100 %|

La somme calculée des points est exactement58705920, égale au compte physique VDU, sans tolérance. Les caractères complets incluent des traitements d’un seul caractère :96,60052 % ne signifie donc pas96,6 % de longues portions. Le sous-total linéaire77,15836 % identifie les portions de plusieurs caractères. Dans le récepteur indépendant,9773400 caractères ont été offerts,321696 ont suivi la boucle par caractère ;1990945 offres linéaires et2223979 offres de caractère établi ont été évaluées. Les offres ne comptent pas des acceptations de portions ; les compteurs de périodes acceptées sont distincts.

| Partie de l’exécution | Temps propre % | ms/champ collecté |
| --- | ---: | ---: |
|PrivateMemory|65.836061|21.519689|
|VIA|29.159054|9.531156|
|Other|5.004885|1.635936|
|Total|100,000000||

Les repères NoInlining VIA/mémoire appellent le même corps de traitement, avec les mêmes gardes. Chaque intervalle est attribué une seule fois au dernier appelant Chips/Acorn ; la partie VIA comprend tout son CPU/vidéo/horloges/bus, pas le coût intrinsèque de Via6522.Read.1397760 lectures VIA ont été représentées pendant la collecte, soit5705,14 par champ. Les compteurs de référence comptent9784320 cycles ;9773400 sont représentés dans le domaine CPU/VIA, dont ces lectures.

| Coût autour du VIA | ms/champ | Part de cette branche |
| --- | ---: | ---: |
|ClockCatchup|2.130077|22.348565 %|
|CpuViaKeyboardAudio|2.092013|21.949209 %|
|Video|2.061697|21.631133 %|
|FloppyComponents|1.971315|20.682858 %|
|BusWiring|0.702673|7.372380 %|
|CoordinationAndOther|0.573380|6.015854 %|
|Total|9,531156|100 %|

| Composant, coût propre | % du total | ms/champ |
| --- | ---: | ---: |
|Cpu6502|23.815393|7.784485|
|AcornSystemMachine|15.421898|5.040922|
|SystemVideo|9.107868|2.977069|
|Sn74Ls138|6.112459|1.997966|
|SystemFastCpuBus|5.983158|1.955702|
|Sn74S163PeriodicInputs|5.399411|1.764894|
|Crtc6845|4.409419|1.441297|
|Vdu80CharacterGenerator|4.166695|1.361958|
|SystemFloppyBoard|3.898553|1.274311|
|Sn74S163|3.536443|1.155949|
|Sn74Ls374|3.295306|1.077129|
|Sn74Ls393|3.086777|1.008968|
|Via6522|2.261502|0.739212|
|SystemCpuClockControl|1.956130|0.639396|
|SystemClockGenerator|1.890235|0.617857|
|Sn74Ls74|1.603857|0.524249|
|Sn74Ls165|1.574333|0.514599|
|Intel8271|1.118669|0.365657|
|Mm52116|0.543365|0.177608|
|Cd4020B|0.351722|0.114967|
|SystemKeyboard|0.223914|0.073190|
|IIndependentClockPeriodReceiver|0.168206|0.054981|
|Sn74Ls138Timing|0.055962|0.018292|
|SystemKeyboardRepeat|0.018726|0.006121|
|Total|100 %||

Conclusion : les fronts dot unitaires ne représentent plus que0,44573 % des périodes, donc leur nombre ne soutient pas une nouvelle refonte point par point en priorité. Le travail autour du VIA se répartit principalement en remises à jour horloges2,1301 ms, CPU/VIA/clavier/audio2,0920 ms, vidéo2,0617 ms et composants disquette1,9713 ms par champ. La priorité reste l’amortissement des mises à jour indépendantes autour des transactions, en conservant chaque adresse reçue, lecture, acquittement, IRQ et événement à sa date. Aucune économie intégrale de ces9,53 ms n’est promise : ils comprennent du travail matériel utile. La réutilisation du décodage demeure reportée ; aucune architecture supplémentaire n’a été conservée de ce diagnostic.
Limite : scénario au repos sans média. Les durées de lecture/écriture disquette et leurs interactions doivent être mesurées séparément avec les scénarios de transfert existants ; ces valeurs ne leur sont pas attribuées. Aucune reprise des autres machines. Rapports complets persistants `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-video-via-profile.{json,md}` : toutes les méthodes propres, inclusifs séparés et attribution par branche, aucun parent ajouté à ses descendants.

### Résolution commune différée entre observations — 10 novembre 2026
Le générateur cumule les portions complètes de douze périodes source déjà représentées par leurs destinataires, puis résout ses trois compteurs canoniques avant une observation pertinente, un nouveau raccordement ou une progression ordinaire. Les temps source et niveaux raccordés 12/2 MHz restent exacts immédiatement. Aucun chemin VIA supplémentaire ni changement du câblage de la machine. Les autres sorties observées ou les portions non complètes conservent la résolution immédiate.
Validation Debug : 60 contrôles autonomes SystemClock réussis (182 ms), puis 170 contrôles System/carte/banc natif réussis (19 s), dont les deux nouveaux cas comparant la machine avec résolution immédiate/différée. Vidéo/audio/durées, registres et cycles CPU, RAM, temps source, ticks et état VIA égaux. Retour aux sorties supplémentaires et fronts explicites vérifié dans les contrôles du composant.
Mesure native appariée isolée : NOS80/VDU80, RAM2000–DFFF, carte400004 sans média,20 champs de chauffe puis3×20, ordre alterné, même DLL. Fenêtres immédiat→différé en ms/champ :30,51730→28,04264 ;30,75249→27,43949 ;34,78871→27,91811. Moyennes32,01950→27,80008, soit13,1777 % de temps en moins ;35,9711 champs calculés/s et71,8271 % du nominal (champ19,968 ms). Les trois fenêtres progressent ; la variation de la troisième référence interdit d’attribuer une précision supérieure à ces mesures. Rapport `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-deferred-source-native.md`, TRX `20261110-deferred-native.trx`.
La première mesure exécutée avec les autres tests variait fortement (37,72585→32,82611 ms/champ) ; elle est conservée dans `20261110-deferred-source-concurrent.md` mais ne sert pas de référence au gain retenu. Le gain réduit le travail logiciel commun, sans promettre 100 %. Aucun résultat de lecture/écriture active de disquette n’est déduit de ce scénario sans média ; ce contrôle reste à faire. System5 demeure non terminé pour la cadence.

### Transferts CPU réels avec média actif — 10 novembre 2026
Deux contrôles autonomes nouveaux comparent la résolution source immédiate/différée sur80 champs chacun : un secteur FM128 octets, puis dix secteurs. Ils exécutent un programme6502 via les adresses réelles0A00/0A01/0A04, buffers et strobes ; attente READY depuis l'entrée du8271, commandes simples/multiples, polling des demandes non-DMA, entrée/retour NMI réels. La vidéo VDU80 est programmée sur128 caractères par ligne,312 lignes, champ19,968 ms à2 MHz ; ROM/programme de test synthétiques, distincts du NOS80. Les supports sont les MemoryTrackDisk existants en mémoire ; aucune image utilisateur modifiée. Première correction du banc : le handler avait été placé à0600 hors RAM montée ; il réside désormais àFF00 en ROM, sans modification de production.
Résultat :2 contrôles réussis en23 s, vidéo/audio/durées/RAM/registres/cycles CPU et nombres d'octets identiques après chaque champ. Pour dix secteurs :1280 octets5A lus,1280 octetsA6 écrits puis1280 octetsA6 relus, trois résultats0 et tous les secteurs de la piste physique décodés enA6. TRX : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-machine-transfers.trx`.
Mesure isolée Debug :80 champs de chauffe du scénario puis trois paires neuves de80 champs, ordre alterné par champ ; RunFrame et DrainAudio chronométrés, comparaisons hors temps. Source immédiate→différée, moyennes par paire ms/champ :75,30463→75,33853 ;76,26053→75,81176 ;78,61150→77,35844. Gain faible et irrégulier de ce seul mécanisme pendant ce scénario actif ; aucune nouvelle optimisation de production réalisée ici. Le banc réussit en43 s, TRX `20261110-active-disk-multiple.trx`.

| Phase observée | Champs par mode, trois paires | Immédiat ms/champ | Différé ms/champ | Vitesse nominale différée |
|---|---:|---:|---:|---:|
| Démarrage moteur et préparation |42|115,04859|114,04931|17,508 %|
| Commande de lecture |48|153,18056|152,85544|13,063 %|
| Commande d'écriture |15|176,91291|179,01332|11,154 %|
| Frontière lecture/écriture |3|191,48930|188,92263|10,569 %|
| Transferts terminés |132|22,73685|21,98181|90,839 %|

Ces catégories attribuent chaque champ une seule fois selon activité avant/après et octets transférés ; les transitions mixtes sont séparées. Elles mesurent un programme scrutant continuellement le registre d'état, pas le coût intrinsèque du8271 ni un débit de logiciel NOS80. Le rapport `20261110-active-disk.md` conserve le protocole et les fenêtres. Première mesure à un seul secteur conservée dans `20261110-active-disk-single.md` : commande de lecture148,62115 ms/champ, repos26,65801 ; écriture trop courte, seulement un champ mixte par paire, donc aucun coût d'écriture isolé déduit de cette première mesure.
Le fonctionnement des transferts est vérifié ; la cadence active demeure insuffisante. La garde CpuClockCanRunIndependently de la carte refuse actuellement le regroupement dès que le8271 annonce une échéance ; toute évolution devra conserver son instant, les demandes de données et NMI. Ce constat ne chiffre pas encore la part récupérable ni ne justifie de supprimer cette garde sans borne équivalente.

### Domaines indépendants pendant les commandes disquette — 10 novembre 2026
Le repli global sur toute échéance du 8271 est remplacé, pour les préfixes déjà qualifiés du domaine CPU, par l'avancement de la carte disquette aux positions source exactes précédant chaque front PH0 montant et descendant. La carte conserve ses clocks, événements et propagations ; ses changements NMI atteignent le CPU avant les mêmes phases que dans le parcours ordinaire. Le générateur et la vidéo utilisent leurs représentations indépendantes déjà vérifiées. Aucun ajout au CPU ou au8271, aucune fréquence modifiée ; garde de bus stabilisé, accès périphériques effectifs, WAIT/DMA et bornes du chemin au repos conservés. Ce mécanisme étend le préfixe commun, sans créer de traitement particulier par lecture. Fichiers : AcornSystemMachine.Clock.cs et AcornSystemMachine.PrivateClock.cs ; témoin interne GroupActiveFloppyPeriods=false rétablit l'ancien repli.
Validation :172 contrôles System/carte réussis en37 s. Les trois cas de transfert ont ensuite été renforcés pour vérifier l'utilisation effective des périodes représentées :3 réussites en33 s, un/dix secteurs, résolution source immédiate/différée. Comparaisons à chaque champ : vidéo, audio, durées, RAM, registres/cycles CPU, nombres d'octets ; chaque notification NMI conserve sa date nanoseconde sur la carte, ses cycles/ticks et la phase PH2 du CPU. Données et résultats des commandes inchangés :1280 octets lus/écrits/relus et dix secteurs physiques enA6. TRX `20261110-active-clock-integration.trx` et `20261110-active-edges.trx`, sous les captures System5.
Mesure isolée avec la même DLL : seul GroupActiveFloppyPeriods diffère ; source différée des deux côtés. Programme/ROM de test synthétiques, dix secteurs FM128 octets, VDU80 progressif, champ19,968 ms ;80 champs de chauffe puis trois paires neuves de80, ordre alterné par champ. RunFrame et DrainAudio inclus, vérifications exclues. Fenêtres ancien→nouveau en ms/champ :76,11898→62,15286 ;76,81273→62,05995 ;75,47201→61,44939. Moyennes76,13457→61,88740, soit environ18,71 % de temps en moins sur l'ensemble du scénario. Banc réussi en38 s, rapport `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-active-clock-native.md`, TRX `20261110-active-clock-native.trx`.

| Phase | Champs par mode | Ancien ms/champ | Nouveau ms/champ | Réduction du temps | Vitesse nominale nouvelle |
|---|---:|---:|---:|---:|---:|
| Démarrage moteur et préparation |42|114,98113|102,67530|10,702 %|19,448 %|
| Lecture |48|152,97658|108,79667|28,880 %|18,354 %|
| Écriture |15|175,85759|130,99916|25,508 %|15,243 %|
| Frontière lecture/écriture |3|184,90793|137,15087|25,827 %|14,559 %|
| Transferts terminés |132|22,02748|22,28737|-1,180 %|89,593 %|

Gain retenu : les trois paires s'améliorent et les effets matériels vérifiés restent identiques. La variation au repos ne démontre aucun gain dans cette phase. Les catégories sont celles du banc précédent, chaque champ compté une seule fois ; programme scrutant le registre d'état, distinct d'un logiciel NOS80 et des FPS présentés par RML. Les commandes actives restent trop lentes : ce gain ne clôture pas System5 et ne chiffre pas les coûts restant à profiler dans la carte et ses composants. Aucun résultat Release ni nouvelle validation RML revendiqués.

### Profil complet avec média actif après aecf9e9ba — 10 novembre 2026
Collecte EventPipe sampled-thread-time dans un processus console isolé, mêmes DLL Debug et fixture dix secteurs que les contrôles autonomes ; aucun changement de production.80 champs de chauffe puis trois scénarios neufs de80 champs, tous validés après exécution. RunFrame et DrainAudio dans quatre repères NoInlining ; les allocations, comparaisons et créations hors de ces repères sont exclues.240 champs collectés,15 698,611 ms attribués ; temps hôte mesuré15 701,033 ms. Attribution exclusive au dernier appelant du dépôt, comprenant explicitement MediaEngine, SDK et disque de test ; descendants framework/natifs imputés à cet appelant. Coûts inclusifs séparés, aucune addition parent/enfant. Les sommes sont vérifiées par phase et au total ; toutes les méthodes et inclusifs sont dans le JSON, toutes les classes et les30 premières méthodes avec reste explicite dans le MD.
Les phases sont qualifiées avant l'entrée du champ, donc un champ de transition reste dans sa phase initiale. Cette attribution diffère volontairement des catégories avant/après du banc apparié : elle sert à localiser les coûts et ne doit pas remplacer ses mesures. Préparation54 champs/6119,459 ms hôte, lecture45/5225,896, écriture9/1320,706, repos132/3034,972. Temps collectés, pas un gain de performance ni des FPS RML.

| Phase | Composant | Part exclusive attribuée | ms/champ de cette phase |
|---|---|---:|---:|
| Lecture |SN74LS393|28,535 %|33,1535|
| Lecture |SN74LS93|17,253 %|20,0457|
| Lecture |SystemFloppyBoard|12,712 %|14,7699|
| Lecture |Cpu6502|8,988 %|10,4425|
| Écriture |MediaDriveMechanics|40,160 %|58,8449|
| Écriture |FluxBitReader|15,009 %|21,9928|
| Écriture |IsoFmDecoder|11,510 %|16,8657|
| Écriture |SN74LS393|6,564 %|9,6182|
| Repos |Cpu6502|30,336 %|6,9741|
| Repos |AcornSystemMachine|14,767 %|3,3949|

Ces lignes sont un extrait : le rapport complet totalise100 % sans reste masqué. En écriture, SetMotor à lui seul représente39,817 % ; la lecture de LinesChanged confirme que chaque front WriteData rappelle UpdateMechanics puis SetMotor et SetActivity pour les deux lecteurs, même si moteur/sélection/écriture sont inchangés. SetMotor conserve sa sémantique de verrouillage/notification audio dans MediaEngine ; le raccordement doit lui transmettre les vrais changements et initialiser chaque média monté, plutôt que ces répétitions. Le remplacement et décodage du flux contient aussi du coût propre à MemoryTrackDisk, qui redécode après chaque fenêtre : ces parts ne sont pas attribuées sans preuve aux fichiers images réels. En lecture, les aides périodiques du393 et du93 restent les principaux coûts ; aucune économie totale de ces parts n'est promise.
Rapports persistants : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-active-profile.json` et `.md`. Le collecteur et les cœurs se sont terminés normalement ; processus attendu et libéré dans finally, supports/cœurs libérés par using. Dossier temporaire et trace brute retirés après consignation. Aucune modification de production attribuée à cette collecte.

### Essai mécanique non retenu — 10 novembre 2026
La réduction des appels SetMotor/SetActivity inchangés a été implémentée et vérifiée avec une sonde mécanique sur les deux ports :200 fronts WriteData sans nouvelle commande moteur/activité, mêmes fenêtres de flux ; montage, arrêt et écriture continue à l'index couverts.175 contrôles réussis en37 s. Toutefois la comparaison native isolée ne démontre pas de gain utile global. Même DLL, seule répétition mécanique faux/vrai,80 champs de chauffe puis3 paires alternées de80 :62,40341→62,42592 ;61,40362→61,36421 ;61,78546→62,00070 ms/champ. L'écriture seule138,90824→136,33241 ms/champ (-1,854 %) et les champs mixtes147,88157→142,41573 (-3,696 %) ne compensent pas les variations des autres phases. Résultats et octets identiques ; banc réussi en35 s, rapport `20261110-mechanics-notifications.md`, TRX `20261110-mechanics-native.trx`.
Tentative retirée : SystemFloppyDrive.cs, SystemFloppyDrive.Media.cs et SystemFloppyBoardTests.cs rétablis exactement depuis aecf9e9ba. Les trois tests ajoutés à cette tentative et son banc temporaire sont retirés ; les contrôles de flux et de transferts CPU préexistants restent conservés. Aucun changement MediaEngine et aucun gain annoncé, aucune nouvelle ligne d'optimisation réutilisable ajoutée pour cet essai.
Limite importante du profil : dotnet-sampled-thread-time échantillonne les piles des threads pour estimer du temps mural, pas exclusivement des cycles CPU. Microsoft précise que l'ancien nom cpu-sampling était trompeur car les threads sont échantillonnés indépendamment de leur utilisation CPU : https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace#dotnet-trace-collect . Les39,817 % attribués à SetMotor constituent une piste de diagnostic ; l'essai apparié contredit l'hypothèse d'une économie proportionnelle par suppression de ces appels dans ce montage. La raison précise de cette divergence n'est pas établie ; ne pas remplacer cette incertitude par une attribution certaine au moteur ou au CPU.

### Contrôle de l'attribution CPU — 10 novembre 2026
Les39,817 % précédemment attribués à SetMotor ne représentent pas son coût récupérable. En écriture,51 219 appels réels prennent4,3778 ms sur1 278,6597 ms mesurées sans profileur, soit0,3424 % et85,5 ns/appel ; le chronométrage augmente lui-même le temps de phase d'environ2,42 %. Aucune contention CLR relevée sur le thread ; temps utilisateur+noyau1 265,625 ms, à la granularité de GetThreadTimes. Avec le profileur CLR, durée directe16,6178 ms sur1 421,826 ms (1,1688 %). La cause exacte de l'attribution initiale reste non isolée ; aucune défaillance du profileur n'est affirmée et aucun changement MediaEngine conservé.
Les compteurs sont vérifiés séparément :45 champs de lecture,1 809 084 appels393.AdvanceTo et2 957 814 appels93.AdvanceTo. La sonde par appel ajoute25 % ; son échantillonnage à1/64 ajoute encore18,306 % (environ22,03 ms/champ). L'estimation77,20 ms/champ pour leurs temps propres ne valide donc pas précisément les45,79 % initiaux. Tous les Services instrumentés ont été rétablis avant la capture Windows.
La relance WPR administrateur demandée a réussi, sans changement de politique Windows. Premier relevé natif sans chronométrage :5 195 échantillons en lecture, deux piles manquantes,0,154 % sans appelant du dépôt identifié ;393+93 représentent44,774 % des attributions propres. Le passage chronométré retrouve44,780 %. Aucun échantillon SetMotor sur les1 324/1 342 échantillons d'écriture, ce qui ne signifie pas un coût nul. Cependant WPR signale391 177 événements perdus globalement : ces pourcentages restent provisoires. Une seconde capture CPU limitée est préparée pour éviter les fournisseurs sans rapport et contrôler cette limite. Le premier enregistrement est terminé et arrêté.
Rapport détaillé et limites : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-direct-cost.md` ; données directes et profil natif dans le même dossier. Aucune optimisation nouvelle ni cadence finale revendiquée ; sources temporaires et traces à retirer après la dernière collecte, puis reconstruction sans sondes.

### Échéances série utiles du8271 — 10 novembre2026
Le contrôleur conserve désormais localement le calcul de la prochaine frontière de lecture (bit ou expiration DMA) ; les cycles sans effet intermédiaire mettent à jour directement les compteurs et fractions avant cette frontière. L'écriture et les buffers de bus restent unitaires. Les entrées de flux et acquittements réannoncent leur échéance. Aucun changement de fréquence, NMI, donnée ou fonction ; câblage System5 inchangé. Le témoin interne GroupSerialReadClocks=false permet la comparaison au réarmement précédent.
Contrôles :165 tests8271 autonomes réussis, puis5 cas de transferts6502 réels réussis en47 s (un/dix secteurs et variantes source). Comparaisons par champ : CPU, RAM, vidéo, audio, nombres d'octets, flux écrits et notifications NMI avec date/phase/cycles. Lecture5A, écriture/relectureA6, trois résultats0 conservés. Aucun test nouveau de sauvegarde ou restauration.
Mesure Debug isolée :80 champs de chauffe puis3paires neuves de80, ordre alterné par champ ; RunFrame/DrainAudio inclus, comparaisons exclues. Ancien→nouveau en ms/champ par paire :62,91877→56,26639 ;64,56140→57,36313 ;65,40166→56,83182. Moyenne64,29394→56,82045, soit11,624 % de temps en moins ; gain retenu dans les trois paires. Chaque scénario passe de1 096 850 à305 656 notifications d'échéance8271 (-72,13 %), sans supprimer de transition matérielle.

| Phase du contrôleur avant le champ | Champs/mode | Ancien ms/champ | Nouveau ms/champ | Temps en moins | Nouveau nominal |
|---|---:|---:|---:|---:|---:|
| Inactif, attente READY et transfert terminé inclus |165|39,12532|38,80747|0,812 %|51,454 %|
| Lecture |45|113,53530|85,86952|24,368 %|23,254 %|
| Écriture, recherche d'en-tête incluse |30|128,85933|112,31824|12,837 %|17,778 %|

Attention : les phases sont classées avant chaque champ ; les165 champs « inactif » incluent la préparation mécanique lorsque le CPU attend READY. Ils ne mesurent pas le seul repos NOS80. Le scénario utilise la ROM/programme synthétiques6502 et dix secteurs FM128 du banc autonome, pas un logiciel NOS80 ni les FPS RML. Le gain en écriture provient notamment de la lecture préalable de l'en-tête ; la sérialisation écrite reste inchangée. Le total correspond à35,14 % de la cadence19,968 ms/champ et environ17,60 champs calculés/s ; la cadence active reste insuffisante et System5 demeure ouvert.
Rapports : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-serial-clock-native.{json,md}`, TRX transferts `20261110-serial-event-transfers.trx`, puce `TestsChips/Intel8271/Acorn System 5/20261110-serial-event-chip.trx`. Données/états identiques et vérifications hors mesure. Le banc temporel temporaire a été supprimé après consignation ; reconstruction et contrôles d'intégration sans lui en cours. Aucun changement App, SDK ou MediaEngine.
Contrôles finaux sans banc temporaire :200 cas System/carte/relai8271 réussis en7 s, en plus des5 comparaisons de transferts et165 contrôles autonomes. Assemblies locales reconstruites après retrait du banc :0 erreur,4 avertissements préexistants. TRX `20261110-serial-event-integration.trx`. Aucun processus/cœur possédé ni capture WPR en cours. Le bloc d'optimisation est vérifié ; System5 n'est pas déclaré terminé.

### Essai écriture aux échéances non retenu — 10 novembre2026
Extension écriture du regroupement8271 testée :173 cas autonomes passent (dont8 nouveaux : formatage et fronts datés à rapports de clocks différents) et2comparaisons CPU un/dix secteurs passent en17 s. Même RAM/CPU/vidéo/audio, données et dates NMI. Les retombées WriteData, demi-cellules et fermeture de porte ont été conservées dans cet essai ; aucune modification de fréquence ou de MediaEngine.
Mesure Debug isolée sur la même DLL : lecture groupée active dans les deux modes, seul GroupSerialWriteClocks faux/vrai.80 champs de chauffe puis3paires neuves de80, ordre alterné par champ, RunFrame/DrainAudio inclus. Moyennes ancien→nouveau par paire en ms/champ :55,18552→54,25101 ;55,51799→54,06299 ;56,23364→54,26136. Total55,64572→54,19178 (-2,613 %). Phase écriture, recherche d'en-tête incluse :111,31394→102,30537 (-8,093 %) ; lecture86,27532→85,05311 (-1,417 %, pas de changement fonctionnel dans ce trajet) ; contrôleur inactif avec attente READY37,17069→37,02714 (-0,386 %). Réannonces d'échéance par scénario305 656→167 436 (-45,22 %).
Décision : gain global trop faible pour conserver cette complexité. Intel8271.Clock.cs, ClockTests.cs, SerialWriteTests.cs et AcornEmuSystemDiskTransferTests.cs rétablis exactement depuis3d7726b8f ; banc temporaire et ses huit tests spécifiques retirés. Le regroupement lecture déjà committé reste conservé. Aucun nouveau conseil d'optimisation ajouté aux directives. Rapports persistants `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-write-clock-native.{json,md}`, TRX `20261110-write-clock-native.trx` et `20261110-write-event-transfers.trx` ; preuve puce dans TestsChips/Intel8271/Acorn System5/20261110-write-event-chip-final.trx. Ces mesures ne sont pas des FPS RML ni un débit NOS80. Reconstruction sans essai à terminer ; System5 reste ouvert.
Retrait terminé et vérifié : aucune différence src/tests avec3d7726b8f ; Chips et Acorn reconstruits sans erreur/avertissement, tests locaux reconstruits sans erreur avec4 avertissements préexistants.200 contrôles System/carte passent de nouveau en7 s sur ces assemblies restaurées (`20261110-write-trial-restored.trx`). Aucun banc temporaire ni processus de test conservé. Aucune modification de production retenue, donc aucun petit commit des seuls documents ; ces résultats restent avec le suivi pour le prochain bloc utile.

### NOS80 actuel, logiciel réel et activité du lecteur — 10 novembre2026
Machine construite par SystemFunctions.Create avec NOS80.bin8K et VDU80.bin2K externes du dossier firmware utilisateur,49Kio RAM selon la fabrique, carte400004 et lecteurs ss80. Image `F:/Retro/Acorn System/System BASIC.ssd` ouverte en lecture seule par EmulationSdk.MediaProcessing/MediaEngine ; aucune injection RAM/CPU ni fenêtre RML. CAT et BASIC envoyés par les touches/HostCharacters du clavier existant, puis PRINT12+34. Captures catalogue et résultat46 inspectées ; les octets sont512 pourCAT et4096 pourBASIC. Le premier seuil du banc attendait à tort4608 octets supplémentaires pourBASIC : ce total incluait déjàCAT. Seuil corrigé dans le diagnostic seulement ; aucun défaut de production constaté. Premier relevé et échec du banc conservés sous `20261110-current-nos80-initial.*`.
Trois collectes réussies ensuite : première collecte corrigée, détail moteur, puis détail complet moteur/sélection/contrôleur. Debug inchangé,40 champs de chauffe,3 fenêtres de40 par scénario stable ; RunFrame et drainage audio inclus, collecte des états hors chronomètre, aucun profilage CLR ou WPR. Dernière collecte : cadence vidéo matérielle50,080 Hz, compte CPU et référence égaux (2MHz matériels). Pas de FPS RML revendiqué.

| Situation actuelle | Champs | ms/champ | Champs calculés/s | MHz CPU calculés | Vitesse nominale |
|---|---:|---:|---:|---:|---:|
| Moniteur, aucun média |120|27,15120|36,831|1,471|73,54 %|
| Moniteur, SSD monté et moteur arrêté |120|27,17650|36,796|1,470|73,48 %|
| Invite BASIC, moteur et sélection actifs |38|75,94620|13,167|0,52585|26,292 %|
| Invite BASIC, arrêt moteur dans le champ |1|46,79450|21,370|0,85343|42,672 %|
| Invite BASIC, moteur et sélection arrêtés |81|31,48879|31,758|1,26826|63,413 %|

Dans les38 champs moteur actif, le contrôleur est inactif avant/après, Reading/Writing faux et aucun octet supplémentaire lu. Le CPU attend dans la même boucle NOS80 (PCFD58–FD5D) que dans les81 champs après arrêt. L'écart44,4574 ms/champ coïncide donc avec la poursuite de l'activité physique sélectionnée du lecteur ; il ne constitue pas encore une attribution exclusive par méthode ni une économie intégralement récupérable. La baisse est observée dans les deux collectes détaillées (78,9376→33,3772 ms/champ dans la précédente), pas une déduction des anciens pourcentages de profileur.
Le code du relais continue légitimement la rotation et les impulsions de flux tant que moteur/sélection restent actifs. Le raccordement CPU limite en plus ses préfixes privés au prochain événement du lecteur, même si le contrôleur est inactif. Candidat à vérifier ensuite : éviter uniquement cette coupure entre domaines prouvés indépendants, avec garde stricte contrôleur inactif, bus privé stabilisé et aucune propagation NMI en attente ; conserver toutes les activités du lecteur et leur avancement existant à la fin du préfixe. Un contrôleur actif sans échéance momentanée (attente index/premier flux) ne doit pas bénéficier de cette qualification. Aucun changement de ce raccordement encore réalisé ni gain promis.
Les durées de commandes de la dernière collecte sontCAT2 832,2652 ms etBASIC5 476,3787 ms ; elles comprennent la saisie émulée et les champs d'attente, pas seulement les transferts. L'empreinte SHA256 du SSD est vérifiée identique avant/après fermeture :69E6E3B3D5C83E4025BAC73161DA74C4B569EDA935E764C992769D84A27209DF.
Preuves dans `artifacts/TestsMachines/Acorn/Acorn System 5/` : `20261110-current-nos80.{json,md,trx}`, `20261110-current-nos80-detailed.{json,md,trx}`, `20261110-current-nos80-activity.{json,md,trx}` et BMP/TXT natifs correspondants. Dernier passage réussi en28 s ; source temporaire à retirer et assembly à reconstruire sans elle. System5 demeure ouvert pour sa cadence ; aucun changement App/SDK/MediaEngine ni de production dans ce diagnostic.

### Indépendance après commande, 10 novembre2026
Câblage CPU/carte400004 : nouvelle qualification reçue du8271 inactif, bus stabilisé, aucune échéance ni propagation NMI en attente. Les événements lecteur ne coupent plus inutilement le préfixe CPU privé ; leur avancement complet à ses dates propres reste exécuté avant la prochaine interaction. Gardes et transferts avec NMI/DataWindow/lignes lecteur datés vérifiés. NOS80/BASIC,3paires alternées de120 champs :43,68878→39,29389 ms/champ (-10,060 %) ; moteur actif70,15062→56,14132 (-19,970 %). Moteur arrêté30,25865→30,75040 : aucun gain revendiqué. CAT/BASIC/PRINT46, vidéo/audio/CPU/RAM identiques et SSD inchangé. Protocole, fenêtres, limites et preuves dans la validation machine `docs/tasks/machines/AcornSystem/system5-validation.md`, rubrique « Préfixes CPU indépendants de la rotation inactive » et rapports20261110-idle-drive-native. Aucun changement interne de puce, de clock ou de média ; aucune prise en charge de machine supplémentaire déduite, System5 reste ouvert pour la cadence.
Validation finale sans banc temporaire :265 contrôles System/carte/relai8271 réussis en1m11 s (`20261110-idle-drive-integration.trx`), incluant les trois variantes de transfert avec nouvelles traces (un/dix secteurs, progressif et entrelacé), la garde NMI et l'attente d'index. Assemblies locales reconstruites sans diagnostic :0 erreur,4 avertissements préexistants. Aucun changement interne d'une puce, aucun outil de capture ni processus de test conservé ; les cœurs/images possédés ont été libérés. Bloc de coordination retenu pour son gain apparié, sans clôturer System5 ni annoncer les100 %.

### Relevé par grandes étapes après7a3c4376a — 10 novembre2026
Diagnostic temporaire Debug, fabrique NOS80/VDU80 et SSD System BASIC en lecture seule. Deux machines appariées, ordre alterné,40 champs de chauffe,20 au moniteur puis CAT/BASIC/PRINT12+34=46 et120 champs après chargement. CPU/RAM/vidéo/audio comparés par champ ; empreinte du média inchangée. Contrôle réussi en37,848 s. Aucun nouveau changement de production retenu dans ce relevé.

| Étape, durée murale exclusive instrumentée | BASIC moteur actif,40 champs (ms/champ) | BASIC moteur arrêté,79 champs (ms/champ) |
|---|---:|---:|
| Carte disquette, AdvanceTo |27,65065|2,27015|
| Parcours privé CPU/VIA/bus/bande |14,44143|13,96770|
| Vidéo indépendante |10,58316|9,86107|
| Horloges représentées |1,57901|1,47176|
| Repli source scalaire, CPU/vidéo inclus |0,16550|0,13538|
| Coordination restante, DrainAudio inclus |7,61496|6,64543|
| Total |62,03471|34,35148|

Le relevé couvre100 % du temps mural exclusif ; les inclusifs ne sont pas additionnés. Environ11589 portions privées et11678 avancements carte/champ : leur fréquence ne prouve pas leur redondance. Moteur actif, la carte est le premier coût observé (44,573 % du temps instrumenté), puis CPU/VIA (23,280 %) et vidéo (17,060 %). Moteur arrêté, CPU/VIA et vidéo dominent. Le périmètre carte ne comprend pas tous les appels de bus/qualification liés au disque.
Calibration avec sondes désactivées dans le même binaire :58,58063→62,03471 ms/champ moteur actif (+5,896 %) ;32,00290→34,35148 moteur arrêté (+7,339 %) ;32,90444→37,23723 au moniteur (+13,168 %). Même sans aucune sonde, les deux modes identiques présentent des écarts de1,932 %,1,009 % et4,390 % respectivement. Ces différences ne permettent pas une correction exacte de la surcharge. Les durées sont murales, pas des temps CPU propres ; GetThreadTimes sur les fenêtres complètes donne un temps CPU proche du mural, avec granularité visible. Aucune économie potentielle n'est assimilée automatiquement à ces pourcentages. Le champ unique de transition ne sert pas à conclure.
Le rapport ne valide ni n'invalide à lui seul les45,79 % auparavant attribués aux deux compteurs : il mesure la carte entière, pas leur temps propre. Preuves :artifacts/TestsMachines/Acorn/Acorn System 5/20261110-stage-cost-{baseline,profile}.{json,md,trx}. Les trois sources instrumentées sont rétablies exactement à7a3c4376a et les deux sources de diagnostic supprimées ; reconstruction sans sonde réussie : module Acorn0 erreur/0 avertissement, tests locaux0 erreur/4 avertissements préexistants. System5 reste incomplet pour sa cadence Debug.

### Portions indépendantes mémoire et VIA — 10 novembre2026
Le préfixe CPU existant accepte maintenant les cycles privés et les lectures VIA déjà qualifiées dans une même portion, au lieu de coordonner les domaines après chaque lecture VIA. Chaque prochain accès reste qualifié avant exécution ; les cycles/transactions CPU, fronts PH2, lectures et acquittements VIA, timers et clavier restent unitaires dans leurs composants. Une écriture VIA, un autre registre ou une lecture VIA en mode de contrôle sortant termine la portion, comme les dépendances WAIT/DMA, accès partagés et frontières vidéo prévues par le parcours existant. Les événements disquette actifs sont toujours livrés avant chaque phase CPU comme auparavant. Les horloges et états restent dans leurs composants ; aucun moteur supplémentaire ni changement du matériel.
Justification du bus : IC4 reçoit A9/A10 ; dans0E00–0FFF ces deux lignes sont hautes, doncY1 (seule sortie raccordée) reste inactive. Les adresses mémoire privées inhibent le décodeur. Les adresses intermédiaires qualifiées ne peuvent donc sélectionner8271 ou activer DACK. L'adresse préparée suivante et ses propagations sont présentées à la fin du préfixe avant une transaction non indépendante. Référence :fdc400004.md, décodage IC4, figure8/PDF108. Sélection interne GroupIndependentBusCycles=false pour comparer au découpage précédent ; aucune option utilisateur.
Mesure Debug native appariée, même DLL et fabrique NOS80/VDU80, RAM49Kio, lecteursss80, SSD System BASIC en lecture seule. Trois paires neuves,40 champs de chauffe, CAT/BASIC/PRINT12+34=46 par clavier, dix champs de stabilisation puis120 champs mesurés par paire. Ordre alterné à chaque champ ; RunFrame/DrainAudio compris, contrôles hors temps, aucune compilation/capture concurrente. CAT512 octets et BASIC4096, aucun octet écrit ; empreinte SSD inchangée après libération des images. CPU/RAM/vidéo/audio et compteurs de transfert identiques à chaque champ dès le démarrage.

| Paire,120 champs/mode | Précédent ms/champ | Nouveau ms/champ | Temps en moins |
|---|---:|---:|---:|
|1|42,79890|32,46000|24,157 %|
|2|42,25019|32,60263|22,834 %|
|3|43,31908|33,64120|22,341 %|
|Moyenne360 champs|42,78939|32,90128|23,109 %|

| Activité avant/après | Champs/mode | Précédent ms/champ | Nouveau ms/champ | Temps en moins | Champs/s calculés | CPU équivalent | Nominal |
|---|---:|---:|---:|---:|---:|---:|---:|
|Moteur actif/actif après chargement|147|58,57317|48,00265|18,047 %|20,832|0,832 MHz|41,598 %|
|Moteur arrêté/arrêté|210|31,75328|22,32799|29,683 %|44,787|1,789 MHz|89,430 %|
|Transition actif/arrêté|3|41,91200|33,06347|21,112 %|30,245|1,208 MHz|60,393 %|

Moyenne globale32,90128 ms/champ :30,394 champs/s,1,214 MHz équivalent,60,691 % nominal. Référence matérielle :19,968 ms/champ,50,080 Hz,CPU2 MHz. Vitesse maximale de calcul sans limite, pas des FPS présentés par RML ; aucune fréquence matérielle changée. La moyenne dépend de la répartition des champs moteur actif/arrêté : comparer les modes appariés, pas les valeurs absolues de collectes antérieures. Ce scénario mesure l'invite après chargement, pas le gain pendant les transferts actifs. La cadence moteur actif reste ouverte ; System5 non terminé.
Contrôles :270 cas System/carte/8271 réussis en1m39 s, dont trois nouveaux programmes lecture/écriture/relecture un/dix secteurs avec lectures VIA intercalées, progressif/entrelacé ; NMI avec temps, phase et cycles CPU, DataWindow et lignes lecteur datées identiques au découpage précédent. Les contrôles privés comparent aussi états VIA, IRQ/timers, clavier, modes de contrôle et notifications vidéo datées. Aucun changement interne de puce.
Preuves :artifacts/TestsMachines/Acorn/Acorn System 5/20261110-mixed-span-native.{json,md,trx},20261110-mixed-span-private.trx et20261110-mixed-span-integration.trx. Banc natif réussi en1m28 s puis supprimé ; reconstruction sans diagnostic réussie : Acorn0 erreur/0 avertissement, tests locaux0 erreur/4 avertissements préexistants.

### Répartition des gestionnaires racines de carte —10 novembre2026
Relevé mural direct, une paire NOS80/VDU80 et SSD BASIC en lecture seule :CAT/BASIC/PRINT46,120 champs après stabilisation, ordre alterné, comparaisons CPU/RAM/vidéo/audio par champ et SSD inchangé. Diagnostic réussi en31 s. Les gestionnaires incluent leurs destinataires synchrones : ce ne sont pas des temps CPU propres des seules puces. Les lignes racines et la synchronisation finale sont disjointes ; le total carte est inclusif, non additionné à ses enfants. Rapport20261110-board-parts.{json,md,trx}.

| Étape, moteur actif (49 champs) | ms/champ | Appels/champ | Part du temps carte instrumenté |
|---|---:|---:|---:|
|Total carte inclusif|33,62768|2496|100 %|
|IC7 séparateur93 et destinataires|14,43209|37204,16|42,917 %|
|IC12 latch et destinataires|9,37052|14081,76|27,866 %|
|Connecteur lecteur et destinataires|5,77747|7252,16|17,181 %|
|IC8 diviseur393 et destinataires|0,84371|624|2,509 %|
|Synchronisation finale contrôleur|0,32345|2496|0,962 %|
|Timeout et contrôle|0,11664|618,73|0,347 %|
|Coordination résiduelle carte|2,76379||8,219 %|

Hors carte :24,84605 ms/champ ; total instrumenté58,47373. Moteur arrêté : carte1,39389 et hors carte23,30943, total24,70332. Surcharge appariée brute :55,23865→58,47373 (+5,857 %) moteur actif,24,58367→24,70332 (+0,487 %) arrêté. Les hooks dormants, l'ordre et la variabilité ne permettent pas une correction exacte de cette surcharge ; aucune économie récupérable n'est assimilée automatiquement à ces parts. Le champ unique de transition est exclu des conclusions. L'ancien45,79 % attribué aux deux compteurs ne peut pas être confirmé comme leur temps propre ; ici IC7 inclut le latch et d'autres destinataires, et IC8 reste faible.
Prochaine cible établie dans le code : Sn74Ls93.ConnectedResponseDeadline renvoie chaque descente CKB avant même la réponse d'une sortie raccordée, puis réveille de nouveau à sa propagation. Calculer dans93 la première réponse raccordée, en gardant ses opérations internes, ses délais et son parcours conservateur en présence d'une description non établie, d'un reset ou d'un observateur interne. Sonde et banc supprimés ; carte rétablie exactement à30af8b3ee, reconstruction sans diagnostic en cours.

### Calcul des seules réponses raccordées : tentative retirée —10 novembre2026
Première version : annonces IC7 mesurées sur trois champs séparés62 428→43 952, mais35,68105→36,22942 ms/champ (+1,537 %) ; rapports20261110-connected-response-native-initial. Après cache causal et calcul du premier bit observé seulement,30 contrôles de puce (906 ms) et58 de carte (19 s) réussissent. Nouvelle comparaison native, trois paires120 champs :33,55538→33,44868 ;37,36512→36,98857 ;37,78418→37,88509. Moyennes360 champs36,23490→36,10745 (-0,352 %), dont moteur actif53,39127→53,01237 (-0,710 %) et arrêté24,67717→24,70091 (+0,096 %). Une paire régresse et le gain global est insuffisant : calcul, cache et flags de candidat retirés, les trois Services93 rétablis exactement à30af8b3ee. Aucun gain nouveau présenté comme retenu ni ajouté au tableau des directives.
Preuves finales20261110-connected-response-native.{json,md,trx} : diagnostic réussi en1m28 s, NOS80/BASIC/PRINT46, CPU/RAM/vidéo/audio égaux à chaque champ et SSD inchangé. Les annonces IC7 sont comptées hors mesure de performance, sur un champ séparé par paire ; ce ne sont pas des comptes d'appels AdvancePart ni des économies de temps. La baisse d'annonces n'a pas réduit substantiellement le travail du parcours complet.
Les comparaisons aux fronts explicites, graines/masques et phases fines sont conservées comme contrôles de comportement, indépendants du candidat. Elles acceptent la borne conservative historique de NextOutputEventNanoseconds et n'exigent pas une API plus précise pour faire réussir le code. La garde spécifique à la projection retirée est supprimée ; les cas d'erreur préexistants restent couverts. Prochaine analyse à cibler sur le travail effectué dans les résolveurs et leurs destinataires, plutôt que le seul nombre d'annonces ; le relevé par gestionnaire donne une attribution inclusive, pas encore leur coût propre individuel. Cadence System5 non clôturée.

Validation finale de la référence restaurée :29 contrôles93 réussis en951 ms (`20261110-reference-oracle-coverage.trx`),58 contrôles carte réussis en16 s (`20261110-reference-board-coverage.trx`). Module Acorn reconstruit sans source de diagnostic :0 erreur/0 avertissement ; quatre avertissements locaux préexistants. Les fichiers de production93 et de carte sont identiques à30af8b3ee ; seules la couverture matérielle indépendante et la consignation de ce passage sont conservées. Aucun gain ni nouvelle solution ajoutés au tableau des directives pour ces essais retirés. Aucun processus de diagnostic possédé conservé. System5 reste ouvert.
