using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.ARM.ARMv3.ARM7;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;
using RetroMediaLab.Chips.Dreamcast.Holly;
using static RetroMediaLab.Chips.Dreamcast.Holly.HollySystemBus;
using static RetroMediaLab.Chips.Dreamcast.Holly.HollyInterruptId;

namespace RetroMediaLab.Chips.Audio.Yamaha.AICA;

internal static unsafe partial class AicaInterface
{
    internal static readonly RamRegion RamRegion = new();

    internal static byte* Ram
    {
        get
        {
            fixed (byte* ram = &RamRegion[0])
                return ram;
        }
    }

    private static uint _vreg;
    private static uint _armReset;
    private static uint _rtcEnabled;
    private static int _dmaSchedulerId = -1;
    private static uint _realTimeClock;
    private static int _rtcSchedulerId = -1;

    internal static partial uint GetRtcNow()
    {
        const int epoch = (20 * 365 + 5) * 24 * 60 * 60;
        if (Sh4Configuration.GgpoEnabled)
            return epoch;

        long rawTime = NativeTime(null);
        NativeLocalTime(out NativeCalendar local, in rawTime);
        NativeUtcTime(out NativeCalendar utc, in rawTime);
        utc.IsDaylightSaving = -1;
        long timeOffset = NativeMakeTime(ref local) - NativeMakeTime(ref utc);
        return unchecked((uint)(epoch + rawTime + timeOffset));
    }

    internal static partial T ReadRtcRegister<T>(uint address) where T : unmanaged, IBinaryInteger<T>
    {
        switch (address & 0xFF)
        {
            case 0:
                return T.CreateTruncating(_realTimeClock >> 16);
            case 4:
                return T.CreateTruncating(_realTimeClock & 0xFFFF);
            case 8:
                return T.Zero;
        }

        Trace.TraceWarning($"readRtcReg: invalid address {address:x} sz {sizeof(T)}");
        return T.Zero;
    }

    internal static partial void WriteRtcRegister<T>(uint address, T data) where T : unmanaged, IBinaryInteger<T>
    {
        uint bits = uint.CreateTruncating(data);
        switch (address & 0xFF)
        {
            case 0:
                if (_rtcEnabled != 0)
                {
                    _realTimeClock &= 0xFFFF;
                    _realTimeClock |= (bits & 0xFFFF) << 16;
                    _rtcEnabled = 0;
                }
                break;
            case 4:
                if (_rtcEnabled != 0)
                {
                    _realTimeClock &= 0xFFFF0000;
                    _realTimeClock |= bits & 0xFFFF;
                }
                break;
            case 8:
                _rtcEnabled = bits & 1;
                break;
            default:
                Trace.TraceWarning($"writeRtcReg: invalid address {address:x} sz {sizeof(T)} data {bits:x}");
                break;
        }
    }

    internal static partial T ReadAicaRegister<T>(uint address) where T : unmanaged, IBinaryInteger<T>
    {
        address &= 0x7FFF;
        if (sizeof(T) == 1)
        {
            switch (address)
            {
                case 0x2C00:
                    return T.CreateTruncating(_armReset);
                case 0x2C01:
                    return T.CreateTruncating(_vreg);
                default:
                    break;
            }
        }
        else if (address == 0x2C00)
        {
            return T.CreateTruncating((_vreg << 8) | _armReset);
        }

        if (sizeof(T) == 4)
            return T.CreateTruncating(AicaMemory.ReadRegisterInternal<ushort>(address));
        else
            return AicaMemory.ReadRegisterInternal<T>(address);
    }

    private static void ArmSetReset()
    {
        _armReset &= 1;
        Arm7Di.Enable(_armReset == 0);
    }

