using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;

public sealed partial class Via6522
{
    private byte _interruptFlags;
    private byte _interruptEnabled;
    private bool _irq;

    /// <summary>Assertion IRQ mise à jour au changement IFR/IER, avant le retour au propriétaire.</summary>
    public Action<bool>? IrqChanged { get; set; }

    private byte InterruptFlags
    {
        get => _interruptFlags;
        set
        {
            if (_interruptFlags == value) return;
            _interruptFlags = value;
            UpdateIrq();
        }
    }

    private byte InterruptEnabled
    {
        get => _interruptEnabled;
        set
        {
            if (_interruptEnabled == value) return;
            _interruptEnabled = value;
            UpdateIrq();
        }
    }

    private void UpdateIrq()
    {
        var asserted = (_interruptFlags & _interruptEnabled & C.InterruptMask) != 0;
        if (_irq == asserted) return;
        _irq = asserted;
        IrqChanged?.Invoke(asserted);
    }
}
