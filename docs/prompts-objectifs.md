# Prompts objectifs

Tu dois toujours finir ce que tu fais, tu dois toujours etre propre, mais tu dois développer vite aussi, tu ne dois pas passer des milliers de tests pour rien, tu avances dans le code, tu testes a la fin, tu corriges les différents problèmes d'un coup au lieu de faire petit a petit.

ensuite, tu fais tout ceux qui sont pas fait la dedans, par bloc
[https://docs.google.com/document/d/1YNQZeVmV0fKFfnvm1RYX4aJaRAqJEn-_mswaLMgl9_E/edit?usp=sharing](https://docs.google.com/document/d/1YNQZeVmV0fKFfnvm1RYX4aJaRAqJEn-_mswaLMgl9_E/edit?usp=sharing)

tu comence un bloc
tu ajoutes les firmwares manquant dans Firmware
et quand c'est fini, tu build debug avec les modules
tu commits
tu fais le bloc suivant

tu fais :
Acorn System, pour le trois, tu fais qu'une fois mais avec le choix du CPU
Acorn Atom, Acorn Electron
Ne fait pas les ABC (on les fera p-e un jour mais pour l'instant non)
Communicator non plus, tu ne fais pas pour le moment
Archimedes : tu les fais. si le matériel est quasi identique ou la machjine quasi identique (exemple : A440 et A440/1, si c'est quasi pareil, tu fais le changement dans CPU, ROM ou suivant ce qui change) si c'est vraiment une autre machine, tu sépares )
voir si le A5000 et A5000 Alpha sont quasi pareil ou pas. (si vraiment différent, alors ce sont deux machines sinon c'est une seule machine et dedans on ajoute ce qu'il faut pour le transformer en A5000 Alpha
Les stations Unix, tu ne fais pas pour le moment
les RISC PC ET A7000, tu les fait, regarde si le A7000 et A7000+ est vraiment différent pour en faire deux machines.
Les pocket Book, tu fais pas pour l'instant

essaie de faire proprement, en faisant comme le code déjà fait, pour éviter de faire dégueulasse, tu peux réarranger le code au besoin, pas de patches vite fait pour "que ca marche", si tu dois refaire propre, refais propre. retierns bien ce que j'avais déjà dis, pour le code propre.
Sers toi de toutes les sources que tu peux trouver pour t'aider, il faut faire un rendu fonctionneel, fiable, identique a la machine d'orgine, et l'emulation doit etre très rapide,

## Traitement complet de toutes les puces

Reprends toutes les puces depuis le début, en commençant par celles des machines Acorn depuis les BBC Micro. Toutes signifie toutes : pas seulement les CPU, pas seulement certaines puces vérifiées rapidement. Cela comprend les processeurs, contrôleurs, circuits vidéo et audio, interfaces d’entrée-sortie, circuits mémoire, réseau, horloges et autres circuits émulés, y compris ceux dont le fonctionnement est encore intégré directement aux fichiers des machines.

### Réalisation complète et fidèle

Tu dois traiter chaque puce entièrement et reproduire exactement le fonctionnement de l’originale, sans oublier de fonctionnalités, de signaux ou de cas limites. Ne fais pas seulement le minimum nécessaire pour démarrer une machine ou faire fonctionner un logiciel.

Vérifie l’implémentation complète : instructions lorsqu’il s’agit d’un CPU, registres, modes, accès au bus, séquences, interruptions, réinitialisation, signaux, attentes, timings et particularités des variantes concernées. Corrige ou recode proprement tout ce qui manque ou ne correspond pas au matériel. Ne te contente pas d’ajouter quelques fonctions puis de passer à une autre puce.

Utilise les documentations des constructeurs et les sources disponibles des autres émulateurs pour vérifier le fonctionnement : MAME, 86Box, b2, B-Em, JSBeeb et les autres projets pertinents. Ne te limite pas à une seule source ou à MAME. Vérifie ce qui est réellement implémenté et corrige les écarts ; ne déduis pas qu’une puce est complète parce que la machine démarre.

Fais un travail propre, complet et structuré, conforme à l’organisation du projet. Pas de rustine rapide pour seulement « que ça marche ». Si une partie doit être refaite pour être correcte, refais-la proprement. Ne mets pas de textes ou de valeurs en brut lorsqu’ils doivent être définis dans les constantes ou ressources existantes ; une déclaration par constante.

### Une puce à la fois

Tant qu’une puce n’est pas totalement terminée, tu ne passes pas à une autre. Termine son implémentation, ses vérifications, ses preuves et son document avant de commencer la suivante.

Quand une puce est finie, committe son travail séparément avant de corriger une autre puce. N’accumule pas les modifications de plusieurs puces sans les terminer et les committer.

Respecte aussi les arrêts de discussion demandés : si je demande qu’on parle après la puce en cours et avant de continuer les autres, termine cette puce, fais son commit, puis attends cette discussion avant de poursuivre.

### Document propre à chaque puce

Crée un document pour chaque puce, rangé par type de matériel :

```text
docs/chips/<type de matériel>/<document de la puce>.md
```

Par exemple, le document du MOS 6502 se trouve dans `docs/chips/CPU/Mos6502.md`.

Ne classe pas ces documents sous une marque comme `docs/chips/acorn/` : Acorn est une marque et un ensemble de machines, pas une machine ni un type de matériel.

Le document doit décrire ce qui est réellement implémenté pour la puce, les fonctions qui restent manquantes, la liste précise des fonctionnalités à tester et les résultats obtenus. Ce n’est pas un fichier de tâches. Ne crée pas en série des fiches génériques ou des trames vides qui donnent l’apparence d’un travail effectué.

### Tests complets et preuves en émulation

Tu dois tester toutes les fonctionnalités de la puce, pas seulement faire quelques vérifications rapides ou constater qu’un démarrage fonctionne. Pour un CPU, cela comprend toutes ses fonctionnalités, pas uniquement les instructions courantes.

Prépare les vérifications adaptées à la puce entière, exécute-les et corrige les problèmes constatés. Regroupe les corrections et les validations utiles ; ne relance pas des milliers de tests répétitifs sans raison à chaque petite modification.

Tu dois aussi prouver le fonctionnement en émulation, avec des captures d’écran des résultats effectivement obtenus. Range-les selon cette structure :

```text
artifacts/TestsChips/<Chip>/<Machine>/
```

Utilise le nom de la puce puis celui de la machine réelle concernée par le test. Ne remplace pas le nom de la machine par la marque Acorn. Si tu as besoin d’images de médias pour tester, il y en a dans `F:\Retro`.

Les captures doivent montrer les résultats de tests exécutés dans l’émulation. Une image de démarrage ne remplace pas les tests de toutes les fonctionnalités. Indique précisément ce que chaque vérification et chaque capture démontrent ; n’annonce pas comme testé ce qui ne l’a pas été.

Communique les résultats au fur et à mesure des passages de tests, sans attendre d’avoir tout terminé. Consigne les résultats concernant la puce et ses performances dans son document. Pour le MOS 6502, utilise `docs/chips/CPU/Mos6502.md` ; n’y ajoute pas des erreurs de l’application sans rapport avec cette puce ou son framerate.

### Optimisation sans perte de fidélité

Toutes les puces doivent être à la fois fidèles à l’original et optimisées au maximum. La vitesse réelle de la machine émulée ne justifie pas un code hôte inutilement lent. Hors limitation à la vitesse réelle, l’émulateur doit exploiter efficacement le processeur hôte.

Mesure les performances, identifie les traitements coûteux et optimise réellement le code sans retirer de comportements matériels, de cycles, de signaux ou de vérifications nécessaires à la fidélité. Ne masque pas un problème en changeant simplement les options de compilation Debug pour faire croire qu’il est corrigé.

Garde cette logique pour toutes les puces et pour les futurs systèmes, notamment Nintendo 64, PlayStation, PlayStation 2, Dreamcast et PlayStation 3. L’organisation et l’optimisation doivent permettre de faire évoluer ensuite les techniques d’exécution, notamment vers la recompilation à la volée, tout en conservant le comportement strict des puces. Ce point n’est pas une demande d’implémenter ces machines ou cette recompilation maintenant.

# PROMPTS OBJECTIFS Refonte de l'orchestration matérielle pour optimiser la vitesse de l'émulation

## RetroMediaLab — Refonte de l'orchestration matérielle

**Arrête les micro-optimisations isolées. Je veux maintenant que tu implémentes réellement l'architecture événementielle des puces, pas que tu rédiges encore une proposition.**

### 1. Fonctionnement des composants

Chaque puce doit fonctionner comme son équivalent électronique réel :

- Elle possède son état interne, ses registres, ses compteurs et ses horloges.
- Elle fonctionne à la fréquence matérielle prévue, indépendamment des autres composants.
- Elle reçoit les signaux sur ses entrées et conserve leurs états.
- Elle traite ces entrées au front d'horloge ou à la phase prévue par le matériel.
- Lorsqu'une sortie change, elle notifie uniquement les composants physiquement raccordés.
- Ses timers et échéances internes continuent de fonctionner sans sollicitation extérieure.

**Aucune puce ne doit interroger continuellement les autres pour savoir si elles ont quelque chose à transmettre.**

### 2. Architecture événementielle

Implémente les mécanismes communs nécessaires dans `RetroMediaLab.Chips`, directement utilisables par les modules de machines comme `RetroMediaLab.Emulation.Acorn` :

- Un ordonnanceur temporel déterministe pour les horloges, leurs phases et les échéances matérielles.
- Un mécanisme de signaux reliant les sorties des composants aux entrées réellement raccordées.
- Une propagation des changements sans sondage permanent.
- Un traitement des événements à leur instant matériel exact, en respectant leur ordre.
- Une conservation des états entre deux changements, sans recalcul inutile.
- Une gestion efficace des périodes d'inactivité, en avançant directement jusqu'au prochain événement lorsque le matériel le permet.

Les modules de machines utilisent directement les puces de `RetroMediaLab.Chips` et assurent leur câblage matériel. **Ne fais pas transiter leurs communications par `RetroMediaLab.Emulation`.**

Il ne faut pas créer un thread ou une tâche asynchrone par puce. Il s'agit d'une simulation déterministe des circuits électroniques.

### 3. CPU, vidéo et périphériques

Le CPU conserve son horloge et exécute normalement ses instructions. Il observe les lignes IRQ/NMI aux phases prévues.

Le circuit vidéo fonctionne selon ses propres horloges et continue de produire ses signaux indépendamment du CPU. L'affichage graphique de l'application doit être découplé du fonctionnement des circuits vidéo émulés.

Les VIA, ACIA, contrôleurs de disquettes, SCSI, réseau, son et autres périphériques fonctionnent selon leurs horloges et événements internes, en émettant des signaux uniquement lorsqu'un changement doit être communiqué.

Si une carte fonctionne à 5 MHz et le CPU à 2 MHz, chacun avance à sa cadence. Une communication entre eux intervient à l'instant matériel prévu, sans que l'un doive interroger l'autre en permanence.

### 4. Respect absolu du matériel

**Ne modifie pas le fonctionnement interne des puces pour obtenir de meilleures performances.**

Conserve leurs fréquences, cycles, fronts, états électriques, délais, priorités, comportements de bus et synchronisations exacts.

Ne crée pas artificiellement un cycle de retard pour une notification. Le signal doit se propager selon le comportement électrique réel ; le récepteur le prend en compte au moment matériel approprié.

Les accès partagés à la RAM et au bus doivent respecter leurs arbitrages et timings.

Toute l'émulation et les états des composants doivent rester directement en mémoire hôte, avec le minimum de traitements intermédiaires nécessaires.

### 5. Implémentation demandée

**Passe maintenant à la modification effective du code.**

Commence par les mécanismes communs d'ordonnancement, de signaux et de câblage dans `RetroMediaLab.Chips`, puis adapte progressivement les modules de machines et leurs raccordements directs aux puces.

L'objectif est d'éliminer structurellement les consultations permanentes entre puces, pas de déplacer ces consultations dans d'autres méthodes.

Ne te limite pas au CRTC, au VIA ou aux interruptions : cette architecture doit pouvoir servir à toutes les machines émulées par RML.

Conserve les fonctionnalités existantes et les résultats matériels exacts. Après chaque bloc fonctionnel, vérifie les régressions, puis poursuis l'implémentation. Mesure les performances globales en Debug et Release lorsque les changements structurels sont intégrés.

**Ne reviens pas à des micro-optimisations locales et ne t'arrête pas à une nouvelle documentation d'architecture. Implémente cette refonte dans le code existant, en conservant la précision matérielle.**