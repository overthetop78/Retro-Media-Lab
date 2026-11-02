using System.IO;
using System.Security.Cryptography;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using C = RetroMediaLab.Emulation.Acorn.Common.Machines.AcornSystem.Constants.SystemModelConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

internal static partial class SystemFunctions
{
    internal static IReadOnlyList<FirmwareSlot> Slots(MachineConfiguration configuration)
    {
        var models = new HashSet<string>(StringComparer.Ordinal) { configuration.Model.ToString() };
        var slots = new List<FirmwareSlot> { new(EmulatorConstants.SystemFirmware, string.Empty, C.MonitorLabel, IsRequired: true, MachineIds: models) };
        if (configuration.Model != MachineModel.AcornSystem1)
            slots.Add(new(EmulatorConstants.CharacterFirmware, string.Empty, C.CharactersLabel, IsRequired: true, MachineIds: models));
        if (configuration.Model == MachineModel.AcornSystem2)
            slots.Add(new(EmulatorConstants.BasicFirmware, string.Empty, C.BasicLabel, IsRequired: false, MachineIds: models));
        return slots;
    }

    private static FirmwareCategory ImageRole(string field) => field switch
    {
        EmulatorConstants.SystemFirmware => FirmwareCategory.OperatingSystem,
        EmulatorConstants.CharacterFirmware => FirmwareCategory.CharacterGenerator,
        EmulatorConstants.BasicFirmware => FirmwareCategory.Basic,
        _ => throw new ArgumentOutOfRangeException(nameof(field))
    };

    internal static async ValueTask<IReadOnlyList<EmulationFirmwareCandidate>> ScanFirmwareFileAsync(
        MachineConfiguration configuration, string path, CancellationToken cancellationToken)
    {
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var hash = Convert.ToHexString(SHA1.HashData(bytes));
        AcornFirmware.BySha1.TryGetValue(hash, out var identity);
        return Slots(configuration).Select(slot => new EmulationFirmwareCandidate(hash + ":" + slot.FieldId, path,
            identity?.Name ?? Path.GetFileName(path), identity?.Version,
            AcornSystemModel.Get(configuration.Model, Cpu(configuration), hash, ImageRole(slot.FieldId)), slot.FieldId)).ToArray();
    }

    private static byte[] ReadImage(MachineConfiguration configuration, string field, bool required)
    {
        var path = configuration.Options.GetValueOrDefault(field);
        if (string.IsNullOrWhiteSpace(path) && !required) return [];
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new EmulationException(ErrorCategory.Firmware, ErrorCode.FirmwareMissing,
                ExceptionText.Get(CoreErrorResourceKeys.NamedFirmwareMissing,
                    ExceptionText.Get(Slots(configuration).Single(slot => slot.FieldId == field).LabelResourceKey)), isLocalized: true);
        var bytes = File.ReadAllBytes(path);
        var role = ImageRole(field);
        var compatibility = AcornSystemModel.Get(configuration.Model, Cpu(configuration), Convert.ToHexString(SHA1.HashData(bytes)), role);
        var validSize = role switch
        {
            FirmwareCategory.Basic => bytes.Length == C.BasicRomBytes,
            FirmwareCategory.CharacterGenerator => bytes.Length == (configuration.Model == MachineModel.AcornSystem5 ? C.MonochromeCharacterRomBytes : C.CharacterRomBytes),
            _ when configuration.Model == MachineModel.AcornSystem1 => bytes.Length == C.KeypadMonitorBytes,
            _ when configuration.Model == MachineModel.AcornSystem5 => bytes.Length is C.MonitorBytes or C.FastMonitorBytes or C.LargestMonitorBytes,
            _ => bytes.Length is C.SmallMonitorBytes or C.MonitorBytes or C.FastMonitorBytes
        };
        if (!validSize || compatibility == EmulationFirmwareCompatibility.Incompatible)
            throw new EmulationException(ErrorCategory.Firmware, ErrorCode.FirmwareInvalid,
                ExceptionText.Get(CoreErrorResourceKeys.FirmwareInvalid), isLocalized: true);
        return bytes;
    }
}
