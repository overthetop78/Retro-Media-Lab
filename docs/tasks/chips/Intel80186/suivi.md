# Intel 80186 — défaut DOS Plus du Master 512

- [x] Identifier le premier transfert de contrôle incorrect avant de modifier le processeur.
  - [x] Modifier temporairement `src/RetroMediaLab.Chips/CPU/Intel/MCS80x86/80186/Services/Cpu80186.cs` : conserver un historique borné des instructions réellement décodées et des entrées d'interruption ; imprimer cet historique seulement au premier opcode invalide, sans accès mémoire supplémentaire.
  - [x] Créer `artifacts/.tmp-tube/Dos512Trace.cs` depuis le diagnostic existant : média en lecture seule, capture native et nettoyage des entrées/médias dans `finally`, erreurs interceptées.
  - [x] Restaurer `src/RetroMediaLab.Chips/CPU/Intel/MCS80x86/80186/Services/Cpu80186.cs` après consignation du résultat ; supprimer `artifacts/.tmp-tube/Dos512Trace.cs` et reconstruire le banc avec `Program.cs` pour retirer l'instrumentation des assemblages.

Le premier passage vers l'espace incorrect suit l'opcode FF à `5A00:5FFE` : l'instruction suivante est à `0000:0000`. Aucune entrée d'interruption n'intervient entre les deux. L'opcode invalide à 008C survient ensuite pendant l'exécution des données de la table des vecteurs. Cela localise le transfert fautif, sans encore établir pourquoi sa cible vaut zéro.

- [x] Identifier l'opérande du transfert à `5A00:5FFE` et l'origine de sa valeur.
  - [x] Modifier temporairement `src/RetroMediaLab.Chips/CPU/Intel/MCS80x86/80186/Services/Cpu80186.Instructions.cs` : enregistrer les opérandes déjà lus de ce saut lointain, ses registres de segment et son adresse effective, sans lecture de bus supplémentaire.
  - [x] Créer `artifacts/.tmp-tube/Dos512Operand.cs` : exécuter le même démarrage, archiver la RAM parasite directement depuis son tableau hôte pour l'analyse du code chargé et garantir le détachement du disque.
  - [x] Restaurer `src/RetroMediaLab.Chips/CPU/Intel/MCS80x86/80186/Services/Cpu80186.Instructions.cs`, supprimer `artifacts/.tmp-tube/Dos512Operand.cs` et reconstruire `Native.csproj` avec `Program.cs` après consignation.

- [x] Identifier l'écriture qui remplace le code DOS autour de 5FFFB.
  - [x] Modifier temporairement `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMaster/Tube/Services/Master512Board.cs` : tracer les écritures de 5FFF0 à 60002 depuis les arguments déjà reçus et les registres CPU, sans accès de bus ajouté.
  - [x] Créer `artifacts/.tmp-tube/Dos512Write.cs` avec le même démarrage en lecture seule et son nettoyage ; arrêter ce diagnostic au premier remplacement ciblé pour éviter de poursuivre dans le code corrompu.
  - [x] Restaurer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMaster/Tube/Services/Master512Board.cs`, supprimer `artifacts/.tmp-tube/Dos512Write.cs` et reconstruire le banc initial après consignation.

Les écritures du chargeur à `3800:0105` déposent bien les octets du disque à la frontière 5FFFF/60000. La routine en `6012:4645` et `6012:4648` réécrit ensuite les mots 5000 et AFFF en 5FFFC–5FFFF. C'est une routine de détection mémoire également présente dans le bootstrap. Le dernier passage du diagnostic a conservé dans la DLL une ancienne instrumentation retirée des sources, car les copies restaurées avaient leur ancien horodatage : reconstruction effectuée après actualisation explicite des sources restaurées. Aucun résultat de performance déduit de ces passages.

La correction matérielle reste ouverte. Avant de poursuivre cette piste, l'utilisateur demande de comparer une autre disquette/version DOS Plus ; suivi dans `docs/tasks/machines/BbcMaster/512-validation.md`.

- [x] Corriger RETF imm16 et vérifier son incidence sur les mêmes démarrages DOS.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Intel/MCS80x86/80186/Services/Cpu80186.Instructions.cs` : conserver séparément l'immédiat de libération de pile et l'IP dépilé pour l'opcode CA, sans modifier les cycles ni les autres instructions.
  - [x] Modifier `tests/RetroMediaLab.Chips.Tests/CPU/Intel/MCS80x86/80186/80186Tests.cs` : vérifier IP, CS, SP et indicateurs pour RETF avec immédiat nul, non nul et rebouclage 16 bits de SP ; exécuter ensuite le groupe autonome 80186.
  - [x] Créer `artifacts/.tmp-tube/Dos512Retf.cs` à partir du scénario DOS existant : refaire les démarrages 1.2 et 2.1 avec la seule correction CA, conserver captures et compteurs, vérifier si A> apparaît.
  - [x] Supprimer `artifacts/.tmp-tube/Dos512Retf.cs` et reconstruire le banc initial après consignation des résultats et de la comparaison historique du décodage mémoire.
  - [x] Supprimer les journaux temporaires `artifacts/.tmp-tube/dos512-{trace,operand,write,alternative,alternative-long,retf}-debug.txt` et le dump `artifacts/.tmp-tube/dos512-operand-ram.bin`, après conservation des résultats dans les fiches et des captures sous TestsMachines/TestsChips.

