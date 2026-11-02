using I = RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Interrupt80186Constants;
using D = RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Dma80186Constants;
using static RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Cpu80186Constants;

namespace RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services;

public sealed partial class Cpu80186
{
    private sealed class DmaChannel
    {
        public int Source;
        public int Destination;
        public ushort Count;
        public ushort Control;
        public bool Request;
    }

    private readonly DmaChannel[] _dma = [new(), new()];
    private bool _dmaBusReleased;
    private int _lastDmaChannel = D.ChannelCount - 1;

    public void SetDmaRequest(int channel, bool asserted) => _dma[channel].Request = asserted;

    private void ResetDma()
    {
        foreach (var channel in _dma)
        {
            channel.Source = channel.Destination = 0;
            channel.Count = channel.Control = 0;
        }
        _lastDmaChannel = D.ChannelCount - 1;
        _dmaBusReleased = false;
    }

    private static bool DecodeDmaRegister(int offset, out int channel, out int register)
    {
        offset -= D.RegisterBase;
        channel = offset / D.ChannelStride;
        register = offset % D.ChannelStride;
        return offset >= 0 && channel < D.ChannelCount && register < D.RegisterBytes;
    }

    private ushort ReadDmaRegister(int channel, int register)
    {
        var dma = _dma[channel];
        return register switch
        {
            D.SourceLow => (ushort)dma.Source,
            D.SourceHigh => (ushort)(dma.Source >> D.HighAddressShift),
            D.DestinationLow => (ushort)dma.Destination,
            D.DestinationHigh => (ushort)(dma.Destination >> D.HighAddressShift),
            D.Count => dma.Count,
            D.Control => dma.Control,
            _ => 0
        };
    }

    private void WriteDmaRegister(int channel, int register, ushort value)
    {
        var dma = _dma[channel];
        switch (register)
        {
            case D.SourceLow:
                dma.Source = (dma.Source & ~D.LowAddressMask) | value;
                break;
            case D.SourceHigh:
                dma.Source = (dma.Source & D.LowAddressMask) | (value & D.HighAddressMask) << D.HighAddressShift;
                break;
            case D.DestinationLow:
                dma.Destination = (dma.Destination & ~D.LowAddressMask) | value;
                break;
            case D.DestinationHigh:
                dma.Destination = (dma.Destination & D.LowAddressMask) | (value & D.HighAddressMask) << D.HighAddressShift;
                break;
            case D.Count:
                dma.Count = value;
                break;
            case D.Control:
                if ((value & D.ChangeStart) == 0)
                    value = (ushort)((value & ~D.Started) | (dma.Control & D.Started));
                dma.Control = (ushort)(value & ~D.ChangeStart);
                if (((_dma[0].Control | _dma[1].Control) & D.TimerRequest) == 0) _timerDmaRequest = false;
                break;
        }
    }

    private bool DmaReady(int channel)
    {
        var dma = _dma[channel];
        if ((dma.Control & D.Started) == 0) return false;
        var synchronization = dma.Control & D.Synchronization;
        if (synchronization == D.Synchronization) return false;
        if ((dma.Control & D.TimerRequest) != 0) return _timerDmaRequest || synchronization != 0 && dma.Request;
        return synchronization == 0 || dma.Request;
    }

    /// <summary>Un transfert par pas pour rendre la main à l'hôte même pendant un bloc non synchronisé.</summary>
    private bool TransferDma()
    {
        if (_dmaBusReleased)
        {
            _dmaBusReleased = false;
            return false;
        }
        var ready0 = DmaReady(0);
        var ready1 = DmaReady(1);
        if (!ready0 && !ready1) return false;
        var selected = ready0 ? 0 : 1;
        if (ready0 && ready1)
        {
            var priority0 = (_dma[0].Control & D.Priority) != 0;
            var priority1 = (_dma[1].Control & D.Priority) != 0;
            selected = priority0 == priority1 ? 1 - _lastDmaChannel : priority0 ? 0 : 1;
        }
        var dma = _dma[selected];
        if ((dma.Control & D.TimerRequest) != 0) _timerDmaRequest = false;
        var word = (dma.Control & D.Word) != 0;
        var width = word ? 2 : 1;
        var sourceMemory = (dma.Control & D.SourceMemory) != 0;
        var destinationMemory = (dma.Control & D.DestinationMemory) != 0;
        var value = ReadDmaOperand(dma.Source, sourceMemory, word);
        WriteDmaOperand(dma.Destination, destinationMemory, word, value);
        dma.Source = AdvanceDmaAddress(dma.Source, dma.Control & D.SourceDirection, D.SourceIncrement, D.SourceDecrement, width);
        dma.Destination = AdvanceDmaAddress(dma.Destination, dma.Control & D.DestinationDirection, D.DestinationIncrement, D.DestinationDecrement, width);
        dma.Count--;
        if (dma.Count == 0)
        {
            if ((dma.Control & D.InterruptOnZero) != 0) _interruptRequest |= InterruptSourceMask(I.FirstDmaSource + selected);
            if ((dma.Control & D.TerminateOnZero) != 0 || (dma.Control & D.Synchronization) == 0)
                dma.Control &= unchecked((ushort)~D.Started);
        }
        _lastDmaChannel = selected;
        _dmaBusReleased = (dma.Control & D.Synchronization) == D.DestinationSynchronization;
        Cycles += D.TransferCycles + _busPenalty + (_dmaBusReleased ? D.DestinationReleaseCycles : 0);
        return true;
    }

    private static int AdvanceDmaAddress(int address, int direction, int increment, int decrement, int width) =>
        (address + (direction == increment ? width : direction == decrement ? -width : 0)) & AddressMask;

    private ushort ReadDmaOperand(int address, bool memory, bool word) => memory
        ? word ? ReadMemoryWord(address) : ReadMemoryByte(address)
        : word ? ReadIoWord((ushort)address) : ReadIoByte((ushort)address);

    private void WriteDmaOperand(int address, bool memory, bool word, ushort value)
    {
        if (memory)
        {
            if (word) WriteMemoryWord(address, value); else WriteMemoryByte(address, (byte)value);
        }
        else
        {
            if (word) WriteIoWord((ushort)address, value); else WriteIoByte((ushort)address, (byte)value);
        }
    }
}
