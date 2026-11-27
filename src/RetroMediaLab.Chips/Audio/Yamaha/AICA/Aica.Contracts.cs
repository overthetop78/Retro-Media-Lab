using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.Audio.Yamaha.AICA;

internal static partial class Aica
{
    internal const uint SCIEB_addr = 0x289C;
    internal const uint SCIPD_addr = (0x289C+4);
    internal const uint SCIRE_addr = (0x289C+8);
    internal const uint MCIEB_addr = 0x28B4;
    internal const uint MCIPD_addr = (0x28B4+4);
    internal const uint MCIRE_addr = (0x28B4+8);
    internal const uint TIMER_A = 0x2890;
    internal const uint TIMER_B = (0x2890+4);
    internal const uint TIMER_C = (0x2890+8);
    internal const uint REG_L = (0x2D00);
    internal const uint REG_M = (0x2D04);
    internal const uint SCILV0_addr = 0x28A8;
    internal const uint SCILV1_addr = 0x28AC;
    internal const uint SCILV2_addr = 0x28B0;
}

[StructLayout(LayoutKind.Explicit, Size = 0x508)]
internal struct AicaCommonData
{
    [FieldOffset(0x0)] private uint _word000;
    [FieldOffset(0x4)] private uint _word004;
    [FieldOffset(0x8)] private uint _word008;
    [FieldOffset(0xc)] private uint _word00c;
    [FieldOffset(0x10)] private uint _word010;
    [FieldOffset(0x14)] private uint _word014;
    [FieldOffset(0x80)] private uint _word080;
    [FieldOffset(0x84)] private uint _word084;
    [FieldOffset(0x88)] private uint _word088;
    [FieldOffset(0x8c)] private uint _word08c;
    [FieldOffset(0x90)] private uint _word090;
    [FieldOffset(0x94)] private uint _word094;
    [FieldOffset(0x98)] private uint _word098;
    [FieldOffset(0x9c)] private uint _word09c;
    [FieldOffset(0xa0)] private uint _word0a0;
    [FieldOffset(0xa4)] private uint _word0a4;
    [FieldOffset(0xa8)] private uint _word0a8;
    [FieldOffset(0xac)] private uint _word0ac;
    [FieldOffset(0xb0)] private uint _word0b0;
    [FieldOffset(0xb4)] private uint _word0b4;
    [FieldOffset(0xb8)] private uint _word0b8;
    [FieldOffset(0xbc)] private uint _word0bc;
    [FieldOffset(0x400)] private uint _word400;
    [FieldOffset(0x500)] private uint _word500;
    [FieldOffset(0x504)] private uint _word504;

