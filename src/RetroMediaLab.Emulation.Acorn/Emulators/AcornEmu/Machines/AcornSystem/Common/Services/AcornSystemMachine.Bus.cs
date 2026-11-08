using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Enums;
using Disk = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

internal sealed partial class AcornSystemMachine
{
    public byte Read(ushort address)
    {
        if (_video?.Handles(address) == true) return _video.Read(address);
        var disk = DiskRegister(address);
        if (_fdc is not null && disk >= 0)
        {
            _fdcDrive!.SynchronizeClock();
            return _fdc.Read(disk);
        }
        if (ViaAddress(address)) return _via!.Read(address & C.ViaRegisterMask);
        if (RamIoAddress(address)) return (address & C.RamIoRamSelect) == 0
            ? _ramIo.ReadIo(address & C.RamIoMask) : _ramIo.ReadRam(address & C.RamIoMask);
        if (address >= _romStart) return _rom[(address - _romStart) % _rom.Length];
        if (_basic.Length != 0 && address >= C.BasicRomStart && address < C.BasicRomStart + _basic.Length) return _basic[address - C.BasicRomStart];
        if (_ramMapped[address]) return _ram[address];
        return _card == SystemCpuCard.Motorola6809 ? byte.MaxValue : (byte)0;
    }

    public void Write(ushort address, byte value)
    {
        if (_video?.Handles(address) == true) { _video.Write(address, value); return; }
        var disk = DiskRegister(address);
        if (_fdc is not null && disk >= 0)
        {
            _fdcDrive!.SynchronizeClock();
            if (disk == 0) _fdc.MechanicalSpeedPercent = _speeds[(value & Disk.Drive1Select) == 0 ? 0 : 1];
            _fdc.Write(disk, value);
            _fdcDrive!.Synchronize();
            return;
        }
        if (ViaAddress(address))
        {
            var register = address & C.ViaRegisterMask;
            _via!.Write(register, value);
            if (register == C.ViaPeripheralControl) _tape.SetOutputBit((value & C.ViaCb2ControlMask) == C.ViaCb2High);
            return;
        }
        if (RamIoAddress(address))
        {
            if ((address & C.RamIoRamSelect) == 0) _ramIo.WriteIo(address & C.RamIoMask, value);
            else _ramIo.WriteRam(address & C.RamIoMask, value);
            return;
        }
        if (address < _romStart && (_basic.Length == 0 || address < C.BasicRomStart || address >= C.BasicRomStart + _basic.Length) && _ramMapped[address]) _ram[address] = value;
    }

    private bool RamIoAddress(int address) => _card is SystemCpuCard.Keypad6502 or SystemCpuCard.Standard6502
        && address >= C.RamIoStart && address < C.RamIoEnd;

    private bool ViaAddress(int address) => _card == SystemCpuCard.Fast6502A
        ? address >= C.ViaStart && address < C.RamIoEnd
        : _card == SystemCpuCard.Motorola6809 && address >= C.Via6809Start && address < C.Via6809Start + C.RamIoRamSelect;

    private static int DiskRegister(int address) => address >= C.FdcStart && address < C.FdcStart + C.FdcRegisters
        ? address - C.FdcStart : (address & ~C.FdcDataMirror) == C.FdcData ? C.FdcRegisters : -1;
}
