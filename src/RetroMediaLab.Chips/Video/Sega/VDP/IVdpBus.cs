using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Video.Sega.VDP;

public interface IVdpBus
{
    int MainCycles { get; set; }
    int MainInstructionCycles { get; }
    uint Z80Cycles { get; set; }
    byte Z80IrqState { get; set; }
    byte Z80BusState { get; set; }
    uint VideoCycles { get; }
    bool SuppressScanlineRendering { get; }
    bool SkipFirstDmaWord(byte sourceHigh);
    ReadOnlyMemory<byte> DmaReadWindow { get; }
    void SetMainIrq(uint level);
    void SetMainIrqDelay(uint level);
    void SetZ80Irq(uint level);
    ushort ReadDmaIoWord(uint address);
    uint ReadDmaWord(uint address);
}
