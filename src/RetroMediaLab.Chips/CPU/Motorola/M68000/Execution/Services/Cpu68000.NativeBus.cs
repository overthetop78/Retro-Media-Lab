using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

internal static class Cpu68000NativeBusAbi
{
    // Nongeneric delegate types are required by Marshal's reverse-call thunks.
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate uint Read(uint address);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void Write(uint address, uint value);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate uint Control(uint operation, uint argument);
}
internal enum Cpu68000NativeControl : uint { SetStatus, Trap, PrivilegeViolation, IllegalInstruction, ALine, FLine, ReadUserStack, WriteUserStack, ObserveCodeFetch }

public sealed partial class Cpu68000<TBus> where TBus : class, ICpu68000Bus
{
    private Cpu68000NativeBusAbi.Read? _nativeRead8, _nativeRead16;
    private Cpu68000NativeBusAbi.Read? _nativeMapped8, _nativeMapped16;
    private Cpu68000NativeBusAbi.Write? _nativeWrite8, _nativeWrite16;
    private Cpu68000NativeBusAbi.Control? _nativeControl;
    private Exception? _nativeCallbackError;
    private int _nativeExecutionDepth, _nativeRunDepth;
    private List<M68000X64Block>? _nativeRetiredBlocks;
    private bool _nativeBusInstalled, _nativeDisposePending;
    public long NativeBusAccessCount { get; private set; }
    public long NativeControlCallCount { get; private set; }

