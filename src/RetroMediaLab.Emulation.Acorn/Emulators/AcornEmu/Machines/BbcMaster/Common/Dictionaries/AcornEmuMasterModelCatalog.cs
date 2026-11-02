using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using C = RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMaster.Constants.MasterModelConstants;
using Video = RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Constants.VideoConstants;
using Bbc = RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Constants.BbcMicroModelConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Dictionaries;

internal static class AcornEmuMasterModelCatalog
{
    internal static IReadOnlyList<AcornEmuModelDefinition> All { get; } = [
        Create(MachineModel.BbcMaster128), Create(MachineModel.BbcMasterET), Create(MachineModel.BbcMasterCompact)
    ];
    private static AcornEmuModelDefinition Create(MachineModel model) => new(model,
        (double)C.CpuFrequency / Bbc.CyclesPerFrame, Video.Width, Video.Height,
        C.PixelBytes, EmulationPixelFormat.Xrgb8888, C.AspectRatio, EmulationVideoStandard.Pal, MasterFunctions.Create);
}
