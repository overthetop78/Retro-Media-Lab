namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Services;

/// <summary>Intel 80186 en mode réel : registres, bus 20 bits, segments et pile. Les périphériques restent sur la carte.</summary>
internal sealed partial class Cpu80186(Func<int, byte> read, Action<int, byte> write, Func<ushort, byte> input, Action<ushort, byte> output)
{
    private const int Ax = 0;
    private const int Cx = 1;
    private const int Dx = 2;
    private const int Bx = 3;
    private const int Sp = 4;
    private const int Bp = 5;
    private const int Si = 6;
    private const int Di = 7;
    private const int Es = 0;
    private const int Cs = 1;
    private const int Ss = 2;
    private const int Ds = 3;
    private const ushort Carry = 0x0001;
    private const ushort Reserved = 0x0002;
    private const ushort ReservedHigh = 0xf000;
    private const ushort Parity = 0x0004;
    private const ushort Auxiliary = 0x0010;
    private const ushort Zero = 0x0040;
    private const ushort Sign = 0x0080;
    private const ushort Trap = 0x0100;
    private const ushort InterruptEnable = 0x0200;
    private const ushort Direction = 0x0400;
    private const ushort Overflow = 0x0800;
    private const int AddressMask = 0xfffff;
    private readonly ushort[] _registers = new ushort[8];
    private readonly ushort[] _segments = new ushort[4];
    private ushort _flags = Reserved;
    private ushort _start;
    private int _segmentOverride = -1;
    private byte _repeat;
    private bool _halted;
    private int _interruptDelay;
    internal ushort IP { get; private set; }
    internal long Cycles { get; private set; }
    internal bool InterruptPending { get; set; }
    internal byte InterruptVector { get; set; }
    internal ushort Register(int index) => _registers[index];
    internal ushort Segment(int index) => _segments[index];
    internal ushort Flags => _flags;
    internal void Reset()
    {
        Array.Clear(_registers); Array.Clear(_segments);
        _segments[Cs] = 0xffff; IP = 0; _flags = Reserved; _halted = false; _interruptDelay = 0;
    }
    private int Physical(int segment, ushort offset) => ((_segments[segment] << 4) + offset) & AddressMask;
    private byte ReadByte(int segment, ushort offset) => read(Physical(segment, offset));
    private ushort ReadWord(int segment, ushort offset) => (ushort)(ReadByte(segment, offset) | ReadByte(segment, (ushort)(offset + 1)) << 8);
    private void WriteByte(int segment, ushort offset, byte value) => write(Physical(segment, offset), value);
    private void WriteWord(int segment, ushort offset, ushort value)
    { WriteByte(segment, offset, (byte)value); WriteByte(segment, (ushort)(offset + 1), (byte)(value >> 8)); }
    private byte Fetch() => ReadByte(Cs, IP++);
    private ushort FetchWord() { var low = Fetch(); return (ushort)(low | Fetch() << 8); }
    private void Push(ushort value) { _registers[Sp] -= 2; WriteWord(Ss, _registers[Sp], value); }
    private ushort Pop() { var value = ReadWord(Ss, _registers[Sp]); _registers[Sp] += 2; return value; }
    private int ByteRegister(int index) => index < 4 ? _registers[index] & 255 : _registers[index - 4] >> 8;
    private void SetByteRegister(int index, int value)
    {
        if (index < 4) _registers[index] = (ushort)((_registers[index] & 0xff00) | (value & 255));
        else _registers[index - 4] = (ushort)((_registers[index - 4] & 255) | ((value & 255) << 8));
    }
    private readonly record struct Operand(bool Register, int Index, int Segment, ushort Offset);
    private Operand Decode(byte modrm)
    {
        var mod = modrm >> 6; var rm = modrm & 7;
        if (mod == 3) return new(true, rm, 0, 0);
        var bp = rm is 2 or 3 || rm == 6 && mod != 0;
        var offset = rm switch
        {
            0 => _registers[Bx] + _registers[Si], 1 => _registers[Bx] + _registers[Di],
            2 => _registers[Bp] + _registers[Si], 3 => _registers[Bp] + _registers[Di],
            4 => _registers[Si], 5 => _registers[Di], 6 => mod == 0 ? FetchWord() : _registers[Bp], _ => _registers[Bx]
        };
        if (mod == 1) offset += (sbyte)Fetch();
        else if (mod == 2) offset += FetchWord();
        return new(false, 0, _segmentOverride >= 0 ? _segmentOverride : bp ? Ss : Ds, (ushort)offset);
    }
    private int Load(Operand operand, bool word) => operand.Register ? word ? _registers[operand.Index] : ByteRegister(operand.Index)
        : word ? ReadWord(operand.Segment, operand.Offset) : ReadByte(operand.Segment, operand.Offset);
    private void Store(Operand operand, bool word, int value)
    {
        if (operand.Register) { if (word) _registers[operand.Index] = (ushort)value; else SetByteRegister(operand.Index, value); }
        else if (word) WriteWord(operand.Segment, operand.Offset, (ushort)value); else WriteByte(operand.Segment, operand.Offset, (byte)value);
    }
    private void Flag(ushort flag, bool value) => _flags = value ? (ushort)(_flags | flag) : (ushort)(_flags & ~flag);
    private bool Has(ushort flag) => (_flags & flag) != 0;
    private void Interrupt(byte vector, bool fault = false)
    {
        _halted = false; Push((ushort)(_flags | ReservedHigh)); Push(_segments[Cs]); Push(fault ? _start : IP);
        _flags &= unchecked((ushort)~(InterruptEnable | Trap));
        IP = (ushort)(read(vector * 4) | read(vector * 4 + 1) << 8);
        _segments[Cs] = (ushort)(read(vector * 4 + 2) | read(vector * 4 + 3) << 8);
    }
    internal void Step()
    {
        if (InterruptPending && Has(InterruptEnable) && _interruptDelay == 0) { Interrupt(InterruptVector); Cycles += 45; return; }
        if (_interruptDelay > 0) _interruptDelay--;
        if (_halted) { Cycles++; return; }
        _start = IP; _segmentOverride = -1; _repeat = 0;
        var trace = Has(Trap); byte opcode;
        while (true)
        {
            opcode = Fetch();
            if (opcode is 0x26 or 0x2e or 0x36 or 0x3e) _segmentOverride = (opcode >> 3) & 3;
            else if (opcode is 0xf2 or 0xf3) _repeat = opcode;
            else if (opcode != 0xf0) break;
        }
        Cycles += Execute(opcode);
        _flags |= Reserved;
        if (trace && Has(Trap)) Interrupt(1);
    }
}
