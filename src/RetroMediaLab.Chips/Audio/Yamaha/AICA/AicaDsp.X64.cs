using System.Diagnostics;
using RetroMediaLab.Chips.Common.Compilation.X64;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;

namespace RetroMediaLab.Chips.Audio.Yamaha.AICA;

using static X64Register;
using static X64Width;

#if !AICA_DSP_DYNAREC_NONE && !ARM7_DYNAREC_NONE
internal static unsafe partial class AicaDsp
{
    private const uint X64CodeBufferSize = 32 * 1024;
    private static readonly Lazy<nuint> X64DeclaredCodeBuffer =
        new(() => Sh4VirtualMemory.DeclareCodeCache(X64CodeBufferSize));
    private static nuint _x64CodeBuffer;
    private static nint _x64ExecutableOffset;

    private sealed class X64DspAssembler(nuint codeBuffer, nuint size)
    {
        private readonly X64Emitter _emitter = new();
        private DspState* _dsp;
        private static AicaDspData* Data => (AicaDspData*)DataAddress;

        internal void Compile(DspState* dsp)
        {
            _dsp = dsp;
            Debug.WriteLine($"DSPAssembler::DSPCompile recompiling for x86/64 at 0x{codeBuffer:x}");

            _emitter.Push(Rbx);
            _emitter.Push(Rbp);
            _emitter.Push(R12);
            _emitter.Push(R13);
            _emitter.Push(R14);
            _emitter.Push(R15);
            if (OperatingSystem.IsWindows())
                _emitter.AllocateStack(40);
            else
                _emitter.AllocateStack(8);

            _emitter.MoveImmediate((long)_dsp->TEMP, Rbx, Qword);
            _emitter.MoveImmediate((long)Data, Rbp, Qword);
            const X64Register inputs = R8;
            const X64Register acc = R12;
            const X64Register b = R9;
            const X64Register x = R10;
            const X64Register y = R11;
            const X64Register yRegister = R13;
            const X64Register addressRegister = R14;
            const X64Register mdec = R15;
            X64Register callArg0 = OperatingSystem.IsWindows() ? Rcx : Rdi;

            Binary(X64Arithmetic.Xor, acc, acc);
            _emitter.StoreImmediate(0, Rbx, DspOperand(&_dsp->FRC_REG), Dword);
            Binary(X64Arithmetic.Xor, yRegister, yRegister);
            Binary(X64Arithmetic.Xor, addressRegister, addressRegister);
            _emitter.Load(Rbx, DspOperand(&_dsp->MDEC_CT), mdec, Dword);

            for (int step = 0; step < 128; ++step)
            {
                uint* mpro = &Data->MPRO[step * 4];
                DecodeInstruction(mpro, out Instruction op);
                int coefficient = step;

                if (op.XSEL || op.YRL || (op.ADRL && op.SHIFT != 3))
                {
                    if (op.IRA <= 0x1f)
                        _emitter.Load(Rbx, DspOperand(_dsp->MEMS, op.IRA), inputs, Dword);
                    else if (op.IRA <= 0x2f)
                    {
                        _emitter.Load(Rbx, DspOperand(_dsp->MIXS, op.IRA - 0x20), inputs, Dword);
                        Shift(X64Shift.Left, inputs, 4);
                    }
                    else if (op.IRA <= 0x31)
                    {
                        _emitter.Load(Rbx, DspOperand(Data->EXTS, op.IRA - 0x30), inputs, Dword);
                        Shift(X64Shift.Left, inputs, 8);
                    }
                    else
                        Binary(X64Arithmetic.Xor, inputs, inputs);
                }

                if (op.IWT)
                {
                    _emitter.Load(Rbx, DspOperand(_dsp->MEMVAL, step & 3), Rax, Dword);
                    _emitter.Store(Rax, Rbx, DspOperand(_dsp->MEMS, op.IWA), Dword);
                }

                if (!op.ZERO)
                {
                    if (op.BSEL)
                        Move(b, acc);
                    else
                    {
                        Move(Rax, mdec);
                        if (op.TRA != 0)
                            Binary(X64Arithmetic.Add, Rax, op.TRA);
                        Binary(X64Arithmetic.And, Rax, 0x7f);
                        _emitter.LoadIndexed(Rbx, Rax, 2, 0, b, Dword);
                    }
                    if (op.NEGB)
                        _emitter.Unary(X64Unary.Negate, b, Dword);
                }

                X64Register xAlias = x;
                if (op.XSEL)
                    xAlias = inputs;
                else
                {
                    if (!op.ZERO && !op.BSEL && !op.NEGB)
                        xAlias = b;
                    else
                    {
                        Move(Rax, mdec);
                        if (op.TRA != 0)
                            Binary(X64Arithmetic.Add, Rax, op.TRA);
                        Binary(X64Arithmetic.And, Rax, 0x7f);
                        _emitter.LoadIndexed(Rbx, Rax, 2, 0, x, Dword);
                    }
                }

                if (op.YSEL == 0)
                    _emitter.Load(Rbx, DspOperand(&_dsp->FRC_REG), y, Dword);
                else if (op.YSEL == 1)
                {
                    _emitter.LoadExtend(Rbp, DspDataOperand(Data->COEF, coefficient), y, Word, Dword, signed: true);
                    Shift(X64Shift.ArithmeticRight, y, 3);
                }
                else if (op.YSEL == 2)
                {
                    Move(y, yRegister);
                    Shift(X64Shift.ArithmeticRight, y, 11);
                }
                else if (op.YSEL == 3)
                {
                    Move(y, yRegister);
                    Shift(X64Shift.ArithmeticRight, y, 4);
                    Binary(X64Arithmetic.And, y, 0x0fff);
                }

                if (op.YRL)
                    Move(yRegister, inputs);

                if (op.TWT || op.FRCL || op.MWT || (op.ADRL && op.SHIFT == 3) || op.EWT)
                {
                    if (op.SHIFT == 0)
                    {
                        Binary(X64Arithmetic.Compare, acc, 0xff800000);
                        Move(Rdx, 0xff800000);
                        Move(Rax, 0x007fffff);
                        ConditionalMove(X64Condition.GreaterOrEqual, Rdx, acc);
                        Binary(X64Arithmetic.Compare, Rdx, 0x007fffff);
                        ConditionalMove(X64Condition.Greater, Rdx, Rax);
                    }
                    else if (op.SHIFT == 1)
                    {
                        Move(Rcx, acc);
                        Shift(X64Shift.Left, Rcx, 1);
                        Binary(X64Arithmetic.Compare, Rcx, 0xff800000);
                        Move(Rdx, 0xff800000);
                        Move(Rax, 0x007fffff);
                        ConditionalMove(X64Condition.GreaterOrEqual, Rdx, Rcx);
                        Binary(X64Arithmetic.Compare, Rdx, 0x007fffff);
                        ConditionalMove(X64Condition.Greater, Rdx, Rax);
                    }
                    else if (op.SHIFT == 2)
                    {
                        Move(Rdx, acc);
                        Shift(X64Shift.Left, Rdx, 1);
                    }
                    else if (op.SHIFT == 3)
                        Move(Rdx, acc);
                }

                _emitter.MoveExtend(xAlias, xAlias, Dword, Qword, signed: true);
                _emitter.MoveExtend(y, Rax, Dword, Qword, signed: true);
                _emitter.MultiplySigned(xAlias, Rax, Qword);
                Shift(X64Shift.ArithmeticRight, Rax, 12, Qword);
                Move(acc, Rax);
                if (!op.ZERO)
                    Binary(X64Arithmetic.Add, acc, b);

                if (op.TWT)
                {
                    Move(Rcx, mdec);
                    if (op.TWA != 0)
                        Binary(X64Arithmetic.Add, Rcx, op.TWA);
                    Binary(X64Arithmetic.And, Rcx, 0x7f);
                    _emitter.StoreIndexed(Rdx, Rbx, Rcx, 2, 0, Dword);
                }

                if (op.FRCL)
                {
                    Move(Rcx, Rdx);
                    if (op.SHIFT == 3)
                        Binary(X64Arithmetic.And, Rcx, 0xfff);
                    else
                        Shift(X64Shift.ArithmeticRight, Rcx, 11);
                    _emitter.Store(Rcx, Rbx, DspOperand(&_dsp->FRC_REG), Dword);
                }

                if ((step & 1) != 0)
                {
                    if (op.MRD || op.MWT)
                    {
                        if ((op.ADRL && op.SHIFT == 3) || op.EWT)
                            _emitter.Push(Rdx);
                        if (op.ADRL && op.SHIFT != 3)
                            _emitter.Push(inputs);
                    }

                    const X64Register address = y;
                    if (op.MRD)
                    {
                        CalculateAddress(address, op, addressRegister, mdec);
                        _emitter.MoveImmediate((long)AicaInterface.Ram, Rcx, Qword);
                        _emitter.LoadExtendIndexed(Rcx, address, 1, 0, callArg0, Word, Dword, signed: false);
                        GenerateCall(UnpackEntry);
                        _emitter.Store(Rax, Rbx, DspOperand(&_dsp->MEMVAL[(step + 2) & 3]), Dword);
                    }
                    if (op.MWT)
                    {
                        Move(callArg0, Rdx);
                        GenerateCall(PackEntry);

                        CalculateAddress(address, op, addressRegister, mdec);
                        _emitter.MoveImmediate((long)AicaInterface.Ram, Rcx, Qword);
                        _emitter.StoreIndexed(Rax, Rcx, address, 0, 0, Word);
                    }
                    if (op.MRD || op.MWT)
                    {
                        if (op.ADRL && op.SHIFT != 3)
                            _emitter.Pop(inputs);
                        if ((op.ADRL && op.SHIFT == 3) || op.EWT)
                            _emitter.Pop(Rdx);
                    }
                }

                if (op.ADRL)
                {
                    if (op.SHIFT == 3)
                    {
                        Move(addressRegister, Rdx);
                        Shift(X64Shift.ArithmeticRight, addressRegister, 12);
                    }
                    else
                    {
                        Move(addressRegister, inputs);
                        Shift(X64Shift.ArithmeticRight, addressRegister, 16);
                    }
                }

                if (op.EWT)
                {
                    Shift(X64Shift.ArithmeticRight, Rdx, 8);
                    _emitter.Store(Rdx, Rbp, DspDataOperand(Data->EFREG, op.EWA), Dword);
                }
            }

            Move(Rax, unchecked(_dsp->RBL + 1));
            Binary(X64Arithmetic.Subtract, mdec, 1);
            ConditionalMove(X64Condition.Zero, mdec, Rax);
            _emitter.Store(mdec, Rbx, DspOperand(&_dsp->MDEC_CT), Dword);

            if (OperatingSystem.IsWindows())
                _emitter.ReleaseStack(40);
            else
                _emitter.ReleaseStack(8);
            _emitter.Pop(R15);
            _emitter.Pop(R14);
            _emitter.Pop(R13);
            _emitter.Pop(R12);
            _emitter.Pop(Rbp);
            _emitter.Pop(Rbx);

            _emitter.Return();
            Ready();
        }

