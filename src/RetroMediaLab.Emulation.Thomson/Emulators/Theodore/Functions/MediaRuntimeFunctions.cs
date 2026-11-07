using CommonCoreHostFunctions = RetroMediaLab.Emulation.Thomson.Common.Functions.CoreHostFunctions;
using System.IO;
using RetroMediaLab.Emulation.Thomson.Emulators.Theodore.Constants;

namespace RetroMediaLab.Emulation.Thomson.Emulators.Theodore.Functions;
internal static class MediaRuntimeFunctions
{
    internal static MediaCategory Category(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            MediaRuntimeConstants.Floppy or MediaRuntimeConstants.SapFloppy => MediaCategory.Floppy,
            MediaRuntimeConstants.Tape => MediaCategory.Tape,
            MediaRuntimeConstants.MoCartridge or MediaRuntimeConstants.ToCartridge or MediaRuntimeConstants.RomCartridge => MediaCategory.Cartridge,
            _ => throw CommonCoreHostFunctions.Messages.CreateError(EmulationMessageCategory.Media, EmulationMessageCode.MediaUnsupported, null)
        };
    }

    internal static IReadOnlyList<string> InitialContentPaths(MachineConfiguration configuration) => configuration.InitialContent is { Length: > 0 } path ? [Path.GetFullPath(path)] : configuration.Media.Where(media => media.IsInserted).Select(media => Path.GetFullPath(media.Path)).ToArray();
    internal static IReadOnlyList<EmulationMediaDevice> Devices(MachineConfiguration configuration)
    {
        var cartridge = configuration.Family == MachineFamily.Mo ? MediaRuntimeConstants.MoCartridge : MediaRuntimeConstants.ToCartridge;
        var integrated = ConfigurationFunctions.IntegratedStorageSlots(configuration.Model);
        return[
            new(EmulationMediaSlot.Tape0, EmulationMediaType.Tape, [MediaRuntimeConstants.Tape], RequiresMachineRecreation: true, IsPermanent: integrated.Contains(EmulationMediaSlot.Tape0)),
            new(EmulationMediaSlot.Floppy0, EmulationMediaType.Floppy, [MediaRuntimeConstants.Floppy, MediaRuntimeConstants.SapFloppy], RequiresMachineRecreation: true, IsPermanent: integrated.Contains(EmulationMediaSlot.Floppy0)),
            new(EmulationMediaSlot.Cartridge0, EmulationMediaType.Cartridge, [cartridge, MediaRuntimeConstants.RomCartridge], RequiresMachineRecreation: true, IsPermanent: integrated.Contains(EmulationMediaSlot.Cartridge0))
        ];
    }
}