    internal static partial void WriteAicaRegister<T>(uint address, T data) where T : unmanaged, IBinaryInteger<T>
    {
        address &= 0x7FFF;
        uint bits = uint.CreateTruncating(data);
        if (sizeof(T) == 1)
        {
            switch (address)
            {
                case 0x2C00:
                    _armReset = bits;
                    Trace.TraceInformation($"ARMRST = {_armReset:X2}");
                    ArmSetReset();
                    return;
                case 0x2C01:
                    _vreg = bits;
                    Trace.TraceInformation($"VREG = {_vreg:X2}");
                    return;
                default:
                    break;
            }
        }
        else if (address == 0x2C00)
        {
            _vreg = (bits >> 8) & 0xFF;
            _armReset = bits & 0xFF;
            Trace.TraceInformation($"VREG = {_vreg:X2} ARMRST {_armReset:X2}");
            ArmSetReset();
            return;
        }

        if (sizeof(T) == 4)
            AicaMemory.WriteRegisterInternal(address, (ushort)bits);
        else
            AicaMemory.WriteRegisterInternal(address, data);
    }

    private static int DreamcastSecond(int tag, int cycles, int jitter, nint argument)
    {
        _realTimeClock++;
        Sh4DreamcastProfiler.Periodical();
#if !SH4_DYNAREC_NONE
        Sh4RecompilerDriver.Current.BlockManager.PeriodicalOneSecond();
#endif
        return Sh4MainClock;
    }

    internal static partial void InitRtc()
    {
        _realTimeClock = GetRtcNow();
        if (_rtcSchedulerId == -1)
            _rtcSchedulerId = Sh4DreamcastRuntime.Scheduler.Register(0, DreamcastSecond);
    }

    internal static partial void ResetRtc(bool hard)
    {
        if (hard)
        {
            InitRtc();
            Sh4DreamcastRuntime.Scheduler.Request(_rtcSchedulerId, Sh4MainClock);
        }

        _vreg = 0;
        _armReset = 0;
    }

    internal static partial void TermRtc()
    {
        Sh4DreamcastRuntime.Scheduler.Unregister(_rtcSchedulerId);
        _rtcSchedulerId = -1;
    }

    private static int DmaEndScheduled(int tag, int cycles, int jitter, nint argument)
    {
        uint length = SB_ADLEN & 0x7FFFFFFF;
        if ((SB_ADLEN & 0x80000000) != 0)
            SB_ADEN = 0;
        else
            SB_ADEN = 1;

        SB_ADSTAR += length;
        SB_ADSTAG += length;
        SB_ADST = 0;
        SB_ADLEN = 0;
        SB_ADSUSP |= 0x10;
        HollyInterruptController.RaiseInterrupt(SpuDma);
        return 0;
    }

    private static bool CheckStag(uint address)
    {
#if SH4_STRICT_MODE
        uint area = (address >> 26) & 7;
        return area == 0 || area == 5;
#else
        return true;
#endif
    }

    private static bool CheckStar(uint address)
    {
#if SH4_STRICT_MODE
        uint area = (address >> 26) & 7;
        return area == 3 || area == 1;
#else
        return true;
#endif
    }

    private static bool CheckStarOverrun(uint address)
    {
#if SH4_STRICT_MODE
        uint bottom = (((SB_G2APRO >> 8) & 0x7F) << 20) | 0x08000000;
        uint top = ((SB_G2APRO & 0x7F) << 20) | 0x080FFFE0;
        return address >= bottom && address <= top;
#else
        return true;
#endif
    }

    private static void WriteDmaStart(uint address, uint data, uint enable, uint start, uint source, uint destination,
        uint length, uint direction, HollyInterruptId interrupt, HollyInterruptId invalidAddressInterrupt,
        HollyInterruptId overrunInterrupt, string logTag)
    {
        ref uint enableReg = ref Register32(enable);
        ref uint startReg = ref Register32(start);
        ref uint sourceReg = ref Register32(source);
        ref uint destinationReg = ref Register32(destination);
        ref uint lengthReg = ref Register32(length);
        uint directionReg = Register32(direction);

        if ((data & 1) == 0 || enableReg == 0)
            return;

        uint src = sourceReg;
        uint dst = destinationReg;
        uint len = lengthReg & 0x7FFFFFFF;

        if (!CheckStar(src))
        {
            Trace.TraceInformation($"{logTag}: Invalid src address {src:x8}");
            startReg = 0;
            enableReg = 0;
            HollyInterruptController.RaiseInterrupt(invalidAddressInterrupt);
            return;
        }

        if (!CheckStag(dst))
        {
            Trace.TraceInformation($"{logTag}: Invalid dst address {dst:x8}");
            startReg = 0;
            enableReg = 0;
            HollyInterruptController.RaiseInterrupt(invalidAddressInterrupt);
            return;
        }

        if (!CheckStarOverrun(src) || !CheckStarOverrun(src + len - 1))
        {
            Trace.TraceInformation($"{logTag}: Overrun address {src:x8} len {len:x}");
            startReg = 0;
            enableReg = 0;
            HollyInterruptController.RaiseInterrupt(overrunInterrupt);
            return;
        }

        if (directionReg == 1)
            (src, dst) = (dst, src);

        Debug.WriteLine($"{logTag}: DMA Write to {dst:X} from {src:X} {len} bytes");
        Sh4DreamcastRuntime.AddressSpace.CopyBlockWithoutMmu(dst, src, len);

        if ((lengthReg & 0x80000000) != 0)
            enableReg = 0;
        else
            enableReg = 1;

        sourceReg += len;
        destinationReg += len;
        startReg = 0;
        lengthReg = 0;
        HollyInterruptController.RaiseInterrupt(interrupt);
    }

