# Master 128 — reprise du 7 novembre 2026

Réutiliser le CPU G65SC12, la RTC HD146818 et les puces communes déjà vérifiées ; ne pas les réécrire sans défaut. Le Tube n'est pas instancié dans le Master 128 sans coprocesseur ; sa clôture restera propre aux variantes Turbo/512. Les budgets actifs WD/HDD communs reportés ne sont pas réputés atteints.

- [x] Vérifier l'assemblage Master 128 après la reprise BBC.
  - [x] Créer temporairement `artifacts/.tmp-tube/master128-tests.log` avec les résultats Debug des six contrôles existants MasterShadowDependsOnInstructionOriginAndAcccon, AccessControlPublishesOnlyItsActualInterruptTransitions, AndyHazelAndMainMemoryAreIndependentAndResetPreservesWarmRam, CmosDataBusIsOnlyDrivenWhenSelectedAndPersistsItsConfiguration, MasterClockPublishesDeadlineAndIrqWithoutSoftwarePollingItsRegisters et MasterCpuExecutesCmosInstructionsAndKeepsSidewaysRamWritable ; ne modifier une source qu'après défaut établi et action détaillée ajoutée.
  - [x] Créer temporairement `artifacts/.tmp-tube/Master128Validation.cs` : démarrer MOS 3.50 externe, vérifier BASIC IV et la progression TIME$ sous le vrai CPU, conserver les trames, puis mesurer la carte entière Debug au repos, sous SOUND et en redessin MODE 1 avec drainage PCM. Détacher les entrées/rappels en finally ; ne toucher ni configuration ni sortie Windows et n'instancier aucun coprocesseur.
  - [x] Supprimer `artifacts/.tmp-tube/Master128Validation.cs` et `artifacts/.tmp-tube/master128-tests.log` après consignation ; reconstruire le banc avec son Program.cs préexistant et conserver les captures uniquement sous TestsMachines/Acorn/BBC Master 128 et TestsChips.

Résultats du bloc : six tests Debug réussis en 155 ms ; BASIC donne MASTER128 PASS, 46 et RTC PASS. Mesures globales : repos 20,609371, SOUND 21,802446, redessin MODE 1 23,643870 ms/image. Le budget n'est pas tenu ; aucune clôture machine ni cause unique affirmée. [Conditions et captures](../../../project/emulations/Acorn/BBC%20Master/Master%20128/board-validation.md). Sources du diagnostic et journal supprimés ; banc par défaut reconstruit, zéro erreur, avertissement NU1900 du flux de vulnérabilités indisponible. Aucun fichier de production modifié.

- [x] Localiser le coût dominant du Master 128 complet avant correction.
  - [x] Créer temporairement `artifacts/.tmp-tube/Master128Profile.cs` depuis le banc Modes existant : limiter le passage au Master 128, MOS 3.50, MODE 1 et 400 images, sans coprocesseur ni média, conserver rendu/drainage, cycles/empreintes et finally ; construire Debug sans optimisation Release.
  - [x] Créer temporairement `artifacts/.tmp-tube/master128-current.nettrace`, sa conversion `master128-current.speedscope.json` et `master128-profile.log` avec dotnet-trace sampled-thread-time ; calculer les coûts inclusifs et propres depuis les intervalles réels des événements Speedscope dans `artifacts/.tmp-tube/master128-summary.json`, sans additionner les parents et enfants. Ne pas utiliser les temps instrumentés comme mesure de cadence libre.
  - [x] Ajouter les fichiers précis de correction au suivi seulement après attribution établie ; conserver la précision matérielle et mesurer la carte complète avant/après si une modification est retenue.
  - [x] Supprimer les seules nouvelles sources, traces, journaux et synthèses temporaires du passage après consignation ; reconstruire le banc par défaut. Conserver les archives de référence préexistantes.

## Décodage Master hors fenêtre I/O

Le profil attribue 8,42 % inclusifs à ReadBus, dont environ 2,69 % à TryReadMasterBus. Le code tente les comparaisons de registres Master même pour les lectures de ROM hors FC00–FEFF. Les sélections privées sont déjà traitées avant cette recherche. Ne pas toucher aux puces ni aux cycles du bus.

- [x] Éviter la recherche des registres périphériques sur les lectures ROM ordinaires.
  - [x] Créer temporairement `artifacts/.tmp-tube/master-bus-baseline.dll` et `master-bus-baseline.cs` depuis l'assembly Acorn Debug et BbcMicroMachine.cs avant modification, ainsi que `Master128Compare.cs` depuis le banc existant, limité au Master 128 et repos/SOUND/redessin MODE 1, avec cycles/empreintes et finally.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/Common/Services/BbcMicroMachine.cs` : après la RAM principale et les sélections privées Master, retourner directement la ROM paginée ou MOS hors fenêtre I/O ; conserver TST et le décodage périphérique dans FC00–FEFF et toutes les phases d'accès.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuMasterTests.cs` avec les frontières ROM/ANDY/HAZEL/I/O et les masques ACCCON/ROMSEL sur carte synthétique ; vérifier le groupe après le bloc.
  - [x] Créer temporairement `artifacts/.tmp-tube/master-bus-comparison.log` et `artifacts/.tmp-tube/master-bus-pass-<0..3>.log` / `.err` avec deux paires Debug référence/candidat, carte complète et même diagnostic, rendus/drainage inclus, cycles/empreintes comparés ; retenir seulement un gain global réel, sinon restaurer exactement la source de référence et reconstruire l'assembly.
  - [x] Supprimer les nouvelles copies/source/journaux temporaires après consignation ; restaurer le banc par défaut. Si le code est retenu, construire Debug All et vérifier sa livraison sans interrompre de session utilisateur.


Bilan : profil courant analysé avec attribution des pseudo-feuilles ; chaîne vidéo à 51,40 % inclusifs, RTC non dominante. Essai de décodage ROM réalisé puis rejeté après deux paires sans gain global ; source de production restaurée exactement. Les 30 tests Master passent sur le candidat, puis les huit nouveaux contrôles passent sur la référence restaurée. Copies, traces et diagnostics nouveaux supprimés ; banc par défaut reconstruit, zéro erreur. Aucun build All du candidat retiré ni clôture du budget Master. [Profil, mesures et décision](../../../project/emulations/Acorn/BBC%20Master/Master%20128/profile-20261107.md).
