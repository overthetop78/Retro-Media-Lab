using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Constants.ElectronConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Services;

internal sealed partial class ElectronMachine
{
    private bool _cpuPrepared;

    public void PrepareRead(ushort address) => PrepareCpuAccess(address, false, 0);
    public void PrepareWrite(ushort address, byte data) => PrepareCpuAccess(address, true, data);

    private void PrepareCpuAccess(ushort address, bool write, byte data)
    {
        _ula.BeginCpuCycle(address, write, data);
        _cpuPrepared = true;
    }

    private void EndCpuAccess()
    {
        _ula.EndCpuCycle();
        _cpuPrepared = false;
    }

    public byte Read(ushort address)
    {
        SynchronizeBus(address, false, 0);
        if (address < C.RamBytes) return _ula.CpuRamData;
        if (address >= C.UlaStart && address < C.IoEnd) return _ula.Read(address);
        if (address >= C.IoStart && address < C.UlaStart) return C.UnconnectedBus;
        if (address >= C.MosStart) return _mos[address - C.MosStart];
        return _ula.RomPage switch
        {
            C.KeyboardPage or C.KeyboardPageMirror => _keyboard.Read(address),
            C.BasicPage or C.BasicPageMirror => _basic[address - C.PagedRomStart],
            _ => C.UnconnectedBus
        };
    }

    public void Write(ushort address, byte value)
    {
        SynchronizeBus(address, true, value);
        if (address >= C.UlaStart && address < C.IoEnd) _ula.Write(address, value);
    }

    private void SynchronizeBus(ushort address, bool write, byte data)
    {
        // Direct board accesses have no preceding CPU Tick. Prepared CPU accesses already do.
        if (!_cpuPrepared) _ula.BeginCpuCycle(address, write, data);
        while (_ula.CpuWaiting) Tick();
        EndCpuAccess();
    }
}
