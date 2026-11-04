using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Enums;
using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronRamConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

/// <summary>Nominal eight-point DRAM transaction; each Tick is one 16 MHz period.</summary>
/// <remarks>Not an analogue propagation model. The board owns the memory and the supplied pins.</remarks>
internal sealed class ElectronRamSequencer(IElectronRamPins pins)
{
    private readonly ElectronRamData _data = new();
    private ushort _address;
    private byte _row;
    private byte _writeData;
    private bool _write;
    private bool _refresh;
    private int _phase;
    private ElectronRamEvent _event;

    internal bool Busy { get; private set; }
    internal bool EvenLoaded => _event == ElectronRamEvent.EvenData;
    internal bool OddLoaded => _event == ElectronRamEvent.OddData;
    internal bool Completed => _event == ElectronRamEvent.TransferComplete;
    internal byte Data => _data.Data;

    internal void Begin(ushort address, bool cpu, bool write = false, byte data = 0)
    {
        Prepare();
        _address = address;
        _row = ElectronRamAddress.Row(address);
        _writeData = data;
        _write = cpu && write && address < C.RamEndAddress;
        _refresh = false;
        Drive(_row, rowHigh: true, columnHigh: true, odd: false);
    }

    internal void BeginRefresh(byte row)
    {
        Prepare();
        _row = row;
        _write = false;
        _refresh = true;
        Drive(_row, rowHigh: true, columnHigh: true, odd: false);
    }

    internal ElectronRamEvent Tick()
    {
        switch (_phase)
        {
            case C.RowLatchPhase: LatchRow(); break;
            case C.EvenColumnPhase: SelectEvenColumn(); break;
            case C.EvenDataPhase: LoadEvenData(); break;
            case C.OddColumnPhase: SelectOddColumn(); break;
            case C.OddDataPhase: LoadOddData(); break;
            case C.RecoveryPhase: CompleteTransfer(); break;
            default: AdvanceIdle(); break;
        }
        return _event;
    }

    // The aligned video period already knows each phase. These same actions are
    // also dispatched by Tick for individual master-clock stepping.
    internal void LatchRow()
    {
        _event = ElectronRamEvent.None;
        if (!Busy) return;
        Drive(_row, rowHigh: false, columnHigh: true, odd: false);
        _phase++;
    }

    internal void SelectEvenColumn()
    {
        _event = ElectronRamEvent.None;
        if (!Busy) return;
        if (!_refresh) Drive(ElectronRamAddress.Column(_address, false), rowHigh: false, columnHigh: false, odd: false);
        _phase++;
    }

    internal void LoadEvenData()
    {
        _event = ElectronRamEvent.None;
        if (!Busy) return;
        if (!_refresh)
        {
            _data.LoadEven(pins.Data);
            _event = ElectronRamEvent.EvenData;
            Drive(ElectronRamAddress.Column(_address, true), rowHigh: false, columnHigh: true, odd: true);
        }
        _phase++;
    }

    internal void SelectOddColumn()
    {
        _event = ElectronRamEvent.None;
        if (!Busy) return;
        if (!_refresh) Drive(ElectronRamAddress.Column(_address, true), rowHigh: false, columnHigh: false, odd: true);
        _phase++;
    }

    internal void LoadOddData()
    {
        _event = ElectronRamEvent.None;
        if (!Busy) return;
        if (!_refresh)
        {
            _data.LoadOdd(pins.Data);
            _event = ElectronRamEvent.OddData;
        }
        _write = false;
        Drive(_row, rowHigh: true, columnHigh: true, odd: false);
        _phase++;
    }

    internal void AdvanceIdle()
    {
        _event = ElectronRamEvent.None;
        if (Busy) _phase++;
    }

    internal bool CompleteTransfer()
    {
        _event = ElectronRamEvent.None;
        if (!Busy) return false;
        Busy = false;
        _event = ElectronRamEvent.TransferComplete;
        _phase++;
        return true;
    }

    /// <summary>Release the bus without clearing the borrowed RAM or undoing a completed write strobe.</summary>
    internal void Cancel()
    {
        Busy = false;
        _write = false;
        _event = ElectronRamEvent.None;
        Drive(_row, rowHigh: true, columnHigh: true, odd: false);
    }

    private void Prepare()
    {
        if (Busy) throw new InvalidOperationException();
        _phase = C.RowLatchPhase;
        Busy = true;
        _event = ElectronRamEvent.None;
    }

    private void Drive(byte address, bool rowHigh, bool columnHigh, bool odd) =>
        pins.Drive(address, rowHigh, columnHigh, !_write,
            _write ? ElectronRamDataPins.Select(_writeData, odd) : (byte)0);
}
