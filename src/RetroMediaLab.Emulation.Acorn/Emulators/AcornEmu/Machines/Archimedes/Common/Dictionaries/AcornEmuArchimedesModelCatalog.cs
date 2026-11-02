using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Constants;
using C = RetroMediaLab.Emulation.Acorn.Common.Machines.Archimedes.Constants.ArchimedesModelConstants;
using V = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Vidc.Constants.ArchimedesVidcConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Dictionaries;

internal static class AcornEmuArchimedesModelCatalog
{
    internal static IReadOnlyList<AcornEmuModelDefinition> All { get; } =
    [
        new(MachineModel.AcornArchimedes, ArchimedesBoardConstants.InitialFrameRate,
            V.DefaultWidth, V.DefaultHeight, V.PixelBytes, EmulationPixelFormat.Xrgb8888,
            C.DisplayAspectRatio, EmulationVideoStandard.Pal, ArchimedesFunctions.Create),
        new(MachineModel.AcornA3000, ArchimedesBoardConstants.InitialFrameRate,
            V.DefaultWidth, V.DefaultHeight, V.PixelBytes, EmulationPixelFormat.Xrgb8888,
            C.DisplayAspectRatio, EmulationVideoStandard.Pal, ArchimedesFunctions.Create)
    ];
}
