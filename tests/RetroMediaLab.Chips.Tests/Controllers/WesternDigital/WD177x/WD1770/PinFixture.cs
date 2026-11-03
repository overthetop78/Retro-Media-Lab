using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.WesternDigital.WD177x.WD1770;

/// <summary>Broches seules : aucun conteneur, SDK, lecteur hôte ou fichier externe.</summary>
internal sealed class PinFixture
{
    internal readonly Wd1770 Chip;
    internal readonly List<byte> Read = [];
    internal readonly Queue<byte> Write = [];
    internal readonly List<(byte Data, Wd1770ReadWordKind Kind)> Written = [];
    internal int Track;
    internal bool ServiceReads = true;
    internal bool ServiceWrites = true;
    private readonly Wd1770ReadChannel _monitor;
    private bool _step;
    private bool _encodingPrevious;
    internal PinFixture(bool mfm, Wd1770Model model = Wd1770Model.Wd1770)
    {
        Chip = new(model) { DoubleDensity = mfm };
        Chip.SetTrackZero(true);
        _monitor = new() { DoubleDensity = mfm };
        _monitor.ByteReceived += (data, kind, _) => Written.Add((data, kind));
    }
    internal void Tick(bool readData = false)
    {
        Chip.SetReadData(readData);
        Chip.Tick();
        _monitor.Tick(Chip.WriteData);
        if (Chip.Step && !_step)
        {
            Track = Math.Clamp(Track + (Chip.Direction ? 1 : -1), 0, 255);
            Chip.SetTrackZero(Track == 0);
        }
        _step = Chip.Step;
        if (!Chip.Drq) return;
        if (Chip.Reading && ServiceReads) Read.Add(Chip.Read(3));
        else if (Chip.Writing && ServiceWrites && Write.TryDequeue(out var value)) Chip.Write(3, value);
    }
    internal void Advance(int clocks) { for (var clock = 0; clock < clocks; clock++) Tick(); }
    internal void Index()
    {
        Chip.SetIndex(false);
        Chip.SetIndex(true);
        Advance(160);
        Chip.SetIndex(false);
        Advance(160);
    }
    internal void Command(byte value, byte sector = 1)
    {
        Chip.Write(2, sector);
        Chip.Write(0, value);
    }
    internal void FeedId(byte cylinder = 0, byte number = 1, byte size = 0, bool badCrc = false, byte head = 0)
        => FeedField(0xfe, [cylinder, head, number, size], badCrc);
    internal void FeedField(byte mark, byte[] data, bool badCrc = false)
    {
        FeedBytes(new byte[Chip.DoubleDensity ? 12 : 6]);
        ushort crc = 0xffff;
        if (Chip.DoubleDensity)
        {
            for (var sync = 0; sync < 3; sync++) { FeedWord(0x4489); crc = Wd1770Crc.Add(crc, 0xa1); }
            _encodingPrevious = true;
            FeedBytes([mark]);
        }
        else FeedWord(FmWord(mark, 0xc7));
        crc = Wd1770Crc.Add(crc, mark);
        foreach (var value in data) crc = Wd1770Crc.Add(crc, value);
        if (badCrc) crc ^= 1;
        FeedBytes(data);
        FeedBytes([(byte)(crc >> 8), (byte)crc]);
    }
    internal void FeedBytes(IEnumerable<byte> bytes)
    {
        foreach (var value in bytes)
        {
            if (!Chip.DoubleDensity) { FeedWord(FmWord(value, 0xff)); continue; }
            var word = 0;
            for (var bit = 7; bit >= 0; bit--)
            {
                var data = (value >> bit & 1) != 0;
                word = word << 2 | (!_encodingPrevious && !data ? 2 : 0) | (data ? 1 : 0);
                _encodingPrevious = data;
            }
            FeedWord((ushort)word);
        }
    }
    internal void FeedWord(ushort word)
    {
        var clocks = Chip.DoubleDensity ? 16 : 32;
        for (var cell = 15; cell >= 0; cell--)
            for (var clock = 0; clock < clocks; clock++) Tick((word >> cell & 1) != 0 && clock < 4);
    }
    private static ushort FmWord(byte data, byte clocks)
    {
        var word = 0;
        for (var bit = 7; bit >= 0; bit--) word = word << 2 | (clocks >> bit & 1) << 1 | (data >> bit & 1);
        return (ushort)word;
    }
}
