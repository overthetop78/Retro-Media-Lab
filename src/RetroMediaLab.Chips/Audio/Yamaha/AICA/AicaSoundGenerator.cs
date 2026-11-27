using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
#if CLIP_WARN
using RetroMediaLab.Chips.CPU.Hitachi.SH4;
#endif

namespace RetroMediaLab.Chips.Audio.Yamaha.AICA;

internal static unsafe class AicaSoundGenerator
{
    private static readonly int[] VolumeLut = new int[16];
    private static readonly uint[] SendLevel =
    [
        255, 14 << 3, 13 << 3, 12 << 3, 11 << 3, 10 << 3, 9 << 3, 8 << 3,
        7 << 3, 6 << 3, 5 << 3, 4 << 3, 3 << 3, 2 << 3, 1 << 3, 0 << 3
    ];
    private static readonly int[] TlLut = new int[256 + 768];

    private static readonly double[] AegAttackTime =
    [
        -1,-1,8100.0,6900.0,6000.0,4800.0,4000.0,3400.0,3000.0,2400.0,2000.0,1700.0,1500.0,
        1200.0,1000.0,860.0,760.0,600.0,500.0,430.0,380.0,300.0,250.0,220.0,190.0,150.0,130.0,110.0,95.0,
        76.0,63.0,55.0,47.0,38.0,31.0,27.0,24.0,19.0,15.0,13.0,12.0,9.4,7.9,6.8,6.0,4.7,3.8,3.4,3.0,2.4,
        2.0,1.8,1.6,1.3,1.1,0.93,0.85,0.65,0.53,0.44,0.40,0.35,0.0,0.0
    ];

    private static readonly double[] AegDsrTime =
    [
        -1,-1,118200.0,101300.0,88600.0,70900.0,59100.0,50700.0,44300.0,35500.0,29600.0,25300.0,22200.0,17700.0,
        14800.0,12700.0,11100.0,8900.0,7400.0,6300.0,5500.0,4400.0,3700.0,3200.0,2800.0,2200.0,1800.0,1600.0,1400.0,1100.0,
        920.0,790.0,690.0,550.0,460.0,390.0,340.0,270.0,230.0,200.0,170.0,140.0,110.0,98.0,85.0,68.0,57.0,49.0,43.0,34.0,
        28.0,25.0,22.0,18.0,14.0,12.0,11.0,8.5,7.1,6.1,5.4,4.3,3.6,3.1
    ];

    private static readonly float[] PlfosScale =
    [
        0f, 3.61f, 7.22f, 14.44f, 28.88f, 57.75f, 115.5f, 231f
    ];
    private static readonly int[,] PlfoScales = new int[8, 256];

    private const int EgStepBits = 16;
    private const int AegAttackShift = 16;

    private static readonly uint[] AegAttackSamples = new uint[64];
    private static readonly uint[] AegDsrSamples = new uint[64];
    private static readonly uint[] FegSamples = new uint[64];

    private static readonly string[] StreamNames =
    [
        "0: 16-bit PCM (two's complement format)",
        "1: 8-bit PCM (two's complement format)",
        "2: 4-bit ADPCM (Yamaha format)",
        "3: 4-bit ADPCM long stream"
    ];

    private static readonly int[] AdpcmQs =
    [
        0x0e6, 0x0e6, 0x0e6, 0x0e6, 0x133, 0x199, 0x200, 0x266,
    ];

    private static readonly int[] AdpcmScale =
    [
        1, 3, 5, 7, 9, 11, 13, 15,
    ];

    private static readonly int[] QTable =
    [
        2048,  1536,  1024,   512,
        0,  -256,  -512,  -768,
        -1024, -1280, -1536, -1792,
        -2048, -2176, -2304, -2432,
        -2560, -2688, -2816, -2944,
        -3072, -3136, -3200, -3264,
        -3328, -3392, -3456, -3520,
        -3584, -3648, -3712, -3776
    ];

    private const uint RandomFactor = 0x41c64e6d;
    private const uint RandomTerm = 0x3039;

    private static int FPs(int a, int bits) => a >> bits;

    private static int FPMul(int a, int b, int bits) => (a * b) >> bits;

    private static uint FPMul(uint a, uint b, int bits) => (a * b) >> bits;

    private static long FPMul(long a, long b, int bits) => (a * b) >> bits;

    private static void VolumePan(int value, uint volume, uint pan, ref int left, ref int right)
    {
        int temp = FPMul(value, VolumeLut[volume], 15);
        int scaled = FPMul(temp, VolumeLut[0xf - (pan & 0xf)], 15);
        if ((pan & 0x10) != 0)
        {
            left += temp;
            right += scaled;
        }
        else
        {
            left += scaled;
            right += temp;
        }
    }

    private sealed class VmuBeeper
    {
        private bool _active;
        private int _attenuation = 256;
        private Fp22_10 _waveIndex;
        private int _waveStep;

        private static readonly int[] Wave =
        [
            503, -3519,
            -7540, -8214, -8209, -8214, -8209, -5199, -1172,  2843,
            6873,  8207,  8214,  8209,  8212,  5866,  1840, -2175,
            -6203, -8210, -8215, -8209, -8214, -6533, -2516,  1507,
            5526,  8212,  8210,  8212,  8210,  7206,  3187,  -841,
            -4856, -8215, -8209, -8214, -8208, -7882, -3850,   162,
            4187,  8213,  8208,  8213,  8209,  8211,  4525
        ];

        internal void Init()
        {
            _active = false;
            _attenuation = 256;
            _waveIndex.Full = 0;
            _waveStep = 0;
        }

        internal void Update(int period, int on)
        {
            if (on == 0 || period == 0 || on >= period)
            {
                _active = false;
            }
            else
            {
                _active = true;
                int frequency = 6000000 / 6 / period;
                _waveStep = frequency * 1024 / 2698;
            }
        }

        internal int GetSample()
        {
            if (!_active && _attenuation >= 256)
                return 0;

            _waveIndex.Full += unchecked((uint)_waveStep);
            _waveIndex.Ip %= (uint)Wave.Length;
            uint nextIndex = (_waveIndex.Ip + 1) % (uint)Wave.Length;
            int sample = (FPMul(Wave[_waveIndex.Ip], (int)(1024 - _waveIndex.Fp), 10)
                + FPMul(Wave[nextIndex], (int)_waveIndex.Fp, 10)) * 2;
            sample = FPMul(sample, TlLut[_attenuation], 15);
            if (_active)
                _attenuation = Math.Max(_attenuation - 2, 0);
            else
                _attenuation = Math.Min(_attenuation + 2, 256);
            return sample;
        }

        internal void Serialize(BinaryWriter writer)
        {
            writer.Write(_active);
            writer.Write(_attenuation);
            writer.Write(_waveIndex.Full);
            writer.Write(_waveStep);
        }

        internal void Deserialize(BinaryReader reader, int version)
        {
            if (version < SaveVersion49)
            {
                if (version >= SaveVersion22)
                {
                    reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadInt32();
                }
                Init();
            }
            else
            {
                _active = reader.ReadBoolean();
                _attenuation = reader.ReadInt32();
                _waveIndex.Full = reader.ReadUInt32();
                _waveStep = reader.ReadInt32();
            }
        }
    }

