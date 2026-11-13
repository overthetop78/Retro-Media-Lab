using System.Buffers.Binary;
using System.IO;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed class Mp3Playback : IMp3Playback
{
    private const int InputSize = 128 * 1024, RefillThreshold = 16 * 1024;
    private Stream? _stream;
    private readonly bool _leaveOpen;
    private readonly CancellationToken _cancellation;
    private readonly Mp3FrameDecoder _decoder = new();
    private readonly byte[] _input = new byte[InputSize];
    private readonly short[] _samples = new short[Mp3FrameDecoder.MaximumSamplesPerFrame];
    private readonly List<FrameIndex> _index = [];
    private readonly record struct FrameIndex(long Offset, ulong Sample, int MainDataBytes, int Samples);
    private int _filled, _consumed, _sampleCount, _sampleConsumed;
    private long _bufferOffset, _startOffset;
    private bool _eof, _indexBuilt;
    private ulong _startDelay, _toSkip, _detectedSamples;
    private int _layer;
    public int Channels { get; private set; }
    public int SampleRate { get; private set; }
    public ulong SamplePosition { get; private set; }
    public bool HasError { get; private set; }

    private Mp3Playback(Stream stream, bool leaveOpen, CancellationToken cancellation)
    { _stream = stream; _leaveOpen = leaveOpen; _cancellation = cancellation; }
    public static Mp3Playback Open(Stream stream, bool leaveOpen, CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Mp3Playback playback = new(stream, leaveOpen, cancellation);
        bool opened = false;
        try { playback.Initialize(); opened = true; return playback; }
        finally { if (!opened) playback.Dispose(); }
    }
    private void PositionInput(long offset)
    {
        _cancellation.ThrowIfCancellationRequested();
        _stream!.Seek(offset, SeekOrigin.Begin);
        _bufferOffset = offset; _filled = _consumed = _sampleCount = _sampleConsumed = 0; _eof = false;
        _decoder.Reset();
    }
    private int ReadInput(Span<byte> destination)
    {
        int total = 0;
        while (!destination.IsEmpty)
        {
            _cancellation.ThrowIfCancellationRequested();
            int read = _stream!.Read(destination);
            if (read == 0) break;
            total += read; destination = destination[read..];
        }
        return total;
    }
    private static int TrimEndTags(ReadOnlySpan<byte> input)
    {
        int count = input.Length;
        if (count >= 128 && input.Slice(count - 128, 3).SequenceEqual("TAG"u8))
        {
            count -= 128;
            if (count >= 227 && input.Slice(count - 227, 4).SequenceEqual("TAG+"u8)) count -= 227;
        }
        if (count > 32 && input.Slice(count - 32, 8).SequenceEqual("APETAGEX"u8))
        {
            count -= 32;
            uint size = BinaryPrimitives.ReadUInt32LittleEndian(input[(count + 12)..]);
            if (size <= count) count -= (int)size;
        }
        return count;
    }
    private void Refill()
    {
        if (_eof || _filled - _consumed >= RefillThreshold) return;
        _input.AsSpan(_consumed, _filled - _consumed).CopyTo(_input);
        _bufferOffset += _consumed; _filled -= _consumed; _consumed = 0;
        int capacity = InputSize - _filled, read = ReadInput(_input.AsSpan(_filled, capacity));
        _filled += read;
        if (read != capacity) { _eof = true; _filled = TrimEndTags(_input.AsSpan(0, _filled)); }
    }
    private bool FindHeader(out Mp3FrameInfo info, out int nominalSamples)
    {
        while (true)
        {
            Refill();
            int available = _filled - _consumed;
            if (available == 0) { info = default; nominalSamples = 0; return false; }
            nominalSamples = _decoder.Decode(_input.AsMemory(_consumed), available, [], out info);
            if (nominalSamples != 0) return true;
            if (info.FrameBytes <= 0 || info.FrameBytes > available) return false;
            _consumed += info.FrameBytes;
        }
    }
    private void Initialize()
    {
        PositionInput(0);
        Span<byte> id3 = stackalloc byte[10];
        int count = ReadInput(id3);
        long skip = 0;
        if (count == 10 && id3[..3].SequenceEqual("ID3"u8) &&
            ((id3[5] & 15) | (id3[6] & 128) | (id3[7] & 128) | (id3[8] & 128) | (id3[9] & 128)) == 0)
            skip = ((id3[6] << 21) | (id3[7] << 14) | (id3[8] << 7) | id3[9]) + 10L + ((id3[5] & 16) != 0 ? 10 : 0);
        PositionInput(skip);
        if (!FindHeader(out Mp3FrameInfo info, out int samples) || info.Channels is < 1 or > 2 || info.SampleRate <= 0)
            throw new InvalidDataException("No decodable MP3 frame was found.");
        Channels = info.Channels; SampleRate = info.SampleRate; _layer = info.Layer;
        long frameOffset = _bufferOffset + _consumed + info.FrameOffset;
        _startOffset = frameOffset;
        if (_layer == 3)
        {
            int frameBytes = info.FrameBytes - info.FrameOffset;
            int tag = _decoder.ReadVbrTag(_input.AsMemory(_consumed + info.FrameOffset), frameBytes, out uint frames, out int delay, out int padding);
            if (tag != 0) _startOffset += frameBytes;
            if (tag > 0)
            {
                _startDelay = (ulong)(delay * Channels); _toSkip = _startDelay;
                _detectedSamples = (ulong)samples * (uint)Channels * frames;
                if (_detectedSamples >= _startDelay) _detectedSamples -= _startDelay;
                if (padding > 0 && _detectedSamples >= (ulong)(padding * Channels)) _detectedSamples -= (ulong)(padding * Channels);
            }
        }
        PositionInput(_startOffset);
    }
    private bool DecodeNext()
    {
        while (true)
        {
            Refill();
            int available = _filled - _consumed;
            if (available == 0) return false;
            int decoded = _decoder.Decode(_input.AsMemory(_consumed), available, _samples, out Mp3FrameInfo info);
            if (info.FrameBytes <= 0 || info.FrameBytes > available) return false;
            _consumed += info.FrameBytes;
            _sampleConsumed = 0;
            if (info.SampleRate != SampleRate || info.Layer != _layer) { HasError = true; return false; }
            _sampleCount = decoded * info.Channels;
            if (_sampleCount != 0)
            {
                int skip = (int)Math.Min((ulong)_sampleCount, _toSkip);
                _sampleConsumed = skip; _toSkip -= (uint)skip;
                if (_sampleConsumed != _sampleCount && Channels != info.Channels) { HasError = true; return false; }
            }
            else if (_toSkip != 0)
            {
                // Failed Layer III frames still count towards a seek's skip,
                // while their bit reservoir is being restored.
                int nominal = (_input[_consumed - info.FrameBytes + info.FrameOffset + 1] & 14) == 2 ? 576 : 1152;
                if (_layer == 1) nominal = 384;
                _toSkip -= Math.Min(_toSkip, (ulong)(nominal * info.Channels));
            }
            if (_sampleConsumed < _sampleCount) return true;
        }
    }
    public int ReadSamples(Span<short> destination)
    {
        ObjectDisposedException.ThrowIf(_stream is null, this);
        if (HasError) return 0;
        bool completed = false;
        try
        {
            int total = 0;
            while (!destination.IsEmpty && (_detectedSamples == 0 || SamplePosition < _detectedSamples))
            {
                _cancellation.ThrowIfCancellationRequested();
                if (_sampleConsumed == _sampleCount && !DecodeNext()) break;
                int count = Math.Min(destination.Length, _sampleCount - _sampleConsumed);
                if (_detectedSamples != 0) count = (int)Math.Min((ulong)count, _detectedSamples - SamplePosition);
                _samples.AsSpan(_sampleConsumed, count).CopyTo(destination);
                _sampleConsumed += count; total += count; SamplePosition += (uint)count; destination = destination[count..];
            }
            completed = true; return total;
        }
        catch (IOException) { HasError = true; return 0; }
        finally { if (!completed) Dispose(); }
    }
    private void BuildIndex()
    {
        PositionInput(_startOffset); _index.Clear();
        ulong sample = 0;
        bool decodingStarted = false;
        while (FindHeader(out Mp3FrameInfo header, out int nominal))
        {
            _cancellation.ThrowIfCancellationRequested();
            int first = _consumed + header.FrameOffset, frameBytes = header.FrameBytes - header.FrameOffset;
            int mainBytes = header.Layer == 3 ? _decoder.MainDataBytes(_input.AsMemory(first), frameBytes) : 0;
            _index.Add(new(_bufferOffset + first, sample, mainBytes, nominal * header.Channels));
            if (!decodingStarted && _index.Count < 256)
            {
                int decoded = _decoder.Decode(_input.AsMemory(_consumed), _filled - _consumed, _samples, out Mp3FrameInfo info);
                sample += (ulong)(decoded * info.Channels); decodingStarted = decoded != 0;
            }
            else sample += (ulong)(nominal * header.Channels);
            _consumed += header.FrameBytes;
        }
        _indexBuilt = true;
    }
    public void SeekSample(ulong position)
    {
        ObjectDisposedException.ThrowIf(_stream is null, this);
        bool completed = false;
        try
        {
            SamplePosition = position;
            ulong target = checked(position + _startDelay);
            if (target == 0) { PositionInput(_startOffset); _toSkip = 0; }
            else
            {
                if (!_indexBuilt) BuildIndex();
                if (_index.Count == 0) { PositionInput(_startOffset); _toSkip = 0; }
                else
                {
                    int low = 0, high = _index.Count - 1, found = 0;
                    while (low <= high)
                    {
                        int mid = low + (high - low) / 2;
                        if (_index[mid].Sample == target) { found = mid; break; }
                        if (_index[mid].Sample > target) high = mid - 1;
                        else { found = mid; low = mid + 1; }
                    }
                    int index = Math.Max(0, found - 2);
                    if (_layer == 3)
                    {
                        int fill = 511;
                        while (index > 0 && fill > 0)
                        {
                            FrameIndex frame = _index[--index];
                            if (frame.MainDataBytes < 0) break;
                            fill -= Math.Min(fill, frame.MainDataBytes);
                        }
                    }
                    PositionInput(_index[index].Offset); _toSkip = target - _index[index].Sample;
                    while (index + 1 < _index.Count && _index[index].Sample == 0 && _index[index + 1].Sample == 0)
                    { _toSkip += (uint)_index[index].Samples; index++; }
                }
            }
            HasError = false; completed = true;
        }
        catch (IOException) { HasError = true; }
        finally { if (!completed) Dispose(); }
    }
    public void Dispose()
    {
        Stream? stream = _stream;
        try { if (!_leaveOpen) stream?.Dispose(); }
        finally { _stream = null; _filled = _consumed = _sampleCount = _sampleConsumed = 0; _index.Clear(); }
    }
}
