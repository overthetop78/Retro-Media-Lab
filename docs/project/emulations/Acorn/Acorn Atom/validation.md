# Acorn Atom — reprise du 8 novembre 2026

## 11 novembre — reprise et revue des puces avant poursuite de la carte

L'utilisateur confirme System 5 terminé et demande Atom, puis impose la revue des puces avant la suite. [Bilan détaillé](../../../../tasks/machines/AcornAtom/chip-review.md) : code relu, limites de fidélité, optimisations présentes/manquantes, 989 tests propres aux cinq groupes en Debug et autant en Release, et mesures isolées réelles. Ces tests ne certifient pas à eux seuls le matériel réel ; aucune puce ni machine n'est clôturée par cet inventaire.

Un premier passage regroupe le diagnostic temporel et les contrôles Atom : 53 succès et un échec bande TAP, A5 attendu et aucune donnée décodée. Les temps de ce passage ne sont pas retenus, car les autres tests pouvaient s'exécuter simultanément. Le diagnostic est ensuite rejoué seul : succès, 31 s, `artifacts/TestsMachines/Acorn/Acorn Atom/20261111-atom-isolated-debug.trx`. Aucun changement de production.

Montages à ROM BASIC/OS, flottants et caractères existantes, RAM texte 5 Kio et vidéo 6 Kio ; 40 champs de chauffe puis trois fenêtres de 120 champs, RunFrame/DrainAudio inclus, empreintes hors temps. Base : moyenne 27,5289 ms/champ, 36,33 champs/s libres, 0,6062 MHz effectifs, 60,62 % nominal. Disc Pack sans support : 26,8508 ms/champ, 37,24 champs/s, 0,6215 MHz, 62,15 %. Cadence matérielle conservée 59,9227434 Hz. Chaque fenêtre exécute 2 002 577–2 002 580 cycles ; les fins d'instruction expliquent ces trois cycles d'écart. Les empreintes vidéo/audio et RAM concordent entre montages à chaque passage correspondant ; les données sont dans `20261111-current-debug.json` (rejoué isolément). Ordre successif, aucune comparaison appariée ni optimisation conservée : l'écart ne prouve aucun gain du Disc Pack. Pas de mesure de présentation RML.

Le récepteur vidéo avec circuit seuls, RAM synthétique, donne 24,3403 ms/champ en moyenne dans ce diagnostic ; il s'agit d'un coût inclusif distinct, jamais ajouté à celui de la carte. Ces résultats montrent une cadence Debug encore insuffisante. La fidélité et l'optimisation doivent maintenant être traitées puce par puce, selon les références du matériel réel ; aucune nouvelle caractéristique n'est déduite des émulateurs tiers ou du LLM. L'échec TAP reste identifié, sans cause attribuée arbitrairement au CPU/PPI.

Optimisation CPU ultérieure dans cette reprise : boucle Step conservée dans le séquenceur canonique, 1130 contrôles CPU hors états réussis en Debug et Release. Mesures avant/après CPU et compromis Clock détaillés dans la fiche CPU ; aucune nouvelle mesure de carte, validation RML ou clôture Atom.

Décisions utilisateur reçues pour la clôture de la revue6502 : conserver les limites du BCD non spécifié et des opcodes non officiels ; reporter les délais PH1/PH2, en plus du report PLP/SEI. Voir la fiche CPU pour les preuves et la portée. La suite reste puce par puce après commit du bloc CPU, avant câblage/validation Atom.

Revue8255 suivante : GPIO C publiés seuls,30 contrôles Debug/Release réussis et mesures isolées comparées consignées dans la fiche8255. Les autres modes et le câblage sont conservés. Aucun nouveau passage de machine ni gain Atom attribué ; le VIA reste le composant suivant après commit.

## Montage et références réutilisées

