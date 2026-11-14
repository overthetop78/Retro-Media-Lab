# GenesisPlusGXCS — premier contrôle Mega Drive

Campagne locale du 14 novembre 2026, **Debug exclusivement**. Les six ROM terminent 1 200 trames sans exception ; les SHA-256 avant/après confirment qu'elles sont intactes. Aucun changement du cœur d'émulation.

## Protocole

Même méthode que le contrôle Master System : appel direct de `SystemCore`, vidéo et audio stéréo 44 100 Hz actifs, sans saut de trame ni limitation de vitesse, sans BIOS, détection automatique de région et de manette. 60 trames de chauffe puis 1 200 trames mesurées par jeu (environ 20 secondes NTSC ou 24 secondes PAL). Start aux trames mesurées 180–183 puis 480–483 ; droite de 540 à 839 avec B pendant les quatre premières trames de chaque période de 60. Captures aux trames mesurées 120, 300, 600 et 1 200 ; ces numéros excluent la chauffe.

Le temps moteur et les allocations couvrent exclusivement `RunFrame`. Les empreintes vidéo, l'analyse d'audio et l'encodage PNG sont exclus du temps moteur. Le temps CPU couvre le processus avec les observations. Le test ne mesure pas la présentation dans l'application, la sortie audio sur le périphérique ou le routage clavier/manette de l'application : les entrées sont injectées dans le cœur.

## Vitesse et fonctionnement observé

| ROM | Région | FPS moteur | Vitesse nominale | Temps moteur / 1 200 trames | Observation |
|---|---|---:|---:|---:|---|
| Sonic The Hedgehog (USA, Europe) | NTSC | 31,40 | 52,41 % | 38,212 s | Logo Sega puis annonce Green Hill Zone Act 1 à la dernière capture ; jeu effectif non vérifié |
| Golden Axe (World) | NTSC | 34,62 | 57,77 % | 34,665 s | Écran Select Player à la dernière capture |
| Streets of Rage 2 (USA) | NTSC | 34,12 | 56,94 % | 35,169 s | Introduction puis titre à 600 trames ; image noire à 1 200 trames, persistance non vérifiée |
| Gunstar Heroes (USA) | NTSC | 35,54 | 59,30 % | 33,769 s | Scène de jeu visible avec personnage, tirs et interface |
| Thunder Force III (Japan, USA) | NTSC | 29,46 | 49,16 % | 40,734 s | Scène de jeu visible avec décor, projectiles et interface |
| Aladdin (Europe) | PAL | 28,37 | 57,08 % | 42,297 s | Menu puis texte d'introduction à Agrabah ; jeu effectif non vérifié |

La référence est la fréquence nominale propre au cœur : environ 59,92 trames/s NTSC et 49,70 trames/s PAL. **Aucun des six scénarios n'atteint le temps réel en Debug** : 49 à 59 % de la vitesse nominale. Aucune mesure Release n'a été effectuée, et son gain ne peut pas être déduit de ces chiffres.

Les captures montrent un démarrage et, pour deux jeux au moins, une scène de jeu. L'absence d'exception ne prouve pas la compatibilité complète. L'écran noir de Streets of Rage 2 n'a pas fait l'objet d'un prolongement ni d'un diagnostic ; il ne permet à lui seul de conclure ni à une panne ni à une transition normale.

## CPU, allocations, mémoire et audio

Mo = 1 000 000 octets. Les allocations sont cumulées, distinctes de la mémoire conservée. La mémoire managée est observée sans collecte forcée. Le pic résident est échantillonné toutes les 30 trames dans un même processus accueillant successivement les six jeux : ce n'est pas une mesure isolée de chaque ROM.

| ROM | Temps CPU processus | Mo alloués / trame | Mémoire managée avant → après | Pic résident observé | Trames avec audio non nul / 1 200 |
|---|---:|---:|---:|---:|---:|
| Sonic | 38,438 s | 2,861 | 89,13 → 76,90 Mo | 183,85 Mo | 992 |
| Golden Axe | 35,031 s | 2,472 | 106,18 → 112,62 Mo | 199,75 Mo | 976 |
| Streets of Rage 2 | 35,422 s | 2,595 | 132,87 → 84,81 Mo | 211,44 Mo | 413 |
| Gunstar Heroes | 33,969 s | 2,341 | 104,43 → 103,07 Mo | 219,06 Mo | 637 |
| Thunder Force III | 40,906 s | 2,723 | 132,20 → 137,78 Mo | 223,16 Mo | 1 006 |
| Aladdin | 42,422 s | 2,823 | 103,49 → 112,22 Mo | 223,70 Mo | 905 |

Les allocations restent élevées : 2,34 à 2,86 Mo par trame, soit 2,81 à 3,43 Go cumulés sur chaque scénario. Le test ne localise pas leur origine et ne constitue pas une mesure de fuite à long terme. Tous les jeux produisent des échantillons audio non nuls ; leur fidélité et leur écoute sur un périphérique ne sont pas validées.

Le JSON fournit aussi le débit temporel 68000 équivalent calculé depuis les cycles maîtres par trame, divisés par 7. Il ne s'agit pas d'un comptage indépendant des instructions exécutées ni d'un profil par composant.

## Captures et données locales

24 captures PNG brutes du framebuffer, sans redimensionnement, conservées avec les mesures dans le dossier d'artefacts local ignoré par Git.

- [Sonic, annonce Green Hill](../../../../artifacts/GenesisPlusGXCS/mega-drive/Sonic%20The%20Hedgehog%20%28USA%2C%20Europe%29-frame-1200.png)
- [Golden Axe, sélection du joueur](../../../../artifacts/GenesisPlusGXCS/mega-drive/Golden%20Axe%20%28World%29-frame-1200.png)
- [Streets of Rage 2, titre](../../../../artifacts/GenesisPlusGXCS/mega-drive/Streets%20of%20Rage%202%20%28USA%29-frame-600.png)
- [Gunstar Heroes, scène de jeu](../../../../artifacts/GenesisPlusGXCS/mega-drive/Gunstar%20Heroes%20%28USA%29-frame-1200.png)
- [Thunder Force III, scène de jeu](../../../../artifacts/GenesisPlusGXCS/mega-drive/Thunder%20Force%20III%20%28Japan%2C%20USA%29-frame-1200.png)
- [Aladdin, capture finale](../../../../artifacts/GenesisPlusGXCS/mega-drive/Aladdin%20%28Europe%29-frame-1200.png)
- [Mesures détaillées, empreintes des ROM et liste des captures](../../../../artifacts/GenesisPlusGXCS/mega-drive/results.json)
- [Journal xUnit : campagne réussie en 3 min 56 s](../../../../artifacts/GenesisPlusGXCS/mega-drive/test.log)

Le fichier de campagne et ses assemblages temporaires sont supprimés après exploitation. Les ROM, le cœur et les autres tests restent inchangés.
