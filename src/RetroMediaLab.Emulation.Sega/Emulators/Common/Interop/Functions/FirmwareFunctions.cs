using System.IO;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Dictionaries;

namespace RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Functions;

internal static class FirmwareFunctions
{
    internal static IReadOnlyList<FirmwareSlot> Slots(MachineConfiguration configuration) => CoreCatalog.Get(configuration.EmulatorId).Firmware.Where(slot => slot.MachineIds is null || slot.MachineIds.Contains(configuration.Model)).ToArray();

    internal static IReadOnlyDictionary<string, string> RuntimeOptions(MachineConfiguration configuration, string systemDirectory) =>
        CoreSettingsFunctions.Configure(configuration).Options!.Where(option => CoreCatalog.Get(configuration.EmulatorId).Options.Any(definition => definition.Key == option.Key))
            .ToDictionary(option => option.Key, option => option.Value, StringComparer.Ordinal);

    internal static void Prepare(MachineConfiguration configuration, string systemDirectory)
    {
        CoreCatalog.Get(configuration.EmulatorId).PrepareSystem?.Invoke(configuration, systemDirectory);
        var slots = Slots(configuration);
        var prepared = new HashSet<string>(StringComparer.Ordinal);
        void Copy(string path, FirmwareSlot slot)
        {
            var destination = Path.Combine(systemDirectory, slot.FileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(path, destination, true);
            prepared.Add(slot.FieldId);
        }
        foreach (var slot in slots)
            if (configuration.FirmwarePaths?.GetValueOrDefault(slot.FieldId) is { } path)
                Copy(path, slot);

        // Some official SMS images are shared by US and European consoles. Only content
        // identities can supply these aliases; an explicitly selected regional ROM wins.
        var regionalSms = slots.Where(slot => slot.FileName is "bios_E.sms" or "bios_U.sms" or "bios_J.sms").ToArray();
        foreach (var source in regionalSms)
            if (configuration.FirmwarePaths?.GetValueOrDefault(source.FieldId) is { } path)
                foreach (var candidate in FirmwareConfigurationFunctions.Candidates(configuration.Model, FirmwareCatalog.Inspect(path), regionalSms))
                    if (candidate.Compatibility is EmulationFirmwareCompatibility.Official or EmulationFirmwareCompatibility.Compatible
                        && candidate.DestinationFieldId is { } field && !prepared.Contains(field))
                        Copy(path, regionalSms.Single(slot => slot.FieldId == field));
    }
}
