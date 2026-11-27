using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Compilation.X64;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

using static Sh4ShilOperation;
using static X64Register;
using static X64Width;

#if !SH4_DYNAREC_NONE
internal abstract unsafe class Sh4XbyakBaseCompiler
{
    protected Sh4XbyakBaseCompiler(Sh4Context context, Sh4CodeBuffer codeBuffer, bool archX64 = true)
        : this(context, codeBuffer, codeBuffer.Current, archX64)
    {
    }

    protected Sh4XbyakBaseCompiler(Sh4Context context, Sh4CodeBuffer codeBuffer, nuint writableCode, bool archX64)
    {
        Context = context;
        CodeBuffer = codeBuffer;
        WritableCode = writableCode;
        ArchX64 = archX64;
        Emitter = new X64Emitter(codeBuffer.FreeSpace, writableCode);
    }

    protected Sh4Context Context { get; }
    protected Sh4CodeBuffer CodeBuffer { get; }
    protected nuint WritableCode { get; }
    protected bool ArchX64 { get; }
    private bool AllocateFloat64 => ArchX64 && OperatingSystem.IsWindows();
    protected X64Emitter Emitter { get; }
    protected abstract Sh4SsaRegisterAllocator Allocator { get; }

    private void GenerateBinaryOperation(Sh4ShilOpcode operation, X64Arithmetic instruction)
    {
        X64Register destination = MapRegister(operation.Destination);
        Sh4ShilParameter right = operation.Source2;
        if (Allocator.MapGeneral(operation.Destination) != Allocator.MapGeneral(operation.Source1))
        {
            if (operation.Source2.IsRegister && Allocator.MapGeneral(operation.Destination) == Allocator.MapGeneral(operation.Source2))
            {
                if (operation.Operation == shop_sub)
                {
                    Emitter.Unary(X64Unary.Negate, destination, Dword);
                    Emitter.Binary(X64Arithmetic.Add, MapRegister(operation.Source1), destination, Dword);
                    return;
                }
                right = operation.Source1;
            }
            else
            {
                Emitter.Move(MapRegister(operation.Source1), destination, Dword);
            }
        }

        if (operation.Source2.IsImmediate)
        {
            Emitter.MoveImmediate(operation.Source2.Immediate, Rcx, Dword);
            Emitter.Binary(instruction, Rcx, destination, Dword);
        }
        else
        {
            Emitter.Binary(instruction, MapRegister(right), destination, Dword);
        }
    }

    private void GenerateBinaryFloatOperation(Sh4ShilOpcode operation, byte instruction)
    {
        int destination = MapXRegister(operation.Destination);
        Sh4ShilParameter right = operation.Source2;
        if (Allocator.MapFloat(operation.Destination) != Allocator.MapFloat(operation.Source1))
        {
            if (operation.Source2.IsRegister && Allocator.MapFloat(operation.Destination) == Allocator.MapFloat(operation.Source2))
            {
                if (operation.Operation is shop_fsub or shop_fdiv)
                {
                    MoveFloatRegister(0, MapXRegister(operation.Source2));
                    MoveFloatRegister(destination, MapXRegister(operation.Source1));
                    EmitSseRegister(0xF3, instruction, destination, 0);
                    return;
                }
                right = operation.Source1;
            }
            else
            {
                MoveFloatRegister(destination, MapXRegister(operation.Source1));
            }
        }

        if (operation.Source2.IsImmediate)
        {
            Emitter.MoveImmediate(operation.Source2.Immediate, Rax, Dword);
            MoveGeneralToFloat(0, Rax);
            EmitSseRegister(0xF3, instruction, destination, 0);
        }
        else
        {
            EmitSseRegister(0xF3, instruction, destination, MapXRegister(right));
        }
    }

