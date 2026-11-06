using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class SidewaysRoms
{
    internal SidewaysRamState CaptureState() => new(_selected,
        _banks.Select((bank, index) => _writable[index] ? bank?.ToArray() : null).ToArray(),
        (bool[])_protected.Clone());

    internal void RestoreState(SidewaysRamState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if ((uint)state.Selected >= _banks.Length || state.RamBanks?.Length != _banks.Length
            || state.Protected?.Length != _protected.Length) throw new ArgumentException(nameof(state));
        for (var bank = 0; bank < _banks.Length; bank++)
            if (_writable[bank] ? state.RamBanks[bank]?.Length != _banks[bank]!.Length : state.RamBanks[bank] is not null)
                throw new ArgumentException(nameof(state));
        for (var bank = 0; bank < _banks.Length; bank++)
            if (_writable[bank]) state.RamBanks[bank]!.CopyTo(_banks[bank]!, 0);
        state.Protected.CopyTo(_protected, 0);
        _selected = state.Selected;
    }
}
