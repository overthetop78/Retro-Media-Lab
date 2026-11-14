using System.IO;

namespace RetroMediaLab.MediaEngine.Images.Formats.Common.Audio.Vorbis;

internal readonly record struct VorbisMode(int BlockFlag, int Mapping);

internal sealed class VorbisSetup
{
    public int Channels { get; private set; }
    public int SampleRate { get; private set; }
    public int[] BlockSizes { get; } = new int[2];
    public VorbisCodebook[] Books { get; private set; } = [];
    public VorbisFloor[] Floors { get; private set; } = [];
    public VorbisResidue[] Residues { get; private set; } = [];
    public VorbisMapping[] Mappings { get; private set; } = [];
    public VorbisMode[] Modes { get; private set; } = [];
    public int ModeBits => VorbisCodebook.ILog((uint)(Modes.Length - 1));
    private bool _commentsRead;
    private static VorbisBits Header(ReadOnlyMemory<byte> packet, byte type)
    {
        if (packet.Length < 7 || packet.Span[0] != type || !packet.Span.Slice(1, 6).SequenceEqual("vorbis"u8))
            throw new InvalidDataException("Invalid Vorbis header packet.");
        VorbisBits bits = new(); bits.Reset(packet[7..]); return bits;
    }
    public static VorbisSetup ReadIdentification(OggPacketInfo packet)
    {
        if (!packet.Beginning) throw new InvalidDataException("Vorbis identification is not at the beginning of its logical stream.");
        VorbisBits bits = Header(packet.Data, 1);
        if (bits.Read(32) != 0) throw new InvalidDataException("Unsupported Vorbis version.");
        VorbisSetup setup = new() { Channels = bits.Read(8), SampleRate = bits.Read(32) };
        bits.Read(32); bits.Read(32); bits.Read(32); // upper, nominal and lower bitrate hints
        int shortBits = bits.Read(4), longBits = bits.Read(4);
        setup.BlockSizes[0] = 1 << shortBits; setup.BlockSizes[1] = 1 << longBits;
        if (setup.SampleRate < 1 || setup.Channels < 1 || shortBits < 0 || longBits < 0 ||
            setup.BlockSizes[0] < 64 || setup.BlockSizes[1] < setup.BlockSizes[0] || setup.BlockSizes[1] > 8192 || bits.Read(1) != 1)
            throw new InvalidDataException("Invalid Vorbis identification parameters.");
        return setup;
    }
    private static void SkipBytes(VorbisBits bits, int count)
    {
        if (count < 0 || (long)count * 8 > bits.BitsRemaining) throw new InvalidDataException("Truncated Vorbis comment field.");
        while (count > 0)
        {
            int chunk = Math.Min(count, int.MaxValue / 8);
            bits.Advance(chunk * 8); count -= chunk;
        }
    }
    public void ReadComments(ReadOnlyMemory<byte> packet)
    {
        VorbisBits bits = Header(packet, 3);
        SkipBytes(bits, bits.Read(32));
        int comments = bits.Read(32);
        if (comments < 0 || comments > bits.BitsRemaining / 32) throw new InvalidDataException("Invalid Vorbis comment count.");
        for (int i = 0; i < comments; i++) SkipBytes(bits, bits.Read(32));
        if (bits.Read(1) != 1) throw new InvalidDataException("Missing Vorbis comment framing bit.");
        _commentsRead = true;
    }
    public void ReadSetup(ReadOnlyMemory<byte> packet)
    {
        if (!_commentsRead) throw new InvalidDataException("Vorbis comments must precede setup.");
        VorbisBits bits = Header(packet, 5);
        int bookCount = bits.Read(8) + 1;
        if (bookCount < 1) throw new InvalidDataException("Truncated Vorbis codebook count.");
        Books = new VorbisCodebook[bookCount];
        for (int i = 0; i < bookCount; i++) Books[i] = VorbisCodebook.Unpack(bits);
        int times = bits.Read(6) + 1;
        if (times < 1) throw new InvalidDataException("Truncated Vorbis time backend count.");
        for (int i = 0; i < times; i++) if (bits.Read(16) != 0) throw new InvalidDataException("Unsupported Vorbis time backend.");
        int floors = bits.Read(6) + 1;
        if (floors < 1) throw new InvalidDataException("Truncated Vorbis floor count.");
        Floors = new VorbisFloor[floors];
        for (int i = 0; i < floors; i++) Floors[i] = VorbisFloor.Unpack(bits, Books, BlockSizes);
        int residues = bits.Read(6) + 1;
        if (residues < 1) throw new InvalidDataException("Truncated Vorbis residue count.");
        Residues = new VorbisResidue[residues];
        for (int i = 0; i < residues; i++) Residues[i] = VorbisResidue.Unpack(bits, Books, Channels, BlockSizes[1]);
        int mappings = bits.Read(6) + 1;
        if (mappings < 1) throw new InvalidDataException("Truncated Vorbis mapping count.");
        Mappings = new VorbisMapping[mappings];
        for (int i = 0; i < mappings; i++) Mappings[i] = VorbisMapping.Unpack(bits, this, times);
        int modes = bits.Read(6) + 1;
        if (modes < 1) throw new InvalidDataException("Truncated Vorbis mode count.");
        Modes = new VorbisMode[modes];
        for (int i = 0; i < modes; i++)
        {
            int block = bits.Read(1), window = bits.Read(16), transform = bits.Read(16), mapping = bits.Read(8);
            if (block < 0 || window != 0 || transform != 0 || (uint)mapping >= mappings) throw new InvalidDataException("Invalid Vorbis mode.");
            Modes[i] = new(block, mapping);
        }
        if (bits.Read(1) != 1) throw new InvalidDataException("Missing Vorbis setup framing bit.");
    }
}