    protected bool GenerateBaseOpcode(Sh4ShilOpcode operation)
    {
        Sh4ShilParameter destinationParameter = operation.Destination;
        Sh4ShilParameter firstParameter = operation.Source1;
        Sh4ShilParameter secondParameter = operation.Source2;
        switch (operation.Operation)
        {
            case shop_jcond:
            case shop_jdyn:
            {
                X64Register destination = MapRegister(destinationParameter);
                if (firstParameter.IsImmediate)
                {
                    if (secondParameter.IsImmediate)
                        Emitter.MoveImmediate(unchecked(firstParameter.Immediate + secondParameter.Immediate), destination, Dword);
                    else
                    {
                        Emitter.MoveImmediate(firstParameter.Immediate, destination, Dword);
                        Sh4HostRuntime.Verify(secondParameter.IsNull);
                    }
                }
                else
                {
                    X64Register source = MapRegister(firstParameter);
                    if (source != destination)
                        Emitter.Move(source, destination, Dword);
                    if (secondParameter.IsImmediate)
                        Emitter.BinaryImmediate(X64Arithmetic.Add, unchecked((int)secondParameter.Immediate), destination, Dword);
                }
                break;
            }
            case shop_mov32:
                Sh4HostRuntime.Verify(destinationParameter.IsRegister);
                Sh4HostRuntime.Verify(firstParameter.IsRegister || firstParameter.IsImmediate);
                if (Allocator.IsFloatAllocated(destinationParameter))
                    ShilParameterToHostRegister(firstParameter, MapXRegister(destinationParameter));
                else
                    ShilParameterToHostRegister(firstParameter, MapRegister(destinationParameter));
                break;
            case shop_swaplb:
                CopyFirstOperand(destinationParameter, firstParameter);
                Emitter.Shift(X64Shift.RotateRight, 8, MapRegister(destinationParameter), Word);
                break;
            case shop_neg:
                CopyFirstOperand(destinationParameter, firstParameter);
                Emitter.Unary(X64Unary.Negate, MapRegister(destinationParameter), Dword);
                break;
            case shop_not:
                CopyFirstOperand(destinationParameter, firstParameter);
                Emitter.Unary(X64Unary.Complement, MapRegister(destinationParameter), Dword);
                break;
            case shop_and:
                GenerateBinaryOperation(operation, X64Arithmetic.And);
                break;
            case shop_or:
                GenerateBinaryOperation(operation, X64Arithmetic.Or);
                break;
            case shop_xor:
                GenerateBinaryOperation(operation, X64Arithmetic.Xor);
                break;
            case shop_add:
                GenerateBinaryOperation(operation, X64Arithmetic.Add);
                break;
            case shop_sub:
                GenerateBinaryOperation(operation, X64Arithmetic.Subtract);
                break;
            case shop_shl:
                GenerateShift(operation, X64Shift.Left);
                break;
            case shop_shr:
                GenerateShift(operation, X64Shift.Right);
                break;
            case shop_sar:
                GenerateShift(operation, X64Shift.ArithmeticRight);
                break;
            case shop_ror:
                GenerateShift(operation, X64Shift.RotateRight);
                break;
            case shop_adc:
            {
                Emitter.BinaryImmediate(X64Arithmetic.Compare, 1, MapRegister(operation.Source3), Dword);
                X64Register destination = MapRegister(operation.Destination);
                X64Register right = default;
                if (operation.Source2.IsRegister)
                {
                    right = MapRegister(operation.Source2);
                    if (Allocator.MapGeneral(operation.Destination) == Allocator.MapGeneral(operation.Source2))
                    {
                        Emitter.Move(right, Rcx, Dword);
                        right = Rcx;
                    }
                }
                if (operation.Source1.IsImmediate)
                    Emitter.MoveImmediate(operation.Source1.Immediate, destination, Dword);
                else if (Allocator.MapGeneral(operation.Destination) != Allocator.MapGeneral(operation.Source1))
                    Emitter.Move(MapRegister(operation.Source1), destination, Dword);
                Emitter.Append([0xF5]);
                if (operation.Source2.IsRegister)
                    Emitter.Binary(X64Arithmetic.AddCarry, right, destination, Dword);
                else
                    Emitter.BinaryImmediate(X64Arithmetic.AddCarry, unchecked((int)operation.Source2.Immediate), destination, Dword);
                EmitCarryFlag(MapRegister(operation.Destination2), X64Condition.Carry);
                break;
            }
            case shop_rocr:
            case shop_rocl:
            {
                X64Register destination = MapRegister(destinationParameter);
                Emitter.BinaryImmediate(X64Arithmetic.Compare, 1, MapRegister(secondParameter), Dword);
                if (firstParameter.IsImmediate)
                    Emitter.MoveImmediate(firstParameter.Immediate, destination, Dword);
                else if (Allocator.MapGeneral(destinationParameter) != Allocator.MapGeneral(firstParameter))
                    Emitter.Move(MapRegister(firstParameter), destination, Dword);
                Emitter.Append([0xF5]);
                Emitter.Shift(operation.Operation == shop_rocr ? X64Shift.RotateCarryRight : X64Shift.RotateCarryLeft,
                    1, destination, Dword);
                Emitter.SetCondition(X64Condition.Carry, Rax);
                Emitter.MoveExtend(Rax, MapRegister(operation.Destination2), Byte, Dword, signed: false);
                break;
            }
            case shop_shld:
            case shop_shad:
            {
                if (operation.Source2.IsRegister)
                    Emitter.Move(MapRegister(operation.Source2), Rcx, Dword);
                else
                    Emitter.MoveImmediate(operation.Source2.Immediate, Rcx, Dword);
                X64Register destination = MapRegister(operation.Destination);
                if (operation.Source1.IsImmediate)
                    Emitter.MoveImmediate(operation.Source1.Immediate, destination, Dword);
                else if (Allocator.MapGeneral(operation.Destination) != Allocator.MapGeneral(operation.Source1))
                    Emitter.Move(MapRegister(operation.Source1), destination, Dword);

                Emitter.BinaryImmediate(X64Arithmetic.Compare, 0, Rcx, Dword);
                int negative = Emitter.ReserveShortJump(X64Condition.Sign);
                Emitter.ShiftByCl(X64Shift.Left, destination, Dword);
                int exitAfterLeft = Emitter.ReserveShortJump();

                Emitter.PatchShortJump(negative, Emitter.Position);
                Emitter.TestImmediate(0x1F, Rcx, Dword);
                int nonZero = Emitter.ReserveShortJump(X64Condition.NotZero);
                bool logical = operation.Operation == shop_shld;
                if (logical)
                    Emitter.Binary(X64Arithmetic.Xor, destination, destination, Dword);
                else
                    Emitter.Shift(X64Shift.ArithmeticRight, 31, destination, Dword);
                int exitAfterZero = Emitter.ReserveShortJump();

                Emitter.PatchShortJump(nonZero, Emitter.Position);
                Emitter.Unary(X64Unary.Negate, Rcx, Dword);
                Emitter.ShiftByCl(logical ? X64Shift.Right : X64Shift.ArithmeticRight, destination, Dword);
                Emitter.PatchShortJump(exitAfterLeft, Emitter.Position);
                Emitter.PatchShortJump(exitAfterZero, Emitter.Position);
                break;
            }
            case shop_test:
            case shop_seteq:
            case shop_setge:
            case shop_setgt:
            case shop_setae:
            case shop_setab:
            {
                X64Register first = MapRegister(operation.Source1);
                Sh4ShilParameter second = operation.Source2;
                if (operation.Operation == shop_test)
                {
                    if (second.IsImmediate)
                        Emitter.TestImmediate(unchecked((int)second.Immediate), first, Dword);
                    else
                        Emitter.Test(MapRegister(second), first, Dword);
                }
                else if (second.IsImmediate)
                {
                    Emitter.BinaryImmediate(X64Arithmetic.Compare, unchecked((int)second.Immediate), first, Dword);
                }
                else
                {
                    Emitter.Binary(X64Arithmetic.Compare, MapRegister(second), first, Dword);
                }

                switch (operation.Operation)
                {
                    case shop_test:
                    case shop_seteq:
                        Emitter.SetCondition(X64Condition.Zero, Rax);
                        break;
                    case shop_setge:
                        Emitter.SetCondition(X64Condition.GreaterOrEqual, Rax);
                        break;
                    case shop_setgt:
                        Emitter.SetCondition(X64Condition.Greater, Rax);
                        break;
                    case shop_setae:
                        Emitter.SetCondition(X64Condition.NoCarry, Rax);
                        break;
                    case shop_setab:
                        Emitter.SetCondition(X64Condition.Above, Rax);
                        break;
                    default:
                        Sh4HostRuntime.Die("invalid case");
                        break;
                }
                Emitter.MoveExtend(Rax, MapRegister(operation.Destination), Byte, Dword, signed: false);
                break;
            }
            case shop_setpeq:
            {
                Emitter.Move(MapRegister(operation.Source1), Rcx, Dword);
                if (operation.Source2.IsInteger32)
                    Emitter.Binary(X64Arithmetic.Xor, MapRegister(operation.Source2), Rcx, Dword);
                else
                    Emitter.BinaryImmediate(X64Arithmetic.Xor, unchecked((int)operation.Source2.Immediate), Rcx, Dword);
                X64Register destination = MapRegister(operation.Destination);
                Emitter.MoveImmediate(1, destination, Dword);
                Emitter.TestImmediate(unchecked((int)0xFF000000), Rcx, Dword);
                int highMatch = Emitter.ReserveShortJump(X64Condition.Zero);
                Emitter.TestImmediate(0x00FF0000, Rcx, Dword);
                int highMiddleMatch = Emitter.ReserveShortJump(X64Condition.Zero);
                Emitter.TestImmediate(0x0000FF00, Rcx, Dword);
                int lowMiddleMatch = Emitter.ReserveShortJump(X64Condition.Zero);
                Emitter.Binary(X64Arithmetic.Xor, destination, destination, Dword);
                Emitter.Test(Rcx, Rcx, Byte);
                EmitCarryFlag(destination, X64Condition.Zero);
                Emitter.PatchShortJump(highMatch, Emitter.Position);
                Emitter.PatchShortJump(highMiddleMatch, Emitter.Position);
                Emitter.PatchShortJump(lowMiddleMatch, Emitter.Position);
                break;
            }
            case shop_mul_u16:
                Emitter.MoveExtend(MapRegister(firstParameter), Rax, Word, Dword, signed: false);
                if (secondParameter.IsRegister)
                    Emitter.MoveExtend(MapRegister(secondParameter), Rcx, Word, Dword, signed: false);
                else
                    Emitter.MoveImmediate(secondParameter.Immediate & 0xFFFF, Rcx, Dword);
                Emitter.Unary(X64Unary.Multiply, Rcx, Dword);
                Emitter.Move(Rax, MapRegister(destinationParameter), Dword);
                break;

            case shop_mul_s16:
                Emitter.MoveExtend(MapRegister(firstParameter), Rax, Word, Dword, signed: true);
                if (secondParameter.IsRegister)
                    Emitter.MoveExtend(MapRegister(secondParameter), Rcx, Word, Dword, signed: true);
                else
                    Emitter.MoveImmediate(unchecked((short)secondParameter.Immediate), Rcx, Dword);
                Emitter.Unary(X64Unary.Multiply, Rcx, Dword);
                Emitter.Move(Rax, MapRegister(destinationParameter), Dword);
                break;

            case shop_mul_i32:
                Emitter.Move(MapRegister(firstParameter), Rax, Dword);
                if (secondParameter.IsRegister)
                {
                    Emitter.Unary(X64Unary.Multiply, MapRegister(secondParameter), Dword);
                }
                else
                {
                    Emitter.MoveImmediate(secondParameter.Immediate, Rcx, Dword);
                    Emitter.Unary(X64Unary.Multiply, Rcx, Dword);
                }
                Emitter.Move(Rax, MapRegister(destinationParameter), Dword);
                break;

            case shop_mul_u64:
                Emitter.Move(MapRegister(firstParameter), Rax, Dword);
                if (secondParameter.IsRegister)
                    Emitter.Move(MapRegister(secondParameter), Rdx, Dword);
                else
                    Emitter.MoveImmediate(secondParameter.Immediate, Rdx, Dword);
                if (ArchX64)
                {
#if !XBYAK32
                    Emitter.Unary(X64Unary.Multiply, Rdx, Qword);
                    Emitter.Move(Rax, MapRegister(destinationParameter), Dword);
                    Emitter.Shift(X64Shift.Right, 32, Rax, Qword);
                    Emitter.Move(Rax, MapRegister(operation.Destination2), Dword);
#endif
                }
                else
                {
                    Emitter.Unary(X64Unary.Multiply, Rdx, Dword);
                    Emitter.Move(Rax, MapRegister(destinationParameter), Dword);
                    Emitter.Move(Rdx, MapRegister(operation.Destination2), Dword);
                }
                break;
            case shop_ext_s8:
                Emitter.Move(MapRegister(firstParameter), Rax, Dword);
                Emitter.MoveExtend(Rax, MapRegister(destinationParameter), Byte, Dword, signed: true);
                break;
            case shop_ext_s16:
                Emitter.MoveExtend(MapRegister(firstParameter), MapRegister(destinationParameter), Word, Dword, signed: true);
                break;
            case shop_xtrct:
            {
                X64Register destination = MapRegister(operation.Destination);
                X64Register first = Rcx;
                if (operation.Source1.IsRegister)
                    first = MapRegister(operation.Source1);
                else
                    Emitter.MoveImmediate(operation.Source1.Immediate, first, Dword);
                X64Register second = Rax;
                if (operation.Source2.IsRegister)
                    second = MapRegister(operation.Source2);
                else
                    Emitter.MoveImmediate(operation.Source2.Immediate, second, Dword);
                if (destination == second)
                {
                    Emitter.Shift(X64Shift.Left, 16, destination, Dword);
                    Emitter.Move(first, Rax, Dword);
                    Emitter.Shift(X64Shift.Right, 16, Rax, Dword);
                    Emitter.Binary(X64Arithmetic.Or, Rax, destination, Dword);
                    break;
                }
                else if (destination != first)
                {
                    Emitter.Move(first, destination, Dword);
                }
                Emitter.Shift(X64Shift.Right, 16, destination, Dword);
                Emitter.Move(second, Rax, Dword);
                Emitter.Shift(X64Shift.Left, 16, Rax, Dword);
                Emitter.Binary(X64Arithmetic.Or, Rax, destination, Dword);
                break;
            }
            case shop_fadd:
                GenerateBinaryFloatOperation(operation, 0x58);
                break;
            case shop_fsub:
                GenerateBinaryFloatOperation(operation, 0x5C);
                break;
            case shop_fmul:
                GenerateBinaryFloatOperation(operation, 0x59);
                break;
            case shop_fdiv:
                GenerateBinaryFloatOperation(operation, 0x5E);
                break;
            case shop_fabs:
                MoveFloatToGeneral(Rax, MapXRegister(firstParameter));
                Emitter.BinaryImmediate(X64Arithmetic.And, 0x7FFFFFFF, Rax, Dword);
                MoveGeneralToFloat(MapXRegister(destinationParameter), Rax);
                break;

            case shop_fneg:
                MoveFloatToGeneral(Rax, MapXRegister(firstParameter));
                Emitter.BinaryImmediate(X64Arithmetic.Xor, unchecked((int)0x80000000), Rax, Dword);
                MoveGeneralToFloat(MapXRegister(destinationParameter), Rax);
                break;
            case shop_fsqrt:
                EmitSseRegister(0xF3, 0x51, MapXRegister(destinationParameter), MapXRegister(firstParameter));
                break;
            case shop_fmac:
            {
                int first = MapXRegister(operation.Source1);
                int second = MapXRegister(operation.Source2);
                int third = MapXRegister(operation.Source3);
                int destination = MapXRegister(operation.Destination);
                if (destination == second)
                {
                    MoveFloatRegister(1, second);
                    second = 1;
                }
                if (destination == third)
                {
                    MoveFloatRegister(2, third);
                    third = 2;
                }
                if (operation.Source1.IsImmediate)
                {
                    Emitter.MoveImmediate(operation.Source1.Immediate, Rax, Dword);
                    MoveGeneralToFloat(destination, Rax);
                }
                else if (destination != first)
                {
                    MoveFloatRegister(destination, first);
                }
                MoveFloatRegister(0, second);
                EmitSseRegister(0xF3, 0x59, 0, third);
                EmitSseRegister(0xF3, 0x58, destination, 0);
                break;
            }
            case shop_fsrra:
            {
                EmitSseRegister(0xF3, 0x51, 0, MapXRegister(operation.Source1));
                if (ArchX64)
                {
                    Emitter.MoveImmediate(0x3F800000, Rax, Dword);
                    MoveGeneralToFloat(MapXRegister(operation.Destination), Rax);
                }
                else
                {
                    fixed (float* address = Rom.One)
                        EmitAbsoluteSse(0x10, MapXRegister(operation.Destination), (uint)address);
                }
                EmitSseRegister(0xF3, 0x5E, MapXRegister(operation.Destination), 0);
                break;
            }
            case shop_fsetgt:
            case shop_fseteq:
            {
                EmitSseRegister(0, 0x2E, MapXRegister(operation.Source1), MapXRegister(operation.Source2));
                if (operation.Operation == shop_fsetgt)
                {
                    Emitter.SetCondition(X64Condition.Above, Rax);
                }
                else
                {
#if __FAST_MATH__
                    Emitter.SetCondition(X64Condition.Zero, Rax);
#else
                    Emitter.MoveImmediate(0, Rdx, Dword);
                    Emitter.SetCondition(X64Condition.NoParity, Rax);
                    Emitter.Append([0x0F, 0x45, 0xC2]);
#endif
                }
                Emitter.MoveExtend(Rax, MapRegister(operation.Destination), Byte, Dword, signed: false);
                break;
            }
            case shop_fsca:
            {
                if (operation.Source1.IsImmediate)
                    Emitter.MoveImmediate(operation.Source1.Immediate & 0xFFFF, Rax, Dword);
                else
                    Emitter.MoveExtend(MapRegister(operation.Source1), Rax, Word, Dword, signed: false);

                Sh4ShilParameter destination = operation.Destination;
                float* table = (float*)Sh4FscaTable.NativeAddress;
                if (ArchX64)
                {
#if !XBYAK32
                    Emitter.MoveImmediate((long)table, Rcx, Qword);
                    Emitter.LoadIndexed(Rcx, Rax, 3, 0, Rcx, Qword);
                    if (!AllocateFloat64)
                    {
                        Emitter.MoveImmediate((long)destination.GetRegisterPointer(Context), Rdx, Qword);
                        Emitter.Store(Rcx, Rdx, 0, Qword);
                    }
                    else
                    {
                        MoveGeneralToFloat(MapXRegister(destination, 0), Rcx);
                        Emitter.Shift(X64Shift.Right, 32, Rcx, Qword);
                        MoveGeneralToFloat(MapXRegister(destination, 1), Rcx);
                    }
#endif
                }
                else
                {
                    if (AllocateFloat64)
                    {
                        EmitAbsoluteSse(0x10, MapXRegister(destination, 0), (uint)table, indexed: true);
                        EmitAbsoluteSse(0x10, MapXRegister(destination, 1), (uint)(table + 1), indexed: true);
                    }
                    else
                    {
                        Sh4HostRuntime.Verify(!Allocator.IsAllocated(destination));
                        EmitAbsoluteInteger(0x8B, Rcx, (uint)table, indexed: true);
                        EmitAbsoluteInteger(0x8B, Rdx, (uint)(table + 1), indexed: true);
                        EmitAbsoluteInteger(0x89, Rcx, (uint)destination.GetRegisterPointer(Context));
                        EmitAbsoluteInteger(0x89, Rdx, (uint)(destination.GetRegisterPointer(Context) + 1));
                    }
                }
                break;
            }
            case shop_cvt_f2i_t:
            {
                X64Register destination = MapRegister(operation.Destination);
                EmitSseRegister(0xF3, 0x2C, (int)Rdx, MapXRegister(operation.Source1));
                Emitter.MoveImmediate(0x7FFFFFFF, destination, Dword);
                Emitter.BinaryImmediate(X64Arithmetic.Compare, 0x7FFFFF80, Rdx, Dword);
                int aboveMax = Emitter.ReserveShortJump(X64Condition.Greater);
                Emitter.Move(Rdx, destination, Dword);
                Emitter.BinaryImmediate(X64Arithmetic.Compare, unchecked((int)0x80000000), destination, Dword);
                int regular = Emitter.ReserveShortJump(X64Condition.NotZero);
                Emitter.Binary(X64Arithmetic.Xor, Rax, Rax, Dword);
                EmitSseRegister(0x66, 0xEF, 0, 0);
                EmitSseRegister(0, 0x2E, MapXRegister(operation.Source1), 0);
                Emitter.SetCondition(X64Condition.Carry, Rax);
                Emitter.BinaryImmediate(X64Arithmetic.Add, 0x7FFFFFFF, Rax, Dword);
                Emitter.Move(Rax, destination, Dword);
                Emitter.PatchShortJump(aboveMax, Emitter.Position);
                Emitter.PatchShortJump(regular, Emitter.Position);
                break;
            }
            case shop_cvt_i2f_n:
            case shop_cvt_i2f_z:
                EmitSseRegister(0xF3, 0x2A, MapXRegister(destinationParameter), (int)MapRegister(firstParameter));
                break;
            default:
                return false;
        }
        return true;
    }

