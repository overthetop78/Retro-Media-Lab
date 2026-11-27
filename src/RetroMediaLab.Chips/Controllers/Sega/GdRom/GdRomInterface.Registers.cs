using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.Controllers.Sega.GdRom;

internal static unsafe partial class GdRomInterface
{
    private enum DriveState
    {
        WaitCmd,
        ProcessAta,
        WaitPacket,
        ProcessPacket,
        PioSendData,
        PioGetData,
        PioEnd,
        ProcessPacketDone,
        ReadSectorPio,
        ReadSectorDma,
        ProcessSetMode,
    }

    [StructLayout(LayoutKind.Explicit, Size = 12)]
    private struct SpiCommandInfo
    {
        [FieldOffset(0)] internal byte CommandCode;
        [FieldOffset(0)] internal fixed byte CommandData[12];
        [FieldOffset(0)] internal fixed ushort CommandWords[6];
    }

    [StructLayout(LayoutKind.Explicit, Size = 4)]
    private struct StatusRegister
    {
        [FieldOffset(0)] internal uint Raw;
        [FieldOffset(0)] internal byte Full;

        internal uint Check
        {
            readonly get => ((uint)Full >> 0) & 1u;
            set => Full = (byte)((Full & ~(1u << 0)) | ((value & 1u) << 0));
        }

        internal uint Corr
        {
            readonly get => ((uint)Full >> 2) & 1u;
            set => Full = (byte)((Full & ~(1u << 2)) | ((value & 1u) << 2));
        }

        internal uint Drq
        {
            readonly get => ((uint)Full >> 3) & 1u;
            set => Full = (byte)((Full & ~(1u << 3)) | ((value & 1u) << 3));
        }

        internal uint Dsc
        {
            readonly get => ((uint)Full >> 4) & 1u;
            set => Full = (byte)((Full & ~(1u << 4)) | ((value & 1u) << 4));
        }

        internal uint Df
        {
            readonly get => ((uint)Full >> 5) & 1u;
            set => Full = (byte)((Full & ~(1u << 5)) | ((value & 1u) << 5));
        }

        internal uint Drdy
        {
            readonly get => ((uint)Full >> 6) & 1u;
            set => Full = (byte)((Full & ~(1u << 6)) | ((value & 1u) << 6));
        }

        internal uint Bsy
        {
            readonly get => ((uint)Full >> 7) & 1u;
            set => Full = (byte)((Full & ~(1u << 7)) | ((value & 1u) << 7));
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 4)]
    private struct ErrorRegister
    {
        [FieldOffset(0)] internal uint Raw;
        [FieldOffset(0)] internal byte Full;

        internal uint Ili
        {
            readonly get => ((uint)Full >> 0) & 1u;
            set => Full = (byte)((Full & ~(1u << 0)) | ((value & 1u) << 0));
        }

        internal uint Eom
        {
            readonly get => ((uint)Full >> 1) & 1u;
            set => Full = (byte)((Full & ~(1u << 1)) | ((value & 1u) << 1));
        }

        internal uint Abrt
        {
            readonly get => ((uint)Full >> 2) & 1u;
            set => Full = (byte)((Full & ~(1u << 2)) | ((value & 1u) << 2));
        }

        internal uint Mcr
        {
            readonly get => ((uint)Full >> 3) & 1u;
            set => Full = (byte)((Full & ~(1u << 3)) | ((value & 1u) << 3));
        }

        internal uint Sense
        {
            readonly get => ((uint)Full >> 4) & 15u;
            set => Full = (byte)((Full & ~(15u << 4)) | ((value & 15u) << 4));
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 4)]
    private struct FeaturesRegister
    {
        [FieldOffset(0)] internal uint Raw;
        [FieldOffset(0)] internal byte Full;

        internal uint Dma
        {
            readonly get => ((uint)Full >> 0) & 1u;
            set => Full = (byte)((Full & ~(1u << 0)) | ((value & 1u) << 0));
        }

        internal uint FeatureNumber
        {
            readonly get => ((uint)Full >> 0) & 127u;
            set => Full = (byte)((Full & ~(127u << 0)) | ((value & 127u) << 0));
        }

