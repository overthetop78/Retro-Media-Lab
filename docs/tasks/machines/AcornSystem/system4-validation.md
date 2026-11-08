# Acorn System 4 — deux lecteurs

- [x] Vérifier le montage System 4 avec les composants System 2/3.
  - [x] Vérifier les transferts par le montage existant.
    - [x] Modifier `artifacts/.tmp-system3/Program.cs`, option --system4 : fabrique SystemFunctions, vrai DOS40, catalogue, BASIC chargé/exécuté, SAVE/LOAD de huit octets, déplacement du média vers le second port et CAT1 ; libération des deux montages en finally.
    - [x] Créer les captures natives et journaux `artifacts/TestsMachines/Acorn/Acorn System 4/20261108-system4-*` ; mesure globale sans RML, puis vérification du second port.

Le code System 4 ne nécessite pas de nouvelle correction propre après les corrections communes. Mesure sans compilation concurrente : 15,520111 ms/champ. Le passage deux lecteurs a chevauché la compilation des tests (23,785812 ms/champ) : ce coût concurrent n'est pas une régression attribuée au montage. Voir la fiche System 4. Le banc commun sera supprimé au terme de System 5 ; livraison groupée suivie dans son fichier.
