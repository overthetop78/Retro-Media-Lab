using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class Cdd
{
    private static bool HasExtension(string name, string extension) => name.EndsWith(extension, StringComparison.Ordinal) || name.EndsWith(extension.ToUpperInvariant(), StringComparison.Ordinal);
    private Stream? OpenFile(string name)
    {
        _cancellation.ThrowIfCancellationRequested();
        try { return _openFile(name); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    public int Load(string filename, Span<byte> header)
    {
        if (header.Length < 0x210) throw new ArgumentException("The CD header buffer must contain at least 528 bytes.", nameof(header));
        Unload();
        Stream? file = OpenFile(filename);
        if (file is null) return 0;
        bool transferred = false, mounted = false;
        try
        {
            if (HasExtension(filename, "chd"))
            {
                _chd = ChdImage.Open(file, leaveOpen: true, _cancellation);
                _chdFile = file;
                transferred = true;
                int result = LoadChd(header);
                mounted = result > 0;
                return result;
            }
            bool isCdFile = true, isMegaSdFile = false;
            string currentName = filename.Length > 255 ? filename[..255] : filename;
            Stream? cue;
            bool cueOwned = false;
            if (!HasExtension(filename, "cue"))
            {
                ReadStream(file, header[..16]);
                if (header[..14].SequenceEqual("SEGADISCSYSTEM"u8))
                {
                    cdd.sectorSize = 2048;
                    cdd.toc.tracks[0].type = 1;
                }
                else if (header[..12].SequenceEqual(new byte[] { 0, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0 }))
                {
                    cdd.sectorSize = 2352;
                    cdd.toc.tracks[0].type = header[15];
                    ReadStream(file, header[..16]);
                }
                if (cdd.toc.tracks[0].type is 1 or 2)
                {
                    ReadStream(file, header.Slice(16, 0x200));
                    track_t track = cdd.toc.tracks[0];
                    track.File = file; transferred = true;
                    track.end = Math.Max(150, unchecked((int)(file.Length / cdd.sectorSize)));
                    track.start = 0;
                    SeekStream(file, 0);
                    cdd.toc.end = track.end; cdd.toc.last = 1;
                }
                else isCdFile = false;
                int dot = currentName.Length;
                while ((dot != 0 && (dot == currentName.Length || currentName[dot] != '.')) || dot > 251) dot--;
                currentName = currentName[..dot] + ".cue";
                cue = OpenFile(currentName);
                cueOwned = cue is not null;
            }
            else cue = file;
            try
            {
                if (cue is not null) ParseCue(cue, ref currentName, header, ref isMegaSdFile);
                else if (cdd.toc.last != 0)
                {
                    LoadCompanionAudio(ref currentName);
                    if (cdd.toc.tracks[0].type == 1 && cdd.toc.last == 1) RestoreOriginalFallbackToc(header);
                }
            }
            finally { if (cueOwned) cue?.Dispose(); }
            if (cdd.toc.last != 0 && cdd.toc.end <= 99 * 60 * 75)
            {
                cdd.toc.tracks[cdd.toc.last].start = cdd.toc.end;
                cdd.loaded = isMegaSdFile ? 2 : 1;
                string subName = currentName.Length >= 4 ? currentName[..^4] + ".sub" : currentName + ".sub";
                _subcode = OpenFile(subName);
                mounted = true;
                return isCdFile ? 1 : 0;
            }
            return 0;
        }
        catch (InvalidDataException) { return -1; }
        finally
        {
            try { if (!transferred) file.Dispose(); }
            finally { if (!mounted) Unload(); }
        }
    }

    private int LoadChd(Span<byte> header)
    {
        if (_chd!.HunkBytes == 0 || _chd.HunkBytes % 2448 != 0) return -1;
        _hunk = new byte[_chd.HunkBytes]; _hunkNumber = -1;
        int sectors = 0;
        for (cdd.toc.last = 0; cdd.toc.last < 99; cdd.toc.last++)
        {
            string? metadata = _chd.GetMetadata("CHT2", cdd.toc.last);
            bool complete = metadata is not null;
            metadata ??= _chd.GetMetadata("CHTR", cdd.toc.last);
            if (metadata is null || metadata.Length >= 256) break;
            string pattern = @"^TRACK:(-?\d+) TYPE:(\S{1,15}) SUBTYPE:(\S{1,15}) FRAMES:(-?\d+)";
            if (complete) pattern += @" PREGAP:(-?\d+) PGTYPE:(\S{1,15}) PGSUB:(\S{1,15}) POSTGAP:(-?\d+)";
            Match match = Regex.Match(metadata, pattern, RegexOptions.CultureInvariant);
            if (!match.Success) break;
            int number = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            string type = match.Groups[2].Value;
            int frames = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
            int pregap = complete ? int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture) : 0;
            int postgap = complete ? int.Parse(match.Groups[8].Value, CultureInfo.InvariantCulture) : 0;
            if (number != cdd.toc.last + 1 || frames < 0 || pregap < 0 || postgap < 0) break;
            track_t track = cdd.toc.tracks[cdd.toc.last];
            if (cdd.toc.last != 0)
            {
                if (type != "AUDIO") break;
                track.start = cdd.toc.end + pregap;
            }
            else
            {
                switch (type)
                {
                    case "MODE1_RAW": cdd.sectorSize = 2352; track.type = 1; break;
                    case "MODE1": cdd.sectorSize = 2048; track.type = 1; break;
                    case "MODE2_RAW": cdd.sectorSize = 2352; track.type = 2; break;
                    case "AUDIO": break;
                    default: return -1;
                }
                track.start = 0;
            }
            if (!complete || !match.Groups[6].Value.StartsWith('V')) pregap = 0;
            track.end = track.start + frames - pregap;
            track.offset = (sectors + pregap - track.start) * 2448;
            cdd.toc.end = track.end + postgap;
            sectors += (frames + 3) / 4 * 4;
        }
        if (cdd.sectorSize != 0)
        {
            int offset = cdd.toc.tracks[0].offset;
            ReadHunk(offset).Slice(cdd.sectorSize == 2048 ? 0 : 16, 0x210).CopyTo(header);
        }
        if (cdd.toc.last == 0 || cdd.toc.end > 99 * 60 * 75) return -1;
        cdd.toc.tracks[cdd.toc.last].start = cdd.toc.end;
        cdd.loaded = 1;
        return 1;
    }

    private int FindWaveData(Stream file)
    {
        Span<byte> chunk = stackalloc byte[8];
        SeekStream(file, 12);
        while (ReadStream(file, chunk) == chunk.Length)
        {
            if (chunk[..4].SequenceEqual("data"u8))
            {
                int offset = unchecked((int)file.Position);
                SeekStream(file, 0);
                return offset;
            }
            int length = BinaryPrimitives.ReadInt32LittleEndian(chunk[4..]);
            SeekStream(file, length, SeekOrigin.Current);
        }
        return 0;
    }

    private void RestoreOriginalFallbackToc(ReadOnlySpan<byte> header)
    {
        string product = Encoding.ASCII.GetString(header[0x180..]);
        if (product.Contains("T-95035", StringComparison.Ordinal)) ApplyToc(toc_snatcher);
        else if (product.Contains("T-127015", StringComparison.Ordinal)) ApplyToc(toc_lunar);
        else if (product.Contains("T-113045", StringComparison.Ordinal)) ApplyToc(toc_shadow);
        else if (product.Contains("T-143025", StringComparison.Ordinal)) ApplyToc(toc_dungeon);
        else if (product.Contains("MK-4410", StringComparison.Ordinal)) ApplyToc(toc_ffight);
        else if (product.Contains("G-6013", StringComparison.Ordinal)) ApplyToc(toc_ffightj);
        else if (!product.Contains("T-06201-01", StringComparison.Ordinal))
        {
            do
            {
                track_t track = cdd.toc.tracks[cdd.toc.last];
                track.start = cdd.toc.end + 150; track.end = track.start + 150;
                cdd.toc.end = track.end; cdd.toc.last++;
            } while (cdd.toc.last < 99 && cdd.toc.end < 56 * 60 * 75);
        }
    }
    private void ApplyToc(ReadOnlySpan<ushort> lengths)
    {
        cdd.toc.last = cdd.toc.end = 0;
        foreach (ushort length in lengths) AppendTocLength(length);
    }
    private void ApplyToc(ReadOnlySpan<uint> lengths)
    {
        cdd.toc.last = cdd.toc.end = 0;
        foreach (uint length in lengths) AppendTocLength(unchecked((int)length));
    }
    private void AppendTocLength(int length)
    {
        track_t track = cdd.toc.tracks[cdd.toc.last];
        track.start = cdd.toc.end; track.end = track.start + length;
        cdd.toc.end = track.end; cdd.toc.last++;
    }
}
