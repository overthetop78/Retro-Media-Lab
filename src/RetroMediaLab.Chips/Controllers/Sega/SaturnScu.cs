// Copyright (c) 2018 Renaud Toumazet
// Translated from Saturnin scu.cpp.
// SPDX-License-Identifier: Apache-2.0

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static RetroMediaLab.Chips.Controllers.Sega.SaturnScuRegisters;

namespace RetroMediaLab.Chips.Controllers.Sega;

internal sealed partial class SaturnScu
{
    internal sealed record Connections(
        Memory<byte> BackingRegisters,
        Func<uint, byte> ReadByte,
        Func<uint, uint> ReadLong,
        Action<uint, byte> WriteByte,
        Action<uint, uint, uint> BurstCopy,
        Action<SaturnInterrupt> MasterInterrupt,
        Action<SaturnInterrupt> SlaveInterrupt,
        Func<bool> IsSlaveOn,
        Func<bool> IsStv);

    private const uint ScuMemoryMask = 0xFF;
    private readonly Connections _connections;

    public SaturnScu(Connections connections)
    {
        _connections = connections;
        InitializeRegisters();
    }

    public uint Read32(uint addr)
    {
        switch (addr)
        {
            case DspProgramControlPort: return _regs.ppaf;
            case DspProgramRamDataport: return _regs.ppd;
            case DspDataRamAddressPort: return _regs.pda;
            case DspDataRamDataPort: return _regs.pdd;
            case Level0DmaEnableRegister: return _regs.d0en;
            case Level1DmaEnableRegister: return _regs.d1en;
            case Level2DmaEnableRegister: return _regs.d2en;
            case Level0DmaAddValueRegister: return _regs.d0ad;
            case Level1DmaAddValueRegister: return _regs.d1ad;
            case Level2DmaAddValueRegister: return _regs.d2ad;
            case Level0DmaReadAddress: return _regs.d0r;
            case Level1DmaReadAddress: return _regs.d1r;
            case Level2DmaReadAddress: return _regs.d2r;
            case Level0DmaWriteAddress: return _regs.d0w;
            case Level1DmaWriteAddress: return _regs.d1w;
            case Level2DmaWriteAddress: return _regs.d2w;
            case Level0DmaModeRegister: return _regs.d0md;
            case Level1DmaModeRegister: return _regs.d1md;
            case Level2DmaModeRegister: return _regs.d2md;
            case Level0DmaTransferByteNumber: return _regs.d0c;
            case Level1DmaTransferByteNumber: return _regs.d1c;
            case Level2DmaTransferByteNumber: return _regs.d2c;
            case InterruptStatusRegister: return _regs.ist;
            case InterruptMaskRegister: return _regs.ims;

            default: return ReadBackingRegister(addr);
        }
    }

    public void Write32(uint addr, uint data)
    {

        switch (addr)
        {
            case DspProgramControlPort:
            {
                if (SaturnScuRegisters.Ppaf.GetT0(_regs.ppaf) == SaturnScuRegisters.Ppaf.D0BusDmaExecution.DmaIsExecuting)
                {
                    Trace.TraceWarning("SCU DSP - D0 Bus DMA execution");
                }

                if (SaturnScuRegisters.Ppaf.GetEx(_regs.ppaf) == SaturnScuRegisters.Ppaf.ProgramExecuteControl.ProgramExecutionBegins)
                {
                    Trace.TraceWarning("SCU DSP - Program execution");
                    _regs.ppaf = 0;
                    SetInterruptStatusRegister(SaturnInterruptSources.DspEnd);
                }

                break;
            }
            case DspProgramRamDataport:
            {
                var index = SaturnScuRegisters.Ppaf.GetP(_regs.ppaf);

                BinaryPrimitives.WriteUInt32BigEndian(_programRam.AsSpan((int)index * 4), data);
                ++index;
                _regs.ppaf = SaturnScuRegisters.Ppaf.SetP(_regs.ppaf, index);
                _regs.ppd = data;
                return;
            }
            case DspDataRamAddressPort:
            {
                _regs.pda = data;
                return;
            }
            case DspDataRamDataPort:
            {
                _regs.pdd = data;
                return;
            }
            case Level0DmaEnableRegister:
            {
                _regs.d0en = data;
                if (SaturnScuRegisters.Dxen.GetDxen(_regs.d0en) == SaturnScuRegisters.Dxen.DmaEnable.Enabled)
                {
                    var dma0Config = ConfigureDmaTransfer(DmaLevel.Level0);
                    AddDmaToQueue(dma0Config);
                }
                return;

            }
            case Level1DmaEnableRegister:
            {
                _regs.d1en = data;
                if (SaturnScuRegisters.Dxen.GetDxen(_regs.d1en) == SaturnScuRegisters.Dxen.DmaEnable.Enabled)
                {
                    var dma1Config = ConfigureDmaTransfer(DmaLevel.Level1);
                    AddDmaToQueue(dma1Config);
                }
                return;
            }
            case Level2DmaEnableRegister:
            {
                _regs.d2en = data;
                if (SaturnScuRegisters.Dxen.GetDxen(_regs.d2en) == SaturnScuRegisters.Dxen.DmaEnable.Enabled)
                {
                    var dma2Config = ConfigureDmaTransfer(DmaLevel.Level2);
                    AddDmaToQueue(dma2Config);
                }
                return;
            }
            case Level0DmaAddValueRegister: _regs.d0ad = data; return;
            case Level1DmaAddValueRegister: _regs.d1ad = data; return;
            case Level2DmaAddValueRegister: _regs.d2ad = data; return;
            case Level0DmaReadAddress: _regs.d0r = data; return;
            case Level1DmaReadAddress: _regs.d1r = data; return;
            case Level2DmaReadAddress: _regs.d2r = data; return;
            case Level0DmaWriteAddress: _regs.d0w = data; return;
            case Level1DmaWriteAddress: _regs.d1w = data; return;
            case Level2DmaWriteAddress: _regs.d2w = data; return;
            case Level0DmaModeRegister: _regs.d0md = data; return;
            case Level1DmaModeRegister: _regs.d1md = data; return;
            case Level2DmaModeRegister: _regs.d2md = data; return;
            case Level0DmaTransferByteNumber: _regs.d0c = data; return;
            case Level1DmaTransferByteNumber: _regs.d1c = data; return;
            case Level2DmaTransferByteNumber: _regs.d2c = data; return;
            case InterruptStatusRegister: _regs.ist = data; return;
            case InterruptMaskRegister: _regs.ims = data; return;
            case Timer0CompareRegister: _regs.t0c = data; return;
            case Timer1SetDataRegister: _regs.t1s = data; return;
            case Timer1ModeRegister:
                _regs.t1md = data;
                if (IsTimer1Enabled()) { Trace.TraceWarning("Timer 1 enabled"); }
                return;
            case DmaStatusRegister:

                break;
            default: break;
        }

        WriteBackingRegister(addr, data);
    }

