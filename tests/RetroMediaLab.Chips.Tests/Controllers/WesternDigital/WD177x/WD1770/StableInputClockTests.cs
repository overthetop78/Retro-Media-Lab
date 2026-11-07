using System.Text.Json;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.WesternDigital.WD177x.WD1770;

/// <summary>Les états existants servent uniquement de vue complète du silicium, sans fichier de sauvegarde.</summary>
public sealed class StableInputClockTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void BoundedClockAdvanceRetainsTimersResetIndexAndSignalTimes(bool wd1772, bool mfm)
    {
        var model = wd1772 ? Wd1770Model.Wd1772 : Wd1770Model.Wd1770;
        var grouped = new Wd1770(model) { DoubleDensity = mfm };
        var unit = new Wd1770(model) { DoubleDensity = mfm };
        var groupedSignals = new List<(ulong Clock, string Signal)>();
        var unitSignals = new List<(ulong Clock, string Signal)>();
        Action Wire(Wd1770 chip, List<(ulong, string)> signals)
        {
            var track = 0;
            var step = false;
            chip.SetTrackZero(true);
            chip.NmiChanged = level => signals.Add((chip.HalfClocks, "NMI " + level));
            Action handler = () =>
            {
                signals.Add((chip.HalfClocks, $"DRIVE {chip.Motor} {chip.Step} {chip.Direction} {chip.WriteGate} {chip.Active}"));
                if (chip.Step && !step) track = Math.Clamp(track + (chip.Direction ? 1 : -1), 0, 255);
                step = chip.Step;
                chip.SetTrackZero(track == 0);
            };
            chip.DriveSignalsChanged += handler;
            return handler;
        }
        var groupedDrive = Wire(grouped, groupedSignals);
        var unitDrive = Wire(unit, unitSignals);
        void Same() => Assert.Equal(JsonSerializer.Serialize(unit.CaptureState()), JsonSerializer.Serialize(grouped.CaptureState()));
        void Advance(int clocks)
        {
            if (clocks == 0) { Assert.Equal(0, grouped.AdvanceWithStableInputs(0)); Same(); return; }
            while (clocks > 0)
            {
                var consumed = grouped.AdvanceWithStableInputs(clocks);
                Assert.InRange(consumed, 0, clocks);
                if (consumed == 0) { grouped.Tick(); consumed = 1; }
                for (var tick = 0; tick < consumed; tick++) unit.Tick();
                Same();
                Assert.Equal(unitSignals, groupedSignals);
                clocks -= consumed;
            }
        }
        void Write(int register, byte value) { grouped.Write(register, value); unit.Write(register, value); Same(); }
        try
        {
            foreach (var count in new[] { 0, 1, 3, 97 }) Advance(count);
            Write(3, 2); Write(0, 0x18);
            foreach (var count in new[] { 1, 13, 99_999, 2, 97_001 }) Advance(count);
            Write(2, 1); Write(0, 0x80);
            for (var index = 0; index < 12; index++)
            {
                grouped.SetIndex(true); unit.SetIndex(true); Advance(31);
                grouped.SetIndex(false); unit.SetIndex(false); Advance(111);
            }
            Write(0, 0xd4);
            grouped.SetIndex(true); unit.SetIndex(true); Advance(2);
            grouped.SetIndex(false); unit.SetIndex(false); Advance(777);
            Write(0, 0xf8); Write(3, 0xf5); Advance(2048);
            grouped.SetIndex(true); unit.SetIndex(true); Advance(64);
            Write(0, 0xd0); Advance(11);
            grouped.SetReset(true); unit.SetReset(true); Advance(1003);
            grouped.SetReset(false); unit.SetReset(false); Advance(7);
        }
        finally
        {
            grouped.NmiChanged = null; unit.NmiChanged = null;
            grouped.DriveSignalsChanged -= groupedDrive;
            unit.DriveSignalsChanged -= unitDrive;
            // Aucun propriétaire externe ni ressource native n'est créé par ce banc.
        }
    }
}
