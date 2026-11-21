// Copyright 2003-2006 Guillaume Duhamel
// Copyright 2005-2006 Theo Berkau
// Copyright 2015 Shinya Miyamoto (devmiyax)
// Translated from Yabause scu.c.
// SPDX-License-Identifier: GPL-2.0-or-later

#if OPTIMIZED_DMA
using System.Buffers.Binary;
using System.Runtime.InteropServices;
#endif

namespace RetroMediaLab.Chips.Controllers.Sega;

public sealed partial class SaturnScuInterpreter
{
    private readonly ScuRegisters _registers = new();
    private readonly DspRegisters _dsp = new();
    private readonly int[] _incrementFlags = new int[4];
    private readonly BreakpointState _breakpoints = new();
    private readonly DspInterface _dspInterface;
    private readonly SaturnMemory.Sh2MemoryMap _masterMemory;
    private readonly Action<uint, uint> _writeNotify;
    private readonly Action<int, int> _sendMasterInterrupt;
    private readonly bool _useDmaTiming;

    public SaturnScuInterpreter(
        SaturnMemory.Sh2MemoryMap masterMemory,
        Action<uint, uint> writeNotify,
        Action<int, int> sendMasterInterrupt,
        bool useDmaTiming)
    {
        _masterMemory = masterMemory;
        _writeNotify = writeNotify;
        _sendMasterInterrupt = sendMasterInterrupt;
        _useDmaTiming = useDmaTiming;
        for (int i = 0; i < MaxBreakpoints; i++)
            _breakpoints.CodeBreakpoints[i].Address = uint.MaxValue;
        _breakpoints.NumberOfCodeBreakpoints = 0;
        _breakpoints.BreakpointCallback = null;
        _breakpoints.InBreakpoint = 0;

        _dspInterface = InitializeDspInterface();
    }

    public void SetupQuickLoad()
    {
        _registers.D1AD = _registers.D2AD = 0;
        _registers.D0EN = 0x101;
        _registers.IST = 0x2006;
        _registers.AIACK = 1;
        _registers.ASR0 = _registers.ASR1 = 0x1FF01FF0;
        _registers.AREF = 0x1F;
        _registers.RSEL = 1;
    }

    public void Reset()
    {
        _cdInterruptAsserted = false;
        _cdInterruptProhibited = false;

        _registers.D0AD = _registers.D1AD = _registers.D2AD = 0x101;
        _registers.D0EN = _registers.D1EN = _registers.D2EN = 0;
        _registers.D0MD = _registers.D1MD = _registers.D2MD = 7;
        _registers.DSTP = 0;
        _registers.DSTA = 0;
        _dsp.ProgControlPort.All = 0;
        _registers.PDA = 0;
        _registers.T1MD = 0;
        _registers.IMS = 0xBFFF;
        _registers.IST = 0;
        _registers.AIACK = 0;
        _registers.ASR0 = _registers.ASR1 = 0;
        _registers.AREF = 0;
        _registers.RSEL = 0;
        _registers.VER = 4;
        _registers.Timer0 = 0;
        _registers.Timer1 = 0;

        Array.Clear(_registers.Interrupts);
        _registers.NumberOfInterrupts = 0;
    }

#if OPTIMIZED_DMA
    private static readonly byte[] DmaMemoryType = InitializeDmaMemoryType();

    private static byte[] InitializeDmaMemoryType()
    {
        var memoryType = new byte[0x20000000 >> 19];
        memoryType[0x00200000 >> 19] = 0x24;
        memoryType[0x00280000 >> 19] = 0x24;
        memoryType[0x05A00000 >> 19] = 0x22;
        memoryType[0x05A80000 >> 19] = 0x22;
        memoryType[0x05C00000 >> 19] = 0x12;
        memoryType[0x05E00000 >> 19] = 0x12;
        memoryType[0x05E80000 >> 19] = 0x12;
        memoryType[0x05F00000 >> 19] = 0x23;
        memoryType.AsSpan((0x06000000 >> 19), 0x02000000 >> 19).Fill(0x24);
        return memoryType;
    }

    private Span<byte> DmaMemoryPointer(uint address)
    {
        if (_masterMemory.DmaPeripherals is not { } peripherals)
            return default;

        uint page = (address & 0x1FF80000) >> 19;
        switch (DmaMemoryType[page])
        {
            case 0x12:
                switch (page)
                {
                    case 0x05C00000 >> 19: return peripherals.Vdp1.Ram.AsSpan((int)(address & 0x7FFFF));
                    case 0x05E00000 >> 19:
                    case 0x05E80000 >> 19: return peripherals.Vdp2.DmaRam[(int)(address & 0x7FFFF)..];
                    default: return default;
                }
            case 0x22: return peripherals.SoundChip is { } sound ? sound.DmaSoundRam[(int)(address & 0x7FFFF)..] : default;
            case 0x23: return peripherals.Vdp2.DmaColorRam[(int)(address & 0xFFF)..];
            case 0x24:
                if (page == (0x00200000 >> 19))
                    return _masterMemory.InstructionMemory.DmaLowWram[(int)(address & 0xFFFFF)..];
                return _masterMemory.InstructionMemory.DmaHighWram[(int)(address & 0xFFFFF)..];
            default: return default;
        }
    }

#endif

    private void DoDma(uint readAddress, uint readAdd, uint writeAddress, uint writeAdd, uint transferSize)
    {
        if (readAdd == 0)
        {
#if OPTIMIZED_DMA
            if (readAddress == 0x25818000 && writeAdd == 4 && _masterMemory.DmaPeripherals is { } peripherals)
            {
                int offset = (int)(writeAddress & 0xFFFFF);
                uint count = transferSize / 4;
                if ((writeAddress & 0x1E000000) == 0x06000000)
                {
                    Span<byte> highWram = _masterMemory.InstructionMemory.DmaHighWram[offset..];
                    if (count <= (uint)highWram.Length / 4)
                    {
                        peripherals.Cs2.RapidCopyT2(highWram, count);
                        _writeNotify(writeAddress, transferSize);
                        return;
                    }
                }
                else if ((writeAddress & 0x1FF00000) == 0x00200000)
                {
                    Span<byte> lowWram = _masterMemory.InstructionMemory.DmaLowWram[offset..];
                    if (count <= (uint)lowWram.Length / 4)
                    {
                        peripherals.Cs2.RapidCopyT2(lowWram, count);
                        _writeNotify(writeAddress, transferSize);
                        return;
                    }
                }
            }
#endif

            bool constantSource = ((readAddress & 0x1FF00000) == 0x00200000)
                                        || ((readAddress & 0x1E000000) == 0x06000000)
                                        || ((readAddress & 0x1FF00000) == 0x05A00000)
                                        || ((readAddress & 0x1DF00000) == 0x05C00000);

            if ((writeAddress & 0x1FFFFFFF) >= 0x5A00000
                    && (writeAddress & 0x1FFFFFFF) < 0x5FF0000)
                    {

                if (constantSource)
                {
                    uint counter = 0;
                    uint val;
                    if ((readAddress & 2) != 0)
                    {
                        val = (uint)_masterMemory.MappedMemoryReadWordNocache(readAddress) << 16
                                | _masterMemory.MappedMemoryReadWordNocache(readAddress + 2);
                    }
                    else
                    {
                        val = _masterMemory.MappedMemoryReadLongNocache(readAddress);
                    }
                    while (counter < transferSize)
                    {
                        _masterMemory.MappedMemoryWriteWordNocache(writeAddress, (ushort)(val >> 16));
                        writeAddress += writeAdd;
                        _masterMemory.MappedMemoryWriteWordNocache(writeAddress, unchecked((ushort)val));
                        writeAddress += writeAdd;
                        counter += 4;
                    }
                }
                else
                {
                    uint counter = 0;
                    while (counter < transferSize)
                    {
                        uint tmp = _masterMemory.MappedMemoryReadLongNocache(readAddress);
                        _masterMemory.MappedMemoryWriteWordNocache(writeAddress, (ushort)(tmp >> 16));
                        writeAddress += writeAdd;
                        _masterMemory.MappedMemoryWriteWordNocache(writeAddress, unchecked((ushort)tmp));
                        writeAddress += writeAdd;
                        readAddress += readAdd;
                        counter += 4;
                    }
                }
            }
            else
            {

                uint start = writeAddress;
                if (constantSource)
                {
                    uint val = _masterMemory.MappedMemoryReadLongNocache(readAddress);
                    uint counter = 0;
                    while (counter < transferSize)
                    {
                        _masterMemory.MappedMemoryWriteLongNocache(writeAddress, val);
                        readAddress += readAdd;
                        writeAddress += writeAdd;
                        counter += 4;
                    }
                }
                else
                {
                    uint counter = 0;
                    while (counter < transferSize)
                    {
                        _masterMemory.MappedMemoryWriteLongNocache(writeAddress,
                                                        _masterMemory.MappedMemoryReadLongNocache(readAddress));
                        readAddress += readAdd;
                        writeAddress += writeAdd;
                        counter += 4;
                    }
                }

                _writeNotify(start, writeAddress - start);
            }

        }

        else
        {
#if OPTIMIZED_DMA
            int sourceType = DmaMemoryType[(readAddress & 0x1FF80000) >> 19];
            int destinationType = DmaMemoryType[(writeAddress & 0x1FF80000) >> 19];
            if (writeAdd == ((destinationType & 2) != 0 ? 2u : 4u))
            {
                ReadOnlySpan<byte> source = DmaMemoryPointer(readAddress);
                Span<byte> destination = DmaMemoryPointer(writeAddress);
                if (transferSize <= (uint)source.Length && transferSize <= (uint)destination.Length)
                {
                    source = source[..(int)transferSize];
                    destination = destination[..(int)transferSize];
                    if (!BitConverter.IsLittleEndian)
                    {
                        if ((sourceType & 0x30) != 0 && (destinationType & 0x30) != 0)
                        {
                            source.CopyTo(destination);
                            if (destinationType == 0x24)
                                _writeNotify(writeAddress, transferSize);
                            else if (destinationType == 0x22)
                                _masterMemory.DmaPeripherals!.SoundChip!.NotifyDmaSoundRamWrite(writeAddress & 0x7FFFF, transferSize);
                            return;
                        }
                    }
                    else if ((sourceType & destinationType & 0x10) != 0)
                    {
                        source.CopyTo(destination);
                        return;
                    }
                    else if ((sourceType & destinationType & 0x20) != 0)
                    {
                        source.CopyTo(destination);
                        if (destinationType == 0x24)
                            _writeNotify(writeAddress, transferSize);
                        else if (destinationType == 0x22)
                            _masterMemory.DmaPeripherals!.SoundChip!.NotifyDmaSoundRamWrite(writeAddress & 0x7FFFF, transferSize);
                        return;
                    }
                    else if ((sourceType | destinationType) >> 4 == 3)
                    {
                        if (((readAddress | writeAddress) & 2) != 0)
                        {
                            if ((transferSize & 1) == 0)
                            {
                                ReadOnlySpan<ushort> source16 = MemoryMarshal.Cast<byte, ushort>(source);
                                Span<ushort> destination16 = MemoryMarshal.Cast<byte, ushort>(destination);
                                int index = 0;
                                for (; index + 3 < source16.Length; index += 4)
                                {
                                    ushort val0 = BinaryPrimitives.ReverseEndianness(source16[index]);
                                    ushort val1 = BinaryPrimitives.ReverseEndianness(source16[index + 1]);
                                    ushort val2 = BinaryPrimitives.ReverseEndianness(source16[index + 2]);
                                    ushort val3 = BinaryPrimitives.ReverseEndianness(source16[index + 3]);
                                    destination16[index] = val0;
                                    destination16[index + 1] = val1;
                                    destination16[index + 2] = val2;
                                    destination16[index + 3] = val3;
                                }
                                for (; index < source16.Length; index++)
                                    destination16[index] = BinaryPrimitives.ReverseEndianness(source16[index]);
                                return;
                            }
                        }
                        else if ((transferSize & 3) == 0)
                        {
                            ReadOnlySpan<uint> source32 = MemoryMarshal.Cast<byte, uint>(source);
                            Span<uint> destination32 = MemoryMarshal.Cast<byte, uint>(destination);
                            int index = 0;
                            for (; index + 3 < source32.Length; index += 4)
                            {
                                uint val0 = ByteSwap16(source32[index]);
                                uint val1 = ByteSwap16(source32[index + 1]);
                                uint val2 = ByteSwap16(source32[index + 2]);
                                uint val3 = ByteSwap16(source32[index + 3]);
                                destination32[index] = val0;
                                destination32[index + 1] = val1;
                                destination32[index + 2] = val2;
                                destination32[index + 3] = val3;
                            }
                            for (; index < source32.Length; index++)
                                destination32[index] = ByteSwap16(source32[index]);
                            return;
                        }
                    }
                }
            }
#endif

            if ((writeAddress & 0x1FFFFFFF) >= 0x5A00000
                    && (writeAddress & 0x1FFFFFFF) < 0x5FF0000)
                    {

                uint counter = 0;
                if ((readAddress & 2) != 0)
                {
                    ushort tmp = _masterMemory.MappedMemoryReadWordNocache(readAddress);
                    _masterMemory.MappedMemoryWriteWordNocache(writeAddress, tmp);
                    writeAddress += writeAdd;
                    readAddress += 2;
                    counter += 2;
                }
                if (transferSize >= 3)
               
              {
                    while (counter < transferSize - 2)
                    {
                        uint tmp = _masterMemory.MappedMemoryReadLongNocache(readAddress);
                        _masterMemory.MappedMemoryWriteWordNocache(writeAddress, (ushort)(tmp >> 16));
                        writeAddress += writeAdd;
                        _masterMemory.MappedMemoryWriteWordNocache(writeAddress, unchecked((ushort)tmp));
                        writeAddress += writeAdd;
                        readAddress += 4;
                        counter += 4;
                    }
                }
                if (counter < transferSize)
                {
                    ushort tmp = _masterMemory.MappedMemoryReadWordNocache(readAddress);
                    _masterMemory.MappedMemoryWriteWordNocache(writeAddress, tmp);
                    writeAddress += writeAdd;
                    readAddress += 2;
                    counter += 2;
                }
            }
            else
            {
                uint counter = 0;
                uint start = writeAddress;
                while (counter < transferSize)
                {
                    _masterMemory.MappedMemoryWriteLongNocache(writeAddress, _masterMemory.MappedMemoryReadLongNocache(readAddress));
                    readAddress += 4;
                    writeAddress += writeAdd;
                    counter += 4;
                }

                _writeNotify(start, writeAddress - start);
            }

        }
    }

#if OPTIMIZED_DMA
    private static uint ByteSwap16(uint value) =>
        ((value >> 8) & 0x00FF00FF) | ((value & 0x00FF00FF) << 8);

#endif

