namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000AicRegisters
{
    private readonly Acb4000BufferController? clockedBuffer;
    private bool clockedTransfers;
    private bool peripheralAddressSampled;
    private bool hostAddressSampled;
    private uint peripheralAddressEpoch;
    private uint hostAddressEpoch;

    /// <summary>Active le câblage CLKA/CLKB du tampon emprunté, sans créer une autre RAM.</summary>
    public void ConfigureClockedBufferTransfers(bool enabled)
    {
        if (enabled && clockedBuffer is null) throw new InvalidOperationException();
        Sequencer.ConfigureClockedBufferTransfers(enabled);
        clockedTransfers = enabled;
        peripheralAddressSampled = false;
        hostAddressSampled = false;
        Clocks.ResetPhase();
    }

    public void AdvanceSystemHalfCycles(int count) => AdvanceBufferClock(count, reference: false);

    public void AdvanceReferenceHalfCycles(int count) => AdvanceBufferClock(count, reference: true);

    /// <summary>Une cellule MFM représente un demi-cycle RD/REF ; RG/WG choisissent cette source.</summary>
    internal void ClockReferenceCell()
    {
        if (!clockedTransfers) return;
        Clocks.SelectReference(Sequencer.ReadGate || Sequencer.WriteGate);
        AdvanceReferenceHalfCycles(1);
    }

    private void AdvanceBufferClock(int count, bool reference)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        // Only a selected, driven source can produce an input edge on the RAM controller.
        if (!clockedTransfers || reference != Clocks.ReferenceSelected
            || !Clocks.OutputsEnabled || !Clocks.SourceIsDefined)
        {
            AdvanceSource(count, reference);
            return;
        }
        while (count > 0 && (peripheralAddressSampled || hostAddressSampled
            || clockedBuffer!.PeripheralCyclePending || clockedBuffer.HostCyclePending || Clocks.TransferPending))
        {
            if (!clockedBuffer!.PeripheralCyclePending || peripheralAddressEpoch != clockedBuffer.PeripheralCycleEpoch)
                peripheralAddressSampled = false;
            if (!clockedBuffer.HostCyclePending || hostAddressEpoch != clockedBuffer.HostCycleEpoch)
                hostAddressSampled = false;
            if (clockedBuffer.PeripheralCyclePending && !peripheralAddressSampled && !Clocks.TransferPending)
                Clocks.TryRequestTransfer();
            var distance = Clocks.HalfCyclesToNextBufferEvent(out var fallingEdge);
            if (distance > count) break;
            AdvanceSource(distance, reference);
            count -= distance;
            if (!fallingEdge) continue;
            // The first CLK edge accepts REQA/address. The following cycle returns RAM data.
            if (peripheralAddressSampled)
            {
                clockedBuffer.CompletePeripheralCycle();
                Sequencer.PollBufferTransfers();
                peripheralAddressSampled = false;
            }
            else if (hostAddressSampled)
            {
                clockedBuffer.CompleteHostCycle();
                hostAddressSampled = false;
            }
            // Address selection for the next RAM cycle: synchronous A has priority over B.
            if (Clocks.ClockB == false && clockedBuffer.PeripheralCyclePending)
            {
                peripheralAddressSampled = true;
                peripheralAddressEpoch = clockedBuffer.PeripheralCycleEpoch;
            }
            else if (clockedBuffer.HostCyclePending)
            {
                hostAddressSampled = true;
                hostAddressEpoch = clockedBuffer.HostCycleEpoch;
            }
        }
        // No pending access: skip the idle span without a half-cycle loop.
        AdvanceSource(count, reference);
    }

    private void AdvanceSource(int count, bool reference)
    {
        if (reference) Clocks.AdvanceReferenceHalfCycles(count);
        else Clocks.AdvanceSystemHalfCycles(count);
    }
}
