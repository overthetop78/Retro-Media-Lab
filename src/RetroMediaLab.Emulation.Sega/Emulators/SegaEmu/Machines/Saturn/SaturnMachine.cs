// Translated from the Saturn timing and frame execution routines in Yabause yabause.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Buffers.Binary;
using System.IO;
using RetroMediaLab.Chips.Audio.Yamaha.SCSP;
using RetroMediaLab.Chips.Controllers.Sega;
using RetroMediaLab.Chips.Controllers.Sega.CD;
using RetroMediaLab.Chips.CPU.Hitachi.SH1;
using RetroMediaLab.Chips.CPU.Hitachi.SH2;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Saturn;

internal sealed class SaturnMachine : IDisposable
{
    internal sealed record MemoryConnections(
        SaturnMemory Memory,
        SaturnCartridge<SaturnSh2Core> Cartridge);

    internal sealed record CdInitialization(
        SaturnCartridge<SaturnSh2Core>.CartridgeType CartridgeType,
        int CoreId,
        string? ImagePath,
        string? MpegPath,
        string? ModemIp,
        string? ModemPort,
        Action<string> ReportInitializationError,
        Func<string?, string?, int> InitializeNetlink,
        Action DeinitializeNetlink,
        Func<string?, string?, int> InitializeJapaneseModem,
        Action DeinitializeJapaneseModem,
        Action<uint> ExecuteNetlink,
        Action<uint> ExecuteJapaneseModem);

    internal sealed record Connections(
        Action<uint, uint> NotifySh2Write,
        CdInitialization Cd,
        SaturnMpegCard MpegCard,
        SaturnScsp.RegisterConnections Scsp,
        SaturnPeripheral Peripheral,
        Action MarkFramePolled,
        Action DoMovie,
        Func<int> FrameCounter,
        Func<bool> MovieActive,
        Action ApplyCheats,
        Action FlushBackups,
        Action FlushTsunami);

    private const int TimingBits = 20;
    private const uint TimingMask = (1u << TimingBits) - 1;
    private const int ScspFractionalBits = 20;

    private readonly Connections _connections;
    private readonly SaturnMemory _memory;
    private readonly SaturnSh2Core _master;
    private readonly SaturnSh2Core _slave;
    private readonly SaturnMemory.Sh2MemoryMap _masterMemory;
    private readonly SaturnMemory.Sh2MemoryMap _slaveMemory;
    private readonly SaturnSh2Core? _sh1;
    private readonly SaturnSh7034? _sh1Onchip;
    private readonly SaturnMemory.Sh2MemoryMap? _sh1Memory;
    private readonly SaturnCdDrive? _cdDrive;
    private readonly SaturnBios? _biosEmulator;
    private readonly bool _emulateBios;
    private readonly bool _useCdBlockLle;
    private readonly bool _useNewScsp;
    private readonly bool _useScsp2;
    private readonly bool _useQuickLoad;
    private bool _slaveRunning;
    private int _decilineCount;
    private int _maxLineCount;
    private int _sh2FrequencyType;
    private uint _decilineStop;
    private uint _decilineUsec;
    private uint _sh2CycleFrac;
    private uint _usecFrac;
    private uint _savedCenticycles;
    private uint _savedM68kCycles;
    private uint _savedScspCycles;
    private uint _savedSh1Cycles;
    private uint _savedCddCycles;