    private const int DmaStatusFinished = 0;
    private const int DmaStatusWaitingFactor = 1;
    private const int DmaStatusQueued = 2;
    private const int DmaStatusActive = 3;
    private const int DmaTransferAToB = 1;
    private const int DmaTransferCpuToB = 2;
    private const int DmaTransferAToCpu = 3;
    private const int DmaTransferBToCpu = 4;
    private const int DmaTransferCpuToA = 5;
    private const int DmaTransferBToA = 6;
    private const int DspCpuBus = 1;
    private const int DspBBus = 2;
    private const int DspABus = 3;

    private struct QueuedDma
    {
        public uint ReadAddress;
        public uint WriteAddress;
        public uint Count;
        public uint OriginalCount;
        public int CountMod4;
        public int Status;
        public int SecondWord;
        public uint Buffer;
        public uint ReadAdd;
        public int AddSetting;
        public uint WriteAdd;
        public int BusType;
        public int IsIndirect;
        public int ReadAddrUpdate;
        public int WriteAddrUpdate;
        public int StartingFactor;
        public int Level;
        public uint IndirectAddress;
        public int IsLastIndirect;
        public int IsDsp;
        public int DspDmaType;
        public int DspBank;
        public int DspAdd;
        public uint DspAddress;
        public byte ProgramRamCounter;
        public byte DspAddSetting;
        public uint DspOrigCount;
        public byte Ct;
        public int NumWritten;
        public int DspBus;
    }

    private readonly QueuedDma[] _dmaQueue = new QueuedDma[16];

    private static uint GetWriteAddValue(uint regVal)
   
  {
        switch (regVal & 0x7)
       
      {
        case 0:
            return 0;
        case 1:
            return 2;
        case 2:
            return 4;
        case 3:
            return 8;
        case 4:
            return 16;
        case 5:
            return 32;
        case 6:
            return 64;
        case 7:
            return 128;
        }

        return 0;
    }

    private bool ScuActiveDmaExists()
   
  {
        int i = 0;
        for (i = 0; i < 16; i++)
       
      {
            if (_dmaQueue[i].Status == DmaStatusActive)
                return true;
        }

        return false;
    }

    private void DmaFinish(ref QueuedDma dma)
   
  {
        dma = default;

        dma.Status = DmaStatusFinished;

        ScuSortDma();
        if (!ScuActiveDmaExists() && _dmaQueue[0].Status == DmaStatusQueued)
            _dmaQueue[0].Status = DmaStatusActive;
    }

    private void DmaFinished(ref QueuedDma dma)
   
  {
        if (dma.BusType == DmaTransferAToB)
            _registers.DSTA &= ~0x300000u;


        switch (dma.Level)
       
      {
        case 0:
            _registers.DSTA &= ~(1u << 4);
            SendLevel0DmaEnd();
            break;
        case 1:
            _registers.DSTA &= ~(1u << 8);
            SendLevel1DmaEnd();
            break;
        case 2:
            _registers.DSTA &= ~(1u << 12);
            SendLevel2DmaEnd();
            break;
        }

        DmaFinish(ref dma);
    }

    private void DmaReadIndirect(ref QueuedDma dma)
    {
        uint source = _masterMemory.MappedMemoryReadLongNocache(dma.IndirectAddress + 8);
        dma.OriginalCount = dma.Count = _masterMemory.MappedMemoryReadLongNocache(dma.IndirectAddress);
        dma.WriteAddress = _masterMemory.MappedMemoryReadLongNocache(dma.IndirectAddress + 4);
        dma.ReadAddress = source & 0x7FFFFFFF;
        dma.SecondWord = 0;
        if ((source & 0x80000000) != 0)
            dma.IsLastIndirect = 1;
    }

    private void CheckDmaFinished(ref QueuedDma dma)
   
  {
        if (dma.Count == 0)
       
      {
            if (dma.IsIndirect != 0)
           
          {
                if (dma.IsLastIndirect != 0)
               
              {
                    dma.Status = DmaStatusFinished;
                    DmaFinished(ref dma);
                }
                else
                {
                    DmaReadIndirect(ref dma);
                    dma.BusType = GetBusType(dma.ReadAddress, dma.WriteAddress);
                    dma.IndirectAddress += 12;
                }
            }
            else
           
          {
                DmaFinished(ref dma);
            }
        }
    }

    private static void GetWriteAddB(ref QueuedDma dma)
   
  {
        switch (dma.AddSetting)
       
      {
        case 0:
            break;
        case 1:
                dma.WriteAddress += 2;
            break;
        case 2:
                dma.WriteAddress += 4;
            break;
        case 3:
                dma.WriteAddress += 8;
            break;
        case 4:
                dma.WriteAddress += 16;
            break;
        case 5:
                dma.WriteAddress += 32;
            break;
        case 6:
                dma.WriteAddress += 64;
            break;
        case 7:
                dma.WriteAddress += 128;
            break;
        }
    }

    private static void GetWriteAddAToCpu(ref QueuedDma dma)
   
  {
        switch (dma.AddSetting)
       
      {
        case 0:
            break;
        case 1:
            if ((dma.NumWritten % 8) == 0)
                dma.WriteAddress += 4;
            break;
        case 2:
            dma.WriteAddress += 4;
            break;
        case 3:
            dma.WriteAddress += 8;
            break;
        case 4:
            dma.WriteAddress += 16;
            break;
        case 5:
            dma.WriteAddress += 32;
            break;
        case 6:
            dma.WriteAddress += 64;
            break;
        case 7:
            dma.WriteAddress += 128;
            break;
        }
    }

    private void DoWritesB(ref QueuedDma dma)
   
  {
        if (dma.SecondWord == 0)
       
      {
            dma.Buffer = _masterMemory.MappedMemoryReadLongNocache(dma.ReadAddress);
            _masterMemory.MappedMemoryWriteWordNocache(dma.WriteAddress, unchecked((ushort)(dma.Buffer >> 16)));
            dma.SecondWord = 1;
            dma.Count -= 2;
            dma.NumWritten += 2;
            GetWriteAddB(ref dma);
        }
        else
       
      {
            _masterMemory.MappedMemoryWriteWordNocache(dma.WriteAddress, unchecked((ushort)(dma.Buffer & 0xffff)));
            dma.ReadAddress += dma.ReadAdd;
            dma.Count -= 2;
            dma.NumWritten += 2;
            dma.SecondWord = 0;
            GetWriteAddB(ref dma);
        }
    }

    private void ScuDmaTickToB(ref QueuedDma dma)
   
  {
        if (_masterMemory.CheckWait(dma.ReadAddress, 2))
            return;

        if (dma.CountMod4 == 0)
       
      {
            DoWritesB(ref dma);
        }
        else if (dma.CountMod4 == 1)
       
      {

            if ((dma.OriginalCount >= 0x5 )
                && (dma.AddSetting >= 2 ) && dma.Count == 1)
           
          {
                _masterMemory.MappedMemoryWriteByteNocache(dma.WriteAddress, unchecked((byte)((dma.Buffer >> 8) & 0xff)));
                _masterMemory.MappedMemoryWriteByteNocache(dma.WriteAddress + 2, 0x90);
                dma.Count = 0;
            }
            else if (dma.Count == 1 && dma.OriginalCount > 1 && dma.AddSetting == 0)
           
          {
                _ = _masterMemory.MappedMemoryReadByteNocache(dma.ReadAddress);
                _masterMemory.MappedMemoryWriteByteNocache(dma.WriteAddress + 2, 0x90);
                dma.Count = 0;
            }
            else if (dma.Count == 1)
           
          {
                byte value = _masterMemory.MappedMemoryReadByteNocache(dma.ReadAddress);
                _masterMemory.MappedMemoryWriteByteNocache(dma.WriteAddress, value);
                dma.Count = 0;
            }
            else
           
          {
                DoWritesB(ref dma);
            }
        }
        else if (dma.CountMod4 == 2)
       
      {
            if (dma.Count == 2)
           
          {
                ushort word = _masterMemory.MappedMemoryReadWordNocache(dma.ReadAddress);
                _masterMemory.MappedMemoryWriteWordNocache(dma.WriteAddress, word);
                dma.Count = 0;
            }
            else
                DoWritesB(ref dma);
        }
        else if (dma.CountMod4 == 3)
       
      {
            if (dma.Count == 3 && dma.OriginalCount > 3)
           
          {
                dma.Buffer = _masterMemory.MappedMemoryReadLongNocache(dma.ReadAddress);
                _masterMemory.MappedMemoryWriteWordNocache(dma.WriteAddress, unchecked((ushort)(dma.Buffer >> 16)));

                GetWriteAddB(ref dma);

                _masterMemory.MappedMemoryWriteByteNocache(dma.WriteAddress, unchecked((byte)((dma.Buffer >> 8) & 0xFF)));
                dma.Count = 0;
            }
            else if (dma.Count == 3)
           
          {
                dma.Buffer = _masterMemory.MappedMemoryReadLongNocache(dma.ReadAddress);
                _masterMemory.MappedMemoryWriteWordNocache(dma.WriteAddress, unchecked((ushort)(dma.Buffer >> 16)));

                GetWriteAddB(ref dma);

                _masterMemory.MappedMemoryWriteByteNocache(dma.WriteAddress, unchecked((byte)((dma.Buffer >> 8) & 0xFF)));
                dma.Count = 0;
            }
            else
           
          {
                DoWritesB(ref dma);
            }
        }

        CheckDmaFinished(ref dma);
    }

