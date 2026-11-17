# Reprise des blocs SH-2 selon le DRC PicoDrive

- [x] Recueillir les fonctions de compilation deja identifiees
  - [x] Creer `artifacts/References/Machines/Sega/32X/PicoDrive/cpu/sh2/compiler.c` et `compiler.h` depuis la revision PicoDrive deja collectee : conserver compilation, decoupage, reutilisation et invalidation de reference.
  - [x] Creer `artifacts/References/Machines/Sega/32X/PicoDrive/cpu/sh2/compiler-source-index.json` : enregistrer revision, URL et SHA256 des deux sources recueillies.
- [x] Refaire les blocs sans activer la route par defaut
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Sh2.Blocks.cs` : decoder jusqu'a 64 operations avec lectures incluses et arret aux ecritures/controle, reutiliser leur contenu tant que le code effectif ne change pas, conserver des variantes inactives et un lookup direct, emettre les suites sans acces bus en lot si budget/echeance le permettent et garder une execution de prefixe bornee sinon ; verifier le code apres les lectures susceptibles de synchroniser l'autre CPU ou de remplacer des lignes de cache.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Sh2.BlockDecoder.cs` : exposer les effets de controle, memoire et synchronisation utilises pour definir les frontieres de blocs en conservant les semantiques existantes.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Sh2.BlockInliner.cs` et `Sh2.NativeRuntime.cs` : reutiliser les aides optimisees du runtime pour les blocs, garder compilation et gestion de cache hors du chemin chaud copie et conserver l'interpreteur de reference.
  - [x] Modifier `src/RetroMediaLab.Chips/Controllers/Hitachi/SH7095/Sh7095.cs` uniquement pour `CpuBus.CodeVersion` : distinguer ligne cache presente, absente et code non compilable ; incorporer index, version de contenu et CCR pour rendre l'invalidation des blocs coherente avec le cache materiel.
  - [x] Modifier `src/RetroMediaLab.Chips/Controllers/Hitachi/SH7095/Sh7095.cs` dans `Write8` P0 : incrementer la version de la ligne cache si et seulement si son octet est modifie, comme les autres chemins d'ecriture du cache, sans changer l'etat serialise ni les horloges.
  - [x] Validation groupee par le parent : verifier instructions, auto-modification, IRQ, budgets/etats et mesures Debug puis Release ; laisser `UseCompiledBlocks` desactive par defaut.

References utilisees : `scan_block` (compiler.c:6094), `sh2_translate` et reutilisation des blocs inactifs (3253), invalidation SMC par plages (5590). La route adaptee utilise le JIT IL .NET pour l'emission hote. Elle ne porte pas encore l'allocation explicite des registres, la propagation des constantes ou les liens natifs entre blocs de PicoDrive. Les lectures sont conservees dans les blocs mais imposent une verification des versions avant la suite ; les ecritures et instructions de controle terminent le bloc. `DT` reste une barriere car son acceleration existante peut consommer le budget. Aucun gain n'est affirme avant les tests du parent.

- [x] Eviter les validations de lignes apres lecture sans possibilite de changement du code
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Contracts/ISh2Bus.cs` : exposer `ulong CodeEpoch(uint address)`, avec zero pour absence d'information et maintien de la validation complete dans ce cas.
  - [x] Modifier `src/RetroMediaLab.Chips/Controllers/Hitachi/SH7095/Sh7095.cs` dans `CpuBus` : fournir l'epoque de mapping cache pour P0 code-cache et C0 cache-RAM, deleguer l'epoque aux zones externes lorsque le code est lu directement.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Sh2.Blocks.cs` : retenir l'epoque apres la validation d'entree, poursuivre directement apres lecture si une epoque non nulle reste identique et verifier toutes les versions seulement si l'epoque change ou n'est pas fournie ; conserver la verification d'entree pour les ecritures propres au CPU.
  - [x] Validation groupee par le parent avec l'epoque SDRAM ajoutee par l'autre agent ; ne pas activer les blocs par defaut et ne pas annoncer de gain avant mesure.

- [x] Associer les blocs natifs a leur module de code genere
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Sh2.Blocks.cs` dans `CompileBlock` : pour un CPU cree par le runtime natif, associer le `DynamicMethod` a `GetType().Module` ; conserver le constructeur avec type proprietaire original pour le CPU de reference. Ne changer aucun attribut de compilation de l'application.
  - [x] Validation groupee du parent : mesurer Debug puis Release et comparer etats/images avant toute conclusion sur le gain.

Justification de ce dernier choix : source officielle dotnet/runtime, `DynamicMethod.Init` associe le module du proprietaire et `DynamicMethod.CoreCLR.GetMethodDescriptor` transmet ce module a `GetCallableMethod`. CoreCLR `CEEInfo::getJitFlags` appelle `GetDebuggerCompileFlags(ftn->GetModule())`, qui active `CORJIT_FLAG_DEBUG_CODE` lorsque le module desactive les optimisations. La branche DynamicScope retire `DEBUG_INFO`, pas `DEBUG_CODE`, et `LCGMethodResolver.GetJitFlags` ne fournit pas de drapeau contraire. Cette inspection explique pourquoi le module associe est pertinent ; elle ne mesure pas encore les drapeaux effectifs du banc ni un gain.

