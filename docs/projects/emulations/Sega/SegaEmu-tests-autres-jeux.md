# SegaEmu : autres jeux MD, 32X et Mega-CD

Les campagnes des jeux ci-dessus portent sur le commit `a839760df`, avec les DLL publiees dans `build/Debug/Retro Media Lab` et `build/Release/Retro Media Lab`. La comparaison des trois routes à la fin du document a ensuite été exécutée avec les modifications locales non commitées ; ses DLL chargées sont vérifiées par SHA et détaillées dans `artifacts/Emulation/segaemu-autres-jeux/summary.json`.

## Perimetre et protocole

Machine emulee complete : CPU, bus, video, audio PCM et CD selon le modele. Pas de fenetre, limiteur ni peripherique audio hote. Les FPS chronometrent uniquement les appels RunFrame ; chargement et captures hors mesure. Le coeur normal cree par BoardFactory est conserve : aucune substitution de route SH-2 par le banc. Ce n'est pas une mesure du cout de presentation graphique de l'application.

Premiere passe : 1200 trames depuis demarrage, memes entrees Debug/Release, captures aux trames 300/600/900/1200 et FPS par segments de 300 trames. Start pendant cinq trames a 360, 720 et 960 ; droite et impulsions C apres 1000.

Deuxieme passe : 600 trames apres reprise de la sauvegarde 1200 pour les cinq jeux hors Sonic. Start a 180 et 360, droite/C apres 420. Ces sequences peuvent passer par une pause : les FPS sont ceux du passage complet, pas une promesse de performance en combat permanent.

Troisieme passe CD : 600 trames apres reprise a 1800. Final Fight sans nouvel appui Start, boutons B et directions ; Dragon Lair avec une seule impulsion Start initiale de depause puis B/directions. Captures a 2100/2400. Les transitions noires initiales ont ete suivies jusqu'au jeu visible.

Regles regionales : cadence observee proche de 59,92 Hz pour Sonic 2, Streets 2, Virtua Fighter et Dragon Lair ; 49,70 Hz pour After Burner Europe et Final Fight Europe. BIOS CD selectionne sur l'identifiant regional demande : Mega-CD Europe v1.00 pour Final Fight, Mega-CD 2 Japan v2.00C pour Dragon Lair. Les chemins et SHA256 exacts figurent dans chaque JSON.

Les titres CD sont sans extension 32X. Aucun nouveau CD32X n'a ete suppose disponible ; Corpse Killer est le titre deja teste, non repete dans cette campagne de nouveaux jeux.

## Demarrage et 1200 premieres trames

| Machine | Jeu | Debug FPS | Release FPS | Derniere scene observee |
| --- | --- | ---: | ---: | --- |
| Mega Drive | Sonic The Hedgehog 2 (World) | 35,35 | 414,82 | Emerald Hill, Sonic et Tails visibles en jeu |
| Mega Drive | Streets of Rage 2 (USA) | 34,42 | 526,83 | Selection des personnages |
| 32X | After Burner Complete (Europe) | 12,72 | 87,94 | Decollage sur porte-avions |
| 32X | Virtua Fighter (JU) | 17,86 | 65,38 | Titre et menu Arcade, puis transition noire a 1200 |
| Mega-CD | Final Fight CD (Europe) | 30,69 | 311,82 | BIOS, logos et transition noire a 1200 |
| Mega-CD | Dragon Lair (Japan) | 32,86 | 208,80 | Selection de langue puis logo ReadySoft |

Ces moyennes incluent logos, menus et transitions. Les ecrans noirs ne sont pas presentes comme une preuve de jeu fonctionnel.

## Passages prolonges avec jeu visible

| Jeu | Trames absolues du passage | Debug FPS | Release FPS | Observation finale |
| --- | --- | ---: | ---: | --- |
| Streets of Rage 2 | 1201–1800 | 34,28 | 232,30 | Axel, ennemis et premier niveau visibles |
| After Burner Complete | 1201–1800 | 18,11 | 63,82 | Avion en vol, mer et ennemis visibles |
| Virtua Fighter | 1201–1800 | 20,19 | 57,31 | Combat Akira/Jacky, decor et polygones visibles |
| Final Fight CD | 1801–2400 | 24,16 | 224,25 | Guy et ennemis visibles dans le niveau |
| Dragon Lair | 1801–2400 | 29,95 | 226,43 | Animation du chevalier apres depause |

Virtua Fighter reste insuffisamment rapide sur ce passage Release : 57,31 FPS moyens pour 59,92 nominaux ; segment de 300 trames a 51,87 FPS. After Burner n'a que 63,82 FPS moyens en Release sur le passage prolonge, segment minimal 61,88 pour 49,70 nominaux. Ces deux jeux confirment que la marge 32X reste faible ; les chiffres des menus ne doivent pas masquer ces mesures.

Pour Final Fight, le passage intermediaire 1201–1800 mesure 13,04 FPS Debug / 243,98 Release et aboutit encore a une transition ; la derniere prolongation confirme le niveau. Dragon Lair intermediaire mesure 27,72 / 266,48 et aboutit a une scene en pause ; la derniere prolongation la depause et montre une autre image. Toutes ces mesures restent dans les JSON.

## Images et reprise d'etat

Toutes les captures correspondantes des passages mesures sont identiques entre Debug et Release. Le controle visuel couvre lancement et scenes ci-dessus, pas une partie complete ni tous les controles/peripheriques.

Erreur corrigee dans le banc : le premier controle relachait les boutons avant la trame attendue, puis LoadState restaurait les boutons sauvegardes pour la trame comparee. Ses `stateExact=false` ne constituent pas des echecs de l'emulateur a entrees identiques. Les FPS et captures restent valides car releves avant ce controle.

Controle corrige separe, sans remesure de vitesse : charger chacune des six sauvegardes initiales, avancer 16 trames pour reconstituer un fonctionnement avant sauvegarde, sauvegarder, comparer une trame continue a une trame rechargee avec les memes boutons relaches dans les deux cas. Dans les douze cas Debug/Release : etat interne et image identiques. Les boutons sauvegardes/restaures valent 40, puis la meme valeur 0 est appliquee a chaque trame comparee.

Le premier bloc PCM apres restauration reste different dans les douze cas : 736 contre 735 echantillons NTSC, 888 contre 887 en PAL. L'exactitude audio de la reprise n'est donc pas validee. Les echantillons PCM non nuls sont releves pendant tous les passages ; cela ne constitue pas une ecoute ni une validation de la qualite sonore.

## Chargement du banc et preuves

Le module publie reference des dependances 2.0.0.0, tandis que les bibliotheques communes publiees portent 0.4.1.0. Un lancement direct du banc sans resolver echouait avant toute trame. Le banc reprend le principe de resolution explicite par nom du lanceur, avant compilation de sa methode utilisant Sega : les DLL publiees ne sont ni remplacees ni reconstruites. Cet incident du banc ne constitue pas un echec de demarrage des jeux dans l'application.

Resultats, segments, controles corriges, empreintes des DLL/etats/captures brutes : `artifacts/Emulation/segaemu-autres-jeux/summary.json`. Details par jeu : JSON et journaux dans le meme dossier. Planches PNG : `<jeu>-<configuration>.png`, `gameplay-<jeu>-<configuration>.png`, `confirm-<jeu>-<configuration>.png` ; vues d'ensemble `overview-*.png`. Les donnees brutes et le nouveau banc temporaire sont supprimes apres conservation des empreintes. Aucun commit de ces resultats effectue.

## Complément : Metal Head et Tempo sur 32X

Ces deux cartouches ont été testées avec le `BoardFactory` et les DLL publiées Debug/Release du commit `a839760df`. Les SHA des DLL effectivement chargées sont vérifiés contre les sorties de l’application dans les JSON. Le 32X est actif sur les quatre passages.

| Jeu | Région / cadence | Debug FPS | Release FPS | Échantillons audio produits | Scène à 600/1200 |
| --- | --- | ---: | ---: | ---: | --- |
| Metal Head | PAL / 49,70 Hz | 18,37 | 112,31 | 1 064 757 | Tampon noir opaque aux deux captures |
| Tempo | NTSC / 59,92 Hz | 28,36 | 170,45 | 883 137 | Tampon noir opaque aux deux captures |

Les captures des huit passages (deux trames, deux jeux, deux configurations) ont toutes le SHA brut `A68935780D3A4131C13783B40B2E1EBF1D1A773B4BD6F993097D46523EDA2BF6`. Les composantes RGB sont nulles; ce n’est pas un problème de transparence ni un PNG noir produit par la conversion. Les deux images de chaque jeu sont identiques. Le 32X s’initialise et `RunFrame` avance, mais aucun écran de jeu n’est visible : ces FPS décrivent le débit de la boucle machine, pas un démarrage validé de ces deux jeux. La cause de l’image noire reste à établir. Le nombre d’échantillons retournés n’est pas une validation audible du son.

Le chemin de capture a été vérifié dans les sources : `SegaBoard` conserve le tableau fourni par `MachineResources` et passe ce même tableau au constructeur de `Vdp`; `Vdp.Output.cs` remappe les lignes directement dans ce tableau. Les captures ne lisent donc pas un tampon vidéo distinct.

Deux défauts du banc ont été corrigés avant ce relevé : les quatre premiers processus `dotnet` ont échoué avant la création des cartes, faute de résolution de `retromedialab.chips`; le résolveur charge désormais les DLL publiées correspondantes et les journaux de ces plantages sont conservés. Un appel PowerShell `Add-Member` empêchait aussi l’écriture du résumé après les premiers passages; son erreur n’a pas interrompu les trames mais le cumul a été corrigé. Le premier essai avait `Video.render=0`; il a été conservé dans `attempts/render-disabled`. Un second passage avec `Video.render=1` reste noir lui aussi. Dans le VDP, `render` ne commande pas l’activation du rendu normal : il choisit le placement des lignes entrelacées. Je n’attribue donc pas l’écran noir à cette option.

Les mesures et SHA se trouvent dans `more32x-{metalhead,tempo}-{Debug,Release}.json`. Les captures sont les fichiers `more32x-{metalhead,tempo}-{Debug,Release}-{600,1200}.png`; les captures BGRA brutes seront retirées après les essais CD32X.

## Extension : Fahrenheit et Night Trap sur Mega-CD + 32X

Chaque titre a été lancé avec ses deux images, un BIOS Mega-CD de la même région, 32X activé et un changement de disque à la trame 600. Les captures sont prises à la trame 600 avant le changement et à la trame 1200 après 600 trames sur le disque 2. Les impulsions de manette du banc sont conservées dans les JSON; elles n’ont pas amené ces scènes jusqu’au jeu visible.

| Jeu | Région / cadence | Debug FPS | Release FPS | BIOS chargé | Scène à 600 / 1200 |
| --- | --- | ---: | ---: | --- | --- |
| Fahrenheit (USA), Key Disc puis 32X CD | NTSC / 59,92 Hz | 30,52 | 246,58 | Sega CD USA v1.00, SHA `779B09A46C25AE2B1493E0EC7E1AAF4B5963AA2E13B3AA1378B2CE5CCCCCA2E9` | Logo Sega aux deux trames; aucun écran de Fahrenheit visible |
| Night Trap (Europe), disques 1 puis 2 | PAL / 49,70 Hz | 23,21 | 146,68 | Mega-CD Europe v1.00, SHA `16735DF92FCC1D0FC6A1ECED244867E95281C5006340926082B74E363E073DC4` | Logo Sega à 600, noir à 1200; jeu non confirmé |

Les quatre changements de disque ont été enregistrés à la trame 600 avec les SHA des CUE source. Le 32X est actif sur tous les passages et les quatre lancements Debug/Release se terminent sans plantage dotnet. Les résultats positifs sont l’amorçage du BIOS régional et l’exécution du chemin Mega-CD + 32X; les limites sont l’absence d’une scène de jeu visible pour Fahrenheit et le retour au noir de Night Trap après changement de disque. Les FPS Release dépassent 100 sur ces passages, mais ne mesurent pas une partie visible. Les trames audio retournées ne valident pas la sortie sonore à l’écoute.

Les détails, SHA des deux disques, BIOS, DLL, captures et changements de disque sont dans `more32xcd-{fahrenheit,nighttrap}-{Debug,Release}.json`. Les PNG correspondants sont `more32xcd-{fahrenheit,nighttrap}-{Debug,Release}-{600,1200}.png`. Aucun plantage dotnet n’est survenu dans cette phase.

## Comparaison des voies Interpréteur, Dynarec et GPU

Après le retrait de la variante CIL fautive ci-dessous, Interpréteur et Dynarec ont été relancés avec les DLL Sega/Chips fraîchement construites. After Burner, les entrées, 1 200 trames, les quatre segments et les SHA DLL sont contrôlés par le banc. Le Dynarec active `UseCompiledBlocks` sur Master et Slave; la référence reste l’interpréteur par défaut.

| Configuration | Interpréteur | Dynarec existant | Variation Dynarec | Segments Interpréteur | Segments Dynarec |
| --- | ---: | ---: | ---: | --- | --- |
| Debug | 18,39 FPS | 12,42 FPS | −32,4 % | 18,98 / 18,03 / 18,32 / 18,24 | 10,78 / 12,65 / 13,93 / 12,75 |
| Release | 83,10 FPS | 29,83 FPS | −64,1 % | 65,64 / 93,08 / 88,12 / 92,49 | 19,35 / 32,70 / 45,96 / 33,22 |

Les deux SH-2 compilent 3 762 blocs Master et 1 584 Slave. Le temps de compilation cumulé est de 14,72 s en Debug et 16,84 s en Release. Les captures brutes aux trames 600 et 1 200 ont le même SHA entre les deux voies, en Debug comme en Release. Aucun des quatre processus de cette comparaison ne s’est terminé en erreur. Sur ce jeu et ces DLL, le Dynarec existant perd du débit dans les deux configurations; Release est aussi sous 100 FPS pour les deux voies. Il ne constitue pas un gain à activer.

### Plantages dotnet rencontrés pendant l’essai

L’essai de mise en cache des registres dans le CIL du Dynarec a fait échouer la préparation du premier bloc avec `InvalidProgramException` dans `RuntimeHelpers.PrepareDelegate`, en Debug et Release. Aucun FPS n’a été produit pour cette variante. J’ai retiré cette émission CIL; `Sh2.Blocks.cs` est revenu exactement à sa version avant cet essai. Après reconstruction, l’Interpréteur et le Dynarec antérieur ont tous deux terminé les quatre passages sans erreur. Les anciens journaux de cette tentative restent dans `attempts/register-local/` pour que l’échec ne soit pas confondu avec le test corrigé.

Un autre défaut antérieur du banc était le chargement d’une DLL `retromedialab.chips` introuvable quand l’assembly resolver n’était pas installé avant la compilation de la méthode de test. Le banc résout maintenant les DLL publiées par leur nom et vérifie leur SHA; cette comparaison n’a pas reproduit ce plantage de lancement.

### Voie GPU : état avant le nouveau banc

À cette étape, aucune route GPU n’avait été exécutée : `MachineFactory` créait `ManagedCore` sans contexte graphique et `Vdp.RemapLineCore` rasterisait toujours sur le CPU. Cette limite est historique pour le test de composition 32X ajouté ci-dessous : elle ne signifie pas qu’un rasteriseur VDP complet a été déplacé vers le GPU.

Résultats historiques et SHA : `artifacts/Emulation/segaemu-autres-jeux/summary.json` et `attempts/register-local/more32xroute-afterburner-{Interpreter,Dynarec}-{Debug,Release}.json`.

### Nouveau passage : Interpréteur, Dynarec et composition GPU 32X

Les trois routes ont été lancées sur After Burner Complete (Europe), 1 200 trames, quatre segments, mêmes entrées et même contenu. Les mesures ont été faites d’abord en Debug, puis en Release. Les DLL Chips/Sega chargées correspondent aux builds courants ; SHA Debug `48DBE35A818B545B62A1BF1884CA68533CA789B2810AEC2E4007114F2DBE8CC5` / `C400FA7242FF6A0392A6AB245F8C1A012317F1A6269666759F6360D162F615FE`, Release `64ABB58FBC57C2216A374F0ADE7BA8608A188878D098537ED46569AC9289A29A` / `6D4DCED80BEFE91585C50AD14E2C5B8BCEDE1E998DA0B81B9AD7D741652BC50D`. Les six processus ont fini sans plantage dotnet ni erreur de validation.

| Configuration | Route | FPS moyens | Variation vs interpréteur | FPS des 4 segments |
| --- | --- | ---: | ---: | --- |
| Debug | Interpréteur | 17,43 | référence | 17,87 / 17,74 / 17,04 / 17,09 |
| Debug | Dynarec SH-2 | 10,86 | −37,7 % | 8,60 / 11,59 / 12,11 / 11,99 |
| Debug | Composition GPU 32X | 17,49 | +0,4 % | 18,38 / 17,88 / 16,69 / 17,12 |
| Release | Interpréteur | 85,91 | référence | 76,41 / 93,92 / 84,98 / 90,40 |
| Release | Dynarec SH-2 | 29,62 | −65,5 % | 19,34 / 32,54 / 46,15 / 32,34 |
| Release | Composition GPU 32X | 65,62 | −23,6 % | 66,90 / 68,37 / 63,71 / 63,74 |

Le Dynarec a réellement compilé des blocs sur Master et Slave (3 762 / 1 584), sans repli pendant les quatre passages. Le coût cumulé de génération a été de 17,13 s en Debug et 16,87 s en Release. Malgré cela, il perd du débit sur ce scénario et n’est pas à activer.

La route GPU ne remplace que le mélange final de la couche 32X : le VDP Mega Drive reste rasterisé par le CPU, les couleurs/priorités sont transférées au shader, puis le résultat est relu vers le framebuffer CPU. Les 1 138 trames composées couvrent 253 947 lignes, sans ligne non prise en charge. Le travail OpenGL mesuré, readback inclus, est de 5,05 s en Debug et 4,23 s en Release. Cette voie ne donne pas de gain Release et coûte 23,6 % de FPS ; elle ne doit pas remplacer le chemin CPU actuel. Le +0,4 % Debug est trop faible pour conclure à un gain.

Les SHA des captures brutes aux trames 600 et 1 200 sont identiques entre l’interpréteur, le Dynarec et la composition GPU, en Debug comme en Release. Les douze PNG correspondants sont conservés dans `attempts/three-routes-retest/`. Le premier contrôle de contexte GPU avait rejeté à tort le type OpenGL annoncé par l’interface commune ; le constructeur accepte maintenant OpenGL version 3+ tandis que le banc demande toujours Core 3.0.

Le repli `InvalidProgramException` ajouté dans `Sh2.Blocks.cs` n’a pas été déclenché dans ces essais. Il protège seulement la préparation de blocs et désactive les blocs compilés sur le SH-2 concerné ; l’interpréteur par défaut reste inchangé. Les anciennes tentatives fautives et leurs plantages restent dans `attempts/register-local/`. Résultats complets, segments, SHA, captures et erreurs : `artifacts/Emulation/segaemu-autres-jeux/summary.json` et `attempts/three-routes-retest/more32xroute-afterburner-{Interpreter,Dynarec,GpuComposition}-{Debug,Release}.json`.

### Re-test des trois voies et comparaison du readback OpenGL

J’ai relancé les trois routes sur After Burner Complete, avec les DLL courantes, 600 trames et une capture à la trame 600, d’abord en Debug puis en Release. Les anciennes captures historiques avaient des SHA de DLL différents ; elles ne servent donc pas de référence à ce passage. L’interpréteur a été relancé avec les DLL du nouveau banc et sert de référence pour les deux autres routes.

| Configuration | Interpréteur | Dynarec SH-2 | Variation DRC | Composition GPU | Variation GPU | Travail OpenGL |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | 18,64 FPS | 11,20 FPS | −39,9 % | 17,10 FPS | −8,3 % | 2,45 s |
| Release | 81,42 FPS | 24,62 FPS | −69,8 % | 66,15 FPS | −18,8 % | 1,58 s |

Les six captures ont le même SHA BGRA `7FBD59F726F8BA5EEE4AE6D46661182607AC5C76910C31DC1572A953FA9748FA` et le même SHA PNG `1DE22870C71736D39FF099BE958217AF63B9458697429B17883A08EE0B696EEF`. Les huit processus du passage (six routes et deux contrôles A/B) se sont terminés normalement, sans plantage dotnet. Le banc a d’abord rejeté la capture historique car elle venait d’un autre SHA de DLL ; la référence a été refaite sur les DLL courantes. Une première compilation du banc a aussi signalé un namespace incorrect de `ModelConstants` (`CS0103`), corrigé avant les exécutions ; ce n’était pas un plantage du cœur.

Pour mesurer l’effet propre à la modification, j’ai temporairement rétabli `glFinish` juste après `glReadPixels`, puis comparé les deux versions avec la même ROM, la même build et 600 trames. Le readback client est identique dans les deux cas : SHA BGRA identique.

| Configuration | Sans `glFinish` | Avec `glFinish` | Gain sans l’attente | Travail OpenGL sans / avec |
| --- | ---: | ---: | ---: | ---: |
| Debug | 17,10 FPS | 14,52 FPS | +17,7 % | 2,45 / 3,39 s |
| Release | 66,15 FPS | 47,25 FPS | +40,0 % | 1,58 / 2,50 s |

La suppression de l’attente redondante est conservée : le test A/B montre un gain dans les deux configurations sans changement d’image. À cette étape, la voie GPU était plus lente que l’interpréteur complet sur ce jeu ; seul le mélange final 32X était rendu par le GPU et le framebuffer était relu en mémoire CPU. Le retest après réduction du rectangle est consigné ci-dessous. Le Dynarec existant reste plus lent.

Résultats, SHA des DLL, JSON et journaux : `artifacts/Emulation/segaemu-autres-jeux/attempts/gl-finish-removal/`. Les PNG des captures sont conservés dans ce dossier ; les tampons BGRA temporaires ont été supprimés après vérification.

### Retest après réduction du rectangle de transfert GPU

J’ai réduit la zone téléversée et relue de l’image complète (1024×626) au rectangle réellement composé par le 32X (320×224 dans ce passage). Les deux textures sources gardent leur véritable pas de ligne grâce à `GL_UNPACK_ROW_LENGTH`; le shader et le readback ne couvrent plus que ce rectangle. Le VDP CPU, le rendu par défaut et la route interprétée restent inchangés.

Même After Burner Complete Europe (SHA ROM `78E4E6ED0DCC2B56018B5F7B7E6758208D1D29355524878829389906624C36A1`), 600 trames par route, mêmes entrées, deux segments de 300 trames, d’abord Debug puis Release. Les trois routes utilisent les DLL construites pour leur configuration.

| Configuration | Interpréteur | Dynarec SH-2 | Variation Dynarec | Composition GPU | Variation GPU | Ancienne composition GPU |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | 16,29 FPS | 10,62 FPS | −34,8 % | 19,14 FPS | +17,5 % | 17,10 FPS |
| Release | 87,77 FPS | 24,78 FPS | −71,8 % | 86,06 FPS | −1,9 % | 66,15 FPS |

La composition GPU gagne 11,9 % en Debug et 30,1 % en Release face à son ancienne version. Le temps mesuré à l’intérieur du compositeur passe de 2,45 à 0,45 s en Debug et de 1,58 à 0,36 s en Release. Les compteurs calculés sur trois transferts (deux uploads et un readback) passent de 1 101 920 256 pixels équivalents si les trois couvrent l’image entière à 122 855 040 pour le rectangle ; cela représente 88,9 % de données logiques en moins. Ce compteur décrit les dimensions demandées à OpenGL, pas une mesure du trafic physique du pilote.

La correction rend la candidate GPU plus rapide en Debug que l’interpréteur sur ce passage. En Release, elle reste 1,9 % derrière : le résultat ne prouve donc pas encore un gain global Release. L’image BGRA est identique sur les six exécutions (SHA `7FBD59F726F8BA5EEE4AE6D46661182607AC5C76910C31DC1572A953FA9748FA`), sans ligne non prise en charge. Les six processus terminent normalement. Le dynarec reste très en retrait et n’est pas amélioré par ce changement.

La composition demeure un prototype hors du chemin de présentation normal : le rendu VDP Mega Drive est encore CPU et `glReadPixels` ramène le résultat sur le CPU. Les relevés JSON, segments, compteurs et SHA des DLL sont dans `artifacts/Emulation/segaemu-autres-jeux/attempts/gpu-rectangle-retest/`; le harnais temporaire a été supprimé après conservation des captures PNG et des relevés.

## Portée des trois routes demandées pour toute la machine 32X

### Reprise du backend Dynarec 68000 à partir de BlastEm

La collecte locale BlastEmWhole contenait surtout les composants SG-1000/Z80 et ne contenait pas les sources du générateur 68000. Treize fichiers 68000/backend/émetteur x86 ont été ajoutés dans `artifacts/References/Machines/Sega/MegaDrive/BlastEmDynarec/Source`, depuis l’arbre Git `ec73825ece2a134beb7825a5fd6fd21a398ff12e` déjà référencé. `source-index.json` enregistre les URL de blobs, SHA Git et SHA256. Les autres collectes restent intactes.

Premier composant porté : `M68000NativeCodeMap`, selon `m68k_core.c/get_native_address`, `get_instruction_start` et `map_native_address`, avec les 65 536 emplacements de pages de 256 octets définis dans `m68k_core.h`. Les pages sont créées à la demande, conservent une base native et des offsets, marquent les octets d’extension, préservent les entrées préexistantes pour le code chevauchant et utilisent une normalisation d’alias fournie par le bus. Ce composant emprunte les pointeurs natifs ; leur allocation/libération reste au propriétaire du code. Clear ne libère que les métadonnées.

Les 20 contrôles passent en Debug et Release : adresse absente, traversée de page, extensions sans entrée exécutable, départ retrouvé, instruction chevauchante, rebouclage 24 bits, alias et remise à zéro. Résultats `attempts/m68000-native-map/{Debug,Release}.json`. Le harnais autonome a été supprimé. Cette table n’est pas encore raccordée au générateur de code 68000 et ne constitue pas un Dynarec exécutable ni un gain FPS. La boucle Run de référence reste identique à sa version restaurée.

**Priorité révisée explicitement par l’utilisateur : MD et Mega-CD sans 32X d’abord.** Les routes Dynarec et interpréteur GPU doivent dépasser l’interpréteur de référence sur ces deux machines avant de reprendre le 32X. Chaque comparaison devra remesurer aussi l’interpréteur sur les mêmes médias, entrées, chauffe et durée, Debug puis Release. L’extension SH-2 native au drapeau T entamée après la restauration MD/MCD a été retirée avant validation ; la compilation Chips Release du retour à la version précédente réussit. Aucun travail SH-2 supplémentaire n’est prioritaire avant cette validation MD/MCD. Les sections 32X ci-dessous conservent les résultats historiques.

### Contrôle Mega Drive et Mega-CD sans 32X

### Restauration des boucles interprétées et protocole historique

Les méthodes Run du 68000 et du Z80 correspondent maintenant exactement aux fichiers Git de référence (`git diff --numstat` vide sur les deux fichiers). Les tests de sélection Dynarec ajoutés dans ces méthodes ont été retirés. La route expérimentale est appelée explicitement via RunDynamic depuis le raccordement SegaEmu, y compris pour le sous-68000 Mega-CD. Le cache Z80 paginé a été retiré : il n’a pas réduit les nombres de blocs sur les jeux et la passe Release a donné 14,63 FPS Aladdin / 35,87 Batman, contre 19,56 / 43,94 avant cette candidate. Le dictionnaire avec invalidation ciblée est restauré.

Le Runner historique conservé sous `segaemu-32x/.work` a été réutilisé, sans RefreshConfiguration ajouté : mêmes ROM/CUE Europe, BIOS CD Europe v1.00, entrée repeat-start, 60 trames de chauffe puis 180 Debug ou 900 Release. Dernière passe avec boucles Run restaurées :

| Interpréteur | Ancienne référence Debug | Debug restauré | Ancienne référence Release | Release restauré |
| --- | ---: | ---: | ---: | ---: |
| Aladdin Europe | 28,90 | 33,43 | 590,60 | 618,88 |
| Batman Returns Europe | 25,79 | 30,59 | 328,28 | 338,77 |

Les SHA des images Release aux trames 600/960 correspondent aux relevés historiques. L’ancienne paire de DLL encore disponible (SHA Chips E5569F0F..., Sega 627261CF..., différente de la paire F065EDC1.../4D4F4630... des notes) a également été exécutée sur ce même protocole et donné 524,76 / 414,71 FPS. La première passe actuelle avant restauration exacte de Run donnait 501,25 / 359,85. Ces chiffres sont conservés distinctement, sans prétendre isoler toute cause des variations entre DLL ou passages. Les FPS des notes sont retrouvés dans la dernière passe, mais ce n’est pas une validation de tous les jeux ni des prototypes.

Le contrôle historique de reprise garde ses défauts visibles : audioExact=false pour ces passages, et stateExact=false pour Batman Debug à la trame 240 ; stateExact=true pour les deux jeux Release à 960 et pour Aladdin Debug. Les images après reprise correspondent. Ces anomalies ne sont pas masquées par les FPS retrouvés. Relevés `attempts/no32x/{restored,preserved,exact}-*.json`, SHA DLL inclus, et `summary.json/restoredInterpreterHistoricalProtocol`. Les nouveaux harnais et leurs fichiers `.state` temporaires ont été supprimés après conservation des JSON ; le banc historique préexistant reste intact.

Deuxième correction du cache Z80 : suppression du vidage global à 4 096 traces, remplacé par une table de 256 pages de 256 adresses, créées uniquement à la demande. L’espace 16 bits du Z80 borne naturellement le nombre d’entrées à 65 536, une trace courante par adresse. La vérification des opcodes et l’invalidation ciblée sont conservées. Le test compile 4 100 adresses distinctes puis revient à la première : le compteur de compilations reste inchangé, même après modification de l’une des traces et retour à une autre. Les 4 103 états comparés à l’interpréteur concordent en Debug et Release. Relevés `attempts/no32x/paged-{Debug,Release}.json`. Cette correction n’a pas encore été remesurée sur les jeux ; les FPS de l’invalidation ciblée ci-dessous sont antérieurs au cache paginé. Harnais de contrôle supprimé après consignation.

Correction suivante : les traces 68000 et Z80 transmettent maintenant leur adresse de départ à leur contrôle d’opcode. Un opcode différent retire seulement la trace exécutée, au lieu de vider tout le cache. Les autres traces continuent de vérifier leurs propres opcodes avant toute exécution. Le contrôle autonome à deux traces confirme, en Debug et Release pour chaque CPU, que modifier la première ne recompile pas la seconde (compteur inchangé à trois blocs), avec contextes et mémoires identiques à l’interpréteur. Relevés `attempts/no32x/selective-{Debug,Release}.json`.

Retest des mêmes 600 trames, mode compilé uniquement, comparé aux relevés précédents : Aladdin Debug 27,52 → 30,99 FPS, Release 8,63 → 19,56 ; Batman Returns Debug 27,24 → 29,59, Release 38,66 → 43,94. Les SHA état/image/audio aux trames 300/600 restent identiques aux relevés précédents, lesquels correspondaient à l’interpréteur. Aladdin construit maintenant 5 876 blocs Z80 contre 27 430, avec 984 blocs 68000 inchangés. La perte Release face à l’interpréteur reste majeure : cette correction ne valide pas la route compilée comme accélération de remplacement. Les caches conservent encore un vidage global à leur limite de 4 096 entrées ; ce second mécanisme reste à examiner. JSON `attempts/no32x/selective-{Debug,Release}-{MegaDrive,MegaCd}.json` et synthèse `summary.json/selectiveInvalidation`. Les deux harnais temporaires ont été supprimés après consignation.

Demande utilisateur : vérifier également les machines sans 32X dans les modes disponibles. Aladdin Europe sur Mega Drive et Batman Returns Europe sur Mega-CD ont été exécutés 600 trames depuis chargement, sans entrée, limiteur ni fenêtre. Les CPU sont tous en interpréteur ou tous en blocs compilés ; le sous-68000 CD est inclus. Has32X est contrôlé faux et les sélections des CPU présents sont contrôlées. BIOS CD Europe v1.00 ; chemins, SHA BIOS/médias/DLL et compteurs sont conservés dans `attempts/no32x/{Debug,Release}-{MegaDrive,MegaCd}-{false,true}.json` et `summary.json/no32XComparison`.

| Jeu sans 32X | Debug interpréteur | Debug compilé | Release interpréteur | Release compilé |
| --- | ---: | ---: | ---: | ---: |
| Aladdin Europe | 28,91 FPS | 27,52 FPS | 262,74 FPS | 8,63 FPS |
| Batman Returns Europe | 27,90 FPS | 27,24 FPS | 194,53 FPS | 38,66 FPS |

Aux trames 300/600, les SHA des états complets, images et audio cumulé correspondent entre routes dans chaque configuration. Les valeurs concordent aussi entre Debug et Release pour ces mêmes captures. Aucun des huit processus ne termine en erreur. Cela ne prouve pas une absence de tout défaut sur tous les jeux, et ces passages depuis démarrage ne constituent pas une campagne de gameplay prolongé. Les FPS actuels ne sont pas présentés comme une comparaison à une ancienne DLL de référence.

La route compilée présente une régression majeure Release et ne doit pas être activée par défaut. Sur Aladdin, elle construit 984 traces 68000 et 27 430 traces Z80 dans les deux configurations. Le code Z80 invalide tout son cache lorsqu’une instruction change ; cette invalidation globale peut provoquer des recompilations d’autres traces, mais sa part dans la durée n’a pas été isolée. Ce mécanisme doit être corrigé et remesuré. Les options de suites natives SH-2 et de composition vidéo 32X ne s’appliquent pas à ces machines ; aucun mode GPU complet n’est encore implémenté. Le harnais a été supprimé après conservation des relevés.

### Sources natives manquantes désormais récupérées

Intégration suivante : la suite x64 est désormais intégrée à la méthode CIL du bloc complet au lieu de constituer un bloc tronqué qui retourne immédiatement au dispatcher. Le reste des lectures, branches et liens conserve son chemin existant. Les allocations natives sont transférées au bloc seulement après compilation et capture des versions réussies, avec libération dans finally en cas d’échec. Contrôle ciblé : 400 comparaisons de contexte complet et cycles en Debug, puis 400 en Release, avec cœur ordinaire/CreateNative, attentes de fetch 0/2, budgets irréguliers, opcode modifié et lecture mémoire après le préfixe natif. Les quatre variantes concordent avec l’interpréteur ; 693 exécutions natives sans attente, 240 avec deux cycles d’attente pour chaque cœur/configuration. Relevés `attempts/sh2-x64-emitter/combined-{Debug,Release}.json`. Le harnais est supprimé après consignation. Ces tests ne mesurent pas les FPS de cette nouvelle intégration sur la machine 32X.

Contrôle sur le vrai bus 32X : After Burner Europe, 600 trames, quatre CPU en blocs, avec suites natives SH-2 désactivées puis activées. Debug : 10,8494 → 10,8596 FPS (+0,09 %, pas un gain établi). Release : 15,4635 → 16,0671 FPS (+3,90 % observés sur une seule passe). Les suites x64 ont été exécutées 851 445 fois dans chaque configuration (204 218 Master / 647 227 Slave). Les SHA de l’état complet, de l’image et de l’audio cumulé correspondent aux trames 300 et 600. Aucun des quatre processus ne termine en erreur. Les JSON détaillent les segments, les délais totaux, les blocs et SHA DLL : `attempts/sh2-x64-machine/{Debug,Release}-{Cil,X64}.json`, également regroupés dans `summary.json/sh2X64Machine`. Le harnais a été supprimé après conservation des résultats.

Ce résultat valide l’exécution des suites natives avec les vrais accès et synchronisations de ce passage. Il ne couvre pas tous les événements possibles et ne prouve pas une absence générale de régression. L’émission native ne traite encore que certaines opérations de registres en début de bloc ; le reste du bloc utilise encore le chemin compilé CIL. Les frontières de blocs et le coût de cette coexistence restent à traiter. Ces FPS restent très inférieurs à l’interpréteur : la voie native ne devient pas le défaut et l’objectif de vitesse n’est pas atteint.

