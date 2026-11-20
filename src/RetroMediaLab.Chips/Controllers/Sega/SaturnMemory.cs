// Translated from the Saturn memory routines in Yabause memory.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using System.IO;
using RetroMediaLab.Chips.Audio.Yamaha.SCSP;
using RetroMediaLab.Chips.Controllers.Sega.CD;
using RetroMediaLab.Chips.CPU.Hitachi.SH1;
using RetroMediaLab.Chips.CPU.Hitachi.SH2;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;
using static RetroMediaLab.Chips.Controllers.Sega.SaturnMemoryAccess;

namespace RetroMediaLab.Chips.Controllers.Sega;

public sealed class SaturnMemory
{
    private readonly byte[] _biosRom = new byte[0x80000];
    private readonly byte[] _highWram = new byte[0x100000];
    private readonly byte[] _lowWram = new byte[0x100000];
    internal Span<byte> DmaHighWram => _highWram;
    internal Span<byte> DmaLowWram => _lowWram;
    private readonly byte[] _backupRam = new byte[0x10000];
    private readonly byte[] _sh1Rom = new byte[0x10000];
    private readonly byte[] _sh1Dram = new byte[0x80000];
    private readonly byte[] _sh1MpegRom = new byte[0x80000];

    internal ReadOnlySpan<byte> Sh1Rom => _sh1Rom;
    internal Span<byte> Sh1Dram => _sh1Dram;
    internal ReadOnlySpan<byte> Sh1MpegRom => _sh1MpegRom;

    public bool BackupRamWritten { get; set; }

    public void ClearWorkRam()
    {
        Array.Clear(_highWram);
        Array.Clear(_lowWram);
    }

    public byte HighWramMemoryReadByte(uint address) => T2ReadByte(_highWram, address & 0xFFFFF);
    public ushort HighWramMemoryReadWord(uint address) => T2ReadWord(_highWram, address & 0xFFFFF);
    public uint HighWramMemoryReadLong(uint address) => T2ReadLong(_highWram, address & 0xFFFFF);

    public void HighWramMemoryWriteByte(uint address, byte value) => T2WriteByte(_highWram, address & 0xFFFFF, value);
    public void HighWramMemoryWriteWord(uint address, ushort value) => T2WriteWord(_highWram, address & 0xFFFFF, value);
    public void HighWramMemoryWriteLong(uint address, uint value) => T2WriteLong(_highWram, address & 0xFFFFF, value);

    public byte LowWramMemoryReadByte(uint address) => T2ReadByte(_lowWram, address & 0xFFFFF);
    public ushort LowWramMemoryReadWord(uint address) => T2ReadWord(_lowWram, address & 0xFFFFF);
    public uint LowWramMemoryReadLong(uint address) => T2ReadLong(_lowWram, address & 0xFFFFF);

    public void LowWramMemoryWriteByte(uint address, byte value) => T2WriteByte(_lowWram, address & 0xFFFFF, value);
    public void LowWramMemoryWriteWord(uint address, ushort value) => T2WriteWord(_lowWram, address & 0xFFFFF, value);
    public void LowWramMemoryWriteLong(uint address, uint value) => T2WriteLong(_lowWram, address & 0xFFFFF, value);

    public byte BiosRomMemoryReadByte(uint address) => T2ReadByte(_biosRom, address & 0x7FFFF);
    public ushort BiosRomMemoryReadWord(uint address) => T2ReadWord(_biosRom, address & 0x7FFFF);
    public uint BiosRomMemoryReadLong(uint address) => T2ReadLong(_biosRom, address & 0x7FFFF);

    public void BiosRomMemoryWriteByte(uint address, byte value) { }
    public void BiosRomMemoryWriteWord(uint address, ushort value) { }
    public void BiosRomMemoryWriteLong(uint address, uint value) { }

    public byte BupRamMemoryReadByte(uint address) => T1ReadByte(_backupRam, address & 0xFFFF);

    public ushort BupRamMemoryReadWord(uint address) => 0;

