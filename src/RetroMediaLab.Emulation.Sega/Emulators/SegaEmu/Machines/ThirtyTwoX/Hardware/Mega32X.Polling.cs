namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.ThirtyTwoX.Hardware;

internal sealed partial class Mega32X
{
    // PicoDrive memory.c p32x_sh2_poll_detect: adjacent halfwords belong to
    // the same polling window; closely spaced parts of one read do not count.
    private readonly uint[] _pollAddress = new uint[2], _pollPc = new uint[2], _pollValue = new uint[2];
    private readonly ulong[] _pollCycle = new ulong[2];
    private readonly int[] _pollCount = new int[2];
    private readonly bool[] _polling = new bool[2];
    private readonly uint[] _pollEpoch = new uint[2];
    private uint PollRead(int cpu, uint address, uint value, int threshold)
    {
        var core = _cpus[cpu]!.Cpu;
        ulong elapsed = core.Cycles - _pollCycle[cpu];
        address &= ~0x20000000u;
        if (unchecked(address - _pollAddress[cpu]) <= 3 && elapsed <= 20)
        {
            // DT sets PicoDrive's no_polling flag until the next detection call.
            // PollingEpoch gives the same inhibition without coupling the chip
            // to the machine. Reads at <= 2 cycles preserve, but do not increment,
            // the existing count (for example two consecutive MOV.W reads).
            if (core.PollingEpoch == _pollEpoch[cpu] && elapsed > 2 && ++_pollCount[cpu] >= threshold)
            {
                _polling[cpu] = true; core.YieldExecution();
            }
        }
        else if (!_polling[cpu]) { _pollCount[cpu] = 0; _pollAddress[cpu] = address & ~1u; }
        // Retain these records for the existing save-state layout and diagnostics;
        // PicoDrive does not require a stable instruction PC or data value.
        _pollPc[cpu] = core.ProgramCounter;
        _pollValue[cpu] = value;
        _pollCycle[cpu] = core.Cycles;
        _pollEpoch[cpu] = core.PollingEpoch;
        return value;
    }
    private void WakePolling(int cpu) { _polling[cpu] = false; _pollCount[cpu] = 0; }
    private void PollWrite(uint address)
    {
        address &= ~0x20000000u;
        for (int cpu = 0; cpu < 2; cpu++)
            if (unchecked(address - _pollAddress[cpu]) <= 3 || (_pollAddress[cpu] & ~3u) == (address & ~3u))
                WakePolling(cpu);
    }
    private void WakeVideoPolling()
    {
        for (int cpu = 0; cpu < 2; cpu++) if (_pollAddress[cpu] is >= 0x4100 and <= 0x410b) WakePolling(cpu);
    }
    private void ResetPolling()
    {
        Array.Clear(_pollAddress); Array.Clear(_pollPc); Array.Clear(_pollValue); Array.Clear(_pollCycle); Array.Clear(_pollCount); Array.Clear(_polling); Array.Clear(_pollEpoch);
    }
}