Raccordement suivant : `Sh2.UseX64RegisterBlocks`, false par défaut, permet aux blocs de démarrer par une suite native d’au moins deux instructions prises en charge. La suite est bornée au mapping direct de fetch disponible, validée par les versions de code existantes et exécutée seulement si le budget et la prochaine échéance autorisent toute la suite. Les délais de fetch sont comptabilisés avec le délai d’entrée distinct. Sinon le bloc exécute une instruction par le chemin ordinaire. Les registres sont épinglés par `fixed` uniquement pendant l’appel natif. Les blocs actifs et inactifs sont libérés lors du vidage du cache ; les variantes définitivement évincées sont aussi libérées. `Mega32X.Dispose` vide les caches des deux SH-2 dans un try/finally et `SegaBoard.Dispose` appelle cette libération avant son nettoyage final.

Contrôle ciblé Debug et Release : cœur générique ordinaire et cœur généré par `CreateNative`, 100 budgets irréguliers chacun, contextes complets et horloges de bus identiques à l’interpréteur. La modification d’un opcode au milieu de la campagne reste prise en compte ; chaque cœur exécute effectivement 922 suites natives. Relevés : `attempts/sh2-x64-emitter/integration-{Debug,Release}.json`. Ces résultats ne couvrent pas encore les IRQ/échéances avancées sur le vrai bus 32X ni les FPS machine. Le sélecteur natif n’est pas activé par le choix commun des quatre CPU et reste indépendant pour la comparaison expérimentale. Le harnais ciblé a été supprimé après consignation.

Premier composant natif ajouté : `CPU/Hitachi/SH2/Compilation/Sh2X64RegisterBlock.cs`, Windows x64 seulement. Les encodages ModRM/REX reprennent `emit_x86.c`. Le contexte du fichier de registres entre par RCX selon l’ABI Windows ; jusqu’à six registres invités sont chargés dans EAX, EDX, R8D–R11D, conservés pendant la suite puis écrits en sortie. Les opérations actuellement portées sont MOV immédiat/registre, ADD immédiat/registre, SUB, AND, OR, XOR et NOP ; les instructions à drapeaux, accès bus et contrôle sont refusées, ainsi que les suites nécessitant plus de six registres invités. La mémoire est allouée RW, copiée, passée en RX et le cache d’instructions est synchronisé ; Dispose la libère. Le constructeur libère aussi après erreur. L’appelant possède et maintient le fichier de registres épinglé pendant l’appel.

Validation autonome : 512 suites de 32 instructions en Debug puis 512 en Release (16 384 instructions par configuration), initialisations pseudo-aléatoires déterministes, valeurs immédiates 0–255 interprétées comme octets signés, registres invités R10–R15 et contrôle de tous les R0–R15. Les résultats natifs correspondent à l’interpréteur. Les tests vérifient aussi le refus d’ADDC et d’une lecture mémoire. Tous les blocs sont libérés et les tableaux désépinglés dans finally. Relevés : `attempts/sh2-x64-emitter/{Debug,Release}.json`. Ce composant n’est pas encore raccordé au scheduler ni au cache de blocs SH-2 : il ne représente pas encore une amélioration FPS machine ni un backend complet. Le harnais autonome est supprimé après consignation.

Après la perte mesurée avec tous les CPU compilés, les deux fichiers PicoDrive qui manquaient au backend x64 ont été récupérés directement à la même révision `9f46ee4e9680d108e548fb427e7c7e1aa1bb9d5a`, sans nouvelle recherche : `cpu/drc/emit_x86.c` (SHA `83FF0FAF9FD50A31B12A64B45C29E8124351CD3AADEAB3F7FEB4BE57298528E4`) et `cpu/drc/cmn.h` (SHA `215464C4AFE71A1152A443656C34AD5D244FDEF8BF96B50D5718F730146AC1A7`). Ils restent dans `artifacts/References/Machines/Sega/32X/PicoDrive/cpu/drc/`, accompagnés de `source-index.json` avec URL et SHA. La première requête réseau a été refusée par le bac à sable ; la même commande autorisée hors bac à sable a réussi.

Les mentions d’absence plus bas décrivent l’état antérieur de la collecte. L’émetteur récupéré génère directement les instructions x86/x64 et définit notamment le registre de contexte xBP et les temporaires xAX/xCX/xDX. Ce code rend maintenant possible la comparaison précise de l’émission native à l’adaptation CIL ; il n’est pas encore porté ni relié à SegaEmu.

### Raccordement expérimental de tous les CPU

Comparaison complète suivante, 600 trames After Burner Europe sans entrée ni limiteur, appels RunFrame seuls chronométrés : Debug 17,26 FPS interpréteur contre 10,33 tous CPU compilés (−40,2 %) ; Release 84,33 contre 16,16 (−80,8 %). État sauvegardé complet, image et audio cumulé sont identiques aux trames 300/600 entre les deux routes et les deux configurations. Chaque run retourne 532 378 trames audio. La route compilée construit 1 880 blocs 68000, 109 Z80 et 3 379/959 SH-2. Segments compilés Debug : 9,58 puis 11,20 FPS ; Release : 13,50 puis 20,13. Aucun gain de vitesse : cette version est rejetée pour une activation par défaut, tout en restant disponible pour le travail expérimental demandé. Les résultats et SHA DLL sont sous `attempts/all-cpu-routes/{Debug,Release}-{Interpreter,Dynamic}-600.json` et dans `summary.json/allCpuBenchmark`. Harnais temporaire supprimé après consignation. Ces temps incluent la génération des blocs et ne suffisent pas à en séparer le coût chaud ; aucune cause unique n’est démontrée par ces seuls FPS.

`MachineResources.UseDynamicCpuBlocks`, false par défaut, transmet désormais le même choix au 68000 principal, au Z80 et aux deux SH-2. `SegaBoard.Composition`, `CpuConfigurationFunctions`, `SegaBoard.ChipWiring` et le constructeur `Mega32X` propagent ce choix lors de la création et de la réinitialisation des CPU. Le sous-68000 Mega-CD reçoit aussi le choix lors de sa création différée ; ce raccordement CD n’est pas encore validé par ce test cartouche. L’application ne propose pas encore ce sélecteur dans ses options publiques.

Contrôle machine : After Burner Complete Europe, 60 trames depuis reset, interpréteur puis tous CPU en blocs, d’abord Debug puis Release. Les quatre exécutions terminent normalement. Dans chaque configuration, la sélection est contrôlée après chargement, après reset et après les trames. Les routes compilées construisent effectivement 489 blocs 68000, 81 Z80 et 1 890/412 SH-2. L’état sauvegardé complet, l’image finale et tous les échantillons audio des 60 trames ont des SHA identiques entre routes et configurations. Relevés : `attempts/all-cpu-routes/{Debug,Release}-60.json`.

Ce contrôle ne mesure aucun FPS et ne remplace pas la comparaison de 600 trames. Il valide le raccordement effectif des CPU et ce court scénario. Les opérations Z80 préfixées utilisent encore leur dispatch interne existant et la route SH-2 conserve les limites de performance précédemment mesurées : ce raccordement n’établit pas un Dynarec complet et rapide. Le harnais est supprimé après conservation des résultats ; chaque board est détruit dans `finally`.

### Première route compilée Z80, non activée dans SegaEmu

La variante `CPU/Zilog/Z80/Instruction` utilisée par SegaEmu dispose maintenant de `UseDynamicBlocks`, désactivé par défaut, et de traces bornées à 16 opérations principales. Les appels de handlers générés reprennent les corps du dispatch principal courant, sans modifier son code interprété. Avant chaque opération, le chemin compilé vérifie le budget, traite l’IRQ selon `after_ei`, vérifie le PC, lit l’opcode et incrémente R comme le chemin original. En cas d’opcode différent, il exécute la nouvelle opération une fois puis invalide les traces. Les préfixes utilisent encore les handlers existants pour leurs sous-opérations : cette route n’est donc pas un Dynarec intégral du Z80.

Le banc a comparé tous les champs du contexte Z80, LastFetch et les 64 Kio de mémoire à chaque budget irrégulier : 200 comparaisons Debug et 200 Release réussies, huit blocs effectivement compilés dans chaque configuration. Programme : LD SP, EI, LD B, DD LD IX, boucle INC B/CB RLC B/JP ; remplacement d’INC par DEC au passage 50, IRQ mode 1 au passage 100 et retour ED RETI. Relevés : `attempts/z80-dynamic/{Debug,Release}.json`. Cela ne couvre pas tous les opcodes et ne mesure pas la vitesse de la machine. Le harnais est supprimé après consignation ; les cœurs de test détachent leurs callbacks et arrêtent l’exécution dans `finally`.

### Première route compilée 68000, non activée dans SegaEmu

`Cpu68000.DynamicBlocks.cs` ajoute `UseDynamicBlocks` (false par défaut) et un compteur de blocs construits. La première traversée enregistre jusqu’à 16 instructions réellement exécutées ; une méthode dynamique réutilisable appelle ensuite leurs handlers directement. La table existante `ExecuteInstruction` reste l’autorité du mapping opcode/handler et n’est pas dupliquée. Les lectures d’opcode vérifient adresse et contenu avant chaque instruction ; extensions et données restent lues sur le bus existant. Un opcode remplacé après compilation est exécuté une seule fois par le dispatch courant, puis les traces sont invalidées. Le cache est borné à 4 096 traces.

Le contrôle autonome a comparé 100 contextes complets et mémoires en Debug puis 100 en Release, avec budgets irréguliers 1/7/19/127/5 cycles, une extension immédiate MOVE.W, une boucle ADDQ/BRA et le remplacement de ADDQ par SUBQ pendant le passage. Les deux configurations ont effectivement compilé cinq blocs et tous les contextes/mémoires correspondent. Relevés : `attempts/m68000-dynamic/{Debug,Release}.json`. Les cœurs de test sont arrêtés dans `finally`. Cette validation ne couvre pas encore toutes les instructions, les interruptions ni les fautes d’adresse, et ne mesure aucun gain FPS. Le backend reste expérimental, non sélectionné par SegaEmu ; le Z80 et la sélection machine complète restent à réaliser.

### Correction de la lecture source lors du redimensionnement GPU

Le shader lisait la colonne Mega Drive `x`, alors que le chemin CPU lit `clamp(x * nativeWidth / outputWidth, 0, nativeWidth - 1)`. Une ligne native de 256 pixels composée vers 320 pixels ne pouvait donc pas correspondre au chemin CPU. `Gpu32XFrameComposer` transmet désormais la colonne source dans les bits 22–30 des métadonnées par pixel. Les pixels de bordure reçoivent aussi cette colonne ; les pixels non capturés conservent leur colonne d’origine. Le transfert de la texture Mega Drive couvre le maximum des largeurs sources et sorties pour préserver également les lignes non composées dans le rectangle.

La colonne encodable est bornée à 511 : les lignes natives au-delà de 512 pixels sont refusées avant modification des métadonnées et restent traitées par le chemin CPU existant. Ce refus empêche une troncature silencieuse du numéro de colonne. Les builds ciblés `RetroMediaLab.Emulation.Sega.csproj`, Debug puis Release, `--no-restore -p:Platform=x64`, ont terminé avec zéro erreur. Journaux : `attempts/gpu-rectangle-retest/scaling-build-{Debug,Release}.log`.

Validation sur le contexte OpenGL réel : 16 cas Debug et 16 Release, zéro octet différent sur le framebuffer complet par rapport à la composition CPU attendue. Le banc couvre les largeurs natives 256, 320 et 512, les sorties 320/336, les bordures 0/8, affichage activé/désactivé et deux priorités. Les lignes 1 et 3 sont composées et la ligne 2 doit rester intacte. Les relevés et SHA des DLL sont dans `attempts/gpu-rectangle-retest/scaling-{Debug,Release}.json`. Le harnais a été supprimé. Aucun gain FPS supplémentaire n’est revendiqué.

Le premier lancement chargeait par erreur l’ancienne sortie `bin/Debug` au lieu de `bin/x64/Debug` : ses écarts ne testaient pas la correction et ont été écartés. Une reconstruction forcée avait été lancée avant de constater cette erreur de chemin. Les contrôles définitifs utilisent les sorties x64 correctes, dont les SHA sont conservés dans les JSON.

Le mode Interpréteur actuel est déjà le chemin complet : `SegaBoard.RunFrame` exécute le scheduler Mega Drive, qui appelle le 68000 et le Z80, puis la synchronisation 32X fait tourner les deux SH-2, `Vdp32X` et `Pwm32X`. Le VDP Mega Drive, `SoundUnit` (YM2612 et PSG), les bus, les interruptions, la mémoire et le mélange audio restent également actifs pendant ce même passage.

Le Dynarec testé jusque-là n’est pas un mode Dynarec complet. `UseCompiledBlocks` n’existe que dans `Sh2<TBus>` et n’est activé que pour les deux cœurs SH-2 de `Mega32X`. `Cpu68000<MegaDriveCpuBus>.Run` et `CpuZ80<SegaBoard>.Run` exécutent encore leur boucle interprétée. Le chemin 32X testé mélange donc l’interpréteur 68000/Z80 avec les blocs CIL des SH-2. Un vrai mode Dynarec pour cette machine doit couvrir ces quatre flux CPU; les VDP, le son et les bus restent leurs circuits émulés habituels et doivent être synchronisés avec les CPU recompilés. Le backend SH-2 actuel reste lui-même une expérimentation lente, pas le moteur de référence des autres cœurs.

La « composition GPU » testée ne déplace pas davantage la machine entière. `Gpu32XFrameComposer` n’est appelée que par `SegaBoard.Compose32XLine`, après le rendu CPU du VDP Mega Drive; le scheduler, les trois processeurs, VDP, YM2612, PSG, PWM, bus, IRQ et buffers audio restent côté CPU. `ManagedCore.StartSystem` construit le board sans compositeur graphique, `VideoFrame` transporte une `ReadOnlyMemory<byte>` CPU, et l’interface de rendu matériel existante expose un framebuffer OpenGL et sa relecture, pas un ordonnanceur compute ni des buffers de mémoire partagés avec les puces. Le test a demandé un contexte OpenGL 3.0, qui ne fournit pas les compute shaders du cœur OpenGL 4.3.

Un interpréteur GPU complet est techniquement une nouvelle machine parallèle, pas le déplacement d’un shader de rendu : états CPU, mémoires/bus, VDP, YM2612, PSG, VDP32X, PWM32X, interruptions et échéances doivent rester sur le GPU pour éviter des allers-retours par instruction ou par ligne. Il faut donc un contexte compute adapté et un ordonnanceur GPU capable de préserver les dépendances entre composants. Le code présent ne possède aucun de ces chemins. Une simple option de route qui conserve les appels C# CPU ne serait pas un mode « toute la machine en GPU ».

La portée réelle à comparer est donc : (1) interpréteur complet existant; (2) recompilation de tous les CPU de la 32X avec périphériques et synchronisation raccordés; (3) interpréteur et périphériques exécutés entièrement en compute GPU, sans lecture mémoire par instruction vers le CPU. Les résultats de la composition partielle ci-dessus n’établissent que le coût d’un sous-chemin de rendu et ne valident pas les routes 2 ou 3.

### Raccordement de la voie GPU à l’application

La comparaison GPU est un essai de banc, pas une option de la machine SegaEmu lancée normalement. `ManagedCore.StartSystem` construit `MachineResources` sans fournir `Gpu32XComposer`, et `MachineFactory` ne transmet ni contexte graphique ni sélection de route au cœur géré. Le Runner temporaire injectait directement le compositeur dans les ressources de la carte ; ce Runner a été supprimé après les mesures. Ainsi, les FPS GPU consignés comparent bien le chemin expérimental, mais ils ne décrivent pas une route actuellement activable dans l’application. La transformer en option utilisateur nécessiterait de raccorder un contexte graphique au contrat de session et à son cycle de vie ; ce changement n’a pas été fait dans ce banc.

### Audit du Dynarec mesuré par rapport aux sources locales

Sur la campagne de 1 200 trames, le Dynarec compile 3 762 blocs Master et 1 584 Slave. La génération prend 17,13 s en Debug et 16,87 s en Release ; sa vitesse moyenne Release est 29,62 FPS, contre 85,91 pour l’interpréteur. Les captures restent identiques et aucun repli n’a été déclenché. Cela établit une perte de vitesse sur ce scénario, sans isoler à lui seul chaque coût interne.

La relance Release permet de préciser le coût : `CompiledBlockBuildTicks` totalise 16,87 s sur 40,51 s de passe, soit 41,6 % du temps mesuré consacré à générer/préparer les 5 346 blocs. Le Dynarec atteint 46,15 puis 32,34 FPS sur les deux derniers segments, contre 84,98 puis 90,40 FPS pour l’interpréteur correspondant. Le banc ne répartit pas les blocs nouvellement générés par segment : ces chiffres montrent une perte persistante tard dans la passe, mais ne constituent pas une mesure parfaitement échauffée sans compilation. Détails : `artifacts/Emulation/segaemu-autres-jeux/attempts/three-routes-retest/more32xroute-afterburner-{Interpreter,Dynarec}-Release.json`.

Le profilage .NET Release du même After Burner (1 200 trames, SHA ROM identique, blocs compilés Master/Slave) montre `TryExecuteCompiledBlock` dans 82,1 % du temps inclusif échantillonné. `ResolveCodeBlock` et `CompileBlock` sont chacun à 36,9 % en inclusif ; `RuntimeHelpers.PrepareDelegate` représente 36,6 % (16,3 s sur les 44,56 s couverts par la trace). Ces valeurs inclusives se recouvrent : elles désignent la génération et la préparation JIT des blocs comme coût dominant, sans pouvoir s’additionner. `SynchronizeBusClock` apparaît à 6,8 % et `Vdp.render_line` à 4,3 %. Le profileur classe aussi 26,2 % en `UNMANAGED_CODE_TIME`, sans nommer les blocs dynamiques concernés ; cette part n’est donc pas attribuée à un opcode précis. La trace couvre compilation et exécution et ajoute le coût de l’échantillonnage : elle sert à localiser les coûts, pas à comparer les FPS. Le processus a fini avec le code 0, sans exception .NET.

Correction de description : `Sh2.Blocks.cs` ne se contente pas d’appeler l’interpréteur instruction par instruction. `Sh2.BlockDecoder.cs` émet le décodage des opcodes et `Sh2.BlockInliner.cs` copie le CIL des handlers existants dans les blocs, avec des appels conservés seulement quand le corps ne peut pas être incorporé. Le générateur actuel reste toutefois dépourvu de l’allocation statique et du cache de registres hôte décrits par le compilateur PicoDrive.

Dans `artifacts/References/Machines/Sega/32X/PicoDrive/cpu/sh2/compiler.c`, PicoDrive documente allocation statique des registres, cache des autres registres, propagation de constantes, liens de blocs et cache de pile d’appels. La collecte locale ne comprend que `compiler.c`, `compiler.h` et l’index : son émetteur hôte et les dépendances `cpu/drc` ne sont pas présents. Cette tranche ne suffit donc pas à porter fidèlement ou construire son backend. Ares fournit un second point d’entrée : `ares/ares/component/processor/sh2/sh2.cpp` sélectionne `recompiler.cpp` et alloue un tampon exécutable de 32 Mio ; `sh2.hpp` inclut `nall/recompiler/generic/generic.hpp`. Ces fichiers inclus sont absents de la collecte locale, qui ne contient que les sources SH-2 listées. À l’inverse, `Gens/sh2a.asm` expose un interpréteur x86 optimisé (PC et budget gardés dans des registres, table d’opcodes), pas un dynarec.

Précision du backend PicoDrive : `compiler.c` inclut `../drc/cmn.h` et, sous `__x86_64__` ou `_M_X64`, inclut `../drc/emit_x86.c`. `cpu/drc/cmn.h` et `cpu/drc/emit_x86.c` sont tous deux absents des références locales (aucune copie trouvée sous `artifacts/References`). C’est précisément le chemin x64 requis sur le PC de test ; traduire `compiler.c` seul ne permettrait pas de construire ou d’exécuter ce Dynarec.

Le profil conservé `current-dispatch-profile` correspond à la route `Switch` de Chaotix, 120 trames ; il ne profile pas le Dynarec. Son échantillon `RuntimeHelpers.CreateSpan` ne peut donc pas expliquer la perte de cette route. La modification combinée des tables déjà consignée dans `SegaEmu-essais-deja-realises.md` reste rejetée et n’a pas été répétée.

Décision : aucune route candidate n’est activée par défaut. L’interpréteur demeure la référence ; les mesures ne justifient pas d’activer le Dynarec ou la composition GPU. Un portage fidèle d’un DRC existant dépend de la récupération des fichiers backend PicoDrive ou Ares ; une nouvelle variation du code CIL déjà mesurée ne remplace pas ces sources.

### Comparaison du quantum de synchronisation SH-2

J’ai comparé le pas SegaEmu actuel de 84 cycles 68000 (588 cycles maître / 252 cycles SH-2) au `STEP_N = 164` de `artifacts/References/Machines/Sega/32X/PicoDrive/pico/32x/32x.c`. Les deux runs utilisent le même After Burner Complete Europe (SHA-256 `78E4E6ED0DCC2B56018B5F7B7E6758208D1D29355524878829389906624C36A1`), la machine Mega Drive + 32X en PAL, l’interpréteur sur les deux SH-2 et 600 trames sans limiteur. Les FPS des deux moitiés sont indiqués pour montrer l’évolution pendant le passage.

| Configuration | Pas 68000 | FPS global | FPS trames 1–300 / 301–600 | Variation vs 84 |
| --- | ---: | ---: | ---: | ---: |
| Debug | 84 | 18,15 | 18,19 / 18,12 | référence |
| Debug | 164 | 15,41 | 16,71 / 14,29 | −15,1 % |
| Release | 84 | 82,39 | 70,87 / 98,40 | référence |
| Release | 164 | 86,62 | 76,10 / 100,55 | +5,1 % |

Les quatre captures SHA BGRA aux trames 300 et 600 sont identiques selon le pas et la configuration. Chaque run produit 532 378 trames audio. Les deux exécutions Release se terminent avec le code 0. Le build Release du banc termine avec 0 erreur ; il signale l’avertissement NU1900 car l’index NuGet de vulnérabilités n’est pas joignable. Le résultat Debug est complet sur 600 trames ; son code de sortie n’a pas été conservé séparément par l’appel de commande.

Décision : le pas 164 gagne 5,1 % dans cet unique scénario Release mais perd 15,1 % en Debug. Je ne conserve pas ce sélecteur dans le code de production et rétablis le pas 84. Les JSON de mesures restent sous `artifacts/Emulation/segaemu-autres-jeux/`; le harnais temporaire est supprimé après l’essai. Cette comparaison du pas CPU est distincte des trois voies déjà mesurées plus haut (interpréteur, Dynarec SH-2 et composition GPU 32X) ; elle ne constitue pas une quatrième voie de rendu ou d’exécution.

### Validation ciblée SH-2/SH7095 après les essais Dynarec

Le projet complet `RetroMediaLab.Chips.Tests` ne compile pas actuellement : `Upd780C1Tests.cs` importe `RetroMediaLab.Chips.Tests.CPU.Common.Z80`, mais aucun `Z80TestCircuit` correspondant n’existe dans les sources locales. Je n’ai pas changé ce test UPD780C, hors périmètre Sega. Pour isoler la validation Sega sans construire les autres émulateurs, un projet temporaire a compilé uniquement `Sh2Tests.cs`, `Mega32XChipTests.cs` et Chips.

| Configuration | Tests SH-2/32X | Échecs | Durée tests |
| --- | ---: | ---: | ---: |
| Debug | 50 réussis | 0 | 0,80 s |
| Release | 50 réussis | 0 | 0,86 s |

Les tests couvrent notamment l’équivalence interpréteur/blocs, les budgets irréguliers, les délais de fetch, l’auto-modification du code et des lignes de cache, les interruptions, les échéances de bus, la reprise de contexte et les écritures octet. Le retest machine complet des trois voies Debug/Release et ses captures identiques sont consignés plus haut. Le filtre Mega32XIntegrationTests Debug a aussi passé 7/7 tests. Un lancement local Release séparé a été interrompu avant les tests parce que son projet référence et compile tous les modules d’émulation ; ce code 1 vient de l’interruption volontaire et n’est pas un plantage .NET. Les avertissements `NU1900` signalent seulement que NuGet ne peut pas joindre son index d’audit.
Le harnais temporaire a aussi permis d’exécuter la sélection SH-2/32X sans compiler les autres émulateurs : Debug, 50/50 réussis ; Release, 50/50 réussis. Le projet complet Chips.Tests reste bloqué à la compilation par `Upd780C1Tests.cs` et sa référence Z80 manquante ; ce défaut préexistant n’a pas été modifié. Le contrôle d’intégration Mega32X Debug passe 7/7. Aucun plantage .NET n’a eu lieu pendant ces trois validations ciblées ; l’échec de compilation du projet complet et l’interruption volontaire du build local Release ne sont pas des plantages du runtime. Le harnais `.work/SegaSh2Validation` a été supprimé après consignation.

### Contrôle ciblé du payload rapide et des échéances MMIO

J’ai ajouté `FastPayloadStopsAtShortenedMmioDeadline` dans `tests/RetroMediaLab.Chips.Tests/CPU/Hitachi/Sh2Tests.cs`. Le bus compilé annonce une borne de six cycles ; pendant la lecture de données simulant le MMIO, cette lecture rapproche la prochaine échéance à un cycle. Le test compare à l’interpréteur le contexte SH-2, la mémoire, les cycles et la livraison de l’interruption. Il passe 51/51 avec l’ensemble isolé SH-2/32X en Debug et 51/51 en Release. Le test de budgets irréguliers exerce aussi le payload borné avec des attentes de lecture 0, 2 et 5 ; les autres cas compilés conservent le bus sans borne (`MaximumWaitStates = -1`) et vérifient le chemin de repli.

Ce contrôle utilise un bus de test. Le test d’intégration sur le vrai bus privé de Mega32X et les écritures SDRAM est documenté ci-dessous ; le passage complet After Burner aux images identiques et sans repli reste une validation machine plus large, mais indirecte pour ce cas précis. Le listing natif détaillé n’a pas été capturé.

### Invalidation réelle du code exécuté depuis la SDRAM 32X

Le test d’intégration `CompiledSdramOpcodesStayCachedWhenUnchangedAndRefreshWhenWritten` exécute le programme 32X synthétique copié en SDRAM, active les blocs compilés sur le SH-2 Master et utilise le vrai bus `Mega32X`. Pour les écritures octet et mot, une écriture qui laisse l’opcode identique conserve le bloc compilé ; une modification de l’opcode exécute sa nouvelle valeur et augmente le nombre de blocs reconstruits. Résultat : Debug 2/2 cas, Release 2/2 cas, aucune exception .NET. Cela valide directement le marquage et l’époque SDRAM pour ces chemins d’écriture.

Le premier essai du test avait réglé `ProgramCounter` sur `instructionAddress + 4` ; son setter ajoute déjà ces quatre octets. L’échec initial (registre resté à 0) venait donc du test, pas de l’émulateur. Le banc a été corrigé pour viser `0x06000200`, puis les deux cas ont réussi en Debug et Release. Le board est disposé dans `finally`.

### Écriture de données hors de la plage de code compilé

Le test `CompiledBlockReadsUpdatedDataOutsideItsCodeRangeWithoutRebuild` change un mot de données à `0x110`, dans la même page de version que le bloc à `0x100`, sans toucher aux instructions marquées. Il confirme que `CodeVersion` change alors que `CodeEpoch` ne change pas ; le SH-2 compilé relit la nouvelle valeur, reste identique à l’interpréteur pour l’état et les cycles, et ne reconstruit pas le bloc. Résultat ciblé : Debug 1/1, Release 1/1, sans exception .NET. Ce test couvre le contrat générique du bus de blocs ; le test d’intégration précédent couvre les écritures d’opcodes sur le vrai bus SDRAM 32X.

### Barrières, liens et réutilisation des blocs

La sélection SH-2/32X passe 51/51 en Debug et 51/51 en Release ; elle couvre `LinkedBlocksKeepFetchWaitsAcrossBudgets`, `CodeWriteBarrierStopsBlockAfterReadThatChangesNextInstruction`, `NativeDispatchPreservesInterruptsAndRestore` et `SoCNativeRuntimeMatchesReferenceWithInterruptAndStateRestore`. Le test supplémentaire d’écriture de données hors code passe 1/1 dans les deux configurations ; l’intégration sur opcode SDRAM passe 2/2 dans les deux configurations. Ensemble, ils vérifient attentes de fetch et budgets, barrières SMC, données modifiées hors code, interruptions, reprise de contexte et réutilisation/recompilation : première exécution compile le bloc, écriture inchangée le réutilise, opcode changé le reconstruit. `UseCompiledBlocks` reste faux par défaut. Les FPS publiés sont ceux de passes machine complètes avec compilation ; aucun FPS séparé après échauffement complet n’a été mesuré, donc ces vérifications établissent la réutilisation fonctionnelle, pas un gain chaud.

### Profilage de l’interpréteur 32X et bilan des plantages .NET

Le profil `dotnet-sampled-thread-time` couvre 1 200 trames d’After Burner Complete Europe avec l’interpréteur, la ROM au SHA `78E4E6ED0DCC2B56018B5F7B7E6758208D1D29355524878829389906624C36A1` et les DLL Release du banc. La trace Speedscope couvre 18,36 s et le processus s’est terminé avec le code 0. Cette durée est sous profileur : elle localise le coût, mais ne se compare pas aux FPS non profilés.

Les piles inclusives se recouvrent : `Cpu68000.Run` couvre 75,92 % du temps échantillonné, `Mega32X.Synchronize` 68,17 %, `SH2.Native.Interpreter.Execute` 62,09 %, sa boucle `N66_ExecuteLoop` 51,13 %, et `N105_ExecuteOpcode` 22,33 %. En attribution propre approximative depuis les intervalles `CPU_TIME` de la trace, les plus fortes méthodes sont `N105_ExecuteOpcode` à 12,36 %, `Execute` à 10,96 %, `N26_SynchronizePeripherals` à 9,35 %, `N66_ExecuteLoop` à 6,48 %, `Cpu68000.Run` à 4,76 %, `Mega32X.Synchronize` à 4,71 % et `N46_SynchronizeBusClock` à 4,60 %. `Vdp.render_line` représente 10,12 % en inclusif et `Vdp.render_bg_m5` 2,80 % en propre. Le profil pointe principalement l’exécution et la synchronisation des cœurs CPU sur ce scénario ; il ne justifie pas d’attribuer la lenteur au rendu vidéo seul. Les pourcentages propres sont des échantillons, pas des mesures instrumentées par appel.

Contre-vérification des références locales avant tout nouvel essai : Gens et ses forks ont déjà une table d’opcodes et un interpréteur x86 qui garde PC/cycles dans des registres (`Gens/sh2.c`, `Gens/sh2a.asm`), mais les routes de dispatch SegaEmu `Switch`, `DelegateTable` et `IndirectTable` ont déjà été mesurées sans gagnant global, et la variante CIL à registres locaux a planté. Le `Sh2.step`/`SH7095` courant cumule déjà les cycles jusqu’aux échéances du bus et des périphériques, une forme également visible dans le traitement par cycles de BlastEm ; refaire ce changement serait un doublon de l’essai d’ordonnancement déjà rejeté. Enfin, le backend x64 PicoDrive (`cpu/drc/cmn.h`, `emit_x86.c`) et le recompiler générique Ares (`recompiler.cpp`, `nall/recompiler/generic/generic.hpp`) sont absents de toutes les références locales vérifiées. Je ne retiens donc pas de nouvelle modification de vitesse non étayée par un backend source complet ; le profil identifie le coût, mais les sources présentes ne donnent pas une route neuve à porter sans réinventer le cœur.

Les plantages conservés dans les journaux ont des causes distinctes et identifiées :

- `FileNotFoundException retromedialab.chips` : quatre essais initiaux Metal Head/Tempo ont échoué avant la création de la carte, le Runner ne résolvant pas les DLL. Le banc a ensuite installé le résolveur et contrôlé le SHA des DLL ; les exécutions corrigées ont démarré.
- `InvalidProgramException` en Debug et Release : la variante d’émission CIL avec registres locaux a échoué à `RuntimeHelpers.PrepareDelegate` avant toute mesure. Cette variante a été retirée. Le Dynarec restant a ensuite terminé ses essais, mais sans gain de FPS.
- `PlatformNotSupportedException` en Debug et Release : le premier contrôle de la composition GPU refusait le type de contexte OpenGL renvoyé par l’interface. Le constructeur accepte maintenant OpenGL 3+ et le banc fournit un contexte Core 3.0 ; les six exécutions comparatives ont fini sans erreur.

Le profil interpréteur n’a produit aucune exception .NET. Les journaux d’échecs antérieurs sont gardés comme traces des essais corrigés ; ils ne décrivent pas un plantage actuellement reproduit par les scénarios validés.

### Tentative de listing natif JIT

Le test ciblé `CompiledBlockReadsUpdatedDataOutsideItsCodeRangeWithoutRebuild` a réussi 1/1 en Debug et 1/1 en Release pendant la capture. `COMPlus_JitDisasm=SH2_00000100_1` n’a produit aucun listing du `DynamicMethod` dans les deux configurations. En Release, le premier lancement avec `--no-build` a échoué parce que le harnais temporaire n’avait pas encore de binaire Release ; le lancement suivant sans cette option a compilé le harnais et réussi 1/1. C’était une erreur d’invocation du banc, pas un plantage .NET à l’exécution. Le filtre global `*` en Debug a produit 443 849 lignes (16 071 701 octets) de code natif, mais aucune méthode `SH2_...` ; cette sortie générale n’est donc pas une preuve du code machine du bloc. Le harnais et ses sorties temporaires ont ensuite été supprimés. Le CIL émis est visible dans `Sh2.Blocks.cs`, mais je ne confonds pas cette source avec un listing natif. Aucun gain FPS n’est déduit de cette tentative.

### Test ciblé du plafond de blocs SH-2 à 128 instructions

Le nouveau test exécute 96 fois `ADD #1,R1` en interpréteur, avec la limite existante de 64 et la candidate de 128. En Debug et Release, registres, contexte, cycles et mémoire sont identiques ; 64 compile deux blocs et 128 en compile un. Le test ciblé passe 1/1 dans chaque configuration. Ce résultat établit l’équivalence sur ce programme droit et la réduction du nombre de blocs, pas encore un gain FPS machine.

Le projet complet `RetroMediaLab.Chips.Tests` ne compile pas actuellement : `Upd780C1Tests.cs(2,37)` référence `RetroMediaLab.Chips.Tests.CPU.Common.Z80`, namespace absent, et `Z80TestCircuit` n’a aucune définition dans les sources suivies. Ce fichier n’a pas été modifié dans cet essai. Pour isoler le nouveau cas, le test a été exécuté avec un projet temporaire qui incluait uniquement `Sh2Tests.cs` ; il n’y a pas eu de plantage .NET dans ces deux exécutions.

### Comparaison machine complète de la limite 64/128

J’ai mesuré le chemin Dynarec avec After Burner Complete (Europe), Mega Drive + 32X PAL, mêmes ressources et aucune entrée, pendant 600 trames. SHA-256 ROM : `78E4E6ED0DCC2B56018B5F7B7E6758208D1D29355524878829389906624C36A1`. Les deux SH-2 utilisent la limite indiquée ; chaque configuration Debug/Release charge les mêmes DLL pour ses deux passages.

| Configuration | Limite | Temps 600 trames | FPS global | FPS trames 1–300 / 301–600 | Blocs Master / Slave | Temps de compilation Master / Slave |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Debug | 64 | 57,94 s | 10,35 | 9,48 / 11,41 | 3 379 / 959 | 10,79 s / 2,17 s |
| Debug | 128 | 84,71 s | 7,08 | 7,27 / 6,90 | 3 379 / 959 | 15,78 s / 3,48 s |
| Release | 64 | 26,70 s | 22,48 | 18,22 / 29,33 | 3 379 / 959 | 11,72 s / 2,45 s |
| Release | 128 | 26,79 s | 22,40 | 17,93 / 29,83 | 3 379 / 959 | 12,23 s / 2,36 s |

Les quatre passages produisent 532 378 échantillons audio et les mêmes SHA de framebuffer aux trames 300 (`DBD12A5FB2DF864A358810432D47FF8AEF48CE81925A08EB2931EDD773A05291`) et 600 (`7FBD59F726F8BA5EEE4AE6D46661182607AC5C76910C31DC1572A953FA9748FA`). Aucun bloc ne passe par le fallback. La limite 128 ne réduit donc pas les blocs compilés sur ce jeu. En Debug elle augmente leur coût de compilation combiné d’environ 12,96 à 19,26 s et le débit baisse de 31,6 %. En Release le débit global baisse de 0,3 %, variation trop petite pour établir un gain, tandis que les nombres de blocs restent identiques. Le premier passage Debug à 64 avait donné 11,33 FPS ; la répétition à DLL identiques a donné 10,35 FPS, ce qui confirme que l’écart Release de 0,3 % n’est pas exploitable comme amélioration.

