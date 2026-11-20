// Copyright 2004 Stephane Dallongeville
// Copyright 2004-2007 Theo Berkau
// Copyright 2006 Guillaume Duhamel
// Copyright 2010 Andrew Church
// Translated from Yabause scsp2.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Runtime.ExceptionServices;
using static RetroMediaLab.Chips.Controllers.Sega.SaturnMemoryAccess;

namespace RetroMediaLab.Chips.Audio.Yamaha.SCSP;

public sealed partial class SaturnScsp
{
    private const int Scsp2TableLength = 1024;
    private const int Scsp2FractionBits = 10;
    private const int Scsp2DecayStart = 1024 << Scsp2FractionBits;
    private const int Scsp2DecayEnd = (2048 << Scsp2FractionBits) - 1;
    private const int Scsp2AttackTime = 8 * 44100;
    private const int Scsp2DecayTime = 12 * Scsp2AttackTime;

    private enum Scsp2EnvelopePhase { Release, Sustain, Decay, Attack }

    private delegate void Scsp2AudioGenerator(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right);

    private struct Scsp2Slot
    {
        public Scsp2AudioGenerator Generate;
        public bool Key, Pcm8;
        public uint Source, LoopControl, Start, LoopStart, LoopEnd;
        public uint SampleBase, Address, Step, ShiftedLoopStart, ShiftedLoopEnd, ShiftedLoopLength;
        public int OctaveShift, KeyRateShift, Attack, Decay, Sustain, Release;
        public int AttackStep, DecayStep, SustainStep, ReleaseStep, SustainTarget;
        public Scsp2EnvelopePhase Phase;
        public int Envelope, EnvelopeStep, EnvelopeTarget, LastEnvelope, Volume;
        public int LfoStep, FrequencyWave, AmplitudeWave, FrequencyShift, AmplitudeShift;
        public uint LfoCounter;
        public int LeftShift, RightShift;
    }

    private readonly Scsp2Slot[] _scsp2Slots = new Scsp2Slot[32];
    private readonly int[] _scsp2Envelope = new int[2 * Scsp2TableLength];
    private readonly int[,] _scsp2Amplitude = new int[4, Scsp2TableLength];
    private readonly int[,] _scsp2Frequency = new int[4, Scsp2TableLength];
    private readonly int[] _scsp2LfoStep = new int[32];
    private readonly int[] _scsp2AttackRate = new int[78];
    private readonly int[] _scsp2DecayRate = new int[78];
    private readonly int[] _scsp2TotalLevel = new int[256];

    private void InitializeScsp2Tables()
    {
        const int mask = Scsp2TableLength - 1;
        for (int i = 0; i < Scsp2TableLength; i++)
        {
            double x = Math.Pow((double)(mask - i) / Scsp2TableLength, 4) * Scsp2TableLength;
            _scsp2Envelope[i] = mask - (int)Math.Floor(x);
            _scsp2Envelope[i + Scsp2TableLength] = mask - i;
        }

        for (int i = 0, divider = 0; i < 32; i++)
        {
            divider += 1 << (i >> 2);
            double frequency = (44100.0 / 256.0) / divider;
            double step = frequency / 44100 * Scsp2TableLength;
            _scsp2LfoStep[31 - i] = LegacyRound(step * (1 << Scsp2FractionBits));
        }

        for (int i = 0; i < Scsp2TableLength; i++)
        {
            _scsp2Amplitude[0, i] = i;
            _scsp2Amplitude[1, i] = i < 512 ? 0 : mask;
            _scsp2Amplitude[2, i] = i < 512 ? i * 2 : mask - (i - 512) * 2;
            _scsp2Amplitude[3, i] = _registers.NextRandom() & mask;
            _scsp2Frequency[0, i] = i < 512 ? i : i - Scsp2TableLength;
            _scsp2Frequency[1, i] = i < 512 ? mask - 512 : -512;
            _scsp2Frequency[2, i] = i < 256 ? i * 2 : i < 768 ? mask - i * 2 : i * 2 - 2048;
            _scsp2Frequency[3, i] = _scsp2Amplitude[3, i] - 512;
        }

        for (int i = 0; i < 60; i++)
        {
            double x = (1.0 + (i & 3) * 0.25) * (1 << (i >> 2)) * Scsp2DecayStart;
            _scsp2AttackRate[i + 4] = Math.Max(LegacyRound(x / Scsp2AttackTime), 1);
            _scsp2DecayRate[i + 4] = Math.Max(LegacyRound(x / Scsp2DecayTime), 1);
        }
        _scsp2AttackRate[63] = Scsp2DecayStart - 1;
        _scsp2DecayRate[61] = _scsp2DecayRate[62] = _scsp2DecayRate[63] = _scsp2DecayRate[60];
        for (int i = 64; i < 78; i++)
        {
            _scsp2AttackRate[i] = _scsp2AttackRate[63];
            _scsp2DecayRate[i] = _scsp2DecayRate[63];
        }

        for (int i = 0; i < 256; i++)
            _scsp2TotalLevel[i] = LegacyRound(Math.Pow(2, -(i / 16.0)) * 1024);
    }

    private void ResetScsp2Slots()
    {
        for (int i = 0; i < _scsp2Slots.Length; i++)
            _scsp2Slots[i] = new Scsp2Slot
            {
                Generate = GenerateNull,
                Envelope = Scsp2DecayEnd,
                LeftShift = 31,
                RightShift = 31,
            };
    }

    private void WriteScsp2SlotByte(uint address, byte data)
    {
        uint aligned = address & ~1u;
        ushort cached = BitConverter.ToUInt16(_registerRam.AsSpan((int)(aligned ^ 2)));
        ushort word = (address & 1) != 0
            ? (ushort)((cached & 0xFF00) | data)
            : (ushort)((cached & 0x00FF) | (data << 8));
        WriteScsp2SlotWord(aligned, word);
    }

