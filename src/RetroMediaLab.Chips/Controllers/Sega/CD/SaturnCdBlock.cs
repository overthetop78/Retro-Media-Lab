// Copyright 2003 Guillaume Duhamel
// Copyright 2004-2006 Theo Berkau
// Translated from Yabause cs2.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Buffers.Binary;
using System.IO;
using static RetroMediaLab.Chips.Controllers.Sega.CD.SaturnCdBlockState;
using static RetroMediaLab.Chips.Controllers.Sega.SaturnMemoryAccess;

namespace RetroMediaLab.Chips.Controllers.Sega.CD;

public sealed class SaturnCdBlock : IDisposable
{
    public delegate void CddaReceiver(ReadOnlySpan<byte> sector);

    public sealed record Connections(
        IReadOnlyList<SaturnCdInterface> CdInterfaces,
        SaturnCdInterface DummyCd,
        Action<string> ReportCdInitializationError,
        Action RecheckRegion,
        Func<string?, string?, int> InitializeNetlink,
        Action DeinitializeNetlink,
        Func<string?, string?, int> InitializeJapaneseModem,
        Action DeinitializeJapaneseModem,
        Action<uint> ExecuteNetlink,
        Action<uint> ExecuteJapaneseModem,
        CddaReceiver ReceiveCdda,
        Action ClearCdda,
        Action<bool> SetInterruptLine);

    private const ushort HirqCmok = 0x0001;
    private const ushort HirqDrdy = 0x0002;
    private const ushort HirqCsct = 0x0004;
    private const ushort HirqBful = 0x0008;
    private const ushort HirqPend = 0x0010;
    private const ushort HirqDchg = 0x0020;
    private const ushort HirqEsel = 0x0040;
    private const ushort HirqEhst = 0x0080;
    private const ushort HirqEcpy = 0x0100;
    private const ushort HirqEfls = 0x0200;
    private const ushort HirqScdq = 0x0400;
    private const ushort HirqMped = 0x0800;
    private const ushort HirqMpcm = 0x1000;
    private const ushort HirqMpst = 0x2000;

    private const byte StatusBusy = 0x00;
    private const byte StatusPause = 0x01;
    private const byte StatusStandby = 0x02;
    private const byte StatusPlay = 0x03;
    private const byte StatusSeek = 0x04;
    private const byte StatusScan = 0x05;
    private const byte StatusOpen = 0x06;
    private const byte StatusNoDisc = 0x07;
    private const byte StatusRetry = 0x08;
    private const byte StatusError = 0x09;
    private const byte StatusFatal = 0x0A;
    private const byte StatusPeriodic = 0x20;
    private const byte StatusTransfer = 0x40;
    private const byte StatusWait = 0x80;
    private const byte StatusReject = 0xFF;

    private const int PlayTypeSector = 0x01;
    private const int PlayTypeFile = 0x02;
    private const int TransferInvalid = -1;
    private const int TransferGetSector = 0;
    private const int TransferGetDeleteSector = 2;
    private const int TransferPutSector = 3;
    private const int NetlinkCartridge = 8;
    private const int JapaneseModemCartridge = 10;
    private const uint CdClockHz = 11_289_600;

    private readonly Connections _connections;
    private SaturnCdBlockState? _area;
    private InitialProgram? _initialProgram;
    private uint _lastSubcodeFad;
    private ushort _subcodeGroup;
    private uint _systemResetMicroseconds;
    private uint _driveStartupMicroseconds;
    private uint _initialSeekMicroseconds;
    private uint _initialSeekFad;
    private uint _seekOriginFad;
    private uint _seekPauseFad;
    private bool _seekToPlayback;
    private bool _authenticatingDisc;
    private int _authenticationFilter;
    private bool _fileInfoValid;
    private enum InitialSeekPhase { None, Position, Seek, FirstSector }
    private InitialSeekPhase _initialSeekPhase;
    private enum EjectPhase { None, DiscChanged, FileSystemEnd, Waiting }
    private EjectPhase _ejectPhase;
    private uint _ejectMicroseconds;
    private readonly ushort[] _executingCommandData = new ushort[4];
    private bool _scanReverse;
    private int _scanCounter;
    private uint _scanSector;
    private uint? _playbackPositionFad;
    private uint? _subcodeRelativeFad;
    private SaturnCdBlockState Area => _area!;
    public SaturnCdInterface CdInterface => Area.CdInterface!;
    private uint ReportedFad => _playbackPositionFad ?? Area.Fad;

    public SaturnCdBlock(Connections connections)
    {
        _connections = connections;
    }

    private void SetHirq(ushort interrupt)
    {
        Area.Reg.Hirq |= interrupt;
        UpdateInterruptLine();
    }

    public void UpdateInterruptLine() =>
        _connections.SetInterruptLine(_area is not null && (Area.Reg.Hirq & Area.Reg.HirqMask) != 0);

    private void ReportCdStatus(byte status)
    {
        uint fad = ReportedFad;
        Area.Reg.Cr1 = (ushort)((status << 8) | ((Area.Options & 0xF) << 4) | (Area.RepeatCount & 0xF));
        Area.Reg.Cr2 = (ushort)((Area.ControlAddress << 8) | Area.Track);
        Area.Reg.Cr3 = (ushort)(((uint)Area.Index << 8) | ((fad >> 16) & 0xFF));
        Area.Reg.Cr4 = unchecked((ushort)fad);
    }

    private void ReportMpegStatus(byte status)
    {
        Area.Reg.Cr1 = (ushort)((status << 8) | Area.ActionStatus);
        Area.Reg.Cr2 = Area.VideoCounter;
        Area.Reg.Cr3 = (ushort)((Area.PictureInfo << 8) | Area.MpegAudioStatus);
        Area.Reg.Cr4 = Area.MpegVideoStatus;
    }

    private static bool IsCdRegister(uint register) =>
        register is 0x90008 or 0x9000C or 0x90018 or 0x9001C or 0x90020 or 0x90024 or 0x90028;

    public byte ReadByte<TSh2>(SaturnCartridge<TSh2> cartridge, TSh2 sh, uint address)
    {
        return cartridge.ReadCs2Byte(sh, address);
    }

    public void WriteByte<TSh2>(SaturnCartridge<TSh2> cartridge, TSh2 sh, uint address, byte value)
    {
        cartridge.WriteCs2Byte(sh, address, value);
    }

    public ushort ReadWord(uint address)
    {
        ushort value = 0;
        address &= 0xFFFFF;
        if (_systemResetMicroseconds != 0 && address is >= 0x90018 and <= 0x90026)
            return 0;

        switch (address)
        {
            case 0x90008:
            case 0x9000A:
                value = Area.Reg.Hirq;

                if (Area.BufferFull != 0)
                    value |= HirqBful;
                else
                    value &= unchecked((ushort)~HirqBful);

                if (Area.DiskChanged != 0)
                    value |= HirqDchg;
                else
                    value &= unchecked((ushort)~HirqDchg);

                if (Area.OneSectorStored)
                    value |= HirqCsct;
                else
                    value &= unchecked((ushort)~HirqCsct);

                Area.Reg.Hirq = value;
                UpdateInterruptLine();
                return value;

            case 0x9000C:
            case 0x9000E: return Area.Reg.HirqMask;
            case 0x90018:
            case 0x9001A: return Area.Reg.Cr1;
            case 0x9001C:
            case 0x9001E: return Area.Reg.Cr2;
            case 0x90020:
            case 0x90022: return Area.Reg.Cr3;
            case 0x90024:
            case 0x90026:
                Area.Command = 0;
                return Area.Reg.Cr4;
            case 0x90028:
            case 0x9002A: return Area.Reg.MpegRgb;
            case 0x98000:
                switch (Area.InfoTransferType)
                {
                    case 0:
                        value = Area.TransferCount % 4 == 0
                            ? (ushort)(Area.Toc[Area.TransferCount >> 2] >> 16)
                            : unchecked((ushort)Area.Toc[Area.TransferCount >> 2]);
                        Area.TransferCount += 2;
                        Area.CdWordNumber += 2;
                        if (Area.TransferCount >= 0xCC * 2)
                        {
                            Area.TransferCount = 0;
                            Area.InfoTransferType = -1;
                        }
                        break;

                    case 1:
                        value = (ushort)((Area.TransferFileInfo[Area.TransferCount] << 8) |
                            Area.TransferFileInfo[Area.TransferCount + 1]);
                        Area.TransferCount += 2;
                        Area.CdWordNumber += 2;
                        if (Area.TransferCount >= 0x6 * 2)
                        {
                            Area.TransferCount = 0;
                            Area.InfoTransferType = -1;
                        }
                        break;

                    case 2:
                        if (Area.TransferCount % (0x6 * 2) == 0)
                            SetupFileInfoTransfer(2 + Area.TransferCount / (0x6 * 2));
                        value = (ushort)((Area.TransferFileInfo[Area.TransferCount % (0x6 * 2)] << 8) |
                            Area.TransferFileInfo[Area.TransferCount % (0x6 * 2) + 1]);
                        Area.TransferCount += 2;
                        Area.CdWordNumber += 2;
                        if (Area.TransferCount >= Area.TransferFileInfoByteCount)
                        {
                            Area.TransferCount = 0;
                            Area.InfoTransferType = -1;
                        }
                        break;

                    case 3:
                        value = (ushort)((Area.TransferSubcodeQ[Area.TransferCount] << 8) |
                            Area.TransferSubcodeQ[Area.TransferCount + 1]);
                        Area.TransferCount += 2;
                        Area.CdWordNumber += 2;
                        if (Area.TransferCount >= 5 * 2)
                        {
                            Area.TransferCount = 0;
                            Area.InfoTransferType = -1;
                        }
                        break;

                    case 4:
                        value = (ushort)((Area.TransferSubcodeRw[Area.TransferCount] << 8) |
                            Area.TransferSubcodeRw[Area.TransferCount + 1]);
                        Area.TransferCount += 2;
                        Area.CdWordNumber += 2;
                        if (Area.TransferCount >= 12 * 2)
                        {
                            Area.TransferCount = 0;
                            Area.InfoTransferType = -1;
                        }
                        break;
                }
                break;
        }

        return value;
    }

    public void WriteWord(uint address, ushort value)
    {
        address &= 0xFFFFF;
        switch (address)
        {
            case 0x90008:
            case 0x9000A:
                Area.Reg.Hirq &= value;
                UpdateInterruptLine();
                return;
            case 0x9000C:
            case 0x9000E:
                Area.Reg.HirqMask = value;
                UpdateInterruptLine();
                return;
            case 0x90018:
            case 0x9001A:
                Area.Status &= unchecked((byte)~StatusPeriodic);
                Area.Command = 1;
                Area.Reg.Cr1 = value;
                return;
            case 0x9001C:
            case 0x9001E:
                Area.Reg.Cr2 = value;
                return;
            case 0x90020:
            case 0x90022:
                Area.Reg.Cr3 = value;
                return;
            case 0x90024:
            case 0x90026:
                Area.Reg.Cr4 = value;
                SetCommandTiming((byte)(Area.Reg.Cr1 >> 8));
                return;
            case 0x90028:
            case 0x9002A:
                Area.Reg.MpegRgb = value;
                return;
        }
    }

    public uint ReadLong(uint address)
    {
        address &= 0xFFFFF;
        if (address == 0x18000)
        {
            if (Area.DataTransferType != TransferInvalid)
            {
                if (Area.DataTransferredSectorCount < Area.DataSectorsToTransfer)
                {
                    Block? block = GetTransferBlock();
                    if (block is null || block.Size < 0)
                        return 0;

                    ReadOnlySpan<byte> sector = block.Data.AsSpan(0, block.Size);
                    uint value = BinaryPrimitives.ReadUInt32BigEndian(sector[Area.DataTransferOffset..]);
                    Area.CdWordNumber += 4;
                    Area.DataTransferOffset += 4;
                    if (Area.DataTransferOffset >= sector.Length)
                    {
                        Area.DataTransferOffset = 0;
                        Area.DataTransferredSectorCount++;
                    }

                    return value;
                }

                if (Area.DataTransferType == TransferGetDeleteSector)
                {
                    Area.DataTransferType = TransferInvalid;
                    DeleteTransferredSectors();
                }
            }

            return 0;
        }

        if (!IsCdRegister(address))
            return 0;

        ushort register = ReadWord(address);
        if (address == 0x90024)
            Area.Command = 0;
        return ((uint)register << 16) | register;
    }

    public void WriteLong(uint address, uint value)
    {
        if ((address & 0xFFFFF) != 0x18000
            || Area.DataTransferType != TransferPutSector
            || Area.DataTransferredSectorCount >= Area.DataSectorsToTransfer)
            return;

        Block? block = GetTransferBlock();
        if (block is null)
            return;

        BinaryPrimitives.WriteUInt32BigEndian(block.Data.AsSpan(Area.DataTransferOffset), value);
        Area.CdWordNumber += 4;
        Area.DataTransferOffset += 4;
        if (Area.DataTransferOffset >= block.Size)
        {
            int modeOffset = block.Size switch
            {
                2352 => 15,
                2340 => 3,
                _ => -1,
            };
            if (modeOffset >= 0 && block.Data[modeOffset] == 0x02)
            {
                block.IsMode2 = true;
                block.FileNumber = block.Data[modeOffset + 1];
                block.ChannelNumber = block.Data[modeOffset + 2];
                block.Submode = block.Data[modeOffset + 3];
                block.CodingInfo = block.Data[modeOffset + 4];
            }

            Area.DataTransferOffset = 0;
            Area.DataTransferredSectorCount++;
            if (Area.DataTransferredSectorCount >= Area.DataSectorsToTransfer)
                SetHirq(HirqEhst);
        }
    }

    public void RapidCopyT1(Span<byte> destination, uint count)
    {
        if (Area.DataTransferType != TransferInvalid)
        {
            while (count > 0 && Area.DataTransferredSectorCount < Area.DataSectorsToTransfer)
            {
                Block? block = GetTransferBlock();
                if (block is null || block.Size < 0)
                    break;
                ReadOnlySpan<byte> sector = block.Data.AsSpan(0, block.Size);
                int copy = (int)Math.Min((uint)(sector.Length - Area.DataTransferOffset), count * 4);
                sector.Slice(Area.DataTransferOffset, copy).CopyTo(destination);
                destination = destination[copy..];
                count -= (uint)copy / 4;
                Area.DataTransferOffset += copy;
                Area.CdWordNumber += (uint)copy;

                if (Area.DataTransferOffset >= sector.Length)
                {
                    Area.DataTransferOffset = 0;
                    Area.DataTransferredSectorCount++;
                }
            }

            if (Area.DataTransferType == TransferGetDeleteSector
                && Area.DataTransferredSectorCount >= Area.DataSectorsToTransfer)
            {
                DeleteTransferredSectors();
                Area.DataTransferType = TransferInvalid;
            }
        }

        if (count > 0)
            destination[..(int)(count * 4)].Fill(0xCD);
    }

    public void RapidCopyT2(Span<byte> destination, uint count)
    {
        if (Area.DataTransferType != TransferInvalid)
        {
            while (count > 0 && Area.DataTransferredSectorCount < Area.DataSectorsToTransfer)
            {
                Block? block = GetTransferBlock();
                if (block is null || block.Size < 0)
                    break;
                ReadOnlySpan<byte> sector = block.Data.AsSpan(0, block.Size);
                int copy = (int)Math.Min((uint)(sector.Length - Area.DataTransferOffset), count * 4);
                ReadOnlySpan<byte> source = sector[Area.DataTransferOffset..];
                int offset = 0;
                for (; offset <= copy - 16; offset += 16)
                {
                    uint word0 = BinaryPrimitives.ReadUInt32BigEndian(source[offset..]);
                    uint word1 = BinaryPrimitives.ReadUInt32BigEndian(source[(offset + 4)..]);
                    uint word2 = BinaryPrimitives.ReadUInt32BigEndian(source[(offset + 8)..]);
                    uint word3 = BinaryPrimitives.ReadUInt32BigEndian(source[(offset + 12)..]);
                    T2WriteLong(destination, (uint)offset, word0);
                    T2WriteLong(destination, (uint)(offset + 4), word1);
                    T2WriteLong(destination, (uint)(offset + 8), word2);
                    T2WriteLong(destination, (uint)(offset + 12), word3);
                }

                for (; offset < copy; offset += 4)
                    T2WriteLong(destination, (uint)offset, BinaryPrimitives.ReadUInt32BigEndian(source[offset..]));

                destination = destination[copy..];
                count -= (uint)copy / 4;
                Area.DataTransferOffset += copy;
                Area.CdWordNumber += (uint)copy;

                if (Area.DataTransferOffset >= sector.Length)
                {
                    Area.DataTransferOffset = 0;
                    Area.DataTransferredSectorCount++;
                }
            }

            if (Area.DataTransferType == TransferGetDeleteSector
                && Area.DataTransferredSectorCount >= Area.DataSectorsToTransfer)
            {
                DeleteTransferredSectors();
                Area.DataTransferType = TransferInvalid;
            }
        }

        if (count > 0)
            destination[..(int)(count * 4)].Fill(0xCD);
    }

