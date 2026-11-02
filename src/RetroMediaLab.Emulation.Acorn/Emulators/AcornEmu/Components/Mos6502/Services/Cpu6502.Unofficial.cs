using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Constants.Cpu6502Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Services;

/// <summary>Décodage NMOS ; les instructions combinées partagent les accès RMW réels.</summary>
internal sealed partial class Cpu6502
{
    private bool TryExecuteUnofficial(byte opcode, out int cycles)
    {
        cycles = 2;
        switch (opcode)
        {
            case 0x02: case 0x12: case 0x22: case 0x32: case 0x42: case 0x52:
            case 0x62: case 0x72: case 0x92: case 0xb2: case 0xd2: case 0xf2:
                _jammed = true;
                return true;
            case 0x1a: case 0x3a: case 0x5a: case 0x7a: case 0xda: case 0xfa: return true;
            case 0x80: case 0x82: case 0x89: case 0xc2: case 0xe2: Fetch(); return true;
            case 0x04: case 0x44: case 0x64: Read(Fetch()); cycles = 3; return true;
            case 0x14: case 0x34: case 0x54: case 0x74: case 0xd4: case 0xf4:
                Read(IndexedZeroPage(X)); cycles = 4; return true;
            case 0x0c: Read(FetchWord()); cycles = 4; return true;
            case 0x1c: case 0x3c: case 0x5c: case 0x7c: case 0xdc: case 0xfc:
                var nopBase = FetchWord(); Read(IndexedAddress(nopBase, X, false));
                cycles = 4 + PageCross(nopBase, X); return true;
            case 0x0b: case 0x2b:
                A = Nz((byte)(A & Fetch())); Flag(Carry, (A & Negative) != 0); return true;
            case 0x4b:
                var and = A & Fetch(); Flag(Carry, (and & Carry) != 0); A = Nz((byte)(and >> 1)); return true;
            case 0x6b: Arr(Fetch()); return true;
            case 0x8b: A = Nz((byte)((A | C.UnstableBusMask) & X & Fetch())); return true;
            case 0xab: A = X = Nz((byte)((A | C.UnstableBusMask) & Fetch())); return true;
            case 0xcb:
                var difference = (A & X) - Fetch(); Flag(Carry, difference >= 0); X = Nz((byte)difference); return true;
            case 0xeb: Subtract(Fetch()); return true;
            case 0x9c: UnstableStore(FetchWord(), X, Y); cycles = 5; return true;
            case 0x9e: UnstableStore(FetchWord(), Y, X); cycles = 5; return true;
        }
        if ((opcode & 3) != 3) return false;
        cycles = CombinedNmos(opcode);
        return true;
    }

    private void Arr(byte operand)
    {
        var masked = (byte)(A & operand);
        var rotated = Nz((byte)((masked >> 1) | (P & Carry) << 7));
        Flag(Overflow, ((rotated ^ (rotated << 1)) & 0x40) != 0);
        if ((P & Decimal) == 0)
        {
            Flag(Carry, (rotated & 0x40) != 0);
            A = rotated;
            return;
        }
        A = rotated;
        if ((masked & 15) + (masked & 1) > 5) A = (byte)((A & 0xf0) | ((A + 6) & 15));
        var carry = (masked & 0xf0) + (masked & 0x10) > 0x50;
        if (carry) A = (byte)(A + 0x60);
        Flag(Carry, carry);
    }

    private void UnstableStore(ushort baseAddress, byte index, byte source)
    {
        var address = IndexedAddress(baseAddress, index, true);
        var value = (byte)(source & ((baseAddress >> C.HighByteShift) + 1));
        if (PageCross(baseAddress, index) != 0) address = (ushort)((value << C.HighByteShift) | (byte)address);
        Write(address, value);
    }

    private int CombinedNmos(byte opcode)
    {
        var operation = opcode >> 5;
        var mode = (opcode >> 2) & 7;
        if (operation == 4 && mode is (4 or 6 or 7))
        {
            if (mode == 6) S = (byte)(A & X);
            var baseAddress = mode == 4 ? ZeroPageWord(Fetch()) : FetchWord();
            UnstableStore(baseAddress, Y, mode == 6 ? S : (byte)(A & X));
            return mode == 4 ? 6 : 5;
        }
        ushort address;
        var extra = 0;
        var cycles = 0;
        var modify = operation is not (4 or 5);
        switch (mode)
        {
            case 0: address = IndexedIndirect(); cycles = modify ? 8 : 6; break;
            case 1: address = Fetch(); cycles = modify ? 5 : 3; break;
            case 3: address = FetchWord(); cycles = modify ? 6 : 4; break;
            case 4:
                var pointer = ZeroPageWord(Fetch());
                extra = modify ? 0 : PageCross(pointer, Y);
                address = IndexedAddress(pointer, Y, modify); cycles = modify ? 8 : 5; break;
            case 5: address = IndexedZeroPage(operation is 4 or 5 ? Y : X); cycles = modify ? 6 : 4; break;
            default:
                var absolute = FetchWord(); var index = operation == 5 || mode == 6 ? Y : X;
                extra = modify ? 0 : PageCross(absolute, index);
                address = IndexedAddress(absolute, index, modify); cycles = modify ? 7 : 4; break;
        }
        if (operation == 4) { Write(address, (byte)(A & X)); return cycles; }
        var value = Read(address);
        if (operation == 5)
        {
            if (mode == 6) A = X = S = Nz((byte)(value & S));
            else A = X = Nz(value);
            return cycles + extra;
        }
        var carry = P & Carry;
        byte result;
        switch (operation)
        {
            case 0: Flag(Carry, (value & Negative) != 0); result = (byte)(value << 1); break;
            case 1: Flag(Carry, (value & Negative) != 0); result = (byte)((value << 1) | carry); break;
            case 2: Flag(Carry, (value & Carry) != 0); result = (byte)(value >> 1); break;
            case 3: Flag(Carry, (value & Carry) != 0); result = (byte)((value >> 1) | carry << 7); break;
            case 6: result = (byte)(value - 1); break;
            default: result = (byte)(value + 1); break;
        }
        Write(address, value);
        Write(address, result);
        switch (operation)
        {
            case 0: A = Nz((byte)(A | result)); break;
            case 1: A = Nz((byte)(A & result)); break;
            case 2: A = Nz((byte)(A ^ result)); break;
            case 3: Add(result); break;
            case 6: Compare(A, result); break;
            case 7: Subtract(result); break;
        }
        return cycles;
    }
}
