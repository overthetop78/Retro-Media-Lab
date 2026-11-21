// Copyright 2004-2005 Theo Berkau
// Copyright 2005 Joost Peters
// Copyright 2006 Guillaume Duhamel
// Translated from Yabause cdbase.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Text;

namespace RetroMediaLab.Chips.Controllers.Sega.CD;

public sealed class SaturnCdImageDrive
{
    private enum ImageType { None, Iso, BinCue, Mds, Ccd, Nrg }

    private struct Track
    {
        public byte ControlAddress;
        public uint StartFad;
        public uint EndFad;
        public uint FileOffset;
        public uint SectorSize;
        public FileStream? File;
        public FileStream? SubchannelFile;
        public int FileSize;
        public int FileId;
        public bool InterleavedSubchannel;
        public uint[]? CueIndexes;
        public uint CuePregap;
        public uint CuePostgap;
        public uint CueFirstFad;
        public uint CueFirstStoredFad;
        public uint CueEndStoredFad;
        public byte CueDataMode;
    }

    private sealed record Session(uint StartFad, uint EndFad, Track[] Tracks);

    private readonly uint[] _toc = new uint[102];
    private readonly SaturnCdInterface.Toc10Entry[] _toc10 = new SaturnCdInterface.Toc10Entry[102];
    private int _toc10Count;
    private uint _ccdImageEndFad;
    private byte _cueDiscType;
    private Session[] _sessions = [];
    private ImageType _imageType = ImageType.Iso;

    public SaturnCdImageDrive()
    {
        IsoCd = new SaturnCdInterface(
            SaturnCdInterface.Iso,
            "ISO-File Virtual Drive",
            IsoInitialize,
            IsoDeinitialize,
            IsoGetStatus,
            IsoReadToc,
            IsoReadToc10,
            IsoReadSectorFad,
            IsoReadAheadFad)
        {
            ReadSubchannelFad = IsoReadSubchannelFad,
        };
    }

    public SaturnCdInterface IsoCd { get; }

    public static SaturnCdInterface DummyCd { get; } = new(
        SaturnCdInterface.Dummy,
        "Dummy CD Drive",
        DummyInitialize,
        DummyDeinitialize,
        DummyGetStatus,
        DummyReadToc,
        DummyReadToc10,
        DummyReadSectorFad,
        DummyReadAheadFad);

    private static int DummyInitialize(string? path) => 0;

    private static void DummyDeinitialize()
    {
    }

    private static int DummyGetStatus() => 2;

    private static int DummyReadToc(Span<uint> toc) => 0;

    private static int DummyReadToc10(Span<SaturnCdInterface.Toc10Entry> toc) => 0;

    private static int DummyReadSectorFad(uint fad, Span<byte> buffer)
    {
        buffer[..2352].Clear();
        return 1;
    }

    private static void DummyReadAheadFad(uint fad)
    {
    }