    private Block? AllocateBlock(ref byte blockNumber, int sectorSize)
    {
        for (int i = 0; i < 200; i++)
        {
            Block block = Area.Blocks[i];
            if (block.Size == -1)
            {
                Area.FreeBlockCount--;
                if (Area.FreeBlockCount <= 0)
                    Area.BufferFull = 1;

                block.Size = sectorSize;
                block.IsMode2 = false;
                blockNumber = (byte)i;
                return block;
            }
        }

        Area.BufferFull = 1;
        return null;
    }

    private void FreeBlock(Block? block)
    {
        if (block is null)
            return;

        block.Size = -1;
        Area.FreeBlockCount++;
        Area.BufferFull = 0;
        Area.Reg.Hirq &= unchecked((ushort)~HirqBful);
        UpdateInterruptLine();
    }

    private static void SortBlocks(Partition partition)
    {
        int to = 0;
        for (int from = 0; from < MaxBlocks; from++)
        {
            if (partition.Blocks[from] is not null)
            {
                if (to != from)
                {
                    partition.Blocks[to] = partition.Blocks[from];
                    partition.BlockNumbers[to] = partition.BlockNumbers[from];
                }

                to++;
            }
        }

        for (; to < MaxBlocks; to++)
        {
            partition.Blocks[to] = null;
            partition.BlockNumbers[to] = 0xFF;
        }
    }

    private void SetupFileInfoTransfer(uint fileIndex)
    {
        ref DirectoryRecord record = ref Area.Files[fileIndex];
        BinaryPrimitives.WriteUInt32BigEndian(Area.TransferFileInfo, record.Lba);
        BinaryPrimitives.WriteUInt32BigEndian(Area.TransferFileInfo.AsSpan(4), record.Size);
        Area.TransferFileInfo[8] = record.FileUnitSize;
        Area.TransferFileInfo[9] = record.InterleaveGapSize;
        Area.TransferFileInfo[10] = record.Xa.FileNumber;
        Area.TransferFileInfo[11] = (byte)((record.Flags & 0x02) | ((record.Xa.Attributes >> 8) & 0xF8));
    }

    public int Initialize(int cartridgeType, int coreId, string? cdPath, string? mpegPath,
        string? modemIp, string? modemPort)
    {
        _area = new SaturnCdBlockState
        {
            CartridgeType = cartridgeType,
            MpegPath = mpegPath,
        };

        bool initialized = false;
        try
        {
            int result = ChangeCdCore(coreId, cdPath);
            if (result != 0)
                return result;

            Reset();

            if (Area.CartridgeType == NetlinkCartridge)
            {
                result = _connections.InitializeNetlink(modemIp, modemPort);
                if (result != 0)
                    return result;
            }
            else if (Area.CartridgeType == JapaneseModemCartridge)
            {
                result = _connections.InitializeJapaneseModem(modemIp, modemPort);
                if (result != 0)
                    return result;
            }

            _initialProgram = new InitialProgram();
            initialized = true;
            return 0;
        }
        finally
        {
            if (!initialized)
                Dispose();
        }
    }

    private int ChangeCdCore(int coreId, string? cdPath, bool initializing = true)
    {
        Area.CdInterface?.Deinitialize();

        if (coreId == SaturnCdInterface.Default)
            coreId = 0;

        foreach (SaturnCdInterface cdInterface in _connections.CdInterfaces)
        {
            if (cdInterface.Id == coreId)
            {
                Area.CdInterface = cdInterface;
                break;
            }
        }

        if (Area.CdInterface is null)
        {
            Area.CdInterface = _connections.DummyCd;
            return -1;
        }

        if (Area.CdInterface.Initialize(cdPath) != 0)
        {
            _connections.ReportCdInitializationError(Area.CdInterface.Name);
            Area.CdInterface = _connections.DummyCd;
        }

        if (initializing)
        {
            Area.DiskChanged = 1;
            Area.Status = StatusPause;
            _connections.RecheckRegion();
        }
        return 0;
    }

    public int ChangeMedia(string? cdPath)
    {
        int coreId = cdPath is null ? SaturnCdInterface.Dummy : SaturnCdInterface.Iso;
        int result = ChangeCdCore(coreId, cdPath, initializing: false);
        if (result != 0)
            return result;

        if (!_mediaTrayOpen)
            SetMediaTrayOpen(true);

        if (cdPath is not null)
        {
            SetMediaTrayOpen(false);
            RefreshMediaToc();
        }

        return 0;
    }

    public void Dispose()
    {
        SaturnCdBlockState? area = _area;
        if (area is null)
            return;

        try
        {
            area.CdInterface?.Deinitialize();
        }
        finally
        {
            try
            {
                if (area.CartridgeType == NetlinkCartridge)
                    _connections.DeinitializeNetlink();
                else if (area.CartridgeType == JapaneseModemCartridge)
                    _connections.DeinitializeJapaneseModem();
            }
            finally
            {
                _area = null;
                _initialProgram = null;
                UpdateInterruptLine();
            }
        }
    }

    private bool _mediaTrayOpen;

    private void SetMediaTrayOpen(bool open)
    {
        _mediaTrayOpen = open;
        if (!open || _systemResetMicroseconds != 0)
            return;

        Area.SaturnAuthentication = 0;
        _authenticatingDisc = false;
        _driveStartupMicroseconds = 0;
        _initialSeekPhase = InitialSeekPhase.None;
        _ejectPhase = EjectPhase.DiscChanged;
        _ejectMicroseconds = CdClocksToMicroseconds(1000);
    }

    private uint AdvanceEjection(uint timing)
    {
        while (_ejectPhase != EjectPhase.None && timing >= _ejectMicroseconds)
        {
            timing -= _ejectMicroseconds;
            switch (_ejectPhase)
            {
                case EjectPhase.DiscChanged:
                    Array.Fill(Area.Toc, uint.MaxValue);
                    Area.RootDirectoryRecord = default;
                    Area.CurrentDirectorySector = 0;
                    _fileInfoValid = false;
                    Area.Status = StatusOpen;
                    Area.Fad = uint.MaxValue;
                    Area.ControlAddress = Area.Track = Area.Index = 0xFF;
                    Area.RepeatCount = 0x7F;
                    Area.DiskChanged = 1;
                    Area.IsAudio = 0;
                    _playbackPositionFad = _subcodeRelativeFad = null;
                    SetTiming(0);
                    SetHirq(HirqDchg);
                    _ejectPhase = EjectPhase.FileSystemEnd;
                    _ejectMicroseconds = CdClocksToMicroseconds(4000);
                    break;
                case EjectPhase.FileSystemEnd:
                    SetHirq(HirqEfls);
                    _ejectPhase = EjectPhase.Waiting;
                    _ejectMicroseconds = CdClocksToMicroseconds(1);
                    break;
                case EjectPhase.Waiting:
                    if (GetMediaDriveStatus() is 0 or 1)
                    {
                        Area.Status = StatusBusy;
                        _driveStartupMicroseconds = 1_000_000;
                        _ejectPhase = EjectPhase.None;
                    }
                    else
                    {
                        _ejectMicroseconds = CdClocksToMicroseconds(1000);
                    }
                    break;
            }
        }

        if (_ejectPhase == EjectPhase.None)
            return timing;

        _ejectMicroseconds -= timing;
        return 0;
    }

    public void RefreshMediaToc() => Area.CdInterface!.ReadToc(Area.Toc);

    internal int GetMediaDriveStatus() => _mediaTrayOpen ? 3 : Area.CdInterface!.GetStatus();

    public void Reset()
    {
        _connections.ClearCdda();
        Array.Clear(_executingCommandData);
        _systemResetMicroseconds = 0;
        _driveStartupMicroseconds = 0;
        _ejectPhase = EjectPhase.None;
        _ejectMicroseconds = 0;
        _initialSeekPhase = InitialSeekPhase.None;
        _initialSeekMicroseconds = 0;
        _playbackPositionFad = null;
        _subcodeRelativeFad = null;
        Array.Clear(Area.TransferSubcodeQ);
        switch (GetMediaDriveStatus())
        {
            case 0:
            case 1:
                Area.Status = StatusPause;
                Area.Fad = 150;
                Area.Options = 0;
                Area.RepeatCount = 0;
                Area.ControlAddress = 0x41;
                Area.Track = 1;
                Area.Index = 1;
                break;

            case 2:
                Area.Status = StatusNoDisc;
                Area.Fad = 0xFFFFFFFF;
                Area.Options = 0xFF;
                Area.RepeatCount = 0xFF;
                Area.ControlAddress = 0xFF;
                Area.Track = 0xFF;
                Area.Index = 0xFF;
                break;

            case 3:
                Area.Status = StatusOpen;
                Area.Fad = 0xFFFFFFFF;
                Area.Options = 0xFF;
                Area.RepeatCount = 0xFF;
                Area.ControlAddress = 0xFF;
                Area.Track = 0xFF;
                Area.Index = 0xFF;
                break;
        }

        Area.InfoTransferType = -1;
        Area.DataTransferType = TransferInvalid;
        Area.TransferCount = 0;
        Area.CdWordNumber = 0;
        Area.GetSectorSize = Area.PutSectorSize = 2048;
        Area.DiskChanged = 1;
        Area.BufferFull = 0;
        Area.IsAudio = 0;

        Area.Reg.Cr1 = 'C';
        Area.Reg.Cr2 = ('D' << 8) | 'B';
        Area.Reg.Cr3 = ('L' << 8) | 'O';
        Area.Reg.Cr4 = ('C' << 8) | 'K';
        Area.Reg.Hirq = 0xFFFF;
        Area.Reg.HirqMask = 0xFFFF;

        Area.PlayFad = 0xFFFFFFFF;
        Area.PlayEndFad = 0xFFFFFFFF;
        Area.PlayType = 0;
        Area.MaxRepeat = 0;
        Area.SaturnAuthentication = 0;
        _authenticatingDisc = false;
        Area.MpegAuthentication = 0;

        ResetSelectorsAndBuffers();
        Array.Fill(Area.Toc, 0xFFFFFFFFu);
        ResetDirectoryAndSearch();

        Area.Command = 0;
        Area.StatusCycles = 0;
        Area.StatusTiming = 1000000;
        Area.PeriodicCycles = 0;
        Area.CommandTiming = 0;
        Area.CommandExecutionTiming = 0;
        Area.DelayedHirq = 0;
        SetTiming(0);

        Area.MpegConnections[0] = new MpegConnection { AudioBuffer = 0xFF, VideoBuffer = 0xFF };
        Area.MpegConnections[1] = new MpegConnection { AudioBuffer = 0xFF, VideoBuffer = 0xFF };
        Area.MpegStreams[0] = default;
        Area.MpegStreams[1] = default;
    }

    private void ResetSelectorsAndBuffers()
    {
        for (int i = 0; i < MaxSelectors; i++)
        {
            Filter filter = Area.Filters[i];
            filter.Fad = 0;
            filter.Range = 0xFFFFFFFF;
            filter.Mode = 0;
            filter.Channel = 0;
            filter.SubmodeMask = 0;
            filter.CodingInfoMask = 0;
            filter.FileId = 0;
            filter.SubmodeValue = 0;
            filter.CodingInfoValue = 0;
            filter.TrueConnection = (byte)i;
            filter.FalseConnection = 0xFF;
        }

        for (int i = 0; i < MaxSelectors; i++)
        {
            Partition partition = Area.Partitions[i];
            partition.Size = -1;
            partition.BlockCount = 0;
            for (int j = 0; j < MaxBlocks; j++)
            {
                partition.Blocks[j] = null;
                partition.BlockNumbers[j] = 0xFF;
            }
        }

        for (int i = 0; i < MaxBlocks; i++)
        {
            Area.Blocks[i].Size = -1;
            Array.Clear(Area.Blocks[i].Data);
        }

        Area.FreeBlockCount = MaxBlocks;
        Area.OneSectorStored = false;
    }

    private void ResetDirectoryAndSearch()
    {
        _fileInfoValid = false;
        Area.RootDirectoryRecord = default;
        Area.CurrentDirectorySector = 0;
        Area.CurrentDirectorySize = 0;
        Area.CurrentDirectoryFileIdOffset = 0;
        Array.Clear(Area.Files);
        Area.FileCount = 0;
        Area.FileInfoMore = false;
        Area.LastBuffer = 0xFF;
        Area.CalculatedSize = 0;
        Area.FadSearchSector = 0;
        Area.FadSearchPartition = 0;
        Area.FadSearchAddress = 0;
    }

    public void ResetAfterSystemCommand()
    {
        _connections.ClearCdda();
        _playbackPositionFad = null;
        _systemResetMicroseconds = (uint)((4_880_000UL * 1_000_000 + CdClockHz - 1) / CdClockHz);
        Area.Reg.Cr1 = Area.Reg.Cr2 = Area.Reg.Cr3 = Area.Reg.Cr4 = 0;
        Area.Reg.Hirq = Area.Reg.HirqMask = 0;
        UpdateInterruptLine();
        Area.BufferFull = Area.DiskChanged = 0;
        Area.Status = StatusBusy;
    }

