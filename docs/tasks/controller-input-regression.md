# Réception des manettes et souris — corrections du 7 novembre

- [ ] Réparer les entrées physiques observées dans RML.
  - [x] Identifier la lecture native des deux manettes avant de modifier son fonctionnement.
    - [x] Créer temporairement `tests/RetroMediaLab.LocalEmulationTests/Emulation/ControllerReadingDiagnostic.cs` : relever les lectures détaillées, les codes de résultat et le runtime GameInput sélectionné ; arrêter le moniteur dans finally.
    - [x] Modifier `src/RetroMediaLab.App/Services/Input/GameInput/Monitoring/GameInputDeviceMonitor.cs` : conserver une référence IGameInputDevice plutôt que IUnknown pour les appels de lecture natifs ; vérifier les deux périphériques réels avec le diagnostic temporaire.
    - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/ControllerReadingDiagnostic.cs` après exploitation et reconstruire l’assembly de tests pour retirer le diagnostic.
  - [x] Corriger les noms des sticks.
    - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Emulation/Input/InputBindingEditorAnalogFunctions.cs` : employer le nom produit et le profil déjà utilisés dans les préférences, conserver les identifiants physiques des associations.
  - [x] Retirer le repli souris ajouté, à la demande de l’utilisateur.
    - [x] Modifier `src/RetroMediaLab.App/Controllers/Emulation/Machine/MachineInputController.cs`, `Contracts/Input/GameInput/GameInputPhysicalState.cs` et `Services/Input/GameInput/Reading/GameInputPhysicalInputReader.cs` sous `src/RetroMediaLab.App/` : supprimer le choix de source et la lecture des boutons Windows ajoutés ; conserver la capture et utiliser directement l’état GameInput.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornSessionInputRouteTests.cs` : retirer les cas du repli supprimé, conserver le contrôle du parcours GameInput et de l’absence de double comptage.
  - [x] Vérifier et nettoyer le bloc avant livraison.
    - [x] Remplacer l’assembly `tests/RetroMediaLab.LocalEmulationTests/bin/Debug/net10.0-windows10.0.19041.0/RetroMediaLab.LocalEmulationTests.dll` par compilation Debug et exécuter les tests concernés du parcours des entrées et des paramètres.
    - [x] Supprimer `artifacts/controller-reading-diagnostic.log`, sortie du banc temporaire dont les résultats sont consignés ci-dessous.
  - [x] Livrer le bloc corrigé.
    - [x] Remplacer `build/Debug/Retro Media Lab/retromedialab.exe`, ses bibliothèques et les modules avec `scripts/local-building.cmd --building=debug --modules=A` ; vérifier le résultat et les 24 modules.
  - [ ] Vérifier le résultat physique dans la livraison.
    - [ ] Créer `artifacts/TestsMachines/Acorn/BBC Micro Model B+/20261107-host-controller-*.jpg` depuis les préférences et l’assignation réellement manipulées par l’utilisateur, en distinguant les valeurs au repos des appuis/déplacements et le réglage hôte d’un résultat invité.

Constats utilisateur : vibrations actives sur Xbox Rematch et Xbox 360, axes/boutons sans réaction dans les préférences et lors de l’assignation. Souris AMX : arrêt correct et mouvements lents corrects, saccades rapides sur A, B et B+. Aucun de ces défauts n’est encore attribué au thème ou à GameInput par une mesure.

Diagnostic natif : IUnknown et IGameInputDevice ont des adresses différentes (écart de 8 octets dans ce runtime). GetCurrentReading échoue avec IUnknown (0x838A0003), réussit avec IGameInputDevice (S_OK), sur Xbox 360 et Rematch. Cette mesure isole le défaut de réception indépendamment des thèmes.

Après correction : le diagnostic réel passe en Debug pour les deux manettes ; timestamp non nul, état Gamepad présent, rapport brut de 18 octets lu. Runtime C:/Windows/system32/GameInputRedist.dll. Banc temporaire supprimé puis assembly reconstruit avec succès. L’historique du pointeur erroné remonte au refactoring GameInput du 16 septembre (`d655a42c5`), antérieur au déplacement des vues dans les thèmes. Les appuis physiques et mouvements rapides dans RML restent à confirmer dans la livraison.

Retrait du repli souris Windows et de HasPointerReading : le chemin public utilise à nouveau directement la lecture GameInput pour les déplacements et boutons. 48 tests ciblés passent en Debug, dont le parcours de session Acorn sans addition des événements Windows, les entrées Model A, l’adaptation AMX, le mélangeur et le sélecteur ROM. Ce résultat ne remplace pas l’essai utilisateur des mouvements rapides et des boutons dans la fenêtre livrée.

Livraison complète terminée avec code 0 : exécutable présent, 24 manifestes et leurs DLL d’entrée présents. La republication de l’application durant la construction des modules intègre le dernier retrait du repli souris ; SHA256 du DLL App livré identique au DLL de préparation (2B965C03407ABEAC65F1B59838E432CA35B02529B86E224DC1623…). Aucun résultat physique supplémentaire n’est déduit de cette vérification de livraison.

Observation RML après réouverture utilisateur : les valeurs d’axes sont désormais affichées dans Préférences → Manettes. Rematch au repos : valeurs visibles à zéro. Xbox 360 : deux captures du même état de fenêtre montrent notamment le stick droit X à 0,212 puis 0,118, et Y à −0,073 puis −0,080 ; la réception/actualisation existe. Cela ne prouve pas encore une amplitude complète, un retour au centre calibré, les boutons ou le parcours vers le joystick invité. Captures réelles `20261107-host-controller-neutral-1794086944130.jpg` et `20261107-host-controller-xbox360-axes-*.jpg` sous TestsMachines/Acorn/BBC Micro Model B+ ; ce sont des réglages hôtes, pas des résultats BASIC.

Confirmation utilisateur dans la livraison corrigée : « Oui, elles réagissent » après demande de mouvement du stick et d’appui sur A de la Rematch dans Préférences → Manettes. La réception physique des entrées Rematch est donc confirmée par l’utilisateur. Le passage de l’assignation et la consommation du joystick par la machine invitée restent à vérifier séparément.

Assignation : l’utilisateur confirme ensuite « Oui, le bouton A est reconnu ». L’observation directe montre les paramètres du Model A sélectionnés, et « Bouton de tir 1 » associé à A de la Xbox Rematch, statut vert. Capture `20261107-rml-equipped-input-rematch-assigned-*.jpg` dans TestsMachines/Acorn/BBC Micro Model A. Le nom produit correct est également affiché sur la ligne. Ce résultat valide la capture/association hôte ; la lecture ADC et du bouton par BASIC reste l’étape suivante.

## Associations modifiables à chaud — tous les modules

- [x] Appliquer les associations clavier, souris et manettes à la session ouverte sans recréer le cœur.
  - [x] Compléter le contrat et ses consommateurs.
    - [x] Modifier `src/RetroMediaLab.Emulation/Interfaces/IEmulationInput.cs` : ajouter la réception de la configuration d’associations de la session ouverte.
    - [x] Modifier `src/RetroMediaLab.Emulation.*/Common/Services/Machine.cs` pour les 24 modules : remplacer uniquement leurs données d’entrée courantes, conserver le cœur, la RAM, les médias et la configuration matérielle ; exposer les associations clavier courantes.
    - [x] Modifier `src/RetroMediaLab.App/Controllers/Emulation/Machine/MachineInputController.cs` : renouveler les raccourcis clavier de la machine et libérer les anciennes touches lors du changement.
    - [x] Modifier `src/RetroMediaLab.App/Theme/Base/Views/Controls/Emulation/Machine/MachineController.cs` : transmettre les associations enregistrées à la machine existante avant de renouveler la fabrique.
  - [x] Vérifier le bloc commun en Debug.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornSessionInputRouteTests.cs` : vérifier le changement des associations et leur consommation dans la même session, sans redémarrage.
    - [x] Remplacer les assemblies Debug des tests et modules par compilation ; exécuter les tests ciblés d’entrée, sans fermer la session RML utilisée par l’utilisateur.
  - [x] Livrer après fermeture confirmée par l’utilisateur.
    - [x] Remplacer `build/Debug/Retro Media Lab/retromedialab.exe`, ses bibliothèques et les 24 modules avec `scripts/local-building.cmd --building=debug --modules=A` ; vérifier le code de sortie, l’exécutable et les DLL des modules.
    - [x] Remplacer `build/Debug/Retro Media Lab/lib/retromedialab.app.dll` et son PDB par la compilation Debug finale d’App : inclure la garde de session éteinte ajoutée pendant la construction des modules, pour ne pas appeler l’ancien objet de machine détruit ; comparer les empreintes aux fichiers compilés.
  - [x] Confirmer une réaffectation clavier dans la même machine invitée.
    - [x] Modifier temporairement `artifacts/.tmp-rml-input/SendKeys.ps1` : accepter une combinaison de touches maintenues, vérifier la fenêtre cible avant émission et relâcher toutes les touches dans finally ; utiliser ce chemin déjà autorisé pour libérer la capture et ouvrir les paramètres pendant BASIC.
    - [x] Modifier temporairement `C:/Users/overt/AppData/Roaming/Retro Media Lab/Emulation/Machines/acorn/Configurations/997e0d119f684356beaa266efeadc0e6/machine.json` uniquement par les paramètres RML : associer le tir 1 à Espace pendant l’exécution de la boucle ADVAL ; créer `artifacts/TestsMachines/Acorn/BBC Micro Model A/20261107-hot-bindings-keyboard-*.jpg` pendant l’appui et après relâchement, sans reset.
    - [x] Modifier le même fichier uniquement dans RML pour associer le tir à Rematch ButtonA, conformément à la correction explicite de l’utilisateur, sans changer les axes ni les équipements ; conserver BASIC en exécution pour la suite physique.
    - [x] Modifier `artifacts/.tmp-rml-input/SendKeys.ps1` : retirer le paramètre temporaire de combinaison et son bloc après l’essai, en conservant le helper préexistant.
  - [x] Confirmer le changement à chaud avec la manette physique.
    - [x] Créer `artifacts/TestsMachines/Acorn/BBC Micro Model A/20261107-hot-bindings-*.jpg` : conserver les valeurs BASIC avant/après changement d’association dans la même session, sans extinction ni redémarrage.

