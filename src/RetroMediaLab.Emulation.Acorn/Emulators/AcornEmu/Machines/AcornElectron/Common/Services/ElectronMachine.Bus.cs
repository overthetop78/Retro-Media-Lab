using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Constants.ElectronConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Services;

internal sealed partial class ElectronMachine
{
    public byte Read(ushort address)
    {
        SynchronizeBus(address);
        if (address < C.RamBytes) return _ram[address];
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
        SynchronizeBus(address);
        if (address < C.RamBytes) _ram[address] = value;
        else if (address >= C.UlaStart && address < C.IoEnd) _ula.Write(address, value);
    }

    private void SynchronizeBus(int address)
    {
        var ram = address < C.RamBytes;
        var keyboard = address >= C.PagedRomStart && address < C.MosStart
            && _ula.RomPage is C.KeyboardPage or C.KeyboardPageMirror;
        var slowIo = address >= C.IoStart && address < C.UlaStart;
        if (!ram && !keyboard && !slowIo) return;
        while (_ticks % C.SlowClockCycles != 0 || ram && !_video.LowBandwidth && _video.DisplayFetching) Tick();
    }
}
