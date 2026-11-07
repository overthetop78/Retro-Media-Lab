# Master 128 — carte assemblée, 7 novembre 2026

## Composants réutilisés et périmètre

G65SC12, HD146818, VIA, ADC, vidéo, série et PSG existants, sans modification de production. Carte Master 128 sans coprocesseur : MOS 3.50 externe, BASIC IV et banques 9 à 15 du même fichier combiné (BASIC en banque 12), SAA5050 externe, RAM principale 32 Kio et mémoires Master. Interfaces WD1770 et Winchester activées, mais aucun média ni réseau monté. Aucun Tube, Turbo ou 80186 exercé par ce passage.

Les tests propres au Master réussissent en Debug, six cas, zéro échec ni ignoré, 155 ms : sélection shadow selon l'origine de l'instruction/ACCCON, notifications IRQ ACCCON, indépendance ANDY/HAZEL/principale et reset chaud, bus CMOS, échéance/IRQ RTC et instructions CMOS avec RAM paginée. Le détail des tests silicium déjà validés reste dans leurs fiches ; ces six cas ne remplacent pas toute la conformité des puces.

## Programme invité et RTC

Le vrai CPU démarre les ROM puis exécute `PRINT"MASTER128 PASS":PRINT 12+34` : le verdict et **46** sont visibles. Il conserve ensuite `A$=TIME$`, avance 150 images puis vérifie `IF A$<>TIME$ THEN PRINT"RTC PASS"`. Le verdict RTC est présent. La saisie elle-même fait avancer le temps : ne pas présenter ce contrôle comme une mesure exacte de trois secondes ni une calibration de l'oscillateur. Le test autonome de câblage RTC vérifie séparément l'échéance et l'IRQ sans lecture périodique de ses registres.

Capture du framebuffer réel inspectée : `artifacts/TestsMachines/Acorn/BBC Master 128/20261107-093511-master128-basic-rtc.bmp`, également sous `artifacts/TestsChips/Hd146818/BBC Master 128/`. Aucun test dans RML ni écoute Windows revendiqués. Le premier build du banc manquait sa fonction locale Capture après une substitution de fins de ligne ; l'invocation/source temporaire a été corrigée avant exécution, sans modification du logiciel.

## Carte complète Debug sans limite hôte

ROM et programmes exécutés sans compilation ni autre test concurrent ; rendu complet et drainage audio inclus. Après démarrage, dix images de chauffe par scénario puis 80 images mesurées. Les captures et leurs six images de stabilisation sont hors de la fenêtre chronométrée. Le redessin est une vraie boucle MODE 1/CLS avec deux couleurs, pas un PRINT en mode télétexte. SOUND reste actif lors de ce redessin.

| Scénario | ms/image | Images émulées/s | MHz émulés | Vitesse réelle | Cycles |
| --- | ---: | ---: | ---: | ---: | ---: |
| Repos après verdicts | 20,609371 | 48,5216 | 1,937760 | 96,89 % | 3 194 881 |
| SOUND | 21,802446 | 45,8664 | 1,831722 | 91,59 % | 3 194 881 |
| MODE 1 / CLS alternés | 23,643870 | 42,2943 | 1,689064 | 84,45 % | 3 194 881 |

Cadence matérielle inchangée, environ 50,08 Hz (2 MHz, 39936 cycles/image). Ces capacités hors limite ne sont pas les compteurs de présentation de RML. Un passage, sans comparaison alternée et sans changement de code : aucun gain ni régression attribuée à une puce sur ces seuls chiffres. La RTC n'est pas désignée comme cause du coût. Le budget 19,97 ms/image reste non atteint sur ces trois scénarios.

Captures également conservées sous `artifacts/TestsChips/Mos65C12/BBC Master 128/` et dans le dossier machine : `20261107-093513-master128-idle.bmp`, `20261107-093517-master128-sound.bmp`, `20261107-093523-master128-redraw.bmp`. Ces sorties seules ne prouvent pas la fidélité audio ni toutes les transitions vidéo.

Entrées/rappels détachés en finally. Aucun fichier firmware, configuration, média utilisateur, session RML ou périphérique Windows modifié. Le diagnostic temporaire et son journal de tests sont supprimés, puis le banc est reconstruit avec son Program.cs antérieur. Le Master 128 reste en cours : le budget de calcul, les médias et la validation finale de cette reprise ne sont pas clôturés par ce contrôle.
