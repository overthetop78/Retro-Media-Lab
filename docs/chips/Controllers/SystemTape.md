# Carte cassette Acorn 200.001 / 100.001

## Circuit et interfaces

`src/RetroMediaLab.Chips/Controllers/Acorn/SystemTape/Services/SystemTapeInterface.cs` reproduit les compteurs et les bascules numériques de la carte ; ses constantes sont dans le dossier Constants voisin. Ce composant reçoit les comptes de PH2 de sa propre source, CASOUT et la sortie d'IC5.3, après inversion du comparateur LM358. Il possède ses phases, compteurs, bascules, sorties et prochaine échéance. Il publie uniquement les changements de CASIN/NHITONE, de sortie audio et d'échéance. Aucun thread, ordonnanceur par tick ni consultation d'un autre composant.

IC1/CD4024 est remis à zéro quand Q5, Q4 et Q2 sont hauts : 16+8+2=26. Ses descendants Q3 se produisent aux comptes 8,16,24. Ces fronts cadencent IC6/CD4024 sept bits ; Q7 est NHITONE, qui revient aussi à zéro lors du débordement naturel. Le front montant d'IC5.3 fait échantillonner Q7 par IC4/4013 avant la remise à zéro différentiée d'IC6. CASIN est la sortie complémentée de cette bascule ; ce n'est pas NHITONE inversé en continu.

IC2 compte les divisions IC1. La seconde moitié d'IC4 conserve CASOUT au front montant de la remise à zéro d'IC1 ; les portes sélectionnent Q4 (division 16) ou Q5 (division 32) d'IC2. C2 remet IC2 à zéro au descendant de CASOUT, en conservant la phase d'IC1. À 1 MHz, les fréquences numériques résultantes sont 2403,846 et 1201,923 Hz, périodes de 416/832 PH2. Le modèle nominal résout les portes à l'instant de l'événement PH2 ; il ne prétend pas reproduire les temps de propagation analogiques des composants, le gain LM358 ou les tolérances RC.

Les fronts visibles sont visités dans leur ordre pendant AdvanceTo. Les fronts internes sans effet extérieur sont comptés exactement par arithmétique (modulo 26, compteur sept bits, trois fronts Q3 par division). Le propriétaire reçoit NextClockEventChanged et garde cette échéance localement. Une lecture du 8154 utilise les niveaux reçus, sans rappeler le transport ni rechercher la porteuse.

## Sources conservées

Schémas Acorn originaux : [carte clavier/afficheur/cassette](https://theoddys.com/acorn/acorn_system_hardware/keyboard%2C_display_%26_cassette_interface_board/keyboard_display_cassette_interface_board.html), 200.001 issue 2 du 11 juillet 1979 et 100.001 issue 3 du 27 mars 1980. Le scan original montre Q7/NHITONE et les bascules. La [retranscription de la réplique Chris Oddy](https://www.theoddys.com/acorn/acorn_replica_boards/replica_keyboard%2C_display_%26_cassette_interface_board/replica_keyboard_display_and_cassette_interface_board.html), schéma du 25 juin 2025, rend le suivi des nets lisible. Les modifications de connecteurs et les composants non montés de cette réplique ne sont pas des ajouts au modèle numérique. Le montage et les broches sont confrontés au manuel System 2 et aux sources Monitor.ca65 / COS.ca65.

Tous ces documents, leurs rendus complets et les détails de compteur/diviseur restent sous `artifacts/.tmp-archives/machines/Acorn/Acorn System 2/`. Ne pas refaire la recherche avant de consulter ces références.

## Vérifications du 8 novembre 2026

Les dix tests autonomes SystemTapeTests passent en Debug en 190 ms : division et sélection des sorties, Q7 sans saturation, CASIN échantillonné avant reset, détection des deux périodes, RESET, CASOUT, notifications et comparaison des sorties à chaque PH2 avec une référence par compteurs binaires. L'avancement groupé conserve toutes les transitions observables de l'avancement unitaire. Résultats `artifacts/TestsChips/SystemTape/system-tape-circuit-debug.trx`.

Le propriétaire AcornSystemMachine utilise le transport AnalogTape existant en DirectWaveform pour les cartes 6502 standard et System 1. Le transport publie les transitions PCM comparées ; le module les inverse pour IC5.3 et câble les niveaux de sortie. Le 8154 reçoit PA7/CASIN et PA5/NHITONE. Le CPU RESET chaud conserve les compteurs et le signal de la carte ; le cycle d'alimentation les réinitialise. Les autres propriétaires du transport gardent leur modem FSK existant.

La ROM COS réelle SAVE/LOAD restitue les huit octets `00 55 AA FF 12 34 56 78` après effacement de la RAM. Signal enregistré : 584 638 échantillons à 48 kHz sur ce passage. Un deuxième défaut indépendant empêchait LEADER de progresser : DataWithStrobe du clavier était actif haut, tandis que RDC et LEADER du COS exigent PB7 actif bas. Sa correction rend appui, relâchement et attente cohérents avec la notification StrobeChanged déjà active basse.

System 1, ROM Monitor d'origine : saisie et GO du programme `A9 5A 8D 00 03 6C FC FF`, SAVE, effacement, LOAD du signal et nouveau GO réussis. La présentation LED garde son échéancier cumulé ; le passage final enregistre 86 400 échantillons, 1,8 s. Capture `artifacts/TestsMachines/Acorn/Acorn System 1/20261108-system1-circuit-monitor-reloaded-program-debug.bmp`.

System 2 complet, CPU/VDU/bande/audio inclus : 15,396809 ms/champ en Debug, 64,949 champs/s sans limite, 1,298971 MHz effectifs / 129,897 % du profil. Le balayage émulé reste à 49,999984 champs/s. Ces chiffres mesurent la carte complète, pas la vitesse isolée du composant. RML fermé, 160 champs après transfert bande ; aucune optimisation Release appliquée en Debug. Un dernier contrôle familial reste prévu après intégration des autres System demandés.

Passage natif final après conservation de CASOUT : System 1 SAVE/LOAD/GO réussi, 86 400 échantillons ; System 2 SAVE/LOAD réussi et 15,788811 ms/champ, 63,336 champs/s, 1,266720 MHz effectifs / 126,672 %. Banc externe et sorties de compilation supprimés après vérification du chemin résolu. Les deux captures de protocole sont également dans artifacts/TestsChips/SystemTape/Acorn System 1 et Acorn System 2.

Contrat public du 8 novembre : ISystemTapeInterface expose les mêmes entrées, sorties, notifications et domaine temporel que la classe. Les tests d’architecture ne signalent plus ce composant ; ils signalent encore 14 classes ACB-4000/SCSI non modifiées dans ce bloc. Raccordement 6809 : CASIN PB7, CASOUT CB2 via niveau et propriété de sortie réels du VIA, tirage haut quand CB2 est libérée.
