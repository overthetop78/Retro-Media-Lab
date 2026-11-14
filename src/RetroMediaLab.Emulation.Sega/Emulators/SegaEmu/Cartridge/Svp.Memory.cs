namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

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

}
