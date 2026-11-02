namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Wd1770.Enums;

/// <summary>Étapes internes du contrôleur Wd1770.</summary>
internal enum Wd1770Phase { Idle, Positioning, ReadSector, WriteSector, ReadAddress, ReadTrack, WriteTrack }
