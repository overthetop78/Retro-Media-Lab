using RetroMediaLab.Emulation.Acorn.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Services;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class EmulatorCore
{
    private KeyboardInputPlayback? _keyboardPlayback;
    public bool UsesKeyboardTimeline => _definition.Model == MachineModel.AcornAtom;

    public void SetKeyboardInputFrame(KeyboardInputFrame frame)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_machine is not AtomMachine atom) throw new NotSupportedException();
        var frequency = atom.ProcessorClockHz ?? throw new InvalidOperationException();
        _keyboardPlayback ??= new KeyboardInputPlayback(frequency, atom.SetInput,
            clock => atom.SetKeyboardInputDeadline(clock, DeliverKeyboardInput));
        // Une frame de latence de transport permet de conserver les événements arrivés
        // pendant l'attente hôte entre deux champs. Les intervalles ne sont pas étirés.
        var latency = checked((long)Math.Ceiling(frequency / FramesPerSecond));
        _keyboardPlayback.Append(frame, atom.ProcessorCycles, latency);
    }

    private void DeliverKeyboardInput(long cycle) => _keyboardPlayback?.Deliver(cycle);

    private void ClearKeyboardPlayback()
    {
        _keyboardPlayback?.Reset();
        if (_machine is AtomMachine atom) atom.SetKeyboardInputDeadline(null, null);
    }
}
