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

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuAtomTests
{
    [Fact]
    public void PpiDirectionsAndBitCommandsKeepInputsSeparateFromOutputLatches()
    {
        var ppi = new AtomPpi8255 { ReadPortA = () => 0x55, ReadPortB = () => 0xaa, ReadPortC = () => 0xf0 };
        Assert.Equal(0x55, ppi.Read(0));
        ppi.Write(3, 0x8a);
        ppi.Write(0, 0x12);
        ppi.Write(1, 0x34);
        ppi.Write(2, 0x05);
        Assert.Equal(0x12, ppi.Read(0));
        Assert.Equal(0xaa, ppi.Read(1));
        Assert.Equal(0xf5, ppi.Read(2));
        ppi.Write(3, 0x03);
        Assert.Equal(0xf7, ppi.Read(2));
        ppi.Write(3, 0x00);
        Assert.Equal(0xf6, ppi.Read(2));
        ppi.Write(3, 0x80);
        Assert.Equal(0, ppi.Read(0));
        Assert.Equal(0, ppi.Read(2));
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
        Assert.Empty(core.Options);
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
    public void DirectCassettePinsRecordTheBoardWaveformWithoutAnotherFskModulator()
    {
        var tape = new AnalogCassette(1000000, CassetteSignalMode.DirectWaveform);
        tape.Insert(new(new short[480], 48000), true);
        tape.ExecuteCassette(EmulationCassetteCommand.Record);
        tape.SetOutputLevel(true);
        for (var tick = 0; tick < 1000; tick++) tape.Tick();
        var transition = tape.Position;
        tape.SetOutputLevel(false);
        for (var tick = 0; tick < 1000; tick++) tape.Tick();
        tape.ExecuteCassette(EmulationCassetteCommand.Stop);
        var samples = tape.RecordedSignal.Samples.Span;
        Assert.True(samples[..transition].ToArray().All(sample => sample > 0));
        Assert.True(samples[transition..tape.Position].ToArray().All(sample => sample < 0));
        tape.Eject();
    }
}
