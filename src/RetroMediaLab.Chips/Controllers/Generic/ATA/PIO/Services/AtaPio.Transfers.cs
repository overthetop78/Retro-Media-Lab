using RetroMediaLab.Chips.Controllers.Generic.ATA.PIO.Enums;
using System.Buffers.Binary;
using System.IO;
using C = RetroMediaLab.Chips.Controllers.Generic.ATA.PIO.Constants.AtaConstants;

namespace RetroMediaLab.Chips.Controllers.Generic.ATA.PIO.Services;

public sealed partial class AtaPio
{
    private ushort ReadData()
    {
        if (_phase != AtaPhase.Sending || !Drq || !CheckAttachment()) return C.OpenBusWord;
        var bytes = Current.EightBit ? 1 : C.WordBytes;
        var value = Current.EightBit ? Current.Buffer[_offset]
            : BinaryPrimitives.ReadUInt16LittleEndian(Current.Buffer.AsSpan(_offset));
        _offset += bytes;
        if (!_dataOnly && _offset % C.SectorBytes == 0) CompleteSector();
        if (_offset == _length)
        {
            if (_dataOnly || _remaining == 0 || _transferError != 0) Finish(_transferError, interrupt: false);
            else Schedule(AtaPhase.Reading);
        }
        return (ushort)value;
    }

    private void WriteData(ushort value)
    {
        if (_phase != AtaPhase.Receiving || !Drq || !CheckAttachment()) return;
        if (Current.EightBit) Current.Buffer[_offset++] = (byte)value;
        else
        {
            BinaryPrimitives.WriteUInt16LittleEndian(Current.Buffer.AsSpan(_offset), value);
            _offset += C.WordBytes;
        }
        if (_offset != _length) return;
        if (_dataOnly) Finish();
        else
        {
            _committed = 0;
            Schedule(AtaPhase.Committing, writing: true);
        }
    }

    private bool CheckAttachment()
    {
        if (ReferenceEquals(Current.Image, _disk(_unit))) return true;
        Current.Image = _disk(_unit);
        Finish(C.AbortError);
        return false;
    }

    private void SendBuffer(int length, bool dataOnly)
    {
        _dataOnly = dataOnly;
        _offset = 0;
        _length = length;
        _phase = AtaPhase.Sending;
        Current.Error = _transferError;
        Current.Status = (byte)(C.IdleStatus | C.DataRequest | (_transferError != 0 ? C.Error : 0));
        Current.Interrupt = true;
        UpdateInterrupt();
    }

    private void ReceiveBuffer(int length, bool interrupt)
    {
        _offset = 0;
        _length = length;
        _phase = AtaPhase.Receiving;
        Current.Status = C.IdleStatus | C.DataRequest;
        if (interrupt) Current.Interrupt = true;
        UpdateInterrupt();
    }

    private void PrepareRead()
    {
        if (!TryAddress(out var block)) { Finish(C.IdNotFound); return; }
        var sectors = Math.Min(_remaining, _command == AtaCommand.ReadMultiple ? Current.Multiple : 1);
        var loaded = 0;
        _transferError = 0;
        try
        {
            for (; loaded < sectors; loaded++)
            {
                if (block + loaded >= Capacity) { _transferError = C.IdNotFound; break; }
                Position(block + loaded, writing: false);
                if (!ReadBlock(block + loaded, Current.Buffer.AsSpan(loaded * C.SectorBytes, C.SectorBytes)))
                    _transferError = C.Uncorrectable;
                Current.Reads++;
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {
            _transferError = C.Uncorrectable;
        }
        if (loaded == 0) Finish(_transferError);
        else SendBuffer(loaded * C.SectorBytes, dataOnly: false);
    }

    private void PrepareWrite()
    {
        if (!TryAddress(out _)) { Finish(C.IdNotFound); return; }
        if (Current.Image!.IsReadOnly) { Finish(C.AbortError, writeFault: true); return; }
        var sectors = Math.Min(_remaining, _command == AtaCommand.WriteMultiple ? Current.Multiple : 1);
        ReceiveBuffer(sectors * C.SectorBytes, interrupt: !_firstWrite);
        _firstWrite = false;
    }

    private void CommitSector()
    {
        if (!TryAddress(out var block)) { Finish(C.IdNotFound); return; }
        if (Current.Image!.IsReadOnly) { Finish(C.AbortError, writeFault: true); return; }
        try
        {
            Position(block, writing: true);
            WriteBlock(block, Current.Buffer.AsSpan(_committed, C.SectorBytes));
            Current.Writes++;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Finish(C.AbortError, writeFault: true);
            return;
        }
        _committed += C.SectorBytes;
        CompleteSector();
        if (_remaining == 0) Finish();
        else if (_committed < _length) Schedule(AtaPhase.Committing, writing: true);
        else PrepareWrite();
    }

    private void VerifySector()
    {
        if (!TryAddress(out var block)) { Finish(C.IdNotFound); return; }
        try
        {
            Position(block, writing: false);
            if (!ReadBlock(block, Current.Buffer.AsSpan(0, C.SectorBytes)))
            { Finish(C.Uncorrectable); return; }
            Current.Reads++;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Finish(C.Uncorrectable);
            return;
        }
        CompleteSector();
        if (_remaining == 0) Finish();
        else Schedule(AtaPhase.Verifying);
    }

    private bool ReadBlock(long block, Span<byte> destination)
    {
        var image = Current.Image!;
        var address = block * C.SectorBytes;
        var valid = true;
        while (!destination.IsEmpty)
        {
            var sector = image.ReadSectorAsync(address / image.SectorSize).GetAwaiter().GetResult();
            if (sector.Data.Length != image.SectorSize) throw new InvalidDataException();
            valid &= sector.IntegrityValid != false;
            var offset = (int)(address % image.SectorSize);
            var length = Math.Min(destination.Length, image.SectorSize - offset);
            sector.Data.Span.Slice(offset, length).CopyTo(destination);
            destination = destination[length..];
            address += length;
        }
        return valid;
    }

    private void WriteBlock(long block, ReadOnlySpan<byte> source)
    {
        var image = Current.Image!;
        var address = block * C.SectorBytes;
        while (!source.IsEmpty)
        {
            var index = address / image.SectorSize;
            var offset = (int)(address % image.SectorSize);
            var length = Math.Min(source.Length, image.SectorSize - offset);
            if (offset == 0 && length == image.SectorSize) image.WriteSector(index, source[..length]);
            else
            {
                var sector = image.ReadSectorAsync(index).GetAwaiter().GetResult();
                if (sector.Data.Length != image.SectorSize) throw new InvalidDataException();
                var original = sector.Data.ToArray();
                source[..length].CopyTo(original.AsSpan(offset));
                image.WriteSector(index, original);
            }
            source = source[length..];
            address += length;
        }
    }
}
