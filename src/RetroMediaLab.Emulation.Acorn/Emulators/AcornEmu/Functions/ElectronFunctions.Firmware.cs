using System.IO;
using System.Security.Cryptography;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using C = RetroMediaLab.Emulation.Acorn.Common.Machines.AcornElectron.Constants.ElectronModelConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

internal static partial class ElectronFunctions
{
    private static bool CombinedSystem(MachineConfiguration configuration)
    {
        var path = configuration.Options.GetValueOrDefault(EmulatorConstants.SystemFirmware);
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path) && new FileInfo(path).Length == C.CombinedRomBytes;
    }

    internal static IReadOnlyList<FirmwareSlot> Slots(MachineConfiguration configuration)
    {
        var models = new HashSet<string>(StringComparer.Ordinal) { configuration.MachineId };
        return
        [
            new(EmulatorConstants.SystemFirmware, string.Empty, C.SystemRomLabel, IsRequired: true, MachineIds: models),
            new(EmulatorConstants.BasicFirmware, string.Empty, C.BasicRomLabel, IsRequired: !CombinedSystem(configuration), MachineIds: models)
        ];
    }

    private static FirmwareCategory ImageRole(string field) => field == EmulatorConstants.SystemFirmware ? FirmwareCategory.OperatingSystem : FirmwareCategory.Basic;

    internal static async ValueTask<IReadOnlyList<EmulationFirmwareCandidate>> ScanFirmwareFileAsync(MachineConfiguration configuration, string path, CancellationToken cancellationToken)
    {
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var hash = Convert.ToHexString(SHA1.HashData(bytes));
        AcornFirmware.BySha1.TryGetValue(hash, out var identity);
        return Slots(configuration).Select(slot => new EmulationFirmwareCandidate(hash + C.IdentitySeparator + slot.FieldId, path,
            identity?.Name ?? Path.GetFileName(path), identity?.Version, AcornElectronModel.Get(hash, ImageRole(slot.FieldId)), slot.FieldId)).ToArray();
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
        if (bytes.Length is not (C.RomBytes or C.CombinedRomBytes)
            || AcornElectronModel.Get(Convert.ToHexString(SHA1.HashData(bytes)), ImageRole(field)) == EmulationFirmwareCompatibility.Incompatible)
            throw new EmulationException(ErrorCategory.Firmware, ErrorCode.FirmwareInvalid, ExceptionText.Get(CoreErrorResourceKeys.FirmwareInvalid), isLocalized: true);
        return bytes;
    }
}
