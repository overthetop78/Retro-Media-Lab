using RetroMediaLab.Chips.IO.Acorn.Archimedes.IOC.Services;
using RetroMediaLab.Chips.Memory.Acorn.Archimedes.MEMC.Services;
using RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Enums;
using RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Services;
using System.Buffers.Binary;
using RetroMediaLab.Chips.IO.Acorn.Archimedes.Keyboard.Services;
using static RetroMediaLab.Chips.Tests.Shared.ArchimedesTestCircuits;

namespace RetroMediaLab.Chips.Tests.IO.Acorn.Archimedes.Keyboard;
public sealed class KeyboardTests
{
    [Fact]
    public void KeyboardRequiresBothByteAcknowledgementsAndCommandsCanBeInterleaved()
    {
        var keyboard = CreateKeyboard();
        keyboard.SetInput([0x3c], false, false, false, false, 0, 0);
        keyboard.Advance(20000);
        Assert.Equal(0xc3, KeyboardRead(keyboard));
        keyboard.Write(7);
        Assert.Equal(7, keyboard.Leds);
        keyboard.Write(0x3f);
        Assert.Equal(0xcc, KeyboardRead(keyboard));
        keyboard.Write(0x30);
        Assert.False(keyboard.ScanEnabled);
        keyboard.SetInput([], false, false, false, false, 0, 0);
        keyboard.Advance(20000);
        Assert.False(keyboard.TryTakeTransmit(out _));
        keyboard.Write(0x20);
        Assert.Equal(0x81, KeyboardRead(keyboard));
        keyboard.Write(0x31);
        keyboard.Advance(20000);
        Assert.Equal(0xd3, KeyboardRead(keyboard));
        keyboard.Write(0x3f);
        Assert.Equal(0xdc, KeyboardRead(keyboard));
    }

    [Fact]
    public void KeyboardMouseCountersLockTogetherOnOverflowUntilPacketIsSent()
    {
        var keyboard = CreateKeyboard();
        keyboard.SetInput([], false, false, false, false, 100, -100);
        keyboard.SetInput([], false, false, false, false, -5, 5);
        keyboard.Advance(20000);
        Assert.Equal(63, KeyboardRead(keyboard));
        keyboard.Write(0x3f);
        Assert.Equal(63, KeyboardRead(keyboard));
        keyboard.Write(0x33);
        keyboard.Advance(20000);
        Assert.False(keyboard.TryTakeTransmit(out _));
        keyboard.Write(0x22);
        keyboard.Advance(20000);
        Assert.Equal(0, KeyboardRead(keyboard));
        keyboard.Write(0x3f);
        Assert.Equal(0, KeyboardRead(keyboard));
    }

    [Fact]
    public void KeyboardInvalidAckRestartsProtocolRatherThanInjectingGuestMemory()
    {
        var keyboard = CreateKeyboard();
        keyboard.Write(0x40 | 5);
        Assert.Equal(0xe5, KeyboardRead(keyboard));
        keyboard.Write(0x3f);
        Assert.Equal(0xff, KeyboardRead(keyboard));
        Assert.False(keyboard.ScanEnabled);
    }
}
