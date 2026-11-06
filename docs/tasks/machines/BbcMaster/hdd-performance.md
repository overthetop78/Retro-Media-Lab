# Diagnostic du coût HDD sur BBC Master

- [x] Reproduire la comparaison sur la carte complète sans modifier les réglages utilisateur.
  - [x] Modifier `artifacts/.tmp-65c12/native/Program.cs` : ajouter le scénario Winchester activé sans image, conserver les mêmes ROM, cycles, entrées et mesures ; relever les états internes hors de la boucle mesurée ; ouvrir le HDD existant en lecture seule via EmulationSdk, le détacher en finally et comparer le coût du rendu mécanique sans sortie audio Windows.
  - [x] Modifier `artifacts/.tmp-65c12/native/Native.csproj` : référencer NAudio 2.3.0 déjà utilisé par MediaEngine pour le rendu mécanique du diagnostic ; lier aussi les enregistrements mécaniques livrés dans Assets et intercepter les erreurs du diagnostic pour éviter une boîte de plantage ; supprimer ces ajouts lors du nettoyage.
  - [x] Modifier `docs/tasks/machines/BbcMaster/hdd-performance.md` avec les résultats Debug et Release, les conditions et les limites ; isoler ensuite le disque monté si le contrôleur seul ne reproduit pas la baisse.
- [x] Consigner et nettoyer le diagnostic temporaire.
  - [x] Modifier `docs/chips/CPU/Mos65C12.md` : consigner la comparaison Master complète et distinguer ce banc des performances de la fenêtre RML.
  - [x] Supprimer les ajouts de diagnostic HDD de `artifacts/.tmp-65c12/native/Program.cs` et les sorties temporaires associées après consignation des résultats.

- [x] Retirer les exécutables temporaires du diagnostic après les mesures.
  - [x] Supprimer `artifacts/.tmp-65c12/native/bin/` et `artifacts/.tmp-65c12/native/obj/` : retirer les assemblies HDD de diagnostic, leurs dépendances et copies des assets ; conserver les sources du diagnostic CPU encore en cours.

## Mesures du 6 novembre 2026

Carte Master 128 complète, MOS 3.50 stock, aucun coprocesseur, WD1770 activé, entrée clavier physique simulée identique. Mesures après 20 images de chauffe, sur 80 images ; rendu vidéo et vidange audio conservés. Aucun réglage utilisateur modifié. HDD existant ouvert en lecture seule par le SDK et détaché/libéré dans finally. Aucun périphérique audio Windows ouvert. Les mesures ont commencé après la fin du build All et du build du banc.

| Debug (ms/image) | Repos | SOUND | Redessin BASIC |
| --- | ---: | ---: | ---: |
| Winchester désactivé | 20,126 | 19,888 | 20,119 |
| Winchester activé, sans image | 19,159 | 20,679 | 22,447 |
| HDD monté, sans rendu mécanique | 19,869 | 20,530 | 20,842 |
| HDD monté, avec rendu mécanique | 19,565 | 20,291 | 20,699 |

| Release (ms/image) | Repos | SOUND | Redessin BASIC |
| --- | ---: | ---: | ---: |
| Winchester désactivé | 3,387 | 3,346 | 3,461 |
| HDD monté, avec rendu mécanique | 3,572 | 3,648 | 3,759 |

Les états relevés après le repos sont `normalSpeedIdle=True`, `canDeferClock=True`, `deferredIdleTicks=1570628`, `clockFraction=0`, avec et sans image. Le contrôleur utilise effectivement son chemin différé : ces essais ne montrent pas une exécution de la totalité du contrôleur à chaque cycle au repos.

Les trois empreintes vidéo sont identiques pour tous ces passages et entre Debug/Release :
- Repos : 18801ECDA8CA9D5138A337E66F2B0AD0C64725E2CF0CE724BD0D279D57790979.
- SOUND : 0DC55D1D27BBA23B266B930E5A4C6D01D8518AB26B55FE1B69C665D3C532E2BA.
- Redessin : 6564B39346B52021BBC29A9ABD92678FBD87667E707D5F80A42D79C343DE7033.

Avec Winchester : 3194877 / 3194881 / 3194881 cycles mesurés ; sans Winchester : 3194881 / 3194881 / 3194879. Le décodage de l'interface modifie le parcours du MOS : les images sont identiques, mais ce ne sont pas des traces CPU strictement identiques entre présence et absence du matériel. Pour un même équipement les cycles, PC et empreintes correspondent entre Debug et Release.

