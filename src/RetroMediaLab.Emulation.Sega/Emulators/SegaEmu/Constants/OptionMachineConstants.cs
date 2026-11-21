namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;

internal static class OptionMachineConstants
{
    internal static IReadOnlySet<string> Sg1000 { get; } = new HashSet<string>(StringComparer.Ordinal) { ModelConstants.Sg1000, ModelConstants.Sc3000 };
    internal static IReadOnlySet<string> MasterSystem { get; } = new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MarkIII, ModelConstants.MasterSystem, ModelConstants.MasterSystemII };
    internal static IReadOnlySet<string> GameGear { get; } = new HashSet<string>(StringComparer.Ordinal) { ModelConstants.GameGear };
    internal static IReadOnlySet<string> MegaDrive { get; } = new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaDrive, ModelConstants.MegaDriveII, ModelConstants.ThirtyTwoX, ModelConstants.MegaCdThirtyTwoX };
    internal static IReadOnlySet<string> ThirtyTwoX { get; } = new HashSet<string>(StringComparer.Ordinal) { ModelConstants.ThirtyTwoX, ModelConstants.MegaCdThirtyTwoX };
    internal static IReadOnlySet<string> LockOn { get; } = new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaDrive, ModelConstants.MegaDriveII, ModelConstants.MegaCd, ModelConstants.MegaCdSecond, ModelConstants.ThirtyTwoX, ModelConstants.MegaCdThirtyTwoX };
    internal static IReadOnlySet<string> MegaCd { get; } = new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaCd, ModelConstants.MegaCdSecond, ModelConstants.MegaCdThirtyTwoX };
    internal static IReadOnlySet<string> MegaDriveAndCd { get; } = new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaDrive, ModelConstants.MegaDriveII, ModelConstants.MegaCd, ModelConstants.MegaCdSecond, ModelConstants.ThirtyTwoX, ModelConstants.MegaCdThirtyTwoX };
    internal static IReadOnlySet<string> PsgSystems { get; } = new HashSet<string>(StringComparer.Ordinal)
    { ModelConstants.Sg1000, ModelConstants.Sc3000, ModelConstants.MarkIII, ModelConstants.MasterSystem, ModelConstants.MasterSystemII, ModelConstants.GameGear, ModelConstants.MegaDrive, ModelConstants.MegaDriveII, ModelConstants.MegaCd, ModelConstants.MegaCdSecond, ModelConstants.ThirtyTwoX, ModelConstants.MegaCdThirtyTwoX };
    internal static IReadOnlySet<string> FmSystems { get; } = new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MarkIII, ModelConstants.MasterSystem, ModelConstants.MasterSystemII, ModelConstants.MegaDrive, ModelConstants.MegaDriveII, ModelConstants.MegaCd, ModelConstants.MegaCdSecond, ModelConstants.ThirtyTwoX, ModelConstants.MegaCdThirtyTwoX };
    internal static IReadOnlySet<string> LightgunSystems { get; } = new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MarkIII, ModelConstants.MasterSystem, ModelConstants.MasterSystemII, ModelConstants.MegaDrive, ModelConstants.MegaDriveII, ModelConstants.MegaCd, ModelConstants.MegaCdSecond, ModelConstants.ThirtyTwoX, ModelConstants.MegaCdThirtyTwoX };
    internal static IReadOnlySet<string> BootRomSystems { get; } = new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MarkIII, ModelConstants.MasterSystem, ModelConstants.MasterSystemII, ModelConstants.GameGear, ModelConstants.MegaDrive, ModelConstants.MegaDriveII, ModelConstants.MegaCd, ModelConstants.MegaCdSecond, ModelConstants.ThirtyTwoX, ModelConstants.MegaCdThirtyTwoX };
}
