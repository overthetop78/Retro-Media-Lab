# Bruitages mécaniques : chemin d’exécution et synchronisation

## Vérification du10 novembre2026

Le lecteur émulé possède sa rotation, ses positions et ses signaux matériels. Les appels `SetMotor`, `SetActivity` et `Seek` transmettent leurs conséquences à la mécanique commune. Dans System5, `SystemFloppyDrive.AdvanceTo` produit les flux et index à leurs dates ; `UpdateMechanics` transmet les commandes lors des changements de lignes, sans générer de PCM à chaque clock.

`EmulatorCore.Disks.cs` crée les mécaniques avec `hostAudio:true`, les configure dans `EmulatorCore.FloppySound.cs` et les fournit aux images. Le pont `EmulationMediaProcessingService.Drives.cs` relaie les appels vers `MediaDriveMechanics`. `MediaRuntimePreparationService` transmet `AudioAvailable` au contrôleur commun de l’application.

`src/RetroMediaLab.App/Services/Audio/MediaDriveAudioController.cs` démarre un worker par `Task.Run(PumpAsync)` ; ce worker appelle `RenderAudio`, mélange puis remet les blocs à la sortie WASAPI. `MediaDriveAudioConstants` fixe44100 Hz mono et882 échantillons par bloc20 ms. Le déclenchement du worker ne fait pas attendre sa tâche au cœur. Les ressources de sortie sont fermées dans finally et le propriétaire attend le worker à sa destruction.

## Limites établies dans le code commun

- `MediaDriveMechanics.Audio.cs` tient `_sync` pendant tout le rendu/mixage. Les commandes `SetMotor`, `SetActivity`, `Seek` et les lectures d’état de `MediaDriveMechanics.cs` utilisent ce même verrou. Un appel du cœur peut donc attendre un rendu en cours ; l’existence d’un worker séparé ne garantit pas l’absence de blocage. Aucun temps de contention réelle n’est encore attribué à ce verrou par cette revue.
- `MediaDriveSoundEvent` contient opération, distance, direction et cylindre, sans horodatage. Le contrat `IEmulationDriveMechanics` transmet les commandes sans temps émulé. Le worker suit le tampon audio ou `Stopwatch` hôte ; cela n’établit pas une synchronisation exacte aux événements de la machine émulée. L’ordre de file et les offsets PCM ne remplacent pas une date émulée.
- Le passage par `NotifyAudio` et l’activation utilise aussi des verrous courts ; leur coût n’est pas démontré comme goulet. Ne pas déduire un temps d’attente depuis leur seule présence.

Ces constats concernent les mécaniques communes et leurs consommateurs, pas seulement System5. Cette vérification n’a pas modifié App, le SDK ou MediaEngine.

## Périmètre des mesures System5

Les relevés natifs `AcornSystemMachine.RunFrame`/`DrainAudio` n’appellent pas `IEmulationDriveMechanics.RenderAudio` et n’instancient pas le contrôleur audio App. `DrainAudio` copie les échantillons du matériel/moniteur bande, distincts des bruitages. Les18,55675 ms/champ attribués à la carte disquette par le profil global étaient donc mesurés sans synthèse ni mixage des sons mécaniques. Ils ne peuvent pas être imputés à ce rendu.

Un comparatif contrôlé ajoute explicitement le rendu commun du lecteur, puis le supprime entièrement, sans baisser le volume ni arrêter la simulation mécanique. Résultats à consigner après exécution ; aucun coût audio chiffré déduit de cette seule inspection.

