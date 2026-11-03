using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Constants.AtomConstants;
using D = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Services;

internal sealed partial class AtomMachine
{
    public byte Read(ushort address)
    {
        if (address >= C.PpiStart && address < C.PpiEnd) return _ppi.Read(address - C.PpiStart);
        if (address >= C.ViaStart && address < C.ViaEnd) return _via.Read(address & C.ViaRegisterMask);
        var disk = DiskRegister(address);
        if (_fdc is not null && disk >= 0) return _fdc.Read(disk);
        if (address >= C.OsRomStart) return _basicOs[C.BasicRomBytes + address - C.OsRomStart];
        if (address >= C.BasicRomStart && address < C.BasicRomStart + C.BasicRomBytes) return _basicOs[address - C.BasicRomStart];
        if (_floatingPoint.Length != 0 && address >= C.FloatingPointRomStart && address < C.FloatingPointRomStart + _floatingPoint.Length)
            return _floatingPoint[address - C.FloatingPointRomStart];
        if (_extension.Length != 0 && address >= C.ExtensionRomStart && address < C.ExtensionRomStart + _extension.Length)
            return _extension[address - C.ExtensionRomStart];
        if (_dos.Length != 0 && address >= C.DiscRomStart && address < C.DiscRomStart + _dos.Length)
            return _dos[address - C.DiscRomStart];
        return IsRam(address) ? _ram[address] : (byte)0;
    }

    public void Write(ushort address, byte value)
    {
        if (address >= C.PpiStart && address < C.PpiEnd) { _ppi.Write(address - C.PpiStart, value); return; }
        if (address >= C.ViaStart && address < C.ViaEnd) { _via.Write(address & C.ViaRegisterMask, value); return; }
        var disk = DiskRegister(address);
        if (_fdc is not null && disk >= 0)
        {
            if (disk == 0) _fdc.MechanicalSpeedPercent = _speeds[(value & D.Drive1Select) != 0 ? 1 : 0];
            _fdc.Write(disk, value);
            _fdcDrive!.Synchronize();
            return;
        }
        if (IsRam(address)) _ram[address] = value;
    }

    private bool IsRam(int address) => address < C.CpuRamBytes
        || address >= C.TextRamStart && address < C.TextRamStart + _textRamBytes
        || address >= C.VideoRamStart && address < C.VideoRamStart + _videoRamBytes
        || _fdc is not null && (address >= C.DiscRamStart && address < C.DiscRamStart + C.DiscRamBytes
            || address >= C.DiscUpperRamStart && address < C.DiscUpperRamStart + C.DiscUpperRamBytes);

    private static int DiskRegister(int address) => address >= C.FdcStart && address < C.FdcStart + C.FdcRegisters
        ? address - C.FdcStart : (address & ~C.FdcDataMirror) == C.FdcData ? C.FdcRegisters : -1;
}
