using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.GameGear;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MarkIII;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MasterSystem;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MegaCd;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MegaDrive;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Pico;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Sc3000;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Sf7000;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Sg1000;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Factories;

internal static class BoardFactory
{
    internal static SegaBoard Create(string model, MachineResources resources) => model switch
    {
        ModelConstants.Sg1000 => new Sg1000Machine(resources),
        ModelConstants.Sc3000 => new Sc3000Machine(resources),
        ModelConstants.Sf7000 => new Sf7000Machine(resources),
        ModelConstants.MarkIII => new MarkIIIMachine(resources),
        ModelConstants.MasterSystem => new MasterSystemMachine(resources, SystemHardware.MasterSystem),
        ModelConstants.MasterSystemII => new MasterSystemMachine(resources, SystemHardware.MasterSystemII),
        ModelConstants.GameGear => new GameGearMachine(resources),
        ModelConstants.MegaDrive or ModelConstants.MegaDriveII => new MegaDriveMachine(resources),
        ModelConstants.MegaCdThirtyTwoX => new MegaCdMachine(resources, enable32X: true),
        ModelConstants.MegaCd or ModelConstants.MegaCdSecond => new MegaCdMachine(resources),
        ModelConstants.Pico => new PicoMachine(resources),
        ModelConstants.ThirtyTwoX => new Machines.ThirtyTwoX.ThirtyTwoXMachine(resources),
        _ => throw new ArgumentOutOfRangeException(nameof(model), model, "Unsupported SegaEmu machine.")
    };
}
