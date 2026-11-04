using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Philips.SAA50xx.SAA5050;

public sealed class SignalTests
{
    [Fact]
    public void LoseAdvancesOncePerFallingEdgeAndDewResetsRowCounter()
    {
        var chip = new TeletextSaa5050([]);
        for (var line = 0; line < 27; line++)
        {
            Assert.Equal(line / 10, chip.CharacterRow);
            Assert.Equal(line % 10, chip.CharacterScanline);
            chip.SetLoadOutputShiftEnable(true);
            chip.SetLoadOutputShiftEnable(true);
            chip.SetLoadOutputShiftEnable(false);
            chip.SetLoadOutputShiftEnable(false);
        }
        chip.SetDataEntryWindow(true);
        Assert.Equal(0, chip.CharacterRow);
        Assert.Equal(0, chip.CharacterScanline);
        chip.SetLoadOutputShiftEnable(true);
        chip.SetLoadOutputShiftEnable(false);
        Assert.Equal(0, chip.CharacterScanline);
        chip.SetDataEntryWindow(false);
        Assert.Equal(1, chip.FlashField);
    }

    [Fact]
    public void FlashCountsDewEdgesRatherThanLoseOrCharacterClocks()
    {
        var chip = new TeletextSaa5050([]);
        for (var i = 0; i < 128; i++)
        {
            chip.SetDataEntryWindow(true);
            chip.SetDataEntryWindow(true);
            Assert.Equal(i % 64, chip.FlashField);
            chip.SetDataEntryWindow(false);
            chip.SetDataEntryWindow(false);
            Assert.Equal((i + 1) % 64, chip.FlashField);
            chip.SetCharacterClock(true); chip.SetCharacterClock(false);
        }
    }

    [Fact]
    public void HeldF1DoesNotTickRevealTimerAgain()
    {
        var chip = new TeletextSaa5050([]);
        SendRemote(chip, 0x46);
        chip.SetCharacterClock(true);
        for (var i = 0; i < 160000; i++) chip.SetCharacterClock(true);
        Assert.True(chip.Reveal);
        for (var i = 0; i < 159999; i++)
        {
            chip.SetCharacterClock(false);
            chip.SetCharacterClock(true);
        }
        Assert.True(chip.Reveal);
        chip.SetCharacterClock(false);
        Assert.False(chip.Reveal);
    }

    [Fact]
    public void SerialRemoteDecoderMatchesAllSevenBitCodes()
    {
        for (var code = 0; code < 128; code++)
        {
            var serial = new TeletextSaa5050([]);
            var decoded = new TeletextSaa5050([]);
            SendRemote(serial, 0x5e);
            decoded.ReceiveRemoteCommand(0x5e);
            SendRemote(serial, (byte)code);
            decoded.ReceiveRemoteCommand((byte)code);
            Assert.Equal(decoded.TextMode, serial.TextMode);
            Assert.Equal(decoded.Superimpose, serial.Superimpose);
            Assert.Equal(decoded.Reveal, serial.Reveal);
        }
    }

    [Fact]
    public void RemoteSamplesSecondRisingDlimAndIgnoresHeldClockOrFallingEdge()
    {
        var chip = new TeletextSaa5050([]);
        foreach (var bit in new[] { 6, 0, 1, 2, 3, 4, 5 })
        {
            chip.RemoteData = (0x5e & (1 << bit)) == 0;
            PulseRemote(chip);
            chip.RemoteData = (0x5e & (1 << bit)) != 0;
            chip.SetRemoteClock(true);
            chip.SetRemoteClock(true);
            chip.RemoteData = !chip.RemoteData;
            chip.SetRemoteClock(false);
        }
        Assert.True(chip.TextMode);
        Assert.True(chip.Superimpose);
    }

    [Fact]
    public void GlrResynchronizesDlimPhaseAndDoesNotAcceptBitsWhileLow()
    {
        var chip = new TeletextSaa5050([]);
        PulseRemote(chip);
        chip.SetGeneralLineReset(false);
        for (var i = 0; i < 20; i++) PulseRemote(chip);
        chip.SetGeneralLineReset(true);
        SendRemote(chip, 0x5e);
        Assert.True(chip.Superimpose);
    }