    private static readonly VmuBeeper Beep = new();

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct ChannelCommonData
    {
        private uint _word0;
        private uint _word1;
        private uint _word2;
        private uint _word3;
        private uint _word4;
        private uint _word5;
        private uint _word6;
        private uint _word7;
        private uint _word8;
        private uint _word9;
        private uint _word10;
        private uint _word11;
        private uint _word12;
        private uint _word13;
        private uint _word14;
        private uint _word15;
        private uint _word16;
        private uint _word17;

        internal uint SA_hi
        {
            readonly get => (_word0 >> 0) & 0x7fu;
            set => _word0 = (_word0 & ~(0x7fu << 0)) | ((value & 0x7fu) << 0);
        }

        internal uint PCMS
        {
            readonly get => (_word0 >> 7) & 0x3u;
            set => _word0 = (_word0 & ~(0x3u << 7)) | ((value & 0x3u) << 7);
        }

        internal uint LPCTL
        {
            readonly get => (_word0 >> 9) & 0x1u;
            set => _word0 = (_word0 & ~(0x1u << 9)) | ((value & 0x1u) << 9);
        }

        internal uint SSCTL
        {
            readonly get => (_word0 >> 10) & 0x1u;
            set => _word0 = (_word0 & ~(0x1u << 10)) | ((value & 0x1u) << 10);
        }

        internal uint KYONB
        {
            readonly get => (_word0 >> 14) & 0x1u;
            set => _word0 = (_word0 & ~(0x1u << 14)) | ((value & 0x1u) << 14);
        }

        internal uint KYONEX
        {
            readonly get => (_word0 >> 15) & 0x1u;
            set => _word0 = (_word0 & ~(0x1u << 15)) | ((value & 0x1u) << 15);
        }

        internal uint SA_low
        {
            readonly get => (_word1 >> 0) & 0xffffu;
            set => _word1 = (_word1 & ~(0xffffu << 0)) | ((value & 0xffffu) << 0);
        }

        internal uint LSA
        {
            readonly get => (_word2 >> 0) & 0xffffu;
            set => _word2 = (_word2 & ~(0xffffu << 0)) | ((value & 0xffffu) << 0);
        }

        internal uint LEA
        {
            readonly get => (_word3 >> 0) & 0xffffu;
            set => _word3 = (_word3 & ~(0xffffu << 0)) | ((value & 0xffffu) << 0);
        }

        internal uint AR
        {
            readonly get => (_word4 >> 0) & 0x1fu;
            set => _word4 = (_word4 & ~(0x1fu << 0)) | ((value & 0x1fu) << 0);
        }

        internal uint D1R
        {
            readonly get => (_word4 >> 6) & 0x1fu;
            set => _word4 = (_word4 & ~(0x1fu << 6)) | ((value & 0x1fu) << 6);
        }

        internal uint D2R
        {
            readonly get => (_word4 >> 11) & 0x1fu;
            set => _word4 = (_word4 & ~(0x1fu << 11)) | ((value & 0x1fu) << 11);
        }

        internal uint RR
        {
            readonly get => (_word5 >> 0) & 0x1fu;
            set => _word5 = (_word5 & ~(0x1fu << 0)) | ((value & 0x1fu) << 0);
        }

        internal uint DL
        {
            readonly get => (_word5 >> 5) & 0x1fu;
            set => _word5 = (_word5 & ~(0x1fu << 5)) | ((value & 0x1fu) << 5);
        }

        internal uint KRS
        {
            readonly get => (_word5 >> 10) & 0xfu;
            set => _word5 = (_word5 & ~(0xfu << 10)) | ((value & 0xfu) << 10);
        }

        internal uint LPSLNK
        {
            readonly get => (_word5 >> 14) & 0x1u;
            set => _word5 = (_word5 & ~(0x1u << 14)) | ((value & 0x1u) << 14);
        }

        internal uint FNS
        {
            readonly get => (_word6 >> 0) & 0x3ffu;
            set => _word6 = (_word6 & ~(0x3ffu << 0)) | ((value & 0x3ffu) << 0);
        }

        internal uint OCT
        {
            readonly get => (_word6 >> 11) & 0xfu;
            set => _word6 = (_word6 & ~(0xfu << 11)) | ((value & 0xfu) << 11);
        }

        internal uint ALFOS
        {
            readonly get => (_word7 >> 0) & 0x7u;
            set => _word7 = (_word7 & ~(0x7u << 0)) | ((value & 0x7u) << 0);
        }

        internal uint ALFOWS
        {
            readonly get => (_word7 >> 3) & 0x3u;
            set => _word7 = (_word7 & ~(0x3u << 3)) | ((value & 0x3u) << 3);
        }

        internal uint PLFOS
        {
            readonly get => (_word7 >> 5) & 0x7u;
            set => _word7 = (_word7 & ~(0x7u << 5)) | ((value & 0x7u) << 5);
        }

        internal uint PLFOWS
        {
            readonly get => (_word7 >> 8) & 0x3u;
            set => _word7 = (_word7 & ~(0x3u << 8)) | ((value & 0x3u) << 8);
        }

        internal uint LFOF
        {
            readonly get => (_word7 >> 10) & 0x1fu;
            set => _word7 = (_word7 & ~(0x1fu << 10)) | ((value & 0x1fu) << 10);
        }

        internal uint LFORE
        {
            readonly get => (_word7 >> 15) & 0x1u;
            set => _word7 = (_word7 & ~(0x1u << 15)) | ((value & 0x1u) << 15);
        }

        internal uint ISEL
        {
            readonly get => (_word8 >> 0) & 0xfu;
            set => _word8 = (_word8 & ~(0xfu << 0)) | ((value & 0xfu) << 0);
        }

        internal uint IMXL
        {
            readonly get => (_word8 >> 4) & 0xfu;
            set => _word8 = (_word8 & ~(0xfu << 4)) | ((value & 0xfu) << 4);
        }

        internal uint DIPAN
        {
            readonly get => (_word9 >> 0) & 0x1fu;
            set => _word9 = (_word9 & ~(0x1fu << 0)) | ((value & 0x1fu) << 0);
        }

        internal uint DISDL
        {
            readonly get => (_word9 >> 8) & 0xfu;
            set => _word9 = (_word9 & ~(0xfu << 8)) | ((value & 0xfu) << 8);
        }

        internal uint Q
        {
            readonly get => (_word10 >> 0) & 0x1fu;
            set => _word10 = (_word10 & ~(0x1fu << 0)) | ((value & 0x1fu) << 0);
        }

        internal uint LPOFF
        {
            readonly get => (_word10 >> 5) & 0x1u;
            set => _word10 = (_word10 & ~(0x1u << 5)) | ((value & 0x1u) << 5);
        }

        internal uint VOFF
        {
            readonly get => (_word10 >> 6) & 0x1u;
            set => _word10 = (_word10 & ~(0x1u << 6)) | ((value & 0x1u) << 6);
        }

        internal uint TL
        {
            readonly get => (_word10 >> 8) & 0xffu;
            set => _word10 = (_word10 & ~(0xffu << 8)) | ((value & 0xffu) << 8);
        }

        internal uint FLV0
        {
            readonly get => (_word11 >> 0) & 0x1fffu;
            set => _word11 = (_word11 & ~(0x1fffu << 0)) | ((value & 0x1fffu) << 0);
        }

        internal uint FLV1
        {
            readonly get => (_word12 >> 0) & 0x1fffu;
            set => _word12 = (_word12 & ~(0x1fffu << 0)) | ((value & 0x1fffu) << 0);
        }

        internal uint FLV2
        {
            readonly get => (_word13 >> 0) & 0x1fffu;
            set => _word13 = (_word13 & ~(0x1fffu << 0)) | ((value & 0x1fffu) << 0);
        }

        internal uint FLV3
        {
            readonly get => (_word14 >> 0) & 0x1fffu;
            set => _word14 = (_word14 & ~(0x1fffu << 0)) | ((value & 0x1fffu) << 0);
        }

        internal uint FLV4
        {
            readonly get => (_word15 >> 0) & 0x1fffu;
            set => _word15 = (_word15 & ~(0x1fffu << 0)) | ((value & 0x1fffu) << 0);
        }

        internal uint FD1R
        {
            readonly get => (_word16 >> 0) & 0x1fu;
            set => _word16 = (_word16 & ~(0x1fu << 0)) | ((value & 0x1fu) << 0);
        }

        internal uint FAR
        {
            readonly get => (_word16 >> 8) & 0x1fu;
            set => _word16 = (_word16 & ~(0x1fu << 8)) | ((value & 0x1fu) << 8);
        }

        internal uint FRR
        {
            readonly get => (_word17 >> 0) & 0x1fu;
            set => _word17 = (_word17 & ~(0x1fu << 0)) | ((value & 0x1fu) << 0);
        }

        internal uint FD2R
        {
            readonly get => (_word17 >> 8) & 0x1fu;
            set => _word17 = (_word17 & ~(0x1fu << 8)) | ((value & 0x1fu) << 8);
        }
    }

