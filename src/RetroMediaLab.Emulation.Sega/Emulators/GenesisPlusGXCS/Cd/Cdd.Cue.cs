using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class Cdd
{
    private string? ReadCueLine(Stream file)
    {
        Span<byte> bytes = stackalloc byte[127];
        int count = 0;
        while (count < bytes.Length)
        {
            _cancellation.ThrowIfCancellationRequested();
            int value = file.ReadByte();
            if (value < 0) break;
            bytes[count++] = (byte)value;
            if (value == '\n') break;
        }
        return count == 0 ? null : Encoding.Latin1.GetString(bytes[..count]);
    }

    private static bool CueTime(string line, string command, out int frames)
    {
        const string number = @"([+-]?\d|\d{1,2})";
        string pattern = "^" + command + @"\s*" + number + ":" + number + ":" + number;
        Match match = Regex.Match(line, pattern, RegexOptions.CultureInvariant);
        if (!match.Success) { frames = 0; return false; }
        frames = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) * 4500
            + int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) * 75
            + int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
        return true;
    }

    private void CloseTrackResource(track_t track)
    {
        try { track.Vorbis?.Dispose(); }
        finally
        {
            track.Vorbis = null;
            try { track.File?.Dispose(); }
            finally { track.File = null; }
        }
    }

    private bool ConfigureAudioFile(track_t track, out int waveOffset, out uint firstWord)
    {
        waveOffset = 0;
        Span<byte> head = stackalloc byte[12];
        head.Clear();
        ReadStream(track.File!, head);
        SeekStream(track.File!, 0);
        firstWord = BinaryPrimitives.ReadUInt32LittleEndian(head);
        if (head[..4].SequenceEqual("RIFF"u8) && head[8..].SequenceEqual("WAVE"u8))
        {
            waveOffset = FindWaveData(track.File!);
            return waveOffset != 0;
        }
        VorbisDecoder? decoder = null;
        bool transferred = false;
        try
        {
            try { decoder = VorbisDecoder.Open(track.File!, leaveOpen: true, _cancellation); }
            catch (InvalidDataException) { return false; }
            if (decoder.SampleRate != 44100 || decoder.Channels != 2) return false;
            track.Vorbis = decoder;
            transferred = true;
            return true;
        }
        finally { if (!transferred) decoder?.Dispose(); }
    }

    private void ParseCue(Stream cue, ref string currentName, Span<byte> header, ref bool megaSd)
    {
        string? line;
        int pregap = 0, index = 0;
        if (cdd.toc.last != 0)
            while ((line = ReadCueLine(cue)) is not null)
                if (line.Contains("INDEX 01", StringComparison.Ordinal) && !line.Contains("INDEX 1", StringComparison.Ordinal)) break;
        while ((line = ReadCueLine(cue)) is not null)
        {
            line = line.TrimStart(' ');
            track_t track = cdd.toc.tracks[cdd.toc.last];
            if (line.StartsWith("FILE", StringComparison.Ordinal))
            {
                string value = line[4..].TrimStart(' ');
                int first = value.StartsWith('"') ? 1 : 0;
                int end = value.IndexOf(first == 1 ? '"' : ' ', first);
                if (end < 0) end = value.Length;
                int slash = currentName.AsSpan().LastIndexOfAny('/', '\\');
                string folder = slash >= 0 ? currentName[..(slash + 1)] : "";
                string name = value[first..end];
                currentName = folder + name;
                if (currentName.Length > 255) currentName = currentName[..255];
                track.File = OpenFile(currentName);
                if (track.File is null) break;
                pregap = 0; track.offset = 0;
                string suffix = value[end..];
                if (!suffix.Contains("BINARY", StringComparison.Ordinal) && !suffix.Contains("MOTOROLA", StringComparison.Ordinal))
                {
                    if (!ConfigureAudioFile(track, out int waveOffset, out _))
                    {
                        CloseTrackResource(track); break;
                    }
                    track.offset -= waveOffset;
                }
            }
            else if (Regex.IsMatch(line, @"^TRACK\s*[+-]?\d+", RegexOptions.CultureInvariant))
            {
                if (cdd.toc.last == 0)
                {
                    if (line.Contains("MODE1/2048", StringComparison.Ordinal)) { cdd.sectorSize = 2048; track.type = 1; }
                    else if (line.Contains("MODE1/2352", StringComparison.Ordinal)) { cdd.sectorSize = 2352; track.type = 1; }
                    else if (line.Contains("MODE2/2352", StringComparison.Ordinal)) { cdd.sectorSize = 2352; track.type = 2; }
                    if (cdd.sectorSize != 0 && track.File is not null)
                    {
                        if (cdd.sectorSize == 2352) SeekStream(track.File, 16);
                        ReadStream(track.File, header[..0x210]);
                        SeekStream(track.File, 0);
                    }
                }
                else if (track.File is null) cdd.toc.tracks[cdd.toc.last - 1].end = 0;
                index = cdd.toc.last;
            }
            else if (Regex.Match(line, @"^REM\s*LOOP\s*([+-]?\d+)", RegexOptions.CultureInvariant) is { Success: true } loop)
            {
                cdd.toc.tracks[index].loopEnabled = 1;
                cdd.toc.tracks[index].loopOffset = int.Parse(loop.Groups[1].Value, CultureInfo.InvariantCulture);
                megaSd = true;
            }
            else if (line.Contains("REM LOOP", StringComparison.Ordinal))
            {
                cdd.toc.tracks[index].loopEnabled = 1; megaSd = true;
            }
            else if (line.Contains("REM NOLOOP", StringComparison.Ordinal))
            {
                cdd.toc.tracks[index].loopEnabled = -1; megaSd = true;
            }
            else if (CueTime(line, "PREGAP", out int pause)) pregap += pause;
            else if (CueTime(line, @"INDEX\s*00", out int zero) || CueTime(line, @"INDEX\s*0", out zero))
            {
                if (cdd.toc.last != 0 && cdd.toc.tracks[cdd.toc.last - 1].end == 0)
                    cdd.toc.tracks[cdd.toc.last - 1].end = zero + pregap;
            }
            else if (CueTime(line, @"INDEX\s*01", out int one) || CueTime(line, @"INDEX\s*1", out one))
            {
                track.offset += pregap * 2352;
                if (track.File is null && cdd.toc.last != 0)
                {
                    track.File = cdd.toc.tracks[cdd.toc.last - 1].File;
                    track.start = one + pregap;
                    if (cdd.toc.tracks[cdd.toc.last - 1].end == 0) cdd.toc.tracks[cdd.toc.last - 1].end = track.start;
                }
                else
                {
                    if (track.File is null) break;
                    track.start = cdd.toc.end + pregap;
                    track.offset += cdd.toc.end * 2352;
                    if (track.Vorbis is not null)
                    {
                        track.offset /= 4;
                        track.end = track.start + unchecked((int)(track.Vorbis.TotalFrames / 588));
                        if (track.end <= track.start)
                        {
                            CloseTrackResource(track);
                            track.start = track.end = track.offset = 0;
                            break;
                        }
                    }
                    else track.end = track.start + unchecked((int)(track.File.Length / (track.type != 0 ? cdd.sectorSize : 2352)));
                    SeekStream(track.File, 0);
                    track.start += one;
                    cdd.toc.end = track.end;
                }
                cdd.toc.last++;
                if (cdd.toc.last == 99) break;
            }
        }
        if (cdd.toc.last != 0 && cdd.toc.tracks[cdd.toc.last - 1].end == 0)
        {
            cdd.toc.end += pregap;
            cdd.toc.tracks[cdd.toc.last - 1].end = cdd.toc.end;
        }
        CloseTrackResource(cdd.toc.tracks[cdd.toc.last]);
    }
}