        private int DspOperand(void* data, int index = 0, uint elementSize = 4) =>
            checked((int)((byte*)data - (byte*)_dsp->TEMP + index * (long)elementSize));

        private static int DspDataOperand(void* data, int index = 0, uint elementSize = 4) =>
            checked((int)((byte*)data - (byte*)Data + index * (long)elementSize));

        private void CalculateAddress(X64Register address, Instruction op, X64Register addressRegister, X64Register mdec)
        {
            _emitter.Load(Rbp, DspDataOperand(Data->MADRS, op.MASA), address, Dword);
            if (op.ADREB)
            {
                Move(Rcx, addressRegister);
                Binary(X64Arithmetic.And, Rcx, 0x0fff);
                Binary(X64Arithmetic.Add, address, Rcx);
            }
            if (op.NXADR)
                Binary(X64Arithmetic.Add, address, 1);
            if (!op.TABLE)
            {
                Binary(X64Arithmetic.Add, address, mdec);
                Binary(X64Arithmetic.And, address, _dsp->RBL);
            }
            else
                Binary(X64Arithmetic.And, address, 0xffff);

            Shift(X64Shift.Left, address, 1);
            Binary(X64Arithmetic.Add, address, _dsp->RBP);
            Binary(X64Arithmetic.And, address, AicaInterface.RamMask);
        }

