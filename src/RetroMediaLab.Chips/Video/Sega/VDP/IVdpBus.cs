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
    byte SystemBios { get; }
    uint CdBootBank { get; }
    uint CartridgeSpecial { get; }
    bool HasSvp { get; }
    byte[] WorkRam { get; }
    void SetMainIrq(uint level);
    void SetMainIrqDelay(uint level);
    void SetZ80Irq(uint level);
    uint ReadIo(uint register);
    uint ReadDmaWord(uint address);
}
