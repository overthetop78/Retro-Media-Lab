using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronVideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Dictionaries;

internal static class AcornEmuElectronModelCatalog
{
    internal static IReadOnlyList<AcornEmuModelDefinition> All { get; } =
    [
        new(MachineModel.AcornElectron, C.FramesPerSecond, C.Width, C.Height, C.PixelBytes,
            EmulationPixelFormat.Xrgb8888, C.AspectRatio, EmulationVideoStandard.Pal, ElectronFunctions.Create)
    ];
}
