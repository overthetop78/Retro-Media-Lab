using System.Text.Json;
using RetroMediaLab.Chips.Memory.NEC.UPD23xx.UPD23256A.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Enums;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Services;

namespace RetroMediaLab.Chips.Tests.Memory.NEC.UPD23xx.UPD23256A;

public sealed class Upd23256ATests
{
    private static byte[] Contents() => Enumerable.Range(0,32768).Select(i => (byte)(i ^ (i >> 8))).ToArray();
    private static Tmm23256Timing Profile(Tmm23256OutputEnableMode mode) => mode switch
    {
        Tmm23256OutputEnableMode.ActiveLow => Upd23256AProfile.ActiveLow,
        Tmm23256OutputEnableMode.ActiveHigh => Upd23256AProfile.ActiveHigh,
        _ => Upd23256AProfile.AlwaysEnabled
    };

    [Theory]
    [InlineData(Tmm23256OutputEnableMode.ActiveLow)]
    [InlineData(Tmm23256OutputEnableMode.ActiveHigh)]
    [InlineData(Tmm23256OutputEnableMode.AlwaysEnabled)]
    public void EveryAddressAndMaskPolarityUsesNecAccessAndCeLatch(Tmm23256OutputEnableMode mode)
    {
        byte[] mask = Contents(); var chip = new Tmm23256(mask,Profile(mode));
        Assert.True(chip.IsInitialized);
        bool activeHigh = mode != Tmm23256OutputEnableMode.ActiveLow;
        long time = 1000;
        for (int address = 0; address < mask.Length; address++,time += 400)
        {
            var pins = new Tmm23256Pins((ushort)address,true,false,activeHigh);
            chip.ReceivePins(time,pins);
            chip.ReceivePins(time + 50,pins with { Address = (ushort)((address + 1) & 32767) });
            chip.AdvanceTo(time + 199); Assert.False(chip.Output.Known);
            chip.AdvanceTo(time + 200); Assert.Equal(new(mask[address],true,true),chip.Output);
            if (mode == Tmm23256OutputEnableMode.AlwaysEnabled)
            {
                chip.ReceivePins(time + 205,pins with { OeHigh = false });
                Assert.Equal(new(mask[address],true,true),chip.Output);
            }
            chip.ReceivePins(time + 220,pins with { CeHigh = true });
            chip.AdvanceTo(time + 290); Assert.Equal(default,chip.Output);
        }
        Assert.True(chip.IsTimingQualified);
        Assert.False(new Tmm23256(mask,Tmm23256Timing.SpecifiedMaximum).IsInitialized);
    }

    [Fact]
    public void OeDelayFirstDisableAndAllSpecifiedMinimumViolationsAreKept()
    {
        var mask = Contents(); var chip = new Tmm23256(mask,Upd23256AProfile.ActiveLow);
        var pins = new Tmm23256Pins(19,true,false,true);
        chip.ReceivePins(1000,pins);
        chip.ReceivePins(1200,pins with { OeHigh = false });
        chip.AdvanceTo(1299); Assert.False(chip.Output.Known);
        chip.AdvanceTo(1300); Assert.Equal(new(mask[19],true,true),chip.Output);
        chip.ReceivePins(1320,pins);
        chip.ReceivePins(1340,pins with { CeHigh = true });
        Assert.Equal(1390,chip.NextEventNanoseconds);
        chip.AdvanceTo(1389); Assert.True(chip.Output.Driven);
        chip.AdvanceTo(1390); Assert.Equal(default,chip.Output);
        Assert.True(chip.IsTimingQualified);

        var cycle = new Tmm23256(mask,Upd23256AProfile.ActiveLow);
        pins = pins with { OeHigh = false };
        cycle.ReceivePins(1000,pins); cycle.ReceivePins(1249,pins with { CeHigh = true });
        cycle.ReceivePins(1299,pins); Assert.False(cycle.IsTimingQualified); // cycle299, precharge99
        var off = new Tmm23256(mask,Upd23256AProfile.ActiveLow);
        off.ReceivePins(1000,pins); off.ReceivePins(1450,pins with { CeHigh = true });
        off.ReceivePins(1499,pins); Assert.False(off.IsTimingQualified); // CE high49
        var hold = new Tmm23256(mask,Upd23256AProfile.ActiveLow);
        hold.ReceivePins(1000,pins); hold.ReceivePins(1049,pins with { Address = 20 });
        hold.AdvanceTo(1200); Assert.False(hold.IsTimingQualified); Assert.False(hold.Output.Known);
    }

    [Fact]
    public void PendingAccessAndDisableResumeAtomicallyWithoutCrossManufacturerProfiles()
    {
        foreach (bool disabling in new[] { false,true })
        {
            byte[] mask = Contents(); var original = new Tmm23256(mask,Upd23256AProfile.ActiveHigh);
            var resumed = new Tmm23256(mask,Upd23256AProfile.ActiveHigh);
            var pins = new Tmm23256Pins(21,true,false,true);
            original.ReceivePins(1000,pins);
            if (disabling) original.ReceivePins(1300,pins with { CeHigh = true });
            original.AdvanceTo(disabling ? 1320 : 1100);
            var state = JsonSerializer.Deserialize<Tmm23256State>(Snapshot(original))!;
            var expected = new List<(long,Tmm23256Output)>(); var actual = new List<(long,Tmm23256Output)>();
            original.OutputChanged = value => expected.Add((original.TimeNanoseconds,value));
            resumed.OutputChanged = value => actual.Add((resumed.TimeNanoseconds,value));
            try
            {
                resumed.RestoreState(state); Assert.Empty(actual);
                foreach (var bad in new[] { state with { Version = 1 },state with { Version = 2 },state with { OutputDue = state.Time },
                    state with { RomHash = "different" },state with { LatchedAddress = 32768 } })
                {
                    Assert.Throws<ArgumentException>(() => resumed.RestoreState(bad));
                    Assert.Equal(Snapshot(original),Snapshot(resumed));
                }
                Assert.Throws<ArgumentException>(() => new Tmm23256(mask,Tmm23256Timing.SpecifiedMaximum).RestoreState(state));
                Assert.Throws<ArgumentException>(() => new Tmm23256(mask,Upd23256AProfile.ActiveLow).RestoreState(state));
                original.AdvanceTo(1500); resumed.AdvanceTo(1500);
                Assert.Equal(expected,actual); Assert.Equal(Snapshot(original),Snapshot(resumed));
            }
            finally { original.OutputChanged = null; resumed.OutputChanged = null; }
        }
    }
    private static byte[] Snapshot(Tmm23256 chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());
}
