# VDU80 — trajet numérique de génération des caractères

Circuit de la carte 400,019, manuel System 5 §2.3–2.6, figures 10, 14, 15 et 18. Références et extraits inspectés dans [la fiche de câblage](../../project/emulations/Acorn/Acorn%20System/System%205/vdu80.md). Composants : SN74LS374, ROM MM52116 et SN74LS165, déjà vérifiés séparément.

Le modèle numérique reçoit les niveaux 2/12 MHz du bus. Le latch capture le caractère sur la montée 2 MHz ; NL charge d'abord le motif encore présent en sortie de ROM. L'accès de la nouvelle adresse ROM conserve son échéance, sans changer le mot déjà chargé dans le sérialiseur. Les fronts 12 MHz décalent les étages ; les données et masques connus proviennent des notifications des composants, jamais d'une consultation périodique de leurs fournisseurs.

Profil documenté pour cette réalisation : NL = 20 ns typiques du manuel ; tA/tAC/tOFF = plafonds 450/120/100 ns de la ROM. Ce ne sont pas des mesures d'un exemplaire. L'absence de valeur nominale constructeur n'empêche pas le raccordement numérique, comme confirmé dans l'échange utilisateur. Le retard de DE est celui d'un caractère décrit au §2.5.1. Les réseaux analogiques ne font pas l'objet d'une simulation de tensions ; les portes de sortie et l'entrée CU sont résolues numériquement, sans délai logiciel artificiel d'un tick.

L'échantillon retourné sur la montée 12 MHz termine l'intervalle précédent, avant le nouveau décalage. Cela permet au rendu de conserver les six positions H/G/F/E/D/C, dont H reliée à zéro. Le niveau vidéo issu des changements de QH reste distinct de cet échantillonnage. Une valeur inconnue reste inconnue si DE la laisse passer. Le rendu doit conserver dimensions, cadence, entrelacement et parité du CRTC ; ce composant ne possède aucun framebuffer ni ressource hôte.

## Réalisation et contrôles — 9 novembre 2026

Code : `RetroMediaLab.Chips/Controllers/Acorn/Vdu80CharacterGenerator/`, contrat, constantes et service partiel. Les composants internes gardent leurs registres et comptes ; le circuit conserve les niveaux reçus et ses échéances NL/DE/ROM. Le temps est accumulé exactement par fractions de demi-période 12 MHz (24 MHz), sans flottants. La prochaine échéance est conservée et recalculée seulement lorsque ses causes changent ; la ROM est synchronisée à ses réponses ou à une vraie transaction d'adresse. Aucun sondage du CRTC ni framebuffer. CS1/CS2 basses et CS3 haute ; données inconnues transmises avec A/B/H connus à zéro.

Sept contrôles autonomes : motif précédent chargé pendant NL, capture indépendante des changements RAM, six sorties H/G/F/E/D/C, RA sans nouvelle capture du caractère, DE retardé et annulation de transition, XOR curseur/inversion sur le motif, niveaux identiques sans faux front. Source SystemClockGenerator raccordée : 240 périodes source = 20 captures caractère, 120 clocks dot et 10 000 ns. Groupe final avec SN74LS165, SN74LS374, MM52116 et SystemClockGenerator : **56 réussites, zéro échec/ignoré, 80 ms en Debug**. ROM synthétiques, sans fichiers externes. Contrôle graphique de la carte à faire après raccordement au rendu.

Mesure Debug normale sans RML ni tâche concurrente : 24 000 périodes de chauffe puis trois fenêtres de 2 400 000 périodes du générateur complet raccordé au circuit, sans CPU ni CRTC. Par fenêtre : 200 000 captures caractère, 1 200 000 échantillons, 600 000 points blancs et 100 000 000 ns internes vérifiés. Cette mesure inclut le générateur 24 MHz et ses trois compteurs ; ce n'est ni le coût propre du seul circuit ni la vitesse de la machine complète.

