using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Constants.ArchimedesBoardConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Services;

internal sealed partial class ArchimedesMachine
{
    public bool TryReadWord(uint address, bool sequential, bool userMode, bool instructionFetch, out uint value)
    {
        _memc.ObserveCpuAddress(address);
        AccessCycle(address, sequential, instructionFetch);
        if (address < C.LogicalLimit) return _memc.TryReadLogicalWord(address, userMode, out value);
        value = C.OpenBusWord;
        if (userMode && address < C.LowRomBase) return false;
        if (address < C.PhysicalLimit) value = _memc.ReadPhysicalWord(address);
        else if (address < C.IoLimit) value = ReadIo(address);
        else if (address >= C.HighRomBase) value = _memc.ReadRomWord(address);
        return true;
    }

    public bool TryReadByte(uint address, bool userMode, out byte value)
    {
        _memc.ObserveCpuAddress(address);
        AccessCycle(address, false);
        if (address < C.LogicalLimit) return _memc.TryReadLogicalByte(address, userMode, out value);
        value = C.OpenBusByte;
        if (userMode && address < C.LowRomBase) return false;
        if (address < C.PhysicalLimit) value = _memc.ReadPhysicalByte(address);
        else if (address < C.IoLimit) value = (byte)ReadIo(address);
        else if (address >= C.HighRomBase) value = _memc.ReadRomByte(address);
        return true;
    }

    public bool TryWriteWord(uint address, uint value, bool sequential, bool userMode)
    {
        _memc.ObserveCpuAddress(address);
        AccessCycle(address, sequential);
        if (address < C.LogicalLimit) return _memc.TryWriteLogicalWord(address, value, userMode);
        if (userMode) return false;
        if (address < C.PhysicalLimit) _memc.WritePhysicalWord(address, value);
        else if (address < C.IoLimit) WriteIo(address, value);
        else if (address < C.MemcWriteBase) _vidc.Write(value);
        else if (address < C.CamWriteBase) _memc.WriteRegister(address);
        else _memc.WriteCam(address);
        return true;
    }

    public bool TryWriteByte(uint address, byte value, bool userMode)
    {
        _memc.ObserveCpuAddress(address);
        if (address >= C.PhysicalLimit) return TryWriteWord(address, value * C.ByteReplicator, false, userMode);
        AccessCycle(address, false);
        if (address < C.LogicalLimit) return _memc.TryWriteLogicalByte(address, value, userMode);
        if (userMode) return false;
        _memc.WritePhysicalByte(address, value);
        return true;
    }

    private uint ReadIo(uint address)
    {
        if ((address & C.IocSelect) == 0) return C.OpenBusWord;
        switch ((address >> C.IoBankShift) & C.IoBankMask)
        {
            case C.IocBank:
                RefreshControlInputs();
                return _ioc.Read(address);
            case C.FloppyBank:
                var value = _floppy.Read((int)(address / C.WordBytes) & C.FloppyRegisterMask);
                RefreshControlInputs();
                return value;
            case C.SerialBank:
                return _serial?.Read((int)(address / C.WordBytes) & C.SerialRegisterMask) ?? C.OpenBusByte;
            case C.PeripheralBank:
                return (address & C.PeripheralRegisterMask) switch
                {
                    C.HardDiskStatusRegister => _hardDisk is { } controller
                        ? (uint)controller.Status << C.HardDiskStatusShift : C.OpenBusWord,
                    C.HardDiskParameterReadRegister => _hardDisk?.ReadParameter() ?? C.OpenBusWord,
                    C.HardDiskDmaReadRegister or C.HardDiskDmaReadMirror => _hardDisk?.ReadDma() ?? C.OpenBusWord,
                    C.FloppyLatchARegister or C.FloppyLatchBRegister or C.IoebRegister => 0,
                    C.MonitorIdRegister => C.AbsentMonitorId,
                    _ => C.OpenBusWord
                };
            default: return C.OpenBusWord;
        }
    }

