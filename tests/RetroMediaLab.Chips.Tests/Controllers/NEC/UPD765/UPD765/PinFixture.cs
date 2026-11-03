using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;
using static RetroMediaLab.Chips.Tests.Shared.Upd765TestCommands;

namespace RetroMediaLab.Chips.Tests.Controllers.NEC.UPD765.UPD765;

// Banc autonome : encodeur de piste et horloge externes, aucun lecteur/image de production.
internal sealed class PinFixture
{
    internal readonly Upd765 Chip;
    internal readonly byte[] Status = [0x38, 0x10, 0x10, 0x10];
    internal readonly int[] Position = new int[4];
    internal readonly List<byte> Received = [];
    internal readonly Queue<byte> HostInput = new();
    internal readonly List<ushort> Output = [];
    internal Action<int>? WordObserved;
    internal bool HostEnabled = true;
    internal int TerminalAfter = int.MaxValue;
    internal int HostTransfers;
    internal int HostLimit = int.MaxValue;
    internal bool Pio;
    private readonly bool _mfm;
    private bool _window;
    private int _outputBits;
    private ushort _outputWord;
    private bool _previousData;
    private readonly List<ushort> _track = [];

    internal PinFixture(bool mfm = true, bool pio = false, Upd765Model model = Upd765Model.Upd765B)
    {
        _mfm = mfm;
        Pio = pio;
        Chip = new(unit => Status[unit], (unit, direction) => Position[unit] += direction, model: model);
        Specify(Chip, pio);
    }

    internal void Command(byte command, byte record = 1, byte end = 1, byte size = 0, byte dtl = 128,
        byte cylinder = 0, byte head = 0, byte unit = 0) =>
        Send(Chip, (byte)(command | (_mfm ? 0x40 : 0)), (byte)(unit | head << 2), cylinder, head,
            record, size, end, 0x1b, dtl);

    internal void ReadyForTrack()
    {
        while (Chip.Active && Chip.NextEventTicks != long.MaxValue) Event(Chip);
        Chip.SetReadSignals(_window, false);
    }

    internal void Index()
    {
        Chip.SetIndex(false);
        Chip.SetIndex(true);
        Chip.SetIndex(false);
    }

    internal void Sector(byte record, byte[] data, byte cylinder = 0, byte head = 0, byte size = 0,
        bool deleted = false, bool badId = false, bool badData = false, bool includeData = true)
    {
        Address(0xfe);
        var crc = CrcPrefix(0xfe);
        foreach (var value in new[] { cylinder, head, record, size }) { Data(value); crc = Crc(crc, value); }
        Data((byte)(crc >> 8)); Data((byte)(crc ^ (badId ? 1 : 0)));
        for (var gap = 0; gap < (_mfm ? 22 : 11); gap++) Data(_mfm ? (byte)0x4e : (byte)0xff);
        if (!includeData) return;
        var mark = deleted ? (byte)0xf8 : (byte)0xfb;
        Address(mark);
        crc = CrcPrefix(mark);
        foreach (var value in data) { Data(value); crc = Crc(crc, value); }
        Data((byte)(crc >> 8)); Data((byte)(crc ^ (badData ? 1 : 0)));
        for (var gap = 0; gap < 24; gap++) Data(_mfm ? (byte)0x4e : (byte)0xff);
    }

    internal void FeedTrack(bool waitForReady = true)
    {
        if (waitForReady) ReadyForTrack();
        for (var index = 0; index < _track.Count; index++)
        {
            var word = _track[index];
            for (var bit = 15; bit >= 0; bit--) Cell((word & 1 << bit) != 0);
            WordObserved?.Invoke(index);
        }
    }

    internal void Clocks(int cells)
    {
        for (var cell = 0; cell < cells; cell++) Cell(false);
    }

    internal void UntilResult(int maximumCells = 50000)
    {
        for (var cell = 0; Chip.Active && cell < maximumCells; cell++) Cell(false);
        Assert.False(Chip.Active);
    }

    internal byte[] Result() => RetroMediaLab.Chips.Tests.Shared.Upd765TestCommands.Result(Chip);

    internal void Cell(bool pulse)
    {
        Chip.SetWriteClock(true);
        Chip.Advance(1);
        if (Chip.WriteEnable)
        {
            _outputWord = (ushort)(_outputWord << 1 | (Chip.WriteData ? 1 : 0));
            if (++_outputBits == 16) { Output.Add(_outputWord); _outputBits = 0; _outputWord = 0; }
        }
        Chip.Advance(1);
        Chip.SetWriteClock(false);
        Chip.Advance(1);
        Chip.SetReadSignals(_window, pulse);
        Chip.SetReadSignals(_window, false);
        _window = !_window;
        Chip.SetReadSignals(_window, false);
        Service();
        Chip.Advance((_mfm ? 8 : 16) - 3);
    }

    internal void Service()
    {
        if (!HostEnabled || HostTransfers >= HostLimit) return;
        var status = Chip.ReadMainStatus() & 0xf0;
        if (!(Pio ? status is 0xb0 or 0xf0 : Chip.Drq)) return;
        if (status == 0xd0) return;
        if ((status & 0x40) != 0)
            Received.Add(Pio ? Chip.ReadFifo() : Chip.ReadDma());
        else
        {
            if (HostInput.Count == 0) return;
            var value = HostInput.Dequeue();
            if (Pio) Chip.WriteFifo(value); else Chip.WriteDma(value);
        }
        if (++HostTransfers == TerminalAfter) Chip.SetTerminalCount(true);
    }

    private void Address(byte mark)
    {
        for (var sync = 0; sync < (_mfm ? 12 : 6); sync++) Data(0);
        if (_mfm)
            for (var sync = 0; sync < 3; sync++) { _track.Add(0x4489); _previousData = true; }
        Data(mark, _mfm ? (byte)0xff : (byte)0xc7);
    }

    private void Data(byte value, byte clocks = 0xff)
    {
        ushort word = 0;
        for (var bit = 7; bit >= 0; bit--)
        {
            var data = (value & 1 << bit) != 0;
            var clock = _mfm ? !_previousData && !data : (clocks & 1 << bit) != 0;
            word = (ushort)(word << 2 | (clock ? 2 : 0) | (data ? 1 : 0));
            _previousData = data;
        }
        _track.Add(word);
    }

    internal static byte Decode(ushort word)
    {
        byte value = 0;
        for (var bit = 14; bit >= 0; bit -= 2) value = (byte)(value << 1 | (word >> bit & 1));
        return value;
    }

    internal ushort CrcPrefix(byte mark)
    {
        ushort crc = 0xffff;
        if (_mfm) for (var sync = 0; sync < 3; sync++) crc = Crc(crc, 0xa1);
        return Crc(crc, mark);
    }

    internal static ushort Crc(ushort crc, byte data)
    {
        for (var bit = 7; bit >= 0; bit--)
        {
            var feedback = ((crc >> 15) ^ (data >> bit)) & 1;
            crc = (ushort)(crc << 1 ^ (feedback != 0 ? 0x1021 : 0));
        }
        return crc;
    }
}
