using System.IO;
using System.Security.Cryptography;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.VideoStandardConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

/// <summary>Le choix matériel explicite prime ; seule une MOS identifiée peut guider le mode automatique.</summary>
internal static class VideoStandardFunctions
{
    internal static EmulationVideoStandard? Identify(string sha1) =>
        FirmwareCatalog.BySha1.TryGetValue(sha1, out var firmware) && firmware.Category == FirmwareCategory.OperatingSystem
            ? firmware.VideoStandard : null;

    internal static EmulationVideoStandard Resolve(IReadOnlyDictionary<string, string> options, EmulationVideoStandard? mosStandard)
    {
        var choice = options.GetValueOrDefault(C.Option, C.Automatic);
        if (!C.Choices.Contains(choice, StringComparer.Ordinal)) throw new ArgumentOutOfRangeException(nameof(options));
        return choice switch
        {
            C.Pal => EmulationVideoStandard.Pal,
            C.Ntsc => EmulationVideoStandard.Ntsc,
            _ => mosStandard ?? (options.GetValueOrDefault(C.PreviousStandard) == C.Ntsc ? EmulationVideoStandard.Ntsc : EmulationVideoStandard.Pal)
        };
    }

    internal static EmulationVideoStandard Resolve(MachineConfiguration configuration)
    {
        EmulationVideoStandard? detected = null;
        var path = configuration.Options.GetValueOrDefault(EmulatorConstants.SystemFirmware);
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            detected = Identify(Convert.ToHexString(SHA1.HashData(File.ReadAllBytes(path))));
        return Resolve(configuration.Options, detected);
    }

    /// <summary>Conserve le dernier standard effectif pour les MOS personnalisées, dans la configuration de cette machine.</summary>
    internal static IReadOnlyDictionary<string, string> Normalize(MachineConfiguration configuration) =>
        new Dictionary<string, string>(configuration.Options, StringComparer.Ordinal)
        {
            [C.PreviousStandard] = Resolve(configuration) == EmulationVideoStandard.Ntsc ? C.Ntsc : C.Pal
        };
}
