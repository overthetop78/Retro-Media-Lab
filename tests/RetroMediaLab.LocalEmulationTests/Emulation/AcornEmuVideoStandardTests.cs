using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using RetroMediaLab.Emulation.Enums;
using ModelCatalog = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Dictionaries.ModelCatalog;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.VideoStandardConstants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuVideoStandardTests
{
    [Theory]
    [InlineData("83d91d089dca092d2c8b7c3650ff8143c9069b89", EmulationVideoStandard.Ntsc)]
    [InlineData("0d9bcaf6a393c9ce2359ed700ddb53c232c2c45d", EmulationVideoStandard.Pal)]
    [InlineData("aadf90338ee9d1c85dfa73beba50e930c2a38f10", EmulationVideoStandard.Pal)]
    [InlineData("b39014610a968789afd7695aa04d1277d874405c", null)]
    [InlineData("dbbec4d2540a854c120be3194c7566a2b79d153b", null)]
    [InlineData("unknown", null)]
    public void OnlyRecognizedMosDeterminesTheAutomaticStandard(string sha1, EmulationVideoStandard? expected) =>
        Assert.Equal(expected, VideoStandardFunctions.Identify(sha1));

    [Theory]
    [InlineData(C.Automatic, C.Pal, EmulationVideoStandard.Ntsc, EmulationVideoStandard.Ntsc)]
    [InlineData(C.Pal, C.Ntsc, EmulationVideoStandard.Ntsc, EmulationVideoStandard.Pal)]
    [InlineData(C.Ntsc, C.Pal, EmulationVideoStandard.Pal, EmulationVideoStandard.Ntsc)]
    [InlineData(C.Automatic, C.Ntsc, null, EmulationVideoStandard.Ntsc)]
    [InlineData(C.Automatic, C.Pal, null, EmulationVideoStandard.Pal)]
    public void ManualChoiceOverridesMosAndUnknownMosKeepsPreviousStandard(string choice, string previous, EmulationVideoStandard? detected, EmulationVideoStandard expected)
    {
        var options = new Dictionary<string, string> { [C.Option] = choice, [C.PreviousStandard] = previous };
        Assert.Equal(expected, VideoStandardFunctions.Resolve(options, detected));
    }

    [Theory]
    [InlineData(MachineModel.BbcMicroModelA)]
    [InlineData(MachineModel.BbcMicroModelB)]
    public void BothExistingModelsExposeTheOverrideAndRememberItForCustomMos(MachineModel model)
    {
        var configuration = new MachineConfiguration(model, core: Emulator.AcornEmu);
        var engine = new Engine();
        configuration = Assert.IsType<MachineConfiguration>(engine.ApplySettings(configuration, new Dictionary<string, string?> { [C.Option] = C.Ntsc }));
        configuration = Assert.IsType<MachineConfiguration>(engine.ApplySettings(configuration, new Dictionary<string, string?> { [C.Option] = C.Automatic }));
        var profile = ModelCatalog.Get(configuration);
        Assert.Equal(model, profile.Model);
        Assert.Equal(EmulationVideoStandard.Ntsc, profile.Region);
        Assert.Equal(200, profile.Height);
        Assert.Equal(2_000_000d / 33536, profile.FramesPerSecond);
        var factory = new AcornEmuMachineFactory();
        var field = factory.DescribeSettings(configuration).SelectMany(block => block.Fields).Single(field => field.Id == C.Option);
        Assert.Equal(new[] { C.Automatic, C.Pal, C.Ntsc }, field.Choices!.Select(choice => choice.Id));
        Assert.True(field.RequiresRestart);
        Assert.True(factory.RequiresOptionRestart(configuration, C.Option));
    }

    [Theory]
    [InlineData(EmulationVideoStandard.Pal, 256, 39936)]
    [InlineData(EmulationVideoStandard.Ntsc, 200, 33536)]
    public void FrameBufferAndCpuBudgetFollowTheSelectedStandard(EmulationVideoStandard standard, int height, int cycles)
    {
        var mos = new byte[16384];
        // Boucle JMP C000 : aucun périphérique ni ROM externe requis.
        mos[0] = 0x4c;
        mos[2] = 0xc0;
        mos[^4] = 0;
        mos[^3] = 0xc0;
        var board = new BbcMicroMachine(mos, new byte[16384], videoStandard: standard);
        var before = board.Cpu.Cycles;
        var image = board.RunFrame();
        Assert.Equal(640 * height * 4, image.Length);
        Assert.Equal(height, board.Video.Height);
        Assert.InRange(board.Cpu.Cycles, cycles, cycles + 2);
        Assert.InRange(board.DrainAudio().Length, (cycles - before) * 44100 / 2_000_000 - 1, cycles * 44100 / 2_000_000 + 1);
        board.RunFrame();
        Assert.InRange(board.Cpu.Cycles, cycles * 2, cycles * 2 + 2);
    }
}
