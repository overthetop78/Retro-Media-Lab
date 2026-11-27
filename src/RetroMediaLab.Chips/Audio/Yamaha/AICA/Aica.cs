using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.ARM.ARMv3.ARM7;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;
using RetroMediaLab.Chips.Dreamcast.Holly;
using static RetroMediaLab.Chips.Audio.Yamaha.AICA.AicaMemory;

namespace RetroMediaLab.Chips.Audio.Yamaha.AICA;

internal static unsafe partial class Aica
{
    private const uint Sh4IrqBit = 1u << ((int)HollyInterruptId.SpuIrq & 31);

    internal static readonly LinkedList<byte> MidiSendBuffer = new();

    private static uint GetLevel(uint interrupt)
    {
        if (interrupt > 7)
            interrupt = 7;

        uint bit = 1u << (int)interrupt;
        uint result = 0;
        if ((CommonData->SCILV0 & bit) != 0)
            result = 1;
        if ((CommonData->SCILV1 & bit) != 0)
            result |= 2;
        if ((CommonData->SCILV2 & bit) != 0)
            result |= 4;
        return result;
    }

    private static void UpdateArmInterrupts()
    {
        uint pending = SCIEB->Full & SCIPD->Full;
        uint level = 0;
        if (pending != 0)
        {
            uint bit = 1;
            for (uint i = 0; i < 11; i++)
            {
                if ((pending & bit) != 0)
                {
                    level = GetLevel(i);
                    break;
                }
                bit <<= 1;
            }
        }

        AicaArmMemory.InterruptChange(pending, level);
    }

    private static bool UpdateSh4Interrupts()
    {
        uint pending = MCIEB->Full & MCIPD->Full;
        if (pending != 0)
        {
            if ((HollySystemBus.SB_ISTEXT & Sh4IrqBit) == 0)
                HollyInterruptController.RaiseInterrupt(HollyInterruptId.SpuIrq);
            return true;
        }
        else
        {
            if ((HollySystemBus.SB_ISTEXT & Sh4IrqBit) != 0)
                HollyInterruptController.CancelInterrupt(HollyInterruptId.SpuIrq);
            return false;
        }
    }

    internal static readonly AicaTimer[] Timers = [new(), new(), new()];
    private static int _schedulerId = -1;
    private const int AicaTick = 4535;

    private static int AicaUpdate(int tag, int cycles, int jitter, nint argument)
    {
        Arm7Di.Run(1);
        return AicaTick;
    }

    internal static void TimeStep()
    {
        foreach (AicaTimer timer in Timers)
            timer.StepTimer(1);

        SCIPD->SAMPLE_DONE = 1;
        MCIPD->SAMPLE_DONE = 1;
        AicaSoundGenerator.Sample();
        UpdateArmInterrupts();
        UpdateSh4Interrupts();
    }

    private static void AicaInternalDma()
    {
        if (CommonData->DEXE == 0)
            return;

        Debug.WriteLine($"AICA internal DMA: DGATE {CommonData->DGATE} DDIR {CommonData->DDIR} DLG {CommonData->DLG:x}");
        if (CommonData->DGATE != 0)
        {
            if (CommonData->DDIR != 0)
            {
                uint address = ((CommonData->DMEA_hi << 16) | (CommonData->DMEA_lo << 2)) & AicaInterface.RamMask;
                uint length = Math.Min(CommonData->DLG, AicaInterface.RamSize - address);
                NativeMemory.Clear(&AicaInterface.Ram[address], length * 4);
            }
            else
            {
                uint address = CommonData->DRGA << 2;
                for (uint i = 0; i < CommonData->DLG; i++, address += 4)
                    AicaInterface.WriteAicaRegister(address, 0u);
            }
        }
        else
        {
            uint waveAddress = ((CommonData->DMEA_hi << 16) | (CommonData->DMEA_lo << 2)) & AicaInterface.RamMask;
            uint registerAddress = CommonData->DRGA << 2;
            uint length = Math.Min(CommonData->DLG, AicaInterface.RamSize - waveAddress);
            if (CommonData->DDIR != 0)
            {
                for (uint i = 0; i < length; i++, waveAddress += 4, registerAddress += 4)
                    *(uint*)&AicaInterface.Ram[waveAddress] = AicaInterface.ReadAicaRegister<uint>(registerAddress);
            }
            else
            {
                for (uint i = 0; i < length; i++, waveAddress += 4, registerAddress += 4)
                    AicaInterface.WriteAicaRegister(registerAddress, *(uint*)&AicaInterface.Ram[waveAddress]);
            }
        }

        CommonData->DEXE = 0;
        MCIPD->DMA_END = 1;
        UpdateSh4Interrupts();
        SCIPD->DMA_END = 1;
        UpdateArmInterrupts();
    }

