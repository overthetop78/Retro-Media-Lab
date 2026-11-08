using T = RetroMediaLab.Chips.Memory.Generic.I2C.EEPROM128.Constants.Pcd8572TimingConstants;

namespace RetroMediaLab.Chips.Memory.Generic.I2C.EEPROM128.Services;

public sealed partial class CompactEeprom
{
    private readonly long _writeCycleClocks;
    private readonly byte[] _writeAddresses = new byte[T.WriteBufferBytes];
    private readonly byte[] _writeValues = new byte[T.WriteBufferBytes];
    private int _writeCount;

    public long ReferenceFrequency { get; }
    public long ClockCount { get; private set; }
    public long? NextClockEvent { get; private set; }
    public bool Writing => NextClockEvent is not null;
    public event Action<long?>? NextClockEventChanged;

    /// <summary>Référence temporelle monotone ; aucun sondage du maître ni tick inactif nécessaire.</summary>
    public void AdvanceTo(long clock)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(clock, ClockCount);
        if (NextClockEvent is { } due && due <= clock)
        {
            ClockCount = due;
            for (var index = 0; index < _writeCount; index++)
                _bytes[_writeAddresses[index]] = _writeValues[index];
            _writeCount = 0;
            NextClockEvent = null;
            NextClockEventChanged?.Invoke(null);
        }
        ClockCount = clock;
    }

    private void StartWriteCycle()
    {
        if (_writeCount == 0 || Writing) return;
        NextClockEvent = checked(ClockCount + _writeCycleClocks * _writeCount);
        NextClockEventChanged?.Invoke(NextClockEvent);
    }
}