    public SaturnMachine(
        Connections connections,
        MemoryConnections memory,
        SaturnSmpcState.Region region,
        bool clockSync,
        uint baseTime,
        bool? pal,
        Stream? bios,
        bool useSh2Cache,
        bool useSh2DmaTiming,
        bool useCdBlockLle,
        bool useScuDmaTiming,
        bool useNewScsp,
        bool useThreads = false,
        int numThreads = 1,
        Stream? backupRam = null,
        Stream? sh1Rom = null,
        bool useQuickLoad = false,
        bool useScsp2 = false)
    {
        _connections = connections;
        _memory = memory.Memory;
        _emulateBios = bios is null;
        _useCdBlockLle = useCdBlockLle;
        _useNewScsp = useNewScsp;
        _useScsp2 = useScsp2;
        _useQuickLoad = useQuickLoad;
        _master = new SaturnSh2Core(useCdBlockLle || useSh2Cache, useCdBlockLle || useSh2DmaTiming);
        _slave = new SaturnSh2Core(useCdBlockLle || useSh2Cache, useCdBlockLle || useSh2DmaTiming, isSlave: true);
        _masterMemory = new SaturnMemory.Sh2MemoryMap(_master, connections.NotifySh2Write);
        _slaveMemory = new SaturnMemory.Sh2MemoryMap(_slave, connections.NotifySh2Write);
        if (!_memory.LoadBackupRam(backupRam))
            _memory.FormatBackupRam();
        _memory.BackupRamWritten = false;

        Scu = new SaturnScuInterpreter(
            _masterMemory, connections.NotifySh2Write, _master.SendInterrupt,
            useCdBlockLle || useScuDmaTiming);

        CdInitialization cd = connections.Cd;
        var drive = new SaturnCdImageDrive();
        CdBlock = new SaturnCdBlock(new SaturnCdBlock.Connections(
            [SaturnCdImageDrive.DummyCd, drive.IsoCd],
            SaturnCdImageDrive.DummyCd,
            cd.ReportInitializationError, RecheckCdRegion,
            cd.InitializeNetlink, cd.DeinitializeNetlink,
            cd.InitializeJapaneseModem, cd.DeinitializeJapaneseModem,
            cd.ExecuteNetlink, cd.ExecuteJapaneseModem, ReceiveCdda, () => Scsp?.ClearCdda(), Scu.SetCdInterruptLine));
        bool initialized = false;
        try
        {
            SaturnYgr? ygr = null;
            if (_useCdBlockLle)
            {
                if (sh1Rom is null || !_memory.LoadSh1Rom(sh1Rom))
                    throw new InvalidDataException("Failed to load the Saturn SH-1 ROM.");

                if (!string.IsNullOrEmpty(cd.MpegPath))
                {
                    using Stream mpegRom = File.OpenRead(cd.MpegPath);
                    if (!_memory.LoadMpegRom(mpegRom))
                        throw new InvalidDataException("Failed to load the Saturn MPEG ROM.");
                }

                _sh1 = new SaturnSh2Core(cacheEmulationEnabled: true, dmaTimingEnabled: false);
                ygr = new SaturnYgr(connections.MpegCard.ReadWord, connections.MpegCard.WriteWord,
                    () => _sh1Onchip!.Iprb, _sh1.SendInterrupt);
                _sh1Onchip = new SaturnSh7034(_memory, ygr, _sh1.SendInterrupt);
                connections.MpegCard.ConnectSh1(_sh1Onchip);
                _sh1Memory = new SaturnMemory.Sh2MemoryMap(_sh1, connections.NotifySh2Write, _sh1Onchip);
                _cdDrive = new SaturnCdDrive(CdBlock, _sh1Onchip, _memory, ygr, ReceiveCdda);
            }

            if (CdBlock.Initialize((int)cd.CartridgeType, cd.CoreId, cd.ImagePath, cd.MpegPath, cd.ModemIp, cd.ModemPort) != 0)
                throw new InvalidOperationException("Failed to initialize the Saturn CD block.");

            Scsp = new SaturnScsp(connections.Scsp with
            {
                SendMainInterrupt = Scu.SendSoundRequest,
                SetCdAudioPlaying = CdBlock.SetAudioPlaying,
                SetCdTiming = CdBlock.SetTiming,
            }, useNewScsp, useScsp2, useThreads);
            if (useScsp2)
                Scsp.SetFrameAccurate(true);

            Video = new SaturnVideoSoftware();
            Vdp1 = new SaturnVdp1(Video, Scu.SendDrawEnd);
            Vdp2 = new SaturnVdp2(new SaturnVdp2.Connections(
                () => LineCount, Video, Vdp1,
                Scu.SendVBlankIn, Scu.SendHBlankIn, Scu.SendVBlankOut,
                () => _slaveRunning, _slave.SendInterrupt));

            Smpc = new SaturnSmpc(new SaturnSmpc.Connections(
                () => (SaturnSmpcState.Region)CdBlock.GetRegionId(),
                () => CdBlock.GetInitialProgram(false), StartSlave,
                StopSlave, Scsp.StartSoundCpu, Scsp.StopSoundCpu,
                Vdp1.Reset, Vdp2.Reset, ResetScu, Scsp.Reset,
                ChangeTiming, _master.Nmi, _master.SendInterrupt,
                Scu.SendSystemManager, connections.FrameCounter, connections.MovieActive,
                connections.Peripheral.Port1, connections.Peripheral.Port2,
                () => SaturnPeripheral.Flush(connections.Peripheral.Port1),
                () => SaturnPeripheral.Flush(connections.Peripheral.Port2),
                connections.MarkFramePolled), region, clockSync, baseTime);

            _memory.MappedMemoryInit(
                _masterMemory, _master, _slaveMemory, _slave,
                memory.Cartridge, new SaturnMemory.Peripherals(
                    Smpc, CdBlock, ygr,
                    new SaturnMemory.MemoryHandlers(
                        Scsp.SoundRamReadByte, Scsp.SoundRamWriteByte,
                        Scsp.SoundRamReadWord, Scsp.SoundRamWriteWord,
                        Scsp.SoundRamReadLong, Scsp.SoundRamWriteLong),
                    new SaturnMemory.MemoryHandlers(
                        Scsp.ReadByte, Scsp.WriteByte, Scsp.ReadWord, Scsp.WriteWord, Scsp.ReadLong, Scsp.WriteLong),
                    Vdp1, Vdp2, Scu, Scsp),
                _sh1Memory);
            SetVideoFormat(pal ?? (((byte)region & 0x08) != 0));
            ChangeTiming(0);
            if (bios is not null && !_memory.LoadBios(bios))
                throw new InvalidDataException("Failed to load the Saturn BIOS.");
            if (_emulateBios && _useCdBlockLle)
                throw new InvalidOperationException("The Saturn CD block LLE requires a real BIOS.");

            if (_emulateBios)
            {
                _biosEmulator = new SaturnBios(_memory, memory.Cartridge, Smpc, connections.FlushBackups);
                _masterMemory.SetEmulatedBios(_biosEmulator, isSlave: false);
                _slaveMemory.SetEmulatedBios(_biosEmulator, isSlave: true);
            }

            Reset();
            if (pal is null)
            {
                bool detectedPal = ((byte)Smpc.RegionId & 0x08) != 0;
                if (IsPal != detectedPal)
                    SetVideoFormat(detectedPal);
            }

            if (useThreads)
            {
                int count = Math.Max(numThreads, 1);
                Video.SetVdp1ThreadEnable(count != 1);
                Video.SetNumLayerThreads(count);
                Video.SetNumPriorityThreads(count);
            }
            else
            {
                Video.SetVdp1ThreadEnable(false);
                Video.SetNumLayerThreads(0);
                Video.SetNumPriorityThreads(0);
            }
            initialized = true;
        }
        finally
        {
            if (!initialized)
                Dispose();
        }
    }

