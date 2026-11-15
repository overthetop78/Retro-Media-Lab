namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.ThirtyTwoX.Hardware;

internal sealed partial class Mega32X
{
    private ulong _cpuOriginMaster, _targetSh2;
    public void Synchronize(uint masterTick)
    {
        if (_syncing) return;
        ulong target = _epoch + masterTick; if (target <= _lastMaster) return;
        _syncing = true;
        try
        {
            while (_lastMaster < target)
            {
                // Gens T_gens_do_32X_line: p_i = 84 68000 cycles, i.e. 588 master
                // clocks. Stop at video transitions as well as the caller's target.
                ulong lineStart = _lastMaster / 3420 * 3420;
                ulong transition = _lastMaster - lineStart < 2560 ? lineStart + 2560 : lineStart + 3420;
                ulong next = Math.Min(target, Math.Min(_lastMaster + 588, transition));
                int lineClocks = (int)(next % 3420), lines = Video.Pal ? 313 : 262;
                int physicalLine = (int)((next / 3420 + (ulong)Video.Height) % (ulong)lines);
                bool blank = physicalLine >= Video.Height;
                ushort oldVideoStatus = Video.ReadRegister(10);
                Video.Advance((int)((next - _lastMaster) * 3 / 7)); Video.SetBlank(lineClocks >= 2560, blank);
                if (oldVideoStatus != Video.ReadRegister(10)) WakeVideoPolling();
                if (physicalLine != _lastPhysicalLine)
                {
                    if (physicalLine == Video.Height) { _pending[0] |= 8; _pending[1] |= 8; }
                    if (--_hintCounter < 0) { _hintCounter = _hintPeriod; for (int c = 0; c < 2; c++) if (!blank || (_mask[c] & 0x80) != 0) _pending[c] |= 4; }
                    _lastPhysicalLine = physicalLine;
                }
                _lastMaster = next;
                if (Enabled && Released && _booted)
                {
                    _targetSh2 = (next - _cpuOriginMaster) * 3 / 7; UpdateInterrupts();
                    for (int c = 0; c < 2; c++) RunProcessor(c, _targetSh2);
                }
                Pwm.Update((uint)(next - _epoch));
            }
        }
        finally { _syncing = false; }
    }
    private void RunProcessor(int cpu, ulong target)
    {
        if (_running[cpu]) return; _running[cpu] = true;
        try
        {
            var core = _cpus[cpu]!;
            while (core.Cpu.Cycles < target)
            {
                if (core.Cpu.InterruptReady) WakePolling(cpu);
                int clocks = (int)Math.Min(252UL, target - core.Cpu.Cycles);
                if (_polling[cpu]) core.Cpu.Idle(clocks); else core.Execute(clocks);
            }
        }
        finally { _running[cpu] = false; }
    }
    private void SynchronizeOther(int cpu)
    {
        if (!_syncing || !_booted) return;
        RunProcessor(1 - cpu, Math.Min(_targetSh2, _cpus[cpu]!.Cpu.Cycles));
    }
    public void EndFrame(uint masterClocks)
    {
        Synchronize(masterClocks); Pwm.EndFrame(masterClocks); _epoch += masterClocks;
    }
}
