using RetroMediaLab.Chips.Memory.Generic.I2C.EEPROM128.Services;
using RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Services;
using RetroMediaLab.Chips.Custom.Acorn.Master512.Refresh.Services;
using RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C12.Services;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Tube.Services;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Dictionaries;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Dictionaries;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using Storage = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuMasterTests
{
    [Theory]
    [InlineData(512, 9, 40)]
    [InlineData(512, 9, 80)]
    [InlineData(1024, 5, 80)]
    public void DoubleDensityDrivesAcceptMaster512DosGeometriesBut8271DrivesDoNot(int sectorSize, int sectors, int tracks)
    {
        using var image = new GeometryImage(sectorSize, sectors, tracks);
        Assert.True(FloppyDriveModels.Supports(Storage.Double80Dd, image));
        Assert.False(FloppyDriveModels.Supports(Storage.Double80, image));
    }

    [Theory]
    [InlineData("d95815c367e07f2d5a00e860092e7d6da795cbe5", EmulationFirmwareCompatibility.Official)]
    [InlineData("eb2a043d139ae0f3d512b4f1afa32286510c886c", EmulationFirmwareCompatibility.Compatible)]
    [InlineData("8acaf42a4a6b5f62f6be5e62df5fcffbf48890dd", EmulationFirmwareCompatibility.Compatible)]
    public void CoprocessorFirmwareKeepsOfficialAndCompatibleVariantsDistinct(string hash, EmulationFirmwareCompatibility expected)
        => Assert.Equal(expected, AcornBBCMicroModel.Get(MachineModel.BbcMaster128, hash, FirmwareCategory.Coprocessor));

    private sealed class GeometryImage(int sectorSize, int sectors, int tracks) : IEmulationDiskImage
    {
        public IEmulationDriveMechanics Mechanics => throw new NotSupportedException();
        public string FormatId => string.Empty;
        public int SectorSize => sectorSize;
        public long SectorCount => (long)tracks * Heads * sectors;
        public long Cylinders => tracks;
        public int Heads => 2;
        public int SectorsPerTrack => sectors;
        public bool IsReadOnly => true;
        public bool HasChanges => false;
        public IReadOnlyDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();
        public long? FindSector(int cylinder, int head, int number) => throw new NotSupportedException();
        public void UpdateMetadata(IReadOnlyDictionary<string, string> metadata) => throw new NotSupportedException();
        public ValueTask<EmulationDiskSector> ReadSectorAsync(long index, CancellationToken token = default) => throw new NotSupportedException();
        public void WriteSector(long index, ReadOnlySpan<byte> data) => throw new NotSupportedException();
        public Task FlushAsync(CancellationToken token = default) => Task.CompletedTask;
        public void Dispose() { }
    }

    [Fact]
    public void Master512MirrorsUpperRamAndTheBootstrapRomWithoutAddingMemory()
    {
        var rom = new byte[16384];
        rom[0x123] = 0x5a;
        var card = new Master512Board(new TubeUla(), rom);
        card.WriteMemory(0x70000, 0x12);
        Assert.Equal(0x12, card.ReadMemory(0xb0000));
        card.WriteMemory(0xb8000, 0x34);
        Assert.Equal(0x34, card.ReadMemory(0x78000));
        Assert.Equal(0, card.ReadMemory(0x38000));
        Assert.Equal(0x5a, card.ReadMemory(0xc0123));
        Assert.Equal(0x5a, card.ReadMemory(0xfc123));
        card.WriteMemory(0xc0123, 0);
        Assert.Equal(0x5a, card.ReadMemory(0xc0123));
    }

    [Fact]
    public void Master512DmaConsumesEachIncomingTubeByteOnlyOnce()
    {
        var tube = new TubeUla();
        var rom = new byte[16384];
        byte[] program = [
            0xba, 0xc0, 0xff, 0xb8, 0x8a, 0, 0xef, // Source : port Tube R3.
            0xba, 0xc4, 0xff, 0xb8, 0, 1, 0xef, // Destination 0100.
            0xba, 0xc8, 0xff, 0xb8, 2, 0, 0xef, // Deux octets.
            0xba, 0xca, 0xff, 0xb8, 0x46, 0xa2, 0xef, 0xf4 // MIO destination, INC, TC, SYN source, CHG/ST.
        ];
        InitializeMaster512Rom(rom, program);
        var card = new Master512Board(tube, rom);
        tube.HostWrite(0, 0x88);
        tube.HostWrite(5, 0x5a);
        for (var cycle = 0; cycle < 1000; cycle++) card.RunHostCycle();
        Assert.Equal(0x5a, card.ReadMemory(0x100));
        Assert.Equal(0, card.ReadMemory(0x101));
        Assert.False(tube.ParasiteNmi);
        tube.HostWrite(5, 0xa5);
        for (var cycle = 0; cycle < 1000; cycle++) card.RunHostCycle();
        Assert.Equal(0xa5, card.ReadMemory(0x101));
        Assert.Equal(0, card.ReadMemory(0x102));
    }

    [Theory]
    [InlineData(0x82)]
    [InlineData(0x92)]
    [InlineData(0xf2)]
    public void Master512TubeUsesOnlyLowByteLaneAndMirrorsItsRegistersWithinPcs1(int port)
    {
        var tube = new TubeUla();
        var rom = new byte[16384];
        byte[] program = [0xba, (byte)port, 0, 0xed, 0xa3, 0, 1, 0xf4];
        InitializeMaster512Rom(rom, program);
        tube.HostWrite(1, 0x5a);
        var card = new Master512Board(tube, rom);
        for (var cycle = 0; cycle < 1000; cycle++) card.RunHostCycle();
        Assert.Equal(0x5a, card.ReadMemory(0x100));
        Assert.Equal(0xff, card.ReadMemory(0x101));
    }

    [Fact]
    public void Master512TimersHaveHighInputsAndRefreshReturnsBusBeforeTimer1Nmi()
    {
        var rom = new byte[16384];
        byte[] program = [
            0xb8, 0, 2, 0xa3, 8, 0, // Vecteur NMI : F000:0200.
            0xb8, 0, 0xf0, 0xa3, 10, 0,
            0xba, 0x52, 0xff, 0xb8, 1, 0, 0xef, // Timer0 : demande de rafraîchissement.
            0xba, 0x56, 0xff, 0xb8, 0, 0xc0, 0xef,
            0xba, 0x5a, 0xff, 0xb8, 3, 0, 0xef, // Maximum A du timer 1.
            0xba, 0x5e, 0xff, 0xb8, 0, 0xc0, 0xef, 0xf4
        ];
        InitializeMaster512Rom(rom, program);
        new byte[] { 0xb0, 0x5a, 0xa2, 0, 1, 0xf4 }.CopyTo(rom, 0x200);
        var card = new Master512Board(new TubeUla(), rom);
        for (var cycle = 0; cycle < 1000; cycle++) card.RunHostCycle();
        Assert.Equal(0x5a, card.ReadMemory(0x100));
    }

    [Theory]
    [InlineData(false, 0xff)]
    [InlineData(true, 0x5a)]
    public void Master512TubeRespondsOnlyAfterItsPeripheralChipSelectHasBeenProgrammed(bool enabled, int expected)
    {
        var tube = new TubeUla();
        tube.HostWrite(1, 0x5a);
        var rom = new byte[16384];
        byte[] program = [0xba, 0x82, 0, 0xec, 0xa2, 0, 1, 0xf4];
        InitializeMaster512Rom(rom, program, enabled);
        var card = new Master512Board(tube, rom);
        for (var cycle = 0; cycle < 1000; cycle++) card.RunHostCycle();
        Assert.Equal(expected, card.ReadMemory(0x100));
    }

    private static void InitializeMaster512Rom(byte[] rom, byte[] program, bool enableTube = true)
    {
        // RESET ne sélectionne qu'un Kio à FFC00. Étendre UCS avant le saut vers F000:0000.
        byte[] reset = [0xba, 0xa0, 0xff, 0xb8, 4, 0xc0, 0xef, 0xea, 0, 0, 0, 0xf0];
        reset.CopyTo(rom, 0x3ff0);
        byte[] selects = [
            0xba, 0xa2, 0xff, 0xb8, 0xc4, 0x3f, 0xef, // LCS, limite basse RAM.
            0xba, 0xa4, 0xff, 0xb8, 4, 0, 0xef, // PCS : base I/O 0.
            0xba, 0xa8, 0xff, 0xb8, 0x84, 0, 0xef, // Sept sélections, PCS1 à 0080.
            0xea, 0x40, 0, 0, 0xf0
        ];
        if (enableTube) selects.CopyTo(rom, 0);
        else new byte[] { 0xea, 0x40, 0, 0, 0xf0 }.CopyTo(rom, 0);
        program.CopyTo(rom, 0x40);
    }

    [Fact]
    public void Intel80186PushSpStoresTheDecrementedStackPointer()
    {
        var memory = new byte[1048576];
        byte[] program = [0xbc, 0, 1, 0x54, 0x58, 0x9c, 0x5b, 0xf4];
        program.CopyTo(memory, 0xffff0);
        var cpu = new RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services.Cpu80186(a => memory[a], (a,v) => memory[a]=v, _ => 255, (_,_) => { });
        cpu.Reset(); for (var i=0;i<6;i++) cpu.Step();
        Assert.Equal(0x00fe, cpu.Register(0));
        Assert.Equal(0xf002, cpu.Register(3));
    }
    [Fact]
    public void MasterShadowDependsOnInstructionOriginAndAcccon()
    {
        var memory = new MasterMemory(); var main = new byte[32768];
        main[0x3000] = 0x11;
        memory.Select(0xfe34, 4); memory.Write(0x3000, 0x2000, 0x22, main);
        memory.Select(0xfe34, 2);
        Assert.Equal(0x11, memory.Read(0x3000, 0x2000, main));
        Assert.Equal(0x22, memory.Read(0x3000, 0xc000, main));
        memory.Select(0xfe34, 1);
        Assert.Equal(0x22, memory.ReadVideo(0x3000, main));
        Assert.Equal(0x11, memory.Read(0x3000, 0xc000, main));
        memory.Select(0xfe34, 4);
        Assert.Equal(0x11, memory.Read(0x3000, 0xc000, main));
        Assert.Equal(0x22, memory.Read(0x3000, 0x2000, main));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(8, false)]
    [InlineData(64, false)]
    [InlineData(72, false)]
    [InlineData(0, true)]
    [InlineData(8, true)]
    [InlineData(64, true)]
    [InlineData(72, true)]
    public void MasterRomDecodePreservesPrivateRamAndIoWindow(int accessControl, bool andy)
    {
        var mos = Enumerable.Range(0, 16384).Select(i => (byte)(i * 13 ^ 0x51)).ToArray();
        var basic = Enumerable.Range(0, 16384).Select(i => (byte)(i * 7 ^ 0xa3)).ToArray();
        var machine = new BbcMicroMachine(mos, basic, 32768, master: true);
        try
        {
            machine.Write(0xfe30, 0x8c);
            machine.Write(0x8000, 0x19); machine.Write(0x8fff, 0x29);
            machine.Write(0xfe34, 8);
            machine.Write(0xc000, 0x39); machine.Write(0xdfff, 0x49);
            var romSelect = (byte)(andy ? 0x8c : 12);
            machine.Write(0xfe30, romSelect);
            machine.Write(0xfe34, (byte)accessControl);
            foreach (ushort address in new ushort[] { 0x8000, 0x8fff, 0x9000, 0xbfff, 0xc000, 0xdfff, 0xe000, 0xfbff, 0xff00, 0xfffc, 0xffff })
            {
                byte expected = andy && address < 0x9000 ? (byte)(address == 0x8000 ? 0x19 : 0x29)
                    : (accessControl & 8) != 0 && address is >= 0xc000 and < 0xe000 ? (byte)(address == 0xc000 ? 0x39 : 0x49)
                    : address < 0xc000 ? basic[address & 0x3fff] : mos[address & 0x3fff];
                Assert.Equal(expected, machine.Read(address));
            }
            Assert.Equal((accessControl & 64) != 0 ? mos[0x3e30] : romSelect, machine.Read(0xfe30));
            Assert.Equal((accessControl & 64) != 0 ? mos[0x3e34] : (byte)accessControl, machine.Read(0xfe34));
        }
        finally
        {
            machine.SetInput(EmulationInputSnapshot.Empty);
            machine.NetworkFrameTransmitted = null; machine.NetworkPoll = null;
            machine.EjectCassette();
        }
    }
    [Fact]
    public void AccessControlPublishesOnlyItsActualInterruptTransitions()
    {
        var memory = new MasterMemory();
        var levels = new List<bool>();
        memory.IrqChanged = levels.Add;
        memory.Select(0xfe34, 0x80);
        memory.Select(0xfe34, 0x81);
        memory.Select(0xfe30, 4);
        Assert.Equal(new[] { true }, levels);
        memory.Reset(false);
        memory.Reset(false);
        Assert.Equal(new[] { true, false }, levels);
    }
    [Fact]
    public void AndyHazelAndMainMemoryAreIndependentAndResetPreservesWarmRam()
    {
        var memory = new MasterMemory(); var main = new byte[32768];
        memory.Select(0xfe30, 0x80); memory.Write(0x8000, 0x2000, 0x12, main);
        memory.Select(0xfe34, 8); memory.Write(0xc000, 0x2000, 0x34, main);
        memory.Reset(false);
        Assert.False(memory.Private(0x8000)); Assert.False(memory.Private(0xc000));
        memory.Select(0xfe30, 0x80); memory.Select(0xfe34, 8);
        Assert.Equal(0x12, memory.Read(0x8000, 0, main));
        Assert.Equal(0x34, memory.Read(0xc000, 0, main));
    }
    [Fact]
    public void CmosDataBusIsOnlyDrivenWhenSelectedAndPersistsItsConfiguration()
    {
        var clock = new MasterClock();
        clock.Address(0xc0, 32); clock.Address(0x40, 32);
        clock.Update(0, 0x5a); clock.Update(4, 0x5a); clock.Update(0, 0x5a);
        Assert.Equal(0x5a, clock.ReadRegister(32));
        clock.Update(2, 0); clock.Update(6, 0);
        Assert.Equal(0x5a, clock.ReadBus());
        clock.Address(0, 0); Assert.Equal(255, clock.ReadBus());
        var restored = new MasterClock(); restored.Import(clock.Export());
        Assert.Equal(0x5a, restored.ReadRegister(32));
    }
    [Fact]
    public void MasterClockPublishesDeadlineAndIrqWithoutSoftwarePollingItsRegisters()
    {
        long ticks = 0;
        var clock = new MasterClock(referenceClock: () => ticks);
        var interrupts = new List<bool>();
        var deadlines = new List<long?>();
        clock.IrqChanged = interrupts.Add;
        clock.NextClockEventChanged = deadlines.Add;
        clock.WriteRegister(10, 0x26);
        clock.WriteRegister(11, 0xc2);
        Assert.Equal(1954, clock.NextClockEvent);
        ticks = clock.NextClockEvent!.Value;
        clock.AdvanceTo(ticks);
        Assert.True(clock.Irq);
        Assert.Equal(0xc0, clock.ReadRegister(12));
        Assert.Equal(new[] { true, false }, interrupts);
        Assert.NotEmpty(deadlines);
        Assert.True(clock.NextClockEvent > ticks);
    }
    [Fact]
    public void MasterCpuExecutesCmosInstructionsAndKeepsSidewaysRamWritable()
    {
        var mos = new byte[16384];
        byte[] program = [0xa9, 0x42, 0x64, 0x20, 0x1a, 0x5a, 0x7a, 0x8d, 0x30, 0xfe, 0xa9, 4, 0x8d, 0x30, 0xfe, 0xa9, 0x66, 0x8d, 0, 0x80, 0x4c, 20, 0xc0];
        program.CopyTo(mos, 0); mos[^4] = 0; mos[^3] = 0xc0;
        var machine = new BbcMicroMachine(mos, new byte[16384], 32768, master: true);
        Assert.IsType<Cpu65C12>(machine.Cpu);
        Assert.Equal(2_000_000, machine.Cpu.ClockFrequency);
        for (var i = 0; i < 12; i++) machine.Cpu.Step();
        Assert.Equal(0, machine.Read(0x20)); Assert.Equal(0x66, machine.Read(0x8000));
    }

    [Fact]
    public void TurboDoesNotExposeAnInstructionWriteBeforeItsActualHostCycle()
    {
        var rom = new byte[2048];
        byte[] program = [0xa9, 0x5a, 0x8d, 0xf9, 0xfe, 0x4c, 5, 0xf0];
        program.CopyTo(rom, 0);
        rom[^4] = 0;
        rom[^3] = 0xf0;
        var tube = new TubeUla();
        var turbo = new TurboBoard(tube, rom);
        turbo.RunHostCycle(); // LDA immediate: two parasite cycles.
        Assert.Equal(0, tube.HostRead(0) & 0x80);
        turbo.RunHostCycle(); // STA opcode and low address, not its write cycle.
        Assert.Equal(0, tube.HostRead(0) & 0x80);
        turbo.RunHostCycle(); // STA high address and write, sixth parasite cycle.
        Assert.Equal(0x80, tube.HostRead(0) & 0x80);
        Assert.Equal(0x5a, tube.HostRead(1));
    }
}