    private void DoWritesAToCpu(ref QueuedDma dma)
   
  {
        if (dma.SecondWord == 0)
       
      {
            dma.Buffer = (uint)_masterMemory.MappedMemoryReadWordNocache(dma.ReadAddress) << 16;
            dma.SecondWord = 1;
            dma.Count -= 2;
            dma.NumWritten += 2;
        }
        else
       
      {
            dma.Buffer |= _masterMemory.MappedMemoryReadWordNocache(dma.ReadAddress + 2);
            _masterMemory.MappedMemoryWriteLongNocache(dma.WriteAddress, dma.Buffer);
            dma.ReadAddress += dma.ReadAdd;
            dma.Count -= 2;
            dma.NumWritten += 2;
            dma.SecondWord = 0;
            GetWriteAddAToCpu(ref dma);
        }
    }

    private void ScuDmaTickAToCpu(ref QueuedDma dma)
   
  {
        if (_masterMemory.CheckWait(dma.ReadAddress, 2))
            return;

        if (dma.CountMod4 == 0)
       
      {
            DoWritesAToCpu(ref dma);
        }
        else if (dma.CountMod4 == 1)
       
      {
            if (dma.Count == 1)
           
          {
                byte value = _masterMemory.MappedMemoryReadByteNocache(dma.ReadAddress);
                _masterMemory.MappedMemoryWriteByteNocache(dma.WriteAddress, value);
                dma.Count = 0;
            }
            else
           
          {
                DoWritesAToCpu(ref dma);
            }
        }
        else if (dma.CountMod4 == 2)
       
      {
            if (dma.Count == 2)
           
          {
                ushort word = _masterMemory.MappedMemoryReadWordNocache(dma.ReadAddress);
                _masterMemory.MappedMemoryWriteWordNocache(dma.WriteAddress, word);
                dma.Count = 0;
            }
            else  DoWritesAToCpu(ref dma);
        }
        else if (dma.CountMod4 == 3)
       
      {
            if (dma.Count == 3)
           
          {
                dma.Buffer = (uint)_masterMemory.MappedMemoryReadWordNocache(dma.ReadAddress) << 16;
                _masterMemory.MappedMemoryWriteWordNocache(dma.WriteAddress, unchecked((ushort)(dma.Buffer >> 16)));
                dma.Buffer |= _masterMemory.MappedMemoryReadWordNocache(dma.ReadAddress + 2);

                _masterMemory.MappedMemoryWriteByteNocache(dma.WriteAddress + 2, unchecked((byte)((dma.Buffer >> 8) & 0xFF)));
                dma.Count = 0;
            }
            else
           
          {
                DoWritesAToCpu(ref dma);
            }
        }

        CheckDmaFinished(ref dma);
    }

    private void SwapDmaQueue(int i, int j)
   
  {
        QueuedDma temp = _dmaQueue[i];
        _dmaQueue[i] = _dmaQueue[j];
        _dmaQueue[j] = temp;
    }

    private void ScuSortDma()
   
  {
        int i = 0;

        for (i = 0; i < 16; i++)
       
      {
            int j = 0;
            for (j = 0; j < 16; j++)
           
          {
                if (_dmaQueue[i].Status > _dmaQueue[j].Status)
               
              {
                    SwapDmaQueue(i, j);
                }
            }
        }
    }

    private void ScuDmaTick32(ref QueuedDma dma)
   
  {
        uint srcValue = 0;

        if (_masterMemory.CheckWait(dma.ReadAddress, 2))
            return;

        srcValue = _masterMemory.MappedMemoryReadLongNocache(dma.ReadAddress);
        _masterMemory.MappedMemoryWriteLongNocache(dma.WriteAddress, srcValue);

        dma.ReadAddress += dma.ReadAdd;
        dma.WriteAddress += dma.WriteAdd;
        dma.Count = unchecked(dma.Count - 4);

        CheckDmaFinished(ref dma);
    }

    private static bool IsABus(uint addr)
   
  {
        addr &= 0xFFFFFFF;

        if (addr >= 0x2000000 && addr <= 0x58FFFFF)
            return true;

        return false;
    }

    private static bool IsBBus(uint addr)
   
  {
        addr &= 0xFFFFFFF;

        if (addr >= 0x5A00000 && addr <= 0x5F8011F)
            return true;

        return false;
    }

    private static bool IsCpuBus(uint addr)
   
  {
        addr &= 0xFFFFFFF;

        if (IsABus(addr))
            return false;
        if (IsBBus(addr))
            return false;

        return true;
    }

    private static int GetBusType(uint src, uint dst)
    {
        bool srcTypeA = IsABus(src);
        bool srcTypeB = IsBBus(src);
        bool srcTypeCpu = (!srcTypeA) && (!srcTypeB);

        bool dstTypeA = IsABus(dst);
        bool dstTypeB = IsBBus(dst);
        bool dstTypeCpu = (!dstTypeA) && (!dstTypeB);

        if (srcTypeA && dstTypeB)
            return DmaTransferAToB;

        if (srcTypeCpu && dstTypeB)
            return DmaTransferCpuToB;

        if (srcTypeA && dstTypeCpu)
            return DmaTransferAToCpu;

        if (srcTypeB && dstTypeCpu)
            return DmaTransferBToCpu;

        if (srcTypeCpu && dstTypeA)
            return DmaTransferCpuToA;

        if (srcTypeB && dstTypeA)
            return DmaTransferBToA;

        return 0;
    }

    private void ScuDmaSortActivate(ref QueuedDma dma)
    {
        ScuSortDma();

        ref QueuedDma next = ref _dmaQueue[0];
        if (ScuActiveDmaExists() || next.Status != DmaStatusQueued)
            return;

        next.Status = DmaStatusActive;
        if (next.Level <= 2)
            _registers.DSTA |= 0x10u << (next.Level * 4);
        if (dma.BusType == DmaTransferAToB)
            _registers.DSTA |= 0x300000;
    }

    private void ScuEnqueueDma(ref QueuedDma dma, uint readReg, uint writeReg, uint countReg, uint addReg, uint modeReg, int level)
   
  {
        dma = default;

        dma.ReadAddress = readReg;
        dma.WriteAddress = writeReg;
        dma.IndirectAddress = writeReg;
        dma.AddSetting = (int)(addReg & 0x7);
        dma.WriteAdd = GetWriteAddValue(addReg & 0x7);
        dma.IsIndirect = (int)((modeReg >> 24) & 1);
        dma.ReadAddrUpdate = (int)((modeReg >> 16) & 1);
        dma.WriteAddrUpdate = (int)((modeReg >> 8) & 1);
        dma.StartingFactor = (int)(modeReg & 7);
        dma.OriginalCount = dma.Count = countReg;
        dma.Level = level;

        if (dma.IsIndirect != 0)
            DmaReadIndirect(ref dma);

        dma.CountMod4 = (int)(dma.Count % 4);
        dma.Status = dma.StartingFactor == 7 ? DmaStatusQueued : DmaStatusWaitingFactor;
        if (((addReg >> 8) & 1) != 0)
            dma.ReadAdd = 4;

        dma.BusType = GetBusType(dma.ReadAddress, dma.WriteAddress);

        if (dma.IsIndirect == 0)
       
      {
            if (dma.Level > 0)
            {
                dma.Count &= 0xFFF;

                if (dma.Count == 0)
                    dma.Count = 0x1000;
            }
            else
            {
                if (dma.Count == 0)
                    dma.Count = 0x100000;
            }
        }

        ScuDmaSortActivate(ref dma);
    }

    private void ScuInsertDma(uint readReg, uint writeReg, uint countReg, uint addReg, uint modeReg, int level)
    {
        int i = 0;
        for (i = 0; i < 16; i++)
       
      {
            if (_dmaQueue[i].Status == DmaStatusFinished)
           
          {
                ScuEnqueueDma(ref _dmaQueue[i], readReg, writeReg, countReg, addReg, modeReg, level);
                break;
            }
        }

        ScuSortDma();
    }

    private void AdjustRa0Wa0(ref QueuedDma dma)
   
  {
        DspRegisters sc = _dsp;

        bool isABus = ((dma.DspAddress & 0x0F000000) >= 0x02000000 &&
            (dma.DspAddress & 0x0FF00000) <= 0x05800000);

        bool isCBus = (dma.DspAddress & 0x0F000000) == 0x06000000;

        if (dma.DspDmaType == 1 || dma.DspDmaType == 3)
       
      {
            switch (dma.DspAddSetting)
           
          {
            case 0:
            case 1:
            case 4:
            case 5:
                sc.RA0 += 1;
                break;
            case 2:
            case 3:
            case 6:
            case 7:
                sc.RA0 += dma.DspOrigCount;
                break;
            default:
                break;
            }
        }
        else if (dma.DspDmaType == 2 || dma.DspDmaType == 4)
       
      {
            if (isCBus || isABus)
           
          {
                switch (dma.DspAddSetting)
               
              {
                case 0:
                    sc.WA0 += 1;
                    break;
                case 1:
                    if (dma.DspOrigCount == 1 || dma.DspOrigCount == 2)
                        sc.WA0 += 1;
                    else
                        sc.WA0 += (dma.DspOrigCount >> 1) + 1;
                    break;
                case 2:
                    sc.WA0 += dma.DspOrigCount;
                    break;
                case 3:
                    sc.WA0 += (dma.DspOrigCount * 2) - 1;
                    break;
                case 4:
                    sc.WA0 += (dma.DspOrigCount * 4) - 3;
                    break;
                case 5:
                    sc.WA0 += (dma.DspOrigCount * 8) - 7;
                    break;
                case 6:
                    sc.WA0 += (dma.DspOrigCount * 16) - 15;
                    break;
                case 7:
                    sc.WA0 += (dma.DspOrigCount * 32) - 31;
                    break;
                default:
                    break;
                }
            }
            else
           
          {

                switch (dma.DspAddSetting)
               
              {
                case 0:
                    sc.WA0 += 1;
                    break;
                case 1:
                    sc.WA0 += dma.DspOrigCount;
                    break;
                case 2:
                    sc.WA0 += (dma.DspOrigCount * 2) - 1;
                    break;
                case 3:
                    sc.WA0 += (dma.DspOrigCount * 4) - 3;
                    break;
                case 4:
                    sc.WA0 += (dma.DspOrigCount * 8) - 7;
                    break;
                case 5:
                    sc.WA0 += (dma.DspOrigCount * 16) - 15;
                    break;
                case 6:
                    sc.WA0 += (dma.DspOrigCount * 32) - 31;
                    break;
                case 7:
                    sc.WA0 += (dma.DspOrigCount * 64) - 63;
                    break;
                default:
                    break;
                }
            }
        }
    }

    private void ScuDmaTickDsp(ref QueuedDma dma)
   
