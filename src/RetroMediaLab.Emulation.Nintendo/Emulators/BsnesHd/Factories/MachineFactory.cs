using RetroMediaLab.Emulation.Nintendo.Emulators.BsnesHd.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.BsnesHd.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.BsnesHd;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}