    [Fact]
    public void ResetCancelsPartialRemotePacketAndPendingOutput()
    {
        var chip = new TeletextSaa5050([]);
        chip.RemoteData = true;
        for (var i = 0; i < 8; i++) PulseRemote(chip);
        chip.SetLoadOutputShiftEnable(true);
        chip.Data = 127;
        chip.SetCharacterClock(true); chip.SetCharacterClock(false);
        chip.Reset();
        SendRemote(chip, 0x5e);
        Assert.True(chip.Superimpose);
        for (var i = 0; i < 40; i++) { chip.SetDotClock(true); chip.SetDotClock(false); }
        Assert.Equal(0, chip.Rgb);
        Assert.False(chip.Monochrome);
        Assert.Equal(0, chip.CharacterRow);
    }

    [Fact]
    public void ActiveLowBcsDoublesTheInternalCharacterHeightAndIgnoresCrsParity()
    {
        var rom = new byte[960];
        for (var row = 0; row < 10; row++) rom[('A' - 32) * 10 + row] = (byte)(1 << (row % 5));
        var even = new TeletextSaa5050(rom) { BigCharacterSelectHigh = false };
        var odd = new TeletextSaa5050(rom) { BigCharacterSelectHigh = false, CharacterRoundingSelect = false };
        for (var line = 0; line < 20; line++)
        {
            Assert.Equal(0, even.CharacterRow);
            Assert.Equal(line, even.CharacterScanline);
            even.SetLoadOutputShiftEnable(true); odd.SetLoadOutputShiftEnable(true);
            Assert.Equal(even.Character('A', line * 2, 0).Mask,
                odd.Character('A', line * 2 + 1, 0).Mask);
            even.SetLoadOutputShiftEnable(false); odd.SetLoadOutputShiftEnable(false);
        }
        Assert.Equal(1, even.CharacterRow);
        Assert.Equal(0, even.CharacterScanline);
    }

    [Fact]
    public void TlcLatchesTransmittedDoubleHeightForTheCharacterRow()
    {
        var chip = new TeletextSaa5050([]);
        chip.BeginLine(0);
        Assert.True(chip.TransmittedLargeCharacterHigh);
        chip.Character(13, 0, 0);
        Assert.False(chip.TransmittedLargeCharacterHigh);
        chip.Character(12, 0, 0);
        chip.BeginLine(0);
        Assert.False(chip.TransmittedLargeCharacterHigh);
        chip.BeginLine(1);
        Assert.True(chip.TransmittedLargeCharacterHigh);
    }

    [Fact]
    public void PowerOnStartsInTelevisionConcealAndNotSuperimpose()
    {
        var rom = new byte[960];
        rom[('A' - 32) * 10] = 31;
        var chip = new TeletextSaa5050(rom);
        Assert.False(chip.TextMode);
        Assert.False(chip.Superimpose);
        Assert.Equal(0, chip.Character('A', 0, 0).Mask);
        chip.ReceiveRemoteCommand(0x47);
        Assert.NotEqual(0, chip.Character('A', 0, 0).Mask);
        chip.Reset();
        Assert.Equal(0, chip.Character('A', 0, 0).Mask);
        chip.BeginLine(0);
        Assert.NotEqual(0, chip.Character('A', 0, 0).Mask);
    }

    internal static void SendRemote(TeletextSaa5050 chip, byte command)
    {
        foreach (var bit in new[] { 6, 0, 1, 2, 3, 4, 5 })
        {
            chip.RemoteData = (command & (1 << bit)) != 0;
            PulseRemote(chip); PulseRemote(chip);
        }
    }

    private static void PulseRemote(TeletextSaa5050 chip)
    {
        chip.SetRemoteClock(true); chip.SetRemoteClock(false);
    }
}

internal sealed class SignalFixture
{
    private bool _dot;
    internal TeletextSaa5050 Chip { get; }
    internal SignalFixture(byte[]? rom = null, bool enable = true)
    {
        Chip = new TeletextSaa5050(rom ?? []);
        if (enable) Chip.SetLoadOutputShiftEnable(true);
    }
    internal void Sample(byte data)
    {
        Chip.Data = data;
        Chip.SetCharacterClock(true); Chip.SetCharacterClock(false);
    }
    internal void HalfDots(int count)
    {
        for (var i = 0; i < count; i++) { _dot = !_dot; Chip.SetDotClock(_dot); }
    }
    internal void Character(byte data) { Sample(data); HalfDots(12); }
}