    public void Dispose()
    {
        try
        {
            CdBlock.Dispose();
        }
        finally
        {
            try
            {
                Scsp?.Dispose();
            }
            finally
            {
                Video?.Dispose();
            }
        }
    }

    public SaturnCdBlock CdBlock { get; }
    public SaturnScuInterpreter Scu { get; }
    public SaturnScsp Scsp { get; }
    public SaturnVideoSoftware Video { get; }
    public SaturnVdp1 Vdp1 { get; }
    public SaturnVdp2 Vdp2 { get; }
    public SaturnSmpc Smpc { get; }
    public int LineCount { get; private set; }
    public bool IsPal { get; private set; }
    public bool DecilineMode { get; set; } = true;
    public bool PlayingSsf { get; set; }

    public void ChangeDisc(string? imagePath)
    {
        if (CdBlock.ChangeMedia(imagePath) != 0)
            throw new InvalidDataException("Failed to change the Saturn CD drive.");

        if (imagePath is not null && _emulateBios)
        {
            CdBlock.GetInitialProgram(false);
            if (!QuickLoadGame())
                throw new InvalidDataException("Failed to load the Saturn game with the emulated BIOS.");
        }
    }

    public bool BackupRamWritten => _memory.BackupRamWritten;

    public void SaveBackupRam(Stream output)
    {
        if (!_memory.SaveBackupRam(output))
            throw new IOException("Failed to save Saturn backup RAM.");

        _memory.BackupRamWritten = false;
    }

