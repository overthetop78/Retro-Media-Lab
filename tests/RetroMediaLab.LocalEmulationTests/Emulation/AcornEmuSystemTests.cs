using RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Services;
using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Services;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Machines.AcornSystem.Constants;
using RetroMediaLab.Emulation.Acorn.Common.Machines.AcornSystem.Dictionaries;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Enums;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuSystemTests
{
    [Theory]
    [InlineData(1_000_000, 8)]
    [InlineData(2_000_000, 16)]
    public void OverlappingBasicAndReturnAreAllDeliveredWithoutReleasingThePreviousKeyFirst(int frequency, int gap)
    {
        var keyboard = new SystemKeyboard(frequency);
        var received = new List<byte>();
        keyboard.StrobeChanged = high => { if (!high) received.Add(keyboard.Data); };
        EmulationKey[] keys = [EmulationKey.B, EmulationKey.A, EmulationKey.S, EmulationKey.I, EmulationKey.C, EmulationKey.Return];
        const string text = "basic\r";
        for (var i = 0; i < keys.Length; i++)
        {
            var held = new HashSet<EmulationKey> { keys[i] };
            var chars = new Dictionary<EmulationKey, char> { [keys[i]] = text[i] };
            if (i != 0) { held.Add(keys[i - 1]); chars[keys[i - 1]] = text[i - 1]; }
            keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = held, HostCharacters = chars });
            if (i != 0)
            {
                Assert.False(keyboard.Strobe);
                keyboard.AdvanceTo(keyboard.InputClockCount + gap - 1);
                Assert.False(keyboard.Strobe);
                keyboard.AdvanceTo(keyboard.InputClockCount + 1);
            }
            Assert.True(keyboard.Strobe);
            keyboard.SetInput(EmulationInputSnapshot.Empty with {
                Keys = new HashSet<EmulationKey> { keys[i] }, HostCharacters = chars });
            keyboard.AdvanceTo(keyboard.InputClockCount + frequency / 50);
        }
        keyboard.SetInput(EmulationInputSnapshot.Empty);
        Assert.Equal(text.Select(ch => (byte)ch), received);
    }

    [Theory]
    [InlineData(1_000_000)]
    [InlineData(2_000_000)]
    public void ReleasingTheLatestKeyDoesNotEncodeAnOlderHeldKeyAgain(int frequency)
    {
        var keyboard = new SystemKeyboard(frequency);
        var received = new List<byte>();
        keyboard.StrobeChanged = high => { if (!high) received.Add(keyboard.Data); };
        void Set(params EmulationKey[] keys) => keyboard.SetInput(EmulationInputSnapshot.Empty with {
            Keys = new HashSet<EmulationKey>(keys), HostCharacters = new Dictionary<EmulationKey, char> {
                [EmulationKey.A] = 'a', [EmulationKey.B] = 'b' } });
        Set(EmulationKey.A);
        keyboard.AdvanceTo(frequency / 50);
        Set(EmulationKey.A, EmulationKey.B);
        keyboard.AdvanceTo(frequency / 25);
        Set(EmulationKey.A);
        Assert.Equal((byte)'b', keyboard.Data);
        keyboard.AdvanceTo(frequency / 20);
        Set();
        Assert.Equal(new byte[] { (byte)'a', (byte)'b' }, received);
    }

    [Fact]
    public void RepeatKeyRepeatsTheRetainedAsciiCodeAfterItsCharacterIsReleased()
    {
        var keyboard = new SystemKeyboard(1_000_000);
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.A } });
        keyboard.AdvanceTo(10_000);
        keyboard.SetInput(EmulationInputSnapshot.Empty);
        Assert.Equal((byte)'a', keyboard.Data);
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.RightControl } });
        keyboard.AdvanceTo(60_000);
        Assert.True(keyboard.Strobe);
        Assert.Equal((byte)'a', keyboard.DataWithStrobe);
        keyboard.SetInput(EmulationInputSnapshot.Empty);
        Assert.False(keyboard.Strobe);
        Assert.Null(keyboard.NextClockEvent);
    }

    [Fact]
    public void TapeRecordingPreservesTheWaveformWithoutFeedingItIntoPlaybackMonitoring()
    {
        var tape = new AnalogTape(48000, TapeSignalMode.DirectWaveform);
        try
        {
            tape.Insert(new EmulationTapeSignal(ReadOnlyMemory<short>.Empty, 48000), true);
            tape.ExecuteTape(EmulationTapeCommand.Record);
            foreach (var level in new[] { false, true, true, false })
            {
                tape.SetOutputLevel(level);
                tape.Tick();
                Assert.Equal((short)0, tape.MonitorSample);
            }
            tape.ExecuteTape(EmulationTapeCommand.Stop);
            var samples = tape.RecordedSignal.Samples.ToArray();
            Assert.Equal(4, samples.Length);
            Assert.True(samples[0] < 0);
            Assert.True(samples[1] > 0);
            Assert.Equal(samples[1], samples[2]);
            Assert.Equal(samples[0], samples[3]);
            Assert.Equal((short)0, tape.MonitorSample);
            tape.ExecuteTape(EmulationTapeCommand.Rewind);
            while (tape.TapeActive) tape.Tick();
            tape.ExecuteTape(EmulationTapeCommand.Play);
            foreach (var sample in samples)
            {
                Assert.Equal(sample, tape.MonitorSample);
                tape.Tick();
            }
        }
        finally { tape.Eject(); }
    }

    [Theory]
    [InlineData(EmulationKey.D3, '"', false)]
    [InlineData(EmulationKey.D1, '1', true)]
    [InlineData(EmulationKey.Comma, '?', true)]
    [InlineData(EmulationKey.Comma, ',', false)]
    [InlineData(EmulationKey.Equals, '+', false)]
    [InlineData(EmulationKey.A, 'A', false)]
    [InlineData(EmulationKey.A, 'a', false)]
    public void SystemAsciiKeyboardUsesTheHostCharacterInsteadOfThePhysicalSymbol(EmulationKey key, char character, bool shift)
    {
        var keyboard = new SystemKeyboard(1000000);
        var keys = new HashSet<EmulationKey> { key };
        if (shift) keys.Add(EmulationKey.LeftShift);
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = keys, HostCharacters = new Dictionary<EmulationKey, char> { [key] = character } });
        Assert.Equal((byte)character, keyboard.Data);
        Assert.True(keyboard.Strobe);
        keyboard.SetInput(EmulationInputSnapshot.Empty);
        Assert.False(keyboard.Strobe);
    }

    [Fact]
    public void HostCharacterChangesDoNotRetriggerTheHeldKeyAndUnsupportedCharactersDoNotBecomeAscii()
    {
        var keyboard = new SystemKeyboard(1000000);
        var edges = new List<bool>();
        keyboard.StrobeChanged = edges.Add;
        var input = EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.Comma } };
        keyboard.SetInput(input with { HostCharacters = new Dictionary<EmulationKey, char> { [EmulationKey.Comma] = ',' } });
        keyboard.SetInput(input with { HostCharacters = new Dictionary<EmulationKey, char> { [EmulationKey.Comma] = '?' } });
        Assert.Equal((byte)'?', keyboard.Data);
        Assert.Equal(new[] { false }, edges);
        keyboard.SetInput(input with { HostCharacters = new Dictionary<EmulationKey, char> { [EmulationKey.Comma] = 'é' } });
        Assert.False(keyboard.Strobe);
        Assert.Equal(new[] { false, true }, edges);
    }

    [Theory]
    [InlineData(EmulationKey.Numpad0, '0')]
    [InlineData(EmulationKey.Numpad1, '1')]
    [InlineData(EmulationKey.Numpad2, '2')]
    [InlineData(EmulationKey.Numpad3, '3')]
    [InlineData(EmulationKey.Numpad4, '4')]
    [InlineData(EmulationKey.Numpad5, '5')]
    [InlineData(EmulationKey.Numpad6, '6')]
    [InlineData(EmulationKey.Numpad7, '7')]
    [InlineData(EmulationKey.Numpad8, '8')]
    [InlineData(EmulationKey.Numpad9, '9')]
    [InlineData(EmulationKey.NumpadPeriod, '.')]
    [InlineData(EmulationKey.NumpadSeparator, ',')]
    [InlineData(EmulationKey.NumpadDivide, '/')]
    [InlineData(EmulationKey.NumpadMultiply, '*')]
    [InlineData(EmulationKey.NumpadMinus, '-')]
    [InlineData(EmulationKey.NumpadPlus, '+')]
    [InlineData(EmulationKey.NumpadEnter, '\r')]
    public void SystemAsciiKeyboardSupportsTheKeypadWithoutHostTranslation(EmulationKey key, char character)
    {
        var keyboard = new SystemKeyboard(1000000);
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { key, EmulationKey.LeftShift } });
        Assert.Equal((byte)character, keyboard.Data);
        Assert.True(keyboard.Strobe);
    }

    [Fact]
    public void WaveformTransportPublishesTransitionsAndReleasesTheComparatorOnStop()
    {
        var tape = new AnalogTape(48000, TapeSignalMode.DirectWaveform);
        var levels = new List<bool>();
        tape.InputSignalChanged = levels.Add;
        try
        {
            tape.Insert(new EmulationTapeSignal(new short[] { 100, 100, -100, -100, 100 }, 48000), false);
            tape.ExecuteTape(EmulationTapeCommand.Play);
            for (var clock = 0; clock < 5; clock++) tape.Tick();
            Assert.Equal(new[] { true, false, true }, levels);
            tape.ExecuteTape(EmulationTapeCommand.Stop);
            for (var clock = 0; clock < 10; clock++) tape.Tick();
            Assert.Equal(new[] { true, false, true, false }, levels);
        }
        finally { tape.InputSignalChanged = null; tape.Eject(); }
    }

    [Fact]
    public void AsciiKeyboardExposesTheSameActiveLowStrobeOnThePortAndTheWire()
    {
        var keyboard = new SystemKeyboard(1000000);
        var levels = new List<bool>();
        keyboard.StrobeChanged = levels.Add;
        Assert.Equal(0x80, keyboard.DataWithStrobe & 0x80);
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.A } });
        Assert.Equal((byte)'a', keyboard.DataWithStrobe);
        keyboard.SetInput(EmulationInputSnapshot.Empty);
        Assert.Equal((byte)('a' | 0x80), keyboard.DataWithStrobe);
        Assert.Equal(new[] { false, true }, levels);
    }

    [Theory]
    [InlineData(MachineModel.AcornSystem2, FirmwareCategory.Basic, EmulationFirmwareCompatibility.Compatible)]
    [InlineData(MachineModel.AcornSystem2, FirmwareCategory.OperatingSystem, EmulationFirmwareCompatibility.Incompatible)]
    [InlineData(MachineModel.AcornSystem1, FirmwareCategory.Basic, EmulationFirmwareCompatibility.Incompatible)]
    public void ReconstructedSystemBasicIsIdentifiedForItsVerifiedRole(MachineModel model, FirmwareCategory role, EmulationFirmwareCompatibility expected)
    {
        Assert.Equal(expected, AcornSystemModel.Get(model, HardwareCpu.Mos6502, "6571fca3e86e6dc99c2b56481979fbc24615bff2", role));
    }

    [Fact]
    public void TeletextCrtcDecodesItsEntirePageWithoutClaimingAdjacentPages()
    {
        var video = new SystemVideo(false, new byte[960]);
        try
        {
            for (var address = 0x0800; address < 0x0900; address += 2)
            {
                Assert.True(video.Handles(address));
                Assert.True(video.Handles(address + 1));
                video.Write(address, 14);
                video.Write(address + 1, (byte)(address >> 1));
                video.Write(0x0800, 14);
                Assert.Equal((byte)((address >> 1) & 0x3f), video.Read(0x0801));
            }
            Assert.False(video.Handles(0x0900));
            Assert.False(video.Handles(0x09ff));
        }
        finally { video.Reset(); }
    }

    [Theory]
    [InlineData(0, 9, false)]
    [InlineData(3, 18, true)]
    public void TeletextPublishesEachNativeFieldAndItsCompletedParity(byte mode, byte raster, bool interlaced)
    {
        var video = new SystemVideo(false, new byte[960]);
        try
        {
            ProgramTeletext(video, mode, raster);
            var modes = new List<EmulationVideoScanMode>();
            // Ignore the first field, whose registers were programmed mid-startup.
            while (video.Frame < 5)
            {
                if (!video.TickCharacter() || video.Frame < 2) continue;
                modes.Add(video.ScanMode);
                Assert.Contains(video.CompletedFieldClocks, interlaced ? new long[] { 19968, 20032 } : [19968]);
                Assert.Equal(480 * 250 * 4, video.CompletedFrame.Length);
            }
            if (interlaced)
            {
                Assert.All(modes, value => Assert.NotEqual(EmulationVideoScanMode.Progressive, value));
                for (var index = 1; index < modes.Count; index++) Assert.NotEqual(modes[index - 1], modes[index]);
            }
            else Assert.All(modes, value => Assert.Equal(EmulationVideoScanMode.Progressive, value));
        }
        finally { video.Reset(); }
    }

    [Fact]
    public void DisplayRetainsDrivenPinsAcrossPresentationsAndExtinguishesAfterRelease()
    {
        var display = new SystemHexDisplay();
        display.SelectDigit(2, true, 0);
        display.SetSegments(1, 0);
        Assert.Equal(240, DisplayRed(display.Render(80), 2, false));
        Assert.Equal(240, DisplayRed(display.Render(160), 2, false));
        display.SelectDigit(2, false, 160);
        display.Render(240); // Finish the previous illuminated interval's finite presentation exposure.
        Assert.Equal(24, DisplayRed(display.Render(320), 2, false));
        display.SelectDigit(2, true, 320);
        display.Reset(320);
        Assert.Equal(24, DisplayRed(display.Render(400), 2, false));
    }

    [Fact]
    public void SegmentsWrittenBeforeDigitSelectionDoNotBecomeFullBrightnessOnThePreviousDigit()
    {
        var display = new SystemHexDisplay();
        display.SelectDigit(0, true, 0);
        display.SetSegments(1, 0);
        display.SetSegments(2, 100); // Monitor STA PIB.
        display.SelectDigit(1, true, 104); // Monitor STX PIA, four CPU clocks later.
        var pixels = display.Render(800);
        Assert.Equal(240, DisplayRed(pixels, 0, false));
        Assert.InRange(DisplayRed(pixels, 0, true), 24, 35); // Short physical transition, not a full segment.
        Assert.Equal(24, DisplayRed(pixels, 1, false));
        Assert.Equal(240, DisplayRed(pixels, 1, true));
    }

    private static byte DisplayRed(byte[] pixels, int digit, bool right)
    {
        var x = 32 + digit * 52 + (right ? 32 : 4);
        var y = 42 + (right ? 4 : 0);
        return pixels[(y * 480 + x) * 4 + 2];
    }

    [Fact]
    public void InterlacedTeletextRendersEveryGlyphRowWithinOneCharacterCell()
    {
        var characters = Enumerable.Repeat((byte)0x1f, 960).ToArray();
        var video = new SystemVideo(false, characters);
        byte[] registers = [3, 1, 2, 0x11, 1, 0, 1, 1, 3, 19, 0x20, 0, 0, 0, 0, 0];
        for (byte register = 0; register < registers.Length; register++)
        {
            video.Write(0x0800, register);
            video.Write(0x0801, registers[register]);
        }
        video.Write(0x0400, (byte)'A');
        for (var tick = 0; tick < 200; tick++) video.TickCharacter();
        for (var row = 0; row < 10; row++)
            Assert.Contains(video.CompletedFrame.Skip(row * 480 * 4).Take(12 * 4), pixel => pixel != 0);
        Assert.All(video.CompletedFrame.Skip(10 * 480 * 4), pixel => Assert.Equal(0, pixel));
    }

    [Fact]
    public void TeletextVduPreservesFortyCharactersThroughItsPinPipeline()
    {
        var video = new SystemVideo(false, new byte[960]);
        try
        {
            ProgramTeletext(video, 0, 9);
            video.Write(0x0400, 17);
            video.Write(0x0401, 127);
            video.Write(0x0402, 18);
            video.Write(0x0427, 127);
            for (var clock = 0; clock < 60000; clock++) video.TickCharacter();
            var image = video.CompletedFrame;
            for (var x = 12; x < 24; x++)
            {
                Assert.Equal(255, image[x * 4 + 2]);
                Assert.Equal(0, image[x * 4 + 1]);
            }
            for (var x = 468; x < 480; x++)
            {
                Assert.Equal(255, image[x * 4 + 1]);
                Assert.Equal(0, image[x * 4 + 2]);
            }
        }
        finally { video.Reset(); }
    }

    [Fact]
    public void TeletextVduCountsTenLinesEvenWhenCrtcRowsOnlyLastOneLine()
    {
        var rom = new byte[960];
        rom[('A' - 32) * 10 + 3] = 31;
        var video = new SystemVideo(false, rom);
        try
        {
            ProgramTeletext(video, 3, 1);
            // Conserver un intervalle de blanking après les 16 lignes de VSYNC.
            video.Write(0x0800, 4); video.Write(0x0801, 63);
            video.Write(0x0800, 7); video.Write(0x0801, 40);
            for (var address = 0x0400; address < 0x0800; address++) video.Write(address, (byte)'A');
            for (var clock = 0; clock < 10000; clock++) video.TickCharacter();
            var image = video.CompletedFrame;
            foreach (var y in new[] { 3, 13, 23 }) Assert.Equal(255, image[(y * 480 + 4) * 4 + 2]);
            foreach (var y in new[] { 2, 4, 12, 14, 22, 24 }) Assert.Equal(0, image[(y * 480 + 4) * 4 + 2]);
        }
        finally { video.Reset(); }
    }

    private static void ProgramTeletext(SystemVideo video, byte mode, byte raster)
    {
        byte[] registers = [63, 40, 51, 5, 30, 2, 25, 27, mode, raster, 0x20, 9, 4, 0, 4, 0];
        for (byte register = 0; register < registers.Length; register++)
        {
            video.Write(0x0800, register);
            video.Write(0x0801, registers[register]);
        }
    }

    [Theory]
    [InlineData(MachineModel.AcornSystem1)]
    [InlineData(MachineModel.AcornSystem2)]
    [InlineData(MachineModel.AcornSystem3)]
    [InlineData(MachineModel.AcornSystem4)]
    [InlineData(MachineModel.AcornSystem5)]
    public void SystemProfilesExposeOnlyTheirNativeRuntimeAndFirmware(MachineModel model)
    {
        var configuration = new MachineConfiguration(model);
        Assert.Equal(Emulator.AcornEmu, configuration.Core);
        Assert.True(SystemModelCatalog.IsSystem(model));
        Assert.Single(SystemModelCatalog.All, item => item.Model == model);
        using var core = new EmulatorCore(model);
        Assert.DoesNotContain(core.Options, option => option.IsVisible);
    }

    [Fact]
    public void SystemThreeKeepsBothCpuChoicesWithinOneMachineIdentity()
    {
        var first = new MachineConfiguration(MachineModel.AcornSystem3);
        var second = first with { Options = new Dictionary<string, string>(first.Options)
            { [SystemModelConstants.CpuOption] = nameof(HardwareCpu.Motorola6809) } };
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.Model, second.Model);
        Assert.Contains(HardwareCpu.Motorola6809, SystemModelCatalog.All.Single(item => item.Model == first.Model).Cpus);
    }

    [Fact]
    public void KeyboardProvidesAsciiAndUpdatesModifiersWithoutInventingAnotherKey()
    {
        var keyboard = new SystemKeyboard(1000000);
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.A } });
        Assert.Equal((byte)'a', keyboard.Data);
        Assert.True(keyboard.Strobe);
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.A, EmulationKey.LeftShift } });
        Assert.Equal((byte)'A', keyboard.Data);
        keyboard.SetInput(EmulationInputSnapshot.Empty);
        Assert.False(keyboard.Strobe);
    }

    [Fact]
    public void TapeStaysStoppedUntilPlayAndPreservesRecordingsAcrossPause()
    {
        var tape = new AnalogTape(1000000);
        tape.Insert(new EmulationTapeSignal(new short[480], 48000), true);
        Assert.Equal(EmulationTapeState.Stopped, tape.TapeState);
        tape.ExecuteTape(EmulationTapeCommand.Record);
        tape.SetOutputBit(true);
        for (var cycle = 0; cycle < 10000; cycle++) tape.Tick();
        Assert.True(tape.HasChanges);
        var position = tape.Position;
        tape.ExecuteTape(EmulationTapeCommand.Pause);
        for (var cycle = 0; cycle < 1000; cycle++) tape.Tick();
        Assert.Equal(position, tape.Position);
        Assert.Contains(tape.RecordedSignal.Samples.ToArray(), sample => sample != 0);
        tape.Eject();
        Assert.Empty(tape.TapeCommands);
    }
}
