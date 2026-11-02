namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Constants;

internal static class AtomDriveConstants
{
    internal const string FormatId = "acorn.dfs.atom";
    internal const string FormatName = "Atom DOS";
    internal const string ExtensionKey = "Dsk";
    internal const decimal DiameterInches = 5.25m;
    internal const int Tracks = 40;
    internal const int Sides = 1;
    internal const int SectorsPerTrack = 10;
    internal const int SectorSize = 256;
    internal const int Capacity = Tracks * Sides * SectorsPerTrack * SectorSize;
}