Défaut d’actualisation établi le 7 novembre : `MachineController.ApplyConfigurationAsync` ne renouvelait que la fabrique et les médias ; la machine ouverte gardait ses anciennes associations. Le changement de sélection ne pouvait donc pas être confirmé par l’ADC tant que la session n’avait pas été recréée. Le bouton A déjà associé était consommé : BASIC affichait 32752 / 32752 / 1 lorsque l’utilisateur maintenait droite + A, contre 32752 / 32752 / 0 au repos. Captures `20261107-rml-equipped-input-joystick-neutral-1794087396749.jpg` et `20261107-rml-equipped-input-joystick-fire-axes-stationary-1794087438244.jpg` dans TestsMachines/Acorn/BBC Micro Model A ; copies des résultats dans TestsChips/Upd7002 et du tir dans TestsChips/Mos6522. Ces valeurs ne valident pas encore les axes physiques.

Correction commune : `IEmulationInput.UpdateBindings` transmet la configuration enregistrée à la machine existante ; chaque module ne remplace que ses données Input. Les 24 modules, dont Example, implémentent ce contrat. Les raccourcis clavier mis en cache sont renouvelés ; les anciennes commandes sont relâchées et une capture devenue désactivée est libérée. Aucune commande de reset, de création de cœur ou de changement des puces n’est ajoutée.

