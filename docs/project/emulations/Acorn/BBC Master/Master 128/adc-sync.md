# Master 128 — ancrage temporel du µPD7002

## Défaut établi le 7 novembre 2026

Le raccordement BBC standard appelle `AdvanceTo(_ticks)` avant lecture/écriture des registres ADC. Le décodage Master à FE18–FE1F appelait seulement Read/Write. Or le convertisseur conserve son dernier temps traité et ne reçoit pas un Tick permanent : une écriture après une période sans accès pouvait calculer sa prochaine échéance depuis un ancien instant. Le même problème concernait le réarmement d'une conversion en cours. Une lecture des seules valeurs ADVAL après de nombreuses entrées clavier ne suffisait pas à prouver l'instant de départ.

Huit tests autonomes nouveaux reproduisent l'écart sur la source précédente : huit échecs, zéro réussite, 280 ms. Les cas couvrent conversion courte/longue, repos de 0/37/25001 ticks, miroir FE1C et réarmement après 2713 ticks. Ils vérifient BUSY jusqu'au dernier tick, EOC/IRQ au suivant et acquittement VIA. Les durées attendues restent 8000/20000 cycles de référence 2 MHz (4/10 ms), sans déplacement de l'événement.

## Correction et vérification

`BbcMicroMachine.Master.cs` avance le convertisseur à l'instant courant avant chaque accès à sa fenêtre, comme dans le raccordement BBC. Aucun compteur ou oscillateur n'est déplacé dans la carte, aucun sondage permanent ajouté et aucun code du composant Chips modifié. L'échéance et les notifications EOC restent possédées par le µPD7002 ; le VIA conserve son entrée mémorisée et son traitement de front.

48 tests Debug regroupés réussissent après correction, zéro échec ni ignoré, 794 ms : huit régressions Master ADC, trente cas Master et dix cas BBC B. Les tests sont autonomes, sans ROM externe ni fichier, avec entrées et rappels nettoyés dans finally. Aucun nouveau gain de performance n'est déduit de cette correction fonctionnelle ; les budgets de calcul précédemment ouverts restent inchangés.

Le MOS 3.50/BASIC IV réel reçoit `*FX16,4`. Deux contrôleurs présentent quatre extrémités analogiques opposées, puis inversées. Les programmes BASIC comparent ADVAL(1..4), extrémité haute au moins 65000 et basse zéro : **MASTER ADC PASS**, puis **MASTER ADC SWAP PASS**. Les états des contrôleurs restent conservés pendant l'injection clavier. Cette preuve confirme les valeurs et le câblage ; les tests autonomes ci-dessus prouvent séparément l'instant de la conversion. Les limites analogiques déjà documentées du µPD7002 ne changent pas.

Captures réelles inspectées : `artifacts/TestsChips/Upd7002/BBC Master 128/20261107-095756-master128-master-adc-pass.bmp` et `20261107-095808-master128-master-adc-swap-pass.bmp`, également dans `artifacts/TestsMachines/Acorn/BBC Master 128/`. Aucun média, firmware ou réglage utilisateur écrit, aucune sortie Windows ouverte. Diagnostic temporaire supprimé, banc par défaut reconstruit avec zéro erreur (avertissement NU1900 du flux de vulnérabilités indisponible).

L'examen des accès ADC d'AcornEmu ne trouve que les chemins BBC et Master : BBC possède déjà la synchronisation. Pas de modification d'une autre machine sans occurrence du même défaut. La correction du chemin partagé bénéficie aux assemblages Master qui l'utilisent, sans déclarer ici une validation physique de toutes leurs variantes ; le Compact sans ADC n'est pas présenté comme testé.

Livraison : `scripts/local-building.cmd --building=debug --modules=A` terminé avec code 0. `build/Debug/Retro Media Lab/retromedialab.exe` présent ; 24 manifestes et leurs DLL présents. Acorn livré identique par SHA-256 à `bin/Debug/net10.0/win-x64`, Chips dans lib identique à sa compilation. Aucun RML ouvert/fermé pour ce build, absence d'instance vérifiée avant lancement. Ce bloc corrige la synchronisation ADC ; il ne clôture pas la cadence globale du Master ni toutes ses variantes.
