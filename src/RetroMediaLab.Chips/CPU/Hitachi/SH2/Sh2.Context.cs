using System.Buffers.Binary;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

public partial class Sh2<TBus> where TBus : Contracts.ISh2Bus
{
    public const int ContextSize = 136;
    public void SaveContext(Span<byte> data)
    {
        SynchronizeBusClock();
        if (data.Length != ContextSize) throw new ArgumentException(nameof(data));
        data.Clear();
        uint[] words = [0x53483201, ..R, PC, PPC, GBR, VBR, PR, SR, unchecked((uint)MAC), unchecked((uint)(MAC >> 32)),
            (uint)_branch, ID, ET, (uint)_irqLevel, (uint)_irqVector, Sleeping ? 1u : 0u];
        for (int i = 0; i < words.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(i * 4, 4), words[i]);
        BinaryPrimitives.WriteUInt64LittleEndian(data[128..], Cycles);
        BinaryPrimitives.WriteUInt32LittleEndian(data[124..], PollingEpoch);
    }
    public static bool CanLoadContext(ReadOnlySpan<byte> data) => data.Length == ContextSize
        && BinaryPrimitives.ReadUInt32LittleEndian(data) == 0x53483201
        && BinaryPrimitives.ReadUInt32LittleEndian(data[100..]) <= 2
        && BinaryPrimitives.ReadUInt32LittleEndian(data[112..]) <= 16
        && BinaryPrimitives.ReadUInt32LittleEndian(data[116..]) <= 255;
    public void LoadContext(ReadOnlySpan<byte> data)
    {
        if (!CanLoadContext(data)) throw new ArgumentException(nameof(data));
        ClearCodeCache(); _pendingBusClocks = 0; _busDeadline = _bus.NextEventCycle;
        uint[] words = new uint[31];
        for (int i = 0; i < words.Length; i++) words[i] = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(i * 4, 4));
        Array.Copy(words, 1, R, 0, 16); PC = words[17]; PPC = words[18]; GBR = words[19]; VBR = words[20]; PR = words[21];
        SR = words[22]; MAC = unchecked((long)((ulong)words[24] << 32 | words[23])); _branch = (int)words[25];
        ID = words[26]; ET = words[27]; _irqLevel = (int)words[28]; _irqVector = (int)words[29];
        Sleeping = words[30] != 0; Cycles = BinaryPrimitives.ReadUInt64LittleEndian(data[128..]);
        PollingEpoch = BinaryPrimitives.ReadUInt32LittleEndian(data[124..]); _yield = false;
    }
}
