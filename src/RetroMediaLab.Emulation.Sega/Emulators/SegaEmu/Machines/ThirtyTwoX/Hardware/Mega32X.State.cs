using System.Buffers.Binary;
using RetroMediaLab.Chips.Controllers.Hitachi.SH7095;
using RetroMediaLab.Chips.Video.Sega.VDP32X;
using RetroMediaLab.Chips.Audio.Sega.PWM32X;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.ThirtyTwoX.Hardware;

internal sealed partial class Mega32X
{
    public const int ContextSize = 256 + 0x40000 + 256 + 2 * Sh7095.ContextSize + Vdp32X.ContextSize + Pwm32X.ContextSize;
    public void SaveContext(Span<byte> data)
    {
        if (data.Length != ContextSize) throw new ArgumentException(nameof(data)); data.Clear();
        uint[] words = [0x32ad0002, _control, _dreqControl, _source, _destination, _dmaLength, (uint)_romBank, (uint)_fifoHead, (uint)_fifoCount,
            (uint)_hintPeriod, (uint)_hintCounter, unchecked((uint)_lastPhysicalLine), _mask[0], _mask[1], _pending[0], _pending[1], _booted ? 1u : 0u];
        for (int i = 0; i < words.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(data[(i * 4)..], words[i]);
        BinaryPrimitives.WriteUInt64LittleEndian(data[80..], _epoch); BinaryPrimitives.WriteUInt64LittleEndian(data[88..], _lastMaster); BinaryPrimitives.WriteUInt64LittleEndian(data[96..], _cpuOriginMaster);
        for (int i = 0; i < 8; i++) { BinaryPrimitives.WriteUInt16LittleEndian(data[(112 + i * 2)..], _communication[i]); BinaryPrimitives.WriteUInt16LittleEndian(data[(128 + i * 2)..], _fifo[i]); }
        for (int cpu = 0; cpu < 2; cpu++)
        {
            Span<byte> record = data.Slice(144 + cpu * 32, 32);
            BinaryPrimitives.WriteUInt32LittleEndian(record, _pollAddress[cpu]); BinaryPrimitives.WriteUInt32LittleEndian(record[4..], _pollPc[cpu]);
            BinaryPrimitives.WriteUInt32LittleEndian(record[8..], _pollValue[cpu]); BinaryPrimitives.WriteInt32LittleEndian(record[12..], _pollCount[cpu]);
            BinaryPrimitives.WriteUInt64LittleEndian(record[16..], _pollCycle[cpu]); record[24] = _polling[cpu] ? (byte)1 : (byte)0;
            BinaryPrimitives.WriteUInt32LittleEndian(record[28..], _pollEpoch[cpu]);
        }
        int offset = 256; _sdram.CopyTo(data[offset..]); offset += _sdram.Length; _bios68k.CopyTo(data[offset..]); offset += _bios68k.Length;
        Master.SaveContext(data.Slice(offset, Sh7095.ContextSize)); offset += Sh7095.ContextSize; Slave.SaveContext(data.Slice(offset, Sh7095.ContextSize)); offset += Sh7095.ContextSize;
        Video.SaveContext(data.Slice(offset, Vdp32X.ContextSize)); offset += Vdp32X.ContextSize; Pwm.SaveContext(data.Slice(offset, Pwm32X.ContextSize));
    }
    public static bool CanLoadContext(ReadOnlySpan<byte> data)
    {
        if (data.Length != ContextSize || BinaryPrimitives.ReadUInt32LittleEndian(data) != 0x32ad0002
            || BinaryPrimitives.ReadUInt32LittleEndian(data[24..]) > 3 || BinaryPrimitives.ReadUInt32LittleEndian(data[28..]) > 7
            || BinaryPrimitives.ReadUInt32LittleEndian(data[32..]) > 8) return false;
        for (int cpu = 0; cpu < 2; cpu++) if (BinaryPrimitives.ReadUInt32LittleEndian(data[(156 + cpu * 32)..]) > 9 || data[168 + cpu * 32] > 1) return false;
        int offset = 256 + 0x40000 + 256;
        bool valid = Sh7095.CanLoadContext(data.Slice(offset, Sh7095.ContextSize)); offset += Sh7095.ContextSize;
        valid &= Sh7095.CanLoadContext(data.Slice(offset, Sh7095.ContextSize)); offset += Sh7095.ContextSize;
        valid &= Vdp32X.CanLoadContext(data.Slice(offset, Vdp32X.ContextSize)); offset += Vdp32X.ContextSize;
        return valid && Pwm32X.CanLoadContext(data.Slice(offset, Pwm32X.ContextSize));
    }
    public void LoadContext(ReadOnlySpan<byte> data)
    {
        if (!CanLoadContext(data)) throw new ArgumentException(nameof(data)); uint[] w = new uint[17];
        for (int i = 0; i < w.Length; i++) w[i] = BinaryPrimitives.ReadUInt32LittleEndian(data[(i * 4)..]);
        _control = (ushort)w[1]; _dreqControl = (ushort)w[2]; _source = w[3]; _destination = w[4]; _dmaLength = (ushort)w[5]; _romBank = (int)w[6];
        _fifoHead = (int)w[7]; _fifoCount = (int)w[8]; _hintPeriod = (int)w[9]; _hintCounter = (int)w[10]; _lastPhysicalLine = unchecked((int)w[11]);
        _mask[0] = (byte)w[12]; _mask[1] = (byte)w[13]; _pending[0] = (byte)w[14]; _pending[1] = (byte)w[15]; _booted = w[16] != 0;
        _epoch = BinaryPrimitives.ReadUInt64LittleEndian(data[80..]); _lastMaster = BinaryPrimitives.ReadUInt64LittleEndian(data[88..]); _cpuOriginMaster = BinaryPrimitives.ReadUInt64LittleEndian(data[96..]);
        for (int i = 0; i < 8; i++) { _communication[i] = BinaryPrimitives.ReadUInt16LittleEndian(data[(112 + i * 2)..]); _fifo[i] = BinaryPrimitives.ReadUInt16LittleEndian(data[(128 + i * 2)..]); }
        for (int cpu = 0; cpu < 2; cpu++)
        {
            ReadOnlySpan<byte> record = data.Slice(144 + cpu * 32, 32);
            _pollAddress[cpu] = BinaryPrimitives.ReadUInt32LittleEndian(record); _pollPc[cpu] = BinaryPrimitives.ReadUInt32LittleEndian(record[4..]);
            _pollValue[cpu] = BinaryPrimitives.ReadUInt32LittleEndian(record[8..]); _pollCount[cpu] = BinaryPrimitives.ReadInt32LittleEndian(record[12..]);
            _pollCycle[cpu] = BinaryPrimitives.ReadUInt64LittleEndian(record[16..]); _polling[cpu] = record[24] != 0;
            _pollEpoch[cpu] = BinaryPrimitives.ReadUInt32LittleEndian(record[28..]);
        }
        int offset = 256; data.Slice(offset, _sdram.Length).CopyTo(_sdram); _sdramEpoch++; offset += _sdram.Length; data.Slice(offset, _bios68k.Length).CopyTo(_bios68k); offset += _bios68k.Length;
        Master.LoadContext(data.Slice(offset, Sh7095.ContextSize)); offset += Sh7095.ContextSize; Slave.LoadContext(data.Slice(offset, Sh7095.ContextSize)); offset += Sh7095.ContextSize;
        Array.Clear(_sdramCodeWords);
        Video.LoadContext(data.Slice(offset, Vdp32X.ContextSize)); offset += Vdp32X.ContextSize; Pwm.LoadContext(data.Slice(offset, Pwm32X.ContextSize));
        _syncing = false; Array.Clear(_running); UpdateInterrupts();
    }
}