    public void Execute(uint timing)
    {
        if (_systemResetMicroseconds != 0)
        {
            if (timing < _systemResetMicroseconds)
            {
                _systemResetMicroseconds -= timing;
                return;
            }

            timing -= _systemResetMicroseconds;
            ushort interruptMask = Area.Reg.HirqMask;
            Reset();
            Area.Reg.HirqMask = interruptMask;
            Area.Reg.Hirq = 0;
            Area.CdDeviceOutput = null;
            Area.CdDeviceOutputNumber = 0xFF;
            if (GetMediaDriveStatus() is 0 or 1)
            {
                _driveStartupMicroseconds = 1_000_000 + (1_000u * 1_000_000 + CdClockHz - 1) / CdClockHz;
                Area.Status = StatusBusy;
            }
            SetHirq(HirqCmok | HirqDchg | HirqEsel | HirqEhst | HirqMped | HirqEcpy | HirqEfls);
        }

        uint seekTiming = AdvanceEjection(timing);
        if (_driveStartupMicroseconds != 0)
        {
            if (seekTiming < _driveStartupMicroseconds)
            {
                _driveStartupMicroseconds -= seekTiming;
                seekTiming = 0;
            }
            else
            {
                seekTiming -= _driveStartupMicroseconds;
                _driveStartupMicroseconds = 0;
                RefreshMediaToc();
                if (_initialSeekPhase == InitialSeekPhase.None
                    && (Area.Status & 0x0F) == StatusBusy)
                {
                    _seekToPlayback = false;
                    _seekOriginFad = 0;
                    _seekPauseFad = 150;
                    _initialSeekPhase = InitialSeekPhase.Position;
                    _initialSeekMicroseconds = CdClocksToMicroseconds(500);
                }
            }
        }
        if (_initialSeekPhase != InitialSeekPhase.None)
            AdvanceInitialSeek(seekTiming);

        Area.StatusCycles = unchecked(Area.StatusCycles + timing * 3);
        Area.PeriodicCycles = unchecked(Area.PeriodicCycles + timing * 3);

        uint commandTime = timing;
        while (commandTime > 0)
        {
            if (Area.CommandExecutionTiming != 0)
            {
                if (commandTime < Area.CommandExecutionTiming)
                {
                    Area.CommandExecutionTiming -= commandTime;
                    break;
                }

                commandTime -= Area.CommandExecutionTiming;
                Area.CommandExecutionTiming = 0;
                CompleteDelayedCommand();
                continue;
            }

            if (Area.CommandTiming == 0)
                break;
            if (commandTime < Area.CommandTiming)
            {
                Area.CommandTiming -= commandTime;
                break;
            }

            commandTime -= Area.CommandTiming;
            ExecuteCommand();
            Area.CommandTiming = 0;
        }

        if (Area.StatusCycles >= Area.StatusTiming && _ejectPhase == EjectPhase.None)
        {
            Area.StatusCycles -= Area.StatusTiming;
            switch (GetMediaDriveStatus())
            {
                case 0:
                case 1:
                    if ((Area.Status & 0xF) == StatusNoDisc || (Area.Status & 0xF) == StatusOpen)
                    {
                        Area.Status = StatusPause;
                        Area.DiskChanged = 1;
                    }
                    break;
                case 2:
                    if ((Area.Status & 0xF) != StatusNoDisc)
                    {
                        Area.Status = StatusNoDisc;
                        _playbackPositionFad = null;
                        Area.RootDirectoryRecord = default;
                        Area.CurrentDirectorySector = 0;
                        _fileInfoValid = false;
                    }
                    break;
                case 3:
                    if ((Area.Status & 0xF) != StatusOpen)
                    {
                        Area.Status = StatusOpen;
                        _playbackPositionFad = null;
                        Area.RootDirectoryRecord = default;
                        Area.CurrentDirectorySector = 0;
                        _fileInfoValid = false;
                    }
                    break;
            }
        }

        if (Area.PeriodicCycles >= Area.PeriodicTiming)
        {
            Area.PeriodicCycles -= Area.PeriodicTiming;

            switch (Area.Status & 0xF)
            {
                case StatusPause:
                    break;
                case StatusPlay:
                {
                    int result = ReadFilteredSector(Area.Fad, out Partition? partition);
                    switch (result)
                    {
                        case 0:
                            Area.Fad++;
                            Area.CdInterface!.ReadAheadFad(Area.Fad);

                            if (partition is not null)
                            {
                                Area.OneSectorStored = true;
                                SetHirq(HirqCsct);
                            }

                            if (Area.Fad >= Area.PlayEndFad)
                            {
                                if (Area.RepeatCount >= Area.MaxRepeat)
                                {
                                    Area.Status = StatusPause;
                                    SetTiming(0);
                                    SetHirq(HirqPend);
                                    if (Area.PlayType == PlayTypeFile)
                                        SetHirq(HirqEfls);
                                }
                                else
                                {
                                    Area.Fad = Area.PlayFad;
                                    if (Area.RepeatCount < 0xE)
                                        Area.RepeatCount++;
                                    Area.Track = FadToTrack(Area.Fad);
                                }
                            }
                            break;
                        case -1:
                        case -2:
                            break;
                    }
                    break;
                }
                case StatusScan:
                    AdvanceScan();
                    break;
                case StatusSeek:
                    break;
                case StatusRetry:
                    break;
            }

            WriteSubcodeQReport(Area.TransferSubcodeQ);
            if (Area.Command != 0)
            {
                SetHirq(HirqScdq);
                return;
            }

            if (_authenticatingDisc)
            {
                Area.Reg.Cr1 = (StatusPeriodic << 8) | 0xFF;
                Area.Reg.Cr2 = Area.Reg.Cr3 = Area.Reg.Cr4 = 0xFFFF;
            }
            else
            {
                Area.Status |= StatusPeriodic;
                ReportCdStatus(Area.Status);
            }
            SetHirq(HirqScdq);
        }

        if (Area.CartridgeType == NetlinkCartridge)
            _connections.ExecuteNetlink(timing);
        else if (Area.CartridgeType == JapaneseModemCartridge)
            _connections.ExecuteJapaneseModem(timing);
    }

    public int GetTimeToNextSector()
    {
        if ((Area.Status & 0xF) != StatusPlay)
            return 0;

        int time = (int)(unchecked(Area.PeriodicTiming - Area.PeriodicCycles + 2) / 3);
        return time < 0 ? 0 : time;
    }

    private void CompleteDelayedCommand()
    {
        SetHirq(Area.DelayedHirq);
    }

    private static uint CdClocksToMicroseconds(uint clocks) =>
        (uint)(((ulong)clocks * 1_000_000 + CdClockHz - 1) / CdClockHz);

    private void AdvanceInitialSeek(uint timing)
    {
        const uint firstFad = 150;
        const uint dataSectorClocks = CdClockHz / 150;
        Span<byte> q = stackalloc byte[12];
        byte expectedStatus = _initialSeekPhase == InitialSeekPhase.FirstSector ? StatusSeek : StatusBusy;
        if ((Area.Status & 0x0F) != expectedStatus)
        {
            if (_authenticatingDisc)
                FinishDiscAuthentication(0);
            _initialSeekPhase = InitialSeekPhase.None;
            _initialSeekMicroseconds = 0;
            return;
        }

        while (_initialSeekPhase != InitialSeekPhase.None && timing >= _initialSeekMicroseconds)
        {
            timing -= _initialSeekMicroseconds;
            switch (_initialSeekPhase)
            {
                case InitialSeekPhase.Position:
                    _initialSeekFad = _seekToPlayback
                        ? Math.Min(Math.Max(Area.PlayFad, firstFad), TrackToFad(0xFFFF))
                        : _seekPauseFad;
                    byte track = FadToTrack(_initialSeekFad);
                    byte repeatCount = Area.RepeatCount;
                    byte firstTrack = (byte)(Area.Toc[99] >> 16);
                    SetupDefaultPlayStats(track is 0 or 0xFF
                        ? firstTrack is >= 1 and <= 99 ? firstTrack : (byte)1
                        : track, false);
                    if (_seekToPlayback)
                    {
                        Area.Options = 8;
                        Area.RepeatCount = repeatCount;
                    }
                    if (_initialSeekFad == TrackToFad(0xFFFF))
                    {
                        Area.Track = 0xAA;
                        Area.ControlAddress = (byte)(Area.Toc[101] >> 24);
                    }
                    Area.Fad = _initialSeekFad;
                    _playbackPositionFad = null;
                    Area.CdInterface!.ReadAheadFad(_initialSeekFad);
                    _initialSeekPhase = InitialSeekPhase.Seek;
                    _initialSeekMicroseconds = CdClocksToMicroseconds(256_000u - 500);
                    break;
                case InitialSeekPhase.Seek:
                    Area.Status = StatusSeek;
                    _initialSeekPhase = InitialSeekPhase.FirstSector;
                    int fadDelta = (int)_initialSeekFad - (int)_seekOriginFad;
                    uint seekClocks = 12 * dataSectorClocks;
                    seekClocks += (uint)Math.Abs(fadDelta) * (fadDelta < 0 ? 28u : 26u);
                    if (fadDelta < 0 || fadDelta >= 150)
                        seekClocks += dataSectorClocks;
                    uint sectorClocks = (Area.ControlAddress & 0x40) != 0 ? dataSectorClocks : CdClockHz / 75;
                    _initialSeekMicroseconds = CdClocksToMicroseconds(seekClocks + sectorClocks);
                    break;
                case InitialSeekPhase.FirstSector:
                    if (TryReadSubchannelQ(_initialSeekFad, q))
                    {
                        if (_authenticatingDisc)
                        {
                            FinishDiscAuthentication(ReadDiscAuthentication(_initialSeekFad));
                            _seekOriginFad = _initialSeekFad;
                            Area.Status = StatusBusy;
                            _initialSeekPhase = InitialSeekPhase.Position;
                            _initialSeekMicroseconds = CdClocksToMicroseconds(500);
                            break;
                        }

                        if (_seekToPlayback)
                        {
                            Area.Fad = _initialSeekFad;
                            Area.IsAudio = (q[0] & 0x40) == 0 ? 1 : 0;
                            Area.Status = StatusPlay;
                            Area.PeriodicCycles = 0;
                            SetTiming(1);
                        }
                        else
                        {
                            Area.Fad = _initialSeekFad;
                            Area.Status = StatusPause;
                            SetTiming(0);
                        }
                    }
                    else if (++_initialSeekFad < (Area.Toc[101] & 0xFFFFFF))
                    {
                        _initialSeekMicroseconds = CdClocksToMicroseconds(dataSectorClocks);
                        break;
                    }
                    else
                    {
                        if (_authenticatingDisc)
                            FinishDiscAuthentication(0);
                        Area.Status = StatusError;
                    }
                    _initialSeekPhase = InitialSeekPhase.None;
                    _initialSeekMicroseconds = 0;
                    break;
            }
        }

        if (_initialSeekPhase != InitialSeekPhase.None)
            _initialSeekMicroseconds -= timing;
    }

    private bool TryReadSubchannelQ(uint fad, Span<byte> q)
    {
        if (Area.CdInterface!.ReadSubchannelFad is { } readSubchannel)
        {
            Span<byte> subchannels = stackalloc byte[96];
            return readSubchannel(fad, subchannels) != 0 && DecodeSubchannelQ(subchannels, q);
        }

        Span<byte> raw = stackalloc byte[2448];
        if (Area.CdInterface!.ReadSectorFad(fad, raw) == 0)
            return false;

        return DecodeSubchannelQ(raw[2352..], q);
    }

    private static bool DecodeSubchannelQ(ReadOnlySpan<byte> subchannels, Span<byte> q)
    {
        q.Clear();
        for (int i = 0; i < subchannels.Length; i++)
            q[i >> 3] |= (byte)(((subchannels[i] >> 6) & 1) << (7 - (i & 7)));

        return (q[0] & 0x0F) == 1 &&
            BinaryPrimitives.ReadUInt16BigEndian(q[10..]) == SaturnCdImageDrive.SubchannelQChecksum(q[..10]);
    }

    private Partition GetPartition(Filter filter) => Area.Partitions[filter.TrueConnection];

    private Partition? FilterData(int filterNumber)
    {
        Block work = Area.WorkBlock;
        int partitionNumber = FindSectorPartition(filterNumber, work, out byte lastBuffer);
        Area.LastBuffer = lastBuffer;
        if (partitionNumber >= MaxSelectors)
            return null;

        Partition partition = Area.Partitions[partitionNumber];

        int index = partition.BlockCount;
        Block? block = AllocateBlock(ref partition.BlockNumbers[index], (int)Area.GetSectorSize);
        partition.Blocks[index] = block;
        if (block is null)
            return null;

        block.Size = work.Size;
        block.Fad = work.Fad;
        block.IsMode2 = work.IsMode2;
        block.ChannelNumber = work.ChannelNumber;
        block.FileNumber = work.FileNumber;
        block.Submode = work.Submode;
        block.CodingInfo = work.CodingInfo;

        switch (work.Size)
        {
            case 2048:
                work.Data.AsSpan(work.Data[0xF] == 0x02 ? 24 : 16, work.Size).CopyTo(block.Data);
                break;
            case 2324:
                work.Data.AsSpan(24, work.Size).CopyTo(block.Data);
                break;
            case 2336:
                work.Data.AsSpan(16, work.Size).CopyTo(block.Data);
                break;
            case 2340:
                work.Data.AsSpan(12, work.Size).CopyTo(block.Data);
                break;
            case 2352:
                work.Data.AsSpan(0, work.Size).CopyTo(block.Data);
                break;
        }

        if (partition.Size == -1)
            partition.Size = 0;
        partition.Size += block.Size;
        partition.BlockCount++;
        return partition;
    }

    private static int CopyDirectoryRecord(ReadOnlySpan<byte> buffer, ref DirectoryRecord record)
    {
        record = default;
        const int fixedLength = 33;
        if (buffer.Length < fixedLength || buffer[0] < fixedLength || buffer[0] > buffer.Length)
            return -1;

        buffer = buffer[..buffer[0]];
        if (buffer[32] > buffer.Length - fixedLength)
            return -1;

        int offset = 0;
        record.RecordSize = buffer[offset++];
        record.XaRecordSize = buffer[offset++];

        record.Lba = BitConverter.ToUInt32(buffer[(offset + (BitConverter.IsLittleEndian ? 0 : 4))..]);
        offset += 8;
        record.Size = BitConverter.ToUInt32(buffer[(offset + (BitConverter.IsLittleEndian ? 0 : 4))..]);
        offset += 8;

        record.DateYear = buffer[offset++];
        record.DateMonth = buffer[offset++];
        record.DateDay = buffer[offset++];
        record.DateHour = buffer[offset++];
        record.DateMinute = buffer[offset++];
        record.DateSecond = buffer[offset++];
        record.GmtOffset = buffer[offset++];
        record.Flags = buffer[offset++];
        record.FileUnitSize = buffer[offset++];
        record.InterleaveGapSize = buffer[offset++];

        record.VolumeSequenceNumber = BitConverter.ToUInt16(buffer[(offset + (BitConverter.IsLittleEndian ? 0 : 2))..]);
        offset += 4;
        record.NameLength = buffer[offset++];
        Span<byte> name = record.Name;
        buffer.Slice(offset, Math.Min(record.NameLength, name.Length)).CopyTo(name);
        offset += record.NameLength;
        offset += 1 - record.NameLength % 2;

        if (record.RecordSize - offset >= 14 && buffer[offset + 6] == (byte)'X' && buffer[offset + 7] == (byte)'A')
        {
            record.Xa.GroupId = BitConverter.ToUInt16(buffer[offset..]);
            offset += 2;
            record.Xa.UserId = BitConverter.ToUInt16(buffer[offset..]);
            offset += 2;
            record.Xa.Attributes = BinaryPrimitives.ReadUInt16BigEndian(buffer[offset..]);
            offset += 2;
            record.Xa.Signature = BitConverter.ToUInt16(buffer[offset..]);
            offset += 2;
            record.Xa.FileNumber = buffer[offset++];
            Span<byte> reserved = record.Xa.Reserved;
            buffer.Slice(offset, 5).CopyTo(reserved);
        }

        return 0;
    }

    private Partition? ReadUnfilteredSector(uint fad)
    {
        if (Area.CdDeviceOutput is not { } filter || Area.BufferFull != 0)
            return null;

        Partition partition = GetPartition(filter);
        Block work = Area.WorkBlock;
        if (Area.CdInterface!.ReadSectorFad(fad, work.Data) == 0)
            return null;

        int blockIndex = partition.BlockCount;
        Block? block = AllocateBlock(ref partition.BlockNumbers[blockIndex], (int)Area.GetSectorSize);
        if (block is null)
            return null;

        switch (Area.GetSectorSize)
        {
            case 2048:
                if (work.Data[0xF] == 0x02)
                {
                    if ((work.Data[0x12] & 0x20) == 0)
                    {
                        work.Data.AsSpan(24, 2048).CopyTo(block.Data);
                        work.Size = 2048;
                    }
                    else
                    {
                        work.Data.AsSpan(24, 2324).CopyTo(block.Data);
                        work.Size = 2324;
                    }
                }
                else
                {
                    work.Data.AsSpan(16, 2048).CopyTo(block.Data);
                    work.Size = 2048;
                }
                break;
            case 2336:
                work.Data.AsSpan(16, 2336).CopyTo(block.Data);
                work.Size = 2336;
                break;
            case 2340:
                work.Data.AsSpan(12, 2340).CopyTo(block.Data);
                work.Size = 2340;
                break;
            case 2352:
                work.Size = 2352;
                break;
        }

        block.Fad = fad;
        block.FileNumber = block.ChannelNumber = block.Submode = block.CodingInfo = 0;
        ReadOnlySpan<byte> syncHeader = [0, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0];
        block.IsMode2 = work.Data.AsSpan(0, 12).SequenceEqual(syncHeader) && work.Data[0xF] == 0x02;
        if (block.IsMode2)
        {
            block.FileNumber = work.Data[0x10];
            block.ChannelNumber = work.Data[0x11];
            block.Submode = work.Data[0x12];
            block.CodingInfo = work.Data[0x13];
        }

        work.Fad = fad;
        partition.Blocks[blockIndex] = block;
        if (partition.Size == -1)
            partition.Size = 0;
        partition.Size += block.Size;
        partition.BlockCount++;
        return partition;
    }

    private int ReadFilteredSector(uint fad, out Partition? partition)
    {
        ReadOnlySpan<byte> syncHeader = [0, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0];
        Span<byte> q = stackalloc byte[12];
        if (Area.CdDeviceOutput is not null && Area.BufferFull == 0)
        {
            Block work = Area.WorkBlock;
            if (Area.CdInterface!.ReadSectorFad(fad, work.Data) == 0)
            {
                partition = null;
                return -2;
            }

            work.Size = (int)Area.GetSectorSize;
            work.Fad = fad;
            UpdatePlaybackPosition(fad, work.Data.AsSpan(2352, 96), q);
            bool isAudio = !work.Data.AsSpan(0, 12).SequenceEqual(syncHeader);
            work.IsMode2 = !isAudio && work.Data[0xF] == 0x02;

            Area.IsAudio = isAudio ? 1 : 0;
            SetTiming(1);

            if (isAudio)
            {
                _connections.ReceiveCdda(work.Data);
                partition = null;
                return 0;
            }
            else if (work.IsMode2)
            {
                if ((work.Data[0x12] & 0x20) != 0)
                    work.Size = 2324;

                work.FileNumber = work.Data[0x10];
                work.ChannelNumber = work.Data[0x11];
                work.Submode = work.Data[0x12];
                work.CodingInfo = work.Data[0x13];
            }

            partition = FilterData(Area.CdDeviceOutputNumber);
            return 0;
        }

        partition = null;
        return -1;
    }

