# Organisation et entretien de la documentation

La documentation distingue le fonctionnement actuel, les procédures, les références et le code
restant à réaliser. Les décisions et les preuves utiles restent disponibles après la suppression
ou la clôture d’un suivi.

## Responsabilité des dossiers

| Dossier | Contenu |
|---|---|
| `architecture` | Structure du code, contrats et échanges entre composants. |
| `project` | Décisions confirmées et procédures de développement, test, publication et entretien. |
| `directives` | Directives de réalisation et consignes permanentes, avec leurs périmètres respectifs. |
| `reference` | Catalogues techniques : machines, moteurs, formats, filtres et périphériques. |
| `chips` | Fiches matérielles, sources, limites et résultats des composants. |
| `machines` | Références transversales des machines et équipements. |
| `project/emulations` | Décisions, recherches et résultats propres aux familles et machines. |
| `ui` | Comportement visible de l’application et organisation des fenêtres. |
| `design` | Conceptions d’interface et de thèmes. |
| `research` | Études techniques. |
| `future` | Évolutions différées. |
| `tasks` | Actions de code encore ouvertes, avec les informations nécessaires à leur reprise. |

## Procédures et tâches

La [méthode de réalisation des modules](emulation-module-development.md) est une procédure
réutilisable, distincte du [guide public de création d’un module](../architecture/emulation-module-authoring.md).
Le [développement de l’application](development.md), les [scripts](scripts.md), les
[tests](testing.md) et la [publication](release.md) ont leurs propres documents permanents.
Le [catalogue des moteurs](../reference/emulation-core-catalog.md) est une référence, pas une tâche.

Une procédure récurrente ne devient pas une publication ou une implémentation autorisée du seul
fait qu’elle est écrite. Les règles de publication restent dans leur procédure ; les prochaines
publications ne sont pas entretenues sous forme de copies de cette procédure dans `tasks`.

Les suivis entièrement terminés et les plans annulés sont supprimés. Les cases réalisées sont
retirées des feuilles encore ouvertes. Les limites, décisions et résultats utiles sont conservés
dans les documents de leurs propriétaires ; retirer un suivi ne prouve pas la conformité du code.
Le dossier `tasks/chips` est conservé pour les prochains composants, même vide.

Les tâches ne reçoivent pas de cases pour la seule tenue obligatoire de leur documentation.
Un index de tâches n’est pas recréé ; les dossiers regroupent directement les suivis restants.

## Vérification du rangement

La revue du 13 novembre 2026 porte sur les 227 fichiers Markdown présents avant le nettoyage :
classement des procédures et références, cases des suivis, liens Markdown locaux et dossiers vides.
Elle ne constitue pas une validation fonctionnelle du logiciel, des puces ou des émulateurs,
ni une nouvelle vérification des affirmations techniques et des sources Internet archivées.

Le script `scripts/tools/find-invalid-documentation.ps1` conserve un contrôle historique de
l’accessibilité de tous les documents depuis un index. Ce critère ne correspond plus au choix
de ne pas entretenir d’index de tâches ; son résultat global ne doit pas être présenté comme
la validation de ce rangement. Les liens locaux sont contrôlés séparément.

## Résultat du rangement du 13 novembre 2026

- Quatre documents déplacés : procédure des modules dans `project`, catalogue des moteurs dans
  `reference`, prompts conservés dans `directives`, revue des puces Atom avec les résultats Atom.
- Contraintes permanentes de reprise extraites du suivi d’uniformisation vers la procédure des modules.
- Dix-sept suivis supprimés : douze feuilles de puces entièrement cochées, deux anciens plans
  SG-1000/EmuChip annulés, deux doublons de suivi Acorn et la copie de la procédure de publication.
- Anciennes cases cochées retirées des suivis restants, avec conservation de toutes leurs actions
  ouvertes ; les groupes cochés contenant encore des actions ouvertes sont remis à l’état ouvert.
- Quatorze dossiers vides supprimés ; `tasks/chips` conservé.
- Deux cent dix documents Markdown présents après rangement, dont vingt-sept suivis encore ouverts.
  Aucun lien Markdown local cassé dans les liens ordinaires ou les définitions de références contrôlés,
  hors exemples de code. Aucun suivi restant entièrement coché.

Les recherches Sega et les passages EmuChip conservés portent une mention historique explicite :
le code supprimé n’est pas présenté comme une implémentation actuellement disponible.
Aucun test logiciel, build ou commit n’est réalisé pour ce rangement documentaire.

## Références locales sous artifacts

Les documents constructeur, schémas, extractions exploitables et sources de référence sont rangés
sous `artifacts/References/Chips`, `Machines`, `Formats` et `Audio`. Les références Winchester sont
sous `References/Machines/Acorn/Acorn Winchester`. Le résultat DOS Plus cité par la fiche 80186 est
sous `References/Measurements/Acorn/Tube` et les trois captures sont dans
`artifacts/TestsMachines/Acorn/BBC Master 512/Tube`.

Le rangement des cinq anciens dossiers temporaires a déplacé 1 268 fichiers, supprimé
137 sorties de compilation ou fichiers vides et retiré 360 dossiers vides. La sauvegarde de
fusion Git a ensuite été supprimée à la demande de l’utilisateur avec ses neuf fichiers et
27 dossiers vides, y compris les parents devenus vides.

Après confirmation explicite de l’utilisateur, les 24 fichiers temporaires de diagnostic, le script
clavier et les médias d’essai regroupés sous `artifacts/Archives/Diagnostics` et
`artifacts/Archives/TestMedia` ont été supprimés, ainsi que leurs dossiers vides. Les cinq anciens
dossiers `.tmp*` sont retirés.
