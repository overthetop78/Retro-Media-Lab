using B = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000BufferConstants;
using F = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000AicFirmwareConstants;
using R = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000AicRegisterConstants;
using S = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000SequencerConstants;
using Read = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000ReadSequencerConstants;
using Write = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000WriteSequencerConstants;
using BusTime = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000BusConstants;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000
{
    private readonly Acb4000AicRegisters _aic;
    private readonly Acb4000BufferController _sequencerBuffer;
    private Acb4000EccRegisters _eccRegisters => _aic.Ecc;
    internal Acb4000AicRegisters Aic => _aic;
    internal Acb4000BufferController BufferController => _sequencerBuffer;
    private bool _nativeReadOperation;
    private readonly long _aicSystemHalfCyclesPerSecond;
    private readonly int _aicSystemEdgesPerTick;
    private readonly long _aicSystemEdgeRemainder;
    private readonly int _aicOwnerTicksPerSecond;
    private long _aicSystemClockFraction;
    private long _aicHostTimeFraction;

    public void AdvanceIdleTicks(uint ticks)
    {
        if (!IsIdle) throw new InvalidOperationException();
        _aicSystemClockFraction += ticks * _aicSystemHalfCyclesPerSecond;
        var edges = Math.DivRem(_aicSystemClockFraction, _aicOwnerTicksPerSecond,
            out _aicSystemClockFraction);
        _aic.Clocks.SelectReference(false);
        _aic.Clocks.AdvanceSystemHalfCycles(edges);
    }

    private void TickAicSystemClock()
    {
        var edges = _aicSystemEdgesPerTick;
        _aicSystemClockFraction += _aicSystemEdgeRemainder;
        if (_aicSystemClockFraction >= _aicOwnerTicksPerSecond)
        {
            _aicSystemClockFraction -= _aicOwnerTicksPerSecond;
            edges++;
        }
        _aic.Clocks.SelectReference(_aic.Sequencer.ReadGate || _aic.Sequencer.WriteGate);
        _aic.AdvanceSystemHalfCycles(edges);
        if (_sequencerBuffer.HostArbitrationPending)
        {
            _aicHostTimeFraction += BusTime.NanosecondsPerSecond;
            var nanoseconds = Math.DivRem(_aicHostTimeFraction, _aicOwnerTicksPerSecond, out _aicHostTimeFraction);
            _sequencerBuffer.AdvanceHostInterfaceNanoseconds((uint)nanoseconds);
        }
    }

    private void InitializeAic()
    {
        _nativeReadOperation = false;
        _aicSystemClockFraction = 0;
        _aicHostTimeFraction = 0;
        _aic.Reset();
        _aic.Ecc.InitializeFirmware();
        _sequencerBuffer.TryWrite(B.ResetControl, B.HoldReset);
        _sequencerBuffer.TryWrite(B.ResetControl, 0);
        _sequencerBuffer.TryWrite(B.BufferSize, B.FirmwareBufferSize);
        _sequencerBuffer.TryWrite(B.DmaControl, B.FirmwareDmaControl);
        var program = F.Program;
        for (var column = 0; column < S.ColumnCount; column++)
            for (var state = 0; state < S.StateCount; state++)
                _aic.TryWrite((byte)(S.RamBase + column * S.ColumnStride + state), program[column * S.StateCount + state]);
        _aic.TryWrite(R.WriteAddressMark, F.InitialWriteAddressMark);
        _aic.TryWrite(R.DetectAddressMark, F.InitialDetectAddressMark);
        _aic.TryWrite(R.GpioControl, F.InitialGpioControl);
        _aic.TryWrite(R.ClockStack, F.InitialClockControl);
        _aic.ConfigureClockedBufferTransfers(true);
    }

    private void BeginNativeReadBuffer()
    {
        _nativeReadOperation = true;
        _sequencerBuffer.TryWrite(B.ResetControl, 0);
        _sequencerBuffer.TryWrite(B.DmaControl, B.HostReadPreparation);
        _aic.Sequencer.WriteOperation((byte)(_aic.Sequencer.ReadOperation() & Read.ReadOperationMask));
    }

    private void MaintainNativeReadCounter()
    {
        var sequencer = _aic.Sequencer;
        if (_stage is not (Acb4000Stage.Read or Acb4000Stage.Write or Acb4000Stage.Format)
            || sequencer.State != Read.DataState || !sequencer.TransferEnabled || sequencer.CarryInhibited) return;
        // Carry belongs to the serial field counter, not the later RAM completion.
        var transferred = sequencer.FieldDataByteCount;
        if (transferred >= (ulong)BlockSize || (ulong)BlockSize - transferred <= Read.DataChunkBytes) return;
        sequencer.WriteOperation((byte)(sequencer.ReadOperation() | S.InhibitCarry));
    }

    private void BeginNativeWrite()
    {
        _sequencerBuffer.TryWrite(B.ReadPointerLow, 0);
        _sequencerBuffer.TryWrite(B.ReadPointerHigh, 0);
        _sequencerBuffer.TryWrite(B.DmaControl, B.FirmwareDmaControl);
        _aic.TryWrite((byte)(S.RamBase + Write.EccState), Write.CompletionState);
        _aic.TryWrite((byte)(S.RamBase + S.ColumnStride + Write.DataState), Write.DataControl);
        _aic.Sequencer.WriteOperation((byte)(_aic.Sequencer.ReadOperation() & Read.ReadOperationMask));
        _writeChannel.BeginSequenced();
    }

    private void BeginNativeDataRead()
    {
        _sequencerBuffer.TryWrite(B.WritePointerLow, 0);
        _sequencerBuffer.TryWrite(B.WritePointerHigh, 0);
        _readChannel.BeginDataRead();
    }

    private void EndNativeRead()
    {
        if (!_nativeReadOperation) return;
        _readChannel.StopRead();
        _nativeReadOperation = false;
    }

}
