using System.Buffers.Binary;
using System.Diagnostics;
using RetroMediaLab.Chips.Common.Compilation.X64;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;

namespace RetroMediaLab.Chips.Audio.Yamaha.AICA;

using static X64Register;
using static X64Width;

#if !AICA_DSP_DYNAREC_NONE && !ARM7_DYNAREC_NONE
internal static unsafe partial class AicaDsp
{
    private const uint X86CodeBufferSize = 32 * 1024;
    private static readonly Lazy<nuint> X86DeclaredCodeBuffer =
        new(() => Sh4VirtualMemory.DeclareCodeCache(X86CodeBufferSize));
    private static nuint _x86CodeBuffer;

    private sealed class X86DspAssembler(nuint codeBuffer, nuint size)
    {
        private readonly X64Emitter _emitter = new();
        private DspState* _dsp;
        private static AicaDspData* Data => (AicaDspData*)DataAddress;

        internal void Compile(DspState* dsp)
        {
            _dsp = dsp;
            Debug.WriteLine($"X86DSPAssembler::Compile recompiling for x86 at 0x{codeBuffer:x}");

            _emitter.Push(Rsi);
            _emitter.Push(Rdi);
            _emitter.Push(Rbp);
            _emitter.Push(Rbx);
            if (!OperatingSystem.IsWindows())
                Binary(X64Arithmetic.Subtract, Rsp, 12);

            const X64Register inputs = Rsi;
            const X64Register acc = Rdi;
            const X64Register x = Rbp;
            const X64Register y = Rbx;

            Binary(X64Arithmetic.Xor, acc, acc);
            StoreImmediate(&_dsp->FRC_REG, 0);
            StoreImmediate(&_dsp->Y_REG, 0);
            StoreImmediate(&_dsp->ADRS_REG, 0);

            for (int step = 0; step < 128; ++step)
            {
                uint* mpro = &Data->MPRO[step * 4];
                DecodeInstruction(mpro, out Instruction op);
                int coefficient = step;

                if (op.XSEL || op.YRL || (op.ADRL && op.SHIFT != 3))
                {
                    if (op.IRA <= 0x1f)
                        Load(inputs, &_dsp->MEMS[op.IRA]);
                    else if (op.IRA <= 0x2f)
                    {
                        Load(inputs, &_dsp->MIXS[op.IRA - 0x20]);
                        Shift(X64Shift.Left, inputs, 4);
                    }
                    else if (op.IRA <= 0x31)
                    {
                        Load(inputs, &Data->EXTS[op.IRA - 0x30]);
                        Shift(X64Shift.Left, inputs, 8);
                    }
                    else
                        Binary(X64Arithmetic.Xor, inputs, inputs);
                }

                if (op.IWT)
                {
                    Load(Rax, &_dsp->MEMVAL[step & 3]);
                    Store(&_dsp->MEMS[op.IWA], Rax);
                }

                if (!op.ZERO)
                {
                    if (op.BSEL)
                        Store(&_dsp->B, acc);
                    else
                    {
                        Load(Rax, &_dsp->MDEC_CT);
                        if (op.TRA != 0)
                            Binary(X64Arithmetic.Add, Rax, op.TRA);
                        Binary(X64Arithmetic.And, Rax, 0x7f);
                        _emitter.LoadAbsoluteIndexed((uint)_dsp->TEMP, Rax, 4, Rax, Dword);
                        Store(&_dsp->B, Rax);
                    }
                    if (op.NEGB)
                        AbsoluteOperand(0xf7, 3, &_dsp->B);
                }

                X64Register xAlias = x;
                if (op.XSEL)
                    xAlias = inputs;
                else
                {
                    if (!op.ZERO && !op.BSEL && !op.NEGB)
                        Load(x, &_dsp->B);
                    else
                    {
                        Load(Rax, &_dsp->MDEC_CT);
                        if (op.TRA != 0)
                            Binary(X64Arithmetic.Add, Rax, op.TRA);
                        Binary(X64Arithmetic.And, Rax, 0x7f);
                        _emitter.LoadAbsoluteIndexed((uint)_dsp->TEMP, Rax, 4, x, Dword);
                    }
                }

                if (op.YSEL == 0)
                    Load(y, &_dsp->FRC_REG);
                else if (op.YSEL == 1)
                {
                    _emitter.Append([0x0f]);
                    AbsoluteOperand(0xbf, (int)y, &Data->COEF[coefficient]);
                    Shift(X64Shift.ArithmeticRight, y, 3);
                }
                else if (op.YSEL == 2)
                {
                    Load(y, &_dsp->Y_REG);
                    Shift(X64Shift.ArithmeticRight, y, 11);
                }
                else if (op.YSEL == 3)
                {
                    Load(y, &_dsp->Y_REG);
                    Shift(X64Shift.ArithmeticRight, y, 4);
                    Binary(X64Arithmetic.And, y, 0x0fff);
                }

                if (op.YRL)
                    Store(&_dsp->Y_REG, inputs);

                if (op.TWT || op.FRCL || op.MWT || (op.ADRL && op.SHIFT == 3) || op.EWT)
                {
                    if (op.SHIFT == 0)
                    {
                        Binary(X64Arithmetic.Compare, acc, 0xff800000);
                        Move(Rcx, 0xff800000);
                        Move(Rax, 0x007fffff);
                        ConditionalMove(X64Condition.GreaterOrEqual, Rcx, acc);
                        Binary(X64Arithmetic.Compare, Rcx, 0x007fffff);
                        ConditionalMove(X64Condition.Greater, Rcx, Rax);
                    }
                    else if (op.SHIFT == 1)
                    {
                        Move(Rdx, acc);
                        Shift(X64Shift.Left, Rdx, 1);
                        Binary(X64Arithmetic.Compare, Rdx, 0xff800000);
                        Move(Rcx, 0xff800000);
                        Move(Rax, 0x007fffff);
                        ConditionalMove(X64Condition.GreaterOrEqual, Rcx, Rdx);
                        Binary(X64Arithmetic.Compare, Rcx, 0x007fffff);
                        ConditionalMove(X64Condition.Greater, Rcx, Rax);
                    }
                    else if (op.SHIFT == 2)
                    {
                        Move(Rcx, acc);
                        Shift(X64Shift.Left, Rcx, 1);
                    }
                    else if (op.SHIFT == 3)
                        Move(Rcx, acc);
                }

                Move(Rax, xAlias);
                _emitter.Unary(X64Unary.MultiplySigned, y, Dword);
                Shift(X64Shift.Left, Rdx, 20);
                Shift(X64Shift.Right, Rax, 12);
                Binary(X64Arithmetic.Or, Rax, Rdx);
                Move(acc, Rax);
                if (!op.ZERO)
                    AbsoluteOperand(0x03, (int)acc, &_dsp->B);

                if (op.TWT)
                {
                    Load(Rdx, &_dsp->MDEC_CT);
                    if (op.TWA != 0)
                        Binary(X64Arithmetic.Add, Rdx, op.TWA);
                    Binary(X64Arithmetic.And, Rdx, 0x7f);
                    StoreTempIndexed(Rcx, Rdx);
                }

                if (op.FRCL)
                {
                    Move(Rdx, Rcx);
                    if (op.SHIFT == 3)
                        Binary(X64Arithmetic.And, Rdx, 0xfff);
                    else
                        Shift(X64Shift.ArithmeticRight, Rdx, 11);
                    Store(&_dsp->FRC_REG, Rdx);
                }

                if ((step & 1) != 0)
                {
                    if (op.MRD || op.MWT)
                    {
                        if ((op.ADRL && op.SHIFT == 3) || op.EWT)
                            _emitter.Push(Rcx);
                    }
                    const X64Register address = y;
                    if (op.MWT)
                    {
                        Call(PackEntry);

                        CalculateAddress(address, op);
                        Move(Rcx, (uint)AicaInterface.Ram);
                        _emitter.StoreIndexed(Rax, Rcx, address, 0, 0, Word);
                    }
                    if (op.MRD)
                    {
                        CalculateAddress(address, op);
                        Move(Rcx, (uint)AicaInterface.Ram);
                        _emitter.LoadExtendIndexed(Rcx, address, 1, 0, Rcx, Word, Dword, signed: false);
                        Call(UnpackEntry);
                        Store(&_dsp->MEMVAL[(step + 2) & 3], Rax);
                    }
                    if (op.MRD || op.MWT)
                    {
                        if ((op.ADRL && op.SHIFT == 3) || op.EWT)
                            _emitter.Pop(Rcx);
                    }
                }

                if (op.ADRL)
                {
                    if (op.SHIFT == 3)
                    {
                        Store(&_dsp->ADRS_REG, Rcx);
                        ShiftAbsoluteRight(&_dsp->ADRS_REG, 12);
                    }
                    else
                    {
                        Store(&_dsp->ADRS_REG, inputs);
                        ShiftAbsoluteRight(&_dsp->ADRS_REG, 16);
                    }
                }

                if (op.EWT)
                {
                    Shift(X64Shift.ArithmeticRight, Rcx, 8);
                    Store(&Data->EFREG[op.EWA], Rcx);
                }
            }

            Move(Rax, unchecked(_dsp->RBL + 1));
            Load(Rcx, &_dsp->MDEC_CT);
            Binary(X64Arithmetic.Subtract, Rcx, 1);
            ConditionalMove(X64Condition.Zero, Rcx, Rax);
            Store(&_dsp->MDEC_CT, Rcx);

            if (!OperatingSystem.IsWindows())
                Binary(X64Arithmetic.Add, Rsp, 12);
            _emitter.Pop(Rbx);
            _emitter.Pop(Rbp);
            _emitter.Pop(Rdi);
            _emitter.Pop(Rsi);

            _emitter.Return();
            Ready();
        }

