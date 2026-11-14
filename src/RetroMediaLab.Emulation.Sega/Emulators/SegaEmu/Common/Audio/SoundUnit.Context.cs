using BlipBuffer = RetroMediaLab.Chips.Audio.Common.Resampling.BandLimitedBuffer;
using Equalizer = RetroMediaLab.Chips.Audio.Common.Resampling.ThreeBandEqualizer;
using Psg = RetroMediaLab.Chips.Audio.TexasInstruments.SN76489.Instruction.Sn76489;
using PsgType = RetroMediaLab.Chips.Audio.TexasInstruments.SN76489.Instruction.Sn76489Type;
using Ym2413 = RetroMediaLab.Chips.Audio.Yamaha.YM2413.Instruction.Ym2413;
using Ym2612 = RetroMediaLab.Chips.Audio.Yamaha.YM2612.Instruction.Ym2612;
using Ym3438 = RetroMediaLab.Chips.Audio.Yamaha.YM3438.Instruction.Ym3438;
using Opll = RetroMediaLab.Chips.Audio.Yamaha.YM2413.Instruction.CycleOpll;
using System.Buffers.Binary;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common.Audio;

internal sealed partial class SoundUnit
{
    public int ContextSize => 1 + FmContextSize + Psg.ContextSize + sizeof(int);
    private int FmContextSize => _isMd
        ? (_configuration.UseYm3438 ? Ym3438.ContextSize + 48 * sizeof(short) + 3 * sizeof(int) : Ym2612.ContextSize)
        : (_configuration.UseOpll ? Opll.ContextSize + 39 * sizeof(int) : Ym2413.ContextSize);

    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("Sound context buffer is too short.", nameof(state));
        int offset = 1;
        if (_isMd)
        {
            state[0] = _configuration.UseYm3438 ? (byte)1 : (byte)0;
            if (_configuration.UseYm3438)
            {
                offset += _ym3438!.SaveContext(state[offset..]);
                foreach (short value in _ym3438Accumulator) WriteShort(state, ref offset, value);
                foreach (int value in _ym3438Sample) WriteInt(state, ref offset, value);
                WriteInt(state, ref offset, _ym3438Cycles);
            }
            else offset += _ym2612!.SaveContext(state[offset..]);
        }
        else
        {
            state[0] = _configuration.UseOpll ? (byte)1 : (byte)0;
            if (_configuration.UseOpll)
            {
                offset += _opll!.SaveContext(state[offset..]);
                foreach (int value in _opllAccumulator) WriteInt(state, ref offset, value);
                WriteInt(state, ref offset, _opllSample);
                WriteInt(state, ref offset, _opllCycles);
                WriteInt(state, ref offset, _opllStatus);
            }
            else offset += _ym2413!.SaveContext(state[offset..]);
        }
        offset += Psg.SaveContext(state[offset..]);
        WriteInt(state, ref offset, _fmCyclesStart);
        return offset;
    }

    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (state.IsEmpty) throw new ArgumentException("Sound context buffer is too short.", nameof(state));
        bool alternate = state[0] != 0;
        int fmSize = _isMd
            ? (alternate ? Ym3438.ContextSize + 48 * sizeof(short) + 3 * sizeof(int) : Ym2612.ContextSize)
            : (alternate ? Opll.ContextSize + 39 * sizeof(int) : Ym2413.ContextSize);
        if (state.Length < 1 + fmSize + Psg.ContextSize + sizeof(int)) throw new ArgumentException("Sound context buffer is too short.", nameof(state));
        int offset = 1;
        if (_isMd)
        {
            if (alternate)
            {
                _ym3438 ??= new();
                offset += _ym3438.LoadContext(state[offset..]);
                for (int i = 0; i < _ym3438Accumulator.Length; i++) _ym3438Accumulator[i] = ReadShort(state, ref offset);
                for (int i = 0; i < _ym3438Sample.Length; i++) _ym3438Sample[i] = ReadInt(state, ref offset);
                _ym3438Cycles = (int)(unchecked((uint)ReadInt(state, ref offset)) % 24);
            }
            else
            {
                _ym2612 ??= new();
                offset += _ym2612.LoadContext(state[offset..]);
            }
        }
        else if (alternate)
        {
            _opll ??= new();
            offset += _opll.LoadContext(state[offset..]);
            for (int i = 0; i < _opllAccumulator.Length; i++) _opllAccumulator[i] = ReadInt(state, ref offset);
            _opllSample = ReadInt(state, ref offset);
            _opllCycles = (int)(unchecked((uint)ReadInt(state, ref offset)) % 18);
            _opllStatus = ReadInt(state, ref offset);
        }
        else
        {
            _ym2413 ??= new();
            offset += _ym2413.LoadContext(state[offset..]);
        }
        offset += Psg.LoadContext(state[offset..]);
        _fmCyclesCount = _fmCyclesStart = ReadInt(state, ref offset);
        return offset;
    }

    private static void WriteInt(Span<byte> state, ref int offset, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(state[offset..], value); offset += sizeof(int);
    }
    private static int ReadInt(ReadOnlySpan<byte> state, ref int offset)
    {
        int value = BinaryPrimitives.ReadInt32LittleEndian(state[offset..]); offset += sizeof(int); return value;
    }
    private static void WriteShort(Span<byte> state, ref int offset, short value)
    {
        BinaryPrimitives.WriteInt16LittleEndian(state[offset..], value); offset += sizeof(short);
    }
    private static short ReadShort(ReadOnlySpan<byte> state, ref int offset)
    {
        short value = BinaryPrimitives.ReadInt16LittleEndian(state[offset..]); offset += sizeof(short); return value;
    }
}