    public void ExecuteDma(ref DmaConfiguration dc)
    {

        const uint maxTransferByteNumber = 0x100000;
        const byte addressAdd0 = 0;
        const byte addressAdd2 = 2;
        const byte addressAdd4 = 4;
        const byte addressAdd8 = 8;
        const byte addressAdd16 = 16;
        const byte addressAdd32 = 32;
        const byte addressAdd64 = 64;
        const byte addressAdd128 = 128;

        switch (dc.Mode)
        {
            case SaturnScuRegisters.Dxmd.DmaMode.Direct:
            {
                uint writeAddress = dc.WriteAddress;
                uint readAddress = dc.ReadAddress;
                var readAddressAdd = (byte)((dc.ReadAddValue == SaturnScuRegisters.Dxad.ReadAddressAddValue.Add4) ? 4 : 0);
                byte writeAddressAdd = 0;

                switch (dc.WriteAddValue)
                {
                    case SaturnScuRegisters.Dxad.WriteAddressAddValue.Add0: writeAddressAdd = addressAdd0; break;
                    case SaturnScuRegisters.Dxad.WriteAddressAddValue.Add2: writeAddressAdd = addressAdd2; break;
                    case SaturnScuRegisters.Dxad.WriteAddressAddValue.Add4: writeAddressAdd = addressAdd4; break;
                    case SaturnScuRegisters.Dxad.WriteAddressAddValue.Add8: writeAddressAdd = addressAdd8; break;
                    case SaturnScuRegisters.Dxad.WriteAddressAddValue.Add16: writeAddressAdd = addressAdd16; break;
                    case SaturnScuRegisters.Dxad.WriteAddressAddValue.Add32: writeAddressAdd = addressAdd32; break;
                    case SaturnScuRegisters.Dxad.WriteAddressAddValue.Add64: writeAddressAdd = addressAdd64; break;
                    case SaturnScuRegisters.Dxad.WriteAddressAddValue.Add128: writeAddressAdd = addressAdd128; break;
                }

                uint byteCounter = 0;
                uint wordCounter = 0;
                uint longCounter = 0;
                uint data = 0;
                uint readOffset = 0;

                switch (GetDmaBus(dc.WriteAddress))
                {
                    case DmaBus.ABus:
                    {

                        switch (GetScuRegion(readAddress))
                        {
                            case ScuRegion.Unknown: Trace.TraceWarning($"Unknown SCU region: {readAddress:X}"); break;
                            case ScuRegion.ABusCs2: break;
                            default: readAddressAdd = 4; break;
                        }

                        switch (GetScuRegion(writeAddress))
                        {
                            case ScuRegion.Rom:
                            case ScuRegion.Smpc:
                            case ScuRegion.BackupRam:
                            case ScuRegion.WorkRamL:
                            case ScuRegion.Minit:
                            case ScuRegion.Sinit:
                            case ScuRegion.ABusCs0:
                            case ScuRegion.ABusCs1:
                            case ScuRegion.ABusDummy: writeAddressAdd = 4; break;

                            default:
                                if (writeAddressAdd != 0) { writeAddressAdd = 4; }
                                break;
                        }

                        while (byteCounter < dc.TransferByteNumber)
                        {
                            data = _connections.ReadByte((readAddress & 0x7FFFFFFFu) + readOffset
                                                               + longCounter * readAddressAdd);
                            _connections.WriteByte(writeAddress + readOffset + longCounter * writeAddressAdd, (byte)data);

                            ++readOffset;

                            if (readOffset == 4)
                            {
                                readOffset = 0;
                                ++longCounter;
                            }
                            ++byteCounter;
                        }
                        break;
                    }
                    case DmaBus.BBus:
                    {

                        switch (GetScuRegion(readAddress))
                        {
                            case ScuRegion.Unknown: Trace.TraceWarning($"Unknown SCU region: {readAddress:X}"); break;
                            case ScuRegion.ABusCs2: break;
                            default: readAddressAdd = 4; break;
                        }

                        uint writeOffset = 0;

                        if ((readAddressAdd == 4) && (writeAddressAdd == 2))
                        {
                            _connections.BurstCopy(readAddress, writeAddress, dc.TransferByteNumber);
                        }
                        else
                        {
                            while (byteCounter < dc.TransferByteNumber)
                            {
                                data = _connections.ReadByte((readAddress & 0x7FFFFFFFu) + readOffset
                                                                   + longCounter * readAddressAdd);
                                _connections.WriteByte(writeAddress + writeOffset + wordCounter * writeAddressAdd, (byte)data);

                                ++readOffset;
                                ++writeOffset;

                                if (readOffset == 4)
                                {
                                    readOffset = 0;
                                    ++longCounter;
                                }
                                if (writeOffset == 2)
                                {
                                    writeOffset = 0;
                                    ++wordCounter;
                                }
                                ++byteCounter;
                            }
                        }
                        break;
                    }
                    case DmaBus.CpuBus:
                    {

                        switch (GetScuRegion(readAddress))
                        {
                            case ScuRegion.Unknown: Trace.TraceWarning($"Unknown SCU region: {readAddress:X}"); break;
                            case ScuRegion.ABusCs2: break;
                            default: readAddressAdd = 4; break;
                        }
                        writeAddressAdd = 4;

                        while (byteCounter < dc.TransferByteNumber)
                        {
                            data = _connections.ReadByte((readAddress & 0x7FFFFFFFu) + readOffset
                                                               + longCounter * readAddressAdd);
                            _connections.WriteByte(writeAddress + readOffset + longCounter * writeAddressAdd, (byte)data);

                            ++readOffset;

                            if (readOffset == 4)
                            {
                                readOffset = 0;
                                ++longCounter;
                            }

                            ++byteCounter;
                        }
                        break;
                    }
                    case DmaBus.DspBus:
                    {

                        Trace.TraceWarning("SCU DSP-bus transfer is unimplemented");

                        break;
                    }
                    default: break;
                }

                if (dc.ReadAddressUpdate == SaturnScuRegisters.Dxmd.ReadAddressUpdate.Update) { dc.ReadAddress += longCounter * readAddressAdd; }

                if (dc.WriteAddressUpdate == SaturnScuRegisters.Dxmd.WriteAddressUpdate.Update)
                {
                    dc.WriteAddress += wordCounter * writeAddressAdd;
                }

                SendDmaEndInterrupt(dc.Level);
                ResetDmaEnable(ref dc);

                break;
            }
            case SaturnScuRegisters.Dxmd.DmaMode.Indirect:
            {
                const byte storageBufferSize = 0xC;
                uint executeAddressStorageBuffer = dc.WriteAddress;

                var readAddressAdd = (byte)((dc.ReadAddValue == SaturnScuRegisters.Dxad.ReadAddressAddValue.Add4) ? 4 : 0);
                byte writeAddressAdd = 0;
                switch (dc.WriteAddValue)
                {
                    case SaturnScuRegisters.Dxad.WriteAddressAddValue.Add0: writeAddressAdd = addressAdd0; break;
                    case SaturnScuRegisters.Dxad.WriteAddressAddValue.Add2: writeAddressAdd = addressAdd2; break;
                    case SaturnScuRegisters.Dxad.WriteAddressAddValue.Add4: writeAddressAdd = addressAdd4; break;
                    case SaturnScuRegisters.Dxad.WriteAddressAddValue.Add8: writeAddressAdd = addressAdd8; break;
                    case SaturnScuRegisters.Dxad.WriteAddressAddValue.Add16: writeAddressAdd = addressAdd16; break;
                    case SaturnScuRegisters.Dxad.WriteAddressAddValue.Add32: writeAddressAdd = addressAdd32; break;
                    case SaturnScuRegisters.Dxad.WriteAddressAddValue.Add64: writeAddressAdd = addressAdd64; break;
                    case SaturnScuRegisters.Dxad.WriteAddressAddValue.Add128: writeAddressAdd = addressAdd128; break;
                }

                uint writeAddress = 0;
                uint readAddress = 0;
                uint count = 0;

                bool isTransferDone = false;
                while (!isTransferDone)
                {
                    readAddress  = _connections.ReadLong(executeAddressStorageBuffer + 8);
                    writeAddress = _connections.ReadLong(executeAddressStorageBuffer + 4);
                    count         = _connections.ReadLong(executeAddressStorageBuffer);
                    if (count == 0) { count = maxTransferByteNumber; }

                    uint byteCounter = 0;
                    uint wordCounter = 0;
                    uint longCounter = 0;
                    uint data = 0;
                    uint readOffset = 0;

                    var writeBus = GetDmaBus(dc.WriteAddress);
                    switch (writeBus)
                    {
                        case DmaBus.ABus:
                        {

                            switch (GetScuRegion(readAddress))
                            {
                                case ScuRegion.Unknown: Trace.TraceWarning($"Unknown SCU region: {readAddress:X}"); break;
                                case ScuRegion.ABusCs2: break;
                                default: readAddressAdd = 4; break;
                            }

                            switch (GetScuRegion(writeAddress))
                            {
                                case ScuRegion.Rom:
                                case ScuRegion.Smpc:
                                case ScuRegion.BackupRam:
                                case ScuRegion.WorkRamL:
                                case ScuRegion.Minit:
                                case ScuRegion.Sinit:
                                case ScuRegion.ABusCs0:
                                case ScuRegion.ABusCs1:
                                case ScuRegion.ABusDummy: writeAddressAdd = 4; break;

                                default:
                                    if (writeAddressAdd != 0) { writeAddressAdd = 4; }
                                    break;
                            }

                            while (byteCounter < count)
                            {
                                data = _connections.ReadByte((readAddress & 0x7FFFFFFFu) + readOffset
                                                                   + longCounter * readAddressAdd);
                                _connections.WriteByte(writeAddress + readOffset + longCounter * writeAddressAdd, (byte)data);

                                ++readOffset;

                                if (readOffset == 4)
                                {
                                    readOffset = 0;
                                    ++longCounter;
                                }
                                ++byteCounter;
                            }
                            break;
                        }
                        case DmaBus.BBus:
                        {

                            switch (GetScuRegion(readAddress))
                            {
                                case ScuRegion.Unknown: Trace.TraceWarning($"Unknown SCU region: {readAddress:X}"); break;
                                case ScuRegion.ABusCs2: break;
                                default: readAddressAdd = 4; break;
                            }

                            uint writeOffset = 0;

                            while (byteCounter < count)
                            {
                                data = _connections.ReadByte((readAddress & 0x7FFFFFFFu) + readOffset
                                                                   + longCounter * readAddressAdd);
                                _connections.WriteByte(writeAddress + writeOffset + wordCounter * writeAddressAdd, (byte)data);

                                ++readOffset;
                                ++writeOffset;

                                if (readOffset == 4)
                                {
                                    readOffset = 0;
                                    ++longCounter;
                                }
                                if (writeOffset == 2)
                                {
                                    writeOffset = 0;
                                    ++wordCounter;
                                }
                                ++byteCounter;
                            }
                            break;
                        }
                        case DmaBus.CpuBus:
                        {

                            switch (GetScuRegion(readAddress))
                            {
                                case ScuRegion.Unknown: Trace.TraceWarning($"Unknown SCU region: {readAddress:X}"); break;
                                case ScuRegion.ABusCs2: break;
                                default: readAddressAdd = 4; break;
                            }
                            writeAddressAdd = 4;

                            while (byteCounter < count)
                            {
                                data = _connections.ReadByte((readAddress & 0x7FFFFFFFu) + readOffset
                                                                   + longCounter * readAddressAdd);
                                _connections.WriteByte(writeAddress + readOffset + longCounter * writeAddressAdd, (byte)data);

                                ++readOffset;

                                if (readOffset == 4)
                                {
                                    readOffset = 0;
                                    ++longCounter;
                                }

                                ++byteCounter;
                            }
                            break;
                        }
                        case DmaBus.DspBus:
                        {

                            Trace.TraceWarning("SCU DSP-bus transfer is unimplemented");

                            break;
                        }
                        default: break;
                    }

                    if ((readAddress & IndirectDmaEndCode) > 0) { isTransferDone = true; }
                    executeAddressStorageBuffer += storageBufferSize;
                }

                if (dc.WriteAddressUpdate == SaturnScuRegisters.Dxmd.WriteAddressUpdate.Update)
                {
                    DmaUpdateWriteAddress(dc.Level, executeAddressStorageBuffer);
                }

                SendDmaEndInterrupt(dc.Level);
                ResetDmaEnable(ref dc);

                break;
            }
            default: break;
        }

    }

