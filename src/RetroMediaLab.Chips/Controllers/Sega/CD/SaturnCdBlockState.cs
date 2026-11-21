// Copyright 2003 Guillaume Duhamel
// Copyright 2004-2006 Theo Berkau
// Translated from Yabause cs2.h.
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Runtime.CompilerServices;

namespace RetroMediaLab.Chips.Controllers.Sega.CD;

internal sealed class SaturnCdBlockState
{
    internal const int MaxBlocks = 200;
    internal const int MaxSelectors = 24;
    internal const int MaxFiles = 256;

    internal sealed class Block(int dataLength = 2352)
    {
        public int Size;
        public uint Fad;
        public bool IsMode2;
        public byte ChannelNumber;
        public byte FileNumber;
        public byte Submode;
        public byte CodingInfo;
        public readonly byte[] Data = new byte[dataLength];
    }

    internal sealed class Filter
    {
        public uint Fad;
        public uint Range;
        public byte Mode;
        public byte Channel;
        public byte SubmodeMask;
        public byte CodingInfoMask;
        public byte FileId;
        public byte SubmodeValue;
        public byte CodingInfoValue;
        public byte TrueConnection;
        public byte FalseConnection;
    }

    internal sealed class Partition
    {
        public int Size;
        public readonly Block?[] Blocks = new Block?[MaxBlocks];
        public readonly byte[] BlockNumbers = new byte[MaxBlocks];
        public byte BlockCount;
    }

    [InlineArray(5)]
    internal struct XaReserved
    {
        private byte _first;
    }

    internal struct XaRecord
    {
        public ushort GroupId;
        public ushort UserId;
        public ushort Attributes;
        public ushort Signature;
        public byte FileNumber;
        public XaReserved Reserved;
    }

    [InlineArray(32)]
    internal struct DirectoryName
    {
        private byte _first;
    }

    internal struct DirectoryRecord
    {
        public byte RecordSize;
        public byte XaRecordSize;
        public uint Lba;
        public uint Size;
        public byte DateYear;
        public byte DateMonth;
        public byte DateDay;
        public byte DateHour;
        public byte DateMinute;
        public byte DateSecond;
        public byte GmtOffset;
        public byte Flags;
        public byte FileUnitSize;
        public byte InterleaveGapSize;
        public ushort VolumeSequenceNumber;
        public byte NameLength;
        public DirectoryName Name;
        public XaRecord Xa;
    }

    internal struct MpegMode
    {
        public byte VideoPlayMode;
        public byte DecodeTimingMode;
        public byte OutputMode;
        public byte StreamLayerMode;
    }

    internal struct MpegConnection
    {
        public byte AudioConnection;
        public byte AudioLayer;
        public byte AudioBuffer;
        public byte VideoConnection;
        public byte VideoLayer;
        public byte VideoBuffer;
    }

    internal struct MpegStream
    {
        public byte AudioStream;
        public byte AudioStreamId;
        public byte AudioChannel;
        public byte VideoStream;
        public byte VideoStreamId;
        public byte VideoChannel;
    }

    internal struct Registers
    {
        public uint Dtr;
        public ushort Unknown;
        public ushort Hirq;
        public ushort HirqMask;
        public ushort Cr1;
        public ushort Cr2;
        public ushort Cr3;
        public ushort Cr4;
        public ushort MpegRgb;
    }

    internal sealed class InitialProgram
    {
        public readonly byte[] System = new byte[17];
        public readonly byte[] Company = new byte[17];
        public readonly byte[] ItemNumber = new byte[11];
        public readonly byte[] Version = new byte[7];
        public readonly byte[] Date = new byte[11];
        public readonly byte[] CdInfo = new byte[9];
        public readonly byte[] Region = new byte[11];
        public readonly byte[] Peripheral = new byte[17];
        public readonly byte[] GameName = new byte[113];
        public uint IpSize;
        public uint MasterSh2Stack;
        public uint SlaveSh2Stack;
        public uint FirstProgramAddress;
        public uint FirstProgramSize;
    }

    public Registers Reg;
    public uint Fad;
    public byte Status;

    public byte Options;
    public byte RepeatCount;
    public byte ControlAddress;
    public byte Track;
    public byte Index;

    public byte ActionStatus;
    public byte PictureInfo;
    public byte MpegAudioStatus;
    public ushort MpegVideoStatus;
    public ushort VideoCounter;

    public ushort SaturnAuthentication;
    public ushort MpegAuthentication;

    public uint TransferCount;
    public uint CdWordNumber;
    public readonly uint[] Toc = new uint[102];
    public uint PlayFad;
    public uint PlayEndFad;
    public uint MaxRepeat;
    public uint GetSectorSize;
    public uint PutSectorSize;
    public uint CalculatedSize;
    public ushort FadSearchSector;
    public byte FadSearchPartition;
    public uint FadSearchAddress;
    public int InfoTransferType;
    public int DataTransferType;
    public int DiskChanged;
    public int BufferFull;
    public bool OneSectorStored;
    public int Speed1X;
    public int IsAudio;
    public uint TransferFileInfoByteCount;
    public readonly byte[] TransferFileInfo = new byte[12];
    public byte LastBuffer;
    public readonly byte[] TransferSubcodeQ = new byte[5 * 2];
    public readonly byte[] TransferSubcodeRw = new byte[12 * 2];

    public readonly Filter[] Filters = new Filter[MaxSelectors];
    public Filter? CdDeviceOutput;
    public Filter? MpegFrameBufferOutput;
    public Filter? MpegBufferOutput;
    public Filter? MpegRomOutput;
    public Filter? HostOutput;
    public byte CdDeviceOutputNumber;
    public byte MpegFrameBufferOutputNumber;
    public byte MpegBufferOutputNumber;
    public byte MpegRomOutputNumber;
    public byte HostOutputNumber;

    public readonly Partition[] Partitions = new Partition[MaxSelectors];
    public byte DataTransferPartitionNumber;
    public ushort DataTransferSectorPosition;
    public int DataTransferOffset;
    public uint DataTransferredSectorCount;
    public ushort DataSectorsToTransfer;

    public uint FreeBlockCount;
    public readonly Block[] Blocks = new Block[MaxBlocks];
    public readonly Block WorkBlock = new(2448);

    public DirectoryRecord RootDirectoryRecord;
    public uint CurrentDirectorySector;
    public uint CurrentDirectorySize;
    public uint CurrentDirectoryFileIdOffset;
    public readonly DirectoryRecord[] Files = new DirectoryRecord[MaxFiles];
    public uint FileCount;
    public bool FileInfoMore;

    public string? MpegPath;
    public uint MpegInterruptMask;
    public MpegMode Mode;
    public readonly MpegConnection[] MpegConnections = new MpegConnection[2];
    public readonly MpegStream[] MpegStreams = new MpegStream[2];

    public int Command;
    public uint StatusCycles;
    public uint StatusTiming;
    public uint PeriodicCycles;
    public uint PeriodicTiming;
    public uint CommandTiming;
    public uint CommandExecutionTiming;
    public ushort DelayedHirq;
    public SaturnCdInterface? CdInterface;
    public int CartridgeType;
    public int PlayType;

    public SaturnCdBlockState()
    {
        for (int i = 0; i < MaxSelectors; i++)
        {
            Filters[i] = new Filter();
            Partitions[i] = new Partition();
        }

        for (int i = 0; i < MaxBlocks; i++)
            Blocks[i] = new Block();
    }
}
