using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MasterSystem;

internal sealed class MasterSystemMachine : SegaBoard
{
    private readonly byte _revision;

    internal MasterSystemMachine(MachineResources resources, byte revision) : base(revision, resources)
    {
        _revision = revision;
    }

    protected override void InitializeMachineBus() => InitializeEightBitMapping();
    protected override void ConfigureZ80Ports() => UseMasterSystemPorts();
    protected override Action<int> CreateFrameScheduler() => CreateEightBitFrameScheduler();
    protected override bool AcceptsHardware(byte hardware) => hardware == _revision;
}