Résultat : la seule correction CA permet à la 2.1 d'atteindre A> sans opcode invalide dans 3 000 champs. La 1.2 conserve le même défaut à 0000:008C ; ce problème distinct reste ouvert. Ne pas clôturer le processeur entier sur ces seuls résultats.

Le démarrage fautif est déjà reproduit avec le code courant ; ne pas répéter ce passage sans nouvelle instrumentation utile. PUSH SP reste inchangé tant que son comportement propre au 80186 n'est pas établi.

- [x] Déterminer pourquoi DOS Plus 1.2 entre dans sa détection mémoire après le chargement.
  - [x] Modifier temporairement `src/RetroMediaLab.Chips/CPU/Intel/MCS80x86/80186/Services/Cpu80186.Prefetch.cs` : tracer les changements de CS et les transferts autour de la routine 6012:46xx, depuis les registres déjà disponibles, sans accès mémoire ajouté ni modification des timings.
  - [x] Créer `artifacts/.tmp-tube/Dos512Flow.cs` avec le scénario 1.2 en lecture seule ; conserver le chemin des transferts conduisant à la routine destructive et l'adresse de son appelant.
  - [x] Restaurer `src/RetroMediaLab.Chips/CPU/Intel/MCS80x86/80186/Services/Cpu80186.Prefetch.cs`, supprimer `artifacts/.tmp-tube/Dos512Flow.cs` et son journal après consignation, puis reconstruire le banc initial en forçant l'actualisation de la source restaurée.

