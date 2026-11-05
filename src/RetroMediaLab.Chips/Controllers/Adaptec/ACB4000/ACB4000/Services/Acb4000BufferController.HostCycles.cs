namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000BufferController
{
    private bool hostCycleRequested;
    private bool hostCycleReady;
    private bool hostCycleWriting;
    private byte hostCycleValue;

    internal bool HostCyclePending => hostCycleRequested;
    internal uint HostCycleEpoch { get; private set; }

    internal bool RequestHostRead() => RequestHostCycle(writing: false, incoming: 0);

    internal bool RequestHostWrite(byte incoming) => RequestHostCycle(writing: true, incoming);

    private bool RequestHostCycle(bool writing, byte incoming)
    {
        if (!HostTransferEnabled || PeripheralReading == writing || hostCycleRequested || hostCycleReady)
            return false;
        hostCycleWriting = writing;
        hostCycleValue = incoming;
        hostCycleRequested = true;
        return true;
    }

    internal bool CompleteHostCycle()
    {
        if (!hostCycleRequested) return false;
        if (!(hostCycleWriting ? TryBeginHostWrite(hostCycleValue) : TryBeginHostRead(out hostCycleValue)))
            return false;
        hostCycleRequested = false;
        hostCycleReady = true;
        return true;
    }

    internal bool TryTakeHostCycle(out byte value)
    {
        value = hostCycleValue;
        if (!hostCycleReady) return false;
        hostCycleReady = false;
        return true;
    }

    private void CancelHostCycle()
    {
        HostCycleEpoch = unchecked(HostCycleEpoch + 1);
        hostCycleRequested = hostCycleReady = false;
    }
}