    private enum EgState
    {
        Attack = 0,
        Decay1 = 1,
        Decay2 = 2,
        Release = 3
    }

    private enum LfoType
    {
        Sawtooth,
        Square,
        Triangle,
        Random
    }

    private enum PcmType
    {
        Pcm16 = 0,
        Pcm8 = 1,
        Adpcm = 2,
        AdpcmStream = 3,
        Noise = 4
    }

    private struct LoopState
    {
        internal uint Lsa;
        internal uint Lea;
        internal uint AlignedLea;
        internal byte Looped;
    }

    private struct AdpcmState
    {
        internal int LastQuant;
        internal int LoopStartQuant;
        internal int LoopStartPreviousSample;
        internal bool InLoop;

        internal void Reset(Channel channel)
        {
            LastQuant = 127;
            LoopStartQuant = 0;
            LoopStartPreviousSample = 0;
            InLoop = false;
            channel.S0 = 0;
        }
    }

    private struct VolumeMix
    {
        internal uint DlAtt;
        internal uint DrAtt;
        internal uint DspAtt;
        internal int* DspOut;
    }

    private struct AegEnvelope
    {
        internal int Value;
        internal EgState State;
        internal uint AttackRate;
        internal uint Decay1Rate;
        internal uint Decay2Value;
        internal uint Decay2Rate;
        internal uint ReleaseRate;

        internal readonly int GetValue() => Value >> EgStepBits;
        internal void SetValue(uint value) => Value = (int)(value << EgStepBits);
    }

    private struct FegEnvelope
    {
        internal uint Value;
        internal EgState State;
        internal int Previous1;
        internal int Previous2;
        internal int Fraction;
        internal int Q;
        internal uint AttackRate;
        internal uint Decay1Rate;
        internal uint Decay2Rate;
        internal uint ReleaseRate;
        internal bool Active;

        internal readonly uint GetValue() => Value >> EgStepBits;
        internal void SetValue(uint value) => Value = value << EgStepBits;
    }

    private sealed class Lfo
    {
        internal uint Counter;
        internal uint StartValue;
        internal byte State;
        internal byte Alfo;
        internal byte AlfoShift;
        internal Fp22_10 PlfoStep;
        internal uint PlfoScale;
        internal LfoType AlfoType;
        internal LfoType PlfoType;
        internal Action<Lfo> AlfoCalc = null!;
        internal Action<Lfo> PlfoCalc = null!;

        internal static readonly Action<Lfo>[] AlfoWaveCalc = new Action<Lfo>[4];
        internal static readonly Action<Lfo>[] PlfoWaveCalc = new Action<Lfo>[4];

        internal void Step()
        {
            Counter--;
            if (Counter == 0)
            {
                State++;
                Counter = StartValue;
                AlfoCalc(this);
                PlfoCalc(this);
            }
            else
            {
                if (AlfoType == LfoType.Random)
                    AlfoCalc(this);
                if (PlfoType == LfoType.Random)
                    PlfoCalc(this);
            }
        }

        internal void Reset()
        {
            State = 0;
            Counter = StartValue;
            AlfoCalc(this);
            PlfoCalc(this);
        }

        internal void SetStartValue(uint value) => StartValue = value;

        internal void CalcAlfo(LfoType type)
        {
            uint value = 0;
            switch (type)
            {
                case LfoType.Sawtooth:
                    value = State;
                    break;
                case LfoType.Square:
                    value = (State & 0x80) != 0 ? 255u : 0u;
                    break;
                case LfoType.Triangle:
                    value = (uint)((State & 0x7f) ^ ((State & 0x80) != 0 ? 0x7f : 0));
                    value <<= 1;
                    break;
                case LfoType.Random:
                    value = (State * RandomFactor + RandomTerm) & 0xff;
                    break;
            }
            Alfo = (byte)(value >> AlfoShift);
        }

        internal void CalcPlfo(LfoType type)
        {
            uint value = 0;
            switch (type)
            {
                case LfoType.Sawtooth:
                    value = State;
                    break;
                case LfoType.Square:
                    value = (State & 0x80) != 0 ? 0xffu : 0u;
                    break;
                case LfoType.Triangle:
                    value = (uint)((State & 0x7f) ^ ((State & 0x80) != 0 ? 0x7f : 0));
                    value <<= 1;
                    break;
                case LfoType.Random:
                    value = (State * RandomFactor + RandomTerm) & 0xff;
                    break;
            }
            PlfoStep.Full = (uint)PlfoScales[(int)PlfoScale, (byte)value];
        }
    }

    private sealed class Channel
    {
        internal ChannelCommonData* Ccd;
        internal uint Sa;
        internal uint Ca;
        internal Fp22_10 StepPosition;
        internal uint UpdateRate;
        internal int S0;
        internal int S1;
        internal LoopState Loop;
        internal AdpcmState Adpcm;
        internal uint NoiseState;
        internal VolumeMix VolMix;
        private Action<Channel> _stepAeg = null!;
        private Action<Channel> _stepFeg = null!;
        private Action<Channel> _stepStream = null!;
        private Action<Channel> _stepStreamInitial = null!;
        internal AegEnvelope Aeg;
        internal FegEnvelope Feg;
        internal readonly Lfo Lfo = new();
        internal bool Enabled;
        internal bool Quiet;
        internal int ChannelNumber;

        internal static readonly Action<Channel>[,,] StreamStepLut = new Action<Channel>[5, 2, 2];
        internal static readonly Action<Channel>[] StreamInitialStepLut = new Action<Channel>[5];
        internal static readonly Action<Channel>[] AegStepLut = new Action<Channel>[4];
        internal static readonly Action<Channel>[] FegStepLut = new Action<Channel>[4];

        internal void Init(int channel, byte* registers)
        {
            Ccd = (ChannelCommonData*)(registers + channel * 0x80);
            ChannelNumber = channel;
            Quiet = true;
            for (uint offset = 0; offset < 0x80; offset += 2)
                RegWrite(offset, 2);
            Quiet = false;
            Disable();
        }

        internal void Disable()
        {
            Enabled = false;
            SetAegState(EgState.Release);
            Aeg.SetValue(0x3ff);
            Ca = 0;
        }

        private void Enable() => Enabled = true;

        private int InterpolateSample()
        {
            int fraction = (int)StepPosition.Fp;
            int sample = FPMul(S0, 1024 - fraction, 10);
            sample += FPMul(S1, fraction, 10);
            return sample;
        }