    private int LoadBinCue(string cuePath, FileStream cueFile)
    {
        cueFile.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(cueFile, Encoding.Latin1, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var tracks = new Track[99];
        var files = new List<FileStream>();
        int trackCount = 0;
        int fileId = -1;
        bool loaded = false;
        try
        {
            while (reader.ReadLine() is string line)
            {
                string[] parts = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                switch (parts[0].ToUpperInvariant())
                {
                    case "FILE":
                    {
                        int start = line.IndexOf('"');
                        int end = start < 0 ? -1 : line.IndexOf('"', start + 1);
                        if (end <= start + 1 || !line[(end + 1)..].Trim().Equals("BINARY", StringComparison.OrdinalIgnoreCase))
                            return -1;

                        string imagePath = line[(start + 1)..end];
                        FileStream? file = OpenImage(Path.Combine(Path.GetDirectoryName(cuePath) ?? string.Empty, imagePath))
                            ?? OpenImage(imagePath);
                        if (file is null)
                            return -1;

                        fileId = files.Count;
                        files.Add(file);
                        break;
                    }
                    case "TRACK":
                    {
                        if (fileId < 0 || parts.Length < 3
                            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)
                            || number != trackCount + 1 || number > tracks.Length)
                            return -1;

                        ref Track track = ref tracks[trackCount++];
                        track.FileId = fileId;
                        track.File = files[fileId];
                        track.CueIndexes = new uint[100];
                        Array.Fill(track.CueIndexes, uint.MaxValue);
                        switch (parts[2].ToUpperInvariant())
                        {
                            case "AUDIO": track.SectorSize = 2352; track.ControlAddress = 1; break;
                            case "MODE1/2048": track.SectorSize = 2048; track.CueDataMode = 1; break;
                            case "MODE1/2352": track.SectorSize = 2352; track.CueDataMode = 1; break;
                            case "MODE1/2448": track.SectorSize = 2448; track.CueDataMode = 1; break;
                            case "MODE2/2336": track.SectorSize = 2336; track.CueDataMode = 2; break;
                            case "MODE2/2352": track.SectorSize = 2352; track.CueDataMode = 2; break;
                            case "MODE2/2448": track.SectorSize = 2448; track.CueDataMode = 2; break;
                            case "CDI/2352": track.SectorSize = 2352; track.CueDataMode = 2; _cueDiscType = 0x10; break;
                            default: return -1;
                        }
                        if (track.CueDataMode != 0)
                            track.ControlAddress = 0x41;
                        if (track.CueDataMode == 2 && _cueDiscType != 0x10)
                            _cueDiscType = 0x20;
                        break;
                    }
                    case "INDEX":
                        if (trackCount == 0 || parts.Length < 3
                            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)
                            || index is < 0 or > 99 || !TryReadCueTime(parts[2], out uint indexFad))
                            return -1;

                        tracks[trackCount - 1].CueIndexes![index] = indexFad;
                        break;
                    case "PREGAP":
                    case "POSTGAP":
                        if (trackCount == 0 || parts.Length < 2 || !TryReadCueTime(parts[1], out uint gap))
                            return -1;
                        if (parts[0].Equals("PREGAP", StringComparison.OrdinalIgnoreCase))
                            tracks[trackCount - 1].CuePregap = gap;
                        else
                            tracks[trackCount - 1].CuePostgap = gap;
                        break;
                    case "FLAGS":
                    {
                        if (trackCount == 0)
                            return -1;

                        ref Track track = ref tracks[trackCount - 1];
                        track.ControlAddress &= 0x4F;
                        for (int i = 1; i < parts.Length; i++)
                        {
                            switch (parts[i])
                            {
                                case "DCP": track.ControlAddress |= 0x20; break;
                                case "4CH": track.ControlAddress |= 0x80; break;
                                case "PRE": track.ControlAddress |= 0x10; break;
                                case "SCMS": break;
                                default: return -1;
                            }
                        }

                        break;
                    }
                }
            }

            if (trackCount == 0 || files.Count != tracks[trackCount - 1].FileId + 1)
                return -1;

            uint runningFad = 0;
            long fileOffset = 0;
            tracks[0].CuePregap += 150;
            for (int i = 0; i < trackCount; i++)
            {
                ref Track track = ref tracks[i];
                uint[] indexes = track.CueIndexes!;
                if (indexes[1] == uint.MaxValue || (indexes[0] != uint.MaxValue && indexes[0] > indexes[1]))
                    return -1;

                int previousFile = i == 0 ? -1 : tracks[i - 1].FileId;
                if (track.FileId != previousFile)
                {
                    if (track.FileId != previousFile + 1)
                        return -1;
                    fileOffset = 0;
                }

                uint storedPregap = indexes[0] == uint.MaxValue ? 0 : indexes[1] - indexes[0];
                track.CueFirstFad = runningFad;
                track.CueFirstStoredFad = runningFad + track.CuePregap;
                track.StartFad = track.CueFirstStoredFad + storedPregap;
                fileOffset += (long)storedPregap * track.SectorSize;
                long fileLength = track.File!.Length;
                if (fileOffset > uint.MaxValue || fileOffset > fileLength)
                    return -1;
                track.FileOffset = (uint)fileOffset;

                long sectors;
                if (i + 1 == trackCount || tracks[i + 1].FileId != track.FileId)
                    sectors = (fileLength - fileOffset) / track.SectorSize;
                else
                {
                    uint[] nextIndexes = tracks[i + 1].CueIndexes!;
                    uint nextStart = nextIndexes[0] != uint.MaxValue ? nextIndexes[0] : nextIndexes[1];
                    if (nextStart == uint.MaxValue || nextStart < indexes[1])
                        return -1;
                    sectors = nextStart - indexes[1];
                }

                if (sectors < 0 || fileOffset + sectors * track.SectorSize > fileLength
                    || (ulong)track.StartFad + (ulong)sectors + track.CuePostgap > 0x7FFFFF)
                    return -1;

                track.CueEndStoredFad = track.StartFad + (uint)sectors;
                runningFad = track.CueEndStoredFad + track.CuePostgap;
                if (runningFad <= track.CueFirstFad)
                    return -1;
                track.EndFad = runningFad - 1;
                fileOffset += sectors * track.SectorSize;
            }

            _sessions = [new Session(150, runningFad, tracks[..trackCount])];
            loaded = true;
            return 0;
        }
        finally
        {
            if (!loaded)
                foreach (FileStream file in files)
                    file.Dispose();
        }
    }

    private static bool TryReadCueTime(string token, out uint fad)
    {
        fad = 0;
        string[] parts = token.Split(':');
        if (parts.Length != 3 ||
            !uint.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint minute) ||
            !uint.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint second) ||
            !uint.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint frame) ||
            minute > 99 || second > 59 || frame > 74)
            return false;

        fad = (minute * 60 + second) * 75 + frame;
        return true;
    }

    private int LoadMds(string path, FileStream descriptor)
    {
        Span<byte> header = stackalloc byte[92];
        if (!ReadDescriptor(descriptor, 0, header) || !header[..16].SequenceEqual("MEDIA DESCRIPTOR"u8) ||
            header[16] > 1 || (BinaryPrimitives.ReadUInt16LittleEndian(header[18..]) & 0x10) != 0)
            return -1;

        int sessionCount = BinaryPrimitives.ReadUInt16LittleEndian(header[20..]);
        if (sessionCount == 0)
            return -1;

        uint sessionsOffset = BinaryPrimitives.ReadUInt32LittleEndian(header[80..]);
        var sessions = new Session[sessionCount];
        var opened = new List<FileStream>();
        bool loaded = false;
        try
        {
            Span<byte> session = stackalloc byte[24];
            for (int i = 0; i < sessionCount; i++)
            {
                if (!ReadDescriptor(descriptor, sessionsOffset + i * 24L, session) ||
                    LoadMdsTracks(path, descriptor, session, opened) is not Session decoded)
                    return -1;

                sessions[i] = decoded;
            }

            _sessions = sessions;
            loaded = true;
            return 0;
        }
        finally
        {
            if (!loaded)
                foreach (FileStream file in opened)
                    file.Dispose();
        }
    }

    private static Session? LoadMdsTracks(string path, FileStream descriptor, ReadOnlySpan<byte> session, List<FileStream> opened)
    {
        int totalBlocks = session[10];
        int lastTrack = BinaryPrimitives.ReadUInt16LittleEndian(session[14..]);
        uint tracksOffset = BinaryPrimitives.ReadUInt32LittleEndian(session[20..]);
        var tracks = new Track[lastTrack];
        uint endFad = 0;
        int trackCount = 0;
        Span<byte> track = stackalloc byte[80];
        Span<byte> footer = stackalloc byte[16];

        for (int i = 0; i < totalBlocks; i++)
        {
            if (!ReadDescriptor(descriptor, tracksOffset + i * 80L, track))
                return null;

            if (track[4] == 0xA2)
                endFad = (uint)((track[9] * 60 + track[10]) * 75 + track[11]);
            if (BinaryPrimitives.ReadUInt32LittleEndian(track[12..]) == 0)
                continue;
            if (trackCount >= tracks.Length)
                return null;

            uint footerOffset = BinaryPrimitives.ReadUInt32LittleEndian(track[52..]);
            FileStream? image = null;
            int fileSize = 0;
            if (footerOffset != 0)
            {
                int previous = 0;
                for (; previous < trackCount; previous++)
                {
                    if (unchecked((uint)tracks[previous].FileId) == footerOffset)
                        break;
                }

                if (previous < trackCount)
                {
                    image = tracks[previous].File;
                    fileSize = tracks[previous].FileSize;
                }
                else
                {
                    if (!ReadDescriptor(descriptor, footerOffset, footer))
                        return null;
                    uint filenameOffset = BinaryPrimitives.ReadUInt32LittleEndian(footer);
                    bool wide = BinaryPrimitives.ReadUInt32LittleEndian(footer[4..]) != 0;
                    string? imagePath = ReadMdsFilename(descriptor, filenameOffset, wide);
                    if (imagePath is null)
                        return null;

                    if (imagePath.StartsWith("*.", StringComparison.Ordinal))
                        imagePath = Path.ChangeExtension(path, imagePath[1..]);

                    image = OpenImage(imagePath);
                    if (image is null)
                        return null;
                    opened.Add(image);
                    fileSize = unchecked((int)image.Seek(0, SeekOrigin.End));
                    image.Seek(0, SeekOrigin.Begin);
                }
            }

            ref Track current = ref tracks[trackCount];
            current.ControlAddress = unchecked((byte)((track[2] << 4) | (track[2] >> 4)));
            current.StartFad = unchecked(BinaryPrimitives.ReadUInt32LittleEndian(track[36..]) + 150);
            if (trackCount > 0)
                tracks[trackCount - 1].EndFad = current.StartFad;
            current.FileOffset = unchecked((uint)BinaryPrimitives.ReadUInt64LittleEndian(track[40..]));
            current.SectorSize = BinaryPrimitives.ReadUInt16LittleEndian(track[16..]);
            current.File = image;
            current.FileSize = fileSize;
            current.FileId = unchecked((int)footerOffset);
            current.InterleavedSubchannel = track[1] != 0;
            trackCount++;
        }

        if (trackCount == 0)
            return null;

        tracks[trackCount - 1].EndFad = endFad;
        return new Session(tracks[0].StartFad, endFad, tracks[..trackCount]);
    }

    private static bool ReadDescriptor(FileStream descriptor, long offset, Span<byte> value)
    {
        descriptor.Seek(offset, SeekOrigin.Begin);
        return descriptor.ReadAtLeast(value, value.Length, throwOnEndOfStream: false) == value.Length;
    }

    private static string? ReadMdsFilename(FileStream descriptor, uint offset, bool wide)
    {
        descriptor.Seek(offset, SeekOrigin.Begin);
        Span<byte> buffer = stackalloc byte[1024];
        Span<byte> name = wide ? buffer : buffer[..512];
        int length = descriptor.ReadAtLeast(name, name.Length, throwOnEndOfStream: false);
        if (length == 0)
            return null;

        string text = wide ? Encoding.Unicode.GetString(name[..(length & ~1)]) : Encoding.Latin1.GetString(name[..length]);
        int zero = text.IndexOf('\0');
        return zero == -1 ? text : text[..zero];
    }

    private readonly record struct CcdEntry(string Section, string Name, string Value);

    private static int LoadParseCcd(FileStream descriptor, List<CcdEntry> dictionary)
    {
        using var reader = new StreamReader(descriptor, Encoding.Latin1, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        string section = string.Empty;
        int lineNumber = 0;
        while (reader.ReadLine() is string rawLine)
        {
            lineNumber++;
            string line = rawLine.Trim();
            if (line.StartsWith('['))
            {
                int closing = line.IndexOf(']', 1);
                if (closing < 0)
                    return lineNumber;
                section = line[1..closing];
            }
            else if (line.Length != 0)
            {
                int separator = line.IndexOf('=');
                if (separator < 0)
                    return lineNumber;
                dictionary.Add(new CcdEntry(section, line[..separator].Trim(), line[(separator + 1)..].Trim()));
            }
        }

        return 0;
    }

    private static int GetIntCcd(List<CcdEntry> dictionary, string section, string name)
    {
        foreach (CcdEntry entry in dictionary)
        {
            if (!entry.Section.Equals(section, StringComparison.OrdinalIgnoreCase) ||
                !entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                continue;

            ReadOnlySpan<char> text = entry.Value;
            bool negative = !text.IsEmpty && text[0] == '-';
            if (!text.IsEmpty && text[0] is '-' or '+')
                text = text[1..];

            int radix = 10;
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                radix = 16;
                text = text[2..];
            }
            else if (!text.IsEmpty && text[0] == '0')
            {
                radix = 8;
            }

            int value = 0;
            foreach (char character in text)
            {
                int digit = character switch
                {
                    >= '0' and <= '9' => character - '0',
                    >= 'a' and <= 'f' => character - 'a' + 10,
                    >= 'A' and <= 'F' => character - 'A' + 10,
                    _ => -1,
                };
                if (digit < 0 || digit >= radix)
                    break;
                value = unchecked(value * radix + digit);
            }
            return negative ? unchecked(-value) : value;
        }

        return -1;
    }

    private int LoadCcd(string path, FileStream descriptor)
    {
        if (path.Length >= 512)
            return -1;
        FileStream? image = OpenImage(Path.ChangeExtension(path, ".img"));
        if (image is null)
            return -1;

        FileStream? subchannel = null;
        bool loaded = false;
        try
        {
            subchannel = OpenImage(Path.ChangeExtension(path, ".sub"));
            descriptor.Seek(0, SeekOrigin.Begin);
            var dictionary = new List<CcdEntry>(100);
            if (LoadParseCcd(descriptor, dictionary) != 0)
                return -1;

            int tocEntries = GetIntCcd(dictionary, "DISC", "TocEntries");
            if (GetIntCcd(dictionary, "DISC", "Sessions") != 1 ||
                GetIntCcd(dictionary, "DISC", "DataTracksScrambled") != 0 ||
                tocEntries is < 0 or > 102)
                return -1;

            _toc10Count = tocEntries;
            Track[]? tracks = null;
            int firstTrack = 0;
            for (int i = 0; i < tocEntries; i++)
            {
                string section = "Entry " + i.ToString(CultureInfo.InvariantCulture);
                int point = GetIntCcd(dictionary, section, "Point");
                _toc10[i] = new SaturnCdInterface.Toc10Entry
                {
                    ControlAddress = unchecked((byte)((GetIntCcd(dictionary, section, "Control") << 4) |
                        GetIntCcd(dictionary, section, "ADR"))),
                    TrackNumber = unchecked((byte)GetIntCcd(dictionary, section, "TrackNo")),
                    Point = unchecked((byte)point),
                    Minute = unchecked((byte)GetIntCcd(dictionary, section, "AMin")),
                    Second = 2,
                    Zero = unchecked((byte)GetIntCcd(dictionary, section, "Zero")),
                    PointMinute = unchecked((byte)GetIntCcd(dictionary, section, "PMin")),
                    PointSecond = unchecked((byte)GetIntCcd(dictionary, section, "PSec")),
                    PointFrame = unchecked((byte)GetIntCcd(dictionary, section, "PFrame")),
                };

                if (point == 0xA0)
                {
                    firstTrack = GetIntCcd(dictionary, section, "PMin");
                    _toc[99] = ((uint)_toc10[i].ControlAddress << 24)
                        | ((uint)_toc10[i].PointMinute << 16) | ((uint)_toc10[i].PointSecond << 8);
                }
                else if (point == 0xA1)
                {
                    int count = GetIntCcd(dictionary, section, "PMin");
                    if (GetIntCcd(dictionary, section, "Session") != 1 || count is < 1 or > 99)
                        return -1;
                    tracks = new Track[count];
                    _toc[100] = ((uint)_toc10[i].ControlAddress << 24) | ((uint)count << 16);
                }
            }

            if (tracks is null || firstTrack < 1 || firstTrack > tracks.Length)
                return -1;

            uint endFad = 0;
            for (int i = 0; i < tocEntries; i++)
            {
                string section = "Entry " + i.ToString(CultureInfo.InvariantCulture);
                int point = GetIntCcd(dictionary, section, "Point");
                if (point is >= 1 and <= 99)
                {
                    int lba = GetIntCcd(dictionary, section, "PLBA");
                    if (GetIntCcd(dictionary, section, "Session") != 1 || point < firstTrack
                        || point > tracks.Length || lba is < 0 or > 0x7FFF69)
                        return -1;

                    ref Track track = ref tracks[point - 1];
                    track.ControlAddress = unchecked((byte)((GetIntCcd(dictionary, section, "Control") << 4) |
                        GetIntCcd(dictionary, section, "ADR")));
                    track.StartFad = (uint)lba + 150;
                    long offset = (long)lba * 2352;
                    if (offset >= image.Length || offset > uint.MaxValue)
                        return -1;
                    track.FileOffset = (uint)offset;
                    track.SectorSize = 2352;
                    track.File = image;
                    track.SubchannelFile = subchannel;
                    track.FileId = 0;
                    track.InterleavedSubchannel = false;
                    _toc[point - 1] = ((uint)track.ControlAddress << 24) | track.StartFad;
                }
                else if (point == 0xA2)
                {
                    int lba = GetIntCcd(dictionary, section, "PLBA");
                    if (GetIntCcd(dictionary, section, "Session") != 1 || lba is < 0 or > 0x7FFF69)
                        return -1;
                    endFad = (uint)lba + 150;
                    _toc[101] = ((uint)_toc10[i].ControlAddress << 24) | endFad;
                }
            }

            for (int i = firstTrack - 1; i < tracks.Length; i++)
            {
                ref Track track = ref tracks[i];
                uint nextFad = i == tracks.Length - 1 ? endFad : tracks[i + 1].StartFad;
                if (track.File is null || nextFad <= track.StartFad)
                    return -1;
                track.EndFad = nextFad - 1;
            }

            _ccdImageEndFad = checked((uint)(image.Length / 2352 + 150));
            _sessions = [new Session(150, endFad, tracks)];
            loaded = true;
            return 0;
        }
        finally
        {
            if (!loaded)
            {
                try
                {
                    image.Dispose();
                }
                finally
                {
                    subchannel?.Dispose();
                }
            }
        }
    }

    private int LoadIso(FileStream file)
    {
        Track track = new()
        {
            ControlAddress = 0x41,
            StartFad = 150,
            FileOffset = 0,
            File = file,
            FileSize = unchecked((int)file.Seek(0, SeekOrigin.End)),
            FileId = 0,
        };

        if (track.FileSize % 2048 == 0)
            track.SectorSize = 2048;
        else if (track.FileSize % 2352 == 0)
            track.SectorSize = 2352;
        else
            return -1;

        track.EndFad = unchecked(track.StartFad + (uint)track.FileSize / track.SectorSize);
        _sessions = [new Session(150, track.EndFad, [track])];
        return 0;
    }

    private void BuildToc()
    {
        Session session = _sessions[0];
        for (int i = 0; i < session.Tracks.Length; i++)
        {
            Track track = session.Tracks[i];
            _toc[i] = ((uint)track.ControlAddress << 24) | track.StartFad;
        }

        _toc[99] = (_toc[0] & 0xFF000000) | 0x010000 | ((uint)_cueDiscType << 8);
        _toc[100] = (_toc[session.Tracks.Length - 1] & 0xFF000000) | ((uint)session.Tracks.Length << 16);
        _toc[101] = (_toc[session.Tracks.Length - 1] & 0xFF000000) | session.EndFad;
    }

    private void BuildToc10()
    {
        Session session = _sessions[0];
        for (int i = 0; i < session.Tracks.Length; i++)
        {
            ref SaturnCdInterface.Toc10Entry entry = ref _toc10[3 + i];
            entry = new SaturnCdInterface.Toc10Entry
            {
                ControlAddress = session.Tracks[i].ControlAddress,
                Point = (byte)(i + 1),
                Second = 2,
            };
            FadToMsf(session.Tracks[i].StartFad, out entry.PointMinute, out entry.PointSecond, out entry.PointFrame);
        }

        _toc10[0] = new SaturnCdInterface.Toc10Entry
        {
            ControlAddress = _toc10[3].ControlAddress,
            Point = 0xA0,
            Second = 2,
            PointMinute = 1,
            PointSecond = _cueDiscType,
        };
        _toc10[1] = new SaturnCdInterface.Toc10Entry
        {
            ControlAddress = _toc10[3 + session.Tracks.Length - 1].ControlAddress,
            Point = 0xA1,
            Second = 2,
            PointMinute = (byte)session.Tracks.Length,
        };
        _toc10[2] = new SaturnCdInterface.Toc10Entry
        {
            ControlAddress = _toc10[1].ControlAddress,
            Point = 0xA2,
            Second = 2,
        };
        FadToMsf(session.EndFad, out _toc10[2].PointMinute, out _toc10[2].PointSecond, out _toc10[2].PointFrame);
        _toc10Count = 3 + session.Tracks.Length;
    }

    private static void FadToMsf(uint fad, out byte minute, out byte second, out byte frame)
    {
        minute = unchecked((byte)(fad / 4500));
        uint remaining = fad % 4500;
        second = (byte)(remaining / 75);
        frame = (byte)(remaining % 75);
    }

    private int IsoInitialize(string? path)
    {
        Array.Fill(_toc, uint.MaxValue);
        _cueDiscType = 0;
        _sessions = [];

        if (path is null)
            return -1;
        FileStream? file = OpenImage(path);
        if (file is null)
            return -1;

        bool transferred = false;
        bool initialized = false;
        try
        {
            Span<byte> header = stackalloc byte[6];
            int bytesRead = file.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
            string extension = Path.GetExtension(path);
            int result;

            if (extension.Equals(".cue", StringComparison.OrdinalIgnoreCase))
            {
                _imageType = ImageType.BinCue;
                result = LoadBinCue(path, file);
            }
            else if (extension.Equals(".mds", StringComparison.OrdinalIgnoreCase) &&
                bytesRead == 6 && header.SequenceEqual("MEDIA "u8))
            {
                _imageType = ImageType.Mds;
                result = LoadMds(path, file);
            }
            else if (extension.Equals(".ccd", StringComparison.OrdinalIgnoreCase))
            {
                _imageType = ImageType.Ccd;
                result = LoadCcd(path, file);
            }
            else
            {
                _imageType = ImageType.Iso;
                result = LoadIso(file);
                transferred = result == 0;
            }

            if (result != 0)
                return -1;

            if (_imageType != ImageType.Ccd)
            {
                BuildToc();
                BuildToc10();
            }
            initialized = true;
            return 0;
        }
        finally
        {
            try
            {
                if (!initialized)
                {
                    _imageType = ImageType.None;
                    IsoDeinitialize();
                }
            }
            finally
            {
                if (!transferred)
                    file.Dispose();
            }
        }
    }

    private static FileStream? OpenImage(string path)
    {
        if (path.Length == 0)
            return null;

        try
        {
            return File.OpenRead(path);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private void IsoDeinitialize()
    {
        Session[] sessions = _sessions;
        _sessions = [];

        var closed = new HashSet<FileStream>();
        foreach (Session session in sessions)
        {
            foreach (Track track in session.Tracks)
            {
                try
                {
                    if (track.File is { } file && closed.Add(file))
                        file.Dispose();
                }
                finally
                {
                    if (track.SubchannelFile is { } subchannel && closed.Add(subchannel))
                        subchannel.Dispose();
                }
            }
        }
    }

    private int IsoGetStatus() => _sessions.Length > 0 ? 0 : 2;

    private int IsoReadToc(Span<uint> toc)
    {
        _toc.AsSpan().CopyTo(toc);
        return 0xCC * 2;
    }

    private int IsoReadToc10(Span<SaturnCdInterface.Toc10Entry> toc)
    {
        _toc10.AsSpan().CopyTo(toc);
        return _toc10Count;
    }

    private Track FindTrack(uint fad, out int trackNumber)
    {
        Track track = default;
        trackNumber = 0;
        int sessionFirstTrack = 1;
        foreach (Session session in _sessions)
        {
            for (int i = 0; i < session.Tracks.Length; i++)
            {
                Track candidate = session.Tracks[i];
                uint firstFad = candidate.CueIndexes is null ? candidate.StartFad : candidate.CueFirstFad;
                if (candidate.File is not null && fad >= firstFad && fad <= candidate.EndFad)
                {
                    track = candidate;
                    trackNumber = sessionFirstTrack + i;
                    break;
                }
            }
            sessionFirstTrack += session.Tracks.Length;
        }

        return track;
    }

    private int IsoReadSubchannelFad(uint fad, Span<byte> buffer)
    {
        if (_sessions.Length == 0)
            return 0;

        Span<byte> subchannels = buffer[..96];
        uint leadOut = _toc[101] & 0xFFFFFF;
        if (_imageType == ImageType.Ccd && fad < 150)
        {
            WriteCcdPregapSubchannelQ(fad, subchannels);
            return 1;
        }

        if (fad >= (_imageType == ImageType.Ccd ? _ccdImageEndFad : leadOut))
        {
            WriteLeadOutSubchannelQ(fad, leadOut, subchannels);
            return 1;
        }

        Track track = FindTrack(fad, out int trackNumber);
        if (track.File is not { } file)
            return 0;

        if (track.SubchannelFile is { } subchannel && ReadCcdSubchannels(subchannel, fad, subchannels))
            return 1;

        if (track.CueIndexes is not null && (fad < track.CueFirstStoredFad || fad >= track.CueEndStoredFad))
        {
            subchannels.Clear();
            WriteTrackSubchannelQ(track, trackNumber, fad, subchannels);
            return 1;
        }

        if (track.SectorSize == 2448 && !track.InterleavedSubchannel)
        {
            long offset = track.FileOffset + ((long)fad - track.StartFad) * track.SectorSize + 2352;
            file.Seek(offset, SeekOrigin.Begin);
            return file.ReadAtLeast(subchannels, 96, throwOnEndOfStream: false) == 96 ? 1 : 0;
        }

        if (track.SectorSize == 2448)
        {
            Span<byte> raw = stackalloc byte[2448];
            if (IsoReadSectorFad(fad, raw) == 0)
                return 0;
            raw[2352..].CopyTo(subchannels);
        }
        else
        {
            subchannels.Clear();
            WriteTrackSubchannelQ(track, trackNumber, fad, subchannels);
        }

        return 1;
    }

    private int IsoReadSectorFad(uint fad, Span<byte> buffer)
    {
        buffer[..2448].Clear();
        uint leadOut = _toc[101] & 0xFFFFFF;
        if (_sessions.Length != 0 && _imageType == ImageType.Ccd && (fad < 150 || fad >= _ccdImageEndFad))
        {
            Span<byte> subchannels = buffer.Slice(2352, 96);
            if (fad < 150)
                WriteCcdPregapSubchannelQ(fad, subchannels);
            else
                WriteLeadOutSubchannelQ(fad, leadOut, subchannels);

            if ((subchannels[1] & 0x40) != 0)
            {
                int discType = (int)((_toc[99] >> 8) & 0xFF);
                if (discType is 0x10 or 0x20)
                {
                    buffer[18] = 0x20;
                    buffer[22] = 0x20;
                    SaturnCdSectorEncoder.EncodeMode2Form2(fad, buffer);
                }
                else
                {
                    SaturnCdSectorEncoder.EncodeMode1(fad, buffer);
                }
            }

            return 1;
        }

        if (_sessions.Length != 0 && _imageType == ImageType.BinCue && fad >= leadOut && fad <= 449849 + 150)
        {
            Span<byte> subchannels = buffer.Slice(2352, 96);
            WriteLeadOutSubchannelQ(fad, leadOut, subchannels);
            if ((subchannels[1] & 0x40) != 0)
            {
                switch (_sessions[0].Tracks[^1].CueDataMode)
                {
                    case 1:
                        SaturnCdSectorEncoder.EncodeMode1(fad, buffer);
                        break;
                    case 2:
                        buffer[18] = 0x20;
                        buffer[22] = 0x20;
                        SaturnCdSectorEncoder.EncodeMode2Form2(fad, buffer);
                        break;
                }
            }

            return 1;
        }

        Track track = FindTrack(fad, out int trackNumber);
        FileStream? file = track.File;
        if (file is null)
            return 0;

        if (track.CueIndexes is not null && (fad < track.CueFirstStoredFad || fad >= track.CueEndStoredFad))
        {
            byte dataMode = track.CueDataMode;
            if ((long)fad - track.StartFad < -150 && (track.ControlAddress & 0x40) != 0 && trackNumber > 1
                && (_toc[trackNumber - 2] & 0x40000000) == 0)
                dataMode = 0;

            switch (dataMode)
            {
                case 1:
                    SaturnCdSectorEncoder.EncodeMode1(fad, buffer);
                    break;
                case 2:
                    buffer[18] = 0x20;
                    buffer[22] = 0x20;
                    SaturnCdSectorEncoder.EncodeMode2Form2(fad, buffer);
                    break;
            }
            WriteTrackSubchannelQ(track, trackNumber, fad, buffer.Slice(2352, 96));
            return 1;
        }

        file.Seek(track.FileOffset + ((long)fad - track.StartFad) * track.SectorSize, SeekOrigin.Begin);
        if (track.SectorSize == 2448)
        {
            if (!track.InterleavedSubchannel)
            {
                file.ReadAtLeast(buffer[..2448], 2448, throwOnEndOfStream: false);
            }
            else
            {
                ReadOnlySpan<ushort> offsets =
                [
                    0, 66, 125, 191, 100, 50, 150, 175, 8, 33, 58, 83,
                    108, 133, 158, 183, 16, 41, 25, 91, 116, 141, 166, 75,
                    24, 90, 149, 215, 124, 74, 174, 199, 32, 57, 82, 107,
                    132, 157, 182, 207, 40, 65, 49, 115, 140, 165, 190, 99,
                    48, 114, 173, 239, 148, 98, 198, 223, 56, 81, 106, 131,
                    156, 181, 206, 231, 64, 89, 73, 139, 164, 189, 214, 123,
                    72, 138, 197, 263, 172, 122, 222, 247, 80, 105, 130, 155,
                    180, 205, 230, 255, 88, 113, 97, 163, 188, 213, 238, 147,
                ];
                Span<byte> subcode = stackalloc byte[96 * 3];
                subcode.Clear();
                file.ReadAtLeast(buffer[..2352], 2352, throwOnEndOfStream: false);
                file.ReadAtLeast(subcode[..96], 96, throwOnEndOfStream: false);
                file.Seek(2352, SeekOrigin.Current);
                file.ReadAtLeast(subcode.Slice(96, 96), 96, throwOnEndOfStream: false);
                file.Seek(2352, SeekOrigin.Current);
                file.ReadAtLeast(subcode.Slice(192, 96), 96, throwOnEndOfStream: false);
                for (int i = 0; i < 96; i++)
                    buffer[2352 + i] = subcode[offsets[i]];
            }
        }
        else if (track.SectorSize == 2352)
        {
            file.ReadAtLeast(buffer[..2352], 2352, throwOnEndOfStream: false);
        }
        else if (track.SectorSize == 2336)
        {
            file.ReadAtLeast(buffer.Slice(16, 2336), 2336, throwOnEndOfStream: false);
            SaturnCdSectorEncoder.EncodeMode2(fad, buffer);
        }
        else if (track.SectorSize == 2048)
        {
            file.ReadAtLeast(buffer.Slice(0x10, 2048), 2048, throwOnEndOfStream: false);
            SaturnCdSectorEncoder.EncodeMode1(fad, buffer);
        }

        if (track.SectorSize != 2448
            && !(track.SubchannelFile is { } subchannel && ReadCcdSubchannels(subchannel, fad, buffer.Slice(2352, 96))))
            WriteTrackSubchannelQ(track, trackNumber, fad, buffer.Slice(2352, 96));

        return 1;
    }

    private static bool ReadCcdSubchannels(FileStream file, uint fad, Span<byte> destination)
    {
        if (fad < 150)
            return false;

        file.Seek((long)(fad - 150) * 96, SeekOrigin.Begin);
        Span<byte> channels = stackalloc byte[96];
        if (file.ReadAtLeast(channels, 96, throwOnEndOfStream: false) != 96)
            return false;

        for (int offset = 0; offset < 12; offset++)
        {
            for (int bit = 0; bit < 8; bit++)
            {
                int value = 0;
                for (int channel = 0; channel < 8; channel++)
                    value |= ((channels[channel * 12 + offset] >> (7 - bit)) & 1) << (7 - channel);

                destination[offset * 8 + bit] = (byte)value;
            }
        }

        return true;
    }

    private void WriteLeadOutSubchannelQ(uint fad, uint leadOut, Span<byte> subchannels)
    {
        subchannels.Fill(0x80);
        int lastTrack = (int)((_toc[100] >> 16) & 0xFF);
        uint control = (_toc[101] >> 24) | ((_toc[lastTrack - 1] >> 24) & 0x40);
        WriteSubchannelQ((byte)control, 0xAA, unchecked(fad - leadOut), fad, subchannels);
    }

    private void WriteCcdPregapSubchannelQ(uint fad, Span<byte> subchannels)
    {
        uint firstTrack = (_toc[99] >> 16) & 0xFF;
        uint discType = (_toc[99] >> 8) & 0xFF;
        byte control = discType == 0x10 && firstTrack > 1 ? (byte)0x40 : (byte)(_toc[firstTrack - 1] >> 24);
        subchannels.Fill(0x80);
        WriteSubchannelQ(control, ToBcd(firstTrack), 149 - fad, fad, subchannels, index: 0);
    }

    private void WriteTrackSubchannelQ(Track track, int trackNumber, uint fad, Span<byte> subchannels)
    {
        uint relativeFad = fad < track.StartFad ? track.StartFad - 1 - fad : fad - track.StartFad;
        byte controlAddress = track.ControlAddress;
        byte index = 1;
        if (track.CueIndexes is { } indexes)
        {
            if (fad < track.StartFad || fad >= track.CueEndStoredFad)
                subchannels.Fill(0x80);

            if ((long)fad - track.StartFad < -150 && (controlAddress & 0x40) != 0 && trackNumber > 1)
            {
                byte previousControlAddress = (byte)(_toc[trackNumber - 2] >> 24);
                if ((previousControlAddress & 0x40) == 0)
                    controlAddress = previousControlAddress;
            }

            index = 0;
            long fileSector = (long)indexes[1] + fad - track.StartFad;
            for (int i = 1; i < indexes.Length; i++)
            {
                if (fileSector >= indexes[i])
                    index = (byte)i;
            }
        }

        WriteSubchannelQ(controlAddress, ToBcd((uint)trackNumber), relativeFad, fad, subchannels, ToBcd(index));
    }

    private static void WriteSubchannelQ(byte controlAddress, byte trackNumber, uint relativeFad, uint fad,
        Span<byte> subchannels, byte index = 1)
    {
        Span<byte> q = stackalloc byte[12];
        q[0] = (byte)((controlAddress & 0xF0) | 1);
        q[1] = trackNumber;
        q[2] = index;
        WriteBcdMsf(relativeFad, q[3..6]);
        q[6] = 0;
        WriteBcdMsf(fad, q[7..10]);
        BinaryPrimitives.WriteUInt16BigEndian(q[10..], SubchannelQChecksum(q[..10]));

        for (int i = 0; i < subchannels.Length; i++)
            subchannels[i] |= (byte)(((q[i >> 3] >> (7 - (i & 7))) & 1) << 6);
    }

    private static void WriteBcdMsf(uint frames, Span<byte> destination)
    {
        destination[0] = ToBcd(frames / (60 * 75));
        destination[1] = ToBcd(frames / 75 % 60);
        destination[2] = ToBcd(frames % 75);
    }

    private static byte ToBcd(uint value) => unchecked((byte)(((value / 10) << 4) | (value % 10)));

    internal static ushort SubchannelQChecksum(ReadOnlySpan<byte> data)
    {
        ushort crc = 0;
        foreach (byte value in data)
        {
            crc ^= (ushort)(value << 8);
            for (int bit = 0; bit < 8; bit++)
                crc = unchecked((ushort)((crc << 1) ^ ((crc & 0x8000) != 0 ? 0x1021 : 0)));
        }
        return unchecked((ushort)~crc);
    }

    private static void IsoReadAheadFad(uint fad)
    {
    }
}
