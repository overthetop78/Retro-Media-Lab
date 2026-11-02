using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.Cpu6809Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class Cpu6809
{
    private ushort Address(int mode) => mode switch
    {
        1 => (ushort)(DP << C.ByteBits | Fetch()),
        2 => Indexed(),
        3 => FetchWord(),
        _ => throw new InvalidOperationException()
    };

    private ushort IndexRegister(int register) => register switch { 0 => X, 1 => Y, 2 => U, _ => S };
    private void SetIndexRegister(int register, ushort value)
    {
        switch (register) { case 0: X = value; break; case 1: Y = value; break; case 2: U = value; break; default: S = value; break; }
    }

    private ushort Indexed()
    {
        var post = Fetch();
        var register = post >> C.RegisterShift & C.RegisterMask;
        var basis = IndexRegister(register);
        if ((post & C.IndexedForm) == 0)
        {
            var offset = post & C.IndexedSmallMask;
            if ((offset & C.IndexedSmallSign) != 0) offset -= C.IndexedSmallMask + 1;
            _indexedCycles++;
            return (ushort)(basis + offset);
        }
        var indirect = (post & C.IndexedIndirect) != 0;
        ushort address;
        switch (post & C.NibbleMask)
        {
            case 0:
                if (indirect) throw Illegal(post);
                address = basis; SetIndexRegister(register, (ushort)(basis + 1)); _indexedCycles += 2; break;
            case 1:
                address = basis; SetIndexRegister(register, (ushort)(basis + 2)); _indexedCycles += 3; break;
            case 2:
                if (indirect) throw Illegal(post);
                address = (ushort)(basis - 1); SetIndexRegister(register, address); _indexedCycles += 2; break;
            case 3:
                address = (ushort)(basis - 2); SetIndexRegister(register, address); _indexedCycles += 3; break;
            case 4: address = basis; break;
            case 5: address = (ushort)(basis + (sbyte)B); _indexedCycles++; break;
            case 6: address = (ushort)(basis + (sbyte)A); _indexedCycles++; break;
            case 8: address = (ushort)(basis + (sbyte)Fetch()); _indexedCycles++; break;
            case 9: address = (ushort)(basis + (short)FetchWord()); _indexedCycles += 4; break;
            case 11: address = (ushort)(basis + (short)D); _indexedCycles += 4; break;
            case 12:
                var byteOffset = (sbyte)Fetch(); address = (ushort)(PC + byteOffset); _indexedCycles++; break;
            case 13:
                var wordOffset = (short)FetchWord(); address = (ushort)(PC + wordOffset); _indexedCycles += 5; break;
            case 15:
                if (!indirect) throw Illegal(post);
                address = FetchWord(); _indexedCycles += 2; break;
            default: throw Illegal(post);
        }
        if (indirect) { address = ReadWord(address); _indexedCycles += 3; }
        return address;
    }

    private void PushByte(ref ushort stack, byte value) => Write(--stack, value);
    private void PushWord(ref ushort stack, ushort value) { PushByte(ref stack, (byte)value); PushByte(ref stack, (byte)(value >> C.ByteBits)); }
    private byte PullByte(ref ushort stack) => Read(stack++);
    private ushort PullWord(ref ushort stack) { var high = PullByte(ref stack); return (ushort)(high << C.ByteBits | PullByte(ref stack)); }

    private int PushRegisters(byte mask, bool user)
    {
        var stack = user ? U : S;
        var count = 0;
        if ((mask & 0x80) != 0) { PushWord(ref stack, PC); count += 2; }
        if ((mask & 0x40) != 0) { PushWord(ref stack, user ? S : U); count += 2; }
        if ((mask & 0x20) != 0) { PushWord(ref stack, Y); count += 2; }
        if ((mask & 0x10) != 0) { PushWord(ref stack, X); count += 2; }
        if ((mask & 0x08) != 0) { PushByte(ref stack, DP); count++; }
        if ((mask & 0x04) != 0) { PushByte(ref stack, B); count++; }
        if ((mask & 0x02) != 0) { PushByte(ref stack, A); count++; }
        if ((mask & 0x01) != 0) { PushByte(ref stack, CC); count++; }
        if (user) U = stack; else S = stack;
        return count;
    }

    private int PullRegisters(byte mask, bool user)
    {
        var stack = user ? U : S;
        var count = 0;
        if ((mask & 0x01) != 0) { CC = PullByte(ref stack); count++; }
        if ((mask & 0x02) != 0) { A = PullByte(ref stack); count++; }
        if ((mask & 0x04) != 0) { B = PullByte(ref stack); count++; }
        if ((mask & 0x08) != 0) { DP = PullByte(ref stack); count++; }
        if ((mask & 0x10) != 0) { X = PullWord(ref stack); count += 2; }
        if ((mask & 0x20) != 0) { Y = PullWord(ref stack); count += 2; }
        if ((mask & 0x40) != 0) { var value = PullWord(ref stack); if (user) S = value; else U = value; count += 2; }
        if ((mask & 0x80) != 0) { PC = PullWord(ref stack); count += 2; }
        if (user) U = stack; else S = stack;
        return count;
    }
}