    private void CopyFirstOperand(Sh4ShilParameter result, Sh4ShilParameter first)
    {
        if (Allocator.MapGeneral(result) != Allocator.MapGeneral(first))
            Emitter.Move(MapRegister(first), MapRegister(result), Dword);
    }

    private void GenerateShift(Sh4ShilOpcode operation, X64Shift shift)
    {
        CopyFirstOperand(operation.Destination, operation.Source1);
        if (operation.Source2.IsImmediate)
            Emitter.Shift(shift, (byte)operation.Source2.Immediate, MapRegister(operation.Destination), Dword);
        else
            Sh4HostRuntime.Die("Unsupported operand");
    }

    private void EmitCarryFlag(X64Register destination, X64Condition condition)
    {
        if ((int)destination >= 4 && !ArchX64)
        {
            Emitter.SetCondition(condition, Rax);
            Emitter.MoveExtend(Rax, destination, Byte, Dword, signed: false);
        }
        else
        {
            Emitter.SetCondition(condition, destination);
        }
    }

    private static class Rom
    {
        internal static readonly float[] One = CreateOne();

        private static float[] CreateOne()
        {
            float[] value = GC.AllocateUninitializedArray<float>(1, pinned: true);
            value[0] = 1;
            return value;
        }

    }

    protected void EmitSseRegister(byte prefix, byte opcode, int destination, int source, bool wide = false)
    {
        EmitSsePrefix(prefix, destination, source, wide);
        Emitter.Append([0x0F, opcode, (byte)(0xC0 | (destination & 7) << 3 | (source & 7))]);
    }

