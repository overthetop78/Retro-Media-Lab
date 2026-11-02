namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Archimedes.Dictionaries;

/// <summary>Rôles des images système des cartes Arthur/RISC OS à ROM de 512 Kio ou 2 Mio.
/// Les cartes portables et les générations ultérieures doivent fournir leurs propres restrictions.</summary>
internal static class AcornArchimedesModel
{
    private static readonly IReadOnlySet<string> Arthur = Roms(
        "6aebd686d97dfdf6726fa5f3246ef35b840b286d",
        "1181ff9c2c2f3d6d414054ec33b2260404bafc81");
    private static readonly IReadOnlySet<string> RiscOs = Roms(
        "b82a78830dac386f9b649b6d32a34f9c6910546d",
        "9ed7dee8553d96a6d58dd23d31682234f57beb62",
        "b4fa494d51dd704c9da9653c2acc6acd08cc8cf4",
        "68533e0a93657a879c1697c04f32d517345b1a61",
        "3487729e87bebc9cb51665838c27beff22f7b3bd");

    internal static bool IsSystemRom(string hash) => Arthur.Contains(hash) || RiscOs.Contains(hash);

    internal static EmulationFirmwareCompatibility Get(string hash, FirmwareCategory role, bool supportsArthur)
    {
        if (!AcornFirmware.BySha1.ContainsKey(hash)) return EmulationFirmwareCompatibility.Unknown;
        if (role == FirmwareCategory.OperatingSystem
            && (RiscOs.Contains(hash) || supportsArthur && Arthur.Contains(hash)))
            return EmulationFirmwareCompatibility.Official;
        return EmulationFirmwareCompatibility.Incompatible;
    }

    private static IReadOnlySet<string> Roms(params string[] hashes) => hashes.ToHashSet(StringComparer.OrdinalIgnoreCase);
}
