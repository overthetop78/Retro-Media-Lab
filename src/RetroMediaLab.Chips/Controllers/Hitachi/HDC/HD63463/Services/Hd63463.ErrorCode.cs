using System.Buffers.Binary;
using C = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Constants.Hd63463Constants;
using Command = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463Command;
using E = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Constants.Hd63463ErrorCodeConstants;
using Codes = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Functions.Hd63463ErrorCodeFunctions;

namespace RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Services;

public sealed partial class Hd63463
{
    private uint _lastEccSyndrome;
    private int _lastEccLength;
    private bool _lastEccAvailable;
    private byte _readFieldStatus;

    private void ResetErrorCode()
    {
        _lastEccSyndrome = 0;
        _lastEccLength = 0;
        _lastEccAvailable = false;
    }

    private bool CheckRecordedData(DiskSector sector, int buffer, bool storeInBuffer)
    {
        var code = sector.RecordedErrorCheckCode.Span;
        if (code.IsEmpty) return true;
        if (sector.Data.Length != RecordBytes || Image!.SectorSize != RecordBytes)
        { Finish(C.DataError); return false; }
        var prefix = sector.RecordedDataPrefix.Span;
        if ((_specification[C.ModeParameter] & E.AddressMarkExcluded) != 0 && !prefix.IsEmpty)
            prefix = prefix[E.AddressMarkBytes..];
        var mode = _specification[C.OperationModeParameter];
        if ((mode & E.EccMode) == 0)
        {
            if (code.Length != E.CrcBytes) { Finish(C.DataError); return false; }
            var polynomial = (mode & E.CrcPolynomialSelect) != 0 ? E.CcittPolynomial : E.AlternateCrcPolynomial;
            var initial = (mode & E.CrcInitialOnes) != 0 ? ushort.MaxValue : (ushort)0;
            if (Codes.Crc(prefix, sector.Data.Span, polynomial, initial) != BinaryPrimitives.ReadUInt16BigEndian(code))
            { Finish(C.DataError); return false; }
            return true;
        }
        if (code.Length != E.FireBytes) { Finish(E.Uncorrectable); return false; }
        var syndrome = Codes.Fire(prefix, sector.Data.Span) ^ BinaryPrimitives.ReadUInt32BigEndian(code);
        if (syndrome == 0) return true;
        _lastEccAvailable = true;
        _lastEccSyndrome = syndrome;
        _lastEccLength = RecordBytes;
        if ((mode & C.AutomaticCorrection) != 0 && _command != Command.ReadErroneousData)
        {
            if (!Codes.Locate(syndrome, RecordBytes, out var address, out var pattern))
            { Finish(E.Uncorrectable); return false; }
            if (storeInBuffer)
                for (var index = 0; index < E.PatternBytes; index++)
                    _buffers[buffer][address + index] ^= (byte)(pattern >> ((E.PatternBytes - 1 - index) * C.ByteBits));
            _readFieldStatus = E.Corrected;
        }
        else
        {
            // The host receives the erroneous DATA before requesting CKE. A
            // provisional correctable status does not promise CKE will succeed.
            _readFieldStatus = E.HostCorrection;
        }
        return true;
    }

    private void CheckEcc(int length)
    {
        if (length != 0) { Finish(C.InvalidParameter); return; }
        if (!_lastEccAvailable || !Codes.Locate(_lastEccSyndrome, _lastEccLength, out var address, out var pattern))
        { Finish(E.Uncorrectable); return; }
        Array.Clear(_parameters);
        BinaryPrimitives.WriteUInt16BigEndian(_parameters, (ushort)address);
        for (var index = 0; index < E.PatternBytes; index++)
            _parameters[E.ResultPatternOffset + index] = (byte)(pattern >> ((E.PatternBytes - 1 - index) * C.ByteBits));
        Finish();
    }
}