Décision : retirer le sélecteur 128, conserver exactement la limite d’origine de 64 instructions, et supprimer le test synthétique propre à cette candidate après consignation. Cet essai ne change pas les résultats antérieurs de comparaison des trois voies complètes (interpréteur, Dynarec, composition GPU) ; il rejette seulement un plafond de blocs plus élevé. Le premier build du harnais avait échoué à la compilation parce que deux espaces de noms Sega manquaient et que la configuration de sortie était mal extraite ; ces erreurs propres au harnais ont été corrigées. Les builds Debug/Release suivants ont abouti et aucun passage d’émulation n’a planté.

## Décodage des opérandes 68000 pour le backend MD/MCD

`M68000OperandDecoder.cs` porte la branche 68000 de `m68k_decode_op_ex` de BlastEm : modes directs et indirects, déplacements signés, descripteurs d’index, adresses absolues et immédiats. Les trois formes étendues réservées sont refusées. Le décodeur ne modifie pas le contexte CPU ; les extensions sont fournies par un rappel de lecture.

Les 53 contrôles passent en Debug puis en Release. Relevés : `artifacts/Emulation/segaemu-autres-jeux/attempts/m68000-operand/{Debug,Release}.json`, journaux de compilation dans le même dossier. Le harnais temporaire est supprimé après conservation des relevés. Cela valide uniquement les cas d’opérandes contrôlés, pas le décodeur complet d’instructions, le backend natif ni un gain FPS. Aucun changement aux méthodes Run interprétées dans cette étape.

## Frontal natif 68000 : MOVE, calcul rapide et branchements

`M68000InstructionDecoder.cs` porte les familles MOVE/MOVEA, MOVEQ, ADDQ/SUBQ, Scc/DBcc, Bcc/BSR et traps A/F de `68kinst.c` de BlastEm, dans le même arbre source que le décodeur d’opérandes. Il conserve l’ordre des extensions, la longueur de deux octets signalée pour une instruction invalide et la distinction 68000/68020 du déplacement de branchement `0xff` (moins un, sans extension longue sur 68000). Une famille non encore portée produit `UnsupportedFamily`, jamais une fausse instruction illégale.

4 148 contrôles passent en Debug et en Release, dont les 4 096 encodages MOVEQ. Les autres cas vérifient les extensions source puis destination, MOVEA mot, les déplacements/index, les destinations interdites et les branchements signés. Relevés : `attempts/m68000-instructions/{Debug,Release}.json`. Le premier lancement du harnais a échoué car les cas MOVE invalides n’offraient pas les extensions que le décodeur source lit avant validation ; les vecteurs ont été corrigés, pas le cœur. Le harnais temporaire est supprimé après consignation. Ces contrôles ne prouvent ni un dynarec complet ni un gain FPS ; ce frontal n’est pas raccordé à l’exécution des jeux. Les méthodes Run interprétées restent inchangées.

## Frontal natif 68000 : groupe immédiat et bits

`M68000InstructionDecoder.Immediate.cs` porte le groupe zéro de la branche 68000 de BlastEm : MOVEP, bits statiques/dynamiques, ORI/ANDI/SUBI/ADDI/EORI/CMPI et variantes CCR/SR. Les mots immédiats et extensions de destination sont lus dans l’ordre source ; les encodages réservés au 68010/68020 sont rejetés. Pour MOVEP, le décodeur conserve le mot brut du déplacement comme BlastEm : le calcul d’adresse du futur backend devra en étendre le signe sur 16 bits. Le décodage BTST dynamique conserve également la forme immédiate acceptée par cette source, sans l’étendre aux formes statiques interdites.

110 contrôles réussissent en Debug puis en Release, sans erreur du harnais. Les contrôles portent sur les trois tailles immédiates, les extensions doubles absolues, les registres CCR/SR, MOVEP dans les deux directions, la taille des bits sur registre/mémoire, les modes PC-relatifs et les rejets. Relevés et builds : `attempts/m68000-immediate/`. Le nouveau harnais est supprimé après consignation. Aucun jeu n’a été remesuré à cette étape : ces fichiers ne sont pas encore connectés à l’exécution. `Cpu68000.cs` et `CpuZ80.cs` ne présentent aucun écart de contenu avec Git. Le frontal complet, l’émetteur natif et les comparaisons MD/MCD restent à terminer.

## Frontal natif 68000 : familles arithmétiques

`M68000InstructionDecoder.Arithmetic.cs` porte OR/DIV/SBCD, SUB/SUBX, CMP/EOR, AND/MUL/ABCD/EXG, ADD/ADDX et les décalages/rotations de la branche 68000 de BlastEm. Les modes ADDA/SUBA/CMPA, les doubles pré-décréments et post-incréments, les décalages mémoire et les restrictions d’adressage sont conservés. Les extensions PACK/UNPK et bitfields 68020 ne sont pas activées.

Le harnais compare les opérations et tailles aux handlers de la table d’opcodes de notre interpréteur existant, pour chacun des 24 576 encodages de ces six familles ; il ajoute huit cas d’opérandes et d’extensions. Résultat : 46 000 contrôles, zéro différence en Debug puis en Release. Relevés : `attempts/m68000-arithmetic/{Debug,Release}.json`. Aucun échec de lancement ou de compilation. Le harnais temporaire est supprimé après consignation. Cette comparaison n’exécute pas les instructions et ne prouve pas encore le fonctionnement ou la vitesse d’un backend natif raccordé aux machines.

## Frontal natif 68000 : opérations diverses et contrôle

`M68000InstructionDecoder.Miscellaneous.cs` porte le groupe quatre de BlastEm : LEA/CHK, MOVEM, opérations unaires, transferts SR/CCR/USP, EXT/SWAP, PEA, TAS/TST, ILLEGAL, JSR/JMP, TRAP, LINK/UNLK, RESET/NOP/STOP et retours. Les extensions 68010/68020 restent exclues. Le masque MOVEM précède l’extension d’adresse ; les formes PC-relatives conservent l’ajustement de deux octets de BlastEm. Une instruction ILLEGAL explicite est distinguée d’un encodage invalide. Le champ taille de SWAP reste Word comme dans le frontal source (taille de la moitié échangée), même si le handler interprété est nommé swap_32.

Les 4 096 encodages du groupe sont comparés aux opérations de la table de notre interpréteur ; les lectures d’extensions et longueurs sont contrôlées sur les formes décodées, avec onze cas supplémentaires de masques, déplacements et instructions de contrôle. Résultat : 6 065 contrôles, zéro différence en Debug puis en Release. Relevés : `attempts/m68000-miscellaneous/{Debug,Release}.json`. Aucun échec de compilation ou d’exécution. Harnais temporaire supprimé après consignation. Tous les groupes du frontal 68000 sont désormais routés ; cela ne constitue pas encore un backend natif exécutant MD/MCD. L’émission x64, son raccordement au contexte/bus/cycles, puis les comparaisons avec l’interpréteur restent nécessaires avant toute revendication de vitesse.

## Émission x64 : premières primitives du backend BlastEm

`M68000X64Emitter.cs` porte les encodages scalaires de `gen_x86.c` : MOV registre/immediate/mémoire, opérations binaires registre/mémoire/immediate, extensions de signe/zéro, décalages/rotations, SETcc, sauts relatifs/indirects, appels et pile. Les identifiants de registres sont normalisés aux numéros matériels ; AH/CH/DH/BH restent distincts et leurs combinaisons interdites avec REX sont refusées. Les accès RSP/R12 ajoutent le SIB nécessaire, les déplacements courts/longs et les tailles 8/16/32/64 sont distingués. Les appels laissent explicitement l’alignement et le shadow space Windows à leur appelant. L’émetteur ne possède que ses octets, aucune allocation exécutable.

425 contrôles passent en Debug puis en Release. Ils comprennent les octets attendus aux limites d’encodage et 393 exécutions natives par configuration : calculs sur quatre tailles, immédiats 64 bits, extensions de signe/zéro, les seize conditions x64 (retenue, zéro, signe, débordement, parité), ADC/SBB avec retenue entrante, décalages immédiats/CL, mémoire avec déplacements signés, bases R12/R13/RBP, boucle native et appel avec pile alignée. Toutes les allocations du harnais sont faites en écriture puis passées en lecture/exécution, et libérées dans finally : 393 allocations, 393 libérations dans chaque passage. Aucun échec du harnais. Relevés : `attempts/m68000-x64-emitter/{Debug,Release}.json` ; harnais supprimé après consignation.

Cette étape ne constitue pas le compilateur 68000 complet : il reste notamment les primitives unaires/multiplication/division/bits, la traduction des opérations invitées avec leur contexte/cycles/bus, la propriété des pages exécutables et le raccordement MD/MCD. Aucun passage de jeu ni gain FPS n’est attribué à ce code non encore raccordé. Les boucles interprétées n’ont pas été modifiées.

## Émission x64 : primitives complémentaires

`M68000X64Emitter.cs` ajoute les encodages unaires NOT/NEG/MUL/IMUL/DIV/IDIV, IMUL à deux registres et immédiat, CWD/CDQ/CQO, XCHG, TEST immédiat, extensions depuis la mémoire, accès indexés SIB et BT/BTS/BTR/BTC. Les permutations de mot utilisent les rotations déjà employées par le backend source. Les index RSP et les registres hauts incompatibles avec REX sont refusés ; REX.X est émis pour les index étendus.

310 contrôles passent en Debug et en Release. Ils comprennent les octets attendus et 293 exécutions natives par configuration : opérations unaires aux limites, IMUL registre/immediate sur 16/32/64 bits, quotient et reste signés/non signés sur 32/64 bits, produit signé/non signé sur 128 bits (RDX:RAX), bits statiques/dynamiques sur 16/32/64 bits, index mémoire avec les quatre facteurs d’échelle, extensions mémoire et permutations. Aucun échec du harnais ; 293 allocations exécutables et 293 libérations dans chaque passage. Relevés : `attempts/m68000-x64-extended/{Debug,Release}.json` ; harnais supprimé après consignation.

Les instructions natives sont ici réellement exécutées, mais les tests n’exécutent pas encore le code invité de MD/MCD dans ce backend. Ils ne prouvent donc ni une équivalence de machine complète, ni un gain FPS. Les gardes d’exceptions invitées (division par zéro/débordement), les cycles et les accès au bus doivent être produits par le traducteur 68000 avant l’utilisation de ces primitives. Le frontend et l’émetteur sont des composants du portage, pas un dynarec raccordé achevé.

## Pages exécutables et accès au contexte 68000 existant

`M68000X64CodeBuffer.cs` possède une allocation Windows x64 par SafeHandle : copie des octets en RW, passage en RX, FlushInstructionCache, puis emprunt du handle pendant chaque appel natif pour empêcher sa libération en cours d’exécution. Les pointeurs des registres et de Cpu68000State restent empruntés et épinglés par l’appelant. Les erreurs de publication ferment le buffer dans finally ; Dispose libère l’allocation, avec le filet de sécurité du SafeHandle.

`M68000X64ContextEmitter.cs` adapte les accès au contexte de BlastEm aux seize uint existants et aux offsets réels de Cpu68000State. Les écritures octet/mot préservent la partie haute des registres ; les chargements étendent le signe ou le zéro selon le besoin. Les drapeaux logiques et d’addition/soustraction reprennent les expressions exactes de Cpu68000.Operations, y compris leurs bits intermédiaires non normalisés. MOVE préserve X ; CMP ne le met pas à jour. Cela évite de modifier la représentation des états sauvegardés du cœur interprété.

10 279 contrôles passent en Debug puis en Release. Pour MOVE/ADD/SUB/CMP sur les trois tailles, les registres et tous les octets de Cpu68000State produits par le code natif sont comparés aux handlers privés existants sur 169 paires limites et 256 paires aléatoires par cas (5 100 comparaisons d’exécution au total). Les autres contrôles portent sur les seize offsets de registres, les extensions signées, les champs d’état, les arguments invalides, le refus d’exécuter un buffer fermé et le cycle de vie mémoire. Les treize buffers de chaque passage sont disposés ; VirtualQuery confirme RX avant l’exécution et MEM_FREE après libération sur les douze buffers des comparaisons. Relevés : `attempts/m68000-x64-context/{Debug,Release}.json`. Aucun échec de compilation ou d’exécution ; harnais supprimé après consignation.

Ces comparaisons exécutent les calculs natifs et les handlers directement : elles n’incluent pas encore fetch/prefetch, cycle des instructions, interruptions ou accès aux périphériques. Le contexte du vrai cœur est la référence, mais les jeux MD/MCD ne passent pas encore par le nouveau compilateur natif. Aucun gain FPS ni fonctionnement complet du dynarec n’est donc déclaré à cette étape.

## Traduction des corps invités : registres et immédiats

`M68000X64InstructionEmitter.cs` relie le décodeur BlastEm aux primitives x64 et au contexte existant pour MOVE/MOVEA/MOVEQ, ADD/SUB/CMP (dont adresse et formes rapides), AND/OR/EOR, CLR/NOT/NEG, EXT/SWAP/EXG et NOP sur les formes registre/immediate prévues. Une forme encore absente renvoie false avant d’émettre des octets. Les exceptions mémoire, branchements et instructions non encore traduites ne sont pas masqués par une prétendue prise en charge. Les corps ne produisent pas encore fetch/prefetch, cycles ou interruptions.

Le passage de tous les opcodes trouve 6 209 encodages acceptés par ce premier groupe. Le harnais retient 327 opcodes représentatifs (premier/dernier de chaque handler de référence, plus registres mixtes), chacun exécuté sur 22 états limites/aléatoires, soit 7 194 exécutions natives par configuration. Les registres et tous les octets de Cpu68000State sont comparés aux handlers de la table existante. Pour les formes immédiates, le fetch existant consomme les extensions côté natif avant le corps ; ses adresses de lecture sont comparées à celles du handler. Cette préparation partagée n’est pas un fetch natif et ne doit pas être présentée comme tel.

81 237 contrôles réussissent en Debug puis en Release, zéro différence. Ce nombre inclut le refus sans émission des opcodes non pris en charge ; il ne représente pas autant de scénarios de jeu. Les 327 buffers de chaque passage sont fermés dans finally. Le premier build du harnais a échoué sur CS1686 (variable dont l’adresse est prise également capturée par le diagnostic LINQ) ; le diagnostic a été corrigé en capturant une copie boxée, puis les deux configurations ont réussi. Relevés : `attempts/m68000-x64-instructions/{Debug,Release}.json` ; harnais supprimé après consignation.

L’adaptation conserve notamment le contenu complet de N après EXT.W, le calcul V de NEG utilisant le registre entier avant écriture, et la remise à zéro de poll_detected des ADDQ sur registre de données. Les boucles interprétées restent inchangées. Ces corps natifs ne sont pas encore raccordés au Run de SegaEmu ; aucun jeu ni gain FPS n’est attribué à cette étape. Il reste les autres corps, les adresses/mémoires/périphériques, les fetchs et budgets, les exceptions et l’invalidation avant la comparaison MD/MCD complète.

## Blocs à fetch direct, préchargement et budgets natifs

`ICpu68000CodeBus` expose une fenêtre empruntée dont les mots hôtes little-endian correspondent aux lectures d’instructions du bus. `MappedCpuBus` ne la fournit que pour les banques directement mappées de 64 Kio, sans handlers de lecture mappée ; ses lectures/écritures habituelles ne sont pas modifiées. Le propriétaire devra résoudre de nouveau la fenêtre avant chaque entrée pour détecter un changement de banque.

`M68000X64Block.cs` épingle cette fenêtre et assemble les corps déjà pris en charge avec leur fetch natif : gardes du budget/PC, sélection du mot préchargé ou de la mémoire, vérification des opcodes et extensions courants, mise à jour PC/IR/prev_pc selon la configuration, préchargement du mot suivant, refresh et cycles. Le calcul des cycles reprend la table du cœur et l’arrondi de USE_CYCLES à chaque instruction. Les gardes de code modifié sortent avant toute modification de l’état de l’instruction concernée. Un opcode préchargé reste prioritaire même si le mot mémoire correspondant a changé, comme dans le Run existant. Les limites de fenêtre sont vérifiées à la compilation, notamment le mot suivant nécessaire au préchargement. Le code et l’épinglage sont possédés par le bloc ; Execute/Dispose sont synchronisés et le code est libéré avant l’épinglage dans finally.

1 571 contrôles passent en Debug et en Release, avec zéro différence sur 771 exécutions de blocs par configuration. Les programmes de 64 instructions sont comparés au véritable Run du cœur existant sous budgets irréguliers, préchargement présent/absent et cache initial valide/invalide, refresh initial dû/différé, multiplicateurs 1/7/4 et ratio 49152/65536 avec arrondi par instruction. Les registres et tous les octets de Cpu68000State correspondent. Les autres cas portent sur mutations d’opcode/extension, opcode préchargé conservé et dernière instruction en bord de fenêtre. Douze blocs sont fermés et treize épinglages libérés par configuration, y compris la compilation refusée à la limite de fenêtre. Relevés : `attempts/m68000-x64-blocks/{Debug,Release}.json` ; harnais supprimé après consignation. Chips et module Sega compilent en Debug et Release (sans reconstruction des projets non utilisés) ; journaux dans le même dossier. Une invocation apply_patch avait initialement un mauvais chemin de projet Sega ; elle a été corrigée avant modification, sans autre échec de compilation/exécution.

Cette étape valide le fetch et les budgets des blocs sur fenêtres sans effets de bord, sans appels à l’interpréteur entre leurs instructions. Elle ne raccorde pas encore leur cache au Run de SegaEmu ni les accès mémoire des instructions, les périphériques, branches ou exceptions. Le chemin interprété par défaut reste inchangé. Aucune vitesse de jeu ni accélération de machine complète n’est déduite de ces contrôles.

## Boucle native optionnelle et durée de vie des caches

`Cpu68000.NativeBlocks.cs` ajoute RunNative sans rediriger ni modifier Run. La boucle conserve l’entrée IRQ, les budgets, les erreurs d’adresse et le nettoyage de cycle_end du cœur existant. Elle résout la fenêtre du bus avant chaque entrée, invalide les changements de banque/ratio et les blocs périmés qui n’ont rien consommé, utilise un cache LRU borné à 4 096 entrées et mémorise aussi les corps absents pour éviter leur recompilation répétée. Les instructions non traduites passent par le fetch et le handler existants : cette route est donc encore mixte, pas un dynarec complet. Les compteurs distinguent compilations, entrées natives et instructions de repli. Dispose ferme tous les blocs, y compris après une erreur de fermeture, et attend la fin d’un RunNative en cours via le verrou du cœur.

SegaBoard ferme désormais le cache du 68000 principal lors de son remplacement et de la destruction ; Scd ferme celui du secondaire avant de détacher les cartes mémoire. Les nettoyages restants restent dans des finally. Ces changements n’activent pas RunNative dans le chemin de jeu actuel : l’interpréteur reste par défaut, et la route compilée antérieure n’est pas présentée comme remplacée par ce backend encore partiel.

196 contrôles passent en Debug puis en Release. Ils comparent RunNative à Run sur des boucles mêlant registres, mémoire et branchements, avec code modifié, extension modifiée, changement de banque, changement de ratio, fenêtre refusée puis rétablie, IRQ effectivement acquittée, arrêt/reprise, erreur d’adresse et STOP. Registres, état brut et contenu des deux banques correspondent. Par configuration : 23 compilations natives, 465 entrées natives, 1 105 instructions de repli ; huit cœurs fermés et 46 épinglages libérés sur 46. Le premier passage donnait un contrôle échoué parce que le harnais n’avait pas activé aerr_enabled : aucun écart avant/après n’était alors observé, mais l’erreur d’adresse n’était pas exercée. Ce relevé reste conservé dans `Debug-initial.json`. Après correction de ce prérequis, le cas d’erreur d’adresse est explicitement confirmé.

Relevés : `attempts/m68000-native-runtime/{Debug,Release}.json`. Chips et Sega compilent dans les deux configurations, sans reconstruction des autres projets ; journaux dans le même dossier. Le harnais temporaire est supprimé après consignation. Aucune mesure de jeu/FPS n’a encore été faite avec cette boucle : elle doit encore recevoir les corps manquants et les accès natifs aux mémoires/périphériques avant les comparaisons MD/MCD demandées. Aucun travail 32X n’a été repris.

## Mesures FPS sur jeux : interpréteur, nouveau natif partiel et ancien CIL

Campagne `attempts/fps-current`, machines complètes sans fenêtre ni limiteur : Aladdin (Europe) Mega Drive et Batman Returns (Europe) Mega-CD, BIOS Mega-CD Europe v1.00. Protocole des notes : 60 trames de chauffe, puis 180 trames Debug / 900 Release, mêmes entrées repeat-start. Les temps additionnent seulement RunFrame ; chargement, empreintes et fermeture sont hors mesure. Les SHA des DLL effectivement chargées sont enregistrés dans chaque JSON. Les DLL antérieures aux changements de mapping/sélection sont conservées dans `baseline/{Debug,Release}`.

Le choix explicite `UseNative68000Blocks`, faux par défaut, raccorde RunNative aux 68000 principal et secondaire. Le Z80 reste interprété dans cette route : ce n’est pas encore le dynarec de toute la machine. L’ancien prototype CIL reste un choix distinct. Aucun backend GPU complet MD/MCD n’existe à cette étape ; aucun FPS n’est inventé pour une route GPU qui retomberait sur le CPU. Les lignes 32X et Mega-CD + 32X seront ajoutées quand des données nouvelles seront disponibles, après la priorité MD/MCD demandée ; aucun ancien relevé 32X n’est réutilisé ici.

| Jeu | Plateforme | Interpréteur Debug | GPU Debug | Natif 68000 partiel Debug | Ancien CIL Debug |
| --- | --- | ---: | --- | ---: | ---: |
| Aladdin (Europe) | Mega Drive | 31,84 | Non implémenté | 29,52 | 27,24 |
| Batman Returns (Europe) | Mega-CD | 29,88 | Non implémenté | 25,98 | 30,28 |

| Jeu | Plateforme | Interpréteur Release | GPU Release | Natif 68000 partiel Release | Ancien CIL Release |
| --- | --- | ---: | --- | ---: | ---: |
| Aladdin (Europe) | Mega Drive | 604,82 | Non implémenté | 552,26 | 30,81 |
| Batman Returns (Europe) | Mega-CD | 370,16 | Non implémenté | 281,10 | 51,56 |

Ces chiffres sont les relevés du passage principal, pas une garantie de débit constant. Le témoin conservé donne 31,92 / 29,13 FPS Debug et 659,99 / 328,98 FPS Release (MD/MCD). L’écart initial d’Aladdin Release a déclenché deux comparaisons supplémentaires sans changement de code : témoin/actuel 466,64 / 531,59, puis ordre inverse actuel/témoin 573,30 / 600,04. Le signe de l’écart n’est donc pas constant ; cette campagne ne démontre pas une perte précise attribuable aux changements de mapping. Aucun diagnostic de charge de la machine hôte n’est déduit de ces relevés. Les empreintes des images finales et de l’audio mesuré correspondent entre témoin, interpréteur actuel, natif partiel et ancien CIL sur les quatre scénarios principaux.

Le natif partiel n’a pas démontré le gain demandé. Les compteurs du passage complet, chauffe comprise, donnent 10 757 527 instructions de repli MD Release et 18 977 347 MCD Release ; ils ne doivent pas être comparés comme un pourcentage aux entrées natives, chacune pouvant exécuter plusieurs instructions. La table de données et son interface sont écrites, mais ne sont pas encore utilisées par RunNative : les corps mémoire restent à porter. Les mauvais résultats de l’ancien CIL sont conservés pour ne pas représenter ce prototype comme un backend performant.

Les seize passages principaux et quatre confirmations ont terminé sans plantage ; les machines ont été fermées dans finally. Chips et Sega ont été construits en Debug/Release, sans reconstruire les projets non concernés. Les relevés, journaux, DLL témoins et `summary.json` sont conservés ; le harnais `FpsCurrent` et ses bin/obj sont supprimés après consignation. Aucun commit. Les travaux mémoire en cours restent incomplets et l’interpréteur reste sélectionné par défaut.

## Accès de données natifs raccordés et nouveaux FPS

`M68000X64MemoryInstructionEmitter.cs` émet les adresses effectives et les lectures/écritures MOVE, ALU et unaires directement dans les fenêtres autorisées : octets avec XOR 1, mots alignés comme le bus, longs en deux mots et deux pages si nécessaire, masque 24 bits, A7 octet à pas de deux, pré/post-incréments ordonnés et index influencé par la modification du registre source. Les gardes lecture/écriture/adresse s’exécutent avant toute mutation de l’instruction. Les périphériques, pages refusées et erreurs d’adresse retournent vers le handler existant. La distinction MemoryDenied/CodeChanged empêche de recompiler sans fin un bloc qui rencontre simplement un périphérique.

`M68000X64MemoryMap` est maintenant possédé par le cœur natif et transmis aux blocs. Les pointeurs sont épinglés, les vues lecture/écriture identiques partagent leur épinglage et les révisions de mapping déclenchent un rafraîchissement. Les registres hôtes R13/R14/R15 sont préservés par le bloc pour table, valeur source et adresse destination. Le buffer natif reçoit le troisième argument de table et renvoie la cause de sortie. Les nettoyages ferment blocs puis table/pins. Aucun changement à Run.

5 901 contrôles réussissent en Debug et en Release : 744 cas de modes/tailles/alias/pages/refus/remapping/automodification. Les 728 cas principaux utilisent le corps natif sans instruction de repli ; les cas complémentaires vérifient explicitement les handlers et l’erreur d’adresse. Registres, état brut, mémoires et appels périphériques contrôlés correspondent au Run existant. Les 3 676 épinglages de chaque passage sont libérés. Les DLL Release d’avant raccordement, vérifiées identiques aux SHA de la campagne FPS précédente, sont conservées dans `attempts/m68000-native-memory/before-Release`. Relevés de correction dans le même dossier ; harnais supprimé après consignation.

Les jeux ont ensuite été remesurés avec le protocole inchangé (60 trames de chauffe, 180 Debug / 900 Release, machine complète sans limiteur). Les tableaux ci-dessous présentent le passage courant, sans le confondre avec le GPU complet qui reste non implémenté, ni avec l’ancien CIL.

| Jeu | Plateforme | Interpréteur Debug | GPU Debug | Dynarec 68000 partiel Debug |
| --- | --- | ---: | --- | ---: |
| Aladdin (Europe) | Mega Drive | 31,39 | Non implémenté | 30,62 |
| Batman Returns (Europe) | Mega-CD | 30,46 | Non implémenté | 27,11 |

| Jeu | Plateforme | Interpréteur Release | GPU Release | Dynarec 68000 partiel Release |
| --- | --- | ---: | --- | ---: |
| Aladdin (Europe) | Mega Drive | 548,75 | Non implémenté | 518,11 |
| Batman Returns (Europe) | Mega-CD | 356,09 | Non implémenté | 291,92 |

Le natif reste derrière l’interpréteur dans ces passages. La répétition des DLL natives d’avant mémoire donne 527,62 FPS MD et 272,11 MCD Release, contre 518,11 et 291,92 après raccordement : le gain n’est pas général. Les images et empreintes audio correspondent entre interpréteur, avant et après. Les compteurs, chauffe comprise, donnent 9 823 798 instructions de repli MD Release (avant 10 757 527) et 16 677 976 MCD (avant 18 977 347). Les entrées natives passent respectivement à 922 838 et 2 999 158 ; une entrée n’est pas une instruction, ces nombres ne donnent pas un taux de couverture natif. Le Z80 reste interprété.

Relevés FPS et SHA dans `attempts/native-memory-fps/summary.json` et les JSON individuels. Les dix passages (huit courants et deux témoins Release) terminent sans plantage et les boards sont fermés dans finally. Chips et Sega sont construits dans les deux configurations ; journaux conservés. Les nouveaux harnais `M68000NativeMemory` et `NativeMemoryFps` sont supprimés après consignation. La route est encore incomplète et ne remplit pas la demande de vitesse ; il reste à identifier le coût des transitions/handlers et à porter les corps encore absents avant le Z80 puis le 32X. Aucun gain ×10 n’est annoncé, aucun FPS GPU ou ancien FPS 32X n’est substitué aux mesures absentes.

## Diagnostic du coût d’entrée native

Le harnais `NativeCallCost` compare le delegate actuel et deux appels par pointeur, en conservant dans les trois cas la même protection SafeHandle (DangerousAddRef/Release), les mêmes arguments et la même fonction x64 feuille sans callbacks/blocage. Six passes de quatre millions d’appels, ordre tournant après échauffement, contrôlent le résultat de chaque somme. Médianes Debug : delegate 31,23 ns, pointeur standard 28,71 ns, pointeur SuppressGCTransition 22,74 ns. Médianes Release : 17,01 / 11,08 / 7,33 ns. Le buffer est fermé dans finally.

Les résultats ne sont pas des FPS de jeu. La différence Release delegate/candidat est d’environ 9,68 ns par entrée ; même appliquée aux 2 999 158 entrées du passage complet Mega-CD (chauffe comprise), elle représente environ 29 ms, soit moins de 1 % des 3,08 s mesurées pour les 900 trames. Elle ne suffit pas à expliquer la perte de la route native. Aucun changement de convention d’appel ni suppression de transition GC n’est retenu à cette étape ; il faut d’abord mesurer les causes de repli et les instructions manquantes. Relevés : `attempts/native-call-cost/{Debug,Release}.json`. Aucun échec du harnais ; source et bin/obj supprimés après consignation.

## Diagnostic source des deux voies perdantes

La baisse observée ne démontre pas que le GPU ou la recompilation seraient intrinsèquement lents ; elle montre que les deux chemins expérimentaux actuels ne déplacent pas le travail principal vers un chemin plus rapide.

**GPU : rendu encore CPU et aller-retour de framebuffer.** `src/RetroMediaLab.Chips/Video/Sega/VDP/Vdp.Rendering.cs`, `render_line`, exécute encore `render_bg` et `render_obj` sur le CPU. `Vdp.Output.cs` appelle ensuite `RemapLineCore`, qui convertit lui aussi les indices de palette en pixels CPU avant le rappel de ligne. `SegaBoard.32X.cs`, `Compose32XLine`, lit cette image CPU déjà produite ; `Gpu32XFrameComposer.cs` ne déplace au shader que le mélange final de la couche 32X, puis appelle `glReadPixels` pour remplir le framebuffer CPU attendu par `VideoFrame`. La voie mesurée ne déplace donc pas le VDP Mega Drive sur le GPU et paie un transfert GPU→CPU. Dans le chemin normal, `ManagedCore.StartSystem` ne fournit pas de compositeur dans `MachineResources`, `ManagedCore.Output.PublishVideo` copie ensuite le framebuffer CPU dans un nouveau tableau, et `OpenGlVideoSurface.Present` retransfère les pixels CPU dans une texture. `VideoFrame` ne transporte qu’une mémoire CPU ; `IEmulationHardwareRenderContext.TryReadFramebuffer` ne fournit également que des pixels relus. Le compositeur du banc n’est pas raccordé à l’application et ne constitue pas encore une voie GPU utilisable par SegaEmu en jeu.

Les points à reprendre pour un vrai chemin vidéo GPU sont donc le contrat de sortie `src/RetroMediaLab.Emulation/Contracts/VideoFrame.cs`, le propriétaire du contexte graphique `src/RetroMediaLab.Emulation/Interfaces/IEmulationHardwareRenderContext.cs`, la présentation `src/RetroMediaLab.App/Rendering/Emulation/Surfaces/OpenGlVideoSurface.cs`, l’injection de ressources `ManagedCore.cs`/`MachineResources.cs`, puis le rendu source dans `Vdp.Rendering.cs` et `Vdp.Output.cs`. Il faudra garder un chemin CPU fonctionnel et éviter une lecture GPU→CPU avant l’affichage ; modifier seulement `Gpu32XFrameComposer.cs` ne peut pas atteindre cet objectif.

**Dynarec : génération managed coûteuse et chemin chaud encore trop chargé.** `src/RetroMediaLab.Chips/CPU/Hitachi/SH2/Sh2.Blocks.cs` produit un `DynamicMethod` par bloc et appelle `RuntimeHelpers.PrepareDelegate` dès sa création. Les 5 346 blocs de la mesure longue mobilisent 16,87 s à générer/préparer en Release (41,6 % des 40,51 s chronométrées). Les segments tardifs restent à 46,15 puis 32,34 FPS contre 84,98 puis 90,40 pour l’interpréteur ; la perte ne se limite donc pas à un seul démarrage froid. L’émission actuelle réutilise les handlers CIL du cœur et conserve des appels d’aides par instruction ; elle n’a ni allocation statique des registres hôte ni liens natifs de blocs du compilateur PicoDrive. Le plafond de 128 instructions n’a pas réduit le nombre de blocs et a été retiré.

La collecte PicoDrive locale contient `cpu/sh2/compiler.c`/`.h`, mais pas leur backend x64 `cpu/drc/emit_x86.c` ni `cpu/drc/cmn.h`. La source Ares SH-2 référence son `recompiler.cpp` et `nall/recompiler/generic/generic.hpp`, absents eux aussi de la collecte ; `Gens/sh2a.asm` est un interpréteur assembleur, pas un dynarec. Le Dynarec CIL actuel est ainsi une adaptation locale, pas le portage complet d’un backend éprouvé. Pour une correction source fidèle, il faut d’abord disposer du backend complet à porter ; recopier une autre couche CIL ou accroître les blocs répéterait des choix déjà mesurés sans gain.

Conclusion : les routes ont été comparées correctement comme prototypes, mais mon implémentation GPU est partielle et mon Dynarec n’est pas équivalent au backend natif des émulateurs de référence. L’interpréteur reste intact, fonctionnel et par défaut. Les trois voies ne doivent pas être présentées comme trois implémentations d’accélération achevées.

### Profil des replis natifs avant TST et branches

Passe distincte des FPS, mêmes 60 trames de chauffe puis 180 Debug/900 Release. Les six profils CPU (MD principal, MCD principal/secondaire, deux configurations) conservent les mêmes images et audio. Aucune sortie pour code changé ni absence de fenêtre.

Release : Aladdin totalise 9 823 798 tentatives de repli, dont BEQ 4 378 563 et TST 4 274 658 (88 % ensemble). Batman Returns principal : 10 796 960 replis, dont BNE 4 716 546 et TST 4 678 609 (87 % ensemble). Secondaire : 5 881 016 replis, BNE 852 395, MOVE 815 797, BEQ 613 172. Les accès périphériques restent à conserver sur leurs handlers. Priorité : TST et branchements natifs, puis DBcc ; pas une modification de convention d'appel. Ces compteurs sont des tentatives de repli, pas une mesure du temps ni du nombre d'instructions natives. Preuves : attempts/native-coverage/summary.json et quatre profils détaillés. Profil optionnel désactivé par défaut ; Run interprété inchangé.

### TST et branchements 68000 natifs — contrôle des jeux

Le profil précédent a guidé une correction groupée : TST registres/mémoire sans écriture, BRA/Bcc/DBcc, préchargement conditionnel des extensions et retours natifs vers les instructions antérieures du même bloc. Les budgets et cycles restent ceux du cœur actuel ; aucun travail matériel n'est supprimé. L'émission de branchement reprend la structure translate_m68k_bcc/translate_m68k_dbcc de BlastEm, adaptée aux indicateurs et cycles existants. Run 68000 et Run Z80 n'ont aucun changement de contenu dans Git.

9 072 comparaisons complètes de contexte/mémoire par configuration, sans différence ni repli sur les cas contrôlés : conditions, branches courtes/word, DBcc avec expiration, conservation de la partie haute du registre, TST des trois tailles, pré/post-incréments, préchargement actif/inactif, multiplicateurs 1/7, rafraîchissement et boucles sous budget. Ces contrôles ne démontrent pas la couverture complète du 68000. Deux erreurs du nouveau harnais ont été corrigées avant les mesures : alias md transmis au lieu de MegaDrive et nom ModelConstants absent des imports. Aucun plantage de jeu constaté.

Machine complète sans fenêtre ni limiteur, mêmes médias et entrées, 60 trames de chauffe puis 180 Debug/900 Release. Le natif reste partiel : corps 68000 manquants en repli et Z80 interprété. Le GPU complet reste absent.

Debug (FPS) :