    private static void WriteSbAdst(uint address, uint data)
    {
        if ((data & 1) == 1 && (SB_ADST & 1) == 0)
        {
            if (SB_ADEN == 1)
            {
                uint src = SB_ADSTAR;
                uint dst = SB_ADSTAG;
                uint len = SB_ADLEN & 0x7FFFFFFF;

                if (!CheckStar(src))
                {
                    Trace.TraceInformation($"AICA-DMA : Invalid src address {src:x8}");
                    SB_ADST = 0;
                    SB_ADEN = 0;
                    HollyInterruptController.RaiseInterrupt(AicaInvalidAddress);
                    return;
                }

                if (!CheckStag(dst))
                {
                    Trace.TraceInformation($"AICA-DMA : Invalid dst address {dst:x8}");
                    SB_ADST = 0;
                    SB_ADEN = 0;
                    HollyInterruptController.RaiseInterrupt(AicaInvalidAddress);
                    return;
                }

                if (!CheckStarOverrun(src) || !CheckStarOverrun(src + len - 1))
                {
                    Trace.TraceInformation($"AICA-DMA : Overrun address {src:x8} len {len:x}");
                    SB_ADST = 0;
                    SB_ADEN = 0;
                    HollyInterruptController.RaiseInterrupt(AicaOverrun);
                    return;
                }

                if (SB_ADDIR == 1)
                {
                    (src, dst) = (dst, src);
                    Debug.WriteLine($"AICA-DMA : SB_ADDIR==1 DMA Read to 0x{dst:X} from 0x{src:X} {SB_ADLEN:x} bytes");
                }
                else
                {
                    Debug.WriteLine($"AICA-DMA : SB_ADDIR==0:DMA Write to 0x{dst:X} from 0x{src:X} {SB_ADLEN:x} bytes");
                }

                Sh4DreamcastRuntime.AddressSpace.CopyBlockWithoutMmu(dst, src, len);
                SB_ADST = 1;
                SB_ADSUSP &= ~0x10u;

                int cycles = unchecked((int)(len * (Sh4MainClock / 2 / G2BusClock)));
                if (cycles <= 512)
                    DmaEndScheduled(0, 0, 0, 0);
                else
                    Sh4DreamcastRuntime.Scheduler.Request(_dmaSchedulerId, cycles);
            }
        }
    }

    private static uint ReadSbAdst(uint address)
    {
        if ((SB_ISTNRM & (1u << (byte)SpuDma)) != 0 && (SB_ADST & 2) == 0)
        {
            SB_ADST |= 2;
            return 1;
        }
        else
        {
            SB_ADST &= ~2u;
            return SB_ADST;
        }
    }

    private static void WriteSbStag(uint address, uint data, uint stag, HollyInterruptId invalidAddressInterrupt, string logTag)
    {
        ref uint stagReg = ref RawRegisters[(stag - SB_BASE) / 4];
        stagReg = data & 0x1FFFFFE0;

        if (!CheckStag(data))
        {
            Trace.TraceInformation($"{logTag} Write_SB_STAG: Invalid address {data:x8}");
            HollyInterruptController.RaiseInterrupt(invalidAddressInterrupt);
        }
    }

