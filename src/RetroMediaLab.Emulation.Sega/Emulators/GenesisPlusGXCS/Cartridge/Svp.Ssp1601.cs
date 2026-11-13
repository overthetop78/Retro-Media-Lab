namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class Svp
{
    public void Run(int cycles)
    {
        _pc = (int)GetHigh(6);
        _cycles = cycles;
        unchecked
        {
            do
            {
                int instruction = (int)Fetch();
                int group = instruction >> 9;
                switch (group)
                {
                    case 0:
                        if (instruction == 0) break;
                        if (instruction == 0x37) { ReadProduct(); Accumulator = GeneralRegisters[7]; }
                        else WriteRegister(instruction >> 4 & 15, ReadRegister(instruction & 15));
                        break;
                    case 1: WriteRegister(instruction >> 4 & 15, PointerRead(instruction)); break;
                    case 2: PointerWrite(instruction, ReadRegister(instruction >> 4 & 15)); break;
                    case 3: SetHigh(3, InternalRam[instruction & 0x1ff]); break;
                    case 4: WriteRegister(instruction >> 4 & 15, Fetch()); break;
                    case 5: WriteRegister(instruction >> 4 & 15, PointerReadIndirect(instruction)); break;
                    case 6: PointerWrite(instruction, Fetch()); break;
                    case 7: InternalRam[instruction & 0x1ff] = (ushort)GetHigh(3); break;
                    case 9: WriteRegister(instruction >> 4 & 15, Pointers[(instruction & 3) | (instruction >> 6 & 4)]); break;
                    case 10: Pointers[(instruction & 3) | (instruction >> 6 & 4)] = (byte)ReadRegister(instruction >> 4 & 15); break;
                    case 12: case 13: case 14: case 15: Pointers[instruction >> 8 & 7] = (byte)instruction; break;
                    case 0x24:
                        if (Condition(instruction)) { uint target = Fetch(); WriteStack((uint)_pc); WritePc(target); }
                        else _pc++;
                        break;
                    case 0x25: WriteRegister(instruction >> 4 & 15, ReadWord(_cartridge.AsSpan(0x200000), (int)GetHigh(3))); break;
                    case 0x26:
                        if (Condition(instruction)) { uint target = Fetch(); WritePc(target); }
                        else _pc++;
                        break;
                    case 0x48:
                        if (Condition(instruction))
                        {
                            switch (instruction & 7)
                            {
                                case 2: Accumulator = (uint)((int)Accumulator >> 1); break;
                                case 3: Accumulator <<= 1; break;
                                case 6: Accumulator = (uint)-(int)Accumulator; break;
                                case 7: if ((int)Accumulator < 0) Accumulator = (uint)-(int)Accumulator; break;
                            }
                            UpdateFlags(Accumulator, false);
                        }
                        break;
                    case 0x1b:
                        ReadProduct(); Accumulator -= GeneralRegisters[7]; UpdateFlags(Accumulator, false);
                        LoadProductOperands(instruction);
                        break;
                    case 0x4b:
                        ReadProduct(); Accumulator += GeneralRegisters[7]; UpdateFlags(Accumulator, false);
                        LoadProductOperands(instruction);
                        break;
                    case 0x5b:
                        Accumulator = 0; Status &= 0x0fff;
                        LoadProductOperands(instruction);
                        break;
                    default:
                        int operation = group & 0x70;
                        int operandMode = group & 15;
                        if (operation is not (0x10 or 0x30 or 0x40 or 0x50 or 0x60 or 0x70)) break;
                        uint value;
                        switch (operandMode)
                        {
                            case 0:
                                int source = instruction & 15;
                                if (source == 7) { ReadProduct(); value = GeneralRegisters[7]; }
                                else if (source == 3) value = Accumulator;
                                else value = ReadRegister(source) << 16;
                                break;
                            case 1: value = PointerRead(instruction) << 16; break;
                            case 3: value = (uint)InternalRam[instruction & 0x1ff] << 16; break;
                            case 4: value = Fetch() << 16; break;
                            case 5: value = PointerReadIndirect(instruction) << 16; break;
                            case 9: value = (uint)Pointers[(instruction & 3) | (instruction >> 6 & 4)] << 16; break;
                            case 12: value = (uint)(instruction & 255) << 16; break;
                            default: continue;
                        }
                        Arithmetic(operation, value);
                        break;
                }
            }
            while (--_cycles > 0 && (EmulationStatus & WaitMask) == 0);
        }
        ReadProduct();
        SetHigh(6, (uint)_pc);
    }

    private bool Condition(int instruction) => (instruction & 0xf0) switch
    {
        0 => true,
        0x50 => ((Status ^ (uint)(instruction << 5)) & FlagZ) == 0,
        0x70 => ((Status ^ (uint)(instruction << 7)) & FlagN) == 0,
        _ => false
    };
    private void LoadProductOperands(int instruction)
    {
        SetHigh(1, PointerRead(instruction & 3, 0, instruction << 1 & 0x18));
        SetHigh(2, PointerRead(instruction >> 4 & 3, 4, instruction >> 3 & 0x18));
    }
    private void Arithmetic(int operation, uint value)
    {
        unchecked
        {
            switch (operation)
            {
                case 0x10: Accumulator -= value; UpdateFlags(Accumulator, true); break;
                case 0x30: UpdateFlags(Accumulator - value, true); break;
                case 0x40: Accumulator += value; UpdateFlags(Accumulator, true); break;
                case 0x50: Accumulator &= value; UpdateFlags(Accumulator, false); break;
                case 0x60: Accumulator |= value; UpdateFlags(Accumulator, false); break;
                case 0x70: Accumulator ^= value; UpdateFlags(Accumulator, false); break;
            }
        }
    }
    private void UpdateFlags(uint value, bool clearCarryOverflow)
    {
        Status &= ~(clearCarryOverflow ? FlagL | FlagZ | FlagV | FlagN : FlagZ | FlagN);
        if (value == 0) Status |= FlagZ;
        else Status |= value >> 16 & FlagN;
    }
}
