using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Common.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mc6809.Services;
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
    public void RamIoBitAddressesSelectBothPortsAndPreserveInputPins()
    {
        var port = new RamIo8154 { ReadPortA = () => 0xa5, ReadPortB = () => 0x5a };
        port.WriteIo(0x22, 0x0f);
        port.WriteIo(0x23, 0xf0);
        port.WriteIo(0x10, 0);
        port.WriteIo(0x1f, 0);
        Assert.Equal(0xa1, port.ReadIo(0x20));
        Assert.Equal(0x8a, port.ReadIo(0x21));
        Assert.Equal(0x80, port.ReadIo(0));
        Assert.Equal(0x80, port.ReadIo(0x0f));
        port.WriteIo(0, 0xff);
        port.WriteIo(0x0f, 0xff);
        Assert.Equal(0xa0, port.ReadIo(0x20));
        Assert.Equal(0x0a, port.ReadIo(0x21));
        port.WriteRam(0xff, 0x45);
        Assert.Equal(0x45, port.ReadRam(0x7f));
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
    public void Cpu6809PushPullUsesItsHardwareStackAndBigEndianWords()
    {
        var bus = new CpuBus([0x10, 0xce, 0x30, 0x00, 0x86, 0x12, 0xc6, 0x34, 0x34, 0x06, 0x4f, 0x5f, 0x35, 0x06]);
        var cpu = new Cpu6809(bus);
        cpu.Reset();
        for (var step = 0; step < 7; step++) cpu.Step();
        Assert.Equal(0x12, cpu.A);
        Assert.Equal(0x34, cpu.B);
        Assert.Equal(0x3000, cpu.S);
        Assert.Equal(0x12, bus.Memory[0x2ffe]);
        Assert.Equal(0x34, bus.Memory[0x2fff]);
    }

    [Fact]
    public void Cpu6809IndexedDoubleIncrementAndSixteenBitStoresUseCorrectAddresses()
    {
        var bus = new CpuBus([0x8e, 0x20, 0x00, 0xcc, 0x12, 0x34, 0xfd, 0x20, 0x00, 0xa6, 0x81]);
        var cpu = new Cpu6809(bus);
        cpu.Reset();
        for (var step = 0; step < 4; step++) cpu.Step();
        Assert.Equal(0x12, bus.Memory[0x2000]);
        Assert.Equal(0x34, bus.Memory[0x2001]);
        Assert.Equal(0x12, cpu.A);
        Assert.Equal(0x2002, cpu.X);
    }

    [Fact]
    public void Cpu6809FastInterruptRestoresOnlyItsPartialFrame()
    {
        var bus = new CpuBus([0x10, 0xce, 0x30, 0x00, 0x1c, 0xaf, 0x12]);
        bus.Memory[0xfff6] = 0x20;
        bus.Memory[0xfff7] = 0x00;
        bus.Memory[0x2000] = 0x86;
        bus.Memory[0x2001] = 0x55;
        bus.Memory[0x2002] = 0x3b;
        var active = false;
        var cpu = new Cpu6809(bus) { Firq = () => active };
        cpu.Reset();
        cpu.Step();
        cpu.Step();
        active = true;
        cpu.Step();
        Assert.Equal(0x2ffd, cpu.S);
        active = false;
        cpu.Step();
        cpu.Step();
        Assert.Equal(0x55, cpu.A);
        Assert.Equal(0x3000, cpu.S);
        Assert.Equal(0x1006, cpu.PC);
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

    private sealed class CpuBus : ICpuBus
    {
        internal readonly byte[] Memory = new byte[65536];
        internal CpuBus(byte[] program)
        {
            program.CopyTo(Memory, 0x1000);
            Memory[0xfffe] = 0x10;
        }
        public bool Irq => false;
        public bool Nmi => false;
        public byte Read(ushort address) => Memory[address];
        public void Write(ushort address, byte value) => Memory[address] = value;
        public void Tick() { }
    }
}