    private void EmitSsePrefix(byte prefix, int reg, int rm, bool wide = false)
    {
        if (prefix != 0)
            Emitter.Append([prefix]);
        int rex = 0x40 | (wide ? 8 : 0) | (reg >> 3 << 2) | (rm >> 3);
        if (rex != 0x40)
            Emitter.Append([(byte)rex]);
    }

    protected void MoveFloatRegister(int target, int source) => EmitSseRegister(0xF3, 0x10, target, source);
    protected void MoveGeneralToFloat(int target, X64Register source) => EmitSseRegister(0x66, 0x6E, target, (int)source);
    protected void MoveFloatToGeneral(X64Register target, int source) => EmitSseRegister(0x66, 0x7E, source, (int)target);
    protected void LoadFloat(int target, X64Register address, int offset) => EmitSseMemory(0x10, target, address, offset);
    protected void StoreFloat(int source, X64Register address, int offset) => EmitSseMemory(0x11, source, address, offset);

    private void EmitSseMemory(byte opcode, int xmm, X64Register address, int offset)
    {
        int register = (int)address;
        EmitSsePrefix(0xF3, xmm, register);
        int rm = register & 7;
        int displacementMode = offset == 0 && rm != 5
            ? 0
            : offset is >= sbyte.MinValue and <= sbyte.MaxValue ? 1 : 2;
        Emitter.Append([0x0F, opcode, (byte)(displacementMode << 6 | (xmm & 7) << 3 | rm)]);
        if (rm == 4)
            Emitter.Append([0x24]);
        if (displacementMode == 1)
            Emitter.Append([unchecked((byte)offset)]);
        else if (displacementMode == 2)
            EmitUInt32(unchecked((uint)offset));
    }

