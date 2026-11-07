using RetroMediaLab.Emulation.Atari.Common.Constants;
using RetroMediaLab.Emulation.Atari.Common.Machines.Common.Constants;
using RetroMediaLab.Emulation.Atari.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Atari.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Atari.Common.Machines.Common.Functions;
using RetroMediaLab.Emulation.Atari.Common.Machines.Atari8Bit.Constants;
using RetroMediaLab.Emulation.Atari.Common.Services;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.Tests.Emulation.MachineAdapters;

internal static class AtariTapeScenarios
{
    internal static void PlaybackSendsOneReturnPulse()
    {
        Assert.Equal(EmulationTapeState.Empty, TapeStateFunctions.From(mounted: false, motorActive: false));
        Assert.Equal(EmulationTapeState.Stopped, TapeStateFunctions.From(mounted: true, motorActive: false));
        Assert.Equal(EmulationTapeState.Playing, TapeStateFunctions.From(mounted: true, motorActive: true));

        var controller = new TapeInputController(Configuration(MachineModel.Atari800, Tape(autoBoot: false), tapeBoot: false));
        Assert.Equal([EmulationTapeCommand.Play], controller.AvailableCommands);
        var unavailable = new TapeInputController(new MachineConfiguration(MachineModel.Atari5200, core: Emulator.A5200));
        Assert.Empty(unavailable.AvailableCommands);
        controller.SetPhysicalInput(EmulationInputSnapshot.Empty with
        {
            Keys = new HashSet<EmulationKey> { EmulationKey.A }
        });

        controller.Execute(EmulationTapeCommand.Play);
        var pulse = controller.NextFrame();
        Assert.Equal(2, pulse.Keys.Count);
        Assert.Contains(EmulationKey.A, pulse.Keys);
        Assert.Contains(EmulationKey.Return, pulse.Keys);
        Assert.Equal([EmulationKey.A], controller.NextFrame().Keys);
    }

    internal static void AutomaticBootUsesTheCoreOptionForEveryComputerModel()
    {
        foreach (var model in new[]
                 {
                     MachineModel.Atari400, MachineModel.Atari800,
                     MachineModel.Atari800Xl, MachineModel.Atari130Xe,
                     MachineModel.Xegs, MachineModel.XlXe
                 })
        {
            var media = Tape(autoBoot: true);
            var configuration = Configuration(model, media, tapeBoot: false);
            var prepared = new Atari800PreparedMedia(media, Atari800ContentType.Tape, "virtual.cas", null);

            Assert.Equal(EightBitSettingsConstants.Enabled, Atari800MediaFunctions.ApplyOptions(configuration, prepared) [EightBitSettingsConstants.TapeBootOptionKey]);
            Assert.Equal(EightBitSettingsConstants.Enabled,
                Atari800MediaFunctions.ApplyOptions(configuration with
                    {
                        Options = new Dictionary<string, string>(configuration.Options)
                        {
                            [EightBitSettingsConstants.SioAccelerationOptionKey] =
                                EightBitSettingsConstants.Disabled
                        }
                    }, prepared)[EightBitSettingsConstants.SioAccelerationOptionKey]);
        }
    }

    internal static void AutomaticBootSendsOneDelayedReturnOnlyForOriginalModels()
    {
        foreach (var model in new[] { MachineModel.Atari400, MachineModel.Atari800 })
        {
            var controller = new TapeInputController(Configuration(model, Tape(autoBoot: true), tapeBoot: false));
            controller.SetPhysicalInput(EmulationInputSnapshot.Empty with
            {
                Keys = new HashSet<EmulationKey> { EmulationKey.A }
            });

            for (var frame = 0; frame < 75; frame++)
                Assert.Equal([EmulationKey.A], controller.NextFrame().Keys);

            var pulse = controller.NextFrame();
            Assert.Equal(2, pulse.Keys.Count);
            Assert.Contains(EmulationKey.A, pulse.Keys);
            Assert.Contains(EmulationKey.Return, pulse.Keys);
            Assert.Equal([EmulationKey.A], controller.NextFrame().Keys);
        }

        foreach (var model in new[]
                 {
                     MachineModel.Atari800Xl, MachineModel.Atari130Xe,
                     MachineModel.Xegs, MachineModel.XlXe
                 })
        {
            var controller = new TapeInputController(Configuration(model, Tape(autoBoot: true), tapeBoot: false));
            for (var frame = 0; frame <= 75; frame++)
                Assert.DoesNotContain(EmulationKey.Return, controller.NextFrame().Keys);
        }
    }

    private static MachineConfiguration Configuration(MachineModel model, MediaConfiguration media, bool tapeBoot) => new(model, media: [media],
        options: new Dictionary<string, string>
        {
            [EightBitSettingsConstants.TapeBootOptionKey] = tapeBoot ? EightBitSettingsConstants.Enabled : EightBitSettingsConstants.Disabled
        });

    private static MediaConfiguration Tape(bool autoBoot) => new("virtual.cas", MediaCategory.Tape, EmulationMediaSlot.Tape0, TapeAutoBoot: autoBoot);
}
