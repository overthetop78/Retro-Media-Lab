using System.Reflection;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuSystemRamIoTests
{
    [Theory]
    [InlineData(EmulationKey.Numpad0, 0, 0x18)]
    [InlineData(EmulationKey.Numpad1, 1, 0x19)]
    [InlineData(EmulationKey.Numpad2, 2, 0x1a)]
    [InlineData(EmulationKey.Numpad3, 3, 0x1b)]
    [InlineData(EmulationKey.Numpad4, 4, 0x1c)]
    [InlineData(EmulationKey.Numpad5, 5, 0x1d)]
    [InlineData(EmulationKey.Numpad6, 6, 0x1e)]
    [InlineData(EmulationKey.Numpad7, 7, 0x1f)]
    [InlineData(EmulationKey.Numpad8, 0, 0x30)]
    [InlineData(EmulationKey.Numpad9, 1, 0x31)]
    public void NumericKeypadDrivesTheExistingDigitContact(EmulationKey key, byte row, byte pins)
    {
        var keyboard = new SystemKeyboard(1008000);
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { key } });
        keyboard.Select(row);
        Assert.Equal(pins, keyboard.ReadMatrix(false));
        keyboard.SetInput(EmulationInputSnapshot.Empty);
        Assert.Equal(0x38 | row, keyboard.ReadMatrix(false));
    }

    [Theory]
    [InlineData(EmulationKey.M, 0, 0x28)]
    [InlineData(EmulationKey.G, 1, 0x29)]
    public void MonitorCommandLetterDrivesTheExistingContact(EmulationKey key, byte row, byte pins)
    {
        var keyboard = new SystemKeyboard(1008000);
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { key } });
        keyboard.Select(row);
        Assert.Equal(pins, keyboard.ReadMatrix(false));
        keyboard.SetInput(EmulationInputSnapshot.Empty);
        Assert.Equal(0x38 | row, keyboard.ReadMatrix(false));
    }

    [Theory]
    [InlineData(EmulationKey.D0, EmulationKey.Numpad0, 0, 0x18)]
    [InlineData(EmulationKey.Minus, EmulationKey.M, 0, 0x28)]
    [InlineData(EmulationKey.X, EmulationKey.G, 1, 0x29)]
    public void ReleasingOneSourceDoesNotOpenAContactStillHeldByTheOtherSource(
        EmulationKey first, EmulationKey second, byte row, byte pins)
    {
        var keyboard = new SystemKeyboard(1008000);
        keyboard.Select(row);
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { first, second } });
        Assert.Equal(pins, keyboard.ReadMatrix(false));
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { first } });
        Assert.Equal(pins, keyboard.ReadMatrix(false));
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { second } });
        Assert.Equal(pins, keyboard.ReadMatrix(false));
        keyboard.SetInput(EmulationInputSnapshot.Empty);
        Assert.Equal(0x38 | row, keyboard.ReadMatrix(false));
    }

    [Fact]
    public void MatrixRetainsIndependentKeysAndCombinesTheLiveTapeAndSelectionLines()
    {
        var keyboard = new SystemKeyboard(1008000);
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.D8, EmulationKey.D0, EmulationKey.X } });
        keyboard.Select(0x40);
        Assert.Equal(0xd0, keyboard.ReadMatrix(true));
        Assert.Equal(0x50, keyboard.ReadMatrix(false));
        Assert.True(keyboard.TapeOutput);
        keyboard.Select(0x41);
        Assert.Equal(0xe9, keyboard.ReadMatrix(true));
        keyboard.Select(2);
        Assert.Equal(0xba, keyboard.ReadMatrix(true));
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.D0 } });
        keyboard.Select(0);
        Assert.Equal(0x98, keyboard.ReadMatrix(true));
        keyboard.Reset();
        Assert.Equal(0xb8, keyboard.ReadMatrix(true));
    }

    [Theory]
    [InlineData((int)SystemCpuCard.Keypad6502, false)]
    [InlineData((int)SystemCpuCard.Standard6502, false)]
    [InlineData((int)SystemCpuCard.Fast6502A, true)]
    [InlineData((int)SystemCpuCard.Motorola6809, true)]
    public void OnlyCpuCardsWithAViaMountThatCircuit(int card, bool hasVia)
    {
        var board = new AcornSystemMachine((SystemCpuCard)card, new byte[512],
            new byte[card == (int)SystemCpuCard.Fast6502A ? 2048 : 960], [], false);
        try
        {
            var via = typeof(AcornSystemMachine).GetField("_via", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(board);
            Assert.Equal(hasVia, via is not null);
        }
        finally { board.Reset(true); }
    }

    [Fact]
    public void FastCpuReceivesTheViaTimerInterruptAndAcknowledgesItOnItsNormalBus()
    {
        var rom = new byte[8192];
        rom[^4] = 0; rom[^3] = 2;
        rom[^2] = 0; rom[^1] = 3;
        var board = new AcornSystemMachine(SystemCpuCard.Fast6502A, rom, new byte[2048], [], false);
        try
        {
            byte[] program = [0x58, 0x4c, 0x01, 0x02]; // CLI; JMP $0201.
            byte[] handler = [0xee, 0x21, 0x00, 0xad, 0x04, 0x0e, 0x40]; // INC $21; LDA T1CL; RTI.
            for (var i = 0; i < program.Length; i++) board.Write((ushort)(0x200 + i), program[i]);
            for (var i = 0; i < handler.Length; i++) board.Write((ushort)(0x300 + i), handler[i]);
            board.Write(0x0e0e, 0xc0);
            board.Write(0x0e04, 0x10);
            board.Write(0x0e05, 0);
            board.RunFrame(); board.DrainAudio();
            Assert.Equal(1, board.Read(0x21));
            Assert.False(board.Irq);
        }
        finally { board.Reset(true); }
    }

    [Fact]
    public void Native8154DrivesIdenticalSegmentsAcrossDigitChangesInMonitorWriteOrder()
    {
        var board = new AcornSystemMachine(SystemCpuCard.Keypad6502, new byte[512], [], [], false);
        try
        {
            var display = (SystemHexDisplay)typeof(AcornSystemMachine)
                .GetField("_display", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(board)!;
            var start = (long)typeof(AcornSystemMachine).GetField("_ticks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(board)!;
            display.Reset(start); // Start the exposure after CPU RESET, before enabling the output drivers.
            board.Write(0x0e22, 0x0f);
            board.Write(0x0e23, 0xff);
            for (byte digit = 0; digit < 8; digit++)
            {
                board.Write(0x0e21, 0x3f);
                board.Write(0x0e20, digit);
                for (var clock = 0; clock < 100; clock++) board.Tick();
            }
            var pixels = display.Render(start + 800);
            for (var digit = 0; digit < 8; digit++)
                Assert.Equal(240, pixels[(42 * 480 + 32 + digit * 52 + 4) * 4 + 2]);
        }
        finally { board.Reset(true); }
    }
}