Limites : passages uniques successifs, dispersion Debug visible, aucune validation statistique d'un gain ; aucune optimisation matérielle ou logicielle HDD appliquée. Ce banc ne comprend ni l'adaptateur complet EmulatorCore, ni le mélange final, ni la sortie audio Windows, ni la présentation WPF ou la perte de focus. Il ne prouve donc pas l'origine de la baisse observée dans la fenêtre. Le rendu mécanique séparé utilise le profil Winchester livré et les échantillons, sans reconfiguration par image ni sortie audio.

Deux erreurs de préparation du diagnostic (NAudio privé non copié, puis Assets manquants) ont été corrigées dans le banc temporaire. Elles ne sont pas des erreurs de RML et ne sont pas incluses dans les mesures.
Nettoyage : options HDD/mécanique et références supplémentaires retirées du banc CPU. Les sorties d'essai n'ont pas été enregistrées en fichiers, hormis le tampon master.frame déjà utilisé par le banc CPU. Aucun fichier de média utilisateur modifié. Les assemblies temporaires, leurs dépendances et copies des assets ont été supprimées avec les dossiers bin/obj du banc.

## Adaptateur complet après le banc de carte

- [x] Mesurer les traitements ajoutés par EmulatorCore, sans manipuler RML.
  - [x] Créer `artifacts/.tmp-65c12/native/AdapterDiagnostic.cs` : charger en lecture seule la configuration Master via le lecteur existant, conserver une copie en mémoire, comparer sans HDD / avec HDD protégé / sans son mécanique, vider la file audio et comparer cycles et empreintes ; utiliser un dossier RTC temporaire possédé et nettoyer le cœur puis le dossier en finally.
  - [x] Modifier `artifacts/.tmp-65c12/native/Program.cs` et `Native.csproj` : raccorder uniquement ce diagnostic temporaire et ses ressources audio existantes.
  - [x] Modifier ce document avec les coûts globaux Debug/Release observés et les limites du banc.
