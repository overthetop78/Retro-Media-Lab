// Copyright 2004 Stephane Dallongeville
// Copyright 2004-2006 Theo Berkau
// Translated from the SCSP bus and register access routines in Yabause scsp.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using RetroMediaLab.Chips.CPU.Motorola.M68000;
using static RetroMediaLab.Chips.Controllers.Sega.SaturnMemoryAccess;

namespace RetroMediaLab.Chips.Audio.Yamaha.SCSP;

public sealed partial class SaturnScsp : IDisposable
{
    public delegate void AudioOutput(ReadOnlySpan<int> left, ReadOnlySpan<int> right, int length);

    public sealed record RegisterConnections(
        Action SendMainInterrupt,
        Func<int> NextRandom,
        Action<bool> SetCdAudioPlaying,
        Action<int> SetCdTiming,
        Func<int> GetAudioSpace,
        AudioOutput UpdateAudio,
        Action<int> ChangeAudioVideoFormat)
    {
        public Action<byte>? WriteMidiOutput { get; init; }
    }

    private readonly RegisterConnections _registers;
    private readonly bool _useNewScsp;
    private readonly bool _useScsp2;
    private readonly byte[] _soundRam = new byte[0x80000];
    private readonly byte[] _registerRam = new byte[0x1000];
    private readonly SaturnScspDsp _dsp;
    private readonly SaturnC68kInterface _soundCpu = new();
    private uint _mem4b;
    private bool _soundCpuRunning;
    private int _savedSoundCpuCycles;

    private readonly int[] _legacyNewOutputLeft = new int[900];
    private readonly int[] _legacyNewOutputRight = new int[900];
    private int _legacyNewOutputPosition;
    private int _legacyNewCycles;

    private const int SoundBufferCount = 10;
    private int _soundLength = 44100 / 60;
    private int _soundLines = 263;
    private int[] _soundLeft = new int[(44100 / 60) * SoundBufferCount];
    private int[] _soundRight = new int[(44100 / 60) * SoundBufferCount];
    private int _soundGeneratePosition;
    private int _soundOutputLeft;
    private int _soundTimingLines;
    private int _soundTimingFraction;

    private readonly byte[] _cdda = new byte[2 * 75 * 2352];
    private int _cddaNextIn;
    private int _cddaBytesLeft;

    private const int MidiInputEmpty = 0x01;
    private const int MidiInputFull = 0x02;
    private const int MidiInputOverflow = 0x04;
    private const int MidiOutputEmpty = 0x08;
    private const int MidiOutputFull = 0x10;

    private readonly byte[] _midiInput = new byte[4];
    private readonly byte[] _midiOutput = new byte[4];
    private int _masterVolume;
    private int _ringLength;
    private int _ringBase;
    private int _monitorSlot;
    private int _callAddress;
    private int _dmaMemoryAddress;
    private int _dmaRegisterAddress;
    private int _dmaFlags;
    private int _dmaLength;
    private int _midiInputCount;
    private int _midiOutputCount;
    private struct Timer
    {
        public int Control;
        public uint Counter;
    }

    private readonly Timer[] _timers = new Timer[3];
    private int _mainInterruptEnable;
    private int _mainInterruptPending;
    private int _soundInterruptEnable;
    private int _soundInterruptPending;
    private int _soundInterruptLevel0;
    private int _soundInterruptLevel1;
    private int _soundInterruptLevel2;
    private int _midiFlags;
    private int _envelopePhase;
    private int _envelopeLevel;

    public SaturnScsp(RegisterConnections registers, bool useNewScsp, bool useScsp2 = false, bool useThreads = false)
    {
        _registers = registers;
        _useNewScsp = useNewScsp || useScsp2;
        _useScsp2 = useScsp2;

        bool initialized = false;
        try
        {
            if (_soundCpu.Initialize() != 0)
                throw new InvalidOperationException("Failed to initialize the SCSP sound CPU.");

            _soundCpu.SetReadByte(SoundCpuReadByte);
            _soundCpu.SetReadWord(SoundCpuReadWord);
            _soundCpu.SetWriteByte(SoundCpuWriteByte);
            _soundCpu.SetWriteWord(SoundCpuWriteWord);
            UpdateSoundCpuFetch();

            _dsp = new SaturnScspDsp(_soundRam);
            if (_useScsp2)
                InitializeScsp2Tables();
            else
                InitializeLegacyTables();
            Reset();
            if (_useScsp2 && useThreads)
                StartScsp2Thread();
            initialized = true;
        }
        finally
        {
            if (!initialized)
                _soundCpu.Deinitialize();
        }
    }

    public void Dispose()
    {
        try
        {
            StopScsp2Thread();
        }
        finally
        {
            _soundCpuRunning = false;
            _soundCpu.Deinitialize();
        }
    }

    internal SaturnScspDspInterface Dsp => _dsp.Interface;

    private const int LegacyFrequencyFractionBits = 10;
    private const int LegacyEnvelopeFractionBits = 10;
    private const int LegacyEnvelopeLength = 1 << 10;
    private const int LegacyEnvelopeMask = LegacyEnvelopeLength - 1;
    private const int LegacyEnvelopeAttackStart = 0;
    private const int LegacyEnvelopeDecayStart = LegacyEnvelopeLength << LegacyEnvelopeFractionBits;
    private const int LegacyEnvelopeAttackEnd = LegacyEnvelopeDecayStart - 1;
    private const int LegacyEnvelopeDecayEnd = ((2 * LegacyEnvelopeLength) << LegacyEnvelopeFractionBits) - 1;
    private const int LegacyLfoLength = 1 << 10;
    private const int LegacyLfoMask = LegacyLfoLength - 1;
    private const int LegacyLfoFractionBits = 10;
    private const int LegacyRamMask = 0x80000 - 1;
    private const int LegacyAttackRate = 8 * 44100;
    private const int LegacyDecayRate = 12 * LegacyAttackRate;

    private enum LegacyEnvelopePhase : uint { Attack, Decay, Sustain, Release }
    private enum LegacyEnvelopeStep { None, Attack, Decay, Sustain, Release }
    private delegate void LegacyEnvelopeEvent(ref LegacySlot slot);

    private readonly record struct LegacyRatePointer(int[]? Table, int Offset);

    private struct LegacySlot
    {
        public byte StackWriteEnable;
        public byte SoundDirect;
        public byte Pcm8Bit;
        public byte SourceBitControl;
        public byte SoundSourceControl;
        public byte LoopControl;
        public byte Key;
        public byte KeyExtended;
        public uint SampleByteBase;
        public uint SampleWordBase;
        public uint FrequencyCounter;
        public uint FrequencyIncrement;
        public uint UnadjustedFrequencyIncrement;
        public int EnvelopeCounter;
        public LegacyEnvelopeStep EnvelopeStep;
        public int AttackIncrement;
        public int DecayIncrement;
        public int SustainIncrement;
        public int ReleaseIncrement;
        public int EnvelopeCompare;
        public LegacyEnvelopePhase EnvelopePhase;
        public int Envelope;
        public LegacyEnvelopeEvent? NextEnvelopeEvent;
        public uint LfoCounter;
        public int LfoIncrement;
        public uint StartAddress;
        public uint LoopStart;
        public uint LoopEnd;
        public int TotalLevel;
        public int SustainLevel;
        public int AttackRate;
        public int DecayRate;
        public int SustainRate;
        public int ReleaseRate;
        public LegacyRatePointer AttackRatePointer;
        public LegacyRatePointer DecayRatePointer;
        public LegacyRatePointer SustainRatePointer;
        public LegacyRatePointer ReleaseRatePointer;
        public uint KeyRateScale;
        public int[]? LfoFrequencyWave;
        public int[]? LfoEnvelopeWave;
        public byte LfoFrequencySensitivity;
        public byte LfoEnvelopeSensitivity;
        public byte FrequencyShift;
        public byte ModulationLevel;
        public byte ModulationSourceX;
        public byte ModulationSourceY;
        public byte InputMixLevel;
        public byte DirectLevelLeft;
        public byte DirectLevelRight;
        public byte EffectLevelLeft;
        public byte EffectLevelRight;
        public byte EnvelopeHold;
        public byte LoopStartLink;
    }

    private readonly int[] _legacyStack = new int[32 * 2];
    private readonly LegacySlot[] _legacySlots = new LegacySlot[32];
    private readonly int[] _legacyEnvelope = new int[LegacyEnvelopeLength * 2];
    private readonly int[] _legacyLfoSawEnvelope = new int[LegacyLfoLength];
    private readonly int[] _legacyLfoSquareEnvelope = new int[LegacyLfoLength];
    private readonly int[] _legacyLfoTriangleEnvelope = new int[LegacyLfoLength];
    private readonly int[] _legacyLfoNoiseEnvelope = new int[LegacyLfoLength];
    private readonly int[] _legacyLfoSawFrequency = new int[LegacyLfoLength];
    private readonly int[] _legacyLfoSquareFrequency = new int[LegacyLfoLength];
    private readonly int[] _legacyLfoTriangleFrequency = new int[LegacyLfoLength];
    private readonly int[] _legacyLfoNoiseFrequency = new int[LegacyLfoLength];
    private readonly int[] _legacyAttackRate = new int[0x40 + 0x20];
    private readonly int[] _legacyDecayRate = new int[0x40 + 0x20];
    private readonly int[] _legacyNullRate = new int[0x20];
    private readonly int[] _legacyLfoStep = new int[32];
    private readonly int[] _legacyTotalLevel = new int[256];

    private enum EnvelopeState { Attack, Decay1, Decay2, Release }

    private struct SlotRegs
    {
        public int Kx;
        public int Kb;
        public int Sbctl;
        public int Ssctl;
        public int Lpctl;
        public int Pcm8b;
        public int Sa;
        public int Lsa;
        public int Lea;
        public int D2r;
        public int D1r;
        public int Hold;
        public int Ar;
        public int Unknown1;
        public int Ls;
        public int Krs;
        public int Dl;
        public int Rr;
        public int Unknown2;
        public int Si;
        public int Sd;
        public int Tl;
        public int Mdl;
        public int Mdxsl;
        public int Mdysl;
        public int Unknown3;
        public int Oct;
        public int Unknown4;
        public int Fns;
        public int Re;
        public int Lfof;
        public int Plfows;
        public int Plfos;
        public int Alfows;
        public int Alfos;
        public int Unknown5;
        public int Isel;
        public int Imxl;
        public int Disdl;
        public int Dipan;
        public int Efsdl;
        public int Efpan;
    }

    private struct SlotState
    {
        public ushort Wave;
        public int Backwards;
        public EnvelopeState Envelope;
        public short Output;
        public ushort Attenuation;
        public int StepCount;
        public uint SampleCounter;
        public uint EnvelopeStepsTaken;
        public int WaveformPhaseValue;
        public int SampleOffset;
        public uint AddressPointer;
        public uint LfoCounter;
        public uint LfoPos;
        public int Num;
    }

    private sealed class Slot
    {
        public SlotRegs Regs;
        public SlotState State;
    }

    private readonly Slot[] _slots = new Slot[32];
    private readonly ushort[] _soundStack = new ushort[64];

    private static readonly byte[,] AttackRateTable =
    {
       { 4, 4, 4, 4 },
       { 3, 4, 4, 4 },
       { 3, 4, 3, 4 },
       { 3, 3, 3, 4 },

       { 3, 3, 3, 3 },
       { 2, 3, 3, 3 },
       { 2, 3, 2, 3 },
       { 2, 2, 2, 3 },

       { 2, 2, 2, 2 },
       { 1, 2, 2, 2 },
       { 1, 2, 1, 2 },
       { 1, 1, 1, 2 },

       { 1, 1, 1, 1 },
       { 1, 1, 1, 1 },
       { 1, 1, 1, 1 },
       { 1, 1, 1, 1 },
    };

    private static readonly byte[,] DecayRateTable =
    {
       { 1, 1, 1, 1 },
       { 2, 1, 1, 1 },
       { 2, 1, 2, 1 },
       { 2, 2, 2, 1 },

       { 2, 2, 2, 2 },
       { 4, 2, 2, 2 },
       { 4, 2, 4, 2 },
       { 4, 4, 4, 2 },

       { 4, 4, 4, 4 },
       { 8, 4, 4, 4 },
       { 8, 4, 8, 4 },
       { 8, 8, 8, 4 },

       { 8, 8, 8, 8 },
       { 8, 8, 8, 8 },
       { 8, 8, 8, 8 },
       { 8, 8, 8, 8 },
    };

    private static readonly int[] LfoStepTable = {
       0x3fc,
       0x37c,
       0x2fc,
       0x27c,
       0x1fc,
       0x1bc,
       0x17c,
       0x13c,
       0x0fc,
       0x0bc,
       0x0dc,
       0x08c,
       0x07c,
       0x06c,
       0x05c,
       0x04c,

       0x03c,
       0x034,
       0x02c,
       0x024,
       0x01c,
       0x018,
       0x014,
       0x010,
       0x00c,
       0x00a,
       0x008,
       0x006,
       0x004,
       0x003,
       0x002,
       0x001,
    };

    private const ushort EffectiveRateEnd = 0xFFFF;
    private static readonly ushort[,] EnvelopeTable =
    {
        { 8192 >> 0, 4096 >> 0, 4096 >> 0, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },
        { 8192 >> 0, 4096 >> 0, 4096 >> 0, 4096 >> 0, 4096 >> 0, 4096 >> 0, 4096 >> 0, EffectiveRateEnd },
        { 4096 >> 0, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd , EffectiveRateEnd },
        { 4096 >> 0, 4096 >> 0, 4096 >> 0, 2048 >> 0, 2048 >> 0, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },

        { 8192 >> 1, 4096 >> 1, 4096 >> 1, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },
        { 8192 >> 1, 4096 >> 1, 4096 >> 1, 4096 >> 1, 4096 >> 1, 4096 >> 1, 4096 >> 1, EffectiveRateEnd },
        { 4096 >> 1, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd , EffectiveRateEnd },
        { 4096 >> 1, 4096 >> 1, 4096 >> 1, 2048 >> 1, 2048 >> 1, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },

        { 8192 >> 2, 4096 >> 2, 4096 >> 2, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },
        { 8192 >> 2, 4096 >> 2, 4096 >> 2, 4096 >> 2, 4096 >> 2, 4096 >> 2, 4096 >> 2, EffectiveRateEnd },
        { 4096 >> 2, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd , EffectiveRateEnd },
        { 4096 >> 2, 4096 >> 2, 4096 >> 2, 2048 >> 2, 2048 >> 2, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },

        { 8192 >> 3, 4096 >> 3, 4096 >> 3, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },
        { 8192 >> 3, 4096 >> 3, 4096 >> 3, 4096 >> 3, 4096 >> 3, 4096 >> 3, 4096 >> 3, EffectiveRateEnd },
        { 4096 >> 3, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd , EffectiveRateEnd },
        { 4096 >> 3, 4096 >> 3, 4096 >> 3, 2048 >> 3, 2048 >> 3, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },

        { 8192 >> 4, 4096 >> 4, 4096 >> 4, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },
        { 8192 >> 4, 4096 >> 4, 4096 >> 4, 4096 >> 4, 4096 >> 4, 4096 >> 4, 4096 >> 4, EffectiveRateEnd },
        { 4096 >> 4, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd , EffectiveRateEnd },
        { 4096 >> 4, 4096 >> 4, 4096 >> 4, 2048 >> 4, 2048 >> 4, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },

        { 8192 >> 5, 4096 >> 5, 4096 >> 5, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },
        { 8192 >> 5, 4096 >> 5, 4096 >> 5, 4096 >> 5, 4096 >> 5, 4096 >> 5, 4096 >> 5, EffectiveRateEnd },
        { 4096 >> 5, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd , EffectiveRateEnd },
        { 4096 >> 5, 4096 >> 5, 4096 >> 5, 2048 >> 5, 2048 >> 5, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },

        { 8192 >> 6, 4096 >> 6, 4096 >> 6, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },
        { 8192 >> 6, 4096 >> 6, 4096 >> 6, 4096 >> 6, 4096 >> 6, 4096 >> 6, 4096 >> 6, EffectiveRateEnd },
        { 4096 >> 6, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd , EffectiveRateEnd },
        { 4096 >> 6, 4096 >> 6, 4096 >> 6, 2048 >> 6, 2048 >> 6, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },

        { 8192 >> 7, 4096 >> 7, 4096 >> 7, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },
        { 8192 >> 7, 4096 >> 7, 4096 >> 7, 4096 >> 7, 4096 >> 7, 4096 >> 7, 4096 >> 7, EffectiveRateEnd },
        { 4096 >> 7, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd , EffectiveRateEnd },
        { 4096 >> 7, 4096 >> 7, 4096 >> 7, 2048 >> 7, 2048 >> 7, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },

        { 8192 >> 8, 4096 >> 8, 4096 >> 8, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },
        { 8192 >> 8, 4096 >> 8, 4096 >> 8, 4096 >> 8, 4096 >> 8, 4096 >> 8, 4096 >> 8, EffectiveRateEnd },
        { 4096 >> 8, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd , EffectiveRateEnd },
        { 4096 >> 8, 4096 >> 8, 4096 >> 8, 2048 >> 8, 2048 >> 8, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },

        { 8192 >> 9, 4096 >> 9, 4096 >> 9, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },
        { 8192 >> 9, 4096 >> 9, 4096 >> 9, 4096 >> 9, 4096 >> 9, 4096 >> 9, 4096 >> 9, EffectiveRateEnd },
        { 4096 >> 9, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd , EffectiveRateEnd },
        { 4096 >> 9, 4096 >> 9, 4096 >> 9, 2048 >> 9, 2048 >> 9, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },

        { 8192 >> 10, 4096 >> 10, 4096 >> 10, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },
        { 8192 >> 10, 4096 >> 10, 4096 >> 10, 4096 >> 10, 4096 >> 10, 4096 >> 10, 4096 >> 10, EffectiveRateEnd },
        { 4096 >> 10, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd , EffectiveRateEnd },
        { 4096 >> 10, 4096 >> 10, 4096 >> 10, 2048 >> 10, 2048 >> 10, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },

        { 8192 >> 11, 4096 >> 11, 4096 >> 11, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },
        { 8192 >> 11, 4096 >> 11, 4096 >> 11, 4096 >> 11, 4096 >> 11, 4096 >> 11, 4096 >> 11, EffectiveRateEnd },
        { 4096 >> 11, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd , EffectiveRateEnd },
        { 4096 >> 11, 4096 >> 11, 4096 >> 11, 2048 >> 11, 2048 >> 11, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },

        { 8192 >> 12, 4096 >> 12, 4096 >> 12, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },
        { 8192 >> 12, 4096 >> 12, 4096 >> 12, 4096 >> 12, 4096 >> 12, 4096 >> 12, 4096 >> 12, EffectiveRateEnd },
        { 4096 >> 12, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd , EffectiveRateEnd },
        { 4096 >> 12, 4096 >> 12, 4096 >> 12, 2048 >> 12, 2048 >> 12, EffectiveRateEnd, EffectiveRateEnd, EffectiveRateEnd },
    };

