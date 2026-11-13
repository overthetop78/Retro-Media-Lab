namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cpu;

internal sealed partial class M68k<TBus> where TBus : class, IM68kBus
{
    public void PulseWait(uint address, bool writeAccess)
    {
        if ((CPU_STOPPED & STOP_LEVEL_WAIT) != 0) return;
        unchecked
        {
            CPU_STOPPED |= STOP_LEVEL_WAIT;
            _state.cycles = (int)_state.cycle_end - InstructionCycles;
            for (int i = 0; i < 8; i++)
            {
                _previousData[i] = _registers[i];
                _previousAddress[i] = _registers[i + 8];
            }
            uint instruction = _state.ir;
            int source = (int)(instruction & 7);
            int destination = (int)(instruction >> 9 & 7);
            if (instruction >= 0x1000 && instruction < 0x4000)
            {
                uint size = MoveSizes[(int)(instruction >> 12 & 3)];
                if ((instruction & 0x38) == 0x18) _previousAddress[source] -= size;
                else if ((instruction & 0x38) == 0x20) _previousAddress[source] += size;
                if (writeAccess)
                {
                    if ((instruction & 0x01c0) == 0x00c0) _previousAddress[destination] -= size;
                    else if ((instruction & 0x01c0) == 0x0100) _previousAddress[destination] += size;
                }
            }
            else if ((instruction & 0xb1f8) == 0x8108)
            {
                _previousAddress[source]++;
                if (address == _previousAddress[destination]) _previousAddress[destination]++;
            }
            else if ((instruction & 0xb1f8) is 0x9108 or 0x9148 or 0x9188)
            {
                uint size = DefaultSizes[(int)(instruction >> 6 & 3)];
                _previousAddress[source] += size;
                if (address == _previousAddress[destination]) _previousAddress[destination] += size;
            }
            else if ((instruction & 0xf138) == 0xb108)
            {
                uint size = DefaultSizes[(int)(instruction >> 6 & 3)];
                _previousAddress[source] -= size;
                if (address == _previousAddress[destination]) _previousAddress[destination] -= size;
            }
            else if ((instruction & 0x38) is 0x18 or 0x20)
            {
                uint size;
                if ((instruction & 0xfb80) == 0x4880) size = 0;
                else if ((instruction & 0xf100) == 0x0100 || (instruction & 0xff00) == 0x0800
                    || (instruction & 0xffc0) == 0x4ac0 || (instruction & 0xf0c0) == 0x50c0) size = 1;
                else if ((instruction & 0xf1c0) == 0x4180) size = 2;
                else if ((instruction & 0xb0c0) == 0x90c0 || (instruction & 0xf0c0) == 0xb0c0) size = (instruction & 0x100) != 0 ? 4u : 2u;
                else size = DefaultSizes[(int)(instruction >> 6 & 3)];
                if ((instruction & 8) != 0) _previousAddress[source] -= size;
                else _previousAddress[source] += size;
            }
        }
    }

    public void ClearWait()
    {
        if ((CPU_STOPPED & STOP_LEVEL_WAIT) == 0) return;
        CPU_STOPPED &= ~STOP_LEVEL_WAIT;
        _state.pc = _state.prev_pc;
        for (int i = 0; i < 8; i++)
        {
            _registers[i] = _previousData[i];
            _registers[i + 8] = _previousAddress[i];
        }
    }

    private static ReadOnlySpan<uint> MoveSizes => [0, 1, 4, 2];
    private static ReadOnlySpan<uint> DefaultSizes => [1, 2, 4, 2];
}