        private int LowPassFilter(int sample)
        {
            if (!Feg.Active)
                return sample;

            const int coefficientBits = 30;
            uint value = Feg.GetValue();
            int exponent = (int)(value >> 9);
            uint mantissa = (value & 0x1ff) | 0x200;
            ulong a0 = ((ulong)mantissa << 30) >> ((15 - exponent) * 2);
            a0 *= (mantissa - 1) / 8;
            a0 >>= 47 - coefficientBits;
            long f = ((long)mantissa << exponent) << (coefficientBits - 25);
            f += Feg.Q * f / 4096;
            long b1 = 128L * 1024 * (1 << (coefficientBits - 16)) - (f + (long)a0);
            long b2 = 64L * 1024 * (1 << (coefficientBits - 16)) - f;
            if (exponent == 0)
                Feg.Fraction = 0;

            long mac = -(long)a0 * sample + b1 * Feg.Previous1 - b2 * Feg.Previous2 - Feg.Fraction;
            sample = (int)(mac >> coefficientBits);
            Feg.Fraction = (int)(((long)sample << coefficientBits) - mac);
            Feg.Previous2 = Feg.Previous1;
            sample = Math.Clamp(sample, -512 * 1024, 512 * 1024 - 1);
            Feg.Previous1 = sample;
            return sample;
        }

        private bool Step(out int left, out int right, out int dsp)
        {
            if (!Enabled)
            {
                left = right = dsp = 0;
                return false;
            }

            int sample = InterpolateSample() << 4;
            sample = LowPassFilter(sample);

            uint offsetAttenuation;
            if (Ccd->VOFF == 1)
            {
                offsetAttenuation = 0;
            }
            else
            {
                offsetAttenuation = unchecked((uint)(Lfo.Alfo + (Aeg.GetValue() >> 2)));
                offsetAttenuation = Math.Min(offsetAttenuation, 255u);
            }

            uint maxAttenuation = ((16 << 4) - 1) - offsetAttenuation;
            uint dl = Math.Min(VolMix.DlAtt, maxAttenuation);
            uint dr = Math.Min(VolMix.DrAtt, maxAttenuation);
            uint ds = Math.Min(VolMix.DspAtt, maxAttenuation);

            left = (int)FPMul((long)sample, TlLut[offsetAttenuation + dl], 19);
            right = (int)FPMul((long)sample, TlLut[offsetAttenuation + dr], 19);
            dsp = (int)FPMul((long)sample, TlLut[offsetAttenuation + ds], 15);

#if CLIP_WARN
            Sh4HostRuntime.Verify((short)left == left);
            Sh4HostRuntime.Verify((short)right == right);
            Sh4HostRuntime.Verify((dsp << 12) >> 12 == dsp);
            Sh4HostRuntime.Verify((long)sample * left >= 0);
            Sh4HostRuntime.Verify((long)sample * right >= 0);
            Sh4HostRuntime.Verify((long)sample * dsp >= 0);
#endif

            _stepAeg(this);
            if (Enabled)
            {
                _stepFeg(this);
                _stepStream(this);
                Lfo.Step();
            }
            return true;
        }

        internal void Step(ref int mixLeft, ref int mixRight)
        {
            Step(out int left, out int right, out int dsp);
            *VolMix.DspOut += dsp;
            if (left + right == 0 && !AicaConfiguration.DspEnabled)
                left = right = dsp >> 4;

            mixLeft += left;
            mixRight += right;
        }

        internal static void StepAll(ref int mixLeft, ref int mixRight)
        {
            foreach (Channel channel in Channels)
                channel.Step(ref mixLeft, ref mixRight);
        }

        internal void SetAegState(EgState state)
        {
            _stepAeg = AegStepLut[(int)state];
            Aeg.State = state;
            if (state == EgState.Release)
                Ccd->KYONB = 0;
        }

        internal void SetFegState(EgState state)
        {
            _stepFeg = FegStepLut[(int)state];
            Feg.State = state;
            if (state == EgState.Attack)
            {
                Feg.SetValue(Ccd->FLV0);
                Feg.Previous1 = 0;
                Feg.Previous2 = 0;
                Feg.Fraction = 0;
            }
        }

        private void KeyOn()
        {
            if (Aeg.State != EgState.Release)
                return;

            Enable();
            SetAegState(EgState.Attack);
            Aeg.SetValue(0x280);
            SetFegState(EgState.Attack);
            Ca = 0;
            StepPosition.Full = 0;
            Loop.Looped = 0;
            Adpcm.Reset(this);
            _stepStreamInitial(this);
            Debug.WriteLine($"[{ChannelNumber}] KEY_ON {StreamNames[Ccd->PCMS]} @ {44100.0 * UpdateRate / 1024:g} Hz, loop {Ccd->LPCTL} - AEG AR {Ccd->AR} DC1R {Ccd->D1R} DC2V {Ccd->DL << 5} DC2R {Ccd->D2R} RR {Ccd->RR} - KRS {Ccd->KRS} OCT {Ccd->OCT} FNS {Ccd->FNS} - SA {Sa:x} LSA {Ccd->LSA:x} LEA {Ccd->LEA:x}");
        }

        private void KeyOff()
        {
            if (Aeg.State == EgState.Release)
                return;
            Debug.WriteLine($"[{ChannelNumber}] KEY_OFF -> Release");
            SetAegState(EgState.Release);
            SetFegState(EgState.Release);
        }

        internal void UpdateStreamStep()
        {
            PcmType format = (PcmType)Ccd->PCMS;
            if (Ccd->SSCTL != 0)
                format = PcmType.Noise;

            _stepStream = StreamStepLut[(int)format, (int)Ccd->LPCTL, (int)Ccd->LPSLNK];
            _stepStreamInitial = StreamInitialStepLut[(int)format];
        }

        private void UpdateSa()
        {
            Sa = (Ccd->SA_hi << 16) | Ccd->SA_low;
            if (Ccd->PCMS == (uint)PcmType.Pcm16)
                Sa &= ~1u;
        }

        internal void UpdateLoop()
        {
            Loop.Lsa = Ccd->LSA;
            Loop.Lea = Ccd->LEA;
            Loop.AlignedLea = (Loop.Lea + 3) & ~3u;
        }

        private uint EffectiveEnvelopeRate(uint rate)
        {
            uint effectiveRate = rate * 2;
            if (Ccd->KRS < 0xf)
            {
                effectiveRate += (Ccd->FNS >> 9) & 1;
                effectiveRate += (uint)Math.Max(0, ((int)Ccd->KRS + ((int)Ccd->OCT ^ 8) - 8) * 2);
            }
            return Math.Min(effectiveRate, 0x3fu);
        }

        internal void UpdateAeg()
        {
            Aeg.AttackRate = AegAttackSamples[EffectiveEnvelopeRate(Ccd->AR)];
            Aeg.Decay1Rate = AegDsrSamples[EffectiveEnvelopeRate(Ccd->D1R)];
            Aeg.Decay2Value = Ccd->DL << 5;
            Aeg.Decay2Rate = AegDsrSamples[EffectiveEnvelopeRate(Ccd->D2R)];
            Aeg.ReleaseRate = AegDsrSamples[EffectiveEnvelopeRate(Ccd->RR)];
        }

        internal void UpdatePitch()
        {
            int octave = (int)Ccd->OCT;
            uint rate = 1024 | Ccd->FNS;
            if ((octave & 8) != 0)
                rate >>= 16 - octave;
            else
                rate <<= octave;
            UpdateRate = rate;
        }

