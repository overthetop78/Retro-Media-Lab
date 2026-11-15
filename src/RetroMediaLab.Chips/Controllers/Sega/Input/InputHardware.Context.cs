using System.Buffers.Binary;

namespace RetroMediaLab.Chips.Controllers.Sega.Input;

public sealed partial class InputHardware
{
    public const int ContextSize = 221;
    public static bool CanLoadContext(ReadOnlySpan<byte> state) => state.Length >= ContextSize && state[0] == 1;

    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("Input context is too short.", nameof(state));
        var w = new ContextWriter(state);
        w.Byte(1); w.Byte(system_hw);
        w.Byte(_deviceConfiguration.AutomaticPadType); w.Byte(_deviceConfiguration.PrimaryDevice);
        w.Byte(_deviceConfiguration.AdditionalPads ? (byte)1 : (byte)0);
        w.Bytes(configuredPadTypes);
        w.Ints(old_system);
        w.Bytes(input.system);
        w.Bytes(input.dev);
        w.UShorts(input.pad);
        w.Shorts(input.analog);
        w.Int(input.x_offset);
        w.Int(input.y_offset);
        w.Bytes(io_reg);
        w.Byte(latch);
        foreach (var device in activator) { w.Byte(device.State); w.Byte(device.Counter); }
        foreach (var device in gamepad) { w.Byte(device.State); w.Byte(device.Counter); w.Byte(device.Timeout); w.UInt(device.Latency); }
        foreach (var device in flipflop) { w.Byte(device.Latch); w.Byte(device.Counter); }
        w.Byte(board.State); w.Byte(board.Counter); w.Byte(board.Port);
        w.Byte(lightgun.State); w.Byte(lightgun.Port);
        w.Byte(mouse.State); w.Byte(mouse.Counter); w.Byte(mouse.Wait); w.Byte(mouse.Port);
        foreach (var device in paddle) { w.Byte(device.State); w.Byte(device.Counter); }
        foreach (var device in sportspad) { w.Byte(device.State); w.Byte(device.Counter); }
        foreach (var device in teamplayer) { w.Byte(device.State); w.Byte(device.Counter); w.Bytes(device.Table); }
        w.Byte(tablet.axis); w.Byte(tablet.busy);
        foreach (var device in xe_1ap) { w.Byte(device.State); w.Byte(device.Counter); w.Byte(device.Latency); }
        if (w.Position != ContextSize) throw new InvalidOperationException("Input context size mismatch.");
        return w.Position;
    }

    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (!CanLoadContext(state)) throw new ArgumentException("Unsupported input context.", nameof(state));
        var r = new ContextReader(state);
        r.Byte(); system_hw = r.Byte();
        byte automatic = r.Byte(), primary = r.Byte();
        _deviceConfiguration = new(automatic, primary, r.Byte() != 0);
        r.Bytes(configuredPadTypes);
        r.Ints(old_system);
        r.Bytes(input.system);
        r.Bytes(input.dev);
        r.UShorts(input.pad);
        r.Shorts(input.analog);
        input.x_offset = r.Int();
        input.y_offset = r.Int();
        r.Bytes(io_reg);
        latch = r.Byte();
        foreach (var device in activator) { device.State = r.Byte(); device.Counter = r.Byte(); }
        foreach (var device in gamepad) { device.State = r.Byte(); device.Counter = r.Byte(); device.Timeout = r.Byte(); device.Latency = r.UInt(); }
        foreach (var device in flipflop) { device.Latch = r.Byte(); device.Counter = r.Byte(); }
        board.State = r.Byte(); board.Counter = r.Byte(); board.Port = r.Byte();
        lightgun.State = r.Byte(); lightgun.Port = r.Byte();
        mouse.State = r.Byte(); mouse.Counter = r.Byte(); mouse.Wait = r.Byte(); mouse.Port = r.Byte();
        foreach (var device in paddle) { device.State = r.Byte(); device.Counter = r.Byte(); }
        foreach (var device in sportspad) { device.State = r.Byte(); device.Counter = r.Byte(); }
        foreach (var device in teamplayer) { device.State = r.Byte(); device.Counter = r.Byte(); r.Bytes(device.Table); }
        tablet.axis = r.Byte(); tablet.busy = r.Byte();
        foreach (var device in xe_1ap) { device.State = r.Byte(); device.Counter = r.Byte(); device.Latency = r.Byte(); }
        BindIoPorts();
        return r.Position;
    }

    private ref struct ContextWriter
    {
        private Span<byte> target;
        public ContextWriter(Span<byte> target) { this.target = target; Position = 0; }
        public int Position;
        public void Byte(byte value) => target[Position++] = value;
        public void UInt(uint value) { BinaryPrimitives.WriteUInt32LittleEndian(target[Position..], value); Position += 4; }
        public void Int(int value) { BinaryPrimitives.WriteInt32LittleEndian(target[Position..], value); Position += 4; }
        public void UShort(ushort value) { BinaryPrimitives.WriteUInt16LittleEndian(target[Position..], value); Position += 2; }
        public void Bytes(ReadOnlySpan<byte> values) { values.CopyTo(target[Position..]); Position += values.Length; }
        public void Ints(ReadOnlySpan<int> values) { foreach (int value in values) Int(value); }
        public void UShorts(ReadOnlySpan<ushort> values) { foreach (ushort value in values) UShort(value); }
        public void Shorts(ReadOnlySpan<short> values) { foreach (short value in values) UShort(unchecked((ushort)value)); }
    }

    private ref struct ContextReader
    {
        private ReadOnlySpan<byte> source;
        public ContextReader(ReadOnlySpan<byte> source) { this.source = source; Position = 0; }
        public int Position;
        public byte Byte() => source[Position++];
        public uint UInt() { uint value = BinaryPrimitives.ReadUInt32LittleEndian(source[Position..]); Position += 4; return value; }
        public int Int() { int value = BinaryPrimitives.ReadInt32LittleEndian(source[Position..]); Position += 4; return value; }
        public ushort UShort() { ushort value = BinaryPrimitives.ReadUInt16LittleEndian(source[Position..]); Position += 2; return value; }
        public void Bytes(Span<byte> values) { source.Slice(Position, values.Length).CopyTo(values); Position += values.Length; }
        public void Ints(Span<int> values) { for (int i = 0; i < values.Length; i++) values[i] = Int(); }
        public void UShorts(Span<ushort> values) { for (int i = 0; i < values.Length; i++) values[i] = UShort(); }
        public void Shorts(Span<short> values) { for (int i = 0; i < values.Length; i++) values[i] = unchecked((short)UShort()); }
    }
}

