using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.Intrinsics.X86;
using RetroMediaLab.Chips.Common.Compilation.X64;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;

namespace RetroMediaLab.Chips.CPU.ARM.ARMv3.ARM7;

using static X64Register;
using static X64Width;

#if !ARM7_DYNAREC_NONE
internal static unsafe partial class Arm7DiRecompiler
{
    private static nuint _armDispatch;
    private static nuint _dispatchEntryPoints;
    private static readonly Sh4UnwindInfo X64Unwinder = new();
    private static readonly X64Register[] X64CallRegisters = OperatingSystem.IsWindows()
        ? [Rcx, Rdx, R8, R9] : [Rdi, Rsi, Rdx, Rcx];
    private static readonly X64Register[] X64AllocationRegisters = OperatingSystem.IsWindows()
        ? [Rbx, Rbp, Rdi, Rsi, R12, R13, R14, R15] : [Rbx, Rbp, R12, R13, R14, R15];

    private sealed class X64ArmRegAlloc(X64ArmCompiler assembler, IReadOnlyList<ArmOp> blockOperations)
        : ArmRegAlloc(X64AllocationRegisters.Length, blockOperations)
    {
        protected override void LoadReg(int hostRegister, Arm7Register register) =>
            assembler.LoadArmRegister(GetReg32(hostRegister), register);

        protected override void StoreReg(int hostRegister, Arm7Register register) =>
            assembler.StoreArmRegister(GetReg32(hostRegister), register);

        private static X64Register GetReg32(int index)
        {
            Sh4HostRuntime.Verify(index >= 0 && (uint)index < X64AllocationRegisters.Length,
                "i >= 0 && (u32)i < alloc_regs.size()");
            return X64AllocationRegisters[index];
        }

        internal X64Register MapReg(Arm7Register register) => GetReg32(Map(register));
    }

    private sealed class X64ArmCompiler
    {
        private bool _logicalOperationSetsFlags;
        private bool _setCarryBit;
        private bool _setFlags;
        private X64ArmRegAlloc? _regAlloc;

        private const uint NegativeFlag = 1u << 31;
        private const uint ZeroFlag = 1u << 30;
        private const uint CarryFlag = 1u << 29;
        private const uint OverflowFlag = 1u << 28;

        private readonly nuint _code = (nuint)CurrentCode();
        private readonly X64Emitter _emitter = new();