        internal void UpdateLfo(bool derivedState)
        {
            Lfo.AlfoType = (LfoType)Ccd->ALFOWS;
            Lfo.PlfoType = (LfoType)Ccd->PLFOWS;
            if (Lfo.AlfoType == LfoType.Random && Lfo.PlfoType == LfoType.Random)
            {
                Lfo.SetStartValue(1);
                Lfo.Counter = 1;
            }
            else
            {
                int n = (int)Ccd->LFOF;
                int s = n >> 2;
                int m = (~n) & 3;
                int g = 128 >> s;
                int l = (g - 1) << 2;
                int o = l + g * (m + 1);
                Lfo.SetStartValue((uint)o);
                if (!derivedState)
                    Lfo.Counter = (uint)o;
            }

            Lfo.AlfoShift = (byte)(8 - Ccd->ALFOS);
            Lfo.AlfoCalc = AicaSoundGenerator.Lfo.AlfoWaveCalc[Ccd->ALFOWS];
            Lfo.PlfoCalc = AicaSoundGenerator.Lfo.PlfoWaveCalc[Ccd->PLFOWS];
            Lfo.PlfoScale = Ccd->PLFOS;

            if (Ccd->LFORE != 0 && !derivedState)
                Lfo.Reset();
            else
            {
                Lfo.AlfoCalc(Lfo);
                Lfo.PlfoCalc(Lfo);
            }
            if (Ccd->ALFOS != 0)
                Debug.WriteLine($"[{ChannelNumber}] ALFO LFOF {Ccd->LFOF} wave {Ccd->ALFOWS} scale {Ccd->ALFOS}");
            if (Ccd->PLFOS != 0)
                Debug.WriteLine($"[{ChannelNumber}] PLFO LFOF {Ccd->LFOF} wave {Ccd->PLFOWS} scale {Ccd->PLFOS}");
        }

        internal void UpdateDspMix()
        {
            VolMix.DspOut = ((AicaDsp.DspState*)AicaDsp.StateAddress)->MIXS + Ccd->ISEL;
        }

        internal void UpdateAttenuations()
        {
            uint total = Ccd->VOFF != 0 ? 0 : Ccd->TL;
            uint full = total + SendLevel[Ccd->DISDL];
            uint pan = full + SendLevel[(~Ccd->DIPAN) & 0xf];
            if ((Ccd->DIPAN & 0x10) != 0)
            {
                VolMix.DlAtt = full;
                VolMix.DrAtt = pan;
            }
            else
            {
                VolMix.DlAtt = pan;
                VolMix.DrAtt = full;
            }
            VolMix.DspAtt = total + SendLevel[Ccd->IMXL];
        }

        internal void UpdateFeg()
        {
            Feg.Active = Ccd->LPOFF == 0
                && (Ccd->FLV0 < 0x1ff8 || Ccd->FLV1 < 0x1ff8
                    || Ccd->FLV2 < 0x1ff8 || Ccd->FLV3 < 0x1ff8
                    || Ccd->FLV4 < 0x1ff8 || Ccd->Q != 4);
            if (!Feg.Active)
                return;
            if (!Quiet)
                Debug.WriteLine($"FEG active channel {ChannelNumber} Q {Ccd->Q} FLV: {Ccd->FLV0:D4} {Ccd->FLV1:D4} {Ccd->FLV2:D4} {Ccd->FLV3:D4} {Ccd->FLV4:D4} AR {Ccd->FAR:D2} FD1R {Ccd->FD1R:D2} FD2R {Ccd->FD2R:D2} FRR {Ccd->FRR:D2}");
            Feg.Q = QTable[Ccd->Q];
            Feg.AttackRate = FegSamples[EffectiveEnvelopeRate(Ccd->FAR)];
            Feg.Decay1Rate = FegSamples[EffectiveEnvelopeRate(Ccd->FD1R)];
            Feg.Decay2Rate = FegSamples[EffectiveEnvelopeRate(Ccd->FD2R)];
            Feg.ReleaseRate = FegSamples[EffectiveEnvelopeRate(Ccd->FRR)];
        }

        internal void RegWrite(uint offset, int size)
        {
            switch (offset)
            {
                case 0x00:
                case 0x01:
                    UpdateStreamStep();
                    if (offset == 0 || size == 2)
                        UpdateSa();
                    if ((offset == 1 || size == 2) && Ccd->KYONEX != 0)
                    {
                        Ccd->KYONEX = 0;
                        foreach (Channel channel in Channels)
                        {
                            if (channel.Ccd->KYONB != 0)
                                channel.KeyOn();
                            else
                                channel.KeyOff();
                        }
                    }
                    break;

                case 0x04:
                case 0x05:
                    UpdateSa();
                    break;

                case 0x08:
                case 0x09:
                case 0x0C:
                case 0x0D:
                    UpdateLoop();
                    break;

                case 0x10:
                case 0x11:
                    UpdateAeg();
                    break;

                case 0x14:
                case 0x15:
                    UpdateStreamStep();
                    UpdateAeg();
                    break;

                case 0x18:
                case 0x19:
                    UpdatePitch();
                    UpdateAeg();
                    UpdateFeg();
                    break;

                case 0x1C:
                case 0x1D:
                    UpdateLfo(false);
                    break;

                case 0x20:
                    UpdateDspMix();
                    UpdateAttenuations();
                    break;

                case 0x24:
                case 0x25:
                    UpdateAttenuations();
                    break;

                case 0x28:
                case 0x29:
                    if (size == 2 || offset == 0x28)
                        UpdateFeg();
                    UpdateAttenuations();
                    break;

                case 0x2C:
                case 0x2D:
                case 0x30:
                case 0x31:
                case 0x34:
                case 0x35:
                case 0x38:
                case 0x39:
                case 0x3C:
                case 0x3D:
                case 0x40:
                case 0x41:
                case 0x44:
                case 0x45:
                    UpdateFeg();
                    break;
            }
        }

        internal static void InitAll()
        {
            for (int index = 0; index < Channels.Length; index++)
                Channels[index].Init(index, AicaMemory.Registers);
        }

        internal void StepDecodeSampleInitial(PcmType format) => StepDecodeSample(format, true, 0);

        private void StepDecodeSample(PcmType format, bool last, uint address)
        {
            if (!last && (format is PcmType.Pcm16 or PcmType.Pcm8 or PcmType.Noise))
                return;

            uint nextAddress = address + 1;
            uint lea = format == PcmType.AdpcmStream ? Loop.AlignedLea : Loop.Lea;
            if (nextAddress >= lea && lea > Loop.Lsa)
                nextAddress = Loop.Lsa;

            int current = 0;
            int next = 0;
            switch (format)
            {
                case PcmType.Noise:
                    NoiseState = NoiseState * RandomFactor + RandomTerm;
                    current = (int)NoiseState;
                    current >>= 16;
                    NoiseState = NoiseState * RandomFactor + RandomTerm;
                    next = (int)NoiseState;
                    next >>= 16;
                    break;

                case PcmType.Pcm16:
                    current = ReadAicaRam<short>(Sa + address * 2);
                    next = ReadAicaRam<short>(Sa + nextAddress * 2);
                    break;

                case PcmType.Pcm8:
                    current = ReadAicaRam<sbyte>(Sa + address) << 8;
                    next = ReadAicaRam<sbyte>(Sa + nextAddress) << 8;
                    break;

                case PcmType.Adpcm:
                case PcmType.AdpcmStream:
                {
                    byte ad1 = ReadAicaRam<byte>(Sa + (address >> 1));
                    byte ad2 = ReadAicaRam<byte>(Sa + (nextAddress >> 1));

                    ad1 >>= (int)((address & 1) * 4);
                    ad2 >>= (int)((nextAddress & 1) * 4);

                    ad1 &= 0xf;
                    ad2 &= 0xf;

                    int q = Adpcm.LastQuant;
                    if (address == Loop.Lsa)
                    {
                        if (!Adpcm.InLoop)
                        {
                            Adpcm.InLoop = true;
                            Adpcm.LoopStartQuant = q;
                            Adpcm.LoopStartPreviousSample = S0;
                        }
                        else if (format == PcmType.Adpcm)
                        {
                            q = Adpcm.LoopStartQuant;
                            S0 = Adpcm.LoopStartPreviousSample;
                        }
                    }
                    current = DecodeAdpcm(ad1, S0, ref q);
                    Adpcm.LastQuant = q;
                    if (last)
                    {
                        int previous = current;
                        if (format == PcmType.Adpcm && nextAddress == Loop.Lsa && Adpcm.InLoop)
                        {
                            q = Adpcm.LoopStartQuant;
                            previous = Adpcm.LoopStartPreviousSample;
                        }
                        next = DecodeAdpcm(ad2, previous, ref q);
                    }
                    else
                    {
                        next = 0;
                    }
                    break;
                }
            }
            S0 = current;
            S1 = next;
        }

