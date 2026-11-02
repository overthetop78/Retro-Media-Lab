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
    public void TubeTwoByteTransferKeepsItsRequestUntilTheFifoIsEmpty()
    {
        var tube = new TubeUla();
        tube.HostRead(5); // Octet initial du canal parasite vers hôte.
        tube.HostWrite(0, 0x98); // M et V : NMI et transfert de deux octets.
        tube.ParasiteWrite(5, 0x11);
        tube.ParasiteWrite(5, 0x22);
        Assert.False(tube.ParasiteNmi);
        Assert.Equal(0x11, tube.HostRead(5));
        Assert.False(tube.ParasiteNmi);
        Assert.Equal(0x22, tube.HostRead(5));
        Assert.True(tube.ParasiteNmi);
        tube.ParasiteWrite(5, 0x33);
        tube.ParasiteWrite(5, 0x44);
        tube.HostWrite(5, 0x55);
        Assert.False(tube.ParasiteNmi);
        tube.HostWrite(5, 0x66);
        Assert.True(tube.ParasiteNmi);
        Assert.Equal(0x55, tube.ParasiteRead(5));
        Assert.True(tube.ParasiteNmi);
        Assert.Equal(0x66, tube.ParasiteRead(5));
        Assert.False(tube.ParasiteNmi);
    }

    [Fact]
    public void Master512DmaConsumesEachIncomingTubeByteOnlyOnce()
    {
        var tube = new TubeUla();
        var rom = new byte[16384];
        byte[] program = [
            0xba, 0xc0, 0xff, 0xb0, 0x8a, 0xee, // Source : port Tube R3.
            0xba, 0xc4, 0xff, 0xb0, 0, 0xee, 0x42, 0xb0, 1, 0xee, // Destination 0100.
            0xba, 0xca, 0xff, 0xb0, 2, 0xee, 0xf4 // DMA activé, HLT.
        ];
        program.CopyTo(rom, 0);
        new byte[] { 0xea, 0, 0, 0, 0xf0 }.CopyTo(rom, 0x3ff0);
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

    [Fact]
    public void Intel80186PushSpStoresTheDecrementedStackPointer()
    {
        var memory = new byte[1048576];
        byte[] program = [0xbc, 0, 1, 0x54, 0x58, 0x9c, 0x5b, 0xf4];
        program.CopyTo(memory, 0xffff0);
        var cpu = new RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services.Cpu80186(a => memory[a], (a,v) => memory[a]=v, _ => 255, (_,_) => { });
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
        clock.Address(0xc0, 32); clock.Update(0, 0x5a); clock.Update(4, 0x5a); clock.Update(0, 0x5a);
        Assert.Equal(0x5a, clock.ReadRegister(32));
        clock.Update(2, 0); clock.Update(6, 0);
        Assert.Equal(0x5a, clock.ReadBus());
        clock.Address(0, 0); Assert.Equal(255, clock.ReadBus());
        var restored = new MasterClock(); restored.Import(clock.Export());
        Assert.Equal(0x5a, restored.ReadRegister(32));
    }
    [Fact]
    public void CompactEepromAcceptsI2cWritesAndRepeatedStartReads()
    {
        var chip = new CompactEeprom();
        void Pins(bool clock, bool data) => chip.WritePort((byte)((clock ? 0x20 : 0) | (data ? 0x10 : 0)));
        void Start() { Pins(false, true); Pins(true, true); Pins(true, false); Pins(false, false); }
        void Stop() { Pins(false, false); Pins(true, false); Pins(true, true); }
        void Send(byte value)
        {
            for (var bit = 7; bit >= 0; bit--) { var high = (value & (1 << bit)) != 0; Pins(false, high); Pins(true, high); }
            Pins(false, true); Pins(true, true); Assert.Equal(0, chip.ReadPort & 0x10); Pins(false, true);
        }
        Start(); Send(0xa0); Send(32); Send(0x5a); Stop();
        Assert.Equal(0x5a, chip.Export()[32]);
        Start(); Send(0xa0); Send(32); Start(); Send(0xa1);
        var value = 0;
        for (var bit = 0; bit < 8; bit++) { Pins(true, true); value = (value << 1) | ((chip.ReadPort & 0x10) != 0 ? 1 : 0); Pins(false, true); }
        Pins(true, true); Pins(false, true); Stop();
        Assert.Equal(0x5a, value);
    }
    [Fact]
    public void MasterCpuExecutesCmosInstructionsAndKeepsSidewaysRamWritable()
    {
        var mos = new byte[16384];
        byte[] program = [0xa9, 0x42, 0x64, 0x20, 0x1a, 0x5a, 0x7a, 0x8d, 0x30, 0xfe, 0xa9, 4, 0x8d, 0x30, 0xfe, 0xa9, 0x66, 0x8d, 0, 0x80, 0x4c, 20, 0xc0];
        program.CopyTo(mos, 0); mos[^4] = 0; mos[^3] = 0xc0;
        var machine = new BbcMicroMachine(mos, new byte[16384], 32768, master: true);
        for (var i = 0; i < 12; i++) machine.Cpu.Step();
        Assert.Equal(0, machine.Read(0x20)); Assert.Equal(0x66, machine.Read(0x8000));
    }
}