| Jeu | Plateforme | Interpréteur | GPU | Dynarec 68000 partiel |
| --- | --- | ---: | --- | ---: |
| Aladdin (Europe) | Mega Drive | 30,09 | Non implémenté | 33,46 |
| Batman Returns (Europe) | Mega-CD | 28,30 | Non implémenté | 28,99 |

Release, première passe (FPS) :

| Jeu | Plateforme | Interpréteur | GPU | Dynarec 68000 partiel |
| --- | --- | ---: | --- | ---: |
| Aladdin (Europe) | Mega Drive | 267,97 | Non implémenté | 417,52 |
| Batman Returns (Europe) | Mega-CD | 308,72 | Non implémenté | 281,47 |

Le témoin Aladdin était nettement inférieur au précédent 548,75 ; répétition native puis interpréteur, mêmes DLL et protocole, puis DLL conservées avant les nouveaux corps mémoire :

| Jeu | Plateforme | Interpréteur actuel | GPU | Dynarec 68000 partiel | Interpréteur DLL conservées |
| --- | --- | ---: | --- | ---: | ---: |
| Aladdin (Europe) | Mega Drive | 525,67 | Non implémenté | 642,39 | 637,20 |
| Batman Returns (Europe) | Mega-CD | 362,10 | Non implémenté | 265,64 | 360,28 |

Toutes les images finales et empreintes audio des quatorze passages correspondent aux références précédentes. La baisse Mega-CD native reste présente dans les deux passages Release. Pour Aladdin, les relevés ne permettent pas d'affirmer un gain stable face aux DLL conservées, malgré le meilleur natif actuel. La variation du témoin Aladdin est conservée dans les résultats, sans lui inventer une cause. Aucun facteur dix constaté ou garanti.

Les replis chauffe comprise passent de 2 317 129 à 230 077 sur MD Debug et de 9 823 798 à 563 078 sur MD Release ; de 4 086 214 à 1 192 467 sur MCD Debug et de 16 677 976 à 4 001 786 sur MCD Release. Réduire les replis ne suffit donc pas à établir un gain global. À garder pour la suite : la couverture TST/branches vérifiée, la référence interprétée indépendante et les preuves. À rectifier : coût natif Mega-CD, corps encore absents et route Z80 avant de qualifier le backend de dynarec machine complète. Ne pas basculer le défaut sur cette route. Ne pas reprendre le 32X avant validation MD/MCD.

Preuves et SHA : attempts/native-control/summary.json, checks-Debug.json, checks-Release.json et JSON individuels. Builds ciblés Chips/Sega réussis dans les deux configurations, avertissements préexistants et NU1900 consignés. Aucun commit. Le harnais temporaire NativeControl, ses copies de DLL et bin/obj sont supprimés après fermeture des cœurs/boards dans finally.

### Coûts natifs Mega-CD après TST/branches

Passe instrumentée distincte des FPS, 60+900 trames Batman Returns. Images/audio identiques aux références. Principal : RunNative 600,80 ms, sélection native incluse 317,93 ms dont compilation 55,76 et exécution 99,19, handlers de repli 236,10 ms. Secondaire : 1 237,95 ms, sélection 706,91 dont compilation 101,72 et exécution 196,15, replis 330,63 ms. Les compteurs ajoutent eux-mêmes du coût ; ces durées ne sont pas les FPS de la passe sans profil, et les colonnes inclusives ne doivent pas être sommées.

Replis fréquents : principal RTS 57 951, LSR.W immédiat 53 503, MOVE SR/CCR 43 773 chacun, BSR.W 37 905. Secondaire BTST absolu court 530 116, RTS 420 634, BSR.W 369 177, DIVS déplacement 198 003, MOVE post-incrément 197 998, MULS déplacement 155 236. Sorties refus mémoire : 98 667 principal, 1 045 230 secondaire. Prochaine correction justifiée : pile BSR/RTS native avec validation des deux mots et erreurs avant mutation ; garder les handlers des banques périphériques refusées. Ne pas remplacer ce diagnostic par une convention d'appel seule ni retirer des cycles. Preuve : attempts/native-mcd-cost/profile-Release.json. Harnais temporaire supprimé après fermeture de la machine.

### Pile BSR/RTS native et fin des blocs inconditionnels

Portage de translate_m68k_bsr/translate_m68k_rts de BlastEm m68k_core.c vers les accès directs déjà présents. BSR empile le PC de retour après extensions, RTS dépile le PC ; ni les indicateurs ni les cycles ne sont simplifiés. Les deux mots sont validés avant mutation, avec repli sur handlers pour page refusée/erreur d'adresse. Le bloc se termine maintenant après BSR, RTS et BRA : la continuation séquentielle qu'il générait après ces transferts était inaccessible. Run interprété inchangé.

1 792 comparaisons complètes de contexte et mémoire par configuration : piles traversant des pages, alias pile/code, préchargement actif/inactif, adresses paires/impaires, erreurs activées/désactivées, page secondaire autorisée/refusée, multiplicateurs 1/7 et ratio 3/2. Aucun écart. 368 replis protégés par configuration ; les refus ne sont pas présentés comme une exécution native de leur instruction. Les huit passages de jeu finissent et gardent les mêmes images/audio que les références précédentes.

Debug, 60 trames de chauffe puis 180 mesurées (FPS) :

| Jeu | Plateforme | Interpréteur | GPU | Dynarec 68000 partiel |
| --- | --- | ---: | --- | ---: |
| Aladdin (Europe) | Mega Drive | 30,82 | Non implémenté | 31,37 |
| Batman Returns (Europe) | Mega-CD | 28,59 | Non implémenté | 29,08 |

Release, 60 trames de chauffe puis 900 mesurées (FPS) :

| Jeu | Plateforme | Interpréteur | GPU | Dynarec 68000 partiel |
| --- | --- | ---: | --- | ---: |
| Aladdin (Europe) | Mega Drive | 478,60 | Non implémenté | 605,84 |
| Batman Returns (Europe) | Mega-CD | 358,57 | Non implémenté | 341,16 |

Le natif Mega-CD reste inférieur de 4,86 % au témoin de cette passe, contre 265,64/362,10 dans la dernière répétition avant pile. La réduction observée de cet écart justifie de conserver ce portage expérimental pour poursuivre, mais les variations du témoin MD empêchent de calculer un gain stable entre campagnes. Les gains Debug ne sont pas généraux par rapport au natif de la campagne précédente (Aladdin 33,46 puis 31,37). Aucune route complète validée, aucune accélération dix fois annoncée, défaut toujours interprété. Reste à traiter la couverture manquante et le coût des replis périphériques avant le Z80 natif et le 32X.

Les erreurs du nouveau harnais et les essais défavorables antérieurs restent consignés dans leurs sections ; ne pas refaire les variantes vidéo Span, la convention d'appel seule ou le prototype CIL comme s'il s'agissait de nouvelles pistes. Preuves : attempts/native-stack/summary.json et checks-{Debug,Release}.json. Les cœurs et boards ont été fermés dans finally. Le harnais NativeStack, ses bin/obj et copies de DLL sont supprimés après consignation. Aucun commit.

### Instructions de bits et multiplication natives

BTST/BCHG/BCLR/BSET registres/mémoire suivent translate_m68k_bit de BlastEm ; conservation du masque réellement testé dans not_z_flag et des cycles supplémentaires sur le mot haut. MULS/MULU suivent translate_m68k_mul ; les cycles variables du cœur actuel sont calculés directement par comptage parallèle des bits sur 16 bits, sans callback ni boucle dépendant de la valeur. Les adresses relatives au PC après un numéro de bit immédiat utilisent la base située après cette extension.

6 872 comparaisons complètes de contexte/mémoire par configuration, aucune différence, dont sources identiques au registre destination, numéros de bits aux bornes et modulo, préchargement, adressages mémoire/PC/indexés, source immédiate, résultats signés/non signés et ratio 3/2 avec multiplicateur 7. Quinze cas nécessitent le repli attendu pour les banques de données refusées. Huit passages de jeux : mêmes images/audio que les témoins précédents.

| Debug — jeu | Plateforme | Interpréteur FPS | GPU | Dynarec partiel FPS |
| --- | --- | ---: | --- | ---: |
| Aladdin | Mega Drive | 29,10 | Non implémenté | 31,45 |
| Batman Returns | Mega-CD | 27,64 | Non implémenté | 30,28 |

| Release — jeu | Plateforme | Interpréteur FPS | GPU | Dynarec partiel FPS |
| --- | --- | ---: | --- | ---: |
| Aladdin | Mega Drive | 639,95 | Non implémenté | 621,93 |
| Batman Returns | Mega-CD | 347,67 | Non implémenté | 318,14 |

Le natif reste inférieur en Release dans cette passe. La couverture supplémentaire est fonctionnelle, mais ne démontre pas une accélération. Le Mega-CD conserve 2 668 119 replis et 4 565 858 entrées natives, chauffe comprise. Les retours vers le dispatcher C# à chaque bloc restent un défaut architectural identifié dans le code et cohérent avec le profil inclusif de sélection antérieur ; leur part exacte après cette passe n'est pas remesurée. Prochaine étape : liens natifs entre blocs déjà publiés et vérifiés, avec budget, invalidation et durée de vie du propriétaire, plutôt qu'une nouvelle retouche d'appel isolée. Preuves : attempts/native-bit-multiply/summary.json, contrôles et journaux. Harnais temporaire supprimé après fermeture des cœurs/machines ; aucun commit.

### Liens natifs entre blocs publiés

Une table paginée hôte des entrées compilées est maintenant raccordée au runtime, sur le modèle des recherches d'adresses natives BlastEm. Le transfert terminal vérifie budget, alignement et PC 24 bits, restaure le cadre hôte puis entre dans le bloc suivant. Les compteurs NativeBlockEntryCount comptent désormais les entrées depuis C#, pas les blocs traversés dans une chaîne. Ils ne constituent pas un nombre d'instructions.

Le propriétaire Cpu68000 garde tous les buffers vivants sous son verrou pendant la chaîne. Les pointeurs sont retirés avant libération et invalidés sur révision mémoire, changement de ratio, éviction et Dispose. Un refus mémoire ou un code modifié retourne au gestionnaire ; la sortie identifie le bloc réellement concerné, pas seulement celui où la chaîne avait commencé. Les bus sans révision de mémoire directe gardent la route sans lien.

44 contrôles par configuration : contexte/mémoire identiques sur boucles appels/retours, budgets, mutations au début et à l'intérieur d'un bloc cible, remapping de fenêtre, refus des données, restauration des données, ratio modifié, retrait des entrées et fermeture idempotente. Chaque contrôle de boucle à chaud parcourt ses blocs jusqu'au budget dans une seule entrée C# (quatre entrées pour quatre runs). Les quatre contrôles de table utilisent directement le même fichier source compilé dans le harnais ; les contrôles CPU exécutent les DLL Chips construites. L'exigence initiale du banc sur le nombre d'entrées incluait une chauffe trop courte pour le multiplicateur 7 ; chauffe corrigée, aucun changement de production motivé par cet échec. Imports internes/unsafe et conversion nint du harnais corrigés avant ces contrôles.

Machine complète, protocole conservé, 60+180 Debug et 60+900 Release :

| Debug — jeu | Plateforme | Interpréteur FPS | GPU | Dynarec partiel FPS |
| --- | --- | ---: | --- | ---: |
| Aladdin | Mega Drive | 25,87 | Non implémenté | 28,08 |
| Batman Returns | Mega-CD | 24,48 | Non implémenté | 24,09 |

| Release — jeu | Plateforme | Interpréteur FPS | GPU | Dynarec partiel FPS |
| --- | --- | ---: | --- | ---: |
| Aladdin | Mega Drive | 283,66 | Non implémenté | 363,00 |
| Batman Returns | Mega-CD | 166,51 | Non implémenté | 176,32 |

Images et audio des huit passages identiques aux références précédentes. Les témoins FPS de cette campagne sont nettement plus bas que ceux de la précédente ; aucune cause n'est établie et aucune baisse n'est attribuée arbitrairement à la machine hôte. Ces chiffres sont consignés tels quels et ne prouvent pas que les liens accélèrent les jeux par rapport à la version native sans liens. Ils prouvent la conservation des sorties et la diminution des retours C# à travail émulé identique : Aladdin Debug 436 932 vers 256 562 entrées, Release 1 309 453 vers 724 275 ; les replis sont inchangés. Mega-CD Debug 1 443 701 vers 1 128 402 entrées. Les replis encore présents empêchent de qualifier cette route de dynarec complet. La correction de vitesse Mega-CD reste ouverte ; ne pas déclarer validée la route native complète ni reprendre le 32X. GPU complet toujours absent. Aucun commit.

Preuves : attempts/native-links/summary.json, checks-Debug.json, checks-Release.json, SHA et journaux. Le harnais NativeLinks et ses bin/obj sont supprimés après fermeture des cœurs et machines dans finally. Les modifications Run 68000 et Z80 sont toujours absentes du diff de contenu.

### Décalages registres et transferts SR/CCR natifs

ASR/ASL/LSR/LSL registres, comptes immédiats ou issus de registre, suivent translate_shift de BlastEm tout en conservant les expressions exactes des indicateurs du cœur existant, y compris les comptes nuls ou supérieurs à la taille. MOVE depuis SR et vers CCR est raccordé pour registres/mémoire avec gardes ; MOVE vers SR et les rotations restent sur leurs handlers. Le principal et le secondaire déclarent une capacité optionnelle de rafraîchissement dans Chips, sans dépendance du chip vers Sega.

La condition secondaire de SKIP_BUS_REFRESH utilise le principal. Le code existant Scd peut avancer le principal dans un handler : le résultat externe est donc échantillonné avant chaque chaîne native après les replis, et non une seule fois au début de RunNative. Il reste stable à l'intérieur de la chaîne, qui ne rappelle pas les handlers. Les bus qui ne déclarent pas cette propriété gardent le repli sur les instructions qui en dépendent. La condition locale compare les cycles après leur ajout variable, comme le handler.

52 426 contrôles complets de contexte/mémoire par configuration, aucun écart final : trois tailles, quatre opérations, comptes registre 0..63 et immédiats 1..8, valeurs de bord, préchargement actif/inactif, fenêtres locales/externes vraies/fausses ou non déclarées, 32 combinaisons CCR et modes mémoire/PC, banques refusées. 10 756 replis attendus, principalement les cas où le bus ne déclare pas de condition native. Le contrôle externe fait changer le résultat par un handler d'écriture entre deux entrées natives ; il correspond au risque identifié dans Scd. Un dernier contrôle vérifie qu'une erreur de lecture de capacité remet cycle_end à zéro, puis ferme le cœur.

Défauts rencontrés et corrigés dans cette passe : le premier émetteur LSR conservait dans N des bits sans effet que le handler remet à zéro ; il a été corrigé avant les jeux. La lecture de capacité native a été placée dans le try/finally de RunNative. Le nouveau cas externe du harnais utilisait un nom local ea déjà employé dans une boucle ; renommé avant son exécution. Les FPS ci-dessous précèdent le dernier déplacement de la lecture dans le try/finally ; les contrôles complets ont ensuite été rejoués avec ce déplacement. Les SHA des DLL effectivement mesurées restent dans chaque JSON.

| Debug — jeu | Plateforme | Interpréteur FPS | GPU | Dynarec partiel FPS |
| --- | --- | ---: | --- | ---: |
| Aladdin | Mega Drive | 27,91 | Non implémenté | 21,80 |
| Batman Returns | Mega-CD | 20,82 | Non implémenté | 25,26 |

| Release — jeu | Plateforme | Interpréteur FPS | GPU | Dynarec partiel FPS |
| --- | --- | ---: | --- | ---: |
| Aladdin | Mega Drive | 629,31 | Non implémenté | 614,73 |
| Batman Returns | Mega-CD | 339,48 | Non implémenté | 302,31 |

Même protocole machine complète sans fenêtre/limiteur, 60 trames de chauffe puis 180 Debug/900 Release ; images finales et audio identiques aux références. Le natif perd encore en Release et Aladdin perd dans cette passe Debug ; les corps supplémentaires ne constituent pas une accélération globale validée. Les replis Release tombent à 252 296 MD et 1 914 802 MCD, mais ce compteur ne prouve pas la vitesse. Les 925 blocs générés MD Debug restent proches des 906 antérieurs, ce qui ne démontre pas une explosion des défauts de cache ; leur nombre ne mesure pas leur coût.

Relevé du coût de génération ajouté uniquement lors d'un défaut de cache, sans timer par instruction. Pendant les 900 trames Release MD : émission native 54,44 ms après 57,39 ms de chauffe, JIT .NET cumulé 482,75 ms contre 383,70 sur l'interpréteur ; temps de passe native 1,464 s. MCD : émission 154,97 ms après 78,98 ms de chauffe, JIT .NET 1 186,93 ms contre 1 111,69 ; passe native 2,977 s. Le temps JIT est cumulé dans le processus et peut s'exécuter sur un autre thread ; il peut chevaucher l'émission et la passe. Ne pas additionner ces colonnes ni les soustraire du temps réel pour fabriquer des FPS. Elles montrent que la chauffe actuelle ne termine pas toutes les compilations ; elles n'expliquent pas à elles seules l'écart d'exécution.

À poursuivre : DIVS/DIVU, autres corps 68000, Z80 natif et vrai backend GPU. Ni le défaut interprété ni Run n'ont été remplacés. La demande de vitesse native MD/MCD reste non satisfaite ; le 32X attend. Preuves : attempts/native-shift-status/summary.json et checks-{Debug,Release}.json, journaux des builds ciblés Chips/Sega et SHA. Harnais NativeShiftStatus et bin/obj supprimés après fermeture des cœurs/machines dans finally ; aucun commit.

### Changement d'angle : chemin de sélection et révisions de code

À la demande de changer d'approche, la couverture des opcodes est mise en attente. Le dispatcher natif faisait encore une résolution de fenêtre, une recherche par dictionnaire, une réorganisation de liste et une publication de lien sur chaque passage. La recherche get_native_address de BlastEm utilise un index paginé. Le runtime actuel a été repris sur cette organisation : index paginé des entrées, conservation des fenêtres selon leur révision explicite, publication des pointeurs une fois par génération. La liste de conservation bornée est maintenant FIFO, mise à jour aux allocations/évictions seulement. Le dictionnaire sert à la possession/libération et aux PC hors 24 bits, pas à chaque recherche 24 bits.

Le nouveau contrat optionnel ICpu68000VersionedCodeBus déclare les changements d'identité/base/politique des fenêtres exécutables. Les mutations de leurs octets restent contrôlées par les gardes natives ; les entrées sans corps relisent leur opcode même sans changement de révision. Le code et les données ont deux contrats de révision distincts : les chaînes sont désactivées pour les bus ne déclarant pas la révision du code. Cela corrige l'hypothèse antérieure selon laquelle la révision des seules données couvrirait toujours le code. SegaEmu utilise le même compteur existant pour les deux, mais les autres bus n'ont pas à faire cette supposition.

5 055 contrôles par configuration, contexte/mémoire identiques à Run : fenêtres versionnées ou non, accès à chaud, mutation sans révision, remplacement du seul code sans révision des données, refus/rétablissement de fenêtre, mutation d'un opcode sans corps en opcode natif, changement de ratio, fermeture idempotente, 5 000 entrées traversant la limite de cache puis retour sur une entrée évincée. Les cas versionnés vérifient zéro nouvelle résolution de fenêtre sur la boucle chaude ; les non versionnés continuent de la résoudre. Les contrôles ne prouvent pas un gain de FPS sur les jeux.

Comparaison des DLL avant/après conservées, même harnais, mêmes médias/entrées, 60 trames de chauffe puis 180 Debug/900 Release. Les seize images finales et audio correspondent aux références, et les SHA effectivement chargés correspondent à chaque paire attendue.

| Debug — jeu | Plateforme | Interpréteur avant | Interpréteur après | Natif avant | Natif après |
| --- | --- | ---: | ---: | ---: | ---: |
| Aladdin | Mega Drive | 32,22 | 33,37 | 34,35 | 33,14 |
| Batman Returns | Mega-CD | 30,53 | 30,70 | 32,50 | 33,13 |

| Release — jeu | Plateforme | Interpréteur avant | Interpréteur après | Natif avant | Natif après |
| --- | --- | ---: | ---: | ---: | ---: |
| Aladdin | Mega Drive | 377,52 | 519,20 | 438,23 | 592,49 |
| Batman Returns | Mega-CD | 318,22 | 316,21 | 271,19 | 273,26 |

Conclusion limitée : le changement ne résout pas la perte Mega-CD. L'augmentation Release MD ne peut pas être attribuée au cache, puisque son témoin interprété augmente lui aussi fortement ; l'évolution du rapport natif/interpréteur ne démontre pas un gain général. En Debug MD, la variante donne moins de FPS que le natif précédent. Ne pas présenter l'index paginé comme une optimisation de vitesse validée. Il constitue un raccordement fonctionnel et une correction de l'hypothèse de validité des fenêtres, à garder sous la route expérimentale tandis que l'interpréteur reste le défaut.

Erreur du harnais corrigée : les anciennes DLL copiées sous before-Debug devenaient des fichiers candidats pour MSBuild et passaient avant les HintPath explicites lors du build Release. L'ordre AssemblySearchPaths du seul harnais a été corrigé pour privilégier HintPath ; aucune mesure Release n'avait démarré avec la mauvaise résolution. Le journal resolve-runner-Release.log conserve le chemin qui l'a montré. Aucun changement de compilation du produit n'a été fait pour ce problème du banc.

Nouvelle priorité architecturale, avant les autres opcodes : conservation de l'état CPU dans les registres hôte et écriture du contexte aux frontières nécessaires. BlastEm native_reg et les affectations opts->dregs/aregs/gen.cycles exploitent ces registres ; le backend actuel charge et stocke encore les champs du contexte à de nombreuses étapes de chaque instruction. Cette différence est vérifiée dans les sources, mais son gain FPS reste à démontrer ; ne pas le promettre avant implémentation et comparaison.

Preuves : attempts/native-entry-path/summary.json, checks-{Debug,Release}.json, before/hashes.json et binaires témoins. Harnais temporaire, copies de test et bin/obj supprimés après fermeture des cœurs/machines dans finally. Les binaires témoins et relevés restent. Pas de nouveau portage d'opcodes pendant ce changement d'angle, GPU complet absent, 32X toujours en attente, aucun commit.

### Contexte en registres hôte et cadre partagé

Cycles gardés dans RBP, PC dans RSI, mot préchargé dans RDI si actif. Tous les émetteurs passent par le même contexte ; les additions directes de cycles ont été supprimées. Les blocs liés du même cœur partagent le cadre sans le reconstruire ni écrire/recharger ces champs entre eux. Chaque sortie écrit les champs avant de restituer les registres hôte. Les registres invités et drapeaux gardent encore leur ancienne représentation. 5 592 contrôles complets par configuration, aucun écart : budgets, arrondis, conditions/DBcc, refus après préfixe, erreurs, préchargement, auto-modifications, chaînes et remapping.

| Debug — jeu | Plateforme | Interpréteur avant | Interpréteur après | Natif avant | Natif après |
| --- | --- | ---: | ---: | ---: | ---: |
| Aladdin | Mega Drive | 27,40 | 27,97 | 31,15 | 29,62 |
| Batman Returns | Mega-CD | 26,89 | 26,10 | 28,24 | 28,96 |

| Release — jeu | Plateforme | Interpréteur avant | Interpréteur après | Natif avant | Natif après |
| --- | --- | ---: | ---: | ---: | ---: |
| Aladdin | Mega Drive | 622,69 | 488,52 | 527,13 | 599,75 |
| Batman Returns | Mega-CD | 339,79 | 348,46 | 329,66 | 299,03 |

Protocole 60+180 Debug/60+900 Release, mêmes médias/entrées, seize images/audio identiques. Les SHA chargés correspondent aux DLL sauvegardées/actuelles. Pas de gain global validé : perte MD Debug et MCD Release. Le code interprété est inchangé mais les témoins FPS varient ; ne pas attribuer toute variation du natif à cette modification.

Cœur isolé distinct : même Cpu68000, profil multiplicateur 7, boucle synthétique ADDQ/BSR/EOR/LSR/RTS/BRA et bus plat de test, sans Z80/VDP/audio/CD. Chauffe réelle une seconde, restauration du contexte CPU et de la même mémoire avant chaque passage ; aucun save-state machine/CD. Un milliard d'unités de cycle par passage, dix passages natifs cumulés et un passage interprété. État final et mémoire identiques, zéro compilation native/repli pendant la mesure. Ce ne sont pas des FPS de jeu.

| Configuration | Interpréteur avant/après, milliards unités/s | Natif avant/après, milliards unités/s |
| --- | ---: | ---: |
| Debug | 0,574 / 0,522 | 20,35 / 31,80 |
| Release | 9,51 / 10,09 | 23,04 / 34,03 |

La boucle native gagne environ 56 % Debug et 48 % Release sur ces passages ; elle était déjà rapide avant. Ce résultat ne prédit pas le gain des jeux et oblige à regarder aussi les sorties du compilé et le reste de la machine. Le compteur JIT de la série inclut les restaurations hors chronomètre ; ne pas le soustraire du temps natif.

Diagnostic JIT MCD pendant la passe froide native : avant 1 115,63 ms cumulées et émission 130,95 ms ; après 1 350,13 et 168,27 ms. Ces temps peuvent se chevaucher et le JIT peut travailler sur un autre thread : ne pas les additionner pour fabriquer des FPS. Témoin supplémentaire : une première machine chauffe le CLR, est fermée, puis une machine fraîche rejoue les mêmes 60+900 trames sans restauration machine. Images/audio toujours identiques :

| Mega-CD Release — CLR chauffé | Interpréteur FPS | Natif FPS | JIT natif ms | Émission native ms |
| --- | ---: | ---: | ---: | ---: |
| Avant | 546,56 | 663,69 | 247,27 | 46,86 |
| Après | 747,71 | 696,35 | 223,62 | 44,55 |

Le JIT diminue mais reste non nul, et le témoin interprété varie beaucoup. Cela ne valide pas un gain général ni la disparition de la perte native. Les environ 1,91 million de replis MCD restent. La demande utilisateur de terminer 68000/Z80 et accès bus en dynarec n'est pas satisfaite.

Erreur du nouveau harnais corrigée : parenthèse surnuméraire dans son retour anonyme. La demande suivante est de retirer les calculs inutiles, notamment la reconstruction des bits internes de drapeaux non consommés, puis terminer les chemins manquants. Les états visibles, registres, mémoires et sorties restent à contrôler ; reproduire les bits ignorés du contexte n'est pas une exigence de perfection matérielle imposée par l'utilisateur.

Preuves : attempts/native-host-state/summary.json, core-summary.json, warm-summary.json, checks-{Debug,Release}.json et DLL témoins. Vingt sorties machine et huit sorties cœur vérifiées, avec SHA. Harnais NativeHostState, copies de test et bin/obj supprimés après fermeture des cœurs/machines dans finally. Aucun commit. Interpréteur inchangé et défaut, GPU complet absent, 32X en attente du résultat MD/MCD.

### Drapeaux natifs : suppression des calculs internes inutiles

Les ALU x86-64 utilisent maintenant leur taille invitée et capturent directement SF/OF/CF/ZF. Suppression des reconstitutions de bits internes non consommés, de la reconstruction spéciale de NEG et de la lecture supplémentaire de EXT.W. Les champs N/V/C/X sont comparés sur leurs bits utilisés, Z sur zéro/non-zéro ; les autres champs, registres et mémoires restent comparés exactement. L’interpréteur Run est inchangé.

21 648 contrôles réussis par configuration, y compris transitions ADDX/SUBX/ABCD/SBCD/ROXL, sauvegarde/rechargement CPU et accès mémoire. Machine complète : 60 trames de chauffe puis 180 Debug/900 Release, calcul audio en mémoire, sans fenêtre/limiteur. Images, audio et nombre d’échantillons identiques pour les seize passages.

| Configuration | Jeu | Interpréteur avant → après | Dynarec avant → après |
| --- | --- | ---: | ---: |
| Debug | Aladdin MD | 18,29 → 30,61 | 18,28 → 33,28 |
| Debug | Batman Returns MCD | 15,52 → 25,63 | 14,59 → 28,00 |
| Release | Aladdin MD | 182,59 → 271,01 | 192,81 → 316,38 |
| Release | Batman Returns MCD | 196,56 → 274,11 | 159,11 → 256,70 |

La variation importante du témoin interprété inchangé empêche d’attribuer ces écarts avant/après à cette suppression. La route native MCD reste derrière le témoin contemporain en Release ; aucun gain général ni restauration des anciens FPS n’est déclaré. Le GPU complet reste absent. Preuves : `artifacts/Emulation/segaemu-autres-jeux/attempts/native-host-flags/`, SHA DLL dans chaque passage. Le dynarec reste incomplet ; ne pas confondre ce contrôle avec son achèvement.

### DIVS/DIVU x86-64

Le calcul et les chemins de débordement sont émis en x86-64 ; la division signée utilise un dividende 64 bits pour ne jamais déclencher de faute hôte sur INT32_MIN/-1. Les cycles variables et le cas spécial de l’interpréteur sont conservés. Les zéros sont rejetés avant les effets de bord et utilisent encore le trap existant. 13 760 comparaisons par configuration : registres, immédiats, indirect/post-incrément/pré-décrément, valeurs limites et aléatoires, préchargement, refresh et ratio. Chaque cas non nul est aussi réexécuté avec code stable et vérification explicite de zéro repli vers un handler ; les invalidations après changement de code sont distinguées.

| Configuration | Jeu | FPS interpréteur | FPS GPU | FPS dynarec partiel |
| --- | --- | ---: | --- | ---: |
| Debug | Aladdin — MD | 30,45 | Non implémenté | 34,36 |
| Debug | Batman Returns — MCD | 27,85 | Non implémenté | 28,49 |
| Release | Aladdin — MD | 642,45 | Non implémenté | 618,75 |
| Release | Batman Returns — MCD | 321,44 | Non implémenté | 315,73 |

Même protocole 60+180/900, mêmes entrées, image/audio identiques par configuration. Ces chiffres ne constituent pas un gain général : le dynarec reste derrière en Release. Les instructions manquantes, les accès bus et le Z80 restent à porter. Preuves : `attempts/native-divide/`. Aucun changement de Run interprété ni commit.

### Transferts natifs : LEA, PEA, JMP, JSR, LINK, UNLK, MOVEM

Les adresses et listes de registres sont spécialisées à la compilation. MOVEM vérifie les deux extrémités de sa plage contiguë (au plus deux pages), avant toute écriture ; les registres invités ne sont pas modifiés pendant cette vérification. Les cas A7, les listes vides, les déplacements PC relatifs et les refus de pages sont contrôlés. Le diviseur précédemment lu en préflight est désormais réutilisé sans seconde lecture.

10 112 comparaisons par configuration réussies, contextes complets et mémoires identiques. Chaque transfert admissible est réexécuté après stabilisation du code et doit ne pas revenir à un handler.

| Configuration | Jeu | FPS interpréteur | FPS GPU | FPS dynarec partiel |
| --- | --- | ---: | --- | ---: |
| Debug | Aladdin — MD | 31,42 | Non implémenté | 30,05 |
| Debug | Batman Returns — MCD | 28,40 | Non implémenté | 31,32 |
| Release | Aladdin — MD | 633,18 | Non implémenté | 627,05 |
| Release | Batman Returns — MCD | 326,23 | Non implémenté | 287,67 |

Protocole 60+180/900 identique ; images/audio identiques. Couverture fonctionnelle étendue, mais perte MCD Release toujours présente : ne pas déclarer la route native complète ni plus rapide. Preuves `attempts/native-transfers/` ; interpréteur inchangé. Aucun commit.

### Retenue et rotations natives

ADDX/SUBX/NEGX et ROL/ROR/ROXL/ROXR sont émis en x86-64, en registres et dans les modes mémoire décodés. Z cumulatif est conservé ; NEGX conserve la formule de V du cœur existant. La normalisation des comptes de rotation utilise une multiplication réciproque exacte sur 0..63, sans division hôte ; RCL/RCR 32 bits traite explicitement le compte 32.

223 168 comparaisons par configuration réussies : tailles, comptes, retenues, Z et adressages, préchargement, refresh et ratios. Chaque cas est aussi réexécuté avec code stable et doit rester natif.

| Configuration | Jeu | FPS interpréteur | FPS GPU | FPS dynarec partiel |
| --- | --- | ---: | --- | ---: |
| Debug | Aladdin — MD | 29,50 | Non implémenté | 31,82 |
| Debug | Batman Returns — MCD | 27,54 | Non implémenté | 29,28 |
| Release | Aladdin — MD | 518,53 | Non implémenté | 645,75 |
| Release | Batman Returns — MCD | 320,10 | Non implémenté | 280,40 |

Protocole 60+180/900, images et audio identiques. La couverture augmente ; la perte MCD Release reste à corriger et interdit de déclarer le travail fini. Les témoins interprétés varient entre campagnes : ne pas calculer un gain d’un groupe à l’autre en mélangeant leurs témoins. Preuves `attempts/native-extended/`. Aucun commit.

### Décimal, conditions, CCR, TAS, décalages mémoire et MOVEP natifs

Scc partage l’évaluation des conditions des branches. TAS respecte WriteTasMemory, OR/AND/EOR CCR ne modifie que CCR. Les décalages mémoire à un bit utilisent les indicateurs x86-64 à la taille mot. ABCD/SBCD/NBCD conservent Z cumulatif et le comportement des chiffres invalides. MOVEP traite les octets espacés et signe son déplacement mot.

584 340 comparaisons par configuration réussies, dont le domaine complet des deux opérandes ABCD/SBCD et de X/Z, contextes/mémoires identiques et réexécution native obligatoire sur code stable.

**Debug — FPS**

| Jeu | Plateforme | Interpréteur | GPU | Dynarec partiel |
| --- | --- | ---: | --- | ---: |
| Aladdin | Mega Drive | 31,88 | Non implémenté | 31,96 |
| Batman Returns | Mega-CD | 27,74 | Non implémenté | 30,03 |

**Release — FPS**

| Jeu | Plateforme | Interpréteur | GPU | Dynarec partiel |
| --- | --- | ---: | --- | ---: |
| Aladdin | Mega Drive | 589,83 | Non implémenté | 650,43 |
| Batman Returns | Mega-CD | 362,84 | Non implémenté | 308,54 |

Protocole 60+180/900, images/audio/nombres d’échantillons identiques. Perte MCD Release toujours présente. Compteurs des passages non profilés, chauffe comprise : 53 787 replis MD Debug, 127 494 MD Release ; MCD Release 99 261 sur le principal et 1 448 937 sur le secondaire.

Profil séparé MCD Release, mêmes sorties : 3 217 replis sans corps natif sur le principal, 21 466 sur le secondaire. Les refus mémoire dominent les sorties (175 436 principal, 2 573 066 secondaire ; une sortie n’est pas nécessairement un repli d’instruction). Les premiers handlers restants concernent des MOVE et les accès pile BSR/JSR/MOVEM. Ne pas présenter ces refus comme des opcodes non portés. Les cycles de sélection/compilation/exécution restent inclusifs selon les champs du profil, ne pas les additionner.

Les accès bus à handlers et le Z80 restent à porter avant toute déclaration de route complète ; le GPU complet reste absent. Preuves `attempts/native-remainder/`. Sources interprétées inchangées. Aucun commit.
# Lot natif MD/MCD — 16 novembre, bus complet et Z80 x64

Le Z80 utilise maintenant un backend x86-64 couvrant les familles de base, CB, ED, DD/FD et DDCB/FDCB. Les opérandes immédiats sont lus à l'exécution, les opcodes sont gardés, les blocs sont chaînés et invalidés individuellement. Le 68000 émet aussi les opérations système et utilise les services communs pour exceptions, statut et interruptions ; les accès aux périphériques ne redispatchent plus l'instruction dans l'interpréteur. Les CPU principal, secondaire et Z80 sont sélectionnés ensemble pour MD/MCD, via le choix natif expérimental existant. Les méthodes Run interprétées restent identiques à Git. Cela ne constitue pas une route GPU.

Corrections prouvées par comparaison avec les handlers existants : lectures PC-relatives via ReadMapped, ordre bas/haut pour MOVE.L pré-décrémenté, PC/prefetch intermédiaires de MOVE mémoire vers mémoire et LINK. Le contrôle des 65 536 opcodes vérifie désormais les cycles, refresh, PC et préfetch visibles à chaque accès bus. Les fenêtres RAM protégées et Word-RAM redirigées utilisent les politiques Sega existantes. Les fenêtres natives sont épinglées seulement lorsqu'elles changent ; les buffers retirés restent vivants jusqu'au retour des chaînes natives actives.

