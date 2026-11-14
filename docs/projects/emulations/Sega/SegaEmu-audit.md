# Audit SegaEmu et Chips — solutions et effets sur la vitesse

État du 14 novembre 2026, après le commit `d53ae1cf1`.

**Statut après annulation des prototypes — 14 novembre 2026.** Les modifications expérimentales du code, des tests et du générateur ont été annulées. Les routes et mesures décrites dans ce document sont un historique ; elles ne sont plus présentes dans le code de main. Les cases cochées des essais décrivent ce travail passé.

**Nouveau chantier autorisé : refactor-sega-emu.** Refaire la séparation des responsabilités dans SegaEmu et ses puces, avec une configuration explicite et des composants réutilisables, en conservant le fonctionnement existant. La lisibilité et la réutilisabilité guident le découpage ; les mesures restent un contrôle des coûts.

Périmètre : `RetroMediaLab.Emulation.Sega/Emulators/SegaEmu` et les composants de `RetroMediaLab.Chips` utilisés par ce cœur. Les autres émulateurs Sega ne sont pas à modifier. Atari, Amiga et Amstrad servent de comparaison pour l'organisation et l'intégration aux services communs.

Ce document consigne l'audit et les essais du premier sélecteur. Cinq routes du 68000 sont disponibles, avec l'original conservé par défaut. Le précédent document de résultats a été supprimé ; les résultats ciblés de remplacement figurent à la fin de cet audit. Les autres points restent à réaliser.

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

Références : [SegaBoard.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Common/SegaBoard.cs), [SegaBoard.FrameSchedulers.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Common/SegaBoard.FrameSchedulers.cs) et [MegaDriveMachine.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/MegaDrive/MegaDriveMachine.cs).

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

Références : [CartridgeAddressSpace.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Cartridge/CartridgeAddressSpace.cs), [SegaBoard.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Common/SegaBoard.cs) et [SoundUnit.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Common/Audio/SoundUnit.cs).

**Solutions à faire en première approche :**

- [ ] Modifier `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Common/Audio/SoundUnit.cs` et ses fichiers `SoundUnit.Context.cs` et `SoundUnit.Output.cs` : ne créer la FM et ses buffers que pour une configuration qui les utilise ; adapter explicitement réinitialisation et sauvegardes au chemin sans FM.
- [ ] Modifier `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Common/SegaBoard.cs` et `SegaBoard.Media.cs` : créer les buffers des sources CD et cartouche audio lors de leur utilisation, sans ajouter de tests répétitifs par échantillon.
- [ ] Modifier `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Cartridge/CartridgeAddressSpace.cs` et `Common/LoadRom.cs` : étudier une capacité ROM adaptée à la famille, en conservant les zones nécessaires aux BIOS, banques, mappers et cartouches spéciales ; ne pas réduire le tableau sans cet inventaire.

**Gain de vitesse possible.** Candidat favorable pour réduire les allocations, le travail d'initialisation et la mémoire par instance. Un gain de FPS est possible par réduction de la pression mémoire et du GC, mais n'est pas établi. Un objet inutilisé créé une seule fois ne coûte pas nécessairement du temps à chaque trame.

**Risque de perte.** Vérifications de présence répétées dans les boucles audio, premières utilisations plus lentes, erreurs de capacité de ROM ou chargements d'états moins rapides. Les cartouches peuvent utiliser des zones au-delà de leur taille de fichier : le simple redimensionnement au nombre d'octets lus n'est pas une solution sûre.

**Décision pour l'objectif vitesse.** Commencer par les composants réellement inutilisés, en conservant des chemins sélectionnés une fois. Mesurer séparément démarrage, allocations et FPS. Ne pas annoncer une optimisation de FPS si seul le démarrage ou la mémoire s'améliore.

## 8. Finition et comparaison avec Atari, Amiga et Amstrad

**Constat.** L'intégration suit déjà les services communs, mais le code matériel garde beaucoup de noms hérités du C, de conversions répétitives, d'imports inutiles et de contrats définis au milieu des implémentations. Les adaptateurs Atari, Amiga et Amstrad constituent surtout une comparaison des couches d'intégration : leurs wrappers natifs ne prouvent pas la qualité interne d'un cœur C#.

Références : [SystemConfiguration.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Common/SystemConfiguration.cs), [MachineResources.cs](../../../../src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Common/MachineResources.cs), [StellaMachineFactory.cs](../../../../src/RetroMediaLab.Emulation.Atari/Emulators/Stella/Factories/StellaMachineFactory.cs), [AmigaMachineFactory.cs](../../../../src/RetroMediaLab.Emulation.Commodore/Emulators/UAE/Common/Interop/Factories/AmigaMachineFactory.cs) et [Caprice32MachineFactory.cs](../../../../src/RetroMediaLab.Emulation.Amstrad/Emulators/Caprice32/Factories/Caprice32MachineFactory.cs).

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
