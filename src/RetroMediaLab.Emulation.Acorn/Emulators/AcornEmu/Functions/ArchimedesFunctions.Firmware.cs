using System.IO;
using System.Security.Cryptography;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Archimedes.Dictionaries;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using C = RetroMediaLab.Emulation.Acorn.Common.Machines.Archimedes.Constants.ArchimedesModelConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

internal static partial class ArchimedesFunctions
{
    internal static IReadOnlyList<FirmwareSlot> Slots(MachineConfiguration configuration) =>
    [
        new(EmulatorConstants.SystemFirmware, string.Empty, SettingsConstants.ResourceFirmware,
            IsRequired: true, MachineIds: new HashSet<string>(StringComparer.Ordinal) { configuration.MachineId })
    ];

    internal static async ValueTask<IReadOnlyList<EmulationFirmwareCandidate>> ScanFirmwareFileAsync(
        MachineConfiguration configuration, string path, bool supportsArthur, CancellationToken cancellationToken)
    {
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var hash = Convert.ToHexString(SHA1.HashData(bytes));
        AcornFirmware.BySha1.TryGetValue(hash, out var identity);
        return Slots(configuration).Select(slot => new EmulationFirmwareCandidate(
            hash + C.IdentitySeparator + slot.FieldId, path, identity?.Name ?? Path.GetFileName(path), identity?.Version,
            AcornArchimedesModel.Get(hash, FirmwareCategory.OperatingSystem, supportsArthur), slot.FieldId)).ToArray();
    }

    internal static byte[] ReadSystemRom(MachineConfiguration configuration, bool supportsArthur)
    {
        var path = configuration.Options.GetValueOrDefault(EmulatorConstants.SystemFirmware);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new EmulationException(ErrorCategory.Firmware, ErrorCode.FirmwareMissing,
                ExceptionText.Get(CoreErrorResourceKeys.NamedFirmwareMissing,
                    ExceptionText.Get(SettingsConstants.ResourceFirmware)), isLocalized: true);
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length is not (C.SmallSystemRomBytes or C.LargeSystemRomBytes)
            || AcornArchimedesModel.Get(Convert.ToHexString(SHA1.HashData(bytes)), FirmwareCategory.OperatingSystem,
                supportsArthur) == EmulationFirmwareCompatibility.Incompatible)
            throw new EmulationException(ErrorCategory.Firmware, ErrorCode.FirmwareInvalid,
                ExceptionText.Get(CoreErrorResourceKeys.FirmwareInvalid), isLocalized: true);
        return bytes;
    }
}
