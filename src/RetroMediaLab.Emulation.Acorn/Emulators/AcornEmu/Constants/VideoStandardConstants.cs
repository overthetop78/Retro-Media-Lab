namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;

internal static class VideoStandardConstants
{
    internal const string Option = "acornemu.video-standard";
    internal const string PreviousStandard = "acornemu.video-standard.previous";
    internal const string Automatic = "auto";
    internal const string Pal = "pal";
    internal const string Ntsc = "ntsc";
    internal const string Label = "Emulation.Video.Standard";
    internal const string AutomaticLabel = "Emulation.Video.Signal.Standard.Automatic";
    internal const string PalLabel = "Emulation.Video.Signal.Standard.Pal";
    internal const string NtscLabel = "Emulation.Video.Signal.Standard.Ntsc";
    internal const string Help = "Emulation.Acorn.Video.Standard.Help";
    internal static IReadOnlyList<string> Choices { get; } = Array.AsReadOnly<string>([Automatic, Pal, Ntsc]);
}
