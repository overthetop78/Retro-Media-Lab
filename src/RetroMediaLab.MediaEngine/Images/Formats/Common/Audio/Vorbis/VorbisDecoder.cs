using System.IO;

namespace RetroMediaLab.MediaEngine.Images.Formats.Common.Audio.Vorbis;

internal sealed class VorbisDecoder : IDisposable
{
    private Stream? _stream;
    private OggPacketReader? _reader;
    private VorbisSynthesis? _synthesis;
    private readonly bool _leaveOpen;
    private readonly CancellationToken _cancellation;
    private readonly List<Link> _links = [];
    private readonly VorbisBits _scanBits = new();
    private int _currentLink;
    private bool _linkEnded, _ended, _seeking, _positionKnown = true;
    private OggPacketInfo? _pendingPacket;
    private sealed class Link
    {
        internal required VorbisSetup Setup;
        internal int Serial;
        internal long HeaderOffset, DataOffset = -1, StartGranule, LastGranule = -1, FirstSample, Frames;
        internal readonly List<Anchor> Anchors = [];
    }
    private readonly record struct Anchor(long Offset, long Granule, bool End);
    public int Channels => _links[_currentLink].Setup.Channels;
    public int SampleRate => _links[_currentLink].Setup.SampleRate;
    public long TotalFrames { get; private set; }
    public long PcmPosition { get; private set; }
    private VorbisDecoder(Stream stream, bool leaveOpen, CancellationToken cancellation)
    {
        _stream = stream; _leaveOpen = leaveOpen; _cancellation = cancellation;
        _reader = new(stream, cancellation);
    }
    public static VorbisDecoder Open(Stream stream, bool leaveOpen, CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(stream);
        VorbisDecoder decoder = new(stream, leaveOpen, cancellation);
        bool opened = false;
        try
        {
            decoder.ReadLinks(); decoder.SelectLink(0, decoder._links[0].DataOffset);
            opened = true; return decoder;
        }
        catch (OverflowException error) { throw new InvalidDataException("Vorbis setup dimensions exceed the format's usable storage.", error); }
        finally { if (!opened) decoder.Dispose(); }
    }
    private static bool IsIdentification(OggPacketInfo packet) => packet.Beginning && packet.Data.Length >= 7 &&
        packet.Data.Span[0] == 1 && packet.Data.Span.Slice(1, 6).SequenceEqual("vorbis"u8);
    private OggPacketInfo NextHeader(int serial)
    {
        if (!_reader!.TryRead(out OggPacketInfo packet) || packet.Serial != serial || packet.HoleBefore)
            throw new InvalidDataException("Missing or damaged Vorbis header packet.");
        return packet;
    }
    private int PacketBlockSize(VorbisSetup setup, ReadOnlyMemory<byte> packet)
    {
        _scanBits.Reset(packet);
        if (_scanBits.Read(1) != 0) return -1;
        int mode = _scanBits.Read(setup.ModeBits);
        return (uint)mode < setup.Modes.Length ? setup.BlockSizes[setup.Modes[mode].BlockFlag] : -1;
    }
    private void ReadLinks()
    {
        OggPacketReader reader = _reader!; reader.Seek(0);
        Link? link = null;
        int lastBlock = -1;
        long accumulated = 0;
        bool initialGranule = false;
        while (reader.TryRead(out OggPacketInfo packet))
        {
            _cancellation.ThrowIfCancellationRequested();
            if (link is null)
            {
                if (!IsIdentification(packet))
                {
                    if (_links.Count == 0) throw new InvalidDataException("The Ogg stream is not Vorbis.");
                    continue;
                }
                VorbisSetup setup = VorbisSetup.ReadIdentification(packet);
                reader.SerialFilter = packet.Serial;
                setup.ReadComments(NextHeader(packet.Serial).Data);
                OggPacketInfo setupPacket = NextHeader(packet.Serial);
                setup.ReadSetup(setupPacket.Data);
                link = new() { Setup = setup, Serial = packet.Serial, HeaderOffset = packet.PageOffset };
                _links.Add(link); lastBlock = -1; accumulated = 0; initialGranule = false;
                continue;
            }
            int block = PacketBlockSize(link.Setup, packet.Data);
            if (block >= 0)
            {
                if (link.DataOffset < 0) link.DataOffset = packet.PageOffset;
                if (lastBlock != -1) accumulated += (lastBlock + block) >> 2;
                lastBlock = block;
                if (packet.Granule != -1)
                {
                    if (!initialGranule)
                    { link.StartGranule = Math.Max(0, packet.Granule - accumulated); initialGranule = true; }
                    link.LastGranule = packet.Granule;
                    link.Anchors.Add(new(packet.PageOffset, packet.Granule, packet.End));
                }
            }
            if (packet.End) { link = null; reader.SerialFilter = null; }
        }
        if (_links.Count == 0) throw new InvalidDataException("No Vorbis logical stream was found.");
        long total = 0;
        foreach (Link item in _links)
        {
            if (item.DataOffset < 0) item.DataOffset = item.HeaderOffset;
            item.FirstSample = total;
            item.Frames = Math.Max(0, item.LastGranule - item.StartGranule);
            total = checked(total + item.Frames);
        }
        TotalFrames = total;
    }
    private void SelectLink(int index, long offset)
    {
        _cancellation.ThrowIfCancellationRequested();
        Link link = _links[index];
        _reader!.Seek(offset, link.Serial);
        if (_synthesis is not null && _currentLink == index) _synthesis.Restart();
        else _synthesis = new(link.Setup);
        _currentLink = index;
        _pendingPacket = null; _linkEnded = _ended = false;
    }
    private int Pump()
    {
        if (_ended) return 0;
        while (_synthesis!.Available == 0)
        {
            _cancellation.ThrowIfCancellationRequested();
            if (_linkEnded)
            {
                if (_currentLink + 1 == _links.Count) { _ended = true; return 0; }
                int next = _currentLink + 1; SelectLink(next, _links[next].DataOffset);
                PcmPosition = _links[next].FirstSample; _positionKnown = true;
            }
            OggPacketInfo packet;
            if (_pendingPacket.HasValue) { packet = _pendingPacket.Value; _pendingPacket = null; }
            else if (!_reader!.TryRead(out packet)) { _linkEnded = true; continue; }
            if (packet.HoleBefore)
            { _pendingPacket = packet with { HoleBefore = false }; return -3; } // OV_HOLE
            if (packet.Serial != _links[_currentLink].Serial) continue;
            if (packet.End) _linkEnded = true;
            if (!_synthesis!.DecodePacket(packet)) continue;
            if (packet.Granule != -1 && !packet.End)
            {
                Link link = _links[_currentLink];
                long granule = packet.Granule - (_currentLink > 0 || _seeking ? link.StartGranule : 0);
                PcmPosition = Math.Max(0, granule) - _synthesis.Available + link.FirstSample;
                _positionKnown = true;
            }
            if (!_positionKnown && _synthesis.Available != 0) _synthesis.Discard(_synthesis.Available);
        }
        return 1;
    }
    public int ReadPcm(Span<byte> destination)
    {
        ObjectDisposedException.ThrowIf(_stream is null, this);
        bool completed = false;
        try
        {
            int result = Pump();
            if (result <= 0) { completed = true; return result; }
            int count = _synthesis!.ReadPcm(destination);
            PcmPosition += count / (Channels * sizeof(short));
            completed = true; return count;
        }
        finally { if (!completed) Dispose(); }
    }
    public void SeekPcm(long position)
    {
        ObjectDisposedException.ThrowIf(_stream is null, this);
        if (position < 0 || position > TotalFrames) return;
        bool completed = false;
        try
        {
            _seeking = true;
            if (_positionKnown && position >= PcmPosition && position - PcmPosition <= _synthesis!.Available)
            { _synthesis.Discard((int)(position - PcmPosition)); PcmPosition = position; completed = true; return; }
            int linkIndex = _links.Count - 1;
            for (int i = 0; i < _links.Count; i++)
                if (position < _links[i].FirstSample + _links[i].Frames) { linkIndex = i; break; }
            Link link = _links[linkIndex];
            if (position == TotalFrames)
            {
                SelectLink(linkIndex, _stream!.Length); _ended = true;
                PcmPosition = position; _positionKnown = true; completed = true; return;
            }
            long targetGranule = position - link.FirstSample + link.StartGranule;
            int low = 0, high = link.Anchors.Count - 1, before = -1;
            while (low <= high)
            {
                int mid = low + ((high - low) >> 1);
                if (link.Anchors[mid].Granule < targetGranule) { before = mid; low = mid + 1; }
                else high = mid - 1;
            }
            int anchor = before - 2;
            long offset = anchor < 0 ? link.DataOffset : link.Anchors[anchor].Offset;
            SelectLink(linkIndex, offset); _positionKnown = anchor < 0; PcmPosition = link.FirstSample;
            bool restarted = false;
            while (!_ended)
            {
                int result = Pump();
                if (result < 0) continue;
                if (result == 0) { PcmPosition = TotalFrames; break; }
                if (PcmPosition > position)
                {
                    // A damaged granule sequence cannot provide a safe anchor.
                    // Restart this link and consume its exact PCM position.
                    if (restarted) throw new InvalidDataException("Vorbis granule sequence cannot reach the requested PCM position.");
                    restarted = true;
                    SelectLink(linkIndex, link.DataOffset); PcmPosition = link.FirstSample; _positionKnown = true; continue;
                }
                long remaining = position - PcmPosition;
                int skip = (int)Math.Min(remaining, _synthesis!.Available);
                _synthesis.Discard(skip); PcmPosition += skip;
                if (PcmPosition == position) break;
            }
            completed = true;
        }
        finally { _seeking = false; if (!completed) Dispose(); }
    }
    public void Dispose()
    {
        Stream? stream = _stream;
        try { if (!_leaveOpen) stream?.Dispose(); }
        finally { _stream = null; _reader = null; _synthesis = null; _pendingPacket = null; _links.Clear(); _ended = true; }
    }
}
