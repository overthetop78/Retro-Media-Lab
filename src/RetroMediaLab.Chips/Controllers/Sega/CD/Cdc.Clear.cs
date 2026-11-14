using RetroMediaLab.Chips.Common.Memory;

using System.Buffers.Binary;


namespace RetroMediaLab.Chips.Controllers.Sega.CD;

public sealed partial class Cdc
{
    private void ClearState()
    {
        cdc.ifstat = default;
        cdc.ifctrl = default;
        cdc.dbc = default;
        cdc.dac = default;
        cdc.pt = default;
        cdc.wa = default;
        cdc.ctrl.AsSpan().Clear();
        cdc.head.AsSpan().Clear();
        cdc.stat.AsSpan().Clear();
        cdc.cycles.AsSpan().Clear();
        cdc.ram.AsSpan().Clear();
        cdc.ar_mask = default;
        cdc.irq = default;
        cdc.dma_w = cdc.halted_dma_w = null;
    }
}
