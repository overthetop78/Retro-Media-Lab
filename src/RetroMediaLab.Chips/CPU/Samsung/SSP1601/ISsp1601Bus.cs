namespace RetroMediaLab.Chips.CPU.Samsung.SSP1601;

/// <summary>External program memory and register ports of an SSP1601.</summary>
public interface ISsp1601Bus
{
    ushort ReadProgramWord(ushort address);
    uint ReadExternalRegister(int register);
    void WriteExternalRegister(int register, uint value);
    void AccumulatorLowRead(ushort previousInstruction) { }
    bool ShouldYield => false;
}
