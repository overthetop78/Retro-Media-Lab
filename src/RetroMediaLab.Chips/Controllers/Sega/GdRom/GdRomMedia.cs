using System.Buffers;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.Controllers.Sega.GdRom;

public readonly record struct GdRomMediaTrack(int Session, int Number, uint StartFad, uint EndFadExclusive, bool IsAudio)
{
    internal byte Control => IsAudio ? (byte)0 : (byte)4;
}

public static unsafe class GdRomMedia
{
    private const uint GdRomEndFad = 549300;
    private const int DataSectorSize = 2048;
    private const int RawSectorSize = 2352;

    private sealed record MountedDisc(
        object Owner, GdRomMediaTrack[] Tracks,
        Func<int, Memory<byte>, bool> ReadData,
        Func<int, Memory<byte>, bool> ReadAudio,
        Func<int, Memory<byte>, bool> ReadRaw);

    private static MountedDisc? _disc;

    public static void Mount(
        object owner, IReadOnlyList<GdRomMediaTrack> tracks,
        Func<int, Memory<byte>, bool> readData, Func<int, Memory<byte>, bool> readAudio,
        Func<int, Memory<byte>, bool> readRaw)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(tracks);
        ArgumentNullException.ThrowIfNull(readData);
        ArgumentNullException.ThrowIfNull(readAudio);
        ArgumentNullException.ThrowIfNull(readRaw);

        GdRomMediaTrack[] ordered = tracks.OrderBy(track => track.Number).ToArray();
        Volatile.Write(ref _disc, new MountedDisc(owner, ordered, readData, readAudio, readRaw));
        GdRomInterface.SetDisc();
    }

    public static void Unmount(object owner)
    {
        MountedDisc? mounted = Volatile.Read(ref _disc);
        if (mounted is null || !ReferenceEquals(mounted.Owner, owner))
            return;
        if (ReferenceEquals(Interlocked.CompareExchange(ref _disc, null, mounted), mounted))
            GdRomInterface.SetDisc();
    }

    internal static void TermDrive() => Interlocked.Exchange(ref _disc, null);

    internal static GdRomDiscType GetDiscType() =>
        Volatile.Read(ref _disc) is null ? GdRomDiscType.NoDisk : GdRomDiscType.GdRom;

    internal static uint ReadSector(byte* destination, uint fad, uint count, uint sectorSize, bool stopOnMiss = false)
    {
        MountedDisc? disc = Volatile.Read(ref _disc);
        int size = checked((int)sectorSize);
        if (disc is null)
        {
            if (stopOnMiss)
                return 0;
            NativeMemory.Clear(destination, (nuint)count * sectorSize);
            return count;
        }

        byte[] scratch = ArrayPool<byte>.Shared.Rent(RawSectorSize);
        try
        {
            for (uint i = 0; i < count; i++, fad++, destination += size)
            {
                if (stopOnMiss && fad >= GdRomEndFad)
                    return i;
                Span<byte> output = new(destination, size);
                if (!TryReadSector(disc, fad, output, scratch))
                    output.Clear();
            }
            return count;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(scratch);
        }
    }

    private static bool TryReadSector(MountedDisc disc, uint fad, Span<byte> output, byte[] scratch)
    {
        GdRomMediaTrack? track = null;
        for (int i = disc.Tracks.Length - 1; i >= 0; i--)
        {
            GdRomMediaTrack candidate = disc.Tracks[i];
            if (fad >= candidate.StartFad && fad < candidate.EndFadExclusive)
            {
                track = candidate;
                break;
            }
        }
        if (track is null || fad > int.MaxValue)
            return false;

        int address = (int)fad;
        if (output.Length == DataSectorSize)
        {
            if (track.Value.IsAudio || !disc.ReadData(address, scratch.AsMemory(0, DataSectorSize)))
                return false;
            scratch.AsSpan(0, DataSectorSize).CopyTo(output);
            return true;
        }

        if (output.Length is not (RawSectorSize or 2340))
            return false;
        bool read = track.Value.IsAudio
            ? disc.ReadAudio(address, scratch.AsMemory(0, RawSectorSize))
            : disc.ReadRaw(address, scratch.AsMemory(0, RawSectorSize));
        if (!read)
        {
            if (output.Length != RawSectorSize || track.Value.IsAudio
                || !disc.ReadData(address, scratch.AsMemory(0, DataSectorSize)))
                return false;
            scratch.AsSpan(DataSectorSize, RawSectorSize - DataSectorSize).Clear();
        }
        scratch.AsSpan(RawSectorSize - output.Length, output.Length).CopyTo(output);
        return true;
    }

    internal static void GetToc(uint* toc, int area)
    {
        Span<uint> entries = new(toc, 102);
        entries.Fill(uint.MaxValue);
        MountedDisc? disc = Volatile.Read(ref _disc);
        if (disc is null)
            return;

        int first = area == 1 ? 3 : 1;
        int last = area == 1 ? disc.Tracks.Length : 2;
        if (first > last || last > disc.Tracks.Length || last > 99)
            return;
        GdRomMediaTrack initial = disc.Tracks[first - 1];
        GdRomMediaTrack final = disc.Tracks[last - 1];
        entries[99] = TrackInfo(initial.Control, (uint)first << 16);
        entries[100] = TrackInfo(final.Control, (uint)last << 16);
        entries[101] = TrackInfo(4, area == 1 ? GdRomEndFad : final.EndFadExclusive);
        for (int i = first - 1; i < last; i++)
            entries[i] = TrackInfo(disc.Tracks[i].Control, disc.Tracks[i].StartFad);
    }

    private static uint TrackInfo(byte control, uint fad) =>
        (uint)((control << 4) | 1) | ((fad >> 8) & 0xff00u) | ((fad << 8) & 0xff0000u) | (fad << 24);

    internal static void GetSessionInfo(byte* info, uint session)
    {
        MountedDisc? disc = Volatile.Read(ref _disc);
        if (disc is null)
            return;

        info[0] = 2;
        info[1] = 0;
        byte track = 0;
        uint fad = 0;
        if (session == 0)
        {
            track = 2;
            fad = GdRomEndFad;
        }
        else
        {
            foreach (GdRomMediaTrack entry in disc.Tracks)
            {
                if (entry.Session == session)
                {
                    track = (byte)entry.Number;
                    fad = entry.StartFad;
                    break;
                }
            }
        }
        info[2] = track;
        info[3] = (byte)(fad >> 16);
        info[4] = (byte)(fad >> 8);
        info[5] = (byte)fad;
    }

    internal static uint GetTrackNumber(uint fad, out uint elapsed)
    {
        MountedDisc? disc = Volatile.Read(ref _disc);
        if (disc is not null)
        {
            foreach (GdRomMediaTrack track in disc.Tracks)
            {
                if (fad >= track.StartFad && fad < track.EndFadExclusive)
                {
                    elapsed = fad - track.StartFad;
                    return (uint)track.Number;
                }
            }
        }
        elapsed = 0;
        return 0xaa;
    }

    internal static void GetTrackAdrAndControl(uint trackNumber, out byte adr, out byte control)
    {
        MountedDisc? disc = Volatile.Read(ref _disc);
        if (disc is null || trackNumber == 0 || trackNumber > disc.Tracks.Length)
        {
            adr = control = 0;
            return;
        }

        GdRomMediaTrack track = disc.Tracks[(int)trackNumber - 1];
        adr = 1;
        control = track.Control;
    }
}
