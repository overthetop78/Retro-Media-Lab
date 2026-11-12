using System.Text.Json;
using RetroMediaLab.Chips.Memory.Fujitsu.MB84xx.MB8464.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Services;

namespace RetroMediaLab.Chips.Tests.Memory.Fujitsu.MB84xx.MB8464;

[Collection("Asynchronous SRAM measurements")]
public sealed class Mb8464Tests
{
    private static readonly Tmm2016Timing[] Profiles = [Mb8464Profile.Grade12,Mb8464Profile.Grade12L,
        Mb8464Profile.Grade12LL,Mb8464Profile.Grade15,Mb8464Profile.Grade15L,Mb8464Profile.Grade15LL];

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public void EveryAddressInEveryGradeUsesAllThreeWriteTerminations(int index)
    {
        var profile = Profiles[index]; var chip = new Tmm2016(profile);
        long time = 1000;
        for (int address = 0; address < 8192; address++,time += 1000)
        {
            byte data = (byte)(address ^ (address >> 8));
            var pins = new Tmm2016Pins((ushort)address,true,false,true,false,data,255,true);
            chip.ReceivePins(time,pins);
            chip.ReceivePins(time + 300,(address % 3) switch
            {
                0 => pins with { WeHigh = true },
                1 => pins with { CsHigh = true },
                _ => pins with { Cs2High = false }
            });
            chip.ReceivePins(time + 350,pins with { CsHigh = true,Cs2High = false,WeHigh = true });
            chip.ReceivePins(time + 500,pins with { OeHigh = false,WeHigh = true });
            chip.AdvanceTo(time + 500 + profile.Access);
            Assert.Equal(new(data,255,true),chip.Output);
            chip.ReceivePins(time + 700,pins with { OeHigh = false,WeHigh = true,Cs2High = false });
            chip.AdvanceTo(time + 800); Assert.Equal(default,chip.Output);
        }
        Assert.True(chip.IsTimingQualified);
        var state = chip.CaptureState(); Assert.Equal(8192,state.Cells.Length);
        for (int address = 0; address < 8192; address++)
        {
            Assert.Equal((byte)(address ^ (address >> 8)),state.Cells[address]);
            Assert.Equal(255,state.Known[address]);
        }
    }

    [Fact]
    public void SecondChipSelectHasItsOwnSelectionEdgeAndStateContinuation()
    {
        var chip = Stored();
        var pins = new Tmm2016Pins(8191,true,false,false,true,0,0,false);
        chip.ReceivePins(2000,pins); Assert.Equal(default,chip.Output);
        chip.ReceivePins(2100,pins with { Cs2High = true });
        chip.AdvanceTo(2115); Assert.Equal(new(0,0,true),chip.Output);
        var state = JsonSerializer.Deserialize<Tmm2016State>(Snapshot(chip))!;
        var resumed = new Tmm2016(Mb8464Profile.Grade15L);
        var actual = new List<(long,Tmm2016Output)>(); var expected = new List<(long,Tmm2016Output)>();
        resumed.OutputChanged = value => actual.Add((resumed.TimeNanoseconds,value));
        chip.OutputChanged = value => expected.Add((chip.TimeNanoseconds,value));
        try
        {
            resumed.RestoreState(state); Assert.Empty(actual);
            foreach (var bad in new[] { state with { Version = 2 },state with { Cells = new byte[2048] },
                state with { SupplyMillivolts = -1 },state with { SupplyRecoveryEnd = -1 } })
            {
                Assert.Throws<ArgumentException>(() => resumed.RestoreState(bad));
                Assert.Equal(Snapshot(chip),Snapshot(resumed));
            }
            Assert.Throws<ArgumentException>(() => new Tmm2016(Tmm2016Timing.AP15).RestoreState(state));
            chip.AdvanceTo(2250); resumed.AdvanceTo(2250);
            Assert.Equal(new(0x5a,255,true),chip.Output);
            chip.ReceivePins(2300,pins); resumed.ReceivePins(2300,pins);
            chip.AdvanceTo(2350); resumed.AdvanceTo(2350);
            Assert.Equal(default,chip.Output); Assert.Equal(expected,actual);
            Assert.Equal(Snapshot(chip),Snapshot(resumed));
        }
        finally { chip.OutputChanged = null; resumed.OutputChanged = null; }
    }

