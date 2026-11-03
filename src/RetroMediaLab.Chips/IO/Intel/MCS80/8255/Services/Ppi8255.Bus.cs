using C = RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Constants.Ppi8255Constants;

namespace RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Services;

public sealed partial class Ppi8255
{
    private bool _readActive;
    private bool _writeActive;
    private int _readRegister;
    private int _writeRegister;
    private byte _writeData;

    public byte? SetBus(bool chipSelectHigh, bool readHigh, bool writeHigh, int register, byte data)
    {
        var selected = !_resetHigh && !chipSelectHigh;
        var reading = selected && !readHigh && writeHigh;
        var writing = selected && readHigh && !writeHigh;
        if (selected && !readHigh && !writeHigh)
        {
            // Outside the manufacturer's legal bus table: no transfer is fabricated.
            _readActive = _writeActive = _readingA = _readingB = _writingA = _writingB = false;
            UpdateInterrupts();
            Publish();
            return null;
        }
        if (_readActive && !reading)
        {
            _readActive = false;
            EndRead(_readRegister);
        }
        if (_writeActive && !writing)
        {
            _writeActive = false;
            EndWrite(_writeRegister, _writeData);
        }
        if (_resetHigh) return null;
        if (writing)
        {
            if (!_writeActive)
            {
                _writeRegister = register & C.RegisterMask;
                _writeActive = true;
                BeginWrite(_writeRegister);
            }
            _writeData = data;
        }
        if (!reading) return null;
        if (!_readActive)
        {
            _readRegister = register & C.RegisterMask;
            _readActive = true;
            BeginRead(_readRegister);
        }
        if (_resetHigh || _readRegister == C.Control) return null;
        var sampled = _readRegister == C.PortC ? ReadC() : ReadData(_readRegister);
        return _resetHigh ? null : sampled;
    }
}