  {
        DspRegisters sc = _dsp;

        if (dma.DspDmaType == 1)
       
      {
            if (_masterMemory.CheckWait(dma.DspAddress, 2))
                return;

            sc.MD[dma.DspBank, dma.Ct] = _masterMemory.MappedMemoryReadLongNocache(dma.DspAddress);
            dma.Ct++;
            dma.Ct &= 0x3F;

            if (dma.DspBus == DspCpuBus || dma.DspBus == DspABus)
           
          {
                switch (dma.DspAddSetting)
               
              {
                case 0:
                case 1:
                case 4:
                case 5:
                    break;
                case 2:
                case 3:
                case 6:
                case 7:
                    dma.DspAddress += 4;
                    break;
                default:
                    break;
                }
            }
            else
                dma.DspAddress += 4;

            dma.Count--;
        }
        else if (dma.DspDmaType == 2 || dma.DspDmaType == 4)
       
      {
            if (dma.DspBus == DspABus || dma.DspBus == DspCpuBus)
           
          {
                uint value = sc.MD[dma.DspBank, dma.Ct];
                _masterMemory.MappedMemoryWriteLongNocache(dma.DspAddress, value);

                switch (dma.DspAddSetting)
               
              {
                case 0:
                    break;
                case 1:
                    if ((dma.NumWritten & 1) != 0)
                        dma.DspAddress += 4;

                    dma.NumWritten++;
                    break;
                case 2:
                    dma.DspAddress += 4;
                    break;
                case 3:
                    dma.DspAddress += 8;
                    break;
                case 4:
                    dma.DspAddress += 16;
                    break;
                case 5:
                    dma.DspAddress += 32;
                    break;
                case 6:
                    dma.DspAddress += 64;
                    break;
                case 7:
                    dma.DspAddress += 128;
                    break;
                default:
                    break;
                }
            }
            else
           
          {
                uint value = sc.MD[dma.DspBank, dma.Ct];
                _masterMemory.MappedMemoryWriteWordNocache(dma.DspAddress, (ushort)(value >> 16));
                dma.DspAddress += (uint)(dma.DspAdd << 1);
                _masterMemory.MappedMemoryWriteWordNocache(dma.DspAddress, unchecked((ushort)value));
                dma.DspAddress += (uint)(dma.DspAdd << 1);
            }

            dma.Ct++;
            dma.Ct &= 0x3F;
            dma.Count--;
        }
        else if (dma.DspDmaType == 3)
       
      {
            if (dma.DspBank > 3)
            {
                sc.ProgramRam[dma.ProgramRamCounter] = _masterMemory.MappedMemoryReadLongNocache(dma.DspAddress);
                dma.ProgramRamCounter = unchecked((byte)(dma.ProgramRamCounter + 1));
                dma.DspAddress += (uint)dma.DspAdd << 1;
                dma.Count--;
            }
            else
           
          {
                if (_masterMemory.CheckWait(dma.DspAddress, 2))
                    return;

                sc.MD[dma.DspBank, dma.Ct] = _masterMemory.MappedMemoryReadLongNocache(dma.DspAddress);
                dma.Ct++;
                dma.Ct &= 0x3F;

                if (dma.DspBus == DspCpuBus || dma.DspBus == DspABus)
               
              {
                    switch (dma.DspAddSetting)
                   
                  {
                    case 0:
                    case 1:
                    case 4:
                    case 5:
                        break;
                    case 2:
                    case 3:
                    case 6:
                    case 7:
                        dma.DspAddress += 4;
                        break;
                    default:
                        break;
                    }
                }
                else
                    dma.DspAddress += (uint)(dma.DspAdd);

                dma.Count--;
            }
        }

        CheckDspDmaFinished(ref dma);
    }

    private void CheckDspDmaFinished(ref QueuedDma dma)
    {
        if (dma.Count != 0)
            return;

        _registers.DSTA &= ~1u;
        _registers.DSTA &= ~0x700000u;
        _dsp.ProgControlPort.T0 = 0;
        DmaFinish(ref dma);
    }

    private static void GetWriteAddFromB(ref QueuedDma dma)
   
  {
        switch (dma.AddSetting)
       
      {
        case 0:
            break;
        case 1:
            if ((dma.NumWritten % 8) == 0)
                dma.WriteAddress += 4;
            break;
        case 2:
            if ((dma.NumWritten % 4) == 0)
                dma.WriteAddress += 4;
            break;
        case 3:
            if ((dma.NumWritten % 4) == 0)
                dma.WriteAddress += 8;
            break;
        case 4:
            if ((dma.NumWritten % 4) == 0)
                dma.WriteAddress += 16;
            break;
        case 5:
            if ((dma.NumWritten % 4) == 0)
                dma.WriteAddress += 32;
            break;
        case 6:
            if ((dma.NumWritten % 4) == 0)
                dma.WriteAddress += 64;
            break;
        case 7:
            if ((dma.NumWritten % 4) == 0)
                dma.WriteAddress += 128;
            break;
        }
    }

    private void DoWriteFromB(ref QueuedDma dma)
   
  {
        if (dma.SecondWord == 0)
       
      {
            dma.Buffer = (uint)_masterMemory.MappedMemoryReadWordNocache(dma.ReadAddress) << 16;
            dma.SecondWord = 1;
            dma.Count -= 2;
            dma.NumWritten += 2;
        }
        else
       
      {
            dma.Buffer |= _masterMemory.MappedMemoryReadWordNocache(dma.ReadAddress + 2);
            _masterMemory.MappedMemoryWriteLongNocache(dma.WriteAddress, dma.Buffer);
            dma.ReadAddress += 4;
            dma.Count -= 2;
            dma.NumWritten += 2;
            dma.SecondWord = 0;

            GetWriteAddAToCpu(ref dma);
        }
    }

    private void ScuDmaTickFromB(ref QueuedDma dma)
   
  {
        if (_masterMemory.CheckWait(dma.ReadAddress, 1))
            return;

        if (dma.CountMod4 == 0)
       
      {
            DoWriteFromB(ref dma);
        }
        else if (dma.CountMod4 == 1)
       
      {
            if (dma.Count == 1)
           
          {
                byte value = _masterMemory.MappedMemoryReadByteNocache(dma.ReadAddress);
                _masterMemory.MappedMemoryWriteByteNocache(dma.WriteAddress, value);
                dma.Count = 0;
            }
            else
           
          {
                DoWriteFromB(ref dma);
            }
        }
        else if (dma.CountMod4 == 2)
       
      {
            if (dma.Count == 2)
           
          {
                ushort word = _masterMemory.MappedMemoryReadWordNocache(dma.ReadAddress);
                _masterMemory.MappedMemoryWriteWordNocache(dma.WriteAddress, word);
                dma.Count = 0;
            }
            else
                DoWriteFromB(ref dma);
        }
        else if (dma.CountMod4 == 3)
       
      {
            if (dma.Count == 3)
           
          {
                byte value;
                ushort word = _masterMemory.MappedMemoryReadWordNocache(dma.ReadAddress);
                _masterMemory.MappedMemoryWriteWordNocache(dma.WriteAddress, word);

                value = _masterMemory.MappedMemoryReadByteNocache(dma.ReadAddress + 2);
                _masterMemory.MappedMemoryWriteByteNocache(dma.WriteAddress + 2, value);
                dma.Count = 0;
            }
            else
           
          {
                DoWriteFromB(ref dma);
            }
        }

        CheckDmaFinished(ref dma);
    }

    private void ScuDmaTick(ref QueuedDma dma)
   
  {
        if (dma.IsDsp != 0)
            ScuDmaTickDsp(ref dma);

        else if (dma.BusType == DmaTransferCpuToB || dma.BusType == DmaTransferAToB)
            ScuDmaTickToB(ref dma);

        else if (dma.BusType == DmaTransferAToCpu || dma.BusType == DmaTransferCpuToA)
            ScuDmaTickAToCpu(ref dma);

        else if (dma.BusType == DmaTransferBToCpu || dma.BusType == DmaTransferBToA)
            ScuDmaTickFromB(ref dma);
        else

            ScuDmaTick32(ref dma);
    }

    private void ScuDmaTickAll(uint cycles)
    {
        for (uint i = 0; i < cycles; i++)
        {
            if (_dmaQueue[0].Status == DmaStatusActive)
                ScuDmaTick(ref _dmaQueue[0]);
        }
    }

    private void ScuDma(DmaInfo dma)
   
  {
        byte readAdd, writeAdd;

        if ((dma.AddValue & 0x100) != 0)
            readAdd = 4;
        else
            readAdd = 0;

        switch (dma.AddValue & 0x7)
        {
            case 0x0:
                writeAdd = 0;
                break;
            case 0x1:
                writeAdd = 2;
                break;
            case 0x2:
                writeAdd = 4;
                break;
            case 0x3:
                writeAdd = 8;
                break;
            case 0x4:
                writeAdd = 16;
                break;
            case 0x5:
                writeAdd = 32;
                break;
            case 0x6:
                writeAdd = 64;
                break;
            case 0x7:
                writeAdd = 128;
                break;
            default:
                writeAdd = 0;
                break;
        }

        if ((dma.ModeAddressUpdate & 0x1000000) != 0)
        {
            while (true)
            {
                uint transferSize = _masterMemory.MappedMemoryReadLongNocache(dma.WriteAddress);
                uint writeAddress = _masterMemory.MappedMemoryReadLongNocache(dma.WriteAddress + 4);
                uint readAddress = _masterMemory.MappedMemoryReadLongNocache(dma.WriteAddress + 8);

                DoDma(readAddress & 0x7FFFFFFF, readAdd, writeAddress, writeAdd, transferSize);

                if ((readAddress & 0x80000000) != 0)
                    break;

                dma.WriteAddress += 12;
            }

            switch (dma.Mode)
            {
                case 0:
                    SendLevel0DmaEnd();
                    break;
                case 1:
                    SendLevel1DmaEnd();
                    break;
                case 2:
                    SendLevel2DmaEnd();
                    break;
            }
        }
        else
        {

            if (dma.Mode > 0)
            {
                dma.TransferNumber &= 0xFFF;

                if (dma.TransferNumber == 0)
                    dma.TransferNumber = 0x1000;
            }
            else
            {
                if (dma.TransferNumber == 0)
                    dma.TransferNumber = 0x100000;
            }

            DoDma(dma.ReadAddress, readAdd, dma.WriteAddress, writeAdd, dma.TransferNumber);

            switch (dma.Mode)
            {
                case 0:
                    SendLevel0DmaEnd();
                    break;
                case 1:
                    SendLevel1DmaEnd();
                    break;
                case 2:
                    SendLevel2DmaEnd();
                    break;
            }
        }
    }

    private uint ReadDspGeneralSource(uint source)
    {
        if (source <= 7)
        {
            int bank = (int)(source & 3);
            _incrementFlags[bank] |= (int)((source >> 2) & 1);
            return _dsp.MD[bank, _dsp.CT[bank] & 0x3F];
        }

        if (source == 9)
            return unchecked((uint)_dsp.ALU.L);
        if (source == 0xA)
            return (uint)((_dsp.ALU.All & 0x0000FFFFFFFF0000L) >> 16);
        return 0;
    }

    private void WriteDspD1Destination(uint destination, uint value)
    {
        switch (destination)
        {
            case 0:
                _dsp.MD[0, _dsp.CT[0] & 0x3F] = value;
                _dsp.CT[0]++;
                _dsp.CT[0] &= 0x3F;
                return;
            case 1:
                _dsp.MD[1, _dsp.CT[1] & 0x3F] = value;
                _dsp.CT[1]++;
                _dsp.CT[1] &= 0x3F;
                return;
            case 2:
                _dsp.MD[2, _dsp.CT[2] & 0x3F] = value;
                _dsp.CT[2]++;
                _dsp.CT[2] &= 0x3F;
                return;
            case 3:
                _dsp.MD[3, _dsp.CT[3] & 0x3F] = value;
                _dsp.CT[3]++;
                _dsp.CT[3] &= 0x3F;
                return;
            case 4:
                _dsp.RX = unchecked((int)value);
                return;
            case 5:
                _dsp.P.All = unchecked((int)value);
                return;
            case 6:
                _dsp.RA0 = value;
                return;
            case 7:
                _dsp.WA0 = value;
                return;
            case 0xA:
                _dsp.LOP = unchecked((ushort)value);
                return;
            case 0xB:
                _dsp.TOP = unchecked((byte)value);
                return;
            case 0xC:
                _dsp.CT[0] = unchecked((byte)value);
                return;
            case 0xD:
                _dsp.CT[1] = unchecked((byte)value);
                return;
            case 0xE:
                _dsp.CT[2] = unchecked((byte)value);
                return;
            case 0xF:
                _dsp.CT[3] = unchecked((byte)value);
                return;
            default:
                break;
        }
    }

