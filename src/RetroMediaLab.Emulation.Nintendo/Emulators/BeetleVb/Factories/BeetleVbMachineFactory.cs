using RetroMediaLab.Emulation.Nintendo.Emulators.BeetleVb.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.BeetleVb.Factories;

internal sealed class BeetleVbMachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.BeetleVb;
    public bool IsDefaultFor(string machineId) => machineId is ModelConstants.VirtualBoy;

    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}
