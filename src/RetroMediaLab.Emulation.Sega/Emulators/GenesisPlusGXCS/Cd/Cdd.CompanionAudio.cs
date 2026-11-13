using System.Buffers.Binary;
using System.Globalization;
using System.IO;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class Cdd
{
    private static readonly string[] CompanionPrefixes = ["", " ", "-", "_", " - "];
    private void LoadCompanionAudio(ref string currentName)
    {
        string stem = currentName.Length >= 4 ? currentName[..^4] : currentName;
        Stream? file = null;
        int sequenceOffset = 1, selectedFormat = -1;
        for (int format = 0; format < 20; format++)
        {
            currentName = CompanionName(stem, format, cdd.toc.last);
            file = OpenFile(currentName);
            if (file is not null) { sequenceOffset = 0; selectedFormat = format; break; }
            currentName = CompanionName(stem, format, cdd.toc.last + 1);
            file = OpenFile(currentName);
            if (file is not null) { selectedFormat = format; break; }
        }
        while (file is not null)
        {
            track_t track = cdd.toc.tracks[cdd.toc.last];
            track.File = file;
            if (!ConfigureAudioFile(track, out int waveOffset, out uint firstWord))
            {
                CloseTrackResource(track); break;
            }
            track.start = cdd.toc.end + 150;
            if (track.Vorbis is null)
            {
                track.end = track.start + unchecked((int)((file.Length - waveOffset + 2351) / 2352));
                track.offset = track.start * 2352;
                SeekStream(file, 100 * 2352);
            }
            else
            {
                track.end = track.start + unchecked((int)((track.Vorbis.TotalFrames + 587) / 588));
                if (track.end <= track.start)
                {
                    CloseTrackResource(track); track.start = track.end = 0; break;
                }
                track.offset = track.start * 588;
                track.Vorbis.SeekPcm(100 * 588);
            }
            Span<byte> probe = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(probe, firstWord);
            if (track.Vorbis is null)
            {
                ReadStream(file, probe);
                SeekStream(file, 0);
            }
            else
            {
                track.Vorbis.ReadPcm(probe);
                track.Vorbis.SeekPcm(0);
            }
            if (BinaryPrimitives.ReadUInt32LittleEndian(probe) == 0)
            {
                track.offset -= 150 * (track.Vorbis is null ? 2352 : 588);
                track.end -= 150;
            }
            cdd.toc.end = track.end;
            if (track.Vorbis is null) track.offset -= waveOffset;
            cdd.toc.last++;
            if (cdd.toc.last == 99) break;
            currentName = CompanionName(stem, selectedFormat, cdd.toc.last + sequenceOffset);
            file = OpenFile(currentName);
        }
    }
    private static string CompanionName(string stem, int format, int index)
    {
        string extension = format < 10 ? ".ogg" : ".wav";
        int variant = format % 10;
        return stem + CompanionPrefixes[variant % 5] + index.ToString(variant < 5 ? "D2" : "D", CultureInfo.InvariantCulture) + extension;
    }
}
