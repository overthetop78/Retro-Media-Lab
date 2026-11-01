using RetroMediaLab.Emulation.Nintendo.Emulators.Mupen64PlusNext.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Mupen64PlusNext.Factories;

internal sealed class Mupen64PlusNextMachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Mupen64PlusNext;
    public bool IsDefaultFor(string machineId) => machineId is ModelConstants.Nintendo64;

    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}
