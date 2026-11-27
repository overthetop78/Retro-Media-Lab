using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;
using static RetroMediaLab.Chips.Audio.Yamaha.AICA.Aica;

namespace RetroMediaLab.Chips.Audio.Yamaha.AICA;

internal static unsafe partial class AicaMemory
{
    private static readonly uint[] RegisterStorage = GC.AllocateArray<uint>(RegisterSize / sizeof(uint), pinned: true);
    internal static Span<byte> RegisterBytes => MemoryMarshal.AsBytes(RegisterStorage.AsSpan());

    internal static byte* Registers
    {
        get
        {
            fixed (uint* registers = RegisterStorage)
                return (byte*)registers;
        }
    }

    internal static AicaCommonData* CommonData => (AicaCommonData*)&Registers[0x2800];
    internal static AicaDspData* DspData => (AicaDspData*)&Registers[0x3000];
    internal static AicaInterruptInfo* SCIEB => (AicaInterruptInfo*)&Registers[SCIEB_addr];
    internal static AicaInterruptInfo* SCIPD => (AicaInterruptInfo*)&Registers[SCIPD_addr];
    internal static AicaInterruptInfo* SCIRE => (AicaInterruptInfo*)&Registers[SCIRE_addr];
    internal static AicaInterruptInfo* MCIEB => (AicaInterruptInfo*)&Registers[MCIEB_addr];
    internal static AicaInterruptInfo* MCIPD => (AicaInterruptInfo*)&Registers[MCIPD_addr];
    internal static AicaInterruptInfo* MCIRE => (AicaInterruptInfo*)&Registers[MCIRE_addr];
    internal static AicaDspOutputVolumeRegister* DspOutVolume => (AicaDspOutputVolumeRegister*)&Registers[0x2000];

    private static Action<byte>? _midiReceiver;

    internal static partial T ReadRegisterInternal<T>(uint address) where T : unmanaged, IBinaryInteger<T>
    {
        address &= 0x7FFF;
        if (address >= 0x2800 && address < 0x2818)
        {
            AicaSoundGenerator.ReadCommonReg(address, sizeof(T) == 1);
        }
        else if (address >= 0x4000 && address < 0x4580)
        {
            if ((address & 2) != 0)
            {
                Trace.TraceInformation($"Unaligned DSP register read @ {address:x}");
                return T.Zero;
            }

            Debug.WriteLine($"DSP register read @ {address:x}");
            var state = (AicaDsp.DspState*)AicaDsp.StateAddress;
            uint value;
            if (address < 0x4500)
            {
                value = unchecked((uint)(address < 0x4400
                    ? state->TEMP[(address - 0x4000) / 8]
                    : state->MEMS[(address - 0x4400) / 8]));
                if ((address & 4) != 0)
                    value = (value >> 8) & 0xFFFF;
                else
                    value &= 0xFF;
            }
            else
            {
                value = unchecked((uint)state->MIXS[(address - 0x4500) / 8]);
                if ((address & 4) != 0)
                    value = (value >> 4) & 0xFFFF;
                else
                    value &= 0xF;
            }

            if (sizeof(T) == 1)
            {
                if ((address & 1) != 0)
                    value >>= 8;
                else
                    value &= 0xFF;
            }

            return T.CreateTruncating(value);
        }

        return Sh4HardwareRegisterMemory.Read<T>(Registers, address);
    }

    private static void WriteCommonRegister8(uint register, byte data)
    {
        Sh4HardwareRegisterMemory.Write(Registers, register, data);
        if (register == 0x2804 || register == 0x2805)
        {
            AicaDsp.State.RBL = (8192u << (int)CommonData->RBL) - 1;
            AicaDsp.State.RBP = (CommonData->RBP * 2048u) & AicaInterface.RamMask;
            AicaDsp.State.Dirty = true;
        }
        else if (register == 0x280C)
        {
            if (_midiReceiver is not null)
                _midiReceiver(data);
        }
    }

    internal static partial void WriteRegisterInternal<T>(uint address, T data) where T : unmanaged, IBinaryInteger<T>
    {
        int size = sizeof(T);
        address &= 0x7FFF;
        uint bits = uint.CreateTruncating(data);

        if (address < 0x2000)
        {
            uint channel = address >> 7;
            uint register = address & 0x7F;
            Sh4HardwareRegisterMemory.Write(Registers, address, data);
            AicaSoundGenerator.WriteChannelReg(channel, register, size);
            return;
        }

        if (address < 0x2800)
        {
            Sh4HardwareRegisterMemory.Write(Registers, address, data);
            return;
        }

        if (address < 0x2818)
        {
            WriteCommonRegister8(address, (byte)(bits & 0xFF));
            if (size == 2)
                WriteCommonRegister8(address + 1, (byte)(bits >> 8));
            return;
        }

        if (address >= 0x3000)
        {
            if ((address & 2) != 0)
            {
                Trace.TraceInformation($"Unaligned DSP register write @ {address:x}");
                return;
            }

            if (address >= 0x4000 && address < 0x4580)
            {
                var state = (AicaDsp.DspState*)AicaDsp.StateAddress;
                if (address < 0x4500)
                {
                    ref int value = ref (address < 0x4400
                        ? ref state->TEMP[(address - 0x4000) / 8]
                        : ref state->MEMS[(address - 0x4400) / 8]);
                    if ((address & 4) != 0)
                    {
                        if (size == 1)
                        {
                            if ((address & 1) != 0)
                                value = (value & 0x0000FFFF) | ((unchecked((int)bits) << 24) >> 8);
                            else
                                value = (value & unchecked((int)0xFFFF00FF)) | (int)((bits & 0xFF) << 8);
                        }
                        else
                        {
                            value = (value & 0xFF) | ((unchecked((int)bits) << 16) >> 8);
                        }
                    }
                    else
                    {
                        if (size != 1 || (address & 1) == 0)
                            value = (value & ~0xFF) | (int)(bits & 0xFF);
                    }
                    Debug.WriteLine($"DSP TEMP/MEMS register write<{size}> @ {address:x} = {value}");
                }
                else
                {
                    ref int value = ref state->MIXS[(address - 0x4500) / 8];
                    if ((address & 4) != 0)
                    {
                        if (size == 1)
                        {
                            if ((address & 1) != 0)
                                value = (value & 0x00000FFF) | ((unchecked((int)bits) << 24) >> 12);
                            else
                                value = (value & unchecked((int)0xFFFFF00F)) | (int)((bits & 0xFF) << 4);
                        }
                        else
                        {
                            value = (value & 0xF) | ((unchecked((int)bits) << 16) >> 12);
                        }
                    }
                    else
                    {
                        if (size != 1 || (address & 1) == 0)
                            value = (value & ~0xF) | (int)(bits & 0xF);
                    }
                    Debug.WriteLine($"DSP MIXS register write<{size}> @ {address:x} = {value}");
                }

                return;
            }

            Sh4HardwareRegisterMemory.Write(Registers, address, data);
            AicaDsp.WriteProg(address);
            if (size == 2)
                AicaDsp.WriteProg(address + 1);
            return;
        }

        WriteTimerAndInterruptRegister(address, data);
    }

    internal static partial void InitMem()
    {
        RegisterBytes.Clear();
        AicaInterface.Ram[AicaInterface.RamSize - 1] = 1;
        AicaInterface.RamRegion.Zero();
        _midiReceiver = null;
    }

    internal static partial void TermMem()
    {
    }

    internal static void SetMidiReceiver(Action<byte>? handler)
    {
        _midiReceiver = handler;
    }
}
