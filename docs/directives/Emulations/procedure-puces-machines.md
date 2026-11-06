# Directives permanentes — puces et machines

Directives confirmées par l'utilisateur les 6 et 7 novembre 2026. Elles s'appliquent à toutes les puces, interfaces, contrôleurs et machines ; les particularités matérielles restent décrites dans leurs documents existants. Réutiliser les composants déjà terminés et vérifiés sans les réécrire inutilement. Les sauvegardes et chargements d'état sont reportés.

- Respecter également les directives de [développement des émulateurs](../emulation-development.md).
- Ne pas appliquer `docs/directives/emulation-uniformity.md` au travail actuel : l'utilisateur l'a explicitement exclu de ce périmètre.

Ces règles doivent toujours être respectées pendant le développement, les corrections, les optimisations et les validations. Ce document ne constitue pas une liste de tâches : il ne se coche pas, ne se clôture pas et reste applicable aux travaux suivants. Les actions concrètes et leur avancement restent dans les suivis sous `docs/tasks/`.

## Règles pour chaque puce

- Tenir les documents à jour directement en appliquant ces directives. Ne pas ajouter aux listes de tâches une entrée dédiée à chaque modification de fichier Markdown : la consignation des informations, décisions, corrections et résultats fait partie intégrante du travail concerné.
- Définir le travail concret dans `docs/tasks/chips/<Chip>/`, avec les fichiers concernés, avant de modifier le code.
- Reprendre dès le départ les directives d'amélioration consignées ci-dessous. Ajouter à la liste de travail les actions applicables à cette puce avant de coder, pour ne pas devoir les ajouter après coup.
- Réunir dans `docs/chips/<type>/<Chip>.md` les références constructeur pertinentes et les fonctions réelles à couvrir : registres, bus, signaux, modes, reset, interruptions, délais, erreurs et variantes. Réutiliser les références déjà acquises ; consulter d'autres émulateurs pour comparer, sans prendre leur accord pour une preuve matérielle.
- Implémenter entièrement la puce dans son dossier `RetroMediaLab.Chips` en respectant les interfaces existantes. État interne, registres, compteurs, diviseurs, phases et échéances appartiennent au composant.
- Recevoir les fronts de la source d'horloge réellement câblée et conserver la cadence propre au composant. Aucun thread par puce, aucune cadence dérivée des images présentées par l'application. Ne pas inventer un oscillateur indépendant lorsque le circuit utilise une horloge externe ou partagée.
- Mémoriser les niveaux des entrées via les méthodes d'entrée existantes. Les prendre en compte au front, à la phase ou de manière asynchrone suivant le comportement réel du circuit ; ne pas imposer un retard d'un tick.
- Publier une sortie depuis l'opération qui la modifie, seulement lorsqu'un changement pertinent doit être communiqué. Les destinataires sont les composants effectivement raccordés par la carte.
- Garder le récepteur passif vis-à-vis de l'émetteur : il reçoit le signal et conserve son état, sans demander continuellement si l'autre a une réponse, est présent ou a changé.
- Conserver les activités internes nécessaires même sans demande extérieure : oscillateurs, compteurs, rafraîchissement, balayage, sérialisation et délais. Avancer directement à une échéance uniquement si aucun front ni effet observable intermédiaire ne doit être perdu.
- Reproduire uniquement les tampons physiques prévus : registre, FIFO, arbitrage, attente, acquittement et débordement. Une différence de fréquence ne justifie ni file logicielle illimitée ni conversion automatique par IRQ/NMI.
- Optimiser la structure avant les tests : enlever les sondages entre composants, recalculs sans changement, conversions et propagations identiques ; conserver les calculs et niveaux stables. Ne pas déplacer le même sondage dans un nouvel appel ni ajouter un agenda coûteux à chaque cycle.
- Conserver les opérations utiles : un accès mémoire ou une transaction de bus demandés par le matériel ne sont pas un sondage inutile. L'échantillonnage d'une entrée mémorisée au front prévu reste nécessaire.
- Exécuter les tests utiles du bloc terminé : transitions, phases, bus, interruptions, reset, cas limites et variantes. Réutiliser les tests fiables ; corriger ensemble les défauts constatés, puis vérifier le bloc corrigé. Ne pas relancer les tests des machines après chaque petite modification.
- Consigner les résultats réels et limites dans le document de la puce. Une performance ou une fidélité complète ne se déduit pas du seul schéma de code ; vérifier ce qui a été réalisé sans rechercher indéfiniment un gain marginal.
- Compléter `docs/chips/<type>/<Chip>.md` après chaque passage de tests : dater le passage, préciser la variante et les conditions, ce qui était correct, les défauts constatés, les fichiers et comportements modifiés, puis les résultats et timings mesurés avec leurs unités. Indiquer les comparaisons avant/après disponibles, les fonctions effectivement couvertes et les points restant ouverts. Ne pas inventer une mesure absente ni écraser l'historique des passages précédents.
- Produire et conserver les captures des résultats réels des puces dans `artifacts/TestsChips/<Chip>/<Machine>/`. Relier chaque capture au passage et au résultat qu'elle prouve dans la fiche de la puce ; ne pas remplacer ces preuves par les seules captures de machine.
- Consigner dans ce document chaque directive d'amélioration réutilisable retenue pendant le travail ; conserver les fichiers, références et résultats propres à la puce dans sa fiche. Les réutiliser directement sur la puce suivante lorsque les mêmes conditions matérielles s'appliquent, sans recommencer les recherches.
- Si une découverte révèle la même redondance dans une autre puce, même déjà traitée, ajouter ses fichiers à la liste de travail avant modification, appliquer la correction puis ses tests utiles et consigner les résultats dans sa fiche. Traiter ces corrections successivement, avec un commit par bloc de puce terminé ; ne pas reporter une amélioration établie au seul motif que cette puce a déjà été validée.
- Committer le bloc de puce terminé avant de passer à la suivante ; exclure captures, journaux et essais temporaires.
- Ne pas accumuler les fichiers de plusieurs blocs terminés : committer également un bloc complet et vérifié devenu volumineux, notamment une puce entière comportant de nombreux fichiers, sans attendre la fin de la machine.
- Lorsqu'un commit de sauvegarde du travail en cours est demandé, inclure aussi les travaux inachevés et indiquer explicitement `in-progress` dans le message ; ne pas les présenter comme terminés.
- Respecter `.gitignore` lors de chaque commit. Ne jamais forcer l'ajout d'un fichier ignoré ; exclure systématiquement `artifacts`, ses captures, archives et fichiers temporaires.

