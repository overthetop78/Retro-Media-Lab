using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;
using RetroMediaLab.Chips.Dreamcast.Holly;
using static RetroMediaLab.Chips.Dreamcast.Holly.HollySystemBus;
using static RetroMediaLab.Chips.Dreamcast.Holly.HollyInterruptId;

namespace RetroMediaLab.Chips.Controllers.Sega.GdRom;

internal static unsafe partial class GdRomInterface
{
    private static int _schedulerId;
    internal static int SenseAsc;
    internal static int SenseAscq;
    internal static int SenseKey;
    private static uint _setModeOffset;
    private static ReadParameters _readParameters;
    private static PacketCommand _packet;
    private static readonly DmaBuffer Dma = new();
    private static readonly PioBuffer Pio = new();
    private static byte _ataCommand;
    private static Cdda _cdda;
    private static DriveState _state;
    private static GdRomDiscType _discType;
    private static uint _dataWriteMode;
    private static uint _driveSelect;
    private static ErrorRegister _error;
    private static InterruptReasonRegister _interruptReason;
    private static FeaturesRegister _features;
    private static SectorCountRegister _sectorCount;
    private static SectorNumberRegister _sectorNumber;
    private static StatusRegister _status;
    private static ByteCountRegister _byteCount;
    private static DriveHardwareInfo _hardware;

    internal static partial void ReadCddaSector(short* sector)
    {
        if (_cdda.Status == CddaStatus.Playing)
        {
            if (GdRomMedia.ReadSector((byte*)sector, _cdda.CurrentAddress.Fad, 1, 2352, true) == 0)
            {
                _cdda.CurrentAddress.Fad--;
                _cdda.Status = CddaStatus.Terminated;
                _sectorNumber.Status = GD_PAUSE;
                new Span<byte>(sector, 2352).Clear();
            }
            else
            {
                _cdda.CurrentAddress.Fad++;
                if (_cdda.CurrentAddress.Fad >= _cdda.EndAddress.Fad)
                {
                    if (_cdda.Repeats == 0)
                    {
                        _cdda.Status = CddaStatus.Terminated;
                        _sectorNumber.Status = GD_PAUSE;
                    }
                    else
                    {
                        if (_cdda.Repeats != 15)
                            _cdda.Repeats--;
                        _cdda.CurrentAddress.Fad = _cdda.StartAddress.Fad;
                    }
                }
            }
        }
        else
        {
            new Span<byte>(sector, 2352).Clear();
        }
    }

    private sealed partial class DmaBuffer
    {
        internal void Fill(ref ReadParameters parameters)
        {
            if (!IsEmpty)
                return;
            _index = 0;
            Sh4HostRuntime.Verify(parameters.RemainingSectors > 0);
            uint count = Math.Min(parameters.RemainingSectors, SectorCount);
            _size = count * parameters.SectorType;

            fixed (byte* cache = _cache)
                GdRomMedia.ReadSector(cache, parameters.StartSector, count, parameters.SectorType);
            parameters.StartSector += count;
            parameters.RemainingSectors -= count;
        }

        internal ReadOnlySpan<byte> Read(uint length)
        {
            Sh4HostRuntime.Verify(length <= _size);
            ReadOnlySpan<byte> data = _cache.AsSpan((int)_index, (int)length);
            _index += length;
            _size -= length;
            return data;
        }

        internal void Serialize(BinaryWriter writer)
        {
            writer.Write(_size);
            writer.Write(_cache.AsSpan((int)_index, (int)_size));
        }

        internal void Deserialize(BinaryReader reader, int version)
        {
            if (version < 849)
            {
                _index = reader.ReadUInt32();
                _size = reader.ReadUInt32();
                if (_index >= _cache.Length)
                    Clear();
                else
                    _size = Math.Min(_size, (uint)_cache.Length - _index);
                reader.BaseStream.ReadExactly(_cache);
                reader.BaseStream.Seek(2352 * 16, SeekOrigin.Current);
            }
            else
            {
                _index = 0;
                _size = reader.ReadUInt32();
                if (_size > _cache.Length)
                    throw new InvalidDataException("Invalid GD-ROM DMA buffer size");
                reader.BaseStream.ReadExactly(_cache.AsSpan(0, (int)_size));
            }
        }
    }

    private sealed partial class PioBuffer
    {
        internal void Serialize(BinaryWriter writer)
        {
            writer.Write((int)NextState);
            writer.Write(_index);
            writer.Write(_size);
            writer.Write(MemoryMarshal.AsBytes(_data.AsSpan())[..(int)_size]);
        }

        internal void Deserialize(BinaryReader reader, int version)
        {
            NextState = (DriveState)reader.ReadInt32();
            _index = reader.ReadUInt32();
            _size = reader.ReadUInt32();
            if (_size > Capacity / sizeof(ushort) || _index > _size)
                throw new InvalidDataException("Invalid GD-ROM PIO buffer state");
            Span<byte> data = MemoryMarshal.AsBytes(_data.AsSpan());
            reader.BaseStream.ReadExactly(version < 849 ? data : data[..(int)_size]);
        }
    }

