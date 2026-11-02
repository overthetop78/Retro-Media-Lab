namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Services;

internal sealed partial class Cpu80186
{
    private bool _lockPrefix;
    private bool _repeatBusLock;
    internal bool HoldInput { get; set; }
    internal bool HoldAcknowledged { get; private set; }
    internal Action<bool>? HoldAcknowledgeChanged { get; set; }
    internal Action<long>? BusClocksAdvanced { get; set; }

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