    private void WriteLoadImmediateDest(uint num, uint val)
    {
        switch (num)
        {
            case 0x0: // MC0
                _dsp.MD[0, _dsp.CT[0] & 0x3F] = val;
                _dsp.CT[0]++;
                _dsp.CT[0] &= 0x3f;
                return;
            case 0x1: // MC1
                _dsp.MD[1, _dsp.CT[1] & 0x3F] = val;
                _dsp.CT[1]++;
                _dsp.CT[1] &= 0x3f;
                return;
            case 0x2: // MC2
                _dsp.MD[2, _dsp.CT[2] & 0x3F] = val;
                _dsp.CT[2]++;
                _dsp.CT[2] &= 0x3f;
                return;
            case 0x3: // MC3
                _dsp.MD[3, _dsp.CT[3] & 0x3F] = val;
                _dsp.CT[3]++;
                _dsp.CT[3] &= 0x3f;
                return;
            case 0x4: // RX
                _dsp.RX = unchecked((int)val);
                return;
            case 0x5: // PL
                _dsp.P.All = val;
                return;
            case 0x6: // RA0
                _dsp.RA0 = val & 0x1FFFFFF;
                return;
            case 0x7: // WA0
                _dsp.WA0 = val & 0x1FFFFFF;
                return;
            case 0xA: // LOP
                _dsp.LOP = unchecked((ushort)val);
                return;
            case 0xC: // PC->TOP, PC
                _dsp.TOP = unchecked((byte)(_dsp.PC + 1));
                _dsp.JumpAddress = val;
                _dsp.JumpDelayed = false;
                return;
            default: break;
        }
    }

    private static int DspGetBus(uint address)
    {
        if (IsABus(address))
            return DspABus;

        if (IsBBus(address))
            return DspBBus;

        return DspCpuBus;
    }

    private void ScuInsertDspDma(ref QueuedDma dma)
    {
        if (dma.Count == 0)
            dma.Count = dma.DspOrigCount = 256;

        dma.DspBus = DspGetBus(dma.DspAddress);
        _registers.DSTA |= 1;
        if (dma.DspBank < 4)
        {
            dma.Ct = (byte)(_dsp.CT[dma.DspBank] & 0x3F);
            _dsp.CT[dma.DspBank] = (byte)((dma.Ct + dma.Count) & 0x3F);
        }

        for (int i = 0; i < _dmaQueue.Length; i++)
        {
            if (_dmaQueue[i].Status == DmaStatusFinished)
            {
                _dmaQueue[i] = dma;
                break;
            }
        }

        ScuDmaSortActivate(ref dma);
    }

    private void SetDsta(uint address)
    {
        if (IsABus(address))
            _registers.DSTA |= 0x500000;
        else if (IsBBus(address))
            _registers.DSTA |= 0x600000;
    }

    private void DspDma01(uint inst)
    {
        uint imm = inst & 0xFF;
        uint sel = (inst >> 8) & 3;

        uint add = 0;

        uint i;
        uint aBusCheck;

        switch (((inst >> 15) & 0x07))
        {
            case 0: add = 0; break;
            case 1: add = 1; break;
            case 2: add = 2; break;
            case 3: add = 4; break;
            case 4: add = 8; break;
            case 5: add = 16; break;
            case 6: add = 32; break;
            case 7: add = 64; break;
        }

        if (_useDmaTiming)
        {
            QueuedDma dma = default;

            dma.IsDsp = 1;
            dma.DspDmaType = 1;
            dma.DspAdd = (int)add;
            dma.DspAddSetting = (byte)((inst >> 15) & 0x07);
            dma.DspBank = (int)sel;
            dma.Count = dma.DspOrigCount = imm;
            dma.DspAddress = _dsp.RA0 << 2;
            dma.Status = DmaStatusQueued;
            dma.Level = 3;

            SetDsta(_dsp.RA0 << 2);
            if (((inst >> 11) & 0x0F) != 0x08)
                AdjustRa0Wa0(ref dma);

            ScuInsertDspDma(ref dma);
            _dsp.ProgControlPort.T0 = 1;
            return;
        }

        aBusCheck = ((_dsp.RA0 << 2) & 0x0FF00000);
        if (aBusCheck >= 0x02000000 && aBusCheck < 0x05900000)
        {
            if (add > 1)
            {
                add = 1;
            }

            for (i = 0; i < imm; i++)
            {
                _dsp.MD[sel, _dsp.CT[sel] & 0x3F] = _masterMemory.MappedMemoryReadLongNocache((_dsp.RA0 << 2));
                _dsp.CT[sel]++;
                _dsp.CT[sel] &= 0x3F;
                _dsp.RA0 += add;
            }
        }
        else
        {
            for (i = 0; i < imm ; i++)
            {
                _dsp.MD[sel, _dsp.CT[sel] & 0x3F] = _masterMemory.MappedMemoryReadLongNocache((_dsp.RA0 << 2));
                _dsp.CT[sel]++;
                _dsp.CT[sel] &= 0x3F;
                _dsp.RA0 += (add >> 1);
            }
        }

        _dsp.ProgControlPort.T0 = 0;
    }

    private void DspDma02(uint inst)
    {
        uint imm = inst & 0xFF;
        uint sel = ((inst >> 8) & 0x03);

        uint add = 0;
        uint i;

        switch (((inst >> 15) & 0x07))
        {
            case 0: add = 0; break;
            case 1: add = 1; break;
            case 2: add = 2; break;
            case 3: add = 4; break;
            case 4: add = 8; break;
            case 5: add = 16; break;
            case 6: add = 32; break;
            case 7: add = 64; break;
        }

        if (_useDmaTiming)
        {
            QueuedDma dma = default;

            dma.IsDsp = 1;
            dma.DspDmaType = 2;
            dma.DspAdd = (int)add;
            dma.DspBank = (int)sel;
            dma.DspAddSetting = (byte)((inst >> 15) & 0x07);
            dma.Count = dma.DspOrigCount = imm;
            dma.DspAddress = _dsp.WA0 << 2;
            dma.Status = DmaStatusQueued;
            dma.Level = 3;

            if (((inst >> 10) & 0x1F) != 0x14)
                AdjustRa0Wa0(ref dma);

            SetDsta(_dsp.WA0 << 2);
            ScuInsertDspDma(ref dma);
            _dsp.ProgControlPort.T0 = 1;
            return;
        }

        if (add != 1)
        {
            for (i = 0; i < imm; i++)
            {
                uint value = _dsp.MD[sel, _dsp.CT[sel] & 0x3F];
                uint address = (_dsp.WA0 << 2);

                _masterMemory.MappedMemoryWriteLongNocache(address, value);
                _dsp.CT[sel]++;
                _dsp.WA0 += add >> 1;
                _dsp.CT[sel] &= 0x3F;
            }
        }
        else
        {

            for (i = 0; i < imm; i++)
            {
                uint value = _dsp.MD[sel, _dsp.CT[sel] & 0x3F];
                uint address = (_dsp.WA0 << 2);

                _masterMemory.MappedMemoryWriteLongNocache(address, value);
                _dsp.CT[sel]++;
                _dsp.CT[sel] &= 0x3F;
                _dsp.WA0 += 1;
            }
        }
        _dsp.ProgControlPort.T0 = 0;
    }

    private static void DetermineABusAdd(ref QueuedDma dma, uint inst)
    {
        if (IsABus(dma.DspAddress))
        {
            if (((inst >> 15) & 1) == 0)
                dma.DspAdd = 0;
        }
    }

    private void DspDma03(uint inst)
    {
        uint count = 0;
        uint i;
        uint add = 0;
        uint sel;
        uint aBusCheck;

        switch ((inst & 0x7))
        {
            case 0x00: count = _dsp.MD[0, _dsp.CT[0] & 0x3F]; break;
            case 0x01: count = _dsp.MD[1, _dsp.CT[1] & 0x3F]; break;
            case 0x02: count = _dsp.MD[2, _dsp.CT[2] & 0x3F]; break;
            case 0x03: count = _dsp.MD[3, _dsp.CT[3] & 0x3F]; break;
            case 0x04: count = _dsp.MD[0, _dsp.CT[0] & 0x3F]; _dsp.CT[0]++; _dsp.CT[0] &= 0x3F; break;
            case 0x05: count = _dsp.MD[1, _dsp.CT[1] & 0x3F]; _dsp.CT[1]++; _dsp.CT[1] &= 0x3F; break;
            case 0x06: count = _dsp.MD[2, _dsp.CT[2] & 0x3F]; _dsp.CT[2]++; _dsp.CT[2] &= 0x3F; break;
            case 0x07: count = _dsp.MD[3, _dsp.CT[3] & 0x3F]; _dsp.CT[3]++; _dsp.CT[3] &= 0x3F; break;
        }

        switch (((inst >> 15) & 0x07))
        {
            case 0: add = 0; break;
            case 1: add = 1; break;
            case 2: add = 2; break;
            case 3: add = 4; break;
            case 4: add = 8; break;
            case 5: add = 16; break;
            case 6: add = 32; break;
            case 7: add = 64; break;
        }

        sel = (inst >> 8) & 3;

        aBusCheck = ((_dsp.RA0 << 2) & 0x0FF00000);
        if (aBusCheck >= 0x02000000 && aBusCheck < 0x05900000)
        {
            if (add > 1)
            {
                add = 1;
            }
            for (i = 0; i < count; i++)
            {
                _dsp.MD[sel, _dsp.CT[sel] & 0x3F] = _masterMemory.MappedMemoryReadLongNocache((_dsp.RA0 << 2));
                _dsp.CT[sel]++;
                _dsp.CT[sel] &= 0x3F;
                _dsp.RA0 += add;
            }
        }
        else
        {
            for (i = 0; i < count; i++)
            {
                _dsp.MD[sel, _dsp.CT[sel] & 0x3F] = _masterMemory.MappedMemoryReadLongNocache((_dsp.RA0 << 2));
                _dsp.CT[sel]++;
                _dsp.CT[sel] &= 0x3F;
                _dsp.RA0 += (add >> 1);
            }
        }

        _dsp.ProgControlPort.T0 = 0;
    }

    private void DspDma04(uint inst)
    {
        uint count = 0;
        uint add = 0;
        uint sel = ((inst >> 8) & 0x03);
        uint i;

        switch ((inst & 0x7))
        {
            case 0x00: count = _dsp.MD[0, _dsp.CT[0] & 0x3F]; break;
            case 0x01: count = _dsp.MD[1, _dsp.CT[1] & 0x3F]; break;
            case 0x02: count = _dsp.MD[2, _dsp.CT[2] & 0x3F]; break;
            case 0x03: count = _dsp.MD[3, _dsp.CT[3] & 0x3F]; break;
            case 0x04: count = _dsp.MD[0, _dsp.CT[0] & 0x3F]; _dsp.CT[0]++; _dsp.CT[0] &= 0x3F; break;
            case 0x05: count = _dsp.MD[1, _dsp.CT[1] & 0x3F]; _dsp.CT[1]++; _dsp.CT[1] &= 0x3F; break;
            case 0x06: count = _dsp.MD[2, _dsp.CT[2] & 0x3F]; _dsp.CT[2]++; _dsp.CT[2] &= 0x3F; break;
            case 0x07: count = _dsp.MD[3, _dsp.CT[3] & 0x3F]; _dsp.CT[3]++; _dsp.CT[3] &= 0x3F; break;
        }

        switch (((inst >> 15) & 0x07))
        {
            case 0: add = 0; break;
            case 1: add = 1; break;
            case 2: add = 2; break;
            case 3: add = 4; break;
            case 4: add = 8; break;
            case 5: add = 16; break;
            case 6: add = 32; break;
            case 7: add = 64; break;
        }

        if (_useDmaTiming)
        {
            QueuedDma dma = default;
            dma.IsDsp = 1;
            dma.DspDmaType = 4;
            dma.DspAdd = (int)add;
            dma.DspBank = (int)sel;
            dma.DspAddSetting = (byte)((inst >> 15) & 0x07);
            dma.Count = dma.DspOrigCount = count;
            dma.DspAddress = _dsp.WA0 << 2;
            dma.Status = DmaStatusQueued;
            dma.Level = 3;

            DetermineABusAdd(ref dma, inst);

            if (((inst >> 10) & 0x1F) != 0x1C)
                AdjustRa0Wa0(ref dma);

            SetDsta(_dsp.WA0 << 2);
            ScuInsertDspDma(ref dma);
            _dsp.ProgControlPort.T0 = 1;
            return;
        }

        for (i = 0; i < count; i++)
        {
            uint value = _dsp.MD[sel, _dsp.CT[sel] & 0x3F];
            uint address = (_dsp.WA0 << 2);
            _masterMemory.MappedMemoryWriteLongNocache(address, value);
            _dsp.CT[sel]++;
            _dsp.CT[sel] &= 0x3F;
            _dsp.WA0 += add;
        }
        _dsp.ProgControlPort.T0 = 0;
    }

