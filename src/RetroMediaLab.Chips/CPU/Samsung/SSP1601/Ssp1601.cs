using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Samsung.SSP1601;

/// <summary>Instruction execution, internal RAM and register state, independent of a cartridge or host machine.</summary>
public sealed partial class Ssp1601
{
    public const int ContextSize = 0x454;
    private const uint FlagL = 0x1000, FlagZ = 0x2000, FlagV = 0x4000, FlagN = 0x8000;
    private readonly ISsp1601Bus _bus;
    private readonly byte[] _state = new byte[ContextSize];
    private int _pc, _cycles;
    private Span<uint> GeneralRegisters => MemoryMarshal.Cast<byte, uint>(_state.AsSpan(0x400, 64));
    private Span<byte> Pointers => _state.AsSpan(0x440, 8);
    private Span<ushort> InternalRam => MemoryMarshal.Cast<byte, ushort>(_state.AsSpan(0, 1024));
    private Span<ushort> Stack => MemoryMarshal.Cast<byte, ushort>(_state.AsSpan(0x448, 12));
    private uint Accumulator { get => GeneralRegisters[3]; set => GeneralRegisters[3] = value; }
    private uint Status { get => GetHigh(4); set => SetHigh(4, value); }
    public ushort ProgramCounter => (ushort)_pc;
    public ushort PreviousProgramCounter => (ushort)(_pc - 1);
    public ushort StatusRegister => (ushort)Status;
    public Memory<byte> RegisterState => _state;

    public Ssp1601(ISsp1601Bus bus) => _bus = bus ?? throw new ArgumentNullException(nameof(bus));
    public void Reset(ushort programCounter = 0)
    {
        GeneralRegisters[0] = 0xffff0000;
        SetHigh(6, programCounter); SetHigh(5, 0); SetHigh(4, 0);
        _pc = programCounter; _cycles = 0;
    }
    public int SaveContext(Span<byte> destination)
    {
        if (destination.Length < ContextSize) throw new ArgumentException("SSP1601 context is too short.", nameof(destination));
        _state.CopyTo(destination); return ContextSize;
    }
    public int LoadContext(ReadOnlySpan<byte> source)
    {
        if (source.Length < ContextSize) throw new ArgumentException("SSP1601 context is too short.", nameof(source));
        source[..ContextSize].CopyTo(_state); _pc = (ushort)GetHigh(6); _cycles = 0; return ContextSize;
    }
    private uint GetHigh(int index) => GeneralRegisters[index] >> 16;
    private void SetHigh(int index, uint data) => GeneralRegisters[index] = (GeneralRegisters[index] & 0xffff) | (data << 16);
    private uint Fetch() => _bus.ReadProgramWord((ushort)_pc++);
    private uint ReadStack()
    {
        SetHigh(5, unchecked(GetHigh(5) - 1));
        if (unchecked((short)GetHigh(5)) < 0) SetHigh(5, 5);
        return Stack[(int)GetHigh(5)];
    }
    private void WriteStack(uint data)
    {
        if (GetHigh(5) >= 6) SetHigh(5, 0);
        int index = (int)GetHigh(5); SetHigh(5, (uint)(index + 1)); Stack[index] = (ushort)data;
    }
    private uint ReadProduct()
    {
        GeneralRegisters[7] = unchecked((uint)((short)GetHigh(1) * (short)GetHigh(2) * 2));
        return GetHigh(7);
    }
    private void WritePc(uint value) { _pc = (ushort)value; _cycles--; }
    private uint ReadRegister(int index)
    {
        if (index <= 4) return GetHigh(index);
        if (index == 15)
        {
            _bus.AccumulatorLowRead(_bus.ReadProgramWord(PreviousProgramCounter));
            return Accumulator & 0xffff;
        }
        return index switch { 5 => ReadStack(), 6 => (uint)_pc, 7 => ReadProduct(), _ => _bus.ReadExternalRegister(index) };
    }
    private void WriteRegister(int index, uint data)
    {
        switch (index)
        {
            case 1: case 2: case 3: case 4: SetHigh(index, data); break;
            case 5: WriteStack(data); break;
            case 6: WritePc(data); break;
            case 15: Accumulator = (Accumulator & 0xffff0000) | (data & 0xffff); break;
            default: if (index >= 8) _bus.WriteExternalRegister(index, data); break;
        }
    }
}
