using System.Runtime.CompilerServices;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cpu;

internal struct M68kState
{
    public int cycles, refresh_cycles;
    public uint cycle_end, pc, prev_pc, ir;
    public uint t1_flag, s_flag, x_flag, n_flag, not_z_flag, v_flag, c_flag;
    public uint int_mask, int_level, stopped, pref_addr, pref_data;
    public uint instr_mode, run_mode, aerr_enabled, aerr_address, aerr_write_mode, aerr_fc;
    public uint tracing, address_space;
    public uint poll_pc, poll_cycle, poll_detected;
}

internal interface IM68kBus
{
    uint Read8(uint address);
    uint Read16(uint address);
    void Write8(uint address, uint value);
    void Write16(uint address, uint value);
    uint ReadMapped8(uint address);
    uint ReadMapped16(uint address);
    void AcknowledgeInterrupt(uint level);
    int MainCycles { get; }
    int MainRefreshCycles { get; }
}

internal enum M68kRegister
{
    D0, D1, D2, D3, D4, D5, D6, D7,
    A0, A1, A2, A3, A4, A5, A6, A7,
    PC, SR, SP, USP, ISP, PrefetchAddress, PrefetchData, IR
}

internal sealed partial class M68k<TBus> where TBus : class, IM68kBus
{
    private readonly TBus _bus;
    private readonly bool _subCpu;
    private readonly bool _addressErrors;
    private readonly uint[] _registers = new uint[16];
    private readonly uint[] _stackPointers = new uint[5];
    private readonly uint[] _previousData = new uint[8];
    private readonly uint[] _previousAddress = new uint[8];
    private readonly ushort[] _exceptionCycles = new ushort[256];
    private readonly uint[] _interruptCycles = new uint[10];
    private M68kState _state;
    private bool _irqLatency;
    private int _cycleRatio = 1;
    private int _cycleShift;
    private int MUL { get; }
    private const uint CPU_ADDRESS_MASK = 0x00ffffff;
    private ReadOnlySpan<byte> CYC_INSTRUCTION => _subCpu ? SubCycles : MainCycles;
    private ReadOnlySpan<ushort> CYC_EXCEPTION => _exceptionCycles;
    private ReadOnlySpan<uint> m68ki_cycle_interrupts => _interruptCycles;
    private uint[] REG_SP_BASE => _stackPointers;
    public ref M68kState State => ref _state;
    public Span<uint> Registers => _registers;
    public Span<uint> StackPointers => _stackPointers;
    public Span<uint> PreviousData => _previousData;
    public Span<uint> PreviousAddress => _previousAddress;
    public int InstructionCycles => CYC_INSTRUCTION[(int)REG_IR];

    public M68k(TBus bus, bool subCpu = false, bool subCpuAddressErrors = false)
    {
        _bus = bus;
        _subCpu = subCpu;
        _addressErrors = !subCpu || subCpuAddressErrors;
        MUL = subCpu ? 4 : 7;
        var exceptions = m68ki_exception_cycle_table_base;
        for (int i = 0; i < exceptions.Length; i++) _exceptionCycles[i] = (ushort)(exceptions[i] * MUL);
        var interrupts = m68ki_cycle_interrupts_base;
        for (int i = 0; i < interrupts.Length; i++) _interruptCycles[i] = interrupts[i] * (uint)MUL;
    }

