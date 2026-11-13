using System.Buffers.Binary;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed class VorbisSynthesis
{
    private readonly VorbisSetup _setup;
    private readonly VorbisBits _bits = new();
    private readonly int[][] _pcm, _history;
    private int _window, _center, _returned, _current;
    private long _granule, _sequence, _sampleCount;
    public int Available => _returned > -1 && _returned < _current ? _current - _returned : 0;
    public long Granule => _granule;
    public int Channels => _setup.Channels;
    public VorbisSynthesis(VorbisSetup setup)
    {
        _setup = setup; _pcm = new int[setup.Channels][]; _history = new int[setup.Channels][];
        for (int i = 0; i < setup.Channels; i++)
        { _pcm[i] = new int[setup.BlockSizes[1]]; _history[i] = new int[setup.BlockSizes[1]]; }
        Restart();
    }
    public void Restart()
    {
        _center = _setup.BlockSizes[1] / 2; _current = _center;
        _returned = -1; _granule = _sequence = _sampleCount = -1;
    }
    public bool DecodePacket(OggPacketInfo packet)
    {
        if (Available != 0) throw new InvalidOperationException("Vorbis output must be consumed before adding another block.");
        _bits.Reset(packet.Data);
        if (_bits.Read(1) != 0) return false;
        int mode = _bits.Read(_setup.ModeBits);
        if ((uint)mode >= _setup.Modes.Length) return false;
        VorbisMode selected = _setup.Modes[mode];
        int previousFlag = 0, nextFlag = 0;
        if (selected.BlockFlag != 0)
        {
            previousFlag = _bits.Read(1); nextFlag = _bits.Read(1);
            if (previousFlag < 0 || nextFlag < 0) return false;
        }
        _setup.Mappings[selected.Mapping].Decode(_bits, _pcm, selected.BlockFlag, previousFlag, nextFlag);
        AddBlock(selected.BlockFlag, packet.Number - 3, packet.Granule, packet.End);
        return true;
    }
    private void AddBlock(int window, long sequence, long granule, bool end)
    {
        int previousWindow = _window; _window = window;
        if (_sequence == -1 || _sequence + 1 != sequence) { _granule = -1; _sampleCount = -1; }
        _sequence = sequence;
        int n = _setup.BlockSizes[window] / 2, n0 = _setup.BlockSizes[0] / 2, n1 = _setup.BlockSizes[1] / 2;
        int currentCenter = _center != 0 ? n1 : 0, previousCenter = _center != 0 ? 0 : n1;
        unchecked
        {
            for (int channel = 0; channel < _setup.Channels; channel++)
            {
                int[] history = _history[channel], pcm = _pcm[channel];
                if (previousWindow != 0)
                {
                    if (window != 0)
                    { for (int i = 0; i < n1; i++) history[previousCenter + i] += pcm[i]; }
                    else
                    {
                        int offset = previousCenter + n1 / 2 - n0 / 2;
                        for (int i = 0; i < n0; i++) history[offset + i] += pcm[i];
                    }
                }
                else if (window != 0)
                {
                    int offset = n1 / 2 - n0 / 2, i = 0;
                    for (; i < n0; i++) history[previousCenter + i] += pcm[offset + i];
                    for (; i < n1 / 2 + n0 / 2; i++) history[previousCenter + i] = pcm[offset + i];
                }
                else for (int i = 0; i < n0; i++) history[previousCenter + i] += pcm[i];
                pcm.AsSpan(n, n).CopyTo(history.AsSpan(currentCenter));
            }
        }
        _center = _center != 0 ? 0 : n1;
        int advance = _setup.BlockSizes[previousWindow] / 4 + _setup.BlockSizes[window] / 4;
        if (_returned == -1) _returned = _current = currentCenter;
        else { _returned = previousCenter; _current = previousCenter + advance; }
        if (_sampleCount == -1) _sampleCount = 0; else _sampleCount += advance;
        if (_granule == -1)
        {
            if (granule != -1)
            {
                _granule = granule;
                if (_sampleCount > _granule)
                {
                    int extra = checked((int)(_sampleCount - _granule));
                    if (end) _current -= extra;
                    else _returned = Math.Min(_current, _returned + extra);
                }
            }
        }
        else
        {
            _granule += advance;
            if (granule != -1 && _granule != granule)
            {
                if (_granule > granule && end) _current -= checked((int)(_granule - granule));
                _granule = granule;
            }
        }
    }
    public int ReadPcm(Span<byte> destination)
    {
        int frames = Math.Min(Available, destination.Length / (Channels * sizeof(short)));
        for (int channel = 0; channel < Channels; channel++)
        {
            int[] source = _history[channel];
            int offset = channel * sizeof(short);
            for (int i = 0; i < frames; i++, offset += Channels * sizeof(short))
            {
                int value = Math.Clamp(source[_returned + i] >> 9, short.MinValue, short.MaxValue);
                BinaryPrimitives.WriteInt16LittleEndian(destination[offset..], (short)value);
            }
        }
        _returned += frames; return frames * Channels * sizeof(short);
    }
    public void Discard(int frames)
    {
        if ((uint)frames > Available) throw new ArgumentOutOfRangeException(nameof(frames));
        _returned += frames;
    }
}