        internal uint Value
        {
            readonly get => ((uint)Full >> 7) & 1u;
            set => Full = (byte)((Full & ~(1u << 7)) | ((value & 1u) << 7));
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 4)]
    private struct InterruptReasonRegister
    {
        [FieldOffset(0)] internal uint Raw;
        [FieldOffset(0)] internal byte Full;

        internal uint CoD
        {
            readonly get => ((uint)Full >> 0) & 1u;
            set => Full = (byte)((Full & ~(1u << 0)) | ((value & 1u) << 0));
        }

        internal uint Io
        {
            readonly get => ((uint)Full >> 1) & 1u;
            set => Full = (byte)((Full & ~(1u << 1)) | ((value & 1u) << 1));
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 4)]
    private struct SectorCountRegister
    {
        [FieldOffset(0)] internal uint Raw;
        [FieldOffset(0)] internal byte Full;

        internal uint ModeVal
        {
            readonly get => ((uint)Full >> 0) & 15u;
            set => Full = (byte)((Full & ~(15u << 0)) | ((value & 15u) << 0));
        }

        internal uint TransMode
        {
            readonly get => ((uint)Full >> 4) & 15u;
            set => Full = (byte)((Full & ~(15u << 4)) | ((value & 15u) << 4));
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 4)]
    private struct SectorNumberRegister
    {
        [FieldOffset(0)] internal uint Raw;
        [FieldOffset(0)] internal byte Full;

        internal uint Status
        {
            readonly get => ((uint)Full >> 0) & 15u;
            set => Full = (byte)((Full & ~(15u << 0)) | ((value & 15u) << 0));
        }