| Chemin | ms par fenêtre | Millions de périodes source/s | Pourcentage des 24 MHz requis |
| --- | --- | --- | --- |
| Recherche des échéances à chaque demi-dot | 537,2887 / 524,3529 / 528,3768 | 4,466872 / 4,577070 / 4,542213 | 18,611968 / 19,071126 / 18,925888 |
| Prochaine échéance conservée aux mutations | 529,6830 / 524,7197 / 523,6695 | 4,531012 / 4,573871 / 4,583043 | 18,879216 / 19,057794 / 19,096014 |

Aucun gain significatif établi : ne pas attribuer une amélioration de vitesse à cette factorisation. L'optimisation principale du montage reste ouverte, notamment le coût des appels des trois compteurs à 24 MHz. Aucun front ni état matériel retiré. Banc temporaire supprimé, assembly régénérée et groupe fonctionnel repassé sans lui. Le rendu machine n'utilise pas encore ce circuit : aucune cadence globale ni capture machine nouvelle revendiquée.

### Séparation des coûts et niveaux conservés

Banc temporaire séparé, même chauffe de 24 000 demi-dots et trois fenêtres de 2 400 000 demi-dots (100 ms internes). Source seule avec récepteurs comptant les fronts : 267,6882 / 268,1011 / 272,4140 ms. VDU80 seul, alimenté par les niveaux externes suivant exactement la phase 2/12 MHz, six échantillons par caractère : 208,9820 / 207,4084 / 203,6901 ms. Chaque fenêtre vérifie 1 200 000 clocks dot, 200 000 captures, 100 000 000 ns et 600 000 points blancs.

Le service conserve désormais directement la sortie vidéo et décode la connaissance/niveau des entrées lors des notifications. Les lectures par dot ne repassent plus par son accesseur et la résolution ne multiplie plus les opérateurs nullable ; un zéro connu masque toujours une entrée inconnue dans le AND. Après changement : 182,4786 / 181,9812 / 171,4158 ms pour le circuit seul ; source témoin 273,2829 / 269,9661 / 276,8352 ms. Réduction médiane du temps propre au scénario isolé : environ 12,3 %, sans en déduire un gain global machine.

Une garde supplémentaire à l'appel AdvanceLocalTime n'a pas établi de gain : 183,1168 / 192,9530 / 180,9337 ms, source témoin 291,5203 / 291,3130 / 285,7704 ms. Cette tentative est retirée. L'avancement local et chaque échéance restent effectués. Banc supprimé et assembly régénérée : groupe final de 56 contrôles, 78 ms, zéro échec/ignoré. Le coût des fronts reste important ; l'optimisation globale du montage reste à traiter avant clôture machine, sans modifier les options Debug ni les fréquences.

### Écart révélé au raccordement — délai CU, 9 novembre 2026

Les contrôles de la carte raccordée vérifient les caractères aux deux bords de 480 pixels, les rasters, les écritures RAM et les champs alternés. Deux cas de curseur échouent : le XOR commute au milieu du caractère dans le modèle actuel. Le contrôle isolé antérieur du XOR prouve sa table logique, pas son alignement électrique avec la chaîne de dots.

Le manuel 400,019 §2.6.2 et figure 18 donne un délai CU par R12 = 1 kΩ, R13 = 2,7 kΩ, C6 = 330 pF avant IC16a (valeurs lues sur le schéma ; R12/R13 absents de la liste des résistances de la nomenclature). Ce réseau est absent du profil numérique initial, qui résout CU instantanément. Le profil n'est donc pas validé pour la phase du curseur. Ne pas ajouter une bascule fictive ni adapter l'attente du test à la sortie incorrecte. Le CRTC possède séparément un délai CUDISP maximal de 250 ns au front descendant ; ce maximum n'est pas une valeur nominale mesurée et ne doit pas être attribué au RC de la carte.

