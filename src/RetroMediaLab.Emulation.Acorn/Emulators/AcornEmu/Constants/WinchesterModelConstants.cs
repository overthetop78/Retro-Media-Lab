using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;
namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;

/// <summary>Mécanismes Winchester proposés par Acorn et limite d'adressage du contrôleur émulé.</summary>
internal static class WinchesterModelConstants
{
    internal const string Model110 = "acorn-winchester-110";
    internal const string Model130 = "acorn-winchester-130";
    internal const string Custom = "acorn-winchester-custom";
    internal const string PhysicalModel110 = "acorn-winchester-110-emu";
    internal const string PhysicalModel130 = "acorn-winchester-130-emu";
    internal const string PhysicalCustom = "acorn-winchester-custom-emu";
    internal const string Model110Name = "Acorn Winchester 110";
    internal const string Model130Name = "Acorn Winchester 130";
    internal const string CustomName = "Acorn SCSI";
    internal const int Cylinders110 = 306;
    internal const int Heads110 = 4;
    internal const int Cylinders130 = 640;
    internal const int Heads130 = 6;
    internal const long MaximumBytes = (long)Acb4000ParameterConstants.MaximumCylinders
        * Acb4000ParameterConstants.MaximumHeads * Acb4000FormatConstants.SmallBlockInterleavedSectors
        * Acb4000ParameterConstants.MinimumBlockSize;
}
