namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

/// <summary>Jeu CMOS du 65C12/65C102 : les BBC NMOS gardent leur décodeur et leurs indicateurs.</summary>
public sealed partial class Cpu6502
{
    private int ExecuteCmos(byte opcode)
    {
        ushort address;
        byte value;
        switch (opcode)
        {
            case 0x04: case 0x0c: case 0x14: case 0x1c:
                address = (opcode & 8) == 0 ? Fetch() : FetchWord();
                value = Read(address); Flag(Zero, (_a & value) == 0);
                _ = Read(address);
                Write(address, (opcode & 0x10) == 0 ? (byte)(value | _a) : (byte)(value & ~_a));
                return (opcode & 8) == 0 ? 5 : 6;
            case 0x1a: _a = Nz((byte)(_a + 1)); return 2;
            case 0x3a: _a = Nz((byte)(_a - 1)); return 2;
            case 0x5a: Read(_pc); Push(_y); return 3;
            case 0x7a: PreparePull(); _y = Nz(Pop()); return 4;
            case 0xda: Read(_pc); Push(_x); return 3;
            case 0xfa: PreparePull(); _x = Nz(Pop()); return 4;
            case 0x64: Write(Fetch(), 0); return 3;
            case 0x74: Write(IndexedZeroPage(_x), 0); return 4;
            case 0x9c: Write(FetchWord(), 0); return 4;
            case 0x9e: Write(IndexedAddress(FetchWord(), _x, true), 0); return 5;
            case 0x80:
                return Branch(true);
            case 0x89: Flag(Zero, (_a & Fetch()) == 0); return 2;
            case 0x34: Bit(Read(IndexedZeroPage(_x))); return 4;
            case 0x3c: address = FetchWord(); Bit(Read(IndexedAddress(address, _x, false))); return 4 + PageCross(address, _x);
            case 0x7c:
                address = (ushort)(FetchWord() + _x);
                Read((ushort)(_pc - 2));
                _pc = ReadWord(address);
                return 6;
        }
        if ((opcode & 0x1f) == 0x12)
        {
            address = ZeroPageWord(Fetch());
            if (opcode == 0x92) Write(address, _a);
            else
            {
                value = Read(address);
                switch (opcode >> 5)
                {
                    case 0: _a = Nz((byte)(_a | value)); break;
                    case 1: _a = Nz((byte)(_a & value)); break;
                    case 2: _a = Nz((byte)(_a ^ value)); break;
                    case 3: Add(value); break;
                    case 5: _a = Nz(value); break;
                    case 6: Compare(_a, value); break;
                    case 7: Subtract(value); break;
                }
            }
            return 5;
        }
        // Le 65C12 ne possède pas les RMB/SMB/BBR/BBS des variantes Rockwell : ces codes sont des NOP.
        if ((opcode & 7) == 7 || opcode is 0x03 or 0x0b or 0x13 or 0x1b or 0x23 or 0x2b or 0x33 or 0x3b or 0x43 or 0x4b or 0x53 or 0x5b or 0x63 or 0x6b or 0x73 or 0x7b or 0x83 or 0x8b or 0x93 or 0x9b or 0xa3 or 0xab or 0xb3 or 0xbb or 0xc3 or 0xcb or 0xd3 or 0xdb or 0xe3 or 0xeb or 0xf3 or 0xfb) return 1;
        if (opcode is 0x02 or 0x22 or 0x42 or 0x62 or 0x82 or 0xc2 or 0xe2) { Fetch(); return 2; }
        if (opcode == 0x44) { Fetch(); return 3; }
        if (opcode is 0x54 or 0xd4 or 0xf4) { Fetch(); return 4; }
        if (opcode is 0x5c or 0xdc or 0xfc) { FetchWord(); return opcode == 0x5c ? 8 : 4; }
        return ExecuteCmosBase(opcode);
    }
}
