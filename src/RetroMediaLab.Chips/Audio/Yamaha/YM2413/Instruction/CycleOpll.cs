using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Audio.Yamaha.YM2413.Instruction;

public sealed partial class CycleOpll
{
    public const uint Ym2413 = 0, Ds1001 = 1, Ym2413B = 2;
    private readonly opll_t[] _state = [new()];
    private ArrayPointer<opll_t> State => new(_state);

    public void Reset(uint type) => OPLL_Reset(State, type);
    public void Clock(Span<int> stereo)
    {
        if (stereo.Length < 2) throw new ArgumentException("Two output channels are required.", nameof(stereo));
        OPLL_Clock(State, new ArrayPointer<int>(stereo));
    }
    public void Write(uint port, byte data) => OPLL_Write(State, port, data);
    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("OPLL context buffer is too short.", nameof(state));
        state[..ContextSize].Clear();
        Writeopll_t(_state[0], state);
        return ContextSize;
    }
    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("OPLL context buffer is too short.", nameof(state));
        Readopll_t(_state[0], state);
        OPLL_Restore(State);
        return ContextSize;
    }
}
