# Champs vidéo natifs — BBC Micro et Master

## Transfert corrigé le 7 novembre 2026

Le balayage MC6845, les accès RAM, VIDPROC et SAA5050 restent inchangés. Le défaut concernait la sortie logicielle : `Video.Rendering.cs` assemblait le champ terminé avec l'ancien champ de l'autre parité, puis copiait toute l'image à chaque transfert.

`Video.Fields.cs` conserve la parité du champ **terminé**, distincte de celle du champ que le CRTC vient de commencer. `Video.Render()` ne décode et ne transfère désormais que les lignes de ce champ. Le cache de lignes inchangées et la propriété indépendante des instantanés sont conservés.

`IAcornMachine.VideoScanMode`, puis `EmulatorCore.LatestVideoFrame`, transmettent le mode progressif ou la parité du champ. Le contrat générique `VideoFrame` décrit les pixels effectivement présents avec `Height` et la hauteur de l'image avec `ImageHeight`. Il ne dépend ni du moniteur Windows ni du redimensionnement de la fenêtre. Les trois codecs existants du SDK transportent le même mode et le même horodatage.

## Présentation

Dans App, `EmulationVideoFieldComposer.Receive()` conserve les références immuables des dernières parités. Il n'appelle aucun circuit et ne recalcule pas l'image pendant l'attente. Le présentateur reçoit chaque champ avant de décider s'il peut présenter une nouvelle image.

`Compose()` assemble les lignes uniquement lorsque le présentateur ou une capture en a besoin. Plusieurs champs reçus avant une présentation ne font donc pas perdre la dernière parité opposée. Le résultat est un instantané indépendant ; ses dimensions restent natives. Les surfaces et leurs shaders reçoivent ensuite cette composition de présentation, comme auparavant. La chronologie, le changement de machine, le format, les dimensions et le passage progressif/entrelacé réinitialisent l'historique applicable.

## Vérifications

Le passage Debug du 7 novembre 2026 comprend **89 tests réussis en 3 s** : rendu scalaire équivalent du champ, immutabilité des tampons, alternance des parités et changements de mode sur l'assemblage vidéo, tests vidéo existants, trois transports, composition des formats RGB565/RGB1555/XRGB8888 avec pitch, et présentation WPF après réception de plusieurs champs avant son rafraîchissement. Les changements de machine et de chronologie ne réutilisent pas l'ancien champ opposé.

Cette durée est celle du groupe de tests, pas une mesure de performance du BBC complet. La livraison et les captures de logiciels invités restent dans la validation intégrée de la machine ; aucun gain global n'est annoncé sur la seule réduction de moitié des octets transférés par le module en entrelacé.