    private void RecheckCdRegion() => Smpc?.RecheckRegion();

    private void ReceiveCdda(ReadOnlySpan<byte> sector) => Scsp.ReceiveCdda(sector);

    public void Reset()
    {
        PlayingSsf = false;
        ResetNoLoad();
        if ((_useQuickLoad || _emulateBios) && !QuickLoadGame())
        {
            if (_emulateBios)
                throw new InvalidDataException("Failed to load the Saturn game with the emulated BIOS.");
            ResetNoLoad();
        }
    }

    private void SpeedySetup()
    {
        if (_biosEmulator is { } biosEmulator)
            biosEmulator.Initialize(_masterMemory);
        else
        {
            for (uint i = 0; i < 0x210; i += 4)
                _masterMemory.MappedMemoryWriteLongNocache(0x06000000 + i,
                    _masterMemory.MappedMemoryReadLongNocache(0x00000600 + i));
            for (uint i = 0; i < 0x8E0; i += 4)
                _masterMemory.MappedMemoryWriteLongNocache(0x06000220 + i,
                    _masterMemory.MappedMemoryReadLongNocache(0x00000820 + i));
            for (uint i = 0; i < 0x700; i += 4)
                _masterMemory.MappedMemoryWriteLongNocache(0x06001100 + i,
                    _masterMemory.MappedMemoryReadLongNocache(0x00001100 + i));

            _masterMemory.MappedMemoryWriteLongNocache(0x06000234, 0x000002AC);
            _masterMemory.MappedMemoryWriteLongNocache(0x06000238, 0x000002BC);
            _masterMemory.MappedMemoryWriteLongNocache(0x0600023C, 0x00000350);
            _masterMemory.MappedMemoryWriteLongNocache(0x06000240, 0x32524459);
            _masterMemory.MappedMemoryWriteLongNocache(0x0600024C, 0);
            _masterMemory.MappedMemoryWriteLongNocache(0x06000268, _masterMemory.MappedMemoryReadLongNocache(0x00001344));
            _masterMemory.MappedMemoryWriteLongNocache(0x0600026C, _masterMemory.MappedMemoryReadLongNocache(0x00001348));
            _masterMemory.MappedMemoryWriteLongNocache(0x0600029C, _masterMemory.MappedMemoryReadLongNocache(0x00001354));
            _masterMemory.MappedMemoryWriteLongNocache(0x060002C4, _masterMemory.MappedMemoryReadLongNocache(0x00001104));
            _masterMemory.MappedMemoryWriteLongNocache(0x060002C8, _masterMemory.MappedMemoryReadLongNocache(0x00001108));
            _masterMemory.MappedMemoryWriteLongNocache(0x060002CC, _masterMemory.MappedMemoryReadLongNocache(0x0000110C));
            _masterMemory.MappedMemoryWriteLongNocache(0x060002D0, _masterMemory.MappedMemoryReadLongNocache(0x00001110));
            _masterMemory.MappedMemoryWriteLongNocache(0x060002D4, _masterMemory.MappedMemoryReadLongNocache(0x00001114));
            _masterMemory.MappedMemoryWriteLongNocache(0x060002D8, _masterMemory.MappedMemoryReadLongNocache(0x00001118));
            _masterMemory.MappedMemoryWriteLongNocache(0x060002DC, _masterMemory.MappedMemoryReadLongNocache(0x0000111C));
            _masterMemory.MappedMemoryWriteLongNocache(0x06000328, 0x000004C8);
            _masterMemory.MappedMemoryWriteLongNocache(0x0600032C, 0x00001800);
            for (uint i = 0; i < 0x80; i += 4)
                _masterMemory.MappedMemoryWriteLongNocache(0x06000A00 + i, 0x0600083C);
        }

        CdBlock.SetupQuickLoad();
        SaturnSh2State registers = _master.State;
        for (int i = 0; i < 15; i++)
            registers.R[i] = 0;
        registers.R[15] = 0x06002000;
        registers.SR.All = 0;
        registers.GBR = 0;
        registers.VBR = 0x06000000;
        registers.MACH = 0;
        registers.MACL = 0;
        registers.PR = 0;

        Scu.SetupQuickLoad();
        Smpc.SetupQuickLoad();

        ref SaturnVdp1Registers vdp1 = ref Vdp1.Registers;
        vdp1.EDSR = 3;
        vdp1.LocalX = 160;
        vdp1.LocalY = 112;
        vdp1.SystemClipX2 = 319;
        vdp1.SystemClipY2 = 223;

        ref SaturnVdp2Registers vdp2 = ref Vdp2.Registers;
        vdp2 = default;
        vdp2.TVMD = 0x8000;
        vdp2.TVSTAT = 0x020A;
        vdp2.CYCA0L = 0x0F44;
        vdp2.CYCA0U = 0xFFFF;
        vdp2.CYCA1L = 0xFFFF;
        vdp2.CYCA1U = 0xFFFF;
        vdp2.CYCB0L = 0xFFFF;
        vdp2.CYCB0U = 0xFFFF;
        vdp2.CYCB1L = 0xFFFF;
        vdp2.CYCB1U = 0xFFFF;
        vdp2.BGON = 0x0001;
        vdp2.PNCN0 = 0x8000;
        vdp2.MPABN0 = 0x0303;
        vdp2.MPCDN0 = 0x0303;
        vdp2.ZMXN0.All = 0x00010000;
        vdp2.ZMYN0.All = 0x00010000;
        vdp2.ZMXN1.All = 0x00010000;
        vdp2.ZMYN1.All = 0x00010000;
        vdp2.BKTAL = 0x4000;
        vdp2.SPCTL = 0x0020;
        vdp2.PRINA = 0x0007;
        vdp2.CLOFEN = 0x0001;
        vdp2.COAR = 0x0200;
        vdp2.COAG = 0x0200;
        vdp2.COAB = 0x0200;
    }