- [x] Supprimer les consultations Winchester au repos sans changer les horloges matérielles.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/WinchesterController.cs` : conserver son compteur temporel et exposer AdvanceTo et la prochaine échéance ; regrouper seulement les périodes déjà démontrées inactives, conserver le chemin actif exact.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/Common/Services/Winchester.cs` : synchroniser avant chaque changement d'entrée et accès bus, notifier les changements de prochaine échéance.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/Common/Services/BbcMicroMachine.cs` : raccorder les échéances Winchester comme celles des autres composants et retirer son appel systématique au repos.
- [x] Vérifier le bloc de raccordement temporel complet.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuWinchesterStorageTests.IdleClock.cs` : comparer l'avance groupée aux ticks unitaires avant/après commandes, reset, changement de vitesse et raccordements ; vérifier l'absence de réveil au repos et sa reprise sur accès.
  - [x] Modifier `artifacts/.tmp-65c12/native/AdapterDiagnostic.cs` et créer `artifacts/.tmp-65c12/adapter-before/` : identifier la configuration par empreinte et comparer alternativement le module Debug livré avant modification et le module modifié, avec les mêmes dépendances Chips ; supprimer la copie avant clôture.
  - [x] Modifier `artifacts/.tmp-65c12/native/Native.csproj` : aligner les références du banc comparatif sur la version technique 2.0.0 demandée par la DLL module livrée, sans modifier la compilation de production ni les optimisations Debug ; renouveler sa copie avant/après.
  - [x] Créer `artifacts/.tmp-65c12/adapter-before-release/` : conserver la DLL Acorn Release du banc avant modification et préparer la comparaison avec la nouvelle DLL ; supprimer cette copie après mesure (comparaison alternée invalidée par la version de module attendue).
  - [x] Modifier `artifacts/.tmp-65c12/native/Program.cs` : isoler le point initial des références de modules pour capturer aussi les erreurs de chargement du banc avant son exécution, sans dialogue de plantage.
  - [x] Modifier `docs/chips/Controllers/AdaptecAcb4000.md` : décrire le raccordement temporel Winchester, ses équivalences et ses mesures sans présenter les puces inchangées comme réécrites.
  - [x] Modifier ce document avec les tests de régression et mesures complètes avant/après Debug/Release ; conserver seulement un changement équivalent et globalement utile.
- [x] Nettoyer ce deuxième diagnostic après les mesures.
  - [x] Supprimer `artifacts/.tmp-65c12/native/AdapterDiagnostic.cs`, son branchement et ses dépendances supplémentaires dans `Program.cs` / `Native.csproj`, les dossiers bin/obj, adapter-before, adapter-before-release et les dossiers RTC temporaires possédés.
### Adaptateur complet avant changement du raccordement Winchester

Configuration Master utilisateur copiée en mémoire ; supports protégés pour le banc ; 180 images de chauffe puis 160 mesurées ; MOS 3.50, pas de Turbo. EmulatorCore effectue le mélange audio et ses parcours habituels, puis le banc vide les blocs audio. Pas de sortie Windows ni de présentation WPF. Dossier RTC temporaire indépendant supprimé dans finally, aucune configuration persistée.

| Équipement | Debug ms/image | Release ms/image |
| --- | ---: | ---: |
| Sans HDD | 20,030 | 3,882 |
| HDD monté, sans son mécanique | 20,360 | 3,692 |
| HDD monté, avec son mécanique | 20,514 | 3,961 |

Chaque passage : 6389758 cycles CPU ; PC EB01 sans HDD, E9C1 avec HDD. Empreinte vidéo : ABF3AAC384ABC3C5E8EA46CC8BABA6A2C14D8FB0ED7A72B376FB8FF4826E4A3D. Les valeurs ne sont pas des FPS de fenêtre ; la dispersion est visible en Release. L'essai ne reproduit pas la forte baisse rapportée et ne clôt pas la recherche dans le chemin hôte.
### Résultats du raccordement par échéance

- Debug : 59 tests Winchester réussis, 35 s ; Release : 59 réussis, 5 s. Aucun test de ce groupe exclu.
- Premier passage après changement : sans HDD 19,367 ms/image ; HDD sans bruit 19,154 ; HDD avec bruit 19,180. Ces chiffres seuls ne suffisent pas à déduire le gain, car tous les parcours varient aussi entre passages.
- Comparaison alternée Debug avec HDD et son : ancien module 19,989 puis 20,124 ms/image ; nouveau module 19,247 puis 19,130. Moyennes 20,0565 / 19,1885 : réduction de 4,33 %.
- Chaque passage alterné utilise la configuration SHA-256 23533C9CAC7B084B134790F1CBEA314E386C0B0CCBECC91B1766AFC4C4E1FF4E, la même DLL Chips BEB21439DF13EA27309B47FD6C8210182F95BC952845EE3FF53F552802051CEC, 6389758 cycles mesurés, PC E9C1, vidéo ABF3AAC384ABC3C5E8EA46CC8BABA6A2C14D8FB0ED7A72B376FB8FF4826E4A3D.
- Release après changement : 3,592 ms/image, mêmes cycles, PC et vidéo. Avant : 3,961 dans le passage précédent. La tentative alternée Release attendait un module 2.0 avec une DLL historique 1.0 : échec de préparation du banc, pas une erreur de RML ; elle n'est pas retenue comme mesure.
- La première tentative alternée Debug avait aussi des versions de dépendances différentes (module livré demandant Chips 2.0, banc ayant Chips 1.0) ; elle a été exclue. La comparaison Debug ci-dessus a été exécutée après alignement des dépendances, sans activer les optimisations Release en Debug.

Le changement est conservé : moins d'appels au repos et gain global Debug confirmé dans ce scénario, sans divergence des sorties et cycles vérifiés. Il ne termine pas la migration des autres composants et n'explique pas à lui seul la perte de focus. Aucun nouveau build de livraison, aucun réglage utilisateur changé, aucun logiciel Windows audio lancé par ces bancs.
Nettoyage du deuxième banc terminé : source AdapterDiagnostic et branchement retirés, références NAudio/Assets/versions temporaires retirées du projet natif, copies avant/après et bin/obj supprimés. Aucun dossier RTC adapter possédé restant. Les sources CPU temporaires encore nécessaires sont conservées selon leur suivi propre.

## Point de sauvegarde du bloc

- [x] Isoler le raccordement Winchester des changements CPU encore en cours.
  - [x] Créer puis supprimer `artifacts/.tmp-65c12/winchester-stage.patch` : ne retenir dans BbcMicroMachine que les trois changements de raccordement Winchester ; indexer ces changements avec WinchesterController, Winchester, les tests IdleClock et les deux documents de ce bloc, sans fichiers temporaires ni modifications CPU.
  - [x] Modifier ce document pour enregistrer la clôture du bloc vérifié, puis committer uniquement ces fichiers et changements.