    private static void WriteSbStar(uint address, uint data, uint star, HollyInterruptId invalidAddressInterrupt, string logTag)
    {
        ref uint starReg = ref RawRegisters[(star - SB_BASE) / 4];
        starReg = data & 0x1FFFFFE0;

        if (!CheckStar(data))
        {
            Trace.TraceInformation($"{logTag} Write_SB_STAR: Invalid address {data:x8}");
            HollyInterruptController.RaiseInterrupt(invalidAddressInterrupt);
        }
    }

    private static void WriteSbG2Apro(uint address, uint data)
    {
        if ((data >> 16) == 0x4659)
            SB_G2APRO = data & 0x00007F7F;
    }

    internal const string AicaTag = "G2-AICA DMA";
    internal const string Ext1Tag = "G2-EXT1 DMA";
    internal const string Ext2Tag = "G2-EXT2 DMA";
    internal const string DdevTag = "G2-DDev DMA";

    internal static partial void SbInit()
    {
        Registers.SetHandlers(SB_ADST_addr, ReadSbAdst, WriteSbAdst);
        Registers.SetWriteHandler(SB_ADSTAR_addr, (a, d) => WriteSbStar(a, d, SB_ADSTAR_addr, AicaInvalidAddress, AicaTag));
        Registers.SetWriteHandler(SB_ADSTAG_addr, (a, d) => WriteSbStag(a, d, SB_ADSTAG_addr, AicaInvalidAddress, AicaTag));

        Registers.SetWriteHandler(SB_E1ST_addr, (a, d) => WriteDmaStart(a, d, SB_E1EN_addr, SB_E1ST_addr, SB_E1STAR_addr,
            SB_E1STAG_addr, SB_E1LEN_addr, SB_E1DIR_addr, ExtDma1, Ext1InvalidAddress, Ext1Overrun, Ext1Tag));
        Registers.SetWriteHandler(SB_E1STAR_addr, (a, d) => WriteSbStar(a, d, SB_E1STAR_addr, Ext1InvalidAddress, Ext1Tag));
        Registers.SetWriteHandler(SB_E1STAG_addr, (a, d) => WriteSbStag(a, d, SB_E1STAG_addr, Ext1InvalidAddress, Ext1Tag));

        Registers.SetWriteHandler(SB_E2ST_addr, (a, d) => WriteDmaStart(a, d, SB_E2EN_addr, SB_E2ST_addr, SB_E2STAR_addr,
            SB_E2STAG_addr, SB_E2LEN_addr, SB_E2DIR_addr, ExtDma2, Ext2InvalidAddress, Ext2Overrun, Ext2Tag));
        Registers.SetWriteHandler(SB_E2STAR_addr, (a, d) => WriteSbStar(a, d, SB_E2STAR_addr, Ext2InvalidAddress, Ext2Tag));
        Registers.SetWriteHandler(SB_E2STAG_addr, (a, d) => WriteSbStag(a, d, SB_E2STAG_addr, Ext2InvalidAddress, Ext2Tag));

        Registers.SetWriteHandler(SB_DDST_addr, (a, d) => WriteDmaStart(a, d, SB_DDEN_addr, SB_DDST_addr, SB_DDSTAR_addr,
            SB_DDSTAG_addr, SB_DDLEN_addr, SB_DDDIR_addr, DevDma, DevInvalidAddress, DevOverrun, DdevTag));
        Registers.SetWriteHandler(SB_DDSTAR_addr, (a, d) => WriteSbStar(a, d, SB_DDSTAR_addr, DevInvalidAddress, DdevTag));
        Registers.SetWriteHandler(SB_DDSTAG_addr, (a, d) => WriteSbStag(a, d, SB_DDSTAG_addr, DevInvalidAddress, DdevTag));

        Registers.SetWriteOnly(SB_G2APRO_addr, WriteSbG2Apro);
        _dmaSchedulerId = Sh4DreamcastRuntime.Scheduler.Register(0, DmaEndScheduled);
    }

    internal static partial void SbReset(bool hard)
    {
        if (hard)
        {
            SB_ADST = 0;
            SB_G2APRO = 0x7F00;
        }
    }

    internal static partial void SbTerm()
    {
        Sh4DreamcastRuntime.Scheduler.Unregister(_dmaSchedulerId);
        _dmaSchedulerId = -1;
    }

