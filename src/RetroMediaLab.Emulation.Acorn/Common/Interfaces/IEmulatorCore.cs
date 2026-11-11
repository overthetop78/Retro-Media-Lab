using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.Emulation.Acorn.Common.Interfaces;

internal interface IEmulatorCore : IEmulationCoreRuntime<MachineConfiguration, CoreOption>, IEmulationCoreCommands<MediaConfiguration, PeripheralCategory>, IEmulationCoreIdentity<Emulator>
{
    bool UsesKeyboardTimeline => false;
    void SetKeyboardInputFrame(RetroMediaLab.Emulation.Acorn.Common.Services.KeyboardInputFrame frame)
        => throw new NotSupportedException();
}