    private static void SetState(DriveState state)
    {
        DriveState previous = _state;
        _state = state;
        switch (state)
        {
            case DriveState.WaitCmd:
                _status.Drdy = 1;
                _status.Bsy = 0;
                break;

            case DriveState.ProcessAta:
                _status.Drdy = 0;
                _status.Bsy = 1;
                ProcessAtaCommand();
                break;

            case DriveState.WaitPacket:
                Sh4HostRuntime.Verify(previous == DriveState.ProcessAta);
                _packet.Index = 0;
                _interruptReason.CoD = 1;
                _status.Bsy = 0;
                _interruptReason.Io = 0;
                _status.Drq = 1;
                break;

            case DriveState.ProcessPacket:
                Sh4HostRuntime.Verify(previous == DriveState.WaitPacket);
                _status.Drq = 0;
                _status.Bsy = 1;
                ProcessSpiCommand();
                break;

            case DriveState.PioGetData:
            case DriveState.PioSendData:
                _byteCount.Full = (ushort)Pio.Size;
                _interruptReason.Io = 1;
                _interruptReason.CoD = 0;
                _status.Drq = 1;
                _status.Bsy = 0;
                HollyInterruptController.RaiseInterrupt(GdromCommand);
                break;

            case DriveState.ReadSectorPio:
            {
                _status.Bsy = 1;
                uint count = _readParameters.RemainingSectors;
                DriveState nextState = DriveState.PioEnd;
                uint maxSectors = (PioBuffer.Capacity - 1) / _readParameters.SectorType;
                if (count > maxSectors)
                {
                    count = maxSectors;
                    nextState = DriveState.ReadSectorPio;
                }

                fixed (ushort* buffer = Pio.Fill(count * _readParameters.SectorType))
                    GdRomMedia.ReadSector((byte*)buffer, _readParameters.StartSector, count, _readParameters.SectorType);
                _readParameters.StartSector += count;
                _readParameters.RemainingSectors -= count;
                EndSpiPio(null, 0, nextState);
                break;
            }

            case DriveState.ReadSectorDma:
                Dma.Clear();
                Dma.Fill(ref _readParameters);
                break;

            case DriveState.PioEnd:
                _status.Drq = 0;
                SetState(DriveState.ProcessPacketDone);
                break;

            case DriveState.ProcessPacketDone:
                _status.Drdy = 1;
                _interruptReason.CoD = 1;
                _interruptReason.Io = 1;
                _status.Drq = 0;
                _status.Bsy = 0;
                HollyInterruptController.RaiseInterrupt(GdromCommand);
                SetState(DriveState.WaitCmd);
                break;

            case DriveState.ProcessSetMode:
                MemoryMarshal.AsBytes(Pio.Data)[..(int)Pio.Size].CopyTo(
                    MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref _hardware, 1))[(int)_setModeOffset..]);
                SetState(DriveState.PioEnd);
                break;