        internal void StreamStep(PcmType format, bool loopControl, bool loopLink)
        {
            StepPosition.Full += FPMul(UpdateRate, Lfo.PlfoStep.Full, 10);
            Fp22_10 position = StepPosition;
            StepPosition.Ip = 0;

            while (position.Ip > 0)
            {
                position.Ip--;

                uint address = Ca + 1;
                uint lea = format == PcmType.AdpcmStream ? Loop.AlignedLea : Loop.Lea;

                if (loopLink)
                {
                    if (Aeg.State == EgState.Attack && address >= Loop.Lsa)
                    {
                        Debug.WriteLine($"[{ChannelNumber}]LPSLNK : Switching to EG_Decay1 {Aeg.GetValue():X}");
                        SetAegState(EgState.Decay1);
                    }
                }

                if (address >= lea)
                {
                    if (Loop.Lsa > lea)
                    {
                        if (address >= Loop.Lsa)
                        {
                            Loop.Looped = 1;
                            address = 0;
                            Disable();
                        }
                    }
                    else
                    {
                        Loop.Looped = 1;
                        if (!loopControl)
                        {
                            address = 0;
                            Disable();
                        }
                        else
                        {
                            address = Loop.Lsa;
                            Debug.WriteLine($"[{ChannelNumber}]LPCTL : Looping LSA {Loop.Lsa:x} LEA {Loop.Lea:x} AEG {Aeg.GetValue():x}");
                        }
                    }
                }

                Ca = address;

                if (position.Ip == 0)
                    StepDecodeSample(format, true, address);
                else
                    StepDecodeSample(format, false, address);
            }
        }

        internal void AegStep(EgState state)
        {
            switch (state)
            {
                case EgState.Attack:
                    if (Aeg.AttackRate != 0)
                    {
                        Aeg.Value = unchecked((int)((ulong)Aeg.Value - ((((ulong)Aeg.Value << AegAttackShift) / Aeg.AttackRate) + 1)));
                        if (Aeg.GetValue() <= 0)
                        {
                            if (Ccd->LPSLNK == 0)
                            {
                                Debug.WriteLine($"[{ChannelNumber}]AEG_step : Switching to EG_Decay1");
                                SetAegState(EgState.Decay1);
                            }
                            Aeg.SetValue(0);
                        }
                    }
                    break;
                case EgState.Decay1:
                    Aeg.Value += unchecked((int)Aeg.Decay1Rate);
                    if (unchecked((uint)Aeg.GetValue()) >= Aeg.Decay2Value)
                    {
                        Debug.WriteLine($"[{ChannelNumber}]AEG_step : Switching to EG_Decay2");
                        SetAegState(EgState.Decay2);
                    }
                    break;
                case EgState.Decay2:
                    Aeg.Value += unchecked((int)Aeg.Decay2Rate);
                    if (Aeg.GetValue() >= 0x3ff)
                    {
                        Debug.WriteLine($"[{ChannelNumber}]AEG_step : Switching to EG_Release");
                        Aeg.SetValue(0x3ff);
                        SetAegState(EgState.Release);
                    }
                    break;
                case EgState.Release:
                    Aeg.Value += unchecked((int)Aeg.ReleaseRate);
                    if (Aeg.GetValue() >= 0x3ff)
                    {
                        Debug.WriteLine($"[{ChannelNumber}]AEG_step : EG_Release End @ {Aeg.GetValue():x}");
                        Disable();
                    }
                    break;
            }
        }

        internal void FegStep(EgState state)
        {
            if (!Feg.Active)
                return;

            uint delta = 0;
            uint target = 0;
            switch (state)
            {
                case EgState.Attack:
                    delta = Feg.AttackRate;
                    target = Ccd->FLV1;
                    break;
                case EgState.Decay1:
                    delta = Feg.Decay1Rate;
                    target = Ccd->FLV2;
                    break;
                case EgState.Decay2:
                    delta = Feg.Decay2Rate;
                    target = Ccd->FLV3;
                    break;
                case EgState.Release:
                    delta = Feg.ReleaseRate;
                    target = Ccd->FLV4;
                    break;
            }

            target <<= EgStepBits;
            if (Feg.Value < target)
            {
                uint maximum = target - Feg.Value;
                if (delta > maximum)
                    delta = maximum;
                Feg.Value += delta;
            }
            else if (Feg.Value > target)
            {
                uint maximum = Feg.Value - target;
                if (delta > maximum)
                    delta = maximum;
                Feg.Value -= delta;
            }
            else if ((int)Feg.State < (int)EgState.Decay2)
            {
                Debug.WriteLine($"[{ChannelNumber}]FEG_step : Switching to next state: {(int)Feg.State + 1} Freq {target >> EgStepBits}");
                SetFegState((EgState)((int)Feg.State + 1));
            }
        }
    }

    private static int DecodeAdpcm(uint sample, int previous, ref int quant)
    {
        int sign = unchecked((int)(1 - 2 * (sample / 8)));
        uint data = sample & 7;
        int value = (quant * AdpcmScale[data]) >> 3;
        if (value > 0x7fff)
            value = 0x7fff;
        value = sign * value + previous;

        quant = (quant * AdpcmQs[data]) >> 8;
        quant = Math.Clamp(quant, 127, 24576);

        return Math.Clamp(value, -32768, 32767);
    }

    private static T ReadAicaRam<T>(uint address) where T : unmanaged =>
        *(T*)(AicaInterface.Ram + (address & AicaInterface.RamMask & ~((uint)sizeof(T) - 1)));

    private static uint CalcEgSteps(float time)
    {
        const double allSteps = 1024 * (1 << EgStepBits) - 1;
        if (time < 0)
            return 0;
        if (time == 0)
            return (uint)allSteps;

        double samples = 44.1 * time;
        double steps = allSteps / samples;
        return (uint)Math.Round(steps, MidpointRounding.AwayFromZero);
    }

    private static uint CalcAttackEgSteps(float time)
    {
        if (time < 0)
            return 0;
        if (time == 0)
            return 1 << AegAttackShift;

        double samples = 44.1 * time;
        double factor = (1.0 / (1.0 - 1.0 / Math.Pow(0x280, 1.0 / samples))) * (1 << AegAttackShift);
        return (uint)Math.Round(factor, MidpointRounding.AwayFromZero);
    }

    private static readonly Channel[] Channels = new Channel[64];