    public uint BupRamMemoryReadLong(uint address) => 0;

    public void BupRamMemoryWriteByte(uint address, byte value)
    {
        T1WriteByte(_backupRam, (address & 0xFFFF) | 1, value);
        BackupRamWritten = true;
    }

    public void BupRamMemoryWriteWord(uint address, ushort value) { }

    public void BupRamMemoryWriteLong(uint address, uint value) { }

    // Each CPU owns the six tables populated by MappedMemoryInit.
    public readonly record struct MemoryHandlers(
        Func<uint, byte> ReadByte,
        Action<uint, byte> WriteByte,
        Func<uint, ushort> ReadWord,
        Action<uint, ushort> WriteWord,
        Func<uint, uint> ReadLong,
        Action<uint, uint> WriteLong);

    public sealed record Peripherals(
        SaturnSmpc Smpc,
        SaturnCdBlock Cs2,
        SaturnYgr? Ygr,
        MemoryHandlers SoundRam,
        MemoryHandlers Scsp,
        SaturnVdp1 Vdp1,
        SaturnVdp2 Vdp2,
        SaturnScuInterpreter Scu,
        SaturnScsp? SoundChip = null);

    private static readonly MemoryHandlers Unhandled = new(
        static _ => 0, static (_, _) => { },
        static _ => 0, static (_, _) => { },
        static _ => 0, static (_, _) => { });