        private X64Register? GetOperand(ArmOp.Operand argument, X64Register scratch)
        {
            X64Register register;
            if (!argument.IsReg())
            {
                if (argument.IsNone() || argument.ShiftImmediate)
                    return null;
                Move(scratch, argument.GetImmediate());
                register = scratch;
            }
            else
                register = _regAlloc!.MapReg(argument.GetReg().ArmReg);

            if (argument.IsShifted())
            {
                if (register != scratch)
                {
                    Move(scratch, register);
                    register = scratch;
                }

                if (argument.ShiftImmediate)
                {
                    if (argument.ShiftType != ArmOp.ShiftOp.ROR && argument.ShiftValue != 0 && !_logicalOperationSetsFlags)
                    {
                        switch (argument.ShiftType)
                        {
                            case ArmOp.ShiftOp.LSL:
                                Shift(X64Shift.Left, register, argument.ShiftValue);
                                break;
                            case ArmOp.ShiftOp.LSR:
                                Shift(X64Shift.Right, register, argument.ShiftValue);
                                break;
                            case ArmOp.ShiftOp.ASR:
                                Shift(X64Shift.ArithmeticRight, register, argument.ShiftValue);
                                break;
                            default:
                                Sh4HostRuntime.Die("invalid");
                                break;
                        }
                    }
                    else if (argument.ShiftValue == 0)
                    {
                        if (_logicalOperationSetsFlags)
                            _setCarryBit = true;
                        if (argument.ShiftType == ArmOp.ShiftOp.LSR)
                        {
                            if (_setCarryBit)
                            {
                                Move(R10, register);
                                Shift(X64Shift.Right, R10, 31);
                            }
                            Move(register, 0u);
                        }
                        else if (argument.ShiftType == ArmOp.ShiftOp.ASR)
                        {
                            if (_setCarryBit)
                            {
                                Move(R10, register);
                                Shift(X64Shift.Right, R10, 31);
                            }
                            Shift(X64Shift.ArithmeticRight, register, 31);
                        }
                        else if (argument.ShiftType == ArmOp.ShiftOp.ROR)
                        {
                            LoadArmRegister(R10, Arm7Register.RN_PSR_FLAGS);
                            Shift(X64Shift.Left, R10, 2);
                            Sh4HostRuntime.Verify(register != Rax, "r != eax");
                            Move(Rax, register);
                            Binary(X64Arithmetic.And, R10, 0x80000000u);
                            Shift(X64Shift.Right, Rax, 1);
                            Binary(X64Arithmetic.Or, Rax, R10);
                            if (_setCarryBit)
                            {
                                Move(R10, register);
                                Binary(X64Arithmetic.And, R10, 1u);
                            }
                            Move(register, Rax);
                        }
                        else
                            Sh4HostRuntime.Die("Invalid shift");
                    }
                    else
                    {
                        if (_logicalOperationSetsFlags)
                            _setCarryBit = true;
                        if (argument.ShiftType == ArmOp.ShiftOp.LSL)
                        {
                            if (_setCarryBit)
                                Move(R10, register);
                            if (_setCarryBit)
                                Shift(X64Shift.Right, R10, 32 - argument.ShiftValue);
                            Shift(X64Shift.Left, register, argument.ShiftValue);
                            if (_setCarryBit)
                                Binary(X64Arithmetic.And, R10, 1u);
                        }
                        else
                        {
                            if (_setCarryBit)
                            {
                                Move(R10, register);
                                Shift(X64Shift.Right, R10, argument.ShiftValue - 1);
                                Binary(X64Arithmetic.And, R10, 1u);
                            }

                            if (argument.ShiftType == ArmOp.ShiftOp.LSR)
                                Shift(X64Shift.Right, register, argument.ShiftValue);
                            else if (argument.ShiftType == ArmOp.ShiftOp.ASR)
                                Shift(X64Shift.ArithmeticRight, register, argument.ShiftValue);
                            else if (argument.ShiftType == ArmOp.ShiftOp.ROR)
                                Shift(X64Shift.RotateRight, register, argument.ShiftValue);
                            else
                                Sh4HostRuntime.Die("Invalid shift");
                        }
                    }
                }
                else
                {
                    X64Register shiftRegister = _regAlloc!.MapReg(argument.ShiftRegister.ArmReg);
                    switch (argument.ShiftType)
                    {
                        case ArmOp.ShiftOp.LSL:
                        case ArmOp.ShiftOp.LSR:
                            Move(Rcx, shiftRegister);
                            Move(Rax, 0u);
                            if (argument.ShiftType == ArmOp.ShiftOp.LSL)
                                _emitter.ShiftByCl(X64Shift.Left, register, Dword);
                            else
                                _emitter.ShiftByCl(X64Shift.Right, register, Dword);
                            Binary(X64Arithmetic.Compare, shiftRegister, 32u);
                            ConditionalMove(X64Condition.NoCarry, register, Rax);
                            break;
                        case ArmOp.ShiftOp.ASR:
                            Move(Rcx, shiftRegister);
                            Move(Rax, register);
                            Shift(X64Shift.ArithmeticRight, Rax, 31);
                            _emitter.ShiftByCl(X64Shift.ArithmeticRight, register, Dword);
                            Binary(X64Arithmetic.Compare, shiftRegister, 32u);
                            ConditionalMove(X64Condition.NoCarry, register, Rax);
                            break;
                        case ArmOp.ShiftOp.ROR:
                            Move(Rcx, shiftRegister);
                            _emitter.ShiftByCl(X64Shift.RotateRight, register, Dword);
                            break;
                        default:
                            Sh4HostRuntime.Die("Invalid shift");
                            break;
                    }
                }
            }
            return register;
        }