    private readonly sbyte[] _plfoSawTable = new sbyte[256];

    private readonly sbyte[] _plfoSquareTable = new sbyte[256];

    private readonly sbyte[] _plfoTriTable = new sbyte[256];

    private readonly sbyte[] _plfoNoiseTable = new sbyte[256];

    private readonly byte[] _alfoSawTable = new byte[256];

    private readonly byte[] _alfoSquareTable = new byte[256];

    private readonly byte[] _alfoTriTable = new byte[256];

    private readonly byte[] _alfoNoiseTable = new byte[256];

    private void FillPlfoTables()
    {
        unchecked
        {
            int i;

            for (i = 0; i < 256; i++)
            {
                if (i < 128)
                    _plfoSawTable[i] = (sbyte)(i);
                else
                    _plfoSawTable[i] = (sbyte)(-256 + i);
            }

            for (i = 0; i < 256; i++)
            {
                if (i < 128)
                    _plfoSquareTable[i] = (sbyte)(127);
                else
                    _plfoSquareTable[i] = (sbyte)(-128);
            }

            for (i = 0; i < 256; i++)
            {
                if (i < 64)
                    _plfoTriTable[i] = (sbyte)(i * 2);
                else if (i < 192)
                    _plfoTriTable[i] = (sbyte)(255 - (i * 2));
                else
                    _plfoTriTable[i] = (sbyte)((i * 2) - 512);
            }

            for (i = 0; i < 256; i++)
            {
                _plfoNoiseTable[i] = (sbyte)(_registers.NextRandom() & 0xff);
            }
        }
    }

    private void FillAlfoTables()
    {
        unchecked
        {
            int i;

            for (i = 0; i < 256; i++)
            {
                _alfoSawTable[i] = (byte)(i);
            }

            for (i = 0; i < 256; i++)
            {
                if (i < 128)
                    _alfoSquareTable[i] = (byte)(0);
                else
                    _alfoSquareTable[i] = (byte)(0xff);
            }

            for (i = 0; i < 256; i++)
            {
                if (i < 128)
                    _alfoTriTable[i] = (byte)(i * 2);
                else
                    _alfoTriTable[i] = (byte)(255 - (i * 2));
            }

            for (i = 0; i < 256; i++)
            {
                _alfoNoiseTable[i] = (byte)(_registers.NextRandom() & 0xff);
            }
        }
    }

    private void Op1(Slot slot)
    {
        unchecked
        {
            int oct = slot.Regs.Oct ^ 8;
            uint fns = (uint)(slot.Regs.Fns ^ 0x400);
            uint phaseIncrement = fns << oct;
            int plfoVal = 0;
            int plfoShifted = 0;

            if (slot.State.Attenuation > 0x3bf)
                return;

            if (slot.State.LfoCounter % LfoStepTable[slot.Regs.Lfof] == 0)
            {
                slot.State.LfoCounter = 0;
                slot.State.LfoPos++;

                if (slot.State.LfoPos > 0xff)
                    slot.State.LfoPos = 0;
            }

            if (slot.Regs.Plfows == 0)
                plfoVal = _plfoSawTable[slot.State.LfoPos];
            else if (slot.Regs.Plfows == 1)
                plfoVal = _plfoSquareTable[slot.State.LfoPos];
            else if (slot.Regs.Plfows == 2)
                plfoVal = _plfoTriTable[slot.State.LfoPos];
            else if (slot.Regs.Plfows == 3)
                plfoVal = _plfoNoiseTable[slot.State.LfoPos];

            plfoShifted = (plfoVal << slot.Regs.Plfos) >> 2;

            slot.State.WaveformPhaseValue &= (1 << 18) - 1;
            slot.State.WaveformPhaseValue += (int)(phaseIncrement + (uint)plfoShifted);
        }
    }

    private int GetSlot(Slot slot, int mdsl)
    {
        unchecked
        {
            return (mdsl + slot.State.Num) & 0x1f;
        }
    }

    private void Op2(Slot slot)
    {
        unchecked
        {
            int mdOut = 0;
            int sampleDelta = slot.State.WaveformPhaseValue >> 18;

            if (slot.State.Attenuation > 0x3bf)
                return;

            if (slot.Regs.Mdl != 0)
            {

                int xSel = GetSlot(slot, slot.Regs.Mdxsl);
                int ySel = GetSlot(slot, slot.Regs.Mdysl);
                short xd = (short)_soundStack[xSel];
                short yd = (short)_soundStack[ySel];

                int zd = (xd + yd) / 2;

                ushort shift = (ushort)(0xF - slot.Regs.Mdl);
                zd >>= shift;

                mdOut = zd;
            }

            if (slot.Regs.Lpctl == 0)
            {
                slot.State.SampleOffset += sampleDelta;

                if (slot.State.SampleOffset >= slot.Regs.Lea)
                    slot.State.Attenuation = 0x3ff;
            }
            else if (slot.Regs.Lpctl == 1)
            {
                slot.State.SampleOffset += sampleDelta;

                if (slot.State.SampleOffset >= slot.Regs.Lea)
                    slot.State.SampleOffset = slot.Regs.Lsa;
            }
            else if (slot.Regs.Lpctl == 2)
            {
                if (slot.State.Backwards == 0)
                    slot.State.SampleOffset += sampleDelta;
                else
                    slot.State.SampleOffset -= sampleDelta;

                if (slot.State.Backwards == 0)
                {
                    if (slot.State.SampleOffset >= slot.Regs.Lea)
                    {
                        slot.State.SampleOffset = slot.Regs.Lea;
                        slot.State.Backwards = 1;
                    }
                }
                else
                {

                    if (slot.State.SampleOffset <= slot.Regs.Lsa)
                        slot.State.SampleOffset = slot.Regs.Lea;
                }
            }
            else if (slot.Regs.Lpctl == 3)
            {
                if (slot.State.Backwards == 0)
                    slot.State.SampleOffset += sampleDelta;
                else
                    slot.State.SampleOffset -= sampleDelta;

                if (slot.State.Backwards == 0)
                {
                    if (slot.State.SampleOffset >= slot.Regs.Lea)
                    {
                        slot.State.SampleOffset = slot.Regs.Lea;
                        slot.State.Backwards = 1;
                    }
                }
                else
                {
                    if (slot.State.SampleOffset <= slot.Regs.Lsa)
                    {
                        slot.State.SampleOffset = slot.Regs.Lsa;
                        slot.State.Backwards = 0;
                    }
                }
            }

            if (slot.Regs.Pcm8b == 0)
                slot.State.AddressPointer = (uint)(slot.Regs.Sa + (slot.State.SampleOffset + mdOut) * 2);
            else
                slot.State.AddressPointer = (uint)(slot.Regs.Sa + (slot.State.SampleOffset + mdOut));
        }
    }

    private void Op3(Slot slot)
    {
        unchecked
        {
            uint addr = (slot.State.AddressPointer) & 0x7FFFF;

            if (slot.State.Attenuation > 0x3bf)
                return;

            if (slot.Regs.Pcm8b == 0)
                slot.State.Wave = (ushort)SoundCpuReadWord(addr & ~1u);
            else
                slot.State.Wave = (ushort)(SoundCpuReadByte(addr) << 8);

            slot.State.Output = (short)slot.State.Wave;
        }
    }

    private void ChangeEnvelopeState(Slot slot, EnvelopeState newState)
    {
        unchecked
        {
            slot.State.Envelope = newState;
            slot.State.StepCount = 0;
        }
    }

    private int NeedEnvelopeStep(int effectiveRate, uint sampleCounter, Slot slot)
    {
        unchecked
        {
            if (sampleCounter == 0)
                return 0;

            if (effectiveRate == 0 || effectiveRate == 1)
            {
                return 0;
            }
            else if (effectiveRate >= 0x30)
            {
                if ((sampleCounter & 1) == 0)
                {
                    slot.State.EnvelopeStepsTaken++;
                    return 1;
                }
                else
                    return 0;
            }
            else
            {
                int pos = effectiveRate - 2;

                int result = 0;

                int value = EnvelopeTable[pos, slot.State.StepCount];

                if (sampleCounter % value == 0)
                {
                    result = 1;

                    slot.State.EnvelopeStepsTaken++;
                    slot.State.StepCount++;

                    if (EnvelopeTable[pos, slot.State.StepCount] == EffectiveRateEnd)
                        slot.State.StepCount = 0;
                }

                return result;
            }
        }
    }

    private int GetRate(Slot slot, int rate)
    {
        unchecked
        {
            int result = 0;

            if (slot.Regs.Krs == 0xf)
                result = rate * 2;
            else
            {
                result = (slot.Regs.Krs * 2) + (rate * 2) + ((slot.Regs.Fns >> 9) & 1);
                result = (8 ^ slot.Regs.Oct) + (result - 8);
            }

            if (result <= 0)
                return 0;

            if (result >= 0x3c)
                return 0x3c;

            return result;
        }
    }

    private void DoDecay(Slot slot, int rateIn)
    {
        unchecked
        {
            int rate = GetRate(slot, rateIn);
            int sampleMod4 = (int)(slot.State.EnvelopeStepsTaken & 3);
            int decayRate;

            if (rate <= 0x30)
                decayRate = DecayRateTable[0, sampleMod4];
            else
                decayRate = DecayRateTable[rate - 0x30, sampleMod4];

            if (NeedEnvelopeStep(rate, slot.State.SampleCounter, slot) != 0)
            {
                if (slot.State.Attenuation < 0x3bf)
                    slot.State.Attenuation = (ushort)(slot.State.Attenuation + decayRate);
            }
        }
    }

    private void Op4(Slot slot)
    {
        unchecked
        {
            int sampleMod4 = (int)(slot.State.EnvelopeStepsTaken & 3);

            if (slot.State.Attenuation >= 0x3bf)
                return;

            if (slot.State.Envelope == EnvelopeState.Attack)
            {
                int rate = GetRate(slot, slot.Regs.Ar);
                int needStep = NeedEnvelopeStep(rate, slot.State.SampleCounter, slot);

                if (needStep != 0)
                {
                    int attackRate = 0;

                    if (rate <= 0x30)
                        attackRate = AttackRateTable[0, sampleMod4];
                    else
                        attackRate = AttackRateTable[rate - 0x30, sampleMod4];

                    slot.State.Attenuation = (ushort)(slot.State.Attenuation - (((slot.State.Attenuation >> attackRate)) + 1));

                    if (slot.State.Attenuation == 0)
                        ChangeEnvelopeState(slot, EnvelopeState.Decay1);
                }
            }
            else if (slot.State.Envelope == EnvelopeState.Decay1)
            {
                DoDecay(slot,slot.Regs.D1r);

                if ((slot.State.Attenuation >> 5) >= slot.Regs.Dl)
                    ChangeEnvelopeState(slot, EnvelopeState.Decay2);
            }
            else if (slot.State.Envelope == EnvelopeState.Decay2)
                DoDecay(slot, slot.Regs.D2r);
            else if (slot.State.Envelope == EnvelopeState.Release)
                DoDecay(slot, slot.Regs.Rr);
        }
    }

    private short ApplyVolume(ushort tl, ushort slotAtt, short s)
    {
        unchecked
        {
            uint tlAtt = (uint)(tl * 4);
            uint attClipped = 0;
            int sampleAtt = 0;
            uint shift = 0;

            tlAtt += slotAtt;

            attClipped = tlAtt;
            attClipped &= 0x3f;
            attClipped ^= 0x7f;
            attClipped += 1;

            sampleAtt = (int)((uint)s * attClipped);
            shift = tlAtt >> 6;

            sampleAtt = sampleAtt >> (int)(shift + 7);

            return (short)sampleAtt;
        }
    }

    private void Op5(Slot slot)
    {
        unchecked
        {
            if (slot.State.Attenuation > 0x3bf)
            {
                slot.State.Output = 0;
                return;
            }
            else
            {
                int alfoVal = 0;
                int lfoAdd = 0;
                short sample = 0;

                if (slot.Regs.Alfows == 0)
                    alfoVal = _alfoSawTable[slot.State.LfoPos];
                else if (slot.Regs.Alfows == 1)
                    alfoVal = _alfoSquareTable[slot.State.LfoPos];
                else if (slot.Regs.Alfows == 2)
                    alfoVal = _alfoTriTable[slot.State.LfoPos];
                else if (slot.Regs.Alfows == 3)
                    alfoVal = _alfoNoiseTable[slot.State.LfoPos];

                lfoAdd = (((alfoVal + 1)) >> (7 - slot.Regs.Alfos)) << 1;

                sample = ApplyVolume((ushort)slot.Regs.Tl, (ushort)(slot.State.Attenuation + lfoAdd), slot.State.Output);
                slot.State.Output = sample;
            }
        }
    }

    private void Op6(Slot slot)
    {
        unchecked
        {

        }
    }

    private void Op7(Slot slot)
    {
        unchecked
        {
            uint previous = _soundStack[slot.State.Num + 32];
            _soundStack[slot.State.Num + 32] = (ushort)slot.State.Output;
            _soundStack[slot.State.Num] = (ushort)previous;

            slot.State.SampleCounter++;
            slot.State.LfoCounter++;
        }
    }

    private void KeyOn(Slot slot)
    {
        unchecked
        {
            if (slot.State.Envelope == EnvelopeState.Release)
            {
                slot.State.Envelope = EnvelopeState.Attack;
                slot.State.Attenuation = 0x280;
                slot.State.SampleCounter = 0;
                slot.State.StepCount = 0;
                slot.State.SampleOffset = 0;
                slot.State.EnvelopeStepsTaken = 0;

            }
        }
    }

    private void KeyOff(Slot slot)
    {
        unchecked
        {
            ChangeEnvelopeState(slot, EnvelopeState.Release);
        }
    }

    private void KeyOnEx()
    {
        unchecked
        {
            int channel;
            for (channel = 0; channel < 32; channel++)
            {
                if (_slots[channel].Regs.Kb != 0)
                    KeyOn(_slots[channel]);
                else
                    KeyOff(_slots[channel]);
            }
        }
    }

