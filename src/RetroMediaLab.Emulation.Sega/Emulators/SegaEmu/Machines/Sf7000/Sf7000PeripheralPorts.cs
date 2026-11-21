using RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Services;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Sf7000;

internal interface ISf7000SerialDevice
{
    (bool RxD, bool Cts, bool Dsr) InputPins { get; }
    void SetOutputPins(bool txD, bool rts, bool dtr);
}

internal interface ISf7000Usart
{
    byte Read(bool control);
    void Write(bool control, byte value);
    void Connect(ISf7000SerialDevice? device);
}

internal interface ISf7000ParallelPrinter
{
    bool Busy { get; }
    void WriteByte(byte value);
}

internal sealed class Sf7000PeripheralPorts
{
    private const int PrinterBusy = 0x02; // PA1
    private const int PrinterStrobe = 0x80; // PC7, active low

    private readonly Ppi8255 _ppi = new();
    private readonly ISf7000Usart? _serial;
    private bool _strobeHigh = true;
    private bool _strobeDriven;
    private bool _settingPpiMode;
    private byte _printerData;
    private byte _printerDataDrive;

    internal Sf7000PeripheralPorts(ISf7000Usart? serial, Func<byte> readBoardPins, Action<byte, byte> writeBoardPins)
    {
        _serial = serial;
        _ppi.ReadPortA = () => (byte)((readBoardPins() & ~PrinterBusy) | (Printer?.Busy == false ? 0 : PrinterBusy));
        _ppi.PortBDriveChanged = (value, mask) =>
        {
            _printerData = value;
            _printerDataDrive = mask;
        };
        _ppi.PortCDriveChanged = (value, mask) =>
        {
            writeBoardPins((byte)(value & ~PrinterStrobe), (byte)(mask & ~PrinterStrobe));
            bool driven = (mask & PrinterStrobe) != 0;
            bool high = (value & PrinterStrobe) != 0;
            bool send = _strobeDriven && driven && _strobeHigh && !high && _printerDataDrive == byte.MaxValue;
            _strobeHigh = high;
            _strobeDriven = driven;
            if (send && !_settingPpiMode && Printer is { Busy: false } printer)
                printer.WriteByte(_printerData);
        };
    }

    internal ISf7000ParallelPrinter? Printer { get; set; }

    internal void ConnectSerial(ISf7000SerialDevice? device) => _serial?.Connect(device);

    internal void Reset() => _ppi.Reset();

    internal bool TryRead(uint port, out byte value)
    {
        int address = (byte)port;
        switch (address)
        {
            case >= 0xE4 and <= 0xE7:
                value = _ppi.Read(address - 0xE4);
                return true;
            case 0xE8 or 0xE9:
                value = _serial?.Read(control: address == 0xE9) ?? byte.MaxValue;
                return true;
            default:
                value = byte.MaxValue;
                return false;
        }
    }

    internal bool TryWrite(uint port, byte value)
    {
        int address = (byte)port;
        switch (address)
        {
            case >= 0xE4 and <= 0xE7:
                _settingPpiMode = address == 0xE7 && (value & 0x80) != 0;
                try { _ppi.Write(address - 0xE4, value); }
                finally { _settingPpiMode = false; }
                return true;
            case 0xE8 or 0xE9:
                _serial?.Write(control: address == 0xE9, value);
                return true;
            default:
                return false;
        }
    }
}