    private void WriteScsp2SlotWord(uint address, ushort data)
    {
        ref Scsp2Slot slot = ref _scsp2Slots[(address & 0x3E0) >> 5];
        switch (address & 0x1F)
        {
            case 0x00:
                slot.Key = (data & 0x800) != 0;
                slot.Source = (uint)(data >> 7) & 3;
                slot.LoopControl = (uint)(data >> 5) & 3;
                slot.Pcm8 = (data & 0x10) != 0;
                slot.Start = ((uint)(data & 0xF) << 16) | (slot.Start & 0xFFFF);
                UpdateScsp2SlotFunction(ref slot);
                if (slot.Envelope < Scsp2DecayEnd)
                    UpdateScsp2SlotAddress(ref slot);
                if ((data & 0x1000) != 0)
                    ApplyScsp2Keys();
                data &= 0x0FFF;
                break;
            case 0x02:
                slot.Start = (slot.Start & 0xF0000) | data;
                if (slot.Envelope < Scsp2DecayEnd)
                    UpdateScsp2SlotAddress(ref slot);
                break;
            case 0x04:
                slot.LoopStart = data;
                if (slot.Envelope < Scsp2DecayEnd)
                    UpdateScsp2SlotAddress(ref slot);
                break;
            case 0x06:
                slot.LoopEnd = data;
                if (slot.Envelope < Scsp2DecayEnd)
                    UpdateScsp2SlotAddress(ref slot);
                break;
            case 0x08:
                slot.Sustain = (data >> 11) & 0x1F;
                slot.Decay = (data >> 6) & 0x1F;
                slot.Attack = data & 0x1F;
                UpdateScsp2SlotEnvelope(ref slot);
                break;
            case 0x0A:
                data &= 0x7FFF;
                int rateScaling = (data >> 10) & 0xF;
                slot.KeyRateShift = rateScaling == 0xF ? 4 : rateScaling >> 2;
                slot.Release = data & 0x1F;
                UpdateScsp2SlotEnvelope(ref slot);
                slot.SustainTarget = (((data >> 5) & 0x1F) << (5 + Scsp2FractionBits)) + Scsp2DecayStart;
                break;
            case 0x0C:
                data &= 0x03FF;
                slot.Volume = _scsp2TotalLevel[data & 0xFF];
                break;
            case 0x10:
                data &= 0x7BFF;
                int octave = (data >> 11) & 0xF;
                slot.OctaveShift = (octave & 8) != 0 ? 23 - octave : 7 - octave;
                slot.Step = ((0x400u + (uint)(data & 0x3FF)) << 7) >> slot.OctaveShift;
                UpdateScsp2SlotEnvelope(ref slot);
                break;
            case 0x12:
                slot.FrequencyWave = (data >> 8) & 3;
                slot.AmplitudeWave = (data >> 3) & 3;
                if ((data & 0x8000) != 0)
                {
                    slot.LfoStep = -1;
                    slot.LfoCounter = 0;
                    slot.FrequencyShift = -1;
                    slot.AmplitudeShift = -1;
                }
                else
                {
                    slot.LfoStep = _scsp2LfoStep[(data >> 10) & 0x1F];
                    slot.FrequencyShift = ((data >> 5) & 7) - 1;
                    int strength = data & 7;
                    slot.AmplitudeShift = strength == 0 ? -1 : 11 - strength;
                }
                UpdateScsp2SlotFunction(ref slot);
                break;
            case 0x14:
                data &= 0x007F;
                break;
            case 0x16:
                int level = (data >> 13) & 7;
                int pan = (data >> 8) & 0x1F;
                if (level == 0)
                {
                    level = (data >> 5) & 7;
                    pan = data & 0x1F;
                }
                slot.LeftShift = slot.RightShift = level == 0 ? 31 : 7 - level + 10;
                if (level != 0)
                {
                    if ((pan & 0x10) != 0)
                        slot.RightShift = pan == 0x1F ? 31 : slot.RightShift + ((pan >> 1) & 7);
                    else
                        slot.LeftShift = pan == 0xF ? 31 : slot.LeftShift + ((pan >> 1) & 7);
                }
                UpdateScsp2SlotFunction(ref slot);
                break;
        }
        BitConverter.TryWriteBytes(_registerRam.AsSpan((int)(address ^ 2)), data);
    }

    private void ApplyScsp2Keys()
    {
        for (int i = 0; i < _scsp2Slots.Length; i++)
        {
            ref Scsp2Slot slot = ref _scsp2Slots[i];
            if (slot.Key)
            {
                if (slot.Phase != Scsp2EnvelopePhase.Release)
                    continue;
                UpdateScsp2SlotAddress(ref slot);
                slot.Address = 0;
                slot.Phase = Scsp2EnvelopePhase.Attack;
                slot.Envelope = 0;
                slot.EnvelopeStep = slot.AttackStep;
                slot.EnvelopeTarget = Scsp2DecayStart - 1;
            }
            else if (slot.Phase != Scsp2EnvelopePhase.Release)
            {
                if (slot.Phase == Scsp2EnvelopePhase.Attack)
                    slot.Envelope = Scsp2DecayEnd - slot.Envelope;
                slot.Phase = Scsp2EnvelopePhase.Release;
                slot.EnvelopeStep = slot.ReleaseStep;
                slot.EnvelopeTarget = Scsp2DecayEnd;
            }
        }
    }

    private void UpdateScsp2SlotAddress(ref Scsp2Slot slot)
    {
        if (slot.Pcm8)
            slot.Start &= ~1u;
        slot.Start &= SoundCpuRamMask;
        slot.SampleBase = slot.Start;
        uint maxSamples = SoundCpuRamMask - slot.Start;
        if (slot.Pcm8)
            maxSamples >>= 1;
        slot.LoopStart = Math.Min(slot.LoopStart, maxSamples);
        slot.LoopEnd = Math.Min(slot.LoopEnd, maxSamples);
        slot.ShiftedLoopStart = slot.LoopStart << Scsp2FractionBits;
        slot.ShiftedLoopEnd = ((slot.LoopEnd + 1) << Scsp2FractionBits) - 1;
        slot.ShiftedLoopLength = unchecked(slot.ShiftedLoopEnd - slot.ShiftedLoopStart + 1);
    }

