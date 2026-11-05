using System.Buffers.Binary;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000Constants;
using D = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000DiagnosticConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000
{
    private bool _reinitializing;
    private bool _diagnosticReading;
    private bool _diagnosticPatching;
    private ushort _diagnosticReadAddress;
    private ushort _diagnosticAddress;
    private int _diagnosticPatchRemaining;

    private void BeginSendDiagnostic()
    {
        var length = BinaryPrimitives.ReadUInt16BigEndian(_command.AsSpan(D.CdbLengthOffset));
        if ((_command[C.UnitOffset] & C.ShortAddressMask) != 0 || _command[D.CdbReservedOffset] != 0
            || length < D.ParameterLength)
        {
            Complete(C.BadArgument);
            return;
        }
        // OEM: only patch operations take more than four bytes; excess bytes are not requested.
        DiagnosticBytesRemaining = length;
        Receive(D.ParameterLength);
    }

    private void EndSendDiagnostic()
    {
        var option = _buffer[D.OptionOffset];
        if (option is D.DumpHardwareOption or D.DumpRamOption or D.PatchHardwareOption or D.PatchRamOption)
        {
            if (_buffer[D.QualifierOffset] != D.FirmwareQualifier) { Complete(C.BadArgument); return; }
            var hardware = option is D.DumpHardwareOption or D.PatchHardwareOption;
            var address = hardware ? D.HardwareBase : D.RamBase;
            if (option is D.DumpHardwareOption or D.DumpRamOption)
            {
                DiagnosticDumpAddress = address;
                DiagnosticDumpReady = true;
                Complete();
                return;
            }
            _diagnosticAddress = (ushort)(address | _buffer[D.AddressOffset]);
            var count = _buffer[D.PatchCountOffset] == 0 ? D.MaximumPatchLength : _buffer[D.PatchCountOffset];
            _diagnosticPatchRemaining = count;
            if (!CanWriteBoardMemory(_diagnosticAddress, Math.Min(count, (int)DiagnosticBytesRemaining)))
            { Complete(C.InvalidCommand); return; }
            if (DiagnosticBytesRemaining == 0) { Complete(); return; }
            _diagnosticPatching = true;
            Request(Acb4000BusPhase.DataOut);
            return;
        }
        if (_buffer[D.OptionOffset] == D.ReinitializeDriveOption)
        {
            if (_buffer[D.QualifierOffset] != D.FirmwareQualifier) { Complete(C.BadArgument); return; }
            _reinitializing = true;
            if (Drive is not { } drive || !_driveReady[Unit] || _driveWriteFault)
            { Complete(D.ReinitializeFailure); return; }
            _parametersLoaded[Unit] = false;
            _completeAfterParameterAcquisition = true;
            LoadParameters(drive);
            return;
        }
        if (_buffer[D.OptionOffset] != D.ReadErrorHandlingOption
            || _buffer[D.ReadModeOffset] > (byte)Acb4000ReadErrorMode.ReportCorrections)
        {
            Complete(C.BadArgument);
            return;
        }
        // Option 65 ignores the qualifier/count and preserves other bits of the context byte.
        SetReadErrorMode((Acb4000ReadErrorMode)_buffer[D.ReadModeOffset]);
        Complete();
    }
    private void BeginReceiveDiagnostic(bool available)
    {
        if (!available) { Complete(C.InvalidCommand); return; }
        if ((_command[C.UnitOffset] & C.ShortAddressMask) != 0 || _command[D.CdbReservedOffset] != 0)
        { Complete(C.BadArgument); return; }
        var allocation = BinaryPrimitives.ReadUInt16BigEndian(_command.AsSpan(D.CdbLengthOffset));
        DiagnosticBytesRemaining = allocation;
        _diagnosticReadAddress = DiagnosticDumpAddress;
        _offset = 0;
        _length = Math.Min((int)allocation, D.DumpHeaderLength + D.DumpMemoryLength);
        if (!CanReadBoardMemory(_diagnosticReadAddress, Math.Max(0, _length - D.DumpHeaderLength)))
        { Complete(C.InvalidCommand); return; }
        if (_length == 0) { Complete(); return; }
        _diagnosticReading = true;
        _stage = Acb4000Stage.DataIn;
        Request(Acb4000BusPhase.DataIn, DiagnosticReadByte(0));
    }

    private byte DiagnosticReadByte(int offset) => offset switch
    {
        D.DumpLengthHighOffset => (byte)(D.DeclaredDumpLength >> C.AddressMiddleShift),
        D.DumpLengthLowOffset => (byte)(D.DeclaredDumpLength & byte.MaxValue),
        D.DumpAddressHighOffset => (byte)(_diagnosticReadAddress >> C.AddressMiddleShift),
        D.DumpAddressLowOffset => (byte)_diagnosticReadAddress,
        _ => ReadBoardMemory((ushort)(_diagnosticReadAddress + offset - D.DumpHeaderLength))
    };

    private void WriteDiagnosticByte(byte value)
    {
        WriteBoardMemory(_diagnosticAddress, value);
        _diagnosticAddress = unchecked((ushort)(_diagnosticAddress + 1));
        if (--_diagnosticPatchRemaining == 0 || DiagnosticBytesRemaining == 0) Complete();
        else if (!CanWriteBoardMemory(_diagnosticAddress, 1)) Complete(C.InvalidCommand);
        else Request(Acb4000BusPhase.DataOut);
    }

}
