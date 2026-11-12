using System.Text.Json;
using RetroMediaLab.Chips.Memory.NEC.UPD40xx.UPD4016.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Services;

namespace RetroMediaLab.Chips.Tests.Memory.NEC.UPD40xx.UPD4016;

[Collection("Asynchronous SRAM measurements")]
public sealed class Upd4016Tests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public void EveryAddressWorksWithAllThreeNecGradesAndZeroAddressSetup()
    {
        foreach (var profile in new[] { Upd4016Profile.Grade1,Upd4016Profile.Grade2,Upd4016Profile.Grade3 })
        {
            var chip = new Tmm2016(profile);
            long time = 1000;
            for (int address = 0; address < 2048; address++)
            {
                byte data = (byte)(address ^ (address >> 8));
                var pins = new Tmm2016Pins((ushort)address,true,false,true,false,data,255);
                chip.ReceivePins(time,pins); // NEC permits tAS0
                chip.ReceivePins(time + 400,pins with { CsHigh = true,WeHigh = true });
                chip.ReceivePins(time + 600,pins with { WeHigh = true,OeHigh = false });
                chip.AdvanceTo(time + 600 + profile.Access);
                Assert.Equal(new(data,255,true),chip.Output);
                chip.ReceivePins(time + 900,pins with { CsHigh = true,WeHigh = true });
                chip.AdvanceTo(time + 1000); time += 1200;
            }
            Assert.True(chip.IsTimingQualified);
        }
    }

    [Fact]
    public void CsControlledWriteHasNecOnlyUnknownDriveWindowAndRestoresBothPendingEdges()
    {
        foreach (long offset in new long[] { 5,20 })
        {
            var nec = new Tmm2016(Upd4016Profile.Grade2);
            var toshiba = new Tmm2016(Tmm2016Timing.AP12);
            var pins = new Tmm2016Pins(17,true,true,false,false,0x69,255);
            nec.ReceivePins(0,pins); toshiba.ReceivePins(0,pins);
            nec.ReceivePins(100,pins with { CsHigh = false });
            toshiba.ReceivePins(100,pins with { CsHigh = false });
            nec.AdvanceTo(100 + offset);
            var state = JsonSerializer.Deserialize<Tmm2016State>(Snapshot(nec))!;
            var resumed = new Tmm2016(Upd4016Profile.Grade2);
            var expected = new List<(long,Tmm2016Output)>();
            var actual = new List<(long,Tmm2016Output)>();
            nec.OutputChanged = value => expected.Add((nec.TimeNanoseconds,value));
            resumed.OutputChanged = value => actual.Add((resumed.TimeNanoseconds,value));
            try
            {
                resumed.RestoreState(state); Assert.Empty(actual);
                if (offset == 5)
                {
                    nec.AdvanceTo(110); resumed.AdvanceTo(110);
                    Assert.Equal(new(0,0,true),nec.Output);
                }
                else Assert.Equal(new(0,0,true),nec.Output);
                nec.AdvanceTo(160); resumed.AdvanceTo(160); toshiba.AdvanceTo(160);
                Assert.Equal(default,nec.Output);
                Assert.Equal(default,toshiba.Output);
                Assert.Equal(expected,actual); Assert.Equal(Snapshot(nec),Snapshot(resumed));
                nec.ReceivePins(400,pins with { WeHigh = true });
                resumed.ReceivePins(400,pins with { WeHigh = true });
                Assert.Equal(0x69,nec.CaptureState().Cells[17]);
                Assert.Equal(Snapshot(nec),Snapshot(resumed));
                Assert.True(nec.IsTimingQualified);
            }
            finally { nec.OutputChanged = null; resumed.OutputChanged = null; }
        }
        var cancelled = new Tmm2016(Upd4016Profile.Grade2);
        var inactive = new Tmm2016Pins(0,true,true,false,false,0,255);
        cancelled.ReceivePins(0,inactive);
        cancelled.ReceivePins(100,inactive with { CsHigh = false });
        cancelled.ReceivePins(105,inactive); // remove CS before low-Z; no orphaned future enable
        cancelled.AdvanceTo(300);
        Assert.False(cancelled.Output.Driven);
        Assert.Null(cancelled.NextEventNanoseconds);
    }

    [Fact]
    public void NecChecksAddressToEndAndActiveOutputWriteRequirementWithoutChangingToshiba()
    {
        var nec = new Tmm2016(Upd4016Profile.Grade2);
        var pins = new Tmm2016Pins(0,true,false,true,false,0x55,255);
        nec.ReceivePins(100,pins);
        nec.ReceivePins(270,pins with { Address = 1,WeHigh = true }); // write ends with valid old address, tWR10 violated
        Assert.False(nec.IsTimingQualified);
        Assert.Equal(0,nec.CaptureState().Known[0]);
        var active = new Tmm2016(Upd4016Profile.Grade2);
        pins = new(0,true,false,false,true,0x51,255);
        active.ReceivePins(100,pins);
        active.AdvanceTo(300);
        active.ReceivePins(400,pins with { WeHigh = false });
        active.ReceivePins(500,pins); // tWP100 meets basic table, but note5 requires60+60=120
        Assert.False(active.IsTimingQualified);
        Assert.Equal(0,active.CaptureState().Known[0]);
        var addressEnd = new Tmm2016(Upd4016Profile.Grade2);
        pins = new(0,true,false,true,true,0x55,255);
        addressEnd.ReceivePins(100,pins);
        addressEnd.ReceivePins(500,pins with { Address = 7,WeHigh = false });
        addressEnd.ReceivePins(610,pins with { Address = 7 }); // tCW510 and tWP110 met, tAW120 not met
        Assert.False(addressEnd.IsTimingQualified);
        Assert.False(Tmm2016Timing.AP12.SelectionDuringWriteCanDrive);
        Assert.Equal(20,Tmm2016Timing.AP12.AddressSetup);
        Assert.Equal(0,Upd4016Profile.Grade2.AddressSetup);
    }
    [Fact]
    public void CompleteReadBusThroughputCoversAllNecGrades()
    {
        foreach (var profile in new[] { Upd4016Profile.Grade1,Upd4016Profile.Grade2,Upd4016Profile.Grade3 })
            RetroMediaLab.Chips.Tests.Memory.Common.AsynchronousSramThroughput.Measure(profile,output);
    }

    private static byte[] Snapshot(Tmm2016 chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());
}
