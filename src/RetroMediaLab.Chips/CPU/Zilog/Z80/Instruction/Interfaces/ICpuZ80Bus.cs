namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;

public interface ICpuZ80Bus
{
    Memory<byte>[] ReadMap { get; }
    byte ReadMemory(int address);
    void WriteMemory(int address, byte value);
    byte ReadPort(int port);
    void WritePort(int port, byte value);
}
