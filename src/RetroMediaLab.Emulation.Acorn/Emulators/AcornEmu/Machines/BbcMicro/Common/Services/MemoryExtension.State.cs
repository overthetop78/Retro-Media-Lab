using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class MemoryExtension
{
    internal MemoryExtensionState CaptureState() => new(_selection, (byte[])_ram.Clone());

    internal void RestoreState(MemoryExtensionState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Ram?.Length != _ram.Length) throw new ArgumentException(nameof(state));
        state.Ram.CopyTo(_ram, 0);
        _selection = state.Selection;
    }
}
