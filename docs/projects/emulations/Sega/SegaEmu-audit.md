# Audit SegaEmu et Chips — solutions et bilan de refonte

Constats initiaux du 14 novembre 2026. Le bilan final de refactor-sega-emu figure en fin de document ; sa référence est le code du commit 476fc8779.

**Statut après annulation des prototypes — 14 novembre 2026.** Les modifications expérimentales du code, des tests et du générateur ont été annulées. Les routes et mesures décrites dans ce document sont un historique ; elles ne sont plus présentes dans le code de main. Les cases cochées des essais décrivent ce travail passé.

**Nouveau chantier autorisé : refactor-sega-emu.** Refaire la séparation des responsabilités dans SegaEmu et ses puces, avec une configuration explicite et des composants réutilisables, en conservant le fonctionnement existant. La lisibilité et la réutilisabilité guident le découpage ; les mesures restent un contrôle des coûts.

Périmètre : `RetroMediaLab.Emulation.Sega/Emulators/SegaEmu` et les composants de `RetroMediaLab.Chips` utilisés par ce cœur. Les autres émulateurs Sega ne sont pas à modifier. Atari, Amiga et Amstrad servent de comparaison pour l'organisation et l'intégration aux services communs.

Les sections historiques décrivent les propositions et les cinq anciennes routes expérimentales, retirées avant la refonte. Le bilan final ci-dessous décrit le code effectivement conservé et ses mesures.

## Objectif et règle de décision

L'objectif est de comparer les gains et pertes des routes sans retirer l'original. En Debug, comparer le même jeu entre solutions et relever les pertes éventuelles. En Release, les seuils demandés sont : rester au-dessus de 100 FPS et conserver plus de la moitié des FPS de l'original. Les contrôles d'état et de sorties servent à vérifier que le travail exécuté reste comparable.

- Mesurer séparément la vitesse du moteur, la vitesse avec publication audio/vidéo, le démarrage, la mémoire et les sauvegardes. Un gain mémoire ou un chargement plus rapide n'est pas automatiquement un gain de FPS.
- Comparer la même charge : mêmes médias, options, BIOS, scène, entrées, nombres de trames et durée émulée. Ne pas augmenter le frameskip ou supprimer des traitements pour annoncer artificiellement une accélération.
- Comparer les routes par cas. Une petite perte n'est pas automatiquement éliminatoire : appliquer les seuils Release demandés, présenter les écarts Debug, puis laisser l'utilisateur choisir les routes à conserver pour les essais suivants.
- Présenter les médianes et la dispersion des répétitions propres à chaque cas ; ne pas utiliser la variation d'une autre machine comme marge d'erreur de ce cas.
- Les changements de rangement et les corrections fonctionnelles sans gain de vitesse restent explicitement distincts des optimisations.

Les routes restent expérimentales : leur admission aux essais ne vaut pas adoption. La machine hôte n'est pas utilisée comme explication des écarts dans cette campagne.

## 1. Le 68000 réutilisable embarque encore des règles Sega

**Constat.** Le cœur impose les multiplicateurs `7` ou `4`, ajoute un rafraîchissement périodique dans sa boucle d'instructions et détermine l'écriture de `TAS` à partir du rôle principal/secondaire. Son contrat demande aussi les cycles du processeur principal et de son rafraîchissement. Cela limite son emploi comme processeur générique.

Références : [Cpu68000.cs](../../../../src/RetroMediaLab.Chips/CPU/Motorola/M68000/Execution/Services/Cpu68000.cs), notamment lignes 75, 166–169 et 224 ; [ICpu68000Bus.cs](../../../../src/RetroMediaLab.Chips/CPU/Motorola/M68000/Execution/Interfaces/ICpu68000Bus.cs).

**Solutions à faire, si ce point est traité :**

- [x] Modifier `src/RetroMediaLab.Chips/CPU/Motorola/M68000/Execution/Interfaces/ICpu68000Bus.cs` et `Services/Cpu68000.cs` : ajouter des routes à politique configurable et à boucle spécialisée, tout en conservant l'API et la boucle historiques pour l'original.
- [x] Modifier `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Common/SegaBoard.Buses.cs` et `Machines/MegaCd/Hardware/Scd.cs` : fournir explicitement les politiques Sega et les conversions de cycles actuelles aux routes candidates, sélectionnées par le banc.
- [x] Modifier `tests/RetroMediaLab.Chips.Tests/SegaEmu/InstructionCpuTests.cs` : couvrir les deux profils Sega, la décision TAS et un profil neutre, sans ROM externe.

**Sélecteur 1 repris et mesuré.** Original, profile, specialized, cached et generic sont fabriqués. Les résultats du CPU seul et des seules machines MD/MCD sont détaillés à la fin du document ; les anciennes conclusions ont été retirées.

**Gains mesurés sur le programme CPU ciblé.** La spécialisée donne +16.08 % en Debug et +7.11 % en Release sur le profil principal ; +18.56 % et +105.88 % sur le secondaire. Ces valeurs décrivent ce programme isolé. Sur les jeux complets mesurés, la spécialisée donne en Release -2.32 % sur Aladdin et +6.19 % sur Batman Returns. Voir les répétitions et les autres routes dans le bilan ciblé ci-dessous.

**Risque de perte.** Important si le découplage ajoute un appel virtuel, un délégué ou une vérification à chaque instruction ou accès mémoire. Une conversion générale des horloges peut également ajouter du travail. Ne pas remplacer le chemin actuel par une chaîne d'abstractions dans la boucle chaude.

**Décision pour les essais.** Conserver les cinq routes pour examen, avec original sélectionné par défaut. Les quatre candidates passent les seuils Release sur les deux jeux mesurés. Ne pas confondre le débit du programme CPU isolé avec le gain de FPS de la machine complète.

## 2. Les états internes des périphériques d'entrée ne sont pas sauvegardés

**Constat.** La machine sérialise les registres d'E/S, mais pas les compteurs, phases et délais internes des manettes six boutons, souris et multitaps. `InputHardware` ne fournit pas de capture/restauration complète de ces états. Une reprise peut donc retrouver un protocole dans une autre phase.

Références : [SegaBoard.State.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/State/SegaBoard.State.cs), ligne 41 ; [InputHardware.State.cs](../../../../src/RetroMediaLab.Chips/Controllers/Sega/Input/InputHardware.State.cs).

**Solutions à faire, si cette correction est retenue :**

