using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelBPlus.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelBPlus.Services;

internal sealed partial class ModelBPlusMemory
{
    internal ModelBPlusMemoryState CaptureState() => new((byte[])_shadow.Clone(), (byte[])_private.Clone(), _paged, ShadowEnabled);

    internal void RestoreState(ModelBPlusMemoryState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Shadow?.Length != _shadow.Length || state.Private?.Length != _private.Length)
            throw new ArgumentException(nameof(state));
        state.Shadow.CopyTo(_shadow, 0);
        state.Private.CopyTo(_private, 0);
        _paged = state.Paged;
        ShadowEnabled = state.ShadowEnabled;
    }
}
