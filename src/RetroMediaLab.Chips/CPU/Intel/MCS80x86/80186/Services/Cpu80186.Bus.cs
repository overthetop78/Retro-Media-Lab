namespace RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services;

public sealed partial class Cpu80186
{
    private bool _lockPrefix;
    private bool _repeatBusLock;
    public bool HoldInput { get; set; }
    public bool HoldAcknowledged { get; private set; }
    public Action<bool>? HoldAcknowledgeChanged { get; set; }
    public Action<long>? BusClocksAdvanced { get; set; }

    private void SetHoldAcknowledged(bool asserted)
    {
        if (HoldAcknowledged == asserted) return;
        HoldAcknowledged = asserted;
        HoldAcknowledgeChanged?.Invoke(asserted);
    }

    private bool ExternalBusLocked => _prefixPending && _lockPrefix || _repeatContinuation && _repeatBusLock;

    private bool GrantExternalBus()
    {
        // Une chaîne LOCK reste indivisible pour les maîtres de bus, mais peut être interrompue.
        if (HoldInput && !ExternalBusLocked)
        {
            FinishPendingPrefetch();
            SetHoldAcknowledged(true);
            Cycles++;
            return true;
        }
        SetHoldAcknowledged(false);
        return false;
    }
}
