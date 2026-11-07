using RetroMediaLab.MediaEngine.Images.Writing.Encoding;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using ModelAMachine = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.BbcMicroMachine;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Enums;
using System.IO;
using NativeStorage = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions.StorageFunctions;

namespace RetroMediaLab.Tests.Emulation;

/// <summary>Exerce le protocole matériel sur des secteurs en mémoire, sans fichiers ni firmware externes.</summary>
public sealed class AcornEmuDiskControllerTests
{
    [Theory]
    [InlineData("ss40", 40, 1, true)]
    [InlineData("ss40", 41, 1, false)]
    [InlineData("ss40", 40, 2, false)]
    [InlineData("ds80", 80, 2, true)]
    [InlineData("ds80", 81, 2, false)]
    [InlineData("ds80-dd", 80, 2, true)]
    public void PhysicalImagesRespectDriveGeometryWithoutInventingASectorLayout(string model, int cylinders, int heads, bool accepted)
    {
        using var image = new MemoryDisk { Cylinders = cylinders, Heads = heads, SectorsPerTrack = 0, SectorBytes = 0, DeclaredSectorCount = 0 };
        if (accepted) NativeStorage.ValidateMedia(image, new(model, "100", false, false));
        else Assert.Throws<InvalidDataException>(() => NativeStorage.ValidateMedia(image, new(model, "100", false, false)));
    }

    [Theory]
    [InlineData("ss40", 40, 1, 10, true)]
    [InlineData("ss40", 80, 1, 10, false)]
    [InlineData("ss40", 40, 2, 10, false)]
    [InlineData("ss40", 40, 1, 16, false)]
    [InlineData("ds80", 80, 2, 10, true)]
    [InlineData("ds80", 80, 2, 16, false)]
    [InlineData("ds80-dd", 80, 2, 16, true)]
    [InlineData("ds80-dd", 40, 1, 10, true)]
    [InlineData("ss80-dd", 80, 2, 16, false)]
    [InlineData("ds40-dd", 80, 1, 16, false)]
    [InlineData("ds40-dd", 40, 2, 16, true)]
    [InlineData("ds80-dd", 0, 2, 16, false)]
    public void DriveRejectsUnsupportedFacesDensityOrGeometry(string model, int cylinders, int heads, int sectors, bool accepted)
    {
        using var image = new MemoryDisk { Cylinders = cylinders, Heads = heads, SectorsPerTrack = sectors };
        var settings = new FloppyDriveSettings(model, "100", false, false);
        if (accepted) NativeStorage.ValidateMedia(image, settings);
        else Assert.Throws<InvalidDataException>(() => NativeStorage.ValidateMedia(image, settings));
    }

    [Fact]
    public void DriveRejectsInconsistentImageCapacity()
    {
        using var image = new MemoryDisk { Cylinders = 40, Heads = 1, SectorsPerTrack = 10, DeclaredSectorCount = 399 };
        Assert.Throws<InvalidDataException>(() => NativeStorage.ValidateMedia(image, new("ss40", "100", false, false)));
    }

    [Theory]
    [InlineData("Tape", "Play")]
    [InlineData("Floppy", "Motor")]
    [InlineData("Floppy", "Read")]
    [InlineData("Floppy", "Write")]
    [InlineData("Floppy", "Seek")]
    [InlineData("Floppy", "Insert")]
    [InlineData("Floppy", "Eject")]
    [InlineData("HardDisk", "Motor")]
    [InlineData("HardDisk", "Read")]
    [InlineData("HardDisk", "Write")]
    [InlineData("HardDisk", "Seek")]
    [InlineData("Optical", "Motor")]
    [InlineData("Optical", "Read")]
    [InlineData("Optical", "Write")]
    [InlineData("Optical", "Seek")]
    [InlineData("Optical", "Insert")]
    [InlineData("Optical", "Eject")]
    public void SharedMechanicalSoundIsIndependentOfAudioChunkBoundaries(string device, string operation)
    {
        var service = RetroMediaLab.Emulation.EmulationSdk.MediaProcessing;
        var complete = service.RenderMechanicalSound(device, operation, 48000, 4800, 0);
        var first = service.RenderMechanicalSound(device, operation, 48000, 137, 0);
        var rest = service.RenderMechanicalSound(device, operation, 48000, 4663, 137);
        Assert.Contains(complete, sample => sample != 0);
        Assert.Equal(complete, first.Concat(rest).ToArray());
        if (operation is "Seek" or "Insert" or "Eject")
        {
            var duration = service.MechanicalSoundDurationSeconds(device, service.MechanicalSoundProfiles(device)[0], operation);
            Assert.All(service.RenderMechanicalSound(device, operation, 48000, 100, (long)Math.Ceiling(duration * 48000) + 1), sample => Assert.Equal(0, sample));
        }
    }