Essai rejeté : remplacer la politique externe de refresh échantillonnée par un appel au service commun à chaque SKIP_BUS_REFRESH n'a pas corrigé le compteur secondaire de Batman Returns (512 contre 1 024 à la trame 61). Cette variante est retirée. Ne pas la refaire comme une nouvelle solution. Le compteur secondaire reste à expliquer ; il ne faut pas annoncer une identité complète de l'état CPU sur Mega-CD. Sur ce contrôle, le principal interprété avec le secondaire natif correspond intégralement, contrairement au cas où les deux sont natifs. Les images/audio contrôlés correspondent ; cela ne suffit pas à nier l'écart d'état.

Les JSON, SHA256 chargés, traces et logs de ce lot sont conservés sous `artifacts/Emulation/segaemu-autres-jeux/attempts/native-protected-memory`. Les relevés communs Debug/Release seront ajoutés après validation du lot final.


## Résultat final du lot CPU natif MD/MCD — 16 novembre

Backend x86-64 : opérations 68000, exceptions/statut via services du cœur existant, et toutes les familles Z80. Aucun changement des boucles interprétées Run. Le tableau GPU indique l'absence de cette route complète. Le 32X n'a pas été avancé pendant ce lot.

La perte Aladdin a été localisée : le code Z80 à 0x02B7 alterne EXX (D9) et JR (18). Le profil initial compte 4 869 changements D9→18 et 4 868 changements 18→D9, sans éviction de cache. Le cache natif conserve désormais les variantes par adresse et premier opcode, dans sa limite globale de 4 096 blocs ; les préfixes ou suffixes réellement changés restent invalidés. Il ne s'agit pas de réessayer l'ancien cache CIL paginé rejeté. Le tableau des timings est aussi réutilisé plutôt que réalloué à chaque tranche. Sur 180 trames mesurées, Aladdin passe de 10 527 à 781 constructions Z80 (301 pendant la chauffe). Sur 900 trames : 1 111 constructions au total, contre 10 857 avant correction. Les invalidations liées aux liens encore dirigés vers une autre variante restent visibles dans le profil ; elles ne signifient plus une recompilation à chaque bascule.

Les cycles Z80 restent en registre hôte pendant les chaînes, sont publiés avant chaque callback et chaque sortie, puis relus après un callback pouvant les changer. Les contrôles comprennent un bus qui ajoute des cycles et déclenche une IRQ. Pour le 68000, N et Z logiques proviennent directement du résultat tronqué ; seuls les bits réellement consommés par le cœur sont requis. Les contrôles PC redondants entre instructions linéaires sont supprimés ; ceux des entrées et après branches/divisions sont conservés. Les contrôles d'opcodes modifiés, de budgets, d'exceptions et de changements du bus restent actifs.

### Debug

Machine émulée complète, 60 trames de chauffe puis 180 mesurées. CPU, vidéo, audio, bus et CD compris ; sans fenêtre, limiteur ni périphérique audio hôte. Les contrôles de puces sont des comparaisons d'états, pas des FPS de cœur isolé.

| Jeu | Plateforme | FPS interpréteur | FPS GPU | FPS dynarec |
| --- | --- | ---: | --- | ---: |
| Aladdin Europe | Mega Drive | 31,82 | Non implémenté | 36,70 |
| Batman Returns Europe | Mega-CD | 29,88 | Non implémenté | 36,66 |

### Release

Même protocole, 60 trames de chauffe puis 900 mesurées.

| Jeu | Plateforme | FPS interpréteur | FPS GPU | FPS dynarec |
| --- | --- | ---: | --- | ---: |
| Aladdin Europe | Mega Drive | 547,88 | Non implémenté | 670,24 |
| Batman Returns Europe | Mega-CD | 328,03 | Non implémenté | 390,75 |

Contrôle dans l'ordre inversé, mêmes DLL : Aladdin natif 663,51 puis interpréteur 498,99 ; Batman natif 390,31 puis interpréteur 341,98. Le témoin interprété utilisant les DLL conservées avant le lot donne ensuite 544,54 et 338,54 FPS. Tous les passages et leurs variations sont conservés ; aucune moyenne entre anciens scénarios ou attribution au PC hôte n'est faite. La première campagne du lot, avant correction EXX/JR et gestion du contexte, donnait 518,63 FPS natifs sur Aladdin et 333,47 sur Batman ; les témoins interprétés de cette première campagne étaient 630,01 et 305,81. Ne pas présenter ces deux campagnes comme une accélération chiffrée parfaitement isolée.

### Vérifications et limites restantes

Dans chaque configuration : 14 240 comparaisons d'opcodes Z80, 3 627 points de séquences/préfixes/IRQ/NMI/retours/budgets et bascules EXX/JR, 65 536 opcodes 68000 avec traces bus comprenant cycles/refresh/PC/préfetch, 374 valeurs limites N/Z, 8 928 cas de stores/protection/franchissements/privilèges/interruptions. Tous passent. Les 131 072 comparaisons 68000 par bus et projection directe ainsi que les 131 072 avec la politique secondaire avaient aussi passé avant les dernières réductions du contexte ; leurs JSON restent distincts. Aucun total gonflé en mélangeant ces étapes n'est annoncé.

Les images SHA256, PCM, nombres d'échantillons, CPU principal et Z80 correspondent sur les deux jeux dans les deux configurations. Le CPU secondaire Mega-CD diffère uniquement par refresh_cycles : +11 264 au point final. Les anciennes DLL natives ne présentent pas cet écart ; c'est bien une différence introduite dans ce lot. Le contrôle principal interprété + secondaire natif correspond, donc il faut reprendre l'interaction des deux routes natives. L'appel direct de SKIP_BUS_REFRESH déjà essayé n'a pas corrigé cet écart et a été retiré. Le résultat d'état Mega-CD ne doit pas être annoncé comme entièrement identique. Les images/audio égaux ne suffisent pas à lever ce point.

Aladdin : zéro repli d'instruction 68000 dans les deux configurations. Batman : un repli principal et un secondaire en Debug ; trois et neuf en Release, chauffe comprise. La protection du code modifié et du préfetch conserve ces replis exceptionnels. Les profils ne montrent plus les millions de replis mémoire/absence de corps du backend partiel précédent. Ne pas annoncer « zéro repli partout » ou un portage GPU terminé.

Preuves : `artifacts/Emulation/segaemu-autres-jeux/attempts/native-protected-memory/summary.json`, `completed-*`, `host-state-checks-*`, `flags-checks-*`, `reverse-Release-*`, `control-before-Release-*`, `profile-opcode-Release-md.json`, `completed-profile-Release-md.json`, et les logs de compilation ciblée Chips/Sega Debug/Release. Chaque JSON machine contient les SHA256 des DLL effectivement chargées. Aucun commit, push ou build complet de l'application.


### DLL Sega finales reconstruites et contrôlées

La reconstruction finale Sega après Chips a produit de nouvelles empreintes : les mesures ci-dessous sont celles des DLL réellement présentes après build. Les empreintes Chips et Sega des JSON module-final-* correspondent aux fichiers de sortie. Images, PCM, nombres d'échantillons et empreintes CPU de chaque route correspondent exactement aux relevés précédents de la même route. Le compteur secondaire divergent reste donc présent, sans nouvel écart détecté. Les tableaux précédents restent les preuves de leur étape et ne sont pas rebaptisés mesures de ces nouvelles DLL.

#### Debug

| Jeu | Plateforme | FPS interpréteur | FPS GPU | FPS dynarec |
| --- | --- | ---: | --- | ---: |
| Aladdin | MD | 32,08 | Non implémenté | 34,35 |
| Batman Returns | MCD | 29,04 | Non implémenté | 36,32 |

#### Release

| Jeu | Plateforme | FPS interpréteur | FPS GPU | FPS dynarec |
| --- | --- | ---: | --- | ---: |
| Aladdin | MD | 496,27 | Non implémenté | 603,89 |
| Batman Returns | MCD | 399,06 | Non implémenté | 404,30 |

Les variations des témoins interprétés entre passes sont conservées ; ces relevés ne démontrent pas un facteur constant d'accélération. Les boucles Run restent inchangées. Le module final et Chips ont été construits en Debug/Release, sans build complet de l'application, commit ou push.

# Correction de la divergence Mega-CD : coût IR après SetIrqDelay

Le compteur secondaire divergent signalé dans les étapes précédentes est corrigé dans les DLL `irq-fixed-*`. Les anciens avertissements sont conservés comme historique.

La première divergence apparaît dès la trame 4 : à PC 0x0A7C, MOVE.W D1,abs.L (0x33C1) déclenche SetIrqDelay dans son callback. Ce service exécute le prochain BRA et laisse IR modifié. Run ajoute alors le coût de IR final, tandis que le bloc natif ajoutait encore le coût du MOVE initial : six unités en trop, soit 42 cycles avec le multiplicateur principal 7. Les états principaux pouvaient se rejoindre à la fin du passage tout en ayant des échéances intermédiaires différentes. À la trame 61, le secondaire prenait sa première décision SKIP différente ; le cumul final atteignait +11 264. La politique SKIP n'était pas la cause.

Le bus natif conserve maintenant le coût du IR final lorsqu'un callback le modifie. Le bloc applique l'ajustement après le dernier accès du corps, sans avancer les cycles visibles aux accès intermédiaires. Les changements de PC/mapping/IR provoquent une sortie de chaîne. Le cas habituel garde son coût constant. Les divisions avec exception sont aussi reconnues comme pouvant appeler le bus. Run et SetIrqDelay interprétés sont inchangés.

Le diagnostic temporaire, retiré avant les mesures, confirme l'identité des 15 514 points d'instruction de la trame 4 et des états principaux de fin de trame 1 à 61. Le compteur secondaire à la trame 61 est 1 024 dans les deux routes. Les 96 cas autonomes passent dans chaque configuration : byte/word/long, plusieurs writes, préfetch actif/inactif, multiplicateurs 1/4/7, quatre ratios, contextes et cycles visibles aux callbacks. Ces cas exécutent réellement le prochain opcode via SetIrqDelay ; le bus passif des anciens tests ne couvrait pas cela.

## Debug — DLL propres après retrait de l'instrumentation

| Jeu | Plateforme | FPS interpréteur | FPS GPU | FPS dynarec |
| --- | --- | ---: | --- | ---: |
| Aladdin Europe | Mega Drive | 32,10 | Non implémenté | 33,68 |
| Batman Returns Europe | Mega-CD | 28,85 | Non implémenté | 32,73 |

## Release — mêmes routes et protocole

| Jeu | Plateforme | FPS interpréteur | FPS GPU | FPS dynarec |
| --- | --- | ---: | --- | ---: |
| Aladdin Europe | Mega Drive | 522,51 | Non implémenté | 646,67 |
| Batman Returns Europe | Mega-CD | 311,98 | Non implémenté | 356,28 |

60 trames de chauffe, puis 180 mesurées en Debug ou 900 en Release, machine émulée complète sans fenêtre ni limiteur. Images, PCM, échantillons et états CPU principal, secondaire et Z80 normalisés correspondent dans tous les cas. Les SHA256 chargés correspondent aux fichiers Chips/Sega construits. Les campagnes précédentes restent distinctes : aucun facteur constant n'est déduit de leurs variations.

Preuves sous `attempts/native-protected-memory` : `interaction-first-difference.json`, `frame4-original.json`, `frame4-fixed-native.json`, `irq-delay-cost-checks-{Debug,Release}.json`, `irq-fixed-*.json` et `summary.json/irqFixedRows`. Instrumentation et banc temporaires supprimés après usage, aucun commit/push. Le service partagé de délai IRQ exécute encore son opcode imbriqué via le handler existant : il reste à porter ce chemin et le GPU complet. L'absence absolue de dispatch interprété dans tous les services natifs n'est pas annoncée.

# Service IRQ natif et coût du correctif IR — 16 novembre

Le service SetIrqDelayNative utilise désormais le même backend x86-64 pour son instruction imbriquée. Le bloc dédié s'exécute une seule fois, sans limite de budget/arrêt, prev_pc, refresh périodique ni ajout du coût de table que le service original n'effectue pas. Il respecte le premier opcode déjà préfetché même si la mémoire a changé. Le cache séparé est limité à 512 entrées et utilise la libération différée des blocs encore actifs. SegaEmu sélectionne ce service pour MD/MCD natifs ; les chemins interprété et 32X restent sur SetIrqDelay. Les méthodes Run et SetIrqDelay originales sont inchangées.

Dans chaque configuration, les 65 536 opcodes imbriqués correspondent au service original sans repli dans ces fenêtres. Les 927 cas ciblés finaux couvrent ratios/multiplicateurs, préfetch actif ou périmé, arrêt, budget dépassé, exceptions, récursion, accès longs à plusieurs callbacks, chronologie des cycles vus par le bus, garde d'extensions modifiées, limite du cache et fermeture des buffers, y compris Dispose pendant le callback. Sur les jeux, le service natif effectue un passage mis en cache sans repli ; ses compteurs sont distincts des replis ordinaires du runner 68000.

## Pourquoi la baisse ne pouvait pas être attribuée directement au correctif

La correction précédente du coût IR avait placé le traitement rare dans le flux généré de chaque instruction pouvant appeler le bus. C'était un surcoût de code évitable. Cela ne prouve pas sa part exacte dans les FPS de campagnes différentes. La première passe du portage IRQ natif donne Aladdin Debug 18,68 FPS, mais la même DLL revient à 31,47 sur le contrôle suivant ; ne pas présenter 18,68 comme un ralentissement déterministe du service ou 35,88 comme un gain intégralement isolé de ce point.

Une première sortie froide mutualisée par coût a été testée puis rejetée : MCD Release 337,80 contre 405,60 avant ; contrôle dans l'ordre inverse 365,52 contre 390,21. Les témoins interprétés du premier couple étaient 350,68 et 350,63. Les résultats Debug étaient plus favorables sur Aladdin (31,47 → 36,17), mais ne justifiaient pas de conserver une baisse MCD répétée. Ne pas refaire cette variante comme une nouvelle piste validée.

La version retenue utilise une seule sortie rare commune. Le callback conserve un delta de cycles ; il est appliqué après tous les accès du corps, sans travail de correction dans le flux courant. Le delta d'avant callback est conservé, puis le changement net de IR remplace les modifications imbriquées pour éviter de les additionner deux fois. Le chemin sans changement garde une branche non prise. Les comparaisons de contextes et chronologie des callbacks passent pour le service legacy comme pour le service natif.

## Debug — machine complète, 180 trames après 60 de chauffe

| Jeu | Plateforme | FPS interpréteur | FPS GPU | FPS dynarec |
| --- | --- | ---: | --- | ---: |
| Aladdin Europe | Mega Drive | 31,63 | Non implémenté | 35,88 |
| Batman Returns Europe | Mega-CD | 28,87 | Non implémenté | 31,74 |

## Release — machine complète, 900 trames après 60 de chauffe

| Jeu | Plateforme | FPS interpréteur | FPS GPU | FPS dynarec |
| --- | --- | ---: | --- | ---: |
| Aladdin Europe | Mega Drive | 566,39 | Non implémenté | 625,03 |
| Batman Returns Europe | Mega-CD | 286,63 | Non implémenté | 380,99 |

Les images, PCM, échantillons et états normalisés des CPU principal, secondaire et Z80 correspondent dans les quatre comparaisons. L'écart Mega-CD de refresh reste corrigé. Les témoins, ordres inversés et variantes sont conservés séparément ; aucune cause liée au PC hôte ni facteur d'accélération constant n'est déduit. Il reste une différence de vitesse entre passes qui n'est pas isolée ; ne pas prétendre que le déplacement du traitement IR explique toute la baisse antérieure.

Preuves sous `artifacts/Emulation/segaemu-autres-jeux/attempts/native-delayed-irq` : `before-sha.json`, `opcodes-{Debug,Release}.json`, `shared-exit-checks-{Debug,Release}.json`, `current-*`, `before-*`, `cold-exit-*`, `cold-reverse-*`, `shared-exit-*` et `summary.json`. Les SHA256 des DLL chargées correspondent aux sorties Chips/Sega construites dans les deux configurations. Le banc fixe explicitement AssemblySearchPaths sur les HintPath : un rebuild avait sinon repris une ancienne DLL témoin dans son sous-dossier et produit des erreurs de références ; ce réglage du banc est corrigé. Les JSON de mesure gardent leurs empreintes et ne sont pas réattribués à une autre DLL.

Les replis ordinaires 68000 pour code modifié/préfetch subsistent sur MCD, distincts de ce service désormais natif. La route GPU complète reste à développer ; 32X reste différé. Banc temporaire supprimé après utilisation, aucun commit/push.

# Gardes de code et opcode préfetché — 16 novembre

Le runner natif compilait le premier opcode depuis la mémoire vive, même lorsque le CPU possédait déjà un autre opcode préchargé. Une garde périmée supprimait le bloc puis exécutait une instruction via le handler. La sélection froide utilise désormais le premier opcode réellement préchargé ; une garde de code modifié relance la sélection native. Un préfetch hors 16 bits conserve le chemin erreur existant sans boucle de recompilation. Aucun changement dans les Run interprétés.

Les 1 152 contrôles passent en Debug et Release : préfetch actif/inactif, opcode/extensions périmés, code modifié par callback, remappage, bus versionné ou non, ratios et multiplicateurs. Les contextes utiles, mémoire, chronologie des writes et erreurs correspondent. La limite du banc a été corrigée de 256 à 1 024 consultations : la construction initiale de la carte demande déjà 256 pages ; ce seuil de diagnostic était trop bas, sans défaut démontré dans le cœur.

Machine complète sans fenêtre ni limiteur : 60 trames de chauffe puis 180 Debug / 900 Release. CPU principal/secondaire/Z80 normalisés, image finale, PCM et échantillons correspondent entre les quatre routes avant/après. Les SHA256 chargés correspondent aux sorties Chips/Sega construites.

## Debug

| Jeu | Plateforme | FPS interpréteur | FPS GPU | FPS dynarec |
| --- | --- | ---: | --- | ---: |
| Aladdin Europe | Mega Drive | 29,40 | Non implémenté | 36,54 |
| Batman Returns Europe | Mega-CD | 28,48 | Non implémenté | 32,26 |

## Release

| Jeu | Plateforme | FPS interpréteur | FPS GPU | FPS dynarec |
| --- | --- | ---: | --- | ---: |
| Aladdin Europe | Mega Drive | 530,30 | Non implémenté | 613,93 |
| Batman Returns Europe | Mega-CD | 358,88 | Non implémenté | 310,92 |

Témoins dynarec avant modification : MD 35,40 / 631,27 et MCD 33,45 / 363,79 FPS (Debug / Release). La baisse initiale MCD Release ne se répète pas dans le contrôle inversé : avant 357,09, après 366,23 FPS, mêmes sorties et CPU. Le correctif est conservé pour ses résultats fonctionnels et la suppression des replis ; aucun gain de vitesse général ni cause précise de la différence entre passes ne sont déclarés. Ne pas présenter seulement le contrôle favorable comme résultat de toute la campagne.

MCD : replis ordinaires main/sub 1/1 → 0/0 Debug et 3/9 → 0/0 Release. Le service IRQ natif reste à un passage sans repli. Les blocs compilés Release passent seulement de 808/1 811 à 808/1 813 ; cela ne démontre pas un emballement du cache. La suppression de douze replis par 960 trames ne doit pas être vendue comme accélération majeure.

Preuves : `artifacts/Emulation/segaemu-autres-jeux/attempts/native-fetch-guards/{checks-*,games-*,reverse-*,*-sha.json,summary.json,build-*}`. Banc temporaire supprimé après libération des cœurs et machines ; aucun commit/push. GPU complet et suite du lot restent à réaliser.

# Positions de champs CPU typées et coût de compilation — 16 novembre

Le profil du CPU secondaire Mega-CD Release, après 60 trames de chauffe puis 180 profilées, attribuait 139,66 ms sur 266,24 ms du runner à la compilation des blocs. Les accès aux positions CLR des champs CPU étaient recalculés pour chaque opérande émis. Le profil porte sur les CPU ; il ne désigne pas la compilation comme poste dominant de toute la machine.

Le contexte natif utilise maintenant des identifiants de champs typés. Les 29 offsets CLR sont calculés une fois, sans supposer le packing de la structure, puis consultés par indice. Tous les émetteurs consommateurs ont été adaptés. Les mêmes offsets sont transmis à l’assembleur ; ni la représentation de Cpu68000State ni les fichiers des interpréteurs 68000/Z80 ne sont modifiés.

## Variante rejetée et contrôle du changement

FrozenDictionary était moins bon dans le contrôle Release : 199,18 ms via l’API d’origine contre 323,82 ms par dictionnaire. Variante retirée avant les essais sur jeux, résultats gardés dans rejected-frozen-*.json. Le dictionnaire de noms ne doit pas être réintroduit comme gain établi.

Le contrôle final vérifie les 29 offsets et la couverture de tous les champs de Cpu68000State, puis 2,9 millions de consultations dans le même processus :

| Configuration | API d’origine | Accès typé | Allocations accès typé |
| --- | ---: | ---: | ---: |
| Debug | 97,81 ms | 16,64 ms | 0 octet |
| Release | 76,45 ms | 6,08 ms | 0 octet |

Ces durées concernent seulement les consultations du compilateur, pas le facteur d’accélération de l’émulation complète. Les contrôles avant/après ont les mêmes valeurs ; les chaînes de noms ont été supprimées du chemin d’émission, y compris les sélecteurs de drapeaux et SR/CCR. Une première compilation de cette adaptation a relevé deux paramètres locaux encore de type string et le nom t1_flag non remplacé ; ces omissions sont corrigées, les deux builds finaux réussissent.

## Debug — machine complète, 60 trames de chauffe puis 180 mesurées

| Jeu | Plateforme | FPS interpréteur | FPS GPU | FPS dynarec |
| --- | --- | ---: | --- | ---: |
| Aladdin Europe | Mega Drive | 33,24 | Non implémenté | 36,79 |
| Batman Returns Europe | Mega-CD | 30,48 | Non implémenté | 34,59 |

Témoins avant : Aladdin I32,28/N37,22 ; Batman Returns I29,39/N34,81. La légère baisse du dynarec est conservée dans les relevés ; aucun gain Debug général n’est annoncé.

## Release — machine complète, 60 trames de chauffe puis 900 mesurées

| Jeu | Plateforme | FPS interpréteur | FPS GPU | FPS dynarec |
| --- | --- | ---: | --- | ---: |
| Aladdin Europe | Mega Drive | 644,68 | Non implémenté | 672,51 |
| Batman Returns Europe | Mega-CD | 298,29 | Non implémenté | 380,79 |

Témoins avant : Aladdin I543,06/N650,94 ; Batman Returns I356,80/N375,96. Les différences importantes entre témoins interprétés interdisent d’attribuer l’intégralité des écarts de FPS à cette modification du compilateur natif. Les fichiers source des deux interpréteurs ont zéro différence Git ; cette vérification ne permet pas de déclarer leurs FPS invariants entre passages.

Les quatre comparaisons machine ont les mêmes états CPU principal/secondaire/Z80 normalisés, image finale, PCM et nombre d’échantillons. Aucun repli 68000 dans ces passages. Les SHA256 des DLL réellement chargées correspondent aux sorties finales Chips/Sega, sans recompilation après mesure. Machine complète sans fenêtre, limiteur ou sortie audio hôte.

La passe diagnostique séparée de 180 trames donne les temps de compilation Release suivants : MD principal 25,51 → 20,13 ms ; MCD principal 38,71 → 27,30 ms, secondaire 139,66 → 103,77 ms. En Debug, MD principal 65,90 → 50,17 ms ; MCD principal 29,67 → 31,11 ms et secondaire 110,52 → 121,10 ms. Conserver aussi ces hausses Debug : le contrôle des offsets ne prouve pas à lui seul une baisse de tous les temps de compilation. Les profils sont instrumentés et distincts des tableaux FPS.

À conserver : accès typés sans recherche par nom, valeurs vérifiées et coût local réduit. Ne pas annoncer ×12 sur l’émulation ni présenter seulement les profils favorables. Le GPU complet reste absent et le lot global demeure ouvert ; aucun essai 32X supplémentaire dans cette passe.

Preuves : `artifacts/Emulation/segaemu-autres-jeux/attempts/native-runtime-cost/summary.json`, `checks-*`, `rejected-frozen-*`, `profile-{before,after}-*`, `games-{before,after}-*`, `*-sha.json` et logs de build. Banc temporaire supprimé après fermeture des machines dans finally ; aucun commit/push.

# Couverture native élargie MD/MCD — 16 novembre

Quatre titres déjà utilisés sont comparés avec les DLL finales du groupe des offsets typés : Sonic 2 et Streets of Rage 2 sur 1 200 trames chacun ; Final Fight CD et Dragon Lair sur 2 400 trames chacun. BIOS Europe v1.00 pour Final Fight, Mega-CD 2 Japan v2.00C pour Dragon Lair. Les hashes des médias cartouches/CUE, BIOS et DLL chargées sont dans summary.json. Les fichiers des ROM et pistes CD ne sont pas modifiés.

Les deux machines sont créées séparément dans le même processus. À chaque trame : mêmes boutons, RunFrame interprété puis natif, comparaison des contextes CPU normalisés (principal, secondaire et Z80), de tous les pixels du framebuffer et des échantillons PCM retournés. Comparaisons et hashes sont hors chronométrage ; seuls les appels RunFrame constituent les FPS. Chargement exclu, démarrage et compilation inclus ; pas de fenêtre, limiteur ou sortie audio hôte. Ce protocole par paires n’est pas substitué aux anciennes campagnes dans des processus séparés.

Entrées : Start pendant cinq trames à 360, 720 et 960 ; droite et impulsions C après 1000. De 1201 à 1800 : Start à 1380 et 1560, droite/C après 1620. Après 1800 : directions alternées et B ; Dragon Lair reçoit une impulsion Start de cinq trames pour la reprise. Le contrôle est automatique, pas une inspection visuelle ou une écoute de ces nouveaux passages ; l’égalité avec l’interpréteur ne prouve pas la compatibilité complète des titres.

## Debug

| Jeu | Plateforme | Trames | FPS interpréteur | FPS GPU | FPS dynarec |
| --- | --- | ---: | ---: | --- | ---: |
| Sonic The Hedgehog 2 World | Mega Drive | 1200 | 31,40 | Non implémenté | 35,19 |
| Streets of Rage 2 USA | Mega Drive | 1200 | 32,11 | Non implémenté | 35,21 |
| Final Fight CD Europe | Mega-CD | 2400 | 23,77 | Non implémenté | 29,24 |
| Dragon Lair Japan | Mega-CD | 2400 | 28,43 | Non implémenté | 33,62 |

## Release

| Jeu | Plateforme | Trames | FPS interpréteur | FPS GPU | FPS dynarec |
| --- | --- | ---: | ---: | --- | ---: |
| Sonic The Hedgehog 2 World | Mega Drive | 1200 | 399,13 | Non implémenté | 435,41 |
| Streets of Rage 2 USA | Mega Drive | 1200 | 509,94 | Non implémenté | 552,10 |
| Final Fight CD Europe | Mega-CD | 2400 | 394,88 | Non implémenté | 400,68 |
| Dragon Lair Japan | Mega-CD | 2400 | 442,92 | Non implémenté | 499,78 |

Les 14 400 trames comparées entre les routes (28 800 trames exécutées au total) correspondent pour les CPU, pixels et PCM. Aucun repli ordinaire ou IRQ des 68000 n’est relevé. Les images et états CPU des points de contrôle, le hash PCM complet et le nombre d’échantillons correspondent aussi entre Debug et Release. Les SHA256 des DLL chargées correspondent aux sorties Chips/Sega finales.

Aucun nouveau défaut de production découvert dans ces scénarios : aucune correction du cœur n’a été inventée ni ajoutée pour cette campagne. Le faible avantage Release Final Fight (environ 1,5 %) ne constitue pas une marge garantie ; ces résultats ne prouvent pas un facteur de vitesse global ni tous les jeux/périphériques.

Incident du diagnostic : l’absence initiale de texte dans le journal Sonic avait été interprétée à tort comme un ralentissement anormal. Le processus a terminé normalement avant la capture de pile ; aucune trace de blocage n’a été produite. L’invocation initiale dotnet-trace cpu-sampling ne convient pas à collect sous Windows dans cette version : utiliser dotnet-sampled-thread-time. Le diagnostic n’a pas relancé Sonic. Les résultats complets et la fin normale priment sur cette hypothèse initiale.

À conserver : les routes natives vérifiées précédemment, l’interpréteur original et ces références élargies. Le GPU complet et la suite 32X restent ouverts. Preuves sous `artifacts/Emulation/segaemu-autres-jeux/attempts/native-game-coverage` : JSON par configuration/titre, logs de progression et build, summary.json. Banc temporaire supprimé après fermeture des deux cartes en finally ; aucun commit/push.
# Validation neuve des routes ROM et registres — 16 novembre

Mesures actuelles, processus séparés, 60 trames de chauffe puis 180 Debug ou 900 Release. Seul RunFrame est chronométré. Les comparaisons CPU/image/PCM restent hors minuterie. GPU complet non implémenté ; 32X différé.

| Debug — jeu | Plateforme | Interpréteur | Natif existant | Registres | ROM stable | Combiné |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Aladdin | Mega Drive | 30,45 | 34,20 | 34,45 | 35,50 | 35,54 |
| Batman Returns | Mega-CD | 29,66 | 33,03 | 34,07 | 32,92 | 36,96 |

| Release — jeu | Plateforme | Interpréteur | Natif existant | Registres | ROM stable | Combiné |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Aladdin | Mega Drive | 446,13 | 353,22 | 538,53 | 614,72 | 571,37 |
| Batman Returns | Mega-CD | 299,08 | 338,21 | 431,05 | 300,83 | 333,86 |

Contre-contrôles neufs dans l'ordre inversé, DLL avant → actuelles : interpréteur Debug Aladdin 33,67 → 33,50 ; Batman 30,62 → 29,53. Release Aladdin 540,11 → 545,32 ; Batman 361,96 → 359,22. Natif existant Aladdin Release 633,80 → 619,39. La baisse initiale à 353,22 ne se reproduit donc pas avec les mêmes DLL. Conserver les deux campagnes ; aucune cause de dispersion n'est isolée. La petite baisse Debug Mega-CD subsiste ; aucune invariance exacte de vitesse n'est démontrée.

659 contrôles ciblés réussis dans chaque configuration : préfetch, changements de contenu/mapping/certification, alias inscriptibles, firmware et invalidation des liens directs. États CPU principal/secondaire/Z80, images et PCM correspondent entre les quatre combinaisons et l'interpréteur aux points contrôlés. Les SHA des DLL chargées correspondent aux sorties finales. Les fichiers des interpréteurs originaux restent inchangés.

Cœur 68000 seul : même boucle, 100 000 002 unités horloge, états identiques. Temps Debug I/natif/registres/ROM/combiné : 1,4407043 / 0,0219632 / 0,0227466 / 0,0194856 / 0,0187953 seconde. Release : 0,5116756 / 0,0267444 / 0,0228531 / 0,0214514 / 0,0187953. Ne pas convertir ce gain local en facteur de FPS machine. Registres seuls ralentissent légèrement cette boucle Debug.

Profils instrumentés séparés, 180 trames, combiné : corps/finalisation MD Debug 3,963/0,884 s ; MCD Debug 3,475/1,784 ; MD Release 0,381/0,030 ; MCD Release 0,976/0,193. La finalisation inclut notamment la fin audio ; le corps contient CPU, bus, vidéo, CD et génération audio. Les profils CPU sont inclusifs, peuvent se chevaucher et ne donnent pas un coût vidéo exclusif.

Décision : aucune variante ne gagne partout. Options désactivées par défaut ; aucun choix automatique par jeu. Ne pas répéter une retouche du compilateur seul sans identifier le coût machine restant. Les révisions de contenu sont séparées du mapping ; les blocs ROM restent réutilisables seulement si fenêtre, contenu et politique sont garantis. RAM et matériels non certifiés restent gardés. GPU complet et accélération générale restent à terminer.

Preuves : `artifacts/Emulation/segaemu-autres-jeux/attempts/native-rom-routes/summary.json`, JSON initiaux/reverse, profils, SHA et logs. Aucun commit/push.


# Toutes les machines SegaEmu — 17 novembre 2026

Nouvelle campagne sur les DLL du raccordement Z80/SH-2, sans ancien FPS reutilise. Dix configurations, un jeu chacune ; cartouches SG compatibles pour SC-3000, cartouche SMS pour Mark III. Deux machines fraiches creees par passage, interpreteur puis route native a chaque trame. 60 trames de chauffe, puis 180 mesurees Debug et 900 Release. Seul RunFrame est chronometre ; chargement, comparaison et hashes sont exclus. Start pendant cinq trames tous les 120 a partir de 120. Pas de fenetre, limiteur ni sortie audio hote. Les passages Debug et Release ont donc des longueurs differentes ; ils ne prouvent pas le cout du meme segment entre configurations.

Chaque variante a son propre temoin interpreteur neuf, conserve dans les JSON. La colonne Interpreteur ci-dessous reprend le temoin du passage natif existant ; les autres colonnes sont des passages distincts et ne doivent pas etre compares uniquement a ce temoin pour annoncer un pourcentage. Night Trap Release utilise un passage interpreteur seul complet distinct, les quatre comparaisons ayant echoue. Les options Registres/ROM stable/Combine concernent les 68000 ; sans objet sur les machines a Z80 seul. GPU complet non implemente sur toutes les lignes.

## Debug — FPS

| Jeu | Plateforme | Interpréteur | Dynarec existant | Registres | ROM stable | Combiné |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 166,89 | 84,86 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 192,59 | 91,47 | Sans objet | Sans objet | Sans objet |
| Sonic the Hedgehog | Mark III | 151,67 | 54,93 | Sans objet | Sans objet | Sans objet |
| Sonic the Hedgehog | Master System | 167,28 | 58,44 | Sans objet | Sans objet | Sans objet |
| Sonic the Hedgehog | Game Gear | 242,98 | 59,32 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 36,16 | 39,41 | 42,08 | 44,25 | 40,99 |
| Aladdin | Mega Drive | 29,47 | 33,20 | 32,58 | 35,73 | 36,12 |
| Batman Returns | Mega-CD | 31,21 | 37,26 | 34,84 | 38,20 | 35,45 |
| Doom | 32X | 20,02 | 15,82 | 16,81 | 16,93 | 16,35 |
| Night Trap, disque 1 | Mega-CD + 32X | 30,10 | 32,83 | 33,04 | 31,27 | 31,53 |

## Release — FPS

| Jeu | Plateforme | Interpréteur | Dynarec existant | Registres | ROM stable | Combiné |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 3175,02 | 851,74 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 2947,00 | 856,22 | Sans objet | Sans objet | Sans objet |
| Sonic the Hedgehog | Mark III | 2490,22 | 518,35 | Sans objet | Sans objet | Sans objet |
| Sonic the Hedgehog | Master System | 2475,49 | 544,61 | Sans objet | Sans objet | Sans objet |
| Sonic the Hedgehog | Game Gear | 2396,01 | 425,07 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 553,28 | 608,58 | 681,62 | 619,53 | 621,09 |
| Aladdin | Mega Drive | 526,74 | 529,43 | 485,25 | 575,91 | 576,49 |
| Batman Returns | Mega-CD | 342,27 | 364,85 | 347,99 | 413,68 | 369,14 |
| Doom | 32X | 123,80 | 5,96 | 5,99 | 5,89 | 5,72 |
| Night Trap, disque 1 | Mega-CD + 32X | 130,39 | Échec : trame 720 | Échec : trame 720 | Échec : trame 720 | Échec : trame 720 |

## Resultats, echecs et limites

46 passages compares reussissent ; les quatre variantes Night Trap Release echouent au controle CPU/RAM de la trame 720. Images et PCM correspondaient jusque-la ; cela ne permet pas de valider le passage complet. Les nombres FPS partiels des JSON en echec ne sont pas des mesures de 900 trames et ne sont pas reutilises dans les tableaux. Le passage interpreteur seul Night Trap Release termine les 960 trames.

Toutes les trames comparees sont controlees pour pixels et PCM, et les etats CPU/RAM sont verifies tous les 60 frames : Z80, registres/indicateurs utiles des 68000 et contextes SH-2, RAM principale/Z80 et SDRAM 32X. Ce ne sont pas des validations completes de jeux, sauvegardes, entree utilisateur ou tous les peripheriques. Le court passage Night Trap Debug precede le demarrage des SH-2 : ne pas le presenter comme une validation des deux SH-2 CD32X. Ils fonctionnent ensuite dans le passage Release en echec.

