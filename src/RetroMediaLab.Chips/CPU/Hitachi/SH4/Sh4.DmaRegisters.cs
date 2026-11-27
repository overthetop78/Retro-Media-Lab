using System.Diagnostics;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4DmaRegisters
{
    private const int BurstBytes = 32;
    private const uint TransferEnd = 2;
    private const uint AddressError = 4;
    private const uint OperationAddress = 0xFFA00040;

    internal uint Channel2Control => Registers[11];

    private static readonly Sh4InterruptId[] ChannelInterrupts =
    [
        Sh4InterruptId.DmacDmte0, Sh4InterruptId.DmacDmte1,
        Sh4InterruptId.DmacDmte2, Sh4InterruptId.DmacDmte3
    ];

    internal void StartChannel2()
    {
        uint operation = Registers[16];
        uint source = Registers[8] & 0x1FFFFFE0;
        uint destination = _systemBus.Channel2Destination & 0x01FFFFE0;
        uint length = _systemBus.Channel2Length;

        if ((operation & OperationMask) != 0x8201)
        {
            Trace.TraceInformation($"DMAC: DMAOR has invalid settings ({operation:X}) !");
            return;
        }

        if (source >> 26 != 3)
        {
            Trace.TraceWarning($"DMAC: invalid source address {Registers[8]:x} dest {_systemBus.Channel2Destination:x} len {_systemBus.Channel2Length:x}");
            Registers[16] |= AddressError;
            _systemBus.RaiseChannel2DmaInterrupt();
            return;
        }

        Debug.WriteLine($">> DMAC: Ch2 DMA SRC={source:X} DST={_systemBus.Channel2Destination:X} LEN={_systemBus.Channel2Length:X}");
        if ((destination & 0x01000000) == 0)
        {
            uint offset = source & _memory.RamMask;
            if (offset + length > _memory.RamSize)
            {
                uint firstLength = _memory.RamSize - offset;
                _systemBus.WriteTileAccelerator(destination, _memory.GetMemory(source, firstLength), firstLength / BurstBytes);
                length -= firstLength;
                source += firstLength;
            }

            _systemBus.WriteTileAccelerator(destination, _memory.GetMemory(source, length), length / BurstBytes);
            source += length;
        }
        else
        {
            bool use64BitPath = ((_systemBus.Channel2Destination & 0x02000000) != 0
                ? _systemBus.TextureMemoryMode1
                : _systemBus.TextureMemoryMode0) == 0;

            if (use64BitPath)
            {
                destination = (destination & 0x00FFFFFF) | 0xA4000000;
                uint offset = source & _memory.RamMask;
                if (offset + length > _memory.RamSize)
                {
                    uint firstLength = _memory.RamSize - offset;
                    _memory.CopyBlockWithoutMmu(destination, source, firstLength);
                    length -= firstLength;
                    source += firstLength;
                    destination += firstLength;
                }

                _memory.CopyBlockWithoutMmu(destination, source, length);
                source += length;
                destination += length;
            }
            else
            {
                destination = (destination & 0x00FFFFFF) | 0xA5000000;
                while (length > 0)
                {
                    _systemBus.WriteVideoMemory32(destination, _memory.Read32WithoutMmu(source));
                    length -= 4;
                    source += 4;
                    destination += 4;
                }
            }

            _systemBus.Channel2Destination = destination;
        }

        Registers[11] |= TransferEnd;
        Registers[10] = 0;
        _systemBus.Channel2Start = 0;
        _systemBus.Channel2Length = 0;
        _systemBus.RaiseChannel2DmaInterrupt();
    }

    private void WriteChannelControl(int channel, uint value)
    {
        int index = channel * 4;
        uint control = value & (channel < 2 ? 0xFF0FFFF7u : 0xFF0AFFF7u);
        Registers[index + 3] = control;

        if ((control & 3) == 1 && (Registers[16] & 1) != 0)
        {
            if ((control >> 8 & 15) == 4)
            {
                uint transferSize = control >> 4 & 7;
                uint sourceMode = control >> 12 & 3;
                uint destinationMode = control >> 14 & 3;
                Debug.WriteLine($"DMAC: Manual DMA ch:{channel} TS:{transferSize} src: {Registers[index]:X8} dst: {Registers[index + 1]:X8} len: {Registers[index + 2]:X8} SM: {sourceMode}, DM: {destinationMode}");
                uint source = Registers[index];
                uint length = Registers[index + 2];
                uint destination = Registers[index + 1];
                int sourceStep;
                int destinationStep;
                switch (sourceMode)
                {
                    case 1:
                        sourceStep = 1;
                        break;
                    case 2:
                        sourceStep = -1;
                        break;
                    default:
                        sourceStep = 0;
                        break;
                }
                switch (destinationMode)
                {
                    case 1:
                        destinationStep = 1;
                        break;
                    case 2:
                        destinationStep = -1;
                        break;
                    default:
                        destinationStep = 0;
                        break;
                }

                switch (transferSize)
                {
                    case 0:
                        sourceStep *= sizeof(ulong);
                        destinationStep *= sizeof(ulong);
                        for (; length != 0; length--)
                        {
                            ulong data = _memory.Read64(source);
                            _memory.Write64(destination, data);
                            source = unchecked(source + (uint)sourceStep);
                            destination = unchecked(destination + (uint)destinationStep);
                        }
                        break;
                    case 1:
                        for (; length != 0; length--)
                        {
                            byte data = _memory.Read8(source);
                            _memory.Write8(destination, data);
                            source = unchecked(source + (uint)sourceStep);
                            destination = unchecked(destination + (uint)destinationStep);
                        }
                        break;
                    case 2:
                        sourceStep *= sizeof(ushort);
                        destinationStep *= sizeof(ushort);
                        for (; length != 0; length--)
                        {
                            ushort data = _memory.Read16(source);
                            _memory.Write16(destination, data);
                            source = unchecked(source + (uint)sourceStep);
                            destination = unchecked(destination + (uint)destinationStep);
                        }
                        break;
                    case 4:
                        length *= BurstBytes / sizeof(uint);
                        goto default;
                    default:
                        sourceStep *= sizeof(uint);
                        destinationStep *= sizeof(uint);
                        for (; length != 0; length--)
                        {
                            uint data = _memory.Read32(source);
                            _memory.Write32(destination, data);
                            source = unchecked(source + (uint)sourceStep);
                            destination = unchecked(destination + (uint)destinationStep);
                        }
                        break;
                }

                Registers[index + 3] |= TransferEnd;
                Registers[index] = source;
                Registers[index + 1] = destination;
                Registers[index + 2] = length;
            }

            _interrupts.SetPending(ChannelInterrupts[channel], (Registers[index + 3] & TransferEnd) != 0);
            _interrupts.SetMask(ChannelInterrupts[channel], (Registers[index + 3] & 4) != 0);
        }
    }

    internal override void Initialize()
    {
        base.Initialize();
        SetReadWrite(0xFFA00000, 4);
        SetReadWrite(0xFFA00004, 4);
        SetReadWrite(0xFFA00008, 4, 0x00FFFFFF);
        SetWriteHandler32(0xFFA0000C, (_, value) => WriteChannelControl(0, value));

        SetReadWrite(0xFFA00010, 4);
        SetReadWrite(0xFFA00014, 4);
        SetReadWrite(0xFFA00018, 4, 0x00FFFFFF);
        SetWriteHandler32(0xFFA0001C, (_, value) => WriteChannelControl(1, value));

        SetReadWrite(0xFFA00020, 4);
        SetReadWrite(0xFFA00024, 4);
        SetReadWrite(0xFFA00028, 4, 0x00FFFFFF);
        SetWriteHandler32(0xFFA0002C, (_, value) => WriteChannelControl(2, value));

        SetReadWrite(0xFFA00030, 4);
        SetReadWrite(0xFFA00034, 4);
        SetReadWrite(0xFFA00038, 4, 0x00FFFFFF);
        SetWriteHandler32(0xFFA0003C, (_, value) => WriteChannelControl(3, value));

        SetReadWrite(OperationAddress, 4, 0x00008307);
        Reset();
    }
}