Vérification Debug : 50 tests ciblés passent (AcornSessionInputRouteTests et ControllerInputTests). Les deux cas ajoutés vérifient la prise en compte des nouvelles données clavier/souris/manettes par la projection de la même machine, la conservation des options matérielles, l’absence d’initialisation supplémentaire du cœur et la libération/remplacement des raccourcis clavier dans le contrôleur App. Ce passage ne prouve pas encore l’essai physique à chaud dans le build livré. Journal ignoré : `artifacts/input-hot-bindings-tests.log`.

Livraison du bloc : script Debug tous modules terminé avec code 0. App finale compilée sans avertissement ni erreur, puis DLL/PDB copiés dans lib avec empreintes identiques. Le renouvellement des raccourcis dans MachineChanged est vérifié dans l’IL final ; les paramètres changés pendant l’extinction seront donc repris à l’allumage suivant. Présence de l’exécutable et des 24 manifestes avec leurs DLL d’entrée vérifiée. Journaux ignorés `artifacts/input-hot-bindings-debug-build.log` et `artifacts/input-hot-bindings-final-app-build.log`. L’utilisateur a confirmé la fermeture de RML avant le build ; aucun lancement automatique pendant cet essai.


Validation RML Debug du 7 novembre, même session et même boucle BASIC conservées : l’assignation du tir est passée de Rematch X à Keyboard:Space par le dialogue pendant l’exécution. Espace maintenu donne ADVAL(0) AND 3 = 1 ; relâchement = 0. Preuves réelles `20261107-hot-bindings-keyboard-pressed-1794089232991.jpg` et `20261107-hot-bindings-keyboard-released-1794089272536.jpg`, dans TestsMachines/Acorn/BBC Micro Model A et copies dans TestsChips/Mos6522/BBC Micro Model A. Cadence ponctuelle autour de 50 FPS, 50,1 Hz et 2 MHz ; ce n’est pas une mesure comparative de gain.

Le stick physique maintenu par l’utilisateur fait ensuite varier ADVAL(1/2) : relevé 768/39408, preuve `20261107-hot-bindings-physical-right-1794089323701.jpg`, copie Upd7002. Après la correction explicite de l’utilisateur « X c’est pas A », le tir final est bien Rematch ButtonA. Configuration vérifiée et capture `20261107-hot-bindings-rematch-A-assigned-1794089457406.jpg`. Sans reset ni extinction, maintien physique de A : troisième colonne à 1 et axes variables, preuve `20261107-hot-bindings-rematch-A-pressed-1794089511721.jpg` ; après relâchement : 32752/32752/0, preuve `20261107-hot-bindings-rematch-A-rest-1794089551935.jpg`. Copies de ces deux résultats dans TestsChips/Mos6522 et TestsChips/Upd7002/BBC Micro Model A. Les quatre directions, le second port et les mouvements AMX rapides ne sont pas tous couverts par ces seuls relevés.

Le paramètre ChordKeys et son bloc temporaire sont retirés du helper préexistant après l’essai ; aucune touche laissée enfoncée. L’émulation est laissée ouverte à l’utilisateur, non possédée par les outils de tests. Le comportement des autres modules est couvert ici par la compilation et le contrat commun, pas par un essai physique de chaque module.

- [x] Enregistrer le bloc commun terminé.
  - [x] Modifier `.git/index` pour indexer le contrat, ses 24 consommateurs, les deux contrôleurs App, les tests et les preuves documentées de ce bloc ; créer l’objet commit et mettre à jour `.git/refs/heads/main` avec le message `fix: apply host input bindings to running emulator sessions`, sans artefacts ni autres travaux en cours.
