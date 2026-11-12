using System.Text.Json;
using RetroMediaLab.Chips.Memory.Fujitsu.MB83xx.MB83256.Models;
using RetroMediaLab.Chips.Memory.NEC.UPD23xx.UPD23256A.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Services;

namespace RetroMediaLab.Chips.Tests.Memory.Fujitsu.MB83xx.MB83256;

public sealed class Mb83256Tests
{
    private static byte[] Contents() => Enumerable.Range(0,32768).Select(i => (byte)(i ^ (i >> 8))).ToArray();

    [Fact]
    public void EveryAddressTracksContinuouslyWithCeAndOeHeldLow()
    {
        byte[] mask = Contents(); var chip = new Tmm23256(mask,Mb83256Profile.Grade25);
        long time = 1000;
        for (int address = 0; address < 32768; address++,time += 400)
        {
            chip.ReceivePins(time,new((ushort)address,true,false,false));
            Assert.False(chip.Output.Known);
            chip.AdvanceTo(time + 249); Assert.False(chip.Output.Known);
            chip.AdvanceTo(time + 250); Assert.Equal(new(mask[address],true,true),chip.Output);
        }
        Assert.True(chip.IsTimingQualified);
    }

    [Fact]
    public void StaticAccessDiffersFromBothLatchedManufacturersAndFirstDisableKeepsItsDate()
    {
        byte[] mask = Contents(); var chip = new Tmm23256(mask,Mb83256Profile.Grade25);
        var toshiba = new Tmm23256(mask,Tmm23256Timing.SpecifiedMaximum);
        var nec = new Tmm23256(mask,Upd23256AProfile.ActiveLow);
        var pins = new Tmm23256Pins(0,true,false,true);
        toshiba.AdvanceTo(100000);
        foreach (var part in new[] { chip,toshiba,nec })
        {
            part.ReceivePins(101000,pins);
            part.ReceivePins(101050,pins with { Address = 1 });
            part.ReceivePins(101150,pins with { Address = 1,OeHigh = false });
        }
        chip.AdvanceTo(101299); Assert.False(chip.Output.Known);
        foreach (var part in new[] { chip,toshiba,nec }) part.AdvanceTo(101300);
        Assert.Equal(new(mask[1],true,true),chip.Output);
        Assert.Equal(new(mask[0],true,true),toshiba.Output);
        Assert.Equal(new(mask[0],true,true),nec.Output);
        pins = pins with { Address = 1 };
        chip.ReceivePins(101400,pins); chip.ReceivePins(101420,pins with { CeHigh = true });
        Assert.Equal(101480,chip.NextEventNanoseconds);
        chip.AdvanceTo(101479); Assert.True(chip.Output.Driven);
        chip.AdvanceTo(101480); Assert.Equal(default,chip.Output);
    }

    [Fact]
    public void SameByteReaddressingRearmsAccessAndUnknownAddressNeverCreatesKnownData()
    {
        byte[] mask = Contents(); mask[1] = mask[0];
        var chip = new Tmm23256(mask,Mb83256Profile.Grade25);
        var pins = new Tmm23256Pins(0,true,false,false);
        chip.ReceivePins(1000,pins); chip.AdvanceTo(1250);
        chip.ReceivePins(1500,pins with { Address = 1 });
        Assert.Equal(new(0,false,true),chip.Output);
        chip.ReceivePins(1600,pins); // same target byte, but a new address needs its full interval
        chip.AdvanceTo(1750); Assert.False(chip.Output.Known);
        chip.AdvanceTo(1850); Assert.Equal(new(mask[0],true,true),chip.Output);
        chip.ReceivePins(1900,pins with { AddressKnown = false });
        chip.AdvanceTo(2200); Assert.Equal(new(0,false,true),chip.Output);
        chip.ReceivePins(2300,pins); chip.AdvanceTo(2550);
        Assert.Equal(new(mask[0],true,true),chip.Output);
    }

    [Fact]
    public void AccessAndDisableSnapshotsResumeWithoutReplayingOutputsOrAcceptingBadStates()
    {
        foreach (bool disabling in new[] { false,true })
        {
            byte[] mask = Contents(); var chip = new Tmm23256(mask,Mb83256Profile.Grade25);
            var pins = new Tmm23256Pins(0,true,false,false);
            chip.ReceivePins(1000,pins); chip.AdvanceTo(1250);
            pins = pins with { Address = 8193 }; chip.ReceivePins(1300,pins);
            if (disabling) chip.ReceivePins(1650,pins with { OeHigh = true });
            chip.AdvanceTo(disabling ? 1700 : 1400);
            var state = JsonSerializer.Deserialize<Tmm23256State>(Snapshot(chip))!;
            var resumed = new Tmm23256(mask,Mb83256Profile.Grade25);
            var expected = new List<(long,Tmm23256Output)>(); var actual = new List<(long,Tmm23256Output)>();
            chip.OutputChanged = value => expected.Add((chip.TimeNanoseconds,value));
            resumed.OutputChanged = value => actual.Add((resumed.TimeNanoseconds,value));
            try
            {
                resumed.RestoreState(state); Assert.Empty(actual);
                foreach (var invalid in new[] { state with { Version = 2 },state with { RomHash = "different" },
                    state with { AddressChanged = state.Time + 1 },state with { InvalidateDue = state.Time },
                    state with { OutputDue = state.Time } })
                {
                    Assert.Throws<ArgumentException>(() => resumed.RestoreState(invalid));
                    Assert.Equal(Snapshot(chip),Snapshot(resumed));
                }
                Assert.Throws<ArgumentException>(() => new Tmm23256(mask,Upd23256AProfile.ActiveLow).RestoreState(state));
                chip.AdvanceTo(2000); resumed.AdvanceTo(2000);
                Assert.Equal(expected,actual); Assert.Equal(Snapshot(chip),Snapshot(resumed));
            }
            finally { chip.OutputChanged = null; resumed.OutputChanged = null; }
        }
    }
    private static byte[] Snapshot(Tmm23256 chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());
}
