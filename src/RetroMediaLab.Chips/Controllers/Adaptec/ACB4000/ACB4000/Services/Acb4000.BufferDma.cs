using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using B = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000BufferConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000
{
    private bool _hostBufferDma;
    private bool _hostBufferReadPending;
    private bool _hostBufferWritePending;
    private byte _hostBufferInitiatorData;
    private bool _hostBufferInitiatorSelect;
    private bool _hostBufferInitiatorAck;
    private bool _hostBufferInitiatorAttention;

    private void BeginHostBufferDma(int length, bool outgoing)
    {
        var physicalRead = outgoing && _readingDisk;
        var physicalWrite = !outgoing && _writingDisk;
        _hostBufferDma = physicalRead || physicalWrite
            || _code is Acb4000CommandCode.ReadBuffer or Acb4000CommandCode.WriteBuffer;
        _hostInterfaceChanged = true;
        if (!_hostBufferDma) return;
        // Firmware 1083h/109Dh: reset pointers, stop at 03FFh, then arm host DMA.
        if (physicalRead)
        {
            _sequencerBuffer.TryWrite(B.ReadPointerLow, 0);
            _sequencerBuffer.TryWrite(B.ReadPointerHigh, 0);
        }
        else _sequencerBuffer.TryWrite(B.ResetControl, 0);
        var stop = length - 1;
        _sequencerBuffer.TryWrite(B.StopPointerLow, (byte)stop);
        _sequencerBuffer.TryWrite(B.StopPointerHigh, (byte)(stop >> B.HighByteShift));
        if (outgoing) _sequencerBuffer.TryWrite(B.DmaControl, B.HostReadPreparation);
        _sequencerBuffer.TryWrite(B.DmaControl, outgoing ? B.HostReadControl : B.HostWriteControl);
    }

    private void BeginHostBufferRead()
    {
        if (!_sequencerBuffer.RequestHostRead()) throw new InvalidOperationException();
        _hostBufferReadPending = true;
    }

    private void CompleteHostBufferRead()
    {
        if (!_hostBufferReadPending || !_sequencerBuffer.TryTakeHostCycle(out var value)) return;
        _hostBufferReadPending = false;
        SetHostDataLatch(value);
        if (!_bus.TryBeginTransfer(Acb4000BusPhase.DataIn, value)) throw new InvalidOperationException();
        SynchronizeHostInterface();
    }

    private void CompleteHostBufferWrite()
    {
        if (!_hostBufferWritePending || !_sequencerBuffer.HostTransferPending) return;
        _hostBufferWritePending = false;
        _bus.SetInitiator(_hostBufferInitiatorData, _hostBufferInitiatorSelect,
            _hostBufferInitiatorAck, reset: false, _hostBufferInitiatorAttention);
    }

    private void EndHostBufferDma()
    {
        if (!_hostBufferDma) return;
        _sequencerBuffer.TryWrite(B.DmaControl, B.HostTargetControl);
        _hostBufferDma = false;
        _hostInterfaceChanged = true;
        _hostBufferReadPending = false;
        _hostBufferWritePending = false;
    }

    private void AbortHostBufferDma()
    {
        if (!_hostBufferDma) return;
        _sequencerBuffer.CancelHostTransfer();
        EndHostBufferDma();
    }
}