    public void SetInterruptStatusRegister(SaturnInterrupt i)
    { _regs.ist |= 1u << i.StatusBit; }

    public void ResetInterruptStatusRegister(SaturnInterrupt i)
    { _regs.ist &= ~(1u << i.StatusBit); }

    public void GenerateInterrupt(SaturnInterrupt i)
    {
        if (i.Level != 0)
        {
            if (!IsInterruptMasked(i))
            {
                SetInterruptStatusRegister(i);
                _connections.MasterInterrupt(i);
            }

            if (_connections.IsSlaveOn())
            {
                switch (i.Vector)
                {
                    case SaturnInterruptSources.VectorNmi:
                    case SaturnInterruptSources.VectorFrtInputCapture:
                    case SaturnInterruptSources.VectorFrtInputCapture2: _connections.SlaveInterrupt(i); break;
                    case SaturnInterruptSources.VectorVBlankIn: _connections.SlaveInterrupt(SaturnInterruptSources.VBlankInSlave); break;
                    case SaturnInterruptSources.VectorHBlankIn: _connections.SlaveInterrupt(SaturnInterruptSources.HBlankInSlave); break;
                }
            }
        }
    }

    public bool IsInterruptMasked(SaturnInterrupt i)
    {
        switch (i.Vector)
        {
            case SaturnInterruptSources.VectorNmi: return true;
            case SaturnInterruptSources.VectorSystemManager:
                if (_connections.IsStv()) { return true; }
                return ((_regs.ims >> i.MaskBit) & 1) != 0;
            case SaturnInterruptSources.VectorVBlankIn:
            case SaturnInterruptSources.VectorVBlankOut:
            case SaturnInterruptSources.VectorHBlankIn:
            case SaturnInterruptSources.VectorTimer0:
            case SaturnInterruptSources.VectorTimer1:
            case SaturnInterruptSources.VectorDspEnd:
            case SaturnInterruptSources.VectorSoundRequest:
            case SaturnInterruptSources.VectorPadInterrupt:
            case SaturnInterruptSources.VectorLevel2DmaEnd:
            case SaturnInterruptSources.VectorLevel1DmaEnd:
            case SaturnInterruptSources.VectorLevel0DmaEnd:
            case SaturnInterruptSources.VectorDmaIllegal:
            case SaturnInterruptSources.VectorSpriteDrawEnd:
            case SaturnInterruptSources.VectorExternal00:
            case SaturnInterruptSources.VectorExternal01:
            case SaturnInterruptSources.VectorExternal02:
            case SaturnInterruptSources.VectorExternal03:
            case SaturnInterruptSources.VectorExternal04:
            case SaturnInterruptSources.VectorExternal05:
            case SaturnInterruptSources.VectorExternal06:
            case SaturnInterruptSources.VectorExternal07:
            case SaturnInterruptSources.VectorExternal08:
            case SaturnInterruptSources.VectorExternal09:
            case SaturnInterruptSources.VectorExternal10:
            case SaturnInterruptSources.VectorExternal11:
            case SaturnInterruptSources.VectorExternal12:
            case SaturnInterruptSources.VectorExternal13:
            case SaturnInterruptSources.VectorExternal14:
            case SaturnInterruptSources.VectorExternal15: return ((_regs.ims >> i.MaskBit) & 1) != 0;
            default: break;
        }
        return false;
    }

