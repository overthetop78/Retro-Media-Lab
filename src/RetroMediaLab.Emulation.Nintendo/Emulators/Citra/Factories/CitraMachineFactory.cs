using RetroMediaLab.Emulation.Nintendo.Emulators.Citra.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Citra.Factories;

internal sealed class CitraMachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Citra;
    public bool IsDefaultFor(string machineId) => machineId is ModelConstants.Nintendo3Ds;

    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}