    private void EmitAbsoluteInteger(byte opcode, X64Register register, uint address, bool indexed = false)
    {
        Emitter.Append([opcode]);
        EmitAbsoluteAddress((int)register, address, indexed);
    }

    private void EmitAbsoluteSse(byte opcode, int xmm, uint address, bool indexed = false)
    {
        Emitter.Append([0xF3, 0x0F, opcode]);
        EmitAbsoluteAddress(xmm, address, indexed);
    }

    private void EmitAbsoluteAddress(int register, uint address, bool indexed)
    {
        Emitter.Append([(byte)((register & 7) << 3 | (indexed ? 4 : 5))]);
        if (indexed)
            Emitter.Append([0xC5]);
        EmitUInt32(address);
    }

    private void EmitUInt32(uint value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        Emitter.Append(bytes);
    }

    private readonly record struct HostRegister(X64Register Integer, X64Width Width, int Float, bool IsFloat);

    protected void ShilParameterToHostRegister(Sh4ShilParameter parameter, X64Register target, X64Width width = Dword) =>
        ShilParameterToHostRegister(parameter, new HostRegister(target, width, 0, false));

    protected void ShilParameterToHostRegister(Sh4ShilParameter parameter, int xmm) =>
        ShilParameterToHostRegister(parameter, new HostRegister(default, default, xmm, true));

