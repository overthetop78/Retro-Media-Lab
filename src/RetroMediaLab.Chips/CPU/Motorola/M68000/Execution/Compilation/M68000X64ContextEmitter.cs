using RetroMediaLab.Chips.Common.Compilation.X64;
using System.Runtime.InteropServices;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using S = RetroMediaLab.Chips.Common.Compilation.X64.X64Shift;
using C = RetroMediaLab.Chips.Common.Compilation.X64.X64Condition;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

internal enum M68000StateField
{
    cycles, refresh_cycles, cycle_end, pc, prev_pc, ir,
    t1_flag, s_flag, x_flag, n_flag, not_z_flag, v_flag, c_flag,
    int_mask, int_level, stopped, pref_addr, pref_data,
    instr_mode, run_mode, aerr_enabled, aerr_address, aerr_write_mode, aerr_fc,
    tracing, address_space, poll_pc, poll_cycle, poll_detected
}
/// <summary>
/// Adapts BlastEm's context/register/flag emission to the existing Cpu68000State
/// and uint[16] layout. Neither pointer is owned. Only the architectural flag bits used by the
/// existing core are materialized; unused payload bits are not reconstructed.
/// </summary>
internal sealed class M68000X64ContextEmitter
{
    private readonly X64Emitter _code;
    private readonly R _registers;
    private readonly R _state;
    public M68000X64ContextEmitter(X64Emitter code, R registers = R.Rcx, R state = R.Rdx)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (registers == state || registers > R.R15 || state > R.R15)
            throw new ArgumentException("Distinct full registers must hold the two context pointers.");
        _code = code; _registers = registers; _state = state;
    }
    // Field IDs avoid both interop and string lookups while emitting blocks.
    // Resolve the actual CLR layout once; never assume the struct packing.
    private static readonly int[] StateOffsets = Enum.GetNames<M68000StateField>()
        .Select(field => checked((int)Marshal.OffsetOf<Cpu68000State>(field))).ToArray();
    public static int StateOffset(M68000StateField field) => StateOffsets[(int)field];
    private static int RegisterOffset(int index)
    {
        if ((uint)index >= 16) throw new ArgumentOutOfRangeException(nameof(index));
        return index * sizeof(uint);
    }
    private void Host(R register)
    {
        if (register > R.R15 || register == _registers || register == _state)
            throw new ArgumentException("A value register must not overwrite a context pointer.", nameof(register));
    }
    internal static W Width(M68000OperandSize size) => size switch
    {
        M68000OperandSize.Byte => W.Byte, M68000OperandSize.Word => W.Word,
        M68000OperandSize.Long => W.Dword, _ => throw new ArgumentOutOfRangeException(nameof(size))
    };
    public void LoadRegister(int index, R destination, M68000OperandSize size, bool signed = false)
    {
        int offset = RegisterOffset(index); Host(destination);
        W width = Width(size);
        if (CachedRegister(index) is { } cached)
        {
            if (width == W.Dword) _code.Move(cached, destination, width);
            else _code.MoveExtend(cached, destination, width, W.Dword, signed);
        }
        else if (width == W.Dword) _code.Load(_registers, offset, destination, W.Dword);
        else _code.LoadExtend(_registers, offset, destination, width, W.Dword, signed);
    }
    public void StoreRegister(int index, R source, M68000OperandSize size)
    {
        int offset = RegisterOffset(index); Host(source);
        if (CachedRegister(index) is { } cached) _code.Move(source, cached, Width(size));
        else _code.Store(source, _registers, offset, Width(size));
    }
    private R? CachedRegister(int index) => _code.CacheGuestRegisters && _registers == R.Rcx ? index switch
    {
        0 => R.R12, 1 => R.Rdi, 8 => R.Rsi, 15 => R.Rbx, _ => null
    } : null;

    public void LoadInstructionWord(int offset, R destination)
    {
        if (_code.CacheGuestRegisters)
        {
            _code.MoveImmediate(unchecked(_code.ReadOnlyDataBase + offset), destination, W.Qword);
            _code.LoadExtend(destination, 0, destination, W.Word, W.Dword, false);
        }
        else _code.LoadExtend(R.R12, offset, destination, W.Word, W.Dword, false);
    }
    internal void ObserveCodeFetch(uint address, List<int> faults)
    {
        if (!_code.ObserveInstructionFetches) return;
        _code.MoveImmediate(address, R.R8, W.Dword);
        new M68000X64BusEmitter(_code, faults).EmitControl(Cpu68000NativeControl.ObserveCodeFetch, R.R8, R.R8);
    }
    public void LoadState(M68000StateField field, R destination)
    {
        Host(destination);
        if (Cached(field) is { } cached) _code.Move(cached, destination, W.Dword);
        else _code.Load(_state, StateOffset(field), destination, W.Dword);
    }
    public void StoreState(M68000StateField field, R source)
    {
        Host(source);
        if (Cached(field) is { } cached) _code.Move(source, cached, W.Dword);
        else _code.Store(source, _state, StateOffset(field), W.Dword);
    }
    public void StoreStateImmediate(M68000StateField field, uint value)
    {
        if (Cached(field) is { } cached) _code.MoveImmediate(value, cached, W.Dword);
        else _code.StoreImmediate(unchecked((int)value), _state, StateOffset(field), W.Dword);
    }
    private R? Cached(M68000StateField field) => _code.CacheExecutionState && _state == R.Rdx ? field switch
    {
        M68000StateField.cycles => R.Rbp,
        M68000StateField.pc when !_code.CacheGuestRegisters => R.Rsi,
        M68000StateField.pref_data when _code.CachePrefetch && !_code.CacheGuestRegisters => R.Rdi,
        _ => null
    } : null;
    public void InitializeHostState()
    {
        if (!_code.CacheExecutionState) return;
        _code.Load(_state, StateOffset(M68000StateField.cycles), R.Rbp, W.Dword);
        if (_code.CacheGuestRegisters)
        {
            foreach (int index in new[] { 0, 1, 8, 15 })
                _code.Load(_registers, RegisterOffset(index), CachedRegister(index)!.Value, W.Dword);
            return;
        }
        _code.Load(_state, StateOffset(M68000StateField.pc), R.Rsi, W.Dword);
        if (_code.CachePrefetch) _code.Load(_state, StateOffset(M68000StateField.pref_data), R.Rdi, W.Dword);
    }
    public void FlushHostState()
    {
        if (!_code.CacheExecutionState) return;
        _code.Store(R.Rbp, _state, StateOffset(M68000StateField.cycles), W.Dword);
        if (_code.CacheGuestRegisters)
        {
            foreach (int index in new[] { 0, 1, 8, 15 })
                _code.Store(CachedRegister(index)!.Value, _registers, RegisterOffset(index), W.Dword);
            return;
        }
        _code.Store(R.Rsi, _state, StateOffset(M68000StateField.pc), W.Dword);
        if (_code.CachePrefetch) _code.Store(R.Rdi, _state, StateOffset(M68000StateField.pref_data), W.Dword);
    }
    public void AddStateImmediate(M68000StateField field, int value)
    {
        if (Cached(field) is { } cached) _code.BinaryImmediate(A.Add, value, cached, W.Dword);
        else _code.BinaryMemoryImmediate(A.Add, value, _state, StateOffset(field), W.Dword);
    }
    public void AddState(M68000StateField field, R value)
    {
        Host(value);
        if (Cached(field) is { } cached) _code.Binary(A.Add, value, cached, W.Dword);
        else _code.BinaryStore(A.Add, value, _state, StateOffset(field), W.Dword);
    }

    private void Scratch(R result, R scratch)
    {
        Host(result); Host(scratch);
        if (result == scratch) throw new ArgumentException("Result and scratch registers must be distinct.");
    }
    public void LogicalFlags(R result, M68000OperandSize size, R scratch)
    {
        Scratch(result, scratch);
        W width = Width(size);
        if (width == W.Dword) _code.Move(result, scratch, W.Dword);
        else _code.MoveExtend(result, scratch, width, W.Dword, false);
        StoreState(M68000StateField.not_z_flag, scratch);
        int signShift = size == M68000OperandSize.Byte ? 0 : size == M68000OperandSize.Word ? 8 : 24;
        if (signShift != 0) _code.Shift(S.Right, (byte)signShift, scratch, W.Dword);
        StoreState(M68000StateField.n_flag, scratch);
        StoreStateImmediate(M68000StateField.v_flag, 0);
        StoreStateImmediate(M68000StateField.c_flag, 0);
        // X is preserved. Only bit 7 of N is consumed by the existing core.
    }
    // Called immediately after an ALU operation at the guest's byte/word/long
    // width. Capture every host condition before NEG changes the host flags.
    public void CaptureArithmeticFlags(bool updateExtend)
    {
        StoreStateImmediate(M68000StateField.not_z_flag, 0);
        Condition(M68000StateField.n_flag, C.Sign);
        Condition(M68000StateField.v_flag, C.Overflow);
        Condition(M68000StateField.c_flag, C.Carry, 1);
        Condition(M68000StateField.not_z_flag, C.NotZero);
        if (updateExtend) Condition(M68000StateField.x_flag, C.Carry, 1);
        MaskByte(M68000StateField.n_flag);
        MaskByte(M68000StateField.v_flag);
        MaskByte(M68000StateField.c_flag, 1);
        if (updateExtend) MaskByte(M68000StateField.x_flag, 1);
    }
    public void ZeroFlagFromCarry()
    {
        StoreStateImmediate(M68000StateField.not_z_flag, 0);
        Condition(M68000StateField.not_z_flag, C.Carry);
    }
    private void Condition(M68000StateField field, C condition, int byteOffset = 0) =>
        _code.SetMemoryCondition(condition, _state, StateOffset(field) + byteOffset);
    private void MaskByte(M68000StateField field, int byteOffset = 0) =>
        _code.UnaryMemory(X64Unary.Negate, _state, StateOffset(field) + byteOffset, W.Byte);
}