    public void SetCycleRatio(int ratio, int shift)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(shift);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(shift, 31);
        _cycleRatio = ratio;
        _cycleShift = shift;
    }

    public uint GetRegister(M68kRegister register)
    {
        int index = (int)register;
        if ((uint)index < 16) return _registers[index];
        return register switch
        {
            M68kRegister.PC => REG_PC,
            M68kRegister.SR => m68ki_get_sr(),
            M68kRegister.SP => REG_SP,
            M68kRegister.USP => FLAG_S != 0 ? REG_USP : REG_SP,
            M68kRegister.ISP => FLAG_S != 0 ? REG_SP : REG_ISP,
            M68kRegister.PrefetchAddress => CPU_PREF_ADDR,
            M68kRegister.PrefetchData => CPU_PREF_DATA,
            M68kRegister.IR => REG_IR,
            _ => 0
        };
    }

    public void SetRegister(M68kRegister register, uint value)
    {
        int index = (int)register;
        if ((uint)index < 16) { _registers[index] = value; return; }
        switch (register)
        {
            case M68kRegister.PC: m68ki_jump(value); break;
            case M68kRegister.SR: m68ki_set_sr(value); break;
            case M68kRegister.SP: REG_SP = value; break;
            case M68kRegister.USP: if (FLAG_S != 0) REG_USP = value; else REG_SP = value; break;
            case M68kRegister.ISP: if (FLAG_S != 0) REG_SP = value; else REG_ISP = value; break;
            case M68kRegister.IR: REG_IR = value & 0xffff; break;
            case M68kRegister.PrefetchAddress: CPU_PREF_ADDR = value; break;
        }
    }

    public void UpdateIrq(uint mask)
    {
        if (_subCpu) CPU_INT_LEVEL = (uint)(mask == 0 ? 0 : 32 - System.Numerics.BitOperations.LeadingZeroCount(mask & 63)) << 8;
        else CPU_INT_LEVEL |= mask << 8;
    }

    public void SetIrq(uint level) => CPU_INT_LEVEL = level << 8;

    public void SetIrqDelay(uint level)
    {
        if (!_irqLatency)
        {
            if ((REG_IR & 0xf000) != 0x2000)
            {
                USE_CYCLES(CYC_INSTRUCTION[(int)REG_IR]);
                _irqLatency = true;
                try { REG_IR = m68ki_read_imm_16(); ExecuteInstruction(); }
                finally { _irqLatency = false; }
            }
            CPU_INT_LEVEL = level << 8;
        }
        m68ki_check_interrupts();
    }

    public void Run(uint cycles)
    {
        if (unchecked((uint)_state.cycles) >= cycles) return;
        m68ki_check_interrupts();
        if (CPU_STOPPED != 0) { _state.cycles = unchecked((int)cycles); return; }
        _state.cycle_end = cycles;
        bool addressFault = false;
        try
        {
            while (true)
            {
                try
                {
                    if (addressFault) { addressFault = false; m68ki_exception_address_error(); }
                    while (unchecked((uint)_state.cycles) < cycles)
                    {
                        if (_subCpu) _state.prev_pc = REG_PC;
                        REG_IR = m68ki_read_imm_16();
                        if (!_subCpu && _state.cycles >= _state.refresh_cycles)
                        {
                            _state.refresh_cycles = unchecked(_state.cycles + 128 * 7);
                            _state.cycles = unchecked(_state.cycles + 2 * 7);
                        }
                        ExecuteInstruction();
                        USE_CYCLES(CYC_INSTRUCTION[(int)REG_IR]);
                    }
                    break;
                }
                catch (AddressFault) { addressFault = true; }
            }
        }
        finally { _state.cycle_end = 0; }
    }

    public void Reset()
    {
        CPU_STOPPED = 0;
        if (_addressErrors) CPU_RUN_MODE = RUN_MODE_BERR_AERR_RESET;
        FLAG_T1 = 0;
        FLAG_INT_MASK = 0x0700;
        CPU_INT_LEVEL = 0;
        _irqLatency = false;
        m68ki_set_s_flag(SFLAG_SET);
        if (!_subCpu) CPU_PREF_ADDR = 0x1000;
        m68ki_jump(0);
        REG_SP = m68ki_read_imm_32();
        REG_PC = m68ki_read_imm_32();
        m68ki_jump(REG_PC);
        if (_addressErrors) CPU_RUN_MODE = RUN_MODE_NORMAL;
        USE_CYCLES(CYC_EXCEPTION[(int)EXCEPTION_RESET]);
        if (!_subCpu) _state.refresh_cycles = unchecked(_state.cycles / (128 * 7) * (128 * 7) + 128 * 7);
    }

    public void Halt() => CPU_STOPPED |= STOP_LEVEL_HALT;
    public void ClearHalt() => CPU_STOPPED &= ~STOP_LEVEL_HALT;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void USE_CYCLES(long value)
    {
        unchecked
        {
            int increment = (int)value;
            _state.cycles += increment * _cycleRatio >> _cycleShift;
        }
    }
    private void SET_CYCLES(uint value) => _state.cycles = unchecked((int)value);
    private void SKIP_BUS_REFRESH()
    {
        int cycles = _subCpu ? _bus.MainCycles : _state.cycles;
        int refresh = _subCpu ? _bus.MainRefreshCycles : _state.refresh_cycles;
        if (cycles >= refresh) _state.refresh_cycles = unchecked(_state.refresh_cycles + 128 * MUL);
    }
    private static int abs(int value) => value < 0 ? unchecked(-value) : value;
    private static int MAKE_INT_8(uint value) => unchecked((sbyte)value);
    private static int MAKE_INT_16(uint value) => unchecked((short)value);
    private static int MAKE_INT_32(uint value) => unchecked((int)value);
    private uint m68ki_tas_callback() => _subCpu ? 1u : 0u;
    private void m68ki_int_ack(uint level) => _bus.AcknowledgeInterrupt(level);

    private uint m68ki_read_imm_16()
    {
        unchecked
        {
            if (_subCpu) { uint pc = REG_PC; REG_PC += 2; return m68k_read_immediate_16(pc); }
            if (REG_PC != CPU_PREF_ADDR) { CPU_PREF_ADDR = REG_PC; CPU_PREF_DATA = m68k_read_immediate_16(REG_PC); }
            uint value = CPU_PREF_DATA;
            REG_PC += 2;
            CPU_PREF_ADDR = REG_PC;
            CPU_PREF_DATA = m68k_read_immediate_16(REG_PC);
            return value;
        }
    }
    private uint m68ki_read_imm_32()
    {
        unchecked
        {
            if (_subCpu) { uint pc = REG_PC; REG_PC += 4; return m68k_read_immediate_32(pc); }
            if (REG_PC != CPU_PREF_ADDR) { CPU_PREF_ADDR = REG_PC; CPU_PREF_DATA = m68k_read_immediate_16(REG_PC); }
            uint value = CPU_PREF_DATA;
            REG_PC += 2;
            value = (value << 16) | m68k_read_immediate_16(REG_PC);
            REG_PC += 2;
            CPU_PREF_ADDR = REG_PC;
            CPU_PREF_DATA = m68k_read_immediate_16(REG_PC);
            return value;
        }
    }

    private uint m68k_read_immediate_16(uint address) => _bus.ReadMapped16(address & CPU_ADDRESS_MASK);
    private uint m68k_read_immediate_32(uint address) => (m68k_read_immediate_16(address) << 16) | m68k_read_immediate_16(unchecked(address + 2));
    private uint m68ki_read_pcrel_8(uint address) => _bus.ReadMapped8(address & CPU_ADDRESS_MASK);
    private uint m68ki_read_pcrel_16(uint address) => m68k_read_immediate_16(address);
    private uint m68ki_read_pcrel_32(uint address) => m68k_read_immediate_32(address);
    private uint m68ki_read_8(uint address) => _bus.Read8(address & CPU_ADDRESS_MASK);
    private uint m68ki_read_16(uint address)
    {
        m68ki_check_address_error(address, MODE_READ, FLAG_S | FUNCTION_CODE_USER_DATA);
        return _bus.Read16(address & CPU_ADDRESS_MASK);
    }
    private uint m68ki_read_32(uint address)
    {
        m68ki_check_address_error(address, MODE_READ, FLAG_S | FUNCTION_CODE_USER_DATA);
        uint value = _bus.Read16(address & CPU_ADDRESS_MASK) << 16;
        return value | _bus.Read16(unchecked(address + 2) & CPU_ADDRESS_MASK);
    }
    private void m68ki_write_8(uint address, uint value) => _bus.Write8(address & CPU_ADDRESS_MASK, value);
    private void m68ki_write_16(uint address, uint value)
    {
        m68ki_check_address_error(address, MODE_WRITE, FLAG_S | FUNCTION_CODE_USER_DATA);
        _bus.Write16(address & CPU_ADDRESS_MASK, value);
    }
    private void m68ki_write_32(uint address, uint value)
    {
        m68ki_check_address_error(address, MODE_WRITE, FLAG_S | FUNCTION_CODE_USER_DATA);
        _bus.Write16(address & CPU_ADDRESS_MASK, value >> 16);
        _bus.Write16(unchecked(address + 2) & CPU_ADDRESS_MASK, value & 0xffff);
    }
    private void m68ki_check_address_error(uint address, uint writeMode, uint functionCode)
    {
        if (_addressErrors && (address & 1) != 0 && _state.aerr_enabled != 0)
        {
            _state.aerr_address = address;
            _state.aerr_write_mode = writeMode;
            _state.aerr_fc = functionCode;
            throw new AddressFault();
        }
    }
    private sealed class AddressFault : Exception { }

    private ref uint DX => ref _registers[(REG_IR >> 9) & 7];
    private ref uint DY => ref _registers[REG_IR & 7];
    private ref uint AX => ref _registers[8 + ((REG_IR >> 9) & 7)];
    private ref uint AY => ref _registers[8 + (REG_IR & 7)];
    private ref uint REG_SP => ref _registers[15];
    private ref uint REG_USP => ref _stackPointers[0];
    private ref uint REG_ISP => ref _stackPointers[4];
    private ref uint REG_PC => ref _state.pc;
    private ref uint REG_IR => ref _state.ir;
    private ref uint FLAG_T1 => ref _state.t1_flag;
    private ref uint FLAG_S => ref _state.s_flag;
    private ref uint FLAG_X => ref _state.x_flag;
    private ref uint FLAG_N => ref _state.n_flag;
    private ref uint FLAG_Z => ref _state.not_z_flag;
    private ref uint FLAG_V => ref _state.v_flag;
    private ref uint FLAG_C => ref _state.c_flag;
    private ref uint FLAG_INT_MASK => ref _state.int_mask;
    private ref uint CPU_INT_LEVEL => ref _state.int_level;
    private ref uint CPU_STOPPED => ref _state.stopped;
    private ref uint CPU_PREF_ADDR => ref _state.pref_addr;
    private ref uint CPU_PREF_DATA => ref _state.pref_data;
    private ref uint CPU_INSTR_MODE => ref _state.instr_mode;
    private ref uint CPU_RUN_MODE => ref _state.run_mode;
}