        private int? StartConditional(ArmOp.Condition condition)
        {
            if (condition == ArmOp.Condition.AL)
                return null;

            condition = (ArmOp.Condition)((uint)condition ^ 1);
            LoadArmRegister(Rax, Arm7Register.RN_PSR_FLAGS);
            switch (condition)
            {
                case ArmOp.Condition.EQ:
                    Binary(X64Arithmetic.And, Rax, ZeroFlag);
                    return _emitter.ReserveJump(X64Condition.NotZero);
                case ArmOp.Condition.NE:
                    Binary(X64Arithmetic.And, Rax, ZeroFlag);
                    return _emitter.ReserveJump(X64Condition.Zero);
                case ArmOp.Condition.CS:
                    Binary(X64Arithmetic.And, Rax, CarryFlag);
                    return _emitter.ReserveJump(X64Condition.NotZero);
                case ArmOp.Condition.CC:
                    Binary(X64Arithmetic.And, Rax, CarryFlag);
                    return _emitter.ReserveJump(X64Condition.Zero);
                case ArmOp.Condition.MI:
                    Binary(X64Arithmetic.And, Rax, NegativeFlag);
                    return _emitter.ReserveJump(X64Condition.NotZero);
                case ArmOp.Condition.PL:
                    Binary(X64Arithmetic.And, Rax, NegativeFlag);
                    return _emitter.ReserveJump(X64Condition.Zero);
                case ArmOp.Condition.VS:
                    Binary(X64Arithmetic.And, Rax, OverflowFlag);
                    return _emitter.ReserveJump(X64Condition.NotZero);
                case ArmOp.Condition.VC:
                    Binary(X64Arithmetic.And, Rax, OverflowFlag);
                    return _emitter.ReserveJump(X64Condition.Zero);
                case ArmOp.Condition.HI:
                    Binary(X64Arithmetic.And, Rax, CarryFlag | ZeroFlag);
                    Binary(X64Arithmetic.Compare, Rax, CarryFlag);
                    return _emitter.ReserveJump(X64Condition.Zero);
                case ArmOp.Condition.LS:
                    Binary(X64Arithmetic.And, Rax, CarryFlag | ZeroFlag);
                    Binary(X64Arithmetic.Compare, Rax, CarryFlag);
                    return _emitter.ReserveJump(X64Condition.NotZero);
                case ArmOp.Condition.GE:
                    Move(Rcx, Rax);
                    Shift(X64Shift.Left, Rcx, 3);
                    Binary(X64Arithmetic.Xor, Rax, Rcx);
                    Binary(X64Arithmetic.And, Rax, NegativeFlag);
                    return _emitter.ReserveJump(X64Condition.Zero);
                case ArmOp.Condition.LT:
                    Move(Rcx, Rax);
                    Shift(X64Shift.Left, Rcx, 3);
                    Binary(X64Arithmetic.Xor, Rax, Rcx);
                    Binary(X64Arithmetic.And, Rax, NegativeFlag);
                    return _emitter.ReserveJump(X64Condition.NotZero);
                case ArmOp.Condition.GT:
                    Move(Rcx, Rax);
                    Move(Rdx, Rax);
                    Shift(X64Shift.Left, Rcx, 3);
                    Shift(X64Shift.Left, Rdx, 1);
                    Binary(X64Arithmetic.Xor, Rax, Rcx);
                    Binary(X64Arithmetic.Or, Rax, Rdx);
                    Binary(X64Arithmetic.And, Rax, NegativeFlag);
                    return _emitter.ReserveJump(X64Condition.Zero);
                case ArmOp.Condition.LE:
                    Move(Rcx, Rax);
                    Move(Rdx, Rax);
                    Shift(X64Shift.Left, Rcx, 3);
                    Shift(X64Shift.Left, Rdx, 1);
                    Binary(X64Arithmetic.Xor, Rax, Rcx);
                    Binary(X64Arithmetic.Or, Rax, Rdx);
                    Binary(X64Arithmetic.And, Rax, NegativeFlag);
                    return _emitter.ReserveJump(X64Condition.NotZero);
                default:
                    Sh4HostRuntime.Die("Invalid condition code");
                    return null;
            }
        }

        private void EndConditional(int? label)
        {
            if (label is int offset)
                _emitter.PatchJump(offset, _emitter.Position);
        }