- [x] Évaluer puis rejeter le masque des autorisations de timers internes sans gain global.
  - [x] Créer `artifacts/.tmp-tube/TimerCard.cs` : reprendre le montage DOS 2.1 connu, mesurer trois fenêtres identiques de 80 champs à A> avec cycles et empreintes vidéo/audio, garantir le nettoyage ; compiler et conserver les DLL avant modification sous `artifacts/.tmp-tube/timer-before/`.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Intel/MCS80x86/80186/Services/Cpu80186.Timers.cs` : résoudre les timers recevant effectivement l'horloge interne lors des changements de contrôle, entrée, déclenchement et arrêt ; employer ce masque dans l'avancement, conserver les délais de sortie et la cascade du timer 2.
  - [x] Modifier `tests/RetroMediaLab.Chips.Tests/CPU/Intel/MCS80x86/80186/80186Tests.cs` : vérifier la fermeture/réouverture de porte et les changements de source d'horloge sans reset ; exécuter ensuite le groupe 80186.
  - [x] Modifier `artifacts/.tmp-tube/TimerCard.cs` uniquement si le banc nécessite une correction, comparer les mêmes DLL avant/après en Debug et conserver la modification de production seulement si le coût global gagne sans différence de cycles ou sorties.
  - [x] Supprimer `artifacts/.tmp-tube/TimerCard.cs`, les copies avant/après et journaux après consignation ; reconstruire le banc avec Program.cs.

- [x] Retirer le diagnostic d'opcode invalide qui relit le bus de production.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Intel/MCS80x86/80186/Services/Cpu80186.cs` : supprimer le compteur et la sortie Console qui effectuent douze lectures supplémentaires lors d'une faute, conserver intégralement l'interruption 6.
  - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmu80186BootDiagnostic.cs` : ancien diagnostic temporaire dépendant de ce champ privé, d'une image Temp et de captures à chemin fixe ; ses preuves sont remplacées par les passages DOS documentés.
  - [x] Modifier `tests/RetroMediaLab.Chips.Tests/CPU/Intel/MCS80x86/80186/80186Tests.cs` : vérifier qu'un opcode invalide entre dans le vecteur 6 sans relire son octet sur le bus pour journalisation ; exécuter le groupe autonome 80186.

- [x] Implémenter la suspension DMA DHLT imposée par NMI sur le 80186.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Intel/MCS80x86/80186/Constants/Interrupt80186Constants.cs`, `Services/Cpu80186.cs`, `Services/Cpu80186.Dma.cs` et `Services/Cpu80186.Instructions.cs` : nommer le bit DHLT déjà inscriptible, le positionner sur NMI matériel hors mode esclave, suspendre les nouveaux transferts DMA jusqu'à acquittement logiciel ou IRET ; conserver les transferts en cours, registres DMA, interruptions imbriquées et effets du mode esclave.
  - [x] Modifier `tests/RetroMediaLab.Chips.Tests/CPU/Intel/MCS80x86/80186/80186Tests.cs` : vérifier arrêt logiciel/reprise, NMI avant un transfert prêt, reprise par IRET, absence de suspension sur INT 2 logiciel et mode esclave ; exécuter le groupe autonome 80186.
  - [x] Modifier `artifacts/.tmp-tube/Master512Joystick.cs` après le passage courant : comparer le trajet DOS avec la correction matérielle seule, sans en présumer la cause ; conserver les verdicts et captures utiles avant suppression prévue dans le suivi machine.
  - [x] Créer `artifacts/.tmp-tube/master-dhlt-regression.trx` en exécutant les tests AcornEmuMasterTests en Debug après DHLT ; supprimer ce journal après consignation des résultats.

## Revue Master 512 — 9 novembre 2026

- [x] Raccorder et gérer le reset maintenu dans le CPU.
  - [x] Modifier `Constants/Cpu80186Constants.cs`, `Interfaces/ICpu80186.cs`, `Services/Cpu80186.cs` et créer `Services/Cpu80186.Reset.cs` sous `src/RetroMediaLab.Chips/CPU/Intel/MCS80x86/80186/` : entrée RES retenue et échéance locale, 6,5 CLKOUT avant prélecture, compteur demi-cycle conservé et aucun transfert sous reset (Intel Hardware Reference, figure 50 et section 4.2).
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMaster/Tube/Services/Master512Board.cs` : transmettre RES au composant et supprimer le gel logiciel de la carte.
  - [x] Créer `tests/RetroMediaLab.Chips.Tests/CPU/Intel/MCS80x86/80186/ResetPinsTests.cs` et modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuMasterTests.cs` : maintien, libération horodatée, nouvelle assertion et reprise sur carte ; exécuter le groupe CPU et les régressions Master.

Résultats : 138 tests CPU Debug réussis en 143 ms, puis 34 tests Master en 855 ms ; zéro échec ni ignoré. Source, comportements et fichiers documentés dans la fiche Intel80186 et la validation Master 512. Aucun gain global ou démarrage DOS nouveau revendiqué.
