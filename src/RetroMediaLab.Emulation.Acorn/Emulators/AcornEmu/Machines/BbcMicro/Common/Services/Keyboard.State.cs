using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class Keyboard
{
    internal KeyboardState CaptureState() => new(_activeColumns, (bool[])_matrix.Clone(), BreakPressed);

    internal void RestoreState(KeyboardState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Matrix?.Length != _matrix.Length) throw new ArgumentException(nameof(state));
        state.Matrix.CopyTo(_matrix, 0);
        _activeColumns = state.ActiveColumns;
        BreakPressed = state.BreakPressed;
    }
}