        private bool EmitDataProcessing(ArmOp operation)
        {
            bool saveOverflowFlag = true;
            X64Register? arg0 = GetOperand(operation.Arg[0], R8);
            X64Register? arg1 = GetOperand(operation.Arg[1], R9);
            X64Register rd = default;
            if (operation.Rd.IsReg())
                rd = _regAlloc!.MapReg(operation.Rd.GetReg().ArmReg);

            if (_logicalOperationSetsFlags)
            {
                if (operation.Arg[0].IsImmediate() && operation.Arg[0].GetImmediate() > 255)
                {
                    _setCarryBit = true;
                    Move(R10, (operation.Arg[0].GetImmediate() & 0x80000000u) >> 31);
                }
                else if (operation.Arg[1].IsImmediate() && operation.Arg[1].GetImmediate() > 255)
                {
                    _setCarryBit = true;
                    Move(R10, (operation.Arg[1].GetImmediate() & 0x80000000u) >> 31);
                }
            }

            switch (operation.Type)
            {
                case ArmOp.OpType.AND:
                    if (arg1 == rd)
                        Binary(X64Arithmetic.And, rd, arg0!.Value);
                    else
                    {
                        if (rd != arg0)
                        {
                            Move(rd, arg0!.Value);
                            Sh4HostRuntime.Verify(rd != arg1, "rd != arg1");
                        }
                        if (arg1.HasValue)
                            Binary(X64Arithmetic.And, rd, arg1.Value);
                        else
                            Binary(X64Arithmetic.And, rd, operation.Arg[1].GetImmediate());
                    }
                    saveOverflowFlag = false;
                    break;
                case ArmOp.OpType.ORR:
                    if (arg1 == rd)
                        Binary(X64Arithmetic.Or, rd, arg0!.Value);
                    else
                    {
                        if (rd != arg0)
                        {
                            if (!arg0.HasValue)
                                Move(rd, operation.Arg[0].GetImmediate());
                            else
                                Move(rd, arg0.Value);
                            Sh4HostRuntime.Verify(rd != arg1, "rd != arg1");
                        }
                        if (arg1.HasValue)
                            Binary(X64Arithmetic.Or, rd, arg1.Value);
                        else
                            Binary(X64Arithmetic.Or, rd, operation.Arg[1].GetImmediate());
                    }
                    saveOverflowFlag = false;
                    break;
                case ArmOp.OpType.EOR:
                    if (arg1 == rd)
                        Binary(X64Arithmetic.Xor, rd, arg0!.Value);
                    else
                    {
                        if (rd != arg0)
                        {
                            Sh4HostRuntime.Verify(rd != arg1, "rd != arg1");
                            Move(rd, arg0!.Value);
                        }
                        if (arg1.HasValue)
                            Binary(X64Arithmetic.Xor, rd, arg1.Value);
                        else
                            Binary(X64Arithmetic.Xor, rd, operation.Arg[1].GetImmediate());
                    }
                    saveOverflowFlag = false;
                    break;
                case ArmOp.OpType.BIC:
                    if (!arg1.HasValue)
                    {
                        Move(Rax, operation.Arg[1].GetImmediate());
                        arg1 = Rax;
                    }
                    if (Bmi1.IsSupported)
                        AndNot(rd, arg1.Value, arg0!.Value);
                    else if (rd == arg0)
                    {
                        if (arg1 != R9)
                            Move(R9, arg1.Value);
                        _emitter.Unary(X64Unary.Complement, R9, Dword);
                        Binary(X64Arithmetic.And, rd, R9);
                    }
                    else
                    {
                        if (arg1 != rd)
                            Move(rd, arg1.Value);
                        _emitter.Unary(X64Unary.Complement, rd, Dword);
                        Binary(X64Arithmetic.And, rd, arg0!.Value);
                    }
                    saveOverflowFlag = false;
                    break;
                case ArmOp.OpType.TST:
                    if (arg1.HasValue)
                        _emitter.Test(arg1.Value, arg0!.Value, Dword);
                    else
                        _emitter.TestImmediate(unchecked((int)operation.Arg[1].GetImmediate()), arg0!.Value, Dword);
                    saveOverflowFlag = false;
                    break;
                case ArmOp.OpType.TEQ:
                    if (arg0 != R8)
                        Move(R8, arg0!.Value);
                    if (arg1.HasValue)
                        Binary(X64Arithmetic.Xor, R8, arg1.Value);
                    else
                        Binary(X64Arithmetic.Xor, R8, operation.Arg[1].GetImmediate());
                    saveOverflowFlag = false;
                    break;
                case ArmOp.OpType.CMP:
                    if (arg1.HasValue)
                        Binary(X64Arithmetic.Compare, arg0!.Value, arg1.Value);
                    else
                        Binary(X64Arithmetic.Compare, arg0!.Value, operation.Arg[1].GetImmediate());
                    if (_setFlags)
                    {
                        _emitter.SetCondition(X64Condition.NoCarry, R10);
                        _setCarryBit = true;
                    }
                    break;
                case ArmOp.OpType.CMN:
                    if (arg0 != R8)
                        Move(R8, arg0!.Value);
                    if (arg1.HasValue)
                        Binary(X64Arithmetic.Add, R8, arg1.Value);
                    else
                        Binary(X64Arithmetic.Add, R8, operation.Arg[1].GetImmediate());
                    if (_setFlags)
                    {
                        _emitter.SetCondition(X64Condition.Carry, R10);
                        _setCarryBit = true;
                    }
                    break;
                case ArmOp.OpType.MOV:
                    if (!arg0.HasValue)
                        Move(rd, operation.Arg[0].GetImmediate());
                    else if (arg0 != rd)
                        Move(rd, arg0.Value);
                    if (_setFlags)
                    {
                        _emitter.Test(rd, rd, Dword);
                        saveOverflowFlag = false;
                    }
                    break;
                case ArmOp.OpType.MVN:
                    if (!arg0.HasValue)
                        Move(rd, ~operation.Arg[0].GetImmediate());
                    else
                    {
                        if (arg0 != rd)
                            Move(rd, arg0.Value);
                        _emitter.Unary(X64Unary.Complement, rd, Dword);
                    }
                    if (_setFlags)
                    {
                        _emitter.Test(rd, rd, Dword);
                        saveOverflowFlag = false;
                    }
                    break;
                case ArmOp.OpType.SUB:
                    if (arg1 == rd)
                    {
                        Binary(X64Arithmetic.Subtract, arg0!.Value, arg1.Value);
                        if (rd != arg0)
                            Move(rd, arg0.Value);
                    }
                    else
                    {
                        if (rd != arg0)
                            Move(rd, arg0!.Value);
                        if (!arg1.HasValue)
                            Binary(X64Arithmetic.Subtract, rd, operation.Arg[1].GetImmediate());
                        else
                            Binary(X64Arithmetic.Subtract, rd, arg1.Value);
                    }
                    if (_setFlags)
                    {
                        _emitter.SetCondition(X64Condition.NoCarry, R10);
                        _setCarryBit = true;
                    }
                    break;
                case ArmOp.OpType.RSB:
                    if (arg1 == rd)
                        Binary(X64Arithmetic.Subtract, rd, arg0!.Value);
                    else
                    {
                        if (rd != arg0)
                            Move(rd, arg0!.Value);
                        _emitter.Unary(X64Unary.Negate, rd, Dword);
                        if (!arg1.HasValue)
                            Binary(X64Arithmetic.Add, rd, operation.Arg[1].GetImmediate());
                        else
                            Binary(X64Arithmetic.Add, rd, arg1.Value);
                    }
                    if (_setFlags)
                    {
                        _emitter.SetCondition(X64Condition.Carry, R10);
                        _setCarryBit = true;
                    }
                    break;
                case ArmOp.OpType.ADD:
                    if (arg1 == rd)
                        Binary(X64Arithmetic.Add, rd, arg0!.Value);
                    else
                    {
                        if (rd != arg0)
                        {
                            if (!arg0.HasValue)
                                Move(rd, operation.Arg[0].GetImmediate());
                            else
                                Move(rd, arg0.Value);
                        }
                        if (!arg1.HasValue)
                            Binary(X64Arithmetic.Add, rd, operation.Arg[1].GetImmediate());
                        else
                            Binary(X64Arithmetic.Add, rd, arg1.Value);
                    }
                    if (_setFlags)
                    {
                        _emitter.SetCondition(X64Condition.Carry, R10);
                        _setCarryBit = true;
                    }
                    break;
                case ArmOp.OpType.ADC:
                    LoadArmRegister(R11, Arm7Register.RN_PSR_FLAGS);
                    Binary(X64Arithmetic.And, R11, CarryFlag);
                    _emitter.Unary(X64Unary.Negate, R11, Dword);
                    if (arg1 == rd)
                        Binary(X64Arithmetic.AddCarry, rd, arg0!.Value);
                    else
                    {
                        if (rd != arg0)
                            Move(rd, arg0!.Value);
                        if (!arg1.HasValue)
                            Binary(X64Arithmetic.AddCarry, rd, operation.Arg[1].GetImmediate());
                        else
                            Binary(X64Arithmetic.AddCarry, rd, arg1.Value);
                    }
                    if (_setFlags)
                    {
                        _emitter.SetCondition(X64Condition.Carry, R10);
                        _setCarryBit = true;
                    }
                    break;
                case ArmOp.OpType.SBC:
                    LoadArmRegister(R11, Arm7Register.RN_PSR_FLAGS);
                    Binary(X64Arithmetic.And, R11, CarryFlag);
                    _emitter.Unary(X64Unary.Negate, R11, Dword);
                    _emitter.Append([0xf5]);
                    if (arg1 == rd)
                    {
                        Binary(X64Arithmetic.SubtractBorrow, arg0!.Value, arg1.Value);
                        if (rd != arg0)
                            Move(rd, arg0.Value);
                    }
                    else
                    {
                        if (rd != arg0)
                            Move(rd, arg0!.Value);
                        if (!arg1.HasValue)
                            Binary(X64Arithmetic.SubtractBorrow, rd, operation.Arg[1].GetImmediate());
                        else
                            Binary(X64Arithmetic.SubtractBorrow, rd, arg1.Value);
                    }
                    if (_setFlags)
                    {
                        _emitter.SetCondition(X64Condition.NoCarry, R10);
                        _setCarryBit = true;
                    }
                    break;
                case ArmOp.OpType.RSC:
                    LoadArmRegister(R11, Arm7Register.RN_PSR_FLAGS);
                    Binary(X64Arithmetic.And, R11, CarryFlag);
                    _emitter.Unary(X64Unary.Negate, R11, Dword);
                    _emitter.Append([0xf5]);
                    if (arg1 == rd)
                        Binary(X64Arithmetic.SubtractBorrow, rd, arg0!.Value);
                    else
                    {
                        if (!arg1.HasValue)
                            Move(rd, operation.Arg[1].GetImmediate());
                        else if (rd != arg1)
                            Move(rd, arg1.Value);
                        Binary(X64Arithmetic.SubtractBorrow, rd, arg0!.Value);
                    }
                    if (_setFlags)
                    {
                        _emitter.SetCondition(X64Condition.NoCarry, R10);
                        _setCarryBit = true;
                    }
                    break;
                default:
                    Sh4HostRuntime.Die("invalid");
                    break;
            }

            return saveOverflowFlag;
        }

