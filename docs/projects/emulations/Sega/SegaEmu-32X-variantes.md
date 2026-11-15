# Comparaison du SH-2 et du bus dans les variantes Gens

Comparaison statique du 15 novembre 2026. Aucune mesure de vitesse des executables Gens n'a ete realisee dans ce travail. Les differences ci-dessous ne constituent pas une promesse de gain FPS dans SegaEmu.

## Sources effectivement comparees

- Gens/GS : `artifacts/References/Machines/Sega/32X/Gens/{sh2a.asm,sh2_io.asm,cpu_sh2.c,mem_sh2.asm,g_32x.cpp}`.
- Gens Rerecording : `artifacts/References/Machines/Sega/32X/GensVariants/GensRR/{SH2a.asm,SH2_IO.inc,Cpu_SH2.c,Mem_SH2.asm,Gens.cpp}`. L'index des sources contient les blobs et les verifications effectuees par la collecte.
- Gens Plus 0.0.9.61 : `artifacts/References/Machines/Sega/32X/GensVariants/gens+00961s/source/{cpu/SH2a.asm,cpu/SH2_IO.inc,cpu/Cpu_SH2.c,cpu/Mem_SH2.asm,gens.cpp}`.

Les trois fichiers ASM annoncent le coeur SH2 1.60 de 2002. La comparaison retire commentaires, differences de casse, espaces et directives d'alignement pour examiner les instructions, sans traiter une simple difference de mise en forme comme une optimisation.

## Coeur d'execution : identites etablies

| Element | Gens/GS, Rerecording et Plus |
|---|---|
| 277 handlers `SH2I_*` communs, delay slots compris | Les 277 corps sont identiques apres la normalisation decrite. |
| Macros `RET`, `GO_DS`, `RET_DS`, `CHECK_INT` | Identiques dans les trois sources. |
| Politique de cycles selectionnee | `SH2_SPEED REALIST` dans les trois fichiers. |
| ASM Rerecording contre Plus | Le contenu normalise complet de `SH2a.asm` est identique. |
| Chemin rapide commun | PC hote dans ESI, budget dans EDI, lecture directe du mot instruction et saut dans `OP_Table`. |

Cela etablit l'absence de nouveau moteur d'instructions dans les fichiers compares de Rerecording et Plus. Cela ne declare pas identiques leurs bus, leur ordonnanceur, leurs outils ni tous leurs composants.

Les differences de `SH2a.asm` entre Gens/GS et les deux autres concernent notamment le contexte assembleur deploye dans le fichier au lieu d'un include, les branches de convention d'appel GCC des macros `READ_BYTE`, `READ_WORD`, `READ_LONG`, `WRITE_BYTE`, `WRITE_WORD`, `WRITE_LONG`, et l'inclusion finale de `SH2_IO.inc`. Ces differences de portabilite et d'assemblage ne demontrent pas un gain d'emulation.

## Acces memoire et synchronisation

Les trois `Cpu_SH2.c`/`cpu_sh2.c` declarent les memes plages principales de fetch via `SH2_Set_Fetch_Reg` : SDRAM a `06000000` et `26000000`, ROM cartouche a `02000000` et `22000000`, ROM de boot et son alias non cache, et cache-RAM a `C0000000`. Ils appellent `SH2_Map_Cache_Trough` pour les gestionnaires des alias. C'est le mecanisme commun directement exploitable pour retirer les callbacks de fetch dans SegaEmu, avec respect de sa representation memoire.

L'ordonnanceur de Gens/GS (`g_32x.cpp`, `T_gens_do_32X_line`), celui de Rerecording (`Gens.cpp`, `Do_32X_Frame`) et celui de Plus (`gens.cpp`, `Do_32X_Frame`/`Do_32X_Frame_No_VDP`) utilisent tous `p_i = 84`, puis les rapports `p_j` et `p_k` vers les deux SH-2. Le quantum de base n'est donc pas une optimisation nouvelle propre a un seul derive.

## Differences fonctionnelles concretes hors decodeur

### DREQ1 lors du remplissage PWM : ajout present dans Gens/GS

`Gens/mem_sh2.asm`, macro `PWM_DMA` vers la ligne 1715 : apres une ecriture FIFO, le code teste si la FIFO est pleine puis appelle `SH2_DMA1_Request` avec zero pour les deux SH-2 afin de desactiver DREQ1. Les chemins octet et mot des pulses PWM l'utilisent.

Les chemins correspondants de `GensRR/Mem_SH2.asm` et `gens+00961s/source/cpu/Mem_SH2.asm` mettent a jour la FIFO puis retournent sans cette macro. Ils possedent par ailleurs d'autres appels DMA0 : l'absence constatee concerne ce raccordement PWM/DREQ1, pas toute leur gestion DMA.

Action utile pour SegaEmu : conserver la propagation du niveau DREQ lie a la FIFO, y compris sa desactivation quand elle devient pleine. Remplacer sans examen le bus Gens/GS par celui d'un autre derive pourrait retirer cette correction. Aucun gain de vitesse n'est attribue a cet ajout.

### Tables de palette du rendu 32 bits dans Rerecording

Dans `GensRR/Mem_SH2.asm`, les ecritures CRAM ajustent les tables `_32X_VDP_CRam_Ajusted` et `_32X_VDP_CRam_Ajusted32`, avec `_32X_Palette_16B` et `_32X_Palette_32B`. Dans les chemins correspondants de Plus, l'ajustement 32 bits est absent.

Cette difference concerne le raccordement du rendu et les formats de sortie. Elle peut ajouter du travail a une ecriture palette ; elle ne permet pas de conclure que tout l'emulateur est plus rapide ou moins rapide. SegaEmu doit maintenir les sorties qu'il expose, sans supprimer un format pour afficher un meilleur chiffre.

## Ce que cette comparaison permet de reprendre immediatement

1. Le mapping direct des plages de fetch est confirme dans les trois variantes, donc sa reprise ne depend pas du seul choix Gens/GS.
2. Le PC et le budget gardes dans le chemin d'execution, au lieu de callbacks et mises a jour de contexte repetes, constituent le chemin commun a porter.
3. Le quantum de synchronisation de 84 cycles 68000 est confirme dans les trois variantes ; ses bornes et transitions restent a raccorder correctement a SegaEmu.
4. Le raccordement DREQ1/PWM de Gens/GS doit etre preserve lors de cette reprise.

Il n'a pas ete trouve, dans ces fichiers compares, de nouvelle boucle SH-2 de Plus ou Rerecording qui remplacerait celle de Gens/GS par un moteur intrinsequement plus rapide. Cela n'ecarte pas leurs autres ameliorations : elles doivent etre attribuees au fichier et au comportement effectivement changes. Aucun classement FPS de ces variantes n'est etabli ici.
