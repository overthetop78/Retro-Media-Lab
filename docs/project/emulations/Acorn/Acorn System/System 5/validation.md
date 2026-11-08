# System 5 — DRAM et contrôle natif du 8 novembre 2026

## Montage et défaut établi

6502A E=2 MHz, VIA et VDU80 existants, deux lecteurs 8271. Firmware Acorn System 5 NOS80.bin, SHA1 E588298239B5360B5D1E15D5CD9F7FE2B1693E5D ; caractères Acorn System VDU80.bin, SHA1 04C326A745BA6A78185EBC2ADEECB53A166A6AB3.

Le profil plaçait 48 Kio de DRAM à 0000–BFFF : le catalogue fonctionnait, mais LOAD BASIC à C000 ne pouvait pas conserver les octets. Le manuel System 5, section carte 400,010, §3.1, donne la carte 32 Kio à 2000–7FFF et C000–DFFF ; l'option 16 Kio, §2.2.1 et figure 5(c), occupe 8000–BFFF. Les 48 Kio de cartes sont maintenant à 2000–DFFF ; la RAM CPU 1 Kio reste à 0000–03FF, total 49 Kio, et les trous I/O ne sont pas transformés en RAM. ROM de ce montage E000–FFFF inchangée. Aucun changement de CPU/FDC pour compenser l'erreur de décodage.

Source : [Acorn System 5 Handbook](https://chrisacorns.computinghistory.org.uk/docs/Acorn/Manuals/Acorn_System5Handbook.pdf). PDF et texte archivés sous artifacts/.tmp-archives/machines/Acorn/Acorn System 5/. Les commandes CAT0/CAT1 sont définies dans DOS §2.6 ; le chargement et lancement BASIC par son nom dans §2.9.2.

## Résultats réels

Image F:/Retro/Acorn System/System BASIC.ssd, modèle simple face 80 pistes. Catalogue, LOAD BASIC (empreinte exacte 6571FCA3E86E6DC99C2B56481979FBC24615BFF2), exécution et PRINT 12+34=46 passent. SAVE PROBE écrit 768 octets ; effacement RAM puis LOAD restitue les huit octets attendus. Après déplacement de l'image vers le second lecteur, CAT1 relit au moins 512 octets et montre PROBE. Ces tests utilisent les vrais signaux FDC et le fournisseur disque du SDK ; aucune lecture invitée remplacée par une injection dans la RAM.

Carte complète Debug, 160 champs : 13,391210 ms/champ, 74,676 champs/s sans limite, 2,982255 MHz effectifs (149,113 %). Ce firmware programme un affichage progressif natif 480×250, 50,080128 Hz ; l'application reçoit ce mode, sans entrelacement imposé. Captures vérifiées visuellement : BASIC calcule 46 et catalogue du lecteur 1 avec PROBE.

Preuves natives : artifacts/TestsMachines/Acorn/Acorn System 5/20261108-system5-*.bmp et 20261108-system5-native-debug.log. Essai RML groupé après livraison Debug ; aucun essai utilisateur présenté comme déjà effectué.
Régression finale commune : 46 tests System passent en Debug (1 seconde), dont le nouveau contrôle des liens DRAM et des zones non équipées. Rapport artifacts/TestsMachines/Acorn/AcornSystem/systems-final-debug.trx.
Livraison groupée du 8 novembre : scripts/local-building.cmd --building=debug --modules=A terminé avec code 0. Exécutable build/Debug/Retro Media Lab/retromedialab.exe, Chips dans lib et 24 manifestes/DLL de modules vérifiés. Acorn SHA256 1B5B4CD5E25AA48EDEC7A63C2B641409223319A1FDC7F2E548320B94172E11E2, identique à la sortie win-x64. Essais utilisateur dans RML à effectuer.
