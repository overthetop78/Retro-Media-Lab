using System.IO;
using System.Globalization;
using RetroMediaLab.Emulation.HardDisks;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Dictionaries;
using CommonHost = RetroMediaLab.Emulation.Acorn.Common.Functions.CoreHostFunctions;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Constants.ArchimedesDriveConstants;
using S = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

internal static partial class ArchimedesFunctions
{
    internal static bool FloppyConnected(MachineConfiguration configuration, int drive) =>
        drive >= 0 && drive < FloppyDriveCount(configuration)
        && (drive == EmulationMediaSlot.Floppy0.Index
            || bool.Parse(configuration.Options.GetValueOrDefault(S.Key(drive, S.ConnectedOption), bool.FalseString)));

    internal static bool HardDiskConnected(MachineConfiguration configuration, int unit) =>
        HasSt506(configuration) && unit >= 0 && unit < ArchimedesBoardConstants.HardDiskDrives
        && bool.Parse(configuration.Options.GetValueOrDefault(S.HardDiskKey(unit, S.ConnectedOption), bool.FalseString));

    internal static string DriveModel(MachineConfiguration configuration, int drive) =>
        configuration.Options.GetValueOrDefault(S.Key(drive, S.ModelOption), C.Model) == C.Model
            ? C.Model : throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(configuration));

    internal static IReadOnlyList<EmulationMediaDevice> FloppyDevices(MachineConfiguration configuration) =>
        Enumerable.Range(0, FloppyDriveCount(configuration)).Select(drive => new EmulationMediaDevice(
            new(EmulationMediaCategory.FloppyDrive, drive), EmulationMediaType.Floppy,
            ArchimedesDriveModels.Floppies[0].AcceptedExtensions,
            FloppyOptions: new(ArchimedesDriveModels.Floppies, configuration.Folders.Floppies ?? string.Empty,
                string.Empty, CommonHost.MediaProcessing.FileExtensions[C.AdfExtensionKey],
                CanCreateBlankMedia: true, CanRedirectWrites: false)
                { SpeedChoices = EmulationDriveSpeedChoices.Standard, CanConfigureWriteProtection = false },
            IsPermanent: drive == EmulationMediaSlot.Floppy0.Index,
            ConfigurationKind: EmulationStorageConfigurationKind.FloppyDrive)).ToArray();

    internal static IReadOnlyList<EmulationMediaDevice> HardDiskDevices(MachineConfiguration configuration) =>
        !HasSt506(configuration) ? [] : Enumerable.Range(0, ArchimedesBoardConstants.HardDiskDrives).Select(unit =>
            new EmulationMediaDevice(new(EmulationMediaCategory.HardDisk, unit), EmulationMediaType.HardDisk,
                S.HardDiskExtensions, IsRemovable: false,
                InterfaceChoices: [new(C.St506Interface, string.Empty, C.St506Interface)],
                HardDiskFormats: ArchimedesDriveModels.HardDisks,
                ConfigurationKind: EmulationStorageConfigurationKind.HardDiskDrive,
                ConfigurationFields: StorageFunctions.HardDiskFields(configuration, unit))).ToArray();

    internal static IReadOnlyList<EmulationMediaDevice> Devices(MachineConfiguration configuration, bool available = false) =>
        FloppyDevices(configuration).Where(device => available || FloppyConnected(configuration, device.Slot.Index))
        .Concat(HardDiskDevices(configuration).Where(device => available || HardDiskConnected(configuration, device.Slot.Index))).ToArray();

    internal static IReadOnlyList<EmulationStorageDeviceSettings> DeviceSettings(MachineConfiguration configuration) =>
        Enumerable.Range(0, FloppyDriveCount(configuration)).Where(drive => FloppyConnected(configuration, drive))
        .Select(drive => new EmulationStorageDeviceSettings(new(EmulationMediaCategory.FloppyDrive, drive), StorageFunctions.Settings(configuration, drive)))
        .Concat(Enumerable.Range(0, ArchimedesBoardConstants.HardDiskDrives).Where(unit => HardDiskConnected(configuration, unit))
            .Select(unit => new EmulationStorageDeviceSettings(new(EmulationMediaCategory.HardDisk, unit), InterfaceId: C.St506Interface,
                Options: StorageFunctions.HardDiskFields(configuration, unit).ToDictionary(field => field.Id, field => field.Value ?? string.Empty, StringComparer.Ordinal))))
        .ToArray();

    internal static EmulationStorageSettings DescribeStorage(MachineConfiguration configuration) =>
        StorageSettingsFunctions.Describe(configuration) with { AvailableDevices = Devices(configuration, true) };

    internal static MachineConfiguration SelectSlots(MachineConfiguration configuration, IReadOnlyList<EmulationMediaSlot> slots)
    {
        var allowed = Devices(configuration, true).Select(device => device.Slot).ToHashSet();
        if (slots.Any(slot => !allowed.Contains(slot)) || slots.Distinct().Count() != slots.Count)
            throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(slots));
        var options = new Dictionary<string, string>(configuration.Options, StringComparer.Ordinal);
        for (var drive = 0; drive < FloppyDriveCount(configuration); drive++)
            options[S.Key(drive, S.ConnectedOption)] = (drive == EmulationMediaSlot.Floppy0.Index
                || slots.Contains(new(EmulationMediaCategory.FloppyDrive, drive))).ToString();
        for (var unit = 0; unit < ArchimedesBoardConstants.HardDiskDrives; unit++)
            options[S.HardDiskKey(unit, S.ConnectedOption)] = slots.Contains(new(EmulationMediaCategory.HardDisk, unit)).ToString();
        return configuration with { Options = options };
    }

    internal static MachineConfiguration ApplyStorage(MachineConfiguration configuration, EmulationStorageSettings settings)
    {
        if (!settings.ConfiguredSlots.Contains(EmulationMediaSlot.Floppy0))
            settings = settings with { ConfiguredSlots = settings.ConfiguredSlots.Append(EmulationMediaSlot.Floppy0).ToArray() };
        return StorageSettingsFunctions.Apply(SelectSlots(configuration, settings.ConfiguredSlots), settings);
    }

    internal static MachineConfiguration ApplyDeviceSettings(MachineConfiguration configuration, IReadOnlyList<EmulationStorageDeviceSettings> settings)
    {
        var allowed = Devices(configuration, true).Select(device => device.Slot).ToHashSet();
        var options = new Dictionary<string, string>(configuration.Options, StringComparer.Ordinal);
        foreach (var device in settings)
        {
            if (!allowed.Contains(device.Slot)) throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(settings));
            if (device.Slot.Category == EmulationMediaCategory.FloppyDrive)
            {
                if (device.Floppy is not { } floppy || floppy.Model != C.Model || !S.Speeds.Contains(floppy.Speed)
                    || floppy.Options?.Keys.Any(key => key is not (S.MechanicalSoundOption or S.SoundVolumeOption)) == true)
                    throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(settings));
                options[S.Key(device.Slot.Index, S.ModelOption)] = floppy.Model;
                options[S.Key(device.Slot.Index, S.SpeedOption)] = floppy.Speed;
                foreach (var pair in floppy.Options ?? new Dictionary<string, string>()) options[S.Key(device.Slot.Index, pair.Key)] = pair.Value;
            }
            else
            {
                if (device.InterfaceId is not null && device.InterfaceId != C.St506Interface
                    || device.Options?.Keys.Any(key => !S.HardDiskOptions.Contains(key, StringComparer.Ordinal)) == true)
                    throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(settings));
                foreach (var pair in device.Options ?? new Dictionary<string, string>()) options[S.HardDiskKey(device.Slot.Index, pair.Key)] = pair.Value;
            }
        }
        var result = configuration with { Options = options };
        ValidateStorage(result);
        return result;
    }

    private static void ValidateStorage(MachineConfiguration configuration)
    {
        for (var drive = 0; drive < FloppyDriveCount(configuration); drive++)
        {
            _ = FloppyConnected(configuration, drive);
            var settings = StorageFunctions.Settings(configuration, drive);
            if (!S.Speeds.Contains(settings.Speed) || !bool.TryParse(settings.Options![S.MechanicalSoundOption], out _))
                throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(configuration));
        }
        for (var unit = 0; unit < ArchimedesBoardConstants.HardDiskDrives; unit++)
        {
            _ = HardDiskConnected(configuration, unit);
            var settings = StorageFunctions.HardDiskSettings(configuration, unit);
            if (settings.ReadLatency is < 0 or > S.MaximumLatencyMilliseconds
                || settings.WriteLatency is < 0 or > S.MaximumLatencyMilliseconds
                || !S.Speeds.Contains(settings.Speed.ToString(CultureInfo.InvariantCulture))
                || !bool.TryParse(configuration.Options.GetValueOrDefault(S.HardDiskKey(unit, S.MechanicalSoundOption), bool.TrueString), out _))
                throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(configuration));
            _ = EmulationMechanicalSoundSettings.ReadVolume(configuration.Options, S.HardDiskKey(unit, S.SoundVolumeOption));
        }
    }

    internal static void CreateHardDisk(string path, long bytes, bool fixedSize, HardDiskPreparation preparation, DiskImageGeometry? geometry)
    {
        if (geometry is null || geometry.Capacity != bytes || geometry.Cylinders is <= 0 or > C.MaximumCylinders
            || geometry.Heads is <= 0 or > C.MaximumHeads || geometry.SectorsPerTrack is <= 0 or > C.MaximumSectorsPerTrack
            || geometry.SectorSize != C.Adfs640SectorBytes || preparation is not (HardDiskPreparation.Blank or HardDiskPreparation.AcornAdfs))
            throw new ArgumentException(ErrorMessages.IncompatibleMedia, nameof(geometry));
        AcornDiskFunctions.CreateWinchester(path, bytes, fixedSize, preparation, geometry);
    }
}