- [ ] Créer `src/RetroMediaLab.Chips/Controllers/Sega/Input/InputHardware.Context.cs` : capturer et restaurer les états locaux des protocoles, sans sérialiser le bus, les callbacks ni les objets de session.
- [ ] Modifier `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/State/SegaBoard.State.cs` : intégrer ce contexte, versionner le format et définir explicitement le traitement des anciens états incomplets.
- [ ] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/Sega/SegaEmu/MachineIntegrationTests.cs` : vérifier une reprise au milieu d'une séquence de manette six boutons et d'un échange souris ou multitap, sur données en mémoire.

**Gain de vitesse possible.** Aucun gain attendu pendant l'exécution ordinaire. C'est une correction de sauvegarde/restauration, pas une optimisation de FPS.

**Risque de perte.** Contexte plus volumineux et opérations de sauvegarde/chargement potentiellement plus longues. Il faut éviter d'ajouter des copies ou allocations à chaque lecture de manette : le travail supplémentaire doit rester dans la capture et la restauration.

**Décision pour l'objectif vitesse.** Hors priorité d'accélération. Si elle est réalisée séparément, mesurer son coût ponctuel et vérifier que les FPS courants ne baissent pas.

## 3. Le SC-3000 est actuellement une variante SG-1000

**Constat.** `Sc3000Machine` reprend le matériel SG-1000, ses ports et son initialisation. Aucun câblage spécifique du clavier SC-3000 n'apparaît dans SegaEmu. La préparation de contenu adapte une extension de fichier, ce qui ne constitue pas une implémentation complète de la machine.

Références : [Sc3000Machine.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/Sc3000/Sc3000Machine.cs) ; [ContentFunctions.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Functions/ContentFunctions.cs).

**Solutions techniques possibles, à traiter comme un chantier fonctionnel distinct :**

- [ ] Modifier `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/Sc3000/Sc3000Machine.cs` : définir la composition et les accès propres au modèle au lieu de reprendre seulement les ports SG-1000.
- [ ] Créer `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/Sc3000/Sc3000KeyboardBus.cs` : raccorder les échanges du clavier aux composants et aux entrées hôtes existants.
- [ ] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/Sega/SegaEmu/MachineIntegrationTests.cs` : distinguer la compatibilité cartouche SG-1000 de la prise en charge des entrées SC-3000.

**Gain de vitesse possible.** Aucun gain attendu du seul ajout de ces fonctions. Des chemins propres à la machine peuvent ensuite être spécialisés, mais cela ne prouve aucune accélération.

**Risque de perte.** Ajouter des composants et des échanges absents aujourd'hui peut augmenter le coût d'exécution. Comparer une implémentation complète à la variante actuelle ne mesure pas la même charge.

**Décision pour l'objectif vitesse.** Ne pas inclure ce développement dans les optimisations promises sans perte. Il faut un périmètre et un accord distincts si l'objectif devient d'étendre les fonctions du SC-3000.

## 4. Le VDP et le RF5C164 conservent des responsabilités de carte

**Constat.** `IVdpBus` expose le BIOS système, la banque de démarrage CD, la présence du SVP, les particularités de cartouche et la RAM machine. Le VDP utilise ces informations. Le RF5C164 contient également un transfert DMA utilisant les adresses et la RAM du contrôleur CD. Ces interfaces sont utilisables avec un adaptateur, mais restent liées à la composition Sega.

Références : [IVdpBus.cs](../../../../src/RetroMediaLab.Chips/Video/Sega/VDP/IVdpBus.cs), [Vdp.Controller.cs](../../../../src/RetroMediaLab.Chips/Video/Sega/VDP/Vdp.Controller.cs) et [Rf5c164.cs](../../../../src/RetroMediaLab.Chips/Audio/Ricoh/RF5C164/Rf5c164.cs), méthode `DmaWrite`.

**Solutions à faire, par extraction limitée :**

- [ ] Modifier `src/RetroMediaLab.Chips/Video/Sega/VDP/IVdpBus.cs`, `Vdp.cs` et `Vdp.Controller.cs` : remplacer les décisions de carte par les opérations de bus réellement nécessaires, en conservant des accès rapides aux données.
- [ ] Modifier `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Common/SegaBoard.Buses.cs` et `Memory/SegaBoard.Memory68k.cs` : porter les décisions liées aux banques CD, cartouches et autres composants sur la carte.
- [ ] Modifier `src/RetroMediaLab.Chips/Audio/Ricoh/RF5C164/Rf5c164.cs` et `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/MegaCd/Hardware/Scd.Operations.cs` : déplacer le câblage DMA CD vers le Mega-CD, en gardant une primitive de transfert PCM adaptée aux blocs.

**Gain de vitesse possible.** Aucun gain assuré par le déplacement. Une spécialisation du bus et des transferts par blocs pourrait réduire des décisions répétées ; elle doit être localisée par profilage avant modification.

**Risque de perte.** Élevé si un accès direct à la RAM devient un callback par octet, par pixel ou par mot transféré. Les boucles de rendu et de DMA doivent conserver un accès efficace ; ne pas remplacer une opération de bloc par des milliers d'appels.

**Décision pour l'objectif vitesse.** Déplacer d'abord sans changer l'algorithme, puis mesurer chaque candidat séparément. Ne pas accepter une perte de vitesse pour obtenir seulement une interface plus abstraite.

## 5. La base SegaBoard concentre encore la majorité du matériel

**Constat.** La classe partielle représente environ 3 989 lignes réparties en 15 fichiers. Elle regroupe composition, bus, accès mémoire, réinitialisation, sauvegardes et ordonnanceurs de plusieurs machines. Les petites classes sous `Machines/` délèguent largement à cette base. Les dossiers séparent les fichiers davantage que les responsabilités.

Références : [SegaBoard.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Boards/SegaBoard.cs), [SegaBoard.FrameSchedulers.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Scheduling/SegaBoard.Schedulers.cs) et [MegaDriveMachine.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/MegaDrive/MegaDriveMachine.cs).

**Solutions à faire progressivement :**

- [ ] Modifier `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Common/SegaBoard.cs`, `SegaBoard.BusInitialization.cs` et `SegaBoard.FrameSchedulers.cs` : identifier les responsabilités communes indispensables et les décisions propres aux familles 8 bits, Mega Drive et Mega-CD.
- [ ] Modifier `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/MegaDrive/MegaDriveMachine.cs` et `Machines/MegaCd/MegaCdMachine.cs` : y porter les décisions de composition correspondantes, sans ajouter de couches intermédiaires dans les chemins chauds.
- [ ] Modifier `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Factories/BoardFactory.cs` : choisir la composition et les chemins d'exécution une fois à la création, plutôt que multiplier les décisions pendant l'émulation.

**Gain de vitesse possible.** Le rangement seul n'accélère pas le code. Une spécialisation réelle peut supprimer des branchements par machine et faciliter l'optimisation par le JIT. C'est une hypothèse de travail, pas un résultat.

**Risque de perte.** Appels virtuels supplémentaires, délégations en cascade, objets auxiliaires et dispersion des données. Un fichier plus court ne garantit pas un chemin d'exécution plus court.

**Décision pour l'objectif vitesse.** Ne conserver une spécialisation que si la machine complète gagne ou reste sans perte reproductible. Éviter une refonte générale avant d'avoir identifié les branches réellement coûteuses.

## 6. Un état Mega-CD peut être refusé après avoir modifié la machine

**Constat.** L'en-tête principal et la longueur sont contrôlés avant la restauration, mais le marqueur interne `SCD!` est vérifié après `Reset()` et après le chargement de plusieurs blocs. Un fichier ayant une longueur valide peut donc être refusé alors que la machine a déjà changé.

