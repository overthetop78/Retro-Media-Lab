using RetroMediaLab.Emulation.Nintendo.Emulators.Mesen.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Mesen.Factories;

internal sealed class MesenMachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Mesen;
    public bool IsDefaultFor(string machineId) => machineId is ModelConstants.Nes or ModelConstants.FamicomDisk;

    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}