        private void EmitMemoryOperation(ArmOp operation)
        {
            X64Register? address = GetOperand(operation.Arg[0], X64CallRegisters[0]);
            if (address != X64CallRegisters[0])
            {
                if (!address.HasValue)
                    Move(X64CallRegisters[0], operation.Arg[0].GetImmediate());
                else
                    Move(X64CallRegisters[0], address.Value);
                address = X64CallRegisters[0];
            }

            if (operation.PreIndex)
            {
                ArmOp.Operand offset = operation.Arg[1];
                X64Register? offsetRegister = GetOperand(offset, R9);
                if (offsetRegister.HasValue)
                {
                    if (operation.AddOffset)
                        Binary(X64Arithmetic.Add, address!.Value, offsetRegister.Value);
                    else
                        Binary(X64Arithmetic.Subtract, address!.Value, offsetRegister.Value);
                }
                else if (offset.IsImmediate() && offset.GetImmediate() != 0)
                {
                    if (operation.AddOffset)
                        Binary(X64Arithmetic.Add, address!.Value, offset.GetImmediate());
                    else
                        Binary(X64Arithmetic.Subtract, address!.Value, offset.GetImmediate());
                }
            }

            if (operation.Type == ArmOp.OpType.STR)
            {
                if (operation.Arg[2].IsImmediate())
                    Move(X64CallRegisters[1], operation.Arg[2].GetImmediate());
                else
                    Move(X64CallRegisters[1], _regAlloc!.MapReg(operation.Arg[2].GetReg().ArmReg));
            }

            Call(GetMemOp(operation.Type == ArmOp.OpType.LDR, operation.ByteTransfer));

            if (operation.Type == ArmOp.OpType.LDR)
                Move(_regAlloc!.MapReg(operation.Rd.GetReg().ArmReg), Rax);
        }