    public void MappedMemoryInit<TSh2>(
        Sh2MemoryMap master, TSh2 masterContext,
        Sh2MemoryMap slave, TSh2 slaveContext,
        SaturnCartridge<TSh2> cartridge, Peripherals peripherals,
        Sh2MemoryMap? sh1)
    {
        MapSh2(master, masterContext);
        MapSh2(slave, slaveContext);
        master.DmaSlaveCore = slave.Core;
        Func<uint, int, bool> checkABusWait = peripherals.Ygr is SaturnYgr activeYgr
            ? activeYgr.CheckABusWait
            : static (_, _) => false;
        master.Initialize(master, checkABusWait);
        slave.Initialize(master, checkABusWait);

        if (peripherals.Ygr is not null)
        {
            sh1!.FillMemoryArea(0x000, 0xFFF, Unhandled);
            sh1.Initialize(master, checkABusWait);
        }

        void MapSh2(Sh2MemoryMap space, TSh2 sh)
        {
            space.InstructionMemory = this;
            space.InstructionVdp1 = peripherals.Vdp1;
            space.DmaPeripherals = peripherals;
            space.FillMemoryArea(0x000, 0xFFF, Unhandled);
            space.FillMemoryArea(0x000, 0x00F, new MemoryHandlers(
                BiosRomMemoryReadByte, BiosRomMemoryWriteByte, BiosRomMemoryReadWord, BiosRomMemoryWriteWord, BiosRomMemoryReadLong, BiosRomMemoryWriteLong));
            SaturnSmpc smpc = peripherals.Smpc;
            space.FillMemoryArea(0x010, 0x017, new MemoryHandlers(
                smpc.ReadByte, smpc.WriteByte, smpc.ReadWord, smpc.WriteWord, smpc.ReadLong, smpc.WriteLong));
            space.FillMemoryArea(0x018, 0x01F, new MemoryHandlers(
                BupRamMemoryReadByte, BupRamMemoryWriteByte, BupRamMemoryReadWord, BupRamMemoryWriteWord, BupRamMemoryReadLong, BupRamMemoryWriteLong));
            space.FillMemoryArea(0x020, 0x02F, new MemoryHandlers(
                LowWramMemoryReadByte, LowWramMemoryWriteByte, LowWramMemoryReadWord, LowWramMemoryWriteWord, LowWramMemoryReadLong, LowWramMemoryWriteLong));
            space.FillMemoryArea(0x100, 0x17F, Unhandled with { WriteWord = slave.InputCaptureWriteWord });
            space.FillMemoryArea(0x180, 0x1FF, Unhandled with { WriteWord = master.InputCaptureWriteWord });
            var cs0 = new MemoryHandlers(
                    address => cartridge.ReadCs0Byte(sh, address),
                    (address, value) => cartridge.WriteCs0Byte(sh, address, value),
                    address => cartridge.ReadCs0Word(sh, address),
                    (address, value) => cartridge.WriteCs0Word(sh, address, value),
                    address => cartridge.ReadCs0Long(sh, address),
                    (address, value) => cartridge.WriteCs0Long(sh, address, value));
            space.FillMemoryArea(0x200, 0x3FF, cs0);
            space.ReadCartridgeInstruction = cs0.ReadWord;
            space.DmaCartridgeCs0 = cs0;

            var cs1 = new MemoryHandlers(
                    address => cartridge.ReadCs1Byte(sh, address),
                    (address, value) => cartridge.WriteCs1Byte(sh, address, value),
                    address => cartridge.ReadCs1Word(sh, address),
                    (address, value) => cartridge.WriteCs1Word(sh, address, value),
                    address => cartridge.ReadCs1Long(sh, address),
                    (address, value) => cartridge.WriteCs1Long(sh, address, value));
            space.FillMemoryArea(0x400, 0x4FF, cs1);
            space.DmaCartridgeCs1 = cs1;

            SaturnCdBlock cs2 = peripherals.Cs2;
            Func<uint, byte> readCs2Byte = address => cartridge.ReadCs2Byte(sh, address);
            Action<uint, byte> writeCs2Byte = (address, value) => cartridge.WriteCs2Byte(sh, address, value);
            space.DmaReadCs2Byte = readCs2Byte;
            space.DmaWriteCs2Byte = writeCs2Byte;
            if (peripherals.Ygr is SaturnYgr ygr)
            {
                space.FillMemoryArea(0x580, 0x58F, new MemoryHandlers(
                    readCs2Byte, writeCs2Byte,
                    ygr.ABusReadWord, ygr.ABusWriteWord, ygr.ABusReadLong, ygr.ABusWriteLong));
            }
            else
            {
                space.FillMemoryArea(0x580, 0x58F, new MemoryHandlers(
                    address => cs2.ReadByte(cartridge, sh, address),
                    (address, value) => cs2.WriteByte(cartridge, sh, address, value),
                    cs2.ReadWord, cs2.WriteWord, cs2.ReadLong, cs2.WriteLong));
            }

            space.FillMemoryArea(0x5A0, 0x5AF, peripherals.SoundRam);
            space.FillMemoryArea(0x5B0, 0x5BF, peripherals.Scsp);
            SaturnVdp1 vdp1 = peripherals.Vdp1;
            space.FillMemoryArea(0x5C0, 0x5C7, new MemoryHandlers(
                vdp1.RamReadByte, vdp1.RamWriteByte,
                vdp1.RamReadWord, vdp1.RamWriteWord,
                vdp1.RamReadLong, vdp1.RamWriteLong));
            space.FillMemoryArea(0x5C8, 0x5CF, new MemoryHandlers(
                vdp1.FrameBufferReadByte, vdp1.FrameBufferWriteByte,
                vdp1.FrameBufferReadWord, vdp1.FrameBufferWriteWord,
                vdp1.FrameBufferReadLong, vdp1.FrameBufferWriteLong));
            space.FillMemoryArea(0x5D0, 0x5D7, new MemoryHandlers(
                vdp1.ReadByte, vdp1.WriteByte, vdp1.ReadWord, vdp1.WriteWord, vdp1.ReadLong, vdp1.WriteLong));
            SaturnVdp2 vdp2 = peripherals.Vdp2;
            space.FillMemoryArea(0x5E0, 0x5EF, new MemoryHandlers(
                vdp2.RamReadByte, vdp2.RamWriteByte,
                vdp2.RamReadWord, vdp2.RamWriteWord,
                vdp2.RamReadLong, vdp2.RamWriteLong));
            space.FillMemoryArea(0x5F0, 0x5F7, new MemoryHandlers(
                vdp2.ColorRamReadByte, vdp2.ColorRamWriteByte,
                vdp2.ColorRamReadWord, vdp2.ColorRamWriteWord,
                vdp2.ColorRamReadLong, vdp2.ColorRamWriteLong));
            space.FillMemoryArea(0x5F8, 0x5FB, new MemoryHandlers(
                vdp2.ReadByte, vdp2.WriteByte, vdp2.ReadWord, vdp2.WriteWord, vdp2.ReadLong, vdp2.WriteLong));
            SaturnScuInterpreter scu = peripherals.Scu;
            space.FillMemoryArea(0x5FE, 0x5FE, new MemoryHandlers(
                scu.ReadByte, scu.WriteByte, scu.ReadWord, scu.WriteWord, scu.ReadLong, scu.WriteLong));
            space.FillMemoryArea(0x600, 0x7FF, new MemoryHandlers(
                HighWramMemoryReadByte, HighWramMemoryWriteByte, HighWramMemoryReadWord, HighWramMemoryWriteWord, HighWramMemoryReadLong, HighWramMemoryWriteLong));
        }
    }

