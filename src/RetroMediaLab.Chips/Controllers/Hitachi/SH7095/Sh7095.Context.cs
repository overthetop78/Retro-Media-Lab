using System.Buffers.Binary;

namespace RetroMediaLab.Chips.Controllers.Hitachi.SH7095;

public sealed partial class Sh7095
{
    public const int ContextSize = 6208;
    public void SaveContext(Span<byte> data)
    {
        SynchronizePeripherals();
        if (data.Length != ContextSize) throw new ArgumentException(nameof(data)); data.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(data, 0x70950001);
        Cpu.SaveContext(data.Slice(4, CPU.Hitachi.SH2.Sh2<CpuBus>.ContextSize));
        int offset = 140;
        foreach (var bytes in new[] { _io, _cache, _valid, _lru }) { bytes.CopyTo(data[offset..]); offset += bytes.Length; }
        foreach (uint tag in _tags) { BinaryPrimitives.WriteUInt32LittleEndian(data[offset..], tag); offset += 4; }
        int[] words = [_ocra, _ocrb, _frtRemainder, _wdtRemainder, _dmaBudget, _externalLevel, _externalVector, Dreq[0] ? 1 : 0, Dreq[1] ? 1 : 0];
        foreach (int word in words) { BinaryPrimitives.WriteInt32LittleEndian(data[offset..], word); offset += 4; }
        BinaryPrimitives.WriteUInt64LittleEndian(data[offset..], Ticks); BinaryPrimitives.WriteUInt64LittleEndian(data[(offset + 8)..], _serialReady);
    }
    public static bool CanLoadContext(ReadOnlySpan<byte> data) => data.Length == ContextSize && BinaryPrimitives.ReadUInt32LittleEndian(data) == 0x70950001
        && CPU.Hitachi.SH2.Sh2<CpuBus>.CanLoadContext(data.Slice(4, CPU.Hitachi.SH2.Sh2<CpuBus>.ContextSize));
    public void LoadContext(ReadOnlySpan<byte> data)
    {
        if (!CanLoadContext(data)) throw new ArgumentException(nameof(data));
        ResetCodeTracking();
        Array.Clear(_codeVersions);
        _fetchMapVersion++;
        Cpu.LoadContext(data.Slice(4, CPU.Hitachi.SH2.Sh2<CpuBus>.ContextSize)); int offset = 140;
        foreach (var bytes in new[] { _io, _cache, _valid, _lru }) { data.Slice(offset, bytes.Length).CopyTo(bytes); offset += bytes.Length; }
        for (int i = 0; i < _tags.Length; i++) { _tags[i] = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]); offset += 4; }
        int[] words = new int[9]; for (int i = 0; i < words.Length; i++) { words[i] = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]); offset += 4; }
        _ocra = (ushort)words[0]; _ocrb = (ushort)words[1]; _frtRemainder = words[2]; _wdtRemainder = words[3]; _dmaBudget = words[4];
        _externalLevel = words[5]; _externalVector = words[6]; _dreq[0] = words[7] != 0; _dreq[1] = words[8] != 0;
        Ticks = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]); _serialReady = BinaryPrimitives.ReadUInt64LittleEndian(data[(offset + 8)..]); _peripheralTicks = Ticks;
        _inDma = false; UpdateInterrupt(); SchedulePeripheralEvent(); Cpu.SynchronizeBusClock();
    }
}