## Règles pour chaque machine

- Reprendre son suivi `docs/tasks/machines/<Machine>/` et identifier ses composants réels, variantes et périphériques. Ne pas ajouter de matériel inventé ni reprendre une puce déjà validée sans défaut établi.
- Terminer les puces manquantes en respectant les directives ci-dessus, successivement.
- Réaliser dans `Emulation.xxx` le câblage direct des puces : horloges, phases, lignes, bus, RAM partagée et arbitrage. Les communications entre puces ne transitent pas par le SDK `Emulation` ni par App.
- Raccorder les notifications une fois, initialiser leurs niveaux lors du branchement/reset, puis transmettre les changements. La coordination temporelle de la carte ne doit pas relire continuellement toutes les sorties des composants.
- Vérifier ensuite la machine assemblée sans fenêtre RML : ROM, exécution effective d'un logiciel, vidéo, son, entrées-sorties et médias disponibles. Respecter les équipements obligatoires et tester les équipements supplémentaires pertinents.
- Mesurer la machine complète en Debug : durée hôte en ms/image, images émulées par seconde, cadence vidéo matérielle en Hz, cycles émulés par seconde et pourcentage de vitesse réelle. Employer Hz/kHz/MHz selon la fréquence. Distinguer vitesse maximale sans limite et cadence limitée ; un dépassement en mode libre indique une marge de calcul.
- Mesurer sans tests ou compilations concurrents, avec scénarios identifiés et rendu vidéo/drainage audio inclus. Ne pas confondre FPS calculés hors interface avec FPS présentés par RML. Réserver les comparaisons Release aux validations utiles, sans les répéter continuellement.
- Corriger les problèmes établis, puis vérifier l'ensemble concerné. Ne pas modifier fréquences, cycles, signaux, options Debug ou précision pour obtenir artificiellement le résultat visé.
- Construire Debug avec tous les modules à la clôture du bloc machine ; effectuer ensuite l'essai en direct avec l'utilisateur. Ne pas ouvrir ou fermer RML pendant ses essais sans coordination.
- Ranger les captures de machine dans `artifacts/TestsMachines/<Marque>/<Machine>/`. Conserver plusieurs étapes datées et nommées par scénario pour montrer l'évolution des essais, sans écraser les preuves précédentes ; indiquer la configuration et le résultat observé dans la fiche de la machine.
- Continuer à ranger séparément les captures des tests de puces dans `artifacts/TestsChips/<Chip>/<Machine>/`. Garder les deux ensembles ignorés par Git et hors des commits. Capturer les résultats réellement exécutés ; ne pas présenter un rendu hors interface comme une capture de RML.
- Ne pas annoncer une machine complète après son seul démarrage.
- Supprimer les diagnostics temporaires et leurs artefacts après consignation ; libérer dans `finally` les ressources possédées, sans détruire les objets empruntés. Committer le bloc terminé.
- Committer chaque machine terminée après ses validations, sans regrouper plusieurs machines achevées dans un lot ultérieur.

