# Diagnostic CD32X : Corpse Killer Europe

## Perimetre et observations disponibles

Analyse des sources deja collectees et des journaux `artifacts/Emulation/segaemu-32x/corpse-diagnostics.log` (1 800 trames) et `corpse-run-Release.log` (2 400 trames). Aucun nouveau benchmark ni build n'a ete lance dans ce diagnostic parallele. Ces anciens journaux ne constituent pas une validation du code courant apres les derniers changements SH-2.

Le disque utilise est `G:\Retro\Cartouches\Sega\Mega CD\Corpse Killer (Europe) (Mega-CD 32X)\Corpse Killer (Europe) (Mega-CD 32X).cue`, avec le BIOS Mega-CD Europe v1.00 deja fourni par l'utilisateur.

## Bootstrap confronte a PicoDrive

Les flux `MasterBootstrap` et `SlaveBootstrap` de `Mega32X.Boot.cs` correspondent mot pour mot a `msh2_code` et `ssh2_code` de `PicoDrive/pico/32x/memory.c` (respectivement 56 et 32 mots). Le chemin CD attend `_CD_` dans COMM, copie l'IPL depuis la zone framebuffer `24000018`, charge les VBR et entrees master/slave, puis emet `M_OK` et `S_OK`.

`StartProcessors` initialise GBR a `20004000` pour les CPU avec BIOS de remplacement. Le chemin sans cartouche ne charge pas les vecteurs de cartouche ; le VBR CD est ensuite charge par le flux bootstrap. `Handles68k` ne superpose plus le BIOS 32X de cartouche aux vecteurs Mega-CD sans cartouche. Ces corrections existantes sont conservees.

Les logs apres 1 560 trames montrent des PC master variant entre `06000514`, `06000634`, `06000654` et `0600053C`, ainsi que des commandes COMM `POLL`, `DCMP`, `SHOW` et `68SH`. Le processeur execute donc le protocole du contenu CD ; ce n'est pas la preuve d'un master fige dans le bootstrap. Le slave a `060002D6`/`060002D8`, SR=`F0`, peut executer une boucle d'attente volontaire. Cette position seule ne prouve ni un probleme FRT ni une interruption manquante.

## Defaut prouve et correction

`Mega32X.Registers.cs`, ecriture 68000 au registre offset `02` : le code precedent ne faisait que mettre les bits CMD a un. Une ecriture de zero ne pouvait pas retirer la demande tant que le SH-2 ne l'avait pas acquittee.

Dans PicoDrive, `memory.c`, `p32x_reg_write8` case `03`, conserve les deux bits ecrits puis appelle `p32x_update_cmd_irq`. Cette fonction de `32x.c` affirme **ou efface** la demande selon les bits du controle. L'acquittement SH-2 a `401A` efface le bit de controle correspondant.

Correction appliquee : l'ecriture 68000 remplace maintenant le seul bit CMD de `_pending[cpu]` selon le controle fusionne avec le masque octet, en preservant les autres interruptions. Une ecriture de zero retire donc la demande. Aucun champ d'etat supplementaire n'est introduit. Cela corrige une difference concrete par rapport a la source ; aucun lien causal avec le fonctionnement final de Corpse Killer n'est encore demontre.

Validation ciblee a integrer a la campagne du parent : ecrire `A15102=3` et lire `3`, ecrire `0` et lire `0`, puis ecrire `2` et verifier que seul le slave est demande ; verifier l'acquittement SH-2 a `401A` et l'effet des masques.

## Criteres de validation du prochain passage CD32X

1. Confirmer machine Mega-CD, extension 32X active, absence de cartouche et BIOS Mega-CD charge ; conserver `Enabled`, `Released` et `_cartridgePresent` dans le releve.
2. Relever les PC des deux SH-2, leurs cycles et les huit mots COMM a plusieurs instants, et non une seule position finale.
3. Capturer la sortie composee et le rendu 32X seul. `Vdp32X.RenderLine` peut produire les lignes sans modifier son etat ; rendre les 224 ou 240 lignes et comparer leurs empreintes et leur contenu aux points intermediaires.
4. Un `Mode` VDP32X non nul ou un changement de banque ne suffit pas : il faut constater des pixels 32X non uniformes et l'evolution de l'image attendue du jeu. Les commandes `SHOW`/`DCMP` sont un signe d'activite, pas une preuve visuelle de jeu fonctionnel.
5. Tester Start apres l'initialisation puis observer la progression dans le contenu, avec audio present. Une intro Mega-CD seule ne valide pas le rendu 32X.
6. Refaire la reprise d'etat sur ce passage. Les anciens resultats differaient entre 1 800 trames (etat/video exacts) et 2 400 trames (etat non exact, video exacte) ; ce point reste ouvert, ainsi que le premier bloc audio restaure.

Aucun correctif FRT, bootstrap ou arret de boucle n'est ajoute sans autre faute prouvee. Le jeu et la composition CD32X ne sont pas declares valides par cette analyse statique.