        private void GenerateCall(nuint function) =>
            _emitter.CallRelativeAddress(codeBuffer, unchecked(function - (nuint)_x64ExecutableOffset));

        private void Move(X64Register destination, X64Register source) =>
            _emitter.Move(source, destination, Dword);

        private void Move(X64Register destination, uint immediate) =>
            _emitter.MoveImmediate(immediate, destination, Dword);

        private void Binary(X64Arithmetic operation, X64Register destination, X64Register source) =>
            _emitter.Binary(operation, source, destination, Dword);

        private void Binary(X64Arithmetic operation, X64Register destination, uint immediate) =>
            _emitter.BinaryImmediate(operation, unchecked((int)immediate), destination, Dword);

        private void Shift(X64Shift operation, X64Register register, byte count, X64Width width = Dword) =>
            _emitter.Shift(operation, count, register, width);

        private void ConditionalMove(X64Condition condition, X64Register destination, X64Register source)
        {
            int rex = 0x40 | (((int)destination & 8) >> 1) | (((int)source & 8) >> 3);
            if (rex != 0x40)
                _emitter.Append([(byte)rex]);
            int modrm = 0xc0 | (((int)destination & 7) << 3) | ((int)source & 7);
            _emitter.Append([0x0f, (byte)(0x40 + (int)condition), (byte)modrm]);
        }

