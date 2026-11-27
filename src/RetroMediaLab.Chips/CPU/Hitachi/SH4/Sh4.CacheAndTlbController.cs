using System.Diagnostics;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

using static Sh4MemoryMappedRegisters;

internal sealed partial class Sh4CacheAndTlbController
{
    internal uint TranslationTableBase => GetValue(TranslationTableBaseAddress);
    internal uint CacheControl => GetValue(CacheControlAddress);
    internal uint InterruptEvent { get => GetValue(InterruptEventAddress); set => SetValue(InterruptEventAddress, value); }
    internal uint ExceptionEvent { set => SetValue(ExceptionEventAddress, value); }
    internal uint PageTableEntryHigh { get => GetValue(PageTableEntryHighAddress); set => SetValue(PageTableEntryHighAddress, value); }
    internal uint PageTableEntryLow { get => GetValue(PageTableEntryLowAddress); set => SetValue(PageTableEntryLowAddress, value); }
    internal uint PageTableEntryAssistance { get => GetValue(PageTableEntryAssistanceAddress); set => SetValue(PageTableEntryAssistanceAddress, value); }
    internal uint TranslationExceptionAddress { set => SetValue(Sh4MemoryMappedRegisters.TranslationExceptionAddress, value); }
    internal uint MmuControl { get => GetValue(MmuControlAddress); set => SetValue(MmuControlAddress, value); }
    internal uint QueueAddress0 => GetValue(Sh4MemoryMappedRegisters.QueueAddress0);
    internal uint QueueAddress1 => GetValue(Sh4MemoryMappedRegisters.QueueAddress1);
    internal uint TrapRegister { set => SetValue(TrapRegisterAddress, value); }

    internal partial void WriteQueueAddress(uint address, uint value)
    {
        SetValue(address, value & 0x1C);
        _context.SetStoreQueueWriteHandler();
    }

    private void WritePageTableEntryHigh(uint address, uint value)
    {
        uint entry = value & 0xFFFFFCFF;
#if !SH4_STRICT_MODE
        if ((entry & 0xFF) != (GetValue(PageTableEntryHighAddress) & 0xFF))
            _mmu.FlushAddressLookupTable(full: false);
#endif
        SetValue(PageTableEntryHighAddress, entry);
    }

    private void WriteMmuControl(uint address, uint value)
    {
        uint control = value & 0xFCFCFF05;
        bool changed = ((control ^ GetValue(MmuControlAddress)) & 1) != 0;
        if ((control & 4) != 0)
        {
            Debug.WriteLine("Full MMU flush");
            _mmu.FlushTable();
            control &= ~4u;
        }
        SetValue(MmuControlAddress, control);

        if (changed)
        {
            _mmu.SetState();
            _executor.ResetCache();
        }
    }

    private void WriteCacheControl(uint address, uint value)
    {
        const uint instructionInvalidate = 0x0800;
        const uint operandInvalidate = 0x0008;
        uint control = value & 0x89AF;
        if ((control & instructionInvalidate) != 0)
        {
            Debug.WriteLine($"Sh4: i-cache invalidation {_context.ProgramCounter:X8}");
            if (!_configuration.DynarecEnabled)
                _instructionCache.Invalidate();
            control &= ~instructionInvalidate;
        }
        if ((control & operandInvalidate) != 0)
        {
            Debug.WriteLine($"Sh4: o-cache invalidation {_context.ProgramCounter:X8}");
            if (!_configuration.DynarecEnabled)
                _operandCache.Invalidate();
            control &= ~operandInvalidate;
        }
        SetValue(CacheControlAddress, control);
    }

    private static uint ReadCpuVersion(uint address) => 0x040205C1;

    private static uint ReadProcessorRevision(uint address) => 0;

    internal override void Initialize()
    {
        base.Initialize();
        SetWriteHandler32(PageTableEntryHighAddress, WritePageTableEntryHigh);
        SetReadWrite(PageTableEntryLowAddress, 4, 0x1FFFFDFF);
        SetReadWrite(TranslationTableBaseAddress, 4);
        SetReadWrite(Sh4MemoryMappedRegisters.TranslationExceptionAddress, 4);
        SetWriteHandler32(MmuControlAddress, WriteMmuControl);
        SetReadWrite(BusAccessStatusAAddress, 1);
        SetReadWrite(BusAccessStatusBAddress, 1);
        SetWriteHandler32(CacheControlAddress, WriteCacheControl);
        SetReadWrite(TrapRegisterAddress, 4, 0x000003FC);
        SetReadWrite(ExceptionEventAddress, 4, 0x00000FFF);
        SetReadWrite(InterruptEventAddress, 4, 0x00000FFF);
        SetReadOnly32(CpuVersionAddress, ReadCpuVersion);
        SetReadWrite(PageTableEntryAssistanceAddress, 4, 0x0000000F);
        SetWriteHandler32(Sh4MemoryMappedRegisters.QueueAddress0, WriteQueueAddress);
        SetWriteHandler32(Sh4MemoryMappedRegisters.QueueAddress1, WriteQueueAddress);
        SetReadOnly32(ProcessorRevisionAddress, ReadProcessorRevision);
        Reset();
    }
}