- [x] Ajouter les barrieres de code et les liaisons de blocs du modele PicoDrive
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Contracts/ISh2Bus.cs` : ajouter `MarkCode(uint start, int length)` sans effet par defaut, avec longueur en octets.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Sh2.Blocks.cs` : marquer les plages compilees, retenir leur epoque, eviter la validation des lignes si une epoque non nulle est identique, conserver deux successeurs par PC et emettre une liaison tail-call vers un successeur valide lorsque budget, evenements, IRQ et etat de branche l'autorisent ; garder les recherches/compilations froides dans le dispatcher externe et desactiver les anciennes liaisons lors d'invalidation/eviction.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Sh2.BlockDecoder.cs` et `Sh2.Blocks.cs` : reconnaitre les branches delayed et inclure seulement leur delay-slot arithmetique sur ; garder le fallback lorsque le slot ne peut etre fusionne sans perte.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Sh2.Blocks.cs` et `Sh2.NativeRuntime.cs` : exposer le temps de construction/JIT des blocs, preparer explicitement le delegate apres emission, partager les nouvelles aides optimisees avec le runtime natif.
  - [x] Validation groupee par le parent : comparer froid/chaud et verifier SMC, ecritures hors code, liens, IRQ et contextes ; conserver la route interpretee et le defaut sans blocs.

Limites de cette etape : les liens restent des appels terminaux managed, pas des patches de branche machine propres a une architecture hote. Les allocations explicites de registres et la propagation de constantes de PicoDrive ne sont pas portees. Le scan utilise encore Fetch16 et peut pre-remplir les lignes du cache materiel avant leur execution ; les timings de miss dans le mode cache materiel ne sont donc pas declares equivalents a ceux de l'interpreteur. La route directe de type Gens et le mode cache materiel doivent etre identifies dans les mesures. `CompiledBlockBuildTicks` couvre scan, emission IL et preparation explicite du delegate ; il ne mesure pas tout le JIT des aides partagees. `RuntimeHelpers.PrepareDelegate` est present dans les sources CoreCLR .NET10, sans changement de drapeaux du build de l'application.

- [ ] Ajouter le payload pre-valide par borne de cycles du modele scan_block PicoDrive
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Contracts/ISh2Bus.cs` et `src/RetroMediaLab.Chips/Controllers/Hitachi/SH7095/Sh7095.cs` : exposer une borne `MaximumWaitStates` negative par defaut et deleguee au bus externe par CpuBus ; aucune constante Sega dans la puce generique.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Sh2.Blocks.cs` : emettre un fast payload pour les instructions lineaires tant que leur pire cout fetch/data rentre dans le budget et avant la prochaine echeance, charger seulement les attentes reelles, supprimer Begin/Complete du payload et conserver l'ancien chemin borne quand la precondition echoue ; branches, DT et delay-slots restent sur ce chemin borne.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Sh2.Blocks.cs`, `Sh2.cs` et `Sh2.NativeRuntime.cs` : rendre explicitement inlinables les seules aides d'horloge/fin/post-lecture et conserver cette directive dans leur copie native ; rafraichir l'echeance de bloc apres lecture et verifier a nouveau la borne restante si un registre MMIO rapproche un evenement.
  - [ ] Validation parent : equivalence horloges, IRQ/yield/SMC, MMIO rapprochant une echeance, fallback sans borne, listing natif et Debug/Release ; ne pas activer les blocs par defaut.

## Banc SH-2 isole pour validation Debug/Release

- [x] Valider le cœur et le SoC SH-2 sans compiler les autres émulateurs
  - [x] Créer `tests/RetroMediaLab.Chips.Tests/.work/SegaSh2Validation/Validation.csproj` avec l’assembly `RetroMediaLab.Chips.Tests`, les seules sources `CPU/Hitachi/Sh2Tests.cs` et `SegaEmu/Mega32XChipTests.cs`, les dépendances xUnit déjà utilisées et une référence à `src/RetroMediaLab.Chips/RetroMediaLab.Chips.csproj`.
  - [x] Modifier `docs/projects/emulations/Sega/SegaEmu-tests-autres-jeux.md` : après exécution de ces tests ciblés en Debug puis Release, consigner le résultat, le blocage du projet complet par son test UPD780C sans helper Z80, et le résultat Mega32X Debug déjà obtenu sans répéter les tests machine.
  - [x] Supprimer `tests/RetroMediaLab.Chips.Tests/.work/SegaSh2Validation` et ses sorties `bin/obj` après les deux configurations, puis vérifier que le harnais temporaire a disparu.

- [x] Corriger l’état final du compte rendu
  - [x] Modifier `docs/projects/emulations/Sega/SegaEmu-tests-autres-jeux.md` : remplacer la formulation au futur sur la suppression du harnais par le constat vérifié au passé.

