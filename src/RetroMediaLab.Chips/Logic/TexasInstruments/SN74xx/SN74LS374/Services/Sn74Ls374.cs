using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS374.Interfaces;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS374.Services;

/// <summary>TI SDLS165B: positive-edge octal register; OC controls only the output drivers.</summary>
/// <remarks>No reset pin exists. A supplied initial value is a model seed, not a
/// guaranteed power-up state. Null while driven means unknown; null while not driven
/// means high impedance. No host resources or other components are queried.</remarks>
public sealed class Sn74Ls374 : ISn74Ls374
{
    private long _inputClockCount;
    private bool _clockHigh;
    private byte _dataInput;
    private byte? _storedData;
    private bool _outputControlHigh;
    private byte? _publishedData;
    private bool _publishedDriven = true;

    public Sn74Ls374(long clockFrequency, byte? initialData = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clockFrequency);
        ClockFrequency = clockFrequency;
        _storedData = _publishedData = initialData;
    }

    public long ClockFrequency { get; }
    public long InputClockCount => _inputClockCount;
    public bool ClockHigh => _clockHigh;
    public byte DataInput => _dataInput;
    public byte? StoredData => _storedData;
    public bool OutputControlHigh => _outputControlHigh;
    public bool OutputsDriven => !_outputControlHigh;
    public byte? OutputData => _outputControlHigh ? null : _storedData;
    public Action<byte?, bool>? OutputsChanged { get; set; }

    public void SetData(byte data) => _dataInput = data;

    public void SetOutputControl(bool high)
    {
        if (_outputControlHigh == high) return;
        _outputControlHigh = high;
        PublishOutputs();
    }

    public void SetClock(bool high)
    {
        if (_clockHigh == high) return;
        _clockHigh = high;
        if (!high) return;
        _inputClockCount = checked(_inputClockCount + 1);
        _storedData = _dataInput;
        PublishOutputs();
    }

    /// <summary>A complete physical CLK period, starting and ending low.</summary>
    public void Clock()
    {
        RequireLowClock();
        SetClock(true);
        SetClock(false);
    }

    /// <summary>Stable inputs between callbacks; every output transition is retained.</summary>
    public void AdvanceClocks(long clocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        RequireLowClock();
        while (clocks > 0)
        {
            if (OutputsChanged is null || _outputControlHigh || _storedData == _dataInput)
            {
                _inputClockCount = checked(_inputClockCount + clocks);
                _storedData = _dataInput;
                PublishOutputs();
                return;
            }
            Clock();
            clocks--;
        }
    }

    private void RequireLowClock()
    {
        if (_clockHigh) throw new InvalidOperationException(nameof(ClockHigh));
    }

    private void PublishOutputs()
    {
        var driven = !_outputControlHigh;
        var data = driven ? _storedData : null;
        if (_publishedDriven == driven && _publishedData == data) return;
        _publishedDriven = driven;
        _publishedData = data;
        OutputsChanged?.Invoke(data, driven);
    }
}
