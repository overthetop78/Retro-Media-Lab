using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories;
using S = RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants;
using Storage = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;
using Tape = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.TapeConstants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuAudioMixerTests
{
    [Theory]
    [InlineData(MachineModel.BbcMicroModelA)]
    [InlineData(MachineModel.BbcMicroModelB)]
    [InlineData(MachineModel.BbcMicroModelBPlus)]
    [InlineData(MachineModel.BbcMaster128)]
    public void MixerFollowsConnectedReadersRatherThanMountedMedia(MachineModel model)
    {
        var factory = new AcornEmuMachineFactory();
        var options = new Dictionary<string, string>
        {
            [Tape.ConnectedOption] = bool.FalseString,
            [Storage.Key(0, Storage.ConnectedOption)] = bool.FalseString,
            [Storage.Key(1, Storage.ConnectedOption)] = bool.FalseString,
            [Storage.HardDiskKey(0, Storage.ConnectedOption)] = bool.FalseString,
            [Storage.HardDiskKey(1, Storage.ConnectedOption)] = bool.FalseString,
            [Storage.Key(1, Storage.SoundVolumeOption)] = "73",
            [Storage.Key(1, Storage.MechanicalSoundOption)] = bool.FalseString
        };
        var configuration = new MachineConfiguration(model, core: Emulator.AcornEmu, options: options);
        options = new Dictionary<string, string>(configuration.Options);
        EmulationSettingsBlock[] Channels(MachineConfiguration current) => factory.DescribeSettings(current)
            .Where(block => block.AudioMixerSlot is not null).ToArray();
        var initial = Channels(configuration);
        if (model == MachineModel.BbcMicroModelBPlus)
            Assert.Equal(EmulationMediaSlot.Floppy0, Assert.Single(initial).AudioMixerSlot);
        else Assert.Empty(initial);

        options[Tape.ConnectedOption] = bool.TrueString;
        options[Storage.Key(1, Storage.ConnectedOption)] = bool.TrueString;
        options[Storage.HardDiskKey(1, Storage.ConnectedOption)] = bool.TrueString;
        configuration = configuration with { Options = new Dictionary<string, string>(options) };
        var channels = Channels(configuration);
        Assert.Contains(channels, channel => channel.AudioMixerSlot == EmulationMediaSlot.Tape0);
        var floppy = Assert.Single(channels, channel => channel.AudioMixerSlot == EmulationMediaSlot.Floppy1);
        Assert.Contains(channels, channel => channel.AudioMixerSlot == new EmulationMediaSlot(EmulationMediaCategory.HardDisk, 1));
        Assert.All(channels, channel => Assert.Equal(EmulationMachineTab.Audio, channel.Tab));
        Assert.All(channels.Where(channel => channel.AudioMixerSlot != EmulationMediaSlot.Floppy1),
            channel => Assert.Equal("10", Assert.Single(channel.Fields, field => field.Editor == EmulationSettingsEditor.Slider).Value));
        var volume = Assert.Single(floppy.Fields, field => field.Editor == EmulationSettingsEditor.Slider);
        Assert.Equal("73", volume.Value);
        Assert.True(volume.NumericRange!.IsVertical);
        Assert.False(volume.RequiresRestart);
        var mute = Assert.Single(floppy.Fields, field => field.Editor == EmulationSettingsEditor.Toggle);
        Assert.Equal(bool.FalseString, mute.Value);
        Assert.Equal(bool.FalseString, mute.EnabledValue);
        Assert.Equal(bool.TrueString, mute.DisabledValue);
        Assert.False(mute.RequiresRestart);

        foreach (var device in factory.DescribeMediaDevices(configuration).Concat(factory.DescribeStorage(configuration).AvailableDevices))
        {
            var fields = (device.ConfigurationFields ?? []).Concat((device.ConfigurationBlocks ?? []).SelectMany(block => block.Fields));
            Assert.DoesNotContain(fields, field => field.Id is Storage.MechanicalSoundOption or Storage.SoundVolumeOption
                || field.Id == RetroMediaLab.Emulation.Constants.EmulationTapeSettingsConstants.Sound
                || field.Id == EmulationTapeSettings.Prefix + S.VolumeOption);
        }
        options[Storage.Key(1, Storage.ConnectedOption)] = bool.FalseString;
        configuration = configuration with { Options = new Dictionary<string, string>(options) };
        Assert.DoesNotContain(Channels(configuration), channel => channel.AudioMixerSlot == EmulationMediaSlot.Floppy1);
        options[Storage.Key(1, Storage.ConnectedOption)] = bool.TrueString;
        configuration = configuration with { Options = new Dictionary<string, string>(options) };
        Assert.Equal("73", Assert.Single(Channels(configuration), channel => channel.AudioMixerSlot == EmulationMediaSlot.Floppy1)
            .Fields.Single(field => field.Editor == EmulationSettingsEditor.Slider).Value);
    }

    [Theory]
    [InlineData(MachineModel.BbcMicroModelA)]
    [InlineData(MachineModel.BbcMicroModelB)]
    [InlineData(MachineModel.BbcMicroModelBPlus)]
    [InlineData(MachineModel.BbcMaster128)]
    public void StorageSnapshotCannotOverwriteAudioMixerChanges(MachineModel model)
    {
        var factory = new AcornEmuMachineFactory();
        var configuration = new MachineConfiguration(model, core: Emulator.AcornEmu, options: new Dictionary<string, string>
        {
            [Tape.ConnectedOption] = bool.TrueString,
            [Storage.Key(1, Storage.ConnectedOption)] = bool.TrueString,
            [Storage.HardDiskKey(1, Storage.ConnectedOption)] = bool.TrueString
        });
        var storage = factory.DescribeStorage(configuration);
        // Reproduit une copie de formulaire de stockage conservée avant un changement dans Audio.
        storage = storage with { DeviceSettings = storage.DeviceSettings!.Select(device => device with
        {
            Options = device.Slot.Category == EmulationMediaCategory.TapeDrive
                ? new Dictionary<string, string>(device.Options ?? new Dictionary<string, string>())
                {
                    [RetroMediaLab.Emulation.Constants.EmulationTapeSettingsConstants.Sound] = bool.TrueString,
                    [EmulationTapeSettings.Prefix + S.VolumeOption] = "50"
                }
                : device.Slot.Category == EmulationMediaCategory.HardDisk
                    ? new Dictionary<string, string>(device.Options ?? new Dictionary<string, string>())
                    {
                        [Storage.MechanicalSoundOption] = bool.TrueString, [Storage.SoundVolumeOption] = "50"
                    } : device.Options,
            Floppy = device.Floppy is { } floppy ? floppy with
            {
                Options = new Dictionary<string, string>
                {
                    [Storage.MechanicalSoundOption] = bool.TrueString, [Storage.SoundVolumeOption] = "50"
                }
            } : null
        }).ToArray() };
        var options = new Dictionary<string, string>(configuration.Options);
        var channels = factory.DescribeSettings(configuration).Where(block => block.AudioMixerSlot is not null).ToArray();
        foreach (var field in channels.SelectMany(channel => channel.Fields))
            options[field.Id] = field.Editor == EmulationSettingsEditor.Slider ? "23" : bool.FalseString;
        configuration = configuration with { Options = options };
        var applied = factory.ApplyStorage(configuration, storage);
        foreach (var field in channels.SelectMany(channel => channel.Fields))
            Assert.Equal(options[field.Id], applied.Options[field.Id]);
        Assert.Equal(storage.ConfiguredSlots.OrderBy(slot => slot.ToString()),
            factory.DescribeStorage(applied).ConfiguredSlots.OrderBy(slot => slot.ToString()));
    }
}
