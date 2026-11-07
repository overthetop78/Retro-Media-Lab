# WD1770/1772 — avancement entre échéances internes, 7 novembre 2026

## Code conservé

Référence de production : `6fb1fa3c1`, qui possède déjà le regroupement de rotation inactive. La puce propose désormais `AdvanceWithStableInputs` dans son interface propre. Elle consomme uniquement les clocks précédant le prochain échantillon du séparateur ou l'expiration du timer ; elle peut retourner zéro. Le câblage traite alors les clocks restantes par son chemin unitaire, dans la **même** période demandée. Ni front, ni notification, ni phase d'échantillonnage n'est repoussé à une période suivante.

Le séparateur possède `ClocksUntilWork` et `AdvanceQuiet` : ses compteurs et niveaux précédents restent dans son circuit interne. Les états de décodage, CRC, mots et notifications ne sont pas recalculés entre deux échéances. L'écriture et ses fronts de précompensation restent unitaires. Aucun thread, agenda périodique ou composant Simulation n'est ajouté ; les communications restent directement entre puces et câblage Acorn.

`Wd1770Drive` présente seulement un intervalle sans transition physique index/RD/flux. Après le nombre de clocks réellement consommées, il actualise son temps et sa rotation, puis reprend TickSilicon à l'échéance. L'événement s'exécute ainsi avec les entrées et le contexte temporel habituel du lecteur, notamment en cas de réentrance d'un récepteur. Les fréquences, commandes, délais et modes mécaniques sont inchangés.

Le premier prototype exécutait l'échéance dans l'avancement puis rendait la main sur une mutation de sortie ; sa génération de notifications a été retirée au profit du retour **avant** l'échéance, plus simple et sans état supplémentaire. Un cache additionnel de prochaine transition physique a ensuite été essayé : 26,606468 ms/image sur LOAD, contre 26,091434 dans le passage précédent sans ce cache. Aucun gain supplémentaire n'est établi ; le cache et son invalidation dans la restauration existante sont retirés. Aucun développement de sauvegarde/chargement d'état n'est conservé.

## Scénario et mesures de carte complète Debug

Même banc que [le passage précédent](idle-clock-validation.md) : Model A 32 Kio, MOS 1.20, BASIC II, SAA5050, DFS WD1770 2.26 variant 1 banque 14, source BBC 2 MHz et WD 8 MHz. SSD IJK Invaders ouvert explicitement en lecture seule, aucun changement de configuration. Le 6502 exécute cent OSCLI LOAD INV/A 3000 demandés dans une boucle BASIC, interrompue par Escape après la fenêtre de mesure ; PRINT I retourne réellement 9 après la fin du transfert en cours.

Rendu et drainage audio inclus, aucun test ou build concurrent, aucune option Release en Debug, aucune fenêtre RML. Même exécutable de diagnostic ; seules les DLL Acorn et Chips de référence/candidat sont alternées. Les deux paires finales portent sur la forme **sans** cache physique supplémentaire et **sans** génération de sortie.

| Fenêtre, ms/image | Référence 1 | Conservé 1 | Référence 2 | Conservé 2 | Moyenne référence → conservé |
| --- | ---: | ---: | ---: | ---: | ---: |
| Repos, 120 images | 17,426229 | 17,266660 | 16,346728 | 17,124600 | 16,886479 → 17,195630 |
| LOAD continu, 640 images | 31,586968 | 26,091434 | 30,078876 | 26,136256 | **30,832922 → 26,113845** |
| Fin de transfert et rotation, 120 images | 26,448438 | 26,194654 | 25,422864 | 25,061728 | 25,935651 → 25,628191 |

