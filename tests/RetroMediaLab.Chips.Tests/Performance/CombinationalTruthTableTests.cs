using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS139.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS139.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS32.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS32.Models;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;

namespace RetroMediaLab.Chips.Tests.Performance;

public sealed class CombinationalTruthTableTests
{
    [Fact]
    public void DecoderCoversEveryCanonicalUnknownInput()
    {
        var chip=new Sn74Ls139(Sn74Ls139Timing.CharacterizedMaximum);
        long time=0;
        for(int known=0;known<64;known++) for(int levels=0;levels<64;levels++)
        {
            if((levels & ~known)!=0) continue;
            chip.AdvanceTo(time);chip.SetInputs((byte)levels,(byte)known);time+=1000;chip.AdvanceTo(time);
            for(int output=0;output<8;output++)
            {
                int shift=(output/4)*3,k=(known>>shift)&7,l=(levels>>shift)&k;
                bool excluded=(l&4)!=0 || (((l^(output&3))&k&3)!=0);
                var expected=excluded?LogicPinState.High:k==7?LogicPinState.Low:LogicPinState.Unknown;
                Assert.Equal(expected,chip.GetOutput(output));
            }
        }
    }

    [Fact]
    public void OrCoversEveryCanonicalUnknownInput()
    {
        var chip=new Sn74Ls32(Sn74Ls32Timing.CharacterizedMaximum);long time=0;
        for(int known=0;known<256;known++)for(int levels=0;levels<256;levels++)
        {
            if((levels & ~known)!=0) continue;
            for(int gate=0;gate<4;gate++)
            {
                bool? a=(known&(1<<(gate*2)))==0?null:(levels&(1<<(gate*2)))!=0;
                bool? b=(known&(2<<(gate*2)))==0?null:(levels&(2<<(gate*2)))!=0;
                chip.AdvanceTo(time);chip.SetInputs(gate,a,b);time+=1000;chip.AdvanceTo(time);
                var expected=a==true||b==true?LogicPinState.High:a.HasValue&&b.HasValue?LogicPinState.Low:LogicPinState.Unknown;
                Assert.Equal(expected,chip.GetOutput(gate));
            }
        }
    }

    [Fact]
    public void PackedPhysicalProjectionPreservesAllControlDriveStates()
    {
        for(int encoding=0;encoding<81;encoding++)
        {
            int state=encoding;var pins=new bool?[4];byte levels=0,known=0;
            for(int bit=0;bit<4;bit++){int value=state%3;state/=3;pins[bit]=value==0?null:value==2;
                if(value!=0)known|=(byte)(1<<bit);if(value==2)levels|=(byte)(1<<bit);}
            var bus=new Z80BusPins(0x1234,0x56,pins[0],pins[1],pins[2],pins[3],true,true);
            Assert.Equal(levels,(byte)(bus.ControlLevels&15));Assert.Equal(known,bus.ControlDriven);
            Assert.True(bus.AddressDriven);Assert.True(bus.DataDriven);
            Assert.Equal((ushort)0x1234,bus.GetAddressOrZero());Assert.Equal((byte)0x56,bus.GetDataOrZero());
        }
        var floating=new Z80BusPins(null,null,null,null,null,null,true,true);
        Assert.False(floating.AddressDriven);Assert.False(floating.DataDriven);
    }
}