    public bool IsInterruptExecuting(SaturnInterrupt i)
    {
        return ((_regs.ist >> i.StatusBit) & 1) != 0;
    }

    public void SendStartFactor(SaturnScuRegisters.Dxmd.StartingFactorSelect sfs)
    {
        foreach (ref DmaConfiguration dc in CollectionsMarshal.AsSpan(_dmaQueue))
        {
            if (dc.Status == DmaStatus.WaitingStartFactor)
            {
                if (dc.StartingFactorSelect == sfs) { dc.Status = DmaStatus.Queued; }
            }
        }

        if (_dmaQueue.Count != 0) { ActivateDma(); }
    }

    public void ClearInterruptFlag(SaturnInterrupt i)
    {
        _regs.ist &= ~(1u << i.StatusBit);
    }

    private void InitializeRegisters()
    {

        const uint addressAddDefaultValue = 0x101;
        _regs.d0ad                               = addressAddDefaultValue;
        _regs.d1ad                               = addressAddDefaultValue;
        _regs.d2ad                               = addressAddDefaultValue;
        _regs.d0en                               = 0;
        _regs.d1en                               = 0;
        _regs.d2en                               = 0;
        const uint modeDefaultValue = 0x7;
        _regs.d0md                               = modeDefaultValue;
        _regs.d1md                               = modeDefaultValue;
        _regs.d2md                               = modeDefaultValue;
        WriteBackingRegister(DmaForcedStop, 0x00000000);
        WriteBackingRegister(DmaStatusRegister, 0x00000000);

        WriteBackingRegister(DspProgramControlPort, 0x00000000);
        WriteBackingRegister(DspDataRamAddressPort, 0x00000000);
        WriteBackingRegister(DspDataRamDataPort, 0x00000000);

        _regs.t1md = 0;

        const uint interruptMaskDefaultValue = 0xBFFF;
        _regs.ims                                   = interruptMaskDefaultValue;
        _regs.ist                                   = 0;

        WriteBackingRegister(ABusInterruptAcknowledge, 0x00000000);
        WriteBackingRegister(ABusSetRegister, 0x00000000);
        WriteBackingRegister((ABusSetRegister + 4), 0x00000000);
        WriteBackingRegister(ABusRefreshRegister, 0x00000000);

        WriteBackingRegister(ScuSdramSelectRegister, 0x00000000);
        WriteBackingRegister(ScuVersionRegister, 0x00000000);
    }