Le gain confirmé concerne LOAD continu : **15,30 % de temps économisé**, capacité sans limite 32,43 → 38,29 images/s et 1,295 → 1,529 MHz, soit 64,76 → 76,47 % des 2 MHz. Cadence vidéo matérielle inchangée : environ 50,1 Hz. Le repos n'a pas de gain établi ; l'effet supplémentaire sur la fin de transfert/rotation est faible et n'est pas présenté comme une optimisation significative. Ces chiffres ne sont pas les compteurs de présentation de RML et ne prouvent pas 19,97 ms/image Debug : ce budget reste ouvert.

Identiques dans les deux versions :

- Repos : 4 792 320 cycles, zéro octet disque ; PC 57064 ; vidéo `FC1E440B9C5533E72A8D68ED28A9B7757C7E13C0733478B14A1C0F8F600624C5`.
- LOAD : 25 559 041 cycles, 63 488 octets ; PC 36452 ; vidéo `5843571514F855E9FBAA4AA734385C76C89AB823CD85E57F0283A7BFBAF1D2C0`.
- Fin de transfert/rotation : 4 792 321 cycles, 6656 octets ; PC 58594 ; vidéo `79D9D7066F3E20EE7DFFC8D2AD9A96DC3A76041579A91EA028BE2F15A5BBA00E`.
- RAM chargée, 9216 octets à &3000 : `51138505BF4641C824AABE4E85CFBD7A30577A21D478E1937949B7941C3CE11A`.

## Vérifications

### Report du budget Debug actif

La fonctionnalité et l'équivalence temporelle du bloc conservé sont vérifiées ci-dessous. Le budget de 19,97 ms/image en LOAD continu reste toutefois non atteint (26,11 ms/image). Les recherches successives sur la projection de piste, les générations de sorties et le cache de prochaine transition physique ont déjà été prolongées puis rejetées faute de gain global confirmé. Aucune nouvelle solution sûre n'est établie pour retirer ce coût restant sans perdre de fronts ni ajouter une surcharge. Selon la directive de report, arrêter ces petits essais sans résultat, conserver le gain de 15,30 % et laisser cette optimisation explicitement incomplète. Ne pas déclarer le budget atteint lors du passage à la machine suivante ; les conditions et résultats précédents restent les références de sa reprise.

Le premier passage donne 191 tests silicium Debug réussis en 568 ms, hors StateTests. Les 183 cas préexistants n'utilisent pas la nouvelle primitive ; ils ne sont pas rejoués après chaque ajustement. Après simplification de la primitive, ses huit tests directement concernés réussissent de nouveau en 282 ms : mêmes états du séparateur et mots horodatés en FM/MFM, même séquenceur, timers, reset et signaux WD1770/1772 que les clocks unitaires. Les vues d'état existantes servent seulement d'assertion de tous les champs, sans fichier ni développement de sauvegarde.

Après retrait du cache physique, **42 raccordements Debug finaux réussissent en 18 s**, aucun échec ni ignoré. Six cas comparent les sources 1, 2 et 4 MHz à la source 16 MHz donnant au plus une clock WD par appel, pour les deux variantes : demi-clocks, octets/DRQ, horodatages NMI, index jusqu'à l'arrêt moteur, puis nouvelle lecture. Les lectures/écritures, faces, CRC, remplacement de support et sources non divisibles restent couverts par le groupe. Les nouveaux tests sont autonomes, avec pistes en mémoire et rappels détachés en finally ; les anciens scénarios externes ne sont pas relancés inutilement.

Capture finale inspectée : `artifacts/TestsChips/Wd1770/BBC Micro Model A/20261107-051446-wd1770-active-clock-validation.bmp`, copiée dans `artifacts/TestsMachines/Acorn/BBC Micro Model A/`. Elle montre le vrai framebuffer du BBC : programme chargé par DFS, Escape à la ligne 10 et PRINT I donnant 9. Les captures datées des prototypes restent des étapes d'évolution, pas la preuve d'une autre fonction ni un HUD fabriqué. Les DLL de comparaison, TRX et diagnostic sont temporaires et retirés après consignation ; les images restent hors Git.
