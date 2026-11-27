namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4DmaRegisters(
    Sh4AddressSpace memory, Sh4Interrupts interrupts, HollySystemBus systemBus)
    : Sh4RegisterBank(17)
{
    private const uint OperationMask = 0xFFFF8201;

    private readonly Sh4AddressSpace _memory = memory;
    private readonly Sh4Interrupts _interrupts = interrupts;
    private readonly HollySystemBus _systemBus = systemBus;
}
