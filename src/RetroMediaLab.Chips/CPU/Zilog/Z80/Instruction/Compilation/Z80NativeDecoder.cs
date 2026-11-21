namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction.Compilation;

internal enum Z80NativeFamily { Base, Cb, Ed, IndexedCb }
internal readonly record struct Z80NativeCodeByte(ushort Address, byte Value);
internal sealed record Z80NativeInstruction(ushort Address, ushort NextAddress, Z80NativeFamily Family, byte Opcode,
    int Index, int OpcodeBytes, int RefreshIncrement, byte LastFetch, int[] CycleCosts, Z80NativeCodeByte[] Guards)
{
    public bool EndsBlock => Family == Z80NativeFamily.Ed || Family == Z80NativeFamily.Base
        && (Opcode == 0x76 || (Opcode & 0xe7) == 0x20 || Opcode is 0x10 or 0x18
            || Opcode >= 0xc0 && ((Opcode & 7) is 0 or 2 or 4 or 7
                || Opcode is 0xc3 or 0xc9 or 0xcd or 0xe9));
}

/// <summary>
/// Prefix/fetch sequencing follows the existing Genesis-derived dispatchers;
/// operand and x86 translation use the same family split as BlastEm z80inst.c.
/// Immediate bytes are read at execution time, not embedded in cached blocks.
/// </summary>
internal static class Z80NativeDecoder
{
    public static Z80NativeInstruction Decode(ushort address, Func<int, byte> read, int[][] cycles)
    {
        var guards = new List<Z80NativeCodeByte>(); var costs = new List<int>();
        int cursor = 0, refresh = 1, index = 0; byte last = 0;
        byte Fetch()
        {
            ushort position = unchecked((ushort)(address + cursor++));
            byte value = read(position); guards.Add(new(position, value)); last = value; return value;
        }
        byte opcode = Fetch(); costs.Add(cycles[0][opcode]);
        var family = Z80NativeFamily.Base;
        if (opcode is 0xdd or 0xfd)
        {
            index = opcode == 0xdd ? 1 : 2; refresh++;
            while (true)
            {
                if (cursor >= 65536) throw new InvalidOperationException("The entire Z80 address space consists of index prefixes.");
                opcode = Fetch(); costs.Add(cycles[3][opcode]);
                if (opcode is 0xdd or 0xfd) { index = opcode == 0xdd ? 1 : 2; continue; }
                if (opcode == 0xcb)
                {
                    ushort final = unchecked((ushort)(address + cursor + 1));
                    opcode = read(final); guards.Add(new(final, opcode));
                    family = Z80NativeFamily.IndexedCb; costs.Add(cycles[4][opcode]);
                    return new(address, unchecked((ushort)(address + cursor + 2)), family, opcode, index,
                        cursor, refresh, last, costs.ToArray(), guards.ToArray());
                }
                if (opcode == 0xed)
                {
                    refresh++; opcode = Fetch(); costs.Add(cycles[2][opcode]); family = Z80NativeFamily.Ed; index = 0;
                }
                break;
            }
        }
        else if (opcode is 0xcb or 0xed)
        {
            family = opcode == 0xcb ? Z80NativeFamily.Cb : Z80NativeFamily.Ed;
            refresh++; opcode = Fetch(); costs.Add(cycles[family == Z80NativeFamily.Cb ? 1 : 2][opcode]);
        }
        int operands = family == Z80NativeFamily.Cb ? 0 : family == Z80NativeFamily.Ed
            ? (opcode & 0xc7) == 0x43 ? 2 : 0 : OperandBytes(opcode, index != 0);
        return new(address, unchecked((ushort)(address + cursor + operands)), family, opcode, index,
            cursor, refresh, last, costs.ToArray(), guards.ToArray());
    }
    private static int OperandBytes(byte op, bool indexed)
    {
        int x = op >> 6, y = op >> 3 & 7, z = op & 7, p = y >> 1, q = y & 1;
        return x switch
        {
            0 => z switch
            {
                0 => y >= 2 ? 1 : 0,
                1 => q == 0 ? 2 : 0,
                2 => p >= 2 ? 2 : 0,
                4 or 5 => indexed && y == 6 ? 1 : 0,
                6 => 1 + (indexed && y == 6 ? 1 : 0), _ => 0
            },
            1 => indexed && op != 0x76 && (y == 6 || z == 6) ? 1 : 0,
            2 => indexed && z == 6 ? 1 : 0,
            _ => z switch { 2 or 4 => 2, 3 => y == 0 ? 2 : y is 2 or 3 ? 1 : 0,
                5 => q != 0 && p == 0 ? 2 : 0, 6 => 1, _ => 0 }
        };
    }
}