Question utilisateur en cours : accepter un modèle RC paramétré avec profil nominal calculé/documenté, distinct d'une mesure de carte. Les composants, niveaux et délais doivent rester dans les composants qui les possèdent, pas dans une correction de framebuffer. Les constantes NL/ROM déjà choisies ne changent pas à cause de cet écart.

Contrôle sur le reset CPU de la carte assemblée : même instance du composant, +9 captures caractère, +54 clocks dot, +4500 ns pendant la séquence CPU, sans remise à zéro de son temps. Neuf contrôles carte/horloges hors curseur réussissent en 204 ms ; les deux échecs curseur restent à corriger et ne sont pas inclus dans ce résultat ciblé.

### Profil RC calculé autorisé

L'utilisateur autorise le profil calculé faute de mesure réelle disponible. Le détail extrait `System5-vdu-CU-detail-93.png` confirme R12 en série depuis CU et R13/C6 en parallèle vers 0 V. Aucun service de réponse RC analytique réutilisable n'a été trouvé dans Chips ; SystemKeyboardRepeat traite un autre montage oscillant avec des échéances nominales fixes.

Pour une source logique résolue, le modèle conserve la tension normalisée de C6 relativement à son niveau haut établi. Gain continu : R13/(R12+R13) = 27/37. Résistance de Thévenin : R12 parallèle R13 = 729,7297 Ω. Constante de temps : τ = (R12 parallèle R13) × C6 = 240,8108 ns. Réponse sur un intervalle d'entrée stable : v(t) = cible + (v0 − cible) exp(−Δt/τ). Un changement d'entrée conserve v0 ; il ne remplace pas la réponse par un retard fixe et ne remet pas C6 à zéro.

Profil de référence calculé : seuil à 50 % du niveau haut établi, état initial déchargé explicitement choisi. Ce seuil relatif est une convention de calcul, pas une valeur typique garantie de l'entrée TTL ni une mesure de carte. Le paramètre reste explicite et modifiable pour une caractérisation réelle. Avec une entrée établie, le franchissement à 50 % vaut τ ln(2) ≈ 166,917 ns ; après une impulsion courte, il est recalculé depuis la charge réellement conservée. Résolution temporelle du modèle : 1 ns, échéance arrondie au prochain ns, pas au prochain caractère/dot. Le modèle décrit ce profil numérique, pas les tensions/transitoires de chaque exemplaire. Les délais de propagation internes du CRTC restent distincts.

### Réalisation du RC et vérification du raccordement — 9 novembre 2026

`Services/Vdu80CursorCircuit.cs` conserve entrée CU, charge normalisée, temps de référence et prochain franchissement. Toutes les valeurs physiques et le seuil relatif sont fournis à son constructeur. Le générateur choisit explicitement les composants de la carte, le profil calculé à 50 % et le condensateur initialement déchargé ; une surcharge permet de préciser un autre seuil relatif. La tension est évaluée analytiquement à une transition d'entrée ou à une observation explicite, pas à chaque dot. Le prochain franchissement est notifié au gestionnaire local VDU80 ; la sortie RC pilote le XOR via notification. Les inversions conservent la charge, une impulsion courte peut ne jamais atteindre le seuil. Aucun reset du RC lors du reset CPU.