    private void DspDma05(uint inst)
    {
        uint saveRa0 = _dsp.RA0;
        DspDma01(inst);
        _dsp.RA0 = saveRa0;
    }

    private void DspDma06(uint inst)
    {
        uint saveWa0 = _dsp.WA0;
        DspDma02(inst);
        _dsp.WA0 = saveWa0;
    }

    private void DspDma07(uint inst)
    {
        uint saveRa0 = _dsp.RA0;
        DspDma03(inst);
        _dsp.RA0 = saveRa0;
    }

    private void DspDma08(uint inst)
    {
        uint saveWa0 = _dsp.WA0;
        DspDma04(inst);
        _dsp.WA0 = saveWa0;
    }

    public void Execute(uint cycles)
    {
        unchecked
        {
            uint timing = cycles / 2;
            int dmaCycles = (int)cycles;

            if (_dsp.ProgControlPort.EX != 0)
            {
                while (timing > 0)
                {
                    for (int i = 0; i < _breakpoints.NumberOfCodeBreakpoints; i++)
                    {
                        if (_dsp.PC == _breakpoints.CodeBreakpoints[i].Address && _breakpoints.InBreakpoint == 0)
                        {
                            _breakpoints.InBreakpoint = 1;
                            _breakpoints.BreakpointCallback?.Invoke(_breakpoints.CodeBreakpoints[i].Address);
                            _breakpoints.InBreakpoint = 0;
                        }
                    }

                    uint instruction = _dsp.ProgramRam[_dsp.PC];
                    if (_dmaQueue[0].Status == DmaStatusActive)
                    {
                        ScuDmaTick(ref _dmaQueue[0]);
                        dmaCycles -= 2;
                    }

                    _incrementFlags[0] = 0;
                    _incrementFlags[1] = 0;
                    _incrementFlags[2] = 0;
                    _incrementFlags[3] = 0;

                    int accumulator = _dsp.AC.L;
                    uint accumulatorLow = (uint)accumulator;
                    uint operandLow = (uint)_dsp.P.L;
                    long upper = accumulator & 0xFFFF00000000L;

                    switch (instruction >> 26)
                    {
                        case 0x00: // NOP
                            _dsp.ALU.All = accumulator;
                            break;
                        case 0x01: // AND
                            _dsp.ALU.All = (long)(accumulator & _dsp.P.L) | upper;
                            _dsp.ProgControlPort.Z = _dsp.ALU.L == 0 ? 1u : 0u;
                            _dsp.ProgControlPort.S = _dsp.ALU.L < 0 ? 1u : 0u;
                            _dsp.ProgControlPort.C = 0;
                            break;
                        case 0x02: // OR
                            _dsp.ALU.All = (accumulatorLow | operandLow) | upper;
                            _dsp.ProgControlPort.Z = _dsp.ALU.L == 0 ? 1u : 0u;
                            _dsp.ProgControlPort.S = _dsp.ALU.L < 0 ? 1u : 0u;
                            _dsp.ProgControlPort.C = 0;
                            break;
                        case 0x03: // XOR
                            _dsp.ALU.All = (accumulatorLow ^ operandLow) | upper;
                            _dsp.ProgControlPort.Z = _dsp.ALU.L == 0 ? 1u : 0u;
                            _dsp.ProgControlPort.S = _dsp.ALU.L < 0 ? 1u : 0u;
                            _dsp.ProgControlPort.C = 0;
                            break;
                        case 0x04: // ADD
                            _dsp.ALU.All = (accumulatorLow + operandLow) | upper;
                            _dsp.ProgControlPort.Z = _dsp.ALU.L == 0 ? 1u : 0u;
                            _dsp.ProgControlPort.S = _dsp.ALU.L < 0 ? 1u : 0u;
                            _dsp.ProgControlPort.C = (uint)(((ulong)accumulatorLow + operandLow) >> 32) & 1;
                            break;
                        case 0x05: // SUB
                            _dsp.ALU.All = (accumulatorLow - operandLow) | upper;
                            _dsp.ProgControlPort.Z = _dsp.ALU.L == 0 ? 1u : 0u;
                            _dsp.ProgControlPort.S = _dsp.ALU.L < 0 ? 1u : 0u;
                            _dsp.ProgControlPort.C = (uint)(((ulong)accumulatorLow - operandLow) >> 32) & 1;
                            break;
                        case 0x06: // AD2
                            _dsp.ALU.All = _dsp.AC.All + _dsp.P.All;
                            _dsp.ProgControlPort.Z = _dsp.ALU.All == 0 ? 1u : 0u;
                            _dsp.ProgControlPort.S = (uint)((_dsp.ALU.All >> 47) & 1);
                            _dsp.ProgControlPort.C = (uint)(((_dsp.AC.All & 0xFFFFFFFFFFFFL) +
                                (_dsp.P.All & 0xFFFFFFFFFFFFL)) >> 48) & 1;
                            break;
                        case 0x08: // SR
                            _dsp.ProgControlPort.C = accumulatorLow & 1;
                            _dsp.ALU.All = ((accumulatorLow & 0x80000000u) | (uint)(accumulator >> 1)) | upper;
                            _dsp.ProgControlPort.Z = _dsp.ALU.All == 0 ? 1u : 0u;
                            _dsp.ProgControlPort.S = (uint)_dsp.ALU.L >> 31;
                            break;
                        case 0x09: // RR
                            _dsp.ProgControlPort.C = accumulatorLow & 1;
                            _dsp.ALU.All = ((_dsp.ProgControlPort.C << 31) | (accumulatorLow >> 1)) | upper;
                            _dsp.ProgControlPort.Z = _dsp.ALU.All == 0 ? 1u : 0u;
                            _dsp.ProgControlPort.S = (uint)_dsp.ALU.L >> 31;
                            break;
                        case 0x0A: // SL
                            _dsp.ProgControlPort.C = accumulatorLow >> 31;
                            _dsp.ALU.All = (accumulatorLow << 1) | upper;
                            _dsp.ProgControlPort.Z = _dsp.ALU.L == 0 ? 1u : 0u;
                            _dsp.ProgControlPort.S = _dsp.ALU.L < 0 ? 1u : 0u;
                            break;
                        case 0x0B: // RL
                            _dsp.ProgControlPort.C = accumulatorLow >> 31;
                            _dsp.ALU.All = ((accumulatorLow << 1) | _dsp.ProgControlPort.C) | upper;
                            _dsp.ProgControlPort.Z = _dsp.ALU.All == 0 ? 1u : 0u;
                            _dsp.ProgControlPort.S = (uint)_dsp.ALU.L >> 31;
                            break;
                        case 0x0F: // RL8
                            _dsp.ALU.All = ((accumulatorLow << 8) | (accumulatorLow >> 24)) | upper;
                            _dsp.ProgControlPort.Z = _dsp.ALU.All == 0 ? 1u : 0u;
                            _dsp.ProgControlPort.S = (uint)_dsp.ALU.L >> 31;
                            _dsp.ProgControlPort.C = (accumulatorLow >> 24) & 1;
                            break;
                    }

                    switch (instruction >> 30)
                    {
                        case 0x00:
                        {
                            if (((instruction >> 23) & 4) != 0)
                                _dsp.RX = unchecked((int)ReadDspGeneralSource((instruction >> 20) & 7));

                            switch ((instruction >> 23) & 3)
                            {
                                case 2:
                                    _dsp.P.All = _dsp.Multiply;
                                    break;
                                case 3:
                                    _dsp.P.All = unchecked((int)ReadDspGeneralSource((instruction >> 20) & 7));
                                    break;
                            }

                            if (((instruction >> 17) & 4) != 0)
                                _dsp.RY = unchecked((int)ReadDspGeneralSource((instruction >> 14) & 7));

                            switch ((instruction >> 17) & 3)
                            {
                                case 1:
                                    _dsp.AC.All = 0;
                                    break;
                                case 2:
                                    _dsp.AC.All = _dsp.ALU.All;
                                    break;
                                case 3:
                                    _dsp.AC.All = unchecked((int)ReadDspGeneralSource((instruction >> 14) & 7));
                                    break;
                            }

                            if (_incrementFlags[0] != 0)
                            {
                                _dsp.CT[0]++;
                                _dsp.CT[0] &= 0x3F;
                                _incrementFlags[0] = 0;
                            }
                            if (_incrementFlags[1] != 0)
                            {
                                _dsp.CT[1]++;
                                _dsp.CT[1] &= 0x3F;
                                _incrementFlags[1] = 0;
                            }
                            if (_incrementFlags[2] != 0)
                            {
                                _dsp.CT[2]++;
                                _dsp.CT[2] &= 0x3F;
                                _incrementFlags[2] = 0;
                            }
                            if (_incrementFlags[3] != 0)
                            {
                                _dsp.CT[3]++;
                                _dsp.CT[3] &= 0x3F;
                                _incrementFlags[3] = 0;
                            }
                            uint destination = (instruction >> 8) & 0xF;
                            switch ((instruction >> 12) & 3)
                            {
                                case 1:
                                    WriteDspD1Destination(destination, unchecked((uint)(sbyte)instruction));
                                    break;
                                case 3:
                                    WriteDspD1Destination(destination, ReadDspGeneralSource(instruction & 0xF));
                                    if (_incrementFlags[0] != 0)
                                    {
                                        _dsp.CT[0]++;
                                        _dsp.CT[0] &= 0x3F;
                                        _incrementFlags[0] = 0;
                                    }
                                    if (_incrementFlags[1] != 0)
                                    {
                                        _dsp.CT[1]++;
                                        _dsp.CT[1] &= 0x3F;
                                        _incrementFlags[1] = 0;
                                    }
                                    if (_incrementFlags[2] != 0)
                                    {
                                        _dsp.CT[2]++;
                                        _dsp.CT[2] &= 0x3F;
                                        _incrementFlags[2] = 0;
                                    }
                                    if (_incrementFlags[3] != 0)
                                    {
                                        _dsp.CT[3]++;
                                        _dsp.CT[3] &= 0x3F;
                                        _incrementFlags[3] = 0;
                                    }
                                    break;
                            }
                            break;
                        }
                        case 0x02: // Load Immediate Commands
                            if (((instruction >> 25) & 1) != 0)
                            {
                                switch ((instruction >> 19) & 0x3F)
                                {
                                    case 0x01: // MVI NZ
                                        if (_dsp.ProgControlPort.Z == 0)
                                            WriteLoadImmediateDest((instruction >> 26) & 0xF,
                                                (instruction & 0x7FFFF) | ((instruction & 0x40000) != 0 ? 0xFFF80000u : 0u));
                                        break;
                                    case 0x02: // MVI NS
                                        if (_dsp.ProgControlPort.S == 0)
                                            WriteLoadImmediateDest((instruction >> 26) & 0xF,
                                                (instruction & 0x7FFFF) | ((instruction & 0x40000) != 0 ? 0xFFF80000u : 0u));
                                        break;
                                    case 0x03: // MVI NZS
                                        if (_dsp.ProgControlPort.Z == 0 || _dsp.ProgControlPort.S == 0)
                                            WriteLoadImmediateDest((instruction >> 26) & 0xF,
                                                (instruction & 0x7FFFF) | ((instruction & 0x40000) != 0 ? 0xFFF80000u : 0u));
                                        break;
                                    case 0x04: // MVI NC
                                        if (_dsp.ProgControlPort.C == 0)
                                            WriteLoadImmediateDest((instruction >> 26) & 0xF,
                                                (instruction & 0x7FFFF) | ((instruction & 0x40000) != 0 ? 0xFFF80000u : 0u));
                                        break;
                                    case 0x08: // MVI NT0
                                        if (_dsp.ProgControlPort.T0 == 0)
                                            WriteLoadImmediateDest((instruction >> 26) & 0xF,
                                                (instruction & 0x7FFFF) | ((instruction & 0x40000) != 0 ? 0xFFF80000u : 0u));
                                        break;
                                    case 0x21: // MVI Z
                                        if (_dsp.ProgControlPort.Z != 0)
                                            WriteLoadImmediateDest((instruction >> 26) & 0xF,
                                                (instruction & 0x7FFFF) | ((instruction & 0x40000) != 0 ? 0xFFF80000u : 0u));
                                        break;
                                    case 0x22: // MVI S
                                        if (_dsp.ProgControlPort.S != 0)
                                            WriteLoadImmediateDest((instruction >> 26) & 0xF,
                                                (instruction & 0x7FFFF) | ((instruction & 0x40000) != 0 ? 0xFFF80000u : 0u));
                                        break;
                                    case 0x23: // MVI ZS
                                        if (_dsp.ProgControlPort.Z != 0 || _dsp.ProgControlPort.S != 0)
                                            WriteLoadImmediateDest((instruction >> 26) & 0xF,
                                                (instruction & 0x7FFFF) | ((instruction & 0x40000) != 0 ? 0xFFF80000u : 0u));
                                        break;
                                    case 0x24: // MVI C
                                        if (_dsp.ProgControlPort.C != 0)
                                            WriteLoadImmediateDest((instruction >> 26) & 0xF,
                                                (instruction & 0x7FFFF) | ((instruction & 0x40000) != 0 ? 0xFFF80000u : 0u));
                                        break;
                                    case 0x28: // MVI T0
                                        if (_dsp.ProgControlPort.T0 != 0)
                                            WriteLoadImmediateDest((instruction >> 26) & 0xF,
                                                (instruction & 0x7FFFF) | ((instruction & 0x40000) != 0 ? 0xFFF80000u : 0u));
                                        break;
                                }
                            }
                            else
                            {
                                uint value = instruction & 0x1FFFFFF;
                                if ((value & 0x1000000) != 0)
                                    value |= 0xFE000000;
                                WriteLoadImmediateDest((instruction >> 26) & 0xF, value);
                            }
                            break;
                        case 0x03:
                            switch ((instruction >> 28) & 0xF)
                            {
                                case 0x0C:
                                    if (((instruction >> 10) & 0x1F) == 0x00)
                                        DspDma01(instruction);
                                    else if (((instruction >> 10) & 0x1F) == 0x04)
                                        DspDma02(instruction);
                                    else if (((instruction >> 11) & 0x0F) == 0x04)
                                        DspDma03(instruction);
                                    else if (((instruction >> 10) & 0x1F) == 0x0C)
                                        DspDma04(instruction);
                                    else if (((instruction >> 11) & 0x0F) == 0x08)
                                        DspDma05(instruction);
                                    else if (((instruction >> 10) & 0x1F) == 0x14)
                                        DspDma06(instruction);
                                    else if (((instruction >> 11) & 0x0F) == 0x0C)
                                        DspDma07(instruction);
                                    else if (((instruction >> 10) & 0x1F) == 0x1C)
                                        DspDma08(instruction);
                                    break;
                                case 0x0D: // Jump Commands
                                    switch ((instruction >> 19) & 0x7F)
                                    {
                                        case 0x00: // JMP
                                            _dsp.JumpAddress = instruction & 0xFF;
                                            _dsp.JumpDelayed = false;
                                            break;
                                        case 0x41: // JMP NZ
                                            if (_dsp.ProgControlPort.Z == 0)
                                            {
                                                _dsp.JumpAddress = instruction & 0xFF;
                                                _dsp.JumpDelayed = false;
                                            }
                                            break;
                                        case 0x42: // JMP NS
                                            if (_dsp.ProgControlPort.S == 0)
                                            {
                                                _dsp.JumpAddress = instruction & 0xFF;
                                                _dsp.JumpDelayed = false;
                                            }
                                            break;
                                        case 0x43: // JMP NZS
                                            if (_dsp.ProgControlPort.Z == 0 || _dsp.ProgControlPort.S == 0)
                                            {
                                                _dsp.JumpAddress = instruction & 0xFF;
                                                _dsp.JumpDelayed = false;
                                            }
                                            break;
                                        case 0x44: // JMP NC
                                            if (_dsp.ProgControlPort.C == 0)
                                            {
                                                _dsp.JumpAddress = instruction & 0xFF;
                                                _dsp.JumpDelayed = false;
                                            }
                                            break;
                                        case 0x48: // JMP NT0
                                            if (_dsp.ProgControlPort.T0 == 0)
                                            {
                                                _dsp.JumpAddress = instruction & 0xFF;
                                                _dsp.JumpDelayed = false;
                                            }
                                            break;
                                        case 0x61: // JMP Z
                                            if (_dsp.ProgControlPort.Z != 0)
                                            {
                                                _dsp.JumpAddress = instruction & 0xFF;
                                                _dsp.JumpDelayed = false;
                                            }
                                            break;
                                        case 0x62: // JMP S
                                            if (_dsp.ProgControlPort.S != 0)
                                            {
                                                _dsp.JumpAddress = instruction & 0xFF;
                                                _dsp.JumpDelayed = false;
                                            }
                                            break;
                                        case 0x63: // JMP ZS
                                            if (_dsp.ProgControlPort.Z != 0 || _dsp.ProgControlPort.S != 0)
                                            {
                                                _dsp.JumpAddress = instruction & 0xFF;
                                                _dsp.JumpDelayed = false;
                                            }
                                            break;
                                        case 0x64: // JMP C
                                            if (_dsp.ProgControlPort.C != 0)
                                            {
                                                _dsp.JumpAddress = instruction & 0xFF;
                                                _dsp.JumpDelayed = false;
                                            }
                                            break;
                                        case 0x68: // JMP T0
                                            if (_dsp.ProgControlPort.T0 != 0)
                                            {
                                                _dsp.JumpAddress = instruction & 0xFF;
                                                _dsp.JumpDelayed = false;
                                            }
                                            break;
                                    }
                                    break;
                                case 0x0E: // Loop bottom Commands
                                    if ((instruction & 0x08000000) != 0)
                                    {
                                        // LPS
                                        if (_dsp.LOP != 0)
                                        {
                                            _dsp.JumpAddress = _dsp.PC;
                                            _dsp.JumpDelayed = false;
                                            _dsp.LOP--;
                                        }
                                    }
                                    else
                                    {
                                        // BTM
                                        if (_dsp.LOP != 0)
                                        {
                                            _dsp.JumpAddress = _dsp.TOP;
                                            _dsp.JumpDelayed = false;
                                            _dsp.LOP--;
                                        }
                                    }
                                    break;
                                case 0x0F:
                                    _dsp.ProgControlPort.EX = 0;
                                    if ((instruction & 0x08000000) != 0)
                                    {
                                        _dsp.ProgControlPort.E = 1;
                                        SendDspEnd();
                                    }
                                    _dsp.ProgControlPort.P = (byte)(_dsp.PC + 1);
                                    timing = 1;
                                    break;
                            }
                            break;
                        default:
                            break;
                    }

                    _dsp.Multiply = (long)_dsp.RX * _dsp.RY;
                    _dsp.PC++;
                    if (_dsp.JumpAddress != uint.MaxValue)
                    {
                        if (_dsp.JumpDelayed)
                        {
                            _dsp.PC = (byte)_dsp.JumpAddress;
                            _dsp.JumpAddress = uint.MaxValue;
                        }
                        else
                            _dsp.JumpDelayed = true;
                    }

                    timing--;
                }
            }

            if (dmaCycles > 0)
                ScuDmaTickAll((uint)dmaCycles);
        }
    }