        private void Ready()
        {
            byte[] bytes = _emitter.ToArray();
            if ((nuint)bytes.Length > size)
                throw new X64EmitterException("code is too big");
            bytes.AsSpan().CopyTo(new Span<byte>((void*)codeBuffer, bytes.Length));
        }
    }

    internal static void RecompileX64()
    {
        Sh4VirtualMemory.SetJitExecutable(_x64CodeBuffer, X64CodeBufferSize, false);
        var assembler = new X64DspAssembler(_x64CodeBuffer, X64CodeBufferSize);
        try
        {
            assembler.Compile((DspState*)StateAddress);
        }
        catch (X64EmitterException exception)
        {
            Sh4VirtualMemory.SetJitExecutable(_x64CodeBuffer, X64CodeBufferSize, true);
            Trace.TraceError($"Xbyak error: {exception.Message}");
            throw new InvalidOperationException($"DSP recompilation error: {exception.Message}");
        }
        Sh4VirtualMemory.SetJitExecutable(_x64CodeBuffer, X64CodeBufferSize, true);
    }

    internal static void InitializeX64()
    {
#if SH4_NO_RWX_PAGES
        if (!Sh4VirtualMemory.PrepareJitBlock(X64DeclaredCodeBuffer.Value, X64CodeBufferSize,
                out _x64CodeBuffer, out _x64ExecutableOffset))
#else
        if (!Sh4VirtualMemory.PrepareJitBlock(X64DeclaredCodeBuffer.Value, X64CodeBufferSize, out _x64CodeBuffer))
#endif
            Sh4HostRuntime.Die("virtmem::prepare_jit_block failed in x64 dsp");
    }

    internal static void TerminateX64()
    {
#if SH4_NO_RWX_PAGES
        if (_x64CodeBuffer != 0)
            Sh4VirtualMemory.ReleaseJitBlock(X64DeclaredCodeBuffer.Value, _x64CodeBuffer, X64CodeBufferSize);
#else
        if (_x64CodeBuffer != 0 && _x64CodeBuffer != X64DeclaredCodeBuffer.Value)
            Sh4VirtualMemory.ReleaseJitBlock(_x64CodeBuffer, X64CodeBufferSize);
#endif
        _x64CodeBuffer = 0;
    }

    internal static void RunStepX64() => ((delegate* unmanaged<void>)_x64CodeBuffer)();
}
#endif
