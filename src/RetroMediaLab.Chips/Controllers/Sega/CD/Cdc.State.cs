using RetroMediaLab.Chips.Common.Memory;

using System.Buffers.Binary;


namespace RetroMediaLab.Chips.Controllers.Sega.CD;

public sealed partial class Cdc
{
    private sealed class cdc_t
    {
        public byte ifstat;
        public byte ifctrl;
        public Register16 dbc;
        public Register16 dac;
        public Register16 pt;
        public Register16 wa;
        public readonly byte[] ctrl = new byte[2];
        public readonly byte[] head = new byte[8];
        public readonly byte[] stat = new byte[4];
        public readonly int[] cycles = new int[2];
        public readonly byte[] ram = new byte[18736];
        public byte ar_mask;
        public byte irq;
        public Action<uint>? dma_w, halted_dma_w;
    }
    private readonly cdc_t cdc = new();
}