    private bool UpdatePlaybackPosition(uint fad, ReadOnlySpan<byte> subchannels, Span<byte> q)
    {
        if (!_playbackPositionFad.HasValue)
            _subcodeRelativeFad = null;
        _playbackPositionFad = fad;
        if (!DecodeSubchannelQ(subchannels, q))
            return false;

        Area.Track = q[1] >= 0xA0 ? q[1] : FromBcd(q[1]);
        Area.Index = FromBcd(q[2]);
        _subcodeRelativeFad = SubcodeMsfToFad(q[3..6]);
        return true;
    }

    public void SetAudioPlaying(bool playing) => Area.IsAudio = playing ? 1 : 0;

    public void SetTiming(int playing)
    {
        if (playing != 0)
        {
            if (Area.IsAudio != 0 || Area.Speed1X == 1)
                Area.PeriodicTiming = 40000;
            else
                Area.PeriodicTiming = 20000;
        }
        else
        {
            Area.PeriodicTiming = 50000;
        }
    }

    private void SetCommandTiming(byte command)
    {
        Area.CommandTiming = command == 0x02 ? 250u : 50u;
    }

    private void ExecuteCommand()
    {
        _executingCommandData[0] = Area.Reg.Cr1;
        _executingCommandData[1] = Area.Reg.Cr2;
        _executingCommandData[2] = Area.Reg.Cr3;
        _executingCommandData[3] = Area.Reg.Cr4;
        int instruction = _executingCommandData[0] >> 8;
        Area.Reg.Hirq &= unchecked((ushort)~HirqCmok);
        UpdateInterruptLine();

        if (_authenticatingDisc)
        {
            Area.Reg.Cr1 = Area.Reg.Cr2 = Area.Reg.Cr3 = Area.Reg.Cr4 = 0xFFFF;
            SetHirq(HirqCmok);
            return;
        }

        switch (instruction)
        {
            case 0x00: GetStatus(); break;
            case 0x01: GetHardwareInfo(); break;
            case 0x02: GetToc(); break;
            case 0x03: GetSessionInfo(); break;
            case 0x04: InitializeCdSystem(); break;
            case 0x05: OpenTray(); break;
            case 0x06: EndDataTransfer(); break;
            case 0x10: PlayDisc(); break;
            case 0x11: SeekDisc(); break;
            case 0x12: ScanDisc(); break;
            case 0x20: GetSubcodeQRw(); break;
            case 0x30: SetCdDeviceConnection(); break;
            case 0x31: GetCdDeviceConnection(); break;
            case 0x32: GetLastBufferDestination(); break;
            case 0x40: SetFilterRange(); break;
            case 0x41: GetFilterRange(); break;
            case 0x42: SetFilterSubheaderConditions(); break;
            case 0x43: GetFilterSubheaderConditions(); break;
            case 0x44: SetFilterMode(); break;
            case 0x45: GetFilterMode(); break;
            case 0x46: SetFilterConnection(); break;
            case 0x47: GetFilterConnection(); break;
            case 0x48: ResetSelector(); break;
            case 0x50: GetBufferSize(); break;
            case 0x51: GetSectorNumber(); break;
            case 0x52: CalculateActualSize(); break;
            case 0x53: GetActualSize(); break;
            case 0x54: GetSectorInfo(); break;
            case 0x55: ExecuteFadSearch(); break;
            case 0x56: GetFadSearchResults(); break;
            case 0x60: SetSectorLength(); break;
            case 0x61: GetSectorData(); break;
            case 0x62: DeleteSectorData(); break;
            case 0x63: GetThenDeleteSectorData(); break;
            case 0x64: PutSectorData(); break;
            case 0x65: CopySectorData(); break;
            case 0x66: MoveSectorData(); break;
            case 0x67: GetCopyError(); break;
            case 0x70: ChangeDirectory(); break;
            case 0x71: ReadDirectory(); break;
            case 0x72: GetFileSystemScope(); break;
            case 0x73: GetFileInfo(); break;
            case 0x74: ReadFile(); break;
            case 0x75: AbortFile(); break;
            case 0x90: MpegGetStatus(); break;
            case 0x91: MpegGetInterrupt(); break;
            case 0x92: MpegSetInterruptMask(); break;
            case 0x93: MpegInitialize(); break;
            case 0x94: MpegSetMode(); break;
            case 0x95: MpegPlay(); break;
            case 0x96: MpegSetDecodingMethod(); break;
            case 0x9A: MpegSetConnection(); break;
            case 0x9B: MpegGetConnection(); break;
            case 0x9D: MpegSetStream(); break;
            case 0x9E: MpegGetStream(); break;
            case 0xA0: MpegDisplay(); break;
            case 0xA1: MpegSetWindow(); break;
            case 0xA2: MpegSetBorderColor(); break;
            case 0xA3: MpegSetFade(); break;
            case 0xA4: MpegSetVideoEffects(); break;
            case 0xAF: MpegSetLsi(); break;
            case 0xE0: AuthenticateDevice(); break;
            case 0xE1: IsDeviceAuthenticated(); break;
            case 0xE2: GetMpegRom(); break;
        }
    }

    private void GetStatus()
    {
        ReportCdStatus(Area.Status);
        SetHirq(HirqCmok);
    }

    private void GetHardwareInfo()
    {
        if ((Area.Status & 0xF) != StatusOpen && (Area.Status & 0xF) != StatusNoDisc)
            Area.DiskChanged = 0;

        Area.Reg.Cr1 = (ushort)(Area.Status << 8);
        Area.Reg.Cr2 = 0x0201;

        if (Area.MpegAuthentication != 0)
            Area.Reg.Cr3 = 1;
        else
            Area.Reg.Cr3 = 0;

        Area.Reg.Cr4 = 0x0400;
        SetHirq(HirqCmok);
    }

    private void GetToc()
    {
        Area.CdInterface!.ReadToc(Area.Toc);
        Area.TransferCount = 0;
        Area.InfoTransferType = 0;
        Area.Reg.Cr1 = (ushort)(Area.Status << 8);
        Area.Reg.Cr2 = 0xCC;
        Area.Reg.Cr3 = 0;
        Area.Reg.Cr4 = 0;
        SetHirq(HirqCmok | HirqDrdy);
        Area.Status = StatusPause;
    }

    private void GetSessionInfo()
    {
        switch (_executingCommandData[0] & 0xFF)
        {
            case 0:
                Area.Reg.Cr3 = (ushort)(0x0100 | ((Area.Toc[101] & 0xFF0000) >> 16));
                Area.Reg.Cr4 = unchecked((ushort)Area.Toc[101]);
                break;
            case 1:
                Area.Reg.Cr3 = 0x0100;
                Area.Reg.Cr4 = 0;
                break;
            default:
                Area.Reg.Cr3 = 0xFFFF;
                Area.Reg.Cr4 = 0xFFFF;
                break;
        }

        Area.Status = StatusPause;
        Area.Reg.Cr1 = (ushort)(Area.Status << 8);
        Area.Reg.Cr2 = 0;
        SetHirq(HirqCmok);
    }

    private void InitializeCdSystem()
    {
        _connections.ClearCdda();
        byte flags = unchecked((byte)_executingCommandData[0]);

        if ((Area.Status & 0xF) != StatusOpen && (Area.Status & 0xF) != StatusNoDisc)
        {
            Area.Status = StatusPause;
            Area.Fad = 150;
            _playbackPositionFad = null;
        }

        if ((flags & 0x10) != 0)
            Area.Speed1X = 1;
        else
            Area.Speed1X = 0;

        ushort value = (ushort)(Area.Reg.Hirq & 0xFFE5);
        Area.BufferFull = 0;
        if (Area.DiskChanged != 0)
            value |= HirqDchg;
        else
            value &= unchecked((ushort)~HirqDchg);

        ReportCdStatus(Area.Status);
        Area.Reg.Hirq = (ushort)(value | HirqCmok | HirqEsel);
        UpdateInterruptLine();
    }

    private void OpenTray()
    {
        _playbackPositionFad = null;
        Area.Status = StatusOpen;
        Area.RootDirectoryRecord = default;
        Area.CurrentDirectorySector = 0;
        _fileInfoValid = false;
        ReportCdStatus(Area.Status);
        Area.Reg.Hirq = HirqCmok | HirqDchg;
        UpdateInterruptLine();
    }

    private void EndDataTransfer()
    {
        if (Area.CdWordNumber != 0)
        {
            Area.Reg.Cr1 = (ushort)(((uint)Area.Status << 8) | ((Area.CdWordNumber >> 17) & 0xFF));
            Area.Reg.Cr2 = unchecked((ushort)(Area.CdWordNumber >> 1));
            Area.Reg.Cr3 = 0;
            Area.Reg.Cr4 = 0;
        }
        else
        {
            Area.Reg.Cr1 = (ushort)((Area.Status << 8) | 0xFF);
            Area.Reg.Cr2 = 0xFFFF;
            Area.Reg.Cr3 = 0;
            Area.Reg.Cr4 = 0;
        }

        switch (Area.DataTransferType)
        {
            case TransferGetSector:
                SetHirq(HirqEhst);
                break;
            case TransferGetDeleteSector:
                if (Area.Partitions[Area.DataTransferPartitionNumber].Size <= 0)
                    break;

                Area.DataTransferType = TransferInvalid;
                DeleteTransferredSectors();
                if (Area.FreeBlockCount == MaxBlocks)
                    Area.OneSectorStored = false;
                SetHirq(HirqEhst);
                break;
        }

        Area.CdWordNumber = 0;
        SetHirq(HirqCmok);
    }

    private void PlayDisc()
    {
        uint startPosition = ((uint)(_executingCommandData[0] & 0xFF) << 16) | _executingCommandData[1];
        uint endPosition = ((uint)(_executingCommandData[2] & 0xFF) << 16) | _executingCommandData[3];
        uint playMode = (uint)(_executingCommandData[2] >> 8);

        if (startPosition != 0xFFFFFF && playMode != 0xFF)
        {
            if ((startPosition & 0x800000) != 0)
            {
                Area.PlayFad = startPosition & 0xFFFFF;
                SetupDefaultPlayStats(FadToTrack(Area.PlayFad), writeFad: false);
                if ((playMode & 0x80) == 0)
                    Area.Fad = Area.PlayFad;
            }
            else
            {
                if (startPosition == 0)
                    startPosition = 0x0100;

                byte track = unchecked((byte)(startPosition >> 8));
                if ((playMode & 0x80) == 0)
                {
                    SetupDefaultPlayStats(track, writeFad: true);
                    Area.PlayFad = Area.Fad;
                    Area.Track = track;
                    Area.Index = unchecked((byte)startPosition);
                }
                else
                {
                    SetupDefaultPlayStats(track, writeFad: false);
                }
            }
        }

        playMode &= 0x7F;
        if (playMode != 0x7F)
            Area.MaxRepeat = playMode;

        if (endPosition != 0xFFFFFF)
        {
            if ((endPosition & 0x800000) != 0)
                Area.PlayEndFad = Area.PlayFad + (endPosition & 0xFFFFF);
            else if (endPosition != 0)
                Area.PlayEndFad = TrackToFad(unchecked((ushort)(endPosition | 0x63)));
            else
                Area.PlayEndFad = TrackToFad(0xFFFF);
        }

        _initialSeekPhase = InitialSeekPhase.None;
        _playbackPositionFad = null;
        SetTiming(1);
        Area.Status = StatusPlay;
        Area.PlayType = PlayTypeSector;
        Area.CdInterface!.ReadAheadFad(Area.Fad);
        ReportCdStatus(Area.Status);
        SetHirq(HirqCmok);
    }

    private void SeekDisc()
    {
        if ((_executingCommandData[0] & 0x80) != 0)
        {
            uint fad = ((uint)(_executingCommandData[0] & 0x0F) << 16) | _executingCommandData[1];
            Area.Status = StatusPause;
            for (int i = 0; i < 16; i++)
            {
                uint trackFad = Area.Toc[i] & 0x00FFFFFF;
                if (trackFad >= fad)
                {
                    SetupDefaultPlayStats((byte)i, writeFad: true);
                    Area.Fad = fad;
                    break;
                }
            }
        }
        else if ((_executingCommandData[1] >> 8) != 0)
        {
            Area.Status = StatusPause;
            SetupDefaultPlayStats((byte)(_executingCommandData[1] >> 8), writeFad: true);
            Area.Index = unchecked((byte)_executingCommandData[1]);
        }
        else
        {
            Area.Status = StatusStandby;
            Area.Options = 0xFF;
            Area.RepeatCount = 0xFF;
            Area.ControlAddress = 0xFF;
            Area.Track = 0xFF;
            Area.Index = 0xFF;
            Area.Fad = uint.MaxValue;
        }

        _connections.ClearCdda();
        _initialSeekPhase = InitialSeekPhase.None;
        _playbackPositionFad = null;
        SetTiming(0);
        ReportCdStatus(Area.Status);
        SetHirq(HirqCmok);
    }

    private void ScanDisc()
    {
        int direction = _executingCommandData[0] & 0xFF;
        if (direction >= 2)
        {
            ReportCdStatus(StatusReject);
        }
        else
        {
            ReportCdStatus(StatusBusy);
            if (GetMediaDriveStatus() is 0 or 1)
            {
                _connections.ClearCdda();
                if (Area.Toc[0] == uint.MaxValue)
                    RefreshMediaToc();

                _scanReverse = direction != 0;
                _scanCounter = 0;
                _scanSector = Area.Fad == uint.MaxValue ? Area.Toc[0] & 0xFFFFFF : Area.Fad;
                Area.RepeatCount = 0;
                Area.Status = StatusScan;
                SetTiming(1);
                Area.CdInterface!.ReadAheadFad(_scanSector);
            }
        }

        SetHirq(HirqCmok);
    }

    private void AdvanceScan()
    {
        uint fad = _scanSector;
        uint firstFad = Area.Toc[0] & 0xFFFFFF;
        uint startFad = Area.PlayFad == uint.MaxValue ? firstFad : Math.Max(firstFad, Area.PlayFad);
        uint endFad = Math.Min(TrackToFad(0xFFFF), Area.PlayEndFad);
        Block work = Area.WorkBlock;

        if (fad < startFad || fad >= endFad || Area.CdInterface!.ReadSectorFad(fad, work.Data) == 0)
        {
            Area.Status = StatusPause;
            Area.IsAudio = 0;
            SetTiming(0);
            return;
        }

        work.Fad = Area.Fad = fad;
        Span<byte> q = stackalloc byte[12];
        if (UpdatePlaybackPosition(fad, work.Data.AsSpan(2352, 96), q))
        {
            Area.ControlAddress = q[0];
        }
        else
        {
            _subcodeRelativeFad = null;
            byte track = FadToTrack(fad);
            if (track is >= 1 and <= 99)
            {
                Area.ControlAddress = (byte)(Area.Toc[track - 1] >> 24);
                Area.Track = track;
                Area.Index = 1;
            }
        }

        bool isAudio = (Area.ControlAddress & 0x40) == 0;
        Area.IsAudio = isAudio ? 1 : 0;
        SetTiming(1);
        if (isAudio)
        {
            Span<byte> audio = stackalloc byte[2352];
            for (int offset = 0; offset < audio.Length; offset += sizeof(short))
            {
                short sample = BinaryPrimitives.ReadInt16LittleEndian(work.Data.AsSpan(offset));
                BinaryPrimitives.WriteInt16LittleEndian(audio[offset..], (short)(sample >> 2));
            }
            _connections.ReceiveCdda(audio);
        }

        // Scan cadence and sector increments from Mednafen cdb.cpp.
        _scanCounter += isAudio ? 2 : 1;
        if (_scanCounter < 6)
        {
            _scanSector = fad + 1;
        }
        else
        {
            _scanCounter = 0;
            const ulong rounding = 1UL << 31;
            if (_scanReverse)
            {
                uint step = 104 + (uint)((2180000UL * fad + rounding) >> 32);
                _scanSector = fad - Math.Min(fad, step);
            }
            else
            {
                _scanSector = fad + 102 + (uint)((1773936UL * fad + rounding) >> 32);
            }
            if (_scanSector >= startFad && _scanSector < endFad)
                Area.CdInterface!.ReadAheadFad(_scanSector);
        }
    }

