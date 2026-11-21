using RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

public partial class Sh2<TBus> where TBus : ISh2Bus
{
    private bool _useNativeInlineIdle;
    /// <summary>Translate certified idle loops; select outside Execute. Original helpers remain available.</summary>
    public bool UseNativeInlineIdle
    {
        get => _useNativeInlineIdle;
        set { if (value == _useNativeInlineIdle) return; ClearCodeCache(); _useNativeInlineIdle = value; }
    }

    private int[]? NativeIdlePlan(uint address, IReadOnlyList<ushort> words)
    {
        if (!_useNativeInlineIdle || !_deferClockUpdates || _bus is not ISh2NativeIdleBus provider) return null;
        var result = new int[words.Count];
        for (int i = 0; i + 1 < words.Count; i++)
        {
            ushort opcode = words[i];
            bool dt = (opcode & 0xf0ff) == 0x4010 && words[i + 1] == 0x8bfd;
            bool bra = opcode == 0xaffe && words[i + 1] == 9;
            bool triple = opcode == 0xaffd && i > 0 && words[i - 1] == 9 && words[i + 1] == 9;
            if (!dt && !bra && !triple) continue;
            uint first = unchecked(address + (uint)((triple ? i - 1 : i) * 2));
            if (provider.TryGetNativeIdleClocks(first, triple ? 3 : 2, out int clocks) && clocks >= (triple ? 4 : 3))
                result[i] = clocks;
        }
        return result;
    }
}
