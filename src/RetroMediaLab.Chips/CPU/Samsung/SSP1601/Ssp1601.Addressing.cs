namespace RetroMediaLab.Chips.CPU.Samsung.SSP1601;

public sealed partial class Ssp1601
{
    private uint PointerRead(int instruction) => PointerRead(instruction & 3, instruction >> 6 & 4, instruction << 1 & 0x18);
    private uint PointerRead(int register, int bank, int modifier)
    {
        int ramBase = bank * 64;
        if (register == 3) return InternalRam[ramBase + (modifier >> 3)];
        int pointer = bank + register;
        byte address = Pointers[pointer];
        uint value = InternalRam[ramBase + address];
        if (modifier != 0)
        {
            int delta = modifier == 16 ? -1 : 1;
            int loop = (int)(Status & 7);
            if (modifier == 8 || loop == 0) Pointers[pointer] = unchecked((byte)(address + delta));
            else
            {
                int mask = (1 << loop) - 1;
                Pointers[pointer] = (byte)((address & ~mask) | ((address + delta) & mask));
            }
        }
        return value;
    }
    private void PointerWrite(int instruction, uint data)
    {
        int register = instruction & 3;
        int bank = instruction >> 6 & 4;
        int modifier = instruction << 1 & 0x18;
        int ramBase = bank * 64;
        if (register == 3) { InternalRam[ramBase + (modifier >> 3)] = unchecked((ushort)data); return; }
        int pointer = bank + register;
        int address = Pointers[pointer];
        if (modifier != 0) Pointers[pointer] = unchecked((byte)(address + (modifier == 16 ? -1 : 1)));
        InternalRam[ramBase + address] = unchecked((ushort)data);
    }
    private uint PointerReadIndirect(int instruction)
    {
        int register = instruction & 3;
        int bank = instruction >> 6 & 4;
        int modifier = instruction << 1 & 0x18;
        int ramBase = bank * 64;
        int index;
        if (register == 3) index = ramBase + (modifier >> 3);
        else
        {
            if (modifier != 0) return 0;
            index = ramBase + Pointers[bank + register];
        }
        int address = InternalRam[index];
        InternalRam[index] = unchecked((ushort)(address + 1));
        return _bus.ReadProgramWord((ushort)address);
    }
}