    [Fact]
    public void AddressToEndAndDataSetupMinimaQualifyActualWriteData()
    {
        var data = new Tmm2016(Mb8464Profile.Grade15L);
        var pins = new Tmm2016Pins(8191,true,false,true,false,0xa5,0x0f,true);
        data.ReceivePins(1000,pins);
        data.ReceivePins(1300,pins with { WeHigh = true });
        Assert.Equal(5,data.CaptureState().Cells[8191]); Assert.Equal(0x0f,data.CaptureState().Known[8191]);
        var badData = new Tmm2016(Mb8464Profile.Grade15L);
        badData.ReceivePins(1000,pins);
        badData.ReceivePins(1251,pins with { Data = 0xa6 });
        badData.ReceivePins(1300,pins with { Data = 0xa6,WeHigh = true }); // tDS49, requires50
        Assert.False(badData.IsTimingQualified); Assert.Equal(0,badData.CaptureState().Known[8191]);
        var addressEnd = new Tmm2016(Mb8464Profile.Grade15L);
        pins = pins with { Address = 0,WeHigh = true };
        addressEnd.ReceivePins(100,pins);
        addressEnd.ReceivePins(500,pins with { Address = 8191,WeHigh = false });
        addressEnd.ReceivePins(599,pins with { Address = 8191 }); // tWP99>=90,tCW499>=100,tAW99<100
        Assert.False(addressEnd.IsTimingQualified); Assert.Equal(0,addressEnd.CaptureState().Known[8191]);
    }

    [Fact]
    public void RetentionAtTwoVoltsAndRecoveryResumeButInvalidConditionsLoseKnowledge()
    {
        var chip = Stored();
        chip.ReceiveSupplyMillivolts(2000,2000);
        Assert.Equal(255,chip.CaptureState().Known[8191]);
        var lowVoltage = new Tmm2016(Mb8464Profile.Grade15L);
        lowVoltage.RestoreState(JsonSerializer.Deserialize<Tmm2016State>(Snapshot(chip))!);
        chip.ReceiveSupplyMillivolts(4000,5000);
        lowVoltage.ReceiveSupplyMillivolts(4000,5000);
        var state = JsonSerializer.Deserialize<Tmm2016State>(Snapshot(chip))!;
        Assert.Equal(4150,state.SupplyRecoveryEnd);
        var resumed = new Tmm2016(Mb8464Profile.Grade15L); resumed.RestoreState(state);
        var pins = new Tmm2016Pins(8191,true,false,false,true,0,0,true);
        chip.ReceivePins(4150,pins); resumed.ReceivePins(4150,pins);
        lowVoltage.ReceivePins(4150,pins);
        chip.AdvanceTo(4300); resumed.AdvanceTo(4300);
        lowVoltage.AdvanceTo(4300);
        Assert.Equal(new(0x5a,255,true),chip.Output);
        Assert.Equal(Snapshot(chip),Snapshot(resumed)); Assert.True(chip.IsTimingQualified);
        Assert.Equal(Snapshot(chip),Snapshot(lowVoltage));
        chip.ReceiveSupplyMillivolts(5000,1999);
        Assert.All(chip.CaptureState().Known,value => Assert.Equal(0,value));
        Assert.Equal(0,chip.Output.Known); Assert.False(chip.IsTimingQualified);

        var early = Stored(); early.ReceiveSupplyMillivolts(2000,2500);
        early.ReceiveSupplyMillivolts(4000,5000); early.ReceivePins(4149,pins);
        Assert.False(early.IsTimingQualified); Assert.Equal(0,early.CaptureState().Known[8191]);
        var selected = Stored(); selected.ReceiveSupplyMillivolts(2000,2500);
        selected.ReceivePins(2100,pins);
        Assert.False(selected.IsTimingQualified); Assert.Equal(0,selected.CaptureState().Known[8191]);
        var byCs2 = Stored(); byCs2.ReceivePins(1600,pins with { Cs2High = false });
        byCs2.ReceiveSupplyMillivolts(2000,2000);
        Assert.True(byCs2.IsTimingQualified); Assert.Equal(255,byCs2.CaptureState().Known[8191]);
        Assert.Throws<NotSupportedException>(() => new Tmm2016(Tmm2016Timing.AP15).ReceiveSupplyMillivolts(0,2000));
    }

    private static Tmm2016 Stored()
    {
        var chip = new Tmm2016(Mb8464Profile.Grade15L);
        var pins = new Tmm2016Pins(8191,true,false,true,false,0x5a,255,true);
        chip.ReceivePins(1000,pins); chip.ReceivePins(1300,pins with { WeHigh = true });
        chip.ReceivePins(1400,pins with { WeHigh = true,CsHigh = true }); chip.AdvanceTo(1500);
        return chip;
    }
    private static byte[] Snapshot(Tmm2016 chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());
}
