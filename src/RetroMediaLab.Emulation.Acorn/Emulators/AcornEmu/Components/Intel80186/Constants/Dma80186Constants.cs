namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Constants;

internal static class Dma80186Constants
{
    internal const int ChannelCount = 2;
    internal const int RegisterBase = 0xc0;
    internal const int ChannelStride = 0x10;
    internal const int RegisterBytes = 0x0c;
    internal const int SourceLow = 0;
    internal const int SourceHigh = 2;
    internal const int DestinationLow = 4;
    internal const int DestinationHigh = 6;
    internal const int Count = 8;
    internal const int Control = 10;
    internal const int HighAddressShift = 16;
    internal const int HighAddressMask = 15;
    internal const int LowAddressMask = 0xffff;
    internal const int ByteShift = 8;
    internal const int TransferCycles = 8;
    internal const int DestinationReleaseCycles = 2;
    internal const ushort DestinationMemory = 0x8000;
    internal const ushort DestinationDecrement = 0x4000;
    internal const ushort DestinationIncrement = 0x2000;
    internal const ushort DestinationDirection = 0x6000;
    internal const ushort SourceMemory = 0x1000;
    internal const ushort SourceDecrement = 0x0800;
    internal const ushort SourceIncrement = 0x0400;
    internal const ushort SourceDirection = 0x0c00;
    internal const ushort TerminateOnZero = 0x0200;
    internal const ushort InterruptOnZero = 0x0100;
    internal const ushort Synchronization = 0x00c0;
    internal const ushort SourceSynchronization = 0x0040;
    internal const ushort DestinationSynchronization = 0x0080;
    internal const ushort Priority = 0x0020;
    internal const ushort TimerRequest = 0x0010;
    internal const ushort ChangeStart = 0x0004;
    internal const ushort Started = 0x0002;
    internal const ushort Word = 0x0001;
}
