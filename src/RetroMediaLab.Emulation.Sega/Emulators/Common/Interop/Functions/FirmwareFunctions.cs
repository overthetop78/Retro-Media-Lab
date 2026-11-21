using System.IO;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Dictionaries;
using SegaEmuFirmware = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants.FirmwareConstants;

namespace RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Functions;

internal static class FirmwareFunctions
{
    internal static string MasterSystemBiosFile(MachineConfiguration configuration) =>
        configuration.Options?.GetValueOrDefault("segaemu_region_detect") switch
        {
            "pal" => "bios_E.sms",
            "ntsc-j" => "bios_J.sms",
            "ntsc-u" => "bios_U.sms",
            _ => SegaEmuFirmware.MasterSystemAutoFile
        };

    internal static IReadOnlyList<FirmwareSlot> Slots(MachineConfiguration configuration, IReadOnlyList<FirmwareSlot>? firmware = null) =>
        (firmware ?? CoreCatalog.Get(configuration.EmulatorId).Firmware).Where(slot =>
            (slot.MachineIds is null || slot.MachineIds.Contains(configuration.Model)
                || configuration.EmulatorId == SegaEmu.Constants.CoreConstants.Id && IsThirtyTwoXFirmware(slot)
                    && ModelConstants.IsMegaDriveFamily(configuration.Model))
            && (configuration.EmulatorId != SegaEmu.Constants.CoreConstants.Id || IsSegaEmuFirmwareVisible(configuration, slot))).ToArray();

    private static bool IsThirtyTwoXFirmware(FirmwareSlot slot) =>
        slot.FileName is "32X_G_BIOS.BIN" or "32X_M_BIOS.BIN" or "32X_S_BIOS.BIN";

    private static bool IsSegaEmuFirmwareVisible(MachineConfiguration configuration, FirmwareSlot slot)
    {
        if (slot.FieldId == SegaEmu.Constants.FirmwareConstants.TmssField)
            return configuration.HasMegaDriveTmss;

        if (slot.FileName == SegaEmuFirmware.MasterSystemAutoFile || SegaEmuFirmware.MasterSystemRegionalFiles.Contains(slot.FileName))
            return slot.FileName == MasterSystemBiosFile(configuration);

        if (IsThirtyTwoXFirmware(slot))
            return ModelConstants.HasThirtyTwoX(configuration.Model) || configuration.ThirtyTwoXEnabled;

        string? accessory = slot.FileName switch
        {
            "sk.bin" or "sk2chip.bin" => "sonic & knuckles",
            "ggenie.bin" => "game genie",
            "areplay.bin" => "action replay (pro)",
            _ => null
        };
        return accessory is null || ModelConstants.GetMasterSystemConverter(configuration.Model, configuration.Options) is null
            && CartridgeStackFunctions.Read(configuration).Contains(accessory, StringComparer.Ordinal);
    }

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
            {
                Copy(path, slot);
                if (configuration.EmulatorId == SegaEmu.Constants.CoreConstants.Id
                    && slot.FileName == SegaEmuFirmware.MasterSystemAutoFile)
                {
                    var firmware = FirmwareCatalog.Inspect(path);
                    var regions = SegaEmuFirmware.MasterSystemRegionalFiles.Where(file =>
                        firmware.ExpectedFileNames?.Contains(file, StringComparer.OrdinalIgnoreCase) == true).ToArray();
                    foreach (string file in firmware.IsKnown ? regions : ["bios_U.sms"])
                        Copy(path, slot with { FileName = file });
                }
            }

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
