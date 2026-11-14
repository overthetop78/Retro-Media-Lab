using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Audio.Yamaha.YM3438.Instruction;

public sealed partial class Ym3438
{
    public const uint Ym2612Mode = 1, ReadMode = 2;
    private uint chip_type = ReadMode;
    private readonly ym3438_t[] _state = [new()];
    private ArrayPointer<ym3438_t> State => new(_state);

    public void Reset() => OPN2_Reset(State);
    public void Configure(uint type) => OPN2_SetChipType(type);
    public void Clock(Span<short> stereo)
    {
        if (stereo.Length < 2) throw new ArgumentException("Two output channels are required.", nameof(stereo));
        OPN2_Clock(State, new ArrayPointer<short>(stereo));
    }
    public void Write(uint port, byte data) => OPN2_Write(State, port, data);
    public byte Read(uint port) => OPN2_Read(State, port);
    public void SetTestPin(uint value) => OPN2_SetTestPin(State, value);
    public uint ReadTestPin() => OPN2_ReadTestPin(State);
    public uint ReadIrqPin() => OPN2_ReadIRQPin(State);
    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("YM3438 context buffer is too short.", nameof(state));
        state[..ContextSize].Clear();
        Writeym3438_t(_state[0], state);
        return ContextSize;
    }
    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("YM3438 context buffer is too short.", nameof(state));
        Readym3438_t(_state[0], state);
        OPN2_Restore(State);
        return ContextSize;
    }
}