## Comparatif rendu mécanique activé/désactivé —10 novembre2026
Trois paires neuves de machines System5 NOS80/VDU80 ; SSD System BASIC en lecture seule, CAT512/BASIC4096 puis PRINT12+34=46 par clavier.40 champs de chauffe, dix champs de stabilisation et un champ séparé, puis120 champs alternés par paire. Dans un mode le profil BBC déclaré par le module, volume50 %, rend et mélange un bloc44100 Hz/882 échantillons par champ ; dans l’autre `Configure(..., enabled:false)` désactive réellement les événements sonores et le banc ne fait aucun appel de rendu/mixage. Ce n’est pas un volume nul.
Le rendu est ajouté séquentiellement au banc pour en mesurer le coût ; aucun worker App ni périphérique WASAPI n’est créé. Ce protocole n’est donc pas une mesure de la contention ou des FPS présentés par RML. Un bloc correspond à20 ms audio contre19,968 ms émulées par champ, soit0,16 % de durée audio supplémentaire, explicitement conservée pour reproduire le format commun. Le banc force aussi le rendu des blocs activés après la fin de la résonance, alors que le worker peut ensuite se désactiver ; ne pas transformer cette charge contrôlée en coût exact de RML.
CPU/registres/cycles, RAM, vidéo, son matériel/moniteur, moteur et comptes de transferts correspondent à chaque champ. Les états physiques des deux lecteurs sont comparés séparément : piste, tête, rotation, temps, sélection, moteur, écriture et position dans les pulses.4608 octets lus, zéro écrit et empreinte du SSD inchangée après libération des cœurs/images. Le mode sans calcul ne produit aucun tampon mécanique ; le PCM du mode actif est non nul sur les144 champs moteur actif, ce qui prouve que le son a bien été calculé. Contrôle natif réussi en1m14 s.
| Group | Fields/mode | No generation ms/field | With generation ms/field | Extra time % | No generation nominal % |
|---|---:|---:|---:|---:|---:|
|Pair 1|120|29.80109|30.12757|1.096|67.004|
|Pair 2|120|28.53683|28.62461|0.308|69.973|
|Pair 3|120|31.64935|32.82253|3.707|63.091|
|All|360|29.99575|30.52491|1.764|66.569|
|Motor True/True|144|40.90523|41.92398|2.491|48.815|
|Motor True/False|3|29.20100|28.74223|-1.571|68.381|
|Motor False/False|213|22.63153|22.84359|0.937|88.231|

|Motor|With core ms|Without core ms|Render+mix ms|Skipped render ms|Fields with nonzero PCM|
|---|---:|---:|---:|---:|---:|
|Motor True/True|41.78396|40.90443|0.13915|0.00000|144|
|Motor True/False|28.60437|29.20020|0.13660|0.00000|3|
|Motor False/False|22.69289|22.63075|0.14990|0.00000|153|

Moteur actif : rendu/mixage0,13915 ms/bloc contre0 sans appel. Le total passe40,90523→41,92398 ms/champ ; le cœur seul40,90443→41,78396. La différence de total1,01875 ms ne doit donc pas être présentée comme le coût propre du rendu ;0,87953 ms se trouvent dans la durée du cœur et ne sont pas isolés causalement par ce comparatif. La moyenne des trois paires passe29,99575→30,52491 ms/champ, avec variabilité entre paires. Aucun temps d’attente sur un worker concurrent n’a été mesuré.
Le rendu mécanique ne peut pas expliquer les18,55675 ms de carte du profil natif précédent, où il n’était pas appelé. Son coût direct mesuré ici est faible face à la simulation physique. Les possibilités d’attente du verrou commun et l’absence de date émulée restent les constats distincts de l’audit, sans leur attribuer les18 ms. Ne pas poursuivre cette piste de synthèse pour optimiser le flux/séparateur de System5.
Preuves : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-mechanical-audio-native.{json,md,trx}`. Banc temporaire supprimé, aucune modification de production App/SDK/MediaEngine ni de la machine pour ce test. Les scripts/sources de sondes des précédents relevés sont également retirés ; les résultats documentaires et contrôles physiques autonomes restent à vérifier sur les assemblages reconstruits.

Après suppression du banc : module Acorn reconstruit0 erreur/0 avertissement,32 contrôles93 et63 de carte réussis. Aucun code de production modifié, aucune ressource native de sortie audio créée par le comparatif, aucun processus de test possédé restant.
