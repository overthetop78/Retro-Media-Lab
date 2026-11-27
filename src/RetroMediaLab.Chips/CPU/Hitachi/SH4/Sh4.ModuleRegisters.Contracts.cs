namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4UserBreakRegisters() : Sh4RegisterBank(9);

internal sealed partial class Sh4BusStateController(Sh4Platform platform) : Sh4RegisterBank(19)
{
    private readonly Sh4Platform _platform = platform;
}

internal sealed partial class Sh4ClockGeneratorRegisters() : Sh4RegisterBank(5);

internal sealed partial class Sh4RealTimeClockRegisters() : Sh4RegisterBank(16);

internal sealed partial class Sh4InterruptControllerRegisters(Sh4Interrupts interrupts) : Sh4RegisterBank(5)
{
    private readonly Sh4Interrupts _interrupts = interrupts;
}

internal sealed partial class Sh4TimerRegisters() : Sh4RegisterBank(12);

internal sealed partial class Sh4SerialCommunicationRegisters() : Sh4RegisterBank(8);

internal sealed partial class Sh4SerialFifoRegisters() : Sh4RegisterBank(10);

internal sealed partial class Sh4SerialFifoPort : Sh4SerialPort
{
    private enum StatusBit : ushort
    {
        DataReady = 0x01,
        ReceiveFifoDataFull = 0x02,
        ParityError = 0x04,
        FramingError = 0x08,
        Break = 0x10,
        TransmitFifoDataEmpty = 0x20,
        TransmitEnd = 0x40,
        ReceiveError = 0x80
    }

    private Sh4SerialPipe? _pipe;
    private int _schedulerId = -1;
    private int _breakSchedulerId = -1;
    private int _frameSize = 10;
    private int _cyclesPerBit = Sh4Context.MainClock / 6103;
    private ushort _statusLastRead;
    private readonly Queue<byte> _transmitFifo = new();
    private readonly Queue<byte> _receiveFifo = new();
    private bool _transmitting;

    internal override void SetPipe(Sh4SerialPipe? pipe) => _pipe = pipe;

    internal Sh4SerialPipe? GetPipe() => _pipe;

    internal override void UpdateStatus()
    {
    }

    internal void PostDeserialize() => UpdateBaudRate();
}