    internal static partial void Serialize(BinaryWriter writer, bool rollback)
    {
        writer.Write(AicaArmMemory.AicaInterrupt);
        writer.Write(AicaArmMemory.AicaRegisterL);
        writer.Write(AicaArmMemory.E68kOutput);
        writer.Write(AicaArmMemory.E68kRegisterL);
        writer.Write(AicaArmMemory.E68kRegisterM);
        writer.Write(MemoryMarshal.AsBytes(Arm7Di.Reg[..((int)Arm7Register.RN_ARM_REG_COUNT - 1)]));
        writer.Write(Arm7Di.ArmIrqEnable);
        writer.Write(Arm7Di.ArmFiqEnable);
        writer.Write(Arm7Di.ArmMode);
        writer.Write(Arm7Di.Enabled);
        writer.Write(Arm7Di.Arm7ClockTicks);

        AicaDsp.State.Serialize(writer);
        foreach (AicaTimer timer in Aica.Timers)
        {
            writer.Write(timer.CStep);
            writer.Write(timer.MStep);
        }

        if (!rollback)
            RamRegion.Serialize(writer);
        writer.Write(_vreg);
        writer.Write(_armReset);
        writer.Write(_rtcEnabled);
        writer.Write(_realTimeClock);
        writer.Write(AicaMemory.RegisterBytes);
        AicaSoundGenerator.Serialize(writer);
    }

    internal static partial void Deserialize(BinaryReader reader, int version, bool rollback)
    {
        AicaArmMemory.AicaInterrupt = reader.ReadBoolean();
        AicaArmMemory.AicaRegisterL = reader.ReadUInt32();
        AicaArmMemory.E68kOutput = reader.ReadBoolean();
        AicaArmMemory.E68kRegisterL = reader.ReadUInt32();
        AicaArmMemory.E68kRegisterM = reader.ReadUInt32();
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(Arm7Di.Reg[..((int)Arm7Register.RN_ARM_REG_COUNT - 1)]));
        Arm7Di.ArmIrqEnable = reader.ReadBoolean();
        Arm7Di.ArmFiqEnable = reader.ReadBoolean();
        Arm7Di.ArmMode = reader.ReadInt32();
        Arm7Di.Enabled = reader.ReadBoolean();
        if (version >= 814)
            Arm7Di.Arm7ClockTicks = reader.ReadInt32();
        else
            Arm7Di.Arm7ClockTicks = 0;

        AicaDsp.State.Deserialize(reader, version, rollback);
        for (int i = 0; i < 3; i++)
        {
            Aica.Timers[i].CStep = reader.ReadInt32();
            Aica.Timers[i].MStep = reader.ReadUInt32();
        }

        if (!rollback)
        {
            RamRegion.Deserialize(reader);
            if (Sh4DreamcastRuntime.Platform.IsAtomiswave && version < 825)
                reader.BaseStream.ReadExactly(new byte[6 * 1024 * 1024]);
        }

        _vreg = reader.ReadUInt32();
        _armReset = reader.ReadUInt32();
        _rtcEnabled = reader.ReadUInt32();
        _realTimeClock = reader.ReadUInt32();
        reader.BaseStream.ReadExactly(AicaMemory.RegisterBytes);
        AicaSoundGenerator.Deserialize(reader, version);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeCalendar
    {
        internal int Second;
        internal int Minute;
        internal int Hour;
        internal int Day;
        internal int Month;
        internal int Year;
        internal int Weekday;
        internal int YearDay;
        internal int IsDaylightSaving;
    }

    [DllImport("ucrtbase.dll", EntryPoint = "_time64", CallingConvention = CallingConvention.Cdecl)]
    private static extern long NativeTime(long* target);

    [DllImport("ucrtbase.dll", EntryPoint = "_localtime64_s", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeLocalTime(out NativeCalendar result, in long time);

    [DllImport("ucrtbase.dll", EntryPoint = "_gmtime64_s", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeUtcTime(out NativeCalendar result, in long time);

    [DllImport("ucrtbase.dll", EntryPoint = "_mktime64", CallingConvention = CallingConvention.Cdecl)]
    private static extern long NativeMakeTime(ref NativeCalendar time);
}