    static AicaSoundGenerator()
    {
        Channel.StreamStepLut[(int)PcmType.Pcm16, 0, 0] = static channel => channel.StreamStep(PcmType.Pcm16, false, false);
        Channel.StreamStepLut[(int)PcmType.Pcm8, 0, 0] = static channel => channel.StreamStep(PcmType.Pcm8, false, false);
        Channel.StreamStepLut[(int)PcmType.Adpcm, 0, 0] = static channel => channel.StreamStep(PcmType.Adpcm, false, false);
        Channel.StreamStepLut[(int)PcmType.AdpcmStream, 0, 0] = static channel => channel.StreamStep(PcmType.AdpcmStream, false, false);
        Channel.StreamStepLut[(int)PcmType.Noise, 0, 0] = static channel => channel.StreamStep(PcmType.Noise, false, false);
        Channel.StreamStepLut[(int)PcmType.Pcm16, 0, 1] = static channel => channel.StreamStep(PcmType.Pcm16, false, true);
        Channel.StreamStepLut[(int)PcmType.Pcm8, 0, 1] = static channel => channel.StreamStep(PcmType.Pcm8, false, true);
        Channel.StreamStepLut[(int)PcmType.Adpcm, 0, 1] = static channel => channel.StreamStep(PcmType.Adpcm, false, true);
        Channel.StreamStepLut[(int)PcmType.AdpcmStream, 0, 1] = static channel => channel.StreamStep(PcmType.AdpcmStream, false, true);
        Channel.StreamStepLut[(int)PcmType.Noise, 0, 1] = static channel => channel.StreamStep(PcmType.Noise, false, true);
        Channel.StreamStepLut[(int)PcmType.Pcm16, 1, 0] = static channel => channel.StreamStep(PcmType.Pcm16, true, false);
        Channel.StreamStepLut[(int)PcmType.Pcm8, 1, 0] = static channel => channel.StreamStep(PcmType.Pcm8, true, false);
        Channel.StreamStepLut[(int)PcmType.Adpcm, 1, 0] = static channel => channel.StreamStep(PcmType.Adpcm, true, false);
        Channel.StreamStepLut[(int)PcmType.AdpcmStream, 1, 0] = static channel => channel.StreamStep(PcmType.AdpcmStream, true, false);
        Channel.StreamStepLut[(int)PcmType.Noise, 1, 0] = static channel => channel.StreamStep(PcmType.Noise, true, false);
        Channel.StreamStepLut[(int)PcmType.Pcm16, 1, 1] = static channel => channel.StreamStep(PcmType.Pcm16, true, true);
        Channel.StreamStepLut[(int)PcmType.Pcm8, 1, 1] = static channel => channel.StreamStep(PcmType.Pcm8, true, true);
        Channel.StreamStepLut[(int)PcmType.Adpcm, 1, 1] = static channel => channel.StreamStep(PcmType.Adpcm, true, true);
        Channel.StreamStepLut[(int)PcmType.AdpcmStream, 1, 1] = static channel => channel.StreamStep(PcmType.AdpcmStream, true, true);
        Channel.StreamStepLut[(int)PcmType.Noise, 1, 1] = static channel => channel.StreamStep(PcmType.Noise, true, true);
        Channel.StreamInitialStepLut[(int)PcmType.Pcm16] = static channel => channel.StepDecodeSampleInitial(PcmType.Pcm16);
        Channel.StreamInitialStepLut[(int)PcmType.Pcm8] = static channel => channel.StepDecodeSampleInitial(PcmType.Pcm8);
        Channel.StreamInitialStepLut[(int)PcmType.Adpcm] = static channel => channel.StepDecodeSampleInitial(PcmType.Adpcm);
        Channel.StreamInitialStepLut[(int)PcmType.AdpcmStream] = static channel => channel.StepDecodeSampleInitial(PcmType.AdpcmStream);
        Channel.StreamInitialStepLut[(int)PcmType.Noise] = static channel => channel.StepDecodeSampleInitial(PcmType.Noise);
        Channel.AegStepLut[(int)EgState.Attack] = static channel => channel.AegStep(EgState.Attack);
        Channel.AegStepLut[(int)EgState.Decay1] = static channel => channel.AegStep(EgState.Decay1);
        Channel.AegStepLut[(int)EgState.Decay2] = static channel => channel.AegStep(EgState.Decay2);
        Channel.AegStepLut[(int)EgState.Release] = static channel => channel.AegStep(EgState.Release);
        Channel.FegStepLut[(int)EgState.Attack] = static channel => channel.FegStep(EgState.Attack);
        Channel.FegStepLut[(int)EgState.Decay1] = static channel => channel.FegStep(EgState.Decay1);
        Channel.FegStepLut[(int)EgState.Decay2] = static channel => channel.FegStep(EgState.Decay2);
        Channel.FegStepLut[(int)EgState.Release] = static channel => channel.FegStep(EgState.Release);
        Lfo.AlfoWaveCalc[(int)LfoType.Sawtooth] = static lfo => lfo.CalcAlfo(LfoType.Sawtooth);
        Lfo.AlfoWaveCalc[(int)LfoType.Square] = static lfo => lfo.CalcAlfo(LfoType.Square);
        Lfo.AlfoWaveCalc[(int)LfoType.Triangle] = static lfo => lfo.CalcAlfo(LfoType.Triangle);
        Lfo.AlfoWaveCalc[(int)LfoType.Random] = static lfo => lfo.CalcAlfo(LfoType.Random);
        Lfo.PlfoWaveCalc[(int)LfoType.Sawtooth] = static lfo => lfo.CalcPlfo(LfoType.Sawtooth);
        Lfo.PlfoWaveCalc[(int)LfoType.Square] = static lfo => lfo.CalcPlfo(LfoType.Square);
        Lfo.PlfoWaveCalc[(int)LfoType.Triangle] = static lfo => lfo.CalcPlfo(LfoType.Triangle);
        Lfo.PlfoWaveCalc[(int)LfoType.Random] = static lfo => lfo.CalcPlfo(LfoType.Random);

        for (int index = 1; index < VolumeLut.Length; index++)
            VolumeLut[index] = (int)((1 << 15) / Math.Pow(2.0, (15 - index) / 2.0));

        for (int index = 0; index < 256; index++)
            TlLut[index] = (int)((1 << 15) / Math.Pow(2.0, index / 16.0));

        for (int index = 0; index < 64; index++)
        {
            AegAttackSamples[index] = CalcAttackEgSteps((float)AegAttackTime[index]);
            AegDsrSamples[index] = CalcEgSteps((float)AegDsrTime[index]);
            FegSamples[index] = AegDsrSamples[index];
        }

        for (int scale = 0; scale < 8; scale++)
        {
            float limit = PlfosScale[scale];
            for (int index = -128; index < 128; index++)
                PlfoScales[scale, index + 128] = (int)(1024f * MathF.Pow(2f, limit * index / 128f / 1200f));
        }

        for (int index = 0; index < Channels.Length; index++)
            Channels[index] = new Channel();
    }

    internal static void Init()
    {
        Channel.InitAll();
        Beep.Init();
        AicaDsp.Init();
    }

    internal static void Term() => AicaDsp.Term();

    internal static void WriteChannelReg(uint channel, uint register, int size) => Channels[channel].RegWrite(register, size);

    internal static void ReadCommonReg(uint register, bool isByte)
    {
        AicaCommonData* common = AicaMemory.CommonData;
        switch (register)
        {
            case 0x2808:
            case 0x2809:
                if (Aica.MidiSendBuffer.Count != 0)
                {
                    if (!isByte || register == 0x2808)
                    {
                        common->MIBUF = Aica.MidiSendBuffer.First!.Value;
                        Aica.MidiSendBuffer.RemoveFirst();
                    }
                    common->MIEMP = 0;
                    common->MIFUL = 1;
                }
                else
                {
                    common->MIEMP = 1;
                    common->MIFUL = 0;
                }
                common->MIOVF = 0;
                common->MOEMP = 1;
                common->MOFUL = 0;
                break;
            case 0x2810:
            case 0x2811:
            {
                Channel channel = Channels[common->MSLC];

                common->LP = channel.Loop.Looped;
                if (common->AFSEL == 1)
                    Trace.TraceWarning("FEG monitor (AFSEL=1) not supported");
                int aeg = channel.Aeg.GetValue();
                if (aeg > 0x3bf)
                    common->EG = 0x1fff;
                else
                    common->EG = unchecked((uint)aeg);
                common->SGC = (uint)channel.Aeg.State;

                if (!isByte || register == 0x2811)
                    channel.Loop.Looped = 0;
                break;
            }
            case 0x2814:
            case 0x2815:
            {
                Channel channel = Channels[common->MSLC];
                common->CA = channel.Ca;
                if (channel.Loop.Lea > channel.Loop.Lsa && channel.Loop.Lea > 3)
                    common->CA = Math.Min(common->CA, channel.Loop.Lea - 3);
                break;
            }
        }
    }