    private DmaConfiguration ConfigureDmaTransfer(DmaLevel level)
    {
        var dc = new DmaConfiguration();
        dc.Level = level;
        switch (level)
        {
            case DmaLevel.Level0:
                InitializeDmaReadAddress(ref dc, _regs.d0r);
                InitializeDmaWriteAddress(ref dc, _regs.d0w);
                InitializeDmaTransferByteNumber(ref dc);
                InitializeDmaAddressAdd(ref dc, _regs.d0ad);
                InitializeDmaEnable(ref dc, _regs.d0en);
                InitializeDmaMode(ref dc, _regs.d0md);
                break;
            case DmaLevel.Level1:
                InitializeDmaReadAddress(ref dc, _regs.d1r);
                InitializeDmaWriteAddress(ref dc, _regs.d1w);
                InitializeDmaTransferByteNumber(ref dc);
                InitializeDmaAddressAdd(ref dc, _regs.d1ad);
                InitializeDmaEnable(ref dc, _regs.d1en);
                InitializeDmaMode(ref dc, _regs.d1md);
                break;
            case DmaLevel.Level2:
                InitializeDmaReadAddress(ref dc, _regs.d2r);
                InitializeDmaWriteAddress(ref dc, _regs.d2w);
                InitializeDmaTransferByteNumber(ref dc);
                InitializeDmaAddressAdd(ref dc, _regs.d2ad);
                InitializeDmaEnable(ref dc, _regs.d2en);
                InitializeDmaMode(ref dc, _regs.d2md);
                break;
            default: break;
        }
        return dc;
    }

