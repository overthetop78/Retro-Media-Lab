using System.Buffers.Binary;

namespace RetroMediaLab.Chips.Video.Sega.VDP32X;

public sealed partial class Vdp32X
{
    public const int ContextSize = 262720;
    public void SaveContext(Span<byte> data)
    {
        if (data.Length != ContextSize) throw new ArgumentException(nameof(data)); data.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(data, 0x32cd0001); int offset = 4;
        int[] words = [_mode, _shift, _length, _address, _fillData, _requestedBank, _activeBank, _fillCycles, Pal ? 1 : 0, HBlank ? 1 : 0, VBlank ? 1 : 0, Sh2Access ? 1 : 0];
        foreach (int word in words) { BinaryPrimitives.WriteInt32LittleEndian(data[offset..], word); offset += 4; }
        foreach (ushort word in _dram) { BinaryPrimitives.WriteUInt16LittleEndian(data[offset..], word); offset += 2; }
        foreach (ushort word in _palette) { BinaryPrimitives.WriteUInt16LittleEndian(data[offset..], word); offset += 2; }
    }
    public static bool CanLoadContext(ReadOnlySpan<byte> data) => data.Length == ContextSize && BinaryPrimitives.ReadUInt32LittleEndian(data) == 0x32cd0001
        && BinaryPrimitives.ReadUInt32LittleEndian(data[24..]) <= 1 && BinaryPrimitives.ReadUInt32LittleEndian(data[28..]) <= 1
        && BinaryPrimitives.ReadUInt32LittleEndian(data[32..]) <= 775;
    public void LoadContext(ReadOnlySpan<byte> data)
    {
        if (!CanLoadContext(data)) throw new ArgumentException(nameof(data)); int offset = 4; int[] w = new int[12];
        for (int i = 0; i < w.Length; i++) { w[i] = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]); offset += 4; }
        _mode = (ushort)w[0]; _shift = (ushort)w[1]; _length = (ushort)w[2]; _address = (ushort)w[3]; _fillData = (ushort)w[4];
        _requestedBank = w[5]; _activeBank = w[6]; _fillCycles = w[7]; Pal = w[8] != 0; HBlank = w[9] != 0; VBlank = w[10] != 0; Sh2Access = w[11] != 0;
        for (int i = 0; i < _dram.Length; i++) { _dram[i] = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]); offset += 2; }
        for (int i = 0; i < _palette.Length; i++) { _palette[i] = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]); offset += 2; }
    }
}