    public byte ReadByte(uint address) =>
        (address & 0xFF) == 0xA7 ? unchecked((byte)_registers.IST) : (byte)0;

    public ushort ReadWord(uint address) => 0;

    private void SetDspProgram(uint value)
    {
        _dsp.ProgramRam[_dsp.PC] = value;
        _dsp.PC = unchecked((byte)(_dsp.PC + 1));
        _dsp.ProgControlPort.P = _dsp.PC;
    }

    private void SetDspDataAddress(uint value)
    {
        _dsp.DataRamPage = (byte)((value >> 6) & 3);
        _dsp.DataRamReadAddress = (byte)(value & 0x3F);
    }

    private void SetDspDataRamData(uint value)
    {
        if (_dsp.ProgControlPort.EX == 0)
        {
            byte address = _dsp.DataRamReadAddress;
            _dsp.MD[_dsp.DataRamPage, address & 0x3F] = value;
            _dsp.DataRamReadAddress = unchecked((byte)(address + 1));
        }
    }

    private void SetDspProgramControl(uint value)
    {
        _dsp.ProgControlPort.All = (_dsp.ProgControlPort.All & 0x00FC0000) | (value & 0x060380FF);
        if (_dsp.ProgControlPort.LE != 0)
            _dsp.PC = (byte)_dsp.ProgControlPort.P;
    }

    private uint GetDspProgramControl() => _dsp.ProgControlPort.All & 0x00FD00FF;

    private uint GetDspDataRam()
    {
        if (_dsp.ProgControlPort.EX == 0)
        {
            byte address = _dsp.DataRamReadAddress;
            uint value = _dsp.MD[_dsp.DataRamPage, address & 0x3F];
            _dsp.DataRamReadAddress = unchecked((byte)(address + 1));
            return value;
        }

        return 0;
    }

    private DspInterface InitializeDspInterface() => new(
        SetDspProgram, SetDspDataAddress, SetDspDataRamData,
        SetDspProgramControl, GetDspProgramControl, GetDspDataRam);

    public uint ReadLong(uint address)
    {
        address &= 0xFF;
        switch (address)
        {
            case 0x00: return _registers.D0R;
            case 0x04: return _registers.D0W;
            case 0x08: return _registers.D0C;
            case 0x20: return _registers.D1R;
            case 0x24: return _registers.D1W;
            case 0x28: return _registers.D1C;
            case 0x40: return _registers.D2R;
            case 0x44: return _registers.D2W;
            case 0x48: return _registers.D2C;
            case 0x7C: return _registers.DSTA;
            case 0x80: return _dspInterface.GetProgramControl();
            case 0x8C: return _dspInterface.GetDataRam();
            case 0xA4: return _registers.IST;
            case 0xA8: return _registers.AIACK;
            case 0xC4: return _registers.RSEL;
            case 0xC8: return _registers.VER;
            default: return 0;
        }
    }

    public void WriteByte(uint address, byte value)
    {
        if ((address & 0xFF) == 0xA7)
            _registers.IST &= 0xFFFFFF00u | value;
    }

    public void WriteWord(uint address, ushort value) { }