    private static void InitializeDmaMode(ref DmaConfiguration dc, uint reg)
    {
        dc.Mode               = SaturnScuRegisters.Dxmd.GetDxmod(reg);
        dc.ReadAddressUpdate    = SaturnScuRegisters.Dxmd.GetDxrup(reg);
        dc.WriteAddressUpdate   = SaturnScuRegisters.Dxmd.GetDxwup(reg);
        dc.StartingFactorSelect = SaturnScuRegisters.Dxmd.GetDxft(reg);
    }

    private static void InitializeDmaEnable(ref DmaConfiguration dc, uint reg)
    {
        dc.Enable   = SaturnScuRegisters.Dxen.GetDxen(reg);
        dc.Starting = SaturnScuRegisters.Dxen.GetDxgo(reg);
    }

    private static void InitializeDmaAddressAdd(ref DmaConfiguration dc, uint reg)
    {
        dc.ReadAddValue  = SaturnScuRegisters.Dxad.GetDxra(reg);
        dc.WriteAddValue = SaturnScuRegisters.Dxad.GetDxwa(reg);
    }

    private void InitializeDmaTransferByteNumber(ref DmaConfiguration dc)
    {
        switch (dc.Level)
        {
            case DmaLevel.Level0: dc.TransferByteNumber = SaturnScuRegisters.D0c.GetTbn(_regs.d0c); break;
            case DmaLevel.Level1: dc.TransferByteNumber = SaturnScuRegisters.D1c.GetTbn(_regs.d1c); break;
            case DmaLevel.Level2: dc.TransferByteNumber = SaturnScuRegisters.D2c.GetTbn(_regs.d2c); break;
            default: break;
        }
    }

    private static void InitializeDmaWriteAddress(ref DmaConfiguration dc, uint reg)
    {
        dc.WriteAddress = SaturnScuRegisters.Dxw.GetWa(reg);
    }

    private static void InitializeDmaReadAddress(ref DmaConfiguration dc, uint reg)
    {
        dc.ReadAddress = SaturnScuRegisters.Dxr.GetRa(reg);
    }