    private void ShilParameterToHostRegister(Sh4ShilParameter parameter, HostRegister target)
    {
        if (parameter.IsImmediate)
        {
            if (!target.IsFloat)
            {
                Emitter.MoveImmediate(parameter.Immediate, target.Integer, target.Width);
            }
            else
            {
                Emitter.MoveImmediate(parameter.Immediate, Rax, Dword);
                MoveGeneralToFloat(target.Float, Rax);
            }
        }
        else if (parameter.IsRegister)
        {
            if (Allocator.IsFloatAllocated(parameter))
            {
                if (parameter.IsFloat32 || parameter.IsFloat64)
                {
                    int source = MapXRegister(parameter, 0);
                    if (!target.IsFloat)
                        MoveFloatToGeneral(target.Integer, source);
                    else if (target.Float != source)
                        MoveFloatRegister(target.Float, source);
#if !XBYAK32
                    if (ArchX64 && parameter.IsFloat64)
                    {
                        source = MapXRegister(parameter, 1);
                        Sh4HostRuntime.Verify(target.IsFloat || target.Integer != Rax || target.Width != Qword);
                        MoveFloatToGeneral(Rax, source);
                        Emitter.Shift(X64Shift.Left, 32, Rax, Qword);
                        if (target.IsFloat || target.Width != Qword)
                            throw new X64EmitterException("bad combination");
                        Emitter.Binary(X64Arithmetic.Or, Rax, target.Integer, Qword);
                    }
#endif
                }
                else
                {
                    Sh4HostRuntime.Verify(!target.IsFloat);
                    if (ArchX64)
                    {
#if !XBYAK32
                        Emitter.MoveImmediate((long)parameter.GetRegisterPointer(Context), Rax, Qword);
                        Emitter.Load(Rax, 0, target.Integer, Dword);
#endif
                    }
                    else
                    {
                        EmitAbsoluteInteger(0x8B, target.Integer, (uint)parameter.GetRegisterPointer(Context));
                    }
                }
            }
            else if (Allocator.IsGeneralAllocated(parameter))
            {
                X64Register source = MapRegister(parameter);
                if (target.IsFloat)
                    MoveGeneralToFloat(target.Float, source);
                else if (target.Integer != source || target.Width != Dword)
                    Emitter.Move(source, target.Integer, Dword);
            }
            else
            {
                if (ArchX64)
                {
#if !XBYAK32
                    Emitter.MoveImmediate((long)parameter.GetRegisterPointer(Context), Rax, Qword);
                    if (!target.IsFloat)
                        Emitter.Load(Rax, 0, target.Integer, Dword);
                    else
                        LoadFloat(target.Float, Rax, 0);
#endif
                }
                else
                {
                    if (!target.IsFloat)
                        EmitAbsoluteInteger(0x8B, target.Integer, (uint)parameter.GetRegisterPointer(Context));
                    else
                        EmitAbsoluteSse(0x10, target.Float, (uint)parameter.GetRegisterPointer(Context));
                }
            }
        }
        else
        {
            Sh4HostRuntime.Verify(parameter.IsNull);
        }
    }