    private bool QuickLoadGame()
    {
        CdBlock.SelectQuickLoadPartition();
        if (!CdBlock.ReadQuickLoadSector(150, out ReadOnlySpan<byte> buffer))
            return false;

        uint size;
        uint address;
        try
        {
            SpeedySetup();
            if (!buffer[..15].SequenceEqual("SEGA SEGASATURN"u8))
                return false;
            size = BinaryPrimitives.ReadUInt32BigEndian(buffer[0xE0..]);
            address = BinaryPrimitives.ReadUInt32BigEndian(buffer[0xF0..]);
        }
        finally
        {
            CdBlock.ReleaseQuickLoadSector();
        }

        uint blocks = (size >> 11) + ((size & 0x7FF) != 0 ? 1u : 0u);
        for (uint i = 0; i < blocks; i++)
        {
            if (!CdBlock.ReadQuickLoadSector(150 + i, out buffer))
                return false;
            try
            {
                uint length = Math.Min(size, 2048u);
                for (uint j = 0; j < length; j++)
                    _masterMemory.MappedMemoryWriteByteNocache(0x06002000 + i * 0x800 + j, buffer[(int)j]);
                size = unchecked(size - 2048);
            }
            finally
            {
                CdBlock.ReleaseQuickLoadSector();
            }
        }
        _connections.NotifySh2Write(0x06002000, blocks << 11);

        if (!CdBlock.ReadQuickLoadSector(166, out buffer))
            return false;
        uint lba;
        try
        {
            if (SaturnCdBlock.ReadQuickLoadDirectoryRecord(buffer[0x9C..], out lba, out _) == 0)
                return false;
        }
        finally
        {
            CdBlock.ReleaseQuickLoadSector();
        }

        if (!CdBlock.ReadQuickLoadSector(lba + 150, out buffer))
            return false;
        try
        {
            for (int i = 0; i < 3; i++)
            {
                int length = SaturnCdBlock.ReadQuickLoadDirectoryRecord(buffer, out lba, out size);
                if (length == 0)
                    return false;
                buffer = buffer[length..];
            }
        }
        finally
        {
            CdBlock.ReleaseQuickLoadSector();
        }

        blocks = (size >> 11) + ((size & 0x7FF) != 0 ? 1u : 0u);
        for (uint i = 0; i < blocks; i++)
        {
            if (!CdBlock.ReadQuickLoadSector(150 + lba + i, out buffer))
                return false;
            try
            {
                uint length = Math.Min(size, 2048u);
                for (uint j = 0; j < length; j++)
                    _masterMemory.MappedMemoryWriteByteNocache(address + i * 0x800 + j, buffer[(int)j]);
                size = unchecked(size - 2048);
            }
            finally
            {
                CdBlock.ReleaseQuickLoadSector();
            }
        }
        _connections.NotifySh2Write(address, blocks << 11);

        _master.State.PC = 0x06002E00;
        _master.State.R[15] = CdBlock.GetMasterStackAddress();
        return true;
    }