    internal uint MVOL { readonly get => _word000 & 0xFu; set => _word000 = (_word000 & ~0xFu) | (value & 0xFu); }
    internal uint VER { readonly get => (_word000 >> 4) & 0xFu; set => _word000 = (_word000 & ~0xF0u) | ((value & 0xFu) << 4); }
    internal uint DAC18B { readonly get => (_word000 >> 8) & 0x1u; set => _word000 = (_word000 & ~0x100u) | ((value & 0x1u) << 8); }
    internal uint MEM8MB { readonly get => (_word000 >> 9) & 0x1u; set => _word000 = (_word000 & ~0x200u) | ((value & 0x1u) << 9); }
    internal uint Mono { readonly get => (_word000 >> 15) & 0x1u; set => _word000 = (_word000 & ~0x8000u) | ((value & 0x1u) << 15); }
    internal uint RBP { readonly get => _word004 & 0xFFFu; set => _word004 = (_word004 & ~0xFFFu) | (value & 0xFFFu); }
    internal uint RBL { readonly get => (_word004 >> 13) & 0x3u; set => _word004 = (_word004 & ~0x6000u) | ((value & 0x3u) << 13); }
    internal uint TESTB0 { readonly get => (_word004 >> 15) & 0x1u; set => _word004 = (_word004 & ~0x8000u) | ((value & 0x1u) << 15); }
    internal uint MIBUF { readonly get => _word008 & 0xFFu; set => _word008 = (_word008 & ~0xFFu) | (value & 0xFFu); }
    internal uint MIEMP { readonly get => (_word008 >> 8) & 0x1u; set => _word008 = (_word008 & ~0x100u) | ((value & 0x1u) << 8); }
    internal uint MIFUL { readonly get => (_word008 >> 9) & 0x1u; set => _word008 = (_word008 & ~0x200u) | ((value & 0x1u) << 9); }
    internal uint MIOVF { readonly get => (_word008 >> 10) & 0x1u; set => _word008 = (_word008 & ~0x400u) | ((value & 0x1u) << 10); }
    internal uint MOEMP { readonly get => (_word008 >> 11) & 0x1u; set => _word008 = (_word008 & ~0x800u) | ((value & 0x1u) << 11); }
    internal uint MOFUL { readonly get => (_word008 >> 12) & 0x1u; set => _word008 = (_word008 & ~0x1000u) | ((value & 0x1u) << 12); }
    internal uint MOBUF { readonly get => _word00c & 0xFFu; set => _word00c = (_word00c & ~0xFFu) | (value & 0xFFu); }
    internal uint MSLC { readonly get => (_word00c >> 8) & 0x3Fu; set => _word00c = (_word00c & ~0x3F00u) | ((value & 0x3Fu) << 8); }
    internal uint AFSEL { readonly get => (_word00c >> 14) & 0x1u; set => _word00c = (_word00c & ~0x4000u) | ((value & 0x1u) << 14); }
    internal uint EG { readonly get => _word010 & 0x1FFFu; set => _word010 = (_word010 & ~0x1FFFu) | (value & 0x1FFFu); }
    internal uint SGC { readonly get => (_word010 >> 13) & 0x3u; set => _word010 = (_word010 & ~0x6000u) | ((value & 0x3u) << 13); }
    internal uint LP { readonly get => (_word010 >> 15) & 0x1u; set => _word010 = (_word010 & ~0x8000u) | ((value & 0x1u) << 15); }
    internal uint CA { readonly get => _word014 & 0xFFFFu; set => _word014 = (_word014 & ~0xFFFFu) | (value & 0xFFFFu); }
    internal uint MRWINH { readonly get => _word080 & 0xFu; set => _word080 = (_word080 & ~0xFu) | (value & 0xFu); }
    internal uint T { readonly get => (_word080 >> 4) & 0x1u; set => _word080 = (_word080 & ~0x10u) | ((value & 0x1u) << 4); }
    internal uint TSCD { readonly get => (_word080 >> 5) & 0x7u; set => _word080 = (_word080 & ~0xE0u) | ((value & 0x7u) << 5); }
    internal uint DMEA_hi { readonly get => (_word080 >> 9) & 0x7Fu; set => _word080 = (_word080 & ~0xFE00u) | ((value & 0x7Fu) << 9); }
    internal uint DMEA_lo { readonly get => (_word084 >> 2) & 0x3FFFu; set => _word084 = (_word084 & ~0xFFFCu) | ((value & 0x3FFFu) << 2); }
    internal uint DRGA { readonly get => (_word088 >> 2) & 0x1FFFu; set => _word088 = (_word088 & ~0x7FFCu) | ((value & 0x1FFFu) << 2); }
    internal uint DGATE { readonly get => (_word088 >> 15) & 0x1u; set => _word088 = (_word088 & ~0x8000u) | ((value & 0x1u) << 15); }
    internal uint DEXE { readonly get => _word08c & 0x1u; set => _word08c = (_word08c & ~0x1u) | (value & 0x1u); }
    internal uint DLG { readonly get => (_word08c >> 2) & 0x1FFFu; set => _word08c = (_word08c & ~0x7FFCu) | ((value & 0x1FFFu) << 2); }
    internal uint DDIR { readonly get => (_word08c >> 15) & 0x1u; set => _word08c = (_word08c & ~0x8000u) | ((value & 0x1u) << 15); }
    internal uint TIMA { readonly get => _word090 & 0xFFu; set => _word090 = (_word090 & ~0xFFu) | (value & 0xFFu); }
    internal uint TACTL { readonly get => (_word090 >> 8) & 0x7u; set => _word090 = (_word090 & ~0x700u) | ((value & 0x7u) << 8); }
    internal uint TIMB { readonly get => _word094 & 0xFFu; set => _word094 = (_word094 & ~0xFFu) | (value & 0xFFu); }
    internal uint TBCTL { readonly get => (_word094 >> 8) & 0x7u; set => _word094 = (_word094 & ~0x700u) | ((value & 0x7u) << 8); }
    internal uint TIMC { readonly get => _word098 & 0xFFu; set => _word098 = (_word098 & ~0xFFu) | (value & 0xFFu); }
    internal uint TCCTL { readonly get => (_word098 >> 8) & 0x7u; set => _word098 = (_word098 & ~0x700u) | ((value & 0x7u) << 8); }
    internal uint SCIEB { readonly get => _word09c & 0x7FFu; set => _word09c = (_word09c & ~0x7FFu) | (value & 0x7FFu); }
    internal uint SCIPD { readonly get => _word0a0 & 0x7FFu; set => _word0a0 = (_word0a0 & ~0x7FFu) | (value & 0x7FFu); }
    internal uint SCIRE { readonly get => _word0a4 & 0x7FFu; set => _word0a4 = (_word0a4 & ~0x7FFu) | (value & 0x7FFu); }
    internal uint SCILV0 { readonly get => _word0a8 & 0xFFu; set => _word0a8 = (_word0a8 & ~0xFFu) | (value & 0xFFu); }
    internal uint SCILV1 { readonly get => _word0ac & 0xFFu; set => _word0ac = (_word0ac & ~0xFFu) | (value & 0xFFu); }
    internal uint SCILV2 { readonly get => _word0b0 & 0xFFu; set => _word0b0 = (_word0b0 & ~0xFFu) | (value & 0xFFu); }
    internal uint MCIEB { readonly get => _word0b4 & 0x7FFu; set => _word0b4 = (_word0b4 & ~0x7FFu) | (value & 0x7FFu); }
    internal uint MCIPD { readonly get => _word0b8 & 0x7FFu; set => _word0b8 = (_word0b8 & ~0x7FFu) | (value & 0x7FFu); }
    internal uint MCIRE { readonly get => _word0bc & 0x7FFu; set => _word0bc = (_word0bc & ~0x7FFu) | (value & 0x7FFu); }
    internal uint AR { readonly get => _word400 & 0x1u; set => _word400 = (_word400 & ~0x1u) | (value & 0x1u); }
    internal uint VREG { readonly get => (_word400 >> 8) & 0x3u; set => _word400 = (_word400 & ~0x300u) | ((value & 0x3u) << 8); }
    internal uint L0_r { readonly get => _word500 & 0x1u; set => _word500 = (_word500 & ~0x1u) | (value & 0x1u); }
    internal uint L1_r { readonly get => (_word500 >> 1) & 0x1u; set => _word500 = (_word500 & ~0x2u) | ((value & 0x1u) << 1); }
    internal uint L2_r { readonly get => (_word500 >> 2) & 0x1u; set => _word500 = (_word500 & ~0x4u) | ((value & 0x1u) << 2); }
    internal uint L3_r { readonly get => (_word500 >> 3) & 0x1u; set => _word500 = (_word500 & ~0x8u) | ((value & 0x1u) << 3); }
    internal uint L4_r { readonly get => (_word500 >> 4) & 0x1u; set => _word500 = (_word500 & ~0x10u) | ((value & 0x1u) << 4); }
    internal uint L5_r { readonly get => (_word500 >> 5) & 0x1u; set => _word500 = (_word500 & ~0x20u) | ((value & 0x1u) << 5); }
    internal uint L6_r { readonly get => (_word500 >> 6) & 0x1u; set => _word500 = (_word500 & ~0x40u) | ((value & 0x1u) << 6); }
    internal uint L7_r { readonly get => (_word500 >> 7) & 0x1u; set => _word500 = (_word500 & ~0x80u) | ((value & 0x1u) << 7); }
    internal uint M0_r { readonly get => _word504 & 0x1u; set => _word504 = (_word504 & ~0x1u) | (value & 0x1u); }
    internal uint M1_r { readonly get => (_word504 >> 1) & 0x1u; set => _word504 = (_word504 & ~0x2u) | ((value & 0x1u) << 1); }
    internal uint M2_r { readonly get => (_word504 >> 2) & 0x1u; set => _word504 = (_word504 & ~0x4u) | ((value & 0x1u) << 2); }
    internal uint M3_r { readonly get => (_word504 >> 3) & 0x1u; set => _word504 = (_word504 & ~0x8u) | ((value & 0x1u) << 3); }
    internal uint M4_r { readonly get => (_word504 >> 4) & 0x1u; set => _word504 = (_word504 & ~0x10u) | ((value & 0x1u) << 4); }
    internal uint M5_r { readonly get => (_word504 >> 5) & 0x1u; set => _word504 = (_word504 & ~0x20u) | ((value & 0x1u) << 5); }
    internal uint M6_r { readonly get => (_word504 >> 6) & 0x1u; set => _word504 = (_word504 & ~0x40u) | ((value & 0x1u) << 6); }
    internal uint M7_r { readonly get => (_word504 >> 7) & 0x1u; set => _word504 = (_word504 & ~0x80u) | ((value & 0x1u) << 7); }
    internal uint RP { readonly get => (_word504 >> 8) & 0x1u; set => _word504 = (_word504 & ~0x100u) | ((value & 0x1u) << 8); }
}

