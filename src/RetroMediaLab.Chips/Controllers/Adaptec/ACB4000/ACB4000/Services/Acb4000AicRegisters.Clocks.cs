namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000AicRegisters
{
    private readonly Acb4000BufferController? clockedBuffer;
    private bool clockedTransfers;
    private bool peripheralAddressSampled;
    private bool hostAddressSampled;
    private uint peripheralAddressEpoch;
    private uint hostAddressEpoch;
    private bool peripheralCyclePending;
    private bool hostCyclePending;

    private void ConnectBufferClockInputs()
    {
        if (clockedBuffer is not null)
        {
            clockedBuffer.PeripheralCycleChanged += ReceivePeripheralCycle;
            clockedBuffer.HostCycleChanged += ReceiveHostCycle;
            ReceivePeripheralCycle(clockedBuffer.PeripheralCyclePending, clockedBuffer.PeripheralCycleEpoch);
            ReceiveHostCycle(clockedBuffer.HostCyclePending, clockedBuffer.HostCycleEpoch);
        }
        Clocks.BufferClockFell += BufferClockFell;
    }

    private void ReceivePeripheralCycle(bool pending, uint epoch)
    {
        if (!pending || peripheralAddressEpoch != epoch) peripheralAddressSampled = false;
        peripheralAddressEpoch = epoch;
        peripheralCyclePending = pending;
        UpdateBufferClockRequest();
    }

    private void ReceiveHostCycle(bool pending, uint epoch)
    {
        if (!pending || hostAddressEpoch != epoch) hostAddressSampled = false;
        hostAddressEpoch = epoch;
        hostCyclePending = pending;
        UpdateBufferClockRequest();
    }

    private void UpdateBufferClockRequest() => Clocks.SetBufferClockRequired(
        clockedTransfers && (peripheralAddressSampled || hostAddressSampled || peripheralCyclePending || hostCyclePending),
        clockedTransfers && peripheralCyclePending && !peripheralAddressSampled);

    /// <summary>Active le câblage CLKA/CLKB du tampon emprunté, sans créer une autre RAM.</summary>
    public void ConfigureClockedBufferTransfers(bool enabled)
    {
        if (enabled && clockedBuffer is null) throw new InvalidOperationException();
        Sequencer.ConfigureClockedBufferTransfers(enabled);
        clockedTransfers = enabled;
        peripheralAddressSampled = false;
        hostAddressSampled = false;
        Clocks.ResetPhase();
        UpdateBufferClockRequest();
    }

    public void AdvanceSystemHalfCycles(int count) => Clocks.AdvanceSystemHalfCycles(count);

    public void AdvanceReferenceHalfCycles(int count) => Clocks.AdvanceReferenceHalfCycles(count);

    /// <summary>Une cellule MFM représente un demi-cycle RD/REF ; RG/WG choisissent cette source.</summary>
    internal void ClockReferenceCell()
    {
        if (!clockedTransfers) return;
        Clocks.SelectReference(Sequencer.ReadGate || Sequencer.WriteGate);
        AdvanceReferenceHalfCycles(1);
    }

    private void BufferClockFell()
    {
        // The generator calls only at an actual selected, driven CLKA falling edge.
        if (peripheralAddressSampled)
        {
            clockedBuffer!.CompletePeripheralCycle();
            Sequencer.PollBufferTransfers();
            peripheralAddressSampled = false;
        }
        else if (hostAddressSampled)
        {
            clockedBuffer!.CompleteHostCycle();
            hostAddressSampled = false;
        }
        // The first edge samples the address, the next granted edge completes RAM.
        // Synchronous A retains priority over host B at this same edge.
        if (Clocks.ClockB == false && peripheralCyclePending) peripheralAddressSampled = true;
        else if (hostCyclePending) hostAddressSampled = true;
        UpdateBufferClockRequest();
    }
}
