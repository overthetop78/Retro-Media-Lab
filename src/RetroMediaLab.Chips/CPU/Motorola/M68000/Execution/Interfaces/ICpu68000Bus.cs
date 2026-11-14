namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

public interface ICpu68000Bus
{
    uint Read8(uint address);
    uint Read16(uint address);
    void Write8(uint address, uint value);
    void Write16(uint address, uint value);
    uint ReadMapped8(uint address);
    uint ReadMapped16(uint address);
    void AcknowledgeInterrupt(uint level);
    int MainCycles { get; }
    int MainRefreshCycles { get; }
}

