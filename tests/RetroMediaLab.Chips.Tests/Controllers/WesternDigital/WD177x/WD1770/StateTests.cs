using System.Text.Json;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Contracts;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.WesternDigital.WD177x.WD1770;

public sealed class StateTests
{
    [Theory]
    [InlineData(false, Wd1770Model.Wd1770)]
    [InlineData(true, Wd1770Model.Wd1770)]
    [InlineData(false, Wd1770Model.Wd1772)]
    [InlineData(true, Wd1770Model.Wd1772)]
    public void ReadResumesAfterReinjectingAnEarlierState(bool mfm, Wd1770Model model)
    {
        var reference = new PinFixture(mfm, model);
        var restored = new PinFixture(mfm, model);
        foreach (var rig in new[] { reference, restored })
        {
            rig.Command(0x88);
            rig.FeedId();
            rig.FeedBytes(Enumerable.Repeat((byte)(mfm ? 0x4e : 0xff), mfm ? 22 : 11));
            rig.Advance(3);
        }
        ReinjectAfterChangingTheController(reference.Chip, restored.Chip);
        var data = Enumerable.Range(0, 128).Select(value => (byte)(value ^ 0x96)).ToArray();
        foreach (var rig in new[] { reference, restored })
        {
            rig.FeedField(0xfb, data);
            rig.Advance(1024);
        }
        Assert.Equal(data, reference.Read);
        Assert.Equal(reference.Read, restored.Read);
        Assert.Equal(Serialize(reference.Chip), Serialize(restored.Chip));
    }

    [Theory]
    [InlineData(false, Wd1770Model.Wd1770)]
    [InlineData(true, Wd1770Model.Wd1770)]
    [InlineData(false, Wd1770Model.Wd1772)]
    [InlineData(true, Wd1770Model.Wd1772)]
    public void WriteResumesWithTheSamePulseTimesAndBytes(bool mfm, Wd1770Model model)
    {
        var reference = new PinFixture(mfm, model);
        var restored = new PinFixture(mfm, model);
        foreach (var rig in new[] { reference, restored })
        {
            foreach (var value in Enumerable.Range(0, 128)) rig.Write.Enqueue((byte)value);
            rig.Command(0xa8);
            rig.FeedId();
            for (var clock = 0; clock < 20_000 && !rig.Chip.WriteGate; clock++) rig.Tick();
            rig.Advance(17);
        }
        Assert.True(reference.Chip.WriteGate);
        ReinjectAfterChangingTheController(reference.Chip, restored.Chip);
        var referenceEdges = new List<(ulong, bool)>();
        var restoredEdges = new List<(ulong, bool)>();
        reference.Chip.WriteDataChanged += (time, level) => referenceEdges.Add((time, level));
        restored.Chip.WriteDataChanged += (time, level) => restoredEdges.Add((time, level));
        reference.Advance(90_000);
        restored.Advance(90_000);
        Assert.False(reference.Chip.Active);
        Assert.Equal(128L, reference.Chip.DataBytesWritten);
        Assert.Equal(referenceEdges, restoredEdges);
        Assert.Equal(reference.Written, restored.Written);
        Assert.Equal(Serialize(reference.Chip), Serialize(restored.Chip));
    }

    private static void ReinjectAfterChangingTheController(Wd1770 reference, Wd1770 restored)
    {
        var bytes = Serialize(reference);
        var snapshot = JsonSerializer.Deserialize<Wd1770State>(bytes)!;
        restored.Write(0, 0xd0);
        restored.Advance(37);
        var notifications = 0;
        restored.NmiChanged = _ => notifications++;
        restored.DriveSignalsChanged += () => notifications++;
        restored.WriteDataChanged += (_, _) => notifications++;
        restored.RestoreState(snapshot);
        Assert.Equal(0, notifications);
        Assert.Equal(bytes, Serialize(restored));
    }

    private static byte[] Serialize(Wd1770 chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());
}