## Sens des notifications

Une notification transporte un changement de signal ; ce n'est pas une instruction imposée au récepteur en dehors de sa phase matérielle. Le CPU continue ses cycles et prend en compte IRQ/NMI selon ses règles. Les circuits peuvent partager une source d'horloge tout en conservant leurs propres diviseurs et phases. Une entrée asynchrone agit selon sa spécification ; elle ne doit pas être artificiellement différée au prochain tick.

## Directives d'amélioration à réutiliser dès l'implémentation

### Sources, archives et informations acquises

- Centraliser les documents techniques récupérés, les informations sources téléchargées et les images extraites des PDF dans `artifacts/.tmp-archives/`. Ne pas les disperser dans des dossiers temporaires successifs.
- Classer les sources de puces sous `artifacts/.tmp-archives/chips/<Type>/<Fabricant>/<Famille>/<Puce>/`, avec la même hiérarchie que les composants de `RetroMediaLab.Chips`.
- Classer les sources de machines sous `artifacts/.tmp-archives/machines/<Marque>/<Machine>/`.
- Garder les extractions avec leur document source ; nommer les images par document et numéro de page pour retrouver immédiatement leur contexte. Conserver l'URL, la référence, la version ou révision, et les pages utiles dans la fiche thématique correspondante.
- Consigner les informations techniques effectivement trouvées dans `docs/chips/<Type>/<Puce>/`, avec un fichier Markdown par information ou groupe d'informations sur un même sujet : horloges, bus, registres, interruptions ou autre sujet pertinent. Conserver la fiche principale existante et y lier ces fichiers complémentaires ; ne pas créer de trames vides.
- Consigner les informations propres aux machines dans les dossiers de référence `docs/project/emulations/<marque>/<gamme>/<modèle>/` définis par `emulation-development.md`, et lier les sources archivées utiles.
- Écrire les détails nécessaires pour réutiliser directement l'information : comportement établi, valeurs et unités, phases et fronts concernés, conditions et variantes, pages ou sections sources, fichiers de code associés et incertitudes restantes. Distinguer une donnée constructeur d'une comparaison avec un émulateur ou d'une hypothèse.
- Après toute recherche apportant une information utile, compléter immédiatement le fichier thématique concerné ; créer un fichier complémentaire seulement si le sujet n'a pas encore de place. Ne pas recopier la même information dans plusieurs fiches.
- Consulter d'abord ces notes lors d'une reprise ou d'une modification ultérieure. Ne refaire une recherche que pour une information manquante, contradictoire ou à vérifier ; consigner ensuite son résultat pour éviter de répéter cette recherche.
- Garder les archives hors des commits. Conserver les documents sources encore utiles à la consultation et les notes Markdown persistantes ; supprimer les sorties de diagnostic jetables sans les confondre avec ces références.