Le raccordement utilise les backends existants : 68000 et Z80 x64, SH-2 a blocs CIL avec petit prefixe x64. Il ne constitue pas un SH-2 entierement traduit par le backend x64. Sur Doom Release, le master construit 32 421 blocs, execute zero prefixe x64 ; le slave 158 blocs et un prefixe x64. Le 68000 principal releve 11 184 526 instructions de repli dans le passage natif existant. Night Trap natif Release avant echec : master 1 321 blocs et 28 949 prefixes x64, slave 22 blocs et zero prefixe ; main 6 710 550 replis. Les options 68000 ne corrigent pas le cout SH-2.

A retenir : raccordement selectionnable et interpreteur conserve par defaut, controles concordants des passages reussis, aucune promotion automatique des nouvelles variantes. A corriger : forte perte des routes Z80 sur SG/SC/MarkIII/SMS/GG ; tres forte perte 32X Release ; divergence CD32X au demarrage des SH-2. Ne pas presenter cette campagne comme une acceleration generale.

Diagnostic du processus Doom Release existant, sans relance : environ 98,17 % du temps echantillonne exclusif dans CompileBlock. Trace bornee de cinq secondes et rapport topN conserves. Cela prouve le cout observe de compilation dans cet echantillon, pas une cause de tous les ralentissements ni un gain theorique apres correction.

Isolement Night Trap : SH-2 compiles sans petit x64 divergent encore a 720 ; SH-2 interpretes avec les autres CPU natifs correspondent a 720. Le probleme est donc dans la voie SH-2 compilee sur ce passage. L essai de decodage uniquement dans les fenetres mappees, sans Fetch16 anticipe, ne corrige pas seul le probleme : divergence plus fine relevee a 677, CPU master/cache/SDRAM. Cet essai est retire, sa source et ses JSON sont conserves ; ne pas le refaire seul comme une correction complete. Les sources/DLL des tableaux sont revenues exactement aux empreintes mesurees, verifiees dans les deux configurations.

Travail suivant concret : mesurer les couts de callbacks et remappages Z80 (le bus huit bits ne fournit actuellement aucune version rapide ni fenetre de donnees native) ; distinguer constructions uniques, invalidations et evictions SH-2 avant de modifier le cache de blocs ; corriger le decodage et les limites temporelles des blocs SH-2 ensemble, sans effets du compilateur sur le cache materiel ; exposer les fenetres de code 68000 transparentes hors registres/overlay 32X pour eviter le repli de banques entieres. Ces pistes restent a realiser et a valider, pas des gains annonces. Aucun changement de vitesse retenu sur une simple hypothese.

Builds cibles Chips/Sega Debug et Release reussis. Empreintes chargees controlees pour les 50 passages contre les DLL finales. Preuves : attempts/native-all-machines/summary.json, JSON par configuration/machine/route, diagnostic-*, cache-fix-*, first-difference-*, cache-decode-trial.cs.txt, trace et logs. Les premieres donnees sont aussi conservees sous before-cache-decode. Aucun commit/push.

# Projection memoire native Z80 — 17 novembre 2026

Correction du raccordement huit bits : le Z80 natif ne disposait ni de version rapide des mappings ni de fenetres de donnees. Chaque callback reappliquait les 64 projections fetch/read/write ; les lectures et ecritures simples traversaient aussi les handlers. BytePageMap conserve son constructeur public existant et ajoute un callback au remplacement effectif de fenetre. Les deux vues cartouche et la vue de la carte notifient la meme revision. Le token natif inclut aussi le materiel et la politique de handlers ; une bascule vers une lecture speciale retire donc les fenetres directes meme si les pages ne changent pas.

Lectures simples directement mappees ; bus ouvert conserve hors RAM. Stores directs uniquement pour le handler sans mapper et les pages Sega hors page 63, qui contient FFFC..FFFF. Autres mappers et lectures specialisees conservent les handlers. La RAM mutable conserve les gardes de code existantes ; son contenu ne provoque pas une reconstruction de toutes les projections. Les methodes Run interpretees ne sont pas modifiees. Aucun changement de selection par defaut.

Deux machines fraiches par titre/configuration, interpreteur puis natif a chaque trame, memes medias et entrees que la campagne native-all-machines : 60 trames de chauffe puis 180 mesurees Debug ou 900 Release. Seuls les appels RunFrame sont chronometres ; pixels et PCM compares a chaque trame, CPU normalises/RAM/LastFetch/EA tous les 60. Les chiffres suivants sont tous remesures, y compris les temoins interpreteurs. Cette passe compare la route native existante ; les options 68000 registres/ROM/combine ne sont pas modifiees ni remesurees.

## Debug — FPS

| Jeu | Plateforme | Interpreteur neuf | Natif | Ecart natif / interpreteur |
| --- | --- | ---: | ---: | ---: |
| Choplifter | SG-1000 | 189,38 | 256,89 | +35,6 % |
| Choplifter | SC-3000 | 179,80 | 240,67 | +33,9 % |
| Sonic | Mark III | 181,57 | 222,28 | +22,4 % |
| Sonic | Master System | 195,25 | 244,09 | +25,0 % |
| Sonic | Game Gear | 228,80 | 294,77 | +28,8 % |
| Aladdin | Mega Drive | 30,04 | 33,80 | +12,5 % |
| Batman Returns | Mega-CD | 31,06 | 37,66 | +21,2 % |

## Release — FPS

| Jeu | Plateforme | Interpreteur neuf | Natif | Ecart natif / interpreteur |
| --- | --- | ---: | ---: | ---: |
| Choplifter | SG-1000 | 1773,64 | 1803,81 | +1,7 % |
| Choplifter | SC-3000 | 2588,41 | 2607,95 | +0,8 % |
| Sonic | Mark III | 2253,34 | 1812,59 | -19,6 % |
| Sonic | Master System | 1972,63 | 2178,78 | +10,5 % |
| Sonic | Game Gear | 1931,03 | 1197,34 | -38,0 % |
| Aladdin | Mega Drive | 572,09 | 629,48 | +10,0 % |
| Batman Returns | Mega-CD | 324,25 | 348,96 | +7,6 % |

## Resultats et points encore ouverts

Les 14 passages principaux correspondent en CPU/RAM, images et PCM aux points verifies. Les 32 controles synthetiques passent dans chaque configuration : banques Sega, registre de controle conserve, bank switch identique sans changement de version inutile, RAM miroir, programme changeant de banque, code RAM puis immediat modifie, handler special et bus ouvert. Le premier banc avait un budget absolu inferieur au compteur de reset : aucun opcode ne tournait dans le programme synthetique. Le banc remet les deux compteurs a zero avant ce test ; aucune panne du coeur ne doit etre deduite de checks-initial-clock-Debug.json.

A conserver : les projections natives et leur invalidation, les handlers speciaux, les sorties controlees. Fort gain face a la route native de la campagne precedente, dont les nombres restent historiques : SG Debug 84,86 -> 256,89 ; SMS 58,44 -> 244,09 ; GG 59,32 -> 294,77. Ces anciens nombres ne remplacent aucun interpreteur actuel. Les cinq titres huit bits sont plus rapides que leur interpreteur neuf en Debug. En Release, les mesures principales gagnent sur SG/SC/SMS, et restent en retrait sur Mark III/GG. La correction du bus ne prouve donc pas que tout le dynarec est maintenant plus rapide dans tous les cas.

Contre-controles Release, fichiers distincts : GG interpreteur/natif 2951,54/1752,90 ; Mark III 1840,50/1856,63. Ne pas effacer les premiers relevés. Le retrait GG persiste ; Mark III reste indecis entre passages. Aucune cause de cette dispersion n est attribuee a la machine hote ou supposee prouvee. Les temoins MD/MCD correspondent et restent plus rapides en natif sur ces passages ; cela ne valide pas toutes les machines ou autres variantes.

Profils instrumentes separes des FPS principaux, 900 trames mesurees apres chauffe. GG : temps machine natif 0,6142046 s ; CPU natif inclusif 0,4194508 ; execution 0,2030015 ; bus 0,0861895 inclus dans execution ; compilation 0,1650903. Mark III : machine 0,5161463 ; CPU 0,1891470 ; execution 0,1230996 ; bus 0,0490503 inclus ; compilation 0,0194893. Ne pas sommer les temps inclusifs. GG construit 3170 blocs au total, Mark III 1095 ; zero eviction dans les deux, zero invalidation de suffixe, 16 invalidations de premier opcode GG et zero Mark III. Agrandir le cache sans autre preuve ne traite donc pas un churn demontre ; ne pas proposer cette piste comme correction etablie. Emission/allocation executable et execution/interop restent a distinguer pour GG.

Limites : un titre par machine, controle automatique des sorties, pas de validation complete des jeux ni de reprise d etat. Les trois modes 68000 ne sont pas compares dans cette passe. Le SH-2, le 32X et le CD32X ne sont pas modifies ici ; leurs pertes et la divergence Night Trap deja consignees restent ouvertes. Le GPU complet reste absent. Aucun nouveau gain SH-2/32X ni resultat ancien n est presente comme une nouvelle mesure.

Preuves : artifacts/Emulation/segaemu-autres-jeux/attempts/native-z80-maps, JSON principaux, checks, reverse, profils, summary.json, source-sha.json et logs. SHA charges conformes aux sorties finales Chips/Sega Debug et Release. Builds cibles reussis, aucun commit/push.

# SH-2 : revalidation des attentes de fetch et campagne complete — 17 novembre 2026

Correction fonctionnelle du compilateur SH-2 : le controle comparait les mots de code lors de la reutilisation, mais ne comparait pas les attentes de fetch incorporees aux prefixes fusionnes. Un bloc pouvait donc conserver les anciens couts apres changement de mode de cache ou mapping, alors que ses octets etaient identiques. La trace Night Trap avait 23 314 acces identiques dans la trame 677 avant une lecture a 0600043C effectuee deux cycles trop tard en compile ; le code precedent contient 6112 / 3210 / 8B02. L ecart temporel se propageait ensuite aux CPU et RAM, detecte au controle grossier a 720.

Sh2.Blocks.cs conserve maintenant les attentes de chaque fetch dans le bloc et les verifie avec les mots lors de la reutilisation ; une variante de couts incompatible n est pas reutilisee. Le premier fetch garde son cout dynamique, les suivants alimentent les couts prevalides. Le decodage et la validation lisent aussi les fenetres deja mappees au lieu de remplir/remplacer les lignes du cache materiel en compilant du code futur. Ce dernier changement seul avait echoue auparavant : il est maintenant retenu dans la correction combinee, pas presente comme une solution suffisante isolee.

Le test autonome CompiledBlocksRevalidateFetchTimingsWhenCodeBytesStayIdentical fait varier les attentes avec des budgets courts et compare registres/contexte/horloges a l interpreteur. Les 34 cas du fichier Sh2Tests passent sur les DLL normales Debug et Release. Le projet Chips.Tests global a echoue CS0234 sur CPU/NEC/UPD780C/UPD780C1/Upd780C1Tests.cs et sa reference CPU.Common ; aucune source NEC n est modifiee. Un projet temporaire ne compile que le fichier SH-2 concerne. Ce resultat ne signifie pas que tout Chips.Tests a ete execute.

Diagnostic conditionnel temporaire : CPU/cache/RAM compares a chaque fin de trame et acces memoire des deux SH-2 compares de 600 a 960 ; 361 trames, aucun ecart avec la correction combinee, maximum 506 372 evenements dans une trame, sous la limite de collecte. Un premier arret a 637 avec etats identiques venait de la limite du journal de sondes ; ce n etait pas une nouvelle panne de l emulation. Les sondes Sh7095 et le drapeau de projet sont retires ; leurs sources correspondent exactement aux empreintes conservees avant instrumentation. Aucun DiagnosticAccess dans les DLL finales.

## Protocole de la nouvelle campagne

Toutes les machines SegaEmu sont remesurees, un titre chacune : aucun ancien FPS reutilise. Deux cartes fraiches par passage, interpreteur puis candidat a chaque trame, memes medias et entrees. 60 trames de chauffe puis 180 mesurees Debug, sauf CD32X prolonge a 900 pour lancer les deux SH-2 ; 900 mesurees Release partout. Les quatre courts passages Debug CD32X de 180 trames sont conserves mais remplaces dans les tableaux par les passages longs distincts, sans effacement de leurs donnees. Seuls les appels RunFrame sont chronometres ; chargement, checks et hashes exclus. Start cinq trames tous les 120 a partir de 120, sans fenetre/limiteur/sortie audio hote.

Pixels et PCM compares chaque trame ; CPU normalises, RAM et caches/peripheriques SH-2 controles tous les 60. Les 50 passages retenus, ainsi que les quatre passages courts supplementaires, correspondent aux points verifies. Chaque variante a son propre interpreteur neuf dans le JSON ; la colonne Interpreteur affiche le temoin du passage natif existant. Les autres colonnes correspondent a des processus distincts ; ne pas calculer leurs pourcentages exclusivement a partir de cette premiere colonne. Sans objet signifie option 68000 sur machine a Z80 seul. GPU complet non implemente pour toutes les lignes.

## Debug — FPS

| Jeu | Plateforme | Interpréteur | Dynarec existant | Registres | ROM stable | Combiné |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 209,18 | 295,87 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 189,95 | 263,58 | Sans objet | Sans objet | Sans objet |
| Sonic | Mark III | 193,35 | 241,61 | Sans objet | Sans objet | Sans objet |
| Sonic | Master System | 171,66 | 206,31 | Sans objet | Sans objet | Sans objet |
| Sonic | Game Gear | 232,23 | 304,90 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 40,32 | 44,51 | 40,69 | 45,97 | 43,46 |
| Aladdin | Mega Drive | 33,03 | 37,21 | 35,51 | 33,15 | 32,01 |
| Batman Returns | Mega-CD | 30,37 | 36,59 | 34,37 | 36,60 | 38,11 |
| Doom | 32X | 20,65 | 16,54 | 16,67 | 17,09 | 16,42 |
| Night Trap, disque 1 | Mega-CD + 32X | 24,80 | 21,35 | 21,08 | 20,26 | 19,87 |

## Release — FPS

| Jeu | Plateforme | Interpréteur | Dynarec existant | Registres | ROM stable | Combiné |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 2609,40 | 2801,80 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 2654,15 | 3146,01 | Sans objet | Sans objet | Sans objet |
| Sonic | Mark III | 2340,48 | 1842,44 | Sans objet | Sans objet | Sans objet |
| Sonic | Master System | 2307,33 | 1891,24 | Sans objet | Sans objet | Sans objet |
| Sonic | Game Gear | 3061,04 | 1754,55 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 623,42 | 705,15 | 550,04 | 658,17 | 559,58 |
| Aladdin | Mega Drive | 537,51 | 612,67 | 645,04 | 658,42 | 630,52 |
| Batman Returns | Mega-CD | 425,31 | 450,48 | 341,19 | 393,65 | 406,34 |
| Doom | 32X | 125,39 | 5,95 | 6,15 | 6,20 | 5,97 |
| Night Trap, disque 1 | Mega-CD + 32X | 166,06 | 69,78 | 67,72 | 69,24 | 70,60 |

## Decision et travail restant

La divergence Night Trap observee a 677/720 est corrigee sur les passages compares : les quatre variantes atteignent 960 trames en Debug normal et Release normal, avec CPU/cache/RAM/image/PCM correspondants. Les deux SH-2 sont effectivement lances dans ces passages longs ; ne plus presenter les 180 trames du debut comme une validation de leur execution. La trace diagnostique plus fine de 600 a 960 correspond aussi pour tous les acces collectes. Cela ne valide pas tous les jeux, toutes les opcodes ou toute la suite d une partie.

La correction ne resout pas la vitesse 32X. Doom Release natif existant reste a 5,95 FPS contre 125,39 pour son interpreteur neuf, avec 32 421 blocs SH-2 master construits. Le profil anterieur placait environ 98 % de son echantillon dans CompileBlock ; aucun nouveau pourcentage de profil n est invente ici. Night Trap Release termine maintenant, mais ses 67–71 FPS natifs restent sous les 159–173 FPS des interpreteurs des passages correspondants. Ne pas confondre correction fonctionnelle et acceleration.

Le chemin SH-2 reste hybride : blocs CIL et petit prefixe x64 ; les delay slots/frontieres refusant un bloc peuvent encore emprunter le chemin interprete. Zero CompiledBlockFallbackCount indique absence d erreur IL ayant desactive le compilateur, pas absence de toute instruction interpretee. Doom n execute aucun prefixe x64 sur le master et un sur le slave. Les compteurs 68000 relevent encore 11 184 526 instructions de repli sur Doom et 6 712 230 sur Night Trap. Les trois options 68000 ont tres peu de chemin natif a influencer dans Doom ; leurs ecarts ne prouvent pas un gain SH-2.

A conserver : correction des projections Z80 deja validee, decodage sans remplissage anticipe couple a la signature des attentes, regression autonome de changement de timings, interpreteur original et selections disponibles. A poursuivre : cout de construction/reconstruction SH-2 et parcours natif incomplet, fenetres de code 68000 transparentes hors effets 32X, retrait Game Gear Release deja constate. Ne pas agrandir un cache sans mesurer ses evictions/reutilisations. Le GPU complet reste a implementer ; aucun resultat GPU n est invente.

Sondes temporaires et drapeau de projet retires ; Sh7095.cs et Chips.csproj retrouvent exactement leurs empreintes avant instrumentation. Les DLL finales ne contiennent pas DiagnosticAccess. Les SHA charges concordent avec les sorties normales pour les 50 passages retenus ; DLL conservees sous final/Debug et final/Release pour les prochaines comparaisons. Aucun commit/push. Preuves sous artifacts/Emulation/segaemu-autres-jeux/attempts/sh2-first-access : traces initiales/corrigees, code et sondes archives, logs des 34 tests par configuration, 54 JSON machine, summary.json, SHA sources/DLL.

## 2026-11-17 — Cause mesuree du cout de compilation SH-2 de Doom

Diagnostic distinct des FPS, trames 1 a 960 : 32 421 constructions master pour 9 778 adresses uniques ; 22 643 reconstructions apres eviction et 28 325 evictions, capacite 4 096. Aucun changement de code/timing ni eviction de variante. Preparation JIT : 137,5189252 s sur 138,6946262 s de construction. Ne pas attribuer ce cout a la machine hote. Preuve : attempts/sh2-cache-profile/doom-Release-before.json.

Correction candidate : capacite de cache parametrable dans Sh2.Blocks.cs, valeur de reference 4 096 conservee, essai 16 384 sur les deux SH-2 uniquement dans le banc. Changer la capacite libere les anciens blocs et liens ; une valeur identique les conserve ; zero refuse. 36 controles Sh2Tests passent dans chaque configuration, dont deux cas utiles d eviction/reutilisation/changement de capacite. Le premier lancement du projet temporaire a echoue car les proprietes globales de dossier obj se propageaient au ProjectReference Chips ; le banc utilise ensuite la DLL explicitement construite, pas une modification des sources pour contourner cet echec.

Attention chemins build : le premier build Chips sans Platform=x64 avait mis a jour bin/Release, alors que le banc reference bin/x64/Release. Invocation corrigee avant tout diagnostic execute. Ne pas reutiliser ce premier build comme preuve de DLL chargee. Les SHA charges sont inclus dans les JSON.

Comparaison machine complete en cours, interpreteur neuf / natif4096 / natif16384, Debug puis Release, Doom et Night Trap, 60 chauffe + 900 mesurees. Aucun gain declare avant resultats. Interpreteur conserve ; GPU complet et dynarec complet restent inacheves. Aucun commit.

### Resultats du cache SH-2 parametrable — controles termines

Deux titres, trois machines fraiches par passage (interpreteur, natif4096, natif16384), 60 trames de chauffe puis 900 mesurees dans chaque configuration. Seuls RunFrame chronometres ; images/PCM compares chaque trame, et registres/contexte SH-2, caches/peripheriques, SDRAM et RAM principales verifies tous les 60. Les quatre passages correspondent. Le profil detaille est desactive pendant les FPS. Ces Debug longs incluent le gameplay et ses compilations, contrairement au precedent Doom Debug de 180 trames : ne pas comparer directement leurs FPS pour annoncer une regression de l interpreteur.

Debug — FPS

| Jeu | Plateforme | Interpreteur neuf | Natif cache 4096 | Natif cache 16384 |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 19,35 | 4,68 | 10,30 |
| Night Trap disque 1 | Mega-CD + 32X | 24,99 | 21,66 | 21,67 |

Release — FPS

| Jeu | Plateforme | Interpreteur neuf | Natif cache 4096 | Natif cache 16384 |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 130,87 | 5,61 | 20,99 |
| Night Trap disque 1 | Mega-CD + 32X | 178,87 | 71,89 | 72,61 |

Doom construit 9778 blocs master contre 32421 ; slave 158 dans les deux cas. Night Trap reste a 1835 master et 22 slave. Retenir la capacite 16384 pour les deux SH-2 de la selection native de Mega32X uniquement : elle elimine les reconstructions constatees et ameliore fortement le passage Doom. Ne pas annoncer un gain Night Trap : ses blocs tenaient deja dans 4096. Le defaut de la puce reste 4096, reglable pour reproduire la reference ; interpreteur/dynamicBlocks non natif inchanges. Aucun autre CPU ou bus n est modifie dans cette correction.

Limites : les deux routes natives restent sous leur interpreteur neuf. Le cache ne supprime pas le cout des premieres compilations de 9778 adresses ; le JIT du code CIL reste un probleme majeur, la voie SH-2 n est pas un dynarec entierement x64. Ne pas presenter ce correctif comme la fin de la reparation. La consommation memoire peut augmenter car davantage de blocs restent residents ; aucun chiffre memoire isole par route n est mesure ici. GPU complet toujours absent.

36 controles autonomes SH-2 reussis dans chaque configuration, dont eviction et changement de capacite. Builds cibles Chips et Sega Debug/Release reussis ; apres raccordement de la politique dans Mega32X, wiring-Debug.json et wiring-Release.json verifient les capacites reelles des deux SH-2 avant toute surcharge du banc (4096 interpreteur, 16384 natif). Les FPS ci-dessus mesurent les memes capacites appliquees explicitement dans le banc avant ce raccordement ; ces controles de cablage ne sont pas de nouveaux FPS.

Preuves : attempts/sh2-cache-profile/summary.json, games-*.json avec SHA charges et checkpoints, diagnostic doom-Release-before.json, logs et sources du banc archives. Aucun commit/push.

Banc temporaire .work/Sh2CacheProfile et ses bin/obj supprimes apres fin des processus. Sources du banc et preuves conservees dans attempts/sh2-cache-profile.


## 2026-11-17 — Corps SH-2 reutilisables entre adresses, selection experimentale

Profil Doom cache16384 : 9778 constructions master mais 7489 signatures code/timing (7488 sequences d opcodes) ; 32,5993964 s de preparation JIT sur 32,9434215 s de compilation. Des opcodes uniques sont reconstruits a plusieurs adresses : 4F22 106 fois, 4F26 98, 2F86 96, 2F96 82, RTS 80. Preuve doom-profile.json. Ce profil n est pas un nouveau FPS.

Modification : extraction du generateur de corps dans CompileBlockBody, selection UseSharedCompiledBodies (false par defaut). La voie de reference conserve ses constantes d adresse. La variante charge l adresse du bloc en cours, puis reutilise le delegate pour les sequences exactement identiques avec memes attentes de fetch, delay slot, prefixe x64 et borne maximale de bus. Les versions, invalidations, adresses, liens et allocations x64 restent propres au bloc logique. Cache des corps borne a la capacite des blocs, vide au reset/changement de capacite/nettoyage ; pas de partage entre CPU. Le corps ne capture aucun board ni peripherique.

38 controles Sh2Tests reussis Debug et Release. Deux cas supplementaires controlent les corps relocalises avec lecture PC-relative, arithmetique/prefixe x64, branchements, budgets courts, attente 2/0/2, code modifie, et cache vide. Le compteur distingue corps generes et reuses. Aucun changement de Sh2.cs ni de l interpreteur.

Machine complete : interpreteur neuf, natif16384 reference, natif16384 corps partages. 60 trames de chauffe et 900 mesurees, memes entrees, sans fenetre ni limiteur ; RunFrame seul chronometre. Pixels/PCM a chaque trame ; registres/contexte SH-2, caches/peripheriques, SDRAM et RAM principales tous les 60. Quatre passages reussis. Pas de donnees historiques substituees aux interpreteurs.

Debug — FPS

| Jeu | Plateforme | Interpreteur neuf | Natif16384 reference | Corps partages |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 20,35 | 10,78 | 11,13 |
| Night Trap disque 1 | Mega-CD + 32X | 24,10 | 20,71 | 21,44 |

Release — FPS

| Jeu | Plateforme | Interpreteur neuf | Natif16384 reference | Corps partages |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 133,04 | 21,83 | 23,64 |
| Night Trap disque 1 | Mega-CD + 32X | 185,86 | 75,19 | 84,54 |

Doom : blocs logiques 9778 master / 158 slave dans les deux routes ; corps de 9778/158 a 7489/139, reuses 2289/19. Night Trap : blocs logiques 1835/22 inchanges ; corps de 1835/22 a 1264/17, reuses 571/5. La reduction des premieres compilations identiques est prouvee ; ces passages ne mesurent pas un nouveau seuil de vitesse apres une chauffe complete du gameplay. La voie reste hybride CIL et petit prefixe x64.

Decision : conserver la selection experimentale et l original, sans activer la nouvelle variante par defaut dans Mega32X. Gains observes limites (+3,3 % et +3,5 % Debug ; +8,3 % et +12,4 % Release), loin de l interpreteur et de la demande de forte acceleration. Ne pas prolonger des retouches CIL en les presentant comme la solution du dynarec complet. Restent le parcours natif incomplet, les replis 68000 et le cout des corps encore distincts ; ces points ne sont pas declares corriges. GPU complet absent, aucun nouveau test MD/MCD sans32X puisque cette passe ne modifie que les blocs SH-2. Aucun commit/push.

Preuves sous attempts/sh2-body-sharing : summary.json, games-*.json, SHA charges, checkpoints, doom-profile.json, logs de builds/38 tests et sources de banc archivees.

Banc .work/Sh2BodyProfile et ses bin/obj supprimes apres fin des processus ; sources, JSON et logs conserves dans attempts/sh2-body-sharing.


## 2026-11-17 — Backend SH-2 x64, tous encodages et contexte resident

Sources consultees localement : References/Machines/Sega/32X/PicoDrive/cpu/sh2/compiler.c et cpu/drc/emit_x86.c, plus les handlers existants de Sh2.Instructions.cs pour conserver leur comportement observable. Aucun nouveau telechargement. Les nouveaux fichiers Chips sont Compilation/Sh2X64Context.cs, Sh2X64Block.cs, Sh2X64Block.Memory.cs, Sh2X64Block.Control.cs et Sh2.NativeBlocks.cs. Contrat independant de Sega : seize registres empruntes, contexte architectural explicite, protocole de bus/horloge ; aucun appel d un payload a un handler gere d opcode. Reference Sh2.cs non modifiee.

Toutes les instructions entieres, T/Q/M, retenues/debordements, multiplication/division, MAC sature, transferts memoire, PC relatif, postincrement/predecrement, registres de controle, branches/delay slots, TRAPA/RTE/SLEEP et illegal/illegal-slot ont un payload x64. Le test parcourt les 65536 encodages et verifie que tous sont couverts. 18902 encodages sans service de bus/horloge : 9677824 cas ; 46634 autres : 2984576 cas avec traces d acces et chronologie. Les deux categories totalisent tous les encodages, y compris les invalides qui executent le trap natif. Cela ne constitue pas une validation exhaustive de toutes les combinaisons de memoire/peripheriques.

47 tests SH-2 (38 preexistants/corrections precedentes et 9 cas/suites nouveaux) reussissent Debug et Release. Apres la derniere optimisation de capture/restauration, les cinq cas affectes d execution complete et d erreur ont ete repasses dans chaque configuration. Ils controlent budgets courts, code RAM change, attente modifiee, IRQ, idle loops, captures de registres et caches vides pendant un callback ; une exception du bus revient au gere avant l instruction suivante et permet une reprise. Le banc initial utilisait PC 06000104 pour une RAM mappee a 00000000 : les sorties correspondaient mais aucune plage pure mappee n etait testee ; PC corrige a 00000104, assertion de fusion conservee. Ne pas attribuer ce premier echec de couverture au coeur.

Defauts corriges pendant ce portage : le delegate declare dans Sh2<TBus> etait refuse par Marshal.GetFunctionPointerForDelegate (type generique implicite), il est maintenant hors de la classe generique ; les decodes LDS.L 4006/4016/4026 et LDC.L 4007/4017/4027 etaient inverses, corriges selon la table existante. Les TRX d echec sont conserves.

Premiere machine complete : Doom Debug, 900 trames mesurees apres 60 chauffe, interpreteur neuf 19,48 FPS, ancien natif CIL cache16384 10,89, nouveau SH-2 x64 7,05. Sorties controlees concordantes mais ralentissement majeur : ne pas presenter cet essai comme un succes de vitesse. JSON games-Debug-doom.json conserve.

Profil separe de cinq secondes sur le nouveau parcours seul apres la trame 660 : dotnet-sampled-thread-time, top30 exclusif, environ 25,65 % dans Marshal.GetFunctionPointerForDelegate et 42,88 % attribues a X64CodeBuffer.ExecuteLinked. Cette derniere attribution inclut le temps situe sous la frontiere native, pas uniquement les controles du SafeHandle ; ne pas inventer une decomposition de ces 42,88 %. Le premier profil cpu-sampling etait reserve a collect-linux dans l outil installe ; invocation corrigee vers dotnet-sampled-thread-time, erreur initiale conservee. Le runner final s arrete cooperativement et est attendu/detruit dans finally. Le profil n est pas un nouveau FPS.

Correction : pointeur de callback cache ; contexte natif et registres pins pendant une tranche CPU entiere ; coeur de boucle et services de contexte/bus optimises dans le meme runtime genere que l interpreteur SH-2 existant. Le cache contient des traductions natives par opcode et des plages d instructions pures. Les retraits de code pendant une execution sont differes jusqu au retour de tous les appels actifs ; les tests vident effectivement les caches au milieu d un callback et apres faute. Le passage ABI direct est autorise seulement pendant ce bail, sans SuppressGCTransition (les payloads peuvent rappeler le gere). L execution native ne genere aucun corps CIL par bloc et ne revient pas a ExecuteOpcode. Les services du bus, de l horloge, des interruptions et des intrinseques idle restent des services hotes, pas des handlers d opcode.

Selection UseFullX64Blocks (avec UseCompiledBlocks) disponible sur la puce ; false par defaut, ni l interpreteur ni la selection actuelle de Mega32X ne sont remplaces. Les blocs CIL et les anciens prefixes restent comparables. Windows x64 et code dynamique requis explicitement, pas de fallback silencieux sur cette selection.

### Campagne apres correction du contexte resident

Trois machines fraiches par passage : interpreteur, natif existant cache16384, puis autres CPU natifs existants avec les deux SH-2 sur UseFullX64Blocks. 60 chauffe + 900 mesurees dans les deux configurations ; memes medias et entrees que precedemment ; RunFrame seul chronometre. Pixels/PCM chaque trame ; registres/contexte SH-2, caches/peripheriques, SDRAM/RAM, registres architecturaux et cycles 68000 main/sub controles tous les 60. Les quatre passages correspondent. Chaque interpreteur est remesure, sans substitution de valeur historique.

Debug — FPS

| Jeu | Plateforme | Interpreteur neuf | Natif existant CIL | SH-2 x64 resident |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 19,48 | 10,88 | 13,70 |
| Night Trap disque 1 | Mega-CD + 32X | 24,21 | 21,06 | 19,79 |

Release — FPS

| Jeu | Plateforme | Interpreteur neuf | Natif existant CIL | SH-2 x64 resident |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 131,26 | 23,38 | 55,43 |
| Night Trap disque 1 | Mega-CD + 32X | 182,53 | 74,41 | 84,85 |

Les compteurs SH-2 resident sont identiques entre configurations sur ces scenarios. Doom : master 57832216 instructions natives, 3177 traductions par opcode, 2190 plages pures et 65435 executions de plages ; slave 57009323 instructions, 143 traductions, 10 plages et deux executions de plages. Night Trap : master 81520759 instructions, 681 traductions, 356 plages et 2028979 executions de plages ; slave 6765750 instructions, 29 traductions, une plage/une execution. Zero corps CIL master/slave dans chaque passage de la nouvelle selection. Les intrinseques idle peuvent coalescer des boucles ; le compteur d instructions natives compte les dispatchs reels, pas les iterations coalescees.

Decision : conserver le portage et sa selection experimentale, ne pas le promouvoir par defaut. Doom gagne face au CIL existant, et le defaut majeur du premier essai Debug est reduit ; Night Trap perd environ 6 % Debug et gagne environ 14 % Release. Les deux jeux restent sous leur interpreteur neuf. La demande de forte acceleration n est pas satisfaite. Le passage ABI demeure par opcode pour les instructions non fusionnees, notamment memoire/branches : poursuivre de vrais blocs multi-instructions avec sorties budget/evenement/code modifie, pas d autres micro-retouches CIL. Le 68000 conserve aussi ses replis actuels et le GPU complet reste absent. Ne pas appeler cette campagne un dynarec complet de toute la machine.

Les sources n ont change que dans le SH-2 et ses tests ; aucun controleur MD/MCD hors SH-2 ou allocateur commun n est modifie. Les autres machines ne sont pas remesurees pour cette passe. Les commentaires du nouvel emetteur ont ete actualises apres mesures, sans changement executable. Les bancs profiler/mesure utilisent des dossiers de sortie distincts et Rebuild explicite pour eviter de reutiliser le mauvais exe de meme nom.

Preuves : attempts/sh2-full-native/summary.json, resident-games-*.json (SHA charges et checkpoints), premier games-Debug-doom.json, TRX de couverture/runner/resident, trace doom-full-Debug.nettrace et top30, logs et sources de banc archivees. Aucun commit/push.

Banc .work/Sh2FullNative, projets de profilage/tests/mesure et leurs bin/obj supprimes apres fin des processus. Les sources de banc archivees et les resultats restent sous attempts/sh2-full-native.


## Nouvelle campagne complete du 17 novembre 2026 - cinq colonnes

Demande : toutes les lignes remesurees, y compris 32X et Mega-CD + 32X. Aucun FPS historique reutilise. Aucun changement du code de production pendant cette campagne, aucun commit. Builds cibles Chips/Sega Debug et Release reussis, zero erreur (62 avertissements au build module).

Par configuration et machine : un interpreteur neuf et quatre cartes candidates neuves pour les machines a 68000, ou un interpreteur neuf et une carte native pour les machines a Z80 seul. Toutes les routes tournent successivement a chaque trame, avec memes entrees. Soit 35 cartes par configuration, 70 cartes liberees dans finally. Seul RunFrame est chronometre : compilation de blocs pendant cet appel incluse, chargement/checks exclus. Aucun affichage ni limiteur ni peripherique audio hote.

60 trames de chauffe puis 180 mesurees Debug, sauf Night Trap : 900 mesurees afin de lancer effectivement les SH-2 ; 900 mesurees Release partout. Start cinq trames tous les 120 a partir de 120. Images completes et PCM compares chaque trame ; RAM, champs architecturaux CPU et, sur 32X, SDRAM/caches/peripheriques SH-2 compares tous les 60. Les 20 passages multi-routes terminent sans ecart a ces points controles. Cela ne valide pas toute une partie ni tous les jeux.

Colonnes : Dynarec existant = selection UseNative68000Blocks de la carte, avec Z80 natif et parcours SH-2 compile existant lorsqu ils sont presents ; Registres ajoute UseNativeGuestRegisters sur les 68000 main/sub ; ROM stable ajoute UseNativeStableCode ; Combine active les deux. Le nouveau SH-2 UseFullX64Blocks et le parcours multi-instructions inacheve ne sont pas selectionnes ici. Cette campagne compare exactement les cinq colonnes demandees, pas toutes les autres experiences SH-2. Le 68000 et le SH-2 existants gardent leurs limites/replis : ne pas appeler ces routes un dynarec integral de toute la machine. Sans objet = aucune puce 68000 sur cette machine ; GPU complet toujours non implemente.

### Debug - FPS

