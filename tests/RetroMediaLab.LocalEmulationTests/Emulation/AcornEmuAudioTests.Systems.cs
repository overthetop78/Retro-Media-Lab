using System.Reflection;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Tests.Application.TestInfrastructure;
using S = RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants;
using Storage = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;
using Tape = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.TapeConstants;

namespace RetroMediaLab.Tests.Emulation;

public sealed partial class AcornEmuAudioTests
{
    [Theory]
    [InlineData(MachineModel.AcornSystem1, false, true, false)]
    [InlineData(MachineModel.AcornSystem2, false, true, false)]
    [InlineData(MachineModel.AcornSystem3, false, false, true)]
    [InlineData(MachineModel.AcornSystem3, true, true, true)]
    [InlineData(MachineModel.AcornSystem4, false, false, true)]
    [InlineData(MachineModel.AcornSystem5, false, false, true)]
    public void SystemMixerContainsOnlyItsMountedHardware(MachineModel model, bool motorola, bool tape, bool disk)
    {
        var options = new Dictionary<string, string>
        {
            [Tape.ConnectedOption] = tape.ToString(),
            [Storage.Key(0, Storage.ConnectedOption)] = disk.ToString(),
            [Storage.Key(1, Storage.ConnectedOption)] = disk.ToString()
        };
        if (motorola) options["acornemu.system.cpu"] = nameof(HardwareCpu.Motorola6809);
        var configuration = new MachineConfiguration(model, core: Emulator.AcornEmu, options: options);
        var channels = new AcornEmuMachineFactory().DescribeSettings(configuration)
            .Where(block => block.AudioMixerSlot is not null).ToArray();
        Assert.Equal((tape ? 1 : 0) + (disk ? 2 : 0), channels.Length);
        Assert.Equal(tape, channels.Any(channel => channel.AudioMixerSlot == EmulationMediaSlot.Tape0));
        Assert.Equal(disk, channels.Any(channel => channel.AudioMixerSlot == EmulationMediaSlot.Floppy0));
        Assert.Equal(disk, channels.Any(channel => channel.AudioMixerSlot == EmulationMediaSlot.Floppy1));
        Assert.All(channels, channel =>
        {
            Assert.Equal(EmulationMachineTab.Audio, channel.Tab);
            Assert.Equal("10", Assert.Single(channel.Fields, field => field.NumericRange?.IsVertical == true).Value);
        });
    }

    [Theory]
    [InlineData(MachineModel.AcornSystem3, false)]
    [InlineData(MachineModel.AcornSystem3, true)]
    [InlineData(MachineModel.AcornSystem4, false)]
    [InlineData(MachineModel.AcornSystem5, false)]
    public void SystemDriveVolumesReachBothReadersWithoutPeriodicReconfiguration(MachineModel model, bool motorola)
    {
        var card = motorola ? SystemCpuCard.Motorola6809
            : model == MachineModel.AcornSystem5 ? SystemCpuCard.Fast6502A : SystemCpuCard.Standard6502;
        var board = new AcornSystemMachine(card, new byte[512], new byte[card == SystemCpuCard.Fast6502A ? 2048 : 960], [], true);
        using var core = new EmulatorCore(model);
        typeof(EmulatorCore).GetField("_machine", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(core, board);
        var commands = new List<(int Drive, double Volume, bool Enabled)>();
        var receivers = Enumerable.Range(0, 2).Select(drive => ControlledDependencies.Simulate<IEmulationDriveMechanics>((method, args) =>
        {
            if (method.Name == "Configure") commands.Add((drive, (double)args![1]!, (bool)args[2]!));
            else if (method.Name != "Reset") throw new InvalidOperationException(method.Name);
            return null;
        })).ToArray();
        void Set(string name, object value) => typeof(EmulatorCore).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(core, value);
        Set("_floppyMechanics", receivers);
        Set("_floppyConnected", new[] { true, true });
        Set("_floppySettings", new FloppyDriveSettings?[]
        {
            new("ss80", Storage.DefaultSpeed, false, false), new("ss80", Storage.DefaultSpeed, false, false)
        });
        for (var drive = 0; drive < 2; drive++)
        {
            core.SetOption(Storage.Key(drive, Storage.SoundVolumeOption), "10");
            Assert.Equal((drive, 10d, true), commands[^1]);
            core.SetOption(Storage.Key(drive, Storage.MechanicalSoundOption), bool.FalseString);
            Assert.Equal((drive, 10d, false), commands[^1]);
            core.SetOption(Storage.Key(drive, Storage.MechanicalSoundOption), bool.TrueString);
            Assert.Equal((drive, 10d, true), commands[^1]);
        }
        for (var clock = 0; clock < 1000; clock++) board.Tick();
        Assert.Equal(6, commands.Count);
        Assert.DoesNotContain(board.DrainAudio(), sample => sample != 0);
    }
}
