namespace RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Services;

public sealed partial class RamIo8154
{
    private bool _readActive;
    private bool _writeActive;
    private byte? _readData;
    private int _readAddress;
    private int _writeAddress;
    private bool _readMemory;
    private bool _writeMemory;

    /// <summary>CS0 active low, CS1 active high; M/IO high selects RAM. NRDS/NWDS
    /// are active low. Null is high impedance. Data is captured at the leading edge;
    /// strobed-mode acknowledgments occur at the trailing edge.</summary>
    public byte? SetBus(bool cs0High, bool cs1High, bool memoryHigh, bool readHigh,
        bool writeHigh, int address, byte input)
    {
        var selected = _resetHigh && !cs0High && cs1High;
        var reading = selected && !readHigh && writeHigh;
        var writing = selected && readHigh && !writeHigh;
        if (selected && !readHigh && !writeHigh)
        {
            _readActive = _writeActive = false;
            return null;
        }
        if (_readActive && !reading)
        {
            _readActive = false;
            if (!_readMemory) EndReadIo(_readAddress);
        }
        if (_writeActive && !writing)
        {
            _writeActive = false;
            if (!_writeMemory) EndWriteIo(_writeAddress);
        }
        if (writing && !_writeActive)
        {
            _writeActive = true;
            _writeAddress = address & Constants.RamIoConstants.RamAddressMask;
            _writeMemory = memoryHigh;
            if (memoryHigh) WriteRam(_writeAddress, input);
            else BeginWriteIo(_writeAddress, input);
        }
        if (!reading) return null;
        if (!_readActive)
        {
            _readActive = true;
            _readAddress = address & Constants.RamIoConstants.RamAddressMask;
            _readMemory = memoryHigh;
            _readData = memoryHigh ? ReadRam(_readAddress) : BeginReadIo(_readAddress);
        }
        return _resetHigh ? _readData : null;
    }
}
