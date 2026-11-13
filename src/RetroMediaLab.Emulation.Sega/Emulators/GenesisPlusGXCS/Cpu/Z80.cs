using System.Runtime.CompilerServices;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cpu;

internal sealed partial class Z80<TBus> where TBus : class, IZ80Bus
{
    private const int CF = 1, NF = 2, PF = 4, VF = PF, XF = 8, HF = 16, YF = 32, ZF = 64, SF = 128;
    private readonly TBus _bus;
    private readonly Memory<byte>[] _readMap;
    private Z80State _state;
    private int EA;
    private uint _cycleRatio = 1;
    private int _cycleShift;
    public byte LastFetch { get; private set; }
    public uint Cycles { get => _state.cycles; set => _state.cycles = value; }
    public Z80State Context { get => _state; set => _state = value; }
    public ref Z80State State => ref _state;

    public Z80(TBus bus, Func<int, int> irqCallback, object? daisy = null)
    {
        _bus = bus;
        _readMap = bus.ReadMap;
        _state.irq_callback = irqCallback;
        _state.daisy = daisy;
        F = ZF;
    }

    public void SetCycleRatio(uint ratio, int shift)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(shift);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(shift, 31);
        _cycleRatio = ratio;
        _cycleShift = shift;
    }

    public void Reset()
    {
        PC = I = R = R2 = IM = IFF1 = IFF2 = HALT = 0;
        _state.after_ei = 0;
        WZ = PCD;
    }

    public void Run(uint cycles)
    {
        while (_state.cycles < cycles)
        {
            if (_state.irq_state != 0 && IFF1 != 0 && _state.after_ei == 0)
            {
                TakeInterrupt();
                if (_state.cycles >= cycles) return;
            }
            _state.after_ei = 0;
            R++;
            Execute_op(ROP());
        }
    }

    public void SetIrqLine(uint state) => _state.irq_state = unchecked((byte)state);

    public void SetNmiLine(uint state)
    {
        if (_state.nmi_state == 0 && state != 0)
        {
            LEAVE_HALT();
            IFF1 = 0;
            PUSH(ref _state.pc);
            PCD = 0x66;
            WZ = PCD;
            USE_CYCLES(11 * 15);
        }
        _state.nmi_state = unchecked((byte)state);
    }

    private void TakeInterrupt()
    {
        LEAVE_HALT();
        IFF1 = IFF2 = 0;
        if (IM == 1)
        {
            PUSH(ref _state.pc);
            PCD = 0x38;
            USE_CYCLES(cc_op[0xff] + cc_ex[0xff]);
        }
        else
        {
            int vector = _state.irq_callback!(0);
            if (IM == 2)
            {
                vector = (vector & 255) | (I << 8);
                PUSH(ref _state.pc);
                RM16(vector, ref _state.pc);
                USE_CYCLES(cc_op[0xcd] + cc_ex[0xff]);
            }
            else
            {
                switch (vector & 0xff0000)
                {
                    case 0xcd0000:
                        PUSH(ref _state.pc);
                        PCD = vector & 0xffff;
                        USE_CYCLES(cc_op[0xcd] + cc_ex[0xff]);
                        break;
                    case 0xc30000:
                        PCD = vector & 0xffff;
                        USE_CYCLES(cc_op[0xc3] + cc_ex[0xff]);
                        break;
                    default:
                        PUSH(ref _state.pc);
                        PCD = vector & 0x38;
                        USE_CYCLES(cc_op[0xff] + cc_ex[0xff]);
                        break;
                }
            }
        }
        WZ = PCD;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void USE_CYCLES(int value) => _state.cycles = unchecked(_state.cycles + ((uint)value * _cycleRatio >> _cycleShift));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int RM(int address) => _bus.ReadMemory(address);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WM(int address, int value) => _bus.WriteMemory(address, unchecked((byte)value));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IN(int port) => _bus.ReadPort(port);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OUT(int port, int value) => _bus.WritePort(port, unchecked((byte)value));

    private void RM16(int address, ref Z80Pair value)
    {
        value.Low = RM(address);
        value.High = RM((address + 1) & 0xffff);
    }

    private void WM16(int address, ref Z80Pair value)
    {
        WM(address, value.Low);
        WM((address + 1) & 0xffff, value.High);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadOpcode(int address) => _readMap[address >> 10].Span[address & 0x3ff];

    private int ROP()
    {
        int pc = PCD;
        PC++;
        LastFetch = (byte)ReadOpcode(pc);
        return LastFetch;
    }

    private int ARG()
    {
        int pc = PCD;
        PC++;
        return ReadOpcode(pc);
    }

    private int ARG16()
    {
        int pc = PCD;
        PC += 2;
        return ReadOpcode(pc) | (ReadOpcode((pc + 1) & 0xffff) << 8);
    }

    private void EAX() { EA = (IX + unchecked((sbyte)ARG())) & 0xffff; WZ = EA; }
    private void EAY() { EA = (IY + unchecked((sbyte)ARG())) & 0xffff; WZ = EA; }
    private void POP(ref Z80Pair register) { RM16(SPD, ref register); SP += 2; }
    private void PUSH(ref Z80Pair register) { SP -= 2; WM16(SPD, ref register); }
    private void JP() { PCD = ARG16(); WZ = PCD; }
    private void JP_COND(bool condition) { if (condition) JP(); else WZ = ARG16(); }
    private void JR() { int displacement = unchecked((sbyte)ARG()); PC += displacement; WZ = PC; }
    private void JR_COND(bool condition, int opcode) { if (condition) { JR(); USE_CYCLES(cc_ex[opcode]); } else PC++; }
    private void CALL() { EA = ARG16(); WZ = EA; PUSH(ref _state.pc); PCD = EA; }
    private void CALL_COND(bool condition, int opcode) { if (condition) { CALL(); USE_CYCLES(cc_ex[opcode]); } else WZ = ARG16(); }
    private void RET_COND(bool condition, int opcode) { if (condition) { POP(ref _state.pc); WZ = PC; USE_CYCLES(cc_ex[opcode]); } }
    private void RETN() { POP(ref _state.pc); WZ = PC; IFF1 = IFF2; }
    private void RETI() { POP(ref _state.pc); WZ = PC; IFF1 = IFF2; }
    private void RST(int address) { PUSH(ref _state.pc); PCD = address; WZ = PC; }
    private void LD_R_A() { R = A; R2 = A & 0x80; }
    private void LD_A_R() { A = (R & 0x7f) | R2; F = (F & CF) | SZ[A] | (IFF2 << 2); }
    private void LD_I_A() => I = A;
    private void LD_A_I() { A = I; F = (F & CF) | SZ[A] | (IFF2 << 2); }
    private void EI() { IFF1 = IFF2 = 1; _state.after_ei = 1; }
    private void ENTER_HALT() { PC--; HALT = 1; }
    private void LEAVE_HALT() { if (HALT != 0) { HALT = 0; PC++; } }
}
