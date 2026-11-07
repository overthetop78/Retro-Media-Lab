using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class RamIntegrationTests
{
    private sealed class Ram : IElectronRamPins
    {
        internal readonly byte[] Memory = new byte[32768];
        internal readonly List<(int Address, bool Odd, bool Write, byte Value)> Accesses = [];
        internal Action<int, bool>? Accessed;
        private byte _row;
        private bool _ras = true;
        private bool _cas = true;
        public byte Data { get; private set; }

        public void Drive(byte address, bool rowStrobeHigh, bool columnStrobeHigh, bool writeEnableHigh, byte data)
        {
            if (_ras && !rowStrobeHigh) _row = address;
            if (_cas && !columnStrobeHigh && !rowStrobeHigh)
            {
                int[] rowBits = [3, 4, 5, 6, 7, 2, 1, 8];
                int[] colBits = [9, 10, 11, 12, 13, 14, 0];
                var logical = 0;
                for (var pin = 0; pin < rowBits.Length; pin++) logical |= ((_row >> pin) & 1) << rowBits[pin];
                for (var pin = 0; pin < colBits.Length; pin++) logical |= ((address >> pin) & 1) << colBits[pin];
                var odd = (address & 128) != 0;
                var input = 0;
                for (var bit = 0; bit < 4; bit++)
                {
                    var shift = 2 * bit + (odd ? 1 : 0);
                    if (!writeEnableHigh)
                        Memory[logical] = (byte)((Memory[logical] & ~(1 << shift)) | (((data >> bit) & 1) << shift));
                    input |= ((Memory[logical] >> shift) & 1) << bit;
                }
                Data = (byte)input;
                Accesses.Add((logical, odd, !writeEnableHigh, Memory[logical]));
                Accessed?.Invoke(logical, odd);
            }
            _ras = rowStrobeHigh;
            _cas = columnStrobeHigh;
        }
    }

    private sealed class Tape : IElectronTapePins
    {
        public bool InputPresent => false;
        public bool InputLevel => false;
        public void SetOutputLevel(bool value) { }
        public void Tick(bool motor) { }
    }

    private static int Finish(ElectronUla ula)
    {
        var ticks = 0;
        while (ula.CpuWaiting && ++ticks <= 128) ula.Tick();
        Assert.False(ula.CpuWaiting);
        ula.EndCpuCycle();
        return ticks;
    }

    [Fact]
    public void CpuReadCombinesTwoDifferentLiveGroupsInsteadOfReadingAWholeByte()
    {
        var ram = new Ram();
        var video = new ElectronVideo(ram);
        var ula = new ElectronUla(video, new Tape());
        ula.Write(7, 0x20);
        ram.Memory[0x0200] = 0x55;
        ram.Accessed = (address, odd) => { if (address == 0x0200 && !odd) ram.Memory[address] = 0xaa; };
        ula.BeginCpuCycle(0x0200, false, 0);
        Assert.Equal(2, Finish(ula));
        Assert.Equal(0xff, ula.CpuRamData);
        Assert.Equal(0xaa, ram.Memory[0x0200]);
        Assert.Equal(new[] { false, true }, ram.Accesses.Where(a => a.Address == 0x0200).Select(a => a.Odd));
    }

    [Fact]
    public void CpuWriteChangesOnlyTheSelectedGroupAtEachStrobe()
    {
        var ram = new Ram();
        var ula = new ElectronUla(new ElectronVideo(ram), new Tape());
        ula.Write(7, 0x20);
        ula.BeginCpuCycle(0x1234, true, 0xff);
        Finish(ula);
        Assert.Equal(new byte[] { 0x55, 0xff }, ram.Accesses.Where(a => a.Write).Select(a => a.Value));
        Assert.Equal(0xff, ram.Memory[0x1234]);
        Assert.Equal(0, ram.Memory[0x1233]);
        Assert.Equal(0, ram.Memory[0x1235]);
    }

    [Fact]
    public void HighBandwidthVideoRetainsItsSlotsUntilBlankingAndRomNeverWaits()
    {
        var ram = new Ram();
        var video = new ElectronVideo(ram);
        var ula = new ElectronUla(video, new Tape());
        ram.Memory[0x0200] = 0xa5;
        ula.BeginCpuCycle(0xc000, false, 0);
        ula.Tick();
        ula.EndCpuCycle();
        Assert.False(ula.CpuWaiting);
        ula.BeginCpuCycle(0x0200, false, 0);
        for (var tick = 0; tick < 77; tick++)
        {
            ula.Tick();
            Assert.True(ula.CpuWaiting);
            Assert.DoesNotContain(ram.Accesses, access => access.Address == 0x0200);
        }
        Assert.True(Finish(ula) <= 4);
        Assert.Equal(0xa5, ula.CpuRamData);
        Assert.True(video.Dot >= 640);
        Assert.Contains(ram.Accesses, access => access.Address >= 0x3000 && !access.Write);
    }

    [Fact]
    public void CpuPriorityStealsTheVideoSlotOnTheSharedDataPins()
    {
        var ram = new Ram();
        var video = new ElectronVideo(ram);
        var ula = new ElectronUla(video, new Tape());
        ram.Memory[0x0200] = 0xff;
        ula.Nmi = true;
        ula.BeginCpuCycle(0x0200, false, 0);
        Assert.Equal(2, Finish(ula));
        Assert.Equal(0xff, ula.CpuRamData);
        Assert.Equal(0xff, video.VideoData);
        Assert.Equal(0, ram.Memory[0x3000]);
    }
}