    [Theory]
    [InlineData("Floppy")]
    [InlineData("HardDisk")]
    [InlineData("Optical")]
    public void RecordedMechanicalBaseAndWrongDeviceProfileAreResolvedExplicitly(string device)
    {
        var service = RetroMediaLab.Emulation.EmulationSdk.MediaProcessing;
        var profiles = service.MechanicalSoundProfiles(device);
        Assert.NotEmpty(profiles);
        var selected = service.RenderMechanicalSound(device, "Motor", 48000, 4800, 0, profiles[0]);
        Assert.Equal(selected, service.RenderMechanicalSound(device, "Motor", 48000, 4800, 0));
        Assert.Equal(selected, service.RenderMechanicalSound(device, "Motor", 48000, 4800, 0, "unavailable-profile"));
        var otherDevice = device == "Floppy" ? RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants.HardDisk35 : RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants.Floppy35;
        Assert.Throws<ArgumentException>(() => service.RenderMechanicalSound(device, "Motor", 48000, 10, 0, otherDevice));
    }
    [Fact]
    public void DiskDriveConfigurationUsesTheExistingStorageContractAndRemainsOptional()
    {
        var factory = new AcornEmuMachineFactory();
        var configuration = new MachineConfiguration(MachineModel.BbcMicroModelA, core: Emulator.AcornEmu);
        var available = factory.DescribeStorage(configuration);
        Assert.Empty(available.ConfiguredSlots); Assert.Equal(2, available.AvailableDevices.Count(device => device.MediaType == EmulationMediaType.Floppy));
        var selected = factory.ApplyStorage(configuration, available with
        {
            ConfiguredSlots = [EmulationMediaSlot.Floppy0, EmulationMediaSlot.Floppy1],
            DeviceSettings = [new(EmulationMediaSlot.Floppy0, new("ss40", "400", true, true)), new(EmulationMediaSlot.Floppy1, new("ds80", "100", false, false))]
        });
        var restored = new MachineConfiguration(MachineModel.BbcMicroModelA, options: selected.Options, core: Emulator.AcornEmu);
        Assert.Equal(2, factory.DescribeStorage(restored).ConfiguredSlots.Count);
        var first = Assert.Single(factory.DescribeDeviceSettings(restored), item => item.Slot == EmulationMediaSlot.Floppy0).Floppy!;
        Assert.Equal("ss40", first.Model); Assert.Equal("400", first.Speed); Assert.False(first.WriteProtected); Assert.False(first.RedirectWrites);
        Assert.Contains(factory.GetFirmwareSlots(restored), slot => slot.FieldId == RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.FloppyExtensionConstants.IntelRom && slot.IsRequired);
        Assert.True(factory.RequiresOptionRestart(restored, "acornemu.floppy.0.model"));
        var removed = factory.ApplyStorage(restored, factory.DescribeStorage(restored) with { ConfiguredSlots = [], DeviceSettings = [] });
        Assert.Empty(factory.DescribeMediaDevices(removed)); Assert.DoesNotContain(factory.GetFirmwareSlots(removed), slot => slot.FieldId == RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.FloppyExtensionConstants.IntelRom && slot.IsRequired);
        Assert.Throws<ArgumentException>(() => factory.ApplyStorage(configuration, available with { ConfiguredSlots = [EmulationMediaSlot.Floppy0], DeviceSettings = [new(EmulationMediaSlot.Floppy0, new("invalid", "100", false, false))] }));
        var activity = factory.MediaActivity(restored, new Dictionary<int, bool> { [0] = true, [4] = true });
        Assert.True(activity[EmulationMediaSlot.Floppy0]); Assert.True(activity[EmulationMediaSlot.Floppy1]); Assert.False(activity[EmulationMediaSlot.Tape0]);
    }

    [Fact]
    public void LegacyProtectionStaysWithTheMountedImageRatherThanTheDrive()
    {
        var factory = new AcornEmuMachineFactory();
        var slot = EmulationMediaSlot.Floppy0;
        var configuration = new MachineConfiguration(MachineModel.BbcMicroModelA, core: Emulator.AcornEmu,
            media: [new("protected.ssd", MediaCategory.Floppy, slot)],
            options: new Dictionary<string,string> { ["acornemu.floppy.0.protected"] = bool.TrueString });
        var settings = factory.DescribeStorage(configuration);
        var updated = factory.ApplyStorage(configuration, settings);
        Assert.True(Assert.Single(updated.Media).IsReadOnly);
        Assert.False(Assert.Single(factory.DescribeDeviceSettings(updated)).Floppy!.WriteProtected);
        Assert.False(Assert.Single(factory.DescribeStorage(updated).AvailableDevices, device => device.Slot == slot).FloppyOptions!.CanConfigureWriteProtection);
        var replacement = factory.ApplyStorage(updated, factory.DescribeStorage(updated) with
        {
            MountedMedia = [new("other.ssd", slot, EmulationMediaType.Floppy, false, true)]
        });
        Assert.False(Assert.Single(replacement.Media).IsReadOnly);
        var protectedReplacement = factory.ApplyStorage(replacement, factory.DescribeStorage(replacement) with
        {
            MountedMedia = [new("other.ssd", slot, EmulationMediaType.Floppy, true, true)]
        });
        Assert.True(Assert.Single(protectedReplacement.Media).IsReadOnly);
    }