    public void ResetNoLoad()
    {
        _master.Reset();
        StopSlave();
        _memory.ClearWorkRam();

        _sh1?.Reset();
        CdBlock.Reset();
        ResetScu();
        Scsp.Reset();
        Vdp1.Reset();
        Vdp2.Reset();
        Smpc.Reset();

        _master.PowerOn(_masterMemory);
        if (_useCdBlockLle)
        {
            _sh1Onchip!.Initialize();
            _cdDrive!.Reset();
            _connections.MpegCard.Initialize();
            _sh1!.PowerOn(_sh1Memory!);
        }
    }

    private void ResetScu()
    {
        Scu.Reset();
        CdBlock.UpdateInterruptLine();
    }

    public void ResetButton() => Smpc.ResetButton();

    private void StartSlave()
    {
        if (_emulateBios)
        {
            _slaveMemory.Write32(0xFFFFFFE0, 0xA55A03F1);
            _slaveMemory.Write32(0xFFFFFFE4, 0xA55A00FC);
            _slaveMemory.Write32(0xFFFFFFE8, 0xA55A5555);
            _slaveMemory.Write32(0xFFFFFFEC, 0xA55A0070);

            _slaveMemory.Write16(0xFFFFFEE0, 0x0000);
            _slaveMemory.Write16(0xFFFFFEE2, 0x0000);
            _slaveMemory.Write16(0xFFFFFE60, 0x0F00);
            _slaveMemory.Write16(0xFFFFFE62, 0x6061);
            _slaveMemory.Write16(0xFFFFFE64, 0x6263);
            _slaveMemory.Write16(0xFFFFFE66, 0x6465);
            _slaveMemory.Write16(0xFFFFFE68, 0x6600);
            _slaveMemory.Write16(0xFFFFFEE4, 0x6869);
            _slaveMemory.Write32(0xFFFFFFA8, 0x0000006C);
            _slaveMemory.Write32(0xFFFFFFA0, 0x0000006D);
            _slaveMemory.Write32(0xFFFFFF0C, 0x0000006E);
            _slaveMemory.Write32(0xFFFFFE10, 0x00000081);

            _slave.State.R[15] = CdBlock.GetSlaveStackAddress();
            _slave.State.VBR = 0x06000400;
            _slave.State.PC = _masterMemory.Read32(0x06000250);
            if (_masterMemory.Read32(0x060002AC) != 0)
                _slave.State.R[15] = _masterMemory.Read32(0x060002AC);
        }
        else
        {
            _slave.PowerOn(_slaveMemory);
        }

        _slaveRunning = true;
    }