    private void WriteIo(uint address, uint value)
    {
        if ((address & C.IocSelect) == 0) return;
        switch ((address >> C.IoBankShift) & C.IoBankMask)
        {
            case C.IocBank:
                _ioc.Write(address, (byte)(value >> C.PeripheralDataShift));
                break;
            case C.FloppyBank:
                var register = (int)(address / C.WordBytes) & C.FloppyRegisterMask;
                if (register != 0 || (_floppyLatchB & C.FloppyReset) != 0) _floppy.Write(register, (byte)value);
                RefreshControlInputs();
                break;
            case C.SerialBank:
                _serial?.Write((int)(address / C.WordBytes) & C.SerialRegisterMask, (byte)value);
                break;
            case C.PeripheralBank:
                switch (address & C.PeripheralRegisterMask)
                {
                    case C.HardDiskCommandRegister: _hardDisk?.WriteCommand((byte)value); break;
                    case C.HardDiskParameterWriteRegister: _hardDisk?.WriteParameter((ushort)value); break;
                    case C.HardDiskDmaWriteRegister:
                    case C.HardDiskDmaWriteMirror: _hardDisk?.WriteDma((ushort)value); break;
                    case C.FloppyLatchARegister: WriteFloppyLatchA((byte)value); break;
                    case C.FloppyLatchBRegister: WriteFloppyLatchB((byte)value); break;
                }
                break;
        }
    }

    public void SetBusLock(bool enabled) => _busLocked = enabled;

    public void InternalCycle()
    {
        _mergeInternalCyclePending = false;
        if (_stretchInternalCycles)
        {
            AdvanceMasterTicks(_lastBusMasterTicks);
            return;
        }
        _cpuPhase += C.MasterClockHz;
        var ticks = (int)(_cpuPhase / _cpuClockHz);
        _cpuPhase %= _cpuClockHz;
        AdvanceMasterTicks(ticks);
    }

    public void MergeInternalCycle()
    {
        // Keep the internal interval observable now (interrupts, timers and
        // DMA still advance). The following N-cycle shares its first MCLK.
        InternalCycle();
        _mergeInternalCyclePending = true;
    }