| Jeu | Plateforme | Interpreteur | Dynarec existant | Registres | ROM stable | Combine |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 190,18 | 261,68 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 183,72 | 251,10 | Sans objet | Sans objet | Sans objet |
| Sonic | Mark III | 169,27 | 206,29 | Sans objet | Sans objet | Sans objet |
| Sonic | Master System | 180,63 | 221,81 | Sans objet | Sans objet | Sans objet |
| Sonic | Game Gear | 234,39 | 311,30 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 39,95 | 43,84 | 43,85 | 44,07 | 44,15 |
| Aladdin | Mega Drive | 27,35 | 30,78 | 30,48 | 30,45 | 30,30 |
| Batman Returns | Mega-CD | 32,35 | 39,47 | 39,17 | 39,39 | 38,99 |
| Doom | 32X | 20,71 | 16,49 | 16,41 | 16,46 | 16,50 |
| Night Trap, disque 1 | Mega-CD + 32X | 25,20 | 21,68 | 21,50 | 21,76 | 21,70 |

### Release - FPS

| Jeu | Plateforme | Interpreteur | Dynarec existant | Registres | ROM stable | Combine |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 1724,66 | 1746,72 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 4547,62 | 4216,80 | Sans objet | Sans objet | Sans objet |
| Sonic | Mark III | 1633,31 | 1450,67 | Sans objet | Sans objet | Sans objet |
| Sonic | Master System | 3631,12 | 3081,86 | Sans objet | Sans objet | Sans objet |
| Sonic | Game Gear | 3785,06 | 2557,68 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 818,83 | 944,20 | 915,20 | 968,78 | 958,34 |
| Aladdin | Mega Drive | 738,22 | 749,13 | 744,41 | 747,09 | 744,35 |
| Batman Returns | Mega-CD | 681,12 | 678,28 | 668,27 | 690,59 | 668,21 |
| Doom | 32X | 134,17 | 18,66 | 18,38 | 18,12 | 18,07 |
| Night Trap, disque 1 | Mega-CD + 32X | 224,24 | 56,47 | 56,10 | 56,81 | 56,89 |

Bilan : sorties controlees concordantes dans les deux configurations. A conserver comme preuves fonctionnelles limitees aux scenarios. La selection native reste plus lente sur Doom/Night Trap et sur plusieurs machines Z80 en Release ; ne pas promouvoir ces routes pour leur vitesse. Les variantes 68000 apportent peu ou perdent suivant le passage ; ne pas presenter leurs faibles ecarts comme une acceleration generale. Les valeurs sont celles des passages courts/longs explicites ci-dessus, et non une comparaison directe a des scenarios historiques differents.

Preuves : artifacts/Emulation/segaemu-autres-jeux/attempts/fresh-all-20261117 : 20 JSON, summary.json, empreintes SHA256 des DLL chargees dans chaque JSON, sources du banc archivees et logs des builds. Les bancs temporaires et leurs bin/obj sont supprimes apres sortie des processus.

## SH-2 : blocs x64 multi-instructions avec memoire et branches - 17 novembre 2026

Apres le tableau complet fresh-all, terminer le travail deja commence dans Sh2X64Block.Execution.cs : gardes budget/IRQ/PC, fetchs, fin d instruction/delay slot, sortie code change. Selection distincte UseMultiInstructionX64Blocks, avec UseFullX64Blocks et UseCompiledBlocks ; false par defaut. Le parcours resident par opcode reste disponible. Aucun changement de Sh2.cs, des bus MD/MCD, des autres CPU ou du chemin GPU dans cette passe.

Les blocs incluent les transferts memoire, operations de controle et leur delay slot mappe. Les callbacks gardent les services du bus/horloge ; aucun handler gere d opcode. Le contexte temporaire du bloc survit aux captures/restaurations des services. Apres acces ou evenement, verifier epoch puis mots/timings si necessaire ; quitter apres l instruction en cours si code change ou cache vide. Les baux existants gardent le code executable vivant pendant les callbacks. Les cas non mappables gardent le payload natif par opcode, sans retour a un handler interprete.

14 controles autonomes passent Debug et Release : parcours originaux et nouveaux, budgets courts, IRQ, changements de timings, idle, automodification, lecture/ecriture changeant l instruction suivante, delay slot normal/illegal, cache vide et faute du bus pendant execution puis reprise. Ils couvrent aussi les coeurs generes optimises. Les anciennes suites exhaustives de semantique par opcode n ont pas ete repassees : le payload autonome conserve sa branche originale ; cette validation concerne les nouvelles frontieres multi-instructions.

Machines : trois cartes neuves par passage, interpreteur / x64 resident par opcode / x64 multi-instructions, chaque route a chaque trame. 60 chauffe + 900 mesurees en Debug et Release. Seul RunFrame chronometre, compilation pendant les trames incluse. Pixels/PCM chaque trame ; registres/cycles 68000, SH-2, caches/peripheriques, RAM et SDRAM tous les 60. Les quatre passages terminent avec sorties controlees concordantes. Aucun ancien FPS utilise. Ces colonnes comparent les nouveaux parcours SH-2, pas les quatre options 68000 du tableau fresh-all.

### Debug - FPS

| Jeu | Plateforme | Interpreteur neuf | SH-2 x64 par opcode | SH-2 x64 multi-instructions |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 20,12 | 13,93 | 15,66 |
| Night Trap disque 1 | Mega-CD + 32X | 24,01 | 19,34 | 22,38 |

### Release - FPS

| Jeu | Plateforme | Interpreteur neuf | SH-2 x64 par opcode | SH-2 x64 multi-instructions |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 132,04 | 53,48 | 63,67 |
| Night Trap disque 1 | Mega-CD + 32X | 188,67 | 85,10 | 113,56 |

Compteurs identiques Debug/Release : Doom master 57832216 instructions, 9088 blocs construits et 24601355 appels de bloc ; slave 57009323 instructions, 145 blocs et 30409327 appels. Night Trap master 81520759 instructions, 2091 blocs et 23144647 appels ; slave 6765750 instructions, 18 blocs et 4957794 appels. Zero construction par opcode et zero corps CIL dans la route multi sur ces passages. Les compteurs d instructions correspondent au resident ; ils excluent les iterations coalescees par les intrinseques idle.

Resultat : face au resident par opcode, gain Debug Doom +12,47 %, Night Trap +15,71 % ; Release +19,06 % et +33,45 %. A conserver comme route experimentale et tests de frontieres. Ne pas promouvoir par defaut : les deux machines restent sous l interpreteur, le but de forte acceleration n est pas atteint. Limite concrete : encore 55,01 millions d appels de blocs sur Doom (environ 2,09 instructions par appel), 28,10 millions sur Night Trap. Le decoupage a diminue les appels mais conserve beaucoup de transitions ; les services memoire/horloge rappellent aussi le gere. Cela ne mesure pas leur temps propre et ne prouve pas qu un seul de ces couts explique toute la perte. Ne pas annoncer un dynarec complet de toute la machine : les 68000 gardent leurs replis precedents, GPU complet absent.

Sources et mesures : artifacts/Emulation/segaemu-autres-jeux/attempts/sh2-multi-native : 4 JSON machine + SHA des DLL chargees/checkpoints/compteurs, summary.json, deux TRX finaux, builds, sources du banc archivees. Les projets temporaires et bin/obj sont supprimes apres sortie des processus ; aucun commit/push.

## SH-2 : regions natives liees - 17 novembre 2026

Selection experimentale UseLinkedX64Regions ajoutee, false par defaut, avec UseFullX64Blocks et UseMultiInstructionX64Blocks. Les regions contiennent les mots deja mappes contigus (limite partagee 64 instructions). Les branches avant/arriere internes utilisent une table relative de sauts x64 en temps constant ; les destinations externes reviennent au repartiteur existant. Les sorties budget/IRQ/code/timing/sommeil/yield et les baux de liberation restent actifs. Aucune modification de Sh2.cs, de MD/MCD hors SH-2 ou du GPU.

Cas traite avant machines : un fetch a froid remplissant le cache peut couter plus cher que le fetch mappe des tours suivants. Le cout dynamique initial est charge une fois ; les retours internes sur le premier mot utilisent son timing mappe compile. Les remappages/changements de ce premier timing invalident aussi la region, meme si les mots restent identiques. Un premier mot non mappable ou change pendant le fetch refuse la liaison et garde le bloc natif precedent. Les compteurs Executed/LinkedHops et le timing initial survivent aux captures de contexte des callbacks.

27 controles autonomes reussissent Debug puis Release : parcours historiques et nouveaux, budgets courts, IRQ, idle, faute et cache retire pendant callback, lecture/ecriture modifiant le code suivant, branches avant/arriere, delay-slot normal/illegal, timing change pendant execution, premier fetch non mappe devenant mappe puis timings 0/2/0 entre budgets, coeurs normaux/generes optimises. Avant ajout des deux derniers cas, 25 controles passaient deja ; ne pas presenter cela comme couverture du fetch a froid. Premier build du projet temporaire : xUnit1009 apres ajout d un parametre a la theorie de faute ; attributs InlineData corriges avant execution. Aucun echec de puce attribue a cette erreur de harnais.

Mesures neuves : trois cartes par passage (interpreteur / blocs multi-instructions precedents / regions liees), 60 chauffe + 900 mesurees Debug et Release pour Doom et Night Trap. Seul RunFrame chronometre, compilation dans les trames incluse ; images/PCM chaque trame ; CPU/cache/peripheriques/RAM/SDRAM et registres/cycles 68000 main/sub tous les 60. Les quatre passages terminent sans ecart aux points controles. Aucun chiffre historique remplace un interpreteur neuf. Les colonnes sont ces trois parcours SH-2, pas les options 68000 du tableau fresh-all.

### Debug - FPS

| Jeu | Plateforme | Interpreteur neuf | Blocs x64 precedents | Regions x64 liees |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 18,11 | 14,11 | 13,82 |
| Night Trap disque 1 | Mega-CD + 32X | 24,44 | 22,75 | 22,85 |

### Release - FPS

| Jeu | Plateforme | Interpreteur neuf | Blocs x64 precedents | Regions x64 liees |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 128,17 | 60,98 | 57,42 |
| Night Trap disque 1 | Mega-CD + 32X | 184,07 | 111,56 | 111,52 |

Compteurs concordants Debug/Release : Doom appels de blocs master 24601355 -> 18730770, slave 30409327 -> 18029858, soit total 55010682 -> 36760628 (-33,18 %). 5322296 sauts internes master et 11638314 slave ; constructions 9088 -> 8908 master et 145 -> 142 slave. Night Trap appels master 23176664 -> 16903572, slave 4957794 -> 4075667 (total 28134458 -> 20979239, -25,43 %) ; sauts internes 4450199 et 882126. Les instructions natives executees correspondent entre les deux routes : Doom 57832216/57009323, Night Trap 81520759/6765750 master/slave. Zero corps CIL dans les deux routes. Les intrinseques idle excluent les iterations coalescees de ces compteurs.

Resultat negatif : reduire les retours au repartiteur ne suffit pas. Doom perd environ 2,06 % Debug et 5,84 % Release ; Night Trap reste a +0,47 % Debug et -0,04 % Release, aucune acceleration nette. Conserver la selection pour diagnostic/reproduction et les tests de frontieres ; laisser false par defaut et ne pas remplacer les blocs precedents. Ne pas refaire cette liaison comme nouveaute ni proposer plus de regions/caches sans preuve nouvelle. Les 68000 gardent leurs replis et le GPU complet reste absent : forte acceleration machine non atteinte.

Profil diagnostique distinct, cinq secondes de Doom lie seul apres la trame 780 (pas un autre FPS) : 48,03 % exclusifs attribues a InvokeLeasedNativePayload, 7,73 % Monitor.Enter_Slowpath ; YM2612 chan_calc 5,25 %, Vdp.merge 4,02 % et Compose32XLine 4,02 %. Le symbole de frontiere native inclut le code dessous et ne permet pas d isoler appel, payload et callbacks ; ne pas appeler ces 48,03 % un cout prouve de l ABI. Le slowpath Monitor ne prouve pas a lui seul de la contention. Le prochain diagnostic doit separer ces couts avant une nouvelle modification de liaison ou de gardes.

Le profil se termine cooperativement, enfant attendu puis Dispose dans finally ; erreur de trace declenche aussi arret/attente/kill de secours. Trace/top30, JSON/SHA et progression conserves sous attempts/sh2-linked-regions. Sources de bancs, TRX et DLL charges archives ; projets temporaires/bin/obj supprimes apres fin des processus. Aucun commit/push.


## SH-2 : tables Windows de deroulement de pile - 17 novembre 2026

Omission confirmee dans les payloads Sh2X64Block : prologue qui sauvegarde RBX/RSI/RDI et reserve 48 octets, appels de callbacks, mais aucune table Windows associee. X64CodeBuffer possede maintenant une surcharge avec UNWIND_INFO v1 optionnel : code, informations et RUNTIME_FUNCTION dans la meme allocation, enregistrement RtlAddFunctionTable avant publication, RtlDeleteFunctionTable avant VirtualFree. Les leases/SafeHandle existants gardent ces ressources vivantes pendant les appels. Create(code) reste sans table pour les autres utilisateurs. Le payload SH-2 fournit ses quatre operations de pile aux offsets 7/3/2/1, prologue de 16 octets. Aucune instruction emise, semantique de bus ou selection par defaut changee.

References primaires : https://learn.microsoft.com/en-us/cpp/build/exception-handling-x64 et https://learn.microsoft.com/en-us/windows/win32/api/winnt/nf-winnt-rtladdfunctiontable ; retrait https://learn.microsoft.com/en-us/windows/win32/api/winnt/nf-winnt-rtldeletefunctiontable. Cette correction concerne le contrat Windows des fonctions non feuilles, pas une promesse de gain FPS ni une preuve sur les anciens plantages dotnet.

43 controles reussis Debug et Release : 27 SH-2 existants de frontieres (budget/IRQ/code/timing/delay slot/fautes/retirement) et 16 nouveaux cas Windows. RtlLookupFunctionEntry retrouve le code reel emis par Sh2X64Block ; RtlVirtualUnwind restaure les registres non volatils, RSP et RIP du caller depuis les prologues partiels, corps et epilogues reels. Les tests de retrait observent la disparition de la table, les allocations ordinaires restent non enregistrees et les entetes invalides sont refuses. Ressources des tests liberees dans finally, y compris si une creation censee echouer reussissait.

Campagne neuve : interpreteur / blocs SH-2 multi-instructions / regions liees, trois cartes fraiches par passage, Doom et Night Trap, 60 chauffe + 900 mesurees Debug et Release. RunFrame seul chronometre, compilation pendant les trames incluse ; pixels/PCM chaque trame, CPU/cache/peripheriques/RAM/SDRAM et registres/cycles 68000 tous les 60. Les quatre passages sont concordants aux points controles. Interpreteur remesure dans chaque passage ; pas de FPS historique reutilise. Pas de comparaison avant/apres controlee permettant d attribuer un gain a la table Windows.

### Debug - FPS

| Jeu | Plateforme | Interpreteur neuf | Blocs x64 | Regions x64 liees |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 20,07 | 15,62 | 15,29 |
| Night Trap disque 1 | Mega-CD + 32X | 24,34 | 22,80 | 22,85 |

### Release - FPS

| Jeu | Plateforme | Interpreteur neuf | Blocs x64 | Regions x64 liees |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 132,13 | 60,83 | 58,10 |
| Night Trap disque 1 | Mega-CD + 32X | 185,33 | 109,09 | 107,50 |

Profil de cinq secondes du lie seul apres trame 780 : collecte/retrait enfant cooperatif et attente/Dispose reussis. Toujours 49,78 % exclusifs attribues a InvokeLeasedNativePayload (contre 48,03 % dans le profil anterieur), sans decomposition fiable de son payload et de ses callbacks. La table Windows est verifiee par les API, mais ce profileur ne fournit toujours pas la separation recherchee ; ne pas presenter le probleme de diagnostic comme termine. Autres lignes : Compose32XLine 5,55 % exclusifs, Monitor.Enter_Slowpath 4,02 %, Vdp.merge 3,70 %, Ym2612.chan_calc 3,39 %. Ces profils ne constituent pas une comparaison statistique des petits pourcentages ni une preuve de contention.

A conserver : enregistrement/retrait des tables SH-2 et tests Windows. Ne pas promouvoir les regions liees ni annoncer une acceleration : elles restent sous les blocs precedents sur Doom Release, et les deux routes restent sous l interpreteur. Le diagnostic doit maintenant horodater directement le payload natif et le callback, dans une selection instrumentee distincte sans assimiler ses temps aux FPS normaux ; tache ajoutee, non realisee. Ne pas refaire une modification de liaison sur la seule baisse du nombre d appels. Les autres generateurs x64 ne recoivent pas automatiquement une table : ils doivent decrire leur propre pile s ils sont non feuilles ; aucune validation de leur deroulement n est revendiquee ici.

Preuves : attempts/sh2-unwind : 4 JSON machines avec SHA/checkpoints/compteurs, summary.json, deux TRX 43 cas, trace/top30 et JSON de profil. Sources et DLL chargees archivees, projets temporaires/bin/obj supprimes apres fin des processus. Aucun commit/push. Les autres backends CPU, GPU complet et replis 68000 restent a traiter dans le but global.

## SH-2 : intervalles natifs et callbacks mesures directement - 17 novembre 2026

UseNativeTimingProfile (false par defaut) genere LFENCE/RDTSC autour du payload et de ses callbacks. Le descripteur emprunte contient un pointeur vers les compteurs locaux a Execute, hors contexte architectural ; capture/restauration ne les efface pas, retrait de code et fautes restent couverts par les leases. Aucune instruction de chronometrage emise quand desactive. Categories read/write/clock/idle/other ; mesures inclusives en ticks TSC, pas cycles de la console ni temps propre CPU. Les synchronisations executees a l interieur d un callback read/write restent comptees dans cette categorie, les appels geres hors payload ne font pas partie du denominateur.

49 controles passent Debug et Release : parcours historiques et instrumentes, budgets/IRQ/cache/fautes, egalite registres/horloges/traces, 576 cas de payload instrumente avec MAC et pile, somme des categories et remise a zero des compteurs, controles Windows de pile. Les observations de jeu instrumentees comparent les sorties natives multi/liees, sans les presenter comme FPS normaux. Calibration de 200000 appels apres 1000 chauffe : payload vide environ 67,81 ticks Debug et 64,62 Release ; intervalle callback vide environ 114,37 et 87,74 ticks. Ces couts et les compteurs/fences perturbent la route instrumentee ; ne pas soustraire ces moyennes comme une decomposition exacte. Les stackalloc de calibration ont ete sortis de la boucle bornee avant son build Release.

Profils : 780 trames de chauffe puis 60 instrumentees, Doom et Night Trap, Debug/Release, cartes multi et liee neuves ; images/PCM chaque trame et CPU/cache/RAM tous les 60 concordants. Part des callbacks dans les intervalles des payloads multi instrumentes :

| Configuration | Jeu | Master | Slave | Categorie dominante master |
| --- | --- | ---: | ---: | --- |
| Debug | Doom | 75,27 % | 76,43 % | clock |
| Debug | Night Trap | 75,03 % | 63,47 % | read |
| Release | Doom | 69,23 % | 69,82 % | clock |
| Release | Night Trap | 68,21 % | 57,42 % | read |

Doom : 3682197 callbacks master dont 3329337 clock sur 60 trames ; ce sont principalement les echeances emises par le bloc, pas un cout demontre de son seul appel ABI. Night Trap master : 11468910 callbacks dont 8121896 read ; slave presque uniquement idle. Les appels du callback vide sont bien moins couteux que le callback complet, mais ne constituent pas une preuve que tous les couts restants viennent de la copie du contexte.

Controle horloge supplementaire Release Doom, JSON distinct clock-detail-Release-doom.json : aux trames 780 et 840, master/slave deferent les horloges, DMA0/DMA1/DMAOR a zero, FRT select0, OCRA1, flags15 (trois drapeaux deja positionnes et remise a zero automatique), masque IRQ0, compteur0 ; prochaines echeances a 4..8 horloges. Source SchedulePeripheralEvent continue a choisir la comparaison A toutes les huit horloges, meme quand tous les drapeaux sont deja positionnes. SynchronizePeripherals ne rappelle UpdateInterrupt que si les drapeaux changent. Cible concrete suivante : regrouper les reveils FRT sans transition observable, en gardant materialisation aux acces et sauvegardes, effacement/rearmement et route originale. Aucun gain de cette correction encore mesure dans cette section.

FPS normaux, instrumentation desactivee, interpreteur neuf / multi / lie : 60 chauffe + 900 mesurees Debug puis Release ; RunFrame seul chronometre, pixels/PCM chaque trame et etats tous les 60 concordants.

### Debug - FPS

| Jeu | Plateforme | Interpreteur neuf | Blocs x64 | Regions liees |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 19,37 | 14,70 | 14,49 |
| Night Trap disque 1 | Mega-CD + 32X | 24,67 | 22,87 | 22,82 |

### Release - FPS

| Jeu | Plateforme | Interpreteur neuf | Blocs x64 | Regions liees |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 132,72 | 60,72 | 57,76 |
| Night Trap disque 1 | Mega-CD + 32X | 186,59 | 108,52 | 108,53 |

Les regions restent sans avantage general ; aucune promotion. Le chronometrage direct a identifie deux cibles distinctes (echeances FRT et lectures memoire), sans modifier l interpreteur ni annoncer un gain a partir de donnees instrumentees. Sources/JSON/SHA/TRX/calibrations sous attempts/sh2-native-timing, bancs temporaires archives et supprimes apres fermeture des proprietaires. Aucun commit.

## FRT SH7095 : regroupement des reveils sans transition - 17 novembre 2026

Correction fondee sur les intervalles natifs mesures : Doom utilisait OCRA=1, flags FRT=15 et masque IRQ=0, avec DMA desactive, mais SchedulePeripheralEvent imposait encore un reveil toutes les huit horloges alors que tous les flags periodiques etaient deja positionnes. UseCoalescedLatchedTimerEvents conserve le chemin original (false, defaut de la puce reutilisable). Si true et flags 0x0e tous positionnes, le prochain checkpoint FRT est repousse d un tour de compteur 16 bits ; serial/watchdog/DMA restent dans le minimum des echeances. Les lectures/ecritures et SaveContext materialisent toujours le compteur, le changement de selection synchronise les horloges, l effacement des flags rearme les echeances normales. Aucun flag ou compteur retire.

40 controles passent Debug et Release : sept nouveaux cas FRT (six configurations de diviseur, comparaisons, remise a zero et compteur ; cas huit horloges/rearmement) et 33 controles SH-2 de frontieres/instrumentation. Contextes materialises, flags, compteur et IRQ concordants, sauvegarde/restauration et retour a la selection originale. Erreur initiale du harnais : NextEventCycle demande directement au controleur au lieu de son CpuBus ; invocation corrigee sans ajouter une API de production inutile.

Quatre cartes fraiches par passage : interpreteur original / interpreteur regroupe / dynarec multi original / dynarec multi regroupe. Doom et Night Trap, Debug puis Release, 60 chauffe + 900 mesurees. Seul RunFrame chronometre ; images et PCM chaque trame, etats CPU/cache/peripheriques/RAM/SDRAM et main/sub68000 tous les 60. Avant comparaison, Cpu.SynchronizeBusClock et Sh7095.SaveContext materialisent les peripheriques des quatre cartes. Les seules echeances privees _busDeadline/_nextPeripheralEvent sont exclues de l egalite : leur changement est le but de la selection ; registres, cycles, compteurs, flags, caches, RAM et audio/video compares restent concordants dans les quatre passages. Aucun ancien FPS reutilise.

### Debug - FPS

| Jeu | Plateforme | Interpreteur original | Interpreteur regroupe | Dynarec original | Dynarec regroupe |
| --- | --- | ---: | ---: | ---: | ---: |
| Doom | 32X | 18,68 | 19,75 | 14,25 | 16,36 |
| Night Trap disque 1 | Mega-CD + 32X | 23,55 | 23,53 | 21,62 | 21,77 |

### Release - FPS

| Jeu | Plateforme | Interpreteur original | Interpreteur regroupe | Dynarec original | Dynarec regroupe |
| --- | --- | ---: | ---: | ---: | ---: |
| Doom | 32X | 128,63 | 191,25 | 60,47 | 98,47 |
| Night Trap disque 1 | Mega-CD + 32X | 195,00 | 202,74 | 108,93 | 110,04 |

Doom : +5,72 % interpreteur et +14,85 % dynarec Debug ; +48,68 % et +62,83 % Release. Night Trap : -0,12 % interpreteur et +0,71 % dynarec Debug ; +3,97 % et +1,01 % Release. Ne pas transformer les tres petits ecarts en gains certains. Les deux parcours dynarec restent sous leur interpreteur, et la forte acceleration globale n est pas terminee.

### Fenetre finale Release - trames 781 a 960

Releves tires des differences des runningTicks des checkpoints 780/960 du meme passage (Stopwatch.Frequency du meme hote consigne dans late-window.json), pas une autre campagne ni une estimation Debug. Cette fenetre evite que la moyenne Night Trap masque sa phase plus lente apres lancement des SH-2.

| Jeu | Interpreteur original | Interpreteur regroupe | Dynarec original | Dynarec regroupe |
| --- | ---: | ---: | ---: | ---: |
| doom | 128,96 | 190,63 | 62,56 | 104,27 |
| nighttrap | 97,70 | 98,94 | 45,88 | 46,44 |

La compilation des blocs SH-2 sur tout le passage Release coute environ 0,70 -> 0,66 seconde Doom et 0,15 -> 0,15 Night Trap (master+slave), avec 9088 et 2032 constructions master identiques. Cela ne justifie pas de revenir a l ancienne explication du CIL dont le profil etait domine par le JIT : ce backend est different. La priorite restante est le cout du parcours natif et de ses lectures/callbacks, particulierement le passage actif CD32X a ~46 FPS.

Decision : retenir le regroupement. Active sur les deux SH7095 dans le constructeur SegaEmu Mega32X, donc 32X et Mega-CD + 32X en interpreteur ou selections compilees ; faux reste disponible pour reprendre le chemin original et le defaut de la puce reutilisable ne change pas. Aucun changement de Sh2.cs, de GPU ni de MD/MCD sans 32X. Constructeur Mega32X protege par finally en cas d initialisation incomplete ; Dispose detache les actions et libere les caches des deux coeurs meme si un retrait echoue, sans detruire les ressources empruntees.

Builds cibles Sega Debug/Release reussis. Apres activation : quatre JSON wiring (32X/CD32X x Debug/Release), deux cartes I/natif chacun, flags master/slave vrais et contextes initiaux identiques a une selection explicite false puis true. Les SHA post-cablage sont dans ces JSON. Les FPS precedents utilisaient deja exactement cette selection true sur les cartes candidates ; les DLL du benchmark et celles du cablage final sont archivees separement. Les regions liees et l instrumentation restent desactivees par defaut.

Preuves : attempts/sh2-frt-events : 4 JSON machines, 4 JSON wiring, late-window.json, summary.json, TRX et logs, bancs et DLL archives ; banc temporaire/bin/obj supprimes apres fermeture des proprietaires. Aucun commit/push.

## Toutes les lignes remesurees apres corrections SH-2 - 17 novembre 2026

Nouvelle campagne : 20 passages, 70 cartes neuves possedees/liberees dans finally, aucun ancien FPS reutilise. Aucun code de production modifie pendant cette campagne, aucun commit. Builds cibles du module Sega et ses dependances Debug puis Release : zero erreur ; builds du banc : zero erreur/avertissement. Application et autres modules non construits.

Seul RunFrame est chronometre, sans fenetre/limiteur/sortie audio hote, compilation survenant dans RunFrame incluse. Chargement et controles exclus. 60 trames de chauffe puis 180 mesurees Debug, sauf Night Trap : 900 ; 900 mesurees Release pour chaque ligne. Les differences de longueur interdisent de comparer directement les ratios Debug/Release comme une charge identique. Entrees identiques, Start cinq trames tous les 120 a partir de la trame 120. Un interpreteur remesure et quatre candidates neuves lorsque le 68000 existe ; deux cartes sur les machines Z80 seul.

Colonnes : Dynarec existant selectionne UseNative68000Blocks, donc le Z80 natif lorsqu il existe, avec les deux SH-2 UseFullX64Blocks + UseMultiInstructionX64Blocks sur les candidates 32X/CD32X. Registres ajoute UseNativeGuestRegisters sur les 68000 main/sub ; ROM stable ajoute UseNativeStableCode ; Combine active les deux. Sans objet = absence de 68000, pas une mesure manquante a recopier. Par rapport a la precedente campagne fresh-all, la selection SH-2 change : les chiffres ne constituent donc pas un avant/apres de code a selection identique. Regions liees, lectures cache natives experimentales et instrumentation desactivees ; regroupement FRT actif sur toutes les cartes 32X, y compris l interpreteur. Les replis 68000 et autres limites subsistent : ce n est pas un dynarec integral de toute la machine. Le GPU complet n est toujours pas implemente et ne fait pas partie des colonnes demandees ici.

Images completes et PCM compares apres chaque trame, champs CPU/RAM suivis tous les 60 ; sur 32X, contexte des peripheriques materialise avant comparaison, avec registres/cycles/cache/IO/SDRAM verifies. Echeances privees _busDeadline/_nextPeripheralEvent exclues comme dans les derniers bancs FRT. Les 20 passages terminent sans difference aux points controles ; cela ne prouve pas la compatibilite de toute une partie ni de tous les jeux.

### Debug - FPS

| Jeu | Plateforme | Interpreteur | Dynarec existant | Registres | ROM stable | Combine |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 208,03 | 289,80 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 200,09 | 278,25 | Sans objet | Sans objet | Sans objet |
| Sonic | Mark III | 189,07 | 232,69 | Sans objet | Sans objet | Sans objet |
| Sonic | Master System | 182,94 | 229,76 | Sans objet | Sans objet | Sans objet |
| Sonic | Game Gear | 242,14 | 325,97 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 41,18 | 45,84 | 46,11 | 46,19 | 45,73 |
| Aladdin | Mega Drive | 30,92 | 35,02 | 34,90 | 34,85 | 34,87 |
| Batman Returns | Mega-CD | 31,39 | 37,98 | 37,66 | 37,98 | 37,76 |
| Doom | 32X | 21,88 | 17,82 | 18,06 | 17,96 | 17,97 |
| Night Trap, disque 1 | Mega-CD + 32X | 24,49 | 22,73 | 22,62 | 22,78 | 22,67 |

### Release - FPS

| Jeu | Plateforme | Interpreteur | Dynarec existant | Registres | ROM stable | Combine |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 2456,11 | 2628,28 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 4198,39 | 3916,54 | Sans objet | Sans objet | Sans objet |
| Sonic | Mark III | 1709,26 | 1500,07 | Sans objet | Sans objet | Sans objet |
| Sonic | Master System | 3162,04 | 2649,35 | Sans objet | Sans objet | Sans objet |
| Sonic | Game Gear | 3766,52 | 2457,66 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 799,99 | 864,68 | 864,40 | 892,42 | 883,75 |
| Aladdin | Mega Drive | 710,24 | 709,63 | 713,31 | 717,51 | 715,98 |
| Batman Returns | Mega-CD | 533,85 | 528,30 | 515,78 | 526,99 | 519,32 |
| Doom | 32X | 188,70 | 94,46 | 94,58 | 93,01 | 93,81 |
| Night Trap, disque 1 | Mega-CD + 32X | 197,41 | 107,64 | 107,00 | 107,23 | 106,71 |

### Dernieres 180 trames des passages 32X

| Configuration | Jeu | Interpreteur | Dynarec existant | Registres | ROM stable | Combine |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Debug | Doom | 21,88 | 17,82 | 18,06 | 17,96 | 17,97 |
| Debug | Night Trap, disque 1 | 18,80 | 15,95 | 15,93 | 15,97 | 15,98 |
| Release | Doom | 189,66 | 102,69 | 102,78 | 101,27 | 102,52 |
| Release | Night Trap, disque 1 | 90,79 | 45,12 | 44,57 | 44,41 | 44,23 |

Bilan et decisions :
- Bon sur ces passages : concordance des sorties/champs compares ; gain Debug de la selection native sur les cinq machines Z80, Pico, MD et MCD. Pico gagne aussi en Release. Conserver ces preuves fonctionnelles et ces observations limitees aux scenarios.
- Mauvais en Release : SC-3000, Mark III, SMS et GG perdent avec le Z80 natif. MD est presque a egalite ; MCD perd legerement. Ne pas presenter les variantes Registres/ROM stable comme une acceleration generale ni retenir les petits ecarts comme acquis.
- Mauvais sur 32X/CD32X : les quatre candidates restent sous l interpreteur dans les deux configurations. La moyenne Night Trap masque encore une phase active bien plus lente, visible dans la fenetre finale. La reparation de performance demandee n est pas terminee ; ne pas promouvoir ces routes pour leur vitesse.
- Diagnostic du banc : en Debug les compteurs SH-2 ont ete collectes apres Dispose, qui detache l adaptateur : le champ diagnostics contient donc des null, sans effet sur les durees ni les controles deja executes. Capture corrigee avant Dispose pour Release, sans autre changement du scenario ou du chronometrage. Program-Debug.cs.txt et Program.cs.txt conservent exactement les deux sources. Les quatre candidates Release attestent des instructions SH-2 natives et zero corps CIL SH-2 construits ; cela ne signifie pas zero replis 68000.

Preuves : artifacts/Emulation/segaemu-autres-jeux/attempts/fresh-all-after-sh2-20261117 : vingt JSON avec ticks/checkpoints/SHA des DLL chargees, summary.json, logs, deux sources du banc, projet/cas/script et DLL archives. Banc temporaire/bin/obj supprimes apres fin des processus, aucun commit/push.

## Lectures de donnees du cache SH7095 dans les payloads natifs - 17 novembre 2026

Cause traitee : une lecture deja presente dans le cache SH7095 repassait par le callback natif -> C# et les copies de contexte. Ajout d une vue empruntee ISh2NativeReadCacheBus (cache/tags/validite/controle/politique live) et d une selection UseNativeCachedReads, fausse par defaut. Le payload lit directement les hits purs zero attente ; garde de mode, echeance, plage, taille/limite de ligne et voies. Les misses, IO, acces avec attente et toutes les ecritures gardent le callback original. Cache RAM et lecture big-endian/signe conserves. Les tableaux sont epingles seulement pendant l appel natif, y compris les callbacks/GC ; pas de ressource native durable ajoutee par cette vue. Aucun changement de Sh2.cs ni de MD/MCD sans 32X dans cette passe.

61 controles cibles passent en Debug et Release, dont matrice 5760 combinaisons de taille/alignement/voie/CCR/mode/plage, echeances dues et changement de mode avec GC compactant en callback, execution complete avec MAC, fautes/reprise et vidage des caches en callback. Les tests verifies incluent aussi les suites SH-2 natives, FRT et unwind Windows ; ce n est pas toute la suite Chips ni toute la compatibilite jeux.

Trois cartes neuves par passage : interpreteur / dynarec multi-instructions existant avec callbacks / meme dynarec avec lectures cache natives. FRT regroupe actif sur les trois ; regions liees et instrumentation desactivees dans les mesures FPS. 60 trames de chauffe puis 900 mesurees dans les deux configurations. Seul RunFrame chronometre, chargement/controles exclus, sans fenetre, limiteur ni audio hote. Entrees identiques, Start comme dans les derniers bancs. Images/PCM verifies chaque trame ; CPU/cache/IO/RAM/SDRAM et main/sub68000 controles tous les 60 apres materialisation des peripheriques. Les echeances privees _busDeadline/_nextPeripheralEvent sont exclues comme dans le banc FRT. Les quatre passages terminent sans divergence aux points controles, pas une preuve de compatibilite de toute une partie.

### Debug - FPS

| Jeu | Plateforme | Interpreteur neuf | Dynarec callbacks | Dynarec lectures cache natives |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 20,44 | 16,85 | 15,91 |
| Night Trap disque 1 | Mega-CD + 32X | 23,87 | 22,20 | 22,93 |

### Release - FPS

| Jeu | Plateforme | Interpreteur neuf | Dynarec callbacks | Dynarec lectures cache natives |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 188,66 | 96,64 | 92,15 |
| Night Trap disque 1 | Mega-CD + 32X | 186,47 | 108,81 | 127,55 |

### Fenetre finale - trames 781 a 960

| Configuration | Jeu | Interpreteur | Dynarec callbacks | Dynarec cache natif |
| --- | --- | ---: | ---: | ---: |
| Debug | doom | 19,82 | 16,80 | 15,85 |
| Debug | nighttrap | 18,64 | 15,60 | 17,01 |
| Release | doom | 189,77 | 103,10 | 99,47 |
| Release | nighttrap | 99,47 | 48,78 | 63,65 |

