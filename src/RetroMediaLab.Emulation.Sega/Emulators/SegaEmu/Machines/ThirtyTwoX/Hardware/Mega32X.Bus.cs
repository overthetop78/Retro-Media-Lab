using System.Buffers.Binary;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.ThirtyTwoX.Hardware;

internal sealed partial class Mega32X
{
    public bool Handles68k(uint address) => (_cartridgePresent && (address < 0x100 || address is >= 0x880000 and <= 0x9fffff)) || address is >= 0x840000 and <= 0x87ffff
        || address is >= 0xa15100 and <= 0xa153ff || address is >= 0xa130ec and <= 0xa130ef;
    public byte Read68k8(uint address) => (byte)(Read68k16(address & ~1u) >> ((1 - (int)(address & 1)) * 8));
    public ushort Read68k16(uint address)
    {
        Synchronize(_mainCycles());
        if (address is >= 0xa130ec and <= 0xa130ef) return address == 0xa130ec ? (ushort)0x4d41 : (ushort)0x5253;
        if (address is >= 0xa15100 and <= 0xa1513f) return ReadRegisters((int)(address & 63), -1);
        if (address is >= 0xa15180 and <= 0xa1518b) return Video.Sh2Access ? (ushort)0xffff : Video.ReadRegister((int)address);
        if (address is >= 0xa15200 and <= 0xa153ff) return Video.Sh2Access ? (ushort)0xffff : Video.ReadPalette(address);
        if (!Enabled) return (ushort)((_rom(address) << 8) | _rom(address + 1));
        if (address < 0x100) return BinaryPrimitives.ReadUInt16BigEndian(_bios68k.AsSpan((int)(address & 254), 2));
        if (address is >= 0x840000 and <= 0x87ffff) return Video.Sh2Access ? (ushort)0xffff : Video.ReadFrame16(address);
        if (address is >= 0x880000 and <= 0x8fffff) address &= 0x7ffff;
        else if (address is >= 0x900000 and <= 0x9fffff) address = (uint)(_romBank * 0x100000) | (address & 0xfffff);
        return _romWord(address);
    }
    public void Write68k8(uint address, byte value)
    {
        Synchronize(_mainCycles());
        if (Enabled && address is >= 0x840000 and <= 0x87ffff) { if (!Video.Sh2Access) Video.WriteFrame8(address, value); return; }
        ushort mask = (ushort)((address & 1) == 0 ? 0xff00 : 255);
        Write68kWord(address & ~1u, (ushort)(value << ((1 - (int)(address & 1)) * 8)), mask);
    }
    public void Write68k16(uint address, ushort value) { Synchronize(_mainCycles()); Write68kWord(address, value, 0xffff); }
    private void Write68kWord(uint address, ushort value, ushort mask)
    {
        if (address is >= 0xa15100 and <= 0xa1513f) { WriteRegisters((int)(address & 63), value, mask, -1); return; }
        if (!Enabled) return;
        if (address < 0x100) { if (address is 0x70 or 0x72) { ushort old = BinaryPrimitives.ReadUInt16BigEndian(_bios68k.AsSpan((int)address, 2)); BinaryPrimitives.WriteUInt16BigEndian(_bios68k.AsSpan((int)address, 2), (ushort)((old & ~mask) | (value & mask))); } return; }
        if (Video.Sh2Access) return;
        if (address is >= 0x840000 and <= 0x87ffff) Video.WriteFrame16(address, value);
        else if (address is >= 0xa15180 and <= 0xa1518b) Video.WriteRegister((int)address, (ushort)((Video.ReadRegister((int)address) & ~mask) | (value & mask)));
        else if (address is >= 0xa15200 and <= 0xa153ff) Video.WritePalette(address, (ushort)((Video.ReadPalette(address) & ~mask) | (value & mask)));
    }
    private sealed class Sh2Bus(Mega32X owner, int cpu) : ISh2Bus
    {
        public long CodeVersion(uint address) => address < 0x4000 || address is >= 0x02000000 and < 0x04000000 ? 0
            : address is >= 0x06000000 and < 0x06040000 ? owner._sdramVersions[(address & 0x3ffff) >> 4] : -1;
        public ulong CodeEpoch(uint address) => address < 0x4000 || address is >= 0x02000000 and < 0x04000000 ? 1
            : address is >= 0x06000000 and < 0x06040000 ? owner._sdramEpoch + 1 : 0;
        public void MarkCode(uint address, int length)
        {
            address &= 0x1fffffff;
            if (length <= 0 || address is < 0x06000000 or >= 0x06040000) return;
            // PicoDrive's sh2_sdram_checks consults translated-code markers
            // before invoking the SMC write barrier. Track exact instruction words.
            int offset = (int)(address & 0x3ffff);
            int first = offset >> 1, last = (offset + Math.Min(length, 0x40000 - offset) - 1) >> 1;
            for (int word = first; word <= last; word++)
                owner._sdramCodeWords[word >> 5] |= 1u << (word & 31);
        }
        public int MaximumWaitStates => 6;
        public int WaitStates(uint address, bool write) => address switch { >= 0x02000000 and < 0x04000000 => 6, >= 0x04000000 and < 0x06000000 => write ? 0 : 4, >= 0x06000000 and < 0x06040000 => write ? 0 : 2, _ => 0 };
        public byte Read8(uint address)
        {
            if (address < 0x4000) { var rom = cpu == 0 ? owner._masterRom : owner._slaveRom; return rom[address % (uint)rom.Length]; }
            if (address is >= 0x02000000 and < 0x04000000) return owner._rom(address & 0x3fffff);
            if (address is >= 0x04000000 and < 0x06000000) return owner.Video.Sh2Access ? owner.Video.ReadFrame8(address) : (byte)0xff;
            if (address is >= 0x06000000 and < 0x06040000) return owner._sdram[address & 0x3ffff];
            return (byte)(Read16(address & ~1u) >> ((1 - (int)(address & 1)) * 8));
        }
        public ushort Fetch16(uint address) => Read16Core(address, false);
        public bool TryMapFetch(uint address, out ReadOnlyMemory<byte> memory, out uint start, out int waitStates)
        {
            if (address is >= 0x06000000 and < 0x06040000)
            { memory = owner._sdram; start = 0x06000000; waitStates = 2; return true; }
            if (address < 0x4000)
            {
                byte[] rom = cpu == 0 ? owner._masterRom : owner._slaveRom;
                memory = rom; start = address & ~((uint)rom.Length - 1); waitStates = 0; return true;
            }
            if (address is >= 0x02000000 and < 0x04000000 && !owner._cartridgeView.IsEmpty)
            { memory = owner._cartridgeView; start = address & ~0x3fffffu; waitStates = 6; return true; }
            memory = default; start = 0; waitStates = 0; return false;
        }
        public uint Fetch32(uint address)
        {
            if (address is >= 0x06000000 and < 0x06040000)
                return BinaryPrimitives.ReadUInt32BigEndian(owner._sdram.AsSpan((int)(address & 0x3fffc), 4));
            return (uint)Read16Core(address, false) << 16 | Read16Core(address + 2, false);
        }
        public ushort Read16(uint address) => Read16Core(address, true);
        private ushort Read16Core(uint address, bool poll)
        {
            if (address < 0x4000) { var rom = cpu == 0 ? owner._masterRom : owner._slaveRom; return BinaryPrimitives.ReadUInt16BigEndian(rom.AsSpan((int)(address % (uint)rom.Length & ~1u), 2)); }
            if (address is >= 0x02000000 and < 0x04000000) return owner._romWord(address & 0x3ffffe);
            if (address is >= 0x06000000 and < 0x06040000)
            {
                ushort value = BinaryPrimitives.ReadUInt16BigEndian(owner._sdram.AsSpan((int)(address & 0x3fffe), 2));
                return poll ? (ushort)owner.PollRead(cpu, address, value, 7) : value;
            }
            if (address is >= 0x4000 and <= 0x403f)
            {
                owner.SynchronizeOther(cpu); ushort value = owner.ReadRegisters((int)(address & 63), cpu);
                return poll && (address == 0x4004 || address is >= 0x4020 and <= 0x402f) ? (ushort)owner.PollRead(cpu, address, value, 9) : value;
            }
            if (address is >= 0x4100 and <= 0x410b)
            {
                ushort value = owner.Video.Sh2Access ? owner.Video.ReadRegister((int)address) : (ushort)0xffff;
                return poll ? (ushort)owner.PollRead(cpu, address, value, 9) : value;
            }
            if (address is >= 0x4200 and <= 0x43ff) return owner.Video.Sh2Access ? owner.Video.ReadPalette(address) : (ushort)0xffff;
            if (address is >= 0x4000 and < 0x02000000 || address >= 0x06040000) return 0;
            return (ushort)((Read8(address) << 8) | Read8(address + 1));
        }
        public uint Read32(uint address)
        {
            if (address is >= 0x06000000 and < 0x06040000)
            {
                uint value = BinaryPrimitives.ReadUInt32BigEndian(owner._sdram.AsSpan((int)(address & 0x3fffc), 4));
                return owner.PollRead(cpu, address, value, 7);
            }
            uint result = (uint)Read16Core(address, false) << 16 | Read16Core(address + 2, false);
            return address is >= 0x4020 and <= 0x402b ? owner.PollRead(cpu, address, result, 9) : result;
        }
        public void Write8(uint address, byte value)
        {
            if (address is >= 0x06000000 and < 0x06040000)
            {
                int offset = (int)(address & 0x3ffff), word = offset >> 1;
                if ((owner._sdramCodeWords[word >> 5] & (1u << (word & 31))) != 0 && owner._sdram[offset] != value)
                    owner._sdramEpoch++;
                owner._sdramVersions[(address & 0x3ffff) >> 4]++; owner.PollWrite(address); owner._sdram[offset] = value; return;
            }
            if (address is >= 0x04000000 and < 0x06000000) { if (owner.Video.Sh2Access) owner.Video.WriteFrame8(address, value); return; }
            ushort mask = (ushort)((address & 1) == 0 ? 0xff00 : 255);
            ushort data = (ushort)(value << ((1 - (int)(address & 1)) * 8));
            if (address is >= 0x4000 and <= 0x403f) owner.WriteRegisters((int)(address & 62), data, mask, cpu);
            else if (owner.Video.Sh2Access && address is >= 0x4100 and <= 0x410b) owner.Video.WriteRegister((int)address, (ushort)((owner.Video.ReadRegister((int)address) & ~mask) | data));
            else if (owner.Video.Sh2Access && address is >= 0x4200 and <= 0x43ff) owner.Video.WritePalette(address, (ushort)((owner.Video.ReadPalette(address) & ~mask) | data));
        }
        public void Write16(uint address, ushort value)
        {
            owner.PollWrite(address);
            if (address is >= 0x4000 and <= 0x403f) owner.WriteRegisters((int)(address & 63), value, 0xffff, cpu);
            else if (address is >= 0x4100 and <= 0x410b) { if (owner.Video.Sh2Access) owner.Video.WriteRegister((int)address, value); }
            else if (address is >= 0x4200 and <= 0x43ff) { if (owner.Video.Sh2Access) owner.Video.WritePalette(address, value); }
            else if (address is >= 0x04000000 and < 0x06000000) { if (owner.Video.Sh2Access) owner.Video.WriteFrame16(address, value); }
            else if (address is >= 0x06000000 and < 0x06040000)
            {
                int offset = (int)(address & 0x3fffe), word = offset >> 1;
                if ((owner._sdramCodeWords[word >> 5] & (1u << (word & 31))) != 0
                    && BinaryPrimitives.ReadUInt16BigEndian(owner._sdram.AsSpan(offset, 2)) != value)
                    owner._sdramEpoch++;
                owner._sdramVersions[(address & 0x3ffff) >> 4]++;
                BinaryPrimitives.WriteUInt16BigEndian(owner._sdram.AsSpan(offset, 2), value);
            }
        }
        public void Write32(uint address, uint value) { Write16(address, (ushort)(value >> 16)); Write16(address + 2, (ushort)value); }
    }
}