    public sealed class Sh2MemoryMap(
        SaturnSh2Core core,
        Action<int> addCycles,
        Action<int, int> sendInterrupt,
        Action<uint, uint> writeNotify) : ISh2Bus
    {

        public Sh2MemoryMap(SaturnSh2Core core, Action<uint, uint> writeNotify)
            : this(core, cycles => core.State.Cycles = unchecked(core.State.Cycles + (uint)cycles),
                core.SendInterrupt, writeNotify)
        {
        }

        public Sh2MemoryMap(SaturnSh2Core core, Action<uint, uint> writeNotify, SaturnSh7034 sh1)
            : this(core, writeNotify)
        {
            Sh1 = sh1;
        }

        private SaturnSh2Cache? _cache;
        private SaturnBios? _bios;
        private bool _biosIsSlave;
        internal SaturnSh7034? Sh1 { get; init; }
        internal SaturnMemory InstructionMemory { get; set; } = null!;
        internal SaturnVdp1 InstructionVdp1 { get; set; } = null!;
        internal Peripherals? DmaPeripherals { get; set; }
        internal SaturnSh2Core DmaSlaveCore { get; set; } = null!;
        internal MemoryHandlers DmaCartridgeCs0 { get; set; }
        internal MemoryHandlers DmaCartridgeCs1 { get; set; }
        internal Func<uint, byte> DmaReadCs2Byte { get; set; } = null!;
        internal Action<uint, byte> DmaWriteCs2Byte { get; set; } = null!;
        internal Func<uint, ushort> ReadCartridgeInstruction { get; set; } = null!;
        internal ushort ReadDataArrayInstruction(uint address) => core.DataArrayReadWord(address);

        private Sh2MemoryMap _masterMemory = null!;
        private Func<uint, int, bool> _checkABusWait = null!;
        internal SaturnSh2Core Core => core;
        internal SaturnSh2Core MasterCore => _masterMemory.Core;
        private readonly Func<uint, byte>[] _readByteList = new Func<uint, byte>[0x1000];
        private readonly Action<uint, byte>[] _writeByteList = new Action<uint, byte>[0x1000];
        private readonly Func<uint, ushort>[] _readWordList = new Func<uint, ushort>[0x1000];
        private readonly Action<uint, ushort>[] _writeWordList = new Action<uint, ushort>[0x1000];
        private readonly Func<uint, uint>[] _readLongList = new Func<uint, uint>[0x1000];
        private readonly Action<uint, uint>[] _writeLongList = new Action<uint, uint>[0x1000];

        internal void Initialize(Sh2MemoryMap master, Func<uint, int, bool> checkABusWait)
        {
            _masterMemory = master;
            _checkABusWait = checkABusWait;
            if (Sh1 is null && core.CacheEmulationEnabled)
                _cache = new SaturnSh2Cache(core, this, master, addCycles);
        }

        internal bool CheckWait(uint address, int size) =>
            SaturnSh2Interpreter.CheckWait(address, size, _checkABusWait);

        internal bool CacheEmulationEnabled => core.CacheEmulationEnabled;

        public void SetEmulatedBios(SaturnBios bios, bool isSlave)
        {
            _bios = bios;
            _biosIsSlave = isSlave;
        }

        internal bool HandleBiosFunction(SaturnSh2State state) =>
            _bios?.HandleFunction(state, this, _biosIsSlave) == true;

        public void ExecuteSh2Peripherals(uint cycles)
        {
            core.FrtExec(cycles);
            core.WdtExec(cycles);
            core.Sh2DmaExec(cycles, _masterMemory, _checkABusWait, sendInterrupt, writeNotify);
        }

        internal void InputCaptureWriteWord(uint address, ushort value) =>
            core.InputCaptureWriteWord();

        public byte Read8(uint address) =>
            Sh1 is { } sh1 ? sh1.Read8(address) :
            _cache is null ? MappedMemoryReadByteNocache(address) : _cache.CacheMemoryReadByte(address);

        public ushort Read16(uint address) =>
            Sh1 is { } sh1 ? sh1.Read16(address) :
            _cache is null ? MappedMemoryReadWordNocache(address) : _cache.CacheMemoryReadWord(address);

        public uint Read32(uint address) =>
            Sh1 is { } sh1 ? sh1.Read32(address) :
            _cache is null ? MappedMemoryReadLongNocache(address) : _cache.CacheMemoryReadLong(address);

        public void Write8(uint address, byte value)
        {
            if (Sh1 is { } sh1)
                sh1.Write8(address, value);
            else if (_cache is null)
                MappedMemoryWriteByteNocache(address, value);
            else
                _cache.CacheMemoryWriteByte(address, value);
        }

        public void Write16(uint address, ushort value)
        {
            if (Sh1 is { } sh1)
                sh1.Write16(address, value);
            else if (_cache is null)
                MappedMemoryWriteWordNocache(address, value);
            else
                _cache.CacheMemoryWriteWord(address, value);
        }

        public void Write32(uint address, uint value)
        {
            if (Sh1 is { } sh1)
                sh1.Write32(address, value);
            else if (_cache is null)
                MappedMemoryWriteLongNocache(address, value);
            else
                _cache.CacheMemoryWriteLong(address, value);
        }

        internal void FillMemoryArea(int start, int end, MemoryHandlers handlers)
        {
            for (int index = start; index <= end; index++)
            {
                _readByteList[index] = handlers.ReadByte;
                _readWordList[index] = handlers.ReadWord;
                _readLongList[index] = handlers.ReadLong;
                _writeByteList[index] = handlers.WriteByte;
                _writeWordList[index] = handlers.WriteWord;
                _writeLongList[index] = handlers.WriteLong;
            }
        }

        public byte MappedMemoryReadByteNocache(uint address)
        {
            switch (address >> 29)
            {
                case 0:
                case 1:
                case 5:
                    return _readByteList[(address >> 16) & 0xFFF](address);
                case 4:
                case 6:
                    return core.DataArrayReadByte(address);
                case 7:
                    if (address >= 0xFFFFFE00)
                        return core.OnChipReadByte(address & 0x1FF);
                    break;
                default:
                    return Unhandled.ReadByte(address);
            }

            return 0;
        }

        public void MappedMemoryWriteByteNocache(uint address, byte value)
        {
            switch (address >> 29)
            {
                case 0:
                case 1:
                case 5:
                    _writeByteList[(address >> 16) & 0xFFF](address, value);
                    return;
                case 4:
                case 6:
                    core.DataArrayWriteByte(address, value);
                    return;
                case 7:
                    if (address >= 0xFFFFFE00)
                        core.OnChipWriteByte(address & 0x1FF, value);
                    return;
                default:
                    Unhandled.WriteByte(address, value);
                    return;
            }
        }

        public ushort MappedMemoryReadWordNocache(uint address)
        {
            switch (address >> 29)
            {
                case 0:
                case 1:
                case 5:
                    return _readWordList[(address >> 16) & 0xFFF](address);
                case 4:
                case 6:
                    return core.DataArrayReadWord(address);
                case 7:
                    if (address >= 0xFFFFFE00)
                        return core.OnChipReadWord(address & 0x1FF);
                    break;
                default:
                    return Unhandled.ReadWord(address);
            }

            return 0;
        }

        public void MappedMemoryWriteWordNocache(uint address, ushort value)
        {
            switch (address >> 29)
            {
                case 0:
                case 1:
                case 5:
                    _writeWordList[(address >> 16) & 0xFFF](address, value);
                    return;
                case 4:
                case 6:
                    core.DataArrayWriteWord(address, value);
                    return;
                case 7:
                    if (address >= 0xFFFFFE00)
                        core.OnChipWriteWord(address & 0x1FF, value);
                    return;
                default:
                    Unhandled.WriteWord(address, value);
                    return;
            }
        }

        public uint MappedMemoryReadLongNocache(uint address)
        {
            switch (address >> 29)
            {
                case 0:
                case 1:
                case 5:
                    return _readLongList[(address >> 16) & 0xFFF](address);
                case 3:
                    return core.AddressArrayReadLong(address);
                case 4:
                case 6:
                    return core.DataArrayReadLong(address);
                case 7:
                    if (address >= 0xFFFFFE00)
                        return core.OnChipReadLong(address & 0x1FF);
                    break;
                default:
                    return Unhandled.ReadLong(address);
            }

            return 0;
        }

        public void MappedMemoryWriteLongNocache(uint address, uint value)
        {
            switch (address >> 29)
            {
                case 0:
                case 1:
                case 5:
                    _writeLongList[(address >> 16) & 0xFFF](address, value);
                    return;
                case 2:
                    return;
                case 3:
                    core.AddressArrayWriteLong(address, value);
                    return;
                case 4:
                case 6:
                    core.DataArrayWriteLong(address, value);
                    return;
                case 7:
                    if (address >= 0xFFFFFE00)
                        core.OnChipWriteLong(address & 0x1FF, value, this, sendInterrupt, writeNotify);
                    return;
                default:
                    Unhandled.WriteLong(address, value);
                    return;
            }
        }
    }