[StructLayout(LayoutKind.Sequential, Size = 0x15C8)]
internal unsafe struct AicaDspData
{
    internal fixed uint COEF[128];
    internal fixed uint MADRS[64];
    private fixed byte _pad0[0x100];
    internal fixed uint MPRO[128 * 4];
    private fixed byte _pad1[0x400];
    internal AicaDspTempWords TEMP;
    internal AicaDspMemsWords MEMS;
    internal AicaDspMixsWords MIXS;
    internal fixed uint EFREG[16];
    internal fixed uint EXTS[2];
}

[StructLayout(LayoutKind.Sequential)]
internal struct AicaDspMemoryWord
{
    internal uint l;
    internal uint h;
}

[InlineArray(128)]
internal struct AicaDspTempWords
{
    private AicaDspMemoryWord _element0;
}

[InlineArray(32)]
internal struct AicaDspMemsWords
{
    private AicaDspMemoryWord _element0;
}

[InlineArray(16)]
internal struct AicaDspMixsWords
{
    private AicaDspMemoryWord _element0;
}

[StructLayout(LayoutKind.Explicit, Size = 4)]
internal struct AicaInterruptInfo
{
    [FieldOffset(0)] internal uint Full;
    [FieldOffset(0x0)] private uint _word000;

    internal uint INTON { readonly get => _word000 & 0x1u; set => _word000 = (_word000 & ~0x1u) | (value & 0x1u); }
    internal uint MIDI_IN { readonly get => (_word000 >> 3) & 0x1u; set => _word000 = (_word000 & ~0x8u) | ((value & 0x1u) << 3); }
    internal uint DMA_END { readonly get => (_word000 >> 4) & 0x1u; set => _word000 = (_word000 & ~0x10u) | ((value & 0x1u) << 4); }
    internal uint SCPU { readonly get => (_word000 >> 5) & 0x1u; set => _word000 = (_word000 & ~0x20u) | ((value & 0x1u) << 5); }
    internal uint TimerA { readonly get => (_word000 >> 6) & 0x1u; set => _word000 = (_word000 & ~0x40u) | ((value & 0x1u) << 6); }
    internal uint TimerB { readonly get => (_word000 >> 7) & 0x1u; set => _word000 = (_word000 & ~0x80u) | ((value & 0x1u) << 7); }
    internal uint TimerC { readonly get => (_word000 >> 8) & 0x1u; set => _word000 = (_word000 & ~0x100u) | ((value & 0x1u) << 8); }
    internal uint MIDI_OUT { readonly get => (_word000 >> 9) & 0x1u; set => _word000 = (_word000 & ~0x200u) | ((value & 0x1u) << 9); }
    internal uint SAMPLE_DONE { readonly get => (_word000 >> 10) & 0x1u; set => _word000 = (_word000 & ~0x400u) | ((value & 0x1u) << 10); }
}

