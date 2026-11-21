using System.Buffers.Binary;

namespace RetroMediaLab.Chips.Audio.Sega.PWM32X;

public sealed partial class Pwm32X
{
    public const int ContextSize = 96;
    public void SaveContext(Span<byte> data)
    {
        if (data.Length != ContextSize) throw new ArgumentException(nameof(data)); data.Clear();
        int[] words = [0x32a0001, _control, _cycle, _head[0], _head[1], _count[0], _count[1], _sample[0], _sample[1], _periods, _lastLeft, _lastRight, InterruptPending ? 1 : 0];
        int offset = 0; foreach (int word in words) { BinaryPrimitives.WriteInt32LittleEndian(data[offset..], word); offset += 4; }
        foreach (ushort word in _fifo) { BinaryPrimitives.WriteUInt16LittleEndian(data[offset..], word); offset += 2; }
        BinaryPrimitives.WriteInt64LittleEndian(data[64..], _nextTick); BinaryPrimitives.WriteUInt32LittleEndian(data[72..], _tick);
    }
    public static bool CanLoadContext(ReadOnlySpan<byte> data) => data.Length == ContextSize && BinaryPrimitives.ReadUInt32LittleEndian(data) == 0x32a0001
        && BinaryPrimitives.ReadUInt32LittleEndian(data[12..]) < 3 && BinaryPrimitives.ReadUInt32LittleEndian(data[16..]) < 3
        && BinaryPrimitives.ReadUInt32LittleEndian(data[20..]) <= 3 && BinaryPrimitives.ReadUInt32LittleEndian(data[24..]) <= 3;
    public void LoadContext(ReadOnlySpan<byte> data)
    {
        if (!CanLoadContext(data)) throw new ArgumentException(nameof(data)); int[] w = new int[13]; int offset = 0;
        for (int i = 0; i < w.Length; i++) { w[i] = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]); offset += 4; }
        _control = (ushort)w[1]; _cycle = (ushort)w[2]; _head[0] = w[3]; _head[1] = w[4]; _count[0] = w[5]; _count[1] = w[6];
        _sample[0] = w[7]; _sample[1] = w[8]; _periods = w[9]; _lastLeft = w[10]; _lastRight = w[11]; InterruptPending = w[12] != 0;
        for (int i = 0; i < _fifo.Length; i++) { _fifo[i] = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]); offset += 2; }
        _nextTick = BinaryPrimitives.ReadInt64LittleEndian(data[64..]); _tick = BinaryPrimitives.ReadUInt32LittleEndian(data[72..]);
        _output.AddDeltaFast(0, _lastLeft, _lastRight); InterruptChanged?.Invoke();
    }
}
