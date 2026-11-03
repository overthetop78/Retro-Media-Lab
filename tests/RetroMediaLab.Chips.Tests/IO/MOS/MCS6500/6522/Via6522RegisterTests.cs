using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6522;

public sealed class Via6522RegisterTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(0x10)]
    [InlineData(0x100)]
    [InlineData(-16)]
    public void EveryRegisterKeepsItsDefinedReadWriteRoleThroughAddressMirrors(int mirror)
    {
        const byte written = 0xa5;
        for (var register = 0; register < C.RegisterCount; register++)
        {
            var via = new Via6522 { ReadPortA = () => 0x6c, ReadPortB = () => 0x3c };
            via.Write(C.Timer1Low, 0x34);
            via.Write(C.Timer1High, 0x12);
            via.Write(C.Timer2Low, 0x78);
            via.Write(C.Timer2High, 0x56);
            via.Write(mirror + register, written);
            var expected = register switch
            {
                C.PortA or C.PortANoHandshake => 0x6c,
                C.PortB => 0x3c,
                C.Timer1Low => 0x34, // A low-byte write updates the latch, not the counter.
                C.Timer2Low => 0x78,
                C.Flags => 0,
                _ => written
            };
            Assert.Equal(expected, via.Read(mirror + register));
        }
    }

    [Fact]
    public void Timer1LatchAddressesDoNotReloadTheRunningCounter()
    {
        var via = new Via6522();
        via.Write(C.Timer1Low, 0x34);
        via.Write(C.Timer1High, 0x12);
        via.Write(C.Latch1Low, 0xcd);
        via.Write(C.Latch1High, 0xab);
        Assert.Equal(0x34, via.Read(C.Timer1Low));
        Assert.Equal(0x12, via.Read(C.Timer1High));
        Assert.Equal(0xcd, via.Read(C.Latch1Low));
        Assert.Equal(0xab, via.Read(C.Latch1High));
        via.Write(C.Timer1High, 0xef);
        Assert.Equal(0xcd, via.Read(C.Timer1Low));
        Assert.Equal(0xef, via.Read(C.Timer1High));
    }
}