[StructLayout(LayoutKind.Explicit, Size = 4)]
internal struct AicaDspOutputVolumeRegister
{
    [FieldOffset(0x0)] private uint _word000;

    internal uint EFPAN { readonly get => _word000 & 0x1Fu; set => _word000 = (_word000 & ~0x1Fu) | (value & 0x1Fu); }
    internal uint EFSDL { readonly get => (_word000 >> 8) & 0xFu; set => _word000 = (_word000 & ~0xF00u) | ((value & 0xFu) << 8); }
}

internal sealed unsafe class AicaTimer
{
    [StructLayout(LayoutKind.Explicit)]
    private struct AicaTimerData
    {
        [FieldOffset(0)] internal uint Data;

        internal byte Count
        {
            readonly get => (byte)Data;
            set => Data = (Data & ~0xFFu) | value;
        }

        internal int Mode
        {
            readonly get => (int)((Data >> 8) & 7);
            set => Data = (Data & ~0x700u) | (((uint)value & 7) << 8);
        }
    }

    private AicaTimerData* _data;
    private uint _id;
    internal int CStep;
    internal uint MStep;

    internal void Init(byte* registerBase, uint timer)
    {
        _data = (AicaTimerData*)&registerBase[0x2890 + timer * 4];
        _id = timer;
        MStep = 1u << _data->Mode;
        CStep = (int)MStep;
    }

    internal void StepTimer(uint samples)
    {
        do
        {
            CStep--;
            if (CStep == 0)
            {
                CStep = (int)MStep;
                _data->Count = unchecked((byte)(_data->Count + 1));
                if (_data->Count == 0)
                {
                    if (_id == 0)
                    {
                        AicaMemory.SCIPD->TimerA = 1;
                        AicaMemory.MCIPD->TimerA = 1;
                    }
                    else if (_id == 1)
                    {
                        AicaMemory.SCIPD->TimerB = 1;
                        AicaMemory.MCIPD->TimerB = 1;
                    }
                    else
                    {
                        AicaMemory.SCIPD->TimerC = 1;
                        AicaMemory.MCIPD->TimerC = 1;
                    }
                }
            }
        }
        while (--samples != 0);
    }

    internal void RegisterWrite()
    {
        uint newStep = 1u << _data->Mode;
        if (newStep != MStep)
        {
            MStep = newStep;
            CStep = (int)MStep;
        }
    }
}
