using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using RetroMediaLab.Tests.Emulation.Wd1770;

namespace RetroMediaLab.Tests.Emulation;

/// <summary>Compare le regroupement au même silicium recevant au plus une clock par appel.</summary>
public sealed class Wd1770IdleRotationTests
{
    [Theory]
    [InlineData(false, 2_000_000)]
    [InlineData(true, 2_000_000)]
    [InlineData(false, 1_000_000)]
    [InlineData(true, 1_000_000)]
    [InlineData(false, 4_000_000)]
    [InlineData(true, 4_000_000)]
    public void IdleRotationPreservesIndexInterruptsMotorTimeoutAndTheNextRead(bool wd1772, int ownerClock)
    {
        using var first = new MemoryTrackDisk(71);
        using var second = new MemoryTrackDisk(71);
        var model = wd1772 ? Wd1770Model.Wd1772 : Wd1770Model.Wd1770;
        var grouped = new Wd1770Drive(_ => first, ticksPerSecond: ownerClock, model: model);
        var unit = new Wd1770Drive(_ => second, ticksPerSecond: 16_000_000, model: model);
        var groupedEdges = new List<(ulong Clock, bool Level)>();
        var unitEdges = new List<(ulong Clock, bool Level)>();
        grouped.Chip.NmiChanged = level => groupedEdges.Add((grouped.Chip.HalfClocks, level));
        unit.Chip.NmiChanged = level => unitEdges.Add((unit.Chip.HalfClocks, level));
        void Step()
        {
            grouped.Tick();
            unit.Advance(16_000_000 / ownerClock);
            if (grouped.Drq != unit.Drq) Assert.Fail("DRQ diverged at the owner clock boundary.");
            if (grouped.Drq) Assert.Equal(grouped.Read(3), unit.Read(3));
        }
        void Verify()
        {
            Assert.Equal(grouped.Chip.HalfClocks, unit.Chip.HalfClocks);
            Assert.Equal(grouped.ReadBytes(0), unit.ReadBytes(0));
            Assert.Equal(grouped.Motor, unit.Motor);
            Assert.Equal(grouped.Read(0), unit.Read(0));
            Assert.Equal(groupedEdges, unitEdges);
        }
        void Advance(int periods)
        {
            for (var period = 0; period < periods; period++) Step();
            Verify();
        }
        void Read()
        {
            grouped.Write(2, 0); unit.Write(2, 0);
            grouped.Write(0, 0x88); unit.Write(0, 0x88);
            var periods = 0;
            while (grouped.Active && periods++ < ownerClock * 2) Step();
            Verify();
            Assert.False(grouped.Active);
            Assert.False(unit.Active);
        }
        try
        {
            Read();
            Assert.Equal(256, grouped.ReadBytes(0));
            grouped.Write(0, 0xd4); unit.Write(0, 0xd4);
            // Plusieurs index et la temporisation réelle d'arrêt moteur ; puis
            // redémarrer une lecture à partir des mêmes phases conservées.
            for (var interval = 0; interval < 45; interval++) Advance(ownerClock / 20 + 3);
            Assert.False(grouped.Motor);
            Read();
            Assert.Equal(512, grouped.ReadBytes(0));
        }
        finally
        {
            grouped.Chip.NmiChanged = null;
            unit.Chip.NmiChanged = null;
            grouped.Reset(); unit.Reset();
        }
    }
}
