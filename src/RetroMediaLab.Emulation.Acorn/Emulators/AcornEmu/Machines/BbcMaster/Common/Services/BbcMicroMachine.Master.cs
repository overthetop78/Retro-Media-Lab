using Master = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Constants.MasterConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

/// <summary>Décodage Master partagé avec les périphériques BBC ; les adresses BBC remplacées restent déconnectées.</summary>
internal sealed partial class BbcMicroMachine
{
    public byte[]? ReadNonvolatileMemory() => _compactEeprom?.Export() ?? _masterClock?.Export();
    public void WriteNonvolatileMemory(ReadOnlySpan<byte> bytes)
    {
        if (_compactEeprom is not null) _compactEeprom.Import(bytes);
        else if (_masterClock is not null) _masterClock.Import(bytes);
        else throw new InvalidOperationException();
    }
    private bool TryReadMasterBus(ushort address, out byte value)
    {
        value = byte.MaxValue;
        if (address is >= Master.TubeStart and < Master.TubeEnd) { value = _tube?.HostRead(address) ?? byte.MaxValue; return true; }
        if (address is >= Master.AdcStart and < Master.AdcStart + Master.RegisterBlockBytes * 2) { value = _adc?.Read(address & (Master.RegisterBlockBytes - 1)) ?? byte.MaxValue; return true; }
        if (address is >= Master.DiskControl and < Master.DiskControl + Master.RegisterBlockBytes) return true;
        if (address is >= Master.DiskRegisters and < Master.DiskRegisters + Master.RegisterBlockBytes) { value = _disks.Read((ushort)(Master.BbcDriveRegisters | (address & (Master.RegisterBlockBytes - 1)))); return true; }
        if (address is >= Master.RomSelect and < Master.RomSelect + Master.RegisterBlockBytes) { value = _masterMemory!.RomSelect; return true; }
        if (address is >= Master.AccessControl and < Master.AccessControl + Master.RegisterBlockBytes) { value = _masterMemory!.AccessControl; return true; }
        if (address is >= Master.EconetStation and < Master.EconetStation + Master.RegisterBlockBytes) { value = _econet.ReadStation(); return true; }
        if (address is >= Master.EconetNmiEnable and < Master.EconetNmiEnable + Master.RegisterBlockBytes) { _econet.EnableNmi(); return true; }
        if (address is >= Master.LegacyDiskStart and < Master.LegacyDiskEnd || address is >= Master.LegacyAdcStart and < Master.LegacyAdcEnd) return true;
        return false;
    }
    private bool TryWriteMasterBus(ushort address, byte value)
    {
        if (address is >= Master.TubeStart and < Master.TubeEnd) { _tube?.HostWrite(address, value); return true; }
        if (address is >= Master.AdcStart and < Master.AdcStart + Master.RegisterBlockBytes * 2) { _adc?.Write(address & (Master.RegisterBlockBytes - 1), value); return true; }
        if (address is >= Master.DiskControl and < Master.DiskControl + Master.RegisterBlockBytes)
        {
            var control = (byte)((value & Master.MasterDriveMask) | ((value & Master.MasterDriveReset) != 0 ? Master.BbcDriveReset : 0) | ((value & Master.MasterDriveSide) != 0 ? Master.BbcDriveSide : 0) | ((value & Master.MasterDriveDensity) != 0 ? Master.BbcDriveDensity : 0));
            _disks.Write(Master.LegacyDiskStart, control); return true;
        }
        if (address is >= Master.DiskRegisters and < Master.DiskRegisters + Master.RegisterBlockBytes) { _disks.Write((ushort)(Master.BbcDriveRegisters | (address & (Master.RegisterBlockBytes - 1))), value); return true; }
        if (address is >= Master.RomSelect and < Master.AccessControl + Master.RegisterBlockBytes)
        {
            _masterMemory!.Select(address, value);
            if (address < Master.AccessControl) _roms.Select(value);
            return true;
        }
        if (address is >= Master.EconetStation and < Master.EconetStation + Master.RegisterBlockBytes) { _econet.DisableNmi(); return true; }
        if (address is >= Master.EconetNmiEnable and < Master.EconetNmiEnable + Master.RegisterBlockBytes) return true;
        if (address is >= Master.LegacyDiskStart and < Master.LegacyDiskEnd || address is >= Master.LegacyAdcStart and < Master.LegacyAdcEnd || address is >= Master.UnusedUlaStart and < Master.RomSelect) return true;
        return false;
    }
}
