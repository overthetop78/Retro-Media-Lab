using C = RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Constants.Cpu6809Constants;

namespace RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Services;

public sealed partial class Cpu6809
:RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Interfaces.ICpu6809{
    private ushort Address(int mode)
    {
        if (mode == 2) return Indexed();
        var address = mode switch
        {
            1 => (ushort)(DP << C.ByteBits | Fetch()),
            3 => FetchWord(),
            _ => throw new InvalidOperationException()
        };
        Idle();
        return address;
    }

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
            Read(PC);
            Idle();
            return (ushort)(basis + offset);
        }
        var indirect = (post & C.IndexedIndirect) != 0;
        ushort address;
        switch (post & C.NibbleMask)
        {
            case 0:
                address = basis; SetIndexRegister(register, (ushort)(basis + 1)); _indexedCycles += 2; Read(PC); Idle(2); break;
            case 1:
                address = basis; SetIndexRegister(register, (ushort)(basis + 2)); _indexedCycles += 3; Read(PC); Idle(3); break;
            case 2:
                address = (ushort)(basis - 1); SetIndexRegister(register, address); _indexedCycles += 2; Read(PC); Idle(2); break;
            case 3:
                address = (ushort)(basis - 2); SetIndexRegister(register, address); _indexedCycles += 3; Read(PC); Idle(3); break;
            case 4: address = basis; Read(PC); break;
            case 5: address = (ushort)(basis + (sbyte)B); _indexedCycles++; Read(PC); Idle(); break;
            case 6: address = (ushort)(basis + (sbyte)A); _indexedCycles++; Read(PC); Idle(); break;
            case 8: address = (ushort)(basis + (sbyte)Fetch()); _indexedCycles++; Read(PC); break;
            case 9: address = (ushort)(basis + (short)FetchWord()); _indexedCycles += 4; Idle(3); break;
            case 11: address = (ushort)(basis + (short)D); _indexedCycles += 4; Read(PC); Read((ushort)(PC + 1)); Idle(3); break;
            case 12:
                var byteOffset = (sbyte)Fetch(); address = (ushort)(PC + byteOffset); _indexedCycles++; Idle(); break;
            case 13:
                var wordOffset = (short)FetchWord(); address = (ushort)(PC + wordOffset); _indexedCycles += 5; Idle(4); break;
            case 15:
                address = FetchWord(); _indexedCycles += 2; Idle(); break;
            default: address = 0; break;
        }
        if (indirect) { address = ReadWord(address); Idle(); _indexedCycles += 3; }
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

    private int StackInstruction(bool pull, bool user)
    {
        var mask = Fetch();
        Idle(C.StackAddressCycles);
        int count;
        if (pull)
        {
            count = PullRegisters(mask, user);
            Read(user ? U : S);
        }
        else
        {
            Read(user ? U : S);
            count = PushRegisters(mask, user);
        }
        return C.StackInstructionBaseCycles + count;
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
        if ((mask & 0x40) != 0) { var value = PullWord(ref stack); if (user) { S = value; _stackInitialized = true; } else U = value; count += 2; }
        if ((mask & 0x80) != 0) { PC = PullWord(ref stack); count += 2; }
        if (user) U = stack; else S = stack;
        return count;
    }
}
