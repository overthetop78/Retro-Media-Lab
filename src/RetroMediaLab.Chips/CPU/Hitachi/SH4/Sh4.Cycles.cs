using System.Diagnostics;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4Cycles
{
#if !SH4_STRICT_MODE
    private static readonly bool[] IsMemoryOperation =
    [
        false, false, true, true, false, true, true, true, false,
        false, false, false, true, false, false, false, false, true,
        true, true, false, false, true, true, false, true, false,
        true, false, true, false, true, false, true, false, true,
        false, false, false, false, false, false, false, false, false,
    ];
#endif

    internal int CountCycles(ushort op)
    {
        Sh4OpcodeDescriptor opcode = _context.OpcodeTable[op];
        int cycles = 0;
#if !SH4_STRICT_MODE
        if (IsMemoryOperation[opcode.ExecutionType])
        {
            if (++_memoryOperations < 4)
                cycles = _context.Mmu.Enabled ? 5 : 2;
        }

#endif
        if (_lastUnit == Sh4ExecutionUnit.CO
                || opcode.Unit == Sh4ExecutionUnit.CO
                || (_lastUnit == opcode.Unit && _lastUnit != Sh4ExecutionUnit.MT))
        {

            _lastUnit = opcode.Unit;
            cycles += opcode.IssueCycles;
        }
        else
        {

            _lastUnit = Sh4ExecutionUnit.CO;
        }
        return cycles * _cpuRatio;
    }

    private int ReadExternalAccessCycles(uint addr, uint size)
    {
        if ((addr & 0xfc000000) == 0xe0000000)
            return 0;

        addr &= 0x1fffffff;
        switch (addr >> 26)
        {
        case 0:
            if (!_context.Platform.IsAtomiswave)
            {
                if (addr < 0x00200000)
                {
                    switch (size)
                    {
                    case 1:
                        return 44;
                    case 2:
                        return 63;
                    case 4:
                        return 99;
                    case 32:
                    default:
                        return 618;
                    }
                }
                if (addr < 0x00200000 + _context.Platform.FlashSize)
                {
                    switch (size)
                    {
                    case 1:
                        return 41;
                    case 2:
                        return 55;
                    case 4:
                        return 83;
                    case 32:
                    default:
                        return 489;
                    }
                }
            }
            else
            {
                if (addr < 0x00020000 || (addr >= 0x00200000 && addr < 0x00200000 + _context.Platform.FlashSize))
                {
                    switch (size)
                    {
                    case 1:
                        return 41;
                    case 2:
                        return 55;
                    case 4:
                        return 83;
                    case 32:
                    default:
                        return 489;
                    }
                }
            }
            addr &= 0x01ffffff;
            if (addr >= 0x005f6800 && addr <= 0x005f69ff)
            {
                if (size != 4)
                    Trace.TraceInformation($"holly system reg: Invalid read size {size} @ {addr:x7}");
                return 5;
            }
            if (addr >= 0x005f6c00 && addr <= 0x005f6cff)
            {
                if (size != 4)
                    Trace.TraceInformation($"maple reg: Invalid read size {size} @ {addr:x7}");
                return 22;
            }
            if (addr >= 0x005f7000 && addr <= 0x005f70ff)
            {
                if (_context.Platform.IsArcade)
                    return 20;
                else
                {
                    if (size > 2)
                        Trace.TraceInformation($"gd-rom: Invalid read size {size} @ {addr:x7}");
                    return 39;
                }
            }
            if (addr >= 0x005f7400 && addr <= 0x005f74ff)
            {
                if (_context.Platform.IsConsole)
                {
                    if (size != 4)
                        Trace.TraceInformation($"G1 I/F: Invalid read size {size} @ {addr:x7}");
                }
                else
                {

                }
                return 24;
            }
            if (addr >= 0x005f7800 && addr <= 0x005f78ff)
            {
                if (size != 4)
                    Trace.TraceInformation($"G2 I/F: Invalid read size {size} @ {addr:x7}");
                return 38;
            }
            if (addr >= 0x005f7c00 && addr <= 0x005f7cff)
            {
                if (size != 4)
                    Trace.TraceInformation($"PVR I/F: Invalid read size {size} @ {addr:x7}");
                return 24;
            }
            if (addr >= 0x005f8000 && addr <= 0x005f9fff)
            {
                if (size != 4)
                    Trace.TraceInformation($"PVR/TA core: Invalid read size {size} @ {addr:x7}");
                return 34;
            }
            if (addr >= 0x00600000 && addr <= 0x006007ff)
            {
                if (_context.Platform.IsConsole)
                    return 20;
                else
                {
                    if (size != 1)
                        Trace.TraceInformation($"modem: Invalid read size {size} @ {addr:x7}");
                    return 67;
                }
            }
            if (addr >= 0x00700000 && addr <= 0x00ffffff)
            {
                if (size < 4)
                    Trace.TraceInformation($"aica: Invalid read size {size} @ {addr:x7}");
                return unchecked((int)(40u * size / 4));
            }
            if (addr >= 0x01000000 && addr <= 0x01ffffff)
            {
                switch (size)
                {
                case 1:
                case 2:
                    return 56;
                case 4:
                    return 60;
                case 32:
                default:
                    return 84;
                }
            }
            break;

        case 1:
            switch (size)
            {
            case 1:
                Trace.TraceInformation($"vram: Invalid read size 1 @ {addr:x7}");
                return 41;
            case 2:
            case 4:
                return 41;
            case 32:
            default:
                return 61;
            }

        case 2:
            Trace.TraceInformation($"Invalid read from area 2 @ {addr:x7}");
            return 60;

        case 3:
            return 7;

        case 4:
            if (size != 32)
                Trace.TraceInformation($"Invalid read size {size} from area 4 (TA FIFO) @ {addr:x7}");
            if ((addr >= 0x11000000 && addr <= 0x11ffffff) || (addr >= 0x13000000 && addr <= 0x13ffffff))
                return 61;
            break;

        case 5:
            switch (size)
            {
            case 1:
            case 2:
                return 56;
            case 4:
                return 60;
            case 32:
            default:
                return 84;
            }

        case 6:
            Trace.TraceInformation($"Invalid read from area 6 @ {addr:x7}");
            return 60;

        case 7:
            return 0;
        }
        Trace.TraceInformation($"Unmapped read @ {addr:x8}");
        return 60;
    }

    private int WriteExternalAccessCycles(uint addr, uint size)
    {
        if ((addr & 0xfc000000) == 0xe0000000)
            return 0;

        addr &= 0x1fffffff;
        switch (addr >> 26)
        {
        case 0:
            if (!_context.Platform.IsAtomiswave)
            {
                if (addr < 0x00200000)
                {
                    Trace.TraceInformation($"Invalid write to rom @ {addr:x7}");
                    return 99;
                }
                if (addr < 0x00200000 + _context.Platform.FlashSize)
                {
                    if (size != 1)
                        Trace.TraceInformation($"flashrom: Invalid write size {size} @ {addr:x7}");
                    return 28;
                }
            }
            else
            {
                if (addr < 0x00020000)
                {
                    if (size != 1)
                        Trace.TraceInformation($"flashrom: Invalid write size {size} @ {addr:x7}");
                    return 28;
                }
                if (addr >= 0x00200000 && addr < 0x00200000 + _context.Platform.FlashSize)
                {
                    return 14;
                }
            }
            addr &= 0x01ffffff;
            if (addr >= 0x005f6800 && addr <= 0x005f69ff)
            {
                if (size != 4)
                    Trace.TraceInformation($"holly system reg: Invalid write size {size} @ {addr:x7}");
                return 5;
            }
            if (addr >= 0x005f6c00 && addr <= 0x005f6cff)
            {
                if (size != 4)
                    Trace.TraceInformation($"maple reg: Invalid write size {size} @ {addr:x7}");
                return 12;
            }
            if (addr >= 0x005f7000 && addr <= 0x005f70ff)
            {
                if (_context.Platform.IsArcade)
                    return 14;
                else
                {
                    if (size > 2)
                        Trace.TraceInformation($"gd-rom: Invalid write size {size} @ {addr:x7}");
                    return 28;
                }
            }
            if (addr >= 0x005f7400 && addr <= 0x005f74ff)
            {
                if (size != 4)
                    Trace.TraceInformation($"G1 I/F: Invalid write size {size} @ {addr:x7}");
                return 12;
            }
            if (addr >= 0x005f7800 && addr <= 0x005f78ff)
            {
                if (size != 4)
                    Trace.TraceInformation($"G2 I/F: Invalid write size {size} @ {addr:x7}");
                return 12;
            }
            if (addr >= 0x005f7c00 && addr <= 0x005f7cff)
            {
                if (size != 4)
                    Trace.TraceInformation($"PVR I/F: Invalid write size {size} @ {addr:x7}");
                return 12;
            }
            if (addr >= 0x005f8000 && addr <= 0x005f9fff)
            {
                if (size != 4)
                    Trace.TraceInformation($"PVR/TA core: Invalid write size {size} @ {addr:x7}");
                return 14;
            }
            if (addr >= 0x00600000 && addr <= 0x006007ff)
            {
                if (_context.Platform.IsAtomiswave)
                    return 14;
                else
                {
                    if (size != 1)
                        Trace.TraceInformation($"modem: Invalid write size {size} @ {addr:x7}");
                    return 44;
                }
            }
            if (addr >= 0x00700000 && addr <= 0x00ffffff)
            {
                if (size < 4)
                    Trace.TraceInformation($"aica: Invalid read size {size} @ {addr:x7}");
                return unchecked((int)(12u * size / 4));
            }
            if (addr >= 0x01000000 && addr <= 0x01ffffff)
            {
                switch (size)
                {
                case 1:
                case 2:
                case 4:
                    return 28;
                case 32:
                default:
                    return 52;
                }
            }
            break;

        case 1:
            switch (size)
            {
            case 1:
                Trace.TraceInformation($"vram: Invalid write size 1 @ {addr:x7}");
                return 12;
            case 2:
            case 4:
                return 12;
            case 32:
            default:
                return 38;
            }

        case 2:
            Trace.TraceInformation($"Invalid read to area 2 @ {addr:x7}");
            return 12;

        case 3:
            return 4;

        case 4:
            if (size != 32)
                Trace.TraceInformation($"Invalid write size {size} to area 4 (TA FIFO) @ {addr:x7}");
            if ((addr >= 0x10000000 && addr <= 0x107fffff) || (addr >= 0x12000000 && addr <= 0x127fffff))
                return 7;
            if ((addr >= 0x10800000 && addr <= 0x10ffffff) || (addr >= 0x12800000 && addr <= 0x12ffffff))
                return 9;
            if ((addr >= 0x11000000 && addr <= 0x11ffffff) || (addr >= 0x13000000 && addr <= 0x13ffffff))
                return 5;
            break;

        case 5:
            switch (size)
            {
            case 1:
            case 2:
            case 4:
                return 28;
            case 32:
            default:
                return 52;
            }

        case 6:
            Trace.TraceInformation($"Invalid write to area 6 @ {addr:x7}");
            return 14;

        case 7:
            return 0;
        }
        Trace.TraceInformation($"Unmapped read @ {addr:x8}");
        return 14;
    }
}
