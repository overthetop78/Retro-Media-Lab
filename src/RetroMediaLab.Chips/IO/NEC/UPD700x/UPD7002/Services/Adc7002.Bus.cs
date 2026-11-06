using C = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants;

namespace RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Services;

public sealed partial class Adc7002
{
    private bool _writeActive;
    private int _writeRegister;
    private byte _writeData;

    public byte? SetBus(bool chipSelectHigh, bool readHigh, bool writeHigh, int register, byte input)
    {
        // NEC p.6 : le séquenceur attend la fin de WR avant de convertir.
        // L'adresse et les données doivent respecter les temps de maintien du bus.
        if (_writeActive && writeHigh)
        {
            _writeActive = false;
            if (_writeRegister == C.StatusRegister) StartConversion();
            else Write(_writeRegister, _writeData);
        }

        if (chipSelectHigh || (!readHigh && !writeHigh))
        {
            // RD et WR simultanément bas n'est pas un accès spécifié par NEC.
            // Ne pas représenter cette combinaison comme une lecture valide.
            _writeActive = false;
            return null;
        }

        if (!writeHigh)
        {
            _writeActive = true;
            _writeRegister = register & C.RegisterMask;
            _writeData = input;
            if (_writeRegister == C.StatusRegister)
            {
                var previousEoc = EndOfConversionHigh;
                PrepareCommand(input);
                if (!previousEoc) _endOfConversion(true);
                PublishClockEvent();
            }
            return null;
        }

        return readHigh ? null : Read(register);
    }
}