        private void CalculateAddress(X64Register address, Instruction op)
        {
            Load(address, &Data->MADRS[op.MASA]);
            if (op.ADREB)
            {
                Load(Rcx, &_dsp->ADRS_REG);
                Binary(X64Arithmetic.And, Rcx, 0x0fff);
                Binary(X64Arithmetic.Add, address, Rcx);
            }
            if (op.NXADR)
                Binary(X64Arithmetic.Add, address, 1);
            if (!op.TABLE)
            {
                AbsoluteOperand(0x03, (int)address, &_dsp->MDEC_CT);
                Binary(X64Arithmetic.And, address, _dsp->RBL);
            }
            else
                Binary(X64Arithmetic.And, address, 0xffff);

            Shift(X64Shift.Left, address, 1);
            Binary(X64Arithmetic.Add, address, _dsp->RBP);
            Binary(X64Arithmetic.And, address, AicaInterface.RamMask);
        }

        private void Move(X64Register destination, X64Register source) =>
            _emitter.Move(source, destination, Dword);

        private void Move(X64Register destination, uint immediate) =>
            _emitter.MoveImmediate(immediate, destination, Dword);

        private void Binary(X64Arithmetic operation, X64Register destination, X64Register source) =>
            _emitter.Binary(operation, source, destination, Dword);

