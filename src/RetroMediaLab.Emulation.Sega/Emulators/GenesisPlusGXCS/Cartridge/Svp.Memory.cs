namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class Svp
{
    private static int GetIncrement(int mode)
    {
        int increment = mode >> 11 & 7;
        if (increment != 0)
        {
            if (increment != 7) increment--;
            increment = 1 << increment;
            if ((mode & 0x8000) != 0) increment = -increment;
        }
        return increment;
    }

    private uint PmIo(int register, bool write, uint data)
    {
        int slot = (write ? 6 : 0) + register;
        if ((EmulationStatus & PmcSet) != 0)
        {
            uint instruction = PreviousOpcode;
            if ((instruction & 0xff0f) != 0 && (instruction & 0xfff0) != 0)
            {
                EmulationStatus &= ~PmcSet;
                return 0;
            }
            Pmac[slot] = Pmc;
            EmulationStatus &= ~PmcSet;
            return 0;
        }
        if ((EmulationStatus & PmcHaveAddress) != 0) EmulationStatus &= ~PmcHaveAddress;
        if (register != 4 && (Status & 0x60) == 0) return uint.MaxValue;
        int address = (int)(Pmac[slot] & 0xffff);
        int mode = (int)(Pmac[slot] >> 16);
        if (write)
        {
            if ((mode & 0x43ff) == 0x0018)
            {
                WriteDramWord(address, data, (mode & 0x400) != 0);
                Pmac[slot] = unchecked(Pmac[slot] + (uint)GetIncrement(mode));
            }
            else if ((mode & 0xfbff) == 0x4018)
            {
                WriteDramWord(address, data, (mode & 0x400) != 0);
                Pmac[slot] = unchecked(Pmac[slot] + ((address & 1) != 0 ? 31u : 1u));
            }
            else if ((mode & 0x47ff) == 0x001c)
            {
                WriteWord(_cartridge.AsSpan(0x200000), address & 0x3ff, data);
                Pmac[slot] = unchecked(Pmac[slot] + (uint)GetIncrement(mode));
            }
        }
        else if ((mode & 0xfff0) == 0x0800)
        {
            Pmac[slot] = unchecked(Pmac[slot] + 1);
            data = ReadWord(_cartridge, address | ((mode & 15) << 16));
        }
        else if ((mode & 0x47ff) == 0x0018)
        {
            data = ReadWord(_cartridge.AsSpan(0x220000), address);
            Pmac[slot] = unchecked(Pmac[slot] + (uint)GetIncrement(mode));
        }
        else data = 0;
        Pmc = Pmac[slot];
        return data;
    }

    private void WriteDramWord(int address, uint data, bool overwrite)
    {
        if (overwrite)
        {
            uint previous = ReadWord(_cartridge.AsSpan(0x220000), address);
            for (int shift = 12; shift >= 0; shift -= 4)
            {
                uint mask = 15u << shift;
                if ((data & mask) != 0) previous = (previous & ~mask) | (data & mask);
            }
            data = previous;
        }
        WriteWord(_cartridge.AsSpan(0x220000), address, data);
    }

    private uint ReadPm(int register)
    {
        uint data = PmIo(register, false, 0);
        if (register == 4 && data == 0)
        {
            if (PreviousByteOffset == 0x0854) EmulationStatus |= Wait30Fe08;
            else if (PreviousByteOffset == 0x4f12) EmulationStatus |= Wait30Fe06;
        }
        if (data != uint.MaxValue) return data;
        data = GetHigh(register + 8);
        if (register == 0)
        {
            if ((data & 2) == 0 && PreviousByteOffset is 0x800 or 0x1851e) EmulationStatus |= WaitPm0;
            SetHigh(8, data & ~2u);
        }
        return data;
    }
    private void WritePm(int register, uint data)
    {
        if (PmIo(register, true, data) != uint.MaxValue) return;
        if (register == 3) SetHigh(8, GetHigh(8) | 1);
        SetHigh(register + 8, data);
    }

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
        return ReadWord(_cartridge.AsSpan(0x200000), address);
    }
}