Revue CPU du 11 novembre, après les références IRQ fournies par l'utilisateur : **618 contrôles 6502 Debug et618 Release** hors états réussissent, zéro échec/ignoré. Trois cas autonomes reproduisent l'observation rapportée CLI/INY sur Apple IIe, par Step, Clock et PH0 ; production inchangée. Les sources, les limites et la divergence PLP/SEI restent dans `docs/chips/CPU/Mos6502/reset-interrupt-reference.md`. Ces tests de puce ne constituent pas une nouvelle validation de la carte Atom ni une mesure de vitesse. Les deux questions ont reçu réponse ; aucun relevé matériel PLP/SEI disponible. Maintien du code confirmé, puis report explicite de ce seul cas par l'utilisateur pour une éventuelle anomalie ; poursuite autorisée sans le présenter comme matériellement validé : voir la note CPU liée. La revue du composant reste inachevée.

Les composants existants sont réutilisés : CPU 6502 à 1 MHz, PPI Intel 8255, VIA 6522, VDG MC6847 et Disc Pack 8271 lorsqu'il est installé. Fiches : `docs/chips/IO/Intel8255.md`, `docs/chips/Video/Mc6847.md` ; historique de réalisation et médias dans `docs/tasks/machines/AcornAtom/implementation.md`. Le PPI n'a pas d'oscillateur : ses accès et handshakes suivent les fronts du bus et de ses broches. Ne pas inventer un Tick interne de ce composant asynchrone.