    private void GetSubcodeQRw()
    {
        int type = _executingCommandData[0] & 0xFF;
        if (type > 1)
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok);
            return;
        }

        Area.Reg.Cr1 = (ushort)((Area.Status | StatusTransfer) << 8);
        Area.Reg.Cr3 = 0;
        Area.TransferCount = 0;
        Area.CdWordNumber = 0;
        switch (type)
        {
            case 0:
            {
                Area.Reg.Cr2 = 5;
                Area.Reg.Cr4 = 0;
                Area.InfoTransferType = 3;
                break;
            }
            case 1:
                Area.Reg.Cr2 = 12;
                uint fad = ReportedFad;
                if (fad != _lastSubcodeFad)
                {
                    _lastSubcodeFad = fad;
                    _subcodeGroup = 0;
                }
                else
                {
                    _subcodeGroup = unchecked((ushort)(_subcodeGroup + 1));
                }
                Area.Reg.Cr4 = _subcodeGroup;

                if (_subcodeGroup < 4 && Area.WorkBlock.Fad == fad)
                {
                    for (int i = 0; i < 24; i++)
                        Area.TransferSubcodeRw[i] = (byte)(Area.WorkBlock.Data[2352 + i + 24 * _subcodeGroup] & 0x3F);
                }
                else
                {
                    Array.Fill(Area.TransferSubcodeRw, (byte)0xFF);
                }
                Area.InfoTransferType = 4;
                break;
        }

        Area.CommandExecutionTiming = CdClocksToMicroseconds(128);
        Area.DelayedHirq = HirqDrdy;
        SetHirq(HirqCmok);
    }

    private void WriteSubcodeQReport(Span<byte> report)
    {
        uint fad = ReportedFad & 0xFFFFFF;
        uint relativeFad = 0xFFFFFF;
        byte track = Area.Track;
        byte index = Area.Index;
        byte controlAddress = Area.ControlAddress;
        if (_playbackPositionFad.HasValue && _subcodeRelativeFad is uint relative)
            relativeFad = relative;
        else if (track is >= 1 and <= 99 && Area.Toc[track - 1] != 0xFFFFFFFF)
        {
            relativeFad = unchecked(fad - (Area.Toc[track - 1] & 0xFFFFFF));
        }

        report[0] = controlAddress;
        report[1] = track;
        report[2] = index;
        report[3] = unchecked((byte)(relativeFad >> 16));
        report[4] = unchecked((byte)(relativeFad >> 8));
        report[5] = unchecked((byte)relativeFad);
        report[6] = 0;
        report[7] = unchecked((byte)(fad >> 16));
        report[8] = unchecked((byte)(fad >> 8));
        report[9] = unchecked((byte)fad);
    }

    private static byte FromBcd(byte value) => (byte)((value >> 4) * 10 + (value & 0x0F));

    private static uint SubcodeMsfToFad(ReadOnlySpan<byte> msf) =>
        (uint)((FromBcd(msf[0]) * 60 + FromBcd(msf[1])) * 75 + FromBcd(msf[2]));

    private void SetCdDeviceConnection()
    {
        int number = _executingCommandData[2] >> 8;
        if (number == 0xFF)
            Area.CdDeviceOutput = null;
        else if (number < MaxSelectors)
            Area.CdDeviceOutput = Area.Filters[number];

        Area.CdDeviceOutputNumber = (byte)number;
        ReportFilterChange();
    }

    private void GetCdDeviceConnection()
    {
        Area.Reg.Cr1 = (ushort)(Area.Status << 8);
        Area.Reg.Cr2 = 0;
        Area.Reg.Cr3 = (ushort)(Area.CdDeviceOutputNumber << 8);
        Area.Reg.Cr4 = 0;
        SetHirq(HirqCmok);
    }

    private void GetLastBufferDestination()
    {
        Area.Reg.Cr1 = (ushort)(Area.Status << 8);
        Area.Reg.Cr2 = 0;
        Area.Reg.Cr3 = (ushort)(Area.LastBuffer << 8);
        Area.Reg.Cr4 = 0;
        SetHirq(HirqCmok);
    }

    private void SetFilterRange()
    {
        if (!TryGetCommandFilterNumber(out int filterNumber))
            return;

        Filter filter = Area.Filters[filterNumber];
        filter.Fad = ((uint)(_executingCommandData[0] & 0xFF) << 16) | _executingCommandData[1];
        filter.Range = ((uint)(_executingCommandData[2] & 0xFF) << 16) | _executingCommandData[3];
        ReportFilterChange();
    }

    private void GetFilterRange()
    {
        if (!TryGetCommandFilterNumber(out int filterNumber))
            return;

        Filter filter = Area.Filters[filterNumber];
        Area.Reg.Cr1 = (ushort)(((uint)Area.Status << 8) | ((filter.Fad & 0xFF0000) >> 16));
        Area.Reg.Cr2 = unchecked((ushort)filter.Fad);
        Area.Reg.Cr3 = (ushort)((filter.Range & 0xFF0000) >> 16);
        Area.Reg.Cr4 = unchecked((ushort)filter.Range);
        SetHirq(HirqCmok);
    }

    private void SetFilterSubheaderConditions()
    {
        if (!TryGetCommandFilterNumber(out int filterNumber))
            return;

        Filter filter = Area.Filters[filterNumber];
        filter.Channel = unchecked((byte)_executingCommandData[0]);
        filter.SubmodeMask = (byte)(_executingCommandData[1] >> 8);
        filter.CodingInfoMask = unchecked((byte)_executingCommandData[1]);
        filter.FileId = unchecked((byte)_executingCommandData[2]);
        filter.SubmodeValue = (byte)(_executingCommandData[3] >> 8);
        filter.CodingInfoValue = unchecked((byte)_executingCommandData[3]);
        ReportFilterChange();
    }

    private void GetFilterSubheaderConditions()
    {
        if (!TryGetCommandFilterNumber(out int filterNumber))
            return;

        Filter filter = Area.Filters[filterNumber];
        Area.Reg.Cr1 = (ushort)((Area.Status << 8) | filter.Channel);
        Area.Reg.Cr2 = (ushort)((filter.SubmodeMask << 8) | filter.CodingInfoMask);
        Area.Reg.Cr3 = filter.FileId;
        Area.Reg.Cr4 = (ushort)((filter.SubmodeValue << 8) | filter.CodingInfoValue);
        SetHirq(HirqCmok | HirqEsel);
    }

    private void SetFilterMode()
    {
        if (!TryGetCommandFilterNumber(out int filterNumber))
            return;

        Filter filter = Area.Filters[filterNumber];
        filter.Mode = unchecked((byte)_executingCommandData[0]);

        if ((filter.Mode & 0x80) != 0)
        {
            filter.Mode = 0;
            filter.Fad = 0;
            filter.Range = 0;
            filter.Channel = 0;
            filter.SubmodeMask = 0;
            filter.CodingInfoMask = 0;
            filter.SubmodeValue = 0;
            filter.CodingInfoValue = 0;
        }

        ReportFilterChange();
    }

    private static void ResetFilterConditions(Filter filter)
    {
        filter.Mode = 0;
        filter.Fad = 0;
        filter.Range = 0xFFFFFFFF;
        filter.Channel = 0;
        filter.FileId = 0;
        filter.SubmodeMask = 0;
        filter.CodingInfoMask = 0;
        filter.SubmodeValue = 0;
        filter.CodingInfoValue = 0;
    }

    private void GetFilterMode()
    {
        if (!TryGetCommandFilterNumber(out int filterNumber))
            return;

        Filter filter = Area.Filters[filterNumber];
        Area.Reg.Cr1 = (ushort)((Area.Status << 8) | filter.Mode);
        Area.Reg.Cr2 = 0;
        Area.Reg.Cr3 = 0;
        Area.Reg.Cr4 = 0;
        SetHirq(HirqCmok | HirqEsel);
    }

    private void SetFilterConnection()
    {
        if (!TryGetCommandFilterNumber(out int filterNumber))
            return;

        Filter filter = Area.Filters[filterNumber];
        if ((_executingCommandData[0] & 1) != 0)
            filter.TrueConnection = (byte)(_executingCommandData[1] >> 8);
        if ((_executingCommandData[0] & 2) != 0)
            filter.FalseConnection = unchecked((byte)_executingCommandData[1]);

        ReportFilterChange();
    }

    private void GetFilterConnection()
    {
        if (!TryGetCommandFilterNumber(out int filterNumber))
            return;

        Filter filter = Area.Filters[filterNumber];
        Area.Reg.Cr1 = (ushort)(Area.Status << 8);
        Area.Reg.Cr2 = (ushort)((filter.TrueConnection << 8) | filter.FalseConnection);
        Area.Reg.Cr3 = 0;
        Area.Reg.Cr4 = 0;
        SetHirq(HirqCmok);
    }

    private bool TryGetCommandFilterNumber(out int filterNumber)
    {
        filterNumber = _executingCommandData[2] >> 8;
        if (filterNumber < MaxSelectors)
            return true;

        ReportCdStatus(StatusReject);
        SetHirq(HirqCmok);
        return false;
    }

    private void ReportFilterChange()
    {
        ReportCdStatus(Area.Status);
        SetHirq(HirqCmok | HirqEsel);
    }

    private void ResetSelector()
    {
        byte flags = unchecked((byte)_executingCommandData[0]);
        if (flags == 0)
        {
            int bufferNumber = _executingCommandData[2] >> 8;
            if (bufferNumber < MaxSelectors)
                ClearSelectorPartition(Area.Partitions[bufferNumber]);

            if (Area.FreeBlockCount > 0)
                Area.BufferFull = 0;
            if (Area.FreeBlockCount == MaxBlocks)
                Area.OneSectorStored = false;
            if (Area.FreeBlockCount == MaxBlocks || Area.DataTransferPartitionNumber == bufferNumber)
                Area.DataTransferType = TransferInvalid;
        }
        else
        {
            if ((flags & 0x80) != 0)
            {
                for (int i = 0; i < MaxSelectors; i++)
                    Area.Filters[i].FalseConnection = 0xFF;
            }

            if ((flags & 0x40) != 0)
            {
                for (int i = 0; i < MaxSelectors; i++)
                    Area.Filters[i].TrueConnection = (byte)i;
            }

            if ((flags & 0x10) != 0)
            {
                for (int i = 0; i < MaxSelectors; i++)
                    ResetFilterConditions(Area.Filters[i]);
            }

            if ((flags & 4) != 0)
            {
                Area.BufferFull = 0;
                for (int i = 0; i < MaxSelectors; i++)
                {
                    Partition partition = Area.Partitions[i];
                    partition.Size = -1;
                    partition.BlockCount = 0;
                    for (int j = 0; j < MaxBlocks; j++)
                    {
                        partition.Blocks[j] = null;
                        partition.BlockNumbers[j] = 0xFF;
                    }
                }

                for (int i = 0; i < MaxBlocks; i++)
                {
                    Area.Blocks[i].Size = -1;
                    Array.Clear(Area.Blocks[i].Data, 0, 2352);
                }

                Area.OneSectorStored = false;
                Area.DataTransferType = TransferInvalid;
            }
        }

        ReportCdStatus(Area.Status);
        SetHirq(HirqCmok | HirqEsel);
    }

    private void ClearSelectorPartition(Partition partition)
    {
        for (int i = 0; i < partition.BlockCount; i++)
        {
            FreeBlock(partition.Blocks[i]);
            partition.Blocks[i] = null;
            partition.BlockNumbers[i] = 0xFF;
        }

        partition.Size = -1;
        partition.BlockCount = 0;
    }

    private void GetBufferSize()
    {
        Area.Reg.Cr1 = (ushort)(Area.Status << 8);
        Area.Reg.Cr2 = unchecked((ushort)Area.FreeBlockCount);
        Area.Reg.Cr3 = MaxSelectors << 8;
        Area.Reg.Cr4 = MaxBlocks;
        SetHirq(HirqCmok);
    }

    private void GetSectorNumber()
    {
        int bufferNumber = _executingCommandData[2] >> 8;
        if (bufferNumber >= MaxSelectors)
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok);
            return;
        }

        Partition partition = Area.Partitions[bufferNumber];
        Area.Reg.Cr4 = partition.Size == -1 ? (ushort)0 : partition.BlockCount;
        Area.Reg.Cr1 = (ushort)(Area.Status << 8);
        Area.Reg.Cr2 = 0;
        Area.Reg.Cr3 = 0;
        SetHirq(HirqCmok | HirqDrdy);
    }

    private void CalculateActualSize()
    {
        ushort sectorOffset = _executingCommandData[1];
        int bufferNumber = _executingCommandData[2] >> 8;
        ushort sectorCount = _executingCommandData[3];
        if (bufferNumber >= MaxSelectors)
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok);
            return;
        }

        Area.CalculatedSize = 0;
        Partition partition = Area.Partitions[bufferNumber];
        if (partition.Size != 0 && sectorOffset < partition.Blocks.Length)
        {
            for (int i = 0; i < sectorCount; i++)
            {
                if (partition.Blocks[sectorOffset] is Block block)
                    Area.CalculatedSize += (uint)(block.Size / 2);
            }
        }

        ReportCdStatus(Area.Status);
        SetHirq(HirqCmok | HirqEsel);
    }

    private void GetActualSize()
    {
        Area.Reg.Cr1 = (ushort)(((uint)Area.Status << 8) | ((Area.CalculatedSize >> 16) & 0xFF));
        Area.Reg.Cr2 = unchecked((ushort)Area.CalculatedSize);
        Area.Reg.Cr3 = 0;
        Area.Reg.Cr4 = 0;
        SetHirq(HirqCmok | HirqEsel);
    }

    private void GetSectorInfo()
    {
        int sectorNumber = _executingCommandData[1] & 0xFF;
        int bufferNumber = _executingCommandData[2] >> 8;
        if (bufferNumber < MaxSelectors)
        {
            Partition partition = Area.Partitions[bufferNumber];
            if (sectorNumber < partition.BlockCount)
            {
                Block block = partition.Blocks[sectorNumber]!;
                uint fad = block.Fad;
                Area.Reg.Cr1 = (ushort)(((uint)Area.Status << 8) | ((fad >> 16) & 0xFF));
                Area.Reg.Cr2 = unchecked((ushort)fad);
                Area.Reg.Cr3 = (ushort)((block.FileNumber << 8) | block.ChannelNumber);
                Area.Reg.Cr4 = (ushort)((block.Submode << 8) | block.CodingInfo);
                SetHirq(HirqCmok | HirqEsel);
                return;
            }
        }

        Area.Reg.Cr1 = (ushort)((StatusReject << 8) | (Area.Reg.Cr1 & 0xFF));
        SetHirq(HirqCmok | HirqEsel);
    }

    private void ExecuteFadSearch()
    {
        int offset = _executingCommandData[1];
        int partitionNumber = _executingCommandData[2] >> 8;
        uint targetAddress = ((uint)(_executingCommandData[2] & 0xFF) << 16) | _executingCommandData[3];

        if (partitionNumber >= MaxSelectors)
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok);
            return;
        }

        Partition partition = Area.Partitions[partitionNumber];
        if (offset == 0xFFFF)
            offset = partition.BlockCount - 1;

        if (offset < 0 || offset >= partition.BlockCount)
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok);
            return;
        }

        Area.FadSearchSector = 0xFFFF;
        Area.FadSearchPartition = (byte)partitionNumber;
        Area.FadSearchAddress = 0;
        bool matchMade = false;

        for (int sector = offset; sector < partition.BlockCount; sector++)
        {
            uint address = partition.Blocks[sector]!.Fad;
            if (address <= targetAddress && (!matchMade || address > Area.FadSearchAddress))
            {
                Area.FadSearchSector = (ushort)sector;
                Area.FadSearchAddress = address;
                matchMade = true;
            }
        }

        ReportCdStatus(Area.Status);
        SetHirq(HirqCmok);
        Area.CommandExecutionTiming = (300u * 1_000_000 + CdClockHz - 1) / CdClockHz;
        Area.DelayedHirq = HirqEsel;
    }

    private void GetFadSearchResults()
    {
        Area.Reg.Cr1 = (ushort)(Area.Status << 8);
        Area.Reg.Cr2 = Area.FadSearchSector;
        Area.Reg.Cr3 = (ushort)(((uint)Area.FadSearchPartition << 8) | (Area.FadSearchAddress >> 16));
        Area.Reg.Cr4 = unchecked((ushort)Area.FadSearchAddress);
        SetHirq(HirqCmok);
    }

    private void SetSectorLength()
    {
        int getLength = _executingCommandData[0] & 0xFF;
        int putLength = _executingCommandData[1] >> 8;
        switch (getLength)
        {
            case 0: Area.GetSectorSize = 2048; break;
            case 1: Area.GetSectorSize = 2336; break;
            case 2: Area.GetSectorSize = 2340; break;
            case 3: Area.GetSectorSize = 2352; break;
        }

        switch (putLength)
        {
            case 0: Area.PutSectorSize = 2048; break;
            case 1: Area.PutSectorSize = 2336; break;
            case 2: Area.PutSectorSize = 2340; break;
            case 3: Area.PutSectorSize = 2352; break;
        }

        ReportCdStatus(Area.Status);
        SetHirq(HirqCmok | HirqEsel);
    }

    private static bool CalculateSectorOffsetNumber(Partition partition, ref uint sectorOffset, ref uint sectorCount)
    {
        uint blockCount = partition.BlockCount;
        if (blockCount == 0)
            return false;

        if (sectorOffset == 0xFFFF)
            sectorOffset = blockCount - 1;

        if (sectorOffset >= blockCount)
            return false;

        uint available = blockCount - sectorOffset;
        if (sectorCount == 0xFFFF)
            sectorCount = available;

        return sectorCount != 0 && sectorCount <= available;
    }

    private void GetSectorData()
    {
        uint sectorOffset = _executingCommandData[1];
        int bufferNumber = _executingCommandData[2] >> 8;
        uint sectorCount = _executingCommandData[3];

        if (bufferNumber >= MaxSelectors)
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok | HirqEhst);
            return;
        }

        Partition partition = Area.Partitions[bufferNumber];
        if (!CalculateSectorOffsetNumber(partition, ref sectorOffset, ref sectorCount))
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok | HirqEhst);
            return;
        }

        BeginSectorReadTransfer(bufferNumber, (int)sectorOffset, (int)sectorCount, TransferGetSector);
    }

    private void DeleteSectorData()
    {
        uint sectorOffset = _executingCommandData[1];
        int bufferNumber = _executingCommandData[2] >> 8;
        uint sectorCount = _executingCommandData[3];

        if (bufferNumber >= MaxSelectors)
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok | HirqEhst);
            return;
        }

        Partition partition = Area.Partitions[bufferNumber];
        if (!CalculateSectorOffsetNumber(partition, ref sectorOffset, ref sectorCount))
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok | HirqEhst);
            return;
        }

        for (uint i = sectorOffset; i < unchecked(sectorOffset + sectorCount); i++)
        {
            partition.Size -= partition.Blocks[i]!.Size;
            FreeBlock(partition.Blocks[i]);
            partition.Blocks[i] = null;
            partition.BlockNumbers[i] = 0xFF;
        }

        SortBlocks(partition);
        partition.BlockCount = unchecked((byte)(partition.BlockCount - (byte)sectorCount));

        if (Area.FreeBlockCount == MaxBlocks)
            Area.OneSectorStored = false;

        ReportCdStatus(Area.Status);
        SetHirq(HirqCmok | HirqEhst);
    }

    private void GetThenDeleteSectorData()
    {
        uint sectorOffset = _executingCommandData[1];
        int bufferNumber = _executingCommandData[2] >> 8;
        uint sectorCount = _executingCommandData[3];

        if (bufferNumber >= MaxSelectors)
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok | HirqEhst);
            return;
        }

        Partition partition = Area.Partitions[bufferNumber];
        if (!CalculateSectorOffsetNumber(partition, ref sectorOffset, ref sectorCount))
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok | HirqEhst);
            return;
        }

        BeginSectorReadTransfer(bufferNumber, (int)sectorOffset, (int)sectorCount, TransferGetDeleteSector);
    }

    private void BeginSectorReadTransfer(int bufferNumber, int offset, int count, int type)
    {
        Area.CdWordNumber = 0;
        Area.DataTransferType = type;
        Area.DataTransferPartitionNumber = (byte)bufferNumber;
        Area.DataTransferSectorPosition = (ushort)offset;
        Area.DataTransferOffset = 0;
        Area.DataTransferredSectorCount = 0;
        Area.DataSectorsToTransfer = (ushort)count;

        ReportCdStatus(Area.Status);
        SetHirq(HirqCmok | HirqDrdy | HirqEhst);
    }

    private void PutSectorData()
    {
        int bufferNumber = _executingCommandData[2] >> 8;
        uint sectorCount = _executingCommandData[3];
        if (bufferNumber >= MaxSelectors)
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok | HirqEhst);
            return;
        }

        if (sectorCount > Area.FreeBlockCount)
        {
            SetHirq(HirqCmok | HirqEhst);
            return;
        }

        Partition partition = Area.Partitions[bufferNumber];
        partition.Size = 0;
        for (uint i = 0; i < sectorCount; i++)
        {
            int index = partition.BlockCount;
            Block block = AllocateBlock(ref partition.BlockNumbers[index], (int)Area.PutSectorSize)!;
            partition.Blocks[index] = block;
            block.Fad = i;
            partition.BlockCount++;
            partition.Size += (int)Area.PutSectorSize;
        }

        Area.CdWordNumber = 0;
        Area.DataTransferType = TransferPutSector;
        Area.DataTransferPartitionNumber = (byte)bufferNumber;
        Area.DataTransferSectorPosition = 0;
        Area.DataTransferOffset = 0;
        Area.DataTransferredSectorCount = 0;
        Area.DataSectorsToTransfer = (ushort)sectorCount;
        SetHirq(HirqCmok | HirqDrdy);
    }

    private void DisconnectFilterInput(int filterNumber)
    {
        if (Area.CdDeviceOutputNumber == filterNumber)
        {
            Area.CdDeviceOutput = null;
            Area.CdDeviceOutputNumber = 0xFF;
        }

        foreach (Filter filter in Area.Filters)
        {
            if (filter.FalseConnection == filterNumber)
                filter.FalseConnection = 0xFF;
        }
    }

    private static bool MatchesSectorFilter(Filter filter, Block block)
    {
        bool matches = true;
        if (block.IsMode2)
        {
            if ((filter.Mode & 1) != 0 && block.FileNumber != filter.FileId)
                matches = false;
            if ((filter.Mode & 2) != 0 && block.ChannelNumber != filter.Channel)
                matches = false;
            if ((filter.Mode & 4) != 0 && (block.Submode & filter.SubmodeMask) != filter.SubmodeValue)
                matches = false;
            if ((filter.Mode & 8) != 0 && (block.CodingInfo & filter.CodingInfoMask) != filter.CodingInfoValue)
                matches = false;
            if ((filter.Mode & 0x10) != 0)
                matches = !matches;
        }

        if ((filter.Mode & 0x40) != 0
            && (block.Fad < filter.Fad || block.Fad >= unchecked(filter.Fad + filter.Range)))
            matches = false;

        return matches;
    }

    private int FindSectorPartition(int filterNumber, Block block, out byte lastBuffer)
    {
        lastBuffer = 0xFF;
        for (int remaining = MaxSelectors; remaining > 0 && filterNumber < MaxSelectors; remaining--)
        {
            Filter filter = Area.Filters[filterNumber];
            if (MatchesSectorFilter(filter, block))
            {
                lastBuffer = filter.TrueConnection;
                return lastBuffer;
            }

            filterNumber = filter.FalseConnection;
            lastBuffer = filter.FalseConnection;
        }

        return 0xFF;
    }

    private void RouteSectorBlock(int filterNumber, byte blockNumber)
    {
        Block block = Area.Blocks[blockNumber];
        int partitionNumber = FindSectorPartition(filterNumber, block, out _);
        if (partitionNumber >= MaxSelectors)
        {
            FreeBlock(block);
            return;
        }

        Partition destination = Area.Partitions[partitionNumber];
        if (destination.Size == -1)
            destination.Size = 0;

        int position = destination.BlockCount++;
        destination.Blocks[position] = block;
        destination.BlockNumbers[position] = blockNumber;
        destination.Size += block.Size;
    }

    private Block? GetTransferBlock() =>
        Area.Partitions[Area.DataTransferPartitionNumber].Blocks[Area.DataTransferredSectorCount];

    private void DeleteTransferredSectors()
    {
        Partition partition = Area.Partitions[Area.DataTransferPartitionNumber];
        int end = Area.DataTransferSectorPosition + Area.DataSectorsToTransfer;
        for (int i = Area.DataTransferSectorPosition; i < end; i++)
        {
            FreeBlock(partition.Blocks[i]);
            partition.Blocks[i] = null;
            partition.BlockNumbers[i] = 0xFF;
        }

        SortBlocks(partition);
        partition.Size = unchecked(partition.Size - (int)Area.CdWordNumber);
        partition.BlockCount = unchecked((byte)(partition.BlockCount - Area.DataSectorsToTransfer));
    }

    private void CopySectorData()
    {
        if (!GetSectorCopyRange(out Partition source, out int filterNumber, out uint offset, out uint count))
            return;

        if (count > Area.FreeBlockCount)
        {
            ReportCdStatus((byte)(Area.Status | StatusWait));
            SetHirq(HirqCmok);
            return;
        }

        DisconnectFilterInput(filterNumber);

        for (uint i = 0; i < count; i++)
        {
            Block original = source.Blocks[offset + i]!;
            byte blockNumber = 0xFF;
            Block copy = AllocateBlock(ref blockNumber, original.Size)!;
            original.Data.CopyTo(copy.Data, 0);
            copy.Fad = original.Fad;
            copy.IsMode2 = original.IsMode2;
            copy.ChannelNumber = original.ChannelNumber;
            copy.FileNumber = original.FileNumber;
            copy.Submode = original.Submode;
            copy.CodingInfo = original.CodingInfo;
            RouteSectorBlock(filterNumber, blockNumber);
        }

        ReportSectorCopyCompletion();
    }

    private void MoveSectorData()
    {
        if (!GetSectorCopyRange(out Partition source, out int filterNumber, out uint offset, out uint count))
            return;

        DisconnectFilterInput(filterNumber);

        int sourceIndex = (int)offset;
        for (uint i = 0; i < count; i++)
        {
            Block block = source.Blocks[sourceIndex]!;
            byte blockNumber = source.BlockNumbers[sourceIndex];
            source.BlockCount--;
            int trailingCount = source.BlockCount - sourceIndex;
            Array.Copy(source.Blocks, sourceIndex + 1, source.Blocks, sourceIndex, trailingCount);
            Array.Copy(source.BlockNumbers, sourceIndex + 1, source.BlockNumbers, sourceIndex, trailingCount);
            source.Blocks[source.BlockCount] = null;
            source.BlockNumbers[source.BlockCount] = 0xFF;
            source.Size -= block.Size;

            RouteSectorBlock(filterNumber, blockNumber);
        }

        ReportSectorCopyCompletion();
    }

    private void ReportSectorCopyCompletion()
    {
        ReportCdStatus(Area.Status);
        SetHirq(HirqCmok);
        Area.CommandExecutionTiming = (300u * 1_000_000 + CdClockHz - 1) / CdClockHz;
        Area.DelayedHirq = Area.FreeBlockCount == 0 ? (ushort)(HirqBful | HirqEcpy) : HirqEcpy;
    }

    private bool GetSectorCopyRange(out Partition source, out int filterNumber, out uint offset, out uint count)
    {
        int sourceNumber = _executingCommandData[2] >> 8;
        filterNumber = _executingCommandData[0] & 0xFF;
        offset = _executingCommandData[1];
        count = _executingCommandData[3];

        if (sourceNumber >= MaxSelectors || filterNumber >= MaxSelectors)
        {
            source = null!;
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok);
            return false;
        }

        source = Area.Partitions[sourceNumber];
        if (!CalculateSectorOffsetNumber(source, ref offset, ref count))
        {
            ReportCdStatus((byte)(Area.Status | StatusWait));
            SetHirq(HirqCmok);
            return false;
        }

        return true;
    }

    private void GetCopyError()
    {
        Area.Reg.Cr1 = (ushort)(Area.Status << 8);
        Area.Reg.Cr2 = 0;
        Area.Reg.Cr3 = 0;
        Area.Reg.Cr4 = 0;
        SetHirq(HirqCmok);
    }

    private int ReadFileSystem(int filterNumber, uint fileId, bool isOffset)
    {
        Partition? partition;
        _fileInfoValid = false;

        DisconnectFilterInput(filterNumber);
        Filter filter = Area.Filters[filterNumber];
        Area.CdDeviceOutput = filter;
        Area.CdDeviceOutputNumber = (byte)filterNumber;
        filter.TrueConnection = (byte)filterNumber;
        filter.FalseConnection = 0xFF;
        byte xaFileNumber;
        if (isOffset)
        {
            if (Area.CurrentDirectorySector == 0)
                return -1;

            Area.CurrentDirectoryFileIdOffset = fileId - 2;
            xaFileNumber = Area.Files[0].Xa.FileNumber;
        }
        else
        {
            if (fileId == 0xFFFFFF)
            {
                if (Area.RootDirectoryRecord.RecordSize == 0)
                {
                    filter.Fad = 0;
                    filter.Range = 0;
                    filter.Mode = 0;
                }

                for (uint fad = 166; Area.RootDirectoryRecord.RecordSize == 0; fad++)
                {
                    partition = ReadUnfilteredSector(fad);
                    if (partition is null)
                        return -2;

                    int blockIndex = partition.BlockCount - 1;
                    Block block = partition.Blocks[blockIndex]!;
                    ReadOnlySpan<byte> userData = block.Data.AsSpan(0, block.Size);
                    byte type = userData[0];
                    bool valid = userData.Slice(1, 5).SequenceEqual("CD001"u8) && type != 0xFF;
                    if (valid && type == 1)
                        CopyDirectoryRecord(userData[0x9C..], ref Area.RootDirectoryRecord);

                    partition.Size -= block.Size;
                    FreeBlock(block);
                    partition.Blocks[blockIndex] = null;
                    partition.BlockNumbers[blockIndex] = 0xFF;
                    SortBlocks(partition);
                    partition.BlockCount--;

                    if (!valid || (type == 1 && Area.RootDirectoryRecord.RecordSize == 0))
                        return -2;
                }

                Area.CurrentDirectorySector = Area.RootDirectoryRecord.Lba;
                Area.CurrentDirectorySize = Area.RootDirectoryRecord.Size;
                Area.CurrentDirectoryFileIdOffset = 0;
                xaFileNumber = Area.RootDirectoryRecord.Xa.FileNumber;
            }
            else
            {
                if (Area.CurrentDirectorySector == 0)
                    return -1;

                uint fileIndex = fileId < 2 ? fileId : unchecked(fileId - Area.CurrentDirectoryFileIdOffset);
                ref DirectoryRecord record = ref Area.Files[fileIndex];
                Area.CurrentDirectorySector = unchecked(record.Lba - 150);
                Area.CurrentDirectorySize = record.Size;
                Area.CurrentDirectoryFileIdOffset = 0;
                xaFileNumber = record.Xa.FileNumber;
            }
        }

        _connections.ClearCdda();
        ClearSelectorPartition(Area.Partitions[filterNumber]);
        filter.Fad = Area.CurrentDirectorySector + 150;
        filter.Range = (uint)(((ulong)Area.CurrentDirectorySize + 2047) >> 11);
        filter.Mode = 0x40;
        filter.FileId = xaFileNumber;
        filter.Channel = 0;
        filter.SubmodeValue = filter.SubmodeMask = 0;
        filter.CodingInfoValue = filter.CodingInfoMask = 0;

        Array.Clear(Area.Files);
        Area.FileCount = 0;
        Area.FileInfoMore = false;
        uint firstFileId = Area.CurrentDirectoryFileIdOffset + 2;
        uint recordNumber = 0;
        uint directoryLba = Area.CurrentDirectorySector;
        uint remainingBytes = Area.CurrentDirectorySize;
        while (remainingBytes != 0 && !Area.FileInfoMore)
        {
            partition = ReadUnfilteredSector(directoryLba + 150);
            if (partition is null)
                return -2;

            int blockIndex = partition.BlockCount - 1;
            Block block = partition.Blocks[blockIndex]!;
            int sectorBytes = (int)Math.Min(remainingBytes, 2048u);
            ReadOnlySpan<byte> work = block.Data.AsSpan(0, block.Size)[..sectorBytes];
            bool invalidRecord = false;
            for (int offset = 0; offset < work.Length;)
            {
                int recordSize = work[offset];
                if (recordSize == 0)
                {
                    offset++;
                    continue;
                }

                if (recordNumber < 2 || recordNumber >= firstFileId)
                {
                    if (Area.FileCount == MaxFiles)
                    {
                        Area.FileInfoMore = true;
                        break;
                    }

                    ref DirectoryRecord record = ref Area.Files[Area.FileCount];
                    if (CopyDirectoryRecord(work[offset..], ref record) != 0)
                    {
                        invalidRecord = true;
                        break;
                    }
                    record.Lba += 150;
                    Area.FileCount++;
                }

                offset += recordSize;
                recordNumber++;
            }

            partition.Size -= block.Size;
            FreeBlock(block);
            partition.Blocks[blockIndex] = null;
            partition.BlockNumbers[blockIndex] = 0xFF;
            SortBlocks(partition);
            partition.BlockCount--;
            if (invalidRecord)
                return -2;
            directoryLba++;
            remainingBytes -= (uint)sectorBytes;
        }

        _fileInfoValid = true;
        return 0;
    }

    private void ChangeDirectory()
    {
        int filterNumber = _executingCommandData[2] >> 8;
        uint fileId = ((uint)(_executingCommandData[2] & 0xFF) << 16) | _executingCommandData[3];
        uint fileIndex = fileId < 2 ? fileId : unchecked(fileId - Area.CurrentDirectoryFileIdOffset);
        if (filterNumber >= MaxSelectors
            || (fileId != 0xFFFFFF && (!_fileInfoValid
                || fileIndex >= Area.FileCount || (fileId >= 2 && fileIndex < 2)
                || (Area.Files[fileIndex].Flags & 2) == 0)))
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok);
            return;
        }

        if (fileId == 0)
        {
            ReportCdStatus(Area.Status);
            SetHirq(HirqCmok);
            Area.CommandExecutionTiming = CdClocksToMicroseconds(400);
            Area.DelayedHirq = HirqEfls;
            return;
        }

        if (ReadFileSystem(filterNumber, fileId, false) != 0)
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok | HirqEfls);
            return;
        }

        ReportCdStatus(Area.Status);
        SetHirq(HirqCmok | HirqEfls);
    }

    private void ReadDirectory()
    {
        int filterNumber = _executingCommandData[2] >> 8;
        uint fileId = ((uint)(_executingCommandData[2] & 0xFF) << 16) | _executingCommandData[3];
        if (fileId < 2)
            fileId = 2;

        if (filterNumber >= MaxSelectors || !_fileInfoValid)
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok);
            return;
        }

        if (ReadFileSystem(filterNumber, fileId, true) != 0)
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok | HirqEfls);
            return;
        }

        ReportCdStatus(Area.Status);
        SetHirq(HirqCmok | HirqEfls);
    }

    private void GetFileSystemScope()
    {
        if (!_fileInfoValid)
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok);
            return;
        }

        uint firstFileId = Area.FileCount > 2 ? Area.CurrentDirectoryFileIdOffset + 2 : 0;
        Area.Reg.Cr1 = (ushort)(Area.Status << 8);
        Area.Reg.Cr2 = Area.FileCount > 2 ? (ushort)(Area.FileCount - 2) : (ushort)0;
        Area.Reg.Cr3 = (ushort)((Area.FileInfoMore ? 0u : 0x100u) | (firstFileId >> 16));
        Area.Reg.Cr4 = unchecked((ushort)firstFileId);
        SetHirq(HirqCmok);
    }

    private void GetFileInfo()
    {
        uint fileId = ((uint)(_executingCommandData[2] & 0xFF) << 16) | _executingCommandData[3];
        bool allFiles = fileId == 0xFFFFFF;
        uint fileIndex = fileId < 2 ? fileId : unchecked(fileId - Area.CurrentDirectoryFileIdOffset);
        if (!_fileInfoValid || (allFiles && Area.FileCount <= 2) || (!allFiles
            && (fileIndex >= Area.FileCount || (fileId >= 2 && fileIndex < 2))))
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok);
            return;
        }

        if (!allFiles)
            SetupFileInfoTransfer(fileIndex);

        Area.TransferCount = 0;
        Area.CdWordNumber = 0;
        Area.InfoTransferType = allFiles ? 2 : 1;
        Area.TransferFileInfoByteCount = allFiles ? (uint)(Area.FileCount - 2) * 12 : 12;
        Area.Reg.Cr1 = (ushort)((Area.Status | StatusTransfer) << 8);
        Area.Reg.Cr2 = (ushort)(Area.TransferFileInfoByteCount / 2);
        Area.Reg.Cr3 = 0;
        Area.Reg.Cr4 = 0;
        SetHirq(HirqCmok);
        Area.CommandExecutionTiming = CdClocksToMicroseconds(128);
        Area.DelayedHirq = HirqDrdy;
    }

    private void ReadFile()
    {
        uint offset = ((uint)(_executingCommandData[0] & 0xFF) << 16) | _executingCommandData[1];
        int filterNumber = _executingCommandData[2] >> 8;
        uint fileId = ((uint)(_executingCommandData[2] & 0xFF) << 16) | _executingCommandData[3];
        uint fileIndex = fileId < 2 ? fileId : unchecked(fileId - Area.CurrentDirectoryFileIdOffset);
        if (filterNumber >= MaxSelectors || !_fileInfoValid || GetMediaDriveStatus() is not (0 or 1)
            || fileIndex >= Area.FileCount || (fileId >= 2 && fileIndex < 2)
            || Area.Files[fileIndex].RecordSize == 0)
        {
            ReportCdStatus(StatusReject);
            SetHirq(HirqCmok);
            return;
        }

        ref DirectoryRecord record = ref Area.Files[fileIndex];
        uint startFad = (record.Lba + offset) & 0xFFFFFF;
        uint size = unchecked(((record.Size + 2047) >> 11) - offset);
        _connections.ClearCdda();

        Partition partition = Area.Partitions[filterNumber];
        for (int i = 0; i < partition.BlockCount; i++)
        {
            FreeBlock(partition.Blocks[i]);
            partition.Blocks[i] = null;
            partition.BlockNumbers[i] = 0xFF;
        }
        partition.BlockCount = 0;
        partition.Size = -1;

        DisconnectFilterInput(filterNumber);
        Filter filter = Area.Filters[filterNumber];
        Area.CdDeviceOutput = filter;
        Area.CdDeviceOutputNumber = (byte)filterNumber;
        filter.TrueConnection = (byte)filterNumber;
        filter.FalseConnection = 0xFF;
        filter.Fad = startFad;
        filter.Range = size;
        filter.Mode = 0x41;
        filter.FileId = record.Xa.FileNumber;
        filter.Channel = 0;
        filter.SubmodeValue = filter.SubmodeMask = 0;
        filter.CodingInfoValue = filter.CodingInfoMask = 0;

        Area.MaxRepeat = 0;
        Area.PlayFad = startFad;
        Area.PlayEndFad = (startFad + size) & 0x7FFFFF;
        Area.Options = 8;
        Area.PlayType = PlayTypeFile;

        ReportCdStatus(Area.Status);
        _seekOriginFad = Area.Fad == uint.MaxValue ? 0 : Area.Fad;
        Area.RepeatCount = 0;
        _seekToPlayback = true;
        _initialSeekPhase = InitialSeekPhase.Position;
        _initialSeekMicroseconds = CdClocksToMicroseconds(500);
        Area.IsAudio = 0;
        Area.Status = StatusBusy;
        SetHirq(HirqCmok);
    }

    private void AbortFile()
    {
        _initialSeekPhase = InitialSeekPhase.None;
        if ((Area.Status & 0xF) != StatusOpen && (Area.Status & 0xF) != StatusNoDisc)
            Area.Status = StatusPause;

        Area.OneSectorStored = false;
        Area.DataTransferType = TransferInvalid;
        Area.CdWordNumber = 0;
        ReportCdStatus(Area.Status);
        SetHirq(HirqCmok | HirqEfls);
    }

    private void MpegGetStatus()
    {
        ReportMpegStatus(Area.Status);
        SetHirq(HirqCmok | HirqMpcm);
    }

    private void MpegGetInterrupt()
    {
        uint interrupt = 0;
        interrupt &= Area.MpegInterruptMask;
        Area.Reg.Cr1 = (ushort)(((uint)Area.Status << 8) | ((interrupt >> 16) & 0xFF));
        Area.Reg.Cr2 = unchecked((ushort)interrupt);
        Area.Reg.Cr3 = 0;
        Area.Reg.Cr4 = 0;
        SetHirq(HirqCmok | HirqMpcm);
    }

    private void MpegSetInterruptMask()
    {
        Area.MpegInterruptMask = ((uint)(_executingCommandData[0] & 0xFF) << 16) | _executingCommandData[1];
        ReportMpegStatus(Area.Status);
        SetHirq(HirqCmok | HirqMpcm);
    }

    private void MpegInitialize()
    {
        if (Area.MpegAuthentication != 0)
            Area.Reg.Cr1 = (ushort)(Area.Status << 8);
        else
            Area.Reg.Cr1 = 0xFF00;

        if (_executingCommandData[1] == 1)
            SetHirq(HirqCmok | HirqMpcm | HirqMped | HirqMpst);
        else
            SetHirq(HirqCmok | HirqMped | HirqMpst);

        Area.Reg.Cr2 = 0;
        Area.Reg.Cr3 = 0;
        Area.Reg.Cr4 = 0;
    }

    private void MpegSetMode()
    {
        byte videoPlayMode = unchecked((byte)_executingCommandData[0]);
        byte decodeTimingMode = (byte)(_executingCommandData[1] >> 8);
        byte outputMode = unchecked((byte)_executingCommandData[1]);
        byte streamLayerMode = (byte)(_executingCommandData[2] >> 8);

        if (videoPlayMode != 0xFF)
            Area.Mode.VideoPlayMode = videoPlayMode;
        if (decodeTimingMode != 0xFF)
            Area.Mode.DecodeTimingMode = decodeTimingMode;
        if (outputMode != 0xFF)
            Area.Mode.OutputMode = outputMode;
        if (streamLayerMode != 0xFF)
            Area.Mode.StreamLayerMode = streamLayerMode;

        ReportMpegStatus(Area.Status);
        SetHirq(HirqCmok | HirqMpcm);
    }

    private void MpegPlay()
    {
        ReportMpegStatus(Area.Status);
        SetHirq(HirqCmok | HirqMpcm);
    }

    private void MpegSetDecodingMethod()
    {
        ReportMpegStatus(Area.Status);
        SetHirq(HirqCmok | HirqMpcm);
    }

    private void MpegSetConnection()
    {
        int next = _executingCommandData[2] >> 8;
        if (next == 0)
        {
            Area.MpegConnections[0].AudioConnection = unchecked((byte)_executingCommandData[0]);
            Area.MpegConnections[0].AudioLayer = (byte)(_executingCommandData[1] >> 8);
            Area.MpegConnections[0].AudioBuffer = unchecked((byte)_executingCommandData[1]);
            Area.MpegConnections[0].VideoConnection = unchecked((byte)_executingCommandData[2]);
            Area.MpegConnections[0].VideoLayer = (byte)(_executingCommandData[3] >> 8);
            Area.MpegConnections[0].VideoBuffer = unchecked((byte)_executingCommandData[3]);
        }
        else
        {
            Area.MpegConnections[1].AudioConnection = unchecked((byte)_executingCommandData[0]);
            Area.MpegConnections[1].AudioLayer = (byte)(_executingCommandData[1] >> 8);
            Area.MpegConnections[1].AudioBuffer = unchecked((byte)_executingCommandData[1]);
            Area.MpegConnections[1].VideoConnection = unchecked((byte)_executingCommandData[2]);
            Area.MpegConnections[1].VideoLayer = (byte)(_executingCommandData[3] >> 8);
            Area.MpegConnections[1].VideoBuffer = unchecked((byte)_executingCommandData[3]);
        }

        ReportMpegStatus(Area.Status);
        SetHirq(HirqCmok | HirqMpcm);
    }

    private void MpegGetConnection()
    {
        int next = _executingCommandData[2] >> 8;
        if (next == 0)
        {
            Area.Reg.Cr1 = (ushort)((Area.Status << 8) | Area.MpegConnections[0].AudioConnection);
            Area.Reg.Cr2 = (ushort)((Area.MpegConnections[0].AudioLayer << 8) | Area.MpegConnections[0].AudioBuffer);
            Area.Reg.Cr3 = Area.MpegConnections[0].VideoConnection;
            Area.Reg.Cr4 = (ushort)((Area.MpegConnections[0].VideoLayer << 8) | Area.MpegConnections[0].VideoBuffer);
        }
        else
        {
            Area.Reg.Cr1 = (ushort)((Area.Status << 8) | Area.MpegConnections[1].AudioConnection);
            Area.Reg.Cr2 = (ushort)((Area.MpegConnections[1].AudioLayer << 8) | Area.MpegConnections[1].AudioBuffer);
            Area.Reg.Cr3 = Area.MpegConnections[1].VideoConnection;
            Area.Reg.Cr4 = (ushort)((Area.MpegConnections[1].VideoLayer << 8) | Area.MpegConnections[1].VideoBuffer);
        }

        SetHirq(HirqCmok | HirqMpcm);
    }

    private void MpegSetStream()
    {
        int next = _executingCommandData[2] >> 8;
        if (next == 0)
        {
            Area.MpegStreams[0].AudioStream = unchecked((byte)_executingCommandData[0]);
            Area.MpegStreams[0].AudioStreamId = (byte)(_executingCommandData[1] >> 8);
            Area.MpegStreams[0].AudioChannel = unchecked((byte)_executingCommandData[1]);
            Area.MpegStreams[0].VideoStream = unchecked((byte)_executingCommandData[2]);
            Area.MpegStreams[0].VideoStreamId = (byte)(_executingCommandData[3] >> 8);
            Area.MpegStreams[0].VideoChannel = unchecked((byte)_executingCommandData[3]);
        }
        else
        {
            Area.MpegStreams[1].AudioStream = unchecked((byte)_executingCommandData[0]);
            Area.MpegStreams[1].AudioStreamId = (byte)(_executingCommandData[1] >> 8);
            Area.MpegStreams[1].AudioChannel = unchecked((byte)_executingCommandData[1]);
            Area.MpegStreams[1].VideoStream = unchecked((byte)_executingCommandData[2]);
            Area.MpegStreams[1].VideoStreamId = (byte)(_executingCommandData[3] >> 8);
            Area.MpegStreams[1].VideoChannel = unchecked((byte)_executingCommandData[3]);
        }

        ReportMpegStatus(Area.Status);
        SetHirq(HirqCmok | HirqMpcm);
    }

    private void MpegGetStream()
    {
        int next = _executingCommandData[2] >> 8;
        if (next == 0)
        {
            Area.Reg.Cr1 = (ushort)((Area.Status << 8) | Area.MpegStreams[0].AudioStream);
            Area.Reg.Cr2 = (ushort)((Area.MpegStreams[0].AudioStreamId << 8) | Area.MpegStreams[0].AudioChannel);
            Area.Reg.Cr3 = Area.MpegStreams[0].VideoStream;
            Area.Reg.Cr4 = (ushort)((Area.MpegStreams[0].VideoStreamId << 8) | Area.MpegStreams[0].VideoChannel);
        }
        else
        {
            Area.Reg.Cr1 = (ushort)((Area.Status << 8) | Area.MpegStreams[1].AudioStream);
            Area.Reg.Cr2 = (ushort)((Area.MpegStreams[1].AudioStreamId << 8) | Area.MpegStreams[1].AudioChannel);
            Area.Reg.Cr3 = Area.MpegStreams[1].VideoStream;
            Area.Reg.Cr4 = (ushort)((Area.MpegStreams[1].VideoStreamId << 8) | Area.MpegStreams[1].VideoChannel);
        }

        SetHirq(HirqCmok | HirqMpcm);
    }

    private void MpegDisplay()
    {
        ReportMpegStatus(Area.Status);
        SetHirq(HirqCmok | HirqMpcm);
    }

    private void MpegSetWindow()
    {
        ReportMpegStatus(Area.Status);
        SetHirq(HirqCmok | HirqMpcm);
    }

    private void MpegSetBorderColor()
    {
        ReportMpegStatus(Area.Status);
        SetHirq(HirqCmok | HirqMpcm);
    }

    private void MpegSetFade()
    {
        ReportMpegStatus(Area.Status);
        SetHirq(HirqCmok | HirqMpcm);
    }

    private void MpegSetVideoEffects()
    {
        ReportMpegStatus(Area.Status);
        SetHirq(HirqCmok | HirqMpcm);
    }

    private void MpegSetLsi()
    {
        SetHirq(HirqCmok | HirqMpcm);
    }

    private void GetMpegRom()
    {
        Area.MpegAuthentication |= 0x300;
        Area.MpegRomOutput = Area.Filters[0];
        Area.MpegRomOutputNumber = 0;

        FileStream? rom = OpenMpegRom(Area.MpegPath);
        if (rom is not null)
        {
            using (rom)
            {
                uint readOffset = ((uint)(_executingCommandData[0] & 0xFF) << 8) | _executingCommandData[1];
                ushort readSize = _executingCommandData[3];
                long fileOffset = unchecked((int)(readOffset * Area.GetSectorSize));
                if (fileOffset >= 0)
                    rom.Seek(fileOffset, SeekOrigin.Begin);

                Partition partition = GetPartition(Area.MpegRomOutput!);
                if (Area.BufferFull == 0)
                {
                    partition.Size = 0;
                    for (int i = 0; i < readSize; i++)
                    {
                        int index = partition.BlockCount;
                        Block? block = AllocateBlock(ref partition.BlockNumbers[index], (int)Area.GetSectorSize);
                        partition.Blocks[index] = block;
                        if (block is not null)
                        {
                            int sectorSize = (int)Area.GetSectorSize;
                            rom.ReadAtLeast(block.Data.AsSpan(0, sectorSize), sectorSize, throwOnEndOfStream: false);
                            partition.BlockCount++;
                            partition.Size += (int)Area.GetSectorSize;
                        }
                    }

                    Area.OneSectorStored = true;
                    SetHirq(HirqCsct);
                }
            }
        }

        ReportCdStatus(Area.Status);
        SetHirq(HirqCmok | HirqMped);
    }

    private static FileStream? OpenMpegRom(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        try
        {
            return File.OpenRead(path);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private void AuthenticateDevice()
    {
        int mpegAuthentication = _executingCommandData[1] & 0xFF;
        if (mpegAuthentication != 1)
        {
            int filterNumber = _executingCommandData[2] >> 8;
            if (filterNumber >= MaxSelectors)
            {
                ReportCdStatus(StatusReject);
                SetHirq(HirqCmok);
                return;
            }
        }

        if ((Area.Status & 0xF) != StatusNoDisc && (Area.Status & 0xF) != StatusOpen)
        {
            Area.Status = StatusBusy;
            Area.Reg.Cr1 = (ushort)((Area.Status << 8) | 0xFF);
            Area.Reg.Cr2 = 0xFFFF;
            Area.Reg.Cr3 = 0xFFFF;
            Area.Reg.Cr4 = 0xFFFF;

            if (mpegAuthentication == 1)
            {
                SetHirq(HirqMped);
                Area.MpegAuthentication = 2;
            }
            else
            {
                RefreshMediaToc();
                if (Area.Toc[0] != uint.MaxValue && (Area.Toc[0] & 0x40000000) == 0)
                {
                    Area.SaturnAuthentication = 1;
                    Area.CommandExecutionTiming = CdClocksToMicroseconds(200);
                    Area.DelayedHirq = HirqEfls;
                }
                else
                {
                    StartDiscAuthentication(_executingCommandData[2] >> 8);
                    ReportCdStatus(Area.Status);
                    SetHirq(HirqCmok);
                    return;
                }
            }

            Area.Status = StatusPause;
        }
        else
        {
            if (mpegAuthentication == 1)
            {
                SetHirq(HirqMped);
                Area.MpegAuthentication = 2;
            }
            else
            {
                Area.OneSectorStored = true;
                SetHirq(HirqEfls | HirqCsct);
            }
        }

        ReportCdStatus(Area.Status);
        SetHirq(HirqCmok);
    }

    private void StartDiscAuthentication(int filterNumber)
    {
        _connections.ClearCdda();
        DisconnectFilterInput(filterNumber);
        Filter filter = Area.Filters[filterNumber];
        filter.TrueConnection = (byte)filterNumber;
        filter.FalseConnection = 0xFF;
        filter.Fad = filter.Range = 0;
        filter.Mode = 0;
        Area.CdDeviceOutput = filter;
        Area.CdDeviceOutputNumber = (byte)filterNumber;
        Area.SaturnAuthentication = 0xFF;
        _authenticatingDisc = true;
        _authenticationFilter = filterNumber;
        _seekOriginFad = ReportedFad == uint.MaxValue ? 0 : ReportedFad;
        _seekPauseFad = 150;
        _seekToPlayback = false;
        _initialSeekPhase = InitialSeekPhase.Position;
        _initialSeekMicroseconds = CdClocksToMicroseconds(500);
    }

    private void FinishDiscAuthentication(ushort result)
    {
        _authenticatingDisc = false;
        Area.SaturnAuthentication = result;
        Area.CdDeviceOutput = null;
        Area.CdDeviceOutputNumber = 0xFF;
        ClearSelectorPartition(Area.Partitions[_authenticationFilter]);
        if (result != 0)
            Area.OneSectorStored = true;
        SetHirq(result != 0 ? (ushort)(HirqEfls | HirqCsct) : HirqEfls);
    }

    private ushort ReadDiscAuthentication(uint fad)
    {
        Span<byte> sector = stackalloc byte[2448];
        if (Area.CdInterface!.ReadSectorFad(fad, sector) == 0)
            return 0;

        int dataOffset = sector[15] == 2 ? 24 : 16;
        return sector.Slice(dataOffset, 16).SequenceEqual("SEGA SEGASATURN "u8) ? (ushort)4 : (ushort)2;
    }

    private void IsDeviceAuthenticated()
    {
        Area.Reg.Cr1 = (ushort)(Area.Status << 8);
        if (_executingCommandData[1] != 0)
            Area.Reg.Cr2 = Area.MpegAuthentication;
        else
            Area.Reg.Cr2 = Area.SaturnAuthentication;
        Area.Reg.Cr3 = 0;
        Area.Reg.Cr4 = 0;
        SetHirq(HirqCmok);
    }

    public byte GetInitialProgram(bool autoRegion)
    {
        byte region = 0;
        Area.CdDeviceOutput = Area.Filters[0];
        Area.CdDeviceOutputNumber = 0;

        Partition? partition = ReadUnfilteredSector(150);
        if (partition is not null)
        {
            Block block = partition.Blocks[partition.BlockCount - 1]!;
            ReadOnlySpan<byte> buffer = block.Data.AsSpan(0, block.Size);
            if (buffer[..15].SequenceEqual("SEGA SEGASATURN"u8))
            {
                InitialProgram ip = _initialProgram!;
                buffer[..16].CopyTo(ip.System);
                ip.System[16] = 0;
                buffer.Slice(0x10, 16).CopyTo(ip.Company);
                ip.Company[16] = 0;
                ScanInitialProgramToken(buffer.Slice(0x20, 10), ip.ItemNumber);
                buffer.Slice(0x2A, 6).CopyTo(ip.Version);
                ip.Version[6] = 0;

                ip.Date[0] = buffer[0x34];
                ip.Date[1] = buffer[0x35];
                ip.Date[2] = (byte)'/';
                ip.Date[3] = buffer[0x36];
                ip.Date[4] = buffer[0x37];
                ip.Date[5] = (byte)'/';
                buffer.Slice(0x30, 4).CopyTo(ip.Date.AsSpan(6));
                ip.Date[10] = 0;

                ScanInitialProgramToken(buffer.Slice(0x38, 8), ip.CdInfo);
                ScanInitialProgramToken(buffer.Slice(0x40, 16), ip.Region);
                ScanInitialProgramToken(buffer.Slice(0x50, 16), ip.Peripheral);
                buffer.Slice(0x60, 112).CopyTo(ip.GameName);
                ip.GameName[112] = 0;

                ip.IpSize = BinaryPrimitives.ReadUInt32BigEndian(buffer[0xE0..]);
                ip.MasterSh2Stack = BinaryPrimitives.ReadUInt32BigEndian(buffer[0xE8..]);
                ip.SlaveSh2Stack = BinaryPrimitives.ReadUInt32BigEndian(buffer[0xEC..]);
                ip.FirstProgramAddress = BinaryPrimitives.ReadUInt32BigEndian(buffer[0xF0..]);
                ip.FirstProgramSize = BinaryPrimitives.ReadUInt32BigEndian(buffer[0xF4..]);

                if (ip.MasterSh2Stack == 0)
                    ip.MasterSh2Stack = 0x06002000;
                if ((ip.MasterSh2Stack & 0x80000000) != 0)
                    ip.MasterSh2Stack = 0x06000000 + (ip.MasterSh2Stack & 0xFFFF);
                if (ip.SlaveSh2Stack == 0)
                    ip.SlaveSh2Stack = 0x06001000;
                if ((ip.SlaveSh2Stack & 0x80000000) != 0)
                    ip.SlaveSh2Stack = 0x06000000 + (ip.SlaveSh2Stack & 0xFFFF);

                if (autoRegion)
                {
                    region = DetectInitialProgramRegion(buffer.Slice(0x40, 16));
                    if (region == 0)
                    {
                        switch (ip.Region[0])
                        {
                            case (byte)'J': region = 1; break;
                            case (byte)'T': region = 2; break;
                            case (byte)'U': region = 4; break;
                            case (byte)'B': region = 5; break;
                            case (byte)'K': region = 6; break;
                            case (byte)'A': region = 0xA; break;
                            case (byte)'E': region = 0xC; break;
                            case (byte)'L': region = 0xD; break;
                        }
                    }
                }
            }

            partition.Size -= block.Size;
            FreeBlock(block);
            partition.Blocks[partition.BlockCount - 1] = null;
            partition.BlockNumbers[partition.BlockCount - 1] = 0xFF;
            SortBlocks(partition);
            partition.BlockCount--;
        }

        return region;
    }

    private byte DetectInitialProgramRegion(ReadOnlySpan<byte> headerRegions)
    {
        Span<byte> raw = stackalloc byte[2448];
        if (Area.CdInterface!.ReadSectorFad(151, raw) == 0 || raw[15] is not (1 or 2))
            return 0;

        int dataOffset = raw[15] == 1 ? 16 : 24;
        ReadOnlySpan<byte> securityRegions = raw.Slice(dataOffset + 0x600, 8 * 32);
        ReadOnlySpan<byte> letters = "UJKEBTAL"u8;
        ReadOnlySpan<byte> regionIds = [4, 1, 6, 0xC, 5, 2, 0xA, 0xD];
        Span<byte> expected = stackalloc byte[32];
        BinaryPrimitives.WriteUInt32BigEndian(expected, 0xA00E0009);

        for (int i = 0; i < letters.Length; i++)
        {
            byte letter = letters[i];
            if (!headerRegions.Contains(letter))
                continue;

            ReadOnlySpan<byte> regionText = letter switch
            {
                (byte)'U' => "For USA and CANADA."u8,
                (byte)'J' => "For JAPAN."u8,
                (byte)'K' => "For KOREA."u8,
                (byte)'E' => "For EUROPE."u8,
                (byte)'B' => "For BRAZIL."u8,
                (byte)'T' => "For TAIWAN and PHILIPINES."u8,
                (byte)'A' => "For ASIA PAL area."u8,
                (byte)'L' => "For LATIN AMERICA."u8,
                _ => default,
            };

            expected[4..].Fill(0x20);
            regionText.CopyTo(expected[4..]);
            for (int offset = 0; offset < securityRegions.Length; offset += 32)
            {
                if (securityRegions.Slice(offset, 32).SequenceEqual(expected))
                    return regionIds[i];
            }
        }

        return 0;
    }

    private static void ScanInitialProgramToken(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        int start = 0;
        while (start < source.Length && IsInitialProgramSpace(source[start]))
            start++;

        int end = start;
        while (end < source.Length && source[end] != 0 && !IsInitialProgramSpace(source[end]))
            end++;

        if (end > start)
        {
            int length = Math.Min(end - start, destination.Length - 1);
            source.Slice(start, length).CopyTo(destination);
            destination[length] = 0;
        }
    }

    private static bool IsInitialProgramSpace(byte character) => character == 32 || character is >= 9 and <= 13;

    public byte GetRegionId() => GetInitialProgram(true);

    public void SelectQuickLoadPartition()
    {
        Area.CdDeviceOutput = Area.Filters[0];
        Area.CdDeviceOutputNumber = 0;
    }

    public bool ReadQuickLoadSector(uint fad, out ReadOnlySpan<byte> data)
    {
        Partition? partition = ReadUnfilteredSector(fad);
        if (partition is null)
        {
            data = default;
            return false;
        }

        Block block = partition.Blocks[partition.BlockCount - 1]!;
        data = block.Data.AsSpan(0, block.Size);
        return true;
    }

    public void ReleaseQuickLoadSector()
    {
        Partition partition = GetPartition(Area.CdDeviceOutput!);
        int last = partition.BlockCount - 1;
        partition.Size = 0;
        FreeBlock(partition.Blocks[last]);
        partition.Blocks[last] = null;
        partition.BlockNumbers[last] = 0xFF;
        partition.BlockCount = 0;
    }

    public static int ReadQuickLoadDirectoryRecord(ReadOnlySpan<byte> data, out uint lba, out uint size)
    {
        DirectoryRecord record = default;
        CopyDirectoryRecord(data, ref record);
        lba = record.Lba;
        size = record.Size;
        return record.RecordSize;
    }

    public void SetupQuickLoad()
    {
        _ejectPhase = EjectPhase.None;
        _ejectMicroseconds = 0;
        _driveStartupMicroseconds = 0;
        Area.Reg.Hirq = 0xFC1;
        UpdateInterruptLine();
        Area.DiskChanged = 0;
        Area.Reg.Cr1 = (ushort)((Area.Status << 8) | ((Area.Options & 0xF) << 4) | (Area.RepeatCount & 0xF));
        Area.Reg.Cr2 = (ushort)((Area.ControlAddress << 8) | Area.Track);
        Area.Reg.Cr3 = (ushort)(((uint)Area.Index << 8) | ((Area.Fad >> 16) & 0xFF));
        Area.Reg.Cr4 = unchecked((ushort)Area.Fad);
        Area.SaturnAuthentication = 4;
    }

    public uint GetMasterStackAddress() => _initialProgram?.MasterSh2Stack ?? 0x06002000;

    public uint GetSlaveStackAddress() => _initialProgram?.SlaveSh2Stack ?? 0x06001000;

    private byte FadToTrack(uint fad)
    {
        for (int i = 0; i < 99; i++)
        {
            if (Area.Toc[i] == uint.MaxValue)
                return 0xFF;

            if (fad >= (Area.Toc[i] & 0xFFFFFF) && fad < (Area.Toc[i + 1] & 0xFFFFFF))
                return (byte)(i + 1);
        }

        return 0;
    }

    private uint TrackToFad(ushort trackAndIndex)
    {
        if (trackAndIndex == 0xFFFF)
            return Area.Toc[101] & 0x00FFFFFF;
        int track = trackAndIndex >> 8;
        if (trackAndIndex != 0 && track < Area.Toc.Length)
        {
            if ((trackAndIndex & 0xFF) == 1)
            {
                return (track == 0 ? Area.CdWordNumber : Area.Toc[track - 1]) & 0x00FFFFFF;
            }
            if ((trackAndIndex & 0xFF) == 0x63)
                return unchecked((Area.Toc[track] & 0x00FFFFFF) - 1);
        }

        return 0;
    }

    private void SetupDefaultPlayStats(byte trackNumber, bool writeFad)
    {
        if (trackNumber <= Area.Toc.Length)
        {
            Area.Options = 0;
            Area.RepeatCount = 0;
            // cdwnum is immediately before TOC in the source Cs2 structure.
            uint trackData = trackNumber == 0 ? Area.CdWordNumber : Area.Toc[trackNumber - 1];
            Area.ControlAddress = (byte)(trackData >> 24);
            Area.Index = 1;
            Area.Track = trackNumber;
            if (writeFad)
                Area.Fad = trackData & 0x00FFFFFF;
        }
    }
}
