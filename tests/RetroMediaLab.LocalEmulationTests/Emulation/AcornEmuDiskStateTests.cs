using System.Text.Json;
using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using IntelDisk = RetroMediaLab.Tests.Emulation.Intel8271.MemoryTrackDisk;
using WdDisk = RetroMediaLab.Tests.Emulation.Wd1770.MemoryTrackDisk;

namespace RetroMediaLab.LocalEmulationTests.Emulation;

public sealed class AcornEmuDiskStateTests
{
    [Fact]
    public void IntelDriveReturnsToTheSameRotationAndPartialRead()
    {
        using var referenceDisk = new IntelDisk(fill: 0xa5);
        using var loadedDisk = new IntelDisk(fill: 0xa5);
        var referenceChip = new Intel8271();
        var loadedChip = new Intel8271();
        var reference = new Intel8271Drive(referenceChip, index => index == 0 ? referenceDisk : null);
        var loaded = new Intel8271Drive(loadedChip, index => index == 0 ? loadedDisk : null);
        foreach (var pair in new[] { (reference, referenceChip), (loaded, loadedChip) })
        {
            pair.Item1.MediaChanged(0);
            Command(pair.Item2, 0x6c);
            pair.Item2.Read(1);
            Command(pair.Item2, 0x35, 0x0d, 0, 0, 0x10);
            Command(pair.Item2, 0x3a, 0x17, 0);
            Command(pair.Item2, 0x52, 0, 1);
            pair.Item1.Synchronize();
            Assert.Equal(17, ReadIntel(pair.Item1, pair.Item2, 17).Count);
        }
        var snapshot = RoundTrip(reference.CaptureState());
        loadedChip.Reset();
        loaded.Reset();
        loaded.RestoreState(snapshot);
        var expected = ReadIntel(reference, referenceChip, 128);
        var actual = ReadIntel(loaded, loadedChip, 128);
        Assert.Equal(111, expected.Count);
        Assert.Equal(expected, actual);
        Assert.Equal(Serialize(reference.CaptureState()), Serialize(loaded.CaptureState()));
    }

    [Fact]
    public void WdDriveReturnsToTheSameRotationAndPartialRead()
    {
        using var referenceDisk = new WdDisk(0x55);
        using var loadedDisk = new WdDisk(0x55);
        var reference = new Wd1770Drive(index => index == 0 ? referenceDisk : null);
        var loaded = new Wd1770Drive(index => index == 0 ? loadedDisk : null);
        foreach (var drive in new[] { reference, loaded })
        {
            drive.Write(2, 0);
            drive.Write(0, 0x88);
            Assert.Equal(17, ReadWd(drive, 17).Count);
        }
        var snapshot = RoundTrip(reference.CaptureState());
        loaded.Chip.Reset();
        for (var tick = 0; tick < 37; tick++) loaded.Tick();
        loaded.RestoreState(snapshot);
        var expected = ReadWd(reference, 256);
        var actual = ReadWd(loaded, 256);
        Assert.Equal(239, expected.Count);
        Assert.Equal(expected, actual);
        Assert.Equal(Serialize(reference.CaptureState()), Serialize(loaded.CaptureState()));
    }

    private static List<byte> ReadIntel(Intel8271Drive drive, Intel8271 chip, int maximum)
    {
        var data = new List<byte>();
        for (var tick = 0; tick < 2_000_000 && chip.Active && data.Count < maximum; tick++)
        {
            drive.Tick();
            if (chip.Drq) data.Add(chip.Read(4));
        }
        return data;
    }

    private static List<byte> ReadWd(Wd1770Drive drive, int maximum)
    {
        var data = new List<byte>();
        for (var tick = 0; tick < 2_000_000 && drive.Active && data.Count < maximum; tick++)
        {
            drive.Tick();
            if (drive.Drq) data.Add(drive.Read(3));
        }
        return data;
    }

    private static void Command(Intel8271 chip, byte command, params byte[] parameters)
    {
        chip.Write(0, command);
        foreach (var value in parameters) chip.Write(1, value);
    }

    private static byte[] Serialize<T>(T state) => JsonSerializer.SerializeToUtf8Bytes(state);
    private static T RoundTrip<T>(T state) => JsonSerializer.Deserialize<T>(Serialize(state))!;
}
