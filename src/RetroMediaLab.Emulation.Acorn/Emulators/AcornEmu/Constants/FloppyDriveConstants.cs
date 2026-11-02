namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;

/// <summary>Caractéristiques des lecteurs raccordables aux interfaces du BBC Micro.</summary>
internal static class FloppyDriveConstants
{
    internal const decimal DiameterInches = 5.25m;
    internal const int FortyTracks = 40;
    internal const int EightyTracks = 80;
    internal const int SectorSize = 256;
    internal const int SingleDensitySectors = 10;
    internal const int DoubleDensitySectors = 16;
    internal const int DosSectorSize = 512;
    internal const int DosSectors = 9;
    internal const int AdfsLargeSectorSize = 1024;
    internal const int AdfsLargeSectors = 5;
    internal const string DfsName = "BBC DFS";
    internal const string AdfsName = "BBC ADFS";
    internal const string DfsSingle40 = "acorn.dfs.ss";
    internal const string DfsSingle80 = "acorn.dfs.ss80";
    internal const string DfsDouble40 = "acorn.dfs.ds";
    internal const string DfsDouble80 = "acorn.dfs.ds80";
    internal const string Adfs160 = "acorn.adfs.160";
    internal const string Adfs320 = "acorn.adfs.320";
    internal const string Adfs640 = "acorn.adfs.640";
}
