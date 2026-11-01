using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using System.Reflection;
using RetroMediaLab.Emulation;
using RetroMediaLab.Tests.Application.TestInfrastructure;
using S = RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants;
using Storage = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using ModelAMachine = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.BbcMicroMachine;
using RetroMediaLab.Emulation.Enums;
using BbcAudio = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.Audio;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories;
using CommonAudioOutput = RetroMediaLab.Emulation.Acorn.Common.Services.AudioOutputController;

namespace RetroMediaLab.Tests.Emulation;

/// <summary>Contrôles autonomes du son BBC : bus et ROM synthétiques, aucune sortie physique ni fichier externe.</summary>
public sealed class AcornEmuAudioTests
{
    [Fact]
    public async Task FactoryConnectsTheProvidedAudioOutputAndSessionDisposesIt()
    {
        var output = new Output();
        var calls = 0;
        var configuration = new MachineConfiguration(MachineModel.BbcMicroModelA, core: Emulator.AcornEmu);
        var context = new EmulatorCreationContext<MachineConfiguration>("synthetic-audio-sessions", "", "", () => { calls++; return output; }, null);
        await using (var machine = new AcornEmuMachineFactory().Create(configuration, context))
        {
            // Vérifie la fabrique et le relais Common sans démarrer de CPU, lire de firmware ou ouvrir de périphérique.
            typeof(RetroMediaLab.Emulation.Acorn.Common.Services.Machine).GetMethod("StartAudio", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(machine, null);
            var audio = (CommonAudioOutput)typeof(RetroMediaLab.Emulation.Acorn.Common.Services.Machine).GetField("_audio", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(machine)!;
            audio.Write(new AudioChunk(new short[] { -123, 456 }, 44100, 2, 1, TimeSpan.Zero, EmulationAudioChannelLayout.Mono));
            Assert.Equal(1, calls); Assert.Equal(44100, output.SampleRate);
            Assert.Equal(EmulationAudioChannelLayout.Mono, output.Layout);
            Assert.Equal(new short[] { -123, 456 }, output.Samples);
        }
        Assert.True(output.Disposed);
    }

    /// <summary>Sortie mémoire possédée par Common : conserve le format et les échantillons reçus, sans matériel.</summary>
    private sealed class Output : IMultichannelAudioOutput
    {
        internal int SampleRate;
        internal EmulationAudioChannelLayout Layout;
        internal short[] Samples = [];
        internal bool Disposed;
        public void Start(int sampleRate) => Start(sampleRate, EmulationAudioChannelLayout.Stereo);
        public void Start(int sampleRate, EmulationAudioChannelLayout channelLayout) { SampleRate = sampleRate; Layout = channelLayout; }
        public void Write(ReadOnlySpan<short> samples) => Samples = samples.ToArray();
        public void Flush() { }
        public void Stop() { }
        public void Dispose() => Disposed = true;
    }

    [Theory]
    [InlineData(250, 500.0)]
    [InlineData(284, 440.1408450704225)]
    [InlineData(0, 122.0703125)]
    public void PcmPreservesHardwarePitchAndSampleRate(int divisor, double expected)
    {
        var audio = new BbcAudio();
        Tone(audio.Write, divisor);
        for (var tick = 0; tick < 2_000_000; tick++) audio.Tick();
        var samples = audio.DrainSamples();
        Assert.Equal(44100, samples.Length);
        var crossings = Enumerable.Range(4411, samples.Length - 4411).Where(index => samples[index - 1] <= 0 && samples[index] > 0).ToArray();
        Assert.True(crossings.Length > 100);
        var measured = 44100.0 * (crossings.Length - 1) / (crossings[^1] - crossings[0]);
        Assert.InRange(measured, expected - 0.06, expected + 0.06);
    }

    [Fact]
    public void PartialChunksKeepTheSamplingPhaseAndMutedChipIsSilent()
    {
        var audio = new BbcAudio();
        Mute(audio.Write);
        var total = 0;
        for (var tick = 1; tick <= 2_000_000; tick++)
        {
            audio.Tick();
            if (tick % 1337 != 0) continue;
            var samples = audio.DrainSamples(); total += samples.Length;
            Assert.All(samples, sample => Assert.Equal(0, sample));
        }
        total += audio.DrainSamples().Length;
        Assert.Equal(44100, total);
        audio.Reset();
        Assert.Empty(audio.DrainSamples());
    }

    [Fact]
    public void ChipUsesThreeIndependentTonesAndTwoDecibelAttenuation()
    {
        for (var channel = 0; channel < 3; channel++)
        {
            var chip = new Sn76489(); Mute(chip.Write);
            chip.Write((byte)(0x80 | channel << 5 | 10)); chip.Write(0);
            chip.Write((byte)(0x90 | channel << 5));
            for (var tick = 0; tick < 16; tick++) chip.Tick();
            var full = chip.Output; Assert.True(full > 0);
            chip.Write((byte)(0x91 | channel << 5));
            Assert.InRange((double)chip.Output / full, 0.794, 0.795);
            chip.Write(15); // Un octet sans bit de sélection modifie le volume déjà sélectionné.
            Assert.Equal(0, chip.Output);
        }
    }

    [Fact]
    public void NoiseIsDeterministicAndWhiteNoiseDiffersFromPeriodicNoise()
    {
        int[] Sequence(byte mode)
        {
            var chip = new Sn76489(); Mute(chip.Write); chip.Write(mode); chip.Write(0xf0);
            var values = new int[96];
            for (var index = 0; index < values.Length; index++)
            {
                for (var tick = 0; tick < (index == 0 ? 16 : 512); tick++) chip.Tick();
                values[index] = chip.Output;
            }
            return values;
        }
        var periodic = Sequence(0xe0); var white = Sequence(0xe4);
        Assert.Equal(periodic, Sequence(0xe0)); Assert.Equal(white, Sequence(0xe4));
        Assert.Contains(periodic, value => value != 0); Assert.Contains(white, value => value != 0);
        Assert.False(periodic.SequenceEqual(white));
        for (var index = 16; index < periodic.Length; index++) Assert.Equal(periodic[index - 15], periodic[index]);
    }

    [Fact]
    public void ViaWritesProduceMonoAndResetClearsPendingAudio()
    {
        var machine = new ModelAMachine(new byte[16384], new byte[16384]);
        void Write(byte value)
        {
            machine.Write(0xfe41, value); // Bus PA : données.
            machine.Write(0xfe40, 0);     // IC32 sortie 0 basse : validation PSG.
            machine.Write(0xfe40, 8);     // Relâche la validation avant l'octet suivant.
        }
        machine.Write(0xfe43, 255); machine.Write(0xfe42, 15);
        Tone(Write, 284);
        using var core = new EmulatorCore(MachineModel.BbcMicroModelA);
        // Assemble une carte synthétique sans passer par la lecture des ROM externes de la fabrique.
        typeof(EmulatorCore).GetField("_machine", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(core, machine);
        for (var frame = 0; frame < 4; frame++) core.RunFrame();
        var chunks = new List<RetroMediaLab.Emulation.Contracts.AudioChunk>();
        while (core.TryDequeueAudio(out var chunk)) chunks.Add(Assert.IsType<RetroMediaLab.Emulation.Contracts.AudioChunk>(chunk));
        Assert.Equal(4, chunks.Count);
        Assert.All(chunks, chunk => { Assert.True(chunk.HasValidLength); Assert.Equal(EmulationAudioChannelLayout.Mono, chunk.ChannelLayout); Assert.Equal(44100, chunk.SampleRate); Assert.Equal(chunk.FrameCount, chunk.InterleavedSamples.Length); });
        Assert.Contains(chunks.SelectMany(chunk => chunk.InterleavedSamples.ToArray()), sample => sample != 0);
        for (var index = 1; index < chunks.Count; index++)
            Assert.InRange((chunks[index].Timestamp - chunks[index - 1].Timestamp).TotalSeconds, chunks[index - 1].FrameCount / 44100.0 - 1e-6, chunks[index - 1].FrameCount / 44100.0 + 1e-6);
        Assert.Equal(0, core.BufferedAudioFrames);
        core.RunFrame(); core.HardReset();
        Assert.Null(core.LatestAudioChunk); Assert.False(core.TryDequeueAudio(out _));
        core.RunFrame(); core.Stop();
        Assert.False(core.TryDequeueAudio(out _));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StartupRestoresInsertedMediaSilentlyAndKeepsLaterPhysicalActions(bool soundEnabled)
    {
        using var core = new EmulatorCore(MachineModel.BbcMicroModelA);
        var mechanics = EmulationSdk.MediaProcessing.CreateDriveMechanics(S.Floppy);
        var image = ControlledDependencies.Simulate<IEmulationDiskImage>((method, _) => method.Name switch
        {
            "get_HasChanges" => false,
            "Dispose" => null,
            _ => throw new InvalidOperationException("Unexpected disk access: " + method.Name)
        });
        Field<IEmulationDriveMechanics?[]>(core, "_floppyMechanics")[0] = mechanics;
        Field<IEmulationDiskImage?[]>(core, "_floppyImages")[0] = image;
        Field<bool[]>(core, "_floppyConnected")[0] = true;
        Field<FloppyDriveSettings?[]>(core, "_floppySettings")[0] = new("ds80", Storage.DefaultSpeed, false, false,
            new Dictionary<string, string> { [Storage.MechanicalSoundOption] = soundEnabled.ToString() });
        mechanics.Insert();
        mechanics.SetMotor(true);
        Assert.True(mechanics.HasPendingAudio);
        typeof(EmulatorCore).GetField("_mechanicalEvent", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(core, S.Insert);
        ResetRestoredMediaAudio(core);
        Assert.False(mechanics.HasPendingAudio);
        Assert.Null(Field<string?>(core, "_mechanicalEvent"));
        mechanics.Eject();
        Assert.Equal(soundEnabled, mechanics.HasPendingAudio);
        mechanics.Reset();
        mechanics.SetMotor(true);
        Assert.Equal(soundEnabled, mechanics.HasPendingAudio);
    }

    [Fact]
    public void StartupDoesNotPretendThatAnEmptyDriveContainsADisk()
    {
        using var core = new EmulatorCore(MachineModel.BbcMicroModelA);
        var mechanics = EmulationSdk.MediaProcessing.CreateDriveMechanics(S.Floppy);
        Field<IEmulationDriveMechanics?[]>(core, "_floppyMechanics")[0] = mechanics;
        Field<bool[]>(core, "_floppyConnected")[0] = true;
        Field<FloppyDriveSettings?[]>(core, "_floppySettings")[0] = new("ds80", Storage.DefaultSpeed, false, false);
        mechanics.SetMotor(true);
        ResetRestoredMediaAudio(core);
        Assert.False(mechanics.HasPendingAudio);
        mechanics.Eject();
        Assert.False(mechanics.HasPendingAudio);
    }

    [Fact]
    public void RestoringDriveStatePreservesTheProgrammedBbcSoundExactly()
    {
        using var reference = CoreWithProgrammedTone();
        using var restored = CoreWithProgrammedTone();
        ResetRestoredMediaAudio(restored);
        reference.RunFrame();
        restored.RunFrame();
        Assert.True(reference.TryDequeueAudio(out var expected));
        Assert.True(restored.TryDequeueAudio(out var actual));
        Assert.Equal(expected!.InterleavedSamples.ToArray(), actual!.InterleavedSamples.ToArray());
    }

    private static EmulatorCore CoreWithProgrammedTone()
    {
        var machine = new ModelAMachine(new byte[16384], new byte[16384]);
        machine.Write(0xfe43, 255);
        machine.Write(0xfe42, 15);
        void Write(byte value)
        {
            machine.Write(0xfe41, value);
            machine.Write(0xfe40, 0);
            machine.Write(0xfe40, 8);
        }
        Tone(Write, 284);
        var core = new EmulatorCore(MachineModel.BbcMicroModelA);
        var transferred = false;
        try
        {
            typeof(EmulatorCore).GetField("_machine", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(core, machine);
            transferred = true;
            return core;
        }
        finally { if (!transferred) core.Dispose(); }
    }

    private static T Field<T>(EmulatorCore core, string name) =>
        (T)typeof(EmulatorCore).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(core)!;

    private static void ResetRestoredMediaAudio(EmulatorCore core) =>
        typeof(EmulatorCore).GetMethod("ResetRestoredMediaAudio", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(core, null);

    /// <summary>Programme un ton par les deux octets matériels, après coupure des quatre voies.</summary>
    private static void Tone(Action<byte> write, int divisor)
    {
        Mute(write); write((byte)(0x80 | (divisor & 15))); write((byte)(divisor >> 4)); write(0x90);
    }

    /// <summary>Atténuation 15 : silence matériel de chaque voie, indépendamment du volume de l'application.</summary>
    private static void Mute(Action<byte> write) { write(0x9f); write(0xbf); write(0xdf); write(0xff); }
}
