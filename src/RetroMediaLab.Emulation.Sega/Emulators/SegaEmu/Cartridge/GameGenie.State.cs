using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal sealed partial class GameGenie
{
    private sealed class GameGenieState
    {
        public byte enabled;
        public readonly ushort[] regs = new ushort[32];
        public readonly ushort[] old = new ushort[6];
        public readonly ushort[] data = new ushort[6];
        public readonly uint[] addr = new uint[6];
    }
    private readonly GameGenieState ggenie = new();
    private void Clear_regs() => ggenie.regs.AsSpan().Clear();
    private void Clear_old() => ggenie.old.AsSpan().Clear();
    private void Clear_data() => ggenie.data.AsSpan().Clear();
    private void Clear_addr() => ggenie.addr.AsSpan().Clear();
}
