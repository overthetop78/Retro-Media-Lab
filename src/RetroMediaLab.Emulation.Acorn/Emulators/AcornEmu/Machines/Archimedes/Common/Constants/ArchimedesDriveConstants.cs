namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Constants;

internal static class ArchimedesDriveConstants
{
    internal const string Model = "archimedes-dd80";
    internal const string Adfs800 = "acorn.adfs.800";
    internal const string Adfs640 = "acorn.adfs.640";
    internal const string AdfsName = "ADFS";
    internal const string AdfExtensionKey = "Adf";
    internal const string AdlExtensionKey = "Adl";
    internal const string ImgExtensionKey = "Img";
    internal const string St506Interface = "ST506";
    internal const string St506Format = "archimedes-st506";
    internal const int Tracks = 80;
    internal const int Sides = 2;
    internal const decimal DiameterInches = 3.5m;
    internal const int Adfs800SectorBytes = 1024;
    internal const int Adfs800SectorsPerTrack = 5;
    internal const int Adfs640SectorBytes = 256;
    internal const int Adfs640SectorsPerTrack = 16;
    internal const int MaximumTrackBytes = Adfs800SectorBytes * Adfs800SectorsPerTrack;
    internal const int MinimumSectorBytes = 128;
    internal const int MaximumCylinders = 1024;
    internal const int MaximumHeads = 8;
    internal const int MaximumSectorsPerTrack = 128;
    internal const int DefaultCylinders = 306;
    internal const int DefaultHeads = 4;
    internal const int DefaultSectorsPerTrack = 16;
    internal const long Adfs800Bytes = (long)Tracks * Sides * Adfs800SectorBytes * Adfs800SectorsPerTrack;
    internal const long Adfs640Bytes = (long)Tracks * Sides * Adfs640SectorBytes * Adfs640SectorsPerTrack;
    internal const long MaximumHardDiskBytes = (long)MaximumCylinders * MaximumHeads * MaximumSectorsPerTrack * Adfs640SectorBytes;
}
