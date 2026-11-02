namespace RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants;

public static class Dma80186Constants
{
    public const int ChannelCount = 2;
    public const int RegisterBase = 0xc0;
    public const int ChannelStride = 0x10;
    public const int RegisterBytes = 0x0c;
    public const int SourceLow = 0;
    public const int SourceHigh = 2;
    public const int DestinationLow = 4;
    public const int DestinationHigh = 6;
    public const int Count = 8;
    public const int Control = 10;
    public const int HighAddressShift = 16;
    public const int HighAddressMask = 15;
    public const int LowAddressMask = 0xffff;
    public const int ByteShift = 8;
    public const int TransferCycles = 8;
    public const int DestinationReleaseCycles = 2;
    public const ushort DestinationMemory = 0x8000;
    public const ushort DestinationDecrement = 0x4000;
    public const ushort DestinationIncrement = 0x2000;
    public const ushort DestinationDirection = 0x6000;
    public const ushort SourceMemory = 0x1000;
    public const ushort SourceDecrement = 0x0800;
    public const ushort SourceIncrement = 0x0400;
    public const ushort SourceDirection = 0x0c00;
    public const ushort TerminateOnZero = 0x0200;
    public const ushort InterruptOnZero = 0x0100;
    public const ushort Synchronization = 0x00c0;
    public const ushort SourceSynchronization = 0x0040;
    public const ushort DestinationSynchronization = 0x0080;
    public const ushort Priority = 0x0020;
    public const ushort TimerRequest = 0x0010;
    public const ushort ChangeStart = 0x0004;
    public const ushort Started = 0x0002;
    public const ushort Word = 0x0001;
}
