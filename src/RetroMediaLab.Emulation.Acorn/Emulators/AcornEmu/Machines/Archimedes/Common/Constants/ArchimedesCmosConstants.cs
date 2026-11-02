using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Enums;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Pcf8583.Constants.Pcf8583Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Constants;

/// <summary>Paramètres initiaux de la RAM sauvegardée, et non firmware embarqué.
/// Valeurs des CMOS aa310 Arthur/RISC OS 2/RISC OS 3, séparées des registres PCF8583.
/// L'OS adresse ses octets depuis &amp;40, puis reprend à &amp;10 après &amp;FF.</summary>
internal static class ArchimedesCmosConstants
{
    private const int ConfigurationStart = 0x40;
    private const int FilingConfigurationStart = 0xc4;
    private const int AdfsBuffers = 0xc9;
    private const int CurrentYear = 0xc0;
    private const int CurrentCentury = 0xc1;
    private const int YearsPerCentury = 100;
    private const int Checksum = 0x3f;
    private const int ShiftCapsMode = 0x4b;
    private const int NetFilerOptions = 0x51;
    private const int FileSwitchOptions = 0x5c;
    private const int ConfiguredLanguage = 0xf9;
    private const int ConfiguredCountry = 0xfa;
    private const byte BritishCountry = 1;
    private const byte EarlyDesktopLanguage = 3;
    private const byte RiscOs3DesktopLanguage = 10;
    private const byte RiscOs2ReferenceYear = 88;
    private const byte RiscOs3ReferenceYear = 92;
    private const byte ReferenceCentury = 19;
    private const byte NetFilerLibrary = 2;
    private const byte RiscOs3CapsMode = 0x50;
    private const byte RiscOs3FileSwitch = 1;
    private const byte RiscOs3Checksum = 0x6d;
    private const int SuffixStart = 0x12;
    private const int AdfsDrives = 0xc7;
    private const int DefaultFloppyDrives = 1;
    private const int St506CountShift = 3;
    private const byte IdeDriveMask = 0xc0;

    // Station/serveurs, ADFS, TV, clavier, répétition, imprimante et format série.
    private static ReadOnlySpan<byte> CommonConfiguration =>
        [0, 0xfe, 0, 0xeb, 0, 8, 0, 0, 0, 0, 0x10, 0x60, 0x20, 8, 10, 0x2c, 0x80, 0];

    // Dump, moniteur, fontes, un lecteur ADFS, buffers, tailles écran/système/sprites et son.
    private static ReadOnlySpan<byte> EarlyFilingConfiguration =>
        [0, 1, 6, 1, 0xff, 0, 8, 0xff, 0xff, 0xff, 0, 0, 0, 0, 2, 1, 0xf0];
    private static ReadOnlySpan<byte> RiscOs3FilingConfiguration =>
        [4, 0xfd, 8, 1, 0xff, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xf0];
    private static ReadOnlySpan<byte> RiscOs3Suffix => [3, 0x14, 0, 0x6f, 0x40];

    private static readonly byte[] ArthurRam = Build(ArchimedesCmosProfile.Arthur);
    private static readonly byte[] RiscOs2Ram = Build(ArchimedesCmosProfile.RiscOs2);
    private static readonly byte[] RiscOs3Ram = Build(ArchimedesCmosProfile.RiscOs3);

    internal static ReadOnlySpan<byte> Reference(ArchimedesCmosProfile profile) => profile switch
    {
        ArchimedesCmosProfile.Arthur => ArthurRam,
        ArchimedesCmosProfile.RiscOs2 => RiscOs2Ram,
        ArchimedesCmosProfile.RiscOs3 => RiscOs3Ram,
        _ => throw new ArgumentOutOfRangeException(nameof(profile))
    };

    /// <summary>Une nouvelle pile CMOS reçoit l'année complète cohérente avec le compteur matériel.
    /// Les restaurations utilisateur n'appellent pas cette création de paramètres initiaux.</summary>
    internal static byte[] Create(ArchimedesCmosProfile profile, DateTime time,
        int floppyDrives = DefaultFloppyDrives, int st506Drives = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(floppyDrives);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(floppyDrives, ArchimedesBoardConstants.FloppyDrives);
        ArgumentOutOfRangeException.ThrowIfNegative(st506Drives);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(st506Drives, ArchimedesBoardConstants.HardDiskDrives);
        var ram = Reference(profile).ToArray();
        var year = (byte)(time.Year % YearsPerCentury);
        var century = (byte)(time.Year / YearsPerCentury);
        var adjustment = year - ram[CurrentYear - C.RamStart] + century - ram[CurrentCentury - C.RamStart];
        ram[CurrentYear - C.RamStart] = year;
        ram[CurrentCentury - C.RamStart] = century;
        var previousDrives = ram[AdfsDrives - C.RamStart];
        var drives = (byte)((previousDrives & IdeDriveMask) | floppyDrives | st506Drives << St506CountShift);
        ram[AdfsDrives - C.RamStart] = drives;
        adjustment += drives - previousDrives;
        if (profile == ArchimedesCmosProfile.RiscOs3)
            ram[Checksum - C.RamStart] = unchecked((byte)(ram[Checksum - C.RamStart] + adjustment));
        return ram;
    }

    private static byte[] Build(ArchimedesCmosProfile profile)
    {
        var ram = new byte[C.RamSize];
        CommonConfiguration.CopyTo(ram.AsSpan(ConfigurationStart - C.RamStart));
        EarlyFilingConfiguration.CopyTo(ram.AsSpan(FilingConfigurationStart - C.RamStart));
        ram[ConfiguredLanguage - C.RamStart] = EarlyDesktopLanguage;
        ram[ConfiguredCountry - C.RamStart] = BritishCountry;
        if (profile == ArchimedesCmosProfile.Arthur) return ram;
        ram[NetFilerOptions - C.RamStart] = NetFilerLibrary;
        ram[CurrentCentury - C.RamStart] = ReferenceCentury;
        if (profile == ArchimedesCmosProfile.RiscOs2)
        {
            ram[CurrentYear - C.RamStart] = RiscOs2ReferenceYear;
            ram[FilingConfigurationStart - C.RamStart] = RiscOs3FilingConfiguration[0];
            // Le deuxième octet des buffers est commun aux paramètres de RISC OS 2 et 3.
            ram[AdfsBuffers - C.RamStart] = RiscOs3FilingConfiguration[AdfsBuffers - FilingConfigurationStart];
            return ram;
        }
        RiscOs3FilingConfiguration.CopyTo(ram.AsSpan(FilingConfigurationStart - C.RamStart));
        RiscOs3Suffix.CopyTo(ram.AsSpan(SuffixStart - C.RamStart));
        ram[CurrentYear - C.RamStart] = RiscOs3ReferenceYear;
        ram[ShiftCapsMode - C.RamStart] = RiscOs3CapsMode;
        ram[FileSwitchOptions - C.RamStart] = RiscOs3FileSwitch;
        ram[ConfiguredLanguage - C.RamStart] = RiscOs3DesktopLanguage;
        ram[Checksum - C.RamStart] = RiscOs3Checksum;
        return ram;
    }
}