    private void WriteNewSlotByte(uint addr, byte data)
    {
        unchecked
        {
            int slotNum = (int)((addr >> 5) & 0x1F);
            Slot slot = _slots[slotNum];
            uint offset = addr - (uint)(0x20 * slotNum);

            switch (offset)
            {
            case 0:
                slot.Regs.Kb = (data >> 3) & 1;

                if (((data >> 4) & 1) != 0)
                    KeyOnEx();

                slot.Regs.Sbctl = (data >> 1) & 3;
                slot.Regs.Ssctl = (slot.Regs.Ssctl & 1) | ((data & 1) << 1);
                break;
            case 1:
                slot.Regs.Ssctl = (slot.Regs.Ssctl & 2) | ((data >> 7) & 1);
                slot.Regs.Lpctl = (data >> 5) & 3;
                slot.Regs.Pcm8b = (data >> 4) & 1;
                slot.Regs.Sa = (slot.Regs.Sa & 0xffff) | ((data & 0xf) << 16);
                break;
            case 2:
                slot.Regs.Sa = (slot.Regs.Sa & 0xf00ff) | (data << 8);
                break;
            case 3:
                slot.Regs.Sa = (slot.Regs.Sa & 0xfff00) | data;
                break;
            case 4:
                slot.Regs.Lsa = (slot.Regs.Lsa & 0x00ff) | (data << 8);
                break;
            case 5:
                slot.Regs.Lsa = (slot.Regs.Lsa & 0xff00) | data;
                break;
            case 6:
                slot.Regs.Lea = (slot.Regs.Lea & 0x00ff) | (data << 8);
                break;
            case 7:
                slot.Regs.Lea = (slot.Regs.Lea & 0xff00) | data;
                break;
            case 8:
                slot.Regs.D2r = (data >> 3) & 0x1f;
                slot.Regs.D1r = (slot.Regs.D1r & 3) | ((data & 0x7) << 2);
                break;
            case 9:
                slot.Regs.D1r = (slot.Regs.D1r & 0x1c) | ((data >> 6) & 3);
                slot.Regs.Hold = (data >> 5) & 1;
                slot.Regs.Ar = data & 0x1f;
                break;
            case 10:
                slot.Regs.Unknown1 = (data >> 7) & 1;
                slot.Regs.Ls = (data >> 6) & 1;
                slot.Regs.Krs = (data >> 2) & 0xf;
                slot.Regs.Dl = (slot.Regs.Dl & 0x7) | ((data & 3) << 3);
                break;
            case 11:
                slot.Regs.Dl = (slot.Regs.Dl & 0x18) | ((data >> 5) & 7);
                slot.Regs.Rr = data & 0x1f;
                break;
            case 12:
                slot.Regs.Unknown2 = (data >> 2) & 3;
                slot.Regs.Si = (data >> 1) & 1;
                slot.Regs.Sd = data & 1;
                break;
            case 13:
                slot.Regs.Tl = data;
                break;
            case 14:
                slot.Regs.Mdl = (data >> 4) & 0xf;
                slot.Regs.Mdxsl = (slot.Regs.Mdxsl & 0x3) | ((data & 0xf) << 2);
                break;
            case 15:
                slot.Regs.Mdxsl = (slot.Regs.Mdxsl & 0x3C) | ((data >> 6) & 3);
                slot.Regs.Mdysl = data & 0x3f;
                break;
            case 16:
                slot.Regs.Unknown3 = (data >> 7) & 1;
                slot.Regs.Oct = (data >> 3) & 0xf;
                slot.Regs.Unknown4 = (data >> 2) & 1;
                slot.Regs.Fns = (slot.Regs.Fns & 0xff) | ((data & 3) << 8);
                break;
            case 17:
                slot.Regs.Fns = (slot.Regs.Fns & 0x300) | data;
                break;
            case 18:
                slot.Regs.Re = (data >> 7) & 1;
                slot.Regs.Lfof = (data >> 2) & 0x1f;
                slot.Regs.Plfows = data & 3;
                break;
            case 19:
                slot.Regs.Plfos = (data >> 5) & 7;
                slot.Regs.Alfows = (data >> 3) & 3;
                slot.Regs.Alfos = data & 7;
                break;
            case 20:

                break;
            case 21:
                slot.Regs.Unknown5 = (data >> 7) & 1;
                slot.Regs.Isel = (data >> 3) & 0xf;
                slot.Regs.Imxl = data & 7;
                break;
            case 22:
                slot.Regs.Disdl = (data >> 5) & 7;
                slot.Regs.Dipan = data & 0x1f;
                break;
            case 23:
                slot.Regs.Efsdl = (data >> 5) & 7;
                slot.Regs.Efpan = data & 0x1f;
                break;
            default:
                break;
            }
        }
    }

    private byte ReadNewSlotByte(uint addr)
    {
        unchecked
        {
            int slotNum = (int)((addr >> 5) & 0x1F);
            Slot slot = _slots[slotNum];
            uint offset = addr - (uint)(0x20 * slotNum);
            int data = 0;

            switch (offset)
            {
            case 0:
                data |= slot.Regs.Kb << 3;
                data |= slot.Regs.Sbctl << 1;
                data |= (slot.Regs.Ssctl >> 1) & 1;
                break;
            case 1:
                data |= (slot.Regs.Ssctl & 1) << 7;
                data |= slot.Regs.Lpctl << 5;
                data |= slot.Regs.Pcm8b << 4;
                data |= (slot.Regs.Sa & 0xf0000) >> 16;
                break;
            case 2:
                data |= (slot.Regs.Sa & 0xff00) >> 8;
                break;
            case 3:
                data |= slot.Regs.Sa & 0xff;
                break;
            case 4:
                data |= (slot.Regs.Lsa & 0xff00) >> 8;
                break;
            case 5:
                data |= slot.Regs.Lsa & 0xff;
                break;
            case 6:
                data |= (slot.Regs.Lea & 0xff00) >> 8;
                break;
            case 7:
                data |= slot.Regs.Lea & 0xff;
                break;
            case 8:
                data |= slot.Regs.D2r << 3;
                data |= slot.Regs.D1r >> 2;
                break;
            case 9:
                data |= slot.Regs.D1r << 6;
                data |= slot.Regs.Hold << 5;
                data |= slot.Regs.Ar;
                break;
            case 10:
                data |= slot.Regs.Unknown1 << 7;
                data |= slot.Regs.Ls << 6;
                data |= slot.Regs.Krs << 2;
                data |= slot.Regs.Dl >> 3;
                break;
            case 11:
                data |= slot.Regs.Dl << 5;
                data |= slot.Regs.Rr;
                break;
            case 12:
                data |= slot.Regs.Unknown2 << 2;
                data |= slot.Regs.Si << 1;
                data |= slot.Regs.Sd;
                break;
            case 13:
                data |= slot.Regs.Tl;
                break;
            case 14:
                data |= slot.Regs.Mdl << 4;
                data |= slot.Regs.Mdxsl >> 2;
                break;
            case 15:
                data |= slot.Regs.Mdxsl << 6;
                data |= slot.Regs.Mdysl;
                break;
            case 16:
                data |= slot.Regs.Unknown3 << 7;
                data |= slot.Regs.Oct << 3;
                data |= slot.Regs.Unknown4 << 2;
                data |= slot.Regs.Fns >> 8;
                break;
            case 17:
                data |= slot.Regs.Fns;
                break;
            case 18:
                data |= slot.Regs.Re << 7;
                data |= slot.Regs.Lfof << 2;
                data |= slot.Regs.Plfows;
                break;
            case 19:
                data |= slot.Regs.Plfos << 5;
                data |= slot.Regs.Alfows << 3;
                data |= slot.Regs.Alfos;
                break;
            case 20:

                break;
            case 21:
                data |= slot.Regs.Unknown5 << 7;
                data |= slot.Regs.Isel << 3;
                data |= slot.Regs.Imxl;
                break;
            case 22:
                data |= slot.Regs.Disdl << 5;
                data |= slot.Regs.Dipan;
                break;
            case 23:
                data |= slot.Regs.Efsdl << 5;
                data |= slot.Regs.Efpan;
                break;
            }

            return (byte)data;
        }
    }

    private void WriteNewSlotWord(uint addr, ushort data)
    {
        unchecked
        {
            int slotNum = (int)((addr >> 5) & 0x1F);
            Slot slot = _slots[slotNum];
            uint offset = addr - (uint)(0x20 * slotNum);

            switch (offset >> 1)
            {
            case 0:
                slot.Regs.Kb = (data >> 11) & 1;

                if ((data & (1 << 12)) != 0)
                    KeyOnEx();

                slot.Regs.Sbctl = (data >> 9) & 3;
                slot.Regs.Ssctl = (data >> 7) & 3;
                slot.Regs.Lpctl = (data >> 5) & 3;
                slot.Regs.Pcm8b = (data >> 4) & 1;
                slot.Regs.Sa = (slot.Regs.Sa & 0xffff) | ((data & 0xf) << 16);
                break;
            case 1:
                slot.Regs.Sa = (slot.Regs.Sa & 0xf0000) | data;
                break;
            case 2:
                slot.Regs.Lsa = data;
                break;
            case 3:
                slot.Regs.Lea = data;
                break;
            case 4:
                slot.Regs.D2r = data >> 11;
                slot.Regs.D1r = (data >> 6) & 0x1f;
                slot.Regs.Hold = (data >> 5) & 1;
                slot.Regs.Ar = data & 0x1f;
                break;
            case 5:
                slot.Regs.Unknown1 = (data >> 15) & 1;
                slot.Regs.Ls = (data >> 14) & 1;
                slot.Regs.Krs = (data >> 10) & 0xf;
                slot.Regs.Dl = (data >> 5) & 0x1f;
                slot.Regs.Rr = data & 0x1f;
                break;
            case 6:
                slot.Regs.Unknown2 = (data >> 10) & 3;
                slot.Regs.Si = (data >> 9) & 1;
                slot.Regs.Sd = (data >> 8) & 1;
                slot.Regs.Tl = data & 0xff;
                break;
            case 7:
                slot.Regs.Mdl = (data >> 12) & 0xf;
                slot.Regs.Mdxsl = (data >> 6) & 0x3f;
                slot.Regs.Mdysl = data & 0x3f;
                break;
            case 8:
                slot.Regs.Unknown3 = (data >> 15) & 1;
                slot.Regs.Unknown4 = (data >> 10) & 1;
                slot.Regs.Oct = (data >> 11) & 0xf;
                slot.Regs.Fns = data & 0x3ff;
                break;
            case 9:
                slot.Regs.Re = (data >> 15) & 1;
                slot.Regs.Lfof = (data >> 10) & 0x1f;
                slot.Regs.Plfows = (data >> 8) & 3;
                slot.Regs.Plfos = (data >> 5) & 7;
                slot.Regs.Alfows = (data >> 3) & 3;
                slot.Regs.Alfos = data & 7;
                break;
            case 10:
                slot.Regs.Unknown5 = (data >> 7) & 1;
                slot.Regs.Isel = (data >> 3) & 0xf;
                slot.Regs.Imxl = data & 7;
                break;
            case 11:
                slot.Regs.Disdl = (data >> 13) & 7;
                slot.Regs.Dipan = (data >> 8) & 0x1f;
                slot.Regs.Efsdl = (data >> 5) & 7;
                slot.Regs.Efpan = data & 0x1f;
                break;
            default:
                break;
            }
        }
    }

    private ushort ReadNewSlotWord(uint addr)
    {
        unchecked
        {
            int slotNum = (int)((addr >> 5) & 0x1F);
            Slot slot = _slots[slotNum];
            uint offset = addr - (uint)(0x20 * slotNum);
            int data = 0;

            switch (offset >> 1)
            {
            case 0:

                data |= slot.Regs.Kb << 11;
                data |= slot.Regs.Sbctl << 9;
                data |= slot.Regs.Ssctl << 7;
                data |= slot.Regs.Lpctl << 5;
                data |= slot.Regs.Pcm8b << 4;
                data |= (slot.Regs.Sa >> 16) & 0xf;
                break;
            case 1:
                data = slot.Regs.Sa & 0xffff;
                break;
            case 2:
                data = slot.Regs.Lsa;
                break;
            case 3:
                data = slot.Regs.Lea;
                break;
            case 4:
                data |= slot.Regs.D2r << 11;
                data |= slot.Regs.D1r << 6;
                data |= slot.Regs.Hold << 5;
                data |= slot.Regs.Ar;
                break;
            case 5:
                data |= slot.Regs.Unknown1 << 15;
                data |= slot.Regs.Ls << 14;
                data |= slot.Regs.Krs << 10;
                data |= slot.Regs.Dl << 5;
                data |= slot.Regs.Rr;
                break;
            case 6:
                data |= slot.Regs.Unknown2 << 10;
                data |= slot.Regs.Si << 9;
                data |= slot.Regs.Sd << 8;
                data |= slot.Regs.Tl;
                break;
            case 7:
                data |= slot.Regs.Mdl << 12;
                data |= slot.Regs.Mdxsl << 6;
                data |= slot.Regs.Mdysl;
                break;
            case 8:
                data |= slot.Regs.Unknown3 << 15;
                data |= slot.Regs.Oct << 11;
                data |= slot.Regs.Unknown4 << 10;
                data |= slot.Regs.Fns;
                break;
            case 9:
                data |= slot.Regs.Re << 15;
                data |= slot.Regs.Lfof << 10;
                data |= slot.Regs.Plfows << 8;
                data |= slot.Regs.Plfos << 5;
                data |= slot.Regs.Alfows << 3;
                data |= slot.Regs.Alfos;
                break;
            case 10:
                data |= slot.Regs.Unknown5 << 7;
                data |= slot.Regs.Isel << 3;
                data |= slot.Regs.Imxl;
                break;
            case 11:
                data |= slot.Regs.Disdl << 13;
                data |= slot.Regs.Dipan << 8;
                data |= slot.Regs.Efsdl << 5;
                data |= slot.Regs.Efpan;
                break;
            }
            return (ushort)data;
        }
    }

    private void GetPanning(int pan, out int panValL, out int panValR)
    {
        unchecked
        {
            if ((pan & 0x10) != 0)
            {

                panValL = 0;
                panValR = pan & 0xf;
            }
            else
            {
                panValL = pan & 0xf;
                panValR = 0;
            }
        }
    }

    private int GetSdlShift(int sdl)
    {
        unchecked
        {
            if (sdl == 0)
                return 16;
            else return (7 - sdl);
        }
    }

    public void GenerateSample(int rbp, int rbl, ref short outL, ref short outR, int mvol, short cdInL, short cdInR)
    {
        unchecked
        {
            int left = 0;
            int right = 0;

            for (int stepNum = 0; stepNum < 32; stepNum++)
            {
                int lastStep = (stepNum - 6) & 0x1f;

                Op1(_slots[stepNum]);
                Op2(_slots[(stepNum - 1) & 0x1f]);
                Op3(_slots[(stepNum - 2) & 0x1f]);
                Op4(_slots[(stepNum - 3) & 0x1f]);
                Op5(_slots[(stepNum - 4) & 0x1f]);
                Op6(_slots[(stepNum - 5) & 0x1f]);
                Op7(_slots[(stepNum - 6) & 0x1f]);

                int disdl = GetSdlShift(_slots[lastStep].Regs.Disdl);

                short disdlApplied = (short)(_slots[lastStep].State.Output >> disdl);

                short mixsInput = (short)(_slots[lastStep].State.Output >>
                    GetSdlShift(_slots[lastStep].Regs.Imxl));

                GetPanning(_slots[lastStep].Regs.Dipan, out int panValL, out int panValR);
                left += disdlApplied >> panValL;
                right += disdlApplied >> panValR;

                Dsp.SetMix((uint)_slots[lastStep].Regs.Isel, (uint)mixsInput);

            }

            Dsp.SetRblRbp((uint)rbl, (uint)rbp);
            Dsp.SetExternalInputs((uint)cdInL, (uint)cdInR);

            Dsp.Execute();

            for (int i = 0; i < 18; i++)
            {
                int efsdl = GetSdlShift(_slots[i].Regs.Efsdl);
                int efsdlApplied = (short)Dsp.GetEffectOut(i) >> efsdl;
                GetPanning(_slots[i].Regs.Efpan, out int panValL, out int panValR);
                left += efsdlApplied >> panValL;
                right += efsdlApplied >> panValR;
            }

            int mvolShift = 0xF - mvol;
            outL = (short)Math.Clamp(left >> mvolShift, short.MinValue, short.MaxValue);
            outR = (short)Math.Clamp(right >> mvolShift, short.MinValue, short.MaxValue);
        }
    }

    public void ResetNewScsp()
    {
        unchecked
        {
            int slotNum;
            Array.Clear(_soundStack);

            for (slotNum = 0; slotNum < 32; slotNum++)
            {
                _slots[slotNum] = new Slot();
                _slots[slotNum].State.Attenuation = 0x3FF;
                _slots[slotNum].State.Envelope = EnvelopeState.Release;
                _slots[slotNum].State.Num = slotNum;
            }

            FillPlfoTables();
            FillAlfoTables();
            _dsp.Reset();
            _legacyNewOutputPosition = 0;
            _legacyNewCycles = 0;
        }
    }

