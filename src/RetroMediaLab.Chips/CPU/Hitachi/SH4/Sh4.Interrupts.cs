using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4Interrupts(Sh4Context context, Sh4Debugger debugger)
{
    private enum PriorityRegister : byte { Irl, A, B, C }
    private readonly record struct Source(PriorityRegister Register, int Shift, Sh4ExceptionEvent Event);

    private static readonly Source[] Sources =
    [
        new(PriorityRegister.Irl, 0, Sh4ExceptionEvent.ExtInterrupt9),
        new(PriorityRegister.Irl, 4, Sh4ExceptionEvent.ExtInterruptB),
        new(PriorityRegister.Irl, 8, Sh4ExceptionEvent.ExtInterruptD),
        new(PriorityRegister.C, 0, Sh4ExceptionEvent.HUDI),
        new(PriorityRegister.C, 12, Sh4ExceptionEvent.GPIO),
        new(PriorityRegister.C, 8, Sh4ExceptionEvent.DMAC_DTME0),
        new(PriorityRegister.C, 8, Sh4ExceptionEvent.DMAC_DTME1),
        new(PriorityRegister.C, 8, Sh4ExceptionEvent.DMAC_DTME2),
        new(PriorityRegister.C, 8, Sh4ExceptionEvent.DMAC_DTME3),
        new(PriorityRegister.C, 8, Sh4ExceptionEvent.DMAC_DMAE),
        new(PriorityRegister.A, 12, Sh4ExceptionEvent.TMU0),
        new(PriorityRegister.A, 8, Sh4ExceptionEvent.TMU1),
        new(PriorityRegister.A, 4, Sh4ExceptionEvent.TMU2),
        new(PriorityRegister.A, 4, Sh4ExceptionEvent.TMU2_TICPI2),
        new(PriorityRegister.A, 0, Sh4ExceptionEvent.RTC_ATI),
        new(PriorityRegister.A, 0, Sh4ExceptionEvent.RTC_PRI),
        new(PriorityRegister.A, 0, Sh4ExceptionEvent.RTC_CUI),
        new(PriorityRegister.B, 4, Sh4ExceptionEvent.SCI_ERI),
        new(PriorityRegister.B, 4, Sh4ExceptionEvent.SCI_RXI),
        new(PriorityRegister.B, 4, Sh4ExceptionEvent.SCI_TXI),
        new(PriorityRegister.B, 4, Sh4ExceptionEvent.SCI_TEI),
        new(PriorityRegister.C, 4, Sh4ExceptionEvent.SCIF_ERI),
        new(PriorityRegister.C, 4, Sh4ExceptionEvent.SCIF_RXI),
        new(PriorityRegister.C, 4, Sh4ExceptionEvent.SCIF_BRI),
        new(PriorityRegister.C, 4, Sh4ExceptionEvent.SCIF_TXI),
        new(PriorityRegister.B, 12, Sh4ExceptionEvent.WDT),
        new(PriorityRegister.B, 8, Sh4ExceptionEvent.REF_RCMI),
        new(PriorityRegister.A, 8, Sh4ExceptionEvent.REF_ROVI)
    ];

    private readonly Sh4ExceptionEvent[] _events = new Sh4ExceptionEvent[32];
    private readonly uint[] _interruptBits = new uint[32];
    private readonly uint[] _levelBits = new uint[16];
    private uint _pending;
    private uint _mask;
    private uint _decodedStatusMask;
    private Sh4InterruptControllerRegisters _registers = null!;
    private Sh4CacheAndTlbController _cacheRegisters = null!;

    internal void AttachRegisters(Sh4InterruptControllerRegisters registers, Sh4CacheAndTlbController cacheRegisters)
    {
        _registers = registers;
        _cacheRegisters = cacheRegisters;
    }

    private void RecalculatePending() => context.PendingInterruptMask = _pending & _mask & _decodedStatusMask;

    internal void RebuildSortedInterruptTable()
    {
        int count = 0;
        uint previousPending = _pending;
        uint previousMask = _mask;
        _pending = 0;
        _mask = 0;
        for (int level = 0; level < 16; level++)
        {
            for (int id = 0; id < Sources.Length; id++)
            {
                Source source = Sources[id];
                uint priority = source.Register switch
                {
                    PriorityRegister.Irl => 0x0246,
                    PriorityRegister.A => _registers.PriorityA,
                    PriorityRegister.B => _registers.PriorityB,
                    _ => _registers.PriorityC
                };
                if (((priority >> source.Shift) & 15) != level)
                    continue;

                _events[count] = source.Event;
                uint pending = _interruptBits[id] & previousPending;
                uint masked = _interruptBits[id] & previousMask;
                _interruptBits[id] = 1u << count;
                if (pending != 0)
                    _pending |= _interruptBits[id];
                if (masked != 0)
                    _mask |= _interruptBits[id];
                count++;
            }

            _levelBits[level] = (1u << count) - 1;
        }
        DecodeStatusRegister();
    }

    internal bool DecodeStatusRegister()
    {
        if (context.StatusRegister.Bl)
            _decodedStatusMask = 0;
        else
            _decodedStatusMask = ~_levelBits[context.StatusRegister.InterruptMask];
        RecalculatePending();
        return context.InterruptPending;
    }

#if !SH4_DYNAREC_NONE
    internal static unsafe nuint UpdateEntry => (nuint)(delegate* unmanaged[Cdecl]<int>)&NativeUpdate;
    internal static unsafe nuint RaiseExceptionEntry =>
        (nuint)(delegate* unmanaged[Cdecl]<uint, uint, void>)&NativeRaiseException;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int NativeUpdate() => Sh4Context.Active.Interrupts.Update();

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void NativeRaiseException(uint pc, uint exception) =>
        Sh4Context.Active.Interrupts.RaiseException(pc, (Sh4ExceptionEvent)exception);
#endif

    internal int Update()
    {
        uint pending = context.PendingInterruptMask;
        if (pending == 0)
            return 0;
        DoInterrupt(_events[BitOperations.Log2(pending)]);
        return 1;
    }

    private void SetInterruptPending(Sh4InterruptId interrupt)
    {
        _pending |= _interruptBits[(int)interrupt];
        RecalculatePending();
    }

    private void ResetInterruptPending(Sh4InterruptId interrupt)
    {
        _pending &= ~_interruptBits[(int)interrupt];
        RecalculatePending();
    }

    private void SetInterruptMask(Sh4InterruptId interrupt)
    {
        _mask |= _interruptBits[(int)interrupt];
        RecalculatePending();
    }

    private void ResetInterruptMask(Sh4InterruptId interrupt)
    {
        _mask &= ~_interruptBits[(int)interrupt];
        RecalculatePending();
    }

    private void DoInterrupt(Sh4ExceptionEvent interrupt)
    {
        _cacheRegisters.InterruptEvent = (ushort)interrupt;

        context.Ssr = context.StatusRegister.GetFull();
        context.Spc = context.ProgramCounter;
        context.Sgr = context.Registers[15];
        context.StatusRegister.Bl = true;
        context.StatusRegister.MD = true;
        context.StatusRegister.Rb = true;
        context.UpdateStatusRegister();
        context.ProgramCounter = unchecked(context.Vbr + 0x600u);
        debugger.SubroutineCall();
    }

    internal void RaiseException(uint pc, Sh4ExceptionEvent exception)
    {
        Debug.Assert(exception is >= Sh4ExceptionEvent.TlbMissRead and <= Sh4ExceptionEvent.SlotIllegalInstruction
            or Sh4ExceptionEvent.FpuDisabled or Sh4ExceptionEvent.SlotFpuDisabled or Sh4ExceptionEvent.UserBreak);
        if (context.StatusRegister.Bl)
            throw new InvalidOperationException("Fatal: SH4 exception when blocked");
        _cacheRegisters.ExceptionEvent = (ushort)exception;

        context.Ssr = context.StatusRegister.GetFull();
        context.Spc = pc;
        context.Sgr = context.Registers[15];
        context.StatusRegister.Bl = true;
        context.StatusRegister.MD = true;
        context.StatusRegister.Rb = true;
        context.UpdateStatusRegister();
        context.ProgramCounter = unchecked(context.Vbr
            + (exception is Sh4ExceptionEvent.TlbMissRead or Sh4ExceptionEvent.TlbMissWrite ? 0x400u : 0x100u));
        debugger.SubroutineCall();
    }

    internal void Initialize()
    {
    }

    internal void Reset()
    {
        _pending = 0;
        _mask = uint.MaxValue;
        _decodedStatusMask = 0;
        for (int id = 0; id < Sources.Length; id++)
            _interruptBits[id] = 1u << id;
        RebuildSortedInterruptTable();
    }

    internal void Terminate()
    {
    }

    internal void Serialize(BinaryWriter writer)
    {
        foreach (Sh4ExceptionEvent interrupt in _events)
            writer.Write((ushort)interrupt);
        foreach (uint value in _interruptBits)
            writer.Write(value);
        foreach (uint value in _levelBits)
            writer.Write(value);
        writer.Write(_pending);
        writer.Write(_mask);
        writer.Write(_decodedStatusMask);
    }

    internal void Deserialize(BinaryReader reader)
    {
        for (int index = 0; index < _events.Length; index++)
            _events[index] = (Sh4ExceptionEvent)reader.ReadUInt16();
        for (int index = 0; index < _interruptBits.Length; index++)
            _interruptBits[index] = reader.ReadUInt32();
        for (int index = 0; index < _levelBits.Length; index++)
            _levelBits[index] = reader.ReadUInt32();
        _pending = reader.ReadUInt32();
        _mask = reader.ReadUInt32();
        _decodedStatusMask = reader.ReadUInt32();
    }
}
