using System.Buffers.Binary;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using M = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000MemoryConstants;
using E = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000EccRegisterConstants;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000Constants;
using B = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000BufferConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000
{
    private readonly byte[] _ram = new byte[M.RamLength];
    // The external LO latch has no reset input; resetting the ASIC disables its drivers instead.
    private byte _hostDataLatch;
    // Deterministic digital convention for undriven diagnostic reads, not a measured 8085 bus voltage.
    // Only driven cycles replace it; an unpopulated address does not become a fictitious register.
    private byte _boardDataBus;
    private bool _hostSignalsPublished;
    private bool _publishedHostSelect;
    private bool _publishedHostBusy;
    private bool _publishedHostAck;
    private byte _publishedHostOutputs;
    private byte _publishedHostData;
    private bool _publishedHostDmaControl;

    private void SynchronizeHostInterface()
    {
        var select = _bus.Select;
        var busy = _bus.Busy;
        if (!_hostSignalsPublished || select != _publishedHostSelect || busy != _publishedHostBusy
            || _hostAckInput != _publishedHostAck) PublishHostInputs(select, busy);
        // ACK must reach the ASIC before BOE is calculated for this same bus transition.
        var outputs = _sequencerBuffer.HostOutputState;
        var dmaControl = _hostBufferDma && _stage == Acb4000Stage.DataIn;
        if (!_hostSignalsPublished || outputs != _publishedHostOutputs || _hostDataLatch != _publishedHostData
            || dmaControl != _publishedHostDmaControl)
        {
            _bus.SetManualOutputs((outputs & B.HostSelectOutput) != 0, (outputs & B.HostBusyOutput) != 0,
                (outputs & B.HostOutputEnable) != 0, _hostDataLatch, dmaControl);
            _publishedHostOutputs = outputs;
            _publishedHostData = _hostDataLatch;
            _publishedHostDmaControl = dmaControl;
            select = _bus.Select;
            busy = _bus.Busy;
            if (select != _publishedHostSelect || busy != _publishedHostBusy) PublishHostInputs(select, busy);
        }
        _hostSignalsPublished = true;
    }

    private void PublishHostInputs(bool select, bool busy)
    {
        _sequencerBuffer.SetHostBusInputs(select, busy, _hostAckInput);
        _publishedHostSelect = select;
        _publishedHostBusy = busy;
        _publishedHostAck = _hostAckInput;
    }

    private bool DiagnosticDumpReady
    {
        get => (_ram[M.DumpFlagsOffset] & M.DumpReadyMask) != 0;
        set => _ram[M.DumpFlagsOffset] = (byte)(value
            ? _ram[M.DumpFlagsOffset] | M.DumpReadyMask
            : _ram[M.DumpFlagsOffset] & ~M.DumpReadyMask);
    }

    private ushort DiagnosticDumpAddress
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(_ram.AsSpan(M.DumpAddressOffset));
        set => BinaryPrimitives.WriteUInt16LittleEndian(_ram.AsSpan(M.DumpAddressOffset), value);
    }

    private ushort DiagnosticBytesRemaining
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(_ram.AsSpan(M.DiagnosticCounterOffset));
        set => BinaryPrimitives.WriteUInt16LittleEndian(_ram.AsSpan(M.DiagnosticCounterOffset), value);
    }

    private void StoreCommandMemory()
    {
        _command.AsSpan(0, _commandLength).CopyTo(_ram.AsSpan(M.WorkingOffset));
        if (_commandLength == C.LongCdbLength)
        {
            _ram[M.WorkingOffset + C.UnitOffset] = (byte)(_command[C.ShortAddressLowOffset] & C.ShortAddressMask);
            _ram[M.WorkingOffset + C.ShortAddressMiddleOffset] = _command[C.ShortCountOffset];
            _ram[M.WorkingOffset + C.ShortAddressLowOffset] = _command[C.ShortCdbLength - 1];
            DiagnosticBytesRemaining = BinaryPrimitives.ReadUInt16BigEndian(_command.AsSpan(C.LongCountOffset));
        }
        else DiagnosticBytesRemaining = _command[C.ShortCountOffset] == 0 ? (ushort)C.MaximumShortBlockCount : _command[C.ShortCountOffset];
        _ram[M.WorkingOffset + C.UnitOffset] &= C.ShortAddressMask;
    }

    private void SaveDiagnosticFlags()
    {
        if (Unit < C.DriveCount)
            _ram[Unit * M.UnitContextStride + M.SavedDumpFlagsOffset] = _ram[M.DumpFlagsOffset];
    }

    private bool CanReadBoardMemory(ushort address, int length)
    {
        if (_registerBus is not null || length == 0) return true;
        var last = (int)address + length - 1;
        if (address >= M.RamBase && last < M.RamBase + M.RamLength) return true;
        return address >= E.RegisterPage && last <= E.RegisterPage + byte.MaxValue;
    }

    private void InitializeMemory()
    {
        // Cold entry 0055-0075. Bus RESET does not execute this RAM clear.
        _ram[M.DriveControlOffset] = M.InitialDriveControl;
        _ram[M.BlockSizeHighOffset] = M.InitialBlockSizeHigh;
        _ram[M.HeadCountOffset] = M.InitialHeadCount;
        _ram[M.ContextPointerHighOffset] = (byte)(M.RamBase >> C.AddressMiddleShift);
    }

    private void SelectMemoryUnit()
    {
        if (Unit >= C.DriveCount || _ram[M.ContextPointerOffset] == Unit * M.UnitContextStride) return;
        // Firmware 05FA-0615 exchanges 808F-80AE, not the command bytes 8080-808E.
        var working = _ram.AsSpan(M.WorkingOffset + M.SwappedContextOffset, M.SwappedContextLength);
        working.CopyTo(_ram.AsSpan((Unit ^ M.OtherUnitMask) * M.UnitContextStride + M.SwappedContextOffset));
        _ram.AsSpan(Unit * M.UnitContextStride + M.SwappedContextOffset, M.SwappedContextLength).CopyTo(working);
        _ram[M.ContextPointerOffset] = (byte)(Unit * M.UnitContextStride);
    }

    private Acb4000ReadErrorMode ReadErrorMode =>
        (Acb4000ReadErrorMode)((_ram[M.WorkingOffset + M.ReadPolicyOffset]
            & M.ReadPolicyMask) >> M.ReadPolicyShift);

    private void SetReadErrorMode(Acb4000ReadErrorMode mode)
    {
        ref var flags = ref _ram[M.WorkingOffset + M.ReadPolicyOffset];
        flags = (byte)((flags & ~M.ReadPolicyMask) | ((byte)mode << M.ReadPolicyShift));
    }

    private byte ReadBoardMemory(ushort address)
    {
        var offset = address - M.RamBase;
        if ((uint)offset < M.RamLength) return _boardDataBus = _ram[offset];
        if (_registerBus is null && address == E.RegisterPage + B.HostData
            && _sequencerBuffer.HostInputEnabled) return _boardDataBus = _bus.SampleHostData();
        if (_registerBus is not null)
        {
            if (address >= E.RegisterPage && address <= E.RegisterPage + byte.MaxValue)
                _aic.BeginExternalCycle((byte)(address - E.RegisterPage), writing: false);
            try { return _boardDataBus = _registerBus.Read(address); }
            finally { _aic.EndExternalCycle(); }
        }
        if (address >= E.RegisterPage && address <= E.RegisterPage + byte.MaxValue)
        {
            if (_sequencerBuffer.TryRead((byte)(address - E.RegisterPage), out var value)
                || _aic.TryRead((byte)(address - E.RegisterPage), out value)) _boardDataBus = value;
            return _boardDataBus;
        }
        throw new InvalidOperationException(); // The diagnostic preflight rejects undecoded addresses.
    }

    private void WriteBoardMemory(ushort address, byte value)
    {
        _boardDataBus = value;
        var offset = address - M.RamBase;
        if ((uint)offset < M.RamLength) { _ram[offset] = value; return; }
        if (_registerBus is not null)
        {
            if (address >= E.RegisterPage && address <= E.RegisterPage + byte.MaxValue)
                _aic.BeginExternalCycle((byte)(address - E.RegisterPage), writing: true);
            try { _registerBus.Write(address, value); }
            finally { _aic.EndExternalCycle(); }
        }
        if (address >= E.RegisterPage && address <= E.RegisterPage + byte.MaxValue)
        {
            var register = (byte)(address - E.RegisterPage);
            if (register == B.HostData) _hostDataLatch = value;
            else if (!_sequencerBuffer.TryWrite(register, value)) _aic.TryWrite(register, value);
            SynchronizeHostInterface();
        }
    }

    private bool CanWriteBoardMemory(ushort address, int length)
    {
        if (_registerBus is not null || length == 0) return true;
        var last = (int)address + length - 1;
        if (address >= M.RamBase && last < M.RamBase + M.RamLength) return true;
        return address >= E.RegisterPage && last <= E.RegisterPage + byte.MaxValue;
    }
}
