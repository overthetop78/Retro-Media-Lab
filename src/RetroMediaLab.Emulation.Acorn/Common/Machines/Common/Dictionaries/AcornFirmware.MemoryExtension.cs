namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries;

/// <summary>Identités des ROM de la catégorie MemoryExtension.</summary>
internal static partial class AcornFirmware
{
    private static IReadOnlyDictionary<string, FirmwareDefinition> MemoryExtensionRoms() =>
        new Dictionary<string, FirmwareDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["466e3b68f1a8e0287ae095cb3c16dac6afc89dcb"] = new("Aries B32", "1.00", FirmwareCategory.MemoryExtension),
            ["0f5b2a5bee23808b34eab2f027f7c1e77395c782"] = new("Computech Integra-B", "1.20", FirmwareCategory.MemoryExtension),
            ["2590596e0864b97b30050c33b21aa89bb8956204"] = new("Aries B20", "2.4", FirmwareCategory.MemoryExtension),
            ["d04bfd332d63f6f23dca2ef81a1d2074e3d88ee9"] = new("Aries B20", "2.2", FirmwareCategory.MemoryExtension),
            ["0a5efb084eb1ae353b9c79c026e9e26cd7695293"] = new("Watford Electronics 32K RAM", "2.40", FirmwareCategory.MemoryExtension),
            ["7f8536576de449916cd77488e94775ed438c5c6c"] = new("Watford Electronics 32K RAM", "2.20", FirmwareCategory.MemoryExtension),
            ["98a3084b20c92427c2e9def58ade96f42659e20a"] = new("Watford Electronics 32K RAM", "2.00", FirmwareCategory.MemoryExtension),
            ["210f6162d634fca8d75197d4f64d9b50abc01e1e"] = new("Watford Electronics 32K RAM", "1.06", FirmwareCategory.MemoryExtension),
            ["1b59a3354cfc704ad8aa60b0ed16e4cd2d815031"] = new("Watford Electronics 32K RAM", "1.00", FirmwareCategory.MemoryExtension)
        };
}