    [Fact]
    public void BoardPagesTheExternalRomAndRoutesControllerRequestsToCpuNmi()
    {
        var mos = new byte[16384]; mos[^6] = 0; mos[^5] = 3; mos[^4] = 0; mos[^3] = 2;
        var basic = new byte[16384]; basic[0] = 0x55; var dfs = new byte[16384]; dfs[0] = 0xa5;
        var board = new ModelAMachine(mos, basic, diskInterface: true, optionRoms: new Dictionary<int, byte[]> { [14] = dfs });
        using var disk = new MemoryDisk();
        try
        {
            Assert.Equal(0x55, board.Read(0x8000)); board.Write(0xfe30, 14); Assert.Equal(0xa5, board.Read(0x8000));
            board.Write(0x8000, 0); Assert.Equal(0xa5, board.Read(0x8000)); board.Write(0xfe30, 12); Assert.Equal(255, board.Read(0x8000));
            board.AttachFloppy(0, disk);
            // Le montage rend Ready haut ; Read Drive Status acquitte son état bas mémorisé avant insertion.
            board.Write(0xfe80, 0x6c); _ = board.Read(0xfe81);
            board.Write(0x200, 0xea); board.Write(0x201, 0x4c); board.Write(0x202, 0); board.Write(0x203, 2);
            board.Write(0xfe80, 0x75); foreach (var value in new byte[] { 0x0d, 0, 0, 0x10 }) board.Write(0xfe81, value);
            board.Write(0xfe80, 0x7a); board.Write(0xfe81, 0x17); board.Write(0xfe81, 0xc1);
            board.Write(0xfe80, 0x53); foreach (var value in new byte[] { 0, 0, 0x21 }) board.Write(0xfe81, value);
            // La recherche passe maintenant par ID/synchronisation/FM avant la première demande CPU.
            const int MaximumInstructionsPerRevolution = 200_000;
            for (var instruction = 0; instruction < MaximumInstructionsPerRevolution && board.Cpu.PC != 0x300; instruction++) board.Cpu.Step();
            Assert.Equal(0x300, board.Cpu.PC); Assert.True(board.Nmi); Assert.True(board.FloppyActive(0)); Assert.False(board.FloppyActive(1));
            Assert.Equal(0, board.Read(0xfe84)); Assert.False(board.Nmi);
            board.Reset(true); Assert.False(board.Nmi); Assert.Equal(0x55, board.Read(0x8000)); Assert.False(disk.Disposed);
        }
        finally { board.AttachFloppy(0, null); }
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void BbcCardWiresTheOptional8271OutputToTheHeadSelect(int drive, int head)
    {
        using var first = new MemoryDisk();
        using var second = new MemoryDisk(0x80);
        var card = new Disks(true);
        try
        {
            card.Attach(0, first);
            card.Attach(1, second);
            card.Write(0xfe80, 0x6c); _ = card.Read(0xfe81);
            card.Write(0xfe80, 0x35);
            foreach (var value in new byte[] { 0x0d, 1, 0, 0x10 }) card.Write(0xfe81, value);
            card.Write(0xfe80, 0x3a);
            card.Write(0xfe81, 0x17);
            card.Write(0xfe81, 0xc1);
            card.Write(0xfe80, 0x3a);
            card.Write(0xfe81, 0x23);
            card.Write(0xfe81, (byte)(head << 5));
            card.Write(0xfe80, (byte)(0x13 | (drive == 0 ? 0x40 : 0x80)));
            foreach (var value in new byte[] { 0, 0, 0x21 }) card.Write(0xfe81, value);
            for (var tick = 0; tick < 800_000 && !card.Nmi; tick++) card.Tick();
            Assert.True(card.Nmi);
            Assert.NotEqual(0, card.Read(0xfe80) & 4);
            Assert.Equal((byte)((drive == 0 ? 0 : 0x80) + head * 10), card.Read(0xfe84));
            Assert.False(card.Nmi);
            Assert.Equal(1, card.ReadBytes(drive));
        }
        finally
        {
            card.Reset();
            card.Attach(0, null);
            card.Attach(1, null);
        }
    }

    [Theory]
    [InlineData(1_000_000)]
    [InlineData(2_000_000)]
    [InlineData(3_000_000)]
    public void ModuleDriveWiringReadsWithEachExistingOwnerClock(int ownerClock)
    {
        using var disk = new MemoryDisk();
        var chip = new RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services.Intel8271(ownerClock);
        var wiring = new RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services.Intel8271Drive(chip,
            index => index == 0 ? disk : null, ownerClock);
        try
        {
            wiring.MediaChanged(0);
            void Command(byte command, params byte[] parameters)
            {
                chip.Write(0, command);
                foreach (var value in parameters) chip.Write(1, value);
                wiring.Synchronize();
            }
            Command(0x6c); _ = chip.Read(1);
            Command(0x35, 0x0d, 1, 0, 0x10);
            Command(0x3a, 0x17, 0xc1);
            Command(0x53, 0, 0, 0x21);
            var bytes = new List<byte>();
            for (var tick = 0; tick < ownerClock * 2 / 5 && (chip.Read(0) & 0x80) != 0; tick++)
            {
                wiring.Tick();
                if ((chip.Read(0) & 4) != 0) bytes.Add(chip.Read(4));
            }
            Assert.Equal(0x10, chip.Read(0) & 0x90);
            Assert.Equal(0, chip.Read(1));
            Assert.Equal(new byte[256], bytes);
            Assert.False(disk.Disposed);
        }
        finally { chip.Reset(); wiring.Reset(); }
    }

    private sealed class MemoryDisk(byte seed = 0) : IEmulationDiskImage
    {
        private readonly Dictionary<long, byte[]> _data = new();
        public IEmulationDriveMechanics Mechanics { get; } = RetroMediaLab.Emulation.EmulationSdk.MediaProcessing.CreateDriveMechanics(RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants.Floppy);
        public string FormatId => "acorn.dfs.ds";
        internal int SectorBytes { get; init; } = 256;
        public int SectorSize => SectorBytes;
        public long SectorCount => DeclaredSectorCount ?? Cylinders * Heads * SectorsPerTrack;
        internal long? DeclaredSectorCount { get; init; }
        public long Cylinders { get; init; } = 2;
        public int Heads { get; init; } = 2;
        public int SectorsPerTrack { get; init; } = 10;
        public bool IsReadOnly { get; init; }
        public bool HasChanges { get; private set; }
        public IReadOnlyDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();
        public void UpdateMetadata(IReadOnlyDictionary<string, string> metadata) => throw new NotSupportedException();
        internal long? BadSector { get; init; }
        internal bool Disposed { get; private set; }
        internal byte[] Data(long index) { if (!_data.TryGetValue(index, out var data)) _data[index] = data = Enumerable.Repeat((byte)(seed + index), SectorSize).ToArray(); return data; }
        public long? FindSector(int cylinder, int head, int number) => cylinder is >= 0 and < 2 && head is >= 0 and < 2 && number is >= 0 and < 10 ? (cylinder * 2L + head) * 10 + number : null;
        public ValueTask<EmulationDiskSector> ReadSectorAsync(long index, CancellationToken cancellationToken = default) => ValueTask.FromResult(new EmulationDiskSector(index, Data(index), (int)(index / 20), (int)(index / 10 % 2), (int)(index % 10), index != BadSector));
        public void WriteSector(long index, ReadOnlySpan<byte> data) { if (IsReadOnly) throw new UnauthorizedAccessException(); if (data.Length != SectorSize) throw new ArgumentException(nameof(data)); _data[index] = data.ToArray(); HasChanges = true; }
        public EmulationDiskTrack ReadTrack(int cylinder, int head, string encodingId, uint bitCellNanoseconds,
            uint indexTimeNanoseconds, IReadOnlyDictionary<string, int>? attributes = null)
        {
            var sectors = Enumerable.Range(0, SectorsPerTrack).Select(number =>
            {
                var index = FindSector(cylinder, head, number) ?? throw new ArgumentOutOfRangeException(nameof(cylinder));
                return new TrackSector(number, Data(index)) { DataIntegrityValid = index != BadSector };
            }).ToArray();
            var track = new FluxEncoderRegistry().Encode(encodingId,
                new(cylinder, head, sectors, attributes, bitCellNanoseconds, indexTimeNanoseconds));
            return new([new(indexTimeNanoseconds, track.Revolution.FluxIntervals)]);
        }
        public Task FlushAsync(CancellationToken cancellationToken = default) { HasChanges = false; return Task.CompletedTask; }
        public void Dispose() { Disposed = true; _data.Clear(); }
    }
}
