using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2.Compilation;

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
internal delegate uint Sh2X64MemoryAccess(nint state, uint address, uint value, uint kind);
internal delegate void Sh2X64Loop(nint registers, nint state, nint access);

/// <summary>Word offsets shared by the native emitter and the CPU state bridge.</summary>
internal static class Sh2X64Context
{
    internal const int Status = 0, GlobalBase = 1, VectorBase = 2, Procedure = 3,
        MacLow = 4, MacHigh = 5, Pc = 6, PreviousPc = 7, IrqDelay = 8, Branch = 9,
        PollingEpoch = 10, CyclesLow = 11, CyclesHigh = 12, EndLow = 13, EndHigh = 14,
        BusDeadlineLow = 15, BusDeadlineHigh = 16, PendingClocks = 17, Sleeping = 18,
        Yield = 19, IrqLevel = 20, IrqVector = 21, ExceptionType = 22, CallbackError = 23, FetchWait = 24,
        EntryPc = 25, CodeChanged = 26, Executed = 27, LinkedHops = 28, IdleEnabled = 29, Words = 32;
}
