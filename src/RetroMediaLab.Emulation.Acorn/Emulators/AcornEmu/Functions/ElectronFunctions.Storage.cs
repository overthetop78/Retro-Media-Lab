using C = RetroMediaLab.Emulation.Acorn.Common.Machines.AcornElectron.Constants.ElectronModelConstants;
using Tape = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.TapeConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

internal static partial class ElectronFunctions
{
    private static bool TapeConnected(MachineConfiguration configuration) => bool.Parse(configuration.Options.GetValueOrDefault(Tape.ConnectedOption, bool.TrueString));

    internal static IReadOnlyList<EmulationMediaDevice> Devices(MachineConfiguration configuration, bool available = false) =>
        available || TapeConnected(configuration)
            ? [new(EmulationMediaSlot.Tape0, EmulationMediaType.Tape, Tape.Extensions,
                ConfigurationKind: EmulationStorageConfigurationKind.TapeDrive,
                ConfigurationBlocks: EmulationTapeSettings.DescribeBlocks(configuration.Options, [C.TapeBaud]))] : [];

    internal static IReadOnlyList<EmulationStorageDeviceSettings> DeviceSettings(MachineConfiguration configuration) =>
        TapeConnected(configuration) ? [new(EmulationMediaSlot.Tape0,
            Options: configuration.Options.Where(pair => pair.Key.StartsWith(EmulationTapeSettings.Prefix, StringComparison.Ordinal)).ToDictionary())] : [];

    internal static EmulationStorageSettings DescribeStorage(MachineConfiguration configuration) =>
        StorageSettingsFunctions.Describe(configuration) with { AvailableDevices = Devices(configuration, true) };

    internal static MachineConfiguration ApplyStorage(MachineConfiguration configuration, EmulationStorageSettings settings)
    {
        if (settings.ConfiguredSlots.Any(slot => slot != EmulationMediaSlot.Tape0))
            throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(settings));
        var options = new Dictionary<string, string>(configuration.Options, StringComparer.Ordinal)
        {
            [Tape.ConnectedOption] = settings.ConfiguredSlots.Contains(EmulationMediaSlot.Tape0).ToString()
        };
        return StorageSettingsFunctions.Apply(configuration with { Options = options }, settings);
    }

    internal static MachineConfiguration ApplyDeviceSettings(MachineConfiguration configuration, IReadOnlyList<EmulationStorageDeviceSettings> settings)
    {
        if (settings.Any(device => device.Slot != EmulationMediaSlot.Tape0))
            throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(settings));
        var options = new Dictionary<string, string>(configuration.Options, StringComparer.Ordinal);
        foreach (var pair in settings.SingleOrDefault()?.Options ?? new Dictionary<string, string>())
            if (pair.Key.StartsWith(EmulationTapeSettings.Prefix, StringComparison.Ordinal)) options[pair.Key] = pair.Value;
        return configuration with { Options = options };
    }
}