    public void MultiplyCycles(uint nextFetchAddress, int cycles)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cycles);
        if (!_stretchInternalCycles)
        {
            for (var cycle = 1; cycle < cycles; cycle++) InternalCycle();
            MergeInternalCycle();
            return;
        }
        MergeInternalCycle();
        for (var cycle = 1; cycle < cycles; cycle++)
        {
            try { AccessCycle(nextFetchAddress, false); }
            finally { _mergeInternalCyclePending = true; }
        }
    }

    private void AccessCycle(uint address, bool sequential, bool instructionFetch = false)
    {
        var merge = _mergeInternalCyclePending && instructionFetch
            && (!_stretchInternalCycles || (address & C.SequentialBoundaryMask) != C.SequentialBoundaryMask);
        if (_mergeInternalCyclePending && instructionFetch) sequential = false;
        _mergeInternalCyclePending = false;
        // MEMC limite les séquences à quatre mots : après A2=A3=1, le cycle suivant
        // est un N-cycle, même si l'ARM garde SEQ haut. Le DMA peut alors prendre le bus.
        sequential &= (address & C.SequentialBoundaryMask) != 0;
        var alignment = (int)(_masterTicks % _memoryMasterTicks);
        if (alignment != 0) AdvanceMasterTicks(_memoryMasterTicks - alignment);
        while (_dmaTicks != 0 || !sequential && !_busLocked && StartDma()) AdvanceMasterTicks(_memoryMasterTicks);
        var cycles = address < C.PhysicalLimit ? sequential ? C.MemorySequentialCycles : C.MemoryNonSequentialCycles
            : address >= C.HighRomBase || address < C.LogicalLimit && _memc.BootRomMapped
                ? _memc.RomCycles(address, sequential) : C.InternalIoCycles;
        if (address < C.LogicalLimit && _memc.BootRomMapped) cycles = _memc.RomCycles(address, sequential);
        _lastBusMasterTicks = cycles * _memoryMasterTicks;
        if (merge && _stretchInternalCycles)
        {
            var sequentialCycles = address < C.PhysicalLimit ? C.MemorySequentialCycles
                : address >= C.HighRomBase ? _memc.RomCycles(address, true) : C.InternalIoCycles;
            if (address < C.LogicalLimit && _memc.BootRomMapped) sequentialCycles = _memc.RomCycles(address, true);
            _lastBusMasterTicks = sequentialCycles * _memoryMasterTicks;
        }
        _cpuBusBusy = true;
        try { AdvanceMasterTicks((merge ? cycles - C.MemorySequentialCycles : cycles) * _memoryMasterTicks); }
        finally { _cpuBusBusy = false; }
    }

    private void AdvanceMasterTicks(int ticks)
    {
        while (ticks > 0)
        {
            var elapsed = Math.Min(ticks, _iocMasterTicks - (int)(_masterTicks % _iocMasterTicks));
            elapsed = Math.Min(elapsed, (C.MasterClockHz - _audioPhase + C.AudioSampleRate - 1) / C.AudioSampleRate);
            if (!_cpuBusBusy && !_busLocked)
                elapsed = Math.Min(elapsed, _memoryMasterTicks - (int)(_masterTicks % _memoryMasterTicks));
            if (_dmaTicks > 0) elapsed = Math.Min(elapsed, _dmaTicks);
            var floppyTicks = ((int)(_masterTicks % _floppyMasterTicks) + elapsed) / _floppyMasterTicks;
            _masterTicks += elapsed;
            ticks -= elapsed;
            _dmaTicks = Math.Max(0, _dmaTicks - elapsed);
            _refreshTicks -= elapsed;
            _vidc.AdvanceMasterClocks(elapsed);
            if (_masterTicks % _iocMasterTicks == 0)
            {
                _ioc.Advance(1);
                _serial?.Advance(1);
                _hardDisk?.Advance(1);
                _keyboard.Advance(1);
                _cmos.Advance(1);
                if (!_ioc.KeyboardReceiveBusy && _keyboard.TryTakeTransmit(out var data)) _ioc.ReceiveKeyboardByte(data);
            }
            if (floppyTicks != 0)
            {
                _floppy.Advance(floppyTicks);
                RefreshControlInputs();
            }
            AdvanceAudio(elapsed);
            if (!_cpuBusBusy && !_busLocked && _dmaTicks == 0 && _masterTicks % _memoryMasterTicks == 0) StartDma();
        }
    }

    private bool StartDma()
    {
        if (_dmaTicks != 0) return true;
        Span<uint> words = stackalloc uint[C.DmaBurstWords];
        if (_memc.VideoDmaEnabled && _vidc.CursorDmaRequest && _memc.CursorEnabled)
        {
            _memc.ReadCursorBurst(words);
            _vidc.SupplyCursorBurst(words);
        }
        else if (_memc.VideoDmaEnabled && _vidc.VideoDmaRequest)
        {
            _memc.ReadVideoBurst(words);
            _vidc.SupplyVideoBurst(words);
        }
        else if (_memc.SoundDmaEnabled && _vidc.SoundDmaRequest)
        {
            _memc.ReadSoundBurst(words);
            _vidc.SupplySoundBurst(words);
        }
        else
        {
            if (_refreshTicks > 0) return false;
            _refreshTicks = C.RefreshPeriodMemoryCycles * _memoryMasterTicks;
            var refresh = (_memc.Control >> C.RefreshControlShift) & C.RefreshControlMask;
            if (refresh != C.RefreshAlways && !(refresh == C.RefreshFlyback && _vidc.Flyback)) return false;
            _dmaTicks = C.RefreshMemoryCycles * _memoryMasterTicks;
            return true;
        }
        _dmaTicks = C.DmaBurstMemoryCycles * _memoryMasterTicks;
        return true;
    }
}