    private void InstallNativeBus()
    {
        if (_nativeBusInstalled) return;
        _nativeRead8 = address => NativeBusAccess(0, address, 0);
        _nativeRead16 = address => NativeBusAccess(1, address, 0);
        _nativeMapped8 = address => NativeBusAccess(4, address, 0);
        _nativeMapped16 = address => NativeBusAccess(5, address, 0);
        _nativeWrite8 = (address, value) => NativeBusAccess(2, address, value);
        _nativeWrite16 = (address, value) => NativeBusAccess(3, address, value);
        _nativeControl = (operation, argument) => NativeBusAccess(unchecked((int)operation), argument, 0, true);
        _nativeMemoryMap!.SetBusFunctions(Marshal.GetFunctionPointerForDelegate(_nativeRead8), Marshal.GetFunctionPointerForDelegate(_nativeRead16),
            Marshal.GetFunctionPointerForDelegate(_nativeWrite8), Marshal.GetFunctionPointerForDelegate(_nativeWrite16), Marshal.GetFunctionPointerForDelegate(_nativeControl),
            Marshal.GetFunctionPointerForDelegate(_nativeMapped8), Marshal.GetFunctionPointerForDelegate(_nativeMapped16));
        _nativeBusInstalled = true;
    }
    private uint NativeBusAccess(int operation, uint address, uint value, bool control = false)
    {
        // This method is an ABI boundary. No managed error may unwind through
        // emitted machine code; the block returns before the owner rethrows it.
        long priorDataVersion = _nativeLinkVersion, priorCodeVersion = _nativeCodeVersion;
        uint priorPc = _state.pc;
        uint priorIr = _state.ir;
        int priorCycleAdjustment = _nativeMemoryMap!.BusCycleAdjustment;
        uint result = 0;
        try
        {
            if (control)
            {
                NativeControlCallCount++;
                // Shared CPU control services, not opcode handlers. Arithmetic,
                // EAs and instruction sequencing remain in the emitted block.
                switch ((Cpu68000NativeControl)operation)
                {
                    case Cpu68000NativeControl.SetStatus: m68ki_set_sr(address); break;
                    case Cpu68000NativeControl.Trap: m68ki_exception_trap(address); break;
                    case Cpu68000NativeControl.PrivilegeViolation: m68ki_exception_privilege_violation(); break;
                    case Cpu68000NativeControl.IllegalInstruction: m68ki_exception_illegal(); break;
                    case Cpu68000NativeControl.ALine: m68ki_exception_1010(); break;
                    case Cpu68000NativeControl.FLine: m68ki_exception_1111(); break;
                    case Cpu68000NativeControl.ReadUserStack: result = REG_USP; break;
                    case Cpu68000NativeControl.WriteUserStack: REG_USP = address; break;
                    case Cpu68000NativeControl.ObserveCodeFetch: ((ICpu68000ObservedCodeBus)_bus).ObserveCodeFetch(address); break;
                    default: throw new ArgumentOutOfRangeException(nameof(operation));
                }
            }
            else
            {
                NativeBusAccessCount++;
                switch (operation)
                {
                    case 0: result = m68ki_read_8(address); break;
                    case 1: result = m68ki_read_16(address); break;
                    case 2: m68ki_write_8(address, value); break;
                    case 3: m68ki_write_16(address, value); break;
                    case 4: result = m68ki_read_pcrel_8(address); break;
                    case 5: result = m68ki_read_pcrel_16(address); break;
                }
            }
        }
        catch (Exception error) { _nativeCallbackError ??= error; }
        finally
        {
            try
            {
                bool changed = false;
                if (_bus is ICpu68000DirectMemoryBus data)
                {
                    long version = data.DirectMemoryVersion;
                    if (version != priorDataVersion || !_nativeLinkVersionKnown)
                    {
                        _nativeMemoryMap!.Update(data); _nativeLinkVersion = version; _nativeLinkVersionKnown = true;
                        changed = true;
                    }
                }
                if (_bus is ICpu68000VersionedCodeBus code)
                {
                    long version = code.CodeMemoryVersion;
                    if (version != priorCodeVersion || !_nativeCodeVersionKnown)
                    { _nativeCodeVersion = version; _nativeCodeVersionKnown = true; changed = true; }
                }
                else changed = true;
                if (_state.pc != priorPc) changed = true;
                if (_state.ir != priorIr || _nativeMemoryMap!.BusCycleAdjustment != priorCycleAdjustment)
                {
                    // SetIrqDelay can execute the next opcode inside a bus
                    // access. Run charges the final IR's base cost after the
                    // outer handler returns, never during its intermediate IO.
                    int previous = unchecked((int)((long)_instructionCycles[(ushort)priorIr] * _cycleRatio >> _cycleShift));
                    int current = unchecked((int)((long)_instructionCycles[(ushort)_state.ir] * _cycleRatio >> _cycleShift));
                    // Supersede nested callback updates with this entire
                    // callback's net IR change, retaining earlier IO deltas.
                    _nativeMemoryMap!.SetBusCycleAdjustment(unchecked(priorCycleAdjustment + current - previous)); changed = true;
                }
                if (changed)
                {
                    if (_bus is ICpu68000DirectMemoryBus updated) _nativeMemoryMap!.Update(updated);
                    InvalidateNativeLinks(); _nativeMemoryMap!.MarkBusChanged();
                }
                _nativeMemoryMap!.SetRefreshWindow(_nativeRefreshMode,
                    _nativeRefreshMode == Cpu68000NativeRefreshMode.StableDuringNativeExecution
                        && _bus.IsRefreshWindowElapsed(_state.cycles, _state.refresh_cycles));
                if (_nativeDisposed) _nativeCallbackError ??= new ObjectDisposedException(GetType().Name);
            }
            catch (Exception error) { _nativeCallbackError ??= error; }
            if (_nativeCallbackError is not null) _nativeMemoryMap!.MarkBusFault();
            else _nativeMemoryMap!.ClearBusFault();
        }
        return result;
    }
    private void ThrowNativeBusError()
    {
        Exception? error = _nativeCallbackError; _nativeCallbackError = null;
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }
    private void RetireNativeBlock(M68000X64Block? block)
    {
        if (block is null) return;
        if (_nativeExecutionDepth != 0) (_nativeRetiredBlocks ??= []).Add(block);
        else block.Dispose();
    }
    private void FinishNativeExecution()
    {
        if (--_nativeExecutionDepth != 0) return;
        var retired = _nativeRetiredBlocks; _nativeRetiredBlocks = null;
        List<Exception>? errors = null;
        try
        {
            if (retired is not null) foreach (var block in retired)
            {
                try { block.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
            }
        }
        finally { if (_nativeDisposePending) ReleaseNativeResources(); }
        if (errors is not null) throw new AggregateException("Unable to retire every in-flight native block.", errors);
    }
    private void ReleaseNativeResources()
    {
        _nativeDisposePending = false;
        try { _nativeMemoryMap?.Dispose(); }
        finally
        {
            _nativeMemoryMap = null; _nativeProfile = null;
            _nativeRead8 = _nativeRead16 = null; _nativeWrite8 = _nativeWrite16 = null; _nativeBusInstalled = false;
            _nativeMapped8 = _nativeMapped16 = null;
            _nativeControl = null;
            try { _nativeLinks?.Dispose(); } finally { _nativeLinks = null; }
        }
    }
}
