# Horloge du magnétophone externe BBC

- [x] Préparer la comparaison sans refaire le SERPROC déjà examiné.
  - [x] Modifier `artifacts/.tmp-65c12/native/Program.cs` : conserver le dernier PCM drainé et son empreinte avec celles de la vidéo et des cycles pour les scénarios Master complets ; conserver une copie avant modification sous `artifacts/.tmp-65c12/tape-before/`.
  - [x] Modifier ce document avec les résultats de référence et la limite du précédent regroupement SERPROC retiré.
- [x] Supprimer les consultations du magnétophone inactif.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/Common/Services/Cassette.cs` : notifier les changements de besoin de traitement du support depuis ses mutations ; conserver la présentation de la dernière entrée au cycle prévu avant suspension.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/Common/Services/BbcMicroMachine.cs` : posséder explicitement le câblage Serial, mémoriser la notification du magnétophone et appeler directement Serial quand le support est inactif, sans double horloge.
- [x] Vérifier le bloc intégré.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuCassetteTests.cs` : comparer le chemin permanent et le chemin notifié aux mêmes cycles, avec insertion, moteur, pause, RS423, recherche, enregistrement, reset et éjection ; conserver les horloges et sorties série identiques.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/Common/Services/Serial.cs` : essayer une frontière de méthode pour le Tick série, comparer le Master complet et retirer cet attribut si le gain global Release ne se confirme pas.
  - [x] Créer temporairement `artifacts/.tmp-65c12/native/ThreadCounter.cs` et modifier `Program.cs` : relever les cycles du thread hôte avant/après chaque scénario complet via QueryThreadCycleTime, sans appels ajoutés dans la boucle ; copier le même exécutable de diagnostic dans la référence Release pour distinguer le coût CPU des interruptions de mesure hôte.
  - [x] Modifier `docs/chips/IO/BbcSerialUla.md` et `docs/chips/CPU/Mos65C12.md` : consigner le raccordement du magnétophone et les performances mesurées sans déclarer une modification du SERPROC ni une clôture du CPU.
  - [x] Modifier ce document avec les régressions cassette/série Debug/Release et les mesures Master complètes alternées ; retirer le changement si son bénéfice global n'est pas démontré.
- [x] Nettoyer le banc de ce bloc.
  - [x] Supprimer `artifacts/.tmp-65c12/tape-before/`, `artifacts/.tmp-65c12/native/ThreadCounter.cs` et les dossiers bin/obj temporaires du banc, retirer les ajouts PCM/compteur hôte de `artifacts/.tmp-65c12/native/Program.cs` après consignation ; conserver les sources du diagnostic CPU encore en cours.

## Référence avant le changement

Le précédent regroupement SERPROC AdvanceTo a été retiré après mesures : ses médianes Debug étaient plus lentes. Ce bloc conserve SERPROC/MC6850 et leurs horloges, et concerne uniquement le traitement du support externe au repos.

Master complet MOS 3.50, sans Turbo, ROM externes lues sans modification ; prompt BASIC, SOUND puis redessin ; 20 images de chauffe et 80 mesurées. Debug : 19,993 / 19,179 / 20,109 ms/image. Release : 3,300 / 3,671 / 3,673. Cycles 3194881 / 3194881 / 3194879 ; PC E7A8 / EA32 / A469, identiques Debug/Release.

Vidéo (repos / son / redessin) : 18801ECDA8CA9D5138A337E66F2B0AD0C64725E2CF0CE724BD0D279D57790979 / 0DC55D1D27BBA23B266B930E5A4C6D01D8518AB26B55FE1B69C665D3C532E2BA / 6564B39346B52021BBC29A9ABD92678FBD87667E707D5F80A42D79C343DE7033.

Dernier bloc PCM drainé de chaque scénario : 52E28210C0BE3BE4E312D3EC209B5C49FC69ABDD4C4AED923F1CE29AD25B0350 / 9E7715129560CFA46E143B415448E4B58F29C5CE4FD6A2E7C4FDD9015F55708A / F5091E0691808FD1A2A73F3DDF9A5EC10FDF334AA1646E4CCA0938CC6847D0E1. Il ne s'agit pas d'une empreinte du flux audio entier.

## Validation et comparaisons après changement