    private void AddDmaToQueue(DmaConfiguration dc)
    {
        var config = dc;

        switch (config.StartingFactorSelect)
        {
            case SaturnScuRegisters.Dxmd.StartingFactorSelect.DmaStartFactor: config.Status = DmaStatus.Queued; break;
            default: config.Status = DmaStatus.WaitingStartFactor; break;
        }
        _dmaQueue.Add(config);
    }

    private void ActivateDma()
    {

        DmaSort();

        if (_dmaQueue[0].Status == DmaStatus.Active) { return; }

        foreach (ref DmaConfiguration dc in CollectionsMarshal.AsSpan(_dmaQueue))
        {
            if (dc.Status == DmaStatus.Queued)
            {
                ExecuteDma(ref dc);
                if (dc.Enable == SaturnScuRegisters.Dxen.DmaEnable.Enabled)
                {
                    dc.Status = DmaStatus.WaitingStartFactor;
                }
                else
                {
                    dc.Status = DmaStatus.Finished;
                }
            }
        }
    }

    private static DmaBus GetDmaBus(uint address)
    {
        uint a = address & 0xFFFFFFF;

        switch (GetScuRegion(a))
        {
            case ScuRegion.WorkRamH:
            case ScuRegion.WorkRamL:
            case ScuRegion.BackupRam:
            case ScuRegion.Rom:
            case ScuRegion.Smpc:
            {
                return DmaBus.CpuBus;
            }
            case ScuRegion.ABusCs0:
            case ScuRegion.ABusCs1:
            case ScuRegion.ABusDummy:
            case ScuRegion.ABusCs2:
            {
                return DmaBus.ABus;
            }
            case ScuRegion.Vdp1:
            case ScuRegion.Vdp2:
            case ScuRegion.Sound:
            {
                return DmaBus.BBus;
            }
            default:

                return DmaBus.UnknownBus;
        }
    }

    private static ScuRegion GetScuRegion(uint address)
    {
        uint a = address & 0xFFFFFFF;

        const uint romStart = 0;
        const uint romEnd = 0x80000;
        if ((a >= romStart) && (a < romEnd)) { return ScuRegion.Rom; }

        const uint smpcStart = 0x100000;
        const uint smpcEnd = 0x100080;
        if ((a >= smpcStart) && (a < smpcEnd)) { return ScuRegion.Smpc; }

        const uint backupRamStart = 0x180000;
        const uint backupRamEnd = 0x190000;
        if ((a >= backupRamStart) && (a < backupRamEnd)) { return ScuRegion.BackupRam; }

        const uint workramLStart = 0x200000;
        const uint workramLEnd = 0x300000;
        if ((a >= workramLStart) && (a < workramLEnd)) { return ScuRegion.WorkRamL; }

        const uint minitStart = 0x1000000;
        const uint minitEnd = 0x1000004;
        if ((a >= minitStart) && (a < minitEnd)) { return ScuRegion.Minit; }

        const uint sinitStart = 0x1800000;
        const uint sinitEnd = 0x1800004;
        if ((a >= sinitStart) && (a < sinitEnd)) { return ScuRegion.Sinit; }

        const uint aBusCs0Start = 0x2000000;
        const uint aBusCs0End = 0x4000000;
        if ((a >= aBusCs0Start) && (a < aBusCs0End)) { return ScuRegion.ABusCs0; }

        const uint aBusCs1Start = 0x4000000;
        const uint aBusCs1End = 0x5000000;
        if ((a >= aBusCs1Start) && (a < aBusCs1End)) { return ScuRegion.ABusCs1; }

        const uint aBusDummyStart = 0x5000000;
        const uint aBusDummyEnd = 0x5800000;
        if ((a >= aBusDummyStart) && (a < aBusDummyEnd)) { return ScuRegion.ABusDummy; }

        const uint aBusCs2Start = 0x5800000;
        const uint aBusCs2End = 0x5900000;
        if ((a >= aBusCs2Start) && (a < aBusCs2End)) { return ScuRegion.ABusCs2; }

        const uint soundStart = 0x5A00000;
        const uint soundEnd = 0x5B00EE4;
        if ((a >= soundStart) && (a < soundEnd)) { return ScuRegion.Sound; }

        const uint vdp1StartArea1 = 0x5C00000;
        const uint vdp1EndArea1 = 0x5CC0000;
        if ((a >= vdp1StartArea1) && (a < vdp1EndArea1)) { return ScuRegion.Vdp1; }

        const uint vdp1StartArea2 = 0x5D00000;
        const uint vdp1EndArea2 = 0x5D00018;
        if ((a >= vdp1StartArea2) && (a < vdp1EndArea2)) { return ScuRegion.Vdp1; }

        const uint vdp2StartArea1 = 0x5E00000;
        const uint vdp2EndArea1 = 0x5E80000;
        if ((a >= vdp2StartArea1) && (a < vdp2EndArea1)) { return ScuRegion.Vdp2; }

        const uint vdp2StartArea2 = 0x5F00000;
        const uint vdp2EndArea2 = 0x5F01000;
        if ((a >= vdp2StartArea2) && (a < vdp2EndArea2)) { return ScuRegion.Vdp2; }

        const uint vdp2StartArea3 = 0x5F80000;
        const uint vdp2EndArea3 = 0x5F80120;
        if ((a >= vdp2StartArea3) && (a < vdp2EndArea3)) { return ScuRegion.Vdp2; }

        const uint scuRegsStart = 0x5FE0000;
        const uint scuRegsEnd = 0x5FE00D0;
        if ((a >= scuRegsStart) && (a < scuRegsEnd)) { return ScuRegion.ScuRegister; }

        const uint workramHStart = 0x6000000;
        const uint workramHEnd = 0x6100000;
        if ((a >= workramHStart) && (a < workramHEnd)) { return ScuRegion.WorkRamH; }

        return ScuRegion.Unknown;
    }

