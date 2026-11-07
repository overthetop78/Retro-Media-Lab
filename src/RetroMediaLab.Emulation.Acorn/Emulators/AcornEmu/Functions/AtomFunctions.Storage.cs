using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using C = RetroMediaLab.Emulation.Acorn.Common.Machines.AcornAtom.Constants.AtomModelConstants;
using Tape = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.TapeConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

internal static partial class AtomFunctions
{
    internal static readonly IReadOnlyList<string> TapeExtensions = Array.AsReadOnly(Tape.Extensions.Append(C.TapExtension).ToArray());
    private static bool TapeConnected(MachineConfiguration configuration) => bool.Parse(configuration.Options.GetValueOrDefault(Tape.ConnectedOption, bool.TrueString));
    private static IReadOnlyList<EmulationMediaDevice> TapeDevices(MachineConfiguration configuration) =>
        [new(EmulationMediaSlot.Tape0, EmulationMediaType.Tape, TapeExtensions,
            ConfigurationKind: EmulationStorageConfigurationKind.TapeDrive,
            ConfigurationBlocks: EmulationTapeSettings.DescribeBlocks(configuration.Options, [C.TapeBaud]))];

    internal static IReadOnlyList<EmulationMediaDevice> Devices(MachineConfiguration configuration, bool available = false) =>
        (HasDiscPack(configuration) ? StorageFunctions.Devices(configuration).Where(device => available || StorageFunctions.Connected(configuration, device.Slot.Index)) : [])
        .Concat(available || TapeConnected(configuration) ? TapeDevices(configuration) : []).ToArray();

    internal static IReadOnlyList<EmulationStorageDeviceSettings> DeviceSettings(MachineConfiguration configuration) =>
        (HasDiscPack(configuration) ? StorageFunctions.DescribeSettings(configuration) : [])
        .Concat(TapeConnected(configuration) ? [new EmulationStorageDeviceSettings(EmulationMediaSlot.Tape0,
            Options: configuration.Options.Where(pair => pair.Key.StartsWith(EmulationTapeSettings.Prefix, StringComparison.Ordinal)).ToDictionary())] : []).ToArray();

    internal static EmulationStorageSettings DescribeStorage(MachineConfiguration configuration) =>
        StorageSettingsFunctions.Describe(configuration) with { AvailableDevices = Devices(configuration, true) };

    internal static MachineConfiguration ApplyStorage(MachineConfiguration configuration, EmulationStorageSettings settings)
    {
        var allowed = Devices(configuration, true).Select(device => device.Slot).ToHashSet();
        if (settings.ConfiguredSlots.Any(slot => !allowed.Contains(slot))) throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(settings));
        var options = new Dictionary<string, string>(configuration.Options, StringComparer.Ordinal)
        {
            [Tape.ConnectedOption] = settings.ConfiguredSlots.Contains(EmulationMediaSlot.Tape0).ToString()
        };
        var selected = StorageFunctions.SelectSlots(configuration with { Options = options }, settings.ConfiguredSlots);
        return StorageSettingsFunctions.Apply(selected, StorageFunctions.MigrateMediaProtection(configuration, settings));
    }

    internal static MachineConfiguration ApplyDeviceSettings(MachineConfiguration configuration, IReadOnlyList<EmulationStorageDeviceSettings> settings)
    {
        var allowed = Devices(configuration, true).Select(device => device.Slot).ToHashSet();
        if (settings.Any(device => !allowed.Contains(device.Slot))) throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(settings));
        var options = new Dictionary<string, string>(configuration.Options, StringComparer.Ordinal);
        foreach (var pair in settings.FirstOrDefault(device => device.Slot == EmulationMediaSlot.Tape0)?.Options ?? new Dictionary<string, string>())
            if (pair.Key.StartsWith(EmulationTapeSettings.Prefix, StringComparison.Ordinal)) options[pair.Key] = pair.Value;
        return StorageFunctions.ApplySettings(configuration with { Options = options }, settings);
    }
}