36 tests cassette/série passent en Debug (29 s) et en Release (5 s). Aucun cas du filtre exclu. Les deux nouveaux tests sont autonomes, sans fichier ni sortie Windows. Les sources SERPROC/MC6850 sont inchangées ; Serial.cs est identique à sa version initiale après retrait de l'essai NoInlining.

| Debug ms/image | Référence 1 | Candidat 1 | Référence 2 | Candidat 2 | Moyenne avant → après |
| --- | ---: | ---: | ---: | ---: | ---: |
| Repos | 19,473 | 18,418 | 18,715 | 18,340 | 19,094 → 18,379 |
| SOUND | 19,659 | 18,849 | 19,336 | 18,541 | 19,4975 → 18,695 |
| Redessin | 20,007 | 19,222 | 19,455 | 19,097 | 19,731 → 19,1595 |

Les gains moyens Debug sont de 3,74 %, 4,12 % et 2,90 %. Premier passage candidat avant la comparaison alternée : 18,590 / 19,461 / 19,353 ms ; il n'est pas utilisé dans ces moyennes.

| Release ms/image | Références successives | Candidats successifs |
| --- | --- | --- |
| Repos | 3,345 / 3,819 / 3,400 / 3,423 / 3,405 / 4,073 | 3,342 / 3,165 / 3,401 / 3,377 / 3,337 / 3,271 |
| SOUND | 3,423 / 3,430 / 3,632 / 3,426 / 3,439 / 3,580 | 3,628 / 4,081 / 3,383 / 3,604 / 3,220 / 3,413 |
| Redessin | 3,612 / 3,492 / 3,501 / 3,542 / 3,511 / 3,754 | 3,622 / 3,438 / 3,776 / 3,629 / 3,579 / 3,436 |

L'ordre des quatre premiers passages est référence/candidat/référence/candidat ; la comparaison croisée suivante est candidat/référence/référence/candidat. Les deux derniers couples relèvent aussi les cycles du thread hôte, hors boucle, avec QueryThreadCycleTime ; ils sont présentés ci-dessous sans les convertir en temps matériel.

| Cycles hôte par image | Référence 1 | Candidat 1 | Référence 2 | Candidat 2 |
| --- | ---: | ---: | ---: | ---: |
| Repos | 10893963 | 10652506 | 12143490 | 10346668 |
| SOUND | 11010641 | 10086162 | 11500951 | 10774618 |
| Redessin | 11264419 | 11322286 | 11880612 | 10884478 |

Ces cycles sont ceux du thread Windows, pas ceux de la machine ni un décompte d'instructions. Ils ne démontrent pas une amélioration systématique dans tous les passages ; la première comparaison redessin est légèrement plus coûteuse. Les durées Release restent dispersées et certaines médianes du candidat sont plus élevées : aucun gain global constant Release n'est annoncé.

L'essai NoInlining de Serial.Tick donne 3,749 / 3,895 / 3,634 ms en Release et ne montre pas d'amélioration ; l'attribut a été retiré avant la régression finale.

Tous les passages conservent les cycles, PC, vidéo et dernier PCM indiqués dans la référence. DLL Chips identique entre référence et candidat : Debug EBA77713377B1FCFDA9A54973376011E6C255EEC4D9F05D8568D45F05640C033 ; Release BAEC209E93136F058D316B326171AF1E8A6A39E16334302D823D88884CECBAE3.

Décision : conserver la notification d'activité du support et le parcours série direct au repos. Le gain global Debug est établi sur les trois scénarios complets, sans divergence vérifiée ; le gain Release ne l'est pas. Aucun comportement de puce ni option d'optimisation Debug n'est modifié. Cela ne clôt ni le CPU ni la machine entière et ne constitue pas une nouvelle mesure de la fenêtre RML.
Nettoyage terminé : copie de référence Debug/Release, compteur hôte, assemblies et obj du banc supprimés ; ajouts PCM/compteur retirés du programme. Aucun fichier ou réglage utilisateur modifié. Les sources du diagnostic CPU restent selon leur propre suivi.

## Sauvegarde indépendante

- [x] Isoler le bloc magnétophone des changements CPU encore en cours.
  - [x] Créer puis supprimer `artifacts/.tmp-65c12/tape-stage.patch` : indexer seulement les trois changements de raccordement cassette dans BbcMicroMachine, Cassette.cs, ses tests, ce suivi et le complément SERPROC ; exclure CPU et fichiers temporaires.
  - [x] Modifier ce document avec la clôture vérifiée puis committer le seul bloc magnétophone.