Le montage vidéo courant conserve le MC6847 de base à 3 579 545 Hz, 228 périodes par ligne et 262 lignes par champ progressif, soit environ 59,92 Hz. La [notice Acorn Atom Colour Board, issue 1, mars 1982](https://electrickery.hosting.philpem.me.uk/comp/atom/colour/index.html), pages 1 et 4, décrit une carte distincte : conversion 60 vers 50 Hz, ajout de 25 lignes à chaque extrémité et arrêt de CLK du MC6847 pendant ces insertions. Cette option ne doit pas être simulée en changeant arbitrairement la fréquence du VDG. Le montage de base et cette extension doivent rester distingués ; l'option 50 Hz n'est pas annoncée comme réalisée par ce passage.

## Entrées et interruptions de carte

`AtomMachine.cs` relisait IRQ/NMI par le bus CPU et reconstruisait les entrées C du PPI à chaque tick dans les modes strobed. Le VIA et le 8271 sont désormais raccordés aux entrées conservées du CPU par leurs notifications existantes. Les autres broches auxiliaires constantes sont déclarées non raccordées par le contrat `ICpuBus.ControlSignals` ; les interruptions restent échantillonnées aux phases du CPU.

Les niveaux PC4 (diviseur bande), PC5 (comparateur bande), PC6 (REPT actif bas) et PC7 (FS du VDG) sont conservés ensemble dans `_inputPins`. Chaque émetteur modifie seulement son fil lorsqu'il change. Le 8255 reçoit alors le nouveau niveau et résout son handshake, y compris ses effets asynchrones ; son entrée C n'a plus de fournisseur relu par Tick. Les lectures de bus demandées par le logiciel et les horloges internes nécessaires sont conservées. Les niveaux sont initialisés au reset avant reprise du programme.

Le MC6847 publie FS aux frontières exactes ; commit `f7db897b0`, 51 contrôles Debug réussis. Aucun changement de compteurs, pixels ou fréquences de la puce.

## Contrôles et mesures de carte complète Debug

18 contrôles Atom réussis, 645 ms : configuration/Disc Pack, bus/RAM/ROM, modes vidéo, matrice et signaux bande, plus quatre nouveaux cas de raccordement. Ils couvrent IRQ du timer VIA et acquittement, NMI/résultat 8271 et reset, FS et comparateur dans les entrées conservées, REPT/ACK sans retard ajouté, et horloge STB/INTR. Les sept cycles RESET initiaux du CPU avancent déjà le diviseur de bande : le troisième front est donc observé après 617 ticks supplémentaires, et non 624. Une attente ancienne du test de configuration a été corrigée : les deux options Tape son/volume existent, cachées ; elles ne constituent pas des options vidéo BBC.

Un diagnostic temporaire exécute les ROM externes `Acorn Atom BASIC OS.bin`, `Acorn Atom Floating Point.bin` et `MC6847.bin`, avec 5 Kio de RAM texte supplémentaire et 6 Kio vidéo, sans Disc Pack dans ces deux mesures. Chaque scénario comporte 20 champs de chauffe puis trois passages successifs de 60 champs, vidéo rendue et audio drainé et haché. Le second joue un signal bande de 48 kHz par les broches existantes. Aucun autre test, build ou RML ne tourne pendant les mesures. Les coûts incluent les deux empreintes de vérification ; ce ne sont pas des FPS présentés par l'application.

| Scénario | Avant, ms/champ | Après, ms/champ | Après, champs/s libres | Après, MHz émulés | Vitesse réelle |
|---|---:|---:|---:|---:|---:|
| BASIC au repos | 24,301 | 24,016 | 41,65 | 0,695 | 69,50 % |
| BASIC avec signal bande en lecture | 25,753 | 25,934 | 38,56 | 0,644 | 64,35 % |

Pour chacun des trois passages de chaque scénario, cycles CPU et empreintes SHA-256 vidéo/audio coïncident exactement avant/après. Les écarts de temps ne démontrent pas d'amélioration globale significative ; aucun gain garanti n'est annoncé. Ce raccordement réalise la propagation par événements demandée et conserve les activités matérielles. Les mesures sont rangées sous `artifacts/TestsMachines/Acorn/Acorn Atom/20261108-{before,after}-{idle,tape}-debug.json`, avec rapports TRX. Le diagnostic dépendant de ROM externes est supprimé après usage.

## Clavier hôte et pavé numérique

`AtomKeyboard.SetInput` projette maintenant `HostCharacters` et le pavé vers les contacts réels, puis conserve les seize colonnes avec leurs modificateurs. `Read` retourne le niveau mémorisé ; aucune nouvelle recherche dans les touches hôtes lors d'une lecture du PPI. Sélection, réinitialisation, BREAK, REPT, CTRL et les flèches partagées conservent leurs contacts. La matrice n'a qu'un SHIFT physique : des symboles simultanés qui exigent deux niveaux incompatibles ne peuvent pas tous deux être injectés ; le deuxième incompatible est écarté, suivant le principe déjà utilisé par le clavier BBC. Un caractère non représentable est écarté, sans presser une autre touche par défaut.

Les légendes sont distinctes des noms des touches hôtes : `-`/`=`, `;`/`+`, `:`/`*`, `/`/`?`, `,`/`<`, `.`/`>` ; les chiffres 1–9 partagent `!"#$%&'()`. `@`, `[`, `]`, `\\` et `^` utilisent leurs contacts dédiés. La touche hôte Retour arrière est raccordée à DELETE, tandis que le caractère `^` conserve le contact flèche haute/caret d'origine. Les lettres gardent leur identité hôte et le rôle de LOCK dans la ROM. La table existante est confrontée aux [déclarations de clavier Atom de MAME](https://github.com/mamedev/mame/blob/master/src/mame/acorn/atom.cpp), lignes 629–705 à la consultation ; ce rapprochement ne remplace pas l'exécution du firmware. Copie acquise le 8 novembre : `artifacts/.tmp-archives/machines/Acorn/Acorn Atom/mame-atom-20261108.cpp`, SHA-256 `AD009BAE7897EBC7E7064B67DA47C4B0243D31FB782DC1364455F0AC595FE2C5`. Notice de la carte couleur archivée dans le même dossier sous `colour-board-issue1-1982.html`.

47 tests Atom Debug réussissent, dont ponctuation AZERTY, chiffres et cinq opérateurs du pavé, Entrée, Retour arrière, relâchements, conservation de l'instantané, caractères absents et conflit de SHIFT. Le moniteur/BASIC externe confirme ensuite, par le clavier projeté et les véritables instructions 6502 : `?#3BFF=12+34` écrit 46, `?#3BFE=7*6` écrit 42, et `?#3BFD=1X` corrigé par Retour arrière puis `+2` écrit 3. L'affichage invité imprime `AZERTY!?` et les trois valeurs. PNG réels du tampon MC6847 : `20261108-keyboard-basic-first.png` et `20261108-keyboard-basic-result.png`, avec `20261108-keyboard-basic.json` et rapport `atom-basic-host-input-debug.trx`. Ces preuves sont hors fenêtre RML, pas une vérification d'appuis physiques par Windows.

Le premier essai du diagnostic omettait des caractères avec des appuis/relâchements de trois champs ; la séquence est corrigée à quatre champs d'appui et douze de relâchement, sans modifier le matériel. Résultat final du diagnostic : un cas réussi en 30 s. Le programme temporaire dépendant des ROM est supprimé après usage. Les tests autonomes sont conservés.

## Points restant à terminer avant validation de machine

Les scénarios invités ci-dessous sont exécutés avant la demande de revue sans nouveaux tests/builds. L'essai RML, la cadence Release de ce dernier montage et la livraison restent à effectuer ; les observations ci-dessous ne terminent pas l'Atom.

## Résultats déjà obtenus — bande, Disc Pack, graphismes et bip

Le BASIC sauvegarde puis recharge le programme qui écrit 42 à 3BFF et affiche `MEDIA PASS`. Pour la bande, SAVE/NEW/LOAD/RUN passe après export WAV, éjection et réouverture par le SDK : `F:/Retro/Acorn Atom/Media-validation-20261108-191415852.wav`, 3 229 802 octets. Pour le Disc Pack, la même séquence passe après détachement, fermeture et réouverture de `F:/Retro/Acorn Atom/Media-validation-20261108-191901725.dsk` : 768 octets écrits puis 768 relus. Les deux captures finales montrent le programme et son résultat. Le premier essai disque envoyait RUN trop tôt après LIST ; son échec est conservé dans `20261108-disk-first-input-too-early`, puis le diagnostic seul est corrigé et le scénario disque réussit (`atom-disk-native-debug.trx`). Aucun correctif de puce déduit de cette faute du diagnostic.

`PRINT $7` donne une crête PCM de 8192 dans les deux montages. La boucle graphique BASIC `CLEAR 4`, `FOR X=0 TO 255`, `MOVE 0,0;DRAW X,191`, `NEXT X`, `GOTO 20` produit les pixels réellement capturés. Mesures Debug de carte entière, 20 champs de chauffe puis 120 champs, vidéo et drainage audio inclus, sans autre émulation :

| Montage/scénario | ms/champ | champs/s libres | MHz CPU effectifs | Vitesse réelle |
|---|---:|---:|---:|---:|
| Base, repos | 23,676 | 42,24 | 0,705 | 70,49 % |
| Base, graphismes | 24,220 | 41,29 | 0,689 | 68,90 % |
| Disc Pack, repos | 28,146 | 35,53 | 0,593 | 59,29 % |
| Disc Pack, graphismes | 24,890 | 40,18 | 0,670 | 67,05 % |

Chaque mesure exécute 2 002 580 cycles ; cadence vidéo matérielle conservée à 59,9227434 Hz pour le montage de base. Les empreintes RAM concordent entre montages pour chaque scénario. Les JSON `20261108-final-debug-{base,discpack}.json`, les PNG bande/disque/graphismes et les rapports TRX sont sous `artifacts/TestsMachines/Acorn/Acorn Atom/`. Ces temps ne tiennent pas la cadence nominale ; aucune accélération globale ni vérification d'écoute Windows n'en est déduite. Aucun nouveau test/build depuis la demande de revue statique. Le diagnostic temporaire est retiré du source ; son assembly sera régénéré au prochain build autorisé.