        private void SaveFlags(bool saveOverflowFlag)
        {
            if (!_setFlags)
                return;

            _emitter.PushFlags();
            _emitter.Pop(Rax);

            if (saveOverflowFlag)
            {
                Move(R11, Rax);
                Shift(X64Shift.Left, R11, 28 - 11);
            }
            Shift(X64Shift.Left, Rax, 30 - 6);
            if (saveOverflowFlag)
                Binary(X64Arithmetic.And, R11, OverflowFlag);
            Binary(X64Arithmetic.And, Rax, ZeroFlag | NegativeFlag);
            if (saveOverflowFlag)
                Binary(X64Arithmetic.Or, Rax, R11);

            LoadArmRegister(R11, Arm7Register.RN_PSR_FLAGS);
            if (_setCarryBit)
            {
                if (saveOverflowFlag)
                    Binary(X64Arithmetic.And, R11, ~(ZeroFlag | NegativeFlag | CarryFlag | OverflowFlag));
                else
                    Binary(X64Arithmetic.And, R11, ~(ZeroFlag | NegativeFlag | CarryFlag));
                Shift(X64Shift.Left, R10, 29);
                Binary(X64Arithmetic.Or, R11, R10);
            }
            else
            {
                if (saveOverflowFlag)
                    Binary(X64Arithmetic.And, R11, ~(ZeroFlag | NegativeFlag | OverflowFlag));
                else
                    Binary(X64Arithmetic.And, R11, ~(ZeroFlag | NegativeFlag));
            }
            Binary(X64Arithmetic.Or, R11, Rax);
            StoreArmRegister(R11, Arm7Register.RN_PSR_FLAGS);
        }

        private void EmitBranch(ArmOp operation)
        {
            X64Register? address = GetOperand(operation.Arg[0], Rax);
            if (!address.HasValue)
                Move(Rax, operation.Arg[0].GetImmediate());
            else
            {
                if (Rax != address.Value)
                    Move(Rax, address.Value);
                Binary(X64Arithmetic.And, Rax, 0xfffffffcu);
            }
            StoreArmRegister(Rax, Arm7Register.R15_ARM_NEXT);
        }

        private void EmitMsr(ArmOp operation)
        {
            if (operation.Arg[0].IsImmediate())
                Move(X64CallRegisters[0], operation.Arg[0].GetImmediate());
            else
                Move(X64CallRegisters[0], _regAlloc!.MapReg(operation.Arg[0].GetReg().ArmReg));
            Move(X64CallRegisters[1], operation.PsrMask);
            if (operation.Spsr)
                Call((nuint)Arm7Di.MsrDoSpsrEntry);
            else
                Call((nuint)Arm7Di.MsrDoCpsrEntry);
        }

        private void EmitMrs(ArmOp operation)
        {
            Call((nuint)Arm7Di.UpdateCpsrEntry);
            if (operation.Spsr)
                LoadArmRegister(_regAlloc!.MapReg(operation.Rd.GetReg().ArmReg), Arm7Register.RN_SPSR);
            else
                LoadArmRegister(_regAlloc!.MapReg(operation.Rd.GetReg().ArmReg), Arm7Register.RN_CPSR);
        }

        private void EmitFallback(ArmOp operation)
        {
            _setFlags = false;
            Move(X64CallRegisters[0], operation.Arg[0].GetImmediate());
            Call((nuint)Arm7Di.InterpretEntry);
        }