    private static void LegacySlotKeyOn(ref LegacySlot slot)
    {
        if (slot.EnvelopePhase == LegacyEnvelopePhase.Release)
        {
            if (slot.Pcm8Bit != 0)
            {
                slot.SampleByteBase = slot.StartAddress;
                if (slot.StartAddress + (slot.LoopEnd >> LegacyFrequencyFractionBits) > LegacyRamMask)
                    slot.LoopEnd = (LegacyRamMask - slot.StartAddress) << LegacyFrequencyFractionBits;
            }
            else
            {
                slot.SampleWordBase = slot.StartAddress & ~1u;
                if (slot.StartAddress + (slot.LoopEnd >> (LegacyFrequencyFractionBits - 1)) > LegacyRamMask)
                    slot.LoopEnd = (LegacyRamMask - slot.StartAddress) << (LegacyFrequencyFractionBits - 1);
            }

            slot.FrequencyCounter = 0;
            slot.EnvelopeCounter = LegacyEnvelopeAttackStart;
            slot.Envelope = 0;
            slot.EnvelopeStep = LegacyEnvelopeStep.Attack;
            slot.EnvelopePhase = LegacyEnvelopePhase.Attack;
            slot.EnvelopeCompare = LegacyEnvelopeAttackEnd;
            slot.NextEnvelopeEvent = LegacyAttackNext;
        }
    }

    private static void LegacySlotKeyOff(ref LegacySlot slot)
    {
        if (slot.EnvelopePhase != LegacyEnvelopePhase.Release)
        {
            if (slot.EnvelopePhase == LegacyEnvelopePhase.Attack)
                slot.EnvelopeCounter = LegacyEnvelopeDecayEnd - slot.EnvelopeCounter;
            slot.EnvelopeStep = LegacyEnvelopeStep.Release;
            slot.EnvelopeCompare = LegacyEnvelopeDecayEnd;
            slot.EnvelopePhase = LegacyEnvelopePhase.Release;
            slot.NextEnvelopeEvent = LegacyReleaseNext;
        }
    }

    private void LegacySlotKeyOnOff()
    {
        for (int i = 0; i < 32; i++)
        {
            ref LegacySlot slot = ref _legacySlots[i];
            if (slot.Key != 0)
                LegacySlotKeyOn(ref slot);
            else
                LegacySlotKeyOff(ref slot);
        }
    }

    private static void LegacyEnvelopeNullNext(ref LegacySlot slot)
    {
    }

    private static void LegacyReleaseNext(ref LegacySlot slot)
    {
        slot.EnvelopeCounter = LegacyEnvelopeDecayEnd;
        slot.EnvelopeStep = LegacyEnvelopeStep.None;
        slot.EnvelopeCompare = LegacyEnvelopeDecayEnd + 1;
        slot.NextEnvelopeEvent = LegacyEnvelopeNullNext;
    }

    private static void LegacySustainNext(ref LegacySlot slot)
    {
        slot.EnvelopeCounter = LegacyEnvelopeDecayEnd;
        slot.EnvelopeStep = LegacyEnvelopeStep.None;
        slot.EnvelopeCompare = LegacyEnvelopeDecayEnd + 1;
        slot.NextEnvelopeEvent = LegacyEnvelopeNullNext;
    }

    private static void LegacyDecayNext(ref LegacySlot slot)
    {
        slot.EnvelopeCounter = slot.SustainLevel;
        slot.EnvelopeStep = LegacyEnvelopeStep.Sustain;
        slot.EnvelopeCompare = LegacyEnvelopeDecayEnd;
        slot.EnvelopePhase = LegacyEnvelopePhase.Sustain;
        slot.NextEnvelopeEvent = LegacySustainNext;
    }

    private static void LegacyAttackNext(ref LegacySlot slot)
    {
        slot.EnvelopeCounter = LegacyEnvelopeDecayStart;
        slot.EnvelopeStep = LegacyEnvelopeStep.Decay;
        slot.EnvelopeCompare = slot.SustainLevel;
        slot.EnvelopePhase = LegacyEnvelopePhase.Decay;
        slot.NextEnvelopeEvent = LegacyDecayNext;
    }

    private static void LegacySlotRefreshEnvelopeIncrement(ref LegacySlot slot, uint adsrMask)
    {
        int index = (14 - slot.FrequencyShift) >> (int)slot.KeyRateScale;
        if (slot.AttackRatePointer.Table is { } attackRates && (adsrMask & 1) != 0)
            slot.AttackIncrement = attackRates[slot.AttackRatePointer.Offset + index];
        if (slot.DecayRatePointer.Table is { } decayRates && (adsrMask & 2) != 0)
            slot.DecayIncrement = decayRates[slot.DecayRatePointer.Offset + index];
        if (slot.SustainRatePointer.Table is { } sustainRates && (adsrMask & 4) != 0)
            slot.SustainIncrement = sustainRates[slot.SustainRatePointer.Offset + index];
        if (slot.ReleaseRatePointer.Table is { } releaseRates && (adsrMask & 8) != 0)
            slot.ReleaseIncrement = releaseRates[slot.ReleaseRatePointer.Offset + index];
    }

    private void WriteLegacySlotByte(uint slotIndex, uint address, byte data)
    {
        ref LegacySlot slot = ref _legacySlots[slotIndex];

        _registerRam[address ^ 3] = data;

        switch (address & 0x1F)
        {
            case 0x00: // KX/KB/SBCTL/SSCTL(high bit)
                slot.Key = (byte)((data >> 3) & 1);
                slot.SourceBitControl = (byte)((data >> 1) & 3);
                slot.SoundSourceControl = (byte)((slot.SoundSourceControl & 1) + ((data & 1) << 1));

                if ((data & 0x10) != 0) LegacySlotKeyOnOff();
                return;

            case 0x01: // SSCTL(low bit)/LPCTL/8B/SA(highest 4 bits)
                slot.SoundSourceControl = (byte)((slot.SoundSourceControl & 2) + ((data >> 7) & 1));
                slot.LoopControl = (byte)((data >> 5) & 3);

                slot.Pcm8Bit = (byte)(data & 0x10);
                slot.StartAddress = (uint)((slot.StartAddress & 0x0FFFF) + ((data & 0xF) << 16));
                slot.StartAddress &= LegacyRamMask;

                if (slot.EnvelopeCounter < LegacyEnvelopeDecayEnd) LegacySlotUpdateKeyOn(ref slot);

                return;

            case 0x02: // SA(next highest byte)
                slot.StartAddress = (uint)((slot.StartAddress & 0xF00FF) + (data << 8));
                slot.StartAddress &= LegacyRamMask;

                if (slot.EnvelopeCounter < LegacyEnvelopeDecayEnd) LegacySlotUpdateKeyOn(ref slot);
                return;

            case 0x03: // SA(low byte)
                slot.StartAddress = (uint)((slot.StartAddress & 0xFFF00) + data);
                slot.StartAddress &= LegacyRamMask;

                if (slot.EnvelopeCounter < LegacyEnvelopeDecayEnd) LegacySlotUpdateKeyOn(ref slot);
                return;

            case 0x04: // LSA(high byte)
                slot.LoopStart = (uint)((slot.LoopStart & (0x00FF << LegacyFrequencyFractionBits)) + (data << (8 + LegacyFrequencyFractionBits)));
                return;

            case 0x05: // LSA(low byte)
                slot.LoopStart = (uint)((slot.LoopStart & (0xFF00 << LegacyFrequencyFractionBits)) + (data << LegacyFrequencyFractionBits));
                return;

            case 0x06: // LEA(high byte)
                slot.LoopEnd = (uint)((slot.LoopEnd & (0x00FF << LegacyFrequencyFractionBits)) + (data << (8 + LegacyFrequencyFractionBits)));
                return;

            case 0x07: // LEA(low byte)
                slot.LoopEnd = (uint)((slot.LoopEnd & (0xFF00 << LegacyFrequencyFractionBits)) + (data << LegacyFrequencyFractionBits));
                return;

            case 0x08: // D2R/D1R(highest 3 bits)
                slot.SustainRate = (data >> 3) & 0x1F;
                slot.DecayRate = (slot.DecayRate & 0x03) + ((data & 7) << 2);

                if (slot.SustainRate != 0)
                    slot.SustainRatePointer = new LegacyRatePointer(_legacyDecayRate, slot.SustainRate << 1);
                else
                    slot.SustainRatePointer = new LegacyRatePointer(_legacyNullRate, 0);

                if (slot.DecayRate != 0)
                    slot.DecayRatePointer = new LegacyRatePointer(_legacyDecayRate, slot.DecayRate << 1);
                else
                    slot.DecayRatePointer = new LegacyRatePointer(_legacyNullRate, 0);

                LegacySlotRefreshEnvelopeIncrement(ref slot, 0x2 | 0x4);
                return;

            case 0x09: // D1R(lowest 2 bits)/EGHOLD/AR
                slot.DecayRate = (slot.DecayRate & 0x1C) + ((data >> 6) & 3);
                slot.EnvelopeHold = (byte)(data & 0x20);
                slot.AttackRate = data & 0x1F;

                if (slot.DecayRate != 0)
                    slot.DecayRatePointer = new LegacyRatePointer(_legacyDecayRate, slot.DecayRate << 1);
                else
                    slot.DecayRatePointer = new LegacyRatePointer(_legacyNullRate, 0);

                if (slot.AttackRate != 0)
                    slot.AttackRatePointer = new LegacyRatePointer(_legacyAttackRate, slot.AttackRate << 1);
                else
                    slot.AttackRatePointer = new LegacyRatePointer(_legacyNullRate, 0);

                LegacySlotRefreshEnvelopeIncrement(ref slot, 0x1 | 0x2);
                return;

            case 0x0A: // LPSLNK/KRS/DL(highest 2 bits)
                slot.LoopStartLink = (byte)(data & 0x40);
                slot.KeyRateScale = (uint)((data >> 2) & 0xF);

                if (slot.KeyRateScale == 0xF)
                    slot.KeyRateScale = 4;
                else
                    slot.KeyRateScale >>= 2;

                slot.SustainLevel &= 0xE0 << LegacyEnvelopeFractionBits;
                slot.SustainLevel += (data & 3) << (8 + LegacyEnvelopeFractionBits);
                slot.SustainLevel += LegacyEnvelopeDecayStart; // adjusted for envelope compare (ecmp)

                LegacySlotRefreshEnvelopeIncrement(ref slot, 0xF);
                return;

            case 0x0B: // DL(lowest 3 bits)/RR
                slot.SustainLevel &= 0x300 << LegacyEnvelopeFractionBits;
                slot.SustainLevel += (data & 0xE0) << LegacyEnvelopeFractionBits;
                slot.SustainLevel += LegacyEnvelopeDecayStart; // adjusted for envelope compare (ecmp)
                slot.ReleaseRate = data & 0x1F;

                if (slot.ReleaseRate != 0)
                    slot.ReleaseRatePointer = new LegacyRatePointer(_legacyDecayRate, slot.ReleaseRate << 1);
                else
                    slot.ReleaseRatePointer = new LegacyRatePointer(_legacyNullRate, 0);

                LegacySlotRefreshEnvelopeIncrement(ref slot, 0x8);
                return;

            case 0x0C: // STWINH/SDIR
                slot.SoundDirect = (byte)(data & 2);
                slot.StackWriteEnable = (byte)(data & 1);
                return;

            case 0x0D: // TL
                slot.TotalLevel = _legacyTotalLevel[(data & 0xFF)];
                return;

            case 0x0E: // MDL/MDXSL(highest 4 bits)
                slot.ModulationLevel = (byte)((data >> 4) & 0xF); // need to adjust for correct shift
                slot.ModulationSourceX = (byte)((slot.ModulationSourceX & 3) + ((data & 0xF) << 2));
                return;

            case 0x0F: // MDXSL(lowest 2 bits)/MDYSL
                slot.ModulationSourceX = (byte)((slot.ModulationSourceX & 0x3C) + ((data >> 6) & 3));
                slot.ModulationSourceY = (byte)(data & 0x3F);
                return;

            case 0x10: // OCT/FNS(highest 2 bits)
                if ((data & 0x40) != 0)
                    slot.FrequencyShift = (byte)(23 - ((data >> 3) & 0xF));
                else
                    slot.FrequencyShift = (byte)(((data >> 3) & 7) ^ 7);

                slot.UnadjustedFrequencyIncrement = (uint)((slot.UnadjustedFrequencyIncrement & 0x7F80) + ((data & 3) << (8 + 7)));
                slot.FrequencyIncrement = (uint)((0x20000 + slot.UnadjustedFrequencyIncrement) >> slot.FrequencyShift);

                LegacySlotRefreshEnvelopeIncrement(ref slot, 0xF);
                return;

            case 0x11: // FNS(low byte)
                slot.UnadjustedFrequencyIncrement = (uint)((slot.UnadjustedFrequencyIncrement & 0x18000) + (data << 7));
                slot.FrequencyIncrement = (uint)((0x20000 + slot.UnadjustedFrequencyIncrement) >> slot.FrequencyShift);
                return;

            case 0x12: // LFORE/LFOF/PLFOWS
                if ((data & 0x80) != 0)
                {
                slot.LfoIncrement = -1;
                return;
                }
                else if (slot.LfoIncrement == -1)
                {
                slot.LfoCounter = 0;
                }

                slot.LfoIncrement = _legacyLfoStep[(data >> 2) & 0x1F];

                switch (data & 3)
                {
                case 0:
                    slot.LfoFrequencyWave = _legacyLfoSawFrequency;
                    return;

                case 1:
                    slot.LfoFrequencyWave = _legacyLfoSquareFrequency;
                    return;

                case 2:
                    slot.LfoFrequencyWave = _legacyLfoTriangleFrequency;
                    return;

                case 3:
                    slot.LfoFrequencyWave = _legacyLfoNoiseFrequency;
                    return;
                }
                return;

            case 0x13: // PLFOS/ALFOWS/ALFOS
                if (((data >> 5) & 7) != 0)
                    slot.LfoFrequencySensitivity = (byte)(((data >> 5) & 7) + 7);
                else
                    slot.LfoFrequencySensitivity = 31;

                if ((data & 7) != 0)
                    slot.LfoEnvelopeSensitivity = (byte)(((data & 7) ^ 7) + 4);
                else
                    slot.LfoEnvelopeSensitivity = 31;

                switch ((data >> 3) & 3)
                {
                case 0:
                    slot.LfoEnvelopeWave = _legacyLfoSawEnvelope;
                    return;

                case 1:
                    slot.LfoEnvelopeWave = _legacyLfoSquareEnvelope;
                    return;

                case 2:
                    slot.LfoEnvelopeWave = _legacyLfoTriangleEnvelope;
                    return;

                case 3:
                    slot.LfoEnvelopeWave = _legacyLfoNoiseEnvelope;
                    break;
                }
                return;

            case 0x15: // ISEL/OMXL
                if ((data & 7) != 0)
                    slot.InputMixLevel = (byte)(((data & 7) ^ 7) + 10);
                else
                    slot.InputMixLevel = 31;
                return;

            case 0x16: // DISDL/DIPAN
                if ((data & 0xE0) != 0)
                {
                // adjusted for envelope calculation
                // some inaccuracy in panning though...
                slot.DirectLevelRight = slot.DirectLevelLeft = (byte)((((data >> 5) & 7) ^ 7) + 10);
                if ((data & 0x10) != 0)
                {
                    // Panning Left
                    if ((data & 0xF) == 0xF)
                        slot.DirectLevelRight = 31;
                    else
                        slot.DirectLevelRight = (byte)(slot.DirectLevelRight + ((data >> 1) & 7));
                }
                else
                {
                    // Panning Right
                    if ((data & 0xF) == 0xF)
                        slot.DirectLevelLeft = 31;
                    else
                        slot.DirectLevelLeft = (byte)(slot.DirectLevelLeft + ((data >> 1) & 7));
                }
                }
                else
                {
                slot.DirectLevelRight = slot.DirectLevelLeft = (byte)(31); // muted
                }
                return;

            case 0x17: // EFSDL/EFPAN
                if ((data & 0xE0) != 0)
                {
                slot.EffectLevelRight = slot.EffectLevelLeft = (byte)((((data >> 5) & 7) ^ 7) + 10);
                if ((data & 0x10) != 0)
                {
                    // Panning Left
                    if ((data & 0xF) == 0xF)
                        slot.EffectLevelRight = 31;
                    else
                        slot.EffectLevelRight = (byte)(slot.EffectLevelRight + ((data >> 1) & 7));
                }
                else
                {
                    // Panning Right
                    if ((data & 0xF) == 0xF)
                        slot.EffectLevelLeft = 31;
                    else
                        slot.EffectLevelLeft = (byte)(slot.EffectLevelLeft + ((data >> 1) & 7));
                }
                }
                else
                {
                slot.EffectLevelRight = slot.EffectLevelLeft = (byte)(31); // muted
                }
                return;
        }
    }