        private void Binary(X64Arithmetic operation, X64Register destination, uint immediate) =>
            _emitter.BinaryImmediate(operation, unchecked((int)immediate), destination, Dword);

        private void Shift(X64Shift operation, X64Register register, byte count) =>
            _emitter.Shift(operation, count, register, Dword);

        private void ConditionalMove(X64Condition condition, X64Register destination, X64Register source)
        {
            int modrm = 0xc0 | ((int)destination << 3) | (int)source;
            _emitter.Append([0x0f, (byte)(0x40 + (int)condition), (byte)modrm]);
        }

        private void AbsoluteOperand(byte opcode, int register, void* address)
        {
            Span<byte> bytes = stackalloc byte[6];
            bytes[0] = opcode;
            bytes[1] = (byte)((register << 3) | 5);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes[2..], (uint)address);
            _emitter.Append(bytes);
        }

        private void Load(X64Register destination, void* address) =>
            AbsoluteOperand(0x8b, (int)destination, address);

        private void Store(void* address, X64Register source) =>
            AbsoluteOperand(0x89, (int)source, address);

        private void StoreImmediate(void* address, uint value)
        {
            AbsoluteOperand(0xc7, 0, address);
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
            _emitter.Append(bytes);
        }

        private void StoreTempIndexed(X64Register source, X64Register index)
        {
            Span<byte> bytes = stackalloc byte[7];
            bytes[0] = 0x89;
            bytes[1] = (byte)(((int)source << 3) | 4);
            bytes[2] = (byte)(0x85 | ((int)index << 3));
            BinaryPrimitives.WriteUInt32LittleEndian(bytes[3..], (uint)_dsp->TEMP);
            _emitter.Append(bytes);
        }

        private void ShiftAbsoluteRight(void* address, byte count)
        {
            AbsoluteOperand(0xc1, 7, address);
            _emitter.Append([count]);
        }

        private void Call(nuint target)
        {
            Span<byte> bytes = stackalloc byte[5];
            bytes[0] = 0xe8;
            uint displacement = unchecked((uint)target - (uint)codeBuffer - (uint)_emitter.Position - 5);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes[1..], displacement);
            _emitter.Append(bytes);
        }

        private void Ready()
        {
            byte[] bytes = _emitter.ToArray();
            if ((nuint)bytes.Length > size)
                throw new X64EmitterException("code is too big");
            bytes.AsSpan().CopyTo(new Span<byte>((void*)codeBuffer, bytes.Length));
        }
    }

    internal static void RecompileX86()
    {
        var assembler = new X86DspAssembler(_x86CodeBuffer, X86CodeBufferSize);
        try
        {
            assembler.Compile((DspState*)StateAddress);
        }
        catch (X64EmitterException exception)
        {
            Trace.TraceError($"Xbyak error: {exception.Message}");
            throw new InvalidOperationException($"DSP recompilation error: {exception.Message}");
        }
    }

    internal static void InitializeX86()
    {
        if (!Sh4VirtualMemory.PrepareJitBlock(X86DeclaredCodeBuffer.Value, X86CodeBufferSize, out _x86CodeBuffer))
            Sh4HostRuntime.Die("mprotect failed in x86 dsp");
    }

    internal static void TerminateX86()
    {
        if (_x86CodeBuffer != 0 && _x86CodeBuffer != X86DeclaredCodeBuffer.Value)
            Sh4VirtualMemory.ReleaseJitBlock(_x86CodeBuffer, X86CodeBufferSize);
        _x86CodeBuffer = 0;
    }

    internal static void RunStepX86() => ((delegate* unmanaged<void>)_x86CodeBuffer)();
}
#endif
