using T = RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Cpu80186Timings;
using static RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Cpu80186Constants;

namespace RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services;

public sealed partial class Cpu80186
{
    private int Execute(byte opcode)
    {
        if (opcode < 0x40 && (opcode & 7) < 6) return ArithmeticInstruction(opcode);
        if (opcode is >= 0x40 and <= 0x4f) { var registerIndex = opcode & 7; _registers[registerIndex] = (ushort)Increment(_registers[registerIndex], (opcode & 8) != 0, true); return T.RegisterAlu; }
        if (opcode is >= 0x50 and <= 0x57) { PushRegister(opcode & RegisterMask); return T.RegisterPush; }
        if (opcode is >= 0x58 and <= 0x5f) { _registers[opcode & 7] = Pop(); return T.RegisterPop; }
        if (opcode is >= 0x70 and <= 0x7f) { var displacement = (sbyte)Fetch(); if (Condition(opcode & 15)) { TransferControl((ushort)(IP + displacement)); return T.ConditionalJumpTaken; } return T.ConditionalJumpNotTaken; }
        if (opcode is >= 0x90 and <= 0x97) { var registerIndex = opcode & 7; (_registers[Ax], _registers[registerIndex]) = (_registers[registerIndex], _registers[Ax]); return T.AccumulatorExchange; }
        if (opcode is >= 0xb0 and <= 0xb7) { SetByteRegister(opcode & 7, Fetch()); return T.ByteImmediateMove; }
        if (opcode is >= 0xb8 and <= 0xbf) { _registers[opcode & 7] = FetchWord(); return T.WordImmediateMove; }
        byte modrm; Operand operand; ushort offset; int value; int source; int index; bool word;
        switch (opcode)
        {
            case 0x06: case 0x0e: case 0x16: case 0x1e: Push(_segments[(opcode >> 3) & 3]); return T.SegmentPush;
            case 0x07: case 0x17: case 0x1f:
                index = (opcode >> 3) & 3; _segments[index] = Pop();
                _interruptDelay = 1; _suppressSingleStep = true; return T.SegmentPop;
            case 0x27: case 0x2f: DecimalAdjust(opcode == 0x2f); return T.DecimalAdjust;
            case 0x37: case 0x3f: AsciiAdjust(opcode == 0x3f); return opcode == 0x3f ? T.AsciiSubtractAdjust : T.AsciiAddAdjust;
            case 0x60:
                var stack = _registers[Sp];
                for (var i = 0; i < 8; i++) Push(i == Sp ? stack : _registers[i]); return T.PushAll;
            case 0x61:
                for (var i = 7; i >= 0; i--) { var popped = Pop(); if (i != Sp) _registers[i] = popped; } return T.PopAll;
            case 0x62:
                modrm = Fetch(); operand = Decode(modrm);
                if (operand.Register) { Interrupt(InvalidOpcodeVector); return T.ExceptionDecode; }
                var bound = (short)_registers[(modrm >> 3) & 7];
                var lowerBound = (short)ReadWord(operand.Segment, operand.Offset);
                var upperBound = (short)ReadWord(operand.Segment, operand.Offset, 2);
                if (bound < lowerBound || bound > upperBound) Interrupt(BoundsVector);
                return T.Bounds;
            case 0x68: Push(FetchWord()); return T.RegisterPush;
            case 0x6a: Push((ushort)(short)(sbyte)Fetch()); return T.RegisterPush;
            case 0x69: case 0x6b:
                modrm = Fetch(); operand = Decode(modrm); value = (short)Load(operand, true); source = opcode == 0x69 ? (short)FetchWord() : (sbyte)Fetch();
                var product = value * source; _registers[(modrm >> 3) & 7] = (ushort)product;
                Flag(Carry, product != (short)product); Flag(Overflow, Has(Carry));
                return (opcode == 0x69 ? T.ImmediateMultiplyWord : T.ImmediateMultiplyByte) + (operand.Register ? 0 : T.ImmediateMultiplyMemoryOperand);
            case 0x6c: case 0x6d: case 0x6e: case 0x6f:
            case 0xa4: case 0xa5: case 0xa6: case 0xa7: case 0xaa: case 0xab: case 0xac: case 0xad: case 0xae: case 0xaf:
                return StringInstruction(opcode);
            case 0x80: case 0x81: case 0x82: case 0x83:
                word = (opcode & 1) != 0; modrm = Fetch(); operand = Decode(modrm);
                source = opcode == 0x83 ? (ushort)(short)(sbyte)Fetch() : word ? FetchWord() : Fetch();
                value = Alu((modrm >> 3) & 7, Load(operand, word), source, word);
                if (((modrm >> 3) & 7) != 7) Store(operand, word, value);
                return operand.Register ? T.ImmediateAlu : ((modrm >> 3) & 7) == 7 ? T.MemoryAlu : T.ImmediateMemoryAlu;
            case 0x84: case 0x85:
                word = (opcode & 1) != 0; modrm = Fetch(); operand = Decode(modrm); index = (modrm >> 3) & 7;
                Alu(4, Load(operand, word), word ? _registers[index] : ByteRegister(index), word); return operand.Register ? T.RegisterAlu : T.MemoryAlu;
            case 0x86: case 0x87:
                word = (opcode & 1) != 0; modrm = Fetch(); operand = Decode(modrm); index = (modrm >> 3) & 7;
                value = Load(operand, word); Store(operand, word, word ? _registers[index] : ByteRegister(index));
                if (word) _registers[index] = (ushort)value; else SetByteRegister(index, value); return operand.Register ? T.RegisterExchange : T.MemoryExchange;
            case 0x88: case 0x89: case 0x8a: case 0x8b:
                word = (opcode & 1) != 0; modrm = Fetch(); operand = Decode(modrm); index = (modrm >> 3) & 7;
                if ((opcode & 2) == 0) Store(operand, word, word ? _registers[index] : ByteRegister(index));
                else { value = Load(operand, word); if (word) _registers[index] = (ushort)value; else SetByteRegister(index, value); } return operand.Register ? T.RegisterMove : (opcode & 2) != 0 ? T.MemoryToRegisterMove : T.RegisterToMemoryMove;
            case 0x8c: case 0x8e:
                modrm = Fetch(); operand = Decode(modrm); index = (modrm >> 3) & 7;
                if (index >= _segments.Length || opcode == 0x8e && index == Cs) { Interrupt(InvalidOpcodeVector); return T.ExceptionDecode; }
                if (opcode == 0x8c) Store(operand, true, _segments[index]);
                else { _segments[index] = (ushort)Load(operand, true); _interruptDelay = 1; _suppressSingleStep = true; } return operand.Register ? T.RegisterMove : opcode == 0x8c ? T.SegmentToMemoryMove : T.MemoryToRegisterMove;
            case 0x8d:
                modrm = Fetch(); operand = Decode(modrm);
                if (operand.Register) Interrupt(InvalidOpcodeVector); else _registers[(modrm >> 3) & 7] = operand.Offset; return T.EffectiveAddress;
            case 0x8f:
                modrm = Fetch(); operand = Decode(modrm);
                if (((modrm >> ModRmOperationShift) & RegisterMask) != 0) { Interrupt(InvalidOpcodeVector); return T.ExceptionDecode; }
                Store(operand, true, Pop()); return operand.Register ? T.RegisterPop : T.MemoryPop;
            case 0x98: _registers[Ax] = (ushort)(short)(sbyte)ByteRegister(Ax); return T.ByteToWord;
            case 0x99: _registers[Dx] = (short)_registers[Ax] < 0 ? ushort.MaxValue : (ushort)0; return T.WordToDoubleWord;
            case 0x9a:
                offset = FetchWord(); var segment = FetchWord(); Push(_segments[Cs]); Push(IP); TransferControl(offset, segment); return T.FarCall;
            case 0x9b:
                _suppressSingleStep = true;
                if (CoprocessorBusy) { _waiting = true; IP = _start; }
                else _interruptDelay = 1;
                return T.Wait;
            case 0x9c: Push((ushort)(_flags | ReservedHigh)); return T.PushFlags;
            case 0x9d: _flags = (ushort)((Pop() & 0x0fd5) | Reserved); return T.PopFlags;
            case 0x9e: _flags = (ushort)((_flags & 0xff00) | (ByteRegister(4) & 0xd5) | Reserved); return T.StoreFlags;
            case 0x9f: SetByteRegister(4, _flags); return T.LoadFlags;
            case 0xa0: case 0xa1: case 0xa2: case 0xa3:
                word = (opcode & 1) != 0; offset = FetchWord(); operand = new(false, 0, _segmentOverride >= 0 ? _segmentOverride : Ds, offset);
                if ((opcode & 2) != 0) Store(operand, word, word ? _registers[Ax] : ByteRegister(Ax));
                else { value = Load(operand, word); if (word) _registers[Ax] = (ushort)value; else SetByteRegister(Ax, value); } return (opcode & 2) != 0 ? T.AccumulatorStore : T.AccumulatorLoad;
            case 0xa8: case 0xa9: word = (opcode & 1) != 0; Alu(4, word ? _registers[Ax] : ByteRegister(Ax), word ? FetchWord() : Fetch(), word); return word ? T.ImmediateAlu : T.ByteAccumulatorAlu;
            case 0xc0: case 0xc1: case 0xd0: case 0xd1: case 0xd2: case 0xd3:
                word = (opcode & 1) != 0; modrm = Fetch(); operand = Decode(modrm);
                var count = opcode < 0xd0 ? Fetch() : opcode < 0xd2 ? 1 : ByteRegister(Cx);
                value = Load(operand, word);
                count &= ShiftCountMask;
                if (count != 0) Store(operand, word, Shift((modrm >> 3) & 7, value, count, word)); return opcode is 0xd0 or 0xd1 ? operand.Register ? T.RegisterShiftOne : T.MemoryShiftOne : (operand.Register ? T.RegisterShiftBase : T.MemoryShiftBase) + count;
            case 0xc2: offset = FetchWord(); TransferControl(Pop()); _registers[Sp] += offset; return T.ReturnNearImmediate;
            case 0xc3: TransferControl(Pop()); return T.ReturnNear;
            case 0xc4: case 0xc5:
                modrm = Fetch(); operand = Decode(modrm);
                if (operand.Register) { Interrupt(InvalidOpcodeVector); return T.ExceptionDecode; }
                _registers[(modrm >> 3) & 7] = ReadWord(operand.Segment, operand.Offset);
                _segments[opcode == 0xc4 ? Es : Ds] = ReadWord(operand.Segment, operand.Offset, 2); return T.PointerLoad;
            case 0xc6: case 0xc7:
                word = (opcode & 1) != 0; modrm = Fetch(); operand = Decode(modrm);
                if (((modrm >> ModRmOperationShift) & RegisterMask) != 0) { Interrupt(InvalidOpcodeVector); return T.ExceptionDecode; }
                Store(operand, word, word ? FetchWord() : Fetch()); return operand.Register ? word ? T.WordImmediateMove : T.ByteImmediateMove : word ? T.WordImmediateMemoryMove : T.ByteImmediateMemoryMove;
            case 0xc8:
                var allocation = FetchWord(); var nesting = Fetch() & 31;
                Push(_registers[Bp]); var frame = _registers[Sp];
                if (nesting > 0) { for (var i = 1; i < nesting; i++) { _registers[Bp] -= 2; Push(ReadWord(Ss, _registers[Bp])); } Push(frame); }
                _registers[Bp] = frame; _registers[Sp] -= allocation; return nesting == 0 ? T.EnterZero : nesting == 1 ? T.EnterOne : T.EnterNestedBase + T.EnterNestedLevel * (nesting - 1);
            case 0xc9: _registers[Sp] = _registers[Bp]; _registers[Bp] = Pop(); return T.Leave;
            case 0xca:
                var stackAdjustment = FetchWord();
                offset = Pop();
                TransferControl(offset, Pop());
                _registers[Sp] += stackAdjustment;
                return T.ReturnFarImmediate;
            case 0xcb: offset = Pop(); TransferControl(offset, Pop()); return T.ReturnFar;
            case 0xcc: Interrupt(3); return 0;
            case 0xcd: Interrupt(Fetch()); return T.InterruptImmediate;
            case 0xce: if (!Has(Overflow)) return T.OverflowInterruptNotTaken; Interrupt(4); return T.OverflowInterruptTaken;
            case 0xcf:
                offset = Pop();
                TransferControl(offset, Pop());
                _flags = (ushort)((Pop() & 0x0fd5) | Reserved);
                _interruptStatus &= unchecked((ushort)~Constants.Interrupt80186Constants.DmaHalt);
                return T.ReturnInterrupt;
            case 0xd4:
                var radix = Fetch(); if (radix == 0) { Interrupt(DivideVector); return T.ExceptionDecode; }
                value = ByteRegister(Ax); SetByteRegister(4, value / radix); SetByteRegister(Ax, SetResult(value % radix, false)); return T.AsciiMultiplyAdjust;
            case 0xd5: value = ByteRegister(Ax) + ByteRegister(4) * Fetch(); _registers[Ax] = (ushort)SetResult(value, false); return T.AsciiDivideAdjust;
            case 0xd6: SetByteRegister(Ax, Has(Carry) ? 255 : 0); return T.SetCarryAccumulator;
            case 0xd7: SetByteRegister(Ax, ReadByte(_segmentOverride >= 0 ? _segmentOverride : Ds, (ushort)(_registers[Bx] + ByteRegister(Ax)))); return T.Translate;
            case >= 0xd8 and <= 0xdf: return EscapeInstruction();
            case 0xe0: case 0xe1: case 0xe2: case 0xe3:
                var displacement = (sbyte)Fetch();
                var taken = opcode == 0xe3 ? _registers[Cx] == 0 : --_registers[Cx] != 0 && (opcode == 0xe2 || Has(Zero) == (opcode == 0xe1));
                if (taken) TransferControl((ushort)(IP + displacement)); return opcode == 0xe3 ? taken ? T.CountZeroTaken : T.CountZeroNotTaken : taken ? T.LoopTaken : T.LoopNotTaken;
            case 0xe4: case 0xe5: case 0xec: case 0xed:
                var port = opcode < 0xec ? Fetch() : _registers[Dx];
                if ((opcode & 1) != 0) _registers[Ax] = ReadIoWord(port);
                else SetByteRegister(Ax, ReadIoByte(port));
                return opcode < 0xec ? T.ImmediateInput : T.RegisterInput;
            case 0xe6: case 0xe7: case 0xee: case 0xef:
                var outPort = opcode < 0xee ? Fetch() : _registers[Dx];
                if ((opcode & 1) != 0) WriteIoWord(outPort, _registers[Ax]);
                else WriteIoAccumulatorByte(outPort);
                return opcode < 0xee ? T.ImmediateOutput : T.RegisterOutput;
            case 0xe8: offset = FetchWord(); var next = IP; Push(next); TransferControl((ushort)(next + (short)offset)); return T.NearCall;
            case 0xe9: offset = FetchWord(); TransferControl((ushort)(IP + (short)offset)); return T.Jump;
            case 0xea: offset = FetchWord(); TransferControl(offset, FetchWord()); return T.Jump;
            case 0xeb: var relative = (sbyte)Fetch(); TransferControl((ushort)(IP + relative)); return T.Jump;
            case 0xf4: _halted = true; return T.Halt;
            case 0xf5: Flag(Carry, !Has(Carry)); return T.FlagOperation;
            case 0xf6: case 0xf7:
                word = (opcode & 1) != 0; modrm = Fetch(); operand = Decode(modrm); value = Load(operand, word); index = (modrm >> 3) & 7;
                if (index <= 1) Alu(4, value, word ? FetchWord() : Fetch(), word);
                else if (index == 2) Store(operand, word, ~value);
                else if (index == 3) Store(operand, word, Alu(5, 0, value, word));
                else MultiplyDivide(index, value, word);
                return index >= 4 ? MultiplyDivideCycles(index, word, operand.Register) : operand.Register ? index <= 1 ? T.ImmediateAlu : T.RegisterUnary : T.MemoryUnary;
            case 0xf8: Flag(Carry, false); return T.FlagOperation;
            case 0xf9: Flag(Carry, true); return T.FlagOperation;
            case 0xfa: Flag(InterruptEnable, false); return T.FlagOperation;
            case 0xfb: Flag(InterruptEnable, true); _interruptDelay = 1; return T.FlagOperation;
            case 0xfc: Flag(Direction, false); return T.FlagOperation;
            case 0xfd: Flag(Direction, true); return T.FlagOperation;
            case 0xfe: case 0xff: return GroupFive(opcode);
            default: Interrupt(InvalidOpcodeVector); return T.ExceptionDecode;
        }
    }
    private int ArithmeticInstruction(byte opcode)
    {
        var operation = opcode >> 3; var form = opcode & 7; var word = (form & 1) != 0;
        if (form >= 4)
        {
            var result = Alu(operation, word ? _registers[Ax] : ByteRegister(Ax), word ? FetchWord() : Fetch(), word);
            if (operation != 7) { if (word) _registers[Ax] = (ushort)result; else SetByteRegister(Ax, result); }
            return word ? T.ImmediateAlu : T.ByteAccumulatorAlu;
        }
        var modrm = Fetch(); var operand = Decode(modrm); var register = (modrm >> 3) & 7;
        var left = Load(operand, word); var right = word ? _registers[register] : ByteRegister(register);
        var toRegister = (form & 2) != 0;
        var value = toRegister ? Alu(operation, right, left, word) : Alu(operation, left, right, word);
        if (operation != 7)
        {
            if (!toRegister) Store(operand, word, value);
            else if (word) _registers[register] = (ushort)value; else SetByteRegister(register, value);
        }
        return operand.Register ? T.RegisterAlu : T.MemoryAlu;
    }
    private int GroupFive(byte opcode)
    {
        var word = opcode == 0xff; var modrm = Fetch(); var operand = Decode(modrm); var operation = (modrm >> 3) & 7;
        var value = Load(operand, word);
        if (operation <= 1) Store(operand, word, Increment(value, operation == 1, word));
        else if (!word || operation == 7) Interrupt(InvalidOpcodeVector);
        else if (operation == 2) { Push(IP); TransferControl((ushort)value); }
        else if (operation is 3 or 5)
        {
            if (operand.Register) { Interrupt(InvalidOpcodeVector); return T.ExceptionDecode; }
            var segment = ReadWord(operand.Segment, operand.Offset, 2);
            if (operation == 3) { Push(_segments[Cs]); Push(IP); }
            TransferControl((ushort)value, segment);
        }
        else if (operation == 4) TransferControl((ushort)value);
        else if (operation == 6)
        {
            if (operand.Register) PushRegister(operand.Index);
            else Push((ushort)value);
        }
        return operation switch
        {
            0 or 1 => operand.Register ? T.RegisterAlu : T.MemoryIncrement,
            2 => operand.Register ? T.RegisterCall : T.MemoryNearCall,
            3 => T.MemoryFarCall,
            4 => operand.Register ? T.RegisterJump : T.MemoryNearJump,
            5 => T.MemoryFarJump,
            6 => operand.Register ? T.RegisterPush : T.MemoryPush,
            _ => 0
        };
    }

    private static int MultiplyDivideCycles(int operation, bool word, bool register)
    {
        var cycles = operation switch
        {
            4 => word ? T.MultiplyWord : T.MultiplyByte,
            5 => word ? T.SignedMultiplyWord : T.SignedMultiplyByte,
            6 => word ? T.DivideWord : T.DivideByte,
            _ => word ? T.SignedDivideWord : T.SignedDivideByte
        };
        return cycles + (register ? 0 : T.ArithmeticMemoryOperand);
    }
}