    private void WriteLegacySlotWord(uint slotIndex, uint address, ushort data)
    {
        ref LegacySlot slot = ref _legacySlots[slotIndex];

        BitConverter.TryWriteBytes(_registerRam.AsSpan((int)(address ^ 2)), data);

        switch (address & 0x1E)
        {
            case 0x00: // KYONEX/KYONB/SBCTL/SSCTL/LPCTL/PCM8B/SA(highest 4 bits)
                slot.Key = (byte)((data >> 11) & 1);
                slot.SourceBitControl = (byte)((data >> 9) & 3);
                slot.SoundSourceControl = (byte)((data >> 7) & 3);
                slot.LoopControl = (byte)((data >> 5) & 3);

                slot.Pcm8Bit = (byte)(data & 0x10);
                slot.StartAddress = (slot.StartAddress & 0x0FFFFu) | (((uint)data & 0xFu) << 16);
                slot.StartAddress &= LegacyRamMask;

                if (slot.EnvelopeCounter < LegacyEnvelopeDecayEnd)
                    LegacySlotUpdateKeyOn(ref slot);

                if ((data & 0x1000) != 0)
                    LegacySlotKeyOnOff();
                return;

            case 0x02: // SA(low word)
                slot.StartAddress = (uint)((slot.StartAddress & 0xF0000) | data);
                slot.StartAddress &= LegacyRamMask;

                if (slot.EnvelopeCounter < LegacyEnvelopeDecayEnd)
                    LegacySlotUpdateKeyOn(ref slot);
                return;

            case 0x04: // LSA
                slot.LoopStart = (uint)(data << LegacyFrequencyFractionBits);
                return;

            case 0x06: // LEA
                slot.LoopEnd = (uint)(data << LegacyFrequencyFractionBits);
                return;

            case 0x08: // D2R/D1R/EGHOLD/AR
                slot.SustainRate = (data >> 11) & 0x1F;
                slot.DecayRate = (data >> 6) & 0x1F;
                slot.EnvelopeHold = (byte)(data & 0x20);
                slot.AttackRate = data & 0x1F;

                if (slot.SustainRate != 0)
                    slot.SustainRatePointer = new LegacyRatePointer(_legacyDecayRate, slot.SustainRate << 1);
                else
                    slot.SustainRatePointer = new LegacyRatePointer(_legacyNullRate, 0);

                if (slot.DecayRate != 0)
                    slot.DecayRatePointer = new LegacyRatePointer(_legacyDecayRate, slot.DecayRate << 1);
                else
                    slot.DecayRatePointer = new LegacyRatePointer(_legacyNullRate, 0);

                if (slot.AttackRate != 0)
                    slot.AttackRatePointer = new LegacyRatePointer(_legacyAttackRate, slot.AttackRate << 1);
                else
                    slot.AttackRatePointer = new LegacyRatePointer(_legacyNullRate, 0);

                LegacySlotRefreshEnvelopeIncrement(ref slot, 0x1 | 0x2 | 0x4);
                return;

            case 0x0A: // LPSLNK/KRS/DL/RR
                slot.LoopStartLink = (byte)((data >> 8) & 0x40);
                slot.KeyRateScale = (uint)((data >> 10) & 0xF);

                if (slot.KeyRateScale == 0xF)
                    slot.KeyRateScale = 4;
                else
                    slot.KeyRateScale >>= 2;

                slot.SustainLevel = ((data & 0x3E0) << LegacyEnvelopeFractionBits) + LegacyEnvelopeDecayStart; // adjusted for envelope compare (ecmp)
                slot.ReleaseRate = data & 0x1F;

                if (slot.ReleaseRate != 0)
                    slot.ReleaseRatePointer = new LegacyRatePointer(_legacyDecayRate, slot.ReleaseRate << 1);
                else
                    slot.ReleaseRatePointer = new LegacyRatePointer(_legacyNullRate, 0);

                LegacySlotRefreshEnvelopeIncrement(ref slot, 0xF);
                return;

            case 0x0C: // STWINH/SDIR
                slot.SoundDirect = (byte)((data >> 8) & 2);
                slot.StackWriteEnable = (byte)((data >> 8) & 1);
                slot.TotalLevel = _legacyTotalLevel[(data & 0xFF)];
                return;

            case 0x0E: // MDL/MDXSL/MDYSL
                slot.ModulationLevel = (byte)((data >> 12) & 0xF); // need to adjust for correct shift
                slot.ModulationSourceX = (byte)((data >> 6) & 0x3F);
                slot.ModulationSourceY = (byte)(data & 0x3F);
                return;

            case 0x10: // OCT/FNS
                if ((data & 0x4000) != 0)
                    slot.FrequencyShift = (byte)(23 - ((data >> 11) & 0xF));
                else
                    slot.FrequencyShift = (byte)((((data >> 11) & 7) ^ 7));

                slot.FrequencyIncrement = (uint)(((0x400 + (data & 0x3FF)) << 7) >> slot.FrequencyShift);

                LegacySlotRefreshEnvelopeIncrement(ref slot, 0xF);
                return;

            case 0x12: // LFORE/LFOF/PLFOWS/PLFOS/ALFOWS/ALFOS
                if ((data & 0x8000) != 0)
                {
                slot.LfoIncrement = -1;
                return;
                }
                else if (slot.LfoIncrement == -1)
                {
                slot.LfoCounter = 0;
                }

                slot.LfoIncrement = _legacyLfoStep[(data >> 10) & 0x1F];
                if (((data >> 5) & 7) != 0)
                    slot.LfoFrequencySensitivity = (byte)(((data >> 5) & 7) + 7);
                else
                    slot.LfoFrequencySensitivity = 31;

                if ((data & 7) != 0)
                    slot.LfoEnvelopeSensitivity = (byte)(((data & 7) ^ 7) + 4);
                else
                    slot.LfoEnvelopeSensitivity = 31;

                switch ((data >> 8) & 3)
                {
                case 0:
                    slot.LfoFrequencyWave = _legacyLfoSawFrequency;
                    break;

                case 1:
                    slot.LfoFrequencyWave = _legacyLfoSquareFrequency;
                    break;

                case 2:
                    slot.LfoFrequencyWave = _legacyLfoTriangleFrequency;
                    break;

                case 3:
                    slot.LfoFrequencyWave = _legacyLfoNoiseFrequency;
                    break;
                }

                switch ((data >> 3) & 3)
                {
                case 0:
                    slot.LfoEnvelopeWave = _legacyLfoSawEnvelope;
                    return;

                case 1:
                    slot.LfoEnvelopeWave = _legacyLfoSquareEnvelope;
                    return;

                case 2:
                    slot.LfoEnvelopeWave = _legacyLfoTriangleEnvelope;
                    return;

                case 3:
                    slot.LfoEnvelopeWave = _legacyLfoNoiseEnvelope;
                    break;
                }
                return;

            case 0x14: // ISEL/OMXL
                if ((data & 7) != 0)
                    slot.InputMixLevel = (byte)(((data & 7) ^ 7) + 10);
                else
                    slot.InputMixLevel = 31;
                return;

            case 0x16: // DISDL/DIPAN/EFSDL/EFPAN
                if ((data & 0xE000) != 0)
                {
                // adjusted fr enveloppe calculation
                // some accuracy lose for panning here...
                slot.DirectLevelRight = slot.DirectLevelLeft = (byte)((((data >> 13) & 7) ^ 7) + 10);
                if ((data & 0x1000) != 0)
                {
                    // Panning Left
                    if ((data & 0xF00) == 0xF00)
                        slot.DirectLevelRight = 31;
                    else
                        slot.DirectLevelRight = (byte)(slot.DirectLevelRight + ((data >> 9) & 7));
                }
                else
                {
                    // Panning Right
                    if ((data & 0xF00) == 0xF00)
                        slot.DirectLevelLeft = 31;
                    else
                        slot.DirectLevelLeft = (byte)(slot.DirectLevelLeft + ((data >> 9) & 7));
                }
                }
                else
                {
                slot.DirectLevelRight = slot.DirectLevelLeft = (byte)(31); // muted
                }

                if ((data & 0xE0) != 0)
                {
                slot.EffectLevelRight = slot.EffectLevelLeft = (byte)((((data >> 5) & 7) ^ 7) + 10);
                if ((data & 0x10) != 0)
                {
                    // Panning Left
                    if ((data & 0xF) == 0xF)
                        slot.EffectLevelRight = 31;
                    else
                        slot.EffectLevelRight = (byte)(slot.EffectLevelRight + ((data >> 1) & 7));
                }
                else
                {
                    // Panning Right
                    if ((data & 0xF) == 0xF)
                        slot.EffectLevelLeft = 31;
                    else
                        slot.EffectLevelLeft = (byte)(slot.EffectLevelLeft + ((data >> 1) & 7));
                }
                }
                else
                {
                slot.EffectLevelRight = slot.EffectLevelLeft = (byte)(31); // muted
                }
                return;
        }
    }

    private static void LegacySlotUpdateKeyOn(ref LegacySlot slot)
    {
        if (slot.Pcm8Bit != 0)
        {
            slot.SampleByteBase = slot.StartAddress;
            if (slot.StartAddress + (slot.LoopEnd >> LegacyFrequencyFractionBits) > LegacyRamMask)
                slot.LoopEnd = (LegacyRamMask - slot.StartAddress) << LegacyFrequencyFractionBits;
        }
        else
        {
            slot.SampleWordBase = slot.StartAddress & ~1u;
            if (slot.StartAddress + (slot.LoopEnd >> (LegacyFrequencyFractionBits - 1)) > LegacyRamMask)
                slot.LoopEnd = (LegacyRamMask - slot.StartAddress) << (LegacyFrequencyFractionBits - 1);
        }

        slot.FrequencyCounter = unchecked(slot.FrequencyCounter + slot.FrequencyIncrement);
        if (slot.FrequencyCounter > slot.LoopEnd)
        {
            if (slot.LoopControl != 0)
                slot.FrequencyCounter = slot.LoopStart;
            else
                slot.EnvelopeCounter = LegacyEnvelopeDecayEnd;
        }
    }

    private byte ReadLegacySlotByte(uint address)
    {
        byte value = _registerRam[address ^ 3];
        if ((address & 0x1F) == 0)
            value &= 0xEF;
        return value;
    }

    private ushort ReadLegacySlotWord(uint address)
    {
        ushort value = BitConverter.ToUInt16(_registerRam.AsSpan((int)(address ^ 2)));
        if ((address & 0x1E) == 0)
            return value &= 0xEFFF;
        return value;
    }

    private void WriteCommonByte(uint address, byte data)
    {
        address &= 0x3F;
        if (_useScsp2)
        {
            WriteScsp2CommonByte(address, data);
            return;
        }

        _registerRam[0x400 + (address ^ 3)] = data;
        switch (address)
        {
            case 0x00:
                _mem4b = (uint)((data >> 1) & 1);
                UpdateSoundCpuFetch();
                return;

            case 0x01:
                _masterVolume = data & 0xF;
                return;

            case 0x02:
                _ringLength = (_ringLength & 1) + ((data & 1) << 1);
                return;

            case 0x03:
                _ringLength = (_ringLength & 2) + ((data >> 7) & 1);
                _ringBase = _useNewScsp ? data & 0x7F : (data & 0x7F) * (4 * 1024 * 2);
                return;

            case 0x07:
                SendMidiOutput(data);
                return;

            case 0x08:
                _monitorSlot = (data >> 3) & 0x1F;
                UpdateMonitor();
                return;

            case 0x12:
                _dmaMemoryAddress = (_dmaMemoryAddress & 0xF00FE) + (data << 8);
                return;

            case 0x13:
                _dmaMemoryAddress = (_dmaMemoryAddress & 0xFFF00) + (data & 0xFE);
                return;

            case 0x14:
                _dmaMemoryAddress = (_dmaMemoryAddress & 0xFFFE) + ((data & 0xF0) << 12);
                _dmaRegisterAddress = (_dmaRegisterAddress & 0xFE) + ((data & 0xF) << 8);
                return;

            case 0x15:
                _dmaRegisterAddress = (_dmaRegisterAddress & 0xF00) + (data & 0xFE);
                return;

            case 0x16:
                _dmaLength = (_dmaLength & 0xFE) + ((data & 0xF) << 8);
                _dmaFlags = data & 0xF0;
                if ((_dmaFlags & 0x10) != 0) CompleteLegacyDma();
                return;

            case 0x17:
                _dmaLength = (_dmaLength & 0xF00) + (data & 0xFE);
                return;

            case 0x18:
            case 0x1A:
            case 0x1C:
                _timers[(address - 0x18) >> 1].Control = data & 7;
                return;

            case 0x19:
            case 0x1B:
            case 0x1D:
                _timers[(address - 0x18) >> 1].Counter = (uint)data << 8;
                return;

            case 0x1E:
                _soundInterruptEnable = (_soundInterruptEnable & 0xFF) | (data << 8);
                for (int i = 0; i < 3; i++)
                {
                    if ((_soundInterruptEnable & _soundInterruptPending & (1 << i)) != 0)
                        TriggerSoundInterrupt(1 << (i + 8));
                }
                return;

            case 0x1F:
                _soundInterruptEnable = (_soundInterruptEnable & 0x700) | data;
                CheckSoundInterrupts(0xFF);
                return;

            case 0x21:
                if ((data & 0x20) != 0) SoundInterrupt(0x20);
                return;

            case 0x22:
                _soundInterruptPending &= ~(data << 8);
                return;

            case 0x23:
                _soundInterruptPending &= ~data;
                return;

            case 0x25:
                _soundInterruptLevel0 = data;
                return;

            case 0x27:
                _soundInterruptLevel1 = data;
                return;

            case 0x29:
                _soundInterruptLevel2 = data;
                return;

            case 0x2A:
                _mainInterruptEnable = (_mainInterruptEnable & 0xFF) | (data << 8);
                return;

            case 0x2B:
                _mainInterruptEnable = (_mainInterruptEnable & 0x700) | data;
                return;

            case 0x2D:
                if ((data & 0x20) != 0) MainInterrupt(0x20);
                return;

            case 0x2E:
                _mainInterruptPending &= ~(data << 8);
                return;

            case 0x2F:
                _mainInterruptPending &= ~data;
                return;
        }
    }

    private void WriteCommonWord(uint address, ushort data)
    {
        address &= 0x3E;
        if (_useScsp2)
        {
            WriteScsp2CommonWord(address, data);
            return;
        }

        BitConverter.TryWriteBytes(_registerRam.AsSpan(0x400 + (int)(address ^ 2)), data);

        switch (address)
        {
            case 0x00:
                _mem4b = (uint)((data >> 9) & 1);
                _masterVolume = data & 0xF;
                UpdateSoundCpuFetch();
                return;

            case 0x02:
                _ringLength = (data >> 7) & 3;
                _ringBase = _useNewScsp ? data & 0x7F : (data & 0x7F) * (4 * 1024 * 2);
                return;

            case 0x06:
                SendMidiOutput((byte)(data & 0xFF));
                return;

            case 0x08:
                _monitorSlot = (data >> 11) & 0x1F;
                UpdateMonitor();
                return;

            case 0x12:
                _dmaMemoryAddress = (_dmaMemoryAddress & 0xF0000) + (data & 0xFFFE);
                return;

            case 0x14:
                _dmaMemoryAddress = (_dmaMemoryAddress & 0xFFFE) + ((data & 0xF000) << 4);
                _dmaRegisterAddress = data & 0xFFE;
                return;

            case 0x16:
                _dmaLength = data & 0xFFE;
                _dmaFlags = (data >> 8) & 0xF0;
                if ((_dmaFlags & 0x10) != 0)
                    CompleteLegacyDma();
                return;

            case 0x18:
            case 0x1A:
            case 0x1C:
                ref Timer timer = ref _timers[(address - 0x18) >> 1];
                timer.Control = (data >> 8) & 7;
                timer.Counter = (uint)(data & 0xFF) << 8;
                return;

            case 0x1E:
                _soundInterruptEnable = data;
                CheckSoundInterrupts(0x7FF);
                return;

            case 0x20:
                if ((data & 0x20) != 0) SoundInterrupt(0x20);
                return;

            case 0x22:
                _soundInterruptPending &= ~data;
                return;

            case 0x24:
                _soundInterruptLevel0 = data;
                return;

            case 0x26:
                _soundInterruptLevel1 = data;
                return;

            case 0x28:
                _soundInterruptLevel2 = data;
                return;

            case 0x2A:
                _mainInterruptEnable = data;
                CheckMainInterrupts(0x7FF);
                return;

            case 0x2C:
                if ((data & 0x20) != 0) MainInterrupt(0x20);
                return;

            case 0x2E:
                _mainInterruptPending &= ~data;
                return;
        }
    }

    private byte ReadCommonByte(uint address)
    {
        address &= 0x3F;
        if (_useScsp2)
        {
            ushort data = ReadScsp2CommonWord(address & ~1u);
            return (address & 1) != 0 ? unchecked((byte)data) : (byte)(data >> 8);
        }

        switch (address)
        {
            case 0x01:
                _registerRam[0x400 + (address ^ 3)] &= 0x0F;
                break;
            case 0x04: return (byte)_midiFlags;
            case 0x05: return ReadMidiInput();
            case 0x07: return ReadMidiOutput();
            case 0x08: return unchecked((byte)(_callAddress >> 8));
            case 0x09: return unchecked((byte)((_callAddress & 0xE0) | (_envelopePhase << 5) | _envelopeLevel));
            case 0x12:
            case 0x13:
            case 0x14:
            case 0x15:
            case 0x17: return 0;
            case 0x16: return (byte)(_dmaFlags & 0x70);
            case 0x1E: return unchecked((byte)(_soundInterruptEnable >> 8));
            case 0x1F: return unchecked((byte)_soundInterruptEnable);
            case 0x20: return unchecked((byte)(_soundInterruptPending >> 8));
            case 0x21: return unchecked((byte)_soundInterruptPending);
            case 0x2C: return unchecked((byte)(_mainInterruptPending >> 8));
            case 0x2D: return unchecked((byte)_mainInterruptPending);
        }

        return _registerRam[0x400 + (address ^ 3)];
    }

