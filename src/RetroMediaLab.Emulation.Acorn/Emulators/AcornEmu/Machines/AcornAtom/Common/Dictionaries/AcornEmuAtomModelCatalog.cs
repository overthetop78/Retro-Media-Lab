using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using C = RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Constants.AtomVideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Dictionaries;

internal static class AcornEmuAtomModelCatalog
{
    internal static IReadOnlyList<AcornEmuModelDefinition> All { get; } =
    [
        new(MachineModel.AcornAtom, C.FramesPerSecond, C.Width, C.Height, C.PixelBytes,
            EmulationPixelFormat.Xrgb8888, C.AspectRatio, EmulationVideoStandard.Ntsc, AtomFunctions.Create)
    ];
}