Diagnostic distinct, uniquement Release, 60 trames apres 780, deux cartes natives instrumentees par jeu. Les deux profils terminent sans divergence aux controles sorties/etats ; ne pas utiliser leur duree instrumentee comme FPS normaux. Night Trap master : lectures callbacks 8 121 896 -> 535 898 ; 7 585 998 hits natifs, exactement la difference, soit 93,40 % des lectures de cette fenetre. Callbacks totaux master 11 468 910 -> 3 882 912. Slave : aucun hit, callbacks inchanges. Doom : aucun hit sur master/slave dans cette fenetre, memes comptes de callbacks ; les gardes/pins/volume de code du chemin ajoute n y evitent aucun callback. Le profil etablit l absence de travail evite, pas la part exacte de chacun de ces surcouts dans la perte FPS.

Decisions :
- Bon mais seulement sur une partie des cas : Night Trap gagne +3,28 % Debug et +17,23 % Release sur la moyenne ; +8,99 % Debug et +30,48 % Release sur les 180 dernieres trames. Conserver la selection et ses preuves ; gain local prouve sur ce passage, pas un gain general ni une victoire sur l interpreteur.
- Mauvais : Doom perd -5,61 % Debug et -4,65 % Release sur la moyenne. Ne pas activer globalement UseNativeCachedReads ni remplacer la voie originale. Ne pas refaire cette meme proposition de lookup inline universel comme une nouvelle idee sans traiter le chemin sans hit.
- Limite persistante : meme avec cette lecture native, Night Trap reste sous l interpreteur et sa phase active Release ne donne que 63,65 FPS contre 99,47. Les autres couts de parcours natif demeurent. Ce resultat ne valide pas un dynarec integral ni un GPU complet.

Preuves : artifacts/Emulation/segaemu-autres-jeux/attempts/sh2-cached-reads : quatre JSON FPS, deux JSON diagnostiques, summary.json, late-window.json, TRX Debug/Release, sources/projets/scripts/logs et DLL/SHA archives. Banc temporaire .work/Sh2CachedReads et ses bin/obj supprimes apres fin des processus ; tests autonomes utiles conserves dans tests. Aucun commit/push.


## Contexte CPU SH-2 direct, 17 novembre 2026

Constat : le bus natif restaurait puis recapturait le contexte architectural a chaque callback. Le Z80 natif existant montre deja comment travailler directement dans les champs CPU avec des offsets CLR et un pin interieur. Nouvelle selection UseLiveX64Context false par defaut : offsets derives par references gerees Unsafe.As/ByteOffset, CPU epingle par PC pendant tout Execute et R epingle separement ; mots 64 bits/booleens de largeur exacte. Aucun offset d objet CLR code en dur. Les callbacks voient et modifient directement les vrais registres, IRQ, cycles, echeances et flags. Les donnees de traduction supplementaires ne sont pas serialisees. La selection reste hors Execute et invalide les blocs.

Premier essai : vue commune NativeX64Frame a base de tableau d offsets dans les boucles gerees. Il modifiait aussi inutilement le parcours original. Archive separee initial/ : quatre JSON, logs, DLL et deux sources. Ne pas refaire cette vue commune comme une nouvelle optimisation. Correction : driver et callback Span originaux restaures depuis le snapshot precedent ; nouveau driver/callback distincts a champs CPU types. Emetteur des instructions, compilateur de blocs, validation/coherence et services de bus restent partages. Le petit dispatch de callback choisit le helper correspondant au pointeur courant, y compris les payloads autonomes a contexte logique. Les tables d offsets ne sont utilisees que pour emettre les instructions natives et non pour parcourir le contexte dans le driver gere.

Validation finale : 67 tests cibles passes Debug et Release. Nouvelles comparaisons interpreteur/contexte copie/direct sur familles d instructions et 1024 encodages tires avec graine fixe, en CPU simple et helpers specialises ; callbacks observant/modifiant registres/IRQ, GC compactant, fautes/reprise/retirement, bascule direct -> original. La suite cache native ajoute les deux cas de contexte direct avec MAC/changement de politique/fautes. Les autres controles concernent budgets/frontieres/timings/regions liees/FRT/unwind ; ce n est pas toute la suite Chips.

Quatre passages machines definitifs : trois cartes neuves par jeu/configuration, 60 trames de chauffe et 900 mesurees, seul RunFrame chronometre, pas de fenetre/limiteur/sortie audio hote. Meme scenario Start que les autres campagnes. Images/PCM chaque trame ; CPU/cache/IO/RAM/SDRAM et main/sub68000 tous les 60 avec peripheriques materialises. Les quatre passages et les deux profils finissent sans divergence aux points controles. NativeFull+Multi selectionnes sur les candidates, lecture cache native false pour isoler le contexte ; FRT regroupe true sur toutes. GPU complet absent ; replis 68000 persistants. MD/MCD sans 32X non touches par les sources de cette passe.

### Debug - FPS definitifs

| Jeu | Plateforme | Interpreteur neuf | Dynarec contexte copie | Dynarec contexte direct |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 19,23 | 15,59 | 16,08 |
| Night Trap disque 1 | Mega-CD + 32X | 23,96 | 21,94 | 22,66 |

### Release - FPS definitifs

| Jeu | Plateforme | Interpreteur neuf | Dynarec contexte copie | Dynarec contexte direct |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 174,26 | 87,01 | 93,48 |
| Night Trap disque 1 | Mega-CD + 32X | 174,94 | 100,35 | 111,26 |

### Dernieres 180 trames - 781 a 960

| Configuration | Jeu | Interpreteur | Contexte copie | Contexte direct |
| --- | --- | ---: | ---: | ---: |
| Debug | doom | 18,78 | 15,69 | 16,15 |
| Debug | nighttrap | 18,17 | 14,81 | 15,84 |
| Release | doom | 179,26 | 94,13 | 102,69 |
| Release | nighttrap | 89,63 | 43,90 | 50,11 |

### Premier essai conserve, sans melanger ses temoins

| Configuration | Jeu | Interpreteur | Vue commune, copie | Vue commune, direct |
| --- | --- | ---: | ---: | ---: |
| Debug | doom | 20,27 | 16,24 | 16,38 |
| Debug | nighttrap | 24,80 | 22,07 | 22,52 |
| Release | doom | 182,13 | 88,86 | 93,90 |
| Release | nighttrap | 181,74 | 102,42 | 112,42 |

Profil Release distinct des FPS : 60 trames apres 780, instrumentation TSC des deux SH-2 dans les deux selections, memes controles. Nombre de callbacks identique : Doom 1 699 224, Night Trap 12 065 988 (master+slave). Intervalles callbacks inclusifs Doom 610 583 352 -> 513 438 945 ticks TSC (-15,91 %) ; Night Trap 3 546 137 317 -> 2 933 614 645 (-17,27 %). Ne pas appeler ces ticks du temps CPU propre ni une fraction de toute la machine. Le cout des callbacks baisse, mais le gain machine reste limite.

Compteurs 68000 du meme profil, differences 780 -> 840 : Doom main 693 960 instructions de repli, zero nouvelle entree dans un bloc natif (une seule entree cumulee), identiques pour les deux routes SH-2. Night Trap main 420 replis, sub zero ; des blocs 68000 sont bien executes. Ce constat pointe un travail distinct et deja liste : les banques 32X a handlers bloquent les fenetres de code 68000. Il ne prouve pas leur part en temps et n autorise pas a supprimer leur synchronisation sans verification.

Decisions/limites :
- Retenir la selection directe comme candidate fonctionnelle, sans l activer par defaut : +3,10 % Doom / +3,30 % Night Trap Debug, +7,43 % / +10,87 % Release dans la campagne definitive. C est un gain limite, insuffisant pour valider l acceleration globale ou depasser l interpreteur.
- Rejeter la vue commune a offsets dans le driver gere ; conserver le parcours Span d origine pour comparaison et les deux jeux de preuves. Les methodes Span de boucle et callback sont comparees textuellement au snapshot precedent dans original-driver-preservation.json.
- Sh2.cs est inchange. Cela ne prouve pas a lui seul l absence de regression de vitesse de l interpreteur : la classe recoit des champs de backend et NativeRuntimeBuilder copie des helpers supplementaires. Les FPS interpreteur definitifs sont sous ceux d une campagne precedente ; aucune cause de cet ecart n est etablie. Gate restant avant promotion : comparer anciennes DLL et DLL actuelles dans un passage apparie avec meme scenario, pas recycler les anciennes valeurs.
- Aucun resultat GPU, aucune affirmation de dynarec integral. La correction globale reste en cours ; prochaine priorite de code apres controle de la reference : replis 68000 Doom et synchronisation des fenetres ROM 32X.

Preuves : attempts/sh2-live-context : initial/ conserve, quatre JSON FPS definitifs, deux profils, summary.json, late-window.json, TRX, logs, sources/projets/scripts et DLL/SHA archives. Banc .work/Sh2LiveContext et ses bin/obj supprimes apres sortie des processus, nouveaux tests autonomes conserves. Aucun commit/push.

## Interpreteur : comparaison appariee des DLL avant/apres contexte direct - 17 novembre 2026

Objectif : verifier la reference interpreteur apres l ajout du backend SH-2 direct, sans recycler les anciens FPS. Anciennes DLL : attempts/sh2-cached-reads/dll/Benchmark ; actuelles : DLL construites pour sh2-live-context. Meme worker binaire pour les deux versions, aucune API du nouveau backend appelee. MachineResources native false ; les deux SH-2 verifies UseCompiledBlocks=false. Aucun changement du code de production pendant cette verification.

Deux AssemblyLoadContext independants dans un meme processus par configuration. Les DLL effectivement chargees sont verifiees sous leurs dossiers respectifs et leurs SHA256 releves. Ordre d execution alterne chaque trame. 60 chauffe + 900 mesurees pour chacun des quatre passages Doom/Night Trap x Debug/Release. Chronometre dans le worker autour de RunFrame seulement : reflection, chargement et controles exclus ; pas de fenetre/limiteur/audio hote. Images et PCM compares chaque trame ; champs CPU/RAM/cache/SDRAM/peripheriques materialises compares tous les 60, selon les snapshots des derniers bancs. Les huit cartes sont fermees dans finally ; les huit contextes sont decharges et leur collecte verifiee par WeakReference apres retour des frames proprietaires.

Le protocole a deux versions isolees differe des precedentes campagnes a plusieurs cartes dans le meme contexte. Les valeurs absolues de ces protocoles ne sont pas un avant/apres ; les deux colonnes ci-dessous constituent la comparaison appariee. Aucun FPS ancien reutilise.

### Debug - FPS interpreteur

| Jeu | Plateforme | Anciennes DLL | DLL actuelles | Ecart |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 20,75 | 19,78 | -4,69 % |
| Night Trap disque 1 | Mega-CD + 32X | 24,66 | 24,22 | -1,81 % |

### Release - FPS interpreteur

| Jeu | Plateforme | Anciennes DLL | DLL actuelles | Ecart |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 162,30 | 166,86 | +2,81 % |
| Night Trap disque 1 | Mega-CD + 32X | 200,46 | 198,33 | -1,06 % |

### Fenetre finale, 180 trames - 781 a 960

| Configuration | Jeu | Anciennes DLL | DLL actuelles |
| --- | --- | ---: | ---: |
| Debug | doom | 20,23 | 19,29 |
| Debug | nighttrap | 18,97 | 18,74 |
| Release | doom | 169,55 | 174,76 |
| Release | nighttrap | 95,10 | 92,88 |

Bilan : quatre passages complets sans divergence aux points controles, huit contextes effectivement liberes. La baisse generale de l interpreteur supposee depuis les valeurs des campagnes precedentes n est pas confirmee : Doom Release +2,81 %, Night Trap -1,06 %. Le retrait Debug Doom -4,69 % et Night Trap -1,81 % est conserve, y compris la fenetre finale ; ne pas le presenter comme zero perte. Un passage par jeu/configuration ne suffit pas a en attribuer la cause a la disposition des champs, au JIT ou a autre chose. Aucune cause de charge hote invoquee. Cela ne valide pas toutes les machines ou tous les jeux, et ne transforme pas le dynarec plus lent en acceleration validee.

Echec du banc conserve sous unload-failed/ : premiere comparaison Doom terminee sans divergence, mais les contextes restaient retenus ; JSON Success=false/Fps=null, pas un resultat de performance retenu. Le worker serialisait ses metadonnees avec des types anonymes definis dans son assemblee collectible, a travers le cache JSON par defaut. Remplacement par des dictionnaires de types primitifs ; passage court Debug de 61 trames avec collecte verifiee sur Doom et Night Trap (smoke/), puis quatre nouveaux passages complets avec collecte verifiee. Ne pas reutiliser le modele de metadata anonyme dans ce banc. Aucune modification de SegaEmu pour corriger ce probleme du banc.

Decision : reference fonctionnelle controlee, absence de grande baisse generale dans ce passage, petits retraits conserves dans les notes. Le backend direct reste selectionnable et non active par defaut. Le verrou prioritaire constate dans le profil SH-2 reste le 68000 de Doom presque entierement en repli, a traiter sans supprimer aveuglement les effets des handlers 32X.

Preuves : attempts/sega-interpreter-reference : quatre JSON complets, summary.json, SHA des DLL reellement chargees, DLL/worker/runner/sources/scripts/logs archives ; unload-failed/ et smoke/ conserves. .work/SegaInterpreterReference et bin/obj/staged supprimes apres fermeture des cartes, collecte des contextes et fin des processus. Aucun commit/push.


## Nouvelle campagne complete a la demande utilisateur - 17 novembre 2026

Vingt nouveaux passages, dix plateformes, cinq cartes independantes par passage : 100 cartes creees et liberees dans finally. Tous les FPS ci-dessous proviennent des JSON de cette campagne, interpreteur compris. Aucun FPS historique repris. Builds cibles Chips/Sega et dependances Debug puis Release : zero erreur, 62 avertissements par configuration ; banc : voir logs archives. Aucun code de production modifie pendant cette campagne, aucun commit/push.

Protocole : machine emulee complete sans fenetre ni limiteur, chargement hors chronometre ; 60 trames de chauffe puis 180 trames mesurees en Debug et 900 en Release. Seul RunFrame est chronometre. Ordre des cinq routes tourne a chaque trame. Memes images/audio/entrees par jeu ; image et PCM compares a chaque trame, etats CPU/RAM/cache/peripheriques compares tous les 60 apres materialisation des horloges SH7095. Vingt passages sans divergence aux points controles. Il ne s agit pas d une validation de partie complete. Les deux configurations ne couvrent pas la meme longueur : ne pas comparer directement leur moyenne comme un facteur de vitesse Debug/Release.

Selections : Interpreteur = parcours interprete neuf ; Dynarec existant = backend natif disponible, avec replis 68000 restants ; Registres = UseNativeGuestRegisters ; ROM stable = UseNativeStableCode ; Combine = les deux options. Pour 32X/CD32X, SH-2 Full+Multi actif sur les quatre candidates ; linked regions, native cached reads, live native context et instrumentation temporelle desactives. FRT regroupe actif sur toutes les cartes. La fenetre ROM 32X experimentale en cours de correction reste desactivee. Z80 natif selectionne sur les candidates. Registres/ROM stable/Combine sont des options 68000 : sans objet sur SG/SC/MarkIII/SMS/GG ; les quatre repetitions natives independantes de ces lignes restent archivees mais ne sont pas quatre routes Z80 distinctes. Aucun FPS GPU : cette colonne ne fait pas partie du tableau demande et l emulation complete GPU n est pas implementee.

### Debug - FPS

| Jeu | Plateforme | Interpreteur | Dynarec existant | Registres | ROM stable | Combine |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 161,80 | 209,21 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 158,76 | 210,62 | Sans objet | Sans objet | Sans objet |
| Sonic | Mark III | 148,64 | 178,29 | Sans objet | Sans objet | Sans objet |
| Sonic | Master System | 151,77 | 181,56 | Sans objet | Sans objet | Sans objet |
| Sonic | Game Gear | 202,00 | 269,40 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 37,15 | 40,85 | 40,95 | 40,96 | 41,10 |
| Aladdin | Mega Drive | 28,81 | 32,27 | 32,29 | 32,64 | 32,66 |
| Batman Returns | Mega-CD | 30,36 | 36,61 | 36,19 | 36,57 | 36,52 |
| Doom | 32X | 16,53 | 13,05 | 13,75 | 13,57 | 11,72 |
| Night Trap, disque 1 | Mega-CD + 32X | 30,77 | 33,50 | 33,53 | 33,42 | 32,76 |

### Release - FPS

| Jeu | Plateforme | Interpreteur | Dynarec existant | Registres | ROM stable | Combine |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Choplifter | SG-1000 | 3708,68 | 3207,83 | Sans objet | Sans objet | Sans objet |
| Choplifter | SC-3000 | 4760,50 | 4433,03 | Sans objet | Sans objet | Sans objet |
| Sonic | Mark III | 3833,51 | 2991,47 | Sans objet | Sans objet | Sans objet |
| Sonic | Master System | 4155,92 | 3226,87 | Sans objet | Sans objet | Sans objet |
| Sonic | Game Gear | 4114,49 | 2258,34 | Sans objet | Sans objet | Sans objet |
| 101-piki Wan-chan | Pico | 773,27 | 858,53 | 849,73 | 877,78 | 880,58 |
| Aladdin | Mega Drive | 708,62 | 682,66 | 650,17 | 706,86 | 698,55 |
| Batman Returns | Mega-CD | 420,29 | 542,48 | 542,62 | 551,08 | 538,26 |
| Doom | 32X | 177,03 | 83,53 | 83,22 | 83,08 | 83,84 |
| Night Trap, disque 1 | Mega-CD + 32X | 208,57 | 107,98 | 107,54 | 102,71 | 106,29 |

### Bilan de cette campagne

- A conserver comme observations : gain natif Debug sur les machines Z80, Pico, MD et MCD ; Pico et Batman Returns gagnent aussi en Release. Ces constats concernent les passages mesures, sans attribuer les petits ecarts a une cause non mesuree.
- A corriger : pertes natives Release sur les cinq machines Z80 et Aladdin ; Doom reste moins rapide en natif, et Night Trap Release passe de 208,57 FPS interpreteur a 102,71-107,98 FPS selon la candidate. Les quatre repetitions natives Z80 ne doivent pas etre interpretees comme des options Registres ou ROM stable ayant un effet sur ce processeur.
- Night Trap Debug ne couvre que les 240 premieres trames, chauffe comprise, et donc davantage le demarrage que les anciens passages de 960 trames. Sa moyenne ne prouve pas un gain dans la phase active ; le passage Release plus long contient cette phase. Les checkpoints et ticks cumules permettent d examiner cette difference.
- Rien ne justifie de promouvoir une route comme acceleration generale. L interpreteur reste disponible. La correction de performance globale du dynarec n est pas declaree terminee ; ce point livre la campagne complete demandee.

Preuves : artifacts/Emulation/segaemu-autres-jeux/attempts/fresh-all-now-20261117 : vingt JSON, summary.json, sources du banc en .txt, cas/scripts, logs builds/execution, DLL et SHA. Les SHA des DLL effectivement chargees figurent aussi dans chaque JSON. Aucun test externe permanent ajoute ; banc temporaire supprime apres fin effective des processus.



## Fenetres de code 68000 32X avec synchronisation de fetch - 17 novembre 2026

Correction experimentale selectionnable, originale conservee. Les aliases ROM 88-9F etaient refuses au compilateur 68000 a cause des handlers de banque. Ajout de fenetres ROM bornee/mirroirs et invalidation enable/banque/reset/restauration. Les fetchs purs sans synchronisation ont echoue : divergence native a la trame 6, interpreteur pur des la trame 4 (horloge/peripheriques SH-2). Ces echecs sont archives ; ne pas refaire cette suppression aveugle.

Correction : contrat reutilisable ICpu68000ObservedCodeBus et observations aux transitions de fetch emises par le bloc natif, y compris refresh/extensions differees. Les lectures de donnees gardent les handlers, notamment PC-relatives ; les BIOS/IO/framebuffer restent exclus. Selection UseMapped32XRomFetch false par defaut ; SynchronizeMapped32XRomFetch true quand la fenetre est choisie. L invalidation initialement detachee au reset a ete rebranchee dans Map32XBanks.

Validation : 20 controles ciblés reussis en Debug et Release (banques/miroirs/limites/invalidation, synchronisation et bascules, traces horloge originales/natives avec/sans extensions et registres caches). Quatre controles courts de 61 trames, puis huit passages longs de 60 chauffe + 900 mesurees. Images/PCM par trame et CPU/RAM/cache/peripheriques tous les 60 concordent. Trois cartes neuves par passage, detruites dans finally ; SHA des DLL chargees dans chaque JSON. Aucun ancien FPS. Ce controle cible les quatre machines affectees ; la derniere campagne toutes machines reste separee.

Selections natives : SH-2 Full+Multi actifs, linked/cache reads/live context false ; quatre machines complètes sans fenetre ni limiteur. La nouvelle fenetre 68000 est seule activee sur la troisieme carte. Les deux colonnes natives sur MD/MCD sans 32X sont des temoins identiques de code, leur difference ne mesure pas un effet de la fenetre 32X. Aucun changement de production pendant les mesures.

### Debug - FPS

| Jeu | Plateforme | Interpreteur neuf | Natif actuel | Natif + fenetre synchronisee |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 19,90 | 15,99 | 17,50 |
| Night Trap, disque 1 | Mega-CD + 32X | 22,60 | 20,63 | 21,99 |
| Aladdin | Mega Drive | 27,72 | 30,96 | 30,80 |
| Batman Returns | Mega-CD | 28,99 | 33,36 | 33,43 |

### Release - FPS

| Jeu | Plateforme | Interpreteur neuf | Natif actuel | Natif + fenetre synchronisee |
| --- | --- | ---: | ---: | ---: |
| Doom | 32X | 157,90 | 80,28 | 81,13 |
| Night Trap, disque 1 | Mega-CD + 32X | 150,20 | 88,49 | 89,77 |
| Aladdin | Mega Drive | 549,22 | 584,87 | 583,08 |
| Batman Returns | Mega-CD | 368,47 | 394,36 | 391,77 |

Doom sur les 960 trames execute 11 184 526 instructions 68000 en repli dans le natif actuel, une seule entree native ; avec la fenetre synchronisee : zero repli 68000, 326 251 entrees et 745 blocs construits, identiques en Debug/Release. Cela ne signifie pas zero callback : les synchronisations sont maintenant des services appeles depuis les blocs emis.

Bilan : gain natif Doom Debug +9,44 %, Night Trap Debug +6,58 % ; en Release seulement +1,06 % et +1,45 %. Ces faibles gains ne reparent pas le deficit global face a l interpreteur. Garder la fenetre comme route de diagnostic/correction du refus de code ROM, sans la promouvoir comme solution de performance complete. Les temoins MD/MCD ne divergent pas. La suite doit isoler les sorties vers le C#, acces bus et synchronisations, comme demande par l utilisateur ; pas multiplier les opcodes natifs sans mesurer ces couts.

Preuves : attempts/m68k-32x-code : before-lifecycle-fix, unsynced-native, interpreter-window, observed/smoke et observed (huit JSON longs, deux TRX, DLL/SHA/sources). Le premier essai de compilation des nouveaux tests utilisait Cpu68000 sans parametre generique : test corrige en Cpu68000<MegaDriveCpuBus> avant execution. Banc temporaire/bin/obj supprimes apres fin effective des processus. Aucun commit/push.


## Diagnostic des passages natifs et synchronisations 32X - 17 novembre 2026

Suite a la piste utilisateur : profiler bus, synchronisations et sorties natives, plutot que poursuivre seulement les instructions SH-2. Deux nouveaux diagnostics Release Doom/Night Trap, chacun avec deux cartes natives (actuel / fenetre 68000 synchronisee), 780 trames de preparation puis 60 instrumentees. Images/audio compares par trame et etats materialises CPU/RAM/cache/peripheriques tous les 60 : aucune divergence. Aucun code de production modifie pendant ce diagnostic.

TSC : intervalles mesures depuis les payloads natifs, callbacks inclusifs regroupes lecture/ecriture/horloge/attente/autre. Compteurs distincts de payloads et instructions. Stopwatch : temps inclusif des handlers ROM 68000 88-9F et observations de fetch, avec detection des horloges identiques consecutives. Ces observations peuvent executer les SH-2 : ne pas additionner leur duree aux intervalles SH-2, ni les appeler cout propre de l interface. Pas de FPS normal calcule a partir de ce diagnostic instrumente.

| Jeu | Route | Instructions SH-2 | Appels payload | Callbacks | Part callbacks dans payload | Lectures | Ecritures | Horloge | Attente |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| doom | native-current | 6189965 | 3021174 | 1699224 | 48,79 % | 667783 | 351749 | 0 | 679692 |
| doom | native-observed-rom | 6189965 | 3021174 | 1699224 | 52,91 % | 667783 | 351749 | 0 | 679692 |
| nighttrap | native-current | 17410178 | 5384831 | 12065988 | 69,25 % | 8121896 | 1005447 | 2331852 | 597044 |
| nighttrap | native-observed-rom | 17410178 | 5384831 | 12065988 | 68,69 % | 8121896 | 1005447 | 2331852 | 597044 |

| Jeu | Route | Handlers ROM 68000 | Horloges handlers repetees | Observations fetch | Horloges observations repetees | Temps inclusif (ms) |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| doom | native-current | 1732680 | 1038480 | 0 | 0 | 786,57 |
| doom | native-observed-rom | 0 | 0 | 694320 | 120 | 715,14 |
| nighttrap | native-current | 0 | 0 | 0 | 0 | 0,00 |
| nighttrap | native-observed-rom | 0 | 0 | 0 | 0 | 0,00 |

Constats et prochaine correction a cibler :
- Doom : la route nouvelle supprime 1 038 360 appels de fetch ROM sur cette fenetre et reduit les horloges repetees de 1 038 480 a 120. Les callbacks SH-2 restent strictement 1 699 224. Il ne reste pas de volume significatif de doublons de meme horloge a supprimer dans ce nouvel observateur ; ne pas reprendre la route pure rejetee ni sacrifier les synchronisations utiles.
- Night Trap : 12 065 988 callbacks SH-2, dont 8 121 896 lectures, 2 331 852 horloges, 1 005 447 ecritures et 597 044 services d attente. Environ 69 % du temps interne aux payloads est dans ces callbacks. Les 5 384 831 entrees de payload pour 17 410 178 instructions montrent aussi des blocs courts (environ 3,23 instructions par entree). Les lectures et leur transfert de contexte sont prioritaires ; le cout du retour au driver doit etre isole, pas assimile au temps des instructions emises.
- Doom : 3 021 174 entrees pour 6 189 965 instructions (environ 2,05 instructions par entree), environ 49-53 % du temps interne des payloads dans les callbacks, dont 679 692 services d attente. Un zero dans la categorie horloge ne signifie pas absence de synchronisation : celle-ci existe aussi dans les lectures, attentes et observes 68000.
- La part callbacks mesure un cout inclusif C#/bus/services et passages ABI. Elle ne prouve pas que tout ce temps soit gaspille dans les interfaces. Ne pas presenter un pourcentage propre de surcout d interface sans comparaison ou mesure specifique. Les compteurs de lecture cache native et la route de contexte direct existent deja : leurs essais/gains/pertes restent dans le journal et ne doivent pas etre refaits comme des idees nouvelles.
- Conserver la concordance fonctionnelle et les reductions de replis deja obtenues ; la route native complete reste plus lente que l interpreteur sur 32X, et cette campagne ne declare pas la reparation terminee. Aucun commit/push.

Preuves : attempts/sh2-bus-boundaries : deux JSON bruts, summary.json, sources en .txt, logs, DLL et SHA. Banc temporaire/bin/obj supprimes apres fin effective du processus et liberation de ses cartes dans finally.


## Attentes SH-2 certifiees emises en x64 - 17 novembre 2026

Cause ciblee apres le diagnostic des raccordements : les boucles BRA/NOP, NOP/BRA/NOP et DT/BF passent encore dans un helper C# pour calculer les iterations sautables. Ajout de UseNativeInlineIdle false par defaut et de ISh2NativeIdleBus : seuls les mots deja mappes, sans effet de bord et au timing certifie peuvent utiliser le calcul x64. Les autres cas conservent le helper. Aucun changement de Sh2.cs ni des instructions interpretees.

Calcul natif : borne min(budget, evenement), IRQ dont NMI, division entiere et maximum du compteur, maintien des registres/PC/PPC/PollingEpoch et horloges pendantes ; synchronisation a l echeance existante. Un evenement qui change les mots pendant le step courant fait abandonner le plan et relire via le helper. Le booléen AccelerateIdleLoops reste dynamique, y compris apres callback et avec contexte CPU direct. Aucun nouveau pointeur durable ou ressource native possedee.

Defaut de la premiere certification introduite dans cette passe : avec certains CCR, le fetch non cache et les attentes de donnees du helper different. Le test sur un vrai SH7095, 16 combinaisons basses du CCR, a reproduit l erreur (ccr-before-fix.trx). Restriction des aliases caches avec inhibition des instructions, sans retirer le chemin original. Les premiers quatre passages et deux profils reussis sont conserves sous pre-ccr-guard, mais ne sont pas les FPS finaux du code corrige. La premiere compilation avait aussi une erreur de visibilite du champ interne de l emetteur, corrigee ; log conserve.

Validation finale : 128 tests passes dans chaque configuration, dont comparaison interpreteur/natif inline sur cinq formes de boucle, acceleration active/inactive, deux echeances, contexte copie/direct, refus et bascule de selection, NOP modifie des la premiere horloge, IRQ, changement de politique, DT aux bornes et horloge au-dela de 32 bits ; controles existants SH-2/cache/FRT/unwind inclus. Quatre nouveaux passages longs du code final, 60 trames chauffe + 900 mesurees, trois cartes neuves par passage ; interpreteur remesure, images/audio par trame et etats CPU/RAM/cache/peripheriques aux points de 60 trames concordent. Fermeture dans finally, sans fenetre ni limiteur.

Colonnes : natif precedent = 68000 avec fenetre synchronisee, SH-2 Full+Multi ; nouveau = meme selection + UseNativeInlineIdle. NativeCachedReads, LiveNativeContext, regions liees et instrumentation temporelle false dans les mesures FPS. Les deux colonnes natives gardent donc les memes autres reglages. SH-2 ne concerne ici que 32X/CD32X : aucun test d une machine qui n utilise pas cette puce dans cette passe.

### Debug - FPS finaux

| Jeu | Plateforme | Interpreteur neuf | Natif precedent | Natif + attentes x64 | Gain natif |
| --- | --- | ---: | ---: | ---: | ---: |
| Doom | 32X | 18,30 | 15,76 | 16,23 | 2,97 % |
| Night Trap disque 1 | Mega-CD + 32X | 19,20 | 17,81 | 18,01 | 1,09 % |

### Release - FPS finaux

| Jeu | Plateforme | Interpreteur neuf | Natif precedent | Natif + attentes x64 | Gain natif |
| --- | --- | ---: | ---: | ---: | ---: |
| Doom | 32X | 154,88 | 78,72 | 82,43 | 4,71 % |
| Night Trap disque 1 | Mega-CD + 32X | 149,10 | 85,33 | 86,90 | 1,83 % |

### Diagnostic distinct - 60 trames actives Release (781-840)

| Jeu | Route | Callbacks attente | Tous callbacks SH-2 | Appels de payload |
| --- | --- | ---: | ---: | ---: |
| doom | native-observed-rom | 679692 | 1699224 | 3021174 |
| doom | native-observed-rom-inline-idle | 26316 | 1045848 | 3021174 |
| nighttrap | native-observed-rom | 597044 | 12065988 | 5384831 |
| nighttrap | native-observed-rom-inline-idle | 0 | 11468951 | 5384831 |

Bilan :
- Bon : concordance aux points controles, elimination de 96,13 % des callbacks attente Doom et de tous ceux de Night Trap sur cette fenetre. Le nombre total de callbacks baisse de 1 699 224 a 1 045 848 sur Doom et de 12 065 988 a 11 468 951 sur Night Trap. Garder cette route comme candidat mesurable, originale conservee et nouvelle option non activee par defaut.
- Limite/mauvais : les gains FPS finaux restent faibles et les deux routes natives restent sous l interpreteur. Ne pas transformer la reduction d une categorie de callbacks en validation du dynarec complet ni en gain de meme pourcentage sur la machine. Les lectures restent nombreuses ; les appels de payload ne baissent pas (3 021 174 et 5 384 831 sur la fenetre). Les retours au pilote C# entre blocs et les lectures restent a traiter, plutot que prolonger seulement les instructions arithmetiques.
- A ne pas refaire : certification generale du fetch sans verifier les attentes du helper et le changement de code pendant le step ; suppression de fetchs non certifies ; publier les chiffres pre-ccr-guard comme ceux du code final. Les essais contexte direct/cache natif/regions liees deja consignes restent des precedents a consulter.
- Les profils TSC/compteurs instrumentes sont separes des FPS normaux ; aucune addition de temps inclusifs imbriques ni attribution de tout le temps callback a l interface. Ce point ne termine pas le chantier global de performance. Aucun commit/push.

Preuves : attempts/sh2-inline-idle : quatre JSON finaux, deux profils finaux, summary.json, deux TRX finaux, echec CCR et premiers essais separes, builds/logs, sources/DLL/SHA archives. Banc temporaire/bin/obj supprimes apres sortie des processus et liberation des proprietaires dans finally.


## Essai SIMD du cache SH7095 retire - passage a Saturn, 17 novembre 2026

Prototype UseNativeVectorCachedReads : index entrelace des tags valides compare par SSE2, rejet precoce des plages non cachees, maintenance des tags et reconstruction a la restauration. 137 tests passes dans chaque configuration, incluant les matrices scalaires/SIMD, priorite/validite, purges/chargements/etats et GC. Quatre passages complets 60 chauffe + 900 mesurees, cinq cartes neuves, images/PCM et etats compares sans divergence aux points controles. Les chiffres ci-dessous sont ceux du prototype, pas des DLL restaurees.

### Debug - FPS prototype rejete

| Jeu | Interpreteur neuf | Natif precedent | Cache scalaire | SIMD | SIMD + contexte direct |
| --- | ---: | ---: | ---: | ---: | ---: |
| doom | 17,47 | 15,39 | 14,27 | 14,29 | 14,46 |
| nighttrap | 21,96 | 21,11 | 21,95 | 21,93 | 21,63 |

### Release - FPS prototype rejete

| Jeu | Interpreteur neuf | Natif precedent | Cache scalaire | SIMD | SIMD + contexte direct |
| --- | ---: | ---: | ---: | ---: | ---: |
| doom | 153,68 | 80,63 | 76,99 | 76,18 | 77,38 |
| nighttrap | 154,50 | 91,14 | 110,86 | 110,70 | 106,72 |

Diagnostic distinct Release, 60 trames actives : Doom zero hit natif de cache dans les deux routes, 1 045 848 callbacks. Night Trap : 7 585 998 hits dans les deux, 3 882 953 callbacks. SIMD ne supprime donc aucun callback supplementaire au cache scalaire. Aucun gain de vitesse etabli : Night Trap Release scalaire 110,86 contre SIMD 110,70 ; la combinaison SIMD+contexte direct baisse a 106,72. Ne pas generaliser le gain du contexte direct seul a son association au cache.

Decision : retirer uniquement le prototype SIMD ajoute dans cette passe, son index derive et son test autonome. Conserver interpreteur, cache scalaire, contexte direct, attentes x64 et fenetre 68000 precedents. Sources et DLL prototype archives sous prototype/ avec SHA. Restauration depuis les fichiers archives avant cette passe, puis builds Chips/Sega Debug/Release et 128 controles existants passes dans chaque configuration. Aucun nouveau FPS apres restauration : l utilisateur demande de passer maintenant a la machine Saturn ; ne pas poursuivre une campagne 32X supplementaire.

A ne pas refaire sans nouvelle preuve : remplacer la recherche scalaire par cet index SIMD ou activer globalement cache+contexte direct en supposant leurs gains additifs. Le cache natif garde un interet sur Night Trap et penalise Doom dans ces scenarios. Les millions de retours entre blocs et les couts hors payload restent ouverts ; aucune reparation globale annoncee. Aucun commit/push. Banc temporaire/bin/obj supprimes apres fermeture des proprietaires et fin des processus.