    private ushort ReadCommonWord(uint address)
    {
        address &= 0x3E;
        if (_useScsp2)
            return ReadScsp2CommonWord(address);

        int offset = 0x400 + (int)(address ^ 2);
        switch (address)
        {
            case 0x00:
                BitConverter.TryWriteBytes(_registerRam.AsSpan(offset), (ushort)(BitConverter.ToUInt16(_registerRam.AsSpan(offset)) & 0xFF0F));
                break;
            case 0x04:
            {
                int data = _midiFlags << 8;
                data |= ReadMidiInput();
                return (ushort)data;
            }
            case 0x06: return ReadMidiOutput();
            case 0x08: return unchecked((ushort)((_callAddress & 0x780) | (_envelopePhase << 5) | _envelopeLevel));
            case 0x12:
            case 0x14: return 0;
            case 0x16: return (ushort)((_dmaFlags & 0x70) << 8);
            case 0x18:
            case 0x1A:
            case 0x1C:
                return (ushort)(_timers[(address - 0x18) >> 1].Control << 8);
            case 0x1E: return unchecked((ushort)_soundInterruptEnable);
            case 0x20: return unchecked((ushort)_soundInterruptPending);
            case 0x2C: return unchecked((ushort)_mainInterruptPending);
        }

        return BitConverter.ToUInt16(_registerRam.AsSpan(offset));
    }

    private void TriggerSoundInterrupt(int id)
    {
        int level = 0;
        if (id > 0x80)
            id = 0x80;
        if ((_soundInterruptLevel0 & id) != 0)
            level |= 1;
        if ((_soundInterruptLevel1 & id) != 0)
            level |= 2;
        if ((_soundInterruptLevel2 & id) != 0)
            level |= 4;
        _soundCpu.SetInterruptLevel(level);
    }

    private void CheckSoundInterrupts(int mask)
    {
        for (int i = 0; i < 11; i++)
        {
            int id = 1 << i;
            if (_useScsp2)
            {
                if ((id & mask & _soundInterruptEnable) != 0 && _soundInterruptPending != 0)
                    SoundInterrupt(id);
            }
            else if ((id & mask & _soundInterruptEnable & _soundInterruptPending) != 0)
            {
                TriggerSoundInterrupt(id);
            }
        }
    }

    private void CheckMainInterrupts(int mask)
    {
        for (int i = 0; i < 11; i++)
        {
            int id = 1 << i;
            if (_useScsp2)
            {
                if ((id & mask & _mainInterruptEnable) != 0 && _mainInterruptPending != 0)
                    MainInterrupt(id);
            }
            else if ((_mainInterruptEnable & id) != 0 && (_mainInterruptPending & id) != 0)
            {
                _registers.SendMainInterrupt();
            }
        }
    }

    private void MainInterrupt(int id)
    {
        _mainInterruptPending |= id;
        if ((_mainInterruptEnable & id) != 0)
        {
            if (_useScsp2)
                RequestScsp2MainInterrupt();
            else
                _registers.SendMainInterrupt();
        }
    }

    private void SoundInterrupt(int id)
    {
        _soundInterruptPending |= id;
        if ((_soundInterruptEnable & id) != 0)
            TriggerSoundInterrupt(id);
    }

    // Transfer loop from Mednafen scsp.inc, SS_SCSP::RunDMA.
    private void CompleteLegacyDma()
    {
        if (_dmaActive)
            return;

        uint memoryAddress = (uint)_dmaMemoryAddress;
        uint registerAddress = (uint)_dmaRegisterAddress;
        int length = _dmaLength;
        bool registersToRam = (_dmaFlags & 0x20) != 0;
        bool gate = (_dmaFlags & 0x40) != 0;

        _dmaActive = true;
        try
        {
            while (length != 0)
            {
                if (registersToRam)
                {
                    ushort value = ReadWord(registerAddress);
                    if (gate)
                        value = 0;

                    if (memoryAddress < _soundRam.Length)
                    {
                        T2WriteWord(_soundRam, memoryAddress, value);
                        _soundCpu.WriteNotify(memoryAddress, 2);
                    }
                }
                else
                {
                    ushort value = memoryAddress < _soundRam.Length ? T2ReadWord(_soundRam, memoryAddress) : (ushort)0;
                    if (gate)
                        value = 0;

                    WriteWordDirect(registerAddress, value);
                }

                registerAddress = (registerAddress + 2) & 0xFFE;
                memoryAddress = (memoryAddress + 2) & 0xFFFFE;
                length -= 2;
            }
        }
        finally
        {
            _dmaActive = false;
        }

        _dmaFlags &= ~0x10;
        _registerRam[0x416 ^ 3] &= 0xE0;
        SoundInterrupt(0x10);
        MainInterrupt(0x10);
    }

    private delegate void LegacySlotUpdate(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length);

    // Frequency modulation, envelope modulation, sample width, left, right.
    private static readonly LegacySlotUpdate[,,,,] LegacySlotUpdates =
    {
        {
            {
                {
                    { MixLegacySlotNull, MixLegacySlot8BitR },
                    { MixLegacySlot8BitL, MixLegacySlot8BitLR },
                },
                {
                    { MixLegacySlotNull, MixLegacySlot16BitR },
                    { MixLegacySlot16BitL, MixLegacySlot16BitLR },
                },
            },
            {
                {
                    { MixLegacySlotNull, MixLegacySlotE8BitR },
                    { MixLegacySlotE8BitL, MixLegacySlotE8BitLR },
                },
                {
                    { MixLegacySlotNull, MixLegacySlotE16BitR },
                    { MixLegacySlotE16BitL, MixLegacySlotE16BitLR },
                },
            },
        },
        {
            {
                {
                    { MixLegacySlotNull, MixLegacySlotF8BitR },
                    { MixLegacySlotF8BitL, MixLegacySlotF8BitLR },
                },
                {
                    { MixLegacySlotNull, MixLegacySlotF16BitR },
                    { MixLegacySlotF16BitL, MixLegacySlotF16BitLR },
                },
            },
            {
                {
                    { MixLegacySlotNull, MixLegacySlotFE8BitR },
                    { MixLegacySlotFE8BitL, MixLegacySlotFE8BitLR },
                },
                {
                    { MixLegacySlotNull, MixLegacySlotFE16BitR },
                    { MixLegacySlotFE16BitL, MixLegacySlotFE16BitLR },
                },
            },
        },
    };

    private int GetLegacyOutput8(ref LegacySlot slot)
    {
        // Mednafen scsp.inc, RunSample: select the byte lane after forming the address.
        uint address = (slot.SampleByteBase + (slot.FrequencyCounter >> LegacyFrequencyFractionBits)) & 0xFFFFF;
        return address < _soundRam.Length ? unchecked((sbyte)T2ReadByte(_soundRam, address)) : 0;
    }

    private int GetLegacyOutput16(ref LegacySlot slot)
    {
        uint offset = (slot.FrequencyCounter >> LegacyFrequencyFractionBits) * 2;
        return BitConverter.ToInt16(_soundRam.AsSpan((int)(slot.SampleWordBase + offset)));
    }

    private void GetLegacyEnvelope(ref LegacySlot slot)
    {
        slot.Envelope = _legacyEnvelope[slot.EnvelopeCounter >> LegacyEnvelopeFractionBits] * slot.TotalLevel / 1024;
    }

    private void GetLegacyEnvelopeLfo(ref LegacySlot slot)
    {
        slot.Envelope = (_legacyEnvelope[slot.EnvelopeCounter >> LegacyEnvelopeFractionBits] * slot.TotalLevel / 1024) -
            (slot.LfoEnvelopeWave![(slot.LfoCounter >> LegacyLfoFractionBits) & LegacyLfoMask] >> slot.LfoEnvelopeSensitivity);
    }

    private static bool UpdateLegacyPhase(ref LegacySlot slot)
    {
        slot.FrequencyCounter = unchecked(slot.FrequencyCounter + slot.FrequencyIncrement);
        if (slot.FrequencyCounter > slot.LoopEnd)
        {
            if (slot.LoopControl != 0)
                slot.FrequencyCounter = slot.LoopStart;
            else
            {
                slot.EnvelopeCounter = LegacyEnvelopeDecayEnd;
                return false;
            }
        }

        return true;
    }

    private static bool UpdateLegacyPhaseLfo(ref LegacySlot slot)
    {
        int modulation = (slot.LfoFrequencyWave![(slot.LfoCounter >> LegacyLfoFractionBits) & LegacyLfoMask] <<
            (slot.LfoFrequencySensitivity - 7)) >> (slot.FrequencyShift + 1);
        slot.FrequencyCounter = unchecked(slot.FrequencyCounter + (uint)modulation);
        slot.FrequencyCounter = unchecked(slot.FrequencyCounter + slot.FrequencyIncrement);

        if (slot.FrequencyCounter > slot.LoopEnd)
        {
            if (slot.LoopControl != 0)
                slot.FrequencyCounter = slot.LoopStart;
            else
            {
                slot.EnvelopeCounter = LegacyEnvelopeDecayEnd;
                return false;
            }
        }

        return true;
    }

    private static bool UpdateLegacyEnvelope(ref LegacySlot slot)
    {
        switch (slot.EnvelopeStep)
        {
            case LegacyEnvelopeStep.Attack: slot.EnvelopeCounter += slot.AttackIncrement; break;
            case LegacyEnvelopeStep.Decay: slot.EnvelopeCounter += slot.DecayIncrement; break;
            case LegacyEnvelopeStep.Sustain: slot.EnvelopeCounter += slot.SustainIncrement; break;
            case LegacyEnvelopeStep.Release: slot.EnvelopeCounter += slot.ReleaseIncrement; break;
        }

        if (slot.EnvelopeCounter >= slot.EnvelopeCompare)
        {
            slot.NextEnvelopeEvent!(ref slot);
            if (slot.EnvelopeCounter >= LegacyEnvelopeDecayEnd)
                return false;
        }

        return true;
    }

