using System.Reflection;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Constants;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using RetroMediaLab.Tests.Application.TestInfrastructure;
using S = RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants;
using Storage = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;

namespace RetroMediaLab.Tests.Emulation;

public sealed partial class AcornEmuAudioTests
{
    /// <summary>Les commandes mécaniques sont reçues aux changements, sans reprogrammer ni mélanger le PSG.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TapeAndHardDiskHotVolumesNotifyOnlyOnCommandsAndPreserveChipPcm(bool hardDisk)
    {
        using var reference = CoreWithProgrammedTone();
        using var core = CoreWithProgrammedTone();
        var commands = new List<(double Volume, bool Enabled)>();
        var resets = 0;
        var receiver = ControlledDependencies.Simulate<IEmulationDriveMechanics>((method, args) =>
        {
            switch (method.Name)
            {
                case "Configure": commands.Add(((double)args![1]!, (bool)args[2]!)); return null;
                case "Reset": resets++; return null;
                case "SetTapeOperation": return null;
                default: throw new InvalidOperationException("Unexpected mechanical call: " + method.Name);
            }
        });
        void Set(string name, object value) => typeof(EmulatorCore).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(core, value);
        string volumeKey, soundKey;
        if (hardDisk)
        {
            Set("_hardDiskConnected", new[] { true });
            Set("_hardDiskSettings", new (bool, int, int, int)[] { (false, 0, 0, Storage.DefaultSpeedPercent) });
            Set("_hardDiskSoundVolume", new[] { S.DefaultVolumePercent });
            Set("_hardDiskSoundEnabled", new[] { true });
            Set("_hardDiskMechanics", new IEmulationDriveMechanics?[] { receiver });
            volumeKey = Storage.HardDiskKey(0, Storage.SoundVolumeOption);
            soundKey = Storage.HardDiskKey(0, Storage.MechanicalSoundOption);
        }
        else
        {
            Set("_cassetteMechanics", receiver);
            volumeKey = EmulationCassetteSettings.Prefix + S.VolumeOption;
            soundKey = EmulationTapeSettingsConstants.Sound;
        }
        Assert.False(Assert.Single(core.Options, option => option.Key == volumeKey).RequiresRestart);
        foreach (int volume in new[] { 0, 50, 100 })
        {
            core.SetOption(volumeKey, volume.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal(((double)volume, true), commands[^1]);
        }
        core.SetOption(soundKey, bool.FalseString);
        Assert.Equal((100d, false), commands[^1]);
        core.SetOption(soundKey, bool.TrueString);
        Assert.Equal((100d, true), commands[^1]);
        Assert.Equal(5, commands.Count);

        reference.RunFrame(); core.RunFrame();
        Assert.Equal(5, commands.Count);
        Assert.True(reference.TryDequeueAudio(out var expected));
        Assert.True(core.TryDequeueAudio(out var actual));
        Assert.Equal(expected!.InterleavedSamples.ToArray(), actual!.InterleavedSamples.ToArray());
        core.Stop();
        Assert.True(resets > 0);
    }
}