    public bool LoadBios(Stream? image) => T123Load(_biosRom, 2, image);

    public bool LoadSh1Rom(Stream? image) => T123Load(_sh1Rom, 2, image);

    public bool LoadMpegRom(Stream? image) => T123Load(_sh1MpegRom, 2, image);

    public bool LoadBackupRam(Stream? image) => T123Load(_backupRam, 1, image);

    public bool SaveBackupRam(Stream output) => T123Save(_backupRam, 1, output);

    public void FormatBackupRam() => FormatBackupRam(_backupRam);

    internal static void FormatBackupRam(Span<byte> memory)
    {
        ReadOnlySpan<byte> header =
        [
            0xFF, (byte)'B', 0xFF, (byte)'a', 0xFF, (byte)'c', 0xFF, (byte)'k',
            0xFF, (byte)'U', 0xFF, (byte)'p', 0xFF, (byte)'R', 0xFF, (byte)'a',
            0xFF, (byte)'m', 0xFF, (byte)' ', 0xFF, (byte)'F', 0xFF, (byte)'o',
            0xFF, (byte)'r', 0xFF, (byte)'m', 0xFF, (byte)'a', 0xFF, (byte)'t'
        ];

        for (uint block = 0; block < 4; block++)
            for (uint index = 0; index < 32; index++)
                T1WriteByte(memory, block * 32 + index, header[(int)index]);

        for (uint index = 0x80; index < memory.Length; index += 2)
        {
            T1WriteByte(memory, index, 0xFF);
            T1WriteByte(memory, index + 1, 0);
        }
    }
}
