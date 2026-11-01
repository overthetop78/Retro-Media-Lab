using RetroMediaLab.Emulation.Nintendo.Emulators.PokeMini.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.PokeMini.Factories;

internal sealed class MachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.PokeMini;
    public bool IsDefaultFor(string machineId) => machineId is ModelConstants.PokemonMini;

    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}
