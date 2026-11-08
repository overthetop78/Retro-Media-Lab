using C = RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Constants.Cpu6809Constants;

namespace RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Services;

public sealed partial class Cpu6809
{
    // Physical MC6809 behaviour measured by the 6809Decoder project. These
    // opcodes do not trap; immediate stores retain an ALU dependency on the
    // preceding instruction that cannot be inferred from CC alone.
    private int StoreImmediate(byte opcode, int page)
    {
        Fetch();
        var word = (opcode & C.NibbleMask) == C.NibbleMask;
        var register = opcode < 0xc0
            ? page == C.FirstPrefix ? Y : X
            : page == C.FirstPrefix ? S : U;
        if (word) Write(PC++, (byte)register);
        var flags = page == 0 ? _immediateStoreFlags : C.Negative;
        if (word)
        {
            flags &= unchecked((byte)~C.Overflow);
            if ((byte)register != 0) flags &= unchecked((byte)~C.Zero);
        }
        CC = (byte)((CC & ~C.NzvMask) | flags);
        return (word ? 3 : 2) + (page == 0 ? 0 : 1);
    }

    private int UndocumentedInterrupt(int page)
    {
        Read(PC);
        Idle();
        PushRegisters(byte.MaxValue, false);
        Idle();
        var vector = page switch
        {
            C.FirstPrefix => C.SwiTwoVector,
            C.SecondPrefix => C.FirqVector,
            _ => C.ResetVector
        };
        PC = ReadVector(vector);
        return page == 0 ? 19 : 20;
    }

    private static byte StoreFlags(byte value, bool overflow = false) =>
        (byte)(((value & C.ByteSign) != 0 ? C.Negative : 0) |
            (value == 0 ? C.Zero : 0) | (overflow ? C.Overflow : 0));

    private static byte DecrementStoreFlags(byte value) =>
        StoreFlags((byte)(value - 1), value == C.ByteSign);

    private void RememberStoreFlags(byte opcode, int page)
    {
        _immediateStoreFlags = C.Negative;
        var operation = opcode & C.NibbleMask;
        if (opcode is >= 0x30 and <= 0x33)
        {
            var address = opcode switch { 0x30 => X, 0x31 => Y, 0x32 => S, _ => U };
            _immediateStoreFlags = DecrementStoreFlags((byte)((address >> C.ByteBits) | address));
        }
        else if ((opcode is >= 0x80 and <= 0xbf && operation == 0x0c) ||
            (page != 0 && operation == 3 && opcode >= 0x80))
            _immediateStoreFlags = DecrementStoreFlags((byte)(_wordResult >> C.ByteBits));
        else if (opcode is >= 0x80 and <= 0xbf && operation is 1 or 5)
            _immediateStoreFlags = C.Zero;
        else if ((opcode is >= 0x80 and <= 0xbf && operation == 6) ||
            (opcode >= 0xc0 && operation == 0x0c))
            _immediateStoreFlags = StoreFlags(A);
        else if (opcode is >= 0x80 and <= 0xbf && operation is 0 or 2 or 4 or 8 or 9 or 0x0a or 0x0b)
            _immediateStoreFlags = StoreFlags((byte)~A);
        else if ((opcode is >= 0x40 and <= 0x4d) || opcode == 0x19 ||
            (page == 0 && opcode is >= 0x80 and <= 0xbf && operation == 3))
            _immediateStoreFlags = DecrementStoreFlags(A);
        else if (opcode == 0x1d)
            _immediateStoreFlags = DecrementStoreFlags(B);
    }
}