            default:
                Sh4HostRuntime.Die("Unhandled GDROM state ...");
                break;
        }
    }

    internal static partial void SetDisc()
    {
        _cdda.Status = CddaStatus.NoInfo;
        GdRomDiscType disc = GdRomMedia.GetDiscType();
        switch (disc)
        {
            case GdRomDiscType.NoDisk:
                _sectorNumber.Status = GD_NODISC;
                break;

            case GdRomDiscType.Open:
                _sectorNumber.Status = GD_OPEN;
                _status.Drdy = 1;
                break;

            default:
                _sectorNumber.Status = _sectorNumber.Status == GD_BUSY ? GD_PAUSE : GD_STANDBY;
                break;
        }

        _discType = disc;
        _sectorNumber.DiscFormat = (uint)_discType >> 4;
    }

    private static void ResetDrive()
    {
        SetDisc();
        SetState(DriveState.WaitCmd);
    }

    private static uint GetFad(byte* data, bool msf)
    {
        if (msf)
            return (uint)(data[0] * 60 * 75 + data[1] * 75 + data[2]);
        return (uint)((data[0] << 16) | (data[1] << 8) | data[2]);
    }

    private static void DiscChange()
    {
        SetDisc();
        _readParameters = default;
        _setModeOffset = 0;
        _packet = default;
        Dma.Clear();
        Pio.Clear();
        _ataCommand = 0;
        _cdda = new Cdda { Status = CddaStatus.NoInfo, Repeats = 0 };
    }

    private static void EndSpiPio(byte* buffer, uint length, DriveState nextState = DriveState.PioEnd)
    {
        if (buffer != null)
        {
            Sh4HostRuntime.Verify(length < 0xffff);
            new ReadOnlySpan<byte>(buffer, (int)length).CopyTo(MemoryMarshal.AsBytes(Pio.Fill(length)));
        }

        Pio.NextState = nextState;
        if (Pio.IsEmpty)
            SetState(nextState);
        else
            SetState(DriveState.PioSendData);
    }

    private static void EndSpiPioRead(uint length, DriveState nextState)
    {
        Sh4HostRuntime.Verify(length < 0xffff);
        Pio.ResetSize(length);
        Pio.NextState = nextState;
        if (length == 0)
            SetState(nextState);
        else
            SetState(DriveState.PioGetData);
    }

    private static void ProcessAtaCommand()
    {
        _error.Abrt = 0;
        _status.Check = SenseKey is 0 or 0xb or 6 ? 0u : 1u;

        switch ((uint)_ataCommand)
        {
            case ATA_NOP:
                Debug.WriteLine("ATA_NOP");
                _error.Abrt = 1;
                _error.Sense = (uint)SenseKey;
                _status.Bsy = 0;
                _status.Check = 1;
                _status.Drq = 0;
                HollyInterruptController.RaiseInterrupt(GdromCommand);
                SetState(DriveState.WaitCmd);
                break;

            case ATA_SOFT_RESET:
                Debug.WriteLine("ATA_SOFT_RESET");
                ResetDrive();
                _status.Full = 0;
                _error.Full = 1;
                SenseKey = 0;
                _sectorNumber.Status = GD_PAUSE;
                _interruptReason.Full = 1;
                _byteCount.Low = 0x14;
                _byteCount.High = 0xeb;
                break;

            case ATA_EXEC_DIAG:
                Debug.WriteLine("ATA_EXEC_DIAG");
                _error.Full = 1;
                SenseKey = 0;
                _status.Bsy = 0;
                _status.Check = 1;
                HollyInterruptController.RaiseInterrupt(GdromCommand);
                SetState(DriveState.WaitCmd);
                break;

            case ATA_SPI_PACKET:
                Debug.WriteLine("ATA_SPI_PACKET");
                SetState(DriveState.WaitPacket);
                break;

            case ATA_IDENTIFY_DEV:
                fixed (byte* packet = _packet.Bytes)
                    Debug.WriteLine($"ATA_IDENTIFY_DEV: offset {packet[2]} len {packet[4]}");
                _status.Bsy = 0;
                fixed (ushort* data = ReplyA1)
                    EndSpiPio((byte*)data, 0x50);
                break;

            case ATA_SET_FEATURES:
                Debug.WriteLine("ATA_SET_FEATURES");
                _error.Abrt = 0;
                _status.Dsc = 0;
                _status.Df = 0;
                _status.Drq = 0;
                HollyInterruptController.RaiseInterrupt(GdromCommand);
                SetState(DriveState.WaitCmd);
                break;

            case ATA_IDENTIFY:
                Debug.WriteLine("ATA_IDENTIFY");
                _driveSelect = 0xa0;
                _sectorCount.Full = 1;
                _sectorNumber.Full = 1;
                _byteCount.Low = 0x14;
                _byteCount.High = 0xeb;
                _error.Full = 0x4;
                _status.Full = 0;
                _status.Drdy = 1;
                _status.Check = 1;
                HollyInterruptController.RaiseInterrupt(GdromCommand);
                SetState(DriveState.WaitCmd);
                break;

            default:
                Trace.TraceWarning($"Unknown ATA command {_ataCommand:x}");
                _error.Abrt = 1;
                _error.Sense = 5;
                _status.Bsy = 0;
                _status.Check = 1;
                HollyInterruptController.RaiseInterrupt(GdromCommand);
                SetState(DriveState.WaitCmd);
                break;
        }
    }

    private static ReadOnlySpan<ushort> CrcTable =>
    [
        0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50a5, 0x60c6, 0x70e7,
        0x8108, 0x9129, 0xa14a, 0xb16b, 0xc18c, 0xd1ad, 0xe1ce, 0xf1ef,
        0x1231, 0x0210, 0x3273, 0x2252, 0x52b5, 0x4294, 0x72f7, 0x62d6,
        0x9339, 0x8318, 0xb37b, 0xa35a, 0xd3bd, 0xc39c, 0xf3ff, 0xe3de,
        0x2462, 0x3443, 0x0420, 0x1401, 0x64e6, 0x74c7, 0x44a4, 0x5485,
        0xa56a, 0xb54b, 0x8528, 0x9509, 0xe5ee, 0xf5cf, 0xc5ac, 0xd58d,
        0x3653, 0x2672, 0x1611, 0x0630, 0x76d7, 0x66f6, 0x5695, 0x46b4,
        0xb75b, 0xa77a, 0x9719, 0x8738, 0xf7df, 0xe7fe, 0xd79d, 0xc7bc,
        0x48c4, 0x58e5, 0x6886, 0x78a7, 0x0840, 0x1861, 0x2802, 0x3823,
        0xc9cc, 0xd9ed, 0xe98e, 0xf9af, 0x8948, 0x9969, 0xa90a, 0xb92b,
        0x5af5, 0x4ad4, 0x7ab7, 0x6a96, 0x1a71, 0x0a50, 0x3a33, 0x2a12,
        0xdbfd, 0xcbdc, 0xfbbf, 0xeb9e, 0x9b79, 0x8b58, 0xbb3b, 0xab1a,
        0x6ca6, 0x7c87, 0x4ce4, 0x5cc5, 0x2c22, 0x3c03, 0x0c60, 0x1c41,
        0xedae, 0xfd8f, 0xcdec, 0xddcd, 0xad2a, 0xbd0b, 0x8d68, 0x9d49,
        0x7e97, 0x6eb6, 0x5ed5, 0x4ef4, 0x3e13, 0x2e32, 0x1e51, 0x0e70,
        0xff9f, 0xefbe, 0xdfdd, 0xcffc, 0xbf1b, 0xaf3a, 0x9f59, 0x8f78,
        0x9188, 0x81a9, 0xb1ca, 0xa1eb, 0xd10c, 0xc12d, 0xf14e, 0xe16f,
        0x1080, 0x00a1, 0x30c2, 0x20e3, 0x5004, 0x4025, 0x7046, 0x6067,
        0x83b9, 0x9398, 0xa3fb, 0xb3da, 0xc33d, 0xd31c, 0xe37f, 0xf35e,
        0x02b1, 0x1290, 0x22f3, 0x32d2, 0x4235, 0x5214, 0x6277, 0x7256,
        0xb5ea, 0xa5cb, 0x95a8, 0x8589, 0xf56e, 0xe54f, 0xd52c, 0xc50d,
        0x34e2, 0x24c3, 0x14a0, 0x0481, 0x7466, 0x6447, 0x5424, 0x4405,
        0xa7db, 0xb7fa, 0x8799, 0x97b8, 0xe75f, 0xf77e, 0xc71d, 0xd73c,
        0x26d3, 0x36f2, 0x0691, 0x16b0, 0x6657, 0x7676, 0x4615, 0x5634,
        0xd94c, 0xc96d, 0xf90e, 0xe92f, 0x99c8, 0x89e9, 0xb98a, 0xa9ab,
        0x5844, 0x4865, 0x7806, 0x6827, 0x18c0, 0x08e1, 0x3882, 0x28a3,
        0xcb7d, 0xdb5c, 0xeb3f, 0xfb1e, 0x8bf9, 0x9bd8, 0xabbb, 0xbb9a,
        0x4a75, 0x5a54, 0x6a37, 0x7a16, 0x0af1, 0x1ad0, 0x2ab3, 0x3a92,
        0xfd2e, 0xed0f, 0xdd6c, 0xcd4d, 0xbdaa, 0xad8b, 0x9de8, 0x8dc9,
        0x7c26, 0x6c07, 0x5c64, 0x4c45, 0x3ca2, 0x2c83, 0x1ce0, 0x0cc1,
        0xef1f, 0xff3e, 0xcf5d, 0xdf7c, 0xaf9b, 0xbfba, 0x8fd9, 0x9ff8,
        0x6e17, 0x7e36, 0x4e55, 0x5e74, 0x2e93, 0x3eb2, 0x0ed1, 0x1ef0,
    ];

    private static ushort CcittCrc(byte* data, int length)
    {
        ushort crc = 0;
        while (length-- > 0)
            crc = (ushort)(((crc & 0xff) << 8) ^ CrcTable[((crc >> 8) & 0xff) ^ *data++]);
        return (ushort)~crc;
    }

    private static int BinaryToBcd(int value) => (value % 10) | ((value / 10) << 4);

    private static void EncodeSubcode(byte* buffer, byte data)
    {
        buffer[0] = (byte)((data & 0x80) != 0 ? 0x40 : 0);
        buffer[1] = (byte)((data & 0x40) != 0 ? 0x40 : 0);
        buffer[2] = (byte)((data & 0x20) != 0 ? 0x40 : 0);
        buffer[3] = (byte)((data & 0x10) != 0 ? 0x40 : 0);
        buffer[4] = (byte)((data & 0x08) != 0 ? 0x40 : 0);
        buffer[5] = (byte)((data & 0x04) != 0 ? 0x40 : 0);
        buffer[6] = (byte)((data & 0x02) != 0 ? 0x40 : 0);
        buffer[7] = (byte)((data & 0x01) != 0 ? 0x40 : 0);
    }

    internal static partial uint GetSubcode(uint format, uint fad, byte* subcode)
    {
        subcode[0] = 0;
        switch (_cdda.Status)
        {
            case CddaStatus.NoInfo:
            default:
                subcode[1] = 0x15;
                break;
            case CddaStatus.Playing:
                subcode[1] = 0x11;
                break;
            case CddaStatus.Paused:
                subcode[1] = 0x12;
                break;
            case CddaStatus.Terminated:
                subcode[1] = 0x13;
                break;
        }

        switch (format)
        {
            case 0:
            {
                subcode[2] = 0;
                subcode[3] = 100;
                uint currentFad = _cdda.Status is CddaStatus.Playing or CddaStatus.Paused ? _cdda.CurrentAddress.Fad : fad;
                uint track = GdRomMedia.GetTrackNumber(currentFad, out uint elapsed);
                byte* q = stackalloc byte[12];
                GdRomMedia.GetTrackAdrAndControl(track, out byte adr, out byte control);
                q[0] = (byte)((control << 4) | adr);
                q[1] = (byte)BinaryToBcd((int)track);
                q[2] = (byte)BinaryToBcd(1);
                q[3] = (byte)BinaryToBcd((int)(elapsed / 60 / 75));
                q[4] = (byte)BinaryToBcd((int)((elapsed / 75) % 60));
                q[5] = (byte)BinaryToBcd((int)(elapsed % 75));
                q[6] = 0;
                q[7] = (byte)BinaryToBcd((int)(currentFad / 60 / 75));
                q[8] = (byte)BinaryToBcd((int)((currentFad / 75) % 60));
                q[9] = (byte)BinaryToBcd((int)(currentFad % 75));
                ushort crc = CcittCrc(q, 10);
                q[10] = (byte)(crc >> 8);
                q[11] = (byte)crc;
                byte* destination = &subcode[4];
                foreach (byte value in new ReadOnlySpan<byte>(q, 12))
                {
                    EncodeSubcode(destination, value);
                    destination += 8;
                }
                break;
            }

            case 1:
            default:
            {
                uint currentFad = _cdda.Status is CddaStatus.Playing or CddaStatus.Paused ? _cdda.CurrentAddress.Fad : fad;
                uint track = GdRomMedia.GetTrackNumber(currentFad, out uint elapsed);
                subcode[2] = 0;
                subcode[3] = 0xe;
                GdRomMedia.GetTrackAdrAndControl(track, out byte adr, out byte control);
                subcode[4] = (byte)((control << 4) | adr);
                byte* dataQ = &subcode[4];
                dataQ[1] = (byte)track;
                dataQ[2] = 1;
                dataQ[3] = (byte)(elapsed >> 16);
                dataQ[4] = (byte)(elapsed >> 8);
                dataQ[5] = (byte)elapsed;
                dataQ[6] = 0;
                dataQ[7] = (byte)(currentFad >> 16);
                dataQ[8] = (byte)(currentFad >> 8);
                dataQ[9] = (byte)currentFad;
                Debug.WriteLine($"gd_get_subcode: {Convert.ToHexString(new ReadOnlySpan<byte>(subcode, 14))}");
                break;
            }

            case 2:
                subcode[2] = 0;
                subcode[3] = 0x18;
                subcode[4] = 2;
                subcode[5] = 0;
                subcode[6] = 0;
                subcode[7] = 0;
                subcode[8] = 0;
                "0000000000000"u8.CopyTo(new Span<byte>(&subcode[9], 13));
                subcode[22] = 0;
                subcode[23] = 0;
                Debug.WriteLine($"gd_get_subcode: format 2 (Media catalog number). audio {subcode[1]:x}");
                break;
        }
        return subcode[3];
    }

    private static void ProcessSpiCommand()
    {
        Debug.WriteLine($"Sense: {SenseAsc:x2} {SenseAscq:x2} {SenseKey:x2}");
        fixed (byte* packet = _packet.Bytes)
        {
            Debug.WriteLine($"SPI command {packet[0]:x2};");
            Debug.WriteLine($"Params: {Convert.ToHexString(new ReadOnlySpan<byte>(packet, 12))}");
            _status.Check = SenseKey is 0 or 0xb ? 0u : 1u;

            switch ((uint)packet[0])
            {
                case SPI_TEST_UNIT:
                    Debug.WriteLine("SPI_TEST_UNIT");
                    _status.Check = _sectorNumber.Status == GD_BUSY ? 1u : 0u;
                    SetState(DriveState.ProcessPacketDone);
                    break;

                case SPI_REQ_MODE:
                    fixed (DriveHardwareInfo* hardware = &_hardware)
                    {
                        hardware->Speed = 0;
                        hardware->Reserved0[0] = 0;
                        hardware->Reserved0[1] = 0;
                        hardware->Reserved1 = 0;
                        hardware->Reserved2[0] = 0;
                        hardware->Reserved2[1] = 0;
                        hardware->ReadFlags &= 0x39;
                        Debug.WriteLine($"SPI_REQ_MODE cd-rom speed {hardware->Speed} flags {hardware->ReadFlags:x} retry {hardware->ReadRetry:x}");
                        EndSpiPio((byte*)hardware + packet[2], packet[4]);
                    }
                    break;

                case SPI_CD_READ:
                case SPI_CD_READ2:
                {
                    _cdda.Status = CddaStatus.NoInfo;
                    ReadBlock read = _packet.ReadBlock;
                    uint sectorSize = 2048;
                    if (read.Header == 1 && read.Subheader == 1 && read.Data == 1 && read.ExpectedDataType == 3 && read.Other == 0)
                        sectorSize = 2340;
                    else if (read.Other == 1 || read.ExpectedDataType == 1)
                        sectorSize = 2352;
                    else if (read.Header == 1 || read.Subheader == 1 || read.Other == 1 || read.Data == 0)
                        Trace.TraceWarning($"GDROM: *FIXME* ADD MORE CD READ SETTINGS head {read.Header} subh {read.Subheader} other {read.Other} data {read.Data} type {read.ExpectedDataType}");

                    _readParameters.StartSector = GetFad(&packet[2], read.ParameterType != 0);
                    if (packet[0] == SPI_CD_READ)
                        _readParameters.RemainingSectors = (uint)((packet[8] << 16) | (packet[9] << 8) | packet[10]);
                    else
                        _readParameters.RemainingSectors = (uint)((packet[6] << 8) | packet[7]);
                    _readParameters.SectorType = sectorSize;

                    Debug.WriteLine($"SPI_CD_READ - Sector={_readParameters.StartSector} Size={_readParameters.RemainingSectors}/{_readParameters.SectorType} DMA={_features.Dma}");
                    if (_features.Dma == 1)
                    {
                        Pio.Clear();
                        SetState(DriveState.ReadSectorDma);
                    }
                    else
                    {
                        Dma.Clear();
                        SetState(DriveState.ReadSectorPio);
                    }
                    break;
                }

                case SPI_GET_TOC:
                {
                    Debug.WriteLine("SPI_GET_TOC");
                    uint* toc = stackalloc uint[102];
                    GdRomMedia.GetToc(toc, packet[1] & 1);
                    EndSpiPio((byte*)toc, Math.Min((uint)(packet[4] | (packet[3] << 8)), 102u * sizeof(uint)));
                    break;
                }

                case 0x70:
                    Debug.WriteLine("SPI : unknown ? [0x70]");
                    SetState(DriveState.ProcessPacketDone);
                    break;

                case 0x71:
                    Debug.WriteLine("SPI : unknown ? [0x71]");
                    fixed (ushort* reply = Reply71)
                        EndSpiPio((byte*)reply, (uint)Reply71Size);

                    if (GdRomMedia.GetDiscType() == GdRomDiscType.GdRom || GdRomMedia.GetDiscType() == GdRomDiscType.CdRom_XA)
                        _sectorNumber.Status = GD_PAUSE;
                    else
                        _sectorNumber.Status = GD_STANDBY;
                    break;

                case SPI_SET_MODE:
                {
                    Debug.WriteLine("SPI_SET_MODE");
                    uint offset = packet[2];
                    uint count = Math.Min((uint)packet[4], 10u - offset);
                    _setModeOffset = offset;
                    EndSpiPioRead(count, DriveState.ProcessSetMode);
                    break;
                }

                case SPI_REQ_STAT:
                {
                    uint fad = _cdda.Status is CddaStatus.Playing or CddaStatus.Paused ? _cdda.CurrentAddress.Fad : _readParameters.StartSector - 1;
                    Debug.WriteLine($"SPI_REQ_STAT: status {_sectorNumber.Status} fmt {_sectorNumber.DiscFormat} fad {fad}");
                    uint track = GdRomMedia.GetTrackNumber(fad, out _);
                    byte* status = stackalloc byte[10];
                    status[0] = (byte)_sectorNumber.Status;
                    status[1] = (byte)((_sectorNumber.DiscFormat << 4) | _cdda.Repeats);
                    GdRomMedia.GetTrackAdrAndControl(track, out byte adr, out byte control);
                    status[2] = (byte)((control << 4) | adr);
                    status[3] = (byte)track;
                    status[4] = 1;
                    status[5] = (byte)(fad >> 16);
                    status[6] = (byte)(fad >> 8);
                    status[7] = (byte)fad;
                    status[8] = 0;
                    status[9] = 0;
                    Sh4HostRuntime.Verify(packet[2] + packet[4] < 11);
                    EndSpiPio(&status[packet[2]], packet[4]);
                    break;
                }

                case SPI_REQ_ERROR:
                {
                    Debug.WriteLine("SPI_REQ_ERROR");
                    byte* response = stackalloc byte[10];
                    response[0] = 0xf0;
                    response[1] = 0;
                    response[2] = (byte)SenseKey;
                    response[3] = 0;
                    response[4] = response[5] = response[6] = response[7] = 0;
                    response[8] = (byte)SenseAsc;
                    response[9] = (byte)SenseAscq;
                    EndSpiPio(response, packet[4]);
                    SenseKey = 0;
                    SenseAsc = 0;
                    SenseAscq = 0;
                    _status.Check = 0;
                    break;
                }

                case SPI_REQ_SES:
                {
                    Debug.WriteLine($"SPI_REQ_SES: status {_sectorNumber.Status}");
                    byte* session = stackalloc byte[6];
                    GdRomMedia.GetSessionInfo(session, packet[2]);
                    session[0] = (byte)_sectorNumber.Status;
                    EndSpiPio(session, packet[4]);
                    break;
                }

                case SPI_CD_OPEN:
                    Debug.WriteLine("SPI_CD_OPEN Unhandled");
                    SetState(DriveState.ProcessPacketDone);
                    break;

                case SPI_CD_PLAY:
                {
                    uint parameterType = (uint)packet[1] & 7;
                    Debug.WriteLine($"SPI_CD_PLAY param_type={parameterType}");

                    if (parameterType is 1 or 2)
                    {
                        bool msf = parameterType == 2;
                        _cdda.CurrentAddress.Fad = _cdda.StartAddress.Fad = GetFad(&packet[2], msf);
                        _cdda.EndAddress.Fad = GetFad(&packet[8], msf);
                        if (_cdda.EndAddress.Fad == 0)
                        {
                            byte* session = stackalloc byte[6] { 0, 0, 0, 0, 0, 0 };
                            GdRomMedia.GetSessionInfo(session, 0);
                            _cdda.EndAddress.Fad = (uint)((session[3] << 16) | (session[4] << 8) | session[5]);
                        }
                        _cdda.Repeats = (uint)packet[6] & 0xf;
                        _cdda.Status = CddaStatus.Playing;
                        _sectorNumber.Status = GD_PLAY;
                        _status.Dsc = 1;
                    }
                    else if (parameterType == 7)
                    {
                        if (_cdda.Status == CddaStatus.Paused)
                        {
                            if (_cdda.CurrentAddress.Fad > _cdda.EndAddress.Fad)
                            {
                                _cdda.Status = CddaStatus.Terminated;
                                _sectorNumber.Status = GD_STANDBY;
                            }
                            else
                            {
                                _cdda.Status = CddaStatus.Playing;
                                _sectorNumber.Status = GD_PLAY;
                            }
                        }
                    }
                    else
                    {
                        Sh4HostRuntime.Die("SPI_CD_PLAY: unknown parameter");
                    }

                    Debug.WriteLine($"CDDA StartAddr={_cdda.StartAddress.Fad} EndAddr={_cdda.EndAddress.Fad} repeats={_cdda.Repeats} status={_cdda.Status} CurrAddr={_cdda.CurrentAddress.Fad}");
                    SetState(DriveState.ProcessPacketDone);
                    break;
                }

                case SPI_CD_SEEK:
                {
                    uint parameterType = (uint)packet[1] & 7;
                    Debug.WriteLine($"SPI_CD_SEEK param_type={parameterType}");
                    _sectorNumber.Status = GD_PAUSE;
                    if (_cdda.Status == CddaStatus.Playing)
                        _cdda.Status = CddaStatus.Paused;

                    if (parameterType is 1 or 2)
                    {
                        bool msf = parameterType == 2;
                        _cdda.StartAddress.Fad = _cdda.CurrentAddress.Fad = GetFad(&packet[2], msf);
#if STRICT_MODE
                        _sectorNumber.Status = GD_SEEK;
                        _status.Dsc = 0;
                        Sh4DreamcastRuntime.Scheduler.Request(_schedulerId, Sh4Scheduler.MainClock / 50);
#else
                        _status.Dsc = 1;
#endif
                    }
                    else if (parameterType == 3)
                    {
                        _cdda.StartAddress.Fad = _cdda.CurrentAddress.Fad = 150;
                        _cdda.Status = CddaStatus.NoInfo;
#if STRICT_MODE
                        _sectorNumber.Status = GD_BUSY;
                        _status.Dsc = 0;
                        Sh4DreamcastRuntime.Scheduler.Request(_schedulerId, Sh4Scheduler.MainClock / 50);
#else
                        _sectorNumber.Status = GD_STANDBY;
                        _status.Dsc = 1;
#endif
                    }
                    else if (parameterType == 4)
                    {
                    }
                    else
                    {
                        Sh4HostRuntime.Die("SPI_CD_SEEK  : not known parameter..");
                    }

                    Debug.WriteLine($"CDDA StartAddr={_cdda.StartAddress.Fad} EndAddr={_cdda.EndAddress.Fad} repeats={_cdda.Repeats} status={_cdda.Status} CurrAddr={_cdda.CurrentAddress.Fad}");
                    SetState(DriveState.ProcessPacketDone);
                    break;
                }

                case SPI_CD_SCAN:
                    Debug.WriteLine("SPI_CD_SCAN Unhandled");
                    SetState(DriveState.ProcessPacketDone);
                    break;

                case SPI_GET_SCD:
                {
                    Debug.WriteLine("SPI_GET_SCD");
                    uint format = (uint)packet[1] & 0xf;
                    uint allocationLength = (uint)((packet[3] << 8) | packet[4]);
                    byte* subcode = stackalloc byte[100];
                    uint length = GetSubcode(format, _readParameters.StartSector - 1, subcode);
                    EndSpiPio(subcode, Math.Min(Math.Min(length, allocationLength), 100u));
                    break;
                }

                default:
                    Trace.TraceInformation($"GDROM: Unhandled Sega SPI frame: {packet[0]:X}");
                    _status.Check = 1;
                    SenseKey = 5;
                    SenseAsc = 0x20;
                    SenseAscq = 0;
                    SetState(DriveState.ProcessPacketDone);
                    break;
            }
        }
    }

    internal static partial uint ReadMem(uint address, uint size)
    {
        switch (address)
        {
            case GD_STATUS_Read:
                HollyInterruptController.CancelInterrupt(GdromCommand);
                if ((_driveSelect & 0x10) != 0)
                    return 0;
                Debug.WriteLine($"GDROM: STATUS [cancel int](v={_status.Full:X})");
                return _status.Full;

            case GD_ALTSTAT_Read:
                return _status.Full;

            case GD_BYCTLLO:
                Debug.WriteLine("GDROM: Read From GD_BYCTLLO");
                return _byteCount.Low;

            case GD_BYCTLHI:
                Debug.WriteLine("GDROM: Read From GD_BYCTLHI");
                return _byteCount.High;

            case GD_DATA:
                if (size != 2)
                    Trace.TraceInformation("GDROM: Bad size on DATA REG Read");
                if (Pio.AtEnd)
                {
                    Trace.TraceInformation("GDROM: Illegal Read From DATA (underflow)");
                }
                else
                {
                    uint value = Pio.Read();
                    _byteCount.Full -= sizeof(ushort);
                    if (Pio.AtEnd)
                    {
                        Sh4HostRuntime.Verify(Pio.NextState != DriveState.PioSendData);
                        SetState(Pio.NextState);
                    }
                    return value;
                }
                return 0;

            case GD_DRVSEL:
                Debug.WriteLine("GDROM: Read From DriveSel");
                return _driveSelect;

            case GD_ERROR_Read:
                Debug.WriteLine("GDROM: Read from ERROR Register");
                _error.Sense = (uint)SenseKey;
                return _error.Full;

            case GD_IREASON_Read:
                Debug.WriteLine("GDROM: Read from INTREASON Register");
                return _interruptReason.Full;

            case GD_SECTNUM:
                return _sectorNumber.Full;

            default:
                Trace.TraceInformation($"GDROM: Unhandled read from address {address:X}, Size:{size:X}");
                return 0;
        }
    }

    internal static partial void WriteMem(uint address, uint data, uint size)
    {
        switch (address)
        {
            case GD_BYCTLLO:
                Debug.WriteLine($"GDROM: Write to GD_BYCTLLO = {data:X}, Size:{size:X}");
                _byteCount.Low = (byte)data;
                break;

            case GD_BYCTLHI:
                Debug.WriteLine($"GDROM: Write to GD_BYCTLHI = {data:X}, Size:{size:X}");
                _byteCount.High = (byte)data;
                break;

            case GD_DATA:
                if (size != 2)
                    Trace.TraceInformation("GDROM: Bad size on DATA REG");
                if (_state == DriveState.WaitPacket)
                {
                    fixed (ushort* packet = _packet.Words)
                        packet[_packet.Index] = (ushort)data;
                    _packet.Index++;
                    if (_packet.Index == 6)
                        SetState(DriveState.ProcessPacket);
                }
                else if (_state == DriveState.PioGetData)
                {
                    Pio.Write((ushort)data);
                    if (Pio.AtEnd)
                    {
                        Sh4HostRuntime.Verify(Pio.NextState != DriveState.PioGetData);
                        SetState(Pio.NextState);
                    }
                }
                else
                {
                    Trace.TraceInformation("GDROM: Illegal Write to DATA");
                }
                return;

            case GD_DEVCTRL_Write:
                Trace.TraceInformation("GDROM: Write GD_DEVCTRL (Not implemented on Dreamcast)");
                break;

            case GD_DRVSEL:
                _driveSelect = (_driveSelect & 0xe0) | (data & 0x1f);
                if ((_driveSelect & 0x10) != 0)
                    Trace.TraceInformation("GD_DRVSEL: slave drive selected");
                break;

            case GD_FEATURES_Write:
                Debug.WriteLine("GDROM: Write to GD_FEATURES");
                _features.Full = (byte)data;
                break;

            case GD_SECTCNT_Write:
                Debug.WriteLine($"GDROM: Write to SecCount = {data:X}");
                _sectorCount.Full = (byte)data;
                break;

            case GD_SECTNUM:
                Trace.TraceInformation($"GDROM: Write to SecNum; not possible = {data:X}");
                break;

            case GD_COMMAND_Write:
                if (size != 1)
                {
                    Trace.TraceInformation("GDROM: Bad size on GD_COMMAND reg");
                }
                else if ((_driveSelect & 0x10) == 0)
                {
                    if (data != ATA_NOP && data != ATA_SOFT_RESET)
                        Sh4HostRuntime.Verify(_state == DriveState.WaitCmd);
                    _ataCommand = (byte)data;
                    SetState(DriveState.ProcessAta);
                }
                else
                {
                    Debug.WriteLine($"ATA command to slave drive ignored: {data:x}");
                }
                break;

            default:
                Trace.TraceInformation($"GDROM: Unhandled write to address {address:X} <= {data:X}, Size:{size:X}");
                break;
        }
    }

    internal static bool FastGdRomLoad { get; set; }

    private static int GetGdRomTicks()
    {
        if ((SB_GDST & 1) != 0)
        {
            if (FastGdRomLoad)
                return 512;
            uint length = SB_GDLEN == 0 ? 0x02000000 : SB_GDLEN;
            if (length - SB_GDLEND > 10240)
                return (int)Sh4Scheduler.CyclesForTransfer(10240, 1_800_000);
            return (int)Sh4Scheduler.CyclesForTransfer(Math.Min(10240u, length - SB_GDLEND), 25_000_000);
        }
        return 0;
    }

    private static int ScheduleGdRom(int tag, int cycles, int jitter, nint argument)
    {
        if (_sectorNumber.Status == GD_SEEK)
        {
            _sectorNumber.Status = GD_PAUSE;
            _status.Dsc = 1;
        }
        else if (_sectorNumber.Status == GD_BUSY)
        {
            _sectorNumber.Status = GD_STANDBY;
            _status.Dsc = 1;
        }

        if ((SB_GDST & 1) == 0 || (SB_GDEN & 1) == 0 || (Dma.IsEmpty && _readParameters.RemainingSectors == 0))
            return 0;

        uint source = SB_GDSTARD;
        uint length = (SB_GDLEN == 0 ? 0x02000000 : SB_GDLEN) - SB_GDLEND;
        if ((SB_GDLEN & 0x1f) != 0)
        {
            Sh4HostRuntime.Die("\n!\tGDROM: SB_GDLEN has invalid size !\n");
            return 0;
        }

        if (_readParameters.RemainingSectors == 0)
            length = Math.Min(length, Dma.Size);
        length = Math.Min(length, 10240u);

        uint dmaOperation = Sh4DreamcastRuntime.AddressSpace.Read32WithoutMmu(0xffa00040);
        if ((dmaOperation & 0xffff8201) != 0x8201)
            Trace.TraceInformation($"GDROM: DMAOR has invalid settings ({dmaOperation:X})");
        if (length == 0)
            Trace.TraceInformation($"GDROM: Len: {length:X}, Abnormal Termination !");

        uint transferred = length;
        if (SB_GDDIR == 1)
        {
            while (length != 0)
            {
                Dma.Fill(ref _readParameters);
                uint size = Math.Min(Dma.Size, length);
                fixed (byte* buffer = Dma.Read(size))
                    Sh4DreamcastRuntime.AddressSpace.WriteBlockWithoutMmu(source, (uint*)buffer, size);
                source += size;
                length -= size;
            }
        }
        else
        {
            Trace.TraceWarning($"GDROM: SB_GDDIR {source:X} (TO AICA WAVE MEM?)");
        }

        SB_GDLEND = (SB_GDLEND + transferred) & 0x01ffffe0;
        SB_GDSTARD += transferred;
        if (SB_GDLEND == SB_GDLEN)
        {
            SB_GDST = 0;
            HollyInterruptController.RaiseInterrupt(GdromDma);
        }

        if (_readParameters.RemainingSectors == 0 && Dma.IsEmpty)
            SetState(DriveState.ProcessPacketDone);

        return GetGdRomTicks();
    }

    private static void StartDma(uint address, uint data)
    {
        SB_GDST |= data & 1;
        if (SB_GDST == 1)
        {
            if (SB_GDEN == 0)
            {
                Trace.TraceInformation("Invalid GD-DMA start, SB_GDEN=0. Ignoring it.");
                return;
            }

            SB_GDSTARD = SB_GDSTAR;
            SB_GDLEND = 0;
            Debug.WriteLine($"GDROM-DMA start addr {SB_GDSTAR:X8} len {SB_GDLEN} fad {_readParameters.StartSector:x}");
            int ticks = GetGdRomTicks();
            if (ticks < Sh4Scheduler.TimeSlice)
                ticks = ScheduleGdRom(0, 0, 0, 0);
            if (ticks != 0)
                Sh4DreamcastRuntime.Scheduler.Request(_schedulerId, ticks);
        }
    }

    private static void EnableDma(uint address, uint data)
    {
        SB_GDEN = data & 1;
        if (SB_GDEN == 0 && SB_GDST == 1)
        {
            Debug.WriteLine("GD-DMA aborted");
            SB_GDST = 0;
        }
    }

    internal static int SchedulerId => _schedulerId;

    internal static partial void RegInit()
    {
        _schedulerId = Sh4DreamcastRuntime.Scheduler.Register(0, ScheduleGdRom);
        DiscChange();
    }

    internal static partial void RegTerm()
    {
        Sh4DreamcastRuntime.Scheduler.Unregister(_schedulerId);
        _schedulerId = -1;
    }

    internal static partial void RegReset(bool hard)
    {
        if (hard)
        {
            Registers.SetWriteHandler(SB_GDST_addr, StartDma);
            Registers.SetWriteHandler(SB_GDEN_addr, EnableDma);
            _hardware = default;
            fixed (DriveHardwareInfo* hardware = &_hardware)
            {
                hardware->Speed = 0;
                hardware->StandbyHigh = 0x00;
                hardware->StandbyLow = 0xb4;
                hardware->ReadFlags = 0x19;
                hardware->ReadRetry = 0x08;
                "SE      "u8.CopyTo(new Span<byte>(hardware->DriveInfo, 8));
                "Rev 6.43"u8.CopyTo(new Span<byte>(hardware->SystemVersion, 8));
                "990408"u8.CopyTo(new Span<byte>(hardware->SystemDate, 6));
            }
            GdRomMedia.TermDrive();
        }

        SB_GDST = 0;
        SB_GDEN = 0;
        _state = DriveState.WaitCmd;
        SenseAsc = 0;
        SenseAscq = 0;
        SenseKey = 0;
        _setModeOffset = 0;
        _readParameters = default;
        _packet = default;
        Dma.Clear();
        Pio.Clear();
        _ataCommand = 0;
        _cdda = default;
        _discType = GdRomDiscType.NoDisk;
        _dataWriteMode = 0;
        _driveSelect = 0xa0;
        _error = default;
        _interruptReason = default;
        _features = default;
        _sectorCount = default;
        _sectorNumber = default;
        _status = default;
        _byteCount = default;
        DiscChange();
    }

    internal static partial void Serialize(BinaryWriter writer)
    {
        writer.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref _hardware, 1)));
        writer.Write(SenseAsc);
        writer.Write(SenseAscq);
        writer.Write(SenseKey);
        writer.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref _packet, 1)));
        writer.Write(_setModeOffset);
        writer.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref _readParameters, 1)));
        Dma.Serialize(writer);
        Pio.Serialize(writer);
        writer.Write(_ataCommand);
        writer.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref _cdda, 1)));
        writer.Write((int)_state);
        writer.Write((int)_discType);
        writer.Write(_dataWriteMode);
        writer.Write(_driveSelect);
        writer.Write(_error.Raw);
        writer.Write(_interruptReason.Raw);
        writer.Write(_features.Raw);
        writer.Write(_sectorCount.Raw);
        writer.Write(_sectorNumber.Raw);
        writer.Write(_status.Raw);
        writer.Write(_byteCount.Full);
    }

    internal static partial void Deserialize(BinaryReader reader, int version)
    {
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref _hardware, 1)));
        SenseAsc = reader.ReadInt32();
        SenseAscq = reader.ReadInt32();
        SenseKey = reader.ReadInt32();
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref _packet, 1)));
        _setModeOffset = reader.ReadUInt32();
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref _readParameters, 1)));
        if (version >= 812)
        {
            Dma.Deserialize(reader, version);
        }
        else
        {
            reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref _packet, 1)));
            Dma.Clear();
        }

        Pio.Deserialize(reader, version);
        if (version < 839)
            reader.BaseStream.Seek(sizeof(uint), SeekOrigin.Current);
        _ataCommand = reader.ReadByte();
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref _cdda, 1)));
        _state = (DriveState)reader.ReadInt32();
        _discType = (GdRomDiscType)reader.ReadInt32();
        _dataWriteMode = reader.ReadUInt32();
        _driveSelect = reader.ReadUInt32();
        _error.Raw = reader.ReadUInt32();
        _interruptReason.Raw = reader.ReadUInt32();
        _features.Raw = reader.ReadUInt32();
        _sectorCount.Raw = reader.ReadUInt32();
        _sectorNumber.Raw = reader.ReadUInt32();
        _status.Raw = reader.ReadUInt32();
        _byteCount.Full = reader.ReadUInt16();
    }
}