Référence : [SegaBoard.State.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/State/SegaBoard.State.cs), lignes 68 et 98. Le contrôle actuel de non-mutation sur état invalide porte notamment sur un scénario SG-1000 ; il ne couvre pas ce cas interne Mega-CD.

**Solutions à faire :**

- [ ] Modifier `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/State/SegaBoard.State.cs` : vérifier les marqueurs, limites et tailles de blocs nécessaires avant la première modification du cœur, sans réinitialiser la machine pendant la validation.
- [ ] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/Sega/SegaEmu/MachineIntegrationTests.cs` : ajouter un état Mega-CD de longueur correcte avec marqueur interne invalide et vérifier que son refus conserve l'état courant.

**Gain de vitesse possible.** Aucun gain de FPS attendu. Un rejet précoce évite seulement du travail inutile lors du chargement d'un état invalide.

**Risque de perte.** Temps supplémentaire au chargement si la validation reparcourt tout le fichier. Une copie complète de sauvegarde de la machine pour effectuer un retour arrière serait particulièrement coûteuse ; privilégier la validation préalable des blocs nécessaires.

**Décision pour l'objectif vitesse.** Correction ponctuelle distincte des optimisations. Elle ne doit ajouter aucun contrôle dans `RunFrame` ; mesurer uniquement son éventuel coût de chargement et vérifier l'absence d'effet sur les FPS courants.

## 7. Des ressources inutilisées sont encore créées sur les petites machines

**Constat.** `CartridgeState` réserve 10 Mio de ROM pour chaque machine. La base crée des tampons audio pour plusieurs sources. `SoundUnit` réserve son tampon FM et instancie un YM2413 même lorsque la FM n'est pas activée. L'instanciation différée existe déjà pour le 68000 et des composants Mega-CD ; elle reste partielle.

Références : [CartridgeAddressSpace.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Cartridge/CartridgeAddressSpace.cs), [SegaBoard.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Boards/SegaBoard.cs) et [SoundUnit.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Audio/SoundUnit.cs).

**Solutions à faire en première approche :**

- [ ] Modifier `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Common/Audio/SoundUnit.cs` et ses fichiers `SoundUnit.Context.cs` et `SoundUnit.Output.cs` : ne créer la FM et ses buffers que pour une configuration qui les utilise ; adapter explicitement réinitialisation et sauvegardes au chemin sans FM.
- [ ] Modifier `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Common/SegaBoard.cs` et `SegaBoard.Media.cs` : créer les buffers des sources CD et cartouche audio lors de leur utilisation, sans ajouter de tests répétitifs par échantillon.
- [ ] Modifier `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Cartridge/CartridgeAddressSpace.cs` et `Common/LoadRom.cs` : étudier une capacité ROM adaptée à la famille, en conservant les zones nécessaires aux BIOS, banques, mappers et cartouches spéciales ; ne pas réduire le tableau sans cet inventaire.

**Gain de vitesse possible.** Candidat favorable pour réduire les allocations, le travail d'initialisation et la mémoire par instance. Un gain de FPS est possible par réduction de la pression mémoire et du GC, mais n'est pas établi. Un objet inutilisé créé une seule fois ne coûte pas nécessairement du temps à chaque trame.

**Risque de perte.** Vérifications de présence répétées dans les boucles audio, premières utilisations plus lentes, erreurs de capacité de ROM ou chargements d'états moins rapides. Les cartouches peuvent utiliser des zones au-delà de leur taille de fichier : le simple redimensionnement au nombre d'octets lus n'est pas une solution sûre.

**Décision pour l'objectif vitesse.** Commencer par les composants réellement inutilisés, en conservant des chemins sélectionnés une fois. Mesurer séparément démarrage, allocations et FPS. Ne pas annoncer une optimisation de FPS si seul le démarrage ou la mémoire s'améliore.

## 8. Finition et comparaison avec Atari, Amiga et Amstrad

**Constat.** L'intégration suit déjà les services communs, mais le code matériel garde beaucoup de noms hérités du C, de conversions répétitives, d'imports inutiles et de contrats définis au milieu des implémentations. Les adaptateurs Atari, Amiga et Amstrad constituent surtout une comparaison des couches d'intégration : leurs wrappers natifs ne prouvent pas la qualité interne d'un cœur C#.

Références : [SystemConfiguration.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Contracts/SystemConfiguration.cs), [MachineResources.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Common/MachineResources.cs), [StellaMachineFactory.cs](../../../../src/RetroMediaLab.Emulation.Atari/Emulators/Stella/Factories/StellaMachineFactory.cs), [AmigaMachineFactory.cs](../../../../src/RetroMediaLab.Emulation.Commodore/Emulators/UAE/Common/Interop/Factories/AmigaMachineFactory.cs) et [Caprice32MachineFactory.cs](../../../../src/RetroMediaLab.Emulation.Amstrad/Emulators/Caprice32/Factories/Caprice32MachineFactory.cs).

**Solutions de rangement possibles :**

- [ ] Déplacer `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Common/MachineResources.cs` vers `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Contracts/MachineResources.cs` : conserver le contrat et adapter son espace de noms et ses références.
- [ ] Déplacer `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Common/SystemConfiguration.cs` vers `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Contracts/SystemConfiguration.cs` : conserver les valeurs, les propriétés et le comportement, puis adapter les références.
- [ ] Modifier les fichiers SegaEmu concernés par les extractions précédentes : supprimer les imports inutiles et clarifier les noms en suivant les conventions existantes, sans retirer aveuglément les conversions ou les blocs `unchecked`.

**Gain de vitesse possible.** Aucun gain attendu d'un changement de nom ou de dossier. Le bénéfice porte sur la compréhension et le coût des prochaines modifications.

**Risque de perte.** Faible pour un déplacement purement structurel, mais un changement simultané de conversions, de types ou d'ordre d'appels devient une modification de comportement. Il ne faut pas mélanger rangement et optimisation dans une même mesure.

**Décision pour l'objectif vitesse.** Pas une priorité d'accélération. Faire uniquement le rangement nécessaire à une optimisation identifiée, puis séparer les autres travaux de finition.

## 9. Complément ciblé : allocations et publication à chaque trame

**Constat supplémentaire utile à l'objectif vitesse.** `PublishVideo` alloue et copie un tableau de pixels à chaque publication ; `RunFrame` copie aussi les échantillons audio dans un nouveau tableau. La publication vidéo est appelée même lorsque le rendu de cette trame a été sauté. Ces opérations sont dans le chemin exécuté en continu.

Références : [ManagedCore.Output.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Services/ManagedCore.Output.cs), méthode `PublishVideo` ; [ManagedCore.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Services/ManagedCore.cs), méthode `RunFrame`.

À titre de calcul de volume, une image de 256 × 192 en 32 bits représente 192 Kio, soit 11,25 Mio/s à 60 publications/s, pour le seul tableau de pixels. Ce calcul n'est pas une mesure de coût CPU ni un gain de vitesse annoncé.

**Solutions à étudier en priorité après profilage :**

- [ ] Modifier `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Services/ManagedCore.cs` et `ManagedCore.Output.cs` : éviter une nouvelle publication et une copie d'image lorsque le rendu a réellement été sauté, en conservant la dernière image et le bon comportement de séquence et de capture.
- [ ] Modifier ces mêmes fichiers : regrouper les copies contiguës lorsque la géométrie et le pitch le permettent, et sortir du chemin par trame les calculs de présentation qui ne changent qu'avec les options ou le format vidéo.

**Gain de vitesse possible.** Candidat plus directement lié aux FPS que les déplacements de dossiers. Éviter une publication inutile peut réduire les allocations et les copies lorsque le frameskip est actif ; cela ne donne pas de gain en mode sans saut par ce seul changement. Les autres réductions de copie restent à mesurer sur le mode normal.

**Risque de perte.** Affichage non actualisé, séquences incohérentes, perte de captures ou copie plus lente avec certains pitches. Un pool de buffers ne peut pas être ajouté en réutilisant un tableau encore détenu par le consommateur : il faut d'abord vérifier le contrat de durée de vie existant. Aucune modification des contrats partagés ou d'autres modules n'est implicitement autorisée par cette proposition.

**Décision pour l'objectif vitesse.** Profiler d'abord la publication et le GC. Retenir seulement les réductions de travail inutiles dont le contrat de consommation reste respecté. Ne pas confondre une baisse du nombre d'images publiées avec une accélération du moteur à charge identique.

## Éléments déjà correctement séparés à préserver

- Aucune référence à l'implémentation ou aux données d'un autre émulateur n'a été trouvée dans SegaEmu ; les services communs Sega sont partagés.
- Le projet Chips ne dépend pas du projet Emulation.
- Les décodeurs de médias restent derrière MediaEngine. Le lecteur émulé emprunte le média ; l'adaptateur qui le possède assure son détachement et sa fermeture.
- Les EEPROM et le SSP1601 présentent des bus ou mémoires externes plus clairement isolés.
- Les chemins de libération examinés emploient des `finally`. Les extractions proposées doivent conserver cette propriété et les responsabilités du propriétaire des ressources.

## Ordre proposé pour rechercher des gains sans perte

| Ordre | Sujet | Effet à rechercher | Condition pour conserver le changement |
| --- | --- | --- | --- |
| 1 | Publication et copies par trame, point 9 | Moins de travail courant et de pression GC | Gain mesuré au niveau concerné, sans ralentissement ailleurs ni image manquante |
| 2 | Composants et buffers inutilisés, point 7 | Moins d'allocations, démarrage et mémoire réduits ; FPS éventuellement meilleurs | Distinguer le gain mémoire/démarrage d'un gain de vitesse ; aucun chemin courant ralenti |
| 3 | Spécialisation des chemins machine, point 5 | Moins de branchements dans les chemins chauds identifiés | Gain ou absence de perte reproductible sur chaque machine concernée |
| 4 | Extraction limitée CPU/VDP/DMA, points 1 et 4 | Réutilisabilité, avec opportunités de spécialisation | Ne pas accepter une baisse de vitesse pour une interface plus abstraite |
| Séparé | États d'entrée et rejet des sauvegardes, points 2 et 6 | Corriger les sauvegardes et les chargements | Aucun coût ajouté à la boucle courante ; coût ponctuel documenté |
| Hors optimisation | SC-3000 complet et rangement seul, points 3 et 8 | Fonctions supplémentaires ou maintenance | Ne pas les présenter comme des gains de vitesse |

## Protocole à appliquer avant toute acceptation de code

1. Établir une référence sur le commit exact précédant chaque candidat ; les mesures anciennes ne remplacent pas cette référence.
2. Mesurer Debug et Release séparément, sur le même environnement, après chauffe. Alterner l'ordre référence/candidat sur plusieurs passages.
3. Utiliser des scènes comparables, hors introduction BIOS sauf si le démarrage est la mesure visée. Si le format d'état change, ne pas utiliser un état incompatible pour fabriquer le point de comparaison.
4. Séparer moteur seul et session avec publication audio/vidéo. Consigner temps par trame, trames émulées par seconde, allocations, collections GC, mémoire et coûts de démarrage/sauvegarde selon le point traité.
5. Comparer par machine et scénario, avec les seuils Release définis plus haut et les gains/pertes Debug affichés. Aucun test de performance sur une machine qui n'exécute pas la puce modifiée.
6. Contrôler que la modification n'a pas supprimé du travail demandé ou cassé les fonctions concernées. Un résultat obtenu en sautant davantage de trames ne valide pas une optimisation à charge constante.
7. Les futurs tests utiles en mémoire peuvent rester dans les projets existants. Tout banc temporaire utilisant ROM, BIOS ou DLL externes devra être supprimé après usage avec ses binaires et sessions ; conserver seulement les résultats utiles.

**Bilan : le premier sélecteur dispose de cinq routes testées. Les autres sélecteurs restent en attente du choix de l'utilisateur. Aucun commit.**

## Reprise ciblée du sélecteur 1 — 68000

Campagne du 14 novembre 2026. Référence : route originale dans le même banc et la même configuration que les candidates.

### Routes fabriquées

| Route | Organisation testée |
| --- | --- |
| original | Constructeur et boucle historiques conservés |
| profile | Politique configurable : conversions de cycles, rafraîchissement, TAS et suivi du PC |
| specialized | Boucles distinctes pour rafraîchissement principal et suivi du PC secondaire, choisies avant l'itération |
| cached | Paramètres immuables chargés une fois par appel ; référence locale au contexte et table de cycles conservée pendant la boucle |
| generic | Traits statiques de boucle pour permettre la spécialisation des branchements, avec valeurs d'horloge fournies par le profil |

Les instructions et leurs traitements restent communs. Les modifications nouvelles concernent Cpu68000ExecutionProfile.cs, Cpu68000.Profile.cs et le raccordement CpuRoute/CpuRouteFunctions de SegaEmu. Les autres choix de l'audit restent inchangés.

### Périmètre et compilation

- **CPU seul** : mêmes sources du 68000 et même fabrique/profils que SegaEmu ; programme fixe en RAM exécutant MOVEQ, ADDQ.L, MOVE.L, TAS et BRA. Le bus minimal compte lectures, lectures mappées et écritures. Aucun autre circuit n'exécute de travail. Ce programme n'est pas une extraction du code des jeux.
- Deux profils CPU utiles : principal MD/MCD, unités multipliées par 7 ; secondaire MCD, unités multipliées par 4. Le principal commun n'est pas testé deux fois pour les deux machines.
- **Machine complète émulée** : SegaEmu.ManagedCore.RunFrame avec CPU, vidéo, audio, bus, entrées au repos, lecteur et composants CD pour MCD, et publication des images/blocs audio. Sans fenêtre, sortie audio de l'hôte ni limiteur.
- Jeux : Aladdin (Europe) sur MD et Batman Returns (Europe) sur MCD, les plus lents de la référence Debug précédente. Ces mêmes jeux sont utilisés en Release. La sélection ne prétend pas identifier le jeu le plus lent dans toutes les configurations Release.
- Seules les sources de SegaEmu et de son 68000 sont compilées dans le banc temporaire. Les dépendances communes existantes sont référencées ; application et autres émulateurs ne sont pas recompilés.
- Les sources liées du 68000 remplacent ses types homonymes de la DLL commune dans la compilation du banc. L'avertissement CS0436 correspondant est supprimé intentionnellement. Les routes réellement instanciées sont contrôlées pour le principal et le secondaire.
- Constructions validées : Debug 15.90 s, Release 15.71 s ; zéro erreur, 32 avertissements sur les sources liées. Les premiers essais avaient demandé 17.58 s et 9.65 s : environ 59 s de compilation cumulée, sans construction des autres modules.
- Une erreur de copie du banc a été détectée : la première exécution Release chargeait une DLL Chips Debug malgré sa référence Release. Elle a été interrompue, isolée dans invalid-release-mixed-dependencies.*, puis remplacée. La copie explicite et le contrôle SHA-256 des dépendances sont maintenant faits avant chaque lancement. Debug a également été revalidé avec ce contrôle.

### Charge, chronométrage et contrôles

- Trois passages par route ; original d'abord au premier passage, ordre tournant aux suivants.
- Chauffe CPU préalable de 750 ms par route. Le budget mesuré est calibré une fois sur l'original par profil et partagé entre toutes les routes ; un appel Run par mesure.
- Pour les machines, une instance de chauffe est détruite, puis chaque mesure utilise une machine neuve avec 60 trames initiales identiques. 60 trames sont mesurées en Debug, 1 800 en Release.
- Les deux jeux tournent ici avec 49.701459 trames nominales/s et un CPU principal nominal de 7 600 489 Hz.
- Budget temporel de machine : 64 227 600 unités d'horloge maître pour chaque passage Debug et 1 926 828 000 pour chaque passage Release. Ce budget inclut la durée de la machine ; il ne compte pas les seules instructions retirées par le CPU.
- Horodatage Stopwatch ; temps CPU du processus, allocations du thread et collections GC génération 0/1/2 relevés séparément. Initialisation, chauffe, empreintes et libération sont hors chronométrage.
- CPU seul : contexte final, mémoire et nombres d'accès au bus identiques à l'original, dans chaque profil et configuration.
- Machine : état final, image finale, dernier bloc audio et unités d'horloge maître identiques. Ce contrôle ne compare pas toutes les images ni toute la trace audio.
- 120 relevés valides : 24 originaux, 96 candidates. Aucun test SG-1000, SMS, GG ou autre machine sans 68000.
- Les FPS Debug/Release couvrent des durées émulées différentes. Le calcul utile est le rapport entre routes du même cas ; aucun facteur global Debug/Release n'est déduit.

### CPU seul — débits

Débits en **millions de cycles CPU émulés par seconde réelle**, et écarts des médianes à l'original. Les nombres ne représentent pas une fréquence physique mesurée du processeur hôte.

| Configuration et profil | Original | Profile | Specialized | Cached | Generic |
| --- | ---: | ---: | ---: | ---: | ---: |
| Debug — MD-MCD main / 7 | 50.14 | 53.69 (7.08 %) | 58.21 (16.08 %) | 57.47 (14.61 %) | 52.93 (5.56 %) |
| Debug — MCD secondary / 4 | 54.12 | 63.11 (16.61 %) | 64.16 (18.56 %) | 65.85 (21.67 %) | 58.13 (7.41 %) |
| Release — MD-MCD main / 7 | 1353.83 | 1436.93 (6.14 %) | 1450.03 (7.11 %) | 1391.71 (2.80 %) | 1097.26 (-18.95 %) |
| Release — MCD secondary / 4 | 690.90 | 686.28 (-0.67 %) | 1422.40 (105.88 %) | 690.25 (-0.09 %) | 875.74 (26.75 %) |

Les comptes exacts du premier original, identiques entre routes d'un même profil :

| Configuration / profil | Cycles CPU effectifs | Lectures | Lectures mappées | Écritures |
| --- | ---: | ---: | ---: | ---: |
| Debug principal | 25 784 862 | 396 690 | 3 966 903 | 793 380 |
| Debug secondaire | 22 485 252 | 351 332 | 3 161 989 | 1 053 996 |
| Release principal | 142 857 162 | 2 197 802 | 21 978 026 | 4 395 606 |
| Release secondaire | 250 000 000 | 3 906 250 | 35 156 250 | 11 718 750 |

Le budget demandé est dépassé seulement pour terminer l'instruction en cours ; les unités effectives sont enregistrées. Les compteurs du bus font partie du coût identique du bus de ce microprogramme.

**Passage à signaler :** Cached, principal Release, donne 458.70 / 1391.71 / 1441.70 Mcycles/s, soit 0.3114382 / 0.1026485 / 0.0990891 s. Son rapport apparié le plus bas est -67.54 %. La médiane de +2.80 % ne masque donc pas ce passage lent ; il est conservé, sans lui attribuer une cause non mesurée. Generic perd environ 18.95 % en médiane sur ce même profil principal Release, tout en gagnant sur le secondaire.

### Machines complètes — FPS

Médianes de trois passages, avec écarts par rapport à l'original du même jeu/configuration.

| Configuration et jeu | Original | Profile | Specialized | Cached | Generic |
| --- | ---: | ---: | ---: | ---: | ---: |
| Debug — Aladdin — MD | 26.95 | 29.02 (7.69 %) | 27.72 (2.87 %) | 29.13 (8.12 %) | 29.55 (9.66 %) |
| Debug — Batman Returns — MCD | 29.14 | 31.27 (7.30 %) | 31.04 (6.50 %) | 30.87 (5.94 %) | 32.56 (11.72 %) |
| Release — Aladdin — MD | 626.32 | 602.94 (-3.73 %) | 611.81 (-2.32 %) | 598.05 (-4.51 %) | 592.75 (-5.36 %) |
| Release — Batman Returns — MCD | 646.73 | 692.26 (7.04 %) | 686.78 (6.19 %) | 681.20 (5.33 %) | 643.27 (-0.53 %) |

### Dispersion propre à chaque comparaison de machine

Plage des trois écarts appariés candidat/original. Aucun écart d'une machine sans cette puce n'est utilisé pour juger ces cas.

| Configuration / jeu | Route | Plus petit écart | Plus grand écart |
| --- | --- | ---: | ---: |
| Debug / MegaCd | Profile | -4.07 % | 16.99 % |
| Debug / MegaCd | Specialized | -4.09 % | 14.84 % |
| Debug / MegaCd | Cached | -2.94 % | 16.96 % |
| Debug / MegaCd | Generic | 2.45 % | 15.64 % |
| Debug / MegaDrive | Profile | -7.23 % | 8.50 % |
| Debug / MegaDrive | Specialized | -2.83 % | 2.87 % |
| Debug / MegaDrive | Cached | -6.26 % | 8.93 % |
| Debug / MegaDrive | Generic | -4.94 % | 10.48 % |
| Release / MegaCd | Profile | 1.37 % | 7.04 % |
| Release / MegaCd | Specialized | 3.75 % | 7.36 % |
| Release / MegaCd | Cached | 2.71 % | 9.80 % |
| Release / MegaCd | Generic | -3.94 % | 6.40 % |
| Release / MegaDrive | Profile | -4.54 % | 5.40 % |
| Release / MegaDrive | Specialized | -2.68 % | 6.96 % |
| Release / MegaDrive | Cached | -6.59 % | 4.55 % |
| Release / MegaDrive | Generic | -6.13 % | 3.62 % |

### Lecture des résultats et suite

- La spécialisée améliore les deux profils du programme CPU en Debug ; en Release, elle améliore le principal de 7.11 % et le secondaire de 105.88 %. Ce doublement du débit secondaire ne signifie pas un doublement des FPS du MCD.
- Sur les machines complètes, la spécialisée donne en Release -2.32 % sur Aladdin et +6.19 % sur Batman Returns. Les écarts de chaque passage sont indiqués ci-dessus.
- Generic est positive sur les médianes des deux jeux Debug, mais coûte davantage sur le programme CPU principal Release ; la décision doit examiner les deux niveaux.
- **Les quatre candidates passent les seuils Release sur ces deux jeux** : aucun passage sous 100 FPS et aucune perte de moitié par rapport à son original apparié. Le passage complet le plus lent est à 572.02 FPS.
- Cette admission aux essais ne vaut pas choix définitif. Les cinq routes restent disponibles, original reste sélectionné par défaut. Les autres sélecteurs ne sont pas commencés.

Relevés détaillés conservés dans artifacts/Emulation/segaemu-routes/cpu-targeted :

- results-Debug.json et results-Release.json : chaque passage, durées réelles/CPU, budgets et cycles effectifs, FPS ou débit CPU, allocations, GC, accès au bus et empreintes.
- summary.json : médianes, minimum/maximum, trois valeurs brutes, trois durées et plages de rapports appariés.
- dependencies-Debug.json et dependencies-Release.json : DLL communes et leurs empreintes contrôlées.
- selection.json, provenance.json, media-before.json, media-after.json et validation.json : sélection, sources, contrôles et nettoyage.
- Les données initiales non retenues restent identifiées séparément, dont le Release interrompu avec dépendances mixtes.

Le précédent document SegaEmu-routes-tests.md reste supprimé. Les sources C# du banc, projets, DLL temporaires et sessions ont été supprimés ; le générateur scripts/emulation/segaemu-cpu-routes.ps1 reste disponible. Son nettoyage couvre aussi la session laissée par l'interruption du passage invalide.

**Arrêt après le premier sélecteur. Aucun commit.**

## Bilan de la refonte — 15 novembre 2026

Les prototypes ont été annulés. Les documents ont été commités et poussés sur main dans 476fc8779, puis la branche refactor-sega-emu a été créée. La refonte a été validée pour son enregistrement sur refactor-sega-emu.

### Solutions effectivement réalisées

| Point | Solution conservée | Effet et limite |
| --- | --- | --- |
| 68000 | Cpu68000Configuration neutre, table canonique, cycles partagés par multiplicateur, boucle commune ; politiques Sega dans CpuConfigurationFunctions, bus MegaDriveCpuBus et MegaCdCpuBus. | Plus de sélection implicite de sous-CPU Sega, de règle TAS Sega ou de masque d’IRQ de carte dans la puce ; accès mémoire directs conservés. |
| Entrées | InputDeviceConfiguration résout le matériel ; métadonnées ROM dans InputConfigurationFunctions. Contexte explicite de 221 octets : phases, compteurs, délais, entrées et registres. | Reprise vérifiée pendant un échange six boutons et pendant un acquittement souris avec mouvements signés ; les callbacks sont reliés sans réinitialiser le protocole restauré. |
| SC-3000 | Classe et compatibilité cartouche SG-1000 conservées. | Le clavier/BASIC spécifique absent avant refonte reste une limite ; aucune nouvelle fonction n’est simulée. |
| VDP | Préréglage de reset fourni par la carte ; signaux de rendu, fenêtre mémoire DMA et lecture du port DMA exposés par IVdpBus. | BIOS, cartouche et SVP ne sont plus des données possédées par le VDP. |
| PCM | WriteWaveRam écrit dans la fenêtre de RAM sélectionnée ; registres et rebouclages du transfert portés par le contrôleur CD. | RF5C164 ne connaît plus le CDC ; franchissement de fenêtre testé. |
| Carte | Boards : composition et cycle de vie ; Buses : adaptateurs et accès mémoire ; Scheduling : trois composants choisis à l’initialisation ; Contracts : paramètres et ressources empruntées. Audio, Media et State regroupent leurs traitements. | La carte reste propriétaire du câblage et des mappers ; les ordonnanceurs restent internes à SegaEmu. Le choix de famille ne se répète plus à chaque trame. |
| États | Format SEGAEMU 00000003 ; taille, machine, moteur audio, marqueur SCD et bloc d’entrée contrôlés avant Reset. Lecture v2 conservée. | Marqueur CD corrompu refusé sans mutation. Les phases absentes d’un ancien fichier v2 ne peuvent pas être reconstituées : elles gardent le comportement antérieur de remise à zéro. |
| Ressources | Tables d’adressage, buffers audio annexes et FM créés à leur utilisation ; ROM 8 bits de 8 Mio au lieu de 10 Mio, croissance contrôlée. | Fenêtre BIOS historique de 4 Mio à partir de 0x400000 préservée. Sauver un état peut créer la FM inactive pour conserver le format audio antérieur. |
| Publication | Présentation mise en cache, copie contiguë lorsque possible, dernière image conservée quand le rendu est sauté ; persistance séparée dans ManagedCore.Persistence.cs. | Chaque image publiée conserve son propre tableau. Les mesures désactivent le frameskip. |
| Horloges | Multiplicateurs Z80, PSG et PCM fournis explicitement par MachineClockRatios ; six tables Z80 préparées et partagées à l’initialisation. | Bases de cycles neutres, ratio dynamique indépendant. Un autre utilisateur doit fournir ses rapports d’horloge et adapter les contrats de bus. |

Aucun appel ni accès aux constantes, contrats ou données d’un autre émulateur n’a été relevé dans SegaEmu. Les services communs Sega, Emulation, Chips et les adaptations MediaEngine existantes restent utilisés. Aucun texte d’option ni traduction n’a été ajouté.

### Défauts trouvés, corrections et retests

1. La réduction initiale de ROM 8 bits omettait les pages non mappées à 0x510000. Les tests d’options avec BIOS actif ont ensuite révélé la fenêtre BIOS plus grande. La capacité finale est de 8 Mio, avec croissance jusqu’à la limite existante de 10 Mio. Tests d’options, démarrage SMS sans cartouche et campagne complète relancés avec succès.
2. Le chargement v3 ne comptait pas le contexte d’entrée dans la taille retournée. Retour corrigé ; lecture ancienne/nouvelle et reprise d’état vérifiées.
3. Les premières mesures avec les DLL réelles relevaient des pertes Debug 8 bits. Le précalcul des six tables Z80 a été ajouté, puis toutes les machines ont été retestées. Champion Tennis SG-1000 passe notamment de 147,3 à 212,6 FPS Debug entre les deux versions refactorisées ; la référence avant refonte est à 188,4 FPS.
4. Les lancements avec mauvaise version de DLL ou type ancien provenant des copies du banc sont exclus. La comparaison finale utilise des assemblages séparés réels : référence reconstruite depuis 476fc8779, version finale du dépôt et dépendances de la configuration demandée, avec empreintes vérifiées. Les premiers relevés avec sources regroupées dans le banc restent des essais intermédiaires.

### Périmètre et protocole

- Huit modèles couverts : SG-1000, SC-3000, Mark III, Master System, Game Gear, Mega Drive, Mega-CD et Pico.
- 22 cas de jeux, 18 médias distincts déjà utilisés ; sept modèles sur médias réels. Pico : programme en mémoire seulement. SC-3000 : deux cartouches SG-1000 compatibles ; Mark III : deux cartouches SMS.
- Même média, BIOS et options avant/après. BIOS cartouche désactivé pour reproduire les scénarios précédents ; BIOS CD fournis pour Mega-CD. Frameskip désactivé, sans fenêtre ni limiteur.
- Trois passages par jeu et version ; 60 trames de chauffe, puis 60 trames mesurées en Debug ou 900 en Release.
- Adaptateur : machine complète avec publication des images et blocs audio. Machine seule : CPU, VDP, audio, bus, cartouches et CD lorsque présents, avec génération audio/vidéo, sans publication par l’adaptateur. Cette seconde mesure repart du même état brut v2.
- FPS des tableaux : médianes de trois passages. JSON : chaque passage, durées, trames, min/max, allocations du thread mesuré, fréquence nominale et pourcentage de vitesse réelle.
- Contrôles : dernière image et dernier bloc audio identiques à la référence pour chaque passage ; lecture d’états anciens ; sauvegarde/reprise des nouveaux ; refus non destructif d’un marqueur CD invalide.
- 24 tests autonomes de puces et 18 tests ciblés d’intégration réussissent dans chaque configuration : 42 par configuration, 84 exécutions. Module Sega et Chips compilés en Debug/Release ; application et autres modules non reconstruits.
- Le projet global Chips.Tests conserve l’import NEC préexistant en erreur. Les tests du périmètre passent dans un projet temporaire ciblé ; le test NEC n’a pas été modifié.

Ces contrôles portent sur les séquences automatisées de démarrage/attract et les programmes en mémoire. Ils ne démontrent pas une partie complète, tous les périphériques/réglages ou une compatibilité commerciale Pico. Cette comparaison SegaEmu avant/après ne réévalue pas l’ancienne différence audio avec GenesisPlusGXCS sur Final Fight CD.

### Mesures finales

Les FPS avec publication et ceux de la machine seule restent séparés ; chaque écart concerne le même cas.

#### Debug

| Machine | Jeu | FPS publication avant → après | Écart | FPS machine seule avant → après | Vitesse nominale après |
| --- | --- | ---: | ---: | ---: | ---: |
| GameGear | Columns (USA, Europe) | 245,8 → 251,0 | +2,1 % | 217,9 → 257,1 | 418,8 % |
| GameGear | Shinobi (USA, Europe) | 218,8 → 244,7 | +11,9 % | 198,6 → 251,2 | 408,4 % |
| GameGear | Sonic The Hedgehog (World) (Rev 1) | 206,6 → 192,6 | -6,8 % | 166,6 → 171,0 | 321,5 % |
| MarkIII | Alex Kidd in Miracle World (USA, Europe) | 176,0 → 165,3 | -6,1 % | 147,0 → 167,5 | 275,9 % |
| MarkIII | Sonic The Hedgehog (USA, Europe) | 162,9 → 165,5 | +1,6 % | 139,6 → 166,1 | 276,2 % |
| MasterSystem | Alex Kidd in Miracle World (USA, Europe) | 168,4 → 194,7 | +15,6 % | 142,1 → 185,2 | 324,9 % |
| MasterSystem | Sonic The Hedgehog (USA, Europe) | 169,4 → 143,3 | -15,4 % | 141,3 → 148,5 | 239,1 % |
| MasterSystem | Wonder Boy III - The Dragon's Trap (USA, Europe) | 90,6 → 92,8 | +2,5 % | 84,2 → 94,7 | 154,9 % |
| MegaCd | Batman Returns (Europe) | 30,7 → 33,2 | +8,2 % | 23,9 → 27,0 | 66,8 % |
| MegaCd | Dragon's Lair (Japan) (En,Ja,Fr,De,It) | 46,7 → 49,1 | +5,2 % | 33,1 → 35,4 | 81,9 % |
| MegaCd | Final Fight CD (Europe) | 31,3 → 33,1 | +5,6 % | 25,2 → 27,4 | 66,5 % |
| MegaDrive | Aladdin (Europe) | 29,6 → 28,4 | -4,0 % | 30,5 → 29,0 | 57,1 % |
| MegaDrive | Golden Axe (World) | 31,5 → 26,6 | -15,4 % | 33,0 → 26,7 | 44,4 % |
| MegaDrive | Gunstar Heroes (USA) | 49,0 → 47,2 | -3,7 % | 57,4 → 50,9 | 78,7 % |
| MegaDrive | Sonic The Hedgehog (USA, Europe) | 40,7 → 34,3 | -15,7 % | 34,1 → 27,6 | 57,2 % |
| MegaDrive | Streets of Rage 2 (USA) | 54,9 → 50,4 | -8,1 % | 45,3 → 41,4 | 84,1 % |
| MegaDrive | Thunder Force III (Japan, USA) | 31,4 → 30,8 | -2,0 % | 31,7 → 30,4 | 51,4 % |
| Sc3000 | 007 James Bond (Japan) (v2.7) (Othello Multivision) | 147,6 → 171,6 | +16,2 % | 126,2 → 162,8 | 286,4 % |
| Sc3000 | Champion Tennis (Japan) | 186,0 → 179,6 | -3,4 % | 162,8 → 188,5 | 299,8 % |
| Sg1000 | 007 James Bond (Japan) (v2.7) (Othello Multivision) | 174,5 → 180,3 | +3,3 % | 133,3 → 173,9 | 300,9 % |
| Sg1000 | Champion Tennis (Japan) | 188,4 → 212,6 | +12,9 % | 162,1 → 214,1 | 354,8 % |
| Sg1000 | Congo Bongo (Japan) | 163,7 → 165,8 | +1,3 % | 138,8 → 167,0 | 276,7 % |

#### Release

| Machine | Jeu | FPS publication avant → après | Écart | FPS machine seule avant → après | Vitesse nominale après |
| --- | --- | ---: | ---: | ---: | ---: |
| GameGear | Columns (USA, Europe) | 4509,5 → 4792,6 | +6,3 % | 4959,1 → 4999,6 | 7997,9 % |
| GameGear | Shinobi (USA, Europe) | 3565,9 → 4324,4 | +21,3 % | 4062,6 → 4735,6 | 7216,7 % |
| GameGear | Sonic The Hedgehog (World) (Rev 1) | 3503,3 → 3456,6 | -1,3 % | 4650,1 → 4691,2 | 5768,4 % |
| MarkIII | Alex Kidd in Miracle World (USA, Europe) | 3431,6 → 3634,7 | +5,9 % | 3593,8 → 3984,4 | 6065,6 % |
| MarkIII | Sonic The Hedgehog (USA, Europe) | 3515,9 → 3709,8 | +5,5 % | 3620,2 → 3850,0 | 6190,9 % |
| MasterSystem | Alex Kidd in Miracle World (USA, Europe) | 3002,6 → 3573,9 | +19,0 % | 3806,7 → 3876,6 | 5964,2 % |
| MasterSystem | Sonic The Hedgehog (USA, Europe) | 3552,8 → 3822,3 | +7,6 % | 3617,7 → 3828,6 | 6378,8 % |
| MasterSystem | Wonder Boy III - The Dragon's Trap (USA, Europe) | 2461,7 → 2590,1 | +5,2 % | 2185,4 → 2300,1 | 4322,4 % |
| MegaCd | Batman Returns (Europe) | 663,2 → 692,7 | +4,5 % | 683,4 → 724,2 | 1393,8 % |
| MegaCd | Dragon's Lair (Japan) (En,Ja,Fr,De,It) | 726,2 → 796,2 | +9,6 % | 700,7 → 754,9 | 1328,7 % |
| MegaCd | Final Fight CD (Europe) | 682,5 → 707,2 | +3,6 % | 611,7 → 654,0 | 1422,9 % |
| MegaDrive | Aladdin (Europe) | 704,0 → 709,2 | +0,7 % | 643,0 → 615,6 | 1426,8 % |
| MegaDrive | Golden Axe (World) | 864,5 → 898,3 | +3,9 % | 734,8 → 746,7 | 1499,1 % |
| MegaDrive | Gunstar Heroes (USA) | 1074,9 → 1098,7 | +2,2 % | 771,7 → 809,1 | 1833,6 % |
| MegaDrive | Sonic The Hedgehog (USA, Europe) | 837,2 → 864,1 | +3,2 % | 688,7 → 730,9 | 1442,0 % |
| MegaDrive | Streets of Rage 2 (USA) | 994,7 → 1008,2 | +1,4 % | 807,1 → 833,9 | 1682,5 % |
| MegaDrive | Thunder Force III (Japan, USA) | 705,9 → 712,6 | +0,9 % | 673,4 → 685,2 | 1189,1 % |
| Sc3000 | 007 James Bond (Japan) (v2.7) (Othello Multivision) | 3959,5 → 4163,9 | +5,2 % | 4542,1 → 4751,2 | 6948,8 % |
| Sc3000 | Champion Tennis (Japan) | 3805,0 → 3740,0 | -1,7 % | 4033,1 → 4099,2 | 6241,3 % |
| Sg1000 | 007 James Bond (Japan) (v2.7) (Othello Multivision) | 3864,3 → 3689,8 | -4,5 % | 4444,3 → 4445,2 | 6157,6 % |
| Sg1000 | Champion Tennis (Japan) | 3657,1 → 4006,4 | +9,6 % | 4196,1 → 4185,5 | 6686,0 % |
| Sg1000 | Congo Bongo (Japan) | 3452,1 → 3831,6 | +11,0 % | 3927,1 → 4475,5 | 6394,2 % |

### Décisions et améliorations restantes

Les 22 médianes Release restent au-dessus de 100 FPS et conservent plus de la moitié de la référence. Elles vont de 692,7 à 4 792,6 FPS avec publication. Les écarts Release vont d’environ −4,5 % à +21,3 %. Les petits écarts ne suffisent pas à attribuer un gain précis à un composant.

Les pertes Debug restent visibles : Sonic Master System −15,4 %, Golden Axe Mega Drive −15,4 % et Sonic Mega Drive −15,7 % dans cette campagne. Les autres cas figurent dans le tableau, gains compris. Le précalcul Z80 améliore plusieurs cas 8 bits par rapport à la première refonte, sans supprimer toutes les pertes de la comparaison complète.

La séparation des puces, des bus et des services est conservée ; aucun retour à l’assemblage monolithique n’est retenu avec les seuils Release demandés. La prochaine amélioration de vitesse doit cibler ces trois cas Debug, distinguer machine et publication, puis vérifier une modification à la fois. Aucun gain d’un composant isolé n’est annoncé à partir de ces FPS de machine.

Les défauts fonctionnels révélés pendant la refonte sont corrigés et retestés. Les limites SC-3000/Pico restent explicites.

### Relevés conservés

- [Comparaison finale détaillée](../../../../artifacts/Emulation/segaemu-refactor/comparison.json)
- [Référence Debug](../../../../artifacts/Emulation/segaemu-refactor/baseline-Debug.json) et [version finale Debug](../../../../artifacts/Emulation/segaemu-refactor/after-Debug.json)
- [Référence Release](../../../../artifacts/Emulation/segaemu-refactor/baseline-Release.json) et [version finale Release](../../../../artifacts/Emulation/segaemu-refactor/after-Release.json)
- [Sélection](../../../../artifacts/Emulation/segaemu-refactor/selection.json), [empreintes des médias](../../../../artifacts/Emulation/segaemu-refactor/media-sha256.json), [sources modifiées](../../../../artifacts/Emulation/segaemu-refactor/source-sha256.json) et [assemblages finaux vérifiés](../../../../artifacts/Emulation/segaemu-refactor/assemblies-final.json)
- Journaux de compilation/exécution sous artifacts/Emulation/segaemu-refactor. Les empreintes CUE identifient les descripteurs, pas chaque piste binaire.
- Projets, sources de banc, copies de référence, binaires de test, états et sessions temporaires supprimés. Les tests autonomes utiles restent dans tests/.

## Complément Pico sur ROM réelles

Les ROM ajoutées après le commit 7583d72ad ont révélé des défauts que le programme synthétique ne pouvait pas montrer : choix régional effectué avant l’identification Pico, bits du registre de version, HINT au mauvais niveau, circuit ADPCM absent et page non sauvegardée. Ces points sont corrigés et les cinq jeux ont été retestés en Debug/Release, avec captures, audio, stylet, pages et non-régression MD/MCD.

Le format Pico devient v4 pour sa page et son contexte audio ; les autres machines gardent v3. Le [bilan Pico détaillé](SegaEmu.md#pico--rom-réelles-et-correctifs) contient les mesures et les limites. La case générale 32X ne signifie pas que SegaEmu implémente cet accessoire ; PicoDrive le déclare séparément.