### Application aux composants

Complément confirmé le 7 novembre 2026 : ces directives concernent les exigences et la méthode de travail. Ne pas les remplacer par une discussion sur l'incertitude des spécifications matérielles. Toute nouvelle amélioration réutilisable retenue doit être ajoutée ici pendant son traitement, avec ses conditions d'application, avant de passer à la puce suivante.

| Situation | Directive à appliquer | Ce qui doit rester conservé |
| --- | --- | --- |
| Le récepteur consulte continuellement un autre composant pour chercher un changement. | Raccorder les sorties aux entrées par notifications et conserver localement les niveaux reçus. | Fronts, phases d'échantillonnage et effets asynchrones réels. |
| Une méthode de publication relit toutes les sorties à chaque tick, même sans modification. | Publier depuis les opérations qui changent effectivement les sorties ; limiter la propagation aux signaux concernés. | Tous les changements observables, y compris les impulsions et leur ordre. |
| Des états, conversions ou résultats sont recalculés sans changement de leurs causes. | Conserver le résultat et l'invalider aux événements qui le modifient réellement. | Toutes les causes d'invalidation et les traitements temporels nécessaires. |
| Un composant reste sans activité observable jusqu'à une échéance déterminée. | Conserver ses compteurs et phases ; avancer jusqu'à cette échéance lorsque l'équivalence est établie. | Horloge matérielle, événements intermédiaires observables et ordre des accès. Aucun agenda par tick ajouté pour remplacer un appel direct. |
| Une puce déjà validée contient une consultation répétée inutile établie. | Corriger le code concerné ; son ancienne validation n'est pas une raison de conserver ce coût. | Fonctionnalités, signaux, fréquences et timings de la puce. Pas de réécriture systématique sans nécessité. |
| Une correction sur une puce révèle une redondance également présente dans d'autres puces. | Consigner immédiatement la directive, identifier les autres composants concernés, ajouter les actions aux suivis puis corriger et tester ces composants successivement, y compris ceux déjà traités. Réutiliser ensuite directement la directive sur les puces suivantes. | Confirmer la présence du même problème et les conditions d'application ; conserver les particularités matérielles de chaque puce et documenter ses propres résultats. |
| Une optimisation vient d'être réalisée. | Vérifier après modification le comportement et consigner le gain avec des conditions comparables et les valeurs de référence disponibles. | Ne pas présenter un gain supposé comme mesuré. Une régression impose d'examiner le remplacement ajouté et sa mesure avant de valider. |
| Le rendu vidéo contient des zones inchangées. | Réutiliser les pixels calculés et actualiser les zones affectées lorsque cela est avantageux ; transférer l'image entière si cette voie est plus efficace. | Balayage, lectures et signaux matériels ; changements de palette, mode, adresse, curseur et clignotement, même sans écriture RAM. |
| Une décision de mise à jour partielle ou complète est nécessaire. | Choisir pendant le développement une règle simple issue des mesures ; utiliser un seuil fixe seulement s'il est utile. | Aucun calcul permanent du coût qui annule l'économie recherchée. |
| Une référence, un raccordement ou une explication a déjà été identifié. | Consigner l'information utile et les chemins concernés dans la fiche de la puce ou machine, puis s'appuyer dessus. | Rouvrir le code concerné pour le modifier ou résoudre un point inconnu ; éviter les lectures et recherches répétitives sans nouveau besoin. |

Pour chaque nouvelle directive, écrire le problème évité, l'action à appliquer, ses conditions et les invariants matériels à préserver. Indiquer dans la fiche de la puce les résultats constatés et la portée réellement vérifiée. Une amélioration spécifique à un circuit ne doit pas être imposée aux autres circuits sans que ses conditions s'appliquent.
