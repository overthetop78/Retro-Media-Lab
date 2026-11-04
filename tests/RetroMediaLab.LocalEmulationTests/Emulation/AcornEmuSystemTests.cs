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

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuSystemTests
{
    [Fact]
    public void MultiplexedDisplayKeepsLitSegmentsUntilTheNextPresentationFrame()
    {
        var keyboard = new SystemKeyboard(1008000);
        keyboard.BeginDisplayFrame();
        keyboard.Select(2);
        keyboard.WriteSegments(0x3f);
        keyboard.WriteSegments(0);
        Assert.Equal(0x3f, keyboard.DisplaySegments[2]);
        keyboard.BeginDisplayFrame();
        Assert.Equal(0, keyboard.DisplaySegments[2]);
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
        Assert.Empty(core.Options);
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
    public void CassetteStaysStoppedUntilPlayAndPreservesRecordingsAcrossPause()
    {
        var cassette = new AnalogCassette(1000000);
        cassette.Insert(new EmulationCassetteSignal(new short[480], 48000), true);
        Assert.Equal(EmulationCassetteState.Stopped, cassette.CassetteState);
        cassette.ExecuteCassette(EmulationCassetteCommand.Record);
        cassette.SetOutputBit(true);
        for (var cycle = 0; cycle < 10000; cycle++) cassette.Tick();
        Assert.True(cassette.HasChanges);
        var position = cassette.Position;
        cassette.ExecuteCassette(EmulationCassetteCommand.Pause);
        for (var cycle = 0; cycle < 1000; cycle++) cassette.Tick();
        Assert.Equal(position, cassette.Position);
        Assert.Contains(cassette.RecordedSignal.Samples.ToArray(), sample => sample != 0);
        cassette.Eject();
        Assert.Empty(cassette.CassetteCommands);
    }
}