    private void StopSlave()
    {
        _slave.Reset();
        _slaveRunning = false;
    }

    internal long ProcessorClockHz => (long)Math.Round(Sh2Frequency(_sh2FrequencyType));

    private double Sh2Frequency(int frequencyType)
    {
        double frequencyBase = IsPal ? 28437500.0 : (39375000.0 / 11.0) * 8.0;
        double frequencyMultiplier = frequencyType == 0 ? 15.0 / 16.0 : 1.0;
        return frequencyBase * frequencyMultiplier;
    }

    private void ChangeTiming(int frequencyType)
    {
        double shiftedFrequency = Sh2Frequency(frequencyType) * (1 << TimingBits);
        double shiftedUsec = 1.0e6 * (1 << TimingBits);
        double decilineTime = IsPal ? 1.0 / 50 / 313 / 10 : 1.0 / (60 / 1.001) / 263 / 10;

        _decilineCount = 0;
        LineCount = 0;
        _sh2FrequencyType = frequencyType;
        _decilineStop = (uint)(shiftedFrequency * decilineTime + 0.5);
        _sh2CycleFrac = 0;
        _decilineUsec = (uint)(shiftedUsec * decilineTime + 0.5);
        _usecFrac = 0;
    }

    private static uint GetCyclesPerLineDivision(uint clock, uint frames, uint lines, uint divisionsPerLine)
        => (uint)(((ulong)(clock / frames) << ScspFractionalBits) / (lines * divisionsPerLine));

