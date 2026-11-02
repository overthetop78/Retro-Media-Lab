namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

/// <summary>Jeu CMOS du 65C12/65C102 : les BBC NMOS gardent leur décodeur et leurs indicateurs.</summary>
internal sealed partial class Cpu6502
{
    private int ExecuteCmos(byte opcode)
    {
        ushort address;
        byte value;
        switch (opcode)
        {
            case 0x04: case 0x0c: case 0x14: case 0x1c:
                address = (opcode & 8) == 0 ? Fetch() : FetchWord();
                value = Read(address); Flag(Zero, (A & value) == 0);
                _ = Read(address);
                Write(address, (opcode & 0x10) == 0 ? (byte)(value | A) : (byte)(value & ~A));
                return (opcode & 8) == 0 ? 5 : 6;
            case 0x1a: A = Nz((byte)(A + 1)); return 2;
            case 0x3a: A = Nz((byte)(A - 1)); return 2;
            case 0x5a: Push(Y); return 3;
            case 0x7a: Y = Nz(Pop()); return 4;
            case 0xda: Push(X); return 3;
            case 0xfa: X = Nz(Pop()); return 4;
            case 0x64: Write(Fetch(), 0); return 3;
            case 0x74: Write((byte)(Fetch() + X), 0); return 4;
            case 0x9c: Write(FetchWord(), 0); return 4;
            case 0x9e: Write((ushort)(FetchWord() + X), 0); return 5;
            case 0x80:
                var displacement = (sbyte)Fetch(); var previous = PC; PC = (ushort)(PC + displacement);
                return (PC & 0xff00) == (previous & 0xff00) ? 3 : 4;
            case 0x89: Flag(Zero, (A & Fetch()) == 0); return 2;
            case 0x34: Bit(Read((byte)(Fetch() + X))); return 4;
            case 0x3c: address = FetchWord(); Bit(Read((ushort)(address + X))); return 4 + PageCross(address, X);
            case 0x7c: address = (ushort)(FetchWord() + X); PC = ReadWord(address); return 6;
        }
        if ((opcode & 0x1f) == 0x12)
        {
            address = ZeroPageWord(Fetch());
            if (opcode == 0x92) Write(address, A);
            else
            {
                value = Read(address);
                switch (opcode >> 5)
                {
                    case 0: A = Nz((byte)(A | value)); break;
                    case 1: A = Nz((byte)(A & value)); break;
                    case 2: A = Nz((byte)(A ^ value)); break;
                    case 3: Add(value); break;
                    case 5: A = Nz(value); break;
                    case 6: Compare(A, value); break;
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
        return Execute(opcode);
    }
}