    public void WriteLong(uint address, uint value)
    {
        address &= 0xFF;
        switch (address)
        {
            case 0x00:
                _registers.D0R = value;
                break;
            case 0x04:
                _registers.D0W = value;
                break;
            case 0x08:
                _registers.D0C = value;
                break;
            case 0x0C:
                _registers.D0AD = value;
                break;
            case 0x10:
                if ((value & 1) != 0 && (_registers.D0MD & 7) == 7)
                {
                    if (_useDmaTiming)
                    {
                        ScuInsertDma(_registers.D0R, _registers.D0W, _registers.D0C, _registers.D0AD, _registers.D0MD, 0);
                    }
                    else
                    {
                        ScuDma(new DmaInfo
                        {
                            Mode = 0,
                            ReadAddress = _registers.D0R,
                            WriteAddress = _registers.D0W,
                            TransferNumber = _registers.D0C,
                            AddValue = _registers.D0AD,
                            ModeAddressUpdate = _registers.D0MD,
                        });
                    }
                }
                _registers.D0EN = value;
                break;
            case 0x14:
                _registers.D0MD = value;
                break;
            case 0x20:
                _registers.D1R = value;
                break;
            case 0x24:
                _registers.D1W = value;
                break;
            case 0x28:
                _registers.D1C = value;
                break;
            case 0x2C:
                _registers.D1AD = value;
                break;
            case 0x30:
                if ((value & 1) != 0 && (_registers.D1MD & 7) == 7)
                {
                    if (_useDmaTiming)
                    {
                        ScuInsertDma(_registers.D1R, _registers.D1W, _registers.D1C, _registers.D1AD, _registers.D1MD, 1);
                    }
                    else
                    {
                        ScuDma(new DmaInfo
                        {
                            Mode = 1,
                            ReadAddress = _registers.D1R,
                            WriteAddress = _registers.D1W,
                            TransferNumber = _registers.D1C,
                            AddValue = _registers.D1AD,
                            ModeAddressUpdate = _registers.D1MD,
                        });
                    }
                }
                _registers.D1EN = value;
                break;
            case 0x34:
                _registers.D1MD = value;
                break;
            case 0x40:
                _registers.D2R = value;
                break;
            case 0x44:
                _registers.D2W = value;
                break;
            case 0x48:
                _registers.D2C = value;
                break;
            case 0x4C:
                _registers.D2AD = value;
                break;
            case 0x50:
                if ((value & 1) != 0 && (_registers.D2MD & 7) == 7)
                {
                    if (_useDmaTiming)
                    {
                        ScuInsertDma(_registers.D2R, _registers.D2W, _registers.D2C, _registers.D2AD, _registers.D2MD, 2);
                    }
                    else
                    {
                        ScuDma(new DmaInfo
                        {
                            Mode = 2,
                            ReadAddress = _registers.D2R,
                            WriteAddress = _registers.D2W,
                            TransferNumber = _registers.D2C,
                            AddValue = _registers.D2AD,
                            ModeAddressUpdate = _registers.D2MD,
                        });
                    }
                }
                _registers.D2EN = value;
                break;
            case 0x54:
                _registers.D2MD = value;
                break;
            case 0x60:
                _registers.DSTP = value;

                break;
            case 0x80:
                _dspInterface.SetProgramControl(value);
                break;
            case 0x84:
                _dspInterface.SetProgram(value);
                break;
            case 0x88:
                _dspInterface.SetDataAddress(value);
                break;
            case 0x8C:
                _dspInterface.SetDataRamData(value);
                break;
            case 0x90:
                _registers.T0C = value;
                break;
            case 0x94:
                _registers.T1S = value;
                break;
            case 0x98:
                _registers.T1MD = value;
                break;
            case 0xA0:
                _registers.IMS = value;
                TestInterruptMask();
                break;
            case 0xA4:
                _registers.IST &= value;
                break;
            case 0xA8:
                _registers.AIACK = value;
                if ((value & 1) != 0)
                {
                    _cdInterruptProhibited = false;
                    CheckCdInterruptLine();
                }
                TestInterruptMask();
                break;
            case 0xB0:
                _registers.ASR0 = value;
                break;
            case 0xB4:
                _registers.ASR1 = value;
                break;
            case 0xB8:
                _registers.AREF = value;
                break;
            case 0xC4:
                _registers.RSEL = value;
                break;
        }
    }

    private void TestInterruptMask()
    {
        for (uint i = 0; i < _registers.NumberOfInterrupts; i++)
        {
            uint index = _registers.NumberOfInterrupts - 1 - i;
            Interrupt interrupt = _registers.Interrupts[index];
            if ((_registers.IMS & interrupt.Mask) == 0)
            {
                _sendMasterInterrupt(interrupt.Vector, interrupt.Level);
                _registers.IST &= ~interrupt.StatusBit;

                for (uint j = index; j < _registers.NumberOfInterrupts - 1; j++)
                    _registers.Interrupts[j] = _registers.Interrupts[j + 1];

                _registers.NumberOfInterrupts--;
                break;
            }
        }
    }

    private void QueueInterrupt(byte vector, byte level, ushort mask, uint statusBit)
    {
        for (uint i = 0; i < _registers.NumberOfInterrupts; i++)
        {
            if (_registers.Interrupts[i].Vector == vector)
                return;
        }

        _registers.Interrupts[_registers.NumberOfInterrupts] = new Interrupt(vector, level, mask, statusBit);
        _registers.NumberOfInterrupts++;

        for (uint i = 0; i < _registers.NumberOfInterrupts - 1; i++)
        {
            for (uint j = i + 1; j < _registers.NumberOfInterrupts; j++)
            {
                if (_registers.Interrupts[i].Level > _registers.Interrupts[j].Level)
                {
                    Interrupt item = _registers.Interrupts[i];
                    _registers.Interrupts[i] = _registers.Interrupts[j];
                    _registers.Interrupts[j] = item;
                }
            }
        }
    }

    private void SendInterrupt(byte vector, byte level, ushort mask, uint statusBit)
    {
        if ((_registers.IMS & mask) == 0)
        {
            _sendMasterInterrupt(vector, level);
        }
        else
        {
            QueueInterrupt(vector, level, mask, statusBit);
            _registers.IST |= statusBit;
        }
    }

    private void CheckInterruptDma(uint id)
    {
        if ((_registers.D0EN & 0x100) != 0 && (_registers.D0MD & 7) == id)
        {
            ScuDma(new DmaInfo
            {
                Mode = 0,
                ReadAddress = _registers.D0R,
                WriteAddress = _registers.D0W,
                TransferNumber = _registers.D0C,
                AddValue = _registers.D0AD,
                ModeAddressUpdate = _registers.D0MD,
            });
            _registers.D0EN = 0;
        }
        if ((_registers.D1EN & 0x100) != 0 && (_registers.D1MD & 7) == id)
        {
            ScuDma(new DmaInfo
            {
                Mode = 1,
                ReadAddress = _registers.D1R,
                WriteAddress = _registers.D1W,
                TransferNumber = _registers.D1C,
                AddValue = _registers.D1AD,
                ModeAddressUpdate = _registers.D1MD,
            });
            _registers.D1EN = 0;
        }
        if ((_registers.D2EN & 0x100) != 0 && (_registers.D2MD & 7) == id)
        {
            ScuDma(new DmaInfo
            {
                Mode = 2,
                ReadAddress = _registers.D2R,
                WriteAddress = _registers.D2W,
                TransferNumber = _registers.D2C,
                AddValue = _registers.D0AD,
                ModeAddressUpdate = _registers.D2MD,
            });
            _registers.D2EN = 0;
        }
    }

    public void SendVBlankIn()
   
  {
        SendInterrupt(0x40, 0xF, 0x0001, 0x0001);
        CheckInterruptDma(0);
    }

    public void SendVBlankOut()
    {
        SendInterrupt(0x41, 0xE, 0x0002, 0x0002);
        _registers.Timer0 = 0;
        if ((_registers.T1MD & 1) != 0 && _registers.Timer0 == _registers.T0C)
            SendTimer0();
        CheckInterruptDma(1);
    }

    public void SendHBlankIn()
    {
        SendInterrupt(0x42, 0xD, 0x0004, 0x0004);
        _registers.Timer0++;
        if ((_registers.T1MD & 1) != 0 && _registers.Timer0 == _registers.T0C)
            SendTimer0();
        CheckInterruptDma(2);
    }

    public void SendTimer0()
   
  {
        SendInterrupt(0x43, 0xC, 0x0008, 0x00000008);
        CheckInterruptDma(3);
    }

    public void SendTimer1()
   
  {
        SendInterrupt(0x44, 0xB, 0x0010, 0x00000010);
        CheckInterruptDma(4);
    }

    public void SendDspEnd()
    {
        SendInterrupt(0x45, 0xA, 0x0020, 0x00000020);
    }

    public void SendSoundRequest()
    {
        SendInterrupt(0x46, 0x9, 0x0040, 0x00000040);
        CheckInterruptDma(5);
    }

    public void SendSystemManager()
   
  {
        SendInterrupt(0x47, 0x8, 0x0080, 0x00000080);
    }

    public void SendPadInterrupt()
   
  {
        SendInterrupt(0x48, 0x8, 0x0100, 0x00000100);
    }

    public void SendLevel2DmaEnd()
   
  {
        SendInterrupt(0x49, 0x6, 0x0200, 0x00000200);
    }

    public void SendLevel1DmaEnd()
   
  {
        SendInterrupt(0x4A, 0x6, 0x0400, 0x00000400);
    }

    public void SendLevel0DmaEnd()
   
  {
        SendInterrupt(0x4B, 0x5, 0x0800, 0x00000800);
    }

    public void SendDmaIllegal()
   
  {
        SendInterrupt(0x4C, 0x3, 0x1000, 0x00001000);
    }

    public void SendDrawEnd()
   
  {
        SendInterrupt(0x4D, 0x2, 0x2000, 0x00002000);
        CheckInterruptDma(6);
    }

    private bool _cdInterruptAsserted;
    private bool _cdInterruptProhibited;

    public void SetCdInterruptLine(bool asserted)
    {
        _cdInterruptAsserted = asserted;
        if (CheckCdInterruptLine())
            TestInterruptMask();
    }

    private bool CheckCdInterruptLine()
    {
        if (!_cdInterruptAsserted || _cdInterruptProhibited)
            return false;

        _cdInterruptProhibited = true;
        _registers.IST |= 0x00010000;
        QueueInterrupt(0x50, 0x7, 0x8000, 0x00010000);
        return true;
    }

    public void SendExternalInterrupt00()
   
  {
        SendInterrupt(0x50, 0x7, 0x8000, 0x00010000);
    }

    public void SendExternalInterrupt01()
   
  {
        SendInterrupt(0x51, 0x7, 0x8000, 0x00020000);
    }

    public void SendExternalInterrupt02()
   
  {
        SendInterrupt(0x52, 0x7, 0x8000, 0x00040000);
    }

    public void SendExternalInterrupt03()
   
  {
        SendInterrupt(0x53, 0x7, 0x8000, 0x00080000);
    }

    public void SendExternalInterrupt04()
   
  {
        SendInterrupt(0x54, 0x4, 0x8000, 0x00100000);
    }

    public void SendExternalInterrupt05()
   
  {
        SendInterrupt(0x55, 0x4, 0x8000, 0x00200000);
    }

    public void SendExternalInterrupt06()
   
  {
        SendInterrupt(0x56, 0x4, 0x8000, 0x00400000);
    }

    public void SendExternalInterrupt07()
   
  {
        SendInterrupt(0x57, 0x4, 0x8000, 0x00800000);
    }

    public void SendExternalInterrupt08()
   
  {
        SendInterrupt(0x58, 0x1, 0x8000, 0x01000000);
    }

    public void SendExternalInterrupt09()
   
  {
        SendInterrupt(0x59, 0x1, 0x8000, 0x02000000);
    }

    public void SendExternalInterrupt10()
   
  {
        SendInterrupt(0x5A, 0x1, 0x8000, 0x04000000);
    }

    public void SendExternalInterrupt11()
   
  {
        SendInterrupt(0x5B, 0x1, 0x8000, 0x08000000);
    }

    public void SendExternalInterrupt12()
   
  {
        SendInterrupt(0x5C, 0x1, 0x8000, 0x10000000);
    }

    public void SendExternalInterrupt13()
   
  {
        SendInterrupt(0x5D, 0x1, 0x8000, 0x20000000);
    }

    public void SendExternalInterrupt14()
   
  {
        SendInterrupt(0x5E, 0x1, 0x8000, 0x40000000);
    }

    public void SendExternalInterrupt15()
   
  {
        SendInterrupt(0x5F, 0x1, 0x8000, 0x80000000);
    }
}