        internal void Compile(IReadOnlyList<ArmOp> blockOperations, uint cycles)
        {
            _regAlloc = new X64ArmRegAlloc(this, blockOperations);

            BinaryArmRegisterImmediate(X64Arithmetic.Subtract, Arm7Register.CYCL_CNT, cycles);

            ArmOp.Condition currentCondition = ArmOp.Condition.AL;
            int? conditionLabel = null;

            for (uint i = 0; i < blockOperations.Count; i++)
            {
                ArmOp operation = blockOperations[(int)i];
                Debug.WriteLine($"-> {operation}");

                _setFlags = (operation.Flags & ArmOp.OP_SETS_FLAGS) != 0;
                _logicalOperationSetsFlags = operation.IsLogicalOp() && _setFlags;
                _setCarryBit = false;
                bool saveOverflowFlag = true;

                if (operation.Type == ArmOp.OpType.FALLBACK)
                {
                    EndConditional(conditionLabel);
                    conditionLabel = null;
                    currentCondition = ArmOp.Condition.AL;
                }
                else if (operation.ConditionCode != currentCondition)
                {
                    EndConditional(conditionLabel);
                    currentCondition = operation.ConditionCode;
                    conditionLabel = StartConditional(operation.ConditionCode);
                }

                _regAlloc.Load(i);

                if ((int)operation.Type <= (int)ArmOp.OpType.MVN)
                    saveOverflowFlag = EmitDataProcessing(operation);
                else if ((int)operation.Type <= (int)ArmOp.OpType.STR)
                    EmitMemoryOperation(operation);
                else if ((int)operation.Type <= (int)ArmOp.OpType.BL)
                    EmitBranch(operation);
                else if (operation.Type == ArmOp.OpType.MRS)
                    EmitMrs(operation);
                else if (operation.Type == ArmOp.OpType.MSR)
                    EmitMsr(operation);
                else if (operation.Type == ArmOp.OpType.FALLBACK)
                    EmitFallback(operation);
                else
                    Sh4HostRuntime.Die("invalid");

                SaveFlags(saveOverflowFlag);

                _regAlloc.Store(i);

                if (_setFlags)
                {
                    currentCondition = ArmOp.Condition.AL;
                    EndConditional(conditionLabel);
                    conditionLabel = null;
                }
            }
            EndConditional(conditionLabel);

            _emitter.JumpRelativeAddress(_code, _armDispatch);

            Ready();
            Advance((uint)_emitter.Position);

            _regAlloc = null;
        }

        internal void GenerateMainLoop()
        {
            if (!Empty())
            {
                Sh4HostRuntime.Verify(ArmMainLoop != null, "arm_mainloop != nullptr");
                Sh4HostRuntime.Verify((nuint)ArmCompileCode != 0, "arm_compilecode != nullptr");
                return;
            }

            Call(CompileEntry);
            int dispatchJump = _emitter.ReserveShortJump();

            int mainLoopOffset = _emitter.Position;
            X64Unwinder.Start(_code + (uint)_emitter.Position);
            int startOffset = _emitter.Position;
            if (OperatingSystem.IsWindows())
            {
                PushAndRecord(Rdi);
                PushAndRecord(Rsi);
            }
            PushAndRecord(R12);
            PushAndRecord(R13);
            PushAndRecord(R14);
            PushAndRecord(R15);
            PushAndRecord(Rbx);
            PushAndRecord(Rbp);
            if (OperatingSystem.IsWindows())
            {
                _emitter.AllocateStack(40);
                X64Unwinder.AllocateStack((uint)_emitter.Position, 40);
            }
            else
            {
                _emitter.AllocateStack(8);
                X64Unwinder.AllocateStack((uint)_emitter.Position, 8);
            }
            X64Unwinder.EndProlog((uint)_emitter.Position);

            fixed (nuint* address = &_dispatchEntryPoints)
                _emitter.StoreRipRelative(X64CallRegisters[1], _code, (nuint)address, Qword);

            int dispatchOffset = _emitter.Position;
            _emitter.PatchShortJump(dispatchJump, dispatchOffset);
            fixed (nuint* address = &_dispatchEntryPoints)
                _emitter.LoadRipRelative(_code, (nuint)address, Rdx, Qword);
            LoadArmRegister(Rcx, Arm7Register.R15_ARM_NEXT);
            LoadArmRegister(Rax, Arm7Register.INTR_PEND);
            BinaryArmRegisterImmediate(X64Arithmetic.Compare, Arm7Register.CYCL_CNT, 0);
            int exitJump = _emitter.ReserveShortJump(X64Condition.LessOrEqual);
            _emitter.Test(Rax, Rax, Dword);
            int fiqJump = _emitter.ReserveShortJump(X64Condition.NotZero);

            Binary(X64Arithmetic.And, Rcx, 0x7ffffc);
            _emitter.Append([0xff, 0x24, 0x4a]);

            _emitter.PatchShortJump(fiqJump, _emitter.Position);
            Call((nuint)Arm7Di.FiqEntry);
            _emitter.JumpShort(dispatchOffset);

            _emitter.PatchShortJump(exitJump, _emitter.Position);
            if (OperatingSystem.IsWindows())
                _emitter.ReleaseStack(40);
            else
                _emitter.ReleaseStack(8);
            _emitter.Pop(Rbp);
            _emitter.Pop(Rbx);
            _emitter.Pop(R15);
            _emitter.Pop(R14);
            _emitter.Pop(R13);
            _emitter.Pop(R12);
            if (OperatingSystem.IsWindows())
            {
                _emitter.Pop(Rsi);
                _emitter.Pop(Rdi);
            }
            _emitter.Return();

            nuint unwindSize = X64Unwinder.End(SpaceLeft() - 128u - (uint)startOffset);
            Sh4HostRuntime.Verify(unwindSize <= 128, "unwindSize <= 128");

            Ready();
            ArmCompileCode = _code;
            ArmMainLoop = (delegate* unmanaged<nint, nint, void>)(_code + (uint)mainLoopOffset);
            _armDispatch = _code + (uint)dispatchOffset;

            Advance((uint)_emitter.Position);
        }