    private static void MixLegacySlotNull(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            chip.GetLegacyEnvelope(ref slot);
            if (!UpdateLegacyPhase(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
        }
    }

    private static void MixLegacySlot8BitL(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            // env = [0..0x3FF] - slot->tl
            int sample = chip.GetLegacyOutput8(ref slot);
            chip.GetLegacyEnvelope(ref slot);
            // don't waste time if no sound...
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                left[position] += sample >> (slot.DirectLevelLeft - 8);
            }
            // calculate new frequency (phase) counter and enveloppe counter
            if (!UpdateLegacyPhase(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
        }
    }

    private static void MixLegacySlot8BitR(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput8(ref slot);
            chip.GetLegacyEnvelope(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                right[position] += sample >> (slot.DirectLevelRight - 8);
            }
            if (!UpdateLegacyPhase(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
        }
    }

    private static void MixLegacySlot8BitLR(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput8(ref slot);
            chip.GetLegacyEnvelope(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                left[position] += sample >> (slot.DirectLevelLeft - 8);
                right[position] += sample >> (slot.DirectLevelRight - 8);
            }
            if (!UpdateLegacyPhase(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
        }
    }

    private static void MixLegacySlotE8BitL(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput8(ref slot);
            chip.GetLegacyEnvelopeLfo(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                left[position] += sample >> (slot.DirectLevelLeft - 8);
            }
            if (!UpdateLegacyPhase(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private static void MixLegacySlotE8BitR(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput8(ref slot);
            chip.GetLegacyEnvelopeLfo(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                right[position] += sample >> (slot.DirectLevelRight - 8);
            }
            if (!UpdateLegacyPhase(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private static void MixLegacySlotE8BitLR(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput8(ref slot);
            chip.GetLegacyEnvelopeLfo(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                left[position] += sample >> (slot.DirectLevelLeft - 8);
                right[position] += sample >> (slot.DirectLevelRight - 8);
            }
            if (!UpdateLegacyPhase(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private static void MixLegacySlotF8BitL(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput8(ref slot);
            chip.GetLegacyEnvelope(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                left[position] += sample >> (slot.DirectLevelLeft - 8);
            }
            if (!UpdateLegacyPhaseLfo(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private static void MixLegacySlotF8BitR(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput8(ref slot);
            chip.GetLegacyEnvelope(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                right[position] += sample >> (slot.DirectLevelRight - 8);
            }
            if (!UpdateLegacyPhaseLfo(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private static void MixLegacySlotF8BitLR(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput8(ref slot);
            chip.GetLegacyEnvelope(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                left[position] += sample >> (slot.DirectLevelLeft - 8);
                right[position] += sample >> (slot.DirectLevelRight - 8);
            }
            if (!UpdateLegacyPhaseLfo(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private static void MixLegacySlotFE8BitL(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput8(ref slot);
            chip.GetLegacyEnvelopeLfo(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                left[position] += sample >> (slot.DirectLevelLeft - 8);
            }
            if (!UpdateLegacyPhaseLfo(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private static void MixLegacySlotFE8BitR(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput8(ref slot);
            chip.GetLegacyEnvelopeLfo(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                right[position] += sample >> (slot.DirectLevelRight - 8);
            }
            if (!UpdateLegacyPhaseLfo(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private static void MixLegacySlotFE8BitLR(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput8(ref slot);
            chip.GetLegacyEnvelopeLfo(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                left[position] += sample >> (slot.DirectLevelLeft - 8);
                right[position] += sample >> (slot.DirectLevelRight - 8);
            }
            if (!UpdateLegacyPhaseLfo(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private static void MixLegacySlot16BitL(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput16(ref slot);
            chip.GetLegacyEnvelope(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                left[position] += sample >> slot.DirectLevelLeft;
            }
            if (!UpdateLegacyPhase(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
        }
    }

    private static void MixLegacySlot16BitR(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput16(ref slot);
            chip.GetLegacyEnvelope(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                right[position] += sample >> slot.DirectLevelRight;
            }
            if (!UpdateLegacyPhase(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
        }
    }

    private static void MixLegacySlot16BitLR(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput16(ref slot);
            chip.GetLegacyEnvelope(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                left[position] += sample >> slot.DirectLevelLeft;
                right[position] += sample >> slot.DirectLevelRight;
            }
            if (!UpdateLegacyPhase(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
        }
    }

    private static void MixLegacySlotE16BitL(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput16(ref slot);
            chip.GetLegacyEnvelopeLfo(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                left[position] += sample >> slot.DirectLevelLeft;
            }
            if (!UpdateLegacyPhase(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private static void MixLegacySlotE16BitR(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput16(ref slot);
            chip.GetLegacyEnvelopeLfo(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                right[position] += sample >> slot.DirectLevelRight;
            }
            if (!UpdateLegacyPhase(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private static void MixLegacySlotE16BitLR(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput16(ref slot);
            chip.GetLegacyEnvelopeLfo(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                left[position] += sample >> slot.DirectLevelLeft;
                right[position] += sample >> slot.DirectLevelRight;
            }
            if (!UpdateLegacyPhase(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private static void MixLegacySlotF16BitL(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput16(ref slot);
            chip.GetLegacyEnvelope(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                left[position] += sample >> slot.DirectLevelLeft;
            }
            if (!UpdateLegacyPhaseLfo(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private static void MixLegacySlotF16BitR(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput16(ref slot);
            chip.GetLegacyEnvelope(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                right[position] += sample >> slot.DirectLevelRight;
            }
            if (!UpdateLegacyPhaseLfo(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private static void MixLegacySlotF16BitLR(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput16(ref slot);
            chip.GetLegacyEnvelope(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                left[position] += sample >> slot.DirectLevelLeft;
                right[position] += sample >> slot.DirectLevelRight;
            }
            if (!UpdateLegacyPhaseLfo(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private static void MixLegacySlotFE16BitL(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput16(ref slot);
            chip.GetLegacyEnvelopeLfo(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                left[position] += sample >> slot.DirectLevelLeft;
            }
            if (!UpdateLegacyPhaseLfo(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private static void MixLegacySlotFE16BitR(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput16(ref slot);
            chip.GetLegacyEnvelopeLfo(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                right[position] += sample >> slot.DirectLevelRight;
            }
            if (!UpdateLegacyPhaseLfo(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private static void MixLegacySlotFE16BitLR(SaturnScsp chip, ref LegacySlot slot,
        Span<int> left, Span<int> right, int length)
    {
        for (int position = 0; position < length; position++)
        {
            int sample = chip.GetLegacyOutput16(ref slot);
            chip.GetLegacyEnvelopeLfo(ref slot);
            if (sample != 0 && slot.Envelope > 0)
            {
                sample *= slot.Envelope;
                left[position] += sample >> slot.DirectLevelLeft;
                right[position] += sample >> slot.DirectLevelRight;
            }
            if (!UpdateLegacyPhaseLfo(ref slot)) return;
            if (!UpdateLegacyEnvelope(ref slot)) return;
            slot.LfoCounter = unchecked(slot.LfoCounter + (uint)slot.LfoIncrement);
        }
    }

    private void UpdateLegacySamples(Span<int> left, Span<int> right, int length)
    {
        for (int i = 0; i < 32; i++)
        {
            ref LegacySlot slot = ref _legacySlots[i];
            if (slot.EnvelopeCounter >= LegacyEnvelopeDecayEnd)
                continue;

            if (slot.SoundSourceControl != 0)
            {
                for (int position = 0; position < length; position++)
                {
                    slot.FrequencyCounter = unchecked(slot.FrequencyCounter + slot.FrequencyIncrement);
                    if (slot.FrequencyCounter > slot.LoopEnd)
                    {
                        if (slot.LoopControl != 0)
                            slot.FrequencyCounter = slot.LoopStart;
                        else
                        {
                            slot.EnvelopeCounter = LegacyEnvelopeDecayEnd;
                            break;
                        }
                    }
                }

                continue;
            }

            if (slot.DirectLevelLeft == 31 && slot.DirectLevelRight == 31)
            {
                slot.DirectLevelLeft = slot.EffectLevelLeft;
                slot.DirectLevelRight = slot.EffectLevelRight;
            }

            LegacySlotUpdates[slot.LfoFrequencySensitivity == 31 ? 0 : 1,
                slot.LfoEnvelopeSensitivity == 31 ? 0 : 1,
                slot.Pcm8Bit == 0 ? 1 : 0,
                slot.DirectLevelLeft == 31 ? 0 : 1,
                slot.DirectLevelRight == 31 ? 0 : 1](this, ref slot, left, right, length);
        }

        if (_cddaBytesLeft > 0)
        {
            int bufferLength;
            if (length > _cddaBytesLeft / 4)
                bufferLength = _cddaBytesLeft / 4;
            else
                bufferLength = length;

            int position = 0;
            while (position < bufferLength)
            {
                int pendingStart = _cddaNextIn - _cddaBytesLeft;
                int bufferOffset = pendingStart < 0 ? pendingStart + _cdda.Length : pendingStart;
                int blockLength = bufferLength - position;
                if (blockLength > (_cdda.Length - bufferOffset) / 4)
                    blockLength = (_cdda.Length - bufferOffset) / 4;
                int target = position + blockLength;

                for (; position < target; position++, bufferOffset += 4)
                {
                    int sample = unchecked((short)((_cdda[bufferOffset + 1] << 8) | _cdda[bufferOffset]));
                    if (sample != 0)
                        left[position] += sample;

                    sample = unchecked((short)((_cdda[bufferOffset + 3] << 8) | _cdda[bufferOffset + 2]));
                    if (sample != 0)
                        right[position] += sample;
                }

                _cddaBytesLeft -= blockLength * 4;
            }
        }
    }

    private void UpdateMonitor()
    {
        if (_useNewScsp)
        {
            ref var slot = ref _slots[_monitorSlot];
            _callAddress = slot.State.SampleOffset >> 5;
            _envelopePhase = (int)slot.State.Envelope;
            _envelopeLevel = slot.State.Attenuation >> 5;
        }
        else
        {
            ref LegacySlot slot = ref _legacySlots[_monitorSlot];
            _callAddress = (int)(((slot.FrequencyCounter >> (LegacyFrequencyFractionBits + 12)) & 0xF) << 7);
            _envelopePhase = (int)slot.EnvelopePhase;
            _envelopeLevel = 0x1F - (slot.Envelope >> (10 - 5));
        }
    }

    public void UpdateTimers(uint length)
    {
        if (_useScsp2)
        {
            UpdateNewTimer(length, ref _timers[0], 0x40);
            UpdateNewTimer(length, ref _timers[1], 0x80);
            UpdateNewTimer(length, ref _timers[2], 0x100);
            return;
        }

        UpdateLegacyTimer(length, ref _timers[0], 0x40);
        UpdateLegacyTimer(length, ref _timers[1], 0x80);
        UpdateLegacyTimer(length, ref _timers[2], 0x100);
        if (length != 0)
        {
            if ((_soundInterruptPending & 0x400) == 0)
                SoundInterrupt(0x400);
            if ((_mainInterruptPending & 0x400) == 0)
                MainInterrupt(0x400);
        }
    }

    // scsp.c, scsp_update_timer.
    private void UpdateLegacyTimer(uint length, ref Timer timer, int id)
    {
        timer.Counter += length << (8 - timer.Control);
        if (timer.Counter >= 0xFF00)
        {
            if ((_soundInterruptPending & id) == 0)
                SoundInterrupt(id);
            if ((_mainInterruptPending & id) == 0)
                MainInterrupt(id);
            timer.Counter -= 0xFF00;
        }
    }

    public void SendMidiInput(byte data)
    {
        if ((_midiFlags & MidiInputEmpty) != 0)
        {
            SoundInterrupt(0x8);
            MainInterrupt(0x8);
        }

        _midiFlags &= ~MidiInputEmpty;
        if (_midiInputCount > 3)
        {
            _midiFlags |= MidiInputOverflow;
            return;
        }

        _midiInput[_midiInputCount++] = data;
        if (_midiInputCount > 3)
            _midiFlags |= MidiInputFull;
    }

    private void SendMidiOutput(byte data)
    {
        _midiFlags &= ~MidiOutputEmpty;
        if (_midiOutputCount > 3)
            return;
        _midiOutput[_midiOutputCount++] = data;
        if (_midiOutputCount > 3)
            _midiFlags |= MidiOutputFull;
    }

    private byte ReadMidiInput()
    {
        _midiFlags &= ~(MidiInputOverflow | MidiInputFull);
        if (_midiInputCount == 0)
            return 0xFF;

        if (_midiInputCount > 1)
        {
            SoundInterrupt(0x8);
            MainInterrupt(0x8);
        }
        else
            _midiFlags |= MidiInputEmpty;

        byte data = _midiInput[0];
        switch ((--_midiInputCount) & 3)
        {
            case 1:
                _midiInput[0] = _midiInput[1];
                break;
            case 2:
                _midiInput[0] = _midiInput[1];
                _midiInput[1] = _midiInput[2];
                break;
            case 3:
                _midiInput[0] = _midiInput[1];
                _midiInput[1] = _midiInput[2];
                _midiInput[2] = _midiInput[3];
                break;
        }
        return data;
    }

    public byte ReadMidiOutput()
    {
        _midiFlags &= ~MidiOutputFull;
        if (_midiOutputCount == 0)
            return 0xFF;

        if (_midiOutputCount == 1)
        {
            _midiFlags |= MidiOutputEmpty;
            SoundInterrupt(0x200);
            MainInterrupt(0x200);
        }

        byte data = _midiOutput[0];
        switch ((--_midiOutputCount) & 3)
        {
            case 1:
                _midiOutput[0] = _midiOutput[1];
                break;
            case 2:
                _midiOutput[0] = _midiOutput[1];
                _midiOutput[1] = _midiOutput[2];
                break;
            case 3:
                _midiOutput[0] = _midiOutput[1];
                _midiOutput[1] = _midiOutput[2];
                _midiOutput[2] = _midiOutput[3];
                break;
        }
        return data;
    }

    public void Reset()
    {
        if (_useScsp2)
            SynchronizeScsp2Thread();

        Array.Clear(_registerRam);
        _mem4b = 0;
        _masterVolume = 0;
        _ringLength = 0;
        _ringBase = 0;
        _monitorSlot = 0;
        _callAddress = 0;
        _dmaMemoryAddress = 0;
        _dmaRegisterAddress = 0;
        _dmaFlags = 0;
        _dmaLength = 0;
        _midiInputCount = 0;
        Array.Clear(_midiInput);
        _midiOutputCount = 0;
        Array.Clear(_midiOutput);
        _midiFlags = MidiInputEmpty | MidiOutputEmpty;
        for (int index = 0; index < _timers.Length; index++)
            _timers[index] = new Timer { Counter = 0xFF00 };
        _mainInterruptEnable = 0;
        _mainInterruptPending = 0;
        _soundInterruptEnable = 0;
        _soundInterruptPending = 0;
        _soundInterruptLevel0 = 0;
        _soundInterruptLevel1 = 0;
        _soundInterruptLevel2 = 0;

        for (int i = 0; i < 32; i++)
        {
            _legacySlots[i] = new LegacySlot
            {
                EnvelopeCounter = LegacyEnvelopeDecayEnd,
                EnvelopePhase = LegacyEnvelopePhase.Release,
                DirectLevelRight = 31,
                DirectLevelLeft = 31,
                EffectLevelRight = 31,
                EffectLevelLeft = 31,
                LfoFrequencyWave = _legacyLfoSawFrequency,
                LfoEnvelopeWave = _legacyLfoSawEnvelope,
            };
        }

        if (_useScsp2)
        {
            ResetScsp2Slots();
            _scspClockFraction = 0;
            _scspSampleTimer = 0;
            Interlocked.Exchange(ref _scsp2MainInterruptPending, 0);
            ClearCdda();
            _soundCpuRunning = false;
        }
        else if (_useNewScsp)
            ResetNewScsp();
        else
        {
            Array.Clear(_soundStack);
            _dsp.Reset();
        }

    }

    private static int LegacyRound(double value) => (int)(value + 0.5);

    private void InitializeLegacyTables()
    {
        const int frequency = 44100;
        for (int i = 0; i < LegacyEnvelopeLength; i++)
        {
            double x = Math.Pow((double)(LegacyEnvelopeMask - i) / LegacyEnvelopeLength, 7);
            x *= LegacyEnvelopeLength;
            _legacyEnvelope[i] = LegacyEnvelopeMask - (int)x;

            x = Math.Pow((double)i / LegacyEnvelopeLength, 1);
            x *= LegacyEnvelopeLength;
            _legacyEnvelope[i + LegacyEnvelopeLength] = LegacyEnvelopeMask - (int)x;
        }

        for (int i = 0, j = 0; i < 32; i++)
        {
            j += 1 << (i >> 2);
            double x = (frequency / 256.0) / j;
            _legacyLfoStep[31 - i] = LegacyRound(x * ((double)LegacyLfoLength / frequency) *
                (1 << LegacyLfoFractionBits));
        }

        for (int i = 0; i < LegacyLfoLength; i++)
        {
            _legacyLfoSawEnvelope[i] = LegacyLfoMask - i;
            if (i < LegacyLfoLength / 2)
                _legacyLfoSquareEnvelope[i] = LegacyLfoMask;
            else
                _legacyLfoSquareEnvelope[i] = 0;

            if (i < LegacyLfoLength / 2)
                _legacyLfoTriangleEnvelope[i] = LegacyLfoMask - i * 2;
            else
                _legacyLfoTriangleEnvelope[i] = (i - LegacyLfoLength / 2) * 2;

            _legacyLfoNoiseEnvelope[i] = _registers.NextRandom() & LegacyLfoMask;
            _legacyLfoSawFrequency[(i + 512) & LegacyLfoMask] = i - LegacyLfoLength / 2;

            if (i < LegacyLfoLength / 2)
                _legacyLfoSquareFrequency[i] = LegacyLfoMask - LegacyLfoLength / 2 - 128;
            else
                _legacyLfoSquareFrequency[i] = 0 - LegacyLfoLength / 2 + 128;

            if (i < LegacyLfoLength / 2)
                _legacyLfoTriangleFrequency[(i + 768) & LegacyLfoMask] = i * 2 - LegacyLfoLength / 2;
            else
                _legacyLfoTriangleFrequency[(i + 768) & LegacyLfoMask] =
                    LegacyLfoMask - (i - LegacyLfoLength / 2) * 2 - LegacyLfoLength / 2 + 1;

            _legacyLfoNoiseFrequency[i] = _legacyLfoNoiseEnvelope[i] - LegacyLfoLength / 2;
        }

        for (int i = 0; i < 4; i++)
        {
            _legacyAttackRate[i] = 0;
            _legacyDecayRate[i] = 0;
        }

        for (int i = 0; i < 60; i++)
        {
            double x = 1.0 + (i & 3) * 0.25;
            x *= 1 << (i >> 2);
            x *= LegacyEnvelopeLength << LegacyEnvelopeFractionBits;
            _legacyAttackRate[i + 4] = LegacyRound(x / LegacyAttackRate);
            _legacyDecayRate[i + 4] = LegacyRound(x / LegacyDecayRate);

            if (_legacyAttackRate[i + 4] == 0) _legacyAttackRate[i + 4] = 1;
            if (_legacyDecayRate[i + 4] == 0) _legacyDecayRate[i + 4] = 1;
        }

        _legacyAttackRate[63] = LegacyEnvelopeAttackEnd;
        _legacyDecayRate[61] = _legacyDecayRate[60];
        _legacyDecayRate[62] = _legacyDecayRate[60];
        _legacyDecayRate[63] = _legacyDecayRate[60];

        for (int i = 64; i < 96; i++)
        {
            _legacyAttackRate[i] = _legacyAttackRate[63];
            _legacyDecayRate[i] = _legacyDecayRate[63];
            _legacyNullRate[i - 64] = 0;
        }

        for (int i = 0; i < 256; i++)
            _legacyTotalLevel[i] = LegacyRound(Math.Pow(10, i * -0.3762 / 20) * 1024.0);
    }

    public void StartSoundCpu()
    {
        if (_useScsp2)
            SynchronizeScsp2Thread();
        _soundCpu.Reset();
        _savedSoundCpuCycles = 0;
        _soundCpuRunning = true;
    }

    public void StopSoundCpu()
    {
        if (_useScsp2)
            SynchronizeScsp2Thread();
        _soundCpuRunning = false;
    }

    public void SynchronizeSoundCpu()
    {
        if (_useScsp2)
            SynchronizeScsp2Thread();
        else
            _soundCpu.Sync();
    }

    public void ExecuteSoundCpu(int cycles)
    {
        int newCycles = _savedSoundCpuCycles - cycles;
        if (_soundCpuRunning)
        {
            if (newCycles < 0)
            {
                int cyclesToExecute = -newCycles;
                newCycles += _soundCpu.Execute(cyclesToExecute);
            }
            _savedSoundCpuCycles = newCycles;
        }
    }

    private void RunNewSample(out int outputLeft, out int outputRight)
    {
        int pendingStart = _cddaNextIn - _cddaBytesLeft;
        int outputPosition = pendingStart < 0 ? pendingStart + _cdda.Length : pendingStart;
        short left = 0;
        short right = 0;
        short cdLeft = 0;
        short cdRight = 0;

        if (_cddaBytesLeft > 0)
        {
            cdLeft = unchecked((short)((_cdda[outputPosition + 1] << 8) | _cdda[outputPosition]));
            cdRight = unchecked((short)((_cdda[outputPosition + 3] << 8) | _cdda[outputPosition + 2]));
            _cddaBytesLeft -= 4;
        }

        UpdateTimers(1);
        GenerateSample(_ringBase, _ringLength, ref left, ref right, _masterVolume, cdLeft, cdRight);

        outputLeft = left;
        outputRight = right;
        UpdateMonitor();
    }

    // scsp.c, new_scsp_exec.
    public void ExecuteNewScspMixer(int cycles)
    {
        int remaining = _legacyNewCycles - cycles;
        if (remaining < 0)
        {
            RunNewSample(out int left, out int right);
            if (_legacyNewOutputPosition < 900)
            {
                _legacyNewOutputLeft[_legacyNewOutputPosition] = left;
                _legacyNewOutputRight[_legacyNewOutputPosition] = right;
            }
            _legacyNewOutputPosition++;
            remaining += 512;
        }
        _legacyNewCycles = remaining;
    }

    public void ClearCdda()
    {
        if (_useScsp2)
        {
            SynchronizeScsp2Thread();
            _scsp2CddaNextIn = 0;
            _scsp2CddaNextOut = 0;
            _scsp2CddaDelay = Scsp2CddaDelaySamples;
            return;
        }

        _cddaNextIn = 0;
        _cddaBytesLeft = 0;
    }

    public void ReceiveCdda(ReadOnlySpan<byte> sector)
    {
        if (_useScsp2)
        {
            int nextIn = _scsp2CddaNextIn;
            int nextNextIn = (nextIn + 1) % Scsp2CddaNumBuffers;
            int nextOut = _scsp2CddaNextOut;
            int start = nextIn * Scsp2CddaSectorSize;
            if (nextOut > start && nextOut <= start + Scsp2CddaSectorSize)
                return;
            sector[..Scsp2CddaSectorSize].CopyTo(_scsp2Cdda.AsSpan(start));
            _scsp2CddaNextIn = nextNextIn;
            return;
        }

        if (_cddaBytesLeft < _cdda.Length / 2)
        {
            _registers.SetCdAudioPlaying(false);
            _registers.SetCdTiming(1);
            _registers.SetCdAudioPlaying(true);
        }
        else if (_cddaBytesLeft > _cdda.Length * 3 / 4)
        {
            _registers.SetCdTiming(0);
        }
        else
        {
            _registers.SetCdAudioPlaying(true);
            _registers.SetCdTiming(1);
        }

        sector[..2352].CopyTo(_cdda.AsSpan(_cddaNextIn));
        if (_cdda.Length - _cddaNextIn <= 2352)
            _cddaNextIn = 0;
        else
            _cddaNextIn += 2352;

        _cddaBytesLeft += 2352;
        if (_cddaBytesLeft > _cdda.Length)
            _cddaBytesLeft = _cdda.Length;
    }

    public void ReceiveMpeg(ReadOnlySpan<byte> samples)
    {
        int length = samples.Length;
        samples.CopyTo(_cdda.AsSpan(_cddaNextIn));
        if (_cdda.Length - _cddaNextIn <= length)
            _cddaNextIn = 0;
        else
            _cddaNextIn += length;

        _cddaBytesLeft += length;
        if (_cddaBytesLeft > _cdda.Length)
            _cddaBytesLeft = _cdda.Length;
    }

    public void ChangeVideoFormat(bool pal)
    {
        _scspClockIncrement = pal ? ScspClockIncrementPal : ScspClockIncrementNtsc;
        int frameRate = pal ? 50 : 60;
        _soundLength = 44100 / frameRate;
        _soundLines = pal ? 313 : 263;
        int bufferSize = _soundLength * SoundBufferCount;
        _soundLeft = new int[bufferSize];
        _soundRight = new int[bufferSize];
        _registers.ChangeAudioVideoFormat(frameRate);
    }

    public void Execute()
    {
        _soundTimingFraction += ((_soundLength << 16) + _soundLines / 2) / _soundLines;
        if (!_useNewScsp)
            UpdateTimers((uint)(_soundTimingFraction >> 16));
        _soundTimingFraction &= 0xFFFF;
        _soundTimingLines++;

        int bufferSize = _soundLeft.Length;
        if (_soundTimingLines >= _soundLines)
        {
            _soundTimingLines -= _soundLines;
            _soundTimingFraction = 0;

            if (_soundGeneratePosition + _soundLength > bufferSize)
                _soundGeneratePosition = 0;

            if (_soundOutputLeft + _soundLength > bufferSize)
            {
                int overrun = _soundOutputLeft + _soundLength - bufferSize;
                _soundOutputLeft -= overrun;
            }

            Span<int> left = _soundLeft.AsSpan(_soundGeneratePosition, _soundLength);
            Span<int> right = _soundRight.AsSpan(_soundGeneratePosition, _soundLength);
            left.Clear();
            right.Clear();
            if (_useNewScsp)
            {
                int count = Math.Min(_legacyNewOutputPosition, _soundLength);
                _legacyNewOutputLeft.AsSpan(0, count).CopyTo(left);
                _legacyNewOutputRight.AsSpan(0, count).CopyTo(right);
                _legacyNewOutputPosition = 0;
            }
            else
            {
                UpdateLegacySamples(left, right, _soundLength);
            }
            _soundGeneratePosition += _soundLength;
            _soundOutputLeft += _soundLength;
        }

        int audioSize;
        while (_soundOutputLeft > 0 && (audioSize = _registers.GetAudioSpace()) > 0)
        {
            int outputStart = _soundGeneratePosition - _soundOutputLeft;
            if (outputStart < 0)
                outputStart += bufferSize;
            if (audioSize > _soundOutputLeft)
                audioSize = _soundOutputLeft;
            if (audioSize > bufferSize - outputStart)
                audioSize = bufferSize - outputStart;

            _registers.UpdateAudio(_soundLeft.AsSpan(outputStart, audioSize),
                _soundRight.AsSpan(outputStart, audioSize), audioSize);
            _soundOutputLeft -= audioSize;
        }

        if (!_useNewScsp)
            UpdateMonitor();

        if (_registers.WriteMidiOutput is { } writeMidiOutput)
        {
            while (_midiOutputCount > 0)
                writeMidiOutput(ReadMidiOutput());
        }
    }

    public void WriteByte(uint address, byte data)
    {
        if (!QueueScsp2Write(address, data, 1))
            WriteByteDirect(address, data);
    }

    private void WriteByteDirect(uint address, byte data)
    {
        address &= 0xFFF;

        if (address < 0x400)
            {
                if (_useScsp2)
                    WriteScsp2SlotByte(address, data);
                else if (_useNewScsp)
                    WriteNewSlotByte(address, data);
                else
                    WriteLegacySlotByte(address >> 5, address, data);

                return;
            }
        else if (address < 0x600)
            {
                if (address < 0x440)
                    {
                    WriteCommonByte(address, data);

                    return;
                    }
            }
        else if (_useScsp2 && address < 0x680)
            {
                int shift = (int)((address & 1) ^ 1) << 3;
                uint index = (address & 0x7F) >> 1;
                _soundStack[index] = (ushort)((_soundStack[index] & ~(0xFF << shift)) | (data << shift));
            }
        else if (address is >= 0x700 and < 0xee4)
            {
                if (!_useScsp2)
                {
                    T1WriteByte(_registerRam, 0x700 + ((address & 0x3FF) ^ 3), data);
                    return;
                }

                int shift = (int)((address & 1) ^ 1) << 3;
                ushort word = ReadWord(address);
                word = (ushort)((word & ~(0xFF << shift)) | (data << shift));
                WriteWord(address, word);
                return;
            }

    }

    public void WriteWord(uint address, ushort data)
    {
        if (!QueueScsp2Write(address, data, 2))
            WriteWordDirect(address, data);
    }

    private void WriteWordDirect(uint address, ushort data)
    {

        address &= 0xFFE;

        if (address < 0x400)
            {
                if (_useScsp2)
                    WriteScsp2SlotWord(address & ~1u, data);
                else if (_useNewScsp)
                    WriteNewSlotWord(address, data);
                else
                    WriteLegacySlotWord(address >> 5, address, data);

                return;
            }
        else if (address < 0x600)
            {
                if (address < 0x440)
                    {
                    WriteCommonWord(address, data);

                    return;
                    }
            }
        else if (_useScsp2 && address < 0x680)
            {
                _soundStack[(address & 0x7F) >> 1] = data;
            }
        else if (address >= 0x700 && address < 0x780)
        {
            uint index = (address - 0x700) / 2;
            _dsp.Interface.SetCoefficient((uint)data >> 3, index);
        }
        else if (address >= 0x780 && address < 0x7C0)
        {
            if (_useScsp2 || address < 0x7A0)
                _dsp.Interface.SetMemoryAddress(data, (address - 0x780) / 2);
        }
        else if (address >= 0x800 && address < 0xC00)
        {
            uint index = (address - 0x800) / 8;
            ulong currentValue = _dsp.Interface.GetMpro(index);

            switch (address & 0xf)
            {
            case 0:
            case 8:
                    currentValue = (currentValue & 0x0000ffffffffffff) | (ulong)data << 48;
                    break;
            case 2:
            case 0xa:
                    currentValue = (currentValue & 0xffff0000ffffffff) | (ulong)data << 32;
                    break;
            case 4:
            case 0xc:
                    currentValue = (currentValue  & 0xffffffff0000ffff) | (ulong)data << 16;
                    break;
            case 6:
            case 0xe:
                    currentValue = (currentValue & 0xffffffffffff0000) | data;
                    break;
            default:
                    break;
            }

            _dsp.Interface.SetMpro(currentValue, index);
        }
        else if (address is >= 0xC00 and < 0xEE4)
        {
            _dsp.AccessDataRegister(address, data);
        }
        else if (!_useScsp2 && address is >= 0x7C0 and < 0x800)
            BitConverter.TryWriteBytes(_registerRam.AsSpan(0x700 + (int)((address & 0x3FF) ^ 2)), data);

    }

    public void WriteLong(uint address, uint data)
    {
        if (QueueScsp2Write(address, data, 4))
            return;
        address &= _useScsp2 ? 0xFFFu : 0xFFCu;
        if (!_useScsp2 && address >= 0x440)
        {
            if (address is >= 0x700 and < 0xEE4)
                BitConverter.TryWriteBytes(_registerRam.AsSpan(0x700 + (int)(address & 0x3FF)), data);
            return;
        }

        WriteWordDirect(address, (ushort)(data >> 16));
        WriteWordDirect(address + 2, unchecked((ushort)data));
    }

    public byte ReadByte(uint address)
    {
        address &= 0xFFF;

        if (address < 0x400)
            {
                if (_useScsp2)
                    return _registerRam[address ^ 3];
                if (_useNewScsp)
                    return ReadNewSlotByte(address);
                else
                    return ReadLegacySlotByte(address);
            }
        else if (address < 0x600)
            {
                if (address < 0x440) return ReadCommonByte(address);
            }
        else if (_useScsp2 && address < 0x680)
            {
                int shift = (int)((address & 1) ^ 1) << 3;
                return unchecked((byte)(_soundStack[(address & 0x7F) >> 1] >> shift));
            }
        else if (address is >= 0x700 and < 0xee4)
            {
                int shift = (int)((address & 1) ^ 1) << 3;
                return unchecked((byte)(ReadWord(address) >> shift));
            }

        return 0;
    }

    public ushort ReadWord(uint address)
    {

        address &= 0xFFE;

        if (address < 0x400)
            {
                if (_useScsp2)
                    return BitConverter.ToUInt16(_registerRam.AsSpan((int)((address & ~1u) ^ 2)));
                if (_useNewScsp)
                    return ReadNewSlotWord(address);
                else
                    return ReadLegacySlotWord(address);
            }
        else if (address < 0x600)
            {
                if (address < 0x440) return ReadCommonWord(address);
            }
        else if ((_useScsp2 && address < 0x680) || (!_useScsp2 && _useNewScsp && address < 0x700))
            {
                return _soundStack[(address & 0x7F) >> 1];
            }
        else if (address >= 0x700 && address < 0x780)
        {
            uint index = (address - 0x700) / 2;
            return unchecked((ushort)(_dsp.Interface.GetCoefficient(index) << 3));
        }
        else if (address >= 0x780 && address < 0x7C0)
        {
            uint index = _useScsp2 ? (address - 0x780) / 2 : (address & 0x1F) / 2;
            return unchecked((ushort)(_dsp.Interface.GetMemoryAddress(index)));
        }
        else if (address >= 0x800 && address < 0xC00)
        {
            uint index = (address - 0x800) / 8;
            ulong value = _dsp.Interface.GetMpro(index);
            switch (address & 0xf)
            {
            case 0:
            case 8:
                    return (ushort)((value >> 48) & 0xffff);
            case 2:
            case 0xa:
                    return (ushort)((value >> 32) & 0xffff);
            case 4:
            case 0xc:
                    return (ushort)((value >> 16) & 0xffff);
            case 6:
            case 0xe:
                    return (ushort)(value & 0xFFFF);
            default:
                    break;
            }
        }
        else if (address is >= 0xC00 and < 0xEE4)
        {
            return _dsp.AccessDataRegister(address);
        }

        return 0;
    }

    public uint ReadLong(uint address)
    {
        address &= 0xFFC;
        if (!_useScsp2 && address >= 0x440)
            return 0;
        return ((uint)ReadWord(address) << 16) | ReadWord(address + 2);
    }

    private uint SoundCpuRamMask => _mem4b == 0 ? 0x3FFFFu : 0x7FFFFu;

    internal Span<byte> DmaSoundRam => _soundRam;

    internal void NotifyDmaSoundRamWrite(uint address, uint length) => _soundCpu.WriteNotify(address, length);

    private uint SoundCpuDataRamMask => _useScsp2 ? SoundCpuRamMask : 0x7FFFFu;

    private void UpdateSoundCpuFetch()
    {
        if (_mem4b != 0)
        {
            _soundCpu.SetFetch(0x000000, 0x080000, _soundRam);
        }
        else
        {
            _soundCpu.SetFetch(0x000000, 0x040000, _soundRam);
            _soundCpu.SetFetch(0x040000, 0x080000, _soundRam);
            _soundCpu.SetFetch(0x080000, 0x0C0000, _soundRam);
            _soundCpu.SetFetch(0x0C0000, 0x100000, _soundRam);
        }
    }

    public uint SoundCpuReadByte(uint address)
    {
        address &= 0xFFFFFF;
        if (address < 0x100000)
            return T2ReadByte(_soundRam, address & SoundCpuDataRamMask);
        return ReadByte(address);
    }

    public void SoundCpuWriteByte(uint address, uint data)
    {
        address &= 0xFFFFFF;
        if (address < 0x100000)
            T2WriteByte(_soundRam, address & SoundCpuDataRamMask, unchecked((byte)data));
        else
            WriteByteDirect(address, unchecked((byte)data));
    }

    public uint SoundCpuReadWord(uint address)
    {
        address &= 0xFFFFFF;
        if (address < 0x100000)
        {
            uint offset = address & SoundCpuDataRamMask;
            if (offset == _soundRam.Length - 1)
                return BitConverter.IsLittleEndian
                    ? (uint)(_soundRam[^1] | (_soundRam[0] << 8))
                    : (uint)((_soundRam[^1] << 8) | _soundRam[0]);
            return T2ReadWord(_soundRam, offset);
        }
        return ReadWord(address);
    }

    public void SoundCpuWriteWord(uint address, uint data)
    {
        address &= 0xFFFFFF;
        if (address < 0x100000)
        {
            uint offset = address & SoundCpuDataRamMask;
            if (offset == _soundRam.Length - 1)
            {
                _soundRam[^1] = unchecked((byte)(data >> (BitConverter.IsLittleEndian ? 0 : 8)));
                _soundRam[0] = unchecked((byte)(data >> (BitConverter.IsLittleEndian ? 8 : 0)));
                return;
            }
            T2WriteWord(_soundRam, offset, unchecked((ushort)data));
        }
        else
            WriteWordDirect(address, unchecked((ushort)data));
    }

    public byte SoundRamReadByte(uint address)
    {
        if (_useScsp2)
        {
            address &= SoundCpuRamMask;
            return T2ReadByte(_soundRam, address);
        }

        address &= 0xFFFFF;

        if (_mem4b == 0)
            address &= 0x3FFFF;
        else if (address > 0x7FFFF)
            return byte.MaxValue;

        return T2ReadByte(_soundRam, address);
    }

    public void SoundRamWriteByte(uint address, byte value)
    {
        if (_useScsp2)
        {
            address &= SoundCpuRamMask;
            T2WriteByte(_soundRam, address, value);
            _soundCpu.WriteNotify(address, 1);
            return;
        }

        address &= 0xFFFFF;

        if (_mem4b == 0)
            address &= 0x3FFFF;
        else if (address > 0x7FFFF)
            return;

        T2WriteByte(_soundRam, address, value);
        _soundCpu.WriteNotify(address, 1);
    }

    public ushort SoundRamReadWord(uint address)
    {
        if (_useScsp2)
        {
            address &= SoundCpuRamMask;
            return T2ReadWord(_soundRam, address);
        }

        address &= 0xFFFFF;

        if (_mem4b == 0)
            address &= 0x3FFFF;
        else if (address > 0x7FFFF)
            return ushort.MaxValue;

        return T2ReadWord(_soundRam, address);
    }

    public void SoundRamWriteWord(uint address, ushort value)
    {
        if (_useScsp2)
        {
            address &= SoundCpuRamMask;
            T2WriteWord(_soundRam, address, value);
            _soundCpu.WriteNotify(address, 2);
            return;
        }

        address &= 0xFFFFF;

        if (_mem4b == 0)
            address &= 0x3FFFF;
        else if (address > 0x7FFFF)
            return;

        T2WriteWord(_soundRam, address, value);
        _soundCpu.WriteNotify(address, 2);
    }

    public uint SoundRamReadLong(uint address)
    {
        if (_useScsp2)
        {
            address &= SoundCpuRamMask;
            return T2ReadLong(_soundRam, address);
        }

        address &= 0xFFFFF;

        if (_mem4b == 0)
            address &= 0x3FFFF;
        else if (address > 0x7FFFF)
            return uint.MaxValue;

        return T2ReadLong(_soundRam, address);
    }

    public void SoundRamWriteLong(uint address, uint value)
    {
        if (_useScsp2)
        {
            address &= SoundCpuRamMask;
            T2WriteLong(_soundRam, address, value);
            _soundCpu.WriteNotify(address, 4);
            return;
        }

        address &= 0xFFFFF;

        if (_mem4b == 0)
            address &= 0x3FFFF;
        else if (address > 0x7FFFF)
            return;

        T2WriteLong(_soundRam, address, value);
        _soundCpu.WriteNotify(address, 4);
    }
}
