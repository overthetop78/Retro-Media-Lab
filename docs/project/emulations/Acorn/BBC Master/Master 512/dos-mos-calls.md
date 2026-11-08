# Master 512 — appels MOS depuis DOS Plus

## Interface documentée

Le guide technique, chapitre 6, décrit les interruptions 40h–4Ch donnant accès aux services MOS. Pour OSBYTE, la section détaillée indique AL = numéro, BL = X du 6502, BH = Y si nécessaire. La phrase d'introduction BH/BL est contradictoire ; employer la section détaillée et vérifier les données effectivement transférées.

XIOS fournit aussi les sous-fonctions 81h (acquisition du sémaphore Tube), 82h (libération), 89h (OSBYTE) et 8Ah (OSWORD). Leur liste se trouve dans l'annexe A. L'annexe C définit l'appel direct XIOS : INT E0h, CL=32h, DS:DX pointant sur un descripteur de **cinq octets** : numéro de sous-fonction à l'offset 0, paramètre CX 16 bits à 1, paramètre DX 16 bits à 3. Le retour est dans AX et BX. Pour la vérification des fonctions 81h/82h, les deux mots paramètres sont mis à zéro ; les résultats restent à observer avant de qualifier cet encadrement de solution.

Références de l'auteur, consultées le 8 novembre :