Sept contrôles du RC : coefficients de la carte, montée/descente, charge résiduelle, impulsion courte, avancement unitaire/groupé, réarmement depuis callback, paramètres non physiques. Pour une impulsion de 500 ns, la montée se produit à 167 ns et la descente à 635 ns (135 ns après la fin d'entrée), car C6 n'atteint pas entièrement son niveau haut. Le test de carte vérifie ces deux délais depuis les notifications CU réelles, sur les deux rasters du caractère.

L'attente initiale d'une inversion des six dots exactement à la frontière du caractère n'était pas fondée sur ce profil électrique : elle supposait implicitement une autre phase de CU. Elle est remplacée par l'attente calculée depuis la réponse RC, et vérifiée séparément par les horodatages de signaux. Pour les clocks du montage, le dernier dot du premier caractère est après la descente RC ; aucun décalage artificiel du framebuffer, changement de skew R8 ou latch supplémentaire n'a été ajouté pour obtenir le résultat. Les limites du profil calculé et le contrat logique du CRTC restent celles indiquées ci-dessus ; ces tests ne prétendent pas être une mesure électrique de carte.

Groupe autonome RC/VDU80/SN74LS165/MM52116 : **44 réussites, zéro échec/ignoré, 79 ms Debug**. Groupe System/8271/mélangeur/volumes, sauvegardes d'état différées exclues : **185 réussites, zéro échec/ignoré, 6 s**. Les deux cas curseur précédemment échoués passent avec ce profil et leurs contrôles de phase. Compilation du module et des contrôles réussie ; seuls quatre avertissements de tests préexistants sont présents. L'essai natif du logiciel et les nouvelles mesures complètes restent distincts et à effectuer avant clôture machine.

### Preuve invitée du raccordement, passage 09:42–09:46

System 5 démarre NOS80 avec ce trajet, charge le vrai BASIC depuis le SSD commun (4608 octets lus, empreinte C000 exacte 6571FCA3E86E6DC99C2B56481979FBC24615BFF2) et exécute PRINT 12+34 = 46. Captures inspectées sous `artifacts/TestsChips/Vdu80CharacterGenerator/Acorn System 5/` : `20261109-094546-vdu-rc-monitor-debug.bmp` (FD50347AC7BB06B1E18C3FC9D626FDF60B80C3B6F15E424A60C12178A457C630) et `20261109-094546-vdu-rc-disk-basic-debug.bmp` (66D1555978655007CC0AFE6BFB34A3380C4E1ABE98845030B8C66E4B72110949). Copies séparées dans TestsMachines, aucune retouche.

Machine complète Debug : 115,806415–116,049232 ms/champ, 17,206490–17,242568 % sur trois fenêtres de 80 champs après 40 de chauffe. Ce sont des mesures incluant CPU, horloges, vidéo, rendu et audio, pas le coût propre de ce circuit ; tableau et contexte dans la validation machine. Le résultat reste insuffisant et l'optimisation globale ouverte. Banc externe supprimé, disque libéré, assembly régénérée ; 185 contrôles System/disque/audio réussissent après nettoyage. Pas de livraison RML dans ce passage.

### Échéances et profil du circuit, 9 novembre 2026

Debug, trois fenêtres de 12 millions de dots (1 s interne) après chauffe ; caractères, rasters, DE et CU changent pendant les deux scénarios. Circuit seul : 2406,0067 / 2342,5606 / 2333,8191 ms avant ; échéances entières : 2483,2081 / 2468,7815 / 2455,4672 ms. Montage source et VDU : 5181,0896 / 5296,9927 / 5245,2004 ms avant ; 5176,4442 / 5238,2151 / 5228,0181 ms après. Aucun gain établi (environ −0,5 % sur le montage, circuit isolé plus lent) : remplacement retiré. Empreintes de tous les échantillons identiques dans les deux scénarios et avant/après : 8DB0AF32125B62F5, CA79C9C3B51B3EE3, 67F9032C31E97304. Chaque fenêtre conserve 12 millions de dots, 2 millions de captures et exactement 1 milliard de ns. 56 contrôles fonctionnels réussis ; banc de débit exécuté séparément pour les chiffres ci-dessus.

Profil EventPipe du circuit seul, 8 s, dotnet-sampled-thread-time, piles evented intégrées sans addition des parents et descendants ; temps attribué au dernier composant réel (appels runtime inclus) : 7062,58 ms. Parts propres : SetDotClock 15,02 %, SN74LS165.PublishOutputs 11,09 %, ResolveGatedClock 8,75 %, get_SerialOutput 7,69 %, PublishVideo 7,27 %, MM52116.FirstDeadline 6,61 %, AdvanceLocalTime 5,61 %, MM52116.PublishNextEvent 5,29 %, MM52116.AdvanceTo 5,19 %, SetClock 4,35 %, UpdateDeadline 3,16 %. Ce sont des échantillons de temps managé, pas des cycles matériels ni une mesure machine. La publication du registre reconstruit son nullable même lorsque sa sortie ne change pas : prochaine action à inscrire dans son suivi. Les échéances restent celles du profil matériel accepté.

Publication SN74LS165 optimisée et vérifiée : durée du montage synthétique source/VDU réduite de 10,7 % avec motifs nuls, 6,9 % avec motifs changeants ; toutes les empreintes inchangées. 71 contrôles après nettoyage (98 ms). Conditions et tableau dans la fiche Sn74LS165 ; aucune nouvelle cadence machine complète déduite.

### Raccordement des intervalles stables vérifié — 9 novembre 2026

Le trajet complet utilise les périodes stables sans dépasser les échéances locales et fronts observables. System 5 entier Debug : 74,691297 / 76,980154 / 76,304280 ms/champ ; référence 102,139359 / 102,206670 / 101,742130. Durée moyenne réduite de 25,5 % pour le bloc combiné, pas pour ce seul composant. Vitesse 25,94–26,73 % du nominal, source/CPU = 12, empreintes des trois champs identiques. Conditions et résultats complets : `docs/tasks/machines/AcornSystem/system5-validation.md`, passage « Intervalles stables raccordés ».

Après nettoyage des bancs et régénération : 96 contrôles de composants (414 ms), 135 contrôles System/audio (3 s) réussis. La comparaison des champs couvre curseur, inversion, entrelacement et caractères variables, avec mêmes niveaux et instants de notifications. Capture réelle inspectée, conservée dans `artifacts/TestsChips/Vdu80CharacterGenerator/Acorn System 5/20261109-stable-intervals-monitor-debug.bmp`, SHA256 162FD4E97CB8FF4D3D07BE20A79393C087D912C1559AEFE60DB9F951DFB9DA12. Aucun gain propre supplémentaire déduit des seules mesures machine ; optimisation globale encore ouverte.

### Mesure autonome finale des périodes stables — 9 novembre 2026

Qualification notifiée depuis le mot ROM parallèle connu nul ; ce booléen décrit l'admissibilité d'une optimisation, pas une nouvelle broche matérielle. Le chemin groupé vérifie ensuite la sortie série effective et refuse toute période atteignant une échéance ROM/NL/DE/CU. Temps entier et fraction de la cadence 24 MHz restent locaux et exacts. Les autres motifs utilisent directement les fronts unitaires.

Debug, 12 000 périodes de chauffe puis trois fenêtres de 12 millions de dots ; horloge caractère alternée tous les trois dots. Référence et regroupement sur le même circuit, toutes les empreintes et le temps final identiques.

| Scénario | Niveaux unitaires, ms | Qualification/groupes, ms | Effet moyen |
| --- | --- | --- | --- |
| Mot parallèle établi nul | 1056,4372 / 1057,6809 / 1048,6312 | 866,3526 / 865,8350 / 886,4948 | durée réduite de 17,2 % |
| Motifs changeants | 2027,7896 / 2043,8152 / 2040,7051 | 2086,8808 / 2069,1376 / 2118,6484 | durée augmentée de 2,7 % |

Le compromis n'est pas présenté comme un gain universel : la qualification évite les recherches stables sur les motifs variables mais conserve un coût d'intégration. Les tentatives précédant le cache paresseux et cette qualification étaient encore moins favorables sur ces motifs ; la rétention s'appuie sur le gain vérifié du montage réel complet (25,5 % de durée moyenne en moins).

Douze millions de dots et exactement 1 milliard de ns par fenêtre. Empreintes stables 20A4679F1A233685 / 5092FB8406E74D85 / E4D73504C65F6485 ; variables 9A1AAF9926676B95 / 41166DCBE409CD99 / 602DB1804A41DF9D. Banc autonome supprimé et assembly régénérée. Contrôles de périodes/échéances et quatre scénarios machine de huit champs : notifications horodatées, comptes, images et parités identiques au parcours unitaire.
### Calcul temporel réutilisé — 9 novembre 2026

Le préfixe complet est évalué d'abord ; lorsqu'il tient avant la prochaine échéance, son numérateur et son temps final servent aussi à l'avancement. Le calcul préalable d'une période et le recalcul final identiques sont évités. Si une échéance intervient, la recherche bornée existante reste appliquée, avec les mêmes comparaisons strictes et fractions. Aucun délai ni signal modifié. Les comparaisons permanentes des niveaux, notifications horodatées et champs passent : 96 contrôles de composants et 135 System/audio.

Bloc raccordé avec compteur et générateur : durée moyenne 75,991910 → 73,052154 ms/champ, soit 3,9 % en moins ; ce résultat ne mesure pas le coût propre de cette seule réutilisation. Tableau et conditions : [validation System 5](../../tasks/machines/AcornSystem/system5-validation.md), passage « Captures compactes et contrôle des intervalles ». Capture réelle inspectée sous TestsChips/Vdu80CharacterGenerator/Acorn System 5/20261109-packed-clock-monitor-debug.bmp, SHA256 162FD4E97CB8FF4D3D07BE20A79393C087D912C1559AEFE60DB9F951DFB9DA12. Bancs supprimés et assemblies régénérées ; performance machine toujours ouverte.

### NL et délai DE internes dans un intervalle établi — 9 novembre 2026

Le registre entièrement connu à zéro, le mot parallèle connu nul et SER relié à la masse prouvent que le relâchement NL ne change pas QH. Le délai DE ne modifie que l'entrée différée du prochain latch caractère ; sa capture reste externe et borne l'intervalle accordé. Dans ce seul cas établi, le VDU peut donc conserver l'intervalle source groupé en exécutant ses événements internes et dots par SetDotClock ordinaire. Les événements ROM (qui peuvent changer le mot chargé ou sa qualification) et curseur (XOR) restent des frontières strictes. Tous les autres états gardent le parcours conservateur. Dès que l'événement interne est passé, le reste du préfixe sans échéance revient au calcul arithmétique ; pas de boucle supplémentaire sur un long intervalle établi.

Deux contrôles nouveaux démontrent le traitement NL et les refus avant ROM/curseur, puis la reprise, avec notifications vidéo et de qualification horodatées identiques aux fronts explicites. Ces cas échouaient avec l'ancien refus NL et passent après modification. Les 98 contrôles de composants et les 135 System/audio, dont les comparaisons de champs complets, passent. Aucune fréquence, activité interne, transition réelle ni délai modifié.

Gain natif vérifié sur System 5 entier : 73,052154 → 60,067840 ms/champ en moyenne, soit 17,8 % de durée en moins ; vitesse 32,95–33,49 % Debug, images et comptes identiques. Tableau et conditions dans la [validation System 5](../../tasks/machines/AcornSystem/system5-validation.md), passage « Événements internes vidéo sans changement de sortie ». Capture réelle inspectée TestsChips/Vdu80CharacterGenerator/Acorn System 5/20261109-local-events-monitor-debug.bmp, SHA256 162FD4E97CB8FF4D3D07BE20A79393C087D912C1559AEFE60DB9F951DFB9DA12 ; preuve du contrôle dans 20261109-local-events-results.md. Banc natif supprimé, assembly régénérée. Pas de build de livraison ni de clôture de la machine dans ce passage.
