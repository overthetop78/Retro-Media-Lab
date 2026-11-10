# Circuit WAIT/READY de la carte 400,005

## Référence et fils réels

[Acorn System 5 Handbook](https://chrisacorns.computinghistory.org.uk/docs/Acorn/Manuals/Acorn_System5Handbook.pdf), §2.4/2.5 et figure 21, page imprimée 23 (PDF 62). Texte, PDF et schéma inspecté sous `artifacts/.tmp-archives/machines/Acorn/Acorn System 5/`. Le schéma précise les fronts et les entrées asynchrones au-delà du résumé textuel. La double bascule est documentée dans [SN74LS74A](../Logic/Sn74Ls74A.md).

| Liaison physique | Prise en compte |
| --- | --- |
| LK2=1 MHz, LK3=2 MHz, LK1=3 MHz vers CLK2, pin 11 | Front montant de la source sélectionnée. |
| MR vers D2, pin 12 | Entrée mémorisée, capturée par cette bascule. |
| Source sélectionnée OU /Q2 (pin 8), via IC17 | PH0 au CPU ; /Q2 haut maintient PH0 haut pendant WAIT. |
| PH0 vers CLK1, pin 3 | READY ne capture que sur un véritable front montant PH0. |
| NBRq OU NON(R/W), via IC1/IC18 vers D1, pin 2 | Une demande active basse ne suspend que les lectures. |
| Q1, pin 5 vers RDY et NBGt | Même niveau électrique ; NBGt bas concède le bus. |
| /Q1, pin 6 vers inhibition des buffers | Buffers adresse/commande inhibés pendant la concession. |
| PRE/CLR maintenus inactifs | Aucun reset logiciel ajouté à IC5. |

MR bas est capturé au prochain front montant source. PH0 est alors haut et reste haut malgré les fronts descendants suivants. MR haut est également capturé au prochain front montant source : comme la source est déjà haute, cette capture ne crée pas un nouveau front PH0. PH0 redescend au front descendant suivant ; le prochain front montant peut de nouveau capturer READY. Une variation de NBRq pendant l'étirement n'invente pas de capture READY supplémentaire. Les autres sorties du générateur libre continuent.

## Réalisation du 9 novembre 2026

`src/RetroMediaLab.Chips/Controllers/Acorn/SystemCpuClock/` contient constantes, contrat `ISystemCpuClockControl` et service `SystemCpuClockControl`. Le contrôleur raccorde une instance complète de SN74LS74A, sans remplacer les bascules par des compteurs dans la machine. D, niveaux CLK, captures, fréquences nominales et compteurs appartiennent à la puce ; le circuit conserve les niveaux raccordés aux portes et aux sorties. Les trois liens réels sont acceptés, aucune fréquence supplémentaire n'est inventée.

Les entrées MR/NBRq/R/W sont transmises au changement et mémorisées. Les sorties PH0 et Q1 ne sont notifiées qu'à mutation. Sur un front PH0, READY est résolu avant publication vers les récepteurs de PH0. Les deux sorties Q1 et /Q1 sont complémentaires dans ce montage, puisque les contrôles asynchrones restent inactifs. La graine Q1=Q2=haut choisit une phase déterministe de fonctionnement normal ; elle n'est pas une valeur de power-on garantie par le constructeur. Aucun ordonnanceur commun, file, délai d'un tick, thread ou ressource native.

## Contrôles et débit actuels

**Neuf contrôles autonomes Debug réussis, zéro échec/ignoré.** `tests/RetroMediaLab.Chips.Tests/Controllers/Acorn/SystemCpuClockControlTests.cs` vérifie les trois fréquences, les deux phases initiales lors du changement de MR, maintien/reprise sans front fictif, NBRq pendant lecture/écriture, ordre READY puis PH0, niveaux de concession/buffers, absence de doublons et READY retenu pendant WAIT. Le branchement au vrai `SystemClockGenerator` conserve ses fronts indépendants 1 MHz et ses compteurs pendant WAIT et DMA. Aucun CPU, logiciel invité ou machine complète n'est présenté comme vérifié par ces seuls contrôles.

Mesure isolée du code actuel, Debug net10.0 hors RML, sans autre compilation/test en cours : 24 000 périodes de chauffe puis trois passages de 2 400 000 périodes normales 2 MHz. PH0 possède un observateur ; MR et NBRq restent hauts, R/W en lecture. Chaque passage vérifie 4 800 000 notifications PH0 et l'égalité des compteurs source/CPU.

| Passage | Durée hôte | Millions de périodes sélectionnées/s |
| --- | --- | --- |
| 1 | 316,199200 ms | 7,590152 |
| 2 | 311,325800 ms | 7,708966 |
| 3 | 309,363600 ms | 7,757862 |

Ce débit concerne le circuit après sélection ; il n'inclut ni la production de ses fronts par la source 24 MHz, ni le CPU et les autres récepteurs. Le banc temporaire est supprimé après consignation et l'assembly régénérée. Les tests autonomes restent. Le raccordement aux broches du CPU dans le module, à ses accès de bus et aux autres circuits de la machine constitue la suite du travail System 5 ; ce document ne clôture pas la machine.


### Débit isolé remesuré sur le code actuel — 9 novembre 2026

Code de production 2480c530d, Debug normal, hors RML. 100 000 périodes de chauffe, puis trois fenêtres de 2 000 000 périodes complètes appelées par un délégué Action ; ce coût d'appel est inclus. Entrées conservées pendant la mesure, aucun récepteur externe de notification ; les opérations internes et leurs raccordements restent actifs. Ces mesures ne valent ni pour un composant sans activité interne ni pour tous les récepteurs d'une carte assemblée. Pas de mesure Release dans ce passage.

Sélection 2 MHz, MR et NBRq hauts, R/W en lecture ; réception de chaque front montant puis descendant. Débits : **7,929548 / 7,978258 / 7,588961 millions de périodes sélectionnées/s**, soit **396,477378 / 398,912883 / 379,448032 %** de 2 MHz. Ce contrôle isolé dépasse sa cadence requise mais n'inclut ni le générateur 24 MHz ni ses destinataires CPU et bus. Le raccordement System 5 est désormais réalisé ; voir sa validation actuelle.

Diagnostic temporaire réussi et supprimé après consignation ; assembly de tests régénérée sans ce banc utilisant les ROM externes pour son autre partie. Aucun changement de production pendant ces mesures.

### Périodes PH0 déjà représentées —10 novembre2026
Le contrôleur mémorise maintenant l'entrée MR et qualifie le regroupement uniquement avec MR/NBRq hauts, READY établi, WAIT inactif et deux niveaux d'horloge bas. AdvanceRepresentedClockPeriods fait avancer les comptes dans les deux bascules74LS74 par leur méthode stable existante ; le propriétaire doit avoir déjà représenté les effets PH0 au CPU. Aucun callback PH0/READY n'est rejoué et toutes les autres phases restent sur le chemin ordinaire. Prévalidation des deux comptes avant mutation.
13 contrôles autonomes réussissent, zéro échec/ignoré,119 ms : trois liens1/2/3 MHz, plages1/100001 périodes, reprise WAIT et DMA avec traces identiques, refus des phases non qualifiées et du dépassement long.MaxValue avant mutation. Le raccordement macro CPU/vidéo reste à faire ; ce résultat n'est pas une mesure de performance machine.
