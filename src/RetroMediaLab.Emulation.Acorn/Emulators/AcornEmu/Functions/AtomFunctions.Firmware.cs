using System.IO;
using System.Security.Cryptography;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using C = RetroMediaLab.Emulation.Acorn.Common.Machines.AcornAtom.Constants.AtomModelConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

internal static partial class AtomFunctions
{
    internal static IReadOnlyList<FirmwareSlot> Slots(MachineConfiguration configuration)
    {
        var models = new HashSet<string>(StringComparer.Ordinal) { configuration.MachineId };
        return
        [
            new(EmulatorConstants.SystemFirmware, string.Empty, C.SystemRomLabel, IsRequired: true, MachineIds: models),
            new(EmulatorConstants.CharacterFirmware, string.Empty, C.CharacterLabel, IsRequired: true, MachineIds: models),
            new(C.FloatingPointFirmware, string.Empty, C.FloatingPointLabel, IsRequired: false, MachineIds: models),
            new(C.UtilityFirmware, string.Empty, C.UtilityLabel, IsRequired: false, MachineIds: models),
            new(StorageConstants.DfsFirmware, string.Empty, C.DosLabel, IsRequired: HasDiscPack(configuration), MachineIds: models)
        ];
    }

    private static FirmwareCategory ImageRole(string field) => field switch
    {
        EmulatorConstants.SystemFirmware => FirmwareCategory.OperatingSystem,
        EmulatorConstants.CharacterFirmware => FirmwareCategory.CharacterGenerator,
        C.FloatingPointFirmware => FirmwareCategory.Basic,
        C.UtilityFirmware => FirmwareCategory.Application,
        StorageConstants.DfsFirmware => FirmwareCategory.DiscFilingSystem,
        _ => throw new ArgumentOutOfRangeException(nameof(field))
    };

    internal static async ValueTask<IReadOnlyList<EmulationFirmwareCandidate>> ScanFirmwareFileAsync(MachineConfiguration configuration, string path, CancellationToken cancellationToken)
    {
        var data = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var hash = Convert.ToHexString(SHA1.HashData(data));
        AcornFirmware.BySha1.TryGetValue(hash, out var identity);
        return Slots(configuration).Select(slot => new EmulationFirmwareCandidate(hash + C.IdentitySeparator + slot.FieldId, path,
            identity?.Name ?? Path.GetFileName(path), identity?.Version, AcornAtomModel.Get(hash, ImageRole(slot.FieldId)), slot.FieldId)).ToArray();
    }

    private static byte[] ReadImage(MachineConfiguration configuration, string field)
    {
        var slot = Slots(configuration).Single(item => item.FieldId == field);
        var path = configuration.Options.GetValueOrDefault(field);
        if (string.IsNullOrWhiteSpace(path) && !slot.IsRequired) return [];
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new EmulationException(ErrorCategory.Firmware, ErrorCode.FirmwareMissing,
                ExceptionText.Get(CoreErrorResourceKeys.NamedFirmwareMissing, ExceptionText.Get(slot.LabelResourceKey)), isLocalized: true);
        var bytes = File.ReadAllBytes(path);
        var size = field == EmulatorConstants.SystemFirmware ? C.SystemRomBytes
            : field == EmulatorConstants.CharacterFirmware ? C.CharacterRomBytes : C.OptionRomBytes;
        if (bytes.Length != size || AcornAtomModel.Get(Convert.ToHexString(SHA1.HashData(bytes)), ImageRole(field)) == EmulationFirmwareCompatibility.Incompatible)
            throw new EmulationException(ErrorCategory.Firmware, ErrorCode.FirmwareInvalid, ExceptionText.Get(CoreErrorResourceKeys.FirmwareInvalid), isLocalized: true);
        return bytes;
    }
}
