namespace RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;

public sealed partial class Acia6850
{
    public Action<bool, bool>? TransmitPinsChanged { get; set; }
    public Action<int>? ClockDividerChanged { get; set; }
    public int TransmitClockPhase => _transmitClockCounter;
    public int ReceiveClockPhase => _receiveClockCounter;

    private void SetTransmitLine(bool level)
    {
        if (_transmitLine == level) return;
        _transmitLine = level;
        if (!_masterReset && !_break)
            TransmitPinsChanged?.Invoke(level, RtsHigh);
    }

    private void NotifyControlChanges(bool previousData, bool previousRequest, int previousDivider)
    {
        if (previousData != TransmitBit || previousRequest != RtsHigh)
            TransmitPinsChanged?.Invoke(TransmitBit, RtsHigh);
        if (previousDivider != ClockDivider)
            ClockDividerChanged?.Invoke(ClockDivider);
    }
}