- [Chapitre 6 — appels MOS](https://acorn.huininga.nl/pub/mirrors/8bs.com/othrdnld/manuals/512/512tech/m512tech06.htm).
- [Annexe A — sous-fonctions XIOS](https://acorn.huininga.nl/pub/mirrors/8bs.com/othrdnld/manuals/512/512tech/m512techa.htm).
- [Annexe C — appel BDOS 32h](https://acorn.huininga.nl/pub/mirrors/8bs.com/othrdnld/manuals/512/512tech/m512techc.htm).

Copies HTML dans `artifacts/.tmp-archives/machines/Acorn/BBC Master 512/`, sous ces trois mêmes noms de fichiers. Ne pas refaire cette recherche sans information manquante ou contradictoire.

## Comparaison avec STAR sous DOS Plus 2.1

Même montage courant MOS 3.20 / client 80186 1.01, correction DHLT présente, disquette 2.1 connue montée en lecture seule. La commande fournie **STAR FX16,4** revient à A> ; **ECHO MOSRETURN** affiche ensuite son texte et revient aussi à A>. Après STAR : 187 148 octets lus, disque inactif ; le compteur ne change pas après ECHO.

Le vecteur 4Bh demeure **F000:1E10** avant STAR, après STAR et après ECHO. La réussite de STAR ne vient donc pas d'un remplacement de ce vecteur pendant cet essai. Le fichier STAR.CMD fourni fait 12 800 octets ; son code contient des appels BDOS INT E0h, notamment dès l'offset de code 0011h. Cette observation ne suffit pas à déterminer tous les détails de sa bibliothèque.

Captures natives inspectées : `artifacts/TestsMachines/Acorn/BBC Master 512/20261108-mos-star-debug.bmp` et `20261108-mos-return-debug.bmp` ; la seconde montre STAR version 1.00, ECHO MOSRETURN, le texte produit et A>. Ne pas qualifier le Tube de défectueux sur la seule attente du programme COM qui appelait directement la ROM.

## Vérification suivante

Le programme COM du banc acquiert maintenant le sémaphore XIOS avant ses appels MOS, puis le libère avant ses sorties DOS. La comparaison est en cours : aucun changement des puces ou du câblage pour cette piste.

## Résolution vérifiée — sémaphore XIOS

Les trois scénarios réussissent sous DOS Plus 2.1 en Debug avec acquisition XIOS 81h avant les appels MOS, puis libération 82h avant l'affichage DOS. Aucune modification de production depuis la correction matérielle DHLT, déjà présente dans le passage 2.1 qui restait en attente. Le seul changement de cette comparaison est dans le programme de contrôle et sa coordination avec DOS.

| Scénario | Voies 1–4 | Retour brut OSBYTE 128,0 | Tirs bas |
| --- | --- | --- | --- |
| Position 1 et tir 1 | 65520 / 0 / 0 / 65520 | 769 (0301h) | 1 |
| Position inverse et tir 2 | 0 / 65520 / 65520 / 0 | 770 (0302h) | 2 |
| Repos | 32752 / 32752 / 32752 / 32752 | 768 (0300h) | 0 |

Les valeurs ont été observées en RAM pendant leur durée de validité, puis comparées aux captures natives réellement affichées. Les trois captures `20261108-joystick-<0|1|2>-dos21-semaphore-debug.bmp` montrent les nombres et le retour à A> ; copies sous TestsMachines/Acorn/BBC Master 512 et TestsChips/Upd7002/BBC Master 512, ainsi que TestsChips/Intel80186/BBC Master 512. Ce sont des entrées normalisées injectées par le contrat existant, pas une observation de manette physique dans RML.

L'attente précédente provenait du programme de contrôle qui utilisait les services ROM sans coordonner l'accès au Tube avec XIOS. Ne pas la transformer en contournement dans une puce, en réponse MOS fabriquée par le module ou en interception d'interruption dans le CPU. La correction est uniquement dans le logiciel invité du banc. Le diagnostic et sa copie de disquette sont supprimés après consignation ; le fichier source de référence n'est pas modifié.

Séquence réutilisable du programme de contrôle : DS pointe sur son segment de données ; un descripteur contient 81h puis deux mots nuls ; DX pointe sur ce descripteur et CX vaut 0032h avant INT E0h. Les appels MOS suivent la convention AL/BX du chapitre 6. Avant les sorties DOS, un second appel INT E0h utilise le même format avec 82h. Ne pas garder le Tube acquis pendant les appels de sortie DOS.

## Souris GEM — protocole à vérifier

Le chapitre 5 du guide technique décrit dans 6502.SYS l'initialisation souris via OSWORD FC, terminateur FE, la redirection IRQ1V et les interruptions du port utilisateur. Le gestionnaire conserve les coordonnées Y/X sur quatre octets. Lors d'un événement VSYNC, la commande asynchrone 2 demande leur transfert avec les boutons par le canal Tube 1. Il ne s'agit pas d'un pilote souris PC à ajouter au 80186. Source : [chapitre 5](https://acorn.huininga.nl/pub/mirrors/8bs.com/othrdnld/manuals/512/512tech/m512tech05.htm), copie archivée m512tech05.htm.

Le disque GEM2.800 fourni contient GEM.BAT à la racine, qui exécute ADDMEM 333, CD \GEMSYS puis GEMVDI. Le premier banc a choisi à tort ibm.800 (512 octets/secteur) d'après la capacité et le répertoire FAT : DOS répond Bad command or file name, sans lancement GEM. Ce résultat ne prouve aucun défaut de puce ni de souris.

Le [chapitre 9 du guide](https://acorn.huininga.nl/pub/mirrors/ftp.nvg.org/pub/bbc/doc/Master%20512%20Technical%20Guide/m512tech09.htm) précise au contraire que le format DOS Plus 800 Kio utilise 1024 octets/secteur et un secteur par cluster. Le banc doit présenter ces secteurs avec le format existant acorn.adfs.800, sans modifier les octets du disque. Ne pas confondre géométrie physique et système de fichiers. La vérification du lancement et des entrées reste à effectuer.
### Correction de la piste intermédiaire ADFS 800

La même capacité et les mêmes tailles de secteurs ne suffisent pas : ADFS D est à base zéro, DOS Plus 800 à base un. Le choix ADFS proposé ci-dessus a été testé puis rejeté. [Géométrie exacte, demandes WD observées, correction du format dans MediaEngine et résultats GEM](dos-plus-800.md). Aucun décalage n'est ajouté dans une puce pour compenser le mauvais profil média.
