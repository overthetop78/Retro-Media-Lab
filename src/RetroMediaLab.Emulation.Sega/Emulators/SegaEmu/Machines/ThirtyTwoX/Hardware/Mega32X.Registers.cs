namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.ThirtyTwoX.Hardware;

internal sealed partial class Mega32X
{
    private uint Timestamp(int cpu)
    {
        if (cpu < 0) return _mainCycles();
        ulong master = _cpuOriginMaster + _cpus[cpu]!.Cpu.Cycles * 7 / 3;
        return master >= _epoch ? (uint)(master - _epoch) : 0;
    }
    private ushort ReadRegisters(int offset, int cpu)
    {
        if (offset >= 0x30) return Pwm.Read(offset - 0x30, Timestamp(cpu));
        if (offset >= 0x20) return _communication[(offset >> 1) & 7];
        return offset switch
        {
            0 => cpu < 0 ? (ushort)((_control & 0x83) | (Video.Sh2Access ? 0x8000 : 0))
                : (ushort)((Enabled ? 0x200 : 0) | (_cartridgePresent ? 0 : 0x100) | (Video.Sh2Access ? 0x8000 : 0) | _mask[cpu]),
            2 => (ushort)(((_pending[0] & 2) != 0 ? 1 : 0) | ((_pending[1] & 2) != 0 ? 2 : 0)),
            4 => (ushort)(cpu < 0 ? _romBank : _hintPeriod),
            6 => (ushort)((_dreqControl & 7) | (_fifoCount == 8 ? (cpu < 0 ? 0x80 : 0x8000) : 0) | (cpu >= 0 && _fifoCount == 0 ? 0x4000 : 0)),
            8 => (ushort)(_source >> 16), 10 => (ushort)_source, 12 => (ushort)(_destination >> 16), 14 => (ushort)_destination,
            16 => _dmaLength, 18 => cpu >= 0 ? PopFifo() : (ushort)0, _ => 0
        };
    }
    private ushort PopFifo()
    {
        if (_fifoCount == 0) return 0;
        ushort value = _fifo[_fifoHead]; _fifoHead = (_fifoHead + 1) & 7; _fifoCount--; UpdateInterrupts(); return value;
    }
    private void WriteRegisters(int offset, ushort value, ushort mask, int cpu)
    {
        PollWrite((uint)(0x4000 + offset));
        if (cpu >= 0) SynchronizeOther(cpu);
        ushort Merge(ushort previous) => (ushort)((previous & ~mask) | (value & mask));
        if (offset >= 0x30) { ushort previous = offset <= 0x32 ? Pwm.Read(offset - 0x30, Timestamp(cpu)) : (ushort)0; Pwm.Write(offset - 0x30, Merge(previous), Timestamp(cpu)); UpdateInterrupts(); return; }
        if (offset >= 0x20) { _communication[offset >> 1 & 7] = Merge(_communication[offset >> 1 & 7]); return; }
        if (offset == 0)
        {
            if ((mask & 0xff00) != 0) Video.Sh2Access = (value & 0x8000) != 0;
            if (cpu < 0)
            {
                bool oldRelease = Released, oldEnabled = Enabled; _control = (ushort)(Merge(_control) & 0x83);
                if (oldEnabled != Enabled) CodeMappingChanged?.Invoke();
                if (Enabled && Released && (!_booted || !oldRelease || !oldEnabled)) StartProcessors();
                if (!Released) _booted = false;
            }
            else if ((mask & 255) != 0) { _mask[cpu] = (byte)(value & 0x8f); _mask[1 - cpu] = (byte)((_mask[1 - cpu] & 15) | (value & 0x80)); }
        }
        else if (offset == 2 && cpu < 0)
        {
            // PicoDrive p32x_reg_write8(0x03) / p32x_update_cmd_irq:
            // the 68000 controls both assertion and deassertion of each CMD line.
            ushort merged = Merge(ReadRegisters(offset, cpu));
            for (int c = 0; c < 2; c++)
                _pending[c] = (byte)((_pending[c] & ~2) | ((merged & (1 << c)) != 0 ? 2 : 0));
        }
        else if (offset == 4)
        {
            if (cpu < 0)
            {
                int next = Merge((ushort)_romBank) & 3;
                if (next != _romBank) { _romBank = next; CodeMappingChanged?.Invoke(); }
            }
            else { _hintPeriod = Merge((ushort)_hintPeriod) & 255; _hintCounter = 0; }
        }
        else if (offset == 6 && cpu < 0) { _dreqControl = (ushort)(Merge(_dreqControl) & 7); if ((_dreqControl & 4) == 0) _fifoHead = _fifoCount = 0; }
        else if (offset == 8) _source = (_source & 65535) | (uint)(Merge((ushort)(_source >> 16)) & 255) << 16;
        else if (offset == 10) _source = (_source & 0xff0000) | (uint)(Merge((ushort)_source) & 65534);
        else if (offset == 12) _destination = (_destination & 65535) | (uint)(Merge((ushort)(_destination >> 16)) & 255) << 16;
        else if (offset == 14) _destination = (_destination & 0xff0000) | (uint)(Merge((ushort)_destination) & 65534);
        else if (offset == 16) _dmaLength = Merge(_dmaLength);
        else if (offset == 18 && cpu < 0 && _fifoCount < 8) { _fifo[(_fifoHead + _fifoCount++) & 7] = value; if (_dmaLength > 0) _dmaLength--; }
        else if (cpu >= 0 && offset is >= 0x14 and <= 0x1c)
        {
            int bit = offset switch { 0x14 => 16, 0x16 => 8, 0x18 => 4, 0x1a => 2, _ => 1 };
            _pending[cpu] &= (byte)~bit; if (bit == 1) Pwm.Acknowledge();
        }
        UpdateInterrupts();
    }
}
