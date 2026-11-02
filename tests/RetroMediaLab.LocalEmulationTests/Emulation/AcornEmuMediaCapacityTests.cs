using System.Reflection;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuMediaCapacityTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 2)]
    public void HostUsesFourFloppiesAndOnlyTheInstalledSt506Units(bool st506, int hardDisks)
    {
        using var core = new EmulatorCore(MachineModel.BbcMicroModelA);
        var board = new ArchimedesMachine(new byte[256], 4 << 20, st506: st506);
        Field("_machine").SetValue(core, board);
        var configuration = new MachineConfiguration(MachineModel.BbcMicroModelA, core: Emulator.AcornEmu);
        Invoke(core, "ConfigureDisks", configuration);
        Invoke(core, "ConfigureHardDisks", configuration);
        for (var drive = 0; drive < 4; drive++)
        {
            var slot = new EmulationMediaSlot(EmulationMediaCategory.FloppyDrive, drive);
            Assert.False(core.HasUnsavedMediaChanges(slot));
            core.SaveMediaChanges(slot);
            core.EjectMedia(slot);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => core.SaveMediaChanges(new(EmulationMediaCategory.FloppyDrive, 4)));
        Assert.Equal(new[] { 0, 1, 4, 5, 6 }.Concat(Enumerable.Range(8, hardDisks)).Order(), core.LedStates.Keys.Order());
        for (var unit = 0; unit < hardDisks; unit++)
        {
            var slot = new EmulationMediaSlot(EmulationMediaCategory.HardDisk, unit);
            core.SaveMediaChanges(slot);
            core.EjectMedia(slot);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => core.SaveMediaChanges(new(EmulationMediaCategory.HardDisk, hardDisks)));
        core.RunFrame();
        Assert.NotNull(core.LatestVideoFrame);
        core.Stop();
    }

    [Fact]
    public void HostDisposesOwnedImagesInAllFourFloppySlots()
    {
        var core = new EmulatorCore(MachineModel.BbcMicroModelA);
        var disks = new AcornTestDisk?[4];
        try
        {
            var board = new ArchimedesMachine(new byte[256], 4 << 20);
            Field("_machine").SetValue(core, board);
            Invoke(core, "ConfigureDisks", new MachineConfiguration(MachineModel.BbcMicroModelA, core: Emulator.AcornEmu));
            var owned = (Array)Field("_floppyImages").GetValue(core)!;
            for (var drive = 0; drive < disks.Length; drive++)
            {
                disks[drive] = new AcornTestDisk();
                owned.SetValue(disks[drive], drive);
                board.AttachFloppy(drive, disks[drive]);
            }
        }
        finally
        {
            try { core.Dispose(); }
            finally
            {
                foreach (var disk in disks)
                    if (disk is { Disposed: false }) disk.Dispose();
            }
        }
        Assert.All(disks, disk => Assert.True(disk!.Disposed));
    }

    private static FieldInfo Field(string name) => typeof(EmulatorCore).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static void Invoke(EmulatorCore core, string name, MachineConfiguration configuration) =>
        typeof(EmulatorCore).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(core, [configuration]);
}