- [x] Ajouter un contrôle ciblé du payload rapide lors d’un MMIO qui rapproche une échéance
  - [x] Modifier `tests/RetroMediaLab.Chips.Tests/CPU/Hitachi/Sh2Tests.cs` : rendre le bus mémoire de test configurable avec `MaximumWaitStates`, puis ajouter un test où une instruction de lecture déclenche un événement MMIO plus tôt que la borne initiale et comparer état CPU, mémoire et cycles au chemin interprété.
  - [x] Créer temporairement `tests/RetroMediaLab.Chips.Tests/.work/SegaSh2Validation/Validation.csproj` avec seulement `Sh2Tests.cs`, `Mega32XChipTests.cs` et la référence Chips, puis lancer le nouveau test en Debug et en Release.
  - [x] Modifier `docs/projects/emulations/Sega/SegaEmu-tests-autres-jeux.md` : noter les résultats du contrôle MMIO et préciser les validations du payload qui restent non couvertes.
  - [x] Supprimer `tests/RetroMediaLab.Chips.Tests/.work/SegaSh2Validation` et ses `bin/obj`, puis vérifier que le harnais a disparu.

- [x] Valider l’invalidation d’un bloc réellement exécuté depuis la SDRAM 32X
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/Sega/SegaEmu/Mega32XIntegrationTests.cs` : ajouter un programme synthétique qui démarre du code copié en SDRAM, active `UseCompiledBlocks`, exécute une instruction, remplace son opcode en SDRAM puis vérifie l’opcode repris et la recompilation, avec `Dispose` du board dans `finally`.
  - [x] Créer temporairement `tests/RetroMediaLab.LocalEmulationTests/.work/SegaSh2Validation/Validation.csproj` : assembly `RetroMediaLab.LocalEmulationTests`, sources d’intégration Sega nécessaires seulement, dépendance xUnit et références limitées aux projets Emulation.Sega et leurs dépendances directes ; exécuter le test ciblé en Debug et Release.
  - [x] Modifier `docs/projects/emulations/Sega/SegaEmu-tests-autres-jeux.md` : consigner l’invalidation SDRAM réelle et les éventuels blocages de compilation de ce banc ciblé.
  - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/.work/SegaSh2Validation` et ses `bin/obj`, puis vérifier sa disparition.

- [x] Vérifier qu’une écriture hors du code compilé reste visible sans invalider ce code
  - [x] Modifier `tests/RetroMediaLab.Chips.Tests/CPU/Hitachi/Sh2Tests.cs` : ajouter un test interpréteur/blocs où un mot de données hors du bloc mais dans la même page de version est changé après compilation ; vérifier valeur lue, contexte, cycles et absence de reconstruction du bloc.
  - [x] Créer temporairement `tests/RetroMediaLab.Chips.Tests/.work/SegaSh2Validation/Validation.csproj`, exécuter le test ciblé Debug et Release, puis enregistrer les deux résultats dans `docs/projects/emulations/Sega/SegaEmu-tests-autres-jeux.md`.
  - [x] Supprimer `tests/RetroMediaLab.Chips.Tests/.work/SegaSh2Validation` et ses `bin/obj`, puis vérifier sa disparition.

- [x] Consigner la validation des barrières et liaisons de blocs
  - [x] Modifier `docs/projects/emulations/Sega/SegaEmu-tests-autres-jeux.md` : relier les tests Debug/Release déjà exécutés aux cas SMC, écriture hors code, liens, IRQ/contexte et réutilisation froide/chaude ; distinguer la réutilisation de blocs d’une mesure FPS échauffée.

- [x] Tenter et consigner la capture d’un listing natif JIT pour un bloc SH-2
  - [x] Créer temporairement `tests/RetroMediaLab.Chips.Tests/.work/SegaSh2Validation/Validation.csproj` avec les deux suites ciblées et référence Chips ; lancer en Debug et Release le test `CompiledBlockReadsUpdatedDataOutsideItsCodeRangeWithoutRebuild` avec la variable `COMPlus_JitDisasm` réglée sur `SH2_00000100_1`, et capturer stdout dans `jit-Debug.txt` et `jit-Release.txt` sous `.work`.
  - [x] Modifier `docs/projects/emulations/Sega/SegaEmu-tests-autres-jeux.md` : noter si le runtime fournit le listing et les appels/branches observés dans le bloc, sans en déduire un gain FPS non mesuré.
  - [x] Supprimer le harnais temporaire, les listings et les sorties `bin/obj`, puis vérifier leur disparition.

- [x] Corriger la chronologie de l’essai Release du listing JIT
  - [x] Modifier `docs/projects/emulations/Sega/SegaEmu-tests-autres-jeux.md` : consigner que `--no-build` a d’abord échoué faute de DLL Release du harnais, puis que le test a réussi après suppression de cette option ; préciser qu’il ne s’agissait pas d’un plantage .NET.