    internal static void WriteTimerAndInterruptRegister<T>(uint register, T data) where T : unmanaged, IBinaryInteger<T>
    {
        uint bits = uint.CreateTruncating(data);
        switch (register)
        {
            case SCIEB_addr:
                SCIEB->Full = bits & 0x7FF;
                UpdateArmInterrupts();
                break;

            case SCIPD_addr:
                if ((bits & (1 << 5)) != 0)
                {
                    SCIPD->SCPU = 1;
                    UpdateArmInterrupts();
                }
                break;

            case SCIRE_addr:
                SCIPD->Full &= ~bits;
                UpdateArmInterrupts();
                break;

            case MCIEB_addr:
                MCIEB->Full = bits & 0x7FF;
                if (UpdateSh4Interrupts())
                    Arm7Di.AvoidRaceCondition();
                break;

            case MCIPD_addr:
                if ((bits & (1 << 5)) != 0)
                {
                    MCIPD->SCPU = 1;
                    if (UpdateSh4Interrupts())
                        Arm7Di.AvoidRaceCondition();
                }
                break;

            case MCIRE_addr:
                MCIPD->Full &= ~bits;
                UpdateSh4Interrupts();
                break;

            case TIMER_A:
                Sh4HardwareRegisterMemory.Write(Registers, register, data);
                Timers[0].RegisterWrite();
                break;

            case TIMER_B:
                Sh4HardwareRegisterMemory.Write(Registers, register, data);
                Timers[1].RegisterWrite();
                break;

            case TIMER_C:
                Sh4HardwareRegisterMemory.Write(Registers, register, data);
                Timers[2].RegisterWrite();
                break;

            case 0x288C:
                Sh4HardwareRegisterMemory.Write(Registers, register, data);
                AicaInternalDma();
                break;

            default:
                Sh4HardwareRegisterMemory.Write(Registers, register, data);
                break;
        }
    }

    internal static void MidiSend(byte data)
    {
        MidiSendBuffer.AddLast(data);
        SCIPD->MIDI_IN = 1;
        UpdateArmInterrupts();
        MCIPD->MIDI_IN = 1;
        UpdateSh4Interrupts();
    }

    internal static void Init()
    {
        InitMem();
        AicaInterface.InitRtc();

        AicaSoundGenerator.Init();
        if (_schedulerId == -1)
            _schedulerId = Sh4DreamcastRuntime.Scheduler.Register(0, AicaUpdate);
        Arm7Di.Init();
    }

    internal static void Reset(bool hard)
    {
        if (hard)
        {
            InitMem();
            AicaSoundGenerator.Term();
            AicaSoundGenerator.Init();
            Sh4DreamcastRuntime.Scheduler.Request(_schedulerId, AicaTick);
        }

        for (uint i = 0; i < Timers.Length; i++)
            Timers[i].Init(Registers, i);

        AicaInterface.ResetRtc(hard);
        Arm7Di.Reset();
    }

    internal static void Term()
    {
        try
        {
            Arm7Di.Term();
        }
        finally
        {
            try
            {
                AicaSoundGenerator.Term();
            }
            finally
            {
                try
                {
                    TermMem();
                }
                finally
                {
                    Sh4DreamcastRuntime.Scheduler.Unregister(_schedulerId);
                    _schedulerId = -1;
                }
            }
        }
    }
}

internal static partial class AicaInterface
{
    internal static void TimeStep() => Aica.TimeStep();
}