        internal void LoadArmRegister(X64Register hostRegister, Arm7Register register) =>
            _emitter.LoadRipRelative(_code, RegisterAddress(register), hostRegister, Dword);

        internal void StoreArmRegister(X64Register hostRegister, Arm7Register register) =>
            _emitter.StoreRipRelative(hostRegister, _code, RegisterAddress(register), Dword);

        private void Move(X64Register destination, X64Register source) => _emitter.Move(source, destination, Dword);

        private void Move(X64Register destination, uint immediate) => _emitter.MoveImmediate(immediate, destination, Dword);

        private void Shift(X64Shift shift, X64Register destination, uint count) =>
            _emitter.Shift(shift, unchecked((byte)count), destination, Dword);

        private void Binary(X64Arithmetic operation, X64Register destination, X64Register source) =>
            _emitter.Binary(operation, source, destination, Dword);

        private void Binary(X64Arithmetic operation, X64Register destination, uint immediate) =>
            _emitter.BinaryImmediate(operation, unchecked((int)immediate), destination, Dword);

        private void ConditionalMove(X64Condition condition, X64Register destination, X64Register source)
        {
            int rex = 0x40 | (((int)destination & 8) >> 1) | (((int)source & 8) >> 3);
            if (rex != 0x40)
                _emitter.Append([(byte)rex]);
            int modrm = 0xc0 | (((int)destination & 7) << 3) | ((int)source & 7);
            _emitter.Append([0x0f, (byte)(0x40 + (int)condition), (byte)modrm]);
        }

        private void AndNot(X64Register destination, X64Register left, X64Register right)
        {
            int vexMap = 0x62 | ((~(int)destination & 8) << 4);
            vexMap &= 0xdf | ((~(int)right & 8) << 2);
            int vexSource = (~(int)left & 15) << 3;
            int modrm = 0xc0 | (((int)destination & 7) << 3) | ((int)right & 7);
            _emitter.Append([0xc4, (byte)vexMap, (byte)vexSource, 0xf2, (byte)modrm]);
        }

        private void BinaryArmRegisterImmediate(X64Arithmetic operation, Arm7Register register, uint immediate)
        {
            int value = unchecked((int)immediate);
            bool shortImmediate = value is >= sbyte.MinValue and <= sbyte.MaxValue;
            Span<byte> code = stackalloc byte[10];
            code[0] = shortImmediate ? (byte)0x83 : (byte)0x81;
            code[1] = (byte)(((int)operation << 3) | 5);
            int instructionSize = shortImmediate ? 7 : 10;
            long displacement = unchecked((long)(RegisterAddress(register) - _code)) - _emitter.Position - instructionSize;
            BinaryPrimitives.WriteInt32LittleEndian(code[2..], checked((int)displacement));
            if (shortImmediate)
                code[6] = unchecked((byte)value);
            else
                BinaryPrimitives.WriteInt32LittleEndian(code[6..], value);
            _emitter.Append(code[..instructionSize]);
        }

        private static nuint RegisterAddress(Arm7Register register) =>
            (nuint)(Arm7Di.RegisterAddress + (int)register);

        private void Call(nuint target) => _emitter.CallRelativeAddress(_code, target);

        private void PushAndRecord(X64Register register)
        {
            _emitter.Push(register);
            X64Unwinder.PushRegister((uint)_emitter.Position, (int)register);
        }

        private void Ready()
        {
            byte[] code = _emitter.ToArray();
            code.AsSpan().CopyTo(new Span<byte>((void*)_code, code.Length));
        }
    }

    internal static void CompileX64Backend(IReadOnlyList<ArmOp> blockOperations, uint cycles)
    {
        nuint start = (nuint)CurrentCode();
        uint length = SpaceLeft();
        Sh4VirtualMemory.SetJitExecutable(start, length, false);

        var compiler = new X64ArmCompiler();
        compiler.Compile(blockOperations, cycles);

        Sh4VirtualMemory.SetJitExecutable(start, length, true);
    }

    internal static void FlushX64Backend()
    {
        nuint start = (nuint)CurrentCode();
        uint length = SpaceLeft();
        Sh4VirtualMemory.SetJitExecutable(start, length, false);
        X64Unwinder.Clear();

        var compiler = new X64ArmCompiler();
        compiler.GenerateMainLoop();

        Sh4VirtualMemory.SetJitExecutable(start, length, true);
    }
}
#endif
