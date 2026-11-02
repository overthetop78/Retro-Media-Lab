using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using C = RetroMediaLab.Emulation.Acorn.Common.Machines.AcornSystem.Constants.SystemModelConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Dictionaries;

internal static class AcornEmuSystemModelCatalog
{
    internal static IReadOnlyList<AcornEmuModelDefinition> All { get; } = SystemModelCatalog.All.Select(model =>
        new AcornEmuModelDefinition(model.Model, C.FrameRate, C.PixelsWide,
            model.Model == MachineModel.AcornSystem1 ? C.KeypadPixelsHigh : C.PixelsHigh,
            C.PixelBytes, EmulationPixelFormat.Xrgb8888,
            model.Model == MachineModel.AcornSystem1 ? C.KeypadAspect : C.ScreenAspect,
            EmulationVideoStandard.Pal, SystemFunctions.Create)).ToArray();
}