    public void ExecuteFrame()
    {
        bool oneFrameExecuted = false;
        uint cyclesIncrement = DecilineMode ? _decilineStop : _decilineStop * 10;
        uint usecIncrement = DecilineMode ? _decilineUsec : _decilineUsec * 10;

        uint m68kCycles = 0;
        uint m68kCenticycles = 0;
        uint m68kCyclesPerDeciline = 0;
        uint scspCyclesPerDeciline = 0;
        uint sh1CyclesPerDeciline = 0;
        uint cddCyclesPerDeciline = 0;
        uint lines = IsPal ? 313u : 263u;
        uint frames = IsPal ? 50u : 60u;

        if (_useCdBlockLle)
        {
            sh1CyclesPerDeciline = GetCyclesPerLineDivision(20_000_000, frames, lines, 10);
            cddCyclesPerDeciline = GetCyclesPerLineDivision(1_000_000, frames, lines, 10);
        }

        if (!_useScsp2 && _useNewScsp)
        {
            scspCyclesPerDeciline = GetCyclesPerLineDivision(44100 * 512, frames, lines, 10);
            m68kCyclesPerDeciline = GetCyclesPerLineDivision(44100 * 256, frames, lines, 10);
        }
        else if (IsPal)
        {
            m68kCycles = DecilineMode ? 72u : 722u;
            m68kCenticycles = DecilineMode ? 20u : 0;
        }
        else
        {
            m68kCycles = DecilineMode ? 71u : 716u;
            m68kCenticycles = DecilineMode ? 62u : 20u;
        }

        _connections.DoMovie();

        while (!oneFrameExecuted)
        {
            if (DecilineMode)
            {
                _sh2CycleFrac += cyclesIncrement;
                uint sh2Cycles = (_sh2CycleFrac >> (TimingBits + 1)) << 1;
                _sh2CycleFrac &= (TimingMask << 1) | 1;

                if (!PlayingSsf)
                {
                    _master.Execute(_masterMemory, sh2Cycles);
                    if (_slaveRunning)
                        _slave.Execute(_slaveMemory, sh2Cycles);
                }
                if (_useScsp2)
                    Scsp.ExecuteNewScsp(1);
                _decilineCount++;
                if (_decilineCount == 9)
                    Vdp2.HBlankIn();

                Scu.Execute(sh2Cycles);
            }
            else
            {
                uint decilineCycles = _decilineStop >> TimingBits;
                _sh2CycleFrac += cyclesIncrement;
                uint sh2Cycles = (_sh2CycleFrac >> (TimingBits + 1)) << 1;
                _sh2CycleFrac &= (TimingMask << 1) | 1;

                if (!PlayingSsf)
                {
                    _master.Execute(_masterMemory, sh2Cycles - decilineCycles);
                    if (_slaveRunning)
                        _slave.Execute(_slaveMemory, sh2Cycles - decilineCycles);
                }
                Vdp2.HBlankIn();
                if (!PlayingSsf)
                {
                    _master.Execute(_masterMemory, decilineCycles);
                    if (_slaveRunning)
                        _slave.Execute(_slaveMemory, decilineCycles);
                }
                if (_useScsp2)
                    Scsp.ExecuteNewScsp(10);
                Scu.Execute(sh2Cycles);
            }

            if (!_useScsp2)
                Scsp.SynchronizeSoundCpu();

            if (!DecilineMode || _decilineCount == 10)
            {
                Vdp2.HBlankOut();
                if (!_useScsp2)
                    Scsp.Execute();
                _decilineCount = 0;
                LineCount++;
                if (LineCount == Vdp2.VBlankLineCount)
                {
                    Smpc.EndIntback();
                    Vdp2.VBlankIn();
                    _connections.ApplyCheats();
                }
                else if (LineCount == _maxLineCount)
                {
                    Vdp2.VBlankOut(Smpc);
                    _connections.MpegCard.SetVideoInterrupt(Vdp2.Registers, Video);
                    LineCount = 0;
                    oneFrameExecuted = true;
                }
            }

            _usecFrac += usecIncrement;
            Smpc.Execute((int)(_usecFrac >> TimingBits), LineCount);
            CdBlock.Execute(_usecFrac >> TimingBits);
            _usecFrac &= TimingMask;

            if (!_useScsp2 && !_useNewScsp)
            {
                uint cycles = m68kCycles;
                _savedCenticycles += m68kCenticycles;
                if (_savedCenticycles >= 100)
                {
                    cycles++;
                    _savedCenticycles -= 100;
                }
                Scsp.ExecuteSoundCpu((int)cycles);
            }
            else if (!_useScsp2)
            {
                _savedM68kCycles += m68kCyclesPerDeciline;
                uint m68kIntegerPart = _savedM68kCycles >> ScspFractionalBits;
                Scsp.ExecuteSoundCpu((int)m68kIntegerPart);
                _savedM68kCycles -= m68kIntegerPart << ScspFractionalBits;

                _savedScspCycles += scspCyclesPerDeciline;
                uint scspIntegerPart = _savedScspCycles >> ScspFractionalBits;
                Scsp.ExecuteNewScspMixer((int)scspIntegerPart);
                _savedScspCycles -= scspIntegerPart << ScspFractionalBits;
            }

            if (_useCdBlockLle)
            {
                _savedSh1Cycles += sh1CyclesPerDeciline;
                uint sh1IntegerPart = _savedSh1Cycles >> ScspFractionalBits;
                _sh1!.ExecuteSh1(_sh1Memory!, sh1IntegerPart, _cdDrive!);
                _savedSh1Cycles -= sh1IntegerPart << ScspFractionalBits;

                _savedCddCycles += cddCyclesPerDeciline;
                uint cddIntegerPart = _savedCddCycles >> ScspFractionalBits;
                _cdDrive!.Execute((int)cddIntegerPart);
                _savedCddCycles -= cddIntegerPart << ScspFractionalBits;
            }
        }

        if (!_useScsp2)
            Scsp.SynchronizeSoundCpu();
        _connections.FlushTsunami();
    }

    public void SetVideoFormat(bool pal)
    {
        IsPal = pal;
        _maxLineCount = pal ? 313 : 263;
        Vdp2.Registers.TVSTAT = (ushort)(Vdp2.Registers.TVSTAT | (pal ? 1 : 0));
        Scsp.ChangeVideoFormat(pal);
        ChangeTiming(_sh2FrequencyType);
    }
}