    private void UpdateScsp2SlotEnvelope(ref Scsp2Slot slot)
    {
        int shift = (15 - slot.OctaveShift) >> slot.KeyRateShift;
        slot.SustainStep = slot.Sustain == 0 ? 0 : _scsp2DecayRate[(slot.Sustain << 1) + shift];
        slot.DecayStep = slot.Decay == 0 ? 0 : _scsp2DecayRate[(slot.Decay << 1) + shift];
        slot.AttackStep = slot.Attack == 0 ? 0 : _scsp2AttackRate[(slot.Attack << 1) + shift];
        slot.ReleaseStep = slot.Release == 0 ? 0 : _scsp2DecayRate[(slot.Release << 1) + shift];
    }

    // The 25 DEFINE_AUDIOGEN instantiations from scsp2.c.
    private static void GenerateNull(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GeneratefaslR(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex;
                    int sample = offset < chip._soundRam.Length ? (sbyte)T2ReadByte(chip._soundRam, offset) << 8 : 0;
                    sample *= volume;
                    right[i] += sample >> slot.RightShift;
                }
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GeneratefasLr(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex;
                    int sample = offset < chip._soundRam.Length ? (sbyte)T2ReadByte(chip._soundRam, offset) << 8 : 0;
                    sample *= volume;
                    left[i] += sample >> slot.LeftShift;
                }
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GeneratefasLR(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex;
                    int sample = offset < chip._soundRam.Length ? (sbyte)T2ReadByte(chip._soundRam, offset) << 8 : 0;
                    sample *= volume;
                    left[i] += sample >> slot.LeftShift;
                    right[i] += sample >> slot.RightShift;
                }
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GeneratefaSlR(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex * 2;
                    int sample = offset + 1 < chip._soundRam.Length ? (short)T2ReadWord(chip._soundRam, offset) : 0;
                    sample *= volume;
                    right[i] += sample >> slot.RightShift;
                }
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GeneratefaSLr(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex * 2;
                    int sample = offset + 1 < chip._soundRam.Length ? (short)T2ReadWord(chip._soundRam, offset) : 0;
                    sample *= volume;
                    left[i] += sample >> slot.LeftShift;
                }
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GeneratefaSLR(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex * 2;
                    int sample = offset + 1 < chip._soundRam.Length ? (short)T2ReadWord(chip._soundRam, offset) : 0;
                    sample *= volume;
                    left[i] += sample >> slot.LeftShift;
                    right[i] += sample >> slot.RightShift;
                }
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GeneratefAslR(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                volume -= chip._scsp2Amplitude[slot.AmplitudeWave, lfoPosition] >> slot.AmplitudeShift;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex;
                    int sample = offset < chip._soundRam.Length ? (sbyte)T2ReadByte(chip._soundRam, offset) << 8 : 0;
                    sample *= volume;
                    right[i] += sample >> slot.RightShift;
                }
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GeneratefAsLr(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                volume -= chip._scsp2Amplitude[slot.AmplitudeWave, lfoPosition] >> slot.AmplitudeShift;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex;
                    int sample = offset < chip._soundRam.Length ? (sbyte)T2ReadByte(chip._soundRam, offset) << 8 : 0;
                    sample *= volume;
                    left[i] += sample >> slot.LeftShift;
                }
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GeneratefAsLR(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                volume -= chip._scsp2Amplitude[slot.AmplitudeWave, lfoPosition] >> slot.AmplitudeShift;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex;
                    int sample = offset < chip._soundRam.Length ? (sbyte)T2ReadByte(chip._soundRam, offset) << 8 : 0;
                    sample *= volume;
                    left[i] += sample >> slot.LeftShift;
                    right[i] += sample >> slot.RightShift;
                }
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GeneratefASlR(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                volume -= chip._scsp2Amplitude[slot.AmplitudeWave, lfoPosition] >> slot.AmplitudeShift;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex * 2;
                    int sample = offset + 1 < chip._soundRam.Length ? (short)T2ReadWord(chip._soundRam, offset) : 0;
                    sample *= volume;
                    right[i] += sample >> slot.RightShift;
                }
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GeneratefASLr(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                volume -= chip._scsp2Amplitude[slot.AmplitudeWave, lfoPosition] >> slot.AmplitudeShift;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex * 2;
                    int sample = offset + 1 < chip._soundRam.Length ? (short)T2ReadWord(chip._soundRam, offset) : 0;
                    sample *= volume;
                    left[i] += sample >> slot.LeftShift;
                }
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GeneratefASLR(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                volume -= chip._scsp2Amplitude[slot.AmplitudeWave, lfoPosition] >> slot.AmplitudeShift;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex * 2;
                    int sample = offset + 1 < chip._soundRam.Length ? (short)T2ReadWord(chip._soundRam, offset) : 0;
                    sample *= volume;
                    left[i] += sample >> slot.LeftShift;
                    right[i] += sample >> slot.RightShift;
                }
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GenerateFaslR(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex;
                    int sample = offset < chip._soundRam.Length ? (sbyte)T2ReadByte(chip._soundRam, offset) << 8 : 0;
                    sample *= volume;
                    right[i] += sample >> slot.RightShift;
                }
                address += (uint)((chip._scsp2Frequency[slot.FrequencyWave, lfoPosition] << slot.FrequencyShift) >> slot.OctaveShift);
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GenerateFasLr(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex;
                    int sample = offset < chip._soundRam.Length ? (sbyte)T2ReadByte(chip._soundRam, offset) << 8 : 0;
                    sample *= volume;
                    left[i] += sample >> slot.LeftShift;
                }
                address += (uint)((chip._scsp2Frequency[slot.FrequencyWave, lfoPosition] << slot.FrequencyShift) >> slot.OctaveShift);
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GenerateFasLR(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex;
                    int sample = offset < chip._soundRam.Length ? (sbyte)T2ReadByte(chip._soundRam, offset) << 8 : 0;
                    sample *= volume;
                    left[i] += sample >> slot.LeftShift;
                    right[i] += sample >> slot.RightShift;
                }
                address += (uint)((chip._scsp2Frequency[slot.FrequencyWave, lfoPosition] << slot.FrequencyShift) >> slot.OctaveShift);
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GenerateFaSlR(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex * 2;
                    int sample = offset + 1 < chip._soundRam.Length ? (short)T2ReadWord(chip._soundRam, offset) : 0;
                    sample *= volume;
                    right[i] += sample >> slot.RightShift;
                }
                address += (uint)((chip._scsp2Frequency[slot.FrequencyWave, lfoPosition] << slot.FrequencyShift) >> slot.OctaveShift);
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GenerateFaSLr(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex * 2;
                    int sample = offset + 1 < chip._soundRam.Length ? (short)T2ReadWord(chip._soundRam, offset) : 0;
                    sample *= volume;
                    left[i] += sample >> slot.LeftShift;
                }
                address += (uint)((chip._scsp2Frequency[slot.FrequencyWave, lfoPosition] << slot.FrequencyShift) >> slot.OctaveShift);
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GenerateFaSLR(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex * 2;
                    int sample = offset + 1 < chip._soundRam.Length ? (short)T2ReadWord(chip._soundRam, offset) : 0;
                    sample *= volume;
                    left[i] += sample >> slot.LeftShift;
                    right[i] += sample >> slot.RightShift;
                }
                address += (uint)((chip._scsp2Frequency[slot.FrequencyWave, lfoPosition] << slot.FrequencyShift) >> slot.OctaveShift);
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GenerateFAslR(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                volume -= chip._scsp2Amplitude[slot.AmplitudeWave, lfoPosition] >> slot.AmplitudeShift;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex;
                    int sample = offset < chip._soundRam.Length ? (sbyte)T2ReadByte(chip._soundRam, offset) << 8 : 0;
                    sample *= volume;
                    right[i] += sample >> slot.RightShift;
                }
                address += (uint)((chip._scsp2Frequency[slot.FrequencyWave, lfoPosition] << slot.FrequencyShift) >> slot.OctaveShift);
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GenerateFAsLr(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                volume -= chip._scsp2Amplitude[slot.AmplitudeWave, lfoPosition] >> slot.AmplitudeShift;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex;
                    int sample = offset < chip._soundRam.Length ? (sbyte)T2ReadByte(chip._soundRam, offset) << 8 : 0;
                    sample *= volume;
                    left[i] += sample >> slot.LeftShift;
                }
                address += (uint)((chip._scsp2Frequency[slot.FrequencyWave, lfoPosition] << slot.FrequencyShift) >> slot.OctaveShift);
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GenerateFAsLR(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                volume -= chip._scsp2Amplitude[slot.AmplitudeWave, lfoPosition] >> slot.AmplitudeShift;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex;
                    int sample = offset < chip._soundRam.Length ? (sbyte)T2ReadByte(chip._soundRam, offset) << 8 : 0;
                    sample *= volume;
                    left[i] += sample >> slot.LeftShift;
                    right[i] += sample >> slot.RightShift;
                }
                address += (uint)((chip._scsp2Frequency[slot.FrequencyWave, lfoPosition] << slot.FrequencyShift) >> slot.OctaveShift);
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GenerateFASlR(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                volume -= chip._scsp2Amplitude[slot.AmplitudeWave, lfoPosition] >> slot.AmplitudeShift;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex * 2;
                    int sample = offset + 1 < chip._soundRam.Length ? (short)T2ReadWord(chip._soundRam, offset) : 0;
                    sample *= volume;
                    right[i] += sample >> slot.RightShift;
                }
                address += (uint)((chip._scsp2Frequency[slot.FrequencyWave, lfoPosition] << slot.FrequencyShift) >> slot.OctaveShift);
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GenerateFASLr(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                volume -= chip._scsp2Amplitude[slot.AmplitudeWave, lfoPosition] >> slot.AmplitudeShift;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex * 2;
                    int sample = offset + 1 < chip._soundRam.Length ? (short)T2ReadWord(chip._soundRam, offset) : 0;
                    sample *= volume;
                    left[i] += sample >> slot.LeftShift;
                }
                address += (uint)((chip._scsp2Frequency[slot.FrequencyWave, lfoPosition] << slot.FrequencyShift) >> slot.OctaveShift);
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static void GenerateFASLR(SaturnScsp chip, ref Scsp2Slot slot, Span<int> left, Span<int> right)
    {
        uint address = slot.Address;
        uint step = slot.Step;
        int envelope = slot.Envelope;
        int envelopeStep = slot.EnvelopeStep;

        unchecked
        {
            for (int i = 0; i < left.Length; i++)
            {
                int lfoPosition = (int)((slot.LfoCounter >> Scsp2FractionBits) & 1023);
                int volume = chip._scsp2Envelope[envelope >> Scsp2FractionBits] * slot.Volume >> 10;
                volume -= chip._scsp2Amplitude[slot.AmplitudeWave, lfoPosition] >> slot.AmplitudeShift;
                slot.LastEnvelope = volume;
                if (volume > 0)
                {
                    uint sampleIndex = address >> Scsp2FractionBits;
                    uint offset = slot.SampleBase + sampleIndex * 2;
                    int sample = offset + 1 < chip._soundRam.Length ? (short)T2ReadWord(chip._soundRam, offset) : 0;
                    sample *= volume;
                    left[i] += sample >> slot.LeftShift;
                    right[i] += sample >> slot.RightShift;
                }
                address += (uint)((chip._scsp2Frequency[slot.FrequencyWave, lfoPosition] << slot.FrequencyShift) >> slot.OctaveShift);
                address += step;
                if (address > slot.ShiftedLoopEnd)
                {
                    if (slot.LoopControl != 0 && slot.ShiftedLoopLength != 0)
                        address = slot.ShiftedLoopStart + (address - slot.ShiftedLoopStart) % slot.ShiftedLoopLength;
                    else
                    {
                        envelope = Scsp2DecayEnd;
                        goto Done;
                    }
                }

                envelope += envelopeStep;
                if (envelope >= slot.EnvelopeTarget)
                {
                    switch (slot.Phase)
                    {
                        case Scsp2EnvelopePhase.Attack:
                            envelope = Scsp2DecayStart;
                            envelopeStep = slot.EnvelopeStep = slot.DecayStep;
                            slot.EnvelopeTarget = slot.SustainTarget;
                            slot.Phase = Scsp2EnvelopePhase.Decay;
                            break;
                        case Scsp2EnvelopePhase.Decay:
                            envelope = slot.SustainTarget;
                            envelopeStep = slot.EnvelopeStep = slot.SustainStep;
                            slot.EnvelopeTarget = Scsp2DecayEnd;
                            slot.Phase = Scsp2EnvelopePhase.Sustain;
                            break;
                        default:
                            envelope = Scsp2DecayEnd;
                            slot.EnvelopeStep = 0;
                            slot.EnvelopeTarget = Scsp2DecayEnd + 1;
                            goto Done;
                    }
                }
                slot.LfoCounter += (uint)slot.LfoStep;
            }
        }

    Done:
        slot.Address = address;
        slot.Envelope = envelope;
    }

    private static readonly Scsp2AudioGenerator[,,,,] Scsp2AudioGenerators =
    {
        {
            {
                { { GenerateNull, GeneratefaslR }, { GeneratefasLr, GeneratefasLR } },
                { { GenerateNull, GeneratefaSlR }, { GeneratefaSLr, GeneratefaSLR } },
            },
            {
                { { GenerateNull, GeneratefAslR }, { GeneratefAsLr, GeneratefAsLR } },
                { { GenerateNull, GeneratefASlR }, { GeneratefASLr, GeneratefASLR } },
            },
        },
        {
            {
                { { GenerateNull, GenerateFaslR }, { GenerateFasLr, GenerateFasLR } },
                { { GenerateNull, GenerateFaSlR }, { GenerateFaSLr, GenerateFaSLR } },
            },
            {
                { { GenerateNull, GenerateFAslR }, { GenerateFAsLr, GenerateFAsLR } },
                { { GenerateNull, GenerateFASlR }, { GenerateFASLr, GenerateFASLR } },
            },
        },
    };

    private static void UpdateScsp2SlotFunction(ref Scsp2Slot slot)
    {
        if (slot.Source != 0)
            slot.Generate = Scsp2AudioGenerators[0, 0, 0, 0, 0];
        else
            slot.Generate = Scsp2AudioGenerators[slot.FrequencyShift >= 0 ? 1 : 0,
                slot.AmplitudeShift >= 0 ? 1 : 0, slot.Pcm8 ? 0 : 1,
                slot.LeftShift != 31 ? 1 : 0, slot.RightShift != 31 ? 1 : 0];
    }

    private const int ScspSoundBufferSize = 10 * (44100 / 50);
    private readonly int[] _newOutputLeft = new int[ScspSoundBufferSize];
    private readonly int[] _newOutputRight = new int[ScspSoundBufferSize];
    private int _newOutputPosition;
    private int _newOutputRemaining;
    private bool _scspFrameAccurate;
    private const uint ScspClockFrequency = 44100 * 256;
    private const uint ScspClockMaxExecution = ScspClockFrequency / 1000;
    private const uint ScspClockIncrementNtsc = (uint)(((ulong)ScspClockFrequency << 20) * 1001 / 60000 / 263 / 10);
    private const uint ScspClockIncrementPal = (uint)(((ulong)ScspClockFrequency << 20) / 50 / 313 / 10);
    private uint _scspClockIncrement = ScspClockIncrementNtsc;
    private uint _scspClockFraction;
    private uint _scspSampleTimer;
    private const int Scsp2CddaSectorSize = 2352;
    private const int Scsp2CddaNumBuffers = 3;
    private const int Scsp2CddaDelaySamples = 100;
    private readonly byte[] _scsp2Cdda = new byte[Scsp2CddaNumBuffers * Scsp2CddaSectorSize];
    private volatile int _scsp2CddaNextIn;
    private volatile int _scsp2CddaNextOut;
    private int _scsp2CddaDelay;
    private bool _dmaActive;

    private Thread? _scsp2Thread;
    private AutoResetEvent? _scsp2WakeEvent;
    private bool _scsp2ThreadRunning;
    private ExceptionDispatchInfo? _scsp2ThreadFailure;
    private uint _scsp2Clock;
    private uint _scsp2ClockTarget;
    private uint _scsp2WriteAddress;
    private uint _scsp2WriteData;
    private int _scsp2WriteSize;
    private int _scsp2MainInterruptPending;

    public void SetFrameAccurate(bool enabled)
    {
        SynchronizeScsp2Thread();
        _scspFrameAccurate = enabled;
    }

    private void StartScsp2Thread()
    {
        var thread = new Thread(RunScsp2Thread) { IsBackground = true, Name = "Saturn SCSP2" };
        var wakeEvent = new AutoResetEvent(false);
        _scsp2WakeEvent = wakeEvent;
        Volatile.Write(ref _scsp2ThreadRunning, true);
        try
        {
            thread.Start();
            _scsp2Thread = thread;
        }
        catch (ThreadStateException) { }
        catch (OutOfMemoryException) { }
        finally
        {
            if (_scsp2Thread is null)
            {
                Volatile.Write(ref _scsp2ThreadRunning, false);
                _scsp2WakeEvent = null;
                wakeEvent.Dispose();
            }
        }
    }

    private void StopScsp2Thread()
    {
        Thread? thread = _scsp2Thread;
        if (thread is null)
            return;

        AutoResetEvent wakeEvent = _scsp2WakeEvent!;
        try
        {
            Volatile.Write(ref _scsp2ThreadRunning, false);
            wakeEvent.Set();
        }
        finally
        {
            try { thread.Join(); }
            finally
            {
                _scsp2Thread = null;
                _scsp2WakeEvent = null;
                wakeEvent.Dispose();
            }
        }
    }

    private void WaitForScsp2Progress()
    {
        Volatile.Read(ref _scsp2ThreadFailure)?.Throw();
        if (!Volatile.Read(ref _scsp2ThreadRunning))
        {
            Volatile.Read(ref _scsp2ThreadFailure)?.Throw();
            throw new ObjectDisposedException(nameof(SaturnScsp));
        }
        _scsp2WakeEvent!.Set();
        Thread.Yield();
    }

    private void SynchronizeScsp2Thread()
    {
        if (_scsp2Thread is null)
            return;

        Volatile.Read(ref _scsp2ThreadFailure)?.Throw();
        while (Volatile.Read(ref _scsp2Clock) != _scsp2ClockTarget)
            WaitForScsp2Progress();
    }

    private bool QueueScsp2Write(uint address, uint data, int size)
    {
        if (_scsp2Thread is null)
            return false;

        Volatile.Read(ref _scsp2ThreadFailure)?.Throw();
        _scsp2WriteAddress = address & 0xFFF;
        _scsp2WriteData = data;
        Volatile.Write(ref _scsp2WriteSize, size);
        while (Volatile.Read(ref _scsp2WriteSize) != 0)
            WaitForScsp2Progress();
        return true;
    }

    private void RequestScsp2MainInterrupt()
    {
        if (_scsp2Thread is null)
            _registers.SendMainInterrupt();
        else
            Interlocked.Exchange(ref _scsp2MainInterruptPending, 1);
    }

    // scsp2.c, ScspExec.
    public void ExecuteNewScsp(uint decilines)
    {
        _scspClockFraction = unchecked(_scspClockFraction + _scspClockIncrement * decilines);
        uint cycles = _scspClockFraction >> 20;
        _scspClockFraction &= 0xFFFFF;
        if (_scsp2Thread is null)
        {
            ExecuteScspCycles(cycles);
            return;
        }

        Volatile.Read(ref _scsp2ThreadFailure)?.Throw();
        uint newTarget = unchecked(_scsp2ClockTarget + cycles);
        Volatile.Write(ref _scsp2ClockTarget, newTarget);
        while (unchecked(newTarget - Volatile.Read(ref _scsp2Clock)) > ScspClockMaxExecution)
            WaitForScsp2Progress();
        if (Interlocked.Exchange(ref _scsp2MainInterruptPending, 0) != 0)
            _registers.SendMainInterrupt();
    }

    // scsp2.c, ScspThread.
    private void RunScsp2Thread()
    {
        AutoResetEvent wakeEvent = _scsp2WakeEvent!;
        try
        {
            while (Volatile.Read(ref _scsp2ThreadRunning))
            {
                int writeSize = Volatile.Read(ref _scsp2WriteSize);
                if (writeSize != 0)
                {
                    uint address = _scsp2WriteAddress;
                    uint data = _scsp2WriteData;
                    if (writeSize == 1)
                        WriteByteDirect(address, unchecked((byte)data));
                    else if (writeSize == 2)
                        WriteWordDirect(address, unchecked((ushort)data));
                    else
                    {
                        WriteWordDirect(address, (ushort)(data >> 16));
                        WriteWordDirect(address + 2, unchecked((ushort)data));
                    }

                    Volatile.Write(ref _scsp2WriteSize, 0);
                }

                uint clock = _scsp2Clock;
                uint cycles = Math.Min(unchecked(Volatile.Read(ref _scsp2ClockTarget) - clock), ScspClockMaxExecution);
                if (cycles > 0)
                {
                    ExecuteScspCycles(cycles);
                    Volatile.Write(ref _scsp2Clock, unchecked(clock + cycles));
                    Thread.Yield();
                }
                else
                    wakeEvent.WaitOne();
            }
        }
        catch (Exception failure)
        {
            Volatile.Write(ref _scsp2ThreadFailure, ExceptionDispatchInfo.Capture(failure));
        }
        finally
        {
            Volatile.Write(ref _scsp2ThreadRunning, false);
        }
    }

    // scsp2.c, ScspDoExec.
    private void ExecuteScspCycles(uint cycles)
    {
        uint sampleCount = 0;
        uint cyclesLeft = cycles;
        while (cyclesLeft > 0)
        {
            uint sliceCycles = cyclesLeft;
            if ((_soundInterruptEnable & 0x40) != 0)
                sliceCycles = Math.Min(sliceCycles, TimerCyclesLeft(_timers[0]));
            if ((_soundInterruptEnable & 0x80) != 0)
                sliceCycles = Math.Min(sliceCycles, TimerCyclesLeft(_timers[1]));
            if ((_soundInterruptEnable & 0x100) != 0)
                sliceCycles = Math.Min(sliceCycles, TimerCyclesLeft(_timers[2]));

            _scspSampleTimer += sliceCycles;
            uint samples = _scspSampleTimer >> 8;
            _scspSampleTimer &= 0xFF;
            cyclesLeft -= sliceCycles;
            sampleCount += samples;

            ExecuteSoundCpu((int)sliceCycles);
            UpdateTimers(samples);
        }

        int audioFree;
        if (_scspFrameAccurate)
        {
            int samplesLeft = (int)sampleCount;
            if (_newOutputRemaining + samplesLeft > ScspSoundBufferSize)
            {
                int overrun = _newOutputRemaining + samplesLeft - ScspSoundBufferSize;
                _newOutputRemaining -= overrun;
            }

            while (samplesLeft > 0)
            {
                if (_newOutputPosition >= ScspSoundBufferSize)
                    _newOutputPosition = 0;
                int count = Math.Min(samplesLeft, ScspSoundBufferSize - _newOutputPosition);
                GenerateScsp2Audio(_newOutputLeft.AsSpan(_newOutputPosition, count),
                    _newOutputRight.AsSpan(_newOutputPosition, count));
                _newOutputPosition += count;
                _newOutputRemaining += count;
                samplesLeft -= count;
            }

            while (_newOutputRemaining > 0 && (audioFree = _registers.GetAudioSpace()) > 0)
            {
                int outputStart = _newOutputPosition - _newOutputRemaining;
                if (outputStart < 0)
                    outputStart += ScspSoundBufferSize;
                audioFree = Math.Min(audioFree, _newOutputRemaining);
                audioFree = Math.Min(audioFree, ScspSoundBufferSize - outputStart);
                _registers.UpdateAudio(_newOutputLeft.AsSpan(outputStart, audioFree),
                    _newOutputRight.AsSpan(outputStart, audioFree), audioFree);
                _newOutputRemaining -= audioFree;
            }
        }
        else if ((audioFree = _registers.GetAudioSpace()) > 0)
        {
            audioFree = Math.Min(audioFree, ScspSoundBufferSize);
            GenerateScsp2Audio(_newOutputLeft.AsSpan(0, audioFree), _newOutputRight.AsSpan(0, audioFree));
            _registers.UpdateAudio(_newOutputLeft.AsSpan(0, audioFree),
                _newOutputRight.AsSpan(0, audioFree), audioFree);
        }
    }

    private static uint TimerCyclesLeft(Timer timer) => unchecked((0xFF00u - timer.Counter) << timer.Control);

    // scsp2.c, ScspUpdateTimer.
    private void UpdateNewTimer(uint samples, ref Timer timer, int id)
    {
        uint next = timer.Counter + (samples << (8 - timer.Control));
        if (next >= 0xFF00)
        {
            MainInterrupt(id);
            SoundInterrupt(id);
            next -= 0xFF00;
        }
        timer.Counter = unchecked((ushort)next);
    }

    // scsp2.c, ScspGenerateAudio.
    private void GenerateScsp2Audio(Span<int> left, Span<int> right)
    {
        int samples = left.Length;
        left.Clear();
        right.Clear();
        for (int i = 0; i < _scsp2Slots.Length; i++)
        {
            ref Scsp2Slot slot = ref _scsp2Slots[i];
            if (slot.Envelope < Scsp2DecayEnd)
                slot.Generate(this, ref slot, left, right);
        }

        if (_scsp2CddaNextOut != _scsp2CddaNextIn * Scsp2CddaSectorSize)
        {
            if (_scsp2CddaDelay > 0)
            {
                if (samples > _scsp2CddaDelay)
                {
                    samples -= _scsp2CddaDelay;
                    _scsp2CddaDelay = 0;
                }
                else
                {
                    _scsp2CddaDelay -= samples;
                    samples = 0;
                }
            }
            if (_scsp2CddaDelay == 0)
                GenerateScsp2Cdda(left, right, samples);
        }
        if (_scsp2CddaNextOut == _scsp2CddaNextIn * Scsp2CddaSectorSize)
            _scsp2CddaDelay = Scsp2CddaDelaySamples;
    }

    // scsp2.c, ScspGenerateAudioForCDDA.
    private void GenerateScsp2Cdda(Span<int> left, Span<int> right, int samples)
    {
        int output = 0;
        while (samples > 0)
        {
            int nextOut = _scsp2CddaNextOut;
            int pending = _scsp2CddaNextIn * Scsp2CddaSectorSize - nextOut;
            int bytesLeft = pending < 0 ? _scsp2Cdda.Length - nextOut : pending;
            int count = Math.Min(samples, bytesLeft / 4);
            if (count == 0)
                break;

            int end = nextOut + count * 4;
            for (int i = nextOut; i < end; i += 4, output++)
            {
                left[output] += unchecked((short)((_scsp2Cdda[i + 1] << 8) | _scsp2Cdda[i]));
                right[output] += unchecked((short)((_scsp2Cdda[i + 3] << 8) | _scsp2Cdda[i + 2]));
            }

            _scsp2CddaNextOut = end >= _scsp2Cdda.Length ? 0 : end;
            samples -= count;
        }
    }

    // scsp2.c, ScspReadWordDirect, for the common register page.
    private ushort ReadScsp2CommonWord(uint address)
    {
        switch (address)
        {
            case 0x04:
                return ReadScsp2MidiInput();
            case 0x08:
                ref Scsp2Slot slot = ref _scsp2Slots[_monitorSlot];
                uint addressCounter = (slot.Address >> (Scsp2FractionBits + 12)) & 0xF;
                int phase = 3 - (int)slot.Phase;
                int envelope = 31 - (slot.LastEnvelope >> 27);
                return unchecked((ushort)((addressCounter << 7) | (uint)(phase << 5) | (uint)envelope));
            case 0x20:
                return unchecked((ushort)_soundInterruptPending);
            case 0x2C:
                return unchecked((ushort)_mainInterruptPending);
            default:
                return BitConverter.ToUInt16(_registerRam.AsSpan(0x400 + (int)(address ^ 2)));
        }
    }

    // scsp2.c, ScspWriteByteDirect, for the common register page.
    private void WriteScsp2CommonByte(uint address, byte data)
    {
        switch (address)
        {
            case 0x1E:
                data &= 7;
                _soundInterruptEnable = (_soundInterruptEnable & 0xFF) | (data << 8);
                CheckSoundInterrupts(0x700);
                break;
            case 0x1F:
                _soundInterruptEnable = (_soundInterruptEnable & 0xFF00) | data;
                CheckSoundInterrupts(0x700);
                break;
            case 0x20:
            case 0x2C:
                return;
            case 0x21:
                if ((data & 0x20) != 0)
                    SoundInterrupt(0x20);
                return;
            case 0x2A:
                data &= 7;
                _mainInterruptEnable = (_mainInterruptEnable & 0xFF) | (data << 8);
                CheckMainInterrupts(0x700);
                break;
            case 0x2B:
                _mainInterruptEnable = (_mainInterruptEnable & 0xFF00) | data;
                CheckMainInterrupts(0x700);
                break;
            case 0x2D:
                if ((data & 0x20) != 0)
                    MainInterrupt(0x20);
                return;
            case <= 0x09:
            case >= 0x12 and <= 0x1D:
            case >= 0x22 and <= 0x29:
            case 0x2E:
            case 0x2F:
            {
                uint aligned = address & ~1u;
                ushort cached = BitConverter.ToUInt16(_registerRam.AsSpan(0x400 + (int)(aligned ^ 2)));
                ushort word = (address & 1) != 0
                    ? (ushort)((cached & 0xFF00) | data)
                    : (ushort)((cached & 0x00FF) | (data << 8));
                WriteScsp2CommonWord(aligned, word);
                return;
            }
        }

        _registerRam[0x400 + (address ^ 3)] = data;
    }

    // scsp2.c, ScspWriteWordDirect, for the common register page.
    private void WriteScsp2CommonWord(uint address, ushort data)
    {
        switch (address)
        {
            case 0x00:
                data &= 0x030F;
                _mem4b = (uint)((data >> 9) & 1);
                _masterVolume = data & 0xF;
                UpdateSoundCpuFetch();
                break;
            case 0x02:
                data &= 0x01FF;
                _ringLength = (data >> 7) & 3;
                _ringBase = (data & 0x7F) << 13;
                break;
            case 0x04:
                return;
            case 0x06:
                data &= 0x00FF;
                SendMidiOutput((byte)data);
                break;
            case 0x08:
                data &= 0x7800;
                _monitorSlot = (data >> 11) & 0x1F;
                break;
            case 0x12:
                data &= 0xFFFE;
                _dmaMemoryAddress = (_dmaMemoryAddress & 0xF0000) | data;
                break;
            case 0x14:
                data &= 0xFFFE;
                _dmaMemoryAddress = (_dmaMemoryAddress & 0xFFFF) | ((data & 0xF000) << 4);
                _dmaRegisterAddress = data & 0xFFF;
                break;
            case 0x16:
                data &= 0x7FFE;
                _dmaFlags = ((data >> 8) & 0x70) | (_dmaFlags & 0x10);
                _dmaLength = data & 0xFFF;
                if ((data & 0x1000) != 0)
                    ExecuteScsp2Dma();
                break;
            case 0x18:
            case 0x1A:
            case 0x1C:
                data &= 0x07FF;
                ref Timer timer = ref _timers[(address - 0x18) >> 1];
                timer.Control = (data >> 8) & 7;
                timer.Counter = (uint)(data & 0xFF) << 8;
                break;
            case 0x1E:
                data &= 0x07FF;
                _soundInterruptEnable = data;
                CheckSoundInterrupts(0x7FF);
                break;
            case 0x20:
                if ((data & 0x20) != 0)
                    SoundInterrupt(0x20);
                return;
            case 0x22:
                _soundInterruptPending &= ~data;
                return;
            case 0x24:
                data &= 0x00FF;
                _soundInterruptLevel0 = data;
                break;
            case 0x26:
                data &= 0x00FF;
                _soundInterruptLevel1 = data;
                break;
            case 0x28:
                data &= 0x00FF;
                _soundInterruptLevel2 = data;
                break;
            case 0x2A:
                data &= 0x07FF;
                _mainInterruptEnable = data;
                CheckMainInterrupts(0x7FF);
                break;
            case 0x2C:
                if ((data & 0x20) != 0)
                    MainInterrupt(0x20);
                return;
            case 0x2E:
                _mainInterruptPending &= ~data;
                return;
        }

        BitConverter.TryWriteBytes(_registerRam.AsSpan(0x400 + (int)(address ^ 2)), data);
    }

    private ushort ReadScsp2MidiInput()
    {
        _midiFlags &= ~(MidiInputOverflow | MidiInputFull);
        byte data = 0xFF;
        if (_midiInputCount > 0)
        {
            data = _midiInput[0];
            _midiInput[0] = _midiInput[1];
            _midiInput[1] = _midiInput[2];
            _midiInput[2] = _midiInput[3];
            _midiInputCount--;
            if (_midiInputCount == 0)
                _midiFlags |= MidiInputEmpty;
            else
            {
                _midiFlags &= ~MidiInputEmpty;
                MainInterrupt(0x8);
                SoundInterrupt(0x8);
            }
        }

        return (ushort)((_midiFlags << 8) | data);
    }

    // scsp2.c, ScspDoDMA.
    private void ExecuteScsp2Dma()
    {
        if (_dmaActive)
            return;

        uint memoryAddress = (uint)_dmaMemoryAddress & SoundCpuRamMask;
        _dmaActive = true;
        try
        {
            if ((_dmaFlags & 0x20) != 0)
            {
                if ((_dmaFlags & 0x40) != 0)
                {
                    for (int i = 0; i < _dmaLength; i += 2)
                        WriteWordDirect((uint)(_dmaRegisterAddress + i), 0);
                }
                else
                {
                    for (int i = 0; i < _dmaLength && memoryAddress + (uint)i < _soundRam.Length; i += 2)
                        WriteWordDirect((uint)(_dmaRegisterAddress + i), T2ReadWord(_soundRam, memoryAddress + (uint)i));
                }
            }
            else
            {
                int length = Math.Min(_dmaLength, _soundRam.Length - (int)memoryAddress);
                if ((_dmaFlags & 0x40) != 0)
                    _soundRam.AsSpan((int)memoryAddress, length).Clear();
                else
                {
                    for (int i = 0; i < length; i += 2)
                        T2WriteWord(_soundRam, memoryAddress + (uint)i, ReadWord((uint)(_dmaRegisterAddress + i)));
                }
                _soundCpu.WriteNotify(memoryAddress, (uint)length);
            }
        }
        finally
        {
            _dmaActive = false;
            _dmaFlags &= ~0x10;
            _registerRam[0x400 + (0x16 ^ 3)] &= 0xEF;
        }

        MainInterrupt(0x10);
        SoundInterrupt(0x10);
    }
}