    private void DmaTest()
    {
        var dc = new DmaConfiguration();
        dc.Status = DmaStatus.Finished;
        _dmaQueue.Add(dc);
        dc.Status = DmaStatus.Queued;
        _dmaQueue.Add(dc);
        dc.StartingFactorSelect = SaturnScuRegisters.Dxmd.StartingFactorSelect.HBlankIn;
        dc.Status             = DmaStatus.WaitingStartFactor;
        _dmaQueue.Add(dc);
        dc.StartingFactorSelect = SaturnScuRegisters.Dxmd.StartingFactorSelect.HBlankIn;
        _dmaQueue.Add(dc);
        dc.StartingFactorSelect = SaturnScuRegisters.Dxmd.StartingFactorSelect.VBlankOut;
        _dmaQueue.Add(dc);

        SendStartFactor(SaturnScuRegisters.Dxmd.StartingFactorSelect.HBlankIn);
    }

    private void SendDmaEndInterrupt(DmaLevel l)
    {
        switch (l)
        {
            case DmaLevel.Level0: GenerateInterrupt(SaturnInterruptSources.Level0DmaEnd); break;
            case DmaLevel.Level1: GenerateInterrupt(SaturnInterruptSources.Level1DmaEnd); break;
            case DmaLevel.Level2: GenerateInterrupt(SaturnInterruptSources.Level2DmaEnd); break;
            case DmaLevel.LevelUnknown: break;
        }
    }

    private void ResetDmaEnable(ref DmaConfiguration dc)
    {
        if (dc.Enable == SaturnScuRegisters.Dxen.DmaEnable.Enabled && dc.StartingFactorSelect == SaturnScuRegisters.Dxmd.StartingFactorSelect.DmaStartFactor)
        {
            dc.Enable = SaturnScuRegisters.Dxen.DmaEnable.Disabled;
        }
    }

    private void DmaUpdateWriteAddress(DmaLevel l, uint data)
    {
        switch (l)
        {
            case DmaLevel.Level0: _regs.d0w = data; break;
            case DmaLevel.Level1: _regs.d1w = data; break;
            case DmaLevel.Level2: _regs.d2w = data; break;
            case DmaLevel.LevelUnknown: break;
        }
    }

    private void DmaSort()
    {
        _dmaQueue.Sort(static (first, second) => first.Status.CompareTo(second.Status));
    }

    public uint GetTimer0CompareValue()
    { return SaturnScuRegisters.T0c.GetT0c(_regs.t0c); }

    public bool IsTimer1Enabled()
    {
        return SaturnScuRegisters.T1md.GetTenb(_regs.t1md) == SaturnScuRegisters.T1md.TimerEnable.TimerOperationOn;
    }

    public void OnVblankIn()
    {
        GenerateInterrupt(SaturnInterruptSources.VBlankIn);
        SendStartFactor(SaturnScuRegisters.Dxmd.StartingFactorSelect.VBlankIn);
    }
    public void OnVblankOut()
    {
        GenerateInterrupt(SaturnInterruptSources.VBlankOut);
        SendStartFactor(SaturnScuRegisters.Dxmd.StartingFactorSelect.VBlankOut);
    }
    public void OnHblankIn()
    {
        GenerateInterrupt(SaturnInterruptSources.HBlankIn);
        SendStartFactor(SaturnScuRegisters.Dxmd.StartingFactorSelect.HBlankIn);
    }
    public void OnTimer0()
    {
        GenerateInterrupt(SaturnInterruptSources.Timer0);
        SendStartFactor(SaturnScuRegisters.Dxmd.StartingFactorSelect.Timer0);
    }

    private uint ReadBackingRegister(uint address)
        => BinaryPrimitives.ReadUInt32BigEndian(_connections.BackingRegisters.Span[(int)(address & ScuMemoryMask)..]);

    private void WriteBackingRegister(uint address, uint value)
        => BinaryPrimitives.WriteUInt32BigEndian(_connections.BackingRegisters.Span[(int)(address & ScuMemoryMask)..], value);
}
