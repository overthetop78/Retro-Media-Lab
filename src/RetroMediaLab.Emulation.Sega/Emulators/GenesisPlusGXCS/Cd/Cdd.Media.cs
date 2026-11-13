using System.Buffers.Binary;
using System.IO;
using System.Runtime.ExceptionServices;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class Cdd
{
    private sealed partial class track_t
    {
        internal Stream? File;
        internal VorbisDecoder? Vorbis;
    }
    private Stream? _subcode;
    private ChdImage? _chd;
    private Stream? _chdFile;
    private byte[]? _hunk;
    private int _hunkNumber = -1;
    private uint _chdAudioPosition;

    private int ReadStream(Stream stream, Span<byte> destination)
    {
        int total = 0;
        while (!destination.IsEmpty)
        {
            _cancellation.ThrowIfCancellationRequested();
            int count;
            try { count = stream.Read(destination); }
            catch (IOException) { break; }
            if (count == 0) break;
            total += count;
            destination = destination[count..];
        }
        return total;
    }
    private static int SeekStream(Stream stream, long position, SeekOrigin origin = SeekOrigin.Begin)
    {
        try { stream.Seek(position, origin); return 0; }
        catch (IOException) { return -1; }
        catch (ArgumentOutOfRangeException) when (position < 0) { return -1; }
    }
    private Span<byte> ReadHunk(int offset)
    {
        int number = offset / _chd!.HunkBytes;
        if (number != _hunkNumber)
        {
            _chd.ReadHunk(number, _hunk!);
            _hunkNumber = number;
        }
        return _hunk.AsSpan(offset % _chd.HunkBytes);
    }

    public void ReadData(Span<byte> destination, Span<byte> subheader)
    {
        if (cdd.toc.tracks[cdd.index].type == 0 || cdd.lba < 0) return;
        if (_chd is not null)
        {
            int offset = cdd.toc.tracks[0].offset + cdd.lba * 2448;
            Span<byte> sector = ReadHunk(offset);
            if (cdd.sectorSize == 2048) sector[..2048].CopyTo(destination);
            else if (subheader.IsEmpty) sector.Slice(16, 2048).CopyTo(destination);
            else
            {
                sector.Slice(16, 4).CopyTo(subheader);
                sector.Slice(24, 2328).CopyTo(destination);
            }
            return;
        }
        Stream file = cdd.toc.tracks[0].File!;
        if (cdd.sectorSize == 2048)
        {
            SeekStream(file, cdd.lba * 2048L);
            ReadStream(file, destination[..2048]);
        }
        else if (subheader.IsEmpty)
        {
            SeekStream(file, cdd.lba * 2352L + 16);
            ReadStream(file, destination[..2048]);
        }
        else
        {
            SeekStream(file, cdd.lba * 2352L + 20);
            ReadStream(file, subheader[..4]);
            ReadStream(file, destination[..2328]);
        }
    }

    public void SeekAudio(int index, int lba)
    {
        track_t track = cdd.toc.tracks[index];
        if (_chd is not null) _chdAudioPosition = unchecked((uint)(track.offset + lba * 2448));
        else if (track.Vorbis is not null) track.Vorbis.SeekPcm(lba * 588L - track.offset);
        else if (track.File is not null) SeekStream(track.File, lba * 2352L - track.offset);
    }

    private void ReadSubcode()
    {
        Span<byte> subcode = stackalloc byte[96];
        scd_regs[0x68 >> 1].Low = (byte)((scd_regs[0x68 >> 1].Low + 98) & 0x7e);
        int index = (scd_regs[0x68 >> 1].Low + 0x100) >> 1;
        ReadStream(_subcode!, subcode);
        for (int i = 0; i < 96; i += 2)
        {
            int code = 0;
            for (int j = 0; j < 8; j++)
            {
                int bits = (subcode[j * 12 + i / 8] >> (6 - (i & 6))) & 3;
                code |= (bits & 1) << (7 - j);
                code |= (bits >> 1) << (15 - j);
            }
            scd_regs[index].Word = (ushort)code;
            index = (index + 1) & 0xbf;
        }
        if ((scd_regs[0x32 >> 1].Low & 0x40) != 0)
        {
            pending |= 1 << 6;
            s68k_update_irq((uint)((pending & scd_regs[0x32 >> 1].Low) >> 1));
        }
    }

    public void Unload()
    {
        Exception? failure = null;
        HashSet<object> released = new(ReferenceEqualityComparer.Instance);
        try
        {
            for (int i = 0; i < cdd.toc.tracks.Length; i++)
            {
                track_t track = cdd.toc.tracks[i];
                try
                {
                    if (track.Vorbis is not null && released.Add(track.Vorbis)) track.Vorbis.Dispose();
                }
                catch (Exception error) { failure ??= error; }
                finally
                {
                    track.Vorbis = null;
                    try
                    {
                        if (track.File is not null && released.Add(track.File)) track.File.Dispose();
                    }
                    catch (Exception error) { failure ??= error; }
                    finally
                    {
                        track.File = null;
                        track.offset = track.start = track.end = track.type = track.loopEnabled = track.loopOffset = 0;
                    }
                }
            }
        }
        finally
        {
            try { _subcode?.Dispose(); }
            catch (Exception error) { failure ??= error; }
            finally
            {
                _subcode = null;
                try { _chd?.Dispose(); }
                catch (Exception error) { failure ??= error; }
                finally
                {
                    try { _chdFile?.Dispose(); }
                    catch (Exception error) { failure ??= error; }
                    finally
                    {
                        _chdFile = null; _chd = null; _hunk = null; _hunkNumber = -1; _chdAudioPosition = 0;
                        cdd.loaded = 0; cdd.toc.last = cdd.toc.end = 0; cdd.sectorSize = 0;
                    }
                }
            }
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
