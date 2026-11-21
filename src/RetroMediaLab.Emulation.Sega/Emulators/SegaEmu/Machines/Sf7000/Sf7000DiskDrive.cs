using System.IO;
using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Sf7000;

internal sealed class Sf7000DiskDrive
{
    private const int Cylinders = 40;
    private const int SectorsPerTrack = 16;
    private const int SectorSize = 256;
    private const int CellTicks = 16; // 2 microseconds at the FDC's 8 MHz clock.
    private const int RevolutionCells = C.ReferenceClockHz * 60 / 300 / CellTicks;
    private const int IndexCells = C.ReferenceClockHz / 500 / CellTicks;
    private static readonly Dictionary<string, int> s_encoding = new()
    {
        [TrackEncodingAttributeKeys.MfmPhysicalLayout] = 1,
        [TrackEncodingAttributeKeys.RequireCompletePhysicalTrack] = 1
    };

    private readonly IReadOnlyList<bool>?[] _tracks = new IReadOnlyList<bool>?[Cylinders];
    private byte[]? _image;
    private int _cylinder;
    private int _cell;
    private int _cellRemainder;
    private bool _motor;
    private bool _window;
    private ulong _lastMasterTick;
    private ulong _clockRemainder;

    internal Sf7000DiskDrive()
    {
        Controller = new Upd765(DriveStatus, Step) { DataRate = Upd765DataRate.Kbps250 };
    }

    internal Upd765 Controller { get; }
    internal bool Index => _motor && _image is not null && _cell < IndexCells;

    internal void Insert(string? path)
    {
        byte[]? image = path is null ? null : File.ReadAllBytes(path);
        if (image is not null && image.Length != Cylinders * SectorsPerTrack * SectorSize)
            throw new InvalidDataException(Path.GetFileName(path));
        _image = image;
        Array.Clear(_tracks);
    }

    internal void Reset()
    {
        _lastMasterTick = _clockRemainder = 0;
        _cellRemainder = _cell = 0;
        _motor = _window = false;
        Controller.Reset();
        Controller.SetIndex(false);
    }

    internal void SetControl(byte value, byte mask)
    {
        _motor = (mask & 2) != 0 && (value & 2) == 0;
        Controller.SetReset((mask & value & 8) != 0);
        Controller.SetTerminalCount((mask & value & 4) != 0);
        Controller.SetIndex(Index);
    }

    internal void Advance(ulong masterTick, uint masterClock)
    {
        if (masterTick <= _lastMasterTick) return;
        ulong numerator = (masterTick - _lastMasterTick) * C.ReferenceClockHz + _clockRemainder;
        int ticks = checked((int)(numerator / masterClock));
        _clockRemainder = numerator % masterClock;
        _lastMasterTick = masterTick;
        while (ticks != 0)
        {
            if (!_motor || _image is null)
            {
                Controller.Advance(ticks);
                return;
            }
            int step = Math.Min(ticks, CellTicks - _cellRemainder);
            Controller.Advance(step);
            ticks -= step;
            _cellRemainder += step;
            if (_cellRemainder != CellTicks) continue;
            _cellRemainder = 0;
            Controller.SetWriteClock(false);
            Controller.SetWriteClock(true);
            if (Controller.Active && Controller.HeadLoaded && Controller.Mfm)
            {
                _window = !_window;
                Controller.SetReadSignals(_window, false);
                if (Track()[_cell])
                    Controller.SetReadSignals(_window, true);
            }
            _cell = (_cell + 1) % RevolutionCells;
            Controller.SetIndex(Index);
        }
    }

    private byte DriveStatus(int unit) => unit == 0
        ? (byte)((_cylinder == 0 ? C.TrackZeroPin : 0) | (_motor && _image is not null ? C.ReadyPin : 0) | C.WriteProtectedPin)
        : (byte)0;

    private void Step(int unit, int direction)
    {
        if (unit == 0) _cylinder = Math.Clamp(_cylinder + direction, 0, Cylinders - 1);
    }

    private IReadOnlyList<bool> Track()
    {
        if (_tracks[_cylinder] is { } track) return track;
        var sectors = new TrackSector[SectorsPerTrack];
        int offset = _cylinder * SectorsPerTrack * SectorSize;
        for (int sector = 0; sector < sectors.Length; sector++)
            sectors[sector] = new TrackSector(sector + 1, _image!.AsSpan(offset + sector * SectorSize, SectorSize).ToArray());
        return _tracks[_cylinder] = new IsoMfmTrackEncoder().Encode(
            new TrackEncodeRequest(_cylinder, 0, sectors, s_encoding, CellTicks, RevolutionCells * CellTicks)).Bits;
    }
}