    protected void HostRegisterToShilParameter(Sh4ShilParameter parameter, X64Register source, X64Width width = Dword) =>
        HostRegisterToShilParameter(parameter, new HostRegister(source, width, 0, false));

    protected void HostRegisterToShilParameter(Sh4ShilParameter parameter, int xmm) =>
        HostRegisterToShilParameter(parameter, new HostRegister(default, default, xmm, true));

    private void HostRegisterToShilParameter(Sh4ShilParameter parameter, HostRegister source)
    {
        if (Allocator.IsGeneralAllocated(parameter))
        {
            X64Register target = MapRegister(parameter);
            if (!source.IsFloat)
                Emitter.Move(source.Integer, target, Dword);
            else
                MoveFloatToGeneral(target, source.Float);
        }
        else if (Allocator.IsFloatAllocated(parameter))
        {
            int target = MapXRegister(parameter, 0);
            if (!source.IsFloat)
                MoveGeneralToFloat(target, source.Integer);
            else if (source.Float != target)
                MoveFloatRegister(target, source.Float);
#if !XBYAK32
            if (ArchX64 && parameter.IsFloat64)
            {
                target = MapXRegister(parameter, 1);
                if (source.IsFloat)
                    throw new X64EmitterException("bad combination");
                Emitter.Shift(X64Shift.Right, 32, source.Integer, source.Width);
                MoveGeneralToFloat(target, source.Integer);
            }
#endif
        }
        else
        {
            if (ArchX64)
            {
#if !XBYAK32
                Emitter.MoveImmediate((long)parameter.GetRegisterPointer(Context), Rax, Qword);
                if (!source.IsFloat)
                    Emitter.Store(source.Integer, Rax, 0, Dword);
                else
                    StoreFloat(source.Float, Rax, 0);
#endif
            }
            else
            {
                if (!source.IsFloat)
                    EmitAbsoluteInteger(0x89, source.Integer, (uint)parameter.GetRegisterPointer(Context));
                else
                    EmitAbsoluteSse(0x11, source.Float, (uint)parameter.GetRegisterPointer(Context));
            }
        }
    }

    protected abstract X64Register MapRegister(Sh4ShilParameter parameter);

    protected abstract int MapXRegister(Sh4ShilParameter parameter, int index = 0);
}
#endif
