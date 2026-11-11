using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Machines.AcornAtom.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Enums;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using System.Reflection;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuAtomTests
{
    [Theory]
    [InlineData(EmulationKey.D3, '"', false, 1, 0x7d)]
    [InlineData(EmulationKey.Slash, '!', false, 2, 0x7d)]
    [InlineData(EmulationKey.Comma, '?', true, 8, 0x77)]
    [InlineData(EmulationKey.Equals, '+', false, 2, 0x7b)]
    [InlineData(EmulationKey.D4, '4', true, 9, 0xfb)]
    [InlineData(EmulationKey.D8, '*', false, 3, 0x7b)]
    [InlineData(EmulationKey.Equals, '=', false, 0, 0x7b)]
    [InlineData(EmulationKey.Semicolon, ':', false, 3, 0xfb)]
    [InlineData(EmulationKey.Backquote, '@', false, 7, 0xf7)]
    [InlineData(EmulationKey.Backquote, '^', false, 5, 0xfe)]
    public void AtomHostSymbolsSelectTheRealContactAndRequiredShift(EmulationKey key, char character, bool hostShift, int column, int expected)
    {
        var keyboard = new AtomKeyboard();
        var keys = new HashSet<EmulationKey> { key };
        if (hostShift) keys.Add(EmulationKey.LeftShift);
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = keys, HostCharacters = new Dictionary<EmulationKey,char> { [key] = character } });
        keyboard.Select((byte)column);
        Assert.Equal(expected, keyboard.Read());
        keyboard.SetInput(EmulationInputSnapshot.Empty);
        Assert.Equal(0xff, keyboard.Read());
    }

    [Theory]
    [InlineData(EmulationKey.Numpad0, 3, 0xfd)]
    [InlineData(EmulationKey.Numpad1, 2, 0xfd)]
    [InlineData(EmulationKey.Numpad2, 1, 0xfd)]
    [InlineData(EmulationKey.Numpad3, 0, 0xfd)]
    [InlineData(EmulationKey.Numpad4, 9, 0xfb)]
    [InlineData(EmulationKey.Numpad5, 8, 0xfb)]
    [InlineData(EmulationKey.Numpad6, 7, 0xfb)]
    [InlineData(EmulationKey.Numpad7, 6, 0xfb)]
    [InlineData(EmulationKey.Numpad8, 5, 0xfb)]
    [InlineData(EmulationKey.Numpad9, 4, 0xfb)]
    [InlineData(EmulationKey.NumpadPlus, 2, 0x7b)]
    [InlineData(EmulationKey.NumpadMultiply, 3, 0x7b)]
    [InlineData(EmulationKey.NumpadMinus, 0, 0xfb)]
    [InlineData(EmulationKey.NumpadDivide, 8, 0xf7)]
    [InlineData(EmulationKey.NumpadPeriod, 9, 0xf7)]
    [InlineData(EmulationKey.NumpadEnter, 6, 0xfd)]
    [InlineData(EmulationKey.Backspace, 4, 0xfd)]
    public void AtomNumericKeypadAndBackspaceDriveTheirIntendedContacts(EmulationKey key, int column, int expected)
    {
        var keyboard = new AtomKeyboard();
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { key } });
        keyboard.Select((byte)column);
        Assert.Equal(expected, keyboard.Read());
    }

    [Fact]
    public void AtomInputSnapshotIsRetainedAndUnsupportedCharactersDoNotPressAnUnrelatedContact()
    {
        var keyboard = new AtomKeyboard();
        var keys = new HashSet<EmulationKey> { EmulationKey.A, EmulationKey.Break, EmulationKey.RightControl };
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = keys });
        keys.Clear();
        keyboard.Select(6);
        Assert.Equal(0xf7, keyboard.Read());
        Assert.True(keyboard.Break);
        Assert.True(keyboard.Repeat);
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.D2 }, HostCharacters = new Dictionary<EmulationKey,char> { [EmulationKey.D2] = 'é' } });
        for (byte column=0;column<16;column++) { keyboard.Select(column); Assert.Equal(0xff, keyboard.Read()); }
        Assert.False(keyboard.Break);
        Assert.False(keyboard.Repeat);
    }

    [Fact]
    public void AtomSymbolsWithConflictingShiftDoNotCreateAThirdCharacter()
    {
        var keyboard = new AtomKeyboard();
        keyboard.SetInput(EmulationInputSnapshot.Empty with
        {
            Keys = new HashSet<EmulationKey> { EmulationKey.D3, EmulationKey.D4 },
            HostCharacters = new Dictionary<EmulationKey,char> { [EmulationKey.D3] = '"', [EmulationKey.D4] = '4' }
        });
        keyboard.Select(1);
        Assert.Equal(0x7d, keyboard.Read());
        keyboard.Select(9);
        Assert.Equal(0x7f, keyboard.Read());
        keyboard.Reset();
        Assert.Equal(0xff, keyboard.Read());
    }

    private static T Component<T>(AtomMachine board, string field) =>
        (T)typeof(AtomMachine).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(board)!;

    [Fact]
    public void InterruptPinsAreConnectedAndFollowTheirSourcesWithoutCpuPolling()
    {
        var board = new AtomMachine(new byte[8192], new byte[280], 1024, 1024, dos: new byte[4096]);
        var cpu = Component<Cpu6502>(board, "_cpu");
        try
        {
            var state = cpu.CaptureState();
            Assert.True(state.IrqPinConnected);
            Assert.True(state.NmiPinConnected);
            Assert.False(state.SamplesReady);
            Assert.False(state.SamplesReset);
            Assert.False(state.SamplesOverflow);
            board.Write(0xb80e, 0xc0);
            board.Write(0xb804, 1);
            board.Write(0xb805, 0);
            for (var tick = 0; tick < 4; tick++) cpu.Clock();
            Assert.True(board.Irq);
            Assert.True(cpu.CaptureState().IrqInput);
            board.Read(0xb804);
            Assert.False(cpu.CaptureState().IrqInput);
            board.Write(0x0a00, 0x3f); // Commande invalide : résultat et interruption du contrôleur.
            Assert.True(board.Nmi);
            Assert.True(cpu.CaptureState().NmiLevel);
            board.Read(0x0a01);
            Assert.False(cpu.CaptureState().NmiLevel);
            board.Reset(true);
            Assert.False(cpu.CaptureState().IrqInput);
            Assert.False(cpu.CaptureState().NmiLevel);
        }
        finally { board.EjectTape(); board.Reset(true); }
    }

    [Fact]
    public void TapeClockReachesTheStrobedPpiAtItsActualEdges()
    {
        var board = new AtomMachine(new byte[8192], new byte[280], 1024, 1024);
        var ppi = Component<Ppi8255>(board, "_ppi");
        try
        {
            Assert.Null(ppi.ReadPortC);
            board.Write(0xb003, 0xbb); // Groupe A en entrée strobed, PC4 = STB.
            board.Write(0xb003, 9); // INTE entrée A.
            var edges = new List<int>();
            var time = 0;
            // Les sept cycles RESET du CPU ont déjà avancé le diviseur de carte.
            Assert.Equal(7, Component<int>(board, "_tapePhase"));
            ppi.InterruptAChanged = level => { if (level) edges.Add(time); };
            for (time = 1; time < 617; time++)
            {
                board.Tick();
                Assert.False(ppi.InterruptA);
            }
            board.Tick();
            Assert.True(ppi.InterruptA);
            Assert.Equal(new[] { 617 }, edges);
            board.Read(0xb000);
            Assert.False(ppi.InterruptA);
        }
        finally { board.EjectTape(); board.Reset(true); }
    }

    [Fact]
    public void RepeatAcknowledgeActsWhenTheInputChangesWithoutWaitingForATick()
    {
        var board = new AtomMachine(new byte[8192], new byte[280], 1024, 1024);
        var ppi = Component<Ppi8255>(board, "_ppi");
        try
        {
            board.Write(0xb003, 0xab); // Groupe A en sortie strobed, PC6 = ACK.
            board.Write(0xb003, 13); // INTE sortie A.
            board.Write(0xb000, 0x5a);
            Assert.False(ppi.InterruptA);
            board.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.RightControl } });
            Assert.False(ppi.InterruptA);
            board.SetInput(EmulationInputSnapshot.Empty);
            Assert.True(ppi.InterruptA);
        }
        finally { board.SetInput(EmulationInputSnapshot.Empty); board.EjectTape(); board.Reset(true); }
    }

    [Fact]
    public void PpiRetainsVideoAndTapeLevelsThroughTheirTransitionsAndReset()
    {
        var board = new AtomMachine(new byte[8192], new byte[280], 1024, 1024);
        var video = Component<AtomVideo>(board, "_video");
        var tape = Component<AnalogTape>(board, "_tape");
        try
        {
            board.InsertTape(new EmulationTapeSignal(new short[] { 10000, -10000, 0, 10000 }, 48000));
            board.TapeTransport.ExecuteTape(EmulationTapeCommand.Play);
            for (var tick = 0; tick < 34000; tick++)
            {
                board.Tick();
                var pins = board.Read(0xb002);
                Assert.Equal(video.FrameSync, (pins & 0x80) != 0);
                Assert.Equal(tape.InputHigh, (pins & 0x20) != 0);
            }
            board.Reset(true);
            Assert.Equal(0xc0, board.Read(0xb002));
        }
        finally { board.SetInput(EmulationInputSnapshot.Empty); board.EjectTape(); board.Reset(true); }
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 2)]
    public void BoardRequestsOnlyInstalledDiskControllers(bool discPack, int drives)
    {
        RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces.IAcornMachine machine =
            new AtomMachine(new byte[8192], new byte[280], 1024, 1024,
                dos: discPack ? new byte[4096] : null);
        try
        {
            Assert.Equal(drives, machine.FloppyDriveCount);
            Assert.Equal(0, machine.HardDiskDriveCount);
        }
        finally { machine.EjectTape(); }
    }

    [Fact]
    public void KeyboardScansTheAtomMatrixAndItsSeparateModifierPins()
    {
        var keyboard = new AtomKeyboard();
        keyboard.Select(6);
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.A, EmulationKey.LeftShift, EmulationKey.LeftControl } });
        Assert.Equal(0x37, keyboard.Read());
        keyboard.Select(9);
        Assert.Equal(0x3f, keyboard.Read());
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.Down } });
        keyboard.Select(2);
        Assert.Equal(0x7e, keyboard.Read());
        keyboard.Select(15);
        keyboard.SetInput(EmulationInputSnapshot.Empty);
        Assert.Equal(0xff, keyboard.Read());
    }

    [Fact]
    public void AtomConfigurationExposesItsOwnMemoryAndOnlyInstalledDiscPackDrives()
    {
        var factory = new AcornEmuMachineFactory();
        var plain = new MachineConfiguration(MachineModel.AcornAtom);
        Assert.Equal(Emulator.AcornEmu, plain.Core);
        Assert.Equal(MachineFamily.AcornAtom, plain.Family);
        Assert.Equal(12, factory.GetMemoryKib(plain));
        Assert.Single(factory.DescribeStorage(plain).AvailableDevices);
        var extended = new MachineConfiguration(MachineModel.AcornAtom, options: new Dictionary<string, string> { [AtomModelConstants.DiscPackOption] = bool.TrueString });
        Assert.Equal(15, factory.GetMemoryKib(extended));
        Assert.Equal(3, factory.DescribeStorage(extended).AvailableDevices.Count);
        Assert.All(factory.DescribeStorage(extended).AvailableDevices.Where(device => device.Slot.Category == EmulationMediaCategory.FloppyDrive),
            device => Assert.Equal(new[] { ".dsk" }, device.AcceptedExtensions));
        using var core = new EmulatorCore(MachineModel.AcornAtom);
        Assert.DoesNotContain(core.Options, option => option.IsVisible);
        Assert.Equal(new[] { "tape.sound", "tape.soundVolume" }, core.Options.Select(option => option.Key));
    }

    [Theory]
    [InlineData(0x10)]
    [InlineData(0x30)]
    [InlineData(0x50)]
    [InlineData(0x70)]
    [InlineData(0x90)]
    [InlineData(0xb0)]
    [InlineData(0xd0)]
    [InlineData(0xf0)]
    public void GraphicsModesCoverTheCompleteActiveRasterWithoutLosingPixelPairs(byte mode)
    {
        var video = new AtomVideo(_ => 0xff, new byte[280]);
        video.SetMode(mode);
        while (!video.Tick()) { }
        var pixels = video.CompletedFrame;
        Assert.Equal(256 * 192 * 4, pixels.Length);
        var reference = pixels.Take(4).ToArray();
        Assert.Contains(reference, component => component != 0);
        for (var pixel = 0; pixel < pixels.Length; pixel += 4)
            Assert.True(pixels.AsSpan(pixel, 4).SequenceEqual(reference));
    }

    [Fact]
    public void BusMapsRomHalvesOptionalMemoryAndDiscPackRamIndependently()
    {
        var rom = Enumerable.Repeat((byte)0xea, 8192).ToArray();
        rom[^4] = 0;
        rom[^3] = 0xf0;
        rom[0] = 0x12;
        rom[4096] = 0x34;
        var machine = new AtomMachine(rom, new byte[280], 1024, 1024, dos: new byte[4096]);
        Assert.Equal(0x12, machine.Read(0xc000));
        Assert.Equal(0x34, machine.Read(0xf000));
        foreach (var address in new ushort[] { 0x0000, 0x2000, 0x2800, 0x3c00, 0x8000 })
        {
            machine.Write(address, 0x56);
            Assert.Equal(0x56, machine.Read(address));
        }
        machine.Write(0x3000, 0x78);
        machine.Write(0x9000, 0x78);
        Assert.Equal(0, machine.Read(0x3000));
        Assert.Equal(0, machine.Read(0x9000));
        machine.Write(0xc000, 0x78);
        Assert.Equal(0x12, machine.Read(0xc000));
    }

    [Fact]
    public void DirectTapePinsRecordTheBoardWaveformWithoutAnotherFskModulator()
    {
        var tape = new AnalogTape(1000000, TapeSignalMode.DirectWaveform);
        tape.Insert(new(new short[480], 48000), true);
        tape.ExecuteTape(EmulationTapeCommand.Record);
        tape.SetOutputLevel(true);
        for (var tick = 0; tick < 1000; tick++) tape.Tick();
        var transition = tape.Position;
        tape.SetOutputLevel(false);
        for (var tick = 0; tick < 1000; tick++) tape.Tick();
        tape.ExecuteTape(EmulationTapeCommand.Stop);
        var samples = tape.RecordedSignal.Samples.Span;
        Assert.True(samples[..transition].ToArray().All(sample => sample > 0));
        Assert.True(samples[transition..tape.Position].ToArray().All(sample => sample < 0));
        tape.Eject();
    }
}
