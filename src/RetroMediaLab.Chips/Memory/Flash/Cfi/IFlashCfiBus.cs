namespace RetroMediaLab.Chips.Memory.Flash.Cfi;

/// <summary>Word-addressed storage operations; host mapping and block persistence remain outside the command decoder.</summary>
public interface IFlashCfiBus
{
    ushort ReadWord(uint address);
    void ProgramWord(uint address, ushort value);
    void EraseChip();
    void EraseBlock(uint block);
}
