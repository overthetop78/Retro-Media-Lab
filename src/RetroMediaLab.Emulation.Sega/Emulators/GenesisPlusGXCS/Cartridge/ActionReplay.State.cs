using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class ActionReplay
{
    private sealed class ActionReplayState
    {
        public byte enabled;
        public byte status;
        public readonly byte[] ram = new byte[65536];
        public readonly ushort[] regs = new ushort[13];
        public readonly ushort[] old = new ushort[4];
        public readonly ushort[] data = new ushort[4];
        public readonly uint[] addr = new uint[4];
    }
    private readonly ActionReplayState action_replay = new();
    private void Clear_ram() => action_replay.ram.AsSpan().Clear();
    private void Clear_regs() => action_replay.regs.AsSpan().Clear();
    private void Clear_old() => action_replay.old.AsSpan().Clear();
    private void Clear_data() => action_replay.data.AsSpan().Clear();
    private void Clear_addr() => action_replay.addr.AsSpan().Clear();
}