    internal static void VmuBeep(int on, int period) => Beep.Update(on, period);

    private const int CddaSize = 2352 / 2;
    private static readonly short[] CddaSector = new short[CddaSize];
    private static uint _cddaIndex = CddaSize;

    internal static void Sample()
    {
        int mixLeft = 0;
        int mixRight = 0;
        AicaDsp.DspState* dspState = (AicaDsp.DspState*)AicaDsp.StateAddress;
        new Span<int>(dspState->MIXS, 16).Clear();

        Channel.StepAll(ref mixLeft, ref mixRight);

        if (_cddaIndex >= CddaSize)
        {
            _cddaIndex = 0;
            fixed (short* sector = CddaSector)
                GdRomAudio.ReadCddaSector(sector);
        }
        int externalLeft = CddaSector[_cddaIndex];
        int externalRight = CddaSector[_cddaIndex + 1];
        _cddaIndex += 2;

        AicaDspOutputVolumeRegister* volume = AicaMemory.DspOutVolume;
        VolumePan(externalLeft, volume[16].EFSDL, volume[16].EFPAN, ref mixLeft, ref mixRight);
        VolumePan(externalRight, volume[17].EFSDL, volume[17].EFPAN, ref mixLeft, ref mixRight);

        AicaDspData* dspData = AicaMemory.DspData;
        dspData->EXTS[0] = unchecked((uint)externalLeft);
        dspData->EXTS[1] = unchecked((uint)externalRight);

        if (AicaConfiguration.DspEnabled)
        {
            AicaDsp.Step();

            for (int index = 0; index < 16; index++)
                VolumePan(*(short*)(dspData->EFREG + index), volume[index].EFSDL, volume[index].EFPAN,
                    ref mixLeft, ref mixRight);
        }

#if LIBRETRO
        if (AicaInterface.MuteAudio)
#else
        if (AicaInterface.FastForwardMode || AicaInterface.MuteAudio)
#endif
            return;

        if (AicaConfiguration.VmuSound)
        {
            int beep = Beep.GetSample();
            mixLeft += beep;
            mixRight += beep;
        }

        AicaCommonData* common = AicaMemory.CommonData;
        if (common->Mono != 0)
            mixLeft = mixRight = FPs(mixLeft + mixRight, 1);

        uint masterVolume = common->MVOL;
        int value = VolumeLut[masterVolume];
        mixLeft = (int)FPMul((long)mixLeft, value, 15);
        mixRight = (int)FPMul((long)mixRight, value, 15);

        if (common->DAC18B != 0)
        {
            mixLeft = FPs(mixLeft, 2);
            mixRight = FPs(mixRight, 2);
        }

#if CLIP_WARN
        if ((short)mixLeft != mixLeft || (short)mixRight != mixRight)
            Console.Write($"Clipped mixl {mixLeft} mixr {mixRight}\n");
#endif

        mixLeft = Math.Clamp(mixLeft, -32768, 32767);
        mixRight = Math.Clamp(mixRight, -32768, 32767);

        AicaAudioOutput.WriteSample((short)mixRight, (short)mixLeft);
    }

    private const int SaveVersion22 = 817;
    private const int SaveVersion28 = 823;
    private const int SaveVersion49 = 844;
    private const int SaveVersion56 = 851;

    internal static void Serialize(BinaryWriter writer)
    {
        foreach (Channel channel in Channels)
        {
            writer.Write(channel.Sa);
            writer.Write(channel.Ca);
            writer.Write(channel.StepPosition.Full);
            writer.Write(channel.S0);
            writer.Write(channel.S1);
            writer.Write(channel.Loop.Looped);
            writer.Write(channel.Adpcm.LastQuant);
            writer.Write(channel.Adpcm.LoopStartQuant);
            writer.Write(channel.Adpcm.LoopStartPreviousSample);
            writer.Write(channel.Adpcm.InLoop);
            writer.Write(channel.NoiseState);

            writer.Write(channel.Aeg.Value);
            writer.Write((int)channel.Aeg.State);
            writer.Write(channel.Feg.Value);
            writer.Write((int)channel.Feg.State);
            writer.Write(channel.Feg.Previous1);
            writer.Write(channel.Feg.Previous2);
            writer.Write(channel.Feg.Fraction);

            writer.Write(channel.Lfo.Counter);
            writer.Write(channel.Lfo.State);
            writer.Write(channel.Enabled);
        }
        Beep.Serialize(writer);
        writer.Write(MemoryMarshal.AsBytes(CddaSector.AsSpan()));
        writer.Write(_cddaIndex);
        writer.Write((uint)Aica.MidiSendBuffer.Count);
        foreach (byte value in Aica.MidiSendBuffer)
            writer.Write(value);
    }

    internal static void Deserialize(BinaryReader reader, int version)
    {
        foreach (Channel channel in Channels)
        {
            channel.Quiet = true;
            channel.Sa = reader.ReadUInt32();
            channel.Ca = reader.ReadUInt32();
            channel.StepPosition.Full = reader.ReadUInt32();
            channel.UpdatePitch();
            channel.S0 = reader.ReadInt32();
            channel.S1 = reader.ReadInt32();
            channel.Loop.Looped = reader.ReadByte();
            channel.UpdateLoop();
            channel.Adpcm.LastQuant = reader.ReadInt32();
            channel.Adpcm.LoopStartQuant = reader.ReadInt32();
            channel.Adpcm.LoopStartPreviousSample = reader.ReadInt32();
            channel.Adpcm.InLoop = reader.ReadBoolean();
            channel.NoiseState = reader.ReadUInt32();
            channel.UpdateAttenuations();
            channel.UpdateDspMix();

            channel.Aeg.Value = reader.ReadInt32();
            channel.Aeg.State = (EgState)reader.ReadInt32();
            channel.SetAegState(channel.Aeg.State);
            channel.UpdateAeg();
            channel.Feg.Value = reader.ReadUInt32();
            channel.Feg.State = (EgState)reader.ReadInt32();
            channel.Feg.Previous1 = reader.ReadInt32();
            channel.Feg.Previous2 = reader.ReadInt32();
            if (version >= SaveVersion56)
            {
                channel.Feg.Fraction = reader.ReadInt32();
            }
            else
            {
                channel.Feg.Fraction = 0;
                channel.Feg.Previous1 = 0;
                channel.Feg.Previous2 = 0;
            }
            channel.SetFegState(channel.Feg.State);
            channel.UpdateFeg();
            channel.UpdateStreamStep();

            channel.Lfo.Counter = reader.ReadUInt32();
            channel.Lfo.State = reader.ReadByte();
            channel.UpdateLfo(true);
            channel.Enabled = reader.ReadBoolean();
            channel.Quiet = false;
        }
        Beep.Deserialize(reader, version);
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(CddaSector.AsSpan()));
        _cddaIndex = reader.ReadUInt32();
        Aica.MidiSendBuffer.Clear();
        if (version >= SaveVersion28)
        {
            uint size = reader.ReadUInt32();
            for (uint index = 0; index < size; index++)
            {
                byte value = reader.ReadByte();
                Aica.MidiSendBuffer.AddLast(value);
            }
        }
    }
}
