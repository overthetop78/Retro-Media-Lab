using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Contracts;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Constants;

internal static class CoreConstants
{
    internal const string Id = "genesisplusgxcs";
    internal const string DisplayName = "GenesisPlusGXCS";
    internal const string Version = "1.7.6+2499cd3";
    internal static IReadOnlyList<string> Extensions { get; } = [".m3u", ".mdx", ".md", ".smd", ".gen", ".bin", ".cue", ".iso", ".chd", ".bms", ".sms", ".gg", ".sg", ContentConstants.Sc3000SourceExtension, ".68k", ".sgd"];
    internal static IReadOnlySet<string> MachineIds { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ModelConstants.Sg1000, ModelConstants.Sc3000, ModelConstants.MarkIII, ModelConstants.MasterSystem,
        ModelConstants.GameGear, ModelConstants.MegaDrive, ModelConstants.MegaCd, ModelConstants.Pico
    };
    internal static CoreDefinition Definition { get; } = new(
        Id, DisplayName, "retromedialab.emulation.sega.dll",
        Extensions, null,
        OptionConstants.All,
        FirmwareConstants.All,
        new(Id, DisplayName, "Emulation.Emulator.genesisplusgxcs.Description", MachineIds),
        MachineOptions: MachineOptionConstants.All,
        PrepareContent: Functions.ContentFunctions.Prepare);
}