        internal uint DiscFormat
        {
            readonly get => ((uint)Full >> 4) & 15u;
            set => Full = (byte)((Full & ~(15u << 4)) | ((value & 15u) << 4));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ReadParameters
    {
        internal uint StartSector;
        internal uint RemainingSectors;
        internal uint SectorType;
    }

    [StructLayout(LayoutKind.Explicit, Size = 12)]
    private struct ReadBlock
    {
        [FieldOffset(0)] internal byte CommandCode;
        [FieldOffset(1)] private byte _parameters;
        [FieldOffset(2)] internal fixed byte Block[10];
        [FieldOffset(0)] internal fixed byte Bytes[12];

        internal readonly uint ParameterType => (uint)(_parameters & 1);
        internal readonly uint ExpectedDataType => (uint)(_parameters >> 1) & 7;
        internal readonly uint Other => (uint)(_parameters >> 4) & 1;
        internal readonly uint Data => (uint)(_parameters >> 5) & 1;
        internal readonly uint Subheader => (uint)(_parameters >> 6) & 1;
        internal readonly uint Header => (uint)(_parameters >> 7) & 1;
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    private struct PacketCommand
    {
        [FieldOffset(0)] internal uint Index;
        [FieldOffset(4)] internal fixed ushort Words[6];
        [FieldOffset(4)] internal fixed byte Bytes[12];
        [FieldOffset(4)] internal ReadBlock ReadBlock;
    }

    private sealed partial class DmaBuffer
    {
        private const uint SectorCount = 16;
        private readonly byte[] _cache = new byte[2352 * SectorCount];
        private uint _index;
        private uint _size;

        internal DmaBuffer() => Clear();

        internal bool IsEmpty => _size == 0;
        internal uint Size => _size;

        internal void Clear()
        {
            _size = 0;
            _index = 0;
        }
    }

    private sealed partial class PioBuffer
    {
        internal const uint Capacity = 64 * 1024;
        private readonly ushort[] _data = new ushort[Capacity / 2];
        private uint _index;
        private uint _size;

        internal PioBuffer() => Clear();

        internal DriveState NextState;
        internal bool IsEmpty => _size == 0;
        internal bool AtEnd => _index == _size;
        internal uint Size => _size * sizeof(ushort);

        internal void Clear()
        {
            NextState = DriveState.WaitCmd;
            _size = 0;
            _index = 0;
        }

        internal ushort Read() => _data[_index++];
        internal ReadOnlySpan<ushort> Data => _data;
        internal void Write(ushort value) => _data[_index++] = value;

        internal Span<ushort> Fill(uint size)
        {
            ResetSize(size);
            return _data;
        }

        internal void ResetSize(uint size)
        {
            _index = 0;
            _size = size / sizeof(ushort);
        }
    }

    private enum CddaStatus
    {
        NoInfo,
        Playing,
        Paused,
        Terminated,
    }

    [StructLayout(LayoutKind.Explicit, Size = 4)]
    private struct CddaAddress
    {
        [FieldOffset(0)] internal uint Fad;
        [FieldOffset(0)] internal byte B0;
        [FieldOffset(1)] internal byte B1;
        [FieldOffset(2)] internal byte B2;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Cdda
    {
        internal CddaStatus Status;
        internal uint Repeats;
        internal CddaAddress CurrentAddress;
        internal CddaAddress EndAddress;
        internal CddaAddress StartAddress;
    }

    [StructLayout(LayoutKind.Explicit, Size = 2)]
    private struct ByteCountRegister
    {
        [FieldOffset(0)] internal byte Low;
        [FieldOffset(1)] internal byte High;
        [FieldOffset(0)] internal ushort Full;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct DriveHardwareInfo
    {
        internal fixed byte Reserved0[2];
        internal byte Speed;
        internal byte Reserved1;
        internal byte StandbyHigh;
        internal byte StandbyLow;
        internal byte ReadFlags;
        internal fixed byte Reserved2[2];
        internal byte ReadRetry;
        internal fixed byte DriveInfo[8];
        internal fixed byte SystemVersion[8];
        internal fixed byte SystemDate[6];
    }

    private const uint GD_BUSY = 0x00;
    private const uint GD_PAUSE = 0x01;
    private const uint GD_STANDBY = 0x02;
    private const uint GD_PLAY = 0x03;
    private const uint GD_SEEK = 0x04;
    private const uint GD_SCAN = 0x05;
    private const uint GD_OPEN = 0x06;
    private const uint GD_NODISC = 0x07;
    private const uint GD_RETRY = 0x08;
    private const uint GD_ERROR = 0x09;
    private const uint GD_IMPEDHI0_Read = 0x005F7000;
    private const uint GD_IMPEDHI4_Read = 0x005F7004;
    private const uint GD_IMPEDHI8_Read = 0x005F7008;
    private const uint GD_IMPEDHIC_Read = 0x005F700C;
    private const uint GD_ALTSTAT_Read = 0x005F7018;
    private const uint GD_DEVCTRL_Write = 0x005F7018;
    private const uint GD_DATA = 0x005F7080;
    private const uint GD_ERROR_Read = 0x005F7084;
    private const uint GD_FEATURES_Write = 0x005F7084;
    private const uint GD_IREASON_Read = 0x005F7088;
    private const uint GD_SECTCNT_Write = 0x005F7088;
    private const uint GD_SECTNUM = 0x005F708C;
    private const uint GD_BYCTLLO = 0x005F7090;
    private const uint GD_BYCTLHI = 0x005F7094;
    private const uint GD_DRVSEL = 0x005F7098;
    private const uint GD_STATUS_Read = 0x005F709C;
    private const uint GD_COMMAND_Write = 0x005F709C;
    private const uint ATA_NOP = 0x00;
    private const uint ATA_SOFT_RESET = 0x08;
    private const uint ATA_EXEC_DIAG = 0x90;
    private const uint ATA_SPI_PACKET = 0xA0;
    private const uint ATA_IDENTIFY_DEV = 0xA1;
    private const uint ATA_IDENTIFY = 0xEC;
    private const uint ATA_SET_FEATURES = 0xEF;
    private const uint SPI_TEST_UNIT = 0x00;
    private const uint SPI_REQ_STAT = 0x10;
    private const uint SPI_REQ_MODE = 0x11;
    private const uint SPI_SET_MODE = 0x12;
    private const uint SPI_REQ_ERROR = 0x13;
    private const uint SPI_GET_TOC = 0x14;
    private const uint SPI_REQ_SES = 0x15;
    private const uint SPI_CD_OPEN = 0x16;
    private const uint SPI_CD_PLAY = 0x20;
    private const uint SPI_CD_SEEK = 0x21;
    private const uint SPI_CD_SCAN = 0x22;
    private const uint SPI_CD_READ = 0x30;
    private const uint SPI_CD_READ2 = 0x31;
    private const uint SPI_GET_SCD = 0x40;
}
