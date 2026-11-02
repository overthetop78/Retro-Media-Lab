using RetroMediaLab.Chips.Memory.Generic.I2C.EEPROM128.Services;
using RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Services;
using RetroMediaLab.Chips.Custom.Acorn.Master512.Refresh.Services;
using RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services;

namespace RetroMediaLab.Chips.Tests.Memory.Generic.I2C.EEPROM128;
public sealed class EepromTests
{
    [Fact]
    public void CompactEepromAcceptsI2cWritesAndRepeatedStartReads()
    {
        var chip = new CompactEeprom();
        void Pins(bool clock, bool data) => chip.WritePort((byte)((clock ? 0x20 : 0) | (data ? 0x10 : 0)));
        void Start()
        {
            Pins(false, true);
            Pins(true, true);
            Pins(true, false);
            Pins(false, false);
        }

        void Stop()
        {
            Pins(false, false);
            Pins(true, false);
            Pins(true, true);
        }

        void Send(byte value)
        {
            for (var bit = 7; bit >= 0; bit--)
            {
                var high = (value & (1 << bit)) != 0;
                Pins(false, high);
                Pins(true, high);
            }

            Pins(false, true);
            Pins(true, true);
            Assert.Equal(0, chip.ReadPort & 0x10);
            Pins(false, true);
        }

        Start();
        Send(0xa0);
        Send(32);
        Send(0x5a);
        Stop();
        Assert.Equal(0x5a, chip.Export()[32]);
        Start();
        Send(0xa0);
        Send(32);
        Start();
        Send(0xa1);
        var value = 0;
        for (var bit = 0; bit < 8; bit++)
        {
            Pins(true, true);
            value = (value << 1) | ((chip.ReadPort & 0x10) != 0 ? 1 : 0);
            Pins(false, true);
        }

        Pins(true, true);
        Pins(false, true);
        Stop();
        Assert.Equal(0x5a, value);
    }
}
