using RetroMediaLab.Emulation.Sega.Emulators.PicoDrive.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Sega.Emulators.PicoDrive.Factories;

internal sealed class MachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.PicoDrive;
    public bool IsDefaultFor(string machineId) => machineId is ModelConstants.ThirtyTwoX;

    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}
