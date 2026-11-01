using RetroMediaLab.Emulation.Nintendo.Emulators.Gambatte.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Gambatte.Factories;

internal sealed class GambatteMachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Gambatte;
    public bool IsDefaultFor(string machineId) => machineId is ModelConstants.GameBoy or ModelConstants.GameBoyColor;

    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}
